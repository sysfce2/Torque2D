
//-----------------------------------------------------------------------------
// The live preview pane of the Gui Profile Editor. Shows a working sample
// control for the selected profile category (or a border probe for border
// categories) on a backdrop filled with the theme's background color, so
// hovering and clicking exercises the highlight and selected states.
//-----------------------------------------------------------------------------

function GuiProfileEditorPreview::onAdd(%this)
{
	// A scratch profile the preview owns. It is standalone, so editing it
	// never touches the theme override machinery.
	%this.borderProbeProfile = new GuiControlProfile();

	// The same tiled grid the Gui Editor uses behind the edited gui.
	%this.backdrop = new GuiSpriteCtrl()
	{
		HorizSizing = "width";
		VertSizing = "height";
		Position = "0 0";
		Extent = %this.getExtent();
		imageColor = "255 255 255 255";
		Image = "EditorCore:editorGrid";
		singleFrameBitmap = "1";
		tileImage = "1";
		positionOffset = "0 0";
		imageSize = "128 128";
		fullSize = "0";
		constrainProportions = "1";
	};
	ThemeManager.setProfile(%this.backdrop, "emptyProfile");
	%this.add(%this.backdrop);

	// An invisible holder for the current samples, kept centered in the
	// pane; layoutStage sizes it to the samples after each rebuild.
	%this.stage = new GuiControl()
	{
		HorizSizing = "center";
		VertSizing = "center";
		Position = "0 0";
		Extent = "200 200";
	};
	ThemeManager.setProfile(%this.stage, "emptyProfile");
	%this.backdrop.add(%this.stage);

	// A tiny object hierarchy for the TreeView sample to display.
	%this.previewTreeGroup = new SimGroup();
	%branch = new SimGroup();
	%branch.add(new ScriptObject());
	%branch.add(new ScriptObject());
	%this.previewTreeGroup.add(%branch);
	%this.previewTreeGroup.add(new ScriptObject());
}

function GuiProfileEditorPreview::onRemove(%this)
{
	// The sample controls must be gone before the scratch profile they may
	// wear is deleted; the backdrop and stage die with the control tree.
	%this.clearSamples();

	if(isObject(%this.borderProbeProfile))
	{
		%this.borderProbeProfile.delete();
	}
	if(isObject(%this.previewTreeGroup))
	{
		%this.previewTreeGroup.delete();
	}
}

function GuiProfileEditorPreview::clearSamples(%this)
{
	if(isObject(%this.stage))
	{
		%this.stage.deleteObjects();
	}
	%this.lastKind = "";
	%this.lastTheme = "";
	%this.lastCategory = "";
	%this.lastMember = "";
}

// Re-run the last show after a field edit. Most changes render live anyway
// since the samples reference the profile objects, but rebuilding also picks
// up structural changes like border reassignments.
function GuiProfileEditorPreview::refresh(%this)
{
	%kind = %this.lastKind;
	%theme = %this.lastTheme;
	%category = %this.lastCategory;
	%member = %this.lastMember;

	if(%kind $= "theme")
	{
		%this.showTheme(%theme);
	}
	else if(%kind $= "category")
	{
		%this.showCategory(%theme, %category, %member);
	}
	else if(%kind $= "border")
	{
		%this.showBorder(%theme, %member);
	}
}

// Size the stage to the bounding box of the current samples and center it
// in the pane, so every demonstration sits in the middle of the backdrop.
function GuiProfileEditorPreview::layoutStage(%this)
{
	%width = 0;
	%height = 0;
	for(%i = 0; %i < %this.stage.getCount(); %i++)
	{
		%sample = %this.stage.getObject(%i);
		%right = getWord(%sample.getPosition(), 0) + getWord(%sample.getExtent(), 0);
		%bottom = getWord(%sample.getPosition(), 1) + getWord(%sample.getExtent(), 1);
		if(%right > %width)
		{
			%width = %right;
		}
		if(%bottom > %height)
		{
			%height = %bottom;
		}
	}

	%this.stage.setExtent(%width, %height);

	%x = mFloor((getWord(%this.backdrop.getExtent(), 0) - %width) / 2);
	%y = mFloor((getWord(%this.backdrop.getExtent(), 1) - %height) / 2);
	%this.stage.setPosition(mGetMax(%x, 0), mGetMax(%y, 0));
}

