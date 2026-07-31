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

#include "console/console.h"
#include "console/consoleTypes.h"
#include "graphics/dgl.h"
#include "gui/guiDefaultControlRender.h"
#include "gui/guiCanvas.h"
#include "input/actionMap.h"

#include "gui/editor/guiMenuBarCtrl.h"

// For the editor-only "+": the colour it borrows and the handle it calls back on.
#include "gui/editor/guiEditCtrl.h"

#include "gui/editor/guiMenuBarCtrl_ScriptBinding.h"

#pragma region GuiMenuBarCtrl
IMPLEMENT_CONOBJECT(GuiMenuBarCtrl);

GuiMenuBarCtrl::GuiMenuBarCtrl()
{
	mBounds.point.set(0, 0);
	mBounds.extent.set(400, 26);
	mActive = true;
	mUseKeyMode = false;
	mAcceleratorKey = "alt";
	setField("profile", "GuiMenuBarProfile");

	mMenuProfile = NULL;
	setField("MenuProfile", "GuiMenuProfile");

	mMenuContentProfile = NULL;
	setField("MenuContentProfile", "GuiMenuContentProfile");

	mMenuItemProfile = NULL;
	setField("MenuItemProfile", "GuiMenuItemProfile");

	mThumbProfile = NULL;
	setField("ThumbProfile", "GuiScrollThumbProfile");

	mArrowProfile = NULL;
	setField("ArrowProfile", "GuiScrollArrowProfile");

	mTrackProfile = NULL;
	setField("TrackProfile", "GuiScrollTrackProfile");

	mHoverTarget = NULL;
	mOpenMenu = NULL;

	// Empty until a layout pass in edit mode fills it in, and read by
	// getAddItemGlobalRect - which script can ask about a bar that has never laid
	// itself out at all.
	mAddItemRect = RectI(0, 0, 0, 0);

	VECTOR_SET_ASSOCIATION(mEditRowRects);
	mEditOpenMenu = NULL;
	mEditBoxRect = RectI(0, 0, 0, 0);
	mEditAddRowRect = RectI(0, 0, 0, 0);

	mUseConstantHeightThumb = false;
	mScrollBarThickness = 12;
	mShowArrowButtons = false;

	mBackground = new GuiMenuBGCtrl(this);
	AssertFatal(mBackground, "GuiDropDownCtrl: Failed to initialize GuiMenuBGCtrl!");
	mBackgroundProfile = mBackground->mProfile;
	mBackgroundProfile->incRefCount();
}

void GuiMenuBarCtrl::initPersistFields()
{
	Parent::initPersistFields();

	addField("MenuProfile", TypeGuiProfile, Offset(mMenuProfile, GuiMenuBarCtrl));
	addField("MenuContentProfile", TypeGuiProfile, Offset(mMenuContentProfile, GuiMenuBarCtrl));
	addField("MenuItemProfile", TypeGuiProfile, Offset(mMenuItemProfile, GuiMenuBarCtrl));
	addField("backgroundProfile", TypeGuiProfile, Offset(mBackgroundProfile, GuiMenuBarCtrl));
	addField("constantThumbHeight", TypeBool, Offset(mUseConstantHeightThumb, GuiMenuBarCtrl));
	addField("scrollBarThickness", TypeS32, Offset(mScrollBarThickness, GuiMenuBarCtrl));
	addField("showArrowButtons", TypeBool, Offset(mShowArrowButtons, GuiMenuBarCtrl));
	addField("thumbProfile", TypeGuiProfile, Offset(mThumbProfile, GuiMenuBarCtrl));
	addField("trackProfile", TypeGuiProfile, Offset(mTrackProfile, GuiMenuBarCtrl));
	addField("arrowProfile", TypeGuiProfile, Offset(mArrowProfile, GuiMenuBarCtrl));
}

void GuiMenuBarCtrl::inspectPostApply()
{
	ApplyMenuSettings();
	calculateMenus();
	Parent::inspectPostApply();
}

void GuiMenuBarCtrl::childResized(GuiControl *child)
{
	calculateMenus();
}

void GuiMenuBarCtrl::resize(const Point2I &newPosition, const Point2I &newExtent)
{
	Parent::resize(Point2I(0, 0), newExtent);
}

void GuiMenuBarCtrl::onChildAdded(GuiControl *child)
{
	GuiMenuItemCtrl *menu = dynamic_cast<GuiMenuItemCtrl*>(child);
	if (!menu)
	{
		Con::warnf("GuiMenuBarCtrl::onChildAdded - Only a GuiMenuItemCtrl can be added to a GuiMenuBarCtrl! Child will not be added.");

		// Somewhere to put it, decided BEFORE taking it out of the bar. Removing
		// it first and then finding nowhere for it - which is what this did - left
		// the control registered with no group at all, so it was neither in the
		// document nor deleted. A bar with no parent is the case that reaches it.
		GuiControl *destination = getParent();
		if (destination == NULL)
		{
			Con::warnf("GuiMenuBarCtrl::onChildAdded - no parent to place it on; leaving it where it is");
			return;
		}

		removeObject(child);
		destination->addObject(child);
		return;
	}
	menu->mMenuBar = this;
	menu->setControlProfile(mMenuProfile);
	calculateMenus();

	if (size() > 1)
	{
		iterator i = end() - 2;
		GuiMenuItemCtrl *ctrl = static_cast<GuiMenuItemCtrl *>(*i);
		menu->mPrevItem = ctrl;
		ctrl->mNextItem = menu;
	}
}

void GuiMenuBarCtrl::onChildRemoved(SimObject *child)
{
	// Everything the bar still remembers about the departing item. None of this
	// mattered while a menu bar was something you built once in script and never
	// edited; deleting an item is an ordinary action now, and each of these is a
	// pointer something dereferences later - onKeyDown reads mOpenMenu,
	// setHoverTarget reads mHoverTarget, and the key walk follows the sibling
	// links.
	GuiMenuItemCtrl *item = dynamic_cast<GuiMenuItemCtrl*>(child);
	if (item != NULL)
	{
		if (item->mPrevItem != NULL)
		{
			item->mPrevItem->mNextItem = item->mNextItem;
		}
		if (item->mNextItem != NULL)
		{
			item->mNextItem->mPrevItem = item->mPrevItem;
		}
		item->mPrevItem = NULL;
		item->mNextItem = NULL;

		if (mHoverTarget == item)
		{
			mHoverTarget = NULL;
		}
		if (mOpenMenu == item)
		{
			mOpenMenu = NULL;
		}
		if (mEditOpenMenu == item)
		{
			mEditOpenMenu = NULL;
			mEditBoxRect.set(Point2I(0, 0), Point2I(0, 0));
			mEditRowRects.clear();
			mEditAddRowRect.set(Point2I(0, 0), Point2I(0, 0));
		}
	}

	calculateMenus();
}

void GuiMenuBarCtrl::calculateMenus()
{
	// Ahead of everything else: a bar that leaves edit mode must not be left
	// holding a "+" that is no longer drawn, or getAddItemRect reports a
	// rectangle nothing will answer a click in.
	mAddItemRect.set(Point2I(0, 0), Point2I(0, 0));

	RectI innerRect = getInnerRect();
	iterator i;
	S32 length = 0;
	for (i = begin(); i != end(); i++)
	{
		GuiControl *ctrl = static_cast<GuiControl *>(*i);
		if (ctrl->isVisible())
		{
			S32 width = ctrl->mProfile->getFont(mFontSizeAdjust)->getStrWidth((const UTF8*)ctrl->getText());
			Point2I innerExtent = Point2I(width, 0);
			Point2I outerExtent = getOuterExtent(innerExtent, NormalState, ctrl->mProfile);
			ctrl->mBounds.set(Point2I(length, 0), Point2I(outerExtent.x, innerRect.extent.y));
			length += ctrl->getExtent().x;
		}
	}

	// The "+" follows the last menu, square on the strip's height so it reads as
	// an affordance rather than a menu with a one-character name. Nothing to wrap
	// and nothing to run out of: the bar is a single row, and onRender keeps it
	// as wide as the clip rect it is drawn into.
	if (isEditMode())
	{
		mAddItemRect.set(Point2I(length, 0), Point2I(innerRect.extent.y, innerRect.extent.y));
	}
}

Point2I GuiMenuBarCtrl::getMenuLocalCoord(const Point2I &src)
{
	Point2I offset = Point2I(0, 0);
	RectI contentRect = getInnerRect(offset);

	return src - contentRect.point;
}

RectI GuiMenuBarCtrl::getAddItemGlobalRect()
{
	// isEditMode as well as the rectangle, because closing the editor does not
	// re-run the layout: the bar would go on reporting the "+" it had until
	// something else happened to resize it.
	if (!mAddItemRect.isValidRect() || !isEditMode())
	{
		return RectI(0, 0, 0, 0);
	}

	Point2I offset = Point2I(0, 0);
	RectI contentRect = getInnerRect(offset);

	return RectI(localToGlobalCoord(contentRect.point) + mAddItemRect.point, mAddItemRect.extent);
}

GuiMenuItemCtrl* GuiMenuBarCtrl::findSelectedMenu()
{
	GuiEditCtrl* edit = GuiControl::smEditorHandle;
	if (edit == NULL || !isEditMode())
		return NULL;

	// Walk up from each selected control. Whichever one reaches a direct child of
	// this bar names the menu being worked in - so selecting a command keeps its
	// menu open, and selecting the bar or anything else closes it.
	SimSet& selected = edit->getSelectedSet();
	for (SimSet::iterator i = selected.begin(); i != selected.end(); i++)
	{
		GuiControl* ctrl = dynamic_cast<GuiControl*>(*i);
		while (ctrl != NULL)
		{
			if (ctrl->getParent() == this)
			{
				return dynamic_cast<GuiMenuItemCtrl*>(ctrl);
			}
			ctrl = ctrl->getParent();
		}
	}

	return NULL;
}

