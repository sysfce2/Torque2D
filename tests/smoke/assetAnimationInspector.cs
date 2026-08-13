// Asset Manager animation-inspector smoke test. Drives the custom pane that
// replaced the generic GuiInspector for animation assets: the three reflowing
// blocks, the field that is deliberately NOT offered, the derived info line, and
// each of the warnings.
// Run: tests/run.ps1 assetAnimationInspector  ; grep AAIN in tests/logs/.
//
// Driven by calling the pane rather than by posting input, for the same reason
// assetImageInspector is: where a row sits depends on how many columns the grid
// chose and how far the scroller has been dragged, neither of which script can
// read.
//
// NOTE: a COPY of toybox/ToyAssets. Committing a field writes the asset straight
// back to its own file -- every setter ends in refreshAsset.

setLogMode(1);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

function ainCheck(%label, %cond)
{
	if(%cond) echo("AAIN PASS: " @ %label);
	else      echo("AAIN FAIL: " @ %label);
}

// 25 frames from a 10 x 10 sheet at 2.083 seconds: every number in the info line
// is one you can check.
$ainAssetId = "ToyAssets:TD_Barbarian_Death";

function ainLoadFixtureAssets()
{
	%copy = testRoot("assetAnimationInspectorSmokeProject/ToyAssets");

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
schedule(2000, 0, "ainStep1");

//-----------------------------------------------------------------------------

function ainStep1()
{
	createPath(testRoot("shots/"));

	// Spelled out rather than held in a variable: tests/run.ps1 finds the folder
	// to delete by reading this file for setProjectFolder("...").
	ProjectManager.setProjectFolder("assetAnimationInspectorSmokeProject");
	EditorPreferences.path = testRoot("shots/assetAnimationInspectorSmokePrefs.taml");

	ainCheck("fixture asset module registered", ainLoadFixtureAssets());

	EditorCore.tabBook.selectPage(2);

	schedule(600, 0, "ainStep2");
}

//-----------------------------------------------------------------------------
// The pane exists, and stands down until an animation is chosen.
//-----------------------------------------------------------------------------

function ainStep2()
{
	$ainInspector = AssetAdmin.inspector;
	$ainPane = $ainInspector.animationPane;

	ainCheck("animation pane built", isObject($ainPane));
	ainCheck("it is an AssetAnimationInspectorPane",
		$ainPane.getClassNamespace() $= "AssetAnimationInspectorPane");
	ainCheck("it inherits the shared pane",
		$ainPane.getSuperClassNamespace() $= "AssetInspectorPane");
	ainCheck("it starts hidden", !$ainInspector.paneScroller["Animation"].isVisible());
	ainCheck("the generic inspector is the one on show", $ainInspector.insScroller.isVisible());

	// The registry knows every pane, in the order they were registered.
	ainCheck("all four panes are registered", $ainInspector.paneKeys $= "Image Animation Font Sound");

	$ainTile = AssetAdmin.Dictionary["AnimationAsset"].getButton($ainAssetId);
	ainCheck("the animation tile is in the library", isObject($ainTile));

	$ainTile.onClick();

	schedule(600, 0, "ainStep3");
}

//-----------------------------------------------------------------------------
// Choosing one hands the page to the pane.
//-----------------------------------------------------------------------------

function ainStep3()
{
	ainCheck("the pane took the page", $ainInspector.paneScroller["Animation"].isVisible());
	ainCheck("the generic inspector stood down", !$ainInspector.insScroller.isVisible());
	ainCheck("the image pane stood down too", !$ainInspector.imageScroller.isVisible());
	ainCheck("the pane is bound to the asset", $ainPane.target == $ainTile.AnimationAsset);
	ainCheck("the inspector reports the pane's asset as the inspected one",
		$ainInspector.inspectedObject() == $ainTile.AnimationAsset);

	// The strongest single statement the pane makes. AnimationFrames is the
	// timeline; a box of numbers beside it would be a second source of truth.
	ainCheck("AnimationFrames is NOT offered as a row", !isObject($ainPane.row["AnimationFrames"]));
	ainCheck("neither is NamedAnimationFrames", !isObject($ainPane.row["NamedAnimationFrames"]));
	ainCheck("nor NamedCellsMode", !isObject($ainPane.row["NamedCellsMode"]));
	ainCheck("nor AssetInternal", !isObject($ainPane.row["AssetInternal"]));

	// The three blocks and what is in them.
	ainCheck("the identity block exists", isObject($ainPane.identityChain));
	ainCheck("the playback block exists", isObject($ainPane.playbackChain));
	ainCheck("the description block exists", isObject($ainPane.descriptionChain));

	ainCheck("asset name row", isObject($ainPane.row["AssetName"]));
	ainCheck("category row", isObject($ainPane.row["AssetCategory"]));
	ainCheck("image row", isObject($ainPane.row["Image"]));
	ainCheck("animation time row", isObject($ainPane.row["AnimationTime"]));
	ainCheck("loop row", isObject($ainPane.row["AnimationCycle"]));
	ainCheck("random start row", isObject($ainPane.row["RandomStart"]));
	ainCheck("description row", isObject($ainPane.row["AssetDescription"]));

	// Renaming an asset changes its id and every file naming it, so the row is
	// readable and inert.
	//
	// Asked of the box rather than of the row: there is no isEnabled binding on
	// anything, so the row form of this check answered "Unknown command" and
	// passed for years without testing a thing.
	ainCheck("the name row is not editable", !$ainPane.row["AssetName"].editor.isActive());

	schedule(300, 0, "ainStep4");
}

//-----------------------------------------------------------------------------
// The derived line, which is the whole reason the block exists.
//-----------------------------------------------------------------------------

function ainStep4()
{
	%asset = $ainPane.target;
	%info = $ainPane.infoLabel.getText();

	ainCheck("the info line counts the frames (" @ %info @ ")", strstr(%info, "25 frames") != -1);
	ainCheck("it names the image", strstr(%info, "TD_Barbarian_CompSprite") != -1);
	ainCheck("it gives the frame size", strstr(%info, "96 x 96") != -1);
	ainCheck("it gives the sheet size and count", strstr(%info, "100 frames") != -1);

	// 25 frames in 2.5 seconds is 10 a second, and the line has to say so.
	%asset.setAnimationTime(2.5);
	%info = $ainPane.infoLabel.getText();
	ainCheck("it works out the frame rate (" @ %info @ ")", strstr(%info, "10.0 per second") != -1);

	schedule(300, 0, "ainStep5");
}

//-----------------------------------------------------------------------------
// Each warning, appearing and clearing.
//-----------------------------------------------------------------------------

function ainStep5()
{
	%asset = $ainPane.target;

	ainCheck("no warning to begin with", !$ainPane.warningLabel.isVisible());

	// Frames outside the image are clamped by the engine rather than refused, so
	// the animation goes on playing and shows the wrong art. That is what this
	// line exists to say.
	%good = trim(%asset.getAnimationFrames());
	%asset.setAnimationFrames("55 56 900");
	ainCheck("out-of-range frames are called out", $ainPane.warningLabel.isVisible());
	ainCheck("and the warning says which way round it is",
		strstr($ainPane.warningLabel.getText(), "clamped") != -1);

	%asset.setAnimationFrames(%good);
	ainCheck("the warning clears when they are back in range", !$ainPane.warningLabel.isVisible());

	// An animation with nothing in it.
	%asset.setAnimationFrames("");
	ainCheck("an empty animation is called out", $ainPane.warningLabel.isVisible());
	ainCheck("and it says what to do about it",
		strstr($ainPane.warningLabel.getText(), "Frame Range") != -1);

	%asset.setAnimationFrames(%good);
	ainCheck("that clears too", !$ainPane.warningLabel.isVisible());

	schedule(300, 0, "ainStep6");
}

//-----------------------------------------------------------------------------
// Committing, and the floor under the animation time.
//-----------------------------------------------------------------------------

function ainStep6()
{
	%asset = $ainPane.target;

	$ainPane.commitValue("AssetCategory", "smokeCategory");
	ainCheck("a committed field reaches the asset", %asset.AssetCategory $= "smokeCategory");

	// Zero has no length, and worse: the playback integrator divides the total by
	// the frame count and then divides by that.
	$ainPane.commitValue("AnimationTime", 0);
	ainCheck("an animation time of zero is floored, not written (" @ %asset.getAnimationTime() @ ")",
		%asset.getAnimationTime() > 0);

	$ainPane.commitValue("AnimationTime", 1.5);
	ainCheck("a sensible time is written as given",
		mAbs(%asset.getAnimationTime() - 1.5) < 0.001);

	// RandomStart has no script accessor at all -- it is field-only -- so this is
	// the path the base's readField and writeField take.
	$ainPane.commitValue("RandomStart", true);
	ainCheck("a field with no accessor still commits", %asset.RandomStart);
	$ainPane.commitValue("RandomStart", false);

	schedule(300, 0, "ainStep7");
}

//-----------------------------------------------------------------------------
// Standing down for another asset kind.
//-----------------------------------------------------------------------------

function ainStep7()
{
	%imageTile = AssetAdmin.Dictionary["ImageAsset"].getButton("ToyAssets:TD_Barbarian_CompSprite");
	%imageTile.onClick();

	ainCheck("the animation pane stood down", !$ainInspector.paneScroller["Animation"].isVisible());
	ainCheck("the image pane took over", $ainInspector.imageScroller.isVisible());
	ainCheck("exactly one pane is on show at a time", !$ainInspector.insScroller.isVisible());
	ainCheck("the animation pane was unbound", !isObject($ainPane.target));

	echo("AAIN DONE");
	schedule(200, 0, "quit");
}
