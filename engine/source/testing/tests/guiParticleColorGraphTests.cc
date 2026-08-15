//-----------------------------------------------------------------------------
// Copyright (c) 2013 GarageGames, LLC
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
//-----------------------------------------------------------------------------

// We don't want tests in a shipping version.
#ifndef TORQUE_SHIPPING

#ifndef _UNIT_TESTING_H_
#include "testing/unitTesting.h"
#endif

#ifndef _GUI_EDIT_PARTICLE_COLOR_GRAPH_H_
#include "gui/editor/guiEditParticleColorGraph.h"
#endif

//-----------------------------------------------------------------------------
// The color graph's arithmetic: how a plot gives up room for the strip under it,
// what a channel's value is between two keys, and where the mixed color is
// allowed to bend.
//
// None of it can be tested through a control. Drawing one asks its profile for a
// font, which loads one, which registers a texture -- and this suite runs with no
// canvas and so no GL context to make one in. TextureManager asserts, and in a
// debug build an assert is a modal box, so the whole run hangs rather than fails.
// So the arithmetic is statics taking everything they use, exactly as
// GuiEditFrameStripCtrl's layout is, and the statics are what these call. They
// construct nothing.
//
// Throughout: the window is 0 to 1 and rects are 100 wide starting at 10, so
// every pixel below can be checked in your head.
//-----------------------------------------------------------------------------

typedef GuiEditParticleColorGraph ColorGraph;

static const S32 sRectLeft = 10;
static const S32 sRectWidth = 100;

// Every stop the merge can produce, so a test never has to guess a bound.
static F32 sStops[ColorGraph::smMaxGradientStops];

// The property the whole draw loop rests on: two stops that go backwards make a
// negative-width RectI, and dglDrawBlendBox draws that as a reversed quad.
static bool isStrictlyIncreasing(const F32* stops, const S32 count)
{
	for (S32 i = 1; i < count; i++)
	{
		if (stops[i] <= stops[i - 1])
		{
			return false;
		}
	}

	return true;
}

//-----------------------------------------------------------------------------
// The band under the plot
//-----------------------------------------------------------------------------

TEST(GuiParticleColorGraphTests, AGraphWithNoStripGivesUpNoRoom)
{
	// The regression guard for every ordinary graph in the particle editor: the
	// base class asks for no band, and its plot must not move by one pixel.
	ASSERT_EQ(GuiParticleGraphInspector::getUnderPlotReserve(0), 0)
		<< "A graph that asked for no band reserved room for one anyway.";

	ASSERT_EQ(GuiParticleGraphInspector::getUnderPlotReserve(-8), 0)
		<< "A negative band height should mean no band, not a negative reservation.";

	SUCCEED();
}

TEST(GuiParticleColorGraphTests, AStripCostsItselfAndAGapEitherSide)
{
	const S32 gap = GuiParticleGraphInspector::smUnderPlotGap;

	ASSERT_EQ(GuiParticleGraphInspector::getUnderPlotReserve(16), 16 + (2 * gap))
		<< "The strip has to clear the plot above it and the labels below it.";

	SUCCEED();
}

TEST(GuiParticleColorGraphTests, TheStripSitsInsideTheRoomReservedForIt)
{
	// What getUnderPlotRect and getXLabelTop are built from: the band starts one
	// gap below the plot and the labels start one gap below the band, so the two
	// can never overlap whatever the height.
	for (S32 height = 1; height <= 64; height++)
	{
		const S32 reserve = GuiParticleGraphInspector::getUnderPlotReserve(height);
		const S32 bandTop = GuiParticleGraphInspector::smUnderPlotGap;
		const S32 bandBottom = bandTop + height;

		ASSERT_LE(bandBottom, reserve)
			<< "height " << height << ": the strip ran past the room reserved for it.";
	}

	SUCCEED();
}

