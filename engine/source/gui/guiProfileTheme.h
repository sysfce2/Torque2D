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

#ifndef _SIM_OBJECT_H_
#include "sim/simObject.h"
#endif

#ifndef _VECTOR_H_
#include "collection/vector.h"
#endif

class GuiProfileTheme;

//-----------------------------------------------------------------------------
/// Embedded by GuiControlProfile and GuiBorderProfile to record membership in
/// a GuiProfileTheme: a non-owning back-pointer to the theme plus the set of
/// fields the user explicitly overrode away from the theme's stamped
/// defaults. A NULL theme means the object is standalone and behaves exactly
/// as it did before themes existed.
//-----------------------------------------------------------------------------
struct GuiThemeMembership
{
    GuiProfileTheme* mTheme;                ///< Owning theme; NULL for standalone objects.
    Vector<StringTableEntry> mOverrides;    ///< Fields explicitly overridden away from the theme's defaults.

    GuiThemeMembership() : mTheme(NULL) {}

    inline bool isOverridden(StringTableEntry field) const
    {
        for (S32 i = 0; i < mOverrides.size(); ++i)
        {
            if (mOverrides[i] == field)
                return true;
        }
        return false;
    }

    inline void markOverride(StringTableEntry field)
    {
        if (!isOverridden(field))
            mOverrides.push_back(field);
    }

    inline void clearOverride(StringTableEntry field)
    {
        for (S32 i = 0; i < mOverrides.size(); ++i)
        {
            if (mOverrides[i] == field)
            {
                mOverrides.erase(i);
                return;
            }
        }
    }

    inline void clearAll() { mOverrides.clear(); }
};

//-----------------------------------------------------------------------------
/// A set of theme-wide values (fonts, palette colors, border size) from which
/// a complete family of GuiControlProfile / GuiBorderProfile objects is
/// derived. Formalizes in C++ the theme pattern used by the script-side
/// editor themes and AppCore profile helpers.
//-----------------------------------------------------------------------------
class GuiProfileTheme : public SimObject
{
private:
    typedef SimObject Parent;

public:
    DECLARE_CONOBJECT(GuiProfileTheme);

    /// Shift a color's HSV value (brightness) by percent (positive brightens,
    /// negative darkens), preserving hue and alpha. The value fraction is
    /// clamped to 0..1, so over-brightening saturates without hue wash-out.
    /// Black, having no hue, brightens along the neutral gray rail.
    static ColorI adjustValue(const ColorI& color, F32 percent);

    /// Replace a color's alpha byte, clamping to 0..255.
    static ColorI setAlpha(const ColorI& color, S32 alpha);
};

#endif // _GUI_PROFILE_THEME_H_
