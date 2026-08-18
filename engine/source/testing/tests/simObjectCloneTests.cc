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

#ifndef _SIMBASE_H_
#include "sim/simBase.h"
#endif

#ifndef _SIMSET_H_
#include "sim/simSet.h"
#endif

#ifndef _GUICONTROL_H_
#include "gui/guiControl.h"
#endif

#ifndef _GUIFRAMESETCTRL_H_
#include "gui/containers/guiFrameSetCtrl.h"
#endif

#ifndef _CONSOLE_H_
#include "console/console.h"
#endif

//-----------------------------------------------------------------------------
// SimObject::deepClone and the copyFieldsFrom it is built on.
//
// A deep clone exists for copy and paste in the Gui Editor, and the two things
// it promises are the two things a clipboard needs: the copy holds everything
// the original held, and nothing of the original's script lifecycle runs while
// it is being made. Both are asserted here, because both are orderings inside
// the implementation that nothing else would catch if they were reversed.
//-----------------------------------------------------------------------------

static StringTableEntry fieldName( const char* name )
{
    return StringTable->insert( name );
}

static const char* readField( SimObject* object, const char* name )
{
    return object->getDataField( fieldName( name ), NULL );
}

static void writeField( SimObject* object, const char* name, const char* value )
{
    object->setDataField( fieldName( name ), NULL, value );
}

//-----------------------------------------------------------------------------
// copyFieldsFrom: the two fields a copy must not take.
//-----------------------------------------------------------------------------

TEST( SimObjectCloneTests, CopyFieldsFromCarriesOrdinaryFields )
{
    SimObject* source = new SimObject();
    source->registerObject();
    writeField( source, "internalName", "carried" );
    writeField( source, "aDynamicField", "also carried" );

    SimObject* target = new SimObject();
    target->registerObject();
    target->copyFieldsFrom( source, 0 );

    ASSERT_STREQ( readField( target, "internalName" ), "carried" );
    ASSERT_STREQ( readField( target, "aDynamicField" ), "also carried" )
        << "Dynamic fields come across too, as assignFieldsFrom always did.";

    source->deleteObject();
    target->deleteObject();
}

// A protected field whose get function answers from the object rather than from
// the raw data it is handed - GuiControl's "text" is one (getTextProperty returns
// obj->getText()) - has to be read through the source. Reading it through the
// destination gives back what the destination already held, so the field silently
// does not copy at all.
TEST( SimObjectCloneTests, CopyFieldsFromCarriesProtectedFieldsWithCustomGetters )
{
    GuiControl* source = new GuiControl();
    source->registerObject();
    writeField( source, "text", "Save As..." );
    writeField( source, "Extent", "123 45" );

    GuiControl* clone = dynamic_cast<GuiControl*>( source->deepClone() );
    ASSERT_TRUE( clone != NULL );

    ASSERT_STREQ( readField( clone, "text" ), "Save As..." )
        << "text is protected with a getter that ignores the data pointer.";
    ASSERT_STREQ( readField( clone, "Extent" ), "123 45" )
        << "Extent is protected too, but with the default getter.";

    clone->deleteObject();
    source->deleteObject();
}

TEST( SimObjectCloneTests, CopyFieldsFromCanSkipTheName )
{
    SimObject* source = new SimObject();
    source->registerObject( "CloneTestNamedSource" );

    SimObject* withName = new SimObject();
    withName->registerObject();
    withName->copyFieldsFrom( source, 0 );
    ASSERT_STREQ( withName->getName(), "CloneTestNamedSource" )
        << "name is an ordinary persist field, so it copies unless asked not to.";

    SimObject* withoutName = new SimObject();
    withoutName->registerObject();
    withoutName->copyFieldsFrom( source, SimObject::CopyFields_SkipName );
    ASSERT_TRUE( withoutName->getName() == NULL || withoutName->getName()[0] == '\0' )
        << "Two objects answering to one name is the bug this flag exists for.";

    source->deleteObject();
    withName->deleteObject();
    withoutName->deleteObject();
}

TEST( SimObjectCloneTests, CopyFieldsFromCanSkipTheParentGroup )
{
    SimGroup* group = new SimGroup();
    group->registerObject();

    SimObject* source = new SimObject();
    source->registerObject();
    group->addObject( source );

    // Copying parentGroup does not record where the object lives, it moves the
    // object there: the field's setter calls parent->addObject().
    SimObject* moved = new SimObject();
    moved->registerObject();
    moved->copyFieldsFrom( source, 0 );
    ASSERT_TRUE( moved->getGroup() == group )
        << "parentGroup is a persist field whose setter adds the object to the group.";

    SimObject* homeless = new SimObject();
    homeless->registerObject();
    homeless->copyFieldsFrom( source, SimObject::CopyFields_SkipParentGroup );
    ASSERT_TRUE( homeless->getGroup() == NULL )
        << "A copy belongs nowhere until someone puts it somewhere.";

    homeless->deleteObject();
    group->deleteObject();      // takes source and moved with it
}

