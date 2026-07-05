//-----------------------------------------------------------------------------
// Laser bolts and impact bursts, both pooled so firing never allocates
// mid-play (same pattern as TruckToy's projectile pool).
//-----------------------------------------------------------------------------

$PlanetX::BulletSpeed      = 40;
$PlanetX::BulletLifeMs     = 1200;
$PlanetX::FireCooldownMs   = 200;
$PlanetX::BulletPoolSize   = 16;
$PlanetX::BurstPoolSize    = 8;

// Gun heat: each shot adds heat, which bleeds off over time. Hitting full
// heat locks the trigger until the gun cools back below the resume threshold.
$PlanetX::HeatPerShot           = 0.13;
$PlanetX::HeatDecayPerSecond    = 0.32;
$PlanetX::HeatTickMs            = 100;
$PlanetX::HeatResumeThreshold   = 0.35;

function PlanetXGame::createBulletPools(%this)
{
	for (%i = 0; %i < $PlanetX::BulletPoolSize; %i++)
	{
		%bullet = new Sprite()
		{
			class = "PlanetXBullet";
			Size = "1 0.5";
			SceneLayer = $PlanetX::BulletLayer;
			SceneGroup = $PlanetX::BulletGroup;
			Image = "PlanetXGame:bolt";
		};
		%bullet.createCircleCollisionShape(0.25);
		%bullet.setCollisionGroups($PlanetX::AlienGroup SPC $PlanetX::WallGroup);
		%bullet.setCollisionCallback(true);
		%bullet.setBullet(true);

		// Bolts fly at the angle they were fired at. Without this, a
		// collision gives the body angular velocity which survives park()
		// and recycling - pooled bullets came back visibly spinning.
		%bullet.setFixedAngle(true);
		PlanetXScene.add(%bullet);
		%bullet.park();

		%this.bullet[%i] = %bullet;
	}
	%this.nextBullet = 0;

	for (%i = 0; %i < $PlanetX::BurstPoolSize; %i++)
	{
		%burst = new Sprite()
		{
			class = "PlanetXBurst";
			Size = "2.5 2.5";
			SceneLayer = $PlanetX::EffectLayer;
			Image = "PlanetXGame:burst";
		};
		%burst.setBodyType("static");
		%burst.setCollisionSuppress(true);
		%burst.setVisible(false);
		PlanetXScene.add(%burst);

		%this.burst[%i] = %burst;
	}
	%this.nextBurst = 0;

	// One reusable steam vent for gun overheats.
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

	%this.steamPlayer = %steam;
}

//-----------------------------------------------------------------------------
// Firing. The player's angle is already aimed at the cursor (input.cs).
//-----------------------------------------------------------------------------

function PlanetXGame::fireBullet(%this)
{
	if ($PlanetX::state !$= "playing" || !isObject(%this.player))
		return;

	// An overheated gun stays locked until it cools (see heatTick).
	if (%this.overheated)
		return;

	%now = getSimTime();
	if (%now - %this.lastFireTime < $PlanetX::FireCooldownMs)
		return;
	%this.lastFireTime = %now;

	%bullet = %this.bullet[%this.nextBullet];
	%this.nextBullet = (%this.nextBullet + 1) % $PlanetX::BulletPoolSize;

	%angle = %this.player.aimAngle;
	%muzzle = %this.player.getMuzzlePosition();

	if (isEventPending(%bullet.recycleEvent))
		cancel(%bullet.recycleEvent);

	%bullet.setPosition(%muzzle);
	%bullet.setAngle(%angle);
	%bullet.setActive(true);
	%bullet.setVisible(true);
	%bullet.setAwake(true);
	%bullet.setLinearVelocityPolar(%angle, $PlanetX::BulletSpeed);
	%bullet.recycleEvent = %bullet.schedule($PlanetX::BulletLifeMs, "recycle");

	Audio.PlaySound("PlanetXGame:laser");

	%this.addGunHeat($PlanetX::HeatPerShot);
}

