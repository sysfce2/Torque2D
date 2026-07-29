//-----------------------------------------------------------------------------
// The Gui Editor's control palette: the generated entry table it is built from,
// its two view modes, and the two things a tile can do.
//
// Driven through script rather than posted input on purpose. A tile's screen
// position depends on the scroll offset, which group is collapsed and how many
// columns the grid decided on, and none of that is exposed to script -- so a
// coordinate-based test would be asserting arithmetic it cannot check. The
// input-driven suites are also the flaky ones.
//-----------------------------------------------------------------------------

setLogMode(2);
setScriptExecEcho(false);
trace(false);
$Scripts::ignoreDSOs = true;
setCompanyAndProduct("Torque Game Engines", "Torque2D");
ModuleDatabase.EchoInfo = false;
AssetDatabase.EchoInfo = false;
AssetDatabase.IgnoreAutoUnload = true;

testExec("editor/main.cs");

$Pass = 0;
$Fail = 0;

function palCheck(%label, %condition)
{
	if(%condition)
	{
		$Pass++;
		echo("PAL PASS: " @ %label);
	}
	else
	{
		$Fail++;
		echo("PAL FAIL: " @ %label);
	}
}

schedule(2000, 0, "palSetup");

// A project, so there is a theme. Every control the editor places is themed on
// arrival, and the four faces of a bare GuiControl are told apart by the profile
// they end up wearing -- with no theme loaded, onControlDropped skips theming
// altogether and there is nothing to check.
function palSetup()
{
	ProjectManager.setProjectFolder("PlanetX");
	ModuleDatabase.ScanModules("PlanetX");
	ModuleDatabase.LoadExplicit("AppCore", 1);

	GuiEditor.open();
	palCheck("the editor adopted a theme", GuiEditor.themeName !$= "");

	schedule(500, 0, "palTableChecks");
}

//-----------------------------------------------------------------------------
// The generated table. Checked before anything is built on it, because every
// failure here shows up much later and much less legibly -- a group whose keys
// come back empty renders as a collapsible section with nothing in it.
//-----------------------------------------------------------------------------

function palTableChecks()
{
	%icons = GuiEditor.controlIcons;
	palCheck("the icon table exists", isObject(%icons));

	%groups = %icons.groups();
	palCheck("four groups (" @ getFieldCount(%groups) @ ")", getFieldCount(%groups) == 4);

	// The one that could quietly fail: "Input & Data" carries a space and an
	// ampersand, and it is used as a dynamic-field subscript. If TorqueScript
	// cannot hold that, keysInGroup answers "" and the group renders empty.
	%awkward = getField(%groups, 2);
	palCheck("third group is named \"" @ %awkward @ "\"", %awkward $= "Input & Data");
	palCheck("a group name with a space and an ampersand still keys its entries (" @
		getFieldCount(%icons.keysInGroup(%awkward)) @ ")",
		getFieldCount(%icons.keysInGroup(%awkward)) == 7);

	// Every entry lands in exactly one group, and the groups account for all of
	// them -- no entry silently dropped, none listed twice.
	%all = %icons.keys();
	%total = getFieldCount(%all);
	palCheck("30 entries outside the fallback (" @ %total @ ")", %total == 30);

	%sum = 0;
	for(%g = 0; %g < getFieldCount(%groups); %g++)
	{
		%group = getField(%groups, %g);
		%keys = %icons.keysInGroup(%group);
		%count = getFieldCount(%keys);
		palCheck("group \"" @ %group @ "\" has entries (" @ %count @ ")", %count > 0);
		%sum += %count;

		for(%i = 0; %i < %count; %i++)
		{
			%key = getField(%keys, %i);
			palCheck(%key @ " reports the group it was listed under",
				%icons.groupFor(%key) $= %group);
		}
	}
	palCheck("the groups account for every entry (" @ %sum @ " of " @ %total @ ")", %sum == %total);

	// The fallback is reachable but never offered.
	palCheck("the fallback is not in any group", %icons.groupFor("unknown") $= "");
	palCheck("the fallback is not in the key list", strstr(%all, "unknown") == -1);
	palCheck("an unknown key resolves to frame 0", %icons.frameFor("NoSuchCtrl") == 0);
	palCheck("an unknown key is not known", !%icons.isKnown("NoSuchCtrl"));
	palCheck("a real key is known", %icons.isKnown("GuiButtonCtrl"));

	// The four faces of a bare GuiControl: same class, different category.
	%faces = "Empty" TAB "Panel" TAB "Label" TAB "Overlay";
	for(%i = 0; %i < 4; %i++)
	{
		%face = getField(%faces, %i);
		%key = "GuiControl:" @ %face;
		palCheck(%key @ " builds a GuiControl", %icons.classFor(%key) $= "GuiControl");
		palCheck(%key @ " carries category " @ %face, %icons.categoryFor(%key) $= %face);
	}

	// Anything else pins its own category, so it asks for none.
	palCheck("an ordinary class asks for no category", %icons.categoryFor("GuiButtonCtrl") $= "");
	palCheck("a class key builds its own class", %icons.classFor("GuiSliderCtrl") $= "GuiSliderCtrl");

	// The sheet threshold is the small sheet's own resolution: above 64 the big
	// sheet is shrunk rather than the small one enlarged.
	palCheck("64px art takes the 64 sheet", %icons.sheetFor(64) $= "GuiEditor:controlIcons64");
	palCheck("80px art takes the 128 sheet", %icons.sheetFor(80) $= "GuiEditor:controlIcons128");
	palCheck("32px art takes the 64 sheet", %icons.sheetFor(32) $= "GuiEditor:controlIcons64");

	palPaletteChecks();
}

