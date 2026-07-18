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

/// Shared setup: base stats, the bullet pool, the steam vent, and the heat loop.
function PlanetXWeapon::init(%this)
{
	// Base stats. A subclass overrides these in its onAdd, after init().
	%this.fireCooldown = 250;
	%this.bulletSpeed = 40;
	%this.bulletLife = 1200;
	%this.bulletPoolSize = 16;
	%this.heatPerShot = 0.13;
	%this.heatDecayPerSecond = 0.32;
	%this.heatTickMs = 100;
	%this.heatResumeThreshold = 0.35;

	%this.buildBulletPool();
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

function PlanetXWeapon::buildBulletPool(%this)
{
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

	%bullet = %this.bullet[%this.nextBullet];
	%this.nextBullet = (%this.nextBullet + 1) % %this.bulletPoolSize;

	%angle = %this.owner.aimAngle;
	%muzzle = %this.owner.getMuzzlePosition();

	if (isEventPending(%bullet.recycleEvent))
		cancel(%bullet.recycleEvent);

	%bullet.setPosition(%muzzle);
	%bullet.setAngle(%angle);
	%bullet.setActive(true);
	%bullet.setVisible(true);
	%bullet.setAwake(true);
	%bullet.setLinearVelocityPolar(%angle, %this.bulletSpeed);
	%bullet.recycleEvent = %bullet.schedule(%this.bulletLife, "recycle");

	Audio.PlaySound("PlanetXGame:laser");

	%this.addHeat(%this.heatPerShot);
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

	if (%this.gunHeat >= 1)
	{
		%this.gunHeat = 1;
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

		if (%this.gunHeat <= %this.heatResumeThreshold)
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
/// the level's HUD does not exist; the HUD starts at zero on its own.)
function PlanetXWeapon::updateHeatBar(%this)
{
	%level = PlanetXGame.level;
	if (isObject(%level) && isObject(%level.hud) && isObject(%this.owner))
		%level.hud.setHeat(%this.owner.playerIndex, %this.gunHeat, %this.overheated, %this.heatTickMs);
}
