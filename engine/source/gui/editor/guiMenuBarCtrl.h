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

#ifndef _GUIMENUBARCTRL_H_
#define _GUIMENUBARCTRL_H_

#ifndef _SIMBASE_H_
#include "sim/simBase.h"
#endif
#ifndef _GUITYPES_H_
#include "gui/guiTypes.h"
#endif
#ifndef _GUICONTROL_H_
#include "gui/guiControl.h"
#endif
#ifndef _GUISCROLLCTRL_H_
#include "gui/containers/guiScrollCtrl.h"
#endif

class GuiMenuItemCtrl;
class GuiMenuBGCtrl;
class GuiMenuListCtrl;

class GuiMenuBarCtrl : public GuiControl
{
private:
   typedef GuiControl Parent;

public:
   //creation methods
   DECLARE_CONOBJECT(GuiMenuBarCtrl);
   GuiMenuBarCtrl();
   static void initPersistFields();

   virtual void childResized(GuiControl *child);
   virtual void resize(const Point2I &newPosition, const Point2I &newExtent);
   virtual void inspectPostApply();
   virtual void onChildAdded(GuiControl *child);
   virtual void onChildRemoved(SimObject *child);
   virtual void childrenReordered();
   /// Re-runs calculateMenus, because a top-level menu is as wide as its text
   /// and so anything that changes the text changes the layout.
   virtual void setUpdate();
   virtual void calculateMenus();
   /// GuiControl's signature, so this overrides rather than hides it. The bar
   /// answers for its own items; nothing outside may descend into them.
   virtual GuiControl* findHitControl(const Point2I &pt, S32 initialLayer = -1, const bool ignoreUseInput = false, const bool ignoreEditSelected = true);
   virtual GuiMenuItemCtrl* findHitMenu(const Point2I &pt);
   virtual void onRender(Point2I offset, const RectI &updateRect);

   /// @name Authoring
   ///
   /// A GuiMenuItemCtrl is not something the control palette offers - it means
   /// nothing outside a bar - so the bar makes its own, from a "+" it draws after
   /// the last menu while the Gui is being authored.
   /// @{

   /// The editor-only "+", in the same coordinates as an item's mBounds: local to
   /// the bar's CONTENT, which is the space calculateMenus lays items out in.
   /// Empty whenever the bar is not being authored, which is how everything
   /// tests for it.
   RectI mAddItemRect;

   /// The menu whose dropdown is showing while authoring, and the rectangles of
   /// what is in it - one row per command, then the "+" row, inside a box. All
   /// in the same content-local space as mAddItemRect.
   ///
   /// Drawn by the bar rather than opened for real: the runtime dropdown is a
   /// full-canvas dialog pushed at layer 99, and a dialog at that layer takes
   /// every click, so the Gui Editor would never see one. Drawing it here costs
   /// nothing at runtime and leaves the editor holding the mouse.
   GuiMenuItemCtrl *mEditOpenMenu;
   RectI mEditBoxRect;
   Vector<RectI> mEditRowRects;
   RectI mEditAddRowRect;

   /// The menu the selection is in - that item, or anything inside it. Derived
   /// rather than toggled, so the dropdown follows the Explorer tree as readily
   /// as the canvas and cannot disagree with what is selected.
   GuiMenuItemCtrl* findSelectedMenu();
   /// Re-derive which menu is open and re-lay its rows. Called every frame from
   /// onPreRender, and on demand by the geometry accessors - the selection can
   /// change without anything drawing.
   void refreshEditMenu();
   void layoutEditMenu();
   /// A separator is a rule between two groups, so it gets the profile's chrome
   /// and none of the height a line of text would need.
   S32 editRowHeight(GuiMenuItemCtrl* item, S32 rowHeight, S32 spacerHeight);
   void renderEditMenu(const Point2I &contentOffset);

   /// The dropdown's "+" row in global coordinates, or an empty rect.
   RectI getAddSubItemGlobalRect();

   virtual void onPreRender();

   /// Control-local to content-local. findHitMenu reaches the same space through
   /// a child's mRenderInsetLT, which only says anything once the bar has drawn
   /// and only if there is a child to ask.
   Point2I getMenuLocalCoord(const Point2I &src);

