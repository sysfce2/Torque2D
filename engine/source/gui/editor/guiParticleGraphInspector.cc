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

#include "console/console.h"
#include "console/consoleTypes.h"
#include "graphics/dgl.h"
#include "gui/guiDefaultControlRender.h"
#include "math/rectClipper.h"
#include "gui/guiCanvas.h"

#ifndef _PARTICLE_ASSET_H_
#include "2d/assets/ParticleAsset.h"
#endif

#include "gui/editor/guiParticleGraphInspector.h"

#include "gui/editor/guiParticleGraphInspector_ScriptBinding.h"

IMPLEMENT_CONOBJECT(GuiParticleGraphInspector);

GuiParticleGraphInspector::GuiParticleGraphInspector()
{
	mBounds.extent.set(300, 200);

	mTargetAsset = NULL;
	mTargetField = StringTable->insert("QuantityScale");
	mEmitterIndex = 0;
	mVariationInspector = NULL;
	mMinX = 0;
	mMinXLabel = StringTable->insert("0");
	mMaxX = 1;
	mMaxXLabel = StringTable->insert("1");
	mMinY = 0;
	mMinYLabel = StringTable->insert("0");
	mMaxY = 10;
	mMaxYLabel = StringTable->insert("10");
	mLabelX = StringTable->insert("Time", true);
	mLabelY = StringTable->insert("Value", true);
	mSelectedIndex = -1;
	mDirty = true;
	mPointList = Vector<GraphPoint>();

	// RectI and Point2I leave their members alone, and onTouchDown reads mGridRect.
	// A control can be clicked after inspect() but before its first render.
	mGridRect = RectI(0, 0, 0, 0);
	mCalculationOffset = Point2I(0, 0);

	setField("profile", "GuiDefaultProfile");
}

void GuiParticleGraphInspector::initPersistFields()
{
	Parent::initPersistFields();
}

void GuiParticleGraphInspector::inspectObject(ParticleAsset* object)
{
	mTargetAsset = object;
	mDirty = true;
}

void GuiParticleGraphInspector::setDisplayField(const char* fieldName)
{
	if (mTargetField != StringTable->insert(fieldName, true))
	{
		mSelectedIndex = -1;
	}
	mTargetField = StringTable->insert(fieldName, true);

	mDirty = true;
}

void GuiParticleGraphInspector::setDisplayField(const char* fieldName, U16 index)
{
	// The same field on a different emitter is a different curve, and a point index
	// into the old one addresses whatever happens to sit at that slot in the new.
	if (mEmitterIndex != (U32)index)
	{
		mSelectedIndex = -1;
	}

	mEmitterIndex = index;
	setDisplayField(fieldName);
}

void GuiParticleGraphInspector::setDisplayArea(StringTableEntry minX, StringTableEntry minY, StringTableEntry maxX, StringTableEntry maxY)
{
	char buffer[32];

	mMinX = dAtof(minX);
	mMinXLabel = StringTable->insert(formatAxisLabel(mMinX, buffer, sizeof(buffer)));

	mMinY = dAtof(minY);
	mMinYLabel = StringTable->insert(formatAxisLabel(mMinY, buffer, sizeof(buffer)));

	mMaxX = dAtof(maxX);
	mMaxXLabel = StringTable->insert(formatAxisLabel(mMaxX, buffer, sizeof(buffer)));

	mMaxY = dAtof(maxY);
	mMaxYLabel = StringTable->insert(formatAxisLabel(mMaxY, buffer, sizeof(buffer)));

	mDirty = true;
}

void GuiParticleGraphInspector::setDisplayLabels(const char* labelX, const char* labelY)
{
	mLabelX = StringTable->insert(labelX, true);
	mLabelY = StringTable->insert(labelY, true);
}

