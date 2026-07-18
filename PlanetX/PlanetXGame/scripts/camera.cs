//-----------------------------------------------------------------------------
// PlanetXCamera: the shared co-op camera, a manager ScriptObject created by the
// level only in two-player mode (single-player keeps the rigid mount). Each tick
// it centers the window between the two players and zooms out just enough to keep
// both framed, then eases back in as they regroup. The same tick watches for the
// living player reaching the rocket to revive a downed teammate.
//
// It drives the window directly (setCameraPosition/setCameraSize), so the window
// must NOT be mounted while this runs. It owns its follow schedule and cancels it
// in onRemove; the level deletes this before it deletes the window.
//-----------------------------------------------------------------------------

// Camera framing. Height is the vertical world span shown; width follows the
// window aspect (as in sceneWindow.cs). Base matches the single-player camera.
$PlanetX::CameraBaseHeight = 45;
$PlanetX::CameraMaxHeight  = 90;   // never zoom past this - keeps the view in-world
$PlanetX::CameraZoomMargin = 18;   // world padding around the two players
$PlanetX::CameraZoomLerp   = 0.12; // per-tick easing of the zoom toward its target
$PlanetX::CameraTickMs     = 16;   // ~60 Hz follow

// How close the living player must get to the rocket to revive a downed teammate.
$PlanetX::ReviveRadius = 4;

function PlanetXCamera::onAdd(%this)
{
	%this.height = $PlanetX::CameraBaseHeight;
	%this.followTick();
}

function PlanetXCamera::onRemove(%this)
{
	if (isEventPending(%this.followEvent))
		cancel(%this.followEvent);
}

/// Follow + revive loop. Stops rescheduling once the game leaves play (win, loss,
/// or teardown), same as the aim loop; onRemove cancels any pending tick.
function PlanetXCamera::followTick(%this)
{
	if ($PlanetX::state !$= "playing")
		return;

	// Keep the loop alive across a pause; hold the frame (and don't revive) while
	// the world is frozen.
	if (!$PlanetX::paused)
	{
		%this.updateFrame();
		%this.reviveCheck();
	}

	%this.followEvent = %this.schedule($PlanetX::CameraTickMs, "followTick");
}

/// Center on the living players' midpoint and zoom to frame both (plus a margin).
function PlanetXCamera::updateFrame(%this)
{
	// The level hands us a direct reference (PlanetXGame.level is not yet assigned
	// while the level is still building, but its players already exist).
	%level = %this.level;
	if (!isObject(%level))
		return;

	// Gather the players still in play.
	%a = "";
	%b = "";
	%p = %level.player;
	if (isObject(%p) && !%p.downed)
		%a = %p;
	%p = %level.player2;
	if (isObject(%p) && !%p.downed)
	{
		if (%a $= "")
			%a = %p;
		else
			%b = %p;
	}

	if (%a $= "")
		return;  // both down; the game is ending - hold the last frame

	%posA = %a.getPosition();

	if (%b $= "")
	{
		// One player left: follow it, no extra zoom.
		%midpoint = %posA;
		%separation = 0;
	}
	else
	{
		%posB = %b.getPosition();
		%midX = (getWord(%posA, 0) + getWord(%posB, 0)) * 0.5;
		%midY = (getWord(%posA, 1) + getWord(%posB, 1)) * 0.5;
		%midpoint = %midX SPC %midY;
		%separation = Vector2Length(Vector2Sub(%posA, %posB));
	}

	// Ease the height toward one that frames the pair; width follows the aspect.
	%target = mClamp(%separation + $PlanetX::CameraZoomMargin,
		$PlanetX::CameraBaseHeight, $PlanetX::CameraMaxHeight);
	%this.height += (%target - %this.height) * $PlanetX::CameraZoomLerp;

	%extent = Canvas.extent;
	%aspect = %extent.x / %extent.y;
	%camWidth = %this.height * %aspect;

	// setViewLimitOn (set in the level) clamps the position at the world edge.
	PlanetXWindow.setCameraSize(%camWidth SPC %this.height);
	PlanetXWindow.setCameraPosition(%midpoint);
}

/// While one player is down, revive them once the survivor reaches the rocket.
function PlanetXCamera::reviveCheck(%this)
{
	%level = %this.level;
	if (!isObject(%level) || !isObject(%level.rocket))
		return;

	// Exactly one of the two is down while the game is still playing (both down
	// ends the game), so the other is the rescuer.
	%downed = "";
	%rescuer = "";

	%p = %level.player;
	if (isObject(%p))
	{
		if (%p.downed)
			%downed = %p;
		else
			%rescuer = %p;
	}
	%p = %level.player2;
	if (isObject(%p))
	{
		if (%p.downed)
			%downed = %p;
		else
			%rescuer = %p;
	}

	if (!isObject(%downed) || !isObject(%rescuer))
		return;

	// Measured to the door (center-bottom of the rocket) - where a player walks in.
	%door = %level.rocket.getDoorPosition();
	%dist = Vector2Length(Vector2Sub(%rescuer.getPosition(), %door));
	if (%dist <= $PlanetX::ReviveRadius)
		%level.revivePlayer(%downed);
}