//-----------------------------------------------------------------------------
// The palette the window actually built.
//-----------------------------------------------------------------------------

function palPaletteChecks()
{
	%window = GuiEditor.ctrlListWindow;
	%icons = GuiEditor.controlIcons;

	palCheck("the mode row was built", isObject(%window.modeRow));
	palCheck("the mode row offers two views", %window.modeRow.choiceCount == 2);
	palCheck("it starts in grid mode", %window.mode $= "grid");

	// A stock engine should have an icon for everything the palette can place,
	// so the sweep finds nothing and there is no fifth group. When that is not
	// true, say which class -- "5 groups" on its own sends the next person
	// hunting.
	%undrawn = "";
	%classes = enumerateConsoleClasses("GuiControl");
	for(%i = 0; %i < getFieldCount(%classes); %i++)
	{
		%name = trim(getField(%classes, %i));
		if(%icons.isPlaceableClass(%name) && !%icons.coversClass(%name) &&
			strstr(%undrawn, %name) == -1)
		{
			%undrawn = %undrawn SPC %name;
		}
	}
	palCheck("every placeable class has an icon (" @ trim(%undrawn) @ ")", %undrawn $= "");
	palCheck("four groups were built (" @ %window.groupCount @ ")", %window.groupCount == 4);

	// Every group open, with its tiles in the inner grid rather than on the
	// panel -- GuiExpandCtrl rewrites mVisible on direct children only.
	%tiles = 0;
	for(%g = 0; %g < %window.groupCount; %g++)
	{
		%group = %window.group[%g];
		palCheck("group " @ %g @ " is open", %group.getExpanded());
		palCheck("group " @ %g @ " has tiles", %group.tileCount > 0);
		palCheck("group " @ %g @ " taller than its header (" @
			getWord(%group.getExtent(), 1) @ ")",
			getWord(%group.getExtent(), 1) > $GuiEditorControlGroup::headerHeight);
		%tiles += %group.tileCount;
	}
	palCheck("30 tiles across the groups (" @ %tiles @ ")", %tiles == 30);

	palModeChecks();
}

//-----------------------------------------------------------------------------
// Switching views, and surviving a collapse.
//-----------------------------------------------------------------------------

