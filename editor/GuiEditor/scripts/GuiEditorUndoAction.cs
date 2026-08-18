
//-----------------------------------------------------------------------------
// One undoable step. Built by GuiEditorUndoRecorder, which is the only thing
// that creates one, and handed to the UndoManager the Gui Editor's GuiEditCtrl
// already owns (guiEditCtrl.cc, getUndoManager). UndoScriptAction is the engine
// class that exists for exactly this - its undo() and redo() call straight back
// into script (collection/undo.h).
//
// An action holds an ordered list of ops rather than one change, because a
// single gesture is several changes: dropping a control moves it into the add
// set, then the theme applier writes a profile into every slot it has, then its
// position is set. That is one Ctrl+Z, so it is one action.
//
//   field    a persist field write. setEditFieldValue, not assignment, so the
//            control sleeps and wakes around it and protected fields run their
//            setters - Position and Extent are protected (guiControl.h), so a
//            geometry op really does resize the control rather than poke
//            mBounds behind its back.
//   dynamic  a dynamic field write, which has no edit-field path.
//   move     a change of parent, of index within the parent, or both. The
//            trash is an ordinary parent here: it is a real SimGroup the edit
//            control owns and never empties, so a deleted control is still a
//            live object with a home, and undoing a delete is the same
//            operation as undoing anything else that moved.
//   items    a list box or drop down's whole set of static rows, before and
//            after. Whole for the same reason an order op is: every gesture the
//            Items pane offers can shift the rows around the one it touched.
//   order    one parent's whole list of children, before and after. Restoring
//            a list is order-independent and cannot be got subtly wrong, which
//            a pile of per-control indices can: every index a move op restores
//            shifts the siblings around it, so a rearrangement that touched
//            several of them at once depends on replay order. A drag in the
//            Explorer tree can move any number of controls between any number
//            of parents, so it records lists.
//
// undo() replays the list backwards writing the before values, redo() forwards
// writing the after values. Every op re-checks its objects: an action can
// outlive the controls it names.
//-----------------------------------------------------------------------------

function GuiEditorUndoAction::onAdd(%this)
{
	%this.opCount = 0;
	%this.fixCount = 0;
	%this.frameCount = 0;
	%this.touchList = "";
}

// Where a control's layout has to end up once the ops have run.
//
// A container that places its own children takes their layout from them when
// they arrive, and every one of them takes something different: a GuiChainCtrl
// zeroes the position outright while the editor is open, a GuiGridCtrl forces
// the sizing off center and fill, a GuiFrameSetCtrl forces it off center and
// then resizes the control to its frame (guiChainCtrl.cc / guiGridCtrl.cc /
// guiFrameSetCtrl.cc, onChildAdded). All of it is right for a control being
// dropped in and wrong for one being put back, which knows what it was.
//
// So a structural op remembers all four of the fields a container can take -
// position, extent, and the two sizing modes - at each end, and writes them
// again afterwards. Afterwards is the point: recording them as ordinary field
// ops would not do, because ops replay forwards for a redo and backwards for an
// undo, and the layout has to be written after the move in both directions.
//
// %before and %after are TAB-delimited, from GuiEditorUndoRecorder::layoutOf.
function GuiEditorUndoAction::addLayoutFix(%this, %ctrl, %before, %after)
{
	%i = %this.fixCount;
	%this.fixCtrl[%i] = %ctrl;
	%this.fixBefore[%i] = %before;
	%this.fixAfter[%i] = %after;
	%this.fixCount = %i + 1;
}

// The same idea one level up, for the one container that keeps a layout of its
// own beside the child list. A GuiFrameSetCtrl destroys a frame when the
// control in it is removed and merges the split into its twin, so putting the
// control back is not enough - the frame it stood in has to be rebuilt too, and
// the sibling that swallowed the space given it back.
function GuiEditorUndoAction::addFrameFix(%this, %frameSet, %before, %after)
{
	%i = %this.frameCount;
	%this.frameSet[%i] = %frameSet;
	%this.frameBefore[%i] = %before;
	%this.frameAfter[%i] = %after;
	%this.frameCount = %i + 1;
}

//-----------------------------------------------------------------------------
// Recording.
//-----------------------------------------------------------------------------

