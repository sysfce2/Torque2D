//-----------------------------------------------------------------------------
// Regression harness for the three Stand Alone profile complaints:
//   1. a stand-alone profile never appears in a control's Profile dropdown
//   2. the preview sample does not follow the category picker
//   3. there is no way to rename one
// Also covers the two paths the fix has to keep working: a bundle written by an
// older version (a SimGroup) migrating on load, and a bundle round-tripping
// through TAML on revert. Echoes SMOKE PASS/FAIL lines and quits.
//-----------------------------------------------------------------------------

setRandomSeed();
setLogMode(2);
setScriptExecEcho(false);
trace(false);
$Scripts::ignoreDSOs = true;
setCompanyAndProduct("Torque Game Engines", "Torque2D");
ModuleDatabase.EchoInfo = false;
AssetDatabase.EchoInfo = false;
AssetDatabase.IgnoreAutoUnload = true;

testExec("editor/main.cs");

function smokeCheck(%label, %condition)
{
    if(%condition)
    {
        echo("SMOKE PASS: " @ %label);
    }
    else
    {
        echo("SMOKE FAIL: " @ %label);
    }
}

function themesPath()
{
    return pathConcat(getMainDotCsDir(), "smokeThemeProject", "themes");
}

// Depth-first search of a control tree for a drop-down offering %text. The
// Profile field of a control is rendered by GuiInspectorTypeGuiProfile as a
// GuiDropDownCtrl filled with every profile the engine could find, so this
// answers "would the user see this profile in the list".
function dropdownOffers(%ctrl, %text)
{
    if(!isObject(%ctrl))
    {
        return false;
    }

    if(%ctrl.getClassName() $= "GuiDropDownCtrl" && %ctrl.findItemText(%text, false) >= 0)
    {
        return true;
    }

    for(%i = 0; %i < %ctrl.getCount(); %i++)
    {
        if(dropdownOffers(%ctrl.getObject(%i), %text))
        {
            return true;
        }
    }
    return false;
}

function previewSampleClass(%dialog, %index)
{
    %stage = %dialog.preview.stage;
    if(!isObject(%stage) || %index >= %stage.getCount())
    {
        return "";
    }
    return %stage.getObject(%index).getClassName();
}

// Does the themes folder really hold this file? Asked of the filesystem, not of
// isFile: isFile answers out of the ResourceManager, which keeps every .taml it
// has read this session, so a file deleted after being loaded still reads as
// present.
function fileOnDisk(%name)
{
    %files = getFileList(themesPath());
    for(%i = 0; %i < getFieldCount(%files); %i++)
    {
        if(getField(%files, %i) $= %name)
        {
            return true;
        }
    }
    return false;
}

// The tree proxy standing for a stand-alone profile.
function standaloneProxyFor(%dialog, %profile)
{
    %folder = %dialog.library.standaloneFolder;
    for(%i = 0; %i < %folder.getCount(); %i++)
    {
        if(%folder.getObject(%i).target == %profile)
        {
            return %folder.getObject(%i);
        }
    }
    return 0;
}

// Drive the category picker the way a click does.
function chooseCategory(%dialog, %category)
{
    %drop = %dialog.profileForm.categoryDrop;
    %index = %drop.findItemText(%category, false);
    if(%index < 0)
    {
        return false;
    }
    %drop.setSelected(%index);
    return true;
}

// Write a bundle in the shape older versions saved: a SimGroup, which owns the
// profile and therefore takes it out of the Gui data group on load.
function writeLegacyBundle(%name)
{
    %profile = new GuiControlProfile(%name);
    %profile.fillColor = "10 20 30 255";

    %group = new ScriptGroup() { class = "GuiProfileBundle"; };
    %group.add(%profile);

    %file = pathConcat(themesPath(), %name @ ".taml");
    TAMLWrite(%group, %file);
    echo("REPRO: legacy file = " @ %file @ ", isFile = " @ isFile(%file) @
        ", profile named '" @ %profile.getName() @ "', group is a SimSet = " @
        %group.isMemberOfClass("SimSet"));

    // A group takes its children with it, which is the whole point of this file.
    %group.delete();
}

schedule(2000, 0, "reproStep1");

