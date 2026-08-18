
//-----------------------------------------------------------------------------
// The shared half of an Asset Manager inspector pane: everything about laying
// fields out and moving values between them and an asset, and nothing about
// which fields an asset has.
//
// It exists because the stock C++ GuiInspector reflects every registered field
// into flat alphabetical groups, which for an ImageAsset means the eight cell
// values arrive split into "X Values" and "Y Values" -- so a cell's width never
// sits beside its height -- with nothing pinned open and nothing said about the
// image itself. Image, animation, font and audio assets have panes; particle and
// spine assets still use the inspector, which is why the pane's knowledge of a
// particular asset lives in the subclass rather than here.
//
// Layout is the arrangement GuiEditorInspectorPane and GuiProfileEditorProfileForm
// both use, and for the same reason: a vertical chain of blocks, each laying its
// fields out in a GuiGridCtrl, so widening the inspector frame reflows the cells
// into more columns instead of leaving dead space. The Asset Manager needs that
// more than either of them -- its inspector is the BOTTOM frame of the frame set
// (AssetAdmin::createFrameSet), so it opens about 700 wide and 360 tall and is
// dragged to whatever shape suits the work.
//
// The pane owns every write to the asset; its rows only marshal values.
//
// Subclassing. onAdd does not chain in TorqueScript (TORQUE_SCRIPT.md rule 8),
// so a subclass's onAdd calls init() first and the base drives from there:
//
//   init()            call from the subclass's onAdd
//   build()           call once after adding the pane to its scroller; it calls
//   buildPane()       which the subclass defines -- its whole layout
//   labelFor()        \  the subclass's field tables. The defaults are the field
//   kindFor()          } name, a text box and no tooltip, which is what an
//   enumItemsFor()     } unlisted field degrades to rather than vanishing.
//   tipFor()          /
//   refreshExtras()   anything the row loop does not reach
//   afterCommit()     anything the rest of the editor has to be told
//
// A subclass may also override readField/writeField, which is how a field whose
// stored form is not its editable form (an asset's loose file path) is handled
// without the row or the loop knowing.
//-----------------------------------------------------------------------------

function AssetInspectorPane::init(%this)
{
	ThemeManager.setProfile(%this, "emptyProfile");

	// The narrowest a cell column may get. The grids run in Variable mode, so
	// they fit as many columns as the pane can hold at this width and share the
	// remainder evenly -- which is what makes dragging the frame wider add
	// columns rather than whitespace.
	%this.rowWidth = 220;

	// Half of that, for the sections whose fields are read in pairs.
	%this.pairWidth = 152;

	%this.rowFields = "";
	%this.panelList = "";
	%this.target = "";
	%this.assetId = "";
}

//-----------------------------------------------------------------------------
// Construction.
//-----------------------------------------------------------------------------

function AssetInspectorPane::build(%this)
{
	%this.buildPane();
	%this.forceLayout();
	%this.setVisible(false);
}

// A GuiPanelCtrl learns its collapsed height only from parentResized -- its
// constructor defaults to 64x64 whatever Extent it was given, and a chain
// positions its children without ever resizing them. Nudging the width by a
// pixel and back forces exactly one parentResized through every child and
// leaves the widths where they started.
//
// Measured from the pane's CURRENT width rather than the width it was built at:
// the pane is HorizSizing "width" inside a filling scroller, so by the time
// anything calls this it is as wide as the frame, and nudging from the build
// width would snap it narrow until the next parent resize pushed it back.
function AssetInspectorPane::forceLayout(%this)
{
	%w = getWord(%this.getExtent(), 0);
	%h = getWord(%this.getExtent(), 1);
	%this.resize(0, 0, %w + 1, %h);
	%this.resize(0, 0, %w, %h);
}