TEST(GuiParticleColorGraphTests, AStripHeightIsClampedButZeroIsLeftAlone)
{
	ASSERT_EQ(ColorGraph::clampStripHeight(0), 0)
		<< "Zero is how you ask for no strip, so it must survive the clamp.";
	ASSERT_EQ(ColorGraph::clampStripHeight(-4), 0)
		<< "A negative height means no strip rather than a clamped one.";
	ASSERT_EQ(ColorGraph::clampStripHeight(2), ColorGraph::smMinStripHeight)
		<< "A strip too thin to read a gradient in is pushed up to the minimum.";
	ASSERT_EQ(ColorGraph::clampStripHeight(1000), ColorGraph::smMaxStripHeight)
		<< "A strip is a reference, not the picture; it does not get the whole control.";

	SUCCEED();
}

//-----------------------------------------------------------------------------
// Axis end labels
//-----------------------------------------------------------------------------

TEST(GuiParticleColorGraphTests, ATenthIsLabelledAsATenth)
{
	// The bug this exists for. A script float is an F32 printed with "%.9g", so
	// the tightest zoom level arrived as the string "0.100000001" -- and the y
	// labels are what the plot gives up its left margin for, so eleven characters
	// of rounding error were costing the graph a third of its width.
	char buffer[32];

	ASSERT_STREQ(GuiParticleGraphInspector::formatAxisLabel(0.1f, buffer, sizeof(buffer)), "0.1")
		<< "A tenth was labelled with its binary expansion instead of with a tenth.";

	SUCCEED();
}

TEST(GuiParticleColorGraphTests, AnAxisEndIsLabelledAsShortlyAsItCanBe)
{
	// Every window edge the zoom levels can produce, on both axes, at every field
	// bound the particle editor registers. None of them may be long.
	const F32 values[] = { 0.0f, 0.1f, 0.2f, 0.25f, 0.3f, 0.5f, 0.75f, 0.9f, 1.0f,
		10.0f, 100.0f, 360.0f, 1000.0f };
	char buffer[32];

	for (S32 i = 0; i < (S32)(sizeof(values) / sizeof(values[0])); i++)
	{
		const char* label = GuiParticleGraphInspector::formatAxisLabel(values[i], buffer, sizeof(buffer));

		ASSERT_LE((S32)dStrlen(label), 6)
			<< "value " << values[i] << " was labelled '" << label
			<< "', which the plot pays for in left margin.";
	}

	SUCCEED();
}

TEST(GuiParticleColorGraphTests, AWholeNumberKeepsNoDecimalPoint)
{
	char buffer[32];

	ASSERT_STREQ(GuiParticleGraphInspector::formatAxisLabel(0.0f, buffer, sizeof(buffer)), "0")
		<< "Zero should read as zero, not as a decimal expansion of it.";
	ASSERT_STREQ(GuiParticleGraphInspector::formatAxisLabel(1.0f, buffer, sizeof(buffer)), "1")
		<< "One should read as one.";
	ASSERT_STREQ(GuiParticleGraphInspector::formatAxisLabel(1000.0f, buffer, sizeof(buffer)), "1000")
		<< "The widest field bound must not turn into exponent notation.";

	SUCCEED();
}

TEST(GuiParticleColorGraphTests, ALabelWithNowhereToGoIsEmptyRatherThanWritten)
{
	ASSERT_STREQ(GuiParticleGraphInspector::formatAxisLabel(0.5f, NULL, 32), "")
		<< "A missing buffer must be refused rather than written through.";

	SUCCEED();
}

//-----------------------------------------------------------------------------
// Snapping the plot to the grid
//-----------------------------------------------------------------------------

TEST(GuiParticleColorGraphTests, ThePlotShrinksToWholeGridCellsAndRecenters)
{
	// 97 wide loses 7 and moves right by 3; 83 tall loses 3 and moves down by 1.
	const RectI snapped = GuiParticleGraphInspector::snapRectToGrid(RectI(10, 20, 97, 83), 10);

	ASSERT_EQ(snapped.extent.x, 90) << "The width was not reduced to whole grid cells.";
	ASSERT_EQ(snapped.extent.y, 80) << "The height was not reduced to whole grid cells.";
	ASSERT_EQ(snapped.point.x, 13) << "What the width gave up was not split either side.";
	ASSERT_EQ(snapped.point.y, 21) << "What the height gave up was not split either side.";

	SUCCEED();
}

