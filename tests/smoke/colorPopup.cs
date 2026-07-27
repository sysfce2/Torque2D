// Color-popup smoke test. Drives the two new optional rows on GuiColorPopupCtrl:
// the swatch grid (which the Profile Editor fills with the selected theme's six
// colors) and the R/G/B/A value row, in both Integer and Float modes. The test
// that matters most is that a color chosen from a swatch survives a rendered
// frame unchanged -- the pickers re-read their color out of the framebuffer, and
// used to overwrite whatever exact value the popup had been given.
// Run: tests/run.ps1 colorPopup  ; grep CPSMOKE in console.log.

setLogMode(1);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

function cCheck(%label, %cond)
{
	if(%cond) echo("CPSMOKE PASS: " @ %label);
	else      echo("CPSMOKE FAIL: " @ %label);
}

function cSelectCategory(%category)
{
	%d = GuiEditor.profileEditorDialog;
	%d.onTreeSelect(%d.library.categoryProxy[$cTheme.getId() @ "_" @ %category]);
}

testExec("editor/main.cs");
schedule(2000, 0, "cStep1");

//-----------------------------------------------------------------------------
// The swatch row is filled from the selected theme when the popup opens.
//-----------------------------------------------------------------------------

function cStep1()
{
	ProjectManager.setProjectFolder("colorPopupSmokeProject");
	GuiEditor.open();
	GuiEditor.openProfileEditor();

	%d = GuiEditor.profileEditorDialog;
	$cTheme = %d.library.createTheme("CPSmoke");
	%d.tree.refresh();
	cSelectCategory("Button");

	$cPopup = %d.profileForm.fillRow.swatch[0];
	cCheck("fill swatch is a themed color popup", $cPopup.getClassName() $= "GuiColorPopupCtrl");
	cCheck("no swatches before the popup is opened", $cPopup.getSwatchCount() == 0);

	$cPopup.openColorPopup();
	cCheck("theme filled six swatches", $cPopup.getSwatchCount() == 6);

	%fields = "colorBackground colorSurface colorForeground colorAccent colorHighlight colorWarning";
	%same = true;
	for(%i = 0; %i < 6; %i++)
	{
		if($cPopup.getSwatchI(%i) !$= $cTheme.getFieldValue(getWord(%fields, %i)))
		{
			%same = false;
			echo("CPSMOKE  swatch " @ %i @ " = " @ $cPopup.getSwatchI(%i) @
				" , theme = " @ $cTheme.getFieldValue(getWord(%fields, %i)));
		}
	}
	cCheck("every swatch matches its theme color", %same);

	// Float and integer spellings of the same swatch have to agree.
	%f = $cPopup.getSwatchF(3);
	%i = $cPopup.getSwatchI(3);
	cCheck("float swatch matches integer swatch",
		mRound(getWord(%f, 0) * 255) == getWord(%i, 0) &&
		mRound(getWord(%f, 3) * 255) == getWord(%i, 3));

	schedule(400, 0, "cStep2");
}

//-----------------------------------------------------------------------------
// A swatch color survives being rendered.
//-----------------------------------------------------------------------------

function cStep2()
{
	$cPopup.selectSwatch(3);
	cCheck("swatch color taken immediately", $cPopup.getColorI() $= $cTheme.colorAccent);

	// Two frames later the pickers have had every chance to read themselves back
	// off the framebuffer and overwrite the value.
	schedule(400, 0, "cStep3");
}

function cStep3()
{
	cCheck("swatch color survived a rendered frame (" @ $cPopup.getColorI() @ " vs " @ $cTheme.colorAccent @ ")",
		$cPopup.getColorI() $= $cTheme.colorAccent);

	// And so does an exact color set from script while the popup is open.
	$cPopup.setColorI("17 200 39 255");
	schedule(400, 0, "cStep4");
}

//-----------------------------------------------------------------------------
// The value row's channels, in both modes.
//-----------------------------------------------------------------------------

