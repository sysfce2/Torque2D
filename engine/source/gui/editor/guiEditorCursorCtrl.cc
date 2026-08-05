//-----------------------------------------------------------------------------
// Copyright (c) 2013 GarageGames, LLC
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
//-----------------------------------------------------------------------------

#include "gui/editor/guiEditorCursorCtrl.h"
#include "gui/guiTypes.h"
#include "gui/guiDefaultControlRender.h"
#include "graphics/dgl.h"
#include "graphics/gBitmap.h"
#include "console/consoleTypes.h"
#include "io/resource/resourceManager.h"

#include "guiEditorCursorCtrl_ScriptBinding.h"

IMPLEMENT_CONOBJECT(GuiEditorCursorCtrl);

GuiEditorCursorCtrl::GuiEditorCursorCtrl()
{
	mCursor = NULL;
	mBitmap = NULL;
	mBitmapKey = StringTable->EmptyString;
	mZoom = 8;
	mDragging = false;

	// Defaults chosen to read against the stock art, which is a white body with
	// a black outline: a saturated red dot is visible on both, and the checker
	// greys are far enough from either to never be mistaken for the art.
	mDotColor.set(220, 40, 40, 255);
	mAnchorColor.set(90, 170, 255, 160);
	mCheckerLight.set(110, 110, 110, 255);
	mCheckerDark.set(80, 80, 80, 255);
	mGridColor.set(0, 0, 0, 60);
}

GuiEditorCursorCtrl::~GuiEditorCursorCtrl()
{
	releaseBitmap();
}

void GuiEditorCursorCtrl::initPersistFields()
{
	Parent::initPersistFields();

	addField("cursor", TypeGuiCursor, Offset(mCursor, GuiEditorCursorCtrl));
	addField("zoom", TypeS32, Offset(mZoom, GuiEditorCursorCtrl));
	addField("dotColor", TypeColorI, Offset(mDotColor, GuiEditorCursorCtrl));
	addField("anchorColor", TypeColorI, Offset(mAnchorColor, GuiEditorCursorCtrl));
	addField("checkerLight", TypeColorI, Offset(mCheckerLight, GuiEditorCursorCtrl));
	addField("checkerDark", TypeColorI, Offset(mCheckerDark, GuiEditorCursorCtrl));
	addField("gridColor", TypeColorI, Offset(mGridColor, GuiEditorCursorCtrl));
}

//-----------------------------------------------------------------------------
// The hot-spot arithmetic. Both directions are static and take everything they
// use: guiCanvas.cc positions a cursor at cursorPos - hotSpot and then
// GuiCursor::render subtracts (S32)(extent * renderOffset), so the pointer ends
// up at hotSpot + trunc(extent * renderOffset) within the image. The truncation
// is the engine's, reproduced rather than rounded, so the dot marks the pixel
// that will really be under the mouse.
//-----------------------------------------------------------------------------

Point2I GuiEditorCursorCtrl::getEffectiveHotSpot(const Point2I& hotSpot, const Point2F& renderOffset, const Point2I& imageExtent)
{
	return Point2I(hotSpot.x + (S32)(imageExtent.x * renderOffset.x),
				   hotSpot.y + (S32)(imageExtent.y * renderOffset.y));
}

Point2I GuiEditorCursorCtrl::getHotSpotForPixel(const Point2I& pixel, const Point2F& renderOffset, const Point2I& imageExtent)
{
	return Point2I(pixel.x - (S32)(imageExtent.x * renderOffset.x),
				   pixel.y - (S32)(imageExtent.y * renderOffset.y));
}

//-----------------------------------------------------------------------------

void GuiEditorCursorCtrl::setCursorObject(GuiCursor* cursor)
{
	mCursor = cursor;
	mDragging = false;
}

void GuiEditorCursorCtrl::releaseBitmap()
{
	if (mBitmap != NULL)
	{
		delete mBitmap;
		mBitmap = NULL;
	}
	mBitmapKey = StringTable->EmptyString;
}

