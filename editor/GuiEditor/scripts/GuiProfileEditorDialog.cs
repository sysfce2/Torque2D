
//-----------------------------------------------------------------------------
// The Gui Profile Editor: a near-full-screen dialog showing a tree of every
// theme and standalone profile in the project's themes folder, a field
// editing pane for the selected member, and a live preview control. Edits apply
// immediately to the live objects; Save writes the dirty themes to their
// files, Cancel (or the window X) reverts dirty themes from their files.
// The themes themselves are owned by GuiEditor's persistent library and
// survive the dialog, so guis can reference their member profiles.
//-----------------------------------------------------------------------------

function GuiProfileEditorDialog::init(%this, %width, %height)
{
	%window = %this.getObject(0);
	%content = %window.getObject(0);

	// The Gui Editor runs with editorMode on, which shadow-names new objects
	// and would break theme member naming, border references, and TAML.
	// Turn it off while the dialog lives; onRemove restores it.
	editorMode(false);

	// The spawner hands us GuiEditor's persistent theme library; the dialog
	// only borrows it for this session.
	%this.library.dialog = %this;

	%this.toolbar = new GuiChainCtrl()
	{
		Class = "EditorButtonBar";
		Position = "6 4";
		Extent = "0 30";
		ChildSpacing = 4;
		IsVertical = false;
		Tool = %this;
	};
	ThemeManager.setProfile(%this.toolbar, "emptyProfile");
	%content.add(%this.toolbar);

	%this.toolbar.addButton("onNewTheme", 11, "New Theme", "");
	%this.toolbar.addButton("onRename", 49, "Rename Theme or Stand Alone Profile", "getRootSelected");
	%this.toolbar.addButton("onDelete", 23, "Delete Theme or Stand Alone Profile", "getRootSelected");
	%this.toolbar.addButton("onNewProfile", 25, "New Profile in Category", "getCategorySelected");
	%this.toolbar.addButton("onRemoveProfile", 23, "Remove Extra Profile", "getExtraSelected");
	%this.toolbar.addButton("onNewStandalone", 25, "New Stand Alone Profile", "");
	// Frame 22 is the circular revert arrow; frame 11 (a plus) was reading as
	// another "new" button next to the three that really are.
	%this.toolbar.addButton("onResetMember", 22, "Reset All Overrides on Member", "getMemberSelected");

	%paneTop = 40;
	%paneHeight = %height - 116;

	// The three sections live in a resizable frame set (like the main Gui
	// Editor): the user drags the dividers to resize the tree, the member
	// editor, and the preview. Adding more sections later is just another split.
	// A GuiFrameSetCtrl fills its parent, so it's wrapped in a plain container
	// positioned to the pane area -- otherwise it expands over the buttons.
	%this.framePane = new GuiControl()
	{
		HorizSizing = "width";
		VertSizing = "height";
		Position = "6" SPC %paneTop;
		Extent = (%width - 12) SPC %paneHeight;
	};
	ThemeManager.setProfile(%this.framePane, "emptyProfile");
	%content.add(%this.framePane);

	%this.frames = new GuiFrameSetCtrl()
	{
		HorizSizing = "width";
		VertSizing = "height";
		Position = "0 0";
		Extent = (%width - 12) SPC %paneHeight;
		DividerThickness = 6;
	};
	ThemeManager.setProfile(%this.frames, "frameSetProfile");
	ThemeManager.setProfile(%this.frames, "dropButtonProfile", "dropButtonProfile");
	ThemeManager.setProfile(%this.frames, "frameSetTabBookProfile", "tabBookProfile");
	ThemeManager.setProfile(%this.frames, "frameSetTabProfile", "tabProfile");
	ThemeManager.setProfile(%this.frames, "frameSetTabPageProfile", "tabPageProfile");
	%this.framePane.add(%this.frames);

	// tree | member editor | preview. The first child of a split is the anchored
	// (fixed-width) frame by default, so sizing the tree and member frames leaves
	// the preview to take the remaining space.
	%ids = %this.frames.createHorizontalSplit(1);
	%treeFrame = getWord(%ids, 0);
	%restFrame = getWord(%ids, 1);
	%this.frames.setFrameSize(%treeFrame, 240);

	// Kept on the dialog because the Properties pane's width is meaningful: the
	// profile form flows its fields into more columns as this frame widens.
	%ids = %this.frames.createHorizontalSplit(%restFrame);
	%this.memberFrame = getWord(%ids, 0);
	%previewFrame = getWord(%ids, 1);
	%this.frames.setFrameSize(%this.memberFrame, 400);

	// A fourth frame holds the Borders pane (shown only for profiles). Splitting
	// the preview frame keeps the order tree | Properties | Borders | preview.
	%ids = %this.frames.createHorizontalSplit(%previewFrame);
	%this.bordersFrame = getWord(%ids, 0);
	%previewFrame = getWord(%ids, 1);
	%this.frames.setFrameSize(%this.bordersFrame, 360);

	// The frame set docks children into empty frames in add-order (depth-first
	// through the splits), so add them tree -> member editor -> preview.

	//--- Frame 1: the theme/profile tree.
	%this.treeScroller = new GuiScrollCtrl()
	{
		HorizSizing = "width";
		VertSizing = "height";
		Position = "0 0";
		Extent = "240" SPC %paneHeight;
		hScrollBar = "alwaysOff";
		vScrollBar = "alwaysOn";
		constantThumbHeight = "0";
		showArrowButtons = "1";
		scrollBarThickness = "14";
	};
	ThemeManager.setProfile(%this.treeScroller, "emptyProfile");
	ThemeManager.setProfile(%this.treeScroller, "thumbProfile", "ThumbProfile");
	ThemeManager.setProfile(%this.treeScroller, "trackProfile", "TrackProfile");
	ThemeManager.setProfile(%this.treeScroller, "scrollArrowProfile", "ArrowProfile");
	%this.frames.add(%this.treeScroller);

	%this.tree = new GuiTreeViewCtrl()
	{
		class = "GuiProfileEditorTree";
		HorizSizing = "width";
		VertSizing = "height";
		Position = "0 0";
		Extent = "224" SPC %paneHeight;
		dialog = %this;
	};
	ThemeManager.setProfile(%this.tree, "treeViewProfile");
	%this.treeScroller.add(%this.tree);

	//--- Frame 2: the member editor, wrapped in a window so it can be dragged
	// out of the frame set. The three custom forms -- profile, theme and border --
	// share this window; onTreeSelect toggles which one is visible.
	%this.memberWindow = new GuiWindowCtrl()
	{
		HorizSizing = "width";
		VertSizing = "height";
		Position = "0 0";
		Extent = "400" SPC %paneHeight;
		MinExtent = "200 100";
		text = "Properties";
		canMove = true;
		canClose = false;
		canMinimize = false;
		canMaximize = false;
		resizeWidth = true;
		resizeHeight = true;
	};
	ThemeManager.setProfile(%this.memberWindow, "windowProfile");
	ThemeManager.setProfile(%this.memberWindow, "windowContentProfile", "ContentProfile");
	ThemeManager.setProfile(%this.memberWindow, "windowButtonProfile", "CloseButtonProfile");
	ThemeManager.setProfile(%this.memberWindow, "windowButtonProfile", "MinButtonProfile");
	ThemeManager.setProfile(%this.memberWindow, "windowButtonProfile", "MaxButtonProfile");
	%this.frames.add(%this.memberWindow);

	// The custom profile pane: it replaced the generic GuiInspector for profile
	// nodes, so the member window now holds three custom forms and no inspector.
	// It shows only the fields the selected profile's category actually uses --
	// see GuiProfileEditorProfileForm and GuiProfileEditorFieldSpec.
	// Starts hidden like the other two panes: nothing is selected yet, so there is
	// no profile to show. onTreeSelect brings whichever pane the selection calls
	// for. (The inspector this replaced started visible and got away with it by
	// rendering as an empty box; this pane always draws its header and
	// essentials, so an unbound one looks like a real profile.)
	%this.profileFormScroller = new GuiScrollCtrl()
	{
		HorizSizing = "fill";
		VertSizing = "fill";
		Position = "0 0";
		Extent = "400" SPC %paneHeight;
		hScrollBar = "alwaysOff";
		vScrollBar = "alwaysOn";
		constantThumbHeight = "0";
		showArrowButtons = "1";
		scrollBarThickness = "14";
		Visible = false;
	};
	ThemeManager.setProfile(%this.profileFormScroller, "emptyProfile");
	ThemeManager.setProfile(%this.profileFormScroller, "thumbProfile", "ThumbProfile");
	ThemeManager.setProfile(%this.profileFormScroller, "trackProfile", "TrackProfile");
	ThemeManager.setProfile(%this.profileFormScroller, "scrollArrowProfile", "ArrowProfile");
	%this.memberWindow.add(%this.profileFormScroller);

	%this.profileForm = new GuiChainCtrl()
	{
		class = "GuiProfileEditorProfileForm";
		HorizSizing = "width";
		Position = "0 0";
		Extent = "386" SPC %paneHeight;
		IsVertical = true;
		ChildSpacing = 6;
		formWidth = 386;
		dialog = %this;
	};
	%this.profileFormScroller.add(%this.profileForm);
	%this.profileForm.build();

	// The custom theme form takes the member pane whenever a theme (rather than a
	// member) is selected. onTreeSelect swaps which of the three scrollers is
	// visible.
	%this.formScroller = new GuiScrollCtrl()
	{
		HorizSizing = "fill";
		VertSizing = "fill";
		Position = "0 0";
		Extent = "400" SPC %paneHeight;
		hScrollBar = "alwaysOff";
		vScrollBar = "dynamic";
		constantThumbHeight = "0";
		showArrowButtons = "1";
		scrollBarThickness = "14";
		Visible = false;
	};
	ThemeManager.setProfile(%this.formScroller, "emptyProfile");
	ThemeManager.setProfile(%this.formScroller, "thumbProfile", "ThumbProfile");
	ThemeManager.setProfile(%this.formScroller, "trackProfile", "TrackProfile");
	ThemeManager.setProfile(%this.formScroller, "scrollArrowProfile", "ArrowProfile");
	%this.memberWindow.add(%this.formScroller);

	%this.themeForm = new GuiGridCtrl()
	{
		class = "ProfileThemeEditForm";
		superclass = "EditorForm";
		HorizSizing = "width";
		VertSizing = "height";
		Position = "0 0";
		Extent = "386" SPC %paneHeight;
		cellSizeX = 356;
		cellSizeY = 50;
		dialog = %this;
	};
	%this.themeForm.addListener(%this.themeForm);
	%this.themeForm.build();
	%this.formScroller.add(%this.themeForm);

	// The custom border form is the third pane sharing the member window: it
	// takes over whenever a border node (rather than a profile) is selected.
	// onTreeSelect swaps which of the three scrollers is visible.
	%this.borderFormScroller = new GuiScrollCtrl()
	{
		HorizSizing = "fill";
		VertSizing = "fill";
		Position = "0 0";
		Extent = "400" SPC %paneHeight;
		hScrollBar = "alwaysOff";
		vScrollBar = "dynamic";
		constantThumbHeight = "0";
		showArrowButtons = "1";
		scrollBarThickness = "14";
		Visible = false;
	};
	ThemeManager.setProfile(%this.borderFormScroller, "emptyProfile");
	ThemeManager.setProfile(%this.borderFormScroller, "thumbProfile", "ThumbProfile");
	ThemeManager.setProfile(%this.borderFormScroller, "trackProfile", "TrackProfile");
	ThemeManager.setProfile(%this.borderFormScroller, "scrollArrowProfile", "ArrowProfile");
	%this.memberWindow.add(%this.borderFormScroller);

	%this.borderForm = new GuiControl()
	{
		class = "GuiProfileEditorBorderForm";
		HorizSizing = "fill";
		VertSizing = "fill";
		Position = "0 0";
		Extent = "386" SPC %paneHeight;
		dialog = %this;
	};
	%this.borderForm.build();
	%this.borderFormScroller.add(%this.borderForm);

	//--- Frame 3: the Borders pane -- a movable window of five border setters,
	// shown only while a profile is selected (onTreeSelect toggles it). Added
	// before the preview so it docks into the Borders frame.
	%this.bordersWindow = new GuiWindowCtrl()
	{
		HorizSizing = "width";
		VertSizing = "height";
		Position = "0 0";
		Extent = "360" SPC %paneHeight;
		MinExtent = "220 120";
		text = "Borders";
		canMove = true;
		canClose = false;
		canMinimize = false;
		canMaximize = false;
		resizeWidth = true;
		resizeHeight = true;
	};
	ThemeManager.setProfile(%this.bordersWindow, "windowProfile");
	ThemeManager.setProfile(%this.bordersWindow, "windowContentProfile", "ContentProfile");
	ThemeManager.setProfile(%this.bordersWindow, "windowButtonProfile", "CloseButtonProfile");
	ThemeManager.setProfile(%this.bordersWindow, "windowButtonProfile", "MinButtonProfile");
	ThemeManager.setProfile(%this.bordersWindow, "windowButtonProfile", "MaxButtonProfile");
	%this.frames.add(%this.bordersWindow);

	%this.bordersScroller = new GuiScrollCtrl()
	{
		HorizSizing = "fill";
		VertSizing = "fill";
		Position = "0 0";
		Extent = "360" SPC %paneHeight;
		hScrollBar = "alwaysOff";
		vScrollBar = "dynamic";
		constantThumbHeight = "0";
		showArrowButtons = "1";
		scrollBarThickness = "14";
	};
	ThemeManager.setProfile(%this.bordersScroller, "emptyProfile");
	ThemeManager.setProfile(%this.bordersScroller, "thumbProfile", "ThumbProfile");
	ThemeManager.setProfile(%this.bordersScroller, "trackProfile", "TrackProfile");
	ThemeManager.setProfile(%this.bordersScroller, "scrollArrowProfile", "ArrowProfile");
	%this.bordersWindow.add(%this.bordersScroller);

	%this.borderChain = new GuiChainCtrl()
	{
		HorizSizing = "width";
		VertSizing = "height";
		Position = "0 0";
		Extent = "346" SPC %paneHeight;
		IsVertical = true;
		ChildSpacing = 8;
	};
	ThemeManager.setProfile(%this.borderChain, "emptyProfile");
	%this.bordersScroller.add(%this.borderChain);

	%this.buildBorderSetters();

	//--- Frame 4: the live preview.
	%this.preview = new GuiControl()
	{
		class = "GuiProfileEditorPreview";
		HorizSizing = "width";
		VertSizing = "height";
		Position = "0 0";
		Extent = "400" SPC %paneHeight;
		dialog = %this;
	};
	ThemeManager.setProfile(%this.preview, "emptyProfile");
	%this.frames.add(%this.preview);

	// The frame set only lays its frames out on a resize event; the dialog is
	// created at its final size, so none fires and the frames stay collapsed
	// until the user drags a divider. Force one layout pass now.
	%this.frames.resize(0, 0, %width - 12, %paneHeight);

	// The Borders pane starts collapsed; onTreeSelect opens it for profiles.
	%this.hideBorders();

	%this.cancelButton = new GuiButtonCtrl()
	{
		HorizSizing = "right";
		VertSizing = "bottom";
		Position = (%width - 226) SPC (%height - 64);
		Extent = "100 30";
		Text = "Cancel";
		Command = %this.getID() @ ".onCancel();";
	};
	ThemeManager.setProfile(%this.cancelButton, "buttonProfile");
	%content.add(%this.cancelButton);

	%this.saveButton = new GuiButtonCtrl()
	{
		HorizSizing = "right";
		VertSizing = "bottom";
		Position = (%width - 116) SPC (%height - 66);
		Extent = "100 34";
		Text = "Save";
		Command = %this.getID() @ ".onSave();";
	};
	ThemeManager.setProfile(%this.saveButton, "primaryButtonProfile");
	%content.add(%this.saveButton);

	%this.library.scanThemes();
	%this.tree.inspect(%this.library.proxyRoot);
	%this.toolbar.refreshEnabled();
}

