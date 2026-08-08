//-----------------------------------------------------------------------------
// Authoring a GuiMenuBarCtrl in the Gui Editor.
//
// A menu item is not something the control palette offers - it means nothing
// outside a bar - so before the "+" there was no way whatsoever to put anything
// into a menu bar you dropped. Now the bar makes its own: one when it is
// dropped, one for every click on the "+" after the last menu, and one for every
// click on the "+" at the foot of an open menu.
//
// Menus nest, which is the whole difficulty. This checks both levels, that the
// dropdown follows the SELECTION rather than a toggle of its own, that a menu
// with nothing in it still offers the row that fills it, and that an item cannot
// be dragged, dropped or pasted anywhere but into a bar or another item.
//
// Runs on the real editor UI throughout, because all of it is gated on
// isEditMode().
//
// Run: tests/run.ps1 menuBar ; grep MENUBAR in console.log.
//-----------------------------------------------------------------------------

setLogMode(2);
setScriptExecEcho(false);
trace(false);
$Scripts::ignoreDSOs = true;
setCompanyAndProduct("Torque Game Engines", "Torque2D");
ModuleDatabase.EchoInfo = false;
AssetDatabase.EchoInfo = false;
AssetDatabase.IgnoreAutoUnload = true;

testExec("editor/main.cs");

function mbCheck(%label, %condition)
{
	echo(%condition ? ("MENUBAR PASS: " @ %label) : ("MENUBAR FAIL: " @ %label));
}

function mbUndoCount()
{
	return GuiEditor.undoRecorder.undoCount();
}

function mbSelect(%ctrl)
{
	GuiEditor.brain.clearSelection();
	GuiEditor.brain.select(%ctrl);
}

// Is the inner rect wholly inside the outer one? Both are "x y width height".
function mbInside(%inner, %outer)
{
	return getWord(%inner, 0) >= getWord(%outer, 0) &&
		getWord(%inner, 1) >= getWord(%outer, 1) &&
		(getWord(%inner, 0) + getWord(%inner, 2)) <= (getWord(%outer, 0) + getWord(%outer, 2)) &&
		(getWord(%inner, 1) + getWord(%inner, 3)) <= (getWord(%outer, 1) + getWord(%outer, 3));
}

schedule(2000, 0, "mbStep1");

function mbStep1()
{
	ProjectManager.setProjectFolder("PlanetX");
	ModuleDatabase.ScanModules("PlanetX");
	ModuleDatabase.LoadExplicit("AppCore", 1);

	// By id: a bare identifier in TorqueScript is a string, and everything here
	// compares object handles.
	$mbTheme = nameToID("PlanetX");
	mbCheck("PlanetX theme loaded", isObject($mbTheme));

	GuiEditor.open();
	GuiEditor.setTheme($mbTheme, false);

	EditorCore.open();
	EditorCore.tabBook.selectPageName("Gui Editor");

	schedule(800, 0, "mbStepPalette");
}

//-----------------------------------------------------------------------------
// The palette, which has refused menu items all along.
//-----------------------------------------------------------------------------

function mbStepPalette()
{
	%icons = GuiEditor.controlIcons;

	mbCheck("the palette will not place a menu item", !%icons.isPlaceableClass("GuiMenuItemCtrl"));
	mbCheck("but a menu bar is still offered",
		strstr(%icons.keysInGroup("Advanced"), "GuiMenuBarCtrl") != -1);

	// Unlike GuiTabPageCtrl, which keeps its icon row and only loses its tile, a
	// menu item has no row in the table at all - so refusedNames is the only
	// thing standing between it and the sweep over the class registry.
	mbCheck("a menu item has no icon row to lose", !%icons.isKnown("GuiMenuItemCtrl"));
	mbCheck("and is not covered by any entry", !%icons.coversClass("GuiMenuItemCtrl"));

	schedule(300, 0, "mbStepDrop");
}

