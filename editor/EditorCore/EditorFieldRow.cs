
//-----------------------------------------------------------------------------
// One field cell in an editor's properties pane: a caption above an editor
// sized to the field's type, with a reset button beside it.
//
// Grown in the Gui Profile Editor and now shared -- the Gui Editor's inspector
// pane, the Profile Editor's three forms and the Asset Manager's asset panes all
// build their rows from this, which is why it lives in EditorCore rather than in
// the module that first wanted it.
//
// Caption-above-editor rather than caption-beside-editor because these are grid
// cells: the pane flows them left-to-right and wraps into as many columns as the
// pane is wide, the way the native inspector does, so a cell has to stay narrow.
// It also stops long captions ("Horizontal Align") from clipping.
//
// The grid resizes every cell it lays out, so the widgets carry sizing flags
// rather than fixed geometry: the caption and editor follow the cell width and
// the reset button stays pinned to its right edge.
//
// The row owns its widgets and nothing else. It never reads or writes the thing
// being edited -- it hands values to its owner and takes them back, so the owner
// stays the single place that knows about theme overrides, array-indexed fields,
// and dirty marking. Commits arrive at owner.onFieldRowCommit(%row) and reset
// clicks at owner.onFieldRowReset(%row).
//
// The creator sets these inline: fieldName, labelText, kind, owner, and for kind
// "enum" the tab-separated enumItems. Optionally swatchWidth (a "color" row that
// should not fill its cell) and editorHeight (how deep a "multiline" box is).
// Call build() once after adding the row to its container -- the container
// decides the cell width, so build() has to run after the add. It records the
// laid-out height in .rowHeight. setTooltip() after that gives the row a standing
// explanation of its field, which survives being greyed and re-enabled.
//
// Two things a row takes from its OWNER rather than from itself, because they
// are decisions the whole pane makes once:
//
//   swatchClass  the class a color popup wears. Empty gives a plain one; the
//                Profile Editor points it at GuiProfileEditorColorPopup, which
//                fills the swatch row from the theme in its tree. That class
//                belongs to the Gui Editor module and cannot be named here.
//   findBase     what a "file" row's Find button makes its path relative to.
//                The default is the game root, which is what a bitmap path
//                means; an asset's loose file is relative to the asset's own
//                folder instead.
//   fileFilters  what that Find button's dialog offers, and
//   fileTitle    what it calls itself. Both default to bitmaps, which is what a
//                "file" row was everywhere until fonts and sounds got panes.
//
// One thing a row takes from ITSELF, because a pane can hold rows that want
// different answers -- the emitter pane has an image row and an animation row
// side by side:
//
//   assetType    what an "asset" row's Find button offers to pick. Defaults to
//                ImageAsset, which is what every asset row was until then.
//
// Kinds: text, number, decimal, point, pointf, bool, color, enum, dropdown,
// file, asset, multiline.
//-----------------------------------------------------------------------------

function EditorFieldRow::onAdd(%this)
{
	ThemeManager.setProfile(%this, "emptyProfile");
}

