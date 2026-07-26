//-----------------------------------------------------------------------------
// Copyright (c) 2013 GarageGames, LLC
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
//-----------------------------------------------------------------------------

function GuiEditor::create( %this )
{
	exec("./scripts/GuiEditorBrain.cs");
	exec("./scripts/GuiEditorControlListWindow.cs");
	exec("./scripts/GuiEditorControlListBox.cs");
	exec("./scripts/GuiEditorInspectorWindow.cs");
    exec("./scripts/GuiEditorInspector.cs");
	exec("./scripts/GuiEditorExplorerWindow.cs");
    exec("./scripts/GuiEditorExplorerTree.cs");
    exec("./scripts/GuiEditorSaveGuiDialog.cs");
    exec("./scripts/GuiEditorGridSizeDialog.cs");
    exec("./scripts/GuiEditorColorWindow.cs");
    exec("./scripts/GuiEditorToolsWindow.cs");
    exec("./scripts/GuiProfileEditorDialog.cs");
    exec("./scripts/GuiProfileEditorBorderGrid.cs");
    exec("./scripts/GuiProfileEditorBorderSetter.cs");
    exec("./scripts/GuiProfileEditorBorderForm.cs");
    exec("./scripts/GuiProfileEditorFieldSpec.cs");
    exec("./scripts/GuiProfileEditorFieldRow.cs");
    exec("./scripts/GuiProfileEditorStateColorRow.cs");
    exec("./scripts/GuiProfileEditorProfileForm.cs");
    exec("./scripts/ProfileThemeEditForm.cs");
    exec("./scripts/GuiProfileEditorLibrary.cs");
    exec("./scripts/GuiProfileEditorTree.cs");
    exec("./scripts/GuiProfileEditorPreview.cs");
    exec("./scripts/GuiProfileEditorNameDialog.cs");
    exec("./scripts/GuiProfileEditorConfirmDialog.cs");
    exec("./scripts/GuiEditorThemeApplier.cs");
    exec("./scripts/GuiEditorThemeDialog.cs");

	%this.guiPage = EditorCore.RegisterEditor("Gui Editor", %this);

    // The theme library and the applier are both wanted before the Profile
    // Editor is ever opened - the Set Theme button and every newly dropped
    // control go through them - so they are built with the editor rather than on
    // demand. The library also outlives each Profile Editor session so theme
    // member profiles stay alive for the Guis wearing them.
    %this.themeLibrary = new ScriptObject()
    {
        class = "GuiProfileEditorLibrary";
        owner = %this;
    };

    // rootContainer is filled in below, once the simulated canvas exists: the
    // applier compares against it to tell a Gui's root controls (which take the
    // Panel profile) from everything nested inside them.
    %this.themeApplier = new ScriptObject()
    {
        class = "GuiEditorThemeApplier";
        library = %this.themeLibrary;
    };

    %this.content = %this.createFrameSet();

    %this.brain = new GuiEditCtrl()
    {
        Class = "GuiEditorBrain";
		HorizSizing = "width";
        VertSizing = "height";
        Position = "0 0";
        Extent = "100 100";
    };
    ThemeManager.setProfile(%this.brain, "guiEditorProfile");

    // The frameset docks children into empty frames in add-order (depth-first
    // through the splits), so the Gui Tools window must be added before the
    // inspector window to land in the frame above it.
    %this.guiToolsWindow = new GuiWindowCtrl()
    {
        Class = "GuiEditorToolsWindow";
        HorizSizing = "right";
        VertSizing = "bottom";
        Position = "0 0";
        Extent = "360 92";
        MinExtent = "100 64";
        text = "Gui Tools";
        canMove = true;
        canClose = false;
        canMinimize = true;
        canMaximize = false;
        resizeWidth = true;
        resizeHeight = true;
    };
    ThemeManager.setProfile(%this.guiToolsWindow, "windowProfile");
    ThemeManager.setProfile(%this.guiToolsWindow, "windowContentProfile", "ContentProfile");
    ThemeManager.setProfile(%this.guiToolsWindow, "windowButtonProfile", "CloseButtonProfile");
    ThemeManager.setProfile(%this.guiToolsWindow, "windowButtonProfile", "MinButtonProfile");
    ThemeManager.setProfile(%this.guiToolsWindow, "windowButtonProfile", "MaxButtonProfile");
    %this.content.add(%this.guiToolsWindow);

    %this.inspectorWindow = new GuiWindowCtrl()
    {
        Class = "GuiEditorInspectorWindow";
        HorizSizing = "right";
        VertSizing = "bottom";
        Position = "0 0";
        Extent = "360 380";
        MinExtent = "100 100";
        text = "Gui Inspector";
        canMove = true;
        canClose = false;
        canMinimize = true;
        canMaximize = false;
        resizeWidth = true;
        resizeHeight = true;
    };
    ThemeManager.setProfile(%this.inspectorWindow, "windowProfile");
    ThemeManager.setProfile(%this.inspectorWindow, "windowContentProfile", "ContentProfile");
    ThemeManager.setProfile(%this.inspectorWindow, "windowButtonProfile", "CloseButtonProfile");
    ThemeManager.setProfile(%this.inspectorWindow, "windowButtonProfile", "MinButtonProfile");
    ThemeManager.setProfile(%this.inspectorWindow, "windowButtonProfile", "MaxButtonProfile");
    %this.content.add(%this.inspectorWindow);
    %this.inspectorWindow.startListening(%this.brain);

    %this.background = new GuiSpriteCtrl() {
		HorizSizing = "right";
        VertSizing = "bottom";
        Position = "0 0";
        Extent = "100 100";
		imageColor = "255 255 255 255";
        Image = "EditorCore:editorGrid";
		singleFrameBitmap = "1";
		tileImage = "1";
		positionOffset = "0 0";
		imageSize = "128 128";
		fullSize = "0";
		constrainProportions = "1";
	};
    ThemeManager.setProfile(%this.background, "emptyProfile");
    %this.content.add(%this.background);

    %this.ctrlListWindow = new GuiWindowCtrl()
    {
        Class = "GuiEditorControlListWindow";
        HorizSizing = "right";
        VertSizing = "bottom";
        Position = "360 0";
        Extent = "250 380";
        MinExtent = "100 100";
        text = "Control List";
        canMove = true;
        canClose = false;
        canMinimize = true;
        canMaximize = false;
        resizeWidth = true;
        resizeHeight = true;
    };
    ThemeManager.setProfile(%this.ctrlListWindow, "windowProfile");
    ThemeManager.setProfile(%this.ctrlListWindow, "windowContentProfile", "ContentProfile");
    ThemeManager.setProfile(%this.ctrlListWindow, "windowButtonProfile", "CloseButtonProfile");
    ThemeManager.setProfile(%this.ctrlListWindow, "windowButtonProfile", "MinButtonProfile");
    ThemeManager.setProfile(%this.ctrlListWindow, "windowButtonProfile", "MaxButtonProfile");
    %this.content.add(%this.ctrlListWindow);

    %this.explorerWindow = new GuiWindowCtrl()
    {
        Class = "GuiEditorExplorerWindow";
        HorizSizing = "right";
        VertSizing = "bottom";
        Position = "610 0";
        Extent = "400  380";
        MinExtent = "100 100";
        text = "Explorer";
        canMove = true;
        canClose = false;
        canMinimize = true;
        canMaximize = false;
        resizeWidth = true;
        resizeHeight = true;
    };
    ThemeManager.setProfile(%this.explorerWindow, "windowProfile");
    ThemeManager.setProfile(%this.explorerWindow, "windowContentProfile", "ContentProfile");
    ThemeManager.setProfile(%this.explorerWindow, "windowButtonProfile", "CloseButtonProfile");
    ThemeManager.setProfile(%this.explorerWindow, "windowButtonProfile", "MinButtonProfile");
    ThemeManager.setProfile(%this.explorerWindow, "windowButtonProfile", "MaxButtonProfile");
    %this.content.add(%this.explorerWindow);
    %this.explorerWindow.startListening(%this.brain);

    %this.rootGui = new GuiControl()
    {
        HorizSizing = "width";
        VertSizing = "height";
        Position = "0 0";
        Extent = %this.background.getExtent();
        Profile = GuiDefaultProfile;
        class = "SimulatedCanvas";
    };
    %this.background.add(%this.rootGui);
    %this.themeApplier.rootContainer = %this.rootGui;
    %this.brain.extent = %this.background.getExtent();
    %this.background.add(%this.brain);
    %this.fileName = "";
    %this.filePath = "";
    %this.formatIndex = 0;
    %this.folder = "";
    %this.module = "";
    %this.brain.setRoot(%this.rootGui);
    %this.brain.root = %this.rootGui;
    %this.explorerWindow.inspect(%this.rootGui);

    /* %this.colorWindow = new GuiWindowCtrl()
    {
        Class = "GuiEditorColorWindow";
        HorizSizing = "right";
        VertSizing = "bottom";
        Position = "610 0";
        Extent = "400  380";
        MinExtent = "100 100";
        text = "Color Test";
        canMove = true;
        canClose = false;
        canMinimize = true;
        canMaximize = false;
        resizeWidth = true;
        resizeHeight = true;
    };
    ThemeManager.setProfile(%this.colorWindow, "windowProfile");
    ThemeManager.setProfile(%this.colorWindow, "windowContentProfile", "ContentProfile");
    ThemeManager.setProfile(%this.colorWindow, "windowButtonProfile", "CloseButtonProfile");
    ThemeManager.setProfile(%this.colorWindow, "windowButtonProfile", "MinButtonProfile");
    ThemeManager.setProfile(%this.colorWindow, "windowButtonProfile", "MaxButtonProfile");
    %this.guiPage.add(%this.colorWindow); */

    EditorCore.FinishRegistration(%this.guiPage);
}



