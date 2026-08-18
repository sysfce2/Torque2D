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
    // this with their own reference (Light/Dark 2, Button 3, ...).
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

static void stampLightBorder(GuiProfileTheme* theme, GuiBorderProfile* border)
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
// Extended border palette: descriptive, control-agnostic borders the six base
// recipes don't cover (a colored rim, inset bevels, padded edges, a couple that
// change on hover/active). Like the base six they are full theme members --
// shown in the editor, scaled by borderSize, renamed and serialized -- so a
// profile that uses one behaves the same as any other border. Margin/border/
// padding are per state [normal, HL, SL, NA].
//-----------------------------------------------------------------------------

static void stampHighlightBorder(GuiProfileTheme* theme, GuiBorderProfile* border)
{
    // A rim in the highlight color plus a 10px content inset.
    BorderRecipe recipe = baseBorderRecipe(theme);
    set4(recipe.border, 1);
    set4(recipe.borderColor, theme->getColorHighlight());
    set4(recipe.padding, 10);
    applyBorderRecipe(border, recipe);
}

static void stampPaddedRimBorder(GuiProfileTheme* theme, GuiBorderProfile* border)
{
    // A plain rim with a 10px content inset: keeps text/items off the frame.
    BorderRecipe recipe = baseBorderRecipe(theme);
    set4(recipe.border, 1);
    set4(recipe.padding, 10);
    applyBorderRecipe(border, recipe);
}

static void stampBevelLightBorder(GuiProfileTheme* theme, GuiBorderProfile* border)
{
    // The light edge of a raised bevel, inset one pixel all round.
    BorderRecipe recipe = baseBorderRecipe(theme);
    set4(recipe.border, 1);
    set4(recipe.borderColor, ColorI(255, 255, 255, 50));
    set4(recipe.margin, 1);
    applyBorderRecipe(border, recipe);
}

static void stampBevelDarkBorder(GuiProfileTheme* theme, GuiBorderProfile* border)
{
    // The shadow edge of a raised bevel, inset one pixel all round.
    BorderRecipe recipe = baseBorderRecipe(theme);
    set4(recipe.border, 1);
    set4(recipe.borderColor, ColorI(0, 0, 0, 50));
    set4(recipe.margin, 1);
    applyBorderRecipe(border, recipe);
}

static void stampPaddedLightBorder(GuiProfileTheme* theme, GuiBorderProfile* border)
{
    // A light bevel edge with 2px padding to inset a glyph.
    BorderRecipe recipe = baseBorderRecipe(theme);
    set4(recipe.border, 1);
    set4(recipe.borderColor, ColorI(255, 255, 255, 50));
    set4(recipe.padding, 2);
    applyBorderRecipe(border, recipe);
}

static void stampPaddedDarkBorder(GuiProfileTheme* theme, GuiBorderProfile* border)
{
    // A shadow bevel edge with 2px padding to inset a glyph.
    BorderRecipe recipe = baseBorderRecipe(theme);
    set4(recipe.border, 1);
    set4(recipe.borderColor, ColorI(0, 0, 0, 50));
    set4(recipe.padding, 2);
    applyBorderRecipe(border, recipe);
}

static void stampRimmedExpanderBorder(GuiProfileTheme* theme, GuiBorderProfile* border)
{
    // A rim that grows on hover/press -- its margin shrinks from 3 to 1 -- and
    // pads a small glyph.
    BorderRecipe recipe = baseBorderRecipe(theme);
    set4(recipe.border, 1);
    set4(recipe.margin, 3, 1, 1, 3);
    set4(recipe.padding, 4, 5, 5, 4);
    applyBorderRecipe(border, recipe);
}

static void stampCondenserLightBorder(GuiProfileTheme* theme, GuiBorderProfile* border)
{
    // The light edge of a frame that shrinks on the active/highlight state -- its
    // margin grows from 0 to 2 -- e.g. a bar inset within its track.
    BorderRecipe recipe = baseBorderRecipe(theme);
    set4(recipe.border, 1);
    set4(recipe.borderColor, ColorI(255, 255, 255, 50));
    set4(recipe.margin, 0, 2, 2, 0);
    applyBorderRecipe(border, recipe);
}

static void stampCondenserDarkBorder(GuiProfileTheme* theme, GuiBorderProfile* border)
{
    // The shadow edge of a frame that shrinks on the active/highlight state.
    BorderRecipe recipe = baseBorderRecipe(theme);
    set4(recipe.border, 1);
    set4(recipe.borderColor, ColorI(0, 0, 0, 50));
    set4(recipe.margin, 0, 2, 2, 0);
    applyBorderRecipe(border, recipe);
}

