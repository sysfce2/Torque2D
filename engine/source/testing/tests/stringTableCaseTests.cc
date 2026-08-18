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

#ifndef _STRINGTABLE_H_
#include "string/stringTable.h"
#endif

//-----------------------------------------------------------------------------
// What the string table does to the case of a string it is given.
//
// insert() defaults to caseSens = false, and the hash is case insensitive by
// construction -- hashString runs every byte through a to-lower table -- so two
// spellings that differ only in case always land in the same bucket. An
// insensitive insert then matches the first one there with dStricmp and hands
// back ITS spelling, not the one it was asked for.
//
// For a name that is only ever compared, that is the point of the table. For a
// FILESYSTEM PATH it is data corruption: whichever subsystem happened to intern
// "Sprites" first decides that a module declaring "sprites" gets "Sprites"
// written back to its module.taml, and on a case sensitive filesystem that
// directory then does not exist.
//
// These pin down both halves -- the trap and the flag that avoids it -- because
// the fix is a one word argument at each call site, and an argument that
// silently stops mattering is worth a test.
//
// Every string here is prefixed and unique to its own test. The table is a
// process-wide singleton with no way to remove an entry, so a shared spelling
// would make these tests depend on each other's order.
//-----------------------------------------------------------------------------

TEST( StringTableCaseTests, AnInsensitiveInsertReturnsTheSpellingAlreadyInTheTable )
{
    StringTableEntry first = StringTable->insert( "StCaseTrapSprites" );
    StringTableEntry second = StringTable->insert( "stcasetrapsprites" );

    // One entry, not two.
    ASSERT_EQ( first, second );

    // And the caller that asked for lower case did not get lower case back.
    ASSERT_STREQ( second, "StCaseTrapSprites" );

    SUCCEED();
}

TEST( StringTableCaseTests, ASensitiveInsertKeepsTheSpellingItWasGiven )
{
    StringTableEntry mixed = StringTable->insert( "StCaseKeepSprites", true );
    StringTableEntry lower = StringTable->insert( "stcasekeepsprites", true );

    ASSERT_NE( mixed, lower );
    ASSERT_STREQ( mixed, "StCaseKeepSprites" );
    ASSERT_STREQ( lower, "stcasekeepsprites" );

    SUCCEED();
}

// The order that does the damage in the wild: something else gets there first.
TEST( StringTableCaseTests, ASensitiveInsertSurvivesAnotherSpellingArrivingFirst )
{
    StringTable->insert( "StCasePoisonFonts" );

    StringTableEntry kept = StringTable->insert( "stcasepoisonfonts", true );

    ASSERT_STREQ( kept, "stcasepoisonfonts" );

    SUCCEED();
}

// Both spellings stay reachable afterwards, which is what makes it safe to store
// one of each: a case sensitive lookup finds the one it names.
TEST( StringTableCaseTests, BothSpellingsRemainDistinctUnderLookup )
{
    StringTable->insert( "StCaseLookupParticles", true );
    StringTable->insert( "stcaselookupparticles", true );

    ASSERT_STREQ( StringTable->lookup( "StCaseLookupParticles", true ), "StCaseLookupParticles" );
    ASSERT_STREQ( StringTable->lookup( "stcaselookupparticles", true ), "stcaselookupparticles" );

    SUCCEED();
}

// THE HAZARD THE FIX INTRODUCES, pinned down so it is not discovered later.
//
// StringTableEntry equality is pointer equality, and that only holds because
// both sides were interned the same way. Once one spelling is in the table
// insensitively and the other sensitively, an insensitive insert of EITHER
// spelling returns whichever node sits earlier in the bucket -- so a case
// sensitive field compared by pointer against a value interned the old way can
// miss.
//
// This is why the flag belongs only on values that are paths, and why every
// pointer comparison against such a field has to be checked when one is changed.
TEST( StringTableCaseTests, AnInsensitiveInsertCanMissACaseSensitiveEntry )
{
    // The old-style intern, first into the bucket.
    StringTableEntry existing = StringTable->insert( "StCaseHazardGui" );

    // The new-style one, a second node in the same bucket.
    StringTableEntry sensitive = StringTable->insert( "stcasehazardgui", true );
    ASSERT_NE( existing, sensitive );

    // A caller still interning the old way gets the FIRST node, whichever
    // spelling it asks for -- so comparing it against the sensitive entry fails.
    ASSERT_EQ( StringTable->insert( "stcasehazardgui" ), existing );
    ASSERT_NE( StringTable->insert( "stcasehazardgui" ), sensitive );

    SUCCEED();
}

#endif // TORQUE_SHIPPING
