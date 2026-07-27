
//-----------------------------------------------------------------------------
// A modal picker for choosing an asset by looking at it rather than by typing
// its id from memory. Opened by EditorCore.openAssetPicker; used by the Gui
// Profile Editor's Image Asset row and by the native inspector's browse button,
// which is why it lives in EditorCore rather than in either of them.
//
// The spawner sets assetType, currentAsset, callbackTarget and callbackMethod.
// Choosing calls callbackTarget.callbackMethod(assetId); cancelling calls
// nothing at all, so a caller never has to distinguish "picked nothing" from
// "changed my mind".
//
// The asset database is queried exactly once, when the dialog opens. Filtering
// after that is pure setVisible over the buttons already built, so typing costs
// a walk over the grid and no database work -- which also means the list cannot
// shift under a search the way a re-query would.
//-----------------------------------------------------------------------------

function EditorAssetPickerDialog::init(%this, %width, %height)
{
	%window = %this.getObject(0);
	%content = %window.getObject(0);

	%pad = 10;
	%rowHeight = 30;
	%searchY = 10;
	%gridTop = %searchY + %rowHeight + 8;
	%countWidth = 110;
	%captionWidth = 64;

	// Everything below the grid is measured back from the bottom edge so the
	// grid absorbs the slack when the window is resized -- which is worth doing
	// here, unlike the other editor dialogs, because a wider picker shows more
	// columns of art.
	%detailY = %height - 96;
	%gridHeight = (%detailY - 8) - %gridTop;

	%searchLabel = new GuiControl()
	{
		Position = %pad SPC %searchY;
		Extent = %captionWidth SPC %rowHeight;
		Text = "Search";
		align = "left";
		vAlign = "middle";
	};
	ThemeManager.setProfile(%searchLabel, "labelProfile");
	%content.add(%searchLabel);

	// Command fires on every keystroke, which is what makes the list narrow as
	// you type; AltCommand would only fire when the box lost focus.
	%this.searchBox = new GuiTextEditCtrl()
	{
		HorizSizing = "width";
		Position = (%pad + %captionWidth + 6) SPC %searchY;
		Extent = (%width - (%pad * 2) - %captionWidth - 6 - %countWidth - 8) SPC %rowHeight;
		align = "left";
	};
	ThemeManager.setProfile(%this.searchBox, "textEditProfile");
	%this.searchBox.Command = %this.getID() @ ".onSearchChanged();";
	%this.searchBox.ReturnCommand = %this.getID() @ ".onDone();";
	%this.searchBox.EscapeCommand = %this.getID() @ ".onClose();";
	%content.add(%this.searchBox);

	%this.countLabel = new GuiControl()
	{
		HorizSizing = "left";
		Position = (%width - %pad - %countWidth) SPC %searchY;
		Extent = %countWidth SPC %rowHeight;
		Text = "";
		align = "right";
		vAlign = "middle";
	};
	ThemeManager.setProfile(%this.countLabel, "labelProfile");
	%content.add(%this.countLabel);

	%this.scroller = new GuiScrollCtrl()
	{
		HorizSizing = "width";
		VertSizing = "height";
		Position = %pad SPC %gridTop;
		Extent = (%width - (%pad * 2)) SPC %gridHeight;
		hScrollBar = "alwaysOff";
		vScrollBar = "dynamic";
		constantThumbHeight = false;
		showArrowButtons = false;
		scrollBarThickness = 16;
	};
	ThemeManager.setProfile(%this.scroller, "scrollingPanelProfile");
	ThemeManager.setProfile(%this.scroller, "scrollingPanelThumbProfile", "ThumbProfile");
	ThemeManager.setProfile(%this.scroller, "scrollingPanelTrackProfile", "TrackProfile");
	ThemeManager.setProfile(%this.scroller, "scrollingPanelArrowProfile", "ArrowProfile");
	%content.add(%this.scroller);

	// IsExtentDynamic is what lets the grid grow taller than the scroller and so
	// gives the scroll bar something to scroll; without it the grid stays the
	// height it was built at and the rows past the first screenful are unreachable.
	%this.grid = new GuiGridCtrl()
	{
		HorizSizing = "width";
		Position = "0 0";
		Extent = (%width - (%pad * 2) - 16) SPC 60;
		CellSizeX = 60;
		CellSizeY = 60;
		CellModeX = "variable";
		CellModeY = "absolute";
		CellSpacingX = 4;
		CellSpacingY = 4;
		MaxColCount = 0;
		MaxRowCount = 0;
		OrderMode = "lrtb";
		IsExtentDynamic = true;
	};
	ThemeManager.setProfile(%this.grid, "emptyProfile");
	%this.scroller.add(%this.grid);

	%this.detailLabel = new GuiControl()
	{
		HorizSizing = "width";
		VertSizing = "top";
		Position = %pad SPC %detailY;
		Extent = (%width - (%pad * 2)) SPC 22;
		Text = "";
		align = "left";
		vAlign = "middle";
	};
	// labelProfile rather than infoProfile: this is a status line, and infoProfile
	// draws a border and fill, which reads as an empty text box while nothing is
	// selected.
	ThemeManager.setProfile(%this.detailLabel, "labelProfile");
	%content.add(%this.detailLabel);

	// "left"/"top" sizing pins these to the bottom-right corner. The names read
	// backwards: they name the edge whose slack absorbs the resize, not the edge
	// the control sticks to.
	%this.cancelButton = new GuiButtonCtrl()
	{
		HorizSizing = "left";
		VertSizing = "top";
		Position = (%width - 226) SPC (%height - 62);
		Extent = "100 30";
		Text = "Cancel";
		Command = %this.getID() @ ".onClose();";
	};
	ThemeManager.setProfile(%this.cancelButton, "buttonProfile");
	%content.add(%this.cancelButton);

	%this.chooseButton = new GuiButtonCtrl()
	{
		HorizSizing = "left";
		VertSizing = "top";
		Position = (%width - 116) SPC (%height - 64);
		Extent = "100 34";
		Text = "Choose";
		Command = %this.getID() @ ".onDone();";
	};
	ThemeManager.setProfile(%this.chooseButton, "primaryButtonProfile");
	%content.add(%this.chooseButton);
	%this.chooseButton.setActive(false);

	%this.load();
}

