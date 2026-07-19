//-----------------------------------------------------------------------------
// PlanetXSettings: the game's user preferences - sound volumes, per-player key
// bindings, and per-player aim mode. A session-long singleton (like
// PlanetXUpgrades), reachable by name from any file. onAdd seeds a default for
// anything unset, loads the saved prefs file over the defaults, and pushes the
// volume levels into the shared Audio module. save() writes the prefs back out;
// the options screen calls it whenever a setting changes.
//
// Everything lives in the $pref::PlanetX:: namespace so one export() captures it
// all, mirroring the engine's own preference pattern (toybox/Sandbox/1/main.cs).
// Bindings are read and written by key name through get()/set() so the options
// screen and key-capture control can drive them generically. See TORQUE_SCRIPT.md.
//-----------------------------------------------------------------------------

// Saved next to the executable (working dir), exactly as the Sandbox toy does.
$PlanetX::PrefsFile = "PlanetXPrefs.cs";

function PlanetXSettings::onAdd(%this)
{
	%this.applyDefaults();

	// Saved prefs, loaded after the defaults, win.
	if (isFile($PlanetX::PrefsFile))
		exec($PlanetX::PrefsFile);

	%this.applyAudio();
}

/// Persist every PlanetX pref to disk in one shot. Called on any settings change
/// and once more on shutdown (see PlanetXGame::destroy).
function PlanetXSettings::save(%this)
{
	export("$pref::PlanetX::*", $PlanetX::PrefsFile, false);
}

//-----------------------------------------------------------------------------
// Defaults. Seed only what is still blank so a first run - or a newly added
// setting on an existing install - always has a sensible value, while saved prefs
// loaded afterward take precedence.
//-----------------------------------------------------------------------------

function PlanetXSettings::applyDefaults(%this)
{
	%this.setDefault("MasterVolume", 1);
	%this.setDefault("MusicVolume", 0.6);
	%this.setDefault("SoundVolume", 1);

	// Player 1: arrow keys; fires with Return (used when P1 aims automatically -
	// in mouse mode the mouse fires); aims with the mouse.
	%this.setDefault("P1Up", "up");
	%this.setDefault("P1Down", "down");
	%this.setDefault("P1Left", "left");
	%this.setDefault("P1Right", "right");
	%this.setDefault("P1Fire", "return");
	%this.setDefault("P1Aim", "mouse");

	// Player 2 (co-op): WASD; fires with Space; aims automatically (no second mouse).
	// Distinct fire keys keep the two players from cross-firing.
	%this.setDefault("P2Up", "w");
	%this.setDefault("P2Down", "s");
	%this.setDefault("P2Left", "a");
	%this.setDefault("P2Right", "d");
	%this.setDefault("P2Fire", "space");
	%this.setDefault("P2Aim", "auto");
}

//-----------------------------------------------------------------------------
// Generic pref access by key name. The names are built at runtime (the options
// screen iterates over "P1Up", "P2Fire", ...), so we reach the $pref:: globals
// through eval - the same idiom the Sandbox uses for dynamic setters.
//-----------------------------------------------------------------------------

/// Read $pref::PlanetX::<key>.
function PlanetXSettings::get(%this, %key)
{
	return eval("return $pref::PlanetX::" @ %key @ ";");
}

/// Write $pref::PlanetX::<key>.
function PlanetXSettings::set(%this, %key, %value)
{
	eval("$pref::PlanetX::" @ %key @ " = \"" @ %value @ "\";");
}

/// Write the key only if it is currently blank.
function PlanetXSettings::setDefault(%this, %key, %value)
{
	if (%this.get(%key) $= "")
		%this.set(%key, %value);
}

/// The display label for a bound key. The pref stores the raw action string (e.g.
/// "up" / "space"); the rebind buttons show it uppercased.
function PlanetXSettings::keyLabel(%this, %key)
{
	return strupr(%key);
}

/// The action (pref key, e.g. "P2Up") currently bound to %key, ignoring %exceptKey,
/// or "" if none. The key-capture control uses this to SWAP bindings: when a key is
/// reassigned, whatever action held it takes over the key being replaced, so no two
/// actions ever share one key.
function PlanetXSettings::actionForKey(%this, %key, %exceptKey)
{
	%actions = "P1Up" TAB "P1Down" TAB "P1Left" TAB "P1Right" TAB "P1Fire" TAB
	           "P2Up" TAB "P2Down" TAB "P2Left" TAB "P2Right" TAB "P2Fire";

	for (%i = 0; %i < getFieldCount(%actions); %i++)
	{
		%action = getField(%actions, %i);
		if (%action $= %exceptKey)
			continue;
		if (%this.get(%action) $= %key)
			return %action;
	}

	return "";
}

//-----------------------------------------------------------------------------
// Audio. Push the saved volume levels into the shared Audio module. Called once at
// startup and again by each volume slider as it moves (via Audio directly).
//-----------------------------------------------------------------------------

function PlanetXSettings::applyAudio(%this)
{
	Audio.setMasterVolume($pref::PlanetX::MasterVolume);
	Audio.SetMusicVolume($pref::PlanetX::MusicVolume);
	Audio.SetSoundVolume($pref::PlanetX::SoundVolume);
}
