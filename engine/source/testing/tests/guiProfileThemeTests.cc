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

#ifndef _GUITYPES_H_
#include "gui/guiTypes.h"
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

//-----------------------------------------------------------------------------
// Theme membership: profiles attached to a theme track which fields were
// explicitly overridden (any external field write marks the field), while
// standalone profiles behave exactly as before.
//-----------------------------------------------------------------------------

TEST( GuiProfileThemeTests, ThemedProfileMarksExternalFieldWritesAsOverridden )
{
    GuiProfileTheme* theme = new GuiProfileTheme();
    theme->registerObject();
    GuiControlProfile* profile = new GuiControlProfile();
    profile->registerObject();
    profile->setTheme( theme );

    StringTableEntry fillColor = StringTable->insert( "fillColor" );
    ASSERT_FALSE( profile->isThemeFieldOverridden( fillColor ) );

    profile->setDataField( fillColor, NULL, "1 2 3 4" );
    ASSERT_TRUE( profile->isThemeFieldOverridden( fillColor ) );

    profile->clearThemeFieldOverride( fillColor );
    ASSERT_FALSE( profile->isThemeFieldOverridden( fillColor ) );

    profile->deleteObject();
    theme->deleteObject();

    SUCCEED();
}

TEST( GuiProfileThemeTests, StandaloneProfileDoesNotTrackOverrides )
{
    GuiControlProfile* profile = new GuiControlProfile();
    profile->registerObject();

    StringTableEntry fillColor = StringTable->insert( "fillColor" );
    profile->setDataField( fillColor, NULL, "1 2 3 4" );
    ASSERT_FALSE( profile->isThemeFieldOverridden( fillColor ) );

    profile->deleteObject();

    SUCCEED();
}

TEST( GuiProfileThemeTests, CategoryFieldIsNotOverridable )
{
    GuiProfileTheme* theme = new GuiProfileTheme();
    theme->registerObject();
    GuiControlProfile* profile = new GuiControlProfile();
    profile->registerObject();
    profile->setTheme( theme );

    StringTableEntry category = StringTable->insert( "category" );
    profile->setDataField( category, NULL, "Button" );
    ASSERT_FALSE( profile->isThemeFieldOverridden( category ) )
        << "The category field is theme-managed and must never be marked overridden.";

    profile->deleteObject();
    theme->deleteObject();

    SUCCEED();
}

TEST( GuiProfileThemeTests, ThemedProfileWriteFieldFiltersUnoverriddenFields )
{
    GuiProfileTheme* theme = new GuiProfileTheme();
    theme->registerObject();
    GuiControlProfile* profile = new GuiControlProfile();
    profile->registerObject();

    StringTableEntry fillColor = StringTable->insert( "fillColor" );
    StringTableEntry category = StringTable->insert( "category" );

    // Standalone: normal serialization decision (non-empty value writes).
    ASSERT_TRUE( profile->writeField( fillColor, "1 2 3 4" ) );

    profile->setTheme( theme );

    // Themed, not overridden: field is derived from the theme, don't write it.
    ASSERT_FALSE( profile->writeField( fillColor, "1 2 3 4" ) );

    // Themed and overridden: the override must persist.
    profile->setDataField( fillColor, NULL, "1 2 3 4" );
    ASSERT_TRUE( profile->writeField( fillColor, "1 2 3 4" ) );

    // Category always persists so a loaded theme can rebind members.
    ASSERT_TRUE( profile->writeField( category, "Button" ) );

    profile->deleteObject();
    theme->deleteObject();

    SUCCEED();
}

TEST( GuiProfileThemeTests, ThemeDeletionNullsMemberBackPointer )
{
    GuiProfileTheme* theme = new GuiProfileTheme();
    theme->registerObject();
    GuiControlProfile* profile = new GuiControlProfile();
    profile->registerObject();
    profile->setTheme( theme );
    ASSERT_EQ( profile->getTheme(), theme );

    theme->deleteObject();
    ASSERT_EQ( profile->getTheme(), (GuiProfileTheme*)NULL )
        << "A member must not be left with a dangling theme pointer.";

    profile->deleteObject();

    SUCCEED();
}

