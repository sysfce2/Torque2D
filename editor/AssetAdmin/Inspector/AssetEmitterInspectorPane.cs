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

//-----------------------------------------------------------------------------
// The inspector for one emitter of a particle asset. The dropdown in the title
// bar chooses which; index 0 is the asset itself and gets
// AssetParticleInspectorPane instead.
//
// This is the pane the whole exercise was for. An emitter registers about thirty
// persistent fields, and which of them are connected to anything depends on four
// or five of the others -- a single-particle emitter ignores ten, a POINT emitter
// ignores its size and its angle, a fixed-aspect emitter ignores every Size-Y
// curve. The generic inspector listed all thirty flat, so the one thing it could
// not tell you was which knobs were live.
//
// A header, then five blocks laid out by the same reflowing grid as every other
// pane:
//
//   Emission           where particles are born
//   Aim & Orientation  which way they are sent and which way they face
//   Particle Image     what one looks like
//   Particle           the switches that change what the other blocks mean
//   Render             how it is drawn and in what order
//
// THE BLOCKS ARE SIZED BEFORE THEY ARE THEMED. A grid row is as tall as its
// tallest cell, so blocks of unequal length do not make a short column and a long
// one -- they make columns of the SAME height with the short ones mostly empty,
// and the whole pane reads as badly laid out rather than as densely packed. Five
// rows each is therefore a constraint on the grouping, not an outcome of it, and
// the first cut (7 / 4 / 2 / 6 / 6) failed it badly.
//
// Two of the placements follow from that constraint and are worth defending on
// their own terms as well:
//
//   PivotPoint sits with Orientation, not with the particle switches, because it
//   is the point a particle is positioned and ROTATED about -- an orientation
//   concern wherever it is filed.
//
//   AlphaTest sits with Particle Image, not with Render, because it is a
//   threshold on the image's own alpha channel. It belongs beside the image whose
//   transparency it reads.
//
//   Aiming (IsTargeting / TargetPosition) moved out of Emission and in beside
//   Orientation, because both answer "which way", where Emission answers "where".
//
// The name and the "Emitter 1 of 2" line are the HEADER, above the grid and
// across the full width, rather than a sixth block -- a two-row Identity block
// beside a five-row one is the same failure in miniature. The same reasoning puts
// the image pane's warning line outside its grid.
//
// TWO WAYS OF SAYING "this does not apply", and the difference is deliberate.
//
//   SWAP   when the fields are alternatives -- an emitter draws an image OR an
//          animation, never both, and an orientation has exactly one offset. The
//          arm that is not in use is hidden outright, because showing an
//          Animation row beside an Image row invites you to fill in both and one
//          of them would silently win.
//
//   GREY   when the field is real, holds a value, and this mode simply does not
//          read it. Hiding those would lose the value from sight and make the
//          pane jump around as you tried modes; greying keeps it where it was
//          and puts the reason in the tooltip.
//
// The 32 graph fields (Quantity, SizeX, Speed, Spin, the colour channels, and
// each one's Variation and Life curves) are not here. Every one of them is a
// curve over time rather than a number, and the Emitter Graph tab beside this one
// is where a curve is drawn.
//
// Absent, each for a checkable reason:
//   PhysicsParticle, PhysicsParticleType   their addProtectedField calls are
//                                          commented out in ParticleAssetEmitter.cc,
//                                          so they are not fields at all -- the
//                                          members and the enum table exist, and
//                                          nothing reads them
//   hidden, locked                         SimObject bookkeeping, as everywhere
//-----------------------------------------------------------------------------

$AssetEmitterInspectorPane::cellWidth = 300;
$AssetEmitterInspectorPane::cellCount = 5;
$AssetEmitterInspectorPane::headerWidth = 300;

function AssetEmitterInspectorPane::onAdd(%this)
{
	// onAdd does not chain, so the shared setup runs from here.
	%this.init();
}

//-----------------------------------------------------------------------------
// Construction.
//-----------------------------------------------------------------------------

function AssetEmitterInspectorPane::buildPane(%this)
{
	%this.buildHeader();

	%grid = %this.makeCellGrid(0, $AssetEmitterInspectorPane::cellWidth,
		$AssetEmitterInspectorPane::cellCount);
	%this.add(%grid);
	%this.contentGrid = %grid;

	// Reading order across a wide screen: where they are born, which way they go,
	// what they look like, how they behave, how they are drawn.
	%this.buildEmissionCell(%grid);
	%this.buildOrientationCell(%grid);
	%this.buildImageCell(%grid);
	%this.buildBehaviorCell(%grid);
	%this.buildRenderCell(%grid);
}

