//-----------------------------------------------------------------------------
// PlanetXKeyCapture: the "press a key" capture control for rebinding. The options
// screen raises one (PlanetXOptionsScreen::captureKey) when a rebind button is
// clicked. It is a GuiInputCtrl - the engine's raw-input control, which on wake
// mouse-locks and becomes first responder, then reports the next non-modifier press
// through onInputEvent(device, action, make) with strings ActionMap.bind accepts.
//
// The options screen (the spawner) sets the fields this control needs: the target
// pref key and the modal overlay the control lives in. On a key it writes the pref
// (swapping with any action that already holds that key), asks the options screen to
// repaint its labels, and tears the overlay down. GuiInputCtrl does not render its
// children, which is why the visible prompt is a sibling in the overlay, not a child
// of the control. See TORQUE_SCRIPT.md.
//-----------------------------------------------------------------------------

/// GuiInputCtrl callback. %make is "1" on press, "0" on a (modifier) release; bare
/// modifiers never arrive as a press, which suits us - the movement/fire map is
/// non-modifier keys.
function PlanetXKeyCapture::onInputEvent(%this, %device, %action, %make)
{
	if (%make !$= "1")
		return;

	// Escape cancels the rebind, leaving the binding unchanged.
	if (%action $= "escape")
	{
		%this.finish();
		return;
	}

	// Only keyboard keys are bindable (the map is built with bind("keyboard", ...)).
	if (%device !$= "keyboard")
		return;

	%settings = PlanetXGame.settings;

	// If another action already holds this key, SWAP: hand that action the key we're
	// replacing, so no two actions ever share one key.
	%conflict = %settings.actionForKey(%action, %this.prefKey);
	if (%conflict !$= "")
		%settings.set(%conflict, %settings.get(%this.prefKey));

	%settings.set(%this.prefKey, %action);
	$PlanetX::bindingsDirty = true;

	// Repaint every binding label - this button and any swapped one.
	PlanetXGame.optionsScreen.refresh();

	// Apply immediately if a level is live (rebinding from the pause menu); from the
	// title there is no map yet - the next level builds it from these prefs.
	if (isObject(PlanetXGame.level) && isObject(PlanetXGame.level.input))
		PlanetXGame.level.input.rebuildMoveMap();

	%this.finish();
}

/// Tear the overlay down NEXT tick and from the OPTIONS SCREEN, not from here: we
/// are inside this control's own onInputEvent, and the overlay owns this control, so
/// popping/deleting it synchronously (or scheduling the delete on the overlay
/// itself) frees %this while its callback is still on the stack - which trips the
/// engine's "deleted whilst performing a script callback" guard.
function PlanetXKeyCapture::finish(%this)
{
	PlanetXGame.optionsScreen.schedule(1, "closeCapture", %this.overlay);
}
