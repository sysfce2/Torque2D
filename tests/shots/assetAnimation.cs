// Visual harness for the Asset Manager's animation editor. Seven shots:
//
//   0  the stage as it opens -- preview and transport left, the image's frames
//      right, the timeline along the bottom of both
//   1  a timeline with a hold in it, to judge how a run of one frame reads
//   2  the insertion caret mid-hover, which is the promise a drop then keeps
//   3  the frame range dialog, showing a ping-pong read back before it is applied
//   4  an image asset selected, where the split must collapse back to one preview
//   5  a NAMED animation, where every cell is labelled with its region name
//   6  the same with a cell deleted, so one frame has a name and no art
//
// What only a picture can settle: whether the three sections balance at the size
// the editor opens at, whether a run of repeated frames reads as one held pose
// rather than as a mistake, whether the caret is findable against the art, and
// whether a cell name is legible at 48 pixels over the frame it labels.
// tests/smoke/assetAnimationTimeline.cs does the checkable half.
//
// Run: tests/run.ps1 -Shots assetAnimation ; look in shots/.
//
// NOTE: a COPY of toybox/ToyAssets, never the module itself. Editing a timeline
// writes the .animation.taml straight back to its own file.

setLogMode(1);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

// The barbarian death animation: 25 frames drawn from a 10 x 10 sheet, and the
// "death is frames 28 to 32" case that started all this.
$aaAnimId = "ToyAssets:TD_Barbarian_Death";
$aaImageId = "ToyAssets:TD_Barbarian_CompSprite";

// The named pair: a 2x2 sheet cut explicitly into block1..block4.
$aaNamedAnimId = "ToyAssets:1234Animation";
$aaNamedImageId = "ToyAssets:1234";

testExec("editor/main.cs");
schedule(2500, 0, "aaOpenProject");

function aaOpenProject()
{
	ProjectManager.setProjectFolder("PlanetX");
	EditorCore.projectSelector.onProjectSelected(pathConcat(getMainDotCsDir(), "PlanetX"));

	createPath(testRoot("shots/"));
	ProjectManager.setProjectFolder("assetAnimationShotProject");
	EditorPreferences.path = testRoot("shots/assetAnimationShotPrefs.taml");

	%copy = testRoot("assetAnimationShotProject/ToyAssets");
	pathCopy(testRoot("toybox/ToyAssets"), %copy, false);
	ModuleDatabase.scanModules(%copy);
	%module = ModuleDatabase.findModule("ToyAssets", 1);
	if(isObject(%module))
	{
		AssetDatabase.addModuleDeclaredAssets(%module);
	}

	schedule(2500, 0, "aaOpenEditor");
}

// Pages register in load order: EditorConsole, ProjectManager, AssetAdmin,
// GuiEditor.
function aaOpenEditor()
{
	EditorCore.toggleEditor();
	EditorCore.tabBook.selectPage(2);
	schedule(1500, 0, "aaSelectAsset");
}

function aaSelectAsset()
{
	AssetAdmin.Dictionary["AnimationAsset"].setExpanded(true);
	AssetAdmin.libWindow.relayout();

	$aaTile = AssetAdmin.Dictionary["AnimationAsset"].getButton($aaAnimId);
	$aaTile.onClick();

	$aaStage = AssetAdmin.animationStage;
	schedule(1200, 0, "aaOpeningShot");
}

function aaGrab(%name)
{
	// screenShot does not create its folder and reports failure by logging, so a
	// tree that has never run a shot writes nothing and says nothing.
	screenShot(testRoot("shots/" @ %name @ ".png"), "PNG");
}

function aaOpeningShot()
{
	aaGrab("assetAnimation0");
	schedule(400, 0, "aaHoldShot");
}

