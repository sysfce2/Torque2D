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

#ifndef _GUICONTROL_H_
#include "gui/guiControl.h"
#endif

#ifndef _GUI_TREEVIEWCTRL_H
#include "gui/guiTreeViewCtrl.h"
#endif

#ifndef _GUI_EDITOR_EXPLORERTREE_H_
#include "gui/editor/guiEditorExplorerTree.h"
#endif

//-----------------------------------------------------------------------------
// How a tree row spends its width, left to right.
//
// A row is a budget: two gutter columns, an indent per level, a triangle, an
// icon, and whatever is left is the text. Every one of those is arithmetic, and
// all of it used to be inline in onRenderItem where nothing could reach it.
//
// It cannot be tested there. Adding a row to a tree calls updateSize, which asks
// the profile for a font, which loads one, which registers a texture -- and this
// suite runs with no canvas and so no GL context to make one in. TextureManager
// asserts, and in a debug build an assert is a modal box, so the whole run hangs
// rather than fails. So the arithmetic is pulled out into statics that take
// everything they use, exactly as GuiScrollCtrl's bar arithmetic was, and the
// statics are what these tests call. They construct nothing.
//-----------------------------------------------------------------------------

//-----------------------------------------------------------------------------
// resolveIndent -- how far one level of depth steps the row in.
//
// The step was always the row's inner height, hard-coded at three sites. It is
// now a field, and the danger in that is the default: mIndentSize sat unread for
// years holding 10, so wiring it up without resetting it would have silently
// re-indented every tree in the engine. Zero means "as before".
//-----------------------------------------------------------------------------

TEST( GuiTreeRowLayoutTests, ZeroIndentMeansOneRowHeight )
{
    ASSERT_EQ( GuiTreeViewCtrl::resolveIndent( 0, 22 ), 22 )
        << "Zero is the default, and the default must be the step the tree always used.";

    SUCCEED();
}

TEST( GuiTreeRowLayoutTests, APositiveIndentWins )
{
    ASSERT_EQ( GuiTreeViewCtrl::resolveIndent( 12, 22 ), 12 )
        << "A tree that asks for a narrower step gets it, whatever the row height is.";

    SUCCEED();
}

TEST( GuiTreeRowLayoutTests, ANegativeIndentFallsBackToTheRowHeight )
{
    ASSERT_EQ( GuiTreeViewCtrl::resolveIndent( -6, 22 ), 22 )
        << "Only a positive value is an instruction; anything else means 'as before'.";

    SUCCEED();
}

TEST( GuiTreeRowLayoutTests, IndentIsNeverNegative )
{
    ASSERT_EQ( GuiTreeViewCtrl::resolveIndent( 0, 0 ), 0 )
        << "A row with no inside indents by nothing.";
    ASSERT_EQ( GuiTreeViewCtrl::resolveIndent( 0, -4 ), 0 )
        << "A negative step would walk the tree backwards, one level at a time.";

    SUCCEED();
}

//-----------------------------------------------------------------------------
// focusLineOffset -- the rule that hangs from a container's triangle.
//
// It marks the branch the editor will drop into, and it runs down from that
// container's triangle, so it has to line up with the triangle's point. It did
// for free while the indent step and the row height were the same number: the
// line was centred in the indent slot and the triangle was drawn in a square of
// the row height, and nobody had to know which one they were centring on.
//
// Making IndentSize settable broke that silently. The line moved four pixels
// left of the triangle it belonged to and everything still "worked". Hence a
// static, and hence this: the invariant is that the line covers the centre of
// the triangle's square, whatever the indent happens to be.
//-----------------------------------------------------------------------------

// The triangle is drawn in a square of the row's inner height, so its point is
// at half that. The line must cover it.
static bool lineCoversTriangleTip( const S32 rowInnerHeight )
{
    const S32 start = GuiTreeViewCtrl::focusLineOffset( rowInnerHeight );
    const S32 end = start + GuiTreeViewCtrl::smFocusLineWidth;
    const S32 tip = rowInnerHeight / 2;
    return tip >= start && tip < end;
}

