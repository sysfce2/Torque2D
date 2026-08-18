//-----------------------------------------------------------------------------
// PlanetXDeathFx: a one-shot particle burst for a dying enemy. A ParticlePlayer
// that self-configures in onAdd and then sits idle until pop() drives it. The
// level owns a small round-robin pool (PlanetXLevel::createDeathFxPool), so a bug
// dying never allocates. Brutes reuse this same green pop at a larger scale.
//
// The "bugDeath" asset runs in STOP mode: each pop() emits a quick burst for the
// asset's short lifetime, then the engine stops emission and parks the player for
// reuse (unlike KILL, which would delete it and defeat the pool). See TORQUE_SCRIPT.md.
//-----------------------------------------------------------------------------

function PlanetXDeathFx::onAdd(%this)
{
	%this.Particle = "PlanetXGame:bugDeath";
	%this.setSceneLayer($PlanetX::EffectLayer);
	%this.setParticleInterpolation(true);
	%this.setBodyType("static");
	%this.setCollisionSuppress(true);
	// No stop() here - the player isn't in a scene yet. Adding it to a scene is what
	// auto-plays it, so the pool calls stop() right after PlanetXScene.add (see level.cs).
}

/// Fire the burst at %position, scaled by %scale (1 for a bug, larger for a brute).
function PlanetXDeathFx::pop(%this, %position, %scale)
{
	%this.setPosition(%position);
	%this.setSizeScale(%scale);
	%this.setForceScale(%scale);   // a bigger blast throws its debris wider too
	%this.play(true);
}
