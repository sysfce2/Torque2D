// Temporary: renders every frame of EditorCore:editorIcons16 at 4x with its index
// so a button can be given an icon that actually means something.
setLogMode(2);
setScriptExecEcho(false);
trace(false);
$Scripts::ignoreDSOs = true;
setCompanyAndProduct("Torque Game Engines", "Torque2D");
ModuleDatabase.EchoInfo = false;
AssetDatabase.EchoInfo = false;

testExec("editor/main.cs");
schedule(2500, 0, "sheetShot");

function sheetShot()
{
	%page = new GuiControl() { Position = "0 0"; Extent = "1024 768"; };
	ThemeManager.setProfile(%page, "panelProfile");

	for(%i = 0; %i < 64; %i++)
	{
		%col = %i % 8;
		%row = mFloor(%i / 8);
		%x = 20 + (%col * 124);
		%y = 20 + (%row * 92);

		%icon = new GuiSpriteCtrl()
		{
			Position = %x SPC %y;
			Extent = "64 64";
			Image = "EditorCore:EditorIcons16";
			ImageSize = "16 16";
			Frame = %i;
			ImageColor = "255 255 255 255";
			constrainProportions = "1";
			fullSize = "0";
		};
		ThemeManager.setProfile(%icon, "spriteProfile");
		%page.add(%icon);

		%label = new GuiControl() { Position = (%x + 66) SPC (%y + 22); Extent = "40 20"; Text = %i; };
		ThemeManager.setProfile(%label, "labelProfile");
		%page.add(%label);
	}

	Canvas.pushDialog(%page);
	schedule(1000, 0, "sheetGrab");
}

function sheetGrab()
{
	screenShot(testRoot("shots/editorIconSheet.png"), "PNG");
	schedule(500, 0, "sheetDone");
}

function sheetDone()
{
	echo("SHOTS DONE");
	quit();
}
