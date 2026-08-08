
//-----------------------------------------------------------------------------
// One static row of a list box or a drop down, on one line of the Items
// section.
//
//     [ Easy                ][ 1 ][c][#][o][*][^][v][X]
//
// caption, ID, "show color", the color, active, starts selected, move up, move
// down, remove.
//
// Show color, not "own color": hasColor does not tint anything. The list draws a
// small colored bullet in front of the caption and indents the text past it
// (GuiListBoxCtrl::onRenderItem, renderColorBullet) - the caption itself is
// drawn in the profile's font color either way. So the switch is about whether
// the dot is there at all.
//
// Nine controls rather than the eight the row looks like it needs, because that
// dot is two values: hasColor and color. Nothing can be read off a swatch alone
// -- a swatch always holds SOME color -- so the toggle says whether there is a
// dot and the swatch says what color it is, dead until the toggle is on. The
// alternative was to read a transparent swatch as "no dot", which would have
// meant picking a hue did nothing until the alpha was raised as well.
//
// The row owns its widgets and no values. It speaks in the same TAB-separated
// records GuiListBoxCtrl::getItemList writes, so the block above it can join
// what its rows say and hand the lot back without translating anything.
//
// The creator sets owner inline and calls build() once AFTER adding the row to
// its cell, because the cell is what decides how wide it is. Everything it does
// reports to owner:
//
//     onItemRowTyped     a caption keystroke - live, and not yet an undo step
//     onItemRowCommit    a box lost focus or took Enter
//     onItemRowToggled   a switch flipped
//     onItemRowMove      the up or down arrow, with -1 or 1
//     onItemRowRemove    the bin
//-----------------------------------------------------------------------------

function GuiEditorItemRow::onAdd(%this)
{
	ThemeManager.setProfile(%this, "emptyProfile");
}

