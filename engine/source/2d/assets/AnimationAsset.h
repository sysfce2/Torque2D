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

#ifndef _ANIMATION_ASSET_H_
#define _ANIMATION_ASSET_H_

#ifndef _ASSET_PTR_H_
#include "assets/assetPtr.h"
#endif

//-----------------------------------------------------------------------------

DefineConsoleType( TypeAnimationAssetPtr )

//-----------------------------------------------------------------------------

class ImageAsset;

//-----------------------------------------------------------------------------

class AnimationAsset : public AssetBase
{
private:
    typedef AssetBase  Parent;

    AssetPtr<ImageAsset>     mImageAsset;
    Vector<S32>              mAnimationFrames;
    Vector<StringTableEntry> mNamedAnimationFrames;
    Vector<S32>              mValidatedFrames;
    Vector<StringTableEntry> mValidatedNameFrames;
    F32                      mAnimationTime;
    bool                     mAnimationCycle;
    bool                     mRandomStart;

public:
    AnimationAsset();
    virtual ~AnimationAsset();

    static void initPersistFields();
    virtual bool onAdd();
    virtual void onRemove();

    void            setImage( const char* pAssetId );
    inline const AssetPtr<ImageAsset>& getImage( void ) const           { return mImageAsset; }

    void            setAnimationFrames( const char* pAnimationFrames );
    inline const Vector<S32>& getSpecifiedAnimationFrames( void ) const { return mAnimationFrames; }
    inline const Vector<S32>& getValidatedAnimationFrames( void ) const { return mValidatedFrames; }

    void            setNamedAnimationFrames( const char* pAnimationFrames );
    inline const Vector<StringTableEntry>& getSpecifiedNamedAnimationFrames( void ) const { return mNamedAnimationFrames; }
    inline const Vector<StringTableEntry>& getValidatedNamedAnimationFrames( void ) const { return mValidatedNameFrames; }

    void            setAnimationTime( const F32 animationTime );
    inline F32      getAnimationTime( void ) const                      { return mAnimationTime; }
    void            setAnimationCycle( const bool animationCycle );
    inline bool     getAnimationCycle( void ) const                     { return mAnimationCycle; }
    void            setRandomStart( const bool randomStart );
    inline bool     getRandomStart( void ) const                        { return mRandomStart; }

    /// Whether this animation addresses its frames by name rather than by index.
    ///
    /// Asked of the image, not stored. An image in explicit mode cuts itself into
    /// named cells and one in cell mode does not, so the image already holds the
    /// only honest answer -- and a copy of it here could disagree with the image
    /// it was copied from the moment that image was re-cut. It was a saved field
    /// once, which meant a person could set it to true on an image that had no
    /// names, and the animation had no frames and no explanation.
    bool            getNamedCellsMode( void ) const;

    // Frame validation.
    void            validateFrames( void );
    void            validateNumericalFrames( void );
    void            validateNamedFrames( void );

    /// Put the frame list into the space the image now uses.
    ///
    /// Deliberately NOT called from the two frame setters, only from the three
    /// places that mean "something outside changed". Setting an empty frame list
    /// is a thing the editor does whenever the timeline is emptied, and something
    /// copyFieldsFrom does on every single copy -- and from inside a setter this
    /// could not tell that from "this list was never filled in", so it put the
    /// frames the user had just cleared straight back.
    void            convertFramesForMode( void );

    /// The specified names that no cell answers to. Empty when all of them do.
    void            getMissingFrames( Vector<StringTableEntry>& missingFrames ) const;

    /// How many frames the animation has, in whichever space it is using.
    S32             getFrameCount( const bool validatedFrames ) const;

    /// Translate a frame list from one space to the other against a table of cell
    /// names, where entry N is what cell N is called.
    ///
    /// Statics taking their whole world rather than methods reaching for the image
    /// asset, so they can be unit tested -- building a real ImageAsset with cells
    /// needs a bitmap, and a unit test has no GL context to load one into.
    ///
    /// An entry that does not resolve is skipped. There is no honest name for an
    /// index with no cell, and inventing one risks colliding with a cell somebody
    /// really does name that later.
    static void     translateFrames( const Vector<S32>& indices, const Vector<StringTableEntry>& cellNames, Vector<StringTableEntry>& outNames );
    static void     translateFrames( const Vector<StringTableEntry>& names, const Vector<StringTableEntry>& cellNames, Vector<S32>& outIndices );

    // Asset validation.
    virtual bool    isAssetValid( void ) const;

    /// Declare Console Object.
    DECLARE_CONOBJECT(AnimationAsset);

protected:
    virtual void initializeAsset( void );
    virtual void onAssetRefresh( void );

protected:
    static bool setImage( void* obj, const char* data )                             { static_cast<AnimationAsset*>(obj)->setImage( data ); return false; }
    static bool writeImage( void* obj, StringTableEntry pFieldName )                { return static_cast<AnimationAsset*>(obj)->mImageAsset.notNull(); }
    static bool setAnimationFrames( void* obj, const char* data )                   { static_cast<AnimationAsset*>(obj)->setAnimationFrames( data ); return false; }
    static bool setNamedAnimationFrames( void* obj, const char* data )              { static_cast<AnimationAsset*>(obj)->setNamedAnimationFrames( data ); return false; }

    // Only the list the animation is actually using is written.
    //
    // Both lists are kept in memory, which is what lets an image change mode and
    // change back without the animation losing anything. Writing both was a
    // round trip that did not close: the named list is applied last and used to
    // force named mode on, so an animation given numbered frames after ever
    // having had named ones came back from its own file named.
    static bool writeAnimationFrames( void* obj, StringTableEntry pFieldName )      { AnimationAsset* pAsset = static_cast<AnimationAsset*>(obj); return !pAsset->getNamedCellsMode() && pAsset->mAnimationFrames.size() > 0; }
    static bool writeNamedAnimationFrames( void* obj, StringTableEntry pFieldName ) { AnimationAsset* pAsset = static_cast<AnimationAsset*>(obj); return  pAsset->getNamedCellsMode() && pAsset->mNamedAnimationFrames.size() > 0; }
    static bool setAnimationTime( void* obj, const char* data )                     { static_cast<AnimationAsset*>(obj)->setAnimationTime( dAtof(data) ); return false; }
    static bool setAnimationCycle( void* obj, const char* data )                    { static_cast<AnimationAsset*>(obj)->setAnimationCycle( dAtob(data) ); return false; }
    static bool writeAnimationCycle( void* obj, StringTableEntry pFieldName )       { return static_cast<AnimationAsset*>(obj)->getAnimationCycle() == false; }
    static bool setRandomStart( void* obj, const char* data )                       { static_cast<AnimationAsset*>(obj)->setRandomStart( dAtob(data) ); return false; }
    static bool writeRandomStart( void* obj, StringTableEntry pFieldName )          { return static_cast<AnimationAsset*>(obj)->getRandomStart() == true; }
};

#endif // _ANIMATION_ASSET_H_