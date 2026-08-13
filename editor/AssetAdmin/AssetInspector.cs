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

	// What the user does to a whole asset rather than to a field of one: save the
	// changes, throw them away, branch them, or step through them.
	//
	// Five 24 pixel buttons at 4 apart is 136 wide, and it sits to the LEFT of the
	// delete button, which is itself pinned to the right edge. The emitter bar
	// starts at 340 and runs to about 448, so there is room for both.
	%this.documentButtonBar = new GuiChainCtrl()
	{
		Class = "EditorButtonBar";
		HorizSizing = "left";
		Position = "514 5";
		Extent = "0 24";
		ChildSpacing = 4;
		IsVertical = false;
		Tool = %this;
		Visible = false;
	};
	ThemeManager.setProfile(%this.documentButtonBar, "emptyProfile");
	%this.add(%this.documentButtonBar);
	%this.documentButtonBar.addButton("SaveAsset", $EditorIcon::save, "Save Asset", "getSaveAssetEnabled");
	%this.documentButtonBar.addButton("RevertAsset", $EditorIcon::reload, "Revert Asset", "getRevertAssetEnabled");
	%this.documentButtonBar.addButton("DuplicateAsset", $EditorIcon::clipboard_copy, "Duplicate Asset", "");
	%this.documentButtonBar.addButton("UndoAsset", $EditorIcon::undo, "Undo", "getUndoAssetEnabled", "getUndoAssetTooltip");
	%this.documentButtonBar.addButton("RedoAsset", $EditorIcon::redo, "Redo", "getRedoAssetEnabled", "getRedoAssetTooltip");

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

	// An asset kind with a pane of its own gets it here, sharing the Inspector
	// page with the generic inspector rather than taking a tab -- for that kind
	// of asset the pane IS the inspector, and chooseInspector decides which one is
	// on show. None of them is ever rebuilt or freed.
	%this.registerPane("Image", %this.createImagePane());
	%this.registerPane("Animation", %this.createAnimationPane());
	%this.registerPane("Font", %this.createFontPane());
	%this.registerPane("Sound", %this.createSoundPane());

	// A particle asset takes two, because the dropdown beside the title chooses
	// between the effect and one of its emitters and those are different objects
	// with different fields. They are two panes rather than one that rebuilds for
	// the same reason as all the others: a pane is built once and only ever bound.
	%this.registerPane("Particle", %this.createParticlePane());
	%this.registerPane("Emitter", %this.createEmitterPane());

	// Named handles for the ones the tests and the load methods reach for
	// directly. The registry is the truth; these are just shorter.
	%this.imageScroller = %this.paneScroller["Image"];
	%this.imagePane = %this.pane["Image"];
	%this.animationPane = %this.pane["Animation"];
	%this.fontPane = %this.pane["Font"];
	%this.soundPane = %this.pane["Sound"];
	%this.particlePane = %this.pane["Particle"];
	%this.emitterPane = %this.pane["Emitter"];

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

// Give a custom pane its own scroller on the Inspector page and remember it
// under a key. Built once, here, and never rebuilt or freed.
function AssetInspector::registerPane(%this, %key, %pane)
{
	%scroller = %this.createScroller();
	%scroller.setVisible(false);
	%this.insPage.add(%scroller);

	%scroller.add(%pane);
	%pane.build();

	%this.paneScroller[%key] = %scroller;
	%this.pane[%key] = %pane;
	%this.paneKeys = (%this.paneKeys $= "") ? %key : (%this.paneKeys SPC %key);
}

function AssetInspector::createAnimationPane(%this)
{
	%width = 686;

	return new GuiChainCtrl()
	{
		class = "AssetAnimationInspectorPane";
		superclass = "AssetInspectorPane";
		HorizSizing = "width";
		Position = "0 0";
		Extent = %width SPC 320;
		IsVertical = true;
		ChildSpacing = 6;
		paneWidth = %width;
	};
}