//-----------------------------------------------------------------------------
// deepClone.
//-----------------------------------------------------------------------------

TEST( SimObjectCloneTests, DeepCloneCopiesFieldsButNotIdentity )
{
    SimGroup* group = new SimGroup();
    group->registerObject();

    SimObject* source = new SimObject();
    source->registerObject( "CloneTestDeepSource" );
    group->addObject( source );
    writeField( source, "internalName", "inner" );
    writeField( source, "aDynamicField", "dynamic" );

    SimObject* clone = source->deepClone();
    ASSERT_TRUE( clone != NULL );
    ASSERT_TRUE( clone != source );

    ASSERT_STREQ( readField( clone, "internalName" ), "inner" );
    ASSERT_STREQ( readField( clone, "aDynamicField" ), "dynamic" );
    ASSERT_TRUE( clone->getName() == NULL || clone->getName()[0] == '\0' );
    ASSERT_TRUE( clone->getGroup() == NULL );

    clone->deleteObject();
    group->deleteObject();
}

TEST( SimObjectCloneTests, DeepCloneCopiesTheWholeTree )
{
    SimGroup* source = new SimGroup();
    source->registerObject();

    SimObject* child = new SimObject();
    child->registerObject();
    source->addObject( child );
    writeField( child, "internalName", "child" );

    SimGroup* grandChildHolder = new SimGroup();
    grandChildHolder->registerObject();
    source->addObject( grandChildHolder );

    SimObject* grandChild = new SimObject();
    grandChild->registerObject();
    grandChildHolder->addObject( grandChild );
    writeField( grandChild, "internalName", "grandChild" );

    SimGroup* clone = dynamic_cast<SimGroup*>( source->deepClone() );
    ASSERT_TRUE( clone != NULL );
    ASSERT_EQ( clone->size(), 2 );

    // Deep, not shared: every object below the clone is a new object.
    ASSERT_TRUE( (*clone)[0] != child );
    ASSERT_STREQ( readField( (*clone)[0], "internalName" ), "child" );

    SimGroup* clonedHolder = dynamic_cast<SimGroup*>( (*clone)[1] );
    ASSERT_TRUE( clonedHolder != NULL );
    ASSERT_EQ( clonedHolder->size(), 1 );
    ASSERT_TRUE( (*clonedHolder)[0] != grandChild );
    ASSERT_STREQ( readField( (*clonedHolder)[0], "internalName" ), "grandChild" );

    clone->deleteObject();
    source->deleteObject();
}

TEST( SimObjectCloneTests, DeepCloneOfASimSetDoesNotDuplicateItsMembers )
{
    // A SimSet references objects some group owns; duplicating those would be
    // inventing objects nobody asked for. Only SimGroup recurses.
    SimSet* set = new SimSet();
    set->registerObject();

    SimObject* member = new SimObject();
    member->registerObject();
    set->addObject( member );

    SimSet* clone = dynamic_cast<SimSet*>( set->deepClone() );
    ASSERT_TRUE( clone != NULL );
    ASSERT_EQ( clone->size(), 0 );

    clone->deleteObject();
    set->deleteObject();
    member->deleteObject();
}

//-----------------------------------------------------------------------------
// The promise that makes a deep clone usable as a clipboard: no script
// lifecycle callback fires on it.
//
// copyTo runs last in cloneInto, which is what makes this true - it is what
// links the namespaces, so while the clone is being filled in there is no
// script class on it for registerObject or onChildAdded to find a method on.
// A class whose onAdd builds children would otherwise build a second set on top
// of the ones being copied.
//-----------------------------------------------------------------------------

