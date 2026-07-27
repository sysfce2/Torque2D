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

#ifndef _GUICOLORPOPUPCTRL_H_
#define _GUICOLORPOPUPCTRL_H_

#include "gui/guiControl.h"
#include "gui/buttons/guiButtonCtrl.h"
#include "gui/guiColorPickerCtrl.h"
#include "gui/guiTextEditCtrl.h"
#include "gui/containers/guiGridCtrl.h"
#include "graphics/dgl.h"
#include "gui/guiDefaultControlRender.h"

class GuiColorPopupCtrl;

class GuiColorPopupBGCtrl : public GuiControl
{
protected:
	GuiColorPopupCtrl* mColorPopupCtrl;
public:
	GuiColorPopupBGCtrl(GuiColorPopupCtrl* ctrl);
	void onTouchUp(const GuiEvent& event);
};

class GuiColorPopupContentCtrl : public GuiControl
{
public:
	GuiColorPopupContentCtrl();
	void onTouchUp(const GuiEvent& event);
};

// The three pickers work out the color they are showing by reading the pixel the
// selector sits on, so whatever they read wins over whatever the popup was told
// to display. That is right when the user drags a selector and wrong when the
// popup placed the selector itself from an exact color -- a swatch, a typed
// value, or simply the color the popup opened on. suppressNextPush() lets the
// popup say "you are being moved, not read": the selector still catches up
// visually, but the color it derives is dropped instead of pushed back.
class GuiColorPopupAlphaCtrl : public GuiColorPickerCtrl
{
protected:
	GuiColorPopupCtrl* mColorPopupCtrl;
	bool mSuppressNextPush;
public:
	GuiColorPopupAlphaCtrl(GuiColorPopupCtrl* ctrl);
	void updatePickColor(const Point2I& offset, const RectI& contentRect);
	inline void suppressNextPush() { mSuppressNextPush = true; }
};

class GuiColorPopupBlendCtrl : public GuiColorPickerCtrl
{
protected:
	GuiColorPopupCtrl* mColorPopupCtrl;
	GuiColorPopupAlphaCtrl* mAlphaCtrl;
	bool mSuppressNextPush;
public:
	GuiColorPopupBlendCtrl(GuiColorPopupCtrl* ctrl, GuiColorPopupAlphaCtrl* alpha);
	void updatePickColor(const Point2I& offset, const RectI& contentRect);
	inline void suppressNextPush() { mSuppressNextPush = true; }
};

class GuiColorPopupHueCtrl : public GuiColorPickerCtrl
{
protected:
	GuiColorPopupBlendCtrl* mBlendCtrl;
	bool mSuppressNextPush;
public:
	GuiColorPopupHueCtrl(GuiColorPopupBlendCtrl* ctrl);
	void updatePickColor(const Point2I& offset, const RectI& contentRect);
	inline void suppressNextPush() { mSuppressNextPush = true; }
};

/// One cell of the optional swatch grid: a pallet-mode picker showing a fixed
/// color that hands that exact color to the popup when clicked.
class GuiColorPopupSwatchCtrl : public GuiColorPickerCtrl
{
protected:
	GuiColorPopupCtrl* mColorPopupCtrl;
public:
	GuiColorPopupSwatchCtrl(GuiColorPopupCtrl* ctrl);
	void onRender(Point2I offset, const RectI& updateRect);
	void onAction();
};

/// One box of the optional value row: a numeric text box owning a single
/// channel of the popup's color. Commits on Enter and on losing focus.
class GuiColorPopupValueCtrl : public GuiTextEditCtrl
{
protected:
	GuiColorPopupCtrl* mColorPopupCtrl;
	S32 mChannel;		///< 0 red, 1 green, 2 blue, 3 alpha.

	bool handleEnterKey();

public:
	GuiColorPopupValueCtrl(GuiColorPopupCtrl* ctrl, S32 channel);
	void onLoseFirstResponder();
	void commitChannel();
};

class GuiColorPopupCtrl : public GuiButtonCtrl
{
public:
	/// How the optional value row spells a color channel.
	enum ValueMode
	{
		Integer = 0,	///< 0 to 255, matching a ColorI field.
		Float			///< 0.0 to 1.0, matching a ColorF field.
	};

private:
	typedef GuiButtonCtrl Parent;
	ColorF mBaseColor;
	bool mIsOpen;
	Point2I mPopupSize;
	S32 mBarHeight;
	bool mShowAlphaBar;