function AssetInspector::createFontPane(%this)
{
	%width = 686;

	return new GuiChainCtrl()
	{
		class = "AssetFontInspectorPane";
		superclass = "AssetInspectorPane";
		HorizSizing = "width";
		Position = "0 0";
		Extent = %width SPC 320;
		IsVertical = true;
		ChildSpacing = 6;
		paneWidth = %width;
	};
}

function AssetInspector::createSoundPane(%this)
{
	%width = 686;

	return new GuiChainCtrl()
	{
		class = "AssetSoundInspectorPane";
		superclass = "AssetInspectorPane";
		HorizSizing = "width";
		Position = "0 0";
		Extent = %width SPC 320;
		IsVertical = true;
		ChildSpacing = 6;
		paneWidth = %width;
	};
}

function AssetInspector::createParticlePane(%this)
{
	%width = 686;

	return new GuiChainCtrl()
	{
		class = "AssetParticleInspectorPane";
		superclass = "AssetInspectorPane";
		HorizSizing = "width";
		Position = "0 0";
		Extent = %width SPC 320;
		IsVertical = true;
		ChildSpacing = 6;
		paneWidth = %width;
	};
}

function AssetInspector::createEmitterPane(%this)
{
	%width = 686;

	return new GuiChainCtrl()
	{
		class = "AssetEmitterInspectorPane";
		superclass = "AssetInspectorPane";
		HorizSizing = "width";
		Position = "0 0";
		Extent = %width SPC 320;
		IsVertical = true;
		ChildSpacing = 6;
		paneWidth = %width;
	};
}

// Which inspector the Inspector page is showing: a registered pane by key, or ""
// for the generic one. The panes standing down are hidden rather than emptied,
// so nothing they hold is ever freed while the engine might be dispatching on it
// -- but they are unbound, so a stale target cannot be written to.
function AssetInspector::chooseInspector(%this, %key)
{
	%this.insScroller.setVisible(%key $= "");
	%this.activePane = "";

	%count = getWordCount(%this.paneKeys);
	for(%i = 0; %i < %count; %i++)
	{
		%thisKey = getWord(%this.paneKeys, %i);
		%chosen = (%thisKey $= %key);

		%this.paneScroller[%thisKey].setVisible(%chosen);

		if(%chosen)
		{
			%this.activePane = %this.pane[%thisKey];
		}
		else
		{
			%this.pane[%thisKey].unbind();
		}
	}
}

// The pane currently standing in for the inspector, or "" when the generic one
// is on show. The one accessor everything below goes through, so there is no
// second place that has to know how many panes there are.
function AssetInspector::activePaneObject(%this)
{
	return %this.activePane;
}

// What the title bar's delete button acts on. The generic inspector knows what
// it was handed; a pane has to be asked, because it is not one.
function AssetInspector::inspectedObject(%this)
{
	%pane = %this.activePaneObject();
	if(isObject(%pane))
	{
		return %pane.target;
	}

	return %this.inspector.getInspectObject();
}

// An asset changed -- possibly the one on show, possibly one it depends on.
// AssetBase::onRefresh sends every one of them here; the pane decides whether it
// is the one it is bound to.
function AssetInspector::onAssetRefreshed(%this, %asset)
{
	%pane = %this.activePaneObject();
	if(isObject(%pane))
	{
		%pane.onAssetRefreshed(%asset);
	}
}

// The four fields no asset kind wants to see, and the inspect call that follows
// them. Repeated verbatim in five load methods before this.
function AssetInspector::inspectStock(%this, %asset)
{
	%this.inspector.clearHiddenFields();
	%this.inspector.addHiddenField("hidden");
	%this.inspector.addHiddenField("locked");
	%this.inspector.addHiddenField("AssetInternal");
	%this.inspector.addHiddenField("AssetPrivate");
	%this.inspector.inspect(%asset);
}

function AssetInspector::hideInspector(%this)
{
	%this.titlebar.setText("");
	%this.titleDropDown.visible = false;
	%this.tabBook.Visible = false;
	%this.emitterButtonBar.visible = false;
	%this.deleteAssetButton.visible = false;
	%this.documentButtonBar.visible = false;
	%this.document = "";

	// Nothing is selected, so nothing is bound. The pane keeps its rows.
	%this.chooseInspector("");
}

