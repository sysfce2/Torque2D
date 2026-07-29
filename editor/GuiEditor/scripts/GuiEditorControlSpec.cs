
//-----------------------------------------------------------------------------
// The context model behind the Gui Editor's properties pane: which of a
// control's fields actually matter for the kind of control it is.
//
// The sibling of GuiProfileEditorFieldSpec, and the same idea. A GuiControl
// registers a field for everything any control might want, but a given class
// reads a fraction of it: a GuiChainCtrl never draws its own text, so the nine
// text fields it inherits do nothing, and a GuiTabPageCtrl's position and
// extent are overwritten by its book on every layout pass. Editing either looks
// like it works and silently reverts.
//
// Unlike the profile spec there is no Show All. A profile field that a category
// never reads could still be read by some other category, so hiding it is a
// filter; here a hidden field is one the engine provably never looks at for
// this class, so there is nothing to reveal.
//
// Every entry below is derived from the class's render and layout paths, not
// from what the field name suggests. The surprising ones carry the source line
// that settles them.
//
// Four traits per class:
//
//   textRole   What the control's own "text" field does.
//                render       drawn through GuiControl::renderText -- every
//                             text field applies.
//                caption      drawn on the control's behalf by something else
//                             (a panel's header button, a tab book drawing its
//                             page's text with mTabProfile). The string matters;
//                             the control's own layout fields do not.
//                placeholder  drawn only while nothing is selected
//                             (guiDropDownCtrl.cc, getSelectedItem() == -1).
//                             Same fields as render, different label.
//                proxy        renderText runs, but with someone else's string
//                             (list and tree item text). Layout fields live,
//                             text and textID dead.
//                inherited    no onRender override, so GuiControl::onRender
//                             draws mText. Live, but nobody wants text on a
//                             grid -- it goes in a collapsed section rather
//                             than the header.
//                none         never drawn.
//   flags      fontAdjust  reads mFontSizeAdjust outside renderText, so the
//                          field is live even where the text block is not.
//              easing      calls GuiEasingSupport::getFillColor, which is what
//                          makes the four ease* fields do anything.
//              command     fires Command on interaction rather than only
//                          through an accelerator, so it earns its own section.
//              bare        registers no GuiControl fields at all.
//   hides      Fields this class must drop from the shared rows despite its
//              textRole -- the per-class exceptions.
//   sections   The class's own collapsible sections, declared below.
//
// A fifth trait, geometryMode, is a property of the control's PARENT rather
// than its class, so it is computed per control instead of tabled.
//
// Fields absent from everything here are never shown: canSave (its write
// function is defaultProtectedNotWriteFn, so it never even serializes),
// canSaveDynamicFields, parentGroup, and every TypeGuiCursor field -- GuiCursor
// has no category and GuiProfileTheme has no cursor table, so cursors cannot
// join the Variants scheme the profile slots use.
//
// The spec is pure data with no UI; the pane owns one and asks it what to show.
//-----------------------------------------------------------------------------