void GuiMenuBarCtrl::refreshEditMenu()
{
	GuiMenuItemCtrl* open = findSelectedMenu();
	if (open != mEditOpenMenu)
	{
		mEditOpenMenu = open;
		setUpdate();
	}

	// Re-laid every time rather than only on a change, so renaming a command in
	// the properties pane widens the box straight away.
	layoutEditMenu();
}

void GuiMenuBarCtrl::onPreRender()
{
	refreshEditMenu();

	Parent::onPreRender();
}

void GuiMenuBarCtrl::layoutEditMenu()
{
	mEditBoxRect.set(Point2I(0, 0), Point2I(0, 0));
	mEditRowRects.clear();
	mEditAddRowRect.set(Point2I(0, 0), Point2I(0, 0));

	if (mEditOpenMenu == NULL || mMenuItemProfile == NULL || mMenuContentProfile == NULL)
		return;

	GFont* font = mMenuItemProfile->getFont(mFontSizeAdjust);
	if (font == NULL)
		return;

	Point2I rowInner = Point2I(0, font->getHeight());
	const S32 rowHeight = getOuterExtent(rowInner, NormalState, mMenuItemProfile).y;
	if (rowHeight <= 0)
		return;

	// Wide enough for the longest label, plus the two gutters the marks live in -
	// a bullet on the left, a submenu arrow on the right, each a row tall. Adding
	// them rather than taking them out of the middle is what the runtime list
	// does, and without it every label is truncated to make room for marks most
	// of the rows do not even have.
	S32 widest = 0;
	for (iterator i = mEditOpenMenu->begin(); i != mEditOpenMenu->end(); i++)
	{
		GuiMenuItemCtrl* child = dynamic_cast<GuiMenuItemCtrl*>(*i);
		if (child == NULL)
			continue;

		Point2I textInner = Point2I(font->getStrWidth((const UTF8*)child->getText()), 0);
		widest = getMax(widest, getOuterExtent(textInner, NormalState, mMenuItemProfile).x);
	}
	widest += 2 * rowHeight;

	// And never narrower than the menu it hangs from, so it reads as belonging to
	// that menu rather than floating under it.
	widest = getMax(widest, mEditOpenMenu->getExtent().x);

	// A separator is the profile's chrome with no line of text in it, which is
	// what makes it read as a rule between two groups rather than a blank row.
	//
	// In the SELECTED state, which is not a detail. Nothing else in a menu uses
	// that state - hover is Highlight, greyed is Disabled - so a theme shapes its
	// separators entirely through the SL fields of the three border profiles, and
	// measuring in any other state prices the row from padding the separator does
	// not use. GuiMenuListCtrl::updateSize measures the real thing the same way.
	Point2I emptyInner = Point2I(0, 0);
	const S32 spacerHeight = getOuterExtent(emptyInner, SelectedState, mMenuItemProfile).y;

	// One row per command plus the "+" row - which is why an empty menu still
	// gets a box. A menu with no children has no GuiMenuListCtrl at all (it is
	// built lazily when the first child arrives), so the runtime machinery could
	// not draw this case even if it were open, and it is the case that matters:
	// a menu the "+" just made has nothing in it yet.
	S32 rowsHeight = rowHeight;
	for (iterator i = mEditOpenMenu->begin(); i != mEditOpenMenu->end(); i++)
	{
		rowsHeight += editRowHeight(dynamic_cast<GuiMenuItemCtrl*>(*i), rowHeight, spacerHeight);
	}

	Point2I boxInner = Point2I(widest, rowsHeight);
	Point2I boxOuter = getOuterExtent(boxInner, NormalState, mMenuContentProfile);

	Point2I boxPoint = Point2I(mEditOpenMenu->mBounds.point.x,
		mEditOpenMenu->mBounds.point.y + mEditOpenMenu->mBounds.extent.y);
	mEditBoxRect.set(boxPoint, boxOuter);

	// Rows start at the box's content, not its corner.
	Point2I boxOrigin = boxPoint;
	RectI boxContent = getInnerRect(boxOrigin, boxOuter, NormalState, mMenuContentProfile);

	// What localToGlobalCoord will add on the way up from a command: the strip's
	// content offset, and the open menu's own contribution. Measured rather than
	// assumed - the inset renderChild stamps on a menu is not reliably there when
	// script asks, and being one border out is exactly the kind of wrong that
	// looks like nothing at all until you see the outline beside the row.
	Point2I barOrigin = Point2I(0, 0);
	Point2I barContentInset = getInnerRect(barOrigin).point;
	Point2I fromMenu = mEditOpenMenu->mBounds.point + mEditOpenMenu->mRenderInsetLT;

	S32 y = boxContent.point.y;
	for (iterator i = mEditOpenMenu->begin(); i != mEditOpenMenu->end(); i++)
	{
		S32 h = editRowHeight(dynamic_cast<GuiMenuItemCtrl*>(*i), rowHeight, spacerHeight);
		RectI rowRect = RectI(boxContent.point.x, y, widest, h);
		mEditRowRects.push_back(rowRect);

		// Give the command the rectangle it is being drawn in, so that everything
		// which asks a control where it is gets the answer the user can see.
		//
		// It is never rendered as a child, so nothing else maintains its bounds:
		// they are either the 64x64 a GuiControl is constructed with, or a
		// screen-space rectangle the RUNTIME dropdown stamped on it the last time
		// one was opened. The Gui Editor draws its selection from
		// localToGlobalCoord, so either of those puts the outline somewhere with
		// no visible relationship to the row that was clicked.
		//
		// localToGlobalCoord walks mBounds.point + mRenderInsetLT and then every
		// ancestor's, so the bounds it needs are the row less everything the walk
		// is going to add for it. This control contributes none of its own.
		GuiMenuItemCtrl* command = dynamic_cast<GuiMenuItemCtrl*>(*i);
		if (command != NULL)
		{
			command->mRenderInsetLT = Point2I(0, 0);
			command->mRenderInsetRB = Point2I(0, 0);
			command->mBounds.set(rowRect.point + barContentInset - fromMenu, rowRect.extent);
		}

		y += h;
	}

	mEditAddRowRect.set(Point2I(boxContent.point.x, y), Point2I(widest, rowHeight));
}

S32 GuiMenuBarCtrl::editRowHeight(GuiMenuItemCtrl* item, S32 rowHeight, S32 spacerHeight)
{
	if (item != NULL && item->mDisplayType == GuiMenuItemCtrl::DisplayType::Spacer)
	{
		return spacerHeight;
	}
	return rowHeight;
}

void GuiMenuBarCtrl::renderEditMenu(const Point2I &contentOffset)
{
	if (!mEditBoxRect.isValidRect())
		return;

	// The clip in force here is this control's PARENT's content rect, not the
	// bar's own bounds (GuiControl::renderChild), so the box is free to hang
	// below the strip and is clipped to the container the bar sits in - which is
	// exactly as far as it should reach.
	RectI box = mEditBoxRect;
	box.point += contentOffset;
	renderUniversalRect(box, mMenuContentProfile, NormalState);

	GuiCanvas* root = getRoot();
	Point2I cursor = (root != NULL) ? root->getCursorPos() : Point2I(-1, -1);

	S32 row = 0;
	for (iterator i = mEditOpenMenu->begin(); i != mEditOpenMenu->end() && row < mEditRowRects.size(); i++, row++)
	{
		GuiMenuItemCtrl* child = dynamic_cast<GuiMenuItemCtrl*>(*i);
		if (child == NULL)
			continue;

		RectI rowRect = mEditRowRects[row];
		rowRect.point += contentOffset;

		// A separator draws as the runtime draws one - the profile in its SELECTED
		// state, no text and no marks - so a rule looks here like it will look in
		// the game.
		if (child->mDisplayType == GuiMenuItemCtrl::DisplayType::Spacer)
		{
			RectI spacerRect = applyMargins(rowRect.point, rowRect.extent, SelectedState, mMenuItemProfile);
			if (spacerRect.isValidRect())
			{
				renderUniversalRect(spacerRect, mMenuItemProfile, SelectedState);
			}
			continue;
		}

		GuiControlState state = child->isActive() ? NormalState : DisabledState;
		if (rowRect.pointInRect(cursor))
			state = HighlightState;

		// Margins first, as GuiMenuListCtrl::onRenderItem does. A theme that puts
		// a margin on a menu item - which is how a separator is given the room
		// that makes its two border lines read as a groove rather than as the
		// edges of a band - would otherwise draw differently here than in the game.
		RectI ctrlRect = applyMargins(rowRect.point, rowRect.extent, state, mMenuItemProfile);
		if (!ctrlRect.isValidRect())
			continue;

		renderUniversalRect(ctrlRect, mMenuItemProfile, state);

		// The same marks GuiMenuListCtrl::onRenderItem draws, so what a kind
		// looks like is what it will look like: a square bullet for a toggle, a
		// round one for a radio item, lit when it starts on, and an arrow on the
		// right for an item that opens a submenu of its own.
		RectI leftIcon = RectI(rowRect.point, Point2I(rowRect.extent.y, rowRect.extent.y));
		if (child->mDisplayType == GuiMenuItemCtrl::DisplayType::Toggle ||
			child->mDisplayType == GuiMenuItemCtrl::DisplayType::Radio)
		{
			ColorI markColor = child->mIsOn ? mMenuItemProfile->getFillColor(HighlightState)
				: mMenuItemProfile->getFillColor(NormalState);
			S32 fontHeight = mMenuItemProfile->getFont(mFontSizeAdjust)->getHeight();
			renderColorBullet(leftIcon, markColor, getMin(fontHeight, 16),
				child->mDisplayType == GuiMenuItemCtrl::DisplayType::Radio);
		}

		if (child->mDisplayType == GuiMenuItemCtrl::DisplayType::Menu)
		{
			RectI rightIcon = RectI(
				Point2I(rowRect.point.x + rowRect.extent.x - rowRect.extent.y, rowRect.point.y),
				leftIcon.extent);
			rightIcon.inset(2, 0);
			ColorI arrowColor = ColorI(getFontColor(mMenuItemProfile, state));
			renderTriangleIcon(rightIcon, arrowColor, GuiDirection::Right,
				mMenuItemProfile->getFont(mFontSizeAdjust)->getHeight() / 2);
		}

		// Indented past the mark column, the way the runtime rows are, so a menu
		// of mixed kinds still reads as one column of labels.
		dglSetBitmapModulation(getFontColor(mMenuItemProfile, state));
		RectI textRect = RectI(rowRect.point.x + rowRect.extent.y, rowRect.point.y,
			rowRect.extent.x - (2 * rowRect.extent.y), rowRect.extent.y);
		renderText(textRect.point, textRect.extent, child->getText(), mMenuItemProfile);
	}

	if (mEditAddRowRect.isValidRect())
	{
		RectI addRow = mEditAddRowRect;
		addRow.point += contentOffset;
		renderAddItem(addRow);
	}
}

