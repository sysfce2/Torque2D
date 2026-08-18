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

function EditorCore::create( %this )
{
	// First, and before any editor builds a control: every icon in every editor
	// is a frame index into the shared sheets, and these are the names for them.
	exec("./EditorIcons.cs");

	%this.editorKeyMap = new ActionMap();
	if(!isObject(AppCore))
	{
		exec("./scripts/constants.cs");
		exec("./scripts/defaultPreferences.cs");
		exec("./gui/guiProfiles.cs");
		%this.createGuiProfiles();

		exec("./scripts/canvas.cs");
		exec("./scripts/EditorCoreSplash.cs");
		exec("./scripts/EditorProjectSelector.cs");
		exec("./scripts/EditorProjectCard.cs");
		exec("./scripts/NewProjectDialog.cs");

		%this.initializeCanvas("Torque2D: Rocket Edition");
	}
	else
	{
		%this.editorKeyMap.bindCmd( "keyboard", "ctrl tilde", "EditorCore.toggleEditor();", "");
	}
	exec("./Themes/ThemeManager.cs");
	exec("./EditorDialog.cs");
	exec("./EditorForm.cs");
	exec("./EditorIconButton.cs");
	exec("./EditorButtonBar.cs");

	// Before initGui builds the bar, and before any editor's create runs to hang
	// its own menus off it.
	exec("./EditorMenu.cs");
	exec("./EditorMenuSet.cs");

	// The segmented toggle row. It lives here rather than in the Gui Editor that
	// first grew it because editor/main.cs loads AssetAdmin FIRST, so anything the
	// Asset Manager builds at create time cannot come out of a module loaded after
	// it. Nothing in either file was ever Gui-Editor specific.
	exec("./EditorToggleIcon.cs");
	exec("./EditorChoiceRow.cs");

	// The chrome a preview's transport bar is made of. Both the Asset Manager's
	// bars build on it, and it uses EditorToggleIcon and EditorIconButton above.
	exec("./EditorTransportBar.cs");

	// The field cell, here for the same reason: the Gui Editor grew it, and the
	// Asset Manager's inspector panes are built at create time from a module that
	// loads before the Gui Editor does.
	exec("./EditorFieldRow.cs");

	exec("./EditorAssetPickerDialog.cs");
	exec("./EditorAssetPickerItem.cs");
	exec("./EditorPreferences.cs");

	// Out here rather than with NewProjectDialog above, because the Project
	// Manager renames modules too and it is loaded once a project is open.
	exec("./ModuleStamper.cs");

	new ScriptObject(ThemeManager);

	// Before any editor builds a control that wants to remember how it was left.
	new ScriptObject(EditorPreferences);

	new ScriptObject(ModuleStamper);

	%this.initGui();
	%this.editorKeyMap.push();
}

//-----------------------------------------------------------------------------

function EditorCore::destroy( %this )
{
	if(isObject(EditorPreferences))
	{
		EditorPreferences.delete();
	}

	if(isObject(ModuleStamper))
	{
		ModuleStamper.delete();
	}

	// Empty by now - every editor deletes its own menus before this runs, and
	// each module is unloaded before the one it depends on.
	if(isObject(%this.menuPark))
	{
		%this.menuPark.delete();
	}
}