function EditorFieldRow::build(%this)
{
	// The grid sizes a cell the moment it is added, which is before build()
	// runs, so lay out against the width we actually have rather than the
	// nominal one -- otherwise every widget would be placed for a 220-wide cell
	// inside a cell the grid had already widened. Sizing flags take it from here.
	%w = getWord(%this.getExtent(), 0);
	%pad = 4;
	%resetW = 24;
	%labelH = 16;

	// No caption means no room kept for one. The text block asks for this: its
	// text box is captioned by the row above, which is what leaves space beside
	// the caption for the wrap and extend icons. The label control is still
	// built, so setLabelText can give it one later.
	%captioned = %this.labelText !$= "";
	%editorY = %captioned ? (%labelH + 4) : 2;

	// A multiline row is a wrapped text box several lines deep rather than one
	// line of a plain one. Everything else about it is a text row. Three lines
	// unless the creator asked for a particular depth -- a row that has a grid
	// cell to itself can afford more, and an empty box the size of its neighbours
	// says "this is where the prose goes" in a way a three-line one does not.
	%editorH = 24;
	if(%this.kind $= "multiline")
	{
		%editorH = (%this.editorHeight > 0) ? %this.editorHeight : 62;
	}
	%h = %editorY + %editorH + 4;

	// The editor stops short of the reset button so the two never overlap once
	// the reset appears.
	%editorW = %w - (%pad * 2) - %resetW - 2;

	%this.rowHeight = %h;
	%this.setExtent(%w, %h);

	%this.label = new GuiControl()
	{
		HorizSizing = "width";
		Position = %pad SPC 2;
		Extent = (%w - %pad * 2) SPC %labelH;
		Text = %this.labelText;
		align = "left";
		vAlign = "middle";
		Visible = %captioned;
	};
	ThemeManager.setProfile(%this.label, "labelProfile");
	%this.add(%this.label);

	%kind = %this.kind;
	if(%kind $= "bool")
	{
		// The caption above already names the field, so the box carries no text
		// of its own -- and it stays square rather than stretching, which a wide
		// empty checkbox would do.
		%this.editor = new GuiCheckBoxCtrl()
		{
			Position = %pad SPC %editorY;
			Extent = "20 20";
			Text = "";
			boxOffset = "0 1";
			boxExtent = "18 18";
			textExtent = "0 18";
			Command = %this.getID() @ ".commit();";
		};
		ThemeManager.setProfile(%this.editor, "checkboxProfile");
		%this.add(%this.editor);
	}
	else if(%kind $= "color")
	{
		// swatchWidth opts a row out of filling its cell. A swatch that spans the
		// whole width reads as a bar of flat color -- a progress bar, a divider --
		// rather than as the button it is. Rows that show a state's colors keep
		// the full width, because there four of them share it and the widths are
		// how you tell which is which.
		%this.editor = %this.makeSwatch(%pad, %editorY,
			(%this.swatchWidth > 0) ? %this.swatchWidth : %editorW, 22);
	}
	else if(%kind $= "enum" || %kind $= "dropdown")
	{
		%this.editor = new GuiDropDownCtrl()
		{
			class = "EditorFieldRowDropDown";
			HorizSizing = "width";
			Position = %pad SPC %editorY;
			Extent = %editorW SPC 22;
			ConstantThumbHeight = false;
			ScrollBarThickness = 12;
			ShowArrowButtons = true;
			owner = %this;
			selectMethod = "commit";
		};
		ThemeManager.setProfile(%this.editor, "dropDownProfile");
		ThemeManager.setProfile(%this.editor, "dropDownItemProfile", "listBoxProfile");
		ThemeManager.setProfile(%this.editor, "emptyProfile", "backgroundProfile");
		ThemeManager.setProfile(%this.editor, "scrollingPanelProfile", "ScrollProfile");
		ThemeManager.setProfile(%this.editor, "scrollingPanelThumbProfile", "ThumbProfile");
		ThemeManager.setProfile(%this.editor, "scrollingPanelTrackProfile", "TrackProfile");
		ThemeManager.setProfile(%this.editor, "scrollingPanelArrowProfile", "ArrowProfile");
		%this.add(%this.editor);

		if(%kind $= "enum")
		{
			%this.fillItems(%this.enumItems);
		}
	}
	else if(%kind $= "point" || %kind $= "pointf")
	{
		// A two-part field ("x y") gets one box per axis; either one commits both.
		// Relative sizing splits the widened cell evenly between them.
		%boxW = (%editorW - 6) / 2;
		%this.editor = %this.makeInput(%pad, %editorY, %boxW, 22, true, "relative");
		%this.editorY = %this.makeInput(%pad + %boxW + 6, %editorY, %boxW, 22, true, "relative");
	}
	else if(%kind $= "file")
	{
		// A path the user should not have to type: a text box plus a Find button
		// that opens a file dialog and writes back what it picked.
		%this.makeFindRow(%pad, %editorY, %editorW, ".onFindClicked();");
	}
	else if(%kind $= "asset")
	{
		// Same shape as a file, but Find opens the asset picker instead of the
		// file dialog -- an asset id is no more typeable from memory than a path.
		%this.makeFindRow(%pad, %editorY, %editorW, ".onFindAssetClicked();");
	}
	else if(%kind $= "multiline")
	{
		// The full cell width: this row's reset button is never shown, and the
		// caption line above it is where its owner puts anything else.
		//
		// mTextWrap is what makes GuiTextEditCtrl multi-line -- it wraps, scrolls
		// and hit-tests in line space (guiTextEditCtrl.cc). It still cannot take a
		// typed newline: handleEnterKey has no insert path, so Enter goes on
		// meaning commit and a long string simply wraps.
		%this.editor = %this.makeInput(%pad, %editorY, %w - (%pad * 2), %editorH, false, "width");
		%this.editor.textWrap = true;
		%this.editor.vAlign = "top";
	}
	else
	{
		%this.editor = %this.makeInput(%pad, %editorY, %editorW, 22,
			%kind $= "number" || %kind $= "decimal", "width");
	}

	// The per-field reset, shown only while the field is overridden. The icon is
	// the circular revert arrow. "left" sizing keeps the button pinned to the
	// cell's right edge as the grid widens.
	%this.resetButton = new GuiButtonCtrl()
	{
		class = "EditorIconButton";
		Frame = $EditorIcon::playback_reload;
		HorizSizing = "left";
		Position = (%w - %resetW - %pad) SPC (%editorY - 1);
		Tooltip = "Reset this field to the theme's value";
		Command = %this.getID() @ ".onResetClicked();";
		Visible = false;
	};
	ThemeManager.setProfile(%this.resetButton, "iconButtonProfile");
	%this.add(%this.resetButton);
}

