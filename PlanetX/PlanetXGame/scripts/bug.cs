//-----------------------------------------------------------------------------
// PlanetXBug: the common alien - a coral critter that wanders the surface and
// swarms the spaceman when he gets close. All of its behavior is PlanetXEnemy;
// this class only builds the bug's body.
//-----------------------------------------------------------------------------

function PlanetXBug::onAdd(%this)
{
	%this.init();   // shared enemy setup (marker, scene group, AI tick, ...)

	%this.setSize("2 2");
	%this.playAnimation("PlanetXGame:alienWalkAnim");

	// Feet-centric collision, feet-centric Y-sort key.
	%this.createCircleCollisionShape(0.65, 0, -0.25);
	%this.setCollisionGroups($PlanetX::PlayerGroup SPC $PlanetX::AlienGroup SPC
		$PlanetX::BulletGroup SPC $PlanetX::WallGroup);
	%this.setSortPoint(0, -0.9);
}