//-----------------------------------------------------------------------------
// Dropping a bar, which has to arrive with a menu in it.
//-----------------------------------------------------------------------------

function mbStepDrop()
{
	GuiEditor.undoRecorder.clear();
	GuiEditor.brain.setCurrentAddSet(GuiEditor.rootGui);

	// Through placeControl, which is what clicking a palette tile does. NOT
	// onControlDropped: that opens with a cursor test a menu bar can never pass.
	// A bar owns nothing but its height - GuiMenuBarCtrl::resize throws away the
	// position it is handed - so its payload sits at 0,0 whatever it is told, and
	// the middle of a 300x30 rectangle at 0,0 is up in the editor's own chrome.
	$mbBar = new GuiMenuBarCtrl() { Extent = "300 30"; };
	$mbBar.Position = GuiEditor.brain.centredPlacement($mbBar);
	GuiEditor.brain.placeControl($mbBar);

	mbCheck("the bar arrived", $mbBar.getParent() == GuiEditor.rootGui);
	mbCheck("a bar refuses the position it was given (" @ $mbBar.getPosition() @ ")",
		$mbBar.getPosition() $= "0 0");
	mbCheck("carrying exactly one menu (" @ $mbBar.getCount() @ ")", $mbBar.getCount() == 1);

	$mbMenu1 = $mbBar.getObject(0);
	mbCheck("which is a menu item", $mbMenu1.getClassName() $= "GuiMenuItemCtrl");
	mbCheck("captioned \"" @ $mbMenu1.getText() @ "\"", $mbMenu1.getText() $= "Menu 1");

	// The bar's own profile, and the two slots it dresses its items out of.
	//
	// Not the item's own Profile: GuiMenuItemCtrl calls SimObject's
	// initPersistFields rather than GuiControl's, so it has no Profile FIELD at
	// all - which is the same reason the properties pane calls it "bare". Its
	// profile is assigned in C++ from these slots (onChildAdded does
	// setControlProfile(mMenuProfile)), so these are the thing worth checking.
	mbCheck("the bar was themed on arrival (" @ $mbBar.Profile.category @ ")",
		$mbBar.Profile.category $= "MenuBar");
	mbCheck("its menu slot was themed (" @ $mbBar.MenuProfile.category @ ")",
		$mbBar.MenuProfile.category $= "Menu");
	mbCheck("and its menu-item slot (" @ $mbBar.MenuItemProfile.category @ ")",
		$mbBar.MenuItemProfile.category $= "MenuItem");

	mbCheck("the whole thing is one undo step (" @ mbUndoCount() @ ")", mbUndoCount() == 1);

	// After the drop's own schedule(40), so the undo is not racing it.
	schedule(200, 0, "mbStepDropUndo");
}

function mbStepDropUndo()
{
	%trash = GuiEditor.brain.getTrash();

	GuiEditor.Undo();

	// getGroup, not getParent: a control sitting in the trash - a plain SimGroup
	// - reads as having no parent at all.
	mbCheck("undo took the bar out of the Gui", $mbBar.getGroup() == %trash);
	mbCheck("with its menu still inside it", $mbMenu1.getParent() == $mbBar);

	GuiEditor.Redo();
	mbCheck("redo put the bar back", $mbBar.getParent() == GuiEditor.rootGui);
	mbCheck("still holding its menu (" @ $mbBar.getCount() @ ")", $mbBar.getCount() == 1);

	schedule(300, 0, "mbStepTopLevel");
}

//-----------------------------------------------------------------------------
// The bar's "+", which asks GuiEditorBrain::onAddMenuItem for a top-level menu.
//-----------------------------------------------------------------------------