function GuiEditorControlSpec::onAdd(%this)
{
	// class TAB textRole TAB flags TAB per-class hides
	%table =
		"GuiControl"            TAB "render"      TAB ""                TAB ""                    NL

		// Buttons. Easing is narrower than the class tree suggests: only
		// GuiButtonCtrl and GuiDropDownCtrl actually call getFillColor.
		// GuiCheckBoxCtrl::onRender draws its box through renderInnerControl and
		// never renders a universal rect for itself, so Radio inherits a dead
		// set too, and GuiImageButtonCtrl draws only its asset.
		"GuiButtonCtrl"         TAB "render"      TAB "command easing"  TAB ""                    NL
		"GuiCheckBoxCtrl"       TAB "render"      TAB "command"         TAB ""                    NL
		"GuiRadioCtrl"          TAB "render"      TAB "command"         TAB ""                    NL
		"GuiDropDownCtrl"       TAB "placeholder" TAB "command easing"  TAB ""                    NL
		"GuiColorPopupCtrl"     TAB "none"        TAB "command"         TAB ""                    NL
		"GuiImageButtonCtrl"    TAB "none"        TAB "command"         TAB ""                    NL

		// A text edit reads mTextWrap (it is the multi-line control) and
		// getAlignmentType, but never mVAlignment or mTextExtend.
		"GuiTextEditCtrl"       TAB "render"      TAB "command"         TAB "vAlign textExtend"   NL
		"GuiTextEditSliderCtrl" TAB "render"      TAB "command"         TAB "vAlign textExtend"   NL

		// The slider draws its value with dglDrawText, not renderText, so the
		// text block is dead -- but guiSliderCtrl.cc still sizes that draw with
		// getFont(mFontSizeAdjust).
		"GuiSliderCtrl"         TAB "none"        TAB "command fontAdjust" TAB ""                 NL
		"GuiColorPickerCtrl"    TAB "render"      TAB "command"         TAB ""                    NL
		"GuiProgressCtrl"       TAB "render"      TAB ""                TAB ""                    NL
		"GuiSpriteCtrl"         TAB "none"        TAB ""                TAB ""                    NL

		// Lists render item text through renderText with their own profile, so
		// the layout fields are live even though "text" is never drawn. Not
		// textExtend: it would resize the whole list to fit one item.
		"GuiListBoxCtrl"        TAB "proxy"       TAB ""                TAB ""                    NL
		"GuiTreeViewCtrl"       TAB "proxy"       TAB ""                TAB "BindToGuiEditor"     NL

		"GuiMenuBarCtrl"        TAB "none"        TAB ""                TAB ""                    NL
		"GuiMenuItemCtrl"       TAB "caption"     TAB "command bare"    TAB ""                    NL

		// Layout containers. The chain draws only a "+" in edit mode.
		"GuiChainCtrl"          TAB "none"        TAB ""                TAB ""                    NL
		"GuiGridCtrl"           TAB "inherited"   TAB ""                TAB ""                    NL
		"GuiScrollCtrl"         TAB "none"        TAB ""                TAB ""                    NL
		"GuiFrameSetCtrl"       TAB "none"        TAB "easing"          TAB ""                    NL

		// A panel hands its text to the header button it owns; the button
		// renders it, so the panel's own layout fields never run.
		"GuiPanelCtrl"          TAB "caption"     TAB ""                TAB ""                    NL
		"GuiExpandCtrl"         TAB "inherited"   TAB ""                TAB ""                    NL

		// The book draws each page's text as the tab label, using mTabProfile
		// and the BOOK's mFontSizeAdjust -- so the layout fields belong to the
		// book and the string belongs to the page.
		"GuiTabBookCtrl"        TAB "proxy"       TAB ""                TAB "textWrap"            NL
		"GuiTabPageCtrl"        TAB "caption"     TAB ""                TAB ""                    NL

		"GuiWindowCtrl"         TAB "render"      TAB ""                TAB ""                    NL
		"GuiDragAndDropCtrl"    TAB "inherited"   TAB ""                TAB ""                    NL
		"GuiInputCtrl"          TAB "inherited"   TAB "command"         TAB ""                    NL
		"SceneWindow"           TAB "none"        TAB ""                TAB "";

	%this.classNames = "";
	%count = getRecordCount(%table);
	for(%i = 0; %i < %count; %i++)
	{
		%rec = getRecord(%table, %i);
		%name = trim(getField(%rec, 0));

		// "class" is a real SimObject field, so the table uses textRole.
		%this.textRole[%name] = trim(getField(%rec, 1));
		%this.flags[%name] = trim(getField(%rec, 2));
		%this.hides[%name] = trim(getField(%rec, 3));
		%this.sectionKeyList[%name] = "";

		%this.classNames = (%this.classNames $= "") ? %name : (%this.classNames SPC %name);
	}

	%this.buildSections();
	%this.buildHeaderValues();
	%this.buildLabels();
}

//-----------------------------------------------------------------------------
// The class-specific sections. Declared in full per class rather than
// accumulated up an inheritance chain: a section list reads as documentation of
// what the control is, and the handful of repeated lines cost less than a
// chain-walking rule that every future exception would have to fight.
//-----------------------------------------------------------------------------

