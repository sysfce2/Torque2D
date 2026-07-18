//-----------------------------------------------------------------------------
// PlanetXBrute: a hulking dark-shelled alien with far more health than a bug.
// Same PlanetXEnemy behavior, just a bigger body and a different sprite; the
// level hands it the brute health via its constructor parameters.
//-----------------------------------------------------------------------------

function PlanetXBrute::onAdd(%this)
{
	%this.init();   // shared enemy setup (marker, scene group, AI tick, ...)

	%this.setSize($PlanetX::BruteSize);
	%this.playAnimation("PlanetXGame:bruteWalkAnim");

	// Feet-centric collision, feet-centric Y-sort key - scaled up.
	%this.createCircleCollisionShape(1.1, 0, -0.45);
	%this.setCollisionGroups($PlanetX::PlayerGroup SPC $PlanetX::AlienGroup SPC
		$PlanetX::BulletGroup SPC $PlanetX::WallGroup);
	%this.setSortPoint(0, -1.6);

	// A hulking brute barely flinches when shot.
	%this.knockResist = 0.35;
}
