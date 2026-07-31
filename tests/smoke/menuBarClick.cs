//-----------------------------------------------------------------------------
// Clicking the menu bar's two "+" affordances, for real.
//
// menuBar.cs calls GuiEditorBrain::onAddMenuItem directly, which is the right
// test for what the editor does once asked. This is the half that asks, and all
// of it is C++ the script cannot reach: GuiMenuBarCtrl::onMouseDownEditor
// converts the mouse point into the bar's content coordinates and tests it
// against the strip's "+", then the open dropdown's "+" row, then the rows, then
// the menus. Four rectangles in a space nothing else in the file uses - the
// runtime findHitMenu reaches it a third way, through a child's render inset.
//
// The sequence is the real one: click the bar's "+", get a menu, and because the
// editor selects what it just made, that menu's dropdown is already open - so
// the second click puts the first command in it.
//
// Driven by menuBarClick.input.ps1. Neither point is hard-coded there: the
// engine works out where each "+" actually landed and hands it over in a file. A
// hard-coded click that drifted off the target would report a missing item,
// which is exactly what a broken hit test reports.
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

function mcCheck(%label, %condition)
{
	echo(%condition ? ("MENUCLICK PASS: " @ %label) : ("MENUCLICK FAIL: " @ %label));
}

// Where the engine leaves the next point for the driver to click.
function mcTargetFile()
{
	return testRoot("shots/menuBarClickTarget.txt");
}

function mcAimAt(%rect, %what)
{
	%x = getWord(%rect, 0) + mFloor(getWord(%rect, 2) / 2);
	%y = getWord(%rect, 1) + mFloor(getWord(%rect, 3) / 2);

	%file = new FileObject();
	%file.openForWrite(mcTargetFile());
	%file.writeLine(%x SPC %y);
	%file.close();
	%file.delete();

	echo("MENUCLICK: " @ %what @ " at " @ %x SPC %y);
}

schedule(2500, 0, "mcStep1");

// Through the project selector, the way a person opens a project. Calling
// GuiEditor.open() directly leaves the selector sitting on top, and a posted
// click lands on whatever is actually in front.
function mcStep1()
{
	ProjectManager.setProjectFolder("PlanetX");
	EditorCore.projectSelector.onProjectSelected(pathConcat(getMainDotCsDir(), "PlanetX"));

	schedule(2500, 0, "mcStep2");
}

function mcStep2()
{
	EditorCore.toggleEditor();
	EditorCore.tabBook.selectPage(3);

	createPath(testRoot("shots/"));

	schedule(1500, 0, "mcStep3");
}

function mcStep3()
{
	GuiEditor.brain.setCurrentAddSet(GuiEditor.rootGui);

	// placeControl, which is what clicking a palette tile does. A menu bar pins
	// itself to its parent's origin, so there is no position to choose.
	$mcBar = new GuiMenuBarCtrl() { Extent = "300 30"; };
	$mcBar.Position = GuiEditor.brain.centredPlacement($mcBar);
	GuiEditor.brain.placeControl($mcBar);

	// Nothing selected: a drop selects what it dropped, and the edit control
	// tests its sizing knobs before it hands the click to the control.
	GuiEditor.brain.clearSelection();

	schedule(300, 0, "mcStep4");
}

function mcStep4()
{
	mcCheck("the bar arrived with a menu (" @ $mcBar.getCount() @ ")", $mcBar.getCount() == 1);

	%rect = $mcBar.getAddItemRect();
	mcCheck("and reports a \"+\" to aim at (" @ %rect @ ")", getWord(%rect, 2) > 0);
	mcAimAt(%rect, "the bar's +");

	GuiEditor.undoRecorder.clear();

	// The driver posts its first click during this window.
	schedule(8000, 0, "mcStep5");
}

