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

#ifndef _GUI_EDITOR_CURSOR_CTRL_H_
#include "gui/editor/guiEditorCursorCtrl.h"
#endif

//-----------------------------------------------------------------------------
// Where a cursor actually points.
//
// Two fields decide it, and the split is not redundant: guiCanvas.cc positions
// the art at cursorPos - hotSpot, then GuiCursor::render subtracts
// (S32)(extent * renderOffset). So the pixel under the mouse is
// hotSpot + trunc(extent * renderOffset).
//
// renderOffset is a FRACTION of the art's own size, which is what makes it
// size-independent: "0.5 0.5" is the middle of a 13x17 pointer and the middle
// of a 32x32 sizer alike, so swapping one for the other does not make the shape
// appear to jump. hotSpot is the pixel nudge on top of that.
//
// The hot-spot editor draws both and drags the second, so this arithmetic is
// what its dot, its hit test and its readout all agree on. It lives in statics
// precisely so it can be checked here: rendering a cursor needs a GL context
// and a texture, and a unit test has neither.
//-----------------------------------------------------------------------------

TEST( GuiCursorHotSpotTests, HotSpotAloneIsTheAnswerWithNoRenderOffset )
{
    // The stock Default cursor: 13x17 art, hot spot one pixel in, no offset.
    const Point2I hot = GuiEditorCursorCtrl::getEffectiveHotSpot(
        Point2I( 1, 1 ), Point2F( 0.0f, 0.0f ), Point2I( 13, 17 ) );

    ASSERT_EQ( hot.x, 1 );
    ASSERT_EQ( hot.y, 1 );

    SUCCEED();
}

TEST( GuiCursorHotSpotTests, RenderOffsetIsAFractionOfTheArtSize )
{
    // The stock Move cursor: 32x32 art centred on the pointer by the anchor
    // alone, so the pointer lands in the middle and the nudge is zero.
    const Point2I move = GuiEditorCursorCtrl::getEffectiveHotSpot(
        Point2I( 0, 0 ), Point2F( 0.5f, 0.5f ), Point2I( 32, 32 ) );
    ASSERT_EQ( move.x, 16 );
    ASSERT_EQ( move.y, 16 );

    // The stock Edit cursor: 8x20 art, centered, no nudge.
    const Point2I edit = GuiEditorCursorCtrl::getEffectiveHotSpot(
        Point2I( 0, 0 ), Point2F( 0.5f, 0.5f ), Point2I( 8, 20 ) );
    ASSERT_EQ( edit.x, 4 );
    ASSERT_EQ( edit.y, 10 );

    SUCCEED();
}

// The point of a fractional offset: the same value centers art of any size, so
// a pointer and a sizer of different dimensions still sit under the same spot.
TEST( GuiCursorHotSpotTests, TheSameRenderOffsetCentersArtOfAnySize )
{
    const Point2F centered( 0.5f, 0.5f );

    const Point2I small = GuiEditorCursorCtrl::getEffectiveHotSpot( Point2I( 0, 0 ), centered, Point2I( 16, 16 ) );
    const Point2I large = GuiEditorCursorCtrl::getEffectiveHotSpot( Point2I( 0, 0 ), centered, Point2I( 32, 32 ) );

    ASSERT_EQ( small.x, 8 );
    ASSERT_EQ( large.x, 16 );
    ASSERT_EQ( small.x * 2, large.x ) << "A fraction must scale with the art, not sit at a fixed pixel.";

    SUCCEED();
}

// The engine truncates rather than rounds, and the editor must mark the pixel
// that will really be under the mouse - not the one that ought to be.
TEST( GuiCursorHotSpotTests, TheFractionTruncatesJustAsTheEngineDoes )
{
    // 32x16 art at an anchor of 0.5, 0.4: 16 * 0.4 is 6.4, and (S32)6.4 is 6 --
    // guiTypes.cc casts, it does not round. Any anchor other than a half or a
    // whole can land between pixels, so the editor has to reproduce the cast
    // rather than round, or its dot would sit a pixel off what the canvas draws.
    const Point2I hot = GuiEditorCursorCtrl::getEffectiveHotSpot(
        Point2I( 0, 0 ), Point2F( 0.5f, 0.4f ), Point2I( 32, 16 ) );

    ASSERT_EQ( hot.x, 16 );
    ASSERT_EQ( hot.y, 6 ) << "6.4 truncates to 6; rounding here would put the dot a pixel off.";

    // An odd extent halved does the same: 17 * 0.5 is 8.5 -> 8.
    const Point2I odd = GuiEditorCursorCtrl::getEffectiveHotSpot(
        Point2I( 0, 0 ), Point2F( 0.5f, 0.5f ), Point2I( 13, 17 ) );

    ASSERT_EQ( odd.x, 6 );
    ASSERT_EQ( odd.y, 8 );

    SUCCEED();
}

// Dragging the dot asks the inverse question: which hotSpot puts the pointer on
// the pixel I clicked? It has to invert exactly, or a drag would drift.
TEST( GuiCursorHotSpotTests, HotSpotForPixelInvertsEffectiveHotSpot )
{
    const Point2F renderOffset( 0.5f, 0.4f );
    const Point2I imageExtent( 32, 16 );

    for ( S32 x = 0; x < imageExtent.x; ++x )
    {
        for ( S32 y = 0; y < imageExtent.y; ++y )
        {
            const Point2I pixel( x, y );
            const Point2I hotSpot = GuiEditorCursorCtrl::getHotSpotForPixel( pixel, renderOffset, imageExtent );
            const Point2I roundTrip = GuiEditorCursorCtrl::getEffectiveHotSpot( hotSpot, renderOffset, imageExtent );

            ASSERT_EQ( roundTrip.x, pixel.x );
            ASSERT_EQ( roundTrip.y, pixel.y );
        }
    }

    SUCCEED();
}

// A drag writes hotSpot and leaves renderOffset alone, so aiming at the same
// pixel under a different anchor must produce a different nudge. This is what
// keeps the two fields meaningfully separate rather than one number in disguise.
TEST( GuiCursorHotSpotTests, TheNudgeAbsorbsTheAnchorChoice )
{
    const Point2I imageExtent( 16, 16 );
    const Point2I target( 4, 4 );

    const Point2I fromCorner = GuiEditorCursorCtrl::getHotSpotForPixel( target, Point2F( 0.0f, 0.0f ), imageExtent );
    const Point2I fromCenter = GuiEditorCursorCtrl::getHotSpotForPixel( target, Point2F( 0.5f, 0.5f ), imageExtent );

    ASSERT_EQ( fromCorner.x, 4 ) << "With no anchor the nudge is the pixel itself.";
    ASSERT_EQ( fromCenter.x, -4 ) << "Anchored at the middle, reaching pixel 4 means nudging back 4.";

    SUCCEED();
}

#endif // TORQUE_SHIPPING