function cStep4()
{
	cCheck("script color survived a rendered frame (" @ $cPopup.getColorI() @ ")",
		$cPopup.getColorI() $= "17 200 39 255");

	// --- Integer mode reads and writes 0..255. ---
	$cPopup.valueMode = "Integer";
	cCheck("integer red channel", $cPopup.getColorChannel(0) == 17);
	cCheck("integer green channel", $cPopup.getColorChannel(1) == 200);
	cCheck("integer blue channel", $cPopup.getColorChannel(2) == 39);
	cCheck("integer alpha channel", $cPopup.getColorChannel(3) == 255);

	$cPopup.setColorChannel(0, 128);
	cCheck("integer write touched only red", $cPopup.getColorI() $= "128 200 39 255");

	// Out of range values clamp rather than wrap.
	$cPopup.setColorChannel(0, 900);
	cCheck("integer write clamps high", $cPopup.getColorChannel(0) == 255);
	$cPopup.setColorChannel(0, -20);
	cCheck("integer write clamps low", $cPopup.getColorChannel(0) == 0);

	// --- Float mode reads and writes 0..1 over the same color. ---
	$cPopup.setColorI("0 128 255 255");
	$cPopup.valueMode = "Float";
	cCheck("float blue channel", $cPopup.getColorChannel(2) == 1);
	cCheck("float red channel", $cPopup.getColorChannel(0) == 0);

	$cPopup.setColorChannel(3, 0.5);
	cCheck("float write touched only alpha", getWord($cPopup.getColorI(), 3) == 128);
	cCheck("float write left the rest alone", $cPopup.getColorI() $= "0 128 255 128");

	$cPopup.valueMode = "Integer";
	$cPopup.closeColorPopup();

	schedule(400, 0, "cStep5");
}

//-----------------------------------------------------------------------------
// Clearing, re-filling on the next open, and a plain popup with neither row.
//-----------------------------------------------------------------------------

function cStep5()
{
	%d = GuiEditor.profileEditorDialog;

	$cPopup.clearSwatches();
	cCheck("clearSwatches emptied the row", $cPopup.getSwatchCount() == 0);

	// Selecting a swatch that is not there warns and leaves the color alone.
	%before = $cPopup.getColorI();
	$cPopup.selectSwatch(0);
	cCheck("selecting a missing swatch changed nothing", $cPopup.getColorI() $= %before);

	// Editing a theme color has to reach the swatches, because they are filled at
	// open time rather than when the row was built.
	$cTheme.colorAccent = "1 2 3 255";
	$cPopup.openColorPopup();
	cCheck("swatches refilled on the next open", $cPopup.getSwatchCount() == 6);
	cCheck("swatch picked up the edited theme color", $cPopup.getSwatchI(3) $= "1 2 3 255");
	$cPopup.closeColorPopup();

	// A stand-alone profile has no theme, so its popups show no swatch row.
	%profile = %d.library.createStandalone("CPSmokeStandalone");
	%d.tree.refresh();
	%d.onTreeSelect(new ScriptObject() { kind = "standalone"; target = %profile; });

	%swatch = %d.profileForm.fillRow.swatch[0];
	%swatch.openColorPopup();
	cCheck("stand-alone profile gets no swatches", %swatch.getSwatchCount() == 0);
	%swatch.closeColorPopup();

	// A plain popup, the way a game would use one: no class, no swatches, no
	// value row, and adding swatches by hand still works.
	$cPlain = new GuiColorPopupCtrl()
	{
		Position = "20 20";
		Extent = "40 40";
	};
	%content = Canvas.getContent();
	%content.add($cPlain);
	cCheck("plain popup starts with no swatches", $cPlain.getSwatchCount() == 0);
	cCheck("plain popup starts with the value row off", !$cPlain.showColorValues);

	$cPlain.addSwatchI("10 20 30 255");
	$cPlain.addSwatchF("0 0.5 1");
	$cPlain.addSwatchI("White");
	cCheck("addSwatchI took integers", $cPlain.getSwatchI(0) $= "10 20 30 255");
	cCheck("addSwatchF took floats", $cPlain.getSwatchI(1) $= "0 128 255 255");
	cCheck("addSwatchI took a stock color name", $cPlain.getSwatchI(2) $= "255 255 255 255");
	cCheck("addSwatchF defaulted alpha to opaque", getWord($cPlain.getSwatchF(1), 3) == 1);

	schedule(400, 0, "cStep6");
}

function cStep6()
{
	$cPlain.selectSwatch(0);
	cCheck("a closed popup still takes a swatch color", $cPlain.getColorI() $= "10 20 30 255");
	$cPlain.delete();

	// Land on a border node before quitting: quitting with a profile node selected
	// trips a teardown crash that predates this work (see smoke/profileForm.cs).
	%d = GuiEditor.profileEditorDialog;
	%bname = getWord($cTheme.getBorderCategoryNames(), 0);
	%d.onTreeSelect(new ScriptObject(){ kind = "border"; theme = $cTheme; category = %bname; treeLabel = %bname; });

	echo("CPSMOKE DONE");
	schedule(300, 0, "quit");
}
