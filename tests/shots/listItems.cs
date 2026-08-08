// Visual harness for the Items section of the Gui Editor's properties pane: the
// static rows a list box or a drop down is authored with.
//
// Worth looking at rather than only asserting on, because the row is dense --
// nine controls on one line -- and whether all nine still fit at the pane's
// width is the whole question. The canvas is in the shot too: a row typed here
// is drawn on the list immediately, and that is the point of the feature.
//
// Every section but Items is shut, so the rows are in frame rather than eight
// screens below it.
//
// Run: tests/run.ps1 -Shots listItems ; look in shots/.

setLogMode(1);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

testExec("editor/main.cs");
schedule(2500, 0, "sOpenProject");

// A real project, loaded the way the project selector does it, so the pane and
// the list on the canvas are both drawn wearing a project's theme.
function sOpenProject()
{
	ProjectManager.setProjectFolder("PlanetX");
	EditorCore.projectSelector.onProjectSelected(pathConcat(getMainDotCsDir(), "PlanetX"));
	schedule(2500, 0, "sOpenEditor");
}

function sOpenEditor()
{
	EditorCore.toggleEditor();
	EditorCore.tabBook.selectPage(3);
	schedule(1500, 0, "sStep1");
}

function sStep1()
{
	$sTheme = GuiEditor.themeLibrary.createTheme("ItemsShotTheme");
	GuiEditor.themeName = $sTheme.getName();

	$sList = sPlace("GuiListBoxCtrl");
	$sList.resize(30, 30, 200, 140);

	// Five rows that between them use every field the section offers: an ID, a
	// color dot, an inactive row and one that starts selected.
	$sList.setItemList(
		"Easy"       TAB "1" TAB "1" TAB "0" TAB "0" TAB "1 1 1 1" NL
		"Normal"     TAB "2" TAB "1" TAB "1" TAB "0" TAB "1 1 1 1" NL
		"Hard"       TAB "3" TAB "1" TAB "0" TAB "1" TAB "1 0.4 0.2 1" NL
		"Nightmare"  TAB "4" TAB "0" TAB "0" TAB "1" TAB "0.8 0.2 0.2 1" NL
		"Impossible" TAB "5" TAB "0" TAB "0" TAB "0" TAB "1 1 1 1");

	$sDrop = sPlace("GuiDropDownCtrl");
	$sDrop.resize(30, 190, 200, 26);
	$sDrop.setItemList(
		"Windowed"           TAB "1" NL
		"Fullscreen"         TAB "2" TAB "1" TAB "1" NL
		"Borderless window"  TAB "3");

	createPath(testRoot("shots/"));

	// case TAB object global
	$sCases =
		"items-list"     TAB "$sList" NL
		"items-dropdown" TAB "$sDrop";
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

	// Everything but Items shut, so the rows are in the shot rather than eight
	// screens below it. The header cannot be collapsed and does not need to be.
	%panels = %pane.panelList SPC %pane.classPanels;
	for(%i = 0; %i < getWordCount(%panels); %i++)
	{
		%panel = %pane.panel[getWord(%panels, %i)];
		if(isObject(%panel))
		{
			%panel.setExpanded(false);
		}
	}
	%pane.textPanel.setExpanded(false);
	%pane.dynamicPanel.setExpanded(true);
	%pane.itemsPanel.setExpanded(true);

	GuiEditor.inspectorWindow.scroller.scrollToBottom();

	// Let the chain, the rows and the panels settle before grabbing.
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
