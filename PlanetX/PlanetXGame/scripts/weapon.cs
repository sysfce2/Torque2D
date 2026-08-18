//-----------------------------------------------------------------------------
// PlanetXWeapon: the base class for the player's weapon. It owns everything to
// do with shooting - a pooled set of bullets, the overheat steam vent, the fire
// cadence, and the gun-heat cooldown - so a different weapon is a drop-in swap
// (see PlanetXBlaster). The player calls startFiring/stopFiring and never needs
// to know which concrete weapon it holds.
//
// Because only the most-derived ::onAdd fires, shared setup lives in init(): a
// concrete subclass's onAdd calls %this.init() first, then overrides stats.
// See TORQUE_SCRIPT.md.
//-----------------------------------------------------------------------------

/// Shared setup: base stats, the steam vent, and the heat loop. A subclass
/// overrides the stats in its onAdd (after init); playthrough upgrades then adjust
/// them once more (PlanetXUpgrades::applyToPlayer). The BULLET POOL is built last -
/// by applyToPlayer - because its size depends on the final fire rate and bolt
/// count, which upgrades are still free to change.
function PlanetXWeapon::init(%this)
{
	// Base stats. A subclass overrides these in its onAdd, after init().
	%this.fireCooldown = 250;
	%this.bulletSpeed = 40;
	%this.bulletLife = 1200;
	%this.heatPerShot = 0.13;
	%this.heatDecayPerSecond = 0.32;
	%this.heatTickMs = 100;
	%this.heatResumeThreshold = 0.35;

	// Upgrade-driven stats (see PlanetXUpgrades). These defaults are the un-upgraded
	// blaster: one bolt per shot, one damage, a 10-degree fan once split, and a heat
	// ceiling of 1. Upgrades add to bulletDamage/bulletCount and maxHeat, and shave
	// fireCooldown/heatPerShot/spreadAngle down toward their floors.
	%this.bulletDamage = 1;
	%this.bulletCount = 1;
	%this.spreadAngle = 10;
	%this.maxHeat = 1.0;

	%this.buildSteamVent();

	%this.lastFireTime = 0;
	%this.firing = false;
	%this.resetHeat();
	%this.heatTick();
}

function PlanetXWeapon::onAdd(%this)
{
	%this.init();
}

/// The weapon owns the fire/heat SCHEDULES, so it cancels them. The bullets and
/// steam vent are SceneObjects: adding them to the scene handed their lifetime
/// to the scene, which safeDeletes them when it tears down (this weapon is freed
/// as part of that same teardown). Deleting them here would double-free them.
function PlanetXWeapon::onRemove(%this)
{
	%this.stopFiring();
	if (isEventPending(%this.heatEvent))
		cancel(%this.heatEvent);
}

//-----------------------------------------------------------------------------
// Pools. Bullets and the steam vent are pre-allocated so firing never allocates
// mid-play (same pattern as TruckToy's projectile pool).
//-----------------------------------------------------------------------------

/// Pre-allocate the bullet pool, sized to the weapon's FINAL stats so a fast,
/// many-bolt gun never runs its pool dry mid-play. The most bolts that can be alive
/// at once is one volley (bulletCount) for each fire that is still within bulletLife
/// of now (bulletLife / fireCooldown of them), plus a little slack. Called once, by
/// applyToPlayer, after upgrades have settled the fire rate and bolt count.
function PlanetXWeapon::buildBulletPool(%this)
{
	%volleys = mFloor(%this.bulletLife / %this.fireCooldown) + 1;
	%this.bulletPoolSize = %volleys * %this.bulletCount + 4;

	for (%i = 0; %i < %this.bulletPoolSize; %i++)
	{
		%bullet = new Sprite() { class = "PlanetXBullet"; };
		PlanetXScene.add(%bullet);
		%bullet.park();
		%this.bullet[%i] = %bullet;
	}
	%this.nextBullet = 0;
}

function PlanetXWeapon::buildSteamVent(%this)
{
	%steam = new ParticlePlayer()
	{
		Particle = "PlanetXGame:steam";
		SceneLayer = $PlanetX::EffectLayer;
		ParticleInterpolation = true;
		SizeScale = 1.3;
	};
	%steam.setBodyType("static");
	%steam.setCollisionSuppress(true);
	PlanetXScene.add(%steam);
	%steam.stop();

	%this.steam = %steam;
}

//-----------------------------------------------------------------------------
// Firing. The owner's angle is already aimed at the cursor (input.cs).
//-----------------------------------------------------------------------------

function PlanetXWeapon::startFiring(%this)
{
	%this.firing = true;
	%this.fireTick();
}

function PlanetXWeapon::stopFiring(%this)
{
	%this.firing = false;
	if (isEventPending(%this.fireEvent))
		cancel(%this.fireEvent);
}

/// Autofire loop while the trigger is held.
function PlanetXWeapon::fireTick(%this)
{
	if (!%this.firing || $PlanetX::state !$= "playing")
		return;

	%this.fire();
	%this.fireEvent = %this.schedule(%this.fireCooldown, "fireTick");
}

