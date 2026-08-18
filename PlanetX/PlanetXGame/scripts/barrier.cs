//-----------------------------------------------------------------------------
// PlanetXBarrier: an invisible static wall. The level sets its position and
// size; the barrier gives itself a matching box collision shape.
//-----------------------------------------------------------------------------

function PlanetXBarrier::onAdd(%this)
{
	%size = %this.getSize();
	%width = getWord(%size, 0);
	%height = getWord(%size, 1);

	%this.setBodyType("static");
	%this.setSceneGroup($PlanetX::WallGroup);
	%this.createPolygonBoxCollisionShape(%width, %height);
}
