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

#ifndef _GUI_INSPECTOR_H_
#include "gui/editor/guiInspector.h"
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
    border->setDataField( StringTable->insert( "category" ), NULL, "Light" );
    ASSERT_STREQ( border->getDataField( StringTable->insert( "category" ), NULL ), "Light" );

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

    GuiBorderProfile* border = dynamic_cast<GuiBorderProfile*>( Sim::findObject( "UnitTestThemeRimmedBorder" ) );
    ASSERT_TRUE( border != NULL ) << "Border members are named <ThemeName><BorderSuffix>.";
    ASSERT_EQ( border->getTheme(), theme );
    ASSERT_STREQ( border->mCategory, "Rimmed" );
    ASSERT_EQ( theme->getBorder( StringTable->insert( "Rimmed" ) ), border );

    theme->deleteObject();

    SUCCEED();
}

TEST( GuiProfileThemeTests, ThemeDeletionDeletesItsMembers )
{
    GuiProfileTheme* theme = new GuiProfileTheme();
    theme->registerObject( "UnitTestTheme" );
    ASSERT_TRUE( Sim::findObject( "UnitTestThemeButtonProfile" ) != NULL );
    ASSERT_TRUE( Sim::findObject( "UnitTestThemeRimmedBorder" ) != NULL );

    theme->deleteObject();

    ASSERT_TRUE( Sim::findObject( "UnitTestThemeButtonProfile" ) == NULL )
        << "The theme owns its members and must delete them.";
    ASSERT_TRUE( Sim::findObject( "UnitTestThemeRimmedBorder" ) == NULL )
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
    ASSERT_EQ( button->getLeftBorder(), theme->getBorder( StringTable->insert( "Light" ) ) );
    ASSERT_EQ( button->getBottomBorder(), theme->getBorder( StringTable->insert( "Dark" ) ) );

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
    ASSERT_EQ( tooltip->mBorderDefault, theme->getBorder( StringTable->insert( "Highlight" ) ) );

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
    ASSERT_TRUE( label->mFontColor == theme->getColorForeground() );

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
    ASSERT_EQ( thumb->mBorderDefault, theme->getBorder( StringTable->insert( "BevelLight" ) ) );
    ASSERT_EQ( thumb->getRightBorder(), theme->getBorder( StringTable->insert( "BevelDark" ) ) );
    ASSERT_EQ( thumb->getBottomBorder(), theme->getBorder( StringTable->insert( "BevelDark" ) ) );

    theme->deleteObject();

    SUCCEED();
}

TEST( GuiProfileThemeTests, EmptyProfileIsTransparentAndUsesTheEmptyBorder )
{
    GuiProfileTheme* theme = new GuiProfileTheme();
    theme->registerObject( "UnitTestTheme" );

    GuiControlProfile* empty = theme->getProfile( StringTable->insert( "Empty" ) );
    ASSERT_TRUE( empty != NULL );
    // Empty keeps the base recipe's transparent fill but must wear the Empty
    // border so it never draws an edge (the base recipe leaves Rimmed on, which
    // Panel below still inherits).
    ASSERT_TRUE( empty->mFillColor == ColorI( 0, 0, 0, 0 ) );
    ASSERT_EQ( empty->mBorderDefault, theme->getBorder( StringTable->insert( "Empty" ) ) );

    // There is no Default category: a control with nothing better to wear takes
    // Empty, and the engine's own GuiDefaultProfile is the floor beneath that.
    ASSERT_TRUE( theme->getProfile( StringTable->insert( "Default" ) ) == NULL );

    theme->deleteObject();

    SUCCEED();
}

