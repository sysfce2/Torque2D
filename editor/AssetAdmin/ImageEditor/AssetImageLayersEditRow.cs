
//-----------------------------------------------------------------------------
// One layer of a composed image: its picture, where it sits, and the color it is
// tinted with.
//
// Row 0 is not a layer the user added -- it is the asset's own image, the base
// that every other row is drawn onto, and its tint is the asset's BlendColor.
// The engine synthesizes it the moment a first real layer is added
// (ImageAsset::insertLayer), which is why its image and offset boxes are inert:
// they belong to the asset, not to a layer.
//
// Its color is inert too, but only while it is alone. ImageAsset::setBlendColor
// stores the value, warns to the console and returns before redrawing anything
// when there are no layers -- so with nothing composed onto the base there is
// nothing for a tint to show up on. Once a layer exists the picker comes to
// life.
//
// A locked picker wears a padlock rather than simply refusing to open. Every
// other inert control in the editor shows it -- a greyed text box plainly is one
// -- and a swatch does not: GuiColorPopupCtrl::onRender fills its face with the
// color whatever state the control is in, so an inert swatch is pixel for pixel
// a live one. The padlock is drawn at 30% black over a base color that is white
// until somebody has a reason to change it.
//-----------------------------------------------------------------------------

// Where the color column sits, and how big the padlock over it is.
$AssetImageLayersEditRow::colorX = 392;
$AssetImageLayersEditRow::colorWidth = 164;
$AssetImageLayersEditRow::lockSize = 16;
$AssetImageLayersEditRow::lockTint = "0 0 0 77";

function AssetImageLayersEditRow::onAdd(%this)
{
	%this.errorColor = "255 0 0 255";
	%this.indexBox = new GuiControl()
	{
		HorizSizing="width";
		VertSizing="height";
		Position="0 0";
		Extent="20 40";
		Align = center;
		vAlign = middle;
		Text = %this.LayerIndex;
		FontSizeAdjust = 1.4;
		FontColor = %this.errorColor;
	};
	ThemeManager.setProfile(%this.indexBox, "codeProfile");
	%this.add(%this.indexBox);

	%this.imageBox = new GuiTextEditCtrl()
	{
		HorizSizing="width";
		VertSizing="height";
		Position="20 3";
		Extent="200 32";
		Text = %this.LayerImage;
		AltCommand = %this.getID() @ ".LayerImageChange();";
		FontColor = %this.errorColor;
		InputMode = "AllText";
	};
	ThemeManager.setProfile(%this.imageBox, "textEditProfile");
	%this.add(%this.imageBox);

	%this.offsetXBox = new GuiTextEditCtrl()
	{
		HorizSizing="width";
		VertSizing="height";
		Position="224 3";
		Extent="80 32";
		Align = right;
		Text = getWord(%this.LayerPosition, 0);
		AltCommand = %this.getID() @ ".LayerPositionXChange();";
		FontColor = %this.errorColor;
		InputMode = "Number";
	};
	ThemeManager.setProfile(%this.offsetXBox, "textEditProfile");
	%this.add(%this.offsetXBox);

	%this.offsetYBox = new GuiTextEditCtrl()
	{
		HorizSizing="width";
		VertSizing="height";
		Position="308 3";
		Extent="80 32";
		Align = right;
		Text = getWord(%this.LayerPosition, 1);
		AltCommand = %this.getID() @ ".LayerPositionYChange();";
		FontColor = %this.errorColor;
		InputMode = "Number";
	};
	ThemeManager.setProfile(%this.offsetYBox, "textEditProfile");
	%this.add(%this.offsetYBox);

	%this.LayerColor = %this.scrubColor(%this.LayerColor);
	%this.buildColorColumn();

	%this.buttonBar = new GuiChainCtrl()
	{
		Class = "EditorButtonBar";
		Position = "564 5";
		Extent = "0 24";
		ChildSpacing = 4;
		IsVertical = false;
		Tool = %this;
	};
	ThemeManager.setProfile(%this.buttonBar, "emptyProfile");
	%this.add(%this.buttonBar);

	if(%this.LayerIndex > 0)
	{
		%this.buttonBar.addButton("MoveLayerUp", $EditorIcon::arrow_top, "Move Layer Up", "getMoveLayerUpEnabled");
		%this.buttonBar.addButton("MoveLayerDown", $EditorIcon::arrow_bottom, "Move Layer Down", "getMoveLayerDownEnabled");
		%this.buttonBar.addButton("RemoveLayer", $EditorIcon::round_delete, "Remove Layer", "");
	}
	else
	{
		%this.imageBox.active = false;
		%this.offsetXBox.active = false;
		%this.offsetYBox.active = false;
	}

	%this.refreshColorLock();
}