const GBitmap* GuiEditorCursorCtrl::resolveBitmap()
{
	if (mCursor == NULL)
	{
		releaseBitmap();
		return NULL;
	}

	StringTableEntry key = mCursor->getBitmapName();
	if (key == NULL || *key == '\0')
	{
		releaseBitmap();
		return NULL;
	}

	// Reload when the cursor is pointed at different art - which is exactly what
	// happens when the user picks a file in the pane beside this.
	if (key != mBitmapKey)
	{
		releaseBitmap();
		mBitmapKey = key;

		// Straight from the resource manager rather than through a
		// TextureHandle: a cursor's handle is a BitmapTexture, whose CPU-side
		// bitmap the texture manager frees once it has uploaded it. The same
		// extension probe TextureManager::loadBitmap does, so a bitmapName
		// written without one (as AppCore's hand-made cursors always were) still
		// finds its file.
		static const char* const extensions[] = { "", ".png", ".jpg" };
		char pathBuffer[1024];
		for (S32 i = 0; i < 3 && mBitmap == NULL; ++i)
		{
			dSprintf(pathBuffer, sizeof(pathBuffer), "%s%s", key, extensions[i]);
			mBitmap = (GBitmap*)ResourceManager->loadInstance(pathBuffer);
		}

		if (mBitmap == NULL)
			Con::warnf("GuiEditorCursorCtrl - could not read the cursor art '%s'.", key);
	}

	return mBitmap;
}

void GuiEditorCursorCtrl::setZoom(S32 zoom)
{
	mZoom = mClamp(zoom, 1, getMaxZoom());
}

// The largest magnification whose art still fits the pane. Asking the control
// rather than assuming smMaxZoom is what keeps the zoom readout honest: a 32x32
// cursor in a 370x200 pane tops out at 6x, and a control that reported 16x while
// drawing 6x would be lying about the one number the user is steering by.
S32 GuiEditorCursorCtrl::getMaxZoom()
{
	const Point2I imageExtent = getImageExtent();
	if (imageExtent.x <= 0 || imageExtent.y <= 0)
		return smMaxZoom;

	Point2I globalOffset = localToGlobalCoord(Point2I(0, 0));
	Point2I extent = mBounds.extent;
	const RectI content = getInnerRect(globalOffset, extent, NormalState, mProfile);

	S32 zoom = smMaxZoom;
	while (zoom > 1 && (imageExtent.x * zoom > content.extent.x || imageExtent.y * zoom > content.extent.y))
		--zoom;

	return zoom;
}

Point2I GuiEditorCursorCtrl::getImageExtent()
{
	const GBitmap* bitmap = resolveBitmap();
	if (bitmap == NULL)
		return Point2I(0, 0);

	return Point2I((S32)bitmap->getWidth(), (S32)bitmap->getHeight());
}

bool GuiEditorCursorCtrl::getArtRect(RectI& artRect, Point2I& imageExtent)
{
	imageExtent = getImageExtent();
	if (imageExtent.x <= 0 || imageExtent.y <= 0)
		return false;

	// Global, because the hit test asks this question with a global mouse point
	// and the renderer draws in global coordinates too.
	Point2I globalOffset = localToGlobalCoord(Point2I(0, 0));
	Point2I extent = mBounds.extent;
	const RectI content = getInnerRect(globalOffset, extent, NormalState, mProfile);

	// Never magnify past what fits: a 32x32 cursor at 16x wants 512 pixels, and
	// a pane that narrow would otherwise clip the art the user is aiming at.
	// Written back rather than used locally, so what mZoom says and what is drawn
	// can never differ -- a readout showing a zoom nothing is drawn at is worse
	// than no readout. A pane that grows lets the user zoom in again.
	S32 zoom = mClamp(mZoom, 1, smMaxZoom);
	while (zoom > 1 && (imageExtent.x * zoom > content.extent.x || imageExtent.y * zoom > content.extent.y))
		--zoom;
	mZoom = zoom;

	const Point2I size(imageExtent.x * zoom, imageExtent.y * zoom);
	artRect.point.set(content.point.x + ((content.extent.x - size.x) / 2),
					  content.point.y + ((content.extent.y - size.y) / 2));
	artRect.extent = size;

	return true;
}