TEST(GuiParticleColorGraphTests, SnappingAnAlreadySnappedPlotChangesNothing)
{
	const RectI once = GuiParticleGraphInspector::snapRectToGrid(RectI(10, 20, 90, 80), 10);
	const RectI twice = GuiParticleGraphInspector::snapRectToGrid(once, 10);

	ASSERT_EQ(once.point.x, twice.point.x) << "Snapping is not idempotent horizontally.";
	ASSERT_EQ(once.point.y, twice.point.y) << "Snapping is not idempotent vertically.";
	ASSERT_EQ(once.extent.x, twice.extent.x) << "A second snap took more width away.";
	ASSERT_EQ(once.extent.y, twice.extent.y) << "A second snap took more height away.";

	SUCCEED();
}

TEST(GuiParticleColorGraphTests, ARectMeasuredMidResizeIsLeftAlone)
{
	// A control being dragged smaller than its own labels produces this. As an
	// unsigned modulus the correction was about four billion, and the rect came
	// back wider than the screen.
	const RectI broken = RectI(10, 20, -30, -40);
	const RectI snapped = GuiParticleGraphInspector::snapRectToGrid(broken, 10);

	ASSERT_EQ(snapped.extent.x, broken.extent.x) << "A negative width was 'corrected' into something else.";
	ASSERT_EQ(snapped.extent.y, broken.extent.y) << "A negative height was 'corrected' into something else.";

	SUCCEED();
}

//-----------------------------------------------------------------------------
// Reading a channel
//-----------------------------------------------------------------------------

TEST(GuiParticleColorGraphTests, AChannelIsFlatOutsideItsKeys)
{
	const F32 times[] = { 0.0f, 0.5f };
	const F32 values[] = { 0.2f, 0.8f };

	ASSERT_FLOAT_EQ(ColorGraph::sampleChannel(times, values, 2, -1.0f), 0.2f)
		<< "Before the first key a channel holds the first key's value.";
	ASSERT_FLOAT_EQ(ColorGraph::sampleChannel(times, values, 2, 1.0f), 0.8f)
		<< "After the last key a channel holds the last key's value, which is the flat run the curve draws to the right edge.";

	SUCCEED();
}

TEST(GuiParticleColorGraphTests, AChannelIsLinearBetweenTwoKeys)
{
	const F32 times[] = { 0.0f, 1.0f };
	const F32 values[] = { 0.0f, 1.0f };

	ASSERT_FLOAT_EQ(ColorGraph::sampleChannel(times, values, 2, 0.5f), 0.5f)
		<< "Halfway between two keys is the mean of their values.";
	ASSERT_FLOAT_EQ(ColorGraph::sampleChannel(times, values, 2, 0.25f), 0.25f)
		<< "A quarter of the way along is a quarter of the way up.";

	SUCCEED();
}

TEST(GuiParticleColorGraphTests, LandingOnAKeyReturnsThatKey)
{
	const F32 times[] = { 0.0f, 0.4f, 0.9f };
	const F32 values[] = { 0.1f, 0.7f, 0.3f };

	ASSERT_FLOAT_EQ(ColorGraph::sampleChannel(times, values, 3, 0.4f), 0.7f)
		<< "Sampling exactly on a key must return it rather than interpolating past it.";

	SUCCEED();
}

TEST(GuiParticleColorGraphTests, OneKeyIsTheWholeChannel)
{
	const F32 times[] = { 0.0f };
	const F32 values[] = { 0.6f };

	ASSERT_FLOAT_EQ(ColorGraph::sampleChannel(times, values, 1, 0.0f), 0.6f)
		<< "A channel with one key has that value at the start.";
	ASSERT_FLOAT_EQ(ColorGraph::sampleChannel(times, values, 1, 1.0f), 0.6f)
		<< "A channel with one key has that value everywhere.";

	SUCCEED();
}

TEST(GuiParticleColorGraphTests, AnEmptyChannelIsNotRead)
{
	// A field can be missing entirely -- an asset with no emitters yet, or a name
	// that is not registered. Nothing should be dereferenced to find that out.
	ASSERT_FLOAT_EQ(ColorGraph::sampleChannel(NULL, NULL, 0, 0.5f), 0.0f)
		<< "An absent channel should read as zero rather than read at all.";

	SUCCEED();
}

//-----------------------------------------------------------------------------
// Where the mixed color bends
//-----------------------------------------------------------------------------