ParticleAssetField* GuiParticleGraphInspector::findField(StringTableEntry fieldName)
{
	if (mTargetAsset == NULL || fieldName == NULL || fieldName == StringTable->EmptyString)
	{
		return NULL;
	}

	ParticleAssetFieldCollection& collection = mTargetAsset->getParticleFields();
	ParticleAssetField* field = collection.findField(fieldName);
	if (field != NULL)
	{
		return field;
	}

	const U32 emitterCount = (U32)mTargetAsset->getEmitterCount();
	if (emitterCount == 0)
	{
		// An asset with no emitters yet. Subtracting one from an unsigned zero is
		// how this used to walk off the end.
		return NULL;
	}

	// Clamp before asking rather than after: getEmitter warns on a bad index, and a
	// bad index here is a per-frame condition, so asking politely first is the
	// difference between a quiet editor and sixty warnings a second in the log.
	if (mEmitterIndex >= emitterCount)
	{
		mEmitterIndex = emitterCount - 1;
	}

	ParticleAssetEmitter* emitter = mTargetAsset->getEmitter(mEmitterIndex);
	if (emitter == NULL)
	{
		return NULL;
	}

	return emitter->getParticleFields().findField(fieldName);
}

ParticleAssetField* GuiParticleGraphInspector::getTargetField()
{
	ParticleAssetField* field = findField(mTargetField);

	// A warning, not a fatal. A half-built asset with no emitters reaches here as a
	// matter of course, and in a debug build an AssertFatal is a modal box -- which
	// arrives as a hang rather than as a failure.
	AssertWarn(field != NULL, "GuiParticleGraphInspector::getTargetField() - Unable to find the requested field.");

	return field;
}

bool GuiParticleGraphInspector::repairDataKeys(ParticleAssetField* field)
{
	if (field == NULL)
	{
		return false;
	}

	bool changed = false;
	F32 time = 0.0f;
	U32 count = field->getDataKeyCount();

	for (U32 i = 0; i < count; i++)
	{
		ParticleAssetField::DataKey key = field->getDataKey(i);

		// Force the first key to always be at time zero.
		//
		// Only when it sits after zero: addDataKey inserts in time order and refuses
		// nothing below mMaxTime, so with a key at a negative time the new one lands
		// at index 1 and the removal below would delete what was just added.
		if (i == 0 && key.mTime > 0.0f)
		{
			field->addDataKey(0.0f, key.mValue);
			field->removeDataKey(1);
			key = field->getDataKey(0);
			count = field->getDataKeyCount();
			changed = true;
		}

		// Remove the point if it has a bad time. Do not advance i: the key that
		// followed the one just removed now sits at this index and has not been
		// looked at.
		if (i > 0 && key.mTime <= time)
		{
			field->removeDataKey(i);
			count--;
			i--;
			changed = true;
			continue;
		}

		time = key.mTime;
	}

	return changed;
}

void GuiParticleGraphInspector::resize(const Point2I &newPosition, const Point2I &newExtent)
{
	GuiControl::resize(newPosition, newExtent);
	mDirty = true;
}

void GuiParticleGraphInspector::setControlProfile(GuiControlProfile *prof)
{
	GuiControl::setControlProfile(prof);
	mDirty = true;
}

void GuiParticleGraphInspector::onTouchUp(const GuiEvent &event)
{
	if(mTargetAsset)
		mTargetAsset->refreshAsset();
}

void GuiParticleGraphInspector::onTouchDown(const GuiEvent &event)
{
	if(!mTargetAsset)
		return;

	mSelectedIndex = findHitGraphPoint(event.mousePoint);

	if (mSelectedIndex != -1 && event.mouseClickCount == 2)
	{
		//remove the point
		ParticleAssetField* field = getTargetField();
		if (!field)
			return;

		field->removeDataKey(mSelectedIndex);

		mDirty = true;
	}
	else if (mSelectedIndex == -1 && mGridRect.pointInRect(event.mousePoint))
	{
		//Time to create a new point!
		ParticleAssetField* field = getTargetField();
		if (!field)
			return;

		F32 time = getGraphTime(event.mousePoint.x);
		F32 value = getGraphValue(event.mousePoint.y);
		mSelectedIndex = field->addDataKey(time, value);

		mDirty = true;
	}
}

