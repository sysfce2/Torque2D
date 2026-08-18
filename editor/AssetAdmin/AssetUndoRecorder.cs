//-----------------------------------------------------------------------------
// Undo and redo for assets.
//
// The unit of undo here is a whole asset, snapshotted. That is not the shape the
// Gui Editor uses -- its recorder stores individual field writes -- and the
// reason for the difference is that two of the Asset Manager's editors never
// reach TorqueScript at all. GuiParticleGraphInspector does every graph key drag
// in C++, and the stock GuiInspector writes particle, emitter, font and audio
// fields straight onto the object. A recorder built out of script-side writes
// would be blind to both, which is to say blind to the particle editor, which is
// the thing that most needed undo in the first place.
//
// What every change does reach is AssetBase::onRefresh. So this keeps a snapshot
// of each asset as it currently stands -- the baseline -- and when a change is
// announced, the baseline is what the asset looked like before it. Push that,
// take a new one, and the history builds itself no matter who made the change or
// in what language.
//
// Snapshots are engine objects (AssetBase::createStateSnapshot). They are
// unowned, so making one is inert: no file, no dirty mark, no notification, and
// for an image no texture work.
//
// Each asset gets its own history, because assets are edited independently and
// any of them can be left unsaved. See [[AssetAdmin]].
//
// NOTE ON ARGUMENTS: the methods that need to read or write an asset take the
// asset OBJECT, not its id, and every caller already has one. Looking an asset up
// by id from script means acquireAsset/releaseAsset, and that pair is not free of
// consequences: releasing an asset whose reference count was zero unloads it, so
// merely asking about an asset could delete it.
//-----------------------------------------------------------------------------

// How many steps to keep per asset. A snapshot of a particle asset with a few
// emitters is not free, and no one is stepping back further than this by hand.
$AssetUndoRecorder::maxSteps = 50;

function AssetUndoRecorder::onAdd(%this)
{
	// The snapshots live in groups so that dropping a history deletes every
	// snapshot in it -- a SimSet would leave them behind.
	%this.history = new SimGroup();

	// Nesting depth of the current transaction, and whether it has already
	// pushed a step. See begin().
	%this.txDepth = 0;
	%this.txPushed = false;

	// Set by whoever is about to change something, consumed by the step it
	// produces.
	%this.pendingLabel = "";

	// True while this recorder is the one doing the changing, so that replaying a
	// step does not record itself.
	%this.replaying = false;
}

function AssetUndoRecorder::onRemove(%this)
{
	if(isObject(%this.history))
	{
		%this.history.deleteObjects();
		%this.history.delete();
	}
}

//-----------------------------------------------------------------------------
// Per-asset bookkeeping.
//
// Three things are kept for each asset: the baseline snapshot, a group of undo
// steps and a group of redo steps, all indexed by asset id.
//-----------------------------------------------------------------------------

function AssetUndoRecorder::isTracking(%this, %assetId)
{
	return isObject(%this.undoGroup[%assetId]);
}

// Start keeping history for an asset, if we are not already. Called when an
// asset is loaded into the inspector.
function AssetUndoRecorder::track(%this, %asset)
{
	if(!isObject(%asset))
	{
		return;
	}

	%assetId = %asset.getAssetId();
	if(%assetId $= "" || %this.isTracking(%assetId))
	{
		return;
	}

	%this.undoGroup[%assetId] = new SimGroup();
	%this.redoGroup[%assetId] = new SimGroup();
	%this.history.add(%this.undoGroup[%assetId]);
	%this.history.add(%this.redoGroup[%assetId]);

	%this.baseline[%assetId] = %this.takeSnapshot(%asset);
}

// Forget an asset's history. Reverting does this, because the steps describe a
// document that no longer exists; so does deleting the asset.
function AssetUndoRecorder::forget(%this, %assetId)
{
	if(!%this.isTracking(%assetId))
	{
		return;
	}

	%this.undoGroup[%assetId].deleteObjects();
	%this.undoGroup[%assetId].delete();
	%this.undoGroup[%assetId] = "";

	%this.redoGroup[%assetId].deleteObjects();
	%this.redoGroup[%assetId].delete();
	%this.redoGroup[%assetId] = "";

	if(isObject(%this.baseline[%assetId]))
	{
		%this.baseline[%assetId].delete();
	}
	%this.baseline[%assetId] = "";
}

// A snapshot of the asset as it stands, tagged with what its unsaved state was
// at the time. The tag is what lets an undo that lands back on the saved state
// report the asset as clean again.
//
// Every snapshot goes into the history group immediately, including the ones that
// are only ever a baseline. createStateSnapshot hands back a registered object
// that belongs to nobody, and a baseline is not in either stack -- so without a
// home here it would still be alive after this recorder was deleted. Pushing one
// onto a stack later just reparents it, which is what a SimGroup add does.
function AssetUndoRecorder::takeSnapshot(%this, %asset)
{
	%snapshot = %asset.createStateSnapshot();

	if(isObject(%snapshot))
	{
		%this.setWasDirty(%snapshot, %asset.isAssetDirty());
		%this.setStepLabel(%snapshot, "");
		%this.history.add(%snapshot);
	}

	return %snapshot;
}

