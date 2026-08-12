//-----------------------------------------------------------------------------
// What the Asset Manager does when an asset changes underneath it.
//
// Every setter on an asset ends in refreshAsset(), which marks it unsaved and
// fires this. So this is the one place that hears about a change however it was
// made -- from the inspector, from the Explicit Frames or Image Layers tab, from
// the particle graph editor down in C++, or as a cascade from some other asset
// that this one depends on.
//
// Note refreshAsset does NOT write the file any more. Saving is a thing the user
// asks for; see AssetInspector's document bar and [[AssetUndoRecorder]].
//
// %direct separates the two reasons this fires:
//
//   true   this asset was changed, and now has unsaved work in it
//   false  something this asset READS FROM was changed, so whatever it derives
//          from that needs rebuilding -- but nothing it saves has moved, and it
//          is not unsaved on account of it
//
// Four things have to be told:
//
//   the recorder   a direct change is one step of undo, and it needs the state
//                  from before it, which is the snapshot it has been holding
//   the library    a tile caches the name, description and category it is
//                  searched and sorted by, and the inspector edits all three
//   the preview    the scene showing the asset is built from its values
//   the inspector  a change made on another tab -- explicit mode, a new layer --
//                  is a change to what the inspector is showing
//-----------------------------------------------------------------------------

function AssetBase::onRefresh(%this, %direct)
{
	// The library has nothing loaded while the Asset Manager is shut, and this
	// also fires as assets are acquired during the load itself, before there is
	// anything to refresh.
	if(!AssetAdmin.isOpen)
	{
		return;
	}

	// First, so that the step records the state from before anything below reacts
	// to the change.
	AssetAdmin.undoRecorder.onAssetChanged(%this, %direct);

	AssetAdmin.libWindow.onAssetRefreshed(%this.getAssetId());
	AssetAdmin.inspector.onAssetRefreshed(%this);

	// Redraws the preview. It does not re-enter the inspector: onClick only loads
	// an asset into it when the selection actually moved.
	AssetAdmin.refreshPreview(%this);
}

//-----------------------------------------------------------------------------
// An asset gained or lost unsaved changes.
//
// A global, fired by AssetManager::markAssetDirty, saveAsset, revertAsset and
// setAssetDirty -- only on the edge, never on every change. It exists so the
// library can mark a tile without asking every asset every frame, and it is
// separate from onRefresh because the two do not coincide: a change to an already
// unsaved asset fires onRefresh and not this, and a save fires this and not
// onRefresh.
//-----------------------------------------------------------------------------

function onAssetDirtyChanged(%assetId)
{
	if(!isObject(AssetAdmin) || !AssetAdmin.isOpen)
	{
		return;
	}

	AssetAdmin.libWindow.onAssetDirtyChanged(%assetId);
	AssetAdmin.inspector.refreshDocumentBar();
}
