
function GuiEditorToolsWindow::onAdd(%this)
{
	%this.buttonBar = new GuiChainCtrl()
	{
		Class = "EditorButtonBar";
		Position = "6 4";
		Extent = "0 30";
		ChildSpacing = 4;
		IsVertical = false;
		Tool = %this;
	};
	ThemeManager.setProfile(%this.buttonBar, "emptyProfile");
	%this.add(%this.buttonBar);

	%this.buttonBar.addButton("onProfileEditor", $EditorIcon::doc_edit, "Open the Gui Profile Editor", "");
	%this.buttonBar.addButton("onSetTheme", $EditorIcon::brush, "Set this Gui's theme", "");
}

// The window's own title carries the document being edited, and a trailing star
// when it has changes that are not on disk. This window rather than anywhere
// else because it is the top-left panel and already holds the theme buttons, so
// it is where the eye goes first -- and because the editor TAB cannot do it:
// EditorCoreTabBook::onTabSelected looks an editor up by its tab text, so
// retitling the tab loses the lookup.
function GuiEditorToolsWindow::showDocument(%this, %name, %modified)
{
	%this.setText(%modified ? (%name @ " *") : %name);
}

function GuiEditorToolsWindow::onRemove(%this)
{
	if(isObject(%this.buttonBar))
	{
		%this.buttonBar.delete();
	}
}

function GuiEditorToolsWindow::onProfileEditor(%this)
{
	GuiEditor.openProfileEditor();
}

function GuiEditorToolsWindow::onSetTheme(%this)
{
	GuiEditor.openThemeDialog();
}
