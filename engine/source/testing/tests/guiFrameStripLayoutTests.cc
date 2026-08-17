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

#ifndef _GUI_EDIT_FRAME_TIMELINE_CTRL_H_
#include "gui/editor/guiEditFrameTimelineCtrl.h"
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

//-----------------------------------------------------------------------------
// insertionAt -- where a dragged frame would land.
//
// A different question from cellAt, with a different shape of answer. cellAt
// asks "which cell is this", and the gaps belong to nobody; this asks "where
// would it go", and every point has an answer including the gaps, the ends, and
// an empty timeline. Getting the two confused is how a drop lands one slot off.
//
// Cells are 20 with a 4 gap, so with three of them the centres are at 10, 34
// and 58.
//-----------------------------------------------------------------------------

TEST( GuiFrameStripLayoutTests, LeftOfEverythingInsertsAtTheStart )
{
    ASSERT_EQ( GuiEditFrameTimelineCtrl::insertionAt( Point2I( 0, 5 ), 3, sCell, sPad ), 0 )
        << "The very first pixel is before the first frame.";
    ASSERT_EQ( GuiEditFrameTimelineCtrl::insertionAt( Point2I( -20, 5 ), 3, sCell, sPad ), 0 )
        << "So is anywhere left of the strip, which a drag arriving from the palette is.";

    SUCCEED();
}

TEST( GuiFrameStripLayoutTests, RightOfEverythingAppends )
{
    ASSERT_EQ( GuiEditFrameTimelineCtrl::insertionAt( Point2I( 500, 5 ), 3, sCell, sPad ), 3 )
        << "Past the last frame is the end of the list, not off it.";

    SUCCEED();
}

TEST( GuiFrameStripLayoutTests, TheBoundaryIsTheCellCentreNotItsEdge )
{
    // This is the whole design of insertionAt. Halfway across a cell is where a
    // person expects "before this one" to become "after it"; using the cell's
    // edge instead makes the caret lag the pointer by half a frame.
    ASSERT_EQ( GuiEditFrameTimelineCtrl::insertionAt( Point2I( 9, 5 ), 3, sCell, sPad ), 0 )
        << "One pixel left of the first cell's centre is still before it.";
    ASSERT_EQ( GuiEditFrameTimelineCtrl::insertionAt( Point2I( 10, 5 ), 3, sCell, sPad ), 1 )
        << "Its centre is where it flips to after.";

    ASSERT_EQ( GuiEditFrameTimelineCtrl::insertionAt( Point2I( 33, 5 ), 3, sCell, sPad ), 1 )
        << "The same one cell along.";
    ASSERT_EQ( GuiEditFrameTimelineCtrl::insertionAt( Point2I( 34, 5 ), 3, sCell, sPad ), 2 )
        << "And it flips at that cell's centre too.";

    SUCCEED();
}

TEST( GuiFrameStripLayoutTests, TheGapStillHasAnAnswer )
{
    // Unlike cellAt, which returns -1 here. A drag hovering over the gap between
    // two frames is asking the clearest question there is.
    ASSERT_EQ( GuiEditFrameTimelineCtrl::insertionAt( Point2I( 21, 5 ), 3, sCell, sPad ), 1 )
        << "Between cell 0 and cell 1 is slot 1, not nothing.";

    SUCCEED();
}

TEST( GuiFrameStripLayoutTests, AnEmptyTimelineTakesTheFirstFrame )
{
    ASSERT_EQ( GuiEditFrameTimelineCtrl::insertionAt( Point2I( 40, 5 ), 0, sCell, sPad ), 0 )
        << "The first frame dragged into an empty animation goes at slot 0, wherever it was dropped.";

    SUCCEED();
}

//-----------------------------------------------------------------------------
// getCaretRect -- and it must agree with insertionAt, because the caret is the
// promise the drop then has to keep.
//-----------------------------------------------------------------------------

TEST( GuiFrameStripLayoutTests, TheCaretSitsBetweenTwoCellsAndTouchesNeither )
{
    const RectI caret = GuiEditFrameTimelineCtrl::getCaretRect( 1, 3, sCell, sPad, sCell );

    const RectI before = GuiEditFrameStripCtrl::getCellRect( 0, 3, sCell, sPad );
    const RectI after = GuiEditFrameStripCtrl::getCellRect( 1, 3, sCell, sPad );

    ASSERT_GE( caret.point.x, before.point.x + before.extent.x )
        << "The caret starts at or after the left neighbour's right edge.";
    ASSERT_LE( caret.point.x + caret.extent.x, after.point.x )
        << "And ends at or before the right neighbour's left edge.";

    SUCCEED();
}

TEST( GuiFrameStripLayoutTests, TheCaretForSlotZeroIsAtTheStart )
{
    const RectI caret = GuiEditFrameTimelineCtrl::getCaretRect( 0, 3, sCell, sPad, sCell );

    ASSERT_EQ( caret.point.x, 0 )
        << "There is no gap before the first cell to sit in, so it goes at the edge.";

    SUCCEED();
}

TEST( GuiFrameStripLayoutTests, TheAppendCaretStaysInsideTheContent )
{
    // There is no trailing gap after the last cell -- getContentExtent leaves
    // none, deliberately -- so the append caret overlaps that cell's last pixels
    // rather than sitting past the content, where it would simply be clipped away
    // and the user would see no caret at all.
    const S32 count = 3;
    const Point2I content = GuiEditFrameStripCtrl::getContentExtent( count, count, sCell, sPad );
    const RectI caret = GuiEditFrameTimelineCtrl::getCaretRect( count, count, sCell, sPad, sCell );
    const RectI last = GuiEditFrameStripCtrl::getCellRect( count - 1, count, sCell, sPad );

    ASSERT_EQ( caret.point.x + caret.extent.x, content.x )
        << "The append caret ends exactly at the right edge of the content.";
    ASSERT_GT( caret.point.x, last.point.x )
        << "And is at the far end of the last cell, so it reads as after it.";

    SUCCEED();
}

TEST( GuiFrameStripLayoutTests, TheCaretIsVisibleOnAnEmptyTimeline )
{
    // An empty animation has no cells and therefore no content width, and the
    // caret must still be somewhere drawable -- this is the first thing a user
    // building an animation from nothing will see.
    const RectI caret = GuiEditFrameTimelineCtrl::getCaretRect( 0, 0, sCell, sPad, sCell );

    ASSERT_EQ( caret.point.x, 0 ) << "At the start.";
    ASSERT_GT( caret.extent.x, 0 ) << "With a width.";
    ASSERT_EQ( caret.extent.y, sCell ) << "And the height it was asked for.";

    SUCCEED();
}

TEST( GuiFrameStripLayoutTests, EveryInsertionPointHasItsOwnCaret )
{
    // Two slots must never share a caret position: if they did, the user could
    // not tell from the picture which of two places a drop was about to go.
    const S32 count = 4;
    S32 previousX = -1;

    for ( S32 i = 0; i <= count; ++i )
    {
        const RectI caret = GuiEditFrameTimelineCtrl::getCaretRect( i, count, sCell, sPad, sCell );

        ASSERT_GT( caret.point.x, previousX )
            << "Each insertion point is strictly right of the one before it.";
        previousX = caret.point.x;
    }

    SUCCEED();
}

#endif // TORQUE_SHIPPING
