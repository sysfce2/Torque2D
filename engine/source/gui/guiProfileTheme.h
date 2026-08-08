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
#define _GUI_PROFILE_THEME_H_

#ifndef _COLOR_H_
#include "graphics/gColor.h"
#endif

#ifndef _SIM_OBJECT_H_
#include "sim/simObject.h"
#endif

#ifndef _VECTOR_H_
#include "collection/vector.h"
#endif

#ifndef _TAML_CHILDREN_H_
#include "persistence/taml/tamlChildren.h"
#endif

class GuiProfileTheme;

//-----------------------------------------------------------------------------
/// Embedded by GuiControlProfile and GuiBorderProfile to record membership in
/// a GuiProfileTheme: a non-owning back-pointer to the theme plus the set of
/// fields the user explicitly overrode away from the theme's stamped
/// defaults. A NULL theme means the object is standalone and behaves exactly
/// as it did before themes existed.
//-----------------------------------------------------------------------------
struct GuiThemeMembership
{
    GuiProfileTheme* mTheme;                ///< Owning theme; NULL for standalone objects.
    Vector<StringTableEntry> mOverrides;    ///< Fields explicitly overridden away from the theme's defaults.

    GuiThemeMembership() : mTheme(NULL) {}

    /// Parse a whitespace-separated field-name list into the override set
    /// (additive), and format the set back into a transient return buffer.
    /// Used by the members' "themeOverrides" persist field.
    void parseOverrideList(const char* list);
    const char* formatOverrideList() const;

    inline bool isOverridden(StringTableEntry field) const
    {
        for (S32 i = 0; i < mOverrides.size(); ++i)
        {
            if (mOverrides[i] == field)
                return true;
        }
        return false;
    }

    inline void markOverride(StringTableEntry field)
    {
        if (!isOverridden(field))
            mOverrides.push_back(field);
    }

    inline void clearOverride(StringTableEntry field)
    {
        for (S32 i = 0; i < mOverrides.size(); ++i)
        {
            if (mOverrides[i] == field)
            {
                mOverrides.erase(i);
                return;
            }
        }
    }

    inline void clearAll() { mOverrides.clear(); }
};

class GuiControlProfile;
class GuiBorderProfile;
class GuiCursor;

//-----------------------------------------------------------------------------
/// A set of theme-wide values (fonts, palette colors, border size) from which
/// a complete family of GuiControlProfile / GuiBorderProfile objects is
/// derived. Formalizes in C++ the theme pattern used by the script-side
/// editor themes and AppCore profile helpers.
///
/// On registration the theme creates one member profile per entry of the
/// engine-defined category table (and one border per border category), named
/// <ThemeName><Suffix>, and stamps every profile field with values derived
/// from the theme. Changing a theme value restamps all members, skipping any
/// field a member has explicitly overridden. The theme owns its members and
/// deletes them on removal; a deleted default member is recreated at the next
/// restamp, so a theme is always complete.
//-----------------------------------------------------------------------------
class GuiProfileTheme : public SimObject, public TamlChildren
{
private:
    typedef SimObject Parent;

public:
    typedef void (*StampProfileFn)(GuiProfileTheme* theme, GuiControlProfile* profile);
    typedef void (*StampBorderFn)(GuiProfileTheme* theme, GuiBorderProfile* border);
    typedef void (*StampCursorFn)(GuiProfileTheme* theme, GuiCursor* cursor);

    /// One entry of the engine-defined category table: the mCategory value,
    /// the member-name suffix, and the recipe deriving the member's fields
    /// from the theme's values.
    struct ProfileCategory
    {
        const char* name;
        const char* suffix;
        StampProfileFn stamp;
    };

    struct BorderCategory
    {
        const char* name;
        const char* suffix;
        StampBorderFn stamp;
    };

