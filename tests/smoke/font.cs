// Font-workflow smoke test for the Gui Profile Editor. Covers the path a
// developer actually walks: the font drop-downs offer the fonts installed on this
// machine, the project keeps its caches in one predetermined folder nobody is
// asked about, and Save bakes a cache for every face/size that was rendered --
// including the sizes a control's fontSizeAdjust produced, which no field names.
// Run: tests/run.ps1 font  ; grep FSMOKE in console.log.

// Mode 1 rather than the usual 2: it opens, appends and closes the log on every
// write, so a crash mid-run still leaves every line that got as far as being
// echoed.
setLogMode(1);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

function fsCheck(%label, %cond)
{
	if(%cond) echo("FSMOKE PASS: " @ %label);
	else      echo("FSMOKE FAIL: " @ %label);
}

// A face this machine has installed that the project has no cache for, so a bake
// has something real to do. Walks the installed list rather than naming a font,
// which would tie the test to one machine.
function fsPickBakeableFace(%library)
{
	%installed = getInstalledFonts();
	%cached = %library.enumerateFonts(%library.getFontsPath());

	%count = getFieldCount(%installed);
	for(%i = 0; %i < %count; %i++)
	{
		%face = getField(%installed, %i);
		// Skip the symbol families: they have no printable Latin-1 to bake.
		if(%face $= "" || %face $= "Marlett" || %face $= "Symbol" || %face $= "Webdings" || %face $= "Wingdings")
		{
			continue;
		}
		if(!%library.tabListContains(%cached, %face))
		{
			return %face;
		}
	}
	return "";
}

testExec("editor/main.cs");
schedule(2000, 0, "fsStep1");

//-----------------------------------------------------------------------------
// The installed-font enumeration and the one font folder.
//-----------------------------------------------------------------------------

function fsStep1()
{
	ProjectManager.setProjectFolder("fontSmokeProject");
	GuiEditor.open();
	GuiEditor.openProfileEditor();
	%d = GuiEditor.profileEditorDialog;
	fsCheck("dialog opened", isObject(%d));

	$fsLib = %d.library;

	// --- The engine offers this machine's fonts. ---
	%installed = getInstalledFonts();
	$fsInstalledCount = getFieldCount(%installed);
	echo("FSMOKE installed face count =" SPC $fsInstalledCount);
	// A Windows box has dozens of families; a minimal Linux fontconfig install
	// (a bare WSL/CI container) can have only ~10 -- DejaVu, Liberation, Ubuntu.
	// The floor is here to catch enumeration collapsing to nothing or a single
	// fallback, not to demand a rich desktop, so keep it low enough to pass on
	// a headless box while still failing a backend that returns (almost) empty.
	fsCheck("getInstalledFonts returns a real list (" @ $fsInstalledCount @ ")", $fsInstalledCount >= 5);
	fsCheck("the list is sorted", stricmp(getField(%installed, 0), getField(%installed, $fsInstalledCount - 1)) < 0);
	fsCheck("no vertical-writing @ twins", strstr(%installed, "@") < 0);

	// --- One predetermined folder, under the project's themes. ---
	%fonts = $fsLib.getFontsPath();
	echo("FSMOKE fonts path =[" @ %fonts @ "]");
	fsCheck("fonts folder sits under the themes folder",
		%fonts $= pathConcat($fsLib.getThemesPath(), "fonts"));
	fsCheck("fonts folder is inside the project", strstr(%fonts, "fontSmokeProject") >= 0);

	// --- A new theme is pointed at it without being asked. ---
	$fsTheme = $fsLib.createTheme("FSmoke");
	fsCheck("theme created", isObject($fsTheme));
	fsCheck("new theme names the project font folder (" @ $fsTheme.fontDirectory @ ")",
		$fsTheme.fontDirectory $= $fsLib.getRelativeFontsPath());
	fsCheck("the stored path is relative, so the project stays portable",
		strstr($fsTheme.fontDirectory, ":") < 0);

	%d.tree.refresh();
	schedule(100, 0, "fsStep2");
}

