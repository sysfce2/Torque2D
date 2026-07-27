//-----------------------------------------------------------------------------
// Pass 1 of the portable-bitmap-path check: build a stand-alone profile through
// the real Profile Editor, point it at an image with an ABSOLUTE path (what a
// file dialog hands back), and save. Pass 2 (bitmapPathRead.cs) boots
// fresh and renders what this wrote.
//-----------------------------------------------------------------------------

setLogMode(2);
setScriptExecEcho(false);
trace(false);
$Scripts::ignoreDSOs = true;
setCompanyAndProduct("Torque Game Engines", "Torque2D");
ModuleDatabase.EchoInfo = false;
AssetDatabase.EchoInfo = false;

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

schedule(2500, 0, "writeStep1");

function writeStep1()
{
    ProjectManager.setProjectFolder("smokeThemeProject");
    GuiEditor.open();
    GuiEditor.openProfileEditor();

    %dialog = GuiEditor.profileEditorDialog;
    %dialog.onNewStandalone();
    %nameDialog = %dialog.childDialog;
    %nameDialog.nameBox.setText("IronTest");
    %nameDialog.onDone();

    %profile = %dialog.library.findProfileByName("IronTest");
    smokeCheck("profile created", isObject(%profile));

    %proxy = %dialog.library.standaloneFolder.getObject(0);
    %dialog.onTreeSelect(%proxy);

    %drop = %dialog.profileForm.categoryDrop;
    %drop.setSelected(%drop.findItemText("Panel", false));

    // The bitmap row must now be a "file" row: a box plus a Find button.
    %row = %dialog.profileForm.row["bitmap"];
    smokeCheck("bitmap row exists", isObject(%row));
    smokeCheck("bitmap row is a file row", %row.kind $= "file");
    smokeCheck("bitmap row has a Find button", isObject(%row.findButton));

    // The text boxes must no longer hijack the click to select everything.
    smokeCheck("row input does not override onTouchDown", !%row.editor.isMethod("onTouchDown"));

    // Feed it exactly what a file dialog returns: an absolute path.
    %absolute = makeFullPath("toybox/themes/image/ironWindow.png", getMainDotCsDir());
    echo("REPRO: handing the field " @ %absolute);
    %row.editor.setText(%absolute);
    %row.commit();

    // The field reads back relative to the game root, which is what gets saved.
    echo("REPRO: field reads back as " @ %profile.bitmap);
    smokeCheck("field reads back relative",
        %profile.bitmap $= "toybox/themes/image/ironWindow.png");

    schedule(600, 0, "writeStep2");
}

function writeStep2()
{
    GuiEditor.profileEditorDialog.onSave();
    schedule(1000, 0, "writeStep3");
}

function writeStep3()
{
    smokeCheck("dialog closed after save", !isObject(GuiEditor.profileEditorDialog));
    echo("SMOKE DONE");
    schedule(250, 0, "quit");
}