function EditorCore::initGui(%this)
{
	%this.baseGui = new GuiControl()
	{
		HorizSizing = width;
		VertSizing = height;
		Position = "0 0";
		Extent = "1024 768";
	};
	ThemeManager.setProfile(%this.baseGui, "emptyProfile");

	%this.menuBar = new GuiMenuBarCtrl()
	{
		new GuiMenuItemCtrl() {
			Text = "Torque2D";

			new GuiMenuItemCtrl() {
				Text = "Close Tools";
				Command = "EditorCore.close();";
			};

			// Both go through the guard chain, because both throw away work an
			// editor is holding and neither is undoable. The chain names each
			// editor in turn and tests it with isObject, which is what keeps
			// quitting working if a module ever fails to load.
			//
			// These two and Close Tools are the only commands written here. Every
			// other menu belongs to whichever editor is open and arrives with it -
			// see setEditorMenus.
			//
			// The window's own X cannot be guarded this way. It posts the quit
			// from the window procedure with no script in between.
			new GuiMenuItemCtrl() {
				Text = "Close Project";
				Command = "EditorCore.guardedCommand(\"restartInstance();\");";
			};

			new GuiMenuItemCtrl() {
				Text = "Exit";
				Command = "EditorCore.guardedCommand(\"quit();\");";
			};
		};
		new GuiMenuItemCtrl() {
			Text = "Theme";

			new GuiMenuItemCtrl() {
				Text = "Construction Vest";
				Radio = true;
				IsOn = true;
				Command = "ThemeManager.setTheme(0);";
				Accelerator = "Ctrl 1";
			};

			new GuiMenuItemCtrl() {
				Text = "Lab Coat";
				Radio = true;
				Command = "ThemeManager.setTheme(1);";
				Accelerator = "Ctrl 2";
			};

			new GuiMenuItemCtrl() {
				Text = "Forest Robe";
				Radio = true;
				Command = "ThemeManager.setTheme(2);";
				Accelerator = "Ctrl 3";
			};

			new GuiMenuItemCtrl() {
				Text = "Torque Suit";
				Radio = true;
				Command = "ThemeManager.setTheme(3);";
				Accelerator = "Ctrl 4";
			};
		};
	};
	ThemeManager.setProfile(%this.menuBar, "menuBarProfile");
	ThemeManager.setProfile(%this.menuBar, "menuProfile", "MenuProfile");
	ThemeManager.setProfile(%this.menuBar, "menuItemProfile", "MenuItemProfile");
	ThemeManager.setProfile(%this.menuBar, "menuContentProfile", "MenuContentProfile");
	ThemeManager.setProfile(%this.menuBar, "scrollingPanelThumbProfile", "ThumbProfile");
	ThemeManager.setProfile(%this.menuBar, "scrollingPanelArrowProfile", "ArrowProfile");
	ThemeManager.setProfile(%this.menuBar, "scrollingPanelTrackProfile", "TrackProfile");

	// The bar reads Torque2D | the open editor's menus | Theme, and the two ends
	// are the only parts that never change. Theme has to be held onto by hand
	// because it has to come off and go back on around every swap - see
	// setEditorMenus - and it is taken by position rather than by name because
	// naming it would put back exactly the by-text coupling the swap exists to
	// remove. Last child, so this is still right once the Gui Editor's menus have
	// moved out of the literal above.
	%this.themeMenu = %this.menuBar.getObject(%this.menuBar.getCount() - 1);

	// Where a set's menus wait while another editor has the bar.
	%this.menuPark = new SimGroup();

	%this.baseGui.add(%this.menuBar);

	%this.tabBook = new GuiTabBookCtrl()
	{
		Class = EditorCoreTabBook;
		HorizSizing = width;
		VertSizing = height;
		Position = "0 26";
		Extent = "1024 742";
		TabPosition = top;
		Core = %this;
	};
	ThemeManager.setProfile(%this.tabBook, "tabBookProfileTop");
	ThemeManager.setProfile(%this.tabBook, "tabProfileTop", "TabProfile");

	%this.baseGui.add(%this.tabBook);

	%this.torqueBackground = new GuiSpriteCtrl()
	{
		HorizSizing = width;
		VertSizing = height;
		Position = "0 0";
		Extent = "1024 768";
		SingleFrameBitmap = "1";
		ConstrainProportions = "0";
		FullSize = "1";
		ImageColor = "255 255 255 255";
		PositionOffset = "0 0";
		Visible = false;
	};
	ThemeManager.setProfile(%this.torqueBackground, "spriteProfile");
	ThemeManager.setImage(%this.torqueBackground, "editorBG");
	%this.baseGui.add(%this.torqueBackground);

	%this.projectSelector = new GuiControl()
	{
		Class = EditorProjectSelector;
		HorizSizing = width;
		VertSizing = height;
		Position = "0 0";
		Extent = "1024 768";
		Visible = false;
	};
	ThemeManager.setProfile(%this.projectSelector, "emptyProfile");
	%this.baseGui.add(%this.projectSelector);

	%this.splash = new GuiSpriteCtrl()
	{
		Class = EditorCoreSplash;
		HorizSizing = width;
		VertSizing = height;
		Position = "0 0";
		Extent = "1024 768";
		Bitmap = "./images/t2d_rocket_splash.png";
		SingleFrameBitmap = "1";
		constrainProportions = "1";
		FullSize = "0";
		ImageSize = "672 480";
		ImageColor = "255 255 255 0";
		PositionOffset = "0 20";
		Visible = false;
	};
	ThemeManager.setProfile(%this.splash, "spriteProfile");
	%this.baseGui.add(%this.splash);
}

function EditorCore::toggleEditor(%this)
{
    // Is the console awake?
    if ( %this.baseGui.isAwake() )
    {
        // Yes, so deactivate it.
        %this.close();
        return;
    }

    // Activate it.
    %this.open();
}

function EditorCore::open(%this)
{
	if ( $enableDirectInput )
        deactivateKeyboard();

    Canvas.pushDialog(%this.baseGui);

	%this.tabBook.selectPage(0);
	Canvas.setCursor(ThemeManager.activeTheme.defaultCursor);
}

