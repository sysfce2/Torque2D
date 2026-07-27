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

#include "gui/guiColorPopupCtrl.h"
#include "gui/guiCanvas.h"
#include "console/consoleTypes.h"

#include "guiColorPopupCtrl_ScriptBinding.h"

ColorI blankColor(0,0,0,0);

#pragma region GuiColorPopupBGCtrl
GuiColorPopupBGCtrl::GuiColorPopupBGCtrl(GuiColorPopupCtrl* ctrl)
{
	mColorPopupCtrl = ctrl;
	mBounds.point.set(0, 0);
	setField("profile", "GuiDefaultProfile");
}

void GuiColorPopupBGCtrl::onTouchUp(const GuiEvent& event)
{
	mColorPopupCtrl->closeColorPopup();
}
#pragma endregion

#pragma region GuiColorPopupContentCtrl
GuiColorPopupContentCtrl::GuiColorPopupContentCtrl()
{
	setField("profile", "GuiDefaultProfile");
}

void GuiColorPopupContentCtrl::onTouchUp(const GuiEvent& event) { }
#pragma endregion

#pragma region GuiColorPopupBlendCtrl
GuiColorPopupBlendCtrl::GuiColorPopupBlendCtrl(GuiColorPopupCtrl* ctrl, GuiColorPopupAlphaCtrl* alpha)
{
	mColorPopupCtrl = ctrl;
	mAlphaCtrl = alpha;
	mSuppressNextPush = false;
	setField("profile", "GuiDefaultProfile");
}

void GuiColorPopupBlendCtrl::updatePickColor(const Point2I& offset, const RectI& contentRect)
{
	if(mPositionChanged)
	{
		GuiColorPickerCtrl::updatePickColor(offset, contentRect);

		if (mSuppressNextPush)
		{
			mSuppressNextPush = false;
			return;
		}

		mColorPopupCtrl->setColor(mPickColor);
		mAlphaCtrl->setValue(mPickColor);
	}
}
#pragma endregion

#pragma region GuiColorPopupHueCtrl
GuiColorPopupHueCtrl::GuiColorPopupHueCtrl(GuiColorPopupBlendCtrl* ctrl)
{
	mBlendCtrl = ctrl;
	mSuppressNextPush = false;
	setField("profile", "GuiDefaultProfile");
}

void GuiColorPopupHueCtrl::updatePickColor(const Point2I& offset, const RectI& contentRect)
{
	if (mPositionChanged)
	{
		GuiColorPickerCtrl::updatePickColor(offset, contentRect);

		if (mSuppressNextPush)
		{
			mSuppressNextPush = false;
			return;
		}

		mBlendCtrl->setValue(mPickColor);
		mBlendCtrl->updateColor();
	}
}
#pragma endregion

#pragma region GuiColorPopupAlphaCtrl
GuiColorPopupAlphaCtrl::GuiColorPopupAlphaCtrl(GuiColorPopupCtrl* ctrl)
{
	mColorPopupCtrl = ctrl;
	mSuppressNextPush = false;
	setField("profile", "GuiDefaultProfile");
}

void GuiColorPopupAlphaCtrl::updatePickColor(const Point2I& offset, const RectI& contentRect)
{
	if (mPositionChanged)
	{
		GuiColorPickerCtrl::updatePickColor(offset, contentRect);

		if (mSuppressNextPush)
		{
			mSuppressNextPush = false;
			return;
		}

		mColorPopupCtrl->setAlpha(mPickColor.alpha);
	}
}
#pragma endregion

#pragma region GuiColorPopupSwatchCtrl
GuiColorPopupSwatchCtrl::GuiColorPopupSwatchCtrl(GuiColorPopupCtrl* ctrl)
{
	mColorPopupCtrl = ctrl;
	mDisplayMode = pPallet;
	mBounds.extent.set(20, 20);
	setField("profile", "GuiDefaultProfile");
}

