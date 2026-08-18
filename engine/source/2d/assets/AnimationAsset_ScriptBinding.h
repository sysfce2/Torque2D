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

ConsoleMethodGroupBeginWithDocs(AnimationAsset, AssetBase)

/*! Sets the image asset Id.
    @return No return value.
*/
ConsoleMethodWithDocs(AnimationAsset, setImage, ConsoleVoid, 3, 3, (assetId))
{
    object->setImage( argv[2] );
}

//-----------------------------------------------------------------------------

/*! Gets the image asset Id.
    @return The image asset Id.
*/
ConsoleMethodWithDocs(AnimationAsset, getImage, ConsoleString, 2, 2, ())
{
    return object->getImage().getAssetId();
}

//-----------------------------------------------------------------------------

/*! Sets the image frames that compose the animation.
    @param animationFrames A set of image frames that compose the animation.
    @return No return value.
*/
ConsoleMethodWithDocs(AnimationAsset, setAnimationFrames, ConsoleVoid, 3, 3, (animationFrames))
{
    // Are we in named cells mode?
    if ( object->getNamedCellsMode() )
    {
        // Yes, so warn.
        Con::warnf( "AnimationAsset::setAnimationFrames() - Method invalid, in named cells mode." );
        return;
    }

    object->setAnimationFrames( argv[2] );
}

//-----------------------------------------------------------------------------

/*! Gets the frames that compose the animation or optionally only the ones validated against the image asset.
    @param validatedFrames - Whether to return only the validated frames or not.  Optional: Default is false.
    @return The image frames that compose the animation or optionally only the ones validated against the image asset.
*/
ConsoleMethodWithDocs(AnimationAsset, getAnimationFrames, ConsoleString, 2, 3, ([bool validatedFrames]))
{
    // Are we in named cells mode?
    if ( object->getNamedCellsMode() )
    {
        // Yes, so warn.
        Con::warnf( "AnimationAsset::getAnimationFrames() - Method invalid, in named cells mode." );
        return StringTable->EmptyString;
    }

    // Fetch a return buffer.
    S32 bufferSize = 4096;
    char* pBuffer = Con::getReturnBuffer( bufferSize );
    char* pReturnBuffer = pBuffer;    

    // Fetch validated frames flag.
    const bool validatedFrames = argc >= 3 ? dAtob( argv[2] ) : false;

    // Fetch specified frames.
    const Vector<S32>& frames = validatedFrames ? object->getValidatedAnimationFrames() : object->getSpecifiedAnimationFrames();

    // Fetch frame count.
    const U32 frameCount = (U32)frames.size();

    // Format frames.
    for ( U32 frameIndex = 0; frameIndex < frameCount; ++frameIndex )
    {
        const S32 offset = dSprintf( pBuffer, bufferSize, "%d ", frames[frameIndex] );
        pBuffer += offset;
        bufferSize -= offset;
    }

    // Return frames.
    return pReturnBuffer;
}

//-----------------------------------------------------------------------------

/*! Gets the count of frame that compose the animation or optionally only the ones validated against the image asset.
    @param validatedFrames - Whether to return only the validated frames or not.  Optional: Default is false.
    @return The image frames that compose the animation or optionally only the ones validated against the image asset.
*/
ConsoleMethodWithDocs(AnimationAsset, getAnimationFrameCount, ConsoleInt, 2, 3, ([bool validatedFrames]))
{
    // Are we in named cells mode?
    if ( object->getNamedCellsMode() )
    {
        // Yes, so warn.
        Con::warnf( "AnimationAsset::getAnimationFrameCount() - Method invalid, in named cells mode." );
        return -1;
    }

    // Fetch validated frames flag.
    const bool validatedFrames = argc >= 3 ? dAtob( argv[2] ) : false;

    // Fetch specified frames.
    const Vector<S32>& frames = validatedFrames ? object->getValidatedAnimationFrames() : object->getSpecifiedAnimationFrames();

    return frames.size();
}

//-----------------------------------------------------------------------------

