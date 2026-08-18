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

#ifndef _SIMBASE_H_
#include "sim/simBase.h"
#endif

//-----------------------------------------------------------------------------
// What happens to a control when it changes parent.
//
// In the Gui Editor a control can change parent two ways: dragged across the
// canvas until the pointer is over a different container, or dragged in the
// Explorer tree from one branch to another. Either way the control is handed to
// GuiControl::addObject, which ends in onChildAdded, which calls parentResized
// with the new parent's inner extent for BOTH the old and the new value -- a
// zero delta, so that a centred or filled child settles into its new home at
// once and everything else is left alone.
//
// "Left alone" is the promise these tests hold it to. A move is a move: the
// control keeps the size it had, in every mode but the two that compute a size
// from the parent every layout.
//
// None of this needs a canvas. Nothing here is woken, nothing measures text, and
// no font is ever asked for, so there is no texture assert waiting -- see the
// note about that in guiScrollLayoutTests.cc. The controls are real ones with
// real parents because the thing under test IS the reparent, not arithmetic that
// could be lifted out of it. The one piece that could be lifted out -- the rescue
// at the bottom of this file -- was.
//-----------------------------------------------------------------------------

static StringTableEntry reparentField( const char* name )
{
    return StringTable->insert( name );
}

// Through the fields rather than resize(), because a control with no parent yet
// is exactly the case resize() has nothing to lay out against. The field setters
// also clear the sizing batteries, which is the state a freshly built control is
// supposed to be in.
static GuiControl* makeControl( const char* position, const char* extent )
{
    GuiControl* ctrl = new GuiControl();
    ctrl->registerObject();
    ctrl->setDataField( reparentField( "Position" ), NULL, position );
    ctrl->setDataField( reparentField( "Extent" ), NULL, extent );
    return ctrl;
}

static void setSizing( GuiControl* ctrl, const char* horiz, const char* vert )
{
    ctrl->setDataField( reparentField( "HorizSizing" ), NULL, horiz );
    ctrl->setDataField( reparentField( "VertSizing" ), NULL, vert );
}

// GuiDefaultProfile's border profile is all zeroes -- GuiBorderProfile's
// constructor sets margin, border and padding to 0 and GuiDefaultBorderProfile
// never changes them -- so a control wearing it has an inner rect the size of
// its bounds. Asserted rather than assumed: every expected number below is
// written as though inner and outer are the same, and if that ever stops being
// true this is the test that says so rather than six confusing failures.
TEST( GuiControlReparentTests, TheDefaultProfileCostsAChildNothing )
{
    GuiControl* parent = makeControl( "0 0", "800 600" );

    const RectI inner = parent->getInnerRect();
    ASSERT_EQ( inner.point.x, 0 );
    ASSERT_EQ( inner.point.y, 0 );
    ASSERT_EQ( inner.extent.x, 800 );
    ASSERT_EQ( inner.extent.y, 600 );

    parent->deleteObject();
}

//-----------------------------------------------------------------------------
// scale -- the mode that was wrong.
//
// A scaled control caches the proportion of its parent it occupies, so that a
// run of layout passes cannot round its edges away a pixel at a time
// (relPosBatteryH, behind the mUseRelPosH flag). resetStoredRelPos clears the
// cache, and the Position and Extent field setters call it, because writing a
// position is the moment the cached proportion stops describing the control.
//
// Changing parent is that moment too. Before the fix nothing called it there, so
// onChildAdded applied the OLD parent's proportion to the NEW parent's extent: a
// button 200 wide at x=100 in an 800-wide container came out 50 wide at x=25 in
// a 200-wide one. The drop then put the position back under the pointer, which
// hid half the damage -- the button landed where it was dropped, at a quarter of
// its size.
//-----------------------------------------------------------------------------