// The name and the line placing this emitter within the effect, above the grid
// and outside it. The name box keeps a column's width rather than the pane's --
// a name is a short thing, and a text box a metre wide invites a sentence.
function AssetEmitterInspectorPane::buildHeader(%this)
{
	%chain = %this.makeChain(0, 4);
	%this.add(%chain);
	%this.headerChain = %chain;

	%row = %this.addFieldRow(%chain, "EmitterName", %this.labelFor("EmitterName"), "text", "");
	%row.HorizSizing = "right";
	%row.setExtent($AssetEmitterInspectorPane::headerWidth, %row.rowHeight);

	%this.infoLabel = %this.makeInfoLabel(%chain, "labelProfile");
	%this.infoLabel.textWrap = true;
	%this.infoLabel.textExtend = true;
	%this.infoLabel.vAlign = "top";
}

// addFieldRow takes the label and the kind as arguments rather than asking the
// tables for them, so every call here would otherwise repeat the same lookups.
function AssetEmitterInspectorPane::addField(%this, %container, %field)
{
	return %this.addFieldRow(%container, %field, %this.labelFor(%field),
		%this.kindFor(%field), %this.enumItemsFor(%field));
}

function AssetEmitterInspectorPane::buildEmissionCell(%this, %grid)
{
	%chain = %this.makeCell(%grid, 4);
	%this.emissionChain = %chain;

	// Type first: it decides whether the two rows under it are read.
	%this.addField(%chain, "EmitterType");
	%this.addField(%chain, "EmitterSize");
	%this.addField(%chain, "EmitterAngle");
	%this.addField(%chain, "EmitterOffset");
	%this.addField(%chain, "LinkEmissionRotation");
}

function AssetEmitterInspectorPane::buildImageCell(%this, %grid)
{
	%chain = %this.makeCell(%grid, 4);
	%this.imageChain = %chain;

	// Not a field. There is no StaticMode on an emitter: the mode is a side
	// effect of whichever of Image and Animation was written last, so the picker
	// exists to make that choice sayable rather than accidental. It is built
	// WITHOUT registering, so the refresh loop never tries to read a field of this
	// name off the emitter -- refreshExtras sets it instead, and writeField below
	// turns a change of it into the write that actually moves the mode.
	%this.sourceRow = %this.makeFieldRow(%chain, "Source", "Drawn With", "dropdown", "");
	%this.sourceRow.fillItems("Static Image" TAB "Animation");

	%this.addField(%chain, "Image");
	%this.addField(%chain, "RandomImageFrame");
	%this.addField(%chain, "Frame");
	%this.addField(%chain, "NamedFrame");
	%this.addField(%chain, "Animation");

	// A threshold on this image's own alpha channel, so it belongs beside the
	// image rather than in Render with the blend rows.
	%this.addField(%chain, "AlphaTest");
}

// Aim and orientation: which way particles are sent, and which way they face
// once they are out. Only one of the three orientation arms is ever on show, so
// the block is five rows in the default state and six for the two arms that
// carry a second control.
function AssetEmitterInspectorPane::buildOrientationCell(%this, %grid)
{
	%chain = %this.makeCell(%grid, 4);
	%this.orientationChain = %chain;

	%this.addField(%chain, "IsTargeting");
	%this.addField(%chain, "TargetPosition");

	%this.addField(%chain, "OrientationType");
	%this.addField(%chain, "FixedAngleOffset");
	%this.addField(%chain, "AlignedAngleOffset");
	%this.addField(%chain, "KeepAligned");
	%this.addField(%chain, "RandomAngleOffset");
	%this.addField(%chain, "RandomArc");

	%this.addField(%chain, "PivotPoint");
}

function AssetEmitterInspectorPane::buildBehaviorCell(%this, %grid)
{
	%chain = %this.makeCell(%grid, 4);
	%this.behaviorChain = %chain;

	%this.addField(%chain, "SingleParticle");
	%this.addField(%chain, "FixedAspect");
	%this.addField(%chain, "FixedForceAngle");
	%this.addField(%chain, "AttachPositionToEmitter");
	%this.addField(%chain, "AttachRotationToEmitter");
}