void GuiParticleGraphInspector::onTouchDragged(const GuiEvent &event)
{
	if (!mTargetAsset)
		return;

	Point2I point = Point2I(mClamp(event.mousePoint.x, mGridRect.point.x, mGridRect.point.x + mGridRect.extent.x), mClamp(event.mousePoint.y, mGridRect.point.y, mGridRect.point.y + mGridRect.extent.y));

	if (mSelectedIndex == 0)
	{
		//Time to move the first point!
		ParticleAssetField* field = getTargetField();
		if (!field)
			return;

		F32 value = getGraphValue(point.y);
		field->setDataKeyValue(mSelectedIndex, value);

		mDirty = true;
	}
	else if (mSelectedIndex > 0)
	{
		//Time to move a point!
		ParticleAssetField* field = getTargetField();
		if (!field)
			return;

		F32 time = getGraphTime(point.x);
		F32 value = getGraphValue(point.y);
		if (time == field->getDataKeyTime(mSelectedIndex) || field->doesKeyExist(time))
		{
			//If we're not moving through time or we tried to drag it into a time with a different point, then just change the value.
			field->setDataKeyValue(mSelectedIndex, value);
		}
		else
		{
			//Time travel! Destroy the old point. Recreate in a new time.
			field->removeDataKey(mSelectedIndex);
			mSelectedIndex = field->addDataKey(time, value);
		}

		mDirty = true;
	}
}

S32 GuiParticleGraphInspector::findHitGraphPoint(const Point2I &point)
{
	for (S32 i = 0; i < mPointList.size(); i++)
	{
		F32 x = (F32)(mPointList[i].mPoint.x - point.x);
		F32 y = (F32)(mPointList[i].mPoint.y - point.y);
		F32 dist = mSqrt((x * x) + (y * y));
		if (dist <= smPointRadius)
		{
			return i;
		}
	}
	return -1;
}

F32 GuiParticleGraphInspector::getGraphValue(const F32 y)
{
	S32 len = mGridRect.extent.y;
	F32 ratio = (F32)((y - mGridRect.point.y) / len);
	ratio = mRound(ratio * 100) / 100; //Snaps to a grid of 100 possible values.
	return mMinY + ((mMaxY - mMinY) * (1 - ratio));
}

F32 GuiParticleGraphInspector::getGraphTime(const F32 x)
{
	S32 len = mGridRect.extent.x;
	F32 ratio = (F32)((x - mGridRect.point.x) / len);
	ratio = mRound(ratio * 100) / 100; //Snaps to a grid of 100 possible values.
	return mMinX + ((mMaxX - mMinX) * ratio);
}

const char* GuiParticleGraphInspector::formatAxisLabel(const F32 value, char* buffer, const U32 bufferSize)
{
	if (buffer == NULL || bufferSize == 0)
	{
		return "";
	}

	dSprintf(buffer, bufferSize, "%g", value);

	return buffer;
}

S32 GuiParticleGraphInspector::getUnderPlotReserve(const S32 bandHeight)
{
	return (bandHeight > 0) ? (bandHeight + (2 * smUnderPlotGap)) : 0;
}

RectI GuiParticleGraphInspector::snapRectToGrid(const RectI &rect, const S32 divisor)
{
	if (divisor < 1 || !rect.isValidRect())
	{
		return rect;
	}

	const S32 modX = rect.len_x() % divisor;
	const S32 modY = rect.len_y() % divisor;

	return RectI(rect.point.x + (modX / 2), rect.point.y + (modY / 2),
		rect.extent.x - modX, rect.extent.y - modY);
}

RectI GuiParticleGraphInspector::calculatePlotRect(const RectI &contentRect)
{
	GFont *font = mProfile->getFont(mFontSizeAdjust);
	const S32 fontHeight = (S32)font->getHeight();

	//Make room for the graph labels, and for a band if a subclass asked for one
	RectI rect = contentRect;
	rect.extent.y -= (fontHeight + getUnderPlotReserve(getUnderPlotBandHeight()));

	const S32 xReduction = getMax(getMax(fontHeight, (S32)font->getStrWidth(mMaxYLabel)), (S32)font->getStrWidth(mMinYLabel));
	rect.extent.x -= xReduction;
	rect.point.x += xReduction;

	return snapRectToGrid(rect, 10);
}