// A profile slot for a sample: the selected member when it belongs to the
// slot's category, otherwise the theme's default for that category.
function GuiProfileEditorPreview::slotProfile(%this, %theme, %slotCategory, %selectedCategory, %member)
{
	if(%slotCategory $= %selectedCategory && isObject(%member))
	{
		return %member;
	}
	if(isObject(%theme))
	{
		%profile = %theme.getProfile(%slotCategory);
		if(isObject(%profile))
		{
			return %profile;
		}
	}
	return %member;
}

//-----------------------------------------------------------------------------
// Show methods, one per tree selection kind.
//-----------------------------------------------------------------------------

// The theme root: load the saved sample gui (a real, hand-authored layout) and
// re-skin every control with this theme's generated category profiles, so the
// preview shows an actual dialog rather than a hand-built mock. The sample ships
// wearing the AppCore default profiles; reskin swaps them for the theme's.
function GuiProfileEditorPreview::showTheme(%this, %theme)
{
	%this.clearSamples();
	if(!isObject(%theme))
	{
		return;
	}
	%this.lastKind = "theme";
	%this.lastTheme = %theme;

	// Absolute path from the repo root: a "./" path would resolve against this
	// script's own directory (editor/GuiEditor/scripts), not the game root.
	%file = makeFullPath("editor/GuiEditor/gui/theme_sample.gui.taml", getMainDotCsDir());
	%sample = TamlRead(%file);
	if(!isObject(%sample))
	{
		return;
	}

	// Anchor the sample top-left with a fixed size. Its saved sizing is
	// "center", which would recenter it to negative coords in the (initially
	// smaller) stage and make layoutStage compute a stage smaller than the
	// sample -- clipping it.
	%sample.HorizSizing = "right";
	%sample.VertSizing = "bottom";
	%sample.setPosition(0, 0);
	%this.reskin(%sample, %theme);
	%this.addSample(%sample);
	%this.layoutStage();
}

// Walk the sample tree and point every profile slot at the theme's matching
// category profile: the main Profile by control class, the secondary profiles
// (window content/buttons, scroll parts, dropdown list/background) by slot.
function GuiProfileEditorPreview::reskin(%this, %ctrl, %theme)
{
	if(!isObject(%ctrl))
	{
		return;
	}

	%category = %this.categoryForClass(%ctrl.getClassName());
	if(%category !$= "" && isObject(%theme.getProfile(%category)))
	{
		%ctrl.setEditFieldValue("Profile", %theme.getProfile(%category));
	}

	%this.reskinSlot(%ctrl, %theme, "contentProfile", "WindowContent");
	%this.reskinSlot(%ctrl, %theme, "closeButtonProfile", "WindowCloseButton");
	%this.reskinSlot(%ctrl, %theme, "minButtonProfile", "WindowButton");
	%this.reskinSlot(%ctrl, %theme, "maxButtonProfile", "WindowButton");
	%this.reskinSlot(%ctrl, %theme, "thumbProfile", "ScrollThumb");
	%this.reskinSlot(%ctrl, %theme, "trackProfile", "ScrollTrack");
	%this.reskinSlot(%ctrl, %theme, "arrowProfile", "ScrollArrow");
	%this.reskinSlot(%ctrl, %theme, "ScrollProfile", "Scroll");
	%this.reskinSlot(%ctrl, %theme, "listBoxProfile", "DropDownItem");
	%this.reskinSlot(%ctrl, %theme, "backgroundProfile", "Overlay");

	%this.fillSampleContent(%ctrl);

	for(%i = 0; %i < %ctrl.getCount(); %i++)
	{
		%this.reskin(%ctrl.getObject(%i), %theme);
	}
}