function GuiEditorControlSpec::buildSections(%this)
{
	%this.addSection("GuiCheckBoxCtrl", "Box", "Check Box",
		"boxOffset boxExtent textOffset textExtent");
	%this.addSection("GuiRadioCtrl", "Box", "Radio Button",
		"boxOffset boxExtent textOffset textExtent");

	%this.addSection("GuiDropDownCtrl", "DropDown", "Drop Down", "maxHeight");
	%this.addSection("GuiDropDownCtrl", "Scrollbar", "Scroll Bar",
		"constantThumbHeight showArrowButtons scrollBarThickness");

	%this.addSection("GuiColorPopupCtrl", "Popup", "Popup",
		"popupSize barHeight showAlphaBar swatchColumns showColorValues valueMode valueBoxHeight");

	%this.addSection("GuiImageButtonCtrl", "Images", "Images",
		"HoverImage DownImage InactiveImage");

	%this.addSection("GuiTextEditCtrl", "Input", "Input",
		"maxLength password returnCausesTab sinkAllKeyEvents");
	%this.addSection("GuiTextEditCtrl", "Commands", "Commands",
		"returnCommand escapeCommand");
	%this.addSection("GuiTextEditSliderCtrl", "Input", "Input",
		"maxLength password returnCausesTab sinkAllKeyEvents");
	%this.addSection("GuiTextEditSliderCtrl", "Commands", "Commands",
		"returnCommand escapeCommand");
	%this.addSection("GuiTextEditSliderCtrl", "Number", "Number", "format");

	%this.addSection("GuiSliderCtrl", "Slider", "Slider", "ticks");
	%this.addSection("GuiColorPickerCtrl", "Picker", "Picker",
		"PickColor ShowSelector ActionOnMove");
	%this.addSection("GuiProgressCtrl", "Progress", "Progress", "animationTime");

	// Image, Animation and Bitmap are three ways to say the same thing, so the
	// pane shows one of the three at a time; sourceMode below picks which.
	%this.addSection("GuiSpriteCtrl", "Fit", "Fit",
		"fullSize imageSize constrainProportions clampImage tileImage positionOffset");
	%this.addSection("GuiSpriteCtrl", "Color", "Color", "imageColor");

	%this.addSection("GuiListBoxCtrl", "List", "List",
		"AllowMultipleSelections FitParentWidth");
	%this.addSection("GuiTreeViewCtrl", "Tree", "Tree", "AllowReorder");
	%this.addSection("GuiTreeViewCtrl", "List", "List",
		"AllowMultipleSelections FitParentWidth");

	%this.addSection("GuiMenuBarCtrl", "Scrollbar", "Scroll Bar",
		"constantThumbHeight showArrowButtons scrollBarThickness");
	// IsOn is the header's principal value, so the section carries only the two
	// fields that decide how the item behaves when it is picked.
	%this.addSection("GuiMenuItemCtrl", "Item", "Menu Item", "Toggle Radio");

	%this.addSection("GuiGridCtrl", "Grid", "Grid",
		"CellSpacingX CellSpacingY MaxColCount MaxRowCount OrderMode IsExtentDynamic");
	%this.addSection("GuiScrollCtrl", "Scrollbar", "Scroll Bar",
		"constantThumbHeight showArrowButtons scrollBarThickness");

	%this.addSection("GuiPanelCtrl", "Expand", "Expand", "easeExpand easeTimeExpand");
	%this.addSection("GuiExpandCtrl", "Expand", "Expand", "easeExpand easeTimeExpand");

	%this.addSection("GuiTabBookCtrl", "Tabs", "Tabs", "MinTabWidth");

	// The six window toggles are a second icon row rather than six checkboxes;
	// the pane reads this key specially. Kept here so the field list stays in
	// one place.
	%this.addSection("GuiWindowCtrl", "Window", "Window",
		"canMove canClose canMinimize canMaximize resizeWidth resizeHeight");
	%this.addSection("GuiWindowCtrl", "Grips", "Resize Grips",
		"resizeRightWidth resizeBottomHeight");

	%this.addSection("GuiDragAndDropCtrl", "Drag", "Drag", "deleteOnMouseUp");

	%this.addSection("SceneWindow", "SceneInput", "Scene Input",
		"lockMouse UseWindowInputEvents UseObjectInputEvents");
	%this.addSection("SceneWindow", "Background", "Background",
		"UseBackgroundColor BackgroundColor");
	%this.addSection("SceneWindow", "Scrollbar", "Scroll Bar",
		"constantThumbHeight showArrowButtons scrollBarThickness");
}

function GuiEditorControlSpec::addSection(%this, %class, %key, %title, %fields)
{
	%this.sectionTitleFor[%class, %key] = %title;
	%this.sectionFieldsFor[%class, %key] = %fields;

	%list = %this.sectionKeyList[%class];
	%this.sectionKeyList[%class] = (%list $= "") ? %key : (%list SPC %key);
}

function GuiEditorControlSpec::sectionKeys(%this, %class)
{
	return %this.sectionKeyList[%class];
}

function GuiEditorControlSpec::sectionTitle(%this, %class, %key)
{
	return %this.sectionTitleFor[%class, %key];
}

function GuiEditorControlSpec::sectionFields(%this, %class, %key)
{
	return %this.sectionFieldsFor[%class, %key];
}

//-----------------------------------------------------------------------------
// The principal value: the one or two fields that are the whole point of the
// control, promoted out of their section and into the always-visible header.
//-----------------------------------------------------------------------------

function GuiEditorControlSpec::buildHeaderValues(%this)
{
	%this.headerValues["GuiCheckBoxCtrl"] = "stateOn";
	%this.headerValues["GuiRadioCtrl"] = "stateOn groupNum";
	%this.headerValues["GuiColorPopupCtrl"] = "baseColor";
	%this.headerValues["GuiImageButtonCtrl"] = "NormalImage";
	%this.headerValues["GuiTextEditCtrl"] = "inputMode";
	%this.headerValues["GuiTextEditSliderCtrl"] = "inputMode range increment";
	%this.headerValues["GuiSliderCtrl"] = "range value";
	%this.headerValues["GuiColorPickerCtrl"] = "BaseColor DisplayMode";
	%this.headerValues["GuiProgressCtrl"] = "Variable";
	%this.headerValues["GuiChainCtrl"] = "IsVertical ChildSpacing";
	%this.headerValues["GuiGridCtrl"] = "CellModeX CellModeY CellSizeX CellSizeY";
	%this.headerValues["GuiScrollCtrl"] = "hScrollBar vScrollBar";
	%this.headerValues["GuiFrameSetCtrl"] = "DividerThickness";
	%this.headerValues["GuiTabBookCtrl"] = "TabPosition";
	%this.headerValues["GuiWindowCtrl"] = "titleHeight";
	%this.headerValues["GuiMenuItemCtrl"] = "IsOn";

	// GuiSpriteCtrl is deliberately absent: its principal value is three
	// mutually exclusive fields rather than a fixed list, so the pane asks
	// spriteSourceFields below instead of reading a header value here.
}

