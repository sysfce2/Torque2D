// Visual harness for the menu bar's editor-only affordances.
//
// ONE bar, shot in three states, because a Gui can only really show one: a menu
// bar owns nothing but its height. GuiMenuBarCtrl::resize throws away the
// position it is handed and passes (0,0), and onRender resizes the bar to the
// clip rect's width on every frame it draws - so two bars in a Gui sit exactly
// on top of each other.
//
//   empty   no menus at all -- which the control palette cannot fix, since it
//           does not offer a GuiMenuItemCtrl, so the "+" is the only way
//           anything ever gets into a bar
//   menus   three of them, so the "+" has to land after the last
//   nested  a menu with commands inside it
//
// The editor's own menu bar is in every shot, along the top, and must never grow
// a "+": it is not inside the Gui being authored, so isEditMode() is false for
// it.
//
// Run: tests/run.ps1 -Shots menuBar ; look in shots/.

setLogMode(1);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

testExec("editor/main.cs");
schedule(2500, 0, "mbOpenProject");

// A real project, loaded the way the project selector does it, so the bars are
// drawn wearing a project's menu profiles rather than the engine's fallbacks.
function mbOpenProject()
{
	ProjectManager.setProjectFolder("PlanetX");
	EditorCore.projectSelector.onProjectSelected(pathConcat(getMainDotCsDir(), "PlanetX"));
	schedule(2500, 0, "mbOpenEditor");
}

// Loading AppCore starts the project's game over the canvas; the editor comes
// back the way Ctrl+~ does it. Pages register in load order: EditorConsole,
// ProjectManager, AssetAdmin, GuiEditor.
function mbOpenEditor()
{
	EditorCore.toggleEditor();
	EditorCore.tabBook.selectPage(3);
	schedule(1500, 0, "mbStep1");
}

function mbStep1()
{
	$mbTheme = GuiEditor.themeLibrary.createTheme("MenuBarShotTheme");
	GuiEditor.themeName = $mbTheme.getName();

	// Position is not ours to choose; the bar pins itself to its parent's origin.
	$mbBar = new GuiMenuBarCtrl() { Extent = "300 30"; };
	GuiEditor.rootGui.add($mbBar);
	GuiEditor.themeApplier.applyToBranch($mbBar, $mbTheme, true);

	createPath(testRoot("shots/"));
	schedule(900, 0, "mbShootEmpty");
}

function mbShootEmpty()
{
	mbGrab("menubar-empty");

	for(%i = 1; %i <= 3; %i++)
	{
		mbItem($mbBar, "Menu " @ %i);
	}
	schedule(600, 0, "mbShootMenus");
}

function mbShootMenus()
{
	mbGrab("menubar-menus");

	// One of each kind, so the shot says what each looks like: a plain command, a
	// separator written as a dash, a toggle that starts on, a radio that does
	// not, and one that opens a submenu of its own.
	%menu = $mbBar.getObject(0);
	mbItem(%menu, "New");
	mbItem(%menu, "-");

	%toggle = mbItem(%menu, "Show Grid");
	%toggle.Toggle = true;
	%toggle.IsOn = true;

	%radio = mbItem(%menu, "Snap to Grid");
	%radio.Radio = true;

	%sub = mbItem(%menu, "Recent");
	mbItem(%sub, "titleGui.gui.taml");

	schedule(600, 0, "mbShootNested");
}

// Nothing selected, so no dropdown: the commands exist but the box does not.
function mbShootNested()
{
	mbGrab("menubar-nested");

	// Selecting the menu is what opens it. Through the brain, so this is the same
	// route a click on the canvas or a row in the Explorer tree takes.
	GuiEditor.brain.selectList($mbBar.getObject(0));
	schedule(600, 0, "mbShootOpen");
}

function mbShootOpen()
{
	mbGrab("menubar-open");

	// And a menu with nothing in it, which is the state every menu starts in and
	// the one the runtime dropdown could not draw at all - its list is not built
	// until a first child arrives.
	GuiEditor.brain.selectList($mbBar.getObject(1));
	schedule(600, 0, "mbShootOpenEmpty");
}

function mbShootOpenEmpty()
{
	mbGrab("menubar-open-empty");

	echo("SHOT DONE");
	schedule(300, 0, "quit");
}

// Added to its parent BEFORE it is given any children of its own: a menu item
// reads its bar out of its parent when a child arrives.
function mbItem(%parent, %text)
{
	%item = new GuiMenuItemCtrl() { Text = %text; };
	%parent.add(%item);
	GuiEditor.themeApplier.applyToBranch(%item, $mbTheme, true);
	return %item;
}

function mbGrab(%name)
{
	screenShot(testRoot("shots/" @ %name @ ".png"), "PNG");
	echo("SHOT: wrote " @ %name @ ".png");
}
