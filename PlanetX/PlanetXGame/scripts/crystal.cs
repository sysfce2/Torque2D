//-----------------------------------------------------------------------------
// The crystal the spaceman is here for. A static sensor in the far corner;
// touching it wins the mission.
//-----------------------------------------------------------------------------

function PlanetXGame::buildCrystal(%this, %position)
{
	%crystal = new Sprite()
	{
		class = "PlanetXCrystal";
		Position = %position;
		Size = "2.5 2.5";
		SceneLayer = $PlanetX::EntityLayer;
		SceneGroup = $PlanetX::PickupGroup;
		Image = "PlanetXGame:crystal";
	};

	%crystal.setBodyType("static");
	%crystal.createCircleCollisionShape(1.2);
	%crystal.setSortPoint(0, -1);

	// A sensor detects contacts without physically blocking them.
	%crystal.setCollisionShapeIsSensor(0, true);
	%crystal.setCollisionCallback(true);

	PlanetXScene.add(%crystal);
	%crystal.pulse();
	%this.crystal = %crystal;
}

/// A slow glow pulse so the crystal reads as the goal from across the map.
function PlanetXCrystal::pulse(%this)
{
	if (!isObject(%this))
		return;

	%this.bright = !%this.bright;

	if (%this.bright)
		%this.setBlendColor(1, 1, 1);
	else
		%this.setBlendColor(0.7, 0.85, 0.78);

	%this.schedule(600, "pulse");
}

function PlanetXCrystal::onCollision(%this, %object, %collisionDetails)
{
	if (%object.class $= "PlanetXPlayer")
		PlanetXGame.onWin();
}
