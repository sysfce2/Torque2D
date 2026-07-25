
//-----------------------------------------------------------------------------
// The custom theme-editing form shown in place of the generic inspector when a
// theme is selected in the Gui Profile Editor. It is a GuiGridCtrl with
// superclass EditorForm (so all the addFormItem / create*Item row factories are
// available) and is its own event listener. Every field edit writes straight to
// the theme, which restamps its members, then marks the library dirty and
// refreshes the preview -- the same path the inspector's onProfileChanged took.
//
// Text fields commit on blur via AltCommand (fired from onLoseFirstResponder),
// matching how the native inspector applies its edits; Enter commits too.
//-----------------------------------------------------------------------------

function ProfileThemeEditForm::build(%this)
{
	%cw = 350;

	// The six theme colors, in display order. Field names double as the
	// dispatch keys the change handlers use; kept on the form (not a global)
	// so bindTheme can walk them.
	%this.colorFields = "colorBackground colorSurface colorForeground colorAccent colorHighlight colorWarning";
	%colorLabels = "Background" TAB "Surface" TAB "Foreground" TAB "Accent" TAB "Highlight" TAB "Warning";

	// Name (display only; rename lives on the toolbar).
	%this.nameLabel = %this.addFormItem("Theme:", %cw SPC 30);

	%item = %this.addFormItem("Border Size", %cw SPC 30);
	%this.borderSizeBox = %this.createTextEditItem(%item);
	%this.borderSizeBox.inputMode = "Number";
	%this.borderSizeBox.AltCommand = %this.getID() @ ".commitBorderSize();";

	// A font file picker: the user finds any font in the target directory and we
	// keep that directory. Uses OpenFileDialog (the working "Find" pattern from
	// the image asset dialog) rather than the folder dialog.
	%item = %this.addFormItem("Font Directory", %cw SPC 30);
	%this.fontDirBox = %this.createFileOpenItem(%item, "Font Files (*.ttf;*.otf;*.fnt;*.uft)|*.ttf;*.otf;*.fnt;*.uft", "Find a Font in the Target Directory");
	%this.fontDirBox.AltCommand = %this.getID() @ ".commitDirectory();";

	%item = %this.addFormItem("Font - Title", %cw SPC 30);
	%this.fontTitleDrop = %this.createDropDownItem(%item);
	%this.fontTitleDrop.themeField = "fontTitle";

	%item = %this.addFormItem("Font - Body", %cw SPC 30);
	%this.fontBodyDrop = %this.createDropDownItem(%item);
	%this.fontBodyDrop.themeField = "fontBody";

	%item = %this.addFormItem("Font - Code", %cw SPC 30);
	%this.fontCodeDrop = %this.createDropDownItem(%item);
	%this.fontCodeDrop.themeField = "fontCode";

	%item = %this.addFormItem("Font Size", %cw SPC 30);
	%this.fontSizeBox = %this.createTextEditItem(%item);
	%this.fontSizeBox.inputMode = "Number";
	%this.fontSizeBox.AltCommand = %this.getID() @ ".commitFontSize();";

	%count = getFieldCount(%colorLabels);
	for(%i = 0; %i < %count; %i++)
	{
		%field = getWord(%this.colorFields, %i);
		%label = getField(%colorLabels, %i);

		%item = %this.addFormItem(%label, %cw SPC 76);
		%item.vAlign = "top";
		%item.align = "left";
		%swatch = %this.createColorItem(%item);
		%swatch.themeField = %field;
		%swatch.Command = %this.getID() @ ".onColorPopup(" @ %swatch.getID() @ ");";

		%boxCmd = %this.getID() @ ".onColorTyped(" @ %swatch.getID() @ ");";
		%swatch.redBox.AltCommand = %boxCmd;
		%swatch.greenBox.AltCommand = %boxCmd;
		%swatch.blueBox.AltCommand = %boxCmd;
		%swatch.alphaBox.AltCommand = %boxCmd;

		%this.swatch[%field] = %swatch;
	}
}

//-----------------------------------------------------------------------------
// Binding.
//-----------------------------------------------------------------------------

function ProfileThemeEditForm::bindTheme(%this, %theme)
{
	if(!isObject(%theme))
	{
		%this.unbind();
		return;
	}

	%this.theme = %theme;

	// Setting control values fires the same change events a user edit would;
	// the flag keeps those from writing back into the theme mid-populate.
	%this.populating = true;

	%this.nameLabel.setText("Theme:  " @ %theme.getName());
	%this.borderSizeBox.setText(%theme.borderSize);
	%this.fontSizeBox.setText(%theme.fontSize);
	%this.fontDirBox.setText(%theme.fontDirectory);

	%this.rebuildFontDropdowns();

	%count = getWordCount(%this.colorFields);
	for(%i = 0; %i < %count; %i++)
	{
		%field = getWord(%this.colorFields, %i);
		%swatch = %this.swatch[%field];
		if(!isObject(%swatch))
		{
			continue;
		}
		%this.showColor(%swatch, %theme.getFieldValue(%field));
	}

	%this.populating = false;
}

function ProfileThemeEditForm::unbind(%this)
{
	%this.theme = "";
}

// The single write path: assign the field (which restamps the theme's
// members), mark the theme dirty, and refresh the preview.
function ProfileThemeEditForm::applyField(%this, %field, %value)
{
	if(%this.populating || !isObject(%this.theme))
	{
		return;
	}
	%this.theme.setFieldValue(%field, %value);
	%this.dialog.library.markDirty(%this.theme);
	// Defer the preview rebuild: this can fire on blur while another control is
	// being clicked, and rebuilding synchronously would free that control mid
	// event. See GuiProfileEditorDialog::schedulePreviewRefresh.
	%this.dialog.schedulePreviewRefresh();
}

