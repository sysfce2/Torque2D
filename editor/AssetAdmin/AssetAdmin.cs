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

function AssetAdmin::create(%this)
{
	exec("./AssetLibraryWindow.cs");
	exec("./AssetDictionary.cs");
	exec("./AssetWindow.cs");
	exec("./AssetDictionaryButton.cs");
	exec("./AssetDictionarySprite.cs");
	exec("./AssetBase.cs");
	exec("./AssetInspector.cs");
	exec("./AssetAudioPlayButton.cs");
	exec("./NewAssetButton.cs");
	exec("./NewImageAssetDialog.cs");
	exec("./NewAnimationAssetDialog.cs");
	exec("./NewParticleAssetDialog.cs");
	exec("./NewFontAssetDialog.cs");
	exec("./NewAudioAssetDialog.cs");
	exec("./DeleteAssetDialog.cs");
	exec("./ParticleEditor/exec.cs");
	exec("./ImageEditor/exec.cs");
	exec("./Inspector/exec.cs");
	exec("./Animation/exec.cs");
	exec("./AssetPreviewSprite.cs");

	%this.guiPage = EditorCore.RegisterEditor("Asset Manager", %this);
	%this.content = %this.createFrameSet();
	%this.buildAssetWindow();
	%this.buildAudioPlayButton();
	%this.buildInspector();
	%this.buildLibrary();

	// The manager that turns the preview into an animation editor and back. It
	// owns the two panes it builds and deletes them in its own onRemove, so
	// deleting it is the whole of the teardown.
	%this.animationStage = new ScriptObject()
	{
		class = "AssetAnimationStage";
		admin = %this;
	};

	EditorCore.FinishRegistration(%this.guiPage);

	%this.isOpen = false;
}

function AssetAdmin::createFrameSet(%this)
{
	%content = new GuiFrameSetCtrl() {
		HorizSizing = "width";
        VertSizing = "height";
        Position = "0 0";
        Extent = %this.guiPage.getExtent();
        DividerThickness = 6;
	};
    ThemeManager.setProfile(%content, "frameSetProfile");
    ThemeManager.setProfile(%content, "dropButtonProfile", "dropButtonProfile");
    ThemeManager.setProfile(%content, "frameSetTabBookProfile", "tabBookProfile");
    ThemeManager.setProfile(%content, "frameSetTabProfile", "tabProfile");
    ThemeManager.setProfile(%content, "frameSetTabPageProfile", "tabPageProfile");
    %this.guiPage.add(%content);

    %idList = %content.createHorizontalSplit(1);
    %leftID = getWord(%idList, 0);
    %rightID = getWord(%idList, 1);
    %content.anchorFrame(%rightID);
    %content.setFrameSize(%rightID, 324);

    %ids = %content.createVerticalSplit(%leftID);
    %centerFrameID = getWord(%ids, 0);
    %inspectorFrameID = getWord(%ids, 1);
	%content.anchorFrame(%inspectorFrameID);
    %content.setFrameSize(%inspectorFrameID, 360);

    // Kept because the only way to move a divider from script is to name the
    // frame, and the ids are handed out once here and never again. The inspector
    // is the bottom frame, so it opens wide and short -- which is why everything
    // in it reflows.
    %this.libraryFrameId = %rightID;
    %this.previewFrameId = %centerFrameID;
    %this.inspectorFrameId = %inspectorFrameID;

	return %content;
}

