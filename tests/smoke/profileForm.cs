// Profile-pane smoke test. Drives the Profile Editor's new custom profile pane
// (GuiProfileEditorProfileForm) that replaces the inspector when a profile node
// is selected: it verifies the pane toggle, the category-driven field filter in
// its four shapes, state greying, Show All, commits, per-field reset, and the
// standalone category picker.
// Run: tests/run.ps1 profileForm  ; grep PFSMOKE in console.log.

// Mode 1 rather than the usual 2: it opens, appends and closes the log on every
// write, so a crash mid-run still leaves every line that got as far as being
// echoed. Mode 2 holds the file open and loses the buffered tail.
setLogMode(1);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

function fCheck(%label, %cond)
{
	if(%cond) echo("PFSMOKE PASS: " @ %label);
	else      echo("PFSMOKE FAIL: " @ %label);
}

// Select a theme category node through the library's proxy, the same object the
// tree hands the dialog.
function fSelectCategory(%category)
{
	%d = GuiEditor.profileEditorDialog;
	%proxy = %d.library.categoryProxy[$fTheme.getId() @ "_" @ %category];
	%d.onTreeSelect(%proxy);
	return %proxy;
}

testExec("editor/main.cs");
schedule(2000, 0, "fStep1");

//-----------------------------------------------------------------------------
// The pane toggle and the spec table itself.
//-----------------------------------------------------------------------------

function fStep1()
{
	ProjectManager.setProjectFolder("profileFormSmokeProject");
	GuiEditor.open();
	GuiEditor.openProfileEditor();
	%d = GuiEditor.profileEditorDialog;
	fCheck("dialog opened", isObject(%d));
	fCheck("profile form built", isObject(%d.profileForm));

	$fTheme = %d.library.createTheme("PFSmoke");
	%d.tree.refresh();

	// --- The spec table must still cover every engine category. ---
	%spec = %d.profileForm.fieldSpec;
	fCheck("field spec exists", isObject(%spec));
	%missing = %spec.findMissingCategories($fTheme.getCategoryNames());
	fCheck("spec covers every engine category (missing: " @ %missing @ ")", %missing $= "");

	// --- Selecting a profile node shows the profile pane and nothing else. ---
	fSelectCategory("Button");
	fCheck("profile form scroller visible", %d.profileFormScroller.isVisible());
	fCheck("theme form hidden for profile", !%d.formScroller.isVisible());
	fCheck("border form hidden for profile", !%d.borderFormScroller.isVisible());
	fCheck("borders pane shown for profile", %d.bordersWindow.isVisible());
	fCheck("form bound to the profile", %d.profileForm.target == %d.currentMember);
	fCheck("name header shows the profile name",
		strstr(%d.profileForm.nameLabel.getText(), %d.currentMember.getName()) >= 0);

	// --- A border node takes the pane away again. ---
	%bname = getWord($fTheme.getBorderCategoryNames(), 0);
	%bproxy = new ScriptObject() { kind = "border"; theme = $fTheme; category = %bname; treeLabel = %bname; };
	%d.onTreeSelect(%bproxy);
	fCheck("profile form hidden for border node", !%d.profileFormScroller.isVisible());
	fCheck("border form shown for border node", %d.borderFormScroller.isVisible());

	schedule(400, 0, "fStep2");
}

//-----------------------------------------------------------------------------
// Filtering across the four text classes.
//-----------------------------------------------------------------------------

function fStep2()
{
	%d = GuiEditor.profileEditorDialog;
	%form = %d.profileForm;

	// --- "none": a scroll thumb draws no text at all. ---
	fSelectCategory("ScrollThumb");
	fCheck("none: fill row visible", %form.fillRow.isVisible());
	fCheck("none: text color row hidden", !%form.fontRow.isVisible());
	fCheck("none: font face row hidden", !%form.row["fontType"].isVisible());
	fCheck("none: font size row hidden", !%form.row["fontSize"].isVisible());
	fCheck("none: Text Layout section hidden", !%form.panel["TextLayout"].isVisible());
	fCheck("none: Rich Text section hidden", !%form.panel["RichText"].isVisible());
	fCheck("none: Interaction section hidden", !%form.panel["Interaction"].isVisible());
	fCheck("none: Image section still visible", %form.panel["Image"].isVisible());

	// --- "full" with a caret and a selection: the text edit. ---
	fSelectCategory("TextEdit");
	fCheck("full: font face row visible", %form.row["fontType"].isVisible());
	fCheck("full: text color row visible", %form.fontRow.isVisible());
	fCheck("full: Text Layout section visible", %form.panel["TextLayout"].isVisible());
	fCheck("full: align row visible", %form.row["align"].isVisible());
	fCheck("full: cursorColor visible", %form.row["cursorColor"].isVisible());
	fCheck("full: cursorColor named for the caret",
		%form.row["cursorColor"].labelText $= "Caret Color");
	fCheck("full: selection fill visible", %form.row["fillColorTextSL"].isVisible());
	fCheck("full: selection text visible", %form.row["fontColorTextSL"].isVisible());
	fCheck("full: focus fields visible", %form.row["canKeyFocus"].isVisible());

	// --- "glyph": a scroll arrow tints a triangle but rasterizes no glyphs. ---
	fSelectCategory("ScrollArrow");
	fCheck("glyph: text color row visible", %form.fontRow.isVisible());
	fCheck("glyph: font face row hidden", !%form.row["fontType"].isVisible());
	fCheck("glyph: font size row hidden", !%form.row["fontSize"].isVisible());
	fCheck("glyph: Rich Text section hidden", !%form.panel["RichText"].isVisible());

	// --- "direct": the slider draws its value without renderText. ---
	fSelectCategory("Slider");
	fCheck("direct: font face row visible", %form.row["fontType"].isVisible());
	fCheck("direct: align row hidden", !%form.row["align"].isVisible());
	fCheck("direct: textOffset row hidden", !%form.row["textOffset"].isVisible());

	// fontDirectory is not a per-profile field at all: the editor owns it and
	// points every profile at the project's one font folder, so the pane never
	// builds a row for it. What is worth checking is that something really does
	// set it -- a profile left without one falls back to $GUI::fontCacheDirectory,
	// which is the EDITOR's font folder while the editor is loaded.
	fCheck("direct: fontDirectory is not offered as a field", !isObject(%form.row["fontDirectory"]));
	fCheck("direct: the profile has a font directory anyway", %form.target.fontDirectory !$= "");

	schedule(400, 0, "fStep3");
}