function EditorCore::close(%this)
{
	if ( $enableDirectInput )
		activateKeyboard();
	Canvas.popDialog(%this.baseGui);

	if(isObject(defaultCursor))
	{
		Canvas.setCursor(defaultCursor);
	}
}

// Run %command, unless an editor is holding something the command would discard
// without being able to give it back. Used by Close Project and Exit.
//
// Two editors have something to lose now, and they are asked one at a time. Each
// guard either runs what it was given or takes the decision away and hands it on
// once the user has answered, so the chain reads:
//
//   guardedCommand              the Asset Manager's unsaved assets, then...
//   guardedCommandAfterAssets   the Gui Editor's unsaved document, then...
//   eval                        the command itself
//
// A third editor with a document joins at the front of that chain, not by being
// added to a list: the answers are not interchangeable, and each guard needs to
// name the one that follows it.
function EditorCore::guardedCommand(%this, %command)
{
	if(isObject(AssetAdmin) && AssetAdmin.hasUnsavedAssets())
	{
		AssetAdmin.guardAssets(%command);
		return;
	}

	%this.guardedCommandAfterAssets(%command);
}

// The rest of the chain, once the Asset Manager has been dealt with. Discarding
// unsaved assets comes back in here rather than at the top, because the assets
// are still unsaved and asking again would never end.
function EditorCore::guardedCommandAfterAssets(%this, %command)
{
	if(isObject(GuiEditor))
	{
		GuiEditor.guardDocument(%command);
		return;
	}

	eval(%command);
}

// Put %menuSet's menus on the bar and take the last editor's off, so the bar
// always reads Torque2D | the open editor's menus | Theme. Pass "" for an editor
// with no menus of its own, which is what the Console and the Project Manager
// are. Called from every editor's open() and close().
//
// Theme comes off and goes back on around the swap, and that is not fussiness.
// The bar links the chain its keyboard walk follows by assuming each new menu
// was appended - it takes the second-to-last child as the new one's neighbour -
// and reordering afterwards repairs the layout but not the chain. Appending is
// the only move that leaves the bar correct, so the fixed tail has to move.
//
// Nothing here is deleted. Each set parks its own menus, which is what keeps
// them alive, keeps them out of the accelerator walk, and keeps the bar they
// remember - set once, never filled in again - pointing at a real bar.
function EditorCore::setEditorMenus(%this, %menuSet)
{
	if(%this.activeMenus == %menuSet)
	{
		return;
	}

	%this.menuPark.add(%this.themeMenu);

	if(isObject(%this.activeMenus))
	{
		%this.activeMenus.detach();
	}
	%this.activeMenus = %menuSet;
	if(isObject(%menuSet))
	{
		%menuSet.attach();
	}

	%this.menuBar.add(%this.themeMenu);

	// The canvas keeps one flat list of accelerators and rebuilds it only when a
	// dialog is pushed or popped. A tab change is neither, so without this the
	// menus that just arrived have dead shortcuts and the ones that just left
	// still have live ones - a parked item is still active and still visible, so
	// its command would run.
	if(isObject(Canvas))
	{
		Canvas.rebuildAcceleratorMap();
	}
}

function EditorCore::RegisterEditor(%this, %name, %editor)
{
	%this.page[%name] = new GuiTabPageCtrl()
	{
		HorizSizing = width;
		VertSizing = height;
		Position = "0 0";
		Extent = "1024 768";
		Text = %name;
		Editor = %editor;
	};
	ThemeManager.setProfile(%this.page[%name], "tabPageProfile");

	return %this.page[%name];
}

function EditorCore::FinishRegistration(%this, %page)
{
	%this.tabBook.add(%page);
}

function EditorCoreTabBook::onTabSelected(%this, %tabText)
{
	if(isObject(%this.openEditor))
	{
		%this.openEditor.close();
	}
	%this.Core.page[%tabText].Editor.open();
	%this.openEditor = %this.Core.page[%tabText].Editor;
}

function EditorCore::findModuleOfPath(%this, %pathWithFile)
{
	%pathWithFile = makeFullPath(%pathWithFile);
	%loadedModules = ModuleDatabase.findModules(false);

	%chosenModSig = "";
	for(%i = 0; %i < getWordCount(%loadedModules); %i++)
	{
		%mod = getWord(%loadedModules, %i);
		%modPath = %this.flattenPath(%mod.ModulePath);

		//Now compare the two paths to see if they go together
		if(strPos(%pathWithFile, %modPath) != -1)
		{
			%chosenModSig = %mod.Signature;
			break;
		}
	}

	return %chosenModSig;
}

