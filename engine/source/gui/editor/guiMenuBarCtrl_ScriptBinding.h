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

ConsoleMethodGroupBeginWithDocs(GuiMenuBarCtrl, GuiControl)

/*! Sets the currently used BackgroundProfile for the GuiControl
	@param p The BackgroundProfile applies to the the entire screen behind the open menu.
	@return No return value
*/
ConsoleMethodWithDocs(GuiMenuBarCtrl, setBackgroundProfile, ConsoleVoid, 3, 3, (GuiControlProfile p))
{
	GuiControlProfile* profile;

	if (Sim::findObject(argv[2], profile))
		object->setControlBackgroundProfile(profile);
}

/*! Sets the currently used MenuProfile for the GuiControl
	@param p The MenuProfile is applied to each top level menu item.
	@return No return value
*/
ConsoleMethodWithDocs(GuiMenuBarCtrl, setMenuProfile, ConsoleVoid, 3, 3, (GuiControlProfile p))
{
	GuiControlProfile* profile;

	if (Sim::findObject(argv[2], profile))
		object->setControlMenuProfile(profile);
}

/*! Sets the currently used MenuItemProfile for the GuiControl
	@param p The MenuItemProfile is applied to each menu item.
	@return No return value
*/
ConsoleMethodWithDocs(GuiMenuBarCtrl, setMenuItemProfile, ConsoleVoid, 3, 3, (GuiControlProfile p))
{
	GuiControlProfile* profile;

	if (Sim::findObject(argv[2], profile))
		object->setControlMenuItemProfile(profile);
}

/*! Sets the currently used MenuContentProfile for the GuiControl
	@param p The MenuContentProfile is applied the menu box that opens.
	@return No return value
*/
ConsoleMethodWithDocs(GuiMenuBarCtrl, setMenuContentProfile, ConsoleVoid, 3, 3, (GuiControlProfile p))
{
	GuiControlProfile* profile;

	if (Sim::findObject(argv[2], profile))
		object->setControlMenuContentProfile(profile);
}

/*! Sets the currently used ThumbProfile for the GuiControl
	@param p The ThumbProfile is applied to the thumb of the scrollbar.
	@return No return value
*/
ConsoleMethodWithDocs(GuiMenuBarCtrl, setThumbProfile, ConsoleVoid, 3, 3, (GuiControlProfile p))
{
	GuiControlProfile* profile;

	if (Sim::findObject(argv[2], profile))
		object->setControlThumbProfile(profile);
}

/*! Sets the currently used TrackProfile for the GuiControl
	@param p The TrackProfile is applied to the track area used by the scroll thumb.
	@return No return value
*/
ConsoleMethodWithDocs(GuiMenuBarCtrl, setTrackProfile, ConsoleVoid, 3, 3, (GuiControlProfile p))
{
	GuiControlProfile* profile;

	if (Sim::findObject(argv[2], profile))
		object->setControlTrackProfile(profile);
}

/*! Sets the currently used ArrowProfile for the GuiControl
	@param p The ArrowProfile is applied to the arrow buttons of the scrollbar if you turn them on.
	@return No return value
*/
ConsoleMethodWithDocs(GuiMenuBarCtrl, setArrowProfile, ConsoleVoid, 3, 3, (GuiControlProfile p))
{
	GuiControlProfile* profile;

	if (Sim::findObject(argv[2], profile))
		object->setControlArrowProfile(profile);
}

/*! Sets a menu item to active or inactive based on a name.
	@param menuName The name of the menu that should be enabled or disabled. If multiple menu items have the same name they will all be enabled or disabled. Case sensative.
	@param isActive True if the menu should be enabled. False if it is disabled.
	@return No return value.
*/
ConsoleMethodWithDocs(GuiMenuBarCtrl, setMenuActive, ConsoleVoid, 4, 4, ("string menuName, bool isActive"))
{
	if (argc != 4)
	{
		Con::warnf("GuiMenuBarCtrl::setMenuActive() - Invalid number of parameters! Should be (string menuName, bool isActive).");
		return;
	}

	object->setMenuActive(argv[2], dAtob(argv[3]));
}

/*! Returns the bounds of the editor-only "+" as "x y width height", in global
	coordinates.

	Empty - "0 0 0 0" - unless the bar is inside the Gui being authored, which is
	the only time the "+" is drawn. A bar with no menus at all still reports one;
	that is what stops an emptied bar from being unrecoverable, now that the
	control palette does not offer a GuiMenuItemCtrl.
	@return The rectangle the "+" occupies on screen.
*/
ConsoleMethodWithDocs(GuiMenuBarCtrl, getAddItemRect, ConsoleString, 2, 2, ())
{
	RectI rect = object->getAddItemGlobalRect();

	char* buffer = Con::getReturnBuffer(64);
	dSprintf(buffer, 64, "%d %d %d %d", rect.point.x, rect.point.y, rect.extent.x, rect.extent.y);

	return buffer;
}

/*! Returns the bounds of the "+" row at the foot of the open dropdown, as
	"x y width height" in global coordinates.

	Empty unless the bar is being authored AND one of its menus is selected -
	that is what decides which dropdown is showing. A menu with no commands in it
	at all still reports one; that row is the only way a command ever gets made.
	@return The rectangle the dropdown's "+" occupies on screen.
*/
ConsoleMethodWithDocs(GuiMenuBarCtrl, getAddSubItemRect, ConsoleString, 2, 2, ())
{
	RectI rect = object->getAddSubItemGlobalRect();

	char* buffer = Con::getReturnBuffer(64);
	dSprintf(buffer, 64, "%d %d %d %d", rect.point.x, rect.point.y, rect.extent.x, rect.extent.y);

	return buffer;
}

ConsoleMethodGroupEndWithDocs(GuiMenuBarCtrl)