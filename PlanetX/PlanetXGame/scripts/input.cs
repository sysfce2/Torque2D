//-----------------------------------------------------------------------------
// PlanetXInput: the level's input controller, a manager ScriptObject.
//
// Keyboard: a WASD/arrow-key ActionMap for movement, pushed in onAdd and popped
// in onRemove. The bound handlers must be global functions (the engine calls
// them by bare name), so they stay global and just forward to the player.
//
// Mouse: this object listens to the scene window's touch events. The last cursor
// position is kept as a *window* point and re-projected into the world every aim
// tick, so aim and crosshair stay correct while the camera moves even when the
// mouse doesn't.
//-----------------------------------------------------------------------------

$PlanetX::AimTickMs = 32;

function PlanetXInput::onAdd(%this)
{
	$PlanetX::keyUp = 0;
	$PlanetX::keyDown = 0;
	$PlanetX::keyLeft = 0;
	$PlanetX::keyRight = 0;

	%map = new ActionMap();
	%map.bind("keyboard", "w", "planetXMoveUp");
	%map.bind("keyboard", "up", "planetXMoveUp");
	%map.bind("keyboard", "s", "planetXMoveDown");
	%map.bind("keyboard", "down", "planetXMoveDown");
	%map.bind("keyboard", "a", "planetXMoveLeft");
	%map.bind("keyboard", "left", "planetXMoveLeft");
	%map.bind("keyboard", "d", "planetXMoveRight");
	%map.bind("keyboard", "right", "planetXMoveRight");
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

/// Convenience: the player owns the trigger; input just relays.
function PlanetXInput::stopFiring(%this)
{
	%player = PlanetXGame.level.player;
	if (isObject(%player))
		%player.stopFiring();
}

//-----------------------------------------------------------------------------
// Key handlers. These MUST be global (ActionMap bind targets). %val is 1 on
// press, 0 on release. Each just updates a flag and nudges the player.
//-----------------------------------------------------------------------------

function planetXMoveUp(%val)    { $PlanetX::keyUp = %val;    planetXUpdateMove(); }
function planetXMoveDown(%val)  { $PlanetX::keyDown = %val;  planetXUpdateMove(); }
function planetXMoveLeft(%val)  { $PlanetX::keyLeft = %val;  planetXUpdateMove(); }
function planetXMoveRight(%val) { $PlanetX::keyRight = %val; planetXUpdateMove(); }

function planetXUpdateMove()
{
	%player = PlanetXGame.level.player;
	if (isObject(%player))
		%player.updateVelocity();
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
// Mouse handlers. %worldPosition arrives already converted to world space.
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
	%this.stopFiring();
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

/// Re-project the stored window point into the world, then point the player
/// (and the crosshair) at it.
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

function PlanetXInput::aimTick(%this)
{
	if ($PlanetX::state !$= "playing")
		return;

	%this.updateAim();
	%this.aimEvent = %this.schedule($PlanetX::AimTickMs, "aimTick");
}
