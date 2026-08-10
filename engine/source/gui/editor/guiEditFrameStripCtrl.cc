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

IMPLEMENT_CONOBJECT(GuiEditFrameStripCtrl);

GuiEditFrameStripCtrl::GuiEditFrameStripCtrl()
{
	mImageAssetID = StringTable->EmptyString;
	mImageAsset = NULL;
	mCellSize = smDefaultCellSize;
	mCellPad = smDefaultCellPad;
	mShowFrameNumbers = true;
	// Spelled out because ColorI's constructor leaves its components
	// uninitialised, so a field nobody sets is whatever was on the stack.
	mNumberColor.set(255, 255, 255, 160);
	mHoverColor.set(255, 255, 255, 90);
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
	addField("NumberColor", TypeColorI, Offset(mNumberColor, GuiEditFrameStripCtrl),
		"The ink those labels are drawn in.");
	addField("HoverColor", TypeColorI, Offset(mHoverColor, GuiEditFrameStripCtrl),
		"The wash over the cell under the pointer.");
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

void GuiEditFrameStripCtrl::renderCell(S32 index, const RectI& cellRect, bool isHovered)
{
	const S32 frame = getFrameAt(index);

	// White is untinted. Set every cell rather than once for the loop, because a
	// subclass drawing its own chrome between cells will have changed it.
	dglSetBitmapModulation(ColorF(1.0f, 1.0f, 1.0f, 1.0f));
	renderImageAssetFrame(cellRect, mImageAsset, (U32)frame);
	dglClearBitmapModulation();

	if (isHovered)
	{
		dglDrawRectFill(cellRect, mHoverColor);
	}

	if (!mShowFrameNumbers)
	{
		return;
	}

	GFont* font = mProfile->getFont(mFontSizeAdjust);
	if (font == NULL)
	{
		return;
	}

	char buffer[16];
	dSprintf(buffer, sizeof(buffer), "%d", frame);

	const S32 textWidth = font->getStrWidth((const UTF8*)buffer);
	const S32 textHeight = (S32)font->getHeight();

	// Only when the label fits inside the cell it belongs to. A number spilling
	// over its neighbours is worse than no number.
	if (textWidth > cellRect.extent.x || textHeight > cellRect.extent.y)
	{
		return;
	}

	const Point2I textPoint(cellRect.point.x + ((cellRect.extent.x - textWidth) / 2),
	                        (cellRect.point.y + cellRect.extent.y) - textHeight);

	dglSetBitmapModulation(mNumberColor);
	dglDrawText(font, textPoint, (const UTF8*)buffer);
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

void GuiEditFrameStripCtrl::onMouseWheelUp(const GuiEvent& event)
{
	// A magnifier over the art, which is what a wheel over a grid of pictures
	// should do. The scroller only gets the wheel when this declines it, which is
	// why the step reports back: at either end there is nothing to zoom and the
	// gesture should go back to scrolling.
	const S32 before = mCellSize;
	setCellSize(mCellSize + 8);

	if (mCellSize == before)
	{
		Parent::onMouseWheelUp(event);
		return;
	}

	if (isMethod("onCellSizeChanged"))
	{
		Con::executef(this, 2, "onCellSizeChanged", Con::getIntArg(mCellSize));
	}
}

void GuiEditFrameStripCtrl::onMouseWheelDown(const GuiEvent& event)
{
	const S32 before = mCellSize;
	setCellSize(mCellSize - 8);

	if (mCellSize == before)
	{
		Parent::onMouseWheelDown(event);
		return;
	}

	if (isMethod("onCellSizeChanged"))
	{
		Con::executef(this, 2, "onCellSizeChanged", Con::getIntArg(mCellSize));
	}
}
