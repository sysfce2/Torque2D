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

#ifndef _PLATFORM_H_
#include "platform/platform.h"
#endif

#ifndef _FILESTREAM_H_
#include "io/fileStream.h"
#endif

#ifndef _BITMAP_FONT_H_
#include "bitmapFont/BitmapFont.h"
#endif

//-----------------------------------------------------------------------------
// What a .fnt file turns into.
//
// This is the half of the font path a unit test can reach. parseFont touches
// only the console, the string table and the stream, so it runs fine here --
// but FontAsset::buildFontData goes on to call LoadTexture for each page, and
// that is TextureManager, which needs a GL context the test harness does not
// have. So everything below drives BitmapFont directly and nothing here ever
// makes a FontAsset. (FontAsset's FIELDS are covered by assetStateCopyTests,
// which works because an unowned asset never reaches initializeAsset and so
// never builds its font data either.)
//
// The fixtures are the three fonts shipped in ToyAssets. Their numbers are read
// out of the files rather than computed, so a test failing here means either
// the parser changed or somebody re-generated the art.
//-----------------------------------------------------------------------------

#define ARIAL_FNT           "toybox/ToyAssets/1/assets/fonts/Arial.fnt"
#define ORATOR_FNT          "toybox/ToyAssets/1/assets/fonts/Orator Bold.fnt"

//-----------------------------------------------------------------------------

static bool parseFontFile( font::BitmapFont& bitmapFont, const char* pPath )
{
    FileStream stream;

    if ( !stream.open( pPath, FileStream::Read ) )
        return false;

    bitmapFont.parseFont( stream );
    stream.close();

    return true;
}

//-----------------------------------------------------------------------------
// A font nobody has read a file into yet answers zero for everything.
//
// It used to answer whatever was on the heap: the constructor had an empty body
// and not one of the six scalars was initialized. Nothing noticed while the only
// reader was TextSprite, which draws nothing either way -- but mWidth and mHeight
// are the divisors ProcessCharacter uses to turn glyph rects into texture
// coordinates, and an editor puts the rest on screen.
//-----------------------------------------------------------------------------
TEST( BitmapFontParseTests, DefaultConstructedFontIsEmpty )
{
    font::BitmapFont bitmapFont;

    ASSERT_EQ( bitmapFont.mSize, 0 ) << "A font with no file read into it must report no size.";
    ASSERT_EQ( bitmapFont.mLineHeight, 0 ) << "A font with no file read into it must report no line height.";
    ASSERT_EQ( bitmapFont.mBaseline, 0 ) << "A font with no file read into it must report no baseline.";
    ASSERT_EQ( bitmapFont.getCharacterCount(), 0U ) << "A font with no file read into it must hold no glyphs.";
    ASSERT_EQ( bitmapFont.mPageName.size(), 0U ) << "A font with no file read into it must declare no pages.";
    ASSERT_EQ( bitmapFont.mTexture.size(), 0U ) << "A font with no file read into it must hold no textures.";
}

//-----------------------------------------------------------------------------
// The whole of the header, against a file whose contents are known.
//-----------------------------------------------------------------------------
TEST( BitmapFontParseTests, ParsesTheArialFixture )
{
    font::BitmapFont bitmapFont;

    ASSERT_TRUE( parseFontFile( bitmapFont, ARIAL_FNT ) )
        << "Could not open " ARIAL_FNT " -- unit tests run from the repository root.";

    // info face="Arial" size=128
    ASSERT_EQ( bitmapFont.mSize, 128 ) << "Wrong native size.";

    // common lineHeight=128 base=103 scaleW=512 scaleH=512 pages=2
    ASSERT_EQ( bitmapFont.mLineHeight, 128 ) << "Wrong line height.";
    ASSERT_EQ( bitmapFont.mBaseline, 103 ) << "Wrong baseline.";

    // Two page lines, and the names are read out from between the quotes.
    ASSERT_EQ( bitmapFont.mPageName.size(), 2U ) << "Wrong page count.";
    ASSERT_STREQ( bitmapFont.mPageName[0], "Arial_0.png" ) << "Wrong first page file.";
    ASSERT_STREQ( bitmapFont.mPageName[1], "Arial_1.png" ) << "Wrong second page file.";

    // 97 char lines, all with distinct ids.
    ASSERT_EQ( bitmapFont.getCharacterCount(), 97U ) << "Wrong glyph count.";

    // A glyph that is definitely in the file, to prove the char lines were read
    // and not merely counted. 'A' is 65.
    ASSERT_GT( bitmapFont.getCharacter( 65 ).mXAdvance, 0.0f ) << "'A' has no advance.";
}

