
function GuiEditorBrain::onAdd(%this)
{
    %this.setFirstResponder();
    %this.setSnapToGrid("10");
}

function GuiEditorBrain::onControlDragged(%this, %payload, %position)
{
	%x = getWord(%position, 0);
	%y = getWord(%position, 1);
	%target = %this.root.findHitControl(%x, %y);

	while(! %target.isContainer )
	{
		%target = %target.getParent();
	}

	if(%target != %this.getCurrentAddset())
	{
		%this.setCurrentAddSet(%target);
	}
}

function GuiEditorBrain::onControlDropped(%this, %payload, %position)
{
   %pos = %payload.getGlobalPosition();
   %x = getWord(%pos, 0);
   %y = getWord(%pos, 1);

   %this.acceptControl(%payload);

   %payload.setPositionGlobal(%x, %y);
   %this.schedule(40, "finishControlDropped", %payload, %x, %y);
}

// Everything about a control arriving in the document except where it lands.
//
// A drop knows where the cursor was and so places the control in global
// coordinates, twice, because the container it landed in may have moved it. A
// paste has no cursor: it knows the position the control held in its old parent,
// which is a local one, and sets it before calling this - so the properties pane
// reads the position the control is going to keep. Everything else is the same
// for both, and lives here rather than in each caller: the undo record, the
// theme, the selection, and the events the Explorer tree and the panes listen
// for.
function GuiEditorBrain::acceptControl(%this, %payload)
{
   // The add itself is the undo step, recorded from onAddNewCtrl below.
   %this.addNewCtrl(%payload);

   // A control arrives wearing whatever its C++ constructor named - a
   // GuiWindowCtrl names five, from GuiWindowProfile down - so put it on the
   // Gui's theme straight away. Themed on arrival is the whole point: the drop
   // is the last time anyone should have to think about which profile a button
   // wants.
   //
   // This has to run after addNewCtrl, because the control's parent decides
   // which category it takes (a control sitting directly on the root is the
   // Gui's backdrop and gets Panel, not Label). But addNewCtrl is also what
   // announces the selection, so everything that inspects the control has
   // already read it wearing the constructor's profiles. Rethemed tells them to
   // look again.
   //
   // None of it is recorded: theming a control that is arriving is part of it
   // arriving, not a second thing the user did. Undo puts the whole control in
   // the trash, where it keeps its profiles and its position, so redo has
   // nothing to put back but the control itself.
   %theme = GuiEditor.themeByName(GuiEditor.themeName);
   if(isObject(%theme))
   {
      GuiEditor.undoRecorder.suspend();
      GuiEditor.themeApplier.applyToBranch(%payload, %theme, false);
      GuiEditor.undoRecorder.resume();

      %this.postEvent("Rethemed", %payload);
   }

   %this.setFirstResponder();
   %this.postEvent("AddControl", %payload);
   %this.postEvent("Inspect", %payload);
}

function GuiEditorBrain::finishControlDropped(%this, %payload, %x, %y)
{
   %payload.setPositionGlobal(%x, %y);
}

function GuiEditorBrain::startRadioSilence(%this)
{
    %this.removeAllListeners();
}

function GuiEditorBrain::endRadioSilence(%this)
{
    %this.addListener(GuiEditor.explorerWindow);
    %this.addListener(GuiEditor.inspectorWindow);
}

//Source callbacks - Events that happened with this control and need to be relayed to other controls.
function GuiEditorBrain::onEdit(%this, %ctrl)
{
    %this.postEvent("Edit", %ctrl);
}

function GuiEditorBrain::onRemoveSelected(%this,%ctrl)
{
    %this.postEvent("ClearInspect", %ctrl);
    %this.toggleMenuItems();
}

function GuiEditorBrain::onClearSelected(%this)
{
    %this.postEvent("ClearInspectAll");
    %this.toggleMenuItems();
}

function GuiEditorBrain::onAddSelected(%this, %ctrl)
{
    %this.postEvent("AlsoInspect", %ctrl);
    %this.toggleMenuItems();
}

function GuiEditorBrain::onDelete(%this)
{
	%this.postEvent("ObjectRemoved");
    %this.toggleMenuItems();
}

function GuiEditorBrain::onSelectionParentChange(%this, %parent)
{
    %this.postEvent("ParentChange", %parent);
    %this.toggleMenuItems();
}

//Receiving Callbacks - Events that happened at other controls and need to be reflected with this control.
function GuiEditorBrain::onInspect(%this, %ctrl)
{
    %this.startRadioSilence();
    %this.clearSelection();
	%this.select(%ctrl);
    %this.endRadioSilence();
    %this.toggleMenuItems();
}

function GuiEditorBrain::onAlsoInspect(%this, %ctrl)
{
    %this.startRadioSilence();
	%this.addSelection(%ctrl);
    %this.endRadioSilence();
    %this.toggleMenuItems();
}

function GuiEditorBrain::onClearInspect(%this, %ctrl)
{
    %this.startRadioSilence();
	%this.removeSelection(%ctrl);
    %this.endRadioSilence();
    %this.toggleMenuItems();
}

function GuiEditorBrain::onClearInspectAll(%this)
{
    %this.startRadioSilence();
	%this.clearSelection();
    %this.endRadioSilence();
    %this.toggleMenuItems();
}

function GuiEditorBrain::onObjectRemoved(%this, %ctrl)
{
    %this.startRadioSilence();
	%this.deleteSelection();
    %this.endRadioSilence();
    %this.toggleMenuItems();
}

