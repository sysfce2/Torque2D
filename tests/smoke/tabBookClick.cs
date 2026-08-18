//-----------------------------------------------------------------------------
// Clicking the "+" tab, for real.
//
// tabBook.cs calls GuiEditorBrain::onAddTabPage directly, which is the right
// test for what the editor does once asked - but it says nothing about the half
// that asks. That half is C++: GuiTabBookCtrl::onMouseDownEditor converts the
// mouse point into the tab strip's own coordinates, tests it against the "+",
// and calls back into script. Nothing about it can be reached from script, and
// its predecessor got the conversion wrong for years (onTouchDown converted and
// this did not), so a click that is a whole border out still looks like a hit.
//
// Driven by tabBookClick.input.ps1, which posts a real WM_LBUTTONDOWN/UP. The
// point is NOT hard-coded there: the engine works out where the "+" actually is
// and hands it over in a file. A hard-coded click landing an inch to the left of
// the "+" would report exactly what a broken hit test reports.
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

function tcCheck(%label, %condition)
{
	echo(%condition ? ("TABCLICK PASS: " @ %label) : ("TABCLICK FAIL: " @ %label));
}

// Where the engine leaves the point for the driver to click.
function tcTargetFile()
{
	return testRoot("shots/tabBookClickTarget.txt");
}

schedule(2500, 0, "tcStep1");

// Through the project selector, the way a person opens a project. The suites
// that call GuiEditor.open() directly get an editor that is awake - enough for
// isEditMode(), and so enough to call the brain's methods - but leaves the
// project selector sitting on top of it. A posted click lands on whatever is
// actually in front, so this one has to dismiss it properly.
function tcStep1()
{
	ProjectManager.setProjectFolder("PlanetX");
	EditorCore.projectSelector.onProjectSelected(pathConcat(getMainDotCsDir(), "PlanetX"));

	schedule(2500, 0, "tcStep2");
}

// Loading AppCore starts the project's game over the canvas; the editor comes
// back the way Ctrl+~ does it. Pages register in load order: EditorConsole,
// ProjectManager, AssetAdmin, GuiEditor.
function tcStep2()
{
	EditorCore.toggleEditor();
	EditorCore.tabBook.selectPage(3);

	createPath(testRoot("shots/"));

	schedule(1500, 0, "tcStep3");
}

function tcStep3()
{
	GuiEditor.brain.setCurrentAddSet(GuiEditor.rootGui);

	// Dropped rather than added, so it arrives themed and with the page the drop
	// seeds it with - the state a book is actually in when someone reaches for
	// the "+".
	%room = GuiEditor.brain.visiblePartOf(GuiEditor.rootGui);
	$tcBook = new GuiTabBookCtrl()
	{
		Extent = "260 120";
		Position = (getWord(%room, 0) + 40) SPC (getWord(%room, 1) + 40);
	};
	GuiEditor.brain.onControlDropped($tcBook, "50 50");

	// After the drop's own schedule(40) has finished placing it.
	schedule(300, 0, "tcStep4");
}

function tcStep4()
{
	// A drop selects what it dropped, and the edit control tests its sizing
	// knobs before it hands the click to the control. Nothing selected means
	// nothing between the cursor and the book.
	GuiEditor.brain.clearSelection();

	tcCheck("the book arrived with a page (" @ $tcBook.getCount() @ ")", $tcBook.getCount() == 1);

	%rect = $tcBook.getAddPageTabRect();
	tcCheck("and reports a \"+\" to aim at (" @ %rect @ ")", getWord(%rect, 2) > 0);

	%x = getWord(%rect, 0) + mFloor(getWord(%rect, 2) / 2);
	%y = getWord(%rect, 1) + mFloor(getWord(%rect, 3) / 2);

	%file = new FileObject();
	%file.openForWrite(tcTargetFile());
	%file.writeLine(%x SPC %y);
	%file.close();
	%file.delete();

	echo("TABCLICK: + tab centre at " @ %x SPC %y);

	GuiEditor.undoRecorder.clear();

	// The driver posts its click during this window.
	schedule(8000, 0, "tcStep5");
}

function tcStep5()
{
	tcCheck("clicking the \"+\" added a page (" @ $tcBook.getCount() @ ")", $tcBook.getCount() == 2);

	if($tcBook.getCount() == 2)
	{
		%page = $tcBook.getObject(1);
		tcCheck("made by the editor, not the raw C++ (" @ %page.getText() @ ")",
			%page.getText() $= "Page 2");
		tcCheck("themed like any arrival (" @ %page.Profile.category @ ")",
			%page.Profile.category $= "TabPage");
		tcCheck("and showing, as the active tab", %page.isVisible());
	}

	tcCheck("one undo step for the click (" @ GuiEditor.undoRecorder.undoCount() @ ")",
		GuiEditor.undoRecorder.undoCount() == 1);

	// The click landed on the "+" and not on the book behind it, which would
	// have selected the book instead of adding anything.
	tcCheck("the click was not taken as a selection",
		GuiEditor.brain.selectionList() !$= $tcBook);

	screenShot(testRoot("shots/tabBookClick.png"), "PNG");
	echo("TABCLICK DONE");
	schedule(400, 0, "quit");
}
