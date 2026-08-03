// The Gui Editor's Explorer tree, with a Gui deep enough to show what the two
// gutter columns cost and what the rows look like once they carry a picture.
//
// This is the proof no assertion can give. The boxes are drawn in the profile's
// HOVER fill, which is deliberately almost the row fill -- the whole point is
// that the rail is quiet -- and "quiet but findable" is a judgement a person has
// to make by looking. Same for the 16px control icons at their real size on the
// real theme, and for how much of a 228px tree is left for text at depth.
//
// Two controls are locked and two are hidden, so both columns show a mix rather
// than a run of one thing, and one row is selected -- which is the case that has
// already been wrong once.
//
// The eye and the padlock are drawn on a box whose colour does NOT change with
// selection, so an icon that took the row's font colour like the text does put
// selected-text ink on an unselected background and all but disappeared. One
// shot per editor theme, because the amount by which that is wrong depends
// entirely on how far apart a theme's selected and normal colours are: it is
// nearly invisible under some and merely odd under others, and a single shot
// would not have shown it.
setLogMode(2);
setScriptExecEcho(false);
trace(false);
$Scripts::ignoreDSOs = true;
setCompanyAndProduct("Torque Game Engines", "Torque2D");
ModuleDatabase.EchoInfo = false;
AssetDatabase.EchoInfo = false;

testExec("editor/main.cs");

// screenShot does not create its folder, and fails by logging rather than
// throwing -- so a missing folder is a silent no-shot.
createPath(testRoot("shots/"));

schedule(2500, 0, "expTreeOpenProject");

function expTreeOpenProject()
{
    ProjectManager.setProjectFolder("PlanetX");
    EditorCore.projectSelector.onProjectSelected(pathConcat(getMainDotCsDir(), "PlanetX"));
    schedule(2500, 0, "expTreeOpenEditor");
}

// Pages register in load order: EditorConsole, ProjectManager, AssetAdmin,
// GuiEditor.
function expTreeOpenEditor()
{
    EditorCore.toggleEditor();
    EditorCore.tabBook.selectPage(3);
    schedule(1500, 0, "expTreeClear");
}

// Start empty, and in a step of its own. PlanetX opens with a Gui already
// loaded and finishes displaying it after the editor page has come up, so
// clearing in the same frame as the build leaves that Gui's controls in the
// tree -- and the shot becomes a picture of PlanetX rather than of a shape
// chosen to show the columns.
function expTreeClear()
{
    GuiEditor.NewGui();
    schedule(500, 0, "expTreeSetup");
}

function expTreeSetup()
{
    %root = GuiEditor.rootGui;

    // A backdrop with a panel inside it, and rows inside that: three levels, so
    // the indent budget is visible rather than theoretical.
    // A spread of classes, so the row icons are not all the same picture. Names
    // come from GuiEditorControlIcons.cs -- there is no GuiTextCtrl in this
    // engine, and a class that does not exist fails silently: eval hands back 0,
    // add() takes it, and the row simply never appears.
    %backdrop = expAdd("GuiControl", %root, "8 8", "420 300", "backdrop");
    %panel = expAdd("GuiPanelCtrl", %backdrop, "12 12", "300 240", "panel");
    %title = expAdd("GuiControl", %panel, "8 8", "200 24", "title");
    %edit = expAdd("GuiTextEditCtrl", %panel, "8 40", "200 28", "nameBox");
    %list = expAdd("GuiListBoxCtrl", %panel, "8 76", "200 90", "choices");
    %check = expAdd("GuiCheckBoxCtrl", %panel, "8 176", "90 24", "agree");
    %ok = expAdd("GuiButtonCtrl", %panel, "8 206", "90 30", "okButton");
    %slider = expAdd("GuiSliderCtrl", %backdrop, "12 260", "300 24", "volume");

    // A mix down both columns, so neither is a run of one thing.
    %title.locked = true;
    %list.locked = true;
    %edit.hidden = true;
    %ok.hidden = true;

    %tree = GuiEditor.explorerWindow.tree;
    %tree.refresh();

    // Open every branch, so the shot shows the whole shape rather than whatever
    // happened to be expanded.
    %count = %tree.getItemCount();
    for(%i = 0; %i < %count; %i++)
    {
        %tree.setItemOpen(%i, true);
    }
    %tree.refresh();

    // Remembered rather than selected once: switching the editor theme drops the
    // tree's selection, and a selected row is the whole point of these shots.
    $expTree::selectId = %panel.getId();
    expTreeSelect();

    echo("EXPTREE: " @ %tree.getItemCount() @ " rows, column width " @
         %tree.getGutterColumnWidth());

    schedule(600, 0, "expTreeShot");
}

// Themed the way a dropped control would be, because the theme is what decides
// a bare GuiControl's category -- and its category is what decides which of the
// four GuiControl faces its row icon shows.
function expAdd(%class, %parent, %pos, %extent, %name)
{
    %ctrl = eval("return new " @ %class @ "();");
    if(!isObject(%ctrl))
    {
        // Loud, because the failure is otherwise a row that quietly is not there
        // and a shot that looks like a smaller Gui than the one asked for.
        error("EXPTREE: could not make a " @ %class);
        return 0;
    }
    %ctrl.Position = %pos;
    %ctrl.Extent = %extent;
    %parent.add(%ctrl);
    %ctrl.setInternalName(%name);

    %theme = GuiEditor.themeByName(GuiEditor.themeName);
    if(isObject(%theme))
    {
        GuiEditor.themeApplier.applyToBranch(%ctrl, %theme, false);
    }
    return %ctrl;
}

// One shot per registered editor theme. ThemeManager owns the editor's own
// chrome -- the tree wears its treeViewProfile -- which is a different thing
// from GuiEditor.setTheme, that being the theme of the Gui being edited.
$expTree::theme = 0;

function expTreeShot()
{
    if($expTree::theme >= ThemeManager.themeList.getCount())
    {
        echo("SHOTS DONE");
        quit();
        return;
    }

    // The .class field, not getName or getClassName: the themes are registered
    // as unnamed ScriptObjects, so the first is empty and the second says
    // "ScriptObject" for all four.
    ThemeManager.setTheme($expTree::theme);
    echo("EXPTREE: theme " @ $expTree::theme @ " is " @ ThemeManager.activeTheme.class);

    // Selecting and grabbing in one step captures the frame BEFORE the
    // selection: screenShot reads the framebuffer, which still holds the last
    // frame drawn. The row has to be selected a frame ahead of the shot.
    expTreeSelect();
    schedule(600, 0, "expTreeGrab");
}

// A selected row, every time. The rail's colours are deliberately independent of
// selection, so a shot without a selected row cannot show whether that is true.
function expTreeSelect()
{
    %tree = GuiEditor.explorerWindow.tree;
    %index = %tree.findItemID($expTree::selectId);
    if(%index >= 0)
    {
        %tree.clearSelection();
        %tree.setSelected(%index, true);
    }
    else
    {
        error("EXPTREE: lost the row that was meant to be selected");
    }
}

function expTreeGrab()
{
    screenShot(testRoot("shots/explorerTree" @ $expTree::theme @ ".png"), "PNG");
    $expTree::theme++;
    schedule(500, 0, "expTreeShot");
}
