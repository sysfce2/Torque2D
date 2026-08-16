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

// ImageAsset FIRST. AnimationAsset.h only forward-declares it, and holds an
// AssetPtr<ImageAsset> -- whose destructor asks the pointee for its class name,
// which needs the complete type. Every .cc that includes AnimationAsset.h has so
// far happened to include ImageAsset.h as well; this is the first that would not.
#ifndef _IMAGE_ASSET_H_
#include "2d/assets/ImageAsset.h"
#endif

#ifndef _ANIMATION_ASSET_H_
#include "2d/assets/AnimationAsset.h"
#endif

//-----------------------------------------------------------------------------
// Moving an animation's frame list between index space and name space.
//
// An image in explicit mode cuts itself into named cells, and an animation on it
// lists those names; an image cut into a grid has no names, and the animation
// lists indices. Switching an image between the two modes therefore switches
// every animation on it, and these functions are what stops that costing the
// animation its frames.
//
// Tested through the statics rather than through a real pair of assets, and that
// is not a shortcut: ImageAsset::addExplicitCell validates every cell against
// getImageWidth()/getImageHeight() and so refuses to add one until a bitmap is
// loaded, which needs a file and a GL context this suite does not have. So the
// translation takes a table of cell names -- entry N is what cell N is called --
// and the caller builds that table from the image. What is left here is the
// arithmetic, which is the part that can be wrong.
//
// Throughout: a four cell sheet called head, body, tail, wing.
//-----------------------------------------------------------------------------

static void buildCellNames( Vector<StringTableEntry>& cellNames )
{
    cellNames.clear();
    cellNames.push_back( StringTable->insert( "head" ) );
    cellNames.push_back( StringTable->insert( "body" ) );
    cellNames.push_back( StringTable->insert( "tail" ) );
    cellNames.push_back( StringTable->insert( "wing" ) );
}

static void buildIndices( Vector<S32>& indices, const S32 a = -1, const S32 b = -1, const S32 c = -1 )
{
    indices.clear();
    if ( a >= 0 ) indices.push_back( a );
    if ( b >= 0 ) indices.push_back( b );
    if ( c >= 0 ) indices.push_back( c );
}

//-----------------------------------------------------------------------------
// The round trip. Everything else here is a way for this to go wrong.
//-----------------------------------------------------------------------------

TEST( AnimationFrameConversionTests, IndicesBecomeNames )
{
    Vector<StringTableEntry> cellNames;
    buildCellNames( cellNames );

    Vector<S32> indices;
    buildIndices( indices, 0, 2, 3 );

    Vector<StringTableEntry> names;
    AnimationAsset::translateFrames( indices, cellNames, names );

    ASSERT_EQ( names.size(), 3 );
    ASSERT_EQ( names[0], StringTable->insert( "head" ) );
    ASSERT_EQ( names[1], StringTable->insert( "tail" ) );
    ASSERT_EQ( names[2], StringTable->insert( "wing" ) );

    SUCCEED();
}

TEST( AnimationFrameConversionTests, NamesBecomeIndices )
{
    Vector<StringTableEntry> cellNames;
    buildCellNames( cellNames );

    Vector<StringTableEntry> names;
    names.push_back( StringTable->insert( "head" ) );
    names.push_back( StringTable->insert( "tail" ) );
    names.push_back( StringTable->insert( "wing" ) );

    Vector<S32> indices;
    AnimationAsset::translateFrames( names, cellNames, indices );

    ASSERT_EQ( indices.size(), 3 );
    ASSERT_EQ( indices[0], 0 );
    ASSERT_EQ( indices[1], 2 );
    ASSERT_EQ( indices[2], 3 );

    SUCCEED();
}

TEST( AnimationFrameConversionTests, ARoundTripChangesNothing )
{
    Vector<StringTableEntry> cellNames;
    buildCellNames( cellNames );

    Vector<S32> indices;
    buildIndices( indices, 3, 1, 0 );

    Vector<StringTableEntry> names;
    AnimationAsset::translateFrames( indices, cellNames, names );

    Vector<S32> backAgain;
    AnimationAsset::translateFrames( names, cellNames, backAgain );

    ASSERT_EQ( backAgain.size(), 3 );
    ASSERT_EQ( backAgain[0], 3 );
    ASSERT_EQ( backAgain[1], 1 );
    ASSERT_EQ( backAgain[2], 0 );

    SUCCEED();
}

//-----------------------------------------------------------------------------
// A hold. One frame repeated is the only way the format has of making a pose
// last longer -- there is no per-frame duration -- so a translation that
// helpfully removed the duplicate would silently change the timing.
//-----------------------------------------------------------------------------

