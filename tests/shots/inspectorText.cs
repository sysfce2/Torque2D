// Visual harness for the Category picker and the text block: the high-score
// heading from the bug report, before and after.
//
//   text-empty  a GuiControl inside a panel, which is what you get when you
//               drop one and type a caption into it afterwards -- the guess ran
//               when it was dropped, saw no text, and made it an Empty.
//   text-label  the same control after its Category is set to Label and its
//               font size raised, which is the whole of the fix from the
//               outside.
//
// Run: tests/run.ps1 -Shots inspectorText ; look in shots/.

setLogMode(1);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

testExec("editor/main.cs");
schedule(2500, 0, "sOpenProject");

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
	$sTheme = GuiEditor.themeLibrary.createTheme("TextShotTheme");
	GuiEditor.themeName = $sTheme.getName();

	// The dialog: a backdrop with a heading in it. The heading is themed while
	// it is still empty, which is the whole of the problem.
	$sPanel = new GuiControl()
	{
		Position = "40 40";
		Extent = "260 180";
	};
	GuiEditor.rootGui.add($sPanel);

	$sHeading = new GuiControl()
	{
		Position = "20 16";
		Extent = "220 32";
	};
	$sPanel.add($sHeading);
	GuiEditor.themeApplier.applyToBranch($sPanel, $sTheme, true);

	$sHeading.text = "High Scores";

	createPath(testRoot("shots/"));
	schedule(500, 0, "sShootEmpty");
}

function sShootEmpty()
{
	sBind();
	schedule(500, 0, "sGrab", "text-empty", "sFix");
}

// What the fix is, in two edits: say what the control is, then set the size the
// caption wants.
function sFix()
{
	%pane = GuiEditor.inspectorWindow.pane;

	%row = %pane.header.categoryRow;
	%row.applyValue("Label");
	%row.commit();

	%row = %pane.row["fontSizeAdjust"];
	%row.applyValue("1.6");
	%row.commit();

	sBind();
	schedule(500, 0, "sGrab", "text-label", "");
}

function sBind()
{
	%pane = GuiEditor.inspectorWindow.pane;
	%pane.bind($sHeading);

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
}

function sGrab(%name, %next)
{
	screenShot(testRoot("shots/" @ %name @ ".png"), "PNG");
	echo("SHOT: wrote " @ %name @ ".png");

	if(%next !$= "")
	{
		schedule(300, 0, %next);
		return;
	}

	echo("SHOT DONE");
	schedule(300, 0, "quit");
}
