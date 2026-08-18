// Temporary: walks PlanetX's script-built screens and screenshots each one, so
// the move from cloned font profiles to FontSizeAdjust can be eyeballed.
setLogMode(2);
setScriptExecEcho(false);
trace(false);
$Scripts::ignoreDSOs = true;
setCompanyAndProduct("Torque Game Engines", "Torque2D");
ModuleDatabase.EchoInfo = false;
AssetDatabase.EchoInfo = false;

ModuleDatabase.scanModules(testRoot("PlanetX"));
ModuleDatabase.LoadExplicit("AppCore");

createPath(testRoot("shots/"));
schedule(4000, 0, "shotOptions");

function shotOptions()
{
	PlanetXGame.openOptions();
	schedule(1500, 0, "grabOptions");
}

function grabOptions()
{
	screenShot(testRoot("shots/planetXOptions.png"), "PNG");
	PlanetXGame.closeOptions();
	schedule(1500, 0, "startLevel");
}

function startLevel()
{
	PlanetXGame.startGame(false);
	schedule(6000, 0, "grabHud");
}

function grabHud()
{
	screenShot(testRoot("shots/planetXHud.png"), "PNG");
	PlanetXGame.pauseGame();
	schedule(1500, 0, "grabPause");
}

function grabPause()
{
	screenShot(testRoot("shots/planetXPause.png"), "PNG");
	echo("SHOTS DONE");
	schedule(500, 0, "quit");
}
