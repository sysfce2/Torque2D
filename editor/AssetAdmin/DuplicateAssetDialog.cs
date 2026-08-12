//-----------------------------------------------------------------------------
// Branch an asset.
//
// Duplicating copies the asset AS IT STANDS IN MEMORY, unsaved edits and all --
// that is what makes it useful next to undo. Try something on a particle, decide
// you want to keep both, duplicate, and the copy has what is on screen rather
// than what was last written to disk.
//
// The copy is written and declared immediately. A declared asset has to have a
// file: there is no such thing as an asset that exists only in memory and still
// appears in the library. So the copy starts saved, and the original keeps
// whatever unsaved state it had.
//
// The engine does the work in AssetManager::duplicateAsset. This is the name and
// the module, and the checks that stop the copy landing somewhere that will not
// have it.
//-----------------------------------------------------------------------------

function DuplicateAssetDialog::init(%this, %width, %height)
{
	//Get the dialog contents
	%window = %this.getObject(0);
	%content = %window.getObject(0);

	%form = new GuiGridCtrl()
	{
		class = "EditorForm";
		extent = %width SPC %height;
		cellSizeX = %width;
		cellSizeY = 50;
	};
	%form.addListener(%this);

	%item = %form.addFormItem("New Asset Name", %width SPC 30);
	%this.assetNameBox = %form.createTextEditItem(%item);
	%this.assetNameBox.Command = %this.getId() @ ".Validate();";

	%content.add(%form);

	// Below whatever the form actually came out as, rather than below a number
	// written here.
	%formBottom = getWord(%form.getPosition(), 1) + getWord(%form.getExtent(), 1);

	// The feedback line says quite a lot in the refusal cases -- the library
	// module one runs to three lines at this width -- and textExtend grows the
	// control downward to fit. Hence the height it starts at, and the clearance
	// below it before the buttons.
	%this.feedback = new GuiControl()
	{
		HorizSizing = "width";
		VertSizing = "anchorTop";
		Position = "12" SPC (%formBottom + 8);
		Extent = (%width - 24) SPC 96;
		text = "";
		textWrap = true;
		textExtend = true;
	};
	ThemeManager.setProfile(%this.feedback, "infoProfile");

	// Measured from the room the content actually has, not from the dialog's own
	// height -- the title bar and border take 34 of it.
	%bottom = %this.contentHeight() - 12;

	%this.cancelButton = new GuiButtonCtrl()
	{
		HorizSizing = "anchorRight";
		VertSizing = "anchorBottom";
		Position = (%width - 222) SPC (%bottom - 32);
		Extent = "100 30";
		Text = "Cancel";
		Command = %this.getID() @ ".onClose();";
	};
	ThemeManager.setProfile(%this.cancelButton, "buttonProfile");

	%this.duplicateButton = new GuiButtonCtrl()
	{
		HorizSizing = "anchorRight";
		VertSizing = "anchorBottom";
		Position = (%width - 112) SPC (%bottom - 34);
		Extent = "100 34";
		Text = "Duplicate";
		Command = %this.getID() @ ".onDuplicate();";
	};
	ThemeManager.setProfile(%this.duplicateButton, "primaryButtonProfile");

	%content.add(%this.feedback);
	%content.add(%this.cancelButton);
	%content.add(%this.duplicateButton);

	// A name that is free to begin with, so the dialog opens ready to go.
	%this.assetNameBox.setText(%this.suggestName());

	%this.Validate();
}

// "thing" becomes "thingCopy", then "thingCopy2", "thingCopy3" and so on until
// one of them is not taken.
function DuplicateAssetDialog::suggestName(%this)
{
	%sourceName = AssetDatabase.getAssetName(%this.sourceAssetId);
	%moduleName = getUnit(%this.sourceAssetId, 0, ":");

	%candidate = %sourceName @ "Copy";
	%suffix = 2;

	while(AssetDatabase.isDeclaredAsset(%moduleName @ ":" @ %candidate))
	{
		%candidate = %sourceName @ "Copy" @ %suffix;
		%suffix++;
	}

	return %candidate;
}

// Where the copy will be written: beside the asset it came from, with the same
// file extension, so the module's DeclaredAssets glob picks it up. A copy that
// landed under a different extension would be written and then never seen again.
function DuplicateAssetDialog::targetPath(%this, %assetName)
{
	%sourcePath = AssetDatabase.getAssetFilePath(%this.sourceAssetId);

	// fileBase strips only the last extension, and these are doubled --
	// "rocket.image.taml" -- so take everything after the first dot of the name.
	%sourceFile = fileName(%sourcePath);
	%firstDot = strpos(%sourceFile, ".");
	%extension = (%firstDot == -1) ? "asset.taml" : getSubStr(%sourceFile, %firstDot + 1, strlen(%sourceFile));

	return pathConcat(filePath(%sourcePath), %assetName @ "." @ %extension);
}

function DuplicateAssetDialog::Validate(%this)
{
	%this.duplicateButton.active = false;

	%assetName = %this.assetNameBox.getText();
	%moduleName = getUnit(%this.sourceAssetId, 0, ":");

	if(%assetName $= "")
	{
		%this.feedback.setText("The copy must have an Asset Name.");
		return false;
	}

	if(AssetDatabase.isDeclaredAsset(%moduleName @ ":" @ %assetName))
	{
		%this.feedback.setText("An asset by this name already exists in this module. Try choosing a different name.");
		return false;
	}

	%module = AssetDatabase.getAssetModule(%this.sourceAssetId);
	if(isObject(%module) && %module.Synchronized)
	{
		%this.feedback.setText("You cannot add assets to a library module. Updates to the module would remove your assets. Instead, copy this asset into your own module.");
		return false;
	}

	%this.duplicateButton.active = true;
	%this.feedback.setText("The copy will be made beside the original, and will include any changes you have not saved.");
	return true;
}

function DuplicateAssetDialog::onDuplicate(%this)
{
	if(!%this.Validate())
	{
		return;
	}

	%assetName = %this.assetNameBox.getText();
	%moduleName = getUnit(%this.sourceAssetId, 0, ":");
	%assetId = %moduleName @ ":" @ %assetName;
	%assetType = AssetDatabase.getAssetType(%this.sourceAssetId);

	if(!AssetDatabase.duplicateAsset(%this.sourceAssetId, %this.targetPath(%assetName), %assetName))
	{
		%this.feedback.setText("The copy could not be made. See the console for what went wrong.");
		return;
	}

	// Put it in the library and select it, the same way a newly created asset is.
	%button = AssetAdmin.Dictionary[%assetType].getButton(%assetId);
	if(!isObject(%button))
	{
		%button = AssetAdmin.Dictionary[%assetType].addButton(%assetId);
	}
	%button.onClick();

	%this.onClose();
}
