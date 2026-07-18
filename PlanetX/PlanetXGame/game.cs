//-----------------------------------------------------------------------------
// PlanetX - a twin-stick demo game for Torque2D 4.0.
//
// Game flow: title -> playing -> won/lost -> title. This file is the module
// singleton and owns ONLY the state machine and the two things that outlive a
// single level: the title screen and the win/lose dialogs. Everything that
// belongs to a running level lives on the PlanetXLevel object (%this.level),
// which builds and tears down its own world. See TORQUE_SCRIPT.md.
//-----------------------------------------------------------------------------

function PlanetXGame::create(%this)
{
	// One class per file; each is named for the class it defines.
	exec("./scripts/settings.cs");
	exec("./gui/titleScreen.cs");
	exec("./gui/upgradeScreen.cs");
	exec("./gui/optionsScreen.cs");
	exec("./gui/pauseMenu.cs");
	exec("./gui/keyCapture.cs");
	exec("./scripts/level.cs");
	exec("./scripts/sceneWindow.cs");
	exec("./scripts/camera.cs");
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
	exec("./scripts/upgrades.cs");
	exec("./scripts/burst.cs");
	exec("./scripts/enemy.cs");
	exec("./scripts/bug.cs");
	exec("./scripts/brute.cs");
	exec("./scripts/hud.cs");
	exec("./scripts/input.cs");

	// ScreenFade posts SwapComplete when a canvas transition finishes.
	%this.startListening(ScreenFade);

	// Dedicated GUI profiles (larger text) - built before any PlanetX GUI.
	%this.createProfiles();

	// User settings (volumes, key bindings, aim mode). Created before any audio
	// plays or any GUI reads a binding; its onAdd seeds defaults, loads saved prefs
	// over them, and pushes the volume levels into the shared Audio module.
	%this.settings = new ScriptObject() { class = "PlanetXSettings"; };

	// The weapon-upgrade catalog and this run's upgrade state. A named session
	// singleton (like PlanetXWindow/PlanetXScene) so any file can reach it; reset()
	// at the start of each run clears it back to the stock blaster.
	new ScriptObject(PlanetXUpgrades) { class = "PlanetXUpgrades"; };

	// Two-player co-op starts off; the title's "START 2 PLAYERS" turns it on.
	$PlanetX::twoPlayer = false;

	// The title screen owns its own GUI; it lives for the whole session.
	%this.titleScreen = new ScriptObject() { class = "PlanetXTitleScreen"; };

	// The options window also lives for the whole session - it is opened from the
	// title and from the pause menu, and refreshed from prefs on each open.
	%this.optionsScreen = new ScriptObject() { class = "PlanetXOptionsScreen"; };

	// Neutral placeholder shown while a level is being torn down and rebuilt.
	%this.blankGui = new GuiControl()
	{
		Profile = "GuiDefaultProfile";
		HorizSizing = "relative";
		VertSizing = "relative";
		Position = "0 0";
		Extent = "1024 768";
	};

	// (Volume levels were applied by PlanetXSettings above.)
	$PlanetX::state = "";
	$PlanetX::paused = false;
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

	// The upgrade picker replaces the old victory dialog; it and the catalog
	// singleton outlive a level, so free them here.
	if (isObject(%this.upgradeScreen))
		%this.upgradeScreen.delete();
	if (isObject(PlanetXUpgrades))
		PlanetXUpgrades.delete();

	if (isObject(%this.gameOverGui))
		%this.gameOverGui.delete();
	if (isObject(%this.blankGui))
		%this.blankGui.delete();

	// The options and pause dialogs outlive a level; free them here.
	if (isObject(%this.optionsScreen))
		%this.optionsScreen.delete();
	if (isObject(%this.pauseMenu))
		%this.pauseMenu.delete();

	// Persist any last changes, then free the settings singleton.
	if (isObject(%this.settings))
	{
		%this.settings.save();
		%this.settings.delete();
	}
}

/// Shared click for the menu and dialog buttons (their Command fields call this
/// before the action, so the asset id lives in one place).
function PlanetXGame::playClick(%this)
{
	Audio.PlaySound("PlanetXGame:uiClick");
}

