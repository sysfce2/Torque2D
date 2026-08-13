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

//-----------------------------------------------------------------------------
// The inspector for a font asset, in place of the generic one.
//
// A FontAsset is the thinnest asset in the library: it registers exactly one
// field of its own, the .fnt file, and everything else about it is inherited
// from AssetBase. So there is very little to arrange, and what makes this worth
// a pane at all is the line that is NOT a field -- what the .fnt turned out to
// contain, and whether it loaded.
//
// Three blocks, the shape AssetAnimationInspectorPane uses:
//
//   Identity     the name and the category
//   Font         the file, what came out of it, and when it lets go
//   Description  the prose the library is searched by
//
// The file row is in the Font block rather than in Identity, which is where the
// image and animation panes put theirs. The rule both of those follow is that
// the readout sits in the block holding the thing it answers, and here the
// readout answers the file: "128 px, 97 glyphs, 2 pages of 512 x 512" is a
// remark about the .fnt, not about the asset's name.
//
// A bitmap font is a .fnt descriptor plus one or more page images, and the pages
// are named INSIDE the .fnt rather than in the asset file. That is why a missing
// page is its own warning: nothing in the asset refers to it, so there is
// nowhere else the loss could show up.
//
// AssetName is shown but not editable, for the reason the other panes give:
// AssetBase::setAssetName does nothing once the asset manager owns the asset, so
// a box that accepted typing would silently do nothing. A real rename is
// AssetDatabase.renameDeclaredAsset.
//
// Absent, each for a checkable reason:
//   AssetInternal, AssetPrivate   they exist to keep an asset OUT of the editor
//   asset id, asset file          the module and the name are on show, and the
//                                 file is where the manager put it
//-----------------------------------------------------------------------------

$AssetFontInspectorPane::cellWidth = 300;
$AssetFontInspectorPane::cellCount = 3;
$AssetFontInspectorPane::descriptionHeight = 150;

function AssetFontInspectorPane::onAdd(%this)
{
	// onAdd does not chain, so the shared setup runs from here.
	%this.init();

	// What the file row's Find button offers. A "file" row was a bitmap
	// everywhere until this pane, and an image filter offers nothing a font
	// asset can use.
	%this.fileFilters = "Bitmap Font (*.fnt)|*.fnt|All Files (*.*)|*.*";
	%this.fileTitle = "Choose a Bitmap Font File";
}

//-----------------------------------------------------------------------------
// Construction.
//-----------------------------------------------------------------------------

function AssetFontInspectorPane::buildPane(%this)
{
	%grid = %this.makeCellGrid(0, $AssetFontInspectorPane::cellWidth,
		$AssetFontInspectorPane::cellCount);
	%this.add(%grid);
	%this.contentGrid = %grid;

	%this.buildIdentityCell(%grid);
	%this.buildFontCell(%grid);
	%this.buildDescriptionCell(%grid);

	%this.buildWarning();

	%this.nameRow.setEnabled(false, "Renaming an asset changes its id and every file that refers to it, " @
		"so it is not something the inspector can do safely on its own.");
}

// addFieldRow takes the label and the kind as arguments rather than asking the
// tables for them, so every call here would otherwise repeat the same lookups.
function AssetFontInspectorPane::addField(%this, %container, %field)
{
	return %this.addFieldRow(%container, %field, %this.labelFor(%field),
		%this.kindFor(%field), %this.enumItemsFor(%field));
}

function AssetFontInspectorPane::buildIdentityCell(%this, %grid)
{
	%chain = %this.makeCell(%grid, 4);
	%this.identityChain = %chain;

	%this.nameRow = %this.addField(%chain, "AssetName");
	%this.addField(%chain, "AssetCategory");
}

function AssetFontInspectorPane::buildFontCell(%this, %grid)
{
	%chain = %this.makeCell(%grid, 4);
	%this.fontChain = %chain;

	%this.fileRow = %this.addField(%chain, "FontFile");

	// Read-only, because none of it is a value the asset holds: it is what the
	// last parse of the .fnt produced. Wrapped and extending, or a sentence in a
	// block a third of the pane wide is simply not drawn.
	%this.infoLabel = %this.makeInfoLabel(%chain, "labelProfile");
	%this.infoLabel.textWrap = true;
	%this.infoLabel.textExtend = true;
	%this.infoLabel.vAlign = "top";

	%this.addField(%chain, "AssetAutoUnload");
}

function AssetFontInspectorPane::buildDescriptionCell(%this, %grid)
{
	%chain = %this.makeCell(%grid, 4);
	%this.descriptionChain = %chain;

	%this.addField(%chain, "AssetDescription");
}

// Below the grid rather than in a block. A warning is a sentence, and a sentence
// read across the whole pane is one or two lines where the same sentence in a
// third of it is five.
function AssetFontInspectorPane::buildWarning(%this)
{
	%this.warningLabel = %this.makeInfoLabel(%this, "overrideLabelProfile");
	%this.warningLabel.textWrap = true;
	%this.warningLabel.textExtend = true;
	%this.warningLabel.vAlign = "top";
	%this.warningLabel.setVisible(false);
}

