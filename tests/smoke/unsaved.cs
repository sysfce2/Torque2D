//-----------------------------------------------------------------------------
// Unsaved-changes protection: the modified flag, the name on screen, the prompt
// that stands between a modified Gui and the four commands that would discard
// it, and Revert.
//
// The flag is derived from the undo recorder, which is already the one funnel
// every change goes through. What makes that worth testing rather than assuming
// is that depth is not identity: save, undo, then edit differently and the stack
// is exactly as deep as it was with a different document underneath it. A flag
// built on getUndoCount reads clean there, which is the one direction that must
// never happen, so that case has a check of its own below.
//
// Run: tests/run.ps1 unsaved ; grep SAVE in tests/logs/.
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

function usCheck(%label, %condition)
{
	echo(%condition ? ("SAVE PASS: " @ %label) : ("SAVE FAIL: " @ %label));
}

function usModified()
{
	return GuiEditor.undoRecorder.isModified();
}

// A dialog is pushed onto the Canvas and nothing keeps a handle to it, so it is
// found the way it is displayed: as the Canvas's newest child.
function usDialog(%class)
{
	for(%i = Canvas.getCount() - 1; %i >= 0; %i--)
	{
		%obj = Canvas.getObject(%i);
		if(%obj.class $= %class)
		{
			return %obj;
		}
	}

	return 0;
}

// One edit, through the properties pane, which is the route every field change
// the user makes takes.
function usEdit(%ctrl, %field, %value)
{
	GuiEditor.brain.clearSelection();
	GuiEditor.brain.select(%ctrl);

	%pane = GuiEditor.inspectorWindow.pane;
	%pane.bind(%ctrl);
	%pane.writeField(%field, %value);
}

schedule(2000, 0, "usSetup");

// A throwaway project rather than PlanetX, because this suite writes a Gui file
// and PlanetX is real content. run.ps1 clears any folder a test names ending in
// SmokeProject before each run, so the save below has somewhere of its own to
// land. Nothing here needs a theme.
function usSetup()
{
	ProjectManager.setProjectFolder("unsavedSmokeProject");
	createPath(testRoot("unsavedSmokeProject/"));

	GuiEditor.open();

	$usCtrl = new GuiButtonCtrl() { Position = "10 10"; Extent = "80 30"; Text = "A"; };
	GuiEditor.rootGui.add($usCtrl);

	// The add itself is an edit, so start the measurements from a clean slate.
	GuiEditor.undoRecorder.markClean();

	schedule(300, 0, "usStepFlag");
}

//-----------------------------------------------------------------------------
// The flag.
//-----------------------------------------------------------------------------

function usStepFlag()
{
	usCheck("a document nobody has touched is not modified", !usModified());

	usEdit($usCtrl, "Text", "B");
	usCheck("an edit marks it modified", usModified());

	GuiEditor.undoRecorder.markClean();
	usCheck("saving clears it", !usModified());

	// Back to where the save was taken. The document is byte for byte what was
	// written, so it is not modified, and a flag that only ever latches would say
	// otherwise.
	usEdit($usCtrl, "Text", "C");
	GuiEditor.Undo();
	usCheck("undoing back to the save point clears it", !usModified());

	GuiEditor.Redo();
	usCheck("and redoing away from it marks it again", usModified());

	schedule(300, 0, "usStepBranch");
}

// The case a depth counter gets wrong: undo past the save point, then make a
// different edit. The stack is the same height it was at the save, and the
// document is not the document that was saved.
function usStepBranch()
{
	GuiEditor.undoRecorder.clear();
	GuiEditor.undoRecorder.markClean();

	usEdit($usCtrl, "Text", "one");
	%depth = GuiEditor.undoRecorder.undoCount();
	GuiEditor.undoRecorder.markClean();

	GuiEditor.Undo();
	usCheck("undo past the save point marks it", usModified());

	usEdit($usCtrl, "Text", "two");
	usCheck("the stack is back to the depth it was saved at",
		GuiEditor.undoRecorder.undoCount() == %depth);
	usCheck("but a different edit is still modified", usModified());

	schedule(300, 0, "usStepWipe");
}

// Clearing the stack does not clean the document. detachTheme does exactly this
// -- a profile the document is wearing is about to be freed, so every record
// naming it has to go -- and the controls are just as edited afterwards.
function usStepWipe()
{
	GuiEditor.undoRecorder.clear();
	GuiEditor.undoRecorder.markClean();

	usEdit($usCtrl, "Text", "edited");
	usCheck("modified before the wipe", usModified());

	GuiEditor.undoRecorder.clear();
	usCheck("and still modified after it", usModified());

	schedule(300, 0, "usStepTitle");
}