//-----------------------------------------------------------------------------
// Per-category field overrides, state greying and Show All.
//-----------------------------------------------------------------------------

function fStep3()
{
	%d = GuiEditor.profileEditorDialog;
	%form = %d.profileForm;

	// --- align is a lie on a menu item (the control rewrites it every render). ---
	fSelectCategory("MenuItem");
	fCheck("MenuItem: align row hidden", !%form.row["align"].isVisible());
	fCheck("MenuItem: vAlign row still visible", %form.row["vAlign"].isVisible());

	// --- ...but a window button really does read it to pick its edge. ---
	fSelectCategory("WindowButton");
	fCheck("WindowButton: align row visible", %form.row["align"].isVisible());
	fCheck("WindowButton: font face row hidden (glyph)", !%form.row["fontType"].isVisible());

	// --- The circle hint, for the categories whose sides do nothing. ---
	fSelectCategory("Radio");
	fCheck("Radio: circle hint shown", %form.circleHint.isVisible());
	fSelectCategory("Button");
	fCheck("Button: circle hint hidden", !%form.circleHint.isVisible());

	// --- State greying: a scroll track renders normal and disabled only. ---
	fSelectCategory("ScrollTrack");
	fCheck("ScrollTrack: normal state active", %form.fillRow.swatch[0].isActive());
	fCheck("ScrollTrack: HL state greyed", !%form.fillRow.swatch[1].isActive());
	fCheck("ScrollTrack: SL state greyed", !%form.fillRow.swatch[2].isActive());
	fCheck("ScrollTrack: NA state active", %form.fillRow.swatch[3].isActive());

	// --- Show All lifts every filter, then puts it back. ---
	fSelectCategory("ScrollThumb");
	%form.showAllBox.setStateOn(true);
	%form.onShowAllToggled();
	fCheck("show all: font face row revealed", %form.row["fontType"].isVisible());
	fCheck("show all: text color row revealed", %form.fontRow.isVisible());
	fCheck("show all: Text Layout section revealed", %form.panel["TextLayout"].isVisible());
	fCheck("show all: Rich Text section revealed", %form.panel["RichText"].isVisible());
	fCheck("show all: every fill state active",
		%form.fillRow.swatch[1].isActive() && %form.fillRow.swatch[3].isActive());

	%form.showAllBox.setStateOn(false);
	%form.onShowAllToggled();
	fCheck("show all off: font face row hidden again", !%form.row["fontType"].isVisible());
	fCheck("show all off: Text Layout section hidden again", !%form.panel["TextLayout"].isVisible());

	schedule(400, 0, "fStep4");
}

//-----------------------------------------------------------------------------
// Commit, override marking and reset.
//-----------------------------------------------------------------------------

