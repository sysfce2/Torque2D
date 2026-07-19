//-----------------------------------------------------------------------------
// PlanetXInput: the level's input controller, a manager ScriptObject. The level
// hands it a direct reference to itself (%this.level) so it works even while the
// level is still building and PlanetXGame.level is not yet assigned.
//
// Keyboard: an ActionMap built from the saved bindings (PlanetXSettings), pushed
// in onAdd and popped in onRemove. Each player has its own move keys and a fire
// key; rebuilding the map (rebuildMoveMap) picks up rebindings live. The bound
// handlers must be global functions (the engine calls them by bare name), so they
// stay global and just forward to the right player.
//
// Aiming is per-player and configurable (PlanetXSettings P1Aim/P2Aim):
//   - "mouse": the player follows the cursor and fires with the mouse. Only one
//     player can hold the mouse (there is a single cursor); %this.mousePlayer is
//     the index that does, or 0 for none. The last cursor position is kept as a
//     *window* point and re-projected into the world every aim tick, so aim and
//     crosshair stay correct while the camera moves even when the mouse doesn't.
//   - "auto": the player points at the nearest alien in range each tick and fires
//     with its fire key.
//
// Pause: pauseGame keeps $PlanetX::state == "playing" and sets $PlanetX::paused;
// the aim loop keeps rescheduling but skips its work, and the move/fire handlers
// early-return, so the level freezes without any loop having to be restarted on
// resume. See game.cs. See TORQUE_SCRIPT.md.
//-----------------------------------------------------------------------------

$PlanetX::AimTickMs = 32;

// Auto-aim only locks onto aliens within this range; past it a player holds its
// last heading instead of pointing at something across the whole map.
$PlanetX::AutoAimRange = 30;

function PlanetXInput::onAdd(%this)
{
	%this.buildMoveMap();

	PlanetXWindow.addInputListener(%this);

	// Start with the aim slightly right of the window center (the camera is
	// centered on the player, so this reads as "straight ahead"). Window
	// coordinates stay valid while the camera moves.
	$PlanetX::aimWindow = "612 384";

	// Set each player's aim mode from prefs and work out who holds the mouse.
	%this.applyAimModes();

	// Hide the OS cursor (the crosshair sprite replaces it). Note: hideCursor,
	// NOT cursorOff - cursorOff stops the canvas from processing mouse events
	// entirely (guiCanvas.cc gates mouse handling on the cursor being on).
	Canvas.hideCursor();
	%this.aimTick();
}

function PlanetXInput::onRemove(%this)
{
	%this.stopFiring();

	if (isEventPending(%this.aimEvent))
		cancel(%this.aimEvent);

	if (isObject(PlanetXWindow))
		PlanetXWindow.removeInputListener(%this);

	Canvas.showCursor();

	if (isObject(%this.moveMap))
	{
		%this.moveMap.pop();
		%this.moveMap.delete();
	}
}

//-----------------------------------------------------------------------------
// Key bindings. Built from the saved prefs so a rebind takes effect on the next
// buildMoveMap. Player 1 is always bound; player 2 only in co-op. Escape is fixed
// (not rebindable - the key-capture control refuses to assign it).
//-----------------------------------------------------------------------------

function PlanetXInput::buildMoveMap(%this)
{
	%s = PlanetXGame.settings;
	%map = new ActionMap();

	%map.bind("keyboard", %s.get("P1Up"),    "planetXP1Up");
	%map.bind("keyboard", %s.get("P1Down"),  "planetXP1Down");
	%map.bind("keyboard", %s.get("P1Left"),  "planetXP1Left");
	%map.bind("keyboard", %s.get("P1Right"), "planetXP1Right");
	%map.bind("keyboard", %s.get("P1Fire"),  "planetXP1Fire");

	if ($PlanetX::twoPlayer)
	{
		%map.bind("keyboard", %s.get("P2Up"),    "planetXP2Up");
		%map.bind("keyboard", %s.get("P2Down"),  "planetXP2Down");
		%map.bind("keyboard", %s.get("P2Left"),  "planetXP2Left");
		%map.bind("keyboard", %s.get("P2Right"), "planetXP2Right");
		%map.bind("keyboard", %s.get("P2Fire"),  "planetXP2Fire");
	}

	%map.bind("keyboard", "escape", "planetXEscape");
	%map.push();
	%this.moveMap = %map;
}

/// Rebuild the live map from the current prefs (called after a rebinding while a
/// level is running).
function PlanetXInput::rebuildMoveMap(%this)
{
	if (isObject(%this.moveMap))
	{
		%this.moveMap.pop();
		%this.moveMap.delete();
	}
	%this.buildMoveMap();
}