function GuiEditorControlSpec::headerValueFields(%this, %class)
{
	return %this.headerValues[%class];
}

// The three ways a GuiSpriteCtrl can name its picture. Only one is ever in
// effect, so offering all three at once invites setting two and wondering which
// won. The pane shows the fields of the mode the control is currently in.
function GuiEditorControlSpec::spriteSourceFields(%this, %mode)
{
	switch$(%mode)
	{
		case "Animation": return "Animation";
		case "Bitmap": return "Bitmap singleFrameBitmap";
	}
	return "Image Frame NamedFrame";
}

function GuiEditorControlSpec::spriteSourceModes(%this)
{
	return "Image" TAB "Animation" TAB "Bitmap";
}

// Which of the three a control is actually using, judged by what it holds.
function GuiEditorControlSpec::spriteSourceModeOf(%this, %ctrl)
{
	if(%ctrl.Animation !$= "")
	{
		return "Animation";
	}
	if(%ctrl.Image $= "" && %ctrl.Bitmap !$= "")
	{
		return "Bitmap";
	}
	return "Image";
}

//-----------------------------------------------------------------------------
// The shared field groups. These rows are built once and filtered with
// setVisible, because every class has them -- only whether they mean anything
// changes.
//-----------------------------------------------------------------------------

// Drawn by GuiControl::renderText, so they live or die with textRole.
function GuiEditorControlSpec::textFields(%this)
{
	return "text textID textWrap textExtend align vAlign fontSizeAdjust overrideFontColor fontColor";
}

// The subset a proxy role keeps: renderText still runs, just with a string the
// control did not get from its text field.
function GuiEditorControlSpec::textLayoutFields(%this)
{
	return "textWrap textExtend align vAlign fontSizeAdjust overrideFontColor fontColor";
}

function GuiEditorControlSpec::geometryFields(%this)
{
	return "Position Extent HorizSizing VertSizing MinExtent";
}

function GuiEditorControlSpec::layoutFields(%this)
{
	return "MinExtent isContainer";
}

function GuiEditorControlSpec::tooltipFields(%this)
{
	return "tooltip tooltipWidth hovertime";
}

function GuiEditorControlSpec::scriptingFields(%this)
{
	return "class superclass internalName Variable Command AltCommand Accelerator";
}

function GuiEditorControlSpec::commandFields(%this)
{
	return "Command AltCommand Variable Accelerator";
}

function GuiEditorControlSpec::localizationFields(%this)
{
	return "langTableMod textID";
}

function GuiEditorControlSpec::easingFields(%this)
{
	return "easeFillColorHL easeFillColorSL easeTimeFillColorHL easeTimeFillColorSL";
}

// The toggles the header draws as icons instead of rows. Visible, Active and
// useInput are what the control does when the game runs; hidden and locked are
// what the editor does with it.
function GuiEditorControlSpec::runtimeToggles(%this)
{
	return "Visible Active useInput";
}

function GuiEditorControlSpec::editorToggles(%this)
{
	return "hidden locked";
}

// Never shown anywhere, for any class.
function GuiEditorControlSpec::deadFields(%this)
{
	return "canSave canSaveDynamicFields parentGroup";
}

// Space-delimited membership. Wrapping both sides in spaces keeps text from
// matching inside textWrap.
function GuiEditorControlSpec::listHas(%this, %list, %item)
{
	return strstr(" " @ %list @ " ", " " @ %item @ " ") >= 0;
}

//-----------------------------------------------------------------------------
// Class lookup. An unrecognized class -- a C++ subclass added after this table
// was written -- is treated as unknown, and an unknown class shows everything
// rather than less. The pane puts its own fields in a single "Other" section,
// which lands it back at roughly what the native inspector did.
//-----------------------------------------------------------------------------

function GuiEditorControlSpec::isKnownClass(%this, %class)
{
	return %class !$= "" && %this.textRole[%class] !$= "";
}

function GuiEditorControlSpec::textRoleFor(%this, %class)
{
	return %this.isKnownClass(%class) ? %this.textRole[%class] : "render";
}

function GuiEditorControlSpec::hasFlag(%this, %class, %flag)
{
	if(!%this.isKnownClass(%class))
	{
		// An unknown class gets the permissive answer everywhere except "bare",
		// which would strip its entire header.
		return %flag !$= "bare";
	}
	return %this.listHas(%this.flags[%class], %flag);
}

function GuiEditorControlSpec::classHides(%this, %class, %field)
{
	return %this.isKnownClass(%class) && %this.listHas(%this.hides[%class], %field);
}

