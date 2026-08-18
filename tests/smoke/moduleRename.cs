// The Project Manager's two module rename paths: New Module from a template,
// and Edit Module changing a module's name.
//
// Both used to rewrite ModuleID in module.taml and stop there. The engine calls
// <ModuleId>::<CreateFunction>, so a module renamed that way loaded, reported
// its new name everywhere the UI looked, and silently did nothing -- and its
// <oldId>:<asset> ids pointed at a module that no longer existed. Nothing about
// the module.taml those paths wrote looked wrong, which is why this checks the
// script and the gui rather than the definition.
//
// The New Project dialog is the third path and has its own suite; what is
// specific here is that these two go through the Project Manager's own panel,
// against a project folder named by a relative path.
//
// Run: tests/run.ps1 moduleRename ; grep MODREN in tests/logs/.
//-----------------------------------------------------------------------------

setLogMode(1);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

function mrCheck(%label, %cond)
{
	if(%cond) echo("MODREN PASS: " @ %label);
	else      echo("MODREN FAIL: " @ %label);
}

function mrRead(%path)
{
	%file = new FileObject();
	if(!%file.openForRead(%path))
	{
		%file.delete();
		return "";
	}

	%text = "";
	while(!%file.isEOF())
	{
		%text = %text @ %file.readLine() @ "\n";
	}
	%file.close();
	%file.delete();

	return %text;
}

// Case insensitive: Taml writes a dynamic field under the name the string table
// interned, which is lower case.
function mrFileHas(%path, %needle)
{
	return strpos(strlwr(mrRead(%path)), strlwr(%needle)) != -1;
}

// The three places a module id is written, checked together because a rename
// that gets one and misses another is exactly the failure this is about.
function mrCheckModule(%label, %folder, %id, %gone)
{
	%path = testRoot("moduleRenameSmokeProject/" @ %folder);

	mrCheck(%label @ ": the folder is named for the module", isDirectory(%path));
	mrCheck(%label @ ": module.taml carries the id", mrFileHas(pathConcat(%path, "module.taml"), "ModuleId=\"" @ %id @ "\""));
	mrCheck(%label @ ": create is on the new namespace", mrFileHas(pathConcat(%path, "game.cs"), "function " @ %id @ "::create"));
	mrCheck(%label @ ": destroy is on the new namespace", mrFileHas(pathConcat(%path, "game.cs"), "function " @ %id @ "::destroy"));
	mrCheck(%label @ ": the asset id in script was rewritten", mrFileHas(pathConcat(%path, "game.cs"), "\"" @ %id @ ":planetfall\""));
	mrCheck(%label @ ": the asset id in the gui was rewritten", mrFileHas(pathConcat(%path, "gui/defaultGui.gui.taml"), %id @ ":torqueBG"));

	mrCheck(%label @ ": module.taml has no trace of " @ %gone, !mrFileHas(pathConcat(%path, "module.taml"), %gone));
	mrCheck(%label @ ": the script has no trace of " @ %gone, !mrFileHas(pathConcat(%path, "game.cs"), %gone));
	mrCheck(%label @ ": the gui has no trace of " @ %gone, !mrFileHas(pathConcat(%path, "gui/defaultGui.gui.taml"), %gone));
}

//-----------------------------------------------------------------------------

testExec("editor/main.cs");
schedule(2000, 0, "mrStep1");

