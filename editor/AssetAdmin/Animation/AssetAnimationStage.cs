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
// Turns the Asset Manager's preview into an animation editor while an animation
// asset is selected, and puts it back the moment anything else is.
//
// A manager rather than a control: it owns two panes and the shape of the frame
// set they live in, and it is the one place that knows an animation is on show.
// Everything that used to ask "is this an animation?" now asks this instead.
//
// The split is built on demand and collapsed on the way out. It has to be, not
// merely for tidiness: the other five asset kinds want the whole preview area,
// and a frame set with three frames cannot give it to them.
//-----------------------------------------------------------------------------

$AssetAnimationStage::defaultPaletteWidth = 220;
$AssetAnimationStage::defaultTimelineHeight = 150;

// A frame set hands out ids from 1, and the root is the first. Named because
// createVerticalSplit(1) reads as a magic number otherwise.
$AssetAnimationStage::rootFrameId = 1;

function AssetAnimationStage::onAdd(%this)
{
	%this.built = false;
	%this.assetId = "";
	%this.playing = false;

	// -1 rather than unset: an unset field reads as an empty string, which is not
	// less than zero, so every "have I got one?" test below would pass with it.
	%this.resumeSlot = -1;
}

function AssetAnimationStage::onRemove(%this)
{
	%this.teardown();
}

//-----------------------------------------------------------------------------
// Selection.
//
// retainFor is called for EVERY asset, whatever its kind, from the one place a
// selection is made. Saying "keep the split if it is for this asset, otherwise
// take it down" in one method is what keeps the five branches that have nothing
// to do with animation completely untouched.
//-----------------------------------------------------------------------------

function AssetAnimationStage::retainFor(%this, %animationAssetId)
{
	if(%animationAssetId $= "" || !%this.canEdit(%animationAssetId))
	{
		%this.teardown();
		return false;
	}

	return true;
}

// Named cells are out of scope for now, and the reason is not squeamishness: the
// engine's named-frame API does not round-trip. Its getter formats a string
// through %d, and the field joins with commas while the setter splits on
// whitespace, so a named list does not survive its own TAML file. Rather than
// build a timeline on top of that, such an asset keeps the plain inspector and
// the old single-sprite preview, which have always worked for it.
function AssetAnimationStage::canEdit(%this, %animationAssetId)
{
	%asset = AssetDatabase.acquireAsset(%animationAssetId);
	if(!isObject(%asset))
	{
		return false;
	}

	%named = %asset.getNamedCellsMode();
	AssetDatabase.releaseAsset(%animationAssetId);

	return !%named;
}

function AssetAnimationStage::select(%this, %imageAsset, %animationAsset, %assetId)
{
	if(%this.busy || !%this.retainFor(%assetId))
	{
		return;
	}

	%this.build();

	// The editor closing resizes the canvas, and AssetWindow::onExtentChange
	// answers a resize by re-clicking the selected tile -- so a selection can
	// arrive while the panes are being torn down around it. Nothing to do then
	// but stand down quietly.
	if(!isObject(%this.palettePane) || !isObject(%this.timelinePane))
	{
		return;
	}

	// Borrowed, not acquired. AssetDictionaryButton::loadAnimationAsset already
	// holds both of these and releases them in its onRemove, and this stage never
	// outlives a selection -- a second acquire here would be a second release to
	// remember somewhere else.
	%this.animationAsset = %animationAsset;
	%this.imageAsset = %imageAsset;
	%this.assetId = %assetId;
	%this.imageAssetId = %animationAsset.getImage();

	%this.palettePane.load(%this.imageAssetId);
	%this.timelinePane.load(%this.imageAssetId, trim(%animationAsset.getAnimationFrames()));

	%this.admin.transportBarContainer.setVisible(true);

	// Adopt the sprite the preview has already made. displayAnimationAsset builds
	// it and announces it BEFORE this runs -- the tile displays first and selects
	// second -- so on a first selection that announcement arrives while there is
	// no stage to hear it. Asking here covers both orders.
	//
	// Before the bar is refreshed, because adopting the sprite is what settles
	// whether the animation is playing.
	%this.onPreviewRebuilt(%this.admin.previewSprite);

	%this.admin.transportBar.refresh();
}

//-----------------------------------------------------------------------------
// Building and collapsing the split.
//-----------------------------------------------------------------------------