TEST( GuiControlReparentTests, ScaleKeepsItsExtentInASmallerParent )
{
    GuiControl* big = makeControl( "0 0", "800 600" );
    GuiControl* small = makeControl( "0 0", "200 150" );

    GuiControl* child = makeControl( "100 100", "200 40" );
    setSizing( child, "scale", "scale" );

    big->addObject( child );
    ASSERT_EQ( child->getExtent().x, 200 ) << "Arriving anywhere must not resize it.";
    ASSERT_EQ( child->getExtent().y, 40 );

    small->addObject( child );

    ASSERT_EQ( child->getExtent().x, 200 )
        << "A move is a move. Scale describes what a control does when its "
           "parent is RESIZED, not what it does when it is handed to a "
           "different one -- 0.125 to 0.375 of 200 is 50, which is the bug.";
    ASSERT_EQ( child->getExtent().y, 40 );
    ASSERT_EQ( child->getPosition().x, 100 )
        << "And the position is the caller's to set afterwards, not the "
           "layout's to guess at.";
    ASSERT_EQ( child->getPosition().y, 100 );

    small->deleteObject();
    big->deleteObject();
}

// The other direction, which fails differently: a stale proportion applied to a
// bigger parent inflates the control instead of shrinking it. Worth its own case
// because the fix could plausibly have been a clamp, and a clamp would pass the
// test above and fail this one.
TEST( GuiControlReparentTests, ScaleKeepsItsExtentInALargerParent )
{
    GuiControl* small = makeControl( "0 0", "200 150" );
    GuiControl* big = makeControl( "0 0", "800 600" );

    GuiControl* child = makeControl( "20 20", "100 30" );
    setSizing( child, "scale", "scale" );

    small->addObject( child );
    big->addObject( child );

    ASSERT_EQ( child->getExtent().x, 100 )
        << "0.1 to 0.6 of 800 is 400 -- four times the size it was dropped at.";
    ASSERT_EQ( child->getExtent().y, 30 );

    big->deleteObject();
    small->deleteObject();
}

// The feature the cache exists for, which the fix must not have switched off.
// Resetting the proportion on a move recharges it against the new parent; it
// does not stop it being used.
TEST( GuiControlReparentTests, ScaleStillScalesWhenItsOwnParentResizes )
{
    GuiControl* parent = makeControl( "0 0", "800 600" );

    GuiControl* child = makeControl( "100 100", "200 40" );
    setSizing( child, "scale", "scale" );
    parent->addObject( child );

    parent->resize( Point2I( 0, 0 ), Point2I( 400, 300 ) );

    ASSERT_EQ( child->getPosition().x, 50 ) << "Half the parent, half the offset.";
    ASSERT_EQ( child->getExtent().x, 100 ) << "And half the width.";
    ASSERT_EQ( child->getPosition().y, 50 );
    ASSERT_EQ( child->getExtent().y, 20 );

    parent->deleteObject();
}

// The whole point of the cache, and the reason the fix had to be a reset rather
// than a removal. Recomputing the proportion from integer bounds every pass
// loses a little each time; the cached one is exact, so a round trip comes back
// where it started.
TEST( GuiControlReparentTests, ScaleSurvivesARoundTripWithoutDrift )
{
    GuiControl* parent = makeControl( "0 0", "800 600" );

    GuiControl* child = makeControl( "101 101", "199 41" );
    setSizing( child, "scale", "scale" );
    parent->addObject( child );

    parent->resize( Point2I( 0, 0 ), Point2I( 333, 251 ) );
    parent->resize( Point2I( 0, 0 ), Point2I( 97, 63 ) );
    parent->resize( Point2I( 0, 0 ), Point2I( 800, 600 ) );

    ASSERT_EQ( child->getPosition().x, 101 ) << "Three resizes, no drift.";
    ASSERT_EQ( child->getExtent().x, 199 );
    ASSERT_EQ( child->getPosition().y, 101 );
    ASSERT_EQ( child->getExtent().y, 41 );

    parent->deleteObject();
}

