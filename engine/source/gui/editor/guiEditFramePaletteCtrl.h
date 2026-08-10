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

#ifndef _GUI_EDIT_FRAME_PALETTE_CTRL_H_
#define _GUI_EDIT_FRAME_PALETTE_CTRL_H_

#ifndef _GUI_EDIT_FRAME_STRIP_CTRL_H_
#include "gui/editor/guiEditFrameStripCtrl.h"
#endif

//-----------------------------------------------------------------------------
// Every frame an image asset offers, wrapped into rows and scrolled vertically:
// the right-hand pane of the Asset Manager's animation editor, and the source
// end of the drag that builds a timeline.
//
// Cells are derived, not held -- frame N of the sheet is cell N -- so there is
// no state here to keep in step with anything. What the palette owns is the
// gesture: a press that becomes either a click or a drag depending on whether it
// moved, which is the same fork the Gui Editor's control palette makes.
//
// Both ends report to script rather than acting, because what a dropped frame
// means is the timeline's business, not this control's.
//-----------------------------------------------------------------------------

class GuiEditFramePaletteCtrl : public GuiEditFrameStripCtrl
{
private:
	typedef GuiEditFrameStripCtrl Parent;

protected:
	bool mPressed;          ///< A press is in progress and has not yet become a drag.
	S32 mPressCell;         ///< The cell it started on.
	Point2I mPressAt;       ///< Where, so the slop can be measured.
	bool mDragged;          ///< It travelled far enough to be a drag, so the release is not a click.

public:
	// The distance a press has to travel before it stops being a click. The same
	// five pixels GuiEditorControlTile uses, so the two palettes in the editor
	// feel the same under the hand.
	static constexpr S32 smDragSlop = 5;

	GuiEditFramePaletteCtrl();

	/// One cell per frame of the sheet.
	S32 getCellCount() const;

	/// As wide as the scroller made it, as tall as its rows need. The scroller
	/// runs vertically, so the width is not this control's to choose.
	Point2I getDesiredExtent();

	void onTouchDown(const GuiEvent& event);
	void onTouchDragged(const GuiEvent& event);
	void onTouchUp(const GuiEvent& event);

	DECLARE_CONOBJECT(GuiEditFramePaletteCtrl);
};

#endif //_GUI_EDIT_FRAME_PALETTE_CTRL_H_
