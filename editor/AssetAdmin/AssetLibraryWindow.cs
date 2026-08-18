//AssetLibraryWindow.cs
//
// The Asset Library: a fixed toolbar over a scroller of collapsible asset
// groups.
//
//   toolbar        view mode, sort field, and the search box
//   scroller       everything else, so the toolbar never scrolls away
//   dictionaryList a chain of AssetDictionary panels, one per asset type
//
// The window owns all three. It also owns the three pieces of state the groups
// share -- view mode, sort field and the search needle -- because all of them
// apply to every group at once: a person looking for "rock" wants the rock
// image AND the rock sound, and switching to rows is a statement about the
// library, not about one type of asset.
//
// Groups are reached from elsewhere through AssetAdmin.Dictionary[%type], which
// is what the New/Delete dialogs have always used; addDictionary keeps writing
// it.

$AssetLibraryWindow::toolbarHeight = 58;
$AssetLibraryWindow::pad = 4;
$AssetLibraryWindow::rowHeight = 24;
$AssetLibraryWindow::searchY = 30;
$AssetLibraryWindow::iconSize = 16;
$AssetLibraryWindow::countWidth = 88;

function AssetLibraryWindow::onAdd(%this)
{
	// The view and the sort are remembered between runs; the search box is not.
	// A filter is about the thing you are doing right now, and reopening the
	// editor to a library that is mysteriously missing most of its assets is a
	// bug report waiting to happen.
	%this.viewMode = EditorPreferences.get("assetLibraryViewMode", "grid");
	%this.sortField = EditorPreferences.get("assetLibrarySortField", "name");
	%this.dictionaryCount = 0;

	%this.buildToolbar();

	// Built filling the whole content rect, then moved down under the toolbar by
	// fitScroller. Fill is the only way to find out how big that rect is: it is
	// the window's extent less the title bar and whatever borders the profile
	// asks for, and script cannot ask for any of those numbers.
	%this.scroller = new GuiScrollCtrl()
	{
		HorizSizing = "fill";
		VertSizing = "fill";
		Position = "0 0";
		Extent = "324 356";
		MinExtent = "0 0";
		hScrollBar = "alwaysOff";
		vScrollBar = "alwaysOn";
		constantThumbHeight = "0";
		showArrowButtons = "1";
		scrollBarThickness = "14";
	};
	ThemeManager.setProfile(%this.scroller, "scrollingPanelProfile");
	ThemeManager.setProfile(%this.scroller, "scrollingPanelThumbProfile", "ThumbProfile");
	ThemeManager.setProfile(%this.scroller, "scrollingPanelTrackProfile", "TrackProfile");
	ThemeManager.setProfile(%this.scroller, "scrollingPanelArrowProfile", "ArrowProfile");
	%this.add(%this.scroller);
	%this.fitScroller();

	// Fill across, and nothing about widths below. The horizontal bar is off, so
	// across is an axis that does not scroll and the engine knows exactly how much
	// room there is. Down is left alone; that axis scrolls, so the chain is as tall
	// as its groups and GuiScrollCtrl refuses fill there.
	%this.dictionaryList = new GuiChainCtrl()
	{
		HorizSizing = "fill";
		Position = "0 0";
		Extent = "310 4";
		MinExtent = "0 0";
		IsVertical = true;
		ChildSpacing = 2;
	};
	ThemeManager.setProfile(%this.dictionaryList, "emptyProfile");
	%this.scroller.add(%this.dictionaryList);

	%this.populate();
}

function AssetLibraryWindow::onRemove(%this)
{
	%this.stopListening(ThemeManager);

	// The toolbar and the scroller are this window's two children; the groups and
	// their tiles hang off the scroller and go with it, and the toolbar's own
	// controls go with the toolbar.
	if(isObject(%this.toolbar))
	{
		%this.toolbar.delete();
	}
	if(isObject(%this.scroller))
	{
		%this.scroller.delete();
	}
}

