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

#ifndef _GUI_EDIT_PARTICLE_COLOR_GRAPH_H_
#define _GUI_EDIT_PARTICLE_COLOR_GRAPH_H_

#ifndef _GUIPARTICLEGRAPHINSPECTOR_H_
#include "gui/editor/guiParticleGraphInspector.h"
#endif

//-----------------------------------------------------------------------------
// An emitter's three color channels on one graph, with a strip under the plot
// showing the color they mix to across the particle's life.
//
// They used to be three graphs behind three list selections, which is the wrong
// shape for the question anyone actually asks of them: not "what does red do"
// but "what color is this at half its life". Nobody can answer that from three
// separate pictures, which is what the strip is for.
//
// One channel is live at a time. The live channel IS the parent's target field,
// so every part of editing it -- the hit test, add, delete, drag, the refresh on
// release and therefore undo -- is the parent's and unchanged. This class adds
// two things and only two: the other two channels drawn dim and read-only, and
// the strip.
//
// Like its parent it repairs the fields it draws as a side effect of drawing: a
// key forced to time zero, out-of-order keys dropped. The parent does that for
// whichever channel is selected; this does it for all three at once, which makes
// it predictable rather than dependent on what the user happened to click. The
// gradient needs it: its whole correctness argument is that between two
// breakpoints every channel is linear, and renderLine will happily draw a
// segment backwards given keys that go back in time.
//
// Note that neither the curves nor the strip apply the field's RepeatTime or
// ValueScale, so on a field carrying either they both disagree with what the
// particle will do at runtime. They agree with each other, which is the property
// an editor needs -- the strip is a reference for the picture directly above it.
//
// Editor-only, and not offered to anyone building a Gui: the palette refuses
// every class whose name begins "GuiEdit", in both copies of that rule
// (GuiEditorControlIcons::isPlaceableClass, generated, and
// GuiEditorControlSpec::isPlaceableClass, hand-typed for its drift guard).
//-----------------------------------------------------------------------------

class GuiEditParticleColorGraph : public GuiParticleGraphInspector
{
private:
	typedef GuiParticleGraphInspector Parent;

public:
	enum Channel
	{
		ChannelRed = 0,
		ChannelGreen,
		ChannelBlue,
		ChannelCount
	};

	/// The most stops the strip will draw. A window holding more keys than this
	/// loses the detail in its tail and nothing else; the alternative is a heap
	/// allocation per frame in a render path.
	static constexpr S32 smMaxGradientStops = 128;

	/// Two stops closer together than this are one stop. Times run 0 to 1 and a
	/// plot is a few hundred pixels wide, so nothing below this could be told
	/// apart on screen anyway.
	static constexpr F32 smStopEpsilon = 1.0e-5f;

	static constexpr S32 smDefaultStripHeight = 16;
	static constexpr S32 smMinStripHeight = 6;
	static constexpr S32 smMaxStripHeight = 64;

	//-------------------------------------------------------------------------
	// Arithmetic. Everything these use is a parameter, so the tests can call them
	// without building a control -- which they could not do anyway, since a unit
	// test has no GL context and asking a profile for a font would load a texture
	// and trip a modal assert.
	//-------------------------------------------------------------------------

	/// A strip height the control will accept. Zero, meaning no strip at all, is
	/// a legal answer and the only one below the minimum.
	static S32 clampStripHeight(const S32 height);

	/// One channel's value at a time, read off the curve the graph DRAWS: linear
	/// between keys, flat before the first and flat after the last.
	///
	/// Deliberately not ParticleAssetField::getFieldValue, which applies a repeat
	/// time and a value scale the plotted curve does not, and which reads key
	/// zero before checking that there is one.
	static F32 sampleChannel(const F32* times, const F32* values, const S32 count, const F32 time);

	/// The times at which the mixed color can bend: every key of every channel
	/// inside the window, plus the window's two edges. Between two neighbours all
	/// three channels are linear, so one interpolated quad per span is exact --
	/// which is why this is not dglDrawBlendRangeBox, whose stops are evenly
	/// spaced and so could only approximate a key at 0.13.
	///
	/// Each channel's times must already ascend; addDataKey inserts in order and
	/// repairDataKeys guarantees the rest. The output strictly increases, which is
	/// what the draw loop depends on. Returns the count written: 0 for a window
	/// with no width, otherwise at least 2.
	static S32 buildGradientStops(const F32* redTimes, const S32 redCount,
		const F32* greenTimes, const S32 greenCount,
		const F32* blueTimes, const S32 blueCount,
		const F32 windowMin, const F32 windowMax,
		F32* outStops, const S32 maxStops);