function palModeChecks()
{
	%window = GuiEditor.ctrlListWindow;
	%group = %window.group[0];
	%tile = %group.tile[0];

	palCheck("grid mode hides the caption", !%tile.caption.isVisible());
	palCheck("grid mode draws 80px art (" @ getWord(%tile.icon.getExtent(), 0) @ ")",
		getWord(%tile.icon.getExtent(), 0) == $GuiEditorControlTile::gridArt);
	palCheck("grid mode takes the 128 sheet", %tile.icon.Image $= "GuiEditor:controlIcons128");

	%window.setMode("rows");
	palCheck("row mode shows the caption", %tile.caption.isVisible());
	palCheck("row mode names the control, not the class",
		%tile.caption.getText() $= GuiEditor.controlIcons.labelFor(%tile.key));
	palCheck("the tooltip still names the class",
		%tile.tooltip $= GuiEditor.controlIcons.classFor(%tile.key));
	palCheck("row mode draws 32px art (" @ getWord(%tile.icon.getExtent(), 0) @ ")",
		getWord(%tile.icon.getExtent(), 0) == $GuiEditorControlTile::rowArt);
	palCheck("row mode takes the 64 sheet", %tile.icon.Image $= "GuiEditor:controlIcons64");
	palCheck("row mode is one tile per row",
		%group.grid.CellSizeX >= getWord(%group.getExtent(), 0));

	%window.setMode("grid");
	palCheck("switching back restores the grid cell",
		%group.grid.CellSizeX == $GuiEditorControlGroup::gridCell);
	palCheck("switching back hides the caption again", !%tile.caption.isVisible());

	// Collapsing force-writes mVisible on the panel's direct children. The tiles
	// are grandchildren, so they must come back.
	%group.setExpanded(false);
	%window.relayout();
	%group.setExpanded(true);
	%window.relayout();
	palCheck("a tile survives its group being collapsed", %tile.isVisible());
	palCheck("the group reopened", %group.getExpanded());

	palDropChecks();
}

//-----------------------------------------------------------------------------
// The two gestures. A click is a drop that never moved, and it has to reach the
// document by the same path a drag does -- that is where theming, selection and
// undo recording live.
//-----------------------------------------------------------------------------

function palDropChecks()
{
	%window = GuiEditor.ctrlListWindow;
	%root = GuiEditor.rootGui;
	%before = %root.getCount();

	%tile = palFindTile("GuiButtonCtrl");
	palCheck("found the button tile", isObject(%tile));
	%tile.onClick();

	palCheck("clicking a tile added a control (" @ %root.getCount() @ ")",
		%root.getCount() == %before + 1);
	%added = %root.getObject(%root.getCount() - 1);
	palCheck("it built the class the tile names", %added.getClassName() $= "GuiButtonCtrl");

	// A drag ends with a mouse-up over the tile, which fires onClick as well --
	// a button keeps mDepressed through a drag. One gesture, one control.
	%count = %root.getCount();
	%tile.dragged = true;
	%tile.onClick();
	palCheck("a click that ended a drag adds nothing", %root.getCount() == %count);
	palCheck("and the drag flag is cleared afterwards", !%tile.dragged);

	// The four faces of a bare GuiControl: the palette says which, so the
	// applier must not fall back to guessing.
	%faces = "Empty" TAB "Panel" TAB "Label" TAB "Overlay";
	for(%i = 0; %i < 4; %i++)
	{
		%face = getField(%faces, %i);
		%tile = palFindTile("GuiControl:" @ %face);
		palCheck("found the " @ %face @ " tile", isObject(%tile));

		%tile.onClick();
		%ctrl = %root.getObject(%root.getCount() - 1);
		palCheck(%face @ " dropped a bare GuiControl", %ctrl.getClassName() $= "GuiControl");
		palCheck(%face @ " wears a " @ %face @ " profile (" @ %ctrl.Profile.category @ ")",
			%ctrl.Profile.category $= %face);
		palCheck(%face @ " consumed its request", %ctrl.paletteCategory $= "");
	}

	echo("PAL DONE  " @ $Pass @ " passed, " @ $Fail @ " failed");
	quit();
}

function palFindTile(%key)
{
	%window = GuiEditor.ctrlListWindow;
	for(%g = 0; %g < %window.groupCount; %g++)
	{
		%group = %window.group[%g];
		for(%i = 0; %i < %group.tileCount; %i++)
		{
			if(%group.tile[%i].key $= %key)
			{
				return %group.tile[%i];
			}
		}
	}
	return 0;
}
