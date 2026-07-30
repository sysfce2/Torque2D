
//-----------------------------------------------------------------------------
// The Gui Editor's clipboard. Owned by GuiEditor; the only thing that holds a
// copied control, and the only thing that puts one back.
//
// A copy is a real copy, made at the moment Ctrl+C is pressed: the selection is
// deep-cloned into a SimGroup of this object's own, outside the document. That
// is what makes the clipboard a snapshot rather than a reference - editing or
// deleting the original afterwards does not change what will be pasted - and it
// is why a paste clones a second time, out of the stash, so the stash survives
// and can be pasted again.
//
// SimObject::deepClone is what does the copying (simObject.cc). Two things it
// promises are the reason a clipboard can be this small: it copies everything -
// fields, dynamic fields, the whole child tree, and a frame set's frame tree -
// and it runs no script lifecycle on the copy, so a control whose class builds
// children in onAdd is copied with the children it has rather than gaining a
// second set of them.
//
// What it deliberately does not copy is the name, because two controls
// answering to one name is a bug. The originals' names are carried across on
// the copy as a dynamic field, and paste turns them back into real names, made
// unique against the document.
//
// Holds live controls, so it has to be emptied when the profiles they wear are
// about to be freed - see GuiEditor::detachTheme, which does the same for the
// undo stack and for the same reason.
//-----------------------------------------------------------------------------

function GuiEditorClipboard::onAdd(%this)
{
	// Where copies live. A SimGroup owns what it holds, so deleting it at
	// teardown frees every copied tree with it.
	%this.stash = new SimGroup();

	%this.entryCount = 0;

	// Paste placement, counted per container: a paste into the container the copy
	// came from steps away from the original so it can be seen, and each further
	// paste into that container steps again. Kept per container rather than as one
	// running count so that pasting into a second container and coming back
	// carries on where this one left off, instead of landing on what is already
	// there.
	%this.stepCount = 0;

	// What the Edit menu was last told about Paste. setMenuActive walks the whole
	// menu tree and re-applies every item's profile, so it is worth not saying
	// the same thing twice.
	%this.menuPaste = -1;
}

function GuiEditorClipboard::onRemove(%this)
{
	if(isObject(%this.stash))
	{
		%this.stash.delete();
	}
}

//-----------------------------------------------------------------------------
// Copying.
//-----------------------------------------------------------------------------

function GuiEditorClipboard::copy(%this, %selection)
{
	if(!isObject(%selection) || %selection.getCount() == 0)
	{
		return false;
	}

	%roots = %this.topLevel(%selection);
	if(%roots $= "")
	{
		return false;
	}

	%this.clear();

	for(%i = 0; %i < getWordCount(%roots); %i++)
	{
		%ctrl = getWord(%roots, %i);
		%copy = %ctrl.deepClone();
		if(!isObject(%copy))
		{
			continue;
		}

		// Out of the Gui group a fresh control registers itself into
		// (guiControl.cc onAdd) and into the stash, which is where it stays.
		%this.stash.add(%copy);
		%this.stampNames(%ctrl, %copy);

		%n = %this.entryCount;
		%this.entry[%n] = %copy;
		%this.entrySource[%n] = %ctrl.getParent();
		%this.entryCount = %n + 1;
	}

	%this.stepCount = 0;
	%this.refreshMenu();

	return %this.entryCount > 0;
}

// The controls a copy actually has to take: those in the selection that no other
// selected control already contains. Selecting a panel and a button inside it is
// one copy, not two - the button is coming along inside the panel, and copying
// it again would paste a second one.
//
// Found by walking the document rather than reading the selection in order,
// which also settles the order: the entries come out in document order, so the
// pasted controls sit in the same z-order as the originals.
function GuiEditorClipboard::topLevel(%this, %selection)
{
	if(!isObject(%this.owner.rootGui))
	{
		return "";
	}

	return %this.collectTopLevel(%this.owner.rootGui, %selection);
}

function GuiEditorClipboard::collectTopLevel(%this, %parent, %selection)
{
	%list = "";

	for(%i = 0; %i < %parent.getCount(); %i++)
	{
		%child = %parent.getObject(%i);

		if(%selection.isMember(%child))
		{
			// Taken whole, and the walk stops here: anything selected below it is
			// part of what was taken.
			%list = (%list $= "") ? %child : (%list SPC %child);
			continue;
		}

		%deeper = %this.collectTopLevel(%child, %selection);
		if(%deeper !$= "")
		{
			%list = (%list $= "") ? %deeper : (%list SPC %deeper);
		}
	}

	return %list;
}