//-----------------------------------------------------------------------------
// The field tables.
//-----------------------------------------------------------------------------

function AssetFontInspectorPane::labelFor(%this, %field)
{
	switch$(%field)
	{
		case "AssetName": return "Asset Name";
		case "AssetCategory": return "Category";
		case "AssetDescription": return "Description";
		case "AssetAutoUnload": return "Auto Unload";
		case "FontFile": return "Font File";
	}

	return %field;
}

function AssetFontInspectorPane::kindFor(%this, %field)
{
	switch$(%field)
	{
		case "FontFile": return "file";
		case "AssetAutoUnload": return "bool";
		case "AssetDescription": return "multiline";
	}

	return "text";
}

function AssetFontInspectorPane::tipFor(%this, %field)
{
	switch$(%field)
	{
		case "FontFile": return "The .fnt descriptor written by AngelCode BMFont, in its text format. " @
			"The page images are named inside it, and are loaded from beside it.";
		case "AssetAutoUnload": return "Let go of the font's textures when nothing is using it any more.";
	}

	return "";
}

function AssetFontInspectorPane::editorHeightFor(%this, %field)
{
	if(%field $= "AssetDescription")
	{
		return $AssetFontInspectorPane::descriptionHeight;
	}

	return 0;
}

// The stored path is absolute -- setFontFile expands whatever it is given against
// the asset's own folder -- and an absolute path is neither readable nor
// portable. Show the collapsed form, which is what the file on disk says.
function AssetFontInspectorPane::readField(%this, %field)
{
	if(%field $= "FontFile")
	{
		return %this.target.getRelativeFontFile();
	}

	return %this.target.getFieldValue(%field);
}

//-----------------------------------------------------------------------------
// Loading. Everything the row loop does not reach.
//-----------------------------------------------------------------------------

function AssetFontInspectorPane::refreshExtras(%this)
{
	%this.infoLabel.setText(%this.describeFont(%this.target));
	%this.showWarning(%this.warningFor(%this.target));
}

// What the .fnt held. Everything here is asked of the asset and none of it is
// stored on it, which is why it is a line of text and not a set of rows.
//
// The page size is the size of the texture that actually loaded, not the scaleW
// and scaleH the .fnt declares -- the same choice the image pane makes when it
// reports the picture it got rather than the one it asked for.
function AssetFontInspectorPane::describeFont(%this, %asset)
{
	%glyphs = %asset.getGlyphCount();

	if(%glyphs == 0)
	{
		return "No font loaded.";
	}

	%line = %asset.getFontSize() SPC "px";

	// Only when it says something the size did not. These are equal in every font
	// shipped with the engine, and a line that repeats itself reads as a mistake.
	if(%asset.getLineHeight() != %asset.getFontSize())
	{
		%line = %line @ "," SPC %asset.getLineHeight() SPC "line height";
	}

	%line = %line @ "," SPC %glyphs SPC ((%glyphs == 1) ? "glyph" : "glyphs");

	%pages = %asset.getPageCount();
	%line = %line @ "," SPC %pages SPC ((%pages == 1) ? "page" : "pages");

	// Nothing to say about the size of a page that did not load; the warning
	// below covers that case instead.
	%pageWidth = %asset.getPageWidth(0);
	if(%pageWidth > 0)
	{
		%line = %line SPC "of" SPC %pageWidth SPC "x" SPC %asset.getPageHeight(0);
	}

	return %line @ ".";
}

// In the order they matter. Only the first is shown, because the first is the
// one that has to be fixed before any of the others can be judged.
//
// Both of these are currently a line in the console log and nothing on screen.
function AssetFontInspectorPane::warningFor(%this, %asset)
{
	if(%asset.getGlyphCount() == 0)
	{
		return "This font did not load. Check that the file is where the path says, and that it is the " @
			"TEXT format AngelCode BMFont writes -- the binary and XML variants are not read.";
	}

	%declared = %asset.getPageCount();
	%loaded = %asset.getLoadedPageCount();

	if(%loaded < %declared)
	{
		return (%declared - %loaded) SPC "of this font's" SPC %declared SPC "page images did not load, so " @
			"some characters will be missing. The pages are named inside the .fnt file rather than here, " @
			"and are loaded from the folder beside it.";
	}

	return "";
}

// forceLayout only when the visibility actually changed: a chain skips hidden
// children when it lays out, and nothing re-lays it out on setVisible.
function AssetFontInspectorPane::showWarning(%this, %text)
{
	%wanted = (%text !$= "");
	%changed = (%wanted != %this.warningLabel.isVisible());

	%this.warningLabel.setText(%text);
	%this.warningLabel.setVisible(%wanted);

	if(%changed)
	{
		%this.forceLayout();
	}
}

// No afterCommit. The preview is a TextSprite wearing this font and it does have
// to be rebuilt when the file changes -- but that already happens: the commit
// ends in refreshAsset, and AssetAdmin::refreshPreview re-clicks the selected
// tile, which is what built the TextSprite in the first place. Doing it here as
// well would build it twice.
