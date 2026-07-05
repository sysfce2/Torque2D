//-----------------------------------------------------------------------------
// The in-game HUD: hull (health) bar top-left and the objective hint. Built
// in script as children of PlanetXRoot, layered over the scene window.
//-----------------------------------------------------------------------------

function PlanetXGame::buildHud(%this)
{
	// A coral-filled clone of the stock progress profile for the heat bar.
	if (!isObject(PlanetXHeatProfile))
	{
		new GuiControlProfile(PlanetXHeatProfile)
		{
			fillColor = "48 0 34 255";
			fillColorHL = "234 72 72 255";
			fillColorSL = "255 120 110 255";
			borderDefault = GuiProgressBorderProfile;
			borderBottom = GuiProgressDarkBorderProfile;
			borderRight = GuiProgressDarkBorderProfile;
		};
	}

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

	%heatLabel = new GuiControl(PlanetXHeatLabel)
	{
		Profile = "GuiLabelProfile";
		HorizSizing = "right";
		VertSizing = "bottom";
		Position = "20 44";
		Extent = "60 18";
		Text = "HEAT";
	};
	PlanetXRoot.add(%heatLabel);

	%heatBar = new GuiProgressCtrl(PlanetXHeatBar)
	{
		Profile = "PlanetXHeatProfile";
		HorizSizing = "right";
		VertSizing = "bottom";
		Position = "84 47";
		Extent = "260 14";
	};
	PlanetXRoot.add(%heatBar);
	%heatBar.setProgress(0);

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

function PlanetXGame::updateHeatBar(%this)
{
	if (isObject(PlanetXHeatBar))
		PlanetXHeatBar.setProgress(%this.gunHeat, $PlanetX::HeatTickMs);

	// The label doubles as the overheat warning.
	if (isObject(PlanetXHeatLabel) && %this.overheated != PlanetXHeatLabel.warning)
	{
		PlanetXHeatLabel.warning = %this.overheated;
		PlanetXHeatLabel.OverrideFontColor = %this.overheated;
		PlanetXHeatLabel.FontColor = "234 72 72 255";
		PlanetXHeatLabel.setText(%this.overheated ? "VENT!" : "HEAT");
	}
}
