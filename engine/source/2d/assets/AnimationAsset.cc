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

#include "console/consoleTypes.h"
#include "2d/assets/ImageAsset.h"
#include "2d/assets/AnimationAsset.h"

// Script bindings.
#include "AnimationAsset_ScriptBinding.h"

// Debug Profiling.
#include "debug/profiler.h"

//------------------------------------------------------------------------------

ConsoleType( animationAssetPtr, TypeAnimationAssetPtr, sizeof(AssetPtr<AnimationAsset>), ASSET_ID_FIELD_PREFIX )

//-----------------------------------------------------------------------------

ConsoleGetType( TypeAnimationAssetPtr )
{
    // Fetch asset Id.
    return (*((AssetPtr<AnimationAsset>*)dptr)).getAssetId();
}

//-----------------------------------------------------------------------------

ConsoleSetType( TypeAnimationAssetPtr )
{
    // Was a single argument specified?
    if( argc == 1 )
    {
        // Yes, so fetch field value.
        const char* pFieldValue = argv[0];

        // Fetch asset pointer.
        AssetPtr<AnimationAsset>* pAssetPtr = dynamic_cast<AssetPtr<AnimationAsset>*>((AssetPtrBase*)(dptr));

        // Is the asset pointer the correct type?
        if ( pAssetPtr == NULL )
        {
            // No, so fail.
            Con::warnf( "(TypeAnimationAssetPtr) - Failed to set asset Id '%d'.", pFieldValue );
            return;
        }

        // Set asset.
        pAssetPtr->setAssetId( pFieldValue );

        return;
   }

    // Warn.
    Con::warnf( "(TypeAnimationAssetPtr) - Cannot set multiple args to a single asset." );
}

//------------------------------------------------------------------------------

IMPLEMENT_CONOBJECT(AnimationAsset);

//------------------------------------------------------------------------------

AnimationAsset::AnimationAsset() :  mAnimationTime(1.0f),
                                    mAnimationCycle(true),
                                    mRandomStart(false)
{
    // Set Vector Associations.
    VECTOR_SET_ASSOCIATION( mAnimationFrames );
    VECTOR_SET_ASSOCIATION( mNamedAnimationFrames );
    VECTOR_SET_ASSOCIATION( mValidatedFrames );
    VECTOR_SET_ASSOCIATION( mValidatedNameFrames );
}

//------------------------------------------------------------------------------

AnimationAsset::~AnimationAsset()
{
}

//------------------------------------------------------------------------------

void AnimationAsset::initPersistFields()
{
    // Call parent.
    Parent::initPersistFields();

    addProtectedField("Image", TypeImageAssetPtr, Offset(mImageAsset, AnimationAsset), &setImage, &defaultProtectedGetFn, &writeImage, "");
    addProtectedField("AnimationFrames", TypeS32Vector, Offset(mAnimationFrames, AnimationAsset), &setAnimationFrames, &defaultProtectedGetFn, &writeAnimationFrames, "");
    addProtectedField("NamedAnimationFrames", TypeStringTableEntryVector, Offset(mNamedAnimationFrames, AnimationAsset), &setNamedAnimationFrames, &defaultProtectedGetFn, &writeNamedAnimationFrames, "");
    addProtectedField("AnimationTime", TypeF32, Offset(mAnimationTime, AnimationAsset), &setAnimationTime, &defaultProtectedGetFn, &defaultProtectedWriteFn, "");
    addProtectedField("AnimationCycle", TypeBool, Offset(mAnimationCycle, AnimationAsset), &setAnimationCycle, &defaultProtectedGetFn, &writeAnimationCycle, "");
    addProtectedField("RandomStart", TypeBool, Offset(mRandomStart, AnimationAsset), &setRandomStart, &defaultProtectedGetFn, &writeRandomStart, "");

    // There is no NamedCellsMode field, and deliberately so. Whether an animation
    // uses names is the image's business -- see getNamedCellsMode.
}

//------------------------------------------------------------------------------

bool AnimationAsset::onAdd()
{
    // Call Parent.
    if(!Parent::onAdd())
        return false;

    // Return Okay.
    return true;
}

//------------------------------------------------------------------------------

void AnimationAsset::onRemove()
{
    // Call Parent.
    Parent::onRemove();
}

//------------------------------------------------------------------------------

