// Visual harness for the unsaved-changes badge on a library tile. Two shots:
//
//   0  tile mode -- the square badge in the top right corner of the tile
//   1  row  mode -- the same badge at the right hand end of the row
//
// The badge is a control of its own wearing impactProfile, so what it looks like
// is entirely a matter of the theme and the two numbers that place it. Neither is
// checkable by assertion beyond "the extent is what I wrote", which is what
// tests/smoke/assetDirtySave.cs already covers -- this is for looking at.
//
// Three assets are dirtied rather than one, so the shot shows the badge against a
// picture, against a busier picture, and next to a tile that is NOT marked.
//
// Run: tests/run.ps1 -Shots assetDirtyMark ; look in shots/.

setLogMode(1);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

testExec("editor/main.cs");
schedule(2500, 0, "adOpenProject");

function adOpenProject()
{
	ProjectManager.setProjectFolder("PlanetX");
	EditorCore.projectSelector.onProjectSelected(pathConcat(getMainDotCsDir(), "PlanetX"));

	// Switching the view below writes a preference, and unredirected that lands
	// in the real per-user folder -- so running the harness would change how the
	// editor opens for the person who ran it.
	createPath(testRoot("shots/"));
	EditorPreferences.path = testRoot("shots/assetDirtyMarkShotPrefs.taml");

	schedule(2500, 0, "adOpenEditor");
}

function adOpenEditor()
{
	EditorCore.toggleEditor();
	EditorCore.tabBook.selectPage(2);
	schedule(1500, 0, "adLoadAssets");
}

// Registered AFTER the editor has opened, and not before it: the project
// selector calls ModuleDatabase.clearDatabase(), which deletes every
// ModuleDefinition and would take this fixture's module with it.
//
// NOTE: this dirties assets in the repository's own toybox/ToyAssets, in memory
// only. Nothing here saves, so nothing on disk is touched -- which is the whole
// point of the change being photographed.
function adLoadAssets()
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

	schedule(1200, 0, "adDirtyAssets");
}

function adDirtyAssets()
{
	// Chosen because they are near the top of the alphabetical grid and so are
	// actually in the shot: a badge over a light picture, a badge over a dark one,
	// and unmarked tiles either side of both.
	adDirty("ToyAssets:Asteroids");
	adDirty("ToyAssets:Blocks");
	adDirty("ToyAssets:brick_02");

	schedule(600, 0, "adTileShot");
}

// A change the asset will accept whatever it currently holds, made only in
// memory. setAssetDirty says outright what a real edit would have said.
function adDirty(%assetId)
{
	if(AssetDatabase.isDeclaredAsset(%assetId))
	{
		AssetDatabase.setAssetDirty(%assetId, true);
	}
}

function adGrab(%name)
{
	// screenShot does not create its folder and reports failure by logging, so a
	// tree that has never run a shot writes nothing and says nothing.
	createPath(testRoot("shots/"));
	screenShot(testRoot("shots/assetDirtyMark" @ %name @ ".png"), "PNG");
}

function adTileShot()
{
	adGrab(0);

	AssetAdmin.libWindow.setViewMode("rows");
	schedule(1200, 0, "adRowShot");
}

function adRowShot()
{
	adGrab(1);
	schedule(600, 0, "quit");
}
