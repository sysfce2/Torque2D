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

ConsoleMethodGroupBeginWithDocs(FontAsset, AssetBase)

//------------------------------------------------------------------------------

/*! Sets the Font file.
    @return No return value.
*/
ConsoleMethodWithDocs(FontAsset, setFontFile, ConsoleVoid, 3, 3, (FontFile))
{
    object->setFontFile( argv[2] );
}

//-----------------------------------------------------------------------------

/*! Gets the Font file.
    @return Returns the Font file.
*/
ConsoleMethodWithDocs(FontAsset, getFontFile, ConsoleString, 2, 2, ())
{
    return object->getFontFile();
}

//-----------------------------------------------------------------------------

/*! Gets the Font file as a path relative to the asset file.
    @return Returns the Font file relative to the asset file.
*/
ConsoleMethodWithDocs(FontAsset, getRelativeFontFile, ConsoleString, 2, 2, ())
{
    return object->getRelativeFontFile();
}

//-----------------------------------------------------------------------------

/*! Gets the number of glyphs the font holds.
    @return Returns the glyph count, or zero if the font did not load.
*/
ConsoleMethodWithDocs(FontAsset, getGlyphCount, ConsoleInt, 2, 2, ())
{
    return (S32)object->getGlyphCount();
}

//-----------------------------------------------------------------------------

/*! Gets the number of texture pages the font declares.
    @return Returns the page count.
*/
ConsoleMethodWithDocs(FontAsset, getPageCount, ConsoleInt, 2, 2, ())
{
    return (S32)object->getPageCount();
}

//-----------------------------------------------------------------------------

/*! Gets the number of declared pages whose image actually loaded. Fewer than
    getPageCount() means a page image named inside the .fnt file is missing.
    @return Returns the loaded page count.
*/
ConsoleMethodWithDocs(FontAsset, getLoadedPageCount, ConsoleInt, 2, 2, ())
{
    return (S32)object->getLoadedPageCount();
}

//-----------------------------------------------------------------------------

/*! Gets the size the font was generated at, in pixels.
    @return Returns the native font size.
*/
ConsoleMethodWithDocs(FontAsset, getFontSize, ConsoleInt, 2, 2, ())
{
    return (S32)object->getFontSize();
}

//-----------------------------------------------------------------------------

/*! Gets the distance between baselines, in pixels.
    @return Returns the line height.
*/
ConsoleMethodWithDocs(FontAsset, getLineHeight, ConsoleInt, 2, 2, ())
{
    return (S32)object->getLineHeight();
}

//-----------------------------------------------------------------------------

/*! Gets the distance from the top of a line to the baseline, in pixels.
    @return Returns the baseline.
*/
ConsoleMethodWithDocs(FontAsset, getBaseline, ConsoleInt, 2, 2, ())
{
    return (S32)object->getBaseline();
}

//-----------------------------------------------------------------------------

/*! Gets the image file for a page, as named inside the .fnt file.
    @param pageIndex The zero-based page index.
    @return Returns the page image file, or an empty string if there is no such page.
*/
ConsoleMethodWithDocs(FontAsset, getPageFile, ConsoleString, 3, 3, (int pageIndex))
{
    return object->getPageFile( (U32)dAtoi(argv[2]) );
}

//-----------------------------------------------------------------------------

/*! Gets the width of a page's loaded texture, in pixels.
    @param pageIndex The zero-based page index.
    @return Returns the page width, or zero if the page did not load.
*/
ConsoleMethodWithDocs(FontAsset, getPageWidth, ConsoleInt, 3, 3, (int pageIndex))
{
    return (S32)object->getPageWidth( (U32)dAtoi(argv[2]) );
}

//-----------------------------------------------------------------------------

/*! Gets the height of a page's loaded texture, in pixels.
    @param pageIndex The zero-based page index.
    @return Returns the page height, or zero if the page did not load.
*/
ConsoleMethodWithDocs(FontAsset, getPageHeight, ConsoleInt, 3, 3, (int pageIndex))
{
    return (S32)object->getPageHeight( (U32)dAtoi(argv[2]) );
}

//------------------------------------------------------------------------------

ConsoleMethodGroupEndWithDocs(FontAsset)