function AssetEmitterInspectorPane::buildRenderCell(%this, %grid)
{
	%chain = %this.makeCell(%grid, 4);
	%this.renderChain = %chain;

	%this.addField(%chain, "OldestInFront");
	%this.addField(%chain, "IntenseParticles");
	%this.addField(%chain, "BlendMode");
	%this.addField(%chain, "SrcBlendFactor");
	%this.addField(%chain, "DstBlendFactor");
}

//-----------------------------------------------------------------------------
// The field tables.
//-----------------------------------------------------------------------------

function AssetEmitterInspectorPane::labelFor(%this, %field)
{
	switch$(%field)
	{
		case "EmitterName": return "Emitter Name";

		case "EmitterType": return "Shape";
		case "EmitterSize": return "Shape Size";
		case "EmitterAngle": return "Shape Angle";
		case "EmitterOffset": return "Offset";
		case "LinkEmissionRotation": return "Follow Player Rotation";
		case "IsTargeting": return "Aim At A Point";
		case "TargetPosition": return "Target Position";

		case "Image": return "Image";
		case "RandomImageFrame": return "Random Frame";
		case "Frame": return "Frame";
		case "NamedFrame": return "Named Frame";
		case "Animation": return "Animation";

		case "OrientationType": return "Orientation";
		case "FixedAngleOffset": return "Angle";
		case "AlignedAngleOffset": return "Angle From Travel";
		case "KeepAligned": return "Keep Aligned";
		case "RandomAngleOffset": return "Centre Angle";
		case "RandomArc": return "Arc";

		case "SingleParticle": return "Single Particle";
		case "FixedAspect": return "Fixed Aspect";
		case "FixedForceAngle": return "Fixed Force Angle";
		case "AttachPositionToEmitter": return "Attach Position";
		case "AttachRotationToEmitter": return "Attach Rotation";
		case "PivotPoint": return "Pivot Point";

		case "OldestInFront": return "Oldest In Front";
		case "IntenseParticles": return "Intense (Additive)";
		case "BlendMode": return "Blending";
		case "SrcBlendFactor": return "Source Factor";
		case "DstBlendFactor": return "Destination Factor";
		case "AlphaTest": return "Alpha Test";
	}

	return %field;
}

function AssetEmitterInspectorPane::kindFor(%this, %field)
{
	switch$(%field)
	{
		case "EmitterType" or "OrientationType" or "SrcBlendFactor" or "DstBlendFactor":
			return "enum";

		case "EmitterSize" or "EmitterOffset" or "TargetPosition" or "PivotPoint":
			return "pointf";

		case "EmitterAngle" or "FixedForceAngle" or "FixedAngleOffset" or "AlignedAngleOffset" or
			"RandomAngleOffset" or "RandomArc" or "AlphaTest":
			return "decimal";

		case "Frame":
			return "number";

		case "Image" or "Animation":
			return "asset";

		case "LinkEmissionRotation" or "IsTargeting" or "RandomImageFrame" or "KeepAligned" or
			"SingleParticle" or "FixedAspect" or "AttachPositionToEmitter" or
			"AttachRotationToEmitter" or "OldestInFront" or "IntenseParticles" or "BlendMode":
			return "bool";
	}

	return "text";
}

function AssetEmitterInspectorPane::enumItemsFor(%this, %field)
{
	// The engine's own labels, from the enum tables the fields are registered
	// with. The drop-down's search ignores case, so the readable ones can be
	// written as words; the GL blend factors are left exactly as they are,
	// because they are names rather than English and an editor that renamed them
	// would not match anything written about them.
	switch$(%field)
	{
		// EmitterTypeTable, ParticleAssetEmitter.cc
		case "EmitterType": return "Point" TAB "Line" TAB "Box" TAB "Disk" TAB "Ellipse" TAB "Torus";

		// OrientationTypeTable, ParticleAssetEmitter.cc
		case "OrientationType": return "Fixed" TAB "Aligned" TAB "Random";

		// srcBlendFactorTable, SceneObject.cc
		case "SrcBlendFactor": return "ZERO" TAB "ONE" TAB "DST_COLOR" TAB "ONE_MINUS_DST_COLOR" TAB
			"SRC_ALPHA" TAB "ONE_MINUS_SRC_ALPHA" TAB "DST_ALPHA" TAB "ONE_MINUS_DST_ALPHA" TAB
			"SRC_ALPHA_SATURATE";

		// dstBlendFactorTable, SceneObject.cc
		case "DstBlendFactor": return "ZERO" TAB "ONE" TAB "SRC_COLOR" TAB "ONE_MINUS_SRC_COLOR" TAB
			"SRC_ALPHA" TAB "ONE_MINUS_SRC_ALPHA" TAB "DST_ALPHA" TAB "ONE_MINUS_DST_ALPHA";
	}

	return "";
}

