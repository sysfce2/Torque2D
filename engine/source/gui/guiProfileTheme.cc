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

#ifndef _GUITYPES_H_
#include "gui/guiTypes.h"
#endif

#ifndef _CONSOLETYPES_H_
#include "console/consoleTypes.h"
#endif

#ifndef _SIMBASE_H_
#include "sim/simBase.h"
#endif

#ifndef _MMATHFN_H_
#include "math/mMathFn.h"
#endif

#include "gui/guiProfileTheme_ScriptBinding.h"

//-----------------------------------------------------------------------------

IMPLEMENT_CONOBJECT(GuiProfileTheme);

//-----------------------------------------------------------------------------
// GuiThemeMembership override-list persistence helpers.
//-----------------------------------------------------------------------------

void GuiThemeMembership::parseOverrideList(const char* list)
{
    if (list == NULL || *list == '\0')
        return;

    char buffer[2048];
    dStrncpy(buffer, list, sizeof(buffer) - 1);
    buffer[sizeof(buffer) - 1] = '\0';

    char* token = dStrtok(buffer, " \t\n");
    while (token != NULL)
    {
        markOverride(StringTable->insert(token));
        token = dStrtok(NULL, " \t\n");
    }
}

const char* GuiThemeMembership::formatOverrideList() const
{
    if (mOverrides.size() == 0)
        return "";

    S32 bufferSize = 1;
    for (S32 i = 0; i < mOverrides.size(); ++i)
        bufferSize += dStrlen(mOverrides[i]) + 1;

    char* buffer = Con::getReturnBuffer(bufferSize);
    char* out = buffer;
    for (S32 i = 0; i < mOverrides.size(); ++i)
    {
        if (i > 0)
            *out++ = ' ';
        dStrcpy(out, mOverrides[i]);
        out += dStrlen(mOverrides[i]);
    }
    *out = '\0';

    return buffer;
}

//-----------------------------------------------------------------------------
// Category recipes. Each stamp function derives every themed field of one
// member from the theme's values, skipping fields the member has explicitly
// overridden. Recipes write raw members directly (never setDataField), so
// stamping cannot mark overrides.
//-----------------------------------------------------------------------------

/// Stamp a member field unless the member has explicitly overridden it.
#define STAMP_FIELD(object, fieldName, member, value) \
    if (!(object)->isThemeFieldOverridden(StringTable->insert(fieldName))) (object)->member = (value)

// Shorthand for the derivation helpers inside recipes.
static inline ColorI adj(const ColorI& color, F32 percent) { return GuiProfileTheme::adjustValue(color, percent); }
static inline ColorI alphaOf(const ColorI& color, S32 alpha) { return GuiProfileTheme::setAlpha(color, alpha); }

//-----------------------------------------------------------------------------
// Border recipes. Every border starts from the Default border recipe (the
// values GuiDefaultBorderProfile provides in AppCore's script profiles, which
// every script-created border copied at construction) and overrides what its
// script source set explicitly. State order is [normal, HL, SL, NA].
//-----------------------------------------------------------------------------

struct BorderRecipe
{
    S32 margin[4];
    S32 border[4];
    ColorI borderColor[4];
    S32 padding[4];
    bool underfill;
    // Every recipe's border widths are authored as reference thicknesses at
    // scale 1 and multiplied by this when applied, so the theme's borderSize
    // scales all borders together (0 = none, 1 = as authored, 2 = doubled).
    S32 borderScale;
};

static void set4(S32* values, S32 v0, S32 v1, S32 v2, S32 v3) { values[0] = v0; values[1] = v1; values[2] = v2; values[3] = v3; }
static void set4(S32* values, S32 value) { set4(values, value, value, value, value); }
static void set4(ColorI* values, const ColorI& v0, const ColorI& v1, const ColorI& v2, const ColorI& v3) { values[0] = v0; values[1] = v1; values[2] = v2; values[3] = v3; }
static void set4(ColorI* values, const ColorI& value) { set4(values, value, value, value, value); }

static BorderRecipe baseBorderRecipe(GuiProfileTheme* theme)
{
    const ColorI& bg = theme->getColorBackground();

    BorderRecipe recipe;
    set4(recipe.margin, 0);
    // Reference thickness 1: the plain Default border (worn by TextEdit, Label,
    // Empty, Overlay) becomes 1 * borderSize, so raising borderSize gives inputs
    // a visible, scaling border. Categories that want a heavier border override
    // this with their own reference (Bright/Dark 2, Button 3, ...).
    set4(recipe.border, 1);
    set4(recipe.borderColor, bg, adj(bg, 10), adj(bg, 10), alphaOf(bg, 100));
    set4(recipe.padding, 0);
    recipe.underfill = true;
    recipe.borderScale = theme->getBorderSize();
    return recipe;
}

static void applyBorderRecipe(GuiBorderProfile* border, const BorderRecipe& recipe)
{
    static const char* marginFields[4] = { "margin", "marginHL", "marginSL", "marginNA" };
    static const char* borderFields[4] = { "border", "borderHL", "borderSL", "borderNA" };
    static const char* colorFields[4] = { "borderColor", "borderColorHL", "borderColorSL", "borderColorNA" };
    static const char* paddingFields[4] = { "padding", "paddingHL", "paddingSL", "paddingNA" };

    for (S32 i = 0; i < 4; ++i)
    {
        if (!border->isThemeFieldOverridden(StringTable->insert(marginFields[i])))
            border->mMargin[i] = recipe.margin[i];
        if (!border->isThemeFieldOverridden(StringTable->insert(borderFields[i])))
            border->mBorder[i] = recipe.border[i] * recipe.borderScale;
        if (!border->isThemeFieldOverridden(StringTable->insert(colorFields[i])))
            border->mBorderColor[i] = recipe.borderColor[i];
        if (!border->isThemeFieldOverridden(StringTable->insert(paddingFields[i])))
            border->mPadding[i] = recipe.padding[i];
    }

    STAMP_FIELD(border, "underfill", mUnderfill, recipe.underfill);
}

static void stampEmptyBorder(GuiProfileTheme* theme, GuiBorderProfile* border)
{
    // Nothing at all: no margin, no border, no padding.
    BorderRecipe recipe = baseBorderRecipe(theme);
    set4(recipe.margin, 0);
    set4(recipe.border, 0);
    set4(recipe.padding, 0);
    applyBorderRecipe(border, recipe);
}

static void stampRimmedBorder(GuiProfileTheme* theme, GuiBorderProfile* border)
{
    // A single rim at the theme's border size (the base recipe: reference
    // width 1, no margin or padding, theme-derived color). Invisible at
    // borderSize 0, one pixel at 1, and so on.
    applyBorderRecipe(border, baseBorderRecipe(theme));
}

static void stampThickBorder(GuiProfileTheme* theme, GuiBorderProfile* border)
{
    // Twice the theme's border size, otherwise a plain rim.
    BorderRecipe recipe = baseBorderRecipe(theme);
    set4(recipe.border, 2);
    applyBorderRecipe(border, recipe);
}

static void stampBrightBorder(GuiProfileTheme* theme, GuiBorderProfile* border)
{
    // A border-size rim in translucent white: the light edge of a bevel.
    BorderRecipe recipe = baseBorderRecipe(theme);
    set4(recipe.border, 1);
    set4(recipe.borderColor, ColorI(255, 255, 255, 50));
    applyBorderRecipe(border, recipe);
}

