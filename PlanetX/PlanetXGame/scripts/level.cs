//-----------------------------------------------------------------------------
// PlanetXLevel: owns one playthrough level - the scene, the scene window, the
// HUD, the input controller, and every object placed in the world. Creating a
// PlanetXLevel builds the whole world in onAdd; deleting it tears the world
// back down in onRemove. PlanetXGame holds exactly one of these as %this.level.
//
// Spawner methods here set only what the LEVEL controls (which class, where it
// goes, and per-level difficulty stats); each object configures the rest of
// itself in its own onAdd. See TORQUE_SCRIPT.md.
//
// World is 192x144 units: a 96x72 tile map of 2-unit tiles centered on the
// origin. The camera shows a 60x45 window (width refit to the window aspect).
//-----------------------------------------------------------------------------

// Scene group ids. Collision groups are whitelists: a body only collides with
// the groups it lists.
$PlanetX::PlayerGroup = 1;
$PlanetX::AlienGroup  = 2;
$PlanetX::BulletGroup = 3;
$PlanetX::WallGroup   = 4;
$PlanetX::PickupGroup = 5;

// Scene layers, back (high) to front (low). Ground entities share EntityLayer,
// depth-sorted by -Y (lower on screen = drawn in front).
$PlanetX::TileLayer   = 30;
$PlanetX::EntityLayer = 20;
$PlanetX::BulletLayer = 12;
$PlanetX::EffectLayer = 10;

$PlanetX::WorldHalfWidth  = 96;
$PlanetX::WorldHalfHeight = 72;

// Objective placement: margin from the world edge and the minimum distance the
// crystal must be from the rocket.
$PlanetX::PlacementMargin = 10;
$PlanetX::MinObjectiveDistance = 100;

// Nest generation: bugs spawn where a noise channel exceeds the threshold, so
// they come in clusters. The safe radius keeps the landing site clear.
$PlanetX::AlienNestZoom = 0.08;
$PlanetX::AlienNestThreshold = 0.7;
$PlanetX::AlienNestStep = 6;
$PlanetX::AlienSafeRadius = 26;
$PlanetX::MaxAliens = 90;

$PlanetX::BurstPoolSize = 8;

// Dying enemies pop a green particle burst from this pool (bugs at scale 1, brutes
// larger - see enemy.cs). Kept small and quick, so slots free fast even in a swarm.
$PlanetX::DeathFxPoolSize = 10;

// A per-asset throttle keeps a mass aggro or a chain of deaths from blaring the
// same enemy sound across the whole swarm at once.
$PlanetX::EnemySoundThrottleMs = 200;

//-----------------------------------------------------------------------------
// Lifecycle.
//-----------------------------------------------------------------------------