TEST( GuiProfileThemeTests, SliderAndThumbProfilesAreStamped )
{
    GuiProfileTheme* theme = new GuiProfileTheme();
    theme->registerObject( "UnitTestTheme" );

    GuiControlProfile* slider = theme->getProfile( StringTable->insert( "Slider" ) );
    ASSERT_TRUE( slider != NULL ) << "The theme must stamp a Slider (groove) profile.";
    ASSERT_STREQ( slider->mCategory, "Slider" );

    GuiControlProfile* thumb = theme->getProfile( StringTable->insert( "SliderThumb" ) );
    ASSERT_TRUE( thumb != NULL ) << "The theme must stamp a SliderThumb profile.";
    ASSERT_STREQ( thumb->mCategory, "SliderThumb" );
    ASSERT_TRUE( thumb->mFillColorSL == theme->getColorAccent() );
    ASSERT_EQ( thumb->mBorderDefault, theme->getBorder( StringTable->insert( "Light" ) ) );
    ASSERT_EQ( thumb->getRightBorder(), theme->getBorder( StringTable->insert( "Dark" ) ) );

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
    GuiBorderProfile* bright = theme->getBorder( StringTable->insert( "Light" ) );
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
    GuiBorderProfile* loadedBright = loaded->getBorder( StringTable->insert( "Light" ) );
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

//-----------------------------------------------------------------------------
// Script bindings: the console API the future Profile Editor drives.
//-----------------------------------------------------------------------------

TEST( GuiProfileThemeTests, ScriptBindingsExposeThemeApi )
{
    GuiProfileTheme* theme = new GuiProfileTheme();
    theme->registerObject( "UnitTestTheme" );

    // getProfile returns the default member for a category.
    GuiControlProfile* button = theme->getProfile( StringTable->insert( "Button" ) );
    ASSERT_EQ( dAtoi( Con::executef( theme, 2, "getProfile", "Button" ) ), (S32)button->getId() );

    // getBorder returns the border member for a border category.
    GuiBorderProfile* bright = theme->getBorder( StringTable->insert( "Light" ) );
    ASSERT_EQ( dAtoi( Con::executef( theme, 2, "getBorder", "Light" ) ), (S32)bright->getId() );

    // The category tables are enumerable for the editor.
    ASSERT_TRUE( dStrstr( Con::executef( theme, 1, "getCategoryNames" ), (const char*)"Button" ) != NULL );
    ASSERT_TRUE( dStrstr( Con::executef( theme, 1, "getBorderCategoryNames" ), (const char*)"Rimmed" ) != NULL );

    // Script field assignment marks an override; resetField re-derives.
    Con::evaluate( "UnitTestThemeButtonProfile.fillColor = \"9 8 7 6\";" );
    ASSERT_TRUE( dAtob( Con::executef( theme, 3, "isFieldOverridden", "UnitTestThemeButtonProfile", "fillColor" ) ) );
    ASSERT_TRUE( button->mFillColor == ColorI( 9, 8, 7, 6 ) );
    Con::executef( theme, 3, "resetField", "UnitTestThemeButtonProfile", "fillColor" );
    ASSERT_FALSE( dAtob( Con::executef( theme, 3, "isFieldOverridden", "UnitTestThemeButtonProfile", "fillColor" ) ) );
    ASSERT_TRUE( button->mFillColor == theme->getColorAccent() );

    // Extra profiles can be created and removed from script.
    const S32 extraId = dAtoi( Con::executef( theme, 2, "createProfile", "Button" ) );
    ASSERT_TRUE( extraId > 0 );
    ASSERT_EQ( theme->getProfileCount(), GuiProfileTheme::getCategoryCount() + 1 );
    ASSERT_TRUE( dAtob( Con::executef( theme, 2, "removeProfile", "UnitTestThemeButtonProfile2" ) ) );
    ASSERT_EQ( theme->getProfileCount(), GuiProfileTheme::getCategoryCount() );

    // The color helpers are exposed for the editor.
    ASSERT_STREQ( Con::executef( theme, 3, "adjustValue", "210 90 30 128", "-20" ), "159 68 23 128" );
    ASSERT_STREQ( Con::executef( theme, 3, "setAlpha", "10 20 30 255", "100" ), "10 20 30 100" );

    theme->deleteObject();

    SUCCEED();
}

//-----------------------------------------------------------------------------
// renameTheme: renames the theme and every member following the
// <ThemeName><Suffix> pattern, updating overridden border-name references so
// they keep pointing at this theme's borders. All-or-nothing: any name
// collision refuses the whole rename.
//-----------------------------------------------------------------------------

TEST( GuiProfileThemeTests, RenameThemeRenamesThemeAndDefaultMembers )
{
    GuiProfileTheme* theme = new GuiProfileTheme();
    theme->registerObject( "UnitTestThemeA" );

    ASSERT_TRUE( theme->renameTheme( "UnitTestThemeB" ) );
    ASSERT_STREQ( theme->getName(), "UnitTestThemeB" );

    // Defaults follow the <ThemeName><Suffix> pattern; old names are freed.
    ASSERT_EQ( (SimObject*)theme->getProfile( StringTable->insert( "Button" ) ),
               Sim::findObject( "UnitTestThemeBButtonProfile" ) );
    // "Default" is not a border category either, so this asked nothing of the
    // rename; Rimmed is the border every base-recipe profile actually wears.
    ASSERT_EQ( (SimObject*)theme->getBorder( StringTable->insert( "Rimmed" ) ),
               Sim::findObject( "UnitTestThemeBRimmedBorder" ) );
    ASSERT_TRUE( Sim::findObject( "UnitTestThemeAButtonProfile" ) == NULL );
    ASSERT_TRUE( Sim::findObject( "UnitTestThemeARimmedBorder" ) == NULL );

    theme->deleteObject();

    SUCCEED();
}

TEST( GuiProfileThemeTests, RenameThemeUpdatesOverriddenBorderSideReferences )
{
    GuiProfileTheme* theme = new GuiProfileTheme();
    theme->registerObject( "UnitTestThemeA" );

    // Override the Button member's left border to another theme border by name.
    GuiControlProfile* button = theme->getProfile( StringTable->insert( "Button" ) );
    StringTableEntry borderLeft = StringTable->insert( "borderLeft" );
    button->setDataField( borderLeft, NULL, "UnitTestThemeADarkBorder" );
    ASSERT_TRUE( button->isThemeFieldOverridden( borderLeft ) );

    ASSERT_TRUE( theme->renameTheme( "UnitTestThemeB" ) );

    // The stored reference follows the border's new name; the override survives.
    ASSERT_STREQ( button->getDataField( borderLeft, NULL ), "UnitTestThemeBDarkBorder" );
    ASSERT_TRUE( button->isThemeFieldOverridden( borderLeft ) );
    ASSERT_EQ( button->getLeftBorder(), theme->getBorder( StringTable->insert( "Dark" ) ) );

    theme->deleteObject();

    SUCCEED();
}

TEST( GuiProfileThemeTests, RenameThemeRenamesPatternNamedExtrasAndKeepsCustomNames )
{
    GuiProfileTheme* theme = new GuiProfileTheme();
    theme->registerObject( "UnitTestThemeA" );

    GuiControlProfile* patternExtra = theme->createProfile( "Button", NULL );
    ASSERT_STREQ( patternExtra->getName(), "UnitTestThemeAButtonProfile2" );
    GuiControlProfile* customExtra = theme->createProfile( "Button", "UnitTestCustomButton" );
    ASSERT_TRUE( customExtra != NULL );

    ASSERT_TRUE( theme->renameTheme( "UnitTestThemeB" ) );

    ASSERT_STREQ( patternExtra->getName(), "UnitTestThemeBButtonProfile2" )
        << "Extras named after the theme must follow the rename.";
    ASSERT_STREQ( customExtra->getName(), "UnitTestCustomButton" )
        << "Custom-named extras must keep their names.";

    theme->deleteObject();

    SUCCEED();
}

TEST( GuiProfileThemeTests, RenameThemeRefusesNameCollision )
{
    GuiProfileTheme* theme = new GuiProfileTheme();
    theme->registerObject( "UnitTestThemeA" );

    // Block one of the member target names.
    SimObject* blocker = new SimObject();
    blocker->registerObject( "UnitTestThemeBButtonProfile" );

    ASSERT_FALSE( theme->renameTheme( "UnitTestThemeB" ) )
        << "A rename into any occupied name must be refused.";
    ASSERT_STREQ( theme->getName(), "UnitTestThemeA" )
        << "A refused rename must leave every name untouched.";
    ASSERT_EQ( (SimObject*)theme->getProfile( StringTable->insert( "Button" ) ),
               Sim::findObject( "UnitTestThemeAButtonProfile" ) );

    blocker->deleteObject();
    theme->deleteObject();

    SUCCEED();
}

TEST( GuiProfileThemeTests, RenameThemeNamesMembersOfPreviouslyUnnamedTheme )
{
    GuiProfileTheme* theme = new GuiProfileTheme();
    theme->registerObject();
    ASSERT_TRUE( theme->getProfile( StringTable->insert( "Button" ) )->getName() == NULL );

    ASSERT_TRUE( theme->renameTheme( "UnitTestThemeC" ) );

    ASSERT_STREQ( theme->getName(), "UnitTestThemeC" );
    ASSERT_EQ( (SimObject*)theme->getProfile( StringTable->insert( "Button" ) ),
               Sim::findObject( "UnitTestThemeCButtonProfile" ) );

    theme->deleteObject();

    SUCCEED();
}

//-----------------------------------------------------------------------------
// Inspector integration: a GuiInspectorField pointed at a themed member knows
// the member's theme, whether its field is overridden, and can clear the
// override so the value re-derives from the theme.
//-----------------------------------------------------------------------------

TEST( GuiProfileThemeTests, InspectorFieldTracksThemeOverrideAndResets )
{
    GuiProfileTheme* theme = new GuiProfileTheme();
    theme->registerObject( "UnitTestTheme" );
    GuiControlProfile* button = theme->getProfile( StringTable->insert( "Button" ) );

    // An unregistered field is enough: the theme helpers don't need a canvas.
    GuiInspectorField* field = new GuiInspectorField();
    field->setTarget( button );
    AbstractClassRep::Field* fillColorField = const_cast<AbstractClassRep::Field*>(
        button->getClassRep()->findField( StringTable->insert( "fillColor" ) ) );
    ASSERT_TRUE( fillColorField != NULL );
    field->setInspectorField( fillColorField );

    ASSERT_EQ( field->getTargetTheme(), theme );
    ASSERT_FALSE( field->isThemeOverridden() );

    button->setDataField( StringTable->insert( "fillColor" ), NULL, "9 8 7 6" );
    ASSERT_TRUE( field->isThemeOverridden() );

    field->resetToThemeDefault();
    ASSERT_FALSE( field->isThemeOverridden() );
    ASSERT_TRUE( button->mFillColor == theme->getColorAccent() )
        << "Clearing the override must re-derive the field from the theme.";

    delete field;
    theme->deleteObject();

    SUCCEED();
}

TEST( GuiProfileThemeTests, InspectorFieldIgnoresNonThemedTargets )
{
    GuiControlProfile* standalone = new GuiControlProfile();
    standalone->registerObject();

    GuiInspectorField* field = new GuiInspectorField();
    field->setTarget( standalone );

    ASSERT_EQ( field->getTargetTheme(), (GuiProfileTheme*)NULL );
    ASSERT_FALSE( field->isThemeOverridden() );

    delete field;
    standalone->deleteObject();

    SUCCEED();
}

TEST( GuiProfileThemeTests, RenameThemeIsExposedToScript )
{
    GuiProfileTheme* theme = new GuiProfileTheme();
    theme->registerObject( "UnitTestThemeA" );

    ASSERT_TRUE( dAtob( Con::executef( theme, 2, "renameTheme", "UnitTestThemeB" ) ) );
    ASSERT_STREQ( theme->getName(), "UnitTestThemeB" );

    theme->deleteObject();

    SUCCEED();
}

//-----------------------------------------------------------------------------
// The named-border palette: a theme generates the six primitives (Light replaces
// the old Bright) plus the descriptive extended borders, all visible in the
// editor and theme-tracked.
//-----------------------------------------------------------------------------

TEST( GuiProfileThemeTests, ThemeGeneratesTheNamedBorderPalette )
{
    GuiProfileTheme* theme = new GuiProfileTheme();
    theme->registerObject( "UnitTestTheme" );

    const char* names[] = { "Empty", "Rimmed", "Thick", "Light", "Dark", "Padded",
        "Highlight", "PaddedRim", "BevelLight", "BevelDark", "PaddedLight",
        "PaddedDark", "RimmedExpander", "CondenserLight", "CondenserDark" };
    const S32 count = sizeof( names ) / sizeof( names[0] );
    ASSERT_EQ( GuiProfileTheme::getBorderCategoryCount(), count );
    for ( S32 i = 0; i < count; ++i )
        ASSERT_TRUE( theme->getBorder( StringTable->insert( names[i] ) ) != NULL ) << names[i];

    // The old Bright name and old category borders are gone.
    ASSERT_TRUE( theme->getBorder( StringTable->insert( "Bright" ) ) == NULL );
    ASSERT_TRUE( theme->getBorder( StringTable->insert( "ButtonBright" ) ) == NULL );

    theme->deleteObject();

    SUCCEED();
}

TEST( GuiProfileThemeTests, ExtendedBordersAreThemeMembersAndAppearInTheEditor )
{
    GuiProfileTheme* theme = new GuiProfileTheme();
    theme->registerObject( "UnitTestTheme" );

    // An extended border is a real member with the expected values...
    GuiBorderProfile* paddedRim = theme->getBorder( StringTable->insert( "PaddedRim" ) );
    ASSERT_TRUE( paddedRim != NULL );
    for ( S32 i = 0; i < 4; ++i )
        ASSERT_EQ( paddedRim->mPadding[i], 10 );

    // ...the Tooltip profile wires to the highlight-colored Highlight border...
    GuiControlProfile* tooltip = theme->getProfile( StringTable->insert( "Tooltip" ) );
    ASSERT_EQ( tooltip->mBorderDefault, theme->getBorder( StringTable->insert( "Highlight" ) ) );
    ASSERT_TRUE( tooltip->mBorderDefault->mBorderColor[0] == theme->getColorHighlight() );

    // ...and the extended borders are listed for the editor, same as the base six.
    const char* names = Con::executef( theme, 1, "getBorderCategoryNames" );
    ASSERT_TRUE( dStrstr( names, (const char*)"PaddedRim" ) != NULL );
    ASSERT_TRUE( dStrstr( names, (const char*)"Highlight" ) != NULL );
    ASSERT_TRUE( dStrstr( names, (const char*)"Rimmed" ) != NULL );

    theme->deleteObject();

    SUCCEED();
}

TEST( GuiProfileThemeTests, SixBorderRecipesUseExpectedValues )
{
    GuiProfileTheme* theme = new GuiProfileTheme();
    theme->registerObject( "UnitTestTheme" );   // constructor borderSize == 1

    GuiBorderProfile* empty  = theme->getBorder( StringTable->insert( "Empty" ) );
    GuiBorderProfile* rimmed = theme->getBorder( StringTable->insert( "Rimmed" ) );
    GuiBorderProfile* thick  = theme->getBorder( StringTable->insert( "Thick" ) );
    GuiBorderProfile* bright = theme->getBorder( StringTable->insert( "Light" ) );
    GuiBorderProfile* dark   = theme->getBorder( StringTable->insert( "Dark" ) );
    GuiBorderProfile* padded = theme->getBorder( StringTable->insert( "Padded" ) );

    for ( S32 i = 0; i < 4; ++i )
    {
        ASSERT_EQ( empty->mBorder[i], 0 );
        ASSERT_EQ( empty->mMargin[i], 0 );
        ASSERT_EQ( empty->mPadding[i], 0 );

        ASSERT_EQ( rimmed->mBorder[i], theme->getBorderSize() );        // 1 * borderSize
        ASSERT_EQ( thick->mBorder[i], 2 * theme->getBorderSize() );

        ASSERT_EQ( bright->mBorder[i], theme->getBorderSize() );
        ASSERT_TRUE( bright->mBorderColor[i] == ColorI( 255, 255, 255, 50 ) );
        ASSERT_EQ( dark->mBorder[i], theme->getBorderSize() );
        ASSERT_TRUE( dark->mBorderColor[i] == ColorI( 0, 0, 0, 50 ) );

        ASSERT_EQ( padded->mBorder[i], 0 );
        ASSERT_EQ( padded->mMargin[i], 0 );
        ASSERT_EQ( padded->mPadding[i], 10 );
    }
    ASSERT_TRUE( rimmed->mBorderColor[0] == theme->getColorBackground() );

    // borderSize scales rim widths but never padding.
    theme->setDataField( StringTable->insert( "borderSize" ), NULL, "3" );
    ASSERT_EQ( rimmed->mBorder[0], 3 );
    ASSERT_EQ( thick->mBorder[0], 6 );
    ASSERT_EQ( padded->mPadding[0], 10 );

    theme->deleteObject();

    SUCCEED();
}

//-----------------------------------------------------------------------------
// Custom borders: single-use, user-authored borders owned by the theme as
// extras. They are not category members, keep their own field values, are
// hidden from the category list, and round-trip through Taml.
//-----------------------------------------------------------------------------

TEST( GuiProfileThemeTests, CreateAndRemoveCustomBorder )
{
    GuiProfileTheme* theme = new GuiProfileTheme();
    theme->registerObject( "UnitTestTheme" );

    ASSERT_EQ( theme->getExtraBorders().size(), 0 );

    GuiBorderProfile* custom = theme->createBorder( "UnitTestThemeButtonProfiletopCustomBorder" );
    ASSERT_TRUE( custom != NULL );
    ASSERT_TRUE( custom->mIsCustom );
    ASSERT_EQ( custom->getTheme(), (GuiProfileTheme*)NULL )
        << "Custom borders are not theme members: they serialize standalone.";
    ASSERT_EQ( theme->getExtraBorders().size(), 1 );
    ASSERT_EQ( (SimObject*)custom, Sim::findObject( "UnitTestThemeButtonProfiletopCustomBorder" ) );

    // Custom borders never appear in the border category list.
    ASSERT_TRUE( dStrstr( Con::executef( theme, 1, "getBorderCategoryNames" ), (const char*)"CustomBorder" ) == NULL );

    ASSERT_TRUE( theme->removeBorder( custom ) );
    ASSERT_EQ( theme->getExtraBorders().size(), 0 );
    ASSERT_TRUE( Sim::findObject( "UnitTestThemeButtonProfiletopCustomBorder" ) == NULL );

    theme->deleteObject();

    SUCCEED();
}

TEST( GuiProfileThemeTests, ThemeDeletionDeletesCustomBorders )
{
    GuiProfileTheme* theme = new GuiProfileTheme();
    theme->registerObject( "UnitTestTheme" );
    theme->createBorder( "UnitTestThemeCustomA" );
    ASSERT_TRUE( Sim::findObject( "UnitTestThemeCustomA" ) != NULL );

    theme->deleteObject();

    ASSERT_TRUE( Sim::findObject( "UnitTestThemeCustomA" ) == NULL )
        << "The theme owns its custom borders and must delete them.";

    SUCCEED();
}

TEST( GuiProfileThemeTests, CustomBorderTamlRoundTripResolvesReferences )
{
    const char* fileName = "unitTestGuiProfileThemeCustomBorder.taml";

    GuiProfileTheme* theme = new GuiProfileTheme();
    theme->registerObject( "UnitTestTheme" );

    // A custom border with distinctive values, referenced by the Button member
    // both as its default border (object reference) and as its top border
    // (name reference).
    GuiBorderProfile* custom = theme->createBorder( "UnitTestThemeButtonProfiledefaultCustomBorder" );
    custom->setDataField( StringTable->insert( "padding" ), NULL, "7" );
    custom->setDataField( StringTable->insert( "margin" ), NULL, "3" );

    GuiControlProfile* button = theme->getProfile( StringTable->insert( "Button" ) );
    button->setDataField( StringTable->insert( "borderDefault" ), NULL, "UnitTestThemeButtonProfiledefaultCustomBorder" );
    button->setDataField( StringTable->insert( "borderTop" ), NULL, "UnitTestThemeButtonProfiledefaultCustomBorder" );
    ASSERT_EQ( button->mBorderDefault, custom );
    ASSERT_EQ( button->getTopBorder(), custom );

    Taml taml;
    ASSERT_TRUE( taml.write( theme, fileName ) );
    theme->deleteObject();
    ASSERT_TRUE( Sim::findObject( "UnitTestThemeButtonProfiledefaultCustomBorder" ) == NULL );

    GuiProfileTheme* loaded = taml.read<GuiProfileTheme>( fileName );
    ASSERT_TRUE( loaded != NULL );

    // The custom border returns, flagged, with its values, owned as an extra.
    GuiBorderProfile* loadedCustom = dynamic_cast<GuiBorderProfile*>( Sim::findObject( "UnitTestThemeButtonProfiledefaultCustomBorder" ) );
    ASSERT_TRUE( loadedCustom != NULL );
    ASSERT_TRUE( loadedCustom->mIsCustom );
    ASSERT_EQ( loadedCustom->mPadding[0], 7 );
    ASSERT_EQ( loadedCustom->mMargin[0], 3 );
    ASSERT_EQ( loaded->getExtraBorders().size(), 1 );

    // Both the object reference and the name reference resolve to it.
    GuiControlProfile* loadedButton = loaded->getProfile( StringTable->insert( "Button" ) );
    ASSERT_EQ( loadedButton->mBorderDefault, loadedCustom );
    ASSERT_EQ( loadedButton->getTopBorder(), loadedCustom );

    loaded->deleteObject();
    Platform::fileDelete( fileName );

    SUCCEED();
}

TEST( GuiProfileThemeTests, ProfileSideBorderReresolvesAndEmptyFallsBackToDefault )
{
    GuiProfileTheme* theme = new GuiProfileTheme();
    theme->registerObject( "UnitTestTheme" );

    GuiControlProfile* button = theme->getProfile( StringTable->insert( "Button" ) );
    GuiBorderProfile* dark   = theme->getBorder( StringTable->insert( "Dark" ) );
    GuiBorderProfile* rimmed = theme->getBorder( StringTable->insert( "Rimmed" ) );

    // Writing a side border name on a live profile must re-resolve the cached
    // side pointer the renderer reads (getTopBorder), not leave it stale.
    button->setDataField( StringTable->insert( "borderTop" ), NULL, dark->getName() );
    ASSERT_EQ( button->getTopBorder(), dark );

    // An empty side name falls back to the current default border.
    button->setDataField( StringTable->insert( "borderDefault" ), NULL, rimmed->getName() );
    button->setDataField( StringTable->insert( "borderTop" ), NULL, "" );
    ASSERT_EQ( button->getTopBorder(), rimmed )
        << "An empty side border must fall back to the default border.";

    theme->deleteObject();

    SUCCEED();
}

TEST( GuiProfileThemeTests, FreshlyStampedProfileResolvesFallbackSidesToDefaultBorder )
{
    // Regression: render and layout read the raw cached side pointers
    // (getLeftBorder(), etc.), never the lazy resolver. The recipe stamp writes
    // raw members and never triggers onStaticModified -- the path that normally
    // repopulates those pointers -- so it must resolve the sides itself. Before
    // the fix a freshly stamped profile whose sides fall back to a non-empty
    // default border was left with NULL side pointers, so it rendered borderless
    // on first display and only gained its border after some later field edit.
    GuiProfileTheme* theme = new GuiProfileTheme();
    theme->registerObject( "UnitTestTheme" );

    // Label adds nothing to the base recipe but an alignment, so borderDefault is
    // Rimmed and every side is empty: all four sides must resolve to the Rimmed
    // border with no field writes at all. (Empty is no longer usable here since
    // its recipe wears the Empty border on purpose.)
    GuiControlProfile* label = theme->getProfile( StringTable->insert( "Label" ) );
    ASSERT_TRUE( label != NULL );
    GuiBorderProfile* rimmed = theme->getBorder( StringTable->insert( "Rimmed" ) );
    ASSERT_EQ( label->mBorderDefault, rimmed );
    ASSERT_EQ( label->getLeftBorder(), rimmed )
        << "A fallback side must be resolved by the stamp, not left NULL.";
    ASSERT_EQ( label->getRightBorder(), rimmed );
    ASSERT_EQ( label->getTopBorder(), rimmed );
    ASSERT_EQ( label->getBottomBorder(), rimmed );

    // ScrollThumb mixes fallback and named sides, and its recipe changes the
    // default border (the base recipe sets Rimmed, then the ScrollThumb recipe
    // sets its bevel): the fallback sides must track the *final* default.
    GuiControlProfile* thumb = theme->getProfile( StringTable->insert( "ScrollThumb" ) );
    GuiBorderProfile* bevel = theme->getBorder( StringTable->insert( "BevelLight" ) );
    GuiBorderProfile* bevelDark = theme->getBorder( StringTable->insert( "BevelDark" ) );
    ASSERT_EQ( thumb->getLeftBorder(), bevel )
        << "A fallback side must resolve to the recipe's final default border.";
    ASSERT_EQ( thumb->getTopBorder(), bevel );
    ASSERT_EQ( thumb->getRightBorder(), bevelDark );
    ASSERT_EQ( thumb->getBottomBorder(), bevelDark );

    theme->deleteObject();

    SUCCEED();
}

#endif // TORQUE_SHIPPING
