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
		Profile = "GuiLabelProfile";
		HorizSizing = "right";
		VertSizing = "bottom";
		Position = "20 16";
		Extent = "60 24";
		Text = "HULL";
	};
	%this.panel.add(%hullLabel);

	%this.healthBar = new GuiProgressCtrl()
	{
		Profile = "GuiProgressProfile";
		HorizSizing = "right";
		VertSizing = "bottom";
		Position = "84 16";
		Extent = "260 24";
	};
	%this.panel.add(%this.healthBar);
	%this.healthBar.setProgress(1);

	%this.heatLabel = new GuiControl()
	{
		Profile = "GuiLabelProfile";
		HorizSizing = "right";
		VertSizing = "bottom";
		Position = "20 44";
		Extent = "60 18";
		Text = "HEAT";
	};
	%this.panel.add(%this.heatLabel);

	%this.heatBar = new GuiProgressCtrl()
	{
		Profile = "PlanetXHeatProfile";
		HorizSizing = "right";
		VertSizing = "bottom";
		Position = "84 47";
		Extent = "260 14";
	};
	%this.panel.add(%this.heatBar);
	%this.heatBar.setProgress(0);

	%levelLabel = new GuiControl()
	{
		Profile = "GuiLabelProfile";
		HorizSizing = "left";
		VertSizing = "bottom";
		Position = "804 16";
		Extent = "200 24";
		Align = "right";
	};
	%this.panel.add(%levelLabel);
	%levelLabel.setText("LEVEL" SPC %this.number);

	%this.hint = new GuiControl()
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

function PlanetXHud::setHealth(%this, %health)
{
	if (isObject(%this.healthBar))
		%this.healthBar.setProgress(%health / $PlanetX::PlayerMaxHealth, 150);
}

function PlanetXHud::setHeat(%this, %heat, %overheated, %tickMs)
{
	if (isObject(%this.heatBar))
		%this.heatBar.setProgress(%heat, %tickMs);

	// The label doubles as the overheat warning.
	if (isObject(%this.heatLabel) && %overheated != %this.heatWarning)
	{
		%this.heatWarning = %overheated;
		%this.heatLabel.OverrideFontColor = %overheated;
		%this.heatLabel.FontColor = "234 72 72 255";
		%this.heatLabel.setText(%overheated ? "VENT!" : "HEAT");
	}
}
