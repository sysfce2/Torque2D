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

// One collapsible group of the Asset Library -- all the assets of one type.
// A GuiPanelCtrl whose header is the toggle, with a "New" button and a grid of
// AssetDictionaryButtons inside it.
//
// The tiles go in that inner grid and never directly on the panel:
// GuiExpandCtrl::toggleHiddenChildren force-writes mVisible on every DIRECT
// child whenever the panel expands, collapses or resizes, which would fight the
// search filter for control of exactly the same flag. Grandchildren are left
// alone.
//
// The group owns the arrangement; AssetLibraryWindow owns the decisions. It is
// told which view mode to draw, which field to sort on and what to filter by,
// because all three apply to the whole library at once.

$AssetDictionary::gridCell = 72;
$AssetDictionary::gridCellHeight = 96;
$AssetDictionary::rowHeight = 40;

// Where the grid starts: under the header and the New button.
$AssetDictionary::gridTop = 62;

function AssetDictionary::onAdd(%this)
{
	if(%this.viewMode $= "")
	{
		%this.viewMode = "grid";
	}
	if(%this.sortField $= "")
	{
		%this.sortField = "name";
	}
	if(%this.title $= "")
	{
		%this.title = %this.getText();
	}

	%this.newButton = new GuiButtonCtrl()
	{
		class = "NewAssetButton";
		Position = "5 27";
		Extent = "300 30";
		HorizSizing = "width";
		VertSizing = "height";
		Text = "New";
	};
	ThemeManager.setProfile(%this.newButton, "buttonProfile");
	%this.add(%this.newButton);

	// CellModeX variable makes CellSizeX a MINIMUM column width -- as many columns
	// as fit, with the remainder shared out -- which is the whole reflow, and it is
	// also what lets a single row stretch across the group in rows mode. CellModeY
	// has to stay absolute; variable would size a row to its tallest child, which
	// puts a tile-sized cell in a 40 pixel row.
	//
	// IsExtentDynamic is what lets the grid grow and shrink with its contents, so
	// the panel has something to measure and a fully filtered group collapses to
	// its header instead of leaving a hole.
	%this.grid = new GuiGridCtrl()
	{
		Position = "0" SPC $AssetDictionary::gridTop;
		Extent = "310 50";
		HorizSizing = "width";
		VertSizing = "height";
		CellSizeX = $AssetDictionary::gridCell;
		CellSizeY = $AssetDictionary::gridCellHeight;
		CellModeX = "variable";
		CellModeY = "absolute";
		CellSpacingX = 4;
		CellSpacingY = 4;
		MaxColCount = 0;
		MaxRowCount = 0;
		OrderMode = "LRTB";
		IsExtentDynamic = true;
	};
	ThemeManager.setProfile(%this.grid, "emptyProfile");
	%this.add(%this.grid);

	%this.setViewMode(%this.viewMode);
}

//-----------------------------------------------------------------------------
// Contents.
//-----------------------------------------------------------------------------

function AssetDictionary::load(%this)
{
	%query = new AssetQuery();
	AssetDatabase.findAllAssets(%query);
	AssetDatabase.findAssetType(%query, %this.Type, true);

	for(%i = 0; %i < %query.getCount(); %i++)
	{
		%assetID = %query.getAsset(%i);

		if(!AssetDatabase.isAssetInternal(%assetID))
		{
			%this.addButton(%assetID, true);
		}
	}
	%query.delete();

	// findAllAssets walks a hash table, so what it returns is in no particular
	// order and not even the same order twice. Sorting once here is what makes the
	// library's default order mean something.
	%this.applySort(%this.sortField);

	%this.newButton.text = "New" SPC %this.type;
	%this.newButton.type = %this.type;
}

