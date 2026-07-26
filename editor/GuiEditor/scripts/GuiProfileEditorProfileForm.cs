
//-----------------------------------------------------------------------------
// The custom profile-editing pane shown in place of the generic inspector when a
// profile node is selected in the Gui Profile Editor -- the third and last of
// the panes that replaced it, after ProfileThemeEditForm and
// GuiProfileEditorBorderForm.
//
// A GuiControlProfile carries every field any control might want, but a given
// profile only ever styles one kind of control, and most of them use a fraction
// of it: seventeen of the engine's thirty-eight profile categories never draw
// text at all. The pane asks GuiProfileEditorFieldSpec what the selected
// profile's category actually reads and hides the rest, so a scroll-bar thumb
// stops offering seven font fields. A Show All checkbox lifts the filter for
// everything a profile can meaningfully carry; fields that are never meaningful
// -- SimObject plumbing, theme bookkeeping, the border references the Borders
// pane owns -- stay hidden either way.
//
// Layout is a vertical chain of blocks -- an identity header, an always-open
// essentials block, then four collapsible GuiPanelCtrl sections -- and each
// block lays its fields out in a GuiGridCtrl, the way the native inspector
// does. That is what makes the pane use the width the Properties frame is given:
// widen it and the cells reflow into two, three or four columns instead of
// leaving dead space to the right.
//
// Filtering is done purely with setVisible. Both container types skip their
// hidden children when they lay out, so a filtered field takes no space and
// leaves no hole, and never rebuilding means a selection change can never free
// a control the engine is mid-dispatch on.
//
// The pane owns every write to the profile; its rows only marshal values. The
// creator sets formWidth and dialog inline, then calls build() once after
// adding the pane to its scroller.
//-----------------------------------------------------------------------------

function GuiProfileEditorProfileForm::onAdd(%this)
{
	ThemeManager.setProfile(%this, "emptyProfile");

	%this.fieldSpec = new ScriptObject()
	{
		class = "GuiProfileEditorFieldSpec";
	};
}

function GuiProfileEditorProfileForm::onRemove(%this)
{
	%this.unbind();

	if(isObject(%this.fieldSpec))
	{
		%this.fieldSpec.delete();
	}
}

//-----------------------------------------------------------------------------
// Construction.
//-----------------------------------------------------------------------------

function GuiProfileEditorProfileForm::build(%this)
{
	%w = %this.formWidth;

	// The minimum cell width. Cells are laid out by a GuiGridCtrl in Variable
	// mode, so this is the narrowest a column may get: the grid fits as many
	// columns as the pane can hold at this width, then shares the leftover
	// evenly between them. Dragging the Properties frame wider therefore adds
	// columns rather than leaving dead space, which is the whole point of using
	// a grid here instead of a chain.
	%this.rowWidth = 220;
	%this.rowFields = "";
	%this.panelList = "";

	%this.buildHeader();
	%this.buildEssentials();

	// Sections in the order the work usually goes: the text block first because
	// it is the one a text-drawing profile reaches for after the essentials.
	%this.buildSection("TextLayout", "Text Layout",
		"align vAlign textOffset fontCharset");
	%this.buildSection("Interaction", "Interaction",
		"tab canKeyFocus cursorColor fillColorTextSL fontColorTextSL");
	%this.buildSection("RichText", "Rich Text Colors",
		"fontColorLink fontColorLinkHL fontColors7 fontColors8 fontColors9");
	%this.buildSection("Image", "Image",
		"imageAsset bitmap");

	// A GuiPanelCtrl learns its collapsed height only from parentResized -- its
	// constructor defaults to 64x64 no matter what Extent the creator set, and a
	// GuiChainCtrl positions its children without ever resizing them, so nothing
	// else would tell the panels how tall their headers are. Nudging the chain's
	// width by a pixel and back forces exactly one parentResized through every
	// child, leaving the widths where they started. (The frame set in
	// GuiProfileEditorDialog::init needs the same kind of forced layout pass.)
	%h = getWord(%this.getExtent(), 1);
	%this.resize(0, 0, %w + 1, %h);
	%this.resize(0, 0, %w, %h);

	// Text Layout starts open -- it is the section a text-drawing profile reaches
	// for after the essentials. This has to run after the nudge above: a panel
	// only records its collapsed height while it is collapsed, so expanding one
	// first would leave it with the constructor's 64-pixel header forever.
	%this.panel["TextLayout"].setExpanded(true);
}

