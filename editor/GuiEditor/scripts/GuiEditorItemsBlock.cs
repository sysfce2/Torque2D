
//-----------------------------------------------------------------------------
// The Items section of the Gui Editor's properties pane: the static rows a list
// box or a drop down is authored with.
//
// Until this existed the rows were script's alone - addItem, from an onWake
// somewhere away from the Gui that shows them - and a list laid out in the
// editor was an empty rectangle. A row typed here appears on the canvas as it is
// typed and is saved with the Gui.
//
// The block edits the list as a WHOLE. It reads it in one getItemList and writes
// it back in one setItemList, and its rows are widgets over the records that
// call returns. That is not laziness: every gesture on offer - add, remove, move
// up, move down, retype a caption - shifts what is around the row it touched, so
// a per-row write would have to know which of them it was, and an undo of one
// would have to know what the others became. A list is short.
//
// It is built ONCE, in GuiEditorInspectorPane::build, and shown or hidden per
// class - the arrangement GuiEditorDynamicFields uses, and for the reason the
// pane's header comment gives: a block rebuilt on a selection change can delete
// a control the engine is mid-dispatch on. The rows inside it are rebuilt, but
// always from a schedule(0), because the click that removes or moves one arrives
// from a button inside the row that is about to be freed.
//
// Not GuiTreeViewCtrl, though it derives from GuiListBoxCtrl: a tree generates
// its rows from a root object. See GuiEditorControlSpec::hasItemList.
//
// The creator sets pane and blockWidth inline, then calls build() once after
// adding it.
//-----------------------------------------------------------------------------

function GuiEditorItemsBlock::onAdd(%this)
{
	ThemeManager.setProfile(%this, "emptyProfile");
}

function GuiEditorItemsBlock::build(%this)
{
	%this.typing = false;
	%this.listBeforeEdit = "";
	%this.pendingFocus = false;

	// The rows in a grid rather than a chain of their own, which is what every
	// other section in this pane uses and the only thing that lays out correctly
	// inside a collapsible panel: a chain sizes itself from its children, so a
	// chain of full-width rows inside a chain inside a panel had each level
	// widening the one above it, a bit per layout pass, until the row ran off the
	// edge of the pane. A grid sizes its children from ITSELF.
	//
	// One column, because a row is a line. MaxColCount is the only thing that
	// says so: left to fit as many columns as it can, the grid would put two
	// short rows side by side the moment the Properties frame was dragged wide.
	%this.grid = %this.pane.makeCellGrid(0);
	%this.grid.MaxColCount = 1;
	%this.grid.CellSizeY = 26;
	%this.add(%this.grid);

	%this.buildAddRow();
}

// A caption box and an Add button, the shape GuiEditorDynamicFields uses. Unlike
// a dynamic field, a row with an empty caption is a legal row - a separator, or
// one whose text a script fills in later - so Add appends whatever the box holds
// and puts the caret in the new row.
function GuiEditorItemsBlock::buildAddRow(%this)
{
	%w = %this.blockWidth;
	%buttonW = 56;

	%row = new GuiControl()
	{
		HorizSizing = "width";
		Position = "0 0";
		Extent = %w SPC 30;
	};
	ThemeManager.setProfile(%row, "emptyProfile");
	%this.add(%row);
	%this.addRow = %row;

	%this.nameBox = new GuiTextEditCtrl()
	{
		HorizSizing = "width";
		Position = "4 4";
		Extent = (%w - %buttonW - 16) SPC 22;
		Tooltip = "What a new row should say. It can be left empty and typed in afterwards.";
	};
	ThemeManager.setProfile(%this.nameBox, "textEditProfile");
	ThemeManager.setProfile(%this.nameBox, "tipProfile", "TooltipProfile");
	%this.nameBox.ReturnCommand = %this.getID() @ ".onAddClicked();";
	%row.add(%this.nameBox);

	%this.addButton = new GuiButtonCtrl()
	{
		HorizSizing = "left";
		Position = (%w - %buttonW - 4) SPC 4;
		Extent = %buttonW SPC 22;
		Text = "Add";
		Command = %this.getID() @ ".onAddClicked();";
	};
	ThemeManager.setProfile(%this.addButton, "buttonProfile");
	%row.add(%this.addButton);
}

