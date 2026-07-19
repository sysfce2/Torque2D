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

#ifndef _TAML_H_
#include "persistence/taml/taml.h"
#endif

#include <stdio.h>

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

    GuiControlProfile* panel = theme->getProfile( StringTable->insert( "Panel" ) );
    ASSERT_TRUE( panel != NULL );

    // Contract: the Panel recipe binds fillColor to the theme's
    // colorBackground. Changing the theme value restamps the member.
    theme->setDataField( StringTable->insert( "colorBackground" ), NULL, "11 22 33 255" );
    ASSERT_TRUE( panel->mFillColor == ColorI( 11, 22, 33, 255 ) );

    // An explicit member override survives later theme changes.
    panel->setDataField( StringTable->insert( "fillColor" ), NULL, "9 8 7 6" );
    theme->setDataField( StringTable->insert( "colorBackground" ), NULL, "44 55 66 255" );
    ASSERT_TRUE( panel->mFillColor == ColorI( 9, 8, 7, 6 ) )
        << "Overridden fields must survive restamping.";

    // Clearing the override re-derives the field from the theme.
    panel->clearThemeFieldOverride( StringTable->insert( "fillColor" ) );
    theme->restamp();
    ASSERT_TRUE( panel->mFillColor == ColorI( 44, 55, 66, 255 ) )
        << "Cleared overrides must re-derive from the theme.";

    theme->deleteObject();

    SUCCEED();
}

//-----------------------------------------------------------------------------
// Category recipe contracts: representative bindings of the engine-defined
// recipes ported from AppCore's guiProfiles.cs, mapped onto the semantic
// theme values (background, panel, text, accent, highlight, warning).
//-----------------------------------------------------------------------------

TEST( GuiProfileThemeTests, ButtonRecipeBindsAccentHighlightAndBevelBorders )
{
    GuiProfileTheme* theme = new GuiProfileTheme();
    theme->registerObject( "UnitTestTheme" );

    GuiControlProfile* button = theme->getProfile( StringTable->insert( "Button" ) );
    ASSERT_TRUE( button != NULL );
    ASSERT_TRUE( button->mFillColor == theme->getColorAccent() );
    ASSERT_TRUE( button->mFillColorHL == GuiProfileTheme::adjustValue( theme->getColorAccent(), 10.0f ) );
    ASSERT_TRUE( button->mFillColorSL == theme->getColorHighlight() );
    ASSERT_TRUE( button->mFillColorNA == GuiProfileTheme::setAlpha( theme->getColorAccent(), 80 ) );
    ASSERT_TRUE( button->mCanKeyFocus );
    ASSERT_TRUE( button->mTabable );
    ASSERT_EQ( button->getLeftBorder(), theme->getBorder( StringTable->insert( "ButtonBright" ) ) );
    ASSERT_EQ( button->getBottomBorder(), theme->getBorder( StringTable->insert( "ButtonDark" ) ) );

    theme->deleteObject();

    SUCCEED();
}

TEST( GuiProfileThemeTests, WindowCloseButtonRecipeUsesWarningColor )
{
    GuiProfileTheme* theme = new GuiProfileTheme();
    theme->registerObject( "UnitTestTheme" );

    GuiControlProfile* close = theme->getProfile( StringTable->insert( "WindowCloseButton" ) );
    ASSERT_TRUE( close != NULL );
    ASSERT_TRUE( close->mFillColorHL == GuiProfileTheme::setAlpha( theme->getColorWarning(), 150 ) );
    ASSERT_TRUE( close->mFillColorSL == GuiProfileTheme::adjustValue( theme->getColorWarning(), 10.0f ) );

    theme->deleteObject();

    SUCCEED();
}