function PlanetXLevel::onAdd(%this)
{
	%this.applyDifficulty();
	%this.buildScene();

	// One seed describes the whole level. The Perlin generator handles the
	// FIELDS (terrain colors, alien nests); the standard RNG - reseeded with
	// the same seed - handles the POINTS (placements, counts, tile variants).
	%this.levelSeed = getRandom(1, 999999);
	%this.generator = new NoiseGenerator();
	%this.generator.setSeed(%this.levelSeed);
	setRandomSeed(%this.levelSeed);
	echo("PlanetX: level seed" SPC %this.levelSeed);

	%this.spawnTileMap();
	%this.spawnBarriers();
	%this.spawnRocks();

	// Drop the rocket at a random spot, then draw crystal spots until one lands
	// far enough away, keeping the farthest candidate seen.
	%rocketPosition = %this.randomWorldPoint();

	%crystalPosition = %rocketPosition;
	%bestDistance = 0;
	for (%try = 0; %try < 20; %try++)
	{
		%candidate = %this.randomWorldPoint();
		%distance = Vector2Length(Vector2Sub(%candidate, %rocketPosition));

		if (%distance > %bestDistance)
		{
			%bestDistance = %distance;
			%crystalPosition = %candidate;
		}

		if (%bestDistance >= $PlanetX::MinObjectiveDistance)
			break;
	}

	echo("PlanetX: objectives" SPC Vector2Length(Vector2Sub(%crystalPosition, %rocketPosition)) SPC "apart");

	%this.spawnRocket(%rocketPosition);
	%this.spawnCrystal(%crystalPosition);
	%this.createBurstPool();
	%this.createDeathFxPool();
	%this.spawnCrosshair();

	// The spaceman steps out beside his rocket. Aim mode comes from the saved
	// settings (default: player 1 mouse, player 2 automatic).
	%spawn = %this.clampToWorld(Vector2Add(%rocketPosition, "6 -3"));
	%this.player = %this.spawnPlayer(%spawn, 1, "PlanetXGame:spacemanIdle",
		"PlanetXGame:spacemanWalkAnim", PlanetXGame.settings.get("P1Aim"));

	if ($PlanetX::twoPlayer)
	{
		// The second spaceman lands right beside the first, in his own colors.
		%spawn2 = %this.clampToWorld(Vector2Add(%rocketPosition, "3 0"));
		%this.player2 = %this.spawnPlayer(%spawn2, 2, "PlanetXGame:spacemanIdle2",
			"PlanetXGame:spacemanWalkAnim2", PlanetXGame.settings.get("P2Aim"));
	}

	// Camera: one player -> rigid follow; two -> a shared camera that frames both
	// (camera.cs). The two-player camera positions the window itself each tick, so
	// it must NOT be mounted.
	if ($PlanetX::twoPlayer)
		%this.camera = new ScriptObject() { class = "PlanetXCamera"; level = %this; };
	else
		PlanetXWindow.mount(%this.player, "0 0", 0, true, false);

	// The level tracks every live enemy (a non-owning set) so player 2's auto-aim
	// and enemy retargeting can find the nearest one. Must exist before spawning.
	%this.enemies = new SimSet();
	%this.spawnBugs();
	%this.spawnBrutes();

	// The HUD and input controller are the level's non-visual managers; each
	// builds and tears down its own pieces.
	%this.hud = new ScriptObject() { class = "PlanetXHud"; number = %this.number; };
	%this.input = new ScriptObject() { class = "PlanetXInput"; level = %this; };

	%this.generator.delete();

	// Level generation is done: give the gameplay RNG (bug wander, the next
	// level's seed) fresh time-based entropy so retries differ.
	setRandomSeed();

	// The mission begins. (One site: new runs, next levels, and retries all build
	// a fresh level, so this fires for every level start.)
	Audio.PlaySound("PlanetXGame:levelStart");
}

/// Tear the whole level down. Deleting the scene fires every SceneObject's
/// onRemove (crystal cancels its pulse, bugs cancel their chase, the player
/// deletes its weapon, ...); deleting the root frees the window and any GUI.
function PlanetXLevel::onRemove(%this)
{
	// The camera owns a schedule that pokes the window; kill it before the window.
	if (isObject(%this.camera))
		%this.camera.delete();
	if (isObject(%this.input))
		%this.input.delete();
	if (isObject(%this.hud))
		%this.hud.delete();

	if (isObject(PlanetXRoot))
		PlanetXRoot.delete();
	if (isObject(PlanetXScene))
		PlanetXScene.delete();

	// The enemy set only references its members (the scene owned and just freed
	// them), so this frees an already-empty set.
	if (isObject(%this.enemies))
		%this.enemies.delete();
}

/// Freeze the level and stop input without tearing it down - used while a
/// win/lose dialog or a fade-out is showing over the still-visible world.
function PlanetXLevel::suspend(%this)
{
	if (isObject(PlanetXScene))
		PlanetXScene.setScenePause(true);

	// Deleting the input controller restores the cursor (so dialog buttons are
	// clickable), pops the ActionMap, and cancels the aim loop.
	if (isObject(%this.input))
		%this.input.delete();
}