//-----------------------------------------------------------------------------
// The toolbar.
//-----------------------------------------------------------------------------

function AssetLibraryWindow::buildToolbar(%this)
{
	%this.toolbar = new GuiControl()
	{
		HorizSizing = "width";
		VertSizing = "bottom";
		Position = "0 0";
		Extent = "310" SPC $AssetLibraryWindow::toolbarHeight;
	};
	ThemeManager.setProfile(%this.toolbar, "emptyProfile");
	%this.add(%this.toolbar);

	// Two segmented rows rather than one: they answer different questions, and a
	// four-button strip would read as one choice of four.
	//
	// labelWidth is 4, not 0 -- EditorChoiceRow::build sizes its caption at
	// labelWidth - 4, so a zero asks for a control four pixels wide in the wrong
	// direction and silently eats the whole row.
	%this.viewRow = new GuiControl()
	{
		class = "EditorChoiceRow";
		HorizSizing = "right";
		Position = $AssetLibraryWindow::pad SPC 2;
		labelText = "";
		labelWidth = 4;
		owner = %this;
		fieldName = "viewMode";
	};
	%this.toolbar.add(%this.viewRow);
	%this.viewRow.addChoice("grid", $EditorIcon::grid_2x2, "Show assets as a grid of thumbnails");
	%this.viewRow.addChoice("rows", $EditorIcon::list_bullets, "Show assets as a compact list");
	%this.viewRow.build();
	%this.viewRow.setValue(%this.viewMode);

	%this.sortRow = new GuiControl()
	{
		class = "EditorChoiceRow";
		HorizSizing = "left";
		Position = "0 2";
		labelText = "";
		labelWidth = 4;
		owner = %this;
		fieldName = "sortField";
	};
	%this.toolbar.add(%this.sortRow);
	// font_size is the sheet's "Aa" pair, which is what every other tool uses for
	// "alphabetical" -- there is no A-Z glyph here. text_letter_t was tried first
	// and is a serif T whose crossbar and stem read as two bars at 16 pixels once
	// the theme tints it.
	%this.sortRow.addChoice("name", $EditorIcon::font_size, "Sort by asset name");
	%this.sortRow.addChoice("category", $EditorIcon::tag, "Sort by asset category, then by name");
	%this.sortRow.build();
	%this.sortRow.setValue(%this.sortField);

	// A funnel rather than the word "Search": the caption was clipped in every
	// theme, and widening it would have come straight out of the box it labels --
	// this pane is 324 wide and the sort row already owns the other end. The
	// funnel also says the truer thing, since the box narrows the library in
	// place rather than jumping to a hit.
	// Sizing left at the default, which pins the top-left corner. NOT "center":
	// that recentres the control in its PARENT on every resize, and the parent
	// here is the whole 58-pixel toolbar -- so the icon jumped up to the middle of
	// the bar, level with the view toggle, instead of staying level with the box.
	%this.searchIcon = new GuiSpriteCtrl()
	{
		Extent = $AssetLibraryWindow::iconSize SPC $AssetLibraryWindow::iconSize;
		MinExtent = $AssetLibraryWindow::iconSize SPC $AssetLibraryWindow::iconSize;
		Position = $AssetLibraryWindow::pad SPC ($AssetLibraryWindow::searchY
			+ (($AssetLibraryWindow::rowHeight - $AssetLibraryWindow::iconSize) / 2));
		Image = "EditorCore:EditorIcons16";
		ImageSize = "16 16";
		constrainProportions = "1";
		fullSize = "0";
		Frame = $EditorIcon::filter;
		UseInput = false;
	};
	ThemeManager.setProfile(%this.searchIcon, "spriteProfile");
	%this.toolbar.add(%this.searchIcon);

	// The sheets are greyscale, drawn to be modulated -- an untinted icon blends
	// with opaque white, which happens to look right on the theme the editor opens
	// in and is a white smear on the light ones. And the tint has to be re-read on
	// a theme change: ThemeManager swaps the profile object, which carries
	// backgrounds and text for free, but a color COPIED onto a sprite stays behind.
	%this.startListening(ThemeManager);
	%this.refreshSearchIcon();

	// Command fires on every keystroke, which is what makes the library narrow as
	// you type; AltCommand would only fire when the box lost focus.
	%this.searchBox = new GuiTextEditCtrl()
	{
		HorizSizing = "width";
		Position = "0" SPC $AssetLibraryWindow::searchY;
		Extent = "180" SPC $AssetLibraryWindow::rowHeight;
		align = "left";
		Tooltip = "Filter every group by asset name, description or category";
	};
	ThemeManager.setProfile(%this.searchBox, "textEditProfile");
	ThemeManager.setProfile(%this.searchBox, "tipProfile", "TooltipProfile");
	%this.searchBox.Command = %this.getID() @ ".onSearchChanged();";
	%this.searchBox.EscapeCommand = %this.getID() @ ".clearSearch();";
	%this.toolbar.add(%this.searchBox);

	// labelProfile rather than infoProfile: this is a status line, and infoProfile
	// draws a border and a fill, which reads as a second empty text box.
	%this.countLabel = new GuiControl()
	{
		HorizSizing = "left";
		Position = "0" SPC $AssetLibraryWindow::searchY;
		Extent = $AssetLibraryWindow::countWidth SPC $AssetLibraryWindow::rowHeight;
		Text = "";
		align = "right";
		vAlign = "middle";
	};
	ThemeManager.setProfile(%this.countLabel, "labelProfile");
	%this.toolbar.add(%this.countLabel);
}