//-----------------------------------------------------------------------------
// Geometry. This one is not a property of the control's class but of its
// parent's: four containers write their children's bounds outright, so the
// fields they own must be read-only wherever the child happens to sit. It has
// to be recomputed when a control is reparented, not only when it is selected.
//-----------------------------------------------------------------------------

// full    the control owns its position, extent and both sizing enums
// none    the parent owns all of it
// chainV  a vertical chain owns the Y position and the vertical sizing
// chainH  a horizontal chain owns the X position and the horizontal sizing
function GuiEditorControlSpec::geometryModeOf(%this, %ctrl)
{
	if(!isObject(%ctrl))
	{
		return "full";
	}

	// A tab page's geometry is not its parent's doing alone -- the class is
	// only ever a child of a book -- but the answer is the same either way.
	%parent = %ctrl.getParent();
	if(!isObject(%parent))
	{
		return "full";
	}

	switch$(%parent.getClassName())
	{
		// guiGridCtrl.cc resize(): every child is resized into its cell.
		case "GuiGridCtrl": return "none";

		// guiFrameSetCtrl.cc: each frame resizes the control it holds.
		case "GuiFrameSetCtrl": return "none";

		// guiTabBookCtrl.cc: a page is forced to (0,0) and the page rect.
		case "GuiTabBookCtrl": return "none";

		// guiChainCtrl.cc positionChildren(): only the stacking axis is taken.
		// The cross axis keeps the position the child was given, and the extent
		// is the child's own on both axes -- the chain reads it to lay out.
		// onChildAdded also rewrites a "center" sizing on the stacking axis,
		// which is why that enum goes too.
		case "GuiChainCtrl": return %parent.IsVertical ? "chainV" : "chainH";
	}

	// A GuiScrollCtrl is the container people expect to own its children and
	// does not: it scrolls them and leaves their bounds alone.
	return "full";
}

// True when the control, sitting where it is, may edit this geometry field.
function GuiEditorControlSpec::isGeometryFieldLive(%this, %mode, %field)
{
	if(%mode $= "none")
	{
		return false;
	}
	if(%mode $= "chainV")
	{
		return %field !$= "VertSizing";
	}
	if(%mode $= "chainH")
	{
		return %field !$= "HorizSizing";
	}
	return true;
}

// Which position axes the control still controls: "" , "x", "y" or "xy".
function GuiEditorControlSpec::livePositionAxes(%this, %mode)
{
	switch$(%mode)
	{
		case "none": return "";
		case "chainV": return "x";
		case "chainH": return "y";
	}
	return "xy";
}

//-----------------------------------------------------------------------------
// The single filter predicate for the shared rows. Everything the pane hides or
// shows among them goes through here, so the rules live in one place.
//-----------------------------------------------------------------------------

function GuiEditorControlSpec::isFieldVisible(%this, %class, %field)
{
	if(%this.listHas(%this.deadFields(), %field))
	{
		return false;
	}

	if(%this.classHides(%class, %field))
	{
		return false;
	}

	// A menu item calls SimObject::initPersistFields directly rather than
	// GuiControl's, so it has none of these fields to show.
	%bare = %this.hasFlag(%class, "bare");

	%role = %this.textRoleFor(%class);

	if(%this.listHas(%this.textFields(), %field))
	{
		if(%field $= "text" || %field $= "textID")
		{
			// Every role but proxy has a string of its own worth editing.
			return %role !$= "none" && %role !$= "proxy";
		}

		// The layout half needs renderText to actually run on this control.
		if(%role $= "caption")
		{
			return false;
		}
		if(%field $= "fontSizeAdjust" && %this.hasFlag(%class, "fontAdjust"))
		{
			return true;
		}
		return %role $= "render" || %role $= "placeholder" ||
			%role $= "inherited" || %role $= "proxy";
	}

	if(%bare)
	{
		return false;
	}

	if(%field $= "isContainer")
	{
		// Dead where the control cannot draw children: GuiControl's
		// setIsContainerFn forces the field false for those, so offering it
		// would be a switch wired to nothing.
		return false;
	}

	if(%this.listHas(%this.easingFields(), %field))
	{
		return %this.hasFlag(%class, "easing");
	}

	return true;
}

// isContainer needs the control, not just its class -- rendersChildren() is a
// C++ answer fixed by the class, read rather than tabled so it cannot drift.
function GuiEditorControlSpec::isContainerFieldVisible(%this, %ctrl)
{
	return isObject(%ctrl) && !%this.hasFlag(%ctrl.getClassName(), "bare") &&
		%ctrl.rendersChildren();
}