//-----------------------------------------------------------------------------
// Binding.
//-----------------------------------------------------------------------------

function GuiEditorItemsBlock::bind(%this, %ctrl)
{
	// A half-typed caption belongs to the control it was typed on. Moving the
	// selection abandons it; the control already holds every keystroke.
	%this.typing = false;
	%this.listBeforeEdit = "";

	%this.target = %ctrl;
	%this.rebuildRows();
}

// Re-read what the control holds without rebuilding, where the rows still line
// up with it. This is the undo and redo path (GuiEditorInspectorWindow::
// onReplayed): a replay can put back a caption, a switch or the whole order, and
// only the last of those changes how many rows there are.
//
// Rebuilding from here would be re-entrant - refresh() is reached from inside a
// commit - so a count that no longer matches goes through the same schedule(0)
// everything else does.
function GuiEditorItemsBlock::refresh(%this)
{
	if(!isObject(%this.target))
	{
		return;
	}

	%list = %this.target.getItemList();
	%count = (%list $= "") ? 0 : getRecordCount(%list);

	if(%count != %this.grid.getCount())
	{
		%this.rebuildDeferred();
		return;
	}

	for(%i = 0; %i < %count; %i++)
	{
		%this.grid.getObject(%i).setRecord(getRecord(%list, %i));
	}

	%this.refreshArrows();
}

function GuiEditorItemsBlock::rebuildRows(%this)
{
	%this.grid.deleteObjects();

	if(isObject(%this.target))
	{
		%list = %this.target.getItemList();
		%count = (%list $= "") ? 0 : getRecordCount(%list);
		for(%i = 0; %i < %count; %i++)
		{
			%this.makeRow(getRecord(%list, %i));
		}
	}

	%this.refreshArrows();
}

function GuiEditorItemsBlock::makeRow(%this, %record)
{
	%row = new GuiControl()
	{
		class = "GuiEditorItemRow";
		HorizSizing = "width";
		Position = "0 0";
		Extent = %this.blockWidth SPC 26;
		owner = %this;
	};

	// Added before build(), because the grid sizes a cell the moment it takes
	// one and the row lays itself out against the width it actually has. The
	// nominal blockWidth above is only what it starts at.
	%this.grid.add(%row);
	%row.build();
	%row.setRecord(%record);

	return %row;
}

// The first row cannot go up and the last cannot go down, so those two arrows
// say so rather than being clickable and doing nothing.
function GuiEditorItemsBlock::refreshArrows(%this)
{
	%count = %this.grid.getCount();
	for(%i = 0; %i < %count; %i++)
	{
		%row = %this.grid.getObject(%i);
		%row.upButton.setActive(%i > 0);
		%row.downButton.setActive(%i < (%count - 1));
	}
}

// Deferred, always: a remove or a move arrives from a button inside the row that
// is about to be freed, and an add arrives from a button whose own Command is
// still running.
function GuiEditorItemsBlock::rebuildDeferred(%this)
{
	%this.schedule(0, "rebuildNow");
}

// Only the deferred path re-measures the section, never bind(): a bind ends with
// the pane's own forceLayout, and forceLayout nudges the pane's width by a pixel
// and back. An expanded GuiPanelCtrl takes the nudge up and does not give it
// back, so a second one in the same bind left the section a pixel wider than the
// pane every time a control was selected -- and the row's right-hand icons were
// what fell off the edge.
function GuiEditorItemsBlock::rebuildNow(%this)
{
	%this.rebuildRows();
	%this.notifyResized();

	// A row added by the Add button is one the user is about to type into.
	if(%this.pendingFocus)
	{
		%this.pendingFocus = false;
		%count = %this.grid.getCount();
		if(%count > 0)
		{
			%this.grid.getObject(%count - 1).setCaretHere();
		}
	}
}