// The same as the pallet-mode render the picker does, with a checkerboard laid in
// first so a translucent swatch doesn't read as the popup's own background.
void GuiColorPopupSwatchCtrl::onRender(Point2I offset, const RectI& updateRect)
{
	RectI ctrlRect = applyMargins(offset, mBounds.extent, NormalState, mProfile);

	renderUniversalRect(ctrlRect, mProfile, NormalState);

	RectI fillRect = applyBorders(ctrlRect.point, ctrlRect.extent, NormalState, mProfile);
	RectI contentRect = applyPadding(fillRect.point, fillRect.extent, NormalState, mProfile);

	if (mBaseColor.alpha < 1.0f)
	{
		dglRenderCheckers(contentRect, 8);
	}

	renderColorBox(contentRect);
}

void GuiColorPopupSwatchCtrl::onAction()
{
	if (!mActive)
		return;

	mColorPopupCtrl->applyColor(mBaseColor);
}
#pragma endregion

#pragma region GuiColorPopupValueCtrl
GuiColorPopupValueCtrl::GuiColorPopupValueCtrl(GuiColorPopupCtrl* ctrl, S32 channel)
{
	mColorPopupCtrl = ctrl;
	mChannel = channel;
	setField("profile", "GuiTextEditProfile");
	setField("align", "center");
}

bool GuiColorPopupValueCtrl::handleEnterKey()
{
	commitChannel();
	return GuiTextEditCtrl::handleEnterKey();
}

void GuiColorPopupValueCtrl::onLoseFirstResponder()
{
	commitChannel();
	GuiTextEditCtrl::onLoseFirstResponder();
}

void GuiColorPopupValueCtrl::commitChannel()
{
	mColorPopupCtrl->onValueBoxCommit(mChannel, getText());
}
#pragma endregion

IMPLEMENT_CONOBJECT(GuiColorPopupCtrl);

