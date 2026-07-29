//-----------------------------------------------------------------------------
// Undo / redo in the Gui Editor. Boots the editor, opens the PlanetX project so
// there is a real theme to work with, and puts every kind of edit through a
// round trip: does undo put it back, does redo do it again, and - the part that
// is easy to get wrong - is one thing the user did one step on the stack.
//
// Stack depth is checked as often as the values are. A gesture that records
// eleven steps is as broken as one that records none, and only the count says
// so. Run: tests/run.ps1 undo ; grep UNDO in console.log.
//-----------------------------------------------------------------------------

setLogMode(2);
setScriptExecEcho(false);
trace(false);
$Scripts::ignoreDSOs = true;
setCompanyAndProduct("Torque Game Engines", "Torque2D");
ModuleDatabase.EchoInfo = false;
AssetDatabase.EchoInfo = false;
AssetDatabase.IgnoreAutoUnload = true;

testExec("editor/main.cs");

function uCheck(%label, %condition)
{
	echo(%condition ? ("UNDO PASS: " @ %label) : ("UNDO FAIL: " @ %label));
}

function uUndoCount()
{
	return GuiEditor.undoRecorder.undoCount();
}

function uRedoCount()
{
	return GuiEditor.undoRecorder.redoCount();
}

// Every pane write needs a bound target, and a replay moves the selection - so
// the pane is re-bound before each case rather than once.
function uBind(%ctrl)
{
	GuiEditor.inspectorWindow.pane.bind(%ctrl);
	return GuiEditor.inspectorWindow.pane;
}

function uIndexOf(%parent, %ctrl)
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

function uSelect(%ctrl)
{
	GuiEditor.brain.clearSelection();
	GuiEditor.brain.select(%ctrl);
}

// Does the control really carry this dynamic field? Comparing against "" proves
// nothing: an absent field and an empty one read back the same, because an empty
// one is exactly what the engine deletes.
function uHasDynamicField(%ctrl, %name)
{
	for(%i = 0; %i < %ctrl.getDynamicFieldCount(); %i++)
	{
		if(getWord(%ctrl.getDynamicField(%i), 0) $= %name)
		{
			return true;
		}
	}
	return false;
}

schedule(2000, 0, "uStep1");

function uStep1()
{
	ProjectManager.setProjectFolder("PlanetX");
	ModuleDatabase.ScanModules("PlanetX");
	ModuleDatabase.LoadExplicit("AppCore", 1);

	// By id: a bare identifier in TorqueScript is a string, and everything here
	// compares object handles.
	$uTheme = nameToID("PlanetX");
	uCheck("PlanetX theme loaded", isObject($uTheme));

	GuiEditor.open();
	uCheck("the editor built a recorder", isObject(GuiEditor.undoRecorder));
	uCheck("which found the brain's UndoManager", isObject(GuiEditor.undoRecorder.manager()));

	// The stage: a panel with three children, which is enough to test index
	// restoration without being hard to read.
	$uPanel = new GuiControl() { Position = "10 10"; Extent = "400 300"; };
	GuiEditor.rootGui.add($uPanel);

	$uA = new GuiButtonCtrl() { Position = "10 10"; Extent = "80 30"; Text = "A"; };
	$uPanel.add($uA);
	$uB = new GuiButtonCtrl() { Position = "10 50"; Extent = "80 30"; Text = "B"; };
	$uPanel.add($uB);
	$uC = new GuiButtonCtrl() { Position = "10 90"; Extent = "80 30"; Text = "C"; };
	$uPanel.add($uC);

	$uOther = new GuiControl() { Position = "10 320"; Extent = "200 100"; };
	GuiEditor.rootGui.add($uOther);

	GuiEditor.setTheme($uTheme, false);

	GuiEditor.undoRecorder.clear();
	uCheck("the stack starts empty", uUndoCount() == 0 && uRedoCount() == 0);

	schedule(300, 0, "uStepFields");
}

//-----------------------------------------------------------------------------
// Field edits, which all arrive through GuiEditorInspectorPane::writeField.
//-----------------------------------------------------------------------------

