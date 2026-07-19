//-----------------------------------------------------------------------------
// PlanetXCrosshair: the aiming reticle - a decorative sprite that follows the
// mouse (repositioned every aim tick by PlanetXInput).
//-----------------------------------------------------------------------------

function PlanetXCrosshair::onAdd(%this)
{
	%this.setSize("1.5 1.5");
	%this.setSceneLayer($PlanetX::EffectLayer);
	%this.setImage("PlanetXGame:crosshair");
	%this.setBodyType("static");
	%this.setCollisionSuppress(true);
}