// Building and collapsing both move the dividers, which resizes the SceneWindow,
// whose onExtentChange answers a resize by re-clicking the selected tile -- which
// lands back here. So both are shut for the duration.
//
// Without it, deleting the first pane re-entered select() while built was still
// true, and the second call reached for a pane that was already half gone.
function AssetAnimationStage::build(%this)
{
	if(%this.built || %this.busy)
	{
		return;
	}

	%this.busy = true;
	%frames = %this.admin.previewFrames;

	// One split at a time, and the pane it makes room for added straight after.
	// GuiFrameSetCtrl::assignChildToFrame puts a child in the empty frame its
	// bounds fall inside, or failing that the first empty frame it walks to --
	// neither of which is worth relying on. Adding while exactly one frame is
	// empty makes the answer certain.
	//
	// splitFrame leaves the existing control -- the preview background -- in
	// child1 and anchors it, so the art stays top left through both splits and is
	// never reparented.
	%ids = %frames.createVerticalSplit($AssetAnimationStage::rootFrameId);
	%topId = getWord(%ids, 0);
	%this.timelineFrameId = getWord(%ids, 1);
	%frames.anchorFrame(%this.timelineFrameId);

	%this.timelinePane = new GuiControl()
	{
		class = "AssetAnimationTimelinePane";
		stage = %this;
		HorizSizing = "width";
		VertSizing = "height";
		Position = "0 0";
		Extent = "100 100";
	};
	%frames.add(%this.timelinePane);

	%ids = %frames.createHorizontalSplit(%topId);
	%this.paletteFrameId = getWord(%ids, 1);
	%frames.anchorFrame(%this.paletteFrameId);

	%this.palettePane = new GuiControl()
	{
		class = "AssetAnimationPalettePane";
		stage = %this;
		HorizSizing = "width";
		VertSizing = "height";
		Position = "0 0";
		Extent = "100 100";
	};
	%frames.add(%this.palettePane);

	// Both sizes LAST, and that ordering is the whole of it. setFrameSize is the
	// only thing here that lays the tree out -- it ends in a resize of the whole
	// frame set -- and a layout can only size the controls that are already in
	// their frames. Sized before the panes were added, the split came out with
	// the right frames and a palette still at the 100 x 100 it was built with,
	// sitting behind the preview where nothing could be seen of it.
	%frames.setFrameSize(%this.timelineFrameId,
		EditorPreferences.get("assetAnimationTimelineHeight", $AssetAnimationStage::defaultTimelineHeight));
	%frames.setFrameSize(%this.paletteFrameId,
		EditorPreferences.get("assetAnimationPaletteWidth", $AssetAnimationStage::defaultPaletteWidth));

	%this.built = true;
	%this.busy = false;
}

function AssetAnimationStage::teardown(%this)
{
	if(!%this.built || %this.busy)
	{
		return;
	}

	%this.busy = true;
	%this.rememberSizes();
	%this.admin.transportBarContainer.setVisible(false);

	// Deleting each pane collapses the frame that held it and hoists its twin's
	// subtree, so two deletes take the tree back to one frame holding the preview
	// background -- which was never moved and does not have to be put back.
	if(isObject(%this.palettePane))
	{
		%this.palettePane.delete();
	}
	if(isObject(%this.timelinePane))
	{
		%this.timelinePane.delete();
	}

	// Forgotten, not merely deleted. Sim ids are handed out again once they are
	// free, so a field still holding a deleted pane's id will happily answer
	// isObject() -- about whatever object took that id next. The symptom is a
	// call into a live object that has never heard of the method, which reads
	// like a namespace problem and is nothing of the sort.
	%this.palettePane = "";
	%this.timelinePane = "";
	%this.previewSprite = "";

	%this.built = false;
	%this.assetId = "";
	%this.animationAsset = "";
	%this.imageAsset = "";
	%this.playing = false;
	%this.busy = false;
}

// A frame set has no divider-moved callback -- there is no Con::executef
// anywhere in guiFrameSetCtrl.cc -- so the sizes are read at the two moments the
// split is going away, which is the last chance to see where the user left them.
function AssetAnimationStage::rememberSizes(%this)
{
	if(!%this.built)
	{
		return;
	}

	%layout = %this.admin.previewFrames.getFrameLayout();

	%paletteWidth = %this.frameSizeFrom(%layout, %this.paletteFrameId, 0);
	%timelineHeight = %this.frameSizeFrom(%layout, %this.timelineFrameId, 1);

	if(%paletteWidth > 0)
	{
		EditorPreferences.set("assetAnimationPaletteWidth", %paletteWidth);
	}
	if(%timelineHeight > 0)
	{
		EditorPreferences.set("assetAnimationTimelineHeight", %timelineHeight);
	}
}