function GuiProfileEditorDialog::onRemove(%this)
{
	// Nothing in the dying dialog may keep referencing theme members.
	if(isObject(%this.preview))
	{
		%this.preview.clearSamples();
	}
	if(isObject(%this.profileForm))
	{
		%this.profileForm.unbind();
	}
	if(isObject(%this.themeForm))
	{
		%this.themeForm.unbind();
	}
	if(isObject(%this.borderForm))
	{
		%this.borderForm.unbind();
	}
	if(isObject(%this.borderChain))
	{
		%this.unbindBorderSetters();
	}

	// The library persists on GuiEditor; just detach from it.
	if(isObject(%this.library))
	{
		%this.library.dialog = "";
	}

	editorMode(true);
}

//-----------------------------------------------------------------------------
// Selection.
//-----------------------------------------------------------------------------

function GuiProfileEditorDialog::onTreeSelect(%this, %proxy)
{
	%this.currentProxy = %proxy;
	%this.currentRoot = "";
	%this.currentMember = "";

	%kind = %proxy.kind;
	if(%kind $= "theme")
	{
		%this.currentRoot = %proxy.target;
		%this.currentMember = %proxy.target;
	}
	else if(%kind $= "category")
	{
		%this.currentRoot = %proxy.theme;
		%this.currentMember = %proxy.theme.getProfile(%proxy.category);
	}
	else if(%kind $= "border")
	{
		%this.currentRoot = %proxy.theme;
		%this.currentMember = %proxy.theme.getBorder(%proxy.category);
	}
	else if(%kind $= "extra")
	{
		%this.currentRoot = %proxy.theme;
		%this.currentMember = %proxy.target;
	}
	else if(%kind $= "standalone")
	{
		%this.currentRoot = isObject(%proxy.root) ? %proxy.root : %proxy.target;
		%this.currentMember = %proxy.target;
	}

	// The Properties window holds three custom panes, one per node kind: a theme
	// gets the theme form, a border gets the border form, and a profile gets the
	// profile form.
	if(%this.isHeaderKind(%kind))
	{
		// Header rows ("Gui Themes", "Profiles", "Borders", "Stand Alone") are
		// grouping labels with nothing to edit. Every pane hides, so the window
		// goes empty instead of leaving the last profile's rows on screen -
		// unbind() only drops the binding, it does not clear what is drawn.
		%this.hideMemberPanes();
	}
	else if(%kind $= "theme")
	{
		%this.hideMemberPanes();
		%this.formScroller.setVisible(true);
		%this.themeForm.bindTheme(%this.currentMember);
	}
	else if(%kind $= "border")
	{
		%this.hideMemberPanes();
		%this.borderFormScroller.setVisible(true);
		%this.borderForm.bind(%this.currentMember, %proxy.treeLabel);
	}
	else
	{
		%this.hideMemberPanes();
		%this.profileFormScroller.setVisible(true);
		%this.profileForm.bind(%this.currentMember, %kind);
	}

	// The Borders pane rides alongside the profile form, but only for profiles.
	if(%this.isProfileKind(%kind))
	{
		%this.showBorders();
	}
	else
	{
		%this.hideBorders();
	}

	%this.updatePreview();
	%this.toolbar.refreshEnabled();
}

