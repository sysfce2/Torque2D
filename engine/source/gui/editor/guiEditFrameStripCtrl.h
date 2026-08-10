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

#ifndef _GUI_EDIT_FRAME_STRIP_CTRL_H_
#define _GUI_EDIT_FRAME_STRIP_CTRL_H_

#ifndef _GUICONTROL_H_
#include "gui/guiControl.h"
#endif

#ifndef _IMAGE_ASSET_H_
#include "2d/assets/ImageAsset.h"
#endif

//-----------------------------------------------------------------------------
// A grid of an image asset's frames: what the Asset Manager's animation editor
// draws twice, once as the palette of every frame the image offers and once as
// the timeline of the frames an animation actually plays.
//
// The two are separate classes because almost nothing about them matches -- one
// derives its cells from the image and wraps them into rows, the other holds an
// editable list and keeps it in a single scrolling line; one appends on a click,
// the other selects and scrubs; only one has a keyboard. What they do share is
// this: an image asset to draw from, and the arithmetic of where cell N sits.
//
// This has to be C++ rather than a grid of GuiSpriteCtrls because script cannot
// ask an image where a frame IS. In implicit cell mode getFrameSize is the only
// per-frame question with an answer, and a palette needs the source rect --
// which ImageAsset::getImageFrameArea has and no binding exposes.
//
// All of the layout is static functions taking everything they use. The renderer
// and the hit test then call the same function with the same numbers and cannot
// drift apart, which is the discipline GuiEditorExplorerTree's gutter uses; and
// it can be tested, which matters because nothing here can be tested any other
// way. A unit test has no GL context, so a suite that builds one of these and
// asks it to draw loads a texture, trips a modal assert, and hangs.
//
// Editor-only, and not offered to anyone building a Gui: the palette refuses
// every class whose name begins "GuiEdit", in both copies of that rule
// (GuiEditorControlIcons::isPlaceableClass, generated, and
// GuiEditorControlSpec::isPlaceableClass, hand-typed for its drift guard).
//-----------------------------------------------------------------------------

class GuiEditFrameStripCtrl : public GuiControl
{
private:
	typedef GuiControl Parent;

protected:
	/// The sheet, held as an id and a pointer both.
	///
	/// TypeAssetId rather than TypeImageAssetPtr, for the reason GuiTreeViewCtrl
	/// and GuiSpriteCtrl both split it this way: a TypeImageAssetPtr field
	/// acquires the asset the instant the field is written, which during a
	/// .gui.taml load is before there is anywhere sensible to put the failure.
	/// The id is the persisted truth; the pointer is resolved from it.
	StringTableEntry mImageAssetID;
	AssetPtr<ImageAsset> mImageAsset;

	S32 mCellSize;              ///< The square one frame draws in.
	S32 mCellPad;               ///< The gap between two cells, and nothing else.
	bool mShowFrameNumbers;     ///< Whether each cell is labelled with the image frame it shows.
	ColorI mNumberColor;
	ColorI mHoverColor;
	S32 mHoverCell;             ///< -1 when the pointer is off the cells or outside.

	/// Lay the control out to fit its cells and tell the scroller.
	///
	/// A GuiScrollCtrl reads its content size from the child's own extent
	/// (calcChildExtents), so there is nothing else to notify: resizing IS the
	/// notification. Called from onWake, from parentResized, and from every
	/// setter that can change how many cells there are or how big they draw.
	void updateExtent();

	/// What the cells need, chrome included.
	Point2I getOuterExtentForCells();

	/// The extent this control wants to be.
	///
	/// The two subclasses grow along opposite axes and each keeps whatever its
	/// sizing flags gave it on the other -- a palette is as wide as its scroller
	/// and as tall as its rows, a timeline is as tall as its scroller and as wide
	/// as its cells. Saying which is which here, in one line each, beats trying to
	/// infer it from the column count.
	virtual Point2I getDesiredExtent() { return mBounds.extent; }

	/// The rect the cells live in, given where the control is being drawn.
	///
	/// Measured with NORMAL-state insets in both the paint and the hit test, so a
	/// profile that pads a hovered control differently cannot make cells jitter
	/// under the pointer or make a click land one cell over.
	RectI getContentRect(const Point2I& offset);

	/// One cell. The base draws the frame and, if asked, its number; a subclass
	/// overrides to put a selection or a playhead on top of that.
	virtual void renderCell(S32 index, const RectI& cellRect, bool isHovered);

