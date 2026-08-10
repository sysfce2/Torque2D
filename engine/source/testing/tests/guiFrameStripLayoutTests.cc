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

#ifndef _GUI_EDIT_FRAME_STRIP_CTRL_H_
#include "gui/editor/guiEditFrameStripCtrl.h"
#endif

//-----------------------------------------------------------------------------
// Where cell N of a frame grid sits, and which cell a point lands on.
//
// The animation editor draws this grid twice -- as the palette of every frame an
// image offers, wrapped into rows, and as the timeline of the frames an
// animation plays, in one long line. Both call the same statics, which is what
// stops the picture and the hit test disagreeing about where a cell is.
//
// It cannot be tested any other way. Drawing one of these loads a texture, and
// this suite runs with no canvas and so no GL context to make one in;
// TextureManager asserts, and in a debug build an assert is a modal box, so the
// run hangs rather than fails. So the arithmetic takes everything it uses and
// these tests call it directly. They construct nothing.
//
// Throughout: cells are 20 wide with a 4 pixel gap, so the advance is 24 and
// every number below can be checked in your head.
//-----------------------------------------------------------------------------

static const S32 sCell = 20;
static const S32 sPad = 4;
static const S32 sAdvance = 24;

//-----------------------------------------------------------------------------
// getColumnsFor -- how many cells fit across a width.
//
// The trap is the last cell's gap. n cells span n advances LESS one pad, because
// nothing follows the last one; asking the naive question (width / advance)
// loses a column at exactly the width that fits it perfectly.
//-----------------------------------------------------------------------------

TEST( GuiFrameStripLayoutTests, AnExactFitIsNotShortOneColumn )
{
    // Four cells and the three gaps between them: 4*20 + 3*4 = 92.
    ASSERT_EQ( GuiEditFrameStripCtrl::getColumnsFor( 92, sCell, sPad ), 4 )
        << "A width that exactly holds four cells holds four cells.";

    SUCCEED();
}

TEST( GuiFrameStripLayoutTests, OnePixelShortLosesTheColumn )
{
    ASSERT_EQ( GuiEditFrameStripCtrl::getColumnsFor( 91, sCell, sPad ), 3 )
        << "One pixel under a perfect fit cannot show the fourth cell whole.";

    SUCCEED();
}

TEST( GuiFrameStripLayoutTests, TheGapAloneDoesNotBuyAColumn )
{
    // 92 through 115 are all four columns: the fifth needs its gap AND its cell.
    ASSERT_EQ( GuiEditFrameStripCtrl::getColumnsFor( 115, sCell, sPad ), 4 )
        << "Room for the gap but not the cell after it is still four columns.";
    ASSERT_EQ( GuiEditFrameStripCtrl::getColumnsFor( 116, sCell, sPad ), 5 )
        << "One more pixel completes the fifth cell.";

    SUCCEED();
}

TEST( GuiFrameStripLayoutTests, ThereIsAlwaysAtLeastOneColumn )
{
    // Not a nicety. getCellRect and cellAt both divide by the column count, and a
    // pane dragged narrower than a single cell is an ordinary thing to do.
    ASSERT_EQ( GuiEditFrameStripCtrl::getColumnsFor( 1, sCell, sPad ), 1 )
        << "A width of one pixel still reports one column, clipped, not zero.";
    ASSERT_EQ( GuiEditFrameStripCtrl::getColumnsFor( 0, sCell, sPad ), 1 )
        << "So does a width of nothing.";
    ASSERT_EQ( GuiEditFrameStripCtrl::getColumnsFor( -40, sCell, sPad ), 1 )
        << "And so does a negative one, which a mid-resize measurement can be.";

    SUCCEED();
}

//-----------------------------------------------------------------------------
// getRowCountFor
//-----------------------------------------------------------------------------

