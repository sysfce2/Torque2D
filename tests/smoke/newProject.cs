// New Project smoke test. Drives the New Project dialog and then reads what it
// left on disk.
//
// The thing under test is the rename. A project is stamped out of a library
// game core, and until now the copy kept the template's name -- so every
// project on disk had a module called BlankGame in it. Renaming it is not one
// edit: the id is in module.taml, it is the namespace the engine calls create
// and destroy on in the module's script, and it is the prefix of every asset id
// the module uses, in script and in taml alike. Miss any of those and the
// module still loads, still reports the new name, and silently does nothing.
//
// So the checks below are not "is the id right" but "is the id right in all
// four places", and the last step loads the module for real: if create did not
// fire, the Canvas has no content, and no amount of correct-looking taml makes
// up for that.
//
// Driven by calling the dialog rather than by posting input: what a click would
// be testing is where the boxes ended up, and every one of them is read back by
// name here anyway.
//
// Run: tests/run.ps1 newProject ; grep NEWPROJ in tests/logs/.
//-----------------------------------------------------------------------------

setLogMode(1);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

function npCheck(%label, %cond)
{
	if(%cond) echo("NEWPROJ PASS: " @ %label);
	else      echo("NEWPROJ FAIL: " @ %label);
}

// FileObject reads through the ResourceManager, which falls back to a real stat
// for a path it has never scanned -- which is every file this test wrote.
function npRead(%path)
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

// Case insensitive, and deliberately so. Taml writes a dynamic field under the
// name the string table interned, which is lower case -- AppCore's Project comes
// back out of a round trip as project -- so a case sensitive search would miss
// half of what is being looked for, in both directions.
function npFileHas(%path, %needle)
{
	return strpos(strlwr(npRead(%path)), strlwr(%needle)) != -1;
}

// Case sensitive, for the one thing here where case IS the subject.
function npFileHasExact(%path, %needle)
{
	return strpos(npRead(%path), %needle) != -1;
}

// Where a control ends, in its parent's coordinates.
function npBottomOf(%control)
{
	return getWord(%control.getPosition(), 1) + getWord(%control.getExtent(), 1);
}

//-----------------------------------------------------------------------------

testExec("editor/main.cs");
schedule(2000, 0, "npStep1");

// Spelled out rather than held in a variable: tests/run.ps1 finds the folder to
// delete by reading this file for setProjectFolder("..."), so a name it cannot
// see is a folder it cannot sweep. Everything this test makes is inside it.
function npStep1()
{
	ProjectManager.setProjectFolder("newProjectSmokeProject");

	// The same size EditorProjectSelector::onNewProject builds it at, because the
	// layout checks below are about that size being enough.
	%width = 700;
	%height = 470;
	%dialog = new GuiControl()
	{
		class = "NewProjectDialog";
		superclass = "EditorDialog";
		dialogSize = (%width + 8) SPC (%height + 8);
		dialogCanClose = true;
		dialogResizable = false;
		dialogText = "New Project";
	};
	%dialog.init(%width, %height);
	Canvas.pushDialog(%dialog);

	$npDialog = %dialog;

	// A window spends 34 pixels of its height on the title bar and its border, so
	// a control placed from the dialog's own height instead of from what is left
	// hangs past the bottom and puts the whole form behind a scroll bar. That is
	// invisible in the arithmetic and obvious on screen, which is exactly the kind
	// of thing worth pinning down here.
	npCheck("the form fits the content pane", npBottomOf(%dialog.form) <= %dialog.contentHeight());
	npCheck("the feedback line fits the content pane", npBottomOf(%dialog.feedback) <= %dialog.contentHeight());
	npCheck("the cancel button fits the content pane", npBottomOf(%dialog.cancelButton) <= %dialog.contentHeight());
	npCheck("the create button fits the content pane", npBottomOf(%dialog.createButton) <= %dialog.contentHeight());
	npCheck("the feedback line clears the buttons", npBottomOf(%dialog.feedback) <= getWord(%dialog.createButton.getPosition(), 1));

	// Every field says what it is for, on the caption as well as on the input.
	npCheck("the title has a tooltip", %dialog.titleBox.tooltip !$= "");
	npCheck("the directory has a tooltip", %dialog.dirBox.tooltip !$= "");
	npCheck("the game core has a tooltip", %dialog.coreDropDown.tooltip !$= "");
	npCheck("the module name has a tooltip", %dialog.moduleNameBox.tooltip !$= "");
	npCheck("the author has a tooltip", %dialog.authorBox.tooltip !$= "");
	npCheck("the description has a tooltip", %dialog.descBox.tooltip !$= "");
	npCheck("the captions carry the tooltip too", %dialog.descBox.getGroup().tooltip $= %dialog.descBox.tooltip);

	// The library ships one game core. Whatever else is in there, Blank Game has
	// to be offered, and nothing that is not a game core may be.
	npCheck("game core dropdown is populated", %dialog.coreDropDown.getItemCount() > 0);
	npCheck("Blank Game is offered by its display name", %dialog.coreDropDown.findItemText("Blank Game", false) != -1);
	npCheck("Art Pack is not offered as a game core", %dialog.coreDropDown.findItemText("Art Pack", false) == -1);
	npCheck("the selected core is a module id", %dialog.selectedGameCore() $= "BlankGame");

	// Nothing typed yet, so there is nothing to create.
	npCheck("create is inactive while empty", !%dialog.createButton.active);

	schedule(100, 0, "npStep2");
}

