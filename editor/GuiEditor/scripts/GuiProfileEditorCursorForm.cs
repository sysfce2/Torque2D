
//-----------------------------------------------------------------------------
// The cursor pane: the fourth form sharing the Gui Profile Editor's Properties
// window, shown when a cursor node is selected.
//
// Most of it is the ordinary field-row machinery the profile pane uses. What is
// not is the hot spot, which cannot be set by typing numbers: it is one pixel
// in a 13x17 image, and whether it is the right pixel is a question about what
// the art looks like. So the pane is built around a GuiEditorCursorCtrl showing
// the art magnified with the hot spot marked and draggable, and the fields
// underneath report what the dragging did.
//
// Two fields decide where the pointer lands and both are shown, because they do
// different jobs:
//
//   Anchor (renderOffset)  a fraction of the art's own size, so "0.5 0.5" is
//                          the middle whatever the art measures. This is what
//                          stops a small pointer and a large sizer from
//                          appearing to leap when one replaces the other, and
//                          it is why the anchor is set from a 3x3 of presets
//                          rather than typed.
//   Nudge (hotSpot)        pixels on top of that. This is what dragging writes.
//
// The creator sets the dialog back-pointer and an initial Extent inline, then
// calls build() once after adding it to its scroller; bind()/unbind() attach a
// cursor.
//-----------------------------------------------------------------------------

// The Marker and Tint buttons are the same size as each other on purpose: they
// do the same kind of job, one on the cursor and one on the editor's own
// marker, and matching them is what says so.
$CursorForm::SwatchWidth = 66;
$CursorForm::SwatchExtent = "66 22";

function GuiProfileEditorCursorForm::onAdd(%this)
{
	ThemeManager.setProfile(%this, "emptyProfile");
	%this.rowWidth = 200;
}

function GuiProfileEditorCursorForm::build(%this)
{
	%w = %this.formWidth;

	%this.nameLabel = new GuiControl()
	{
		HorizSizing = "width";
		Position = "0 0";
		Extent = %w SPC 24;
		Text = "Cursor:";
		align = "left";
		vAlign = "middle";
	};
	ThemeManager.setProfile(%this.nameLabel, "labelProfile");
	%this.add(%this.nameLabel);

	%this.buildEditor();
	%this.buildAnchor();

	%grid = %this.makeCellGrid();
	%this.add(%grid);
	%this.addFieldRow(%grid, "bitmapName", "Art", "file");
	%this.addFieldRow(%grid, "color", "Tint", "color", $CursorForm::SwatchWidth);
	%this.addFieldRow(%grid, "hotSpot", "Nudge (pixels)", "point");
	%this.addFieldRow(%grid, "renderOffset", "Anchor (fraction)", "pointf");

	// Same forced layout pass the profile form needs: a GuiChainCtrl positions
	// its children without resizing them, so nothing would otherwise tell the
	// grid how wide it is.
	%h = getWord(%this.getExtent(), 1);
	%this.resize(0, 0, %w + 1, %h);
	%this.resize(0, 0, %w, %h);
}

