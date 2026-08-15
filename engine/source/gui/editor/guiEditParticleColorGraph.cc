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

#ifndef _PARTICLE_ASSET_H_
#include "2d/assets/ParticleAsset.h"
#endif

#include "gui/editor/guiEditParticleColorGraph.h"

#include "guiEditParticleColorGraph_ScriptBinding.h"

IMPLEMENT_CONOBJECT(GuiEditParticleColorGraph);

//-----------------------------------------------------------------------------
// Arithmetic
//-----------------------------------------------------------------------------

S32 GuiEditParticleColorGraph::clampStripHeight(const S32 height)
{
	// Zero is a legal answer and means no strip. Anything else is dragged up to
	// something a gradient can actually be read in.
	if (height <= 0)
	{
		return 0;
	}

	return mClamp(height, smMinStripHeight, smMaxStripHeight);
}

F32 GuiEditParticleColorGraph::sampleChannel(const F32* times, const F32* values, const S32 count, const F32 time)
{
	if (times == NULL || values == NULL || count <= 0)
	{
		return 0.0f;
	}

	// Flat before the first key and flat after the last, which is what the curve
	// draws: renderPoints runs the final value out to the right edge, and there is
	// nothing to the left of key zero because key zero is always at time zero.
	if (time <= times[0])
	{
		return values[0];
	}

	if (time >= times[count - 1])
	{
		return values[count - 1];
	}

	for (S32 i = 1; i < count; i++)
	{
		if (time <= times[i])
		{
			const F32 span = times[i] - times[i - 1];
			if (span <= 0.0f)
			{
				return values[i];
			}

			const F32 ratio = (time - times[i - 1]) / span;
			return (values[i - 1] * (1.0f - ratio)) + (values[i] * ratio);
		}
	}

	return values[count - 1];
}

S32 GuiEditParticleColorGraph::buildGradientStops(const F32* redTimes, const S32 redCount,
	const F32* greenTimes, const S32 greenCount,
	const F32* blueTimes, const S32 blueCount,
	const F32 windowMin, const F32 windowMax,
	F32* outStops, const S32 maxStops)
{
	if (outStops == NULL || maxStops < 2 || (windowMax - windowMin) <= smStopEpsilon)
	{
		return 0;
	}

	const F32* times[ChannelCount] = { redTimes, greenTimes, blueTimes };
	S32 counts[ChannelCount] = { redCount, greenCount, blueCount };
	S32 pen[ChannelCount] = { 0, 0, 0 };

	for (S32 c = 0; c < ChannelCount; c++)
	{
		if (times[c] == NULL)
		{
			counts[c] = 0;
		}
	}

	S32 count = 0;
	outStops[count++] = windowMin;

	// One slot is always held back so the window's far edge can be written no
	// matter how many keys were merged.
	while (count < (maxStops - 1))
	{
		S32 which = -1;
		F32 best = 0.0f;

		for (S32 c = 0; c < ChannelCount; c++)
		{
			// Skip everything at or before the stop just written. This is the
			// de-duplication -- within a channel and across all three at once -- and
			// it is also what clips the head of each list to the window.
			while (pen[c] < counts[c] && times[c][pen[c]] <= (outStops[count - 1] + smStopEpsilon))
			{
				pen[c]++;
			}

			if (pen[c] < counts[c] && (which == -1 || times[c][pen[c]] < best))
			{
				which = c;
				best = times[c][pen[c]];
			}
		}

		if (which == -1 || best >= (windowMax - smStopEpsilon))
		{
			break;
		}

		outStops[count++] = best;
		pen[which]++;
	}

	outStops[count++] = windowMax;

	return count;
}

