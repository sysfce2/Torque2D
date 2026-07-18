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

// One footstep per foot-plant: half of the 400ms walk cycle (spacemanWalkAnim).
$PlanetX::FootstepIntervalMs = 200;

// How often the Nanite Weave auto-repair upgrade mends the suit back up.
$PlanetX::RepairTickMs = 250;

// Collision knockback. The player is driven at a fixed key-velocity, so whatever
// velocity a physics contact adds sticks until the next key event - a hit or a
// rock used to leave them drifting. On a contact we shove them off, then re-read
// the held keys a moment later to snap velocity back to intent. Enemies shove hard
// and recover slowly; solid props (rocks, walls) barely nudge and recover fast.
$PlanetX::EnemyKnockbackSpeed = 28;
$PlanetX::EnemyKnockbackMs    = 220;
$PlanetX::PropBounceSpeed     = 6;
$PlanetX::PropBounceMs        = 70;
$PlanetX::KnockbackCooldownMs = 260;

function PlanetXPlayer::onAdd(%this)
{
	%this.setSceneLayer($PlanetX::EntityLayer);
	%this.setSceneGroup($PlanetX::PlayerGroup);

	// Identity: the level sets these per player (index, sprites, aim mode) in
	// spawnPlayer. Default them so a bare PlanetXPlayer is still player 1 - the
	// single-player path behaves exactly as before.
	if (%this.playerIndex $= "")
		%this.playerIndex = 1;
	if (%this.idleImage $= "")
		%this.idleImage = "PlanetXGame:spacemanIdle";
	if (%this.walkAnim $= "")
		%this.walkAnim = "PlanetXGame:spacemanWalkAnim";
	if (%this.aimMode $= "")
		%this.aimMode = "mouse";

	// "off" layout: addSprite's args are the sprite's local position.
	%this.setBatchLayout("off");
	%this.setBatchSortMode("Z");
	%this.setDefaultSpriteSize(2, 2);

	// Body: drawn behind the gun (higher depth renders first).
	%this.addSprite("0 0");
	%this.setSpriteName("body");
	%this.setSpriteImage(%this.idleImage);
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
	%this.setCollisionCallback(true);
	%this.setSortPoint(0, -0.9);

	// The body never rotates - the gun sprite carries the aim.
	%this.setFixedAngle(true);

	%this.health = $PlanetX::PlayerMaxHealth;
	%this.moving = false;
	%this.facingLeft = false;
	%this.aimAngle = 0;
	%this.downed = false;
	%this.lastKnockback = 0;

	// Upgrade-driven suit stats; PlanetXUpgrades::applyToPlayer sets the real values
	// from this player's banked choices just below (0 = the upgrade isn't owned).
	%this.selfDestructRadius = 0;
	%this.autoRepairRate = 0;

	// Per-player held-move flags (input.cs sets these; updateVelocity reads them).
	%this.inUp = 0;
	%this.inDown = 0;
	%this.inLeft = 0;
	%this.inRight = 0;

	// The spaceman arrives armed. Swap this class to change the weapon; nothing
	// else in the player needs to know which weapon it is holding.
	%this.weapon = new ScriptObject()
	{
		class = "PlanetXBlaster";
		superclass = "PlanetXWeapon";
		owner = %this;
	};

	// Stamp this player's playthrough upgrades onto the fresh weapon and suit (the
	// player is rebuilt every level, so upgrades are re-applied here each time), then
	// start the auto-repair loop if that upgrade is owned.
	PlanetXUpgrades.applyToPlayer(%this);
	if (%this.autoRepairRate > 0)
		%this.repairTick();
}

