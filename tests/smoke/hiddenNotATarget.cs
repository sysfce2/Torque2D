//-----------------------------------------------------------------------------
// The three ways a hidden control could still be picked up, none of which goes
// through findHitControl.
//
// hiddenClickThrough.cs asks findHitControl directly, which is the rule itself.
// These two get in front of it:
//
// 1. The sizing knobs. GuiEditCtrl::onTouchDown tests them BEFORE it hit tests
//    anything, and clicking the eye deliberately does not move the selection --
//    so a control hidden while selected would keep eight invisible grab zones
//    straddling its edges, which is exactly where someone aiming at what is
//    behind it would click. editGeometryFrozen is what closes that.
//
// 2. The container being worked in. It is where a palette click places the next
//    control, and it is not chosen by hit testing at all, so hiding it would
//    leave the next control placed inside a hidden parent, appearing nowhere.
//    GuiEditCtrl::controlHidden is what closes that.
//
// 3. The rubber band. It walks the children of the container being worked in
//    rather than hit testing them, so it would sweep up a control that is not
//    drawn along with the ones that are.
//
// All three need real input: the knob test lives in the middle of the touch
// path, only the Explorer's gutter click calls controlHidden, and a band needs a
// press, a run of moves and a release. Driving any of them from script would
// skip the code under test. hiddenNotATarget.input.ps1 posts them.
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

function hntCheck(%label, %condition)
{
    if(%condition)
    {
        $Pass++;
        echo("HNT PASS: " @ %label);
    }
    else
    {
        $Fail++;
        echo("HNT FAIL: " @ %label);
    }
}

// The engine works out where to click and leaves the point for the PowerShell
// side, exactly as explorerGutter.cs does: a hard-coded coordinate that drifted
// off its target would report a control that never reacted, which is what a
// broken hit test reports too.
function hntPostTarget(%point)
{
    createPath(testRoot("shots/"));
    %file = new FileObject();
    %file.openForWrite(testRoot("shots/hiddenNotATargetTarget.txt"));
    %file.writeLine(%point);
    %file.close();
    %file.delete();
}

schedule(2500, 0, "hntOpenProject");

function hntOpenProject()
{
    ProjectManager.setProjectFolder("PlanetX");
    EditorCore.projectSelector.onProjectSelected(pathConcat(getMainDotCsDir(), "PlanetX"));
    schedule(2500, 0, "hntOpenEditor");
}

// Pages register in load order: EditorConsole, ProjectManager, AssetAdmin,
// GuiEditor.
function hntOpenEditor()
{
    EditorCore.toggleEditor();
    EditorCore.tabBook.selectPage(3);
    schedule(1500, 0, "hntClear");
}

function hntClear()
{
    GuiEditor.NewGui();
    discardUnsavedPrompt();
    schedule(500, 0, "hntBuild");
}

//   rootGui  the simulated canvas
//    back    100,100  400x300
//    front   200,150  200x100     -- selected, then hidden
//
// front's top left corner sits well inside back, so the click below is both on
// front's top-left sizing knob and over back. Before the fix it resized front.
function hntBuild()
{
    %root = GuiEditor.rootGui;

    $hntBack = new GuiControl()
    {
        Position = "100 100";
        Extent = "400 300";
        HorizSizing = "right";
        VertSizing = "bottom";
    };
    %root.add($hntBack);

    $hntFront = new GuiControl()
    {
        Position = "200 150";
        Extent = "200 100";
        HorizSizing = "right";
        VertSizing = "bottom";
    };
    %root.add($hntFront);

    // A frame to lay out and render, so the global positions read below are the
    // ones the mouse would land on.
    schedule(500, 0, "hntKnobs");
}

function hntKnobs()
{
    GuiEditor.brain.select($hntFront);
    $hntFront.hidden = true;

    %sel = GuiEditor.brain.getSelected();
    hntCheck("the hidden control is the one selected",
             %sel.getCount() == 1 && %sel.getObject(0) == $hntFront.getId());

    echo("HNT: canvas at " @ GuiEditor.rootGui.getGlobalPosition() @
         " extent " @ GuiEditor.rootGui.getExtent());
    echo("HNT: front's top-left knob at " @ $hntFront.getGlobalPosition());

    // Its top-left knob: dead centre of the eight, and the one furthest from
    // anything else that could claim the press.
    hntPostTarget($hntFront.getGlobalPosition());

    schedule(9000, 0, "hntAfterKnobClick");
}