// The magnifier, and the zoom controls that belong to it.
function GuiProfileEditorCursorForm::buildEditor(%this)
{
	%w = %this.formWidth;

	// Tall enough that the small cursors reach the full 16x: the stock pointer is
	// 17 pixels high, so it needs 272 for the art plus what the profile's border
	// and padding take off the content rect. The big ones (a 32x32 move cursor
	// would want 512) still cap lower, which is what the greyed "+" is for.
	%viewHeight = 304;

	%this.editorBox = new GuiControl()
	{
		HorizSizing = "width";
		Position = "0 0";
		Extent = %w SPC (%viewHeight + 60);
	};
	ThemeManager.setProfile(%this.editorBox, "emptyProfile");
	%this.add(%this.editorBox);

	%this.editor = new GuiEditorCursorCtrl()
	{
		class = "GuiProfileEditorCursorView";
		HorizSizing = "width";
		VertSizing = "height";
		Position = "0 0";
		Extent = %w SPC %viewHeight;
		zoom = 8;
		owner = %this;
	};
	ThemeManager.setProfile(%this.editor, "displayBoxProfile");
	%this.editorBox.add(%this.editor);

	// The readout names the pixel the pointer really lands on, which is neither
	// field on its own -- so without it the two numbers below would look like
	// they disagreed with the dot.
	%rowY = %viewHeight + 6;

	%this.readout = new GuiControl()
	{
		HorizSizing = "width";
		Position = "0" SPC %rowY;
		Extent = (%w - 96) SPC 22;
		Text = "";
		align = "left";
		vAlign = "middle";
	};
	ThemeManager.setProfile(%this.readout, "labelProfile");
	%this.editorBox.add(%this.readout);

	%this.zoomOut = %this.makeZoomButton(%w - 92, %rowY, "-", "Zoom out");
	%this.zoomLabel = new GuiControl()
	{
		HorizSizing = "left";
		Position = (%w - 62) SPC %rowY;
		Extent = "34 22";
		Text = "8x";
		align = "center";
		vAlign = "middle";
	};
	ThemeManager.setProfile(%this.zoomLabel, "labelProfile");
	%this.editorBox.add(%this.zoomLabel);
	%this.zoomIn = %this.makeZoomButton(%w - 26, %rowY, "+", "Zoom in");

	%this.dotRow = new GuiControl()
	{
		HorizSizing = "width";
		Position = "0" SPC (%rowY + 26);
		Extent = %w SPC 24;
	};
	ThemeManager.setProfile(%this.dotRow, "emptyProfile");
	%this.editorBox.add(%this.dotRow);

	%dotLabel = new GuiControl()
	{
		Position = "0 0";
		Extent = "80 22";
		Text = "Marker";
		align = "left";
		vAlign = "middle";
	};
	ThemeManager.setProfile(%dotLabel, "labelProfile");
	%this.dotRow.add(%dotLabel);

	// The hot-spot marker's own color, because a dark dot vanishes on dark art
	// and a light one on light art. It belongs to the editor, not the cursor, so
	// it is never written to the theme.
	//
	// A button, not a bar: left-aligned at a fixed width rather than filling the
	// row, because a full-width band of flat color reads as a progress bar.
	%this.dotSwatch = new GuiColorPopupCtrl()
	{
		class = "GuiProfileEditorColorPopup";
		Position = "84 0";
		Extent = $CursorForm::SwatchExtent;
		showColorValues = false;
	};
	ThemeManager.setProfile(%this.dotSwatch, "colorPickerProfile");
	ThemeManager.setProfile(%this.dotSwatch, "emptyProfile", "backgroundProfile");
	ThemeManager.setProfile(%this.dotSwatch, "colorPopupProfile", "popupProfile");
	ThemeManager.setProfile(%this.dotSwatch, "emptyProfile", "pickerProfile");
	ThemeManager.setProfile(%this.dotSwatch, "colorPickerSelectorProfile", "selectorProfile");
	ThemeManager.setProfile(%this.dotSwatch, "textEditProfile", "valueProfile");
	%this.dotSwatch.Command = %this.getID() @ ".onDotColorChanged();";
	%this.dotRow.add(%this.dotSwatch);
	%this.dotSwatch.setColorI(%this.editor.dotColor);
}

function GuiProfileEditorCursorForm::makeZoomButton(%this, %x, %y, %text, %tip)
{
	%button = new GuiButtonCtrl()
	{
		HorizSizing = "left";
		Position = %x SPC %y;
		Extent = "26 22";
		Text = %text;
		tooltip = %tip;
		Command = %this.getID() @ ".onZoom(" @ (%text $= "+" ? 1 : -1) @ ");";
	};
	ThemeManager.setProfile(%button, "buttonProfile");
	ThemeManager.setProfile(%button, "tipProfile", "TooltipProfile");
	%this.editorBox.add(%button);
	return %button;
}

