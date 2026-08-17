// Border-pane persistence smoke test. Drives the Profile Editor's Borders pane
// through creating a custom border, editing a value, saving, and reloading from
// disk -- for a themed profile and for a standalone (bundled) profile.
// Run: tests/run.ps1 border  ; grep BSMOKE in tests/logs/.

setLogMode(2);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

function bCheck(%label, %cond)
{
	if(%cond) echo("BSMOKE PASS: " @ %label);
	else      echo("BSMOKE FAIL: " @ %label);
}

function bReadFile(%file)
{
	%fo = new FileObject();
	if(!%fo.openForRead(%file)) { %fo.delete(); return ""; }
	%text = "";
	while(!%fo.isEOF()) %text = %text @ %fo.readLine() @ " ";
	%fo.close();
	%fo.delete();
	return %text;
}

testExec("editor/main.cs");
schedule(2000, 0, "bStep1");

//--- Themed profile: custom side border, edit, save, verify file. -------------
function bStep1()
{
	ProjectManager.setProjectFolder("borderSmokeProject");
	GuiEditor.open();
	GuiEditor.openProfileEditor();
	%d = GuiEditor.profileEditorDialog;
	bCheck("dialog opened", isObject(%d));

	%theme = %d.library.createTheme("BSmoke");
	%theme.borderSize = 2;
	%d.tree.refresh();

	%proxy = %d.library.categoryProxy[%theme.getId() @ "_Button"];
	%d.onTreeSelect(%proxy);
	bCheck("borders pane shown for a profile", %d.bordersWindow.isVisible());

	%setter = %d.borderSetter["top"];
	bCheck("top setter bound", isObject(%setter));

	// The Button's top is already a themed border; switch it to Custom.
	%setter.onSelect("Custom...");
	%custom = BSmokeButtonProfiletopCustomBorder;
	bCheck("custom border created", isObject(%custom));
	bCheck("custom flag set", isObject(%custom) && %custom.isCustom);
	bCheck("profile top references custom", BSmokeButtonProfile.borderTop $= "BSmokeButtonProfiletopCustomBorder");
	bCheck("custom seeded from Bright (border=2)", isObject(%custom) && %custom.border == 2);

	// Bump the normal-state padding to 7 through the input box + commit. The
	// grid (not the setter) now owns the boxes and the commit.
	%box = %setter.grid.box["padding", 0];
	%box.setText("7");
	%setter.grid.commitBox(%box);
	bCheck("custom padding committed", isObject(%custom) && %custom.padding == 7);

	// Underfill now lives in the shared grid too; toggle it on and commit.
	%setter.grid.underfillBox.setStateOn(true);
	%setter.grid.commitUnderfill();
	bCheck("custom underfill committed", isObject(%custom) && %custom.underfill);

	%d.library.markDirty(%theme);
	%d.onSave();
	schedule(600, 0, "bStep2");
}

function bStep2()
{
	%file = pathConcat(getMainDotCsDir(), "borderSmokeProject", "themes", "BSmoke.taml");
	bCheck("theme file written", isFile(%file));
	%text = bReadFile(%file);
	bCheck("file carries the custom border", strstr(%text, "topCustomBorder") >= 0);
	bCheck("file carries isCustom flag", strstr(%text, "isCustom=\"true\"") >= 0);
	bCheck("file carries the edited padding", strstr(%text, "padding=\"7\"") >= 0);

	// Reload straight from disk with a fresh TAML read.
	%lib = GuiEditor.themeLibrary;
	%theme = BSmoke;
	%lib.removeProxiesFor(%theme);
	%theme.delete();

	// The dialog closed on save, so editorMode is back on (it shadow-names new
	// objects); turn it off around the read as the dialog itself does.
	editorMode(false);
	%reloaded = TAMLRead(%file);
	editorMode(true);
	bCheck("theme reloaded from disk", isObject(%reloaded) && %reloaded.getName() $= "BSmoke");
	%rc = BSmokeButtonProfiletopCustomBorder;
	bCheck("custom border reloaded", isObject(%rc));
	bCheck("reloaded custom flag", isObject(%rc) && %rc.isCustom);
	bCheck("reloaded padding preserved", isObject(%rc) && %rc.padding == 7);
	bCheck("reloaded profile still references custom", BSmokeButtonProfile.borderTop $= "BSmokeButtonProfiletopCustomBorder");
	if(isObject(%reloaded)) %reloaded.delete();

	schedule(400, 0, "bStep3");
}

// The tree leaf for a stand-alone profile, which is what carries the bundle on
// its .root. Keyed off the profile because the bundle is what we are looking for.
function bStandaloneProxy(%dialog, %profile)
{
	%folder = %dialog.library.standaloneFolder;
	for(%i = 0; %i < %folder.getCount(); %i++)
	{
		if(%folder.getObject(%i).target == %profile)
		{
			return %folder.getObject(%i);
		}
	}
	return 0;
}

