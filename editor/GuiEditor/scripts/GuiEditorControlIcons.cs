//-----------------------------------------------------------------------------
// The control palette's entries and their icons. GENERATED - do not hand-edit.
//
// GuiEditor:controlIcons64 and controlIcons128 are the same 31 drawings at two
// sizes in the same 8x4 grid, so one index names the same icon in both and a
// tile can change size without changing its Frame. The two sizes are source
// resolution, not tile size: the grid view draws from the 128 sheet and the row
// view from the 64.
//
// An entry is not always a class. A bare GuiControl is the wrapper, the backdrop,
// the line of text and the modal scrim, so it holds four entries that share a
// class and differ by the category they drop pre-stamped with. That is why this
// is a table and not a list of constants.
//
// Frame 0 is the fallback, deliberately. frameFor answers 0 for any key it does
// not know, and an unset Frame field reads as 0 too, so both failures land on a
// legible "unknown control" rather than on an arbitrary wrong picture. A control
// class added to the engine after this file was generated still reaches the
// palette -- see GuiEditorControlListWindow::populate -- it simply wears the
// question mark until someone draws it.
//
// Frame index is the order the sheet packs them in, which is deliberately NOT
// the order the palette shows them: display order comes from the group column
// and the group list below. That way regrouping the palette is a rerun that
// leaves the sheets untouched, and a reflowed index cannot repoint anything.
// Nothing should ever write a raw frame number; ask frameFor.
//-----------------------------------------------------------------------------