/// Pause the level in place: freeze the world and quiet input, but keep everything
/// alive so resume() continues exactly where it left off. Used by the pause dialog
/// (contrast suspend(), which is for a terminal win/lose and tears input down).
function PlanetXLevel::pause(%this)
{
	if (isObject(PlanetXScene))
		PlanetXScene.setScenePause(true);

	if (isObject(%this.input))
		%this.input.pause();
}

/// Unfreeze after a pause. Input restores the crosshair/cursor and re-reads any
/// bindings or aim modes changed in the options screen; the scene resumes stepping.
function PlanetXLevel::resume(%this)
{
	if (isObject(%this.input))
		%this.input.resume();

	if (isObject(PlanetXScene))
		PlanetXScene.setScenePause(false);
}

//-----------------------------------------------------------------------------
// Difficulty. Base stats describe level 1; each level up makes the swarm denser
// and every bug tougher, faster, and harder-hitting. The per-level results are
// stored on the level and passed to each bug/brute as constructor parameters.
//-----------------------------------------------------------------------------

function PlanetXLevel::applyDifficulty(%this)
{
	%step = %this.number - 1;

	%this.bugHealth      = $PlanetX::AlienHealth + mFloor(%step / 2);
	%this.bruteHealth    = 4 * %this.bugHealth;
	%this.chaseSpeed     = mClamp($PlanetX::AlienChaseSpeed + 0.25 * %step, 0, 11);
	%this.contactDamage  = mClamp($PlanetX::AlienContactDamage + %step, 0, 25);
	%this.nestThreshold  = mClamp($PlanetX::AlienNestThreshold - 0.015 * %step, 0.62, 1);
	%this.bruteBonus     = mClamp(%step, 0, 8);

	echo("PlanetX: difficulty for level" SPC %this.number
		SPC "- hp" SPC %this.bugHealth
		SPC "speed" SPC %this.chaseSpeed
		SPC "damage" SPC %this.contactDamage
		SPC "threshold" SPC %this.nestThreshold);
}

//-----------------------------------------------------------------------------
// Scene, root GUI, and camera window.
//-----------------------------------------------------------------------------

function PlanetXLevel::buildScene(%this)
{
	new Scene(PlanetXScene);
	PlanetXScene.setGravity(0, 0);

	// Y-sort the ground entities: higher world-Y renders first (behind).
	PlanetXScene.setLayerSortMode($PlanetX::EntityLayer, "-Y");

	// Root GUI control so HUD elements can overlay the scene window.
	new GuiControl(PlanetXRoot)
	{
		Profile = "PlanetXEmptyProfile";
		HorizSizing = "relative";
		VertSizing = "relative";
		Position = "0 0";
		Extent = "1024 768";
	};

	new SceneWindow(PlanetXWindow)
	{
		class = "PlanetXSceneWindow";
		Profile = "PlanetXEmptyProfile";
		HorizSizing = "relative";
		VertSizing = "relative";
		Position = "0 0";
		Extent = "1024 768";
	};
	PlanetXRoot.add(PlanetXWindow);

	PlanetXWindow.setScene(PlanetXScene);
	PlanetXWindow.setCameraSize(60, 45);
	PlanetXWindow.updateCameraAspect();
	PlanetXWindow.setViewLimitOn(-$PlanetX::WorldHalfWidth, -$PlanetX::WorldHalfHeight,
		$PlanetX::WorldHalfWidth, $PlanetX::WorldHalfHeight);
}

/// A uniformly random point inside the world bounds (seeded RNG).
function PlanetXLevel::randomWorldPoint(%this)
{
	%rangeX = $PlanetX::WorldHalfWidth - $PlanetX::PlacementMargin;
	%rangeY = $PlanetX::WorldHalfHeight - $PlanetX::PlacementMargin;

	return getRandom(-%rangeX, %rangeX) SPC getRandom(-%rangeY, %rangeY);
}

