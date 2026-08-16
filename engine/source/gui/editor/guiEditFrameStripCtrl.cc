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

#include "gui/editor/guiEditFrameStripCtrl.h"
#include "graphics/dgl.h"
#include "gui/guiDefaultControlRender.h"
#include "console/consoleTypes.h"

#include "guiEditFrameStripCtrl_ScriptBinding.h"

IMPLEMENT_CONOBJECT(GuiEditFrameStripCtrl);

GuiEditFrameStripCtrl::GuiEditFrameStripCtrl()
{
	mImageAssetID = StringTable->EmptyString;
	mImageAsset = NULL;
	mCellSize = smDefaultCellSize;
	mCellPad = smDefaultCellPad;
	mShowFrameNumbers = true;
	mHoverCell = -1;
	mActive = true;
}

//-----------------------------------------------------------------------------
// The layout. Every one of these takes what it uses and touches no member, so
// the paint and the hit test can share them and a unit test can reach them.
//-----------------------------------------------------------------------------

S32 GuiEditFrameStripCtrl::getCellAdvance(S32 cellSize, S32 cellPad)
{
	return getMax(1, cellSize) + getMax(0, cellPad);
}

S32 GuiEditFrameStripCtrl::getColumnsFor(S32 contentWidth, S32 cellSize, S32 cellPad)
{
	// n cells span n advances less the pad the last one does not need, so the
	// question "how many fit" is asked of a width one pad wider than the real one.
	const S32 advance = getCellAdvance(cellSize, cellPad);
	const S32 columns = (contentWidth + getMax(0, cellPad)) / advance;

	return getMax(1, columns);
}

S32 GuiEditFrameStripCtrl::getRowCountFor(S32 cellCount, S32 columns)
{
	if (cellCount < 1)
	{
		return 0;
	}

	columns = getMax(1, columns);
	return ((cellCount + columns) - 1) / columns;
}

RectI GuiEditFrameStripCtrl::getCellRect(S32 index, S32 columns, S32 cellSize, S32 cellPad)
{
	columns = getMax(1, columns);
	cellSize = getMax(1, cellSize);

	const S32 advance = getCellAdvance(cellSize, cellPad);
	const S32 row = index / columns;
	const S32 column = index % columns;

	return RectI(column * advance, row * advance, cellSize, cellSize);
}

S32 GuiEditFrameStripCtrl::cellAt(const Point2I& local, S32 columns, S32 cellCount, S32 cellSize, S32 cellPad)
{
	if (cellCount < 1 || local.x < 0 || local.y < 0)
	{
		return -1;
	}

	columns = getMax(1, columns);
	cellSize = getMax(1, cellSize);

	const S32 advance = getCellAdvance(cellSize, cellPad);

	// The remainder of the advance is the gap after a cell. Landing in it is
	// landing on nothing, which is a different answer from landing on a neighbour.
	if ((local.x % advance) >= cellSize || (local.y % advance) >= cellSize)
	{
		return -1;
	}

	const S32 column = local.x / advance;
	if (column >= columns)
	{
		return -1;
	}

	const S32 index = ((local.y / advance) * columns) + column;

	// Past the end: the tail of a short last row is empty, not the last cell.
	return (index < cellCount) ? index : -1;
}

Point2I GuiEditFrameStripCtrl::getContentExtent(S32 cellCount, S32 columns, S32 cellSize, S32 cellPad)
{
	if (cellCount < 1)
	{
		return Point2I(0, 0);
	}

	columns = getMax(1, columns);
	cellSize = getMax(1, cellSize);

	const S32 advance = getCellAdvance(cellSize, cellPad);
	const S32 usedColumns = getMin(cellCount, columns);
	const S32 rows = getRowCountFor(cellCount, columns);

	// One advance per cell, less the trailing gap the last one in each direction
	// does not need.
	return Point2I((usedColumns * advance) - getMax(0, cellPad),
	               (rows * advance) - getMax(0, cellPad));
}

//-----------------------------------------------------------------------------

void GuiEditFrameStripCtrl::initPersistFields()
{
	Parent::initPersistFields();

	addProtectedField("Image", TypeAssetId, Offset(mImageAssetID, GuiEditFrameStripCtrl), &setImage, &getImage,
		"The image asset whose frames the cells are drawn from.");
	addField("CellSize", TypeS32, Offset(mCellSize, GuiEditFrameStripCtrl),
		"The pixel size of the square one frame draws in.");
	addField("CellPad", TypeS32, Offset(mCellPad, GuiEditFrameStripCtrl),
		"The gap between two cells.");
	addField("ShowFrameNumbers", TypeBool, Offset(mShowFrameNumbers, GuiEditFrameStripCtrl),
		"Whether each cell is labelled with the image frame it is showing.");
}