function GuiEditor::createFrameSet(%this)
{
	%content = new GuiFrameSetCtrl() {
		HorizSizing = "width";
        VertSizing = "height";
        Position = "0 0";
        Extent = %this.guiPage.getExtent();
        DividerThickness = 6;
	};
    ThemeManager.setProfile(%content, "frameSetProfile");
    ThemeManager.setProfile(%content, "dropButtonProfile", "dropButtonProfile");
    ThemeManager.setProfile(%content, "frameSetTabBookProfile", "tabBookProfile");
    ThemeManager.setProfile(%content, "frameSetTabProfile", "tabProfile");
    ThemeManager.setProfile(%content, "frameSetTabPageProfile", "tabPageProfile");
    %this.guiPage.add(%content);

    %idList = %content.createHorizontalSplit(1);
    %leftID = getWord(%idList, 0);
    %rightID = getWord(%idList, 1);
    %content.anchorFrame(%rightID);
    %content.setFrameSize(%rightID, 300);
    
    %ids = %content.createHorizontalSplit(%leftID);
    %inspectorFrameID = getWord(%ids, 0);
    %centerFrameID = getWord(%ids, 1);
    %content.setFrameSize(%inspectorFrameID, 360);

    // Split the inspector column so the Gui Tools window docks above the
    // Gui Inspector. The top child of a vertical split is the anchored frame.
    %ids = %content.createVerticalSplit(%inspectorFrameID);
    %guiToolsFrameID = getWord(%ids, 0);
    %content.setFrameSize(%guiToolsFrameID, 92);

    %ids = %content.createVerticalSplit(%rightID);
    %toolFrameID = getWord(%ids, 0);
    %explorerFrameID = getWord(%ids, 1);
    %content.setFrameSize(%toolFrameID, 380);

    return %content;
}