TEST( GuiFrameStripLayoutTests, APartialRowIsStillARow )
{
    ASSERT_EQ( GuiEditFrameStripCtrl::getRowCountFor( 8, 4 ), 2 )
        << "Eight cells in fours is two full rows.";
    ASSERT_EQ( GuiEditFrameStripCtrl::getRowCountFor( 9, 4 ), 3 )
        << "One cell over needs a third row to put it on.";
    ASSERT_EQ( GuiEditFrameStripCtrl::getRowCountFor( 1, 4 ), 1 )
        << "A single cell is one row, not a fraction of one.";

    SUCCEED();
}

TEST( GuiFrameStripLayoutTests, NoCellsIsNoRows )
{
    ASSERT_EQ( GuiEditFrameStripCtrl::getRowCountFor( 0, 4 ), 0 )
        << "An empty timeline occupies no rows, so it asks for no height.";

    SUCCEED();
}

//-----------------------------------------------------------------------------
// getCellRect -- the picture's half of the contract.
//-----------------------------------------------------------------------------

TEST( GuiFrameStripLayoutTests, TheFirstCellIsAtTheOrigin )
{
    const RectI cell = GuiEditFrameStripCtrl::getCellRect( 0, 4, sCell, sPad );

    ASSERT_EQ( cell.point.x, 0 ) << "Cell zero starts at the content rect's left edge.";
    ASSERT_EQ( cell.point.y, 0 ) << "And at its top.";
    ASSERT_EQ( cell.extent.x, sCell ) << "A cell is its cell size wide.";
    ASSERT_EQ( cell.extent.y, sCell ) << "And square.";

    SUCCEED();
}

TEST( GuiFrameStripLayoutTests, NeighboursStepByTheAdvanceNotTheCell )
{
    // The gap belongs between cells, so the step is 24 and not 20. Getting this
    // wrong overlaps every cell by the pad and is invisible until the pad changes.
    const RectI first = GuiEditFrameStripCtrl::getCellRect( 0, 4, sCell, sPad );
    const RectI second = GuiEditFrameStripCtrl::getCellRect( 1, 4, sCell, sPad );

    ASSERT_EQ( second.point.x - first.point.x, sAdvance )
        << "Two cells side by side are one advance apart.";
    ASSERT_EQ( second.point.y, first.point.y )
        << "And on the same row.";

    SUCCEED();
}

TEST( GuiFrameStripLayoutTests, TheNextRowStartsUnderTheFirstColumn )
{
    const RectI first = GuiEditFrameStripCtrl::getCellRect( 0, 4, sCell, sPad );
    const RectI wrapped = GuiEditFrameStripCtrl::getCellRect( 4, 4, sCell, sPad );

    ASSERT_EQ( wrapped.point.x, first.point.x )
        << "Cell four begins the second row, so it is back at the left.";
    ASSERT_EQ( wrapped.point.y - first.point.y, sAdvance )
        << "One advance down, by the same reasoning as across.";

    SUCCEED();
}

TEST( GuiFrameStripLayoutTests, OneColumnMeansOneCellPerRow )
{
    // This is the timeline's layout inverted, and the case a palette dragged very
    // narrow falls into.
    const RectI second = GuiEditFrameStripCtrl::getCellRect( 1, 1, sCell, sPad );

    ASSERT_EQ( second.point.x, 0 ) << "With one column every cell is in it.";
    ASSERT_EQ( second.point.y, sAdvance ) << "So the second cell is on the second row.";

    SUCCEED();
}

//-----------------------------------------------------------------------------
// cellAt -- the hit test's half, which must agree with getCellRect exactly.
//-----------------------------------------------------------------------------

