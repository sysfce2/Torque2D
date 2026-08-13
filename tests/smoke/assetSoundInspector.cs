// Asset Manager sound-inspector smoke test. Drives the custom pane that replaced
// the generic GuiInspector for audio assets: the three reflowing blocks, the
// field the engine will not let anyone change, the two clamps, and each warning.
// Run: tests/run.ps1 assetSoundInspector  ; grep ASND in tests/logs/.
//
// Nothing here asserts a duration. alxGetAudioLength decodes the file to answer,
// which needs OpenAL running, and a machine with no audio device answers zero for
// a perfectly good file. The pane reports that as "length unknown" rather than as
// an error, and the test only checks the parts that do not depend on a device.
//
// Selecting an audio tile auditions it (AssetWindow::displayAudioAsset calls the
// play button), and every commit re-clicks the tile through
// AssetAdmin::refreshPreview, so this suite makes noise on a machine that has
// speakers. buttonSound is a short one-shot for exactly that reason, and the last
// step stops it.
//
// NOTE: a COPY of toybox/ToyAssets, for the same reason the other asset suites
// take one.

setLogMode(1);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

function asnCheck(%label, %cond)
{
	if(%cond) echo("ASND PASS: " @ %label);
	else      echo("ASND FAIL: " @ %label);
}

// TD_ButtonSound.wav -- short, one-shot, and not music.
$asnAssetId = "ToyAssets:buttonSound";