// Nine presets for the anchor, laid out as the thing they mean: where in the
// art the pointer sits. Typing "0.5 0.5" is the same edit, but nobody reads a
// pair of decimals as "the middle" at a glance.
function GuiProfileEditorCursorForm::buildAnchor(%this)
{
	%w = %this.formWidth;

	// Tall enough for four wrapped lines beside the pin cluster. The cluster
	// itself only needs 82, but a hint that clips mid-sentence is worse than
	// none, and the pane scrolls.
	%this.anchorBox = new GuiControl()
	{
		HorizSizing = "width";
		Position = "0 0";
		Extent = %w SPC 104;
	};
	ThemeManager.setProfile(%this.anchorBox, "emptyProfile");
	%this.add(%this.anchorBox);

	%label = new GuiControl()
	{
		Position = "0 0";
		Extent = "200 20";
		Text = "Anchor";
		align = "left";
		vAlign = "middle";
	};
	ThemeManager.setProfile(%label, "labelProfile");
	%this.anchorBox.add(%label);

	%fractions = "0" TAB "0.5" TAB "1";
	for(%row = 0; %row < 3; %row++)
	{
		for(%col = 0; %col < 3; %col++)
		{
			%x = getField(%fractions, %col);
			%y = getField(%fractions, %row);

			%pin = new GuiButtonCtrl()
			{
				Position = (%col * 20) SPC (22 + (%row * 20));
				Extent = "18 18";
				Text = "";
				tooltip = "Anchor at" SPC %x SPC %y;
				Command = %this.getID() @ ".onAnchorPreset(" @ %x @ "," SPC %y @ ");";
			};
			ThemeManager.setProfile(%pin, "buttonProfile");
			ThemeManager.setProfile(%pin, "tipProfile", "TooltipProfile");
			%this.anchorBox.add(%pin);
		}
	}

	%hint = new GuiControl()
	{
		HorizSizing = "width";
		Position = "68 22";
		Extent = (%w - 68) SPC 78;
		Text = "A fraction of the art's size, so one anchor suits art of any size - which is what stops a cursor appearing to jump when another replaces it.";
		align = "left";
		vAlign = "top";
		textWrap = true;
	};
	ThemeManager.setProfile(%hint, "labelProfile");
	%this.anchorBox.add(%hint);
}