/// Clamp a point to the world bounds, respecting the placement margin.
function PlanetXLevel::clampToWorld(%this, %point)
{
	%rangeX = $PlanetX::WorldHalfWidth - $PlanetX::PlacementMargin;
	%rangeY = $PlanetX::WorldHalfHeight - $PlanetX::PlacementMargin;

	return mClamp(getWord(%point, 0), -%rangeX, %rangeX) SPC
	       mClamp(getWord(%point, 1), -%rangeY, %rangeY);
}

//-----------------------------------------------------------------------------
// Spawners. Each sets the class and only the values the level decides; the
// object's onAdd does the rest.
//-----------------------------------------------------------------------------

function PlanetXLevel::spawnTileMap(%this)
{
	// The tile map builds its own grid from the level's noise generator.
	%map = new CompositeSprite() { class = "PlanetXTileMap"; generator = %this.generator; };
	PlanetXScene.add(%map);
	%this.tileMap = %map;
}

/// Four invisible static walls just outside the visible world.
function PlanetXLevel::spawnBarriers(%this)
{
	%w = $PlanetX::WorldHalfWidth;
	%h = $PlanetX::WorldHalfHeight;
	%t = 4;

	%this.spawnBarrier(-(%w + %t / 2), 0, %t, 2 * %h + 4 * %t);
	%this.spawnBarrier(%w + %t / 2, 0, %t, 2 * %h + 4 * %t);
	%this.spawnBarrier(0, -(%h + %t / 2), 2 * %w, %t);
	%this.spawnBarrier(0, %h + %t / 2, 2 * %w, %t);
}

function PlanetXLevel::spawnBarrier(%this, %x, %y, %width, %height)
{
	%wall = new SceneObject()
	{
		class = "PlanetXBarrier";
		Position = %x SPC %y;
		Size = %width SPC %height;
	};
	PlanetXScene.add(%wall);
}

/// Boulders, hand-placed to break the open ground into loose lanes.
function PlanetXLevel::spawnRocks(%this)
{
	%rocks = "-60 -30 3" TAB "-40 -55 2.5" TAB "-30 -10 3.5" TAB "-55 20 3" TAB
	         "-20 40 2.5" TAB "-5 -40 3" TAB "0 15 3.5" TAB "20 -20 2.5" TAB
	         "25 55 3" TAB "40 -55 3.5" TAB "45 10 2.5" TAB "60 -30 3" TAB
	         "60 40 3.5" TAB "75 20 2.5" TAB "35 30 3" TAB "-75 55 3";

	for (%i = 0; %i < getFieldCount(%rocks); %i++)
	{
		%field = getField(%rocks, %i);
		%size = getWord(%field, 2);

		// Position, size, and which of the two rock images: all level-decided.
		%rock = new Sprite()
		{
			class = "PlanetXRock";
			Position = getWord(%field, 0) SPC getWord(%field, 1);
			Size = %size SPC %size;
			variant = 1 + (%i % 2);
		};
		PlanetXScene.add(%rock);
	}
}

function PlanetXLevel::spawnRocket(%this, %position)
{
	%rocket = new Sprite() { class = "PlanetXRocket"; Position = %position; };
	PlanetXScene.add(%rocket);
	%this.rocket = %rocket;
}

function PlanetXLevel::spawnCrystal(%this, %position)
{
	%crystal = new Sprite() { class = "PlanetXCrystal"; Position = %position; };
	PlanetXScene.add(%crystal);
	%this.crystal = %crystal;
}

function PlanetXLevel::spawnCrosshair(%this)
{
	%crosshair = new Sprite() { class = "PlanetXCrosshair"; };
	PlanetXScene.add(%crosshair);
	%this.crosshair = %crosshair;
}

/// Spawn a spaceman. The level decides only its identity - where it lands, which
/// player it is, its sprites, and how it aims; the player configures the rest of
/// itself in onAdd.
function PlanetXLevel::spawnPlayer(%this, %position, %index, %idleImage, %walkAnim, %aimMode)
{
	%player = new CompositeSprite()
	{
		class = "PlanetXPlayer";
		Position = %position;
		playerIndex = %index;
		idleImage = %idleImage;
		walkAnim = %walkAnim;
		aimMode = %aimMode;
	};
	PlanetXScene.add(%player);
	return %player;
}