/// The player owns its weapon, so it deletes it. (The player's own sprites go
/// when the scene tears down; the weapon is a ScriptObject and must be freed.)
function PlanetXPlayer::onRemove(%this)
{
	if (isEventPending(%this.footstepEvent))
		cancel(%this.footstepEvent);
	if (isEventPending(%this.velocityResetEvent))
		cancel(%this.velocityResetEvent);
	if (isEventPending(%this.repairEvent))
		cancel(%this.repairEvent);

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
	// A downed player is out of play until revived - ignore any held keys.
	if (%this.downed)
	{
		%this.setLinearVelocity(0, 0);
		return;
	}

	%x = %this.inRight - %this.inLeft;
	%y = %this.inUp - %this.inDown;

	if (%x == 0 && %y == 0)
	{
		%this.setLinearVelocity(0, 0);

		if (%this.moving)
		{
			%this.selectSpriteName("body");
			%this.setSpriteImage(%this.idleImage);
			%this.moving = false;

			// Halt the footstep loop the instant he stops.
			if (isEventPending(%this.footstepEvent))
				cancel(%this.footstepEvent);
		}
		return;
	}

	%length = mSqrt(%x * %x + %y * %y);
	%this.setLinearVelocity(%x / %length * $PlanetX::PlayerSpeed,
	                        %y / %length * $PlanetX::PlayerSpeed);

	if (!%this.moving)
	{
		%this.selectSpriteName("body");
		%this.setSpriteAnimation(%this.walkAnim);
		%this.moving = true;

		// Kick off the footstep loop (plays now, then reschedules while walking).
		%this.footstep();
	}
}

/// Repeating footstep while the spaceman walks. Self-cancels when he stops moving
/// or the game leaves play; the pending event is also cancelled in onRemove and
/// when movement stops.
function PlanetXPlayer::footstep(%this)
{
	if ($PlanetX::state !$= "playing" || !%this.moving)
		return;

	Audio.PlaySound("PlanetXGame:footstep");
	%this.footstepEvent = %this.schedule($PlanetX::FootstepIntervalMs, "footstep");
}

//-----------------------------------------------------------------------------
// Collision knockback: shove off whatever we hit, then re-read the held keys a
// moment later so a contact never leaves the player drifting. A per-player
// cooldown keeps a lingering contact from re-shoving every physics step (the same
// reason enemy contact damage has one).
//-----------------------------------------------------------------------------

function PlanetXPlayer::onCollision(%this, %object, %collisionDetails)
{
	if (%this.downed)
		return;

	%now = getSimTime();
	if (%now - %this.lastKnockback < $PlanetX::KnockbackCooldownMs)
		return;

	if (%object.isEnemy)
	{
		%this.lastKnockback = %now;
		%this.shoveFrom(%object, $PlanetX::EnemyKnockbackSpeed, $PlanetX::EnemyKnockbackMs);
	}
	else if (%object.getSceneGroup() == $PlanetX::WallGroup)
	{
		%this.lastKnockback = %now;
		%this.shoveFrom(%object, $PlanetX::PropBounceSpeed, $PlanetX::PropBounceMs);
	}
	// Sensors (the crystal) don't knock the player around.
}

/// Push the player directly away from %object at %speed, then re-derive velocity
/// from the held keys after %resetMs so control snaps back and the shove doesn't
/// linger as drift.
function PlanetXPlayer::shoveFrom(%this, %object, %speed, %resetMs)
{
	%away = mAtan(Vector2Sub(%this.getPosition(), %object.getPosition()));
	%this.setLinearVelocityPolar(%away, %speed);

	if (isEventPending(%this.velocityResetEvent))
		cancel(%this.velocityResetEvent);
	%this.velocityResetEvent = %this.schedule(%resetMs, "updateVelocity");
}

//-----------------------------------------------------------------------------
// Damage.
//-----------------------------------------------------------------------------

function PlanetXPlayer::takeDamage(%this, %amount)
{
	if ($PlanetX::state !$= "playing" || %this.downed)
		return;

	%this.health -= %amount;

	%level = PlanetXGame.level;
	if (isObject(%level) && isObject(%level.hud))
		%level.hud.setHealth(%this.playerIndex, %this.health);

	// Hit feedback: coral flash and a camera jolt. Object-level blend color
	// does not tint batch sprites, so flash each sprite individually.
	%this.flash("1 0.45 0.45 1");
	%this.schedule(120, "flash", "1 1 1 1");
	PlanetXWindow.startCameraShake(4, 0.3);

	if (%this.health <= 0)
		PlanetXGame.onPlayerDown(%this);
	else
		Audio.PlaySound("PlanetXGame:playerHurt");
}

