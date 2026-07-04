//-----------------------------------------------------------------------------
// Laser bolts and impact bursts, both pooled so firing never allocates
// mid-play (same pattern as TruckToy's projectile pool).
//-----------------------------------------------------------------------------

$PlanetX::BulletSpeed      = 40;
$PlanetX::BulletLifeMs     = 1200;
$PlanetX::FireCooldownMs   = 200;
$PlanetX::BulletPoolSize   = 16;
$PlanetX::BurstPoolSize    = 8;
$PlanetX::BulletMuzzleGap  = 1.8;

function PlanetXGame::createBulletPools(%this)
{
	for (%i = 0; %i < $PlanetX::BulletPoolSize; %i++)
	{
		%bullet = new Sprite()
		{
			class = "PlanetXBullet";
			Size = "1.5 0.75";
			SceneLayer = $PlanetX::BulletLayer;
			SceneGroup = $PlanetX::BulletGroup;
			Image = "PlanetXGame:bolt";
		};
		%bullet.createCircleCollisionShape(0.4);
		%bullet.setCollisionGroups($PlanetX::AlienGroup SPC $PlanetX::WallGroup);
		%bullet.setCollisionCallback(true);
		%bullet.setBullet(true);
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
			Size = "4 4";
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
}

//-----------------------------------------------------------------------------
// Firing. The player's angle is already aimed at the cursor (input.cs).
//-----------------------------------------------------------------------------

function PlanetXGame::fireBullet(%this)
{
	if ($PlanetX::state !$= "playing" || !isObject(%this.player))
		return;

	%now = getSimTime();
	if (%now - %this.lastFireTime < $PlanetX::FireCooldownMs)
		return;
	%this.lastFireTime = %now;

	%bullet = %this.bullet[%this.nextBullet];
	%this.nextBullet = (%this.nextBullet + 1) % $PlanetX::BulletPoolSize;

	%angle = %this.player.getAngle();
	%muzzle = Vector2Add(%this.player.getPosition(),
		Vector2Direction(%angle, $PlanetX::BulletMuzzleGap));

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
