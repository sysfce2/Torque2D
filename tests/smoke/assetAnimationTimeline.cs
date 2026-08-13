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

	// Spelled out rather than inherited. The preferences file lives in shots/ and
	// survives between runs, so a suite that left this on would change what every
	// commit below does to the animation's time.
	EditorPreferences.set("assetAnimationKeepFrameRate", false);

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

	// A sprite built with an Animation on it is already running -- nothing
	// pressed play -- so choosing an animation has to arrive with the transport
	// already saying Stop. It said Play over a preview that was busy playing,
	// because the flag was initialised false rather than read off the sprite.
	aniCheck("choosing an animation arrives playing", $aniStage.playing);
	aniCheck("so the transport offers Stop", AssetAdmin.transportBar.stopButton.isVisible());
	aniCheck("and not Play", !AssetAdmin.transportBar.playButton.isVisible());

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

	schedule(300, 0, "aniStepPreview");
}

//-----------------------------------------------------------------------------
// The refresh storm. Every timeline edit writes the asset, and the editor's
// answer to a written asset used to be "rebuild the preview from nothing" --
// which would restart the animation on every dragged frame.
//-----------------------------------------------------------------------------

function aniStepPreview()
{
	$aniTimeline.setFrames("20 21 22 23 24 25 26 27");
	$aniStage.timelinePane.commitFrames();

	$aniSprite = AssetAdmin.previewSprite;
	aniCheck("there is a preview sprite", isObject($aniSprite));

	$aniStage.play();
	schedule(400, 0, "aniStepPreview2");
}

function aniStepPreview2()
{
	// Park it somewhere that is not the start, so a restart would be obvious.
	$aniStage.stop();
	$aniStage.scrubTo(5);

	aniCheck("the preview scrubbed to slot 5", $aniSprite.getAnimationFrame() == 5);

	// An edit, which writes the file and comes straight back through onRefresh.
	$aniStage.timelinePane.appendFrame(28);

	aniCheck("the edit landed", $aniTimeline.getCellCount() == 9);
	aniCheck("the preview sprite was NOT rebuilt", AssetAdmin.previewSprite == $aniSprite);
	aniCheck("the playhead did not jump back to the start (" @ $aniSprite.getAnimationFrame() @
		" of " @ $aniTimeline.getCellCount() @ ")", $aniSprite.getAnimationFrame() == 5);
	aniCheck("the preview is still paused", !$aniStage.playing);

	// And a refresh raised from somewhere else entirely still reaches the strip.
	$aniStage.animationAsset.setAnimationFrames("30 31 32");

	aniCheck("an outside change reloads the timeline", $aniTimeline.getFrames() $= "30 31 32");
	aniCheck("and still did not rebuild the preview", AssetAdmin.previewSprite == $aniSprite);

	schedule(300, 0, "aniStepDrop");
}

//-----------------------------------------------------------------------------
// The drop, and the two traps in GuiDragAndDropCtrl that shape it.
//
// The callbacks are called directly with a real payload parked at real
// coordinates. That is not a shortcut around the interesting part -- it IS the
// interesting part. GuiDragAndDropCtrl hit-tests from its own parent and
// findHitControl answers "me" without testing its bounds, so the drop arrives
// here from anywhere on screen and the boundary check is the timeline's own.
//-----------------------------------------------------------------------------

function aniMakePayload(%frame, %globalPoint)
{
	// The drop reads the cursor from the payload's middle, because the position
	// it is handed is in the drag control's parent's space and useless here.
	%payload = new GuiSpriteCtrl()
	{
		Position = "0 0";
		Extent = "40 40";
		frameIndex = %frame;
	};
	ThemeManager.setProfile(%payload, "emptyProfile");
	AssetAdmin.content.add(%payload);

	%payload.setPosition(getWord(%globalPoint, 0) - 20, getWord(%globalPoint, 1) - 20);
	return %payload;
}

