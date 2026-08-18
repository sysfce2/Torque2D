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

	// A sensor, not a solid: the bolt still reports its hit (onCollision fires, as it
	// does for the crystal sensor) but takes no impulse from the contact. Without this,
	// when a forked volley lands two bolts on the same alien in one step, the bolt that
	// does NOT land the kill gets physically deflected but never receives its recycle
	// callback - the alien's death suppresses its collision mid-step - so it drifts off
	// slowly. As a sensor it just flies straight through and recycles on its next hit
	// or when its life expires.
	%this.setCollisionShapeIsSensor(0, true);

	// Bolts fly at the angle they were fired at. Without this, a collision gives
	// the body angular velocity which survives park() and recycling - pooled
	// bullets came back visibly spinning.
	%this.setFixedAngle(true);

	// The weapon stamps the real per-shot damage onto each bolt as it launches it
	// (launchBolt); this is just the un-upgraded default for a bolt that never was.
	%this.damage = 1;
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
	{
		%object.takeDamage(%this.damage);

		// Shove a surviving alien back along the bolt's travel - the physical knock the
		// old solid bolts used to give, now that the bolt is a sensor. A dead alien
		// (health spent) is on its way out, so skip it.
		if (isObject(%object) && %object.health > 0)
			%object.knockback(%this.getAngle());
	}

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