	/// Where a time lands across a rect. Floored and clamped, so two touching
	/// spans share an edge exactly -- rounding a pair independently can overlap
	/// them by a pixel, and a pixel of overlap between two opaque quads is a
	/// visible seam. Flooring also matches convertToRenderPoint, so a bend in the
	/// strip sits directly under the bend in the curve.
	static S32 timeToPixel(const F32 time, const F32 windowMin, const F32 windowMax,
		const S32 rectLeft, const S32 rectWidth);

	/// The field name a channel is, in the spelling ParticleAssetEmitter
	/// registered it under. Never the caller's spelling: the field lookup folds
	/// case but setDisplayField interns case-sensitively, so "redchannel" would
	/// make a second entry that stops comparing equal to this one.
	static StringTableEntry getChannelFieldName(const Channel channel);

	/// A channel from a name, taking "Red" and "RedChannel" alike, or ChannelCount
	/// for anything that is neither.
	static Channel getChannelFromName(const char* name);

	/// What a channel draws in. Fixed hues rather than profile colors, because no
	/// theme can know which curve is the red one; lightened, because pure blue on
	/// a dark panel is unreadable. The inactive form is the same hue at a lower
	/// alpha -- dglDrawLine blends, so the grid shows through and the curve reads
	/// as sitting behind the live one.
	static ColorI getChannelColor(const Channel channel, const bool isActive);

	//-------------------------------------------------------------------------

	DECLARE_CONOBJECT(GuiEditParticleColorGraph);
	GuiEditParticleColorGraph();
	static void initPersistFields();

	/// Which channel is live: drawn bright, the only one with dots, and the only
	/// one a click can reach. Setting it goes through the parent's target field,
	/// which is what makes every editing path work with no code here.
	void setActiveChannel(const Channel channel);
	inline Channel getActiveChannel() const { return mActiveChannel; }

	/// The live channel and the field being edited are the same fact, so they are
	/// written in one place. Setting the field is how the emitter index gets set --
	/// the parent's two argument form -- and doing it here means an emitter change
	/// cannot leave the two disagreeing about which channel is live.
	virtual void setDisplayField(const char* fieldName);

	/// The mixed color at a time, alpha always opaque. For a readout, and for a
	/// test that has no way to look at what was drawn.
	ColorF getColorAtTime(const F32 time);

	/// The stops the strip will draw, as a space separated list of times.
	const char* getGradientStopList();

	void setStripHeight(const S32 height);
	inline S32 getStripHeight() const { return mStripHeight; }

	/// Refused. A color graph has no variation to shade, and getRenderPoints
	/// rewrites the graph it is asked of to a one pixel grid rect behind its back.
	virtual void setVariationGraphInspector(GuiParticleGraphInspector* object);

protected:
	virtual S32 getUnderPlotBandHeight() { return mStripHeight; }
	virtual void renderUnderlay(const RectI &plotRect);
	virtual void renderUnderPlot(const RectI &bandRect);
	virtual ColorI getCurveColor() { return getChannelColor(mActiveChannel, true); }

	/// Rebuild the snapshot the dim curves and the strip both read.
	///
	/// Takes no rect: nothing it builds is in pixels, which is what lets a console
	/// getter call it before the control has ever drawn. Called from renderUnderlay
	/// while the parent's mDirty is still set -- but the flag is only one of the
	/// reasons to rebuild, because renderPoints clears it halfway through the frame
	/// and the strip is drawn after that. Anything keyed on mDirty alone would show
	/// the previous frame's color on every frame the user was dragging.
	void refreshChannelCaches();

	void renderChannelCurve(const RectI &plotRect, const Channel channel);

	/// The mixed color at a time, read straight off the snapshot. Refreshes
	/// nothing, so it is safe to call for every stop of the strip.
	ColorF sampleColorAtTime(const F32 time);

	static bool setStripHeightField(void* obj, const char* data)
	{
		static_cast<GuiEditParticleColorGraph*>(obj)->setStripHeight(dAtoi(data));
		return false;
	}
	static const char* getStripHeightField(void* obj, const char* data)
	{
		return Con::getIntArg(static_cast<GuiEditParticleColorGraph*>(obj)->getStripHeight());
	}

private:
	Channel mActiveChannel;
	S32 mStripHeight;

	/// Each channel's keys, flat. Flat because buildGradientStops wants plain
	/// arrays and so does the test, and because the render points are two divides
	/// away and not worth a second cache.
	Vector<F32> mChannelTimes[ChannelCount];
	Vector<F32> mChannelValues[ChannelCount];

	/// The strip's stops, in time. Built with the curves, never in renderUnderPlot.
	Vector<F32> mGradientStops;

	/// What the snapshot was built from. mDirty alone is not enough; see
	/// refreshChannelCaches.
	bool mCacheValid;
	ParticleAsset* mCacheAsset;
	U32 mCacheEmitterIndex;
	F32 mCacheMinX;
	F32 mCacheMaxX;
};

#endif //_GUI_EDIT_PARTICLE_COLOR_GRAPH_H_