TEST(GuiParticleColorGraphTests, AFlatColorIsOneSpan)
{
	const F32 times[] = { 0.0f };

	const S32 count = ColorGraph::buildGradientStops(times, 1, times, 1, times, 1,
		0.0f, 1.0f, sStops, ColorGraph::smMaxGradientStops);

	ASSERT_EQ(count, 2) << "Three channels with nothing but a key at zero can only bend at the window's edges.";
	ASSERT_FLOAT_EQ(sStops[0], 0.0f) << "The first stop is the left edge of the window.";
	ASSERT_FLOAT_EQ(sStops[1], 1.0f) << "The last stop is the right edge of the window.";

	SUCCEED();
}

TEST(GuiParticleColorGraphTests, AKeyInOneChannelBendsTheMix)
{
	const F32 red[] = { 0.0f, 0.5f };
	const F32 flat[] = { 0.0f };

	const S32 count = ColorGraph::buildGradientStops(red, 2, flat, 1, flat, 1,
		0.0f, 1.0f, sStops, ColorGraph::smMaxGradientStops);

	ASSERT_EQ(count, 3) << "A key in any one channel is a bend in the mixed color.";
	ASSERT_FLOAT_EQ(sStops[1], 0.5f) << "The bend is at the key's time.";
	ASSERT_TRUE(isStrictlyIncreasing(sStops, count)) << "The stops went backwards.";

	SUCCEED();
}

TEST(GuiParticleColorGraphTests, TheSameTimeInTwoChannelsIsOneStop)
{
	const F32 keyed[] = { 0.0f, 0.5f };
	const F32 flat[] = { 0.0f };

	const S32 count = ColorGraph::buildGradientStops(keyed, 2, keyed, 2, flat, 1,
		0.0f, 1.0f, sStops, ColorGraph::smMaxGradientStops);

	ASSERT_EQ(count, 3) << "Two channels bending at the same time is still one bend.";
	ASSERT_TRUE(isStrictlyIncreasing(sStops, count)) << "A duplicate slipped through as a zero-width span.";

	SUCCEED();
}

TEST(GuiParticleColorGraphTests, KeysOnTheWindowEdgesAreNotDuplicated)
{
	const F32 times[] = { 0.0f, 1.0f };

	const S32 count = ColorGraph::buildGradientStops(times, 2, times, 2, times, 2,
		0.0f, 1.0f, sStops, ColorGraph::smMaxGradientStops);

	ASSERT_EQ(count, 2) << "The window's own edges are already stops; a key there must not be added twice.";
	ASSERT_TRUE(isStrictlyIncreasing(sStops, count)) << "The stops went backwards.";

	SUCCEED();
}

TEST(GuiParticleColorGraphTests, ZoomingInDropsTheKeysOutsideTheWindow)
{
	const F32 times[] = { 0.0f, 0.1f, 0.5f, 0.9f };
	const F32 flat[] = { 0.0f };

	const S32 count = ColorGraph::buildGradientStops(times, 4, flat, 1, flat, 1,
		0.25f, 0.75f, sStops, ColorGraph::smMaxGradientStops);

	ASSERT_EQ(count, 3) << "Only the key at 0.5 is inside a 0.25 to 0.75 window.";
	ASSERT_FLOAT_EQ(sStops[0], 0.25f) << "The strip starts where the plot starts.";
	ASSERT_FLOAT_EQ(sStops[1], 0.5f) << "The one key inside the window was lost.";
	ASSERT_FLOAT_EQ(sStops[2], 0.75f) << "The strip ends where the plot ends.";

	SUCCEED();
}

TEST(GuiParticleColorGraphTests, AWindowWithNoWidthDrawsNothing)
{
	const F32 times[] = { 0.0f, 0.5f };

	ASSERT_EQ(ColorGraph::buildGradientStops(times, 2, times, 2, times, 2,
		0.5f, 0.5f, sStops, ColorGraph::smMaxGradientStops), 0)
		<< "A window with no width has no spans to draw.";

	ASSERT_EQ(ColorGraph::buildGradientStops(times, 2, times, 2, times, 2,
		1.0f, 0.0f, sStops, ColorGraph::smMaxGradientStops), 0)
		<< "An inverted window has no spans either, and must not be drawn backwards.";

	SUCCEED();
}

