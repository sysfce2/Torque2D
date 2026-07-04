//-----------------------------------------------------------------------------
// TakesDamageBehavior: a health pool with a hit flash and a burst on death.
// Adapted from DeathBallToy's behavior of the same name.
//-----------------------------------------------------------------------------

function PlanetXGame::createTakesDamageBehaviorTemplate(%this)
{
	if (isObject(TakesDamageBehavior))
		return;

	%template = new BehaviorTemplate(TakesDamageBehavior);
	%template.friendlyName = "Takes Damage";
	%template.behaviorType = "Combat";
	%template.description = "Gives the owner health; it dies when health runs out.";

	%template.addBehaviorField(health, "Hit points.", float, 3.0);

	%this.add(%template);
}

function TakesDamageBehavior::initialize(%this, %health)
{
	%this.health = %health;
}

function TakesDamageBehavior::takeDamage(%this, %amount)
{
	if (!isObject(%this.owner) || %this.health <= 0)
		return;

	%this.health -= %amount;

	if (%this.health <= 0)
	{
		PlanetXGame.playBurst(%this.owner.getPosition());
		%this.owner.setCollisionSuppress(true);
		%this.owner.safeDelete();
		return;
	}

	// Hit flash.
	%this.owner.setBlendColor(1, 0.45, 0.45);
	%this.owner.schedule(120, "setBlendColor", 1, 1, 1);
}
