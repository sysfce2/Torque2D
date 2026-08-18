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

#include "gui/editor/guiEditorExplorerTree.h"
#include "graphics/dgl.h"
#include "gui/guiDefaultControlRender.h"
#include "gui/guiCanvas.h"
#include "gui/editor/guiEditCtrl.h"

#include "guiEditorExplorerTree_ScriptBinding.h"

IMPLEMENT_CONOBJECT(GuiEditorExplorerTree);

GuiEditorExplorerTree::GuiEditorExplorerTree()
{
	mStateImageAssetID = StringTable->EmptyString;
	mStateImageAsset = NULL;
	// -1 is "no art", so a tree given no sheet draws empty boxes rather than
	// whatever frame 0 happens to be.
	mEyeFrame = -1;
	mLockFrame = -1;
	mShownTip = StringTable->EmptyString;
	mHiddenTip = StringTable->EmptyString;
	mLockedTip = StringTable->EmptyString;
	mUnlockedTip = StringTable->EmptyString;
	mGutterPress = false;
}

void GuiEditorExplorerTree::initPersistFields()
{
	Parent::initPersistFields();

	// The frame numbers come from script, never from here: EditorIcons.cs is
	// generated and alphabetical, so an index baked into the engine would
	// silently become a different picture the next time the sheet is rebuilt.
	addProtectedField("StateIcons", TypeAssetId, Offset(mStateImageAssetID, GuiEditorExplorerTree), &setStateImage, &getStateImage, "The image asset the eye and padlock are frames of.");
	addField("EyeFrame", TypeS32, Offset(mEyeFrame, GuiEditorExplorerTree), "Frame of StateIcons drawn when a control is not hidden.");
	addField("LockFrame", TypeS32, Offset(mLockFrame, GuiEditorExplorerTree), "Frame of StateIcons drawn when a control is locked.");

	addField("ShownTip", TypeString, Offset(mShownTip, GuiEditorExplorerTree), "Tooltip for the eye column when the control is not hidden.");
	addField("HiddenTip", TypeString, Offset(mHiddenTip, GuiEditorExplorerTree), "Tooltip for the eye column when the control is hidden.");
	addField("LockedTip", TypeString, Offset(mLockedTip, GuiEditorExplorerTree), "Tooltip for the padlock column when the control is locked.");
	addField("UnlockedTip", TypeString, Offset(mUnlockedTip, GuiEditorExplorerTree), "Tooltip for the padlock column when the control is not locked.");
}

void GuiEditorExplorerTree::setStateImageAsset(const char* pImageAssetID)
{
	// Sanity!
	AssertFatal(pImageAssetID != NULL, "Cannot use a NULL asset ID.");

	mStateImageAssetID = StringTable->insert(pImageAssetID);

	if (mStateImageAssetID != StringTable->EmptyString)
	{
		mStateImageAsset = pImageAssetID;
	}
	else
	{
		mStateImageAsset.clear();
	}
}

//-----------------------------------------------------------------------------
// Geometry. All of it static and taking everything it uses, so it can be tested
// away from a canvas, a Sim and a GL context -- adding a row to a tree loads a
// font, which registers a texture, which is the one thing a unit test here
// cannot do.
//-----------------------------------------------------------------------------

S32 GuiEditorExplorerTree::getGutterWidth()
{
	return 2 * smColumnWidth;
}

void GuiEditorExplorerTree::getGutterCells(S32 left, S32 top, S32 height, RectI& hiddenCell, RectI& lockedCell)
{
	hiddenCell.set(Point2I(left, top), Point2I(smColumnWidth, height));
	lockedCell.set(Point2I(left + smColumnWidth, top), Point2I(smColumnWidth, height));
}

GuiEditorExplorerTree::GutterColumn GuiEditorExplorerTree::columnAt(S32 x, S32 left)
{
	const S32 offset = x - left;
	if (offset < 0)
	{
		return GutterNone;
	}
	if (offset < smColumnWidth)
	{
		return GutterHidden;
	}
	if (offset < (2 * smColumnWidth))
	{
		return GutterLocked;
	}
	return GutterNone;
}