TEST(GuiParticleColorGraphTests, MoreKeysThanStopsStillReachesTheEndOfTheWindow)
{
	// The documented degradation: the tail becomes one long linear span and
	// nothing else goes wrong. What must not happen is the strip stopping short.
	F32 many[40];
	for (S32 i = 0; i < 40; i++)
	{
		many[i] = (F32)i / 40.0f;
	}

	const F32 flat[] = { 0.0f };
	const S32 count = ColorGraph::buildGradientStops(many, 40, flat, 1, flat, 1,
		0.0f, 1.0f, sStops, 8);

	ASSERT_EQ(count, 8) << "The merge wrote past the buffer it was given.";
	ASSERT_FLOAT_EQ(sStops[0], 0.0f) << "The first stop is still the window's left edge.";
	ASSERT_FLOAT_EQ(sStops[count - 1], 1.0f) << "The strip stopped short of the window's right edge.";
	ASSERT_TRUE(isStrictlyIncreasing(sStops, count)) << "The stops went backwards.";

	SUCCEED();
}

//-----------------------------------------------------------------------------
// Placing a stop on screen
//-----------------------------------------------------------------------------

TEST(GuiParticleColorGraphTests, TheStripSpansExactlyTheRectItIsGiven)
{
	ASSERT_EQ(ColorGraph::timeToPixel(0.0f, 0.0f, 1.0f, sRectLeft, sRectWidth), sRectLeft)
		<< "The window's start must land on the rect's left edge.";
	ASSERT_EQ(ColorGraph::timeToPixel(1.0f, 0.0f, 1.0f, sRectLeft, sRectWidth), sRectLeft + sRectWidth)
		<< "The window's end must land on the rect's right edge, so the strip lines up with the plot.";
	ASSERT_EQ(ColorGraph::timeToPixel(0.5f, 0.0f, 1.0f, sRectLeft, sRectWidth), sRectLeft + 50)
		<< "Half the window is half the rect.";

	SUCCEED();
}

TEST(GuiParticleColorGraphTests, ATimeOutsideTheWindowIsClampedToTheEdge)
{
	ASSERT_EQ(ColorGraph::timeToPixel(-2.0f, 0.0f, 1.0f, sRectLeft, sRectWidth), sRectLeft)
		<< "A time before the window must not draw to the left of the strip.";
	ASSERT_EQ(ColorGraph::timeToPixel(3.0f, 0.0f, 1.0f, sRectLeft, sRectWidth), sRectLeft + sRectWidth)
		<< "A time after the window must not draw past the strip.";

	SUCCEED();
}

TEST(GuiParticleColorGraphTests, ADegenerateRectOrWindowCollapsesToTheLeftEdge)
{
	ASSERT_EQ(ColorGraph::timeToPixel(0.5f, 0.0f, 1.0f, sRectLeft, 0), sRectLeft)
		<< "A rect with no width has one x, not a division by zero.";
	ASSERT_EQ(ColorGraph::timeToPixel(0.5f, 1.0f, 0.0f, sRectLeft, sRectWidth), sRectLeft)
		<< "An inverted window has no x to map to, so nothing is drawn rather than something reversed.";

	SUCCEED();
}

TEST(GuiParticleColorGraphTests, EverySpanOfTheStripHasAWidthThatIsNotNegative)
{
	// The two halves joined up. A stop list that increases must produce pixels
	// that do not decrease, at every rect width -- flooring a pair independently
	// is what would otherwise let one span start left of where the last ended.
	const F32 red[] = { 0.0f, 0.13f, 0.31f, 0.86f };
	const F32 green[] = { 0.0f, 0.5f };
	const F32 blue[] = { 0.0f, 0.13f, 0.99f };

	const S32 count = ColorGraph::buildGradientStops(red, 4, green, 2, blue, 3,
		0.0f, 1.0f, sStops, ColorGraph::smMaxGradientStops);

	ASSERT_TRUE(isStrictlyIncreasing(sStops, count)) << "The stops went backwards before they were ever placed.";

	for (S32 width = 1; width <= 400; width++)
	{
		S32 previous = ColorGraph::timeToPixel(sStops[0], 0.0f, 1.0f, sRectLeft, width);

		for (S32 i = 1; i < count; i++)
		{
			const S32 x = ColorGraph::timeToPixel(sStops[i], 0.0f, 1.0f, sRectLeft, width);

			ASSERT_GE(x, previous)
				<< "width " << width << ", stop " << i << ": a span of the strip would be drawn backwards.";

			previous = x;
		}
	}

	SUCCEED();
}

