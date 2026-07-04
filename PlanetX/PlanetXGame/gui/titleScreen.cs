//-----------------------------------------------------------------------------
// PlanetX title screen. Layout lives in titleGui.gui.taml; this file loads it
// and fills in the pieces TAML can't (the engine version label).
//-----------------------------------------------------------------------------

function PlanetXGame::buildTitleScreen(%this)
{
	if (!isObject(%this.titleGui))
		%this.titleGui = TamlRead(expandPath("^PlanetXGame/gui/titleGui.gui.taml"));

	TitleVersionLabel.setText(getEngineVersion());

	return %this.titleGui;
}