// A scaled control that has been moved scales against the parent it is in now.
// This is the pair to the test above: the cache must be recharged by the move,
// not merely ignored during it.
TEST( GuiControlReparentTests, ScaleScalesAgainstTheNewParentAfterAMove )
{
    GuiControl* big = makeControl( "0 0", "800 600" );
    GuiControl* small = makeControl( "0 0", "200 150" );

    GuiControl* child = makeControl( "20 20", "100 30" );
    setSizing( child, "scale", "scale" );

    big->addObject( child );
    small->addObject( child );

    small->resize( Point2I( 0, 0 ), Point2I( 100, 75 ) );

    ASSERT_EQ( child->getPosition().x, 10 )
        << "Halving the parent it lives in now halves it. Against the 800-wide "
           "parent it came from, 20 of 800 would round to 0.";
    ASSERT_EQ( child->getExtent().x, 50 );
    ASSERT_EQ( child->getPosition().y, 10 );
    ASSERT_EQ( child->getExtent().y, 15 );

    small->deleteObject();
    big->deleteObject();
}

//-----------------------------------------------------------------------------
// The modes that were already right, locked down so that the fix above cannot
// quietly change them. Each one is a different branch of parentResized.
//-----------------------------------------------------------------------------

TEST( GuiControlReparentTests, AnchoredKeepsBothPositionAndExtent )
{
    GuiControl* big = makeControl( "0 0", "800 600" );
    GuiControl* small = makeControl( "0 0", "200 150" );

    GuiControl* child = makeControl( "100 100", "200 40" );
    setSizing( child, "anchorLeft", "anchorTop" );

    big->addObject( child );
    small->addObject( child );

    ASSERT_EQ( child->getPosition().x, 100 )
        << "A zero delta moves an anchored control not at all -- which is what "
           "lets it end up outside a smaller parent. See the rescue below.";
    ASSERT_EQ( child->getPosition().y, 100 );
    ASSERT_EQ( child->getExtent().x, 200 );
    ASSERT_EQ( child->getExtent().y, 40 );

    small->deleteObject();
    big->deleteObject();
}

// width and height pin both edges, so they respond to a delta by changing size.
// A move has no delta, so they behave like the anchors here.
TEST( GuiControlReparentTests, WidthAndHeightKeepBothPositionAndExtent )
{
    GuiControl* big = makeControl( "0 0", "800 600" );
    GuiControl* small = makeControl( "0 0", "200 150" );

    GuiControl* child = makeControl( "100 100", "200 40" );
    setSizing( child, "width", "height" );

    big->addObject( child );
    small->addObject( child );

    ASSERT_EQ( child->getPosition().x, 100 );
    ASSERT_EQ( child->getPosition().y, 100 );
    ASSERT_EQ( child->getExtent().x, 200 );
    ASSERT_EQ( child->getExtent().y, 40 );

    small->deleteObject();
    big->deleteObject();
}

TEST( GuiControlReparentTests, CenterRecentersInTheNewParent )
{
    GuiControl* big = makeControl( "0 0", "800 600" );
    GuiControl* small = makeControl( "0 0", "200 150" );

    GuiControl* child = makeControl( "0 0", "100 40" );
    setSizing( child, "center", "center" );

    big->addObject( child );
    ASSERT_EQ( child->getPosition().x, 350 ) << "(800 - 100) / 2";
    ASSERT_EQ( child->getPosition().y, 280 ) << "(600 - 40) / 2";

    small->addObject( child );
    ASSERT_EQ( child->getPosition().x, 50 ) << "(200 - 100) / 2";
    ASSERT_EQ( child->getPosition().y, 55 ) << "(150 - 40) / 2";
    ASSERT_EQ( child->getExtent().x, 100 ) << "Centering moves a control; it does not resize one.";
    ASSERT_EQ( child->getExtent().y, 40 );

    small->deleteObject();
    big->deleteObject();
}