//-----------------------------------------------------------------------------
// Both panes: the drop-downs are the machine's fonts, and neither asks for a
// directory any more.
//-----------------------------------------------------------------------------

function fsStep2()
{
	%d = GuiEditor.profileEditorDialog;

	// --- The theme pane. ---
	%d.onTreeSelect($fsLib.themeProxy[$fsTheme.getId()]);
	%form = %d.themeForm;
	fsCheck("theme form bound", isObject(%form) && %form.theme == $fsTheme);
	fsCheck("theme pane has no font directory row", !isObject(%form.fontDirBox));

	%titleCount = %form.fontTitleDrop.getItemCount();
	echo("FSMOKE theme drop-down item count =" SPC %titleCount);
	fsCheck("theme font drop-down offers the installed fonts (" @ %titleCount @ ")",
		%titleCount >= $fsInstalledCount);

	// --- The profile pane. ---
	%d.onTreeSelect($fsLib.categoryProxy[$fsTheme.getId() @ "_Button"]);
	%pane = %d.profileForm;
	fsCheck("profile pane bound", isObject(%pane) && isObject(%pane.target));
	fsCheck("profile pane has no font directory row", !isObject(%pane.row["fontDirectory"]));
	fsCheck("fontDirectory is not among the pane's fields",
		!%pane.fieldSpec.listHas(%pane.fieldSpec.allFields(), "fontDirectory"));

	%faceCount = %pane.row["fontType"].editor.getItemCount();
	echo("FSMOKE profile face drop-down item count =" SPC %faceCount);
	fsCheck("profile font drop-down offers the installed fonts (" @ %faceCount @ ")",
		%faceCount >= $fsInstalledCount);

	// --- A member profile inherits the theme's folder, not the editor's. ---
	fsCheck("member profile carries the project font folder (" @ %pane.target.fontDirectory @ ")",
		%pane.target.fontDirectory $= $fsLib.getRelativeFontsPath());

	schedule(100, 0, "fsStep3");
}

//-----------------------------------------------------------------------------
// Choosing a font. Nothing is baked yet, and a control that scales its font
// makes the engine ask for a size no field declares.
//-----------------------------------------------------------------------------

function fsStep3()
{
	%d = GuiEditor.profileEditorDialog;

	$fsFace = fsPickBakeableFace($fsLib);
	echo("FSMOKE chosen face =[" @ $fsFace @ "]");
	fsCheck("found an installed face with no cache yet", $fsFace !$= "");

	// Choose it the way the theme pane does -- which means going back to the theme
	// node first, since selecting the button unbound the theme form. All three
	// roles, so every member profile ends up on the face whichever one it uses (a
	// button takes the title font, not the body font).
	%d.onTreeSelect($fsLib.themeProxy[$fsTheme.getId()]);
	%d.themeForm.applyField("fontTitle", $fsFace);
	%d.themeForm.applyField("fontBody", $fsFace);
	%d.themeForm.applyField("fontCode", $fsFace);
	fsCheck("theme took the face", $fsTheme.fontBody $= $fsFace && $fsTheme.fontTitle $= $fsFace);

	$fsButton = getWord($fsTheme.getProfiles("Button"), 0);
	fsCheck("the face reached the button profile", $fsButton.fontType $= $fsFace);

	// --- Nothing is baked while editing. Asked of the filesystem: isFile answers
	// out of the resource manager, which knows about every font the editor has
	// rendered whether or not it was ever written. ---
	fsCheck("no cache written during the edit",
		!$fsLib.cacheFileExists($fsLib.getFontsPath(), $fsFace, $fsTheme.fontSize));

	// --- A size no field declares. A control's fontSizeAdjust multiplies its
	// profile's fontSize, so rendering one is the only way the engine comes to ask
	// for it -- and the only way anything can know to bake it. ---
	$fsAdjustedSize = mRound($fsButton.fontSize * 1.25);
	$fsSample = new GuiButtonCtrl()
	{
		profile = $fsButton;
		text = "Ag";
		extent = "80 30";
		position = "0 0";
		fontSizeAdjust = 1.25;
	};
	%d.preview.add($fsSample);

	// Let it render.
	schedule(300, 0, "fsStep4");
}