//-----------------------------------------------------------------------------
// Populating.
//-----------------------------------------------------------------------------

// One query, one button per asset, and that is the last the database hears from
// this dialog. findAssetType filters the query it is handed rather than
// searching afresh, which is what the trailing true means.
function EditorAssetPickerDialog::load(%this)
{
	%query = new AssetQuery();
	AssetDatabase.findAllAssets(%query);
	AssetDatabase.findAssetType(%query, %this.assetType, true);

	for(%i = 0; %i < %query.getCount(); %i++)
	{
		%assetId = %query.getAsset(%i);

		// Internal assets are the engine's own bookkeeping and belong to nobody
		// the user could sensibly point a profile at.
		if(!AssetDatabase.isAssetInternal(%assetId))
		{
			%this.addItem(%assetId);
		}
	}
	%query.delete();

	%this.totalCount = %this.grid.getCount();
	%this.applyFilter();

	// Open showing what the field already holds, so reopening the picker reads
	// as coming back to a choice rather than starting over.
	%current = %this.findItem(%this.currentAsset);
	if(isObject(%current))
	{
		%this.onItemClicked(%current);
	}
}

function EditorAssetPickerDialog::addItem(%this, %assetId)
{
	%item = new GuiButtonCtrl()
	{
		class = "EditorAssetPickerItem";
		assetId = %assetId;
		assetType = %this.assetType;
		owner = %this;
	};
	%this.grid.add(%item);

	return %item;
}

function EditorAssetPickerDialog::findItem(%this, %assetId)
{
	if(%assetId $= "")
	{
		return 0;
	}

	for(%i = 0; %i < %this.grid.getCount(); %i++)
	{
		%item = %this.grid.getObject(%i);
		if(%item.assetId $= %assetId)
		{
			return %item;
		}
	}
	return 0;
}

//-----------------------------------------------------------------------------
// Filtering.
//-----------------------------------------------------------------------------

function EditorAssetPickerDialog::onSearchChanged(%this)
{
	%this.applyFilter();
}