function aniStepDrop()
{
	%pane = $aniStage.timelinePane;
	$aniStage.timelinePane.setFrames("10 11 12 13");

	// What the thing under the cursor is actually showing.
	//
	// It showed frame 0 whatever was picked up, and only became right once it was
	// dropped. GuiSpriteCtrl::setImage returns early when the control is not
	// awake -- it keeps the asset id and discards the frame -- and a payload is
	// built detached, so the frame never landed. onWake then re-applies the image
	// from the Frame FIELD, which nothing had set.
	//
	// Asserted on the real makePayload rather than on a sprite built here, because
	// the bug was entirely in how that one is built.
	%payload = $aniStage.palettePane.strip.makePayload(7);
	AssetAdmin.content.add(%payload);

	aniCheck("a drag payload shows the frame it was made for (" @ %payload.getImageFrame() @ ")",
		%payload.getImageFrame() == 7);
	aniCheck("and carries it for the drop", %payload.frameIndex == 7);
	%payload.delete();

	// Parked over the library, which is nowhere near the timeline.
	%away = AssetAdmin.libWindow.getGlobalPosition();
	%payload = aniMakePayload(99,
		(getWord(%away, 0) + 40) SPC (getWord(%away, 1) + 40));

	%pane.onControlDropped(%payload, "0 0");
	aniCheck("a drop outside the timeline changes nothing",
		$aniTimeline.getFrames() $= "10 11 12 13");
	%payload.delete();

	// Over the left half of slot 2, which is before it.
	%slotRect = $aniTimeline.getSlotRect(2);
	%point = (getWord(%slotRect, 0) + 2) SPC
		(getWord(%slotRect, 1) + (getWord(%slotRect, 3) / 2));

	%payload = aniMakePayload(99, %point);
	%pane.onControlDropped(%payload, "0 0");
	aniCheck("a drop on a slot's left half goes in before it (" @ $aniTimeline.getFrames() @ ")",
		$aniTimeline.getFrames() $= "10 11 99 12 13");
	%payload.delete();

	// And the caret agrees with where the drop landed, which is the promise the
	// user was shown.
	%payload = aniMakePayload(98, %point);
	aniCheck("the caret shows the slot the drop would use",
		$aniTimeline.showCaretAt(%payload.getGlobalPosition()) >= 0);
	%pane.onControlDragExit(%payload, "0 0");
	%payload.delete();

	schedule(300, 0, "aniStepTransport");
}

//-----------------------------------------------------------------------------
// The transport, and the trap that makes a stopped preview unscrubbable.
//-----------------------------------------------------------------------------

