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

#ifndef _H_GUIDEFAULTCONTROLRENDER_
#define _H_GUIDEFAULTCONTROLRENDER_

#ifndef _GUITYPES_H_
#include "gui/guiTypes.h"
#endif

class GuiControlProfile;

void renderUniversalRect(RectI &bounds, GuiControlProfile *profile, GuiControlState state, const ColorI &fillColor = "White", const bool bUseFillColor = false);
void renderBorderedRect(RectI &bounds, GuiControlProfile *profile, GuiControlState state);
void renderBorderedRect(RectI &bounds, GuiControlProfile *profile, GuiControlState state, const ColorI &fillColor);
void renderBorderedCircle(Point2I &center, S32 radius, GuiControlProfile *profile, GuiControlState state);
void renderBorderedRing(Point2I& center, S32 outerRadius, S32 innerRadius, GuiControlProfile* profile, GuiControlState state);
void renderRing(const Point2I& center, const F32 radius, const ColorI& color, F32 borderSize);
void renderSizableBorderedImageAsset(RectI &bounds, U8 frame, ImageAsset *mImageAsset, S32 frameCount);
void renderSizableBorderedBitmap(RectI &bounds, U8 frame, TextureHandle &texture, RectI *bitmapBounds, S32 frameCount);
void renderSizableBorderedTexture(RectI &bounds, TextureHandle &texture, RectI &TopLeft, RectI &Top, RectI &TopRight, RectI &Left, RectI &Fill, RectI &Right, RectI &BottomLeft, RectI &Bottom, RectI &BottomRight);
void renderFixedBitmapBordersFilled(RectI &bounds, S32 baseMultiplier, GuiControlProfile *profile);
void renderStretchedBitmap(RectI &bounds, U8 frame, GuiControlProfile *profile);
void renderStretchedImageAsset(RectI &bounds, U8 frame, GuiControlProfile *profile);

/// One frame of a sheet, stretched to fill bounds, drawn with whatever bitmap
/// modulation is current.
///
/// The plain counterpart to renderStretchedImageAsset just above, and the
/// differences are the whole reason it exists. That one reads the sheet off a
/// PROFILE, so it can only ever draw the sheet a control is wearing; this one is
/// handed the asset, so a control can draw frames of something it merely holds.
/// That one's first act is dglClearBitmapModulation, which throws away a tint
/// already set for a row's state; this one leaves the modulation alone, so a
/// white mask inherits whatever ink the caller established. And that one takes
/// the frame as a U8, which quietly cannot reach past frame 255 of a sheet that
/// may have a thousand.
void renderImageAssetFrame(const RectI &bounds, ImageAsset *imageAsset, U32 frame);
void renderColorBullet(RectI &bounds, ColorI &color, S32 maxSize, bool useCircle = false);
void renderTriangleIcon(RectI &bounds, ColorI &color, GuiDirection pointsToward, S32 maxSize);

#endif
