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

#ifndef _GUI_EDITOR_EXPLORERTREE_H_
#define _GUI_EDITOR_EXPLORERTREE_H_

#ifndef _GUI_TREEVIEWCTRL_H
#include "gui/guiTreeViewCtrl.h"
#endif

//-----------------------------------------------------------------------------
// The Gui Editor's Explorer tree: a tree with two columns of editor state down
// its left edge.
//
// hidden and locked are not properties of the Gui being authored. Neither is
// ever written to a file -- SimObject::_writeHidden and _writeLocked both refuse
// -- because saving them would put a working state into the document. They lived
// in the properties pane beside Visible, Active and useInput, which are the real
// thing, and being next to them read as a promise that they were too.
//
// So they moved here, as the eye and the padlock of a layers panel. The columns
// do NOT indent with the tree: they are a fixed rail at the left, in row with
// each item, which is what makes a whole branch's state scannable at a glance.
//
// Editor-only, and not offered to anyone building a Gui: the palette refuses
// every class whose name begins "GuiEdit", in both copies of that rule
// (GuiEditorControlIcons::isPlaceableClass, generated, and
// GuiEditorControlSpec::isPlaceableClass, hand-typed for its drift guard).
//-----------------------------------------------------------------------------

class GuiEditorExplorerTree : public GuiTreeViewCtrl
{
private:
	typedef GuiTreeViewCtrl Parent;

public:
	enum GutterColumn
	{
		GutterNone = 0,
		GutterHidden,
		GutterLocked
	};

	// The box is the icon, drawn at its own size: 16px art in a 16px box is the
	// only scale that is sharp. Fixed rather than derived from the row height,
	// which moves with the theme's font size -- a rail that changes width when
	// someone bumps the font has stopped being a rail. The last pixel of a
	// column is its divider.
	//
	// constexpr rather than const: a test asserting against one binds it to a
	// const reference, which would odr-use a plain static const and fail to link.
	static constexpr S32 smBoxSize = 16;
	static constexpr S32 smBoxPad = 2;
	static constexpr S32 smColumnWidth = smBoxSize + (2 * smBoxPad) + 1;

	/// What the two columns cost a row, together.
	static S32 getGutterWidth();

	/// The two cells, given the row's left content edge and its vertical span.
	/// Deliberately a pure function of three numbers: the renderer and the hit
	/// test call it with the same numbers, so the two cannot drift apart. The
	/// eye is leftmost, as it is in every layers panel anyone has used.
	static void getGutterCells(S32 left, S32 top, S32 height, RectI& hiddenCell, RectI& lockedCell);

	/// Which column an x falls in, measured from the same left edge. The row was
	/// already settled by y, so this is the whole hit test.
	static GutterColumn columnAt(S32 x, S32 left);

	/// The square inside a cell that the icon draws in and the eye reads as a
	/// box. Centered in what the divider leaves.
	static RectI getBoxRect(const RectI& cell);

	GuiEditorExplorerTree();
	static void initPersistFields();

	void onTouchDown(const GuiEvent& event);
	void onTouchDragged(const GuiEvent& event);
	void onTouchUp(const GuiEvent& event);
	bool renderTooltip(Point2I& cursorPos, const char* tipText = NULL);

	/// The row a global point lands on, as a raw mItems index, or -1.
	///
	/// getHitIndex would answer this, but it also recomputes the drag state as a
	/// side effect -- mDragIndex, mReorderMethod, mIsDragLegal -- and the tooltip
	/// asks this question every frame the pointer sits still. Asking must not arm
	/// anything.
	S32 visibleRowAt(const Point2I& globalPoint);

	/// The row's left content edge, in the control's own coordinates.
	///
	/// Measured with NORMAL-state insets in both the paint and the hit test, so a
	/// profile that pads a highlighted row differently cannot make the columns
	/// jitter under the pointer or make a click miss by the width of the change.
	S32 gutterInset();

	/// The center of one column's box on one row, in global coordinates. For
	/// tests, which must not hard-code a point: a stale coordinate reports a
	/// missing item, which is exactly what a broken hit test reports, and the
	/// test would be lying either way.
	Point2I getGutterPoint(S32 index, GutterColumn column);

protected:
	void renderItemGutter(const RectI& itemRect, RectI& contentRect, TreeItem* treeItem, GuiControlState currentState);
	/// One cell: its divider always, then its box if the row has one, then the
	/// icon if the flag says so. The root row draws dividers and no box, so the
	/// rail runs the whole height of the tree.
	void renderGutterCell(const RectI& cell, const Point2I& cursorPt, bool showBox, bool showIcon, S32 frame);

	/// Which column a press landed in, and on what. GutterNone for the root row,
	/// for an inactive row, and for anything with no object behind it.
	GutterColumn hitGutter(const Point2I& globalPoint, SimObject** objOut);

	/// The tip for whatever the cursor is over, or NULL to leave the control's
	/// own tooltip alone.
	const char* tipForPoint(const Point2I& globalPoint);

	StringTableEntry mStateImageAssetID;
	AssetPtr<ImageAsset> mStateImageAsset;
	S32 mEyeFrame;
	S32 mLockFrame;

	// Four strings rather than a format, and named for the state each describes
	// rather than for an on and an off. The eye reads inverted -- it is present
	// when the control is NOT hidden -- so "the tip for the on state" would have
	// to be read twice by everyone who ever touched it.
	//
	// The text itself is script's: the "Locked - On" heading style belongs with
	// the other toggles that use it, and nothing in the engine should be
	// inventing user-facing prose.
	StringTableEntry mShownTip;
	StringTableEntry mHiddenTip;
	StringTableEntry mLockedTip;
	StringTableEntry mUnlockedTip;

	/// Latched by a press that landed in a column, so the drag and the release
	/// that follow it know to do nothing.
	bool mGutterPress;

	void setStateImageAsset(const char* pImageAssetID);
	inline StringTableEntry getStateImageAsset(void) const { return mStateImageAssetID; }
	static bool setStateImage(void* obj, const char* data) { static_cast<GuiEditorExplorerTree*>(obj)->setStateImageAsset(data); return false; }
	static const char* getStateImage(void* obj, const char* data) { return static_cast<GuiEditorExplorerTree*>(obj)->getStateImageAsset(); }

public:
	DECLARE_CONOBJECT(GuiEditorExplorerTree);
};

#endif //_GUI_EDITOR_EXPLORERTREE_H_
