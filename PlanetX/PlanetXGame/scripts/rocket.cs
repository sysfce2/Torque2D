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

/// The door - center-bottom of the rocket sprite, where a spaceman walks in. This
/// is where the surviving player revives a downed teammate (see camera.cs).
function PlanetXRocket::getDoorPosition(%this)
{
	%halfHeight = getWord(%this.getSize(), 1) * 0.5;
	return Vector2Add(%this.getPosition(), "0" SPC (-%halfHeight));
}