// The header spans the whole pane rather than sitting in the cell grid: it
// identifies what is being edited and sets the filter, so it stays put while
// the cells below reflow into however many columns fit.
function GuiProfileEditorProfileForm::buildHeader(%this)
{
	%w = %this.formWidth;

	%this.header = new GuiControl()
	{
		HorizSizing = "width";
		Position = "0 0";
		Extent = %w SPC 78;
	};
	ThemeManager.setProfile(%this.header, "emptyProfile");
	%this.add(%this.header);

	%this.nameLabel = new GuiControl()
	{
		HorizSizing = "width";
		Position = "6 2";
		Extent = (%w - 12) SPC 22;
		Text = "Profile:";
		align = "left";
		vAlign = "middle";
	};
	ThemeManager.setProfile(%this.nameLabel, "labelProfile");
	%this.header.add(%this.nameLabel);

	%forLabel = new GuiControl()
	{
		Position = "6 28";
		Extent = "30 22";
		Text = "For:";
		align = "left";
		vAlign = "middle";
	};
	ThemeManager.setProfile(%forLabel, "labelProfile");
	%this.header.add(%forLabel);

	// The category is what the whole pane filters on. It is fixed for a theme
	// member (the theme names its slot) and free for a standalone profile, which
	// starts with none -- setting it there also picks the preview's sample.
	%this.categoryDrop = new GuiDropDownCtrl()
	{
		class = "GuiProfileEditorRowDropDown";
		Position = "38 28";
		Extent = "150 22";
		ConstantThumbHeight = false;
		ScrollBarThickness = 12;
		ShowArrowButtons = true;
		owner = %this;
		selectMethod = "onCategoryChanged";
	};
	ThemeManager.setProfile(%this.categoryDrop, "dropDownProfile");
	ThemeManager.setProfile(%this.categoryDrop, "dropDownItemProfile", "listBoxProfile");
	ThemeManager.setProfile(%this.categoryDrop, "emptyProfile", "backgroundProfile");
	ThemeManager.setProfile(%this.categoryDrop, "scrollingPanelProfile", "ScrollProfile");
	ThemeManager.setProfile(%this.categoryDrop, "scrollingPanelThumbProfile", "ThumbProfile");
	ThemeManager.setProfile(%this.categoryDrop, "scrollingPanelTrackProfile", "TrackProfile");
	ThemeManager.setProfile(%this.categoryDrop, "scrollingPanelArrowProfile", "ArrowProfile");
	%this.header.add(%this.categoryDrop);

	%this.categoryDrop.addItem(%this.anyCategoryLabel());
	%names = %this.fieldSpec.categoryNames;
	%count = getFieldCount(%names);
	for(%i = 0; %i < %count; %i++)
	{
		%this.categoryDrop.addItem(getField(%names, %i));
	}

	%showAllW = %w - 200;
	%this.showAllBox = new GuiCheckBoxCtrl()
	{
		HorizSizing = "width";
		Position = "194 28";
		Extent = %showAllW SPC 22;
		Text = "Show all fields";
		boxOffset = "0 2";
		boxExtent = "18 18";
		textOffset = "24 2";
		textExtent = (%showAllW - 24) SPC 18;
		Command = %this.getID() @ ".onShowAllToggled();";
	};
	ThemeManager.setProfile(%this.showAllBox, "checkboxProfile");
	%this.header.add(%this.showAllBox);

	// Shown only for the categories whose control draws a circle or a ring:
	// renderBorderedCircle and renderBorderedRing read the default border alone,
	// so the Borders pane's four sides do nothing for them.
	%this.circleHint = new GuiControl()
	{
		Position = "6 54";
		Extent = (%w - 12) SPC 20;
		Text = "Draws a circle - only the default border applies.";
		align = "left";
		vAlign = "middle";
		Visible = false;
	};
	ThemeManager.setProfile(%this.circleHint, "labelProfile");
	%this.header.add(%this.circleHint);
}

