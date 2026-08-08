//-----------------------------------------------------------------------------
// A tree row can wear a small picture, drawn between the triangle and the text.
//
// The frame is pulled from script ONCE, while the tree is building itself, and
// cached on the row -- onRenderItem runs for every visible row of every frame,
// so asking per draw would be a console call per row per frame. That caching is
// the whole risk in the feature: a row whose picture should have changed but
// whose cache was never invalidated is wrong in a way nothing else reports.
//
// This has to run with a canvas. Adding a row calls updateSize, which asks the
// profile for a font, which loads one, which registers a texture -- so the row
// arithmetic is unit tested (guiTreeRowLayoutTests.cc) and the plumbing is here.
//-----------------------------------------------------------------------------

setLogMode(2);
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

function iconCheck(%label, %condition)
{
    if(%condition)
    {
        $Pass++;
        echo("TICO PASS: " @ %label);
    }
    else
    {
        $Fail++;
        echo("TICO FAIL: " @ %label);
    }
}

// The tree under test answers from here. Returning "" is how a handler declines
// a row, which must read the same as having no handler at all.
function IconTestTree::onGetItemIcon(%this, %obj)
{
    if(%obj.iconDeclines)
    {
        return "";
    }
    return %obj.wantIcon;
}

schedule(2500, 0, "icoRun");

function icoRun()
{
    // A little tree of our own rather than the editor's, so this tests the base
    // class and not the Gui Editor's use of it.
    %root = new SimGroup();
    %a = new SimObject(); %a.wantIcon = 7;
    %b = new SimObject(); %b.wantIcon = 3;
    %c = new SimObject(); %c.iconDeclines = true;
    %root.add(%a);
    %root.add(%b);
    %root.add(%c);

    %tree = new GuiTreeViewCtrl()
    {
        class = "IconTestTree";
        Position = "0 0";
        Extent = "200 200";
    };
    ThemeManager.setProfile(%tree, "treeViewProfile");
    Canvas.getContent().add(%tree);

    // --- With no sheet set, the question is never asked.
    %tree.inspect(%root);
    iconCheck("no sheet means no icon on the root", %tree.getItemIcon(0) == -1);
    iconCheck("no sheet means no icon on a branch", %tree.getItemIcon(1) == -1);

    // --- Set one, and the rows pick their frames up on the next build.
    %tree.IconImage = "EditorCore:editorIcons16";
    iconCheck("the sheet round-trips through the field",
              %tree.IconImage $= "EditorCore:editorIcons16");

    %tree.refresh();
    iconCheck("a branch wears the frame script named", %tree.getItemIcon(1) == 7);
    iconCheck("each branch answers for itself", %tree.getItemIcon(2) == 3);
    iconCheck("a declined row wears none", %tree.getItemIcon(3) == -1);

    // --- The cache is the risk. A row whose answer changes must be refreshable
    // without rebuilding the whole tree, because that is what the properties
    // pane does when a control is re-profiled under a selection.
    %b.wantIcon = 21;
    iconCheck("the cache does not follow the object on its own", %tree.getItemIcon(2) == 3);
    %tree.refreshItem(2);
    iconCheck("refreshItem re-asks", %tree.getItemIcon(2) == 21);
    iconCheck("refreshItem left its neighbours alone", %tree.getItemIcon(1) == 7);

    // --- And clearing the sheet turns the feature back off.
    %tree.IconImage = "";
    %tree.refresh();
    iconCheck("clearing the sheet stops the asking", %tree.getItemIcon(1) == -1);

    // --- Out-of-range indices report rather than crash.
    iconCheck("an index past the end answers -1", %tree.getItemIcon(999) == -1);

    %tree.deleteObject();
    %root.deleteObject();

    echo("TICO RESULT: " @ $Pass @ " passed, " @ $Fail @ " failed");
    quit();
}
