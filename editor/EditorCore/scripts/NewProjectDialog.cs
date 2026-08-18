
function NewProjectDialog::init(%this, %width, %height)
{
	//Get the dialog contents
	%window = %this.getObject(0);
	%content = %window.getObject(0);

	//Create the file text box
	%form = new GuiGridCtrl()
	{
		class = "EditorForm";
		extent = %width SPC %height;
		cellSizeX = %width;
		cellSizeY = 50;
	};
	%form.addListener(%this);

	%item = %form.addFormItem("Title", %width SPC 30);
	%this.titleBox = %form.createTextEditItem(%item);
	%form.setItemTip(%item, %this.titleBox, "What your project is called. This is the name on its card in the project picker, so write it for people to read - spaces and punctuation are fine.");

	%item = %form.addFormItem("Directory", %width SPC 30);
	%this.dirBox = %form.createTextEditItem(%item);
	%form.setItemTip(%item, %this.dirBox, "The folder your project lives in, made inside the Torque2D folder. It has to be empty or not exist yet, so nothing you already have is written over.");

	%item = %form.addFormItem("Game Core", %width SPC 30);
	%this.coreDropDown = %form.createDropDownItem(%item);
	%this.populateGameCores();
	%form.setItemTip(%item, %this.coreDropDown, "Which template your game is copied from. Blank Game gives you a window, a background and some music, ready to be replaced with your own.");

	%item = %form.addFormItem("Module Name", %width SPC 30);
	%this.moduleNameBox = %form.createTextEditItem(%item);
	%form.setItemTip(%item, %this.moduleNameBox, "What to call your game module. It becomes the module's folder, its ModuleId, and the namespace your game's create and destroy functions hang off - so letters, numbers and underscores only. It follows the title until you edit it.");

	%item = %form.addFormItem("Author", %width SPC 30);
	%this.authorBox = %form.createTextEditItem(%item);
	%form.setItemTip(%item, %this.authorBox, "Who to credit for the game module. Optional: left empty, the Project Manager credits Torque2D. You can change it later under Edit Module.");

	%item = %form.addFormItem("Description", %width SPC 30);
	%this.descBox = %form.createTextEditItem(%item);
	%form.setItemTip(%item, %this.descBox, "A sentence saying what the project is. It shows under the title on the project card and on the game module in the Project Manager.");

	%this.form = %form;
	%content.add(%form);

	// Positioned from the room the content actually gets rather than from the
	// dialog's own height: the window spends 34 pixels of it on the title bar and
	// its border (EditorDialog::contentHeight), and a button placed past that puts
	// the whole form behind a scroll bar.
	%formHeight = 6 * 50;
	%buttonTop = %this.contentHeight() - 46;

	%this.feedback = new GuiControl()
	{
		HorizSizing = "right";
		VertSizing = "bottom";
		Position = "12" SPC (%formHeight + 10);
		Extent = (%width - 24) SPC (%buttonTop - %formHeight - 28);
		text = "";
		textWrap = true;
		textExtend = true;
	};
	ThemeManager.setProfile(%this.feedback, "infoProfile");

	%this.cancelButton = new GuiButtonCtrl()
	{
		HorizSizing = "right";
		VertSizing = "bottom";
		Position = "478" SPC (%buttonTop + 2);
		Extent = "100 30";
		Text = "Cancel";
		Command = %this.getID() @ ".onClose();";
	};
	ThemeManager.setProfile(%this.cancelButton, "buttonProfile");

	%this.createButton = new GuiButtonCtrl()
	{
		HorizSizing = "right";
		VertSizing = "bottom";
		Position = "588" SPC %buttonTop;
		Extent = "100 34";
		Text = "Create";
		Command = %this.getID() @ ".onCreate();";
	};
	ThemeManager.setProfile(%this.createButton, "primaryButtonProfile");

	%content.add(%this.feedback);
	%content.add(%this.cancelButton);
	%content.add(%this.createButton);

	%this.validate();
}