function GuiEditorItemRow::build(%this)
{
	// The width the row actually has, not the one it was created with: the grid
	// sizes a cell the moment it is added, which is before build() runs, so
	// laying out against the nominal width would place every widget for a
	// 338-wide row inside a cell the grid had already made narrower - and the
	// icons at the right-hand end would sit off the edge of the pane. Sizing
	// flags take it from here.
	%w = getWord(%this.getExtent(), 0);
	%pad = 4;
	%gap = 2;
	%iconW = 24;
	%swatchW = 26;
	%idW = 34;
	%boxH = 22;

	// Everything but the caption is a fixed size, so the group is measured once
	// and the caption takes what is left. They all carry "left" sizing, which
	// keeps them against the right edge as the Properties frame is dragged wider
	// and gives the caption the slack.
	%groupW = %idW + %swatchW + (%iconW * 6) + (%gap * 7);
	%groupX = %w - %pad - %groupW;
	%captionW = %groupX - %pad - %gap;

	%this.setExtent(%w, 26);

	%this.captionBox = new GuiTextEditCtrl()
	{
		HorizSizing = "width";
		Position = %pad SPC 2;
		Extent = %captionW SPC %boxH;
		Tooltip = "What this row says.";
	};
	ThemeManager.setProfile(%this.captionBox, "textEditProfile");
	ThemeManager.setProfile(%this.captionBox, "tipProfile", "TooltipProfile");
	// Command is per keystroke - GuiTextEditCtrl runs it on every edit to its
	// buffer - which is what makes the row appear on the canvas as it is typed.
	// AltCommand is the blur and ReturnCommand the Enter, and those are the two
	// that make an undo step.
	%this.captionBox.Command = %this.getID() @ ".onCaptionTyped();";
	%this.captionBox.AltCommand = %this.getID() @ ".onCommit();";
	%this.captionBox.ReturnCommand = %this.getID() @ ".onCommit();";
	%this.add(%this.captionBox);

	%x = %groupX;

	%this.idBox = new GuiTextEditCtrl()
	{
		HorizSizing = "left";
		Position = %x SPC 2;
		Extent = %idW SPC %boxH;
		align = "center";
		inputMode = "Number";
		Tooltip = "A number script can find this row by, with findItemID. Rows that nothing looks up can all be left at zero.";
	};
	ThemeManager.setProfile(%this.idBox, "textEditProfile");
	ThemeManager.setProfile(%this.idBox, "tipProfile", "TooltipProfile");
	%this.idBox.AltCommand = %this.getID() @ ".onCommit();";
	%this.idBox.ReturnCommand = %this.getID() @ ".onCommit();";
	%this.add(%this.idBox);
	%x += %idW + %gap;

	%this.colorToggle = %this.makeToggle(%x, "color", "Show color",
		"", $EditorIcon::brush,
		"Draws a colored dot in front of the caption, and moves the caption over to make room for it.",
		"No dot. The caption starts at the edge of the row.");
	%x += %iconW + %gap;

	%this.swatch = new GuiColorPopupCtrl()
	{
		class = "GuiProfileEditorColorPopup";
		HorizSizing = "left";
		Position = %x SPC 2;
		Extent = %swatchW SPC %boxH;
		showColorValues = true;
		Tooltip = "The color of the dot.";
	};
	ThemeManager.setProfile(%this.swatch, "colorPickerProfile");
	ThemeManager.setProfile(%this.swatch, "emptyProfile", "backgroundProfile");
	ThemeManager.setProfile(%this.swatch, "colorPopupProfile", "popupProfile");
	ThemeManager.setProfile(%this.swatch, "emptyProfile", "pickerProfile");
	ThemeManager.setProfile(%this.swatch, "colorPickerSelectorProfile", "selectorProfile");
	ThemeManager.setProfile(%this.swatch, "textEditProfile", "valueProfile");
	ThemeManager.setProfile(%this.swatch, "tipProfile", "TooltipProfile");
	%this.swatch.Command = %this.getID() @ ".onCommit();";
	%this.add(%this.swatch);
	%x += %swatchW + %gap;

	%this.activeToggle = %this.makeToggle(%x, "active", "Active",
		$EditorIcon::on, $EditorIcon::off,
		"Can be picked when the game runs.",
		"Drawn greyed out and cannot be picked. Use it for a choice that is there but not available yet.");
	%x += %iconW + %gap;

	%this.selectedToggle = %this.makeToggle(%x, "selected", "Starts selected",
		$EditorIcon::round_checkmark, $EditorIcon::round,
		"Already picked when the Gui loads. A drop down shows this row instead of its placeholder text.",
		"Not picked when the Gui loads.");
	%x += %iconW + %gap;

	%this.upButton = %this.makeIconButton(%x, $EditorIcon::sq_up,
		"Move this row up.", ".onMoveUp();");
	%x += %iconW + %gap;

	%this.downButton = %this.makeIconButton(%x, $EditorIcon::sq_down,
		"Move this row down.", ".onMoveDown();");
	%x += %iconW + %gap;

	%this.removeButton = %this.makeIconButton(%x, $EditorIcon::trash,
		"Remove this row.", ".onRemoveClicked();");
}

// A checkbox wearing an icon, the same EditorToggleIcon the header's flags
// use. frameOn is optional: where there is one picture for the idea, the tint
// alone carries the state.
function GuiEditorItemRow::makeToggle(%this, %x, %name, %label, %frameOn, %frameOff, %tipOn, %tipOff)
{
	%toggle = new GuiCheckBoxCtrl()
	{
		class = "EditorToggleIcon";
		HorizSizing = "left";
		Position = %x SPC 1;
		Extent = "24 24";
		frameOn = %frameOn;
		frameOff = %frameOff;
		tipOn = %tipOn;
		tipOff = %tipOff;
		toggleName = %name;
		toggleLabel = %label;
		owner = %this;
	};
	ThemeManager.setProfile(%toggle, "iconButtonProfile");
	ThemeManager.setProfile(%toggle, "tipProfile", "TooltipProfile");
	%this.add(%toggle);

	return %toggle;
}

function GuiEditorItemRow::makeIconButton(%this, %x, %frame, %tip, %command)
{
	%button = new GuiButtonCtrl()
	{
		class = "EditorIconButton";
		HorizSizing = "left";
		Frame = %frame;
		Position = %x SPC 1;
		Extent = "24 24";
		Tooltip = %tip;
		Command = %this.getID() @ %command;
	};
	ThemeManager.setProfile(%button, "iconButtonProfile");
	%this.add(%button);

	return %button;
}