function reproStep1()
{
    ProjectManager.setProjectFolder("smokeThemeProject");
    createPath(themesPath() @ "/");
    writeLegacyBundle("LegacyProfile");
    smokeCheck("legacy bundle written and released", !isObject(LegacyProfile));

    GuiEditor.open();
    GuiEditor.openProfileEditor();

    %dialog = GuiEditor.profileEditorDialog;
    smokeCheck("dialog opened", isObject(%dialog));
    echo("REPRO: library themes path = " @ %dialog.library.getThemesPath());

    // The legacy file is picked up by the dialog's scan.
    smokeCheck("legacy bundle loaded", isObject(LegacyProfile));
    smokeCheck("legacy profile migrated into the gui data group",
        isObject(LegacyProfile) && LegacyProfile.getGroup() == GuiDataGroup.getId());
    smokeCheck("legacy profile kept its values",
        isObject(LegacyProfile) && LegacyProfile.fillColor $= "10 20 30 255");

    %dialog.onNewStandalone();
    %nameDialog = %dialog.childDialog;
    smokeCheck("name dialog opened", isObject(%nameDialog));
    %nameDialog.nameBox.setText("RubyButton");
    %nameDialog.onDone();

    %profile = %dialog.library.findProfileByName("RubyButton");
    smokeCheck("standalone profile created", isObject(%profile));

    // PROBLEM 1: the profile must stay where the engine looks for it.
    smokeCheck("standalone profile is in the gui data group",
        %profile.getGroup() == GuiDataGroup.getId());

    %proxy = standaloneProxyFor(%dialog, %profile);
    smokeCheck("standalone proxy found", isObject(%proxy));
    %dialog.onTreeSelect(%proxy);
    smokeCheck("profile form bound", %dialog.profileForm.target == %profile);

    // PROBLEM 2: the sample follows the category picker.
    smokeCheck("CheckBox category offered", chooseCategory(%dialog, "CheckBox"));
    smokeCheck("category written to the profile", %profile.category $= "CheckBox");
    smokeCheck("preview shows a check box", previewSampleClass(%dialog, 0) $= "GuiCheckBoxCtrl");

    // A category whose sample borrows sibling profiles from a theme: with no
    // theme it must still build rather than fall back to the generic sample.
    smokeCheck("DropDown category offered", chooseCategory(%dialog, "DropDown"));
    smokeCheck("preview shows a drop down", previewSampleClass(%dialog, 0) $= "GuiDropDownCtrl");

    smokeCheck("Window category offered", chooseCategory(%dialog, "Window"));
    smokeCheck("preview shows a window", previewSampleClass(%dialog, 0) $= "GuiWindowCtrl");

    // Back to the category this profile is really for.
    chooseCategory(%dialog, "Button");
    smokeCheck("preview shows a button", previewSampleClass(%dialog, 0) $= "GuiButtonCtrl");

    // PROBLEM 3: rename it, through the toolbar path.
    smokeCheck("rename enabled for a standalone", %dialog.getRootSelected());
    %dialog.onRename();
    %renameDialog = %dialog.childDialog;
    smokeCheck("rename dialog opened", isObject(%renameDialog));
    smokeCheck("rename dialog seeded with the old name", %renameDialog.defaultName $= "RubyButton");
    %renameDialog.nameBox.setText("RubySwitch");
    %renameDialog.onDone();

    smokeCheck("profile renamed", %profile.getName() $= "RubySwitch");
    // A bare word is a string in TorqueScript, so this is a name lookup.
    smokeCheck("renamed profile answers to the new name", RubySwitch.getId() == %profile);
    smokeCheck("tree label followed the rename", %proxy.baseLabel $= "RubySwitch");
    smokeCheck("profile pane header followed the rename",
        %dialog.profileForm.nameLabel.getText() $= "Profile:  RubySwitch");

    schedule(600, 0, "reproStep2");
}

function reproStep2()
{
    %dialog = GuiEditor.profileEditorDialog;
    smokeCheck("dialog survived the helper dialogs", isObject(%dialog));
    %dialog.onSave();

    schedule(1000, 0, "reproStep3");
}

function reproStep3()
{
    smokeCheck("standalone file written under the new name",
        fileOnDisk("RubySwitch.taml"));
    smokeCheck("file under the old name removed",
        !fileOnDisk("RubyButton.taml"));
    smokeCheck("standalone profile persists after close", isObject(RubySwitch));
    smokeCheck("standalone profile still in the gui data group",
        isObject(RubySwitch) && RubySwitch.getGroup() == GuiDataGroup.getId());

    // Round-trip: dirty the profile, then discard, which deletes the bundle and
    // re-reads the file it was saved to.
    GuiEditor.openProfileEditor();
    %dialog = GuiEditor.profileEditorDialog;
    smokeCheck("dialog reopened", isObject(%dialog));

    RubySwitch.fillColor = "1 2 3 4";
    %proxy = standaloneProxyFor(%dialog, RubySwitch.getId());
    smokeCheck("reopened dialog knows the standalone", isObject(%proxy));
    %dialog.library.markDirty(%proxy.root);

    schedule(600, 0, "reproStep4");
}

function reproStep4()
{
    %dialog = GuiEditor.profileEditorDialog;
    %dialog.onClose();
    %confirm = %dialog.childDialog;
    smokeCheck("discard confirm opened", isObject(%confirm));
    %confirm.onConfirm();

    schedule(800, 0, "reproStep5");
}