GuiColorPopupCtrl::GuiColorPopupCtrl()
{
	mBounds.extent.set(140, 24);
	mIsOpen = false;
	mActive = true;
	mRendersChildren = false;
	mIsContainer = false;
	mBaseColor = ColorF(0.5f, 0.5f, 0.5f);
	mPopupSize = Point2I(240, 208);
	mBarHeight = 20;
	mShowAlphaBar = true;
	mBounds.extent.set(40, 40);

	mSwatchesDirty = false;
	mSwatchColumns = 6;
	mShowColorValues = false;
	mValueMode = ValueMode::Integer;
	mValueBoxHeight = 24;

	setField("profile", "GuiColorPopupProfile");

	mBackground = new GuiColorPopupBGCtrl(this);
	AssertFatal(mBackground, "GuiColorPopupCtrl: Failed to initialize GuiColorPopupBGCtrl!");
	mBackgroundProfile = mBackground->mProfile;
	mBackgroundProfile->incRefCount();

	mContent = new GuiColorPopupContentCtrl();
	AssertFatal(mContent, "GuiColorPopupCtrl: Failed to initialize GuiControl!");
	mContent->setField("profile", "GuiPanelProfile");
	mPopupProfile = mContent->mProfile;
	mPopupProfile->incRefCount();
	mContent->setExtent(mPopupSize);
	RectI contentRect = mContent->getInnerRect();

	mColorAlphaPicker = new GuiColorPopupAlphaCtrl(this);
	AssertFatal(mColorAlphaPicker, "GuiColorPopupCtrl: Failed to initialize GuiColorPopupAlphaCtrl!");
	mColorAlphaPicker->setField("profile", "GuiColorPickerProfile");
	mColorAlphaPicker->setField("displayMode", "horizAlpha");
	mColorAlphaPicker->setExtent(Point2I(contentRect.extent.x, mBarHeight));
	mColorAlphaPicker->showSelector();
	mPickerProfile = mColorAlphaPicker->mProfile;
	mPickerProfile->incRefCount();

	mColorAlphaPicker->setField("selectorProfile", "GuiColorSelectorProfile");
	mSelectorProfile = mColorAlphaPicker->mSelectorProfile;
	mSelectorProfile->incRefCount();

	mColorBlendPicker = new GuiColorPopupBlendCtrl(this, mColorAlphaPicker);
	AssertFatal(mColorBlendPicker, "GuiColorPopupCtrl: Failed to initialize GuiColorPopupBlendCtrl!");
	mColorBlendPicker->setField("profile", "GuiColorPickerProfile");
	mColorBlendPicker->setField("displayMode", "blendColor");
	mColorBlendPicker->setExtent(Point2I(contentRect.extent.x, 100));
	mColorBlendPicker->showSelector();
	mPickerProfile = mColorBlendPicker->mProfile;
	mPickerProfile->incRefCount();

	mColorBlendPicker->setField("selectorProfile", "GuiColorSelectorProfile");
	mSelectorProfile = mColorBlendPicker->mSelectorProfile;
	mSelectorProfile->incRefCount();

	mColorHuePicker = new GuiColorPopupHueCtrl(mColorBlendPicker);
	AssertFatal(mColorHuePicker, "GuiColorPopupCtrl: Failed to initialize GuiColorPopupHueCtrl!");
	mColorHuePicker->setField("profile", "GuiColorPickerProfile");
	mColorHuePicker->setField("displayMode", "horizColor");
	mColorHuePicker->setExtent(Point2I(contentRect.extent.x, mBarHeight));
	mColorHuePicker->showSelector();
	mPickerProfile = mColorHuePicker->mProfile;
	mPickerProfile->incRefCount();

	mColorHuePicker->setField("selectorProfile", "GuiColorSelectorProfile");
	mSelectorProfile = mColorHuePicker->mSelectorProfile;
	mSelectorProfile->incRefCount();

	// The two optional rows exist from the start and are simply hidden when off,
	// the same way the alpha bar is. The grid draws nothing of its own -- the
	// gaps between its cells are what separate one swatch from the next, which
	// matters because a picker profile is often borderless.
	mSwatchGrid = new GuiGridCtrl();
	AssertFatal(mSwatchGrid, "GuiColorPopupCtrl: Failed to initialize GuiGridCtrl!");
	mSwatchGrid->setField("profile", "GuiDefaultProfile");
	mSwatchGrid->setField("cellModeX", "variable");
	mSwatchGrid->setField("cellModeY", "absolute");
	mSwatchGrid->setField("cellSpacingX", "4");
	mSwatchGrid->setField("cellSpacingY", "4");
	mSwatchGrid->setField("orderMode", "lrtb");
	mSwatchGrid->setField("isExtentDynamic", "1");
	mSwatchGrid->setVisible(false);

	const char* channelTip[4] = { "Red", "Green", "Blue", "Alpha" };
	for (S32 i = 0; i < 4; i++)
	{
		mValueBox[i] = new GuiColorPopupValueCtrl(this, i);
		AssertFatal(mValueBox[i], "GuiColorPopupCtrl: Failed to initialize GuiColorPopupValueCtrl!");
		mValueBox[i]->setField("tooltip", channelTip[i]);
		mValueBox[i]->setVisible(false);
	}
	mValueProfile = mValueBox[0]->mProfile;
	mValueProfile->incRefCount();

	mContent->addObject(mColorBlendPicker);
	mContent->addObject(mColorHuePicker);
	mContent->addObject(mColorAlphaPicker);
	mContent->addObject(mSwatchGrid);
	for (S32 i = 0; i < 4; i++)
	{
		mContent->addObject(mValueBox[i]);
	}
	mBackground->addObject(mContent);
}

static EnumTable::Enums gColorPopupValueModeEnums[] =
{
	{ GuiColorPopupCtrl::Integer,	"Integer" },
	{ GuiColorPopupCtrl::Float,		"Float" }
};

static EnumTable gColorPopupValueModeTable(2, gColorPopupValueModeEnums);

