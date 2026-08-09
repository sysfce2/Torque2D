//-----------------------------------------------------------------------------
// Copyright (c) 2013 GarageGames, LLC
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
//-----------------------------------------------------------------------------

function AssetInspector::onAdd(%this)
{
	%this.titlebar = new GuiControl()
	{
		HorizSizing="width";
		VertSizing="bottom";
		Position="0 0";
		Extent="700 34";
		MinExtent="0 34";
		Text = "";
	};
	ThemeManager.setProfile(%this.titlebar, "panelProfile");
	%this.add(%this.titlebar);

	%this.titleDropDown = new GuiDropDownCtrl()
	{
		Position = "5 3";
		Extent = 320 SPC 26;
		ConstantThumbHeight = false;
		ScrollBarThickness = 12;
		ShowArrowButtons = true;
		Visible = false;
	};
	ThemeManager.setProfile(%this.titleDropDown, "dropDownProfile");
	ThemeManager.setProfile(%this.titleDropDown, "dropDownItemProfile", "listBoxProfile");
	ThemeManager.setProfile(%this.titleDropDown, "emptyProfile", "backgroundProfile");
	ThemeManager.setProfile(%this.titleDropDown, "scrollingPanelProfile", "ScrollProfile");
	ThemeManager.setProfile(%this.titleDropDown, "scrollingPanelThumbProfile", "ThumbProfile");
	ThemeManager.setProfile(%this.titleDropDown, "scrollingPanelTrackProfile", "TrackProfile");
	ThemeManager.setProfile(%this.titleDropDown, "scrollingPanelArrowProfile", "ArrowProfile");
	%this.titlebar.add(%this.titleDropDown);

	%this.deleteAssetButton = new GuiButtonCtrl()
	{
		HorizSizing = "left";
		Class = "EditorIconButton";
		Frame = $EditorIcon::trash;
		Position = "660 5";
		Command = %this.getId() @ ".deleteAsset();";
		Tooltip = "Delete Asset";
		Visible = false;
	};
	ThemeManager.setProfile(%this.deleteAssetButton, "iconButtonProfile");
	%this.add(%this.deleteAssetButton);

	%this.emitterButtonBar = new GuiChainCtrl()
	{
		Class = "EditorButtonBar";
		Position = "340 5";
		Extent = "0 24";
		ChildSpacing = 4;
		IsVertical = false;
		Tool = %this;
		Visible = false;
	};
	ThemeManager.setProfile(%this.emitterButtonBar, "emptyProfile");
	%this.add(%this.emitterButtonBar);
	%this.emitterButtonBar.addButton("AddEmitter", $EditorIcon::round_plus, "Add Emitter", "");
	%this.emitterButtonBar.addButton("MoveEmitterBackward", $EditorIcon::rnd_br_up, "Move Emitter Backward", "getMoveEmitterBackwardEnabled");
	%this.emitterButtonBar.addButton("MoveEmitterForward", $EditorIcon::rnd_br_down, "Move Emitter Forward", "getMoveEmitterForwardEnabled");
	%this.emitterButtonBar.addButton("RemoveEmitter", $EditorIcon::round_delete, "Remove Emitter", "getRemoveEmitterEnabled");

	%this.tabBook = new GuiTabBookCtrl()
	{
		Class = AssetInspectorTabBook;
		HorizSizing = width;
		VertSizing = height;
		Position = "0 34";
		Extent = "700 336";
		TabPosition = top;
		Visible = false;
	};
	ThemeManager.setProfile(%this.tabBook, "smallTabBookProfile");
	ThemeManager.setProfile(%this.tabBook, "smallTabProfile", "TabProfile");
	%this.add(%this.tabBook);

	//Inspector Tab
	%this.insPage = %this.createTabPage("Inspector", "");
	%this.insScroller = %this.createScroller();
	%this.insPage.add(%this.insScroller);
	%this.tabBook.add(%this.insPage);

	%this.inspector = %this.createInspector();
	%this.insScroller.add(%this.inspector);

	// The image asset's own pane, in place of the generic inspector for the one
	// asset kind that has one so far. It shares the Inspector page with the
	// inspector rather than taking a tab of its own -- it IS the inspector, for
	// that kind of asset -- and chooseInspector decides which of the two is on
	// show. Neither is ever rebuilt or freed.
	%this.imageScroller = %this.createScroller();
	%this.imageScroller.setVisible(false);
	%this.insPage.add(%this.imageScroller);

	%this.imagePane = %this.createImagePane();
	%this.imageScroller.add(%this.imagePane);
	%this.imagePane.build();

	//Particle Graph Tool
	%this.scaleGraphPage = %this.createTabPage("Scale Graph", "AssetParticleGraphTool", "");

	//Emitter Graph Tool
	%this.emitterGraphPage = %this.createTabPage("Emitter Graph", "AssetParticleGraphEmitterTool", "AssetParticleGraphTool");

	//Image Frame Edit Tool
	%this.imageFrameEditPage = %this.createTabPage("Explicit Frames", "AssetImageFrameEditTool", "");

	//Image Layer Edit Tool
	%this.imageLayersEditPage = %this.createTabPage("Image Layers", "AssetImageLayersEditTool", "");
}

