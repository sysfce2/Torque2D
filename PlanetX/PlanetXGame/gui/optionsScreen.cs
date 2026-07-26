//-----------------------------------------------------------------------------
// PlanetXOptionsScreen: the options window - sound volumes, per-player key
// bindings, and a per-player aim-mode toggle. A manager ScriptObject (same shape
// as PlanetXUpgradeScreen): it builds one GuiControl dialog of controls in onAdd
// and deletes it in onRemove, so the whole screen frees with a single delete.
//
// It is a session singleton held by PlanetXGame (built once, reused). It opens
// two ways: over the title screen (PlanetXGame::openOptions) and from the pause
// menu (PlanetXGame::openOptionsFromPause, a dialog swap). refresh() reseeds every
// slider and label from the current prefs on each open, since prefs can change
// between openings. Back saves and returns to wherever it was opened from
// (PlanetXGame::closeOptions).
//
// The volume sliders write straight to their $pref:: global (Variable) and apply
// live through the Audio module (AltCommand); the key buttons open the key-capture
// overlay (keyCapture.cs). See TORQUE_SCRIPT.md.
//-----------------------------------------------------------------------------

// Player column heading colors - the same green/red the hull bars and upgrade
// cards use for the two players.
$PlanetX::OptionsP1Color = "33 191 132 255";
$PlanetX::OptionsP2Color = "246 75 72 255";

function PlanetXOptionsScreen::onAdd(%this)
{
	%this.build();
}

/// The dialog and every control in it hang off %this.dialog, so one delete frees
/// the whole screen. ScreenFade only ever removes the dialog from its backdrop; it
/// never deletes it, so ownership stays here.
function PlanetXOptionsScreen::onRemove(%this)
{
	if (isObject(%this.dialog))
		%this.dialog.delete();
}

//-----------------------------------------------------------------------------
// Construction.
//-----------------------------------------------------------------------------

function PlanetXOptionsScreen::build(%this)
{
	%w = 820;
	%h = 660;

	%this.dialog = new GuiControl()
	{
		Profile = "PlanetXWindowProfile";
		HorizSizing = "center";
		VertSizing = "center";
		Position = ((1024 - %w) / 2) SPC ((768 - %h) / 2);
		Extent = %w SPC %h;
	};

	%this.addLabel(0, 16, %w, 40, "OPTIONS", "PlanetXLabelProfile", "center", "", 2);

	// --- Sound ---------------------------------------------------------------
	%this.addLabel(60, 64, 300, 26, "SOUND", "PlanetXLabelProfile", "left", "", 1.24);
	%this.buildSlider(98,  "MASTER", "MasterVolume", "Audio.setMasterVolume($pref::PlanetX::MasterVolume);");
	%this.buildSlider(136, "MUSIC",  "MusicVolume",  "Audio.SetMusicVolume($pref::PlanetX::MusicVolume);");
	%this.buildSlider(174, "SFX",    "SoundVolume",  "Audio.SetSoundVolume($pref::PlanetX::SoundVolume);");

	// --- Controls ------------------------------------------------------------
	%this.addLabel(60, 214, 300, 26, "CONTROLS", "PlanetXLabelProfile", "left", "", 1.24);

	%p1x = 70;
	%p2x = 440;

	%this.buildColumnHeader(%p1x, "PLAYER 1", $PlanetX::OptionsP1Color);
	%this.buildColumnHeader(%p2x, "PLAYER 2 (CO-OP)", $PlanetX::OptionsP2Color);

	%this.buildAimToggle(%p1x, 288, "P1");
	%this.buildAimToggle(%p2x, 288, "P2");

	// One row per movement/fire action, both columns.
	%actions = "Up" TAB "Down" TAB "Left" TAB "Right" TAB "Fire";
	for (%i = 0; %i < getFieldCount(%actions); %i++)
	{
		%action = getField(%actions, %i);
		%y = 328 + %i * 38;
		%this.buildKeyRow(%p1x, %y, "P1", %action);
		%this.buildKeyRow(%p2x, %y, "P2", %action);
	}

	// --- Back ----------------------------------------------------------------
	%back = new GuiButtonCtrl()
	{
		Profile = "PlanetXButtonProfile";
		FontSizeAdjust = 2;
		HorizSizing = "center";
		VertSizing = "bottom";
		Position = ((%w - 260) / 2) SPC 588;
		Extent = "260 50";
		Text = "BACK";
		Command = "PlanetXGame.playClick(); PlanetXGame.closeOptions();";
	};
	%this.dialog.add(%back);
}