//-----------------------------------------------------------------------------

function GuiEditor::destroy( %this )
{
	// Order matters. The Profile Editor's live preview wears theme member
	// profiles owned by the theme library, and the library deliberately outlives
	// the dialog (see openProfileEditor). Freeing it while the dialog is still up
	// leaves those preview controls holding freed profiles, and the dangling
	// mProfile is not touched until the canvas itself is torn down - inside
	// Sim::shutdown, long after this runs - so it surfaces as an access violation
	// at exit with no obvious cause. Close the dialog first.
	%this.closeProfileEditor();

	if(isObject(%this.themeApplier))
	{
		%this.themeApplier.delete();
	}

	if(isObject(%this.themeLibrary))
	{
		%this.themeLibrary.delete();
	}
}

function GuiEditor::open(%this, %content)
{
    // First time in: pick up the project's theme. Not done at create time -
    // the editor registers before a project's AppCore has loaded its themes.
    if(%this.themeName $= "")
    {
        %this.adoptTheme("");
    }

    EditorCore.menuBar.setMenuActive("File", true);
    //EditorCore.menuBar.setMenuActive("Edit", true); //These features still need development
    EditorCore.menuBar.setMenuActive("Layout", true);
    EditorCore.menuBar.setMenuActive("Select", true);
    editorMode(true);
}

function GuiEditor::close(%this)
{
    editorMode(false);
    EditorCore.menuBar.setMenuActive("File", false);
    EditorCore.menuBar.setMenuActive("Edit", false);
    EditorCore.menuBar.setMenuActive("Layout", false);
    EditorCore.menuBar.setMenuActive("Select", false);
}

