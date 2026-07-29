
//-----------------------------------------------------------------------------
// Puts a theme on a Gui: walks a control tree and fills every profile slot from
// the theme, choosing each slot's category from the control's class and the
// field's name. Owned by GuiEditor, which hands it the theme library.
//
// Every profile slot in the engine is a persist field of type GuiProfile -
// "Profile" itself, plus "contentProfile", "closeButtonProfile", "thumbProfile",
// "menuItemProfile" and the rest - so the walk asks each control for its field
// list and fills all of them rather than working from a hand-written slot map
// per class. Only the category each field wants is knowledge this file has to
// carry.
//-----------------------------------------------------------------------------

function GuiEditorThemeApplier::onAdd(%this)
{
	%this.buildFieldTable();
	%this.buildClassTable();
}

// What each named slot is for, independent of the control wearing it. "Profile"
// is the exception: it means "whatever this control is", so it defers to the
// class table.
function GuiEditorThemeApplier::buildFieldTable(%this)
{
	%this.setFieldCategory("tooltipProfile", "Tooltip");

	%this.setFieldCategory("contentProfile", "WindowContent");
	%this.setFieldCategory("closeButtonProfile", "WindowCloseButton");
	%this.setFieldCategory("minButtonProfile", "WindowButton");
	%this.setFieldCategory("maxButtonProfile", "WindowButton");

	%this.setFieldCategory("scrollProfile", "Scroll");
	%this.setFieldCategory("thumbProfile", "ScrollThumb");
	%this.setFieldCategory("trackProfile", "ScrollTrack");
	%this.setFieldCategory("arrowProfile", "ScrollArrow");

	// The list a drop-down opens, so its items rather than a plain list box - the
	// drop-down is the only control with this field.
	%this.setFieldCategory("listBoxProfile", "DropDownItem");
	%this.setFieldCategory("tabBookProfile", "TabBook");
	%this.setFieldCategory("tabProfile", "Tab");
	%this.setFieldCategory("tabPageProfile", "TabPage");
	%this.setFieldCategory("dropButtonProfile", "FrameSetDropButton");

	%this.setFieldCategory("menuProfile", "Menu");
	%this.setFieldCategory("menuItemProfile", "MenuItem");
	%this.setFieldCategory("menuContentProfile", "MenuContent");

	%this.setFieldCategory("popupProfile", "ColorPopup");
	%this.setFieldCategory("pickerProfile", "ColorPicker");
	%this.setFieldCategory("selectorProfile", "ColorSelector");

	// The full-screen catcher a drop-down, menu or color popup puts behind its
	// open list. It has to be invisible - Overlay is the deliberate dimming scrim
	// for modal dialogs, and dimming the game behind an open drop-down is not what
	// anyone means by this field.
	%this.setFieldCategory("backgroundProfile", "Empty");

	// Where a slot means something different depending on the control wearing it.
	// A slider's thumb is not a scrollbar's.
	%this.setCategoryFieldCategory("Slider", "thumbProfile", "SliderThumb");
}

// Which category a control's own Profile wants. Ordered most-derived first and
// matched with isMemberOfClass, so a subclass - script-side or a future C++ one -
// inherits its parent's answer without an entry of its own.
function GuiEditorThemeApplier::buildClassTable(%this)
{
	%this.classCount = 0;

	// Buttons: Radio derives from CheckBox derives from Button, and both the
	// drop-down and the color popup are buttons too. Order is load-bearing.
	%this.addClass("GuiRadioCtrl", "Radio");
	%this.addClass("GuiCheckBoxCtrl", "CheckBox");
	%this.addClass("GuiDropDownCtrl", "DropDown");
	%this.addClass("GuiColorPopupCtrl", "ColorPopup");
	%this.addClass("GuiButtonCtrl", "Button");

	// Lists: TreeView derives from ListBox.
	%this.addClass("GuiTreeViewCtrl", "TreeView");
	%this.addClass("GuiListBoxCtrl", "ListBox");

	%this.addClass("GuiTextEditCtrl", "TextEdit");
	%this.addClass("GuiScrollCtrl", "Scroll");
	%this.addClass("GuiTabBookCtrl", "TabBook");
	%this.addClass("GuiTabPageCtrl", "TabPage");
	%this.addClass("GuiWindowCtrl", "Window");
	%this.addClass("GuiMenuBarCtrl", "MenuBar");
	%this.addClass("GuiMenuItemCtrl", "MenuItem");
	%this.addClass("GuiProgressCtrl", "Progress");
	%this.addClass("GuiFrameSetCtrl", "FrameSet");
	%this.addClass("GuiColorPickerCtrl", "ColorPicker");
	%this.addClass("GuiDragAndDropCtrl", "DragAndDrop");
	%this.addClass("GuiSliderCtrl", "Slider");
	%this.addClass("GuiPanelCtrl", "Panel");
}