function uStepFields()
{
	GuiEditor.undoRecorder.clear();

	// Geometry. Position and Extent are protected fields, so writing them runs
	// setPosition/setExtent rather than poking mBounds - which is what makes an
	// undone resize a real resize.
	%pane = uBind($uA);
	%before = $uA.getExtent();
	%pane.writeField("Extent", "120 40");

	uCheck("a field write is one step", uUndoCount() == 1);
	uCheck("and it reached the control", $uA.getExtent() $= "120 40");

	GuiEditor.Undo();
	uCheck("undo put the extent back", $uA.getExtent() $= %before);
	uCheck("and moved the step to the redo stack", uUndoCount() == 0 && uRedoCount() == 1);

	GuiEditor.Redo();
	uCheck("redo did it again", $uA.getExtent() $= "120 40");
	uCheck("and moved it back", uUndoCount() == 1 && uRedoCount() == 0);

	// A profile slot, which is recorded by id because a profile made this
	// session carries a name the Sim never registered.
	%pane = uBind($uA);
	%pane.writeField("Profile", PlanetXPanelProfile.getId());
	uCheck("a profile write is one step", uUndoCount() == 2);
	uCheck("and it landed", $uA.getFieldValue("Profile") $= "PlanetXPanelProfile");

	GuiEditor.Undo();
	uCheck("undo put the old profile back",
		$uA.getFieldValue("Profile") $= "PlanetXButtonProfile");
	GuiEditor.Redo();
	uCheck("and redo re-applied it", $uA.getFieldValue("Profile") $= "PlanetXPanelProfile");
	GuiEditor.Undo();

	// A toggle.
	%pane = uBind($uB);
	%pane.writeField("Visible", false);
	uCheck("a toggle is one step", uUndoCount() == 2);
	uCheck("and it took", !$uB.Visible);
	GuiEditor.Undo();
	uCheck("undo turned it back on", $uB.Visible);

	// A write that changes nothing is not a step.
	%depth = uUndoCount();
	%pane = uBind($uB);
	%pane.writeField("Visible", true);
	uCheck("writing the value it already had records nothing", uUndoCount() == %depth);

	// A dynamic field, which has no setEditFieldValue path of its own.
	GuiEditor.undoRecorder.writeDynamicField($uB, "uSmokeTag", "hello");
	uCheck("a dynamic field write is one step", uUndoCount() == %depth + 1);
	uCheck("and the field exists", uHasDynamicField($uB, "uSmokeTag"));

	GuiEditor.Undo();
	uCheck("undo removed the dynamic field", !uHasDynamicField($uB, "uSmokeTag"));
	GuiEditor.Redo();
	uCheck("redo put it back", $uB.uSmokeTag $= "hello");
	GuiEditor.Undo();

	schedule(300, 0, "uStepSelection");
}

//-----------------------------------------------------------------------------
// What the user is looking at after a replay.
//
// Changing a setting and pressing Ctrl+Z is the commonest undo there is, and the
// control whose setting it was is the one already selected -- so the properties
// pane has to still be on it, with the old value back in the row. Losing the
// selection there empties the whole panel, which reads as though the undo did
// something much larger than it did.
//-----------------------------------------------------------------------------

function uStepSelection()
{
	GuiEditor.undoRecorder.clear();

	uSelect($uC);
	%pane = GuiEditor.inspectorWindow.pane;
	uCheck("selecting a control put it in the pane", %pane.target == $uC);

	%before = $uC.getExtent();
	%pane.writeField("Extent", "150 60");

	GuiEditor.Undo();

	%selection = GuiEditor.brain.getSelected();
	uCheck("undo kept the selection", %selection.getCount() == 1);
	uCheck("and kept it on the control that changed", %selection.getObject(0) == $uC);
	uCheck("the pane is still showing that control", %pane.target == $uC);
	uCheck("and the Explorer row is still selected",
		GuiEditor.explorerWindow.tree.getSelCount() == 1);
	uCheck("the control's value went back", $uC.getExtent() $= %before);
	uCheck("and the row it is shown in went back with it",
		%pane.header.extentRow.getValue() $= %before);

	GuiEditor.Redo();
	uCheck("redo leaves it selected too", %pane.target == $uC);
	uCheck("with the row showing the new value",
		%pane.header.extentRow.getValue() $= "150 60");
	GuiEditor.Undo();

	// An undo whose controls are not the ones selected moves the selection to
	// them, which is the other half of the same promise: you see what changed.
	GuiEditor.undoRecorder.clear();
	uSelect($uA);
	%pane.writeField("Visible", false);

	uSelect($uC);
	uCheck("the selection moved away", %pane.target == $uC);

	GuiEditor.Undo();
	%selection = GuiEditor.brain.getSelected();
	uCheck("undo selected what it actually changed", %selection.getObject(0) == $uA);
	uCheck("and the pane followed", %pane.target == $uA);

	// Two controls, from one step that moved both. A nudge rather than an align:
	// aligning left never moves the leftmost control, so only one of the two
	// would have changed - and the selection lands on what changed.
	GuiEditor.undoRecorder.clear();
	GuiEditor.brain.clearSelection();
	GuiEditor.brain.select($uA);
	GuiEditor.brain.addSelection($uC);
	GuiEditor.brain.moveSelection(1, 0);

	GuiEditor.Undo();
	%selection = GuiEditor.brain.getSelected();
	uCheck("undoing a two-control step selects both", %selection.getCount() == 2);

	schedule(300, 0, "uStepSizing");
}

