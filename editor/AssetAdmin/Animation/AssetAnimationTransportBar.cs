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
// Play, rewind, loop, and the two switches that decide what an edit does: the
// bar over the animation preview.
//
// It sits as an overlay on the preview background rather than in a strip of its
// own, which is where the audio play button already sits and proof that an
// overlay there receives clicks over the SceneWindow. It costs no layout and
// takes no room from the art.
//
// Not built from EditorButtonBar: that makes EditorIconButtons, which are
// momentary, and two of these have to SHOW a state.
//
// Play and Stop are two buttons with one hidden rather than one toggle, and the
// difference is not cosmetic. A toggle says "this setting is on"; these two say
// "here is what will happen if you press me", which is a different promise and
// the one a transport makes. It also means the button cannot get stuck showing
// Stop after something else halted the preview -- there is no state to fall out
// of step, only whichever button is currently on show.
//
// Order reads left to right as rewind, then the big play, then a gap, then the
// three that are settings rather than actions.
//-----------------------------------------------------------------------------

$AssetAnimationTransportBar::buttonSize = 24;
$AssetAnimationTransportBar::playSize = 36;
$AssetAnimationTransportBar::iconSize = 20;
$AssetAnimationTransportBar::spacing = 4;
$AssetAnimationTransportBar::gap = 16;

function AssetAnimationTransportBar::onAdd(%this)
{
	ThemeManager.setProfile(%this, "emptyProfile");

	%this.addButton("rewind", $EditorIcon::playback_rew, "Back to the first frame",
		$AssetAnimationTransportBar::buttonSize);

	// The one you reach for, so it is half again the size of the rest. They sit
	// in the same place, and exactly one of them is ever visible.
	%this.playButton = %this.addButton("play", $EditorIcon::playback_play, "Play the preview",
		$AssetAnimationTransportBar::playSize);
	%this.stopButton = %this.addButton("stop", $EditorIcon::playback_stop, "Stop the preview",
		$AssetAnimationTransportBar::playSize);
	%this.stopButton.setVisible(false);

	// A chain lays out what it can see, so an empty control is how a gap is
	// spelled -- there is no spacing-before on a child.
	%this.addSpacer($AssetAnimationTransportBar::gap);

	%this.loopButton = %this.addToggle("Loop", $EditorIcon::playback_reload, $EditorIcon::playback_reload,
		"Looping. Click to play once and stop on the last frame.",
		"Playing once. Click to loop.");

	%this.rateButton = %this.addToggle("KeepRate", $EditorIcon::stop_watch, $EditorIcon::stop_watch,
		"Keeping the frame rate: adding or removing frames rewrites the animation's time to match.",
		"Keeping the animation's time: adding a frame makes every frame play faster.");

	%this.addButton("openRangeDialog", $EditorIcon::list_num, "Fill the timeline from a range of frames",
		$AssetAnimationTransportBar::buttonSize);
}

function AssetAnimationTransportBar::addToggle(%this, %name, %frameOn, %frameOff, %tipOn, %tipOff)
{
	%size = $AssetAnimationTransportBar::buttonSize;

	%button = new GuiCheckBoxCtrl()
	{
		class = "EditorToggleIcon";
		Position = "0 0";
		Extent = %size SPC %size;

		// Matching EditorIconButton, which draws its picture at 20 in the same
		// 24 pixel button. At the toggle's own default of 16 the row read as two
		// sizes of button rather than one.
		iconSize = $AssetAnimationTransportBar::iconSize;

		frameOn = %frameOn;
		frameOff = %frameOff;
		tipOn = %tipOn;
		tipOff = %tipOff;
		toggleName = %name;
		owner = %this;
	};
	ThemeManager.setProfile(%button, "iconButtonProfile");
	ThemeManager.setProfile(%button, "tipProfile", "TooltipProfile");
	%this.add(%button);

	return %button;
}

function AssetAnimationTransportBar::addButton(%this, %method, %frame, %tooltip, %size)
{
	%button = new GuiButtonCtrl()
	{
		class = "EditorIconButton";
		Position = "0 0";
		Frame = %frame;
		Command = %this.getId() @ "." @ %method @ "();";
		Tooltip = %tooltip;
	};
	ThemeManager.setProfile(%button, "iconButtonProfile");
	ThemeManager.setProfile(%button, "tipProfile", "TooltipProfile");
	%this.add(%button);

	// After the add, because EditorIconButton::onAdd sets its own 24 x 24 and
	// would undo anything the new{} block said. The icon inside it is sized
	// against the button, so both grow together.
	if(%size !$= "" && %size != $AssetAnimationTransportBar::buttonSize)
	{
		%button.setExtent(%size, %size);
		%button.icon.setExtent(%size - 4, %size - 4);
	}

	return %button;
}

function AssetAnimationTransportBar::addSpacer(%this, %width)
{
	%spacer = new GuiControl()
	{
		Position = "0 0";
		Extent = %width SPC $AssetAnimationTransportBar::buttonSize;
		UseInput = false;
	};
	ThemeManager.setProfile(%spacer, "emptyProfile");
	%this.add(%spacer);

	return %spacer;
}

//-----------------------------------------------------------------------------
// One handler, switching on which toggle spoke.
//-----------------------------------------------------------------------------

function AssetAnimationTransportBar::onToggleIconChanged(%this, %button)
{
	switch$(%button.toggleName)
	{
		case "Loop":
			%this.stage.setCycle(%button.getValue());

		case "KeepRate":
			// An editor preference, not a property of the asset: it decides what
			// the editor does on the user's behalf, and the user who wants it
			// wants it next time too.
			EditorPreferences.set("assetAnimationKeepFrameRate", %button.getValue());
	}
}

function AssetAnimationTransportBar::play(%this)
{
	%this.stage.play();
}

function AssetAnimationTransportBar::stop(%this)
{
	%this.stage.stop();
}

function AssetAnimationTransportBar::rewind(%this)
{
	// The playing state is left alone on purpose. Rewinding while it plays
	// restarts the run, which is what a rewind is.
	%this.stage.scrubTo(0);
}

function AssetAnimationTransportBar::openRangeDialog(%this)
{
	%this.stage.openRangeDialog();
}

//-----------------------------------------------------------------------------
// Reading the state back out. Called whenever something else may have moved it.
//-----------------------------------------------------------------------------

// Called from every path that can change the playing state, and there are more
// of them than the two buttons: clicking a slot stops to scrub, dragging a frame
// out stops, and a one-shot animation stops itself by reaching the end. Each of
// those used to leave a Stop button on show over a preview that had stopped.
function AssetAnimationTransportBar::refresh(%this)
{
	%playing = %this.stage.playing;
	%this.playButton.setVisible(!%playing);
	%this.stopButton.setVisible(%playing);

	// A chain lays out only the children it can see, and nothing re-lays it out
	// when one is hidden -- so swapping the two is a resize away from leaving a
	// hole where the other one was.
	%this.resize(getWord(%this.getPosition(), 0), getWord(%this.getPosition(), 1),
		getWord(%this.getExtent(), 0), getWord(%this.getExtent(), 1));

	if(!isObject(%this.stage.animationAsset))
	{
		return;
	}

	%this.loopButton.setValue(%this.stage.animationAsset.getAnimationCycle());
	%this.rateButton.setValue(EditorPreferences.get("assetAnimationKeepFrameRate", false));
}