function AssetInspector::createTabPage(%this, %name, %class, %superClass)
{
	%page = new GuiTabPageCtrl()
	{
		Class = %class;
		SuperClass = %superClass;
		HorizSizing = width;
		VertSizing = height;
		Position = "0 0";
		Extent = "700 320";
		Text = %name;
	};
	ThemeManager.setProfile(%page, "tabPageProfile");

	return %page;
}

// Fill, not width/height. A scroller here is its tab page's only child and wants
// the whole content rect, and the two flags answer that differently: "height"
// keeps the gap to each edge that the control had when it was added, so a page
// resized twice -- once when it joined the book and again when the frame set
// gave the window its real size -- left the scroller 12 pixels taller than the
// page holding it, and the page clipped the 12 pixels at the bottom. That is the
// scroll bar's down arrow. Fill recomputes from the parent's content rect every
// time, so it cannot drift. (GuiEditorInspectorWindow says the same thing.)
function AssetInspector::createScroller(%this)
{
	%scroller = new GuiScrollCtrl()
	{
		HorizSizing="fill";
		VertSizing="fill";
		Position="0 0";
		Extent="700 320";
		hScrollBar="alwaysOff";
		vScrollBar="alwaysOn";
		constantThumbHeight="0";
		showArrowButtons="1";
		scrollBarThickness="14";
	};
	ThemeManager.setProfile(%scroller, "scrollingPanelProfile");
	ThemeManager.setProfile(%scroller, "scrollingPanelThumbProfile", "ThumbProfile");
	ThemeManager.setProfile(%scroller, "scrollingPanelTrackProfile", "TrackProfile");
	ThemeManager.setProfile(%scroller, "scrollingPanelArrowProfile", "ArrowProfile");

	return %scroller;
}

function AssetInspector::createInspector(%this)
{
	%inspector = new GuiInspector()
	{
		HorizSizing="width";
		VertSizing="height";
		Position="0 0";
		Extent="686 320";
		FieldCellSize="300 40";
		ControlOffset="10 18";
		ConstantThumbHeight=false;
		ScrollBarThickness=12;
		ShowArrowButtons=true;
	};
	ThemeManager.setProfile(%inspector, "emptyProfile");
	ThemeManager.setProfile(%inspector, "panelProfile", "GroupPanelProfile");
	ThemeManager.setProfile(%inspector, "emptyProfile", "GroupGridProfile");
	ThemeManager.setProfile(%inspector, "labelProfile", "LabelProfile");
	ThemeManager.setProfile(%inspector, "textEditProfile", "textEditProfile");
	ThemeManager.setProfile(%inspector, "dropDownProfile", "dropDownProfile");
	ThemeManager.setProfile(%inspector, "dropDownItemProfile", "dropDownItemProfile");
	ThemeManager.setProfile(%inspector, "emptyProfile", "backgroundProfile");
	ThemeManager.setProfile(%inspector, "scrollingPanelProfile", "ScrollProfile");
	ThemeManager.setProfile(%inspector, "scrollingPanelThumbProfile", "ThumbProfile");
	ThemeManager.setProfile(%inspector, "scrollingPanelTrackProfile", "TrackProfile");
	ThemeManager.setProfile(%inspector, "scrollingPanelArrowProfile", "ArrowProfile");
	ThemeManager.setProfile(%inspector, "checkboxProfile", "checkboxProfile");
	ThemeManager.setProfile(%inspector, "buttonProfile", "buttonProfile");
	ThemeManager.setProfile(%inspector, "tipProfile", "tooltipProfile");
	ThemeManager.setProfile(%inspector, "colorPickerProfile", "colorPopupProfile");
	ThemeManager.setProfile(%inspector, "colorPopupProfile", "colorPopupPanelProfile");
	ThemeManager.setProfile(%inspector, "emptyProfile", "colorPopupPickerProfile");
	ThemeManager.setProfile(%inspector, "colorPickerSelectorProfile", "colorPopupSelectorProfile");

	return %inspector;
}