RectI GuiParticleGraphInspector::getUnderPlotRect(const RectI &plotRect)
{
	const S32 bandHeight = getUnderPlotBandHeight();
	if (bandHeight <= 0 || !plotRect.isValidRect())
	{
		return RectI(0, 0, 0, 0);
	}

	return RectI(plotRect.point.x, plotRect.point.y + plotRect.extent.y + smUnderPlotGap,
		plotRect.extent.x, bandHeight);
}

S32 GuiParticleGraphInspector::getXLabelTop(const RectI &plotRect)
{
	return plotRect.point.y + plotRect.extent.y + getUnderPlotReserve(getUnderPlotBandHeight()) + 2;
}

void GuiParticleGraphInspector::onRender(Point2I offset, const RectI &updateRect)
{
	RectI ctrlRect = applyMargins(offset, mBounds.extent, NormalState, mProfile);
	if (!ctrlRect.isValidRect())
	{
		return;
	}
	renderUniversalRect(ctrlRect, mProfile, NormalState);

	RectI fillRect = applyBorders(ctrlRect.point, ctrlRect.extent, NormalState, mProfile);
	RectI contentRect = applyPadding(fillRect.point, fillRect.extent, NormalState, mProfile);

	RectI plotRect = calculatePlotRect(contentRect);

	//Draw the labels
	ColorI gridColor = mProfile->getFillColor(HighlightState);
	renderLabels(plotRect, gridColor);

	if (plotRect.isValidRect())
	{
		renderGrid(plotRect, gridColor);

		// Before the underlay rather than after it: a subclass caching anything of
		// its own has to see the same dirty flag renderPoints is about to clear.
		if (mCalculationOffset != offset)
		{
			mDirty = true;
		}

		renderUnderlay(plotRect);
		renderPoints(plotRect, getCurveColor());
		mCalculationOffset = offset;

		const RectI bandRect = getUnderPlotRect(plotRect);
		if (bandRect.isValidRect())
		{
			renderUnderPlot(bandRect);
		}
	}
}

void GuiParticleGraphInspector::renderLabels(const RectI &contentRect, const ColorI &labelColor)
{
	GFont *font = mProfile->getFont(mFontSizeAdjust);
	U32 fontHeight = font->getHeight();
	U32 textWidth;
	Point2I textPoint;

	//Set the color used for the grid. This will also be used for the text.
	dglSetBitmapModulation(labelColor);

	// The x row sits below anything a subclass reserved a band for, not directly
	// under the plot -- otherwise the labels draw over the band.
	const S32 xLabelTop = getXLabelTop(contentRect);

	//x label
	textWidth = font->getStrWidth(mLabelX);
	textPoint = Point2I(contentRect.point.x + (contentRect.extent.x / 2) - (textWidth / 2), xLabelTop);
	dglDrawText(font, textPoint, mLabelX, NULL, 0, 0);

	//x min label
	textWidth = font->getStrWidth(mMinXLabel);
	textPoint = Point2I(contentRect.point.x + 1, xLabelTop);
	dglDrawText(font, textPoint, mMinXLabel, NULL, 0, 0);

	//x max label
	textWidth = font->getStrWidth(mMaxXLabel);
	textPoint = Point2I((contentRect.point.x + contentRect.extent.x - 1) - textWidth, xLabelTop);
	dglDrawText(font, textPoint, mMaxXLabel, NULL, 0, 0);

	//y label
	textWidth = font->getStrWidth(mLabelY);
	textPoint = Point2I(contentRect.point.x - (fontHeight + 2), contentRect.point.y + (contentRect.extent.y / 2) + (textWidth / 2));
	dglDrawText(font, textPoint, mLabelY, NULL, 0, 90);

	//y min label
	textWidth = font->getStrWidth(mMinYLabel);
	textPoint = Point2I(contentRect.point.x - (textWidth + 2), (contentRect.point.y + contentRect.extent.y - 2) - (fontHeight / 2));
	dglDrawText(font, textPoint, mMinYLabel, NULL, 0, 0);

	//y max label
	textWidth = font->getStrWidth(mMaxYLabel);
	textPoint = Point2I(contentRect.point.x - (textWidth + 2), (contentRect.point.y + 4) - (fontHeight / 2));
	dglDrawText(font, textPoint, mMaxYLabel, NULL, 0, 0);
}
	
