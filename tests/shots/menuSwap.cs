// Visual harness for the menu bar swapping with the editor tab. Three shots of
// the same strip, at the top of each:
//
//   0  Console         Torque2D | Theme, and nothing between them
//   1  Gui Editor      Torque2D | File | Edit | Layout | Select | Theme
//   2  Asset Manager   Torque2D | File | Edit | Theme
//
// What to look for is that the bar in 1 and 2 has two menus with the same names
// in the same place holding entirely different commands, and that Theme is still
// the last item in all three - the bar can only be added to at the end, so the
// fixed tail comes off and goes back on around every swap.
//
// The dropdowns are not here. A menu opens from a real mouse position and the bar
// refuses to hand a click to a child (findHitControl answers "me"), so a harness
// with no input script cannot open one; what is IN each menu is asserted instead
// by tests/smoke/menuSwap.cs.
//
// Run: tests/run.ps1 -Shots menuSwap ; look in shots/.

setLogMode(1);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

testExec("editor/main.cs");
schedule(2500, 0, "mssOpenProject");

function mssOpenProject()
{
	ProjectManager.setProjectFolder("PlanetX");
	EditorCore.projectSelector.onProjectSelected(pathConcat(getMainDotCsDir(), "PlanetX"));

	createPath(testRoot("shots/"));
	EditorPreferences.path = testRoot("shots/menuSwapShotPrefs.taml");

	schedule(2500, 0, "mssOpenEditor");
}

function mssGrab(%name)
{
	// screenShot does not create its folder and reports failure by logging.
	createPath(testRoot("shots/"));
	screenShot(testRoot("shots/menuSwap" @ %name @ ".png"), "PNG");
}

function mssOpenEditor()
{
	EditorCore.toggleEditor();
	EditorCore.tabBook.selectPage(0);
	schedule(1200, 0, "mssConsoleGrab");
}

function mssConsoleGrab()
{
	mssGrab(0);

	EditorCore.tabBook.selectPage(3);
	schedule(1500, 0, "mssGuiGrab");
}

function mssGuiGrab()
{
	mssGrab(1);

	EditorCore.tabBook.selectPage(2);
	schedule(1500, 0, "mssLoadAssets");
}

// Registered after the editor has opened: the project selector calls
// ModuleDatabase.clearDatabase(), which would take this module with it. The
// library is loaded so the Asset Manager shot is of a working editor rather than
// an empty one.
function mssLoadAssets()
{
	ModuleDatabase.scanModules(testRoot("toybox/ToyAssets"));
	%module = ModuleDatabase.findModule("ToyAssets", 1);
	if(isObject(%module))
	{
		AssetDatabase.addModuleDeclaredAssets(%module);
	}
	AssetAdmin.libWindow.loadAssets();

	AssetAdmin.Dictionary["ImageAsset"].setExpanded(true);
	AssetAdmin.libWindow.relayout();

	schedule(1000, 0, "mssSelectImage");
}

function mssSelectImage()
{
	%tile = AssetAdmin.Dictionary["ImageAsset"].getButton("ToyAssets:Gems");
	if(isObject(%tile))
	{
		%tile.onClick();
	}

	schedule(900, 0, "mssAssetGrab");
}

function mssAssetGrab()
{
	mssGrab(2);

	echo("SHOTS DONE");
	schedule(600, 0, "quit");
}