static void stampSelectedInsetBorder(GuiProfileTheme* theme, GuiBorderProfile* border)
{
    // A padded inset in three states and a rule in the fourth. The odd one out is
    // SELECTED, and it is deliberate: a menu separator is drawn as the menu item
    // profile in its selected state and nothing else in a menu ever uses that
    // state (hover is Highlight, greyed is Disabled), so the SL fields of a menu
    // item's borders belong to separators alone. One border therefore gives a
    // theme both the room around a label and the groove between two groups of
    // them, which is why this is what a generated MenuItem wears.
    //
    // The margin is what makes it a groove rather than a band. A separator's
    // height is nothing but this border's margin, rim and padding
    // (GuiMenuListCtrl::updateSize measures it with a zero-height interior), so
    // dropping the padding to 0 and giving it 4 of margin buys 4px of the menu's
    // own fill above and below a 2px rule.
    BorderRecipe recipe = baseBorderRecipe(theme);
    set4(recipe.margin, 0, 0, 4, 0);
    set4(recipe.border, 0, 0, 1, 0);
    set4(recipe.borderColor, theme->getColorSurface());
    set4(recipe.padding, 10, 10, 0, 10);
    recipe.underfill = true;

    // Alone among the recipes, not scaled by the theme's border size. This rim is
    // a rule between two groups of commands rather than the edge of a control: at
    // borderSize 0 it would vanish and the menu would lose its grouping, and at 2
    // or 3 it would thicken into a band.
    recipe.borderScale = 1;

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

    // Render and layout read the cached side pointers directly (getLeftBorder(),
    // etc.), never the lazy resolver, so a side that falls back to the default
    // must have its pointer resolved here and now. Recipes write raw members and
    // never trigger onStaticModified -- the path that normally repopulates these
    // -- so resolve the sides ourselves, exactly as onStaticModified does. Each
    // fallback side above was reset to NULL, so these pick up the current
    // borderDefault; sides carrying their own border early-out unchanged. Without
    // this, a freshly stamped profile whose sides fall back to a non-empty default
    // border renders borderless until a later field edit forces a re-resolve.
    profile->getLeftProfile();
    profile->getRightProfile();
    profile->getTopProfile();
    profile->getBottomProfile();
}

/// Override a profile's font face and size (a signed delta from the theme's
/// base font size), respecting user overrides. Called after stampProfileBase,
/// which has already set the body font at the base size. This is how the three
/// theme fonts get their roles: title on chrome, code on input/data, body on
/// content, with a size bump or trim where a category wants one.
static void stampProfileFont(GuiProfileTheme* theme, GuiControlProfile* profile, StringTableEntry face, S32 sizeDelta)
{
    STAMP_FIELD(profile, "fontType", mFontType, face);
    STAMP_FIELD(profile, "fontSize", mFontSize, theme->getFontSize() + sizeDelta);
}

/// The base recipe: every category recipe starts from this, so every themed
/// field of every member is always derived from something. Not a category of its
/// own - a control with nothing better to wear takes Empty, and the engine's
/// GuiDefaultProfile is the floor beneath even that.
static void stampProfileBase(GuiProfileTheme* theme, GuiControlProfile* profile)
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
    // Empty keeps the base recipe's transparent fill and themed text, and is the
    // profile a layout control wants: it wears the Empty border so it never draws
    // an edge, regardless of the theme's borderSize. (The base recipe leaves the
    // Rimmed border on, which every category that wants definition inherits.)
    stampProfileBase(theme, profile);
    stampProfileBorders(theme, profile, "Empty", NULL, NULL, NULL, NULL);
}

static void stampTooltipProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    stampProfileBase(theme, profile);
    stampProfileFont(theme, profile, theme->getFontBody(), -2);
    STAMP_FIELD(profile, "fillColor", mFillColor, alphaOf(theme->getColorBackground(), 220));
    STAMP_FIELD(profile, "fontColor", mFontColor, theme->getColorHighlight());
    stampProfileBorders(theme, profile, "Highlight", NULL, NULL, NULL, NULL);
}

static void stampPanelProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& bg = theme->getColorBackground();
    stampProfileBase(theme, profile);
    STAMP_FIELD(profile, "fillColor", mFillColor, bg);
    STAMP_FIELD(profile, "fillColorHL", mFillColorHL, adj(bg, 10));
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, adj(bg, 15));
    STAMP_FIELD(profile, "fillColorNA", mFillColorNA, alphaOf(bg, 100));
    stampProfileBorders(theme, profile, "Light", NULL, NULL, NULL, NULL);
}

static void stampButtonProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& bg = theme->getColorBackground();
    const ColorI& accent = theme->getColorAccent();
    stampProfileBase(theme, profile);
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
    // Raised bevel: a Light default (so top/left catch the light) with Dark on the
    // bottom/right for the shadow.
    stampProfileBorders(theme, profile, "Light", NULL, "Dark", NULL, "Dark");
}

static void stampCheckBoxProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    stampButtonProfile(theme, profile);
}

static void stampRadioProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& text = theme->getColorForeground();
    const ColorI& highlight = theme->getColorHighlight();
    stampProfileBase(theme, profile);
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
    // The radio draws a circle (renderBorderedCircle) that reads only the default
    // border -- side borders don't apply to a circle -- so give it a single clean
    // rim and leave the sides on the default. A padding-free border also keeps the
    // circle from being squeezed to nothing.
    stampProfileBorders(theme, profile, "Rimmed", NULL, NULL, NULL, NULL);
}

static void stampLabelProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    stampProfileBase(theme, profile);
    STAMP_FIELD(profile, "align", mAlignment, AlignmentType::LeftAlign);
}

