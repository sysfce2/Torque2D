// Visual harness for the emitter's unified color graph. Shots:
//
//   0  the color graph as the Color Channel entry opens it -- three curves on one
//      plot, the three channel toggles down the left, and the mixed color strip
//      under the plot
//   1  green made live, so the dim/bright pairing and the toggle lighting can be
//      read against shot 0
//   2  a color with somewhere to go: red falling away as green arrives, which is
//      the picture the strip exists to show
//   3  the same, with the value axis at its tightest and the time axis part way
//      in: the strip has to stay aligned with the curves above it, the x labels
//      have to stay under it, and the "0.1" at the top of the axis has to be
//      three characters rather than eleven
//   4  the tab dragged tall, and the whole list visible in one piece -- thirteen
//      entries where there were fifteen. The unit keeps the height it was built
//      with rather than growing into the room, which is what every graph unit
//      here has always done; shot 5 shows an untouched one doing the same
//   5  the Alpha Channel beside it, still one ordinary curve -- what did NOT change
//   6  shot 2's colors under every other editor theme in turn. The three channel
//      hues are the one thing here a theme cannot restyle, so this is where it is
//      settled that they stay legible on a panel that is not the default's
//
// What only a picture can settle: whether three curves layered on one grid are
// still individually readable, whether the strip's gradient reads as the colors
// the curves describe, and whether the x axis labels ended up under the strip
// rather than on top of it. tests/smoke/particleColorGraph.cs does the part that
// is checkable by assertion.
//
// Run: tests/run.ps1 -Shots particleColorGraph ; look in shots/.

setLogMode(1);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

$pcgAssetId = "ToyAssets:bonfire";

// Where "Color Channel" and "Alpha Channel" sit in the emitter list.
$pcgColorIndex = 11;
$pcgAlphaIndex = 12;

testExec("editor/main.cs");
schedule(2500, 0, "pcgOpenProject");

function pcgOpenProject()
{
	ProjectManager.setProjectFolder("PlanetX");
	EditorCore.projectSelector.onProjectSelected(pathConcat(getMainDotCsDir(), "PlanetX"));

	// A copy: this harness writes data keys. tests/run.ps1 sweeps the folder by
	// reading the spelled-out name out of this file.
	createPath(testRoot("shots/"));
	ProjectManager.setProjectFolder("particleColorGraphShotProject");
	EditorPreferences.path = testRoot("shots/particleColorGraphShotPrefs.taml");

	%copy = testRoot("particleColorGraphShotProject/ToyAssets");
	pathCopy(testRoot("toybox/ToyAssets"), %copy, false);
	ModuleDatabase.scanModules(%copy);
	%module = ModuleDatabase.findModule("ToyAssets", 1);
	if(isObject(%module))
	{
		AssetDatabase.addModuleDeclaredAssets(%module);
	}

	schedule(2500, 0, "pcgOpenEditor");
}

function pcgOpenEditor()
{
	EditorCore.toggleEditor();
	EditorCore.tabBook.selectPage(2);
	schedule(1500, 0, "pcgSelectAsset");
}

function pcgSelectAsset()
{
	AssetAdmin.Dictionary["ParticleAsset"].setExpanded(true);
	AssetAdmin.libWindow.relayout();

	%tile = AssetAdmin.Dictionary["ParticleAsset"].getButton($pcgAssetId);
	%tile.onClick();

	$pcgInspector = AssetAdmin.inspector;
	$pcgAsset = %tile.ParticleAsset;

	// The first emitter. The Emitter Graph tab only exists for one.
	$pcgInspector.titleDropDown.setSelected(1);
	$pcgInspector.onChooseParticleAsset($pcgAsset);

	// Room to actually see a graph. The tab starts in a 360 tall bottom frame.
	AssetAdmin.content.setFrameSize(AssetAdmin.inspectorFrameId, 460);

	schedule(1200, 0, "pcgOpenGraph");
}

function pcgGrab(%name)
{
	// screenShot does not create its folder and reports failure by logging, so a
	// tree that has never run a shot writes nothing and says nothing.
	createPath(testRoot("shots/"));
	screenShot(testRoot("shots/particleColorGraph" @ %name @ ".png"), "PNG");
}

// Selecting a row the way the tool's own inspect() does: clear first, or a list
// box that allows more than one selection simply adds to it.
function pcgSelect(%index)
{
	$pcgTool.baseList.clearSelection();
	$pcgTool.baseList.setCurSel(%index);
	$pcgTool.onSelect(%index);
}

