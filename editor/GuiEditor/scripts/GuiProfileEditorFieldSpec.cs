
//-----------------------------------------------------------------------------
// The context model behind the Gui Profile Editor's profile pane: which of a
// GuiControlProfile's fields actually matter for the control the profile is
// made for.
//
// A profile already records its purpose in its "category" field -- one of the
// engine-defined names in GuiProfileTheme::smProfileCategories[] -- so that is
// the only filter input. Each category is described by three things:
//
//   textClass  How the category's control draws text with this profile:
//                full    through GuiControl::renderText -- every font field
//                        applies, including align/vAlign/textOffset (those are
//                        read nowhere else; see guiControl.cc renderText).
//                direct  through dglDrawText without renderText, so the face,
//                        size and colors apply but the layout fields do not.
//                glyph   no text at all, but getFontColor(state) tints a drawn
//                        icon (scroll-bar arrows, window title-bar buttons).
//                none    never draws text or reads a font color.
//   states     Which of normal/HL/SL/NA the control actually passes to
//              renderUniversalRect, as four flags. Unreachable states are
//              greyed rather than hidden, so a value is never lost.
//   flags      focus      tab + canKeyFocus mean something here.
//              caretBeam  cursorColor is the text caret.
//              caretRect  cursorColor is the keyboard-focus rectangle.
//              textsel    fillColorTextSL + fontColorTextSL are used.
//              circle     the control draws a circle or ring, which reads only
//                         the default border -- the four sides do nothing.
//              alignOn    keep "align" even though the class would drop it.
//              alignOff   drop "align" even though the class would keep it.
//
// Fields absent from the seven groups below are never shown at all, even with
// Show All: SimObject plumbing (class, superclass, hidden, locked, parentGroup,
// internalName, canSaveDynamicFields), theme bookkeeping (category,
// themeOverrides), the border references the Borders pane owns, fontColors0-6,
// which the inspector's array expansion duplicates from the seven named aliases,
// and fontDirectory, which the editor owns: every project keeps its font caches
// in one folder (GuiProfileEditorLibrary::getFontsPath), so there is nothing for
// a profile to decide.
//
// The spec is pure data with no UI; the pane owns one and asks it what to show.
//-----------------------------------------------------------------------------

function GuiProfileEditorFieldSpec::onAdd(%this)
{
	// category TAB textClass TAB states TAB flags. States read normal, HL, SL,
	// NA. Every entry is derived from the control's render path, not from the
	// theme recipe -- a recipe stamps far more than its control ever reads.
	%table =
		"Empty"              TAB "none"   TAB "1000" TAB ""                             NL
		"Tooltip"            TAB "direct" TAB "1000" TAB ""                             NL
		"Panel"              TAB "none"   TAB "1000" TAB ""                             NL
		"Button"             TAB "full"   TAB "1111" TAB "focus"                        NL
		"CheckBox"           TAB "full"   TAB "1111" TAB "focus caretRect"              NL
		"Radio"              TAB "full"   TAB "1111" TAB "focus caretRect circle"       NL
		"Label"              TAB "full"   TAB "1000" TAB ""                             NL
		"TextEdit"           TAB "full"   TAB "1111" TAB "focus caretBeam textsel"      NL
		"Scroll"             TAB "none"   TAB "1000" TAB ""                             NL
		"ScrollTrack"        TAB "none"   TAB "1001" TAB ""                             NL
		"ScrollThumb"        TAB "none"   TAB "1110" TAB ""                             NL
		"ScrollArrow"        TAB "glyph"  TAB "1111" TAB ""                             NL
		"TabBook"            TAB "none"   TAB "1000" TAB ""                             NL
		"Tab"                TAB "full"   TAB "1111" TAB ""                             NL
		"TabPage"            TAB "none"   TAB "1000" TAB ""                             NL
		"ListBox"            TAB "full"   TAB "1111" TAB "focus"                        NL
		"DropDown"           TAB "full"   TAB "1111" TAB "focus caretRect"              NL
		"DropDownItem"       TAB "full"   TAB "1111" TAB ""                             NL
		"Window"             TAB "full"   TAB "1111" TAB ""                             NL
		"WindowContent"      TAB "none"   TAB "1111" TAB ""                             NL
		"WindowButton"       TAB "glyph"  TAB "1111" TAB "alignOn"                      NL
		"WindowCloseButton"  TAB "glyph"  TAB "1111" TAB "alignOn"                      NL
		"MenuBar"            TAB "none"   TAB "1000" TAB "focus"                        NL
		"Menu"               TAB "full"   TAB "1111" TAB ""                             NL
		"MenuItem"           TAB "full"   TAB "1111" TAB "alignOff"                     NL
		"MenuContent"        TAB "none"   TAB "1000" TAB ""                             NL
		"Overlay"            TAB "none"   TAB "1000" TAB ""                             NL
		"Progress"           TAB "full"   TAB "1110" TAB ""                             NL
		"TreeView"           TAB "full"   TAB "1111" TAB "focus"                        NL
		"FrameSet"           TAB "none"   TAB "1111" TAB ""                             NL
		"FrameSetDropButton" TAB "none"   TAB "1110" TAB ""                             NL
		"ColorPicker"        TAB "full"   TAB "1111" TAB "focus"                        NL
		"ColorSelector"      TAB "none"   TAB "1110" TAB "circle"                       NL
		"ColorPopup"         TAB "none"   TAB "1000" TAB ""                             NL
		"DragAndDrop"        TAB "full"   TAB "1000" TAB ""                             NL
		"Slider"             TAB "direct" TAB "1000" TAB ""                             NL
		"SliderThumb"        TAB "none"   TAB "1110" TAB "";

	%this.categoryNames = "";
	%count = getRecordCount(%table);
	for(%i = 0; %i < %count; %i++)
	{
		%rec = getRecord(%table, %i);
		%name = trim(getField(%rec, 0));

		// "class" is a real SimObject field, so the table uses textClass.
		%this.textClass[%name] = trim(getField(%rec, 1));
		%this.liveStates[%name] = trim(getField(%rec, 2));
		%this.flags[%name] = trim(getField(%rec, 3));

		%this.categoryNames = (%this.categoryNames $= "") ? %name : (%this.categoryNames TAB %name);
	}
}