static void stampTextEditProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& bg = theme->getColorBackground();
    const ColorI& text = theme->getColorForeground();
    stampProfileBase(theme, profile);
    stampProfileFont(theme, profile, theme->getFontCode(), 0);
    // A light field built from the foreground color, with dark (background) text.
    STAMP_FIELD(profile, "fillColor", mFillColor, text);
    STAMP_FIELD(profile, "fillColorHL", mFillColorHL, adj(text, 10));
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, adj(text, 15));
    STAMP_FIELD(profile, "fillColorNA", mFillColorNA, alphaOf(text, 100));
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
    stampProfileBorders(theme, profile, "Rimmed", "PaddedRim", "PaddedRim", NULL, NULL);
}

static void stampScrollProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    // A scroll view almost always sits inside a window/panel, so it carries no
    // border of its own; a faint surface tint just separates it from behind.
    stampProfileBase(theme, profile);
    STAMP_FIELD(profile, "fillColor", mFillColor, alphaOf(theme->getColorSurface(), 50));
    stampProfileBorders(theme, profile, "Empty", NULL, NULL, NULL, NULL);
}

static void stampScrollTrackProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& bg = theme->getColorBackground();
    stampProfileBase(theme, profile);
    STAMP_FIELD(profile, "fillColor", mFillColor, bg);
    STAMP_FIELD(profile, "fillColorHL", mFillColorHL, bg);
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, bg);
    STAMP_FIELD(profile, "fillColorNA", mFillColorNA, bg);
}

static void stampScrollThumbProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& text = theme->getColorForeground();
    stampProfileBase(theme, profile);
    STAMP_FIELD(profile, "fillColor", mFillColor, text);
    STAMP_FIELD(profile, "fillColorHL", mFillColorHL, adj(text, 10));
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, theme->getColorAccent());
    STAMP_FIELD(profile, "fillColorNA", mFillColorNA, alphaOf(text, 100));
    stampProfileBorders(theme, profile, "BevelLight", NULL, "BevelDark", NULL, "BevelDark");
}

static void stampScrollArrowProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& text = theme->getColorForeground();
    stampProfileBase(theme, profile);
    STAMP_FIELD(profile, "fillColor", mFillColor, text);
    STAMP_FIELD(profile, "fillColorHL", mFillColorHL, adj(text, 10));
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, theme->getColorAccent());
    STAMP_FIELD(profile, "fillColorNA", mFillColorNA, alphaOf(text, 100));
    STAMP_FIELD(profile, "fontColor", mFontColor, ColorI(0, 0, 0, 100));
    STAMP_FIELD(profile, "fontColorHL", mFontColorHL, ColorI(0, 0, 0, 150));
    STAMP_FIELD(profile, "fontColorSL", mFontColorSL, theme->getColorHighlight());
    STAMP_FIELD(profile, "fontColorNA", mFontColorNA, ColorI(0, 0, 0, 50));
    stampProfileBorders(theme, profile, "PaddedLight", NULL, "PaddedDark", NULL, "PaddedDark");
}

static void stampSliderProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    // The slider's main profile styles the groove/track: a subtle sunken channel
    // (dark top/left, bright bottom/right). The thumb is styled by SliderThumb.
    const ColorI& bg = theme->getColorBackground();
    stampProfileBase(theme, profile);
    STAMP_FIELD(profile, "fillColor", mFillColor, adj(bg, -5));
    STAMP_FIELD(profile, "fillColorHL", mFillColorHL, adj(bg, -5));
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, adj(bg, -5));
    STAMP_FIELD(profile, "fillColorNA", mFillColorNA, alphaOf(bg, 100));
    // Sunken groove: a Dark default (top/left in shadow) with Light on the
    // bottom/right.
    stampProfileBorders(theme, profile, "Dark", NULL, "Light", NULL, "Light");
}

static void stampSliderThumbProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    // The draggable thumb: a raised bevel (bright top/left, dark bottom/right),
    // mirroring the scroll thumb recipe.
    const ColorI& text = theme->getColorForeground();
    stampProfileBase(theme, profile);
    STAMP_FIELD(profile, "fillColor", mFillColor, text);
    STAMP_FIELD(profile, "fillColorHL", mFillColorHL, adj(text, 10));
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, theme->getColorAccent());
    STAMP_FIELD(profile, "fillColorNA", mFillColorNA, alphaOf(text, 100));
    stampProfileBorders(theme, profile, "Light", NULL, "Dark", NULL, "Dark");
}

static void stampTabBookProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    stampProfileBase(theme, profile);
    stampProfileFont(theme, profile, theme->getFontTitle(), 0);
    STAMP_FIELD(profile, "fillColor", mFillColor, alphaOf(theme->getColorBackground(), 100));
}

static void stampTabProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& bg = theme->getColorBackground();
    const ColorI& text = theme->getColorForeground();
    stampProfileBase(theme, profile);
    stampProfileFont(theme, profile, theme->getFontTitle(), 0);
    STAMP_FIELD(profile, "fillColor", mFillColor, bg);
    STAMP_FIELD(profile, "fillColorHL", mFillColorHL, adj(bg, 10));
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, adj(bg, 15));
    STAMP_FIELD(profile, "fillColorNA", mFillColorNA, alphaOf(bg, 100));
    STAMP_FIELD(profile, "fontColor", mFontColor, text);
    STAMP_FIELD(profile, "fontColorHL", mFontColorHL, adj(text, 10));
    STAMP_FIELD(profile, "fontColorSL", mFontColorSL, text);
    STAMP_FIELD(profile, "fontColorNA", mFontColorNA, alphaOf(text, 100));
    stampProfileBorders(theme, profile, "Thick", NULL, NULL, NULL, NULL);
}