// Runtime-only content the saved TAML can't carry: dropdown items and a
// progress value. Runs on every (re)load, which is a fresh sample, so items
// never accumulate.
function GuiProfileEditorPreview::fillSampleContent(%this, %ctrl)
{
	%class = %ctrl.getClassName();
	if(%class $= "GuiDropDownCtrl")
	{
		%ctrl.clearItems();
		%ctrl.addItem("Item1");
		%ctrl.addItem("Item2");
		%ctrl.addItem("Item3");
		%ctrl.addItem("Item4");
		%ctrl.setSelected(0);
	}
	else if(%class $= "GuiProgressCtrl")
	{
		%ctrl.setProgress(0.6);
	}
}

// Point one secondary profile slot at a theme category, but only if the control
// actually uses that slot (so we never graft stray fields onto controls).
function GuiProfileEditorPreview::reskinSlot(%this, %ctrl, %theme, %field, %category)
{
	if(%ctrl.getFieldValue(%field) $= "")
	{
		return;
	}
	%profile = %theme.getProfile(%category);
	if(isObject(%profile))
	{
		%ctrl.setEditFieldValue(%field, %profile);
	}
}

function GuiProfileEditorPreview::categoryForClass(%this, %class)
{
	switch$(%class)
	{
		case "GuiWindowCtrl": return "Window";
		case "GuiButtonCtrl": return "Button";
		case "GuiCheckBoxCtrl": return "CheckBox";
		case "GuiRadioCtrl": return "Radio";
		case "GuiProgressCtrl": return "Progress";
		case "GuiScrollCtrl": return "Scroll";
		case "GuiTextEditCtrl": return "TextEdit";
		case "GuiDropDownCtrl": return "DropDown";
		case "GuiListBoxCtrl": return "ListBox";
		case "GuiTreeViewCtrl": return "TreeView";
		case "GuiTabBookCtrl": return "TabBook";
		case "GuiTabPageCtrl": return "TabPage";
		case "GuiSliderCtrl": return "Slider";
		case "GuiMenuBarCtrl": return "MenuBar";
		case "GuiChainCtrl": return "Empty";
		case "GuiControl": return "Label";
		default: return "";
	}
}