//-----------------------------------------------------------------------------
// Sizing, which is a field write that moves the control.
//
// Setting an axis to center or fill hands that axis's geometry to the engine,
// which lays the control out there and then. The enum and the position it took
// are one change, so undo has to give back both -- restoring the enum alone
// leaves the control sitting where centring put it.
//-----------------------------------------------------------------------------

function uStepSizing()
{
	GuiEditor.undoRecorder.clear();

	%pane = uBind($uA);
	$uA.setPosition(20, 40);
	%posBefore = $uA.getPosition();
	%horizBefore = $uA.getFieldValue("HorizSizing");

	%pane.onSizingChanged("center", $uA.getFieldValue("VertSizing"));
	%centred = $uA.getPosition();

	uCheck("centring moved the control", %centred !$= %posBefore);
	uCheck("and the enum and the move it caused are one step", uUndoCount() == 1);

	GuiEditor.Undo();
	uCheck("undo restored the sizing", $uA.getFieldValue("HorizSizing") $= %horizBefore);
	uCheck("and the position centring took", $uA.getPosition() $= %posBefore);

	GuiEditor.Redo();
	uCheck("redo centred it again", $uA.getPosition() $= %centred);
	uCheck("with the sizing back on center", $uA.getFieldValue("HorizSizing") $= "center");
	GuiEditor.Undo();

	// Fill takes the extent as well as the position.
	GuiEditor.undoRecorder.clear();
	%pane = uBind($uA);
	%posBefore = $uA.getPosition();
	%extentBefore = $uA.getExtent();

	%pane.onSizingChanged("fill", $uA.getFieldValue("VertSizing"));
	uCheck("filling resized the control", $uA.getExtent() !$= %extentBefore);

	GuiEditor.Undo();
	uCheck("undo restored the extent fill took", $uA.getExtent() $= %extentBefore);
	uCheck("and the position with it", $uA.getPosition() $= %posBefore);

	schedule(300, 0, "uStepText");
}

//-----------------------------------------------------------------------------
// Typing, which reaches the control once per keystroke so the canvas keeps up,
// and reaches the stack once.
//-----------------------------------------------------------------------------

function uStepText()
{
	GuiEditor.undoRecorder.clear();

	%pane = uBind($uC);
	%block = %pane.activeTextBlock();
	%row = %pane.row["text"];
	%before = $uC.text;

	// What a keystroke does: the box's buffer changes and the engine runs its
	// Command. Set the buffer and call the handler that Command names.
	%row.editor.setText("Ca");
	%block.onTextTyped();
	%row.editor.setText("Cat");
	%block.onTextTyped();

	uCheck("the control filled in as we typed", $uC.text $= "Cat");
	uCheck("and nothing was recorded yet", uUndoCount() == 0);

	%row.commit();
	uCheck("three keystrokes are one step", uUndoCount() == 1);
	uCheck("and the control kept the typed text", $uC.text $= "Cat");

	GuiEditor.Undo();
	uCheck("undo restored the whole caption at once", $uC.text $= %before);
	GuiEditor.Redo();
	uCheck("redo typed it again", $uC.text $= "Cat");

	schedule(300, 0, "uStepAdd");
}

//-----------------------------------------------------------------------------
// Adding a control, which is a move op with the trash on one end. Undo does not
// delete: the control keeps living in the trash, which is the whole reason redo
// can put it back wearing everything it had.
//-----------------------------------------------------------------------------

function uStepAdd()
{
	GuiEditor.undoRecorder.clear();

	GuiEditor.brain.setCurrentAddSet($uPanel);

	$uDropped = new GuiCheckBoxCtrl() { Position = "0 0"; Extent = "120 30"; Text = "Dropped"; };
	GuiEditor.brain.onControlDropped($uDropped, "50 50");

	uCheck("a drop is one step, not one per thing it did", uUndoCount() == 1);
	uCheck("the control arrived", $uDropped.getParent() == $uPanel);
	uCheck("and was themed on arrival",
		$uDropped.getFieldValue("Profile") $= "PlanetXCheckBoxProfile");

	// After the drop's own schedule(40) has run, so the undo is not racing it.
	schedule(200, 0, "uStepAddUndo");
}