//MENU FUNCTIONS---------------------------------------------------------------
function GuiEditor::NewGui(%this)
{
    %this.rootGui.clear();
    %this.fileName = "";
    %this.filePath = "";
    %this.formatIndex = 0;
    %this.folder = "";
    %this.module = "";
    %this.brain.clearSelection();
    %this.explorerWindow.tree.refresh();

    // A new Gui joins the theme this session is working in, so the first control
    // dropped into it is already themed.
    %theme = %this.defaultTheme();
    %this.themeName = isObject(%theme) ? %theme.getName() : "";
}

function GuiEditor::OpenGui(%this)
{
    %path = pathConcat(getMainDotCsDir(), ProjectManager.getProjectFolder());
	%dialog = new OpenFileDialog()
	{
		Filters = "ALL (*.GUI;*.GUI.DSO;*.GUI.TAML)|*.GUI;*.GUI.DSO;*.GUI.TAML|GUI (*.GUI;*.GUI.DSO)|*.GUI;*.GUI.DSO|TAML (*.GUI.TAML)|*.GUI.TAML";
		ChangePath = false;
		MultipleFiles = false;
		DefaultFile = "";
		defaultPath = %path;
		title = "Open Gui File";
	};
	%result = %dialog.execute();

	if ( %result )
	{
        if(fileExt(%dialog.fileName) $= ".taml")
        {
            %guiContent = TAMLRead(%dialog.fileName);
            %includesSimulatedCanvas = (%guiContent.class $= "SimulatedCanvas");
        }
        else 
        {
            exec(%dialog.fileName);
        }
        if(%includesSimulatedCanvas $= "")
        {
            %includesSimulatedCanvas = true;
        }
        if(isObject(%guiContent))
        {
            %this.fileName = fileName(%dialog.fileName);
            %this.filePath = %dialog.fileName;
            %this.formatIndex = 0;
            if(getSubStr(%dialog.fileName, strlen(%dialog.fileName) - 5, 5) $= ".taml")
            {
                %this.formatIndex = 1;
            }
            %this.folder = makeRelativePath(filePath(%dialog.fileName), getMainDotCsDir());
            %this.module = EditorCore.findModuleOfPath(%dialog.fileName);
            %this.DisplayGuiContent(%guiContent, %includesSimulatedCanvas);
        }
        else 
        {
            EditorCore.alert("Something went wrong while opening the Gui File. Gui Files should be structures with the root object assigned to %guiContent. If this file was made outside of the editor, you can change it manually and then open it in the Gui Editor.");
        }
    }
	// Cleanup
	%dialog.delete();
}

function GuiEditor::DisplayGuiContent(%this, %content, %includesSimulatedCanvas)
{
    %this.rootGui.deleteObjects();
    %this.brain.clearSelection();

    // Read off the root before it is unpacked - in the simulated-canvas case the
    // object carrying the field is deleted a few lines down.
    %recordedTheme = %content.guiTheme;

    if(%includesSimulatedCanvas)
    {
        %count = %content.getCount();
        for(%i = 0; %i < %count; %i++)
        {
            %obj[%i] = %content.getObject(%i);
        }
        for(%i = 0; %i < %count; %i++)
        {
            %this.rootGui.add(%obj[%i]);
        }
        %content.delete();
        %this.explorerWindow.tree.refresh();
        %this.brain.onSelect(%this.rootGui.getObject(0));
    }
    else
    {
        %this.rootGui.add(%content);
        %this.explorerWindow.tree.refresh();
        %this.brain.onSelect(%content);
    }

    %this.adoptTheme(%recordedTheme);
}

function GuiEditor::SaveGui(%this)
{
    if(%this.fileName $= "")
    {
        %this.SaveGuiAs();
    }
    else 
    {
        %this.SaveCore(%this.filePath, %this.formatIndex, %this.folder, %this.module);
    }
}

function GuiEditor::SaveGuiAs(%this)
{
    %width = 700;
	%height = 390;
	%dialog = new GuiControl()
	{
		class = "GuiEditorSaveGuiDialog";
		superclass = "EditorDialog";
		dialogSize = (%width + 8) SPC (%height + 8);
		dialogCanClose = true;
		dialogText = "Save Gui";
        defaultFileName = %this.fileName;
        formatIndex = %this.formatIndex;
        defaultFolder = %this.folder;
        defaultModule = %this.module;
	};
	%dialog.init(%width, %height);

	Canvas.pushDialog(%dialog);
}

