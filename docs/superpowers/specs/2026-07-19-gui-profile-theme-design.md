# GuiProfileTheme — Design

Date: 2026-07-19
Branch: `Gui-Profile-Editor`
Status: approved (Peter, 2026-07-19)

## Context

Torque2D's GUI skinning rests on `GuiControlProfile`, originally designed for instant reskinning by profile swap. In practice profiles must be fine-tuned per control, many controls take several profiles (scroll = track/thumb/arrow; window = content + 3 buttons; menu = 4+ parts), and the `mCategory` field meant to express "what control is this profile for" is never read anywhere in the engine — it's a write-only persist field (`guiTypes.cc:437`, declared `guiTypes.h:253`). Missing profiles fall back by name convention to `GuiDefaultProfile` and, in release builds, null-deref (`guiControl.cc:1137-1192`, `AssertFatal` compiled out).

Meanwhile, real theming exists only in TorqueScript, three times over: editor `BaseTheme` (~67 profiles, 2,310 lines, `editor/EditorCore/Themes/BaseTheme/BaseTheme.cs`), `AppCore::createGuiProfiles` (`library/AppCore/gui/guiProfiles.cs`), and legacy Sandbox globals. All share one shape: a small palette + fonts + two helpers (`adjustValue` = HSV-brightness shift, `setAlpha`), deriving every profile's state colors (base = palette color, HL = adjustValue(+10), SL = accent, NA = setAlpha(~80–150)).

`GuiProfileTheme` formalizes that pattern as a C++ object — theme-wide values, auto-created profiles per category, per-field overrides, TAML persistence. It is the foundation for a future full-screen Profile Editor dialog in the GUI Editor; the theme is designed to be driven by that editor, not hand-authored.

## Decisions (Peter, 2026-07-19)

1. **Category list**: engine-defined static table in C++ (not script/data-driven).
2. **Propagation**: stamp-on-change — profile fields stay plain members; theme writes derived defaults into member profiles when theme values change, skipping overridden fields. No live fall-through reads, no rendering changes.
3. **Scope**: pure container — no changes to `GuiControl` or the profile-lookup/fallback chain. Lookup integration (active theme in `onWake` fallback) is a future branch.
4. **Theme value shape**: BaseTheme shape + destructive color — 3 semantic fonts + fontDirectory + fontSize, 6 semantic colors, borderSize.
5. **Architecture**: code-table theme — per-category C++ stamp functions porting the AppCore recipes. (Expression-driven derivation deliberately rejected for scope; can be layered later.)
6. **Category set**: full ~36-category table in v1, not a starter subset.

## Design

### Object model & ownership

- **`GuiProfileTheme : SimObject`** — new files `engine/source/gui/guiProfileTheme.h`, `.cc`, `guiProfileTheme_ScriptBinding.h`. `DECLARE_CONOBJECT`/`IMPLEMENT_CONOBJECT`, `initPersistFields()` calling Parent first, registers into `Sim::getGuiDataGroup()` in `onAdd` (mirror `guiTypes.cc:445`).
- **Theme owns members**: lists of member `GuiControlProfile*` and `GuiBorderProfile*`. `onAdd` creates one profile per category-table entry (skipping any already present from TAML read), `onRemove` deletes all members. Ownership flows one way.
- **Members point back**: `GuiControlProfile` and `GuiBorderProfile` gain non-owning `GuiProfileTheme* mTheme` (never serialized; reconstructed at load). `mTheme == NULL` ⇒ standalone profile, behavior identical to today. **No changes to `GuiControl`.**
- **Delete safety both ways**: theme `deleteNotify()`s each member and overrides `onDeleteNotify` to drop the entry. Deleted *default* ⇒ recreated at next stamp (a theme is always complete). Deleted *extra* ⇒ removed. Theme deleted ⇒ members are deleted by `onRemove`; a member that outlives has its `mTheme` nulled via the member's own `onDeleteNotify`.

### Category table (the engine-defined canon)

Static table in `guiProfileTheme.cc`: `{ categoryName, profileNameSuffix, stampFunc }`. Theme sets `mCategory` on every member — making the field load-bearing for the first time. Auto-created member names: **`<ThemeName><Suffix>`** (e.g. `DarkThemeButtonProfile`); extras get user names but keep the category (default extra name `<ThemeName><Suffix><N>`). Renaming a theme does not rename existing members (documented v1 limitation).

v1 categories (36) — union of profile slots engine controls consume (GUI Guide slot map + AppCore inventory):

> Default, Empty (transparent), Tooltip, Panel, Button, CheckBox, Radio, Label, TextEdit, Scroll, ScrollTrack, ScrollThumb, ScrollArrow, TabBook, Tab, TabPage, ListBox, DropDown, DropDownItem, Window, WindowContent, WindowButton, WindowCloseButton (destructive color), MenuBar, Menu, MenuItem, MenuContent, Overlay (menu/popup click-catcher), Progress, TreeView, FrameSet, FrameSetDropButton, ColorPicker, ColorSelector, ColorPopup, DragAndDrop

