
//-----------------------------------------------------------------------------
// One of the color graph's three channel buttons: a swatch that stays pressed.
//
// An EditorToggleIcon with one thing changed. The stock toggle tints its icon
// with the editor's own inks, bright for on and dim for off, which is right for a
// switch -- but these three buttons stand for red, green and blue, so the color
// IS the label. A themed ink would say which button is pressed and nothing about
// which curve it belongs to.
//
// So the tint is the channel's own hue, matched to what GuiEditParticleColorGraph
// draws that channel's curve in: full strength when the channel is live, faded
// when it is not, exactly as the curve is. The button and the line it controls
// are then visibly the same thing.
//
// Radio behavior belongs to the owner, not here: a checkbox flips itself, so
// clicking the live channel would switch it off. AssetParticleColorGraphUnit
// puts it back.
//
// The creator sets channel, owner, and tipOff inline.
//-----------------------------------------------------------------------------

function AssetParticleChannelToggle::getIconTint(%this, %on)
{
	if(!%this.isActive())
	{
		return ThemeManager.activeTheme.iconButtonProfile.fontColorNA;
	}

	// Lifted off the primaries for the same reason the curves are: a pure blue
	// swatch on a dark panel is close to unreadable. These stay unmistakably red,
	// green and blue on every editor theme.
	switch$(%this.channel)
	{
		case "Green":
			%color = %on ? "90 220 110 255" : "90 220 110 130";

		case "Blue":
			%color = %on ? "105 155 255 255" : "105 155 255 130";

		default:
			%color = %on ? "255 95 95 255" : "255 95 95 130";
	}

	return %color;
}
