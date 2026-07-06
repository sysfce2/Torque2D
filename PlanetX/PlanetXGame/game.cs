//-----------------------------------------------------------------------------
// PlanetX - a twin-stick demo game for Torque2D 4.0.
//
// Game flow: title -> playing -> won/lost -> title. This file owns the module
// lifecycle and the state machine; the scripts folder owns the pieces.
//-----------------------------------------------------------------------------

function PlanetXGame::create(%this)
{
	exec("./gui/titleScreen.cs");
	exec("./scripts/level.cs");
	exec("./scripts/player.cs");
	exec("./scripts/input.cs");
	exec("./scripts/bullet.cs");
	exec("./scripts/alien.cs");
	exec("./scripts/crystal.cs");
	exec("./scripts/hud.cs");
	exec("./scripts/behaviors/chaseBehavior.cs");
	exec("./scripts/behaviors/takesDamageBehavior.cs");

	%this.createChaseBehaviorTemplate();
	%this.createTakesDamageBehaviorTemplate();

	// ScreenFade posts SwapComplete when a canvas transition finishes.
	%this.startListening(ScreenFade);

	// Neutral placeholder shown while a level is being torn down and rebuilt.
	%this.blankGui = new GuiControl()
	{
		Profile = "GuiDefaultProfile";
		HorizSizing = "relative";
		VertSizing = "relative";
		Position = "0 0";
		Extent = "1024 768";
	};

	$PlanetX::state = "";
	%this.showTitle(true);
	echo("PlanetX: title screen ready (" @ getEngineVersion() @ ")");
}

function PlanetXGame::destroy(%this)
{
	%this.stopListening(ScreenFade);
	%this.teardownLevel();

	if (isObject(%this.titleGui))
		%this.titleGui.delete();
	if (isObject(%this.victoryGui))
		%this.victoryGui.delete();
	if (isObject(%this.gameOverGui))
		%this.gameOverGui.delete();
	if (isObject(%this.blankGui))
		%this.blankGui.delete();
}

//-----------------------------------------------------------------------------
// State transitions.
//-----------------------------------------------------------------------------

/// Show the title screen. %instant skips the fade (used on first boot when
/// there is no previous canvas content to fade from).
function PlanetXGame::showTitle(%this, %instant)
{
	$PlanetX::state = "title";

	%title = %this.buildTitleScreen();

	if (%instant)
		Canvas.setContent(%title);
	else
		ScreenFade.swapCanvas(%title, "48 0 34", 800);

	Canvas.showCursor();
	Audio.PlayMusic("PlanetXGame:planetfall");
}

/// A fresh run from the title screen always begins at level 1.
function PlanetXGame::startGame(%this)
{
	if ($PlanetX::state $= "playing")
		return;

	%this.level = 1;
	%this.launchLevel();
}

/// Build and enter the current level (used by new runs, retries, and
/// level-to-level advancement).
function PlanetXGame::launchLevel(%this)
{
	$PlanetX::state = "playing";

	%this.applyDifficulty();
	%this.buildLevel();
	ScreenFade.swapCanvas(PlanetXRoot, "48 0 34", 800);

	echo("PlanetX: level" SPC %this.level SPC "started -" SPC PlanetXScene.getCount() SPC "scene objects");
}

/// Crystal secured: tear the level down and build the next, harder one.
function PlanetXGame::nextLevel(%this)
{
	%this.activeDialog.postEvent("dialogClose");

	// Park the canvas on a blank control so the old level can be deleted.
	Canvas.setContent(%this.blankGui);
	$PlanetX::state = "";
	%this.teardownLevel();

	%this.level++;
	%this.launchLevel();
}

function PlanetXGame::returnToTitle(%this)
{
	if ($PlanetX::state $= "title")
		return;

	// Stop input immediately; the level itself is torn down once the fade
	// has moved the canvas off it (see onSwapComplete).
	%this.popControls();
	%this.teardownPending = true;
	%this.showTitle(false);
}

/// ScreenFade callback: a canvas swap finished.
function PlanetXGame::onSwapComplete(%this)
{
	if (%this.teardownPending)
	{
		%this.teardownPending = false;
		%this.teardownLevel();
	}
}

//-----------------------------------------------------------------------------
// Winning.
//-----------------------------------------------------------------------------

function PlanetXGame::onWin(%this)
{
	if ($PlanetX::state !$= "playing")
		return;

	$PlanetX::state = "won";

	PlanetXScene.setScenePause(true);
	%this.popControls();

	if (!isObject(%this.victoryGui))
		%this.victoryGui = TamlRead(expandPath("^PlanetXGame/gui/victoryGui.gui.taml"));

	VictoryHeading.setText("LEVEL" SPC %this.level SPC "CLEARED");

	%this.activeDialog = %this.victoryGui;
	ScreenFade.openDialog(%this.victoryGui, "48 0 34 220", 400);
}

//-----------------------------------------------------------------------------
// Losing.
//-----------------------------------------------------------------------------

function PlanetXGame::onPlayerDeath(%this)
{
	if ($PlanetX::state !$= "playing")
		return;

	$PlanetX::state = "lost";

	%this.playBurst(%this.player.getPosition());
	%this.player.setVisible(false);

	PlanetXScene.setScenePause(true);
	%this.popControls();

	if (!isObject(%this.gameOverGui))
		%this.gameOverGui = TamlRead(expandPath("^PlanetXGame/gui/gameOverGui.gui.taml"));

	%this.activeDialog = %this.gameOverGui;
	ScreenFade.openDialog(%this.gameOverGui, "48 0 34 220", 400);
}

/// Death retry: replay the CURRENT level (fresh seed, same difficulty).
function PlanetXGame::retryMission(%this)
{
	%this.activeDialog.postEvent("dialogClose");

	// Park the canvas on a blank control so the old level can be deleted,
	// then rebuild from scratch.
	Canvas.setContent(%this.blankGui);
	$PlanetX::state = "";
	%this.teardownLevel();
	%this.launchLevel();
}

function PlanetXGame::dialogToTitle(%this)
{
	%this.activeDialog.postEvent("dialogClose");
	%this.teardownPending = true;
	%this.showTitle(false);
}

//-----------------------------------------------------------------------------

/// Destroy every gameplay object so the level can be rebuilt from scratch.
function PlanetXGame::teardownLevel(%this)
{
	%this.popControls();

	if (isObject(PlanetXRoot))
		PlanetXRoot.delete();

	if (isObject(PlanetXScene))
		PlanetXScene.delete();

	%this.player = "";
	%this.tileMap = "";
	%this.crosshair = "";
	%this.steamPlayer = "";
}
