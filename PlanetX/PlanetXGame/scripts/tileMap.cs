//-----------------------------------------------------------------------------
// PlanetXTileMap: the terrain - a CompositeSprite of 96x72 tiles, each tinted
// from a Perlin noise field so the ground reads as a rocky alien surface. The
// level passes in its noise generator (%this.generator); the tile map builds
// its whole grid in onAdd.
//
// Noise is sampled once per grid VERTEX ((N+1)x(M+1) samples); each tile's four
// corners take the color of the vertices it shares with its neighbors, so the
// Gouraud interpolation lines up seamlessly across tile seams.
//-----------------------------------------------------------------------------

// Tile grid: 96x72 tiles of 2 units, spanning -96..96 x -72..72.
$PlanetX::TileSize   = 2;
$PlanetX::TileCountX = 96;
$PlanetX::TileCountY = 72;

// Perlin terrain tuning.
$PlanetX::NoiseZoom = 0.06;
$PlanetX::NoiseOctaves = 4;
$PlanetX::NoisePersistence = 0.5;

function PlanetXTileMap::onAdd(%this)
{
	%startTime = getRealTime();

	%countX = $PlanetX::TileCountX;
	%countY = $PlanetX::TileCountY;

	%this.setSceneLayer($PlanetX::TileLayer);

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
	// Layout must be set before any sprite is added.
	%this.setBatchLayout("rect");
	%this.setBatchCulling(true);
	%this.setBatchSortMode("Batch");
	%this.setDefaultSpriteStride($PlanetX::TileSize, $PlanetX::TileSize);
	%this.setDefaultSpriteSize($PlanetX::TileSize, $PlanetX::TileSize);

	// Logical coords are scaled by the stride; offsetting the composite by half
	// a tile keeps the grid span exactly on the world bounds.
	%this.setPosition($PlanetX::TileSize / 2, $PlanetX::TileSize / 2);

	%halfX = %countX / 2;
	%halfY = %countY / 2;

	for (%x = -%halfX; %x < %halfX; %x++)
	{
		for (%y = -%halfY; %y < %halfY; %y++)
		{
			%this.addSprite(%x SPC %y);
			%this.setSpriteImage("PlanetXGame:tiles", %this.pickTileFrame());

			// This tile's bottom-left grid vertex.
			%cx = %x + %halfX;
			%cy = %y + %halfY;

			// Corner order is TL, TR, BR, BL; world +Y is up.
			%this.setSpriteUseComplexColor(true);
			%this.setSpriteComplexColor(
				%corner[%cx, %cy + 1], %corner[%cx + 1, %cy + 1],
				%corner[%cx + 1, %cy], %corner[%cx, %cy]);
		}
	}

	%this.setBodyType("static");
	%this.setCollisionSuppress(true);

	echo("PlanetX: terrain built in" SPC getRealTime() - %startTime SPC "ms");
}

/// Map a 0..1 noise value onto the Rocket Edition palette, dark to light.
/// Returns an "r g b 1" float color string for setSpriteComplexColor.
function PlanetXTileMap::rocketRampColor(%this, %value)
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
function PlanetXTileMap::pickTileFrame(%this)
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
