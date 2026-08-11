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
// The bottom pane of the animation editor: the frames the animation plays, in
// order, and the target of every drag out of the palette.
//
// The DROP TARGET is this pane rather than the grid inside it, on purpose. The
// grid is only as wide as its cells, so with four frames in a wide pane most of
// what looks like the timeline is not the grid at all -- and a frame let go over
// that empty space obviously means "put it at the end", not "nowhere".
//-----------------------------------------------------------------------------

$AssetAnimationTimelinePane::captionHeight = 20;

function AssetAnimationTimelinePane::onAdd(%this)
{
	ThemeManager.setProfile(%this, "emptyProfile");

	%this.caption = new GuiControl()
	{
		HorizSizing = "width";
		VertSizing = "bottom";
		Position = "0 0";
		Extent = "100" SPC $AssetAnimationTimelinePane::captionHeight;
		Text = "Timeline";
	};
	// panelProfile for its color5 font, as the palette's caption is and the Asset
	// Inspector's title bar is. See the note there.
	ThemeManager.setProfile(%this.caption, "panelProfile");
	%this.add(%this.caption);

	// "height", not "fill": fill would put the scroller over the caption.
	%this.scroller = new GuiScrollCtrl()
	{
		HorizSizing = "width";
		VertSizing = "height";
		Position = "0" SPC $AssetAnimationTimelinePane::captionHeight;
		Extent = "100 80";
		hScrollBar = "dynamic";
		vScrollBar = "alwaysOff";
		constantThumbHeight = false;
		scrollBarThickness = 14;
		showArrowButtons = true;
	};
	ThemeManager.setProfile(%this.scroller, "scrollingPanelProfile");
	ThemeManager.setProfile(%this.scroller, "tinyThumbProfile", "ThumbProfile");
	ThemeManager.setProfile(%this.scroller, "tinyTrackProfile", "TrackProfile");
	ThemeManager.setProfile(%this.scroller, "tinyScrollArrowProfile", "ArrowProfile");
	%this.add(%this.scroller);

	// The mirror of the palette: "fill" down the axis whose bar is alwaysOff, so
	// the row is as tall as the scroller, and "right" across, where the strip
	// sets its own width from its cells and the bar scrolls it.
	%this.strip = new GuiEditFrameTimelineCtrl()
	{
		pane = %this;
		HorizSizing = "right";
		VertSizing = "fill";
		Position = "0 0";
		Extent = "100 100";
		CellSize = 48;
		CellPad = 4;
		ShowFrameNumbers = true;
	};
	// frameGridProfile, not listBoxProfile: the grids read six of its colors for
	// things a list has no equivalent of, and BaseTheme documents which is which.
	// It also carries the canKeyFocus the Delete key depends on.
	ThemeManager.setProfile(%this.strip, "frameGridProfile");
	%this.scroller.add(%this.strip);
}

function AssetAnimationTimelinePane::load(%this, %imageAssetId, %frames)
{
	%this.strip.setImageAsset(%imageAssetId);
	%this.strip.setFrames(%frames);
	%this.refreshCaption();
}

function AssetAnimationTimelinePane::setPreviewSprite(%this, %sprite)
{
	%this.strip.setPreviewSprite(%sprite);
}

function AssetAnimationTimelinePane::refreshCaption(%this)
{
	%count = %this.strip.getCellCount();
	if(%count == 0)
	{
		%this.caption.setText("Timeline - empty");
		return;
	}

	%this.caption.setText("Timeline -" SPC %count SPC (%count == 1 ? "frame" : "frames"));
}

//-----------------------------------------------------------------------------
// Everything that changes the list ends up in commitFrames, and only here. The
// grid reports that it changed; deciding what that means to the asset is this
// pane's job, and writing it is the stage's.
//-----------------------------------------------------------------------------

function AssetAnimationTimelinePane::commitFrames(%this)
{
	%this.refreshCaption();
	%this.stage.commitFrames(%this.strip.getFrames());
}

function AssetAnimationTimelinePane::appendFrame(%this, %frame)
{
	%this.strip.insertFrame(%this.strip.getCellCount(), %frame);
	%this.commitFrames();
}

function AssetAnimationTimelinePane::setFrames(%this, %frames)
{
	%this.strip.setFrames(%frames);
	%this.commitFrames();
}

function AssetAnimationTimelinePane::appendFrames(%this, %frames)
{
	%existing = %this.strip.getFrames();
	%this.strip.setFrames(%existing $= "" ? %frames : (%existing SPC %frames));
	%this.commitFrames();
}

//-----------------------------------------------------------------------------
// The drop, and the two traps in GuiDragAndDropCtrl that shape all of it.
//
// findDragTarget hit-tests from the drag control's PARENT, and
// GuiControl::findHitControl ends in a bare "return this" without ever testing
// its own bounds -- so a drop anywhere on the screen arrives here, over the
// library, over the menu bar, over the inspector. The target has to police its
// own boundary; nobody else will.
//
// And %position is not where the cursor is. GuiDragAndDropCtrl::sendDragEvent
// builds it from the drag control's own bounds, which are local to that outer
// frame set. So nothing here measures with it -- the payload is asked instead,
// which a drag grabbed by the middle answers exactly.
//-----------------------------------------------------------------------------

function AssetAnimationTimelinePane::onControlDragged(%this, %payload, %position)
{
	%cursor = %this.cursorFrom(%payload);
	if(!%this.isOverStrip(%cursor))
	{
		%this.strip.clearCaret();
		return;
	}

	%this.strip.showCaretAt(%cursor);
}

function AssetAnimationTimelinePane::onControlDragExit(%this, %payload, %position)
{
	%this.strip.clearCaret();
}

function AssetAnimationTimelinePane::onControlDropped(%this, %payload, %position)
{
	%this.strip.clearCaret();

	%cursor = %this.cursorFrom(%payload);
	if(!%this.isOverStrip(%cursor))
	{
		return;
	}

	if(%this.strip.insertFrameAtPoint(%cursor, %payload.frameIndex))
	{
		%this.commitFrames();
	}
}

// The middle of the payload, which is where the cursor is holding it.
function AssetAnimationTimelinePane::cursorFrom(%this, %payload)
{
	%at = %payload.getGlobalPosition();
	%size = %payload.getExtent();

	return mFloor(getWord(%at, 0) + (getWord(%size, 0) / 2)) SPC
		mFloor(getWord(%at, 1) + (getWord(%size, 1) / 2));
}

// Measured against the SCROLLER, not the grid. The grid stops at its last cell;
// the scroller is the whole timeline as far as anyone looking at it is concerned.
function AssetAnimationTimelinePane::isOverStrip(%this, %point)
{
	%at = %this.scroller.getGlobalPosition();
	%size = %this.scroller.getExtent();

	%x = getWord(%point, 0);
	%y = getWord(%point, 1);

	return %x >= getWord(%at, 0) && %y >= getWord(%at, 1) &&
		%x < getWord(%at, 0) + getWord(%size, 0) &&
		%y < getWord(%at, 1) + getWord(%size, 1);
}