function reproStep5()
{
    smokeCheck("dialog closed after discard", !isObject(GuiEditor.profileEditorDialog));
    smokeCheck("standalone re-read from its file", isObject(RubySwitch));
    smokeCheck("re-read standalone is in the gui data group",
        isObject(RubySwitch) && RubySwitch.getGroup() == GuiDataGroup.getId());
    smokeCheck("re-read standalone reverted its value",
        isObject(RubySwitch) && RubySwitch.fillColor !$= "1 2 3 4");
    smokeCheck("re-read standalone kept its category",
        isObject(RubySwitch) && RubySwitch.category $= "Button");

    // The end of PROBLEM 1: inspect a freshly made control and read the real
    // Profile dropdown the Gui Editor builds for it.
    %button = new GuiButtonCtrl()
    {
        Position = "0 0";
        Extent = "100 30";
        Text = "Probe";
    };
    GuiEditor.inspectorWindow.inspector.inspect(%button);

    smokeCheck("inspector offers a known engine profile",
        dropdownOffers(GuiEditor.inspectorWindow.inspector, "GuiDefaultProfile"));
    smokeCheck("inspector offers the standalone profile",
        dropdownOffers(GuiEditor.inspectorWindow.inspector, "RubySwitch"));
    smokeCheck("inspector offers the migrated legacy profile",
        dropdownOffers(GuiEditor.inspectorWindow.inspector, "LegacyProfile"));

    GuiEditor.inspectorWindow.inspector.clear();
    %button.delete();

    // Reopen for the delete pass, and give the profile a custom border first:
    // the bundle is a set and does not own what it holds, so a delete that only
    // dropped the set would leak both objects.
    GuiEditor.openProfileEditor();
    %dialog = GuiEditor.profileEditorDialog;
    %proxy = standaloneProxyFor(%dialog, RubySwitch.getId());
    smokeCheck("standalone still in the tree on reopen", isObject(%proxy));
    %dialog.onTreeSelect(%proxy);

    %border = %dialog.createCustomBorder(%proxy.root, "RubySwitchEdge");
    smokeCheck("custom border created on the bundle", isObject(%border));
    smokeCheck("custom border is in the gui data group",
        isObject(%border) && %border.getGroup() == GuiDataGroup.getId());
    smokeCheck("custom border is named by the bundle", %proxy.root.isMember(%border));

    schedule(600, 0, "reproStep6");
}

function reproStep6()
{
    %dialog = GuiEditor.profileEditorDialog;
    smokeCheck("delete enabled for a standalone", %dialog.getRootSelected());

    %bundle = %dialog.selectedRoot();
    %bundleId = %bundle.getId();

    %dialog.onDelete();
    %confirm = %dialog.childDialog;
    smokeCheck("delete confirm opened", isObject(%confirm));
    %confirm.onConfirm();

    smokeCheck("standalone profile deleted", !isObject(RubySwitch));
    smokeCheck("its custom border went with it", !isObject(RubySwitchEdge));
    smokeCheck("the bundle itself is gone", !isObject(%bundle));
    smokeCheck("tree proxy removed", !isObject(%dialog.library.standaloneProxy[%bundleId]));
    smokeCheck("standalone folder emptied of it",
        %dialog.library.standaloneFolder.getCount() == 1);
    smokeCheck("the other standalone survived", isObject(LegacyProfile));

    // The file only goes on save.
    smokeCheck("file still on disk before saving",
        fileOnDisk("RubySwitch.taml"));
    smokeCheck("delete leaves the library dirty", %dialog.library.isDirty());
    %dialog.onSave();

    schedule(800, 0, "reproStep7");
}

function reproStep7()
{
    smokeCheck("dialog closed after save", !isObject(GuiEditor.profileEditorDialog));
    smokeCheck("file removed on save", !fileOnDisk("RubySwitch.taml"));
    smokeCheck("untouched standalone file kept",
        fileOnDisk("LegacyProfile.taml"));

    // The deleted profile is gone from the dropdowns; the survivor is not.
    %button = new GuiButtonCtrl()
    {
        Position = "0 0";
        Extent = "100 30";
        Text = "Probe";
    };
    GuiEditor.inspectorWindow.inspector.inspect(%button);
    smokeCheck("inspector no longer offers the deleted profile",
        !dropdownOffers(GuiEditor.inspectorWindow.inspector, "RubySwitch"));
    smokeCheck("inspector still offers the surviving profile",
        dropdownOffers(GuiEditor.inspectorWindow.inspector, "LegacyProfile"));
    GuiEditor.inspectorWindow.inspector.clear();
    %button.delete();

    %names = "RubyButton" TAB "RubySwitch" TAB "LegacyProfile";
    for(%i = 0; %i < getFieldCount(%names); %i++)
    {
        %file = pathConcat(themesPath(), getField(%names, %i) @ ".taml");
        if(isFile(%file))
        {
            fileDelete(%file);
        }
    }

    echo("SMOKE DONE");
    schedule(250, 0, "quit");
}