TEST( AnimationFrameConversionTests, ARepeatedFrameStaysRepeated )
{
    Vector<StringTableEntry> cellNames;
    buildCellNames( cellNames );

    Vector<S32> indices;
    buildIndices( indices, 1, 1, 1 );

    Vector<StringTableEntry> names;
    AnimationAsset::translateFrames( indices, cellNames, names );

    ASSERT_EQ( names.size(), 3 )
        << "A hold is three slots holding the same frame. Collapsing it to one "
           "would make the pose a third as long.";
    ASSERT_EQ( names[0], names[2] );

    SUCCEED();
}

//-----------------------------------------------------------------------------
// What does not resolve. Skipped, in both directions.
//
// There is no honest name for an index with no cell behind it, and inventing one
// risks colliding with a cell somebody names that later -- at which point the
// animation would silently start playing real art in place of a hole.
//-----------------------------------------------------------------------------

TEST( AnimationFrameConversionTests, AnIndexPastTheCellsIsDropped )
{
    Vector<StringTableEntry> cellNames;
    buildCellNames( cellNames );

    Vector<S32> indices;
    buildIndices( indices, 0, 9, 1 );

    Vector<StringTableEntry> names;
    AnimationAsset::translateFrames( indices, cellNames, names );

    ASSERT_EQ( names.size(), 2 );
    ASSERT_EQ( names[0], StringTable->insert( "head" ) );
    ASSERT_EQ( names[1], StringTable->insert( "body" ) );

    SUCCEED();
}

TEST( AnimationFrameConversionTests, ANegativeIndexIsDropped )
{
    Vector<StringTableEntry> cellNames;
    buildCellNames( cellNames );

    // -1 is what a name that resolved to nothing looks like once it has been
    // through the timeline, so it can genuinely arrive here.
    Vector<S32> indices;
    indices.push_back( -1 );
    indices.push_back( 2 );

    Vector<StringTableEntry> names;
    AnimationAsset::translateFrames( indices, cellNames, names );

    ASSERT_EQ( names.size(), 1 );
    ASSERT_EQ( names[0], StringTable->insert( "tail" ) );

    SUCCEED();
}

TEST( AnimationFrameConversionTests, ANameNoCellAnswersToIsDropped )
{
    Vector<StringTableEntry> cellNames;
    buildCellNames( cellNames );

    Vector<StringTableEntry> names;
    names.push_back( StringTable->insert( "head" ) );
    names.push_back( StringTable->insert( "elbow" ) );
    names.push_back( StringTable->insert( "tail" ) );

    Vector<S32> indices;
    AnimationAsset::translateFrames( names, cellNames, indices );

    ASSERT_EQ( indices.size(), 2 );
    ASSERT_EQ( indices[0], 0 );
    ASSERT_EQ( indices[1], 2 );

    SUCCEED();
}

//-----------------------------------------------------------------------------
// An unnamed cell cannot be addressed by name, so an index pointing at one has
// nothing to become. This is the case the auto-naming in ImageAsset exists to
// make impossible; the translation still has to survive meeting it.
//-----------------------------------------------------------------------------

TEST( AnimationFrameConversionTests, AnIndexOnAnUnnamedCellIsDropped )
{
    Vector<StringTableEntry> cellNames;
    cellNames.push_back( StringTable->insert( "head" ) );
    cellNames.push_back( StringTable->EmptyString );
    cellNames.push_back( StringTable->insert( "tail" ) );

    Vector<S32> indices;
    buildIndices( indices, 0, 1, 2 );

    Vector<StringTableEntry> names;
    AnimationAsset::translateFrames( indices, cellNames, names );

    ASSERT_EQ( names.size(), 2 );
    ASSERT_EQ( names[0], StringTable->insert( "head" ) );
    ASSERT_EQ( names[1], StringTable->insert( "tail" ) );

    SUCCEED();
}

//-----------------------------------------------------------------------------
// The empty cases, both of which are reached on a perfectly ordinary edit: an
// animation whose timeline has just been emptied, and an image with no cells cut
// yet.
//-----------------------------------------------------------------------------

TEST( AnimationFrameConversionTests, AnEmptyListTranslatesToAnEmptyList )
{
    Vector<StringTableEntry> cellNames;
    buildCellNames( cellNames );

    Vector<S32> indices;
    Vector<StringTableEntry> names;

    // Seeded with something, so that clearing it is what is being observed rather
    // than it merely never having been filled.
    names.push_back( StringTable->insert( "stale" ) );

    AnimationAsset::translateFrames( indices, cellNames, names );

    ASSERT_EQ( names.size(), 0 );

    SUCCEED();
}

TEST( AnimationFrameConversionTests, NoCellsMeansNothingResolves )
{
    Vector<StringTableEntry> cellNames;

    Vector<S32> indices;
    buildIndices( indices, 0, 1, 2 );

    Vector<StringTableEntry> names;
    AnimationAsset::translateFrames( indices, cellNames, names );

    ASSERT_EQ( names.size(), 0 );

    SUCCEED();
}

#endif // TORQUE_SHIPPING
