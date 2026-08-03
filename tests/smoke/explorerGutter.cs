//-----------------------------------------------------------------------------
// The Explorer tree's two columns of editor state -- the eye and the padlock.
//
// The geometry is unit tested (guiTreeRowLayoutTests.cc): where the columns are,
// which one an x falls in, where the box sits in a cell. None of that needs a
// canvas and none of it is repeated here.
//
// What IS here is the part no unit test can reach. Adding a row to a tree loads
// a font, which registers a texture, which asserts with no GL context -- so
// anything involving real rows has to run in a real engine. And the central
// promise of the feature is a negative: clicking a box toggles the flag and does
// NOT change the selection. Calling the toggle directly would skip the very code
// that could break that, so the click itself is posted for real, by
// explorerGutter.input.ps1.
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

function gutCheck(%label, %condition)
{
    if(%condition)
    {
        $Pass++;
        echo("EGUT PASS: " @ %label);
    }
    else
    {
        $Fail++;
        echo("EGUT FAIL: " @ %label);
    }
}

// The engine works out where a box actually landed and leaves the point for the
// PowerShell side to click. A hard-coded coordinate that drifted off the box
// would report a missing item, which is exactly what a broken hit test reports.
function gutPostTarget(%point)
{
    createPath(testRoot("shots/"));
    %file = new FileObject();
    %file.openForWrite(testRoot("shots/explorerGutterTarget.txt"));
    %file.writeLine(%point);
    %file.close();
    %file.delete();
}

schedule(2500, 0, "gutOpenProject");

// The long way in, and it has to be. GuiEditor.open() is enough for a suite that
// only calls methods, but it does not put the editor on the canvas -- and a
// posted click goes to whatever is actually on screen. Clicks against an editor
// opened the short way land on the project selector and do nothing at all, which
// reads exactly like a broken hit test.
function gutOpenProject()
{
    ProjectManager.setProjectFolder("PlanetX");
    EditorCore.projectSelector.onProjectSelected(pathConcat(getMainDotCsDir(), "PlanetX"));
    schedule(2500, 0, "gutOpenEditor");
}

// Pages register in load order: EditorConsole, ProjectManager, AssetAdmin,
// GuiEditor.
function gutOpenEditor()
{
    EditorCore.toggleEditor();
    EditorCore.tabBook.selectPage(3);
    schedule(1500, 0, "gutClear");
}

// Start from an empty Gui, in a step of its own: PlanetX finishes displaying
// its own Gui after the editor page comes up, so clearing in the same frame as
// the build leaves its controls in the tree and moves every row.
function gutClear()
{
    GuiEditor.NewGui();
    schedule(500, 0, "gutRun");
}