// A grid of field cells. Variable column mode reflows into as many columns as
// the pane can hold at rowWidth and shares the leftover width between them;
// variable row mode lets a taller cell (the state-color ones) set the height of
// its own row; a dynamic extent grows the grid downward inside the scroller.
// This is the same configuration the native GuiInspector gives its group grids.
//
// A hidden cell is skipped by the grid, so the cells after it close up rather
// than flowing around a hole -- see GuiGridCtrl::resize.
function GuiProfileEditorProfileForm::makeCellGrid(%this, %y)
{
	%grid = new GuiGridCtrl()
	{
		HorizSizing = "width";
		Position = "0" SPC %y;
		Extent = %this.formWidth SPC 4;
		CellModeX = "variable";
		CellModeY = "variable";
		CellSizeX = %this.rowWidth;
		CellSizeY = 48;
		CellSpacingX = 4;
		CellSpacingY = 4;
		MaxColCount = 0;
		MaxRowCount = 0;
		OrderMode = "lrtb";
		IsExtentDynamic = true;
	};
	ThemeManager.setProfile(%grid, "emptyProfile");
	return %grid;
}

// The always-visible block: the two state-color cells and the typeface, which
// together cover nearly every edit anyone makes to a profile.
function GuiProfileEditorProfileForm::buildEssentials(%this)
{
	%grid = %this.makeCellGrid(0);
	%this.add(%grid);
	%this.essentialsGrid = %grid;

	%this.fillRow = %this.addStateColorRow(%grid, "fillColor", "Fill Color");

	%this.addFieldRow(%grid, "fontType", "Font Face", "dropdown", "", "");
	%this.addFieldRow(%grid, "fontSize", "Font Size", "number", "", "");

	%this.fontRow = %this.addStateColorRow(%grid, "fontColor", "Text Color");
}

// A collapsible section. Its cells live in an inner grid rather than directly on
// the panel: GuiExpandCtrl::toggleHiddenChildren force-writes mVisible on every
// direct child whenever it expands, collapses or is resized, which would undo
// the filter. Grandchildren are untouched, and the grid skips the hidden ones.
function GuiProfileEditorProfileForm::buildSection(%this, %key, %title, %fields)
{
	%w = %this.formWidth;
	%headerH = 24;

	%panel = new GuiPanelCtrl()
	{
		HorizSizing = "width";
		Text = %title;
		Position = "0 0";
		Extent = %w SPC %headerH;
		MinExtent = "80" SPC %headerH;
	};
	ThemeManager.setProfile(%panel, "panelProfile");
	%this.add(%panel);

	%grid = %this.makeCellGrid(%headerH);
	%panel.add(%grid);

	%this.panel[%key] = %panel;
	%this.panelFields[%key] = %fields;
	%this.panelList = (%this.panelList $= "") ? %key : (%this.panelList SPC %key);

	%count = getWordCount(%fields);
	for(%i = 0; %i < %count; %i++)
	{
		%field = getWord(%fields, %i);
		%this.addFieldRow(%grid, %field, %this.labelFor(%field), %this.kindFor(%field),
			%this.enumItemsFor(%field), %this.arrayIndexFor(%field));
	}
}

function GuiProfileEditorProfileForm::addFieldRow(%this, %container, %field, %label, %kind, %enumItems, %arrayIndex)
{
	%row = new GuiControl()
	{
		class = "GuiProfileEditorFieldRow";
		Position = "0 0";
		fieldName = %field;
		labelText = %label;
		kind = %kind;
		enumItems = %enumItems;
		arrayIndex = %arrayIndex;
		owner = %this;
	};
	%container.add(%row);
	%row.build();

	%this.row[%field] = %row;
	%this.rowFields = (%this.rowFields $= "") ? %field : (%this.rowFields SPC %field);
	return %row;
}

function GuiProfileEditorProfileForm::addStateColorRow(%this, %container, %fieldBase, %label)
{
	%row = new GuiControl()
	{
		class = "GuiProfileEditorStateColorRow";
		Position = "0 0";
		fieldBase = %fieldBase;
		labelText = %label;
		owner = %this;
	};
	%container.add(%row);
	%row.build();
	return %row;
}

//-----------------------------------------------------------------------------
// Field descriptions. Kept as lookups rather than a table so the section
// definitions above read as plain field lists.
//-----------------------------------------------------------------------------

function GuiProfileEditorProfileForm::labelFor(%this, %field)
{
	// Deliberately no backslash-c in any of these: that sequence is the engine's
	// in-string color escape (see dglDrawTextN), so writing it in a caption would
	// recolor the label instead of naming the field.
	switch$(%field)
	{
		case "align":            return "Horizontal Align";
		case "vAlign":           return "Vertical Align";
		case "textOffset":       return "Text Offset";
		case "fontCharset":      return "Font Charset";
		case "tab":              return "Tab Stop";
		case "canKeyFocus":      return "Keyboard Focus";
		case "cursorColor":      return "Caret Color";
		case "fillColorTextSL":  return "Selection Fill";
		case "fontColorTextSL":  return "Selection Text";
		case "fontColorLink":    return "Link Color";
		case "fontColorLinkHL":  return "Link Highlight";
		case "fontColors7":      return "Custom Color 1";
		case "fontColors8":      return "Custom Color 2";
		case "fontColors9":      return "Custom Color 3";
		case "imageAsset":       return "Image Asset";
		case "bitmap":           return "Bitmap";
	}
	return %field;
}

