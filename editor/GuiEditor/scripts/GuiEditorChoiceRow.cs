
//-----------------------------------------------------------------------------
// A row of icon buttons where exactly one is chosen: a radio group that looks
// like a segmented control. Built for the properties pane's two alignment rows,
// where a drop-down was a poor fit -- there are only three or four values, they
// each have a picture, and seeing which one is set matters more than reading
// its name.
//
// The buttons are GuiEditorToggleIcon, the same checkbox-as-button the header
// and the anchor picker use, so a choice looks pressed and a disabled row
// refuses to act. Exclusivity is this row's job rather than the button's: a
// click turns the others off, and clicking the chosen one again does nothing --
// unlike a toggle, a radio cannot be un-picked, only replaced.
//
// The first entry is conventionally the "unset" one. For alignment that is
// GuiControl's "default", which getAlignmentType resolves to the profile's own
// alignment -- so it is a real value meaning "inherit", not an absence, and it
// gets a button with no icon rather than no button.
//
// The creator sets owner, fieldName and blockWidth inline, calls addChoice()
// once per value, then build(). Changes arrive at
// owner.onChoiceRowChanged(%row).
//-----------------------------------------------------------------------------

function GuiEditorChoiceRow::onAdd(%this)
{
	ThemeManager.setProfile(%this, "emptyProfile");
	%this.choiceCount = 0;
	%this.value = "";
}

// %icon may be "" for the entry that means "unset" -- GuiEditorToggleIcon draws
// nothing when its frame is empty, leaving a plain button.
function GuiEditorChoiceRow::addChoice(%this, %value, %icon, %tip)
{
	%i = %this.choiceCount;
	%this.choiceValue[%i] = %value;
	%this.choiceIcon[%i] = %icon;
	%this.choiceTip[%i] = %tip;
	%this.choiceCount = %i + 1;
}

function GuiEditorChoiceRow::build(%this)
{
	// Wide enough for a caption by default. The text block asks for a narrow one:
	// its two rows sit side by side under the text box and are labelled "H:" and
	// "V:", where the icons say the rest.
	%labelW = (%this.labelWidth $= "") ? 68 : %this.labelWidth;
	%size = 24;
	%gap = 2;

	%this.setExtent(%labelW + ((%size + %gap) * %this.choiceCount), %size + 4);

	%this.label = new GuiControl()
	{
		Position = "0 2";
		Extent = (%labelW - 4) SPC %size;
		Text = %this.labelText;
		align = "left";
		vAlign = "middle";
	};
	ThemeManager.setProfile(%this.label, "labelProfile");
	%this.add(%this.label);

	for(%i = 0; %i < %this.choiceCount; %i++)
	{
		%button = new GuiCheckBoxCtrl()
		{
			class = "GuiEditorToggleIcon";
			Position = (%labelW + (%i * (%size + %gap))) SPC 2;
			Extent = %size SPC %size;
			frameOn = %this.choiceIcon[%i];
			frameOff = %this.choiceIcon[%i];
			tipOn = %this.choiceTip[%i];
			tipOff = %this.choiceTip[%i];
			toggleName = %this.choiceValue[%i];
			owner = %this;
		};
		ThemeManager.setProfile(%button, "iconButtonProfile");
		ThemeManager.setProfile(%button, "tipProfile", "TooltipProfile");
		%this.add(%button);
		%this.choiceButton[%i] = %button;
	}
}

// Hide a choice that does not apply to whatever is bound. The buttons are placed
// absolutely, so this leaves a gap where the choice was rather than reflowing the
// row -- which is the right trade for a choice that comes and goes with the
// selection: the ones that stay do not move under the cursor.
function GuiEditorChoiceRow::setChoiceVisible(%this, %value, %visible)
{
	for(%i = 0; %i < %this.choiceCount; %i++)
	{
		if(%this.choiceValue[%i] $= %value)
		{
			%this.choiceButton[%i].setVisible(%visible);
			return;
		}
	}
}

//-----------------------------------------------------------------------------
// Value.
//-----------------------------------------------------------------------------

// Load a value without telling the owner. A value the row does not offer leaves
// every button up rather than guessing, so nothing is silently rewritten.
function GuiEditorChoiceRow::setValue(%this, %value)
{
	%this.value = %value;
	%this.populating = true;
	for(%i = 0; %i < %this.choiceCount; %i++)
	{
		%this.choiceButton[%i].setValue(%this.choiceValue[%i] $= %value);
	}
	%this.populating = false;
}

function GuiEditorChoiceRow::getValue(%this)
{
	return %this.value;
}

// A button toggled itself. Whatever it did to its own state, the row's rule is
// that exactly one is on -- so re-assert that from the value rather than from
// what the button happens to hold.
function GuiEditorChoiceRow::onToggleIconChanged(%this, %button)
{
	if(%this.populating)
	{
		return;
	}

	%chosen = %button.toggleName;
	if(%chosen $= %this.value)
	{
		// Clicking the current choice cannot clear it; put the button back down.
		%this.setValue(%chosen);
		return;
	}

	%this.setValue(%chosen);

	if(isObject(%this.owner))
	{
		%this.owner.onChoiceRowChanged(%this);
	}
}

function GuiEditorChoiceRow::setEnabled(%this, %enabled)
{
	%this.label.setActive(%enabled);
	for(%i = 0; %i < %this.choiceCount; %i++)
	{
		%this.choiceButton[%i].setActive(%enabled);
	}
}