// Remember what every control in the copied tree was called, since deepClone
// deliberately leaves names behind. Kept on the copy itself rather than in a
// table here, so a tree of any shape carries its own answers; paste consumes the
// field and clears it.
//
// Walked by index, which pairs the two trees: a deep clone copies children in
// order, so the source's nth child and the copy's nth child are the same
// control.
function GuiEditorClipboard::stampNames(%this, %source, %copy)
{
	%name = %source.getName();
	if(%name !$= "")
	{
		%copy.clipName = %name;
	}

	%count = %source.getCount();
	for(%i = 0; %i < %count && %i < %copy.getCount(); %i++)
	{
		%this.stampNames(%source.getObject(%i), %copy.getObject(%i));
	}
}

//-----------------------------------------------------------------------------
// Pasting.
//-----------------------------------------------------------------------------

function GuiEditorClipboard::paste(%this)
{
	if(%this.isEmpty())
	{
		return false;
	}

	%addSet = %this.owner.brain.getCurrentAddSet();
	if(!isObject(%addSet))
	{
		%addSet = %this.owner.rootGui;
	}

	%offset = %this.nextOffset(%addSet);
	%pasted = "";

	// However many controls it puts back, a paste is one thing the user did and
	// so one thing they can take back. The adds inside record themselves into
	// this transaction (GuiEditorUndoRecorder::recordAdd, from the brain's
	// onAddNewCtrl).
	%this.owner.undoRecorder.begin("Paste", "");

	for(%i = 0; %i < %this.entryCount; %i++)
	{
		%entry = %this.entry[%i];
		if(!isObject(%entry))
		{
			continue;
		}

		%copy = %entry.deepClone();
		if(!isObject(%copy))
		{
			continue;
		}

		// Not everything can live everywhere: a tab page pasted onto a panel is
		// a page nothing will ever draw a tab for. The clipboard is left alone,
		// so selecting a tab book and pasting again does what was meant. The
		// clone goes, because nothing else is holding it.
		if(!%copy.canBeChildOf(%addSet))
		{
			%copy.delete();
			continue;
		}

		// Both before the control arrives, so they are part of it arriving rather
		// than a second edit on top of it - and so that everything the arrival
		// announces reads the name and the position the control is going to keep.
		%this.applyNames(%copy);
		%copy.Position = %this.pastePosition(%copy, %offset);

		// Through the brain, which is where theming on arrival, the undo record,
		// the selection and the AddControl event that refreshes the Explorer tree
		// all live. The control palette's click-to-place goes through the same
		// door for the same reason; the only thing a paste does differently is
		// place the control itself, which is why that half is not in here.
		%this.owner.brain.acceptControl(%copy);

		%pasted = (%pasted $= "") ? %copy : (%pasted SPC %copy);
	}

	%this.owner.undoRecorder.end();

	// Each arrival announced its own control as the selection, so the last one
	// would otherwise be the only one selected.
	if(%pasted !$= "")
	{
		%this.owner.brain.selectList(%pasted);
	}

	return %pasted !$= "";
}

// How far this paste steps away from where the copy was taken.
//
// Pasting into the container the control came from puts it one grid step off, so
// it is not hidden exactly behind the original; pasting somewhere else keeps the
// position it had. Every further paste into a container steps again, counted per
// container, so nothing a paste puts down is ever laid exactly on top of
// something an earlier paste put there.
function GuiEditorClipboard::nextOffset(%this, %addSet)
{
	%grid = %this.owner.brain.getGridSize();
	if(%grid <= 0)
	{
		%grid = 10;
	}

	for(%i = 0; %i < %this.stepCount; %i++)
	{
		if(%this.stepParent[%i] == %addSet)
		{
			%this.stepValue[%i]++;
			return %grid * %this.stepValue[%i];
		}
	}

	// First paste into this container. Starting at one step out only makes sense
	// where the original is; anywhere else the position it had is free.
	%n = %this.stepCount;
	%this.stepParent[%n] = %addSet;
	%this.stepValue[%n] = (%addSet == %this.entrySource[0]) ? 1 : 0;
	%this.stepCount = %n + 1;

	return %grid * %this.stepValue[%n];
}

// The position the copy takes in its new parent: the one it had in its old one,
// plus however far this paste is stepping.
//
// Local, and set before the control is added, which is the whole reason paste
// does its own placing rather than handing a point to onControlDropped. A global
// position would have to account for the border inset of the container it is
// landing in - and that inset is not knowable in advance: a control's
// mRenderInsetLT is written by its parent when the parent renders it
// (guiControl.cc renderChild), so a control that has never been drawn in this
// container does not have one yet.
//
// A container that places its own children is then free to overrule this, which
// is right: a chain or a grid decides where its children sit, and a control
// pasted into one belongs wherever the container puts it.
function GuiEditorClipboard::pastePosition(%this, %copy, %offset)
{
	%at = %copy.getPosition();

	return (getWord(%at, 0) + %offset) SPC (getWord(%at, 1) + %offset);
}