function mbStepTopLevel()
{
	GuiEditor.undoRecorder.clear();

	GuiEditor.brain.onAddMenuItem($mbBar, "");

	mbCheck("the bar grew a menu (" @ $mbBar.getCount() @ ")", $mbBar.getCount() == 2);

	$mbMenu2 = $mbBar.getObject(1);
	mbCheck("numbered on from the last (" @ $mbMenu2.getText() @ ")", $mbMenu2.getText() $= "Menu 2");
	mbCheck("the new menu is selected", GuiEditor.brain.selectionList() $= $mbMenu2);
	mbCheck("and is where the next control would land",
		GuiEditor.brain.getCurrentAddSet() == $mbMenu2);

	mbCheck("adding a menu is one step (" @ mbUndoCount() @ ")", mbUndoCount() == 1);

	GuiEditor.Undo();
	mbCheck("undo took it back off (" @ $mbBar.getCount() @ ")", $mbBar.getCount() == 1);
	GuiEditor.Redo();
	mbCheck("redo put it back (" @ $mbBar.getCount() @ ")", $mbBar.getCount() == 2);

	schedule(300, 0, "mbStepNested");
}

//-----------------------------------------------------------------------------
// The dropdown's "+", which is the same call with a parent named.
//-----------------------------------------------------------------------------

function mbStepNested()
{
	GuiEditor.undoRecorder.clear();

	GuiEditor.brain.onAddMenuItem($mbBar, $mbMenu1);

	mbCheck("the menu grew a command (" @ $mbMenu1.getCount() @ ")", $mbMenu1.getCount() == 1);
	mbCheck("the bar did not (" @ $mbBar.getCount() @ ")", $mbBar.getCount() == 2);

	%command = $mbMenu1.getObject(0);

	// Numbering is per parent, so a menu's commands start from 1 rather than
	// carrying on from the bar's count.
	mbCheck("numbered from 1 inside its own menu (" @ %command.getText() @ ")",
		%command.getText() $= "Menu 1");
	mbCheck("adding a command is one step (" @ mbUndoCount() @ ")", mbUndoCount() == 1);

	GuiEditor.brain.onAddMenuItem($mbBar, $mbMenu1);
	mbCheck("a second command counts on (" @ $mbMenu1.getObject(1).getText() @ ")",
		$mbMenu1.getObject(1).getText() $= "Menu 2");

	schedule(300, 0, "mbStepDropdown");
}

//-----------------------------------------------------------------------------
// Which dropdown is showing, and where its "+" row is. Derived from the
// selection rather than toggled, so the Explorer tree opens it too.
//-----------------------------------------------------------------------------