//-----------------------------------------------------------------------------
// The color column: a swatch, and a padlock over it for when the swatch is not
// yet worth using.
//
// A picker rather than the four numbers it used to be. The numbers are still
// there -- showColorValues puts an R/G/B/A row inside the popup, so an exact
// value can still be typed -- but a color is a thing you look at, and four
// decimals in a text box is the one form in which you cannot.
//
// Both live in a container of their own so the padlock can be centred on the
// swatch rather than on the row: "center" sizing recentres a control in its
// PARENT, and with the row as the parent the padlock would sit in the middle of
// the row instead of over the color.
//-----------------------------------------------------------------------------

function AssetImageLayersEditRow::buildColorColumn(%this)
{
	%w = $AssetImageLayersEditRow::colorWidth;
	%h = 32;
	%lock = $AssetImageLayersEditRow::lockSize;

	%this.colorArea = new GuiControl()
	{
		HorizSizing = "width";
		VertSizing = "height";
		Position = $AssetImageLayersEditRow::colorX SPC 3;
		Extent = %w SPC %h;
	};
	ThemeManager.setProfile(%this.colorArea, "emptyProfile");
	%this.add(%this.colorArea);

	%this.colorBox = new GuiColorPopupCtrl()
	{
		HorizSizing = "width";
		VertSizing = "height";
		Position = "0 0";
		Extent = %w SPC %h;
		showColorValues = true;
	};
	ThemeManager.setProfile(%this.colorBox, "colorPickerProfile");
	ThemeManager.setProfile(%this.colorBox, "emptyProfile", "backgroundProfile");
	ThemeManager.setProfile(%this.colorBox, "colorPopupProfile", "popupProfile");
	ThemeManager.setProfile(%this.colorBox, "emptyProfile", "pickerProfile");
	ThemeManager.setProfile(%this.colorBox, "colorPickerSelectorProfile", "selectorProfile");
	ThemeManager.setProfile(%this.colorBox, "textEditProfile", "valueProfile");
	// Passed on to the popup's R/G/B/A boxes, which name their channel with a
	// tooltip; without it each would fall back to a profile of its own.
	ThemeManager.setProfile(%this.colorBox, "tipProfile", "TooltipProfile");
	%this.colorBox.Command = %this.getID() @ ".LayerColorChange();";
	%this.colorArea.add(%this.colorBox);

	%this.colorBox.setColorF(%this.LayerColor);

	// Drawn after the swatch, so it draws over it, and deaf to input so a click
	// aimed at the swatch is not swallowed by the thing explaining why the swatch
	// will not respond.
	%this.lockIcon = new GuiSpriteCtrl()
	{
		HorizSizing = "center";
		VertSizing = "center";
		Position = ((%w - %lock) / 2) SPC ((%h - %lock) / 2);
		Extent = %lock SPC %lock;
		MinExtent = %lock SPC %lock;
		Image = "EditorCore:EditorIcons16";
		ImageSize = "16 16";
		constrainProportions = "1";
		fullSize = "0";
		Frame = $EditorIcon::padlock_closed;
		UseInput = false;
		Visible = false;
	};
	ThemeManager.setProfile(%this.lockIcon, "spriteProfile");
	%this.colorArea.add(%this.lockIcon);

	// Not the theme's label color, which is what every other icon in the editor
	// takes: this one is read against the color being edited rather than against
	// the panel, and the base is white until somebody has a reason to change it.
	%this.lockIcon.setImageColor($AssetImageLayersEditRow::lockTint);
}

// Row 0 alone is the asset's own image with nothing composed onto it, and a tint
// on that shows up nowhere. Every other row, and row 0 once it has company, is
// live.
function AssetImageLayersEditRow::refreshColorLock(%this)
{
	%this.setColorLocked(%this.LayerIndex == 0 && %this.LayerCount == 0);
}

function AssetImageLayersEditRow::setColorLocked(%this, %locked)
{
	%this.colorLocked = %locked;
	%this.colorBox.setActive(!%locked);
	%this.lockIcon.setVisible(%locked);

	// The tooltip still reaches an inactive control -- the hit test does not ask
	// whether a control is active -- so this is where the reason goes.
	%this.colorBox.Tooltip = %locked
		? "This tints the base image that layers are drawn onto, and there are no layers yet. Add one and this unlocks."
		: "";
}