function uStepAddUndo()
{
	%trash = GuiEditor.brain.getTrash();

	GuiEditor.Undo();

	// getGroup, not getParent: getParent is a GuiControl's view of the hierarchy
	// and dynamic_casts the group it is in, so a control sitting in the trash --
	// a plain SimGroup -- reads as having no parent at all.
	uCheck("undo took the control out of the Gui", $uDropped.getGroup() == %trash);
	uCheck("but did not delete it", isObject($uDropped));

	GuiEditor.Redo();
	uCheck("redo put it back", $uDropped.getParent() == $uPanel);
	uCheck("still wearing its theme profile",
		$uDropped.getFieldValue("Profile") $= "PlanetXCheckBoxProfile");

	GuiEditor.Undo();

	schedule(300, 0, "uStepDelete");
}

//-----------------------------------------------------------------------------
// Deleting, which has to restore the index as well as the parent - a control
// put back at the end of its parent is a control that changed z-order.
//-----------------------------------------------------------------------------

function uStepDelete()
{
	GuiEditor.undoRecorder.clear();
	%trash = GuiEditor.brain.getTrash();

	// One control, from the middle.
	uCheck("B starts at index 1", uIndexOf($uPanel, $uB) == 1);
	uSelect($uB);
	GuiEditor.brain.deleteSelection();

	uCheck("a delete is one step", uUndoCount() == 1);
	uCheck("and the control went to the trash", $uB.getGroup() == %trash);

	GuiEditor.Undo();
	uCheck("undo put it back in its parent", $uB.getParent() == $uPanel);
	uCheck("at the index it came from", uIndexOf($uPanel, $uB) == 1);

	GuiEditor.Redo();
	uCheck("redo trashed it again", $uB.getGroup() == %trash);
	GuiEditor.Undo();

	// Two at once, from either side of a third.
	GuiEditor.undoRecorder.clear();
	%indexA = uIndexOf($uPanel, $uA);
	%indexC = uIndexOf($uPanel, $uC);

	GuiEditor.brain.clearSelection();
	GuiEditor.brain.select($uA);
	GuiEditor.brain.addSelection($uC);
	GuiEditor.brain.deleteSelection();

	uCheck("deleting two controls is still one step", uUndoCount() == 1);
	uCheck("both went to the trash",
		$uA.getGroup() == %trash && $uC.getGroup() == %trash);

	GuiEditor.Undo();
	uCheck("undo restored both", $uA.getParent() == $uPanel && $uC.getParent() == $uPanel);
	uCheck("A at its old index", uIndexOf($uPanel, $uA) == %indexA);
	uCheck("C at its old index", uIndexOf($uPanel, $uC) == %indexC);

	schedule(300, 0, "uStepNudge");
}

//-----------------------------------------------------------------------------
// Nudging. Holding an arrow key down is one move, which is what the C++'s
// separate onPreSelectionNudged callback was always for.
//-----------------------------------------------------------------------------

function uStepNudge()
{
	GuiEditor.undoRecorder.clear();

	%before = $uA.getPosition();
	uSelect($uA);
	GuiEditor.brain.moveSelection(1, 0);
	GuiEditor.brain.moveSelection(1, 0);
	GuiEditor.brain.moveSelection(1, 0);

	// Not "moved by three": snap to grid is on by default (the brain turns it on
	// in onAdd), so a nudge of 1 is a nudge to the next grid line. What the run
	// landed on is what undo has to take back.
	%after = $uA.getPosition();
	uCheck("three nudges are one step", uUndoCount() == 1);
	uCheck("and the control moved", %after !$= %before);

	GuiEditor.Undo();
	uCheck("undo takes back the whole run", $uA.getPosition() $= %before);
	GuiEditor.Redo();
	uCheck("redo replays the whole run", $uA.getPosition() $= %after);

	// Anything in between breaks the run, or a nudge made after a different edit
	// would be folded into the move before it.
	GuiEditor.undoRecorder.clear();
	uSelect($uA);
	GuiEditor.brain.moveSelection(0, 1);

	%pane = uBind($uA);
	%pane.writeField("Visible", false);

	uSelect($uA);
	GuiEditor.brain.moveSelection(0, 1);

	uCheck("an edit between two nudges keeps them apart", uUndoCount() == 3);

	%pane = uBind($uA);
	%pane.writeField("Visible", true);

	// A mouse-down that selects without dragging is not an edit.
	GuiEditor.undoRecorder.clear();
	%selection = GuiEditor.brain.getSelected();
	GuiEditor.undoRecorder.snapshot(%selection);
	GuiEditor.undoRecorder.commitGeometry("", "");
	uCheck("a gesture that moved nothing records nothing", uUndoCount() == 0);

	schedule(300, 0, "uStepLayout");
}

