// Visual harness for the Asset Manager's image inspector. Four shots:
//
//   0  the pane as it opens -- the wide, short bottom frame the inspector is,
//      where the four blocks sit two by two
//   1  the same width with the frame dragged taller, where everything fits
//   2  tall and narrow, where every block stacks into one column
//   3  wide again, with an impossible cell cut, to see the warning line
//   4  on a 1600-wide screen, where the four blocks become a single row
//   5  the Image Layers tab, base row alone: the color picker wears a padlock
//   6  the same with a layer added, so the base color unlocks
//
// What only a picture can settle: whether the identity block and the cell table
// balance beside each other, whether the info and warning lines read as
// statements rather than as fields, and whether the whole thing survives being
// reshaped. None of that is checkable by assertion beyond "the numbers are what
// I wrote" -- tests/smoke/assetImageInspector.cs does that part.
//
// Run: tests/run.ps1 -Shots assetImageInspector ; look in shots/.

setLogMode(1);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

// Gems is 512 x 512 cut into 8 x 8 cells of 64, so every readout has something
// to say.
$aiAssetId = "ToyAssets:Gems";

testExec("editor/main.cs");
schedule(2500, 0, "aOpenProject");

function aOpenProject()
{
	ProjectManager.setProjectFolder("PlanetX");
	EditorCore.projectSelector.onProjectSelected(pathConcat(getMainDotCsDir(), "PlanetX"));

	// The pane writes the asset's file on every commit, so shot 3 works on a copy
	// -- the same rule the smoke test follows. tests/run.ps1 sweeps the folder by
	// reading the spelled-out name out of this file.
	createPath(testRoot("shots/"));
	ProjectManager.setProjectFolder("assetImageInspectorShotProject");
	EditorPreferences.path = testRoot("shots/assetImageInspectorShotPrefs.taml");

	%copy = testRoot("assetImageInspectorShotProject/ToyAssets");
	pathCopy(testRoot("toybox/ToyAssets"), %copy, false);
	ModuleDatabase.scanModules(%copy);
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
	schedule(1500, 0, "aSelectAsset");
}

function aSelectAsset()
{
	AssetAdmin.Dictionary["ImageAsset"].setExpanded(true);
	AssetAdmin.libWindow.relayout();

	$aiTile = AssetAdmin.Dictionary["ImageAsset"].getButton($aiAssetId);
	$aiTile.onClick();

	$aiPane = AssetAdmin.inspector.imagePane;
	schedule(1200, 0, "aDefaultShot");
}

function aGrab(%name)
{
	// screenShot does not create its folder and reports failure by logging, so a
	// tree that has never run a shot writes nothing and says nothing.
	createPath(testRoot("shots/"));
	screenShot(testRoot("shots/assetImageInspector" @ %name @ ".png"), "PNG");
}

function aDefaultShot()
{
	aGrab(0);

	// The same width with the divider dragged up, which is the shape somebody
	// editing an asset would settle into: everything fits with no scrolling.
	AssetAdmin.content.setFrameSize(AssetAdmin.inspectorFrameId, 480);

	schedule(1200, 0, "aTallShot");
}

function aTallShot()
{
	aGrab(1);

	// Tall and narrow: give the library most of the width and the inspector most
	// of what is left of the height. This is the shape the reflow exists for.
	%canvas = Canvas.getExtent();
	AssetAdmin.content.setFrameSize(AssetAdmin.libraryFrameId,
		getWord(%canvas, 0) - 400);
	AssetAdmin.content.setFrameSize(AssetAdmin.inspectorFrameId,
		getWord(%canvas, 1) - 220);

	schedule(1200, 0, "aNarrowShot");
}

function aNarrowShot()
{
	aGrab(2);

	AssetAdmin.content.setFrameSize(AssetAdmin.libraryFrameId, 324);
	AssetAdmin.content.setFrameSize(AssetAdmin.inspectorFrameId, 360);

	schedule(1200, 0, "aWarnShot");
}

// Eight cells of 128 needs 1024 pixels and the image is 512, so the asset
// refuses the cut and falls back to one frame. The pane says so.
function aWarnShot()
{
	%grid = $aiPane.cellGrid;
	%grid.box["CellWidth"].setText(128);
	%grid.commitBox(%grid.box["CellWidth"]);

	schedule(900, 0, "aWideScreen");
}

// The case the grid exists for, and the only one the default test window is too
// small to show: at 1600 the inspector runs the width of the screen and the four
// blocks become a single row instead of a column of fields with a yard of empty
// panel beside it.
function aWideScreen()
{
	aGrab(3);

	%grid = $aiPane.cellGrid;
	%grid.box["CellWidth"].setText(64);
	%grid.commitBox(%grid.box["CellWidth"]);

	setScreenMode(1600, 900, 32, false);
	schedule(1500, 0, "aLayersTab");
}

// The color the inspector used to carry, in the row it belongs to. Row 0 is the
// asset's own image -- the base every layer is drawn onto -- and its tint is
// locked while it is alone, because there is nothing composed onto it for a tint
// to show up on.
//
// Pages for an image asset: Inspector, Explicit Frames, Image Layers.
function aLayersTab()
{
	aGrab(4);

	AssetAdmin.inspector.tabBook.selectPage(2);
	schedule(900, 0, "aLockedShot");
}

function aLockedShot()
{
	aGrab(5);

	AssetAdmin.inspector.imageLayersEditPage.addNewLayer();
	schedule(900, 0, "aFinish");
}

function aFinish()
{
	aGrab(6);

	echo("SHOTS DONE");
	schedule(500, 0, "quit");
}