// The grid configuration every block here uses. A hidden cell is skipped rather
// than left as a hole, so filtering closes the gap (GuiGridCtrl::resize).
//
// %cellW is the NARROWEST a column may be, not its width: the grid fits as many
// columns as the pane can hold at that size and shares the remainder evenly.
// Omit it for the ordinary one-field-per-row width.
//
// %maxCols caps the count, and a grid whose cells are blocks rather than single
// fields wants it. GuiGridCtrl works out its chain length from the width alone
// and never asks how many children it has (GetGridItemWidth), so a four-block
// grid on a wide screen computes six columns, fills four and leaves two empty --
// and the blocks come out narrower than the width they were given. Capping at
// the number of blocks makes the last step 4-across rather than 4-of-6.
function AssetInspectorPane::makeCellGrid(%this, %y, %cellW, %maxCols)
{
	%grid = new GuiGridCtrl()
	{
		HorizSizing = "width";
		Position = "0" SPC %y;
		Extent = %this.paneWidth SPC 4;
		CellModeX = "variable";
		CellModeY = "variable";
		CellSizeX = (%cellW $= "") ? %this.rowWidth : %cellW;
		CellSizeY = 48;
		CellSpacingX = 4;
		CellSpacingY = 4;
		MaxColCount = (%maxCols $= "") ? 0 : %maxCols;
		MaxRowCount = 0;
		OrderMode = "lrtb";
		IsExtentDynamic = true;
	};
	ThemeManager.setProfile(%grid, "emptyProfile");
	return %grid;
}

// One cell of such a grid: a vertical chain that measures itself from what is
// put in it, so the grid's row grows to the tallest block rather than to a
// number written here. Added to the grid before anything goes in it, because the
// grid sizes a cell as it arrives and everything inside lays out to that width.
function AssetInspectorPane::makeCell(%this, %grid, %spacing)
{
	%cell = %this.makeChain(0, (%spacing $= "") ? 2 : %spacing);
	%grid.add(%cell);
	return %cell;
}

// A plain vertical chain, for the places that stack full-width blocks rather
// than flowing cells.
function AssetInspectorPane::makeChain(%this, %y, %spacing)
{
	%chain = new GuiChainCtrl()
	{
		HorizSizing = "width";
		Position = "0" SPC %y;
		Extent = %this.paneWidth SPC 4;
		IsVertical = true;
		ChildSpacing = %spacing;
	};
	ThemeManager.setProfile(%chain, "emptyProfile");
	return %chain;
}

// A collapsible section. Its cells sit in an inner grid rather than directly on
// the panel: GuiExpandCtrl::toggleHiddenChildren force-writes mVisible on every
// direct child whenever it expands, collapses or resizes, which would undo any
// filtering. Grandchildren are left alone and the grid skips the hidden ones.
function AssetInspectorPane::makeSectionPanel(%this, %title)
{
	%headerH = 24;

	%panel = new GuiPanelCtrl()
	{
		HorizSizing = "width";
		Text = %title;
		Position = "0 0";
		Extent = %this.paneWidth SPC %headerH;
		MinExtent = "80" SPC %headerH;
	};
	ThemeManager.setProfile(%panel, "panelProfile");
	return %panel;
}

// A read-only line of text. Used for the things an asset can only be asked,
// never told -- its size, how many frames it cuts into, why it did not load.
//
// Sized to the container it is going into rather than to the pane, and added
// here rather than by the caller, because a GuiChainCtrl positions its children
// without resizing them: a label authored at the pane's width and dropped into a
// block a quarter that wide does not wrap, it hangs off the end and is clipped.
// The sizing flag takes it from there.
function AssetInspectorPane::makeInfoLabel(%this, %container, %profile)
{
	%label = new GuiControl()
	{
		HorizSizing = "width";
		Position = "0 0";
		Extent = getWord(%container.getExtent(), 0) SPC 20;
		MinExtent = "0 0";
		Text = "";
		align = "left";
		vAlign = "middle";
		UseInput = false;
	};
	ThemeManager.setProfile(%label, %profile);
	%container.add(%label);
	return %label;
}

//-----------------------------------------------------------------------------
// Rows.
//-----------------------------------------------------------------------------