function GuiEditorUndoAction::addFieldOp(%this, %ctrl, %field, %before, %after, %isDynamic)
{
	%i = %this.opCount;
	%this.opType[%i] = %isDynamic ? "dynamic" : "field";
	%this.opCtrl[%i] = %ctrl;
	%this.opField[%i] = %field;
	%this.opBefore[%i] = %before;
	%this.opAfter[%i] = %after;
	%this.opCount = %i + 1;
}

function GuiEditorUndoAction::addMoveOp(%this, %ctrl, %oldParent, %oldIndex, %newParent, %newIndex)
{
	%i = %this.opCount;
	%this.opType[%i] = "move";
	%this.opCtrl[%i] = %ctrl;
	%this.opOldParent[%i] = %oldParent;
	%this.opOldIndex[%i] = %oldIndex;
	%this.opNewParent[%i] = %newParent;
	%this.opNewIndex[%i] = %newIndex;
	%this.opCount = %i + 1;
}

// %before and %after are lists of child ids. The parent is the op's object.
function GuiEditorUndoAction::addOrderOp(%this, %parent, %before, %after)
{
	%i = %this.opCount;
	%this.opType[%i] = "order";
	%this.opCtrl[%i] = %parent;
	%this.opBefore[%i] = %before;
	%this.opAfter[%i] = %after;
	%this.opCount = %i + 1;
}

// %before and %after are whole item lists, from GuiListBoxCtrl::getItemList.
// Like an order op it names no field, so it holds the same two slots a field op
// does and findOp matches it on the object alone.
function GuiEditorUndoAction::addItemsOp(%this, %ctrl, %before, %after)
{
	%i = %this.opCount;
	%this.opType[%i] = "items";
	%this.opCtrl[%i] = %ctrl;
	%this.opBefore[%i] = %before;
	%this.opAfter[%i] = %after;
	%this.opCount = %i + 1;
}

function GuiEditorUndoAction::isEmpty(%this)
{
	return %this.opCount <= 0;
}

// Name a control the user should be looking at after this action replays, for
// the cases where the ops do not say it themselves - an order op names the
// parent whose children were rearranged, not the control that was dragged.
function GuiEditorUndoAction::noteTouched(%this, %ctrl)
{
	if(!isObject(%ctrl) || %this.listHas(%this.touchList, %ctrl))
	{
		return;
	}

	%this.touchList = (%this.touchList $= "") ? %ctrl : (%this.touchList SPC %ctrl);
}

// Fold another action's ops into this one, for coalescing a run of nudges into
// the single move the user thinks they made. An op that touches something this
// action already touched updates that op's after value and keeps its before -
// so ten nudges read as one move from where the control started to where it
// ended up. Anything new is appended.
function GuiEditorUndoAction::mergeFrom(%this, %other)
{
	for(%i = 0; %i < %other.opCount; %i++)
	{
		%found = %this.findOp(%other.opType[%i], %other.opCtrl[%i], %other.opField[%i]);
		if(%found == -1)
		{
			if(%other.opType[%i] $= "move")
			{
				%this.addMoveOp(%other.opCtrl[%i], %other.opOldParent[%i], %other.opOldIndex[%i],
					%other.opNewParent[%i], %other.opNewIndex[%i]);
			}
			else if(%other.opType[%i] $= "order")
			{
				%this.addOrderOp(%other.opCtrl[%i], %other.opBefore[%i], %other.opAfter[%i]);
			}
			else if(%other.opType[%i] $= "items")
			{
				%this.addItemsOp(%other.opCtrl[%i], %other.opBefore[%i], %other.opAfter[%i]);
			}
			else
			{
				%this.addFieldOp(%other.opCtrl[%i], %other.opField[%i], %other.opBefore[%i],
					%other.opAfter[%i], %other.opType[%i] $= "dynamic");
			}
			continue;
		}

		if(%other.opType[%i] $= "move")
		{
			%this.opNewParent[%found] = %other.opNewParent[%i];
			%this.opNewIndex[%found] = %other.opNewIndex[%i];
		}
		else if(%other.opType[%i] $= "order")
		{
			%this.opAfter[%found] = %other.opAfter[%i];
		}
		else
		{
			%this.opAfter[%found] = %other.opAfter[%i];
		}
	}
}

