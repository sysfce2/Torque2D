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
// Indices are display order, so inserting an icon reflows them. Nothing should
// ever write a raw frame number; ask frameFor.
//-----------------------------------------------------------------------------

function GuiEditorControlIcons::onAdd(%this)
{
	// key TAB class TAB category TAB label
	%table =
		"unknown"               TAB ""                      TAB ""        TAB "Unknown" NL
		"GuiControl:Empty"      TAB "GuiControl"            TAB "Empty"   TAB "Empty" NL
		"GuiControl:Panel"      TAB "GuiControl"            TAB "Panel"   TAB "Panel" NL
		"GuiControl:Label"      TAB "GuiControl"            TAB "Label"   TAB "Label" NL
		"GuiControl:Overlay"    TAB "GuiControl"            TAB "Overlay" TAB "Overlay" NL
		"GuiButtonCtrl"         TAB "GuiButtonCtrl"         TAB ""        TAB "Button" NL
		"GuiCheckBoxCtrl"       TAB "GuiCheckBoxCtrl"       TAB ""        TAB "Check Box" NL
		"GuiRadioCtrl"          TAB "GuiRadioCtrl"          TAB ""        TAB "Radio Button" NL
		"GuiDropDownCtrl"       TAB "GuiDropDownCtrl"       TAB ""        TAB "Drop Down" NL
		"GuiColorPopupCtrl"     TAB "GuiColorPopupCtrl"     TAB ""        TAB "Color Popup" NL
		"GuiImageButtonCtrl"    TAB "GuiImageButtonCtrl"    TAB ""        TAB "Image Button" NL
		"GuiTextEditCtrl"       TAB "GuiTextEditCtrl"       TAB ""        TAB "Text Edit" NL
		"GuiTextEditSliderCtrl" TAB "GuiTextEditSliderCtrl" TAB ""        TAB "Number Box" NL
		"GuiSliderCtrl"         TAB "GuiSliderCtrl"         TAB ""        TAB "Slider" NL
		"GuiProgressCtrl"       TAB "GuiProgressCtrl"       TAB ""        TAB "Progress" NL
		"GuiColorPickerCtrl"    TAB "GuiColorPickerCtrl"    TAB ""        TAB "Color Picker" NL
		"GuiSpriteCtrl"         TAB "GuiSpriteCtrl"         TAB ""        TAB "Sprite" NL
		"SceneWindow"           TAB "SceneWindow"           TAB ""        TAB "Scene Window" NL
		"GuiListBoxCtrl"        TAB "GuiListBoxCtrl"        TAB ""        TAB "List Box" NL
		"GuiTreeViewCtrl"       TAB "GuiTreeViewCtrl"       TAB ""        TAB "Tree View" NL
		"GuiMenuBarCtrl"        TAB "GuiMenuBarCtrl"        TAB ""        TAB "Menu Bar" NL
		"GuiChainCtrl"          TAB "GuiChainCtrl"          TAB ""        TAB "Chain" NL
		"GuiGridCtrl"           TAB "GuiGridCtrl"           TAB ""        TAB "Grid" NL
		"GuiScrollCtrl"         TAB "GuiScrollCtrl"         TAB ""        TAB "Scroll" NL
		"GuiFrameSetCtrl"       TAB "GuiFrameSetCtrl"       TAB ""        TAB "Frame Set" NL
		"GuiPanelCtrl"          TAB "GuiPanelCtrl"          TAB ""        TAB "Panel" NL
		"GuiExpandCtrl"         TAB "GuiExpandCtrl"         TAB ""        TAB "Expand" NL
		"GuiTabBookCtrl"        TAB "GuiTabBookCtrl"        TAB ""        TAB "Tab Book" NL
		"GuiTabPageCtrl"        TAB "GuiTabPageCtrl"        TAB ""        TAB "Tab Page" NL
		"GuiWindowCtrl"         TAB "GuiWindowCtrl"         TAB ""        TAB "Window" NL
		"GuiInputCtrl"          TAB "GuiInputCtrl"          TAB ""        TAB "Input";

	%this.keyList = "";
	%count = getRecordCount(%table);
	for(%i = 0; %i < %count; %i++)
	{
		%rec = getRecord(%table, %i);
		%key = trim(getField(%rec, 0));

		%this.frame[%key] = %i;
		%this.ctrlClass[%key] = trim(getField(%rec, 1));
		%this.category[%key] = trim(getField(%rec, 2));
		%this.label[%key] = trim(getField(%rec, 3));

		// The fallback is not something anyone drags, so it stays out of the
		// list the palette walks.
		if(%i > 0)
		{
			%this.keyList = (%this.keyList $= "") ? %key : (%this.keyList TAB %key);
		}
	}
}

// The palette's entries, in display order, tab separated. The fallback is not
// among them.
function GuiEditorControlIcons::keys(%this)
{
	return %this.keyList;
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

// The sheet to draw from at a given tile size. Anything under the halfway point
// takes the smaller source rather than downsampling the large one.
function GuiEditorControlIcons::sheetFor(%this, %tileSize)
{
	return (%tileSize > 96) ? "GuiEditor:controlIcons128" : "GuiEditor:controlIcons64";
}