//-----------------------------------------------------------------------------
// Values, in the records GuiListBoxCtrl::getItemList speaks:
//
//     text  ID  active  selected  hasColor  "r g b a"
//-----------------------------------------------------------------------------

function GuiEditorItemRow::setRecord(%this, %record)
{
	%this.populating = true;

	%this.captionBox.setText(getField(%record, 0));
	%this.idBox.setText(getField(%record, 1) + 0);
	%this.activeToggle.setValue(getField(%record, 2));
	%this.selectedToggle.setValue(getField(%record, 3));

	%hasColor = getField(%record, 4);
	%this.colorToggle.setValue(%hasColor);

	// A row showing no dot still needs something in the swatch, or turning the
	// toggle on would give it whatever was there before. The profile's font color
	// is the useful default: it is the one color the list is already known to be
	// legible in, so the first dot lands visible rather than black on black.
	%color = getField(%record, 5);
	if(!%hasColor || getWordCount(%color) < 4)
	{
		%color = %this.defaultBulletColor();
	}
	%this.swatch.setColorF(%color);

	%this.populating = false;
	%this.refreshColorState();

	%this.lastRecord = %this.getRecord();
}

function GuiEditorItemRow::getRecord(%this)
{
	%hasColor = %this.colorToggle.getValue() ? 1 : 0;

	return %this.captionBox.getText() TAB
		(%this.idBox.getText() + 0) TAB
		(%this.activeToggle.getValue() ? 1 : 0) TAB
		(%this.selectedToggle.getValue() ? 1 : 0) TAB
		%hasColor TAB
		%this.swatch.getColorF();
}

function GuiEditorItemRow::hasChanged(%this)
{
	return strcmp(%this.getRecord(), %this.lastRecord) != 0;
}

function GuiEditorItemRow::markClean(%this)
{
	%this.lastRecord = %this.getRecord();
}

// What a dot starts as before anyone picks a color for it: the color the list
// draws its captions in, which is the one color the profile guarantees reads
// against its own background.
function GuiEditorItemRow::defaultBulletColor(%this)
{
	%ctrl = isObject(%this.owner) ? %this.owner.target : "";
	if(isObject(%ctrl))
	{
		%profile = %ctrl.getFieldValue("Profile");
		if(isObject(%profile))
		{
			return %profile.fontColor;
		}
	}

	return "1 1 1 1";
}

// The swatch means nothing while there is no dot to color, so it says so rather
// than sitting there looking editable.
function GuiEditorItemRow::refreshColorState(%this)
{
	%this.swatch.setActive(%this.colorToggle.getValue());
}

function GuiEditorItemRow::setCaretHere(%this)
{
	%this.captionBox.setFirstResponder();
}

//-----------------------------------------------------------------------------
// Reporting. Nothing here writes to the control; the block owns every write, so
// it stays the only thing that knows the list as a whole.
//-----------------------------------------------------------------------------

function GuiEditorItemRow::onCaptionTyped(%this)
{
	if(%this.populating || !isObject(%this.owner))
	{
		return;
	}

	%this.owner.onItemRowTyped(%this);
}

function GuiEditorItemRow::onCommit(%this)
{
	if(%this.populating || !isObject(%this.owner))
	{
		return;
	}

	%this.owner.onItemRowCommit(%this);
}

function GuiEditorItemRow::onToggleIconChanged(%this, %toggle)
{
	if(%this.populating || !isObject(%this.owner))
	{
		return;
	}

	if(%toggle.toggleName $= "color")
	{
		%this.refreshColorState();
	}

	%this.owner.onItemRowToggled(%this, %toggle.toggleName);
}

function GuiEditorItemRow::onMoveUp(%this)
{
	if(isObject(%this.owner))
	{
		%this.owner.onItemRowMove(%this, -1);
	}
}

function GuiEditorItemRow::onMoveDown(%this)
{
	if(isObject(%this.owner))
	{
		%this.owner.onItemRowMove(%this, 1);
	}
}

function GuiEditorItemRow::onRemoveClicked(%this)
{
	if(isObject(%this.owner))
	{
		%this.owner.onItemRowRemove(%this);
	}
}