TEST( GuiProfileThemeTests, TooltipRecipeUsesHighlightTextOverDimmedBackground )
{
    GuiProfileTheme* theme = new GuiProfileTheme();
    theme->registerObject( "UnitTestTheme" );

    GuiControlProfile* tooltip = theme->getProfile( StringTable->insert( "Tooltip" ) );
    ASSERT_TRUE( tooltip != NULL );
    ASSERT_TRUE( tooltip->mFillColor == GuiProfileTheme::setAlpha( theme->getColorBackground(), 220 ) );
    ASSERT_TRUE( tooltip->mFontColor == theme->getColorHighlight() );
    ASSERT_EQ( tooltip->mBorderDefault, theme->getBorder( StringTable->insert( "Bright" ) ) );

    theme->deleteObject();

    SUCCEED();
}

TEST( GuiProfileThemeTests, LabelRecipeUsesBodyFontLeftAligned )
{
    GuiProfileTheme* theme = new GuiProfileTheme();
    theme->registerObject( "UnitTestTheme" );

    GuiControlProfile* label = theme->getProfile( StringTable->insert( "Label" ) );
    ASSERT_TRUE( label != NULL );
    ASSERT_EQ( label->mFontType, theme->getFontBody() );
    ASSERT_EQ( label->mFontSize, theme->getFontSize() );
    ASSERT_EQ( label->mAlignment, AlignmentType::LeftAlign );
    ASSERT_TRUE( label->mFontColor == theme->getColorText() );

    theme->deleteObject();

    SUCCEED();
}

TEST( GuiProfileThemeTests, ScrollThumbRecipeWiresScrollBorders )
{
    GuiProfileTheme* theme = new GuiProfileTheme();
    theme->registerObject( "UnitTestTheme" );

    GuiControlProfile* thumb = theme->getProfile( StringTable->insert( "ScrollThumb" ) );
    ASSERT_TRUE( thumb != NULL );
    ASSERT_TRUE( thumb->mFillColorSL == theme->getColorAccent() );
    ASSERT_EQ( thumb->mBorderDefault, theme->getBorder( StringTable->insert( "ScrollBright" ) ) );
    ASSERT_EQ( thumb->getRightBorder(), theme->getBorder( StringTable->insert( "ScrollDark" ) ) );
    ASSERT_EQ( thumb->getBottomBorder(), theme->getBorder( StringTable->insert( "ScrollDark" ) ) );

    theme->deleteObject();

    SUCCEED();
}

//-----------------------------------------------------------------------------
// TAML persistence: one theme file holds the theme values plus every member
// as a child object carrying only its name, category, and overridden fields.
//-----------------------------------------------------------------------------