TEST( GuiControlReparentTests, FillFillsTheNewParent )
{
    GuiControl* big = makeControl( "0 0", "800 600" );
    GuiControl* small = makeControl( "0 0", "200 150" );

    GuiControl* child = makeControl( "10 10", "100 40" );
    setSizing( child, "fill", "fill" );

    big->addObject( child );
    ASSERT_EQ( child->getPosition().x, 0 );
    ASSERT_EQ( child->getPosition().y, 0 );
    ASSERT_EQ( child->getExtent().x, 800 );
    ASSERT_EQ( child->getExtent().y, 600 );

    small->addObject( child );
    ASSERT_EQ( child->getPosition().x, 0 );
    ASSERT_EQ( child->getPosition().y, 0 );
    ASSERT_EQ( child->getExtent().x, 200 );
    ASSERT_EQ( child->getExtent().y, 150 );

    small->deleteObject();
    big->deleteObject();
}

// The axes are independent, and mixing them is the case a fix that resets "the
// battery" as one thing would get wrong.
TEST( GuiControlReparentTests, TheTwoAxesAreIndependent )
{
    GuiControl* big = makeControl( "0 0", "800 600" );
    GuiControl* small = makeControl( "0 0", "200 150" );

    GuiControl* child = makeControl( "100 100", "200 40" );
    setSizing( child, "scale", "center" );

    big->addObject( child );
    small->addObject( child );

    ASSERT_EQ( child->getExtent().x, 200 ) << "Scale across: the size is kept.";
    ASSERT_EQ( child->getPosition().x, 100 );
    ASSERT_EQ( child->getPosition().y, 55 ) << "Center down: (150 - 40) / 2.";
    ASSERT_EQ( child->getExtent().y, 40 );

    small->deleteObject();
    big->deleteObject();
}

//-----------------------------------------------------------------------------
// The minExtent battery, which is the same stale-state bug wearing different
// clothes. mStoredExtent records extent a control gave up to its minExtent and
// is owed back when there is room again. A debt run up under one parent means
// nothing under the next, so a move clears it.
//-----------------------------------------------------------------------------

TEST( GuiControlReparentTests, AMinExtentDebtDoesNotFollowTheControl )
{
    GuiControl* squeezer = makeControl( "0 0", "800 600" );

    GuiControl* child = makeControl( "0 0", "400 300" );
    child->setDataField( reparentField( "MinExtent" ), NULL, "100 80" );
    setSizing( child, "width", "height" );
    squeezer->addObject( child );

    // Squeeze it past its minimum: it stops at 100 wide and remembers it is owed
    // the rest.
    squeezer->resize( Point2I( 0, 0 ), Point2I( 400, 380 ) );
    ASSERT_EQ( child->getExtent().x, 100 ) << "Clamped at the minimum.";

    GuiControl* roomy = makeControl( "0 0", "800 600" );
    roomy->addObject( child );
    ASSERT_EQ( child->getExtent().x, 100 ) << "The move itself changes nothing.";

    // Growing the new parent by 50 should give the control 50, not pay back a
    // debt it ran up somewhere else.
    roomy->resize( Point2I( 0, 0 ), Point2I( 850, 600 ) );
    ASSERT_EQ( child->getExtent().x, 150 )
        << "A control that arrives at 100 wide is 100 wide. Carrying the debt "
           "over would swallow the 50 and leave it at 100.";

    roomy->deleteObject();
    squeezer->deleteObject();
}

//-----------------------------------------------------------------------------
// rescuedPosition -- where a control goes when a move has stranded it.
//
// A tree drag has no pointer, so nothing supplies a position and the control
// keeps the local one it held in its old parent. Dropped into something smaller
// that can put it entirely outside: not clipped, not partly visible, gone.
//
// Per axis, because a placement that is still valid should be kept -- a button
// that was 20 pixels down and 400 across is still 20 pixels down. Only when the
// control is ENTIRELY outside, because a control the user can see is a control
// the user can drag, and moving one that merely overhangs the edge would be
// undoing a placement rather than rescuing it.
//
// Static, so the arithmetic can be read on its own: the same shape as
// GuiScrollCtrl::subtractScrollBars and GuiTreeViewCtrl::resolveIndent.
//-----------------------------------------------------------------------------