// Where the class's text block goes. The block itself is one component holding
// every field GuiControl::renderText reads, so the only question left here is
// which of the two copies of it -- the header's or the Text section's -- the
// class wants.
//
//   header   the string, or the type it is drawn in, is a principal property.
//            Proxy joins the three header roles: a list draws its items with
//            this control's font, and the size of that is worth reaching for
//            even though the "text" field itself is never drawn.
//   section  live, but not what anyone opens the pane for. A grid can draw text
//            and nobody asks it to; a slider draws none but still sizes its
//            value with fontSizeAdjust.
//   none     nothing in the block applies.
function GuiEditorControlSpec::textBlockHome(%this, %class)
{
	%role = %this.textRoleFor(%class);
	if(%role $= "inherited")
	{
		return "section";
	}
	if(%role $= "none")
	{
		return %this.hasFlag(%class, "fontAdjust") ? "section" : "none";
	}
	return "header";
}

// The text block owns every field in textFields() except textID, which belongs
// to Localization and is the only one of the nine that is not about how the
// text is drawn.
//
// These three of them are ordinary field rows, so they go in the pane's shared
// registry and are filtered and loaded with everything else. The other four are
// two toggle icons and two segmented rows, which the block owns outright.
//
// overrideFontColor is in neither group: it has no row at all. The font colour
// swatch is both fields, because picking a colour IS turning the override on.
function GuiEditorControlSpec::textBlockRowFields(%this)
{
	return "text fontSizeAdjust fontColor";
}

//-----------------------------------------------------------------------------
// Profile category. Every class but one is pinned by
// GuiEditorThemeApplier::buildClassTable -- a check box wants a CheckBox
// profile, and there is nothing to ask about. A bare GuiControl is the
// exception: in 4.0 it is the wrapper, the backdrop, the line of text, the
// paragraph and the modal scrim, and which of those it is cannot be read off
// the class. The applier guesses from what the control holds when it is dropped
// (categoryForControl), which is a good default and a guess all the same, so
// the pane offers these four and lets the answer be corrected.
//
// The choice needs no storage: a profile carries the category it was stamped
// for, so what the control wears is the record of what it was told to be.
//-----------------------------------------------------------------------------

function GuiEditorControlSpec::categoryChoices(%this, %class)
{
	return (%class $= "GuiControl") ? "Empty Panel Label Overlay" : "";
}

// Which of the choices a profile's category names, spelled the way the choices
// spell it -- or "" if it names none of them.
//
// The case has to be thrown away to match and kept to answer. A category is
// stored with StringTable->insert, which interns case-insensitively and hands
// back whichever spelling reached the table first, so a profile stamped for
// "Empty" reads back as "empty" if anything else interned that word in lower
// case first. listHas cannot be used here: it is built on strstr, which unlike
// $= is case-sensitive.
function GuiEditorControlSpec::matchCategory(%this, %choices, %category)
{
	if(%category $= "")
	{
		return "";
	}

	%count = getWordCount(%choices);
	for(%i = 0; %i < %count; %i++)
	{
		%choice = getWord(%choices, %i);
		if(%choice $= %category)
		{
			return %choice;
		}
	}
	return "";
}

// What to call the text field. A drop-down only ever shows it while nothing is
// selected, and a panel and a tab page hand theirs to something else to draw.
function GuiEditorControlSpec::textLabelFor(%this, %class)
{
	switch$(%this.textRoleFor(%class))
	{
		case "placeholder": return "Placeholder";
		case "caption": return (%class $= "GuiTabPageCtrl") ? "Tab Caption" : "Header Text";
	}
	return "Text";
}

//-----------------------------------------------------------------------------
// Dynamic fields. A GuiFrameSetCtrl serializes its whole frame tree into
// dynamic fields (guiFrameSetCtrl.cc writeFields), and hand-editing those can
// leave a Gui that will not load. Nothing else in the engine does this.
//-----------------------------------------------------------------------------

function GuiEditorControlSpec::dynamicHidePrefixes(%this, %class)
{
	if(%class $= "GuiFrameSetCtrl")
	{
		return "frame";
	}
	return "";
}

function GuiEditorControlSpec::hidesDynamicField(%this, %class, %name)
{
	%prefixes = %this.dynamicHidePrefixes(%class);
	%count = getWordCount(%prefixes);
	for(%i = 0; %i < %count; %i++)
	{
		%prefix = getWord(%prefixes, %i);
		if(strlwr(getSubStr(%name, 0, strlen(%prefix))) $= strlwr(%prefix))
		{
			return true;
		}
	}
	return false;
}

//-----------------------------------------------------------------------------
// Field presentation. getFieldType answers with a console type's class name --
// what ConsoleType's first argument spells -- so the mapping is from those, not
// from the TypeXxx constants the engine uses in initPersistFields.
//-----------------------------------------------------------------------------

function GuiEditorControlSpec::kindForType(%this, %type)
{
	switch$(%type)
	{
		case "bool": return "bool";
		case "int" or "char": return "number";

		// Separate kinds for the real-numbered fields, because the row rounds a
		// whole-number one on the way out. A slider's value is a TypeF32 between
		// its two bounds and fontSizeAdjust is a multiplier around 1: rounding
		// either is the difference between the field working and not.
		case "float": return "decimal";
		case "enumval": return "enum";
		case "Point2I": return "point";
		case "Point2F" or "Vector2": return "pointf";
		case "ColorI" or "ColorF" or "FluidColorI": return "color";
		case "filename": return "file";
		case "assetIdString": return "asset";
		case "GuiProfile": return "profile";
		case "GuiCursor" or "GuiBProfile": return "hidden";
	}
	return "text";
}