function aniStepTransport()
{
	%bar = AssetAdmin.transportBar;
	aniCheck("the transport bar exists", isObject(%bar));
	aniCheck("it is on show for an animation", AssetAdmin.transportBarContainer.isVisible());

	$aniStage.timelinePane.setFrames("40 41 42 43 44 45");

	// The bar is as tall as its tallest button, and the tallest button fits in it.
	//
	// A chain does not grow to fit a taller child -- and worse, GuiChainCtrl is
	// born VERTICAL and its resize refuses to change whichever axis is currently
	// the length, so an Extent set before IsVertical silently kept the
	// constructor's 30. The big play button was then centred in 30 and lost three
	// pixels off each end.
	%barHeight = getWord(%bar.getExtent(), 1);
	%playHeight = getWord(%bar.playButton.getExtent(), 1);

	aniCheck("the bar is as tall as its biggest button (" @ %barHeight @ " vs " @ %playHeight @ ")",
		%barHeight >= %playHeight);
	aniCheck("so the play button is not clipped at the top",
		getWord(%bar.playButton.getPosition(), 1) >= 0);
	aniCheck("nor at the bottom",
		getWord(%bar.playButton.getPosition(), 1) + %playHeight <= %barHeight);

	// A toggle draws a box clamped inside its borders; a push button paints its
	// whole rect. Same extent means the toggle LOOKS smaller, so it is built
	// bigger by exactly the border and the two come out matching.
	%inset = %bar.chromeInset();
	%toggleDrawn = getWord(%bar.loopButton.getExtent(), 0) - getWord(%inset, 0);

	aniCheck("a toggle draws the same size as a push button (" @ %toggleDrawn @ ")",
		%toggleDrawn == $EditorTransportBar::buttonSize);

	// Every button draws the same size of picture, whatever size the button is.
	//
	// GuiSpriteCtrl::growTo animates the PICTURE and leaves the sprite control
	// alone, so a button that scaled both together had its icon animate from 32
	// down to 28 on the first hover -- it looked like the icon exploded and never
	// went back. The big play button is a bigger button, not a bigger icon.
	%rewindIcon = %bar.getObject(0).icon;
	aniCheck("the play button's picture matches the small buttons' (" @
		%bar.playButton.icon.imageSize @ " vs " @ %rewindIcon.imageSize @ ")",
		%bar.playButton.icon.imageSize $= %rewindIcon.imageSize);
	aniCheck("and so does a toggle's",
		%bar.loopButton.icon.imageSize $= %rewindIcon.imageSize);

	// The sprite holding it must be bigger than the picture at its hovered size,
	// or the hover is clamped away by the sprite's own content rect.
	aniCheck("the picture has room to grow into (" @ %rewindIcon.getExtent() @ ")",
		getWord(%rewindIcon.getExtent(), 0) >=
			getWord(%rewindIcon.imageSize, 0) + $EditorIconButton::hoverGrowth);

	// Play and Stop are two buttons with one hidden, not one toggle, so "which is
	// on show" is the only state there is and it cannot fall out of step.
	//
	// Stopped explicitly rather than relying on an earlier step having done it,
	// so this cannot pass by inheritance.
	$aniStage.stop();
	aniCheck("it offers Play while stopped", %bar.playButton.isVisible());
	aniCheck("and not Stop", !%bar.stopButton.isVisible());

	$aniStage.play();
	aniCheck("play starts it", $aniStage.playing);
	aniCheck("and the button becomes Stop", %bar.stopButton.isVisible());
	aniCheck("with Play hidden", !%bar.playButton.isVisible());

	$aniStage.stop();
	aniCheck("stop halts it", !$aniStage.playing);
	aniCheck("and the button goes back to Play", %bar.playButton.isVisible());

	// The button has to follow every path that stops the preview, not just the
	// one that is a button. Clicking a slot stops in order to scrub, and used to
	// leave Stop showing over something that had stopped.
	$aniStage.play();
	$aniStage.onSlotSelected(2, $aniTimeline.getFrameAt(2));
	aniCheck("clicking a slot stops the preview", !$aniStage.playing);
	aniCheck("and the button followed it", %bar.playButton.isVisible());

	// So does a one-shot animation reaching its end on its own.
	$aniStage.play();
	$aniStage.onPreviewFinished();
	aniCheck("an animation finishing stops it", !$aniStage.playing);
	aniCheck("and the button followed that too", %bar.playButton.isVisible());

	$aniStage.scrubTo(3);
	aniCheck("scrubbing moves the preview", $aniSprite.getAnimationFrame() == 3);

	// The trap, asserted head on. SpriteBase::stopAnimation sets the finished
	// flag, and ImageFrameProviderCore::updateAnimation returns immediately on it
	// -- so setAnimationFrame is dead and the preview could never be scrubbed
	// again. armPreview is the only way back, and every path that moves the
	// playhead goes through it.
	$aniSprite.stopAnimation();
	$aniStage.scrubTo(1);
	aniCheck("scrubbing still works after the animation has finished",
		$aniSprite.getAnimationFrame() == 1);

	// Rewind leaves the playing state alone.
	%bar.rewind();
	aniCheck("rewind goes to the first slot", $aniSprite.getAnimationFrame() == 0);

	schedule(300, 0, "aniStepToggles");
}

//-----------------------------------------------------------------------------
// The two toggles: one writes the asset, one is an editor preference.
//-----------------------------------------------------------------------------

