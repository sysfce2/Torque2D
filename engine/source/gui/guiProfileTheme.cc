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
#include "gui/guiProfileTheme.h"
#endif

#ifndef _MMATHFN_H_
#include "math/mMathFn.h"
#endif

//-----------------------------------------------------------------------------

ColorI GuiProfileTheme::adjustValue(const ColorI& color, F32 percent)
{
    const U8 largest = getMax(color.red, getMax(color.green, color.blue));
    const F32 currentValue = F32(largest) / 255.0f;

    // Project each channel onto the full-brightness rail, preserving hue.
    // Black has no hue, so it brightens along the neutral gray rail.
    F32 fullRed, fullGreen, fullBlue;
    if (largest == 0)
    {
        fullRed = fullGreen = fullBlue = 255.0f;
    }
    else
    {
        fullRed = F32(color.red) / currentValue;
        fullGreen = F32(color.green) / currentValue;
        fullBlue = F32(color.blue) / currentValue;
    }

    const F32 newValue = mClampF(currentValue + (percent / 100.0f), 0.0f, 1.0f);

    return ColorI(U8(mRound(mClampF(fullRed * newValue, 0.0f, 255.0f))),
                  U8(mRound(mClampF(fullGreen * newValue, 0.0f, 255.0f))),
                  U8(mRound(mClampF(fullBlue * newValue, 0.0f, 255.0f))),
                  color.alpha);
}

//-----------------------------------------------------------------------------

ColorI GuiProfileTheme::setAlpha(const ColorI& color, S32 alpha)
{
    return ColorI(color.red, color.green, color.blue, U8(mClamp(alpha, 0, 255)));
}