function GuiProfileEditorProfileForm::kindFor(%this, %field)
{
	switch$(%field)
	{
		case "align" or "vAlign" or "fontCharset": return "enum";
		case "textOffset":                         return "point";
		case "tab" or "canKeyFocus":               return "bool";
		case "cursorColor" or "fillColorTextSL" or "fontColorTextSL":         return "color";
		case "fontColorLink" or "fontColorLinkHL":                            return "color";
		case "fontColors7" or "fontColors8" or "fontColors9":                 return "color";
	}
	return "text";
}

// The engine's own enum tables, from guiTypes.cc. Space-separated here and
// converted to the tab list the row wants, because no value contains a space.
function GuiProfileEditorProfileForm::enumItemsFor(%this, %field)
{
	%items = "";
	switch$(%field)
	{
		case "align":        %items = "left center right";
		case "vAlign":       %items = "top middle bottom";
		case "fontCharset":  %items = "ANSI SYMBOL SHIFTJIS HANGEUL HANGUL GB2312 CHINESEBIG5 OEM JOHAB HEBREW ARABIC GREEK TURKISH VIETNAMESE THAI EASTEUROPE RUSSIAN MAC BALTIC";
	}
	return %this.wordsToTabs(%items);
}

// The three user color slots live in the fontColors array rather than in named
// fields, so their rows carry the index and the pane writes them through it.
function GuiProfileEditorProfileForm::arrayIndexFor(%this, %field)
{
	switch$(%field)
	{
		case "fontColors7": return 7;
		case "fontColors8": return 8;
		case "fontColors9": return 9;
	}
	return "";
}

function GuiProfileEditorProfileForm::wordsToTabs(%this, %words)
{
	%out = "";
	%count = getWordCount(%words);
	for(%i = 0; %i < %count; %i++)
	{
		%word = getWord(%words, %i);
		%out = (%out $= "") ? %word : (%out TAB %word);
	}
	return %out;
}

function GuiProfileEditorProfileForm::anyCategoryLabel(%this)
{
	return "(any)";
}

//-----------------------------------------------------------------------------
// Binding. %kind is the tree proxy's kind, which decides whether the category
// is the user's to choose.
//-----------------------------------------------------------------------------

function GuiProfileEditorProfileForm::bind(%this, %profile, %kind)
{
	if(!isObject(%profile))
	{
		%this.unbind();
		return;
	}

	%this.target = %profile;
	%this.proxyKind = %kind;
	%this.isStandalone = (%kind $= "standalone");

	%this.nameLabel.setText("Profile:  " @ %profile.getName());

	// A theme member's category names its slot and renaming it would orphan the
	// member; only a standalone profile gets to choose.
	%this.populating = true;
	%category = %profile.category;
	%this.selectCategory(%category);
	%this.categoryDrop.setActive(%this.isStandalone);
	%this.populating = false;

	%this.applyFilter();
	%this.refresh();
}

function GuiProfileEditorProfileForm::unbind(%this)
{
	%this.target = "";
	%this.proxyKind = "";
	%this.isStandalone = false;
}

// The theme the bound profile belongs to, or nothing for a standalone profile.
// Overrides only exist against a theme.
function GuiProfileEditorProfileForm::currentTheme(%this)
{
	%root = %this.dialog.currentRoot;
	if(isObject(%root) && %root.getClassName() $= "GuiProfileTheme")
	{
		return %root;
	}
	return "";
}

function GuiProfileEditorProfileForm::currentCategory(%this)
{
	if(!isObject(%this.target))
	{
		return "";
	}
	return %this.target.category;
}

function GuiProfileEditorProfileForm::selectCategory(%this, %category)
{
	%label = (%category $= "") ? %this.anyCategoryLabel() : %category;
	%index = %this.categoryDrop.findItemText(%label, false);
	if(%index < 0)
	{
		// A category this build does not know: keep it rather than silently
		// retagging the profile.
		%this.categoryDrop.addItem(%label);
		%index = %this.categoryDrop.findItemText(%label, false);
	}
	%this.categoryDrop.setSelected(%index);
}