TEST( GuiProfileThemeTests, ThemeTamlRoundTripPreservesValuesAndOverrides )
{
    const char* fileName = "unitTestGuiProfileTheme.taml";

    GuiProfileTheme* theme = new GuiProfileTheme();
    theme->registerObject( "UnitTestTheme" );

    // Theme value, a member override, a border override, and an extra profile.
    theme->setDataField( StringTable->insert( "colorAccent" ), NULL, "10 20 30 255" );
    GuiControlProfile* button = theme->getProfile( StringTable->insert( "Button" ) );
    button->setDataField( StringTable->insert( "fillColor" ), NULL, "9 8 7 6" );
    GuiBorderProfile* bright = theme->getBorder( StringTable->insert( "Bright" ) );
    bright->setDataField( StringTable->insert( "margin" ), NULL, "7" );
    ASSERT_TRUE( theme->createProfile( "Button", NULL ) != NULL );

    Taml taml;
    ASSERT_TRUE( taml.write( theme, fileName ) );
    theme->deleteObject();
    ASSERT_TRUE( Sim::findObject( "UnitTestTheme" ) == NULL );

    GuiProfileTheme* loaded = taml.read<GuiProfileTheme>( fileName );
    ASSERT_TRUE( loaded != NULL );
    ASSERT_TRUE( loaded->getColorAccent() == ColorI( 10, 20, 30, 255 ) );

    // The default member keeps its name, override flag, and override value.
    GuiControlProfile* loadedButton = loaded->getProfile( StringTable->insert( "Button" ) );
    ASSERT_TRUE( loadedButton != NULL );
    ASSERT_EQ( (SimObject*)loadedButton, Sim::findObject( "UnitTestThemeButtonProfile" ) );
    ASSERT_EQ( loadedButton->getTheme(), loaded );
    ASSERT_TRUE( loadedButton->isThemeFieldOverridden( StringTable->insert( "fillColor" ) ) );
    ASSERT_TRUE( loadedButton->mFillColor == ColorI( 9, 8, 7, 6 ) );

    // Non-overridden fields re-derive from the loaded theme values.
    ASSERT_TRUE( loadedButton->mFillColorHL == GuiProfileTheme::adjustValue( ColorI( 10, 20, 30, 255 ), 10.0f ) );

    // The border override survives.
    GuiBorderProfile* loadedBright = loaded->getBorder( StringTable->insert( "Bright" ) );
    ASSERT_TRUE( loadedBright != NULL );
    ASSERT_TRUE( loadedBright->isThemeFieldOverridden( StringTable->insert( "margin" ) ) );
    ASSERT_EQ( loadedBright->mMargin[0], 7 );

    // The extra profile survives alongside the full default set.
    ASSERT_EQ( loaded->getProfileCount(), GuiProfileTheme::getCategoryCount() + 1 );
    GuiControlProfile* loadedExtra = dynamic_cast<GuiControlProfile*>( Sim::findObject( "UnitTestThemeButtonProfile2" ) );
    ASSERT_TRUE( loadedExtra != NULL );
    ASSERT_STREQ( loadedExtra->mCategory, "Button" );
    ASSERT_EQ( loadedExtra->getTheme(), loaded );

    loaded->deleteObject();
    Platform::fileDelete( fileName );

    SUCCEED();
}

TEST( GuiProfileThemeTests, ThemeTamlWritesOnlyOverriddenMemberFields )
{
    const char* fileName = "unitTestGuiProfileTheme.taml";

    GuiProfileTheme* theme = new GuiProfileTheme();
    theme->registerObject( "UnitTestTheme" );
    GuiControlProfile* button = theme->getProfile( StringTable->insert( "Button" ) );
    button->setDataField( StringTable->insert( "fillColor" ), NULL, "9 8 7 6" );

    Taml taml;
    ASSERT_TRUE( taml.write( theme, fileName ) );
    theme->deleteObject();

    // Read the raw file text.
    FILE* file = fopen( fileName, "rb" );
    ASSERT_TRUE( file != NULL );
    fseek( file, 0, SEEK_END );
    const long fileSize = ftell( file );
    fseek( file, 0, SEEK_SET );
    char* text = new char[fileSize + 1];
    fread( text, 1, fileSize, file );
    text[fileSize] = '\0';
    fclose( file );

    // The override is written; derived-only fields are not.
    ASSERT_TRUE( dStrstr( (const char*)text, (const char*)"9 8 7 6" ) != NULL )
        << "The overridden field value must be serialized.";
    ASSERT_TRUE( dStrstr( (const char*)text, (const char*)"fontColorLinkHL" ) == NULL )
        << "Fields derived from the theme must not be serialized.";

    delete [] text;
    Platform::fileDelete( fileName );

    SUCCEED();
}

TEST( GuiProfileThemeTests, AccentChangePropagatesToButtonFill )
{
    GuiProfileTheme* theme = new GuiProfileTheme();
    theme->registerObject( "UnitTestTheme" );

    theme->setDataField( StringTable->insert( "colorAccent" ), NULL, "10 20 30 255" );

    GuiControlProfile* button = theme->getProfile( StringTable->insert( "Button" ) );
    ASSERT_TRUE( button != NULL );
    ASSERT_TRUE( button->mFillColor == ColorI( 10, 20, 30, 255 ) );

    theme->deleteObject();

    SUCCEED();
}

#endif // TORQUE_SHIPPING
