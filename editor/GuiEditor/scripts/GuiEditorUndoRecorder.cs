
//-----------------------------------------------------------------------------
// The Gui Editor's undo recorder. Owned by GuiEditor; the only thing in the
// editor that touches the UndoManager, and the only thing that builds a
// GuiEditorUndoAction.
//
// Everything that changes the Gui being authored comes through here, by one of
// two routes:
//
//   writeField / writeDynamicField    a field write. The recorder does the
//                                     writing, so a write that is not recorded
//                                     is a write that did not happen.
//   snapshot + commitGeometry         a gesture whose result is only known
//                                     when it ends - a drag, a run of nudges.
//                                     The C++ edit control brackets both with
//                                     callbacks (onPreEdit/onPostEdit,
//                                     onPreSelectionNudged/...) which is what
//                                     they were always for.
//
// A transaction groups whatever happens between begin() and end() into one
// action, so that a gesture the user made once is one Ctrl+Z. Transactions
// nest: the outermost owns the action, and a write outside any transaction
// gets one of its own.
//
// Records name objects by id, so the stack has to be dropped whenever the
// document or the profiles under it are replaced. See GuiEditor::NewGui,
// DisplayGuiContent and detachTheme.
//-----------------------------------------------------------------------------

function GuiEditorUndoRecorder::onAdd(%this)
{
	%this.depth = 0;
	%this.pending = 0;
	%this.pendingKind = "";
	%this.suspended = false;
	%this.lastAction = 0;
	%this.lastKind = "";
	%this.snapCount = 0;
	%this.hierCount = 0;
	%this.hierCtrlCount = 0;
	%this.watchCount = 0;
	%this.replayTouched = "";

	// What the Edit menu was last told, so a run of nudges does not walk the
	// whole menu tree once per key.
	%this.menuUndo = -1;
	%this.menuRedo = -1;
}

function GuiEditorUndoRecorder::onRemove(%this)
{
	// A transaction still open at teardown owns an action nobody will ever
	// push, and an action that never reached a manager is nobody else's to
	// free.
	if(isObject(%this.pending))
	{
		%this.pending.delete();
		%this.pending = 0;
	}
}

// The manager is a member of the C++ edit control (guiEditCtrl.cc), registered
// with it and freed with it, so it is asked for rather than held.
function GuiEditorUndoRecorder::manager(%this)
{
	if(!isObject(%this.owner) || !isObject(%this.owner.brain))
	{
		return 0;
	}

	return %this.owner.brain.getUndoManager();
}

//-----------------------------------------------------------------------------
// Transactions.
//-----------------------------------------------------------------------------

// %kind is only read by the coalescing test; "" means "never merge this with
// anything".
function GuiEditorUndoRecorder::begin(%this, %name, %kind)
{
	if(%this.suspended)
	{
		return;
	}

	%this.depth++;
	if(%this.depth > 1)
	{
		return;
	}

	%this.pending = new UndoScriptAction()
	{
		class = "GuiEditorUndoAction";
		actionName = (%name $= "") ? "Edit" : %name;
		recorder = %this;
	};
	%this.pendingKind = %kind;
}

function GuiEditorUndoRecorder::end(%this)
{
	if(%this.suspended || %this.depth <= 0)
	{
		return;
	}

	%this.depth--;
	if(%this.depth > 0)
	{
		return;
	}

	%action = %this.pending;
	%this.pending = 0;
	if(!isObject(%action))
	{
		return;
	}

	// Nothing happened between the begin and the end: a click that selected
	// without moving, a text box the user only tabbed through, a commit whose
	// value was already what it is. Not an undo step.
	if(%action.isEmpty())
	{
		%action.delete();
		return;
	}

	if(%this.canCoalesce(%action))
	{
		%this.lastAction.mergeFrom(%action);
		%action.delete();
		return;
	}

	// Now that the operation is over, what any frame set involved in it ended up
	// looking like.
	%this.emitFrameFixes(%action);

	%action.addToManager(%this.manager());
	%this.lastAction = %action;
	%this.lastKind = %this.pendingKind;
	%this.refreshMenu();
}