//-----------------------------------------------------------------------------
// The field groups. Membership of one of these is what makes a field showable
// at all; the predicates below decide whether the current category wants it.
//-----------------------------------------------------------------------------

// Shown for every category: every profile renders through renderUniversalRect,
// which takes the image path before the fill path, so images always apply.
function GuiProfileEditorFieldSpec::universalFields(%this)
{
	return "fillColor fillColorHL fillColorSL fillColorNA imageAsset bitmap";
}

// The typeface itself: needed by anything that rasterizes glyphs.
function GuiProfileEditorFieldSpec::fontFaceFields(%this)
{
	return "fontType fontSize fontCharset";
}

// Read only inside GuiControl::renderText, so only the "full" class needs them.
function GuiProfileEditorFieldSpec::textLayoutFields(%this)
{
	return "align vAlign textOffset";
}

// The four state colors, used for glyphs and for icons drawn in getFontColor.
function GuiProfileEditorFieldSpec::fontColorFields(%this)
{
	return "fontColor fontColorHL fontColorSL fontColorNA";
}

// Reachable only as \c4, \c5 and \c7-\c9 escapes inside drawn text (the color
// table dglDrawText receives), so they need a category that draws glyphs.
function GuiProfileEditorFieldSpec::richTextFields(%this)
{
	return "fontColorLink fontColorLinkHL fontColors7 fontColors8 fontColors9";
}

function GuiProfileEditorFieldSpec::focusFields(%this)
{
	return "tab canKeyFocus";
}

// The text-selection pair, whose only consumer is GuiTextEditCtrl.
function GuiProfileEditorFieldSpec::textSelectionFields(%this)
{
	return "fillColorTextSL fontColorTextSL";
}

// Every field the pane can ever show, in section order.
function GuiProfileEditorFieldSpec::allFields(%this)
{
	return %this.universalFields() SPC %this.fontFaceFields() SPC
		%this.textLayoutFields() SPC %this.fontColorFields() SPC
		%this.richTextFields() SPC %this.focusFields() SPC
		%this.textSelectionFields() SPC "cursorColor";
}

// Space-delimited membership. Wrapping both sides in spaces keeps fontColor
// from matching inside fontColorHL.
function GuiProfileEditorFieldSpec::listHas(%this, %list, %item)
{
	return strstr(" " @ %list @ " ", " " @ %item @ " ") >= 0;
}

//-----------------------------------------------------------------------------
// Category lookup. An empty or unrecognized category (a fresh standalone
// profile, or a category this table has not caught up with) is treated as
// unknown, and an unknown category shows everything -- never less.
//-----------------------------------------------------------------------------

function GuiProfileEditorFieldSpec::isKnownCategory(%this, %category)
{
	return %category !$= "" && %this.textClass[%category] !$= "";
}

function GuiProfileEditorFieldSpec::textClassFor(%this, %category)
{
	return %this.isKnownCategory(%category) ? %this.textClass[%category] : "full";
}