//-----------------------------------------------------------------------------
// Gun heat.
//-----------------------------------------------------------------------------

function PlanetXGame::resetGunHeat(%this)
{
	%this.gunHeat = 0;
	%this.overheated = false;
	%this.updateHeatBar();
}

function PlanetXGame::addGunHeat(%this, %amount)
{
	%this.gunHeat += %amount;

	if (%this.gunHeat >= 1)
	{
		%this.gunHeat = 1;
		%this.overheated = true;

		// Vent: a steam plume off the gun and a hiss.
		if (isObject(%this.steamPlayer))
		{
			%this.steamPlayer.setPosition(%this.player.getMuzzlePosition());
			%this.steamPlayer.play(true);
		}
		Audio.PlaySound("PlanetXGame:steam");
	}

	%this.updateHeatBar();
}

/// Bleed heat off over time; an overheated gun unlocks once it has cooled
/// below the resume threshold.
function PlanetXGame::heatTick(%this)
{
	if ($PlanetX::state !$= "playing")
		return;

	%this.heatEvent = %this.schedule($PlanetX::HeatTickMs, "heatTick");

	if (%this.gunHeat <= 0)
		return;

	%this.gunHeat -= $PlanetX::HeatDecayPerSecond * $PlanetX::HeatTickMs / 1000;
	if (%this.gunHeat < 0)
		%this.gunHeat = 0;

	if (%this.overheated)
	{
		// The vent plume follows the gun while it cools.
		if (isObject(%this.steamPlayer))
			%this.steamPlayer.setPosition(%this.player.getMuzzlePosition());

		if (%this.gunHeat <= $PlanetX::HeatResumeThreshold)
		{
			%this.overheated = false;

			// Let the last puffs finish rather than vanishing.
			if (isObject(%this.steamPlayer))
				%this.steamPlayer.stop(true, false);
		}
	}

	%this.updateHeatBar();
}

/// Autofire loop while the mouse button is held.
function PlanetXGame::fireTick(%this)
{
	if (!$PlanetX::firing || $PlanetX::state !$= "playing")
		return;

	%this.fireBullet();
	%this.fireEvent = %this.schedule($PlanetX::FireCooldownMs, "fireTick");
}

function PlanetXGame::stopFiring(%this)
{
	$PlanetX::firing = false;

	if (isEventPending(%this.fireEvent))
		cancel(%this.fireEvent);
}

//-----------------------------------------------------------------------------
// Bullet behavior.
//-----------------------------------------------------------------------------

function PlanetXBullet::onCollision(%this, %object, %collisionDetails)
{
	if (%object.class $= "PlanetXAlien")
	{
		%damage = %object.getBehavior("TakesDamageBehavior");
		if (isObject(%damage))
			%damage.takeDamage(1);
	}

	PlanetXGame.playBurst(%this.getPosition());
	%this.recycle();
}

function PlanetXBullet::recycle(%this)
{
	if (isEventPending(%this.recycleEvent))
		cancel(%this.recycleEvent);

	%this.park();
}

/// Deactivate and stash the bullet outside the world until it is fired again.
function PlanetXBullet::park(%this)
{
	%this.setLinearVelocity(0, 0);
	%this.setActive(false);
	%this.setVisible(false);
	%this.setPosition(0, -500);
}

//-----------------------------------------------------------------------------
// Impact bursts.
//-----------------------------------------------------------------------------

function PlanetXGame::playBurst(%this, %position)
{
	%burst = %this.burst[%this.nextBurst];
	%this.nextBurst = (%this.nextBurst + 1) % $PlanetX::BurstPoolSize;

	%burst.setPosition(%position);
	%burst.setVisible(true);
	%burst.playAnimation("PlanetXGame:burstAnim");
}

function PlanetXBurst::onAnimationEnd(%this)
{
	%this.setVisible(false);
}