function GuiProfileEditorDialog::updatePreview(%this)
{
	if(!isObject(%this.preview))
	{
		return;
	}

	%proxy = %this.currentProxy;
	%kind = isObject(%proxy) ? %proxy.kind : "";

	if(%kind $= "theme")
	{
		%this.preview.showTheme(%proxy.target);
	}
	else if(%kind $= "category")
	{
		%this.preview.showCategory(%proxy.theme, %proxy.category, %this.currentMember);
	}
	else if(%kind $= "extra")
	{
		%this.preview.showCategory(%proxy.theme, %proxy.category, %proxy.target);
	}
	else if(%kind $= "border")
	{
		%this.preview.showBorder(%proxy.theme, %this.currentMember);
	}
	else if(%kind $= "standalone")
	{
		%this.preview.showCategory("", %proxy.target.category, %proxy.target);
	}
	else
	{
		%this.preview.clearSamples();
	}
}

// The profile form reports every applied field edit here.
function GuiProfileEditorDialog::onProfileChanged(%this, %object)
{
	if(isObject(%this.currentRoot))
	{
		%this.library.markDirty(%this.currentRoot);
	}
	%this.schedulePreviewRefresh();
}

// Rebuild the preview on the next tick instead of right now, coalescing rapid
// edits. A field commit can arrive from inside an input-event/focus-change
// callback -- e.g. a border or profile-form text box losing first-responder while
// the user clicks a live preview sample. Rebuilding there would delete the very
// sample control the engine is mid-dispatch on, freeing it under its own
// onTouchDown/setFirstResponder (a use-after-free crash). Deferring runs the
// rebuild only after the current event has fully unwound.
function GuiProfileEditorDialog::schedulePreviewRefresh(%this)
{
	if(!isObject(%this.preview))
	{
		return;
	}
	if(isEventPending(%this.previewRefreshEvent))
	{
		cancel(%this.previewRefreshEvent);
	}
	%this.previewRefreshEvent = %this.schedule(0, "doPreviewRefresh");
}