void GuiColorPopupCtrl::initPersistFields()
{
	Parent::initPersistFields();

	addGroup("Color Popup");
	addField("backgroundProfile", TypeGuiProfile, Offset(mBackgroundProfile, GuiColorPopupCtrl));
	addField("popupProfile", TypeGuiProfile, Offset(mPopupProfile, GuiColorPopupCtrl));
	addField("pickerProfile", TypeGuiProfile, Offset(mPickerProfile, GuiColorPopupCtrl));
	addField("selectorProfile", TypeGuiProfile, Offset(mSelectorProfile, GuiColorPopupCtrl));
	addField("valueProfile", TypeGuiProfile, Offset(mValueProfile, GuiColorPopupCtrl));
	addField("baseColor", TypeColorF, Offset(mBaseColor, GuiColorPopupCtrl));
	addField("popupSize", TypePoint2I, Offset(mPopupSize, GuiColorPopupCtrl));
	addField("barHeight", TypeS32, Offset(mBarHeight, GuiColorPopupCtrl));
	addField("showAlphaBar", TypeBool, Offset(mShowAlphaBar, GuiColorPopupCtrl));
	addField("swatchColumns", TypeS32, Offset(mSwatchColumns, GuiColorPopupCtrl));
	addField("showColorValues", TypeBool, Offset(mShowColorValues, GuiColorPopupCtrl));
	addField("valueMode", TypeEnum, Offset(mValueMode, GuiColorPopupCtrl), 1, &gColorPopupValueModeTable);
	addField("valueBoxHeight", TypeS32, Offset(mValueBoxHeight, GuiColorPopupCtrl));
	endGroup("Color Popup");
}

void GuiColorPopupCtrl::onTouchUp(const GuiEvent& event)
{
	if (!mActive)
		return;

	Parent::onTouchUp(event);

	mouseUnlock();

	if (!mIsOpen)
	{
		openColorPopup();
	}
	else
	{
		closeColorPopup();
	}
}

GuiControlState GuiColorPopupCtrl::getCurrentState()
{
	if (!mActive)
		return GuiControlState::DisabledState;
	else if (mDepressed || mIsOpen)
		return GuiControlState::SelectedState;
	else if (mMouseOver)
		return GuiControlState::HighlightState;
	else
		return GuiControlState::NormalState;
}

void GuiColorPopupCtrl::onRender(Point2I offset, const RectI& updateRect)
{
	GuiControlState currentState = getCurrentState();
	RectI ctrlRect = applyMargins(offset, mBounds.extent, currentState, mProfile);

	renderUniversalRect(ctrlRect, mProfile, currentState, blankColor, true);

	//Get the content area
	RectI fillRect = applyBorders(ctrlRect.point, ctrlRect.extent, currentState, mProfile);
	RectI contentRect = applyPadding(fillRect.point, fillRect.extent, currentState, mProfile);

	if (mBaseColor.alpha < 1.0f)
	{
		if (mBounds.extent.x < 50 || mBounds.extent.y < 50)
		{
			dglRenderCheckers(contentRect, 8);
		}
		else 
		{
			dglRenderCheckers(contentRect);
		}
	}
	dglDrawRectFill(contentRect, mBaseColor);
}

bool GuiColorPopupCtrl::onKeyDown(const GuiEvent& event)
{
	//if the control is a dead end, don't process the input:
	if (!mVisible || !mActive || !mAwake)
		return false;

	//see if the key down is a <return> or not
	if (event.keyCode == KEY_RETURN && event.modifier == 0)
	{
		if (!mIsOpen)
		{
			openColorPopup();
		}
		else
		{
			closeColorPopup();
		}
		return true;
	}

	return false;
}

void GuiColorPopupCtrl::onAction() //called when the button is clicked.
{
	if (!mActive)
		return;

	setUpdate();
}