function GuiProfileEditorPreview::showCategory(%this, %theme, %category, %member)
{
	%this.clearSamples();
	if(!isObject(%member))
	{
		return;
	}
	%this.lastKind = "category";
	%this.lastTheme = %theme;
	%this.lastCategory = %category;
	%this.lastMember = %member;

	// Standalone profiles (no theme) get the generic sample: sibling slots
	// that would come from theme defaults aren't available.
	if(!isObject(%theme))
	{
		%this.addGenericSample(%member);
		%this.layoutStage();
		return;
	}

	if(%category $= "Button" || %category $= "CheckBox" || %category $= "Radio" || %category $= "TextEdit")
	{
		%this.addStateSamples(%category, %member);
	}
	else if(%category $= "Label")
	{
		%this.addSample(new GuiControl()
		{
			Position = "0 0";
			Extent = "340 30";
			Text = "The quick brown fox jumps over the lazy dog.";
			Profile = %member;
		});
	}
	else if(%category $= "Panel")
	{
		%this.addSample(new GuiPanelCtrl()
		{
			Position = "0 0";
			Extent = "240 140";
			Profile = %member;
		});
	}
	else if(%category $= "Scroll" || %category $= "ScrollTrack" || %category $= "ScrollThumb" || %category $= "ScrollArrow")
	{
		%scroll = new GuiScrollCtrl()
		{
			Position = "0 0";
			Extent = "240 160";
			hScrollBar = "alwaysOff";
			vScrollBar = "alwaysOn";
			constantThumbHeight = "0";
			showArrowButtons = "1";
			scrollBarThickness = "14";
			Profile = %this.slotProfile(%theme, "Scroll", %category, %member);
			TrackProfile = %this.slotProfile(%theme, "ScrollTrack", %category, %member);
			ThumbProfile = %this.slotProfile(%theme, "ScrollThumb", %category, %member);
			ArrowProfile = %this.slotProfile(%theme, "ScrollArrow", %category, %member);
		};
		%this.addSample(%scroll);
		%scroll.add(new GuiControl()
		{
			Position = "0 0";
			Extent = "200 400";
			Profile = %theme.getProfile("Empty");
		});
	}
	else if(%category $= "TabBook" || %category $= "Tab" || %category $= "TabPage")
	{
		%book = new GuiTabBookCtrl()
		{
			Position = "0 0";
			Extent = "260 160";
			Profile = %this.slotProfile(%theme, "TabBook", %category, %member);
			TabProfile = %this.slotProfile(%theme, "Tab", %category, %member);
		};
		%this.addSample(%book);
		%book.add(new GuiTabPageCtrl()
		{
			Text = "One";
			Profile = %this.slotProfile(%theme, "TabPage", %category, %member);
		});
		%book.add(new GuiTabPageCtrl()
		{
			Text = "Two";
			Profile = %this.slotProfile(%theme, "TabPage", %category, %member);
		});
	}
	else if(%category $= "ListBox")
	{
		%list = new GuiListBoxCtrl()
		{
			Position = "0 0";
			Extent = "220 140";
			Profile = %member;
		};
		%this.addSample(%list);
		%list.addItem("First item");
		%list.addItem("Second item");
		%list.addItem("Third item");
		%list.addItem("Fourth item");
	}
	else if(%category $= "DropDown" || %category $= "DropDownItem")
	{
		%dropDown = new GuiDropDownCtrl()
		{
			Position = "0 0";
			Extent = "220 30";
			Profile = %this.slotProfile(%theme, "DropDown", %category, %member);
			listBoxProfile = %this.slotProfile(%theme, "DropDownItem", %category, %member);
			backgroundProfile = %theme.getProfile("Overlay");
			scrollProfile = %theme.getProfile("Scroll");
			trackProfile = %theme.getProfile("ScrollTrack");
			thumbProfile = %theme.getProfile("ScrollThumb");
			arrowProfile = %theme.getProfile("ScrollArrow");
		};
		%this.addSample(%dropDown);
		%dropDown.addItem("First choice");
		%dropDown.addItem("Second choice");
		%dropDown.addItem("Third choice");
		%dropDown.setSelected(0);
	}
	else if(%category $= "Window" || %category $= "WindowContent" || %category $= "WindowButton" || %category $= "WindowCloseButton")
	{
		%this.addSample(new GuiWindowCtrl()
		{
			Position = "0 0";
			Extent = "260 160";
			Text = "Window";
			canMove = false;
			canClose = true;
			canMinimize = true;
			canMaximize = true;
			titleHeight = 30;
			Profile = %this.slotProfile(%theme, "Window", %category, %member);
			contentProfile = %this.slotProfile(%theme, "WindowContent", %category, %member);
			closeButtonProfile = %this.slotProfile(%theme, "WindowCloseButton", %category, %member);
			minButtonProfile = %this.slotProfile(%theme, "WindowButton", %category, %member);
			maxButtonProfile = %this.slotProfile(%theme, "WindowButton", %category, %member);
		});
	}
	else if(%category $= "MenuBar" || %category $= "Menu" || %category $= "MenuItem" || %category $= "MenuContent")
	{
		%menuBar = new GuiMenuBarCtrl()
		{
			Position = "0 0";
			Extent = "340 30";
			Profile = %this.slotProfile(%theme, "MenuBar", %category, %member);
			MenuProfile = %this.slotProfile(%theme, "Menu", %category, %member);
			MenuItemProfile = %this.slotProfile(%theme, "MenuItem", %category, %member);
			MenuContentProfile = %this.slotProfile(%theme, "MenuContent", %category, %member);
			backgroundProfile = %theme.getProfile("Overlay");

			new GuiMenuItemCtrl()
			{
				Text = "File";

				new GuiMenuItemCtrl() { Text = "New"; };
				new GuiMenuItemCtrl() { Text = "Open"; };
				new GuiMenuItemCtrl() { Text = "Open Recent"; };
				new GuiMenuItemCtrl() { Text = "-"; };
				new GuiMenuItemCtrl() { Text = "Save"; };
				new GuiMenuItemCtrl() { Text = "Save As..."; };
				new GuiMenuItemCtrl() { Text = "-"; };
				new GuiMenuItemCtrl() { Text = "Exit"; };
			};
			new GuiMenuItemCtrl()
			{
				Text = "Edit";

				new GuiMenuItemCtrl() { Text = "Undo"; };
				new GuiMenuItemCtrl() { Text = "Redo"; };
				new GuiMenuItemCtrl() { Text = "-"; };
				new GuiMenuItemCtrl() { Text = "Cut"; };
				new GuiMenuItemCtrl() { Text = "Copy"; };
				new GuiMenuItemCtrl() { Text = "Paste"; };
			};
			new GuiMenuItemCtrl()
			{
				Text = "View";

				new GuiMenuItemCtrl() { Text = "Zoom In"; };
				new GuiMenuItemCtrl() { Text = "Zoom Out"; };
				new GuiMenuItemCtrl() { Text = "Reset Zoom"; };
			};
		};
		%this.addSample(%menuBar);
	}
	else if(%category $= "Progress")
	{
		%progress = new GuiProgressCtrl()
		{
			Position = "0 0";
			Extent = "220 24";
			Profile = %member;
		};
		%this.addSample(%progress);
		%progress.setProgress(0.65);
	}
	else if(%category $= "TreeView")
	{
		%tree = new GuiTreeViewCtrl()
		{
			Position = "0 0";
			Extent = "220 140";
			Profile = %member;
		};
		%this.addSample(%tree);
		%tree.inspect(%this.previewTreeGroup);
	}
	else if(%category $= "FrameSet" || %category $= "FrameSetDropButton")
	{
		%frameSet = new GuiFrameSetCtrl()
		{
			Position = "0 0";
			Extent = "260 160";
			DividerThickness = 6;
			Profile = %this.slotProfile(%theme, "FrameSet", %category, %member);
			dropButtonProfile = %this.slotProfile(%theme, "FrameSetDropButton", %category, %member);
		};
		%this.addSample(%frameSet);
		%frameSet.createHorizontalSplit(1);
	}
	else if(%category $= "ColorPicker" || %category $= "ColorSelector")
	{
		%this.addSample(new GuiColorPickerCtrl()
		{
			Position = "0 0";
			Extent = "200 120";
			Profile = %this.slotProfile(%theme, "ColorPicker", %category, %member);
			SelectorProfile = %this.slotProfile(%theme, "ColorSelector", %category, %member);
		});
	}
	else if(%category $= "ColorPopup")
	{
		%this.addSample(new GuiColorPopupCtrl()
		{
			Position = "0 0";
			Extent = "60 30";
			Profile = %member;
			backgroundProfile = %theme.getProfile("Overlay");
			popupProfile = %theme.getProfile("Panel");
			pickerProfile = %theme.getProfile("ColorPicker");
			selectorProfile = %theme.getProfile("ColorSelector");
		});
	}
	else if(%category $= "Slider" || %category $= "SliderThumb")
	{
		// A horizontal and a vertical slider, so both groove orientations show.
		// The main profile styles the groove, thumbProfile styles the thumb.
		%this.addSample(new GuiSliderCtrl()
		{
			Position = "0 0";
			Extent = "220 30";
			range = "0 100";
			ticks = "10";
			value = "60";
			Profile = %this.slotProfile(%theme, "Slider", %category, %member);
			thumbProfile = %this.slotProfile(%theme, "SliderThumb", %category, %member);
		});

		%this.addSample(new GuiSliderCtrl()
		{
			Position = "0 50";
			Extent = "30 160";
			range = "0 100";
			ticks = "10";
			value = "40";
			Profile = %this.slotProfile(%theme, "Slider", %category, %member);
			thumbProfile = %this.slotProfile(%theme, "SliderThumb", %category, %member);
		});
	}
	else if(%category $= "Tooltip")
	{
		// Tooltips can't be hovered in the preview, so show the profile as a
		// static swatch (no button) with text that reaches toward the edges so
		// the fill, border, and padding all read.
		%this.addSample(new GuiControl()
		{
			Position = "0 0";
			Extent = "320 40";
			Text = "A short hint that describes a control.";
			Profile = %member;
		});
	}
	else
	{
		// Empty, Overlay, DragAndDrop, and anything new.
		%this.addGenericSample(%member);
	}

	%this.layoutStage();
}