   /// The "+" in global coordinates, or an empty rect when there is none.
   RectI getAddItemGlobalRect();

   /// The menu under a content-local point, drawn or not, active or not - which
   /// is what authoring needs and findHitMenu deliberately does not give.
   GuiMenuItemCtrl* findMenuAt(const Point2I &menuLocalPt);

   /// Draw the "+". Ghosted rather than drawn as a menu, because it is an
   /// affordance and not an item.
   void renderAddItem(RectI itemRect);

   /// Ask the Gui Editor for an item. The bar draws the affordance; it does not
   /// make the item, because an item made while authoring has to be themed,
   /// recorded for undo and announced to the Explorer tree.
   /// @param parent The menu to put it in, or NULL for a top-level one.
   void requestNewMenuItem(GuiMenuItemCtrl *parent);

   virtual bool onMouseDownEditor(const GuiEvent &event, const Point2I& offset);

   /// The bar's own bounds, plus the dropdown it draws below them while
   /// authoring. Hit testing walks bounds, and the dropdown hangs outside the
   /// bar's - so without this the click never reaches the bar at all and the
   /// "+" row is unreachable.
   virtual bool pointInControl(const Point2I& parentCoordPoint);

   /// @}

   virtual void processHover(const GuiEvent &event);
   virtual void setHoverTarget(GuiMenuItemCtrl *ctrl);
   virtual void onTouchMove(const GuiEvent &event);
   virtual void onTouchEnter(const GuiEvent &event);
   virtual void onTouchLeave(const GuiEvent &event);
   virtual void onTouchDown(const GuiEvent &event);

   void openMenu();
   void closeMenu();
   void ApplyMenuSettings();
   void setMenuActive(const char* name, bool isActive);

   bool mUseKeyMode;
   virtual void acceleratorKeyPress(U32 index);
   virtual bool onKeyDown(const GuiEvent &event);

   GuiControlProfile *mMenuProfile;
   GuiControlProfile *mMenuContentProfile;
   GuiControlProfile *mMenuItemProfile;
   GuiControlProfile *mBackgroundProfile;
   GuiControlProfile *mThumbProfile;
   GuiControlProfile *mArrowProfile;
   GuiControlProfile *mTrackProfile;

   bool onWake();
   void onSleep();
   void setControlBackgroundProfile(GuiControlProfile* prof);
   void setControlMenuProfile(GuiControlProfile* prof);
   void setControlMenuItemProfile(GuiControlProfile* prof);
   void setControlMenuContentProfile(GuiControlProfile* prof);
   void setControlThumbProfile(GuiControlProfile* prof);
   void setControlArrowProfile(GuiControlProfile* prof);
   void setControlTrackProfile(GuiControlProfile* prof);

   S32 mScrollBarThickness;
   bool mShowArrowButtons;
   bool mUseConstantHeightThumb;

   GuiMenuItemCtrl *mHoverTarget;
   GuiMenuItemCtrl *mOpenMenu;
   GuiMenuBGCtrl *mBackground;
};

class GuiMenuItemCtrl : public GuiControl
{
private:
	typedef GuiControl Parent;

public:
	//creation methods
	DECLARE_CONOBJECT(GuiMenuItemCtrl);
	GuiMenuItemCtrl();
	static void initPersistFields();

	enum DisplayType
	{
		TextCommand = 0,
		Toggle,
		Radio,
		Spacer,
		Menu
	};

	DisplayType			mDisplayType;
	bool				mIsOpen;
	bool				mIsHover;
	bool				mIsOn;
	bool				mToggle;
	bool				mRadio;
	bool				mHasGoodChildren;

	static bool setToggle(void *obj, const char *data) 
	{ 
		GuiMenuItemCtrl* pCastObject = static_cast<GuiMenuItemCtrl *>(obj);
		pCastObject->mToggle = dAtob(data);
		pCastObject->mDisplayType = pCastObject->mToggle ? Toggle : TextCommand; 
		return false; 
	}
	static bool writeToggle(void* obj, StringTableEntry pFieldName) { return static_cast<GuiMenuItemCtrl*>(obj)->mToggle; }

