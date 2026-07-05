//-----------------------------------------------------------------------------
// PlanetX level construction: scene, scene window, tile map, boundary walls,
// rock obstacles, and the crashed rocket the spaceman arrived in.
//
// World is 192x144 units: a 48x36 rectilinear tile map of 4-unit tiles,
// centered on the origin. The camera shows a 40x30 window onto it.
//-----------------------------------------------------------------------------

// Scene group ids. Collision groups are whitelists: a body only collides with
// the groups it lists.
$PlanetX::PlayerGroup = 1;
$PlanetX::AlienGroup  = 2;
$PlanetX::BulletGroup = 3;
$PlanetX::WallGroup   = 4;
$PlanetX::PickupGroup = 5;

// Scene layers, back (high) to front (low). Everything that stands on the
// ground shares EntityLayer, which is depth-sorted by -Y (lower on screen =
// drawn in front) for the beat-em-up look.
$PlanetX::TileLayer   = 30;
$PlanetX::EntityLayer = 20;
$PlanetX::BulletLayer = 12;
$PlanetX::EffectLayer = 10;

$PlanetX::WorldHalfWidth  = 96;
$PlanetX::WorldHalfHeight = 72;

function PlanetXGame::buildLevel(%this)
{
	new Scene(PlanetXScene);
	PlanetXScene.setGravity(0, 0);

	// Y-sort the ground entities: higher world-Y renders first (behind).
	PlanetXScene.setLayerSortMode($PlanetX::EntityLayer, "-Y");

	// Root GUI control so HUD elements can overlay the scene window.
	new GuiControl(PlanetXRoot)
	{
		Profile = "GuiDefaultProfile";
		HorizSizing = "relative";
		VertSizing = "relative";
		Position = "0 0";
		Extent = "1024 768";
	};

	new SceneWindow(PlanetXWindow)
	{
		class = "PlanetXSceneWindow";
		Profile = "GuiDefaultProfile";
		HorizSizing = "relative";
		VertSizing = "relative";
		Position = "0 0";
		Extent = "1024 768";
	};
	PlanetXRoot.add(PlanetXWindow);

	PlanetXWindow.setScene(PlanetXScene);
	PlanetXWindow.setCameraSize(60, 45);

	// The camera size above assumes a 4:3 window; refit it to whatever shape
	// the window actually has right now (it may have been resized while the
	// title screen was up).
	PlanetXWindow.updateCameraAspect();

	PlanetXWindow.setViewLimitOn(-$PlanetX::WorldHalfWidth, -$PlanetX::WorldHalfHeight,
		$PlanetX::WorldHalfWidth, $PlanetX::WorldHalfHeight);

	// One seeded generator drives the terrain colors, the objective
	// placements, and the alien nests, so a level is fully described by
	// its seed.
	%this.generator = new NoiseGenerator();
	%this.generator.setSeed(getRandom(1, 999999));

	%this.buildTileMap();
	%this.buildBarriers();
	%this.buildRocks();

	// Drop the rocket and the crystal at noise-chosen spots, pushed apart
	// if they landed too close together.
	%rocketPosition = %this.noiseWorldPoint(101.3, 897.7);
	%crystalPosition = %this.noiseWorldPoint(431.9, 213.1);

	%delta = Vector2Sub(%crystalPosition, %rocketPosition);
	%distance = Vector2Length(%delta);
	if (%distance < $PlanetX::MinObjectiveDistance)
	{
		// Push both away from their midpoint along the line between them.
		// Degenerate overlap: pick an arbitrary separation axis.
		%angle = %distance < 0.1 ? 0 : mAtan(%delta);

		%mid = (getWord(%rocketPosition, 0) + getWord(%crystalPosition, 0)) / 2 SPC
		       (getWord(%rocketPosition, 1) + getWord(%crystalPosition, 1)) / 2;
		%half = $PlanetX::MinObjectiveDistance / 2;

		%rocketPosition = %this.clampToWorld(Vector2Add(%mid, Vector2Direction(%angle + 180, %half)));
		%crystalPosition = %this.clampToWorld(Vector2Add(%mid, Vector2Direction(%angle, %half)));

		// Clamping can eat the separation when the push line runs into a
		// world edge. Fall back to the corner diagonally opposite the
		// rocket, which is always far enough.
		%delta = Vector2Sub(%crystalPosition, %rocketPosition);
		if (Vector2Length(%delta) < $PlanetX::MinObjectiveDistance)
		{
			%rangeX = $PlanetX::WorldHalfWidth - $PlanetX::PlacementMargin;
			%rangeY = $PlanetX::WorldHalfHeight - $PlanetX::PlacementMargin;
			%crystalPosition = (getWord(%rocketPosition, 0) < 0 ? %rangeX : -%rangeX) SPC
			                   (getWord(%rocketPosition, 1) < 0 ? %rangeY : -%rangeY);
		}
	}

	echo("PlanetX: objectives" SPC Vector2Length(Vector2Sub(%crystalPosition, %rocketPosition)) SPC "apart");

	%this.buildRocket(%rocketPosition);
	%this.buildCrystal(%crystalPosition);

	%this.createBulletPools();
	%this.buildCrosshair();

	// The spaceman steps out beside his rocket.
	%spawn = %this.clampToWorld(Vector2Add(%rocketPosition, "6 -3"));
	%this.player = %this.spawnPlayer(%spawn);

	// Rigid camera follow, no rotation.
	PlanetXWindow.mount(%this.player, "0 0", 0, true, false);

	%this.spawnAliens();
	%this.spawnBrutes();
	%this.buildHud();
	%this.pushControls();

	%this.generator.delete();
}

