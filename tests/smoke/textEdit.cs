//-----------------------------------------------------------------------------
// The multi-line text box, at the engine level. Driven by textEdit.input.ps1,
// which posts a real click and real Return/Up keys, so the whole input path
// runs rather than the methods behind it.
//
// Two behaviours, both of them GuiControl::getLineList's doing:
//
//   A. How many lines a piece of text is. getLineList built its paragraph list
//      with getline, which cannot tell an empty string from no string -- it
//      fails immediately on "" -- so empty text produced NO lines at all, and a
//      line block is what draws a GuiTextEditCtrl's caret. An empty multi-line
//      box therefore had no cursor in it. getline also dropped the empty
//      paragraph that a trailing newline makes, so pressing return at the end
//      of the text left the caret with no line to sit on.
//
//      Measured through textExtend, which sizes a control from
//      textHeight * lineList.size() -- the only place the line count is visible
//      from script.
//
//   B. Return puts a line break in a wrapped box rather than ending the edit,
//      and the up arrow then moves the caret to the line above.
//-----------------------------------------------------------------------------

setLogMode(2);
setScriptExecEcho(false);
trace(false);
$Scripts::ignoreDSOs = true;
setCompanyAndProduct("Torque Game Engines", "Torque2D");
ModuleDatabase.EchoInfo = false;
AssetDatabase.EchoInfo = false;

testExec("editor/main.cs");

function teCheck(%label, %condition)
{
    if(%condition) echo("TESMOKE PASS: " @ %label);
    else           echo("TESMOKE FAIL: " @ %label);
}

// A wrapped, extending control holding %text. Its height after a frame is
// one line per line of text, which is what makes the line count observable.
function teProbe(%stage, %x, %text)
{
    %probe = new GuiControl()
    {
        Position = %x SPC 400;
        Extent = "180 20";
        textWrap = true;
        textExtend = true;
        Text = %text;
    };
    ThemeManager.setProfile(%probe, "labelProfile");
    %stage.add(%probe);
    return %probe;
}

$teText = "abc def";

createPath(testRoot("shots/"));
schedule(2500, 0, "teStep1");

function teStep1()
{
    // The Gui Editor owns the profiles these wear.
    GuiEditor.open();

    %stage = new GuiControl()
    {
        Position = "0 0";
        Extent = "1024 768";
    };
    ThemeManager.setProfile(%stage, "overlayProfile");

    // vAlign top so the first line sits at the top of the content rect. Left to
    // the profile it is centred, and a click above the line reads as position 0
    // (calculateIbeamPosition returns 0 for a point above the first block) --
    // which looks exactly like a click that missed.
    $teBox = new GuiTextEditCtrl()
    {
        Position = "200 200";
        Extent = "500 90";
        textWrap = true;
        vAlign = "top";
        Text = $teText;
    };
    ThemeManager.setProfile($teBox, "textEditProfile");
    %stage.add($teBox);

    // One line each, two lines, and two lines where the second is empty.
    $teEmpty = teProbe(%stage, 100, "");
    $teOneLine = teProbe(%stage, 300, "x");
    $teTwoLines = teProbe(%stage, 500, "x" NL "y");
    $teTrailing = teProbe(%stage, 700, "x" NL "");

    Canvas.pushDialog(%stage);

    // Pushing a dialog hands the first responder to the box, and taking focus
    // selects the whole text -- which leaves the caret at the end, exactly where
    // a successful click would leave it. Give it back so that a caret at the end
    // later can only be the click's doing.
    $teBox.makeFirstResponder(false);
    $teBox.setCursorPos(0);

    // The driver clicks into the box during this window. The windows are wide
    // on purpose: the driver sleeps against the wall clock while these run
    // against a boot that takes longer when the whole suite is running, and a
    // key that arrives before its step has been mistaken for a broken engine
    // once already.
    schedule(4500, 0, "teStep2");
}

//-----------------------------------------------------------------------------
// A. The line count.
//-----------------------------------------------------------------------------

function teStep2()
{
    %empty = getWord($teEmpty.getExtent(), 1);
    %one = getWord($teOneLine.getExtent(), 1);
    %two = getWord($teTwoLines.getExtent(), 1);
    %trailing = getWord($teTrailing.getExtent(), 1);

    echo("TESMOKE: heights empty=" @ %empty @ " one=" @ %one @
         " two=" @ %two @ " trailing=" @ %trailing);

    // The one that was broken: empty text is one (blank) line, not none.
    teCheck("empty text is one line, like a one-word line", %empty == %one);
    teCheck("two paragraphs are taller than one", %two > %one);
    teCheck("a trailing newline keeps its empty line", %trailing == %two);

    // The driver clicks past the end of the text, so the engine's own hit test
    // parks the caret at the end -- which is where Return has to be for it to
    // make a new empty line rather than split the text in two. Placed by the
    // click rather than by setCursorPos here, so that nothing depends on this
    // step running before the key arrives.
    echo("TESMOKE: caret at " @ $teBox.getCursorPos() @ " of " @ strlen($teText));
    teCheck("the box took the click and the caret is at the end",
        $teBox.getCursorPos() == strlen($teText));

    // The driver presses Return during this window.
    schedule(4000, 0, "teStep3");
}

//-----------------------------------------------------------------------------
// B. Return, and then the up arrow.
//-----------------------------------------------------------------------------

function teStep3()
{
    %text = $teBox.getText();
    echo("TESMOKE: after return, length " @ strlen(%text) @
         " cursor " @ $teBox.getCursorPos());

    teCheck("return added a character", strlen(%text) == (strlen($teText) + 1));
    teCheck("and the character is a line break", %text $= ($teText NL ""));
    teCheck("the caret moved onto the new line",
        $teBox.getCursorPos() == strlen($teText) + 1);

    // Whether the box kept the focus is answered in teStep4: a control that
    // ended its edit on return would not be there to receive the up arrow.
    $teCursorAfterReturn = $teBox.getCursorPos();

    // The driver presses Up during this window.
    schedule(4000, 0, "teStep4");
}

function teStep4()
{
    %pos = $teBox.getCursorPos();
    echo("TESMOKE: after up arrow, cursor " @ %pos @
         " (was " @ $teCursorAfterReturn @ ")");

    // Up moves the caret to the line above. Without this the key was swallowed
    // by a script onUpArrow that had nothing to do with a text box.
    teCheck("the up arrow moved the caret off the new line",
        %pos < $teCursorAfterReturn);

    // The same fact, read the other way: the key could only arrive because
    // return left the box holding the focus instead of ending the edit.
    teCheck("so return did not end the edit", %pos != $teCursorAfterReturn);

    screenShot(testRoot("shots/textEdit.png"), "PNG");
    echo("TESMOKE DONE");
    schedule(400, 0, "quit");
}