function GuiEditorUndoAction::findOp(%this, %type, %ctrl, %field)
{
	for(%i = 0; %i < %this.opCount; %i++)
	{
		if(%this.opType[%i] !$= %type || %this.opCtrl[%i] != %ctrl)
		{
			continue;
		}

		// A move, an order or an items op names no field, so matching the object
		// - the control for one, the parent for the other - is the whole test.
		if(%type $= "move" || %type $= "order" || %type $= "items" ||
			%this.opField[%i] $= %field)
		{
			return %i;
		}
	}

	return -1;
}

// Every control this action names, once each, in the order they were recorded.
// The recorder re-selects these after a replay so the user can see what a
// Ctrl+Z did.
function GuiEditorUndoAction::touched(%this)
{
	if(%this.touchList !$= "")
	{
		return %this.touchList;
	}

	%list = "";
	for(%i = 0; %i < %this.opCount; %i++)
	{
		%ctrl = %this.opCtrl[%i];
		if(%ctrl $= "" || %this.listHas(%list, %ctrl))
		{
			continue;
		}
		%list = (%list $= "") ? %ctrl : (%list SPC %ctrl);
	}

	return %list;
}

function GuiEditorUndoAction::listHas(%this, %list, %item)
{
	for(%i = 0; %i < getWordCount(%list); %i++)
	{
		if(getWord(%list, %i) == %item)
		{
			return true;
		}
	}

	return false;
}

//-----------------------------------------------------------------------------
// Replaying. Called by the UndoManager (collection/undo.h, UndoScriptAction).
//-----------------------------------------------------------------------------

function GuiEditorUndoAction::undo(%this)
{
	%this.replay(false);
}

function GuiEditorUndoAction::redo(%this)
{
	%this.replay(true);
}

// Backwards for an undo: the ops were recorded in the order they happened, and
// unwinding them in that same order would replay a control's second write
// before its first.
function GuiEditorUndoAction::replay(%this, %forward)
{
	// The recorder that built this action, handed over when it did. It outlives
	// the stack it filled, but check anyway: an action can still be replayed
	// while the editor is being torn down around it.
	// With the direction, because the recorder tracks which state the document is
	// in and that is the only thing that says which way it just moved.
	if(isObject(%this.recorder))
	{
		%this.recorder.noteReplay(%this, %forward);
	}

	if(%forward)
	{
		for(%i = 0; %i < %this.opCount; %i++)
		{
			%this.applyOp(%i, true);
		}
	}
	else
	{
		for(%i = %this.opCount - 1; %i >= 0; %i--)
		{
			%this.applyOp(%i, false);
		}
	}

	%this.applyLayoutFixes(%forward);
	%this.applyFrameFixes(%forward);
	%this.notifyParents(%forward);
}

// After the layout fixes, because rebuilding the tree lays the children out
// from their frames -- which is the frame set's answer, and it outranks the
// control's own.
function GuiEditorUndoAction::applyFrameFixes(%this, %forward)
{
	for(%i = 0; %i < %this.frameCount; %i++)
	{
		%frameSet = %this.frameSet[%i];
		if(!isObject(%frameSet))
		{
			continue;
		}

		%frameSet.setFrameLayout(%forward ? %this.frameAfter[%i] : %this.frameBefore[%i]);
	}
}

function GuiEditorUndoAction::applyLayoutFixes(%this, %forward)
{
	for(%i = 0; %i < %this.fixCount; %i++)
	{
		%ctrl = %this.fixCtrl[%i];
		if(!isObject(%ctrl))
		{
			continue;
		}

		%layout = %forward ? %this.fixAfter[%i] : %this.fixBefore[%i];

		// The sizing modes first: a control still set to center or fill
		// overrides any position or extent written to it, so writing the
		// geometry while the old mode is still on would not stick.
		%ctrl.setEditFieldValue("HorizSizing", getField(%layout, 2));
		%ctrl.setEditFieldValue("VertSizing", getField(%layout, 3));
		%ctrl.setEditFieldValue("Position", getField(%layout, 0));
		%ctrl.setEditFieldValue("Extent", getField(%layout, 1));
	}
}