function PlanetXWeapon::fire(%this)
{
	if ($PlanetX::state !$= "playing" || !isObject(%this.owner))
		return;

	// An overheated gun stays locked until it cools (see heatTick).
	if (%this.overheated)
		return;

	%now = getSimTime();
	if (%now - %this.lastFireTime < %this.fireCooldown)
		return;
	%this.lastFireTime = %now;

	// One volley leaves the barrel together: bulletCount bolts in a fan centered on
	// the aim, spreadAngle degrees apart. The split-shot upgrade adds bolts; the
	// focusing upgrade tightens the fan. A single-bolt gun is just the count == 1 case.
	%muzzle = %this.owner.getMuzzlePosition();
	%aim = %this.owner.aimAngle;
	%count = %this.bulletCount;

	for (%i = 0; %i < %count; %i++)
	{
		%angle = %aim + (%i - (%count - 1) / 2) * %this.spreadAngle;
		%this.launchBolt(%muzzle, %angle);
	}

	Audio.PlaySound("PlanetXGame:laser");

	// Heat is charged once per shot, not per bolt, so the extra split bolts are free
	// (by design - see PlanetXUpgrades).
	%this.addHeat(%this.heatPerShot);
}

/// Fire one pooled bolt from %muzzle along %angle, stamped with the weapon's current
/// per-shot damage. Pulled out of fire() so a volley is just a loop of these.
function PlanetXWeapon::launchBolt(%this, %muzzle, %angle)
{
	%bullet = %this.bullet[%this.nextBullet];
	%this.nextBullet = (%this.nextBullet + 1) % %this.bulletPoolSize;

	if (isEventPending(%bullet.recycleEvent))
		cancel(%bullet.recycleEvent);

	%bullet.damage = %this.bulletDamage;
	%bullet.setPosition(%muzzle);
	%bullet.setAngle(%angle);
	%bullet.setActive(true);
	%bullet.setVisible(true);
	%bullet.setAwake(true);
	%bullet.setLinearVelocityPolar(%angle, %this.bulletSpeed);
	%bullet.recycleEvent = %bullet.schedule(%this.bulletLife, "recycle");
}

//-----------------------------------------------------------------------------
// Gun heat: each shot adds heat, which bleeds off over time. Hitting full heat
// locks the trigger until the gun cools below the resume threshold.
//-----------------------------------------------------------------------------

function PlanetXWeapon::resetHeat(%this)
{
	%this.gunHeat = 0;
	%this.overheated = false;
	%this.updateHeatBar();
}

function PlanetXWeapon::addHeat(%this, %amount)
{
	%this.gunHeat += %amount;

	// maxHeat is the venting ceiling (1 by default; the heat-sink upgrade raises it).
	if (%this.gunHeat >= %this.maxHeat)
	{
		%this.gunHeat = %this.maxHeat;
		%this.overheated = true;

		// Vent: a steam plume off the gun and a hiss.
		if (isObject(%this.steam) && isObject(%this.owner))
		{
			%this.steam.setPosition(%this.owner.getMuzzlePosition());
			%this.steam.play(true);
		}
		Audio.PlaySound("PlanetXGame:steamHiss");
	}

	%this.updateHeatBar();
}

/// Bleed heat off over time; an overheated gun unlocks once it has cooled below
/// the resume threshold.
function PlanetXWeapon::heatTick(%this)
{
	if ($PlanetX::state !$= "playing")
		return;

	%this.heatEvent = %this.schedule(%this.heatTickMs, "heatTick");

	if (%this.gunHeat <= 0)
		return;

	%this.gunHeat -= %this.heatDecayPerSecond * %this.heatTickMs / 1000;
	if (%this.gunHeat < 0)
		%this.gunHeat = 0;

	if (%this.overheated)
	{
		// The vent plume follows the gun while it cools.
		if (isObject(%this.steam) && isObject(%this.owner))
			%this.steam.setPosition(%this.owner.getMuzzlePosition());

		// Resume is proportional to capacity, so a bigger heat sink still vents the
		// same FRACTION of its heat before the trigger frees up.
		if (%this.gunHeat <= %this.heatResumeThreshold * %this.maxHeat)
		{
			%this.overheated = false;

			// Let the last puffs finish rather than vanishing.
			if (isObject(%this.steam))
				%this.steam.stop(true, false);
		}
	}

	%this.updateHeatBar();
}

/// Push the current heat to the HUD, if there is one yet. (During construction
/// the level's HUD does not exist; the HUD starts at zero on its own.) The bar is
/// 0..1, so heat is normalized against maxHeat - a raised heat sink still reads as a
/// full bar at the vent point.
function PlanetXWeapon::updateHeatBar(%this)
{
	%level = PlanetXGame.level;
	if (isObject(%level) && isObject(%level.hud) && isObject(%this.owner))
		%level.hud.setHeat(%this.owner.playerIndex, %this.gunHeat / %this.maxHeat, %this.overheated, %this.heatTickMs);
}
