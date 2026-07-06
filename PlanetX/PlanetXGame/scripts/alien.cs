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

// Nest generation: aliens spawn where a noise channel exceeds the threshold,
// so they come in clusters. The safe radius keeps the landing site clear.
$PlanetX::AlienNestZoom = 0.08;
$PlanetX::AlienNestThreshold = 0.7;
$PlanetX::AlienNestStep = 6;
$PlanetX::AlienSafeRadius = 26;
$PlanetX::MaxAliens = 90;

// The brute: a hulking dark-shelled variant with four times the health.
$PlanetX::BruteSize = 3.5;

/// Difficulty scaling. The base constants above describe level 1; each level
/// up makes the swarm denser (lower nest threshold), adds a brute, and makes
/// every alien tougher, faster, and harder-hitting. Caps keep late levels
/// brutal but survivable. Called before buildLevel for every level.
function PlanetXGame::applyDifficulty(%this)
{
	%step = %this.level - 1;

	$PlanetX::CurAlienHealth   = $PlanetX::AlienHealth + mFloor(%step / 2);
	$PlanetX::CurBruteHealth   = 4 * $PlanetX::CurAlienHealth;
	$PlanetX::CurChaseSpeed    = mClamp($PlanetX::AlienChaseSpeed + 0.25 * %step, 0, 11);
	$PlanetX::CurContactDamage = mClamp($PlanetX::AlienContactDamage + %step, 0, 25);
	$PlanetX::CurNestThreshold = mClamp($PlanetX::AlienNestThreshold - 0.015 * %step, 0.62, 1);
	$PlanetX::CurBruteBonus    = mClamp(%step, 0, 8);

	echo("PlanetX: difficulty for level" SPC %this.level
		SPC "- hp" SPC $PlanetX::CurAlienHealth
		SPC "speed" SPC $PlanetX::CurChaseSpeed
		SPC "damage" SPC $PlanetX::CurContactDamage
		SPC "threshold" SPC $PlanetX::CurNestThreshold);
}

/// Populate the planet from the level's noise field: sample a coarse grid,
/// spawn an alien wherever the nest channel runs hot. Clustered by nature -
/// contiguous hot regions of the noise become nests.
function PlanetXGame::spawnAliens(%this)
{
	%count = 0;
	%step = $PlanetX::AlienNestStep;
	%rangeX = $PlanetX::WorldHalfWidth - %step;
	%rangeY = $PlanetX::WorldHalfHeight - %step;

	for (%wy = -%rangeY; %wy <= %rangeY; %wy += %step)
	{
		for (%wx = -%rangeX; %wx <= %rangeX; %wx += %step)
		{
			%value = %this.generator.getNoise(
				%wx * $PlanetX::AlienNestZoom + 700.13,
				%wy * $PlanetX::AlienNestZoom + 700.13);

			if (%value < $PlanetX::CurNestThreshold)
				continue;

			// Jitter off the grid using a second, finer noise channel.
			%jx = (%this.generator.getNoise(%wx * 0.31 + 1300.7, %wy * 0.31) - 0.5) * %step;
			%jy = (%this.generator.getNoise(%wx * 0.31, %wy * 0.31 + 1300.7) - 0.5) * %step;
			%position = %wx + %jx SPC %wy + %jy;

			// Keep the landing site clear.
			%toPlayer = Vector2Sub(%position, %this.player.getPosition());
			if (Vector2Length(%toPlayer) < $PlanetX::AlienSafeRadius)
				continue;

			%this.spawnAlien(%position);
			%count++;

			if (%count >= $PlanetX::MaxAliens)
			{
				echo("PlanetX:" SPC %count SPC "aliens (capped)");
				return;
			}
		}
	}

	echo("PlanetX:" SPC %count SPC "aliens spawned");
}

/// Three or four brutes at random spots, nudged away from the landing site
/// if one lands on it.
function PlanetXGame::spawnBrutes(%this)
{
	%count = getRandom(3, 4) + $PlanetX::CurBruteBonus;

	for (%i = 0; %i < %count; %i++)
	{
		%position = %this.randomWorldPoint();

		%toPlayer = Vector2Sub(%position, %this.player.getPosition());
		if (Vector2Length(%toPlayer) < $PlanetX::AlienSafeRadius * 2)
		{
			%angle = mAtan(%toPlayer);
			%position = %this.clampToWorld(Vector2Add(%this.player.getPosition(),
				Vector2Direction(%angle, $PlanetX::AlienSafeRadius * 2)));
		}

		%this.spawnBrute(%position);
	}

	echo("PlanetX:" SPC %count SPC "brutes spawned");
}

function PlanetXGame::spawnBrute(%this, %position)
{
	%brute = new Sprite()
	{
		class = "PlanetXAlien";
		Position = %position;
		Size = $PlanetX::BruteSize SPC $PlanetX::BruteSize;
		SceneLayer = $PlanetX::EntityLayer;
		SceneGroup = $PlanetX::AlienGroup;
		Animation = "PlanetXGame:bruteWalkAnim";
	};

	// Feet-centric collision, feet-centric Y-sort key - scaled up.
	%brute.createCircleCollisionShape(1.1, 0, -0.45);
	%brute.setCollisionGroups($PlanetX::PlayerGroup SPC $PlanetX::AlienGroup SPC
		$PlanetX::BulletGroup SPC $PlanetX::WallGroup);
	%brute.setCollisionCallback(true);
	%brute.setSortPoint(0, -1.6);
	%brute.setFixedAngle(true);

	%chase = ChaseBehavior.createInstance();
	%chase.initialize(%this.player, $PlanetX::CurChaseSpeed,
		$PlanetX::AlienWanderSpeed, $PlanetX::AlienAggroRadius);
	%brute.addBehavior(%chase);

	%damage = TakesDamageBehavior.createInstance();
	%damage.initialize($PlanetX::CurBruteHealth);
	%brute.addBehavior(%damage);

	PlanetXScene.add(%brute);
	return %brute;
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
	%chase.initialize(%this.player, $PlanetX::CurChaseSpeed,
		$PlanetX::AlienWanderSpeed, $PlanetX::AlienAggroRadius);
	%alien.addBehavior(%chase);

	%damage = TakesDamageBehavior.createInstance();
	%damage.initialize($PlanetX::CurAlienHealth);
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

	%object.takeDamage($PlanetX::CurContactDamage);
}
