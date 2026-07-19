//-----------------------------------------------------------------------------
// PlanetXBlaster: the standard-issue blaster the spaceman lands with. A concrete
// PlanetXWeapon - it runs the base weapon's setup, then dials in its own feel.
// This is the template for adding a new gun: copy the file, give it a class,
// and override the stats. The player never changes.
//-----------------------------------------------------------------------------

function PlanetXBlaster::onAdd(%this)
{
	%this.init();   // base weapon: bullet pool, steam vent, heat loop, defaults

	// The blaster's tuning.
	%this.fireCooldown = 200;
	%this.bulletSpeed = 40;
	%this.bulletLife = 1200;
	%this.heatPerShot = 0.13;
	%this.heatDecayPerSecond = 0.32;
	%this.heatResumeThreshold = 0.35;
}