function mcStep5()
{
	mcCheck("clicking the bar's \"+\" added a menu (" @ $mcBar.getCount() @ ")",
		$mcBar.getCount() == 2);

	if($mcBar.getCount() != 2)
	{
		echo("MENUCLICK DONE");
		schedule(400, 0, "quit");
		return;
	}

	$mcMenu = $mcBar.getObject(1);
	mcCheck("made by the editor, not the raw C++ (" @ $mcMenu.getText() @ ")",
		$mcMenu.getText() $= "Menu 2");
	mcCheck("one undo step for the click (" @ GuiEditor.undoRecorder.undoCount() @ ")",
		GuiEditor.undoRecorder.undoCount() == 1);

	// The editor selected what it made, and the dropdown follows the selection -
	// so the new menu is already open, with nothing in it but its "+" row. That
	// is the whole point of deriving it from the selection rather than a toggle.
	mcCheck("the new menu is selected", GuiEditor.brain.selectionList() $= $mcMenu);

	%rect = $mcBar.getAddSubItemRect();
	mcCheck("and its dropdown is already open (" @ %rect @ ")", getWord(%rect, 2) > 0);
	mcAimAt(%rect, "the dropdown's +");

	GuiEditor.undoRecorder.clear();

	// And the second click.
	schedule(8000, 0, "mcStep6");
}

function mcStep6()
{
	mcCheck("clicking the dropdown's \"+\" added a command (" @ $mcMenu.getCount() @ ")",
		$mcMenu.getCount() == 1);
	mcCheck("inside the menu, not on the bar (" @ $mcBar.getCount() @ ")",
		$mcBar.getCount() == 2);

	if($mcMenu.getCount() == 1)
	{
		mcCheck("numbered from 1 in its own menu (" @ $mcMenu.getObject(0).getText() @ ")",
			$mcMenu.getObject(0).getText() $= "Menu 1");
	}

	mcCheck("one undo step for that click too (" @ GuiEditor.undoRecorder.undoCount() @ ")",
		GuiEditor.undoRecorder.undoCount() == 1);

	screenShot(testRoot("shots/menuBarClick.png"), "PNG");

	schedule(300, 0, "mcStep7");
}

//-----------------------------------------------------------------------------
// And the editor's OWN menu bar, which is a real GuiMenuBarCtrl full of real
// menu items sitting a few pixels above everything this suite just did.
//
// It must still open the ordinary way. Nothing here touched openMenu or the
// full-canvas dialog it pushes, but the bar's findHitControl was given
// GuiControl's signature so that it overrides rather than hides - and that
// changes which control the canvas resolves a point to at runtime, not just in
// the editor. This is the check for that.
//-----------------------------------------------------------------------------

function mcStep7()
{
	%file = mcFindItem(EditorCore.menuBar, "File");
	mcCheck("the editor has a File menu", isObject(%file));
	mcCheck("which has commands in it (" @ %file.getCount() @ ")", %file.getCount() > 0);
	mcCheck("and no \"+\" of its own (" @ EditorCore.menuBar.getAddItemRect() @ ")",
		getWord(EditorCore.menuBar.getAddItemRect(), 2) == 0);

	// An open menu is a dialog pushed on the canvas, so the canvas child count is
	// what says whether it opened.
	$mcDialogs = Canvas.getCount();

	mcAimAt(%file.getGlobalPosition() SPC %file.getExtent(), "the editor's own File menu");

	schedule(8000, 0, "mcStep8");
}

function mcStep8()
{
	mcCheck("clicking a real menu still opens it (" @ $mcDialogs @ " -> " @
		Canvas.getCount() @ ")", Canvas.getCount() == ($mcDialogs + 1));

	screenShot(testRoot("shots/menuBarRuntime.png"), "PNG");

	// And shut it again before leaving. An open menu holds its scroller inside a
	// dialog pushed on the canvas, and quitting out from under that hangs the
	// engine on the way down - which is nothing to do with this feature, but is
	// a state no test should leave behind. The background catcher is full-canvas,
	// so a click anywhere closes it.
	mcAimAt("500 600 4 4", "somewhere else, to close it");

	schedule(8000, 0, "mcStep9");
}

function mcStep9()
{
	mcCheck("clicking away closes it again (" @ Canvas.getCount() @ ")",
		Canvas.getCount() == $mcDialogs);

	echo("MENUCLICK DONE");
	schedule(400, 0, "quit");
}

// Menu items are nested controls, so this walks rather than indexes.
function mcFindItem(%parent, %text)
{
	for(%i = 0; %i < %parent.getCount(); %i++)
	{
		%item = %parent.getObject(%i);
		if(%item.Text $= %text)
		{
			return %item;
		}

		%found = mcFindItem(%item, %text);
		if(isObject(%found))
		{
			return %found;
		}
	}

	return 0;
}