// The module name follows the title until someone types their own.
function npStep2()
{
	%dialog = $npDialog;

	%dialog.titleBox.setText("New Project Smoke");
	%dialog.onKeyPressed(%dialog.titleBox);
	npCheck("module name follows the title", %dialog.moduleNameBox.getText() $= "NewProjectSmokeGame");

	%dialog.moduleNameBox.setText("SmokeGame");
	%dialog.onKeyPressed(%dialog.moduleNameBox);
	%dialog.titleBox.setText("New Project Smoke Test");
	%dialog.onKeyPressed(%dialog.titleBox);
	npCheck("an edited module name stops following", %dialog.moduleNameBox.getText() $= "SmokeGame");

	// Validate stops at the first thing wrong with the form, so the directory
	// goes in before the module name is worth asking about.
	%dialog.dirBox.setText("newProjectSmokeProject");

	// A module name becomes a folder, an id, and a script namespace.
	%dialog.moduleNameBox.setText("Smoke Game");
	npCheck("a space in the module name is refused", !%dialog.validate());
	%dialog.moduleNameBox.setText("2SmokeGame");
	npCheck("a leading digit is refused", !%dialog.validate());
	%dialog.moduleNameBox.setText("AppCore");
	npCheck("a name already used by the project is refused", !%dialog.validate());
	%dialog.moduleNameBox.setText("SmokeGame");

	npCheck("no description means no create", !%dialog.validate());

	%dialog.authorBox.setText("Smoke Tester");
	%dialog.descBox.setText("A project made by the New Project smoke test.");
	npCheck("a filled in form validates", %dialog.validate());

	%dialog.onCreate();

	schedule(500, 0, "npStep3");
}