/// A noise-derived point inside the world bounds. %cx/%cy select a sampling
/// spot in the noise field, far from the terrain's own samples; the two
/// coordinates use offset spots so they are decorrelated.
function PlanetXGame::noiseWorldPoint(%this, %cx, %cy)
{
	// Stretch the mid-clustered noise toward the full 0..1 range.
	%vx = mClamp((%this.generator.getNoise(%cx, %cy) - 0.25) / 0.5, 0, 1);
	%vy = mClamp((%this.generator.getNoise(%cx + 57.3, %cy + 91.7) - 0.25) / 0.5, 0, 1);

	%rangeX = $PlanetX::WorldHalfWidth - $PlanetX::PlacementMargin;
	%rangeY = $PlanetX::WorldHalfHeight - $PlanetX::PlacementMargin;

	return -%rangeX + %vx * 2 * %rangeX SPC -%rangeY + %vy * 2 * %rangeY;
}

/// Clamp a point to the world bounds, respecting the placement margin.
function PlanetXGame::clampToWorld(%this, %point)
{
	%rangeX = $PlanetX::WorldHalfWidth - $PlanetX::PlacementMargin;
	%rangeY = $PlanetX::WorldHalfHeight - $PlanetX::PlacementMargin;

	return mClamp(getWord(%point, 0), -%rangeX, %rangeX) SPC
	       mClamp(getWord(%point, 1), -%rangeY, %rangeY);
}

//-----------------------------------------------------------------------------
// Window resizing. Keep the camera height fixed and fit its width to the
// window's aspect ratio, so resizing the window widens or narrows the view
// instead of stretching it (same pattern as the Sandbox's scene.cs).
//-----------------------------------------------------------------------------

function PlanetXSceneWindow::updateCameraAspect(%this)
{
	%extent = Canvas.extent;
	%aspect = %extent.x / %extent.y;

	%camera = %this.getCameraSize();
	%camera.x = %camera.y * %aspect;
	%this.setCameraSize(%camera);
}

/// Engine callback: fires on every live window resize.
function PlanetXSceneWindow::onExtentChange(%this)
{
	%this.updateCameraAspect();
}

//-----------------------------------------------------------------------------

// Tile grid: 96x72 tiles of 2 units, spanning -96..96 x -72..72.
$PlanetX::TileSize   = 2;
$PlanetX::TileCountX = 96;
$PlanetX::TileCountY = 72;

// Perlin terrain tuning.
$PlanetX::NoiseZoom = 0.06;
$PlanetX::NoiseOctaves = 4;
$PlanetX::NoisePersistence = 0.5;

// Objective placement: margin from the world edge and the minimum distance
// the crystal must be from the rocket.
$PlanetX::PlacementMargin = 10;
$PlanetX::MinObjectiveDistance = 100;

