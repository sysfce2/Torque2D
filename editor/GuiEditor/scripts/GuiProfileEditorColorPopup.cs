
//-----------------------------------------------------------------------------
// The color popup the Profile Editor puts on every color field: an ordinary
// GuiColorPopupCtrl whose swatch row holds the six colors of whichever theme is
// selected in the tree. Setting a fill color to the theme's accent is then a
// click, rather than a hunt around the color wheel for something close.
//
// The swatches are filled when the popup opens, not when it is built, because
// the theme in play changes as the user moves around the tree while the same
// widgets stay on screen. A stand-alone profile belongs to no theme, so it gets
// no swatches and the row does not appear at all.
//-----------------------------------------------------------------------------

// The six theme colors, in the order the theme form lists them.
function GuiProfileEditorColorPopup::themeColorFields(%this)
{
	return "colorBackground colorSurface colorForeground colorAccent colorHighlight colorWarning";
}

function GuiProfileEditorColorPopup::onOpen(%this)
{
	%this.fillThemeSwatches();
}

// Replace the swatch row with the selected theme's colors, or empty it when
// there is no theme to take them from.
function GuiProfileEditorColorPopup::fillThemeSwatches(%this)
{
	%this.clearSwatches();

	%theme = %this.currentTheme();
	if(!isObject(%theme))
	{
		return;
	}

	%fields = %this.themeColorFields();
	%count = getWordCount(%fields);
	for(%i = 0; %i < %count; %i++)
	{
		%this.addSwatchI(%theme.getFieldValue(getWord(%fields, %i)));
	}
}

// The theme whose colors belong in the swatch row. The dialog's currentRoot is a
// GuiProfileTheme for every node kind except a stand-alone profile, where it is
// the profile itself -- hence the class check rather than a bare isObject.
function GuiProfileEditorColorPopup::currentTheme(%this)
{
	if(!isObject(GuiEditor) || !isObject(GuiEditor.profileEditorDialog))
	{
		return "";
	}

	%root = GuiEditor.profileEditorDialog.currentRoot;
	if(!isObject(%root) || %root.getClassName() !$= "GuiProfileTheme")
	{
		return "";
	}

	return %root;
}
