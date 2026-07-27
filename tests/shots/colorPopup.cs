// Visual harness for the color popup's two new rows. Screenshots the popup in the
// cases where the layout has to behave: nothing turned on (the old look), the
// swatch row alone, both rows in each value mode, and enough swatches to wrap the
// grid onto three rows so the popup has to grow to fit them.
// Run: tests/run.ps1 -Shots colorPopup ; look in shots/.

setLogMode(1);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

testExec("editor/main.cs");
schedule(2500, 0, "sStep1");

function sStep1()
{
	ProjectManager.setProjectFolder("colorPopupShotProject");
	GuiEditor.open();
	GuiEditor.openProfileEditor();

	%d = GuiEditor.profileEditorDialog;
	$sTheme = %d.library.createTheme("ShotTheme");
	%d.tree.refresh();
	%d.onTreeSelect(%d.library.categoryProxy[$sTheme.getId() @ "_Button"]);

	$sPopup = %d.profileForm.fillRow.swatch[0];

	createPath(testRoot("shots/"));

	// case TAB swatches TAB value row TAB value mode
	$sCases =
		"bare"      TAB "none"  TAB "0" TAB "Integer" NL
		"swatches"  TAB "theme" TAB "0" TAB "Integer" NL
		"integer"   TAB "theme" TAB "1" TAB "Integer" NL
		"float"     TAB "theme" TAB "1" TAB "Float"   NL
		"wrapped"   TAB "many"  TAB "1" TAB "Integer";
	$sIndex = 0;
	schedule(800, 0, "sShoot");
}

function sShoot()
{
	%rec = getRecord($sCases, $sIndex);
	%name = getField(%rec, 0);
	%swatches = getField(%rec, 1);
	%showValues = getField(%rec, 2);
	%mode = getField(%rec, 3);

	$sPopup.showColorValues = %showValues;
	$sPopup.valueMode = %mode;

	// swatchSource says what onOpen should do: "theme" leaves the Profile Editor's
	// own filling alone, the other two take it over for the shot.
	$sPopup.swatchSource = %swatches;

	echo("SHOT: " @ %name @ " - swatches " @ %swatches @ ", values " @ %showValues @ " (" @ %mode @ ")");

	$sPopup.openColorPopup();
	schedule(500, 0, "sGrab", %name);
}

function sGrab(%name)
{
	screenShot(testRoot("shots/colorPopup_" @ %name @ ".png"), "PNG");
	echo("SHOT: wrote colorPopup_" @ %name @ ".png");

	$sPopup.closeColorPopup();

	$sIndex++;
	if($sIndex < getRecordCount($sCases))
	{
		schedule(400, 0, "sShoot");
		return;
	}

	// Land on a border node so the known profile-node teardown crash stays out of
	// the way (see smoke/profileForm.cs).
	%d = GuiEditor.profileEditorDialog;
	%bname = getWord($sTheme.getBorderCategoryNames(), 0);
	%d.onTreeSelect(new ScriptObject(){ kind = "border"; theme = $sTheme; category = %bname; treeLabel = %bname; });

	echo("SHOT DONE");
	schedule(400, 0, "quit");
}

// Take over the popup's swatch filling for the two cases the theme cannot
// produce: an empty row, and more swatches than fit on one line.
function GuiProfileEditorColorPopup::onOpen(%this)
{
	if(%this.swatchSource $= "none")
	{
		%this.clearSwatches();
		return;
	}

	if(%this.swatchSource $= "many")
	{
		%this.clearSwatches();
		for(%i = 0; %i < 13; %i++)
		{
			%this.addSwatchF((%i / 12) SPC (1 - (%i / 12)) SPC 0.5);
		}
		return;
	}

	%this.fillThemeSwatches();
}