RectI GuiEditorExplorerTree::getBoxRect(const RectI& cell)
{
	// Square, and never taller than the row: the art is square and stretching it
	// to a short row would be the one thing that makes 16px work look wrong.
	const S32 size = getMin(smBoxSize, cell.extent.y);
	// The divider owns the cell's last pixel, so center in what is left of it.
	const S32 usable = cell.extent.x - 1;
	return RectI(cell.point.x + ((usable - size) / 2),
				 cell.point.y + ((cell.extent.y - size) / 2),
				 size, size);
}

S32 GuiEditorExplorerTree::gutterInset()
{
	if (!mProfile)
	{
		return 0;
	}

	GuiBorderProfile* leftProfile = mProfile->getLeftBorder();
	if (!leftProfile)
	{
		return 0;
	}

	return leftProfile->getMargin(NormalState) + leftProfile->getBorder(NormalState) + leftProfile->getPadding(NormalState);
}

//-----------------------------------------------------------------------------
// Drawing
//-----------------------------------------------------------------------------

void GuiEditorExplorerTree::renderItemGutter(const RectI& itemRect, RectI& contentRect, TreeItem* treeItem, GuiControlState currentState)
{
	if (!mProfile || !treeItem)
	{
		return;
	}

	const S32 left = itemRect.point.x + gutterInset();
	const S32 want = (left + getGutterWidth()) - contentRect.point.x;
	if (want <= 0 || contentRect.extent.x <= want)
	{
		// No room for the rail and something after it. Draw nothing and carve
		// nothing: a cramped tree falls back to plain rows, which is legible,
		// rather than to text written over the columns, which is not.
		return;
	}

	// Reserve before anything below can return early, so a row that draws no
	// boxes still keeps its text off the strip and lined up with every other row.
	contentRect.point.x += want;
	contentRect.extent.x -= want;

	RectI hiddenCell, lockedCell;
	getGutterCells(left, itemRect.point.y, itemRect.extent.y, hiddenCell, lockedCell);

	// Hover is polled, not evented: there is no per-row enter or leave to hook,
	// and the row above has already read the cursor once to pick its own state.
	Point2I cursorPt = Point2I(0, 0);
	GuiCanvas* root = getRoot();
	if (root)
	{
		cursorPt = root->getCursorPos();
	}

	// The root row is the simulated canvas: stage furniture, not a control in the
	// document. Hiding it would blank the canvas and locking it would do nothing,
	// so it gets no boxes -- but it still draws its dividers, or the rail would
	// start one row down and read as a column that had failed to paint.
	SimObject* obj = getItemObject(treeItem);
	const bool hasBoxes = (obj != NULL && treeItem->trunk != NULL);

	// The rail is drawn in ONE ink, on every row, whatever state the row is in.
	//
	// Inheriting the row's font colour is what the icons did at first, and it read
	// correctly nowhere: the box behind them does not change with selection, so a
	// selected row put selected-TEXT ink on an unselected background and the eye
	// all but vanished. Checked against all four themes. The boxes and dividers
	// are already state-independent -- the icons have to match them, not the text.
	dglSetBitmapModulation(getFontColor(mProfile, NormalState));

	// Photoshop's order and Photoshop's reading: the eye is shown when the
	// control is NOT hidden, the padlock when it IS locked. So a tidy Gui is a
	// column of eyes and a column of nothing.
	renderGutterCell(hiddenCell, cursorPt, hasBoxes, hasBoxes && !obj->isHidden(), mEyeFrame);
	renderGutterCell(lockedCell, cursorPt, hasBoxes, hasBoxes && obj->isLocked(), mLockFrame);

	// Put back what the row was drawing with. onRenderItem sets the modulation
	// once, before the gutter, and everything after this -- the triangle, the
	// class icon, renderText -- is still relying on it.
	dglSetBitmapModulation(getFontColor(mProfile, currentState));
}