void GuiColorPopupCtrl::openColorPopup()
{
	if (mIsOpen)
		return;

	GuiCanvas* root = getRoot();
	AssertFatal(root, "GuiColorPopupCtrl::openColorPopup: Unable to optain the Canvas!");
	mBackground->mBounds.extent = root->mBounds.extent;

	// The popup's contents are the script's to arrange before anything is
	// measured: swatches added from onOpen have to be in place for the popup to
	// know how tall it needs to be. That is why the callback fires up front
	// rather than after the popup is on screen.
	if (isMethod("onOpen"))
		Con::executef(this, 1, "onOpen");

	//Update all pass through values
	mBackground->setControlProfile(mBackgroundProfile);
	mContent->setControlProfile(mPopupProfile);
	mColorBlendPicker->setControlProfile(mPickerProfile);
	mColorBlendPicker->setControlSelectorProfile(mSelectorProfile);
	mColorHuePicker->setControlProfile(mPickerProfile);
	mColorHuePicker->setControlSelectorProfile(mSelectorProfile);
	mColorAlphaPicker->setControlProfile(mPickerProfile);
	mColorAlphaPicker->setControlSelectorProfile(mSelectorProfile);

	rebuildSwatches();
	for (S32 i = 0; i < mSwatchCells.size(); i++)
	{
		mSwatchCells[i]->setControlProfile(mPickerProfile);
	}

	// The boxes name their channel through a tooltip, so they are the one part of
	// the popup that draws tips of its own. They wear the popup's tooltip profile
	// rather than working one out for themselves, which keeps the whole popup
	// looking like one control -- and if the popup was given none, passing NULL
	// on leaves each box to fall back exactly as it would have anyway.
	for (S32 i = 0; i < 4; i++)
	{
		mValueBox[i]->setControlProfile(mValueProfile);
		mValueBox[i]->setControlTooltipProfile(mTooltipProfile);
		mValueBox[i]->setInputMode(mValueMode == ValueMode::Float ? GuiTextEditCtrl::Decimal : GuiTextEditCtrl::Number);
		mValueBox[i]->setActive(mShowColorValues);
		mValueBox[i]->setVisible(mShowColorValues);
	}

	// popupSize covers the wheel and the bars; each optional row is added below
	// them, so turning a row on never costs the wheel any of its height.
	mContent->setExtent(mPopupSize);
	RectI contentRect = mContent->getInnerRect();

	S32 width = mPopupSize.x;
	S32 height = mPopupSize.y + measureSwatchRow(contentRect.extent.x) + measureValueRow();
	Point2I pos = localToGlobalCoord(Point2I(0, 0));

	//Is there enough space below?
	if ((height + pos.y + mBounds.extent.y) <= root->mBounds.extent.y)
	{
		pos.y += mBounds.extent.y;
	}
	else if (height <= pos.y) //Is there enough space above?
	{
		pos.y -= height;
	}
	else if (pos.y < (root->mBounds.extent.y - (pos.y + mBounds.extent.y))) //Is there more space below?
	{
		pos.y += mBounds.extent.y;
		height = root->mBounds.extent.y - pos.y;
	}
	else //There must be more space above
	{
		height = pos.y;
		pos.y = 0;
	}

	mContent->resize(pos, Point2I(width, height));
	layoutPopupContent(mContent->getInnerRect());

	// After the layout, because moving a picker rescales where its selector sits.
	syncPickersToColor();
	refreshValueBoxes();

	root->pushDialogControl(mBackground, 99);

	mIsOpen = true;

	setFirstResponder();
}

S32 GuiColorPopupCtrl::measureSwatchRow(const S32 contentWidth)
{
	if (mSwatchColors.size() == 0)
	{
		mSwatchGrid->setVisible(false);
		return 0;
	}

	mSwatchGrid->setVisible(true);
	mSwatchGrid->setMaxColCount(getMax(mSwatchColumns, 1));
	mSwatchGrid->setCellSize((F32)mBarHeight, (F32)mBarHeight);

	// The grid has a dynamic extent, so laying it out at the width it will get is
	// what tells us how many rows the swatches wrapped into.
	mSwatchGrid->resize(Point2I(0, 0), Point2I(contentWidth, mBarHeight));

	return mSwatchGrid->getExtent().y;
}