// The emitter's two asset rows want different pickers, which is the whole reason
// EditorFieldRow learned to be told.
function AssetEmitterInspectorPane::assetTypeFor(%this, %field)
{
	switch$(%field)
	{
		case "Image": return "ImageAsset";
		case "Animation": return "AnimationAsset";
	}

	return "";
}

// None of these fields carries a doc string in the engine, so the pane is where
// the explanation lives. The ones whose names already say it are left out.
function AssetEmitterInspectorPane::tipFor(%this, %field)
{
	switch$(%field)
	{
		case "Source": return "Whether each particle draws a still frame of an image asset or plays an " @
			"animation. An emitter is one or the other -- choosing here clears the other one.";

		case "EmitterType": return "The shape particles are born on. Point emits from a single spot. Line " @
			"uses the X size as its half-length. Box and Disk fill their area; Ellipse and Torus emit on " @
			"the edge, Torus between an outer and an inner radius.";

		case "EmitterSize": return "The shape's dimensions, in world units.";

		case "EmitterAngle": return "How far the shape itself is turned, in degrees. This rotates where " @
			"particles are born, not which way they travel.";

		case "EmitterOffset": return "Where the shape sits relative to the player's position.";

		case "LinkEmissionRotation": return "Add the player's own rotation to the emission angle, so " @
			"turning the player turns the spray with it.";

		case "IsTargeting": return "Aim particles at a fixed point instead of using the Emission Angle " @
			"graph. The Emission Arc graph still spreads them around that aim.";

		case "TargetPosition": return "The point particles are aimed at while Aim At A Point is on.";

		case "Image": return "The image asset each particle draws a frame of.";

		case "RandomImageFrame": return "Give every particle a random frame of the image instead of the " @
			"one chosen below. A sheet of different sparks or leaves is what this is for.";

		case "Frame": return "Which frame of the image each particle draws, counting from zero.";

		case "NamedFrame": return "Which named cell of the image each particle draws. Only an image with " @
			"explicit named cells has these.";

		case "Animation": return "The animation asset each particle plays. Every particle starts its own " @
			"copy from the beginning.";

		case "OrientationType": return "Which way a particle faces. Fixed points them all one way. Aligned " @
			"points them along their direction of travel. Random gives each one its own angle.";

		case "FixedAngleOffset": return "The angle every particle faces, in degrees.";

		case "AlignedAngleOffset": return "Added to the direction of travel, in degrees -- so art drawn " @
			"pointing up rather than right can be corrected here.";

		case "KeepAligned": return "Keep turning particles as they change direction, instead of aiming " @
			"them once when they are born.";

		case "RandomAngleOffset": return "The middle of the range random angles are drawn from, in degrees.";

		case "RandomArc": return "How wide that range is, in degrees. 360 is a completely free angle.";

		case "SingleParticle": return "Emit one immortal particle at the offset instead of a stream. It " @
			"never moves and never expires, so the whole Emission block, and the Lifetime and Quantity " @
			"graphs, stop being read.";

		case "FixedAspect": return "Keep particles square: the Size-Y graphs are ignored and height " @
			"follows width.";

		case "FixedForceAngle": return "Which way the Fixed Force graph pushes, in degrees. 90 is up, " @
			"which is what a rising flame or smoke wants.";

		case "AttachPositionToEmitter": return "Carry particles with the player as it moves, instead of " @
			"leaving them behind in the world where they were born.";

		case "AttachRotationToEmitter": return "Turn those carried particles with the player as well. " @
			"Only read while Attach Position is on.";

		case "PivotPoint": return "The point within a particle that it is positioned and rotated about, " @
			"as a fraction of its size from the centre.";

		case "OldestInFront": return "Draw the oldest particles on top of the newest, rather than the " @
			"other way round.";

		case "IntenseParticles": return "Force additive blending, which makes overlapping particles glow. " @
			"Overrides the three blending rows below.";

		case "BlendMode": return "Blend particles with what is behind them. Turning this off draws them " @
			"opaque, edges and all.";

		case "SrcBlendFactor": return "How much of the particle's own colour goes into the blend. With " @
			"Destination Factor, these are the two halves of the OpenGL blend equation.";

		case "DstBlendFactor": return "How much of what is already on screen survives the blend. " @
			"ONE_MINUS_SRC_ALPHA is ordinary transparency; ONE is additive.";

		case "AlphaTest": return "Discard any pixel less opaque than this, before blending. Below zero " @
			"turns the test off, which is the default.";
	}

	return "";
}