A parallel smaller **border table** works identically for theme-owned `GuiBorderProfile`s (Default, Bright, Dark, button-bevel sides, etc., from AppCore's border profiles). `GuiBorderProfile` gains a `category` persist field for symmetry (it has none today). Control-profile stamp functions wire borders: `borderDefault` by object pointer, the four side-border fields by **name** (they are lazily name-resolved strings already — `guiTypes.cc:455-613`).

Recipes: port from `library/AppCore/gui/guiProfiles.cs` (game-facing baseline), mapping its `color1..6` onto the semantic roles. Fixed bevel colors stay literal (`255 255 255 80` / `0 0 0 80` — deliberately hue-independent). Stamp functions set **all** persist fields of a profile (colors, fonts, alignment, geometry via borders, behavior flags); `bitmap`/`imageAsset` are left empty by stamps (user-overridable only).

### Theme-wide values (persist fields)

`fontBody`, `fontTitle`, `fontCode` (font type names), `fontDirectory`, `fontSize`;
`colorBackground`, `colorPanel`, `colorText`, `colorAccent`, `colorHighlight`, `colorWarning` (ColorI);
`borderSize` (S32).

(Amended 2026-07-19 with Peter: the originally-planned `colorTextSubtle` was replaced by `colorHighlight` — AppCore's palette has two accents (blue interaction + yellow flavor) and no subtle-text color, so the 6 roles now map 1:1 onto AppCore's six palette entries with no dead fields.)

Helpers as C++ statics on the class, exposed to script: `adjustValue(color, percent)` — HSV-value shift preserving hue/alpha, **fixing** BaseTheme's clamp bug (`mClamp(newValue, 0, 100)` should clamp the value fraction to 0..1, `BaseTheme.cs:2280-2309`) — and `setAlpha(color, alpha)`.

### Stamping & override tracking (core mechanism)

- `GuiProfileTheme::onStaticModified` (virtual on `SimObject`, `simObject.h:486`, fired from `setDataField` at `simObject.cc:558` — so script, editor inspector, and all three TAML readers hit it) ⇒ re-stamp all members synchronously (36 profiles × ~30 fields is trivial).
- **Stamp functions write raw member variables directly** (not `setDataField`) — so stamping never fires `onStaticModified`, needs no guard flag, and never marks overrides.
- Members override `onStaticModified`: when `mTheme != NULL`, any external field write adds the field name (`StringTableEntry`) to the member's override set; stamping skips fields in the set. Writes to `category` are ignored/not overridable (theme-managed).
- Override set: small `Vector<StringTableEntry>` (pointer-compare, linear scan) inside a shared helper struct (e.g. `GuiThemeMembership { GuiProfileTheme* theme; Vector<StringTableEntry> overrides; }`) embedded in both member classes; thin per-class glue, no multiple inheritance.
- `resetField`/`resetProfile` clear override entries and re-stamp.
- Note: `GuiControlProfile`'s constructor copy-from-`GuiDefaultProfile` (`guiTypes.cc:348-382`) runs before stamping and is simply overwritten — harmless, leave as-is.

### TAML persistence

- One theme file = theme values (ordinary persist fields) + all members as **TAML custom nodes** via `TamlCallbacks::onTamlCustomWrite/Read` (pattern: `engine/source/2d/assets/ParticleAsset.*`, also `SpriteBatch`, `guiFrameSetCtrl`; API in `engine/source/persistence/taml/tamlCustom.h`).
- Each member node writes: object name, category, and **only overridden fields** — enforced by overriding virtual `SimObject::writeField` (`simObject.h:673`; TAML consults it at `taml.cc:697,777`) to filter non-overridden fields when `mTheme != NULL` (always allow `name`/`category`). Standalone profile serialization unchanged.
- On read: members created from nodes and attached to the theme; fields applied via `setDataField` automatically re-mark themselves overridden (readers use `setPrefixedDataField` — `tamlXmlReader.cc:294`, `tamlBinaryReader.cc:241`, `tamlJSONReader.cc:258`). `onAdd` then creates any missing defaults and stamps everything.

### Script API (`guiProfileTheme_ScriptBinding.h`, ConsoleMethodWithDocs style)

`getProfile(category)`, `getProfiles(category)`, `createProfile(category [,name])`, `removeProfile(profile)` (extras only), `resetField(profile, field)`, `resetProfile(profile)`, `isFieldOverridden(profile, field)`, `restamp()`, `adjustValue(color, percent)`, `setAlpha(color, alpha)`; plus `getCategoryNames()` (static/console function) so the future editor enumerates the table.

### Adjacent fix (small, justified)

`GuiControlProfile` registers `deleteNotify` on its side-border profiles (`guiTypes.cc:479-613`) but never overrides `onDeleteNotify`, so deleting a border leaves dangling pointers (base is a no-op, `simObject.cc:904-906`). Since the theme now deletes/recreates borders routinely, this latent bug becomes live. Add `GuiControlProfile::onDeleteNotify` nulling the matching `mBorderDefault`/`mBorderLeft/Right/Top/Bottom` pointers.

## Out of scope (explicitly)

- The Profile Editor dialog (later task on this branch).
- Profile-lookup/fallback integration (`GuiControl::onWake` consulting an active theme) and the release-build null-profile crash fix — future branch.
- Migrating editor `BaseTheme`/`ThemeManager` or AppCore script themes to GuiProfileTheme.
- Theme-owned image assets / bitmap recipes; expression-driven derivation.

## Testing

GoogleTest in `engine/source/testing/tests/guiProfileThemeTests.cc`, run via `runAllUnitTests()`:

- theme creates one profile per category with `mCategory` set;
- theme-value change propagates to non-overridden member fields;
- an overridden field survives re-stamp; reset restores derivation;
- TAML round-trip preserves values + override sets and writes only overridden fields;
- delete-safety in both directions;
- `adjustValue` clamp correctness (including the fixed over-brighten rail).

Manual verification: editor boots and skins normally (standalone profiles unaffected); interactive console smoke (create theme, inspect members, edit theme color, override, reset, TAML round-trip, deletion behavior).
