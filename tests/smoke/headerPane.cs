//-----------------------------------------------------------------------------
// Selecting a header row ("Gui Themes", "Profiles", "Borders", "Stand Alone")
// must empty the Properties window rather than leaving the previously selected
// profile's rows on screen.
//
// Theme nodes are skipped on purpose - selecting one blocks in
// GuiProfileEditorPreview::showTheme, a separate pre-existing bug.
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

function paneCheck(%label, %condition)
{
    if(%condition)
    {
        $Pass++;
        echo("HDR PASS: " @ %label);
    }
    else
    {
        $Fail++;
        echo("HDR FAIL: " @ %label);
    }
}

// The tree is multi-select (GuiListBoxCtrl defaults AllowMultipleSelections on),
// so the setSelected binding ADDS to the selection and no-ops on an already
// selected row. A real click clears first - do the same here.
function selectRow(%tree, %index)
{
    %tree.clearSelection();
    %tree.setSelected(%index, true);
}

// Reports which of the three member panes are showing.
function paneState(%dialog)
{
    return (%dialog.profileFormScroller.isVisible() ? "profile " : "") @
           (%dialog.formScroller.isVisible() ? "theme " : "") @
           (%dialog.borderFormScroller.isVisible() ? "border " : "");
}

schedule(2500, 0, "hdrSetup");

function hdrSetup()
{
    ProjectManager.setProjectFolder("smokeThemeProject");
    GuiEditor.open();
    GuiEditor.openProfileEditor();

    %dialog = GuiEditor.profileEditorDialog;
    %dialog.onNewTheme();
    %nameDialog = %dialog.childDialog;
    %nameDialog.nameBox.setText("HdrTheme");
    %nameDialog.onDone();

    schedule(500, 0, "hdrRun");
}

function hdrRun()
{
    %dialog = GuiEditor.profileEditorDialog;
    %tree = %dialog.tree;
    %count = %tree.getItemCount();

    // Locate one node of each kind by asking the proxy behind each row.
    %categoryIdx = -1;
    %borderIdx = -1;
    %folderIdx = -1;
    %rootIdx = -1;
    for(%i = 0; %i < %count; %i++)
    {
        %proxy = %tree.getItemID(%i);
        %kind = %proxy.kind;
        if(%kind $= "category" && %categoryIdx == -1) { %categoryIdx = %i; }
        if(%kind $= "border" && %borderIdx == -1) { %borderIdx = %i; }
        if(%kind $= "folder" && %folderIdx == -1) { %folderIdx = %i; }
        if(%kind $= "root" && %rootIdx == -1) { %rootIdx = %i; }
    }
    echo("HDR: category=" @ %categoryIdx @ " border=" @ %borderIdx @
         " folder=" @ %folderIdx @ " root=" @ %rootIdx);

    // --- A profile node fills the Properties window.
    selectRow(%tree, %categoryIdx);
    echo("HDR: after category -> [" @ paneState(%dialog) @ "]");
    paneCheck("category shows the profile pane", %dialog.profileFormScroller.isVisible());
    paneCheck("category binds the profile form", isObject(%dialog.profileForm.target));

    // --- Then a header row must empty it again.
    selectRow(%tree, %folderIdx);
    echo("HDR: after folder -> [" @ paneState(%dialog) @ "]");
    paneCheck("folder hides the profile pane", !%dialog.profileFormScroller.isVisible());
    paneCheck("folder hides the theme pane", !%dialog.formScroller.isVisible());
    paneCheck("folder hides the border pane", !%dialog.borderFormScroller.isVisible());
    paneCheck("folder unbinds the profile form", !isObject(%dialog.profileForm.target));

    // --- Same for the tree root.
    selectRow(%tree, %categoryIdx);
    selectRow(%tree, %rootIdx);
    echo("HDR: after root -> [" @ paneState(%dialog) @ "]");
    paneCheck("root hides every pane", !%dialog.profileFormScroller.isVisible() &&
                                       !%dialog.formScroller.isVisible() &&
                                       !%dialog.borderFormScroller.isVisible());

    // --- A border node still gets its own pane (no regression from the refactor).
    echo("HDR: before border: selected=" @ %tree.getSelectedItem() @
         " selCount=" @ %tree.getSelCount() @ " items=" @ %tree.getItemCount());
    selectRow(%tree, %borderIdx);
    echo("HDR: after border: selected=" @ %tree.getSelectedItem() @
         " selCount=" @ %tree.getSelCount() @ " items=" @ %tree.getItemCount() @
         " proxyKind=" @ %dialog.currentProxy.kind);
    echo("HDR: after border -> [" @ paneState(%dialog) @ "]");
    paneCheck("border shows only the border pane",
              %dialog.borderFormScroller.isVisible() &&
              !%dialog.profileFormScroller.isVisible() &&
              !%dialog.formScroller.isVisible());

    // --- And a profile node still works after a header selection.
    echo("HDR: item count now " @ %tree.getItemCount() @
         "; idx " @ %folderIdx @ " kind=" @ %tree.getItemID(%folderIdx).kind @
         ", idx " @ %categoryIdx @ " kind=" @ %tree.getItemID(%categoryIdx).kind);
    selectRow(%tree, %folderIdx);
    echo("HDR: selected=" @ %tree.getSelectedItem() @ " panes=[" @ paneState(%dialog) @ "]");
    selectRow(%tree, %categoryIdx);
    echo("HDR: selected=" @ %tree.getSelectedItem() @
         " proxy kind=" @ %dialog.currentProxy.kind);
    echo("HDR: after folder->category -> [" @ paneState(%dialog) @ "]");
    paneCheck("profile pane returns after a header row",
              %dialog.profileFormScroller.isVisible() &&
              isObject(%dialog.profileForm.target));

    echo("HDR RESULT: " @ $Pass @ " passed, " @ $Fail @ " failed");
    quit();
}
