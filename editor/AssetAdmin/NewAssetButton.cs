//NewAssetButton.cs

// The library's per-group New button. The dialogs it opens are also on the File
// menu, so the bodies live on AssetAdmin and this is only the button half - .type
// is the asset class name the group holds ("ImageAsset"), which is exactly the
// back half of AssetAdmin::newImageAsset.
function NewAssetButton::onClick(%this)
{
	AssetAdmin.call("new" @ %this.type);
}