RectI GuiMenuBarCtrl::getAddSubItemGlobalRect()
{
	// Worked out on demand rather than read back from the last frame. Which menu
	// is open follows the selection, and the selection changes without anything
	// being drawn - script selects a menu and asks about it in the same breath.
	refreshEditMenu();

	if (!mEditAddRowRect.isValidRect() || !isEditMode())
	{
		return RectI(0, 0, 0, 0);
	}

	Point2I offset = Point2I(0, 0);
	RectI contentRect = getInnerRect(offset);

	return RectI(localToGlobalCoord(contentRect.point) + mEditAddRowRect.point, mEditAddRowRect.extent);
}

GuiMenuItemCtrl* GuiMenuBarCtrl::findMenuAt(const Point2I &menuLocalPt)
{
	// Deliberately not findHitMenu: that one refuses an inactive menu, and a
	// greyed-out menu is exactly the kind you open the editor to fix. Only
	// visibility is honoured, because calculateMenus does not place a hidden item
	// and its rectangle is therefore whatever it was last time.
	iterator i;
	for (i = begin(); i != end(); i++)
	{
		GuiMenuItemCtrl *ctrl = dynamic_cast<GuiMenuItemCtrl *>(*i);
		if (ctrl != NULL && ctrl->isVisible() && ctrl->mBounds.pointInRect(menuLocalPt))
		{
			return ctrl;
		}
	}
	return NULL;
}

void GuiMenuBarCtrl::renderAddItem(RectI itemRect)
{
	GuiEditCtrl* edit = GuiControl::smEditorHandle;
	if (edit == NULL)
		return;

	// Ghosted, so it never passes for a menu. Brighter under the cursor, so it
	// reads as something to click rather than the empty tail of the bar - the
	// same treatment the tab book's "+" tab gets.
	ColorI fill = edit->getEditorColor();
	fill.alpha = 100;

	GuiCanvas* root = getRoot();
	if (root != NULL && itemRect.pointInRect(root->getCursorPos()))
		fill.alpha = 200;

	dglDrawRectFill(itemRect, fill);

	dglSetBitmapModulation(getFontColor(edit->mProfile, NormalState));
	F32 tempAdjust = mFontSizeAdjust;
	mFontSizeAdjust = 1.5f;
	renderText(itemRect.point, itemRect.extent, "+", edit->mProfile);
	mFontSizeAdjust = tempAdjust;
}

void GuiMenuBarCtrl::requestNewMenuItem(GuiMenuItemCtrl *parent)
{
	// The GuiEditCtrl wears the GuiEditorBrain namespace, so this arrives at
	// GuiEditorBrain::onAddMenuItem. A bar being edited by anything with no
	// handler for it simply gets no item, which is the right way for this to
	// fail.
	GuiEditCtrl* edit = GuiControl::smEditorHandle;
	if (edit != NULL && edit->isMethod("onAddMenuItem"))
	{
		Con::executef(edit, 3, "onAddMenuItem", getIdString(),
			(parent != NULL) ? parent->getIdString() : "");
	}
}

bool GuiMenuBarCtrl::pointInControl(const Point2I& parentCoordPoint)
{
	if (Parent::pointInControl(parentCoordPoint))
		return true;

	// The dropdown drawn while authoring hangs BELOW the strip, outside the bar's
	// own bounds - and both findHitControl and the canvas walk bounds, so without
	// this nobody ever offers the bar a click on its own "+" row. Empty outside
	// edit mode, so this says nothing about a bar in a running game.
	if (mEditBoxRect.isValidRect())
	{
		Point2I origin = Point2I(0, 0);
		RectI contentRect = getInnerRect(origin);

		RectI box = mEditBoxRect;
		box.point += mBounds.point + contentRect.point;

		if (box.pointInRect(parentCoordPoint))
			return true;
	}

	return false;
}

bool GuiMenuBarCtrl::onMouseDownEditor(const GuiEvent &event, const Point2I& offset)
{
	Point2I menuLocalMouse = getMenuLocalCoord(globalToLocalCoord(event.mousePoint));
	GuiEditCtrl* edit = GuiControl::smEditorHandle;

	if (mAddItemRect.isValidRect() && mAddItemRect.pointInRect(menuLocalMouse))
	{
		requestNewMenuItem(NULL);

		// Nothing else happens on this click. Selection follows the item the
		// editor is about to make.
		return true;
	}

	// The open dropdown, which is drawn over whatever is beneath it and so gets
	// asked before it.
	if (mEditAddRowRect.isValidRect() && mEditAddRowRect.pointInRect(menuLocalMouse))
	{
		requestNewMenuItem(mEditOpenMenu);
		return true;
	}

	if (mEditOpenMenu != NULL && edit != NULL)
	{
		for (S32 row = 0; row < mEditRowRects.size(); row++)
		{
			if (mEditRowRects[row].pointInRect(menuLocalMouse) && row < mEditOpenMenu->size())
			{
				edit->select(dynamic_cast<GuiControl*>((*mEditOpenMenu)[row]));
				return true;
			}
		}
	}

	// The bar is opaque to findHitControl, so nothing else is going to offer the
	// menu itself as the thing that was clicked.
	GuiMenuItemCtrl *item = findMenuAt(menuLocalMouse);
	if (item != NULL && edit != NULL)
	{
		edit->select(item);
		return true;
	}

	return Parent::onMouseDownEditor(event, offset);
}

// The bar is opaque to hit testing: it places its items itself and answers for
// them by hand, through findHitMenu.
//
// This used to take two parameters where GuiControl's takes four, so it hid the
// base rather than overriding it and never ran - every caller reaches this
// through a GuiControl*. The base then descended into the items, and on into
// THEIR children, whose mBounds are either the default 64x64 or a canvas-global
// rectangle GuiMenuListCtrl stamped on them the last time a dropdown was drawn.
// Clicking a menu on an authored bar therefore handed the Gui Editor the last
// command inside it.
GuiControl* GuiMenuBarCtrl::findHitControl(const Point2I &pt, S32 initialLayer, const bool ignoreUseInput, const bool ignoreEditSelected)
{
	return this;
}

void GuiMenuBarCtrl::setUpdate()
{
	Parent::setUpdate();

	// A top-level menu is exactly as wide as its caption, so the strip's layout
	// is a function of the text and has to be redone whenever the text might
	// have moved. The properties pane writes a caption on every keystroke, and
	// this is what makes the bar reflow under the cursor as it is typed.
	calculateMenus();
}

void GuiMenuBarCtrl::childrenReordered()
{
	// The strip is packed left to right in child order, and nothing else re-runs
	// it on a reorder - so dragging an item in the Gui Editor's tree would leave
	// every item wearing the rectangle of whichever item used to be there.
	calculateMenus();

	Parent::childrenReordered();
}

GuiMenuItemCtrl* GuiMenuBarCtrl::findHitMenu(const Point2I &pt)
{
	iterator i;
	for (i = begin(); i != end(); i++)
	{
		GuiMenuItemCtrl *ctrl = static_cast<GuiMenuItemCtrl *>(*i);
		if (ctrl && ctrl->isVisible() && ctrl->isActive())
		{
			if (ctrl->mBounds.pointInRect(pt - ctrl->mRenderInsetLT))
			{
				return ctrl;
			}
		}
	}
	return NULL;
}

void GuiMenuBarCtrl::processHover(const GuiEvent &event)
{
	GuiMenuItemCtrl *ctrl = findHitMenu(globalToLocalCoord(event.mousePoint));
	mUseKeyMode = false;
	setHoverTarget(ctrl);
}

void GuiMenuBarCtrl::setHoverTarget(GuiMenuItemCtrl *ctrl)
{
	if (mHoverTarget != ctrl)
	{
		if (mHoverTarget != NULL)
		{
			mHoverTarget->mIsHover = false;
		}
		if(ctrl != NULL)
		{
			ctrl->mIsHover = true;
		}
		mHoverTarget = ctrl;
	}
}

void GuiMenuBarCtrl::onTouchMove(const GuiEvent &event)
{
	processHover(event);
}