function GuiEditorThemeApplier::setFieldCategory(%this, %field, %category)
{
	%this.fieldCategory[strlwr(%field)] = %category;
}

// An answer that only applies to controls of one kind, keyed by the category the
// control itself takes - which is one per kind of control, so it doubles as the
// class key without a second class lookup.
function GuiEditorThemeApplier::setCategoryFieldCategory(%this, %mainCategory, %field, %category)
{
	%this.categoryField[%mainCategory, strlwr(%field)] = %category;
}

function GuiEditorThemeApplier::addClass(%this, %className, %category)
{
	%this.className[%this.classCount] = %className;
	%this.classCategory[%this.classCount] = %category;
	%this.classCount++;
}

//-----------------------------------------------------------------------------
// Applying.
//-----------------------------------------------------------------------------

// Theme every child of %parent (and everything below them), leaving %parent
// itself alone. The Gui Editor calls this with its simulated canvas, which is
// stage furniture rather than part of the Gui being authored.
function GuiEditorThemeApplier::applyToChildren(%this, %parent, %theme, %overrideStandalone)
{
	if(!isObject(%parent) || !isObject(%theme))
	{
		return 0;
	}

	%this.beginApply();
	%changed = 0;
	for(%i = 0; %i < %parent.getCount(); %i++)
	{
		%changed += %this.walk(%parent.getObject(%i), %theme, %overrideStandalone);
	}
	%this.endApply();

	return %changed;
}

// Theme %ctrl and everything below it. Used when a single control is dropped
// into an already-themed Gui.
function GuiEditorThemeApplier::applyToBranch(%this, %ctrl, %theme, %overrideStandalone)
{
	if(!isObject(%ctrl) || !isObject(%theme))
	{
		return 0;
	}

	%this.beginApply();
	%changed = %this.walk(%ctrl, %theme, %overrideStandalone);
	%this.endApply();

	return %changed;
}

// The loaded themes change only when the library loads or creates one, so the
// list is taken once per apply rather than per profile the walk has to place.
function GuiEditorThemeApplier::beginApply(%this)
{
	%this.themeList = %this.library.getThemes();
}

function GuiEditorThemeApplier::endApply(%this)
{
	%this.themeList = "";
}

function GuiEditorThemeApplier::walk(%this, %ctrl, %theme, %overrideStandalone)
{
	%changed = %this.applyToControl(%ctrl, %theme, %overrideStandalone);

	for(%i = 0; %i < %ctrl.getCount(); %i++)
	{
		%changed += %this.walk(%ctrl.getObject(%i), %theme, %overrideStandalone);
	}

	return %changed;
}

