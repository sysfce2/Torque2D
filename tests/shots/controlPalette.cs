// Visual harness for the Gui Editor's control palette. Four shots:
//
//   0  grid mode, every group open -- a picture over its name, the default view
//   1  row mode, every group open  -- a small picture with the name beside it
//   2  grid mode with two groups collapsed
//   3  grid mode scrolled to the two-line names
//
// Neither of the last two is decoration. GuiExpandCtrl force-writes mVisible on
// every direct child of a panel whenever it expands or collapses, which is why
// the tiles live in an inner grid; collapsing and reopening is what proves the
// tiles survived it. And every name in Basics fits on one line, so only the
// fourth shot shows what the caption band is sized for -- a name that wraps,
// stacking upward off the floor of the tile without reaching the picture.
//
// Run: tests/run.ps1 -Shots controlPalette ; look in shots/.

setLogMode(1);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

testExec("editor/main.cs");
schedule(2500, 0, "pOpenProject");

function pOpenProject()
{
	ProjectManager.setProjectFolder("PlanetX");
	EditorCore.projectSelector.onProjectSelected(pathConcat(getMainDotCsDir(), "PlanetX"));
	schedule(2500, 0, "pOpenEditor");
}

// Pages register in load order: EditorConsole, ProjectManager, AssetAdmin,
// GuiEditor.
function pOpenEditor()
{
	EditorCore.toggleEditor();
	EditorCore.tabBook.selectPage(3);
	schedule(1500, 0, "pGridShot");
}

function pGrab(%name)
{
	// screenShot does not create its folder and reports failure by logging, so a
	// tree that has never run a shot writes nothing and says nothing.
	createPath(testRoot("shots/"));
	screenShot(testRoot("shots/controlPalette" @ %name @ ".png"), "PNG");
}

function pGridShot()
{
	pGrab(0);
	GuiEditor.ctrlListWindow.setMode("rows");
	GuiEditor.ctrlListWindow.modeRow.setValue("rows");
	schedule(800, 0, "pRowShot");
}

function pRowShot()
{
	pGrab(1);
	GuiEditor.ctrlListWindow.setMode("grid");
	GuiEditor.ctrlListWindow.modeRow.setValue("grid");
	schedule(800, 0, "pCollapseShot");
}

// Collapse the FIRST group, not a later one: the palette column is short enough
// that everything past Basics is below the fold, so closing group 3 would look
// identical to not closing anything.
function pCollapseShot()
{
	%window = GuiEditor.ctrlListWindow;
	%window.group[0].setExpanded(false);
	%window.group[2].setExpanded(false);
	%window.relayout();
	schedule(800, 0, "pFinish");
}

function pFinish()
{
	pGrab(2);

	// Reopen what was closed, so the shot has proved that a tile survives a
	// collapse rather than being left hidden by the expand control.
	%window = GuiEditor.ctrlListWindow;
	%window.group[0].setExpanded(true);
	%window.group[2].setExpanded(true);
	%window.relayout();

	schedule(800, 0, "pWrapShot");
}

// The long names live in Input & Data -- "Radio Button", "Image Button" -- which
// is the third group and so below the fold. Shutting the two above it is what
// brings it to the top; scrolling would depend on how tall the window happens to
// be on the machine running this.
function pWrapShot()
{
	%window = GuiEditor.ctrlListWindow;
	%window.group[0].setExpanded(false);
	%window.group[1].setExpanded(false);
	%window.relayout();
	schedule(800, 0, "pWrapGrab");
}

function pWrapGrab()
{
	pGrab(3);

	%window = GuiEditor.ctrlListWindow;
	%window.group[0].setExpanded(true);
	%window.group[1].setExpanded(true);
	%window.relayout();

	echo("SHOTS DONE");
	schedule(500, 0, "quit");
}
