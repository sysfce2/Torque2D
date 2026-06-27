//-----------------------------------------------------------------------------
// Copyright (c) 2013 GarageGames, LLC
// Portions Copyright (c) 2014 James S Urquhart
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

#include "platform/platform.h"
#include "platformEmscripten/platformEmscripten.h"
#include "platformEmscripten/EmscriptenFont.h"
#include "string/unicode.h"
#include "console/console.h"

//------------------------------------------------------------------------------
// FreeType-backed font for the web. There are no system fonts in the browser, so
// createPlatformFont rasterizes a bundled .ttf (modeled on AndroidFont, which is
// likewise FreeType-based). GFont::create only reaches here on a .uft/.fnt cache
// miss, so designed faces keep their pre-baked look and only the gaps land here.
PlatformFont *createPlatformFont(const char *name, U32 size, U32 charset /* = TGE_ANSI_CHARSET */)
{
    PlatformFont *retFont = new EmscriptenFont;

    if(retFont->create(name, size, charset))
        return retFont;

    delete retFont;
    return NULL;
}

//------------------------------------------------------------------------------

void PlatformFont::enumeratePlatformFonts( Vector<StringTableEntry>& fonts )
{
}

//------------------------------------------------------------------------------

EmscriptenFont::EmscriptenFont()
{
    mBaseline       = 0;
    mHeight         = 0;
    face            = NULL;
    fontFaceCreated = false;

    if( FT_Init_FreeType( &library ) )
        Con::errorf("EmscriptenFont: failed to initialize the FreeType library");
}

//------------------------------------------------------------------------------

EmscriptenFont::~EmscriptenFont()
{
    if( fontFaceCreated )
        FT_Done_Face( face );
    FT_Done_FreeType( library );
}

//------------------------------------------------------------------------------

bool EmscriptenFont::create( const char* name, U32 size, U32 charset )
{
    if( name == NULL || size < 1 )
        return false;

    // The browser has no system fonts and no per-face .ttf is bundled (yet), so every
    // request rasterizes the generic fallback .ttf. The active core registers its OWN
    // copy's path in $pref::Web::fallbackFont (AppCore for a shipped game, EditorCore
    // for the editor — see each defaultPreferences.cs), which keeps the editor's and
    // the app's fonts separate. The requested face `name` is intentionally ignored
    // here until per-face .ttf resolution is added.
    const char* fontPath = Con::getVariable("$pref::Web::fallbackFont");
    if( fontPath == NULL || fontPath[0] == '\0' )
    {
        Con::errorf("EmscriptenFont::create - $pref::Web::fallbackFont is not set; "
                    "cannot rasterize '%s' %d.", name, size);
        return false;
    }

    // FreeType opens the file itself (ANSI stdio in ftsystem.c); under Emscripten the
    // path is a preloaded MEMFS file, so this works like the Android FT_New_Face path.
    int error = FT_New_Face( library, fontPath, 0, &face );
    if( error )
    {
        Con::errorf("EmscriptenFont::create - FreeType could not open font '%s' (error %d).", fontPath, error);
        fontFaceCreated = false;
        mHeight   = 0;
        mBaseline = 0;
        return false;
    }
    fontFaceCreated = true;

    FT_Set_Pixel_Sizes( face, 0, size );

    // 26.6 fixed-point -> integer pixels (round). 'baseline' == ascent in Torque.
    mBaseline = (face->size->metrics.ascender + 32) >> 6;
    mHeight   = ((face->size->metrics.ascender + (-face->size->metrics.descender)) + 32) >> 6;

    return true;
}

//------------------------------------------------------------------------------

bool EmscriptenFont::isValidChar( const UTF8* str ) const
{
    // since only low order characters are invalid, and since those characters
    // are single codeunits in UTF8, we can safely cast here.
    return isValidChar((UTF16)*str);
}

//------------------------------------------------------------------------------

bool EmscriptenFont::isValidChar( const UTF16 character) const
{
    // We cut out the ASCII control chars here. Only printable characters are valid.
    // 0x20 == 32 == space
    if( character < 0x20 )
        return false;

    return true;
}

//------------------------------------------------------------------------------

PlatformFont::CharInfo& EmscriptenFont::getCharInfo(const UTF8 *str) const
{
    return getCharInfo( oneUTF32toUTF16(oneUTF8toUTF32(str,NULL)) );
}

//------------------------------------------------------------------------------

PlatformFont::CharInfo& EmscriptenFont::getCharInfo(const UTF16 character) const
{
    // Declare and clear out the CharInfo that will be returned.
    static PlatformFont::CharInfo characterInfo;
    dMemset(&characterInfo, 0, sizeof(characterInfo));

    // prep values for GFont::addBitmap()
    characterInfo.bitmapIndex = 0;
    characterInfo.xOffset = 0;
    characterInfo.yOffset = 0;

    // Guard: a font whose face never created has no glyphs to render.
    if( !fontFaceCreated || face == NULL )
        return characterInfo;

    FT_GlyphSlot slot = face->glyph;

    int error = FT_Load_Char( face, character, FT_LOAD_RENDER );
    if( error )
        return characterInfo;

    // Use the rendered BITMAP dimensions for width/height (the advance comes from the
    // glyph metrics). This keeps the allocated bitmapData, the copy stride, and the
    // CharInfo size all consistent — FreeType's anti-aliased bitmap can be a pixel
    // wider than metrics.width/64, so sizing off the metrics would risk an overrun.
    characterInfo.xOrigin    = slot->bitmap_left;
    characterInfo.yOrigin    = slot->bitmap_top;
    characterInfo.width      = slot->bitmap.width;
    characterInfo.height     = slot->bitmap.rows;
    characterInfo.xIncrement = slot->advance.x / 64;

    // Finish if the character is undrawable (e.g. space).
    if( characterInfo.width == 0 || characterInfo.height == 0 )
        return characterInfo;

    // Allocate a bitmap surface and copy the glyph's 8-bit alpha coverage into it.
    const U32 bitmapSize = characterInfo.width * characterInfo.height;
    characterInfo.bitmapData = new U8[bitmapSize];
    dMemset(characterInfo.bitmapData, 0x00, bitmapSize);

    if (slot->bitmap.buffer != NULL)
    {
        for (U32 j = 0; j < characterInfo.height; j++)
            for (U32 i = 0; i < characterInfo.width; i++)
                characterInfo.bitmapData[i + (j * characterInfo.width)] =
                    slot->bitmap.buffer[i + (j * slot->bitmap.pitch)];
    }

    // Return character information.
    return characterInfo;
}
