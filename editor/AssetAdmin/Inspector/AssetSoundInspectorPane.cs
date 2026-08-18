//-----------------------------------------------------------------------------
// Copyright (c) 2013 GarageGames, LLC
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
//-----------------------------------------------------------------------------

//-----------------------------------------------------------------------------
// The inspector for an audio asset, in place of the generic one. Three blocks,
// the shape AssetAnimationInspectorPane uses:
//
//   Identity     the name, the category, the sound file, and how long that file
//                is -- the one thing about a sound that no field says
//   Playback     how loud, on which channel, and the three flags
//   Description  the prose the library is searched by
//
// An AudioAsset registers six fields of its own and not one of them carries a
// doc string, so the generic inspector showed six labels and explained none of
// them. Half the value of this pane is tipFor below.
//
// The preview is unchanged: selecting a sound in the library auditions it
// through the Play button overlaid on the preview area
// (AssetAdmin::buildAudioPlayButton, AssetWindow::displayAudioAsset). This pane
// deliberately does not grow a transport of its own.
//
// Absent, each for a checkable reason:
//
//   AssetAutoUnload               NOT hidden because it is uninteresting -- it is
//                                 hidden because it does not work here.
//                                 AudioAsset::initializeAsset calls
//                                 setAssetAutoUnload(false) unconditionally, so
//                                 every audio asset reads false whatever its file
//                                 says, and a tick would silently come back off
//                                 the next time the asset was loaded. A checkbox
//                                 that cannot be changed is worse than no
//                                 checkbox: it invites the attempt.
//   AssetInternal, AssetPrivate   they exist to keep an asset OUT of the editor
//   asset id, asset file          the module and the name are on show, and the
//                                 file is where the manager put it
//   the eight 3D fields           is3D, referenceDistance, maxDistance, the cone
//                                 family and environmentLevel are commented out
//                                 of AudioAsset::initPersistFields, and mIs3D is
//                                 hard-set false in the constructor. A sound
//                                 played from an asset id is never positional;
//                                 the positional path is SceneObject::playSound
//                                 with an AudioDescription datablock, which is a
//                                 different object with its own fields.
//-----------------------------------------------------------------------------

$AssetSoundInspectorPane::cellWidth = 300;
$AssetSoundInspectorPane::cellCount = 3;
$AssetSoundInspectorPane::descriptionHeight = 150;

// The gain at or below which alxCreateSource refuses to make a source at all
// rather than making a quiet one (MIN_GAIN, audio.cc). A sound this quiet is not
// faint, it is absent, which is worth saying out loud.
$AssetSoundInspectorPane::minimumGain = 0.05;

// Audio::AudioVolumeChannels - 1. Every channel has its own volume and there is
// no engine-side naming for any of them.
$AssetSoundInspectorPane::maxChannel = 31;

function AssetSoundInspectorPane::onAdd(%this)
{
	// onAdd does not chain, so the shared setup runs from here.
	%this.init();

	// What the file row's Find button offers. These are the formats the engine
	// actually registers with the resource manager (OpenALInitDriver) -- the same
	// list NewAudioAssetDialog uses.
	%this.fileFilters = "Audio Files (*.wav;*.ogg)|*.wav;*.ogg|All Files (*.*)|*.*";
	%this.fileTitle = "Choose an Audio File";
}

//-----------------------------------------------------------------------------
// Construction.
//-----------------------------------------------------------------------------

function AssetSoundInspectorPane::buildPane(%this)
{
	%grid = %this.makeCellGrid(0, $AssetSoundInspectorPane::cellWidth,
		$AssetSoundInspectorPane::cellCount);
	%this.add(%grid);
	%this.contentGrid = %grid;

	%this.buildIdentityCell(%grid);
	%this.buildPlaybackCell(%grid);
	%this.buildDescriptionCell(%grid);

	%this.buildWarning();

	%this.nameRow.setEnabled(false, "Renaming an asset changes its id and every file that refers to it, " @
		"so it is not something the inspector can do safely on its own.");
}

function AssetSoundInspectorPane::addField(%this, %container, %field)
{
	return %this.addFieldRow(%container, %field, %this.labelFor(%field),
		%this.kindFor(%field), %this.enumItemsFor(%field));
}

