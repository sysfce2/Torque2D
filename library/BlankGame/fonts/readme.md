# fonts

Bitmap fonts your game draws text with live here.

A font is a `.fnt` description plus the page images it refers to, and it becomes
an asset when a `.font.taml` names it:

```xml
<FontAsset
    AssetName="titleFont"
    FontFile="title.fnt"/>
```

A control then asks for it as `YourModule:titleFont` — where `YourModule` is the
`ModuleId` at the top of the `module.taml` next to this folder.

This folder is scanned **recursively**, so subfolders are fine.

The `.fnt` files themselves come from a bitmap font tool; BMFont's text format is
what the engine reads. The editor's font tools are under **Ctrl + ~**, and the
theme a project ships with keeps its own baked font caches separately, in
`themes/`, so nothing here is needed just to get text on screen.

This file is only here to keep the folder around in an empty project. Delete it
whenever you like.