static void stampTabPageProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& bg = theme->getColorBackground();
    stampProfileBase(theme, profile);
    STAMP_FIELD(profile, "fillColor", mFillColor, bg);
    STAMP_FIELD(profile, "fillColorHL", mFillColorHL, adj(bg, 10));
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, adj(bg, 15));
    STAMP_FIELD(profile, "fillColorNA", mFillColorNA, alphaOf(bg, 100));
}

static void stampListBoxProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& bg = theme->getColorBackground();
    const ColorI& text = theme->getColorForeground();
    stampProfileBase(theme, profile);
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
    stampProfileBorders(theme, profile, "Rimmed", "PaddedRim", NULL, NULL, NULL);
}

static void stampDropDownItemProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    stampListBoxProfile(theme, profile);
}

static void stampDropDownProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& bg = theme->getColorBackground();
    const ColorI& text = theme->getColorForeground();
    stampProfileBase(theme, profile);
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
    stampProfileBorders(theme, profile, "Light", "PaddedRim", "Dark", NULL, "Dark");
}

static void stampWindowProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& bg = theme->getColorBackground();
    stampProfileBase(theme, profile);
    // The window's own text is the title bar caption -- give it the title font,
    // a notch larger than the base size.
    stampProfileFont(theme, profile, theme->getFontTitle(), 4);
    STAMP_FIELD(profile, "fillColor", mFillColor, adj(bg, 10));
    STAMP_FIELD(profile, "fillColorHL", mFillColorHL, adj(bg, 12));
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, theme->getColorAccent());
    STAMP_FIELD(profile, "fillColorNA", mFillColorNA, bg);
    STAMP_FIELD(profile, "fontColorSL", mFontColorSL, theme->getColorHighlight());
    STAMP_FIELD(profile, "align", mAlignment, AlignmentType::LeftAlign);
    stampProfileBorders(theme, profile, "Rimmed", "PaddedRim", NULL, NULL, NULL);
}

static void stampWindowContentProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& bg = theme->getColorBackground();
    stampProfileBase(theme, profile);
    STAMP_FIELD(profile, "fillColor", mFillColor, adj(bg, -10));
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, adj(bg, -10));
    stampProfileBorders(theme, profile, "Rimmed", NULL, NULL, NULL, NULL);
}

static void stampWindowButtonProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& bg = theme->getColorBackground();
    const ColorI& text = theme->getColorForeground();
    const ColorI& accent = theme->getColorAccent();
    stampProfileBase(theme, profile);
    stampProfileFont(theme, profile, theme->getFontTitle(), 0);
    STAMP_FIELD(profile, "fillColor", mFillColor, alphaOf(bg, 150));
    STAMP_FIELD(profile, "fillColorHL", mFillColorHL, alphaOf(accent, 150));
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, adj(accent, 10));
    STAMP_FIELD(profile, "fillColorNA", mFillColorNA, bg);
    STAMP_FIELD(profile, "fontColor", mFontColor, alphaOf(text, 150));
    STAMP_FIELD(profile, "fontColorHL", mFontColorHL, alphaOf(text, 170));
    STAMP_FIELD(profile, "fontColorSL", mFontColorSL, theme->getColorHighlight());
    STAMP_FIELD(profile, "fontColorNA", mFontColorNA, alphaOf(text, 150));
    stampProfileBorders(theme, profile, "RimmedExpander", NULL, NULL, NULL, NULL);
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
    stampProfileBase(theme, profile);
    stampProfileFont(theme, profile, theme->getFontTitle(), 0);
    STAMP_FIELD(profile, "fillColor", mFillColor, adj(theme->getColorBackground(), -7));
    STAMP_FIELD(profile, "canKeyFocus", mCanKeyFocus, true);
    stampProfileBorders(theme, profile, "Rimmed", NULL, NULL, NULL, NULL);
}

static void stampMenuProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& text = theme->getColorForeground();
    stampProfileBase(theme, profile);
    stampProfileFont(theme, profile, theme->getFontTitle(), 0);
    STAMP_FIELD(profile, "fillColor", mFillColor, ColorI(0, 0, 0, 0));
    STAMP_FIELD(profile, "fillColorHL", mFillColorHL, ColorI(255, 255, 255, 10));
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, adj(theme->getColorAccent(), -15));
    STAMP_FIELD(profile, "fillColorNA", mFillColorNA, ColorI(0, 0, 0, 0));
    STAMP_FIELD(profile, "fontColor", mFontColor, text);
    STAMP_FIELD(profile, "fontColorHL", mFontColorHL, adj(text, 10));
    STAMP_FIELD(profile, "fontColorSL", mFontColorSL, text);
    STAMP_FIELD(profile, "fontColorNA", mFontColorNA, alphaOf(text, 100));
    stampProfileBorders(theme, profile, "Empty", "Padded", "Padded", NULL, NULL);
}