// Matching is a substring of the whole asset id, so "app" finds a module's
// worth of assets and "rock" finds them across modules. Deliberately not
// AssetDatabase.findAssetName: that one takes no query-as-source argument, so
// it always re-searches the entire database and would throw away the type
// filter this dialog was opened with.
function EditorAssetPickerDialog::applyFilter(%this)
{
	%needle = strlwr(trim(%this.searchBox.getText()));
	%shown = 0;

	for(%i = 0; %i < %this.grid.getCount(); %i++)
	{
		%item = %this.grid.getObject(%i);
		%match = (%needle $= "") || (strstr(%item.searchKey, %needle) != -1);
		%item.setVisible(%match);

		if(%match)
		{
			%shown++;
		}
	}

	%this.countLabel.setText(%shown SPC "of" SPC %this.totalCount);
	%this.reflowGrid();
}

// A grid re-lays out when a child is added, removed, moved or resized, and
// setVisible is none of those -- so without this the hidden cells leave their
// holes behind and the grid keeps its old height. Resizing it to the size it
// already has walks the children again, and the walk skips the invisible ones.
function EditorAssetPickerDialog::reflowGrid(%this)
{
	%position = %this.grid.getPosition();
	%extent = %this.grid.getExtent();
	%this.grid.resize(getWord(%position, 0), getWord(%position, 1),
		getWord(%extent, 0), getWord(%extent, 1));
}

//-----------------------------------------------------------------------------
// Selection.
//-----------------------------------------------------------------------------

function EditorAssetPickerDialog::onItemClicked(%this, %item)
{
	if(%this.chosenItem == %item)
	{
		return;
	}

	if(isObject(%this.chosenItem))
	{
		%this.chosenItem.setChosen(false);
	}
	%item.setChosen(true);

	%this.chosenItem = %item;
	%this.selectedAsset = %item.assetId;
	%this.chooseButton.setActive(true);
	%this.showDetail(%item.assetId);
}

// Only the selected asset is ever acquired, and the one before it is let go
// first, so the dialog holds a single reference however much clicking around
// happens. onRemove drops the last one.
function EditorAssetPickerDialog::showDetail(%this, %assetId)
{
	%this.releaseHeldAsset();

	%asset = AssetDatabase.acquireAsset(%assetId);
	%this.heldAsset = %assetId;

	%detail = %assetId;
	if(isObject(%asset) && %this.assetType $= "ImageAsset")
	{
		%frames = %asset.getFrameCount();
		%detail = %detail SPC "-" SPC %frames SPC (%frames == 1 ? "frame," : "frames,") SPC
			%asset.getImageWidth() SPC "x" SPC %asset.getImageHeight();
	}
	%this.detailLabel.setText(%detail);
}

function EditorAssetPickerDialog::releaseHeldAsset(%this)
{
	if(%this.heldAsset !$= "")
	{
		AssetDatabase.releaseAsset(%this.heldAsset);
		%this.heldAsset = "";
	}
}

//-----------------------------------------------------------------------------
// Leaving.
//-----------------------------------------------------------------------------

// Pop before calling back. The caller's handler commits a field, which can
// rebuild the pane the button that started all this lives in, and none of that
// should run while this dialog is still on the canvas. The dialog itself
// survives either way -- the delete is a hundred milliseconds out.
function EditorAssetPickerDialog::onDone(%this)
{
	if(%this.selectedAsset $= "")
	{
		return;
	}

	%target = %this.callbackTarget;
	%method = %this.callbackMethod;
	%asset = %this.selectedAsset;

	%this.onClose();

	if(isObject(%target))
	{
		%target.call(%method, %asset);
	}
}

// This dialog opens on top of another one, so it must not use the shared
// EditorCore.dialog delete slot - closing both within the scheduled delay would
// leak one of them. The parent object deletes it after a pause; scheduling
// "delete" on the dialog itself would fire inside its own script-callback guard
// and assert.
function EditorAssetPickerDialog::onClose(%this)
{
	Canvas.popDialog(%this);
	EditorCore.schedule(100, "deleteDialogObject", %this);
}

function EditorAssetPickerDialog::onRemove(%this)
{
	%this.releaseHeldAsset();
}