function GuiProfileEditorPreview::showBorder(%this, %theme, %border)
{
	%this.clearSamples();
	if(!isObject(%border))
	{
		return;
	}
	%this.lastKind = "border";
	%this.lastTheme = %theme;
	%this.lastMember = %border;

	// A probe profile shows the border on a plain panel fill; the button
	// shape exercises the border's highlight and selected states.
	if(isObject(%theme))
	{
		%this.borderProbeProfile.fillColor = %theme.colorSurface;
		%this.borderProbeProfile.fontColor = %theme.colorForeground;
	}
	%this.borderProbeProfile.borderDefault = %border;

	%probe = new GuiButtonCtrl()
	{
		Position = "0 0";
		Extent = "200 60";
		Text = "Border sample";
		Profile = %this.borderProbeProfile;
	};
	%this.addSample(%probe);

	%disabled = new GuiButtonCtrl()
	{
		Position = "0 80";
		Extent = "200 60";
		Text = "Disabled";
		Profile = %this.borderProbeProfile;
	};
	%this.addSample(%disabled);
	%disabled.setActive(false);

	%this.layoutStage();
}

//-----------------------------------------------------------------------------
// Sample builders.
//-----------------------------------------------------------------------------

function GuiProfileEditorPreview::addSample(%this, %sample)
{
	%this.stage.add(%sample);
	return %sample;
}

