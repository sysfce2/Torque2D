
//-----------------------------------------------------------------------------
// Picks the theme the Gui being edited wears. Applying re-profiles every control
// in the document from that theme, so this is the one place a developer has to
// think about profiles at all.
//
// Stand-alone profiles are the exception the checkbox exists for: they are the
// supported alternative to theming and are left alone by default. Everything
// else - a script profile, one of AppCore's, another theme's - is replaced.
//-----------------------------------------------------------------------------

function GuiEditorThemeDialog::init(%this, %width, %height)
{
	%window = %this.getObject(0);
	%content = %window.getObject(0);

	// The dialog's scrolling content is the dialog size less the window frame
	// (4px each side) and its title bar (30) - see EditorDialog::onAdd. Lay out
	// inside that, or the form pushes a scrollbar in and clips the button.
	%contentWidth = %width - 8;
	%contentHeight = %height - 26;

	%form = new GuiGridCtrl()
	{
		class = "EditorForm";
		extent = %contentWidth SPC 120;
		cellSizeX = %contentWidth;
		cellSizeY = 60;
	};
	%content.add(%form);

	%item = %form.addFormItem("Theme", %contentWidth SPC 50);
	%this.themeDrop = %form.createDropDownItem(%item);

	%item = %form.addFormItem("Override stand alone profiles", %contentWidth SPC 40);
	%this.overrideBox = %form.createCheckboxItem(%item);
	%this.overrideBox.setStateOn(false);

	%this.applyButton = new GuiButtonCtrl()
	{
		HorizSizing = "right";
		VertSizing = "bottom";
		Position = (%contentWidth - 110) SPC (%contentHeight - 44);
		Extent = "100 34";
		Text = "Apply";
		Command = %this.getID() @ ".onApply();";
	};
	ThemeManager.setProfile(%this.applyButton, "primaryButtonProfile");
	%content.add(%this.applyButton);

	%this.populate();
}

function GuiEditorThemeDialog::populate(%this)
{
	%themes = GuiEditor.getThemeLibrary().getThemes();
	%count = getWordCount(%themes);

	%this.themeDrop.clearItems();
	%this.themeCount = 0;

	%selected = -1;
	for(%i = 0; %i < %count; %i++)
	{
		%theme = getWord(%themes, %i);
		if(%theme.getName() $= "")
		{
			continue;
		}

		%this.themeDrop.addItem(%theme.getName());
		%this.theme[%this.themeCount] = %theme;
		if(%theme.getName() $= GuiEditor.themeName)
		{
			%selected = %this.themeCount;
		}
		%this.themeCount++;
	}

	if(%this.themeCount == 0)
	{
		// Nothing to apply. A project always has at least one theme once AppCore
		// has run, so this means the Gui Editor is open without a project.
		%this.themeDrop.setActive(false);
		%this.applyButton.setActive(false);
		return;
	}

	%this.themeDrop.setSelected(%selected >= 0 ? %selected : 0);
}

function GuiEditorThemeDialog::onApply(%this)
{
	%index = %this.themeDrop.getSelectedItem();
	if(%index < 0 || %index >= %this.themeCount)
	{
		%this.onClose();
		return;
	}

	GuiEditor.setTheme(%this.theme[%index], %this.overrideBox.getStateOn());
	%this.onClose();
}
