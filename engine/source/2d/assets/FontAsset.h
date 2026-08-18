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

#ifndef _FONT_ASSET_H_
#define _FONT_ASSET_H_

#ifndef _ASSET_PTR_H_
#include "assets/assetPtr.h"
#endif

#ifndef _IMAGE_ASSET_H_
#include "2d/assets/ImageAsset.h"
#endif

#ifndef _BITMAP_FONT_H_
#include "bitmapFont/BitmapFont.h"
#endif

//-----------------------------------------------------------------------------

DefineConsoleType(TypeFontAssetPtr)

//-----------------------------------------------------------------------------

using namespace font;

class FontAsset : public AssetBase
{
private:
    typedef AssetBase Parent;

public:
    StringTableEntry                mFontFile;
    AssetPtr<ImageAsset>            mImageAsset;
    BitmapFont                      mBitmapFont;

public:
    FontAsset();
    virtual ~FontAsset();

    /// Core.
    static void initPersistFields();
    virtual bool onAdd();
    virtual void onRemove();

    void                    setFontFile( const char* pFontFile );
    inline StringTableEntry getFontFile( void ) const                   { return mFontFile; }

    /// The font file as it is stored on disk -- relative to the folder the asset
    /// itself lives in. getFontFile answers the expanded absolute path, which is
    /// what the engine needs and is neither readable nor portable anywhere else.
    inline StringTableEntry getRelativeFontFile( void ) const           { return collapseAssetFilePath(mFontFile); }

    inline TextureHandle&   getImageTexture(U16 pageID)                         { return mBitmapFont.mTexture[pageID]; }

    /// What the .fnt turned out to hold.
    ///
    /// None of this is a value the asset stores -- it is the result of the last
    /// parse, so it can only be asked, never told. An editor needs it to say
    /// whether a font loaded and what came out of it; before this there was no way
    /// to find out from script at all.
    inline U32 getGlyphCount( void ) const          { return mBitmapFont.getCharacterCount(); }
    inline U32 getPageCount( void ) const           { return (U32)mBitmapFont.mPageName.size(); }
    inline U32 getFontSize( void ) const            { return (U32)mBitmapFont.mSize; }
    inline U32 getLineHeight( void ) const          { return (U32)mBitmapFont.mLineHeight; }
    inline U32 getBaseline( void ) const            { return (U32)mBitmapFont.mBaseline; }

    /// How many of the declared pages actually have a texture. A page is named
    /// inside the .fnt rather than in the asset file, so a missing page image is
    /// invisible from the asset and this is the only way to notice it.
    inline U32 getLoadedPageCount( void ) const
    {
        U32 loaded = 0;
        for ( U32 index = 0; index < (U32)mBitmapFont.mTexture.size(); ++index )
        {
            if ( mBitmapFont.mTexture[index].NotNull() )
                loaded++;
        }
        return loaded;
    }

    inline StringTableEntry getPageFile( const U32 pageIndex ) const
    {
        return ( pageIndex < (U32)mBitmapFont.mPageName.size() )
            ? mBitmapFont.mPageName[pageIndex] : StringTable->EmptyString;
    }

    inline U32 getPageWidth( const U32 pageIndex ) const
    {
        return ( pageIndex < (U32)mBitmapFont.mTexture.size() && mBitmapFont.mTexture[pageIndex].NotNull() )
            ? mBitmapFont.mTexture[pageIndex].getWidth() : 0;
    }

    inline U32 getPageHeight( const U32 pageIndex ) const
    {
        return ( pageIndex < (U32)mBitmapFont.mTexture.size() && mBitmapFont.mTexture[pageIndex].NotNull() )
            ? mBitmapFont.mTexture[pageIndex].getHeight() : 0;
    }

    /// Declare Console Object.
    DECLARE_CONOBJECT(FontAsset);

private:
    void buildFontData( void );

protected:
    virtual void initializeAsset( void );
    virtual void onAssetRefresh( void );

    /// Taml callbacks.
    virtual void onTamlPreWrite( void );
    virtual void onTamlPostWrite( void );
    virtual void onTamlCustomWrite( TamlCustomNodes& customNodes );
    virtual void onTamlCustomRead( const TamlCustomNodes& customNodes );


protected:
    static bool setFontFile( void* obj, const char* data )              { static_cast<FontAsset*>(obj)->setFontFile(data); return false; }
    static bool writeFontFile( void* obj, StringTableEntry pFieldName ) { return static_cast<FontAsset*>(obj)->getFontFile() != StringTable->EmptyString; }
};

#endif // _FONT_ASSET_H_