//-----------------------------------------------------------------------------
// Filtering. Nothing here creates or deletes a control -- it only decides what
// is visible and what is inert.
//-----------------------------------------------------------------------------

function GuiProfileEditorProfileForm::applyFilter(%this)
{
	%spec = %this.fieldSpec;
	%category = %this.currentCategory();
	%showAll = %this.showAllBox.getStateOn();

	%count = getWordCount(%this.rowFields);
	for(%i = 0; %i < %count; %i++)
	{
		%field = getWord(%this.rowFields, %i);
		%row = %this.row[%field];
		%row.setVisible(%spec.isFieldVisible(%category, %field, %showAll));
	}

	// cursorColor paints the text caret in one control and the keyboard-focus
	// rectangle in another; name it for whichever this profile is.
	%this.row["cursorColor"].setLabelText(%spec.cursorLabelFor(%category));

	// The fill row is universal; the text colors follow the category's text
	// class exactly as their individual fields would.
	%this.fillRow.setVisible(true);
	%this.fontRow.setVisible(%spec.isFieldVisible(%category, "fontColor", %showAll));

	%this.applyStateFilter(%this.fillRow, %category, %showAll);
	%this.applyStateFilter(%this.fontRow, %category, %showAll);

	// A section with nothing left to show gets out of the way entirely.
	%panels = getWordCount(%this.panelList);
	for(%i = 0; %i < %panels; %i++)
	{
		%key = getWord(%this.panelList, %i);
		%this.panel[%key].setVisible(%spec.anyFieldVisible(%category, %this.panelFields[%key], %showAll));
	}

	%this.circleHint.setVisible(!%showAll && %spec.hasFlag(%category, "circle"));
}

// Grey the states the bound control never renders in rather than hiding them,
// so a value set earlier (or by hand in the file) is never lost.
function GuiProfileEditorProfileForm::applyStateFilter(%this, %row, %category, %showAll)
{
	%spec = %this.fieldSpec;
	%reason = "This control never renders in this state.";
	for(%i = 0; %i < 4; %i++)
	{
		%live = %showAll || %spec.isStateLive(%category, %i);
		%row.setStateEnabled(%i, %live, %reason);
	}
}

function GuiProfileEditorProfileForm::onShowAllToggled(%this)
{
	%this.applyFilter();
	%this.refresh();
}

// The standalone category picker. Writing category on a standalone profile is
// safe: GuiControlProfile::onStaticModified only records overrides for a
// profile that belongs to a theme.
function GuiProfileEditorProfileForm::onCategoryChanged(%this)
{
	if(%this.populating || !isObject(%this.target) || !%this.isStandalone)
	{
		return;
	}

	%choice = %this.categoryDrop.getText();
	%this.target.category = (%choice $= %this.anyCategoryLabel()) ? "" : %choice;

	%this.applyFilter();
	%this.refresh();

	// The preview picks its sample control from the category too.
	%this.dialog.onProfileChanged(%this.target);
	%this.dialog.updatePreview();
}

//-----------------------------------------------------------------------------
// Loading values. The populating guard keeps every setText / setColorI /
// setStateOn from echoing back through the commits and marking the profile as
// having overridden fields it never touched.
//-----------------------------------------------------------------------------

function GuiProfileEditorProfileForm::refresh(%this)
{
	if(!isObject(%this.target))
	{
		return;
	}

	%this.populating = true;

	// Every font installed on this machine, plus any the project already has a
	// cache for, plus this profile's own face if it is neither.
	%this.row["fontType"].fillItems(
		%this.dialog.library.getFontFaceList(%this.target.fontType));

	%count = getWordCount(%this.rowFields);
	for(%i = 0; %i < %count; %i++)
	{
		%field = getWord(%this.rowFields, %i);
		%this.row[%field].setValue(%this.readField(%field));
	}

	for(%i = 0; %i < 4; %i++)
	{
		%this.fillRow.setValue(%i, %this.target.getFieldValue(%this.fillRow.stateField(%i)));
		%this.fontRow.setValue(%i, %this.target.getFieldValue(%this.fontRow.stateField(%i)));
	}

	%this.populating = false;

	%this.refreshOverrides();
}

// The three user color slots are array elements, which getFieldValue cannot
// reach (it always passes a null index), so they are read through slot access.
function GuiProfileEditorProfileForm::readField(%this, %field)
{
	switch$(%field)
	{
		case "fontColors7": return %this.target.fontColors[7];
		case "fontColors8": return %this.target.fontColors[8];
		case "fontColors9": return %this.target.fontColors[9];
	}
	return %this.target.getFieldValue(%field);
}