//-----------------------------------------------------------------------------
// The document bar: Save, Revert, Duplicate, Undo, Redo.
//
// These act on the asset as a whole. Note the asset they act on is the ASSET,
// never the emitter that may be showing in the inspector instead -- an emitter
// has no file of its own, and saving one means saving the particle asset that
// owns it. documentAsset() is what settles that.
//-----------------------------------------------------------------------------

// Start keeping undo history for an asset, and show the bar. Every load method
// calls this with whatever it just put on show.
function AssetInspector::beginDocument(%this, %asset)
{
	if(!isObject(%asset))
	{
		return;
	}

	AssetAdmin.undoRecorder.track(%asset);

	%this.document = %asset;
	%this.documentButtonBar.visible = true;
	%this.refreshDocumentBar();
}

// The asset the document commands act on: the one beginDocument was handed,
// which is the one with a file.
//
// Remembered rather than worked out from what is on screen. Deducing it went
// through inspectedObject(), and that has two holes: it is answered by the
// active pane, which every load method binds AFTER calling beginDocument, so
// during the refresh that follows a load there is no pane to ask yet; and when
// there is no pane it falls through to whatever the generic inspector was last
// given, which nothing clears when the selection goes away. Neither showed while
// only the document bar asked - the bar is hidden in exactly those moments - but
// the menus are never hidden, and both "this asset" and "no asset" are things
// they have to be able to say.
//
// It also settles the particle case for free. An emitter is what the rows edit
// and what inspectedObject answers with, but an emitter has no file of its own;
// saving one means saving the particle asset that owns it. That asset is what
// beginDocument was given, so it is what comes back here whichever emitter the
// title dropdown is showing.
function AssetInspector::documentAsset(%this)
{
	return isObject(%this.document) ? %this.document : 0;
}

function AssetInspector::refreshDocumentBar(%this)
{
	if(%this.documentButtonBar.visible)
	{
		%this.documentButtonBar.refreshEnabled();
	}

	// Outside the guard above, deliberately. The bar is hidden whenever nothing
	// is selected; the menus never are, and "nothing is selected" is exactly what
	// they have to be able to say. The predicates answer correctly either way.
	if(isObject(AssetAdmin.menus))
	{
		AssetAdmin.menus.refresh();
	}
}

function AssetInspector::getSaveAssetEnabled(%this)
{
	%asset = %this.documentAsset();

	return isObject(%asset) && %asset.isAssetDirty();
}

// Revert is offered for exactly as long as there is something to throw away.
function AssetInspector::getRevertAssetEnabled(%this)
{
	return %this.getSaveAssetEnabled();
}

function AssetInspector::getUndoAssetEnabled(%this)
{
	%asset = %this.documentAsset();

	return isObject(%asset) && AssetAdmin.undoRecorder.getUndoCount(%asset.getAssetId()) > 0;
}

function AssetInspector::getRedoAssetEnabled(%this)
{
	%asset = %this.documentAsset();

	return isObject(%asset) && AssetAdmin.undoRecorder.getRedoCount(%asset.getAssetId()) > 0;
}

function AssetInspector::getUndoAssetTooltip(%this)
{
	%asset = %this.documentAsset();
	if(!isObject(%asset))
	{
		return "Undo";
	}

	%label = AssetAdmin.undoRecorder.getUndoLabel(%asset.getAssetId());

	return (%label $= "") ? "Undo" : ("Undo" SPC %label);
}

function AssetInspector::getRedoAssetTooltip(%this)
{
	%asset = %this.documentAsset();
	if(!isObject(%asset))
	{
		return "Redo";
	}

	%label = AssetAdmin.undoRecorder.getRedoLabel(%asset.getAssetId());

	return (%label $= "") ? "Redo" : ("Redo" SPC %label);
}

function AssetInspector::SaveAsset(%this)
{
	%asset = %this.documentAsset();
	if(!isObject(%asset) || !%asset.isAssetDirty())
	{
		return;
	}

	%assetId = %asset.getAssetId();

	if(!%asset.saveAsset())
	{
		return;
	}

	AssetAdmin.undoRecorder.onAssetSaved(%assetId);
	%this.refreshDocumentBar();
}