//-----------------------------------------------------------------------------
// The list, as the rows currently spell it.
//-----------------------------------------------------------------------------

// What the ROWS say, for the three edits whose new value exists only in a widget:
// a retyped caption, an ID, a flipped switch.
//
// Read off the chain rather than an index of its own, so the order the rows are
// in and the order they are written in cannot disagree.
function GuiEditorItemsBlock::collect(%this)
{
	%list = "";
	%count = %this.grid.getCount();
	for(%i = 0; %i < %count; %i++)
	{
		%record = %this.grid.getObject(%i).getRecord();
		%list = (%i == 0) ? %record : (%list NL %record);
	}

	return %list;
}

// What the CONTROL says, which is what adding, removing and moving work from.
//
// Not collect(), because the rows lag: every one of those three ends in a
// rebuild that has to be deferred, so between the write and the next tick the
// chain still shows the list as it was. Two clicks on Add inside one frame read
// the same stale chain and the first row added was lost. The control is never
// stale - it was written before the rebuild was even scheduled - and a box the
// user was typing in has already committed on the way out of it, because taking
// focus is what AltCommand fires on.
function GuiEditorItemsBlock::currentList(%this)
{
	return isObject(%this.target) ? %this.target.getItemList() : "";
}

// Whether the chain can still be trusted to say which row is which. False only
// inside the window a deferred rebuild has not closed yet, where an index into
// the chain would name the wrong record.
function GuiEditorItemsBlock::rowsAreCurrent(%this)
{
	%list = %this.currentList();
	%count = (%list $= "") ? 0 : getRecordCount(%list);

	return %count == %this.grid.getCount();
}

function GuiEditorItemsBlock::indexOf(%this, %row)
{
	%count = %this.grid.getCount();
	for(%i = 0; %i < %count; %i++)
	{
		if(%this.grid.getObject(%i) == %row)
		{
			return %i;
		}
	}

	return -1;
}

// One write, one undo step. The recorder does the writing, so a write that was
// not recorded is a write that did not happen.
function GuiEditorItemsBlock::writeList(%this, %list, %name)
{
	if(!isObject(%this.target))
	{
		return;
	}

	GuiEditor.undoRecorder.begin(%name, "");
	GuiEditor.undoRecorder.writeItems(%this.target, %list);
	GuiEditor.undoRecorder.end();

	%this.pane.afterCommit();
}

//-----------------------------------------------------------------------------
// Editing.
//-----------------------------------------------------------------------------

// A caption keystroke. Straight onto the control rather than through the
// recorder, because this runs per character and an edit is one change however
// many keys it took; the undo step is written once, on commit, from the list
// stashed here.
function GuiEditorItemsBlock::onItemRowTyped(%this, %row)
{
	if(%this.populating || !isObject(%this.target))
	{
		return;
	}

	// Taken on the first keystroke, because that is the last moment the control
	// still holds what the edit started from.
	if(!%this.typing)
	{
		%this.typing = true;
		%this.listBeforeEdit = %this.target.getItemList();
	}

	%this.target.setItemList(%this.collect());
	%this.pane.afterCommit();
}

function GuiEditorItemsBlock::onItemRowCommit(%this, %row)
{
	if(%this.populating || !isObject(%this.target))
	{
		return;
	}

	%wasTyping = %this.typing;
	%before = %this.listBeforeEdit;
	%this.typing = false;
	%this.listBeforeEdit = "";

	if(!%row.hasChanged())
	{
		return;
	}
	%row.markClean();

	// Put back what the edit started from, so the one write below is the whole
	// of it. Without this the recorder would compare the control against itself
	// and find nothing to record.
	if(%wasTyping)
	{
		%this.target.setItemList(%before);
	}

	%this.writeList(%this.collect(), "Edit Item");
}