function GuiProfileEditorProfileForm::writeField(%this, %field, %value)
{
	switch$(%field)
	{
		case "fontColors7": %this.target.fontColors[7] = %value; return;
		case "fontColors8": %this.target.fontColors[8] = %value; return;
		case "fontColors9": %this.target.fontColors[9] = %value; return;
	}
	%this.target.setFieldValue(%field, %value);
}

// Mark every field that has been pushed away from its theme's stamped value. A
// standalone profile has no theme, so nothing is ever marked.
function GuiProfileEditorProfileForm::refreshOverrides(%this)
{
	%theme = %this.currentTheme();
	%hasTheme = isObject(%theme);

	%count = getWordCount(%this.rowFields);
	for(%i = 0; %i < %count; %i++)
	{
		%field = getWord(%this.rowFields, %i);
		%this.row[%field].setOverridden(%hasTheme && %theme.isFieldOverridden(%this.target, %field));
	}

	%this.refreshStateOverrides(%this.fillRow, %theme, %hasTheme);
	%this.refreshStateOverrides(%this.fontRow, %theme, %hasTheme);
}

function GuiProfileEditorProfileForm::refreshStateOverrides(%this, %row, %theme, %hasTheme)
{
	for(%i = 0; %i < 4; %i++)
	{
		%field = %row.stateField(%i);
		%row.setStateOverridden(%i, %hasTheme && %theme.isFieldOverridden(%this.target, %field));
	}
	%row.refreshResetButton();
}

//-----------------------------------------------------------------------------
// Commits. Every write to the profile goes through here, and every one of them
// ends at the dialog's commit sink, which marks the theme dirty and defers the
// preview rebuild (see GuiProfileEditorDialog::schedulePreviewRefresh -- a
// synchronous rebuild here can free a control the engine is mid-event on).
//-----------------------------------------------------------------------------

function GuiProfileEditorProfileForm::onProfileRowCommit(%this, %row)
{
	if(%this.populating || !isObject(%this.target))
	{
		return;
	}

	// A text box commits on blur, so most commits arrive from a field the user
	// only tabbed through. Writing one would mark the field as overridden away
	// from its theme -- a permanent change to the saved file -- for an edit that
	// never happened, so a value that matches what was loaded is dropped here.
	if(!%row.hasChanged())
	{
		return;
	}

	// No font cache is baked here, whatever face or size was just picked: the
	// preview renders it straight from the platform font, and baking costs seconds
	// -- it happens on Save instead.
	%this.writeField(%row.fieldName, %row.getValue());
	%row.markClean();
	%this.afterCommit();
}

function GuiProfileEditorProfileForm::onProfileStateColorCommit(%this, %row, %index)
{
	if(%this.populating || !isObject(%this.target))
	{
		return;
	}

	// As in onProfileRowCommit: a swatch that came back holding what was loaded
	// into it is not an edit, and must not record a theme override.
	if(!%row.hasChanged(%index))
	{
		return;
	}

	%this.target.setFieldValue(%row.stateField(%index), %row.getValue(%index));
	%row.markClean(%index);
	%this.afterCommit();
}

function GuiProfileEditorProfileForm::onProfileRowReset(%this, %row)
{
	%theme = %this.currentTheme();
	if(!isObject(%theme) || !isObject(%this.target))
	{
		return;
	}

	%theme.resetField(%this.target, %row.fieldName);
	%this.refresh();
	%this.afterCommit();
}

// Clears whichever of the row's four states are overridden. Resetting a state
// that is not overridden would be a no-op, but skipping them avoids a restamp
// of the whole theme per state.
function GuiProfileEditorProfileForm::onProfileStateColorReset(%this, %row)
{
	%theme = %this.currentTheme();
	if(!isObject(%theme) || !isObject(%this.target))
	{
		return;
	}

	for(%i = 0; %i < 4; %i++)
	{
		%field = %row.stateField(%i);
		if(%theme.isFieldOverridden(%this.target, %field))
		{
			%theme.resetField(%this.target, %field);
		}
	}

	%this.refresh();
	%this.afterCommit();
}

function GuiProfileEditorProfileForm::afterCommit(%this)
{
	%this.refreshOverrides();
	%this.dialog.onProfileChanged(%this.target);
}
