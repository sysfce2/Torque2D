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

	// Cursors come from the theme now. The canonical names still answer -- the
	// engine hard-codes them for any control that names no cursor of its own --
	// but what they hold is a copy of the theme's member, art and tint included.
	smokeCheck("the theme owns a cursor per category",
		isObject(PlanetXDefaultCursor) && isObject(PlanetXEditCursor) && isObject(PlanetXNWSECursor));
	// Either the recipe's tint or one the project chose. Asserting it always
	// equals colorForeground would forbid the override the pane exists to make;
	// that the tint tracks the palette when NOT overridden is covered by
	// GuiProfileThemeTests and smoke/cursorPane.
	smokeCheck("cursor tint is the palette's, or a deliberate override",
		PlanetX.isFieldOverridden(PlanetXDefaultCursor, "color") ||
		PlanetXDefaultCursor.color $= PlanetX.colorForeground);
	smokeCheck("theme cursors have their own art",
		strstr(PlanetXDefaultCursor.bitmapName, "themes/cursors/PlanetX") >= 0);
	smokeCheck("the art was seeded beside the theme",
		isDirectory(testRoot("PlanetX/themes/cursors/PlanetX")));
	smokeCheck("the canonical names carry the theme's cursors",
		DefaultCursor.bitmapName $= PlanetXDefaultCursor.bitmapName &&
		DefaultCursor.hotSpot $= PlanetXDefaultCursor.hotSpot &&
		DefaultCursor.color $= PlanetXDefaultCursor.color);
	smokeCheck("an installed copy is not mistaken for a theme member", DefaultCursor.category $= "");

	// Counted relative to what the project's own theme file holds, which is the
	// user's to change: PlanetX already ships an extra Default cursor of its own.
	%before = getWordCount(PlanetX.getCursors("Default"));
	%extra = PlanetX.createCursor("Default");
	%extra.bitmapName = "unitTestArt/other.png";
	smokeCheck("a category can hold another cursor", getWordCount(PlanetX.getCursors("Default")) == (%before + 1));
	PlanetX.removeCursor(%extra);
	smokeCheck("an extra cursor can be removed again", getWordCount(PlanetX.getCursors("Default")) == %before);
	smokeCheck("the project's own extra cursor survived", %before >= 1 && isObject(PlanetXDefaultCursor2));

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
