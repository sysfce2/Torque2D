
function GuiProfileEditorTree::onSelect(%this, %index, %text, %item)
{
	%this.dialog.onTreeSelect(%item);
}

function GuiProfileEditorTree::onGetObjectText(%this, %obj)
{
	return %obj.treeLabel;
}