function GuiProfileEditorDialog::doPreviewRefresh(%this)
{
	%this.previewRefreshEvent = "";
	if(isObject(%this.preview))
	{
		%this.preview.refresh();
	}
}

//-----------------------------------------------------------------------------
// Borders pane.
//-----------------------------------------------------------------------------

function GuiProfileEditorDialog::isProfileKind(%this, %kind)
{
	return %kind $= "category" || %kind $= "extra" || %kind $= "standalone";
}

// The tree's grouping rows: the "Gui Themes" root and the "Stand Alone",
// "Profiles" and "Borders" folders. They carry no editable target.
function GuiProfileEditorDialog::isHeaderKind(%this, %kind)
{
	return %kind $= "root" || %kind $= "folder";
}

// Drops all three member panes out of the Properties window. Each pane is also
// unbound so it stops tracking whatever it last showed.
function GuiProfileEditorDialog::hideMemberPanes(%this)
{
	%this.profileFormScroller.setVisible(false);
	%this.profileForm.unbind();
	%this.formScroller.setVisible(false);
	%this.themeForm.unbind();
	%this.borderFormScroller.setVisible(false);
	%this.borderForm.unbind();
}

// Build the five setters into the pane chain: the default (no checkbox, full
// width) then the four sides (checkbox, indented 20px so they read as "under"
// the default).
function GuiProfileEditorDialog::buildBorderSetters(%this)
{
	%w = 340;
	%slots = "default" TAB "top" TAB "bottom" TAB "left" TAB "right";
	%titles = "Default" TAB "Top" TAB "Bottom" TAB "Left" TAB "Right";

	for(%i = 0; %i < 5; %i++)
	{
		%slot = getField(%slots, %i);
		%isDefault = %slot $= "default";
		%indent = %isDefault ? 0 : 20;

		%setter = new GuiChainCtrl()
		{
			class = "GuiProfileEditorBorderSetter";
			Position = %indent SPC 0;
			slot = %slot;
			slotTitle = getField(%titles, %i);
			hasCheckbox = !%isDefault;
			setterWidth = %w - %indent;
			dialog = %this;
		};
		%this.borderChain.add(%setter);
		%this.borderSetter[%slot] = %setter;
	}
}

