// Visual harness for the Gui Editor's control palette. Three shots:
//
//   0  grid mode, every group open -- the default view
//   1  row mode, every group open  -- icon plus human-readable name
//   2  grid mode with two groups collapsed
//
// The third is not decoration. GuiExpandCtrl force-writes mVisible on every
// direct child of a panel whenever it expands or collapses, which is why the
// tiles live in an inner grid; collapsing and reopening is what proves the tiles
// survived it.
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

	// Reopen what was closed, so the last thing the shot proves is that a tile
	// survives a collapse rather than being left hidden by the expand control.
	%window = GuiEditor.ctrlListWindow;
	%window.group[0].setExpanded(true);
	%window.group[2].setExpanded(true);
	%window.relayout();

	echo("SHOTS DONE");
	schedule(500, 0, "quit");
}