void GuiMenuBarCtrl::onTouchEnter(const GuiEvent &event)
{
	processHover(event);
}

void GuiMenuBarCtrl::onTouchLeave(const GuiEvent &event)
{
	if (mHoverTarget != NULL) {
		mHoverTarget->mIsHover = false;
	}
	mHoverTarget = NULL;
	clearFirstResponder();
}

void GuiMenuBarCtrl::onTouchDown(const GuiEvent &event)
{
	GuiMenuItemCtrl *ctrl = findHitMenu(globalToLocalCoord(event.mousePoint));
	if (mOpenMenu != ctrl)
	{
		if (mOpenMenu != NULL)
		{
			mOpenMenu->closeMenu();
		}
		if (ctrl != NULL)
		{
			ctrl->mIsOpen = true;
		}
		mOpenMenu = ctrl;
	}
	openMenu();
}

void GuiMenuBarCtrl::onRender(Point2I offset, const RectI &updateRect)
{
	RectI clipRect = dglGetClipRect();
	if (clipRect.extent.x != mBounds.extent.x || mBounds.point.x != 0 || mBounds.point.y != 0)
	{
		resize(Point2I(0, 0), Point2I(clipRect.extent.x, mBounds.extent.y));
	}
	RectI ctrlRect = applyMargins(offset, mBounds.extent, NormalState, mProfile);

	if (!ctrlRect.isValidRect())
	{
		return;
	}

	renderUniversalRect(ctrlRect, mProfile, NormalState);

	RectI fillRect = applyBorders(ctrlRect.point, ctrlRect.extent, NormalState, mProfile);
	RectI contentRect = applyPadding(fillRect.point, fillRect.extent, NormalState, mProfile);

	if (contentRect.isValidRect())
	{
		//Render the childen
		renderChildControls(offset, contentRect, updateRect);

		// After the menus, so it always reads as the end of the strip. Empty
		// unless calculateMenus found itself in edit mode, which is the whole
		// test - and on a bar with no menus at all it is the only way to get one.
		if (mAddItemRect.isValidRect())
		{
			RectI addBounds = mAddItemRect;
			addBounds.point += contentRect.point;
			renderAddItem(addBounds);
		}

		// Last, so the open menu's box covers the strip's own "+" if the two ever
		// overlap.
		renderEditMenu(contentRect.point);
	}
}

void GuiMenuBarCtrl::openMenu()
{
	if (mOpenMenu == NULL)
	{
		return;
	}

	GuiCanvas *root = getRoot();
	AssertFatal(root, "GuiMenuBarCtrl::openMenu: Unable to optain the Canvas!");
	mBackground->mBounds.extent = root->mBounds.extent;
	mBackground->setControlProfile(mBackgroundProfile);

	ApplyMenuSettings();

	mBackground->openMenu();

	root->pushDialogControl(mBackground, 99);

	setFirstResponder();

	if (isMethod("onOpen"))
		Con::executef(this, 1, "onOpen");
}

void GuiMenuBarCtrl::closeMenu()
{
	if (!mOpenMenu)
		return;

	mOpenMenu->closeMenu();

	mBackground->getRoot()->popDialogControl(mBackground);

	mOpenMenu->mIsOpen = false;
	mOpenMenu->mIsHover = false;
	mOpenMenu = NULL;

	if (isMethod("onClose"))
		Con::executef(this, 1, "onClose");
}

void GuiMenuBarCtrl::ApplyMenuSettings()
{
	iterator i;
	for (i = begin(); i != end(); i++)
	{
		GuiMenuItemCtrl *ctrl = static_cast<GuiMenuItemCtrl *>(*i);
		ctrl->ApplyMenuSettings();
		ctrl->setControlProfile(mMenuProfile);
	}
	setUpdate();
}

void GuiMenuBarCtrl::setMenuActive(const char* name, bool isActive)
{
	iterator i;
	for (i = begin(); i != end(); i++)
	{
		GuiMenuItemCtrl* ctrl = static_cast<GuiMenuItemCtrl*>(*i);
		StringTableEntry ctrlName = StringTable->insert(ctrl->getText(), true);
		StringTableEntry target = StringTable->insert(name, true);
		if (ctrlName == target)
		{
			ctrl->setActive(isActive);
		}
		ctrl->setMenuActive(target, isActive);
		ctrl->ApplyMenuSettings();
		ctrl->setControlProfile(mMenuProfile);
	}
}

void GuiMenuBarCtrl::acceleratorKeyPress(U32 index)
{
	if (mUseKeyMode && mOpenMenu != NULL)
	{
		mUseKeyMode = true;
		GuiMenuItemCtrl *ctrl = mOpenMenu;
		closeMenu();
		mHoverTarget = ctrl;
		mHoverTarget->mIsHover = true;
	}
	else if (mUseKeyMode)
	{
		mUseKeyMode = false;
		if (mHoverTarget != NULL)
		{
			mHoverTarget->mIsHover = false;
		}
		mHoverTarget = NULL;
		clearFirstResponder();
	}
	else
	{
		mUseKeyMode = true;
		GuiMenuItemCtrl *newTarget = dynamic_cast<GuiMenuItemCtrl *>(first());
		setHoverTarget(newTarget);
		setFirstResponder();
	}
}

bool GuiMenuBarCtrl::onKeyDown(const GuiEvent &event)
{
	if (!mVisible || !mActive || !mAwake)
		return false;

	if (mHoverTarget == NULL && mOpenMenu == NULL)
	{
		clearFirstResponder();
		return false;
	}

	if ((event.keyCode == KEY_ALT || event.keyCode == KEY_LALT || event.keyCode == KEY_RALT) && event.modifier == 0)
	{
		acceleratorKeyPress(0);
		return true;
	}
	else if (mOpenMenu != NULL)
	{
		mOpenMenu->onKeyDown(event);
	}
	else if (event.keyCode == KEY_RIGHT && event.modifier == 0)
	{
		mUseKeyMode = true;
		if (mHoverTarget->mNextItem != NULL)
		{
			setHoverTarget(mHoverTarget->mNextItem);
			if (!mHoverTarget->isVisible() || !mHoverTarget->isActive() || !mHoverTarget->mHasGoodChildren)
			{
				onKeyDown(event);
			}
		}
		else
		{
			GuiMenuItemCtrl *newTarget = dynamic_cast<GuiMenuItemCtrl *>(first());
			setHoverTarget(newTarget);
			if (!mHoverTarget->isVisible() || !mHoverTarget->isActive() || !mHoverTarget->mHasGoodChildren)
			{
				onKeyDown(event);
			}
		}
		return true;
	}
	else if (event.keyCode == KEY_LEFT && event.modifier == 0)
	{
		mUseKeyMode = true;
		if (mHoverTarget->mPrevItem != NULL)
		{
			setHoverTarget(mHoverTarget->mPrevItem);
			if (!mHoverTarget->isVisible() || !mHoverTarget->isActive() || !mHoverTarget->mHasGoodChildren)
			{
				onKeyDown(event);
			}
		}
		else
		{
			GuiMenuItemCtrl *newTarget = dynamic_cast<GuiMenuItemCtrl *>(last());
			setHoverTarget(newTarget);
			if (!mHoverTarget->isVisible() || !mHoverTarget->isActive() || !mHoverTarget->mHasGoodChildren)
			{
				onKeyDown(event);
			}
		}
		return true;
	}
	else if (event.keyCode == KEY_DOWN && event.modifier == 0)
	{
		mUseKeyMode = true;
		if (mOpenMenu == NULL && mHoverTarget->mDisplayType == GuiMenuItemCtrl::DisplayType::Menu && mHoverTarget->isVisible() && mHoverTarget->isActive() && mHoverTarget->mHasGoodChildren)
		{
			mOpenMenu = mHoverTarget;
			mOpenMenu->mIsHover = false;
			mOpenMenu->mIsOpen = true;
			openMenu();
			return true;
		}
	}

	return false;
}

bool GuiMenuBarCtrl::onWake()
{
	if (!Parent::onWake())
		return false;

	if (mMenuProfile != NULL)
		mMenuProfile->incRefCount();

	if (mMenuItemProfile != NULL)
		mMenuItemProfile->incRefCount();

	if (mMenuContentProfile != NULL)
		mMenuContentProfile->incRefCount();

	if (mBackgroundProfile != NULL)
		mBackgroundProfile->incRefCount();

	if (mThumbProfile != NULL)
		mThumbProfile->incRefCount();

	if (mTrackProfile != NULL)
		mTrackProfile->incRefCount();

	if (mArrowProfile != NULL)
		mArrowProfile->incRefCount();

	return true;
}

void GuiMenuBarCtrl::onSleep()
{
	Parent::onSleep();

	if (mMenuProfile != NULL)
		mMenuProfile->decRefCount();

	if (mMenuItemProfile != NULL)
		mMenuItemProfile->decRefCount();

	if (mMenuContentProfile != NULL)
		mMenuContentProfile->decRefCount();

	if (mBackgroundProfile != NULL)
		mBackgroundProfile->decRefCount();

	if (mThumbProfile != NULL)
		mThumbProfile->decRefCount();

	if (mTrackProfile != NULL)
		mTrackProfile->decRefCount();

	if (mArrowProfile != NULL)
		mArrowProfile->decRefCount();
}

void GuiMenuBarCtrl::setControlBackgroundProfile(GuiControlProfile* prof)
{
	AssertFatal(prof, "GuiMenuBarCtrl::setControlBackgroundProfile: invalid background profile");
	if (prof == mBackgroundProfile)
		return;
	if (mAwake)
		mBackgroundProfile->decRefCount();
	mBackgroundProfile = prof;
	if (mAwake)
		mBackgroundProfile->incRefCount();

	ApplyMenuSettings();
}