// The colour the caption this replaced would have been drawn in, so the icon
// reads as part of the same row rather than as a picture sitting next to it.
function AssetLibraryWindow::refreshSearchIcon(%this)
{
	%this.searchIcon.setImageColor(ThemeManager.activeTheme.labelProfile.fontColor);
}

function AssetLibraryWindow::onThemeChange(%this, %theme)
{
	%this.refreshSearchIcon();
}

// The three controls that hang off the right edge cannot be placed until the
// content rect has been measured, so this is called from fitScroller with the
// width it found. Their sizing flags hold them there through every later resize.
function AssetLibraryWindow::layoutToolbar(%this, %width)
{
	%pad = $AssetLibraryWindow::pad;
	%count = $AssetLibraryWindow::countWidth;
	%y = $AssetLibraryWindow::searchY;

	%this.toolbar.resize(0, 0, %width, $AssetLibraryWindow::toolbarHeight);

	%sortWidth = getWord(%this.sortRow.getExtent(), 0);
	%this.sortRow.setPosition(%width - %pad - %sortWidth, 2);

	%boxLeft = %pad + $AssetLibraryWindow::iconSize + 6;
	%boxWidth = %width - %boxLeft - %pad - %count - 6;
	%this.searchBox.resize(%boxLeft, %y, %boxWidth, $AssetLibraryWindow::rowHeight);

	%this.countLabel.setPosition(%width - %pad - %count, %y);
}

