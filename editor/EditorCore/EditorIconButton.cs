
// The button, and the picture on it, are both sizeable now.
//
// They have to be said as fields rather than set afterwards, because this
// forces its own extent here and the hover handlers below animate the icon to
// numbers of their own -- so anything a caller set was undone by onAdd, and then
// undone again by the first hover.
//
// iconSize is the size of the PICTURE, not of the sprite control holding it, and
// keeping those two apart is the whole trick here.
//
// GuiSpriteCtrl::growTo animates mImageSize -- what is drawn -- and leaves the
// control alone. The sprite then clamps its picture to its own content rect, so
// the control has to be bigger than the biggest the picture will ever grow to or
// the hover is clipped. Conflating the two gave a 36 pixel button a picture that
// animated 32 down to 28: it looked like the icon exploded on hover and never
// went back.
//
// Defaults reproduce the numbers this button has always used: a 24 button, a 20
// sprite, a 16 picture that goes to 18 under the pointer.
$EditorIconButton::defaultButtonSize = 24;
$EditorIconButton::defaultIconSize = 16;

// Room around the picture for it to grow into without being clamped.
$EditorIconButton::iconSlack = 4;
$EditorIconButton::hoverGrowth = 2;

function EditorIconButton::onAdd(%this)
{
	%this.text = "";

	%buttonSize = (%this.buttonSize $= "") ? $EditorIconButton::defaultButtonSize : %this.buttonSize;
	%this.iconSize = (%this.iconSize $= "") ? $EditorIconButton::defaultIconSize : %this.iconSize;

	%this.extent = %buttonSize SPC %buttonSize;

	// The container, sized to hold the picture at its hovered size with room to
	// spare. Never equal to the picture: the sprite clamps to its content rect.
	%holder = %this.iconSize + $EditorIconButton::iconSlack;

	%this.icon = new GuiSpriteCtrl()
	{
		HorizSizing="center";
		VertSizing="center";
		Extent = %holder SPC %holder;
		minExtent = %holder SPC %holder;
		Position = "0 0";
		constrainProportions = "1";
		fullSize = "0";
		Image = "EditorCore:EditorIcons16";
		ImageSize = %this.iconSize SPC %this.iconSize;
		ImageColor = ThemeManager.activeTheme.iconButtonProfile.FontColor;
		Frame = %this.frame;
		Tooltip = %this.Tooltip;
		UseInput = false;
	};
	ThemeManager.setProfile(%this.icon, "spriteProfile");
	%this.add(%this.icon);

	%this.startListening(ThemeManager);
	if(%this.Tooltip !$= "")
	{
		ThemeManager.setProfile(%this, "tipProfile", "TooltipProfile");
	}
}

// Every hover and press handler below is guarded on isActive(), because the
// engine still delivers touch events to a disabled control: GuiControl::
// findHitControl tests mVisible and mUseInput and never mActive. Without the
// guard, moving the pointer over a disabled button repainted its icon in the
// enabled hover color and the disabled look was gone until something else
// forced it back -- the button read as clickable when it was not.
function EditorIconButton::onTouchEnter(%this)
{
	if(!%this.isActive())
	{
		return;
	}
	%this.icon.fadeTo(ThemeManager.activeTheme.iconButtonProfile.fontColorHL, 200, "EaseInOut");

	%hover = %this.iconSize + $EditorIconButton::hoverGrowth;
	%this.icon.growTo(%hover SPC %hover, 200, "EaseInOut");
}

function EditorIconButton::onTouchLeave(%this)
{
	if(!%this.isActive())
	{
		return;
	}
	%this.icon.fadeTo(ThemeManager.activeTheme.iconButtonProfile.fontColor, 200, "EaseInOut");
	%this.icon.growTo(%this.iconSize SPC %this.iconSize, 200, "EaseInOut");
}

function EditorIconButton::onTouchDown(%this)
{
	if(!%this.isActive())
	{
		return;
	}
	%this.icon.fadeTo(ThemeManager.activeTheme.iconButtonProfile.fontColorSL, 200, "EaseInOut");
}

function EditorIconButton::onTouchUp(%this)
{
	if(!%this.isActive())
	{
		return;
	}
	%this.icon.fadeTo(ThemeManager.activeTheme.iconButtonProfile.fontColorHL, 200, "EaseInOut");
}

function EditorIconButton::onActive(%this)
{
	%this.icon.fadeTo(ThemeManager.activeTheme.iconButtonProfile.fontColor, 200, "EaseInOut");
}

function EditorIconButton::onInactive(%this)
{
	%this.icon.fadeTo(ThemeManager.activeTheme.iconButtonProfile.fontColorNA, 200, "EaseInOut");
}

function EditorIconButton::onThemeChange(%this, %theme)
{
	if(%this.active)
	{
		%this.icon.setImageColor(ThemeManager.activeTheme.iconButtonProfile.fontColor);
	}
	else
	{
		%this.icon.setImageColor(ThemeManager.activeTheme.iconButtonProfile.fontColorNA);
	}
}

function EditorIconButton::onRemove(%this)
{
	if(isObject(%this.icon))
	{
		%this.icon.delete();
	}
}
