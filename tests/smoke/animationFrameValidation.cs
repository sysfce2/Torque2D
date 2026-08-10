// An animation asset re-validates its frames when the image underneath it is
// re-cut. AnimationAsset::onAssetRefresh used to call nothing but its empty
// parent, so the validated list kept indices from the old cut -- and
// ImageAsset::getImageFrameArea CLAMPS an out-of-range index rather than
// failing, so the animation quietly played the wrong art and said nothing.
// Run: tests/run.ps1 animationFrameValidation  ; grep AFV in tests/logs/.
//
// Here rather than in a C++ unit test because it needs a real image asset with a
// real texture behind it, and a unit test has no GL context -- loading one trips
// a modal assert that arrives as a hang.
//
// NOTE: a COPY of toybox/ToyAssets, never the module itself. setCellCountY ends
// in refreshAsset, which writes the .asset.taml straight back to its own file;
// aimed at the repository copy this test would rewrite tracked content.

setLogMode(1);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

function afvCheck(%label, %cond)
{
	if(%cond) echo("AFV PASS: " @ %label);
	else      echo("AFV FAIL: " @ %label);
}

// The barbarian death animation: 10 x 10 cells of 96, and 25 frames drawn from
// the last five rows -- so halving the cut leaves every one of them out of range,
// which is the case that used to go unnoticed.
$afvAnimId = "ToyAssets:TD_Barbarian_Death";
$afvImageId = "ToyAssets:TD_Barbarian_CompSprite";

function afvLoadFixtureAssets()
{
	%copy = testRoot("animationFrameValidationSmokeProject/ToyAssets");

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
schedule(2000, 0, "afvStep1");

//-----------------------------------------------------------------------------
// The fixture, uncut.
//-----------------------------------------------------------------------------

function afvStep1()
{
	// Spelled out rather than held in a variable: tests/run.ps1 finds the folder
	// to delete by reading this file for setProjectFolder("..."), so a name it
	// cannot see is a folder it cannot sweep.
	ProjectManager.setProjectFolder("animationFrameValidationSmokeProject");

	afvCheck("fixture asset module registered", afvLoadFixtureAssets());

	// Both have to be acquired, not just named. AssetManager::refreshAsset walks
	// the depended-on list but skips anything that is not loaded, so an animation
	// nobody holds never hears that its image moved.
	$afvAnim = AssetDatabase.acquireAsset($afvAnimId);
	$afvImage = AssetDatabase.acquireAsset($afvImageId);

	afvCheck("animation asset acquired", isObject($afvAnim));
	afvCheck("image asset acquired", isObject($afvImage));

	$afvSpecified = trim($afvAnim.getAnimationFrames());

	afvCheck("image is cut into 100 frames", $afvImage.getFrameCount() == 100);
	afvCheck("animation specifies 25 frames", $afvAnim.getAnimationFrameCount() == 25);
	afvCheck("nothing is clamped while every frame is in range",
		trim($afvAnim.getAnimationFrames(true)) $= $afvSpecified);

	schedule(300, 0, "afvStep2");
}

//-----------------------------------------------------------------------------
// Re-cut the image to half the rows. Every frame the animation names is now out
// of range, and the validated list must say so.
//-----------------------------------------------------------------------------

function afvStep2()
{
	$afvImage.setCellCountY(5);

	afvCheck("image is now cut into 50 frames", $afvImage.getFrameCount() == 50);

	// What the user asked for does not change. Only what will be drawn does --
	// which is the distinction the inspector's warning line is built on.
	afvCheck("the specified list is untouched by a re-cut",
		trim($afvAnim.getAnimationFrames()) $= $afvSpecified);

	%validated = trim($afvAnim.getAnimationFrames(true));

	afvCheck("the validated list no longer matches what was specified",
		%validated !$= $afvSpecified);
	afvCheck("the validated list is still 25 frames long",
		$afvAnim.getAnimationFrameCount(true) == 25);

	// validateNumericalFrames clamps an out-of-range frame to frameCount - 1
	// rather than dropping it, so all 25 collapse onto frame 49.
	afvCheck("every out-of-range frame clamped to the last one", afvAllWords(%validated, 49));

	schedule(300, 0, "afvStep3");
}

//-----------------------------------------------------------------------------
// Put the rows back. Validation is a live derivation, not a one-way trip.
//-----------------------------------------------------------------------------

function afvStep3()
{
	$afvImage.setCellCountY(10);

	afvCheck("image is back to 100 frames", $afvImage.getFrameCount() == 100);
	afvCheck("the validated list recovers when the frames come back",
		trim($afvAnim.getAnimationFrames(true)) $= $afvSpecified);

	AssetDatabase.releaseAsset($afvAnimId);
	AssetDatabase.releaseAsset($afvImageId);

	echo("AFV DONE");
	schedule(200, 0, "quit");
}

//-----------------------------------------------------------------------------

function afvAllWords(%list, %value)
{
	%count = getWordCount(%list);
	if(%count == 0)
	{
		return false;
	}

	for(%i = 0; %i < %count; %i++)
	{
		if(getWord(%list, %i) != %value)
		{
			return false;
		}
	}
	return true;
}