//-----------------------------------------------------------------------------

void GuiEditFrameStripCtrl::setImageAssetId(const char* pImageAssetID)
{
	// Sanity!
	AssertFatal(pImageAssetID != NULL, "Cannot use a NULL asset ID.");

	mImageAssetID = StringTable->insert(pImageAssetID);

	// Resolved now rather than deferred to onWake, as GuiTreeViewCtrl's icon
	// sheet is: nothing is lending this asset to anyone, so there is no refcount
	// to wait on. An empty id clears, which is how the grid is emptied.
	if (mImageAssetID != StringTable->EmptyString)
	{
		mImageAsset = pImageAssetID;
	}
	else
	{
		mImageAsset.clear();
	}

	mHoverCell = -1;
	updateExtent();
}

S32 GuiEditFrameStripCtrl::getImageFrameCount() const
{
	if (mImageAsset.isNull() || !mImageAsset->isAssetValid())
	{
		return 0;
	}

	return (S32)mImageAsset->getFrameCount();
}

void GuiEditFrameStripCtrl::setCellSize(S32 cellSize)
{
	const S32 clamped = mClamp(cellSize, smMinCellSize, smMaxCellSize);
	if (clamped == mCellSize)
	{
		return;
	}

	mCellSize = clamped;
	updateExtent();
}

S32 GuiEditFrameStripCtrl::getColumnCount()
{
	Point2I offset(0, 0);
	const RectI content = getContentRect(offset);

	return getColumnsFor(content.extent.x, mCellSize, mCellPad);
}

//-----------------------------------------------------------------------------

bool GuiEditFrameStripCtrl::onWake()
{
	if (!Parent::onWake())
	{
		return false;
	}

	updateExtent();
	return true;
}

void GuiEditFrameStripCtrl::onSleep()
{
	mHoverCell = -1;
	Parent::onSleep();
}

//-----------------------------------------------------------------------------

RectI GuiEditFrameStripCtrl::getContentRect(const Point2I& offset)
{
	// NORMAL state in both the paint and the hit test. A profile that pads its
	// highlighted state differently would otherwise shift every cell the moment
	// the pointer arrived, so a click would land where the cell used to be.
	Point2I contentOffset = offset;
	Point2I contentExtent = mBounds.extent;

	return getInnerRect(contentOffset, contentExtent, NormalState, mProfile);
}

Point2I GuiEditFrameStripCtrl::getOuterExtentForCells()
{
	Point2I inner = getContentExtent(getCellCount(), getColumnCount(), mCellSize, mCellPad);

	// The chrome the cells sit inside is paid for on top of them.
	return getOuterExtent(inner, NormalState, mProfile);
}

void GuiEditFrameStripCtrl::updateExtent()
{
	// A GuiScrollCtrl takes its content size from this control's own extent
	// (calcChildExtents), so resizing IS how the bars are told. Nothing else has
	// to be notified.
	const Point2I desired = getDesiredExtent();

	if (desired == mBounds.extent)
	{
		return;
	}

	resize(mBounds.point, desired);
}

void GuiEditFrameStripCtrl::parentResized(const Point2I& oldParentExtent, const Point2I& newParentExtent)
{
	Parent::parentResized(oldParentExtent, newParentExtent);

	// Recomputed from the width this control now has, never from a remembered
	// one. A scroller calls this a second time with only the delta its bars cost
	// (notifyChildrenOfBarChange), and a pure recompute settles on that pass
	// instead of stacking one adjustment on the other.
	updateExtent();
}

//-----------------------------------------------------------------------------