void GuiEditorExplorerTree::renderGutterCell(const RectI& cell, const Point2I& cursorPt, bool showBox, bool showIcon, S32 frame)
{
	const ColorI& quiet = mProfile->getFillColor(HighlightState);

	// One pixel down the cell's right, over the row's WHOLE height rather than
	// its content's, so the two columns read as continuous rules down the tree
	// even under a profile that puts air between rows.
	RectI divider(cell.point.x + cell.extent.x - 1, cell.point.y, 1, cell.extent.y);
	dglDrawRectFill(divider, quiet);

	RectI box = getBoxRect(cell);
	if (!showBox || !box.isValidRect())
	{
		return;
	}

	// The hover fill is deliberately barely there against the row fill. The cell
	// under the pointer steps up to the selected fill, which is the only thing
	// saying the rail can be clicked at all -- on an unlocked, visible control
	// both boxes are empty and there is otherwise nothing to find.
	dglDrawRectFill(box, cell.pointInRect(cursorPt) ? mProfile->getFillColor(SelectedState) : quiet);

	// The caller has set the modulation to the normal-state font colour and will
	// put the row's own back afterwards, so the icon is the same ink on every row.
	if (showIcon && frame >= 0 && !mStateImageAsset.isNull())
	{
		renderImageAssetFrame(box, mStateImageAsset, (U32)frame);
	}
}

//-----------------------------------------------------------------------------
// Hit testing
//-----------------------------------------------------------------------------

S32 GuiEditorExplorerTree::visibleRowAt(const Point2I& globalPoint)
{
	const Point2I local = globalToLocalCoord(globalPoint);
	if (local.y < 0 || mItemSize.y <= 0)
	{
		return -1;
	}

	const S32 slot = (S32)mFloor((F32)local.y / (F32)mItemSize.y);
	for (S32 i = 0, j = 0; i < mItems.size(); i++)
	{
		TreeItem* treeItem = dynamic_cast<TreeItem*>(mItems[i]);
		if (treeItem && treeItem->isVisible)
		{
			if (j == slot)
			{
				return i;
			}
			j++;
		}
	}
	return -1;
}

GuiEditorExplorerTree::GutterColumn GuiEditorExplorerTree::hitGutter(const Point2I& globalPoint, SimObject** objOut)
{
	const GutterColumn column = columnAt(globalToLocalCoord(globalPoint).x, gutterInset());
	if (column == GutterNone)
	{
		return GutterNone;
	}

	const S32 index = visibleRowAt(globalPoint);
	if (index < 0)
	{
		return GutterNone;
	}

	TreeItem* treeItem = dynamic_cast<TreeItem*>(mItems[index]);
	// The root keeps its columns' width but has no boxes, so it has none to hit.
	if (!treeItem || !treeItem->isActive || treeItem->trunk == NULL)
	{
		return GutterNone;
	}

	SimObject* obj = getItemObject(treeItem);
	if (!obj)
	{
		return GutterNone;
	}

	if (objOut)
	{
		*objOut = obj;
	}
	return column;
}

Point2I GuiEditorExplorerTree::getGutterPoint(S32 index, GutterColumn column)
{
	// Visible-row space, which is what the rows are drawn and hit tested in.
	S32 visible = 0;
	for (S32 i = 0; i < mItems.size() && i < index; i++)
	{
		TreeItem* treeItem = dynamic_cast<TreeItem*>(mItems[i]);
		if (treeItem && treeItem->isVisible)
		{
			visible++;
		}
	}

	RectI hiddenCell, lockedCell;
	getGutterCells(gutterInset(), visible * mItemSize.y, mItemSize.y, hiddenCell, lockedCell);

	const RectI& cell = (column == GutterLocked) ? lockedCell : hiddenCell;
	const RectI box = getBoxRect(cell);
	Point2I local(box.point.x + (box.extent.x / 2), box.point.y + (box.extent.y / 2));
	return localToGlobalCoord(local);
}

