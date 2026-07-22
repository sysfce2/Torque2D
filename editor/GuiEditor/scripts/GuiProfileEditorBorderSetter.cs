
//-----------------------------------------------------------------------------
// One row of the Profile Editor's Borders pane: edits a single border slot of
// a GuiControlProfile (its default border, or one of the top/bottom/left/right
// sides). A dropdown picks one of the theme's six named borders or "Custom...";
// choosing Custom opens a compact editor of the sixteen values that matter for
// a border (margin / border / border-color / padding, each across the four
// states normal / HL / SL / NA), backed by a hidden single-use border profile.
//
// The base control is a vertical GuiChainCtrl (a GuiControl subclass) wearing a
// panel profile, so it auto-sizes as the editor expands/collapses and ripples
// that height change up through the pane's outer chain. Its two chain items are
// the header (checkbox + label + dropdown) and the collapsible editor.
//
// The creator sets these fields inline on the new object: slot
// ("default"/"top"/"bottom"/"left"/"right"), slotTitle, hasCheckbox,
// setterWidth, dialog. The dialog drives it through bind()/unbind() and reads
// currentCategory() to grey the default's border out of the side lists.
//-----------------------------------------------------------------------------

$BorderSetter::CustomRow = "Custom...";
$BorderSetter::HeaderHeight = 30;
$BorderSetter::EditorHeight = 236;
// The editor panelProfile insets its content by 10px of padding on the left and
// right; children must fit inside that or they clip. (All editor themes match.)
$BorderSetter::PanelHPad = 20;

function GuiProfileEditorBorderSetter::onAdd(%this)
{
	%full = %this.setterWidth;
	%w = %full - $BorderSetter::PanelHPad;

	%this.IsVertical = true;
	%this.ChildSpacing = 0;
	%this.setExtent(%full, $BorderSetter::HeaderHeight);
	ThemeManager.setProfile(%this, "panelProfile");

	//--- Header: checkbox (optional) + label + dropdown.
	%this.header = new GuiControl()
	{
		Position = "0 0";
		Extent = %w SPC $BorderSetter::HeaderHeight;
	};
	ThemeManager.setProfile(%this.header, "emptyProfile");
	%this.add(%this.header);

	%labelX = 6;
	if(%this.hasCheckbox)
	{
		%this.checkbox = new GuiCheckBoxCtrl()
		{
			Position = "6 5";
			Extent = "20 20";
			Text = "";
			boxOffset = "0 0";
			boxExtent = "18 18";
			textExtent = "0 0";
			Command = %this.getID() @ ".onCheck();";
		};
		ThemeManager.setProfile(%this.checkbox, "checkboxProfile");
		%this.header.add(%this.checkbox);
		%labelX = 28;
	}

	%this.label = new GuiControl()
	{
		Position = %labelX SPC 6;
		Extent = "64 18";
		Text = %this.slotTitle;
		align = "left";
		vAlign = "middle";
	};
	ThemeManager.setProfile(%this.label, "labelProfile");
	%this.header.add(%this.label);

	%dropX = %labelX + 68;
	%this.dropdown = new GuiDropDownCtrl()
	{
		class = "GuiProfileEditorBorderDropDown";
		Position = %dropX SPC 3;
		Extent = (%w - %dropX - 6) SPC 24;
		ConstantThumbHeight = false;
		ScrollBarThickness = 12;
		ShowArrowButtons = true;
		setter = %this;
	};
	ThemeManager.setProfile(%this.dropdown, "dropDownProfile");
	ThemeManager.setProfile(%this.dropdown, "dropDownItemProfile", "listBoxProfile");
	ThemeManager.setProfile(%this.dropdown, "emptyProfile", "backgroundProfile");
	ThemeManager.setProfile(%this.dropdown, "scrollingPanelProfile", "ScrollProfile");
	ThemeManager.setProfile(%this.dropdown, "scrollingPanelThumbProfile", "ThumbProfile");
	ThemeManager.setProfile(%this.dropdown, "scrollingPanelTrackProfile", "TrackProfile");
	ThemeManager.setProfile(%this.dropdown, "scrollingPanelArrowProfile", "ArrowProfile");
	%this.header.add(%this.dropdown);

	//--- Editor: the sixteen-value grid, collapsed until "Custom..." is chosen.
	%this.editor = new GuiControl()
	{
		Position = "0 0";
		Extent = %w SPC $BorderSetter::EditorHeight;
		Visible = false;
	};
	ThemeManager.setProfile(%this.editor, "emptyProfile");
	%this.add(%this.editor);

	%this.buildEditor();
	%this.editor.setExtent(%w, 0);   // start collapsed
}