// %deferPlacement is for load(), which sorts once at the end rather than paying
// for a sort per asset. Everything else -- the New Asset dialogs -- adds one
// asset to a library that is already open, and wants it to land where it belongs.
function AssetDictionary::addButton(%this, %assetID, %deferPlacement)
{
	// Authored at the cell size for the current mode so the tile's first pass at
	// arranging itself is close; the grid resizes it for real on add, and the
	// second setViewMode below is what actually settles it. Spelled from the
	// constants rather than read back off the grid, because CellSizeX and
	// CellSizeY are floats and "72.000000 96.000000" is not a Point2I.
	%cellHeight = (%this.viewMode $= "rows")
		? $AssetDictionary::rowHeight
		: $AssetDictionary::gridCellHeight;

	%button = new GuiButtonCtrl()
	{
		Class = "AssetDictionaryButton";
		HorizSizing = "center";
		VertSizing = "center";
		Extent = $AssetDictionary::gridCell SPC %cellHeight;
		Tooltip = AssetDatabase.getAssetName(%assetID);
		Text = "";
		AssetID = %assetID;
		Type = %this.Type;
		viewMode = %this.viewMode;
	};
	ThemeManager.setProfile(%button, "itemSelectProfile");
	ThemeManager.setProfile(%button, "tipProfile", "TooltipProfile");
	%this.grid.add(%button);

	// Again, now that the grid has resized it to a real cell: the pass inside
	// onAdd could only measure the extent the tile was authored with.
	%button.setViewMode(%this.viewMode);

	if(!%deferPlacement)
	{
		%this.applySort(%this.sortField);

		// The library owns the needle, and a new asset has to face it like every
		// other: adding one while a filter is up must not sneak an unmatched tile
		// onto the screen.
		if(isObject(%this.owner))
		{
			%this.owner.applyFilter();
		}
		else
		{
			%this.fixSize();
		}
	}

	return %button;
}

function AssetDictionary::removeButton(%this, %assetID)
{
	%button = %this.getButton(%assetID);
	if(isObject(%button))
	{
		%button.delete();

		if(isObject(%this.owner))
		{
			%this.owner.applyFilter();
		}
		else
		{
			%this.fixSize();
		}
		return true;
	}
	return false;
}

function AssetDictionary::getButton(%this, %assetID)
{
	for(%i = 0; %i < %this.grid.getCount(); %i++)
	{
		%button = %this.grid.getObject(%i);
		if(%button.AssetID $= %assetID)
		{
			return %button;
		}
	}
	return 0;
}

function AssetDictionary::getButtonCount(%this)
{
	return %this.grid.getCount();
}

function AssetDictionary::unload(%this)
{
	//Remove all the child gui controls
	for(%i = %this.grid.getCount() - 1; %i >= 0; %i--)
	{
		%obj = %this.grid.getObject(%i);
		%obj.delete();
	}
}

function AssetDictionary::reload(%this)
{
	%this.unload();
	%this.load();

	if(isObject(%this.owner))
	{
		%this.owner.applyFilter();
	}
}

//-----------------------------------------------------------------------------
// View mode.
//-----------------------------------------------------------------------------

// Both modes are one grid with different cell metrics, not two layouts. Nothing
// is rebuilt, hidden or swapped -- every level rewrites its own numbers and the
// same controls stay put, so the selection and the running animations survive a
// mode change.
function AssetDictionary::setViewMode(%this, %mode)
{
	%this.viewMode = %mode;
	%width = getWord(%this.getExtent(), 0);

	if(%mode $= "rows")
	{
		// One column, said outright. MaxColCount clamps the chain length, and
		// CellModeX variable then hands that single column the whole width, so a
		// row fills the group however wide the frame is dragged. Asking for a cell
		// wider than the pane would do it too, but only until the next resize.
		%this.grid.MaxColCount = 1;
		%this.grid.CellSizeY = $AssetDictionary::rowHeight;
	}
	else
	{
		%this.grid.MaxColCount = 0;
		%this.grid.CellSizeY = $AssetDictionary::gridCellHeight;
	}

	// The grid lays out on resize, so nudge it before the tiles read their own
	// extents -- otherwise each one measures the cell it had in the other mode.
	%this.grid.resize(0, $AssetDictionary::gridTop, %width, 4);

	for(%i = 0; %i < %this.grid.getCount(); %i++)
	{
		%this.grid.getObject(%i).setViewMode(%mode);
	}
}

//-----------------------------------------------------------------------------
// Search.
//-----------------------------------------------------------------------------

// Hide what does not match and report what is left. The needle arrives already
// lowercased and trimmed, and every tile's searchKey was lowercased once when it
// was built, because this walks every tile on every keystroke.
function AssetDictionary::applyFilter(%this, %needle)
{
	%shown = 0;

	for(%i = 0; %i < %this.grid.getCount(); %i++)
	{
		%button = %this.grid.getObject(%i);
		%match = (%needle $= "") || (strstr(%button.searchKey, %needle) != -1);
		%button.setVisible(%match);

		if(%match)
		{
			%shown++;
		}
	}

	// A group whose matches are all gone keeps its header and its New button, so
	// the shape of the library does not change under the person typing.
	%this.setText(%this.title SPC "(" @ %shown @ ")");

	%this.reflowGrid();

	return %shown;
}

