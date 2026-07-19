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

ConsoleMethodGroupBeginWithDocs(GuiProfileTheme, SimObject)

/*! Gets the default member profile for a category.
    @param category The category name (see getCategoryNames).
    @return The GuiControlProfile id, or 0 if the category is unknown.
*/
ConsoleMethodWithDocs(GuiProfileTheme, getProfile, ConsoleInt, 3, 3, (category))
{
    GuiControlProfile* profile = object->getProfile(StringTable->insert(argv[2]));
    return profile != NULL ? profile->getId() : 0;
}

/*! Gets all member profiles for a category: the default first, then extras.
    @param category The category name (see getCategoryNames).
    @return A space-separated list of GuiControlProfile ids.
*/
ConsoleMethodWithDocs(GuiProfileTheme, getProfiles, ConsoleString, 3, 3, (category))
{
    StringTableEntry category = StringTable->insert(argv[2]);

    char* buffer = Con::getReturnBuffer(1024);
    S32 offset = 0;
    buffer[0] = '\0';

    GuiControlProfile* profile = object->getProfile(category);
    if (profile != NULL)
        offset += dSprintf(buffer + offset, 1024 - offset, "%d", profile->getId());

    const Vector<GuiControlProfile*>& extras = object->getExtraProfiles();
    for (S32 i = 0; i < extras.size(); ++i)
    {
        if (extras[i]->mCategory != category)
            continue;
        offset += dSprintf(buffer + offset, 1024 - offset, "%s%d", offset > 0 ? " " : "", extras[i]->getId());
    }

    return buffer;
}

/*! Gets the number of member profiles in the theme (defaults plus extras).
    @return The member profile count.
*/
ConsoleMethodWithDocs(GuiProfileTheme, getProfileCount, ConsoleInt, 2, 2, ())
{
    return object->getProfileCount();
}

/*! Gets the member border profile for a border category.
    @param category The border category name (see getBorderCategoryNames).
    @return The GuiBorderProfile id, or 0 if the category is unknown.
*/
ConsoleMethodWithDocs(GuiProfileTheme, getBorder, ConsoleInt, 3, 3, (category))
{
    GuiBorderProfile* border = object->getBorder(StringTable->insert(argv[2]));
    return border != NULL ? border->getId() : 0;
}

/*! Gets the engine-defined profile category names.
    @return A space-separated list of category names.
*/
ConsoleMethodWithDocs(GuiProfileTheme, getCategoryNames, ConsoleString, 2, 2, ())
{
    char* buffer = Con::getReturnBuffer(1024);
    S32 offset = 0;
    buffer[0] = '\0';

    for (S32 i = 0; i < GuiProfileTheme::getCategoryCount(); ++i)
        offset += dSprintf(buffer + offset, 1024 - offset, "%s%s", i > 0 ? " " : "", GuiProfileTheme::getCategoryName(i));

    return buffer;
}

/*! Gets the engine-defined border category names.
    @return A space-separated list of border category names.
*/
ConsoleMethodWithDocs(GuiProfileTheme, getBorderCategoryNames, ConsoleString, 2, 2, ())
{
    char* buffer = Con::getReturnBuffer(1024);
    S32 offset = 0;
    buffer[0] = '\0';

    for (S32 i = 0; i < GuiProfileTheme::getBorderCategoryCount(); ++i)
        offset += dSprintf(buffer + offset, 1024 - offset, "%s%s", i > 0 ? " " : "", GuiProfileTheme::getBorderCategoryName(i));

    return buffer;
}

/*! Creates an extra member profile in a category, alongside the default.
    @param category The category name (see getCategoryNames).
    @param name Optional object name; defaults to <ThemeName><Suffix><N>.
    @return The new GuiControlProfile id, or 0 on failure.
*/
ConsoleMethodWithDocs(GuiProfileTheme, createProfile, ConsoleInt, 3, 4, (category, [name]))
{
    GuiControlProfile* profile = object->createProfile(argv[2], argc > 3 ? argv[3] : NULL);
    return profile != NULL ? profile->getId() : 0;
}

/*! Removes an extra member profile. Default members are refused: a theme
    always provides one profile per category.
    @param profile The extra profile to remove.
    @return True if the profile was an extra and was removed.
*/
ConsoleMethodWithDocs(GuiProfileTheme, removeProfile, ConsoleBool, 3, 3, (profile))
{
    GuiControlProfile* profile = dynamic_cast<GuiControlProfile*>(Sim::findObject(argv[2]));
    if (profile == NULL)
    {
        Con::warnf("GuiProfileTheme::removeProfile() - could not find profile '%s'.", argv[2]);
        return false;
    }

    return object->removeProfile(profile);
}