void GuiParticleGraphInspector::renderGrid(const RectI &contentRect, const ColorI &gridColor)
{
	S32 x, y;
	x = contentRect.len_x() / 10;
	y = contentRect.len_y() / 10;

	//horizontal lines
	for (U8 i = 0; i < 11; i++)
	{
		if(i != 5)
		{
			dglDrawLine(Point2I(contentRect.point.x, contentRect.point.y + (y * i)), Point2I(contentRect.point.x + contentRect.extent.x, contentRect.point.y + (y * i)), gridColor);
		}
		else
		{
			Point2I p1 = Point2I(contentRect.point.x, contentRect.point.y + (y * i) - 1);
			Point2I p2 = Point2I(contentRect.point.x + contentRect.extent.x, contentRect.point.y + (y * i) - 1);
			Point2I p3 = Point2I(contentRect.point.x + contentRect.extent.x, contentRect.point.y + (y * i) + 1);
			Point2I p4 = Point2I(contentRect.point.x, contentRect.point.y + (y * i) + 1);
			dglDrawQuadFill(p1, p2, p3, p4, gridColor);
		}
	}

	//vertical lines
	for (U8 i = 0; i < 11; i++)
	{
		if (i != 5)
		{
			dglDrawLine(Point2I(contentRect.point.x + (x * i), contentRect.point.y), Point2I(contentRect.point.x + (x * i), contentRect.point.y + contentRect.extent.y), gridColor);
		}
		else
		{
			Point2I p1 = Point2I(contentRect.point.x + (x * i) - 1, contentRect.point.y);
			Point2I p2 = Point2I(contentRect.point.x + (x * i) + 1, contentRect.point.y);
			Point2I p3 = Point2I(contentRect.point.x + (x * i) + 1, contentRect.point.y + contentRect.extent.y);
			Point2I p4 = Point2I(contentRect.point.x + (x * i) - 1, contentRect.point.y + contentRect.extent.y);
			dglDrawQuadFill(p1, p2, p3, p4, gridColor);
		}
	}
}

void GuiParticleGraphInspector::calculatePoints(const RectI &contentRect)
{
	mGridRect = RectI(contentRect);

	mPointList.clear();

	// Cleared here rather than at the end, so an early return still counts as
	// having recalculated and the next frame does not try again.
	mDirty = false;

	ParticleAssetField* field = getTargetField();
	if (field == NULL)
	{
		return;
	}

	if (repairDataKeys(field))
	{
		// A removed key makes the selection an index into something else.
		mSelectedIndex = -1;
	}

	Point2I p;
	const U32 count = field->getDataKeyCount();
	for (U32 i = 0; i < count; i++)
	{
		ParticleAssetField::DataKey key = field->getDataKey(i);
		p = convertToRenderPoint(contentRect, key.mTime, key.mValue);
		mPointList.push_back(GraphPoint(p, key.mTime, key.mValue, i));
	}
	mDirty = false;
}

Point2I GuiParticleGraphInspector::convertToRenderPoint(const RectI& contentRect, F32 time, F32 value)
{
	F32 width = mMaxX - mMinX;
	F32 height = mMaxY - mMinY;

	F32 ratioX = (time - mMinX) / width;
	F32 ratioY = (value - mMinY) / height;

	return Point2I(contentRect.point.x + (contentRect.extent.x * ratioX), contentRect.point.y + (contentRect.extent.y * (1 - ratioY)));
}

