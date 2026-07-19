//-----------------------------------------------------------------------------
// PlanetXCrystal: the crystal the spaceman is here for. A static sensor dropped
// at a random spot each level (the level pushes it away from the rocket);
// touching it clears the level. The level sets only its position; the crystal
// configures its own look, collision, and glow pulse, and cancels the pulse
// when it is deleted.
//-----------------------------------------------------------------------------

function PlanetXCrystal::onAdd(%this)
{
	%this.setSize("2.5 2.5");
	%this.setSceneLayer($PlanetX::EntityLayer);
	%this.setSceneGroup($PlanetX::PickupGroup);
	%this.setImage("PlanetXGame:crystal");

	%this.setBodyType("static");
	%this.createCircleCollisionShape(1.2);
	%this.setSortPoint(0, -1);

	// A sensor detects contacts without physically blocking them.
	%this.setCollisionShapeIsSensor(0, true);
	%this.setCollisionCallback(true);

	%this.pulse();
}

/// Cancel the self-rescheduling pulse so no orphaned event survives the crystal.
function PlanetXCrystal::onRemove(%this)
{
	if (isEventPending(%this.pulseEvent))
		cancel(%this.pulseEvent);
}

/// A slow glow pulse so the crystal reads as the goal from across the map.
function PlanetXCrystal::pulse(%this)
{
	%this.bright = !%this.bright;

	if (%this.bright)
		%this.setBlendColor(1, 1, 1);
	else
		%this.setBlendColor(0.7, 0.85, 0.78);

	%this.pulseEvent = %this.schedule(600, "pulse");
}

function PlanetXCrystal::onCollision(%this, %object, %collisionDetails)
{
	if (%object.class $= "PlanetXPlayer")
		PlanetXGame.onWin();
}
