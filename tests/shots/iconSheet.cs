// Renders the editor icon sheets so a button can be given an icon that actually
// means something -- and so a regenerated sheet can be checked against the
// constants that name it.
//
// Page 0-3 walk EditorCore:editorIcons16/24/32/48 a screenful at a time, each
// icon drawn at its frame index. Every sheet is the same 32x10 grid of the same
// 304 icons, so an index names the same picture on all four pages; the pages
// differ only in which source art is being magnified.
setLogMode(2);
setScriptExecEcho(false);
trace(false);
$Scripts::ignoreDSOs = true;
setCompanyAndProduct("Torque Game Engines", "Torque2D");
ModuleDatabase.EchoInfo = false;
AssetDatabase.EchoInfo = false;

testExec("editor/main.cs");
schedule(2500, 0, "sheetShot");

$iconSheet::perPage = 60;
$iconSheet::count = 304;
$iconSheet::page = 0;

function sheetShot()
{
	%first = $iconSheet::page * $iconSheet::perPage;
	if(%first >= $iconSheet::count)
	{
		echo("SHOTS DONE");
		quit();
		return;
	}

	%page = new GuiControl() { Position = "0 0"; Extent = "1024 768"; };
	ThemeManager.setProfile(%page, "panelProfile");
	$iconSheet::gui = %page;

	// One column per icon size, so the same index can be compared across sheets
	// in a single glance.
	%sizes = "16 24 32 48";
	for(%i = 0; %i < $iconSheet::perPage; %i++)
	{
		%index = %first + %i;
		if(%index >= $iconSheet::count)
		{
			break;
		}

		%col = %i % 5;
		%row = mFloor(%i / 5);
		%x = 12 + (%col * 202);
		%y = 12 + (%row * 62);

		for(%s = 0; %s < 4; %s++)
		{
			%size = getWord(%sizes, %s);
			%icon = new GuiSpriteCtrl()
			{
				Position = (%x + (%s * 44)) SPC %y;
				Extent = "40 40";
				Image = "EditorCore:editorIcons" @ %size;
				ImageSize = %size SPC %size;
				Frame = %index;
				ImageColor = "255 255 255 255";
				constrainProportions = "1";
				fullSize = "0";
			};
			ThemeManager.setProfile(%icon, "spriteProfile");
			%page.add(%icon);
		}

		%label = new GuiControl()
		{
			Position = %x SPC (%y + 42);
			Extent = "180 16";
			Text = %index;
		};
		ThemeManager.setProfile(%label, "labelProfile");
		%page.add(%label);
	}

	Canvas.pushDialog(%page);
	schedule(1000, 0, "sheetGrab");
}

function sheetGrab()
{
	screenShot(testRoot("shots/editorIconSheet" @ $iconSheet::page @ ".png"), "PNG");
	schedule(500, 0, "sheetNext");
}

function sheetNext()
{
	Canvas.popDialog($iconSheet::gui);
	$iconSheet::gui.delete();
	$iconSheet::page++;
	schedule(200, 0, "sheetShot");
}
