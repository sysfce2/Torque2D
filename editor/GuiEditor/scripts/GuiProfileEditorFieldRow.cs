
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
// Kinds: text, number, point, bool, color, enum, dropdown, file.
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
	%editorY = %labelH + 4;
	%editorH = 24;
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
		%this.editor = %this.makeSwatch(%pad, %editorY, %editorW, 22);
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
	else if(%kind $= "point")
	{
		// A Point2I field ("x y") gets one box per axis; either one commits both.
		// Relative sizing splits the widened cell evenly between them.
		%boxW = (%editorW - 6) / 2;
		%this.editor = %this.makeInput(%pad, %editorY, %boxW, 22, true, "relative");
		%this.editorY = %this.makeInput(%pad + %boxW + 6, %editorY, %boxW, 22, true, "relative");
	}
	else if(%kind $= "file")
	{
		// A font directory: the user finds any font inside the target folder and
		// we keep the folder, matching the theme form's Find pattern.
		%buttonW = 56;
		%this.editor = %this.makeInput(%pad, %editorY, %editorW - %buttonW - 4, 22, false, "width");
		%this.findButton = new GuiButtonCtrl()
		{
			HorizSizing = "left";
			Position = (%pad + %editorW - %buttonW) SPC %editorY;
			Extent = %buttonW SPC 22;
			Text = "Find";
			Command = %this.getID() @ ".onFindClicked();";
		};
		ThemeManager.setProfile(%this.findButton, "buttonProfile");
		%this.add(%this.findButton);
	}
	else
	{
		%this.editor = %this.makeInput(%pad, %editorY, %editorW, 22, %kind $= "number", "width");
	}

	// The per-field reset, shown only while the field is overridden. Frame 22 of
	// EditorCore:editorIcons16 is the circular revert arrow. "left" sizing keeps
	// the button pinned to the cell's right edge as the grid widens.
	%this.resetButton = new GuiButtonCtrl()
	{
		class = "EditorIconButton";
		Frame = 22;
		HorizSizing = "left";
		Position = (%w - %resetW - %pad) SPC (%editorY - 1);
		Tooltip = "Reset this field to the theme's value";
		Command = %this.getID() @ ".onResetClicked();";
		Visible = false;
	};
	ThemeManager.setProfile(%this.resetButton, "iconButtonProfile");
	%this.add(%this.resetButton);
}

// A text box that commits on blur (AltCommand) and on Enter, matching how the
// native inspector and the border grid apply their edits.
function GuiProfileEditorFieldRow::makeInput(%this, %x, %y, %w, %h, %numeric, %sizing)
{
	%box = new GuiTextEditCtrl()
	{
		class = "GuiProfileEditorRowInput";
		HorizSizing = %sizing;
		Position = %x SPC %y;
		Extent = %w SPC %h;
		align = %numeric ? "center" : "left";
		row = %this;
		numeric = %numeric;
	};
	if(%numeric)
	{
		%box.inputMode = "Number";
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
		HorizSizing = "width";
		Position = %x SPC %y;
		Extent = %w SPC %h;
	};
	ThemeManager.setProfile(%swatch, "colorPickerProfile");
	ThemeManager.setProfile(%swatch, "emptyProfile", "backgroundProfile");
	ThemeManager.setProfile(%swatch, "colorPopupProfile", "popupProfile");
	ThemeManager.setProfile(%swatch, "emptyProfile", "pickerProfile");
	ThemeManager.setProfile(%swatch, "colorPickerSelectorProfile", "selectorProfile");
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
	else if(%kind $= "point")
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
	if(%kind $= "point")
	{
		return mFloor(%this.editor.getText()) SPC mFloor(%this.editorY.getText());
	}
	if(%kind $= "number")
	{
		return mFloor(%this.editor.getText());
	}
	return %this.editor.getText();
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

function GuiProfileEditorFieldRow::onFindClicked(%this)
{
	%path = pathConcat(getMainDotCsDir(), ProjectManager.getProjectFolder());
	%dialog = new OpenFileDialog()
	{
		Filters = "Font Files (*.ttf;*.otf;*.fnt;*.uft)|*.ttf;*.otf;*.fnt;*.uft";
		ChangePath = false;
		MultipleFiles = false;
		DefaultFile = "";
		defaultPath = %path;
		title = "Find a Font in the Target Directory";
	};
	%result = %dialog.execute();
	%fileName = %dialog.fileName;
	%dialog.delete();

	if(!%result)
	{
		return;
	}

	// Keep the directory relative to the game root, not the absolute path.
	%dir = filePath(makeRelativePath(%fileName, getMainDotCsDir()));
	%this.editor.setText(%dir);
	%this.commit();
}

//-----------------------------------------------------------------------------
// Two widget helpers, kept here because each exists only to route one engine
// callback back to whatever owns the widget -- the same arrangement the border
// grid uses for GuiProfileEditorBorderInput. The drop-down is shared: the
// profile pane's category picker uses it too, pointing selectMethod at its own
// handler.
//-----------------------------------------------------------------------------

function GuiProfileEditorRowInput::onTouchDown(%this)
{
	// The engine places the cursor on click; re-select so a click selects all.
	%this.selectAllText();
}

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
	%this.setText(%this.getText() + %delta);
	%this.selectAllText();
	%this.row.commit();
}

function GuiProfileEditorRowDropDown::onSelect(%this)
{
	%this.owner.call(%this.selectMethod);
}