static void stampDarkBorder(GuiProfileTheme* theme, GuiBorderProfile* border)
{
    // A border-size rim in translucent black: the shadow edge of a bevel.
    BorderRecipe recipe = baseBorderRecipe(theme);
    set4(recipe.border, 1);
    set4(recipe.borderColor, ColorI(0, 0, 0, 50));
    applyBorderRecipe(border, recipe);
}

static void stampPaddedBorder(GuiProfileTheme* theme, GuiBorderProfile* border)
{
    // No rim, just a fixed 10px inset on every side for content padding.
    // Padding is not scaled by borderSize, so this pads even at borderSize 0.
    BorderRecipe recipe = baseBorderRecipe(theme);
    set4(recipe.margin, 0);
    set4(recipe.border, 0);
    set4(recipe.padding, 10);
    applyBorderRecipe(border, recipe);
}

//-----------------------------------------------------------------------------
// Profile recipes. Every profile starts from the Default recipe and overrides
// what its category needs; borders are wired to the theme's border members.
//-----------------------------------------------------------------------------

/// Wire a profile's border slots to theme border members (by category name),
/// respecting overrides. A NULL side leaves the side falling back to
/// borderDefault, matching the engine's lazy side-border resolution.
static void stampProfileBorders(GuiProfileTheme* theme, GuiControlProfile* profile,
    const char* defaultBorder, const char* left, const char* right, const char* top, const char* bottom)
{
    if (!profile->isThemeFieldOverridden(StringTable->insert("borderDefault")))
    {
        GuiBorderProfile* border = (defaultBorder != NULL) ? theme->getBorder(StringTable->insert(defaultBorder)) : NULL;
        if (profile->mBorderDefault != border)
        {
            if (profile->mBorderDefault != NULL)
                profile->clearNotify(profile->mBorderDefault);
            profile->mBorderDefault = border;
            if (border != NULL)
                profile->deleteNotify(border);
        }
    }

    if (!profile->isThemeFieldOverridden(StringTable->insert("borderLeft")))
    {
        GuiBorderProfile* border = (left != NULL) ? theme->getBorder(StringTable->insert(left)) : NULL;
        profile->mLeftProfileName = (border != NULL) ? border->getName() : NULL;
        profile->setLeftProfile(border);
    }

    if (!profile->isThemeFieldOverridden(StringTable->insert("borderRight")))
    {
        GuiBorderProfile* border = (right != NULL) ? theme->getBorder(StringTable->insert(right)) : NULL;
        profile->mRightProfileName = (border != NULL) ? border->getName() : NULL;
        profile->setRightProfile(border);
    }

    if (!profile->isThemeFieldOverridden(StringTable->insert("borderTop")))
    {
        GuiBorderProfile* border = (top != NULL) ? theme->getBorder(StringTable->insert(top)) : NULL;
        profile->mTopProfileName = (border != NULL) ? border->getName() : NULL;
        profile->setTopProfile(border);
    }

    if (!profile->isThemeFieldOverridden(StringTable->insert("borderBottom")))
    {
        GuiBorderProfile* border = (bottom != NULL) ? theme->getBorder(StringTable->insert(bottom)) : NULL;
        profile->mBottomProfileName = (border != NULL) ? border->getName() : NULL;
        profile->setBottomProfile(border);
    }
}

/// Override a profile's font face and size (a signed delta from the theme's
/// base font size), respecting user overrides. Called after stampDefaultProfile,
/// which has already set the body font at the base size. This is how the three
/// theme fonts get their roles: title on chrome, code on input/data, body on
/// content, with a size bump or trim where a category wants one.
static void stampProfileFont(GuiProfileTheme* theme, GuiControlProfile* profile, StringTableEntry face, S32 sizeDelta)
{
    STAMP_FIELD(profile, "fontType", mFontType, face);
    STAMP_FIELD(profile, "fontSize", mFontSize, theme->getFontSize() + sizeDelta);
}

/// The Default recipe: every category recipe starts from this, so every
/// themed field of every member is always derived from something.
static void stampDefaultProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& text = theme->getColorForeground();
    const ColorI& accent = theme->getColorAccent();

    STAMP_FIELD(profile, "tab", mTabable, false);
    STAMP_FIELD(profile, "canKeyFocus", mCanKeyFocus, false);

    STAMP_FIELD(profile, "fillColor", mFillColor, ColorI(0, 0, 0, 0));
    STAMP_FIELD(profile, "fillColorHL", mFillColorHL, ColorI(0, 0, 0, 0));
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, ColorI(0, 0, 0, 0));
    STAMP_FIELD(profile, "fillColorNA", mFillColorNA, ColorI(0, 0, 0, 0));
    STAMP_FIELD(profile, "fillColorTextSL", mFillColorTextSL, theme->getColorHighlight());

    STAMP_FIELD(profile, "fontType", mFontType, theme->getFontBody());
    STAMP_FIELD(profile, "fontDirectory", mFontDirectory, theme->getFontDirectory());
    STAMP_FIELD(profile, "fontSize", mFontSize, theme->getFontSize());
    STAMP_FIELD(profile, "fontCharset", mFontCharset, TGE_ANSI_CHARSET);

    STAMP_FIELD(profile, "fontColor", mFontColor, text);
    STAMP_FIELD(profile, "fontColorHL", mFontColorHL, adj(text, 10));
    STAMP_FIELD(profile, "fontColorSL", mFontColorSL, text);
    STAMP_FIELD(profile, "fontColorNA", mFontColorNA, alphaOf(text, 100));
    STAMP_FIELD(profile, "fontColorLink", mFontColorLink, accent);
    STAMP_FIELD(profile, "fontColorLinkHL", mFontColorLinkHL, adj(accent, 10));
    STAMP_FIELD(profile, "fontColorTextSL", mFontColorTextSL, theme->getColorBackground());

    STAMP_FIELD(profile, "align", mAlignment, AlignmentType::CenterAlign);
    STAMP_FIELD(profile, "vAlign", mVAlignment, VertAlignmentType::MiddleVAlign);
    STAMP_FIELD(profile, "cursorColor", mCursorColor, ColorI(0, 0, 0, 255));
    STAMP_FIELD(profile, "textOffset", mTextOffset, Point2I(0, 0));

    stampProfileBorders(theme, profile, "Rimmed", NULL, NULL, NULL, NULL);
}

static void stampEmptyProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    stampDefaultProfile(theme, profile);
}

static void stampTooltipProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    stampDefaultProfile(theme, profile);
    stampProfileFont(theme, profile, theme->getFontBody(), -2);
    STAMP_FIELD(profile, "fillColor", mFillColor, alphaOf(theme->getColorBackground(), 220));
    STAMP_FIELD(profile, "fontColor", mFontColor, theme->getColorHighlight());
    stampProfileBorders(theme, profile, "Bright", NULL, NULL, NULL, NULL);
}

static void stampPanelProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& bg = theme->getColorBackground();
    stampDefaultProfile(theme, profile);
    STAMP_FIELD(profile, "fillColor", mFillColor, bg);
    STAMP_FIELD(profile, "fillColorHL", mFillColorHL, adj(bg, 10));
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, adj(bg, 15));
    STAMP_FIELD(profile, "fillColorNA", mFillColorNA, alphaOf(bg, 100));
    stampProfileBorders(theme, profile, "Bright", NULL, NULL, NULL, NULL);
}

