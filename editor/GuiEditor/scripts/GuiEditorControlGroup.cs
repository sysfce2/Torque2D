//-----------------------------------------------------------------------------
// One collapsible section of the control palette -- "Basics", "Layout" and so
// on -- holding a grid of GuiEditorControlTiles.
//
// The same shape the Asset Manager uses for its asset dictionaries
// (AssetLibraryWindow::addDictionary, AssetDictionary.cs): a GuiPanelCtrl whose header
// is the toggle, with the real content in a grid inside it.
//
// The tiles go in that inner grid and never directly on the panel.
// GuiExpandCtrl::toggleHiddenChildren force-writes mVisible on every DIRECT
// child of a panel whenever it expands, collapses or resizes -- so a tile
// parented to the panel would have its visibility rewritten out from under
// whatever set it. Grandchildren are left alone.
//
// Both view modes are this one grid with different cell metrics rather than two
// layouts. GuiGridCtrl treats CellSizeX as the NARROWEST a column may be, not
// its width: it fits as many columns as the pane affords and shares the
// remainder evenly (see GuiEditorInspectorPane::makeCellGrid). So grid mode asks
// for 100 and gets two or three columns depending on how wide the frame is
// dragged, and rows mode asks for something wider than the pane, which can only
// ever be one column.
//
// The creator sets Text and owner inline, calls addTile() per entry, then
// setMode(). There is no onRemove: the tiles were added to the grid and the grid
// to this panel, so deleting the panel frees the lot.
//-----------------------------------------------------------------------------

// Width and height are separate numbers because they mean different things: the
// width is the NARROWEST a column may be and the reflow shares out the rest,
// while the height is exactly what a tile gets.
//
// A grid tile needs 108 of INNER height -- 56 of picture, a 44-pixel band for
// the name under it and 8 of air between and above. The 8 on top of that is the
// tile's own border: itemSelectProfile insets 3 pixels a side on the base theme
// and 4 on Torque Suit, and a cell sized to the interior would leave the fatter
// of the two clipping the bottom line of every name. The tile centers its
// picture in whatever room the inset actually leaves, so a theme that spends
// less than 4 gets slightly more air rather than a layout that drifts.
$GuiEditorControlGroup::gridCell = 100;
$GuiEditorControlGroup::gridCellHeight = 116;
$GuiEditorControlGroup::rowHeight = 40;
$GuiEditorControlGroup::headerHeight = 24;

function GuiEditorControlGroup::onAdd(%this)
{
	%this.tileCount = 0;

	%this.grid = new GuiGridCtrl()
	{
		HorizSizing = "width";
		Position = "0" SPC $GuiEditorControlGroup::headerHeight;
		Extent = "200 4";
		// Variable across, absolute down. Variable is what makes CellSizeX a
		// minimum -- as many columns as fit, remainder shared -- and that is the
		// whole reflow. Vertically it would instead size each row to its tallest
		// child, which in row mode means a 116-pixel tile in a 40-pixel row.
		CellModeX = "variable";
		CellModeY = "absolute";
		CellSizeX = $GuiEditorControlGroup::gridCell;
		CellSizeY = $GuiEditorControlGroup::gridCellHeight;
		CellSpacingX = 4;
		CellSpacingY = 4;
		MaxColCount = 0;
		MaxRowCount = 0;
		OrderMode = "lrtb";

		// Without this the grid keeps the height it was built at, and the panel
		// it lives in measures itself against that -- so a section would collapse
		// to a sliver or leave a hole under its last row.
		IsExtentDynamic = true;
	};
	ThemeManager.setProfile(%this.grid, "emptyProfile");
	%this.add(%this.grid);
}

function GuiEditorControlGroup::addTile(%this, %key)
{
	%tile = new GuiButtonCtrl()
	{
		class = "GuiEditorControlTile";
		HorizSizing = "width";
		VertSizing = "height";
		Position = "0 0";
		Extent = $GuiEditorControlGroup::gridCell SPC $GuiEditorControlGroup::gridCellHeight;
		Text = "";
		key = %key;
		owner = %this;
	};
	ThemeManager.setProfile(%tile, "itemSelectProfile");
	%this.grid.add(%tile);

	%this.tile[%this.tileCount] = %tile;
	%this.tileCount++;

	return %tile;
}

//-----------------------------------------------------------------------------
// Modes.
//-----------------------------------------------------------------------------

function GuiEditorControlGroup::setMode(%this, %mode)
{
	%this.mode = %mode;
	%width = getWord(%this.getExtent(), 0);

	if(%mode $= "rows")
	{
		// One column, by asking for a cell wider than there is room for.
		%this.grid.CellSizeX = %width;
		%this.grid.CellSizeY = $GuiEditorControlGroup::rowHeight;
	}
	else
	{
		%this.grid.CellSizeX = $GuiEditorControlGroup::gridCell;
		%this.grid.CellSizeY = $GuiEditorControlGroup::gridCellHeight;
	}

	// The grid lays out on resize, so nudge it before the tiles read their own
	// extents -- otherwise each one measures the cell it had in the other mode.
	%this.grid.resize(0, $GuiEditorControlGroup::headerHeight, %width, 4);

	for(%i = 0; %i < %this.tileCount; %i++)
	{
		%this.tile[%i].setMode(%mode);
	}
}

// A GuiPanelCtrl learns its own height only from parentResized -- its
// constructor defaults to 64x64 whatever Extent it was handed, and a chain
// positions its children without ever resizing them. Nudging the width by a
// pixel and back forces exactly one parentResized through the whole subtree and
// leaves the widths where they started. Same trick as
// GuiEditorInspectorPane::forceLayout.
function GuiEditorControlGroup::forceLayout(%this)
{
	%w = getWord(%this.getExtent(), 0);
	%h = getWord(%this.getExtent(), 1);
	%this.resize(0, 0, %w + 1, %h);
	%this.resize(0, 0, %w, %h);

	// A panel caches the height it opens to, measured from its children when it
	// was last opened. Switching modes changes every one of those, so the cache
	// has to be thrown away -- closing and reopening is what does it. Same move
	// as AssetDictionary::fixSize, and skipped while collapsed so a section the
	// user shut does not spring back open.
	if(%this.getExpanded())
	{
		%this.setExpanded(false);
		%this.setExpanded(true);
	}
}