TEST( GuiFrameStripLayoutTests, EveryCellFindsItselfBackAgain )
{
    // A 3 x 4 grid, round-tripped. If the two functions ever disagree about where
    // a cell is, this is where it shows -- and it is the disagreement, not either
    // function alone, that a user experiences as clicking the wrong frame.
    const S32 columns = 3;
    const S32 count = 12;

    for ( S32 i = 0; i < count; ++i )
    {
        const RectI cell = GuiEditFrameStripCtrl::getCellRect( i, columns, sCell, sPad );

        const Point2I topLeft( cell.point.x, cell.point.y );
        const Point2I middle( cell.point.x + (sCell / 2), cell.point.y + (sCell / 2) );
        const Point2I bottomRight( (cell.point.x + sCell) - 1, (cell.point.y + sCell) - 1 );

        ASSERT_EQ( GuiEditFrameStripCtrl::cellAt( topLeft, columns, count, sCell, sPad ), i )
            << "A cell's own top left corner belongs to it.";
        ASSERT_EQ( GuiEditFrameStripCtrl::cellAt( middle, columns, count, sCell, sPad ), i )
            << "So does its middle.";
        ASSERT_EQ( GuiEditFrameStripCtrl::cellAt( bottomRight, columns, count, sCell, sPad ), i )
            << "And its last pixel, which is one short of the next advance.";
    }

    SUCCEED();
}

TEST( GuiFrameStripLayoutTests, TheGapBelongsToNobody )
{
    // Landing between two cells must not round to the nearer one. The gap is
    // where a click means "not that one", and on the timeline it is also where
    // the insertion caret lives -- a different question with a different answer.
    const Point2I inTheGap( sCell + 1, sCell / 2 );
    ASSERT_EQ( GuiEditFrameStripCtrl::cellAt( inTheGap, 4, 12, sCell, sPad ), -1 )
        << "A point in the horizontal gap is on no cell.";

    const Point2I belowTheRow( sCell / 2, sCell + 1 );
    ASSERT_EQ( GuiEditFrameStripCtrl::cellAt( belowTheRow, 4, 12, sCell, sPad ), -1 )
        << "Nor is one in the gap between rows.";

    SUCCEED();
}

TEST( GuiFrameStripLayoutTests, TheEmptyTailOfALastRowIsEmpty )
{
    // Ten cells in fours leaves two empty places on the third row. They look like
    // cells to the arithmetic -- same row, valid column -- and are not.
    const S32 columns = 4;
    const S32 count = 10;

    const RectI wouldBeTen = GuiEditFrameStripCtrl::getCellRect( 10, columns, sCell, sPad );
    const Point2I middle( wouldBeTen.point.x + (sCell / 2), wouldBeTen.point.y + (sCell / 2) );

    ASSERT_EQ( GuiEditFrameStripCtrl::cellAt( middle, columns, count, sCell, sPad ), -1 )
        << "The eleventh place holds no cell, so a click there hits nothing.";

    const RectI ninth = GuiEditFrameStripCtrl::getCellRect( 9, columns, sCell, sPad );
    const Point2I onNine( ninth.point.x + (sCell / 2), ninth.point.y + (sCell / 2) );

    ASSERT_EQ( GuiEditFrameStripCtrl::cellAt( onNine, columns, count, sCell, sPad ), 9 )
        << "But the last real cell on that row is still hittable.";

    SUCCEED();
}

TEST( GuiFrameStripLayoutTests, PastTheLastColumnIsNotTheNextRow )
{
    // x beyond the grid must not wrap. Divide without the column check and a
    // click to the right of a four-column grid lands on row+1, column 0.
    const Point2I pastTheRight( (4 * sAdvance) + 2, sCell / 2 );

    ASSERT_EQ( GuiEditFrameStripCtrl::cellAt( pastTheRight, 4, 12, sCell, sPad ), -1 )
        << "Right of the last column is off the grid, not onto the next row.";

    SUCCEED();
}

TEST( GuiFrameStripLayoutTests, NegativeCoordinatesHitNothing )
{
    // Integer division truncates towards zero, so -1 / 24 is 0 -- a point above
    // and left of the grid would otherwise land on cell zero.
    ASSERT_EQ( GuiEditFrameStripCtrl::cellAt( Point2I( -1, 5 ), 4, 12, sCell, sPad ), -1 )
        << "Left of the grid is off it.";
    ASSERT_EQ( GuiEditFrameStripCtrl::cellAt( Point2I( 5, -1 ), 4, 12, sCell, sPad ), -1 )
        << "So is above it.";

    SUCCEED();
}