TEST( GuiTreeRowLayoutTests, TheFocusLineHangsFromTheTrianglesPoint )
{
    for( S32 rowInnerHeight = 4; rowInnerHeight <= 64; rowInnerHeight++ )
    {
        ASSERT_TRUE( lineCoversTriangleTip( rowInnerHeight ) )
            << "row height " << rowInnerHeight << ": the line missed the triangle it hangs from.";
    }

    SUCCEED();
}

TEST( GuiTreeRowLayoutTests, TheFocusLineDoesNotDependOnTheIndent )
{
    // The point of the static. focusLineOffset takes the row height and nothing
    // else, so no value of IndentSize can move the line off its triangle -- which
    // is precisely what happened when the offset was computed from the indent.
    ASSERT_EQ( GuiTreeViewCtrl::focusLineOffset( 20 ), 9 );
    ASSERT_EQ( GuiTreeViewCtrl::resolveIndent( 12, 20 ), 12 )
        << "A narrow indent is still honoured...";
    ASSERT_EQ( GuiTreeViewCtrl::focusLineOffset( 20 ), 9 )
        << "...and the line does not follow it.";

    SUCCEED();
}

TEST( GuiTreeRowLayoutTests, TheFocusLineNeverStartsLeftOfItsSlot )
{
    ASSERT_EQ( GuiTreeViewCtrl::focusLineOffset( 0 ), 0 );
    ASSERT_EQ( GuiTreeViewCtrl::focusLineOffset( 1 ), 0 )
        << "A row too short to centre a 2px rule in must not push it into the slot before.";
    ASSERT_EQ( GuiTreeViewCtrl::focusLineOffset( -8 ), 0 );

    SUCCEED();
}

//-----------------------------------------------------------------------------
// iconSlot -- where the row's picture goes, and what it costs.
//
// Two promises: it never enlarges the art, and when it cannot fit it consumes
// nothing at all. The second is what stops a cramped tree drawing its text on
// top of its icons -- degrading to a plain row is legible, overlapping is not.
//-----------------------------------------------------------------------------

TEST( GuiTreeRowLayoutTests, TheIconCentersInATallerRow )
{
    RectI dst;
    S32 advance = 0;
    const bool fits = GuiTreeViewCtrl::iconSlot( RectI( 100, 50, 200, 22 ), 16, dst, advance );

    ASSERT_TRUE( fits );
    ASSERT_EQ( dst.point.x, 100 ) << "It draws at the left edge of what is left.";
    ASSERT_EQ( dst.point.y, 53 ) << "Six spare pixels, three above and three below.";
    ASSERT_EQ( dst.extent.x, 16 );
    ASSERT_EQ( dst.extent.y, 16 ) << "A square: the art is square and must not be stretched.";
    ASSERT_EQ( advance, 16 + GuiTreeViewCtrl::smIconGap ) << "The icon, plus one breath before the text.";

    SUCCEED();
}

TEST( GuiTreeRowLayoutTests, TheIconNeverEnlarges )
{
    RectI dst;
    S32 advance = 0;
    const bool fits = GuiTreeViewCtrl::iconSlot( RectI( 0, 0, 200, 12 ), 16, dst, advance );

    ASSERT_TRUE( fits );
    ASSERT_EQ( dst.extent.y, 12 ) << "A row shorter than the art shrinks the art, rather than blowing it up.";
    ASSERT_EQ( dst.extent.x, 12 ) << "Still square.";
    ASSERT_EQ( dst.point.y, 0 ) << "Nothing spare, so nothing to center.";
    ASSERT_EQ( advance, 12 + GuiTreeViewCtrl::smIconGap );

    SUCCEED();
}

