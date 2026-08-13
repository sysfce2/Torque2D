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
// The bar over the particle preview: restart, play/pause, stop, a speed, and the
// two switches that reduce the effect to the one emitter being tuned.
//
// The chrome is EditorTransportBar in EditorCore, shared with the animation
// preview's bar; what is here is what these buttons do.
//
// Play and Pause are two buttons with one hidden rather than one toggle, for the
// reason the animation bar gives: a toggle says "this setting is on", a transport
// says "here is what will happen if you press me", and two buttons cannot get
// stuck showing the wrong state because there is no state to get stuck -- only
// whichever button is on show.
//
// SOLO AND EMITTER-OFF ARE NOT ASSET STATE. They are ParticlePlayer::setEmitterVisible
// and setEmitterPaused on the preview's own player, so nothing they do can dirty
// the effect or reach its file. That also means they do not survive: every edit
// ends in refreshAsset, ParticlePlayer::onAssetRefreshed rebuilds every emitter
// node, and the rebuilt nodes are all visible and running again. reapply() is
// called from the panes' afterCommit for exactly that reason.
//
// Both act on the emitter the title dropdown has selected, which is the one whose
// fields are on show -- so "solo" always means "the one I am looking at". On the
// effect itself (index 0) there is no emitter to isolate and both stand down.
//-----------------------------------------------------------------------------

$AssetParticleTransportBar::speeds = "0.1 0.25 0.5 1 2";
$AssetParticleTransportBar::defaultSpeedIndex = 3;

function AssetParticleTransportBar::onAdd(%this)
{
	%this.init();

	%this.addButton("restart", $EditorIcon::playback_rew, "Play the effect again from the beginning",
		$EditorTransportBar::buttonSize);

	// The one you reach for, so it is half again the size of the rest. They sit in
	// the same place, and exactly one of them is ever visible.
	%this.playButton = %this.addButton("play", $EditorIcon::playback_play, "Play the preview",
		$EditorTransportBar::playSize);
	%this.pauseButton = %this.addButton("pause", $EditorIcon::playback_pause, "Pause the preview",
		$EditorTransportBar::playSize);
	%this.pauseButton.setVisible(false);

	%this.addButton("stop", $EditorIcon::playback_stop, "Stop the preview and clear its particles",
		$EditorTransportBar::buttonSize);

	%this.addSpacer($EditorTransportBar::gap);

	// Not a toggle: five speeds cycled by pressing, with the current one in the
	// tooltip. A slider would need a label to be readable and a label needs room
	// the bar does not have over the art.
	%this.speedIndex = $AssetParticleTransportBar::defaultSpeedIndex;
	%this.speedButton = %this.addButton("cycleSpeed", $EditorIcon::stop_watch, "",
		$EditorTransportBar::buttonSize);

	%this.addSpacer($EditorTransportBar::gap);

	%this.soloButton = %this.addToggle("Solo", $EditorIcon::eye, $EditorIcon::eye_inv,
		"Showing this emitter only. Click to show them all again.",
		"Showing every emitter. Click to show only the selected one.");

	// on / off rather than the speaker pair this started with. Nothing here makes
	// a sound, and a crossed-out speaker reads as "muted audio" however it is
	// captioned -- what the button actually does is switch one emitter off.
	%this.emitterOffButton = %this.addToggle("PauseEmitter", $EditorIcon::off, $EditorIcon::on,
		"This emitter is switched off. Click to let it emit again.",
		"This emitter is emitting. Click to switch just this one off.");
}

//-----------------------------------------------------------------------------
// What the buttons do. All of it is on the preview's player, none of it on the
// asset.
//-----------------------------------------------------------------------------

function AssetParticleTransportBar::player(%this)
{
	return isObject(AssetAdmin.previewPlayer) ? AssetAdmin.previewPlayer : "";
}

function AssetParticleTransportBar::play(%this)
{
	%player = %this.player();
	if(!isObject(%player))
	{
		return;
	}

	// Paused and stopped are different states with one button between them: a
	// paused effect resumes where it was, a stopped one has to be started again.
	if(%player.getIsPlaying())
	{
		%player.setPaused(false);
	}
	else
	{
		%player.play(true);
	}

	%this.refresh();
}

function AssetParticleTransportBar::pause(%this)
{
	%player = %this.player();
	if(isObject(%player))
	{
		%player.setPaused(true);
	}

	%this.refresh();
}

// stop(false, false): free the particles now, and do not kill the effect.
//
// Not stop(TRUE, false), the "let the particles finish" form, for two reasons.
// It leaves mPlaying set until the last particle dies, so getIsPlaying goes on
// answering true and the bar would offer Pause over an effect that had been
// stopped -- and Pause on a stopped effect does nothing, so the button would lie
// twice. It also pauses every emitter to do its waiting, which is the same flag
// solo and emitter-off are written in, so a graceful stop would quietly undo them.
//
// Killing is the other thing this is not: that deletes the player and leaves the
// preview with nothing in it and nothing to restart.
function AssetParticleTransportBar::stop(%this)
{
	%player = %this.player();
	if(isObject(%player))
	{
		%player.stop(false, false);
	}

	%this.refresh();
}