void GuiParticleGraphInspector::renderPoints(const RectI &contentRect, const ColorI &lineColor)
{
	if (mTargetAsset)
	{
		if (mDirty)
		{
			calculatePoints(contentRect);
		}

		// The tail below indexes count - 1, and renderVariation walks size() - 1.
		// Both are unsigned, and calculatePoints leaves the list empty when the
		// field it wanted has gone.
		if (mPointList.size() == 0)
		{
			return;
		}

		//get the cursor position
		Point2I cursorPt = Point2I(0, 0);
		GuiCanvas *root = getRoot();
		if (root)
		{
			cursorPt = root->getCursorPos();
		}

		//Render variation
		if (mVariationInspector != NULL)
		{
			ColorI variColor = ColorI(lineColor);
			variColor.alpha /= 2;
			renderVariation(contentRect, variColor);
		}

		//Render the lines
		Point2I p1, p2;
		U32 count = mPointList.size();
		for(U32 i = 1; i < count; i++)
		{
			p1 = mPointList[i-1].mPoint;
			p2 = mPointList[i].mPoint;

			renderLine(contentRect, p1, p2, lineColor);
			renderDot(contentRect, p1, cursorPt, mSelectedIndex == (i - 1));
		}
		p1 = mPointList[count - 1].mPoint;
		p2 = Point2I(contentRect.point.x + contentRect.extent.x, p1.y);
		if (p1.x < p2.x)
		{
			renderLine(contentRect, p1, p2, lineColor);
		}
		renderDot(contentRect, p1, cursorPt, mSelectedIndex == (count - 1));
	}
}

Vector<GuiParticleGraphInspector::GraphPoint>* GuiParticleGraphInspector::getRenderPoints()
{
	if (!mAwake)
	{
		return NULL;
	}

	if (mDirty) 
	{ 
		RectI rect = RectI(0, 0, 1, 1); 
		calculatePoints(rect); mDirty = true; 
	} 
	return &mPointList;
}

void GuiParticleGraphInspector::renderVariation(const RectI& contentRect, const ColorI& color)
{
	Vector<GraphPoint>* variPointList = mVariationInspector->getRenderPoints();
	if (!variPointList)
	{
		return;
	}

	S32 vPen = 0;
	S32 bPen = 0;
	Point2I up1, down1, up2, down2;
	while (vPen < variPointList->size() || bPen < mPointList.size())
	{
		GraphPoint& vari = variPointList->at(getMin(vPen, variPointList->size() - 1));
		GraphPoint& base = mPointList.at(getMin(bPen, mPointList.size() - 1));

		if (vPen == 0 && bPen == 0)
		{
			up1 = convertToRenderPoint(contentRect, 0, base.mValue + vari.mValue);
			down1 = convertToRenderPoint(contentRect, 0, base.mValue - vari.mValue);
			vPen = 1;
			bPen = 1;
			continue;
		}

		if (vPen >= variPointList->size() || bPen >= mPointList.size())
		{
			F32 time = vPen >= variPointList->size() ? base.mTime : vari.mTime;
			up2 = convertToRenderPoint(contentRect, time, base.mValue + vari.mValue);
			down2 = convertToRenderPoint(contentRect, time, base.mValue - vari.mValue);
			vPen++;
			bPen++;
		}
		else if (vari.mTime == base.mTime)
		{
			up2 = convertToRenderPoint(contentRect, base.mTime, base.mValue + vari.mValue);
			down2 = convertToRenderPoint(contentRect, base.mTime, base.mValue - vari.mValue);
			vPen++;
			bPen++;
		}
		else if (vari.mTime < base.mTime)
		{
			GraphPoint& oldBase = mPointList.at(bPen - 1);
			F32 timeDeltaB = base.mTime - oldBase.mTime;
			F32 timeDeltaV = vari.mTime - oldBase.mTime;
			F32 ratio = timeDeltaV / timeDeltaB;
			F32 baseValue = oldBase.mValue + ((base.mValue - oldBase.mValue) * ratio);
			up2 = convertToRenderPoint(contentRect, vari.mTime, baseValue + vari.mValue);
			down2 = convertToRenderPoint(contentRect, vari.mTime, baseValue - vari.mValue);
			vPen++;
		}
		else if (vari.mTime > base.mTime)
		{
			GraphPoint& oldVari = variPointList->at(vPen - 1);
			F32 timeDeltaB = base.mTime - oldVari.mTime;
			F32 timeDeltaV = vari.mTime - oldVari.mTime;
			F32 ratio = timeDeltaB / timeDeltaV;
			F32 variValue = oldVari.mValue + ((vari.mValue - oldVari.mValue) * ratio);
			up2 = convertToRenderPoint(contentRect, base.mTime, base.mValue + variValue);
			down2 = convertToRenderPoint(contentRect, base.mTime, base.mValue - variValue);
			bPen++;
		}
		renderQuad(contentRect, up1, up2, down1, down2, color);
		up1 = up2;
		down1 = down2;
	}
	up2 = Point2I(contentRect.point.x + contentRect.extent.x, up1.y);
	down2 = Point2I(contentRect.point.x + contentRect.extent.x, down1.y);
	if (up1.x < up2.x)
	{
		renderQuad(contentRect, up1, up2, down1, down2, color);
	}
}