function mbStepDropdown()
{
	%bar = mbGlobalRect($mbBar);

	mbCheck("the bar reports a \"+\" (" @ $mbBar.getAddItemRect() @ ")",
		getWord($mbBar.getAddItemRect(), 2) > 0);
	mbCheck("square, as an affordance rather than a menu",
		getWord($mbBar.getAddItemRect(), 2) == getWord($mbBar.getAddItemRect(), 3));
	mbCheck("inside the bar it belongs to", mbInside($mbBar.getAddItemRect(), %bar));
	mbCheck("after the menus rather than before them",
		getWord($mbBar.getAddItemRect(), 0) > getWord(%bar, 0));

	// Nothing selected, no dropdown.
	GuiEditor.brain.clearSelection();
	mbCheck("nothing selected means no dropdown (" @ $mbBar.getAddSubItemRect() @ ")",
		getWord($mbBar.getAddSubItemRect(), 2) == 0);

	// The menu itself.
	mbSelect($mbMenu1);
	%row = $mbBar.getAddSubItemRect();
	mbCheck("selecting a menu opens it (" @ %row @ ")", getWord(%row, 2) > 0);
	mbCheck("and its \"+\" row sits below the bar",
		getWord(%row, 1) >= (getWord(%bar, 1) + getWord(%bar, 3)));

	// A command inside it keeps it open, which is what makes the dropdown usable
	// at all: selecting the row you just made must not close the menu.
	mbSelect($mbMenu1.getObject(0));
	mbCheck("selecting a command keeps it open (" @ $mbBar.getAddSubItemRect() @ ")",
		$mbBar.getAddSubItemRect() $= %row);

	// Another menu switches it.
	mbSelect($mbMenu2);
	mbCheck("selecting another menu switches the dropdown",
		$mbBar.getAddSubItemRect() !$= %row &&
		getWord($mbBar.getAddSubItemRect(), 2) > 0);

	// Menu 2 has nothing in it, and that is the case that matters: a menu the
	// "+" just made has no GuiMenuListCtrl at all, so the runtime machinery could
	// not draw this even if it were open.
	mbCheck("an empty menu still offers a \"+\" row (" @ $mbMenu2.getCount() @ " children)",
		$mbMenu2.getCount() == 0 && getWord($mbBar.getAddSubItemRect(), 2) > 0);

	// A command's own bounds are the row it is drawn in, so everything that asks
	// a control where it is - the editor's selection outline most of all - gets
	// the answer the user can see. Before this they were the 64x64 a GuiControl
	// is constructed with, and the outline appeared nowhere near the row.
	mbSelect($mbMenu1);
	%command = $mbMenu1.getObject(0);

	// The dropdown is laid out in onPreRender, or on demand by either of the two
	// geometry accessors. Asking for the "+" row first is what makes the rows
	// current for a selection that has not been drawn yet - without it these read
	// whatever the previously open menu left behind.
	%addRow = $mbBar.getAddSubItemRect();
	%row = %command.getGlobalPosition() SPC %command.getExtent();

	mbCheck("a command is not still 64x64 (" @ %command.getExtent() @ ")",
		%command.getExtent() !$= "64 64");
	mbCheck("it lines up with the \"+\" row below it (" @ %row @ " vs " @ %addRow @ ")",
		getWord(%row, 0) == getWord(%addRow, 0) && getWord(%row, 2) == getWord(%addRow, 2));
	mbCheck("and sits inside the bar's dropdown",
		getWord(%row, 1) < getWord(%addRow, 1));

	// The bar itself is not one of its own menus.
	mbSelect($mbBar);
	mbCheck("selecting the bar closes the dropdown (" @ $mbBar.getAddSubItemRect() @ ")",
		getWord($mbBar.getAddSubItemRect(), 2) == 0);

	schedule(300, 0, "mbStepCaption");
}

//-----------------------------------------------------------------------------
// A top-level menu is exactly as wide as its caption, so the strip has to be
// re-laid whenever the text moves - including on every keystroke, which is what
// the properties pane does while a caption is being typed.
//-----------------------------------------------------------------------------

function mbStepCaption()
{
	%menu = $mbBar.getObject(0);
	%wide = getWord(%menu.getExtent(), 0);

	// A plain field assignment, which is exactly what the pane's per-keystroke
	// path does - not setEditFieldValue, so nothing here is going through
	// inspectPostApply.
	%menu.text = "A Considerably Longer Caption";
	mbCheck("a longer caption widens the menu (" @ %wide @ " -> " @
		getWord(%menu.getExtent(), 0) @ ")", getWord(%menu.getExtent(), 0) > %wide);

	%menu.text = "M";
	mbCheck("and a shorter one narrows it again (" @ getWord(%menu.getExtent(), 0) @ ")",
		getWord(%menu.getExtent(), 0) < %wide);

	// The menu after it moves too, because the strip packs left to right.
	%second = $mbBar.getObject(1);
	mbCheck("the menu after it moved up (" @ %second.getPosition() @ ")",
		getWord(%second.getPosition(), 0) == getWord(%menu.getExtent(), 0));

	%menu.text = "Menu 1";

	schedule(300, 0, "mbStepSpacer");
}

//-----------------------------------------------------------------------------
// Separators. A single dash in the caption is the whole of one - there is no
// field for it - which is what the documentation says and what a .gui file
// carries, so it has to survive being read back as well as being typed.
//-----------------------------------------------------------------------------

