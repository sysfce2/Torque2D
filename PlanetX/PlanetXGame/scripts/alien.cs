//-----------------------------------------------------------------------------
// The aliens of PlanetX: coral critters that wander the surface and swarm the
// spaceman when he gets close. AI and health come from behaviors; contact
// damage is handled here.
//-----------------------------------------------------------------------------

$PlanetX::AlienHealth = 3;
$PlanetX::AlienChaseSpeed = 7;
$PlanetX::AlienWanderSpeed = 3;
$PlanetX::AlienAggroRadius = 20;
$PlanetX::AlienContactDamage = 10;
$PlanetX::AlienDamageCooldownMs = 500;

function PlanetXGame::spawnAliens(%this)
{
	// Three guard the crystal corner; the rest roam the map.
	%positions = "70 50" TAB "86 44" TAB "68 64" TAB
	             "-40 20" TAB "-10 -20" TAB "0 45" TAB
	             "30 -45" TAB "45 30" TAB "-65 45" TAB
	             "20 10" TAB "60 -10" TAB "-20 -55";

	for (%i = 0; %i < getFieldCount(%positions); %i++)
		%this.spawnAlien(getField(%positions, %i));
}

function PlanetXGame::spawnAlien(%this, %position)
{
	%alien = new Sprite()
	{
		class = "PlanetXAlien";
		Position = %position;
		Size = "2 2";
		SceneLayer = $PlanetX::EntityLayer;
		SceneGroup = $PlanetX::AlienGroup;
		Animation = "PlanetXGame:alienWalkAnim";
	};

	// Feet-centric collision, feet-centric Y-sort key.
	%alien.createCircleCollisionShape(0.65, 0, -0.25);
	%alien.setCollisionGroups($PlanetX::PlayerGroup SPC $PlanetX::AlienGroup SPC
		$PlanetX::BulletGroup SPC $PlanetX::WallGroup);
	%alien.setCollisionCallback(true);
	%alien.setSortPoint(0, -0.9);

	// The AI flips the sprite toward its heading; contacts must not spin it.
	%alien.setFixedAngle(true);

	%chase = ChaseBehavior.createInstance();
	%chase.initialize(%this.player, $PlanetX::AlienChaseSpeed,
		$PlanetX::AlienWanderSpeed, $PlanetX::AlienAggroRadius);
	%alien.addBehavior(%chase);

	%damage = TakesDamageBehavior.createInstance();
	%damage.initialize($PlanetX::AlienHealth);
	%alien.addBehavior(%damage);

	PlanetXScene.add(%alien);
	return %alien;
}

//-----------------------------------------------------------------------------

/// Contact damage against the player, with a per-alien cooldown so a lingering
/// contact doesn't drain health every physics step.
function PlanetXAlien::onCollision(%this, %object, %collisionDetails)
{
	if (%object.class !$= "PlanetXPlayer")
		return;

	%now = getSimTime();
	if (%now - %this.lastContactDamage < $PlanetX::AlienDamageCooldownMs)
		return;
	%this.lastContactDamage = %now;

	%object.takeDamage($PlanetX::AlienContactDamage);
}