    /// One entry of the cursor table. Unlike a profile or a border, a cursor is
    /// a bitmap, which no recipe can derive from a palette - so the table also
    /// carries the stock art a new member starts from and the placement values
    /// that art wants. Those three are written once when the member is created
    /// and never stamped again; the recipe derives only the tint.
    ///
    /// The suffixes deliberately match the canonical cursor names the engine
    /// looks up when a control names none ("EditCursor", "LeftRightCursor" and
    /// the rest), so installing a theme's cursors under those names is a matter
    /// of dropping the theme's own name from the front.
    struct CursorCategory
    {
        const char* name;
        const char* suffix;
        const char* stockFile;      ///< Art seeded into the theme's cursor folder.
        S32 hotSpotX;
        S32 hotSpotY;
        F32 renderOffsetX;          ///< A fraction of the bitmap's own size, so it
        F32 renderOffsetY;          ///< anchors the same way whatever the art measures.
        StampCursorFn stamp;
    };

private:
    // Theme-wide values, the inputs to every category recipe.
    StringTableEntry mFontBody;         ///< Font for body text; the most common font.
    StringTableEntry mFontTitle;        ///< Font for titles and headers.
    StringTableEntry mFontCode;         ///< Monospace font for code and console text.
    StringTableEntry mFontDirectory;    ///< Directory searched for the fonts.
    StringTableEntry mCursorDirectory;  ///< Directory holding this theme's own cursor art.
    S32 mFontSize;                      ///< Base font size; recipes may offset it.
    ColorI mColorBackground;            ///< Deepest background color.
    ColorI mColorSurface;               ///< Raised surface/control background color.
    ColorI mColorForeground;            ///< Foreground/content text color.
    ColorI mColorAccent;                ///< Primary accent color used during interaction.
    ColorI mColorHighlight;             ///< Secondary accent for selected/active flavor.
    ColorI mColorWarning;               ///< Destructive-action color (e.g. window close).
    S32 mBorderSize;                    ///< Base border width; recipes may scale it.

    // Members. Defaults parallel the category tables; extras are additional
    // user-created profiles that share a category with a default.
    Vector<GuiControlProfile*> mDefaultProfiles;
    Vector<GuiControlProfile*> mExtraProfiles;
    Vector<GuiBorderProfile*> mDefaultBorders;
    Vector<GuiBorderProfile*> mExtraBorders;    ///< User-authored single-use "custom" borders owned by the theme (not category members).
    Vector<GuiCursor*> mDefaultCursors;
    Vector<GuiCursor*> mExtraCursors;           ///< Extra cursors, each one within a category (the profile pattern, not the border one).

    static const ProfileCategory smProfileCategories[];
    static const BorderCategory smBorderCategories[];
    static const CursorCategory smCursorCategories[];
    static const S32 smProfileCategoryCount;
    static const S32 smBorderCategoryCount;
    static const S32 smCursorCategoryCount;

    bool mTamlReading;      ///< Suppresses auto-creation/stamping while Taml populates the theme.

    GuiControlProfile* createMemberProfile(S32 categoryIndex, const char* objectName);
    GuiBorderProfile* createMemberBorder(S32 categoryIndex);
    GuiCursor* createMemberCursor(S32 categoryIndex, const char* objectName);

    /// Point a cursor at this theme's copy of its category's stock art. Only
    /// ever fills an EMPTY bitmapName: art is the user's, and a restamp must
    /// never overwrite what they chose. Does nothing until the theme knows a
    /// cursor directory, which is why it is retried on every restamp.
    void fillCursorArt(GuiCursor* cursor, S32 categoryIndex);

public:
    DECLARE_CONOBJECT(GuiProfileTheme);
    GuiProfileTheme();
    static void initPersistFields();
    bool onAdd();
    void onRemove();
    virtual void onStaticModified(const char* slotName, const char* newValue = NULL);
    virtual void onDeleteNotify(SimObject* object);

    // Taml persistence. Members serialize as ordinary child objects (each
    // writing only name, category, and overridden fields); reads suppress
    // auto-creation until the file's members are attached, then restamp.
    virtual U32 getTamlChildCount(void) const;
    virtual SimObject* getTamlChild(const U32 childIndex) const;
    virtual void addTamlChild(SimObject* pSimObject);
    virtual void onTamlPreRead(void);
    virtual void onTamlPostRead(const TamlCustomNodes& customNodes);

