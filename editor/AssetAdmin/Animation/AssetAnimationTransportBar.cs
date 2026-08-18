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
// The chrome -- the sized buttons, the toggles, the gaps -- is EditorTransportBar
// in EditorCore, shared with the particle preview's bar. What is left here is
// what these particular buttons do.
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

function AssetAnimationTransportBar::onAdd(%this)
{
	%this.init();

	%this.addButton("rewind", $EditorIcon::playback_rew, "Back to the first frame",
		$EditorTransportBar::buttonSize);

	// The one you reach for, so it is half again the size of the rest. They sit
	// in the same place, and exactly one of them is ever visible.
	%this.playButton = %this.addButton("play", $EditorIcon::playback_play, "Play the preview",
		$EditorTransportBar::playSize);
	%this.stopButton = %this.addButton("stop", $EditorIcon::playback_stop, "Stop the preview",
		$EditorTransportBar::playSize);
	%this.stopButton.setVisible(false);

	%this.addSpacer($EditorTransportBar::gap);

	%this.loopButton = %this.addToggle("Loop", $EditorIcon::playback_reload, $EditorIcon::playback_reload,
		"Looping. Click to play once and stop on the last frame.",
		"Playing once. Click to loop.");

	%this.rateButton = %this.addToggle("KeepRate", $EditorIcon::stop_watch, $EditorIcon::stop_watch,
		"Keeping the frame rate: adding or removing frames rewrites the animation's time to match.",
		"Keeping the animation's time: adding a frame makes every frame play faster.");

	%this.addButton("openRangeDialog", $EditorIcon::list_num, "Fill the timeline from a range of frames",
		$EditorTransportBar::buttonSize);
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
	%this.relayout();

	if(!isObject(%this.stage.animationAsset))
	{
		return;
	}

	%this.loopButton.setValue(%this.stage.animationAsset.getAnimationCycle());
	%this.rateButton.setValue(EditorPreferences.get("assetAnimationKeepFrameRate", false));
}
