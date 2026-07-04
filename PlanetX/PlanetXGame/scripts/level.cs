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

// Scene layers, back (high) to front (low).
$PlanetX::TileLayer    = 30;
$PlanetX::RockLayer    = 25;
$PlanetX::CrystalLayer = 24;
$PlanetX::RocketLayer  = 21;
$PlanetX::AlienLayer   = 20;
$PlanetX::PlayerLayer  = 15;
$PlanetX::BulletLayer  = 12;
$PlanetX::EffectLayer  = 10;

$PlanetX::WorldHalfWidth  = 96;
$PlanetX::WorldHalfHeight = 72;

function PlanetXGame::buildLevel(%this)
{
	new Scene(PlanetXScene);
	PlanetXScene.setGravity(0, 0);

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
		Profile = "GuiDefaultProfile";
		HorizSizing = "relative";
		VertSizing = "relative";
		Position = "0 0";
		Extent = "1024 768";
	};
	PlanetXRoot.add(PlanetXWindow);

	PlanetXWindow.setScene(PlanetXScene);
	PlanetXWindow.setCameraSize(40, 30);
	PlanetXWindow.setViewLimitOn(-$PlanetX::WorldHalfWidth, -$PlanetX::WorldHalfHeight,
		$PlanetX::WorldHalfWidth, $PlanetX::WorldHalfHeight);

	%this.buildTileMap();
	%this.buildBarriers();
	%this.buildRocks();
	%this.buildRocket("-86 -52");

	%this.createBulletPools();
	%this.buildCrosshair();

	%this.player = %this.spawnPlayer("-78 -58");

	// Rigid camera follow, no rotation.
	PlanetXWindow.mount(%this.player, "0 0", 0, true, false);

	%this.buildCrystal("80 58");
	%this.spawnAliens();
	%this.buildHud();
	%this.pushControls();
}

//-----------------------------------------------------------------------------

function PlanetXGame::buildTileMap(%this)
{
	%map = new CompositeSprite()
	{
		SceneLayer = $PlanetX::TileLayer;
	};

	// Layout must be set before any sprite is added.
	%map.setBatchLayout("rect");
	%map.setDefaultSpriteStride(4, 4);
	%map.setDefaultSpriteSize(4, 4);

	// Logical coords are scaled by the stride; offsetting the composite by
	// half a tile makes the 48x36 grid span exactly -96..96 x -72..72.
	%map.setPosition(2, 2);

	for (%x = -24; %x < 24; %x++)
	{
		for (%y = -18; %y < 18; %y++)
		{
			%map.addSprite(%x SPC %y);
			%map.setSpriteImage("PlanetXGame:tiles", %this.pickTileFrame());
		}
	}

	%map.setBodyType("static");
	%map.setCollisionSuppress(true);

	PlanetXScene.add(%map);
	%this.tileMap = %map;
}

/// Weighted pick over the 16-frame tile sheet: mostly plain ground with
/// occasional dark patches, cracks, crystal veins, and rubble.
function PlanetXGame::pickTileFrame(%this)
{
	%roll = getRandom(0, 99);

	if (%roll < 80)
		return getRandom(0, 5);   // red ground
	if (%roll < 87)
		return getRandom(6, 9);   // darker magenta patches
	if (%roll < 93)
		return getRandom(10, 11); // cracked
	if (%roll < 97)
		return getRandom(12, 13); // crystal veins
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
	%rocks = "-60 -30 5" TAB "-40 -55 4" TAB "-30 -10 6" TAB "-55 20 5" TAB
	         "-20 40 4" TAB "-5 -40 5" TAB "0 15 6" TAB "20 -20 4" TAB
	         "25 55 5" TAB "40 -55 6" TAB "45 10 4" TAB "60 -30 5" TAB
	         "60 40 6" TAB "75 20 4" TAB "35 30 5" TAB "-75 55 5";

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
			SceneLayer = $PlanetX::RockLayer;
			SceneGroup = $PlanetX::WallGroup;
			Image = "PlanetXGame:rock" @ (1 + (%i % 2));
		};
		%rock.setBodyType("static");
		%rock.createCircleCollisionShape(%size * 0.38);
		PlanetXScene.add(%rock);
	}
}

//-----------------------------------------------------------------------------

/// The aiming reticle: a decorative sprite that follows the mouse (input.cs).
function PlanetXGame::buildCrosshair(%this)
{
	%crosshair = new Sprite()
	{
		Size = "2 2";
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
		Size = "9 18";
		SceneLayer = $PlanetX::RocketLayer;
		SceneGroup = $PlanetX::WallGroup;
		Image = "PlanetXGame:rocket";
	};
	%rocket.setBodyType("static");
	%rocket.createPolygonBoxCollisionShape(5, 4, "0 -6");
	PlanetXScene.add(%rocket);
}