// Put the scroller under the toolbar without ever naming a border thickness.
//
// A fixed bar above a stretching pane has no sizing flag of its own: "fill"
// takes the whole inner rect and throws the position away, so the bar ends up
// underneath it, and "height" keeps whatever gaps the authored extent happened
// to start with -- which means guessing the title height and the border sizes.
//
// So let the engine measure instead: the scroller is built filling, one resize
// pass makes that real, and what it reports back IS the content rect. Take the
// bar off the top of it and switch to "height", which from then on holds the top
// edge where it was put and lets the bottom follow the window.
//
// Callable more than once, which the palette's version is not. The measurement
// this makes is only as good as the profile the window is wearing at the time,
// and AssetAdmin::buildLibrary applies the window profiles AFTER the new{} block
// returns -- so the pass inside onAdd measures GuiDefaultProfile's title bar and
// borders. Going back to fill before measuring makes a second pass, once the
// real profiles are on and the frame set has sized the window, give the right
// answer instead of subtracting the bar height twice.
function AssetLibraryWindow::fitScroller(%this)
{
	%x = getWord(%this.getPosition(), 0);
	%y = getWord(%this.getPosition(), 1);
	%w = getWord(%this.getExtent(), 0);
	%h = getWord(%this.getExtent(), 1);

	%this.scroller.HorizSizing = "fill";
	%this.scroller.VertSizing = "fill";

	// Nudge the width by a pixel and back: one parentResized through every child,
	// widths unchanged. The position is carried through rather than zeroed, so
	// this does not move the window it is measuring.
	%this.resize(%x, %y, %w + 1, %h);
	%this.resize(%x, %y, %w, %h);

	%inner = %this.scroller.getExtent();
	%bar = $AssetLibraryWindow::toolbarHeight;

	%this.layoutToolbar(getWord(%inner, 0));

	%this.scroller.HorizSizing = "width";
	%this.scroller.VertSizing = "height";
	%this.scroller.resize(0, %bar, getWord(%inner, 0), getWord(%inner, 1) - %bar);
}

//-----------------------------------------------------------------------------
// The groups.
//-----------------------------------------------------------------------------

function AssetLibraryWindow::populate(%this)
{
	%this.addDictionary("Images", "ImageAsset");
	%this.addDictionary("Animations", "AnimationAsset");
	%this.addDictionary("Particle Effects", "ParticleAsset");
	%this.addDictionary("Fonts", "FontAsset");
	%this.addDictionary("Audio", "AudioAsset");
	//%this.addDictionary("Spines", "SpineAsset");
}

// Groups size with "width" rather than "fill": GuiExpandCtrl::parentResized
// writes mExpandedExtent straight into mBounds.extent, bypassing resize(), which
// is the only thing that honours fill.
function AssetLibraryWindow::addDictionary(%this, %title, %type)
{
	%dictionary = new GuiPanelCtrl()
	{
		Class = "AssetDictionary";
		Text = %title;
		command = "";
		HorizSizing = "width";
		VertSizing = "bottom";
		Position = "0 0";
		Extent = "306 22";
		MinExtent = "80 22";
		Type = %type;
		title = %title;
		owner = %this;
		viewMode = %this.viewMode;
		sortField = %this.sortField;
	};
	%dictionary.setExpandEase("EaseInOut", 1000);
	ThemeManager.setProfile(%dictionary, "panelProfile");
	%this.dictionaryList.add(%dictionary);

	%this.dictionary[%this.dictionaryCount] = %dictionary;
	%this.dictionaryCount++;

	// How the New and Delete dialogs have always found a group.
	AssetAdmin.Dictionary[%type] = %dictionary;

	return %dictionary;
}

function AssetLibraryWindow::loadAssets(%this)
{
	for(%i = 0; %i < %this.dictionaryCount; %i++)
	{
		%this.dictionary[%i].load();
	}

	%this.applyFilter();
}

// An asset's name, description or category was edited. The tile that shows it
// cached all three when it was built, so it has to re-read them -- and then the
// order and the filter it fed into are both potentially wrong.
//
// Which group holds it is not known here, so ask them all; that is the same move
// DeleteAssetDialog makes, and there are five.
function AssetLibraryWindow::onAssetRefreshed(%this, %assetID)
{
	for(%i = 0; %i < %this.dictionaryCount; %i++)
	{
		%button = %this.dictionary[%i].getButton(%assetID);
		if(isObject(%button))
		{
			%button.refreshKeys();
			%this.dictionary[%i].applySort(%this.sortField);
			%this.applyFilter();
			return;
		}
	}
}

// An asset gained or lost unsaved changes. Only the tile's caption is affected --
// no re-sort and no re-filter, because the mark is not part of the name anything
// is sorted or searched by.
function AssetLibraryWindow::onAssetDirtyChanged(%this, %assetID)
{
	for(%i = 0; %i < %this.dictionaryCount; %i++)
	{
		%button = %this.dictionary[%i].getButton(%assetID);
		if(isObject(%button))
		{
			%button.refreshDirtyMark();
			return;
		}
	}
}