void AnimationAsset::onAssetRefresh( void )
{
    // Ignore if not yet added to the sim.
    if ( !isProperlyAdded() )
        return;

    // A refresh reaches us both when we were changed ourselves and when the image
    // asset we depend on was, which is also the only warning we get that the image
    // has changed which space our frames are counted in.
    convertFramesForMode();

    // Re-validate the frames.  The image may have been re-cut into a different
    // number of cells, and without this the validated list keeps indices from the
    // old cut, and getImageFrameArea() clamps them to the last frame -- so the
    // animation plays the wrong art and says nothing.
    validateFrames();

    // Call parent.
    Parent::onAssetRefresh();
}

//------------------------------------------------------------------------------

void AnimationAsset::setImage( const char* pAssetId )
{
    // Ignore no change.
    if ( mImageAsset.getAssetId() == StringTable->insert( pAssetId ) )
        return;

    // Update.
    mImageAsset = pAssetId;

    // Repointing at an image that counts its frames differently is a mode switch
    // like any other.
    convertFramesForMode();

    // Validate frames.
    validateFrames();

    // Refresh the asset.
    refreshAsset();
}

//------------------------------------------------------------------------------

void AnimationAsset::setAnimationFrames( const char* pAnimationFrames )
{
    // Debug Profiling.
    PROFILE_SCOPE(AnimationAsset_SetAnimationFrames);

    // Ignore no change, as every setter on AssetBase already does.
    //
    // This one did not, so writing the same frame list back counted as a change:
    // it announced itself, marked the asset unsaved, and -- once the Asset Manager
    // started recording undo -- left a step that put nothing back. The list is the
    // whole comparison now; it used to have to consider the mode as well, because
    // this setter also cleared named cells mode, and no longer does.
    {
        const U32 currentCount = StringUnit::getUnitCount( pAnimationFrames, " \t\n" );

        if ( currentCount == (U32)mAnimationFrames.size() )
        {
            bool changed = false;

            for( U32 frameIndex = 0; frameIndex < currentCount; ++frameIndex )
            {
                if ( dAtoi( StringUnit::getUnit( pAnimationFrames, frameIndex, " \t\n" ) ) != mAnimationFrames[frameIndex] )
                {
                    changed = true;
                    break;
                }
            }

            if ( !changed )
                return;
        }
    }

    // Clear any existing frames.
    mAnimationFrames.clear();

    // Fetch frame count.
    const U32 frameCount = StringUnit::getUnitCount( pAnimationFrames, " \t\n" );

    // Iterate frames.
    for( U32 frameIndex = 0; frameIndex < frameCount; ++frameIndex )
    {
        // Store frame.
        mAnimationFrames.push_back( dAtoi( StringUnit::getUnit( pAnimationFrames, frameIndex, " \t\n" ) ) );
    }

    // The named list is left alone, deliberately. Both lists survive so that an
    // image changing mode and changing back costs the animation nothing, and only
    // the one in use is written to the file.

    // Validate frames.
    validateFrames();

    // Refresh the asset.
    refreshAsset();
}

//------------------------------------------------------------------------------

// The comma is not decoration. This field is a TypeStringTableEntryVector, and
// that type's getter -- the one TAML writes through, and the one copyFieldsFrom
// reads through -- joins its entries with commas (consoleTypes.cc, ConsoleGetType
// for TypeStringTableEntryVector). Splitting on whitespace alone meant everything
// that was written came back as a single frame named "head,body,tail": a named
// cells animation did not survive being saved and loaded, nor being copied.
//
// Numbered frames never had the problem, because TypeS32Vector's getter joins
// with spaces, which is what the sibling setter below already splits on.
void AnimationAsset::setNamedAnimationFrames( const char* pAnimationFrames )
{
    // Ignore no change, for the same reason as the numbered setter above.
    {
        const U32 currentCount = StringUnit::getUnitCount( pAnimationFrames, " \t\n," );

        if ( currentCount == (U32)mNamedAnimationFrames.size() )
        {
            bool changed = false;

            for( U32 frameIndex = 0; frameIndex < currentCount; ++frameIndex )
            {
                if ( StringTable->insert( StringUnit::getUnit( pAnimationFrames, frameIndex, " \t\n," ) ) != mNamedAnimationFrames[frameIndex] )
                {
                    changed = true;
                    break;
                }
            }

            if ( !changed )
                return;
        }
    }

    // Clear any existing frames.
    mNamedAnimationFrames.clear();

    // Fetch frame count.
    const U32 frameCount = StringUnit::getUnitCount( pAnimationFrames, " \t\n," );

    // Iterate frames.
    for( U32 frameIndex = 0; frameIndex < frameCount; ++frameIndex )
    {
        // Store frame.
        mNamedAnimationFrames.push_back( StringTable->insert( StringUnit::getUnit( pAnimationFrames, frameIndex, " \t\n," ) ) );
    }

    // The numbered list is left alone; see the sibling setter above.

    // Validate frames.
    validateFrames();

    // Refresh the asset.
    refreshAsset();
}