TEST( GuiTreeRowLayoutTests, ANarrowRowGetsNoSlotAndPaysNothing )
{
    RectI dst;
    S32 advance = 0;
    const bool fits = GuiTreeViewCtrl::iconSlot( RectI( 0, 0, 10, 22 ), 16, dst, advance );

    ASSERT_FALSE( fits ) << "There is no room for the icon and a space after it.";
    ASSERT_EQ( advance, 0 ) << "Consuming width it did not draw in would put the text under nothing.";

    SUCCEED();
}

TEST( GuiTreeRowLayoutTests, TheSlotFitsInExactlyItsOwnWidth )
{
    RectI dst;
    S32 advance = 0;

    ASSERT_TRUE( GuiTreeViewCtrl::iconSlot( RectI( 0, 0, 16 + GuiTreeViewCtrl::smIconGap, 22 ), 16, dst, advance ) )
        << "Exactly enough is enough.";

    ASSERT_FALSE( GuiTreeViewCtrl::iconSlot( RectI( 0, 0, 15 + GuiTreeViewCtrl::smIconGap, 22 ), 16, dst, advance ) )
        << "One pixel short is short. The gap is part of the cost, not a nicety to drop.";

    SUCCEED();
}

TEST( GuiTreeRowLayoutTests, NoIconSizeIsNoSlot )
{
    RectI dst;
    S32 advance = 0;

    ASSERT_FALSE( GuiTreeViewCtrl::iconSlot( RectI( 0, 0, 200, 22 ), 0, dst, advance ) )
        << "A tree with no sheet set asks for nothing and must be charged nothing.";
    ASSERT_EQ( advance, 0 );

    ASSERT_FALSE( GuiTreeViewCtrl::iconSlot( RectI( 0, 0, 200, 0 ), 16, dst, advance ) )
        << "A row with no height has nowhere to put it.";
    ASSERT_EQ( advance, 0 );

    SUCCEED();
}

//-----------------------------------------------------------------------------
// The Explorer's two gutter columns.
//
// The eye and the padlock are a fixed rail at the row's left edge, and the only
// thing keeping the picture and the click in agreement is that both ask the same
// function the same question. Nothing on screen would report them disagreeing:
// the boxes would draw where they always did and the clicks would land a few
// pixels off, toggling the wrong control or nothing at all.
//
// So the geometry is checked here, from both sides of every boundary. An
// off-by-one in columnAt reads as "the eye is fussy about where you click",
// which is the kind of thing that gets lived with rather than reported.
//-----------------------------------------------------------------------------

// Where the rail starts. Nonzero on purpose: with the editor's treeViewProfile
// the inset is 0, so testing only at 0 would pass on arithmetic that had dropped
// the left edge entirely.
static const S32 GutterLeft = 7;

TEST( GuiTreeRowLayoutTests, TheEyeColumnIsLeftmost )
{
    RectI eye, lock;
    GuiEditorExplorerTree::getGutterCells( GutterLeft, 40, 22, eye, lock );

    ASSERT_EQ( eye.point.x, GutterLeft )
        << "Every layers panel anyone has used puts visibility first. This is that order.";
    ASSERT_LT( eye.point.x, lock.point.x );

    SUCCEED();
}

TEST( GuiTreeRowLayoutTests, TheTwoColumnsAbutAndSpanTheRow )
{
    RectI eye, lock;
    GuiEditorExplorerTree::getGutterCells( GutterLeft, 40, 22, eye, lock );

    ASSERT_EQ( eye.point.x + eye.extent.x, lock.point.x ) << "No seam between them.";
    ASSERT_EQ( eye.extent.x, GuiEditorExplorerTree::smColumnWidth );
    ASSERT_EQ( lock.extent.x, GuiEditorExplorerTree::smColumnWidth );
    ASSERT_EQ( eye.extent.x + lock.extent.x, GuiEditorExplorerTree::getGutterWidth() )
        << "What the renderer carves off the row must be what the two cells actually occupy.";

    ASSERT_EQ( eye.point.y, 40 );
    ASSERT_EQ( eye.extent.y, 22 ) << "The full row height, so the dividers read as continuous rules.";
    ASSERT_EQ( lock.point.y, 40 );
    ASSERT_EQ( lock.extent.y, 22 );

    SUCCEED();
}

