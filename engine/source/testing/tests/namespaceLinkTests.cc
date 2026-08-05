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

#ifndef _CONSOLE_H_
#include "console/console.h"
#endif

#ifndef _CONSOLENAMESPACE_H_
#include "console/consoleNamespace.h"
#endif

//-----------------------------------------------------------------------------
// Namespace linkage for an object named after its own class.
//
// A script singleton is written as
//
//     new ScriptObject(PlanetXUpgrades) { class = "PlanetXUpgrades"; };
//
// so that every file can reach it by name and its methods can be written as
// PlanetXUpgrades::foo. SimObject::linkNamespaces then links the class to the
// C++ class, and a moment later links the OBJECT NAME to the class - but those
// are one namespace, so it asks that namespace to become its own parent.
//
// Self-parenting is a no-op: the namespace is already the one the object wears.
// It used to be an error instead ("cannot change namespace parent linkage of
// PlanetXUpgrades from ScriptObject to PlanetXUpgrades"), with the mirror image
// at teardown - a failure reported for something that neither failed nor did
// anything. It is a warning now, because the object did say one word twice and
// the second one buys nothing.
//
// These tests hold that warning to what it is worth - said once, where the
// repeat is written, naming the namespace - hold the parent reference count
// balanced across the no-op, which is what a mismatched link/unlink pair would
// quietly break, and hold onto the error the guard really exists for: one
// namespace being given two different parents.
//-----------------------------------------------------------------------------

static bool sCapturing = false;
static U32  sErrorCount = 0;
static U32  sWarningCount = 0;
static char sFirstError[1024];
static char sFirstWarning[1024];

static void remember( char* buffer, U32 size, const char* line )
{
    dStrncpy( buffer, line, size - 1 );
    buffer[size - 1] = '\0';
}

static void captureOutput( ConsoleLogEntry::Level level, const char* line )
{
    if ( !sCapturing )
        return;

    if ( level == ConsoleLogEntry::Error )
    {
        if ( sErrorCount == 0 )
            remember( sFirstError, sizeof(sFirstError), line );

        sErrorCount++;
    }
    else if ( level == ConsoleLogEntry::Warning )
    {
        if ( sWarningCount == 0 )
            remember( sFirstWarning, sizeof(sFirstWarning), line );

        sWarningCount++;
    }
}

static void beginCapture()
{
    sErrorCount = 0;
    sWarningCount = 0;
    sFirstError[0] = '\0';
    sFirstWarning[0] = '\0';
    sCapturing = true;
    Con::addConsumer( captureOutput );
}

static void endCapture()
{
    Con::removeConsumer( captureOutput );
    sCapturing = false;
}

//-----------------------------------------------------------------------------
// The singleton named after its class.
//-----------------------------------------------------------------------------

TEST( NamespaceLinkTests, ObjectNamedAfterItsClassIsWarnedNotFailed )
{
    SimObject* singleton = new SimObject();
    singleton->setClassNamespace( "NsLinkTestSingleton" );

    beginCapture();
    singleton->registerObject( "NsLinkTestSingleton" );
    endCapture();

    ASSERT_EQ( sErrorCount, 0u )
        << "Nothing failed, so nothing should say it did: " << sFirstError;
    ASSERT_EQ( sWarningCount, 1u )
        << "The redundant class is worth one warning - no more, and not none.";
    ASSERT_TRUE( dStrstr( sFirstWarning, "NsLinkTestSingleton" ) != NULL )
        << "It has to name the namespace to be actionable, and said: " << sFirstWarning;

    ASSERT_TRUE( singleton->getNamespace() != NULL );
    ASSERT_STREQ( singleton->getNamespace()->mName, "NsLinkTestSingleton" )
        << "It still wears the namespace its script methods are written in.";

    beginCapture();
    singleton->deleteObject();
    endCapture();

    ASSERT_EQ( sErrorCount, 0u )
        << "Deleting it said: " << sFirstError;
    ASSERT_EQ( sWarningCount, 0u )
        << "Teardown repeats nothing - the warning belongs where the repeat is written.";
}

// Link and unlink have to agree about whether the self-link counted, or the
// count drifts: too many unlinks and the class loses its parent while another
// object is still wearing it, too few and it never comes back at all. Two full
// rounds catch either one.
TEST( NamespaceLinkTests, SelfLinkLeavesTheParentReferenceCountBalanced )
{
    StringTableEntry name = StringTable->insert( "NsLinkTestBalanced" );

    for ( U32 round = 0; round < 2; round++ )
    {
        SimObject* singleton = new SimObject();
        singleton->setClassNamespace( name );
        singleton->registerObject( name );

        Namespace* linked = Namespace::find( name );
        ASSERT_TRUE( linked->mParent != NULL )
            << "Round " << round << ": the class namespace must inherit the C++ class.";
        ASSERT_STREQ( linked->mParent->mName, "SimObject" )
            << "Round " << round << ": the self-link must not have displaced that parent.";

        singleton->deleteObject();
    }

    Namespace* ns = Namespace::find( name );
    ASSERT_EQ( ns->mRefCountToParent, 0u )
        << "Every link was given back.";
    ASSERT_TRUE( ns->mParent == NULL )
        << "So the namespace is unlinked again.";
}

//-----------------------------------------------------------------------------
// What the guard is really for.
//-----------------------------------------------------------------------------

// Two objects giving one class namespace two different superclasses is a real
// ambiguity - whichever registered first silently decides what the other
// inherits - so it must still be reported. This test therefore writes one
// genuine error line to the console log; the NsLinkTest names in it say so.
TEST( NamespaceLinkTests, TwoDifferentParentsForOneClassIsStillAnError )
{
    SimObject* first = new SimObject();
    first->setSuperClassNamespace( "NsLinkTestParentOne" );
    first->setClassNamespace( "NsLinkTestShared" );
    first->registerObject();

    SimObject* second = new SimObject();
    second->setSuperClassNamespace( "NsLinkTestParentTwo" );
    second->setClassNamespace( "NsLinkTestShared" );

    beginCapture();
    second->registerObject();
    endCapture();

    ASSERT_EQ( sErrorCount, 1u )
        << "One namespace cannot have two parents, and saying so is the point of the guard.";

    second->deleteObject();
    first->deleteObject();
}

#endif // TORQUE_SHIPPING
