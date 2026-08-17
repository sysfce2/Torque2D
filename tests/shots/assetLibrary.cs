// Visual harness for the Asset Library. Three shots:
//
//   0  tile mode  -- a thumbnail over its name, the default view
//   1  row mode   -- a small thumbnail with the name beside it
//   2  tile mode, filtered so one group empties and another does not
//
// The toolbar is the part that only a picture can settle: the filter icon, the
// search box and the count line share a row, and the two segmented rows share
// the one above it. None of that is checkable by assertion beyond "the numbers
// are what I wrote".
//
// Run: tests/run.ps1 -Shots assetLibrary ; look in shots/.

setLogMode(1);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

testExec("editor/main.cs");
schedule(2500, 0, "aOpenProject");

function aOpenProject()
{
	ProjectManager.setProjectFolder("PlanetX");
	EditorCore.projectSelector.onProjectSelected(pathConcat(getMainDotCsDir(), "PlanetX"));

	// Switching the view below writes a preference, and unredirected that lands
	// in the real per-user folder -- so running the harness would change how the
	// editor opens for the person who ran it.
	createPath(testRoot("shots/"));
	EditorPreferences.path = testRoot("shots/assetLibraryShotPrefs.taml");

	// A project's own assets belong to modules the editor has only scanned, so the
	// library opens empty and every shot below would show five headers and nothing
	// else. ToyAssets is nothing but assets -- no ScriptFile, no CreateFunction --
	// so registering what it declares fills the library without running toy code.
	ModuleDatabase.scanModules(testRoot("toybox/ToyAssets"));
	%module = ModuleDatabase.findModule("ToyAssets", 1);
	if(isObject(%module))
	{
		AssetDatabase.addModuleDeclaredAssets(%module);
	}

	schedule(2500, 0, "aOpenEditor");
}

// Pages register in load order: EditorConsole, ProjectManager, AssetAdmin,
// GuiEditor.
function aOpenEditor()
{
	EditorCore.toggleEditor();
	EditorCore.tabBook.selectPage(2);
	schedule(1500, 0, "aOpenGroups");
}

// Every group starts collapsed -- GuiExpandCtrl's constructor leaves mExpanded
// false and the library has always opened that way -- so a shot taken as-is is
// five headers and nothing else. Open the two that carry real art.
function aOpenGroups()
{
	AssetAdmin.Dictionary["ImageAsset"].setExpanded(true);
	AssetAdmin.Dictionary["AnimationAsset"].setExpanded(true);
	AssetAdmin.libWindow.relayout();

	schedule(1200, 0, "aTileShot");
}

function aGrab(%name)
{
	// screenShot does not create its folder and reports failure by logging, so a
	// tree that has never run a shot writes nothing and says nothing.
	createPath(testRoot("shots/"));
	screenShot(testRoot("shots/assetLibrary" @ %name @ ".png"), "PNG");
}

function aTileShot()
{
	aGrab(0);

	AssetAdmin.libWindow.setViewMode("rows");
	AssetAdmin.libWindow.viewRow.setValue("rows");
	schedule(800, 0, "aRowShot");
}

function aRowShot()
{
	aGrab(1);

	AssetAdmin.libWindow.setViewMode("grid");
	AssetAdmin.libWindow.viewRow.setValue("grid");
	schedule(800, 0, "aFilterShot");
}

// A needle that leaves images behind and empties the sound and font groups, so
// the shot shows both halves of the rule: a group that matched keeps its tiles,
// a group that did not keeps its header and reads (0).
function aFilterShot()
{
	AssetAdmin.libWindow.searchBox.setText("a");
	AssetAdmin.libWindow.applyFilter();
	schedule(800, 0, "aFinish");
}

function aFinish()
{
	aGrab(2);

	AssetAdmin.libWindow.searchBox.setText("");
	AssetAdmin.libWindow.applyFilter();

	echo("SHOTS DONE");
	schedule(500, 0, "quit");
}