//-----------------------------------------------------------------------------
// The Layout menu, which records in script because the C++ says nothing when it
// aligns or restacks.
//-----------------------------------------------------------------------------

function uStepLayout()
{
	GuiEditor.undoRecorder.clear();

	$uA.setPosition(10, 10);
	$uC.setPosition(60, 90);
	%beforeA = $uA.getPosition();
	%beforeC = $uC.getPosition();

	GuiEditor.brain.clearSelection();
	GuiEditor.brain.select($uA);
	GuiEditor.brain.addSelection($uC);
	GuiEditor.Justify(0);

	uCheck("aligning is one step", uUndoCount() == 1);
	uCheck("and it moved something", $uC.getPosition() !$= %beforeC);

	GuiEditor.Undo();
	uCheck("undo put both back",
		$uA.getPosition() $= %beforeA && $uC.getPosition() $= %beforeC);
	GuiEditor.Redo();
	uCheck("redo aligned them again", $uC.getPosition() !$= %beforeC);

	// Restacking. The C++ works on the current add set, so that has to be the
	// control's parent - and setting it clears the selection, so it goes first.
	GuiEditor.undoRecorder.clear();
	GuiEditor.brain.setCurrentAddSet($uPanel);
	uSelect($uA);

	%before = uIndexOf($uPanel, $uA);
	GuiEditor.BringToFront();

	uCheck("bring to front is one step", uUndoCount() == 1);
	uCheck("and it moved the control up the list", uIndexOf($uPanel, $uA) != %before);

	GuiEditor.Undo();
	uCheck("undo restored the index", uIndexOf($uPanel, $uA) == %before);
	GuiEditor.Redo();
	uCheck("redo restacked it again", uIndexOf($uPanel, $uA) != %before);
	GuiEditor.Undo();

	schedule(300, 0, "uStepTheme");
}

//-----------------------------------------------------------------------------
// Set Theme, which writes a profile into every slot of every control in the
// document and is one Ctrl+Z.
//-----------------------------------------------------------------------------

function uStepTheme()
{
	GuiEditor.undoRecorder.clear();

	%library = GuiEditor.getThemeLibrary();
	$uThemeB = %library.createTheme("UndoThemeB");
	uCheck("second theme created", isObject($uThemeB));

	GuiEditor.setTheme($uThemeB, false);
	uCheck("a whole-document re-theme is one step", uUndoCount() == 1);
	uCheck("and it re-profiled the controls",
		$uA.getFieldValue("Profile") $= "UndoThemeBButtonProfile");

	GuiEditor.Undo();
	uCheck("undo put the old theme's profiles back",
		$uA.getFieldValue("Profile") $= "PlanetXButtonProfile");
	uCheck("all of them", $uPanel.getFieldValue("Profile") $= "PlanetXPanelProfile");

	GuiEditor.Redo();
	uCheck("redo re-applied the theme",
		$uA.getFieldValue("Profile") $= "UndoThemeBButtonProfile");

	GuiEditor.Undo();
	GuiEditor.setTheme($uTheme, false);

	// Deleting the theme detaches the document from it first, which is one of the
	// paths that has to empty the stack: every record holds profile ids that stop
	// resolving.
	%library.deleteTheme($uThemeB);
	uCheck("deleting a theme emptied the stack", uUndoCount() == 0 && uRedoCount() == 0);

	schedule(300, 0, "uStepReparent");
}

//-----------------------------------------------------------------------------
// Dragging in the Explorer tree, which rearranges the hierarchy in C++ and can
// move any number of controls into any number of parents at once. The engine
// now brackets it with onPreReorder / onPostReorder; those are called directly
// here, around the same hierarchy change the drag makes.
//-----------------------------------------------------------------------------

