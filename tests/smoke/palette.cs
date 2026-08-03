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

	// Every entry is either in a group or refused -- none silently dropped. A
	// refused entry keeps its row, and so its frame and its label, because the
	// frame IS the row index; it just isn't offered. GuiTabPageCtrl is the one
	// that is: only a tab book makes a page, from the + tab it draws in the
	// editor.
	%refused = 0;
	for(%i = 0; %i < %total; %i++)
	{
		%key = getField(%all, %i);
		if(!%icons.isPlaceableClass(%icons.classFor(%key)))
		{
			%refused++;
		}
	}
	palCheck("the table refuses one entry (" @ %refused @ ")", %refused == 1);
	palCheck("Tab Page is the refused one", !%icons.isPlaceableClass("GuiTabPageCtrl"));
	palCheck("a refused entry keeps its icon", %icons.isKnown("GuiTabPageCtrl"));
	palCheck("a refused entry still counts as covered", %icons.coversClass("GuiTabPageCtrl"));
	palCheck("every entry is either grouped or refused (" @ %sum @ " + " @ %refused @
		" of " @ %total @ ")", (%sum + %refused) == %total);

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

	// Each threshold is that sheet's own resolution: past it the next sheet up is
	// shrunk rather than this one enlarged. Both boundaries are checked from
	// either side, because an off-by-one here reads as "the icons went soft"
	// rather than as a failure.
	palCheck("64px art takes the 64 sheet", %icons.sheetFor(64) $= "GuiEditor:controlIcons64");
	palCheck("80px art takes the 128 sheet", %icons.sheetFor(80) $= "GuiEditor:controlIcons128");
	palCheck("32px art takes the 64 sheet", %icons.sheetFor(32) $= "GuiEditor:controlIcons64");
	palCheck("17px art takes the 64 sheet", %icons.sheetFor(17) $= "GuiEditor:controlIcons64");
	palCheck("16px art takes the 16 sheet", %icons.sheetFor(16) $= "GuiEditor:controlIcons16");

	// And every sheet it can name has to exist. A missing image renders as
	// nothing rather than throwing, so an unregistered asset would show up as
	// blank space in the tree and nowhere else at all.
	for(%i = 0; %i < 3; %i++)
	{
		%size = getWord("16 64 128", %i);
		%sheet = %icons.sheetFor(%size);
		%asset = AssetDatabase.acquireAsset(%sheet);
		palCheck(%sheet @ " is a declared asset", isObject(%asset));
		if(isObject(%asset))
		{
			palCheck(%sheet @ " carries all 32 cells", %asset.getFrameCount() == 32);
			AssetDatabase.releaseAsset(%sheet);
		}
	}

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
	// One fewer than the 30 table entries: Tab Page is refused, so it has a row
	// and an icon but no tile.
	palCheck("29 tiles across the groups (" @ %tiles @ ")", %tiles == 29);
	palCheck("no Tab Page tile", palFindTile("GuiTabPageCtrl") == 0);

	palWidthChecks();
	palResizeChecks();
	palModeChecks();
}

//-----------------------------------------------------------------------------
// Dragging the frame.
//
// The static case passing means nothing on its own: a width handed out once is
// right once. The palette was fixed in script that way and it held only until
// the frame was next dragged, because "width" sizing adds the parent's CHANGE to
// a child's own width and never re-reads anything.
//
// So sweep the window across a range of widths, the way a person dragging its
// edge does, and check the invariant at every stop. Nothing calls relayout here
// on purpose -- dragging a frame does not, and if the layout only survives a
// nudge from script then it has not survived.
//-----------------------------------------------------------------------------

function palResizeChecks()
{
	%window = GuiEditor.ctrlListWindow;
	%bar = %window.scroller.scrollBarThickness;
	%pos = %window.getPosition();
	%was = %window.getExtent();
	%height = getWord(%was, 1);

	%worst = "";
	%worstOver = 0;

	for(%w = 250; %w <= 430; %w += 6)
	{
		%window.resize(getWord(%pos, 0), getWord(%pos, 1), %w, %height);

		%scrollerW = getWord(%window.scroller.getExtent(), 0);
		for(%g = 0; %g < %window.groupCount; %g++)
		{
			%over = (getWord(%window.group[%g].getExtent(), 0) + %bar) - %scrollerW;
			if(%over > %worstOver)
			{
				%worstOver = %over;
				%worst = "window " @ %w @ ", group " @ %g @ ": " @
					getWord(%window.group[%g].getExtent(), 0) @ " + " @ %bar @
					" is " @ %over @ " past " @ %scrollerW;
			}
		}
	}

	%window.resize(getWord(%pos, 0), getWord(%pos, 1), getWord(%was, 0), %height);

	palCheck("no width leaves a group under the scroll bar (" @
		(%worstOver > 0 ? %worst : "31 widths clear") @ ")", %worstOver <= 0);
}

