//-----------------------------------------------------------------------------
// What the Asset Manager does when an asset changes underneath it.
//
// AssetBase::setAssetName, setAssetDescription and setAssetCategory all end in
// refreshAsset(), which fires this. The inspector edits all three, and the
// library now searches and sorts by all three -- so a tile that cached them has
// to be told, or the search box goes on answering about the old values.
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

	if(isObject(AssetAdmin.chosenButton))
	{
		AssetAdmin.chosenButton.onClick();
	}
}
