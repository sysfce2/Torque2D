//-----------------------------------------------------------------------------
// The Gui Editor's four menus: File, Edit, Layout and Select.
//
// They used to be written into the shared bar in EditorCore, greyed in when this
// editor opened and greyed out when it closed. They live here now because they
// were never shared - every command in them names GuiEditor - and because the
// Asset Manager wanted a File and an Edit of its own that mean something else.
// EditorCore swaps whole sets in and out; see EditorMenuSet.
//
// The greying divides in two. Revert, Undo, Redo and Paste each answer their own
// question and are held by name. Everything in Layout and Select answers the
// same one - how much is selected - so those thirteen, five, two and two items
// are groups, and refreshSelection flips them with four calls instead of
// twenty-two.
//-----------------------------------------------------------------------------

function GuiEditorMenus::onAdd(%this)
{
	%this.init();
}

function GuiEditorMenus::build(%this)
{
	%file = %this.addMenu("File");
	%file.addItem("New Gui", "GuiEditor.NewGui();", "Ctrl N");
	%file.addItem("Open Gui...", "GuiEditor.OpenGui();", "Ctrl O");
	%file.addSeparator();
	%file.addItem("Save Gui...", "GuiEditor.SaveGui();", "Ctrl S");
	%file.addItem("Save Gui As...", "GuiEditor.SaveGuiAs();", "Ctrl-Shift S");
	%file.addSeparator();

	// Offered only once the Gui has a file to go back to; refreshFile keeps that
	// up to date. No accelerator - it throws away everything since the last save,
	// and that is not a thing to have a shortcut for.
	%this.revert = %file.addItem("Revert", "GuiEditor.Revert();");

	%edit = %this.addMenu("Edit");
	%this.undo = %edit.addItem("Undo", "GuiEditor.Undo();", "Ctrl Z");
	%this.redo = %edit.addItem("Redo", "GuiEditor.Redo();", "Ctrl-Shift Z");
	%edit.addSeparator();
	%edit.addItem("Cut", "GuiEditor.Cut();", "Ctrl X", "selection");
	%edit.addItem("Copy", "GuiEditor.Copy();", "Ctrl C", "selection");
	%this.paste = %edit.addItem("Paste", "GuiEditor.Paste();", "Ctrl V");
	%edit.addItem("Duplicate", "GuiEditor.Duplicate();", "Ctrl D", "selection");
	%edit.addSeparator();

	// DeleteSelection, not Delete: delete is a console method on every SimObject,
	// so GuiEditor.Delete() would quietly destroy the editor rather than the
	// selection.
	//
	// The accelerator cannot double-fire with the Delete key the canvas and the
	// Explorer tree already handle themselves. The canvas consults accelerators
	// only once the first responder has passed on the key -- the same thing that
	// lets a text box in the properties pane keep Ctrl+C. What it adds is Delete
	// working while focus is in a tool window.
	%edit.addItem("Delete", "GuiEditor.DeleteSelection();", "Delete", "selection");

	%layout = %this.addMenu("Layout");
	%layout.addItem("Nudge Up", "GuiEditor.brain.moveSelection(0,-1);", "Up", "selection");
	%layout.addItem("Nudge Down", "GuiEditor.brain.moveSelection(0,1);", "Down", "selection");
	%layout.addItem("Nudge Left", "GuiEditor.brain.moveSelection(-1,0);", "Left", "selection");
	%layout.addItem("Nudge Right", "GuiEditor.brain.moveSelection(1,0);", "Right", "selection");
	%layout.addSeparator();
	%layout.addItem("Shrink Height", "GuiEditor.changeExtent(0,-1);", "Ctrl Up", "selection");
	%layout.addItem("Expand Height", "GuiEditor.changeExtent(0, 1);", "Ctrl Down", "selection");
	%layout.addItem("Shrink Width", "GuiEditor.changeExtent(-1,0);", "Ctrl Left", "selection");
	%layout.addItem("Expand Width", "GuiEditor.changeExtent(1,0);", "Ctrl Right", "selection");
	%layout.addSeparator();
	%layout.addItem("Align Top", "GuiEditor.Justify(3);", "Ctrl T", "align");
	%layout.addItem("Align Bottom", "GuiEditor.Justify(4);", "Ctrl B", "align");
	%layout.addItem("Align Left", "GuiEditor.Justify(0);", "Ctrl L", "align");
	%layout.addItem("Align Right", "GuiEditor.Justify(2);", "Ctrl R", "align");
	%layout.addSeparator();
	%layout.addItem("Center Horizontally", "GuiEditor.Justify(1);", "", "align");
	%layout.addItem("Space Vertically", "GuiEditor.Justify(5);", "", "space");
	%layout.addItem("Space Horizontally", "GuiEditor.Justify(6);", "", "space");
	%layout.addSeparator();
	%layout.addItem("Bring to Front", "GuiEditor.BringToFront();", "Ctrl-Shift Up", "restack");
	%layout.addItem("Push to Back", "GuiEditor.PushToBack();", "Ctrl-Shift Down", "restack");
	%layout.addSeparator();
	%layout.addItem("Set Grid Size...", "GuiEditor.SetGridSize();", "Ctrl-Shift G");
	%layout.addToggle("Snap to Grid", "GuiEditor.SnapToGrid(true);", "GuiEditor.SnapToGrid(false);", "Ctrl G", true);

	%select = %this.addMenu("Select");
	%select.addItem("Select All", "GuiEditor.brain.SelectAll();", "Ctrl A");

	// Ctrl-Shift A rather than Ctrl D, which Duplicate has: this is what deselect
	// is bound to nearly everywhere else, and it pairs with Ctrl A above.
	%select.addItem("Deselect", "GuiEditor.brain.clearSelection();", "Ctrl-Shift A", "selection");
}

//-----------------------------------------------------------------------------
// Greying. Each of these is called by whoever owns the answer; refresh is what
// EditorCore calls when the set goes back on the bar, so the menus look new
// every time the editor is opened rather than carrying the state they had when
// it was last closed.
//-----------------------------------------------------------------------------

function GuiEditorMenus::refresh(%this)
{
	%this.refreshFile();
	%this.tool.undoRecorder.refreshMenu();
	%this.tool.clipboard.refreshMenu();
	%this.tool.brain.toggleMenuItems();
}

// Revert is the only File item whose offer changes, and what it turns on is
// whether the document has a file to go back to.
function GuiEditorMenus::refreshFile(%this)
{
	%this.revert.setActive(%this.tool.filePath !$= "");
}

function GuiEditorMenus::refreshUndo(%this, %undoCount, %redoCount)
{
	%this.undo.setActive(%undoCount > 0);
	%this.redo.setActive(%redoCount > 0);
}

function GuiEditorMenus::refreshPaste(%this, %hasCopy)
{
	%this.paste.setActive(%hasCopy);
}

// Everything in Layout and Select, plus the half of Edit that acts on controls.
// The thresholds are here rather than at the call site because the groups are.
function GuiEditorMenus::refreshSelection(%this, %count)
{
	%this.setGroupActive("selection", %count != 0);
	%this.setGroupActive("align", %count > 1);
	%this.setGroupActive("space", %count > 2);
	%this.setGroupActive("restack", %count == 1);
}