static void stampButtonProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& bg = theme->getColorBackground();
    const ColorI& accent = theme->getColorAccent();
    stampDefaultProfile(theme, profile);
    stampProfileFont(theme, profile, theme->getFontTitle(), 0);
    STAMP_FIELD(profile, "fillColor", mFillColor, accent);
    STAMP_FIELD(profile, "fillColorHL", mFillColorHL, adj(accent, 10));
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, theme->getColorHighlight());
    STAMP_FIELD(profile, "fillColorNA", mFillColorNA, alphaOf(accent, 80));
    STAMP_FIELD(profile, "fontColor", mFontColor, bg);
    STAMP_FIELD(profile, "fontColorHL", mFontColorHL, adj(bg, 10));
    STAMP_FIELD(profile, "fontColorSL", mFontColorSL, bg);
    STAMP_FIELD(profile, "fontColorNA", mFontColorNA, alphaOf(bg, 100));
    STAMP_FIELD(profile, "canKeyFocus", mCanKeyFocus, true);
    STAMP_FIELD(profile, "tab", mTabable, true);
    stampProfileBorders(theme, profile, "Rimmed", "Bright", "Dark", "Bright", "Dark");
}

static void stampCheckBoxProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    stampButtonProfile(theme, profile);
}

static void stampRadioProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& text = theme->getColorForeground();
    const ColorI& highlight = theme->getColorHighlight();
    stampDefaultProfile(theme, profile);
    stampProfileFont(theme, profile, theme->getFontTitle(), 0);
    STAMP_FIELD(profile, "fillColor", mFillColor, text);
    STAMP_FIELD(profile, "fillColorHL", mFillColorHL, adj(text, -10));
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, theme->getColorAccent());
    STAMP_FIELD(profile, "fillColorNA", mFillColorNA, alphaOf(text, 100));
    STAMP_FIELD(profile, "fontColor", mFontColor, text);
    STAMP_FIELD(profile, "fontColorHL", mFontColorHL, adj(highlight, -10));
    STAMP_FIELD(profile, "fontColorSL", mFontColorSL, highlight);
    STAMP_FIELD(profile, "fontColorNA", mFontColorNA, alphaOf(text, 100));
    STAMP_FIELD(profile, "align", mAlignment, AlignmentType::LeftAlign);
    STAMP_FIELD(profile, "canKeyFocus", mCanKeyFocus, true);
    STAMP_FIELD(profile, "tab", mTabable, true);
    stampProfileBorders(theme, profile, "Rimmed", "Bright", "Dark", "Bright", "Dark");
}

static void stampLabelProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    stampDefaultProfile(theme, profile);
    STAMP_FIELD(profile, "align", mAlignment, AlignmentType::LeftAlign);
}

static void stampTextEditProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& bg = theme->getColorBackground();
    const ColorI& accent = theme->getColorAccent();
    stampDefaultProfile(theme, profile);
    stampProfileFont(theme, profile, theme->getFontCode(), 0);
    STAMP_FIELD(profile, "fillColor", mFillColor, accent);
    STAMP_FIELD(profile, "fillColorHL", mFillColorHL, adj(accent, 10));
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, accent);
    STAMP_FIELD(profile, "fillColorNA", mFillColorNA, adj(accent, 80));
    STAMP_FIELD(profile, "fillColorTextSL", mFillColorTextSL, theme->getColorHighlight());
    STAMP_FIELD(profile, "fontColor", mFontColor, alphaOf(bg, 220));
    STAMP_FIELD(profile, "fontColorHL", mFontColorHL, adj(bg, 10));
    STAMP_FIELD(profile, "fontColorSL", mFontColorSL, bg);
    STAMP_FIELD(profile, "fontColorNA", mFontColorNA, alphaOf(bg, 100));
    STAMP_FIELD(profile, "fontColorTextSL", mFontColorTextSL, bg);
    STAMP_FIELD(profile, "align", mAlignment, AlignmentType::LeftAlign);
    STAMP_FIELD(profile, "cursorColor", mCursorColor, theme->getColorSurface());
    STAMP_FIELD(profile, "canKeyFocus", mCanKeyFocus, true);
    STAMP_FIELD(profile, "tab", mTabable, true);
}

static void stampScrollProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    stampDefaultProfile(theme, profile);
    STAMP_FIELD(profile, "fillColor", mFillColor, theme->getColorSurface());
}

static void stampScrollTrackProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& bg = theme->getColorBackground();
    stampDefaultProfile(theme, profile);
    STAMP_FIELD(profile, "fillColor", mFillColor, bg);
    STAMP_FIELD(profile, "fillColorHL", mFillColorHL, bg);
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, bg);
    STAMP_FIELD(profile, "fillColorNA", mFillColorNA, bg);
}

static void stampScrollThumbProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& text = theme->getColorForeground();
    stampDefaultProfile(theme, profile);
    STAMP_FIELD(profile, "fillColor", mFillColor, text);
    STAMP_FIELD(profile, "fillColorHL", mFillColorHL, adj(text, 10));
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, theme->getColorAccent());
    STAMP_FIELD(profile, "fillColorNA", mFillColorNA, alphaOf(text, 100));
    stampProfileBorders(theme, profile, "Bright", NULL, "Dark", NULL, "Dark");
}

static void stampScrollArrowProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& text = theme->getColorForeground();
    stampDefaultProfile(theme, profile);
    STAMP_FIELD(profile, "fillColor", mFillColor, text);
    STAMP_FIELD(profile, "fillColorHL", mFillColorHL, adj(text, 10));
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, theme->getColorAccent());
    STAMP_FIELD(profile, "fillColorNA", mFillColorNA, alphaOf(text, 100));
    STAMP_FIELD(profile, "fontColor", mFontColor, ColorI(0, 0, 0, 100));
    STAMP_FIELD(profile, "fontColorHL", mFontColorHL, ColorI(0, 0, 0, 150));
    STAMP_FIELD(profile, "fontColorSL", mFontColorSL, theme->getColorHighlight());
    STAMP_FIELD(profile, "fontColorNA", mFontColorNA, ColorI(0, 0, 0, 50));
    stampProfileBorders(theme, profile, "Bright", NULL, "Dark", NULL, "Dark");
}

static void stampTabBookProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    stampDefaultProfile(theme, profile);
    stampProfileFont(theme, profile, theme->getFontTitle(), 0);
    STAMP_FIELD(profile, "fillColor", mFillColor, alphaOf(theme->getColorBackground(), 100));
}

static void stampTabProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& bg = theme->getColorBackground();
    const ColorI& text = theme->getColorForeground();
    stampDefaultProfile(theme, profile);
    stampProfileFont(theme, profile, theme->getFontTitle(), 0);
    STAMP_FIELD(profile, "fillColor", mFillColor, bg);
    STAMP_FIELD(profile, "fillColorHL", mFillColorHL, adj(bg, 10));
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, adj(bg, 15));
    STAMP_FIELD(profile, "fillColorNA", mFillColorNA, alphaOf(bg, 100));
    STAMP_FIELD(profile, "fontColor", mFontColor, text);
    STAMP_FIELD(profile, "fontColorHL", mFontColorHL, adj(text, 10));
    STAMP_FIELD(profile, "fontColorSL", mFontColorSL, text);
    STAMP_FIELD(profile, "fontColorNA", mFontColorNA, alphaOf(text, 100));
    stampProfileBorders(theme, profile, "Bright", NULL, NULL, NULL, NULL);
}

static void stampTabPageProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& bg = theme->getColorBackground();
    stampDefaultProfile(theme, profile);
    STAMP_FIELD(profile, "fillColor", mFillColor, bg);
    STAMP_FIELD(profile, "fillColorHL", mFillColorHL, adj(bg, 10));
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, adj(bg, 15));
    STAMP_FIELD(profile, "fillColorNA", mFillColorNA, alphaOf(bg, 100));
}