function GuiEditorThemeApplier::applyToControl(%this, %ctrl, %theme, %overrideStandalone)
{
	%isRoot = (%ctrl.getParent() == %this.rootContainer);
	%mainCategory = %this.categoryForControl(%ctrl, %isRoot);
	%changed = 0;

	%count = %ctrl.getFieldCount();
	for(%i = 0; %i < %count; %i++)
	{
		%field = %ctrl.getField(%i);
		if(%ctrl.getFieldType(%field) !$= "GuiProfile")
		{
			continue;
		}

		%current = %this.fieldProfile(%ctrl, %field);
		%owner = isObject(%current) ? %this.themeOf(%current) : 0;

		// Already wearing this theme. Whatever it is - the category's main
		// profile, a second button profile someone picked - it was chosen
		// deliberately and this is not the place to second-guess it.
		if(%owner == %theme && isObject(%owner))
		{
			continue;
		}

		if(isObject(%owner))
		{
			// From another theme: carry the category straight across, so a
			// control on that theme's WindowContent lands on this one's. What
			// the developer set outranks what this editor would have guessed.
			%category = %current.category;
		}
		else
		{
			// A script profile, one of AppCore's, or nothing at all. Stand-alone
			// profiles are the supported alternative to theming and are left
			// alone unless this apply was asked to override them.
			if(!%overrideStandalone && isObject(%current) && %this.library.isStandaloneProfile(%current))
			{
				continue;
			}

			%category = %this.categoryForField(%field, %mainCategory);
		}

		if(%category $= "")
		{
			// A slot on some control this editor has never heard of. Better to
			// leave whatever it holds than to guess.
			continue;
		}

		%target = %theme.getProfile(%category);
		if(!isObject(%target) || %current == %target)
		{
			continue;
		}

		// Through the Gui Editor's undo recorder, which does the write. Set Theme
		// fills a slot on every control in the document and that is one Ctrl+Z, so
		// GuiEditor::setTheme opens a transaction around the whole sweep; the
		// recorder is what collects the writes into it.
		//
		// setEditFieldValue, not a plain assignment: a profile field is a raw
		// field offset, so writing it does not touch reference counts. The
		// inspect-apply pair sleeps the control first and wakes it after, which
		// releases the old profile and takes a count on the new one - and, in the
		// editor, records the change for undo.
		//
		// By id, not by name: the Gui Editor runs the engine in editor mode, where
		// SimObject::assignName stashes a new object's name rather than
		// registering it (simObject.cc), so that naming a control being edited
		// does not create a global. A profile made during the session - an extra
		// added to a theme, a new stand-alone - therefore cannot be found by name
		// until it has been saved and reloaded. An id always resolves, and the
		// field still writes its name when the Gui is saved.
		GuiEditor.undoRecorder.writeField(%ctrl, %field, %target.getId());
		%changed++;
	}

	return %changed;
}

// The object in a profile field, or 0. The field reads back as a name (or an id
// string for an unnamed profile). A name the Sim cannot resolve is not
// necessarily a dead reference: in editor mode a profile created this session
// carries a name that was never registered (see the note in applyToControl), so
// fall back to searching what the library holds before giving up. Getting this
// wrong would read as "the slot is empty" and quietly overwrite a deliberate
// choice.
function GuiEditorThemeApplier::fieldProfile(%this, %ctrl, %field)
{
	%value = %ctrl.getFieldValue(%field);
	if(%value $= "")
	{
		return 0;
	}
	if(isObject(%value))
	{
		return %value.getId();
	}
	return %this.library.findProfileByName(%value);
}

// The theme a profile belongs to, or 0. A profile carries the category it was
// stamped for, which narrows the search to one list per theme; a profile with no
// category cannot be a member of anything.
function GuiEditorThemeApplier::themeOf(%this, %profile)
{
	%category = %profile.category;
	if(%category $= "")
	{
		return 0;
	}

	%themeCount = getWordCount(%this.themeList);
	for(%i = 0; %i < %themeCount; %i++)
	{
		%theme = getWord(%this.themeList, %i);
		%members = %theme.getProfiles(%category);
		for(%m = 0; %m < getWordCount(%members); %m++)
		{
			if(getWord(%members, %m) == %profile)
			{
				return %theme;
			}
		}
	}

	return 0;
}

//-----------------------------------------------------------------------------
// Detaching.
//-----------------------------------------------------------------------------

// Move every slot below %parent that wears one of the doomed profiles onto the
// engine's GuiDefaultProfile, which is the one profile that always exists.
//
// This is what keeps a Profile Editor session from taking the Gui down with it.
// Reverting or deleting a theme frees its member profiles, and a control's
// profile field is a raw pointer - nothing tells the control its profile is
// gone, and the dangling read surfaces much later, usually as a crash at exit.
// Pass either a theme (all of its members) or a single profile.
function GuiEditorThemeApplier::detach(%this, %parent, %theme, %profile)
{
	if(!isObject(%parent))
	{
		return 0;
	}

	%this.beginApply();
	%changed = 0;
	for(%i = 0; %i < %parent.getCount(); %i++)
	{
		%changed += %this.detachWalk(%parent.getObject(%i), %theme, %profile);
	}
	%this.endApply();

	return %changed;
}