// The same bundle, found through the library rather than the tree. Saving closes
// the Profile Editor and schedules the dialog's deletion, so anything checked
// after an onSave has to ask the library, which outlives it.
function bBundleFor(%profile)
{
	%lib = GuiEditor.themeLibrary;
	%group = %lib.themeGroup;
	for(%i = 0; %i < %group.getCount(); %i++)
	{
		%root = %group.getObject(%i);
		if(%lib.isBundle(%root) && %root.isMember(%profile))
		{
			return %root;
		}
	}
	return 0;
}

//--- Standalone profile: bundle wraps the profile + a custom default border. --
function bStep3()
{
	GuiEditor.openProfileEditor();
	%d = GuiEditor.profileEditorDialog;

	%profile = %d.library.createStandalone("BSolo");
	bCheck("standalone profile created", isObject(%profile));

	// The bundle is a SimSet, not a SimGroup, so it does NOT become the profile's
	// group -- a group would take the profile out of GuiDataGroup, which is the
	// only place the engine looks when it fills a control's Profile dropdown. That
	// is why the bundle has to be found through the tree proxy rather than through
	// %profile.getGroup(), which still answers GuiDataGroup.
	%d.tree.refresh();
	%proxy = bStandaloneProxy(%d, %profile.getId());
	bCheck("standalone proxy found", isObject(%proxy));
	%bundle = %proxy.root;
	bCheck("standalone wrapped in a bundle", %d.library.isBundle(%bundle));
	bCheck("bundle holds the profile", isObject(%bundle) && %bundle.isMember(%profile));
	bCheck("profile stays in the gui data group", %profile.getGroup() == GuiDataGroup.getId());

	%d.onTreeSelect(%proxy);
	bCheck("standalone root is the bundle", %d.currentRoot == %bundle.getId());

	// A standalone profile has no themed borders; give its default a custom one.
	%setter = %d.borderSetter["default"];
	%setter.onSelect("Custom...");
	%custom = BSolodefaultCustomBorder;
	bCheck("standalone custom default created", isObject(%custom));
	// Named by the bundle, but living in the gui data group, for the same reason.
	bCheck("bundle names the custom border", isObject(%custom) && %bundle.isMember(%custom));
	bCheck("custom border is in the gui data group", isObject(%custom) && %custom.getGroup() == GuiDataGroup.getId());
	bCheck("standalone default references custom", BSolo.borderDefault $= "BSolodefaultCustomBorder");

	%box = %setter.grid.box["margin", 0];
	%box.setText("5");
	%setter.grid.commitBox(%box);

	%d.library.markDirty(%bundle);
	%d.onSave();
	schedule(600, 0, "bStep4");
}

function bStep4()
{
	%file = pathConcat(getMainDotCsDir(), "borderSmokeProject", "themes", "BSolo.taml");
	bCheck("standalone bundle file written", isFile(%file));
	%text = bReadFile(%file);
	bCheck("bundle file is a SimSet", strstr(%text, "SimSet") >= 0);
	bCheck("bundle file carries the profile", strstr(%text, "BSolo") >= 0);
	bCheck("bundle file carries the custom default border", strstr(%text, "defaultCustomBorder") >= 0);
	bCheck("bundle file carries the edited margin", strstr(%text, "margin=\"5\"") >= 0);

	// Reload the bundle straight from disk. deleteRoot, not delete: a bundle is a
	// set and does not own what it holds, so dropping the set alone would leave
	// the profile and its custom border behind and the reload would collide with
	// the names they still hold.
	%lib = GuiEditor.themeLibrary;
	%bundle = bBundleFor(BSolo.getId());
	bCheck("bundle found for teardown", %lib.isBundle(%bundle));
	%lib.removeProxiesFor(%bundle);
	%lib.deleteRoot(%bundle);

	editorMode(false);
	%reloaded = TAMLRead(%file);
	editorMode(true);
	bCheck("standalone reloaded from disk", isObject(%reloaded) && isObject(BSolo));
	bCheck("reloaded standalone custom border", isObject(BSolodefaultCustomBorder));
	bCheck("reloaded default reference resolves", BSolo.borderDefault $= "BSolodefaultCustomBorder");
	if(isObject(%reloaded)) %reloaded.delete();

	// Clean up test files.
	fileDelete(pathConcat(getMainDotCsDir(), "borderSmokeProject", "themes", "BSmoke.taml"));
	fileDelete(%file);

	echo("BSMOKE DONE");
	schedule(300, 0, "quit");
}