// Human labels for the fields whose registered name reads badly in a caption.
// Anything absent falls back to the field name, which is usually right.
function GuiEditorControlSpec::buildLabels(%this)
{
	%this.label["HorizSizing"] = "Horizontal Sizing";
	%this.label["VertSizing"] = "Vertical Sizing";
	%this.label["MinExtent"] = "Minimum Extent";
	%this.label["useInput"] = "Accepts Input";
	%this.label["isContainer"] = "Accepts Children";
	%this.label["hovertime"] = "Hover Time";
	%this.label["tooltipWidth"] = "Tooltip Width";
	%this.label["langTableMod"] = "Language Table";
	%this.label["textID"] = "Text ID";
	%this.label["fontSizeAdjust"] = "Font Size Adjust";
	%this.label["overrideFontColor"] = "Override Font Color";
	%this.label["align"] = "Horizontal Align";
	%this.label["vAlign"] = "Vertical Align";
	%this.label["textWrap"] = "Wrap Text";
	%this.label["textExtend"] = "Extend To Fit Text";
	%this.label["stateOn"] = "Checked";
	%this.label["groupNum"] = "Radio Group";
	%this.label["IsVertical"] = "Vertical";
	%this.label["ChildSpacing"] = "Child Spacing";
	%this.label["IsExtentDynamic"] = "Grow To Fit";
	%this.label["constantThumbHeight"] = "Constant Thumb";
	%this.label["scrollBarThickness"] = "Bar Thickness";
	%this.label["showArrowButtons"] = "Arrow Buttons";
	%this.label["hScrollBar"] = "Horizontal Bar";
	%this.label["vScrollBar"] = "Vertical Bar";
	%this.label["maxHeight"] = "Max Height";
	%this.label["titleHeight"] = "Title Height";
	%this.label["resizeRightWidth"] = "Right Grip Width";
	%this.label["resizeBottomHeight"] = "Bottom Grip Height";
	%this.label["DividerThickness"] = "Divider Thickness";
	%this.label["MinTabWidth"] = "Min Tab Width";
	%this.label["TabPosition"] = "Tab Position";
	%this.label["AllowMultipleSelections"] = "Multiple Selection";
	%this.label["FitParentWidth"] = "Fit Parent Width";
	%this.label["AllowReorder"] = "Allow Reorder";
	%this.label["animationTime"] = "Animation Time";
	%this.label["easeExpand"] = "Expand Easing";
	%this.label["easeTimeExpand"] = "Expand Time";
	%this.label["easeFillColorHL"] = "Hover Easing";
	%this.label["easeFillColorSL"] = "Press Easing";
	%this.label["easeTimeFillColorHL"] = "Hover Time";
	%this.label["easeTimeFillColorSL"] = "Press Time";
	%this.label["sinkAllKeyEvents"] = "Sink Key Events";
	%this.label["returnCausesTab"] = "Return Tabs";
	%this.label["returnCommand"] = "Return Command";
	%this.label["escapeCommand"] = "Escape Command";
	%this.label["maxLength"] = "Max Length";
	%this.label["inputMode"] = "Input Mode";
	%this.label["deleteOnMouseUp"] = "Delete On Drop";
	%this.label["lockMouse"] = "Lock Mouse";
	%this.label["UseWindowInputEvents"] = "Window Input Events";
	%this.label["UseObjectInputEvents"] = "Object Input Events";
	%this.label["UseBackgroundColor"] = "Use Background Color";
	%this.label["BackgroundColor"] = "Background Color";
	%this.label["constrainProportions"] = "Keep Proportions";
	%this.label["positionOffset"] = "Position Offset";
	%this.label["singleFrameBitmap"] = "Single Frame";
	%this.label["NamedFrame"] = "Named Frame";
	%this.label["imageColor"] = "Image Color";
	%this.label["imageSize"] = "Image Size";
	%this.label["fullSize"] = "Full Size";
	%this.label["clampImage"] = "Clamp Image";
	%this.label["tileImage"] = "Tile Image";
	%this.label["NormalImage"] = "Normal";
	%this.label["HoverImage"] = "Hover";
	%this.label["DownImage"] = "Down";
	%this.label["InactiveImage"] = "Inactive";
	%this.label["baseColor"] = "Color";
	%this.label["BaseColor"] = "Color";
	%this.label["PickColor"] = "Picked Color";
	%this.label["DisplayMode"] = "Display Mode";
	%this.label["ActionOnMove"] = "Action On Move";
	%this.label["ShowSelector"] = "Show Selector";
	%this.label["showAlphaBar"] = "Alpha Bar";
	%this.label["showColorValues"] = "Color Values";
	%this.label["swatchColumns"] = "Swatch Columns";
	%this.label["valueBoxHeight"] = "Value Box Height";
	%this.label["valueMode"] = "Value Mode";
	%this.label["popupSize"] = "Popup Size";
	%this.label["barHeight"] = "Bar Height";
	%this.label["boxOffset"] = "Box Offset";
	%this.label["boxExtent"] = "Box Extent";
	%this.label["textOffset"] = "Text Offset";
	%this.label["textExtent"] = "Text Extent";
	%this.label["CellModeX"] = "Column Mode";
	%this.label["CellModeY"] = "Row Mode";
	%this.label["CellSizeX"] = "Column Size";
	%this.label["CellSizeY"] = "Row Size";
	%this.label["CellSpacingX"] = "Column Spacing";
	%this.label["CellSpacingY"] = "Row Spacing";
	%this.label["MaxColCount"] = "Max Columns";
	%this.label["MaxRowCount"] = "Max Rows";
	%this.label["OrderMode"] = "Order";
	%this.label["canMove"] = "Move";
	%this.label["canClose"] = "Close";
	%this.label["canMinimize"] = "Minimize";
	%this.label["canMaximize"] = "Maximize";
	%this.label["resizeWidth"] = "Resize Width";
	%this.label["resizeHeight"] = "Resize Height";
	%this.label["internalName"] = "Internal Name";
	%this.label["superclass"] = "Super Class";
	%this.label["AltCommand"] = "Alt Command";
	%this.label["IsOn"] = "On";

	// The secondary profile slots, named for the part they style rather than
	// for the field. These are the Variants rows; the slot-to-category mapping
	// they are filtered by lives in GuiEditorThemeApplier::buildFieldTable.
	%this.label["tooltipProfile"] = "Tooltip";
	%this.label["contentProfile"] = "Content";
	%this.label["closeButtonProfile"] = "Close Button";
	%this.label["minButtonProfile"] = "Minimise Button";
	%this.label["maxButtonProfile"] = "Maximise Button";
	%this.label["scrollProfile"] = "Scroller";
	%this.label["thumbProfile"] = "Thumb";
	%this.label["trackProfile"] = "Track";
	%this.label["arrowProfile"] = "Arrows";
	%this.label["listBoxProfile"] = "List";
	%this.label["tabBookProfile"] = "Tab Book";
	%this.label["tabProfile"] = "Tab";
	%this.label["tabPageProfile"] = "Tab Page";
	%this.label["dropButtonProfile"] = "Drop Button";
	%this.label["menuProfile"] = "Menu";
	%this.label["menuItemProfile"] = "Menu Item";
	%this.label["menuContentProfile"] = "Menu Content";
	%this.label["popupProfile"] = "Popup";
	%this.label["pickerProfile"] = "Picker";
	%this.label["selectorProfile"] = "Selector";
	%this.label["valueProfile"] = "Value Boxes";
	%this.label["backgroundProfile"] = "Background";
}

