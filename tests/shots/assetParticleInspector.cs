// Visual harness for the Asset Manager's particle and emitter inspectors. Shots:
//
//   0  the effect pane as the inspector opens -- the wide, short bottom frame,
//      where its three blocks sit in a row and the Lifetime row is greyed
//   1  the emitter pane at the same size, where six blocks wrap into two rows
//   2  the emitter pane with the frame dragged taller, everything at once
//   3  tall and narrow, where every block stacks into one column
//   4  the same emitter with Single Particle on -- the greying rule that reaches
//      furthest, and the one worth looking at rather than asserting
//   5  an animated emitter, where the source swap has taken the image rows away
//      and put the animation row in their place
//   6  on a 1600-wide screen, where the six blocks become a single row
//   7  the effect pane with no emitters, for the warning line
//
// What only a picture can settle: whether six blocks of unequal height still
// read as a row rather than as a ragged pile, whether a greyed row is legible
// enough to be worth keeping on show, and whether the swap leaves a hole where
// the hidden rows were. tests/smoke/assetParticleInspector.cs does the part that
// is checkable by assertion.
//
// Run: tests/run.ps1 -Shots assetParticleInspector ; look in shots/.

setLogMode(1);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

// bonfire's two emitters differ in exactly the ways the pane reshapes for: the
// first draws a still image, the second plays an animation.
$apAssetId = "ToyAssets:bonfire";

testExec("editor/main.cs");
schedule(2500, 0, "apOpenProject");

function apOpenProject()
{
	ProjectManager.setProjectFolder("PlanetX");
	EditorCore.projectSelector.onProjectSelected(pathConcat(getMainDotCsDir(), "PlanetX"));

	// A copy: shot 4 and shot 7 both edit. tests/run.ps1 sweeps the folder by
	// reading the spelled-out name out of this file.
	createPath(testRoot("shots/"));
	ProjectManager.setProjectFolder("assetParticleInspectorShotProject");
	EditorPreferences.path = testRoot("shots/assetParticleInspectorShotPrefs.taml");

	%copy = testRoot("assetParticleInspectorShotProject/ToyAssets");
	pathCopy(testRoot("toybox/ToyAssets"), %copy, false);
	ModuleDatabase.scanModules(%copy);
	%module = ModuleDatabase.findModule("ToyAssets", 1);
	if(isObject(%module))
	{
		AssetDatabase.addModuleDeclaredAssets(%module);
	}

	schedule(2500, 0, "apOpenEditor");
}

// Pages register in load order: EditorConsole, ProjectManager, AssetAdmin,
// GuiEditor.
function apOpenEditor()
{
	EditorCore.toggleEditor();
	EditorCore.tabBook.selectPage(2);
	schedule(1500, 0, "apSelectAsset");
}

function apSelectAsset()
{
	AssetAdmin.Dictionary["ParticleAsset"].setExpanded(true);
	AssetAdmin.libWindow.relayout();

	$apTile = AssetAdmin.Dictionary["ParticleAsset"].getButton($apAssetId);
	$apTile.onClick();

	$apInspector = AssetAdmin.inspector;
	$apPane = $apInspector.particlePane;
	$apEmitterPane = $apInspector.emitterPane;
	$apAsset = $apTile.ParticleAsset;

	schedule(1200, 0, "apEffectShot");
}

function apGrab(%name)
{
	// screenShot does not create its folder and reports failure by logging, so a
	// tree that has never run a shot writes nothing and says nothing.
	createPath(testRoot("shots/"));
	screenShot(testRoot("shots/assetParticleInspector" @ %name @ ".png"), "PNG");
}

function apSelect(%index)
{
	$apInspector.titleDropDown.setSelected(%index);
	$apInspector.onChooseParticleAsset($apAsset);
}

function apEffectShot()
{
	apGrab(0);

	// The first emitter: a LINE drawing a still image.
	apSelect(1);

	schedule(1200, 0, "apEmitterShot");
}

function apEmitterShot()
{
	apGrab(1);

	// The shape somebody tuning an emitter would settle into: everything at once
	// with no scrolling.
	AssetAdmin.content.setFrameSize(AssetAdmin.inspectorFrameId, 520);

	schedule(1200, 0, "apTallShot");
}

function apTallShot()
{
	apGrab(2);

	// Tall and narrow: give the library most of the width and the inspector most
	// of what is left of the height. This is the shape the reflow exists for.
	%canvas = Canvas.getExtent();
	AssetAdmin.content.setFrameSize(AssetAdmin.libraryFrameId,
		getWord(%canvas, 0) - 400);
	AssetAdmin.content.setFrameSize(AssetAdmin.inspectorFrameId,
		getWord(%canvas, 1) - 220);

	schedule(1200, 0, "apNarrowShot");
}

function apNarrowShot()
{
	apGrab(3);

	AssetAdmin.content.setFrameSize(AssetAdmin.libraryFrameId, 324);
	AssetAdmin.content.setFrameSize(AssetAdmin.inspectorFrameId, 520);

	schedule(900, 0, "apSingleShot");
}

// The widest greying rule on the pane: one immortal particle at the offset means
// the whole Emission block stops being read.
function apSingleShot()
{
	$apEmitterPane.commitValue("SingleParticle", true);

	schedule(900, 0, "apAnimatedShot");
}

function apAnimatedShot()
{
	apGrab(4);

	$apEmitterPane.commitValue("SingleParticle", false);

	// The second emitter plays an animation, so the image rows are gone rather
	// than greyed.
	apSelect(2);

	schedule(1200, 0, "apWideScreen");
}

// The case the grid exists for, and the only one the default test window is too
// small to show: at 1600 the inspector runs the width of the screen and six
// blocks become a single row instead of a column with a yard of empty panel
// beside it.
function apWideScreen()
{
	apGrab(5);

	setScreenMode(1600, 900, false);

	schedule(2000, 0, "apWideShot");
}

function apWideShot()
{
	AssetAdmin.content.setFrameSize(AssetAdmin.libraryFrameId, 324);
	AssetAdmin.content.setFrameSize(AssetAdmin.inspectorFrameId, 420);

	schedule(1200, 0, "apWarnShot");
}

function apWarnShot()
{
	apGrab(6);

	// An effect that draws nothing. It can arrive from a file in this state, so
	// the pane has to say so rather than showing an empty preview and no reason.
	apSelect(0);
	$apAsset.clearEmitters();
	$apAsset.refreshAsset();

	schedule(1200, 0, "apDoneShot");
}

function apDoneShot()
{
	apGrab(7);

	echo("APSHOT DONE");
	schedule(400, 0, "quit");
}
