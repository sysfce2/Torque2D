# Gui Control Icon Set — Design

Date: 2026-07-29
Branch: `gui-editor-improvements`
Status: approved (Peter, 2026-07-29)

## Context

The Gui Editor's control palette (`editor/GuiEditor/scripts/GuiEditorControlListWindow.cs`) is a plain-text `GuiListBoxCtrl` fed by `enumerateConsoleClasses("GuiControl")` minus the plumbing — `GuiTextEditSliderCtrl` above `GuiTreeViewCtrl` and below `GuiTextEditCtrl`, telling a person nothing about what any of them looks like. The intent is a visual palette: a grid of 128px tiles, or rows of 64px tiles with names, user's choice.

This spec covers **the art and its pipeline**. The palette rewrite that consumes it is a separate follow-on, so the art can be reviewed on its own before UI work depends on it.

The set is modelled on the purchased **Mono Icon Set** (`C:\Users\Peter\Documents\Circuit Hive\T2D Rocket Edition\Mono Icon Set`), which is what every editor icon already uses. New art lives in a sibling folder so the bought art stays untouched — the separation `Mono Icon Set/derived/` already keeps.

## Decisions (Peter, 2026-07-29)

1. **Two palette modes** — a grid of 128px tiles (icon only), or rows of 64px tiles with names. In grid mode the icon carries the whole load.
2. **Drawing language: miniature of the control**, not metaphor. A slider looks like a slider. Metaphor only where a control has no appearance (`GuiInputCtrl`).
3. **Scope: palette only (128 + 64)**, but every drawing constrained so the Explorer tree could adopt it later without a redraw.
4. **`GuiControl` gets four tiles**, one per profile category — Empty, Panel, Label, Overlay — so the commonest control in the editor drops pre-categorised instead of guessed at.
5. **Output lands in `editor/GuiEditor/`**, the only consumer.
6. **A generated entry table**, not `$ControlIcon::` constants, because an entry key is not always a class name.
7. **A fallback icon, not a build break**, for a class with no icon defined.

## Design

### The drawing language

Measured off the Mono art rather than guessed at (`app_window_icon&48.png` studied at 4×):

- Solid white, negative space knocked **out** of the fill rather than stroked. A frame is a filled rect with a smaller rect removed.
- Ink ramp `255` at the top of the tile falling linearly to `218` at the bottom — the same ramp `derive_valign.py` fits by least squares (≈ −0.84/row at 48px). Held as endpoints so it is size-independent, and applied from one function so 31 icons cannot drift apart tonally.
- Corner radius **6 units**, one constant, for the same reason.
- Art **bleeds to the tile edge**; Mono's padding is near zero. Breathing room is the tile control's job.
- **White only.** `EditorIconButton` (`editor/EditorCore/EditorIconButton.cs`) tints its `GuiSpriteCtrl` with `ImageColor` from the theme font colour and fades that on hover, press and disable. The art is a tint mask; the ramp survives underneath as shading.

### The unit grid

Every icon is placed in a **64-unit square**. Primary structure — frames, bars, dividers, major splits — snaps to multiples of **4 units**, which is a whole pixel at 16, 32, 64 and 128 alike. Secondary detail may use multiples of 2.

The honest limit: the rule keeps structure *aligned* at 16px, it does not promise *legibility* there. `GuiTreeViewCtrl`, `GuiMenuBarCtrl`, `GuiFrameSetCtrl` and `SceneWindow` carry detail that will merge at 16 and would want simplified variants if the tree ever adopts the set.

### Rendering

Each size is drawn at **8× supersample** and downsampled with Lanczos. Corners get real antialiasing; straight edges cost nothing, because primary geometry lands on exact pixel boundaries at every size.

### The 31 icons

Frame index is display order in an 8×4 sheet — 31 used, 1 spare. Grouped by what a person is looking for, not alphabetically.

