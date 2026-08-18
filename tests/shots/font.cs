// Visual check for the font work: the theme pane with its three font drop-downs
// and no font-directory row, and the profile pane's Font Face drop-down open on
// the machine's installed fonts.
// Run: tests/run.ps1 -Shots font ; look in shots/.

setLogMode(1);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

testExec("editor/main.cs");
schedule(2500, 0, "fshStep1");

function fshStep1()
{
	createPath(testRoot("shots/"));
	ProjectManager.setProjectFolder("fontShotProject");
	GuiEditor.open();
	GuiEditor.openProfileEditor();

	%d = GuiEditor.profileEditorDialog;
	$fshTheme = %d.library.createTheme("FontShotTheme");
	%d.tree.refresh();

	%d.onTreeSelect(%d.library.themeProxy[$fshTheme.getId()]);
	schedule(400, 0, "fshShoot", "themePane");
}

function fshStep2()
{
	%d = GuiEditor.profileEditorDialog;
	%d.onTreeSelect(%d.library.categoryProxy[$fshTheme.getId() @ "_Button"]);
	schedule(400, 0, "fshShoot", "profilePane");
}

function fshStep3()
{
	%d = GuiEditor.profileEditorDialog;
	%d.profileForm.row["fontType"].editor.openDropDown();
	schedule(400, 0, "fshShoot", "faceList");
}

function fshStep4()
{
	quit();
}

function fshShoot(%name)
{
	screenShot(testRoot("shots/font_" @ %name @ ".png"), "PNG");
	echo("FSHOT wrote shots/font_" @ %name @ ".png");

	if(%name $= "themePane")       schedule(200, 0, "fshStep2");
	else if(%name $= "profilePane") schedule(200, 0, "fshStep3");
	else                            schedule(200, 0, "fshStep4");
}