//-----------------------------------------------------------------------------
// Naming a channel
//-----------------------------------------------------------------------------

TEST(GuiParticleColorGraphTests, AChannelIsNamedEitherWayAndInAnyCase)
{
	ASSERT_EQ(ColorGraph::getChannelFromName("Red"), ColorGraph::ChannelRed)
		<< "The short name is what a script button would pass.";
	ASSERT_EQ(ColorGraph::getChannelFromName("red"), ColorGraph::ChannelRed)
		<< "Script string compares fold case, so the lookup here has to as well.";
	ASSERT_EQ(ColorGraph::getChannelFromName("RedChannel"), ColorGraph::ChannelRed)
		<< "The field's own name is the other spelling a caller will reach for.";
	ASSERT_EQ(ColorGraph::getChannelFromName("GREENCHANNEL"), ColorGraph::ChannelGreen)
		<< "Case folding has to apply to the field name too.";
	ASSERT_EQ(ColorGraph::getChannelFromName("Blue"), ColorGraph::ChannelBlue)
		<< "Blue is a channel.";

	SUCCEED();
}

TEST(GuiParticleColorGraphTests, AnythingElseIsNotAChannel)
{
	ASSERT_EQ(ColorGraph::getChannelFromName(""), ColorGraph::ChannelCount)
		<< "An empty name must be refused rather than defaulting to red.";
	ASSERT_EQ(ColorGraph::getChannelFromName(NULL), ColorGraph::ChannelCount)
		<< "A missing name must be refused without being read.";
	ASSERT_EQ(ColorGraph::getChannelFromName("AlphaChannel"), ColorGraph::ChannelCount)
		<< "Alpha is a channel, but not one of this graph's -- it keeps its own.";
	ASSERT_EQ(ColorGraph::getChannelFromName("purple"), ColorGraph::ChannelCount)
		<< "A name that is not a channel at all must be refused.";

	SUCCEED();
}

TEST(GuiParticleColorGraphTests, EveryChannelSurvivesTheRoundTripThroughItsFieldName)
{
	// The trip a toggle button takes: a channel becomes the field name the graph
	// edits, and getActiveChannel has to find its way back.
	for (S32 c = 0; c < ColorGraph::ChannelCount; c++)
	{
		const ColorGraph::Channel channel = (ColorGraph::Channel)c;

		ASSERT_EQ(ColorGraph::getChannelFromName(ColorGraph::getChannelFieldName(channel)), channel)
			<< "channel " << c << ": a channel did not survive being turned into a field name and back.";
	}

	SUCCEED();
}

TEST(GuiParticleColorGraphTests, EveryChannelDrawsInItsOwnColor)
{
	// The curve and the toggle beside it wear the same color; if two channels
	// returned the same one, two curves would be indistinguishable.
	for (S32 a = 0; a < ColorGraph::ChannelCount; a++)
	{
		const ColorI first = ColorGraph::getChannelColor((ColorGraph::Channel)a, true);

		ASSERT_GT(ColorGraph::getChannelColor((ColorGraph::Channel)a, false).alpha, 0)
			<< "channel " << a << ": an inactive curve is dimmed, not hidden.";
		ASSERT_LT(ColorGraph::getChannelColor((ColorGraph::Channel)a, false).alpha, first.alpha)
			<< "channel " << a << ": the inactive curve is not dimmer than the live one.";

		for (S32 b = a + 1; b < ColorGraph::ChannelCount; b++)
		{
			const ColorI second = ColorGraph::getChannelColor((ColorGraph::Channel)b, true);

			ASSERT_FALSE(first.red == second.red && first.green == second.green && first.blue == second.blue)
				<< "channels " << a << " and " << b << " draw in the same color.";
		}
	}

	SUCCEED();
}

#endif // TORQUE_SHIPPING