// A text box with a Find button beside it, for the two fields whose value is
// something to be chosen rather than typed. The caller supplies the method the
// button calls; everything else about the two rows is identical, including the
// button keeping its place at the cell's right edge as the grid widens.
function EditorFieldRow::makeFindRow(%this, %pad, %editorY, %editorW, %command)
{
	%buttonW = 56;
	%this.editor = %this.makeInput(%pad, %editorY, %editorW - %buttonW - 4, 22, false, "width");
	%this.findButton = new GuiButtonCtrl()
	{
		HorizSizing = "left";
		Position = (%pad + %editorW - %buttonW) SPC %editorY;
		Extent = %buttonW SPC 22;
		Text = "Find";
		Command = %this.getID() @ %command;
	};
	ThemeManager.setProfile(%this.findButton, "buttonProfile");
	%this.add(%this.findButton);
}

// True where the field holds a real number rather than a whole one. Kept as a
// question about the kind rather than a flag on the row, because it is asked
// from three places and has to answer the same way in all of them.
function EditorFieldRow::isDecimalKind(%this)
{
	return %this.kind $= "decimal" || %this.kind $= "pointf";
}

// A text box that commits on blur (AltCommand) and on Enter, matching how the
// native inspector and the border grid apply their edits.
function EditorFieldRow::makeInput(%this, %x, %y, %w, %h, %numeric, %sizing)
{
	%decimal = %this.isDecimalKind();

	%box = new GuiTextEditCtrl()
	{
		// The class only goes on a box that wants the arrow keys to step its
		// value. GuiTextEditCtrl gives a script onUpArrow the key before its own
		// caret movement (guiTextEditCtrl.cc handleKeyDownWithNoModifier), and
		// isMethod answers for the CLASS, not the instance -- so wearing this on
		// a text box meant nudge() swallowed both arrows and did nothing with
		// them, which is what stopped the caret moving between lines in the
		// multi-line box.
		class = %numeric ? "EditorFieldRowInput" : "";
		HorizSizing = %sizing;
		Position = %x SPC %y;
		Extent = %w SPC %h;
		align = %numeric ? "center" : "left";
		row = %this;
		numeric = %numeric;

		// What an arrow key is worth. A font size multiplier lives between 0.5
		// and 3, so stepping it by one is the same as not offering the key.
		step = %decimal ? 0.1 : 1;
	};
	if(%numeric)
	{
		// Number mode refuses the decimal point, which would make a float field
		// impossible to type into.
		%box.inputMode = %decimal ? "Decimal" : "Number";
	}
	ThemeManager.setProfile(%box, "textEditProfile");
	%box.AltCommand = %this.getID() @ ".commit();";
	%box.ReturnCommand = %this.getID() @ ".commit();";
	%this.add(%box);
	return %box;
}