| # | key | icon |
|---|---|---|
| 0 | `unknown` | Rounded frame with a **?** — the fallback |
| 1 | `GuiControl:Empty` | Four corner brackets — bounds, nothing painted |
| 2 | `GuiControl:Panel` | Border band, inset cut, filled space inside |
| 3 | `GuiControl:Label` | Two unframed text bars, ragged right |
| 4 | `GuiControl:Overlay` | Diagonal-hatched sheet with a dialog floating on it |
| 5 | `GuiButtonCtrl` | Rounded rect, centred label bar knocked out |
| 6 | `GuiCheckBoxCtrl` | Box with a check, label bars right |
| 7 | `GuiRadioCtrl` | Ring with a dot, label bars right |
| 8 | `GuiDropDownCtrl` | Field, divider, ▼ chevron |
| 9 | `GuiColorPopupCtrl` | 3×2 swatch block + the same chevron |
| 10 | `GuiImageButtonCtrl` | Button with a small picture **on** it |
| 11 | `GuiTextEditCtrl` | Field, text bar, I-beam caret |
| 12 | `GuiTextEditSliderCtrl` | The same field with ▲▼ spinner |
| 13 | `GuiSliderCtrl` | Thin track, thumb, tick marks |
| 14 | `GuiProgressCtrl` | Pill filled from the left |
| 15 | `GuiColorPickerCtrl` | Blend field with a selector ring, banded hue strip |
| 16 | `GuiSpriteCtrl` | Closed frame, peak, sun — a photo |
| 17 | `SceneWindow` | Heavier frame, ground line, ball and crate — a game view |
| 18 | `GuiListBoxCtrl` | Framed rows, one inverted |
| 19 | `GuiTreeViewCtrl` | Spine, elbows, disclosure triangle |
| 20 | `GuiMenuBarCtrl` | Title bar with a menu dropped open |
| 21 | `GuiChainCtrl` | Three equal unframed bars, equal gaps |
| 22 | `GuiGridCtrl` | 3×3 cells with gutters |
| 23 | `GuiScrollCtrl` | Content with a track and thumb |
| 24 | `GuiFrameSetCtrl` | Panes split by thick dividers |
| 25 | `GuiPanelCtrl` | Captioned header, chevron right, body |
| 26 | `GuiExpandCtrl` | Bare header, centred double chevron, body with content |
| 27 | `GuiTabBookCtrl` | Three tabs, first merged into the page |
| 28 | `GuiTabPageCtrl` | One tab and its page, content drawn |
| 29 | `GuiWindowCtrl` | Title bar with three buttons, body |
| 30 | `GuiInputCtrl` | Keycap with a pointer over it — the one metaphor |

### The eight pairs that must stay apart

Each is a review checkpoint, and `review.py pairs` renders them side by side:

| pair | differentiator |
|---|---|
| Sprite / ImageButton | button margin around the picture |
| Sprite / SceneWindow | photo furniture vs. physics furniture |
| Empty / SceneWindow | open brackets vs. closed frame |
| ColorPicker / ColorPopup | chevron only on the popup |
| Panel / Expand | caption present or absent |
| TabBook / TabPage | three tabs or one |
| Slider / Progress | thin track vs. pill |
| Label / Chain | ragged lengths vs. equal blocks |

### Files

Authoring, in `T2D Rocket Edition/Control Icon Set/` (not a git repo — it is where the Mono set lives):

| | |
|---|---|
| `icons.py` | primitives, the 64-unit space, the renderer, the ink ramp, all 31 drawings, and `ICONS` — the ordered palette table |
| `build_control_icon_sheets.py` | renders, asserts, packs, and generates |
| `review.py` | contact sheets: `all`, `probe`, `pairs` |

**Run with `py`, not `python`** — 3.11 has PIL 12.3.0, the 3.10 first on PATH does not.

Generated into the repo:

```
editor/GuiEditor/images/controlIcons128.png        8×4, 1024×512
editor/GuiEditor/images/controlIcons128.asset.taml
editor/GuiEditor/images/controlIcons64.png         8×4, 512×256
editor/GuiEditor/images/controlIcons64.asset.taml
editor/GuiEditor/scripts/GuiEditorControlIcons.cs
```

8×4 rather than 6×6: 31 icons need more than 30 cells, and 8×4 lands both sheets on power-of-two dimensions. Both are well inside the 2048px cap that silently renders oversized textures black. The two sizes are **source resolution**, not tile size — `sheetFor()` picks the 128 sheet above 96px and the 64 sheet below.

### The lookup