void GuiColorPopupCtrl::layoutPopupContent(const RectI& contentRect)
{
	const S32 width = contentRect.extent.x;
	S32 bottom = contentRect.extent.y;

	if (mShowColorValues)
	{
		const S32 gap = 4;
		const S32 boxWidth = (width - (3 * gap)) / 4;
		bottom -= mValueBoxHeight;
		for (S32 i = 0; i < 4; i++)
		{
			mValueBox[i]->resize(Point2I(i * (boxWidth + gap), bottom), Point2I(boxWidth, mValueBoxHeight));
		}
	}

	if (mSwatchGrid->isVisible())
	{
		const S32 gridHeight = mSwatchGrid->getExtent().y;
		bottom -= gridHeight;
		mSwatchGrid->resize(Point2I(0, bottom), Point2I(width, gridHeight));
	}

	if (mShowAlphaBar)
	{
		mColorAlphaPicker->setActive(true);
		mColorAlphaPicker->setVisible(true);
		bottom -= mBarHeight;
		mColorAlphaPicker->resize(Point2I(0, bottom), Point2I(width, mBarHeight));
	}
	else
	{
		mColorAlphaPicker->setActive(false);
		mColorAlphaPicker->setVisible(false);
	}

	bottom -= mBarHeight;
	mColorHuePicker->resize(Point2I(0, bottom), Point2I(width, mBarHeight));

	mColorBlendPicker->resize(Point2I(0, 0), Point2I(width, bottom));
}

void GuiColorPopupCtrl::syncPickersToColor()
{
	ColorF hueColor = mBaseColor.getHueColor();
	ColorF alphaBase = mBaseColor;

	Point2I huePos = mColorHuePicker->getSelectorPositionForColor(mBaseColor);
	mColorHuePicker->setSelectorPos(huePos);
	mColorHuePicker->suppressNextPush();

	mColorBlendPicker->setValue(hueColor);
	Point2I selPos = mColorBlendPicker->getSelectorPositionForColor(mBaseColor);
	mColorBlendPicker->setSelectorPos(selPos);
	mColorBlendPicker->suppressNextPush();

	mColorAlphaPicker->setValue(alphaBase);
	Point2I alphaPos = mColorAlphaPicker->getSelectorPositionForColor(mBaseColor);
	mColorAlphaPicker->setSelectorPos(alphaPos);
	mColorAlphaPicker->suppressNextPush();
}

void GuiColorPopupCtrl::rebuildSwatches()
{
	if (!mSwatchesDirty)
		return;

	mSwatchesDirty = false;

	// Cells outlive the colors that made them: the pool grows to the longest list
	// ever shown and the spares are hidden, so the grid's children stay put while
	// the popup is live. A hidden child takes no cell, so the rest close up.
	while (mSwatchCells.size() < mSwatchColors.size())
	{
		GuiColorPopupSwatchCtrl* cell = new GuiColorPopupSwatchCtrl(this);
		AssertFatal(cell, "GuiColorPopupCtrl: Failed to initialize GuiColorPopupSwatchCtrl!");
		mSwatchCells.push_back(cell);
		mSwatchGrid->addObject(cell);
	}

	for (S32 i = 0; i < mSwatchCells.size(); i++)
	{
		const bool used = i < mSwatchColors.size();
		if (used)
		{
			ColorF color = ColorF(mSwatchColors[i]);
			mSwatchCells[i]->setValue(color);
		}
		mSwatchCells[i]->setVisible(used);
		mSwatchCells[i]->setActive(used);
	}
}

void GuiColorPopupCtrl::closeColorPopup()
{
	if (!mIsOpen)
		return;

	GuiCanvas* root = mBackground->getRoot();
	if (!root)
	{
		return;
	}
	root->popDialogControl(mBackground);

	mIsOpen = false;

	if (isMethod("onClose"))
		Con::executef(this, 1, "onClose");

	if (mConsoleCommand[0])
		Con::evaluate(mConsoleCommand, false);
}

bool GuiColorPopupCtrl::onWake()
{
	if (!Parent::onWake())
		return false;

	if (mBackgroundProfile != NULL)
		mBackgroundProfile->incRefCount();

	if (mPopupProfile != NULL)
		mPopupProfile->incRefCount();

	if (mPickerProfile != NULL)
		mPickerProfile->incRefCount();

	if (mSelectorProfile != NULL)
		mSelectorProfile->incRefCount();

	if (mValueProfile != NULL)
		mValueProfile->incRefCount();

	return true;
}