// Everything inside the library -- the toolbar, the scroller, the chain and the
// groups -- belongs to AssetLibraryWindow, which builds it in its own onAdd. All
// this has to decide is where the window goes.
function AssetAdmin::buildLibrary(%this)
{
	%this.libWindow = new GuiWindowCtrl()
    {
        Class = "AssetLibraryWindow";
        HorizSizing = "right";
        VertSizing = "bottom";
        Position = "0 0";
        Extent = "330 380";
        MinExtent = "200 100";
        text = "Asset Library";
        canMove = true;
        canClose = false;
        canMinimize = true;
        canMaximize = false;
        resizeWidth = true;
        resizeHeight = true;
    };
    ThemeManager.setProfile(%this.libWindow, "windowProfile");
    ThemeManager.setProfile(%this.libWindow, "windowContentProfile", "ContentProfile");
    ThemeManager.setProfile(%this.libWindow, "windowButtonProfile", "CloseButtonProfile");
    ThemeManager.setProfile(%this.libWindow, "windowButtonProfile", "MinButtonProfile");
    ThemeManager.setProfile(%this.libWindow, "windowButtonProfile", "MaxButtonProfile");
    %this.content.add(%this.libWindow);

    // Measure again. The window sized its own contents in onAdd, which is before
    // any of the five profiles above were on it and before the frame set gave it
    // its real extent -- so that pass was against GuiDefaultProfile's title bar.
    %this.libWindow.fitScroller();
}

function AssetAdmin::buildInspector(%this)
{
	%this.inspectorWindow = new GuiWindowCtrl()
    {
        HorizSizing = "right";
        VertSizing = "bottom";
        text = "Asset Inspector";
		Extent = "706 380";

		// Narrow enough to hold the cell table and its scroll bar, and no
		// narrower. A frame set moves its divider whatever the window in the frame
		// thinks, so a minimum the user can drag past is not a floor -- it is 106
		// pixels of window hanging off the right-hand edge, clipped away, taking
		// the Find button and a column of settings with them. The pane reflows all
		// the way down to one column, so there is nothing here to protect.
		MinExtent = "260 200";
        canMove = true;
        canClose = false;
        canMinimize = true;
        canMaximize = false;
        resizeWidth = true;
        resizeHeight = true;
    };
    ThemeManager.setProfile(%this.inspectorWindow, "windowProfile");
    ThemeManager.setProfile(%this.inspectorWindow, "windowContentProfile", "ContentProfile");
    ThemeManager.setProfile(%this.inspectorWindow, "windowButtonProfile", "CloseButtonProfile");
    ThemeManager.setProfile(%this.inspectorWindow, "windowButtonProfile", "MinButtonProfile");
    ThemeManager.setProfile(%this.inspectorWindow, "windowButtonProfile", "MaxButtonProfile");
    %this.content.add(%this.inspectorWindow);

	%this.inspector = new GuiControl()
	{
		class = "AssetInspector";
		Extent="700 370";
        HorizSizing = "fill";
        VertSizing = "fill";
	};
	ThemeManager.setProfile(%this.inspector, "overlayProfile");

	%this.inspectorWindow.add(%this.inspector);
}