TEST( GuiProfileThemeTests, DeletingBorderProfileNullsProfileBorderPointers )
{
    GuiControlProfile* profile = new GuiControlProfile();
    profile->registerObject();
    GuiBorderProfile* border = new GuiBorderProfile();
    border->registerObject();

    profile->setLeftProfile( border );
    ASSERT_EQ( profile->getLeftBorder(), border );

    border->deleteObject();
    ASSERT_EQ( profile->getLeftBorder(), (GuiBorderProfile*)NULL )
        << "Deleting a border profile must not leave a dangling pointer.";

    profile->deleteObject();

    SUCCEED();
}

TEST( GuiProfileThemeTests, BorderProfileSupportsThemeMembershipAndCategory )
{
    GuiProfileTheme* theme = new GuiProfileTheme();
    theme->registerObject();
    GuiBorderProfile* border = new GuiBorderProfile();
    border->registerObject();
    border->setTheme( theme );

    StringTableEntry margin = StringTable->insert( "margin" );
    ASSERT_FALSE( border->isThemeFieldOverridden( margin ) );
    border->setDataField( margin, NULL, "4" );
    ASSERT_TRUE( border->isThemeFieldOverridden( margin ) );

    // Border profiles carry a category persist field like control profiles.
    border->setDataField( StringTable->insert( "category" ), NULL, "Bright" );
    ASSERT_STREQ( border->getDataField( StringTable->insert( "category" ), NULL ), "Bright" );

    border->deleteObject();
    theme->deleteObject();

    SUCCEED();
}

//-----------------------------------------------------------------------------
// Theme core: a registered theme auto-creates one named member profile per
// engine-defined category (and border members per border category), owns
// them, restamps them when theme values change, and supports extra profiles
// alongside the defaults.
//-----------------------------------------------------------------------------

TEST( GuiProfileThemeTests, ThemeAutoCreatesOneProfilePerCategory )
{
    GuiProfileTheme* theme = new GuiProfileTheme();
    theme->registerObject( "UnitTestTheme" );

    ASSERT_EQ( theme->getProfileCount(), GuiProfileTheme::getCategoryCount() );

    GuiControlProfile* button = dynamic_cast<GuiControlProfile*>( Sim::findObject( "UnitTestThemeButtonProfile" ) );
    ASSERT_TRUE( button != NULL ) << "Members are named <ThemeName><CategorySuffix>.";
    ASSERT_EQ( button->getTheme(), theme );
    ASSERT_STREQ( button->mCategory, "Button" );
    ASSERT_EQ( theme->getProfile( StringTable->insert( "Button" ) ), button );

    theme->deleteObject();

    SUCCEED();
}

TEST( GuiProfileThemeTests, ThemeAutoCreatesBorderMembers )
{
    GuiProfileTheme* theme = new GuiProfileTheme();
    theme->registerObject( "UnitTestTheme" );

    GuiBorderProfile* border = dynamic_cast<GuiBorderProfile*>( Sim::findObject( "UnitTestThemeDefaultBorder" ) );
    ASSERT_TRUE( border != NULL ) << "Border members are named <ThemeName><BorderSuffix>.";
    ASSERT_EQ( border->getTheme(), theme );
    ASSERT_STREQ( border->mCategory, "Default" );
    ASSERT_EQ( theme->getBorder( StringTable->insert( "Default" ) ), border );

    theme->deleteObject();

    SUCCEED();
}

TEST( GuiProfileThemeTests, ThemeDeletionDeletesItsMembers )
{
    GuiProfileTheme* theme = new GuiProfileTheme();
    theme->registerObject( "UnitTestTheme" );
    ASSERT_TRUE( Sim::findObject( "UnitTestThemeButtonProfile" ) != NULL );
    ASSERT_TRUE( Sim::findObject( "UnitTestThemeDefaultBorder" ) != NULL );

    theme->deleteObject();

    ASSERT_TRUE( Sim::findObject( "UnitTestThemeButtonProfile" ) == NULL )
        << "The theme owns its members and must delete them.";
    ASSERT_TRUE( Sim::findObject( "UnitTestThemeDefaultBorder" ) == NULL )
        << "The theme owns its border members and must delete them.";

    SUCCEED();
}