// A grid re-lays out when a child is added, removed, moved or resized, and
// setVisible is none of those -- so without this the hidden cells leave their
// holes behind and the grid keeps its old height. Resizing it to the size it
// already has walks the children again, and the walk skips the invisible ones.
function AssetDictionary::reflowGrid(%this)
{
	%position = %this.grid.getPosition();
	%extent = %this.grid.getExtent();
	%this.grid.resize(getWord(%position, 0), getWord(%position, 1),
		getWord(%extent, 0), getWord(%extent, 1));
}

//-----------------------------------------------------------------------------
// Sort.
//-----------------------------------------------------------------------------

// Reorder the tiles that are already there rather than rebuilding them. A
// rebuild would release and re-acquire every asset, throw away and remake every
// sprite, restart every animation and drop the current selection -- all to
// change the order of a list.
function AssetDictionary::applySort(%this, %field)
{
	%this.sortField = %field;

	%count = %this.grid.getCount();
	if(%count < 2)
	{
		return;
	}

	for(%i = 0; %i < %count; %i++)
	{
		%button = %this.grid.getObject(%i);
		%item[%i] = %button;
		%major[%i] = (%field $= "category") ? %button.sortCategory : %button.sortName;
		%minor[%i] = %button.sortName;
	}

	// Insertion sort, the same shape as GuiProfileEditorLibrary::sortTabList. A
	// group holds tens of assets, sometimes low hundreds, and this runs when the
	// sort field changes or a group loads -- never per frame and never per
	// keystroke -- so the quadratic cost never shows.
	for(%i = 1; %i < %count; %i++)
	{
		%heldItem = %item[%i];
		%heldMajor = %major[%i];
		%heldMinor = %minor[%i];

		%j = %i - 1;
		while(%j >= 0 && %this.sortsAfter(%major[%j], %minor[%j], %heldMajor, %heldMinor))
		{
			%item[%j + 1] = %item[%j];
			%major[%j + 1] = %major[%j];
			%minor[%j + 1] = %minor[%j];
			%j--;
		}

		%item[%j + 1] = %heldItem;
		%major[%j + 1] = %heldMajor;
		%minor[%j + 1] = %heldMinor;
	}

	// SimSet::reOrder inserts its first argument IN FRONT OF its second, so
	// walking backwards and putting each tile ahead of the one that follows it
	// lands the whole list in order. The grid does not hear about it on its own.
	for(%i = %count - 2; %i >= 0; %i--)
	{
		%this.grid.reorderChild(%item[%i], %item[%i + 1]);
	}
	%this.grid.childrenReordered();
}

// True when A belongs after B. A category sort falls back to the name, so the
// contents of one category are still alphabetical -- and so the many assets that
// carry no category at all keep a stable order among themselves rather than
// whatever the hash table last handed over.
function AssetDictionary::sortsAfter(%this, %aMajor, %aMinor, %bMajor, %bMinor)
{
	%order = stricmp(%aMajor, %bMajor);
	if(%order != 0)
	{
		return %order > 0;
	}

	return stricmp(%aMinor, %bMinor) > 0;
}

//-----------------------------------------------------------------------------
// Layout.
//-----------------------------------------------------------------------------

// A GuiPanelCtrl caches the height it opens to, measured from the children it
// had at the moment it opened. Anything that changes the size of what is inside
// has to throw that cache away.
function AssetDictionary::fixSize(%this)
{
	if(%this.getExpanded())
	{
		%this.setExpanded(false);
		%this.setExpanded(true);
	}
}

// fixSize plus a width nudge: one parentResized through every child with the
// widths unchanged, which is what makes the grid re-measure its cells before the
// panel measures the grid.
function AssetDictionary::forceLayout(%this)
{
	%w = getWord(%this.getExtent(), 0);
	%h = getWord(%this.getExtent(), 1);
	%this.resize(0, 0, %w + 1, %h);
	%this.resize(0, 0, %w, %h);

	%this.fixSize();
}