// The swatch's class comes from the owner, not from here: the Profile Editor
// wants one that fills its swatch row from the theme under the tree, and that
// class lives in the Gui Editor module, which EditorCore knows nothing about.
// Empty gives the plain popup, which is what a pane with no theme to offer wants.
function EditorFieldRow::makeSwatch(%this, %x, %y, %w, %h)
{
	%swatch = new GuiColorPopupCtrl()
	{
		class = isObject(%this.owner) ? %this.owner.swatchClass : "";
		// A fixed-width swatch stays put as the cell widens; a full-width one
		// follows it.
		HorizSizing = (%this.swatchWidth > 0) ? "anchorLeft" : "width";
		Position = %x SPC %y;
		Extent = %w SPC %h;
		showColorValues = true;
	};
	ThemeManager.setProfile(%swatch, "colorPickerProfile");
	ThemeManager.setProfile(%swatch, "emptyProfile", "backgroundProfile");
	ThemeManager.setProfile(%swatch, "colorPopupProfile", "popupProfile");
	ThemeManager.setProfile(%swatch, "emptyProfile", "pickerProfile");
	ThemeManager.setProfile(%swatch, "colorPickerSelectorProfile", "selectorProfile");
	ThemeManager.setProfile(%swatch, "textEditProfile", "valueProfile");
	// The popup hands this on to its R/G/B/A boxes, which name their channel with
	// a tooltip -- without it they would each fall back to a profile of their own.
	ThemeManager.setProfile(%swatch, "tipProfile", "TooltipProfile");
	%swatch.Command = %this.getID() @ ".commit();";
	%this.add(%swatch);
	return %swatch;
}

//-----------------------------------------------------------------------------
// Value marshalling. The owner supplies and receives plain field strings; the
// row knows how its widget spells them.
//-----------------------------------------------------------------------------

// Loading a value also records it, so a later commit can tell an actual edit
// from a text box that merely lost focus. The recorded form is whatever the
// widget reads back, not what was passed in, because the two differ: a ColorI
// field holding "White" comes back out of the swatch as "255 255 255 255".
function EditorFieldRow::setValue(%this, %value)
{
	%this.applyValue(%value);
	%this.lastValue = %this.getValue();
}

// True when the widget now holds something other than what was loaded into it.
function EditorFieldRow::hasChanged(%this)
{
	return %this.getValue() !$= %this.lastValue;
}

// Accept the widget's current contents as the new baseline, after a commit.
function EditorFieldRow::markClean(%this)
{
	%this.lastValue = %this.getValue();
}

function EditorFieldRow::applyValue(%this, %value)
{
	%kind = %this.kind;
	if(%kind $= "bool")
	{
		%this.editor.setStateOn(%value);
	}
	else if(%kind $= "color")
	{
		// setColorI wants four integers, but a ColorI field holding a stock color
		// comes back as a single name token; baseColor parses those.
		if(getWordCount(%value) >= 4)
		{
			%this.editor.setColorI(%value);
		}
		else
		{
			%this.editor.baseColor = %value;
		}
	}
	else if(%kind $= "enum" || %kind $= "dropdown")
	{
		%this.selectItem(%value);
	}
	else if(%kind $= "point" || %kind $= "pointf")
	{
		%this.editor.setText(getWord(%value, 0));
		%this.editorY.setText(getWord(%value, 1));
	}
	else
	{
		%this.editor.setText(%value);
	}
}

function EditorFieldRow::getValue(%this)
{
	%kind = %this.kind;
	if(%kind $= "bool")
	{
		return %this.editor.getStateOn();
	}
	if(%kind $= "color")
	{
		return %this.editor.getColorI();
	}
	if(%kind $= "enum" || %kind $= "dropdown")
	{
		return %this.editor.getText();
	}
	if(%kind $= "point" || %kind $= "pointf")
	{
		return %this.numberIn(%this.editor) SPC %this.numberIn(%this.editorY);
	}
	if(%kind $= "number" || %kind $= "decimal")
	{
		return %this.numberIn(%this.editor);
	}
	return %this.editor.getText();
}

// A whole-number field is floored, so a box left holding "12.0" does not write
// "12.0" into a Point2I. A real one is not: flooring a font size multiplier
// turns every 1.5 into a 1, which is how a control that would not resize looked
// like a control whose font size did nothing.
function EditorFieldRow::numberIn(%this, %box)
{
	return %this.isDecimalKind() ? %box.getText() : mFloor(%box.getText());
}

//-----------------------------------------------------------------------------
// Drop-down contents.
//-----------------------------------------------------------------------------