function GuiProfileEditorCursorForm::makeCellGrid(%this)
{
	%grid = new GuiGridCtrl()
	{
		HorizSizing = "width";
		Position = "0 0";
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

function GuiProfileEditorCursorForm::addFieldRow(%this, %container, %field, %label, %kind, %swatchWidth)
{
	%row = new GuiControl()
	{
		class = "GuiProfileEditorFieldRow";
		Position = "0 0";
		fieldName = %field;
		labelText = %label;
		kind = %kind;
		swatchWidth = %swatchWidth;
		owner = %this;
	};
	%container.add(%row);
	%row.build();

	%this.row[%field] = %row;
	%this.rowFields = (%this.rowFields $= "") ? %field : (%this.rowFields SPC %field);
	return %row;
}

//-----------------------------------------------------------------------------
// Binding.
//-----------------------------------------------------------------------------

function GuiProfileEditorCursorForm::bind(%this, %cursor, %label)
{
	if(!isObject(%cursor))
	{
		%this.unbind();
		return;
	}

	%this.target = %cursor;
	%this.nameLabel.setText("Cursor:  " @ %label);
	%this.editor.cursor = %cursor;
	%this.refresh();
}

function GuiProfileEditorCursorForm::unbind(%this)
{
	%this.target = "";
	if(isObject(%this.editor))
	{
		%this.editor.cursor = "";
	}
}

function GuiProfileEditorCursorForm::refresh(%this)
{
	if(!isObject(%this.target))
	{
		return;
	}

	// populating stops a row that is only being filled in from reporting itself
	// as an edit, which would record a theme override nobody asked for.
	%this.populating = true;

	%count = getWordCount(%this.rowFields);
	for(%i = 0; %i < %count; %i++)
	{
		%field = getWord(%this.rowFields, %i);
		%this.row[%field].setValue(%this.target.getFieldValue(%field));
	}

	%this.populating = false;

	%this.refreshOverrides();
	%this.refreshReadout();
}

// Only the tint is derived from the theme, so it is the only row that can be
// overridden and the only one with anything to reset. The art fields are the
// user's outright -- there is no theme value behind them to go back to.
function GuiProfileEditorCursorForm::refreshOverrides(%this)
{
	%theme = %this.currentTheme();
	%hasTheme = isObject(%theme) && isObject(%this.target);

	%count = getWordCount(%this.rowFields);
	for(%i = 0; %i < %count; %i++)
	{
		%field = getWord(%this.rowFields, %i);
		%this.row[%field].setOverridden(%hasTheme && %theme.isFieldOverridden(%this.target, %field));
	}
}

function GuiProfileEditorCursorForm::refreshReadout(%this)
{
	if(!isObject(%this.target) || !isObject(%this.editor))
	{
		return;
	}

	%extent = %this.editor.getImageExtent();
	if(getWord(%extent, 0) <= 0)
	{
		%this.readout.setText("No art - choose a file.");
		%this.zoomIn.setActive(false);
		%this.zoomOut.setActive(false);
		return;
	}

	%hot = %this.editor.getEffectiveHotSpot();
	%this.readout.setText("Points at pixel" SPC getWord(%hot, 0) @ "," SPC getWord(%hot, 1) SPC
		"of" SPC getWord(%extent, 0) @ "x" @ getWord(%extent, 1));

	// The zoom the control is really drawing at, which is not always the one
	// that was asked for: art too big for the pane is clamped to what fits. The
	// buttons grey out at both ends rather than letting a click do nothing and
	// leaving the number to explain why.
	%zoom = %this.editor.getZoom();
	%this.zoomLabel.setText(%zoom @ "x");
	%this.zoomIn.setActive(%zoom < %this.editor.getMaxZoom());
	%this.zoomOut.setActive(%zoom > 1);
}

function GuiProfileEditorCursorForm::currentTheme(%this)
{
	%root = %this.dialog.currentRoot;
	if(isObject(%root) && %root.getClassName() $= "GuiProfileTheme")
	{
		return %root;
	}
	return "";
}

//-----------------------------------------------------------------------------
// Edits.
//-----------------------------------------------------------------------------

function GuiProfileEditorCursorForm::onProfileRowCommit(%this, %row)
{
	if(%this.populating || !isObject(%this.target))
	{
		return;
	}

	// A text box commits on blur, so most commits arrive from a field the user
	// only tabbed through; writing one would record an override for an edit that
	// never happened.
	if(!%row.hasChanged())
	{
		return;
	}

	%this.target.setFieldValue(%row.fieldName, %row.getValue());
	%row.markClean();
	%this.afterCommit();
}

function GuiProfileEditorCursorForm::onProfileRowReset(%this, %row)
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

// The magnifier reports a drag here. The value is already on the cursor -- the
// control writes it as the drag happens, which is what makes the dot follow the
// mouse -- so this only has to catch the rows up and mark the theme dirty.
function GuiProfileEditorCursorForm::onHotSpotChanged(%this, %x, %y)
{
	if(!isObject(%this.target))
	{
		return;
	}

	%this.populating = true;
	%this.row["hotSpot"].setValue(%x SPC %y);
	%this.populating = false;

	%this.refreshReadout();
	%this.afterCommit();
}

function GuiProfileEditorCursorForm::onAnchorPreset(%this, %x, %y)
{
	if(!isObject(%this.target))
	{
		return;
	}

	%this.target.renderOffset = %x SPC %y;
	%this.refresh();
	%this.afterCommit();
}

function GuiProfileEditorCursorForm::onZoom(%this, %step)
{
	%this.editor.setZoom(%this.editor.getZoom() + %step);
	%this.refreshReadout();
}

function GuiProfileEditorCursorForm::onDotColorChanged(%this)
{
	%this.editor.dotColor = %this.dotSwatch.getColorI();
}

function GuiProfileEditorCursorForm::afterCommit(%this)
{
	%this.refreshOverrides();

	// The readout is a function of BOTH placement fields, so typing into either
	// one moves it. Without this it goes on reporting the pixel the dot used to
	// be on while the dot itself has already moved -- which reads as the
	// magnifier and the numbers disagreeing.
	%this.refreshReadout();

	%this.dialog.onProfileChanged(%this.target);
}

//-----------------------------------------------------------------------------
// The magnifier forwards its callbacks to the pane that owns it.
//-----------------------------------------------------------------------------

function GuiProfileEditorCursorView::onHotSpotChanged(%this, %x, %y)
{
	if(isObject(%this.owner))
	{
		%this.owner.onHotSpotChanged(%x, %y);
	}
}

function GuiProfileEditorCursorView::onZoomChanged(%this, %zoom)
{
	if(isObject(%this.owner))
	{
		%this.owner.refreshReadout();
	}
}