function AssetSoundInspectorPane::buildIdentityCell(%this, %grid)
{
	%chain = %this.makeCell(%grid, 4);
	%this.identityChain = %chain;

	%this.nameRow = %this.addField(%chain, "AssetName");
	%this.addField(%chain, "AssetCategory");
	%this.fileRow = %this.addField(%chain, "AudioFile");

	// Under the file, which is what it describes -- the length and the format are
	// both facts about that .wav, not about how loudly it is played. It also has
	// room to breathe here: the playback block is five rows deep and this block is
	// three, so the space at the bottom of this one was going spare.
	%this.infoLabel = %this.makeInfoLabel(%chain, "labelProfile");
	%this.infoLabel.textWrap = true;
	%this.infoLabel.textExtend = true;
	%this.infoLabel.vAlign = "top";
}

function AssetSoundInspectorPane::buildPlaybackCell(%this, %grid)
{
	%chain = %this.makeCell(%grid, 4);
	%this.playbackChain = %chain;

	%this.addField(%chain, "Volume");
	%this.addField(%chain, "VolumeChannel");
	%this.addField(%chain, "Looping");
	%this.addField(%chain, "Streaming");
	%this.addField(%chain, "Priority");
}

function AssetSoundInspectorPane::buildDescriptionCell(%this, %grid)
{
	%chain = %this.makeCell(%grid, 4);
	%this.descriptionChain = %chain;

	%this.addField(%chain, "AssetDescription");
}

function AssetSoundInspectorPane::buildWarning(%this)
{
	%this.warningLabel = %this.makeInfoLabel(%this, "overrideLabelProfile");
	%this.warningLabel.textWrap = true;
	%this.warningLabel.textExtend = true;
	%this.warningLabel.vAlign = "top";
	%this.warningLabel.setVisible(false);
}

//-----------------------------------------------------------------------------
// The field tables.
//-----------------------------------------------------------------------------

function AssetSoundInspectorPane::labelFor(%this, %field)
{
	switch$(%field)
	{
		case "AssetName": return "Asset Name";
		case "AssetCategory": return "Category";
		case "AssetDescription": return "Description";
		case "AudioFile": return "Audio File";
		case "VolumeChannel": return "Volume Channel";
	}

	return %field;
}

function AssetSoundInspectorPane::kindFor(%this, %field)
{
	switch$(%field)
	{
		case "AudioFile": return "file";
		case "Volume": return "decimal";
		case "VolumeChannel": return "number";
		case "Looping" or "Streaming" or "Priority": return "bool";
		case "AssetDescription": return "multiline";
	}

	return "text";
}

// The engine's own doc strings for all six of these fields are the empty string,
// so there is nothing to inherit and nowhere else a reader could find this out.
function AssetSoundInspectorPane::tipFor(%this, %field)
{
	switch$(%field)
	{
		case "AudioFile": return "A .wav or .ogg file. Those are the only two formats the engine reads.";

		case "Volume": return "How loud this sound is, from 0 to 1, before the channel volume is applied.";

		case "VolumeChannel": return "Which of the 32 mixer channels (0 to" SPC
			$AssetSoundInspectorPane::maxChannel @ ") this sound plays on. Each channel has its own volume, " @
			"so a game can fade music without touching effects. This project's scripts use 0 for music and " @
			"1 for effects, but that is a convention -- the engine attaches no meaning to any channel.";

		case "Looping": return "Repeat until something stops it. Looping sounds are also never dropped " @
			"when the mixer runs short of voices.";

		case "Streaming": return "Decode a piece at a time while it plays rather than loading the whole " @
			"file up front. Worth it for music, wasteful for a short effect.";

		case "Priority": return "Keep this sound when the mixer runs out of voices and has to drop one.";
	}

	return "";
}

function AssetSoundInspectorPane::editorHeightFor(%this, %field)
{
	if(%field $= "AssetDescription")
	{
		return $AssetSoundInspectorPane::descriptionHeight;
	}

	return 0;
}

// The stored path is absolute -- setAudioFile expands whatever it is given
// against the asset's own folder -- and an absolute path is neither readable nor
// portable. Show the collapsed form, which is what the file on disk says.
function AssetSoundInspectorPane::readField(%this, %field)
{
	if(%field $= "AudioFile")
	{
		return %this.target.getRelativeAudioFile();
	}

	return %this.target.getFieldValue(%field);
}