	// The optional swatch row. mSwatchColors is what script authored; mSwatchCells
	// is the pool of controls showing it. The pool only ever grows: spare cells are
	// hidden, and a hidden child takes no cell in the grid.
	Vector<ColorI> mSwatchColors;
	Vector<GuiColorPopupSwatchCtrl*> mSwatchCells;
	bool mSwatchesDirty;
	S32 mSwatchColumns;

	// The optional value row.
	bool mShowColorValues;
	ValueMode mValueMode;
	S32 mValueBoxHeight;

	GuiColorPopupBGCtrl* mBackground;
	GuiControl* mContent;
	GuiColorPopupBlendCtrl* mColorBlendPicker;
	GuiColorPopupHueCtrl* mColorHuePicker;
	GuiColorPopupAlphaCtrl* mColorAlphaPicker;
	GuiGridCtrl* mSwatchGrid;
	GuiColorPopupValueCtrl* mValueBox[4];

	GuiControlProfile* mBackgroundProfile; //Used to render the background when the drop down is open
	GuiControlProfile* mPopupProfile; //Used for the content box of the popup
	GuiControlProfile* mPickerProfile; //Used for the three color pickers used in the popup
	GuiControlProfile* mSelectorProfile; //Used for the selectors in the popup
	GuiControlProfile* mValueProfile; //Used for the value row's numeric boxes

	/// Recreate the swatch cells from mSwatchColors. Only ever called while the
	/// popup is closed, so the grid is never mutated inside a live dialog.
	void rebuildSwatches();

	/// The height each optional row wants, or zero when the row is off. The
	/// swatch grid answers by laying itself out, so the number covers however
	/// many rows the swatches wrapped into.
	S32 measureSwatchRow(const S32 contentWidth);
	inline S32 measureValueRow() const { return mShowColorValues ? mValueBoxHeight : 0; }

	/// Place the pickers and the optional rows inside the (already sized) content
	/// box, from the bottom up: values, swatches, alpha bar, hue bar, and the
	/// blend box taking whatever is left.
	void layoutPopupContent(const RectI& contentRect);

	/// Move the three pickers' selectors to show theColor without letting them
	/// push their own reading of it back (see suppressNextPush).
	void syncPickersToColor();

	/// Rewrite the value boxes from mBaseColor, leaving alone whichever box the
	/// user is currently typing in.
	void refreshValueBoxes();
	void writeValueBox(const S32 channel);

	/// One channel of a color as a 0 to 1 fraction, whatever the value mode is.
	static F32 channelOf(const ColorF& color, const S32 channel);
	static void setChannelOf(ColorF& color, const S32 channel, const F32 value);

protected:

public:
	GuiColorPopupCtrl();
	static void initPersistFields();

	virtual void onTouchUp(const GuiEvent& event);
	GuiControlState getCurrentState();
	void onRender(Point2I offset, const RectI& updateRect);
	void setPopupSize(const Point2I& size) { mPopupSize.set(size.x, size.y); }

	bool onKeyDown(const GuiEvent& event);
	virtual void onAction();
	void openColorPopup();
	void closeColorPopup();

	bool onWake();
	void onSleep();
	void setControlBackgroundProfile(GuiControlProfile* prof);
	void setControlPopupProfile(GuiControlProfile* prof);
	void setControlPickerProfile(GuiControlProfile* prof);
	void setControlSelectorProfile(GuiControlProfile* prof);
	void setControlValueProfile(GuiControlProfile* prof);

	void setColor(const ColorF& theColor);
	void setAlpha(const F32 alpha);
	void setValue(ColorF& value) { mBaseColor = value; }
	ColorF getValue() { return mBaseColor; }
	const char* getScriptValue();
	void setScriptValue(const char* value);

	/// Show theColor and make the pickers agree with it. This is the path an
	/// exact color takes -- a swatch, a typed channel -- as opposed to setColor,
	/// which is what a picker calls to report what it read.
	void applyColor(const ColorF& theColor);

	/// The swatch row.
	void addSwatch(const ColorI& color);
	void clearSwatches();
	inline S32 getSwatchCount() const { return mSwatchColors.size(); }
	inline const ColorI& getSwatch(const S32 index) const { return mSwatchColors[index]; }
	void selectSwatch(const S32 index);

	/// The value row. The channel accessors read and write in whatever units
	/// valueMode is set to, which is what the boxes themselves show.
	inline bool getShowColorValues() const { return mShowColorValues; }
	inline ValueMode getValueMode() const { return mValueMode; }
	F32 getColorChannel(const S32 channel) const;
	void setColorChannel(const S32 channel, const F32 value);
	void onValueBoxCommit(const S32 channel, const char* text);

	DECLARE_CONOBJECT(GuiColorPopupCtrl);
};

#endif