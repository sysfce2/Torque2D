//-----------------------------------------------------------------------------
// Temporary runtime smoke test: boots the PlanetX demo the way a shipped game
// does (no editor), checks the theme supplies what the GUIs ask for, grabs a
// screenshot of the title screen, and quits.
//-----------------------------------------------------------------------------

setLogMode(2);
setScriptExecEcho(false);
trace(false);
$Scripts::ignoreDSOs = true;
setCompanyAndProduct("Torque Game Engines", "Torque2D");
ModuleDatabase.EchoInfo = false;
AssetDatabase.EchoInfo = false;

ModuleDatabase.scanModules(testRoot("PlanetX"));
ModuleDatabase.LoadExplicit("AppCore");

function smokeCheck(%label, %condition)
{
	echo(%condition ? ("SMOKE PASS: " @ %label) : ("SMOKE FAIL: " @ %label));
}

createPath(testRoot("shots/"));
schedule(4000, 0, "planetXSmoke");

function planetXSmoke()
{
	smokeCheck("engine GuiDefaultProfile exists", isObject(GuiDefaultProfile));
	smokeCheck("AppCore no longer creates GuiButtonProfile", !isObject(GuiButtonProfile));
	smokeCheck("AppCore no longer creates GuiWindowProfile", !isObject(GuiWindowProfile));
	smokeCheck("AppCore still creates its cursors", isObject(DefaultCursor) && isObject(EditCursor));

	smokeCheck("the PlanetX theme loaded", isObject(PlanetX));
	smokeCheck("theme button profile", isObject(PlanetXButtonProfile));
	smokeCheck("theme window profile", isObject(PlanetXWindowProfile));
	smokeCheck("theme progress profile", isObject(PlanetXProgressProfile));
	smokeCheck("theme condenser borders", isObject(PlanetXCondenserLightBorder) && isObject(PlanetXCondenserDarkBorder));
	smokeCheck("theme slider thumb profile", isObject(PlanetXSliderThumbProfile));

	// The two profiles that are not just a look now live in the theme as extras,
	// and the six font-size clones are gone entirely (controls use FontSizeAdjust).
	smokeCheck("heat bar profile is a theme extra", isObject(PlanetXHeatProfile) && PlanetXHeatProfile.category !$= "");
	smokeCheck("heat bar keeps its coral highlight", PlanetXHeatProfile.fillColorHL $= "234 72 72 255");
	smokeCheck("heat bar tracks the theme's background", PlanetXHeatProfile.fillColor $= PlanetXProgressProfile.fillColor);
	smokeCheck("key-capture profile is a theme extra", isObject(PlanetXCaptureProfile) && PlanetXCaptureProfile.canKeyFocus);
	smokeCheck("no cloned menu profiles remain", !isObject(PlanetXBigButtonProfile) && !isObject(PlanetXCardTitleProfile));

	%window = new GuiWindowCtrl();
	smokeCheck("windows default to a 28px title bar", %window.titleHeight == 28);
	%window.delete();

	smokeCheck("the title screen is up", isObject(PlanetXTitle));
	smokeCheck("title wears a theme profile", PlanetXTitle.getFieldValue("Profile") $= "PlanetXEmptyProfile");

	screenShot(testRoot("shots/planetXThemeSmoke.png"), "PNG");
	schedule(1000, 0, "planetXSmokeDone");
}

function planetXSmokeDone()
{
	echo("SMOKE DONE");
	quit();
}