Point2I GuiEditorCursorCtrl::pixelAt(const Point2I& globalPoint, const RectI& artRect, const Point2I& imageExtent)
{
	const S32 zoom = artRect.extent.x / imageExtent.x;
	if (zoom <= 0)
		return Point2I(0, 0);

	return Point2I(mClamp((globalPoint.x - artRect.point.x) / zoom, 0, imageExtent.x - 1),
				   mClamp((globalPoint.y - artRect.point.y) / zoom, 0, imageExtent.y - 1));
}

void GuiEditorCursorCtrl::setHotSpotPixel(const Point2I& pixel)
{
	if (mCursor == NULL)
		return;

	const Point2I imageExtent = getImageExtent();
	if (imageExtent.x <= 0 || imageExtent.y <= 0)
		return;

	const Point2I hotSpot = getHotSpotForPixel(pixel, mCursor->getRenderOffset(), imageExtent);
	if (hotSpot == mCursor->getHotSpot())
		return;

	mCursor->setHotSpot(hotSpot);

	// The pane writes the field and marks the theme dirty; this only reports
	// that the value moved, so the control never has to know about themes.
	Con::executef(this, 3, "onHotSpotChanged", Con::getIntArg(hotSpot.x), Con::getIntArg(hotSpot.y));
}

//-----------------------------------------------------------------------------
// Rendering.
//-----------------------------------------------------------------------------

void GuiEditorCursorCtrl::onRender(Point2I offset, const RectI& updateRect)
{
	RectI ctrlRect = applyMargins(offset, mBounds.extent, NormalState, mProfile);
	renderUniversalRect(ctrlRect, mProfile, NormalState);

	RectI artRect;
	Point2I imageExtent;
	if (getArtRect(artRect, imageExtent))
	{
		renderChecker(artRect);
		renderArt(artRect, imageExtent);
		renderGrid(artRect, imageExtent);
		renderMarks(artRect, imageExtent);
	}

	Point2I contentOffset = offset;
	Point2I contentExtent = mBounds.extent;
	renderChildControls(offset, getInnerRect(contentOffset, contentExtent, NormalState, mProfile), updateRect);
}

void GuiEditorCursorCtrl::renderChecker(const RectI& artRect)
{
	// Squares in screen space rather than image space, so the backdrop stays the
	// same size as the zoom changes and never reads as part of the art.
	const S32 square = 8;

	dglDrawRectFill(artRect, mCheckerDark);

	for (S32 y = 0; y < artRect.extent.y; y += square)
	{
		for (S32 x = ((y / square) % 2) * square; x < artRect.extent.x; x += square * 2)
		{
			RectI cell(artRect.point.x + x, artRect.point.y + y,
					   getMin(square, artRect.extent.x - x), getMin(square, artRect.extent.y - y));
			dglDrawRectFill(cell, mCheckerLight);
		}
	}
}

void GuiEditorCursorCtrl::renderArt(const RectI& artRect, const Point2I& imageExtent)
{
	const GBitmap* bitmap = resolveBitmap();
	if (bitmap == NULL)
		return;

	const S32 zoom = artRect.extent.x / imageExtent.x;
	const ColorI& tint = mCursor->mColor;

	for (S32 y = 0; y < imageExtent.y; ++y)
	{
		for (S32 x = 0; x < imageExtent.x; ++x)
		{
			ColorI pixel;
			if (!bitmap->getColor((U32)x, (U32)y, pixel) || pixel.alpha == 0)
				continue;

			// The same multiply the canvas applies through bitmap modulation, so
			// what is magnified here is what will be drawn on screen.
			pixel.red = (U8)((pixel.red * tint.red) / 255);
			pixel.green = (U8)((pixel.green * tint.green) / 255);
			pixel.blue = (U8)((pixel.blue * tint.blue) / 255);
			pixel.alpha = (U8)((pixel.alpha * tint.alpha) / 255);

			dglDrawRectFill(RectI(artRect.point.x + (x * zoom), artRect.point.y + (y * zoom), zoom, zoom), pixel);
		}
	}
}