function GuiProfileEditorDialog::bindBorderSetters(%this)
{
	if(!isObject(%this.currentMember))
	{
		return;
	}
	%container = %this.currentRoot;

	// The default binds first so the sides can grey out the border it resolves to.
	%this.borderSetter["default"].bind(%this.currentMember, %container, "");
	%disable = %this.borderSetter["default"].currentCategory();

	%sides = "top" TAB "bottom" TAB "left" TAB "right";
	for(%i = 0; %i < 4; %i++)
	{
		%this.borderSetter[getField(%sides, %i)].bind(%this.currentMember, %container, %disable);
	}
}

function GuiProfileEditorDialog::unbindBorderSetters(%this)
{
	%slots = "default" TAB "top" TAB "bottom" TAB "left" TAB "right";
	for(%i = 0; %i < 5; %i++)
	{
		%setter = %this.borderSetter[getField(%slots, %i)];
		if(isObject(%setter))
		{
			%setter.unbind();
		}
	}
}

// After the Default setter's border changes, update which border each side list
// greys out (the one the default now resolves to).
function GuiProfileEditorDialog::refreshSideDisables(%this)
{
	if(!isObject(%this.borderSetter["default"]))
	{
		return;
	}
	%disable = %this.borderSetter["default"].currentCategory();
	%sides = "top" TAB "bottom" TAB "left" TAB "right";
	for(%i = 0; %i < 4; %i++)
	{
		%setter = %this.borderSetter[getField(%sides, %i)];
		%setter.clearIfMatches(%disable);
		%setter.applyDisable(%disable);
	}
}