void GuiMenuBarCtrl::setControlMenuProfile(GuiControlProfile* prof)
{
	AssertFatal(prof, "GuiMenuBarCtrl::setControlMenuProfile: invalid profile");
	if (prof == mMenuProfile)
		return;
	if (mAwake)
		mMenuProfile->decRefCount();
	mMenuProfile = prof;
	if (mAwake)
		mMenuProfile->incRefCount();

	ApplyMenuSettings();
	setUpdate();
}

void GuiMenuBarCtrl::setControlMenuItemProfile(GuiControlProfile* prof)
{
	AssertFatal(prof, "GuiMenuBarCtrl::setControlMenuItemProfile: invalid profile");
	if (prof == mMenuItemProfile)
		return;
	if (mAwake)
		mMenuItemProfile->decRefCount();
	mMenuItemProfile = prof;
	if (mAwake)
		mMenuItemProfile->incRefCount();

	ApplyMenuSettings();
	setUpdate();
}

void GuiMenuBarCtrl::setControlMenuContentProfile(GuiControlProfile* prof)
{
	AssertFatal(prof, "GuiMenuBarCtrl::setControlMenuContentProfile: invalid profile");
	if (prof == mMenuContentProfile)
		return;
	if (mAwake)
		mMenuContentProfile->decRefCount();
	mMenuContentProfile = prof;
	if (mAwake)
		mMenuContentProfile->incRefCount();

	ApplyMenuSettings();
}

void GuiMenuBarCtrl::setControlThumbProfile(GuiControlProfile* prof)
{
	AssertFatal(prof, "GuiMenuBarCtrl::setControlThumbProfile: invalid thumb profile");
	if (prof == mThumbProfile)
		return;
	if (mAwake)
		mThumbProfile->decRefCount();
	mThumbProfile = prof;
	if (mAwake)
		mThumbProfile->incRefCount();

	ApplyMenuSettings();
}

void GuiMenuBarCtrl::setControlTrackProfile(GuiControlProfile* prof)
{
	AssertFatal(prof, "GuiMenuBarCtrl::setControlTrackProfile: invalid track profile");
	if (prof == mTrackProfile)
		return;
	if (mAwake)
		mTrackProfile->decRefCount();
	mTrackProfile = prof;
	if (mAwake)
		mTrackProfile->incRefCount();

	ApplyMenuSettings();
}

void GuiMenuBarCtrl::setControlArrowProfile(GuiControlProfile* prof)
{
	AssertFatal(prof, "GuiMenuBarCtrl::setControlArrowProfile: invalid arrow profile");
	if (prof == mArrowProfile)
		return;
	if (mAwake)
		mArrowProfile->decRefCount();
	mArrowProfile = prof;
	if (mAwake)
		mArrowProfile->incRefCount();

	ApplyMenuSettings();
}
#pragma endregion

#pragma region GuiMenuItemCtrl
IMPLEMENT_CONOBJECT(GuiMenuItemCtrl);

GuiMenuItemCtrl::GuiMenuItemCtrl()
{
	mText = "Menu";
	mDisplayType = TextCommand;
	mActive = true;
	mIsOpen = false;
	mIsHover = false;
	mIsOn = false;
	mToggle = false;
	mRadio = false;
	mHasGoodChildren = true;

	mPrevItem = NULL;
	mNextItem = NULL;
	mOpenSubMenu = NULL;

	// Only ever assigned once this item is inside a bar, or inside an item that
	// is - and onChildAdded reads it before either has necessarily happened.
	mMenuBar = NULL;

	mScroll = NULL;
	mList = NULL;
}

void GuiMenuItemCtrl::initPersistFields()
{
	SimObject::initPersistFields();

	addGroup("MenuItem");
	addField("Variable", TypeString, Offset(mConsoleVariable, GuiMenuItemCtrl));
	addField("Command", TypeString, Offset(mConsoleCommand, GuiMenuItemCtrl));
	addField("AltCommand", TypeString, Offset(mAltConsoleCommand, GuiMenuItemCtrl));
	addField("Accelerator", TypeString, Offset(mAcceleratorKey, GuiMenuItemCtrl));
	addField("Active", TypeBool, Offset(mActive, GuiMenuItemCtrl));
	addField("Visible", TypeBool, Offset(mVisible, GuiMenuItemCtrl));
	addProtectedField("IsOn", TypeBool, Offset(mIsOn, GuiMenuItemCtrl), &defaultProtectedSetFn, &defaultProtectedGetFn, &writeIsOn, "");
	addProtectedField("Toggle", TypeBool, Offset(mToggle, GuiMenuItemCtrl), &setToggle, &defaultProtectedGetFn, &writeToggle, "");
	// TypeBool, not TypeS32: mRadio is a bool, and setRadio has always written it
	// with dAtob. Declared as an S32 the default getter read four bytes off a
	// one-byte member and answered with whatever was next to it, so a plain
	// command could read back as a radio item.
	addProtectedField("Radio", TypeBool, Offset(mRadio, GuiMenuItemCtrl), &setRadio, &defaultProtectedGetFn, &writeRadio, "");
	endGroup("MenuItem");

	addGroup("Localization");
	addField("langTableMod", TypeString, Offset(mLangTableName, GuiMenuItemCtrl));
	endGroup("Localization");

	addGroup("Text");
	addProtectedField("text", TypeCaseString, Offset(mText, GuiMenuItemCtrl), setTextProperty, getTextProperty, "");
	addField("textID", TypeString, Offset(mTextID, GuiMenuItemCtrl));
	endGroup("Text");
}

void GuiMenuItemCtrl::onAction()
{
	if (!mActive || !mVisible)
		return;

	if (mDisplayType == TextCommand)
	{
		Parent::onAction();
	}
	else if (mDisplayType == Toggle || mDisplayType == Radio)
	{
		toggleControl();
	}
}

void GuiMenuItemCtrl::inspectPostApply()
{
	Parent::inspectPostApply();

	if(mDisplayType == Menu)
	{
		checkForGoodChildren();
	}

	GuiMenuItemCtrl *menu = dynamic_cast<GuiMenuItemCtrl *>(getParent());
	if (menu != NULL)
	{
		menu->checkForGoodChildren();
	}
}

void GuiMenuItemCtrl::onChildAdded(GuiControl *child)
{
	GuiMenuItemCtrl *subMenu = dynamic_cast<GuiMenuItemCtrl*>(child);
	if (!subMenu)
	{
		Con::warnf("GuiMenuItemCtrl::onChildAdded - Only a GuiMenuItemCtrl can be added to a GuiMenuItemCtrl! Child will not be added.");

		// As in GuiMenuBarCtrl::onChildAdded above: find the destination first, so
		// a refusal cannot orphan the control it refused.
		GuiControl *destination = getParent();
		if (destination == NULL)
		{
			Con::warnf("GuiMenuItemCtrl::onChildAdded - no parent to place it on; leaving it where it is");
			return;
		}

		removeObject(child);
		destination->addObject(child);
		return;
	}
	subMenu->mMenuBar = this->mMenuBar;

	// mMenuBar is NULL until this item is itself inside a bar, and an item built
	// in script can be given children before it is added to one.
	if (mMenuBar != NULL)
	{
		subMenu->setControlProfile(mMenuBar->mMenuItemProfile);
	}
	if (dStrcmp(subMenu->getText(), "-") == 0)
	{
		// The text is left as "-" rather than cleared. It is the only record that
		// this is a separator - there is no field for it - so clearing it meant a
		// separator did not survive being saved and read back: the reloaded item
		// had no dash left to recognise it by. Keeping it also lets the properties
		// pane show what it is, and lets someone type their way back out of it.
		subMenu->mDisplayType = Spacer;
	}
	else if (subMenu->mToggle)
	{
		subMenu->mDisplayType = Toggle;
	}
	else if (subMenu->mRadio)
	{
		subMenu->mDisplayType = Radio;
	}

	if (size() > 1)
	{
		iterator i = end() - 2;
		GuiMenuItemCtrl *ctrl = static_cast<GuiMenuItemCtrl *>(*i);
		subMenu->mPrevItem = ctrl;
		ctrl->mNextItem = subMenu;
	}

	if (mDisplayType != Menu)
	{
		mDisplayType = Menu;
		if(mScroll == NULL)
		{
			mScroll = new GuiScrollCtrl();
			mScroll->setField("horizSizing","right");
			mScroll->setField("vertSizing","bottom");
			AssertFatal(mScroll, "GuiMenuItemCtrl::onChildAdded Failed to initialize GuiScrollCtrl!");
		}

		if(mList == NULL)
		{
			mList = new GuiMenuListCtrl(this);
			AssertFatal(mList, "GuiMenuItemCtrl::onChildAdded Failed to initialize GuiMenuListCtrl!");
			mScroll->addObject(mList);
		}
	}

	if (mDisplayType == Menu)
	{
		mList->updateSize();
	}
	checkForGoodChildren();
}