function uStepReparent()
{
	GuiEditor.undoRecorder.clear();
	%tree = GuiEditor.explorerWindow.tree;

	%index = uIndexOf($uPanel, $uA);

	%tree.onPreReorder();
	$uOther.add($uA);
	%tree.onPostReorder();

	uCheck("a reparent is one step", uUndoCount() == 1);
	uCheck("and the control changed parent", $uA.getParent() == $uOther);

	GuiEditor.Undo();
	uCheck("undo returned it to its old parent", $uA.getParent() == $uPanel);
	uCheck("at its old index", uIndexOf($uPanel, $uA) == %index);

	GuiEditor.Redo();
	uCheck("redo reparented it again", $uA.getParent() == $uOther);
	GuiEditor.Undo();

	// A reorder inside one parent, which is the same op restoring a list rather
	// than a parent.
	GuiEditor.undoRecorder.clear();
	%first = $uPanel.getObject(0);
	%last = $uPanel.getObject($uPanel.getCount() - 1);

	%tree.onPreReorder();
	$uPanel.reorderChild(%last, %first);
	%tree.onPostReorder();

	uCheck("a reorder is one step", uUndoCount() == 1);
	uCheck("and the order changed", $uPanel.getObject(0) == %last);

	GuiEditor.Undo();
	uCheck("undo restored the order", $uPanel.getObject(0) == %first);

	schedule(300, 0, "uStepMenu");
}

//-----------------------------------------------------------------------------
// The Edit menu, which is what tells the user whether there is anything to take
// back. It was inactive entirely until now.
//-----------------------------------------------------------------------------

// Menu items are nested controls, so this walks rather than indexes.
function uMenuItem(%parent, %text)
{
	for(%i = 0; %i < %parent.getCount(); %i++)
	{
		%item = %parent.getObject(%i);
		if(%item.Text $= %text)
		{
			return %item;
		}

		%found = uMenuItem(%item, %text);
		if(isObject(%found))
		{
			return %found;
		}
	}

	return 0;
}

function uStepMenu()
{
	%editMenu = uMenuItem(EditorCore.menuBar, "Edit");
	%undoItem = uMenuItem(EditorCore.menuBar, "Undo");
	%redoItem = uMenuItem(EditorCore.menuBar, "Redo");
	%cutItem = uMenuItem(EditorCore.menuBar, "Cut");

	uCheck("the Edit menu exists", isObject(%editMenu));
	uCheck("and is offered at last", %editMenu.Active);
	uCheck("Cut is still a stub and is not offered", !%cutItem.Active);

	GuiEditor.undoRecorder.clear();
	uCheck("Undo is greyed with an empty stack", !%undoItem.Active);
	uCheck("Redo is greyed with an empty stack", !%redoItem.Active);

	%pane = uBind($uC);
	%pane.writeField("Visible", false);
	uCheck("an edit lights Undo", %undoItem.Active);
	uCheck("and leaves Redo greyed", !%redoItem.Active);

	GuiEditor.Undo();
	uCheck("undoing the last step greys Undo", !%undoItem.Active);
	uCheck("and lights Redo", %redoItem.Active);

	GuiEditor.Redo();
	uCheck("redoing lights Undo again", %undoItem.Active);
	uCheck("and greys Redo", !%redoItem.Active);

	GuiEditor.Undo();

	schedule(300, 0, "uStepStale");
}

//-----------------------------------------------------------------------------
// The two ways the stack goes stale.
//-----------------------------------------------------------------------------

function uStepStale()
{
	GuiEditor.undoRecorder.clear();

	// A record naming a control that has since been deleted for real. Nothing
	// clears the stack for this - a control the editor did not delete can still
	// go away - so the replay has to survive it.
	%doomed = new GuiButtonCtrl() { Position = "10 10"; Extent = "60 20"; Text = "X"; };
	$uOther.add(%doomed);

	%pane = uBind(%doomed);
	%pane.writeField("Extent", "90 40");
	uCheck("the doomed control's edit was recorded", uUndoCount() == 1);

	%doomed.delete();
	GuiEditor.Undo();
	uCheck("undoing an edit on a deleted control is a no-op, not a crash",
		uUndoCount() == 0 && uRedoCount() == 1);

	// A new document. Every id on the stack has just been freed.
	GuiEditor.NewGui();
	uCheck("New Gui empties both stacks", uUndoCount() == 0 && uRedoCount() == 0);

	schedule(300, 0, "uStepChain");
}