//-----------------------------------------------------------------------------
// GUI profiles. The stock GuiButton/Label/Text profiles are shared by the editor
// and every toy, so we don't touch them; instead we clone each into a PlanetX-
// owned profile at double the font size for readable in-game text. Built once and
// kept for the session (like PlanetXHeatProfile).
//-----------------------------------------------------------------------------

function PlanetXGame::createProfiles(%this)
{
	// GuiLabelProfile reliably carries the platform font size; double it. Fall back
	// to 24 if it is somehow unavailable, so text is never sized to zero.
	%big = 24;
	if (isObject(GuiLabelProfile) && GuiLabelProfile.fontSize > 0)
		%big = GuiLabelProfile.fontSize * 2;

	%this.makeBigProfile("PlanetXButtonProfile", GuiButtonProfile, %big);
	%this.makeBigProfile("PlanetXLabelProfile", GuiLabelProfile, %big);
	%this.makeBigProfile("PlanetXTextProfile", GuiTextProfile, %big);

	// Upgrade cards want smaller text than the big menu profiles: a title a touch
	// under the labels, a body smaller still, and a compact card button.
	%this.makeBigProfile("PlanetXCardTitleProfile", GuiLabelProfile, mRound(%big * 0.62));
	%this.makeBigProfile("PlanetXCardBodyProfile", GuiTextProfile, mRound(%big * 0.5));
	%this.makeBigProfile("PlanetXCardButtonProfile", GuiButtonProfile, mRound(%big * 0.5));
}

/// Clone %base into a named profile at %size font (no-op if it already exists).
function PlanetXGame::makeBigProfile(%this, %name, %base, %size)
{
	if (isObject(%name))
		return;

	%profile = new GuiControlProfile();
	%profile.assignFieldsFrom(%base);
	%profile.fontSize = %size;
	%profile.setName(%name);
}

//-----------------------------------------------------------------------------
// State transitions.
//-----------------------------------------------------------------------------

/// Show the title screen. %instant skips the fade (used on first boot when
/// there is no previous canvas content to fade from).
function PlanetXGame::showTitle(%this, %instant)
{
	// Leaving play for the title always clears the pause flag (Main Menu from the
	// pause dialog lands here).
	$PlanetX::paused = false;
	$PlanetX::optionsOpen = false;
	$PlanetX::state = "title";

	%this.titleScreen.open(%instant);

	Canvas.showCursor();
	Audio.PlayMusic("PlanetXGame:planetfall");
}