function EditorCore::flattenPath(%this, %path)
{
	%pen = 0;
	for(%i = 0; %i < getUnitCount(%path, "/"); %i++)
	{
		%token = getUnit(%path, %i, "/");

		if(%token $= "..")
		{
			%pen = mGetMax(%pen - 1, 0);
		}
		else
		{
			%token[%pen] = %token;
			%pen++;
		}
	}

	%result = %token[0];
	for(%i = 1; %i <= %pen; %i++)
	{
		%result = %result @ "/" @ %token[%i];
	}

	return %result;
}

function EditorCore::showProjectSelector(%this)
{
	%this.menuBar.setVisible(false);
	%this.tabBook.setVisible(false);
	%this.projectSelector.setVisible(true);
	%this.splash.show();
	%this.torqueBackground.setVisible(true);
	%this.projectSelector.schedule(2800, "show");
	%this.startListening(%this.projectSelector);
}

function EditorCore::onProjectSelected(%this)
{
	%this.close();
	%this.menuBar.setVisible(true);
	%this.tabBook.setVisible(true);
	%this.projectSelector.setVisible(false);
	%this.torqueBackground.setVisible(false);
	%this.stopListening(%this.projectSelector);
	%this.schedule(500, "finishProjectSelection");
}

function EditorCore::finishProjectSelection(%this)
{
	ModuleDatabase.LoadExplicit("AppCore", 1);
	%this.editorKeyMap.bindCmd( "keyboard", "ctrl tilde", "EditorCore.toggleEditor();", "");
}

function EditorCore::alert(%this, %text)
{
	//Eventually this should open a dialog with this text. For now we'll dump it into the console and hope the user notices.
	warn(%text);
}

function EditorCore::deleteDialog(%this)
{
	%this.dialog.delete();
}

// Deferred delete for a specific dialog. Unlike deleteDialog's shared
// %this.dialog slot, this takes the dialog as an argument, so stacked
// dialogs can each schedule their own cleanup without racing. Never
// schedule the native "delete" directly on an object - the scheduler
// dispatches it inside the object's own script-callback guard, which
// asserts; routing through a parent's script method like this one is safe.
function EditorCore::deleteDialogObject(%this, %dialog)
{
	if(isObject(%dialog))
	{
		%dialog.delete();
	}
}

// Titles the picker after what it is picking. The type names arrive from the
// engine as one word ("ImageAsset"), which is how a class is spelled and not how
// a title is, so the internal capital becomes a space and the article is chosen
// to suit: an Image Asset, but a Font Asset.
function EditorCore::assetTypeTitle(%this, %assetType)
{
	// strcmp, not $=. String-equal is case-insensitive in TorqueScript, so
	// "m" $= "M" is true and a test for "is this letter a capital" written that
	// way answers yes for every letter.
	%spaced = "";
	for(%i = 0; %i < strlen(%assetType); %i++)
	{
		%char = getSubStr(%assetType, %i, 1);
		%isCapital = (strcmp(%char, strupr(%char)) == 0) && (strcmp(%char, strlwr(%char)) != 0);
		if(%i > 0 && %isCapital)
		{
			%spaced = %spaced SPC %char;
		}
		else
		{
			%spaced = %spaced @ %char;
		}
	}

	%first = strupr(getSubStr(%assetType, 0, 1));
	%article = (strstr("AEIOU", %first) != -1) ? "an" : "a";

	return "Choose" SPC %article SPC %spaced;
}

// The one way into the asset picker. Both callers come through here: the Gui
// Profile Editor's Image Asset row, and the native inspector's browse button,
// which the engine points at this method by name (see
// GuiInspectorTypeAsset::constructEditControl). The picker hands the chosen
// asset id to %callbackTarget.%callbackMethod, the same target-and-method pair
// every other editor dialog returns through.
function EditorCore::openAssetPicker(%this, %callbackTarget, %callbackMethod, %currentAsset, %assetType)
{
	if(%assetType $= "")
	{
		%this.alert("openAssetPicker needs an asset type to look for.");
		return;
	}

	%width = 640;
	%height = 500;
	%dialog = new GuiControl()
	{
		class = "EditorAssetPickerDialog";
		superclass = "EditorDialog";
		dialogSize = (%width + 8) SPC (%height + 8);
		dialogCanClose = true;
		dialogText = %this.assetTypeTitle(%assetType);
		assetType = %assetType;
		currentAsset = %currentAsset;
		callbackTarget = %callbackTarget;
		callbackMethod = %callbackMethod;
	};
	%dialog.init(%width, %height);

	Canvas.pushDialog(%dialog);
}