// Both of these are clamped by the engine as well. Clamping here too is not
// belt and braces: setVolume and setVolumeChannel compare the value they were
// given against the one they hold and clamp only afterwards, so handing them
// something out of range reads as a change every single time and marks the asset
// unsaved for an edit that moves nothing. Sending only values that are already
// in range means that never comes up.
function AssetSoundInspectorPane::writeField(%this, %field, %value)
{
	// mClamp, not mClampF. The latter is the C++ name and is not bound to script
	// at all, so it would return an empty string here and quietly write a zero.
	if(%field $= "Volume")
	{
		%this.target.Volume = mClamp(%value, 0, 1);
		return;
	}

	if(%field $= "VolumeChannel")
	{
		%this.target.VolumeChannel = mFloor(mClamp(%value, 0, $AssetSoundInspectorPane::maxChannel));
		return;
	}

	%this.target.setFieldValue(%field, %value);
}

//-----------------------------------------------------------------------------
// Loading. Everything the row loop does not reach.
//-----------------------------------------------------------------------------

function AssetSoundInspectorPane::refreshExtras(%this)
{
	%this.infoLabel.setText(%this.describeSound(%this.target));
	%this.showWarning(%this.warningFor(%this.target));
}

// How long the sound is, in milliseconds, measured at most once per file.
//
// Not measured in refreshExtras, which runs on every bind, every commit and
// every refresh an asset announces: alxGetAudioLength decodes the file to find
// out, and doing that to a music track each time somebody types a letter in the
// description box is not free. The answer only changes when the file does.
function AssetSoundInspectorPane::lengthOf(%this, %asset)
{
	%file = %asset.getRelativeAudioFile();

	if(%this.measuredId $= %this.assetId && %this.measuredFile $= %file)
	{
		return %this.measuredLength;
	}

	%this.measuredId = %this.assetId;
	%this.measuredFile = %file;
	%this.measuredLength = alxGetAudioLength(%this.assetId);

	return %this.measuredLength;
}

// The one thing about a sound that is not on show as a field: how long it is.
//
// A length of zero is reported as unknown rather than as an error. It means the
// buffer could not be read, and the reasons for that are mostly not the asset's
// fault -- no audio device on the machine, or a driver that would not start.
//
// The channel's current volume is deliberately NOT reported here, though it is
// the best answer to "why can I not hear it". alxGetChannelVolume reads a plain
// global array that is only filled in when the audio driver starts, so before
// that every channel answers zero -- and an editor that said "channel 0 is at 0%"
// about every sound in the library would be worse than saying nothing.
function AssetSoundInspectorPane::describeSound(%this, %asset)
{
	%length = %this.lengthOf(%asset);
	%format = strupr(getSubStr(fileExt(%asset.getRelativeAudioFile()), 1, 8));

	if(%length > 0)
	{
		%line = mFloatLength(%length / 1000, 2) SPC "s";
		%line = (%format $= "") ? %line : (%line @ "," SPC %format);
	}
	else
	{
		%line = (%format $= "") ? "Length unknown" : (%format @ ", length unknown");
	}

	return %line @ ".";
}

// In the order they matter. Only the first is shown, because the first is the one
// that has to be fixed before any of the others can be judged.
function AssetSoundInspectorPane::warningFor(%this, %asset)
{
	%ext = fileExt(%asset.getRelativeAudioFile());

	// The stream factory answers with nothing at all for any other extension, so
	// the sound simply never plays and says nothing about why.
	if(%asset.Streaming && %ext !$= ".wav" && %ext !$= ".ogg")
	{
		return "Streaming only works for .wav and .ogg files. This one is" SPC
			(%ext $= "" ? "not either" : %ext) @ ", so it will not play at all while Streaming is on.";
	}

	// Not "quiet" -- absent. alxCreateSource refuses to make a source whose gain
	// has fallen this low, and it makes the exception for looping and streaming
	// sounds, which are never dropped this way.
	if(%asset.Volume <= $AssetSoundInspectorPane::minimumGain && !%asset.Looping && !%asset.Streaming)
	{
		return "A volume of" SPC $AssetSoundInspectorPane::minimumGain SPC "or below means this sound is " @
			"not created at all rather than played quietly. Raise it, or turn on Looping or Streaming, " @
			"which are exempt.";
	}

	// A muted channel is not warned about, for the reason describeSound gives:
	// the channel volumes are zero until the audio driver starts, so the check
	// cannot tell "somebody muted this" from "nothing has made a sound yet".

	return "";
}

// forceLayout only when the visibility actually changed: a chain skips hidden
// children when it lays out, and nothing re-lays it out on setVisible.
function AssetSoundInspectorPane::showWarning(%this, %text)
{
	%wanted = (%text !$= "");
	%changed = (%wanted != %this.warningLabel.isVisible());

	%this.warningLabel.setText(%text);
	%this.warningLabel.setVisible(%wanted);

	if(%changed)
	{
		%this.forceLayout();
	}
}