//-----------------------------------------------------------------------------
// Deleting out of a layout container.
//
// A GuiChainCtrl lays its children out in list order and zeroes the position of
// any child added to it while the editor is open (guiChainCtrl.cc,
// onChildAdded) -- both right for a control being dropped in, both wrong for
// one being put back, which knows where it was and which slot it held.
//
// Last, and on the real editor UI, because that second half only happens when
// isEditMode() is true: that needs the brain awake, which needs the editor
// pushed onto the canvas rather than merely registered. Everything above runs
// without it, so this is where the canvas gets taken over.
//-----------------------------------------------------------------------------

function uStepChain()
{
	EditorCore.open();
	EditorCore.tabBook.selectPageName("Gui Editor");

	schedule(500, 0, "uStepChainRun");
}

function uStepChainRun()
{
	GuiEditor.undoRecorder.clear();

	$uChain = new GuiChainCtrl()
	{
		Position = "10 10";
		Extent = "200 40";
		IsVertical = true;
		ChildSpacing = 4;
	};
	GuiEditor.rootGui.add($uChain);

	// Proof by behaviour that the canvas really is in edit mode, since that is
	// what this whole step turns on: zeroing an added child's position is
	// something a chain only does then.
	%probe = new GuiButtonCtrl() { Extent = "80 24"; };
	%probe.setPosition(33, 33);
	$uChain.add(%probe);
	uCheck("the editor is really in edit mode now (a chain zeroes what it is given)",
		getWord(%probe.getPosition(), 0) == 0);
	%probe.delete();

	// The x is set after the add, because the add is what zeroes it.
	for(%i = 0; %i < 4; %i++)
	{
		%button = new GuiButtonCtrl() { Extent = "80 24"; Text = "chain" @ %i; };
		$uChain.add(%button);
		%button.setPosition(%i * 5, 0);
		$uButton[%i] = %button;
	}

	uCheck("the chain holds four buttons", $uChain.getCount() == 4);
	%xBefore = getWord($uButton[1].getPosition(), 0);
	uCheck("and the second one has an x of its own", %xBefore == 5);

	uSelect($uButton[1]);
	GuiEditor.brain.deleteSelection();
	uCheck("deleting left three", $uChain.getCount() == 3);

	GuiEditor.Undo();

	uCheck("undo put it back in the chain", $uButton[1].getParent() == $uChain);
	uCheck("at the index it came from", uIndexOf($uChain, $uButton[1]) == 1);
	uCheck("keeping the x the chain does not own (" @
		getWord($uButton[1].getPosition(), 0) @ " was " @ %xBefore @ ")",
		getWord($uButton[1].getPosition(), 0) == %xBefore);

	// The chain owns y, so the proof it laid out again is that the four are back
	// in flow order rather than the restored one sitting where the list briefly
	// had it -- last.
	%y0 = getWord($uButton[0].getPosition(), 1);
	%y1 = getWord($uButton[1].getPosition(), 1);
	%y2 = getWord($uButton[2].getPosition(), 1);
	%y3 = getWord($uButton[3].getPosition(), 1);
	uCheck("and the chain laid them out in order again (" @
		%y0 SPC %y1 SPC %y2 SPC %y3 @ ")", %y0 < %y1 && %y1 < %y2 && %y2 < %y3);

	schedule(300, 0, "uStepContainers");
}

//-----------------------------------------------------------------------------
// Every other container that places its own children.
//
// A chain is not a special case: a grid, a tab book and a frame set all take
// something from a child when it arrives and all lay their children out by list
// order, so all of them can hand back the wrong thing after an undo. The test
// each one gets is the same, and it is the strongest one available: the whole
// container must look exactly as it did before the delete.
//-----------------------------------------------------------------------------

// Every child's identity, position and extent, in list order. TAB-delimited
// because a position has a space in it.
function uLayoutOf(%parent)
{
	%text = "";
	for(%i = 0; %i < %parent.getCount(); %i++)
	{
		%child = %parent.getObject(%i);
		%entry = %child @ "[" @ %child.getPosition() @ "][" @ %child.getExtent() @ "]";
		%text = (%text $= "") ? %entry : (%text TAB %entry);
	}

	return %text;
}

// Delete the second child of %parent and undo it. Everything has to come back:
// the list order, and every child's geometry, which is the container's to give.
function uRoundTrip(%label, %parent)
{
	GuiEditor.undoRecorder.clear();

	%before = uLayoutOf(%parent);
	%child = %parent.getObject(1);

	uSelect(%child);
	GuiEditor.brain.deleteSelection();
	uCheck(%label @ ": the delete took", %parent.getCount() == 3);

	GuiEditor.Undo();

	uCheck(%label @ ": undo put it back at index 1", uIndexOf(%parent, %child) == 1);

	%after = uLayoutOf(%parent);
	uCheck(%label @ ": and the container looks as it did" NL
		"           before: " @ %before NL
		"           after:  " @ %after, %after $= %before);
}