// An interactive control plus a disabled twin, for the four-state categories.
function GuiProfileEditorPreview::addStateSamples(%this, %category, %member)
{
	if(%category $= "Button")
	{
		%this.addButtonSample(%member, 0);
	}
	else if(%category $= "CheckBox")
	{
		%active = new GuiCheckBoxCtrl()
		{
			Position = "0 0";
			Extent = "160 30";
			Text = "Check box";
			Profile = %member;
		};
		%this.addSample(%active);
		%active.setStateOn(true);

		%disabled = new GuiCheckBoxCtrl()
		{
			Position = "0 40";
			Extent = "160 30";
			Text = "Disabled";
			Profile = %member;
		};
		%this.addSample(%disabled);
		%disabled.setActive(false);
	}
	else if(%category $= "Radio")
	{
		// Several radios sharing a groupNum toggle exclusively; plus a disabled one.
		for(%i = 0; %i < 4; %i++)
		{
			%radio = new GuiRadioCtrl()
			{
				Position = "0" SPC (%i * 34);
				Extent = "180 30";
				Text = "Option" SPC (%i + 1);
				groupNum = "700";
				Profile = %member;
			};
			%this.addSample(%radio);
			if(%i == 0)
			{
				%radio.setStateOn(true);
			}
		}

		%disabled = new GuiRadioCtrl()
		{
			Position = "0" SPC (4 * 34);
			Extent = "180 30";
			Text = "Disabled";
			groupNum = "700";
			Profile = %member;
		};
		%this.addSample(%disabled);
		%disabled.setActive(false);
	}
	else if(%category $= "TextEdit")
	{
		%this.addSample(new GuiTextEditCtrl()
		{
			Position = "0 0";
			Extent = "200 30";
			Text = "Edit me";
			Profile = %member;
		});

		%disabled = new GuiTextEditCtrl()
		{
			Position = "0 40";
			Extent = "200 30";
			Text = "Disabled";
			Profile = %member;
		};
		%this.addSample(%disabled);
		%disabled.setActive(false);
	}
}

function GuiProfileEditorPreview::addButtonSample(%this, %profile, %y)
{
	%this.addSample(new GuiButtonCtrl()
	{
		Position = "0" SPC %y;
		Extent = "140 36";
		Text = "Button";
		Profile = %profile;
	});

	%disabled = new GuiButtonCtrl()
	{
		Position = "0" SPC (%y + 46);
		Extent = "140 36";
		Text = "Disabled";
		Profile = %profile;
	};
	%this.addSample(%disabled);
	%disabled.setActive(false);
}

// For profiles with no theme siblings: a plain container and a button, both
// wearing the profile, so fill, font, and borders all show somewhere.
function GuiProfileEditorPreview::addGenericSample(%this, %profile)
{
	%this.addSample(new GuiControl()
	{
		Position = "0 0";
		Extent = "340 80";
		Text = "The quick brown fox jumps over the lazy dog.";
		Profile = %profile;
	});

	%this.addButtonSample(%profile, 100);
}