function aaHoldShot()
{
	$aaStage.timelinePane.setFrames("55 56 56 56 57 58 59");

	// A picked slot and a playhead somewhere else, so the shot shows both at once
	// -- they are frequently the same cell and have to stay tellable apart.
	$aaStage.stop();
	$aaStage.scrubTo(5);
	$aaStage.timelinePane.strip.setSelectedSlot(1);

	schedule(500, 0, "aaGrabHold");
}

function aaGrabHold()
{
	aaGrab("assetAnimation1");

	// A shot only needs the paint, so the caret is asked for directly rather than
	// driven by a real drag -- tests/smoke/assetAnimationDrag.cs does that part
	// with real posted input.
	%slotRect = $aaStage.timelinePane.strip.getSlotRect(2);
	%x = getWord(%slotRect, 0) + 4;
	%y = getWord(%slotRect, 1) + (getWord(%slotRect, 3) / 2);
	$aaStage.timelinePane.strip.showCaretAt(%x SPC %y);

	schedule(400, 0, "aaGrabCaret");
}

function aaGrabCaret()
{
	aaGrab("assetAnimation2");
	$aaStage.timelinePane.strip.clearCaret();

	// The range dialog, with a ping-pong in the feedback line -- the thing that
	// makes step, hold and ping-pong comprehensible without explaining the rules.
	$aaStage.openRangeDialog();
	schedule(500, 0, "aaFillRangeDialog");
}

function aaFillRangeDialog()
{
	// A pushed dialog is the last thing on the canvas.
	$aaDialog = Canvas.getObject(Canvas.getCount() - 1);
	if(isObject($aaDialog))
	{
		$aaDialog.startBox.setText(28);
		$aaDialog.endBox.setText(32);
		$aaDialog.pingPongBox.setStateOn(true);
		$aaDialog.validate();
	}

	schedule(400, 0, "aaGrabRange");
}

function aaGrabRange()
{
	aaGrab("assetAnimation3");

	if(isObject($aaDialog))
	{
		$aaDialog.onClose();
	}

	schedule(400, 0, "aaCollapse");
}

function aaCollapse()
{
	// Selecting anything else must take the split back down to one preview.
	AssetAdmin.Dictionary["ImageAsset"].setExpanded(true);
	AssetAdmin.libWindow.relayout();

	%imageTile = AssetAdmin.Dictionary["ImageAsset"].getButton($aaImageId);
	if(isObject(%imageTile))
	{
		%imageTile.onClick();
	}

	schedule(800, 0, "aaGrabCollapsed");
}

function aaGrabCollapsed()
{
	aaGrab("assetAnimation4");

	// The named-cells case: 1234Animation lists four cells of an explicitly cut
	// sheet by name, so every cell in both grids is labelled block1..block4 rather
	// than 0..3. Only a picture can settle whether a word reads at 48 pixels, where
	// the ellipsis falls, and whether a name is legible against the art under it.
	AssetAdmin.Dictionary["AnimationAsset"].setExpanded(true);
	AssetAdmin.libWindow.relayout();

	%namedTile = AssetAdmin.Dictionary["AnimationAsset"].getButton($aaNamedAnimId);
	if(isObject(%namedTile))
	{
		%namedTile.onClick();
	}

	schedule(1200, 0, "aaGrabNamed");
}

function aaGrabNamed()
{
	aaGrab("assetAnimation5");

	// And a frame whose cell has gone, which draws as an outlined empty slot in
	// the theme's error color with the name it could not find still under it.
	$aaNamedImage = AssetDatabase.acquireAsset($aaNamedImageId);
	if(isObject($aaNamedImage))
	{
		$aaNamedImage.removeExplicitCell(1);
	}

	schedule(700, 0, "aaGrabMissing");
}

function aaGrabMissing()
{
	aaGrab("assetAnimation6");

	if(isObject($aaNamedImage))
	{
		$aaNamedImage.insertExplicitCell(1, 32, 0, 32, 32, "block2");
		AssetDatabase.releaseAsset($aaNamedImageId);
	}

	echo("SHOTS DONE");
	schedule(300, 0, "quit");
}