function AssetInspector::RevertAsset(%this)
{
	%asset = %this.documentAsset();
	if(!isObject(%asset) || !%asset.isAssetDirty())
	{
		return;
	}

	if(!%asset.revertAsset())
	{
		return;
	}

	// The undo history described the document that was just thrown away.
	AssetAdmin.undoRecorder.onAssetReverted(%asset);

	// A revert can change anything, including which tabs and rows apply, so this
	// reloads rather than refreshing in place.
	%this.reloadDocument(%asset);
}

function AssetInspector::UndoAsset(%this)
{
	%asset = %this.documentAsset();
	if(!isObject(%asset))
	{
		return;
	}

	if(AssetAdmin.undoRecorder.undo(%asset))
	{
		%this.reloadDocument(%asset);
	}
}

function AssetInspector::RedoAsset(%this)
{
	%asset = %this.documentAsset();
	if(!isObject(%asset))
	{
		return;
	}

	if(AssetAdmin.undoRecorder.redo(%asset))
	{
		%this.reloadDocument(%asset);
	}
}

function AssetInspector::DuplicateAsset(%this)
{
	%asset = %this.documentAsset();
	if(!isObject(%asset))
	{
		return;
	}

	// One field is 50, the feedback line is 96 with room to grow into, and the
	// buttons want 34 and a margin. Plus the 34 the title bar and border take out
	// of the window before the content sees any of it.
	%width = 420;
	%height = 250;

	%dialog = new GuiControl()
	{
		class = "DuplicateAssetDialog";
		superclass = "EditorDialog";
		dialogSize = (%width + 8) SPC (%height + 8);
		dialogCanClose = true;
		dialogResizable = false;
		dialogText = "Duplicate Asset";
		sourceAssetId = %asset.getAssetId();
	};
	%dialog.init(%width, %height);

	Canvas.pushDialog(%dialog);
}

// Put the asset back on show from scratch.
//
// An undo or a revert can move anything, including the things that decide which
// tabs exist -- explicit mode, the emitter list, named cells mode -- so the tile
// is re-clicked rather than the rows being refreshed in place. Clearing
// chosenButton is what makes onClick treat it as a fresh selection instead of one
// of the re-clicks it usually gets.
function AssetInspector::reloadDocument(%this, %asset)
{
	%button = AssetAdmin.chosenButton;

	if(isObject(%button))
	{
		AssetAdmin.chosenButton = "";
		%button.onClick();
	}

	%this.refreshDocumentBar();
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
	%this.chooseInspector("");
}

function AssetInspector::loadImageAsset(%this, %imageAsset, %assetID)
{
	%this.resetInspector();
	%this.tabBook.add(%this.imageFrameEditPage);
	%this.tabBook.add(%this.imageLayersEditPage);
	%this.tabBook.selectPage(0);
	%this.titlebar.setText("Image Asset:" SPC %imageAsset.AssetName);

	%this.beginDocument(%imageAsset);

	%this.chooseInspector("Image");
	%this.imagePane.bind(%imageAsset, %assetID);

	%this.imageFrameEditPage.inspect(%imageAsset);
	%this.imageLayersEditPage.inspect(%imageAsset);
}

function AssetInspector::loadAnimationAsset(%this, %animationAsset, %assetID)
{
	%this.resetInspector();
	%this.titlebar.setText("Animation Asset:" SPC %animationAsset.AssetName);
	%this.beginDocument(%animationAsset);

	// Named cells still fall back to the generic inspector.
	//
	// The reason they used to is now gone: NamedAnimationFrames is a
	// TypeStringTableEntryVector, whose getter joins with commas, and
	// setNamedAnimationFrames split on whitespace alone -- so a named list did not
	// survive its own TAML file, and a pane built on it would have quietly lost
	// work. The setter accepts commas now (AnimationAsset.cc), so a named-cells
	// pane is buildable. It is simply not built yet, which is a job of its own
	// rather than a hazard.
	if(%animationAsset.getNamedCellsMode())
	{
		%this.inspectStock(%animationAsset);
		return;
	}

	%this.chooseInspector("Animation");
	%this.animationPane.bind(%animationAsset, %assetID);
}