// Holding an arrow key down is one move, not thirty. Only a like-for-like
// repeat merges: the same kind of gesture, touching exactly the same controls,
// with nothing in between. lastAction is dropped by any replay and any clear,
// so a merge can never reach an action that is no longer on top of the stack.
function GuiEditorUndoRecorder::canCoalesce(%this, %action)
{
	if(%this.pendingKind $= "" || %this.pendingKind !$= %this.lastKind)
	{
		return false;
	}

	if(!isObject(%this.lastAction))
	{
		return false;
	}

	return %this.lastAction.touched() $= %action.touched();
}

//-----------------------------------------------------------------------------
// Writes. The recorder does the writing so that recording cannot be forgotten.
//-----------------------------------------------------------------------------

// setEditFieldValue rather than a plain assignment: it brackets the write with
// inspectPreApply / inspectPostApply, which sleeps and wakes the control (so a
// re-profile releases the old profile and takes a count on the new one) and
// runs the protected-field setters - Position and Extent are protected fields
// whose setters call setPosition/setExtent.
function GuiEditorUndoRecorder::writeField(%this, %ctrl, %field, %value)
{
	if(!isObject(%ctrl))
	{
		return;
	}

	%before = %this.fieldSnapshot(%ctrl, %field);
	%ctrl.setEditFieldValue(%field, %value);
	%this.recordField(%ctrl, %field, %before, %this.fieldSnapshot(%ctrl, %field), false);
}

function GuiEditorUndoRecorder::writeDynamicField(%this, %ctrl, %field, %value)
{
	if(!isObject(%ctrl))
	{
		return;
	}

	%before = %ctrl.getFieldValue(%field);
	%ctrl.setFieldValue(%field, %value);
	%this.recordField(%ctrl, %field, %before, %ctrl.getFieldValue(%field), true);
}

// What the field holds, in the form an undo should write back.
//
// A profile slot is read as an id, never the name the field reads back: the
// editor runs in editor mode, where SimObject::assignName stashes a new
// object's name instead of registering it, so a profile made this session
// cannot be found by name. GuiEditorThemeApplier::fieldProfile already knows
// how to resolve one either way.
//
// A control's own name is asked of the object for the same reason -
// getFieldValue answers "" for it in editor mode.
function GuiEditorUndoRecorder::fieldSnapshot(%this, %ctrl, %field)
{
	if(%ctrl.getFieldType(%field) $= "GuiProfile")
	{
		%profile = %this.owner.themeApplier.fieldProfile(%ctrl, %field);
		return isObject(%profile) ? %profile.getId() : "";
	}

	if(%field $= "name")
	{
		return %ctrl.getName();
	}

	return %ctrl.getFieldValue(%field);
}

// Reading the value back after the write, rather than trusting what was asked
// for, is what keeps redo faithful: the engine normalises plenty of them - a
// bool, a point, a profile written by id and read back by name.
function GuiEditorUndoRecorder::recordField(%this, %ctrl, %field, %before, %after, %isDynamic)
{
	if(%this.suspended || !isObject(%ctrl))
	{
		return;
	}

	// strcmp, not $=: $= runs dStricmp, so recasing a caption would read as no
	// change at all and vanish from the stack.
	if(strcmp(%before, %after) == 0)
	{
		return;
	}

	%owned = %this.autoBegin("Change " @ %field);
	if(isObject(%this.pending))
	{
		%this.pending.addFieldOp(%ctrl, %field, %before, %after, %isDynamic);
	}
	%this.autoEnd(%owned);
}

//-----------------------------------------------------------------------------
// Structure: a control changing parent, index, or both.
//-----------------------------------------------------------------------------

