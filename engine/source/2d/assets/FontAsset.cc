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

#ifndef _CONSOLE_H_
#include "console/console.h"
#endif

#ifndef _CONSOLEINTERNAL_H_
#include "console/consoleInternal.h"
#endif

#ifndef _GBITMAP_H_
#include "graphics/gBitmap.h"
#endif

#ifndef _UTILITY_H_
#include "2d/core/Utility.h"
#endif

#ifndef _SCENE_OBJECT_H_
#include "2d/sceneobject/SceneObject.h"
#endif

#ifndef _FONT_ASSET_H_
#include "2d/assets/FontAsset.h"
#endif

// Script bindings.
#include "FontAsset_ScriptBinding.h"

//------------------------------------------------------------------------------

IMPLEMENT_CONOBJECT(FontAsset);

//------------------------------------------------------------------------------

ConsoleType( FontAssetPtr, TypeFontAssetPtr, sizeof(AssetPtr<FontAsset>), ASSET_ID_FIELD_PREFIX )

//-----------------------------------------------------------------------------

ConsoleGetType( TypeFontAssetPtr )
{
    // Fetch asset Id.
    return (*((AssetPtr<FontAsset>*)dptr)).getAssetId();
}

//-----------------------------------------------------------------------------

ConsoleSetType( TypeFontAssetPtr )
{
    // Was a single argument specified?
    if( argc == 1 )
    {
        // Yes, so fetch field value.
        const char* pFieldValue = argv[0];

        // Fetch asset pointer.
        AssetPtr<FontAsset>* pAssetPtr = dynamic_cast<AssetPtr<FontAsset>*>((AssetPtrBase*)(dptr));

        // Is the asset pointer the correct type?
        if (pAssetPtr == NULL )
        {
            // No, so fail.
            Con::warnf( "(TypeFontAssetPtr) - Failed to set asset Id '%d'.", pFieldValue );
            return;
        }

        // Set asset.
        pAssetPtr->setAssetId( pFieldValue );

        return;
   }

    // Warn.
    Con::warnf( "(TypeFontAssetPtr) - Cannot set multiple args to a single asset." );
}


//------------------------------------------------------------------------------

FontAsset::FontAsset() :    mFontFile(StringTable->EmptyString),
                                    mBitmapFont()
{
}

//------------------------------------------------------------------------------

FontAsset::~FontAsset()
{
    
}

//------------------------------------------------------------------------------

void FontAsset::initPersistFields()
{
    // Call parent.
    Parent::initPersistFields();

    // Fields.
    addProtectedField("FontFile", TypeAssetLooseFilePath, Offset(mFontFile, FontAsset), &setFontFile, &defaultProtectedGetFn, &writeFontFile, "The loose file pointing to a .fnt file");
}

//------------------------------------------------------------------------------

bool FontAsset::onAdd()
{
    // Call Parent.
    if (!Parent::onAdd())
       return false;

    return true;
}

//------------------------------------------------------------------------------

void FontAsset::onRemove()
{
    // Call Parent.
    Parent::onRemove();
}

//------------------------------------------------------------------------------

void FontAsset::setFontFile( const char* pFontFile )
{
    // Sanity!
    AssertFatal( pFontFile != NULL, "Cannot use a NULL Font file." );

    // Fetch Font file.
    pFontFile = StringTable->insert( pFontFile );

    // Ignore no change.
    if (pFontFile == mFontFile )
        return;

    // Update.
    mFontFile = getOwned() ? expandAssetFilePath( pFontFile ) : StringTable->insert( pFontFile );

    // Refresh the asset.
    refreshAsset();
}

//------------------------------------------------------------------------------

void FontAsset::initializeAsset( void )
{
    // Call parent.
    Parent::initializeAsset();

    // Ensure the Font file is expanded.
    mFontFile = expandAssetFilePath( mFontFile );

    // Build the Font data
    buildFontData();
}

//------------------------------------------------------------------------------

void FontAsset::onAssetRefresh( void )
{
    // Ignore if not yet added to the sim.
    if (!isProperlyAdded() )
        return;

    // Call parent.
    Parent::onAssetRefresh();

    buildFontData();
}

//-----------------------------------------------------------------------------

void FontAsset::buildFontData( void )
{
   // Before the open, not after it.
   //
   // This runs again every time the font file changes, and what it used to clear
   // was the page list and the textures -- never the glyphs or the kerning, which
   // are maps that parseFont adds to. So pointing an asset at a second .fnt left
   // the union of both fonts and a glyph count that only ever grew. And on a file
   // that would not open it cleared nothing at all and returned, so a broken path
   // kept the font it used to have and there was no way to tell from the outside
   // that anything was wrong.
   mBitmapFont.clear();

   FileStream fStream;

   if (!fStream.open(mFontFile, FileStream::Read))
   {
      Con::printf("Failed to open file '%s'.", expandAssetFilePath(mFontFile));
      return;
   }

   mBitmapFont.parseFont(fStream);

   fStream.close();

   //load the images
   for (auto iter = mBitmapFont.mPageName.begin(); iter != mBitmapFont.mPageName.end(); iter++)
   {
      mBitmapFont.mTexture.push_back(mBitmapFont.LoadTexture(expandAssetFilePath(*iter)));
   }
}

//-----------------------------------------------------------------------------

void FontAsset::onTamlPreWrite( void )
{
    // Call parent.
    Parent::onTamlPreWrite();

    // Ensure the Font file is collapsed.
    mFontFile = collapseAssetFilePath( mFontFile );
}

//-----------------------------------------------------------------------------

void FontAsset::onTamlPostWrite( void )
{
    // Call parent.
    Parent::onTamlPostWrite();

    // Ensure the Font file is expanded.
    mFontFile = expandAssetFilePath( mFontFile );
}

//------------------------------------------------------------------------------

void FontAsset::onTamlCustomWrite( TamlCustomNodes& customNodes )
{
    // Debug Profiling.
    PROFILE_SCOPE(FontAsset_OnTamlCustomWrite);

    // Call parent.
    Parent::onTamlCustomWrite( customNodes );
}

//-----------------------------------------------------------------------------

void FontAsset::onTamlCustomRead( const TamlCustomNodes& customNodes )
{
    // Debug Profiling.
    PROFILE_SCOPE(FontAsset_OnTamlCustomRead);

    // Call parent.
    Parent::onTamlCustomRead( customNodes );
}