function AssetInspector::loadParticleAsset(%this, %particleAsset, %assetID)
{
	%this.resetInspector();
	%this.titleDropDown.visible = true;
	%this.beginDocument(%particleAsset);

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

// Index 0 is the effect; anything above it is one of its emitters. Each gets a
// pane of its own and the graph tab that belongs with it.
function AssetInspector::onChooseParticleAsset(%this, %particleAsset)
{
	%index = %this.titleDropDown.getSelectedItem();
	%curSel = %this.tabBook.getSelectedPage();

	if(%index <= 0)
	{
		%this.chooseInspector("Particle");
		%this.particlePane.bind(%particleAsset, %particleAsset.getAssetId());

		%this.tabBook.removeIfMember(%this.emitterGraphPage);
		%this.tabBook.add(%this.scaleGraphPage);
		%this.scaleGraphPage.inspect(%particleAsset);
	}
	else
	{
		%emitter = %particleAsset.getEmitter(%index - 1);

		%this.chooseInspector("Emitter");
		%this.emitterPane.bind(%emitter, %particleAsset.getAssetId());

		%this.tabBook.removeIfMember(%this.scaleGraphPage);
		%this.tabBook.add(%this.emitterGraphPage);
		%this.emitterGraphPage.inspect(%particleAsset, %index - 1);
	}
	%this.tabBook.selectPage(%curSel);

	%this.emitterButtonBar.visible = true;
	%this.emitterButtonBar.refreshEnabled();

	// Solo and mute act on whichever emitter is selected, so moving the selection
	// moves what they isolate -- and on the effect itself there is nothing to
	// isolate and both stand down.
	if(isObject(AssetAdmin.particleTransportBar))
	{
		AssetAdmin.particleTransportBar.refresh();
	}
}

// Re-label the dropdown without disturbing what is selected or rebuilding the
// pane under it. Renaming an emitter is the only thing that needs this: the name
// is a field on the pane and the caption is a copy of it in the title bar.
function AssetInspector::refreshEmitterLabels(%this)
{
	%asset = %this.documentAsset();
	if(!isObject(%asset) || !%this.titleDropDown.isVisible())
	{
		return;
	}

	%this.refreshParticleTitleDropDown(%asset, %this.titleDropDown.getSelectedItem());
}

// Which emitter the dropdown is on, or "" when it is on the effect itself. The
// one place that knows how the list maps to the asset, so nothing below has to
// repeat the minus one.
function AssetInspector::selectedEmitter(%this)
{
	%asset = %this.documentAsset();
	%index = %this.titleDropDown.getSelectedItem();

	if(!isObject(%asset) || %index <= 0 || %index > %asset.getEmitterCount())
	{
		return "";
	}

	return %asset.getEmitter(%index - 1);
}

function AssetInspector::loadFontAsset(%this, %fontAsset, %assetID)
{
	%this.resetInspector();
	%this.titlebar.setText("Font Asset:" SPC %fontAsset.AssetName);
	%this.beginDocument(%fontAsset);

	%this.chooseInspector("Font");
	%this.fontPane.bind(%fontAsset, %assetID);
}

function AssetInspector::loadAudioAsset(%this, %audioAsset, %assetID)
{
	%this.resetInspector();
	%this.titlebar.setText("Audio Asset:" SPC %audioAsset.AssetName);
	%this.beginDocument(%audioAsset);

	%this.chooseInspector("Sound");
	%this.soundPane.bind(%audioAsset, %assetID);
}

function AssetInspector::loadSpineAsset(%this, %spineAsset, %assetID)
{
	%this.resetInspector();
	%this.titlebar.setText("Spine Asset:" SPC %spineAsset.AssetName);
	%this.beginDocument(%spineAsset);

	%this.inspectStock(%spineAsset);
}

function AssetInspector::deleteAsset(%this)
{
	// The asset, never the emitter showing in its place - deleting one emitter of
	// a particle is RemoveEmitter's job, and this offers to take files off disk.
	%asset = %this.documentAsset();
	if(!isObject(%asset))
	{
		return;
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

//-----------------------------------------------------------------------------
// The emitter bar beside the dropdown.
//
// Every one of these used to start from %this.inspector.getInspectObject() --
// the generic inspector -- and work out from the dropdown index whether what it
// answered was the asset or one of its emitters. That only held while index 0
// went through the generic inspector, which it no longer does: the effect has a
// pane of its own now and the generic inspector is never handed a particle at
// all. They go through documentAsset() and selectedEmitter() instead, which are
// true whichever inspector is on show.
//-----------------------------------------------------------------------------

function AssetInspector::addEmitter(%this)
{
	%asset = %this.documentAsset();
	if(!isObject(%asset))
	{
		return;
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
	%asset = %this.documentAsset();
	%index = %this.titleDropDown.getSelectedItem();

	if(!isObject(%asset) || %index <= 0 || %index >= %asset.getEmitterCount())
	{
		return;
	}

	%asset.moveEmitter(%index - 1, %index);
	%this.refreshParticleTitleDropDown(%asset, %index + 1);

	%asset.refreshAsset();
	%this.onChooseParticleAsset(%asset);
}

function AssetInspector::MoveEmitterBackward(%this)
{
	%asset = %this.documentAsset();
	%index = %this.titleDropDown.getSelectedItem();

	// Index 1 is the FIRST emitter, so it has nowhere to go: moveEmitter(0, -1)
	// is what the missing half of this test used to ask for.
	if(!isObject(%asset) || %index <= 1)
	{
		return;
	}

	%asset.moveEmitter(%index - 1, %index - 2);
	%this.refreshParticleTitleDropDown(%asset, %index - 1);

	%asset.refreshAsset();
	%this.onChooseParticleAsset(%asset);
}

function AssetInspector::RemoveEmitter(%this)
{
	%asset = %this.documentAsset();
	%emitter = %this.selectedEmitter();

	if(!isObject(%asset) || !isObject(%emitter))
	{
		return;
	}

	%index = %this.titleDropDown.getSelectedItem();
	%asset.RemoveEmitter(%emitter, true);

	// Rebuilt rather than deleteItem'd, so the captions cannot drift out of step
	// with the emitters they name.
	//
	// Selection falls back to the emitter that took this one's place, or to the
	// last one if this was the last -- and to the EFFECT at index 0 when the one
	// removed was the only emitter. That last case is why this is clamped at all:
	// it used to clamp to an item index of 0 and then ask for getEmitter(-1).
	%count = %asset.getEmitterCount();
	if(%index > %count)
	{
		%index = %count;
	}

	%this.refreshParticleTitleDropDown(%asset, %index);

	%asset.refreshAsset();
	%this.onChooseParticleAsset(%asset);
}

//-----------------------------------------------------------------------------
// What the bar greys itself against. All three read the dropdown, which is the
// thing the buttons act through -- they used to read emitterGraphPage.emitterID,
// a tab page that is only on the book while an emitter is selected.
//-----------------------------------------------------------------------------

function AssetInspector::getMoveEmitterForwardEnabled(%this)
{
	%asset = %this.documentAsset();
	%index = %this.titleDropDown.getSelectedItem();

	if(!isObject(%asset) || %index <= 0)
	{
		return false;
	}

	// The last emitter is at item index getEmitterCount().
	return %index < %asset.getEmitterCount();
}

function AssetInspector::getMoveEmitterBackwardEnabled(%this)
{
	return isObject(%this.documentAsset()) && %this.titleDropDown.getSelectedItem() > 1;
}

function AssetInspector::getRemoveEmitterEnabled(%this)
{
	%asset = %this.documentAsset();

	if(!isObject(%asset) || %this.titleDropDown.getSelectedItem() <= 0)
	{
		return false;
	}

	// An effect with no emitters at all draws nothing, so the last one is not
	// removable. RemoveEmitter above still handles the empty case, because a
	// predicate is a greyed button rather than a guarantee.
	return %asset.getEmitterCount() > 1;
}