/// A fresh run from the title screen always begins at level 1. %twoPlayer picks
/// solo or co-op (the two START buttons pass it; see titleGui.gui.taml).
function PlanetXGame::startGame(%this, %twoPlayer)
{
	if ($PlanetX::state $= "playing")
		return;

	$PlanetX::twoPlayer = %twoPlayer;

	// A fresh run starts with the stock blaster - wipe last run's upgrades.
	PlanetXUpgrades.reset();

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
// Pause and options. ESC during play raises the pause dialog instead of quitting
// to the title. We keep $PlanetX::state == "playing" and freeze the world with a
// separate $PlanetX::paused flag, so the gameplay loops (aim, footsteps, auto-
// repair, co-op camera) keep rescheduling themselves and just skip their work
// while paused - nothing has to be restarted on resume. See input.cs.
//-----------------------------------------------------------------------------

function PlanetXGame::pauseGame(%this)
{
	if ($PlanetX::state !$= "playing" || $PlanetX::paused)
		return;

	$PlanetX::paused = true;
	%this.level.pause();

	// Built once and reused; it lives until the game shuts down (see destroy).
	if (!isObject(%this.pauseMenu))
		%this.pauseMenu = new ScriptObject() { class = "PlanetXPauseMenu"; };

	%this.activeDialog = %this.pauseMenu.dialog;
	ScreenFade.openDialog(%this.pauseMenu.dialog, "48 0 34 220", 300);
}

/// Continue: drop the dialog and unfreeze exactly where we left off.
function PlanetXGame::resumeGame(%this)
{
	if (!$PlanetX::paused)
		return;

	%this.pauseMenu.dialog.postEvent("dialogClose");
	%this.level.resume();
	$PlanetX::paused = false;
}

/// Open the options window over the title screen (Back closes it, revealing the
/// title again).
function PlanetXGame::openOptions(%this)
{
	%this.optionsContext = "title";
	$PlanetX::optionsOpen = true;

	%this.optionsScreen.refresh();
	%this.activeDialog = %this.optionsScreen.dialog;
	ScreenFade.openDialog(%this.optionsScreen.dialog, "48 0 34 220", 300);
}

/// Open the options window from the pause menu by SWAPPING the visible dialog, so
/// the pause backdrop stays up and Back can swap straight back to the pause menu.
function PlanetXGame::openOptionsFromPause(%this)
{
	%this.optionsContext = "pause";
	$PlanetX::optionsOpen = true;

	%this.optionsScreen.refresh();
	%this.activeDialog.postEvent("dialogSwap", %this.optionsScreen.dialog);
	%this.activeDialog = %this.optionsScreen.dialog;
}

/// Options Back: save the settings, then return to wherever it was opened from -
/// the title (close the dialog) or the pause menu (swap back to it).
function PlanetXGame::closeOptions(%this)
{
	$PlanetX::optionsOpen = false;
	%this.settings.save();

	if (%this.optionsContext $= "pause")
	{
		%this.activeDialog.postEvent("dialogSwap", %this.pauseMenu.dialog);
		%this.activeDialog = %this.pauseMenu.dialog;
	}
	else
		%this.activeDialog.postEvent("dialogClose");
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

	// Offer upgrades in place of the old victory dialog: two cards solo, three in
	// co-op (each player claims a different one). Build a fresh screen each win - the
	// offered set changes - and free the previous one, whose close animation is long
	// finished. The screen builds its dialog in onAdd from the fields set here.
	if (isObject(%this.upgradeScreen))
		%this.upgradeScreen.delete();

	%needed = $PlanetX::twoPlayer ? 3 : 2;
	%offered = PlanetXUpgrades.offer(%needed);
	%this.upgradeScreen = new ScriptObject()
	{
		class = "PlanetXUpgradeScreen";
		levelNum = %this.levelNum;
		offered = %offered;
	};

	%this.activeDialog = %this.upgradeScreen.dialog;
	ScreenFade.openDialog(%this.upgradeScreen.dialog, "48 0 34 220", 400);
}

//-----------------------------------------------------------------------------
// Losing.
//-----------------------------------------------------------------------------

/// A player's health hit zero. In single-player that ends the run. In co-op the
/// player is downed and out of play; a living teammate can revive them at the
/// rocket (see camera.cs / PlanetXLevel::revivePlayer), and the run only ends
/// once BOTH players are down.
function PlanetXGame::onPlayerDown(%this, %player)
{
	if ($PlanetX::state !$= "playing")
		return;

	// Single-player: a death is game over.
	if (!$PlanetX::twoPlayer)
	{
		%this.onPlayerDeath(%player);
		return;
	}

	// Co-op: take this player out of play.
	Audio.PlaySound("PlanetXGame:playerDeath");
	%this.level.playBurst(%player.getPosition());
	%player.goDown();

	// If the teammate is already down too, the run is over.
	if (%player.playerIndex == 1)
		%mate = %this.level.player2;
	else
		%mate = %this.level.player;

	if (!isObject(%mate) || %mate.downed)
		%this.onPlayerDeath(%player);
}

/// Terminal game over: burst on the fallen player, freeze the level, and show the
/// game-over dialog.
function PlanetXGame::onPlayerDeath(%this, %player)
{
	if ($PlanetX::state !$= "playing")
		return;

	$PlanetX::state = "lost";

	// Burst/hide the player that just fell - unless co-op already downed them.
	if (isObject(%player) && !%player.downed)
	{
		Audio.PlaySound("PlanetXGame:playerDeath");
		%this.level.playBurst(%player.getPosition());
		%player.setVisible(false);
	}

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