function GuiProfileEditorDialog::showBorders(%this)
{
	%this.frames.setFrameSize(%this.bordersFrame, 360);
	%this.bordersWindow.setVisible(true);
	%this.bindBorderSetters();
}

function GuiProfileEditorDialog::hideBorders(%this)
{
	%this.unbindBorderSetters();
	if(isObject(%this.bordersWindow))
	{
		%this.bordersWindow.setVisible(false);
	}
	%this.frames.setFrameSize(%this.bordersFrame, 0);
}

// The theme's six named border categories for the dropdowns; a standalone
// bundle (no theme) has none.
function GuiProfileEditorDialog::borderNamesFor(%this, %container)
{
	if(isObject(%container) && %container.getClassName() $= "GuiProfileTheme")
	{
		return %container.getBorderCategoryNames();
	}
	return "";
}

function GuiProfileEditorDialog::borderObjectFor(%this, %container, %category)
{
	if(isObject(%container) && %container.getClassName() $= "GuiProfileTheme")
	{
		return %container.getBorder(%category);
	}
	return "";
}

// Create a single-use custom border owned by the container (the caller seeds it).
// Named outside editor mode, like everything else the library names, so the name
// a profile stores for its border actually resolves (see beginNaming).
function GuiProfileEditorDialog::createCustomBorder(%this, %container, %name)
{
	if(isObject(%container) && %container.getClassName() $= "GuiProfileTheme")
	{
		%this.library.beginNaming();
		%border = %container.createBorder(%name);
		%this.library.endNaming();
		return %border;
	}
	// Standalone bundle: a plain custom border added to the bundle, kept ahead
	// of the profile so the default's object reference resolves on reload.
	%this.library.beginNaming();
	%border = new GuiBorderProfile(%name) { isCustom = true; };
	%this.library.endNaming();

	if(%this.library.isBundle(%container))
	{
		%container.add(%border);
		if(isObject(%this.currentMember))
		{
			%container.pushToBack(%this.currentMember);
		}
	}
	return %border;
}

function GuiProfileEditorDialog::onBorderChanged(%this)
{
	if(isObject(%this.currentRoot))
	{
		%this.library.markDirty(%this.currentRoot);
	}
	%this.schedulePreviewRefresh();
}

//-----------------------------------------------------------------------------
// Toolbar enabled-state callbacks.
//-----------------------------------------------------------------------------

