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

// The brute: a hulking dark-shelled variant.
$PlanetX::BruteSize = 3.5;

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

	if ($PlanetX::state !$= "playing" || !isObject(%this.target))
		return;

	%toTarget = Vector2Sub(%this.target.getPosition(), %this.getPosition());
	%distance = Vector2Length(%toTarget);

	if (%distance < %this.aggroRadius)
	{
		// Chase: head straight for the target.
		%angle = mAtan(%toTarget);
		%this.setLinearVelocityPolar(%angle, %this.chaseSpeed);
		%this.setFlipX(getWord(Vector2Direction(%angle, 1), 0) < 0);
		%this.wanderTicks = 0;
		return;
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
		%this.setCollisionSuppress(true);
		%this.safeDelete();
		return;
	}

	// Hit flash.
	%this.setBlendColor(1, 0.45, 0.45);
	%this.schedule(120, "setBlendColor", 1, 1, 1);
}

/// Contact damage against the player, with a per-enemy cooldown so a lingering
/// contact doesn't drain health every physics step.
function PlanetXEnemy::onCollision(%this, %object, %collisionDetails)
{
	if (%object.class !$= "PlanetXPlayer")
		return;

	%now = getSimTime();
	if (%now - %this.lastContactDamage < $PlanetX::AlienDamageCooldownMs)
		return;
	%this.lastContactDamage = %now;

	%object.takeDamage(%this.contactDamage);
}