function uStepContainers()
{
	// A grid, which places children into cells by list order and forces a
	// child's sizing off center and fill when it arrives.
	$uGrid = new GuiGridCtrl()
	{
		Position = "10 200";
		Extent = "300 120";
		CellSizeX = 60;
		CellSizeY = 30;
		MaxColCount = 2;
	};
	GuiEditor.rootGui.add($uGrid);

	for(%i = 0; %i < 4; %i++)
	{
		%cell = new GuiButtonCtrl() { Extent = "50 20"; Text = "cell" @ %i; };
		$uGrid.add(%cell);
	}
	uRoundTrip("grid", $uGrid);

	// A container takes a child's sizing as well as its geometry: a grid forces
	// center and fill off on arrival. A child put back on center afterwards
	// would lose it again on the way in.
	%cell = $uGrid.getObject(1);
	%cell.setEditFieldValue("HorizSizing", "center");
	uCheck("grid: the cell is on center to start",
		%cell.getFieldValue("HorizSizing") $= "center");

	GuiEditor.undoRecorder.clear();
	uSelect(%cell);
	GuiEditor.brain.deleteSelection();
	GuiEditor.Undo();
	uCheck("grid: undo restored the sizing the grid takes on arrival (" @
		%cell.getFieldValue("HorizSizing") @ ")",
		%cell.getFieldValue("HorizSizing") $= "center");

	// A tab book, whose tab strip is drawn from a page list of its own rather
	// than from the children.
	$uBook = new GuiTabBookCtrl()
	{
		Position = "330 200";
		Extent = "300 120";
	};
	GuiEditor.rootGui.add($uBook);

	for(%i = 0; %i < 4; %i++)
	{
		%page = new GuiTabPageCtrl() { Text = "page" @ %i; };
		$uBook.add(%page);
		$uPage[%i] = %page;
	}
	uRoundTrip("tab book", $uBook);

	// The pages all share the book's page rect, so their geometry cannot say
	// whether the tab strip came back in the right order. Selecting by index
	// can: it selects the page list's second entry, which has to be the child
	// that is second.
	$uBook.selectPage(1);
	uCheck("tab book: the second tab is the second page",
		$uPage[1].isVisible());

	schedule(300, 0, "uStepFrameSet");
}

function uStepFrameSet()
{
	// A frame set, which places each child in a frame of a tree it keeps beside
	// the child list.
	$uFrames = new GuiFrameSetCtrl()
	{
		Position = "10 340";
		Extent = "400 200";
		DividerThickness = 4;
	};
	GuiEditor.rootGui.add($uFrames);

	%ids = $uFrames.createHorizontalSplit(1);
	%left = getWord(%ids, 0);
	%right = getWord(%ids, 1);
	%ids = $uFrames.createVerticalSplit(%left);
	%ids = $uFrames.createVerticalSplit(%right);

	for(%i = 0; %i < 4; %i++)
	{
		%panel = new GuiControl() { Extent = "40 20"; };
		$uFrames.add(%panel);
	}

	// Settle the layout before anything is measured. A frame set places its
	// children when it resizes, and until then they are still sitting at the
	// bounds they were built with -- which would make the before-and-after
	// comparison meaningless.
	$uFrames.childrenReordered();

	// The frame tree itself has to come back, not just the child list: removing
	// a control destroys the frame it stood in and merges the split into its
	// twin, so without that the sibling keeps the space it swallowed.
	$uFrameLayout = $uFrames.getFrameLayout();
	uCheck("frame set: its layout can be read", $uFrameLayout !$= "");

	uRoundTrip("frame set", $uFrames);

	uCheck("frame set: and the frame tree came back with it",
		$uFrames.getFrameLayout() $= $uFrameLayout);

	// Redo has to take it apart again, or a second undo would be restoring
	// against a tree that no longer matches.
	GuiEditor.Redo();
	uCheck("frame set: redo collapsed the frame again",
		$uFrames.getFrameLayout() !$= $uFrameLayout);
	GuiEditor.Undo();
	uCheck("frame set: and undoing again rebuilt it",
		$uFrames.getFrameLayout() $= $uFrameLayout);

	schedule(300, 0, "uDone");
}

function uDone()
{
	echo("UNDO DONE");
	quit();
}