static void stampListBoxProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& bg = theme->getColorBackground();
    const ColorI& text = theme->getColorForeground();
    stampDefaultProfile(theme, profile);
    stampProfileFont(theme, profile, theme->getFontCode(), 0);
    STAMP_FIELD(profile, "fillColor", mFillColor, bg);
    STAMP_FIELD(profile, "fillColorHL", mFillColorHL, adj(bg, 10));
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, theme->getColorAccent());
    STAMP_FIELD(profile, "fillColorNA", mFillColorNA, alphaOf(bg, 100));
    STAMP_FIELD(profile, "fontColor", mFontColor, text);
    STAMP_FIELD(profile, "fontColorHL", mFontColorHL, adj(text, 20));
    STAMP_FIELD(profile, "fontColorSL", mFontColorSL, adj(text, 20));
    STAMP_FIELD(profile, "fontColorNA", mFontColorNA, adj(text, -30));
    STAMP_FIELD(profile, "align", mAlignment, AlignmentType::LeftAlign);
    STAMP_FIELD(profile, "canKeyFocus", mCanKeyFocus, true);
    stampProfileBorders(theme, profile, "Rimmed", NULL, NULL, NULL, NULL);
}

static void stampDropDownItemProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    stampListBoxProfile(theme, profile);
}

static void stampDropDownProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& bg = theme->getColorBackground();
    const ColorI& text = theme->getColorForeground();
    stampDefaultProfile(theme, profile);
    stampProfileFont(theme, profile, theme->getFontTitle(), 0);
    STAMP_FIELD(profile, "fillColor", mFillColor, adj(text, -15));
    STAMP_FIELD(profile, "fillColorHL", mFillColorHL, adj(text, -8));
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, theme->getColorAccent());
    STAMP_FIELD(profile, "fillColorNA", mFillColorNA, alphaOf(text, 100));
    STAMP_FIELD(profile, "fontColor", mFontColor, bg);
    STAMP_FIELD(profile, "fontColorHL", mFontColorHL, bg);
    STAMP_FIELD(profile, "fontColorSL", mFontColorSL, text);
    STAMP_FIELD(profile, "fontColorNA", mFontColorNA, alphaOf(bg, 100));
    STAMP_FIELD(profile, "align", mAlignment, AlignmentType::LeftAlign);
    STAMP_FIELD(profile, "canKeyFocus", mCanKeyFocus, true);
    STAMP_FIELD(profile, "tab", mTabable, true);
    stampProfileBorders(theme, profile, "Bright", NULL, "Dark", NULL, "Dark");
}

static void stampWindowProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& bg = theme->getColorBackground();
    stampDefaultProfile(theme, profile);
    // The window's own text is the title bar caption -- give it the title font,
    // a notch larger than the base size.
    stampProfileFont(theme, profile, theme->getFontTitle(), 4);
    STAMP_FIELD(profile, "fillColor", mFillColor, adj(bg, 10));
    STAMP_FIELD(profile, "fillColorHL", mFillColorHL, adj(bg, 12));
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, theme->getColorAccent());
    STAMP_FIELD(profile, "fillColorNA", mFillColorNA, bg);
    STAMP_FIELD(profile, "fontColorSL", mFontColorSL, theme->getColorHighlight());
    STAMP_FIELD(profile, "align", mAlignment, AlignmentType::LeftAlign);
    stampProfileBorders(theme, profile, "Rimmed", NULL, NULL, NULL, NULL);
}

static void stampWindowContentProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& bg = theme->getColorBackground();
    stampDefaultProfile(theme, profile);
    STAMP_FIELD(profile, "fillColor", mFillColor, adj(bg, -10));
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, adj(bg, -10));
    stampProfileBorders(theme, profile, "Rimmed", NULL, NULL, NULL, NULL);
}

static void stampWindowButtonProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& bg = theme->getColorBackground();
    const ColorI& text = theme->getColorForeground();
    const ColorI& accent = theme->getColorAccent();
    stampDefaultProfile(theme, profile);
    stampProfileFont(theme, profile, theme->getFontTitle(), 0);
    STAMP_FIELD(profile, "fillColor", mFillColor, alphaOf(bg, 150));
    STAMP_FIELD(profile, "fillColorHL", mFillColorHL, alphaOf(accent, 150));
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, adj(accent, 10));
    STAMP_FIELD(profile, "fillColorNA", mFillColorNA, bg);
    STAMP_FIELD(profile, "fontColor", mFontColor, alphaOf(text, 150));
    STAMP_FIELD(profile, "fontColorHL", mFontColorHL, alphaOf(text, 170));
    STAMP_FIELD(profile, "fontColorSL", mFontColorSL, theme->getColorHighlight());
    STAMP_FIELD(profile, "fontColorNA", mFontColorNA, alphaOf(text, 150));
    stampProfileBorders(theme, profile, "Rimmed", NULL, NULL, NULL, NULL);
}

static void stampWindowCloseButtonProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& warning = theme->getColorWarning();
    stampWindowButtonProfile(theme, profile);
    STAMP_FIELD(profile, "fillColorHL", mFillColorHL, alphaOf(warning, 150));
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, adj(warning, 10));
}

static void stampMenuBarProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    stampDefaultProfile(theme, profile);
    stampProfileFont(theme, profile, theme->getFontTitle(), 0);
    STAMP_FIELD(profile, "fillColor", mFillColor, adj(theme->getColorBackground(), -7));
    STAMP_FIELD(profile, "canKeyFocus", mCanKeyFocus, true);
    stampProfileBorders(theme, profile, "Rimmed", NULL, NULL, NULL, NULL);
}

static void stampMenuProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& text = theme->getColorForeground();
    stampDefaultProfile(theme, profile);
    stampProfileFont(theme, profile, theme->getFontTitle(), 0);
    STAMP_FIELD(profile, "fillColor", mFillColor, ColorI(0, 0, 0, 0));
    STAMP_FIELD(profile, "fillColorHL", mFillColorHL, ColorI(255, 255, 255, 10));
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, adj(theme->getColorAccent(), -15));
    STAMP_FIELD(profile, "fillColorNA", mFillColorNA, ColorI(0, 0, 0, 0));
    STAMP_FIELD(profile, "fontColor", mFontColor, text);
    STAMP_FIELD(profile, "fontColorHL", mFontColorHL, adj(text, 10));
    STAMP_FIELD(profile, "fontColorSL", mFontColorSL, text);
    STAMP_FIELD(profile, "fontColorNA", mFontColorNA, alphaOf(text, 100));
    stampProfileBorders(theme, profile, "Rimmed", NULL, NULL, NULL, NULL);
}

static void stampMenuItemProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& bg = theme->getColorBackground();
    const ColorI& text = theme->getColorForeground();
    stampDefaultProfile(theme, profile);
    stampProfileFont(theme, profile, theme->getFontTitle(), 0);
    STAMP_FIELD(profile, "fillColor", mFillColor, adj(bg, -5));
    STAMP_FIELD(profile, "fillColorHL", mFillColorHL, adj(theme->getColorAccent(), -15));
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, ColorI(0, 0, 0, 0));
    STAMP_FIELD(profile, "fillColorNA", mFillColorNA, bg);
    STAMP_FIELD(profile, "fontColor", mFontColor, text);
    STAMP_FIELD(profile, "fontColorHL", mFontColorHL, adj(text, 10));
    STAMP_FIELD(profile, "fontColorNA", mFontColorNA, alphaOf(text, 150));
    STAMP_FIELD(profile, "align", mAlignment, AlignmentType::LeftAlign);
    stampProfileBorders(theme, profile, "Padded", NULL, NULL, NULL, NULL);
}

