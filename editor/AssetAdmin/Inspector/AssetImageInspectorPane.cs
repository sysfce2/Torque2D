
//-----------------------------------------------------------------------------
// The Asset Manager's Inspector tab for an image asset, in place of the generic
// C++ GuiInspector. Everything about laying fields out and moving values between
// them and the asset is in AssetInspectorPane; this is only what an ImageAsset
// holds and how it should read.
//
// There are no collapsible sections. The Gui Editor has them because a control
// carries dozens of fields; an image asset carries eleven, and eleven fit. So
// everything is in the open, as four blocks of roughly equal size in one grid:
//
//   Identity     name, category, the file
//   Frames       the eight cell values as one table, the row order, and a line
//                saying what actually loaded and how it cut up
//   Settings     filter, color depth, and when it unloads
//   Description  the prose the library is searched by
//
// Four blocks and not four columns: the grid decides how many columns it can
// afford and the blocks flow into them, so the same pane is 1x4 in a tall narrow
// frame, 2x2 at the size the inspector opens at, and 4x1 across the foot of a
// wide screen. That last one is the case that made this a grid -- laid out as a
// stack, a 1600-pixel inspector was a column of fields down the left and a
// description box a yard wide, with most of the panel empty.
//
// AssetName is shown but not editable. AssetBase::setAssetName does nothing once
// the asset manager owns the asset (assetBase.h), so the stock inspector has
// always offered a box that silently did not work. A real rename is
// AssetDatabase.renameDeclaredAsset, which changes the asset id and rewrites
// every file referring to it -- its own piece of work, not a side effect of
// typing in a box.
//
// Five of the asset's values are deliberately absent. AssetInternal and
// AssetPrivate exist to keep an asset OUT of the editor, so an editor is the one
// place they are no use. The asset id is the module and the name, both of which
// are on show; the asset file is where the manager put it, and wanting a
// different one means wanting a different asset.
//
// BlendColor is the fifth, and it is absent because it is not really a property
// of the image: it tints the BASE LAYER that an asset's layers are composed
// onto, does nothing at all when there are no layers, and is edited in the row
// it belongs to on the Image Layers tab -- where the layer it tints is on screen
// beside it. Offered here it was a color picker that did nothing on nearly every
// asset in the library.
//
// ExplicitMode belongs to another tab and is only read here, to grey the cell
// table: the frames then come from the Explicit Frames tab. It reaches the pane
// the way any other change does -- the setter calls refreshAsset, which fires
// AssetBase::onRefresh, which tells the inspector.
//-----------------------------------------------------------------------------

// The narrowest a block may get, and how many of them there are.
//
// 300 is chosen against the three widths that matter. The identity block has to
// hold a path and a Find button, which sets the floor; and with 4-pixel spacing
// the grid takes 1 column below 608, 2 up to 912, and 4 once it has 1216 -- so a
// tall narrow frame stacks, the size the inspector opens at is 2x2, and the foot
// of a wide screen is a single row of four.
$AssetImageInspectorPane::cellWidth = 300;
$AssetImageInspectorPane::cellCount = 4;

// Deep enough that an empty description box reads as "the prose goes here" and
// stands about as tall as the blocks beside it.
$AssetImageInspectorPane::descriptionHeight = 150;

function AssetImageInspectorPane::onAdd(%this)
{
	// onAdd does not chain, so the shared setup runs from here.
	%this.init();
}

//-----------------------------------------------------------------------------
// Construction. Four blocks in one grid, then the warning beneath it.
//-----------------------------------------------------------------------------

function AssetImageInspectorPane::buildPane(%this)
{
	%grid = %this.makeCellGrid(0, $AssetImageInspectorPane::cellWidth,
		$AssetImageInspectorPane::cellCount);
	%this.add(%grid);
	%this.contentGrid = %grid;

	%this.buildIdentityCell(%grid);
	%this.buildFramesCell(%grid);
	%this.buildSettingsCell(%grid);
	%this.buildDescriptionCell(%grid);

	%this.buildWarning();

	// AssetName is the only row that is there to be read rather than changed, and
	// nothing about a selection can make it editable, so it is said once.
	%this.nameRow.setEnabled(false,
		"Renaming an asset changes its id and every file that refers to it, so it is not done from here yet.");
}

