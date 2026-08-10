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

// One asset in the library: a picture and the asset's name, arranged as a tile
// or as a row depending on the library's view mode.
//
// The picture is whatever the asset can show of itself -- the image, the running
// animation -- falling back to a flat icon for the kinds that have no likeness.
// The name is drawn as well as being the tooltip, because the library can now be
// searched and sorted by it and an order you cannot read is not an order.

$AssetDictionaryButton::gridArt = 50;

// The band at the bottom of a tile the caption is allowed to fill.
$AssetDictionaryButton::gridCaption = 34;

$AssetDictionaryButton::rowArt = 28;
$AssetDictionaryButton::rowTextLeft = 36;

function AssetDictionaryButton::onAdd(%this)
{
	%this.buildSearchKey();
	%this.buildCaption();

	%this.call("load" @ %this.type, %this.assetID);

	if(%this.viewMode $= "")
	{
		%this.viewMode = "grid";
	}
	%this.setViewMode(%this.viewMode);
}

// Everything the library asks about this asset, worked out once.
//
// The name, description and category all come off the AssetDefinition without
// loading anything, and any of the three may be empty -- most assets carry no
// description and no category at all. They are lowercased here rather than in
// the filter because the filter walks every tile on every keystroke, and strstr
// is case sensitive (unlike $=, which is not).
function AssetDictionaryButton::buildSearchKey(%this)
{
	%name = AssetDatabase.getAssetName(%this.assetID);
	%description = AssetDatabase.getAssetDescription(%this.assetID);
	%category = AssetDatabase.getAssetCategory(%this.assetID);

	%this.assetName = %name;
	%this.assetCategory = %category;

	%this.sortName = strlwr(%name);
	%this.sortCategory = strlwr(%category);

	// Trimmed, because two of the three are usually empty: without it an asset
	// with neither a description nor a category ends up keyed "name  ", and one
	// with nothing at all ends up keyed "  " -- which a needle of a single space
	// would match. (The needle is trimmed too, so that case cannot arise today;
	// the key should not depend on that staying true.)
	%this.searchKey = trim(strlwr(%name SPC %description SPC %category));
}

// The three fields are editable in the inspector, so what was worked out once
// has to be worked out again when they change. Everything the tile shows or is
// found by comes from here.
function AssetDictionaryButton::refreshKeys(%this)
{
	%this.buildSearchKey();

	%this.caption.setText(%this.assetName);
	%this.Tooltip = %this.assetName;
}

function AssetDictionaryButton::buildCaption(%this)
{
	%this.caption = new GuiControl()
	{
		Position = "0 0";
		Extent = "60 20";
		MinExtent = "0 0";
		Text = %this.assetName;
		align = "center";
		vAlign = "bottom";
		textWrap = true;
		UseInput = false;
	};
	ThemeManager.setProfile(%this.caption, "labelProfile");
	%this.add(%this.caption);
}

//-----------------------------------------------------------------------------
// The picture, one loader per asset kind.
//-----------------------------------------------------------------------------

function AssetDictionaryButton::loadImageAsset(%this, %assetID)
{
	%imageAsset = AssetDatabase.acquireAsset(%assetID);

	%texture = %this.buildIcon();
	%texture.setImage(%assetID);

	%this.ImageAsset = %imageAsset;
	%this.ImageAssetID = %assetID;

	%this.add(%texture);
}

function AssetDictionaryButton::loadAnimationAsset(%this, %assetID)
{
	%animationAsset = AssetDatabase.acquireAsset(%assetID);
	%imageAssetID = %animationAsset.getImage();
	%imageAsset = AssetDatabase.acquireAsset(%imageAssetID);

	%texture = %this.buildIcon();
	%texture.setAnimation(%assetID);

	%this.ImageAsset = %imageAsset;
	%this.imageAssetID = %imageAssetID;
	%this.AnimationAsset = %animationAsset;
	%this.AnimationAssetID = %assetID;

	%this.add(%texture);
}