void GuiMenuItemCtrl::onChildRemoved(SimObject *child)
{
	// The same repair GuiMenuBarCtrl::onChildRemoved makes, for a submenu's own
	// children: the sibling links the keyboard walk follows, and the two pointers
	// that would otherwise name a control this item no longer holds.
	GuiMenuItemCtrl *item = dynamic_cast<GuiMenuItemCtrl*>(child);
	if (item != NULL)
	{
		if (item->mPrevItem != NULL)
		{
			item->mPrevItem->mNextItem = item->mNextItem;
		}
		if (item->mNextItem != NULL)
		{
			item->mNextItem->mPrevItem = item->mPrevItem;
		}
		item->mPrevItem = NULL;
		item->mNextItem = NULL;

		if (mOpenSubMenu == item)
		{
			mOpenSubMenu = NULL;
		}
		if (mList != NULL && mList->mHoveredItem == item)
		{
			mList->mHoveredItem = NULL;
		}
	}

	if (size() <= 0)
	{
		if (mDisplayType == Menu)
		{
			// Back to a plain command, which is what an item that never had
			// children is - so an emptied menu draws the same as a fresh one.
			mDisplayType = TextCommand;

			// The scroller and the list are KEPT, not freed. They are built with a
			// bare new and never registered - as is every hidden helper control in
			// this file - so deleteObject asserts on them ("Object not
			// registered"), and registering them instead moves them into the
			// canvas's ownership and hangs the engine on the way down if a menu is
			// open when it quits. onChildAdded above reuses them if a child ever
			// comes back, which in the Gui Editor it routinely does.
			//
			// Nothing reached any of this until a menu could be emptied one
			// command at a time while authoring.
			if (mIsOpen && mMenuBar != NULL)
			{
				closeMenu();
			}
		}
	}

	checkForGoodChildren();
}

// A menu item is a row in a bar or a row in a menu and nothing else: it exposes
// none of GuiControl's geometry (initPersistFields calls SimObject's), its
// rectangle is dictated by whatever is laying it out, and anywhere else it is a
// control nothing will ever draw a row for. So it refuses every other parent and
// the Gui Editor leaves it where it is.
//
// Two legal kinds of parent rather than the one a tab page has - moving a
// command from one menu to another, or a menu from one bar to another, are both
// ordinary things to want.
bool GuiMenuItemCtrl::canBeChildOf(GuiControl* parent)
{
	return dynamic_cast<GuiMenuBarCtrl*>(parent) != NULL ||
		dynamic_cast<GuiMenuItemCtrl*>(parent) != NULL;
}

void GuiMenuItemCtrl::setText(const char *txt)
{
	Parent::setText(txt);

	// A single dash is how the documentation says you write a separator, and the
	// text is the only record of one. Derived here rather than only when the item
	// is added, so typing a dash into the properties pane turns the row into a
	// separator as it is typed - and typing over the dash turns it back.
	//
	// Only inside a menu: a separator on the bar itself would be a gap in the
	// strip with nothing either side of it to separate. And never over a Menu,
	// which is what having children makes an item.
	if (mDisplayType != Menu && dynamic_cast<GuiMenuItemCtrl*>(getParent()) != NULL)
	{
		if (dStrcmp(getText(), "-") == 0)
		{
			mDisplayType = Spacer;
		}
		else if (mDisplayType == Spacer)
		{
			mDisplayType = mToggle ? Toggle : (mRadio ? Radio : TextCommand);
		}
	}

	// Whoever is laying this item out sized it from the text that just changed.
	// For a top-level menu that is the bar, whose setUpdate re-runs
	// calculateMenus; for a command inside a menu the parent is another item,
	// whose base setUpdate does nothing - and it does not need to, because the
	// authored dropdown is re-measured every frame in refreshEditMenu.
	GuiControl *parent = getParent();
	if (parent != NULL)
	{
		parent->setUpdate();
	}
}

void GuiMenuItemCtrl::checkForGoodChildren()
{
	if (mDisplayType != Menu)
	{
		mHasGoodChildren = true;
		return;
	}

	iterator iter;
	for (iter = begin(); iter != end(); iter++)
	{
		GuiMenuItemCtrl *ctrl = static_cast<GuiMenuItemCtrl *>(*iter);
		if (ctrl->isVisible() && ctrl->isActive() && ctrl->mDisplayType != Spacer)
		{
			mHasGoodChildren = true;
			return;
		}
	}
	mHasGoodChildren = false;
}

void GuiMenuItemCtrl::closeMenu()
{
	if (mOpenSubMenu != NULL)
	{
		mOpenSubMenu->closeMenu();
	}
	mOpenSubMenu = NULL;
	mList->mHoveredItem = NULL;
	mMenuBar->mBackground->removeObject(mScroll);
	mIsOpen = false;
}

void GuiMenuItemCtrl::ApplyMenuSettings()
{
	if (mDisplayType == Menu)
	{
		mScroll->setControlProfile(mMenuBar->mMenuContentProfile);
		mScroll->setControlThumbProfile(mMenuBar->mThumbProfile);
		mScroll->setControlArrowProfile(mMenuBar->mArrowProfile);
		mScroll->setControlTrackProfile(mMenuBar->mTrackProfile);
		mScroll->setField("hScrollBar", "AlwaysOff");
		mScroll->setField("vScrollBar", "Dynamic");
		mScroll->mUseConstantHeightThumb = mMenuBar->mUseConstantHeightThumb;
		mScroll->mScrollBarThickness = mMenuBar->mScrollBarThickness;
		mScroll->mShowArrowButtons = mMenuBar->mShowArrowButtons;
	}

	iterator i;
	for (i = begin(); i != end(); i++)
	{
		GuiMenuItemCtrl *ctrl = static_cast<GuiMenuItemCtrl *>(*i);
		ctrl->ApplyMenuSettings();
	}
	setControlProfile(mMenuBar->mMenuItemProfile);
}

void GuiMenuItemCtrl::onRender(Point2I offset, const RectI& updateRect)
{
	GuiControlState currentState = GuiControlState::NormalState;
	if (!mActive || !mHasGoodChildren)
		currentState = GuiControlState::DisabledState;
	else if (mIsOpen)
		currentState = GuiControlState::SelectedState;
	else if (mIsHover)
		currentState = GuiControlState::HighlightState;

	RectI ctrlRect = applyMargins(offset, mBounds.extent, currentState, mProfile);

	renderUniversalRect(ctrlRect, mProfile, currentState);

	//Render Text
	dglSetBitmapModulation(getFontColor(mProfile, currentState));
	RectI fillRect = applyBorders(ctrlRect.point, ctrlRect.extent, currentState, mProfile);
	RectI contentRect = applyPadding(fillRect.point, fillRect.extent, currentState, mProfile);
	renderText(contentRect.point, contentRect.extent, mText, mProfile);
}

void GuiMenuItemCtrl::toggleControl()
{
	if(mDisplayType == Toggle && !mIsOn)
	{
		mIsOn = true;
		Parent::onAction();
	}
	else if (mDisplayType == Toggle && mIsOn)
	{
		mIsOn = false;
		execAltConsoleCallback();
	}
	else if(mDisplayType == Radio && !mIsOn)
	{
		mIsOn = true;
		Parent::onAction();
		//Turn off other radio controls in this block
		turnOffPrevRadio();
		turnOffNextRadio();
	}
}

void GuiMenuItemCtrl::turnOffPrevRadio()
{
	if (mPrevItem != NULL && mPrevItem->mDisplayType == Radio)
	{
		if(mPrevItem->mIsOn)
		{
			mPrevItem->mIsOn = false;
			mPrevItem->execAltConsoleCallback();
		}
		mPrevItem->turnOffPrevRadio();
	}
}

void GuiMenuItemCtrl::turnOffNextRadio()
{
	if (mNextItem != NULL && mNextItem->mDisplayType == Radio)
	{
		if (mNextItem->mIsOn)
		{
			mNextItem->mIsOn = false;
			mNextItem->execAltConsoleCallback();
		}
		mNextItem->turnOffNextRadio();
	}
}

const char* GuiMenuItemCtrl::getHotKeyText()
{
	if(mAcceleratorKey == StringTable->EmptyString || mDisplayType == Menu)
		return StringTable->EmptyString;

	char key[128];
	dSprintf(key, 128, "%s", mAcceleratorKey);

	char buffer[128];
	buffer[0] = '\0';
	char *tok;
	tok = dStrtok((char*)key, " -");
	while (tok != NULL)
	{
		if(buffer[0] != '\0')
			dStrcat(buffer, "+");
		dStrcat(buffer, ActionMap::swapCtrlForCmd(tok));

		tok = dStrtok(NULL, " -");
	}

	return StringTable->insert(buffer);
}

