
//-----------------------------------------------------------------------------
// An icon that stays pressed: a toggle button, built from a GuiCheckBoxCtrl.
//
// A checkbox already IS a toggle. GuiCheckBoxCtrl::onAction flips mStateOn and
// then runs the Command, and it refuses to do either while the control is
// inactive -- which is the whole contract, and more than GuiButtonCtrl offers.
// What makes it look like a checkbox rather than a button is only its layout:
// a small box beside a caption. Take the caption away and let the box have the
// whole control and the same class renders as a button that holds its state:
//
//     boxOffset  "0 0"      the box starts at the content rect's corner
//     boxExtent  the extent GuiCheckBoxCtrl clamps it to the content rect
//     text       ""         and textExtent "0 0", so no caption is drawn
//
// GuiCheckBoxCtrl::renderInnerControl then draws a universal rect for the whole
// control in the current state, and an icon sitting on top (UseInput off, so it
// never eats the click) says what the button is for.
//
// The icon carries the on/off reading rather than the background, because a
// profile cannot express it without art: GuiControlProfile::getFillColor maps
// NormalStateOn to mFillColor, the same color as NormalState, so the four "On"
// states differ only when the profile renders from an image or bitmap. Bright
// icon means on, dim means off, and a second frame can say it again where there
// is art for one (a closed and an open padlock).
//
// There is deliberately no hover animation. EditorIconButton has one and it is
// where its disabled state went: the engine delivers touch events to inactive
// controls (findHitControl checks mVisible and mUseInput, never mActive), so
// the hover handler repainted over the disabled color. Here the state is
// computed in one place, refresh(), and nothing else writes the icon.
//
// The creator sets frameOn, frameOff, tipOn, tipOff, owner and toggleName
// inline. Clicks arrive at owner.onToggleIconChanged(%this).
//-----------------------------------------------------------------------------

function EditorToggleIcon::onAdd(%this)
{
	// Field assignment, not setBoxOffset/setBoxExtent: those bindings document
	// one argument and read two (argv[2] and argv[3]), so a single "0 0" string
	// leaves whatever happened to be in the second slot. The fields themselves
	// are TypePoint2I and parse the pair correctly.
	%extent = %this.getExtent();

	%this.setText("");
	%this.boxOffset = "0 0";
	%this.boxExtent = %extent;
	%this.textOffset = "0 0";
	%this.textExtent = "0 0";

	// 16 is the size the segmented rows and the header panes have always drawn at.
	// A creator sitting a toggle beside an EditorIconButton wants 20, which is
	// what that one uses -- same 24 x 24 button, a visibly bigger picture on it,
	// and a row of the two together looked mismatched until this could be said.
	%iconSize = (%this.iconSize $= "") ? 16 : %this.iconSize;

	%this.icon = new GuiSpriteCtrl()
	{
		HorizSizing = "center";
		VertSizing = "center";
		Extent = %iconSize SPC %iconSize;
		MinExtent = %iconSize SPC %iconSize;
		Position = "0 0";
		Image = "EditorCore:EditorIcons16";
		ImageSize = "16 16";
		constrainProportions = "1";
		fullSize = "0";
		Frame = %this.frameOff;
		UseInput = false;
	};
	ThemeManager.setProfile(%this.icon, "spriteProfile");
	%this.add(%this.icon);

	// ThemeManager repaints what it was given a profile for, and the icon's tint
	// is not that: refresh() COPIES a color off the profile onto the sprite, so
	// swapping the profile underneath leaves the copy behind. Without this the
	// button's background changed theme and the picture on it did not, until
	// something happened to call refresh() again -- which for a segmented row
	// meant the icons corrected themselves the moment you clicked one.
	%this.startListening(ThemeManager);

	%this.Command = %this.getID() @ ".onToggled();";
	%this.refresh();
}

function EditorToggleIcon::onThemeChange(%this, %theme)
{
	%this.refresh();
}

// GuiCheckBoxCtrl has already flipped mStateOn by the time the Command runs, so
// the owner is told what the value became rather than being asked to work it
// out.
function EditorToggleIcon::onToggled(%this)
{
	%this.refresh();

	if(isObject(%this.owner))
	{
		%this.owner.onToggleIconChanged(%this);
	}
}

// Set the state without telling the owner, for loading a value in.
function EditorToggleIcon::setValue(%this, %on)
{
	%this.setStateOn(%on);
	%this.refresh();
}

function EditorToggleIcon::getValue(%this)
{
	return %this.getStateOn();
}

// The single place the icon's look is decided: which frame, which tooltip, and
// which of the profile's font colors tints it.
function EditorToggleIcon::refresh(%this)
{
	%on = %this.getStateOn();
	%profile = ThemeManager.activeTheme.iconButtonProfile;

	// frameOn is optional. Where there is only one icon for the idea, the tint
	// alone carries the state.
	%frame = (%on && %this.frameOn !$= "") ? %this.frameOn : %this.frameOff;

	// So is frameOff. A button with no icon at all is a deliberate shape: it is
	// how EditorChoiceRow spells the "unset" end of a segmented control,
	// where the absence of a picture is the meaning.
	%this.icon.setVisible(%frame !$= "");
	if(%frame !$= "")
	{
		%this.icon.setImageFrame(%frame);
	}

	if(!%this.isActive())
	{
		%this.icon.setImageColor(%profile.fontColorNA);
	}
	else
	{
		%this.icon.setImageColor(%on ? %profile.fontColorHL : %profile.fontColor);
	}

	%this.Tooltip = %this.buildTip(%on);
}

// Two lines, now that a control's text can hold a line break: what this is and
// which way it is set, then what that means.
//
//     Visible - On
//     Draws when the game runs...
//
// The first line only appears for a toggle that names itself. A segmented row's
// buttons are choices rather than switches -- "Centre text - On" would be a
// worse caption than "Centre text" -- so those pass no label and keep the one
// line they had.
function EditorToggleIcon::buildTip(%this, %on)
{
	%tip = %on ? %this.tipOn : %this.tipOff;

	if(%this.toggleLabel $= "")
	{
		return %tip;
	}

	%heading = %this.toggleLabel @ " - " @ (%on ? "On" : "Off");
	return (%tip $= "") ? %heading : (%heading NL %tip);
}

// setActive does not repaint on its own, and the disabled tint is ours to draw.
function EditorToggleIcon::onActive(%this)
{
	%this.refresh();
}

function EditorToggleIcon::onInactive(%this)
{
	%this.refresh();
}
