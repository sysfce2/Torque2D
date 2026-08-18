//-----------------------------------------------------------------------------
// PlanetXRock: a static boulder. The level sets its position, size, and which
// of the two rock images (%this.variant); the rock derives its feet-centric
// collision shape and Y-sort key from that size.
//-----------------------------------------------------------------------------

function PlanetXRock::onAdd(%this)
{
	%size = getWord(%this.getSize(), 0);

	%this.setSceneLayer($PlanetX::EntityLayer);
	%this.setSceneGroup($PlanetX::WallGroup);
	%this.setImage("PlanetXGame:rock" @ %this.variant);
	%this.setBodyType("static");

	// Feet-centric: collision and Y-sort key sit at the boulder's base.
	%this.createCircleCollisionShape(%size * 0.33, 0, -%size * 0.15);
	%this.setSortPoint(0, -%size * 0.4);
}
