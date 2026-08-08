
//-----------------------------------------------------------------------------
// One field cell in the Gui Profile Editor's profile pane: a caption above an
// editor sized to the field's type, with a reset button that appears only while
// the field is overridden away from its theme's stamped value.
//
// Caption-above-editor rather than caption-beside-editor because these are grid
// cells: the pane flows them left-to-right and wraps into as many columns as the
// Properties pane is wide, the way the native inspector does, so a cell has to
// stay narrow. It also stops long captions ("Horizontal Align") from clipping.
//
// The grid resizes every cell it lays out, so the widgets carry sizing flags
// rather than fixed geometry: the caption and editor follow the cell width and
// the reset button stays pinned to its right edge.
//
// The row owns its widgets and nothing else. It never reads or writes the
// profile -- it hands values to its owner and takes them back, so the owner
// stays the single place that knows about theme overrides, array-indexed
// fields, and dirty marking. Commits arrive at owner.onProfileRowCommit(%row)
// and reset clicks at owner.onProfileRowReset(%row).
//
// The creator sets these inline: fieldName, labelText, kind, owner, and for kind
// "enum" the tab-separated enumItems. Call build() once after adding the row to
// its container -- the container decides the cell width, so build() has to run
// after the add. It records the laid-out height in .rowHeight.
//
// Kinds: text, number, point, bool, color, enum, dropdown, file, asset.
//-----------------------------------------------------------------------------

function GuiProfileEditorFieldRow::onAdd(%this)
{
	ThemeManager.setProfile(%this, "emptyProfile");
}

function GuiProfileEditorFieldRow::build(%this)
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

	// A multiline row is three lines of a wrapped text box rather than one line of
	// a plain one. Everything else about it is a text row.
	%editorH = (%this.kind $= "multiline") ? 62 : 24;
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
			class = "GuiProfileEditorRowDropDown";
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
function GuiProfileEditorFieldRow::makeFindRow(%this, %pad, %editorY, %editorW, %command)
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
function GuiProfileEditorFieldRow::isDecimalKind(%this)
{
	return %this.kind $= "decimal" || %this.kind $= "pointf";
}

// A text box that commits on blur (AltCommand) and on Enter, matching how the
// native inspector and the border grid apply their edits.
function GuiProfileEditorFieldRow::makeInput(%this, %x, %y, %w, %h, %numeric, %sizing)
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
		class = %numeric ? "GuiProfileEditorRowInput" : "";
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

function GuiProfileEditorFieldRow::makeSwatch(%this, %x, %y, %w, %h)
{
	%swatch = new GuiColorPopupCtrl()
	{
		class = "GuiProfileEditorColorPopup";
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
function GuiProfileEditorFieldRow::setValue(%this, %value)
{
	%this.applyValue(%value);
	%this.lastValue = %this.getValue();
}

// True when the widget now holds something other than what was loaded into it.
function GuiProfileEditorFieldRow::hasChanged(%this)
{
	return %this.getValue() !$= %this.lastValue;
}

// Accept the widget's current contents as the new baseline, after a commit.
function GuiProfileEditorFieldRow::markClean(%this)
{
	%this.lastValue = %this.getValue();
}

function GuiProfileEditorFieldRow::applyValue(%this, %value)
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

function GuiProfileEditorFieldRow::getValue(%this)
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
function GuiProfileEditorFieldRow::numberIn(%this, %box)
{
	return %this.isDecimalKind() ? %box.getText() : mFloor(%box.getText());
}

//-----------------------------------------------------------------------------
// Drop-down contents.
//-----------------------------------------------------------------------------

// %items is tab-separated. The current selection survives a refill even when
// the new list does not contain it (a font face outside the directory).
function GuiProfileEditorFieldRow::fillItems(%this, %items)
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

function GuiProfileEditorFieldRow::selectItem(%this, %value)
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

// A field the current control never reads stays visible but inert, so its value
// is never lost -- the pane's Show All puts it back in reach.
function GuiProfileEditorFieldRow::setEnabled(%this, %enabled, %reason)
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
	%this.editor.Tooltip = %enabled ? "" : %reason;
}

// One field wears a different name depending on the category (cursorColor is a
// text caret in one control and a focus rectangle in another), so the pane can
// retitle a row after it is built.
function GuiProfileEditorFieldRow::setLabelText(%this, %text)
{
	%this.labelText = %text;
	%this.label.setText(%text);
}

function GuiProfileEditorFieldRow::setOverridden(%this, %overridden)
{
	ThemeManager.setProfile(%this.label, %overridden ? "overrideLabelProfile" : "labelProfile");
	%this.resetButton.setVisible(%overridden);
}

//-----------------------------------------------------------------------------
// Commit. Every path lands in commit(), which does nothing while the owner is
// populating -- otherwise loading a profile would echo back as user edits and
// mark spurious theme overrides.
//-----------------------------------------------------------------------------

function GuiProfileEditorFieldRow::commit(%this)
{
	if(!isObject(%this.owner) || %this.owner.populating)
	{
		return;
	}
	if(%this.kind $= "enum" || %this.kind $= "dropdown")
	{
		%this.currentItem = %this.editor.getText();
	}
	%this.owner.onProfileRowCommit(%this);
}

function GuiProfileEditorFieldRow::onResetClicked(%this)
{
	if(isObject(%this.owner))
	{
		%this.owner.onProfileRowReset(%this);
	}
}

// The Find button on a "file" row. Picks a file and writes its path back into
// the box relative to the game root, which is the only form that means the same
// thing on somebody else's machine.
function GuiProfileEditorFieldRow::onFindClicked(%this)
{
	%dialog = new OpenFileDialog()
	{
		Filters = "Image Files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|All Files (*.*)|*.*";
		ChangePath = false;
		MultipleFiles = false;
		DefaultFile = "";
		defaultPath = pathConcat(getMainDotCsDir(), ProjectManager.getProjectFolder());
		title = "Choose an Image";
	};
	%result = %dialog.execute();
	%fileName = %dialog.fileName;
	%dialog.delete();

	if(!%result || %fileName $= "")
	{
		return;
	}

	%this.editor.setText(makeRelativePath(%fileName, getMainDotCsDir()));
	%this.commit();
}

// The Find button on an "asset" row. The picker lives in EditorCore because the
// native inspector's browse button uses it too; it hands back the chosen id
// through onAssetPicked. Whatever the box holds now is passed along so the
// picker opens on the current choice.
function GuiProfileEditorFieldRow::onFindAssetClicked(%this)
{
	EditorCore.openAssetPicker(%this, "onAssetPicked", %this.editor.getText(), "ImageAsset");
}

// An asset id is already portable -- it names a module and an asset, not a
// place on this machine -- so unlike a bitmap path it goes in as it came out.
function GuiProfileEditorFieldRow::onAssetPicked(%this, %assetId)
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

function GuiProfileEditorRowInput::onUpArrow(%this)
{
	%this.nudge(1);
}

function GuiProfileEditorRowInput::onDownArrow(%this)
{
	%this.nudge(-1);
}

function GuiProfileEditorRowInput::nudge(%this, %delta)
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

function GuiProfileEditorRowDropDown::onSelect(%this)
{
	%this.owner.call(%this.selectMethod);
}