// What the asset is: its name, the category the library groups it under, and the
// picture itself.
function AssetImageInspectorPane::buildIdentityCell(%this, %grid)
{
	%cell = %this.makeCell(%grid);
	%this.identityChain = %cell;

	%this.nameRow = %this.addFieldRow(%cell, "AssetName", "Asset Name", "text", "");
	%this.categoryRow = %this.addFieldRow(%cell, "AssetCategory", "Category", "text", "");
	%this.fileRow = %this.addFieldRow(%cell, "ImageFile", "Image File", "file", "");
}

// How it is cut into frames, and what that produced. The readout is in this
// block rather than across the pane because it is an answer to the table above
// it -- "512 x 512 pixels, 64 frames" is only interesting beside the numbers
// that decided it.
function AssetImageInspectorPane::buildFramesCell(%this, %grid)
{
	%cell = %this.makeCell(%grid);
	%this.framesChain = %cell;

	// One widget rather than eight rows -- see AssetImageCellGrid for why.
	%this.cellGrid = new GuiControl()
	{
		class = "AssetImageCellGrid";
		Position = "0 0";
		Extent = getWord(%cell.getExtent(), 0) SPC 160;
		owner = %this;
	};
	%cell.add(%this.cellGrid);
	%this.cellGrid.build();

	// Read-only, because none of it is a value the asset holds: it is the size of
	// the picture that loaded and the number of frames the cell values actually
	// produced. Wrapped, because a block is a quarter of the pane at its widest.
	%this.infoLabel = %this.makeInfoLabel(%cell, "labelProfile");
	%this.infoLabel.textWrap = true;
	%this.infoLabel.textExtend = true;
	%this.infoLabel.vAlign = "top";
}

// How it is drawn, and when it lets go of its texture.
function AssetImageInspectorPane::buildSettingsCell(%this, %grid)
{
	%cell = %this.makeCell(%grid);
	%this.settingsChain = %cell;

	%this.addFieldRow(%cell, "FilterMode", %this.labelFor("FilterMode"),
		"enum", %this.enumItemsFor("FilterMode"));
	%this.addFieldRow(%cell, "Force16bit", %this.labelFor("Force16bit"), "bool", "");
	%this.addFieldRow(%cell, "AssetAutoUnload", %this.labelFor("AssetAutoUnload"), "bool", "");
}

// What it is for. One field, and it fills the block: the library searches by
// this, so it is worth writing more than a few words in.
function AssetImageInspectorPane::buildDescriptionCell(%this, %grid)
{
	%cell = %this.makeCell(%grid);
	%this.descriptionChain = %cell;

	%this.descriptionRow = %this.addFieldRow(%cell, "AssetDescription",
		%this.labelFor("AssetDescription"), "multiline", "");
}

// Below the grid rather than in a block, and the only thing that is. A warning
// is a sentence, and a sentence read across the whole pane is one or two lines
// where the same sentence in a quarter of it is six.
//
// Hidden while there is nothing to say, so it costs no height -- a chain skips
// its hidden children. textWrap with textExtend so it takes the lines it needs;
// rendering is where that is measured, being the only place a font can be asked
// how wide a word is.
function AssetImageInspectorPane::buildWarning(%this)
{
	%this.warningLabel = %this.makeInfoLabel(%this, "overrideLabelProfile");
	%this.warningLabel.textWrap = true;
	%this.warningLabel.textExtend = true;
	%this.warningLabel.vAlign = "top";
	%this.warningLabel.setVisible(false);
}

//-----------------------------------------------------------------------------
// Field presentation.
//-----------------------------------------------------------------------------

function AssetImageInspectorPane::labelFor(%this, %field)
{
	switch$(%field)
	{
		case "AssetName": return "Asset Name";
		case "AssetCategory": return "Category";
		case "AssetDescription": return "Description";
		case "AssetAutoUnload": return "Auto Unload";
		case "ImageFile": return "Image File";
		case "FilterMode": return "Filter";
		case "Force16bit": return "16 Bit Color";
	}
	return %field;
}

function AssetImageInspectorPane::kindFor(%this, %field)
{
	switch$(%field)
	{
		case "AssetAutoUnload" or "Force16bit": return "bool";
		case "AssetDescription": return "multiline";
		case "ImageFile": return "file";
		case "FilterMode": return "enum";
	}
	return "text";
}