// Build a row without claiming a name for it, for the rare widget that reads
// like a field but is not one -- it must stay out of the registry the refresh
// loop walks.
function AssetInspectorPane::makeFieldRow(%this, %container, %field, %label, %kind, %enumItems)
{
	%row = new GuiControl()
	{
		class = "EditorFieldRow";

		// A grid resizes every cell it lays out, which makes the flag moot there;
		// a chain does not, so a row in one follows the pane's width from here.
		HorizSizing = "width";
		Position = "0 0";
		Extent = getWord(%container.getExtent(), 0) SPC 48;
		fieldName = %field;
		labelText = %label;
		kind = %kind;
		enumItems = %enumItems;
		editorHeight = %this.editorHeightFor(%field);
		assetType = %this.assetTypeFor(%field);
		owner = %this;
	};
	%container.add(%row);
	%row.build();

	// The row's reset button means "back to the theme's stamped value", which has
	// no analogue for an asset: there is no layer under it to fall back to.
	%row.resetButton.setVisible(false);

	// After build(), because the widgets the tooltip goes on do not exist until
	// then. Empty for most fields, which is the same as not having one.
	%row.setTooltip(%this.tipFor(%field));

	return %row;
}

function AssetInspectorPane::addFieldRow(%this, %container, %field, %label, %kind, %enumItems)
{
	%row = %this.makeFieldRow(%container, %field, %label, %kind, %enumItems);

	%this.row[%field] = %row;
	%this.rowFields = (%this.rowFields $= "") ? %field : (%this.rowFields SPC %field);
	return %row;
}

// A collapsible section holding one row per named field, with the labels and
// kinds coming from the subclass's tables.
function AssetInspectorPane::buildSection(%this, %key, %title, %fields, %cellW)
{
	%panel = %this.makeSectionPanel(%title);
	%this.add(%panel);

	%grid = %this.makeCellGrid(24, %cellW);
	%panel.add(%grid);

	%this.panel[%key] = %panel;
	%this.panelFields[%key] = %fields;
	%this.panelList = (%this.panelList $= "") ? %key : (%this.panelList SPC %key);

	%count = getWordCount(%fields);
	for(%i = 0; %i < %count; %i++)
	{
		%field = getWord(%fields, %i);
		%this.addFieldRow(%grid, %field, %this.labelFor(%field),
			%this.kindFor(%field), %this.enumItemsFor(%field));
	}
	return %panel;
}

//-----------------------------------------------------------------------------
// Field presentation. The defaults answer for any field at all, so a subclass
// that forgets one gets a text box named after it rather than nothing.
//-----------------------------------------------------------------------------

function AssetInspectorPane::labelFor(%this, %field)
{
	return %field;
}

function AssetInspectorPane::kindFor(%this, %field)
{
	return "text";
}

function AssetInspectorPane::enumItemsFor(%this, %field)
{
	return "";
}

// What a field means, for the ones whose name does not say it.
//
// The engine would be the obvious place to get this from and is not one: a
// field's registered doc string is empty on all six of AudioAsset's and on most
// of everything else, so the stock inspector has nothing to show either. Left
// empty here, which is the same as having no tooltip; a pane answers for the
// handful of its own fields that need explaining.
function AssetInspectorPane::tipFor(%this, %field)
{
	return "";
}

// How deep a paragraph box should be. Zero takes the row's own three lines,
// which is right for a field sharing a block with others and wrong for one that
// has a whole cell of the grid to fill.
function AssetInspectorPane::editorHeightFor(%this, %field)
{
	return 0;
}

// What kind of asset an "asset" row offers to pick. Empty leaves the row on its
// own default of ImageAsset, which is what every asset row on every pane wanted
// until an emitter turned out to reference an animation as readily as an image.
function AssetInspectorPane::assetTypeFor(%this, %field)
{
	return "";
}

//-----------------------------------------------------------------------------
// Binding. There is no rebuild here at all: the pane is built once for the kind
// of asset it edits, and binding only ever loads values -- so a selection change
// can never free a control the engine is mid-dispatch on.
//-----------------------------------------------------------------------------

function AssetInspectorPane::bind(%this, %asset, %assetId)
{
	if(!isObject(%asset))
	{
		%this.unbind();
		return;
	}

	%this.target = %asset;
	%this.assetId = %assetId;

	// What a "file" row's Find button measures its answer against. An asset's
	// loose files are stored relative to the folder the asset itself lives in,
	// not to the game root -- see EditorFieldRow::pathBase.
	%this.findBase = AssetDatabase.getAssetPath(%assetId);

	%this.refresh();
	%this.forceLayout();
	%this.setVisible(true);
}