TEST( GuiTreeRowLayoutTests, EveryColumnBoundaryIsWhereItLooks )
{
    const S32 w = GuiEditorExplorerTree::smColumnWidth;

    ASSERT_EQ( GuiEditorExplorerTree::columnAt( GutterLeft - 1, GutterLeft ),
               GuiEditorExplorerTree::GutterNone ) << "One pixel left of the rail is not the rail.";
    ASSERT_EQ( GuiEditorExplorerTree::columnAt( GutterLeft, GutterLeft ),
               GuiEditorExplorerTree::GutterHidden ) << "Its first pixel is the eye.";
    ASSERT_EQ( GuiEditorExplorerTree::columnAt( GutterLeft + w - 1, GutterLeft ),
               GuiEditorExplorerTree::GutterHidden )
        << "Including its divider: the whole cell is clickable, not just the box.";
    ASSERT_EQ( GuiEditorExplorerTree::columnAt( GutterLeft + w, GutterLeft ),
               GuiEditorExplorerTree::GutterLocked ) << "And the next pixel is the padlock.";
    ASSERT_EQ( GuiEditorExplorerTree::columnAt( GutterLeft + ( 2 * w ) - 1, GutterLeft ),
               GuiEditorExplorerTree::GutterLocked );
    ASSERT_EQ( GuiEditorExplorerTree::columnAt( GutterLeft + ( 2 * w ), GutterLeft ),
               GuiEditorExplorerTree::GutterNone )
        << "Past the rail is the tree, where a click selects rather than toggles.";

    SUCCEED();
}

TEST( GuiTreeRowLayoutTests, AFarawayPointIsInNoColumn )
{
    ASSERT_EQ( GuiEditorExplorerTree::columnAt( -400, GutterLeft ),
               GuiEditorExplorerTree::GutterNone );
    ASSERT_EQ( GuiEditorExplorerTree::columnAt( 4000, GutterLeft ),
               GuiEditorExplorerTree::GutterNone );

    SUCCEED();
}

TEST( GuiTreeRowLayoutTests, TheBoxSitsInsideItsCellClearOfTheDivider )
{
    RectI eye, lock;
    GuiEditorExplorerTree::getGutterCells( GutterLeft, 40, 22, eye, lock );
    const RectI box = GuiEditorExplorerTree::getBoxRect( eye );

    ASSERT_EQ( box.extent.x, GuiEditorExplorerTree::smBoxSize );
    ASSERT_EQ( box.extent.y, GuiEditorExplorerTree::smBoxSize ) << "Square: the art is square.";
    ASSERT_GE( box.point.x, eye.point.x ) << "Inside its own cell.";
    ASSERT_LT( box.point.x + box.extent.x, eye.point.x + eye.extent.x )
        << "Clear of the divider, which owns the cell's last pixel.";
    ASSERT_EQ( box.point.y, 40 + 3 ) << "Six spare pixels of row, three above and three below.";

    SUCCEED();
}

TEST( GuiTreeRowLayoutTests, TheBoxShrinksIntoAShortRowRatherThanOverflowing )
{
    RectI eye, lock;
    GuiEditorExplorerTree::getGutterCells( GutterLeft, 0, 10, eye, lock );
    const RectI box = GuiEditorExplorerTree::getBoxRect( eye );

    ASSERT_EQ( box.extent.y, 10 ) << "A row too short for the art gets the art shrunk to it.";
    ASSERT_EQ( box.extent.x, 10 ) << "Still square, so the icon is not distorted.";
    ASSERT_GE( box.point.y, 0 );
    ASSERT_LE( box.point.y + box.extent.y, 10 ) << "And it stays within the row it is in.";

    SUCCEED();
}

#endif // TORQUE_SHIPPING
