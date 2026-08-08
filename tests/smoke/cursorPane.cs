// Cursor-pane smoke test. Drives the Gui Profile Editor's cursor support: the
// Cursors folder in the tree, the pane that replaces the other three when a
// cursor node is selected, the seeded per-theme art, the hot-spot magnifier and
// its drag arithmetic, extras within a category, and the theme rename that has
// to take the art folder with it.
// Run: tests/run.ps1 cursorPane  ; grep CURSMOKE in tests/logs/.

setLogMode(2);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

function cCheck(%label, %cond)
{
	if(%cond) echo("CURSMOKE PASS: " @ %label);
	else      echo("CURSMOKE FAIL: " @ %label);
}

testExec("editor/main.cs");
schedule(2000, 0, "cStep1");

function cStep1()
{
	ProjectManager.setProjectFolder("cursorPaneSmokeProject");
	GuiEditor.open();
	GuiEditor.openProfileEditor();
	%d = GuiEditor.profileEditorDialog;
	cCheck("dialog opened", isObject(%d));

	%theme = %d.library.createTheme("CurSmoke");
	%d.tree.refresh();
	$curSmokeTheme = %theme;

	// --- The theme provides a cursor per category, with art of its own. ---
	%categories = %theme.getCursorCategoryNames();
	cCheck("theme reports seven cursor categories", getWordCount(%categories) == 7);

	%cursor = %theme.getCursor("Default");
	cCheck("default cursor member exists", isObject(%cursor));
	cCheck("member is named for the theme", %cursor.getName() $= "CurSmokeDefaultCursor");
	cCheck("member carries its category", %cursor.category $= "Default");
	cCheck("art was seeded into the theme's own folder",
		strstr(%cursor.bitmapName, "cursors/CurSmoke") >= 0);
	cCheck("the art file is really there",
		isFile(makeFullPath(%cursor.bitmapName, getMainDotCsDir())));
	cCheck("the tint came from the palette", %cursor.color $= %theme.colorForeground);

	// --- Selecting a cursor node brings the cursor pane, and only that. ---
	%proxy = %d.library.cursorCategoryProxy[%theme.getId() @ "_Default"];
	cCheck("cursor category proxy built", isObject(%proxy));
	%d.onTreeSelect(%proxy);

	cCheck("cursor form shown", %d.cursorFormScroller.isVisible());
	cCheck("profile form hidden for a cursor", !%d.profileFormScroller.isVisible());
	cCheck("border form hidden for a cursor", !%d.borderFormScroller.isVisible());
	cCheck("theme form hidden for a cursor", !%d.formScroller.isVisible());
	cCheck("borders pane hidden for a cursor", !%d.bordersWindow.isVisible());
	cCheck("current member is the cursor", %d.currentMember == %cursor.getId());
	cCheck("pane bound to the cursor", %d.cursorForm.target == %cursor.getId());
	cCheck("magnifier bound to the cursor", %d.cursorForm.editor.cursor $= %cursor.getName());

	// The two color buttons are buttons, not bars: a swatch spanning the whole
	// row reads as a progress bar. Both are the same size as each other, which
	// is what says they do the same kind of job.
	cCheck("the tint swatch is button-shaped",
		getWord(%d.cursorForm.row["color"].editor.getExtent(), 0) == $CursorForm::SwatchWidth);
	cCheck("the marker swatch matches it",
		getWord(%d.cursorForm.dotSwatch.getExtent(), 0) == $CursorForm::SwatchWidth);
	// ...while a profile's state-color rows still fill their cell, where four
	// swatches share the width and that is how you tell them apart.
	%d.onTreeSelect(%d.library.categoryProxy[%theme.getId() @ "_Button"]);
	cCheck("profile color rows still fill their cell",
		getWord(%d.profileForm.fillRow.swatch[0].getExtent(), 0) > 40);
	%d.onTreeSelect(%proxy);

	schedule(400, 0, "cStep2");
}

