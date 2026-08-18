//-----------------------------------------------------------------------------
// Pass 2: boot fresh, let the Profile Editor's library read back what pass 1
// saved, and put a control on screen wearing it. If the saved path were the
// absolute one, this would still pass here - it is the same machine - so the
// check that matters is the text of the file, done outside the engine.
// What this proves is that the relative form loads and renders.
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

createPath(testRoot("shots/"));
schedule(2500, 0, "readStep1");

function readStep1()
{
    ProjectManager.setProjectFolder("smokeThemeProject");
    GuiEditor.open();
    GuiEditor.openProfileEditor();

    %profile = GuiEditor.themeLibrary.findProfileByName("IronTest");
    smokeCheck("saved profile read back", isObject(%profile));
    echo("REPRO: loaded field = " @ %profile.bitmap);

    GuiEditor.profileEditorDialog.onCancel();
    schedule(600, 0, "readStep2");
}

function readStep2()
{
    %stage = new GuiControl()
    {
        Position = "0 0";
        Extent = "1024 768";
    };
    ThemeManager.setProfile(%stage, "overlayProfile");

    %stage.add(new GuiControl()
    {
        Position = "120 160";
        Extent = "300 240";
        Text = "skinned by IronTest";
        Profile = IronTest;
    });

    // A second one at a different size, so a nine-slice shows it is stretching
    // the middle rather than the corners.
    %stage.add(new GuiControl()
    {
        Position = "500 160";
        Extent = "420 120";
        Text = "wider";
        Profile = IronTest;
    });

    Canvas.pushDialog(%stage);
    schedule(700, 0, "readStep3");
}

function readStep3()
{
    screenShot(testRoot("shots/bitmapSkinned.png"), "PNG");
    echo("SMOKE DONE");
    schedule(400, 0, "quit");
}
