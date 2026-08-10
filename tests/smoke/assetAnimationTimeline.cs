// The Asset Manager's animation editor: the three-way split, the palette, the
// timeline, and the collapse back to a plain preview.
// Run: tests/run.ps1 assetAnimationTimeline  ; grep AANI in tests/logs/.
//
// Driven by calling the panes rather than by posting input. Where a cell lands
// depends on the reflow and on how far the scroller has been dragged, neither of
// which script can read -- so a click at a computed point would be testing the
// arithmetic in this file. tests/smoke/assetAnimationDrag.cs does the one gesture
// that genuinely needs a real pointer.
//
// NOTE: a COPY of toybox/ToyAssets, never the module itself. Every timeline edit
// writes the .animation.taml straight back to its own file.

setLogMode(1);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

function aniCheck(%label, %cond)
{
	if(%cond) echo("AANI PASS: " @ %label);
	else      echo("AANI FAIL: " @ %label);
}

// The barbarian death animation: 25 frames drawn from a 10 x 10 sheet.
$aniAnimId = "ToyAssets:TD_Barbarian_Death";
$aniImageId = "ToyAssets:TD_Barbarian_CompSprite";

function aniLoadFixtureAssets()
{
	%copy = testRoot("assetAnimationTimelineSmokeProject/ToyAssets");

	if(!pathCopy(testRoot("toybox/ToyAssets"), %copy, false))
	{
		return false;
	}

	ModuleDatabase.scanModules(%copy);
	%module = ModuleDatabase.findModule("ToyAssets", 1);
	if(!isObject(%module))
	{
		return false;
	}
	AssetDatabase.addModuleDeclaredAssets(%module);
	return true;
}

testExec("editor/main.cs");
schedule(2000, 0, "aniStep1");

//-----------------------------------------------------------------------------

function aniStep1()
{
	ProjectManager.setProjectFolder("assetAnimationTimelineSmokeProject");
	EditorPreferences.path = testRoot("shots/assetAnimationTimelineSmokePrefs.taml");
	createPath(testRoot("shots/"));

	aniCheck("fixture asset module registered", aniLoadFixtureAssets());

	EditorCore.tabBook.selectPage(2);

	schedule(700, 0, "aniStep2");
}

//-----------------------------------------------------------------------------
// The stage stands down until an animation is chosen.
//-----------------------------------------------------------------------------

function aniStep2()
{
	$aniStage = AssetAdmin.animationStage;

	aniCheck("the animation stage exists", isObject($aniStage));
	aniCheck("it starts collapsed", !$aniStage.built);

	// Eight words per frame, so one frame is a frame set that has never split.
	aniCheck("the preview is one frame to begin with",
		getWordCount(AssetAdmin.previewFrames.getFrameLayout()) == 8);

	$aniTile = AssetAdmin.Dictionary["AnimationAsset"].getButton($aniAnimId);
	aniCheck("the animation tile is in the library", isObject($aniTile));

	$aniTile.onClick();

	schedule(600, 0, "aniStep3");
}

//-----------------------------------------------------------------------------
// Choosing one splits the preview three ways.
//-----------------------------------------------------------------------------