//------------------------------------------------------------------------------

void AnimationAsset::setAnimationTime( const F32 animationTime )
{
    // Ignore no change,
    if ( mIsEqual( animationTime, mAnimationTime ) )
        return;

    // Update.
    mAnimationTime = animationTime;

    // Refresh the asset.
    refreshAsset();
}

//------------------------------------------------------------------------------

void AnimationAsset::setAnimationCycle( const bool animationCycle )
{
    // Ignore no change.
    if ( animationCycle == mAnimationCycle )
        return;

    // Update.
    mAnimationCycle = animationCycle;

    // Refresh the asset.
    refreshAsset();
}

//------------------------------------------------------------------------------

void AnimationAsset::setRandomStart( const bool randomStart )
{
    // Ignore no change.
    if ( randomStart == mRandomStart )
        return;

    // Update.
    mRandomStart = randomStart;

    // Refresh the asset.
    refreshAsset();
}

//------------------------------------------------------------------------------

bool AnimationAsset::getNamedCellsMode( void ) const
{
    // Asked of the image every time rather than cached, so there is nothing that
    // can be left stale. An image in explicit mode has named cells; one cut into
    // a grid does not.
    return mImageAsset.notNull() && mImageAsset->getExplicitMode();
}

//------------------------------------------------------------------------------

void AnimationAsset::translateFrames( const Vector<S32>& indices, const Vector<StringTableEntry>& cellNames, Vector<StringTableEntry>& outNames )
{
    outNames.clear();

    for( Vector<S32>::const_iterator frameItr = indices.begin(); frameItr != indices.end(); ++frameItr )
    {
        const S32 frame = *frameItr;

        if ( frame < 0 || frame >= cellNames.size() )
            continue;

        if ( cellNames[frame] == StringTable->EmptyString )
            continue;

        outNames.push_back( cellNames[frame] );
    }
}

//------------------------------------------------------------------------------

void AnimationAsset::translateFrames( const Vector<StringTableEntry>& names, const Vector<StringTableEntry>& cellNames, Vector<S32>& outIndices )
{
    outIndices.clear();

    for( Vector<StringTableEntry>::const_iterator frameItr = names.begin(); frameItr != names.end(); ++frameItr )
    {
        StringTableEntry frame = *frameItr;

        if ( frame == StringTable->EmptyString )
            continue;

        for ( S32 cellIndex = 0; cellIndex < cellNames.size(); ++cellIndex )
        {
            if ( cellNames[cellIndex] == frame )
            {
                outIndices.push_back( cellIndex );
                break;
            }
        }
    }
}

//------------------------------------------------------------------------------

void AnimationAsset::convertFramesForMode( void )
{
    // Nothing to translate against.
    if ( mImageAsset.isNull() )
        return;

    const bool namedCellsMode = getNamedCellsMode();

    // Only when the list the animation now needs has nothing in it and the other
    // one does. That makes this idempotent, and it makes the switch reversible:
    // going named leaves the numbers where they were, so coming back finds them
    // rather than rebuilding them, and any editing done while named wins.
    if ( namedCellsMode )
    {
        if ( mNamedAnimationFrames.size() > 0 || mAnimationFrames.size() == 0 )
            return;
    }
    else
    {
        if ( mAnimationFrames.size() > 0 || mNamedAnimationFrames.size() == 0 )
            return;
    }

    // What each cell is called, by index. Read from the explicit cells rather than
    // the resolved frames, because this has to work while explicit mode is OFF --
    // which is exactly the case that translates names back into indices.
    Vector<StringTableEntry> cellNames;
    const S32 cellCount = mImageAsset->getExplicitCellCount();
    for ( S32 cellIndex = 0; cellIndex < cellCount; ++cellIndex )
    {
        cellNames.push_back( mImageAsset->getExplicitCellName( cellIndex ) );
    }

    if ( namedCellsMode )
    {
        translateFrames( mAnimationFrames, cellNames, mNamedAnimationFrames );
    }
    else
    {
        translateFrames( mNamedAnimationFrames, cellNames, mAnimationFrames );
    }
}

//------------------------------------------------------------------------------

void AnimationAsset::getMissingFrames( Vector<StringTableEntry>& missingFrames ) const
{
    missingFrames.clear();

    if ( !getNamedCellsMode() )
        return;

    for( Vector<StringTableEntry>::const_iterator frameItr = mNamedAnimationFrames.begin(); frameItr != mNamedAnimationFrames.end(); ++frameItr )
    {
        if ( !mImageAsset->containsFrame( *frameItr ) )
            missingFrames.push_back( *frameItr );
    }
}

