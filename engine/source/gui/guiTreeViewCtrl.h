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

#ifndef _GUI_TREEVIEWCTRL_H
#define _GUI_TREEVIEWCTRL_H

#include "gui/guiListBoxCtrl.h"
#include <vector>

//------------------------------------------------------------------------------

class GuiTreeViewCtrl : public GuiListBoxCtrl
{
private:
	typedef GuiListBoxCtrl Parent;

	enum class ReorderMethod { Above, Below, Insert };
	S32 mFocusLevel;

protected:
	SimObjectPtr<SimObject> mRootObject;
	S32 mIndentSize;
	/// The sheet a row's icon is a frame of, and how big to draw it. Empty by
	/// default: with no sheet a row asks script for nothing, draws nothing and
	/// costs nothing, so a tree that wants no icons is exactly as it was.
	StringTableEntry mIconImageAssetID;
	AssetPtr<ImageAsset> mIconImageAsset;
	S32 mIconSize;
	Point2I mTouchPoint;
	bool mDragActive;
	S32 mDragIndex;
	bool mIsDragLegal;
	ReorderMethod mReorderMethod;
	bool mIsBoundToGuiEditor;
	bool mAllowReorder;
	const GuiControl* mFocusControl;

public:
	GuiTreeViewCtrl();
	virtual ~GuiTreeViewCtrl();
	static void initPersistFields();

	class TreeItem : public GuiListBoxCtrl::LBItem
	{
	public:
		TreeItem() : isOpen(1), level(0), triangleArea(RectI()), isVisible(1), branchList(vector<TreeItem*>()), trunk(nullptr), iconFrame(-1) { }
		virtual ~TreeItem() { }

		bool				isOpen;
		U16					level;
		RectI               triangleArea;
		bool				isVisible;
		vector<TreeItem*>	branchList;
		TreeItem*			trunk;
		/// Which frame of the tree's icon sheet this row draws, or -1 for none.
		/// Asked of script once when the row is built rather than every frame -
		/// onRenderItem runs for every visible row of every frame, so a callback
		/// here would be a console call per row per frame.
		S32					iconFrame;
	};

	/// The per-level indent step. Zero means "one row height", which is what the
	/// tree has always done and so is the default; a positive value is used as
	/// given. Static, and taking both numbers, so it can be tested away from a
	/// canvas, a Sim and a GL context - the same reason GuiScrollCtrl's bar
	/// arithmetic is two statics.
	static S32 resolveIndent(S32 indentSize, S32 rowInnerHeight);

	/// Where an icon of iconSize draws inside contentRect, and what it costs in
	/// width. Never enlarges: a row shorter than the art shrinks the art rather
	/// than promising a slot it cannot hold. Answers false - consuming nothing -
	/// when there is no room at all, so a cramped tree degrades to plain rows
	/// instead of to text drawn over an icon.
	static bool iconSlot(const RectI& contentRect, S32 iconSize, RectI& dstOut, S32& advanceOut);

	/// Where the focus line's 2px rule sits, measured from the left of the focus
	/// level's slot.
	///
	/// Centred on the TRIANGLE's square rather than on the indent step, because
	/// the line hangs down from a container's triangle and should line up with
	/// its point. The two were the same number until IndentSize became settable,
	/// which is exactly how they silently stopped agreeing.
	static S32 focusLineOffset(S32 rowInnerHeight);

	/// Width of the focus line itself.
	static constexpr S32 smFocusLineWidth = 2;

	/// Air between the icon and the text it labels. Four rather than two: the art
	/// on these sheets bleeds to the tile edge on purpose, so two pixels of gap
	/// reads as none and the picture runs into the first letter.
	///
	/// constexpr rather than const: a test asserting against it binds it to a
	/// const reference, which would odr-use a plain static const and fail to link
	/// for want of a definition.
	static constexpr S32 smIconGap = 4;

protected:
	// Reachable by a subclass: a render hook is handed a TreeItem and needs the
	// SimObject behind it. Neither is something a caller outside the hierarchy
	// should be doing, so neither is public.
	TreeItem* grabItemPtr(S32 index);
	// The tree always stores a SimObject* in LBItem::itemData; recover it
	// safely (item may be null) so callers can dynamic_cast to the real type.
	SimObject* getItemObject(TreeItem* item);

	/// Two seams in a row, so a subclass can add to one without copying the whole
	/// of onRenderItem - which could not be copied faithfully anyway, the focus
	/// line being driven by private state. Both are handed contentRect by
	/// reference and may carve space off its left; every step after them uses
	/// what is left.
	///
	/// renderItemGutter runs BEFORE the focus line and the depth indent, so what
	/// it draws stays pinned to the row's left edge instead of travelling with
	/// the tree. renderItemIcon runs between the triangle and the text.
	virtual void renderItemGutter(const RectI& itemRect, RectI& contentRect, TreeItem* treeItem, GuiControlState currentState) { }
	virtual void renderItemIcon(RectI& contentRect, TreeItem* treeItem, GuiControlState currentState);