static void stampMenuItemProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& bg = theme->getColorBackground();
    const ColorI& text = theme->getColorForeground();
    stampProfileBase(theme, profile);
    stampProfileFont(theme, profile, theme->getFontTitle(), 0);
    STAMP_FIELD(profile, "fillColor", mFillColor, adj(bg, -5));
    STAMP_FIELD(profile, "fillColorHL", mFillColorHL, adj(theme->getColorAccent(), -15));
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, ColorI(0, 0, 0, 0));
    STAMP_FIELD(profile, "fillColorNA", mFillColorNA, bg);
    STAMP_FIELD(profile, "fontColor", mFontColor, text);
    STAMP_FIELD(profile, "fontColorHL", mFontColorHL, adj(text, 10));
    STAMP_FIELD(profile, "fontColorNA", mFontColorNA, alphaOf(text, 150));
    STAMP_FIELD(profile, "align", mAlignment, AlignmentType::LeftAlign);
    // Top and bottom take SelectedInset, so a new theme's menus arrive with
    // separators that read as separators. The sides are held back from it and
    // given the same inset with no state to it: were they to fall through, a
    // separator would pick up the 4px margin and the rim at each end too, capping
    // the rule with a tick rather than running it the width of the menu.
    stampProfileBorders(theme, profile, "SelectedInset", "Padded", "Padded", NULL, NULL);
}

static void stampMenuContentProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    stampProfileBase(theme, profile);
    STAMP_FIELD(profile, "fillColor", mFillColor, adj(theme->getColorBackground(), -5));
    stampProfileBorders(theme, profile, "Rimmed", NULL, NULL, NULL, NULL);
}

static void stampOverlayProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    // A click-catcher/scrim behind popups: a partially transparent dark wash so
    // it reads as a distinct dimming layer rather than an invisible passthrough.
    stampProfileBase(theme, profile);
    stampProfileFont(theme, profile, theme->getFontBody(), -2);
    STAMP_FIELD(profile, "fillColor", mFillColor, alphaOf(theme->getColorBackground(), 120));
}

static void stampProgressProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& accent = theme->getColorAccent();
    stampProfileBase(theme, profile);
    STAMP_FIELD(profile, "fillColor", mFillColor, theme->getColorBackground());
    STAMP_FIELD(profile, "fillColorHL", mFillColorHL, accent);
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, adj(accent, 10));
    STAMP_FIELD(profile, "fontColorHL", mFontColorHL, theme->getColorForeground());
    STAMP_FIELD(profile, "fontColorSL", mFontColorSL, theme->getColorHighlight());
    stampProfileBorders(theme, profile, "CondenserLight", NULL, "CondenserDark", NULL, "CondenserDark");
}

static void stampTreeViewProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& bg = theme->getColorBackground();
    const ColorI& text = theme->getColorForeground();
    stampProfileBase(theme, profile);
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
    stampProfileBase(theme, profile);
    STAMP_FIELD(profile, "fillColor", mFillColor, bg);
    STAMP_FIELD(profile, "fillColorHL", mFillColorHL, adj(bg, 10));
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, adj(bg, 15));
    STAMP_FIELD(profile, "fillColorNA", mFillColorNA, alphaOf(bg, 100));
}

static void stampFrameSetDropButtonProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& accent = theme->getColorAccent();
    stampProfileBase(theme, profile);
    STAMP_FIELD(profile, "fillColor", mFillColor, alphaOf(accent, 100));
    STAMP_FIELD(profile, "fillColorHL", mFillColorHL, alphaOf(accent, 180));
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, adj(accent, 10));
    STAMP_FIELD(profile, "fillColorNA", mFillColorNA, alphaOf(accent, 50));
}

static void stampColorPickerProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    stampProfileBase(theme, profile);
    STAMP_FIELD(profile, "canKeyFocus", mCanKeyFocus, true);
    stampProfileBorders(theme, profile, "Rimmed", NULL, NULL, NULL, NULL);
}

static void stampColorSelectorProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    stampProfileBase(theme, profile);
    // Deliberately hue-independent neutral ring colors, like the bevels.
    STAMP_FIELD(profile, "fillColor", mFillColor, ColorI(240, 240, 240, 255));
    STAMP_FIELD(profile, "fillColorHL", mFillColorHL, ColorI(250, 250, 250, 255));
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, theme->getColorHighlight());
    stampProfileBorders(theme, profile, "Dark", NULL, NULL, NULL, NULL);
}

static void stampColorPopupProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    const ColorI& bg = theme->getColorBackground();
    stampProfileBase(theme, profile);
    STAMP_FIELD(profile, "fillColor", mFillColor, bg);
    STAMP_FIELD(profile, "fillColorHL", mFillColorHL, bg);
    STAMP_FIELD(profile, "fillColorSL", mFillColorSL, bg);
    STAMP_FIELD(profile, "fillColorNA", mFillColorNA, bg);
    stampProfileBorders(theme, profile, "Rimmed", NULL, NULL, NULL, NULL);
}

static void stampDragAndDropProfile(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    stampProfileBase(theme, profile);
    STAMP_FIELD(profile, "fillColor", mFillColor, alphaOf(theme->getColorAccent(), 50));
    STAMP_FIELD(profile, "fontColor", mFontColor, theme->getColorHighlight());
}

//-----------------------------------------------------------------------------
// Cursor recipe. There is only the one: a pointer is the mouse's equivalent of
// text and wants the same contrast against the background, so every cursor in a
// theme takes the foreground color and the set reads as a set. Anything else is
// a per-cursor override, which costs one click in the editor.
//
// This tints art that is deliberately grayscale - black outline, white body -
// so the body takes the color and the outline stays put. A theme that brings
// its own colored art sets the tint to white and this stops mattering.
//-----------------------------------------------------------------------------