//-----------------------------------------------------------------------------
// Room for the scroll bar.
//
// A GuiScrollCtrl subtracts its bar when it CLIPS -- applyScrollBarSpacing feeds
// renderChildControls a narrowed rect -- but never when it lays out: the content
// child keeps its full extent and is simply drawn cut off. A group that took the
// scroller's whole width therefore ran one column under the bar, and the grid,
// sizing itself from that width, fitted a column that could not be seen.
//
// The palette takes the bar off itself. These checks are what says so.
//-----------------------------------------------------------------------------

function palWidthChecks()
{
	%window = GuiEditor.ctrlListWindow;
	%bar = %window.scroller.scrollBarThickness;
	%scroller = getWord(%window.scroller.getExtent(), 0);

	for(%g = 0; %g < %window.groupCount; %g++)
	{
		%group = %window.group[%g];
		%width = getWord(%group.getExtent(), 0);

		// The scroller's own borders are on top of this, so a group that merely
		// fits inside the outer extent is still too wide. Leaving a whole bar
		// spare is the part that cannot happen by accident.
		palCheck("group " @ %g @ " leaves room for the bar (" @ %width @ " + " @
			%bar @ " within " @ %scroller @ ")", (%width + %bar) <= %scroller);
	}

	// The grid is the thing that actually reflows, and it is sized from the
	// group, so this is the assertion that the columns are countable.
	%group = %window.group[0];
	palCheck("the grid is no wider than its group",
		getWord(%group.grid.getExtent(), 0) <= getWord(%group.getExtent(), 0));
}

//-----------------------------------------------------------------------------
// Switching views, and surviving a collapse.
//-----------------------------------------------------------------------------

function palModeChecks()
{
	%window = GuiEditor.ctrlListWindow;
	%group = %window.group[0];
	%tile = %group.tile[0];

	palCheck("grid mode shows the caption", %tile.caption.isVisible());
	palCheck("grid mode names the control, not the class",
		%tile.caption.getText() $= GuiEditor.controlIcons.labelFor(%tile.key));
	palCheck("grid mode centers the caption and sits it on the floor",
		%tile.caption.align $= "center" && %tile.caption.vAlign $= "bottom");
	palCheck("grid mode wraps a long name onto a second line", %tile.caption.textWrap);

	// The caption fills the tile's INNER rect -- that is both how the text finds
	// the floor and how the tile measures a border it cannot ask the theme for.
	// So it starts at the origin and is strictly smaller than the outer extent.
	palCheck("the grid caption fills the tile", %tile.caption.getPosition() $= "0 0");
	%innerH = getWord(%tile.caption.getExtent(), 1);
	palCheck("the caption measures the inner rect, not the outer (" @ %innerH @
		" inside " @ getWord(%tile.getExtent(), 1) @ ")",
		%innerH < getWord(%tile.getExtent(), 1));

	// The picture has to leave the band clear, or the second line of a long name
	// draws over it. This is the assertion the cell height exists to satisfy.
	%iconBottom = getWord(%tile.icon.getPosition(), 1) + getWord(%tile.icon.getExtent(), 1);
	palCheck("the icon clears the caption band (" @ %iconBottom @ " vs " @
		(%innerH - $GuiEditorControlTile::gridCaption) @ ")",
		%iconBottom <= (%innerH - $GuiEditorControlTile::gridCaption));
	palCheck("the icon is not pushed off the top of the tile",
		getWord(%tile.icon.getPosition(), 1) >= 0);
	palCheck("grid mode draws 56px art (" @ getWord(%tile.icon.getExtent(), 0) @ ")",
		getWord(%tile.icon.getExtent(), 0) == $GuiEditorControlTile::gridArt);
	palCheck("grid mode takes the 64 sheet", %tile.icon.Image $= "GuiEditor:controlIcons64");
	palCheck("the grid cell is tall enough for the picture and the band",
		%group.grid.CellSizeY == $GuiEditorControlGroup::gridCellHeight);

	%window.setMode("rows");
	palCheck("row mode shows the caption", %tile.caption.isVisible());
	palCheck("row mode names the control, not the class",
		%tile.caption.getText() $= GuiEditor.controlIcons.labelFor(%tile.key));
	palCheck("the tooltip still names the class",
		%tile.tooltip $= GuiEditor.controlIcons.classFor(%tile.key));

	// The two modes share one caption, so each has to put back what the other
	// wrote. This is the half that would rot silently.
	palCheck("row mode puts the caption back beside the icon",
		%tile.caption.align $= "left" && %tile.caption.vAlign $= "middle");
	palCheck("row mode stops wrapping", !%tile.caption.textWrap);
	palCheck("row mode draws 32px art (" @ getWord(%tile.icon.getExtent(), 0) @ ")",
		getWord(%tile.icon.getExtent(), 0) == $GuiEditorControlTile::rowArt);
	palCheck("row mode takes the 64 sheet", %tile.icon.Image $= "GuiEditor:controlIcons64");
	palCheck("row mode is one tile per row",
		%group.grid.CellSizeX >= getWord(%group.getExtent(), 0));

	%window.setMode("grid");
	palCheck("switching back restores the grid cell",
		%group.grid.CellSizeX == $GuiEditorControlGroup::gridCell);
	palCheck("switching back centers the caption again",
		%tile.caption.isVisible() && %tile.caption.align $= "center" &&
		%tile.caption.vAlign $= "bottom" && %tile.caption.textWrap);

	// Collapsing force-writes mVisible on the panel's direct children. The tiles
	// are grandchildren, so they must come back.
	%group.setExpanded(false);
	%window.relayout();
	%group.setExpanded(true);
	%window.relayout();
	palCheck("a tile survives its group being collapsed", %tile.isVisible());
	palCheck("the group reopened", %group.getExpanded());

	palThemeChecks();
}