// getFrameLayout is eight words per frame:
//   id child1 child2 isVertical extentX extentY isAnchored controlID
function AssetAnimationStage::frameSizeFrom(%this, %layout, %frameId, %axis)
{
	%count = getWordCount(%layout);
	for(%i = 0; %i < %count; %i += 8)
	{
		if(getWord(%layout, %i) == %frameId)
		{
			return getWord(%layout, %i + 4 + %axis);
		}
	}

	return 0;
}

//-----------------------------------------------------------------------------
// The preview.
//-----------------------------------------------------------------------------

// The preview scene is cleared and rebuilt from scratch by the display path, so
// the sprite the timeline follows is a different object every time. Said here
// rather than found by the timeline, because the timeline should not have to
// know how the preview is made.
function AssetAnimationStage::onPreviewRebuilt(%this, %sprite)
{
	if(!%this.built || %this.busy || !isObject(%sprite) || !isObject(%this.timelinePane))
	{
		return;
	}

	%this.previewSprite = %sprite;
	%this.timelinePane.setPreviewSprite(%sprite);

	// A sprite built with an Animation on it is ALREADY RUNNING -- nothing had to
	// press play -- so the editor's idea of the playing state has to be read off
	// the sprite rather than assumed. It was initialised false, so selecting an
	// animation put a Play button over a preview that was busy playing.
	%this.playing = !%sprite.getIsAnimationFinished();
	%this.refreshTransport();
}

// Put a finished animation back in a state where it can be moved.
//
// The single answer to a trap that otherwise looks like a dead control:
// ImageFrameProviderCore::updateAnimation returns immediately once
// mAnimationFinished is set, and setAnimationFrame goes through it -- so a
// non-cycling preview that has run to the end cannot be scrubbed at all.
// playAnimation is the only way back, and it clears the pause on its way, so the
// pause has to be put back afterwards.
function AssetAnimationStage::armPreview(%this)
{
	if(!isObject(%this.previewSprite))
	{
		return false;
	}

	if(%this.previewSprite.getIsAnimationFinished())
	{
		%this.previewSprite.playAnimation(%this.assetId);
		%this.previewSprite.pauseAnimation(!%this.playing);
	}

	return true;
}

// The one place a slot is ever set.
function AssetAnimationStage::scrubTo(%this, %slot)
{
	if(!%this.armPreview())
	{
		return;
	}

	%count = %this.timelinePane.strip.getCellCount();
	if(%count < 1)
	{
		return;
	}

	%this.previewSprite.setAnimationFrame(mClamp(%slot, 0, %count - 1));
}

// Clicking a slot stops first, deliberately: scrubbing a running preview shows a
// frame for a thirtieth of a second and then moves on, which reads as the click
// having done nothing.
function AssetAnimationStage::onSlotSelected(%this, %slot, %frame)
{
	%this.stop();
	%this.scrubTo(%slot);
}

// Pausing, not stopping. SpriteBase::stopAnimation sets the finished flag, which
// is what kills setAnimationFrame -- so a preview stopped that way could never be
// scrubbed again. Pausing halts it just as visibly and leaves every other gesture
// alive.
function AssetAnimationStage::stop(%this)
{
	if(!isObject(%this.previewSprite))
	{
		return;
	}

	%this.playing = false;
	%this.previewSprite.pauseAnimation(true);
	%this.refreshTransport();
}

// Every path that changes the playing state ends here, and there are more of
// them than the two buttons: clicking a slot stops in order to scrub, dragging a
// frame off the timeline stops, and a one-shot animation stops itself by reaching
// the end. Each of those left a Stop button showing over a stopped preview until
// the bar was told.
function AssetAnimationStage::refreshTransport(%this)
{
	if(!%this.built || !isObject(%this.admin.transportBar))
	{
		return;
	}

	%this.admin.transportBar.refresh();
}

function AssetAnimationStage::play(%this)
{
	if(!%this.armPreview())
	{
		return;
	}

	%this.playing = true;
	%this.previewSprite.pauseAnimation(false);
	%this.refreshTransport();
}

