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

#ifndef _GUI_EDITOR_CURSOR_CTRL_H_
#define _GUI_EDITOR_CURSOR_CTRL_H_

#ifndef _GUICONTROL_H_
#include "gui/guiControl.h"
#endif

//-----------------------------------------------------------------------------
// The Gui Profile Editor's hot-spot editor: one cursor's art, magnified, with
// its hot spot marked and draggable.
//
// This exists because a hot spot cannot be set by typing numbers. It is a
// single pixel in a 13x17 image, and whether it is the right pixel is a
// question about what the art looks like -- so the only way to answer it is to
// see the two together at a size where individual pixels are visible.
//
// Two fields decide where the pointer lands, and the display keeps them
// distinct because they do different jobs:
//
//   renderOffset  a FRACTION of the bitmap's own size, so "0.5 0.5" means
//                 "centered" whatever the art measures. This is what stops a
//                 13x17 arrow and a 32x32 sizer from appearing to leap when one
//                 replaces the other, and it is drawn as the faint anchor mark.
//   hotSpot       a pixel nudge applied on top of it, drawn as the bright dot.
//
// The pointer ends up at hotSpot + trunc(extent * renderOffset), which is the
// dot; dragging moves the dot and writes hotSpot, leaving the anchor alone.
//
// Art is drawn a pixel at a time rather than as a stretched bitmap: magnifying
// through the texture filter would blur exactly the pixel boundaries the user
// is trying to aim at. Transparent pixels are skipped, which is most of a
// cursor.
//
// Editor-only, like guiEditCtrl and the explorer tree, and never offered in the
// palette (every class whose name begins "GuiEdit" is refused there -- this one
// is excluded by having no entry in the placeable list at all).
//-----------------------------------------------------------------------------

class GuiCursor;
class GBitmap;

class GuiEditorCursorCtrl : public GuiControl
{
private:
	typedef GuiControl Parent;

protected:
	GuiCursor* mCursor;         ///< The cursor being edited. Nothing is drawn without one.

	/// The cursor we hold a delete notification from, which is not always the one
	/// in mCursor: the "cursor" field is written straight through TypeGuiCursor,
	/// which overwrites the pointer without telling us what used to be there. So
	/// the registration is tracked separately and reconciled after every write.
	GuiCursor* mNotifiedCursor;

	/// Point the delete notification at whatever mCursor now holds. Cheap and
	/// idempotent, so it is safe to call after any write to the field.
	void bindCursorNotify();

	// Its own decoded copy of the art, because the pixels are the point. A
	// cursor's TextureHandle is a BitmapTexture, and TextureManager deletes the
	// CPU-side bitmap once it has uploaded one of those -- so asking the cursor
	// for its pixels gets NULL. Reloaded whenever the cursor, or the file it
	// names, changes.
	GBitmap* mBitmap;
	StringTableEntry mBitmapKey;
	S32 mZoom;                  ///< Magnification, 1 to smMaxZoom.
	ColorI mDotColor;           ///< The hot-spot mark. User-settable because a dark dot vanishes on dark art.
	ColorI mAnchorColor;        ///< The renderOffset anchor mark.
	ColorI mCheckerLight;       ///< The two squares of the transparency backdrop.
	ColorI mCheckerDark;
	ColorI mGridColor;          ///< Pixel grid, drawn only when zoomed enough to be useful.
	bool mDragging;

	/// The decoded art, loading or reloading it if the cursor now names a
	/// different file. NULL when there is no cursor or the file is missing.
	const GBitmap* resolveBitmap();
	void releaseBitmap();

	/// The rect the magnified art occupies, in global coordinates, and the
	/// image size it was computed from. Returns false when there is nothing to
	/// draw (no cursor, or art that failed to load).
	bool getArtRect(RectI& artRect, Point2I& imageExtent);

	/// Which image pixel a global point falls on, clamped into the image.
	Point2I pixelAt(const Point2I& globalPoint, const RectI& artRect, const Point2I& imageExtent);

	/// Move the hot spot to an image pixel and tell script. hotSpot absorbs the
	/// change; renderOffset is deliberately untouched.
	void setHotSpotPixel(const Point2I& pixel);

	void renderChecker(const RectI& artRect);
	void renderArt(const RectI& artRect, const Point2I& imageExtent);
	void renderGrid(const RectI& artRect, const Point2I& imageExtent);
	void renderMarks(const RectI& artRect, const Point2I& imageExtent);

public:
	static const S32 smMaxZoom = 16;

	/// Where the pointer sits within the image: the anchor, floored to a pixel,
	/// plus the nudge. Static and taking everything it uses, so the arithmetic
	/// that the renderer, the hit test and the script readout all depend on can
	/// be tested without a canvas -- rendering a cursor needs a GL context, and
	/// a unit test has none.
	static Point2I getEffectiveHotSpot(const Point2I& hotSpot, const Point2F& renderOffset, const Point2I& imageExtent);

	/// The inverse: the hotSpot that puts the pointer on a given pixel.
	static Point2I getHotSpotForPixel(const Point2I& pixel, const Point2F& renderOffset, const Point2I& imageExtent);

	GuiEditorCursorCtrl();
	~GuiEditorCursorCtrl();
	static void initPersistFields();

	void onRender(Point2I offset, const RectI& updateRect);

	/// A cursor can be deleted while this pane still points at it -- removing a
	/// theme's extra cursor does exactly that -- and the next frame would draw
	/// from freed memory. Forget it instead.
	virtual void onDeleteNotify(SimObject* object);

	/// The "cursor" field bypasses setCursorObject entirely (script assigns it
	/// directly, and so does a .gui.taml load), so this is the only place such a
	/// write can be noticed.
	virtual void onStaticModified(const char* slotName, const char* newValue = NULL);

	void onTouchDown(const GuiEvent& event);
	void onTouchDragged(const GuiEvent& event);
	void onTouchUp(const GuiEvent& event);

	/// Zoom in and out over the art, which is what a magnifier should do.
	void onMouseWheelUp(const GuiEvent& event);
	void onMouseWheelDown(const GuiEvent& event);

	inline GuiCursor* getCursor() const { return mCursor; }
	void setCursorObject(GuiCursor* cursor);
	void setZoom(S32 zoom);
	inline S32 getZoom() const { return mZoom; }

	/// The largest magnification the pane can actually show this art at. The
	/// zoom is clamped to it, so getZoom() never reports one that is not being
	/// drawn -- and the pane's "+" can go grey when there is no more to give.
	S32 getMaxZoom();

	/// The image's real size, or (0,0) when there is no art.
	Point2I getImageExtent();

	DECLARE_CONOBJECT(GuiEditorCursorCtrl);
};

#endif //_GUI_EDITOR_CURSOR_CTRL_H_