// The game cores are the library templates a project can be built out of. A
// private ModuleManager rather than ModuleDatabase: the project selector has
// nothing scanned at this point and this must not leave anything behind that
// would show up as a module of the project about to be created.
function NewProjectDialog::populateGameCores(%this)
{
	%manager = new ModuleManager();
	%manager.EchoInfo = false;
	%manager.ScanModules(pathConcat(getMainDotCsDir(), "library"));

	%cores = %manager.findModuleTypes("Game Core", false);
	for(%i = 0; %i < getWordCount(%cores); %i++)
	{
		%core = getWord(%cores, %i);
		if(!%core.Template)
		{
			continue;
		}

		// The dropdown shows a display name and sortByText reorders it, so the id
		// is remembered against the name. The module definitions do not outlive
		// the manager.
		%name = ModuleStamper.displayName(%core);
		%this.coreDropDown.addItem(%name);
		%this.coreID[%name] = %core.ModuleID;
	}

	%manager.delete();

	%this.coreDropDown.sortByText();
	if(%this.coreDropDown.getItemCount() > 0)
	{
		%this.coreDropDown.setSelected(0);
	}
}

function NewProjectDialog::selectedGameCore(%this)
{
	return %this.coreID[%this.coreDropDown.getText()];
}

// Titles are written for people and module ids are written for the script
// compiler, so the suggestion is the title with everything a namespace cannot
// carry taken out of it.
function NewProjectDialog::suggestModuleName(%this)
{
	%title = %this.titleBox.getText();
	%name = "";

	for(%i = 0; %i < strlen(%title); %i++)
	{
		%char = getSubStr(%title, %i, 1);
		if(ModuleStamper.isIdentifierChar(%char))
		{
			%name = %name @ %char;
		}
	}

	if(%name $= "")
	{
		return "";
	}

	return %name @ "Game";
}

// The module name follows the title until the moment someone types their own,
// and from then on it is theirs. setText does not run the box's command, so the
// guard is belt and braces against that changing.
function NewProjectDialog::onKeyPressed(%this, %textBox)
{
	if(%textBox == %this.moduleNameBox)
	{
		if(!%this.settingModuleName)
		{
			%this.moduleNameEdited = true;
		}
	}
	else if(%textBox == %this.titleBox && !%this.moduleNameEdited)
	{
		%this.settingModuleName = true;
		%this.moduleNameBox.setText(%this.suggestModuleName());
		%this.settingModuleName = false;
	}

	%this.validate();
}

function NewProjectDialog::onReturnPressed(%this, %textBox)
{
	%this.onCreate();
}

function NewProjectDialog::onDropDownClosed(%this, %dropDown)
{
	%this.validate();
}

function NewProjectDialog::Validate(%this)
{
	%this.createButton.active = false;

	%title = %this.titleBox.getText();
	%directory = %this.dirBox.getText();
	%moduleName = %this.moduleNameBox.getText();
	%description = %this.descBox.getText();

	if(%title $= "")
	{
		%this.feedback.setText("Please enter a title for your project.");
		return false;
	}

	if(%directory $= "")
	{
		%this.feedback.setText("Please select a name for the directory that your project will launch from.");
		return false;
	}
	else
	{
		%path = makeFullPath(%directory, getMainDotCsDir());
		if(isDirectory(%path))
		{
			%fileList = getFileList(%path);
			%dirList = getDirectoryList(%path);
			if(getWordCount(%fileList) > 0 || getWordCount(%dirList) > 0)
			{
				%this.feedback.setText("The directory should be an empty directory or a non-existing directory in the main T2d directory.");
				return false;
			}
		}
	}

	if(%this.selectedGameCore() $= "")
	{
		%this.feedback.setText("Please choose the game core to build your project from. If this list is empty, the library folder has no game core template in it.");
		return false;
	}

	if(!%this.validateModuleName(%moduleName))
	{
		return false;
	}

	if(%description $= "")
	{
		%this.feedback.setText("Please add a short, meaningful description for your project.");
		return false;
	}

	%this.createButton.active = true;
	%this.feedback.setText("Press the Create button to create your new project!");
	return true;
}

