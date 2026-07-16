//-----------------------------------------------------------------------------
// PlanetXBurst: a one-shot impact flash. Pooled by the level and replayed by
// PlanetXLevel::playBurst wherever a bullet lands, a bug dies, or the spaceman
// falls. It hides itself again when its animation ends.
//-----------------------------------------------------------------------------

function PlanetXBurst::onAdd(%this)
{
	%this.setSize("2.5 2.5");
	%this.setSceneLayer($PlanetX::EffectLayer);
	%this.setImage("PlanetXGame:burst");
	%this.setBodyType("static");
	%this.setCollisionSuppress(true);
	%this.setVisible(false);
}

function PlanetXBurst::onAnimationEnd(%this)
{
	%this.setVisible(false);
}
