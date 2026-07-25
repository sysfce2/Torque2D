
//-----------------------------------------------------------------------------
// A profile's fill and text colors are each one value per control state, so
// each set gets a single grid cell holding four swatches with the state
// captions beneath -- the same shape GuiProfileEditorBorderGrid gives the
// border values, so the two panes read alike. Two of these replace eight
// full-width inspector rows.
//
// Field names follow the engine's own convention: the base name is the normal
// state and HL / SL / NA are suffixes, which holds for both fillColor* and
// fontColor*, so one row class serves both.
//
// A state the bound control never renders in is greyed rather than dropped, so
// its value survives; the pane's Show All puts every state back in reach.
//
// Overrides are per state, and the caption under each swatch turns the theme's
// override color to show it. Resetting is per row rather than per state: a
// caption is a label, and making it a button would give it a control's hover
// behaviour. The single reset button clears whichever of the four states are
// overridden, which is per-state in every case but the rare one where a user
// overrode two states of the same row and wants only one back.
//
// The creator sets fieldBase, labelText and owner inline; call build() once
// after adding the row to its container, which decides the cell width. It
// records .rowHeight. The row never touches the profile -- commits go to
// owner.onProfileStateColorCommit(%row, %stateIndex) and resets to
// owner.onProfileStateColorReset(%row).
//-----------------------------------------------------------------------------

function GuiProfileEditorStateColorRow::onAdd(%this)
{
	ThemeManager.setProfile(%this, "emptyProfile");
}

// Suffix per control state; index 0 is the normal state, drawn with no caption.
function GuiProfileEditorStateColorRow::stateSuffix(%this, %index)
{
	return getWord("_ HL SL NA", %index);
}

function GuiProfileEditorStateColorRow::stateField(%this, %index)
{
	return (%index == 0) ? %this.fieldBase : (%this.fieldBase @ %this.stateSuffix(%index));
}

function GuiProfileEditorStateColorRow::build(%this)
{
	// The grid has already sized this cell by the time build() runs, so lay out
	// against the width we actually have. See GuiProfileEditorFieldRow::build.
	%w = getWord(%this.getExtent(), 0);
	%pad = 4;
	%resetW = 24;
	%gap = 4;
	%swatchW = 42;
	%labelH = 16;
	%swatchY = %labelH + 4;
	%captionY = %swatchY + 24;
	%h = %captionY + 14 + 4;

	%this.rowHeight = %h;
	%this.setExtent(%w, %h);

	// The caption sits above the swatches, matching the plain field cells, so a
	// grid can flow the two kinds of cell together in one column.
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

	for(%i = 0; %i < 4; %i++)
	{
		%x = %pad + %i * (%swatchW + %gap);

		// Swatches keep their size as the cell widens -- four fixed chips read
		// as a state row, where four stretched bars would not.
		%swatch = new GuiColorPopupCtrl()
		{
			Position = %x SPC %swatchY;
			Extent = %swatchW SPC 22;
		};
		ThemeManager.setProfile(%swatch, "colorPickerProfile");
		ThemeManager.setProfile(%swatch, "emptyProfile", "backgroundProfile");
		ThemeManager.setProfile(%swatch, "colorPopupProfile", "popupProfile");
		ThemeManager.setProfile(%swatch, "emptyProfile", "pickerProfile");
		ThemeManager.setProfile(%swatch, "colorPickerSelectorProfile", "selectorProfile");
		%swatch.Command = %this.getID() @ ".commitState(" @ %i @ ");";
		%this.add(%swatch);
		%this.swatch[%i] = %swatch;

		%caption = new GuiControl()
		{
			Position = %x SPC %captionY;
			Extent = %swatchW SPC 14;
			Text = (%i == 0) ? "" : %this.stateSuffix(%i);
			align = "center";
		};
		ThemeManager.setProfile(%caption, "labelProfile");
		%this.add(%caption);
		%this.caption[%i] = %caption;
	}

	// Frame 22 of EditorCore:editorIcons16 is the circular revert arrow. "left"
	// sizing pins the button to the cell's right edge as the grid widens.
	%this.resetButton = new GuiButtonCtrl()
	{
		class = "EditorIconButton";
		Frame = 22;
		HorizSizing = "left";
		Position = (%w - %resetW - %pad) SPC (%swatchY - 1);
		Tooltip = "Reset this row's overridden states to the theme's values";
		Command = %this.getID() @ ".onResetClicked();";
		Visible = false;
	};
	ThemeManager.setProfile(%this.resetButton, "iconButtonProfile");
	%this.add(%this.resetButton);
}

//-----------------------------------------------------------------------------
// Values.
//-----------------------------------------------------------------------------

// Loading a swatch also records what it ended up holding, so a later commit can
// tell an actual edit from a no-op. The recorded form is what the swatch reads
// back: a ColorI field holding "White" comes back as "255 255 255 255".
function GuiProfileEditorStateColorRow::setValue(%this, %index, %value)
{
	// setColorI wants four integers, but a ColorI field holding a stock color
	// comes back as a single name token; baseColor parses those.
	if(getWordCount(%value) >= 4)
	{
		%this.swatch[%index].setColorI(%value);
	}
	else
	{
		%this.swatch[%index].baseColor = %value;
	}
	%this.lastValue[%index] = %this.getValue(%index);
}

function GuiProfileEditorStateColorRow::hasChanged(%this, %index)
{
	return %this.getValue(%index) !$= %this.lastValue[%index];
}

function GuiProfileEditorStateColorRow::markClean(%this, %index)
{
	%this.lastValue[%index] = %this.getValue(%index);
}

function GuiProfileEditorStateColorRow::getValue(%this, %index)
{
	return %this.swatch[%index].getColorI();
}

//-----------------------------------------------------------------------------
// Filtering and the override markers.
//-----------------------------------------------------------------------------

function GuiProfileEditorStateColorRow::setStateEnabled(%this, %index, %enabled, %reason)
{
	%this.swatch[%index].setActive(%enabled);
	%this.swatch[%index].Tooltip = %enabled ? "" : %reason;
}

function GuiProfileEditorStateColorRow::setStateOverridden(%this, %index, %overridden)
{
	ThemeManager.setProfile(%this.caption[%index], %overridden ? "overrideLabelProfile" : "labelProfile");
	%this.overridden[%index] = %overridden;
}

// Called after all four states have been marked, so the row's single reset
// button appears exactly when there is something to reset.
function GuiProfileEditorStateColorRow::refreshResetButton(%this)
{
	%any = false;
	for(%i = 0; %i < 4; %i++)
	{
		if(%this.overridden[%i])
		{
			%any = true;
		}
	}
	%this.resetButton.setVisible(%any);
}

//-----------------------------------------------------------------------------
// Commit.
//-----------------------------------------------------------------------------

function GuiProfileEditorStateColorRow::commitState(%this, %index)
{
	if(!isObject(%this.owner) || %this.owner.populating)
	{
		return;
	}
	%this.owner.onProfileStateColorCommit(%this, %index);
}

function GuiProfileEditorStateColorRow::onResetClicked(%this)
{
	if(isObject(%this.owner))
	{
		%this.owner.onProfileStateColorReset(%this);
	}
}