static void stampMenuContentProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    stampDefaultProfile(theme, profile);
    STAMP_FIELD(profile, "fillColor", mFillColor, adj(theme->getColorBackground(), -5));
    stampProfileBorders(theme, profile, "Rimmed", NULL, NULL, NULL, NULL);
}

static void stampOverlayProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    stampDefaultProfile(theme, profile);
    stampProfileFont(theme, profile, theme->getFontBody(), -2);
}

static void stampProgressProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& accent = theme->getColorAccent();
    stampDefaultProfile(theme, profile);
    STAMP_FIELD(profile, "fillColor", mFillColor, theme->getColorBackground());
    STAMP_FIELD(profile, "fillColorHL", mFillColorHL, accent);
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, adj(accent, 10));
    STAMP_FIELD(profile, "fontColorHL", mFontColorHL, theme->getColorForeground());
    STAMP_FIELD(profile, "fontColorSL", mFontColorSL, theme->getColorHighlight());
    stampProfileBorders(theme, profile, "Rimmed", NULL, "Dark", NULL, "Dark");
}

static void stampTreeViewProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& bg = theme->getColorBackground();
    const ColorI& text = theme->getColorForeground();
    stampDefaultProfile(theme, profile);
    stampProfileFont(theme, profile, theme->getFontCode(), 0);
    STAMP_FIELD(profile, "fillColor", mFillColor, bg);
    STAMP_FIELD(profile, "fillColorHL", mFillColorHL, adj(bg, 10));
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, adj(bg, 15));
    STAMP_FIELD(profile, "fillColorNA", mFillColorNA, alphaOf(bg, 100));
    STAMP_FIELD(profile, "fontColor", mFontColor, text);
    STAMP_FIELD(profile, "fontColorHL", mFontColorHL, adj(text, 10));
    STAMP_FIELD(profile, "fontColorSL", mFontColorSL, text);
    STAMP_FIELD(profile, "fontColorNA", mFontColorNA, alphaOf(text, 100));
    STAMP_FIELD(profile, "canKeyFocus", mCanKeyFocus, true);
}

static void stampFrameSetProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& bg = theme->getColorBackground();
    stampDefaultProfile(theme, profile);
    STAMP_FIELD(profile, "fillColor", mFillColor, bg);
    STAMP_FIELD(profile, "fillColorHL", mFillColorHL, adj(bg, 10));
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, adj(bg, 15));
    STAMP_FIELD(profile, "fillColorNA", mFillColorNA, alphaOf(bg, 100));
}

static void stampFrameSetDropButtonProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& accent = theme->getColorAccent();
    stampDefaultProfile(theme, profile);
    STAMP_FIELD(profile, "fillColor", mFillColor, alphaOf(accent, 100));
    STAMP_FIELD(profile, "fillColorHL", mFillColorHL, alphaOf(accent, 180));
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, adj(accent, 10));
    STAMP_FIELD(profile, "fillColorNA", mFillColorNA, alphaOf(accent, 50));
}

static void stampColorPickerProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    stampDefaultProfile(theme, profile);
    STAMP_FIELD(profile, "canKeyFocus", mCanKeyFocus, true);
    stampProfileBorders(theme, profile, "Rimmed", NULL, NULL, NULL, NULL);
}

static void stampColorSelectorProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    stampDefaultProfile(theme, profile);
    // Deliberately hue-independent neutral ring colors, like the bevels.
    STAMP_FIELD(profile, "fillColor", mFillColor, ColorI(240, 240, 240, 255));
    STAMP_FIELD(profile, "fillColorHL", mFillColorHL, ColorI(250, 250, 250, 255));
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, theme->getColorHighlight());
    stampProfileBorders(theme, profile, "Dark", NULL, NULL, NULL, NULL);
}

static void stampColorPopupProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& bg = theme->getColorBackground();
    stampDefaultProfile(theme, profile);
    STAMP_FIELD(profile, "fillColor", mFillColor, bg);
    STAMP_FIELD(profile, "fillColorHL", mFillColorHL, bg);
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, bg);
    STAMP_FIELD(profile, "fillColorNA", mFillColorNA, bg);
    stampProfileBorders(theme, profile, "Rimmed", NULL, NULL, NULL, NULL);
}

static void stampDragAndDropProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    stampDefaultProfile(theme, profile);
    STAMP_FIELD(profile, "fillColor", mFillColor, alphaOf(theme->getColorAccent(), 50));
    STAMP_FIELD(profile, "fontColor", mFontColor, theme->getColorHighlight());
}

//-----------------------------------------------------------------------------
// The engine-defined category tables: the canonical set of profiles a
// complete theme provides, one entry per profile slot the stock GuiControls
// consume. Adding a control with a new slot means adding its category here.
//-----------------------------------------------------------------------------

const GuiProfileTheme::ProfileCategory GuiProfileTheme::smProfileCategories[] =
{
    { "Default",            "DefaultProfile",            stampDefaultProfile },
    { "Empty",              "EmptyProfile",              stampEmptyProfile },
    { "Tooltip",            "TooltipProfile",            stampTooltipProfile },
    { "Panel",              "PanelProfile",              stampPanelProfile },
    { "Button",             "ButtonProfile",             stampButtonProfile },
    { "CheckBox",           "CheckBoxProfile",           stampCheckBoxProfile },
    { "Radio",              "RadioProfile",              stampRadioProfile },
    { "Label",              "LabelProfile",              stampLabelProfile },
    { "TextEdit",           "TextEditProfile",           stampTextEditProfile },
    { "Scroll",             "ScrollProfile",             stampScrollProfile },
    { "ScrollTrack",        "ScrollTrackProfile",        stampScrollTrackProfile },
    { "ScrollThumb",        "ScrollThumbProfile",        stampScrollThumbProfile },
    { "ScrollArrow",        "ScrollArrowProfile",        stampScrollArrowProfile },
    { "TabBook",            "TabBookProfile",            stampTabBookProfile },
    { "Tab",                "TabProfile",                stampTabProfile },
    { "TabPage",            "TabPageProfile",            stampTabPageProfile },
    { "ListBox",            "ListBoxProfile",            stampListBoxProfile },
    { "DropDown",           "DropDownProfile",           stampDropDownProfile },
    { "DropDownItem",       "DropDownItemProfile",       stampDropDownItemProfile },
    { "Window",             "WindowProfile",             stampWindowProfile },
    { "WindowContent",      "WindowContentProfile",      stampWindowContentProfile },
    { "WindowButton",       "WindowButtonProfile",       stampWindowButtonProfile },
    { "WindowCloseButton",  "WindowCloseButtonProfile",  stampWindowCloseButtonProfile },
    { "MenuBar",            "MenuBarProfile",            stampMenuBarProfile },
    { "Menu",               "MenuProfile",               stampMenuProfile },
    { "MenuItem",           "MenuItemProfile",           stampMenuItemProfile },
    { "MenuContent",        "MenuContentProfile",        stampMenuContentProfile },
    { "Overlay",            "OverlayProfile",            stampOverlayProfile },
    { "Progress",           "ProgressProfile",           stampProgressProfile },
    { "TreeView",           "TreeViewProfile",           stampTreeViewProfile },
    { "FrameSet",           "FrameSetProfile",           stampFrameSetProfile },
    { "FrameSetDropButton", "FrameSetDropButtonProfile", stampFrameSetDropButtonProfile },
    { "ColorPicker",        "ColorPickerProfile",        stampColorPickerProfile },
    { "ColorSelector",      "ColorSelectorProfile",      stampColorSelectorProfile },
    { "ColorPopup",         "ColorPopupProfile",         stampColorPopupProfile },
    { "DragAndDrop",        "DragAndDropProfile",        stampDragAndDropProfile },
};
const S32 GuiProfileTheme::smProfileCategoryCount = sizeof(smProfileCategories) / sizeof(smProfileCategories[0]);

