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

#include "gui/editor/guiEditFramePaletteCtrl.h"
#include "console/consoleTypes.h"

IMPLEMENT_CONOBJECT(GuiEditFramePaletteCtrl);

GuiEditFramePaletteCtrl::GuiEditFramePaletteCtrl()
{
	mPressed = false;
	mPressCell = -1;
	mPressAt.set(0, 0);
	mDragged = false;
}

//-----------------------------------------------------------------------------

S32 GuiEditFramePaletteCtrl::getCellCount() const
{
	return getImageFrameCount();
}

Point2I GuiEditFramePaletteCtrl::getDesiredExtent()
{
	// The width belongs to the scroller -- HorizSizing keeps this control as wide
	// as the space inside it -- and the height is however many rows that width
	// produced. Growing the width too would give the scroller something to scroll
	// sideways over, which is the one thing a wrapping palette must never do.
	return Point2I(mBounds.extent.x, getOuterExtentForCells().y);
}

//-----------------------------------------------------------------------------

void GuiEditFramePaletteCtrl::onTouchDown(const GuiEvent& event)
{
	const S32 cell = cellAtGlobal(event.mousePoint);
	if (cell == -1)
	{
		// A press on the gaps or past the last frame is not the start of
		// anything, so let it bubble -- the pane behind may want it.
		Parent::onTouchDown(event);
		return;
	}

	mPressed = true;
	mDragged = false;
	mPressCell = cell;
	mPressAt = event.mousePoint;

	// Held so the drag can be measured even as the pointer leaves. Handed
	// straight back the moment this becomes a real drag, because the
	// GuiDragAndDropCtrl script builds does its own capturing.
	mouseLock();
}

void GuiEditFramePaletteCtrl::onTouchDragged(const GuiEvent& event)
{
	if (!mPressed || mDragged)
	{
		return;
	}

	const Point2I travelled = event.mousePoint - mPressAt;
	if (mAbs(travelled.x) < smDragSlop && mAbs(travelled.y) < smDragSlop)
	{
		return;
	}

	mDragged = true;
	mPressed = false;
	mouseUnlock();

	// Script makes the payload and the drag control: what a frame looks like
	// while it is in flight, and where it may be dropped, are both questions
	// about the editor rather than about this grid.
	if (isMethod("onFrameDragBegan"))
	{
		Con::executef(this, 4, "onFrameDragBegan",
			Con::getIntArg(getFrameAt(mPressCell)),
			Con::getIntArg(event.mousePoint.x),
			Con::getIntArg(event.mousePoint.y));
	}
}

void GuiEditFramePaletteCtrl::onTouchUp(const GuiEvent& event)
{
	const bool wasPressed = mPressed;
	const S32 pressCell = mPressCell;

	mPressed = false;
	mPressCell = -1;

	// Safe whether or not this control is the one holding the capture:
	// GuiCanvas::mouseUnlock ignores a control that is not.
	mouseUnlock();

	// A release that followed a drag is that drag ending, not a click. Without
	// this the frame would be both dropped where it was let go AND appended to
	// the end, which is the double-fire GuiEditorControlTile guards against with
	// the same flag.
	if (mDragged)
	{
		mDragged = false;
		return;
	}

	if (!wasPressed)
	{
		Parent::onTouchUp(event);
		return;
	}

	// Released somewhere else entirely: the press is abandoned rather than
	// applied to whichever cell happens to be under the pointer now.
	if (cellAtGlobal(event.mousePoint) != pressCell)
	{
		return;
	}

	if (isMethod("onFrameClicked"))
	{
		Con::executef(this, 2, "onFrameClicked", Con::getIntArg(getFrameAt(pressCell)));
	}
}