function AssetImageLayersEditRow::LayerImageChange(%this)
{
	%name = %this.imageBox.getText();
	%name = stripChars(%name, " ");
	%this.imageBox.setText(%name);
	if(%name !$= %this.LayerImage)
	{
		if(%name $= "")
		{
			%this.setNameError(true);
		}
		else
		{
			%this.setNameError(false);
			%this.postEvent("LayerImageChange", %this SPC %name);
		}
	}
}

function AssetImageLayersEditRow::LayerPositionXChange(%this)
{
	%x = %this.offsetXBox.getText();
	%x = stripChars(%x, " ");
	if(%x $= "")
	{
		%x = 0;
	}
	%this.offsetXBox.setText(%x);

	if(%x !$= getWord(%this.LayerPosition, 0))
	{
		%this.postEvent("LayerPositionChange", %this SPC %x SPC getWord(%this.LayerPosition, 1));
	}
}

function AssetImageLayersEditRow::LayerPositionYChange(%this)
{
	%y = %this.offsetYBox.getText();
	%y = stripChars(%y, " ");
	if(%y $= "")
	{
		%y = 0;
	}
	%this.offsetYBox.setText(%y);

	if(%y !$= getWord(%this.CellOffset, 1))
	{
		%this.postEvent("LayerPositionChange", %this SPC getWord(%this.LayerPosition, 0) SPC %y);
	}
}

// getColorF, not getText: a layer's tint is a ColorF, so its four numbers run 0
// to 1 and reading them off a ColorI swatch would round every one but white to
// black.
function AssetImageLayersEditRow::LayerColorChange(%this)
{
	%color = %this.scrubColor(%this.colorBox.getColorF());

	if(%color !$= %this.LayerColor)
	{
		%this.postEvent("LayerColorChange", %this SPC %color);
	}
}

function AssetImageLayersEditRow::setNameError(%this, %hasError)
{
	%this.imageBox.overrideFontColor = %hasError;
	%this.indexBox.overrideFontColor = %hasError;
}

function AssetImageLayersEditRow::getMoveLayerUpEnabled(%this)
{
	return %this.LayerIndex != 1;
}

function AssetImageLayersEditRow::getMoveLayerDownEnabled(%this)
{
	return %this.LayerIndex != %this.LayerCount;
}

function AssetImageLayersEditRow::getRemoveCellEnabled(%this)
{
	return true;
}

// Sent to every row whenever a layer is added or removed, which is also the only
// thing that can lock or unlock row 0's color.
function AssetImageLayersEditRow::updateLayerCount(%this, %newCount)
{
	%this.LayerCount = %newCount;
	%this.buttonBar.refreshEnabled();
	%this.refreshColorLock();
}

function AssetImageLayersEditRow::MoveLayerUp(%this)
{
	%this.postEvent("swapLayers", (%this.LayerIndex - 1) SPC %this.LayerIndex);
}

function AssetImageLayersEditRow::MoveLayerDown(%this)
{
	%this.postEvent("swapLayers", %this.LayerIndex SPC (%this.LayerIndex + 1));
}

function AssetImageLayersEditRow::RemoveLayer(%this)
{
	%this.postEvent("removeLayer", %this.LayerIndex);
}

function AssetImageLayersEditRow::refresh(%this)
{
	%this.indexBox.setText(%this.LayerIndex);
	%this.imageBox.setText(%this.LayerImage);
	%this.offsetXBox.setText(getWord(%this.LayerPosition, 0));
	%this.offsetYBox.setText(getWord(%this.LayerPosition, 1));
	%this.colorBox.setColorF(%this.LayerColor);
	%this.refreshColorLock();
}

function AssetImageLayersEditRow::onRemove(%this)
{
	%this.deleteObjects();
}

function AssetImageLayersEditRow::scrubColor(%this, %color)
{
	%red = %this.scrubChannel(getWord(%color, 0));
	%green = %this.scrubChannel(getWord(%color, 1));
	%blue = %this.scrubChannel(getWord(%color, 2));
	%alpha = %this.scrubChannel(getWord(%color, 3));

	return %red SPC %green SPC %blue SPC %alpha;
}

function AssetImageLayersEditRow::scrubChannel(%this, %val)
{
	%val = mFloatLength(mClamp(%val, 0, 1), 3);
	if(getSubStr(%val, 4, 1) !$= "0")
	{
		return %val;
	}
	%val = getSubStr(%val, 0, 4);

	if(getSubStr(%val, 3, 1) !$= "0")
	{
		return %val;
	}
	%val = getSubStr(%val, 0, 3);

	if(getSubStr(%val, 2, 1) !$= "0")
	{
		return %val;
	}
	return getSubStr(%val, 0, 1);
}
