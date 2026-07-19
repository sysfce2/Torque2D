//-----------------------------------------------------------------------------
// PlanetXBurst: a one-shot impact flash. Pooled by the level and replayed by
// PlanetXLevel::playBurst where a bullet lands. It hides itself again when its
// animation ends. (Deaths pop particle bursts instead - see deathFx.cs and the
// player's own effect in player.cs.)
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