// Spelled out rather than held in a variable: tests/run.ps1 finds the folder to
// delete by reading this file for setProjectFolder("..."), so a name it cannot
// see is a folder it cannot sweep.
function mrStep1()
{
	ProjectManager.setProjectFolder("moduleRenameSmokeProject");
	createPath(testRoot("moduleRenameSmokeProject/"));

	// The BlankGame template depends on Audio, so the last step cannot load the
	// renamed module unless Audio is in the project beside it.
	ModuleDatabase.scanModules(testRoot("library"));
	ModuleDatabase.CopyModule("Audio", 1, "Audio", testRoot("moduleRenameSmokeProject"), true);
	ModuleDatabase.clearDatabase();

	// The dialog offers templates by display name and hands back a module id, so
	// the two have to still agree after the ModuleManager it read them from is
	// gone.
	%width = 500;
	%height = 190;
	%dialog = new GuiControl()
	{
		class = "NewModuleDialog";
		superclass = "EditorDialog";
		dialogSize = (%width + 8) SPC (%height + 8);
		dialogCanClose = true;
		dialogText = "Create Module";
	};
	%dialog.init(%width, %height);
	Canvas.pushDialog(%dialog);

	mrCheck("the template dropdown offers Blank Game", %dialog.templateDropDown.findItemText("Blank Game", false) != -1);
	mrCheck("the template dropdown offers Art Pack", %dialog.templateDropDown.findItemText("Art Pack", false) != -1);
	mrCheck("nothing is offered under its raw module id", %dialog.templateDropDown.findItemText("BlankGame", false) == -1);
	mrCheck("no template means no template", %dialog.getSelectedTemplate() $= "none");

	%dialog.templateDropDown.setSelected(%dialog.templateDropDown.findItemText("Blank Game", false));
	mrCheck("a display name maps back to its module id", %dialog.getSelectedTemplate() $= "BlankGame");

	%dialog.onClose();

	// What NewModuleDialog posts when someone picks a template and names it.
	%data = new ScriptObject()
	{
		template = "BlankGame";
		moduleName = "ArcadeCore";
		path = testRoot("moduleRenameSmokeProject/ArcadeCore");
	};

	ProjectManager.gamePanel.onModuleCreated(%data);
	%data.delete();

	mrCheckModule("new module", "ArcadeCore", "ArcadeCore", "BlankGame");

	// A copy of a template is a module of its own, not something to stamp out
	// again and not a Game Core.
	%definition = testRoot("moduleRenameSmokeProject/ArcadeCore/module.taml");
	mrCheck("new module: the template markers are gone", !mrFileHas(%definition, "Template=") && !mrFileHas(%definition, "DisplayName="));
	mrCheck("new module: the game core type is gone", !mrFileHas(%definition, "Type="));

	schedule(500, 0, "mrStep2");
}

// And renaming it again, through Edit Module.
function mrStep2()
{
	%module = ModuleDatabase.findModule("ArcadeCore", 1);
	mrCheck("the new module registered", isObject(%module));
	if(!isObject(%module))
	{
		echo("MODREN DONE");
		schedule(200, 0, "quit");
		return;
	}

	// EditModuleDialog edits the module the card is showing, so that is what has
	// to be selected for the panel to act on it.
	ProjectManager.gamePanel.card.activeModule = %module;

	%data = new ScriptObject()
	{
		moduleID = "PinballCore";
		versionID = %module.versionID;
		buildID = %module.buildID;
		description = "Renamed by the module rename smoke test.";
		type = "";
		author = "Smoke Tester";
	};

	ProjectManager.gamePanel.onModuleEdited(%data);
	%data.delete();

	mrCheck("the old folder is gone", !isDirectory(testRoot("moduleRenameSmokeProject/ArcadeCore")));
	mrCheckModule("renamed module", "PinballCore", "PinballCore", "ArcadeCore");

	schedule(500, 0, "mrStep3");
}

// The proof: load it and see create run. A module whose namespace still said
// ArcadeCore would load without complaint and leave the Canvas empty.
function mrStep3()
{
	ModuleDatabase.scanModules(testRoot("moduleRenameSmokeProject"));

	%module = ModuleDatabase.findModule("PinballCore", 1);
	mrCheck("the renamed module registers under its new id", isObject(%module));

	if(isObject(%module))
	{
		ModuleDatabase.loadExplicit("PinballCore", 1);
		mrCheck("the module reports itself loaded", ModuleDatabase.isModuleLoaded("PinballCore"));

		%content = Canvas.getContent();
		mrCheck("create ran and put its gui on the Canvas", isObject(%content) && %content.getName() $= "DefaultGui");

		ModuleDatabase.unloadExplicit("PinballCore", 1);
	}

	echo("MODREN DONE");
	schedule(200, 0, "quit");
}
