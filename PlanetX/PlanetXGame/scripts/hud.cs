//-----------------------------------------------------------------------------
// PlanetXHud: the in-game heads-up display - hull (health) bar, gun-heat bar,
// level readout, and the objective hint. A manager ScriptObject: it builds all
// its controls into one panel over the scene window in onAdd and deletes that
// panel in onRemove, so the level frees the whole HUD with a single delete.
//
// The level passes the level number (%this.number) for the readout. The player
// and weapon push updates through setHealth/setHeat.
//-----------------------------------------------------------------------------

function PlanetXHud::onAdd(%this)
{
	// A coral-filled clone of the stock progress profile for the heat bar. It
	// is a shared resource, so it persists across levels (created once).
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

	// One panel holds every HUD widget; deleting it frees them all. It is
	// full-screen and sits ON TOP of the scene window, so it MUST be mouse-
	// transparent (useInput = false) or it would swallow every aim/fire event
	// meant for the scene beneath it. useInput is inherited by hit-testing:
	// with it off, the panel and all its children are skipped by findHitControl.
	%this.panel = new GuiControl()
	{
		Profile = "GuiDefaultProfile";
		HorizSizing = "relative";
		VertSizing = "relative";
		Position = "0 0";
		Extent = "1024 768";
		useInput = false;
	};
	PlanetXRoot.add(%this.panel);

	%hullLabel = new GuiControl()
	{
		Profile = "PlanetXLabelProfile";
		HorizSizing = "right";
		VertSizing = "bottom";
		Position = "20 14";
		Extent = "90 34";
		Text = "HULL";
	};
	%this.panel.add(%hullLabel);

	%this.healthBar = new GuiProgressCtrl()
	{
		Profile = "GuiProgressProfile";
		HorizSizing = "right";
		VertSizing = "bottom";
		Position = "116 20";
		Extent = "240 26";
	};
	%this.panel.add(%this.healthBar);
	%this.healthBar.setProgress(1);

	%this.heatLabel = new GuiControl()
	{
		Profile = "PlanetXLabelProfile";
		HorizSizing = "right";
		VertSizing = "bottom";
		Position = "20 54";
		Extent = "90 30";
		Text = "HEAT";
	};
	%this.panel.add(%this.heatLabel);

	%this.heatBar = new GuiProgressCtrl()
	{
		Profile = "PlanetXHeatProfile";
		HorizSizing = "right";
		VertSizing = "bottom";
		Position = "116 60";
		Extent = "240 16";
	};
	%this.panel.add(%this.heatBar);
	%this.heatBar.setProgress(0);

	// In co-op, player 2 gets a matching hull/heat readout mirrored along the
	// top-right edge (player 1 stays top-left). Built only in two-player mode.
	if ($PlanetX::twoPlayer)
	{
		%hullLabel2 = new GuiControl()
		{
			Profile = "PlanetXLabelProfile";
			HorizSizing = "left";
			VertSizing = "bottom";
			Position = "914 14";
			Extent = "90 34";
			Text = "HULL";
			Align = "right";
		};
		%this.panel.add(%hullLabel2);

		%this.healthBar2 = new GuiProgressCtrl()
		{
			Profile = "GuiProgressProfile";
			HorizSizing = "left";
			VertSizing = "bottom";
			Position = "668 20";
			Extent = "240 26";
		};
		%this.panel.add(%this.healthBar2);
		%this.healthBar2.setProgress(1);

		%this.heatLabel2 = new GuiControl()
		{
			Profile = "PlanetXLabelProfile";
			HorizSizing = "left";
			VertSizing = "bottom";
			Position = "914 54";
			Extent = "90 30";
			Text = "HEAT";
			Align = "right";
		};
		%this.panel.add(%this.heatLabel2);

		%this.heatBar2 = new GuiProgressCtrl()
		{
			Profile = "PlanetXHeatProfile";
			HorizSizing = "left";
			VertSizing = "bottom";
			Position = "668 60";
			Extent = "240 16";
		};
		%this.panel.add(%this.heatBar2);
		%this.heatBar2.setProgress(0);

		%this.heatWarning2 = false;
	}

	// The level readout sits in the top center in both modes - there is room and
	// it reads better there than tucked in a corner.
	%levelLabel = new GuiControl()
	{
		Profile = "PlanetXLabelProfile";
		HorizSizing = "center";
		VertSizing = "bottom";
		Position = "362 14";
		Extent = "300 36";
		Align = "center";
	};
	%this.panel.add(%levelLabel);
	%levelLabel.setText("LEVEL" SPC %this.number);

	%this.hint = new GuiControl()
	{
		Profile = "PlanetXTextProfile";
		HorizSizing = "center";
		VertSizing = "bottom";
		Position = "262 56";
		Extent = "500 34";
		Text = "FIND THE CRYSTAL";
		Align = "center";
		OverrideFontColor = "1";
		FontColor = "234 72 72 255";
	};
	%this.panel.add(%this.hint);
	%this.hintEvent = %this.hint.schedule(6000, "setVisible", false);

	%this.heatWarning = false;
}

function PlanetXHud::onRemove(%this)
{
	if (isEventPending(%this.hintEvent))
		cancel(%this.hintEvent);
	if (isObject(%this.panel))
		%this.panel.delete();
}

function PlanetXHud::setHealth(%this, %index, %health)
{
	%bar = (%index == 2) ? %this.healthBar2 : %this.healthBar;
	if (isObject(%bar))
		%bar.setProgress(%health / $PlanetX::PlayerMaxHealth, 150);
}

function PlanetXHud::setHeat(%this, %index, %heat, %overheated, %tickMs)
{
	%bar   = (%index == 2) ? %this.heatBar2   : %this.heatBar;
	%label = (%index == 2) ? %this.heatLabel2 : %this.heatLabel;
	%warned = (%index == 2) ? %this.heatWarning2 : %this.heatWarning;

	if (isObject(%bar))
		%bar.setProgress(%heat, %tickMs);

	// The label doubles as the overheat warning.
	if (isObject(%label) && %overheated != %warned)
	{
		if (%index == 2)
			%this.heatWarning2 = %overheated;
		else
			%this.heatWarning = %overheated;

		%label.OverrideFontColor = %overheated;
		%label.FontColor = "234 72 72 255";
		%label.setText(%overheated ? "VENT!" : "HEAT");
	}
}
