// Visual harness for the cursor pane. The hot-spot editor is the part of this
// feature that only exists to be looked at, so these are the shots that say
// whether it works: the magnifier at a few zooms, the anchor mark and the hot
// spot mark distinguishable from each other, the tint following the theme, and
// the try-it range in the preview frame.
// Run: tests/run.ps1 -Shots cursorPane ; look in shots/.

setLogMode(1);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

testExec("editor/main.cs");
schedule(2500, 0, "sStep1");

function sStep1()
{
	ProjectManager.setProjectFolder("cursorPaneShotProject");
	GuiEditor.open();
	GuiEditor.openProfileEditor();

	%d = GuiEditor.profileEditorDialog;
	$sTheme = %d.library.createTheme("CursorShot");
	%d.tree.refresh();

	createPath(testRoot("shots/"));

	// category TAB zoom TAB name
	$sCases =
		"Default"   TAB "8"  TAB "default"   NL
		"Default"   TAB "16" TAB "close"     NL
		"Move"      TAB "6"  TAB "move"      NL
		"Edit"      TAB "10" TAB "ibeam"     NL
		"LeftRight" TAB "8"  TAB "leftRight" NL
		"NWSE"      TAB "8"  TAB "corner";
	$sIndex = 0;
	schedule(600, 0, "sShoot");
}

function sShoot()
{
	%rec = getRecord($sCases, $sIndex);
	%category = getField(%rec, 0);
	%zoom = getField(%rec, 1);
	%name = getField(%rec, 2);

	%d = GuiEditor.profileEditorDialog;
	%d.onTreeSelect(%d.library.cursorCategoryProxy[$sTheme.getId() @ "_" @ %category]);
	%d.cursorForm.editor.setZoom(%zoom);
	%d.cursorForm.refreshReadout();

	echo("SHOT: " @ %name @ " - " @ %category @ " at " @ %zoom @ "x");
	schedule(500, 0, "sGrab", %name);
}

function sGrab(%name)
{
	screenShot(testRoot("shots/cursorPane_" @ %name @ ".png"), "PNG");
	echo("SHOT: wrote cursorPane_" @ %name @ ".png");

	$sIndex++;
	if($sIndex < getRecordCount($sCases))
	{
		schedule(400, 0, "sShoot");
		return;
	}

	schedule(300, 0, "sTinted");
}

// The tint is what makes a grayscale stock set look like it belongs to the
// theme, so it needs a shot of its own against a colour nobody could mistake
// for the art.
function sTinted()
{
	%d = GuiEditor.profileEditorDialog;
	$sTheme.colorForeground = "60 200 255 255";
	%d.onTreeSelect(%d.library.cursorCategoryProxy[$sTheme.getId() @ "_Move"]);
	%d.cursorForm.editor.setZoom(8);

	schedule(500, 0, "sGrabTinted");
}

function sGrabTinted()
{
	screenShot(testRoot("shots/cursorPane_tinted.png"), "PNG");
	echo("SHOT: wrote cursorPane_tinted.png");

	// An anchored cursor: the faint crosshair (renderOffset) and the marked
	// pixel (hotSpot) should be visibly apart, which is the whole reason the
	// pane draws both.
	%d = GuiEditor.profileEditorDialog;
	%d.cursorForm.onAnchorPreset(0.5, 0.5);
	%d.cursorForm.row["hotSpot"].applyValue("6 -4");
	%d.cursorForm.onProfileRowCommit(%d.cursorForm.row["hotSpot"]);

	schedule(500, 0, "sGrabAnchored");
}

function sGrabAnchored()
{
	screenShot(testRoot("shots/cursorPane_anchored.png"), "PNG");
	echo("SHOT: wrote cursorPane_anchored.png");

	echo("SHOT DONE");
	schedule(400, 0, "quit");
}