TEST( GuiFrameStripLayoutTests, AnEmptyGridHasNothingToHit )
{
    ASSERT_EQ( GuiEditFrameStripCtrl::cellAt( Point2I( 5, 5 ), 4, 0, sCell, sPad ), -1 )
        << "An animation with no frames yet answers every click with nothing.";

    SUCCEED();
}

//-----------------------------------------------------------------------------
// getContentExtent -- what the scroller is told, and therefore whether a bar
// appears. A trailing pad here is a scroll bar for a gap.
//-----------------------------------------------------------------------------

TEST( GuiFrameStripLayoutTests, TheLastCellPaysNoTrailingGap )
{
    // Four cells across: 4*20 + 3*4 = 92, not 96.
    const Point2I extent = GuiEditFrameStripCtrl::getContentExtent( 4, 4, sCell, sPad );

    ASSERT_EQ( extent.x, 92 ) << "One row of four is four cells and the three gaps between them.";
    ASSERT_EQ( extent.y, sCell ) << "And exactly one cell tall, with no gap under it.";

    SUCCEED();
}

TEST( GuiFrameStripLayoutTests, AWrappedGridIsAsWideAsItsColumnsAndAsTallAsItsRows )
{
    // Ten cells in fours: three rows, the last of them short, but the width is
    // still four columns because two of them are full.
    const Point2I extent = GuiEditFrameStripCtrl::getContentExtent( 10, 4, sCell, sPad );

    ASSERT_EQ( extent.x, 92 ) << "Four columns wide.";
    ASSERT_EQ( extent.y, 68 ) << "Three rows: 3*20 + 2*4.";

    SUCCEED();
}

TEST( GuiFrameStripLayoutTests, FewerCellsThanColumnsDoesNotPadTheWidth )
{
    // Two frames in a palette wide enough for eight. Claiming eight columns of
    // width would give the scroller a horizontal bar over empty space.
    const Point2I extent = GuiEditFrameStripCtrl::getContentExtent( 2, 8, sCell, sPad );

    ASSERT_EQ( extent.x, 44 ) << "Two cells and the one gap between them.";
    ASSERT_EQ( extent.y, sCell ) << "One row.";

    SUCCEED();
}

TEST( GuiFrameStripLayoutTests, NoCellsTakeNoRoom )
{
    const Point2I extent = GuiEditFrameStripCtrl::getContentExtent( 0, 4, sCell, sPad );

    ASSERT_EQ( extent.x, 0 ) << "An empty grid asks for no width...";
    ASSERT_EQ( extent.y, 0 ) << "...and no height, rather than one empty cell's worth.";

    SUCCEED();
}

TEST( GuiFrameStripLayoutTests, TheExtentAgreesWithTheLastCellsRect )
{
    // The two are computed apart, so this is the check that they cannot drift:
    // whatever the extent claims must be exactly enough to hold the last cell.
    const S32 columns = 4;
    const S32 count = 10;

    const Point2I extent = GuiEditFrameStripCtrl::getContentExtent( count, columns, sCell, sPad );
    const RectI last = GuiEditFrameStripCtrl::getCellRect( count - 1, columns, sCell, sPad );

    ASSERT_LE( last.point.y + last.extent.y, extent.y )
        << "The last cell must fit inside the height the scroller was given.";
    ASSERT_EQ( GuiEditFrameStripCtrl::getRowCountFor( count, columns ) * sAdvance - sPad, extent.y )
        << "And the height must be exactly the rows, not a row and a bit.";

    SUCCEED();
}

//-----------------------------------------------------------------------------
// getCellAdvance
//-----------------------------------------------------------------------------

TEST( GuiFrameStripLayoutTests, TheAdvanceIsACellAndItsGap )
{
    ASSERT_EQ( GuiEditFrameStripCtrl::getCellAdvance( sCell, sPad ), sAdvance )
        << "The one number the rest of the layout is built from.";
    ASSERT_EQ( GuiEditFrameStripCtrl::getCellAdvance( sCell, 0 ), sCell )
        << "No gap means cells touch.";

    SUCCEED();
}

#endif // TORQUE_SHIPPING