/*! Sets the named image frames that compose the animation.
    @param animationFrames A set of named image frames that compose the animation.
    @return No return value.
*/
ConsoleMethodWithDocs(AnimationAsset, setNamedAnimationFrames, ConsoleVoid, 3, 3, (animationFrames))
{
    // Are we in named cells mode?
    if ( !object->getNamedCellsMode() )
    {
        // No, so warn.
        Con::warnf( "AnimationAsset::setNamedAnimationFrames() - Method invalid, not in named cells mode." );
        return;
    }

    object->setNamedAnimationFrames( argv[2] );
}

//-----------------------------------------------------------------------------

/*! Gets the named frames that compose the animation or optionally only the ones validated against the image asset.
    @param validatedFrames - Whether to return only the validated frames or not.  Optional: Default is false.
    @return The named image frames that compose the animation or optionally only the ones validated against the image asset.
*/
ConsoleMethodWithDocs(AnimationAsset, getNamedAnimationFrames, ConsoleString, 2, 3, ([bool validatedFrames]))
{
    // Are we in named cells mode?
    if ( !object->getNamedCellsMode() )
    {
        // No, so warn.
        Con::warnf( "AnimationAsset::getNamedAnimationFrames() - Method invalid, not in named cells mode." );
        return StringTable->EmptyString;
    }

    // Fetch validated frames flag.
    const bool validatedFrames = argc >= 3 ? dAtob( argv[2] ) : false;

    // Fetch specified frames.
    const Vector<StringTableEntry>& frames = validatedFrames ? object->getValidatedNamedAnimationFrames() : object->getSpecifiedNamedAnimationFrames();

    // Fetch frame count.
    const U32 frameCount = (U32)frames.size();

    // Measured rather than assumed, unlike the numbered sibling above.
    //
    // A cell name comes from a TAML attribute and has no length limit, so the
    // fixed 4096 that serves a list of integers can genuinely be too small here
    // -- and dSprintf truncates in silence, which would have shortened a long
    // animation to whatever fitted and told nobody.
    U32 bufferLength = 1;
    for ( U32 frameIndex = 0; frameIndex < frameCount; ++frameIndex )
    {
        bufferLength += dStrlen( frames[frameIndex] ) + 1;
    }

    S32 bufferSize = (S32)bufferLength;
    char* pBuffer = Con::getReturnBuffer( bufferSize );
    char* pReturnBuffer = pBuffer;
    *pBuffer = 0;

    // Format frames.
    //
    // As "%s". These are StringTableEntry -- const char* -- and formatting one
    // through "%d" printed the pointer, so every named animation read back as a
    // list of addresses. That single character is why the Asset Manager could
    // not edit a named animation at all: nothing downstream could recover the
    // names it had just asked for.
    for ( U32 frameIndex = 0; frameIndex < frameCount; ++frameIndex )
    {
        const S32 offset = dSprintf( pBuffer, bufferSize, "%s ", frames[frameIndex] );
        pBuffer += offset;
        bufferSize -= offset;
    }

    // Return frames.
    return pReturnBuffer;
}

//-----------------------------------------------------------------------------

/*! Gets the count of named frames that compose the animation or optionally only the ones validated against the image asset.
    @param validatedFrames - Whether to return only the validated frames or not.  Optional: Default is false.
    @return The named image frames that compose the animation or optionally only the ones validated against the image asset.
*/
ConsoleMethodWithDocs(AnimationAsset, getNamedAnimationFrameCount, ConsoleInt, 2, 3, ([bool validatedFrames]))
{
    // Are we in named cells mode?
    if ( !object->getNamedCellsMode() )
    {
        // No, so warn.
        Con::warnf( "AnimationAsset::getNamedAnimationFrameCount() - Method invalid, not in named cells mode." );
        return -1;
    }

    // Fetch validated frames flag.
    const bool validatedFrames = argc >= 3 ? dAtob( argv[2] ) : false;

    // Fetch specified frames.
    const Vector<StringTableEntry>& frames = validatedFrames ? object->getValidatedNamedAnimationFrames() : object->getSpecifiedNamedAnimationFrames();

    return frames.size();
}

