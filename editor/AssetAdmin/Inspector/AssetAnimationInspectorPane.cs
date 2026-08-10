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
// The inspector for an animation asset, in place of the generic one: three
// blocks that reflow, and a line saying what the numbers add up to.
//
// The single most important thing about it is a field that is NOT here.
// AnimationFrames is the timeline, in the editor above. A box of space-separated
// numbers beside a timeline editing the same list is two sources of truth, and
// the one that is only a box cannot say which frame 67 is.
//
// Also absent, each for a checkable reason:
//   NamedAnimationFrames, NamedCellsMode  v1 is numeric, and the engine's named
//                                         frame API does not round-trip through
//                                         its own file -- so this pane is never
//                                         shown for such an asset rather than
//                                         offering a switch into it
//   AssetInternal, AssetPrivate           they exist to keep an asset out of the
//                                         editor
//   asset id, asset file                  the module and the name are on show,
//                                         and the file is where the manager put it
//-----------------------------------------------------------------------------

$AssetAnimationInspectorPane::cellWidth = 300;
$AssetAnimationInspectorPane::cellCount = 3;
$AssetAnimationInspectorPane::descriptionHeight = 150;

// A time of zero has no length, and worse: ImageFrameProviderCore divides the
// total by the frame count to get a frame's length and then divides by that.
$AssetAnimationInspectorPane::minimumTime = 0.01;

function AssetAnimationInspectorPane::onAdd(%this)
{
	%this.init();
}

function AssetAnimationInspectorPane::buildPane(%this)
{
	%grid = %this.makeCellGrid(0, $AssetAnimationInspectorPane::cellWidth,
		$AssetAnimationInspectorPane::cellCount);
	%this.add(%grid);
	%this.contentGrid = %grid;

	%this.buildIdentityCell(%grid);
	%this.buildPlaybackCell(%grid);
	%this.buildDescriptionCell(%grid);

	%this.buildWarning();

	%this.nameRow.setEnabled(false, "Renaming an asset changes its id and every file that refers to it, " @
		"so it is not something the inspector can do safely on its own.");
}

// addFieldRow takes the label and the kind as arguments rather than asking the
// tables for them, so every call here would otherwise repeat the same two
// lookups. One place to go through them is also one place to be wrong.
function AssetAnimationInspectorPane::addField(%this, %container, %field)
{
	return %this.addFieldRow(%container, %field, %this.labelFor(%field),
		%this.kindFor(%field), %this.enumItemsFor(%field));
}

function AssetAnimationInspectorPane::buildIdentityCell(%this, %grid)
{
	%chain = %this.makeCell(%grid, 4);
	%this.identityChain = %chain;

	%this.nameRow = %this.addField(%chain, "AssetName");
	%this.addField(%chain, "AssetCategory");

	// Kind "asset": EditorFieldRow's Find button opens the picker already filtered
	// to image assets, which is the only asset an animation can name.
	%this.addField(%chain, "Image");
}

function AssetAnimationInspectorPane::buildPlaybackCell(%this, %grid)
{
	%chain = %this.makeCell(%grid, 4);
	%this.playbackChain = %chain;

	%this.addField(%chain, "AnimationTime");
	%this.addField(%chain, "AnimationCycle");
	%this.addField(%chain, "RandomStart");

	// Wrapping and extending: makeInfoLabel gives a label one line of 20 pixels,
	// which is right for a short readout and not for a sentence about frame
	// counts, sizes and rates. Without them the line is simply not drawn.
	%this.infoLabel = %this.makeInfoLabel(%chain, "labelProfile");
	%this.infoLabel.textWrap = true;
	%this.infoLabel.textExtend = true;
}

function AssetAnimationInspectorPane::buildDescriptionCell(%this, %grid)
{
	%chain = %this.makeCell(%grid, 4);
	%this.descriptionChain = %chain;

	%this.addField(%chain, "AssetDescription");
}

function AssetAnimationInspectorPane::buildWarning(%this)
{
	%this.warningLabel = %this.makeInfoLabel(%this, "overrideLabelProfile");
	%this.warningLabel.textWrap = true;
	%this.warningLabel.textExtend = true;
	%this.warningLabel.setVisible(false);
}

//-----------------------------------------------------------------------------
// The field tables.
//-----------------------------------------------------------------------------

function AssetAnimationInspectorPane::labelFor(%this, %field)
{
	switch$(%field)
	{
		case "AssetName": return "Asset Name";
		case "AssetCategory": return "Category";
		case "Image": return "Image Asset";
		case "AnimationTime": return "Animation Time (seconds)";
		case "AnimationCycle": return "Loop";
		case "RandomStart": return "Start On A Random Frame";
		case "AssetDescription": return "Description";
	}

	return %field;
}