//-----------------------------------------------------------------------------
// What the recorder knows about each snapshot: what to call the step, and
// whether the asset counted as unsaved at that point.
//
// Kept HERE, keyed by the snapshot's id, and deliberately not as fields on the
// snapshot itself. A snapshot is copied onto the live asset when it is restored,
// and copyFieldsFrom carries dynamic fields across -- so a stepLabel written on a
// snapshot ends up on the asset, and from there into the asset's .taml the next
// time it is saved. Which is exactly what happened: real content files grew
// stepLabel="Set Frames" wasDirty="1" the first time anyone undid and saved.
//
// An object id can be reused once its snapshot has been deleted, but every
// snapshot has both of these written by takeSnapshot before anything reads them,
// so a recycled id is always overwritten rather than inherited.
//-----------------------------------------------------------------------------

function AssetUndoRecorder::setStepLabel(%this, %snapshot, %label)
{
	%this.stepLabelOf[%snapshot] = %label;
}

function AssetUndoRecorder::getStepLabel(%this, %snapshot)
{
	return %this.stepLabelOf[%snapshot];
}

function AssetUndoRecorder::setWasDirty(%this, %snapshot, %wasDirty)
{
	%this.wasDirtyOf[%snapshot] = %wasDirty;
}

function AssetUndoRecorder::getWasDirty(%this, %snapshot)
{
	return %this.wasDirtyOf[%snapshot];
}

//-----------------------------------------------------------------------------
// Transactions.
//
// One thing the user did should be one step, and a couple of paths in the Asset
// Manager write twice for a single action -- committing animation frames also
// rewrites the frame rate when Keep Frame Rate is on. Wrapping those in
// begin/end folds them together: the first change inside the transaction pushes
// a step, and the rest only move the baseline forward.
//
// Nesting is by depth, so an inner begin/end pair inside an outer one is
// absorbed rather than closing the transaction early.
//-----------------------------------------------------------------------------

function AssetUndoRecorder::begin(%this, %label)
{
	if(%this.txDepth == 0)
	{
		%this.txPushed = false;
		%this.pendingLabel = %label;
	}

	%this.txDepth++;
}

function AssetUndoRecorder::end(%this)
{
	%this.txDepth--;

	if(%this.txDepth <= 0)
	{
		%this.txDepth = 0;
		%this.txPushed = false;
		%this.pendingLabel = "";
	}
}

// What the next step will be called. Set by the script that is about to make a
// change and knows what to call it; anything that does not say gets "Edit",
// which is what every change made in C++ gets.
function AssetUndoRecorder::setLabel(%this, %label)
{
	if(%this.txDepth == 0)
	{
		%this.pendingLabel = %label;
	}
}

//-----------------------------------------------------------------------------
// Recording.
//-----------------------------------------------------------------------------

// An asset announced a change. %direct is false when the announcement is only a
// cascade -- something this asset reads from was changed, and nothing this asset
// saves has moved -- so there is nothing to record.
function AssetUndoRecorder::onAssetChanged(%this, %asset, %direct)
{
	if(%this.replaying || !%direct || !isObject(%asset))
	{
		return;
	}

	%assetId = %asset.getAssetId();
	if(!%this.isTracking(%assetId))
	{
		return;
	}

	// Inside a transaction, only the first change opens a step.
	if(%this.txDepth > 0 && %this.txPushed)
	{
		%this.rebaseline(%asset);
		return;
	}

	%before = %this.baseline[%assetId];
	if(!isObject(%before))
	{
		%this.rebaseline(%asset);
		return;
	}

	%this.setStepLabel(%before, (%this.pendingLabel $= "") ? "Edit" : %this.pendingLabel);

	%this.undoGroup[%assetId].add(%before);
	%this.trimHistory(%assetId);

	// A new change is a new future: whatever was undone is no longer reachable.
	%this.redoGroup[%assetId].deleteObjects();

	// The baseline object was handed to the undo group, so this is a fresh one
	// rather than a move.
	%this.baseline[%assetId] = %this.takeSnapshot(%asset);

	if(%this.txDepth > 0)
	{
		%this.txPushed = true;
	}
	else
	{
		%this.pendingLabel = "";
	}

	%this.refreshUI();
}

// Move the baseline to where the asset is now, without recording anything.
function AssetUndoRecorder::rebaseline(%this, %asset)
{
	%assetId = %asset.getAssetId();

	if(isObject(%this.baseline[%assetId]))
	{
		%this.baseline[%assetId].delete();
	}

	%this.baseline[%assetId] = %this.takeSnapshot(%asset);
}