function AssetAdmin::buildAssetWindow(%this)
{
	// Two layers between the frame and the preview, and each earns its place.
	//
	// previewFrames is a frame set so the preview can be split three ways for an
	// animation -- the art, the frames available, and the timeline -- with
	// dividers the user can drag. Unsplit it is a pass-through: GuiFrameSetCtrl
	// resizes its one frame to its own extent with no insets, so every other
	// asset type sees exactly what it saw before. Splitting it later never
	// reparents anything either, because splitFrame only rewrites which frame
	// holds a control, and removing one collapses the frame and hoists its twin.
	//
	// previewHost looks like a pointless wrapper and is not. GuiWindowCtrl finds
	// its dock target as a cast of its parent's FIRST child to GuiFrameSetCtrl.
	// Today that child is the background sprite, the cast fails, and docking is
	// quietly off in the Asset Manager. Put the frame set there instead and
	// docking switches itself on, aimed at the animation split -- so the Asset
	// Inspector window would offer to dock into frames the stage later deletes
	// out from under it. One plain control in between keeps that answer "no".
	%this.previewHost = new GuiControl()
	{
		HorizSizing = "right";
		VertSizing = "bottom";
		Position = "0 0";
		Extent = "100 100";
	};
	ThemeManager.setProfile(%this.previewHost, "emptyProfile");
	%this.content.add(%this.previewHost);

	%this.previewFrames = new GuiFrameSetCtrl()
	{
		HorizSizing = "width";
		VertSizing = "height";
		Position = "0 0";
		Extent = "100 100";
		DividerThickness = 6;
	};
	ThemeManager.setProfile(%this.previewFrames, "frameSetProfile");
	ThemeManager.setProfile(%this.previewFrames, "dropButtonProfile", "dropButtonProfile");
	%this.previewHost.add(%this.previewFrames);

	%this.background = new GuiSpriteCtrl() {
		HorizSizing = "right";
        VertSizing = "bottom";
        Position = "0 0";
        Extent = "100 100";
		imageColor = "255 255 255 255";
        Image = "EditorCore:editorGrid";
		singleFrameBitmap = "1";
		tileImage = "1";
		positionOffset = "0 0";
		imageSize = "128 128";
		fullSize = "0";
		constrainProportions = "1";
	};
    ThemeManager.setProfile(%this.background, "emptyProfile");
    %this.previewFrames.add(%this.background);

	%this.assetScene = new Scene();
	%this.assetScene.setScenePause(true);

	%this.assetWindow = new SceneWindow()
	{
		class = AssetWindow;
		profile = ThemeManager.activeTheme.overlayProfile;
		position = "0 0";
		extent = %this.background.extent;
        HorizSizing = "width";
        VertSizing = "height";
		minExtent = "0 0";
		cameraPosition = "0 0";
		cameraSize = "175 111";
		useWindowInputEvents = true;
		useObjectInputEvents = true;
		constantThumbHeight = false;
		scrollBarThickness = 14;
		 showArrowButtons = false;
	};
	ThemeManager.setProfile(%this.assetWindow, "thumbProfile", ThumbProfile);
	ThemeManager.setProfile(%this.assetWindow, "trackProfile", TrackProfile);
	ThemeManager.setProfile(%this.assetWindow, "scrollArrowProfile", ArrowProfile);

	%this.assetWindow.setScene(%this.assetScene);
	%this.assetWindow.setViewLimitOn("-87.5 -55.5 87.5 55.5");
	%this.assetWindow.setShowScrollBar(true);
	%this.assetWindow.setMouseWheelScrolls(false);

    %this.background.add(%this.assetWindow);
}

function AssetAdmin::buildAudioPlayButton(%this)
{
	%this.audioPlayButtonContainer = new GuiControl()
	{
		position = "0 0";
		extent = %this.background.extent;
		HorizSizing="width";
		VertSizing="height";
		Visible="0";
	};
	ThemeManager.setProfile(%this.audioPlayButtonContainer, "emptyProfile");

	%this.audioPlayButton = new GuiButtonCtrl()
	{
		class="AssetAudioPlayButton";
		HorizSizing="center";
		VertSizing="center";
		Extent="100 48";
		Text = "Play";
	};
	ThemeManager.setProfile(%this.audioPlayButton, "buttonProfile");
	%this.audioPlayButtonContainer.add(%this.audioPlayButton);

	%this.background.add(%this.audioPlayButtonContainer);
}

function AssetAdmin::destroy(%this)
{
	if(isObject(%this.animationStage))
	{
		%this.animationStage.delete();
	}
}

function AssetAdmin::open(%this)
{
	%this.libWindow.loadAssets();

	%this.assetScene.setScenePause(false);
	%this.isOpen = true;
}

function AssetAdmin::close(%this)
{
	// The last chance to see where the user left the animation editor's dividers:
	// a frame set announces nothing when one is dragged, so the sizes are read at
	// the moments the split is about to go away.
	%this.animationStage.rememberSizes();

	%this.libWindow.unloadAssets();

	%this.assetScene.setScenePause(true);
	%this.isOpen = false;
}