void GuiParticleGraphInspector::renderDot(const RectI &contentRect, const Point2I &point, const Point2I &cursorPt, bool isSelected)
{
	if(point.x >= contentRect.point.x && point.x <= contentRect.point.x + contentRect.extent.x && point.y >= contentRect.point.y && point.y <= contentRect.point.y + contentRect.extent.y)
	{
		F32 x = (F32)(cursorPt.x - point.x);
		F32 y = (F32)(cursorPt.y - point.y);
		F32 dist = mSqrt((x * x) + (y * y));
		ColorI color;
		if (isSelected)
		{
			color = mProfile->getFontColor(SelectedState);
		}
		else if (dist <= smPointRadius)
		{
			color = mProfile->getFontColor(HighlightState);
		}
		else
		{
			color = mProfile->getFontColor(NormalState);
		}

		dglDrawCircleFill(point, smPointRadius, ColorI(0, 0, 0, 100));
		dglDrawCircleFill(point, smPointRadius - 2, color);
	}
}

void GuiParticleGraphInspector::renderLine(const RectI &contentRect, const Point2I &point1, const Point2I &point2, const ColorI &lineColor)
{
	RectClipper clipper = RectClipper(contentRect);

	Point2I p1;
	Point2I p2;
	if(clipper.clipLine(point1, point2, p1, p2))
	{
		dglDrawLine(p1, p2, ColorI(lineColor));
	}
}

