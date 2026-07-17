//-----------------------------------------------------------------------------
// PlanetX - a twin-stick demo game for Torque2D 4.0.
//
// Game flow: title -> playing -> won/lost -> title. This file is the module
// singleton and owns ONLY the state machine and the two things that outlive a
// single level: the title screen and the win/lose dialogs. Everything that
// belongs to a running level lives on the PlanetXLevel object (%this.level),
// which builds and tears down its own world. See TORQUE_SCRIPT.md.
//-----------------------------------------------------------------------------

// Hold the soundtrack under the SFX so effects cut through (music is channel 0,
// SFX channel 1). Lower this for quieter music, raise toward 1 for louder.
$PlanetX::MusicVolume = 0.6;

function PlanetXGame::create(%this)
{
	// One class per file; each is named for the class it defines.
	exec("./gui/titleScreen.cs");
	exec("./scripts/level.cs");
	exec("./scripts/sceneWindow.cs");
	exec("./scripts/tileMap.cs");
	exec("./scripts/barrier.cs");
	exec("./scripts/rock.cs");
	exec("./scripts/rocket.cs");
	exec("./scripts/crystal.cs");
	exec("./scripts/crosshair.cs");
	exec("./scripts/player.cs");
	exec("./scripts/weapon.cs");
	exec("./scripts/blaster.cs");
	exec("./scripts/bullet.cs");
	exec("./scripts/burst.cs");
	exec("./scripts/enemy.cs");
	exec("./scripts/bug.cs");
	exec("./scripts/brute.cs");
	exec("./scripts/hud.cs");
	exec("./scripts/input.cs");

	// ScreenFade posts SwapComplete when a canvas transition finishes.
	%this.startListening(ScreenFade);

	// The title screen owns its own GUI; it lives for the whole session.
	%this.titleScreen = new ScriptObject() { class = "PlanetXTitleScreen"; };

	// Neutral placeholder shown while a level is being torn down and rebuilt.
	%this.blankGui = new GuiControl()
	{
		Profile = "GuiDefaultProfile";
		HorizSizing = "relative";
		VertSizing = "relative";
		Position = "0 0";
		Extent = "1024 768";
	};

	// Set the music level under the SFX before the title track starts (persists
	// for the session; the shared Audio module defaults every channel to full).
	Audio.SetMusicVolume($PlanetX::MusicVolume);

	$PlanetX::state = "";
	%this.showTitle(true);
	echo("PlanetX: title screen ready (" @ getEngineVersion() @ ")");
}

function PlanetXGame::destroy(%this)
{
	%this.stopListening(ScreenFade);

	// Deleting the level cascades teardown of its whole world (see
	// PlanetXLevel::onRemove); the title screen frees its own GUI.
	if (isObject(%this.level))
		%this.level.delete();
	if (isObject(%this.titleScreen))
		%this.titleScreen.delete();

	if (isObject(%this.victoryGui))
		%this.victoryGui.delete();
	if (isObject(%this.gameOverGui))
		%this.gameOverGui.delete();
	if (isObject(%this.blankGui))
		%this.blankGui.delete();
}

/// Shared click for the menu and dialog buttons (their Command fields call this
/// before the action, so the asset id lives in one place).
function PlanetXGame::playClick(%this)
{
	Audio.PlaySound("PlanetXGame:uiClick");
}

//-----------------------------------------------------------------------------
// State transitions.
//-----------------------------------------------------------------------------

/// Show the title screen. %instant skips the fade (used on first boot when
/// there is no previous canvas content to fade from).
function PlanetXGame::showTitle(%this, %instant)
{
	$PlanetX::state = "title";

	%this.titleScreen.open(%instant);

	Canvas.showCursor();
	Audio.PlayMusic("PlanetXGame:planetfall");
}

/// A fresh run from the title screen always begins at level 1.
function PlanetXGame::startGame(%this)
{
	if ($PlanetX::state $= "playing")
		return;

	%this.levelNum = 1;
	%this.launchLevel();
}

/// Build and enter the current level (used by new runs, retries, and
/// level-to-level advancement). Creating the PlanetXLevel builds its world in
/// onAdd; we hold the object so deleting it later tears the world back down.
function PlanetXGame::launchLevel(%this)
{
	$PlanetX::state = "playing";

	%this.level = new ScriptObject()
	{
		class = "PlanetXLevel";
		number = %this.levelNum;
	};

	ScreenFade.swapCanvas(PlanetXRoot, "48 0 34", 800);

	echo("PlanetX: level" SPC %this.levelNum SPC "started -" SPC PlanetXScene.getCount() SPC "scene objects");
}

/// Crystal secured: tear the level down and build the next, harder one.
function PlanetXGame::nextLevel(%this)
{
	%this.activeDialog.postEvent("dialogClose");

	// Park the canvas on a blank control so the old level can be deleted.
	Canvas.setContent(%this.blankGui);
	$PlanetX::state = "";
	%this.teardownLevel();

	%this.levelNum++;
	%this.launchLevel();
}

function PlanetXGame::returnToTitle(%this)
{
	if ($PlanetX::state $= "title")
		return;

	// Stop input immediately; the level itself is torn down once the fade
	// has moved the canvas off it (see onSwapComplete).
	if (isObject(%this.level))
		%this.level.suspend();
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

	Audio.PlaySound("PlanetXGame:crystalGet");

	$PlanetX::state = "won";
	%this.level.suspend();

	if (!isObject(%this.victoryGui))
		%this.victoryGui = TamlRead(expandPath("^PlanetXGame/gui/victoryGui.gui.taml"));

	VictoryHeading.setText("LEVEL" SPC %this.levelNum SPC "CLEARED");

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

	Audio.PlaySound("PlanetXGame:playerDeath");

	$PlanetX::state = "lost";

	%this.level.playBurst(%this.level.player.getPosition());
	%this.level.player.setVisible(false);
	%this.level.suspend();

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

/// Destroy the current level. One delete cascades through PlanetXLevel::onRemove
/// to free the scene, HUD, input, and every object they own.
function PlanetXGame::teardownLevel(%this)
{
	if (isObject(%this.level))
		%this.level.delete();
	%this.level = "";
}