`GuiEditorControlIcons` is a generated `ScriptObject` holding every entry in display order — `key`, `ctrlClass`, `category`, `label`, `frame` — with `keys()`, `frameFor()`, `classFor()`, `categoryFor()`, `labelFor()`, `isKnown()` and `sheetFor()`.

The array is `ctrlClass`, not `class`: `class` is a live SimObject field, and `GuiEditorControlSpec` already documents dodging exactly that collision.

### Drift: a fallback, not a build break

Trading a runtime enumeration for a build-time list means a control class added to the engine could silently never reach the palette. That is handled by degrading, not by failing:

1. **Frame 0 is the fallback**, deliberately not last. `frameFor` returns 0 for any unknown key, and an unset `Frame` field reads as 0 too — so both failures land on a legible "unknown control" rather than an arbitrary wrong picture.
2. **The palette keeps its runtime enumeration as a tail.** It walks the curated table for order, then appends anything `enumerateConsoleClasses("GuiControl")` yields that the table does not name, filtered by the rule `populate` uses today. A new class still appears and still drags; it wears the `?` until someone draws it. *(This half lands in the follow-on palette spec; the contract — curated ordered head plus a zero fallback — is established here.)*
3. **The build script prints, without failing**, both directions: placeable classes with no icon, and icons naming a class the engine no longer registers.

Determining "placeable" needs the real ancestry, not a name prefix. Two traps found writing it:

- `GuiControl` registers with **`IMPLEMENT_CONOBJECT_CHILDREN`**, not `IMPLEMENT_CONOBJECT`. Matching only the plain macro loses the one class the palette needs most.
- `GuiCursor`, `GuiControlProfile`, `GuiBorderProfile` and `GuiProfileTheme` are all registered and all named `Gui…`, but derive from `SimObject` and never appear in the palette. The script walks first-base ancestry to `GuiControl` — which is what `enumerateConsoleClasses("GuiControl")` actually answers.

### Module wiring — two silent failures

- **`editor/GuiEditor/module.taml` had no `<DeclaredAssets>` block and no `images` folder** — the one editor module shipping no assets. Without the block the PNGs sit on disk and never register, with no error anywhere. Added, matching `EditorCore/module.taml`; note the extension is `asset.taml`, not `image.taml`. Asset ids are `GuiEditor:controlIcons64` / `…128`.
- **`GuiEditor::create` execs every script by name.** Added the exec, plus the `ScriptObject` beside `themeLibrary` and its `delete()` in `GuiEditor::destroy`, per the ownership chain in `TORQUE_SCRIPT.md`.

## Verification

**Build-script assertions**, run every build:

- every icon renders at both sizes, exactly `size × size`
- opaque bbox reaches at least two tile edges — catches an icon that silently drew small (this caught `GuiGridCtrl` stopping 2 units short of the right edge)
- **every opaque pixel carries the exact ramp value for its row.** Sampling the tile's top and bottom rows does not work: most icons are inset and touch neither. The first attempt asserted on the sheet's row 0 and failed against correct art, because no icon in the first sheet row happens to reach y=0.
- frame 0 is the fallback; no duplicate keys; 31 entries in 32 cells

**`review.py`** writes contact sheets on a dark ground — the whole set at both sizes, the style probe at 2×/4×, and the eight pairs side by side.

**`tests/shots/controlIcons.cs`** renders both sheets in-engine at the sizes the palette will draw them, tinted the way `EditorIconButton` tints, labelled by key, asking `GuiEditor.controlIcons` for the list so the page cannot drift from the sheet. Its real job is proving the assets load: a missing image renders as nothing rather than throwing, so the `DeclaredAssets` failure mode is a page of labels over blank space — which nothing else would catch.

Full smoke suite re-run after the wiring changes.

## Out of scope

- The palette rewrite — grid/rows toggle, tile control, `populate` walking the table then appending the enumerated tail.
- 16/24/32 sheets and the Explorer tree adopting them. The 4-unit grid rule keeps that a rerun.

## Open item for the follow-on

The Control List window is **250px wide** by default (`GuiEditor.cs`, `Extent = "250 380"`). A true 128px grid gets one column at that width. The palette spec will need to widen the default extent, draw the 128 art at ~96px for two columns, or both.
