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
// The script half of the timeline grid: what a changed list and a picked slot
// mean to the rest of the editor.
//
// As with the palette, there is no class= on the control -- the C++ class owns
// this namespace -- and the owning pane arrives as %this.pane.
//
// Both handlers are deliberately thin. The grid has already done the editing by
// the time it says anything, exactly once per completed gesture; all that is
// left is to tell the asset and the preview.
//-----------------------------------------------------------------------------

function GuiEditFrameTimelineCtrl::onFramesChanged(%this)
{
	if(!isObject(%this.pane))
	{
		return;
	}

	%this.pane.commitFrames();
}

function GuiEditFrameTimelineCtrl::onSlotSelected(%this, %slot, %frame)
{
	if(!isObject(%this.pane) || !isObject(%this.pane.stage))
	{
		return;
	}

	%this.pane.stage.onSlotSelected(%slot, %frame);
}