S32 GuiEditParticleColorGraph::timeToPixel(const F32 time, const F32 windowMin, const F32 windowMax,
	const S32 rectLeft, const S32 rectWidth)
{
	const F32 span = windowMax - windowMin;
	if (span <= 0.0f || rectWidth <= 0)
	{
		return rectLeft;
	}

	const F32 ratio = (time - windowMin) / span;

	return mClamp(rectLeft + (S32)mFloor(ratio * (F32)rectWidth), rectLeft, rectLeft + rectWidth);
}

StringTableEntry GuiEditParticleColorGraph::getChannelFieldName(const Channel channel)
{
	switch (channel)
	{
	case ChannelGreen:
		return StringTable->insert("GreenChannel", true);
	case ChannelBlue:
		return StringTable->insert("BlueChannel", true);
	default:
		return StringTable->insert("RedChannel", true);
	}
}

GuiEditParticleColorGraph::Channel GuiEditParticleColorGraph::getChannelFromName(const char* name)
{
	if (name == NULL || *name == 0)
	{
		return ChannelCount;
	}

	// dStricmp rather than a string table compare: this is the one place a
	// caller's spelling is allowed, and both the short and the field name are
	// accepted so script can pass either.
	if (dStricmp(name, "Red") == 0 || dStricmp(name, "RedChannel") == 0)
	{
		return ChannelRed;
	}

	if (dStricmp(name, "Green") == 0 || dStricmp(name, "GreenChannel") == 0)
	{
		return ChannelGreen;
	}

	if (dStricmp(name, "Blue") == 0 || dStricmp(name, "BlueChannel") == 0)
	{
		return ChannelBlue;
	}

	return ChannelCount;
}

ColorI GuiEditParticleColorGraph::getChannelColor(const Channel channel, const bool isActive)
{
	// Lifted off the primaries: a pure blue line on a dark panel is close to
	// unreadable, and pure red is not much better. These stay unmistakably red,
	// green and blue while being legible on every editor theme.
	//
	// The inactive form is the same hue at a lower alpha rather than a darker one,
	// so it reads as sitting behind the live curve rather than as a fourth color.
	const U8 alpha = isActive ? 255 : 110;

	switch (channel)
	{
	case ChannelGreen:
		return ColorI(90, 220, 110, alpha);
	case ChannelBlue:
		return ColorI(105, 155, 255, alpha);
	default:
		return ColorI(255, 95, 95, alpha);
	}
}

//-----------------------------------------------------------------------------

GuiEditParticleColorGraph::GuiEditParticleColorGraph()
{
	mActiveChannel = ChannelRed;
	mStripHeight = smDefaultStripHeight;

	// The parent's defaults describe a scale field, and they are what the very
	// first frame draws with -- before any script has called setDisplayArea.
	mTargetField = getChannelFieldName(ChannelRed);
	mMinY = 0.0f;
	mMinYLabel = StringTable->insert("0");
	mMaxY = 1.0f;
	mMaxYLabel = StringTable->insert("1");
	mLabelY = StringTable->insert("Color", true);

	mCacheValid = false;
	mCacheAsset = NULL;
	mCacheEmitterIndex = 0;
	mCacheMinX = 0.0f;
	mCacheMaxX = 0.0f;
}

void GuiEditParticleColorGraph::initPersistFields()
{
	Parent::initPersistFields();

	addProtectedField("StripHeight", TypeS32, Offset(mStripHeight, GuiEditParticleColorGraph),
		&setStripHeightField, &getStripHeightField,
		"How tall the mixed color strip under the plot is drawn. Zero for no strip.");
}

void GuiEditParticleColorGraph::setActiveChannel(const Channel channel)
{
	if (channel < 0 || channel >= ChannelCount)
	{
		return;
	}

	// Through the parent's target field, which is the whole trick: the live
	// channel is the field it edits, so the hit test, the drag and the refresh on
	// release all belong to it and none of them are written twice.
	setDisplayField(getChannelFieldName(channel));
}