// The two node kinds that are a save root: a theme and a stand-alone profile
// each carry a name of their own and own a file of their own, which is exactly
// what Rename and Delete need. Everything else in the tree belongs to one of
// them - a theme member takes the name its theme gives it and is written into
// the theme's file.
function GuiProfileEditorDialog::getRootSelected(%this)
{
	if(!isObject(%this.currentProxy))
	{
		return false;
	}
	%kind = %this.currentProxy.kind;
	return %kind $= "theme" || %kind $= "standalone";
}

// The save root the selection stands for: a theme is its own, a stand-alone
// profile's is the bundle its file holds.
function GuiProfileEditorDialog::selectedRoot(%this)
{
	if(!%this.getRootSelected())
	{
		return 0;
	}
	return (%this.currentProxy.kind $= "theme") ? %this.currentProxy.target : %this.currentProxy.root;
}

function GuiProfileEditorDialog::getCategorySelected(%this)
{
	return isObject(%this.currentProxy) && %this.currentProxy.kind $= "category";
}

function GuiProfileEditorDialog::getExtraSelected(%this)
{
	return isObject(%this.currentProxy) && %this.currentProxy.kind $= "extra";
}

function GuiProfileEditorDialog::getMemberSelected(%this)
{
	if(!isObject(%this.currentProxy))
	{
		return false;
	}
	%kind = %this.currentProxy.kind;
	return %kind $= "category" || %kind $= "border" || %kind $= "extra";
}

//-----------------------------------------------------------------------------
// Toolbar operations.
//-----------------------------------------------------------------------------

function GuiProfileEditorDialog::onNewTheme(%this)
{
	%this.openNameDialog("New Theme", "doCreateTheme", "");
}

function GuiProfileEditorDialog::doCreateTheme(%this, %name)
{
	%theme = %this.library.createTheme(%name);
	if(isObject(%theme))
	{
		%this.tree.refresh();
	}
}

// One button for both things that carry a name of their own: a theme, and a
// stand-alone profile. A theme member has no name to rename - it takes the one
// its theme gives it.
function GuiProfileEditorDialog::onRename(%this)
{
	if(!%this.getRootSelected())
	{
		return;
	}

	%isTheme = %this.currentProxy.kind $= "theme";
	%title = %isTheme ? "Rename Theme" : "Rename Stand Alone Profile";
	%this.openNameDialog(%title, "doRename", %this.currentProxy.target.getName());
}

function GuiProfileEditorDialog::doRename(%this, %name)
{
	if(!%this.getRootSelected())
	{
		return;
	}

	%proxy = %this.currentProxy;
	if(%proxy.kind $= "theme")
	{
		if(!%this.library.renameThemeTo(%proxy.target, %name))
		{
			return;
		}
		// The theme form is the pane showing for a theme node; refresh
		// its Name label to the new name.
		%this.themeForm.bindTheme(%proxy.target);
	}
	else
	{
		if(!%this.library.renameStandaloneTo(%proxy.root, %name))
		{
			return;
		}
		// Likewise the profile pane, whose header names the profile.
		%this.profileForm.bind(%proxy.target, %proxy.kind);
	}

	%this.tree.refresh();
}

function GuiProfileEditorDialog::onDelete(%this)
{
	if(!%this.getRootSelected())
	{
		return;
	}

	%name = %this.currentProxy.target.getName();
	if(%this.currentProxy.kind $= "theme")
	{
		%title = "Delete Theme";
		%message = "Delete the theme \"" @ %name @ "\" and all of its profiles? The file is removed when you save.";
	}
	else
	{
		// Nothing can list the Guis that name this profile, so say plainly what
		// becomes of them rather than pretending to have checked.
		%title = "Delete Stand Alone Profile";
		%message = "Delete the stand alone profile \"" @ %name @ "\"? The file is removed when you save, and any control still asking for it falls back to the default profile.";
	}

	%this.openConfirmDialog(%title, %message, "Delete", "doDelete");
}

function GuiProfileEditorDialog::doDelete(%this)
{
	%root = %this.selectedRoot();
	if(!isObject(%root))
	{
		return;
	}

	%isTheme = %this.currentProxy.kind $= "theme";

	// Detach everything that renders or inspects what is about to die: the
	// preview samples wear these profiles, the member panes are bound to one,
	// and the Borders pane's setters hold both the profile and its container.
	%this.preview.clearSamples();
	%this.hideMemberPanes();
	%this.hideBorders();
	%this.currentProxy = "";
	%this.currentRoot = "";
	%this.currentMember = "";

	if(%isTheme)
	{
		%this.library.deleteTheme(%root);
	}
	else
	{
		%this.library.deleteStandalone(%root);
	}

	%this.tree.refresh();
	%this.toolbar.refreshEnabled();
}