function cStep2()
{
	%d = GuiEditor.profileEditorDialog;
	%theme = $curSmokeTheme;
	%cursor = %theme.getCursor("Default");
	%editor = %d.cursorForm.editor;

	// --- The magnifier read the art, so it knows its real size. ---
	%extent = %editor.getImageExtent();
	cCheck("magnifier measured the art", getWord(%extent, 0) == 13 && getWord(%extent, 1) == 17);

	// --- The try-it range lays out against the padded content, not the box. ---
	// It wears a profile from the theme being edited, and a theme may give its
	// panels any padding it likes, so nothing here may be positioned by number.
	// These two hold whatever the padding is: the hint fills the content width,
	// and the target sits in the middle of that same width.
	%range = %d.preview.stage.getObject(0);
	cCheck("the try-it range is on the stage", isObject(%range));
	%hint = %range.getObject(0);
	%target = %range.getObject(1);

	%hintWidth = getWord(%hint.getExtent(), 0);
	cCheck("the hint filled the content width",
		%hintWidth > 0 && %hintWidth <= getWord(%range.getExtent(), 0));
	cCheck("the hint starts at the content's left edge", getWord(%hint.getPosition(), 0) == 0);
	cCheck("the target is centred in that same width",
		getWord(%target.getPosition(), 0) == ((%hintWidth - getWord(%target.getExtent(), 0)) / 2));

	// --- The zoom never claims a magnification it is not drawing. ---
	// A stock-sized cursor must be able to reach the full 16x: the pane is sized
	// for it. Anything less means the view shrank and the ceiling came with it.
	%max = %editor.getMaxZoom();
	cCheck("a 13x17 cursor reaches the full 16x", %max == 16);
	%editor.setZoom(16);
	cCheck("asking past the ceiling reports the ceiling, not the wish",
		%editor.getZoom() == %max);
	%d.cursorForm.refreshReadout();
	cCheck("the zoom label agrees with what is drawn",
		%d.cursorForm.zoomLabel.getText() $= (%max @ "x"));
	cCheck("zoom in is greyed at the ceiling", !%d.cursorForm.zoomIn.isActive());
	cCheck("zoom out is live at the ceiling", %d.cursorForm.zoomOut.isActive());

	%editor.setZoom(1);
	%d.cursorForm.refreshReadout();
	cCheck("zoom out is greyed at 1x", !%d.cursorForm.zoomOut.isActive());
	cCheck("zoom in is live at 1x", %d.cursorForm.zoomIn.isActive());
	%editor.setZoom(8);

	// The stock Default cursor: hot spot 1,1 with no anchor, so the pointer
	// lands on pixel 1,1.
	cCheck("effective hot spot combines both fields", %editor.getEffectiveHotSpot() $= "1 1");

	// --- Anchoring is a fraction of the art, and the nudge absorbs it. ---
	// Compared numerically: a Point2F reads back in the console's float format,
	// not as the string it was written with.
	%d.cursorForm.onAnchorPreset(0.5, 0.5);
	cCheck("anchor preset wrote renderOffset",
		getWord(%cursor.renderOffset, 0) == 0.5 && getWord(%cursor.renderOffset, 1) == 0.5);
	// 13 * 0.5 truncates to 6, 17 * 0.5 to 8, and the nudge is still 1,1.
	cCheck("anchor moved where the cursor points", %editor.getEffectiveHotSpot() $= "7 9");

	%d.cursorForm.onAnchorPreset(0, 0);
	cCheck("anchor cleared again", %editor.getEffectiveHotSpot() $= "1 1");

	// The readout is a function of both placement fields, so a typed edit to
	// either has to move it. It used to report the pixel the dot had left,
	// which reads as the magnifier disagreeing with its own numbers.
	%d.cursorForm.row["hotSpot"].applyValue("5 6");
	%d.cursorForm.onProfileRowCommit(%d.cursorForm.row["hotSpot"]);
	cCheck("a typed nudge moved where the cursor points", %editor.getEffectiveHotSpot() $= "5 6");
	cCheck("the readout followed the typed nudge",
		strstr(%d.cursorForm.readout.getText(), "5, 6") >= 0);

	%d.cursorForm.row["hotSpot"].applyValue("1 1");
	%d.cursorForm.onProfileRowCommit(%d.cursorForm.row["hotSpot"]);

	// --- A tint edit is an override; the art is not. ---
	// applyValue rather than setValue: setValue also records the new value as
	// the baseline, so the commit that follows would see nothing changed -- the
	// guard that stops a text box committing a field the user only tabbed past.
	%d.cursorForm.row["color"].applyValue("10 20 30 255");
	%d.cursorForm.onProfileRowCommit(%d.cursorForm.row["color"]);
	cCheck("tint committed to the cursor",
		getWord(%cursor.color, 0) == 10 && getWord(%cursor.color, 2) == 30);
	cCheck("tint counts as a theme override", %theme.isFieldOverridden(%cursor, "color"));
	cCheck("hot spot is never an override", !%theme.isFieldOverridden(%cursor, "hotSpot"));
	cCheck("art is never an override", !%theme.isFieldOverridden(%cursor, "bitmapName"));
	cCheck("editing marked the theme dirty", %d.library.isDirty());

	// A restamp must not undo the art, only re-derive the tint.
	%theme.resetField(%cursor, "color");
	cCheck("resetting the tint re-derives it", %cursor.color $= %theme.colorForeground);
	cCheck("the art survived the restamp", strstr(%cursor.bitmapName, "cursors/CurSmoke") >= 0);

	schedule(400, 0, "cStep3");
}

