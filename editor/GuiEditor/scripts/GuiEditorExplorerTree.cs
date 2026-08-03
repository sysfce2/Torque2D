
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

// refreshItem rather than refreshItemText: a control's picture can change
// without its row moving. Re-profiling a bare GuiControl from a panel to a label
// is the same object in the same place wearing a different face, and the
// Category picker does exactly that.
function GuiEditorExplorerTree::onPostApply(%this, %obj)
{
    %index = %this.findItemID(%obj.getId());
    if(%index > -1)
    {
        %this.refreshItem(%index);
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

// Which frame of the tree's sheet a row wears. Asked once per row as the tree
// builds itself, never per draw.
function GuiEditorExplorerTree::onGetItemIcon(%this, %obj)
{
    // The simulated canvas is stage furniture, not a control in the document,
    // and giving it a picture would say otherwise.
    if(%obj == GuiEditor.rootGui)
    {
        return -1;
    }
    return GuiEditor.controlIcons.frameFor(%this.keyFor(%obj));
}

// A live control back to a palette entry.
//
// The two are not the same thing. Everything but a bare GuiControl is keyed by
// its class, but a GuiControl is the wrapper, the backdrop, the line of text and
// the modal scrim -- four entries sharing one class and told apart by the
// profile category they wear. So the class alone cannot pick the picture.
//
// The pane already owns that question, and asking it here is what keeps the
// Category dropdown and the row icon from disagreeing about the same control:
// currentCategory prefers the category stamped on the profile the control is
// actually wearing over the guess made from its shape.
function GuiEditorExplorerTree::keyFor(%this, %ctrl)
{
    %class = %ctrl.getClassName();
    if(%class $= "GuiControl")
    {
        return "GuiControl:" @ GuiEditor.inspectorWindow.pane.currentCategory(%ctrl);
    }

    // frameFor answers 0 -- the question mark -- for anything it has never heard
    // of, so a class with no icon reads as "no picture for this yet" rather than
    // as some other control.
    return %class;
}