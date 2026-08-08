//-----------------------------------------------------------------------------
// Save Gui As, and the one thing its form owes the person filling it in: the
// Save button says whether what they have typed can be saved.
//
// Nothing here saves. Validate is the whole subject, and the folder it is
// pointed at is real content in the PlanetX project -- writing a file into that
// to prove a button is grey would be a poor trade.
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

function sdCheck(%label, %condition)
{
	echo(%condition ? ("SAVED PASS: " @ %label) : ("SAVED FAIL: " @ %label));
}

// A dialog is pushed onto the Canvas and nothing keeps a handle to it, so it is
// found the way it is displayed: as the Canvas's newest child.
function sdDialog(%class)
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

schedule(2000, 0, "sdSetup");

function sdSetup()
{
	ProjectManager.setProjectFolder("PlanetX");
	ModuleDatabase.ScanModules("PlanetX");
	ModuleDatabase.LoadExplicit("AppCore", 1);

	GuiEditor.open();

	schedule(300, 0, "sdStepValidate");
}

function sdStepValidate()
{
	GuiEditor.SaveGuiAs();

	$sdDialog = sdDialog("GuiEditorSaveGuiDialog");
	sdCheck("the Save Gui dialog opened", isObject($sdDialog));

	// A Gui that has never been saved opens the form with no target folder, which
	// is the first thing it asks for.
	sdCheck("an unfilled form does not validate", !$sdDialog.validate());
	sdCheck("and Save is not offered", !$sdDialog.saveButton.isActive());

	// A real module folder, which is what the form is holding out for: it refuses
	// anything outside a module, and anything inside a library one.
	$sdDialog.folderBox.setText("PlanetX/PlanetXGame/gui");
	$sdDialog.guiNameBox.setText("smokeSaveDialog.gui");

	sdCheck("a filled form validates", $sdDialog.validate());
	sdCheck("and Save is offered", $sdDialog.saveButton.isActive());

	// And back, because a form that only ever enables is a form that never told
	// the truth in the first place.
	$sdDialog.guiNameBox.setText("");

	sdCheck("clearing the name invalidates it again", !$sdDialog.validate());
	sdCheck("and Save is withdrawn", !$sdDialog.saveButton.isActive());

	schedule(300, 0, "sdDone");
}

function sdDone()
{
	// Closed rather than left standing, so the process exits through the same
	// path a person's Cancel would take.
	$sdDialog.onClose();

	echo("SAVED DONE");
	quit();
}