function GuiEditorThemeApplier::detachWalk(%this, %ctrl, %theme, %profile)
{
	%changed = 0;

	%count = %ctrl.getFieldCount();
	for(%i = 0; %i < %count; %i++)
	{
		%field = %ctrl.getField(%i);
		if(%ctrl.getFieldType(%field) !$= "GuiProfile")
		{
			continue;
		}

		%current = %this.fieldProfile(%ctrl, %field);
		if(!isObject(%current))
		{
			continue;
		}

		%doomed = (isObject(%profile) && %current == %profile);
		if(!%doomed && isObject(%theme))
		{
			%doomed = (%this.themeOf(%current) == %theme);
		}
		if(!%doomed)
		{
			continue;
		}

		%ctrl.setEditFieldValue(%field, "GuiDefaultProfile");
		%changed++;
	}

	for(%i = 0; %i < %ctrl.getCount(); %i++)
	{
		%changed += %this.detachWalk(%ctrl.getObject(%i), %theme, %profile);
	}

	return %changed;
}

//-----------------------------------------------------------------------------
// Category lookup.
//-----------------------------------------------------------------------------

function GuiEditorThemeApplier::categoryForField(%this, %field, %mainCategory)
{
	%key = strlwr(%field);
	if(%key $= "profile")
	{
		return %mainCategory;
	}

	%override = %this.categoryField[%mainCategory, %key];
	if(%override !$= "")
	{
		return %override;
	}

	return %this.fieldCategory[%key];
}

// Empty is the answer for anything that is not a recognized control: it is the
// deliberate invisible wrapper, which is what a layout control wants and the
// least wrong thing for a control this editor does not know.
//
// A bare GuiControl is the interesting case, because in 4.0 it is both the
// wrapper and the text control. At the root of a Gui it is the backdrop the rest
// of the screen sits on, so it takes Panel. Below that, a line of text is a
// Label; no text, or text that wraps (a paragraph rather than a caption), is a
// wrapper.
function GuiEditorThemeApplier::categoryForControl(%this, %ctrl, %isRoot)
{
	if(%ctrl.getClassName() $= "GuiControl")
	{
		if(%isRoot)
		{
			return "Panel";
		}
		return (%ctrl.text !$= "" && !%ctrl.textWrap) ? "Label" : "Empty";
	}

	for(%i = 0; %i < %this.classCount; %i++)
	{
		if(%ctrl.isMemberOfClass(%this.className[%i]))
		{
			return %this.classCategory[%i];
		}
	}

	return "Empty";
}

//-----------------------------------------------------------------------------
// Reading a Gui's theme back.
//-----------------------------------------------------------------------------

// Which theme a control tree is already wearing, judged by the first themed
// profile found in it. Used when a Gui arrives without a recorded theme - one
// written before the field existed, or authored by hand.
function GuiEditorThemeApplier::inferTheme(%this, %parent)
{
	if(!isObject(%parent))
	{
		return 0;
	}

	%this.beginApply();
	%theme = %this.inferFrom(%parent);
	%this.endApply();

	return %theme;
}

function GuiEditorThemeApplier::inferFrom(%this, %ctrl)
{
	%count = %ctrl.getFieldCount();
	for(%i = 0; %i < %count; %i++)
	{
		%field = %ctrl.getField(%i);
		if(%ctrl.getFieldType(%field) !$= "GuiProfile")
		{
			continue;
		}

		%profile = %this.fieldProfile(%ctrl, %field);
		if(isObject(%profile))
		{
			%theme = %this.themeOf(%profile);
			if(isObject(%theme))
			{
				return %theme;
			}
		}
	}

	for(%i = 0; %i < %ctrl.getCount(); %i++)
	{
		%theme = %this.inferFrom(%ctrl.getObject(%i));
		if(isObject(%theme))
		{
			return %theme;
		}
	}

	return 0;
}