void GuiColorPopupCtrl::onSleep()
{
	Parent::onSleep();

	if (mBackgroundProfile != NULL)
		mBackgroundProfile->decRefCount();

	if (mPopupProfile != NULL)
		mPopupProfile->decRefCount();

	if (mPickerProfile != NULL)
		mPickerProfile->decRefCount();

	if (mSelectorProfile != NULL)
		mSelectorProfile->decRefCount();

	if (mValueProfile != NULL)
		mValueProfile->decRefCount();
}

void GuiColorPopupCtrl::setControlBackgroundProfile(GuiControlProfile* prof)
{
	AssertFatal(prof, "GuiColorPopupCtrl::setControlBackgroundProfile: invalid background profile");
	if (prof == mBackgroundProfile)
		return;
	if (mAwake)
		mBackgroundProfile->decRefCount();
	mBackgroundProfile = prof;
	if (mAwake)
		mBackgroundProfile->incRefCount();
}

void GuiColorPopupCtrl::setControlPopupProfile(GuiControlProfile* prof)
{
	AssertFatal(prof, "GuiColorPopupCtrl::setControlPopupProfile: invalid popup profile");
	if (prof == mPopupProfile)
		return;
	if (mAwake)
		mPopupProfile->decRefCount();
	mPopupProfile = prof;
	if (mAwake)
		mPopupProfile->incRefCount();
}

void GuiColorPopupCtrl::setControlPickerProfile(GuiControlProfile* prof)
{
	AssertFatal(prof, "GuiColorPopupCtrl::setControlPickerProfile: invalid picker profile");
	if (prof == mPickerProfile)
		return;
	if (mAwake)
		mPickerProfile->decRefCount();
	mPickerProfile = prof;
	if (mAwake)
		mPickerProfile->incRefCount();
}

void GuiColorPopupCtrl::setControlSelectorProfile(GuiControlProfile* prof)
{
	AssertFatal(prof, "GuiColorPopupCtrl::setControlSelectorProfile: invalid selector profile");
	if (prof == mSelectorProfile)
		return;
	if (mAwake)
		mSelectorProfile->decRefCount();
	mSelectorProfile = prof;
	if (mAwake)
		mSelectorProfile->incRefCount();
}

void GuiColorPopupCtrl::setControlValueProfile(GuiControlProfile* prof)
{
	AssertFatal(prof, "GuiColorPopupCtrl::setControlValueProfile: invalid value profile");
	if (prof == mValueProfile)
		return;
	if (mAwake)
		mValueProfile->decRefCount();
	mValueProfile = prof;
	if (mAwake)
		mValueProfile->incRefCount();
}

// What a picker calls to report the color it just read off itself. It must not
// turn around and move the pickers, or the two would chase each other; that is
// what applyColor is for.
void GuiColorPopupCtrl::setColor(const ColorF& theColor)
{
	mBaseColor.red = theColor.red;
	mBaseColor.green = theColor.green;
	mBaseColor.blue = theColor.blue;
	mBaseColor.alpha = theColor.alpha;

	refreshValueBoxes();
}

void GuiColorPopupCtrl::setAlpha(const F32 alpha)
{
	mBaseColor.alpha = alpha;

	refreshValueBoxes();
}

// The path an exact color takes -- a swatch click, a typed channel, or script
// asking for a color outright. Unlike setColor, the pickers are moved to agree.
void GuiColorPopupCtrl::applyColor(const ColorF& theColor)
{
	setColor(theColor);

	if (mIsOpen)
	{
		syncPickersToColor();
	}
}

//-----------------------------------------------------------------------------
// The swatch row.
//-----------------------------------------------------------------------------

void GuiColorPopupCtrl::addSwatch(const ColorI& color)
{
	mSwatchColors.push_back(color);
	mSwatchesDirty = true;
}