// The four border-profile field state suffixes and their captions (box 0 is the
// normal state, drawn with no caption).
function GuiProfileEditorBorderSetter::stateSuffix(%this, %i)
{
	return getWord("_ HL SL NA", %i);
}

function GuiProfileEditorBorderSetter::stateField(%this, %field, %i)
{
	return (%i == 0) ? %field : (%field @ %this.stateSuffix(%i));
}

// Build the four property blocks (label, four inputs, four captions).
function GuiProfileEditorBorderSetter::buildEditor(%this)
{
	%w = %this.setterWidth - $BorderSetter::PanelHPad;
	%blockH = 58;
	%boxW = 42;
	%boxGap = 4;
	%x0 = 8;

	%rows = "Margin" TAB "margin" TAB "num" NL "Border" TAB "border" TAB "num" NL "Border Color" TAB "borderColor" TAB "color" NL "Padding" TAB "padding" TAB "num";

	%count = getRecordCount(%rows);
	for(%r = 0; %r < %count; %r++)
	{
		%rec = getRecord(%rows, %r);
		%title = getField(%rec, 0);
		%field = getField(%rec, 1);
		%isColor = getField(%rec, 2) $= "color";
		%y = %r * %blockH + 2;

		%rowLabel = new GuiControl()
		{
			Position = %x0 SPC %y;
			Extent = (%w - %x0 - 4) SPC 16;
			Text = %title;
			align = "left";
		};
		ThemeManager.setProfile(%rowLabel, "labelProfile");
		%this.editor.add(%rowLabel);

		for(%i = 0; %i < 4; %i++)
		{
			%bx = %x0 + %i * (%boxW + %boxGap);
			%by = %y + 18;

			if(%isColor)
			{
				%box = new GuiColorPopupCtrl()
				{
					Position = %bx SPC %by;
					Extent = %boxW SPC 22;
				};
				ThemeManager.setProfile(%box, "colorPickerProfile");
				ThemeManager.setProfile(%box, "emptyProfile", "backgroundProfile");
				ThemeManager.setProfile(%box, "colorPopupProfile", "popupProfile");
				ThemeManager.setProfile(%box, "emptyProfile", "pickerProfile");
				ThemeManager.setProfile(%box, "colorPickerSelectorProfile", "selectorProfile");
				%box.isColor = true;
				%box.Command = %this.getID() @ ".commitBox(" @ %box.getID() @ ");";
			}
			else
			{
				%box = new GuiTextEditCtrl()
				{
					class = "GuiProfileEditorBorderInput";
					Position = %bx SPC %by;
					Extent = %boxW SPC 22;
					inputMode = "Number";
					align = "center";
				};
				ThemeManager.setProfile(%box, "textEditProfile");
				%box.isColor = false;
				%box.setter = %this;
				%box.AltCommand = %this.getID() @ ".commitBox(" @ %box.getID() @ ");";
			}
			%box.borderField = %field;
			%box.stateIndex = %i;
			%this.editor.add(%box);
			%this.box[%field, %i] = %box;

			%cap = new GuiControl()
			{
				Position = %bx SPC (%by + 24);
				Extent = %boxW SPC 14;
				Text = (%i == 0) ? "" : %this.stateSuffix(%i);
				align = "center";
			};
			ThemeManager.setProfile(%cap, "labelProfile");
			%this.editor.add(%cap);
		}
	}
}

