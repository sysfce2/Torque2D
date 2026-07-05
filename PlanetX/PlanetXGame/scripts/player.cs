//-----------------------------------------------------------------------------
// The spaceman: a CompositeSprite of two batch sprites - the side-view "body"
// (which flips left/right and plays the walk cycle) and the "gun" (which
// rotates to the true 360-degree aim angle). One SceneObject means the gun
// tracks the body with zero lag, and one body/Y-sort key.
//
// Movement comes from held-key flags (see input.cs); aim from the mouse
// (see input.cs). Angle convention: 0 degrees = +X, counter-clockwise
// positive - all directional art is drawn facing +X.
//-----------------------------------------------------------------------------

$PlanetX::PlayerSpeed = 15;
$PlanetX::PlayerMaxHealth = 100;
$PlanetX::GunMuzzleLength = 0.9;

function PlanetXGame::spawnPlayer(%this, %position)
{
	%player = new CompositeSprite()
	{
		class = "PlanetXPlayer";
		Position = %position;
		SceneLayer = $PlanetX::EntityLayer;
		SceneGroup = $PlanetX::PlayerGroup;
	};

	// "off" layout: addSprite's args are the sprite's local position.
	%player.setBatchLayout("off");
	%player.setBatchSortMode("Z");
	%player.setDefaultSpriteSize(2, 2);

	// Body: drawn behind the gun (higher depth renders first).
	%player.addSprite("0 0");
	%player.setSpriteName("body");
	%player.setSpriteImage("PlanetXGame:spacemanIdle");
	%player.setSpriteDepth(1);

	// Gun: pivots at the sprite center, held slightly above body center.
	%player.addSprite("0 -0.15");
	%player.setSpriteName("gun");
	%player.setSpriteImage("PlanetXGame:gun");
	%player.setSpriteSize(1.5, 0.75);
	%player.setSpriteDepth(0);

	// Feet-centric collision, feet-centric Y-sort key.
	%player.createCircleCollisionShape(0.6, 0, -0.4);
	%player.setCollisionGroups($PlanetX::AlienGroup SPC $PlanetX::WallGroup SPC $PlanetX::PickupGroup);
	%player.setSortPoint(0, -0.9);

	// The body never rotates - the gun sprite carries the aim.
	%player.setFixedAngle(true);

	%player.health = $PlanetX::PlayerMaxHealth;
	%player.moving = false;
	%player.facingLeft = false;
	%player.aimAngle = 0;

	PlanetXScene.add(%player);
	return %player;
}

//-----------------------------------------------------------------------------
// Aiming: flip the body toward the cursor, rotate the gun to the exact angle.
//-----------------------------------------------------------------------------

function PlanetXPlayer::setAim(%this, %angle)
{
	%this.aimAngle = %angle;

	// Dead zone so straight-up/down aim doesn't jitter the facing on
	// floating-point noise.
	%x = getWord(Vector2Direction(%angle, 1), 0);
	if (mAbs(%x) > 0.05)
	{
		%left = %x < 0;
		if (%left != %this.facingLeft)
		{
			%this.facingLeft = %left;
			%this.selectSpriteName("body");
			%this.setSpriteFlipX(%left);
		}
	}

	%this.selectSpriteName("gun");
	%this.setSpriteAngle(%angle);

	// Keep the gun right-side up when aiming left.
	%this.setSpriteFlipY(%this.facingLeft);
}

/// Where bullets leave the barrel, in world coordinates.
function PlanetXPlayer::getMuzzlePosition(%this)
{
	%grip = Vector2Add(%this.getPosition(), "0 -0.15");
	return Vector2Add(%grip, Vector2Direction(%this.aimAngle, $PlanetX::GunMuzzleLength));
}

//-----------------------------------------------------------------------------
// Movement.
//-----------------------------------------------------------------------------

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
			%this.selectSpriteName("body");
			%this.setSpriteImage("PlanetXGame:spacemanIdle");
			%this.moving = false;
		}
		return;
	}

	%length = mSqrt(%x * %x + %y * %y);
	%this.setLinearVelocity(%x / %length * $PlanetX::PlayerSpeed,
	                        %y / %length * $PlanetX::PlayerSpeed);

	if (!%this.moving)
	{
		%this.selectSpriteName("body");
		%this.setSpriteAnimation("PlanetXGame:spacemanWalkAnim");
		%this.moving = true;
	}
}

//-----------------------------------------------------------------------------
// Damage.
//-----------------------------------------------------------------------------

function PlanetXPlayer::takeDamage(%this, %amount)
{
	if ($PlanetX::state !$= "playing")
		return;

	%this.health -= %amount;
	PlanetXGame.updateHealthBar(%this.health);

	// Hit feedback: coral flash and a camera jolt. Object-level blend color
	// does not tint batch sprites, so flash each sprite individually.
	%this.flash("1 0.45 0.45 1");
	%this.schedule(120, "flash", "1 1 1 1");
	PlanetXWindow.startCameraShake(4, 0.3);

	if (%this.health <= 0)
		PlanetXGame.onPlayerDeath();
}

function PlanetXPlayer::flash(%this, %color)
{
	%this.selectSpriteName("body");
	%this.setSpriteBlendColor(%color);
	%this.selectSpriteName("gun");
	%this.setSpriteBlendColor(%color);
}
