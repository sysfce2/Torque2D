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

#ifndef _GUI_PROFILE_THEME_H_
#include "gui/guiProfileTheme.h"
#endif

//-----------------------------------------------------------------------------
// adjustValue: HSV-value (brightness) shift that preserves hue and alpha.
// Positive percent brightens, negative darkens. This is the C++ port of the
// script-side BaseTheme::adjustValue / AppCore::AdjustColorValue helpers,
// with the over-brighten clamp fixed (value fraction clamps to 0..1).
//-----------------------------------------------------------------------------

TEST( GuiProfileThemeTests, AdjustValueDarkensPreservingHueAndAlpha )
{
    // largest channel 210 -> value 210/255; -20 percent -> value 159/255.
    const ColorI result = GuiProfileTheme::adjustValue( ColorI( 210, 90, 30, 128 ), -20.0f );

    ASSERT_EQ( result.red, 159 );
    ASSERT_EQ( result.green, 68 );
    ASSERT_EQ( result.blue, 23 );
    ASSERT_EQ( result.alpha, 128 ) << "Alpha must be preserved.";

    SUCCEED();
}

TEST( GuiProfileThemeTests, AdjustValueBrightens )
{
    // largest channel 153 -> value 0.6; +20 percent -> value 0.8.
    const ColorI result = GuiProfileTheme::adjustValue( ColorI( 51, 102, 153, 255 ), 20.0f );

    ASSERT_EQ( result.red, 68 );
    ASSERT_EQ( result.green, 136 );
    ASSERT_EQ( result.blue, 204 );
    ASSERT_EQ( result.alpha, 255 );

    SUCCEED();
}

TEST( GuiProfileThemeTests, AdjustValueClampsAtFullBrightnessWithoutHueWashout )
{
    // The script helper's clamp bug let the value fraction exceed 1, railing
    // channels individually and shifting hue. Brightening an already
    // full-value color must be a no-op.
    const ColorI result = GuiProfileTheme::adjustValue( ColorI( 255, 100, 0, 255 ), 50.0f );

    ASSERT_EQ( result.red, 255 );
    ASSERT_EQ( result.green, 100 ) << "Hue must not wash out when over-brightening.";
    ASSERT_EQ( result.blue, 0 );
    ASSERT_EQ( result.alpha, 255 );

    SUCCEED();
}

TEST( GuiProfileThemeTests, AdjustValueBrightensBlackAlongGrayRail )
{
    // Black has no hue; brightening walks the neutral gray rail.
    const ColorI result = GuiProfileTheme::adjustValue( ColorI( 0, 0, 0, 255 ), 20.0f );

    ASSERT_EQ( result.red, 51 );
    ASSERT_EQ( result.green, 51 );
    ASSERT_EQ( result.blue, 51 );
    ASSERT_EQ( result.alpha, 255 );

    SUCCEED();
}

TEST( GuiProfileThemeTests, AdjustValueDarkensToBlackFloor )
{
    const ColorI result = GuiProfileTheme::adjustValue( ColorI( 100, 100, 100, 7 ), -200.0f );

    ASSERT_EQ( result.red, 0 );
    ASSERT_EQ( result.green, 0 );
    ASSERT_EQ( result.blue, 0 );
    ASSERT_EQ( result.alpha, 7 ) << "Alpha must be preserved.";

    SUCCEED();
}

//-----------------------------------------------------------------------------
// setAlpha: replaces the alpha byte only, clamping to 0..255.
//-----------------------------------------------------------------------------

TEST( GuiProfileThemeTests, SetAlphaReplacesAlphaOnly )
{
    const ColorI result = GuiProfileTheme::setAlpha( ColorI( 10, 20, 30, 255 ), 100 );

    ASSERT_EQ( result.red, 10 );
    ASSERT_EQ( result.green, 20 );
    ASSERT_EQ( result.blue, 30 );
    ASSERT_EQ( result.alpha, 100 );

    SUCCEED();
}

TEST( GuiProfileThemeTests, SetAlphaClampsRange )
{
    ASSERT_EQ( GuiProfileTheme::setAlpha( ColorI( 1, 2, 3, 4 ), 300 ).alpha, 255 );
    ASSERT_EQ( GuiProfileTheme::setAlpha( ColorI( 1, 2, 3, 4 ), -5 ).alpha, 0 );

    SUCCEED();
}

#endif // TORQUE_SHIPPING