function AssetParticleTransportBar::restart(%this)
{
	%player = %this.player();
	if(isObject(%player))
	{
		// True clears the particles already out, so what follows is the effect
		// from nothing rather than the effect over its own tail.
		%player.play(true);
		%player.setPaused(false);
	}

	%this.refresh();
}

function AssetParticleTransportBar::cycleSpeed(%this)
{
	%count = getWordCount($AssetParticleTransportBar::speeds);
	%this.speedIndex = (%this.speedIndex + 1) % %count;

	%this.applySpeed();
	%this.refresh();
}

function AssetParticleTransportBar::speed(%this)
{
	return getWord($AssetParticleTransportBar::speeds, %this.speedIndex);
}

function AssetParticleTransportBar::applySpeed(%this)
{
	%player = %this.player();
	if(isObject(%player))
	{
		%player.setTimeScale(%this.speed());
	}
}

//-----------------------------------------------------------------------------
// Solo and emitter-off, on the emitter the dropdown has selected.
//-----------------------------------------------------------------------------

function AssetParticleTransportBar::onToggleIconChanged(%this, %button)
{
	switch$(%button.toggleName)
	{
		case "Solo":
			%this.soloOn = %button.getValue();

		case "PauseEmitter":
			%this.emitterOff = %button.getValue();
	}

	%this.reapply();
}

// The selected emitter's index, or -1 when the dropdown is on the effect itself.
function AssetParticleTransportBar::selectedIndex(%this)
{
	if(!isObject(AssetAdmin.inspector) || !AssetAdmin.inspector.titleDropDown.isVisible())
	{
		return -1;
	}

	return AssetAdmin.inspector.titleDropDown.getSelectedItem() - 1;
}

// Push the whole visible/paused state onto the player. Written as "say it for
// every emitter" rather than "change the one that moved", because the player's
// emitter nodes are rebuilt from the asset on every edit and arrive visible and
// running -- so there is nothing to change, only something to say again.
function AssetParticleTransportBar::reapply(%this)
{
	%player = %this.player();
	%asset = isObject(AssetAdmin.inspector) ? AssetAdmin.inspector.documentAsset() : "";

	if(!isObject(%player) || !isObject(%asset))
	{
		return;
	}

	%selected = %this.selectedIndex();
	%count = %asset.getEmitterCount();

	for(%i = 0; %i < %count; %i++)
	{
		%isSelected = (%i == %selected);

		// Solo hides everything except the selected one. With nothing selected
		// there is nothing to solo, so everything stays visible.
		%visible = !%this.soloOn || %selected < 0 || %isSelected;

		// Switching off pauses only the selected one -- the opposite shape to solo, and the
		// pair is what lets you hear one emitter or everything but it.
		%paused = %this.emitterOff && %isSelected;

		%player.setEmitterVisible(%visible, %i);
		%player.setEmitterPaused(%paused, %i);
	}
}

//-----------------------------------------------------------------------------
// Reading the state back out. Called whenever something else may have moved it:
// a new selection, an edit that rebuilt the player, or one of the buttons above.
//-----------------------------------------------------------------------------

function AssetParticleTransportBar::refresh(%this)
{
	%player = %this.player();

	%running = isObject(%player) && %player.getIsPlaying() && !%player.getPaused();
	%this.playButton.setVisible(!%running);
	%this.pauseButton.setVisible(%running);
	%this.relayout();

	%this.speedButton.Tooltip = "Preview speed:" SPC %this.speed() @ "x. Click for the next one.";

	// Neither switch means anything while the effect itself is selected, and a
	// switch left on from the last emitter would be a lie about this one.
	%onEmitter = %this.selectedIndex() >= 0;
	%this.soloButton.setActive(%onEmitter);
	%this.emitterOffButton.setActive(%onEmitter);

	%this.applySpeed();
	%this.reapply();
}

// A new preview player was built, for the asset named here.
//
// The switches are kept when it is the same effect and cleared when it is not,
// which is the distinction that makes solo usable at all: EVERY edit rebuilds the
// preview -- the commit ends in refreshAsset and AssetAdmin::refreshPreview
// re-clicks the tile -- so a bar that reset on every rebuild would drop the solo
// the moment you changed the field you were soloing to look at.
//
// The speed is not part of that: it is how you are watching rather than what you
// are watching, so it carries across assets like a preference.
function AssetParticleTransportBar::onPreviewRebuilt(%this, %assetId)
{
	if(%this.assetId !$= %assetId)
	{
		%this.assetId = %assetId;
		%this.soloOn = false;
		%this.emitterOff = false;
	}

	%this.soloButton.setValue(%this.soloOn);
	%this.emitterOffButton.setValue(%this.emitterOff);

	%this.refresh();
}
