// Renders the control palette's icon sheets, tinted the way the editor will tint
// them, with each entry's key beside its picture.
//
// Two jobs. The obvious one is looking at the art at the size it will actually be
// drawn -- the review sheets the build script writes are on a dark ground of
// their own choosing, and this is the real theme.
//
// The load-bearing one is proving the assets load at all. GuiEditor's module.taml
// carried no <DeclaredAssets> block until these sheets arrived; without it the
// PNGs sit on disk and are never registered, with no error anywhere. A missing
// image renders as nothing rather than throwing, so the failure is a page of
// labels with blank space where the icons should be -- which is exactly what this
// shot shows, and nothing else would.
//
// Page 0 is the 128 sheet at 96px, which is what the grid view will draw. Page 1
// is the 64 sheet at 48px, which is the row view.
setLogMode(2);
setScriptExecEcho(false);
trace(false);
$Scripts::ignoreDSOs = true;
setCompanyAndProduct("Torque Game Engines", "Torque2D");
ModuleDatabase.EchoInfo = false;
AssetDatabase.EchoInfo = false;

testExec("editor/main.cs");
schedule(2500, 0, "controlIconsShot");

$controlIcons::page = 0;
$controlIcons::pages = 2;

function controlIconsShot()
{
	if($controlIcons::page >= $controlIcons::pages)
	{
		echo("SHOTS DONE");
		quit();
		return;
	}

	// The table the Gui Editor built at create time. Asking it rather than
	// hard-coding 31 means this page cannot drift from the sheet.
	%icons = GuiEditor.controlIcons;
	%keys = %icons.keys();
	%count = getFieldCount(%keys);

	%big = ($controlIcons::page == 0);
	%tile = %big ? 96 : 48;
	%sheet = %icons.sheetFor(%tile);
	%cell = %big ? 128 : 64;

	%page = new GuiControl() { Position = "0 0"; Extent = "1024 768"; };
	ThemeManager.setProfile(%page, "panelProfile");
	$controlIcons::gui = %page;

	%cols = %big ? 6 : 8;
	%stepX = %big ? 168 : 126;
	%stepY = %big ? 128 : 76;

	// The fallback is not in keys() -- it is what the palette falls back TO, not
	// something anyone drags -- so it is drawn first and by hand. It is the one
	// frame a wrong answer from frameFor would land on, so seeing it is the point.
	controlIconsTile(%page, 12, 8, %tile, %sheet, %cell, 0, "unknown");

	for(%i = 0; %i < %count; %i++)
	{
		%key = getField(%keys, %i);
		%slot = %i + 1;
		%x = 12 + ((%slot % %cols) * %stepX);
		%y = 8 + (mFloor(%slot / %cols) * %stepY);
		controlIconsTile(%page, %x, %y, %tile, %sheet, %cell, %icons.frameFor(%key), %key);
	}

	Canvas.pushDialog(%page);
	schedule(1000, 0, "controlIconsGrab");
}

// One tile: the sprite, tinted from the theme exactly as EditorIconButton tints
// its icon, with the entry's key under it.
function controlIconsTile(%page, %x, %y, %tile, %sheet, %cell, %frame, %label)
{
	%icon = new GuiSpriteCtrl()
	{
		Position = %x SPC %y;
		Extent = %tile SPC %tile;
		Image = %sheet;
		ImageSize = %cell SPC %cell;
		Frame = %frame;
		ImageColor = ThemeManager.activeTheme.iconButtonProfile.FontColor;
		constrainProportions = "1";
		fullSize = "0";
	};
	ThemeManager.setProfile(%icon, "spriteProfile");
	%page.add(%icon);

	%text = new GuiControl()
	{
		Position = %x SPC (%y + %tile + 2);
		Extent = (%tile + 60) SPC 16;
		Text = %label;
	};
	ThemeManager.setProfile(%text, "labelProfile");
	%page.add(%text);
}

function controlIconsGrab()
{
	screenShot(testRoot("shots/controlIcons" @ $controlIcons::page @ ".png"), "PNG");
	schedule(500, 0, "controlIconsNext");
}

function controlIconsNext()
{
	Canvas.popDialog($controlIcons::gui);
	$controlIcons::gui.delete();
	$controlIcons::page++;
	schedule(200, 0, "controlIconsShot");
}