// The scroller is 700 wide with a bar always on, so the pane lays out against
// what is left. "width" from there: the pane follows the frame as it is dragged,
// and its grids answer by reflowing into more or fewer columns.
function AssetInspector::createImagePane(%this)
{
	%width = 686;

	return new GuiChainCtrl()
	{
		class = "AssetImageInspectorPane";
		superclass = "AssetInspectorPane";
		HorizSizing = "width";
		Position = "0 0";
		Extent = %width SPC 320;
		IsVertical = true;
		ChildSpacing = 6;
		paneWidth = %width;
	};
}

// Which of the two inspectors the Inspector page is showing. The one standing
// down is hidden rather than emptied, so nothing it holds is ever freed while
// the engine might be dispatching on it.
function AssetInspector::chooseInspector(%this, %useImagePane)
{
	%this.insScroller.setVisible(!%useImagePane);
	%this.imageScroller.setVisible(%useImagePane);

	if(!%useImagePane)
	{
		%this.imagePane.unbind();
	}
}

// What the title bar's delete button acts on. The generic inspector knows what
// it was handed; the image pane has to be asked, because it is not one.
function AssetInspector::inspectedObject(%this)
{
	if(%this.imageScroller.isVisible())
	{
		return %this.imagePane.target;
	}
	return %this.inspector.getInspectObject();
}

// An asset changed -- possibly the one on show, possibly one it depends on.
// AssetBase::onRefresh sends every one of them here; the pane decides whether it
// is the one it is bound to.
function AssetInspector::onAssetRefreshed(%this, %asset)
{
	if(%this.imageScroller.isVisible())
	{
		%this.imagePane.onAssetRefreshed(%asset);
	}
}

function AssetInspector::hideInspector(%this)
{
	%this.titlebar.setText("");
	%this.titleDropDown.visible = false;
	%this.tabBook.Visible = false;
	%this.emitterButtonBar.visible = false;
	%this.deleteAssetButton.visible = false;

	// Nothing is selected, so nothing is bound. The pane keeps its rows.
	%this.chooseInspector(false);
}

function AssetInspector::resetInspector(%this)
{
	%this.titlebar.setText("");
	%this.titleDropDown.visible = false;
	%this.tabBook.Visible = true;
	%this.tabBook.selectPage(0);
	%this.tabBook.removeIfMember(%this.scaleGraphPage);
	%this.tabBook.removeIfMember(%this.emitterGraphPage);
	%this.tabBook.removeIfMember(%this.imageFrameEditPage);
	%this.tabBook.removeIfMember(%this.imageLayersEditPage);

	%this.emitterButtonBar.visible = false;
	%this.deleteAssetButton.visible = true;

	// Back to the generic inspector. The one asset kind with a pane of its own
	// says so straight after.
	%this.chooseInspector(false);
}

function AssetInspector::loadImageAsset(%this, %imageAsset, %assetID)
{
	%this.resetInspector();
	%this.tabBook.add(%this.imageFrameEditPage);
	%this.tabBook.add(%this.imageLayersEditPage);
	%this.tabBook.selectPage(0);
	%this.titlebar.setText("Image Asset:" SPC %imageAsset.AssetName);

	%this.chooseInspector(true);
	%this.imagePane.bind(%imageAsset, %assetID);

	%this.imageFrameEditPage.inspect(%imageAsset);
	%this.imageLayersEditPage.inspect(%imageAsset);
}