//------------------------------------------------------------------------------

S32 AnimationAsset::getFrameCount( const bool validatedFrames ) const
{
    if ( getNamedCellsMode() )
        return validatedFrames ? mValidatedNameFrames.size() : mNamedAnimationFrames.size();

    return validatedFrames ? mValidatedFrames.size() : mAnimationFrames.size();
}

//------------------------------------------------------------------------------

void AnimationAsset::validateNumericalFrames( void )
{
    // Clear validated frames.
    mValidatedFrames.clear();

    // Fetch Animation Frame Count.
    const U32 animationFrameCount = (U32)mAnimationFrames.size();

    // Finish if no animation frames are specified.
    if ( animationFrameCount == 0 )
        return;

    // Fetch image asset frame count.
    const S32 imageAssetFrameCount = (S32)mImageAsset->getFrameCount();

    // Finish if the image has no frames.
    if ( imageAssetFrameCount == 0 )
        return;

    // Validate each specified frame.
    for ( U32 frameIndex = 0; frameIndex < animationFrameCount; ++frameIndex )
    {
        // Fetch frame.
        S32 frame = mAnimationFrames[frameIndex];

        // Valid Frame?
        if ( frame < 0 || frame >= imageAssetFrameCount )
        {
            // No, warn.
            Con::warnf( "AnimationAsset::validateFrames() - Animation asset '%s' specifies an out-of-bound frame of '%d' (key-index:'%d') against image asset Id '%s'.", 
                getAssetName(),
                frame,
                frameIndex,
                mImageAsset.getAssetId() );

            // Set the frame to a valid one.
            if ( frame < 0 )
                frame = 0;
            else if ( frame >= imageAssetFrameCount )
                frame = imageAssetFrameCount-1;
        }

        // Use frame.
        mValidatedFrames.push_back( frame );
    }
}

//------------------------------------------------------------------------------

void AnimationAsset::validateNamedFrames( void )
{
    mValidatedNameFrames.clear();

    // Fetch Animation Frame Count.
    const U32 animationFrameCount = (U32)mNamedAnimationFrames.size();

    // Finish if no animation frames are specified.
    if ( animationFrameCount == 0 )
        return;

    // Fetch image asset frame count.
    const S32 imageAssetFrameCount = (S32)mImageAsset->getFrameCount();

    // Finish if the image has no frames.
    if ( imageAssetFrameCount == 0 )
        return;

    // Validate each specified frame.
    for ( U32 frameIndex = 0; frameIndex < animationFrameCount; ++frameIndex )
    {
        // Fetch frame.
        const char* frame = mNamedAnimationFrames[frameIndex];

        // Valid Frame?
        if ( frame ==  StringTable->EmptyString || !mImageAsset->containsFrame(frame) )
        {
            // No, warn.
            Con::warnf( "AnimationAsset::validateNamedFrames() - Animation asset '%s' specifies a bad frame '%s' against image asset Id '%s'.",
                getAssetName(),
                frame,
                mImageAsset.getAssetId() );
        }

        // Use frame.
        mValidatedNameFrames.push_back( StringTable->insert(frame) );
    }
}

//------------------------------------------------------------------------------

void AnimationAsset::validateFrames( void )
{
    // Debug Profiling.
    PROFILE_SCOPE(AnimationAsset_ValidateFrames);

    // Finish if we don't have a valid image asset.
    if ( mImageAsset.isNull() )
        return;

    // Only the list in use, and nothing else. This is a pure derivation -- it must
    // not touch either specified list, because it runs from inside both setters
    // and would otherwise be undoing the write that called it.
    if (getNamedCellsMode())
    {
        validateNamedFrames();
    }
    else
    {
        validateNumericalFrames();
    }
}

//------------------------------------------------------------------------------

bool AnimationAsset::isAssetValid( void ) const
{
    return mImageAsset.notNull() && mImageAsset->isAssetValid() && (mValidatedFrames.size() > 0 || mValidatedNameFrames.size() > 0);
}

//------------------------------------------------------------------------------

void AnimationAsset::initializeAsset( void )
{
    // Call parent.
    Parent::initializeAsset();

    // Settle the frames once, now that the whole file has been read.
    //
    // Until now this relied on each field validating as TAML applied it, so
    // whichever of Image and the two frame lists happened to be written last got
    // the final say. That was survivable while nothing depended on more than one
    // field at a time. Converting between the two spaces depends on all three, so
    // it has to happen where all three are known to be in.
    convertFramesForMode();
    validateFrames();
}