// A one-shot animation reached its end on its own. Nothing to do to the preview
// -- the engine has already parked it on the last frame -- but the button still
// says Stop, over something that has already stopped.
function AssetAnimationStage::onPreviewFinished(%this)
{
	%this.playing = false;
	%this.refreshTransport();
}

//-----------------------------------------------------------------------------
// Absorbing a refresh instead of being rebuilt by one.
//
// Every asset setter ends in refreshAsset, which rewrites the file and fires
// onRefresh -- and the editor's answer to onRefresh has always been to re-click
// the selected tile, which clears the preview scene and builds a new sprite. For
// an image that is exactly right. For an animation being edited it means the
// preview restarts from frame one every time a frame is dragged.
//
// So the stage says "I've got this" for the asset it is showing, and the old
// path is untouched for everything else.
//-----------------------------------------------------------------------------

function AssetAnimationStage::absorbRefresh(%this, %asset)
{
	if(!%this.built || %this.busy || !isObject(%asset))
	{
		return false;
	}

	%isAnimation = (%asset == %this.animationAsset);
	%isImage = (%asset == %this.imageAsset);

	if(!%isAnimation && !%isImage)
	{
		return false;
	}

	// Unless the strip is what produced this value in the first place. Reloading
	// it from the asset mid-commit would throw away the selection and the caret
	// for a list it already holds. The same guard shape AssetInspectorPane uses.
	if(%isAnimation && !%this.committing)
	{
		%this.timelinePane.load(%this.imageAssetId, trim(%this.animationAsset.getAnimationFrames()));
	}

	// The image may have been re-cut, so the palette's frame count has moved --
	// and so has what the animation's frames mean.
	if(%isImage)
	{
		%this.palettePane.reload();
	}

	%this.resyncPreview();
	return true;
}

// Put the preview back the way it was before the write disturbed it.
//
// Three cases, because ImageFrameProviderCore::onAssetRefreshed has already had
// its say by the time this runs: it calls playAnimation on a RUNNING preview,
// restarting it from slot zero, and does nothing at all to a finished one. And
// playAnimation opens by clearing the pause, so a paused preview comes back
// playing.
function AssetAnimationStage::resyncPreview(%this)
{
	if(!isObject(%this.previewSprite))
	{
		return;
	}

	%this.armPreview();
	%this.previewSprite.pauseAnimation(!%this.playing);

	// Only when a commit of ours put a slot aside. One write raises more than one
	// refresh -- the asset manager fans them out as it re-reads the file it just
	// wrote -- and the later ones arrive with nothing remembered.
	//
	// The obvious fallback, asking the strip where its marker was drawn, is a
	// trap: that marker is a cached value updated once a frame in onPreRender, so
	// mid-script it is whatever the last rendered frame said. Restoring from it
	// undid the correct restore the first refresh had just made, which is how the
	// playhead ended up back at zero after every edit.
	//
	// And by SLOT, not by image frame: a slot's meaning shifts when something is
	// inserted before it, so the preview can appear to skip -- but tracking the
	// image frame breaks the moment a frame appears twice, which is what a hold is.
	if(%this.resumeSlot >= 0)
	{
		%this.scrubTo(%this.resumeSlot);
	}
}

// A divider moved, or the editor was resized. The sprite is already there and
// only its size is wrong, so there is nothing to rebuild.
function AssetAnimationStage::resizePreview(%this)
{
	if(!%this.built || %this.busy || !isObject(%this.previewSprite) || !isObject(%this.imageAsset))
	{
		return false;
	}

	%this.previewSprite.setSize(%this.admin.assetWindow.getWorldSize(%this.imageAsset.getFrameSize(0)));
	return true;
}

//-----------------------------------------------------------------------------
// Editing. Every path that changes the list ends here, and this is the only
// place in the editor that writes the animation's frames.
//-----------------------------------------------------------------------------

