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

	schedule(300, 0, "afvStep4");
}

//-----------------------------------------------------------------------------
// Named cells. A different fixture: the 1234 sheet is cut explicitly into four
// cells called block1..block4, and 1234Animation lists those names.
//
// The single most valuable assertion in this file is the first one below.
// getNamedAnimationFrames formatted a StringTableEntry -- a const char* -- through
// %d, so it returned a row of pointer values, and nothing that asked an animation
// for its named frames could recover them. That one character is why the Asset
// Manager could not edit a named animation at all.
//-----------------------------------------------------------------------------

$afvNamedAnimId = "ToyAssets:1234Animation";
$afvNamedImageId = "ToyAssets:1234";

function afvStep4()
{
	$afvNamedAnim = AssetDatabase.acquireAsset($afvNamedAnimId);
	$afvNamedImage = AssetDatabase.acquireAsset($afvNamedImageId);

	afvCheck("named animation acquired", isObject($afvNamedAnim));
	afvCheck("explicit image acquired", isObject($afvNamedImage));

	afvCheck("the image is in explicit mode", $afvNamedImage.getExplicitMode());
	afvCheck("it has four explicit cells", $afvNamedImage.getExplicitCellCount() == 4);

	// Derived, not stored. There is no NamedCellsMode field any more -- the answer
	// is read from the image every time it is asked for.
	afvCheck("the animation reports named cells mode from its image",
		$afvNamedAnim.getNamedCellsMode());

	%named = trim($afvNamedAnim.getNamedAnimationFrames());

	afvCheck("named frames read back as names, not as pointers (" @ %named @ ")",
		%named $= "block1 block2 block3 block4");
	afvCheck("there are four of them", $afvNamedAnim.getNamedAnimationFrameCount() == 4);
	afvCheck("getFrameCount answers without caring which space it is in",
		$afvNamedAnim.getFrameCount() == 4);
	afvCheck("nothing is missing to begin with", trim($afvNamedAnim.getMissingFrames()) $= "");

	schedule(300, 0, "afvStep5");
}

//-----------------------------------------------------------------------------
// Take a cell away. The name is KEPT rather than dropped -- dropping it is a
// deletion the user never asked for and could not have seen happen.
//-----------------------------------------------------------------------------

function afvStep5()
{
	$afvNamedImage.removeExplicitCell(2);

	afvCheck("the image is down to three cells", $afvNamedImage.getExplicitCellCount() == 3);
	afvCheck("the animation still specifies all four frames",
		trim($afvNamedAnim.getNamedAnimationFrames()) $= "block1 block2 block3 block4");
	afvCheck("and names the one that no longer resolves (" @ trim($afvNamedAnim.getMissingFrames()) @ ")",
		trim($afvNamedAnim.getMissingFrames()) $= "block3");

	schedule(300, 0, "afvStep6");
}

//-----------------------------------------------------------------------------
// Put it back. Like the numeric case above, this is a live derivation.
//-----------------------------------------------------------------------------

function afvStep6()
{
	$afvNamedImage.insertExplicitCell(2, 0, 32, 32, 32, "block3");

	afvCheck("the cell is back", $afvNamedImage.getExplicitCellCount() == 4);
	afvCheck("and it went back in the right place",
		$afvNamedImage.getExplicitCellName(2) $= "block3");
	afvCheck("nothing is missing any more", trim($afvNamedAnim.getMissingFrames()) $= "");

	schedule(300, 0, "afvStep7");
}

//-----------------------------------------------------------------------------
// A cell added with no name is named for us, so that "explicit mode means every
// frame can be addressed by name" holds without the user having to maintain it.
//-----------------------------------------------------------------------------

function afvStep7()
{
	$afvNamedImage.addExplicitCell(0, 0, 32, 32, "");

	afvCheck("the unnamed cell was named for its own index (" @ $afvNamedImage.getExplicitCellName(4) @ ")",
		$afvNamedImage.getExplicitCellName(4) $= "Frame4");

	// Now take the name the NEXT blank cell would want -- cell 5 is called Frame6,
	// which is what cell 6 would otherwise be named -- and add that blank. The
	// search has to walk past it. Not hypothetical: deleting a cell from the
	// middle renumbers every one after it, so a sheet arrives in this state on its
	// own.
	$afvNamedImage.addExplicitCell(0, 0, 32, 32, "Frame6");
	$afvNamedImage.addExplicitCell(0, 0, 32, 32, "");

	afvCheck("a taken name is walked past (" @ $afvNamedImage.getExplicitCellName(6) @ ")",
		$afvNamedImage.getExplicitCellName(6) $= "Frame7");
	afvCheck("and the cell that took it keeps it",
		$afvNamedImage.getExplicitCellName(5) $= "Frame6");

	schedule(300, 0, "afvStep8");
}

//-----------------------------------------------------------------------------
// Turn explicit mode off and on. The animation moves between name space and
// index space and keeps its frames both ways, which is what makes re-cutting a
// sheet a decision rather than a commitment.
//-----------------------------------------------------------------------------

function afvStep8()
{
	$afvNamedImage.setExplicitMode(false);

	afvCheck("the animation is no longer in named cells mode",
		!$afvNamedAnim.getNamedCellsMode());
	afvCheck("its frames converted to indices (" @ trim($afvNamedAnim.getAnimationFrames()) @ ")",
		trim($afvNamedAnim.getAnimationFrames()) $= "0 1 2 3");
	afvCheck("and getMissingFrames says nothing about a numbered animation",
		trim($afvNamedAnim.getMissingFrames()) $= "");

	// The cells have to SURVIVE the mode being off, in memory and in the file.
	//
	// The file used to gate its Cells node on explicit mode, so saving an image
	// with the mode off deleted every cell -- and with them the only thing that
	// could ever resolve the animation's names again. Which makes the state of
	// this file the whole reason the mode is reversible at all.
	afvCheck("the cells are still there with the mode off",
		$afvNamedImage.getExplicitCellCount() == 7);

	AssetDatabase.saveAsset($afvNamedImageId);

	%text = afvReadFile(AssetDatabase.getAssetFilePath($afvNamedImageId));

	afvCheck("the saved file kept its cells", strstr(%text, "block1") != -1);
	afvCheck("and says out loud that explicit mode is off",
		strstr(%text, "ExplicitMode=\"0\"") != -1 || strstr(%text, "ExplicitMode=\"false\"") != -1);

	schedule(300, 0, "afvStep9");
}

function afvStep9()
{
	$afvNamedImage.setExplicitMode(true);

	afvCheck("the animation is named again", $afvNamedAnim.getNamedCellsMode());

	// The names come back unchanged rather than being rebuilt from the indices,
	// because the named list was never cleared -- which is the whole reason both
	// lists are kept.
	afvCheck("and its names are the ones it started with (" @ trim($afvNamedAnim.getNamedAnimationFrames()) @ ")",
		trim($afvNamedAnim.getNamedAnimationFrames()) $= "block1 block2 block3 block4");

	AssetDatabase.releaseAsset($afvNamedAnimId);
	AssetDatabase.releaseAsset($afvNamedImageId);

	echo("AFV DONE");
	schedule(200, 0, "quit");
}

//-----------------------------------------------------------------------------

function afvReadFile(%path)
{
	%file = new FileObject();
	%text = "";

	if(%file.openForRead(%path))
	{
		while(!%file.isEOF())
		{
			%text = %text @ %file.readLine() @ " ";
		}
		%file.close();
	}
	%file.delete();

	return %text;
}

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
