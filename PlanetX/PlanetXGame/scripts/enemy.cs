//-----------------------------------------------------------------------------
// PlanetXEnemy: the base class for the planet's hostiles. It owns all the shared
// behavior - wander-then-chase AI, a health pool with a hit flash and a death
// burst, and contact damage against the player. The two concrete kinds
// (PlanetXBug, PlanetXBrute) differ only in their body (size, sprite, collision)
// and the stats the level hands them.
//
// The level passes target/health/chaseSpeed/contactDamage in as constructor
// parameters, so init() must NOT set those (it would clobber them); it sets only
// the values that are always the same. Only the most-derived ::onAdd fires, so
// each subclass calls %this.init() and then builds its body. See TORQUE_SCRIPT.md.
//-----------------------------------------------------------------------------

$PlanetX::AlienHealth = 3;
$PlanetX::AlienChaseSpeed = 7;
$PlanetX::AlienWanderSpeed = 3;
$PlanetX::AlienAggroRadius = 20;
$PlanetX::AlienContactDamage = 10;
$PlanetX::AlienDamageCooldownMs = 500;
$PlanetX::ChaseTickMs = 400;

// A bolt's hit shoves the alien back along the shot; the AI reclaims its velocity on
// the next chase tick, so it reads as a brief recoil. BulletKnockbackSpeed is the
// velocity bump a bug takes (brutes take a fraction - see knockResist). One shove per
// cooldown de-dups a forked volley, and the max-speed gate keeps a fast gun from
// piling shoves up between AI ticks into a launch.
$PlanetX::BulletKnockbackSpeed = 6;
$PlanetX::BulletKnockbackCooldownMs = 70;
$PlanetX::BulletKnockbackMaxSpeed = 14;

// The brute: a hulking dark-shelled variant.
$PlanetX::BruteSize = "3.5 2.1875";

/// Shared setup. Sets only the always-the-same values (the passed-in target and
/// stats are left untouched) and starts the AI tick.
function PlanetXEnemy::init(%this)
{
	// Marker so a bullet can tell an enemy from a wall without knowing the type.
	%this.isEnemy = true;

	%this.setSceneLayer($PlanetX::EntityLayer);
	%this.setSceneGroup($PlanetX::AlienGroup);
	%this.setCollisionCallback(true);

	// The AI flips the sprite toward its heading; contacts must not spin it.
	%this.setFixedAngle(true);

	%this.wanderSpeed = $PlanetX::AlienWanderSpeed;
	%this.aggroRadius = $PlanetX::AlienAggroRadius;
	%this.wanderTicks = 0;
	%this.lastContactDamage = 0;
	%this.lastKnockback = 0;
	%this.knockResist = 1;   // fraction of a bolt's shove this alien takes (brutes < 1)

	// Chase/wander state, so the sound events fire only on the transition.
	%this.chasing = false;

	%this.chaseEvent = %this.schedule($PlanetX::ChaseTickMs, "updateChase");
}

function PlanetXEnemy::onAdd(%this)
{
	%this.init();
}

/// Cancel the AI tick so no orphaned reschedule outlives the enemy.
function PlanetXEnemy::onRemove(%this)
{
	if (isEventPending(%this.chaseEvent))
		cancel(%this.chaseEvent);
}

//-----------------------------------------------------------------------------
// AI: wander aimlessly until the target comes within the aggro radius, then
// chase it. (Sim schedules keep firing while the scene is paused for a dialog,
// so the tick only acts while the game is actually playing.)
//-----------------------------------------------------------------------------

function PlanetXEnemy::updateChase(%this)
{
	%this.chaseEvent = %this.schedule($PlanetX::ChaseTickMs, "updateChase");

	if ($PlanetX::state !$= "playing")
		return;

	// Co-op: chase whichever living player is nearest, re-evaluated each tick so
	// a downed player is dropped and a revived one is picked back up. In single-
	// player the target stays the sole player the level handed us.
	if ($PlanetX::twoPlayer && isObject(PlanetXGame.level))
		%this.target = PlanetXGame.level.nearestLivingPlayer(%this.getPosition());

	if (!isObject(%this.target))
		return;

	%toTarget = Vector2Sub(%this.target.getPosition(), %this.getPosition());
	%distance = Vector2Length(%toTarget);

	if (%distance < %this.aggroRadius)
	{
		// Just locked on - announce it once (the level turns this into a sound).
		if (!%this.chasing)
		{
			%this.chasing = true;
			%this.postEvent("EnemyStartChase");
		}

		// Chase: head straight for the target.
		%angle = mAtan(%toTarget);
		%this.setLinearVelocityPolar(%angle, %this.chaseSpeed);
		%this.setFlipX(getWord(Vector2Direction(%angle, 1), 0) < 0);
		%this.wanderTicks = 0;
		return;
	}

	// Lost the target - announce giving up once.
	if (%this.chasing)
	{
		%this.chasing = false;
		%this.postEvent("EnemyStopChase");
	}

	// Wander: hold a random heading for a few ticks, then pick a new one.
	%this.wanderTicks--;
	if (%this.wanderTicks <= 0)
	{
		%this.wanderTicks = getRandom(4, 9);
		%angle = getRandom(0, 359);
		%this.setLinearVelocityPolar(%angle, %this.wanderSpeed);
		%this.setFlipX(getWord(Vector2Direction(%angle, 1), 0) < 0);
	}
}

//-----------------------------------------------------------------------------
// Health and damage.
//-----------------------------------------------------------------------------

function PlanetXEnemy::takeDamage(%this, %amount)
{
	if (%this.health <= 0)
		return;

	%this.health -= %amount;

	if (%this.health <= 0)
	{
		if (isObject(PlanetXGame.level))
			PlanetXGame.level.playBurst(%this.getPosition());
		%this.postEvent("EnemyDeath");
		%this.setCollisionSuppress(true);
		%this.safeDelete();
		return;
	}

	// Hit flash.
	%this.setBlendColor(1, 0.45, 0.45);
	%this.schedule(120, "setBlendColor", 1, 1, 1);
}

/// A bolt's hit shoves the alien back along the shot direction. The impulse is mass x
/// the desired velocity bump, so the recoil speed is consistent whatever the body
/// size, then scaled by knockResist (a heavy brute barely budges). The next chase tick
/// re-derives velocity, so it's a brief pop; a per-alien cooldown keeps a forked volley
/// from stacking several shoves into a launch.
function PlanetXEnemy::knockback(%this, %angle)
{
	%now = getSimTime();
	if (%now - %this.lastKnockback < $PlanetX::BulletKnockbackCooldownMs)
		return;
	%this.lastKnockback = %now;

	// Already flying from earlier shoves this AI window - don't pile on and launch it.
	if (Vector2Length(%this.getLinearVelocity()) >= $PlanetX::BulletKnockbackMaxSpeed)
		return;

	%deltaV = $PlanetX::BulletKnockbackSpeed * %this.knockResist;
	%this.applyLinearImpulse(Vector2Direction(%angle, %this.getMass() * %deltaV), %this.getPosition());
}

/// Contact damage against the player, with a per-enemy cooldown so a lingering
/// contact doesn't drain health every physics step.
function PlanetXEnemy::onCollision(%this, %object, %collisionDetails)
{
	if (%object.class !$= "PlanetXPlayer" || %object.downed)
		return;

	%now = getSimTime();
	if (%now - %this.lastContactDamage < $PlanetX::AlienDamageCooldownMs)
		return;
	%this.lastContactDamage = %now;

	%object.takeDamage(%this.contactDamage);
}
