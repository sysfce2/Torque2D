
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

	%this.buttonBar.addButton("onProfileEditor", 49, "Open the Gui Profile Editor", "");
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