void GuiEditorExplorerTree::onTouchDown(const GuiEvent& event)
{
	mGutterPress = false;

	// Above everything the base does, including its row-0 case -- which never
	// calls Parent::onTouchDown at all, so the root row would otherwise be the
	// one row whose columns did nothing.
	//
	// Returning before Parent::onTouchDown is what buys every requirement at
	// once. GuiListBoxCtrl::onTouchDown never runs, so there is no
	// setFirstResponder and no handleItemClick; no handleItemClick means no
	// selection change and no onClick or onDoubleClick; and mLastClickItem is
	// left alone, so a press in a column followed by a press on the row is not
	// read as a double click.
	SimObject* obj = NULL;
	const GutterColumn column = hitGutter(event.mousePoint, &obj);
	if (column != GutterNone && obj)
	{
		// Written straight, with no undo record, and that is deliberate. Neither
		// flag is ever saved -- SimObject::_writeHidden and _writeLocked both
		// refuse, see the comment there -- so an undo step would restore
		// something that can never reach a file, and would leave Ctrl+Z after
		// "hide three things, then move a button" un-hiding something instead of
		// putting the button back.
		//
		// It acts on the row that was clicked and not on the selection, which is
		// what a layers panel does and what someone reaching for one row's eye
		// means. If this ever should be undoable, it is this branch that changes
		// and nothing else.
		if (column == GutterHidden)
		{
			obj->setHidden(!obj->isHidden());

			// A hidden control is not a target on the canvas any more, and that
			// includes being the container the next control is placed into.
			if (GuiControl::smEditorHandle != NULL)
			{
				GuiControl::smEditorHandle->controlHidden(dynamic_cast<GuiControl*>(obj));
			}
		}
		else
		{
			obj->setLocked(!obj->isLocked());
		}

		mGutterPress = true;
		setUpdate();
		return;
	}

	Parent::onTouchDown(event);
}

void GuiEditorExplorerTree::onTouchDragged(const GuiEvent& event)
{
	// A press that landed in a column never becomes a reorder. Returning here
	// also skips getHitIndex, which matters: its side effects are the drag state
	// the drop indicator and reorderFromDrag read, and letting them update during
	// a gutter press would leave the tree half-primed to move something.
	if (mGutterPress)
	{
		return;
	}

	Parent::onTouchDragged(event);
}

void GuiEditorExplorerTree::onTouchUp(const GuiEvent& event)
{
	if (mGutterPress)
	{
		mGutterPress = false;
		mDragActive = false;
		// Swallowed rather than chained: GuiControl::onTouchUp hands an event the
		// script did not consume to the parent, and the toggle already happened.
		return;
	}

	Parent::onTouchUp(event);
}

//-----------------------------------------------------------------------------
// Tooltips
//-----------------------------------------------------------------------------

const char* GuiEditorExplorerTree::tipForPoint(const Point2I& globalPoint)
{
	SimObject* obj = NULL;
	const GutterColumn column = hitGutter(globalPoint, &obj);
	if (column == GutterNone || !obj)
	{
		return NULL;
	}

	StringTableEntry tip = (column == GutterHidden)
		? (obj->isHidden() ? mHiddenTip : mShownTip)
		: (obj->isLocked() ? mLockedTip : mUnlockedTip);

	return (tip != StringTable->EmptyString) ? tip : NULL;
}

bool GuiEditorExplorerTree::renderTooltip(Point2I& cursorPos, const char* tipText)
{
	// The canvas re-calls this every frame once the pointer has settled, and it
	// decides whether to at all by comparing whole controls rather than points.
	// So one control can serve different text per region, and the text follows
	// the cursor from column to column without anything having to invalidate it.
	if (tipText == NULL)
	{
		const char* gutterTip = tipForPoint(cursorPos);
		if (gutterTip != NULL)
		{
			tipText = gutterTip;
		}
	}

	return Parent::renderTooltip(cursorPos, tipText);
}