const GuiProfileTheme::BorderCategory GuiProfileTheme::smBorderCategories[] =
{
    { "Empty",  "EmptyBorder",  stampEmptyBorder },
    { "Rimmed", "RimmedBorder", stampRimmedBorder },
    { "Thick",  "ThickBorder",  stampThickBorder },
    { "Bright", "BrightBorder", stampBrightBorder },
    { "Dark",   "DarkBorder",   stampDarkBorder },
    { "Padded", "PaddedBorder", stampPaddedBorder },
};
const S32 GuiProfileTheme::smBorderCategoryCount = sizeof(smBorderCategories) / sizeof(smBorderCategories[0]);

//-----------------------------------------------------------------------------

GuiProfileTheme::GuiProfileTheme()
{
    mTamlReading = false;

    mFontBody = StringTable->insert("Arial");
    mFontTitle = StringTable->insert("Arial");
    mFontCode = StringTable->insert("Courier New");
    mFontDirectory = StringTable->EmptyString;
    mFontSize = 12;

    mColorBackground.set(43, 43, 43, 255);
    mColorSurface.set(81, 92, 102, 255);
    mColorForeground.set(224, 224, 224, 255);
    mColorAccent.set(54, 135, 196, 255);
    mColorHighlight.set(245, 210, 50, 255);
    mColorWarning.set(196, 54, 71, 255);

    mBorderSize = 1;

    mDefaultProfiles.setSize(smProfileCategoryCount);
    for (S32 i = 0; i < smProfileCategoryCount; ++i)
        mDefaultProfiles[i] = NULL;

    mDefaultBorders.setSize(smBorderCategoryCount);
    for (S32 i = 0; i < smBorderCategoryCount; ++i)
        mDefaultBorders[i] = NULL;
}

void GuiProfileTheme::initPersistFields()
{
    Parent::initPersistFields();

    addGroup("Fonts");
        addField("fontBody", TypeString, Offset(mFontBody, GuiProfileTheme));
        addField("fontTitle", TypeString, Offset(mFontTitle, GuiProfileTheme));
        addField("fontCode", TypeString, Offset(mFontCode, GuiProfileTheme));
        addField("fontDirectory", TypeString, Offset(mFontDirectory, GuiProfileTheme));
        addField("fontSize", TypeS32, Offset(mFontSize, GuiProfileTheme));
    endGroup("Fonts");

    addGroup("Colors");
        addField("colorBackground", TypeColorI, Offset(mColorBackground, GuiProfileTheme));
        addField("colorSurface", TypeColorI, Offset(mColorSurface, GuiProfileTheme));
        addField("colorForeground", TypeColorI, Offset(mColorForeground, GuiProfileTheme));
        addField("colorAccent", TypeColorI, Offset(mColorAccent, GuiProfileTheme));
        addField("colorHighlight", TypeColorI, Offset(mColorHighlight, GuiProfileTheme));
        addField("colorWarning", TypeColorI, Offset(mColorWarning, GuiProfileTheme));
    endGroup("Colors");

    addField("borderSize", TypeS32, Offset(mBorderSize, GuiProfileTheme));
}

bool GuiProfileTheme::onAdd()
{
    if (!Parent::onAdd())
        return false;

    Sim::getGuiDataGroup()->addObject(this);

    restamp();

    return true;
}

void GuiProfileTheme::onRemove()
{
    // The theme owns its members: delete extras, then defaults, then borders.
    // Each deletion notifies us back and clears its own list entry.
    while (mExtraProfiles.size() > 0)
        mExtraProfiles.last()->deleteObject();

    while (mExtraBorders.size() > 0)
        mExtraBorders.last()->deleteObject();

    for (S32 i = 0; i < smProfileCategoryCount; ++i)
    {
        if (mDefaultProfiles[i] != NULL)
        {
            GuiControlProfile* profile = mDefaultProfiles[i];
            mDefaultProfiles[i] = NULL;
            profile->deleteObject();
        }
    }

    for (S32 i = 0; i < smBorderCategoryCount; ++i)
    {
        if (mDefaultBorders[i] != NULL)
        {
            GuiBorderProfile* border = mDefaultBorders[i];
            mDefaultBorders[i] = NULL;
            border->deleteObject();
        }
    }

    Parent::onRemove();
}

void GuiProfileTheme::onStaticModified(const char* slotName, const char* newValue)
{
    Parent::onStaticModified(slotName, newValue);

    // Any theme value change re-derives all members. Harmlessly no-ops while
    // fields are still being applied before registration.
    restamp();
}

void GuiProfileTheme::onDeleteNotify(SimObject* object)
{
    // A member died: drop it from our lists. A missing default is recreated
    // at the next restamp.
    for (S32 i = 0; i < smProfileCategoryCount; ++i)
    {
        if (mDefaultProfiles[i] == object)
            mDefaultProfiles[i] = NULL;
    }

    for (S32 i = 0; i < mExtraProfiles.size(); ++i)
    {
        if (mExtraProfiles[i] == object)
        {
            mExtraProfiles.erase(i);
            break;
        }
    }

    for (S32 i = 0; i < mExtraBorders.size(); ++i)
    {
        if (mExtraBorders[i] == object)
        {
            mExtraBorders.erase(i);
            break;
        }
    }

    for (S32 i = 0; i < smBorderCategoryCount; ++i)
    {
        if (mDefaultBorders[i] == object)
            mDefaultBorders[i] = NULL;
    }

    Parent::onDeleteNotify(object);
}

//-----------------------------------------------------------------------------
// Category tables.
//-----------------------------------------------------------------------------

StringTableEntry GuiProfileTheme::getCategoryName(S32 index)
{
    if (index < 0 || index >= smProfileCategoryCount)
        return NULL;

    return StringTable->insert(smProfileCategories[index].name);
}

S32 GuiProfileTheme::findCategoryIndex(StringTableEntry categoryName)
{
    for (S32 i = 0; i < smProfileCategoryCount; ++i)
    {
        if (StringTable->insert(smProfileCategories[i].name) == categoryName)
            return i;
    }
    return -1;
}

StringTableEntry GuiProfileTheme::getBorderCategoryName(S32 index)
{
    if (index < 0 || index >= smBorderCategoryCount)
        return NULL;

    return StringTable->insert(smBorderCategories[index].name);
}

S32 GuiProfileTheme::findBorderCategoryIndex(StringTableEntry categoryName)
{
    for (S32 i = 0; i < smBorderCategoryCount; ++i)
    {
        if (StringTable->insert(smBorderCategories[i].name) == categoryName)
            return i;
    }
    return -1;
}

//-----------------------------------------------------------------------------
// Members.
//-----------------------------------------------------------------------------