function AssetDictionaryButton::loadParticleAsset(%this, %assetID)
{
	%particleAsset = AssetDatabase.acquireAsset(%assetID);

	%texture = %this.buildIcon();
	%texture.setImage("AssetAdmin:assetIcons");
	%texture.setImageFrame(0);

	%this.particleAsset = %particleAsset;
	%this.particleAssetID = %assetID;

	%this.add(%texture);
}

function AssetDictionaryButton::loadFontAsset(%this, %assetID)
{
	%fontAsset = AssetDatabase.acquireAsset(%assetID);

	%texture = %this.buildIcon();
	%texture.setImage("AssetAdmin:assetIcons");
	%texture.setImageFrame(1);

	%this.fontAsset = %fontAsset;
	%this.fontAssetID = %assetID;

	%this.add(%texture);
}

function AssetDictionaryButton::loadAudioAsset(%this, %assetID)
{
	%audioAsset = AssetDatabase.acquireAsset(%assetID);

	%texture = %this.buildIcon();
	%texture.setImage("AssetAdmin:assetIcons");
	%texture.setImageFrame(3);

	%this.audioAsset = %audioAsset;
	%this.audioAssetID = %assetID;

	%this.add(%texture);
}

function AssetDictionaryButton::loadSpineAsset(%this, %assetID)
{
	%spineAsset = AssetDatabase.acquireAsset(%assetID);

	%texture = %this.buildIcon();
	%texture.setImage("AssetAdmin:assetIcons");
	%texture.setImageFrame(2);

	%this.spineAsset = %spineAsset;
	%this.spineAssetID = %assetID;

	%this.add(%texture);
}

// MinExtent is deliberately tiny: setViewMode drives this down to the row art
// size, and a minimum of the tile size would silently refuse.
function AssetDictionaryButton::buildIcon(%this)
{
	%this.icon = new GuiSpriteCtrl()
	{
		class = "AssetDictionarySprite";
		HorizSizing = "center";
		VertSizing = "center";
		Extent = $AssetDictionaryButton::gridArt SPC $AssetDictionaryButton::gridArt;
		minExtent = "8 8";
		Position = "0 0";
		constrainProportions = "1";
		fullSize = "1";
		UseInput = false;
	};
	ThemeManager.setProfile(%this.icon, "spriteProfile");
	return %this.icon;
}

//-----------------------------------------------------------------------------
// View mode.
//-----------------------------------------------------------------------------

// The picture and the caption swap places rather than the tile being rebuilt.
// The sprite scales itself (constrainProportions and fullSize), so only its
// extent and position change -- the image it holds, and an animation part way
// through, are untouched.
function AssetDictionaryButton::setViewMode(%this, %mode)
{
	%this.viewMode = %mode;

	if(!isObject(%this.icon) || !isObject(%this.caption))
	{
		return;
	}

	%w = getWord(%this.getExtent(), 0);
	%h = getWord(%this.getExtent(), 1);

	if(%mode $= "rows")
	{
		%art = $AssetDictionaryButton::rowArt;
		%left = $AssetDictionaryButton::rowTextLeft;

		%this.icon.HorizSizing = "anchorLeft";
		%this.icon.VertSizing = "center";
		%this.icon.setExtent(%art, %art);
		%this.icon.setPosition(4, (%h - %art) / 2);

		%this.caption.HorizSizing = "width";
		%this.caption.VertSizing = "center";
		%this.caption.setExtent(%w - %left - 4, %art);
		%this.caption.setPosition(%left, (%h - %art) / 2);
		%this.caption.align = "left";
		%this.caption.vAlign = "middle";
		%this.caption.textWrap = false;
	}
	else
	{
		%art = $AssetDictionaryButton::gridArt;
		%band = $AssetDictionaryButton::gridCaption;

		%this.caption.HorizSizing = "fill";
		%this.caption.VertSizing = "fill";
		%this.caption.align = "center";
		%this.caption.vAlign = "bottom";
		%this.caption.textWrap = true;
		%this.caption.applySizing();

		// And that fill is how the button's own border inset gets measured: what
		// the caption reports back after filling IS the content rect.
		%innerH = getWord(%this.caption.getExtent(), 1);

		%this.icon.HorizSizing = "center";
		%this.icon.VertSizing = "anchorTop";
		%this.icon.setExtent(%art, %art);
		%this.icon.setPosition(0, (%innerH - %band - %art) / 2);
		%this.icon.applySizing();
	}
}

