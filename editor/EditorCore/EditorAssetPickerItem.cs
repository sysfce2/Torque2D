
//-----------------------------------------------------------------------------
// One cell in the asset picker's grid: a selectable button wearing a thumbnail
// of the asset it stands for.
//
// The spawner sets assetId, assetType and owner. A click goes straight back out
// as owner.onItemClicked(%this) -- the item never learns what being chosen
// means, only which asset it is, so the dialog stays the single place that
// knows about selection, detail text and the callback.
//
// searchKey is the lowercased asset id, worked out once here rather than on
// every keystroke, because filtering walks every button in the grid.
//-----------------------------------------------------------------------------

function EditorAssetPickerItem::onAdd(%this)
{
	// A cell looks the same wherever it is, so it dresses itself: the grid sizes
	// it anyway, and the spawner is left setting only what it actually decides --
	// which asset this is and who to tell about a click.
	%this.HorizSizing = "center";
	%this.VertSizing = "center";
	%this.setExtent(56, 56);
	%this.setText("");
	%this.setChosen(false);
	ThemeManager.setProfile(%this, "tipProfile", "TooltipProfile");

	%this.searchKey = strlwr(%this.assetId);
	%this.Tooltip = %this.assetId;

	// Only the picture types have a picture to show. The rest name themselves,
	// which reads better than a grid of identical placeholder icons -- and it
	// keeps EditorCore off AssetAdmin's icon sheet, which it must not depend on.
	if(%this.assetType $= "ImageAsset")
	{
		%thumbnail = %this.buildThumbnail();
		%thumbnail.setImage(%this.assetId);
	}
	else if(%this.assetType $= "AnimationAsset")
	{
		%thumbnail = %this.buildThumbnail();
		%thumbnail.setAnimation(%this.assetId);
	}
	else
	{
		%this.setText(getUnit(%this.assetId, 1, ":"));
	}
}

// Centered and proportional so a wide image and a tall one both sit square in
// the cell. UseInput is off: without it the sprite eats the click meant for the
// button underneath it.
function EditorAssetPickerItem::buildThumbnail(%this)
{
	%thumbnail = new GuiSpriteCtrl()
	{
		HorizSizing = "center";
		VertSizing = "center";
		Position = "0 0";
		Extent = "50 50";
		minExtent = "50 50";
		constrainProportions = true;
		fullSize = true;
		UseInput = false;
	};
	ThemeManager.setProfile(%thumbnail, "spriteProfile");
	%this.add(%thumbnail);
	return %thumbnail;
}

function EditorAssetPickerItem::onClick(%this)
{
	%this.owner.onItemClicked(%this);
}

// Selection is shown by swapping profiles rather than by drawing anything, so a
// theme decides what a chosen cell looks like.
function EditorAssetPickerItem::setChosen(%this, %chosen)
{
	ThemeManager.setProfile(%this, %chosen ? "itemSelectedProfile" : "itemSelectProfile");
}
