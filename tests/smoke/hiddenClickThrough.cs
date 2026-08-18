//-----------------------------------------------------------------------------
// Clicking through a control the Gui Editor's eye has hidden.
//
// The rule itself is unit tested (guiHitTestTests.cc) against a GuiEditCtrl the
// test builds and points the statics at. What that cannot check is the wiring:
// that the editor as it really boots has the Gui under edit inside its edit
// root, so isEditMode is true for the controls on the canvas and the eye means
// anything at all. Get that wrong and every unit test still passes while the
// feature does nothing.
//
// So this asks the same question of the real editor, through the same entry
// point the mouse uses -- GuiControl::findHitControl, via its script binding.
// The unhidden answers are asserted first: they are what make the hidden ones
// mean something rather than just meaning the coordinates were wrong.
//-----------------------------------------------------------------------------

setLogMode(1);
setScriptExecEcho(false);
trace(false);
$Scripts::ignoreDSOs = true;
setCompanyAndProduct("Torque Game Engines", "Torque2D");
ModuleDatabase.EchoInfo = false;
AssetDatabase.EchoInfo = false;
AssetDatabase.IgnoreAutoUnload = true;

testExec("editor/main.cs");

$Pass = 0;
$Fail = 0;

function hctCheck(%label, %condition)
{
    if(%condition)
    {
        $Pass++;
        echo("HCT PASS: " @ %label);
    }
    else
    {
        $Fail++;
        echo("HCT FAIL: " @ %label);
    }
}

schedule(2500, 0, "hctOpenProject");

// The long way in. GuiEditor.open() would be enough for a suite that only calls
// methods on it, but the thing under test is whether the Gui being edited is
// really inside the editor's edit root -- so the editor has to be brought up the
// way it comes up for a person.
function hctOpenProject()
{
    ProjectManager.setProjectFolder("PlanetX");
    EditorCore.projectSelector.onProjectSelected(pathConcat(getMainDotCsDir(), "PlanetX"));
    schedule(2500, 0, "hctOpenEditor");
}

// Pages register in load order: EditorConsole, ProjectManager, AssetAdmin,
// GuiEditor.
function hctOpenEditor()
{
    EditorCore.toggleEditor();
    EditorCore.tabBook.selectPage(3);
    schedule(1500, 0, "hctClear");
}

// Start from an empty Gui, in a step of its own: PlanetX finishes displaying its
// own Gui after the editor page comes up, so clearing in the same frame as the
// build leaves its controls on the canvas and in the way of every point below.
function hctClear()
{
    GuiEditor.NewGui();
    discardUnsavedPrompt();
    schedule(500, 0, "hctBuild");
}

// One back panel, a smaller front panel on top of it, and a control deeper still
// inside the front one. Later children are drawn last and hit first, so "front"
// really is in front.
//
//   rootGui  0,0     the simulated canvas
//    back    100,100  400x300     -> 100..499, 100..399
//    front   200,150  200x100     -> 200..399, 150..249
//     deep    20,20    60x40      -> 220..279, 170..209 in rootGui's space
//
// Sizing is nailed down rather than left to the default, because a mode that
// computes its position from the parent would move these out from under the
// points below the moment they were added.
function hctBuild()
{
    %root = GuiEditor.rootGui;

    $hctBack = new GuiControl()
    {
        Position = "100 100";
        Extent = "400 300";
        HorizSizing = "right";
        VertSizing = "bottom";
    };
    %root.add($hctBack);

    $hctFront = new GuiControl()
    {
        Position = "200 150";
        Extent = "200 100";
        HorizSizing = "right";
        VertSizing = "bottom";
    };
    %root.add($hctFront);

    $hctDeep = new GuiControl()
    {
        Position = "20 20";
        Extent = "60 40";
        HorizSizing = "right";
        VertSizing = "bottom";
    };
    $hctFront.add($hctDeep);

    // A frame for the canvas to lay everything out and render once, so the
    // render insets findHitControl subtracts are the real ones.
    schedule(500, 0, "hctRun");
}

// Over front, clear of deep.
function hctOverFront()
{
    return "380 240";
}

// Over deep, and so over front and back as well.
function hctOverDeep()
{
    return "250 190";
}

function hctHit(%point)
{
    return GuiEditor.rootGui.findHitControl(getWord(%point, 0), getWord(%point, 1));
}

function hctRun()
{
    // --- The baseline. If these fail, nothing below is about hiding.
    hctCheck("the front panel is hit where it covers the back one",
             hctHit(hctOverFront()) == $hctFront.getId());
    hctCheck("and its child is hit where the child is",
             hctHit(hctOverDeep()) == $hctDeep.getId());
    hctCheck("the back panel is hit where nothing covers it",
             hctHit("120 120") == $hctBack.getId());

    // --- The whole point.
    $hctFront.hidden = true;

    hctCheck("hiding the front panel lets the click reach the back one",
             hctHit(hctOverFront()) == $hctBack.getId());
    hctCheck("and takes its children with it",
             hctHit(hctOverDeep()) == $hctBack.getId());

    // --- It is out of the way, not gone: the Explorer is the only way back.
    %tree = GuiEditor.explorerWindow.tree;
    %tree.refresh();
    hctCheck("a hidden control still has a row in the Explorer",
             %tree.findItemID($hctFront.getId()) > 0);
    hctCheck("and still answers as hidden",
             $hctFront.hidden);

    // --- And back.
    $hctFront.hidden = false;

    hctCheck("showing it again makes it a target again",
             hctHit(hctOverFront()) == $hctFront.getId());
    hctCheck("and its child with it",
             hctHit(hctOverDeep()) == $hctDeep.getId());

    // --- One control, not the branch it sits in.
    $hctDeep.hidden = true;

    hctCheck("hiding a child leaves its parent hit-testable",
             hctHit(hctOverDeep()) == $hctFront.getId());
    hctCheck("and the parent is still hit where it always was",
             hctHit(hctOverFront()) == $hctFront.getId());

    echo("HCT RESULT: " @ $Pass @ " passed, " @ $Fail @ " failed");
    quit();
}
