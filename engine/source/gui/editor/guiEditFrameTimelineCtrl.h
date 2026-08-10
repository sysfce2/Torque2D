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

#ifndef _GUI_EDIT_FRAME_TIMELINE_CTRL_H_
#define _GUI_EDIT_FRAME_TIMELINE_CTRL_H_

#ifndef _GUI_EDIT_FRAME_STRIP_CTRL_H_
#include "gui/editor/guiEditFrameStripCtrl.h"
#endif

#ifndef _SPRITE_BASE_H_
#include "2d/core/SpriteBase.h"
#endif

//-----------------------------------------------------------------------------
// The frames an animation plays, in order, with a marker on the one being shown
// in the preview above: the bottom pane of the Asset Manager's animation editor.
//
// The cells are an editable list, not a derivation, and the same image frame may
// appear in several of them. That repetition is load-bearing -- the asset format
// has no per-frame duration, every frame gets AnimationTime divided by the count,
// so the only way to hold a pose is to name it twice. A run of duplicates is
// drawn joined by a dimmed divider, which is what makes a hold legible as one
// held frame rather than as somebody's mistake.
//
// The list here is a COPY. This control never writes the asset: it reports one
// onFramesChanged per completed gesture and script decides what that means,
// which is the contract AssetImageCellGrid established. It matters more than
// usual here because AnimationAsset::setAnimationFrames has no equality guard,
// so every call rewrites the .animation.taml -- a per-drag-tick write would be
// hundreds of file writes for one reorder.
//-----------------------------------------------------------------------------

class GuiEditFrameTimelineCtrl : public GuiEditFrameStripCtrl
{
private:
	typedef GuiEditFrameStripCtrl Parent;

protected:
	Vector<S32> mSlots;
	S32 mSelected;              ///< -1 when nothing is picked.
	S32 mCaret;                 ///< Insertion point under a hovering drag, -1 when none.
	S32 mPlayhead;              ///< The slot the preview is showing, -1 when unknown.

	/// The sprite whose playback the marker follows.
	///
	/// SimObjectPtr rather than a delete notification: the preview sprite is
	/// destroyed and remade every time the preview scene is cleared, and this
	/// nulls itself and re-points on assignment with no bookkeeping at all. The
	/// image stays an AssetPtr, because the asset system refcounts that already.
	SimObjectPtr<SpriteBase> mPreview;

	bool mPressed;              ///< A press is in progress on a cell.
	bool mDragging;             ///< It travelled far enough to be a reorder.
	S32 mDragFrom;              ///< The slot it picked up.
	Point2I mPressAt;
	bool mDragOutside;          ///< The pointer has left, so releasing removes rather than moves.

	ColorI mSelectColor;
	ColorI mPlayheadColor;
	ColorI mCaretColor;
	ColorI mHoldColor;
	ColorI mRemoveColor;

	/// What the preview is showing, or -1 when there is nothing to ask.
	S32 readPlayhead();

	void renderCell(S32 index, const RectI& cellRect, bool isHovered);
	void renderOverlay(const RectI& contentRect);

	/// Tell script the list changed. One call per completed gesture, never per
	/// drag tick, so script has exactly one place to commit from.
	void notifyFramesChanged();

	/// Pick a slot and say so, which is what makes the preview scrub to it.
	void selectSlotAndNotify(S32 slot);

public:
	static constexpr S32 smDragSlop = 5;
	static constexpr S32 smCaretWidth = 2;

	/// Where a dropped frame would go, as an index into the list from 0 to the
	/// count inclusive.
	///
	/// Counts cell CENTRES left of the point, not edges, so the caret flips
	/// halfway across a cell -- which is where a person expects "before this one"
	/// to become "after it". Never -1: unlike a click, which has to land on a
	/// frame, a drop always has somewhere to go.
	static S32 insertionAt(const Point2I& local, S32 cellCount, S32 cellSize, S32 cellPad);

	/// The bar drawn at an insertion point, relative to the content rect.
	///
	/// Sits in the gap before the named cell, touching neither it nor the one
	/// before. The one past the last cell is the exception: there is no gap after
	/// the last cell -- getContentExtent deliberately leaves no trailing pad -- so
	/// it goes flush against the right edge of the content, overlapping that
	/// cell's last couple of pixels. Anywhere further right would be clipped away
	/// and the user would see no caret at all.
	static RectI getCaretRect(S32 insertIndex, S32 cellCount, S32 cellSize, S32 cellPad, S32 height);

	GuiEditFrameTimelineCtrl();
	static void initPersistFields();

	S32 getCellCount() const { return mSlots.size(); }
	S32 getFrameAt(S32 index) const;

	/// One row, always. Passing the cell count as the column count is what turns
	/// the shared grid arithmetic into a single line.
	S32 getColumnCount() { return getMax(1, mSlots.size()); }

	/// As tall as the scroller made it, as wide as its cells need -- the mirror
	/// image of the palette, because this one scrolls sideways.
	Point2I getDesiredExtent();

	void onPreRender();

	void onTouchDown(const GuiEvent& event);
	void onTouchDragged(const GuiEvent& event);
	void onTouchUp(const GuiEvent& event);
	bool onKeyDown(const GuiEvent& event);

	/// The list, as a space-separated string. Trimmed, deliberately unlike
	/// AnimationAsset::getAnimationFrames, which leaves a trailing space.
	const char* getFrames();
	void setFrames(const char* frames);

	bool insertFrame(S32 slot, S32 frame);
	bool removeSlot(S32 slot);
	bool moveSlot(S32 from, S32 to);

	void setSelectedSlot(S32 slot);
	inline S32 getSelectedSlot() const { return mSelected; }
	inline S32 getPlayheadSlot() const { return mPlayhead; }

	void setPreviewSprite(SpriteBase* sprite);
	inline void clearPreviewSprite() { mPreview = NULL; mPlayhead = -1; }

	/// Show the insertion caret for a point on the canvas, and where it would go.
	S32 showCaretAt(const Point2I& globalPoint);
	void clearCaret();

	/// Put a frame in at the point a drag was released. The caret the user saw
	/// and the index used here come out of the same static, so what was shown is
	/// what happens.
	bool insertFrameAtPoint(const Point2I& globalPoint, S32 frame);

	DECLARE_CONOBJECT(GuiEditFrameTimelineCtrl);
};

#endif //_GUI_EDIT_FRAME_TIMELINE_CTRL_H_