//-----------------------------------------------------------------------------
// The name on screen. The Gui Tools window's own title carries it: the window is
// the top-left panel and already holds the theme buttons, so it becomes the
// strip that says what is being worked on.
//-----------------------------------------------------------------------------

function usTitle()
{
	return GuiEditor.guiToolsWindow.getText();
}

function usStepTitle()
{
	GuiEditor.undoRecorder.clear();
	GuiEditor.undoRecorder.markClean();

	usCheck("a Gui with no file of its own is untitled", usTitle() $= "untitled.gui");

	usEdit($usCtrl, "Text", "titled");
	usCheck("and wears a marker once edited", usTitle() $= "untitled.gui *");

	// A real save rather than writing the field, because the point is that
	// SaveCore both clears the flag and puts the new name up.
	$usFile = testRoot("unsavedSmokeProject/unsavedTitle.gui");
	GuiEditor.SaveCore($usFile, 0, "unsavedSmokeProject", "");

	usCheck("saving puts the file name up", usTitle() $= "unsavedTitle.gui");
	usCheck("and takes the marker off", !usModified());

	usEdit($usCtrl, "Text", "changed since");
	usCheck("editing a saved Gui marks it again", usTitle() $= "unsavedTitle.gui *");

	usCheck("and the file really was written", isFile($usFile));

	schedule(300, 0, "usStepNoAsk");
}

//-----------------------------------------------------------------------------
// The guard. Four commands throw the document away; each one asks first, and
// only when there is something to lose.
//-----------------------------------------------------------------------------

function usGuardDialog()
{
	return usDialog("GuiEditorConfirmSaveDialog");
}

// A control on the canvas with an edit against it, so there is work to lose.
function usDirtyDocument()
{
	$usCtrl = new GuiButtonCtrl() { Position = "10 10"; Extent = "80 30"; Text = "A"; };
	GuiEditor.rootGui.add($usCtrl);
	usEdit($usCtrl, "Text", "worth keeping");
}

function usStepNoAsk()
{
	GuiEditor.undoRecorder.markClean();
	GuiEditor.NewGui();

	usCheck("New on a clean document does not ask", !isObject(usGuardDialog()));
	usCheck("it empties the document", GuiEditor.rootGui.getCount() == 0);
	usCheck("the new document is clean", !usModified());
	usCheck("and untitled again", usTitle() $= "untitled.gui");

	schedule(300, 0, "usStepCancel");
}

function usStepCancel()
{
	usDirtyDocument();
	%count = GuiEditor.rootGui.getCount();

	GuiEditor.NewGui();
	%dialog = usGuardDialog();
	usCheck("New on a modified document asks", isObject(%dialog));

	%dialog.onCancel();
	usCheck("Cancel leaves the document alone", GuiEditor.rootGui.getCount() == %count);
	usCheck("and leaves it modified", usModified());

	schedule(300, 0, "usStepDiscard");
}

function usStepDiscard()
{
	GuiEditor.NewGui();
	%dialog = usGuardDialog();
	usCheck("still asking", isObject(%dialog));

	%dialog.onDiscard();
	usCheck("Discard goes through with the New", GuiEditor.rootGui.getCount() == 0);
	usCheck("and what it left behind is clean", !usModified());

	schedule(300, 0, "usStepSave");
}

// Save from the prompt, on a Gui that already has a file: it writes and carries
// on without another question.
function usStepSave()
{
	usDirtyDocument();
	GuiEditor.SaveCore($usFile, 0, "unsavedSmokeProject", "");
	usEdit($usCtrl, "Text", "changed after the save");

	GuiEditor.NewGui();
	%dialog = usGuardDialog();
	usCheck("a saved-but-changed Gui asks too", isObject(%dialog));

	%dialog.onSave();
	usCheck("Save did not need the Save As dialog",
		!isObject(usDialog("GuiEditorSaveGuiDialog")));
	usCheck("and then went through with the New", GuiEditor.rootGui.getCount() == 0);
	usCheck("leaving a clean document", !usModified());

	schedule(300, 0, "usStepSaveAsCancelled");
}