// Called after the control has arrived. Undoing an add moves the control to
// the trash rather than deleting it, which is what leaves redo something to
// put back.
function GuiEditorUndoRecorder::recordAdd(%this, %ctrl, %name)
{
	if(%this.suspended || !isObject(%ctrl))
	{
		return;
	}

	%parent = %ctrl.getParent();
	if(!isObject(%parent))
	{
		return;
	}

	%this.watchFrameSet(%parent);

	%owned = %this.autoBegin((%name $= "") ? "Add Control" : %name);
	if(isObject(%this.pending))
	{
		%this.pending.addMoveOp(%ctrl, %this.trash(), -1, %parent, %this.indexOf(%parent, %ctrl));

		// The layout it arrived with, which is what a redo has to put back: a
		// container that places its own children will have taken it once
		// already. Both ends are the same value - undo sends the control to the
		// trash, where its layout is nobody's business.
		%layout = %this.layoutOf(%ctrl);
		%this.pending.addLayoutFix(%ctrl, %layout, %layout);
	}
	%this.autoEnd(%owned);
}

// Called before the control is trashed - the C++ fires onTrashSelection ahead
// of the move for this reason - because where it came from is exactly what is
// about to be lost.
function GuiEditorUndoRecorder::recordDelete(%this, %ctrl, %name)
{
	if(%this.suspended || !isObject(%ctrl))
	{
		return;
	}

	%parent = %ctrl.getParent();
	if(!isObject(%parent))
	{
		return;
	}

	// Before the trash move, which is the last moment the frame it stands in
	// still exists.
	%this.watchFrameSet(%parent);

	%owned = %this.autoBegin((%name $= "") ? "Delete Control" : %name);
	if(isObject(%this.pending))
	{
		%this.pending.addMoveOp(%ctrl, %parent, %this.indexOf(%parent, %ctrl), %this.trash(), -1);

		// How it was laid out, which the parent will take from it the moment it
		// is put back, if the parent is one that places its own children.
		%layout = %this.layoutOf(%ctrl);
		%this.pending.addLayoutFix(%ctrl, %layout, %layout);
	}
	%this.autoEnd(%owned);
}

// A whole selection on its way to the trash, recorded highest index first.
//
// Order is load-bearing. An undo replays the ops backwards, so recording them
// in descending index order restores them in ascending order - and restoring
// low to high is the only order that lands them all where they were, because
// putting a control back at index 3 shifts everything from 3 down one place.
function GuiEditorUndoRecorder::recordDeleteSelection(%this, %selection)
{
	if(%this.suspended || !isObject(%selection))
	{
		return;
	}

	%count = %selection.getCount();
	for(%i = 0; %i < %count; %i++)
	{
		%list[%i] = %selection.getObject(%i);
	}

	%this.begin("Delete Control", "");
	for(%out = 0; %out < %count; %out++)
	{
		%pick = -1;
		for(%i = 0; %i < %count; %i++)
		{
			if(%list[%i] $= "" || !isObject(%list[%i]))
			{
				continue;
			}
			if(%pick == -1 || %this.parentIndexOf(%list[%i]) > %this.parentIndexOf(%list[%pick]))
			{
				%pick = %i;
			}
		}

		if(%pick == -1)
		{
			break;
		}

		%this.recordDelete(%list[%pick], "");
		%list[%pick] = "";
	}
	%this.end();
}

function GuiEditorUndoRecorder::parentIndexOf(%this, %ctrl)
{
	%parent = %ctrl.getParent();
	return isObject(%parent) ? %this.indexOf(%parent, %ctrl) : -1;
}

// Called after the move, with where the control used to be.
function GuiEditorUndoRecorder::recordMove(%this, %ctrl, %oldParent, %oldIndex, %name)
{
	if(%this.suspended || !isObject(%ctrl) || !isObject(%oldParent))
	{
		return;
	}

	%parent = %ctrl.getParent();
	if(!isObject(%parent))
	{
		return;
	}

	%index = %this.indexOf(%parent, %ctrl);
	if(%parent == %oldParent && %index == %oldIndex)
	{
		return;
	}

	%owned = %this.autoBegin((%name $= "") ? "Move Control" : %name);
	if(isObject(%this.pending))
	{
		%this.pending.addMoveOp(%ctrl, %oldParent, %oldIndex, %parent, %index);
	}
	%this.autoEnd(%owned);
}