function gutRun()
{
    %tree = GuiEditor.explorerWindow.tree;

    // --- The class swap itself. Both halves matter: GuiEditor.cs and the theme
    // applier both branch on isMemberOfClass("GuiTreeViewCtrl"), so a tree that
    // stopped being one would break things nowhere near here.
    gutCheck("the Explorer wears the editor tree class",
             %tree.getClassName() $= "GuiEditorExplorerTree");
    gutCheck("and is still a tree view", %tree.isMemberOfClass("GuiTreeViewCtrl"));

    // --- Editor-only: the palette must refuse it, and BOTH copies of that rule
    // have to agree. They are separately maintained -- one generated, one typed
    // -- and this is the assertion that catches them drifting apart.
    gutCheck("the palette refuses the editor tree",
             !GuiEditor.controlIcons.isPlaceableClass("GuiEditorExplorerTree"));
    gutCheck("and the spec's copy of the rule agrees",
             !GuiEditor.inspectorWindow.pane.spec.isPlaceableClass("GuiEditorExplorerTree"));

    // --- The columns answer where they say they do. gutterColumnAt is the same
    // function the paint uses, so this is the contract between them.
    %w = %tree.getGutterColumnWidth();
    gutCheck("a column is a sensible width", %w > 0 && %w < 64);
    gutCheck("the first column is the eye", %tree.gutterColumnAt(0) $= "hidden");
    gutCheck("the second is the padlock", %tree.gutterColumnAt(%w) $= "locked");
    gutCheck("past both is the tree itself", %tree.gutterColumnAt(2 * %w) $= "");

    // --- Now a real control to toggle.
    %ctrl = new GuiButtonCtrl();
    GuiEditor.rootGui.add(%ctrl);
    %ctrl.setInternalName("gutTarget");
    %theme = GuiEditor.themeByName(GuiEditor.themeName);
    if(isObject(%theme))
    {
        GuiEditor.themeApplier.applyToBranch(%ctrl, %theme, false);
    }
    %tree.refresh();

    $gutCtrl = %ctrl;
    $gutIndex = %tree.findItemID(%ctrl.getId());
    gutCheck("the control has a row", $gutIndex > 0);

    gutCheck("it starts shown", !%ctrl.hidden);
    gutCheck("and unlocked", !%ctrl.locked);

    // --- Select something ELSE, so "the click did not change the selection" is
    // a claim with something to lose. Selecting the target itself would pass
    // whether the click preserved the selection or set it.
    %tree.clearSelection();
    %tree.setSelected(0, true);
    $gutSelected = %tree.getSelectedItem();
    gutCheck("something else is selected to start with", $gutSelected != $gutIndex);

    echo("EGUT: tree extent " @ %tree.getExtent() @ ", " @ %tree.getItemCount() @ " rows");

    // --- The row icons. The frame is cached on the row when the tree builds, so
    // what is checked is that the cache holds the right answer and that it is
    // re-asked when the answer changes.
    %icons = GuiEditor.controlIcons;
    gutCheck("the tree draws from the 16px sheet",
             %tree.IconImage $= "GuiEditor:controlIcons16");
    gutCheck("the root row wears no picture", %tree.getItemIcon(0) == -1);
    gutCheck("a button row wears the button",
             %tree.getItemIcon($gutIndex) == %icons.frameFor("GuiButtonCtrl"));

    // A bare GuiControl is four palette entries sharing one class, told apart by
    // the category it wears -- so the class alone cannot pick its picture. This
    // is the case keyFor exists for.
    %bare = new GuiControl();
    GuiEditor.rootGui.add(%bare);
    %bare.setInternalName("gutBare");
    if(isObject(%theme))
    {
        GuiEditor.themeApplier.applyToBranch(%bare, %theme, false);
    }
    %tree.refresh();

    %bareIndex = %tree.findItemID(%bare.getId());
    %pane = GuiEditor.inspectorWindow.pane;
    %wasCategory = %pane.currentCategory(%bare);
    gutCheck("a bare GuiControl wears the face for its category",
             %tree.getItemIcon(%bareIndex) ==
             %icons.frameFor("GuiControl:" @ %wasCategory));

    // Change the face, and the row must follow. This is the invalidation path:
    // the Category picker rewrites the Profile, which commits, which posts
    // PostApply, which is what re-asks this one row.
    %newCategory = (%wasCategory $= "Label") ? "Panel" : "Label";
    %pane.bind(%bare);
    %pane.setCategory(%newCategory);
    gutCheck("changing the category changed the row's picture",
             %tree.getItemIcon(%tree.findItemID(%bare.getId())) ==
             %icons.frameFor("GuiControl:" @ %newCategory));
    gutCheck("and it really is a different picture",
             %icons.frameFor("GuiControl:" @ %newCategory) !=
             %icons.frameFor("GuiControl:" @ %wasCategory));

    %bare.delete();
    %tree.refresh();
    $gutIndex = %tree.findItemID($gutCtrl.getId());
    %tree.clearSelection();
    %tree.setSelected(0, true);
    $gutSelected = %tree.getSelectedItem();

    // Hand the eye box's center to the PowerShell side.
    gutPostTarget(%tree.getGutterPoint($gutIndex, "hidden"));
    echo("EGUT: eye box of row " @ $gutIndex @ " at " @
         %tree.getGutterPoint($gutIndex, "hidden"));

    schedule(9000, 0, "gutAfterEyeClick");
}

function gutAfterEyeClick()
{
    %tree = GuiEditor.explorerWindow.tree;

    // --- The whole point of the feature.
    gutCheck("clicking the eye hid the control", $gutCtrl.hidden);
    gutCheck("and did not move the selection", %tree.getSelectedItem() == $gutSelected);
    gutCheck("and did not select the row it was on", %tree.getSelCount() == 1);

    // Now the padlock on the same row, which also proves the two columns are
    // told apart by a real click and not just by gutterColumnAt.
    gutPostTarget(%tree.getGutterPoint($gutIndex, "locked"));
    echo("EGUT: padlock box of row " @ $gutIndex @ " at " @
         %tree.getGutterPoint($gutIndex, "locked"));

    schedule(9000, 0, "gutAfterLockClick");
}

function gutAfterLockClick()
{
    %tree = GuiEditor.explorerWindow.tree;

    gutCheck("clicking the padlock locked the control", $gutCtrl.locked);
    gutCheck("and left the eye alone", $gutCtrl.hidden);
    gutCheck("and still did not move the selection",
             %tree.getSelectedItem() == $gutSelected);

    // And a click on the row's TEXT must still select, or the columns have
    // swallowed the tree.
    %point = %tree.getGutterPoint($gutIndex, "hidden");
    %textPoint = (getWord(%point, 0) + (3 * %tree.getGutterColumnWidth())) SPC getWord(%point, 1);
    gutPostTarget(%textPoint);
    echo("EGUT: row text at " @ %textPoint);

    schedule(9000, 0, "gutAfterTextClick");
}

function gutAfterTextClick()
{
    %tree = GuiEditor.explorerWindow.tree;

    gutCheck("clicking the row still selects it", %tree.getSelectedItem() == $gutIndex);
    gutCheck("and selecting did not toggle anything", $gutCtrl.hidden && $gutCtrl.locked);

    echo("EGUT RESULT: " @ $Pass @ " passed, " @ $Fail @ " failed");
    quit();
}