function mbStepSpacer()
{
	%menu = $mbBar.getObject(0);
	%command = %menu.getObject(0);

	mbSelect(%menu);

	// Asking for the "+" row is what lays the dropdown out, and the row height is
	// how a separator shows itself: it is the profile's chrome with no line of
	// text in it.
	$mbBar.getAddSubItemRect();
	%tall = getWord(%command.getExtent(), 1);

	%command.text = "-";
	$mbBar.getAddSubItemRect();

	mbCheck("a dash keeps its dash (" @ %command.getText() @ ")", %command.getText() $= "-");
	mbCheck("and makes the row a thin rule (" @ %tall @ " -> " @
		getWord(%command.getExtent(), 1) @ ")", getWord(%command.getExtent(), 1) < %tall);

	// The pane has to notice it, because typing the dash is how you make one.
	%pane = GuiEditor.inspectorWindow.pane;
	%pane.bind(%command);
	mbCheck("the pane calls it a spacer (" @ %pane.header.menuItemBlock.kindRow.getValue() @ ")",
		%pane.header.menuItemBlock.kindRow.getValue() $= "spacer");
	mbCheck("and drops the fields a rule cannot use",
		!%pane.header.menuItemBlock.commandRow.isVisible());

	// And back out of it again.
	%command.text = "Open";
	$mbBar.getAddSubItemRect();
	mbCheck("typing over the dash makes it a command again (" @
		getWord(%command.getExtent(), 1) @ ")", getWord(%command.getExtent(), 1) == %tall);

	%pane.bind(%command);
	mbCheck("and the pane agrees (" @ %pane.header.menuItemBlock.kindRow.getValue() @ ")",
		%pane.header.menuItemBlock.kindRow.getValue() $= "command");

	// A dash on the BAR is just a caption: a rule across a menu bar would have
	// nothing either side of it to separate.
	%menu.text = "-";
	$mbBar.getAddSubItemRect();
	%pane.bind(%menu);
	mbCheck("a dash on the bar is not a separator (" @
		%pane.header.menuItemBlock.kindRow.getValue() @ ")",
		%pane.header.menuItemBlock.kindRow.getValue() $= "command");
	mbCheck("and the bar is not offered the choice",
		!%pane.header.menuItemBlock.kindRow.choiceButton[3].isVisible());
	%menu.text = "Menu 1";

	// A command inside a menu is.
	%pane.bind(%command);
	mbCheck("a command inside a menu is offered it",
		%pane.header.menuItemBlock.kindRow.choiceButton[3].isVisible());

	schedule(300, 0, "mbStepDelete");
}

function mbGlobalRect(%ctrl)
{
	return %ctrl.getGlobalPosition() SPC %ctrl.getExtent();
}

//-----------------------------------------------------------------------------
// Emptying a bar, which used to be permanent.
//-----------------------------------------------------------------------------

function mbStepDelete()
{
	GuiEditor.undoRecorder.clear();

	while($mbBar.getCount() > 0)
	{
		%menu = $mbBar.getObject(0);
		mbSelect(%menu);
		GuiEditor.brain.onObjectRemoved(%menu);
	}

	mbCheck("the bar can be emptied (" @ $mbBar.getCount() @ ")", $mbBar.getCount() == 0);
	mbCheck("and is still recoverable (" @ $mbBar.getAddItemRect() @ ")",
		getWord($mbBar.getAddItemRect(), 2) > 0);

	GuiEditor.brain.onAddMenuItem($mbBar, "");
	mbCheck("the \"+\" refills an emptied bar (" @ $mbBar.getCount() @ ")",
		$mbBar.getCount() == 1);
	mbCheck("numbering from the start again (" @ $mbBar.getObject(0).getText() @ ")",
		$mbBar.getObject(0).getText() $= "Menu 1");

	schedule(300, 0, "mbStepRules");
}

//-----------------------------------------------------------------------------
// Where a menu item is allowed to live. Two legal kinds of parent, unlike a tab
// page, because moving a command between menus is an ordinary thing to want.
//-----------------------------------------------------------------------------