static const Point2I RescueInner( 100, 300 );
static const Point2I RescueSize( 80, 24 );

TEST( GuiControlReparentTests, RescueLeavesAControlThatFitsAlone )
{
    const Point2I at = GuiControl::rescuedPosition( Point2I( 10, 40 ), RescueSize, RescueInner );

    ASSERT_EQ( at.x, 10 );
    ASSERT_EQ( at.y, 40 );
}

TEST( GuiControlReparentTests, RescueZerosTheAxisThatIsPastTheRightEdge )
{
    const Point2I at = GuiControl::rescuedPosition( Point2I( 400, 20 ), RescueSize, RescueInner );

    ASSERT_EQ( at.x, 0 );
    ASSERT_EQ( at.y, 20 ) << "20 down is still 20 down.";
}

TEST( GuiControlReparentTests, RescueZerosTheAxisThatIsPastTheBottomEdge )
{
    const Point2I at = GuiControl::rescuedPosition( Point2I( 20, 500 ), RescueSize, RescueInner );

    ASSERT_EQ( at.x, 20 );
    ASSERT_EQ( at.y, 0 );
}

TEST( GuiControlReparentTests, RescueZerosBothWhenBothAreOut )
{
    const Point2I at = GuiControl::rescuedPosition( Point2I( 400, 500 ), RescueSize, RescueInner );

    ASSERT_EQ( at.x, 0 );
    ASSERT_EQ( at.y, 0 );
}

// Off the left and off the top are just as invisible, and cost nothing to catch.
TEST( GuiControlReparentTests, RescueCatchesOffTheLeftAndOffTheTop )
{
    const Point2I left = GuiControl::rescuedPosition( Point2I( -90, 20 ), RescueSize, RescueInner );
    ASSERT_EQ( left.x, 0 ) << "-90 + 80 is -10: the right edge is off the left side.";
    ASSERT_EQ( left.y, 20 );

    const Point2I above = GuiControl::rescuedPosition( Point2I( 20, -30 ), RescueSize, RescueInner );
    ASSERT_EQ( above.x, 20 );
    ASSERT_EQ( above.y, 0 ) << "-30 + 24 is -6.";
}

TEST( GuiControlReparentTests, RescueLeavesAControlThatIsOnlyPartlyOutside )
{
    const Point2I over = GuiControl::rescuedPosition( Point2I( 90, 20 ), RescueSize, RescueInner );
    ASSERT_EQ( over.x, 90 ) << "10 pixels of it are visible, so it can be dragged.";
    ASSERT_EQ( over.y, 20 );

    const Point2I under = GuiControl::rescuedPosition( Point2I( -10, 20 ), RescueSize, RescueInner );
    ASSERT_EQ( under.x, -10 ) << "And 70 pixels here.";
    ASSERT_EQ( under.y, 20 );
}

// The boundaries, where "entirely outside" is decided. A control whose left edge
// sits exactly on the parent's right edge shows nothing; one pixel back shows one
// pixel.
TEST( GuiControlReparentTests, RescueIsExactAtTheEdges )
{
    ASSERT_EQ( GuiControl::rescuedPosition( Point2I( 100, 0 ), RescueSize, RescueInner ).x, 0 )
        << "Left edge on the parent's right edge: nothing is visible.";
    ASSERT_EQ( GuiControl::rescuedPosition( Point2I( 99, 0 ), RescueSize, RescueInner ).x, 99 )
        << "One pixel visible is visible.";
    ASSERT_EQ( GuiControl::rescuedPosition( Point2I( -80, 0 ), RescueSize, RescueInner ).x, 0 )
        << "Right edge on the parent's left edge: nothing is visible.";
    ASSERT_EQ( GuiControl::rescuedPosition( Point2I( -79, 0 ), RescueSize, RescueInner ).x, -79 );
}