function GuiProfileEditorFieldSpec::hasFlag(%this, %category, %flag)
{
	if(!%this.isKnownCategory(%category))
	{
		return true;
	}
	return %this.listHas(%this.flags[%category], %flag);
}

// True if the control ever renders in this state (0 = normal, 1 = HL, 2 = SL,
// 3 = NA). Unknown categories report every state live.
function GuiProfileEditorFieldSpec::isStateLive(%this, %category, %stateIndex)
{
	if(!%this.isKnownCategory(%category))
	{
		return true;
	}
	return getSubStr(%this.liveStates[%category], %stateIndex, 1) $= "1";
}

// The label cursorColor should wear: the same field paints the text caret in a
// GuiTextEditCtrl and the keyboard-focus rectangle on a checkbox or drop-down.
function GuiProfileEditorFieldSpec::cursorLabelFor(%this, %category)
{
	return %this.hasFlag(%category, "caretRect") ? "Focus Rect Color" : "Caret Color";
}

//-----------------------------------------------------------------------------
// The single filter predicate. Everything the pane hides or shows goes through
// here, so the rules live in exactly one place.
//-----------------------------------------------------------------------------

function GuiProfileEditorFieldSpec::isFieldVisible(%this, %category, %field, %showAll)
{
	// Fields outside the groups are never shown, Show All included.
	if(!%this.listHas(%this.allFields(), %field))
	{
		return false;
	}

	if(%showAll)
	{
		return true;
	}

	if(%this.listHas(%this.universalFields(), %field))
	{
		return true;
	}

	%class = %this.textClassFor(%category);
	%drawsGlyphs = %class $= "full" || %class $= "direct";
	%usesFontColor = %drawsGlyphs || %class $= "glyph";

	if(%field $= "align")
	{
		// Two categories disagree with their class: the window title-bar buttons
		// read align to pick their edge (guiWindowCtrl.cc renderButton) while a
		// menu item has align overwritten every render (guiMenuBarCtrl.cc), so
		// editing it there would be a lie.
		if(%this.hasFlag(%category, "alignOff"))
		{
			return false;
		}
		if(%this.hasFlag(%category, "alignOn"))
		{
			return true;
		}
		return %class $= "full";
	}

	if(%this.listHas(%this.textLayoutFields(), %field))
	{
		return %class $= "full";
	}

	if(%this.listHas(%this.fontFaceFields(), %field))
	{
		return %drawsGlyphs;
	}

	if(%this.listHas(%this.fontColorFields(), %field))
	{
		return %usesFontColor;
	}

	if(%this.listHas(%this.richTextFields(), %field))
	{
		return %drawsGlyphs;
	}

	if(%this.listHas(%this.focusFields(), %field))
	{
		return %this.hasFlag(%category, "focus");
	}

	if(%this.listHas(%this.textSelectionFields(), %field))
	{
		return %this.hasFlag(%category, "textsel");
	}

	if(%field $= "cursorColor")
	{
		return %this.hasFlag(%category, "caretBeam") || %this.hasFlag(%category, "caretRect");
	}

	return false;
}

// True when any of the fields is visible, so a section with nothing left to
// show can hide its whole panel.
function GuiProfileEditorFieldSpec::anyFieldVisible(%this, %category, %fields, %showAll)
{
	%count = getWordCount(%fields);
	for(%i = 0; %i < %count; %i++)
	{
		if(%this.isFieldVisible(%category, getWord(%fields, %i), %showAll))
		{
			return true;
		}
	}
	return false;
}

// Confirm the table still matches the engine's category list. The editor never
// calls this; the smoke test does, so a category added to
// GuiProfileTheme::smProfileCategories[] cannot drift unnoticed.
//
// %engineNames is what GuiProfileTheme::getCategoryNames() returns: a
// space-separated list. Its casing will not always match this file's, because
// the names are string-table entries and StringTable::insert is
// case-insensitive -- "Empty" comes back as whatever casing the engine
// interned first. That costs nothing here: a dynamic field name is interned the
// same way, so textClass["empty"] and textClass["Empty"] are one slot.
function GuiProfileEditorFieldSpec::findMissingCategories(%this, %engineNames)
{
	%missing = "";
	%count = getWordCount(%engineNames);
	for(%i = 0; %i < %count; %i++)
	{
		%name = getWord(%engineNames, %i);
		if(%name $= "" || %this.textClass[%name] !$= "")
		{
			continue;
		}
		%missing = (%missing $= "") ? %name : (%missing SPC %name);
	}
	return %missing;
}