function pcgOpenGraph()
{
	$pcgTool = $pcgInspector.emitterGraphPage;

	// Two pages for an emitter, not three: the Scale Graph tab belongs to the
	// effect and is taken out of the book when an emitter is chosen.
	$pcgInspector.tabBook.selectPage(1);

	pcgSelect($pcgColorIndex);

	schedule(1200, 0, "pcgDefaultShot");
}

function pcgDefaultShot()
{
	pcgGrab(0);

	// Green live. Everything about which curve is editable changes; nothing about
	// the strip does, which is the pairing worth reading across the two shots.
	$pcgTool.colorGraph.toggle["Green"].setStateOn(true);
	$pcgTool.colorGraph.onToggleIconChanged($pcgTool.colorGraph.toggle["Green"]);

	schedule(800, 0, "pcgGreenShot");
}

function pcgGreenShot()
{
	pcgGrab(1);

	// A color with somewhere to go. bonfire's channels are close to flat, and a
	// flat gradient says nothing about whether the gradient works.
	%emitter = $pcgAsset.getEmitter(0);

	%emitter.selectField("RedChannel");
	%emitter.clearDataKeys();
	%emitter.setSingleDataKey(1);
	%emitter.addDataKey(0.35, 0.9);
	%emitter.addDataKey(1, 0);

	%emitter.selectField("GreenChannel");
	%emitter.clearDataKeys();
	%emitter.setSingleDataKey(0.1);
	%emitter.addDataKey(0.6, 0.85);
	%emitter.addDataKey(1, 1);

	%emitter.selectField("BlueChannel");
	%emitter.clearDataKeys();
	%emitter.setSingleDataKey(0);
	%emitter.addDataKey(0.8, 0.2);
	%emitter.addDataKey(1, 0.8);

	$pcgAsset.refreshAsset();
	$pcgTool.colorGraph.setToColor($pcgTool.emitterID);

	schedule(800, 0, "pcgRampShot");
}

function pcgRampShot()
{
	pcgGrab(2);

	// The value axis all the way in and the time axis part way. The strip follows
	// the plot's window, so it has to magnify the same stretch of life the curves
	// do -- and the tightest value window is 0 to 0.1, which is where an axis end
	// label printed from a script float used to read "0.100000001" and take a
	// third of the plot's width with it.
	$pcgTool.colorGraph.timeZoomIn();
	$pcgTool.colorGraph.timeZoomIn();
	$pcgTool.colorGraph.timeMoveForward();
	$pcgTool.colorGraph.valueZoomIn();
	$pcgTool.colorGraph.valueZoomIn();
	$pcgTool.colorGraph.valueZoomIn();

	schedule(800, 0, "pcgZoomShot");
}

function pcgZoomShot()
{
	pcgGrab(3);

	// Back out, then give the tab the height a tall inspector has -- enough for the
	// whole list at once, which is where the collapse is easiest to read.
	$pcgTool.colorGraph.timeZoomOut();
	$pcgTool.colorGraph.timeZoomOut();
	$pcgTool.colorGraph.valueZoomOut();
	$pcgTool.colorGraph.valueZoomOut();
	$pcgTool.colorGraph.valueZoomOut();

	%canvas = Canvas.getExtent();
	AssetAdmin.content.setFrameSize(AssetAdmin.inspectorFrameId, getWord(%canvas, 1) - 220);

	schedule(800, 0, "pcgTallShot");
}

function pcgTallShot()
{
	pcgGrab(4);

	// Alpha: unchanged, and the reason the color entry is not simply "all four".
	pcgSelect($pcgAlphaIndex);

	schedule(800, 0, "pcgAlphaShot");
}

function pcgAlphaShot()
{
	pcgGrab(5);

	// Back to the color graph, then walk the themes. The curves and the swatches
	// are fixed hues by design -- no theme can know which curve is the red one --
	// so they are the one thing here that has to be looked at on every panel color
	// rather than trusted to re-theme.
	pcgSelect($pcgColorIndex);
	$pcgTheme = 0;

	schedule(800, 0, "pcgThemeShot");
}

function pcgThemeShot()
{
	if($pcgTheme >= ThemeManager.themeList.getCount())
	{
		echo("SHOTS DONE");
		quit();
		return;
	}

	ThemeManager.setTheme($pcgTheme);
	$pcgTheme++;

	schedule(800, 0, "pcgGrabTheme");
}

function pcgGrabTheme()
{
	pcgGrab("6_" @ ThemeManager.activeTheme.getClassNamespace());

	schedule(400, 0, "pcgThemeShot");
}