//-----------------------------------------------------------------------------
// Frame sets, the one container that keeps a layout of its own beside the child
// list. Removing a control destroys the frame it stood in and merges the split
// into its twin, so the tree has to be kept before the removal and put back
// after -- getFrameLayout / setFrameLayout, which exist for this.
//
// The tree afterwards is only knowable once the operation is over, so what is
// taken here is the before, and the after is read when the transaction closes.
//-----------------------------------------------------------------------------

function GuiEditorUndoRecorder::watchFrameSet(%this, %parent)
{
	if(%this.suspended || !isObject(%parent) || !%parent.isMemberOfClass("GuiFrameSetCtrl"))
	{
		return;
	}

	for(%i = 0; %i < %this.watchCount; %i++)
	{
		if(%this.watchSet[%i] == %parent)
		{
			return;
		}
	}

	%i = %this.watchCount;
	%this.watchSet[%i] = %parent;
	%this.watchBefore[%i] = %parent.getFrameLayout();
	%this.watchCount = %i + 1;
}

function GuiEditorUndoRecorder::emitFrameFixes(%this, %action)
{
	for(%i = 0; %i < %this.watchCount; %i++)
	{
		%frameSet = %this.watchSet[%i];
		if(isObject(%frameSet))
		{
			%action.addFrameFix(%frameSet, %this.watchBefore[%i], %frameSet.getFrameLayout());
		}
	}

	%this.watchCount = 0;
}

// Everything a container can take from a child when it arrives, in the order
// GuiEditorUndoAction::applyLayoutFixes reads it back out. TAB-delimited
// because a position has a space in it.
function GuiEditorUndoRecorder::layoutOf(%this, %ctrl)
{
	return %ctrl.getPosition() TAB %ctrl.getExtent() TAB
		%ctrl.getFieldValue("HorizSizing") TAB %ctrl.getFieldValue("VertSizing");
}

// The edit control's trash: a SimGroup it owns and never empties, which is
// where deleteSelection puts controls instead of deleting them.
function GuiEditorUndoRecorder::trash(%this)
{
	return %this.owner.brain.getTrash();
}

function GuiEditorUndoRecorder::indexOf(%this, %parent, %ctrl)
{
	for(%i = 0; %i < %parent.getCount(); %i++)
	{
		if(%parent.getObject(%i) == %ctrl)
		{
			return %i;
		}
	}

	return -1;
}

// A record made outside any transaction is its own undo step. Returns whether
// it opened one, so the matching end only fires for the one that did.
function GuiEditorUndoRecorder::autoBegin(%this, %name)
{
	if(%this.depth > 0)
	{
		return false;
	}

	%this.begin(%name, "");
	return true;
}

function GuiEditorUndoRecorder::autoEnd(%this, %owned)
{
	if(%owned)
	{
		%this.end();
	}
}

//-----------------------------------------------------------------------------
// Geometry, which is only known to have changed once the gesture is over.
//-----------------------------------------------------------------------------

// %selection is the edit control's selected set, which is one reused SimSet
// (guiEditCtrl.cc, updateSelectedSet) - so its contents are copied out here
// rather than the set being held on to.
function GuiEditorUndoRecorder::snapshot(%this, %selection)
{
	%this.snapCount = 0;
	if(%this.suspended || !isObject(%selection))
	{
		return;
	}

	for(%i = 0; %i < %selection.getCount(); %i++)
	{
		%ctrl = %selection.getObject(%i);
		%this.snapCtrl[%i] = %ctrl;
		%this.snapPos[%i] = %ctrl.getPosition();
		%this.snapExt[%i] = %ctrl.getExtent();
	}

	%this.snapCount = %selection.getCount();
}