function AssetAnimationInspectorPane::kindFor(%this, %field)
{
	switch$(%field)
	{
		case "Image": return "asset";
		case "AnimationTime": return "decimal";
		case "AnimationCycle": return "bool";
		case "RandomStart": return "bool";
		case "AssetDescription": return "multiline";
	}

	return "text";
}

function AssetAnimationInspectorPane::editorHeightFor(%this, %field)
{
	if(%field $= "AssetDescription")
	{
		return $AssetAnimationInspectorPane::descriptionHeight;
	}

	return 0;
}

//-----------------------------------------------------------------------------
// Reading and writing.
//
// RandomStart has no script accessor at all -- it is field-only -- which is
// exactly what the base's readField and writeField already do through
// getFieldValue and setFieldValue. No override; the note is here because the
// missing accessor looks like an oversight rather than a decision.
//-----------------------------------------------------------------------------

function AssetAnimationInspectorPane::writeField(%this, %field, %value)
{
	if(%field $= "AnimationTime")
	{
		// Floored rather than refused, because the number a person is halfway
		// through typing passes through here on its way to a sensible one.
		if(%value < $AssetAnimationInspectorPane::minimumTime)
		{
			%value = $AssetAnimationInspectorPane::minimumTime;
		}
		%this.target.setAnimationTime(%value);
		return;
	}

	%this.target.setFieldValue(%field, %value);
}

function AssetAnimationInspectorPane::refreshExtras(%this)
{
	%this.infoLabel.setText(%this.describeAnimation(%this.target));
	%this.showWarning(%this.warningFor(%this.target));
}

// Everything the numbers add up to, which is the question the fields cannot
// answer separately: how long, how many, and therefore how fast.
function AssetAnimationInspectorPane::describeAnimation(%this, %asset)
{
	%count = %asset.getAnimationFrameCount();
	%time = %asset.getAnimationTime();

	%line = %count SPC ((%count == 1) ? "frame," : "frames,") SPC %time SPC "s";

	if(%count > 0 && %time > 0)
	{
		%line = %line @ "," SPC mFloatLength(%count / %time, 1) SPC "per second";
	}
	%line = %line @ ".";

	%image = AssetDatabase.acquireAsset(%asset.getImage());
	if(isObject(%image))
	{
		%frameSize = %image.getFrameSize(0);
		%line = %line SPC "Frames are" SPC getWord(%frameSize, 0) SPC "x" SPC getWord(%frameSize, 1) @
			", from" SPC %asset.getImage() SPC "(" @ %image.getImageWidth() SPC "x" SPC
			%image.getImageHeight() @ "," SPC %image.getFrameCount() SPC "frames).";

		AssetDatabase.releaseAsset(%asset.getImage());
	}

	return %line;
}

// In the order they matter. Only the first is shown, because the first is the one
// that has to be fixed before any of the others can be judged.
function AssetAnimationInspectorPane::warningFor(%this, %asset)
{
	%imageId = %asset.getImage();
	if(%imageId $= "")
	{
		return "This animation has no image asset, so there is nothing to play.";
	}

	%image = AssetDatabase.acquireAsset(%imageId);
	%valid = isObject(%image) && %image.getFrameCount() > 0;
	if(isObject(%image))
	{
		AssetDatabase.releaseAsset(%imageId);
	}

	if(!%valid)
	{
		return "The image asset" SPC %imageId SPC "did not load, so there is nothing to play.";
	}

	// Specified against validated is the only comparison script can make, and it
	// is exactly the right one: validateNumericalFrames CLAMPS an out-of-range
	// frame to the last one rather than dropping it, so the animation keeps
	// playing and quietly shows the wrong art.
	if(trim(%asset.getAnimationFrames()) !$= trim(%asset.getAnimationFrames(true)))
	{
		return "Some frames are outside the image's" SPC %image.getFrameCount() SPC
			"and are being clamped to the nearest one. The timeline shows what was asked for; " @
			"the preview shows what is being drawn.";
	}

	if(%asset.getAnimationFrameCount() == 0)
	{
		return "This animation has no frames yet. Drag one in from the palette, or use Frame Range.";
	}

	if(%asset.getAnimationTime() <= 0)
	{
		return "An animation time of zero has no length, so nothing plays.";
	}

	return "";
}

// forceLayout only when the visibility actually changed: a chain skips hidden
// children when it lays out, and nothing re-lays it out on setVisible.
function AssetAnimationInspectorPane::showWarning(%this, %text)
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

// The timeline is showing the same list, so it has to hear about a change made
// here -- picking a different image asset moves every frame's meaning.
function AssetAnimationInspectorPane::afterCommit(%this)
{
	if(isObject(AssetAdmin.animationStage))
	{
		AssetAdmin.animationStage.onInspectorCommit();
	}
}