//-----------------------------------------------------------------------------
// Selection.
//-----------------------------------------------------------------------------

function AssetDictionaryButton::onClick(%this)
{
	%firstLoad = false;
	if(AssetAdmin.chosenButton != %this)
	{
		if(isObject(AssetAdmin.chosenButton))
		{
			ThemeManager.setProfile(AssetAdmin.chosenButton, "itemSelectProfile");
		}
		ThemeManager.setProfile(%this, "itemSelectedProfile");
		%firstLoad = true;
		AssetAdmin.AssetWindow.resetCamera();
	}

	AssetAdmin.audioPlayButtonContainer.setVisible(false);
	AssetAdmin.AssetWindow.setVisible(true);

	// One line, above the whole chain, and every branch below is untouched. The
	// stage keeps the animation split up if this asset is the one it is already
	// showing and takes it down otherwise, which is the entire answer for the
	// five asset kinds that have never heard of it.
	AssetAdmin.animationStage.retainFor(%this.AnimationAssetID);

	// The animation branch has to stay first: an animation tile caches its image
	// asset too, so the image branch would swallow it.
	if(isObject(%this.AnimationAsset) && %this.AnimationAssetID !$= "")
	{
		AssetAdmin.AssetWindow.displayAnimationAsset(%this.imageAsset, %this.AnimationAsset, %this.AnimationAssetID);
		AssetAdmin.animationStage.select(%this.imageAsset, %this.AnimationAsset, %this.AnimationAssetID);
		if(%firstLoad)
		{
			AssetAdmin.inspector.loadAnimationAsset(%this.AnimationAsset, %this.AnimationAssetID);
		}
	}
	else if(isObject(%this.ImageAsset) && %this.ImageAssetID !$= "")
	{
		AssetAdmin.AssetWindow.displayImageAsset(%this.ImageAsset, %this.ImageAssetID);
		if(%firstLoad)
		{
			AssetAdmin.inspector.loadImageAsset(%this.ImageAsset, %this.ImageAssetID);
		}
	}
	else if(isObject(%this.ParticleAsset) && %this.ParticleAssetID !$= "")
	{
		AssetAdmin.AssetWindow.displayParticleAsset(%this.ParticleAsset, %this.ParticleAssetID);
		if(%firstLoad)
		{
			AssetAdmin.inspector.loadParticleAsset(%this.ParticleAsset, %this.ParticleAssetID);
		}
	}
	else if(isObject(%this.FontAsset) && %this.FontAssetID !$= "")
	{
		AssetAdmin.AssetWindow.displayFontAsset(%this.FontAsset, %this.FontAssetID);
		if(%firstLoad)
		{
			AssetAdmin.inspector.loadFontAsset(%this.FontAsset, %this.FontAssetID);
		}
	}
	else if(isObject(%this.AudioAsset) && %this.AudioAssetID !$= "")
	{
		AssetAdmin.AssetWindow.displayAudioAsset(%this.AudioAsset, %this.AudioAssetID);
		if(%firstLoad)
		{
			AssetAdmin.inspector.loadAudioAsset(%this.AudioAsset, %this.AudioAssetID);
		}
	}
	else if(isObject(%this.SpineAsset) && %this.SpineAssetID !$= "")
	{
		AssetAdmin.AssetWindow.displaySpineAsset(%this.SpineAsset, %this.SpineAssetID);
		if(%firstLoad)
		{
			AssetAdmin.inspector.loadSpineAsset(%this.SpineAsset, %this.SpineAssetID);
		}
	}

	AssetAdmin.chosenButton = %this;
}

function AssetDictionaryButton::onRemove(%this)
{
	if(isObject(%this.ImageAsset))
	{
		AssetDatabase.releaseAsset(%this.ImageAssetID);
	}
	if(isObject(%this.AnimationAsset))
	{
		AssetDatabase.releaseAsset(%this.AnimationAssetID);
	}
}