function GuiEditorControlSpec::labelFor(%this, %field)
{
	%label = %this.label[%field];
	return (%label $= "") ? %field : %label;
}

//-----------------------------------------------------------------------------
// Drift guard. The editor never calls this; the smoke test does, so a control
// class added to the engine cannot quietly fall through to the unknown-class
// fallback without anyone noticing.
//
// %engineNames is what enumerateConsoleClasses("GuiControl") returns: a
// tab-separated list, whose first entry it repeats. Duplicates cost nothing
// here, since each name is only looked up. The classes the Gui Editor's palette
// refuses are not this table's business, so they are dropped with the same rule
// the palette uses (GuiEditorControlListWindow::populate).
//-----------------------------------------------------------------------------

function GuiEditorControlSpec::isPlaceableClass(%this, %name)
{
	if(%name $= "GuiCanvas" || %name $= "GuiMessageVectorCtrl" ||
		%name $= "GuiParticleGraphInspector" || %name $= "GuiGraphCtrl" ||
		%name $= "GuiSceneObjectCtrl")
	{
		return false;
	}
	if(%name !$= "SceneWindow" && getSubStr(%name, 0, 3) !$= "Gui")
	{
		return false;
	}
	return getSubStr(%name, 0, 10) !$= "GuiConsole" &&
		getSubStr(%name, 0, 7) !$= "GuiEdit" &&
		getSubStr(%name, 0, 12) !$= "GuiInspector";
}

function GuiEditorControlSpec::findMissingClasses(%this, %engineNames)
{
	%missing = "";
	%count = getFieldCount(%engineNames);
	for(%i = 0; %i < %count; %i++)
	{
		%name = trim(getField(%engineNames, %i));
		if(%name $= "" || !%this.isPlaceableClass(%name) || %this.textRole[%name] !$= "")
		{
			continue;
		}
		%missing = (%missing $= "") ? %name : (%missing SPC %name);
	}
	return %missing;
}
