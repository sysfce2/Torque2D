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

#include "gui/editor/guiEditFrameTimelineCtrl.h"
#include "graphics/dgl.h"
#include "console/consoleTypes.h"

#include "guiEditFrameTimelineCtrl_ScriptBinding.h"

IMPLEMENT_CONOBJECT(GuiEditFrameTimelineCtrl);

GuiEditFrameTimelineCtrl::GuiEditFrameTimelineCtrl()
{
	mSelected = -1;
	mCaret = -1;
	mPlayhead = -1;
	mPreview = NULL;

	mPressed = false;
	mDragging = false;
	mDragFrom = -1;
	mPressAt.set(0, 0);
	mDragOutside = false;
}

//-----------------------------------------------------------------------------
// Every color here comes off the profile, so the timeline follows the editor's
// theme like everything else. A profile offers four fills and four font colors,
// and this is what each is used for:
//
//   fill    HighlightState   the cell under the pointer
//   fill    SelectedState    the picked cell, and the run joining a held frame
//   fill    DisabledState    a cell that dragging further would throw away
//   font    SelectedState    the playhead bar -- an ink rather than a fill, so
//                            it stays legible against the selected cell it is
//                            frequently sitting on top of
//   font    HighlightState   the insertion caret
//   font    NormalState      the frame numbers (drawn by the base class)
//
// The strips wear frameGridProfile, which exists for this and nothing else --
// BaseTheme::makeFrameGridProfile says what each field is chosen to be and why
// borrowing listBoxProfile got two of them wrong.
//-----------------------------------------------------------------------------

//-----------------------------------------------------------------------------
// The two statics this class adds. Both take everything they use, so the caret
// the user sees and the index a drop lands on come from the same arithmetic.
//-----------------------------------------------------------------------------

S32 GuiEditFrameTimelineCtrl::insertionAt(const Point2I& local, S32 cellCount, S32 cellSize, S32 cellPad)
{
	if (cellCount < 1)
	{
		return 0;
	}

	cellSize = getMax(1, cellSize);

	const S32 advance = getCellAdvance(cellSize, cellPad);

	// Measured from the first cell's CENTRE rather than its edge, so the boundary
	// between "before this cell" and "after it" sits halfway across it.
	const S32 fromFirstCentre = local.x - (cellSize / 2);
	const S32 index = (fromFirstCentre < 0) ? 0 : ((fromFirstCentre / advance) + 1);

	return mClamp(index, 0, cellCount);
}

RectI GuiEditFrameTimelineCtrl::getCaretRect(S32 insertIndex, S32 cellCount, S32 cellSize, S32 cellPad, S32 height)
{
	cellSize = getMax(1, cellSize);
	cellCount = getMax(0, cellCount);
	height = getMax(0, height);

	const S32 advance = getCellAdvance(cellSize, cellPad);
	const S32 pad = getMax(0, cellPad);
	const S32 contentWidth = (cellCount < 1) ? 0 : ((cellCount * advance) - pad);

	insertIndex = mClamp(insertIndex, 0, cellCount);

	S32 x;
	if (insertIndex <= 0)
	{
		x = 0;
	}
	else if (insertIndex >= cellCount)
	{
		// Flush with the right-hand edge. There is no gap after the last cell to
		// sit in, and a caret drawn past the content is a caret clipped away.
		x = getMax(0, contentWidth - smCaretWidth);
	}
	else
	{
		// Centred in the gap, which touches neither neighbour.
		x = (insertIndex * advance) - (pad / 2) - (smCaretWidth / 2);
	}

	return RectI(x, 0, smCaretWidth, height);
}

//-----------------------------------------------------------------------------

S32 GuiEditFrameTimelineCtrl::getFrameAt(S32 index) const
{
	if (index < 0 || index >= mSlots.size())
	{
		return -1;
	}

	return mSlots[index];
}

Point2I GuiEditFrameTimelineCtrl::getDesiredExtent()
{
	// The height belongs to the scroller; the width is however far the cells
	// reach, which is what gives the horizontal bar something to scroll.
	return Point2I(getOuterExtentForCells().x, mBounds.extent.y);
}

//-----------------------------------------------------------------------------

const char* GuiEditFrameTimelineCtrl::getFrames()
{
	if (mSlots.size() == 0)
	{
		return "";
	}

	// 12 characters is room for a signed 32-bit number and its separator.
	const U32 bufferSize = (mSlots.size() * 12) + 1;
	char* buffer = Con::getReturnBuffer(bufferSize);
	U32 offset = 0;

	for (S32 i = 0; i < mSlots.size(); ++i)
	{
		offset += dSprintf(buffer + offset, bufferSize - offset, (i == 0) ? "%d" : " %d", mSlots[i]);
	}

	return buffer;
}

