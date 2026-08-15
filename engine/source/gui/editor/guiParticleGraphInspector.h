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

#ifndef _GUIPARTICLEGRAPHINSPECTOR_H_
#define _GUIPARTICLEGRAPHINSPECTOR_H_

#ifndef _GUICONTROL_H_
#include "gui/guiControl.h"
#endif

// Named here rather than included: this header only holds pointers to them, and
// the header that defines them pulls in most of the 2d layer. Anyone who needs to
// call through one of these pointers includes ParticleAsset.h themselves.
class ParticleAsset;
class ParticleAssetField;

//-----------------------------------------------------------------------------
// One particle field's data keys drawn as an editable curve.
//
// Rendering repairs the data it draws: the first key is forced to time zero and
// any key that does not advance the time is deleted. That has always been true
// of this control; it is now in repairDataKeys where it can be said out loud and
// called from more than one place.
//
// The class is a template method. onRender owns the box model, the axis labels,
// the grid and the editable curve; a subclass adds to that picture through
// getUnderPlotBandHeight/renderUnderlay/renderUnderPlot/getCurveColor rather than
// by reimplementing any of it. The base asks for no band, so the base's layout is
// exactly what it always was.
//-----------------------------------------------------------------------------

class GuiParticleGraphInspector : public GuiControl
{
private:
   typedef GuiControl Parent;

protected:
   StringTableEntry mTargetField;
   ParticleAsset* mTargetAsset;
   U32 mEmitterIndex;

   F32 mMinX, mMinY, mMaxX, mMaxY; //Display settings
   StringTableEntry mLabelX, mLabelY;
   StringTableEntry mMaxYLabel, mMinYLabel, mMaxXLabel, mMinXLabel;

   RectI mGridRect;
   Point2I mCalculationOffset;

   GuiParticleGraphInspector* mVariationInspector;

public:
	/// What a point draws at, and what a click has to land inside to hit one.
	///
	/// static constexpr rather than a const member: a test binds it by const
	/// reference, which would odr-use a static const and fail to link, and a
	/// subclass's layout wants it without an instance in hand.
	static constexpr F32 smPointRadius = 7.0f;

	/// The gap left above and below a band reserved under the plot.
	static constexpr S32 smUnderPlotGap = 3;


	struct GraphPoint
	{
		GraphPoint() {}
		GraphPoint(Point2I p, F32 time, F32 value, U32 index) { mTime = time; mValue = value; mPoint = p; mIndex = index; }

		F32 mTime;
		F32 mValue;
		Point2I mPoint;
		U32 mIndex;
	};
	S32 mSelectedIndex;
	bool mDirty;
	Vector<GraphPoint> mPointList;

   //creation methods
   DECLARE_CONOBJECT(GuiParticleGraphInspector);
   GuiParticleGraphInspector();
   static void initPersistFields();

	/// What a band under the plot costs altogether: itself plus the gap above and
	/// below it, or nothing at all when none was asked for.
	///
	/// One function because the space reserved, the rect drawn into and the label
	/// row pushed down by it are three formulas that have to agree, and three
	/// chances to be a pixel out is two too many.
	static S32 getUnderPlotReserve(const S32 bandHeight);

	/// An axis end label, printed from the value rather than kept as whatever text
	/// the caller built it out of.
	///
	/// Script hands these over as strings, and a script float is an F32 printed
	/// with "%.9g" -- so a tenth arrives as "0.100000001". The y labels are the
	/// whole reason the plot gives up a left margin, so eleven characters of
	/// rounding error were costing a zoomed-in graph a third of its width. Six
	/// significant digits is more than an axis end can usefully show, and it drops
	/// the trailing zeros with them.
	///
	/// Only the label is reprinted; the window itself keeps the value it parsed, so
	/// nothing the camera computes has to agree with what is drawn to the pixel.
	static const char* formatAxisLabel(const F32 value, char* buffer, const U32 bufferSize);

	/// Shrink a rect to a whole number of grid cells and re-center what is left.
	/// The graph draws ten by ten, so a plot whose width is not a multiple of ten
	/// has grid lines landing between pixels.
	///
	/// A rect measured mid-resize can have a negative extent. That used to be an
	/// unsigned modulus, which made the correction about four billion.
	static RectI snapRectToGrid(const RectI &rect, const S32 divisor);

