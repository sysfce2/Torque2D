//-----------------------------------------------------------------------------
// Temporary smoke test for "Set Theme" in the Gui Editor and AppCore's runtime
// theme loading. Boots the editor, opens the PlanetX project, builds a small
// control tree, applies the theme, and checks every profile slot landed on the
// right category. Echoes SMOKE PASS/FAIL lines and quits.
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

function smokeCheck(%label, %condition)
{
	echo(%condition ? ("SMOKE PASS: " @ %label) : ("SMOKE FAIL: " @ %label));
}

function slotIs(%label, %ctrl, %field, %expected)
{
	%actual = %ctrl.getFieldValue(%field);
	smokeCheck(%label @ " (" @ %field @ " = " @ %actual @ ")", %actual $= %expected);
}

schedule(2000, 0, "smokeStep1");

function smokeStep1()
{
	smokeCheck("engine created GuiDefaultProfile", isObject(GuiDefaultProfile));
	smokeCheck("engine created GuiDefaultBorderProfile", isObject(GuiDefaultBorderProfile));

	// Open PlanetX the way the project selector does.
	ProjectManager.setProjectFolder("PlanetX");
	ModuleDatabase.ScanModules("PlanetX");
	ModuleDatabase.LoadExplicit("AppCore", 1);

	smokeCheck("AppCore loaded the PlanetX theme", isObject(PlanetX));
	smokeCheck("theme member profile exists", isObject(PlanetXButtonProfile));
	smokeCheck("theme border member exists", isObject(PlanetXRimmedBorder));

	// Everything past here works with the theme by id: a bare identifier in
	// TorqueScript is a string, and the applier compares object handles.
	$SmokeTheme = nameToID("PlanetX");

	schedule(500, 0, "smokeStep2");
}

function smokeStep2()
{
	GuiEditor.open();
	smokeCheck("editor adopted a theme on open", GuiEditor.themeName !$= "");

	// A small tree covering the interesting cases.
	$SmokeWindow = new GuiWindowCtrl() { Position = "10 10"; Extent = "300 200"; Text = "W"; };
	GuiEditor.rootGui.add($SmokeWindow);

	$SmokeButton = new GuiButtonCtrl() { Position = "10 10"; Extent = "80 30"; Text = "B"; };
	$SmokeWindow.add($SmokeButton);

	$SmokeScroll = new GuiScrollCtrl() { Position = "10 50"; Extent = "200 100"; };
	$SmokeWindow.add($SmokeScroll);

	$SmokeEdit = new GuiTextEditCtrl() { Position = "10 160"; Extent = "120 30"; };
	$SmokeWindow.add($SmokeEdit);

	// Bare GuiControls below the root: caption / paragraph / wrapper.
	$SmokeLabel = new GuiControl() { Position = "10 220"; Extent = "200 30"; Text = "a caption"; };
	$SmokeWindow.add($SmokeLabel);

	$SmokeParagraph = new GuiControl() { Position = "10 260"; Extent = "200 60"; Text = "a wrapping block of text"; textWrap = true; };
	$SmokeWindow.add($SmokeParagraph);

	$SmokeBox = new GuiControl() { Position = "10 330"; Extent = "200 60"; };
	$SmokeWindow.add($SmokeBox);

	// A bare GuiControl at the root of the Gui is the backdrop, so it takes Panel.
	$SmokeRoot = new GuiControl() { Position = "400 10"; Extent = "300 200"; };
	GuiEditor.rootGui.add($SmokeRoot);

	GuiEditor.setTheme($SmokeTheme, false);

	slotIs("window wears the Window profile", $SmokeWindow, "Profile", "PlanetXWindowProfile");
	slotIs("window content slot", $SmokeWindow, "contentProfile", "PlanetXWindowContentProfile");
	slotIs("window close slot", $SmokeWindow, "closeButtonProfile", "PlanetXWindowCloseButtonProfile");
	slotIs("window min slot", $SmokeWindow, "minButtonProfile", "PlanetXWindowButtonProfile");
	slotIs("button", $SmokeButton, "Profile", "PlanetXButtonProfile");
	slotIs("button tooltip slot", $SmokeButton, "tooltipProfile", "PlanetXTooltipProfile");
	slotIs("scroll", $SmokeScroll, "Profile", "PlanetXScrollProfile");
	slotIs("scroll thumb slot", $SmokeScroll, "thumbProfile", "PlanetXScrollThumbProfile");
	slotIs("scroll track slot", $SmokeScroll, "trackProfile", "PlanetXScrollTrackProfile");
	slotIs("scroll arrow slot", $SmokeScroll, "arrowProfile", "PlanetXScrollArrowProfile");
	slotIs("text edit", $SmokeEdit, "Profile", "PlanetXTextEditProfile");
	slotIs("captioned GuiControl is a Label", $SmokeLabel, "Profile", "PlanetXLabelProfile");
	slotIs("wrapping GuiControl is Empty", $SmokeParagraph, "Profile", "PlanetXEmptyProfile");
	slotIs("textless GuiControl is Empty", $SmokeBox, "Profile", "PlanetXEmptyProfile");
	slotIs("root GuiControl is a Panel", $SmokeRoot, "Profile", "PlanetXPanelProfile");

	schedule(500, 0, "smokeStep3");
}