bool GuiMenuItemCtrl::onKeyDown(const GuiEvent &event)
{
	if (mOpenSubMenu != NULL && (mOpenSubMenu->mList->mHoveredItem != NULL || mOpenSubMenu->mOpenSubMenu != NULL))
	{
		return mOpenSubMenu->onKeyDown(event);
	}
	else if (event.keyCode == KEY_DOWN && event.modifier == 0)
	{
		mMenuBar->mUseKeyMode = true;
		if (mList->mHoveredItem == NULL || mList->mHoveredItem->mNextItem == NULL)
		{
			mList->setHoveredItem(dynamic_cast<GuiMenuItemCtrl *>(first()));
			if (!mList->mHoveredItem->isVisible() || !mList->mHoveredItem->isActive() || !mList->mHoveredItem->mHasGoodChildren)
			{
				onKeyDown(event);
			}
		}
		else if(mList->mHoveredItem->mNextItem->mDisplayType == DisplayType::Spacer || !mList->mHoveredItem->mNextItem->isVisible() || !mList->mHoveredItem->mNextItem->isActive() || !mList->mHoveredItem->mNextItem->mHasGoodChildren)
		{
			//To skip the spacer and still do all the needed checks, we'll move the hovered item to the next item and simulate a second down button push
			mList->setHoveredItem(mList->mHoveredItem->mNextItem);
			onKeyDown(event);
		}
		else
		{
			mList->setHoveredItem(mList->mHoveredItem->mNextItem);
		}
		return true;
	}
	else if (event.keyCode == KEY_UP && event.modifier == 0)
	{
		mMenuBar->mUseKeyMode = true;
		if (mList->mHoveredItem == NULL || mList->mHoveredItem->mPrevItem == NULL)
		{
			mList->setHoveredItem(dynamic_cast<GuiMenuItemCtrl *>(last()));
			if (!mList->mHoveredItem->isVisible() || !mList->mHoveredItem->isActive() || !mList->mHoveredItem->mHasGoodChildren)
			{
				onKeyDown(event);
			}
		}
		else if (mList->mHoveredItem->mPrevItem->mDisplayType == DisplayType::Spacer || !mList->mHoveredItem->mPrevItem->isVisible() || !mList->mHoveredItem->mPrevItem->isActive() || !mList->mHoveredItem->mPrevItem->mHasGoodChildren)
		{
			//To skip the spacer and still do all the needed checks, we'll move the hovered item to the prev item and simulate a second up button push
			mList->setHoveredItem(mList->mHoveredItem->mPrevItem);
			onKeyDown(event);
		}
		else
		{
			mList->setHoveredItem(mList->mHoveredItem->mPrevItem);
		}
		return true;
	}
	else if (event.keyCode == KEY_RIGHT && event.modifier == 0)
	{
		mMenuBar->mUseKeyMode = true;
		if (mList->mHoveredItem == NULL)
		{
			//loop to find the first valid menu item
			GuiMenuItemCtrl *foundCtrl = NULL;
			iterator iter;
			for (iter = begin(); iter != end(); iter++)
			{
				GuiMenuItemCtrl *ctrl = static_cast<GuiMenuItemCtrl *>(*iter);
				if (ctrl->isVisible() && ctrl->isActive() && ctrl->mDisplayType != Spacer && ctrl->mHasGoodChildren )
				{
					foundCtrl = ctrl;
					break;
				}
			}
			if (foundCtrl != NULL)
			{
				mList->setHoveredItem(foundCtrl);
			}
		}
		else if (mList->mHoveredItem == mOpenSubMenu)
		{
			mOpenSubMenu->onKeyDown(event);
			mList->mHoveredItem = NULL;
		}
		else if(mList->mHoveredItem->mDisplayType == DisplayType::Menu && mList->mHoveredItem->isVisible() && mList->mHoveredItem->isActive() && mList->mHoveredItem->mHasGoodChildren)
		{
			mList->processMenuItem(mList->mHoveredItem);
			mOpenSubMenu->onKeyDown(event);
			mList->mHoveredItem = NULL;
		}
		return true;
	}
	else if (event.keyCode == KEY_LEFT && event.modifier == 0)
	{
		mMenuBar->mUseKeyMode = true;

		//Is this the open sub menu?
		GuiMenuItemCtrl *menu = dynamic_cast<GuiMenuItemCtrl *>(getParent());
		if (menu != NULL && menu->mOpenSubMenu == this)
		{
			menu->mList->mHoveredItem = menu->mOpenSubMenu;
			closeMenu();
			menu->mOpenSubMenu = NULL;
		}
		return true;
	}
	else if (event.keyCode == KEY_RETURN && event.modifier == 0)
	{
		mMenuBar->mUseKeyMode = true;
		if (mList->mHoveredItem == NULL)
		{
			mList->setHoveredItem(dynamic_cast<GuiMenuItemCtrl *>(first()));
		}
		else
		{
			mList->processMenuItem(mList->mHoveredItem);
		}
		return true;
	}

	// Any other key belongs to whoever asked next - matching GuiMenuBarCtrl's own
	// onKeyDown. Falling off the end here returned whatever happened to be in the
	// return register, for every ordinary letter and for any of the five keys
	// above arriving with a modifier.
	return false;
}

void GuiMenuItemCtrl::setMenuActive(StringTableEntry name, bool isActive)
{
	iterator i;
	for (i = begin(); i != end(); i++)
	{
		GuiMenuItemCtrl* ctrl = static_cast<GuiMenuItemCtrl*>(*i);
		StringTableEntry ctrlName = StringTable->insert(ctrl->getText(), true);
		if (ctrlName == name)
		{
			ctrl->setActive(isActive);
		}
		ctrl->setMenuActive(name, isActive);
	}
}
#pragma endregion

#pragma region GuiMenuBGCtrl
GuiMenuBGCtrl::GuiMenuBGCtrl(GuiMenuBarCtrl *ctrl)
{
	mMenuBarCtrl = ctrl;
	mBounds.point.set(0, 0);
	setField("profile", "GuiDefaultProfile");
}

void GuiMenuBGCtrl::openMenu()
{
	GuiMenuItemCtrl *menu = mMenuBarCtrl->mOpenMenu;
	if (menu == NULL)
	{
		return;
	}

	positionMenu(menu, Point2I(menu->mBounds.point.x, menu->mBounds.point.y + menu->mBounds.extent.y));

	addObject(menu->mScroll);
}

void GuiMenuBGCtrl::closeMenu()
{
	mMenuBarCtrl->closeMenu();
}

void GuiMenuBGCtrl::openSubMenu(GuiMenuItemCtrl *subMenu)
{
	if (subMenu == NULL)
	{
		return;
	}

	GuiMenuItemCtrl *menu = dynamic_cast<GuiMenuItemCtrl *>(subMenu->getParent());
	if (subMenu == menu->mOpenSubMenu)
	{
		return;
	}

	if(menu->mOpenSubMenu != NULL)
	{
		menu->mOpenSubMenu->closeMenu();
	}
	menu->mOpenSubMenu = subMenu;
	subMenu->mIsOpen = true;

	positionMenu(subMenu, Point2I(subMenu->mBounds.point.x + subMenu->mBounds.extent.x - 4, subMenu->mBounds.point.y - 4));

	addObject(subMenu->mScroll);

	menu->mList->mArmSubMenu = false;
}

void GuiMenuBGCtrl::onTouchMove(const GuiEvent &event)
{
	processHover(event);
}

void GuiMenuBGCtrl::onTouchDragged(const GuiEvent &event)
{
	processHover(event);
}

void GuiMenuBGCtrl::onTouchDown(const GuiEvent &event)
{
	mMenuBarCtrl->closeMenu();
}

void GuiMenuBGCtrl::processHover(const GuiEvent &event)
{
	if (mMenuBarCtrl->pointInControl(event.mousePoint))
	{
		GuiMenuItemCtrl *menu = mMenuBarCtrl->findHitMenu(globalToLocalCoord(event.mousePoint));
		if (menu != NULL && mMenuBarCtrl->mOpenMenu != menu)
		{
			mMenuBarCtrl->mOpenMenu->closeMenu();
			mMenuBarCtrl->mOpenMenu = menu;
			mMenuBarCtrl->mOpenMenu->mIsOpen = true;
			openMenu();
			setUpdate();
		}
	}
	mMenuBarCtrl->mUseKeyMode = false;
}

void GuiMenuBGCtrl::positionMenu(const GuiMenuItemCtrl *menu, const Point2I &topLeft)
{
	menu->mList->updateSize();
	Point2I menuSize = menu->mList->mBounds.extent;
	RectI box = RectI(topLeft, menuSize);
	box.extent = getOuterExtent(box.extent, NormalState, menu->mScroll->mProfile);

	//Is it too tall?
	if ((box.point.y + box.extent.y) > mBounds.extent.y)
	{
		//can we move it up?
		if ((mMenuBarCtrl->mBounds.point.y + mMenuBarCtrl->mBounds.extent.y) < box.point.y)
		{
			box.point.y = getMax(mMenuBarCtrl->mBounds.point.y + mMenuBarCtrl->mBounds.extent.y, mBounds.extent.y - box.extent.y);
		}

		//Is it still too tall?
		if ((box.point.y + box.extent.y) > mBounds.extent.y)
		{
			box.extent.y = mBounds.extent.y - box.point.y;
			box.extent.x += menu->mScroll->mScrollBarThickness;
		}
	}

	//Is it too far to the right?
	if ((box.point.x + box.extent.x) > mBounds.extent.x)
	{
		box.point.x = mBounds.extent.x - box.extent.x;
	}

	menu->mScroll->mBounds.extent = box.extent;
	menu->mScroll->mBounds.point = box.point;
}

#pragma endregion

#pragma region GuiMenuListCtrl
GuiMenuListCtrl::GuiMenuListCtrl(GuiMenuItemCtrl *ctrl)
{
	mMenu = ctrl;
	mBounds.point.set(0,0);
	mHoveredItem = NULL;
	mEnterItemTime = 0;
	mSubMenuStallTime = 500;
}

void GuiMenuListCtrl::onTouchMove(const GuiEvent &event)
{
	Parent::onTouchMove(event);
	mMenu->mMenuBar->mUseKeyMode = false;
}

GuiMenuItemCtrl* GuiMenuListCtrl::GetHitItem(const Point2I &pt)
{
	iterator iter;
	for (iter = mMenu->begin(); iter != mMenu->end(); iter++)
	{
		GuiMenuItemCtrl *ctrl = static_cast<GuiMenuItemCtrl *>(*iter);
		if (!ctrl->mVisible) continue;

		if (ctrl->pointInControl(pt))
		{
			return ctrl;
		}
	}
	return nullptr;
}