function GuiEditorControlIcons::onAdd(%this)
{
	// The palette's collapsible sections, in the order they stack.
	%this.groupList = "Basics" TAB "Layout" TAB "Input & Data" TAB "Advanced";

	// Classes the palette will not offer, whatever the engine registers. The
	// first group is editor and engine plumbing; the last two are real controls
	// that nobody places by hand -- GuiDragAndDropCtrl is built at runtime to
	// carry a drag payload, and GuiMenuItemCtrl means nothing outside a menu bar.
	%this.refusedNames = "GuiCanvas GuiDragAndDropCtrl GuiGraphCtrl GuiMenuItemCtrl GuiMessageVectorCtrl GuiParticleGraphInspector GuiSceneObjectCtrl";
	%this.refusedPrefixes = "GuiConsole GuiEdit GuiInspector";
	%count = getWordCount(%this.refusedNames);
	for(%i = 0; %i < %count; %i++)
	{
		%this.refused[getWord(%this.refusedNames, %i)] = true;
	}

	// key TAB class TAB category TAB group TAB label
	%table =
		"unknown"               TAB ""                      TAB ""        TAB ""             TAB "Unknown" NL
		"GuiControl:Empty"      TAB "GuiControl"            TAB "Empty"   TAB "Basics"       TAB "Empty" NL
		"GuiControl:Panel"      TAB "GuiControl"            TAB "Panel"   TAB "Basics"       TAB "Panel" NL
		"GuiControl:Label"      TAB "GuiControl"            TAB "Label"   TAB "Basics"       TAB "Label" NL
		"GuiControl:Overlay"    TAB "GuiControl"            TAB "Overlay" TAB "Advanced"     TAB "Overlay" NL
		"GuiButtonCtrl"         TAB "GuiButtonCtrl"         TAB ""        TAB "Basics"       TAB "Button" NL
		"GuiCheckBoxCtrl"       TAB "GuiCheckBoxCtrl"       TAB ""        TAB "Basics"       TAB "Check Box" NL
		"GuiRadioCtrl"          TAB "GuiRadioCtrl"          TAB ""        TAB "Input & Data" TAB "Radio Button" NL
		"GuiDropDownCtrl"       TAB "GuiDropDownCtrl"       TAB ""        TAB "Basics"       TAB "Drop Down" NL
		"GuiColorPopupCtrl"     TAB "GuiColorPopupCtrl"     TAB ""        TAB "Advanced"     TAB "Color Popup" NL
		"GuiImageButtonCtrl"    TAB "GuiImageButtonCtrl"    TAB ""        TAB "Input & Data" TAB "Image Button" NL
		"GuiTextEditCtrl"       TAB "GuiTextEditCtrl"       TAB ""        TAB "Basics"       TAB "Text Edit" NL
		"GuiTextEditSliderCtrl" TAB "GuiTextEditSliderCtrl" TAB ""        TAB "Input & Data" TAB "Number Box" NL
		"GuiSliderCtrl"         TAB "GuiSliderCtrl"         TAB ""        TAB "Input & Data" TAB "Slider" NL
		"GuiProgressCtrl"       TAB "GuiProgressCtrl"       TAB ""        TAB "Input & Data" TAB "Progress" NL
		"GuiColorPickerCtrl"    TAB "GuiColorPickerCtrl"    TAB ""        TAB "Advanced"     TAB "Color Picker" NL
		"GuiSpriteCtrl"         TAB "GuiSpriteCtrl"         TAB ""        TAB "Basics"       TAB "Sprite" NL
		"SceneWindow"           TAB "SceneWindow"           TAB ""        TAB "Advanced"     TAB "Scene Window" NL
		"GuiListBoxCtrl"        TAB "GuiListBoxCtrl"        TAB ""        TAB "Input & Data" TAB "List Box" NL
		"GuiTreeViewCtrl"       TAB "GuiTreeViewCtrl"       TAB ""        TAB "Input & Data" TAB "Tree View" NL
		"GuiMenuBarCtrl"        TAB "GuiMenuBarCtrl"        TAB ""        TAB "Advanced"     TAB "Menu Bar" NL
		"GuiChainCtrl"          TAB "GuiChainCtrl"          TAB ""        TAB "Layout"       TAB "Chain" NL
		"GuiGridCtrl"           TAB "GuiGridCtrl"           TAB ""        TAB "Layout"       TAB "Grid" NL
		"GuiScrollCtrl"         TAB "GuiScrollCtrl"         TAB ""        TAB "Layout"       TAB "Scroll" NL
		"GuiFrameSetCtrl"       TAB "GuiFrameSetCtrl"       TAB ""        TAB "Advanced"     TAB "Frame Set" NL
		"GuiPanelCtrl"          TAB "GuiPanelCtrl"          TAB ""        TAB "Layout"       TAB "Panel" NL
		"GuiExpandCtrl"         TAB "GuiExpandCtrl"         TAB ""        TAB "Layout"       TAB "Expand" NL
		"GuiTabBookCtrl"        TAB "GuiTabBookCtrl"        TAB ""        TAB "Layout"       TAB "Tab Book" NL
		"GuiTabPageCtrl"        TAB "GuiTabPageCtrl"        TAB ""        TAB "Layout"       TAB "Tab Page" NL
		"GuiWindowCtrl"         TAB "GuiWindowCtrl"         TAB ""        TAB "Layout"       TAB "Window" NL
		"GuiInputCtrl"          TAB "GuiInputCtrl"          TAB ""        TAB "Advanced"     TAB "Input";

	%this.keyList = "";
	%count = getRecordCount(%table);
	for(%i = 0; %i < %count; %i++)
	{
		%rec = getRecord(%table, %i);
		%key = trim(getField(%rec, 0));

		%this.frame[%key] = %i;
		%this.ctrlClass[%key] = trim(getField(%rec, 1));
		%this.category[%key] = trim(getField(%rec, 2));
		%this.group[%key] = trim(getField(%rec, 3));
		%this.label[%key] = trim(getField(%rec, 4));

		// The fallback is not something anyone drags, so it stays out of both the
		// flat list and every group. It is what an unknown key resolves TO.
		if(%i > 0)
		{
			%this.keyList = (%this.keyList $= "") ? %key : (%this.keyList TAB %key);

			%group = %this.group[%key];
			%held = %this.groupKeys[%group];
			%this.groupKeys[%group] = (%held $= "") ? %key : (%held TAB %key);

			// Which CLASSES the table accounts for, as opposed to which keys. The
			// two differ for exactly one class: GuiControl is covered four times
			// over, and by no key spelled "GuiControl".
			%this.covered[%this.ctrlClass[%key]] = true;
		}
	}
}