// Nothing selected. The rows keep their values and the whole pane stops drawing,
// so nothing stale is left on show.
function AssetInspectorPane::unbind(%this)
{
	%this.target = "";
	%this.assetId = "";
	%this.setVisible(false);
}

//-----------------------------------------------------------------------------
// Loading values. The populating guard keeps every setText / setColorF /
// setStateOn from echoing straight back through the commits.
//-----------------------------------------------------------------------------

function AssetInspectorPane::refresh(%this)
{
	if(!isObject(%this.target))
	{
		return;
	}

	%this.populating = true;

	%count = getWordCount(%this.rowFields);
	for(%i = 0; %i < %count; %i++)
	{
		%field = getWord(%this.rowFields, %i);
		%row = %this.row[%field];
		if(isObject(%row))
		{
			%row.setValue(%this.readField(%field));
		}
	}

	%this.refreshExtras();

	%this.populating = false;
}

// Whatever the row loop does not reach: read-only readouts, widgets that are not
// field rows, and any greying that depends on the asset's current state.
function AssetInspectorPane::refreshExtras(%this)
{
}

// An asset changed underneath us -- possibly this pane's own commit coming back
// round, because every asset setter ends in refreshAsset(), and possibly a
// change made on one of the other tabs.
//
// The committing guard is what stops the bounce. Nothing here rebuilds, so the
// re-entry is not dangerous, but reloading every row in the middle of a commit
// would overwrite the box the user is still in.
function AssetInspectorPane::onAssetRefreshed(%this, %asset)
{
	if(%this.committing || !isObject(%this.target) || %this.target != %asset)
	{
		return;
	}

	%this.refresh();
}

function AssetInspectorPane::readField(%this, %field)
{
	return %this.target.getFieldValue(%field);
}

function AssetInspectorPane::writeField(%this, %field, %value)
{
	%this.target.setFieldValue(%field, %value);
}

//-----------------------------------------------------------------------------
// Commits. Every write to the asset goes through here.
//
// Each of them also writes the asset's file: every setter on AssetBase and
// ImageAsset ends in refreshAsset(), which saves the .asset.taml then and there
// and cascades to anything depending on it. That is why a row commits on blur
// and on Enter and never per keystroke -- EditorFieldRow's AltCommand and
// ReturnCommand -- and why an unchanged row is left alone rather than written
// back over itself.
//-----------------------------------------------------------------------------

function AssetInspectorPane::onFieldRowCommit(%this, %row)
{
	if(%this.populating || !isObject(%this.target) || !%row.hasChanged())
	{
		return;
	}

	%this.commitValue(%row.fieldName, %row.getValue());
	%row.markClean();
}

// One value, written and announced. Kept apart from the row handler so the
// widgets that are not rows -- a cell table, a toggle -- reach the asset the
// same way, through the same guard.
function AssetInspectorPane::commitValue(%this, %field, %value)
{
	if(!isObject(%this.target))
	{
		return;
	}

	// Name the undo step after the field being changed, so the tooltip reads
	// "Undo Cell Width" rather than "Undo Edit". Only the paths that know what
	// they did can do this; a particle graph drag happens entirely in C++ and gets
	// the generic name.
	AssetAdmin.undoRecorder.setLabel(%field);

	// The change lands in refreshAsset, which comes straight back at
	// onAssetRefreshed. The values here are already what the asset holds, so the
	// bounce has nothing to say; what follows it does.
	%this.committing = true;
	%this.writeField(%field, %value);
	%this.committing = false;

	%this.refresh();
	%this.afterCommit();
}

function AssetInspectorPane::onFieldRowReset(%this, %row)
{
}

// Everything a write has to tell the rest of the editor.
function AssetInspectorPane::afterCommit(%this)
{
}

//-----------------------------------------------------------------------------
// Enabling. A field an asset is currently ignoring stays visible but inert, with
// a tooltip saying why -- blanking it would leave no way to see what it holds.
//-----------------------------------------------------------------------------

function AssetInspectorPane::setRowsEnabled(%this, %fields, %enabled, %reason)
{
	%count = getWordCount(%fields);
	for(%i = 0; %i < %count; %i++)
	{
		%row = %this.row[getWord(%fields, %i)];
		if(isObject(%row))
		{
			%row.setEnabled(%enabled, %reason);
		}
	}
}