void GuiMenuListCtrl::onPreRender()
{
	U32 currentTime = Platform::getVirtualMilliseconds();
	if (mArmSubMenu && mHoveredItem != NULL && mHoveredItem != mMenu->mOpenSubMenu && (currentTime - mEnterItemTime) > mSubMenuStallTime)
	{
		//Close an open sub menu
		if (mMenu->mOpenSubMenu != NULL)
		{
			mMenu->mOpenSubMenu->closeMenu();
			mMenu->mOpenSubMenu = NULL;
		}

		//Open a sub menu
		if (mHoveredItem->mDisplayType == GuiMenuItemCtrl::DisplayType::Menu)
		{
			processMenuItem(mHoveredItem);
		}
	}
}

void GuiMenuListCtrl::onRender(Point2I offset, const RectI &updateRect)
{
	bool foundHoveredItem = false;
	iterator iter;
	S32 h = 0;
	for (iter = mMenu->begin(); iter != mMenu->end(); iter++)
	{
		GuiMenuItemCtrl *ctrl = static_cast<GuiMenuItemCtrl *>(*iter);

		if (!ctrl->mVisible) continue;

		S32 top = h;
		S32 bottom = h;
		if (ctrl->mDisplayType == GuiMenuItemCtrl::DisplayType::Spacer)
		{
			 bottom += mSpacerSize.y;
		}
		else
		{
			bottom += mItemSize.y;
		}

		// Only render visible items
		if (bottom + offset.y < updateRect.point.y)
			continue;

		// Break out once we're no longer in visible item range
		if (top + offset.y >= updateRect.point.y + updateRect.extent.y)
			break;

		RectI itemRect;
		if (ctrl->mDisplayType == GuiMenuItemCtrl::DisplayType::Spacer)
		{
			itemRect = RectI(offset.x, offset.y + top, mSpacerSize.x, mSpacerSize.y);
		}
		else
		{
			itemRect = RectI(offset.x, offset.y + top, mItemSize.x, mItemSize.y);
		}

		// Render our item
		foundHoveredItem |= onRenderItem(itemRect, ctrl);

		h = bottom;
	}

	if (!foundHoveredItem)
	{
		mHoveredItem = NULL;
	}
}

bool GuiMenuListCtrl::onRenderItem(RectI &itemRect, GuiMenuItemCtrl *item)
{
	GuiControlProfile *profile = item->mProfile;
	if (!profile)
		return false;

	if (item->mBounds != itemRect)
	{
		item->mBounds = itemRect;
	}

	Point2I cursorPt = Point2I(0, 0);
	GuiCanvas *root = getRoot();
	if (root)
	{
		cursorPt = root->getCursorPos();
	}

	//is this item being hovered?
	bool foundHoveredItem = false;
	if (mMenu->mMenuBar->mUseKeyMode && mHoveredItem == item)
	{
		foundHoveredItem = true;
	}
	else if (!mMenu->mMenuBar->mUseKeyMode && itemRect.pointInRect(cursorPt) && (mMenu->mOpenSubMenu == NULL || !mMenu->mOpenSubMenu->mScroll->mBounds.pointInRect(cursorPt)))
	{
		if (mHoveredItem != item)
		{
			setHoveredItem(item);
		}
		foundHoveredItem = true;
	}

	//Get the current state
	GuiControlState currentState = GuiControlState::NormalState;
	if (!item->mActive || !item->mHasGoodChildren)
		currentState = GuiControlState::DisabledState;
	else if (item->mDisplayType == GuiMenuItemCtrl::DisplayType::Spacer)
		currentState = GuiControlState::SelectedState;
	else if (mHoveredItem == item ||
		(item->mIsOpen && (mMenu->mMenuBar->mUseKeyMode || !mMenu->mScroll->mBounds.pointInRect(cursorPt) || mMenu->mOpenSubMenu->mScroll->mBounds.pointInRect(cursorPt))))
		currentState = GuiControlState::HighlightState;
	RectI ctrlRect = applyMargins(itemRect.point, itemRect.extent, currentState, profile);

	if (!ctrlRect.isValidRect())
	{
		return foundHoveredItem;
	}

	renderUniversalRect(ctrlRect, profile, currentState);

	//Render Text
	dglSetBitmapModulation(profile->getFontColor(currentState));
	RectI fillRect = applyBorders(ctrlRect.point, ctrlRect.extent, currentState, profile);
	RectI contentRect = applyPadding(fillRect.point, fillRect.extent, currentState, profile);

	if(contentRect.isValidRect())
	{
		//Left Icon
		RectI leftIconRect = RectI(itemRect.point, Point2I(itemRect.extent.y, itemRect.extent.y));
		if(item->mDisplayType == GuiMenuItemCtrl::DisplayType::Toggle)
		{
			ColorI itemColor = item->mIsOn ? profile->getFillColor(HighlightState) : profile->getFillColor(NormalState);
			S32 size = profile->getFont(mFontSizeAdjust)->getHeight();
			renderColorBullet(leftIconRect, itemColor, getMin(size, 16));
		}
		else if (item->mDisplayType == GuiMenuItemCtrl::DisplayType::Radio)
		{
			ColorI itemColor = item->mIsOn ? profile->getFillColor(HighlightState) : profile->getFillColor(NormalState);
			S32 size = profile->getFont(mFontSizeAdjust)->getHeight();
			renderColorBullet(leftIconRect, itemColor, getMin(size, 16), true);
		}

		//Right Icon
		RectI rightIconRect = RectI(Point2I(itemRect.point.x + itemRect.extent.x - itemRect.extent.y, itemRect.point.y), leftIconRect.extent);
		if (item->mDisplayType == GuiMenuItemCtrl::DisplayType::Menu)
		{
			S32 size = (profile->getFont(mFontSizeAdjust)->getHeight() / 2);
			rightIconRect.inset(2, 0);
			ColorI color = ColorI(profile->getFontColor(currentState));
			renderTriangleIcon(rightIconRect, color, GuiDirection::Right, size);
		}

		//Text Space
		RectI textRect = RectI(itemRect.point.x + itemRect.extent.y, itemRect.point.y, itemRect.extent.x - (2 * itemRect.extent.y), itemRect.extent.y);

		//Command Description
		profile->mAlignment = AlignmentType::LeftAlign;
		renderText(textRect.point, textRect.extent, item->getText(), profile);

		//Hot Keys!!
		profile->mAlignment = AlignmentType::RightAlign;
		renderText(textRect.point, textRect.extent, item->getHotKeyText(), profile);
	}

	return foundHoveredItem;
}

void GuiMenuListCtrl::updateSize()
{
	if(mMenu->size() == 0)
	{
		resize(mBounds.extent, Point2I(100, 20));
		return;
	}

	GuiMenuItemCtrl *child = dynamic_cast<GuiMenuItemCtrl *>(mMenu->first());
	if (!child)
		return;

	GuiControlProfile *profile = child->mProfile;
	if (!profile)
		return;

	GFont *font = profile->getFont(mFontSizeAdjust);
	Point2I contentSize = Point2I(10, font->getHeight() + 2);
	Point2I spacerSize = Point2I(10, 0);

	// Find the maximum width cell:
	mItemCount = 0;
	mSpacerCount = 0;
	S32 maxWidth = 1;
	iterator i;
	for (i = mMenu->begin(); i != mMenu->end(); i++)
	{
		GuiMenuItemCtrl *ctrl = static_cast<GuiMenuItemCtrl *>(*i);

		if(!ctrl->mVisible) continue;

		S32 width = font->getStrWidth(ctrl->getText());
		if(ctrl->mDisplayType != GuiMenuItemCtrl::DisplayType::Menu && ctrl->mDisplayType != GuiMenuItemCtrl::DisplayType::Spacer)
		{
			StringTableEntry hotKey = ctrl->getHotKeyText();
			if(hotKey != StringTable->EmptyString)
			{
				width += font->getStrWidth(hotKey) + 30;
			}
		}

		if (width > maxWidth)
			maxWidth = width;

		if (ctrl->mDisplayType == GuiMenuItemCtrl::DisplayType::Spacer)
		{
			mSpacerCount++;
		}
		else
		{
			mItemCount++;
		}
	}
	contentSize.x = maxWidth;
	spacerSize.x = contentSize.x;

	mItemSize = this->getOuterExtent(contentSize, NormalState, profile);
	mSpacerSize = this->getOuterExtent(spacerSize, SelectedState, profile);

	//Add a square of space at both ends for icons
	mItemSize.x += (2 * mItemSize.y);
	mSpacerSize.x += (2 * mItemSize.y);

	Point2I newExtent = Point2I(mItemSize.x, (mItemSize.y * mItemCount) + (mSpacerSize.y * mSpacerCount));

	resize(mBounds.point, newExtent);
}

void GuiMenuListCtrl::onTouchDown(const GuiEvent &) { }

void GuiMenuListCtrl::onTouchUp(const GuiEvent &event)
{
	GuiMenuItemCtrl *ctrl = GetHitItem(event.mousePoint);
	if (ctrl != nullptr )
	{
		processMenuItem(ctrl);
	}
}

void GuiMenuListCtrl::processMenuItem(GuiMenuItemCtrl *ctrl)
{
	if(!ctrl->mActive || ctrl->mDisplayType == GuiMenuItemCtrl::DisplayType::Spacer || !ctrl->mHasGoodChildren)
		return;

	ctrl->onAction();

	GuiScrollCtrl* parent = dynamic_cast<GuiScrollCtrl *>(getParent());
	if (parent)
	{
		GuiMenuBGCtrl* grandParent = dynamic_cast<GuiMenuBGCtrl *>(parent->getParent());
		if (grandParent)
		{
			if (ctrl->mDisplayType != GuiMenuItemCtrl::DisplayType::Menu)
			{
				grandParent->closeMenu();
			}
			else
			{
				grandParent->openSubMenu(ctrl);

			}
		}
	}
}

#pragma endregion
