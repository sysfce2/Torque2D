
//-----------------------------------------------------------------------------
// The Gui Profile Editor: a near-full-screen dialog showing a tree of every
// theme and standalone profile in the project's themes folder, a field
// inspector for the selected member, and a live preview control. Edits apply
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
	%this.toolbar.addButton("onRenameTheme", 49, "Rename Theme", "getThemeSelected");
	%this.toolbar.addButton("onDeleteTheme", 23, "Delete Theme", "getThemeSelected");
	%this.toolbar.addButton("onNewProfile", 25, "New Profile in Category", "getCategorySelected");
	%this.toolbar.addButton("onRemoveProfile", 23, "Remove Extra Profile", "getExtraSelected");
	%this.toolbar.addButton("onNewStandalone", 25, "New Stand Alone Profile", "");
	%this.toolbar.addButton("onResetMember", 11, "Reset All Overrides on Member", "getMemberSelected");

	%paneTop = 40;
	%paneHeight = %height - 116;

	%this.treeScroller = new GuiScrollCtrl()
	{
		HorizSizing = "right";
		VertSizing = "bottom";
		Position = "6" SPC %paneTop;
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
	%content.add(%this.treeScroller);

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

	%this.inspectorScroller = new GuiScrollCtrl()
	{
		HorizSizing = "right";
		VertSizing = "bottom";
		Position = "252" SPC %paneTop;
		Extent = "400" SPC %paneHeight;
		hScrollBar = "alwaysOff";
		vScrollBar = "alwaysOn";
		constantThumbHeight = "0";
		showArrowButtons = "1";
		scrollBarThickness = "14";
	};
	ThemeManager.setProfile(%this.inspectorScroller, "emptyProfile");
	ThemeManager.setProfile(%this.inspectorScroller, "thumbProfile", "ThumbProfile");
	ThemeManager.setProfile(%this.inspectorScroller, "trackProfile", "TrackProfile");
	ThemeManager.setProfile(%this.inspectorScroller, "scrollArrowProfile", "ArrowProfile");
	%content.add(%this.inspectorScroller);

	%this.inspector = new GuiInspector()
	{
		Class = "GuiProfileEditorInspector";
		HorizSizing = "width";
		VertSizing = "height";
		Position = "0 0";
		Extent = "386" SPC %paneHeight;
		FieldCellSize = "288 40";
		ControlOffset = "10 18";
		ConstantThumbHeight = false;
		ScrollBarThickness = 12;
		ShowArrowButtons = true;
		dialog = %this;
	};
	ThemeManager.setProfile(%this.inspector, "emptyProfile");
	ThemeManager.setProfile(%this.inspector, "panelProfile", "GroupPanelProfile");
	ThemeManager.setProfile(%this.inspector, "emptyProfile", "GroupGridProfile");
	ThemeManager.setProfile(%this.inspector, "labelProfile", "LabelProfile");
	ThemeManager.setProfile(%this.inspector, "overrideLabelProfile", "OverrideLabelProfile");
	ThemeManager.setProfile(%this.inspector, "textEditProfile", "textEditProfile");
	ThemeManager.setProfile(%this.inspector, "dropDownProfile", "dropDownProfile");
	ThemeManager.setProfile(%this.inspector, "dropDownItemProfile", "dropDownItemProfile");
	ThemeManager.setProfile(%this.inspector, "emptyProfile", "backgroundProfile");
	ThemeManager.setProfile(%this.inspector, "scrollingPanelProfile", "ScrollProfile");
	ThemeManager.setProfile(%this.inspector, "scrollingPanelThumbProfile", "ThumbProfile");
	ThemeManager.setProfile(%this.inspector, "scrollingPanelTrackProfile", "TrackProfile");
	ThemeManager.setProfile(%this.inspector, "scrollingPanelArrowProfile", "ArrowProfile");
	ThemeManager.setProfile(%this.inspector, "checkboxProfile", "checkboxProfile");
	ThemeManager.setProfile(%this.inspector, "buttonProfile", "buttonProfile");
	ThemeManager.setProfile(%this.inspector, "tipProfile", "tooltipProfile");
	ThemeManager.setProfile(%this.inspector, "colorPickerProfile", "colorPopupProfile");
	ThemeManager.setProfile(%this.inspector, "colorPopupProfile", "colorPopupPanelProfile");
	ThemeManager.setProfile(%this.inspector, "emptyProfile", "colorPopupPickerProfile");
	ThemeManager.setProfile(%this.inspector, "colorPickerSelectorProfile", "colorPopupSelectorProfile");
	%this.inspectorScroller.add(%this.inspector);

	// These fields are theme bookkeeping, not something to hand-edit.
	%this.inspector.addHiddenField("category");
	%this.inspector.addHiddenField("themeOverrides");

	%this.preview = new GuiControl()
	{
		class = "GuiProfileEditorPreview";
		HorizSizing = "right";
		VertSizing = "bottom";
		Position = "664" SPC %paneTop;
		Extent = (%width - 670) SPC %paneHeight;
		dialog = %this;
	};
	ThemeManager.setProfile(%this.preview, "emptyProfile");
	%content.add(%this.preview);

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
	if(isObject(%this.inspector))
	{
		%this.inspector.clear();
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
		%this.currentRoot = %proxy.target;
		%this.currentMember = %proxy.target;
	}

	if(isObject(%this.currentMember))
	{
		%this.inspector.inspect(%this.currentMember);
	}
	else
	{
		%this.inspector.clear();
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

// The inspector reports every applied field edit here.
function GuiProfileEditorDialog::onProfileChanged(%this, %object)
{
	if(isObject(%this.currentRoot))
	{
		%this.library.markDirty(%this.currentRoot);
	}
	if(isObject(%this.preview))
	{
		%this.preview.refresh();
	}
}

//-----------------------------------------------------------------------------
// Toolbar enabled-state callbacks.
//-----------------------------------------------------------------------------

function GuiProfileEditorDialog::getThemeSelected(%this)
{
	return isObject(%this.currentProxy) && %this.currentProxy.kind $= "theme";
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

function GuiProfileEditorDialog::onRenameTheme(%this)
{
	if(!%this.getThemeSelected())
	{
		return;
	}
	%this.openNameDialog("Rename Theme", "doRenameTheme", %this.currentProxy.target.getName());
}

function GuiProfileEditorDialog::doRenameTheme(%this, %name)
{
	if(!%this.getThemeSelected())
	{
		return;
	}
	if(%this.library.renameThemeTo(%this.currentProxy.target, %name))
	{
		%this.tree.refresh();
		if(isObject(%this.currentMember))
		{
			%this.inspector.inspect(%this.currentMember);
		}
	}
}

function GuiProfileEditorDialog::onDeleteTheme(%this)
{
	if(!%this.getThemeSelected())
	{
		return;
	}
	%message = "Delete the theme \"" @ %this.currentProxy.target.getName() @ "\" and all of its profiles? The file is removed when you save.";
	%this.openConfirmDialog("Delete Theme", %message, "Delete", "doDeleteTheme");
}

function GuiProfileEditorDialog::doDeleteTheme(%this)
{
	if(!%this.getThemeSelected())
	{
		return;
	}
	%theme = %this.currentProxy.target;

	// Detach everything that renders or inspects members before they die.
	%this.preview.clearSamples();
	%this.inspector.clear();
	%this.currentProxy = "";
	%this.currentRoot = "";
	%this.currentMember = "";

	%this.library.deleteTheme(%theme);
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
	%this.inspector.clear();
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
	%this.inspector.inspect(%this.currentMember);
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
	%height = 160;
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
	%this.inspector.clear();
	%this.library.revertAll();
	%this.closeNow();
}

function GuiProfileEditorDialog::closeNow(%this)
{
	Canvas.popDialog(%this);
	EditorCore.dialog = %this;
	EditorCore.schedule(100, "deleteDialog");
}
