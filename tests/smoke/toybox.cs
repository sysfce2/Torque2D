// Temporary regression check: the toybox has its own complete profile set in
// Sandbox and must be untouched by AppCore losing its profiles.
setRandomSeed();
setLogMode(2);
setScriptExecEcho(false);
trace(false);
$Scripts::ignoreDSOs = true;
setCompanyAndProduct("Torque Game Engines", "Torque2D");
ModuleDatabase.EchoInfo = false;
AssetDatabase.EchoInfo = false;

ModuleDatabase.scanModules(testRoot("toybox"));
ModuleDatabase.LoadExplicit("AppCore");

function smokeCheck(%label, %condition)
{
	echo(%condition ? ("SMOKE PASS: " @ %label) : ("SMOKE FAIL: " @ %label));
}

createPath(testRoot("shots/"));
schedule(5000, 0, "toySmoke");

function toySmoke()
{
	smokeCheck("engine GuiDefaultProfile exists", isObject(GuiDefaultProfile));
	smokeCheck("Sandbox still defines its own profiles", isObject(GuiButtonProfile) && isObject(GuiWindowProfile));
	smokeCheck("sandbox window is up", isObject(SandboxWindow));
	screenShot(testRoot("shots/toyboxThemeSmoke.png"), "PNG");
	schedule(1000, 0, "toySmokeDone");
}

function toySmokeDone()
{
	echo("SMOKE DONE");
	quit();
}