/// Small helper: a plain text control added to the dialog. %color "" leaves the
/// profile's default font color.
// %fontAdjust multiplies the profile's font size (blank = leave it alone). It is
// how this screen gets menu-sized and card-sized text out of the theme's one
// Label profile, instead of cloning the profile per size.
function PlanetXOptionsScreen::addLabel(%this, %x, %y, %w, %h, %text, %profile, %align, %color, %fontAdjust)
{
	%label = new GuiControl()
	{
		Profile = %profile;
		HorizSizing = "right";
		VertSizing = "bottom";
		Position = %x SPC %y;
		Extent = %w SPC %h;
		Text = %text;
		Align = %align;
	};

	if (%fontAdjust !$= "")
	{
		%label.FontSizeAdjust = %fontAdjust;
	}

	if (%color !$= "")
	{
		%label.OverrideFontColor = "1";
		%label.FontColor = %color;
	}

	%this.dialog.add(%label);
	return %label;
}

/// A volume row: a label plus a slider bound to $pref::PlanetX::<prefKey>. The
/// slider writes the pref as it moves (Variable), applies live through the Audio
/// module (AltCommand), and saves once on release (Command).
function PlanetXOptionsScreen::buildSlider(%this, %y, %labelText, %prefKey, %audioCall)
{
	%this.addLabel(80, %y, 180, 26, %labelText, "PlanetXLabelProfile", "left", "", 1.24);

	// The theme's Slider profile draws the groove; its thumb comes from the
	// SliderThumb profile the control picks up by name.
	%slider = new GuiSliderCtrl()
	{
		Profile = "PlanetXSliderProfile";
		ThumbProfile = "PlanetXSliderThumbProfile";
		HorizSizing = "right";
		VertSizing = "bottom";
		Position = "270" SPC (%y - 2);
		Extent = "470 26";
		Range = "0 1";
		Ticks = "0";
		Value = PlanetXGame.settings.get(%prefKey);
		Variable = "$pref::PlanetX::" @ %prefKey;
		AltCommand = %audioCall;
		Command = "PlanetXGame.settings.save();";
	};
	%this.dialog.add(%slider);
	%this.slider[%prefKey] = %slider;
}

/// A player column heading in that player's color.
function PlanetXOptionsScreen::buildColumnHeader(%this, %colX, %text, %color)
{
	%this.addLabel(%colX + 8, 252, 300, 28, %text, "PlanetXLabelProfile", "left", %color, 1.24);
}

/// A rebind row: the action label plus a button showing the current key. Clicking
/// the button raises the key-capture overlay (keyCapture.cs), which relabels it.
function PlanetXOptionsScreen::buildKeyRow(%this, %colX, %y, %player, %action)
{
	%prefKey = %player @ %action;
	%actionLabel = strupr(%action);

	%this.addLabel(%colX + 8, %y, 100, 30, %actionLabel, "PlanetXLabelProfile", "left", "", 1.24);

	%btn = new GuiButtonCtrl()
	{
		Profile = "PlanetXButtonProfile";
		HorizSizing = "right";
		VertSizing = "bottom";
		Position = (%colX + 118) SPC %y;
		Extent = "180 32";
		Text = PlanetXGame.settings.keyLabel(PlanetXGame.settings.get(%prefKey));
		prefKey = %prefKey;
		actionLabel = %actionLabel;
	};
	%btn.Command = "PlanetXGame.playClick(); PlanetXGame.optionsScreen.captureKey(" @ %btn @ ");";

	%this.dialog.add(%btn);
	%this.keyButton[%prefKey] = %btn;
}