	/// Anything drawn over the whole grid rather than per cell -- an insertion
	/// caret, say. Called inside the content clip, after every cell.
	virtual void renderOverlay(const RectI& contentRect) { }

public:
	// A cell is square and its size is in pixels of screen, not of art: a 16x16
	// sprite and a 256x256 sprite both get the same box, because the question the
	// grid answers is "which frame is this", not "how big is it".
	//
	// constexpr rather than const: a test asserting against one binds it to a
	// const reference, which would odr-use a plain static const and fail to link.
	static constexpr S32 smMinCellSize = 16;
	static constexpr S32 smMaxCellSize = 128;
	static constexpr S32 smDefaultCellSize = 48;
	static constexpr S32 smDefaultCellPad = 4;

	/// Cell to cell, including the gap. The one number the rest of the layout is
	/// built from, so it exists to be named rather than repeated.
	static S32 getCellAdvance(S32 cellSize, S32 cellPad);

	/// How many cells fit across a content width.
	///
	/// Never zero. A width narrower than a single cell still reports one column,
	/// which shows a clipped cell -- the alternative is a zero the row arithmetic
	/// divides by.
	static S32 getColumnsFor(S32 contentWidth, S32 cellSize, S32 cellPad);

	/// How many rows a given number of cells needs.
	static S32 getRowCountFor(S32 cellCount, S32 columns);

	/// Where cell N draws, relative to the content rect's top left.
	static RectI getCellRect(S32 index, S32 columns, S32 cellSize, S32 cellPad);

	/// Which cell a point falls on, measured from the same top left, or -1.
	///
	/// -1 for the gaps between cells and for the empty tail of a short last row,
	/// both deliberately: a click on nothing must not become a click on the
	/// nearest something.
	static S32 cellAt(const Point2I& local, S32 columns, S32 cellCount, S32 cellSize, S32 cellPad);

	/// The space the cells take up altogether, with no trailing gap on the last
	/// row or column -- a pad of overshoot is a scroll bar for nothing.
	static Point2I getContentExtent(S32 cellCount, S32 columns, S32 cellSize, S32 cellPad);

	GuiEditFrameStripCtrl();
	static void initPersistFields();

	bool onWake();
	void onSleep();
	void onRender(Point2I offset, const RectI& updateRect);
	void parentResized(const Point2I& oldParentExtent, const Point2I& newParentExtent);

	void onTouchMove(const GuiEvent& event);
	void onTouchLeave(const GuiEvent& event);
	void onMouseWheelUp(const GuiEvent& event);
	void onMouseWheelDown(const GuiEvent& event);

	/// How many cells there are. The palette derives it from the image, the
	/// timeline from its own list; the base has none, so it draws nothing.
	virtual S32 getCellCount() const { return 0; }

	/// Which image frame cell N shows. They are the same number for a palette and
	/// quite different for a timeline, where one image frame can fill many cells.
	virtual S32 getFrameAt(S32 index) const { return index; }

	/// How many columns to lay out in. The palette fits as many as the width
	/// allows; the timeline answers with its cell count, which is one row.
	virtual S32 getColumnCount();

	void setImageAssetId(const char* pImageAssetID);
	inline StringTableEntry getImageAssetId() const { return mImageAssetID; }
	inline ImageAsset* getImageAssetObject() const { return mImageAsset; }

	/// How many frames the sheet has, or 0 when there is no usable one.
	S32 getImageFrameCount() const;

	void setCellSize(S32 cellSize);
	inline S32 getCellSize() const { return mCellSize; }

	/// Which cell a point in canvas coordinates falls on, or -1. The whole hit
	/// test, and what every gesture in both subclasses starts with.
	S32 cellAtGlobal(const Point2I& globalPoint);

	/// Where cell N is on the canvas, for a test that must not hard-code a point:
	/// a stale coordinate reports a missing cell, which is exactly what a broken
	/// hit test reports, and the test would be lying either way.
	RectI getCellRectGlobal(S32 index);

	static bool setImage(void* obj, const char* data) { static_cast<GuiEditFrameStripCtrl*>(obj)->setImageAssetId(data); return false; }
	static const char* getImage(void* obj, const char* data) { return static_cast<GuiEditFrameStripCtrl*>(obj)->getImageAssetId(); }

	DECLARE_CONOBJECT(GuiEditFrameStripCtrl);
};

#endif //_GUI_EDIT_FRAME_STRIP_CTRL_H_