void GuiEditFrameStripCtrl::onRender(Point2I offset, const RectI& updateRect)
{
	RectI ctrlRect = applyMargins(offset, mBounds.extent, NormalState, mProfile);
	if (!ctrlRect.isValidRect())
	{
		return;
	}

	renderUniversalRect(ctrlRect, mProfile, NormalState);

	RectI fillRect = applyBorders(ctrlRect.point, ctrlRect.extent, NormalState, mProfile);
	RectI contentRect = applyPadding(fillRect.point, fillRect.extent, NormalState, mProfile);
	if (!contentRect.isValidRect())
	{
		return;
	}

	const S32 cellCount = getCellCount();
	const S32 columns = getColumnCount();

	const RectI oldClip = dglGetClipRect();
	RectI clipRect = contentRect;
	if (clipRect.intersect(oldClip))
	{
		dglSetClipRect(clipRect);

		for (S32 i = 0; i < cellCount; ++i)
		{
			RectI cellRect = getCellRect(i, columns, mCellSize, mCellPad);
			cellRect.point += contentRect.point;

			// A sheet can have a thousand frames and a scroller shows a dozen.
			// Culling here is the difference between drawing what is on screen and
			// drawing all of it and letting the clip throw it away.
			if (!cellRect.overlaps(clipRect))
			{
				continue;
			}

			renderCell(i, cellRect, i == mHoverCell);
		}

		renderOverlay(contentRect);

		dglSetClipRect(oldClip);
	}

	renderChildControls(offset, contentRect, updateRect);
}

bool GuiEditFrameStripCtrl::getCellBackColor(S32 index, bool isHovered, ColorI& color)
{
	if (!isHovered)
	{
		return false;
	}

	color = mProfile->getFillColor(HighlightState);
	return true;
}

void GuiEditFrameStripCtrl::getCellLabel(S32 index, char* buffer, U32 bufferSize)
{
	const S32 frame = getFrameAt(index);

	// The name, when the sheet has one for this frame.
	//
	// This is the whole visible point of naming a cell: an animation built out of
	// "block1 block2" reads as those cells in the palette and the timeline, not as
	// two numbers the person then has to look up somewhere else.
	if (isNamedMode() && frame >= 0)
	{
		StringTableEntry name = mImageAsset->getExplicitCellName(frame);
		if (name != StringTable->EmptyString)
		{
			dStrncpy(buffer, name, bufferSize - 1);
			buffer[bufferSize - 1] = '\0';
			return;
		}
	}

	dSprintf(buffer, bufferSize, "%d", frame);
}

const ColorI& GuiEditFrameStripCtrl::getCellLabelColor(S32 index, bool isHovered)
{
	return mProfile->getFontColor(isHovered ? HighlightState : NormalState);
}

//-----------------------------------------------------------------------------

S32 GuiEditFrameStripCtrl::clipLabel(GFont* font, const char* text, S32 maxWidth, char* out, U32 outSize)
{
	static const char* ellipsis = "...";
	static const U32 ellipsisLength = 3;

	dStrncpy(out, text, outSize - 1);
	out[outSize - 1] = '\0';

	S32 width = (S32)font->getStrWidth((const UTF8*)out);
	if (width <= maxWidth)
	{
		return width;
	}

	const S32 ellipsisWidth = (S32)font->getStrWidth((const UTF8*)ellipsis);
	if (ellipsisWidth > maxWidth)
	{
		out[0] = '\0';
		return 0;
	}

	// Walked DOWN from the full length rather than up from nothing. A label that
	// only just fails to fit is by far the commonest case, and each step costs a
	// measurement of the whole string.
	U32 length = dStrlen(out);
	if (length > outSize - ellipsisLength - 1)
	{
		length = outSize - ellipsisLength - 1;
	}

	while (length > 0)
	{
		out[length] = '\0';
		width = (S32)font->getStrWidth((const UTF8*)out) + ellipsisWidth;
		if (width <= maxWidth)
		{
			break;
		}
		--length;
	}

	dStrcat(out, ellipsis);
	return width;
}

//-----------------------------------------------------------------------------

const char* GuiEditFrameStripCtrl::tipForPoint(const Point2I& globalPoint)
{
	const S32 cell = cellAtGlobal(globalPoint);
	if (cell < 0 || cell >= getCellCount())
	{
		return NULL;
	}

	// The unclipped name. A cell is 48 pixels and a region name is whatever
	// somebody typed, so the label in the cell is frequently the front of a word
	// and this is the only place the rest of it can be read.
	static char tipBuffer[256];
	getCellLabel(cell, tipBuffer, sizeof(tipBuffer));

	return (tipBuffer[0] != '\0') ? tipBuffer : NULL;
}