//-----------------------------------------------------------------------------
// Reading and writing.
//-----------------------------------------------------------------------------

// The mode is not a persistent field -- it is a side effect of which of setImage
// and setAnimation ran last. Ask the engine rather than inferring it from which
// asset is set: an emitter switched to animation before an animation has been
// chosen is in animated mode holding nothing, and "no animation asset" would
// read that as static. isStaticMode exists for this.
function AssetEmitterInspectorPane::isAnimated(%this)
{
	return isObject(%this.target) && !%this.target.isStaticMode();
}

function AssetEmitterInspectorPane::writeField(%this, %field, %value)
{
	switch$(%field)
	{
		// The picker's own row. Writing whichever field defines the wanted mode IS
		// the mode switch: both setters set the mode and clear the other asset.
		// Writing the arm's current value rather than an empty one keeps a choice
		// that was made earlier and then switched away from.
		case "Source":
			if(%value $= "Animation")
			{
				%this.target.setFieldValue("Animation", %this.target.getAnimation());
			}
			else
			{
				%this.target.setFieldValue("Image", %this.target.getImage());
			}
			return;

		// The one emitter setter with no refreshAsset of its own, and deliberately
		// so -- a target position is aimed at something that moves, and AngleToy
		// writes it on every mouse move. See the comment on
		// ParticleAssetEmitter::setTargetPosition. An editor writing it once has to
		// ask for the refresh itself, or nothing is marked dirty and the preview
		// does not move.
		case "TargetPosition":
			%this.target.setFieldValue(%field, %value);
			%this.target.refreshAsset();
			return;
	}

	%this.target.setFieldValue(%field, %value);
}

// The refresh chain announces the ASSET. This pane's target is one of its
// emitters, so the base class's "is this mine?" test has to be asked one step up
// -- otherwise every emitter edit would bounce off and the pane would show
// whatever the engine had clamped the value to only after the next selection.
function AssetEmitterInspectorPane::onAssetRefreshed(%this, %asset)
{
	if(%this.committing || !isObject(%this.target))
	{
		return;
	}
	if(%this.target.getOwner() != %asset)
	{
		return;
	}

	%this.refresh();
}

//-----------------------------------------------------------------------------
// Loading. Everything the row loop does not reach.
//-----------------------------------------------------------------------------

function AssetEmitterInspectorPane::refreshExtras(%this)
{
	%this.sourceRow.setValue(%this.isAnimated() ? "Animation" : "Static Image");

	%this.infoLabel.setText(%this.describeEmitter(%this.target));
	%this.applyGating();
}

// Where this emitter sits in the effect and what it draws. The dropdown above
// says which one is selected but not how many there are, and render order is the
// reason the order matters.
function AssetEmitterInspectorPane::describeEmitter(%this, %emitter)
{
	%asset = %emitter.getOwner();
	if(!isObject(%asset))
	{
		return "";
	}

	%count = %asset.getEmitterCount();
	%index = -1;
	for(%i = 0; %i < %count; %i++)
	{
		if(%asset.getEmitter(%i) == %emitter)
		{
			%index = %i;
			break;
		}
	}

	%line = "Emitter" SPC (%index + 1) SPC "of" SPC %count;

	// Drawn in list order, so the last one is on top of the rest.
	if(%count > 1)
	{
		if(%index == %count - 1)
		{
			%line = %line @ ", drawn last (on top)";
		}
		else if(%index == 0)
		{
			%line = %line @ ", drawn first (behind)";
		}
	}

	return %line @ ".";
}

//-----------------------------------------------------------------------------
// The gating. One method, run on every refresh, because every rule here reads a
// field the row loop has just reloaded.
//-----------------------------------------------------------------------------

function AssetEmitterInspectorPane::applyGating(%this)
{
	if(!isObject(%this.target))
	{
		return;
	}

	%this.gateSource();
	%this.gateEmission();
	%this.gateOrientation();
	%this.gateBehavior();
	%this.gateRender();

	// Swapping arms changes how tall the blocks are, and a chain lays out only
	// the children it can see -- nothing re-lays it out on setVisible.
	%this.forceLayout();
}