S32 GuiProfileTheme::getProfileCount() const
{
    S32 count = mExtraProfiles.size();
    for (S32 i = 0; i < smProfileCategoryCount; ++i)
    {
        if (mDefaultProfiles[i] != NULL)
            ++count;
    }
    return count;
}

GuiControlProfile* GuiProfileTheme::getProfile(StringTableEntry categoryName) const
{
    const S32 index = findCategoryIndex(categoryName);
    return (index >= 0) ? mDefaultProfiles[index] : NULL;
}

GuiBorderProfile* GuiProfileTheme::getBorder(StringTableEntry categoryName) const
{
    const S32 index = findBorderCategoryIndex(categoryName);
    return (index >= 0) ? mDefaultBorders[index] : NULL;
}

GuiControlProfile* GuiProfileTheme::createMemberProfile(S32 categoryIndex, const char* objectName)
{
    const ProfileCategory& category = smProfileCategories[categoryIndex];

    GuiControlProfile* profile = new GuiControlProfile();

    char nameBuffer[256];
    if (objectName == NULL && getName() != NULL && *getName() != '\0')
    {
        dSprintf(nameBuffer, sizeof(nameBuffer), "%s%s", getName(), category.suffix);
        objectName = nameBuffer;
    }
    if (objectName != NULL && *objectName != '\0')
        profile->assignName(objectName);

    if (!profile->registerObject())
    {
        delete profile;
        return NULL;
    }

    profile->mCategory = StringTable->insert(category.name);
    profile->setTheme(this);
    deleteNotify(profile);

    return profile;
}

GuiBorderProfile* GuiProfileTheme::createMemberBorder(S32 categoryIndex)
{
    const BorderCategory& category = smBorderCategories[categoryIndex];

    GuiBorderProfile* border = new GuiBorderProfile();

    if (getName() != NULL && *getName() != '\0')
    {
        char nameBuffer[256];
        dSprintf(nameBuffer, sizeof(nameBuffer), "%s%s", getName(), category.suffix);
        border->assignName(nameBuffer);
    }

    if (!border->registerObject())
    {
        delete border;
        return NULL;
    }

    border->mCategory = StringTable->insert(category.name);
    border->setTheme(this);
    deleteNotify(border);

    return border;
}

GuiControlProfile* GuiProfileTheme::createProfile(const char* categoryName, const char* objectName)
{
    const S32 categoryIndex = findCategoryIndex(StringTable->insert(categoryName));
    if (categoryIndex < 0)
    {
        Con::warnf("GuiProfileTheme::createProfile() - unknown category '%s'.", categoryName);
        return NULL;
    }

    // Generate <ThemeName><Suffix><N> when no name is given.
    char nameBuffer[256];
    if ((objectName == NULL || *objectName == '\0') && getName() != NULL && *getName() != '\0')
    {
        for (S32 n = 2; n < 1000000; ++n)
        {
            dSprintf(nameBuffer, sizeof(nameBuffer), "%s%s%d", getName(), smProfileCategories[categoryIndex].suffix, n);
            if (Sim::findObject(nameBuffer) == NULL)
                break;
        }
        objectName = nameBuffer;
    }

    GuiControlProfile* profile = createMemberProfile(categoryIndex, objectName);
    if (profile == NULL)
        return NULL;

    mExtraProfiles.push_back(profile);
    smProfileCategories[categoryIndex].stamp(this, profile);

    return profile;
}

bool GuiProfileTheme::removeProfile(GuiControlProfile* profile)
{
    for (S32 i = 0; i < mExtraProfiles.size(); ++i)
    {
        if (mExtraProfiles[i] == profile)
        {
            // Deletion notifies us back and erases the list entry.
            profile->deleteObject();
            return true;
        }
    }

    // Default members are never removed: a theme is always complete.
    return false;
}

GuiBorderProfile* GuiProfileTheme::createBorder(const char* objectName)
{
    GuiBorderProfile* border = new GuiBorderProfile();
    border->mIsCustom = true;

    if (objectName != NULL && *objectName != '\0')
        border->assignName(objectName);

    if (!border->registerObject())
    {
        delete border;
        return NULL;
    }

    // A custom border is user-authored, not derived from a recipe, so it is
    // NOT a theme member (mTheme stays NULL) and serializes all its own
    // fields like a standalone border. The theme still owns it for lifetime
    // and Taml, tracked here and released in onRemove.
    mExtraBorders.push_back(border);
    deleteNotify(border);

    return border;
}

bool GuiProfileTheme::removeBorder(GuiBorderProfile* border)
{
    for (S32 i = 0; i < mExtraBorders.size(); ++i)
    {
        if (mExtraBorders[i] == border)
        {
            // Deletion notifies us back and erases the list entry.
            border->deleteObject();
            return true;
        }
    }

    return false;
}

bool GuiProfileTheme::renameTheme(const char* newName)
{
    if (!isProperlyAdded())
    {
        Con::warnf("GuiProfileTheme::renameTheme() - the theme must be registered first.");
        return false;
    }

    if (newName == NULL || *newName == '\0')
    {
        Con::warnf("GuiProfileTheme::renameTheme() - a non-empty name is required.");
        return false;
    }

    const char* oldName = getName();
    const bool hasOldName = (oldName != NULL && *oldName != '\0');
    const S32 oldNameLength = hasOldName ? dStrlen(oldName) : 0;

    // Every rename this operation would perform, computed up front so a
    // collision anywhere refuses the whole batch.
    struct PendingRename
    {
        SimObject* object;
        StringTableEntry target;
    };
    Vector<PendingRename> renames;

    char nameBuffer[256];

    PendingRename themeRename = { this, StringTable->insert(newName) };
    renames.push_back(themeRename);

    // Defaults always take the theme-managed name, so a previously unnamed
    // theme's members gain names here.
    for (S32 i = 0; i < smBorderCategoryCount; ++i)
    {
        if (mDefaultBorders[i] == NULL)
            continue;
        dSprintf(nameBuffer, sizeof(nameBuffer), "%s%s", newName, smBorderCategories[i].suffix);
        PendingRename rename = { mDefaultBorders[i], StringTable->insert(nameBuffer) };
        renames.push_back(rename);
    }

    for (S32 i = 0; i < smProfileCategoryCount; ++i)
    {
        if (mDefaultProfiles[i] == NULL)
            continue;
        dSprintf(nameBuffer, sizeof(nameBuffer), "%s%s", newName, smProfileCategories[i].suffix);
        PendingRename rename = { mDefaultProfiles[i], StringTable->insert(nameBuffer) };
        renames.push_back(rename);
    }

    // Extras rename only when they follow the <ThemeName>... pattern.
    for (S32 i = 0; i < mExtraProfiles.size(); ++i)
    {
        const char* extraName = mExtraProfiles[i]->getName();
        if (!hasOldName || extraName == NULL || dStrncmp(extraName, oldName, oldNameLength) != 0)
            continue;
        dSprintf(nameBuffer, sizeof(nameBuffer), "%s%s", newName, extraName + oldNameLength);
        PendingRename rename = { mExtraProfiles[i], StringTable->insert(nameBuffer) };
        renames.push_back(rename);
    }

    // Collision pre-check: every target must be free or already belong to the
    // object being renamed to it.
    for (S32 i = 0; i < renames.size(); ++i)
    {
        SimObject* existing = Sim::findObject(renames[i].target);
        if (existing != NULL && existing != renames[i].object)
        {
            Con::warnf("GuiProfileTheme::renameTheme() - the name '%s' is already taken; nothing was renamed.", renames[i].target);
            return false;
        }
    }

    // Overridden border-name references must follow the rename. Resolve the
    // stored names before anything is renamed (the cached border pointers are
    // resolved lazily, so the names are the source of truth).
    struct PendingBorderRef
    {
        GuiControlProfile* profile;
        S32 side;                       ///< 0 left, 1 right, 2 top, 3 bottom.
        GuiBorderProfile* border;
    };
    Vector<PendingBorderRef> borderRefs;

    static const char* const sideFields[4] = { "borderLeft", "borderRight", "borderTop", "borderBottom" };

    for (S32 i = 0; i < smProfileCategoryCount + mExtraProfiles.size(); ++i)
    {
        GuiControlProfile* profile = (i < smProfileCategoryCount) ? mDefaultProfiles[i] : mExtraProfiles[i - smProfileCategoryCount];
        if (profile == NULL)
            continue;

        StringTableEntry sideNames[4] = { profile->mLeftProfileName, profile->mRightProfileName, profile->mTopProfileName, profile->mBottomProfileName };
        for (S32 side = 0; side < 4; ++side)
        {
            if (!profile->isThemeFieldOverridden(StringTable->insert(sideFields[side])) || sideNames[side] == NULL || *sideNames[side] == '\0')
                continue;

            GuiBorderProfile* border = dynamic_cast<GuiBorderProfile*>(Sim::findObject(sideNames[side]));
            if (border != NULL)
            {
                PendingBorderRef ref = { profile, side, border };
                borderRefs.push_back(ref);
            }
        }
    }

    for (S32 i = 0; i < renames.size(); ++i)
        renames[i].object->assignName(renames[i].target);

    // Rewrite the stashed references that point at this theme's borders;
    // references to outside borders keep their names.
    for (S32 i = 0; i < borderRefs.size(); ++i)
    {
        GuiBorderProfile* border = borderRefs[i].border;
        if (border->getTheme() != this)
            continue;

        GuiControlProfile* profile = borderRefs[i].profile;
        const StringTableEntry borderName = border->getName();
        switch (borderRefs[i].side)
        {
        case 0: profile->mLeftProfileName = borderName;   profile->setLeftProfile(border);   break;
        case 1: profile->mRightProfileName = borderName;  profile->setRightProfile(border);  break;
        case 2: profile->mTopProfileName = borderName;    profile->setTopProfile(border);    break;
        case 3: profile->mBottomProfileName = borderName; profile->setBottomProfile(border); break;
        }
    }

    restamp();

    return true;
}

