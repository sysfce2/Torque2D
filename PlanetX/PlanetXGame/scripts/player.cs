//-----------------------------------------------------------------------------
// PlanetXPlayer: the spaceman. A CompositeSprite of two batch sprites - the
// side-view "body" (which flips left/right and plays the walk cycle) and the
// "gun" (which rotates to the true 360-degree aim angle). One SceneObject means
// the gun tracks the body with zero lag, and one body/Y-sort key.
//
// The player HAS-A weapon (%this.weapon): a swappable PlanetXWeapon subclass
// that owns all firing behavior. The player builds both its sprites and its
// weapon in onAdd, and deletes the weapon in onRemove.
//
// The level sets only the spawn position. Movement comes from held-key flags
// (input.cs); aim from the mouse (input.cs). Angle convention: 0 degrees = +X,
// counter-clockwise positive - all directional art is drawn facing +X.
//-----------------------------------------------------------------------------

$PlanetX::PlayerSpeed = 15;
$PlanetX::PlayerMaxHealth = 100;
$PlanetX::GunMuzzleLength = 0.9;

function PlanetXPlayer::onAdd(%this)
{
	%this.setSceneLayer($PlanetX::EntityLayer);
	%this.setSceneGroup($PlanetX::PlayerGroup);

	// "off" layout: addSprite's args are the sprite's local position.
	%this.setBatchLayout("off");
	%this.setBatchSortMode("Z");
	%this.setDefaultSpriteSize(2, 2);

	// Body: drawn behind the gun (higher depth renders first).
	%this.addSprite("0 0");
	%this.setSpriteName("body");
	%this.setSpriteImage("PlanetXGame:spacemanIdle");
	%this.setSpriteDepth(1);

	// Gun: pivots at the sprite center, held slightly above body center.
	%this.addSprite("0 -0.15");
	%this.setSpriteName("gun");
	%this.setSpriteImage("PlanetXGame:gun");
	%this.setSpriteSize(1.5, 0.75);
	%this.setSpriteDepth(0);

	// Feet-centric collision, feet-centric Y-sort key.
	%this.createCircleCollisionShape(0.6, 0, -0.4);
	%this.setCollisionGroups($PlanetX::AlienGroup SPC $PlanetX::WallGroup SPC $PlanetX::PickupGroup);
	%this.setSortPoint(0, -0.9);

	// The body never rotates - the gun sprite carries the aim.
	%this.setFixedAngle(true);

	%this.health = $PlanetX::PlayerMaxHealth;
	%this.moving = false;
	%this.facingLeft = false;
	%this.aimAngle = 0;

	// The spaceman arrives armed. Swap this class to change the weapon; nothing
	// else in the player needs to know which weapon it is holding.
	%this.weapon = new ScriptObject()
	{
		class = "PlanetXBlaster";
		superclass = "PlanetXWeapon";
		owner = %this;
	};
}

/// The player owns its weapon, so it deletes it. (The player's own sprites go
/// when the scene tears down; the weapon is a ScriptObject and must be freed.)
function PlanetXPlayer::onRemove(%this)
{
	if (isObject(%this.weapon))
		%this.weapon.delete();
}

//-----------------------------------------------------------------------------
// Firing is delegated to the weapon.
//-----------------------------------------------------------------------------

function PlanetXPlayer::startFiring(%this)
{
	if (isObject(%this.weapon))
		%this.weapon.startFiring();
}

function PlanetXPlayer::stopFiring(%this)
{
	if (isObject(%this.weapon))
		%this.weapon.stopFiring();
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

	// A stationary CompositeSprite does not re-render a sub-sprite's transform
	// change until the composite itself moves (it only rebuilds its batch when
	// spatially dirty), so the gun would freeze while the spaceman stands still.
	// While idle, nudge the body's transform to itself to mark it dirty and
	// refresh the batch. When walking, the movement already refreshes it.
	if (!%this.moving)
		%this.setPosition(%this.getPosition());
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

	%level = PlanetXGame.level;
	if (isObject(%level) && isObject(%level.hud))
		%level.hud.setHealth(%this.health);

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