// Diff against the snapshot and push whatever actually moved. A mouse-down
// that selected without dragging, or a nudge into a wall, records nothing.
function GuiEditorUndoRecorder::commitGeometry(%this, %name, %kind)
{
	if(%this.suspended || %this.snapCount <= 0)
	{
		return;
	}

	// An empty name means "say what happened". The C++ knows whether the gesture
	// was a drag or a resize (mMouseDownMode) but does not pass it, and the
	// snapshot can answer it anyway.
	if(%name $= "")
	{
		%name = "Move Control";
		for(%i = 0; %i < %this.snapCount; %i++)
		{
			%ctrl = %this.snapCtrl[%i];
			if(isObject(%ctrl) && strcmp(%this.snapExt[%i], %ctrl.getExtent()) != 0)
			{
				%name = "Resize Control";
				break;
			}
		}
	}

	%this.begin(%name, %kind);
	for(%i = 0; %i < %this.snapCount; %i++)
	{
		%ctrl = %this.snapCtrl[%i];
		if(!isObject(%ctrl))
		{
			continue;
		}

		%this.recordField(%ctrl, "Position", %this.snapPos[%i], %ctrl.getPosition(), false);
		%this.recordField(%ctrl, "Extent", %this.snapExt[%i], %ctrl.getExtent(), false);
	}
	%this.end();

	%this.snapCount = 0;
}

//-----------------------------------------------------------------------------
// Hierarchy, for a rearrangement whose shape is not known until it is over.
//
// A drag in the Explorer tree can move any number of selected controls into any
// number of parents at once (GuiTreeViewCtrl::reorderFromDrag), so rather than
// try to follow it, the whole document's shape is remembered before and read
// again after. What changed is then whichever parents' child lists differ.
//-----------------------------------------------------------------------------

function GuiEditorUndoRecorder::snapshotHierarchy(%this, %root)
{
	%this.hierCount = 0;
	%this.hierCtrlCount = 0;

	// Taken here rather than in commitHierarchy: a drag that pulls a control out
	// of a frame set destroys the frame on the way, so by the time the move is
	// over the tree it came from is already gone.
	%this.watchCount = 0;

	if(%this.suspended || !isObject(%root))
	{
		return;
	}

	%this.hierWalk(%root);
}

function GuiEditorUndoRecorder::hierWalk(%this, %parent)
{
	%n = %this.hierCount;
	%this.hierParent[%n] = %parent;
	%this.hierList[%n] = %this.childList(%parent);
	%this.hierCount = %n + 1;

	for(%i = 0; %i < %parent.getCount(); %i++)
	{
		%ctrl = %parent.getObject(%i);

		// Per control as well as per parent: the lists are what gets restored,
		// but they cannot say which control the user actually dragged, and that
		// is what the selection should land on afterwards.
		%c = %this.hierCtrlCount;
		%this.hierCtrl[%c] = %ctrl;
		%this.hierCtrlParent[%c] = %parent;
		%this.hierCtrlIndex[%c] = %i;
		%this.hierCtrlLayout[%c] = %this.layoutOf(%ctrl);
		%this.hierCtrlCount = %c + 1;

		// Every frame set in the document, whether the drag turns out to touch
		// it or not: which ones it touches is not known until it is over, and by
		// then their trees have already changed.
		%this.watchFrameSet(%ctrl);

		%this.hierWalk(%ctrl);
	}
}

function GuiEditorUndoRecorder::childList(%this, %parent)
{
	%list = "";
	for(%i = 0; %i < %parent.getCount(); %i++)
	{
		%child = %parent.getObject(%i);
		%list = (%list $= "") ? %child : (%list SPC %child);
	}

	return %list;
}