//-----------------------------------------------------------------------------
// Binding.
//-----------------------------------------------------------------------------

// %container owns any custom borders (the theme, or a standalone bundle).
// %disableName is the border the Default slot resolves to, greyed out of the
// side lists (empty for the Default setter itself).
function GuiProfileEditorBorderSetter::bind(%this, %profile, %container, %disableName)
{
	%this.editProfile = %profile;
	%this.container = %container;
	%this.disableName = %disableName;
	%this.customBorder = "";
	%this.populating = true;

	%this.populateDropdown();

	%border = %this.currentBorder();
	%checked = %this.hasCheckbox && %this.slotHasOwnBorder();

	if(%this.hasCheckbox)
	{
		%this.checkbox.setStateOn(%checked);
		%this.dropdown.setActive(%checked);
	}

	if(!%this.hasCheckbox || %checked)
	{
		%this.selectForBorder(%border);
	}
	else
	{
		%this.dropdown.setSelected(-1);
		%this.setEditorExpanded(false);
	}

	%this.populating = false;
}

function GuiProfileEditorBorderSetter::unbind(%this)
{
	%this.editProfile = "";
	%this.container = "";
	%this.customBorder = "";
	%this.setEditorExpanded(false);
}

// The border object (its name) this slot currently uses, or "" if none.
function GuiProfileEditorBorderSetter::currentBorder(%this)
{
	if(!isObject(%this.editProfile))
	{
		return "";
	}
	if(%this.slot $= "default")
	{
		return %this.editProfile.borderDefault;
	}
	return %this.sideName();
}

function GuiProfileEditorBorderSetter::sideName(%this)
{
	switch$(%this.slot)
	{
		case "top": return %this.editProfile.borderTop;
		case "bottom": return %this.editProfile.borderBottom;
		case "left": return %this.editProfile.borderLeft;
		case "right": return %this.editProfile.borderRight;
	}
	return "";
}

function GuiProfileEditorBorderSetter::slotHasOwnBorder(%this)
{
	return %this.slot !$= "default" && %this.sideName() !$= "";
}

// The category the current border belongs to ("" if none, blank, or custom).
function GuiProfileEditorBorderSetter::currentCategory(%this)
{
	%border = %this.currentBorder();
	if(isObject(%border) && !%border.isCustom)
	{
		return %border.category;
	}
	return "";
}

// If this side now duplicates the default border, clear it back to "use
// default" -- a side should never carry the same border as the default.
function GuiProfileEditorBorderSetter::clearIfMatches(%this, %category)
{
	if(%category $= "" || %this.slot $= "default")
	{
		return;
	}
	if(%this.currentCategory() $= %category)
	{
		%this.assignSide("");
		if(%this.hasCheckbox)
		{
			%this.checkbox.setStateOn(false);
			%this.dropdown.setActive(false);
		}
		%this.dropdown.setSelected(-1);
		%this.setEditorExpanded(false);
		%this.commit();
	}
}

//-----------------------------------------------------------------------------
// Dropdown.
//-----------------------------------------------------------------------------

function GuiProfileEditorBorderSetter::populateDropdown(%this)
{
	%dd = %this.dropdown;
	%dd.clearItems();

	if(%this.hasCheckbox)
	{
		%dd.addItem("", 0);   // the initially-selected blank row
	}

	%names = %this.dialog.borderNamesFor(%this.container);
	for(%i = 0; %i < getWordCount(%names); %i++)
	{
		%dd.addItem(getWord(%names, %i));
	}

	%dd.addItem($BorderSetter::CustomRow);

	// Grey out the border the Default slot already uses (redundant for a side).
	if(%this.disableName !$= "")
	{
		%idx = %dd.findItemText(%this.disableName, false);
		if(%idx >= 0)
		{
			%dd.setItemInactive(%idx);
		}
	}
}