void GuiColorPopupCtrl::clearSwatches()
{
	mSwatchColors.clear();
	mSwatchesDirty = true;
}

void GuiColorPopupCtrl::selectSwatch(const S32 index)
{
	if (index < 0 || index >= mSwatchColors.size())
	{
		Con::warnf("GuiColorPopupCtrl::selectSwatch() - No swatch at index %d!", index);
		return;
	}

	ColorF color = ColorF(mSwatchColors[index]);
	applyColor(color);
}

//-----------------------------------------------------------------------------
// The value row.
//-----------------------------------------------------------------------------

F32 GuiColorPopupCtrl::channelOf(const ColorF& color, const S32 channel)
{
	switch (channel)
	{
	case 0: return color.red;
	case 1: return color.green;
	case 2: return color.blue;
	default: return color.alpha;
	}
}

void GuiColorPopupCtrl::setChannelOf(ColorF& color, const S32 channel, const F32 value)
{
	const F32 clamped = value < 0.0f ? 0.0f : (value > 1.0f ? 1.0f : value);

	switch (channel)
	{
	case 0: color.red = clamped; break;
	case 1: color.green = clamped; break;
	case 2: color.blue = clamped; break;
	default: color.alpha = clamped; break;
	}
}

F32 GuiColorPopupCtrl::getColorChannel(const S32 channel) const
{
	if (channel < 0 || channel > 3)
	{
		Con::warnf("GuiColorPopupCtrl::getColorChannel() - Channel must be 0 (red) through 3 (alpha)!");
		return 0.0f;
	}

	const F32 fraction = channelOf(mBaseColor, channel);
	return mValueMode == ValueMode::Float ? fraction : mRound(fraction * 255.0f);
}

void GuiColorPopupCtrl::setColorChannel(const S32 channel, const F32 value)
{
	if (channel < 0 || channel > 3)
	{
		Con::warnf("GuiColorPopupCtrl::setColorChannel() - Channel must be 0 (red) through 3 (alpha)!");
		return;
	}

	ColorF newColor = mBaseColor;
	setChannelOf(newColor, channel, mValueMode == ValueMode::Float ? value : (value / 255.0f));
	applyColor(newColor);
}

void GuiColorPopupCtrl::onValueBoxCommit(const S32 channel, const char* text)
{
	if (channel < 0 || channel > 3)
		return;

	setColorChannel(channel, mValueMode == ValueMode::Float ? dAtof(text) : (F32)dAtoi(text));

	// The typed text may have been clamped or reformatted on the way in, so this
	// box is written back too -- refreshValueBoxes deliberately leaves alone the
	// box that has focus, which is this one.
	writeValueBox(channel);
}

void GuiColorPopupCtrl::refreshValueBoxes()
{
	if (!mShowColorValues)
		return;

	for (S32 i = 0; i < 4; i++)
	{
		//Never overwrite what the user is in the middle of typing.
		if (mValueBox[i]->isFirstResponder())
			continue;

		writeValueBox(i);
	}
}

void GuiColorPopupCtrl::writeValueBox(const S32 channel)
{
	char buffer[32];
	if (mValueMode == ValueMode::Float)
	{
		dSprintf(buffer, sizeof(buffer), "%g", channelOf(mBaseColor, channel));
	}
	else
	{
		dSprintf(buffer, sizeof(buffer), "%d", (S32)mRound(channelOf(mBaseColor, channel) * 255.0f));
	}
	mValueBox[channel]->setText(buffer);
}

const char* GuiColorPopupCtrl::getScriptValue()
{
	static char temp[256];
	ColorF color = getValue();
	dSprintf(temp, 256, "%g %g %g %g", color.red, color.green, color.blue, color.alpha);
	return temp;
}
  
void GuiColorPopupCtrl::setScriptValue(const char* value)
{
	ColorF newValue;
	dSscanf(value, "%g %g %g %g", &newValue.red, &newValue.green, &newValue.blue, &newValue.alpha);
	setValue(newValue);
}