function AssetUndoRecorder::trimHistory(%this, %assetId)
{
	%group = %this.undoGroup[%assetId];

	while(%group.getCount() > $AssetUndoRecorder::maxSteps)
	{
		// The oldest step is the one at the front.
		%oldest = %group.getObject(0);
		%group.remove(%oldest);
		%oldest.delete();
	}
}

//-----------------------------------------------------------------------------
// Stepping.
//-----------------------------------------------------------------------------

function AssetUndoRecorder::getUndoCount(%this, %assetId)
{
	return %this.isTracking(%assetId) ? %this.undoGroup[%assetId].getCount() : 0;
}

function AssetUndoRecorder::getRedoCount(%this, %assetId)
{
	return %this.isTracking(%assetId) ? %this.redoGroup[%assetId].getCount() : 0;
}

function AssetUndoRecorder::getUndoLabel(%this, %assetId)
{
	%count = %this.getUndoCount(%assetId);

	return (%count == 0) ? "" : %this.getStepLabel(%this.undoGroup[%assetId].getObject(%count - 1));
}

function AssetUndoRecorder::getRedoLabel(%this, %assetId)
{
	%count = %this.getRedoCount(%assetId);

	return (%count == 0) ? "" : %this.getStepLabel(%this.redoGroup[%assetId].getObject(%count - 1));
}

function AssetUndoRecorder::undo(%this, %asset)
{
	if(!isObject(%asset))
	{
		return false;
	}

	%assetId = %asset.getAssetId();
	if(%this.getUndoCount(%assetId) == 0)
	{
		return false;
	}

	%group = %this.undoGroup[%assetId];
	%step = %group.getObject(%group.getCount() - 1);
	%group.remove(%step);

	// Where we are now becomes the way back.
	%current = %this.baseline[%assetId];
	%this.setStepLabel(%current, %this.getStepLabel(%step));
	%this.redoGroup[%assetId].add(%current);

	%this.applyStep(%asset, %step);

	return true;
}

function AssetUndoRecorder::redo(%this, %asset)
{
	if(!isObject(%asset))
	{
		return false;
	}

	%assetId = %asset.getAssetId();
	if(%this.getRedoCount(%assetId) == 0)
	{
		return false;
	}

	%group = %this.redoGroup[%assetId];
	%step = %group.getObject(%group.getCount() - 1);
	%group.remove(%step);

	%current = %this.baseline[%assetId];
	%this.setStepLabel(%current, %this.getStepLabel(%step));
	%this.undoGroup[%assetId].add(%current);

	%this.applyStep(%asset, %step);

	return true;
}

// Put a snapshot back onto the asset and make it the new baseline.
//
// The snapshot is not deleted: it becomes the baseline, which is exactly what it
// describes. The replaying guard stops the change this causes from being
// recorded as a fresh step.
function AssetUndoRecorder::applyStep(%this, %asset, %step)
{
	%assetId = %asset.getAssetId();

	%this.replaying = true;
	%asset.restoreStateSnapshot(%step);
	%this.replaying = false;

	// restoreStateSnapshot deliberately leaves the unsaved state alone, because
	// only this knows what the restored state means: back at the last save is
	// clean, anywhere else is not.
	AssetDatabase.setAssetDirty(%assetId, %this.getWasDirty(%step));

	// Taken off its stack above, so it needs a home again as the baseline.
	%this.history.add(%step);
	%this.baseline[%assetId] = %step;

	%this.refreshUI();
}

//-----------------------------------------------------------------------------
// Saving and reverting.
//
// Both settle the asset against its file, so both change what every step in its
// history means about being saved.
//-----------------------------------------------------------------------------

// After a save, the state we are in is the saved one -- but every step still in
// the history describes a state that is not, and so does every redo step.
function AssetUndoRecorder::onAssetSaved(%this, %assetId)
{
	if(!%this.isTracking(%assetId))
	{
		return;
	}

	%undoGroup = %this.undoGroup[%assetId];
	for(%i = 0; %i < %undoGroup.getCount(); %i++)
	{
		%this.setWasDirty(%undoGroup.getObject(%i), true);
	}

	%redoGroup = %this.redoGroup[%assetId];
	for(%i = 0; %i < %redoGroup.getCount(); %i++)
	{
		%this.setWasDirty(%redoGroup.getObject(%i), true);
	}

	if(isObject(%this.baseline[%assetId]))
	{
		%this.setWasDirty(%this.baseline[%assetId], false);
	}

	%this.refreshUI();
}

// A revert throws the document away and starts again from the file, so the steps
// that described the old one go with it.
function AssetUndoRecorder::onAssetReverted(%this, %asset)
{
	if(!isObject(%asset))
	{
		return;
	}

	%this.forget(%asset.getAssetId());
	%this.track(%asset);

	%this.refreshUI();
}

function AssetUndoRecorder::refreshUI(%this)
{
	if(isObject(AssetAdmin.inspector))
	{
		AssetAdmin.inspector.refreshDocumentBar();
	}
}