// %items is tab-separated. The current selection survives a refill even when
// the new list does not contain it (a font face outside the directory).
function EditorFieldRow::fillItems(%this, %items)
{
	%selected = %this.currentItem;
	%this.editor.clearItems();

	%count = getFieldCount(%items);
	for(%i = 0; %i < %count; %i++)
	{
		%item = getField(%items, %i);
		if(%item !$= "")
		{
			%this.editor.addItem(%item);
		}
	}

	if(%selected !$= "")
	{
		%this.selectItem(%selected);
	}
}

function EditorFieldRow::selectItem(%this, %value)
{
	%this.currentItem = %value;
	if(%value $= "")
	{
		return;
	}

	%index = %this.editor.findItemText(%value, false);
	if(%index >= 0)
	{
		%this.editor.setSelected(%index);
		return;
	}

	// Not offered: keep it anyway rather than silently switching the field.
	%this.editor.insertItem(0, %value);
	%this.editor.setSelected(0);
}

//-----------------------------------------------------------------------------
// Filtering, enabling and the override marker.
//-----------------------------------------------------------------------------

// What the row says about itself when it is working normally, as opposed to the
// reason setEnabled gives for it not being.
//
// The two share one tooltip, so they have to be told about each other: a row that
// is greyed and then re-enabled used to come back with no tooltip at all, because
// setEnabled blanked it. The explanation is kept here so enabling can put it back.
//
// A field's own doc string would be the obvious source and is not one -- the
// engine's is empty on every AudioAsset field and on most others, so this is set
// by the pane that knows what the field means (AssetInspectorPane::tipFor).
function EditorFieldRow::setTooltip(%this, %tip)
{
	%this.baseTip = %tip;
	%this.applyTooltip(%tip);
}

// One tooltip on every widget the row owns. The editor alone is not enough on a
// "file" or "asset" row, where the Find button is half the row's hit area, nor on
// a point row, where the second box is half of it.
function EditorFieldRow::applyTooltip(%this, %tip)
{
	%this.editor.Tooltip = %tip;
	if(isObject(%this.editorY))
	{
		%this.editorY.Tooltip = %tip;
	}
	if(isObject(%this.findButton))
	{
		%this.findButton.Tooltip = %tip;
	}
}

// A field the current control never reads stays visible but inert, so its value
// is never lost -- the pane's Show All puts it back in reach.
function EditorFieldRow::setEnabled(%this, %enabled, %reason)
{
	%this.editor.setActive(%enabled);
	if(isObject(%this.editorY))
	{
		%this.editorY.setActive(%enabled);
	}
	if(isObject(%this.findButton))
	{
		%this.findButton.setActive(%enabled);
	}

	// Enabling restores what the row normally says rather than blanking it.
	%this.applyTooltip(%enabled ? %this.baseTip : %reason);
}

// One field wears a different name depending on the category (cursorColor is a
// text caret in one control and a focus rectangle in another), so the pane can
// retitle a row after it is built.
function EditorFieldRow::setLabelText(%this, %text)
{
	%this.labelText = %text;
	%this.label.setText(%text);
}

function EditorFieldRow::setOverridden(%this, %overridden)
{
	ThemeManager.setProfile(%this.label, %overridden ? "overrideLabelProfile" : "labelProfile");
	%this.resetButton.setVisible(%overridden);
}

//-----------------------------------------------------------------------------
// Commit. Every path lands in commit(), which does nothing while the owner is
// populating -- otherwise loading a profile would echo back as user edits and
// mark spurious theme overrides.
//-----------------------------------------------------------------------------

function EditorFieldRow::commit(%this)
{
	if(!isObject(%this.owner) || %this.owner.populating)
	{
		return;
	}
	if(%this.kind $= "enum" || %this.kind $= "dropdown")
	{
		%this.currentItem = %this.editor.getText();
	}
	%this.owner.onFieldRowCommit(%this);
}

function EditorFieldRow::onResetClicked(%this)
{
	if(isObject(%this.owner))
	{
		%this.owner.onFieldRowReset(%this);
	}
}

// What a chosen path is written back relative to. The game root is the right
// answer for a bitmap named in a profile -- it is the only form that means the
// same thing on somebody else's machine -- but an asset's loose file is stored
// relative to the asset's own folder, so a pane that edits one sets findBase.
function EditorFieldRow::pathBase(%this)
{
	if(isObject(%this.owner) && %this.owner.findBase !$= "")
	{
		return %this.owner.findBase;
	}
	return getMainDotCsDir();
}