/// Raise the "press a key" overlay for a rebind button (which carries .prefKey and
/// .actionLabel). We build the modal overlay and the PlanetXKeyCapture control that
/// captures the next key; the control writes the pref, relabels the button, and
/// tears this overlay down when it finishes (keyCapture.cs).
function PlanetXOptionsScreen::captureKey(%this, %button)
{
	// The capture control wears PlanetXCaptureProfile, an Empty variant in the
	// theme with canKeyFocus and tab turned on. GuiInputCtrl only becomes the
	// keyboard first responder if its profile allows key focus
	// (GuiControl::setFirstResponder is gated on canKeyFocus, which is off in
	// every other profile), so without it the "press a key" prompt can never be
	// answered or dismissed.

	// Full-screen modal layer: blocks the options controls beneath and holds the
	// prompt. Transparent itself - the panel inside carries the visible box.
	%overlay = new GuiControl()
	{
		Profile = "PlanetXEmptyProfile";
		HorizSizing = "relative";
		VertSizing = "relative";
		Position = "0 0";
		Extent = "1024 768";
	};

	%panel = new GuiControl()
	{
		Profile = "PlanetXWindowProfile";
		HorizSizing = "center";
		VertSizing = "center";
		Position = "312 324";
		Extent = "400 120";
	};
	%overlay.add(%panel);

	%prompt = new GuiControl()
	{
		Profile = "PlanetXLabelProfile";
		FontSizeAdjust = 2;
		HorizSizing = "center";
		VertSizing = "center";
		Position = "20 24";
		Extent = "360 72";
		Text = "PRESS A KEY FOR" SPC %button.actionLabel;
		Align = "center";
		TextWrap = "1";
	};
	%panel.add(%prompt);

	// The capture control: invisible, full-screen, first responder on wake. It does
	// not render children, so the prompt panel above is a sibling, not a child. The
	// spawner sets the fields it controls; behavior lives in keyCapture.cs.
	%input = new GuiInputCtrl()
	{
		class = "PlanetXKeyCapture";
		Profile = "PlanetXCaptureProfile";
		HorizSizing = "relative";
		VertSizing = "relative";
		Position = "0 0";
		Extent = "1024 768";
		prefKey = %button.prefKey;
		overlay = %overlay;
	};
	%overlay.add(%input);

	Canvas.pushDialog(%overlay);
}

/// Pop and free a key-capture overlay. Scheduled from PlanetXKeyCapture::finish so
/// the delete runs a tick later, outside the capture control's own input callback
/// (the overlay owns that control, so freeing it mid-callback would be a use-after-
/// free the engine guards against).
function PlanetXOptionsScreen::closeCapture(%this, %overlay)
{
	if (!isObject(%overlay))
		return;

	Canvas.popDialog(%overlay);
	%overlay.delete();
}

/// The Mouse/Automatic aim toggle for a player.
function PlanetXOptionsScreen::buildAimToggle(%this, %colX, %y, %player)
{
	%this.addLabel(%colX + 8, %y, 100, 30, "AIM", "PlanetXLabelProfile", "left", "", 1.24);

	%btn = new GuiButtonCtrl()
	{
		Profile = "PlanetXButtonProfile";
		HorizSizing = "right";
		VertSizing = "bottom";
		Position = (%colX + 118) SPC %y;
		Extent = "180 32";
		Text = strupr(PlanetXGame.settings.get(%player @ "Aim"));
		Command = "PlanetXGame.playClick(); PlanetXGame.optionsScreen.toggleAim(\"" @ %player @ "\");";
	};
	%this.dialog.add(%btn);
	%this.aimButton[%player] = %btn;
}

//-----------------------------------------------------------------------------
// Live changes.
//-----------------------------------------------------------------------------

/// Flip a player's aim between mouse and automatic, relabel the button, and apply
/// it live if a level is running (aim mode was toggled from the pause menu).
function PlanetXOptionsScreen::toggleAim(%this, %player)
{
	%key = %player @ "Aim";
	%mode = (PlanetXGame.settings.get(%key) $= "mouse") ? "auto" : "mouse";

	PlanetXGame.settings.set(%key, %mode);
	%this.aimButton[%player].setText(strupr(%mode));

	if (isObject(PlanetXGame.level) && isObject(PlanetXGame.level.input))
		PlanetXGame.level.input.applyAimModes();
}

/// Reseed every control from the current prefs. Called on each open, because the
/// screen is a reused singleton and prefs may have changed since it was last shown.
function PlanetXOptionsScreen::refresh(%this)
{
	%this.slider["MasterVolume"].setValue(PlanetXGame.settings.get("MasterVolume"));
	%this.slider["MusicVolume"].setValue(PlanetXGame.settings.get("MusicVolume"));
	%this.slider["SoundVolume"].setValue(PlanetXGame.settings.get("SoundVolume"));

	%players = "P1" TAB "P2";
	%actions = "Up" TAB "Down" TAB "Left" TAB "Right" TAB "Fire";

	for (%p = 0; %p < getFieldCount(%players); %p++)
	{
		%player = getField(%players, %p);

		for (%i = 0; %i < getFieldCount(%actions); %i++)
		{
			%key = %player @ getField(%actions, %i);
			%this.keyButton[%key].setText(PlanetXGame.settings.keyLabel(PlanetXGame.settings.get(%key)));
		}

		%this.aimButton[%player].setText(strupr(PlanetXGame.settings.get(%player @ "Aim")));
	}
}