/// Convenience: the players own their triggers; input just relays. Stops both so
/// a teardown or a pause mid-fire leaves no weapon looping.
function PlanetXInput::stopFiring(%this)
{
	%level = %this.level;
	if (!isObject(%level))
		return;

	if (isObject(%level.player))
		%level.player.stopFiring();
	if (isObject(%level.player2))
		%level.player2.stopFiring();
}

//-----------------------------------------------------------------------------
// Key handlers. These MUST be global (ActionMap bind targets). %val is 1 on
// press, 0 on release. Each forwards to its player; all early-return while paused
// so a key pressed over the pause dialog never mutates player state.
//-----------------------------------------------------------------------------

function planetXP1Up(%val)    { if ($PlanetX::paused) return; %p = PlanetXGame.level.player; if (isObject(%p)) { %p.inUp = %val;    %p.updateVelocity(); } }
function planetXP1Down(%val)  { if ($PlanetX::paused) return; %p = PlanetXGame.level.player; if (isObject(%p)) { %p.inDown = %val;  %p.updateVelocity(); } }
function planetXP1Left(%val)  { if ($PlanetX::paused) return; %p = PlanetXGame.level.player; if (isObject(%p)) { %p.inLeft = %val;  %p.updateVelocity(); } }
function planetXP1Right(%val) { if ($PlanetX::paused) return; %p = PlanetXGame.level.player; if (isObject(%p)) { %p.inRight = %val; %p.updateVelocity(); } }

function planetXP2Up(%val)    { if ($PlanetX::paused) return; %p = PlanetXGame.level.player2; if (isObject(%p)) { %p.inUp = %val;    %p.updateVelocity(); } }
function planetXP2Down(%val)  { if ($PlanetX::paused) return; %p = PlanetXGame.level.player2; if (isObject(%p)) { %p.inDown = %val;  %p.updateVelocity(); } }
function planetXP2Left(%val)  { if ($PlanetX::paused) return; %p = PlanetXGame.level.player2; if (isObject(%p)) { %p.inLeft = %val;  %p.updateVelocity(); } }
function planetXP2Right(%val) { if ($PlanetX::paused) return; %p = PlanetXGame.level.player2; if (isObject(%p)) { %p.inRight = %val; %p.updateVelocity(); } }

function planetXP1Fire(%val)
{
	if ($PlanetX::paused)
		return;

	%p = PlanetXGame.level.player;
	if (!isObject(%p) || %p.downed)
		return;

	if (%val)
		%p.startFiring();
	else
		%p.stopFiring();
}

function planetXP2Fire(%val)
{
	if ($PlanetX::paused)
		return;

	%p = PlanetXGame.level.player2;
	if (!isObject(%p) || %p.downed)
		return;

	if (%val)
		%p.startFiring();
	else
		%p.stopFiring();
}

function planetXEscape(%val)
{
	if (!%val)
		return;

	// While the options window is up, Esc backs out of it rather than toggling
	// pause. Deferred one tick (like the toggle below) so the dialog swap does not
	// run from inside a bound handler mid-dispatch.
	if ($PlanetX::optionsOpen)
	{
		PlanetXGame.schedule(1, "closeOptions");
		return;
	}

	if ($PlanetX::state !$= "playing")
		return;

	// Toggle: pause if running, resume if already paused. Deferred one tick so we
	// don't post dialog events or touch the ActionMap from inside one of its own
	// bound handlers while the key event is still dispatching.
	if (!$PlanetX::paused)
		PlanetXGame.schedule(1, "pauseGame");
	else
		PlanetXGame.schedule(1, "resumeGame");
}

//-----------------------------------------------------------------------------
// Mouse handlers - always drive whichever player holds the mouse (mousePlayer).
// %worldPosition arrives already converted to world space. All no-op while paused
// (and the modal pause dialog captures the cursor anyway).
//-----------------------------------------------------------------------------

function PlanetXInput::onTouchDown(%this, %touchID, %worldPosition)
{
	if ($PlanetX::paused)
		return;

	%this.setAimFromWorld(%worldPosition);

	%player = %this.mousePlayerObject();
	if (isObject(%player) && !%player.downed)
		%player.startFiring();
}

function PlanetXInput::onTouchUp(%this, %touchID, %worldPosition)
{
	%player = %this.mousePlayerObject();
	if (isObject(%player))
		%player.stopFiring();
}

function PlanetXInput::onTouchMoved(%this, %touchID, %worldPosition)
{
	if ($PlanetX::paused)
		return;
	%this.setAimFromWorld(%worldPosition);
}

function PlanetXInput::onTouchDragged(%this, %touchID, %worldPosition)
{
	if ($PlanetX::paused)
		return;
	%this.setAimFromWorld(%worldPosition);
}

//-----------------------------------------------------------------------------
// Aiming.
//-----------------------------------------------------------------------------

