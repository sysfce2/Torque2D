
//-----------------------------------------------------------------------------
// A reusable editor for the sixteen values that matter for a GuiBorderProfile:
// margin / border / border-color / padding, each across the four control states
// normal / HL / SL / NA -- plus the border's underfill flag. Laid out as four
// labelled rows of four inputs (numeric boxes, or color-popup swatches for the
// color row) with a state caption under each column, and an Underfill checkbox
// below the grid.
//
// The grid never decides what it edits or where edits go: the host sets .target
// (the GuiBorderProfile to read/write) and .owner (notified after every commit
// via owner.onBorderGridCommit()). The same grid therefore serves both the
// Borders pane's "Custom..." slot (editing a hidden single-use border) and the
// Border pane (editing a theme's named border in place).
//
// The creator sets these fields inline: gridWidth (the content width to lay out
// within) and owner. Call build() once after adding the grid to a container,
// then bind()/unbind() to attach a border. build() records the laid-out height
// in .gridHeight so the host can size its container to fit.
//-----------------------------------------------------------------------------

function GuiProfileEditorBorderGrid::onAdd(%this)
{
	ThemeManager.setProfile(%this, "emptyProfile");
}

// The four border-profile field state suffixes (box 0 is the normal state,
// drawn with no caption).
function GuiProfileEditorBorderGrid::stateSuffix(%this, %i)
{
	return getWord("_ HL SL NA", %i);
}

function GuiProfileEditorBorderGrid::stateField(%this, %field, %i)
{
	return (%i == 0) ? %field : (%field @ %this.stateSuffix(%i));
}

// Build the four property blocks (label, four inputs, four captions) and the
// underfill checkbox beneath them.
function GuiProfileEditorBorderGrid::build(%this)
{
	%w = %this.gridWidth;
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
		%this.add(%rowLabel);

		for(%i = 0; %i < 4; %i++)
		{
			%bx = %x0 + %i * (%boxW + %boxGap);
			%by = %y + 18;

			if(%isColor)
			{
				%box = new GuiColorPopupCtrl()
				{
					class = "GuiProfileEditorColorPopup";
					Position = %bx SPC %by;
					Extent = %boxW SPC 22;
					showColorValues = true;
				};
				ThemeManager.setProfile(%box, "colorPickerProfile");
				ThemeManager.setProfile(%box, "emptyProfile", "backgroundProfile");
				ThemeManager.setProfile(%box, "colorPopupProfile", "popupProfile");
				ThemeManager.setProfile(%box, "emptyProfile", "pickerProfile");
				ThemeManager.setProfile(%box, "colorPickerSelectorProfile", "selectorProfile");
				ThemeManager.setProfile(%box, "textEditProfile", "valueProfile");
				ThemeManager.setProfile(%box, "tipProfile", "TooltipProfile");
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
			%this.add(%box);
			%this.box[%field, %i] = %box;

			%cap = new GuiControl()
			{
				Position = %bx SPC (%by + 24);
				Extent = %boxW SPC 14;
				Text = (%i == 0) ? "" : %this.stateSuffix(%i);
				align = "center";
			};
			ThemeManager.setProfile(%cap, "labelProfile");
			%this.add(%cap);
		}
	}

	// Underfill: whether the border's fill is drawn beneath the control's
	// content. A single checkbox sits under the grid. The inner box is sized
	// square explicitly and the row is tall enough that onRender's fit-to-content
	// clamp never shrinks it (the default box, at offset y=7 + 16 high, gets
	// clamped vertically in a short row and renders as a rectangle).
	%uy = %count * %blockH + 2;
	%cbW = %w - %x0 - 4;
	%this.underfillBox = new GuiCheckBoxCtrl()
	{
		Position = %x0 SPC %uy;
		Extent = %cbW SPC 26;
		Text = "Underfill";
		boxOffset = "0 4";
		boxExtent = "18 18";
		textOffset = "26 4";
		textExtent = (%cbW - 26) SPC 18;
		Command = %this.getID() @ ".commitUnderfill();";
	};
	ThemeManager.setProfile(%this.underfillBox, "checkboxProfile");
	%this.add(%this.underfillBox);

	%this.gridHeight = %uy + 32;
	%this.setExtent(%w, %this.gridHeight);
}

//-----------------------------------------------------------------------------
// Binding.
//-----------------------------------------------------------------------------

function GuiProfileEditorBorderGrid::bind(%this, %border)
{
	%this.target = %border;
	%this.refresh();
}

function GuiProfileEditorBorderGrid::unbind(%this)
{
	%this.target = "";
}

// Load the bound border's values into the boxes. The populating guard keeps the
// setText/setColorI/setStateOn calls from echoing back through the commits.
function GuiProfileEditorBorderGrid::refresh(%this)
{
	if(!isObject(%this.target))
	{
		return;
	}
	%this.populating = true;
	%numFields = "margin" TAB "border" TAB "padding";
	for(%f = 0; %f < 3; %f++)
	{
		%field = getField(%numFields, %f);
		for(%i = 0; %i < 4; %i++)
		{
			%this.box[%field, %i].setText(%this.target.getFieldValue(%this.stateField(%field, %i)));
		}
	}
	for(%i = 0; %i < 4; %i++)
	{
		%this.box["borderColor", %i].setColorI(%this.target.getFieldValue(%this.stateField("borderColor", %i)));
	}
	%this.underfillBox.setStateOn(%this.target.underfill);
	%this.populating = false;
}

// Copy all sixteen values plus underfill from one border to another.
function GuiProfileEditorBorderGrid::copyValues(%this, %from, %to)
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
	%to.underfill = %from.underfill;
}

//-----------------------------------------------------------------------------
// Commit.
//-----------------------------------------------------------------------------

// A single input box committed a value into the bound border.
function GuiProfileEditorBorderGrid::commitBox(%this, %box)
{
	if(%this.populating || !isObject(%this.target))
	{
		return;
	}
	%name = %this.stateField(%box.borderField, %box.stateIndex);
	%value = %box.isColor ? %box.getColorI() : %box.getText();
	%this.target.setFieldValue(%name, %value);
	%this.notifyCommit();
}

function GuiProfileEditorBorderGrid::commitUnderfill(%this)
{
	if(%this.populating || !isObject(%this.target))
	{
		return;
	}
	%this.target.underfill = %this.underfillBox.getStateOn();
	%this.notifyCommit();
}

// Every commit routes through here so the host can react (mark dirty, refresh
// the preview) without the grid knowing whether it edits a hidden copy or a
// named border in place.
function GuiProfileEditorBorderGrid::notifyCommit(%this)
{
	if(isObject(%this.owner))
	{
		%this.owner.onBorderGridCommit();
	}
}

//-----------------------------------------------------------------------------
// The numeric input boxes: up/down arrows nudge by 1. Clicking places the caret,
// as it does in every other box in the editor - see the note in
// GuiProfileEditorFieldRow for why nothing re-selects here.
//-----------------------------------------------------------------------------

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
