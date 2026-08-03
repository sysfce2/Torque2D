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
	exec("./EditorAssetPickerDialog.cs");
	exec("./EditorAssetPickerItem.cs");

	new ScriptObject(ThemeManager);

	%this.initGui();
	%this.editorKeyMap.push();
}

//-----------------------------------------------------------------------------

function EditorCore::destroy( %this )
{
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

			// Both go through the Gui Editor's guard, because both throw away a
			// Gui that is being authored and neither is undoable. GuiEditor is
			// named directly, as it is by every item in the File, Edit, Layout
			// and Select menus below; the isObject test is what keeps quitting
			// working if the Gui Editor module ever fails to load.
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
			Text = "File";
			Active = "0";

			new GuiMenuItemCtrl() {
				Text = "New Gui";
				Command = "GuiEditor.NewGui();";
				Accelerator = "Ctrl N";
			};
			new GuiMenuItemCtrl() {
				Text = "Open Gui...";
				Command = "GuiEditor.OpenGui();";
				Accelerator = "Ctrl O";
			};
			new GuiMenuItemCtrl() { Text = "-"; };
			new GuiMenuItemCtrl() {
				Text = "Save Gui...";
				Command = "GuiEditor.SaveGui();";
				Accelerator = "Ctrl S";
			};
			new GuiMenuItemCtrl() {
				Text = "Save Gui As...";
				Command = "GuiEditor.SaveGuiAs();";
				Accelerator = "Ctrl-Shift S";
			};
			new GuiMenuItemCtrl() { Text = "-"; };
			// Offered only once the Gui has a file to go back to; GuiEditor
			// keeps that up to date in refreshFileMenu. No accelerator - it
			// throws away everything since the last save, and that is not a
			// thing to have a shortcut for.
			new GuiMenuItemCtrl() {
				Text = "Revert";
				Command = "GuiEditor.Revert();";
				Active = "0";
			};
		};
		new GuiMenuItemCtrl() {
			Text = "Edit";
			Active = "0";

			new GuiMenuItemCtrl() {
				Text = "Undo";
				Command = "GuiEditor.Undo();";
				Accelerator = "Ctrl Z";
			};
			new GuiMenuItemCtrl() {
				Text = "Redo";
				Command = "GuiEditor.Redo();";
				Accelerator = "Ctrl-Shift Z";
			};
			new GuiMenuItemCtrl() { Text = "-"; };
			new GuiMenuItemCtrl() {
				Text = "Cut";
				Command = "GuiEditor.Cut();";
				Accelerator = "Ctrl X";
			};
			new GuiMenuItemCtrl() {
				Text = "Copy";
				Command = "GuiEditor.Copy();";
				Accelerator = "Ctrl C";
			};
			new GuiMenuItemCtrl() {
				Text = "Paste";
				Command = "GuiEditor.Paste();";
				Accelerator = "Ctrl V";
			};
			new GuiMenuItemCtrl() {
				Text = "Duplicate";
				Command = "GuiEditor.Duplicate();";
				Accelerator = "Ctrl D";
			};
			new GuiMenuItemCtrl() { Text = "-"; };
			// DeleteSelection, not Delete: delete is a console method on every
			// SimObject, so GuiEditor.Delete() would quietly destroy the editor
			// rather than the selection.
			//
			// The accelerator cannot double-fire with the Delete key the canvas
			// and the Explorer tree already handle themselves. The canvas
			// consults accelerators only once the first responder has passed on
			// the key -- the same thing that lets a text box in the properties
			// pane keep Ctrl+C. What it adds is Delete working while focus is in
			// a tool window.
			new GuiMenuItemCtrl() {
				Text = "Delete";
				Command = "GuiEditor.DeleteSelection();";
				Accelerator = "Delete";
			};
		};
		new GuiMenuItemCtrl() {
			Text = "Layout";
			Active = "0";

			new GuiMenuItemCtrl() {
				Text = "Nudge Up";
				Command = "GuiEditor.brain.moveSelection(0,-1);";
				Accelerator = "Up";
			};
			new GuiMenuItemCtrl() {
				Text = "Nudge Down";
				Command = "GuiEditor.brain.moveSelection(0,1);";
				Accelerator = "Down";
			};
			new GuiMenuItemCtrl() {
				Text = "Nudge Left";
				Command = "GuiEditor.brain.moveSelection(-1,0);";
				Accelerator = "Left";
			};
			new GuiMenuItemCtrl() {
				Text = "Nudge Right";
				Command = "GuiEditor.brain.moveSelection(1,0);";
				Accelerator = "Right";
			};
			new GuiMenuItemCtrl() { Text = "-"; };
			new GuiMenuItemCtrl() {
				Text = "Shrink Height";
				Command = "GuiEditor.changeExtent(0,-1);";
				Accelerator = "Ctrl Up";
			};
			new GuiMenuItemCtrl() {
				Text = "Expand Height";
				Command = "GuiEditor.changeExtent(0, 1);";
				Accelerator = "Ctrl Down";
			};
			new GuiMenuItemCtrl() {
				Text = "Shrink Width";
				Command = "GuiEditor.changeExtent(-1,0);";
				Accelerator = "Ctrl Left";
			};
			new GuiMenuItemCtrl() {
				Text = "Expand Width";
				Command = "GuiEditor.changeExtent(1,0);";
				Accelerator = "Ctrl Right";
			};
			new GuiMenuItemCtrl() { Text = "-"; };
			new GuiMenuItemCtrl() {
				Text = "Align Top";
				Command = "GuiEditor.Justify(3);";
				Accelerator = "Ctrl T";
			};
			new GuiMenuItemCtrl() {
				Text = "Align Bottom";
				Command = "GuiEditor.Justify(4);";
				Accelerator = "Ctrl B";
			};
			new GuiMenuItemCtrl() {
				Text = "Align Left";
				Command = "GuiEditor.Justify(0);";
				Accelerator = "Ctrl L";
			};
			new GuiMenuItemCtrl() {
				Text = "Align Right";
				Command = "GuiEditor.Justify(2);";
				Accelerator = "Ctrl R";
			};
			new GuiMenuItemCtrl() { Text = "-"; };
			new GuiMenuItemCtrl() {
				Text = "Center Horizontally";
				Command = "GuiEditor.Justify(1);";
			};
			new GuiMenuItemCtrl() {
				Text = "Space Vertically";
				Command = "GuiEditor.Justify(5);";
			};
			new GuiMenuItemCtrl() {
				Text = "Space Horizontally";
				Command = "GuiEditor.Justify(6);";
			};
			new GuiMenuItemCtrl() { Text = "-"; };
			new GuiMenuItemCtrl() {
				Text = "Bring to Front";
				Command = "GuiEditor.BringToFront();";
				Accelerator = "Ctrl-Shift Up";
			};
			new GuiMenuItemCtrl() {
				Text = "Push to Back";
				Command = "GuiEditor.PushToBack();";
				Accelerator = "Ctrl-Shift Down";
			};
			new GuiMenuItemCtrl() { Text = "-"; };
			new GuiMenuItemCtrl() {
				Text = "Set Grid Size...";
				Command = "GuiEditor.SetGridSize();";
				Accelerator = "Ctrl-Shift G";
			};
			new GuiMenuItemCtrl() {
				Text = "Snap to Grid";
				Toggle = "1";
				IsOn = "1";
				Command = "GuiEditor.SnapToGrid(true);";
				AltCommand = "GuiEditor.SnapToGrid(false);";
				Accelerator = "Ctrl G";
			};
		};
		new GuiMenuItemCtrl() {
			Text = "Select";
			Active = "0";

			new GuiMenuItemCtrl() {
				Text = "Select All";
				Command = "GuiEditor.brain.SelectAll();";
				Accelerator = "Ctrl A";
			};
			// Ctrl-Shift A rather than Ctrl D, which Duplicate has: this is what
			// deselect is bound to nearly everywhere else, and it pairs with
			// Ctrl A above.
			new GuiMenuItemCtrl() {
				Text = "Deselect";
				Command = "GuiEditor.brain.clearSelection();";
				Accelerator = "Ctrl-Shift A";
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

// Run %command, unless there is a Gui being authored with changes in it - in
// which case the Gui Editor asks about those first and runs it afterwards, or
// not at all. Used by Close Project and Exit, which are the two commands in this
// menu that discard a document without being able to give it back.
//
// The Gui Editor is the only editor with a document to lose. If another ever
// grows one, this is where it joins in.
function EditorCore::guardedCommand(%this, %command)
{
	if(isObject(GuiEditor))
	{
		GuiEditor.guardDocument(%command);
		return;
	}

	eval(%command);
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