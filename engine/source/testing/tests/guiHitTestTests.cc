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

#ifndef _GUIEDITCTRL_H_
#include "gui/editor/guiEditCtrl.h"
#endif

#ifndef _SIMBASE_H_
#include "sim/simBase.h"
#endif

//-----------------------------------------------------------------------------
// What the mouse can hit, and what the Gui Editor's eye takes away.
//
// Clicking the eye in the Explorer sets SimObject's Hidden flag, and
// renderChildControls stops drawing that control and everything under it. The
// hit test has to agree, or the eye takes a control out of sight while leaving
// it in the way of every click aimed at what is behind it -- which is the one
// thing hiding is for. Both paths now ask isHiddenInEditor, and these are the
// tests that hold them to the same answer.
//
// The rule is scoped to the editor, twice over: isEditMode walks up the parent
// chain looking for the edit root, and gives up at once if smDesignTime is
// false. The last two tests are the ones that pin that down -- a shipped game
// must not pay for this and must not obey it.
//
// None of it needs a canvas. findHitControl reads mBounds and mRenderInsetLT and
// nothing else; nothing here is woken, nothing measures text, and no font is
// ever asked for, so there is no texture assert waiting (see the note in
// guiScrollLayoutTests.cc). The GuiEditCtrl is real because isEditMode reads the
// real static and asks it for its real root -- stubbing that would test the stub.
//-----------------------------------------------------------------------------

static StringTableEntry hitTestField( const char* name )
{
    return StringTable->insert( name );
}

// Through the fields rather than resize(), for the reason given in
// guiControlReparentTests.cc: a control with no parent yet is exactly the case
// resize() has nothing to lay out against.
static GuiControl* makeHitControl( const char* position, const char* extent )
{
    GuiControl* ctrl = new GuiControl();
    ctrl->registerObject();
    ctrl->setDataField( hitTestField( "Position" ), NULL, position );
    ctrl->setDataField( hitTestField( "Extent" ), NULL, extent );
    return ctrl;
}

// One back panel with a smaller front panel sitting on top of it, and a control
// deeper still inside the front one. Later children are drawn last and hit
// first, so "front" really is in front.
//
//   root   0,0    800x600
//    back  100,100  400x300     -> 100..499, 100..399
//    front 200,150  200x100     -> 200..399, 150..249
//     deep  20,20    60x40      -> 220..279, 170..209 in root's coordinates
class GuiHitTestTests : public ::testing::Test
{
protected:
    virtual void SetUp()
    {
        mWasDesignTime = GuiControl::smDesignTime;
        mWasEditorHandle = GuiControl::smEditorHandle;

        mRoot = makeHitControl( "0 0", "800 600" );
        mBack = makeHitControl( "100 100", "400 300" );
        mFront = makeHitControl( "200 150", "200 100" );
        mDeep = makeHitControl( "20 20", "60 40" );

        mRoot->addObject( mBack );
        mRoot->addObject( mFront );
        mFront->addObject( mDeep );

        // Exactly what GuiEditCtrl::onWake does when the editor opens.
        mEdit = new GuiEditCtrl();
        mEdit->registerObject();
        mEdit->setRoot( mRoot );
        GuiControl::smDesignTime = true;
        GuiControl::smEditorHandle = mEdit;
    }

    virtual void TearDown()
    {
        // Put the statics back before anything else can run: leaving the editor
        // switched on would follow this suite into the next one.
        GuiControl::smDesignTime = mWasDesignTime;
        GuiControl::smEditorHandle = mWasEditorHandle;

        mEdit->deleteObject();
        mRoot->deleteObject();   // and the three controls it holds
    }

    // A point over deep, and so over front and back as well.
    Point2I overDeep() const { return Point2I( 250, 190 ); }

    // Over front and back, but outside deep.
    Point2I overFront() const { return Point2I( 380, 240 ); }

    GuiControl*  mRoot;
    GuiControl*  mBack;
    GuiControl*  mFront;
    GuiControl*  mDeep;
    GuiEditCtrl* mEdit;
    bool         mWasDesignTime;
    GuiEditCtrl* mWasEditorHandle;
};

TEST_F( GuiHitTestTests, TheEditRootIsInEditMode )
{
    ASSERT_TRUE( mRoot->isEditMode() ) << "Everything below depends on this.";
    ASSERT_TRUE( mFront->isEditMode() ) << "isEditMode walks up to the edit root.";
}

TEST_F( GuiHitTestTests, TheFrontOneIsHit )
{
    ASSERT_EQ( mRoot->findHitControl( overFront() ), mFront );
    ASSERT_EQ( mRoot->findHitControl( overDeep() ), mDeep );
}

TEST_F( GuiHitTestTests, HidingTheFrontOneLetsTheClickThrough )
{
    mFront->setHidden( true );

    ASSERT_EQ( mRoot->findHitControl( overFront() ), mBack )
        << "The whole point of the eye: a control that is not drawn is not a "
           "target, so the click reaches what is behind it.";
}

TEST_F( GuiHitTestTests, AHiddenControlTakesItsChildrenWithIt )
{
    mFront->setHidden( true );

    ASSERT_EQ( mRoot->findHitControl( overDeep() ), mBack )
        << "Hiding a container stops the whole branch being drawn, so the whole "
           "branch has to stop being hit -- otherwise a hidden panel's children "
           "still eat every click aimed through it.";
}

TEST_F( GuiHitTestTests, HidingAChildLeavesItsParentAlone )
{
    mDeep->setHidden( true );

    ASSERT_EQ( mRoot->findHitControl( overDeep() ), mFront )
        << "One control, not the branch it sits in.";
    ASSERT_EQ( mRoot->findHitControl( overFront() ), mFront );
}

TEST_F( GuiHitTestTests, ShowingItAgainPutsItBack )
{
    mFront->setHidden( true );
    mFront->setHidden( false );

    ASSERT_EQ( mRoot->findHitControl( overFront() ), mFront );
    ASSERT_EQ( mRoot->findHitControl( overDeep() ), mDeep );
}

// The flag is editor scaffolding and is never written to a file, so the only
// thing standing between it and a shipped game is isEditMode. These two tests
// are that guarantee, from both ends.

TEST_F( GuiHitTestTests, OutsideTheEditRootTheFlagMeansNothing )
{
    GuiControl* stray = makeHitControl( "0 0", "800 600" );
    GuiControl* strayBack = makeHitControl( "100 100", "400 300" );
    GuiControl* strayFront = makeHitControl( "200 150", "200 100" );
    stray->addObject( strayBack );
    stray->addObject( strayFront );
    strayFront->setHidden( true );

    ASSERT_FALSE( stray->isEditMode() ) << "Never parented into the edit root.";
    ASSERT_EQ( stray->findHitControl( overFront() ), strayFront )
        << "A Gui that is not the one being edited must behave exactly as it "
           "did before, flag or no flag.";

    stray->deleteObject();
}

TEST_F( GuiHitTestTests, WithTheEditorShutTheFlagMeansNothing )
{
    mFront->setHidden( true );
    GuiControl::smDesignTime = false;

    ASSERT_EQ( mRoot->findHitControl( overFront() ), mFront )
        << "smDesignTime is off the moment the editor sleeps, and a running "
           "game must not consult the flag at all.";
}

#endif // TORQUE_SHIPPING
