
function GuiEditorExplorerTree::onAdd(%this)
{
    %this.endRadioSilence();
}

function GuiEditorExplorerTree::startRadioSilence(%this)
{
    %this.removeAllListeners();
}

function GuiEditorExplorerTree::endRadioSilence(%this)
{
    %this.addListener(GuiEditor.brain);
    %this.addListener(GuiEditor.inspectorWindow);
}

function GuiEditorExplorerTree::onSelect(%this, %index, %text, %item)
{
    if(%this.getSelCount() == 1)
    {
        %this.postEvent("ClearInspectAll");
        %this.postEvent("AlsoInspect", %item);
    }
    else 
    {
        %this.postEvent("AlsoInspect", %item);
    }
}

function GuiEditorExplorerTree::onUnselect(%this, %index, %text, %item)
{
    %this.postEvent("ClearInspect", %item);
}

function GuiEditorExplorerTree::onUnselectAll(%this)
{
    %this.postEvent("ClearInspectAll");
}

function GuiEditorExplorerTree::onDeleteKey(%this, %index, %text, %item)
{
	%this.postEvent("ObjectRemoved", %item);
}

// Drag-to-reorder in the tree rearranges the real control hierarchy in C++
// (GuiTreeViewCtrl::reorderFromDrag), and can move several selected controls
// into several parents in one go. The pair brackets the whole rearrangement:
// the document's shape is remembered here and read again afterwards, and the
// difference is the undo step.
function GuiEditorExplorerTree::onPreReorder(%this)
{
	GuiEditor.undoRecorder.snapshotHierarchy(GuiEditor.rootGui);
}

function GuiEditorExplorerTree::onPostReorder(%this)
{
	GuiEditor.undoRecorder.commitHierarchy("Reparent Control");
}

function GuiEditorExplorerTree::onPostApply(%this, %obj)
{
    %index = %this.findItemID(%obj.getId());
    if(%index > -1)
    {
        %this.refreshItemText(%index);
    }
}

function GuiEditorExplorerTree::onGetObjectText(%this, %obj)
{
    if(%obj == GuiEditor.rootGui)
    {
        return "Canvas Simulation";
    }
    return "";
}