StringTableEntry GuiEditFrameTimelineCtrl::nameForFrame(S32 frame) const
{
	if (!isNamedMode() || frame < 0)
	{
		return StringTable->EmptyString;
	}

	return mImageAsset->getExplicitCellName(frame);
}

StringTableEntry GuiEditFrameTimelineCtrl::getNameAt(S32 index) const
{
	if (index < 0 || index >= mSlotNames.size())
	{
		return StringTable->EmptyString;
	}

	return mSlotNames[index];
}

bool GuiEditFrameTimelineCtrl::isSlotMissing(S32 index) const
{
	if (index < 0 || index >= mSlots.size())
	{
		return false;
	}

	// A name that resolved to no cell. Only ever true in named mode: a numbered
	// frame out of range is clamped by the asset rather than lost, so it has art
	// to draw and nothing to report.
	return mSlots[index] < 0 && getNameAt(index) != StringTable->EmptyString;
}

const char* GuiEditFrameTimelineCtrl::getNamedFrames()
{
	if (mSlotNames.size() == 0)
	{
		return "";
	}

	// Measured, because a region name has no length limit.
	U32 bufferSize = 1;
	for (S32 i = 0; i < mSlotNames.size(); ++i)
	{
		bufferSize += dStrlen(mSlotNames[i]) + 1;
	}

	char* buffer = Con::getReturnBuffer(bufferSize);
	U32 offset = 0;

	for (S32 i = 0; i < mSlotNames.size(); ++i)
	{
		offset += dSprintf(buffer + offset, bufferSize - offset, (i == 0) ? "%s" : " %s", mSlotNames[i]);
	}

	return buffer;
}

void GuiEditFrameTimelineCtrl::setNamedFrames(const char* names)
{
	mSlots.clear();
	mSlotNames.clear();

	if (names != NULL)
	{
		// Commas as well as whitespace, because that is what a named list looks
		// like coming out of a TAML field -- TypeStringTableEntryVector joins with
		// commas, and AnimationAsset::setNamedAnimationFrames splits on both for
		// exactly the same reason.
		char* copy = dStrdup(names);
		for (const char* token = dStrtok(copy, " \t\n,"); token != NULL; token = dStrtok(NULL, " \t\n,"))
		{
			StringTableEntry name = StringTable->insert(token);

			mSlotNames.push_back(name);

			// -1 when the image has no such cell. Kept as a slot rather than
			// skipped: the list the user gets back has to be the list they had, or
			// the next edit commits a deletion nobody asked for.
			mSlots.push_back(mImageAsset.notNull() ? mImageAsset->getExplicitCellIndex(name) : -1);
		}
		dFree(copy);
	}

	if (mSelected >= mSlots.size())
	{
		mSelected = -1;
	}
	mCaret = -1;

	updateExtent();
	setUpdate();
}

void GuiEditFrameTimelineCtrl::setFrames(const char* frames)
{
	mSlots.clear();
	mSlotNames.clear();

	if (frames != NULL)
	{
		// Split on the same three separators AnimationAsset::setAnimationFrames
		// uses, so a list can be handed straight from one to the other.
		char* copy = dStrdup(frames);
		for (const char* token = dStrtok(copy, " \t\n"); token != NULL; token = dStrtok(NULL, " \t\n"))
		{
			const S32 frame = dAtoi(token);

			mSlots.push_back(frame);
			mSlotNames.push_back(nameForFrame(frame));
		}
		dFree(copy);
	}

	if (mSelected >= mSlots.size())
	{
		mSelected = -1;
	}
	mCaret = -1;

	updateExtent();
	setUpdate();
}

bool GuiEditFrameTimelineCtrl::insertFrame(S32 slot, S32 frame)
{
	if (slot < 0 || slot > mSlots.size() || frame < 0)
	{
		return false;
	}

	mSlots.insert(slot);
	mSlots[slot] = frame;

	mSlotNames.insert(slot);
	mSlotNames[slot] = nameForFrame(frame);

	// The picked slot moves along with everything else it was standing after.
	if (mSelected >= slot)
	{
		mSelected++;
	}

	updateExtent();
	setUpdate();
	return true;
}