// Every entry, tab separated, in frame order. The fallback is not among them.
function GuiEditorControlIcons::keys(%this)
{
	return %this.keyList;
}

// The collapsible sections the palette builds, in the order they stack.
function GuiEditorControlIcons::groups(%this)
{
	return %this.groupList;
}

// The entries in one section, tab separated. Their order within a section is
// frame order, which is already grouped by kind.
function GuiEditorControlIcons::keysInGroup(%this, %group)
{
	return %this.groupKeys[%group];
}

function GuiEditorControlIcons::groupFor(%this, %key)
{
	return %this.group[%key];
}

// Whether the palette can place a class at all. Generated from the same rule the
// sheet is built with, so the sweep for classes with no icon cannot disagree
// with the table above about what counts as placeable.
//
// GuiEditorControlSpec carries its own copy for its drift guard. That one
// answers "should this class be in the spec table"; this one answers "should the
// palette show it" -- same rule today, different questions, and this is the copy
// generated rather than typed.
function GuiEditorControlIcons::isPlaceableClass(%this, %name)
{
	if(%name $= "" || %this.refused[%name])
	{
		return false;
	}

	// SceneWindow is the one placeable control not named for the Gui hierarchy.
	if(%name !$= "SceneWindow" && getSubStr(%name, 0, 3) !$= "Gui")
	{
		return false;
	}

	%count = getWordCount(%this.refusedPrefixes);
	for(%i = 0; %i < %count; %i++)
	{
		%prefix = getWord(%this.refusedPrefixes, %i);
		if(getSubStr(%name, 0, strlen(%prefix)) $= %prefix)
		{
			return false;
		}
	}

	return true;
}

// Frame 0 - the question mark - for anything this table has never heard of.
function GuiEditorControlIcons::frameFor(%this, %key)
{
	%frame = %this.frame[%key];
	return (%frame $= "") ? 0 : %frame;
}

// The class to instantiate for an entry, and the profile category to stamp it
// with. An empty category means the class pins its own and there is nothing to
// choose - see GuiEditorThemeApplier::buildClassTable.
function GuiEditorControlIcons::classFor(%this, %key)
{
	// ctrlClass, not class: "class" is a real SimObject field, and an array named
	// for it is a collision waiting for whoever adds the next lookup.
	%name = %this.ctrlClass[%key];
	return (%name $= "") ? %key : %name;
}

function GuiEditorControlIcons::categoryFor(%this, %key)
{
	return %this.category[%key];
}

// What the row view writes beside the icon. Falls back to the key, which for
// everything but the four GuiControl entries is the class name.
function GuiEditorControlIcons::labelFor(%this, %key)
{
	%label = %this.label[%key];
	return (%label $= "") ? %key : %label;
}

// True when this key came from the table rather than from the runtime sweep over
// enumerateConsoleClasses. An unknown entry still works; it just has no icon.
function GuiEditorControlIcons::isKnown(%this, %key)
{
	return %this.frame[%key] !$= "";
}

// Whether some entry builds this class, which is not the same question as
// isKnown. A bare GuiControl has four entries and none of them is keyed
// "GuiControl", so asking isKnown about the class name says no and the sweep for
// undrawn classes would offer a fifth, iconless copy of a control the palette
// already shows four ways.
function GuiEditorControlIcons::coversClass(%this, %name)
{
	return %this.covered[%name] !$= "";
}

// The sheet to draw from at a given tile size. The threshold is the SMALL
// sheet's own resolution, not the midpoint between the two: above 64 the small
// sheet would have to be enlarged, and enlarging is what looks soft. Taking the
// 128 and shrinking it costs nothing and stays sharp.
function GuiEditorControlIcons::sheetFor(%this, %tileSize)
{
	return (%tileSize > 64) ? "GuiEditor:controlIcons128" : "GuiEditor:controlIcons64";
}