function AssetLibraryWindow::unloadAssets(%this)
{
	for(%i = 0; %i < %this.dictionaryCount; %i++)
	{
		%this.dictionary[%i].unload();
	}
}

//-----------------------------------------------------------------------------
// View mode and sort field.
//-----------------------------------------------------------------------------

function AssetLibraryWindow::onChoiceRowChanged(%this, %row)
{
	if(%row.fieldName $= "viewMode")
	{
		%this.setViewMode(%row.getValue());
	}
	else if(%row.fieldName $= "sortField")
	{
		%this.setSortField(%row.getValue());
	}
}

function AssetLibraryWindow::setViewMode(%this, %mode)
{
	%this.viewMode = %mode;

	for(%i = 0; %i < %this.dictionaryCount; %i++)
	{
		%this.dictionary[%i].setViewMode(%mode);
	}

	%this.relayout();

	EditorPreferences.set("assetLibraryViewMode", %mode);
}

function AssetLibraryWindow::setSortField(%this, %field)
{
	%this.sortField = %field;

	for(%i = 0; %i < %this.dictionaryCount; %i++)
	{
		%this.dictionary[%i].applySort(%field);
	}

	EditorPreferences.set("assetLibrarySortField", %field);
}

//-----------------------------------------------------------------------------
// Search.
//-----------------------------------------------------------------------------

function AssetLibraryWindow::onSearchChanged(%this)
{
	%this.applyFilter();
}

function AssetLibraryWindow::clearSearch(%this)
{
	%this.searchBox.setText("");
	%this.applyFilter();
}

// One needle, lowercased and trimmed once, then handed to every group. Each
// group hides what does not match and reports how much survived, so the count
// line can say "12 of 40" for the library as a whole.
function AssetLibraryWindow::applyFilter(%this)
{
	%needle = strlwr(trim(%this.searchBox.getText()));
	%shown = 0;
	%total = 0;

	for(%i = 0; %i < %this.dictionaryCount; %i++)
	{
		%dictionary = %this.dictionary[%i];
		%shown += %dictionary.applyFilter(%needle);
		%total += %dictionary.getButtonCount();
	}

	// "152/152" rather than "152 of 152": this shares a row with the search box in
	// a 324 pane, and the words were clipped on a library of three figures. The
	// compact form still fits at four, which no real project reaches.
	%this.countLabel.setText(%shown @ "/" @ %total);

	// settle(), not relayout(): no group changed width, so none of them needs the
	// width nudge -- and this runs on every keystroke.
	%this.settle();
}

//-----------------------------------------------------------------------------
// Layout.
//-----------------------------------------------------------------------------

// A GuiPanelCtrl caches the height it opens to, so anything that changes the
// size of what is inside one has to make it measure again -- and a chain
// positions its children without resizing them, so it has to be told too.
//
// The cheap one: the groups are the width they already were, and only the number
// of visible cells changed. This is the keystroke path.
function AssetLibraryWindow::settle(%this)
{
	for(%i = 0; %i < %this.dictionaryCount; %i++)
	{
		%this.dictionary[%i].fixSize();
	}

	%w = getWord(%this.dictionaryList.getExtent(), 0);
	%h = getWord(%this.dictionaryList.getExtent(), 1);
	%this.dictionaryList.resize(0, 0, %w, %h);
}

// The full one, for a change of view mode: every cell is a different size now,
// so each group needs the width nudge that makes its grid re-measure before the
// panel measures the grid.
function AssetLibraryWindow::relayout(%this)
{
	for(%i = 0; %i < %this.dictionaryCount; %i++)
	{
		%this.dictionary[%i].forceLayout();
	}

	%w = getWord(%this.dictionaryList.getExtent(), 0);
	%h = getWord(%this.dictionaryList.getExtent(), 1);
	%this.dictionaryList.resize(0, 0, %w, %h);
}