TEST( SimObjectCloneTests, DeepCloneRunsNoScriptLifecycle )
{
    Con::evaluate(
        "function CloneTestProbe::onAdd(%this) { $CloneTestProbeAdds = $CloneTestProbeAdds + 1; }\n"
        "function CloneTestProbe::onChildAdded(%this, %child) { $CloneTestProbeChildAdds = $CloneTestProbeChildAdds + 1; }\n",
        false, NULL );

    Con::setIntVariable( "$CloneTestProbeAdds", 0 );
    Con::setIntVariable( "$CloneTestProbeChildAdds", 0 );

    GuiControl* source = new GuiControl();
    writeField( source, "class", "CloneTestProbe" );
    source->registerObject();

    ASSERT_EQ( Con::getIntVariable( "$CloneTestProbeAdds" ), 1 )
        << "The original really does have a class whose onAdd runs.";

    GuiControl* child = new GuiControl();
    child->registerObject();
    source->addObject( child );

    ASSERT_EQ( Con::getIntVariable( "$CloneTestProbeChildAdds" ), 1 )
        << "And a class whose onChildAdded runs.";

    Con::setIntVariable( "$CloneTestProbeAdds", 0 );
    Con::setIntVariable( "$CloneTestProbeChildAdds", 0 );

    GuiControl* clone = dynamic_cast<GuiControl*>( source->deepClone() );
    ASSERT_TRUE( clone != NULL );

    ASSERT_EQ( Con::getIntVariable( "$CloneTestProbeAdds" ), 0 )
        << "A deep clone is data: onAdd must not run on it.";
    ASSERT_EQ( Con::getIntVariable( "$CloneTestProbeChildAdds" ), 0 )
        << "Nor onChildAdded, or a class that builds children would double them.";

    ASSERT_EQ( clone->size(), 1 )
        << "Exactly the children the original had, and no more.";
    ASSERT_STREQ( readField( clone, "class" ), "CloneTestProbe" )
        << "The class itself does come across - copyTo is what copies it.";

    clone->deleteObject();
    source->deleteObject();
}

//-----------------------------------------------------------------------------
// GuiFrameSetCtrl, the one control whose layout is not in its field list.
//-----------------------------------------------------------------------------

static U32 countFrames( const GuiFrameSetCtrl::Frame* frame )
{
    if ( frame == NULL )
        return 0;

    return 1 + countFrames( frame->child1 ) + countFrames( frame->child2 );
}

static bool framesMatch( const GuiFrameSetCtrl::Frame* a, const GuiFrameSetCtrl::Frame* b )
{
    if ( a == NULL || b == NULL )
        return a == b;

    if ( a->id != b->id || a->isVertical != b->isVertical ||
         a->isAnchored != b->isAnchored || a->extent != b->extent )
        return false;

    if ( (a->control == NULL) != (b->control == NULL) )
        return false;

    return framesMatch( a->child1, b->child1 ) && framesMatch( a->child2, b->child2 );
}

// Every control the tree holds is one of %owner's own children.
static bool framesHoldOwnChildren( const GuiFrameSetCtrl::Frame* frame, GuiFrameSetCtrl* owner )
{
    if ( frame == NULL )
        return true;

    if ( frame->control != NULL && frame->control->getGroup() != owner )
        return false;

    return framesHoldOwnChildren( frame->child1, owner ) &&
           framesHoldOwnChildren( frame->child2, owner );
}

TEST( SimObjectCloneTests, DeepCloneRebuildsAFrameSetsTree )
{
    GuiFrameSetCtrl* source = new GuiFrameSetCtrl();
    source->registerObject();
    source->resize( Point2I( 0, 0 ), Point2I( 400, 200 ) );

    // Root frame is 1. Split it, then split the right half, for three leaves.
    const Point2I halves = source->splitFrame( 1, false );
    source->splitFrame( halves.y, true );

    for ( U32 i = 0; i < 3; i++ )
    {
        GuiControl* panel = new GuiControl();
        panel->registerObject();
        source->addObject( panel );
    }

    // Settle the tree before anything is measured. A split leaves its new frames
    // at their constructed extent until the control resizes, and the copy ends
    // with a resize of its own (as setFrameLayout does) - so an unsettled source
    // would differ from its copy over extents neither of them had been told yet.
    source->resize( Point2I( 0, 0 ), Point2I( 400, 200 ) );

    ASSERT_EQ( countFrames( &source->mRootFrame ), 5u )
        << "Two splits make five frames: a root, two halves, and two under one of them.";
    ASSERT_TRUE( framesHoldOwnChildren( &source->mRootFrame, source ) );

    GuiFrameSetCtrl* clone = dynamic_cast<GuiFrameSetCtrl*>( source->deepClone() );
    ASSERT_TRUE( clone != NULL );
    ASSERT_EQ( clone->size(), 3 );

    ASSERT_EQ( countFrames( &clone->mRootFrame ), countFrames( &source->mRootFrame ) );
    ASSERT_TRUE( framesMatch( &clone->mRootFrame, &source->mRootFrame ) )
        << "Same shape, same ids, same extents, same anchoring, same frames filled.";
    ASSERT_TRUE( framesHoldOwnChildren( &clone->mRootFrame, clone ) )
        << "And filled with the CLONE's children, not the original's.";

    clone->deleteObject();
    source->deleteObject();
}

#endif // TORQUE_SHIPPING
