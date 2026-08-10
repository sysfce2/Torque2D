//-----------------------------------------------------------------------------
// What the Asset Manager does when an asset changes underneath it.
//
// Every setter on an asset ends in refreshAsset(), which saves the asset's file
// and fires this. So this is the one place that hears about a change however it
// was made -- from the inspector, from the Explicit Frames or Image Layers tab,
// or as a cascade from some other asset that this one depends on.
//
// Three things have to be told:
//
//   the library    a tile caches the name, description and category it is
//                  searched and sorted by, and the inspector edits all three
//   the preview    the scene showing the asset is built from its values
//   the inspector  a change made on another tab -- explicit mode, a new layer --
//                  is a change to what the inspector is showing
//-----------------------------------------------------------------------------

function AssetBase::onRefresh(%this)
{
	// The library has nothing loaded while the Asset Manager is shut, and this
	// also fires as assets are acquired during the load itself, before there is
	// anything to refresh.
	if(!AssetAdmin.isOpen)
	{
		return;
	}

	AssetAdmin.libWindow.onAssetRefreshed(%this.getAssetId());
	AssetAdmin.inspector.onAssetRefreshed(%this);

	// Redraws the preview. It does not re-enter the inspector: onClick only loads
	// an asset into it when the selection actually moved.
	AssetAdmin.refreshPreview(%this);
}