// Turn the copied names back into real ones, made unique. A control that had no
// name stays nameless.
function GuiEditorClipboard::applyNames(%this, %ctrl)
{
	%wanted = %ctrl.clipName;
	if(%wanted !$= "")
	{
		%ctrl.setEditFieldValue("name", %this.freeName(%wanted));
	}

	// Assigning "" is how a dynamic field is removed, so the marker leaves no
	// trace on the pasted control.
	%ctrl.clipName = "";

	for(%i = 0; %i < %ctrl.getCount(); %i++)
	{
		%this.applyNames(%ctrl.getObject(%i));
	}
}

// okButton -> okButton2 -> okButton3. A name that already ends in a number
// counts on from it rather than growing another digit, so a copy of okButton2 is
// okButton3 and not okButton22.
function GuiEditorClipboard::freeName(%this, %wanted)
{
	if(!%this.nameTaken(%wanted))
	{
		return %wanted;
	}

	%stem = %wanted;
	%next = 2;

	%end = strlen(%wanted) - 1;
	while(%end >= 0 && %this.isDigit(getSubStr(%wanted, %end, 1)))
	{
		%end--;
	}

	// Not a name that is nothing but digits, which has no stem to count from.
	if(%end >= 0 && %end < (strlen(%wanted) - 1))
	{
		%stem = getSubStr(%wanted, 0, %end + 1);
		%next = getSubStr(%wanted, %end + 1, strlen(%wanted)) + 1;
	}

	for(%i = %next; %i < %next + 1000; %i++)
	{
		%try = %stem @ %i;
		if(!%this.nameTaken(%try))
		{
			return %try;
		}
	}

	return "";
}

function GuiEditorClipboard::isDigit(%this, %char)
{
	return strpos("0123456789", %char) != -1;
}

// Is anything in the document called this?
//
// The document is the whole question: Sim cannot answer it, because the editor
// runs in editor mode, where assignName stashes a control's name instead of
// registering it (simObject.cc) - so isObject() on the name would say no for
// every control the user has ever named in here. A control in the trash is not
// in the document and its name is free, which is what lets a cut and paste keep
// the name it had.
function GuiEditorClipboard::nameTaken(%this, %name)
{
	if(%name $= "" || !isObject(%this.owner.rootGui))
	{
		return false;
	}

	return %this.nameTakenBelow(%this.owner.rootGui, %name);
}

function GuiEditorClipboard::nameTakenBelow(%this, %parent, %name)
{
	for(%i = 0; %i < %parent.getCount(); %i++)
	{
		%child = %parent.getObject(%i);

		// strcmp, not $=: $= is case-insensitive, and two controls whose names
		// differ only in case are two different names to everything else.
		if(strcmp(%child.getName(), %name) == 0)
		{
			return true;
		}

		if(%this.nameTakenBelow(%child, %name))
		{
			return true;
		}
	}

	return false;
}

//-----------------------------------------------------------------------------
// State, and what the Edit menu is told about it.
//-----------------------------------------------------------------------------

function GuiEditorClipboard::isEmpty(%this)
{
	for(%i = 0; %i < %this.entryCount; %i++)
	{
		if(isObject(%this.entry[%i]))
		{
			return false;
		}
	}

	return true;
}

function GuiEditorClipboard::clear(%this)
{
	for(%i = 0; %i < %this.entryCount; %i++)
	{
		if(isObject(%this.entry[%i]))
		{
			%this.entry[%i].delete();
		}
		%this.entry[%i] = "";
		%this.entrySource[%i] = "";
	}

	%this.entryCount = 0;
	%this.stepCount = 0;
	%this.refreshMenu();
}

function GuiEditorClipboard::refreshMenu(%this)
{
	%has = %this.isEmpty() ? 0 : 1;

	if(%has == %this.menuPaste)
	{
		return;
	}

	%this.menuPaste = %has;

	if(isObject(EditorCore) && isObject(EditorCore.menuBar))
	{
		EditorCore.menuBar.setMenuActive("Paste", %has);
	}
}

// The menu looks rebuilt every time the editor is opened, and the cache above
// would otherwise decide there was nothing to say.
function GuiEditorClipboard::forceRefreshMenu(%this)
{
	%this.menuPaste = -1;
	%this.refreshMenu();
}