function PlanetXPlayer::flash(%this, %color)
{
	%this.selectSpriteName("body");
	%this.setSpriteBlendColor(%color);
	%this.selectSpriteName("gun");
	%this.setSpriteBlendColor(%color);
}

/// Auto-repair upgrade (Nanite Weave): while alive and in play, mend the suit back
/// toward full a little each tick. Self-reschedules like the weapon's heat loop -
/// started in onAdd only when the rate is above zero, cancelled in onRemove, and
/// stopped whenever the game leaves play. A downed player and a full hull are no-ops
/// but keep the loop alive so it resumes on revive / after the next hit.
function PlanetXPlayer::repairTick(%this)
{
	if ($PlanetX::state !$= "playing")
		return;

	%this.repairEvent = %this.schedule($PlanetX::RepairTickMs, "repairTick");

	if (%this.downed || %this.health >= $PlanetX::PlayerMaxHealth)
		return;

	%this.health = mClamp(%this.health + %this.autoRepairRate * $PlanetX::RepairTickMs / 1000,
	                      0, $PlanetX::PlayerMaxHealth);

	%level = PlanetXGame.level;
	if (isObject(%level) && isObject(%level.hud))
		%level.hud.setHealth(%this.playerIndex, %this.health);
}

//-----------------------------------------------------------------------------
// Downed / revive (co-op). A killed player is downed - hidden and out of play -
// until a living teammate reaches the rocket and revives them (see camera.cs and
// PlanetXLevel::revivePlayer). In single-player nothing calls these.
//-----------------------------------------------------------------------------

/// Take this player out of play: stop firing/moving, hide, and drop out of
/// collisions so aliens ignore the empty body.
function PlanetXPlayer::goDown(%this)
{
	// Dead Man's Payload: detonate a clearing blast on the way down (co-op only, and
	// only if this player owns the upgrade).
	if (%this.selfDestructRadius > 0)
		%this.detonate();

	%this.downed = true;
	%this.stopFiring();
	%this.setLinearVelocity(0, 0);

	%this.inUp = 0;
	%this.inDown = 0;
	%this.inLeft = 0;
	%this.inRight = 0;

	%this.moving = false;
	if (isEventPending(%this.footstepEvent))
		cancel(%this.footstepEvent);
	if (isEventPending(%this.velocityResetEvent))
		cancel(%this.velocityResetEvent);

	%this.setVisible(false);
	%this.setCollisionSuppress(true);
}

/// Bring a downed player back at %position with full health.
function PlanetXPlayer::revive(%this, %position)
{
	%this.setPosition(%position);
	%this.setLinearVelocity(0, 0);
	%this.health = $PlanetX::PlayerMaxHealth;
	%this.downed = false;

	%this.setVisible(true);
	%this.setCollisionSuppress(false);

	// Back to the idle look; aim refreshes on the next input/aim tick.
	%this.selectSpriteName("body");
	%this.setSpriteImage(%this.idleImage);
	%this.moving = false;

	%level = PlanetXGame.level;
	if (isObject(%level) && isObject(%level.hud))
		%level.hud.setHealth(%this.playerIndex, %this.health);
}

/// Self-destruct upgrade (Dead Man's Payload): wipe out every enemy within
/// selfDestructRadius of where this player fell. Walks the level's live-enemy set
/// from the top down because a kill removes the enemy from the set; overkill damage
/// is fine (takeDamage no-ops once an enemy is dead).
function PlanetXPlayer::detonate(%this)
{
	%level = PlanetXGame.level;
	if (!isObject(%level) || !isObject(%level.enemies))
		return;

	%origin = %this.getPosition();

	for (%i = %level.enemies.getCount() - 1; %i >= 0; %i--)
	{
		%enemy = %level.enemies.getObject(%i);
		if (isObject(%enemy)
			&& Vector2Length(Vector2Sub(%enemy.getPosition(), %origin)) <= %this.selfDestructRadius)
			%enemy.takeDamage(99999);
	}

	%level.playBurst(%origin);
	PlanetXWindow.startCameraShake(6, 0.4);
}