// A container lays its children out in list order, and the SimSet ordering
// methods change that order without telling it - which is why a control put
// back into a chain kept the slot it briefly held at the end of the list.
// Adding and removing a child notify on their own; being rearranged does not.
//
// Only the parent the control ends up in needs telling: the one it came from
// hears about it through onChildRemoved.
function GuiEditorUndoAction::notifyParents(%this, %forward)
{
	%told = "";

	for(%i = 0; %i < %this.opCount; %i++)
	{
		%type = %this.opType[%i];
		if(%type $= "order")
		{
			%parent = %this.opCtrl[%i];
		}
		else if(%type $= "move")
		{
			%parent = %forward ? %this.opNewParent[%i] : %this.opOldParent[%i];
		}
		else
		{
			continue;
		}

		// The trash is a plain SimGroup, and lays nothing out.
		if(!isObject(%parent) || !%parent.isMemberOfClass("GuiControl") ||
			%this.listHas(%told, %parent))
		{
			continue;
		}

		%told = (%told $= "") ? %parent : (%told SPC %parent);
		%parent.childrenReordered();
	}
}

function GuiEditorUndoAction::applyOp(%this, %i, %forward)
{
	%ctrl = %this.opCtrl[%i];
	if(!isObject(%ctrl))
	{
		return;
	}

	%type = %this.opType[%i];

	// Rebuilding a list: adding each child in turn and pushing it to the back
	// leaves the parent holding exactly the recorded order. add() is what makes
	// this cover reparenting too - a control listed by one parent is taken from
	// whichever other one currently holds it - and it means the order the ops
	// replay in cannot matter, since every control appears in exactly one list.
	if(%type $= "order")
	{
		%list = %forward ? %this.opAfter[%i] : %this.opBefore[%i];
		for(%w = 0; %w < getWordCount(%list); %w++)
		{
			%child = getWord(%list, %w);
			if(!isObject(%child))
			{
				continue;
			}
			%ctrl.add(%child);
			%ctrl.pushToBack(%child);
		}
		return;
	}

	// A list box or drop down's static rows, whole. setItemList replaces the lot
	// and resizes the control, which is the same thing the pane's every gesture
	// does, so a replay needs nothing else.
	if(%type $= "items")
	{
		%ctrl.setItemList(%forward ? %this.opAfter[%i] : %this.opBefore[%i]);
		return;
	}

	if(%type $= "move")
	{
		%parent = %forward ? %this.opNewParent[%i] : %this.opOldParent[%i];
		%index = %forward ? %this.opNewIndex[%i] : %this.opOldIndex[%i];
		if(isObject(%parent))
		{
			%this.placeAt(%parent, %ctrl, %index);
		}
		return;
	}

	%value = %forward ? %this.opAfter[%i] : %this.opBefore[%i];

	// A profile field holds an id, and a Profile Editor revert frees and reloads
	// the theme's profiles - so an id recorded before that no longer resolves.
	// The recorder clears the stack on those paths; this is the belt to its
	// braces, because writing a dead id would put the control on nothing.
	if(%ctrl.getFieldType(%this.opField[%i]) $= "GuiProfile" && !isObject(%value))
	{
		warn("Gui Editor: skipping undo of " @ %this.opField[%i] @ " - the profile it named is gone.");
		return;
	}

	if(%type $= "dynamic")
	{
		%ctrl.setFieldValue(%this.opField[%i], %value);
		return;
	}

	%ctrl.setEditFieldValue(%this.opField[%i], %value);
}

// Put %ctrl in %parent at %index, which is the whole of what a move op does.
// An index of -1 means "wherever" - used for the trash, where order is not a
// thing anyone can see.
//
// add() is a no-op when the control is already a child (SimGroup::addObject
// returns early on obj->mGroup == this), so the same call covers a reparent and
// a reorder within one parent. reorderChild inserts before its target, which is
// why moving down the list has to aim one past the index: erasing the control
// first shifts everything below it up by one.
function GuiEditorUndoAction::placeAt(%this, %parent, %ctrl, %index)
{
	%parent.add(%ctrl);

	if(%index < 0)
	{
		return;
	}

	%count = %parent.getCount();
	if(%index >= %count - 1)
	{
		%parent.pushToBack(%ctrl);
		return;
	}

	%current = %this.indexOf(%parent, %ctrl);
	if(%current == %index || %current == -1)
	{
		return;
	}

	%target = (%current > %index) ? %parent.getObject(%index) : %parent.getObject(%index + 1);
	%parent.reorderChild(%ctrl, %target);
}

function GuiEditorUndoAction::indexOf(%this, %parent, %ctrl)
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
