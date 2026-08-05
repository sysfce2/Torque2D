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

/*! Creates a single-use custom border owned by the theme. A custom border is
    not a category member: it keeps its own field values and never appears in
    getBorderCategoryNames. Used by the Profile Editor's per-side "Custom..."
    flow, named e.g. <ProfileName><Slot>CustomBorder.
    @param name The object name for the new border.
    @return The new GuiBorderProfile id, or 0 on failure.
*/
ConsoleMethodWithDocs(GuiProfileTheme, createBorder, ConsoleInt, 3, 3, (name))
{
    GuiBorderProfile* border = object->createBorder(argv[2]);
    return border != NULL ? border->getId() : 0;
}

/*! Removes a custom border created with createBorder.
    @param border The custom border to remove.
    @return True if the border was a custom border owned by this theme and was removed.
*/
ConsoleMethodWithDocs(GuiProfileTheme, removeBorder, ConsoleBool, 3, 3, (border))
{
    GuiBorderProfile* border = dynamic_cast<GuiBorderProfile*>(Sim::findObject(argv[2]));
    if (border == NULL)
    {
        Con::warnf("GuiProfileTheme::removeBorder() - could not find border '%s'.", argv[2]);
        return false;
    }

    return object->removeBorder(border);
}

/*! Gets the default member cursor for a cursor category.
    @param category The cursor category name (see getCursorCategoryNames).
    @return The GuiCursor id, or 0 if the category is unknown.
*/
ConsoleMethodWithDocs(GuiProfileTheme, getCursor, ConsoleInt, 3, 3, (category))
{
    GuiCursor* cursor = object->getCursor(StringTable->insert(argv[2]));
    return cursor != NULL ? cursor->getId() : 0;
}

/*! Gets all member cursors for a category: the default first, then extras.
    This is how a game picks between several cursors a theme offers for the same
    job - Canvas.setCursor(getWord(%theme.getCursors("Default"), 1)).
    @param category The cursor category name (see getCursorCategoryNames).
    @return A space-separated list of GuiCursor ids.
*/
ConsoleMethodWithDocs(GuiProfileTheme, getCursors, ConsoleString, 3, 3, (category))
{
    StringTableEntry category = StringTable->insert(argv[2]);

    char* buffer = Con::getReturnBuffer(1024);
    S32 offset = 0;
    buffer[0] = '\0';

    GuiCursor* cursor = object->getCursor(category);
    if (cursor != NULL)
        offset += dSprintf(buffer + offset, 1024 - offset, "%d", cursor->getId());

    const Vector<GuiCursor*>& extras = object->getExtraCursors();
    for (S32 i = 0; i < extras.size(); ++i)
    {
        if (extras[i]->mCategory != category)
            continue;
        offset += dSprintf(buffer + offset, 1024 - offset, "%s%d", offset > 0 ? " " : "", extras[i]->getId());
    }

    return buffer;
}

/*! Gets the engine-defined cursor category names. The suffixes these produce
    are also the canonical cursor names the engine falls back to when a control
    names no cursor of its own.
    @return A space-separated list of cursor category names.
*/
ConsoleMethodWithDocs(GuiProfileTheme, getCursorCategoryNames, ConsoleString, 2, 2, ())
{
    char* buffer = Con::getReturnBuffer(1024);
    S32 offset = 0;
    buffer[0] = '\0';

    for (S32 i = 0; i < GuiProfileTheme::getCursorCategoryCount(); ++i)
        offset += dSprintf(buffer + offset, 1024 - offset, "%s%s", i > 0 ? " " : "", GuiProfileTheme::getCursorCategoryName(i));

    return buffer;
}

/*! Gets the stock art file name a cursor category starts from
    ("defaultCursor.png"). The editor asks so it can seed a theme's own cursor
    folder; a theme never copies files itself.
    @param category The cursor category name (see getCursorCategoryNames).
    @return The stock file name, or "" if the category is unknown.
*/
ConsoleMethodWithDocs(GuiProfileTheme, getCursorStockFile, ConsoleString, 3, 3, (category))
{
    return GuiProfileTheme::getCursorStockFile(GuiProfileTheme::findCursorCategoryIndex(StringTable->insert(argv[2])));
}

/*! Gets the name the engine falls back to for a cursor category
    ("EditCursor"). Installing a theme's cursors means registering copies of
    them under these names; see AppCore::installThemeCursors.
    @param category The cursor category name (see getCursorCategoryNames).
    @return The canonical cursor name, or "" if the category is unknown.
*/
ConsoleMethodWithDocs(GuiProfileTheme, getCursorCanonicalName, ConsoleString, 3, 3, (category))
{
    return GuiProfileTheme::getCursorCanonicalName(GuiProfileTheme::findCursorCategoryIndex(StringTable->insert(argv[2])));
}

/*! Creates an extra member cursor in a category, alongside the default. A
    category holding more than one cursor is what makes the Gui Editor offer a
    choice on a control's cursor slot.
    @param category The cursor category name (see getCursorCategoryNames).
    @param name Optional object name; defaults to <ThemeName><Suffix><N>.
    @return The new GuiCursor id, or 0 on failure.
*/
ConsoleMethodWithDocs(GuiProfileTheme, createCursor, ConsoleInt, 3, 4, (category, [name]))
{
    GuiCursor* cursor = object->createCursor(argv[2], argc > 3 ? argv[3] : NULL);
    return cursor != NULL ? cursor->getId() : 0;
}

/*! Removes an extra member cursor. Default members are refused: a theme always
    provides one cursor per category.
    @param cursor The extra cursor to remove.
    @return True if the cursor was an extra and was removed.
*/
ConsoleMethodWithDocs(GuiProfileTheme, removeCursor, ConsoleBool, 3, 3, (cursor))
{
    GuiCursor* cursor = dynamic_cast<GuiCursor*>(Sim::findObject(argv[2]));
    if (cursor == NULL)
    {
        Con::warnf("GuiProfileTheme::removeCursor() - could not find cursor '%s'.", argv[2]);
        return false;
    }

    return object->removeCursor(cursor);
}

/*! Clears a member's override on one field, so the field re-derives from the
    theme. Accepts a member profile, border profile or cursor.
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

    GuiCursor* cursor = dynamic_cast<GuiCursor*>(target);
    if (cursor != NULL && cursor->getTheme() == object)
    {
        cursor->clearThemeFieldOverride(field);
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

    GuiCursor* cursor = dynamic_cast<GuiCursor*>(target);
    if (cursor != NULL && cursor->getTheme() == object)
    {
        cursor->clearAllThemeOverrides();
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

    GuiCursor* cursor = dynamic_cast<GuiCursor*>(target);
    if (cursor != NULL)
        return cursor->isThemeFieldOverridden(field);

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

/*! Renames the theme and every member following the theme-name pattern,
    updating overridden border references that point at this theme's borders.
    All-or-nothing: any name collision refuses the whole rename.
    @param newName The new theme name.
    @return True if the rename was applied.
*/
ConsoleMethodWithDocs(GuiProfileTheme, renameTheme, ConsoleBool, 3, 3, (newName))
{
    return object->renameTheme(argv[2]);
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