// What the Find dialog offers, and what it calls itself.
//
// Bitmaps by default: a "file" row was a picture everywhere until the Asset
// Manager grew panes for fonts and sounds, whose loose file is a .fnt or a .wav
// and for which an image filter offers nothing that can be chosen. Taken from
// the owner rather than the row for the same reason findBase is -- it is one
// decision a pane makes about the one loose file it edits.
function EditorFieldRow::fileFilters(%this)
{
	if(isObject(%this.owner) && %this.owner.fileFilters !$= "")
	{
		return %this.owner.fileFilters;
	}
	return "Image Files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All Files (*.*)|*.*";
}

function EditorFieldRow::fileTitle(%this)
{
	if(isObject(%this.owner) && %this.owner.fileTitle !$= "")
	{
		return %this.owner.fileTitle;
	}
	return "Choose an Image";
}

// The Find button on a "file" row. Picks a file and writes its path back into
// the box relative to whatever pathBase() says it should be measured from.
function EditorFieldRow::onFindClicked(%this)
{
	// Where the path is measured from, and where the dialog opens. They are the
	// same folder when a pane has named one; with no base the path is the game
	// root's but the dialog still opens on the project, which is where the
	// pictures are.
	%base = %this.pathBase();
	%start = (isObject(%this.owner) && %this.owner.findBase !$= "")
		? %base : pathConcat(%base, ProjectManager.getProjectFolder());

	%dialog = new OpenFileDialog()
	{
		Filters = %this.fileFilters();
		ChangePath = false;
		MultipleFiles = false;
		DefaultFile = "";
		defaultPath = %start;
		title = %this.fileTitle();
	};
	%result = %dialog.execute();
	%fileName = %dialog.fileName;
	%dialog.delete();

	if(!%result || %fileName $= "")
	{
		return;
	}

	%this.editor.setText(makeRelativePath(%fileName, %base));
	%this.commit();
}

// The Find button on an "asset" row. The picker lives in EditorCore because the
// native inspector's browse button uses it too; it hands back the chosen id
// through onAssetPicked. Whatever the box holds now is passed along so the
// picker opens on the current choice.
//
// assetType is per ROW rather than per pane -- unlike findBase and fileFilters
// above -- because the emitter pane carries an Image row and an Animation row in
// the same block and they want different lists. It defaults to ImageAsset, which
// was hardcoded here until the second kind of asset row existed.
function EditorFieldRow::onFindAssetClicked(%this)
{
	%type = (%this.assetType $= "") ? "ImageAsset" : %this.assetType;

	EditorCore.openAssetPicker(%this, "onAssetPicked", %this.editor.getText(), %type);
}

// An asset id is already portable -- it names a module and an asset, not a
// place on this machine -- so unlike a bitmap path it goes in as it came out.
function EditorFieldRow::onAssetPicked(%this, %assetId)
{
	%this.editor.setText(%assetId);
	%this.commit();
}

//-----------------------------------------------------------------------------
// Two widget helpers, kept here because each exists only to route one engine
// callback back to whatever owns the widget -- the same arrangement the border
// grid uses for GuiProfileEditorBorderInput. The drop-down is shared: the
// profile pane's category picker uses it too, pointing selectMethod at its own
// handler.
//-----------------------------------------------------------------------------

// No onTouchDown override here on purpose. Re-selecting the whole value on every
// click made the caret impossible to place: the engine had already put it where
// the click landed, and selecting all moved the selection anchor back to the
// start, so the next drag swept from the beginning of the field. Tabbing in
// still selects everything - GuiTextEditCtrl::setFirstResponder does that - and
// a click now does what a click does.

function EditorFieldRowInput::onUpArrow(%this)
{
	%this.nudge(1);
}

function EditorFieldRowInput::onDownArrow(%this)
{
	%this.nudge(-1);
}

function EditorFieldRowInput::nudge(%this, %delta)
{
	if(!%this.numeric)
	{
		return;
	}

	// Rounded, or repeated tenths accumulate into 1.2000000476837158.
	%value = %this.getText() + (%delta * %this.step);
	%this.setText(%this.step < 1 ? mFloatLength(%value, 2) : %value);

	%this.selectAllText();
	%this.row.commit();
}

function EditorFieldRowDropDown::onSelect(%this)
{
	%this.owner.call(%this.selectMethod);
}
