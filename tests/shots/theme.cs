// Temporary: screenshots the Gui Editor (chrome + the new Set Theme button) and
// the Set Theme dialog.
setLogMode(2);
setScriptExecEcho(false);
trace(false);
$Scripts::ignoreDSOs = true;
setCompanyAndProduct("Torque Game Engines", "Torque2D");
ModuleDatabase.EchoInfo = false;
AssetDatabase.EchoInfo = false;

testExec("editor/main.cs");
// The splash animates for ~2.8s before the project selector appears; give it room.
createPath(testRoot("shots/"));
schedule(7000, 0, "shot1");

function shot1()
{
	// The real project-selection path, so the editor page is actually shown.
	ProjectManager.setProjectFolder("PlanetX");
	EditorCore.projectSelector.onProjectSelected(pathConcat(getMainDotCsDir(), "PlanetX"));

	schedule(2500, 0, "shot1b");
}

function shot1b()
{
	// Loading AppCore starts the project's game over the canvas; the editor is
	// brought back the way Ctrl+~ does it. Pages register in load order:
	// EditorConsole, ProjectManager, AssetAdmin, GuiEditor.
	EditorCore.toggleEditor();
	EditorCore.tabBook.selectPage(3);
	schedule(1500, 0, "shot2");
}

function shot2()
{
	screenShot(testRoot("shots/themeEditorChrome.png"), "PNG");
	GuiEditor.openThemeDialog();
	schedule(1200, 0, "shot3");
}

function shot3()
{
	screenShot(testRoot("shots/themeDialog.png"), "PNG");
	schedule(500, 0, "shotDone");
}

function shotDone()
{
	echo("SHOTS DONE");
	quit();
}