/// The terrain: near-white tiles tinted per-corner from a Perlin noise field.
/// Noise is sampled once per grid VERTEX ((N+1)x(M+1) samples); each tile's
/// four corners take the color of the vertices it shares with its neighbors,
/// so the Gouraud interpolation lines up seamlessly across tile seams.
function PlanetXGame::buildTileMap(%this)
{
	%startTime = getRealTime();

	%countX = $PlanetX::TileCountX;
	%countY = $PlanetX::TileCountY;

	// Pass 1: one ramp color per grid vertex.
	for (%cy = 0; %cy <= %countY; %cy++)
	{
		for (%cx = 0; %cx <= %countX; %cx++)
		{
			%value = %this.generator.getComplexNoise(%cx * $PlanetX::NoiseZoom,
				%cy * $PlanetX::NoiseZoom, $PlanetX::NoiseOctaves,
				$PlanetX::NoisePersistence);
			%corner[%cx, %cy] = %this.rocketRampColor(%value);
		}
	}

	// Pass 2: the tile batch, one shared corner color per touching tile.
	%map = new CompositeSprite()
	{
		SceneLayer = $PlanetX::TileLayer;
	};

	// Layout must be set before any sprite is added.
	%map.setBatchLayout("rect");
	%map.setBatchCulling(true);
	%map.setBatchSortMode("Batch");
	%map.setDefaultSpriteStride($PlanetX::TileSize, $PlanetX::TileSize);
	%map.setDefaultSpriteSize($PlanetX::TileSize, $PlanetX::TileSize);

	// Logical coords are scaled by the stride; offsetting the composite by
	// half a tile keeps the grid span exactly on the world bounds.
	%map.setPosition($PlanetX::TileSize / 2, $PlanetX::TileSize / 2);

	%halfX = %countX / 2;
	%halfY = %countY / 2;

	for (%x = -%halfX; %x < %halfX; %x++)
	{
		for (%y = -%halfY; %y < %halfY; %y++)
		{
			%map.addSprite(%x SPC %y);
			%map.setSpriteImage("PlanetXGame:tiles", %this.pickTileFrame());

			// This tile's bottom-left grid vertex.
			%cx = %x + %halfX;
			%cy = %y + %halfY;

			// Corner order is TL, TR, BR, BL; world +Y is up.
			%map.setSpriteUseComplexColor(true);
			%map.setSpriteComplexColor(
				%corner[%cx, %cy + 1], %corner[%cx + 1, %cy + 1],
				%corner[%cx + 1, %cy], %corner[%cx, %cy]);
		}
	}

	%map.setBodyType("static");
	%map.setCollisionSuppress(true);

	PlanetXScene.add(%map);
	%this.tileMap = %map;

	echo("PlanetX: terrain built in" SPC getRealTime() - %startTime SPC "ms");
}

/// Map a 0..1 noise value onto the Rocket Edition palette, dark to light.
/// Returns an "r g b 1" float color string for setSpriteComplexColor.
function PlanetXGame::rocketRampColor(%this, %value)
{
	// Multi-octave noise clusters around 0.5; stretch for contrast.
	%value = mClamp((%value - 0.2) / 0.6, 0, 1);

	// Ramp stops: position, r, g, b (0..1 floats).
	// #300022 -> #801946 -> #A62646 -> #C43C3E -> #F2D7DA
	if (%value < 0.3)
	{
		%t = %value / 0.3;
		%from = "0.188 0.0 0.133";
		%to = "0.502 0.098 0.275";
	}
	else if (%value < 0.55)
	{
		%t = (%value - 0.3) / 0.25;
		%from = "0.502 0.098 0.275";
		%to = "0.651 0.149 0.275";
	}
	else if (%value < 0.8)
	{
		%t = (%value - 0.55) / 0.25;
		%from = "0.651 0.149 0.275";
		%to = "0.769 0.235 0.243";
	}
	else
	{
		%t = (%value - 0.8) / 0.2;
		%from = "0.769 0.235 0.243";
		%to = "0.949 0.843 0.855";
	}

	%r = getWord(%from, 0) + (getWord(%to, 0) - getWord(%from, 0)) * %t;
	%g = getWord(%from, 1) + (getWord(%to, 1) - getWord(%from, 1)) * %t;
	%b = getWord(%from, 2) + (getWord(%to, 2) - getWord(%from, 2)) * %t;

	return %r SPC %g SPC %b SPC "1";
}