//-----------------------------------------------------------------------------
// Undo. The C++ edit control has always announced every edit it makes at the
// moment it makes it - guiEditCtrl.cc calls all of these, and each one sits
// beside a bare "// undo" comment marking where the recording used to happen.
// Nothing implemented them until now.
//
// The pairs matter: what a drag or a nudge did is only known once it is over,
// so the pre half remembers where everything was and the post half works out
// what actually moved. A mouse-down that only selected records nothing.
//-----------------------------------------------------------------------------

// Mouse-down on a selection, before a drag-move or a handle-resize.
function GuiEditorBrain::onPreEdit(%this, %selection)
{
    GuiEditor.undoRecorder.snapshot(%selection);
}

function GuiEditorBrain::onPostEdit(%this, %selection)
{
    GuiEditor.undoRecorder.commitGeometry("", "");
}

// Arrow-key and menu nudges, which the C++ gives a callback of their own
// precisely so that a run of them can be folded into one action.
function GuiEditorBrain::onPreSelectionNudged(%this, %selection)
{
    GuiEditor.undoRecorder.snapshot(%selection);
}

function GuiEditorBrain::onPostSelectionNudged(%this, %selection)
{
    GuiEditor.undoRecorder.commitGeometry("", "nudge");
}

// Fired before the controls are moved to the trash, which is the only moment
// where each one still knows the parent and index it has to go back to.
function GuiEditorBrain::onTrashSelection(%this, %selection)
{
    GuiEditor.undoRecorder.recordDeleteSelection(%selection);
}

// Fired after the control has been put in the add set.
function GuiEditorBrain::onAddNewCtrl(%this, %ctrl)
{
    GuiEditor.undoRecorder.recordAdd(%ctrl, "");
}

// The same for a whole set of them, which is how a loaded selection arrives.
function GuiEditorBrain::onAddNewCtrlSet(%this, %selection)
{
    GuiEditor.undoRecorder.begin("Add Controls", "");
    for(%i = 0; %i < %selection.getCount(); %i++)
    {
        GuiEditor.undoRecorder.recordAdd(%selection.getObject(%i), "");
    }
    GuiEditor.undoRecorder.end();
}

// Put the selection on the controls a replay changed, and tell everyone.
function GuiEditorBrain::restoreSelection(%this, %list)
{
    %this.selectList(%list);
}

// Select exactly these controls, and say so.
//
// The announcement has to be made here, because addSelection makes none:
// it is the receiving half of the bus - what this class calls, under radio
// silence, when the tree or the pane has already announced a selection - so it
// changes the C++ selection and says nothing. Calling it on its own leaves the
// canvas drawing handles round a control the properties pane has never heard
// of, and the clearSelection ahead of it has already emptied the pane.
function GuiEditorBrain::selectList(%this, %list)
{
    // Already the selection, with values that changed underneath it - which is
    // the commonest undo there is: change a setting, press Ctrl+Z. Re-announcing
    // would rebuild the whole properties pane, when all it needs is to re-read
    // the control it is already showing.
    if(%list $= %this.selectionList())
    {
        %this.postEvent("Replayed");
        return;
    }

    %this.clearSelection();

    for(%i = 0; %i < getWordCount(%list); %i++)
    {
        %ctrl = getWord(%list, %i);
        %this.addSelection(%ctrl);

        // What the C++ would have called had it done the selecting, so there is
        // one definition of what announcing a selection means.
        %this.onAddSelected(%ctrl);
    }
}

function GuiEditorBrain::selectionList(%this)
{
    %set = %this.getSelected();
    %list = "";

    for(%i = 0; %i < %set.getCount(); %i++)
    {
        %ctrl = %set.getObject(%i);
        %list = (%list $= "") ? %ctrl : (%list SPC %ctrl);
    }

    return %list;
}

function GuiEditorBrain::toggleMenuItems(%this)
{
    %count = %this.getSelected().getCount();
    EditorCore.menuBar.setMenuActive("Deselect", %count != 0);
    EditorCore.menuBar.setMenuActive("Cut", %count != 0);
    EditorCore.menuBar.setMenuActive("Copy", %count != 0);
    EditorCore.menuBar.setMenuActive("Nudge Up", %count != 0);
    EditorCore.menuBar.setMenuActive("Nudge Down", %count != 0);
    EditorCore.menuBar.setMenuActive("Nudge Left", %count != 0);
    EditorCore.menuBar.setMenuActive("Nudge Right", %count != 0);
    EditorCore.menuBar.setMenuActive("Expand Height", %count != 0);
    EditorCore.menuBar.setMenuActive("Shrink Height", %count != 0);
    EditorCore.menuBar.setMenuActive("Expand Width", %count != 0);
    EditorCore.menuBar.setMenuActive("Shrink Width", %count != 0);
    EditorCore.menuBar.setMenuActive("Align Top", %count > 1);
    EditorCore.menuBar.setMenuActive("Align Bottom", %count > 1);
    EditorCore.menuBar.setMenuActive("Align Left", %count > 1);
    EditorCore.menuBar.setMenuActive("Align Right", %count > 1);
    EditorCore.menuBar.setMenuActive("Center Horizontally", %count > 1);
    EditorCore.menuBar.setMenuActive("Space Vertically", %count > 2);
    EditorCore.menuBar.setMenuActive("Space Horizontally", %count > 2);
    EditorCore.menuBar.setMenuActive("Bring to Front", %count == 1);
    EditorCore.menuBar.setMenuActive("Push to Back", %count == 1);
}