	static bool setRadio(void *obj, const char *data)
	{
		GuiMenuItemCtrl* pCastObject = static_cast<GuiMenuItemCtrl *>(obj);
		pCastObject->mRadio = dAtob(data);
		pCastObject->mDisplayType = pCastObject->mRadio ? Radio : TextCommand;
		return false;
	}
	static bool writeRadio(void* obj, StringTableEntry pFieldName) { return static_cast<GuiMenuItemCtrl*>(obj)->mRadio; }
	static bool writeIsOn(void* obj, StringTableEntry pFieldName) { GuiMenuItemCtrl* pCastObject = static_cast<GuiMenuItemCtrl *>(obj); return pCastObject->mToggle || pCastObject->mRadio; }

	GuiMenuBarCtrl *mMenuBar;
	GuiScrollCtrl *mScroll;
	GuiMenuListCtrl *mList;

	GuiMenuItemCtrl *mPrevItem;
	GuiMenuItemCtrl *mNextItem;
	GuiMenuItemCtrl *mOpenSubMenu;

	virtual void onAction();
	virtual void inspectPostApply();
	virtual void onChildAdded(GuiControl *child);
	virtual void onChildRemoved(SimObject *child);

	/// A menu item only means anything inside a bar or inside another item, so it
	/// refuses every other parent. See GuiControl::canBeChildOf.
	bool canBeChildOf(GuiControl* parent);

	/// Its bar decides where it sits and how wide it is, from the text. See
	/// GuiControl::isGeometryEditable.
	bool isGeometryEditable() { return false; };

	/// Tell whoever is laying this out that its text - and so its width -
	/// changed. The properties pane writes the text on every keystroke, so this
	/// is what makes the bar reflow as you type.
	virtual void setText(const char *txt = NULL);
	void checkForGoodChildren();
	virtual void closeMenu();
	void ApplyMenuSettings();
	virtual void onRender(Point2I offset, const RectI &updateRect);
	virtual const char* getHotKeyText();
	virtual void toggleControl();
	virtual void turnOffPrevRadio();
	virtual void turnOffNextRadio();
	virtual bool onKeyDown(const GuiEvent &event);
	void setMenuActive(StringTableEntry name, bool isActive);
};

class GuiMenuBGCtrl : public GuiControl
{
protected:
	typedef GuiControl Parent;
	GuiMenuBarCtrl *mMenuBarCtrl;
public:
	GuiMenuBGCtrl(GuiMenuBarCtrl *ctrl);
	void openMenu();
	void closeMenu();
	void openSubMenu(GuiMenuItemCtrl *subMenu);
	virtual void onTouchMove(const GuiEvent &event);
	virtual void onTouchDragged(const GuiEvent &event);
	virtual void processHover(const GuiEvent &event);
	virtual void onTouchDown(const GuiEvent &event);
private:
	void positionMenu(const GuiMenuItemCtrl *menu, const Point2I &topLeft);
};

class GuiMenuListCtrl : public GuiControl
{
protected:
	typedef GuiControl Parent;
	GuiMenuItemCtrl *mMenu;
public:
	GuiMenuListCtrl(GuiMenuItemCtrl *ctrl);
	virtual void onTouchMove(const GuiEvent &event);
	GuiMenuItemCtrl* GetHitItem(const Point2I &pt);
	virtual void onPreRender();
	void onRender(Point2I offset, const RectI &updateRect);
	bool onRenderItem(RectI &itemRect, GuiMenuItemCtrl *item);
	void updateSize();

	Point2I mItemSize;
	Point2I	mSpacerSize;
	S32 mItemCount;
	S32 mSpacerCount;
	U32 mEnterItemTime;
	U32 mSubMenuStallTime;
	GuiMenuItemCtrl *mHoveredItem;
	bool mArmSubMenu;

	inline void setHoveredItem(GuiMenuItemCtrl *item) { mEnterItemTime = Platform::getVirtualMilliseconds(); mHoveredItem = item; mArmSubMenu = true; }

	virtual void onTouchDown(const GuiEvent& event);
	virtual void onTouchUp(const GuiEvent& event);
	void processMenuItem(GuiMenuItemCtrl *ctrl);
};

#endif