//Points are leftTop, rightTop, leftBottom, rightBottom
void GuiParticleGraphInspector::renderQuad(const RectI& contentRect, const Point2I& point1, const Point2I& point2, const Point2I& point3, const Point2I& point4, const ColorI& quadColor)
{
	//if the heights of the left and right sides are both zero then we can exit now
	if ((point1.y - point3.y) == 0 && (point2.y - point4.y) == 0)
	{
		return;
	}

	RectI area = RectI(point1.x, getMin(point1.y, point2.y), point2.x - point1.x, point1.y < point2.y ? getMax(point3.y, point4.y) - point1.y : getMax(point3.y, point4.y) - point2.y);
	if (!contentRect.overlaps(area))
	{
		//Nothing to draw here...
		return;
	}

	if ((point1.y > point4.y || point2.y > point3.y) && area.extent.y > 1)
	{
		Point2I point5 = Point2I(mRound((point1.x + point2.x) / 2), mRound((point1.y + point2.y) / 2));
		Point2I point6 = Point2I(mRound((point3.x + point4.x) / 2), mRound((point3.y + point4.y) / 2));
		renderQuad(contentRect, point1, point5, point3, point6, quadColor);
		renderQuad(contentRect, point5, point2, point6, point4, quadColor);
		return;
	}

	RectClipper clipper = RectClipper(contentRect);

	Point2I topStart;
	Point2I topEnd;
	bool hasTop = clipper.clipLine(point1, point2, topStart, topEnd);

	Point2I bottomStart;
	Point2I bottomEnd;
	bool hasBottom = clipper.clipLine(point3, point4, bottomStart, bottomEnd);

	Point2I leftStart;
	Point2I leftEnd;
	bool hasLeft = clipper.clipLine(point1, point3, leftStart, leftEnd);

	Point2I rightStart;
	Point2I rightEnd;
	bool hasRight = clipper.clipLine(point2, point4, rightStart, rightEnd);

	//Replace left and right if they're missing
	if (!hasLeft)
	{
		leftStart.x = leftEnd.x = contentRect.point.x;
		leftStart.y = hasTop ? topStart.y : contentRect.point.y;
		leftEnd.y = hasBottom ? bottomStart.y : contentRect.point.y + contentRect.extent.y;
	}
	if (!hasRight)
	{
		rightStart.x = rightEnd.x = contentRect.point.x + contentRect.extent.x - 1;
		rightStart.y = hasTop ? topEnd.y : contentRect.point.y;
		rightEnd.y = hasBottom ? bottomEnd.y : contentRect.point.y + contentRect.extent.y;
	}

	S32 leftEdge = leftStart.x;
	S32 rightEdge = rightStart.x;

	//Middle Section
	S32 y = getMax(leftStart.y, rightStart.y);
	S32 h = getMin(leftEnd.y - y, rightEnd.y - y);
	RectI fillRect = RectI(leftStart.x, y, rightStart.x - leftStart.x, h);
	dglDrawRectFill(fillRect, quadColor);

	//Top Section
	if (hasTop && topStart.y != topEnd.y)
	{
		if (leftEdge != topStart.x && topStart.y < topEnd.y)
		{
			RectI rect = RectI(leftEdge, topStart.y, topStart.x - leftEdge, topEnd.y - topStart.y);
			dglDrawRectFill(rect, quadColor);

			Point2I p = Point2I(topStart.x, topEnd.y);
			dglDrawTriangleFill(topStart, p, topEnd, quadColor);
		}
		else if (rightEdge != topEnd.x && topStart.y > topEnd.y)
		{
			RectI rect = RectI(topEnd.x, topEnd.y, rightEdge - topEnd.x, topStart.y - topEnd.y);
			dglDrawRectFill(rect, quadColor);

			Point2I p = Point2I(topEnd.x, topStart.y);
			dglDrawTriangleFill(topStart, p, topEnd, quadColor);
		}
		else if (topStart.y > topEnd.y)
		{
			Point2I p = Point2I(topEnd.x, topStart.y);
			dglDrawTriangleFill(topStart, p, topEnd, quadColor);
		}
		else if (topStart.y < topEnd.y)
		{
			Point2I p = Point2I(topStart.x, topEnd.y);
			dglDrawTriangleFill(topStart, p, topEnd, quadColor);
		}
	}

	//Bottom Section
	if (hasBottom && bottomStart.y != bottomEnd.y)
	{
		if (leftEdge != bottomStart.x && bottomStart.y > bottomEnd.y)
		{
			RectI rect = RectI(leftEdge, bottomEnd.y, bottomStart.x - leftEdge, bottomStart.y - bottomEnd.y);
			dglDrawRectFill(rect, quadColor);

			Point2I p = Point2I(bottomStart.x, bottomEnd.y);
			dglDrawTriangleFill(bottomEnd, p, bottomStart, quadColor);
		}
		else if (rightEdge != bottomEnd.x && bottomStart.y < bottomEnd.y)
		{
			RectI rect = RectI(bottomEnd.x, bottomStart.y, rightEdge - bottomEnd.x, bottomEnd.y - bottomStart.y);
			dglDrawRectFill(rect, quadColor);

			Point2I p = Point2I(bottomEnd.x, bottomStart.y);
			dglDrawTriangleFill(bottomStart, bottomEnd, p, quadColor);
		}
		else if (bottomStart.y < bottomEnd.y)
		{
			Point2I p = Point2I(bottomEnd.x, bottomStart.y);
			dglDrawTriangleFill(bottomStart, bottomEnd, p, quadColor);
		}
		else if (bottomStart.y > bottomEnd.y)
		{
			Point2I p = Point2I(bottomStart.x, bottomEnd.y);
			dglDrawTriangleFill(bottomStart, bottomEnd, p, quadColor);
		}
	}
}