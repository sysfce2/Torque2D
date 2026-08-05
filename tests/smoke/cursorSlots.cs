// Cursor-slot smoke test: the Gui Editor half of cursor support.
//
// A control's cursor fields follow the same rule as its secondary profile
// slots. Set Theme fills them without being asked, so a window is on its own
// theme's cursors from the moment it is dropped -- but a row only appears once
// the theme holds a second cursor for that job and there is a choice to make.
// Detaching a theme has to move them somewhere real as well: a GuiCursor* is as
// raw a pointer as a profile's.
// Run: tests/run.ps1 cursorSlots  ; grep CSSMOKE in console.log.

setLogMode(1);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

function sCheck(%label, %cond)
{
	if(%cond) echo("CSSMOKE PASS: " @ %label);
	else      echo("CSSMOKE FAIL: " @ %label);
}

function sPane()
{
	return GuiEditor.inspectorWindow.pane;
}

function sRebind(%ctrl)
{
	%pane = sPane();
	%pane.bind(%ctrl);
	return %pane;
}

function sHasRow(%pane, %field)
{
	return isObject(%pane.row[%field]);
}

testExec("editor/main.cs");
schedule(2000, 0, "sStep1");

//-----------------------------------------------------------------------------
// Set Theme fills the cursor slots silently, and shows nothing for them.
//-----------------------------------------------------------------------------

function sStep1()
{
	ProjectManager.setProjectFolder("cursorSlotsSmokeProject");
	GuiEditor.open();

	// A window has three cursor slots; a frame set and a text edit have the
	// others between them.
	$sWindow = new GuiWindowCtrl();
	GuiEditor.rootGui.add($sWindow);
	$sEdit = new GuiTextEditCtrl();
	GuiEditor.rootGui.add($sEdit);

	$sTheme = GuiEditor.themeLibrary.createTheme("CSSmoke");
	sCheck("theme created", isObject($sTheme));

	GuiEditor.themeApplier.applyToBranch($sWindow, $sTheme, true);
	GuiEditor.themeApplier.applyToBranch($sEdit, $sTheme, true);
	GuiEditor.themeName = $sTheme.getName();

	// Filled without being asked: the point is that a Gui wears ITS theme's
	// cursors rather than whichever set the project installed globally.
	sCheck("window took the theme's corner cursor",
		$sWindow.nWSECursor $= $sTheme.getCursor("NWSE").getName());
	sCheck("window took the theme's horizontal cursor",
		$sWindow.leftRightCursor $= $sTheme.getCursor("LeftRight").getName());
	sCheck("text edit took the theme's text cursor",
		$sEdit.editCursor $= $sTheme.getCursor("Edit").getName());

	// ...and says nothing about it, because there is nothing to choose.
	%pane = sRebind($sWindow);
	sCheck("no Variants section with one cursor per category",
		!isObject(%pane.panel["Variants"]));
	sCheck("no corner cursor row", !sHasRow(%pane, "nWSECursor"));
	sCheck("no horizontal cursor row", !sHasRow(%pane, "leftRightCursor"));

	schedule(200, 0, "sStep2");
}

//-----------------------------------------------------------------------------
// A second cursor in one category: that slot, and only that slot, appears.
//-----------------------------------------------------------------------------

function sStep2()
{
	$sExtra = GuiEditor.themeLibrary.createExtraCursor($sTheme, "NWSE");
	sCheck("extra NWSE cursor created", isObject($sExtra));
	sCheck("theme reports two NWSE cursors",
		getWordCount($sTheme.getCursors("NWSE")) == 2);

	%pane = sRebind($sWindow);
	sCheck("Variants section appeared", isObject(%pane.panel["Variants"]));
	sCheck("corner cursor got a row", sHasRow(%pane, "nWSECursor"));

	// Only the category with a choice in it.
	sCheck("horizontal cursor row still hidden", !sHasRow(%pane, "leftRightCursor"));
	sCheck("vertical cursor row still hidden", !sHasRow(%pane, "upDownCursor"));

	%row = %pane.row["nWSECursor"];
	sCheck("row offers the default member",
		%row.editor.findItemText($sTheme.getCursor("NWSE").getName(), false) >= 0);
	sCheck("row offers the extra member",
		%row.editor.findItemText($sExtra.getName(), false) >= 0);
	sCheck("row shows what the control wears",
		%row.getValue() $= $sTheme.getCursor("NWSE").getName());

	// A text edit has no NWSE slot at all, so its pane is unaffected.
	%editPane = sRebind($sEdit);
	sCheck("text edit shows no cursor row", !sHasRow(%editPane, "editCursor"));

	schedule(200, 0, "sStep3");
}

//-----------------------------------------------------------------------------
// Choosing the extra writes it, and detaching the theme leaves nothing dangling.
//-----------------------------------------------------------------------------

function sStep3()
{
	%pane = sRebind($sWindow);
	%row = %pane.row["nWSECursor"];

	%row.applyValue($sExtra.getName());
	%pane.onProfileRowCommit(%row);
	sCheck("choosing the extra wrote it to the control",
		$sWindow.nWSECursor $= $sExtra.getName());

	// Re-applying the theme leaves a deliberate second choice alone: it already
	// belongs to this theme, which is the whole test for "someone chose this".
	GuiEditor.themeApplier.applyToBranch($sWindow, $sTheme, true);
	sCheck("re-applying the theme keeps the chosen cursor",
		$sWindow.nWSECursor $= $sExtra.getName());

	// Detach moves every slot off the doomed theme, onto the canonical name for
	// that slot -- not an empty string, which through TypeGuiCursor would land
	// on DefaultCursor and put an arrow on a resize edge.
	//
	// Give two of the three names something to resolve to first, the way a
	// project's AppCore does at boot. The editor on its own registers no
	// cursors under them (its theme builds them anonymously), and the third is
	// deliberately left unregistered to cover the other branch.
	//
	// editorMode off while naming: in editor mode assignName stashes a name
	// instead of registering it, which is right for a control being authored
	// and wrong for these.
	editorMode(false);
	$sCorner = new GuiCursor();
	$sCorner.setName("NWSECursor");
	$sHorizontal = new GuiCursor();
	$sHorizontal.setName("LeftRightCursor");
	editorMode(true);

	GuiEditor.detachTheme($sTheme, 0);

	sCheck("corner cursor detached to its canonical name",
		$sWindow.nWSECursor $= "NWSECursor");
	sCheck("horizontal cursor detached to its canonical name",
		$sWindow.leftRightCursor $= "LeftRightCursor");
	sCheck("nothing was left pointing at the theme",
		$sWindow.nWSECursor !$= $sExtra.getName());

	// The third had no canonical cursor to land on, so the field cleared. That
	// is safe rather than broken: an empty cursor slot is what every untouched
	// control has, and the engine re-resolves it by name the next time the
	// pointer is over the control (guiTextEditCtrl.cc getCursor).
	sCheck("a slot with no canonical cursor cleared instead of dangling",
		$sEdit.editCursor $= "");

	$sCorner.delete();
	$sHorizontal.delete();

	echo("CSSMOKE DONE");
	schedule(300, 0, "quit");
}
