//-----------------------------------------------------------------------------
// ChaseBehavior: wander aimlessly until the target comes within the aggro
// radius, then chase it. Adapted from DeathBallToy's MoveTowardBehavior.
//
// The tick keeps rescheduling while the owner exists, but only acts while the
// game is in the "playing" state (Sim schedules keep firing even when the
// scene is paused for a dialog).
//-----------------------------------------------------------------------------

$PlanetX::ChaseTickMs = 400;

function PlanetXGame::createChaseBehaviorTemplate(%this)
{
	if (isObject(ChaseBehavior))
		return;

	%template = new BehaviorTemplate(ChaseBehavior);
	%template.friendlyName = "Chase";
	%template.behaviorType = "AI";
	%template.description = "Wander until the target is close, then chase it.";

	%template.addBehaviorField(target, "The object to chase.", object, "", sceneObject);
	%template.addBehaviorField(chaseSpeed, "Speed while chasing.", float, 7.0);
	%template.addBehaviorField(wanderSpeed, "Speed while wandering.", float, 3.0);
	%template.addBehaviorField(aggroRadius, "Distance at which the chase starts.", float, 18.0);

	%this.add(%template);
}

function ChaseBehavior::initialize(%this, %target, %chaseSpeed, %wanderSpeed, %aggroRadius)
{
	%this.target = %target;
	%this.chaseSpeed = %chaseSpeed;
	%this.wanderSpeed = %wanderSpeed;
	%this.aggroRadius = %aggroRadius;
}

function ChaseBehavior::onBehaviorAdd(%this)
{
	%this.wanderTicks = 0;
	%this.schedule($PlanetX::ChaseTickMs, "updateChase");
}

function ChaseBehavior::updateChase(%this)
{
	if (!isObject(%this.owner))
		return;

	%this.schedule($PlanetX::ChaseTickMs, "updateChase");

	if ($PlanetX::state !$= "playing" || !isObject(%this.target))
		return;

	%toTarget = Vector2Sub(%this.target.getPosition(), %this.owner.getPosition());
	%distance = Vector2Length(%toTarget);

	if (%distance < %this.aggroRadius)
	{
		// Chase: head straight for the target.
		%angle = mAtan(%toTarget);
		%this.owner.setLinearVelocityPolar(%angle, %this.chaseSpeed);
		%this.owner.setAngle(%angle);
		%this.wanderTicks = 0;
		return;
	}

	// Wander: hold a random heading for a few ticks, then pick a new one.
	%this.wanderTicks--;
	if (%this.wanderTicks <= 0)
	{
		%this.wanderTicks = getRandom(4, 9);
		%angle = getRandom(0, 359);
		%this.owner.setLinearVelocityPolar(%angle, %this.wanderSpeed);
		%this.owner.setAngle(%angle);
	}
}