void GuiProfileTheme::restamp()
{
    if (!isProperlyAdded() || mTamlReading)
        return;

    // Borders first: profile recipes reference them.
    for (S32 i = 0; i < smBorderCategoryCount; ++i)
    {
        if (mDefaultBorders[i] == NULL)
            mDefaultBorders[i] = createMemberBorder(i);
        if (mDefaultBorders[i] != NULL)
            smBorderCategories[i].stamp(this, mDefaultBorders[i]);
    }

    for (S32 i = 0; i < smProfileCategoryCount; ++i)
    {
        if (mDefaultProfiles[i] == NULL)
            mDefaultProfiles[i] = createMemberProfile(i, NULL);
        if (mDefaultProfiles[i] != NULL)
            smProfileCategories[i].stamp(this, mDefaultProfiles[i]);
    }

    for (S32 i = 0; i < mExtraProfiles.size(); ++i)
    {
        const S32 categoryIndex = findCategoryIndex(mExtraProfiles[i]->mCategory);
        if (categoryIndex >= 0)
            smProfileCategories[categoryIndex].stamp(this, mExtraProfiles[i]);
    }
}

//-----------------------------------------------------------------------------
// Taml persistence. Members serialize as ordinary child objects; their
// writeField filter reduces each to name, category, themeOverrides, and the
// overridden field values. Borders are written before profiles so overridden
// border-name references resolve on read. Defaults precede extras so read-back
// fills each category's default slot first.
//-----------------------------------------------------------------------------

U32 GuiProfileTheme::getTamlChildCount(void) const
{
    U32 count = (U32)mExtraProfiles.size() + (U32)mExtraBorders.size();

    for (S32 i = 0; i < smBorderCategoryCount; ++i)
    {
        if (mDefaultBorders[i] != NULL)
            ++count;
    }

    for (S32 i = 0; i < smProfileCategoryCount; ++i)
    {
        if (mDefaultProfiles[i] != NULL)
            ++count;
    }

    return count;
}

SimObject* GuiProfileTheme::getTamlChild(const U32 childIndex) const
{
    U32 index = childIndex;

    for (S32 i = 0; i < smBorderCategoryCount; ++i)
    {
        if (mDefaultBorders[i] == NULL)
            continue;
        if (index == 0)
            return mDefaultBorders[i];
        --index;
    }

    // Custom borders follow the category borders so a profile's overridden
    // border reference resolves to an already-read child.
    if (index < (U32)mExtraBorders.size())
        return mExtraBorders[index];
    index -= (U32)mExtraBorders.size();

    for (S32 i = 0; i < smProfileCategoryCount; ++i)
    {
        if (mDefaultProfiles[i] == NULL)
            continue;
        if (index == 0)
            return mDefaultProfiles[i];
        --index;
    }

    if (index < (U32)mExtraProfiles.size())
        return mExtraProfiles[index];

    return NULL;
}

void GuiProfileTheme::addTamlChild(SimObject* pSimObject)
{
    GuiBorderProfile* border = dynamic_cast<GuiBorderProfile*>(pSimObject);
    if (border != NULL)
    {
        // A custom (user-authored) border is not a category member: it keeps
        // its own fields and is owned as an extra border.
        if (border->mIsCustom)
        {
            mExtraBorders.push_back(border);
            deleteNotify(border);
            return;
        }

        const S32 categoryIndex = findBorderCategoryIndex(border->mCategory);
        if (categoryIndex < 0 || mDefaultBorders[categoryIndex] != NULL)
        {
            Con::warnf("GuiProfileTheme::addTamlChild() - border child with unknown or duplicate category '%s' left unattached.", border->mCategory);
            return;
        }

        mDefaultBorders[categoryIndex] = border;
        border->setTheme(this, true);
        deleteNotify(border);
        return;
    }

    GuiControlProfile* profile = dynamic_cast<GuiControlProfile*>(pSimObject);
    if (profile != NULL)
    {
        const S32 categoryIndex = findCategoryIndex(profile->mCategory);
        if (categoryIndex < 0)
        {
            Con::warnf("GuiProfileTheme::addTamlChild() - profile child with unknown category '%s' left unattached.", profile->mCategory);
            return;
        }

        if (mDefaultProfiles[categoryIndex] == NULL)
            mDefaultProfiles[categoryIndex] = profile;
        else
            mExtraProfiles.push_back(profile);

        profile->setTheme(this, true);
        deleteNotify(profile);
        return;
    }

    Con::warnf("GuiProfileTheme::addTamlChild() - unsupported child type '%s'.", pSimObject != NULL ? pSimObject->getClassName() : "NULL");
}

void GuiProfileTheme::onTamlPreRead(void)
{
    // Suppress auto-creation and stamping until the file's members are
    // attached, so they claim their names and category slots first.
    mTamlReading = true;
}

void GuiProfileTheme::onTamlPostRead(const TamlCustomNodes& customNodes)
{
    mTamlReading = false;

    // Create any defaults the file did not carry and derive every
    // non-overridden field from the loaded theme values.
    restamp();
}

//-----------------------------------------------------------------------------
// Color helpers.
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
