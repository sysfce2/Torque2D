//-----------------------------------------------------------------------------
// The spaceman. Movement comes from held-key flags (see input.cs); facing
// comes from the mouse (see input.cs). Angle convention: 0 degrees = +X,
// counter-clockwise positive - the sprite art is drawn facing +X, so angles
// from mAtan() apply with no offset.
//-----------------------------------------------------------------------------

$PlanetX::PlayerSpeed = 15;
$PlanetX::PlayerMaxHealth = 100;

function PlanetXGame::spawnPlayer(%this, %position)
{
	%player = new Sprite()
	{
		class = "PlanetXPlayer";
		Position = %position;
		Size = "3 3";
		SceneLayer = $PlanetX::PlayerLayer;
		SceneGroup = $PlanetX::PlayerGroup;
		Image = "PlanetXGame:spacemanIdle";
	};

	%player.createCircleCollisionShape(1.2);
	%player.setCollisionGroups($PlanetX::AlienGroup SPC $PlanetX::WallGroup SPC $PlanetX::PickupGroup);

	// The mouse controls facing; physics contacts must not spin the body.
	%player.setFixedAngle(true);

	%player.health = $PlanetX::PlayerMaxHealth;
	%player.moving = false;

	PlanetXScene.add(%player);
	return %player;
}

/// Re-derive velocity from the held-key flags. Called on every key make/break.
function PlanetXPlayer::updateVelocity(%this)
{
	%x = $PlanetX::keyRight - $PlanetX::keyLeft;
	%y = $PlanetX::keyUp - $PlanetX::keyDown;

	if (%x == 0 && %y == 0)
	{
		%this.setLinearVelocity(0, 0);

		if (%this.moving)
		{
			%this.setImage("PlanetXGame:spacemanIdle");
			%this.moving = false;
		}
		return;
	}

	%length = mSqrt(%x * %x + %y * %y);
	%this.setLinearVelocity(%x / %length * $PlanetX::PlayerSpeed,
	                        %y / %length * $PlanetX::PlayerSpeed);

	if (!%this.moving)
	{
		%this.playAnimation("PlanetXGame:spacemanWalkAnim");
		%this.moving = true;
	}
}

function PlanetXPlayer::takeDamage(%this, %amount)
{
	if ($PlanetX::state !$= "playing")
		return;

	%this.health -= %amount;
	PlanetXGame.updateHealthBar(%this.health);

	// Hit feedback: coral flash and a camera jolt.
	%this.setBlendColor(1, 0.45, 0.45);
	%this.schedule(120, "setBlendColor", 1, 1, 1);
	PlanetXWindow.startCameraShake(4, 0.3);

	if (%this.health <= 0)
		PlanetXGame.onPlayerDeath();
}