bool GuiEditFrameStripCtrl::renderTooltip(Point2I& cursorPos, const char* tipText)
{
	// Per cell rather than per control, the way GuiEditorExplorerTree's gutter
	// does it: the canvas re-calls this every frame once the pointer has settled
	// and decides whether to by comparing whole controls, so the text can follow
	// the cursor from cell to cell with nothing having to invalidate it.
	if (tipText == NULL)
	{
		const char* cellTip = tipForPoint(cursorPos);
		if (cellTip != NULL)
		{
			tipText = cellTip;
		}
	}

	return Parent::renderTooltip(cursorPos, tipText);
}

//-----------------------------------------------------------------------------

void GuiEditFrameStripCtrl::renderCell(S32 index, const RectI& cellRect, bool isHovered)
{
	const S32 frame = getFrameAt(index);

	// The background first, so an opaque theme color sits behind the art rather
	// than over it.
	ColorI backColor;
	if (getCellBackColor(index, isHovered, backColor))
	{
		dglDrawRectFill(cellRect, backColor);
	}

	// White is untinted. Set every cell rather than once for the loop, because a
	// subclass drawing its own chrome between cells will have changed it.
	dglSetBitmapModulation(ColorF(1.0f, 1.0f, 1.0f, 1.0f));
	renderImageAssetFrame(cellRect, mImageAsset, (U32)frame);
	dglClearBitmapModulation();

	if (!mShowFrameNumbers)
	{
		return;
	}

	GFont* font = mProfile->getFont(mFontSizeAdjust);
	if (font == NULL)
	{
		return;
	}

	// Sized for a name, not for a number. A region name comes out of a TAML
	// attribute and has no length limit, so the 16 that comfortably held "%d"
	// would have truncated most of them.
	char buffer[128];
	getCellLabel(index, buffer, sizeof(buffer));

	const S32 textHeight = (S32)font->getHeight();

	// Height is still all-or-nothing -- there is no clipping a line of text
	// vertically that leaves it readable -- but the width is now fitted rather
	// than refused, because a name long enough to overflow its cell is the normal
	// case rather than the exception a frame number was.
	if (textHeight > cellRect.extent.y)
	{
		return;
	}

	char clipped[132];
	const S32 textWidth = clipLabel(font, buffer, cellRect.extent.x, clipped, sizeof(clipped));
	if (textWidth == 0)
	{
		return;
	}

	const Point2I textPoint(cellRect.point.x + ((cellRect.extent.x - textWidth) / 2),
	                        (cellRect.point.y + cellRect.extent.y) - textHeight);

	dglSetBitmapModulation(getCellLabelColor(index, isHovered));
	dglDrawText(font, textPoint, (const UTF8*)clipped);
	dglClearBitmapModulation();
}

//-----------------------------------------------------------------------------

S32 GuiEditFrameStripCtrl::cellAtGlobal(const Point2I& globalPoint)
{
	Point2I origin(0, 0);
	const RectI content = getContentRect(origin);

	Point2I local = globalToLocalCoord(globalPoint);
	local -= content.point;

	return cellAt(local, getColumnCount(), getCellCount(), mCellSize, mCellPad);
}

RectI GuiEditFrameStripCtrl::getCellRectGlobal(S32 index)
{
	if (index < 0 || index >= getCellCount())
	{
		return RectI(0, 0, 0, 0);
	}

	Point2I origin(0, 0);
	const RectI content = getContentRect(origin);

	RectI cellRect = getCellRect(index, getColumnCount(), mCellSize, mCellPad);
	cellRect.point += content.point;
	cellRect.point = localToGlobalCoord(cellRect.point);

	return cellRect;
}

//-----------------------------------------------------------------------------

void GuiEditFrameStripCtrl::onTouchMove(const GuiEvent& event)
{
	const S32 cell = cellAtGlobal(event.mousePoint);
	if (cell != mHoverCell)
	{
		mHoverCell = cell;
		setUpdate();
	}

	Parent::onTouchMove(event);
}

void GuiEditFrameStripCtrl::onTouchLeave(const GuiEvent& event)
{
	if (mHoverCell != -1)
	{
		mHoverCell = -1;
		setUpdate();
	}

	Parent::onTouchLeave(event);
}

// Deliberately no mouse wheel handler.
//
// It used to zoom the cells, and that was wrong twice over: a wheel over a
// scrolling list of pictures means scroll, and taking the event meant the wheel
// never reached the scroller -- so shrinking the cells until they all fitted was
// the only way to reach the frames at the bottom. Left unhandled, the event
// bubbles to the scroller and does what everyone expects. Cell size is still a
// field a pane can set.