function AssetImageInspectorPane::enumItemsFor(%this, %field)
{
	// The engine's own labels are NEAREST, BILINEAR and DEFAULT (textureFilterLookup
	// in ImageAsset.cc). Both the lookup on the way in and the drop-down's own
	// search ignore case, so these can read as words.
	if(%field $= "FilterMode")
	{
		return "Default" TAB "Nearest" TAB "Bilinear";
	}
	return "";
}

// The description has a block to itself, so it is not three lines deep.
function AssetImageInspectorPane::editorHeightFor(%this, %field)
{
	return (%field $= "AssetDescription")
		? $AssetImageInspectorPane::descriptionHeight : 0;
}

// The stored path is absolute -- setImageFile expands whatever it is given
// against the asset's own folder -- and an absolute path is neither readable nor
// portable. Show the collapsed form, which is what the file on disk says.
function AssetImageInspectorPane::readField(%this, %field)
{
	if(%field $= "ImageFile")
	{
		return %this.target.getRelativeImageFile();
	}
	return %this.target.getFieldValue(%field);
}

//-----------------------------------------------------------------------------
// Loading. Everything the row loop does not reach.
//-----------------------------------------------------------------------------

function AssetImageInspectorPane::refreshExtras(%this)
{
	%asset = %this.target;

	%this.cellGrid.load(%asset);
	%this.cellGrid.setEnabled(!%asset.getExplicitMode(),
		"Explicit frame mode is on, so the frames come from the Explicit Frames tab and these values are not used.");

	%this.infoLabel.setText(%this.describeImage(%asset));
	%this.showWarning(%this.warningFor(%asset));
}

// What loaded and what it cut into. Everything here is asked of the asset, never
// stored on it, which is why it is a line of text and not a row.
function AssetImageInspectorPane::describeImage(%this, %asset)
{
	%w = %asset.getImageWidth();
	%h = %asset.getImageHeight();

	if(%w <= 0 || %h <= 0)
	{
		return "No image loaded.";
	}

	// No "pixels" after the size. It is the one word here that says nothing a
	// reader of an image asset did not already know, and without it the whole
	// line fits on one line in a block a quarter of the pane wide.
	%frames = %asset.getFrameCount();
	%text = %w @ " x " @ %h @ ", " @ %frames SPC ((%frames == 1) ? "frame" : "frames");

	// Worth saying either way. A texture whose sides are not powers of two is
	// legal here but is the first thing to look at when one will not load on a
	// phone or in a browser.
	return %text @ ", " @ (%asset.getIsImagePOT() ? "power of two" : "not a power of two");
}

// The three ways an image asset ends up looking wrong, in the order they matter.
// Each of them is currently a line in the console log and nothing on screen.
function AssetImageInspectorPane::warningFor(%this, %asset)
{
	if(%asset.getImageWidth() <= 0 || %asset.getImageHeight() <= 0)
	{
		// The size cap is the likeliest cause and the least discoverable one:
		// TextureManager refuses a bitmap over 2048 on either side and says so
		// only in the log, leaving a sprite that draws nothing at all.
		return "This image did not load. Check that the file is where the path says, and that neither side is over 2048 pixels -- the engine refuses anything larger.";
	}

	if(%asset.getExplicitMode())
	{
		return "Explicit frame mode is on. The frames are the ones listed on the Explicit Frames tab, and the cell values above are not being used.";
	}

	// The cut did not survive calculateImage, which warns to the console and
	// falls back to treating the whole image as one frame.
	%cx = %asset.getCellCountX();
	%cy = %asset.getCellCountY();
	if(%cx > 0 && %cy > 0 && %asset.getFrameCount() != (%cx * %cy))
	{
		return "These cell values do not fit the image, so it is being used as a single frame. Check the sizes and offsets against the image size above.";
	}

	return "";
}

// A chain skips hidden children when it lays out, but nothing re-lays it out on
// setVisible -- so the pane has to ask, and only when the answer changed.
function AssetImageInspectorPane::showWarning(%this, %text)
{
	%show = (%text !$= "");
	%this.warningLabel.setText(%text);

	if(%this.warningLabel.isVisible() == %show)
	{
		return;
	}

	%this.warningLabel.setVisible(%show);
	%this.forceLayout();
}

//-----------------------------------------------------------------------------
// Commits.
//-----------------------------------------------------------------------------

// One of the cell table's nine values. It goes through commitValue like a row's
// would, so the pane stays the only thing that writes to the asset.
function AssetImageInspectorPane::onCellGridCommit(%this, %field, %value)
{
	%this.commitValue(%field, %value);
}