function mbStepRules()
{
	$mbPanel = new GuiControl() { Position = "10 400"; Extent = "300 200"; };
	GuiEditor.rootGui.add($mbPanel);

	$mbButton = new GuiButtonCtrl() { Position = "10 10"; Extent = "80 30"; Text = "B"; };
	$mbPanel.add($mbButton);

	%menu = $mbBar.getObject(0);
	GuiEditor.brain.onAddMenuItem($mbBar, %menu);
	%command = %menu.getObject(0);

	mbCheck("a menu belongs in its bar", %menu.canBeChildOf($mbBar));
	mbCheck("a command belongs in its menu", %command.canBeChildOf(%menu));
	mbCheck("and in a bar too", %command.canBeChildOf($mbBar));
	mbCheck("but not in a panel", !%command.canBeChildOf($mbPanel));
	mbCheck("nor on the root", !%command.canBeChildOf(GuiEditor.rootGui));
	mbCheck("an ordinary control still goes anywhere", $mbButton.canBeChildOf($mbPanel));

	// And what the editor may do to it once it is there. A menu item's position
	// and extent are the bar's to write, so the canvas draws it an outline rather
	// than eight handles you could drag to no effect.
	mbCheck("a menu's geometry is not the editor's", !%menu.isGeometryEditable());
	mbCheck("nor a command's", !%command.isGeometryEditable());
	mbCheck("an ordinary control's is", $mbButton.isGeometryEditable());

	// The canvas drag. Both halves, because a rule that refused everything would
	// pass the first check on its own.
	mbSelect(%command);
	GuiEditor.brain.moveSelectionToCtrl($mbPanel);
	mbCheck("dragging a command onto a panel leaves it alone", %command.getParent() == %menu);

	mbSelect(%command);
	GuiEditor.brain.moveSelectionToCtrl($mbBar);
	mbCheck("dragging it onto the bar moves it", %command.getParent() == $mbBar);

	mbSelect($mbButton);
	GuiEditor.brain.moveSelectionToCtrl($mbPanel);
	mbCheck("an ordinary control is not caught by the rule",
		$mbButton.getParent() == $mbPanel);

	schedule(300, 0, "mbStepPaste");
}

function mbStepPaste()
{
	%menu = $mbBar.getObject(0);

	mbSelect(%menu);
	GuiEditor.Copy();

	// Into a panel: refused, and the clipboard is left holding it.
	%before = $mbPanel.getCount();
	GuiEditor.brain.setCurrentAddSet($mbPanel);
	GuiEditor.Paste();
	mbCheck("pasting a menu into a panel puts nothing there (" @ $mbPanel.getCount() @ ")",
		$mbPanel.getCount() == %before);

	%before = $mbBar.getCount();
	GuiEditor.brain.setCurrentAddSet($mbBar);
	GuiEditor.Paste();
	mbCheck("pasting it into a bar does (" @ $mbBar.getCount() @ ")",
		$mbBar.getCount() == (%before + 1));

	schedule(300, 0, "mbStepChrome");
}

//-----------------------------------------------------------------------------
// The editor's own menu bar, which is a real GuiMenuBarCtrl full of real menu
// items and must be untouched by any of this. It is not inside the Gui being
// authored, so isEditMode() is false for it.
//-----------------------------------------------------------------------------

function mbStepChrome()
{
	mbCheck("the editor's own bar exists", isObject(EditorCore.menuBar));
	mbCheck("and has its menus (" @ EditorCore.menuBar.getCount() @ ")",
		EditorCore.menuBar.getCount() > 0);
	mbCheck("but no \"+\" of its own (" @ EditorCore.menuBar.getAddItemRect() @ ")",
		getWord(EditorCore.menuBar.getAddItemRect(), 2) == 0);
	mbCheck("and no dropdown of its own (" @ EditorCore.menuBar.getAddSubItemRect() @ ")",
		getWord(EditorCore.menuBar.getAddSubItemRect(), 2) == 0);

	echo("MENUBAR DONE");
	quit();
}
