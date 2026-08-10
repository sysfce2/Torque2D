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
// The script half of the frame palette: what a click means, and what a frame
// looks like while it is being dragged.
//
// Note there is no class= on the control this belongs to, and there must not be:
// the C++ class owns this namespace. Setting class to the same name makes
// Namespace::classLinkTo log "cannot change namespace parent linkage" every time
// the editor opens. The owning pane arrives as an ordinary field, %this.pane.
//-----------------------------------------------------------------------------

// How big the thing under the cursor is while a frame is in flight. Bigger than a
// palette cell on purpose: it is being carried, and it has to stay findable
// against the preview art behind it.
$GuiEditFramePaletteCtrl::payloadSize = 56;

//-----------------------------------------------------------------------------
// A click is a drop that never moved.
//
// It routes to the same appendFrame the drop path ends in rather than adding the
// frame itself. Two ways into the list would each have to remember to commit, to
// re-arm the preview and to keep the selection honest, and the second one would
// go stale the first time any of that changed.
//-----------------------------------------------------------------------------

function GuiEditFramePaletteCtrl::onFrameClicked(%this, %frame)
{
	if(!isObject(%this.pane) || !isObject(%this.pane.stage))
	{
		return;
	}

	%this.pane.stage.appendFrame(%frame);
}

//-----------------------------------------------------------------------------
// The drag. The control tells us it has begun and hands over the frame; making
// the payload and deciding where it may be dropped are the editor's business,
// not the grid's.
//-----------------------------------------------------------------------------

function GuiEditFramePaletteCtrl::onFrameDragBegan(%this, %frame, %x, %y)
{
	if(!isObject(%this.pane) || !isObject(%this.pane.stage))
	{
		return;
	}

	%payload = %this.makePayload(%frame);
	if(!isObject(%payload))
	{
		return;
	}

	// The drag control lives in the outer frame set, for two reasons. It has to
	// be an ancestor of the timeline, because GuiDragAndDropCtrl::findDragTarget
	// hit-tests from its own PARENT downwards; and the payload has to be able to
	// travel over the whole page, which a control parented into the palette could
	// not do.
	%host = AssetAdmin.content;

	// Position is relative to that parent, so the drag control's own offset has
	// to come out of the cursor position or the payload jumps on the first frame.
	%hostAt = %host.getGlobalPosition();
	%xOffset = (getWord(%payload.extent, 0) / 2) + getWord(%hostAt, 0);
	%yOffset = (getWord(%payload.extent, 1) / 2) + getWord(%hostAt, 1);

	%dragCtrl = new GuiDragAndDropCtrl()
	{
		canSaveDynamicFields = "0";
		Profile = "GuiDragAndDropProfile";
		HorizSizing = "anchorLeft";
		VertSizing = "anchorTop";
		Position = (%x - %xOffset) SPC (%y - %yOffset);
		Extent = %payload.extent;
		MinExtent = "16 16";
		Visible = "1";
		deleteOnMouseUp = true;
	};

	%dragCtrl.add(%payload);
	%host.add(%dragCtrl);

	// Grabbed by the middle, which is what lets the drop target work out where
	// the cursor is from the payload alone -- the position the drop callback is
	// handed is in the drag control's parent's space and cannot be used.
	%dragCtrl.startDragging(%xOffset, %yOffset);
}

function GuiEditFramePaletteCtrl::makePayload(%this, %frame)
{
	%size = $GuiEditFramePaletteCtrl::payloadSize;

	%payload = new GuiSpriteCtrl()
	{
		canSaveDynamicFields = "0";
		Position = "0 0";
		Extent = %size SPC %size;
		imageColor = "255 255 255 255";
		singleFrameBitmap = "0";
		tileImage = "0";
		fullSize = "1";
		constrainProportions = "1";

		// What the drop reads back. The payload IS the message.
		frameIndex = %frame;
	};
	ThemeManager.setProfile(%payload, "emptyProfile");

	%payload.setImage(%this.getImageAsset(), %frame);

	return %payload;
}

