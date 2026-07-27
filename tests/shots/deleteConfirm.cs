// Temporary: screenshots the Profile Editor toolbar with a stand-alone profile
// selected (Rename and Delete both enabled) and the Delete confirmation, whose
// message is longer than the theme one and has to fit the fixed text area.

setLogMode(2);
setScriptExecEcho(false);
trace(false);
$Scripts::ignoreDSOs = true;
setCompanyAndProduct("Torque Game Engines", "Torque2D");
ModuleDatabase.EchoInfo = false;
AssetDatabase.EchoInfo = false;

testExec("editor/main.cs");

schedule(2500, 0, "shotStep1");

function shotStep1()
{
	ProjectManager.setProjectFolder("smokeThemeProject");
	GuiEditor.open();
	GuiEditor.openProfileEditor();

	%dialog = GuiEditor.profileEditorDialog;
	%dialog.onNewStandalone();
	%nameDialog = %dialog.childDialog;
	%nameDialog.nameBox.setText("RubyButton");
	%nameDialog.onDone();

	%proxy = %dialog.library.standaloneFolder.getObject(0);
	%dialog.onTreeSelect(%proxy);
	%dialog.profileForm.categoryDrop.setSelected(
		%dialog.profileForm.categoryDrop.findItemText("Button", false));

	schedule(900, 0, "shotStep2");
}

function shotStep2()
{
	screenShot(testRoot("shots/standaloneSelected.png"), "PNG");
	GuiEditor.profileEditorDialog.onDelete();
	schedule(900, 0, "shotStep3");
}

function shotStep3()
{
	screenShot(testRoot("shots/deleteStandaloneConfirm.png"), "PNG");
	schedule(400, 0, "shotStep4");
}

function shotStep4()
{
	// Nothing was saved, so there is no file to clean up.
	echo("SHOTS DONE");
	schedule(250, 0, "quit");
}