// The trap. On a Gui with no file, Save opens Save As -- which has its own
// Cancel, and taking it must abandon the New rather than quietly going ahead
// with it having saved nothing.
function usStepSaveAsCancelled()
{
	usDirtyDocument();
	%count = GuiEditor.rootGui.getCount();

	GuiEditor.NewGui();
	usGuardDialog().onSave();

	%saveDialog = usDialog("GuiEditorSaveGuiDialog");
	usCheck("Save on a never-saved Gui opens Save As", isObject(%saveDialog));

	%saveDialog.onClose();
	usCheck("cancelling Save As leaves the document in place",
		GuiEditor.rootGui.getCount() == %count);
	usCheck("and leaves it modified", usModified());

	schedule(300, 0, "usStepMenuCommands");
}

// The two exits that cannot be run from a suite that has to survive to report.
// Menu items are nested controls, so this walks rather than indexes.
function usMenuItem(%parent, %text)
{
	for(%i = 0; %i < %parent.getCount(); %i++)
	{
		%item = %parent.getObject(%i);
		if(%item.Text $= %text)
		{
			return %item;
		}

		%found = usMenuItem(%item, %text);
		if(isObject(%found))
		{
			return %found;
		}
	}

	return 0;
}

function usStepMenuCommands()
{
	%exit = usMenuItem(EditorCore.menuBar, "Exit");
	%closeProject = usMenuItem(EditorCore.menuBar, "Close Project");

	usCheck("the Exit item exists", isObject(%exit));
	usCheck("the Close Project item exists", isObject(%closeProject));

	// Both are EditorCore.guardedCommand with the real command inside, and
	// running either one for real would take the process with it. So the route
	// itself is exercised with a harmless command instead, and the two items are
	// checked for naming that route.
	usCheck("Exit goes through the guard",
		strstr(%exit.Command, "guardedCommand") >= 0);
	usCheck("Close Project goes through the guard",
		strstr(%closeProject.Command, "guardedCommand") >= 0);

	schedule(300, 0, "usStepGuardedCommand");
}

// The route those two items take, with something safe at the end of it.
function usStepGuardedCommand()
{
	$usRan = false;

	// Nothing to lose: straight through.
	GuiEditor.undoRecorder.markClean();
	EditorCore.guardedCommand("$usRan = true;");
	usCheck("a guarded command on a clean document just runs", $usRan);

	// Something to lose: held until answered for.
	$usRan = false;
	usDirtyDocument();
	EditorCore.guardedCommand("$usRan = true;");

	usCheck("on a modified one it asks first", isObject(usGuardDialog()));
	usCheck("and holds the command back", !$usRan);

	usGuardDialog().onCancel();
	usCheck("Cancel drops it for good", !$usRan);

	EditorCore.guardedCommand("$usRan = true;");
	usGuardDialog().onDiscard();
	usCheck("Discard lets it through", $usRan);

	schedule(300, 0, "usStepRevertGreyed");
}

//-----------------------------------------------------------------------------
// Revert, which is the only command here that puts something back rather than
// taking it away - and which still asks first, because putting the file back is
// discarding everything done since.
//-----------------------------------------------------------------------------

function usStepRevertGreyed()
{
	// A document with no file of its own. There is nothing to revert TO.
	GuiEditor.NewGui();
	usGuardDialog().onDiscard();

	$usRevert = usMenuItem(EditorCore.menuBar, "Revert");
	usCheck("the Revert item exists", isObject($usRevert));
	usCheck("and is not offered before the first save", !$usRevert.Active);

	schedule(300, 0, "usStepRevert");
}

function usStepRevert()
{
	$usCtrl = new GuiButtonCtrl() { Position = "10 10"; Extent = "80 30"; Text = "A"; };
	GuiEditor.rootGui.add($usCtrl);
	usEdit($usCtrl, "Text", "on disk");

	GuiEditor.SaveCore($usFile, 0, "unsavedSmokeProject", "");
	usCheck("Revert is offered once the Gui has a file", $usRevert.Active);

	usEdit($usCtrl, "Text", "not on disk");
	usCheck("and the change is showing", $usCtrl.getText() $= "not on disk");

	GuiEditor.Revert();
	usCheck("Revert asks before discarding", isObject(usGuardDialog()));
	usGuardDialog().onDiscard();

	schedule(300, 0, "usStepRevertCheck");
}

function usStepRevertCheck()
{
	usCheck("the document was re-read", GuiEditor.rootGui.getCount() == 1);
	usCheck("with what was on disk in it",
		GuiEditor.rootGui.getObject(0).getText() $= "on disk");
	usCheck("and it is clean again", !usModified());
	usCheck("still under its own name", usTitle() $= "unsavedTitle.gui");

	schedule(300, 0, "usDone");
}

function usDone()
{
	echo("SAVE DONE");
	quit();
}
