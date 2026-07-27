//-----------------------------------------------------------------------------
// Does clicking a Profile Editor text box put the caret where you clicked?
// Driven by textClick.input.ps1, which posts a real WM_LBUTTONDOWN/UP into the
// middle of the box, so the whole engine input path is exercised.
//
// Old behaviour: GuiProfileEditorRowInput::onTouchDown re-selected everything
// after the engine had placed the caret, which left the caret at the END of the
// text and the selection anchored at the start.
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

$clickText = "toybox/themes/image/ironWindow.png and then some more text";

schedule(2500, 0, "clickStep1");

function clickStep1()
{
    // The Gui Editor owns the profiles the row wears.
    GuiEditor.open();

    %stage = new GuiControl()
    {
        Position = "0 0";
        Extent = "1024 768";
    };
    ThemeManager.setProfile(%stage, "overlayProfile");

    $box = new GuiTextEditCtrl()
    {
        class = "GuiProfileEditorRowInput";
        Position = "200 200";
        Extent = "500 30";
        Text = $clickText;
    };
    ThemeManager.setProfile($box, "textEditProfile");
    %stage.add($box);
    Canvas.pushDialog(%stage);

    smokeCheck("no onTouchDown override on the row input", !$box.isMethod("onTouchDown"));
    echo("CLICKTEST: text length = " @ strlen($clickText));

    // The driver posts its click during this window.
    schedule(6000, 0, "clickStep2");
}

function clickStep2()
{
    %pos = $box.getCursorPos();
    %len = strlen($clickText);
    echo("CLICKTEST: cursor landed at " @ %pos @ " of " @ %len);

    // Clicking ~50px in should land a few characters in: not at the start, and
    // nowhere near the end, which is where a select-all would have parked it.
    smokeCheck("caret moved off the start", %pos > 0);
    smokeCheck("caret did not jump to the end (select-all)", %pos < (%len - 5));

    screenShot(testRoot("shots/textClick.png"), "PNG");
    echo("SMOKE DONE");
    schedule(400, 0, "quit");
}
