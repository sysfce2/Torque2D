//-----------------------------------------------------------------------------
// PlanetXBullet: a laser bolt. Pooled by the weapon and parked off-world when
// idle, so firing never allocates. The weapon positions and launches it; the
// bullet handles its own collision, recycling, and parking.
//-----------------------------------------------------------------------------

function PlanetXBullet::onAdd(%this)
{
	%this.setSize("1 0.5");
	%this.setSceneLayer($PlanetX::BulletLayer);
	%this.setSceneGroup($PlanetX::BulletGroup);
	%this.setImage("PlanetXGame:bolt");
	%this.createCircleCollisionShape(0.25);
	%this.setCollisionGroups($PlanetX::AlienGroup SPC $PlanetX::WallGroup);
	%this.setCollisionCallback(true);
	%this.setBullet(true);

	// Bolts fly at the angle they were fired at. Without this, a collision gives
	// the body angular velocity which survives park() and recycling - pooled
	// bullets came back visibly spinning.
	%this.setFixedAngle(true);
}

/// Cancel any pending recycle so no orphaned event outlives the bullet.
function PlanetXBullet::onRemove(%this)
{
	if (isEventPending(%this.recycleEvent))
		cancel(%this.recycleEvent);
}

function PlanetXBullet::onCollision(%this, %object, %collisionDetails)
{
	if (%object.isEnemy)
		%object.takeDamage(1);

	if (isObject(PlanetXGame.level))
		PlanetXGame.level.playBurst(%this.getPosition());

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
