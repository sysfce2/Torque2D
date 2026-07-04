//-----------------------------------------------------------------------------
// The in-game HUD: hull (health) bar top-left and the objective hint. Built
// in script as children of PlanetXRoot, layered over the scene window.
//-----------------------------------------------------------------------------

function PlanetXGame::buildHud(%this)
{
	%hullLabel = new GuiControl()
	{
		Profile = "GuiLabelProfile";
		HorizSizing = "right";
		VertSizing = "bottom";
		Position = "20 16";
		Extent = "60 24";
		Text = "HULL";
	};
	PlanetXRoot.add(%hullLabel);

	%healthBar = new GuiProgressCtrl(PlanetXHealthBar)
	{
		Profile = "GuiProgressProfile";
		HorizSizing = "right";
		VertSizing = "bottom";
		Position = "84 16";
		Extent = "260 24";
	};
	PlanetXRoot.add(%healthBar);
	%healthBar.setProgress(1);

	%hint = new GuiControl(PlanetXObjectiveHint)
	{
		Profile = "GuiTextProfile";
		HorizSizing = "center";
		VertSizing = "bottom";
		Position = "312 24";
		Extent = "400 24";
		Text = "FIND THE CRYSTAL";
		Align = "center";
		OverrideFontColor = "1";
		FontColor = "234 72 72 255";
	};
	PlanetXRoot.add(%hint);
	%hint.schedule(6000, "setVisible", false);
}

function PlanetXGame::updateHealthBar(%this, %health)
{
	if (isObject(PlanetXHealthBar))
		PlanetXHealthBar.setProgress(%health / $PlanetX::PlayerMaxHealth, 150);
}
