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

#ifndef _GUI_PROFILE_THEME_H_
#define _GUI_PROFILE_THEME_H_

#ifndef _COLOR_H_
#include "graphics/gColor.h"
#endif

//-----------------------------------------------------------------------------
/// A set of theme-wide values (fonts, palette colors, border size) from which
/// a complete family of GuiControlProfile / GuiBorderProfile objects is
/// derived. Formalizes in C++ the theme pattern used by the script-side
/// editor themes and AppCore profile helpers.
//-----------------------------------------------------------------------------
class GuiProfileTheme
{
public:
    /// Shift a color's HSV value (brightness) by percent (positive brightens,
    /// negative darkens), preserving hue and alpha. The value fraction is
    /// clamped to 0..1, so over-brightening saturates without hue wash-out.
    /// Black, having no hue, brightens along the neutral gray rail.
    static ColorI adjustValue(const ColorI& color, F32 percent);

    /// Replace a color's alpha byte, clamping to 0..255.
    static ColorI setAlpha(const ColorI& color, S32 alpha);
};

#endif // _GUI_PROFILE_THEME_H_