/// Weighted pick over the 16-frame tile sheet: mostly plain white with
/// occasional speckle, cracks, pebbles, and rubble details.
function PlanetXGame::pickTileFrame(%this)
{
	%roll = getRandom(0, 99);

	if (%roll < 50)
		return getRandom(0, 2);   // plain
	if (%roll < 75)
		return getRandom(3, 5);   // barely-there speckle
	if (%roll < 88)
		return getRandom(6, 9);   // speckle
	if (%roll < 93)
		return getRandom(10, 11); // hairline cracks
	if (%roll < 97)
		return getRandom(12, 13); // pebbles
	return getRandom(14, 15);     // rubble
}

//-----------------------------------------------------------------------------

/// Four invisible static walls just outside the visible world.
function PlanetXGame::buildBarriers(%this)
{
	%w = $PlanetX::WorldHalfWidth;
	%h = $PlanetX::WorldHalfHeight;
	%thickness = 4;

	%this.createBarrier(-(%w + %thickness / 2), 0, %thickness, 2 * %h + 4 * %thickness);
	%this.createBarrier(%w + %thickness / 2, 0, %thickness, 2 * %h + 4 * %thickness);
	%this.createBarrier(0, -(%h + %thickness / 2), 2 * %w, %thickness);
	%this.createBarrier(0, %h + %thickness / 2, 2 * %w, %thickness);
}

function PlanetXGame::createBarrier(%this, %x, %y, %width, %height)
{
	%wall = new SceneObject()
	{
		Size = %width SPC %height;
		Position = %x SPC %y;
	};
	%wall.setBodyType("static");
	%wall.setSceneGroup($PlanetX::WallGroup);
	%wall.createPolygonBoxCollisionShape(%width, %height);
	PlanetXScene.add(%wall);
}

//-----------------------------------------------------------------------------

/// Boulders, hand-placed to loosely channel the walk to the crystal corner.
function PlanetXGame::buildRocks(%this)
{
	%rocks = "-60 -30 3" TAB "-40 -55 2.5" TAB "-30 -10 3.5" TAB "-55 20 3" TAB
	         "-20 40 2.5" TAB "-5 -40 3" TAB "0 15 3.5" TAB "20 -20 2.5" TAB
	         "25 55 3" TAB "40 -55 3.5" TAB "45 10 2.5" TAB "60 -30 3" TAB
	         "60 40 3.5" TAB "75 20 2.5" TAB "35 30 3" TAB "-75 55 3";

	for (%i = 0; %i < getFieldCount(%rocks); %i++)
	{
		%field = getField(%rocks, %i);
		%x = getWord(%field, 0);
		%y = getWord(%field, 1);
		%size = getWord(%field, 2);

		%rock = new Sprite()
		{
			Position = %x SPC %y;
			Size = %size SPC %size;
			SceneLayer = $PlanetX::EntityLayer;
			SceneGroup = $PlanetX::WallGroup;
			Image = "PlanetXGame:rock" @ (1 + (%i % 2));
		};
		%rock.setBodyType("static");

		// Feet-centric: collision and Y-sort key sit at the boulder's base.
		%rock.createCircleCollisionShape(%size * 0.33, 0, -%size * 0.15);
		%rock.setSortPoint(0, -%size * 0.4);
		PlanetXScene.add(%rock);
	}
}

//-----------------------------------------------------------------------------

/// The aiming reticle: a decorative sprite that follows the mouse (input.cs).
function PlanetXGame::buildCrosshair(%this)
{
	%crosshair = new Sprite()
	{
		Size = "1.5 1.5";
		SceneLayer = $PlanetX::EffectLayer;
		Image = "PlanetXGame:crosshair";
	};
	%crosshair.setBodyType("static");
	%crosshair.setCollisionSuppress(true);
	PlanetXScene.add(%crosshair);

	%this.crosshair = %crosshair;
}

/// The spaceman's rocket: a tall landmark at the spawn corner with a small
/// collision footprint at its base.
function PlanetXGame::buildRocket(%this, %position)
{
	%rocket = new Sprite()
	{
		Position = %position;
		Size = "6 12";
		SceneLayer = $PlanetX::EntityLayer;
		SceneGroup = $PlanetX::WallGroup;
		Image = "PlanetXGame:rocket";
	};
	%rocket.setBodyType("static");
	%rocket.createPolygonBoxCollisionShape(3.5, 2.5, "0 -4");
	%rocket.setSortPoint(0, -5);
	PlanetXScene.add(%rocket);
}