function cStep3()
{
	%d = GuiEditor.profileEditorDialog;
	%theme = $curSmokeTheme;

	// --- An extra in a category: what makes the Gui Editor offer a choice. ---
	%extra = %d.library.createExtraCursor(%theme, "Default");
	cCheck("extra cursor created", isObject(%extra));
	cCheck("extra sits in the same category", %extra.category $= "Default");
	cCheck("category now offers two cursors", getWordCount(%theme.getCursors("Default")) == 2);
	cCheck("extra got art of its own",
		%extra.bitmapName !$= %theme.getCursor("Default").bitmapName);
	cCheck("the extra's art file exists",
		isFile(makeFullPath(%extra.bitmapName, getMainDotCsDir())));

	%leaf = %d.library.cursorExtraProxy[%extra.getId()];
	cCheck("extra has a tree leaf", isObject(%leaf));
	%d.onTreeSelect(%leaf);
	cCheck("cursor pane binds an extra too", %d.cursorForm.target == %extra.getId());

	// The two shared toolbar buttons describe what they are about to act on.
	cCheck("remove tip names a cursor while one is selected",
		%d.removeExtraTip() $= "Remove Extra Cursor");
	%d.onTreeSelect(%d.library.cursorCategoryProxy[%theme.getId() @ "_Default"]);
	cCheck("new tip names a cursor in a cursor category",
		%d.newInCategoryTip() $= "New Cursor in Category");
	%d.onTreeSelect(%d.library.categoryProxy[%theme.getId() @ "_Button"]);
	cCheck("new tip still names a profile in a profile category",
		%d.newInCategoryTip() $= "New Profile in Category");
	%d.onTreeSelect(%leaf);

	// --- Renaming the theme takes the members and the art folder with it. ---
	cCheck("theme renamed", %d.library.renameThemeTo(%theme, "CurSmokeTwo"));
	cCheck("members followed the rename",
		%theme.getCursor("Default").getName() $= "CurSmokeTwoDefaultCursor");
	cCheck("art folder followed the rename",
		strstr(%theme.getCursor("Default").bitmapName, "cursors/CurSmokeTwo") >= 0);
	cCheck("the moved art file exists",
		isFile(makeFullPath(%theme.getCursor("Default").bitmapName, getMainDotCsDir())));

	// An extra's art is named after the member rather than the category, so a
	// rename that moved only the stock files left it behind while its cursor
	// pointed hopefully into the new folder.
	cCheck("an extra's art followed the rename too",
		strstr(%extra.bitmapName, "cursors/CurSmokeTwo") >= 0);
	cCheck("and the extra's file is really there",
		isFile(makeFullPath(%extra.bitmapName, getMainDotCsDir())));

	// --- Removing the extra puts the category back to one, and offers to take
	// its picture with it rather than doing so behind the user's back. ---
	%art = %extra.bitmapName;
	%artFile = makeFullPath(%art, getMainDotCsDir());
	%orphaned = %d.library.removeExtraCursor(%theme, %extra);
	cCheck("extra removed", getWordCount(%theme.getCursors("Default")) == 1);
	cCheck("its now-unused art is offered up, not deleted", %orphaned $= %artFile);
	cCheck("the file is still there until someone says otherwise", isFile(%artFile));

	// Saying yes only dooms it -- Cancel would still keep it, like every other
	// file this editor removes.
	%d.doomedCursorArt = %orphaned;
	%d.doDeleteCursorArt();
	cCheck("confirming dooms the file", %d.library.isDirty());

	// --- Art that is still in use is never offered. This is the case that
	// would really have hurt: an extra pointed at the category's stock art,
	// which the default member is also using. ---
	%shared = %d.library.createExtraCursor(%theme, "Edit");
	%shared.bitmapName = %theme.getCursor("Edit").bitmapName;
	%sharedFile = makeFullPath(%shared.bitmapName, getMainDotCsDir());
	%orphaned = %d.library.removeExtraCursor(%theme, %shared);
	cCheck("shared art is not offered for deletion", %orphaned $= "");
	cCheck("and the file the default still uses survives", isFile(%sharedFile));

	// --- Nor is art the user chose from somewhere else. ---
	%outside = %d.library.createExtraCursor(%theme, "Move");
	%outside.bitmapName = "editor/EditorCore/Themes/BaseTheme/images/cursors/move.png";
	cCheck("art from outside the theme's folder is left alone",
		%d.library.removeExtraCursor(%theme, %outside) $= "");

	cCheck("a default cursor cannot be removed",
		!%theme.removeCursor(%theme.getCursor("Default")));

	echo("CURSMOKE DONE");
	schedule(300, 0, "quit");
}
