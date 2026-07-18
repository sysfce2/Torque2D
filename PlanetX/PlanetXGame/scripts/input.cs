//-----------------------------------------------------------------------------
// PlanetXInput: the level's input controller, a manager ScriptObject.
//
// Keyboard: an ActionMap for movement, pushed in onAdd and popped in onRemove.
// Player 1 always drives the arrow keys; in single-player WASD mirrors them. In
// two-player co-op WASD drives player 2 and the space bar fires player 2's gun
// (player 2 aims itself - see updateAutoAim). The bound handlers must be global
// functions (the engine calls them by bare name), so they stay global and just
// forward to the right player, setting that player's own held-key flags.
//
// Mouse: this object listens to the scene window's touch events, and always
// aims/fires PLAYER 1. The last cursor position is kept as a *window* point and
// re-projected into the world every aim tick, so aim and crosshair stay correct
// while the camera moves even when the mouse doesn't.
//-----------------------------------------------------------------------------

$PlanetX::AimTickMs = 32;

// Player 2's auto-aim only locks onto aliens within this range; past it P2 holds
// its last heading instead of pointing at something across the whole map.
$PlanetX::AutoAimRange = 30;

function PlanetXInput::onAdd(%this)
{
	%map = new ActionMap();

	// Player 1 always drives the arrow keys (and aims/fires with the mouse).
	%map.bind("keyboard", "up", "planetXP1Up");
	%map.bind("keyboard", "down", "planetXP1Down");
	%map.bind("keyboard", "left", "planetXP1Left");
	%map.bind("keyboard", "right", "planetXP1Right");

	if ($PlanetX::twoPlayer)
	{
		// Player 2: WASD to move, space to fire (auto-aim does the aiming).
		%map.bind("keyboard", "w", "planetXP2Up");
		%map.bind("keyboard", "s", "planetXP2Down");
		%map.bind("keyboard", "a", "planetXP2Left");
		%map.bind("keyboard", "d", "planetXP2Right");
		%map.bind("keyboard", "space", "planetXP2Fire");
	}
	else
	{
		// Single player: WASD mirrors the arrow keys.
		%map.bind("keyboard", "w", "planetXP1Up");
		%map.bind("keyboard", "s", "planetXP1Down");
		%map.bind("keyboard", "a", "planetXP1Left");
		%map.bind("keyboard", "d", "planetXP1Right");
	}

	%map.bind("keyboard", "escape", "planetXEscape");
	%map.push();
	%this.moveMap = %map;

	PlanetXWindow.addInputListener(%this);

	// Start with the aim slightly right of the window center (the camera is
	// centered on the player, so this reads as "straight ahead"). Window
	// coordinates stay valid while the camera moves.
	$PlanetX::aimWindow = "612 384";

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

/// Convenience: the players own their triggers; input just relays. Stops both so
/// a teardown mid-fire leaves no weapon looping.
function PlanetXInput::stopFiring(%this)
{
	%level = PlanetXGame.level;
	if (!isObject(%level))
		return;

	if (isObject(%level.player))
		%level.player.stopFiring();
	if (isObject(%level.player2))
		%level.player2.stopFiring();
}

//-----------------------------------------------------------------------------
// Key handlers. These MUST be global (ActionMap bind targets). %val is 1 on
// press, 0 on release. Each sets the held-key flag on its player and nudges it.
//-----------------------------------------------------------------------------

function planetXP1Up(%val)    { %p = PlanetXGame.level.player; if (isObject(%p)) { %p.inUp = %val;    %p.updateVelocity(); } }
function planetXP1Down(%val)  { %p = PlanetXGame.level.player; if (isObject(%p)) { %p.inDown = %val;  %p.updateVelocity(); } }
function planetXP1Left(%val)  { %p = PlanetXGame.level.player; if (isObject(%p)) { %p.inLeft = %val;  %p.updateVelocity(); } }
function planetXP1Right(%val) { %p = PlanetXGame.level.player; if (isObject(%p)) { %p.inRight = %val; %p.updateVelocity(); } }

function planetXP2Up(%val)    { %p = PlanetXGame.level.player2; if (isObject(%p)) { %p.inUp = %val;    %p.updateVelocity(); } }
function planetXP2Down(%val)  { %p = PlanetXGame.level.player2; if (isObject(%p)) { %p.inDown = %val;  %p.updateVelocity(); } }
function planetXP2Left(%val)  { %p = PlanetXGame.level.player2; if (isObject(%p)) { %p.inLeft = %val;  %p.updateVelocity(); } }
function planetXP2Right(%val) { %p = PlanetXGame.level.player2; if (isObject(%p)) { %p.inRight = %val; %p.updateVelocity(); } }

function planetXP2Fire(%val)
{
	%p = PlanetXGame.level.player2;
	if (!isObject(%p))
		return;

	if (%val && !%p.downed)
		%p.startFiring();
	else
		%p.stopFiring();
}

function planetXEscape(%val)
{
	// Deferred one tick: returnToTitle deletes the input controller (and its
	// ActionMap), and deleting the map from inside one of its own bound handlers
	// would be a use-after-free when the key event finishes dispatching.
	if (%val)
		PlanetXGame.schedule(1, "returnToTitle");
}

//-----------------------------------------------------------------------------
// Mouse handlers - always player 1. %worldPosition arrives already converted to
// world space.
//-----------------------------------------------------------------------------

function PlanetXInput::onTouchDown(%this, %touchID, %worldPosition)
{
	%this.setAimFromWorld(%worldPosition);

	%player = PlanetXGame.level.player;
	if (isObject(%player))
		%player.startFiring();
}

function PlanetXInput::onTouchUp(%this, %touchID, %worldPosition)
{
	%player = PlanetXGame.level.player;
	if (isObject(%player))
		%player.stopFiring();
}

function PlanetXInput::onTouchMoved(%this, %touchID, %worldPosition)
{
	%this.setAimFromWorld(%worldPosition);
}

function PlanetXInput::onTouchDragged(%this, %touchID, %worldPosition)
{
	%this.setAimFromWorld(%worldPosition);
}

//-----------------------------------------------------------------------------
// Aiming.
//-----------------------------------------------------------------------------

function PlanetXInput::setAimFromWorld(%this, %worldPosition)
{
	if (!isObject(PlanetXWindow))
		return;

	$PlanetX::aimWindow = PlanetXWindow.getWindowPoint(%worldPosition);
	%this.updateAim();
}

/// Re-project the stored window point into the world, then point PLAYER 1 (and
/// the crosshair) at it.
function PlanetXInput::updateAim(%this)
{
	if ($PlanetX::state !$= "playing")
		return;

	%level = PlanetXGame.level;
	if (!isObject(%level) || !isObject(%level.player))
		return;

	%world = PlanetXWindow.getWorldPoint($PlanetX::aimWindow);

	if (isObject(%level.crosshair))
		%level.crosshair.setPosition(%world);

	%level.player.setAim(mAtan(Vector2Sub(%world, %level.player.getPosition())));
}

/// Player 2 has no mouse: aim it at the nearest alien in range each tick, so
/// holding its fire key shoots whatever is closest (firing rides on aimAngle).
function PlanetXInput::updateAutoAim(%this)
{
	%level = PlanetXGame.level;
	if (!isObject(%level) || !isObject(%level.player2) || %level.player2.downed)
		return;

	%enemy = %level.nearestEnemy(%level.player2.getPosition(), $PlanetX::AutoAimRange);
	if (isObject(%enemy))
		%level.player2.setAim(mAtan(Vector2Sub(%enemy.getPosition(), %level.player2.getPosition())));
}

function PlanetXInput::aimTick(%this)
{
	if ($PlanetX::state !$= "playing")
		return;

	%this.updateAim();

	if ($PlanetX::twoPlayer)
		%this.updateAutoAim();

	%this.aimEvent = %this.schedule($PlanetX::AimTickMs, "aimTick");
}
