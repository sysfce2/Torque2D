//-----------------------------------------------------------------------------
// PlanetXTitleScreen: the title menu, a manager ScriptObject that owns its own
// GUI. It builds the GUI (layout in titleGui.gui.taml, plus the engine version
// label TAML can't fill) in onAdd and frees it in onRemove. open()/close() show
// and hide it. It lives for the whole session, so the game just calls open()
// each time it returns to the title.
//
// The menu buttons' Command fields call back into PlanetXGame (startGame/quit),
// which is the state machine - see titleGui.gui.taml.
//-----------------------------------------------------------------------------

function PlanetXTitleScreen::onAdd(%this)
{
	%this.gui = TamlRead(expandPath("^PlanetXGame/gui/titleGui.gui.taml"));
	TitleVersionLabel.setText(getEngineVersion());
}

function PlanetXTitleScreen::onRemove(%this)
{
	if (isObject(%this.gui))
		%this.gui.delete();
}

/// Show the title. %instant skips the fade (first boot, with no prior content).
function PlanetXTitleScreen::open(%this, %instant)
{
	if (%instant)
		Canvas.setContent(%this.gui);
	else
		ScreenFade.swapCanvas(%this.gui, "48 0 34", 800);
}

/// The canvas moves to the level (or the blank control) on its own when play
/// starts, so there is nothing to tear down between showings.
function PlanetXTitleScreen::close(%this)
{
}