//-----------------------------------------------------------------------------
// Text-field commits (fired on blur via AltCommand, and on Enter via the
// EditorForm ReturnPressed event).
//-----------------------------------------------------------------------------

function ProfileThemeEditForm::commitBorderSize(%this)
{
	if(%this.populating || !isObject(%this.theme))
	{
		return;
	}
	%v = mFloor(%this.borderSizeBox.getText());
	if(%v < 0)
	{
		%v = 0;
	}
	%this.borderSizeBox.setText(%v);
	%this.applyField("borderSize", %v);
}

function ProfileThemeEditForm::commitFontSize(%this)
{
	if(%this.populating || !isObject(%this.theme))
	{
		return;
	}
	%v = mFloor(%this.fontSizeBox.getText());
	if(%v < 1)
	{
		%v = 1;
	}
	%this.fontSizeBox.setText(%v);
	%this.applyField("fontSize", %v);
}

function ProfileThemeEditForm::commitDirectory(%this)
{
	if(%this.populating || !isObject(%this.theme))
	{
		return;
	}
	%this.applyField("fontDirectory", %this.fontDirBox.getText());
	%this.rebuildFontDropdowns();
}

function ProfileThemeEditForm::onReturnPressed(%this, %ctrl)
{
	if(%ctrl == %this.borderSizeBox)
	{
		%this.commitBorderSize();
	}
	else if(%ctrl == %this.fontSizeBox)
	{
		%this.commitFontSize();
	}
	else if(%ctrl == %this.fontDirBox)
	{
		%this.commitDirectory();
	}
}

// The user picked a font file; keep its directory as the font directory.
function ProfileThemeEditForm::onFileOpened(%this, %box)
{
	if(%box == %this.fontDirBox)
	{
		// Keep the directory relative to the game root, not the absolute path.
		%dir = filePath(%box.getText());
		%dir = makeRelativePath(makeFullPath(%dir, getMainDotCsDir()), getMainDotCsDir());
		%box.setText(%dir);
		%this.applyField("fontDirectory", %dir);
		%this.rebuildFontDropdowns();
	}
}

//-----------------------------------------------------------------------------
// Color commits. The swatch and its four numeric boxes are kept in sync; the
// swatch (not string math) is the single source of truth for the value.
//-----------------------------------------------------------------------------

function ProfileThemeEditForm::onColorPopup(%this, %swatch)
{
	if(%this.populating)
	{
		return;
	}
	%ci = %swatch.getColorI();
	%this.setColorBoxes(%swatch, %ci);
	%this.applyField(%swatch.themeField, %ci);
}

function ProfileThemeEditForm::onColorTyped(%this, %swatch)
{
	if(%this.populating)
	{
		return;
	}
	%r = mClamp(mRound(%swatch.redBox.getText()), 0, 255);
	%g = mClamp(mRound(%swatch.greenBox.getText()), 0, 255);
	%b = mClamp(mRound(%swatch.blueBox.getText()), 0, 255);
	%a = mClamp(mRound(%swatch.alphaBox.getText()), 0, 255);
	%ci = %r SPC %g SPC %b SPC %a;

	%swatch.setColorI(%ci);
	%this.setColorBoxes(%swatch, %ci);
	%this.applyField(%swatch.themeField, %ci);
}

function ProfileThemeEditForm::setColorBoxes(%this, %swatch, %ci)
{
	%swatch.redBox.setText(getWord(%ci, 0));
	%swatch.greenBox.setText(getWord(%ci, 1));
	%swatch.blueBox.setText(getWord(%ci, 2));
	%swatch.alphaBox.setText(getWord(%ci, 3));
}

// Display a color value on the swatch and its boxes. setColorI wants four
// integers; a stock color name (e.g. "White") comes back from a ColorI field as
// a single token, so route that through the baseColor field, which parses named
// colors. (The C++ setField the native inspector uses is not a script method,
// so we must handle both forms here - this is what makes the initial values
// show correctly instead of the swatch's default grey.)
function ProfileThemeEditForm::showColor(%this, %swatch, %value)
{
	if(getWordCount(%value) >= 4)
	{
		%swatch.setColorI(%value);
	}
	else
	{
		%swatch.baseColor = %value;
	}
	%this.setColorBoxes(%swatch, %swatch.getColorI());
}

//-----------------------------------------------------------------------------
// Font selection. The enumeration, drop-down filling and cache baking all live
// on the shared library (GuiProfileEditorLibrary) because the profile pane
// names a face out of a directory exactly the way a theme does.
//-----------------------------------------------------------------------------

function ProfileThemeEditForm::onDropDownSelect(%this, %ctrl)
{
	if(%this.populating || %ctrl.themeField $= "")
	{
		return;
	}
	%face = %ctrl.getText();
	%this.applyField(%ctrl.themeField, %face);
}

function ProfileThemeEditForm::rebuildFontDropdowns(%this)
{
	if(!isObject(%this.theme))
	{
		return;
	}
	%library = %this.dialog.library;
	%faces = %library.enumerateFonts(%this.theme.fontDirectory);
	%library.fillFontDropdown(%this.fontTitleDrop, %faces, %this.theme.fontTitle);
	%library.fillFontDropdown(%this.fontBodyDrop, %faces, %this.theme.fontBody);
	%library.fillFontDropdown(%this.fontCodeDrop, %faces, %this.theme.fontCode);
}

// Font caches are not baked while the theme is being edited: the preview
// renders a newly chosen face straight from the platform font, and baking one
// costs seconds. GuiProfileEditorLibrary::bakeFontsFor does it on Save.
