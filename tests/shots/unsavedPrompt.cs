//-----------------------------------------------------------------------------
// The unsaved-changes prompt, for looking at. Three buttons and a message whose
// length depends on the document's name, in a box of a fixed size -- which is
// the shape of the bug that made the Profile Editor's confirm dialog grow to
// fit its message.
//
// Two shots: a Gui that has never been saved (the longest name it can have is
// the default one) and a saved one.
//-----------------------------------------------------------------------------

setLogMode(2);
setScriptExecEcho(false);
trace(false);
$Scripts::ignoreDSOs = true;
setCompanyAndProduct("Torque Game Engines", "Torque2D");
ModuleDatabase.EchoInfo = false;
AssetDatabase.EchoInfo = false;

testExec("editor/main.cs");

createPath(testRoot("shots/"));
schedule(2500, 0, "upOpenProject");

// The long way round rather than GuiEditor.open(), because the point of a shot
// is the picture: opening the editor the way a person does is what puts the
// editor's own chrome behind the dialog instead of a black canvas.
function upOpenProject()
{
	ProjectManager.setProjectFolder("PlanetX");
	EditorCore.projectSelector.onProjectSelected(pathConcat(getMainDotCsDir(), "PlanetX"));
	schedule(2500, 0, "upOpenEditor");
}

// Pages register in load order: EditorConsole, ProjectManager, AssetAdmin,
// GuiEditor.
function upOpenEditor()
{
	EditorCore.toggleEditor();
	EditorCore.tabBook.selectPage(3);
	schedule(1500, 0, "upStep1");
}

function upStep1()
{
	%ctrl = new GuiButtonCtrl() { Position = "10 10"; Extent = "80 30"; Text = "A"; };
	GuiEditor.rootGui.add(%ctrl);

	GuiEditor.inspectorWindow.pane.bind(%ctrl);
	GuiEditor.inspectorWindow.pane.writeField("Text", "unsaved work");

	GuiEditor.NewGui();
	schedule(600, 0, "upStep2");
}

function upStep2()
{
	screenShot(testRoot("shots/unsavedPromptUntitled.png"), "PNG");
	schedule(400, 0, "upStep3");
}

function upStep3()
{
	discardUnsavedPrompt();

	// And again with a name that came from a file, which is the longer message.
	%ctrl = new GuiButtonCtrl() { Position = "10 10"; Extent = "80 30"; Text = "A"; };
	GuiEditor.rootGui.add(%ctrl);

	GuiEditor.fileName = "titleScreen.gui";
	GuiEditor.refreshDocumentTitle();

	GuiEditor.inspectorWindow.pane.bind(%ctrl);
	GuiEditor.inspectorWindow.pane.writeField("Text", "unsaved work");

	GuiEditor.NewGui();
	schedule(600, 0, "upStep4");
}

function upStep4()
{
	screenShot(testRoot("shots/unsavedPromptNamed.png"), "PNG");
	schedule(400, 0, "upDone");
}

function upDone()
{
	discardUnsavedPrompt();
	echo("PROMPT SHOTS DONE");
	quit();
}