function GuiProfileEditorDialog::onNewProfile(%this)
{
	if(!%this.getCategorySelected())
	{
		return;
	}
	%profile = %this.library.createExtraProfile(%this.currentProxy.theme, %this.currentProxy.category);
	if(isObject(%profile))
	{
		%this.tree.refresh();
	}
}

function GuiProfileEditorDialog::onRemoveProfile(%this)
{
	if(!%this.getExtraSelected())
	{
		return;
	}
	%theme = %this.currentProxy.theme;
	%profile = %this.currentProxy.target;

	%this.preview.clearSamples();
	%this.profileForm.unbind();
	%this.currentProxy = "";
	%this.currentRoot = "";
	%this.currentMember = "";

	%this.library.removeExtraProfile(%theme, %profile);
	%this.tree.refresh();
	%this.toolbar.refreshEnabled();
}

function GuiProfileEditorDialog::onNewStandalone(%this)
{
	%this.openNameDialog("New Stand Alone Profile", "doCreateStandalone", "");
}

function GuiProfileEditorDialog::doCreateStandalone(%this, %name)
{
	%profile = %this.library.createStandalone(%name);
	if(isObject(%profile))
	{
		%this.tree.refresh();
	}
}

function GuiProfileEditorDialog::onResetMember(%this)
{
	if(!%this.getMemberSelected() || !isObject(%this.currentMember))
	{
		return;
	}

	%this.currentRoot.resetProfile(%this.currentMember);

	// Reload whichever pane is showing the member the overrides were cleared on.
	if(%this.currentProxy.kind $= "border")
	{
		%this.borderForm.bind(%this.currentMember, %this.currentProxy.treeLabel);
	}
	else
	{
		%this.profileForm.refresh();
	}

	%this.library.markDirty(%this.currentRoot);
	%this.updatePreview();
}

//-----------------------------------------------------------------------------
// Helper dialogs.
//-----------------------------------------------------------------------------

function GuiProfileEditorDialog::openNameDialog(%this, %title, %callback, %defaultName)
{
	%width = 400;
	%height = 130;
	%dialog = new GuiControl()
	{
		class = "GuiProfileEditorNameDialog";
		superclass = "EditorDialog";
		dialogSize = (%width + 8) SPC (%height + 8);
		dialogCanClose = true;
		dialogText = %title;
		callbackTarget = %this;
		callbackMethod = %callback;
		defaultName = %defaultName;
	};
	%dialog.init(%width, %height);
	%this.childDialog = %dialog;

	Canvas.pushDialog(%dialog);
}

function GuiProfileEditorDialog::openConfirmDialog(%this, %title, %message, %confirmText, %callback)
{
	%width = 500;

	// Grow the dialog to fit the message rather than clipping it: the text wraps
	// at roughly 54 characters to a line at this width, and the dialog needs
	// room for the buttons under it. Two lines is the floor, so the short
	// messages keep the shape they had.
	%lines = mGetMax(mCeil(strlen(%message) / 54), 2);
	%height = 100 + (%lines * 24);
	%dialog = new GuiControl()
	{
		class = "GuiProfileEditorConfirmDialog";
		superclass = "EditorDialog";
		dialogSize = (%width + 8) SPC (%height + 8);
		dialogCanClose = true;
		dialogText = %title;
		message = %message;
		confirmText = %confirmText;
		callbackTarget = %this;
		callbackMethod = %callback;
	};
	%dialog.init(%width, %height);
	%this.childDialog = %dialog;

	Canvas.pushDialog(%dialog);
}

//-----------------------------------------------------------------------------
// Close protocol. The window X and the Cancel button both land in onClose;
// discarding reverts every dirty theme by reloading it from its file (the
// themes themselves persist beyond the dialog).
//-----------------------------------------------------------------------------

function GuiProfileEditorDialog::onCancel(%this)
{
	%this.onClose();
}

function GuiProfileEditorDialog::onClose(%this)
{
	if(%this.library.isDirty())
	{
		%this.openConfirmDialog("Discard Changes", "You have unsaved theme changes. Discard them?", "Discard", "discardAndClose");
		return;
	}
	%this.closeNow();
}

function GuiProfileEditorDialog::onSave(%this)
{
	%this.library.saveAll();
	%this.closeNow();
}

function GuiProfileEditorDialog::discardAndClose(%this)
{
	// Reverting deletes dirty themes and reloads them from their files, so
	// detach everything that renders or inspects members first.
	%this.preview.clearSamples();
	%this.profileForm.unbind();
	%this.borderForm.unbind();
	%this.themeForm.unbind();
	%this.library.revertAll();
	%this.closeNow();
}

function GuiProfileEditorDialog::closeNow(%this)
{
	Canvas.popDialog(%this);
	EditorCore.dialog = %this;
	EditorCore.schedule(100, "deleteDialog");
}