    // The engine-defined category tables.
    static S32 getCategoryCount() { return smProfileCategoryCount; }
    static StringTableEntry getCategoryName(S32 index);
    static S32 findCategoryIndex(StringTableEntry categoryName);
    static S32 getBorderCategoryCount() { return smBorderCategoryCount; }
    static StringTableEntry getBorderCategoryName(S32 index);
    static S32 findBorderCategoryIndex(StringTableEntry categoryName);
    static S32 getCursorCategoryCount() { return smCursorCategoryCount; }
    static StringTableEntry getCursorCategoryName(S32 index);
    static S32 findCursorCategoryIndex(StringTableEntry categoryName);

    /// The stock art file name for a cursor category ("defaultCursor.png"). The
    /// editor asks so it can seed a theme's cursor folder; the theme itself
    /// never copies files.
    static const char* getCursorStockFile(S32 index);

    /// The name the engine falls back to for a cursor category ("EditCursor").
    /// Also the member-name suffix, which is why a member is findable both ways.
    static const char* getCursorCanonicalName(S32 index);

    // Members.
    S32 getProfileCount() const;
    inline const Vector<GuiControlProfile*>& getExtraProfiles() const { return mExtraProfiles; }
    inline const Vector<GuiBorderProfile*>& getExtraBorders() const { return mExtraBorders; }
    inline const Vector<GuiCursor*>& getExtraCursors() const { return mExtraCursors; }
    GuiControlProfile* getProfile(StringTableEntry categoryName) const;   ///< The category's default member.
    GuiBorderProfile* getBorder(StringTableEntry categoryName) const;     ///< The border category's default member.
    GuiCursor* getCursor(StringTableEntry categoryName) const;            ///< The cursor category's default member.
    GuiControlProfile* createProfile(const char* categoryName, const char* objectName);  ///< Create an extra profile in a category.
    bool removeProfile(GuiControlProfile* profile);                       ///< Delete an extra; defaults are refused.
    GuiBorderProfile* createBorder(const char* objectName);               ///< Create a single-use custom border owned by the theme.
    bool removeBorder(GuiBorderProfile* border);                          ///< Delete a custom border.
    GuiCursor* createCursor(const char* categoryName, const char* objectName);  ///< Create an extra cursor in a category.
    bool removeCursor(GuiCursor* cursor);                                 ///< Delete an extra; defaults are refused.

    /// Create any missing default members and re-derive every non-overridden
    /// field of every member from the current theme values.
    void restamp();

    /// Rename the theme and every member following the <ThemeName><Suffix>
    /// pattern (extras rename only when their name starts with the old theme
    /// name), updating overridden border-name references that point at this
    /// theme's borders. All-or-nothing: any target-name collision refuses the
    /// whole rename and returns false.
    bool renameTheme(const char* newName);

    // Theme value access for category recipes.
    inline StringTableEntry getFontBody() const { return mFontBody; }
    inline StringTableEntry getFontTitle() const { return mFontTitle; }
    inline StringTableEntry getFontCode() const { return mFontCode; }
    inline StringTableEntry getFontDirectory() const { return mFontDirectory; }
    inline StringTableEntry getCursorDirectory() const { return mCursorDirectory; }
    inline S32 getFontSize() const { return mFontSize; }
    inline const ColorI& getColorBackground() const { return mColorBackground; }
    inline const ColorI& getColorSurface() const { return mColorSurface; }
    inline const ColorI& getColorForeground() const { return mColorForeground; }
    inline const ColorI& getColorAccent() const { return mColorAccent; }
    inline const ColorI& getColorHighlight() const { return mColorHighlight; }
    inline const ColorI& getColorWarning() const { return mColorWarning; }
    inline S32 getBorderSize() const { return mBorderSize; }

    /// Shift a color's HSV value (brightness) by percent (positive brightens,
    /// negative darkens), preserving hue and alpha. The value fraction is
    /// clamped to 0..1, so over-brightening saturates without hue wash-out.
    /// Black, having no hue, brightens along the neutral gray rail.
    static ColorI adjustValue(const ColorI& color, F32 percent);

    /// Replace a color's alpha byte, clamping to 0..255.
    static ColorI setAlpha(const ColorI& color, S32 alpha);
};

#endif // _GUI_PROFILE_THEME_H_
