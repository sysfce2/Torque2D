//-----------------------------------------------------------------------------
// Temporary smoke test for the Gui Profile Editor dialog. Boots the editor,
// drives the dialog through create/edit/save/reopen/verify, echoes SMOKE
// PASS/FAIL lines to the console log, and quits.
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

$SmokeStep = 0;

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

schedule(2000, 0, "smokeStep1");

// Open the dialog in a scratch project folder, create and edit a theme, save.
function smokeStep1()
{
    ProjectManager.setProjectFolder("smokeThemeProject");

    // Match real usage: the dialog opens from inside the Gui Editor tab,
    // which runs with editorMode on (the name-shadowing trap).
    GuiEditor.open();

    GuiEditor.openProfileEditor();
    %dialog = GuiEditor.profileEditorDialog;
    smokeCheck("dialog opened", isObject(%dialog));
    smokeCheck("library created", isObject(%dialog.library));
    smokeCheck("tree shows proxy root", isObject(%dialog.library.proxyRoot));

    // Create the theme through the real New Theme flow: the name dialog's
    // OK path runs the callback and then closes/deletes the name dialog.
    %dialog.onNewTheme();
    %nameDialog = %dialog.childDialog;
    smokeCheck("name dialog opened", isObject(%nameDialog));
    %nameDialog.nameBox.setText("SmokeTheme");
    %nameDialog.onDone();

    %theme = SmokeTheme;
    smokeCheck("theme created", isObject(%theme));
    smokeCheck("theme member exists", isObject(SmokeThemeButtonProfile));

    SmokeThemeButtonProfile.fillColor = "9 8 7 6";
    smokeCheck("override marked", SmokeTheme.isFieldOverridden(SmokeThemeButtonProfile, "fillColor"));
    %dialog.library.markDirty(%theme);
    smokeCheck("library dirty", %dialog.library.isDirty());

    // Select the TreeView category so the inspector and the preview's own
    // tree sample build, then leave the dialog on screen so frames actually
    // render the tree, inspector, and preview before saving.
    %proxy = %dialog.library.categoryProxy[%theme.getId() @ "_TreeView"];
    smokeCheck("category proxy found", isObject(%proxy));
    %dialog.onTreeSelect(%proxy);

    schedule(600, 0, "smokeStep1b");
}

// After the dialog has rendered for a while (and the name dialog's deferred
// delete has fired), exercise the dirty-cancel confirm dialog, then back out.
function smokeStep1b()
{
    %dialog = GuiEditor.profileEditorDialog;
    smokeCheck("dialog survived name dialog close", isObject(%dialog));
    smokeCheck("name dialog deleted", !isObject(%dialog.childDialog));

    // Cancel while dirty opens the confirm dialog; cancel the confirm to
    // return to the still-open editor.
    %dialog.onCancel();
    %confirm = %dialog.childDialog;
    smokeCheck("confirm dialog opened", isObject(%confirm));
    %confirm.onClose();

    schedule(600, 0, "smokeStep1c");
}

// After the confirm dialog's deferred delete has fired, save for real.
function smokeStep1c()
{
    %dialog = GuiEditor.profileEditorDialog;
    smokeCheck("dialog survived confirm close", isObject(%dialog));
    smokeCheck("confirm dialog deleted", !isObject(%dialog.childDialog));
    %dialog.onSave();

    schedule(500, 0, "smokeStep2");
}

// The dialog is gone; the file exists; the theme objects PERSIST so guis
// (and the Gui Editor's profile dropdowns) can reference them.
function smokeStep2()
{
    %file = pathConcat(getMainDotCsDir(), "smokeThemeProject", "themes", "SmokeTheme.taml");
    smokeCheck("theme file written", isFile(%file));
    smokeCheck("dialog deleted after save", !isObject(GuiEditor.profileEditorDialog));
    smokeCheck("theme persists after close", isObject(SmokeTheme));
    smokeCheck("member profile persists after close", isObject(SmokeThemeButtonProfile));
    smokeCheck("member profile is in the gui data group",
        isObject(SmokeThemeButtonProfile) && SmokeThemeButtonProfile.getGroup() == GuiDataGroup.getId());

    GuiEditor.openProfileEditor();
    %dialog = GuiEditor.profileEditorDialog;
    smokeCheck("dialog reopened", isObject(%dialog));
    smokeCheck("theme still present on reopen", isObject(SmokeTheme));
    smokeCheck("reopen does not duplicate themes", %dialog.library.proxyRoot.getCount() == 2);
    if(isObject(SmokeTheme))
    {
        smokeCheck("override persisted", SmokeTheme.isFieldOverridden(SmokeThemeButtonProfile, "fillColor"));
        smokeCheck("override value persisted", SmokeThemeButtonProfile.fillColor $= "9 8 7 6");
    }

    // Let the reopened dialog render before exercising the discard path.
    schedule(600, 0, "smokeStep2b");
}

// Make a second, never-saved theme plus an edit to the saved one, then
// discard: the unsaved theme must vanish and the saved one revert.
function smokeStep2b()
{
    %dialog = GuiEditor.profileEditorDialog;
    smokeCheck("reopened dialog survived rendering", isObject(%dialog));

    %second = %dialog.library.createTheme("SmokeThemeTwo");
    smokeCheck("second theme created", isObject(%second));

    SmokeThemeButtonProfile.fillColor = "1 1 1 1";
    %dialog.library.markDirty(SmokeTheme);

    %dialog.onClose();
    %confirm = %dialog.childDialog;
    smokeCheck("discard confirm opened", isObject(%confirm));
    %confirm.onConfirm();

    schedule(500, 0, "smokeStep3");
}

// After the discard: the unsaved theme is gone, the saved theme reverted
// from its file, and editorMode is restored.
function smokeStep3()
{
    smokeCheck("dialog closed after discard", !isObject(GuiEditor.profileEditorDialog));
    smokeCheck("unsaved theme discarded", !isObject(SmokeThemeTwoButtonProfile));
    smokeCheck("saved theme survives discard", isObject(SmokeTheme));
    smokeCheck("saved theme reverted from file",
        isObject(SmokeTheme) && SmokeThemeButtonProfile.fillColor $= "9 8 7 6");

    // With editorMode restored, a new object's name is shadowed and must not
    // land in the real name dictionary.
    %probe = new ScriptObject(SmokeModeProbe);
    smokeCheck("editorMode restored after close", !isObject(SmokeModeProbe));
    %probe.delete();

    %file = pathConcat(getMainDotCsDir(), "smokeThemeProject", "themes", "SmokeTheme.taml");
    if(isFile(%file))
    {
        fileDelete(%file);
    }

    echo("SMOKE DONE");
    schedule(250, 0, "quit");
}