function AssetAnimationStage::commitFrames(%this, %frames)
{
	if(!%this.built || !isObject(%this.animationAsset))
	{
		return;
	}

	// How many frames there were, asked of the ASSET rather than of the strip:
	// the strip already holds the edited list by the time it reports, so it can
	// no longer say what the animation used to be.
	%before = %this.animationAsset.getAnimationFrameCount();

	// Where the preview is, captured BEFORE the write, because the engine
	// restarts playback in the middle of it: AssetManager::refreshAsset notifies
	// every AssetPtr pointing at the asset -- which for a sprite means
	// playAnimation, from slot zero -- and only then fires the script onRefresh
	// this editor listens on. By the time we are asked to put things back, the
	// sprite has already forgotten where it was.
	%this.resumeSlot = isObject(%this.previewSprite) ? %this.previewSprite.getAnimationFrame() : -1;

	// Guarded because the write comes straight back: every asset setter ends in
	// refreshAsset, which rewrites the .animation.taml and fires onRefresh
	// synchronously, inside this call.
	%this.committing = true;
	%this.animationAsset.setAnimationFrames(%frames);
	%this.committing = false;

	// Before the slot is forgotten, because this writes the asset a second time
	// and every write restarts playback. Cleared only once BOTH are done, so the
	// one remembered slot covers the pair -- forgetting it in between left the
	// second refresh with nothing to restore, and the preview back at frame zero
	// after every edit.
	%this.keepFrameRate(%before);

	%this.resumeSlot = -1;
}

// Hold the per-frame rate steady across an edit, when the user has asked for it.
//
// Uniform timing means AnimationTime is shared out over however many frames there
// are, so adding one makes every frame play faster and the animation no longer
// lasts as long. Which of those two a person wants is genuinely a matter of what
// they are doing -- lengthening a walk cycle, or dropping in a hold without
// speeding everything up -- so it is a switch, off by default, and the info line
// in the inspector always shows both numbers either way.
function AssetAnimationStage::keepFrameRate(%this, %beforeCount)
{
	%afterCount = %this.timelinePane.strip.getCellCount();

	if(%beforeCount < 1 || %afterCount == %beforeCount)
	{
		return;
	}

	if(!EditorPreferences.get("assetAnimationKeepFrameRate", false))
	{
		return;
	}

	%time = %this.animationAsset.getAnimationTime() * (%afterCount / %beforeCount);

	// Clamped by hand rather than with mClamp, which rounds to a whole number and
	// would turn every animation shorter than a second into no time at all. The
	// floor matters: a time of zero divides by zero in ImageFrameProviderCore, so
	// nothing may ever write one.
	if(%time < 0.01) { %time = 0.01; }
	if(%time > 3600) { %time = 3600; }

	%this.committing = true;
	%this.animationAsset.setAnimationTime(%time);
	%this.committing = false;
}

// The inspector wrote something. Most of its fields do not concern the stage --
// the refresh they raise is absorbed like any other -- but changing the image
// asset moves what every frame number means, so the palette and the timeline
// both have to be re-pointed at it.
function AssetAnimationStage::onInspectorCommit(%this)
{
	if(!%this.built || %this.busy || !isObject(%this.animationAsset))
	{
		return;
	}

	%imageAssetId = %this.animationAsset.getImage();
	if(%imageAssetId $= %this.imageAssetId)
	{
		return;
	}

	%this.imageAssetId = %imageAssetId;
	%this.palettePane.load(%imageAssetId);
	%this.timelinePane.load(%imageAssetId, trim(%this.animationAsset.getAnimationFrames()));

	%this.admin.transportBar.refresh();
}

function AssetAnimationStage::setCycle(%this, %on)
{
	if(!isObject(%this.animationAsset))
	{
		return;
	}

	// Writes the file, like every other asset edit. The write comes back through
	// absorbRefresh, which re-arms the preview; there is nothing else to do.
	%this.animationAsset.setAnimationCycle(%on);
}

function AssetAnimationStage::appendFrame(%this, %frame)
{
	if(!%this.built)
	{
		return;
	}

	%this.timelinePane.appendFrame(%frame);
}

function AssetAnimationStage::setFrames(%this, %frames)
{
	if(!%this.built)
	{
		return;
	}

	%this.timelinePane.setFrames(%frames);
}

function AssetAnimationStage::appendFrames(%this, %frames)
{
	if(!%this.built)
	{
		return;
	}

	%this.timelinePane.appendFrames(%frames);
}

function AssetAnimationStage::openRangeDialog(%this)
{
	if(!%this.built)
	{
		return;
	}

	%width = 460;
	%height = 260;

	%dialog = new GuiControl()
	{
		class = "AssetAnimationRangeDialog";
		superclass = "EditorDialog";
		dialogSize = (%width + 8) SPC (%height + 8);
		dialogCanClose = true;
		dialogText = "Frame Range";
		stage = %this;
	};
	%dialog.init(%width, %height);

	Canvas.pushDialog(%dialog);
}