bool GuiEditFrameTimelineCtrl::removeSlot(S32 slot)
{
	if (slot < 0 || slot >= mSlots.size())
	{
		return false;
	}

	mSlots.erase(slot);
	mSlotNames.erase(slot);

	if (mSelected == slot)
	{
		// Stay on the same position rather than jumping to the start, so holding
		// Delete walks along the list removing one after another.
		mSelected = (slot < mSlots.size()) ? slot : (mSlots.size() - 1);
	}
	else if (mSelected > slot)
	{
		mSelected--;
	}

	updateExtent();
	setUpdate();
	return true;
}

bool GuiEditFrameTimelineCtrl::moveSlot(S32 from, S32 to)
{
	if (from < 0 || from >= mSlots.size() || to < 0 || to > mSlots.size())
	{
		return false;
	}

	// `to` is an insertion point measured against the list as it stands, so once
	// the slot is lifted out everything past it has shuffled down by one.
	const S32 frame = mSlots[from];
	StringTableEntry name = getNameAt(from);
	const S32 landing = (to > from) ? (to - 1) : to;

	if (landing == from)
	{
		return false;
	}

	mSlots.erase(from);
	mSlots.insert(landing);
	mSlots[landing] = frame;

	// Carried, not re-derived. A slot whose cell has gone has no index to look a
	// name up from, and moving it must not be what finally loses it.
	mSlotNames.erase(from);
	mSlotNames.insert(landing);
	mSlotNames[landing] = name;

	mSelected = landing;

	setUpdate();
	return true;
}

void GuiEditFrameTimelineCtrl::setSelectedSlot(S32 slot)
{
	const S32 settled = (slot >= 0 && slot < mSlots.size()) ? slot : -1;
	if (settled == mSelected)
	{
		return;
	}

	mSelected = settled;
	setUpdate();
}

void GuiEditFrameTimelineCtrl::selectSlotAndNotify(S32 slot)
{
	setSelectedSlot(slot);

	// Arrow keys scrub the preview exactly as a click does. Walking the list with
	// the keyboard and watching the frame change is the whole point of having
	// them; a selection that moved silently would be a highlight and nothing more.
	if (isMethod("onSlotSelected"))
	{
		Con::executef(this, 3, "onSlotSelected", Con::getIntArg(mSelected), Con::getIntArg(getFrameAt(mSelected)));
	}
}

void GuiEditFrameTimelineCtrl::setPreviewSprite(SpriteBase* sprite)
{
	mPreview = sprite;
	mPlayhead = -1;
	setUpdate();
}

//-----------------------------------------------------------------------------

S32 GuiEditFrameTimelineCtrl::showCaretAt(const Point2I& globalPoint)
{
	Point2I origin(0, 0);
	const RectI content = getContentRect(origin);

	Point2I local = globalToLocalCoord(globalPoint);
	local -= content.point;

	const S32 caret = insertionAt(local, mSlots.size(), mCellSize, mCellPad);
	if (caret != mCaret)
	{
		mCaret = caret;
		setUpdate();
	}

	return caret;
}

void GuiEditFrameTimelineCtrl::clearCaret()
{
	if (mCaret == -1)
	{
		return;
	}

	mCaret = -1;
	setUpdate();
}

bool GuiEditFrameTimelineCtrl::insertFrameAtPoint(const Point2I& globalPoint, S32 frame)
{
	Point2I origin(0, 0);
	const RectI content = getContentRect(origin);

	Point2I local = globalToLocalCoord(globalPoint);
	local -= content.point;

	const bool inserted = insertFrame(insertionAt(local, mSlots.size(), mCellSize, mCellPad), frame);
	if (inserted)
	{
		notifyFramesChanged();
	}

	return inserted;
}

void GuiEditFrameTimelineCtrl::notifyFramesChanged()
{
	if (isMethod("onFramesChanged"))
	{
		Con::executef(this, 1, "onFramesChanged");
	}
}

//-----------------------------------------------------------------------------

S32 GuiEditFrameTimelineCtrl::readPlayhead()
{
	if (mPreview.isNull() || mSlots.size() == 0)
	{
		return -1;
	}

	if (mPreview->isStaticFrameProvider() || !mPreview->isAnimationValid())
	{
		return -1;
	}

	// The ANIMATION frame -- the slot -- not getCurrentAnimationFrame, which is
	// the image frame. One image frame can fill several slots, and the marker has
	// to be on the one actually playing.
	const S32 slot = mPreview->getAnimationFrame();

	return (slot >= 0 && slot < mSlots.size()) ? slot : -1;
}

