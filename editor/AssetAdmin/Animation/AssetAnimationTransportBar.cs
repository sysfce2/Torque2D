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
// momentary. Three of these five have to SHOW a state, which is what
// EditorToggleIcon exists for, so the row is assembled by hand.
//-----------------------------------------------------------------------------

$AssetAnimationTransportBar::buttonSize = 24;
$AssetAnimationTransportBar::spacing = 4;

function AssetAnimationTransportBar::onAdd(%this)
{
	ThemeManager.setProfile(%this, "emptyProfile");

	%this.playButton = %this.addToggle("Play", $EditorIcon::playback_stop, $EditorIcon::playback_play,
		"Stop the preview", "Play the preview");

	%this.addButton("rewind", $EditorIcon::playback_rew, "Back to the first frame");

	%this.loopButton = %this.addToggle("Loop", $EditorIcon::playback_reload, $EditorIcon::playback_reload,
		"Looping. Click to play once and stop on the last frame.",
		"Playing once. Click to loop.");

	%this.rateButton = %this.addToggle("KeepRate", $EditorIcon::stop_watch, $EditorIcon::stop_watch,
		"Keeping the frame rate: adding or removing frames rewrites the animation's time to match.",
		"Keeping the animation's time: adding a frame makes every frame play faster.");

	%this.addButton("openRangeDialog", $EditorIcon::list_num, "Fill the timeline from a range of frames");
}

function AssetAnimationTransportBar::addToggle(%this, %name, %frameOn, %frameOff, %tipOn, %tipOff)
{
	%size = $AssetAnimationTransportBar::buttonSize;

	%button = new GuiCheckBoxCtrl()
	{
		class = "EditorToggleIcon";
		Position = "0 0";
		Extent = %size SPC %size;
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

function AssetAnimationTransportBar::addButton(%this, %method, %frame, %tooltip)
{
	%size = $AssetAnimationTransportBar::buttonSize;

	%button = new GuiButtonCtrl()
	{
		class = "EditorIconButton";
		Position = "0 0";
		Extent = %size SPC %size;
		Frame = %frame;
		Command = %this.getId() @ "." @ %method @ "();";
		Tooltip = %tooltip;
	};
	ThemeManager.setProfile(%button, "iconButtonProfile");
	ThemeManager.setProfile(%button, "tipProfile", "TooltipProfile");
	%this.add(%button);

	return %button;
}

//-----------------------------------------------------------------------------
// One handler, switching on which toggle spoke.
//-----------------------------------------------------------------------------

function AssetAnimationTransportBar::onToggleIconChanged(%this, %button)
{
	switch$(%button.toggleName)
	{
		case "Play":
			if(%button.getValue()) { %this.stage.play(); }
			else                   { %this.stage.stop(); }

		case "Loop":
			%this.stage.setCycle(%button.getValue());

		case "KeepRate":
			// An editor preference, not a property of the asset: it decides what
			// the editor does on the user's behalf, and the user who wants it
			// wants it next time too.
			EditorPreferences.set("assetAnimationKeepFrameRate", %button.getValue());
	}
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

function AssetAnimationTransportBar::refresh(%this)
{
	if(!isObject(%this.stage.animationAsset))
	{
		return;
	}

	%this.playButton.setValue(%this.stage.playing);
	%this.loopButton.setValue(%this.stage.animationAsset.getAnimationCycle());
	%this.rateButton.setValue(EditorPreferences.get("assetAnimationKeepFrameRate", false));
}
