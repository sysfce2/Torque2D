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

//-----------------------------------------------------------------------------
// The right-hand pane of the animation editor: every frame the animation's image
// has to offer, scrolled vertically, to click or drag into the timeline.
//
// The pane is the scroller and the caption; the grid inside it is C++, because
// script cannot ask an image where one of its frames is.
//-----------------------------------------------------------------------------

$AssetAnimationPalettePane::captionHeight = 20;

function AssetAnimationPalettePane::onAdd(%this)
{
	ThemeManager.setProfile(%this, "panelProfile");

	%this.caption = new GuiControl()
	{
		HorizSizing = "width";
		VertSizing = "bottom";
		Position = "0 0";
		Extent = "100" SPC $AssetAnimationPalettePane::captionHeight;
		Text = "Frames";
	};
	// panelProfile, which is what the Asset Inspector's own title bar wears: its
	// font is the theme's color5, legible on the panel fill behind it. labelProfile
	// is meant for a caption on the window background and comes out near-black on
	// dark blue under Lab Coat.
	ThemeManager.setProfile(%this.caption, "panelProfile");
	%this.add(%this.caption);

	// "height", not "fill": fill means "be the whole of the parent", which here
	// would put the scroller over the caption. Anchoring both edges keeps the
	// caption's height at the top and takes everything below it.
	%this.scroller = new GuiScrollCtrl()
	{
		HorizSizing = "width";
		VertSizing = "height";
		Position = "0" SPC $AssetAnimationPalettePane::captionHeight;
		Extent = "100 80";
		hScrollBar = "alwaysOff";

		// Always on, not dynamic. A sheet worth opening the palette for has more
		// frames than fit, so the bar is all but permanent anyway -- and a
		// dynamic bar has to be decided from the strip's height, which the strip
		// works out during the very layout pass that would have to notice it.
		vScrollBar = "alwaysOn";
		constantThumbHeight = false;
		scrollBarThickness = 14;
		showArrowButtons = false;
	};
	ThemeManager.setProfile(%this.scroller, "scrollingPanelProfile");
	ThemeManager.setProfile(%this.scroller, "tinyThumbProfile", "ThumbProfile");
	ThemeManager.setProfile(%this.scroller, "tinyTrackProfile", "TrackProfile");
	ThemeManager.setProfile(%this.scroller, "tinyScrollArrowProfile", "ArrowProfile");
	%this.add(%this.scroller);

	// No class= on the grid. The C++ class owns that namespace, and setting class
	// to the same name makes Namespace::classLinkTo complain every time the
	// editor opens. The owner is passed as a plain field instead.
	// "fill" across, and it is legal precisely because the horizontal bar is
	// alwaysOff: GuiScrollCtrl only refuses fill on an axis it can scroll, where
	// filling would clamp the content to what is already visible. Across, there
	// is nothing to scroll and fill is how the grid asks for the real width --
	// which it must have before it can work out how many columns fit.
	%this.strip = new GuiEditFramePaletteCtrl()
	{
		pane = %this;
		HorizSizing = "fill";
		VertSizing = "bottom";
		Position = "0 0";
		Extent = "100 100";
		CellSize = 48;
		CellPad = 4;
		ShowFrameNumbers = true;
	};
	ThemeManager.setProfile(%this.strip, "emptyProfile");
	%this.scroller.add(%this.strip);
}

function AssetAnimationPalettePane::load(%this, %imageAssetId)
{
	%this.strip.setImageAsset(%imageAssetId);
	%this.refreshCaption();
}

function AssetAnimationPalettePane::reload(%this)
{
	// The image may have been re-cut, so the frame count has moved. Setting the
	// same id again is what makes the grid ask it afresh.
	%this.strip.setImageAsset(%this.strip.getImageAsset());
	%this.refreshCaption();
}

function AssetAnimationPalettePane::refreshCaption(%this)
{
	%count = %this.strip.getImageFrameCount();
	if(%count == 1)
	{
		%this.caption.setText("1 frame");
		return;
	}

	%this.caption.setText(%count SPC "frames");
}
