// Visual harness for the Gui Editor's properties pane. Screenshots the header
// archetypes side by side, which is the thing the design turns on: one shell,
// with the geometry, text and value blocks swapped per control.
//
// The four cases are the ones that look different from each other:
//   button    the ordinary shape -- full geometry, a text block, easing
//   window    the most secondary fields of anything, plus a title
//   tabpage   no geometry at all; the book owns every bit of it
//   chainkid  one axis owned, the other still the control's
//
// Run: tests/run.ps1 -Shots inspectorPane ; look in shots/.

setLogMode(1);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

testExec("editor/main.cs");
schedule(2500, 0, "sOpenProject");

// A real project, loaded the way the project selector does it, because the pane
// has to be drawn wearing a project's theme to be worth looking at.
function sOpenProject()
{
	ProjectManager.setProjectFolder("PlanetX");
	EditorCore.projectSelector.onProjectSelected(pathConcat(getMainDotCsDir(), "PlanetX"));
	schedule(2500, 0, "sOpenEditor");
}

// Loading AppCore starts the project's game over the canvas; the editor comes
// back the way Ctrl+~ does it. Pages register in load order: EditorConsole,
// ProjectManager, AssetAdmin, GuiEditor.
function sOpenEditor()
{
	EditorCore.toggleEditor();
	EditorCore.tabBook.selectPage(3);
	schedule(1500, 0, "sStep1");
}

function sStep1()
{
	// A theme, so the pane is drawn wearing what a real project would.
	$sTheme = GuiEditor.themeLibrary.createTheme("PaneShotTheme");
	GuiEditor.themeName = $sTheme.getName();

	// One of each interesting shape, parked in the Gui being edited.
	$sButton = sPlace("GuiButtonCtrl");
	$sWindow = sPlace("GuiWindowCtrl");

	%book = sPlace("GuiTabBookCtrl");
	$sPage = new GuiTabPageCtrl();
	%book.add($sPage);
	GuiEditor.themeApplier.applyToBranch($sPage, $sTheme, true);

	%chain = sPlace("GuiChainCtrl");
	%chain.IsVertical = true;
	$sChainKid = new GuiButtonCtrl();
	%chain.add($sChainKid);
	GuiEditor.themeApplier.applyToBranch($sChainKid, $sTheme, true);

	// A dynamic field and a second WindowContent, so the two sections that only
	// appear when they have something to say are in the window's shot.
	$sWindow.myTag = "example";
	$sTheme.createProfile("WindowContent");

	createPath(testRoot("shots/"));

	// case TAB object global
	$sCases =
		"pane-button"   TAB "$sButton"   NL
		"pane-window"   TAB "$sWindow"   NL
		"pane-tabpage"  TAB "$sPage"     NL
		"pane-chainkid" TAB "$sChainKid";
	$sIndex = 0;
	schedule(1000, 0, "sShoot");
}

function sPlace(%class)
{
	%ctrl = eval("return new " @ %class @ "();");
	GuiEditor.rootGui.add(%ctrl);
	GuiEditor.themeApplier.applyToBranch(%ctrl, $sTheme, true);
	return %ctrl;
}

function sShoot()
{
	%rec = getRecord($sCases, $sIndex);
	%name = getField(%rec, 0);
	%ctrl = eval("return " @ getField(%rec, 1) @ ";");

	%pane = GuiEditor.inspectorWindow.pane;
	%pane.bind(%ctrl);

	// Every section open, so the shot shows what each control actually offers
	// rather than a column of collapsed headers.
	// textPanel is not in panelList: it holds one component rather than a list
	// of rows, so the pane decides its visibility outright.
	%pane.textPanel.setExpanded(true);

	%panels = %pane.panelList SPC %pane.classPanels;
	for(%i = 0; %i < getWordCount(%panels); %i++)
	{
		%panel = %pane.panel[getWord(%panels, %i)];
		if(isObject(%panel) && %panel.isVisible())
		{
			%panel.setExpanded(true);
		}
	}
	%pane.dynamicPanel.setExpanded(true);

	// Let the chain, the grids and the panels settle before grabbing.
	schedule(500, 0, "sGrab", %name);
}

function sGrab(%name)
{
	screenShot(testRoot("shots/" @ %name @ ".png"), "PNG");
	echo("SHOT: wrote " @ %name @ ".png");

	$sIndex++;
	if($sIndex < getRecordCount($sCases))
	{
		schedule(300, 0, "sShoot");
		return;
	}

	echo("SHOT DONE");
	schedule(300, 0, "quit");
}
