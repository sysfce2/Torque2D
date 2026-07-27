// Border-pane smoke test. Drives the Profile Editor's new custom border pane
// (GuiProfileEditorBorderForm) that replaces the inspector when a border node is
// selected: it verifies the three-way Properties toggle, that the shared grid
// binds/edits the selected border in place, and that underfill commits.
// Run: tests/run.ps1 borderPane  ; grep PBSMOKE in console.log.

setLogMode(2);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

function pCheck(%label, %cond)
{
	if(%cond) echo("PBSMOKE PASS: " @ %label);
	else      echo("PBSMOKE FAIL: " @ %label);
}

testExec("editor/main.cs");
schedule(2000, 0, "pStep1");

function pStep1()
{
	ProjectManager.setProjectFolder("borderPaneSmokeProject");
	GuiEditor.open();
	GuiEditor.openProfileEditor();
	%d = GuiEditor.profileEditorDialog;
	pCheck("dialog opened", isObject(%d));

	%theme = %d.library.createTheme("PBSmoke");
	%d.tree.refresh();

	// --- Select a border node: the border pane replaces the inspector. ---
	%bname = getWord(%theme.getBorderCategoryNames(), 0);
	pCheck("theme has a named border", %bname !$= "");
	%border = %theme.getBorder(%bname);
	pCheck("border object resolved", isObject(%border));

	%bproxy = new ScriptObject()
	{
		kind = "border";
		theme = %theme;
		category = %bname;
		treeLabel = %bname;
	};
	%d.onTreeSelect(%bproxy);

	pCheck("border form scroller visible", %d.borderFormScroller.isVisible());
	pCheck("profile form hidden for border", !%d.profileFormScroller.isVisible());
	pCheck("theme form hidden for border", !%d.formScroller.isVisible());
	pCheck("borders pane hidden for border", !%d.bordersWindow.isVisible());
	pCheck("current member is the border", %d.currentMember == %border.getId());
	pCheck("form bound to the border", %d.borderForm.border == %border.getId());
	pCheck("name header shows the border name",
		strstr(%d.borderForm.nameLabel.getText(), %bname) >= 0);

	// --- Edit a numeric value in place through the shared grid. ---
	%box = %d.borderForm.grid.box["border", 0];
	pCheck("grid box present", isObject(%box));
	%old = %border.border;
	%box.setText(%old + 3);
	%d.borderForm.grid.commitBox(%box);
	pCheck("border thickness edited in place", %border.border == (%old + 3));
	pCheck("theme marked dirty by edit", %d.library.isDirty());

	// --- Toggle underfill (now part of the shared grid). ---
	%prevUnderfill = %border.underfill;
	%d.borderForm.grid.underfillBox.setStateOn(!%prevUnderfill);
	%d.borderForm.grid.commitUnderfill();
	pCheck("underfill toggled in place", %border.underfill == (!%prevUnderfill));

	schedule(400, 0, "pStep2");
}

// Verify the three-way toggle: theme node -> theme form; profile node ->
// inspector + borders pane; both hide the border form.
function pStep2()
{
	echo("PBSMOKE: pStep2 running");
	%d = GuiEditor.profileEditorDialog;
	%theme = %d.currentRoot;   // the theme selected in pStep1 (avoids name shadowing)

	// --- Profile node: inspector + borders pane; border form hides. ---
	%cproxy = %d.library.categoryProxy[%theme.getId() @ "_Button"];
	pCheck("category proxy found", isObject(%cproxy));
	echo("PBSMOKE: selecting category proxy");
	%d.onTreeSelect(%cproxy);
	echo("PBSMOKE: category proxy selected");
	pCheck("profile form shown for profile node", %d.profileFormScroller.isVisible());
	pCheck("borders pane shown for profile node", %d.bordersWindow.isVisible());
	pCheck("border form hidden for profile node", !%d.borderFormScroller.isVisible());

	// --- Re-select the border node: border form comes back, inspector hides. ---
	%bname = getWord(%theme.getBorderCategoryNames(), 0);
	%bproxy = new ScriptObject() { kind = "border"; theme = %theme; category = %bname; treeLabel = %bname; };
	%d.onTreeSelect(%bproxy);
	pCheck("border form shown again on reselect", %d.borderFormScroller.isVisible());
	pCheck("profile form hidden again on reselect", !%d.profileFormScroller.isVisible());

	// (The theme-node branch runs the pre-existing themeForm/preview path, which
	// is exercised by smoke/profileEditor.cs -- not re-tested here.)

	echo("PBSMOKE DONE");
	schedule(300, 0, "quit");
}