// What landed on disk.
function npStep3()
{
	%project = testRoot("newProjectSmokeProject");
	%module = pathConcat(%project, "SmokeGame");

	npCheck("the game module is named after the module name", isDirectory(%module));
	npCheck("no folder is left named after the template", !isDirectory(pathConcat(%project, "BlankGame")));
	npCheck("AppCore came with it", isDirectory(pathConcat(%project, "AppCore")));
	npCheck("Audio came with it", isDirectory(pathConcat(%project, "Audio")));
	npCheck("the stock theme came with it", isDirectory(pathConcat(%project, "themes")));

	// The module definition kept its own filename. CopyModule renames it to
	// <ModuleId>.module.taml when the ids differ, which is not the name any of
	// the editor's module code opens.
	%definition = pathConcat(%module, "module.taml");
	npCheck("the definition is still called module.taml", npFileHas(%definition, "ModuleDefinition"));
	npCheck("the id is the module name", npFileHas(%definition, "ModuleId=\"SmokeGame\""));
	npCheck("the definition mentions the template nowhere", !npFileHas(%definition, "BlankGame"));
	npCheck("the module launches with the project", npFileHas(%definition, "Group=\"launch\""));
	npCheck("the type says what it is", npFileHas(%definition, "Type=\"Game Module\""));
	npCheck("the author is the one typed in", npFileHas(%definition, "Author=\"Smoke Tester\""));
	npCheck("the description is the one typed in", npFileHas(%definition, "A project made by the New Project smoke test."));
	npCheck("the template's own description is gone", !npFileHas(%definition, "ready for you to craft"));
	npCheck("the template markers are gone", !npFileHas(%definition, "Template=") && !npFileHas(%definition, "DisplayName="));

	// The declared paths are directories on a case sensitive filesystem, and a
	// taml round trip used to fold them to whatever spelling the string table
	// happened to hold -- so a project came out declaring Sprites and Fonts,
	// and anything put in sprites/ afterwards was never scanned. Checked
	// exactly, because the whole point is the spelling.
	npCheck("the declared sprites path keeps its spelling", npFileHasExact(%definition, "Path=\"sprites\""));
	npCheck("the declared fonts path keeps its spelling", npFileHasExact(%definition, "Path=\"fonts\""));
	npCheck("the declared extensions keep their spelling", npFileHasExact(%definition, "Extension=\"image.taml\""));
	npCheck("the script file keeps its spelling", npFileHasExact(%definition, "ScriptFile=\"game.cs\""));

	// The namespace the engine calls, and an asset id, in a file no taml visitor
	// can reach.
	%script = pathConcat(%module, "game.cs");
	npCheck("create is on the new namespace", npFileHas(%script, "function SmokeGame::create"));
	npCheck("destroy is on the new namespace", npFileHas(%script, "function SmokeGame::destroy"));
	npCheck("the asset id in script was rewritten", npFileHas(%script, "\"SmokeGame:planetfall\""));
	npCheck("the script mentions the template nowhere", !npFileHas(%script, "BlankGame"));

	// And an asset id in taml.
	%gui = pathConcat(%module, "gui/defaultGui.gui.taml");
	npCheck("the asset id in the gui was rewritten", npFileHas(%gui, "SmokeGame:torqueBG"));
	npCheck("the gui mentions the template nowhere", !npFileHas(%gui, "BlankGame"));

	// The project's own identity, which lives on AppCore.
	%appCore = pathConcat(%project, "AppCore/1/module.taml");
	npCheck("the project is titled", npFileHas(%appCore, "Project=\"New Project Smoke Test\""));
	npCheck("the project is described", npFileHas(%appCore, "ProjectDescription=\"A project made by the New Project smoke test.\""));

	schedule(500, 0, "npStep4");
}

// The proof the rename is complete: load the module and see create run. A module
// whose namespace still said BlankGame would load without complaint and leave
// the Canvas empty.
function npStep4()
{
	// The whole project, not just the module: loadExplicit pulls the module's
	// Audio dependency in, and it has to be registered to be found.
	ModuleDatabase.scanModules(testRoot("newProjectSmokeProject"));
	%module = ModuleDatabase.findModule("SmokeGame", 1);
	npCheck("the renamed module registers under its new id", isObject(%module));

	if(isObject(%module))
	{
		ModuleDatabase.loadExplicit("SmokeGame", 1);
		npCheck("the module reports itself loaded", ModuleDatabase.isModuleLoaded("SmokeGame"));

		%content = Canvas.getContent();
		npCheck("create ran and put its gui on the Canvas", isObject(%content) && %content.getName() $= "DefaultGui");

		ModuleDatabase.unloadExplicit("SmokeGame", 1);
	}

	echo("NEWPROJ DONE");
	schedule(200, 0, "quit");
}