function asnLoadFixtureAssets()
{
	%copy = testRoot("assetSoundInspectorSmokeProject/ToyAssets");

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
schedule(2000, 0, "asnStep1");

//-----------------------------------------------------------------------------

function asnStep1()
{
	createPath(testRoot("shots/"));

	// Spelled out rather than held in a variable: tests/run.ps1 finds the folder
	// to delete by reading this file for setProjectFolder("...").
	ProjectManager.setProjectFolder("assetSoundInspectorSmokeProject");
	EditorPreferences.path = testRoot("shots/assetSoundInspectorSmokePrefs.taml");

	asnCheck("fixture asset module registered", asnLoadFixtureAssets());

	EditorCore.tabBook.selectPage(2);

	schedule(600, 0, "asnStep2");
}

//-----------------------------------------------------------------------------
// The pane exists, and stands down until a sound is chosen.
//-----------------------------------------------------------------------------

function asnStep2()
{
	$asnInspector = AssetAdmin.inspector;
	$asnPane = $asnInspector.soundPane;

	asnCheck("sound pane built", isObject($asnPane));
	asnCheck("it is an AssetSoundInspectorPane",
		$asnPane.getClassNamespace() $= "AssetSoundInspectorPane");
	asnCheck("it inherits the shared pane",
		$asnPane.getSuperClassNamespace() $= "AssetInspectorPane");
	asnCheck("it starts hidden", !$asnInspector.paneScroller["Sound"].isVisible());
	asnCheck("the sound pane is in the registry", strstr($asnInspector.paneKeys, "Sound") != -1);

	$asnTile = AssetAdmin.Dictionary["AudioAsset"].getButton($asnAssetId);
	asnCheck("the sound tile is in the library", isObject($asnTile));

	$asnTile.onClick();

	schedule(600, 0, "asnStep3");
}

//-----------------------------------------------------------------------------
// Choosing one hands the page to the pane.
//-----------------------------------------------------------------------------

function asnStep3()
{
	asnCheck("the pane took the page", $asnInspector.paneScroller["Sound"].isVisible());
	asnCheck("the generic inspector stood down", !$asnInspector.insScroller.isVisible());
	asnCheck("the pane is bound to the asset", $asnPane.target == $asnTile.AudioAsset);

	// The three blocks and what is in them.
	asnCheck("the identity block exists", isObject($asnPane.identityChain));
	asnCheck("the playback block exists", isObject($asnPane.playbackChain));
	asnCheck("the description block exists", isObject($asnPane.descriptionChain));
	asnCheck("the grid holds three blocks", $asnPane.contentGrid.getCount() == 3);

	asnCheck("asset name row", isObject($asnPane.row["AssetName"]));
	asnCheck("category row", isObject($asnPane.row["AssetCategory"]));
	asnCheck("audio file row", isObject($asnPane.row["AudioFile"]));
	asnCheck("volume row", isObject($asnPane.row["Volume"]));
	asnCheck("volume channel row", isObject($asnPane.row["VolumeChannel"]));
	asnCheck("looping row", isObject($asnPane.row["Looping"]));
	asnCheck("streaming row", isObject($asnPane.row["Streaming"]));
	asnCheck("priority row", isObject($asnPane.row["Priority"]));
	asnCheck("description row", isObject($asnPane.row["AssetDescription"]));

	// The one that is left out because it does not work, rather than because it
	// is uninteresting: AudioAsset::initializeAsset forces it off on every load,
	// so a checkbox here would silently undo itself.
	asnCheck("AssetAutoUnload is NOT offered", !isObject($asnPane.row["AssetAutoUnload"]));
	asnCheck("and the engine is still forcing it off", !$asnPane.target.AssetAutoUnload);

	asnCheck("AssetInternal is NOT offered", !isObject($asnPane.row["AssetInternal"]));
	asnCheck("nor is AssetPrivate", !isObject($asnPane.row["AssetPrivate"]));

	// None of the 3D fields is registered at all, so a sound played from an asset
	// id is never positional. Worth pinning: they are commented out rather than
	// deleted, and putting them back would put them on this pane.
	asnCheck("no 3D fields", !isObject($asnPane.row["is3D"]) && !isObject($asnPane.row["maxDistance"]));

	asnCheck("the name row is not editable", !$asnPane.row["AssetName"].editor.isActive());

	schedule(300, 0, "asnStep4");
}

//-----------------------------------------------------------------------------
// The tooltips. AudioAsset registers six fields and gives every one of them an
// empty doc string, so this pane is the only place any of them is explained.
//-----------------------------------------------------------------------------

function asnStep4()
{
	asnCheck("volume explains itself", $asnPane.row["Volume"].editor.Tooltip !$= "");
	asnCheck("the channel explains itself", $asnPane.row["VolumeChannel"].editor.Tooltip !$= "");
	asnCheck("and says the naming is only a convention",
		strstr($asnPane.row["VolumeChannel"].editor.Tooltip, "convention") != -1);
	asnCheck("streaming explains itself", $asnPane.row["Streaming"].editor.Tooltip !$= "");
	asnCheck("priority explains itself", $asnPane.row["Priority"].editor.Tooltip !$= "");
	asnCheck("looping explains itself", $asnPane.row["Looping"].editor.Tooltip !$= "");

	// The row that carries no tip keeps an empty one rather than inheriting.
	asnCheck("the category row has none", $asnPane.row["AssetCategory"].editor.Tooltip $= "");

	// A greyed row shows the reason it is greyed; re-enabling puts the standing
	// explanation back rather than blanking it.
	%row = $asnPane.row["Streaming"];
	%tip = %row.editor.Tooltip;
	%row.setEnabled(false, "a reason");
	asnCheck("greying a row swaps in the reason", %row.editor.Tooltip $= "a reason");
	%row.setEnabled(true, "");
	asnCheck("re-enabling puts the field's own tip back", %row.editor.Tooltip $= %tip);

	schedule(300, 0, "asnStep5");
}

//-----------------------------------------------------------------------------
// The path, and the line describing the file it names.
//-----------------------------------------------------------------------------

function asnStep5()
{
	%asset = $asnPane.target;

	// The whole reason getRelativeAudioFile was added.
	%shown = $asnPane.row["AudioFile"].getValue();
	asnCheck("the file row shows the relative path (" @ %shown @ ")", %shown $= "TD_ButtonSound.wav");
	asnCheck("which is not what the field holds", %asset.AudioFile !$= %shown);

	// The format comes from the file name, so it is there whether or not the
	// audio driver came up. The length is not asserted: it needs a real device.
	%info = $asnPane.infoLabel.getText();
	asnCheck("the info line names the format (" @ %info @ ")", strstr(%info, "WAV") != -1);

	// Selecting the sound above should have started the driver -- nothing else in
	// the editor ever does, so before this the Play button had no context to play
	// through. A machine with no sound card legitimately answers false.
	asnCheck("the audio driver was started on demand", AssetAdmin.audioDriverTried);

	schedule(300, 0, "asnStep6");
}

//-----------------------------------------------------------------------------
// The clamps.
//
// The engine clamps these too, but it compares before it clamps -- so handing it
// an out-of-range value reads as a change every time and marks the asset unsaved
// for an edit that moves nothing. The pane sends only values already in range.
//-----------------------------------------------------------------------------

function asnStep6()
{
	%asset = $asnPane.target;

	$asnPane.commitValue("Volume", 5);
	asnCheck("a volume over one is clamped (" @ %asset.Volume @ ")", mAbs(%asset.Volume - 1) < 0.001);

	$asnPane.commitValue("Volume", -1);
	asnCheck("and under zero (" @ %asset.Volume @ ")", mAbs(%asset.Volume) < 0.001);

	$asnPane.commitValue("Volume", 0.75);
	asnCheck("a sensible volume is written as given", mAbs(%asset.Volume - 0.75) < 0.001);

	$asnPane.commitValue("VolumeChannel", 99);
	asnCheck("a channel over 31 is clamped (" @ %asset.VolumeChannel @ ")", %asset.VolumeChannel == 31);

	$asnPane.commitValue("VolumeChannel", -4);
	asnCheck("and under zero", %asset.VolumeChannel == 0);

	$asnPane.commitValue("VolumeChannel", 1);
	asnCheck("a real channel is written as given", %asset.VolumeChannel == 1);

	// The engine's own clamp, now that it compares against what it will store
	// rather than against the raw value it was handed.
	//
	// Both fields have to be sitting AT the limit for this to mean anything: it
	// is re-sending a value that clamps to what is already there that used to
	// read as a change, call refreshAsset and mark the asset unsaved for an edit
	// that moved nothing.
	$asnPane.commitValue("Volume", 1);
	$asnPane.commitValue("VolumeChannel", 31);
	%asset.saveAsset();
	asnCheck("saving clears the dirty flag", !%asset.isAssetDirty());

	%asset.Volume = 5;
	asnCheck("re-sending an out-of-range volume is not treated as a change",
		!%asset.isAssetDirty());
	%asset.VolumeChannel = 99;
	asnCheck("nor an out-of-range channel", !%asset.isAssetDirty());
	asnCheck("and neither value moved",
		mAbs(%asset.Volume - 1) < 0.001 && %asset.VolumeChannel == 31);

	$asnPane.commitValue("VolumeChannel", 0);

	schedule(300, 0, "asnStep7");
}

//-----------------------------------------------------------------------------
// Each warning, appearing and clearing.
//-----------------------------------------------------------------------------

function asnStep7()
{
	%asset = $asnPane.target;

	$asnPane.commitValue("Volume", 1);
	asnCheck("no warning to begin with", !$asnPane.warningLabel.isVisible());

	// Not "quiet" -- absent. alxCreateSource refuses to make a source at all once
	// the gain reaches MIN_GAIN, and a slider at 0.01 looks like it should work.
	$asnPane.commitValue("Volume", 0.01);
	asnCheck("a volume below the cull threshold is called out", $asnPane.warningLabel.isVisible());
	asnCheck("and the warning says it is not created at all",
		strstr($asnPane.warningLabel.getText(), "not created at all") != -1);

	// Looping sounds are exempt from the cull, so the same volume is fine there.
	$asnPane.commitValue("Looping", true);
	asnCheck("a looping sound is exempt", !$asnPane.warningLabel.isVisible());
	$asnPane.commitValue("Looping", false);
	$asnPane.commitValue("Volume", 1);
	asnCheck("and the warning clears with the volume back up", !$asnPane.warningLabel.isVisible());

	// The stream factory answers with nothing at all for any other extension, so
	// the sound never plays and says nothing about why.
	$asnPane.commitValue("Streaming", true);
	asnCheck("streaming a .wav is fine", !$asnPane.warningLabel.isVisible());

	$asnPane.commitValue("AudioFile", "../fonts/Arial.fnt");
	asnCheck("streaming something that is neither .wav nor .ogg is called out",
		$asnPane.warningLabel.isVisible());
	asnCheck("and the warning names the two formats",
		strstr($asnPane.warningLabel.getText(), ".wav and .ogg") != -1);

	$asnPane.commitValue("AudioFile", "TD_ButtonSound.wav");
	$asnPane.commitValue("Streaming", false);
	asnCheck("that clears too", !$asnPane.warningLabel.isVisible());

	schedule(300, 0, "asnStep8");
}

//-----------------------------------------------------------------------------
// Committing the plain fields.
//-----------------------------------------------------------------------------

function asnStep8()
{
	%asset = $asnPane.target;

	$asnPane.commitValue("AssetCategory", "smokeCategory");
	asnCheck("a committed field reaches the asset", %asset.AssetCategory $= "smokeCategory");

	$asnPane.commitValue("AssetDescription", "A sound, for smoke testing.");
	asnCheck("so does the description", %asset.AssetDescription $= "A sound, for smoke testing.");

	// Priority has no script accessor -- AudioAsset has none at all -- so this is
	// the base's readField/writeField path through getFieldValue.
	$asnPane.commitValue("Priority", true);
	asnCheck("a field with no accessor still commits", %asset.Priority);
	$asnPane.commitValue("Priority", false);

	asnCheck("the asset is dirty after an edit", %asset.isAssetDirty());
	asnCheck("and the inspector offers to save it", $asnInspector.getSaveAssetEnabled());

	schedule(300, 0, "asnStepReflow");
}

//-----------------------------------------------------------------------------
// Reflow. Three blocks in a grid with a 300 floor: one column when the inspector
// is dragged narrow, three across the foot of a wide screen.
//-----------------------------------------------------------------------------

// How many blocks are sharing the top row, read from where the grid actually put
// them rather than from the arithmetic that placed them.
function asnColumnCount()
{
	%grid = $asnPane.contentGrid;
	%topY = getWord(%grid.getObject(0).getPosition(), 1);

	%count = 0;
	for(%i = 0; %i < %grid.getCount(); %i++)
	{
		if(getWord(%grid.getObject(%i).getPosition(), 1) == %topY)
		{
			%count++;
		}
	}
	return %count;
}

function asnStepReflow()
{
	%h = getWord($asnPane.getExtent(), 1);

	// Put back afterwards -- the pane follows its scroller by the CHANGE in width.
	%natural = getWord($asnPane.getExtent(), 0);

	$asnPane.resize(0, 0, 380, %h);
	asnCheck("a narrow pane stacks the blocks (" @ asnColumnCount() @ " across)",
		asnColumnCount() == 1);

	$asnPane.resize(0, 0, 672, %h);
	asnCheck("the pane as it opens is two across (" @ asnColumnCount() @ ")",
		asnColumnCount() == 2);

	$asnPane.resize(0, 0, 1600, %h);
	asnCheck("a wide pane is a single row (" @ asnColumnCount() @ " across)",
		asnColumnCount() == 3);
	asnCheck("wide, the blocks share the width evenly",
		getWord($asnPane.playbackChain.getExtent(), 0)
			== getWord($asnPane.identityChain.getExtent(), 0));
	asnCheck("wide, the blocks reach the right-hand edge",
		getWord($asnPane.descriptionChain.getPosition(), 0)
			+ getWord($asnPane.descriptionChain.getExtent(), 0) >= 1580);

	$asnPane.resize(0, 0, %natural, %h);

	schedule(300, 0, "asnStepPreview");
}

//-----------------------------------------------------------------------------
// The preview auditions the ASSET, not the asset as the running game is mixing
// it.
//
// This is the case that made the whole thing necessary: a game that turns its
// music channel down to nothing does not merely make a music asset quiet in the
// editor, it makes it unplayable -- alxCreateSource refuses to build a source on
// a muted channel, so there is no handle and nothing to distinguish that from a
// broken file.
//-----------------------------------------------------------------------------

function asnStepPreview()
{
	%asset = $asnPane.target;
	%channel = %asset.VolumeChannel;

	// Everything below needs a real device. A machine without one is not a
	// failure, so say what was skipped rather than asserting into the dark.
	if(!OpenALIsInitialized())
	{
		echo("ASND SKIP: no audio driver, so the preview path was not exercised");
		schedule(300, 0, "asnStep9");
		return;
	}

	%was = alxGetChannelVolume(%channel);
	alxSetChannelVolume(%channel, 0);

	%plain = alxPlay($asnAssetId);
	asnCheck("alxPlay on a muted channel gives no handle at all", %plain == 0);

	%preview = alxPlayPreview($asnAssetId);
	asnCheck("alxPlayPreview plays it anyway", %preview != 0);
	asnCheck("and it really is playing", alxIsPlaying(%preview));
	alxStop(%preview);

	alxSetChannelVolume(%channel, %was);
	asnCheck("the game's own channel volume is put back untouched",
		alxGetChannelVolume(%channel) == %was);

	// The preview channel is the editor's, and no asset should be sitting on it.
	asnCheck("the asset was not moved onto the preview channel",
		%asset.VolumeChannel == %channel);

	schedule(300, 0, "asnStep9");
}

//-----------------------------------------------------------------------------
// Standing down for another asset kind.
//-----------------------------------------------------------------------------

function asnStep9()
{
	AssetAdmin.audioPlayButton.resetSound();

	%imageTile = AssetAdmin.Dictionary["ImageAsset"].getButton("ToyAssets:TD_Barbarian_CompSprite");
	%imageTile.onClick();

	asnCheck("the sound pane stood down", !$asnInspector.paneScroller["Sound"].isVisible());
	asnCheck("the image pane took over", $asnInspector.imageScroller.isVisible());
	asnCheck("exactly one pane is on show at a time", !$asnInspector.insScroller.isVisible());
	asnCheck("the sound pane was unbound", !isObject($asnPane.target));

	echo("ASND DONE");
	schedule(200, 0, "quit");
}
