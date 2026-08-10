// Visual harness for the Asset Manager's animation editor. Four shots:
//
//   0  the stage as it opens -- preview and transport left, the image's frames
//      right, the timeline along the bottom of both
//   1  a timeline with a hold in it, to judge how a run of one frame reads
//   2  the insertion caret mid-hover, which is the promise a drop then keeps
//   3  an image asset selected, where the split must collapse back to one preview
//
// What only a picture can settle: whether the three sections balance at the size
// the editor opens at, whether a run of repeated frames reads as one held pose
// rather than as a mistake, and whether the caret is findable against the art.
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
	aaGrab("assetAnimation3");

	echo("SHOTS DONE");
	schedule(300, 0, "quit");
}