//-----------------------------------------------------------------------------
// Co-op helpers: nearest living player (enemy targeting), nearest enemy (player
// 2 auto-aim), and reviving a downed teammate at the rocket.
//-----------------------------------------------------------------------------

/// The closest player that is still up (not downed) to %position. In single-
/// player it is always the sole player. Returns "" only if every player is down.
function PlanetXLevel::nearestLivingPlayer(%this, %position)
{
	%best = "";
	%bestDist = 0;

	%p = %this.player;
	if (isObject(%p) && !%p.downed)
	{
		%best = %p;
		%bestDist = Vector2Length(Vector2Sub(%p.getPosition(), %position));
	}

	%p = %this.player2;
	if (isObject(%p) && !%p.downed)
	{
		%dist = Vector2Length(Vector2Sub(%p.getPosition(), %position));
		if (%best $= "" || %dist < %bestDist)
			%best = %p;
	}

	return %best;
}

/// The closest live enemy to %position within %maxRange, or "" if none - backs
/// player 2's auto-aim. Every spawned enemy is kept in %this.enemies.
function PlanetXLevel::nearestEnemy(%this, %position, %maxRange)
{
	if (!isObject(%this.enemies))
		return "";

	%best = "";
	%bestDist = %maxRange;

	%count = %this.enemies.getCount();
	for (%i = 0; %i < %count; %i++)
	{
		%enemy = %this.enemies.getObject(%i);
		%dist = Vector2Length(Vector2Sub(%enemy.getPosition(), %position));
		if (%dist <= %bestDist)
		{
			%bestDist = %dist;
			%best = %enemy;
		}
	}

	return %best;
}

/// Bring a downed teammate back at the rocket's door at full health.
function PlanetXLevel::revivePlayer(%this, %player)
{
	%player.revive(%this.clampToWorld(%this.rocket.getDoorPosition()));
	Audio.PlaySound("PlanetXGame:levelStart");
}

/// Populate the planet from the level's noise field: sample a coarse grid, spawn
/// a bug wherever the nest channel runs hot. Clustered by nature.
function PlanetXLevel::spawnBugs(%this)
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

			if (%value < %this.nestThreshold)
				continue;

			// Jitter off the grid using a second, finer noise channel.
			%jx = (%this.generator.getNoise(%wx * 0.31 + 1300.7, %wy * 0.31) - 0.5) * %step;
			%jy = (%this.generator.getNoise(%wx * 0.31, %wy * 0.31 + 1300.7) - 0.5) * %step;
			%position = %wx + %jx SPC %wy + %jy;

			// Keep the landing site clear.
			if (Vector2Length(Vector2Sub(%position, %this.player.getPosition())) < $PlanetX::AlienSafeRadius)
				continue;

			%this.spawnBug(%position);
			%count++;

			if (%count >= $PlanetX::MaxAliens)
			{
				echo("PlanetX:" SPC %count SPC "bugs (capped)");
				return;
			}
		}
	}

	echo("PlanetX:" SPC %count SPC "bugs spawned");
}