void GuiEditFrameTimelineCtrl::onPreRender()
{
	Parent::onPreRender();

	// preRender recurses from the canvas every frame, which makes this the
	// cheapest correct place to notice the preview has moved on, and the
	// documented place for a control to mark itself dirty. One integer read.
	const S32 playhead = readPlayhead();
	if (playhead != mPlayhead)
	{
		mPlayhead = playhead;
		setUpdate();
	}
}

//-----------------------------------------------------------------------------

bool GuiEditFrameTimelineCtrl::getCellBackColor(S32 index, bool isHovered, ColorI& color)
{
	// The slot being carried shows where it would end up: greyed while it is
	// still over the strip, and in the disabled fill once dragging further would
	// throw it away.
	if (mDragging && index == mDragFrom)
	{
		color = mProfile->getFillColor(mDragOutside ? DisabledState : HighlightState);
		return true;
	}

	if (index == mSelected)
	{
		color = mProfile->getFillColor(SelectedState);
		return true;
	}

	return Parent::getCellBackColor(index, isHovered, color);
}

void GuiEditFrameTimelineCtrl::getCellLabel(S32 index, char* buffer, U32 bufferSize)
{
	// A missing slot answers with the name that failed rather than with its
	// frame, which is -1 and tells the user nothing about what they have lost.
	if (isSlotMissing(index))
	{
		dStrncpy(buffer, getNameAt(index), bufferSize - 1);
		buffer[bufferSize - 1] = '\0';
		return;
	}

	Parent::getCellLabel(index, buffer, bufferSize);
}

const ColorI& GuiEditFrameTimelineCtrl::getCellLabelColor(S32 index, bool isHovered)
{
	// The same ink as the outline around it. A red box with a white word in it
	// reads as two things -- a broken cell, and a name -- when it is one: this
	// name is why the cell is broken.
	if (isSlotMissing(index))
	{
		return mProfile->getFontColor(DisabledState);
	}

	return Parent::getCellLabelColor(index, isHovered);
}

void GuiEditFrameTimelineCtrl::renderCell(S32 index, const RectI& cellRect, bool isHovered)
{
	Parent::renderCell(index, cellRect, isHovered);

	// A frame whose cell has gone. Nothing was drawn for the art -- the frame is
	// -1, and renderImageAssetFrame declines an out-of-range one -- so what is
	// left is an empty cell with a name under it, which reads as a cell that has
	// simply not loaded yet.
	//
	// Outlined rather than filled, and in the one profile color this control does
	// not otherwise use. All four fills are spoken for, and the disabled one in
	// particular already means "let go now and this is thrown away" during a drag
	// -- the opposite of what this slot needs to say, which is that it is still
	// here and wants attention.
	if (isSlotMissing(index))
	{
		dglDrawRect(cellRect, mProfile->getFontColor(DisabledState));
	}

	// Selected and playing are drawn differently on purpose -- a background and a
	// bar -- because they are frequently the same cell and the user needs to see
	// both. Scrubbing sets one and reads the other.
	if (index == mPlayhead)
	{
		RectI marker = cellRect;
		marker.extent.y = 3;
		dglDrawRectFill(marker, mProfile->getFontColor(SelectedState));
	}
}

void GuiEditFrameTimelineCtrl::renderOverlay(const RectI& contentRect)
{
	const S32 columns = getColumnCount();

	// A run of the same frame is a hold -- the only way the format has of making
	// one frame last longer. Joining the repeats across the gap says "this is one
	// pose held" rather than "somebody added the same frame twice by accident".
	//
	// Compared by name while the image names its cells, because two DIFFERENT
	// missing frames are both slot -1, and joining those would draw a hold of a
	// pose that does not exist out of two unrelated broken frames.
	const bool namedMode = isNamedMode();

	for (S32 i = 1; i < mSlots.size(); ++i)
	{
		const bool sameFrame = namedMode
			? (getNameAt(i) == getNameAt(i - 1))
			: (mSlots[i] == mSlots[i - 1]);

		if (!sameFrame)
		{
			continue;
		}

		const RectI previous = getCellRect(i - 1, columns, mCellSize, mCellPad);
		const RectI current = getCellRect(i, columns, mCellSize, mCellPad);

		const S32 left = contentRect.point.x + previous.point.x + previous.extent.x;
		const S32 right = contentRect.point.x + current.point.x;
		if (right <= left)
		{
			continue;
		}

		RectI join(left, contentRect.point.y + previous.point.y + (previous.extent.y / 3),
		           right - left, getMax(1, previous.extent.y / 3));
		dglDrawRectFill(join, mProfile->getFillColor(SelectedState));
	}

	if (mCaret < 0)
	{
		return;
	}

	RectI caret = getCaretRect(mCaret, mSlots.size(), mCellSize, mCellPad, mCellSize);
	caret.point += contentRect.point;
	dglDrawRectFill(caret, mProfile->getFontColor(HighlightState));
}