/// Read each player's aim mode from prefs and decide who holds the mouse. Prefer
/// player 1 if both are set to mouse (there is only one cursor); 0 = nobody. Also
/// toggles the crosshair, which only makes sense when someone aims with the mouse.
function PlanetXInput::applyAimModes(%this)
{
	%level = %this.level;
	if (!isObject(%level))
		return;

	%s = PlanetXGame.settings;

	if (isObject(%level.player))
		%level.player.aimMode = %s.get("P1Aim");
	if (isObject(%level.player2))
		%level.player2.aimMode = %s.get("P2Aim");

	%this.mousePlayer = 0;
	if (isObject(%level.player) && %level.player.aimMode $= "mouse")
		%this.mousePlayer = 1;
	else if (isObject(%level.player2) && %level.player2.aimMode $= "mouse")
		%this.mousePlayer = 2;

	if (isObject(%level.crosshair))
		%level.crosshair.setVisible(%this.mousePlayer != 0);
}

/// The player that currently holds the mouse, or "" if none.
function PlanetXInput::mousePlayerObject(%this)
{
	%level = %this.level;
	if (!isObject(%level))
		return "";

	if (%this.mousePlayer == 1)
		return %level.player;
	if (%this.mousePlayer == 2)
		return %level.player2;
	return "";
}

function PlanetXInput::setAimFromWorld(%this, %worldPosition)
{
	if (!isObject(PlanetXWindow))
		return;

	$PlanetX::aimWindow = PlanetXWindow.getWindowPoint(%worldPosition);
	%this.updateAim();
}

/// Re-project the stored window point into the world, then point the mouse player
/// (and the crosshair) at it. No-op if nobody holds the mouse.
function PlanetXInput::updateAim(%this)
{
	if ($PlanetX::state !$= "playing" || $PlanetX::paused)
		return;

	%player = %this.mousePlayerObject();
	if (!isObject(%player))
		return;

	%world = PlanetXWindow.getWorldPoint($PlanetX::aimWindow);

	%level = %this.level;
	if (isObject(%level.crosshair))
		%level.crosshair.setPosition(%world);

	%player.setAim(mAtan(Vector2Sub(%world, %player.getPosition())));
}

/// Point an auto-aim player at the nearest alien in range (firing rides on the aim
/// angle, so holding its fire key shoots whatever is closest).
function PlanetXInput::autoAimPlayer(%this, %player)
{
	if (!isObject(%player) || %player.downed)
		return;

	%level = %this.level;
	%enemy = %level.nearestEnemy(%player.getPosition(), $PlanetX::AutoAimRange);
	if (isObject(%enemy))
		%player.setAim(mAtan(Vector2Sub(%enemy.getPosition(), %player.getPosition())));
}

function PlanetXInput::aimTick(%this)
{
	if ($PlanetX::state !$= "playing")
		return;

	// Keep the loop alive across a pause (state stays "playing"); just skip the aim
	// work while frozen.
	if (!$PlanetX::paused)
	{
		%level = %this.level;
		if (isObject(%level))
		{
			%this.updateAim();

			if (isObject(%level.player) && %level.player.aimMode $= "auto")
				%this.autoAimPlayer(%level.player);
			if (isObject(%level.player2) && %level.player2.aimMode $= "auto")
				%this.autoAimPlayer(%level.player2);
		}
	}

	%this.aimEvent = %this.schedule($PlanetX::AimTickMs, "aimTick");
}

//-----------------------------------------------------------------------------
// Pause / resume. The ActionMap stays pushed through a pause so Esc still toggles;
// only the handlers and the aim work are gated (on $PlanetX::paused).
//-----------------------------------------------------------------------------

function PlanetXInput::pause(%this)
{
	// Stop any in-progress fire and zero every held-move flag, so a key held across
	// the pause doesn't drive motion on resume (the scene is frozen regardless).
	%this.stopFiring();

	%level = %this.level;
	if (isObject(%level))
	{
		if (isObject(%level.player))
			%this.clearMovement(%level.player);
		if (isObject(%level.player2))
			%this.clearMovement(%level.player2);
	}

	// The crosshair hides the OS cursor during play; show it so the dialog buttons
	// are clickable.
	Canvas.showCursor();
}

function PlanetXInput::resume(%this)
{
	Canvas.hideCursor();

	// Bindings and aim modes may have changed in the options screen while paused.
	if ($PlanetX::bindingsDirty)
	{
		%this.rebuildMoveMap();
		$PlanetX::bindingsDirty = false;
	}
	%this.applyAimModes();
	// aimTick kept rescheduling through the pause (gated), so there is nothing to
	// restart here.
}

/// Zero a player's held-move flags and settle its velocity/animation to idle.
function PlanetXInput::clearMovement(%this, %player)
{
	%player.inUp = 0;
	%player.inDown = 0;
	%player.inLeft = 0;
	%player.inRight = 0;
	%player.updateVelocity();
}