// A container with no room at all cannot show anything wherever the control is
// put, and 0 is the least surprising answer.
TEST( GuiControlReparentTests, RescueHandlesAParentWithNoRoom )
{
    const Point2I at = GuiControl::rescuedPosition( Point2I( 40, 40 ), RescueSize, Point2I( 0, 0 ) );

    ASSERT_EQ( at.x, 0 );
    ASSERT_EQ( at.y, 0 );
}

//-----------------------------------------------------------------------------
// pullIntoView -- the same thing against the parent a control actually has.
//-----------------------------------------------------------------------------

TEST( GuiControlReparentTests, PullIntoViewRescuesAStrandedControl )
{
    GuiControl* big = makeControl( "0 0", "800 600" );
    GuiControl* small = makeControl( "0 0", "100 300" );

    GuiControl* child = makeControl( "400 20", "80 24" );
    setSizing( child, "anchorLeft", "anchorTop" );

    big->addObject( child );
    small->addObject( child );
    ASSERT_EQ( child->getPosition().x, 400 ) << "Stranded by the move itself.";

    ASSERT_TRUE( child->pullIntoView() ) << "It moved, so it says so.";
    ASSERT_EQ( child->getPosition().x, 0 );
    ASSERT_EQ( child->getPosition().y, 20 );
    ASSERT_EQ( child->getExtent().x, 80 ) << "A rescue moves a control; it does not resize one.";
    ASSERT_EQ( child->getExtent().y, 24 );

    ASSERT_FALSE( child->pullIntoView() ) << "And a second call has nothing to do.";

    small->deleteObject();
    big->deleteObject();
}

TEST( GuiControlReparentTests, PullIntoViewLeavesAVisibleControlAlone )
{
    GuiControl* parent = makeControl( "0 0", "800 600" );

    GuiControl* child = makeControl( "100 100", "200 40" );
    parent->addObject( child );

    ASSERT_FALSE( child->pullIntoView() );
    ASSERT_EQ( child->getPosition().x, 100 );
    ASSERT_EQ( child->getPosition().y, 100 );

    parent->deleteObject();
}

// A control with no parent has no view to be pulled into, and asking must not
// crash -- the Explorer tree asks about a whole selection without checking each
// one, and the root of the document is in that selection.
TEST( GuiControlReparentTests, PullIntoViewIsSafeWithNoParent )
{
    GuiControl* orphan = makeControl( "400 400", "80 24" );

    ASSERT_FALSE( orphan->pullIntoView() );
    ASSERT_EQ( orphan->getPosition().x, 400 ) << "Left exactly as it was.";

    orphan->deleteObject();
}

// A rescue has to leave the sizing cache honest, or the next time the parent
// resizes the control jumps back to where it was rescued from.
TEST( GuiControlReparentTests, PullIntoViewLeavesScaleMeasuringFromWhereItLanded )
{
    GuiControl* big = makeControl( "0 0", "800 600" );
    GuiControl* small = makeControl( "0 0", "100 300" );

    GuiControl* child = makeControl( "400 20", "80 24" );
    setSizing( child, "scale", "scale" );

    big->addObject( child );
    small->addObject( child );
    ASSERT_TRUE( child->pullIntoView() );
    ASSERT_EQ( child->getPosition().x, 0 );

    small->resize( Point2I( 0, 0 ), Point2I( 200, 300 ) );

    ASSERT_EQ( child->getPosition().x, 0 )
        << "Doubling the parent doubles an offset of 0, which is 0.";
    ASSERT_EQ( child->getExtent().x, 160 ) << "And doubles the width.";

    small->deleteObject();
    big->deleteObject();
}

#endif // TORQUE_SHIPPING
