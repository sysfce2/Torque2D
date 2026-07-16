//-----------------------------------------------------------------------------
// PlanetXRocket: the spaceman's crashed rocket - a tall landmark at the spawn
// corner with a small collision footprint at its base. The level sets only its
// position.
//-----------------------------------------------------------------------------

function PlanetXRocket::onAdd(%this)
{
	%this.setSize("6 12");
	%this.setSceneLayer($PlanetX::EntityLayer);
	%this.setSceneGroup($PlanetX::WallGroup);
	%this.setImage("PlanetXGame:rocket");
	%this.setBodyType("static");
	%this.createPolygonBoxCollisionShape(3.5, 2.5, "0 -4");
	%this.setSortPoint(0, -5);
}