void GuiEditParticleColorGraph::setDisplayField(const char* fieldName)
{
	const Channel channel = getChannelFromName(fieldName);
	if (channel == ChannelCount)
	{
		Con::warnf("GuiEditParticleColorGraph::setDisplayField() - '%s' is not a color channel.", fieldName);
		return;
	}

	mActiveChannel = channel;

	// The channel's own spelling, never the caller's: the field lookup folds case
	// but the string table entry is interned case-sensitively, so passing "red"
	// straight through would make a second entry that compares equal to nothing.
	Parent::setDisplayField(getChannelFieldName(channel));
}

void GuiEditParticleColorGraph::setStripHeight(const S32 height)
{
	const S32 clamped = clampStripHeight(height);
	if (clamped == mStripHeight)
	{
		return;
	}

	mStripHeight = clamped;

	// The band changes the plot rect, and mGridRect -- which is what a click is
	// tested against -- is only rebuilt on a dirty frame.
	mDirty = true;
}

void GuiEditParticleColorGraph::setVariationGraphInspector(GuiParticleGraphInspector* object)
{
	Con::warnf("GuiEditParticleColorGraph::setVariationGraphInspector() - A color graph has no variation to shade.");
}

ColorF GuiEditParticleColorGraph::sampleColorAtTime(const F32 time)
{
	F32 component[ChannelCount];
	for (S32 c = 0; c < ChannelCount; c++)
	{
		component[c] = mClampF(sampleChannel(mChannelTimes[c].address(), mChannelValues[c].address(),
			(S32)mChannelTimes[c].size(), time), 0.0f, 1.0f);
	}

	// Always opaque. The strip answers what hues a particle passes through; alpha
	// is its own graph and mixing it in here would darken every reading.
	return ColorF(component[ChannelRed], component[ChannelGreen], component[ChannelBlue], 1.0f);
}

ColorF GuiEditParticleColorGraph::getColorAtTime(const F32 time)
{
	refreshChannelCaches();

	return sampleColorAtTime(time);
}

const char* GuiEditParticleColorGraph::getGradientStopList()
{
	refreshChannelCaches();

	if (mGradientStops.size() == 0)
	{
		return StringTable->EmptyString;
	}

	const S32 stopCount = (S32)mGradientStops.size();
	const U32 bufferSize = (U32)stopCount * 16;
	char* buffer = Con::getReturnBuffer(bufferSize);
	U32 offset = 0;

	for (S32 i = 0; i < stopCount; i++)
	{
		offset += dSprintf(buffer + offset, bufferSize - offset, (i == 0) ? "%g" : " %g", mGradientStops[i]);
	}

	return buffer;
}

//-----------------------------------------------------------------------------

void GuiEditParticleColorGraph::refreshChannelCaches()
{
	const bool moved = !mCacheValid
		|| mDirty
		|| mCacheAsset != mTargetAsset
		|| mCacheEmitterIndex != mEmitterIndex
		|| mCacheMinX != mMinX
		|| mCacheMaxX != mMaxX;

	if (!moved)
	{
		return;
	}

	for (S32 c = 0; c < ChannelCount; c++)
	{
		mChannelTimes[c].clear();
		mChannelValues[c].clear();
	}
	mGradientStops.clear();

	mCacheValid = true;
	mCacheAsset = mTargetAsset;
	mCacheEmitterIndex = mEmitterIndex;
	mCacheMinX = mMinX;
	mCacheMaxX = mMaxX;

	if (mTargetAsset == NULL)
	{
		return;
	}

	// The active channel first. This runs before calculatePoints, so repairing it
	// here means the curve above the strip and the strip itself are built from the
	// same keys on the same frame. repairDataKeys is idempotent, so the call
	// calculatePoints makes next walks the list and changes nothing.
	for (S32 i = 0; i < ChannelCount; i++)
	{
		const Channel channel = (Channel)((mActiveChannel + i) % ChannelCount);

		ParticleAssetField* field = findField(getChannelFieldName(channel));
		if (field == NULL)
		{
			continue;
		}

		repairDataKeys(field);

		const U32 count = field->getDataKeyCount();
		for (U32 k = 0; k < count; k++)
		{
			const ParticleAssetField::DataKey& key = field->getDataKey(k);
			mChannelTimes[channel].push_back(key.mTime);
			mChannelValues[channel].push_back(key.mValue);
		}
	}

	F32 stops[smMaxGradientStops];
	const S32 stopCount = buildGradientStops(
		mChannelTimes[ChannelRed].address(), (S32)mChannelTimes[ChannelRed].size(),
		mChannelTimes[ChannelGreen].address(), (S32)mChannelTimes[ChannelGreen].size(),
		mChannelTimes[ChannelBlue].address(), (S32)mChannelTimes[ChannelBlue].size(),
		mMinX, mMaxX, stops, smMaxGradientStops);

	for (S32 i = 0; i < stopCount; i++)
	{
		mGradientStops.push_back(stops[i]);
	}
}

