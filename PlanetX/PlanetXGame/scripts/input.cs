//-----------------------------------------------------------------------------
// PlanetX input.
//
// Keyboard: a WASD/arrow-key ActionMap for movement, pushed while playing and
// popped on every exit path.
//
// Mouse: PlanetXInput listens to the scene window's touch events. The last
// cursor position is kept as a *window* point and re-projected into the world
// every aim tick, so aim and crosshair stay correct while the camera moves
// even when the mouse doesn't.
//-----------------------------------------------------------------------------

$PlanetX::AimTickMs = 32;

function PlanetXGame::pushControls(%this)
{
	if (isObject(%this.moveMap))
		return;

	$PlanetX::keyUp = 0;
	$PlanetX::keyDown = 0;
	$PlanetX::keyLeft = 0;
	$PlanetX::keyRight = 0;
	$PlanetX::firing = false;

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

	if (!isObject(PlanetXInput))
		new ScriptObject(PlanetXInput);
	PlanetXWindow.addInputListener(PlanetXInput);

	// Start with the aim slightly right of the window center (the camera is
	// centered on the player, so this reads as "straight ahead"). Window
	// coordinates are used because they stay valid while the camera moves.
	$PlanetX::aimWindow = "612 384";

	// Hide the OS cursor (the crosshair sprite replaces it). Note: this must
	// be hideCursor, NOT cursorOff - cursorOff stops the canvas from
	// processing mouse events entirely (guiCanvas.cc gates mouse handling on
	// the cursor being on).
	Canvas.hideCursor();
	%this.aimTick();

	%this.resetGunHeat();
	%this.heatTick();
}

function PlanetXGame::popControls(%this)
{
	%this.stopFiring();

	if (isEventPending(%this.aimEvent))
		cancel(%this.aimEvent);
	if (isEventPending(%this.heatEvent))
		cancel(%this.heatEvent);

	if (isObject(PlanetXWindow) && isObject(PlanetXInput))
		PlanetXWindow.removeInputListener(PlanetXInput);

	Canvas.showCursor();

	if (!isObject(%this.moveMap))
		return;

	%this.moveMap.pop();
	%this.moveMap.delete();
}

//-----------------------------------------------------------------------------
// Key handlers. %val is 1 on press, 0 on release.
//-----------------------------------------------------------------------------

function planetXMoveUp(%val)
{
	$PlanetX::keyUp = %val;
	PlanetXGame.updatePlayerVelocity();
}

function planetXMoveDown(%val)
{
	$PlanetX::keyDown = %val;
	PlanetXGame.updatePlayerVelocity();
}

function planetXMoveLeft(%val)
{
	$PlanetX::keyLeft = %val;
	PlanetXGame.updatePlayerVelocity();
}

function planetXMoveRight(%val)
{
	$PlanetX::keyRight = %val;
	PlanetXGame.updatePlayerVelocity();
}

function planetXEscape(%val)
{
	// Deferred one tick: returnToTitle pops and deletes the ActionMap, and
	// deleting the map from inside one of its own bound handlers would be a
	// use-after-free when the key event finishes dispatching.
	if (%val)
		PlanetXGame.schedule(1, "returnToTitle");
}

function PlanetXGame::updatePlayerVelocity(%this)
{
	if (isObject(%this.player))
		%this.player.updateVelocity();
}

//-----------------------------------------------------------------------------
// Mouse handlers. %worldPosition arrives already converted to world space.
//-----------------------------------------------------------------------------

function PlanetXInput::onTouchDown(%this, %touchID, %worldPosition)
{
	PlanetXGame.setAimFromWorld(%worldPosition);

	$PlanetX::firing = true;
	PlanetXGame.fireTick();
}

function PlanetXInput::onTouchUp(%this, %touchID, %worldPosition)
{
	PlanetXGame.stopFiring();
}

function PlanetXInput::onTouchMoved(%this, %touchID, %worldPosition)
{
	PlanetXGame.setAimFromWorld(%worldPosition);
}

function PlanetXInput::onTouchDragged(%this, %touchID, %worldPosition)
{
	PlanetXGame.setAimFromWorld(%worldPosition);
}

//-----------------------------------------------------------------------------
// Aiming.
//-----------------------------------------------------------------------------

function PlanetXGame::setAimFromWorld(%this, %worldPosition)
{
	if (!isObject(PlanetXWindow))
		return;

	$PlanetX::aimWindow = PlanetXWindow.getWindowPoint(%worldPosition);
	%this.updateAim();
}

/// Re-project the stored window point into the world, then point the player
/// (and the crosshair) at it.
function PlanetXGame::updateAim(%this)
{
	if ($PlanetX::state !$= "playing" || !isObject(%this.player))
		return;

	%world = PlanetXWindow.getWorldPoint($PlanetX::aimWindow);

	if (isObject(%this.crosshair))
		%this.crosshair.setPosition(%world);

	%this.player.setAim(mAtan(Vector2Sub(%world, %this.player.getPosition())));
}

function PlanetXGame::aimTick(%this)
{
	if ($PlanetX::state !$= "playing")
		return;

	%this.updateAim();
	%this.aimEvent = %this.schedule($PlanetX::AimTickMs, "aimTick");
}
