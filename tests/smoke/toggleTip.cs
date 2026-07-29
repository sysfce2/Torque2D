//-----------------------------------------------------------------------------
// Two-line tooltips on the properties pane's toggle icons. Driven by
// toggleTip.input.ps1, which hovers a real mouse over one of them long enough
// for the tip to appear, so what is screenshotted is what a user would see.
//
// A toggle says what it is and which way it is set on the first line, and what
// that means on the second:
//
//     Visible - On
//     Draws when the game runs...
//
// Which needed the tooltip renderer taught about line breaks: it does its own
// word wrapping, splitting on spaces alone, so a newline used to be part of a
// word -- and once GFont stopped drawing a glyph for it, the two lines would
// have run together into one.
//-----------------------------------------------------------------------------

setLogMode(2);
setScriptExecEcho(false);
trace(false);
$Scripts::ignoreDSOs = true;
setCompanyAndProduct("Torque Game Engines", "Torque2D");
ModuleDatabase.EchoInfo = false;
AssetDatabase.EchoInfo = false;

testExec("editor/main.cs");

function ttCheck(%label, %condition)
{
    if(%condition) echo("TTSMOKE PASS: " @ %label);
    else           echo("TTSMOKE FAIL: " @ %label);
}

createPath(testRoot("shots/"));
schedule(2500, 0, "ttOpenProject");

// A tip has to be hovered to be seen, so unlike the other pane suites this one
// needs the editor actually on screen: a real project, loaded the way the
// selector loads it, and then the editor brought up the way ctrl+~ does.
function ttOpenProject()
{
    ProjectManager.setProjectFolder("PlanetX");
    EditorCore.projectSelector.onProjectSelected(pathConcat(getMainDotCsDir(), "PlanetX"));
    schedule(2500, 0, "ttOpenEditor");
}

function ttOpenEditor()
{
    EditorCore.toggleEditor();
    EditorCore.tabBook.selectPage(3);
    schedule(1500, 0, "ttStep1");
}

function ttStep1()
{
    $ttTheme = GuiEditor.themeLibrary.createTheme("TTSmoke");
    GuiEditor.themeName = $ttTheme.getName();

    $ttCtrl = new GuiControl()
    {
        Position = "40 40";
        Extent = "200 60";
    };
    GuiEditor.rootGui.add($ttCtrl);
    GuiEditor.themeApplier.applyToBranch($ttCtrl, $ttTheme, true);

    %pane = GuiEditor.inspectorWindow.pane;
    %pane.bind($ttCtrl);

    // --- The format, which needs no mouse. ---
    %visible = %pane.header.visibleButton;
    %tip = %visible.Tooltip;
    echo("TTSMOKE: visible tip = [" @ %tip @ "]");

    ttCheck("the tip leads with the name and the state",
        getRecord(%tip, 0) $= "Visible - On");
    ttCheck("and explains that state underneath",
        strstr(getRecord(%tip, 1), "Draws when the game runs") >= 0);
    ttCheck("it is two lines, not one", getRecordCount(%tip) == 2);

    // Turning it off rewrites both lines.
    %visible.setValue(false);
    echo("TTSMOKE: off tip = [" @ %visible.Tooltip @ "]");
    ttCheck("off says off", getRecord(%visible.Tooltip, 0) $= "Visible - Off");
    ttCheck("with the other explanation",
        strstr(getRecord(%visible.Tooltip, 1), "does not draw") >= 0 ||
        strstr(getRecord(%visible.Tooltip, 1), "Does not draw") >= 0);
    %visible.setValue(true);

    // A segmented row's buttons are choices rather than switches, so they keep
    // the one line they had -- "Centre text - On" would be a worse caption.
    %align = %pane.header.alignRow;
    ttCheck("a choice button has no On/Off heading",
        getRecordCount(%align.choiceButton[2].Tooltip) == 1);

    // The driver hovers the Visible toggle during this window; the tip is
    // screenshotted so the two lines can be seen to actually render.
    schedule(6000, 0, "ttStep2");
}

function ttStep2()
{
    screenShot(testRoot("shots/toggleTip.png"), "PNG");
    echo("TTSMOKE DONE");
    schedule(400, 0, "quit");
}