function aniStepToggles()
{
	%asset = $aniStage.animationAsset;

	%wasCycling = %asset.getAnimationCycle();
	$aniStage.setCycle(!%wasCycling);
	aniCheck("the loop toggle writes the asset's cycle flag",
		%asset.getAnimationCycle() == !%wasCycling);
	$aniStage.setCycle(%wasCycling);

	// Keep frame rate off: the count changes and the time does not.
	EditorPreferences.set("assetAnimationKeepFrameRate", false);
	$aniStage.timelinePane.setFrames("0 1 2 3");
	%asset.setAnimationTime(1.0);

	$aniStage.timelinePane.appendFrame(4);
	aniCheck("with keep-rate off the animation time is left alone",
		mAbs(%asset.getAnimationTime() - 1.0) < 0.001);

	// Keep frame rate on: 4 frames at 1.0s becomes 5 frames at 1.25s, so each
	// frame still lasts a quarter of a second.
	EditorPreferences.set("assetAnimationKeepFrameRate", true);
	$aniStage.timelinePane.setFrames("0 1 2 3");
	%asset.setAnimationTime(1.0);

	$aniStage.timelinePane.appendFrame(4);
	aniCheck("with keep-rate on the time grows with the frame count (" @
		%asset.getAnimationTime() @ " from " @ $aniTimeline.getCellCount() @ " frames)",
		mAbs(%asset.getAnimationTime() - 1.25) < 0.001);

	EditorPreferences.set("assetAnimationKeepFrameRate", false);

	schedule(200, 0, "aniStepRange");
}

//-----------------------------------------------------------------------------
// The range builder. Eight rows, no dialog: it is a pure function and the
// dialog's feedback line is this same answer read back.
//-----------------------------------------------------------------------------

function aniStepRange()
{
	%r = AssetAdmin.frameRange;

	aniCheck("a plain range", %r.build(28, 32, 1, 1, false) $= "28 29 30 31 32");
	aniCheck("a reversed range counts down", %r.build(32, 28, 1, 1, false) $= "32 31 30 29 28");
	aniCheck("a step skips frames", %r.build(0, 8, 2, 1, false) $= "0 2 4 6 8");
	aniCheck("a hold repeats each frame", %r.build(0, 3, 1, 2, false) $= "0 0 1 1 2 2 3 3");

	// Both ends appear once, not twice: keeping them would hold the turn at each
	// end for twice as long as every other frame, which reads as a stutter.
	aniCheck("ping-pong drops the shared end frames", %r.build(0, 3, 1, 1, true) $= "0 1 2 3 2 1");

	// Hold applies AFTER ping-pong, so "shared" means one frame, not N slots.
	aniCheck("hold applies to the ping-pong too",
		%r.build(0, 3, 1, 2, true) $= "0 0 1 1 2 2 3 3 2 2 1 1");

	aniCheck("a single frame cannot ping-pong", %r.build(0, 0, 1, 1, true) $= "0");
	aniCheck("a step past the end still gives the first frame", %r.build(5, 5, 3, 1, false) $= "5");

	// And it refuses what it should.
	aniCheck("a frame outside the image is refused",
		%r.problemWith(0, 500, 1, 1, false, 100) !$= "");
	aniCheck("an enormous hold is refused",
		%r.problemWith(0, 99, 1, 100, false, 100) !$= "");
	aniCheck("a sensible range is not refused",
		%r.problemWith(28, 32, 1, 1, false, 100) $= "");

	// Applied through the stage, both ways.
	$aniStage.timelinePane.setFrames("1 2");
	$aniStage.appendFrames(%r.build(28, 30, 1, 1, false));
	aniCheck("appending adds to what is there",
		$aniTimeline.getFrames() $= "1 2 28 29 30");

	$aniStage.setFrames(%r.build(28, 30, 1, 1, false));
	aniCheck("replacing does not", $aniTimeline.getFrames() $= "28 29 30");

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