//-----------------------------------------------------------------------------

/*! Sets the total time to cycle through all animation frames.
    @param animationTime The total time to cycle through all animation frames.
    @return No return value.
*/
ConsoleMethodWithDocs(AnimationAsset, setAnimationTime, ConsoleVoid, 3, 3, (float animationTime))
{
    object->setAnimationTime( dAtof(argv[2] ) );
}

//-----------------------------------------------------------------------------

/*! Gets the total time to cycle through all animation frames.
    @return The total time to cycle through all animation frames.
*/
ConsoleMethodWithDocs(AnimationAsset, getAnimationTime, ConsoleFloat, 2, 2, ())
{
    return object->getAnimationTime();
}


//-----------------------------------------------------------------------------

/*! Sets whether the animation cycles or not.
    @param animationCycle Whether the animation cycles or not.
    @return No return value.
*/
ConsoleMethodWithDocs(AnimationAsset, setAnimationCycle, ConsoleVoid, 3, 3, (bool animationCycle))
{
    object->setAnimationCycle( dAtob(argv[2] ) );
}

//-----------------------------------------------------------------------------

/*! Gets whether the animation cycles or not.
    @return Whether the animation cycles or not.
*/
ConsoleMethodWithDocs(AnimationAsset, getAnimationCycle, ConsoleBool, 2, 2, ())
{
    return object->getAnimationCycle();
}

//-----------------------------------------------------------------------------

/*! Gets whether the animation is using names for its cells.

    This is not a setting.  It is read from the image asset: an image in explicit
    mode cuts itself into named cells, so an animation on it addresses them by
    name, and an image cut into a grid has no names to address.  Change the image,
    or change that image's explicit mode, to change this.
    @return True if the animation is using named cells.
*/
ConsoleMethodWithDocs(AnimationAsset, getNamedCellsMode, ConsoleBool, 2, 2, ())
{
    return object->getNamedCellsMode();
}

//-----------------------------------------------------------------------------

/*! Gets the count of frames that compose the animation, whether it is using named
    cells or numbered ones.
    @param validatedFrames - Whether to count only the validated frames or not.  Optional: Default is false.
    @return The number of frames that compose the animation.
*/
ConsoleMethodWithDocs(AnimationAsset, getFrameCount, ConsoleInt, 2, 3, ([bool validatedFrames]))
{
    // Fetch validated frames flag.
    const bool validatedFrames = argc >= 3 ? dAtob( argv[2] ) : false;

    return object->getFrameCount( validatedFrames );
}

//-----------------------------------------------------------------------------

/*! Gets the named frames that no cell of the image answers to.

    Empty when every frame resolves, and empty for an animation using numbered
    frames, which cannot have this problem -- an out-of-range number is clamped.
    @return The space separated names of the frames that cannot be found.
*/
ConsoleMethodWithDocs(AnimationAsset, getMissingFrames, ConsoleString, 2, 2, ())
{
    Vector<StringTableEntry> missingFrames;
    object->getMissingFrames( missingFrames );

    // Fetch frame count.
    const U32 frameCount = (U32)missingFrames.size();

    if ( frameCount == 0 )
        return StringTable->EmptyString;

    // Measured, because a cell name has no length limit.
    U32 bufferLength = 1;
    for ( U32 frameIndex = 0; frameIndex < frameCount; ++frameIndex )
    {
        bufferLength += dStrlen( missingFrames[frameIndex] ) + 1;
    }

    S32 bufferSize = (S32)bufferLength;
    char* pBuffer = Con::getReturnBuffer( bufferSize );
    char* pReturnBuffer = pBuffer;
    *pBuffer = 0;

    for ( U32 frameIndex = 0; frameIndex < frameCount; ++frameIndex )
    {
        const S32 offset = dSprintf( pBuffer, bufferSize, "%s ", missingFrames[frameIndex] );
        pBuffer += offset;
        bufferSize -= offset;
    }

    return pReturnBuffer;
}

ConsoleMethodGroupEndWithDocs(AnimationAsset)
