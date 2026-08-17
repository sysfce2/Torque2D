// Real clicks on the animation editor's two grids: one on a palette frame,
// which must append it, and one on a timeline slot, which must select it and
// scrub the preview there.
// Run: tests/run.ps1 assetAnimationClick  ; grep ACLK in tests/logs/.
//
// These have to be real. What is being checked is the touch path in
// GuiEditFrameStripCtrl and its two subclasses -- the press, the five pixels of
// slop that decide whether it was a click or a drag, the capture taken and given
// back, and the suppression that stops a released drag also counting as a click.
// Calling onFrameClicked from script would skip every one of them, which is what
// tests/smoke/assetAnimationTimeline.cs already does.
//
// The DRAG from the palette to the timeline is deliberately not here. A
// GuiDragAndDropCtrl gesture cannot be driven by posted mouse messages -- it
// follows the real cursor, which a posted WM_MOUSEMOVE does not move -- so the
// drop path is tested by calling the callbacks with a payload parked at real
// coordinates, in that same suite. That is where its traps are anyway: the
// boundary policing and the cursor-from-the-payload conversion are script, and
// the capture is the engine's.
//
// Neither point is written here. Where a cell lands depends on the reflow and on
// the theme's borders, so the engine works it out and leaves it in a file for the
// PowerShell side. A hard-coded point that drifted off the cell would report a
// control that never fired -- which is precisely what a broken hit test reports.

setLogMode(1);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

function clkCheck(%label, %cond)
{
	if(%cond) echo("ACLK PASS: " @ %label);
	else      echo("ACLK FAIL: " @ %label);
}

$clkAnimId = "ToyAssets:TD_Barbarian_Death";

function clkPostTarget(%point)
{
	createPath(testRoot("shots/"));
	%file = new FileObject();
	%file.openForWrite(testRoot("shots/assetAnimationClickTarget.txt"));
	%file.writeLine(%point);
	%file.close();
	%file.delete();
}

function clkCentreOf(%rect)
{
	return (getWord(%rect, 0) + (getWord(%rect, 2) / 2)) SPC
		(getWord(%rect, 1) + (getWord(%rect, 3) / 2));
}

testExec("editor/main.cs");
schedule(2500, 0, "clkOpenProject");

// The long way in, and it has to be. A posted click goes to whatever is actually
// on screen, so an editor opened the short way leaves the clicks landing on the
// project selector -- which reads exactly like a broken hit test.
function clkOpenProject()
{
	ProjectManager.setProjectFolder("PlanetX");
	EditorCore.projectSelector.onProjectSelected(pathConcat(getMainDotCsDir(), "PlanetX"));

	createPath(testRoot("shots/"));
	ProjectManager.setProjectFolder("assetAnimationClickSmokeProject");
	EditorPreferences.path = testRoot("shots/assetAnimationClickSmokePrefs.taml");

	%copy = testRoot("assetAnimationClickSmokeProject/ToyAssets");
	pathCopy(testRoot("toybox/ToyAssets"), %copy, false);
	ModuleDatabase.scanModules(%copy);
	%module = ModuleDatabase.findModule("ToyAssets", 1);
	if(isObject(%module))
	{
		AssetDatabase.addModuleDeclaredAssets(%module);
	}

	schedule(2500, 0, "clkOpenEditor");
}

function clkOpenEditor()
{
	EditorCore.toggleEditor();
	EditorCore.tabBook.selectPage(2);
	schedule(1500, 0, "clkSelectAsset");
}

function clkSelectAsset()
{
	AssetAdmin.Dictionary["AnimationAsset"].setExpanded(true);
	AssetAdmin.libWindow.relayout();

	%tile = AssetAdmin.Dictionary["AnimationAsset"].getButton($clkAnimId);
	clkCheck("the animation tile is in the library", isObject(%tile));
	%tile.onClick();

	$clkStage = AssetAdmin.animationStage;
	schedule(1200, 0, "clkAimAtPalette");
}

//-----------------------------------------------------------------------------
// A click on a palette frame appends it.
//-----------------------------------------------------------------------------

function clkAimAtPalette()
{
	clkCheck("the stage is built", $clkStage.built);

	// A short, known list so the append is unambiguous.
	$clkStage.timelinePane.setFrames("10 11 12");
	$clkBefore = $clkStage.timelinePane.strip.getCellCount();

	%rect = $clkStage.palettePane.strip.getCellRect(5);
	clkCheck("the palette reported where frame 5 is (" @ %rect @ ")", getWord(%rect, 2) > 0);

	clkPostTarget(clkCentreOf(%rect));
	schedule(2000, 0, "clkCheckPalette");
}

function clkCheckPalette()
{
	%strip = $clkStage.timelinePane.strip;

	clkCheck("clicking a palette frame appended one (" @ %strip.getFrames() @ ")",
		%strip.getCellCount() == $clkBefore + 1);
	clkCheck("and it appended the frame that was clicked",
		%strip.getFrameAt(%strip.getCellCount() - 1) == 5);
	clkCheck("the asset was written",
		trim($clkStage.animationAsset.getAnimationFrames()) $= %strip.getFrames());

	schedule(400, 0, "clkAimAtTimeline");
}

//-----------------------------------------------------------------------------
// A click on a timeline slot selects it and scrubs the preview to it.
//-----------------------------------------------------------------------------

function clkAimAtTimeline()
{
	$clkStage.play();

	%rect = $clkStage.timelinePane.strip.getSlotRect(2);
	clkCheck("the timeline reported where slot 2 is (" @ %rect @ ")", getWord(%rect, 2) > 0);

	clkPostTarget(clkCentreOf(%rect));
	schedule(2000, 0, "clkCheckTimeline");
}

function clkCheckTimeline()
{
	clkCheck("clicking a slot selects it (" @ $clkStage.timelinePane.strip.getSelectedSlot() @ ")",
		$clkStage.timelinePane.strip.getSelectedSlot() == 2);

	// Stopping first is deliberate: scrubbing a running preview shows the frame
	// for a thirtieth of a second and then moves on, which reads as the click
	// having done nothing.
	clkCheck("and stops the preview", !$clkStage.playing);
	clkCheck("and scrubs it to that slot (" @ AssetAdmin.previewSprite.getAnimationFrame() @ ")",
		AssetAdmin.previewSprite.getAnimationFrame() == 2);

	echo("ACLK DONE");
	schedule(300, 0, "quit");
}