function GuiEditorUndoRecorder::commitHierarchy(%this, %name)
{
	if(%this.suspended || %this.hierCount <= 0)
	{
		return;
	}

	%this.begin(%name, "");

	for(%i = 0; %i < %this.hierCount; %i++)
	{
		%parent = %this.hierParent[%i];
		if(!isObject(%parent))
		{
			continue;
		}

		%now = %this.childList(%parent);
		if(%this.hierList[%i] $= %now)
		{
			continue;
		}

		%this.pending.addOrderOp(%parent, %this.hierList[%i], %now);
	}

	// Which controls moved, for the selection after a replay.
	if(isObject(%this.pending) && !%this.pending.isEmpty())
	{
		for(%i = 0; %i < %this.hierCtrlCount; %i++)
		{
			%ctrl = %this.hierCtrl[%i];
			if(!isObject(%ctrl))
			{
				continue;
			}

			%parent = %ctrl.getParent();
			if(%parent != %this.hierCtrlParent[%i] ||
				%this.indexOf(%parent, %ctrl) != %this.hierCtrlIndex[%i])
			{
				%this.pending.noteTouched(%ctrl);

				// Dragged into a container that places its own children, its
				// layout is no longer its own - so both ends are kept and put
				// back after the lists are.
				%this.pending.addLayoutFix(%ctrl, %this.hierCtrlLayout[%i],
					%this.layoutOf(%ctrl));
			}
		}
	}

	%this.end();

	%this.hierCount = 0;
	%this.hierCtrlCount = 0;
}

//-----------------------------------------------------------------------------
// Replaying, and the state that has to be dropped when one happens.
//-----------------------------------------------------------------------------

function GuiEditorUndoRecorder::suspend(%this)
{
	%this.suspended = true;
}

function GuiEditorUndoRecorder::resume(%this)
{
	%this.suspended = false;
}

// Called by the action as it replays, both ways. Two jobs: hand GuiEditor the
// controls to re-select afterwards, and drop the coalescing state - the action
// on top of the undo stack is no longer the one a merge would be aiming at.
function GuiEditorUndoRecorder::noteReplay(%this, %action)
{
	%this.replayTouched = %action.touched();
	%this.lastAction = 0;
	%this.lastKind = "";
}

function GuiEditorUndoRecorder::clear(%this)
{
	if(isObject(%this.pending))
	{
		%this.pending.delete();
		%this.pending = 0;
	}
	%this.depth = 0;

	%manager = %this.manager();
	if(isObject(%manager))
	{
		%manager.clearAll();
	}

	%this.lastAction = 0;
	%this.lastKind = "";
	%this.snapCount = 0;
	%this.replayTouched = "";
	%this.refreshMenu();
}

//-----------------------------------------------------------------------------
// The Edit menu.
//-----------------------------------------------------------------------------

function GuiEditorUndoRecorder::undoCount(%this)
{
	%manager = %this.manager();
	return isObject(%manager) ? %manager.getUndoCount() : 0;
}

function GuiEditorUndoRecorder::redoCount(%this)
{
	%manager = %this.manager();
	return isObject(%manager) ? %manager.getRedoCount() : 0;
}

// setMenuActive walks the whole menu tree by item text and re-applies every
// item's profile, so it is worth knowing what it was last told: a run of
// nudges would otherwise do that once per key press.
function GuiEditorUndoRecorder::refreshMenu(%this)
{
	%undo = %this.undoCount();
	%redo = %this.redoCount();

	if(%undo == %this.menuUndo && %redo == %this.menuRedo)
	{
		return;
	}

	%this.menuUndo = %undo;
	%this.menuRedo = %redo;

	if(isObject(EditorCore) && isObject(EditorCore.menuBar))
	{
		EditorCore.menuBar.setMenuActive("Undo", %undo > 0);
		EditorCore.menuBar.setMenuActive("Redo", %redo > 0);
	}
}

// The menu is rebuilt-looking every time the editor is opened, and the cache
// above would otherwise decide there was nothing to say.
function GuiEditorUndoRecorder::forceRefreshMenu(%this)
{
	%this.menuUndo = -1;
	%this.menuRedo = -1;
	%this.refreshMenu();
}