// Re-grey the item the Default slot resolves to, without re-binding (so the side
// lists stay correct when the default border is changed live).
function GuiProfileEditorBorderSetter::applyDisable(%this, %name)
{
	%this.disableName = %name;
	%dd = %this.dropdown;
	for(%i = 0; %i < %dd.getItemCount(); %i++)
	{
		%dd.setItemActive(%i);
	}
	if(%name !$= "")
	{
		%idx = %dd.findItemText(%name, false);
		if(%idx >= 0)
		{
			%dd.setItemInactive(%idx);
		}
	}
}

function GuiProfileEditorBorderSetter::selectForBorder(%this, %border)
{
	if(isObject(%border) && %border.isCustom)
	{
		%this.dropdown.setSelected(%this.dropdown.findItemText($BorderSetter::CustomRow, false));
		%this.showCustom(%border);
		return;
	}

	if(isObject(%border) && %border.category !$= "")
	{
		%idx = %this.dropdown.findItemText(%border.category, false);
		if(%idx >= 0)
		{
			%this.dropdown.setSelected(%idx);
			%this.setEditorExpanded(false);
			return;
		}
	}

	%this.dropdown.setSelected(%this.hasCheckbox ? 0 : -1);
	%this.setEditorExpanded(false);
}

// Forwarded from GuiProfileEditorBorderDropDown::onSelect.
function GuiProfileEditorBorderSetter::onSelect(%this, %text)
{
	if(%this.populating)
	{
		return;
	}

	if(%text $= $BorderSetter::CustomRow)
	{
		%this.chooseCustom();
	}
	else if(%text $= "")
	{
		%this.assignSide("");
		%this.setEditorExpanded(false);
	}
	else
	{
		%this.assignNamed(%text);
		%this.setEditorExpanded(false);
	}

	%this.commit();

	// Changing the default border re-greys it out of the side lists.
	if(%this.slot $= "default" && isObject(%this.dialog))
	{
		%this.dialog.refreshSideDisables();
	}
}

//-----------------------------------------------------------------------------
// Checkbox (sides only).
//-----------------------------------------------------------------------------

function GuiProfileEditorBorderSetter::onCheck(%this)
{
	if(%this.populating)
	{
		return;
	}
	%on = %this.checkbox.getStateOn();
	%this.dropdown.setActive(%on);

	if(%on)
	{
		%this.dropdown.setSelected(0);   // blank; nothing changes until a pick
	}
	else
	{
		%this.assignSide("");
		%this.dropdown.setSelected(-1);
		%this.setEditorExpanded(false);
		%this.commit();
	}
}

//-----------------------------------------------------------------------------
// Assigning a border to this slot.
//-----------------------------------------------------------------------------

// Assign one of the theme's six named borders (by category name).
function GuiProfileEditorBorderSetter::assignNamed(%this, %category)
{
	if(%this.slot $= "default")
	{
		%this.editProfile.borderDefault = %this.dialog.borderObjectFor(%this.container, %category);
	}
	else
	{
		%border = %this.dialog.borderObjectFor(%this.container, %category);
		%this.assignSide(isObject(%border) ? %border.getName() : "");
	}
}

function GuiProfileEditorBorderSetter::assignSide(%this, %name)
{
	switch$(%this.slot)
	{
		case "top": %this.editProfile.borderTop = %name;
		case "bottom": %this.editProfile.borderBottom = %name;
		case "left": %this.editProfile.borderLeft = %name;
		case "right": %this.editProfile.borderRight = %name;
	}
}

//-----------------------------------------------------------------------------
// Custom borders: a single-use border named <Profile><Slot>CustomBorder, seeded
// from whatever the slot currently shows.
//-----------------------------------------------------------------------------

function GuiProfileEditorBorderSetter::customBorderName(%this)
{
	return %this.editProfile.getName() @ %this.slot @ "CustomBorder";
}