   virtual void inspectObject(ParticleAsset* object);
   virtual void setDisplayField(const char* fieldName);
   virtual void setDisplayField(const char* fieldName, U16 index);
   inline StringTableEntry getDisplayField() const { return mTargetField; }
   inline U32 getEmitterIndex() const { return mEmitterIndex; }
   virtual void setDisplayArea(StringTableEntry minX, StringTableEntry minY, StringTableEntry maxX, StringTableEntry maxY);
   virtual void setDisplayLabels(const char* labelX, const char* labelY);
   virtual void setVariationGraphInspector(GuiParticleGraphInspector* object) { mVariationInspector = object; }

   virtual void resize(const Point2I &newPosition, const Point2I &newExtent);
   virtual void setControlProfile(GuiControlProfile *prof);

   virtual void onTouchUp(const GuiEvent &event);
   virtual void onTouchDown(const GuiEvent &event);
   virtual void onTouchDragged(const GuiEvent &event);

   void onRender(Point2I offset, const RectI &updateRect);
   Vector<GraphPoint>* getRenderPoints();

protected:
	S32 findHitGraphPoint(const Point2I &point);
	F32 getGraphValue(const F32 y);
	F32 getGraphTime(const F32 x);

	/// How tall a band under the plot this control wants, between the curve and
	/// the x axis labels. Zero in the base, which is what keeps the base's layout
	/// identical to what it has always drawn.
	virtual S32 getUnderPlotBandHeight() { return 0; }

	/// Drawn after the grid and before the editable curve: whatever belongs behind
	/// it. mDirty is still set when this is called on a recalculating frame, which
	/// is what lets a subclass rebuild its own caches in step with the one
	/// renderPoints is about to build.
	virtual void renderUnderlay(const RectI &plotRect) { }

	/// Drawn into the band. Not called when no band was asked for.
	virtual void renderUnderPlot(const RectI &bandRect) { }

	/// The color the curve and its dots draw in.
	virtual ColorI getCurveColor() { return mProfile->getFillColor(SelectedState); }

	/// The plot area inside a content rect: the label rows and any band a subclass
	/// reserved taken off, then snapped to the grid. Measures text, so it cannot
	/// be tested -- everything in it that is arithmetic lives in the two statics.
	RectI calculatePlotRect(const RectI &contentRect);

	/// The reserved band, placed against the SNAPPED plot rect rather than measured
	/// from the content rect. The snap moves the plot by up to nine pixels and a
	/// band measured independently would drift by exactly that much. An empty rect
	/// when no band was asked for.
	RectI getUnderPlotRect(const RectI &plotRect);

	/// The y the x axis label row starts at. A band sits between it and the plot,
	/// so this is the only place that knows the order of the two.
	S32 getXLabelTop(const RectI &plotRect);

	void calculatePoints(const RectI &contentRect);
	Point2I convertToRenderPoint(const RectI& contentRect, F32 time, F32 value);
	void renderLabels(const RectI &contentRect, const ColorI &labelColor);
	void renderGrid(const RectI &contentRect, const ColorI &gridColor);
	void renderPoints(const RectI &contentRect, const ColorI &lineColor);
	void renderVariation(const RectI &contentRect, const ColorI& color);
	void renderDot(const RectI &contentRect, const Point2I &point, const Point2I &cursorPt, bool isSelected);
	void renderLine(const RectI &contentRect, const Point2I &point1, const Point2I &point2, const ColorI &lineColor);
	void renderQuad(const RectI& contentRect, const Point2I& point1, const Point2I& point2, const Point2I& point3, const Point2I& point4, const ColorI& quadColor);

	/// The field a name refers to, or NULL. The asset's own collection first and
	/// then the addressed emitter's, because a name is usually an emitter's but the
	/// asset's are the ones that always exist.
	///
	/// Never asserts and never dereferences a missing emitter. An asset with no
	/// emitters is an ordinary state for a half-built particle, and it used to take
	/// this through getEmitterCount() - 1 on an unsigned count of zero.
	ParticleAssetField* findField(StringTableEntry fieldName);

	/// The field being edited, or NULL.
	ParticleAssetField* getTargetField();

	/// Force a field's keys into the shape everything downstream assumes: a key at
	/// time zero, and strictly increasing times after it. Idempotent, so a second
	/// caller on the same frame walks the list and changes nothing. Returns true
	/// when it changed something.
	///
	/// This edits the asset as a side effect of drawing, which is what this control
	/// has always done.
	bool repairDataKeys(ParticleAssetField* field);
};

#endif