/*! Clears a member's override on one field, so the field re-derives from the
    theme. Accepts a member profile or border profile.
    @param member The member profile or border profile.
    @param fieldName The field to reset.
    @return No return value.
*/
ConsoleMethodWithDocs(GuiProfileTheme, resetField, ConsoleVoid, 4, 4, (member, fieldName))
{
    SimObject* target = Sim::findObject(argv[2]);
    StringTableEntry field = StringTable->insert(argv[3]);

    GuiControlProfile* profile = dynamic_cast<GuiControlProfile*>(target);
    if (profile != NULL && profile->getTheme() == object)
    {
        profile->clearThemeFieldOverride(field);
        object->restamp();
        return;
    }

    GuiBorderProfile* border = dynamic_cast<GuiBorderProfile*>(target);
    if (border != NULL && border->getTheme() == object)
    {
        border->clearThemeFieldOverride(field);
        object->restamp();
        return;
    }

    Con::warnf("GuiProfileTheme::resetField() - '%s' is not a member of this theme.", argv[2]);
}

/*! Clears all of a member's overrides, so every field re-derives from the
    theme. Accepts a member profile or border profile.
    @param member The member profile or border profile.
    @return No return value.
*/
ConsoleMethodWithDocs(GuiProfileTheme, resetProfile, ConsoleVoid, 3, 3, (member))
{
    SimObject* target = Sim::findObject(argv[2]);

    GuiControlProfile* profile = dynamic_cast<GuiControlProfile*>(target);
    if (profile != NULL && profile->getTheme() == object)
    {
        profile->clearAllThemeOverrides();
        object->restamp();
        return;
    }

    GuiBorderProfile* border = dynamic_cast<GuiBorderProfile*>(target);
    if (border != NULL && border->getTheme() == object)
    {
        border->clearAllThemeOverrides();
        object->restamp();
        return;
    }

    Con::warnf("GuiProfileTheme::resetProfile() - '%s' is not a member of this theme.", argv[2]);
}

/*! Checks whether a member has overridden a field away from the theme.
    @param member The member profile or border profile.
    @param fieldName The field to check.
    @return True if the field is overridden.
*/
ConsoleMethodWithDocs(GuiProfileTheme, isFieldOverridden, ConsoleBool, 4, 4, (member, fieldName))
{
    SimObject* target = Sim::findObject(argv[2]);
    StringTableEntry field = StringTable->insert(argv[3]);

    GuiControlProfile* profile = dynamic_cast<GuiControlProfile*>(target);
    if (profile != NULL)
        return profile->isThemeFieldOverridden(field);

    GuiBorderProfile* border = dynamic_cast<GuiBorderProfile*>(target);
    if (border != NULL)
        return border->isThemeFieldOverridden(field);

    return false;
}

/*! Recreates any missing default members and re-derives every non-overridden
    member field from the current theme values.
    @return No return value.
*/
ConsoleMethodWithDocs(GuiProfileTheme, restamp, ConsoleVoid, 2, 2, ())
{
    object->restamp();
}

/*! Shifts a color's brightness by a percentage, preserving hue and alpha.
    Positive percentages brighten, negative darken.
    @param color The color as "red green blue alpha".
    @param percent The brightness shift in percentage points.
    @return The adjusted color as "red green blue alpha".
*/
ConsoleMethodWithDocs(GuiProfileTheme, adjustValue, ConsoleString, 4, 4, (color, percent))
{
    S32 red = 0, green = 0, blue = 0, alpha = 255;
    dSscanf(argv[2], "%d %d %d %d", &red, &green, &blue, &alpha);

    const ColorI result = GuiProfileTheme::adjustValue(ColorI(U8(red), U8(green), U8(blue), U8(alpha)), dAtof(argv[3]));

    char* buffer = Con::getReturnBuffer(32);
    dSprintf(buffer, 32, "%d %d %d %d", result.red, result.green, result.blue, result.alpha);
    return buffer;
}

/*! Replaces a color's alpha, clamping to 0..255.
    @param color The color as "red green blue alpha".
    @param alpha The new alpha value.
    @return The color with the new alpha as "red green blue alpha".
*/
ConsoleMethodWithDocs(GuiProfileTheme, setAlpha, ConsoleString, 4, 4, (color, alpha))
{
    S32 red = 0, green = 0, blue = 0, alpha = 255;
    dSscanf(argv[2], "%d %d %d %d", &red, &green, &blue, &alpha);

    const ColorI result = GuiProfileTheme::setAlpha(ColorI(U8(red), U8(green), U8(blue), U8(alpha)), dAtoi(argv[3]));

    char* buffer = Con::getReturnBuffer(32);
    dSprintf(buffer, 32, "%d %d %d %d", result.red, result.green, result.blue, result.alpha);
    return buffer;
}

ConsoleMethodGroupEndWithDocs(GuiProfileTheme)