void GuiEditParticleColorGraph::renderUnderlay(const RectI &plotRect)
{
	if (mTargetAsset == NULL)
	{
		return;
	}

	refreshChannelCaches();

	// The live channel is not drawn here. The parent draws it next, brighter and
	// with the dots that say it is the one a click will reach.
	for (S32 c = 0; c < ChannelCount; c++)
	{
		if (c != (S32)mActiveChannel)
		{
			renderChannelCurve(plotRect, (Channel)c);
		}
	}
}

void GuiEditParticleColorGraph::renderChannelCurve(const RectI &plotRect, const Channel channel)
{
	const S32 count = (S32)mChannelTimes[channel].size();
	if (count == 0)
	{
		return;
	}

	const ColorI lineColor = getChannelColor(channel, false);

	Point2I p1 = convertToRenderPoint(plotRect, mChannelTimes[channel][0], mChannelValues[channel][0]);
	for (S32 i = 1; i < count; i++)
	{
		const Point2I p2 = convertToRenderPoint(plotRect, mChannelTimes[channel][i], mChannelValues[channel][i]);
		renderLine(plotRect, p1, p2, lineColor);
		p1 = p2;
	}

	// The flat run out to the right edge, the same one renderPoints draws for the
	// live channel. Without it a dim curve stops at its last key while the live one
	// carries on, and the two look like they disagree.
	const Point2I edge = Point2I(plotRect.point.x + plotRect.extent.x, p1.y);
	if (p1.x < edge.x)
	{
		renderLine(plotRect, p1, edge, lineColor);
	}
}

void GuiEditParticleColorGraph::renderUnderPlot(const RectI &bandRect)
{
	// Draws only. Everything it reads was snapshotted in renderUnderlay, because
	// by now the parent has cleared mDirty and an edit made this frame would not
	// show up until the next one.
	if (mTargetAsset == NULL || mGradientStops.size() < 2)
	{
		return;
	}

	const S32 stopCount = (S32)mGradientStops.size();

	S32 x1 = timeToPixel(mGradientStops[0], mMinX, mMaxX, bandRect.point.x, bandRect.extent.x);
	ColorF c1 = sampleColorAtTime(mGradientStops[0]);

	for (S32 i = 1; i < stopCount; i++)
	{
		const S32 x2 = timeToPixel(mGradientStops[i], mMinX, mMaxX, bandRect.point.x, bandRect.extent.x);
		ColorF c2 = sampleColorAtTime(mGradientStops[i]);

		if (x2 > x1)
		{
			RectI span = RectI(x1, bandRect.point.y, x2 - x1, bandRect.extent.y);

			// Corners are top left, top right, bottom right, bottom left, so a pure
			// left to right ramp repeats each edge color top and bottom. The named
			// locals are not tidiness: the signature takes non-const references and a
			// temporary will not bind to one.
			dglDrawBlendBox(span, c1, c2, c2, c1);
		}

		x1 = x2;
		c1 = c2;
	}
}