//-----------------------------------------------------------------------------

void GuiEditFrameTimelineCtrl::onTouchDown(const GuiEvent& event)
{
	// Keys only arrive at the first responder, and Delete is how a slot is
	// removed, so the click that picks a slot has to claim focus as well. The
	// profile must allow it -- listBoxProfile and treeViewProfile are the theme's
	// two with canKeyFocus set.
	setFirstResponder();

	const S32 cell = cellAtGlobal(event.mousePoint);
	if (cell == -1)
	{
		Parent::onTouchDown(event);
		return;
	}

	mPressed = true;
	mDragging = false;
	mDragOutside = false;
	mDragFrom = cell;
	mPressAt = event.mousePoint;

	setSelectedSlot(cell);

	if (isMethod("onSlotSelected"))
	{
		Con::executef(this, 3, "onSlotSelected", Con::getIntArg(cell), Con::getIntArg(getFrameAt(cell)));
	}

	// Held for the whole gesture. This is also what makes "dragged off the
	// timeline" observable at all: without the capture, dragging out of the
	// control simply stops delivering events here.
	mouseLock();
}

void GuiEditFrameTimelineCtrl::onTouchDragged(const GuiEvent& event)
{
	if (!mPressed)
	{
		return;
	}

	if (!mDragging)
	{
		const Point2I travelled = event.mousePoint - mPressAt;
		if (mAbs(travelled.x) < smDragSlop && mAbs(travelled.y) < smDragSlop)
		{
			return;
		}

		mDragging = true;
	}

	// Policed against this control's own bounds, in the same global space the
	// event arrives in. Inside, the caret shows where the slot would land;
	// outside, releasing throws it away.
	RectI globalBounds(localToGlobalCoord(Point2I(0, 0)), mBounds.extent);
	const bool outside = !globalBounds.pointInRect(event.mousePoint);

	if (outside != mDragOutside)
	{
		mDragOutside = outside;
		setUpdate();
	}

	if (outside)
	{
		clearCaret();
		return;
	}

	showCaretAt(event.mousePoint);
}

void GuiEditFrameTimelineCtrl::onTouchUp(const GuiEvent& event)
{
	if (!mPressed)
	{
		Parent::onTouchUp(event);
		return;
	}

	const bool wasDragging = mDragging;
	const bool wasOutside = mDragOutside;
	const S32 from = mDragFrom;
	const S32 caret = mCaret;

	mPressed = false;
	mDragging = false;
	mDragOutside = false;
	mDragFrom = -1;
	clearCaret();
	mouseUnlock();

	// A press that never moved is a selection, and that already happened on the
	// way down.
	if (!wasDragging)
	{
		return;
	}

	const bool changed = wasOutside ? removeSlot(from) : moveSlot(from, caret);

	if (changed)
	{
		notifyFramesChanged();
	}
}

bool GuiEditFrameTimelineCtrl::onKeyDown(const GuiEvent& event)
{
	switch (event.keyCode)
	{
		case KEY_DELETE:
		case KEY_BACKSPACE:
		{
			if (mSelected < 0)
			{
				break;
			}

			if (removeSlot(mSelected))
			{
				notifyFramesChanged();
			}
			return true;
		}

		case KEY_LEFT:
		{
			if (mSlots.size() == 0)
			{
				break;
			}
			selectSlotAndNotify(getMax(0, mSelected - 1));
			return true;
		}

		case KEY_RIGHT:
		{
			if (mSlots.size() == 0)
			{
				break;
			}
			selectSlotAndNotify(getMin(mSlots.size() - 1, mSelected + 1));
			return true;
		}

		case KEY_HOME:
		{
			if (mSlots.size() == 0)
			{
				break;
			}
			selectSlotAndNotify(0);
			return true;
		}

		case KEY_END:
		{
			if (mSlots.size() == 0)
			{
				break;
			}
			selectSlotAndNotify(mSlots.size() - 1);
			return true;
		}

		default:
			break;
	}

	return Parent::onKeyDown(event);
}