function GuiEditor::getThemeLibrary(%this)
{
	%this.themeLibrary.scanThemes();
	return %this.themeLibrary;
}

function GuiEditor::openProfileEditor(%this)
{
	%canvasSize = Canvas.getExtent();
	%width = getWord(%canvasSize, 0) - 80;
	%height = getWord(%canvasSize, 1) - 80;

	%dialog = new GuiControl()
	{
		class = "GuiProfileEditorDialog";
		superclass = "EditorDialog";
		dialogSize = (%width + 8) SPC (%height + 8);
		dialogCanClose = true;
		dialogText = "Gui Profile Editor";
		library = %this.themeLibrary;
	};
	%dialog.init(%width, %height);
	%this.profileEditorDialog = %dialog;

	Canvas.pushDialog(%dialog);
}

//THEMES-----------------------------------------------------------------------
//
// A Gui belongs to a theme. Setting one re-profiles every control in the
// document by category, a newly dropped control joins it on arrival, and the
// theme's name is saved with the Gui so reopening it lands back where it was.
// The intent is that profiles are something a developer chooses once, in the
// Profile Editor, and rarely thinks about again.
//-----------------------------------------------------------------------------

function GuiEditor::openThemeDialog(%this)
{
	%width = 420;
	%height = 200;
	%dialog = new GuiControl()
	{
		class = "GuiEditorThemeDialog";
		superclass = "EditorDialog";
		dialogSize = (%width + 8) SPC (%height + 8);
		dialogCanClose = true;
		dialogText = "Set Gui Theme";
	};
	%dialog.init(%width, %height);

	Canvas.pushDialog(%dialog);
}

// Put %theme on the whole document. The simulated canvas is skipped: it is the
// editor's stage, not part of the Gui being authored.
function GuiEditor::setTheme(%this, %theme, %overrideStandalone)
{
	if(!isObject(%theme))
	{
		return;
	}

	%this.themeName = %theme.getName();
	%this.lastThemeName = %this.themeName;

	%changed = %this.themeApplier.applyToChildren(%this.rootGui, %theme, %overrideStandalone);
	%this.explorerWindow.tree.refresh();

	echo("Gui Editor: " @ %this.themeName @ " applied to " @ %changed @ " profile slot(s).");
}

// The theme a new Gui starts on: the last one used this session, falling back to
// whatever the project has. There is no preferences file to remember it across
// runs, and none is needed - an existing Gui carries its own theme.
function GuiEditor::defaultTheme(%this)
{
	%themes = %this.getThemeLibrary().getThemes();

	if(%this.lastThemeName !$= "")
	{
		for(%i = 0; %i < getWordCount(%themes); %i++)
		{
			%theme = getWord(%themes, %i);
			if(%theme.getName() $= %this.lastThemeName)
			{
				return %theme;
			}
		}
	}

	return (getWordCount(%themes) > 0) ? getWord(%themes, 0) : 0;
}

function GuiEditor::themeByName(%this, %name)
{
	if(%name $= "")
	{
		return 0;
	}

	%themes = %this.getThemeLibrary().getThemes();
	for(%i = 0; %i < getWordCount(%themes); %i++)
	{
		%theme = getWord(%themes, %i);
		if(%theme.getName() $= %name)
		{
			return %theme;
		}
	}

	return 0;
}

// Work out which theme a freshly opened Gui is on. The name recorded when it was
// saved wins; a Gui written before that field existed, or authored by hand, is
// judged by the profiles its controls wear; failing both, it joins the theme the
// session is already working in.
function GuiEditor::adoptTheme(%this, %recordedName)
{
	%theme = %this.themeByName(%recordedName);

	if(!isObject(%theme))
	{
		%theme = %this.themeApplier.inferTheme(%this.rootGui);
	}

	if(!isObject(%theme))
	{
		%theme = %this.defaultTheme();
	}

	%this.themeName = isObject(%theme) ? %theme.getName() : "";
	if(%this.themeName !$= "")
	{
		%this.lastThemeName = %this.themeName;
	}
}

// Called by the theme library before it frees a theme or profile the document
// might be wearing. A control's profile field is a raw pointer, so it has to be
// moved off the doomed profile before the delete rather than after.
function GuiEditor::detachTheme(%this, %theme, %profile)
{
	%this.themeApplier.detach(%this.rootGui, %theme, %profile);
}