TEST( GuiProfileThemeTests, DeletedDefaultMemberIsRecreatedOnRestamp )
{
    GuiProfileTheme* theme = new GuiProfileTheme();
    theme->registerObject( "UnitTestTheme" );

    GuiControlProfile* button = theme->getProfile( StringTable->insert( "Button" ) );
    ASSERT_TRUE( button != NULL );
    button->deleteObject();
    ASSERT_TRUE( Sim::findObject( "UnitTestThemeButtonProfile" ) == NULL );

    theme->restamp();

    GuiControlProfile* recreated = dynamic_cast<GuiControlProfile*>( Sim::findObject( "UnitTestThemeButtonProfile" ) );
    ASSERT_TRUE( recreated != NULL ) << "A theme is always complete: deleted defaults are recreated.";
    ASSERT_EQ( recreated->getTheme(), theme );
    ASSERT_STREQ( recreated->mCategory, "Button" );

    theme->deleteObject();

    SUCCEED();
}

TEST( GuiProfileThemeTests, ExtraProfilesShareCategoryAndOnlyExtrasAreRemovable )
{
    GuiProfileTheme* theme = new GuiProfileTheme();
    theme->registerObject( "UnitTestTheme" );

    const S32 baseCount = theme->getProfileCount();
    GuiControlProfile* extra = theme->createProfile( "Button", NULL );
    ASSERT_TRUE( extra != NULL );
    ASSERT_STREQ( extra->getName(), "UnitTestThemeButtonProfile2" );
    ASSERT_STREQ( extra->mCategory, "Button" );
    ASSERT_EQ( extra->getTheme(), theme );
    ASSERT_EQ( theme->getProfileCount(), baseCount + 1 );

    GuiControlProfile* defaultButton = theme->getProfile( StringTable->insert( "Button" ) );
    ASSERT_FALSE( theme->removeProfile( defaultButton ) ) << "Default members must not be removable.";
    ASSERT_TRUE( theme->removeProfile( extra ) );
    ASSERT_EQ( theme->getProfileCount(), baseCount );

    theme->deleteObject();

    SUCCEED();
}

TEST( GuiProfileThemeTests, ThemeValueChangeRestampsMembersPreservingOverrides )
{
    GuiProfileTheme* theme = new GuiProfileTheme();
    theme->registerObject( "UnitTestTheme" );

    GuiControlProfile* defaultProfile = theme->getProfile( StringTable->insert( "Default" ) );
    ASSERT_TRUE( defaultProfile != NULL );

    // Contract: the Default recipe binds fillColor to the theme's
    // colorBackground. Changing the theme value restamps the member.
    theme->setDataField( StringTable->insert( "colorBackground" ), NULL, "11 22 33 255" );
    ASSERT_TRUE( defaultProfile->mFillColor == ColorI( 11, 22, 33, 255 ) );

    // An explicit member override survives later theme changes.
    defaultProfile->setDataField( StringTable->insert( "fillColor" ), NULL, "9 8 7 6" );
    theme->setDataField( StringTable->insert( "colorBackground" ), NULL, "44 55 66 255" );
    ASSERT_TRUE( defaultProfile->mFillColor == ColorI( 9, 8, 7, 6 ) )
        << "Overridden fields must survive restamping.";

    // Clearing the override re-derives the field from the theme.
    defaultProfile->clearThemeFieldOverride( StringTable->insert( "fillColor" ) );
    theme->restamp();
    ASSERT_TRUE( defaultProfile->mFillColor == ColorI( 44, 55, 66, 255 ) )
        << "Cleared overrides must re-derive from the theme.";

    theme->deleteObject();

    SUCCEED();
}

#endif // TORQUE_SHIPPING
