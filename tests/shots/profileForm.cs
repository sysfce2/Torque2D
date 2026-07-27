// Visual harness for the Gui Profile Editor's profile pane. Opens the editor and
// screenshots the pane in the cases where the grid layout has to behave: a busy
// category at two Properties-frame widths, a category that filters most fields
// away, and Show All. Run: tests/run.ps1 -Shots profileForm ; look in
// shots/.

setLogMode(1);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

testExec("editor/main.cs");
schedule(2500, 0, "sStep1");

function sStep1()
{
	ProjectManager.setProjectFolder("profileFormShotProject");
	GuiEditor.open();
	GuiEditor.openProfileEditor();

	%d = GuiEditor.profileEditorDialog;
	$sTheme = %d.library.createTheme("ShotTheme");
	%d.tree.refresh();

	// Every section open, so the section grids are visible in the shots too.
	%form = %d.profileForm;
	%panels = %form.panelList;
	for(%i = 0; %i < getWordCount(%panels); %i++)
	{
		%form.panel[getWord(%panels, %i)].setExpanded(true);
	}

	createPath(testRoot("shots/"));

	// case TAB category TAB frame width TAB show-all
	$sCases =
		"narrow"   TAB "TextEdit"    TAB "400" TAB "0" NL
		"wide"     TAB "TextEdit"    TAB "900" TAB "0" NL
		"filtered" TAB "ScrollThumb" TAB "900" TAB "0" NL
		"showall"  TAB "ScrollThumb" TAB "900" TAB "1";
	$sIndex = 0;
	schedule(1200, 0, "sShoot");
}

function sShoot()
{
	%d = GuiEditor.profileEditorDialog;
	%rec = getRecord($sCases, $sIndex);
	%name = getField(%rec, 0);
	%category = getField(%rec, 1);
	%width = getField(%rec, 2);
	%showAll = getField(%rec, 3);

	%d.onTreeSelect(%d.library.categoryProxy[$sTheme.getId() @ "_" @ %category]);
	%d.frames.setFrameSize(%d.memberFrame, %width);

	%form = %d.profileForm;
	%form.showAllBox.setStateOn(%showAll);
	%form.onShowAllToggled();

	echo("SHOT: " @ %name @ " - " @ %category @ " at " @ %width @ (%showAll ? " (show all)" : ""));

	// Let the frame set, the grids and the panels settle before grabbing.
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

	// Land on a border node so the known profile-node teardown crash stays out
	// of the way (see smoke/profileForm.cs).
	%d = GuiEditor.profileEditorDialog;
	%bname = getWord($sTheme.getBorderCategoryNames(), 0);
	%d.onTreeSelect(new ScriptObject(){ kind = "border"; theme = $sTheme; category = %bname; treeLabel = %bname; });

	echo("SHOT DONE");
	schedule(300, 0, "quit");
}