function smokeStep3()
{
	// A stand-alone profile survives an apply unless the apply overrides it.
	%library = GuiEditor.getThemeLibrary();
	$SmokeStandalone = %library.createStandalone("SmokeStandaloneProfile");
	smokeCheck("stand alone profile created", isObject($SmokeStandalone));

	$SmokeButton.setEditFieldValue("Profile", "SmokeStandaloneProfile");
	GuiEditor.setTheme($SmokeTheme, false);
	slotIs("stand alone survives a normal apply", $SmokeButton, "Profile", "SmokeStandaloneProfile");

	GuiEditor.setTheme($SmokeTheme, true);
	slotIs("stand alone yields when overridden", $SmokeButton, "Profile", "PlanetXButtonProfile");

	// An extra profile in the right category is a deliberate choice and survives.
	$SmokeExtra = %library.createExtraProfile($SmokeTheme, "Button");
	smokeCheck("extra profile created", isObject($SmokeExtra));
	$SmokeButton.setEditFieldValue("Profile", $SmokeExtra.getName());
	GuiEditor.setTheme($SmokeTheme, false);
	slotIs("extra in the same category survives", $SmokeButton, "Profile", $SmokeExtra.getName());

	// A profile from this theme in another category was still a deliberate choice.
	$SmokeButton.setEditFieldValue("Profile", "PlanetXPanelProfile");
	GuiEditor.setTheme($SmokeTheme, false);
	slotIs("this theme's profile is never second-guessed", $SmokeButton, "Profile", "PlanetXPanelProfile");
	$SmokeButton.setEditFieldValue("Profile", "PlanetXButtonProfile");

	schedule(500, 0, "smokeStepSwitch");
}

// Switching themes carries each slot's category across, whatever this editor
// would otherwise have guessed for that control.
function smokeStepSwitch()
{
	%library = GuiEditor.getThemeLibrary();
	$SmokeThemeB = %library.createTheme("SmokeThemeB");
	smokeCheck("second theme created", isObject($SmokeThemeB));

	// A bare GuiControl deliberately wearing WindowContent - the class rules would
	// have said Empty.
	$SmokeCarried = new GuiControl() { Position = "400 300"; Extent = "200 60"; };
	GuiEditor.rootGui.add($SmokeCarried);
	$SmokeCarried.setEditFieldValue("Profile", PlanetXWindowContentProfile.getId());

	GuiEditor.setTheme($SmokeThemeB, false);
	slotIs("category carried to the new theme", $SmokeCarried, "Profile", "SmokeThemeBWindowContentProfile");
	slotIs("window followed the switch", $SmokeWindow, "Profile", "SmokeThemeBWindowProfile");
	slotIs("scroll thumb followed the switch", $SmokeScroll, "thumbProfile", "SmokeThemeBScrollThumbProfile");

	// ...and back, so the rest of the run is on the PlanetX theme again.
	GuiEditor.setTheme($SmokeTheme, false);
	slotIs("and carries back again", $SmokeCarried, "Profile", "PlanetXWindowContentProfile");

	%library.deleteTheme($SmokeThemeB);
	$SmokeCarried.delete();

	schedule(500, 0, "smokeStep4");
}

function smokeStep4()
{
	// A dropped control joins the Gui's theme on arrival.
	%dropped = new GuiCheckBoxCtrl() { Position = "10 400"; Extent = "120 30"; Text = "C"; };
	GuiEditor.rootGui.add(%dropped);
	GuiEditor.themeApplier.applyToBranch(%dropped, $SmokeTheme, false);
	slotIs("dropped control is themed", %dropped, "Profile", "PlanetXCheckBoxProfile");

	// Save records the theme; reopening reads it back.
	%path = pathConcat(getMainDotCsDir(), "smokeThemeApply.gui.taml");
	GuiEditor.SaveCore(%path, 1, "", "");
	smokeCheck("saved the gui", isFile(%path));
	smokeCheck("theme recorded on the saved root", GuiEditor.rootGui.guiTheme $= "PlanetX");

	// The recorded name has to survive the write: a GuiControl clears
	// canSaveDynamicFields in its constructor, so SaveCore turns it back on.
	%saved = TAMLRead(%path);
	smokeCheck("theme name is in the saved file", %saved.guiTheme $= "PlanetX");

	GuiEditor.themeName = "";
	GuiEditor.DisplayGuiContent(%saved, true);
	smokeCheck("reopened gui restored its theme", GuiEditor.themeName $= "PlanetX");
	fileDelete(%path);

	// Detaching is what keeps a Profile Editor revert from freeing profiles the
	// Gui is still wearing.
	%detached = GuiEditor.themeApplier.detach(GuiEditor.rootGui, $SmokeTheme, 0);
	smokeCheck("detach moved slots off the theme (" @ %detached @ ")", %detached > 0);

	schedule(500, 0, "smokeDone");
}

function smokeDone()
{
	echo("SMOKE DONE");
	quit();
}