// The other half: after a revert has re-read the theme files, the document's
// theme is a new object with the same name, so put it back on.
function GuiEditor::reattachTheme(%this)
{
	%theme = %this.themeByName(%this.themeName);
	if(isObject(%theme))
	{
		%this.themeApplier.applyToChildren(%this.rootGui, %theme, false);
		%this.explorerWindow.tree.refresh();
	}
}

// Tear the Profile Editor dialog down synchronously (not via the usual deferred
// close). Called at shutdown before the editor and AppCore modules unload, so
// the dialog's live preview and controls stop referencing theme and editor
// profiles before those profiles are freed - otherwise the controls' onSleep
// runs decRefCount on freed profiles during final teardown and crashes.
function GuiEditor::closeProfileEditor(%this)
{
	if(isObject(%this.profileEditorDialog))
	{
		if(isObject(Canvas))
		{
			Canvas.popDialog(%this.profileEditorDialog);
		}
		%this.profileEditorDialog.delete();
		%this.profileEditorDialog = "";
	}
}

function GuiEditor::SaveCore(%this, %filePath, %formatIndex, %folder, %module)
{
    // Record the theme on whichever object is about to be written, so reopening
    // the Gui does not have to guess. It means nothing to the game.
    //
    // canSaveDynamicFields has to be turned on for it to survive the trip: every
    // GuiControl clears that flag in its constructor (guiControl.cc), so dynamic
    // fields on controls are dropped by both writers by default.
    %root = (%this.rootGui.getCount() == 1) ? %this.rootGui.getObject(0) : %this.rootGui;
    %root.canSaveDynamicFields = true;
    %root.guiTheme = %this.themeName;

    if(%formatIndex == 0)
    {
        %fo = new FileObject();
        %fo.openForWrite(%filePath);
        %fo.writeLine("//--- Created with the GuiEditor ---//");
        if(%this.rootGui.getCount() == 1)
        {
            //Saved without the simulated canvas
            %fo.writeLine("%includesSimulatedCanvas = false;");
            %fo.writeObject(%this.rootGui.getObject(0), "%guiContent = ");
        }
        else 
        {
            //We have multiple top level objects so include the containing simulated canvas
            %fo.writeLine("%includesSimulatedCanvas = true;");
            %fo.writeObject(%this.rootGui, "%guiContent = ");
        }
        %fo.writeLine("//--- GuiEditor End ---//");
        %fo.close();
        %fo.delete();
    }
    else 
    {
        if(GuiEditor.rootGui.getCount() == 1)
        {
            //Saved without the Simulated Canvas
            TAMLWrite(%this.rootGui.getObject(0), %filePath);
        }
        else 
        {
            TAMLWrite(%this.rootGui, %filePath);
        }
    }
    %this.fileName = fileName(%filePath);
    %this.filePath = %filePath;
    %this.formatIndex = %formatIndex;
    %this.folder = %folder;
    %this.module = %module;
}

function GuiEditor::Undo(%this)
{
    %undoManager = %this.brain.getUndoManager();
    %undoManager.undo();
}

function GuiEditor::Redo(%this)
{
    %undoManager = %this.brain.getUndoManager();
    %undoManager.redo();

    %count = %undoManager.getRedoCount();

}

function GuiEditor::Cut(%this)
{
    
}

function GuiEditor::Copy(%this)
{
    
}

function GuiEditor::Paste(%this)
{
    
}

function GuiEditor::changeExtent(%this, %x, %y)
{
    %set = %this.brain.getSelected();
    if(%set.getCount() >= 1)
    {
        %obj = %set.getObject(0);
        %ext = %obj.getExtent();
        %obj.setExtent(getWord(%ext, 0) + %x, getWord(%ext, 1) + %y);
    }
}

function GuiEditor::SetGridSize(%this)
{
    %width = 300;
	%height = 140;
	%dialog = new GuiControl()
	{
		class = "GuiEditorGridSizeDialog";
		superclass = "EditorDialog";
		dialogSize = (%width + 8) SPC (%height + 8);
		dialogCanClose = true;
		dialogText = "Grid Size";
	};
	%dialog.init(%width, %height);

	Canvas.pushDialog(%dialog);
}

function GuiEditor::SnapToGrid(%this, %gridOn)
{
    if(%gridOn)
    {
        %this.brain.setSnapToGrid(10);
    }
    else 
    {
        %this.brain.setSnapToGrid(0);
    }
}

//METHODS-----------------------------------------------------------------