//-----------------------------------------------------------------------------
// The editor theme.
//
// Every picture in this window is greyscale art that means nothing until it is
// modulated: on a light theme an untinted icon is a white smear on a pale
// panel. So the tint has to be set when the control is built AND set again when
// the theme changes -- and the second half is the one that goes missing,
// because it looks as though ThemeManager already does it. It does not: it
// swaps the profile OBJECT on every control that registered one, which repaints
// backgrounds and text on its own, and cannot reach a color that script has
// already copied onto a sprite.
//
// Checked against the default theme and the light one, because the default's
// text color is white -- the same color an untinted sprite draws in -- so a
// tile that is never tinted at all still looks right there and only breaks when
// someone switches.
//-----------------------------------------------------------------------------

function palThemeChecks()
{
	%was = ThemeManager.curTheme;

	palThemeTints("as built");

	// Lab Coat is the light theme: its text color is near-black where the
	// default's is white. Assert they differ, or every check below could pass
	// without a single sprite being touched.
	%before = ThemeManager.activeTheme.itemSelectProfile.fontColor;
	ThemeManager.setTheme(1);
	%after = ThemeManager.activeTheme.itemSelectProfile.fontColor;
	palCheck("the two themes really do differ (" @ %before @ " / " @ %after @ ")",
		!palSameColor(%before, %after));

	palThemeTints("after a theme change");

	ThemeManager.setTheme(%was);
	palThemeTints("after changing back");

	palDropChecks();
}

function palThemeTints(%when)
{
	%window = GuiEditor.ctrlListWindow;
	%theme = ThemeManager.activeTheme;
	%tile = %window.group[0].tile[0];

	// The tile is drawn on its own profile, so that is where its picture takes
	// its color from -- not from the caption's label profile, which happens to
	// name the same color in every theme that ships.
	palCheck("a tile tints its icon " @ %when @ " (" @ %tile.icon.getImageColor() @ ")",
		palSameColor(%tile.icon.getImageColor(), %theme.itemSelectProfile.fontColor));

	// The mode row is a radio group: one button is down and one is up, and the
	// two take different colors out of the same profile. Checking both is what
	// catches a refresh that only ever runs for one state.
	%on = %window.modeRow.choiceButton[0];
	%off = %window.modeRow.choiceButton[1];
	palCheck("the chosen mode button tints its icon " @ %when @ " (" @
		%on.icon.getImageColor() @ ")",
		palSameColor(%on.icon.getImageColor(), %theme.iconButtonProfile.fontColorHL));
	palCheck("the other mode button tints its icon " @ %when @ " (" @
		%off.icon.getImageColor() @ ")",
		palSameColor(%off.icon.getImageColor(), %theme.iconButtonProfile.fontColor));
}

// TypeColorI reads back as a stock color NAME whenever the components match one,
// so a white profile color answers "White" while the sprite that was set from it
// answers "255 255 255 255". Comparing the two as strings fails on exactly the
// colors a theme is most likely to use.
function palSameColor(%a, %b)
{
	return palColorI(%a) $= palColorI(%b);
}

function palColorI(%color)
{
	return isStockColor(%color) ? getStockColorI(%color) : %color;
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

	%depth = GuiEditor.undoRecorder.undoCount();
	%tile.onClick();

	palCheck("clicking a tile added a control (" @ %root.getCount() @ ")",
		%root.getCount() == %before + 1);
	%added = %root.getObject(%root.getCount() - 1);
	palCheck("it built the class the tile names", %added.getClassName() $= "GuiButtonCtrl");

	// The reason a click is routed through onControlDropped rather than adding
	// the control itself: that path reaches GuiEditCtrl::addNewControl, which
	// fires onAddNewCtrl, which is where the recorder hooks in. If a click ever
	// grows its own way into the document, this is the check that notices.
	palCheck("clicking a tile is one undo step (" @
		(GuiEditor.undoRecorder.undoCount() - %depth) @ ")",
		GuiEditor.undoRecorder.undoCount() == %depth + 1);

	GuiEditor.Undo();
	palCheck("undoing a click takes the control back out (" @ %root.getCount() @ ")",
		%root.getCount() == %before);
	GuiEditor.Redo();
	palCheck("and redo puts it back", %root.getCount() == %before + 1);
	%added = %root.getObject(%root.getCount() - 1);
	palCheck("redo restores the same class", %added.getClassName() $= "GuiButtonCtrl");

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