function aniStep3()
{
	aniCheck("the stage built", $aniStage.built);
	aniCheck("the preview is split", getWordCount(AssetAdmin.previewFrames.getFrameLayout()) > 8);

	aniCheck("the palette pane exists", isObject($aniStage.palettePane));
	aniCheck("the timeline pane exists", isObject($aniStage.timelinePane));

	if(!isObject($aniStage.palettePane) || !isObject($aniStage.timelinePane))
	{
		echo("AANI DONE");
		schedule(200, 0, "quit");
		return;
	}

	$aniPalette = $aniStage.palettePane.strip;
	$aniTimeline = $aniStage.timelinePane.strip;

	// The panes must be sized by their frames, not left at the extent they were
	// built with. setFrameSize is the only thing that lays the tree out, so
	// sizing the frames before the panes were added produced exactly this: the
	// right frames, and a palette still 100 x 100 and parked behind the preview
	// where nothing could be seen of it.
	aniCheck("the palette pane was sized by its frame",
		$aniStage.palettePane.getExtent() !$= "100 100");
	aniCheck("the palette pane sits to the right of the preview",
		getWord($aniStage.palettePane.getGlobalPosition(), 0) > 0);
	aniCheck("the timeline pane sits below it",
		getWord($aniStage.timelinePane.getGlobalPosition(), 1) >
			getWord($aniStage.palettePane.getGlobalPosition(), 1));

	aniCheck("the palette shows every frame of the image (100)",
		$aniPalette.getImageFrameCount() == 100);
	// 100 frames at four columns is far taller than the pane, which is what gives
	// the scroller something to scroll.
	aniCheck("the palette is taller than the room it has",
		getWord($aniPalette.getExtent(), 1) > getWord($aniStage.palettePane.getExtent(), 1));
	aniCheck("the palette has a cell per frame",
		$aniPalette.getCellCount() == 100);
	aniCheck("the timeline holds the animation's 25 frames",
		$aniTimeline.getCellCount() == 25);
	aniCheck("the timeline matches what the asset says",
		$aniTimeline.getFrames() $= trim($aniStage.animationAsset.getAnimationFrames()));

	schedule(300, 0, "aniStep4");
}

//-----------------------------------------------------------------------------
// Editing, and the file that gets written.
//-----------------------------------------------------------------------------

function aniStep4()
{
	%asset = $aniStage.animationAsset;

	// A palette click appends, which is the same path a drop ends in.
	$aniStage.palettePane.strip.onFrameClicked(7);

	aniCheck("clicking a palette frame appends it", $aniTimeline.getCellCount() == 26);
	aniCheck("it went on the end", $aniTimeline.getFrameAt(25) == 7);
	aniCheck("the asset was written", trim(%asset.getAnimationFrames()) $= $aniTimeline.getFrames());

	// Removing.
	$aniTimeline.setSelectedSlot(0);
	$aniTimeline.removeSlot(0);
	$aniStage.timelinePane.commitFrames();

	aniCheck("removing a slot shortens the list", $aniTimeline.getCellCount() == 25);
	aniCheck("the asset followed", trim(%asset.getAnimationFrames()) $= $aniTimeline.getFrames());

	// Reordering.
	$aniTimeline.setFrames("10 11 12");
	$aniStage.timelinePane.commitFrames();
	$aniTimeline.moveSlot(0, 3);
	$aniStage.timelinePane.commitFrames();

	aniCheck("a slot moved to the end", $aniTimeline.getFrames() $= "11 12 10");

	schedule(300, 0, "aniStep5");
}

//-----------------------------------------------------------------------------
// Selecting anything else takes the split down again.
//-----------------------------------------------------------------------------

function aniStep5()
{
	%imageTile = AssetAdmin.Dictionary["ImageAsset"].getButton($aniImageId);
	aniCheck("the image tile is in the library", isObject(%imageTile));

	%imageTile.onClick();

	schedule(500, 0, "aniStep6");
}

function aniStep6()
{
	aniCheck("the stage collapsed", !$aniStage.built);
	aniCheck("the preview is one frame again",
		getWordCount(AssetAdmin.previewFrames.getFrameLayout()) == 8);

	// The guarantee that makes the whole restructure safe: the preview window and
	// its scene are the same objects they always were.
	aniCheck("the preview window survived", isObject(AssetAdmin.assetWindow));
	aniCheck("it still has its scene", AssetAdmin.assetWindow.getScene() == AssetAdmin.assetScene);

	// And it rebuilds.
	$aniTile.onClick();

	schedule(500, 0, "aniStep7");
}

function aniStep7()
{
	aniCheck("choosing the animation again rebuilds the split", $aniStage.built);
	aniCheck("the preview is split again",
		getWordCount(AssetAdmin.previewFrames.getFrameLayout()) > 8);

	echo("AANI DONE");
	schedule(200, 0, "quit");
}