void GuiEditorCursorCtrl::renderGrid(const RectI& artRect, const Point2I& imageExtent)
{
	const S32 zoom = artRect.extent.x / imageExtent.x;

	// Below this the lines cost more than the pixels they separate.
	if (zoom < 4)
		return;

	for (S32 x = 1; x < imageExtent.x; ++x)
	{
		const S32 lineX = artRect.point.x + (x * zoom);
		dglDrawLine(lineX, artRect.point.y, lineX, artRect.point.y + artRect.extent.y, mGridColor);
	}

	for (S32 y = 1; y < imageExtent.y; ++y)
	{
		const S32 lineY = artRect.point.y + (y * zoom);
		dglDrawLine(artRect.point.x, lineY, artRect.point.x + artRect.extent.x, lineY, mGridColor);
	}

	dglDrawRect(artRect, mGridColor);
}

void GuiEditorCursorCtrl::renderMarks(const RectI& artRect, const Point2I& imageExtent)
{
	const S32 zoom = artRect.extent.x / imageExtent.x;
	const Point2F renderOffset = mCursor->getRenderOffset();

	// The anchor: where renderOffset alone puts the pointer. Drawn first and
	// faintly, as crosshairs across the whole art, so the distance between it
	// and the dot IS the hotSpot nudge -- which is the one thing a reader needs
	// to understand why two fields exist.
	const Point2I anchor((S32)(imageExtent.x * renderOffset.x), (S32)(imageExtent.y * renderOffset.y));
	const S32 anchorX = artRect.point.x + (anchor.x * zoom);
	const S32 anchorY = artRect.point.y + (anchor.y * zoom);
	dglDrawLine(anchorX, artRect.point.y, anchorX, artRect.point.y + artRect.extent.y, mAnchorColor);
	dglDrawLine(artRect.point.x, anchorY, artRect.point.x + artRect.extent.x, anchorY, mAnchorColor);

	// The hot spot itself: the pixel that will sit under the mouse, boxed so the
	// pixel is identifiable, with a dot at its center for low zooms where the
	// box is only a few pixels across.
	const Point2I hot = getEffectiveHotSpot(mCursor->getHotSpot(), renderOffset, imageExtent);
	const RectI hotPixel(artRect.point.x + (hot.x * zoom), artRect.point.y + (hot.y * zoom), zoom, zoom);

	dglDrawRect(hotPixel, mDotColor);
	if (zoom >= 3)
	{
		const RectI inner(hotPixel.point.x + 1, hotPixel.point.y + 1, hotPixel.extent.x - 2, hotPixel.extent.y - 2);
		dglDrawRectFill(inner, mDotColor);
	}
	else
	{
		dglDrawRectFill(hotPixel, mDotColor);
	}
}

//-----------------------------------------------------------------------------
// Input.
//-----------------------------------------------------------------------------

void GuiEditorCursorCtrl::onTouchDown(const GuiEvent& event)
{
	RectI artRect;
	Point2I imageExtent;
	if (!getArtRect(artRect, imageExtent))
		return;

	// A press outside the art is not a hot spot: it would clamp to an edge pixel
	// nobody aimed at.
	if (!artRect.pointInRect(event.mousePoint))
		return;

	mDragging = true;
	mouseLock();
	setHotSpotPixel(pixelAt(event.mousePoint, artRect, imageExtent));
}

void GuiEditorCursorCtrl::onTouchDragged(const GuiEvent& event)
{
	if (!mDragging)
		return;

	RectI artRect;
	Point2I imageExtent;
	if (!getArtRect(artRect, imageExtent))
		return;

	// Clamped rather than ignored: a drag that wanders off the art should pin the
	// hot spot to the edge it left by, not stop responding.
	setHotSpotPixel(pixelAt(event.mousePoint, artRect, imageExtent));
}

void GuiEditorCursorCtrl::onTouchUp(const GuiEvent& event)
{
	if (!mDragging)
		return;

	mDragging = false;
	mouseUnlock();
}

void GuiEditorCursorCtrl::onMouseWheelUp(const GuiEvent& event)
{
	setZoom(mZoom + 1);
	Con::executef(this, 2, "onZoomChanged", Con::getIntArg(mZoom));
}

void GuiEditorCursorCtrl::onMouseWheelDown(const GuiEvent& event)
{
	setZoom(mZoom - 1);
	Con::executef(this, 2, "onZoomChanged", Con::getIntArg(mZoom));
}
