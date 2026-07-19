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

//-----------------------------------------------------------------------------

IMPLEMENT_CONOBJECT(GuiProfileTheme);

//-----------------------------------------------------------------------------
// Category recipes. Each stamp function derives every themed field of one
// member from the theme's values, skipping fields the member has explicitly
// overridden. Recipes write raw members directly (never setDataField), so
// stamping cannot mark overrides.
//-----------------------------------------------------------------------------

/// Stamp a member field unless the member has explicitly overridden it.
#define STAMP_FIELD(object, fieldName, member, value) \
    if (!(object)->isThemeFieldOverridden(StringTable->insert(fieldName))) (object)->member = (value)

static void stampDefault(GuiProfileTheme* theme, GuiControlProfile* profile)
{
    STAMP_FIELD(profile, "fillColor", mFillColor, theme->getColorBackground());
    STAMP_FIELD(profile, "fontType", mFontType, theme->getFontBody());
    STAMP_FIELD(profile, "fontDirectory", mFontDirectory, theme->getFontDirectory());
    STAMP_FIELD(profile, "fontSize", mFontSize, theme->getFontSize());
    STAMP_FIELD(profile, "fontColor", mFontColor, theme->getColorText());
}

static void stampDefaultBorder(GuiProfileTheme* theme, GuiBorderProfile* border)
{
    STAMP_FIELD(border, "border", mBorder[0], 0);
    STAMP_FIELD(border, "borderHL", mBorder[1], 0);
    STAMP_FIELD(border, "borderSL", mBorder[2], 0);
    STAMP_FIELD(border, "borderNA", mBorder[3], 0);
    STAMP_FIELD(border, "borderColor", mBorderColor[0], theme->getColorBackground());
    STAMP_FIELD(border, "borderColorHL", mBorderColor[1], theme->getColorBackground());
    STAMP_FIELD(border, "borderColorSL", mBorderColor[2], theme->getColorBackground());
    STAMP_FIELD(border, "borderColorNA", mBorderColor[3], theme->getColorBackground());
}

//-----------------------------------------------------------------------------
// The engine-defined category tables: the canonical set of profiles a
// complete theme provides, one entry per profile slot the stock GuiControls
// consume. Adding a control with a new slot means adding its category here.
//-----------------------------------------------------------------------------

const GuiProfileTheme::ProfileCategory GuiProfileTheme::smProfileCategories[] =
{
    { "Default",            "DefaultProfile",            stampDefault },
    { "Empty",              "EmptyProfile",              stampDefault },
    { "Tooltip",            "TooltipProfile",            stampDefault },
    { "Panel",              "PanelProfile",              stampDefault },
    { "Button",             "ButtonProfile",             stampDefault },
    { "CheckBox",           "CheckBoxProfile",           stampDefault },
    { "Radio",              "RadioProfile",              stampDefault },
    { "Label",              "LabelProfile",              stampDefault },
    { "TextEdit",           "TextEditProfile",           stampDefault },
    { "Scroll",             "ScrollProfile",             stampDefault },
    { "ScrollTrack",        "ScrollTrackProfile",        stampDefault },
    { "ScrollThumb",        "ScrollThumbProfile",        stampDefault },
    { "ScrollArrow",        "ScrollArrowProfile",        stampDefault },
    { "TabBook",            "TabBookProfile",            stampDefault },
    { "Tab",                "TabProfile",                stampDefault },
    { "TabPage",            "TabPageProfile",            stampDefault },
    { "ListBox",            "ListBoxProfile",            stampDefault },
    { "DropDown",           "DropDownProfile",           stampDefault },
    { "DropDownItem",       "DropDownItemProfile",       stampDefault },
    { "Window",             "WindowProfile",             stampDefault },
    { "WindowContent",      "WindowContentProfile",      stampDefault },
    { "WindowButton",       "WindowButtonProfile",       stampDefault },
    { "WindowCloseButton",  "WindowCloseButtonProfile",  stampDefault },
    { "MenuBar",            "MenuBarProfile",            stampDefault },
    { "Menu",               "MenuProfile",               stampDefault },
    { "MenuItem",           "MenuItemProfile",           stampDefault },
    { "MenuContent",        "MenuContentProfile",        stampDefault },
    { "Overlay",            "OverlayProfile",            stampDefault },
    { "Progress",           "ProgressProfile",           stampDefault },
    { "TreeView",           "TreeViewProfile",           stampDefault },
    { "FrameSet",           "FrameSetProfile",           stampDefault },
    { "FrameSetDropButton", "FrameSetDropButtonProfile", stampDefault },
    { "ColorPicker",        "ColorPickerProfile",        stampDefault },
    { "ColorSelector",      "ColorSelectorProfile",      stampDefault },
    { "ColorPopup",         "ColorPopupProfile",         stampDefault },
    { "DragAndDrop",        "DragAndDropProfile",        stampDefault },
};
const S32 GuiProfileTheme::smProfileCategoryCount = sizeof(smProfileCategories) / sizeof(smProfileCategories[0]);

const GuiProfileTheme::BorderCategory GuiProfileTheme::smBorderCategories[] =
{
    { "Default", "DefaultBorder", stampDefaultBorder },
};
const S32 GuiProfileTheme::smBorderCategoryCount = sizeof(smBorderCategories) / sizeof(smBorderCategories[0]);

//-----------------------------------------------------------------------------

GuiProfileTheme::GuiProfileTheme()
{
    mFontBody = StringTable->insert("Arial");
    mFontTitle = StringTable->insert("Arial");
    mFontCode = StringTable->insert("Courier New");
    mFontDirectory = StringTable->EmptyString;
    mFontSize = 12;

    mColorBackground.set(43, 43, 43, 255);
    mColorPanel.set(81, 92, 102, 255);
    mColorTextSubtle.set(160, 160, 160, 255);
    mColorText.set(224, 224, 224, 255);
    mColorAccent.set(54, 135, 196, 255);
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
        addField("colorPanel", TypeColorI, Offset(mColorPanel, GuiProfileTheme));
        addField("colorTextSubtle", TypeColorI, Offset(mColorTextSubtle, GuiProfileTheme));
        addField("colorText", TypeColorI, Offset(mColorText, GuiProfileTheme));
        addField("colorAccent", TypeColorI, Offset(mColorAccent, GuiProfileTheme));
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

void GuiProfileTheme::restamp()
{
    if (!isProperlyAdded())
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