static void stampCursor(GuiProfileTheme* theme, GuiCursor* cursor)
{
    STAMP_FIELD(cursor, "color", mColor, theme->getColorForeground());
}

//-----------------------------------------------------------------------------
// The engine-defined category tables: the canonical set of profiles a
// complete theme provides, one entry per profile slot the stock GuiControls
// consume. Adding a control with a new slot means adding its category here.
//-----------------------------------------------------------------------------

const GuiProfileTheme::ProfileCategory GuiProfileTheme::smProfileCategories[] =
{
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
    { "Slider",             "SliderProfile",             stampSliderProfile },
    { "SliderThumb",        "SliderThumbProfile",        stampSliderThumbProfile },
};
const S32 GuiProfileTheme::smProfileCategoryCount = sizeof(smProfileCategories) / sizeof(smProfileCategories[0]);

const GuiProfileTheme::BorderCategory GuiProfileTheme::smBorderCategories[] =
{
    // The named-border palette, all shown in the editor and theme-tracked. The
    // first six are the primitives; the rest are descriptive combinations.
    { "Empty",          "EmptyBorder",          stampEmptyBorder },
    { "Rimmed",         "RimmedBorder",         stampRimmedBorder },
    { "Thick",          "ThickBorder",          stampThickBorder },
    { "Light",          "LightBorder",          stampLightBorder },
    { "Dark",           "DarkBorder",           stampDarkBorder },
    { "Padded",         "PaddedBorder",         stampPaddedBorder },
    { "Highlight",      "HighlightBorder",      stampHighlightBorder },
    { "PaddedRim",      "PaddedRimBorder",      stampPaddedRimBorder },
    { "BevelLight",     "BevelLightBorder",     stampBevelLightBorder },
    { "BevelDark",      "BevelDarkBorder",      stampBevelDarkBorder },
    { "PaddedLight",    "PaddedLightBorder",    stampPaddedLightBorder },
    { "PaddedDark",     "PaddedDarkBorder",     stampPaddedDarkBorder },
    { "RimmedExpander", "RimmedExpanderBorder", stampRimmedExpanderBorder },
    { "CondenserLight", "CondenserLightBorder", stampCondenserLightBorder },
    { "CondenserDark",  "CondenserDarkBorder",  stampCondenserDarkBorder },
    { "SelectedInset",  "SelectedInsetBorder",  stampSelectedInsetBorder },
};
const S32 GuiProfileTheme::smBorderCategoryCount = sizeof(smBorderCategories) / sizeof(smBorderCategories[0]);

// The seven cursors the engine can ask for by name.
//
// The placement values started as the ones AppCore's hand-written cursors had
// always used and are now what came back from actually aiming them in the
// hot-spot editor -- which is the point of having built it. Five of the seven
// moved: the resize cursors want their crosshair centred on the pointer rather
// than a pixel down and right of it, and the two bars want one pixel of lift so
// the gap between their arrowheads straddles the edge being dragged.
//
// A "0" hot spot with a centred anchor is not a missing value: the anchor does
// the placing, and the nudge is only what the anchor cannot express.
const GuiProfileTheme::CursorCategory GuiProfileTheme::smCursorCategories[] =
{
    { "Default",   "DefaultCursor",   "defaultCursor.png", 1,  1, 0.0f, 0.0f, stampCursor },
    { "Edit",      "EditCursor",      "ibeam.png",         0,  0, 0.5f, 0.5f, stampCursor },
    { "Move",      "MoveCursor",      "move.png",          0,  0, 0.5f, 0.5f, stampCursor },
    { "LeftRight", "LeftRightCursor", "leftRight.png",     0, -1, 0.5f, 0.5f, stampCursor },
    { "UpDown",    "UpDownCursor",    "upDown.png",        0, -1, 0.5f, 0.5f, stampCursor },
    { "NWSE",      "NWSECursor",      "NWSE.png",          0,  0, 0.5f, 0.5f, stampCursor },
    { "NESW",      "NESWCursor",      "NESW.png",          0,  0, 0.5f, 0.5f, stampCursor },
};
const S32 GuiProfileTheme::smCursorCategoryCount = sizeof(smCursorCategories) / sizeof(smCursorCategories[0]);

//-----------------------------------------------------------------------------