//-----------------------------------------------------------------------------
// Save: the recorded misses are baked into the project's font folder.
//-----------------------------------------------------------------------------

function fsStep4()
{
	%d = GuiEditor.profileEditorDialog;

	%rows = getUncachedFonts();
	echo("FSMOKE uncached rows =[" @ strreplace(%rows, "\n", " | ") @ "]");
	fsCheck("the engine recorded the fontSizeAdjust size " @ $fsAdjustedSize,
		strstr(%rows, $fsFace @ "\t" @ $fsAdjustedSize) >= 0);

	$fsSample.delete();
	%d.onSave();
	schedule(1000, 0, "fsStep5");
}

function fsStep5()
{
	%fonts = $fsLib.getFontsPath();
	echo("FSMOKE font folder now holds =[" @ strreplace(getFileList(%fonts), "\t", " | ") @ "]");

	fsCheck("Save baked the declared size (" @ $fsFace SPC $fsTheme.fontSize @ ")",
		$fsLib.cacheFileExists(%fonts, $fsFace, $fsTheme.fontSize));
	fsCheck("Save baked the fontSizeAdjust size (" @ $fsAdjustedSize @ ")",
		$fsLib.cacheFileExists(%fonts, $fsFace, $fsAdjustedSize));
	fsCheck("the recorded misses were consumed", getUncachedFonts() $= "");

	fsCheck("theme file written", isFile($fsLib.sourceFile[$fsTheme.getId()]));

	// The baked face is now discoverable from the folder, which is how a machine
	// without it installed still gets to choose it.
	fsCheck("the baked face is found by the folder scan",
		$fsLib.tabListContains($fsLib.enumerateFonts($fsLib.getFontsPath()), $fsFace));

	schedule(100, 0, "fsStep6");
}

//-----------------------------------------------------------------------------
// A theme authored against an older font folder is repointed.
//-----------------------------------------------------------------------------

function fsStep6()
{
	%theme = new GuiProfileTheme() { Name = "FSmokeLegacy"; };
	%theme.fontDirectory = "toybox/AppCore/1/fonts";

	$fsLib.applyFontsPath(%theme);
	fsCheck("a stale font folder is repaired", %theme.fontDirectory $= $fsLib.getRelativeFontsPath());
	fsCheck("its members followed",
		getWord(%theme.getProfiles("Button"), 0).fontDirectory $= $fsLib.getRelativeFontsPath());
	%theme.delete();

	schedule(100, 0, "fsStep7");
}

//-----------------------------------------------------------------------------
// The same thing for a theme actually on disk: toybox's, written when a theme
// carried whatever font folder the developer had picked.
//-----------------------------------------------------------------------------

function fsStep7()
{
	ProjectManager.setProjectFolder("toybox");
	$fsLib.scanThemes();

	// A theme carries its own name field rather than a Sim name, so it is found by
	// walking what the scan loaded.
	%theme = 0;
	for(%i = 0; %i < $fsLib.themeGroup.getCount(); %i++)
	{
		%root = $fsLib.themeGroup.getObject(%i);
		if(%root.getClassName() $= "GuiProfileTheme" && %root.getName() $= "BigCity")
		{
			%theme = %root;
			break;
		}
	}

	if(!isObject(%theme))
	{
		echo("FSMOKE SKIP: no toybox BigCity theme on disk to re-point");
		echo("FSMOKE done");
		quit();
		return;
	}

	fsCheck("a theme loaded from disk is re-pointed (" @ %theme.fontDirectory @ ")",
		%theme.fontDirectory $= $fsLib.getRelativeFontsPath());
	fsCheck("re-pointing does not dirty it (its file is only rewritten on Save)",
		!$fsLib.dirtySet.isMember(%theme));
	fsCheck("its members followed",
		getWord(%theme.getProfiles("Button"), 0).fontDirectory $= $fsLib.getRelativeFontsPath());

	echo("FSMOKE done");
	quit();
}