// SWAP. An emitter draws one or the other, never both.
function AssetEmitterInspectorPane::gateSource(%this)
{
	%animated = %this.isAnimated();

	%this.row["Animation"].setVisible(%animated);

	%this.row["Image"].setVisible(!%animated);
	%this.row["RandomImageFrame"].setVisible(!%animated);

	// SWAP again, one level down: an image is addressed by number or by name, and
	// which one is in play is decided by whichever was written last. Only an image
	// with explicit named cells can be addressed by name at all.
	%named = !%animated && %this.target.isUsingNamedImageFrame();
	%this.row["Frame"].setVisible(!%animated && !%named);
	%this.row["NamedFrame"].setVisible(%named);

	// GREY. A random frame is still a frame of the same image, so the row below
	// keeps its value -- it is simply not the one used.
	if(!%animated)
	{
		%this.setRowsEnabled("Frame NamedFrame", !%this.target.getRandomImageFrame(),
			"Random Frame is on, so each particle picks its own and this one is not used.");
	}
}

function AssetEmitterInspectorPane::gateEmission(%this)
{
	// GREY, the widest rule on the pane. A single particle sits at the offset and
	// never moves, so nothing about the shape or the direction is read.
	if(%this.target.getSingleParticle())
	{
		%this.setRowsEnabled("EmitterType EmitterSize EmitterAngle LinkEmissionRotation IsTargeting TargetPosition",
			false, "Single Particle is on: one particle sits at the offset and never moves, so nothing " @
			"about the emitter's shape or direction is read.");
		return;
	}

	%this.setRowsEnabled("EmitterType LinkEmissionRotation IsTargeting", true, "");

	// GREY. A point has no size and no angle; a torus has a size but its branch in
	// ParticlePlayer never applies the rotation.
	%type = %this.target.getEmitterType();

	%this.setRowsEnabled("EmitterSize", %type !$= "POINT",
		"A Point emitter emits from one spot, so it has no size. Choose another shape to use this.");

	%this.setRowsEnabled("EmitterAngle", %type !$= "POINT" && %type !$= "TORUS",
		(%type $= "POINT") ?
			"A Point emitter emits from one spot, so turning it changes nothing." :
			"A Torus is the same shape whichever way it is turned, and the engine does not apply this to it.");

	// GREY. Aiming replaces the Emission Angle graph rather than this row, so what
	// goes inert here is the target when aiming is off.
	%this.setRowsEnabled("TargetPosition", %this.target.getIsTargeting(),
		"Only read while Aim At A Point is on.");
}

// SWAP. Each orientation has exactly one set of controls; the other two would be
// three more rows that do nothing.
function AssetEmitterInspectorPane::gateOrientation(%this)
{
	%type = %this.target.getOrientationType();

	%this.row["FixedAngleOffset"].setVisible(%type $= "FIXED");

	%this.row["AlignedAngleOffset"].setVisible(%type $= "ALIGNED");
	%this.row["KeepAligned"].setVisible(%type $= "ALIGNED");

	%this.row["RandomAngleOffset"].setVisible(%type $= "RANDOM");
	%this.row["RandomArc"].setVisible(%type $= "RANDOM");
}

function AssetEmitterInspectorPane::gateBehavior(%this)
{
	// GREY. Attaching rotation is read only from inside the position-attach test,
	// so on its own it does nothing at all.
	%this.setRowsEnabled("AttachRotationToEmitter", %this.target.getAttachPositionToEmitter(),
		"Only read while Attach Position is on -- particles have to be carried with the player before " @
		"they can be turned with it.");
}

function AssetEmitterInspectorPane::gateRender(%this)
{
	// GREY. Intense particles force additive blending before the blend rows are
	// consulted at all.
	if(%this.target.getIntenseParticles())
	{
		%this.setRowsEnabled("BlendMode SrcBlendFactor DstBlendFactor", false,
			"Intense (Additive) is on, which forces additive blending and overrides these.");
		return;
	}

	%this.setRowsEnabled("BlendMode", true, "");

	// GREY. With blending off there is nothing for the two factors to weigh.
	%this.setRowsEnabled("SrcBlendFactor DstBlendFactor", %this.target.getBlendMode(),
		"Blending is off, so these are not used.");
}

// The commit rebuilt the preview player's emitter nodes, and it may have renamed
// this emitter -- which the dropdown in the title bar is showing.
function AssetEmitterInspectorPane::afterCommit(%this)
{
	if(isObject(AssetAdmin.inspector))
	{
		AssetAdmin.inspector.refreshEmitterLabels();
	}

	if(isObject(AssetAdmin.particleTransportBar))
	{
		AssetAdmin.particleTransportBar.refresh();
	}
}