GuiProfileTheme::GuiProfileTheme()
{
    mTamlReading = false;

    mFontBody = StringTable->insert("Arial");
    mFontTitle = StringTable->insert("Arial");
    mFontCode = StringTable->insert("Courier New");
    mFontDirectory = StringTable->EmptyString;
    mCursorDirectory = StringTable->EmptyString;
    mFontSize = 12;

    // Semantic dark palette. Every profile fill/border is derived from these six
    // via adjustValue()/setAlpha(), so this is the single lever for a theme's
    // look. Values are a considered starting point meant to be tuned live against
    // the editor preview; the recipes bind to the getters, not to literals.
    mColorBackground.set(38, 40, 46, 255);    // cool near-black with a little depth
    mColorSurface.set(74, 86, 100, 255);      // muted slate for raised surfaces
    mColorForeground.set(230, 232, 236, 255); // crisp off-white text
    mColorAccent.set(58, 142, 210, 255);      // vivid interaction blue
    mColorHighlight.set(245, 210, 50, 255);   // warm selection yellow
    mColorWarning.set(206, 62, 78, 255);      // destructive red

    mBorderSize = 1;

    mDefaultProfiles.setSize(smProfileCategoryCount);
    for (S32 i = 0; i < smProfileCategoryCount; ++i)
        mDefaultProfiles[i] = NULL;

    mDefaultBorders.setSize(smBorderCategoryCount);
    for (S32 i = 0; i < smBorderCategoryCount; ++i)
        mDefaultBorders[i] = NULL;

    mDefaultCursors.setSize(smCursorCategoryCount);
    for (S32 i = 0; i < smCursorCategoryCount; ++i)
        mDefaultCursors[i] = NULL;
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

    // Where this theme's own cursor art lives, relative to the game root. Each
    // theme gets its own folder so two themes can carry dramatically different
    // cursors without one overwriting the other's files. Filled by whoever
    // seeds the art (the Profile Editor, or AppCore for the stock theme); a
    // theme that names none simply has cursors with no bitmap yet.
    addField("cursorDirectory", TypeString, Offset(mCursorDirectory, GuiProfileTheme));
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

    while (mExtraCursors.size() > 0)
        mExtraCursors.last()->deleteObject();

    for (S32 i = 0; i < smCursorCategoryCount; ++i)
    {
        if (mDefaultCursors[i] != NULL)
        {
            GuiCursor* cursor = mDefaultCursors[i];
            mDefaultCursors[i] = NULL;
            cursor->deleteObject();
        }
    }

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

    for (S32 i = 0; i < mExtraCursors.size(); ++i)
    {
        if (mExtraCursors[i] == object)
        {
            mExtraCursors.erase(i);
            break;
        }
    }

    for (S32 i = 0; i < smCursorCategoryCount; ++i)
    {
        if (mDefaultCursors[i] == object)
            mDefaultCursors[i] = NULL;
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

StringTableEntry GuiProfileTheme::getCursorCategoryName(S32 index)
{
    if (index < 0 || index >= smCursorCategoryCount)
        return NULL;

    return StringTable->insert(smCursorCategories[index].name);
}

S32 GuiProfileTheme::findCursorCategoryIndex(StringTableEntry categoryName)
{
    for (S32 i = 0; i < smCursorCategoryCount; ++i)
    {
        if (StringTable->insert(smCursorCategories[i].name) == categoryName)
            return i;
    }
    return -1;
}

const char* GuiProfileTheme::getCursorStockFile(S32 index)
{
    if (index < 0 || index >= smCursorCategoryCount)
        return "";

    return smCursorCategories[index].stockFile;
}

const char* GuiProfileTheme::getCursorCanonicalName(S32 index)
{
    if (index < 0 || index >= smCursorCategoryCount)
        return "";

    return smCursorCategories[index].suffix;
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

GuiCursor* GuiProfileTheme::getCursor(StringTableEntry categoryName) const
{
    const S32 index = findCursorCategoryIndex(categoryName);
    return (index >= 0) ? mDefaultCursors[index] : NULL;
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

GuiCursor* GuiProfileTheme::createMemberCursor(S32 categoryIndex, const char* objectName)
{
    const CursorCategory& category = smCursorCategories[categoryIndex];

    GuiCursor* cursor = new GuiCursor();

    char nameBuffer[256];
    if (objectName == NULL && getName() != NULL && *getName() != '\0')
    {
        dSprintf(nameBuffer, sizeof(nameBuffer), "%s%s", getName(), category.suffix);
        objectName = nameBuffer;
    }
    if (objectName != NULL && *objectName != '\0')
        cursor->assignName(objectName);

    if (!cursor->registerObject())
    {
        delete cursor;
        return NULL;
    }

    cursor->mCategory = StringTable->insert(category.name);

    // Placement comes from the table and belongs to the art, so it is set here
    // rather than stamped -- a restamp must never move a hot spot the user
    // tuned. setTheme comes after, so these writes are not seen as overrides.
    cursor->setHotSpot(Point2I(category.hotSpotX, category.hotSpotY));
    cursor->setRenderOffset(Point2F(category.renderOffsetX, category.renderOffsetY));

    cursor->setTheme(this);
    fillCursorArt(cursor, categoryIndex);
    deleteNotify(cursor);

    return cursor;
}

void GuiProfileTheme::fillCursorArt(GuiCursor* cursor, S32 categoryIndex)
{
    if (cursor == NULL || mCursorDirectory == NULL || *mCursorDirectory == '\0')
        return;

    // Only ever fills a blank. A cursor pointed at the user's own art keeps it
    // through every restamp, which is the whole difference between art and the
    // derived fields around it.
    const StringTableEntry current = cursor->getBitmapName();
    if (current != NULL && *current != '\0')
        return;

    char pathBuffer[1024];
    dSprintf(pathBuffer, sizeof(pathBuffer), "%s/%s", mCursorDirectory, smCursorCategories[categoryIndex].stockFile);

    // Through setDataField so TypeFilename expands it: the directory is stored
    // relative to the game root, and the texture manager wants a real path.
    // GuiCursor treats bitmapName as art, so this does not mark an override.
    cursor->setDataField(StringTable->insert("bitmapName"), NULL, pathBuffer);
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

// An extra cursor belongs to a category, exactly as an extra profile does: a
// theme with two "Default" cursors is offering a choice between two pointers,
// which is the case the Gui Editor shows a cursor slot for. It starts on the
// category's stock art; the editor gives it a copy of its own to edit.
GuiCursor* GuiProfileTheme::createCursor(const char* categoryName, const char* objectName)
{
    const S32 categoryIndex = findCursorCategoryIndex(StringTable->insert(categoryName));
    if (categoryIndex < 0)
    {
        Con::warnf("GuiProfileTheme::createCursor() - unknown category '%s'.", categoryName);
        return NULL;
    }

    // Generate <ThemeName><Suffix><N> when no name is given.
    char nameBuffer[256];
    if ((objectName == NULL || *objectName == '\0') && getName() != NULL && *getName() != '\0')
    {
        for (S32 n = 2; n < 1000000; ++n)
        {
            dSprintf(nameBuffer, sizeof(nameBuffer), "%s%s%d", getName(), smCursorCategories[categoryIndex].suffix, n);
            if (Sim::findObject(nameBuffer) == NULL)
                break;
        }
        objectName = nameBuffer;
    }

    GuiCursor* cursor = createMemberCursor(categoryIndex, objectName);
    if (cursor == NULL)
        return NULL;

    mExtraCursors.push_back(cursor);
    smCursorCategories[categoryIndex].stamp(this, cursor);

    return cursor;
}

bool GuiProfileTheme::removeCursor(GuiCursor* cursor)
{
    for (S32 i = 0; i < mExtraCursors.size(); ++i)
    {
        if (mExtraCursors[i] == cursor)
        {
            // Deletion notifies us back and erases the list entry.
            cursor->deleteObject();
            return true;
        }
    }

    // Default members are never removed: a theme is always complete.
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

    for (S32 i = 0; i < smCursorCategoryCount; ++i)
    {
        if (mDefaultCursors[i] == NULL)
            continue;
        dSprintf(nameBuffer, sizeof(nameBuffer), "%s%s", newName, smCursorCategories[i].suffix);
        PendingRename rename = { mDefaultCursors[i], StringTable->insert(nameBuffer) };
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

    for (S32 i = 0; i < mExtraCursors.size(); ++i)
    {
        const char* extraName = mExtraCursors[i]->getName();
        if (!hasOldName || extraName == NULL || dStrncmp(extraName, oldName, oldNameLength) != 0)
            continue;
        dSprintf(nameBuffer, sizeof(nameBuffer), "%s%s", newName, extraName + oldNameLength);
        PendingRename rename = { mExtraCursors[i], StringTable->insert(nameBuffer) };
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

    // Cursors. fillCursorArt runs on every pass rather than only at creation:
    // a theme usually learns where its cursor folder is after its members
    // already exist (the editor names the folder once the theme has a name),
    // and it is also what re-points a member whose art went missing.
    for (S32 i = 0; i < smCursorCategoryCount; ++i)
    {
        if (mDefaultCursors[i] == NULL)
            mDefaultCursors[i] = createMemberCursor(i, NULL);
        if (mDefaultCursors[i] != NULL)
        {
            fillCursorArt(mDefaultCursors[i], i);
            smCursorCategories[i].stamp(this, mDefaultCursors[i]);
        }
    }

    for (S32 i = 0; i < mExtraCursors.size(); ++i)
    {
        const S32 categoryIndex = findCursorCategoryIndex(mExtraCursors[i]->mCategory);
        if (categoryIndex >= 0)
        {
            fillCursorArt(mExtraCursors[i], categoryIndex);
            smCursorCategories[categoryIndex].stamp(this, mExtraCursors[i]);
        }
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
    U32 count = (U32)mExtraProfiles.size() + (U32)mExtraBorders.size() + (U32)mExtraCursors.size();

    for (S32 i = 0; i < smBorderCategoryCount; ++i)
    {
        if (mDefaultBorders[i] != NULL)
            ++count;
    }

    for (S32 i = 0; i < smCursorCategoryCount; ++i)
    {
        if (mDefaultCursors[i] != NULL)
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

    // Cursors reference nothing and are referenced by nothing inside the file,
    // so their position is free; they sit between the borders and the profiles
    // to keep the written order stable and readable.
    for (S32 i = 0; i < smCursorCategoryCount; ++i)
    {
        if (mDefaultCursors[i] == NULL)
            continue;
        if (index == 0)
            return mDefaultCursors[i];
        --index;
    }

    if (index < (U32)mExtraCursors.size())
        return mExtraCursors[index];
    index -= (U32)mExtraCursors.size();

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

    GuiCursor* cursor = dynamic_cast<GuiCursor*>(pSimObject);
    if (cursor != NULL)
    {
        const S32 categoryIndex = findCursorCategoryIndex(cursor->mCategory);
        if (categoryIndex < 0)
        {
            Con::warnf("GuiProfileTheme::addTamlChild() - cursor child with unknown category '%s' left unattached.", cursor->mCategory);
            return;
        }

        // First one in a category is that category's default; the rest are the
        // extras the user added. Same rule as profiles.
        if (mDefaultCursors[categoryIndex] == NULL)
            mDefaultCursors[categoryIndex] = cursor;
        else
            mExtraCursors.push_back(cursor);

        cursor->setTheme(this, true);
        deleteNotify(cursor);
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
