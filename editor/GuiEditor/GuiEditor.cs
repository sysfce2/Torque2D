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
	exec("./scripts/GuiEditorControlIcons.cs");
	exec("./scripts/GuiEditorControlListWindow.cs");
	exec("./scripts/GuiEditorControlGroup.cs");
	exec("./scripts/GuiEditorControlTile.cs");
	exec("./scripts/GuiEditorInspectorWindow.cs");
	exec("./scripts/GuiEditorExplorerWindow.cs");
    exec("./scripts/GuiEditorExplorerTree.cs");
    exec("./scripts/GuiEditorSaveGuiDialog.cs");
    exec("./scripts/GuiEditorGridSizeDialog.cs");
    exec("./scripts/GuiEditorToolsWindow.cs");
    exec("./scripts/GuiProfileEditorDialog.cs");
    exec("./scripts/GuiProfileEditorColorPopup.cs");
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

    // The properties pane that replaced the native GuiInspector.
    exec("./scripts/GuiEditorControlSpec.cs");
    exec("./scripts/GuiEditorToggleIcon.cs");
    exec("./scripts/GuiEditorChoiceRow.cs");
    exec("./scripts/GuiEditorAnchorPicker.cs");
    exec("./scripts/GuiEditorTextBlock.cs");
    exec("./scripts/GuiEditorMenuItemBlock.cs");
    exec("./scripts/GuiEditorHeaderBlock.cs");
    exec("./scripts/GuiEditorDynamicFields.cs");
    exec("./scripts/GuiEditorItemRow.cs");
    exec("./scripts/GuiEditorItemsBlock.cs");
    exec("./scripts/GuiEditorInspectorPane.cs");

    // Undo. The engine has owned the machinery all along - GuiEditCtrl holds an
    // UndoManager and a trash group it never empties - and nothing had ever
    // built it an action.
    exec("./scripts/GuiEditorUndoAction.cs");
    exec("./scripts/GuiEditorUndoRecorder.cs");

    // Copy, cut and paste, which is undo's machinery plus a deep clone.
    exec("./scripts/GuiEditorClipboard.cs");

	%this.guiPage = EditorCore.RegisterEditor("Gui Editor", %this);

    // What the control palette can offer and what each entry looks like. Built
    // before the palette window, which reads it as it populates. Generated from
    // the icon sheets, so the table and the art cannot disagree.
    %this.controlIcons = new ScriptObject()
    {
        class = "GuiEditorControlIcons";
    };

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

    // Every change to the Gui being authored goes through the recorder. It asks
    // the brain for the UndoManager when it needs one, so it can be built before
    // the brain is.
    %this.undoRecorder = new ScriptObject()
    {
        class = "GuiEditorUndoRecorder";
        owner = %this;
    };

    // Holds copied controls for as long as the editor is open, which is longer
    // than any one document: a copy taken from one Gui can be pasted into the
    // next one opened.
    %this.clipboard = new ScriptObject()
    {
        class = "GuiEditorClipboard";
        owner = %this;
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

    // 340, not 300: this column holds the control palette, whose grid view fits
    // as many 100-pixel tiles per row as the width allows. At 300, once the
    // scroll bar is taken out, that is two -- and the leftover is shared between
    // them, so the tiles sit in gappy columns. 340 makes it three.
    %content.setFrameSize(%rightID, 340);
    
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

	if(isObject(%this.controlIcons))
	{
		%this.controlIcons.delete();
	}

	// Empty the stacks while the brain (and so the UndoManager it owns) is still
	// here, rather than leaving the actions to the manager's destructor during
	// canvas teardown.
	if(isObject(%this.undoRecorder))
	{
		%this.undoRecorder.clear();
		%this.undoRecorder.delete();
	}

	// The copies it holds are real controls wearing real profiles, so they go the
	// same way and for the same reason: before the profiles do.
	if(isObject(%this.clipboard))
	{
		%this.clipboard.delete();
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
    EditorCore.menuBar.setMenuActive("Edit", true);
    EditorCore.menuBar.setMenuActive("Layout", true);
    EditorCore.menuBar.setMenuActive("Select", true);

    // Undo and Redo are greyed from the stacks, Cut and Copy from the selection,
    // and Paste from whether anything has been copied. All three of the last are
    // cached against what the menu was last told, so they are forced here: the
    // menu looks new every time the editor is opened.
    %this.undoRecorder.forceRefreshMenu();
    %this.clipboard.forceRefreshMenu();
    %this.brain.toggleMenuItems();

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

    // Every record on the stack names controls that have just been freed.
    %this.undoRecorder.clear();

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

    // The document the stack was recorded against has just been deleted.
    %this.undoRecorder.clear();

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

	// One undo step for the whole sweep, however many profile slots it fills.
	%this.undoRecorder.begin("Set Theme", "");
	%changed = %this.themeApplier.applyToChildren(%this.rootGui, %theme, %overrideStandalone);
	%this.undoRecorder.end();

	%this.explorerWindow.tree.refresh();

	// The properties pane caches which profiles it offers, so it has to be told
	// as well -- otherwise it goes on showing the profile the selected control
	// wore before the sweep, from a theme that is no longer the Gui's.
	%this.inspectorWindow.onRethemed(%this.inspectorWindow.pane.target);

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
	// The stack is full of profile ids that are about to stop resolving, and a
	// detach is not itself something to undo - the profile it moved off will not
	// exist to move back to.
	%this.undoRecorder.clear();

	// And the clipboard holds controls whose profile fields are raw pointers to
	// the same doomed profiles. Nothing reads them while the copy sits in the
	// stash, but a paste would - and by then the profile is gone.
	%this.clipboard.clear();

	%this.themeApplier.detach(%this.rootGui, %theme, %profile);
}

// The other half: after a revert has re-read the theme files, the document's
// theme is a new object with the same name, so put it back on.
function GuiEditor::reattachTheme(%this)
{
	%theme = %this.themeByName(%this.themeName);
	if(isObject(%theme))
	{
		// Repairing the document after a revert is not an edit the user made, so
		// it is not one they can take back. Suspended rather than cleared: the
		// detach that preceded this already emptied the stack.
		%this.undoRecorder.suspend();
		%this.themeApplier.applyToChildren(%this.rootGui, %theme, false);
		%this.undoRecorder.resume();

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

//-----------------------------------------------------------------------------
// What the legacy .gui script format cannot carry.
//
// FileObject::writeObject walks a control's fields and its child objects, and
// that is the whole of it. Two things in the engine are neither: a list box or
// drop down's static rows, and a frame set's layout. Both are written as TAML
// custom nodes, so saving as .gui drops them silently - which the frame set has
// done since it was written, and which is worth saying out loud now that
// something people use every day is in the same boat.
//
// Returns a sentence naming what would go, or "" when there is nothing to say.
//-----------------------------------------------------------------------------

function GuiEditor::tamlOnlyStateSummary(%this)
{
    %this.tamlOnlyRows = 0;
    %this.tamlOnlyLists = 0;
    %this.tamlOnlyFrameSets = 0;
    %this.countTamlOnlyState(%this.rootGui);

    if(%this.tamlOnlyRows == 0 && %this.tamlOnlyFrameSets == 0)
    {
        return "";
    }

    // Only what the document actually holds gets named, in the heading and in
    // the tally both: telling someone their frame layouts are at risk when there
    // is not a frame set in the Gui is how a warning gets ignored.
    %kinds = "";
    %parts = "";

    if(%this.tamlOnlyRows > 0)
    {
        %kinds = "list rows";
        %parts = %this.tamlOnlyRows SPC
            ((%this.tamlOnlyRows == 1) ? "row" : "rows") SPC "on" SPC
            %this.tamlOnlyLists SPC ((%this.tamlOnlyLists == 1) ? "list" : "lists");
    }

    if(%this.tamlOnlyFrameSets > 0)
    {
        %kinds = (%kinds $= "") ? "frame layouts" : (%kinds @ " or frame layouts");

        %frames = %this.tamlOnlyFrameSets SPC
            ((%this.tamlOnlyFrameSets == 1) ? "frame layout" : "frame layouts");
        %parts = (%parts $= "") ? %frames : (%parts @ " and " @ %frames);
    }

    return "This format cannot save" SPC %kinds @ ":" SPC %parts SPC
        "would be lost. Save as TAML to keep them.";
}

function GuiEditor::countTamlOnlyState(%this, %ctrl)
{
    if(!isObject(%ctrl))
    {
        return;
    }

    // A tree's rows are generated from a root object and are never written, so
    // it is not a list for this purpose however much it derives from one.
    if((%ctrl.isMemberOfClass("GuiListBoxCtrl") || %ctrl.isMemberOfClass("GuiDropDownCtrl")) &&
        !%ctrl.isMemberOfClass("GuiTreeViewCtrl"))
    {
        %rows = %ctrl.getItemCount();
        if(%rows > 0)
        {
            %this.tamlOnlyRows += %rows;
            %this.tamlOnlyLists++;
        }
    }

    // An unsplit frame set has a layout of one frame holding one control, which
    // is what it would be rebuilt as anyway. Eight numbers is one frame.
    if(%ctrl.isMemberOfClass("GuiFrameSetCtrl") && getWordCount(%ctrl.getFrameLayout()) > 8)
    {
        %this.tamlOnlyFrameSets++;
    }

    for(%i = 0; %i < %ctrl.getCount(); %i++)
    {
        %this.countTamlOnlyState(%ctrl.getObject(%i));
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
        // The save dialog says this in its feedback line, but a re-save never
        // opens one: Ctrl+S goes straight here with the format the Gui was
        // last written in.
        %warning = %this.tamlOnlyStateSummary();
        if(%warning !$= "")
        {
            warn("Gui Editor: " @ %warning);
        }

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

//UNDO-------------------------------------------------------------------------
//
// The stack lives on the UndoManager the brain (a C++ GuiEditCtrl) has always
// owned; GuiEditorUndoRecorder is what fills it. Undoing writes to the same
// controls the editor writes to, so the recorder is suspended for the duration
// or the replay would record itself.
//-----------------------------------------------------------------------------

function GuiEditor::Undo(%this)
{
    %undoManager = %this.brain.getUndoManager();
    if(%undoManager.getUndoCount() == 0)
    {
        return;
    }

    %this.undoRecorder.suspend();
    %undoManager.undo();
    %this.undoRecorder.resume();

    %this.afterReplay();
}

function GuiEditor::Redo(%this)
{
    %undoManager = %this.brain.getUndoManager();
    if(%undoManager.getRedoCount() == 0)
    {
        return;
    }

    %this.undoRecorder.suspend();
    %undoManager.redo();
    %this.undoRecorder.resume();

    %this.afterReplay();
}

// What the rest of the editor has to be told after a replay. The action reports
// which controls it touched on its way through, so the selection can land on
// what just changed - a Ctrl+Z that moves a control scrolled off the top of the
// canvas would otherwise look like nothing happened.
function GuiEditor::afterReplay(%this)
{
    %this.explorerWindow.tree.refresh();
    %this.selectAfterReplay(%this.undoRecorder.replayTouched);
    %this.undoRecorder.refreshMenu();
}

function GuiEditor::selectAfterReplay(%this, %list)
{
    %wanted = "";

    for(%i = 0; %i < getWordCount(%list); %i++)
    {
        %ctrl = getWord(%list, %i);

        // Undoing an add puts the control in the trash, and redoing a delete
        // puts it back there. Either way it is no longer part of the Gui, so
        // there is nothing to select.
        if(isObject(%ctrl) && %this.inDocument(%ctrl))
        {
            %wanted = (%wanted $= "") ? %ctrl : (%wanted SPC %ctrl);
        }
    }

    %this.brain.restoreSelection(%wanted);
}

function GuiEditor::inDocument(%this, %ctrl)
{
    %parent = %ctrl.getParent();
    while(isObject(%parent))
    {
        if(%parent == %this.rootGui)
        {
            return true;
        }
        %parent = %parent.getParent();
    }

    return false;
}

//CLIPBOARD--------------------------------------------------------------------
//
// The copies live on GuiEditorClipboard; these three are the Edit menu's way in.
// Ctrl+X/C/V reach them as menu accelerators, which the canvas only consults
// once the first responder has passed on the key (guiCanvas.cc) - so a text box
// in the properties pane keeps Ctrl+C for its own text, and the canvas gets it
// only when nothing else wanted it.
//-----------------------------------------------------------------------------

function GuiEditor::Copy(%this)
{
    %this.clipboard.copy(%this.brain.getSelected());
}

// Copy, then the delete the Delete key already does: the C++ moves the selection
// into the trash and announces it, which the recorder turns into one undo step
// (GuiEditorBrain::onTrashSelection). Nothing is deleted for real, so a cut is
// undoable and the controls it took are still alive in the trash - which is also
// why a cut and paste keeps the names it had: a trashed control is not in the
// document, so nothing there holds its name.
function GuiEditor::Cut(%this)
{
    if(!%this.clipboard.copy(%this.brain.getSelected()))
    {
        return;
    }

    %this.brain.deleteSelection();
    %this.brain.onDelete();
}

function GuiEditor::Paste(%this)
{
    %this.clipboard.paste();
}

//LAYOUT-----------------------------------------------------------------------
//
// The Layout menu's commands go through here rather than straight to the brain,
// because the brain's C++ says nothing when it aligns or restacks a selection -
// unlike a drag or a nudge, which it brackets with callbacks. Recording either
// side of the call is cheaper than teaching the engine to announce them.
//-----------------------------------------------------------------------------

function GuiEditor::changeExtent(%this, %x, %y)
{
    %set = %this.brain.getSelected();
    if(%set.getCount() >= 1)
    {
        %this.undoRecorder.snapshot(%set);

        %obj = %set.getObject(0);
        %ext = %obj.getExtent();
        %obj.setExtent(getWord(%ext, 0) + %x, getWord(%ext, 1) + %y);

        // Same kind as a nudge, and for the same reason: holding the key down is
        // one resize, not one per repeat.
        %this.undoRecorder.commitGeometry("Resize Control", "resize");
    }
}

function GuiEditor::Justify(%this, %mode)
{
    %this.undoRecorder.snapshot(%this.brain.getSelected());
    %this.brain.Justify(%mode);
    %this.undoRecorder.commitGeometry("Align Controls", "");
}

function GuiEditor::BringToFront(%this)
{
    %this.restack("BringToFront", "Bring to Front");
}

function GuiEditor::PushToBack(%this)
{
    %this.restack("PushToBack", "Push to Back");
}

// Both do the same thing to the same one control - the C++ ignores anything but
// a single selection - and both change only its index among its siblings.
function GuiEditor::restack(%this, %method, %name)
{
    %set = %this.brain.getSelected();
    if(%set.getCount() != 1)
    {
        return;
    }

    %ctrl = %set.getObject(0);
    %parent = %ctrl.getParent();
    if(!isObject(%parent))
    {
        return;
    }

    %oldIndex = %this.undoRecorder.indexOf(%parent, %ctrl);
    %this.brain.call(%method);
    %this.undoRecorder.recordMove(%ctrl, %parent, %oldIndex, %name);
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

// The Layout menu's Snap to Grid toggle, which says whether to use the grid and
// nothing about how big it is. Turning it back on asks the brain what the grid
// was rather than naming a number: setSnapToGrid(0) clears the flag and leaves
// the spacing alone precisely so that it can be picked back up here, and a size
// the user chose in Set Grid Size should not be thrown away by a switch that was
// never about the size. There is always an answer to pick up - the brain sets a
// grid of 10 in onAdd, and 0 only ever means "off".
function GuiEditor::SnapToGrid(%this, %gridOn)
{
    if(%gridOn)
    {
        %this.brain.setSnapToGrid(%this.brain.getGridSize());
    }
    else
    {
        %this.brain.setSnapToGrid(0);
    }
}

//METHODS-----------------------------------------------------------------