function hntAfterKnobClick()
{
    %sel = GuiEditor.brain.getSelected();

    hntCheck("clicking a hidden control's sizing knob selects what is behind it",
             %sel.getCount() == 1 && %sel.getObject(0) == $hntBack.getId());
    hntCheck("and did not resize the hidden control",
             $hntFront.getExtent() $= "200 100");
    hntCheck("nor move it", $hntFront.getPosition() $= "200 150");

    schedule(200, 0, "hntAddSet");
}

// Now the container being worked in. Hiding it has to put the add set back
// somewhere that is still drawn, or the next control placed lands inside it.
function hntAddSet()
{
    $hntBox = new GuiControl()
    {
        Position = "520 100";
        Extent = "150 150";
        HorizSizing = "right";
        VertSizing = "bottom";
    };
    GuiEditor.rootGui.add($hntBox);

    %tree = GuiEditor.explorerWindow.tree;
    %tree.refresh();

    GuiEditor.brain.setCurrentAddSet($hntBox);
    hntCheck("the container being worked in is the one about to be hidden",
             GuiEditor.brain.getCurrentAddSet() == $hntBox.getId());

    $hntBoxIndex = %tree.findItemID($hntBox.getId());
    hntCheck("it has a row in the Explorer", $hntBoxIndex > 0);

    echo("HNT: its eye box at " @ %tree.getGutterPoint($hntBoxIndex, "hidden"));
    hntPostTarget(%tree.getGutterPoint($hntBoxIndex, "hidden"));

    schedule(9000, 0, "hntAfterEyeClick");
}

function hntAfterEyeClick()
{
    hntCheck("clicking the eye hid the container", $hntBox.hidden);
    hntCheck("and came back out of it, to what still draws",
             GuiEditor.brain.getCurrentAddSet() == GuiEditor.rootGui.getId());

    schedule(200, 0, "hntBand");
}

// The third path that does not hit test: a rubber band walks the children of the
// container being worked in and takes whatever it encloses.
//
// Both of these have to sit inside the canvas frame, which is a good deal
// smaller than the Gui it is showing -- the band is posted in window
// coordinates, and a point past the frame's edge would land on a tool window
// instead. The band starts clear of every control, or the press would be read as
// selecting one rather than as beginning a band.
function hntBand()
{
    // Said outright rather than inherited from the step above. A band only ever
    // looks at the children of the container being worked in, so a phase that
    // took the add set on trust would test nothing at all the moment the step
    // before it went wrong -- which is precisely how it would go wrong.
    GuiEditor.brain.setCurrentAddSet(GuiEditor.rootGui);
    GuiEditor.brain.clearSelection();

    $hntBandShown = new GuiControl()
    {
        Position = "20 400";
        Extent = "100 60";
        HorizSizing = "right";
        VertSizing = "bottom";
    };
    GuiEditor.rootGui.add($hntBandShown);

    $hntBandHidden = new GuiControl()
    {
        Position = "140 400";
        Extent = "100 60";
        HorizSizing = "right";
        VertSizing = "bottom";
    };
    GuiEditor.rootGui.add($hntBandHidden);
    $hntBandHidden.hidden = true;

    %origin = GuiEditor.rootGui.getGlobalPosition();
    %x = getWord(%origin, 0);
    %y = getWord(%origin, 1);

    %band = (%x + 10) SPC (%y + 380) SPC (%x + 280) SPC (%y + 480);
    echo("HNT: band " @ %band);
    hntPostTarget(%band);

    schedule(12000, 0, "hntAfterBand");
}

function hntAfterBand()
{
    %sel = GuiEditor.brain.getSelected();

    // The band has to have done something, or the two checks below would pass on
    // a gesture that never happened.
    hntCheck("the band selected what it enclosed", %sel.getCount() > 0);
    hntCheck("it took the control that is drawn",
             %sel.isMember($hntBandShown));
    hntCheck("and left the hidden one where a band cannot reach it",
             !%sel.isMember($hntBandHidden));

    echo("HNT RESULT: " @ $Pass @ " passed, " @ $Fail @ " failed");
    quit();
}