function GuiEditorItemsBlock::onItemRowToggled(%this, %row, %name)
{
	if(%this.populating || !isObject(%this.target) || !%this.rowsAreCurrent())
	{
		return;
	}

	%row.markClean();

	%list = %this.collect();

	// Only one row can start selected on a list that only allows one selection.
	// The engine's setItemList restores exactly what it is given - it has to, or
	// an undo could not put a multi-selection back - so the rule belongs here.
	if(%name $= "selected" && %row.selectedToggle.getValue() &&
		!%this.target.getFieldValue("AllowMultipleSelections"))
	{
		%list = %this.clearOtherSelections(%list, %this.indexOf(%row));
	}

	%this.writeList(%list, "Change Item");

	// The rows have to catch up with a selection that was taken off them.
	if(%name $= "selected")
	{
		%this.rebuildDeferred();
	}
}

function GuiEditorItemsBlock::clearOtherSelections(%this, %list, %keepIndex)
{
	%out = "";
	%count = getRecordCount(%list);
	for(%i = 0; %i < %count; %i++)
	{
		%record = getRecord(%list, %i);
		if(%i != %keepIndex)
		{
			%record = setField(%record, 3, 0);
		}
		%out = (%i == 0) ? %record : (%out NL %record);
	}

	return %out;
}

function GuiEditorItemsBlock::onItemRowMove(%this, %row, %delta)
{
	if(!isObject(%this.target) || !%this.rowsAreCurrent())
	{
		return;
	}

	%index = %this.indexOf(%row);
	%swapWith = %index + %delta;
	if(%index == -1 || %swapWith < 0 || %swapWith >= %this.grid.getCount())
	{
		return;
	}

	%list = %this.currentList();
	%out = "";
	%count = getRecordCount(%list);
	for(%i = 0; %i < %count; %i++)
	{
		%pick = %i;
		if(%i == %index)
		{
			%pick = %swapWith;
		}
		else if(%i == %swapWith)
		{
			%pick = %index;
		}

		%record = getRecord(%list, %pick);
		%out = (%i == 0) ? %record : (%out NL %record);
	}

	%this.writeList(%out, "Move Item");
	%this.rebuildDeferred();
}

function GuiEditorItemsBlock::onItemRowRemove(%this, %row)
{
	if(!isObject(%this.target) || !%this.rowsAreCurrent())
	{
		return;
	}

	%index = %this.indexOf(%row);
	if(%index == -1)
	{
		return;
	}

	%list = %this.currentList();
	%out = "";
	%count = getRecordCount(%list);
	for(%i = 0; %i < %count; %i++)
	{
		if(%i == %index)
		{
			continue;
		}

		%record = getRecord(%list, %i);
		%out = (%out $= "") ? %record : (%out NL %record);
	}

	%this.writeList(%out, "Remove Item");
	%this.rebuildDeferred();
}

function GuiEditorItemsBlock::onAddClicked(%this)
{
	if(!isObject(%this.target))
	{
		return;
	}

	// The defaults an LBItem is born with: no ID, active, unselected, and no
	// color dot.
	%record = trim(%this.nameBox.getText()) TAB "0" TAB "1" TAB "0" TAB "0" TAB "1 1 1 1";

	%list = %this.currentList();
	%list = (%list $= "") ? %record : (%list NL %record);

	%this.nameBox.setText("");
	%this.writeList(%list, "Add Item");

	%this.pendingFocus = true;
	%this.rebuildDeferred();
}

// A row was added or taken away, so the section's height changed under the
// chain. Nothing above it moved, but the panel has to be told to re-measure.
function GuiEditorItemsBlock::notifyResized(%this)
{
	if(isObject(%this.pane))
	{
		%this.pane.onItemsChanged();
	}
}