function AssetInspector::loadAnimationAsset(%this, %animationAsset, %assetID)
{
	%this.resetInspector();
	%this.titlebar.setText("Animation Asset:" SPC %animationAsset.AssetName);

	%this.inspector.clearHiddenFields();
	%this.inspector.addHiddenField("hidden");
	%this.inspector.addHiddenField("locked");
	%this.inspector.addHiddenField("AssetInternal");
	%this.inspector.addHiddenField("AssetPrivate");
	%this.inspector.inspect(%animationAsset);
}

function AssetInspector::loadParticleAsset(%this, %particleAsset, %assetID)
{
	%this.resetInspector();
	%this.titleDropDown.visible = true;

	%this.refreshParticleTitleDropDown(%particleAsset, 0);
	%this.titleDropDown.Command = %this.getId() @ ".onChooseParticleAsset(" @ %particleAsset.getId() @ ");";

	%this.onChooseParticleAsset(%particleAsset);
}

function AssetInspector::refreshParticleTitleDropDown(%this, %particleAsset, %index)
{
	%this.titleDropDown.clearItems();
	%this.titleDropDown.addItem("Particle Asset:" SPC %particleAsset.AssetName);
	for(%i = 0; %i < %particleAsset.getEmitterCount(); %i++)
	{
		%emitter = %particleAsset.getEmitter(%i);
		%this.titleDropDown.addItem("Emitter:" SPC %emitter.EmitterName);
		%this.titleDropDown.setItemColor(%i + 1, ThemeManager.activeTheme.color5);
	}
	%this.titleDropDown.setCurSel(%index);
}

function AssetInspector::onChooseParticleAsset(%this, %particleAsset)
{
	%index = %this.titleDropDown.getSelectedItem();
	%this.inspector.clearHiddenFields();
	%curSel = %this.tabBook.getSelectedPage();
	if(%index == 0)
	{
		%this.inspector.addHiddenField("hidden");
		%this.inspector.addHiddenField("locked");
		%this.inspector.addHiddenField("AssetInternal");
		%this.inspector.addHiddenField("AssetPrivate");
		%this.inspector.inspect(%particleAsset);

		%this.tabBook.removeIfMember(%this.emitterGraphPage);
		%this.tabBook.add(%this.scaleGraphPage);
		%this.scaleGraphPage.inspect(%particleAsset);
	}
	else if(%index > 0)
	{
		%this.inspector.addHiddenField("hidden");
		%this.inspector.addHiddenField("locked");
		%this.inspector.inspect(%particleAsset.getEmitter(%index - 1));

		%this.tabBook.removeIfMember(%this.scaleGraphPage);
		%this.tabBook.add(%this.emitterGraphPage);
		%this.emitterGraphPage.inspect(%particleAsset, %index - 1);
	}
	%this.tabBook.selectPage(%curSel);

	%this.emitterButtonBar.visible = true;
	%this.emitterButtonBar.refreshEnabled();
}

function AssetInspector::loadFontAsset(%this, %fontAsset, %assetID)
{
	%this.resetInspector();
	%this.titlebar.setText("Font Asset:" SPC %fontAsset.AssetName);

	%this.inspector.clearHiddenFields();
	%this.inspector.addHiddenField("hidden");
	%this.inspector.addHiddenField("locked");
	%this.inspector.addHiddenField("AssetInternal");
	%this.inspector.addHiddenField("AssetPrivate");
	%this.inspector.inspect(%fontAsset);
}

function AssetInspector::loadAudioAsset(%this, %audioAsset, %assetID)
{
	%this.resetInspector();
	%this.titlebar.setText("Audio Asset:" SPC %audioAsset.AssetName);

	%this.inspector.clearHiddenFields();
	%this.inspector.addHiddenField("hidden");
	%this.inspector.addHiddenField("locked");
	%this.inspector.addHiddenField("AssetInternal");
	%this.inspector.addHiddenField("AssetPrivate");
	%this.inspector.inspect(%audioAsset);
}

function AssetInspector::loadSpineAsset(%this, %spineAsset, %assetID)
{
	%this.resetInspector();
	%this.titlebar.setText("Spine Asset:" SPC %spineAsset.AssetName);

	%this.inspector.clearHiddenFields();
	%this.inspector.addHiddenField("hidden");
	%this.inspector.addHiddenField("locked");
	%this.inspector.addHiddenField("AssetInternal");
	%this.inspector.addHiddenField("AssetPrivate");
	%this.inspector.inspect(%spineAsset);
}