/// Three or four brutes - plus one more per level, capped - at random spots,
/// nudged away from the landing site if one lands on it.
function PlanetXLevel::spawnBrutes(%this)
{
	%count = getRandom(3, 4) + %this.bruteBonus;

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

function PlanetXLevel::spawnBug(%this, %position)
{
	%bug = new Sprite()
	{
		class = "PlanetXBug";
		superclass = "PlanetXEnemy";
		Position = %position;
		target = %this.player;
		health = %this.bugHealth;
		chaseSpeed = %this.chaseSpeed;
		contactDamage = %this.contactDamage;
	};
	PlanetXScene.add(%bug);
	%this.enemies.add(%bug);

	// The level listens for this enemy's sound events (see onEnemyStartChase etc.).
	%this.startListening(%bug);
	return %bug;
}

function PlanetXLevel::spawnBrute(%this, %position)
{
	%brute = new Sprite()
	{
		class = "PlanetXBrute";
		superclass = "PlanetXEnemy";
		Position = %position;
		target = %this.player;
		health = %this.bruteHealth;
		chaseSpeed = %this.chaseSpeed;
		contactDamage = %this.contactDamage;
	};
	PlanetXScene.add(%brute);
	%this.enemies.add(%brute);

	// The level listens for this enemy's sound events (see onEnemyStartChase etc.).
	%this.startListening(%brute);
	return %brute;
}

//-----------------------------------------------------------------------------
// Impact bursts: a small pool of one-shot animated sprites, fired where a bullet
// lands. A level-wide effects service. (Deaths use particle bursts instead - see
// createDeathFxPool below and the player's own effect in player.cs.)
//-----------------------------------------------------------------------------

function PlanetXLevel::createBurstPool(%this)
{
	for (%i = 0; %i < $PlanetX::BurstPoolSize; %i++)
	{
		%burst = new Sprite() { class = "PlanetXBurst"; };
		PlanetXScene.add(%burst);
		%this.burst[%i] = %burst;
	}
	%this.nextBurst = 0;
}

function PlanetXLevel::playBurst(%this, %position)
{
	%burst = %this.burst[%this.nextBurst];
	%this.nextBurst = (%this.nextBurst + 1) % $PlanetX::BurstPoolSize;

	%burst.setPosition(%position);
	%burst.setVisible(true);
	%burst.playAnimation("PlanetXGame:burstAnim");
}

//-----------------------------------------------------------------------------
// Enemy death pops: a pool of pre-built particle bursts, replayed by playDeathFx
// wherever an alien dies. Pre-building the ParticlePlayers here means a death in
// the thick of a swarm never allocates. Brutes pass a larger scale for a bigger
// blast off the same green effect (see PlanetXDeathFx in deathFx.cs).
//-----------------------------------------------------------------------------

function PlanetXLevel::createDeathFxPool(%this)
{
	for (%i = 0; %i < $PlanetX::DeathFxPoolSize; %i++)
	{
		%fx = new ParticlePlayer() { class = "PlanetXDeathFx"; };
		PlanetXScene.add(%fx);
		%fx.stop();   // added-to-scene auto-plays; park it until the first pop
		%this.deathFx[%i] = %fx;
	}
	%this.nextDeathFx = 0;
}

function PlanetXLevel::playDeathFx(%this, %position, %scale)
{
	%fx = %this.deathFx[%this.nextDeathFx];
	%this.nextDeathFx = (%this.nextDeathFx + 1) % $PlanetX::DeathFxPoolSize;

	%fx.pop(%position, %scale);
}

//-----------------------------------------------------------------------------
// Enemy sounds: a level-wide service, alongside playBurst. Enemies announce state
// changes as object-to-object events (see enemy.cs); the level listens to every
// enemy it spawns and turns those events into distance-attenuated, throttled
// sounds. One listener for the whole swarm makes the throttle naturally global.
//-----------------------------------------------------------------------------

function PlanetXLevel::onEnemyStartChase(%this)
{
	%this.playEnemySound("PlanetXGame:enemyChase");
}

function PlanetXLevel::onEnemyStopChase(%this)
{
	%this.playEnemySound("PlanetXGame:enemyGiveUp");
}

function PlanetXLevel::onEnemyDeath(%this)
{
	%this.playEnemySound("PlanetXGame:enemyDeath");
}

/// Play an enemy event's sound at full volume, rate-limited per asset name across
/// the whole swarm so a mass aggro or a chain of deaths does not blare. Uses only
/// the Audio module's public API.
function PlanetXLevel::playEnemySound(%this, %name)
{
	// Per-asset throttle (keyed by asset id), shared across every enemy.
	%now = getSimTime();
	if (%now - %this.lastEnemySound[%name] < $PlanetX::EnemySoundThrottleMs)
		return;
	%this.lastEnemySound[%name] = %now;

	Audio.PlaySound(%name);
}