	/// Which frame of the sheet a row should wear, asked of script once while the
	/// tree is being built. -1 - the answer when no sheet is set, no handler
	/// exists, or the handler declines - means no icon and no width spent.
	S32 getObjectIconFrame(SimObject* obj);

	void setIconImageAsset(const char* pImageAssetID);
	inline StringTableEntry getIconImageAsset(void) const { return mIconImageAssetID; }
	static bool setIconImage(void* obj, const char* data) { static_cast<GuiTreeViewCtrl*>(obj)->setIconImageAsset(data); return false; }
	static const char* getIconImage(void* obj, const char* data) { return static_cast<GuiTreeViewCtrl*>(obj)->getIconImageAsset(); }

private:
	// Commits a completed drag-reorder. Fully guarded: bails on any missing or
	// wrong-typed item rather than trusting itemData is a GuiControl/SimGroup.
	void reorderFromDrag();

	// The container a drop on the given row would land in: the row itself when
	// inserting into it, otherwise its parent branch. Both the hover, which draws
	// the drop indicator, and the drop itself resolve it through here, so the
	// indicator cannot promise a target the drop then refuses.
	SimGroup* resolveDropTarget(TreeItem* dragItem);
	// Whether every selected control would accept that container as a parent.
	// GuiControl::canBeChildOf is the question; a tab page is what says no.
	bool selectionAcceptsTarget(SimGroup* target);

	// Keyboard navigation has to work in visible-row space. Collapsed branches
	// stay in mItems with isVisible false and never render, so stepping raw
	// indices - which is what the list box base class does - walks the selection
	// into rows the user cannot see. These count the way onRender does.
	S32 getAdjacentVisibleIndex(S32 fromIndex, S32 direction);
	S32 getEdgeVisibleIndex(bool wantFirst);
	// Moves (rather than extends) the selection, so arrows behave the same on
	// multi-select trees as on single-select ones.
	void setSelectedIndex(S32 index);
	// Opens/closes a branch the way handleItemClick's triangle hit does, so the
	// keyboard and the mouse leave the tree in the same state.
	bool setItemExpanded(S32 index, bool isOpen);
	bool itemHasBranches(S32 index);

public:
	/// A tree's rows are not its own to save. Every TreeItem is generated from
	/// mRootObject - inspectObject builds the lot and rebuilds them whenever the
	/// object under them changes - so a set written into the .gui.taml would be
	/// stale the moment the tree next built itself, and would then be thrown away
	/// unread. The list box's static rows stop here.
	virtual bool writesItems() { return false; }

	// GuiControl
	//bool onWake();
	//void onSleep();
	//void onPreRender();
	bool onKeyDown(const GuiEvent& event);
	void onTouchDown(const GuiEvent& event);
	//void onMiddleMouseDown(const GuiEvent& event);
	//void onTouchMove(const GuiEvent& event);
	//void onTouchEnter(const GuiEvent& event);
	//void onTouchLeave(const GuiEvent& event);
	//void onRightMouseDown(const GuiEvent& event);
	void onTouchDragged(const GuiEvent& event);
	void onTouchUp(const GuiEvent& event);

	//bool onAdd();
	void onPreRender();
	void onRender(Point2I offset, const RectI& updateRect);
	//void setControlProfile(GuiControlProfile* prof);
	//void resize(const Point2I& newPosition, const Point2I& newExtent);
	virtual void onRenderItem(RectI& itemRect, LBItem* item);
	virtual void onRenderDragLine(RectI& itemRect);
	void updateSize();
	void ScrollToIndex(const S32 targetIndex);

	S32 getHitIndex(const GuiEvent& event);
	virtual void handleItemClick(LBItem* hitItem, S32 hitIndex, const GuiEvent& event);
	virtual void handleItemClick_ClickCallbacks(LBItem* hitItem, S32 hitIndex, const GuiEvent& event);

	void inspectObject(SimObject* obj);
	void uninspectObject();
	void addBranches(TreeItem* treeItem, SimObject* obj, U16 level);
	void refreshTree();
	StringTableEntry getObjectText(SimObject* obj);
	/// Re-asks the inspected object for one row's text and icon. A row's picture
	/// can change without the row moving - a bare GuiControl re-profiled from a
	/// panel to a label is still the same object in the same place - so the two
	/// have to be refreshable together.
	void refreshItem(S32 index);
	virtual GuiListBoxCtrl::LBItem* createItem();
	void setBranchesVisible(TreeItem* treeItem, bool isVisible);
	void setItemOpen(S32 index, bool isOpen);
	bool getItemOpen(S32 index);
	S32 getItemTrunk(S32 index);

	DECLARE_CONOBJECT(GuiTreeViewCtrl);
};

#endif