// The module name becomes a folder, a ModuleId, and the namespace the engine
// calls create and destroy on, so it has to be a legal script identifier and it
// cannot collide with the modules copied in beside it.
function NewProjectDialog::validateModuleName(%this, %moduleName)
{
	if(%moduleName $= "")
	{
		%this.feedback.setText("Please enter a name for your game module.");
		return false;
	}

	for(%i = 0; %i < strlen(%moduleName); %i++)
	{
		if(!ModuleStamper.isIdentifierChar(getSubStr(%moduleName, %i, 1)))
		{
			%this.feedback.setText("The module name becomes a script namespace, so it can only contain letters, numbers and underscores.");
			return false;
		}
	}

	if(strpos("0123456789", getSubStr(%moduleName, 0, 1)) != -1)
	{
		%this.feedback.setText("The module name cannot start with a number.");
		return false;
	}

	if(%moduleName $= "AppCore" || %moduleName $= "Audio" || %moduleName $= "themes")
	{
		%this.feedback.setText("AppCore, Audio and themes are already used by every project. Please pick another module name.");
		return false;
	}

	return true;
}

function NewProjectDialog::onCreate(%this)
{
	if(%this.validate())
	{
		%title = %this.titleBox.getText();
		%directory = %this.dirBox.getText();
		%moduleName = %this.moduleNameBox.getText();
		%author = %this.authorBox.getText();
		%description = %this.descBox.getText();
		%core = %this.selectedGameCore();

		// createPath wants a separator on the end -- without one the last folder is
		// read as a filename and never made -- and everything else wants none. A
		// trailing separator left on %path ends up in the middle of every path
		// built from it, and while the engine's own file calls expand that away,
		// script side isDirectory does not: it stats the string it is given.
		%path = makeFullPath(%directory, getMainDotCsDir());
		%lastChar = getSubStr(%path, strlen(%path) - 1, 1);
		if(%lastChar $= "\\" || %lastChar $= "/")
		{
			%path = getSubStr(%path, 0, strlen(%path) - 1);
		}
		createPath(%path @ "/");

		%modulePath = pathConcat(%path, %moduleName);

		ModuleDatabase.scanModules(pathConcat(getMainDotCsDir(), "library"));
		ModuleDatabase.CopyModule("AppCore", 1, "AppCore", %path, true);
		ModuleDatabase.CopyModule("Audio", 1, "Audio", %path, true);

		// Copied under the core's own id, then renamed on the copy. Handing
		// CopyModule a different target id would make it rename module.taml to
		// <ModuleId>.module.taml, which is not the name the rest of the editor
		// opens a module definition by, and there is no fileRename in script to
		// put it back. Its taml rewriting is also root-only and cannot reach a
		// script file at all, which is where the namespace lives.
		ModuleDatabase.CopyModule(%core, 1, %core, %modulePath, false);
		ModuleDatabase.clearDatabase();

		// The stock theme and its baked font caches. Not a module: a theme belongs
		// to the project and has to survive AppCore being updated, which replaces
		// that module's whole directory. pathCopy recurses, so this brings the
		// fonts folder with it. (AppCore generates a theme if it ever finds none,
		// so a project without this still works - it just starts from a theme with
		// no baked fonts.)
		pathCopy(pathConcat(getMainDotCsDir(), "library", "themes"), pathConcat(%path, "themes"), false);

		ModuleStamper.renameInPlace(%modulePath, %core, %moduleName);

		%file = TamlRead(pathConcat(%path, "AppCore", "1", "module.taml"));
		%file.Project = %title;
		%file.ProjectDescription = %description;
		TamlWrite(%file, pathConcat(%path, "AppCore", "1", "module.taml"));
		%file.delete();

		%file = TamlRead(pathConcat(%modulePath, "module.taml"));
		%file.Group = "launch";
		%file.Type = "Game Module";
		%file.Author = %author;
		%file.Description = %description;
		ModuleStamper.clearTemplateMarkers(%file);
		TamlWrite(%file, pathConcat(%modulePath, "module.taml"));
		%file.delete();

		%this.postEvent("ProjectCreated", %directory);
		%this.onClose();
	}
}
