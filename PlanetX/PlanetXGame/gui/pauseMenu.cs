//-----------------------------------------------------------------------------
// PlanetXPauseMenu: the "GAME PAUSED" dialog raised by Esc during play. A manager
// ScriptObject (same shape as PlanetXUpgradeScreen): it builds one GuiControl
// dialog in onAdd and deletes it in onRemove, so the whole menu frees with a single
// delete. PlanetXGame builds it once (lazily) and reuses it.
//
// The buttons call back into PlanetXGame - the state machine. Continue resumes the
// frozen level; Options swaps in the shared options window; Main Menu tears the
// level down to the title (dialogToTitle, shared with the game-over dialog); Quit
// exits. See game.cs and TORQUE_SCRIPT.md.
//-----------------------------------------------------------------------------

function PlanetXPauseMenu::onAdd(%this)
{
	%this.build();
}

function PlanetXPauseMenu::onRemove(%this)
{
	if (isObject(%this.dialog))
		%this.dialog.delete();
}

function PlanetXPauseMenu::build(%this)
{
	%w = 480;
	%h = 430;

	%this.dialog = new GuiControl()
	{
		Profile = "GuiWindowProfile";
		HorizSizing = "center";
		VertSizing = "center";
		Position = ((1024 - %w) / 2) SPC ((768 - %h) / 2);
		Extent = %w SPC %h;
	};

	%heading = new GuiControl()
	{
		Profile = "PlanetXLabelProfile";
		HorizSizing = "center";
		VertSizing = "bottom";
		Position = "0 30";
		Extent = %w SPC 44;
		Text = "GAME PAUSED";
		Align = "center";
	};
	%this.dialog.add(%heading);

	%this.addButton(110, "CONTINUE",  "PlanetXGame.resumeGame();");
	%this.addButton(180, "OPTIONS",   "PlanetXGame.openOptionsFromPause();");
	%this.addButton(250, "MAIN MENU", "PlanetXGame.dialogToTitle();");
	%this.addButton(320, "QUIT",      "quit();");
}

/// One centered menu button. Every button plays the shared click before its action.
function PlanetXPauseMenu::addButton(%this, %y, %text, %action)
{
	%btn = new GuiButtonCtrl()
	{
		Profile = "PlanetXButtonProfile";
		HorizSizing = "center";
		VertSizing = "bottom";
		Position = "100" SPC %y;
		Extent = "280 52";
		Text = %text;
		Command = "PlanetXGame.playClick(); " @ %action;
	};
	%this.dialog.add(%btn);
}