function AssetInspector::deleteAsset(%this)
{
	%asset = %this.inspectedObject();
	if(%this.titleDropDown.visible && %this.titleDropDown.getSelectedItem() != 0)
	{
		%asset = %asset.getOwner();
	}

	%width = 700;
	%height = 230;
	%dialog = new GuiControl()
	{
		class = "DeleteAssetDialog";
		superclass = "EditorDialog";
		dialogSize = (%width + 8) SPC (%height + 8);
		dialogCanClose = true;
		dialogText = "Delete Asset";
		doomedAsset = %asset;
	};
	%dialog.init(%width, %height);

	Canvas.pushDialog(%dialog);
}

function AssetInspector::addEmitter(%this)
{
	%asset = %this.inspector.getInspectObject();
	if(%this.titleDropDown.getSelectedItem() != 0)
	{
		%asset = %asset.getOwner();
	}

	%width = 700;
	%height = 230;
	%dialog = new GuiControl()
	{
		class = "NewParticleEmitterDialog";
		superclass = "EditorDialog";
		dialogSize = (%width + 8) SPC (%height + 8);
		dialogCanClose = true;
		dialogText = "New Particle Emitter";
		parentAsset = %asset;
	};
	%dialog.init(%width, %height);

	Canvas.pushDialog(%dialog);
}

function AssetInspector::MoveEmitterForward(%this)
{
	%emitter = %this.inspector.getInspectObject();
	%asset = %emitter.getOwner();
	%index = %this.titleDropDown.getSelectedItem();
	%asset.moveEmitter(%index-1, %index);

	%this.refreshParticleTitleDropDown(%asset, %index+1);

	%asset.refreshAsset();
}

function AssetInspector::MoveEmitterBackward(%this)
{
	%emitter = %this.inspector.getInspectObject();
	%asset = %emitter.getOwner();
	%index = %this.titleDropDown.getSelectedItem();
	%asset.moveEmitter(%index-1, %index-2);

	%this.refreshParticleTitleDropDown(%asset, %index-1);

	%asset.refreshAsset();
}

function AssetInspector::RemoveEmitter(%this)
{
	%emitter = %this.inspector.getInspectObject();
	%asset = %emitter.getOwner();
	%asset.RemoveEmitter(%emitter, true);

	%index = %this.titleDropDown.getSelectedItem();
	%this.titleDropDown.deleteItem(%index);

	if(%this.titleDropDown.getItemCount() <= %index)
	{
		%index = %this.titleDropDown.getItemCount() - 1;
	}
	%this.titleDropDown.setCurSel(%index);
	%this.inspector.inspect(%asset.getEmitter(%index - 1));
	%this.emitterGraphPage.inspect(%asset, %index - 1);
	%this.emitterButtonBar.refreshEnabled();

	%asset.refreshAsset();
}

function AssetInspector::getMoveEmitterForwardEnabled(%this)
{
	if(isObject(%this.titleDropDown) && %this.titleDropDown.getSelectedItem() <= 0)
	{
		return false;
	}
	if(isObject(%this.inspector))
	{
		%asset = %this.inspector.getInspectObject();
		%emitterID = %this.emitterGraphPage.emitterID;

		return %emitterID != (%asset.getOwner().getEmitterCount() - 1);
	}
	return false;
}

function AssetInspector::getMoveEmitterBackwardEnabled(%this)
{
	if(isObject(%this.titleDropDown) && %this.titleDropDown.getSelectedItem() <= 0)
	{
		return false;
	}
	if(isObject(%this.inspector))
	{
		return %this.emitterGraphPage.emitterID != 0;
	}
	return false;
}

function AssetInspector::getRemoveEmitterEnabled(%this)
{
	if(isObject(%this.titleDropDown) && %this.titleDropDown.getSelectedItem() <= 0)
	{
		return false;
	}
	if(isObject(%this.inspector))
	{
		%asset = %this.inspector.getInspectObject();
		return %asset.getOwner().getEmitterCount() > 1;
	}
	return false;
}