function fStep4()
{
	%d = GuiEditor.profileEditorDialog;
	%form = %d.profileForm;

	fSelectCategory("Button");
	%profile = %form.target;

	// --- A commit that changes nothing is not an edit. Text boxes commit on
	//     blur, so this is what tabbing through an untouched field does. ---
	%form.row["fontSize"].commit();
	fCheck("no-op blur left the field alone", !$fTheme.isFieldOverridden(%profile, "fontSize"));
	fCheck("no-op blur did not mark the row changed", !%form.row["fontSize"].resetButton.isVisible());
	%form.onProfileStateColorCommit(%form.fillRow, 0);
	fCheck("no-op swatch commit left the field alone", !$fTheme.isFieldOverridden(%profile, "fillColor"));
	fCheck("no-op swatch commit did not mark the row changed", !%form.fillRow.resetButton.isVisible());

	// A named color must survive the round trip too: the swatch reads "White"
	// back as "255 255 255 255", which a naive compare would call an edit.
	%profile.fillColorNA = "White";
	%form.refresh();
	%form.onProfileStateColorCommit(%form.fillRow, 3);
	fCheck("named color round trip is not an edit", %profile.fillColorNA $= "White");

	// --- A state-color commit writes the field and records the override. ---
	%form.fillRow.swatch[1].setColorI("12 34 56 255");
	%form.onProfileStateColorCommit(%form.fillRow, 1);
	fCheck("state color committed", %profile.fillColorHL $= "12 34 56 255");
	fCheck("theme marked dirty by state color edit", %d.library.isDirty());
	fCheck("state color override recorded", $fTheme.isFieldOverridden(%profile, "fillColorHL"));
	fCheck("state row reset button shown", %form.fillRow.resetButton.isVisible());

	// --- A plain row commit does the same. ---
	%oldSize = %profile.fontSize;
	%form.row["fontSize"].editor.setText(%oldSize + 5);
	%form.row["fontSize"].commit();
	fCheck("number field committed", %profile.fontSize == (%oldSize + 5));
	fCheck("number field override recorded", $fTheme.isFieldOverridden(%profile, "fontSize"));
	fCheck("number field reset button shown", %form.row["fontSize"].resetButton.isVisible());

	// --- A user color goes through the fontColors array, which getFieldValue
	//     cannot reach; check the pane's slot-access path round-trips. ---
	%form.showAllBox.setStateOn(true);
	%form.onShowAllToggled();
	%form.row["fontColors7"].editor.setColorI("9 8 7 255");
	%form.row["fontColors7"].commit();
	fCheck("user color written through the array", %profile.fontColors[7] $= "9 8 7 255");
	%form.showAllBox.setStateOn(false);
	%form.onShowAllToggled();

	// --- Reset puts a field back to the theme's stamped value. ---
	%form.onProfileRowReset(%form.row["fontSize"]);
	fCheck("field override cleared by reset", !$fTheme.isFieldOverridden(%profile, "fontSize"));
	fCheck("field value restamped by reset", %profile.fontSize == %oldSize);
	fCheck("field reset button hidden again", !%form.row["fontSize"].resetButton.isVisible());

	// --- Row reset clears the overridden states of that row only. ---
	%form.onProfileStateColorReset(%form.fillRow);
	fCheck("state override cleared by row reset", !$fTheme.isFieldOverridden(%profile, "fillColorHL"));
	fCheck("state row reset button hidden again", !%form.fillRow.resetButton.isVisible());

	schedule(400, 0, "fStep5");
}

//-----------------------------------------------------------------------------
// The standalone category picker.
//-----------------------------------------------------------------------------

function fStep5()
{
	%d = GuiEditor.profileEditorDialog;
	%form = %d.profileForm;

	%profile = %d.library.createStandalone("PFSmokeStandalone");
	fCheck("standalone profile created", isObject(%profile));
	%d.tree.refresh();

	%proxy = new ScriptObject() { kind = "standalone"; target = %profile; };
	%d.onTreeSelect(%proxy);
	fCheck("profile form shown for standalone", %d.profileFormScroller.isVisible());
	fCheck("category picker enabled for standalone", %form.categoryDrop.isActive());

	// An uncategorized profile shows everything rather than guessing.
	fCheck("standalone starts uncategorized", %profile.category $= "");
	fCheck("uncategorized: font face row visible", %form.row["fontType"].isVisible());

	// Choosing a purpose filters the pane and retags the profile.
	%index = %form.categoryDrop.findItemText("ScrollThumb", false);
	%form.categoryDrop.setSelected(%index);
	%form.onCategoryChanged();
	fCheck("category written to the profile", %profile.category $= "ScrollThumb");
	fCheck("category drove the filter", !%form.row["fontType"].isVisible());

	%index = %form.categoryDrop.findItemText("Button", false);
	%form.categoryDrop.setSelected(%index);
	%form.onCategoryChanged();
	fCheck("category change re-filters", %form.row["fontType"].isVisible());
	fCheck("standalone has no override markers", !%form.row["fontSize"].resetButton.isVisible());

	// A theme member's category is not the user's to change.
	fSelectCategory("Button");
	fCheck("category picker disabled for a theme member", !%form.categoryDrop.isActive());

	// Land on a border node before quitting. Quitting with a profile node
	// selected trips a teardown crash that predates this pane -- the preview's
	// sample controls still wear the theme's member profiles when the modules
	// unload and delete them (see the GuiControl profile-lifetime problem). It
	// reproduces identically on the pre-pane build, so leaving it here would only
	// make this harness's exit code useless for spotting a real regression.
	%bname = getWord($fTheme.getBorderCategoryNames(), 0);
	%d.onTreeSelect(new ScriptObject(){ kind = "border"; theme = $fTheme; category = %bname; treeLabel = %bname; });

	echo("PFSMOKE DONE");
	schedule(300, 0, "quit");
}