//-----------------------------------------------------------------------------
// clear() puts a font back to how it started.
//-----------------------------------------------------------------------------
TEST( BitmapFontParseTests, ClearForgetsTheFile )
{
    font::BitmapFont bitmapFont;

    ASSERT_TRUE( parseFontFile( bitmapFont, ARIAL_FNT ) ) << "Could not open " ARIAL_FNT ".";
    ASSERT_EQ( bitmapFont.getCharacterCount(), 97U ) << "The fixture did not parse.";

    bitmapFont.clear();

    ASSERT_EQ( bitmapFont.mSize, 0 ) << "Size survived a clear.";
    ASSERT_EQ( bitmapFont.mLineHeight, 0 ) << "Line height survived a clear.";
    ASSERT_EQ( bitmapFont.mBaseline, 0 ) << "Baseline survived a clear.";
    ASSERT_EQ( bitmapFont.getCharacterCount(), 0U ) << "Glyphs survived a clear.";
    ASSERT_EQ( bitmapFont.mPageName.size(), 0U ) << "Pages survived a clear.";
}

//-----------------------------------------------------------------------------
// Reading a second font over a first replaces it rather than joining it.
//
// This is the contract FontAsset::buildFontData rests on. parseFont has no clear
// of its own and mChar is a map, so without the clear the count becomes the union
// of the two fonts and only ever grows -- and re-pointing FontFile is exactly
// what an inspector makes easy. Arial and Orator Bold overlap almost completely
// (97 glyphs each) while disagreeing about every scalar, so an accumulating
// parse shows up in the glyph count only if the two are compared, and in the
// scalars either way.
//-----------------------------------------------------------------------------
TEST( BitmapFontParseTests, ASecondFontReplacesTheFirst )
{
    font::BitmapFont bitmapFont;

    ASSERT_TRUE( parseFontFile( bitmapFont, ARIAL_FNT ) ) << "Could not open " ARIAL_FNT ".";
    ASSERT_EQ( bitmapFont.mSize, 128 ) << "The first fixture did not parse.";
    ASSERT_EQ( bitmapFont.mPageName.size(), 2U ) << "The first fixture did not parse.";

    bitmapFont.clear();

    ASSERT_TRUE( parseFontFile( bitmapFont, ORATOR_FNT ) ) << "Could not open " ORATOR_FNT ".";

    // info face="Orator Std" size=72 / common lineHeight=72 base=56 pages=2
    ASSERT_EQ( bitmapFont.mSize, 72 ) << "The second font kept the first one's size.";
    ASSERT_EQ( bitmapFont.mLineHeight, 72 ) << "The second font kept the first one's line height.";
    ASSERT_EQ( bitmapFont.mBaseline, 56 ) << "The second font kept the first one's baseline.";

    // Not four. Both fonts declare two pages, so a page list that was appended to
    // rather than replaced is the one number that would not merely be wrong but
    // doubled.
    ASSERT_EQ( bitmapFont.mPageName.size(), 2U ) << "The page list accumulated instead of being replaced.";
    ASSERT_STREQ( bitmapFont.mPageName[0], "Orator Bold_0.png" ) << "Wrong first page file.";

    // Both fonts hold 97 glyphs over the same character ids, so this is a union
    // that would look identical either way -- it is here to say so, not to catch
    // anything the scalars above would miss.
    ASSERT_EQ( bitmapFont.getCharacterCount(), 97U ) << "Wrong glyph count.";
}

#endif // TORQUE_SHIPPING
