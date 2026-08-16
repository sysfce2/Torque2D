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

#ifndef _IMAGE_ASSET_H_
#include "2d/assets/ImageAsset.h"
#endif

//-----------------------------------------------------------------------------
// Naming an explicit cell that arrived without a name.
//
// An animation on an explicit image addresses its frames by cell name, so a cell
// with no name is a frame no animation can reach. The engine names those on the
// way through calculateExplicitMode, which is the one funnel every path that
// changes cells ends in -- including the TAML read, which pushes straight into
// the vector without going near addExplicitCell.
//
// Tested through the static, because the naming has to be right in a place a
// unit test cannot reach: building a real cell needs a bitmap, and a bitmap needs
// a GL context this suite has none of. What the static needs is the set of names
// already spoken for and the index of the cell being named, which is exactly what
// the caller has.
//
// The seed is the cell's OWN index rather than zero, so a sheet whose cells have
// never been named comes out Frame0, Frame1, Frame2 -- matching the number the
// person reads off the frame grid beside it.
//-----------------------------------------------------------------------------

TEST( ImageAssetCellNameTests, TheFirstCellOfAnUnnamedSheetIsFrameZero )
{
    Vector<StringTableEntry> used;

    ASSERT_EQ( ImageAsset::nextAvailableCellName( used, 0 ), StringTable->insert( "Frame0" ) );

    SUCCEED();
}

TEST( ImageAssetCellNameTests, ACellIsNamedForItsOwnIndex )
{
    Vector<StringTableEntry> used;

    ASSERT_EQ( ImageAsset::nextAvailableCellName( used, 2 ), StringTable->insert( "Frame2" ) )
        << "Seeded at zero instead, the third cell of an unnamed sheet would be "
           "called Frame0 and the grid beside it would say 2.";

    SUCCEED();
}

//-----------------------------------------------------------------------------
// The collisions, which are not hypothetical: delete a cell from the middle of a
// named sheet and every index after it now belongs to a cell called something
// else.
//-----------------------------------------------------------------------------

TEST( ImageAssetCellNameTests, ATakenNameIsWalkedPast )
{
    Vector<StringTableEntry> used;
    used.push_back( StringTable->insert( "Frame2" ) );

    ASSERT_EQ( ImageAsset::nextAvailableCellName( used, 2 ), StringTable->insert( "Frame3" ) );

    SUCCEED();
}

TEST( ImageAssetCellNameTests, ARunOfTakenNamesIsWalkedPast )
{
    Vector<StringTableEntry> used;
    used.push_back( StringTable->insert( "Frame2" ) );
    used.push_back( StringTable->insert( "Frame3" ) );
    used.push_back( StringTable->insert( "Frame4" ) );

    ASSERT_EQ( ImageAsset::nextAvailableCellName( used, 2 ), StringTable->insert( "Frame5" ) );

    SUCCEED();
}

TEST( ImageAssetCellNameTests, ATakenNameBelowTheSeedIsNotWalkedPast )
{
    // Frame0 and Frame1 being taken says nothing about Frame2. Searching from
    // zero every time would step past them for no reason and number the cells
    // further and further from their own indices.
    Vector<StringTableEntry> used;
    used.push_back( StringTable->insert( "Frame0" ) );
    used.push_back( StringTable->insert( "Frame1" ) );

    ASSERT_EQ( ImageAsset::nextAvailableCellName( used, 2 ), StringTable->insert( "Frame2" ) );

    SUCCEED();
}

//-----------------------------------------------------------------------------
// A name that is not of the form the search generates cannot collide with one
// that is, so it must not be allowed to push the search along.
//-----------------------------------------------------------------------------

TEST( ImageAssetCellNameTests, AnUnrelatedNameIsIgnored )
{
    Vector<StringTableEntry> used;
    used.push_back( StringTable->insert( "head" ) );
    used.push_back( StringTable->insert( "body" ) );

    ASSERT_EQ( ImageAsset::nextAvailableCellName( used, 0 ), StringTable->insert( "Frame0" ) );

    SUCCEED();
}

//-----------------------------------------------------------------------------
// Case. StringTable folds it by default, so "FRAME2" and "Frame2" are the same
// entry -- which means a cell somebody typed in capitals still blocks the name,
// as it must: getExplicitCellIndex would find either one for the other.
//-----------------------------------------------------------------------------

TEST( ImageAssetCellNameTests, ATakenNameCollidesRegardlessOfCase )
{
    Vector<StringTableEntry> used;
    used.push_back( StringTable->insert( "FRAME0" ) );

    ASSERT_EQ( ImageAsset::nextAvailableCellName( used, 0 ), StringTable->insert( "Frame1" ) );

    SUCCEED();
}

#endif // TORQUE_SHIPPING