function GuiProfileEditorBorderSetter::chooseCustom(%this)
{
	%source = %this.currentBorder();
	if(!isObject(%source))
	{
		%source = %this.editProfile.borderDefault;   // blank side copies the default
	}

	%name = %this.customBorderName();
	if(isObject(%name))
	{
		%border = %name;
	}
	else
	{
		%border = %this.dialog.createCustomBorder(%this.container, %name);
	}

	if(!isObject(%border))
	{
		return;
	}

	// Seed the custom border from whatever the slot currently shows.
	if(isObject(%source) && %source != %border)
	{
		%this.copyBorderValues(%source, %border);
	}

	if(%this.slot $= "default")
	{
		%this.editProfile.borderDefault = %border;
	}
	else
	{
		%this.assignSide(%border.getName());
	}

	%this.showCustom(%border);
}

function GuiProfileEditorBorderSetter::showCustom(%this, %border)
{
	%this.customBorder = %border;
	%this.refreshEditor(%border);
	%this.setEditorExpanded(true);
}

function GuiProfileEditorBorderSetter::refreshEditor(%this, %border)
{
	%this.populating = true;
	%numFields = "margin" TAB "border" TAB "padding";
	for(%f = 0; %f < 3; %f++)
	{
		%field = getField(%numFields, %f);
		for(%i = 0; %i < 4; %i++)
		{
			%this.box[%field, %i].setText(%border.getFieldValue(%this.stateField(%field, %i)));
		}
	}
	for(%i = 0; %i < 4; %i++)
	{
		%this.box["borderColor", %i].setColorI(%border.getFieldValue(%this.stateField("borderColor", %i)));
	}
	%this.populating = false;
}

function GuiProfileEditorBorderSetter::copyBorderValues(%this, %from, %to)
{
	%fields = "margin" TAB "border" TAB "borderColor" TAB "padding";
	for(%f = 0; %f < 4; %f++)
	{
		%field = getField(%fields, %f);
		for(%i = 0; %i < 4; %i++)
		{
			%name = %this.stateField(%field, %i);
			%to.setFieldValue(%name, %from.getFieldValue(%name));
		}
	}
}

// A single input box committed a value into the custom border.
function GuiProfileEditorBorderSetter::commitBox(%this, %box)
{
	if(%this.populating || !isObject(%this.customBorder))
	{
		return;
	}
	%name = %this.stateField(%box.borderField, %box.stateIndex);
	%value = %box.isColor ? %box.getColorI() : %box.getText();
	%this.customBorder.setFieldValue(%name, %value);
	%this.commit();
}

//-----------------------------------------------------------------------------
// Expand / collapse + commit.
//-----------------------------------------------------------------------------

function GuiProfileEditorBorderSetter::setEditorExpanded(%this, %open)
{
	%w = %this.setterWidth - $BorderSetter::PanelHPad;
	if(%open)
	{
		%this.editor.setVisible(true);
		%this.editor.setExtent(%w, $BorderSetter::EditorHeight);
	}
	else
	{
		%this.editor.setVisible(false);
		%this.editor.setExtent(%w, 0);
	}
}

function GuiProfileEditorBorderSetter::commit(%this)
{
	if(isObject(%this.dialog))
	{
		%this.dialog.onBorderChanged();
	}
}

//-----------------------------------------------------------------------------
// The dropdown forwards its selection to the owning setter.
//-----------------------------------------------------------------------------

function GuiProfileEditorBorderDropDown::onSelect(%this, %index, %text, %id)
{
	if(isObject(%this.setter))
	{
		%this.setter.onSelect(%text);
	}
}


//-----------------------------------------------------------------------------
// The numeric input boxes: click selects all, up/down arrows nudge by 1.
//-----------------------------------------------------------------------------

function GuiProfileEditorBorderInput::onTouchDown(%this)
{
	// The engine places the cursor on click; re-select so a click selects all.
	%this.selectAllText();
}

function GuiProfileEditorBorderInput::onUpArrow(%this)
{
	%this.nudge(1);
}

function GuiProfileEditorBorderInput::onDownArrow(%this)
{
	%this.nudge(-1);
}

function GuiProfileEditorBorderInput::nudge(%this, %delta)
{
	%this.setText(%this.getText() + %delta);
	%this.selectAllText();
	if(isObject(%this.setter))
	{
		%this.setter.commitBox(%this);
	}
}
