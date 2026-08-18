// Asset Manager particle-inspector smoke test. Drives the two custom panes that
// replaced the generic GuiInspector for particle assets: the small effect pane at
// dropdown index 0, the six-block emitter pane above it, and the gating that
// decides which of an emitter's thirty fields are live.
// Run: tests/run.ps1 assetParticleInspector  ; grep APRT in tests/logs/.
//
// Driven by calling the panes rather than by posting input, for the same reason
// assetImageInspector is: where a row sits depends on how many columns the grid
// chose and how far the scroller has been dragged, neither of which script can
// read.
//
// NOTE: a COPY of toybox/ToyAssets. This one really does edit -- SingleParticle,
// EmitterType and EmitterName are all written -- so it must not touch the real
// content tree.

setLogMode(1);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

function aprtCheck(%label, %cond)
{
	if(%cond) echo("APRT PASS: " @ %label);
	else      echo("APRT FAIL: " @ %label);
}

// bonfire has exactly two emitters and they differ in the two ways the emitter
// pane cares most about:
//
//   "smoke"   a LINE emitter drawing a still frame of an ImageAsset
//   "flames"  a LINE emitter playing an AnimationAsset, with IntenseParticles on
//
// so the source swap and the blend greying both have a real case to read, and
// neither had to be set up by the test.
$aprtAssetId = "ToyAssets:bonfire";

function aprtLoadFixtureAssets()
{
	%copy = testRoot("assetParticleInspectorSmokeProject/ToyAssets");

	if(!pathCopy(testRoot("toybox/ToyAssets"), %copy, false))
	{
		return false;
	}

	ModuleDatabase.scanModules(%copy);
	%module = ModuleDatabase.findModule("ToyAssets", 1);
	if(!isObject(%module))
	{
		return false;
	}
	AssetDatabase.addModuleDeclaredAssets(%module);
	return true;
}

// How many rows of a block are actually on show. The swaps hide rows rather than
// removing them, so getCount() is not the answer.
function aprtVisibleRows(%chain)
{
	%shown = 0;
	for(%i = 0; %i < %chain.getCount(); %i++)
	{
		if(%chain.getObject(%i).isVisible())
		{
			%shown++;
		}
	}
	return %shown;
}

// Choosing an entry in the title dropdown, the way the dialog and the button bar
// do it: move the selection, then tell the inspector.
function aprtSelect(%index)
{
	$aprtInspector.titleDropDown.setSelected(%index);
	$aprtInspector.onChooseParticleAsset($aprtAsset);
}

testExec("editor/main.cs");
schedule(2000, 0, "aprtStep1");

//-----------------------------------------------------------------------------

function aprtStep1()
{
	createPath(testRoot("shots/"));

	// Spelled out rather than held in a variable: tests/run.ps1 finds the folder
	// to delete by reading this file for setProjectFolder("...").
	ProjectManager.setProjectFolder("assetParticleInspectorSmokeProject");
	EditorPreferences.path = testRoot("shots/assetParticleInspectorSmokePrefs.taml");

	aprtCheck("fixture asset module registered", aprtLoadFixtureAssets());

	EditorCore.tabBook.selectPage(2);

	schedule(600, 0, "aprtStep2");
}

//-----------------------------------------------------------------------------
// Both panes exist, and stand down until a particle asset is chosen.
//-----------------------------------------------------------------------------

function aprtStep2()
{
	$aprtInspector = AssetAdmin.inspector;
	$aprtPane = $aprtInspector.particlePane;
	$aprtEmitterPane = $aprtInspector.emitterPane;

	aprtCheck("particle pane built", isObject($aprtPane));
	aprtCheck("it is an AssetParticleInspectorPane",
		$aprtPane.getClassNamespace() $= "AssetParticleInspectorPane");
	aprtCheck("it inherits the shared pane",
		$aprtPane.getSuperClassNamespace() $= "AssetInspectorPane");

	aprtCheck("emitter pane built", isObject($aprtEmitterPane));
	aprtCheck("it is an AssetEmitterInspectorPane",
		$aprtEmitterPane.getClassNamespace() $= "AssetEmitterInspectorPane");
	aprtCheck("it inherits the shared pane too",
		$aprtEmitterPane.getSuperClassNamespace() $= "AssetInspectorPane");

	aprtCheck("both are in the registry",
		strstr($aprtInspector.paneKeys, "Particle") != -1 &&
		strstr($aprtInspector.paneKeys, "Emitter") != -1);

	aprtCheck("the particle pane starts hidden", !$aprtInspector.paneScroller["Particle"].isVisible());
	aprtCheck("and so does the emitter pane", !$aprtInspector.paneScroller["Emitter"].isVisible());
	aprtCheck("the generic inspector is the one on show", $aprtInspector.insScroller.isVisible());

	$aprtTile = AssetAdmin.Dictionary["ParticleAsset"].getButton($aprtAssetId);
	aprtCheck("the particle tile is in the library", isObject($aprtTile));

	$aprtTile.onClick();

	schedule(600, 0, "aprtStep3");
}

//-----------------------------------------------------------------------------
// Choosing one hands the page to the EFFECT pane -- the particle asset used to
// be the last editable kind still going through the generic inspector.
//-----------------------------------------------------------------------------

function aprtStep3()
{
	$aprtAsset = $aprtTile.ParticleAsset;

	aprtCheck("the particle pane took the page", $aprtInspector.paneScroller["Particle"].isVisible());
	aprtCheck("the generic inspector stood down", !$aprtInspector.insScroller.isVisible());
	aprtCheck("the emitter pane is not also on show",
		!$aprtInspector.paneScroller["Emitter"].isVisible());
	aprtCheck("the pane is bound to the asset", $aprtPane.target == $aprtAsset);
	aprtCheck("the inspector reports the pane's asset as the inspected one",
		$aprtInspector.inspectedObject() == $aprtAsset);

	// The three blocks and what is in them.
	aprtCheck("the identity block exists", isObject($aprtPane.identityChain));
	aprtCheck("the effect block exists", isObject($aprtPane.effectChain));
	aprtCheck("the description block exists", isObject($aprtPane.descriptionChain));
	aprtCheck("the grid holds three blocks", $aprtPane.contentGrid.getCount() == 3);

	aprtCheck("asset name row", isObject($aprtPane.row["AssetName"]));
	aprtCheck("category row", isObject($aprtPane.row["AssetCategory"]));
	aprtCheck("life mode row", isObject($aprtPane.row["LifeMode"]));
	aprtCheck("lifetime row", isObject($aprtPane.row["Lifetime"]));
	aprtCheck("description row", isObject($aprtPane.row["AssetDescription"]));

	// The two that exist to keep an asset OUT of the editor.
	aprtCheck("AssetInternal is NOT offered", !isObject($aprtPane.row["AssetInternal"]));
	aprtCheck("nor is AssetPrivate", !isObject($aprtPane.row["AssetPrivate"]));

	// A scale field is a curve over the effect's age, not a number, so no text box
	// can hold one -- they belong to the Scale Graph tab.
	aprtCheck("no scale graph is offered as a field", !isObject($aprtPane.row["QuantityScale"]));
	aprtCheck("nor is the alpha one", !isObject($aprtPane.row["AlphaChannelScale"]));

	aprtCheck("the name row is not editable", !$aprtPane.row["AssetName"].editor.isActive());
	aprtCheck("the life mode row explains itself", $aprtPane.row["LifeMode"].editor.Tooltip !$= "");

	// What the rows actually SHOW, which is a different question from what the
	// asset holds and the one the assertions used to skip. ParticleAsset had its
	// own getFieldValue(time) shadowing SimObject's getFieldValue(fieldName), so
	// every row on both panes read back a sample of a graph curve -- a plausible
	// "1" in every box -- while every check on the asset went on passing.
	aprtCheck("the name row shows the asset's name (" @ $aprtPane.row["AssetName"].getValue() @ ")",
		$aprtPane.row["AssetName"].getValue() $= $aprtAsset.AssetName);
	aprtCheck("the life mode row shows the mode",
		$aprtPane.row["LifeMode"].getValue() $= $aprtAsset.getLifeMode());

	schedule(300, 0, "aprtStep4");
}

//-----------------------------------------------------------------------------
// What the effect pane says about the effect, and its one gating rule.
//-----------------------------------------------------------------------------

function aprtStep4()
{
	%info = $aprtPane.infoLabel.getText();
	aprtCheck("the info line counts the emitters (" @ %info @ ")",
		strstr(%info, "2 emitters") != -1);
	aprtCheck("and names them", strstr(%info, "smoke") != -1 && strstr(%info, "flames") != -1);

	aprtCheck("no warning for a working effect", !$aprtPane.warningLabel.isVisible());

	// bonfire writes neither field, so it is INFINITE -- and an infinite effect
	// never reaches its lifetime, so the number under it is not read.
	aprtCheck("the effect is infinite", $aprtAsset.getLifeMode() $= "INFINITE");
	aprtCheck("so the lifetime row is inert", !$aprtPane.row["Lifetime"].editor.isActive());
	aprtCheck("and says why", strstr($aprtPane.row["Lifetime"].editor.Tooltip, "infinite") != -1);

	$aprtPane.commitValue("LifeMode", "CYCLE");
	aprtCheck("choosing a finite mode takes", $aprtAsset.getLifeMode() $= "CYCLE");
	aprtCheck("and the lifetime row comes live", $aprtPane.row["Lifetime"].editor.isActive());

	$aprtPane.commitValue("LifeMode", "INFINITE");
	aprtCheck("and goes inert again", !$aprtPane.row["Lifetime"].editor.isActive());

	schedule(300, 0, "aprtStep5");
}

//-----------------------------------------------------------------------------
// The dropdown swaps to the emitter pane. Index 1 is "smoke": a LINE emitter
// drawing a still image.
//-----------------------------------------------------------------------------

function aprtStep5()
{
	aprtSelect(1);

	$aprtEmitter = $aprtAsset.getEmitter(0);

	aprtCheck("the emitter pane took the page", $aprtInspector.paneScroller["Emitter"].isVisible());
	aprtCheck("the effect pane stood down", !$aprtInspector.paneScroller["Particle"].isVisible());
	aprtCheck("the generic inspector is still down", !$aprtInspector.insScroller.isVisible());
	aprtCheck("the pane is bound to the emitter", $aprtEmitterPane.target == $aprtEmitter);

	// The document is still the ASSET, because that is what has a file to save.
	aprtCheck("the document is still the particle asset",
		$aprtInspector.documentAsset() == $aprtAsset);

	// A header above the grid, then five blocks in it. The name is not a block:
	// a grid row is as tall as its tallest cell, so a two-row Identity block
	// beside the seven-row Emission one was two thirds empty at every width.
	aprtCheck("the header exists", isObject($aprtEmitterPane.headerChain));
	aprtCheck("and is not in the grid",
		$aprtEmitterPane.headerChain.getGroup() != $aprtEmitterPane.contentGrid);

	aprtCheck("the emission block exists", isObject($aprtEmitterPane.emissionChain));
	aprtCheck("the image block exists", isObject($aprtEmitterPane.imageChain));
	aprtCheck("the orientation block exists", isObject($aprtEmitterPane.orientationChain));
	aprtCheck("the behavior block exists", isObject($aprtEmitterPane.behaviorChain));
	aprtCheck("the render block exists", isObject($aprtEmitterPane.renderChain));
	aprtCheck("the grid holds five blocks", $aprtEmitterPane.contentGrid.getCount() == 5);

	// THE BLOCKS ARE BALANCED. A grid row is as tall as its tallest cell, so
	// unequal blocks do not give a short column and a long one -- they give
	// columns of equal height with the short ones mostly empty, which is what
	// makes a wide layout look unplanned. The first cut was 7 / 4 / 2 / 6 / 6.
	//
	// Five is the number in the default state: a LINE emitter drawing a numbered
	// frame of a still image with FIXED orientation, which is what bonfire's first
	// emitter is. The swaps move it by one either way and nothing else should.
	$aprtBlocks = "emissionChain orientationChain imageChain behaviorChain renderChain";
	for(%i = 0; %i < 5; %i++)
	{
		// getFieldValue, because TorqueScript has no %obj.%name form -- and this
		// is the very call the graph sampler used to shadow.
		%name = getWord($aprtBlocks, %i);
		%rows = aprtVisibleRows($aprtEmitterPane.getFieldValue(%name));
		aprtCheck("the " @ %name @ " block shows five rows (" @ %rows @ ")", %rows == 5);
	}

	aprtCheck("the info line places the emitter (" @ $aprtEmitterPane.infoLabel.getText() @ ")",
		strstr($aprtEmitterPane.infoLabel.getText(), "Emitter 1 of 2") != -1);

	// The 32 graph fields belong to the Emitter Graph tab, not here.
	aprtCheck("no graph field is offered as a row", !isObject($aprtEmitterPane.row["Quantity"]));
	aprtCheck("nor a variation of one", !isObject($aprtEmitterPane.row["SizeXVariation"]));
	aprtCheck("nor a colour channel", !isObject($aprtEmitterPane.row["RedChannel"]));

	// Not registered as fields in the engine at all -- their addProtectedField
	// calls are commented out.
	aprtCheck("physics particles are not offered",
		!isObject($aprtEmitterPane.row["PhysicsParticle"]));

	// What the rows SHOW, checked against the emitter rather than assumed. See
	// the note on the effect pane above: ParticleAssetEmitter had its own
	// getFieldValue(time) shadowing SimObject's, so every one of these read "1".
	aprtCheck("the name row shows the emitter's name (" @
		$aprtEmitterPane.row["EmitterName"].getValue() @ ")",
		$aprtEmitterPane.row["EmitterName"].getValue() $= "smoke");
	aprtCheck("the shape row shows its type",
		$aprtEmitterPane.row["EmitterType"].getValue() $= $aprtEmitter.getEmitterType());
	aprtCheck("the shape size row shows the size (" @
		$aprtEmitterPane.row["EmitterSize"].getValue() @ ")",
		$aprtEmitterPane.row["EmitterSize"].getValue() $= $aprtEmitter.getEmitterSize());
	aprtCheck("the image row shows the image",
		$aprtEmitterPane.row["Image"].getValue() $= $aprtEmitter.getImage());
	aprtCheck("the fixed force angle row shows the angle",
		$aprtEmitterPane.row["FixedForceAngle"].getValue() == $aprtEmitter.getFixedForceAngle());

	schedule(300, 0, "aprtStep6");
}

//-----------------------------------------------------------------------------
// SWAP: image versus animation, and numeric frame versus named.
//-----------------------------------------------------------------------------

function aprtStep6()
{
	aprtCheck("smoke draws a still image", !$aprtEmitterPane.isAnimated());
	aprtCheck("so the source picker says so",
		$aprtEmitterPane.sourceRow.getValue() $= "Static Image");

	aprtCheck("the image row is on show", $aprtEmitterPane.row["Image"].isVisible());
	aprtCheck("and the random-frame switch with it",
		$aprtEmitterPane.row["RandomImageFrame"].isVisible());
	aprtCheck("the animation row is hidden", !$aprtEmitterPane.row["Animation"].isVisible());

	// The image is addressed by number, so the named row is the one that is gone.
	aprtCheck("the frame row is on show", $aprtEmitterPane.row["Frame"].isVisible());
	aprtCheck("the named frame row is hidden", !$aprtEmitterPane.row["NamedFrame"].isVisible());

	aprtCheck("the image row picks images",
		$aprtEmitterPane.row["Image"].assetType $= "ImageAsset");
	aprtCheck("and the animation row picks animations",
		$aprtEmitterPane.row["Animation"].assetType $= "AnimationAsset");

	// GREY, not swap: a random frame is still a frame of the same image.
	aprtCheck("the frame row is live", $aprtEmitterPane.row["Frame"].editor.isActive());
	$aprtEmitterPane.commitValue("RandomImageFrame", true);
	aprtCheck("turning on random frames makes it inert",
		!$aprtEmitterPane.row["Frame"].editor.isActive());
	$aprtEmitterPane.commitValue("RandomImageFrame", false);
	aprtCheck("and turning it off brings it back",
		$aprtEmitterPane.row["Frame"].editor.isActive());

	schedule(300, 0, "aprtStep7");
}

//-----------------------------------------------------------------------------
// GREY: the emission rules, driven by the shape and by Single Particle.
//-----------------------------------------------------------------------------

function aprtStep7()
{
	aprtCheck("smoke is a LINE emitter", $aprtEmitter.getEmitterType() $= "LINE");
	aprtCheck("so its size is live", $aprtEmitterPane.row["EmitterSize"].editor.isActive());
	aprtCheck("and its angle is live", $aprtEmitterPane.row["EmitterAngle"].editor.isActive());

	// A point emits from one spot, so neither means anything.
	$aprtEmitterPane.commitValue("EmitterType", "POINT");
	aprtCheck("a POINT emitter has no size", !$aprtEmitterPane.row["EmitterSize"].editor.isActive());
	aprtCheck("and no angle", !$aprtEmitterPane.row["EmitterAngle"].editor.isActive());
	aprtCheck("and the size row says why",
		strstr($aprtEmitterPane.row["EmitterSize"].editor.Tooltip, "one spot") != -1);

	// A torus is the same shape whichever way it is turned, and the engine's TORUS
	// branch never applies the rotation -- but it does have a size.
	$aprtEmitterPane.commitValue("EmitterType", "TORUS");
	aprtCheck("a TORUS has a size", $aprtEmitterPane.row["EmitterSize"].editor.isActive());
	aprtCheck("but still no angle", !$aprtEmitterPane.row["EmitterAngle"].editor.isActive());

	$aprtEmitterPane.commitValue("EmitterType", "LINE");

	// Targeting replaces the emission ANGLE graph; what goes inert here is the
	// target position while targeting is off.
	aprtCheck("targeting is off", !$aprtEmitter.getIsTargeting());
	aprtCheck("so the target position is inert",
		!$aprtEmitterPane.row["TargetPosition"].editor.isActive());
	$aprtEmitterPane.commitValue("IsTargeting", true);
	aprtCheck("turning targeting on brings it live",
		$aprtEmitterPane.row["TargetPosition"].editor.isActive());

	// The one setter in the engine with no refreshAsset of its own, because
	// AngleToy writes it every mouse move. The pane asks for the refresh instead.
	$aprtEmitterPane.commitValue("TargetPosition", "3 4");
	aprtCheck("a target position written through the pane takes",
		$aprtEmitter.getTargetPosition() $= "3 4");
	aprtCheck("and marks the asset dirty, which the engine setter does not",
		$aprtAsset.isAssetDirty());

	$aprtEmitterPane.commitValue("IsTargeting", false);

	// The widest rule on the pane.
	$aprtEmitterPane.commitValue("SingleParticle", true);
	aprtCheck("a single particle has no shape",
		!$aprtEmitterPane.row["EmitterType"].editor.isActive());
	aprtCheck("no size", !$aprtEmitterPane.row["EmitterSize"].editor.isActive());
	aprtCheck("no targeting", !$aprtEmitterPane.row["IsTargeting"].editor.isActive());
	aprtCheck("and the reason names Single Particle",
		strstr($aprtEmitterPane.row["EmitterType"].editor.Tooltip, "Single Particle") != -1);

	// The switch that turned it all off is itself still live, or there would be no
	// way back.
	aprtCheck("but the switch itself stays live",
		$aprtEmitterPane.row["SingleParticle"].editor.isActive());

	$aprtEmitterPane.commitValue("SingleParticle", false);
	aprtCheck("turning it off brings the shape back",
		$aprtEmitterPane.row["EmitterType"].editor.isActive());

	schedule(300, 0, "aprtStep8");
}

//-----------------------------------------------------------------------------
// SWAP: the orientation arms. GREY: attach rotation, and the blend rows.
//-----------------------------------------------------------------------------

function aprtStep8()
{
	$aprtEmitterPane.commitValue("OrientationType", "FIXED");
	aprtCheck("FIXED shows its own angle", $aprtEmitterPane.row["FixedAngleOffset"].isVisible());
	aprtCheck("and hides the aligned one", !$aprtEmitterPane.row["AlignedAngleOffset"].isVisible());
	aprtCheck("and the random arc", !$aprtEmitterPane.row["RandomArc"].isVisible());

	$aprtEmitterPane.commitValue("OrientationType", "ALIGNED");
	aprtCheck("ALIGNED shows its angle", $aprtEmitterPane.row["AlignedAngleOffset"].isVisible());
	aprtCheck("and Keep Aligned with it", $aprtEmitterPane.row["KeepAligned"].isVisible());
	aprtCheck("and hides the fixed angle", !$aprtEmitterPane.row["FixedAngleOffset"].isVisible());

	$aprtEmitterPane.commitValue("OrientationType", "RANDOM");
	aprtCheck("RANDOM shows the centre angle",
		$aprtEmitterPane.row["RandomAngleOffset"].isVisible());
	aprtCheck("and the arc", $aprtEmitterPane.row["RandomArc"].isVisible());
	aprtCheck("and hides Keep Aligned", !$aprtEmitterPane.row["KeepAligned"].isVisible());

	$aprtEmitterPane.commitValue("OrientationType", "FIXED");

	// Attach rotation is read only from inside the position-attach test.
	aprtCheck("attach rotation is inert on its own",
		!$aprtEmitterPane.row["AttachRotationToEmitter"].editor.isActive());
	$aprtEmitterPane.commitValue("AttachPositionToEmitter", true);
	aprtCheck("attaching position brings it live",
		$aprtEmitterPane.row["AttachRotationToEmitter"].editor.isActive());
	$aprtEmitterPane.commitValue("AttachPositionToEmitter", false);

	// Blending off leaves the two factors nothing to weigh.
	aprtCheck("the blend factors are live", $aprtEmitterPane.row["SrcBlendFactor"].editor.isActive());
	$aprtEmitterPane.commitValue("BlendMode", false);
	aprtCheck("turning blending off makes them inert",
		!$aprtEmitterPane.row["SrcBlendFactor"].editor.isActive() &&
		!$aprtEmitterPane.row["DstBlendFactor"].editor.isActive());
	$aprtEmitterPane.commitValue("BlendMode", true);

	schedule(300, 0, "aprtStep9");
}

//-----------------------------------------------------------------------------
// The second emitter: an animation, with intense particles on.
//-----------------------------------------------------------------------------

function aprtStep9()
{
	aprtSelect(2);
	$aprtFlames = $aprtAsset.getEmitter(1);

	aprtCheck("the pane rebound to the second emitter", $aprtEmitterPane.target == $aprtFlames);
	aprtCheck("without rebuilding itself", $aprtEmitterPane.contentGrid.getCount() == 5);
	aprtCheck("the info line places it",
		strstr($aprtEmitterPane.infoLabel.getText(), "Emitter 2 of 2") != -1);

	// Kept so step 10 can put it back: switching source clears the other asset,
	// which is the point of the swap, and an emitter holding neither is what the
	// effect pane's second warning is about.
	$aprtFlamesAnim = $aprtFlames.getAnimation();

	aprtCheck("flames plays an animation", $aprtEmitterPane.isAnimated());
	aprtCheck("so the source picker says so",
		$aprtEmitterPane.sourceRow.getValue() $= "Animation");
	aprtCheck("the animation row is on show", $aprtEmitterPane.row["Animation"].isVisible());
	aprtCheck("the image row is hidden", !$aprtEmitterPane.row["Image"].isVisible());
	aprtCheck("and so are both frame rows",
		!$aprtEmitterPane.row["Frame"].isVisible() &&
		!$aprtEmitterPane.row["NamedFrame"].isVisible());

	// Intense particles force additive blending before the blend rows are read.
	aprtCheck("intense particles is on", $aprtFlames.getIntenseParticles());
	aprtCheck("so the blend rows are inert",
		!$aprtEmitterPane.row["BlendMode"].editor.isActive() &&
		!$aprtEmitterPane.row["SrcBlendFactor"].editor.isActive());
	aprtCheck("and the reason names it",
		strstr($aprtEmitterPane.row["BlendMode"].editor.Tooltip, "Intense") != -1);

	$aprtEmitterPane.commitValue("IntenseParticles", false);
	aprtCheck("turning it off brings blending back",
		$aprtEmitterPane.row["BlendMode"].editor.isActive() &&
		$aprtEmitterPane.row["SrcBlendFactor"].editor.isActive());
	$aprtEmitterPane.commitValue("IntenseParticles", true);

	schedule(300, 0, "aprtStep10");
}

//-----------------------------------------------------------------------------
// The source swap, written. And the dropdown caption following a rename.
//-----------------------------------------------------------------------------

function aprtStep10()
{
	// Switching an emitter to a still image and back. There is no StaticMode
	// field: the mode is whichever of the two assets was written last, so this
	// checks the picker really moves it rather than just relabelling.
	$aprtEmitterPane.commitValue("Source", "Static Image");
	aprtCheck("switching to a still image takes", !$aprtEmitterPane.isAnimated());
	aprtCheck("and the animation is let go", $aprtFlames.getAnimation() $= "");
	aprtCheck("the image row appeared", $aprtEmitterPane.row["Image"].isVisible());
	aprtCheck("and the animation row went", !$aprtEmitterPane.row["Animation"].isVisible());

	$aprtEmitterPane.commitValue("Image", "ToyAssets:Particles4");
	aprtCheck("an image can then be chosen", $aprtFlames.getImage() $= "ToyAssets:Particles4");

	// Switching back with no animation chosen leaves the emitter in animated mode
	// holding nothing. That is exactly the case that cannot be read off the
	// assets -- "no animation asset" looks identical to static -- and is why the
	// pane asks the engine through isStaticMode instead of guessing.
	$aprtEmitterPane.commitValue("Source", "Animation");
	aprtCheck("and switching back takes as well", $aprtEmitterPane.isAnimated());
	aprtCheck("even with no animation chosen yet", $aprtFlames.getAnimation() $= "");
	aprtCheck("the image is let go in turn", $aprtFlames.getImage() $= "");

	// Put the effect back together, or the emitter draws nothing and the warning
	// step below is reading the mess this step made rather than the asset.
	$aprtEmitterPane.commitValue("Animation", $aprtFlamesAnim);
	aprtCheck("the animation can be chosen again", $aprtFlames.getAnimation() $= $aprtFlamesAnim);

	// The caption in the title bar is a copy of a field on the pane, so renaming
	// has to reach it.
	$aprtEmitterPane.commitValue("EmitterName", "embers");
	aprtCheck("the emitter renamed", $aprtFlames.getEmitterName() $= "embers");
	aprtCheck("and the dropdown caption followed it (" @
		$aprtInspector.titleDropDown.getText() @ ")",
		strstr($aprtInspector.titleDropDown.getText(), "embers") != -1);
	aprtCheck("without moving the selection", $aprtInspector.titleDropDown.getSelectedItem() == 2);

	schedule(300, 0, "aprtStep11");
}

//-----------------------------------------------------------------------------
// The emitter bar beside the dropdown. Every one of these used to start from the
// generic inspector, which a particle asset no longer goes through.
//-----------------------------------------------------------------------------

function aprtStep11()
{
	// On the effect itself, none of the three apply.
	aprtSelect(0);
	aprtCheck("nothing to move forward from the effect",
		!$aprtInspector.getMoveEmitterForwardEnabled());
	aprtCheck("nor backward", !$aprtInspector.getMoveEmitterBackwardEnabled());
	aprtCheck("nor to remove", !$aprtInspector.getRemoveEmitterEnabled());

	// The first emitter cannot go back -- this is the moveEmitter(0, -1) that the
	// missing half of the old test used to ask for.
	aprtSelect(1);
	aprtCheck("the first emitter cannot move backward",
		!$aprtInspector.getMoveEmitterBackwardEnabled());
	aprtCheck("but it can move forward", $aprtInspector.getMoveEmitterForwardEnabled());

	// And the last cannot go forward.
	aprtSelect(2);
	aprtCheck("the last emitter cannot move forward",
		!$aprtInspector.getMoveEmitterForwardEnabled());
	aprtCheck("but it can move backward", $aprtInspector.getMoveEmitterBackwardEnabled());

	// Reordering is render order, so it has to actually reorder.
	$aprtInspector.MoveEmitterBackward();
	aprtCheck("moving it back reordered the asset", $aprtAsset.getEmitter(0) == $aprtFlames);
	aprtCheck("and the selection followed the emitter",
		$aprtInspector.titleDropDown.getSelectedItem() == 1);
	aprtCheck("and the pane is still bound to it", $aprtEmitterPane.target == $aprtFlames);

	$aprtInspector.MoveEmitterForward();
	aprtCheck("and forward puts it back", $aprtAsset.getEmitter(1) == $aprtFlames);

	schedule(300, 0, "aprtStepTransport");
}

//-----------------------------------------------------------------------------
// The no-emitter warning, and standing down for another asset kind.
//-----------------------------------------------------------------------------

function aprtStep12()
{
	// Removing the last emitter is refused, so the empty warning is reached by
	// clearing the asset directly -- which is also what makes it worth having:
	// an effect can arrive from a file in this state.
	aprtSelect(0);
	aprtCheck("no warning while it has emitters", !$aprtPane.warningLabel.isVisible());

	$aprtAsset.clearEmitters();
	$aprtAsset.refreshAsset();

	aprtCheck("an effect with no emitters is called out", $aprtPane.warningLabel.isVisible());
	aprtCheck("and the warning says nothing will be drawn",
		strstr($aprtPane.warningLabel.getText(), "nothing will be drawn") != -1);
	aprtCheck("and the info line agrees", $aprtPane.infoLabel.getText() $= "No emitters.");

	schedule(300, 0, "aprtStep13");
}

//-----------------------------------------------------------------------------
// The transport over the preview. None of it is asset state, so none of it may
// dirty the asset -- which is the one thing about it worth asserting hardest.
//-----------------------------------------------------------------------------

function aprtStepTransport()
{
	// Back to a whole effect, and a freshly built preview player with it.
	$aprtTile.onClick();

	schedule(400, 0, "aprtStepTransport2");
}

function aprtStepTransport2()
{
	$aprtBar = AssetAdmin.particleTransportBar;

	aprtCheck("the particle transport exists", isObject($aprtBar));
	aprtCheck("it is on show over a particle preview",
		AssetAdmin.particleTransportBarContainer.isVisible());
	aprtCheck("it inherits the shared transport chrome",
		$aprtBar.getSuperClassNamespace() $= "EditorTransportBar");
	aprtCheck("it has a preview player to drive", isObject(AssetAdmin.previewPlayer));

	%player = AssetAdmin.previewPlayer;

	// Play and Pause are one button's worth of space with exactly one on show.
	aprtCheck("a fresh preview is playing", %player.getIsPlaying() && !%player.getPaused());
	aprtCheck("so the bar offers Pause", $aprtBar.pauseButton.isVisible());
	aprtCheck("and not Play", !$aprtBar.playButton.isVisible());

	$aprtBar.pause();
	aprtCheck("pausing pauses the player", %player.getPaused());
	aprtCheck("and the bar offers Play again", $aprtBar.playButton.isVisible());
	aprtCheck("and not Pause", !$aprtBar.pauseButton.isVisible());

	$aprtBar.play();
	aprtCheck("playing resumes it", !%player.getPaused());

	// Stop lets the particles already out finish rather than killing the player,
	// which would leave the preview empty.
	$aprtBar.stop();
	aprtCheck("stopping stops emission", !%player.getIsPlaying());
	aprtCheck("but the player is still there", isObject(AssetAdmin.previewPlayer));

	$aprtBar.restart();
	aprtCheck("restart starts it again", %player.getIsPlaying() && !%player.getPaused());

	// Speed.
	aprtCheck("the preview starts at normal speed", $aprtBar.speed() == 1);
	$aprtBar.cycleSpeed();
	aprtCheck("cycling changes the speed", $aprtBar.speed() != 1);
	aprtCheck("and the player took it", %player.getTimeScale() == $aprtBar.speed());
	aprtCheck("and the tooltip says which",
		strstr($aprtBar.speedButton.Tooltip, $aprtBar.speed() @ "x") != -1);

	schedule(300, 0, "aprtStepSolo");
}

function aprtStepSolo()
{
	%player = AssetAdmin.previewPlayer;

	// On the effect itself there is no emitter to isolate.
	aprtSelect(0);
	aprtCheck("solo is unavailable on the effect", !$aprtBar.soloButton.isActive());
	aprtCheck("and so is mute", !$aprtBar.emitterOffButton.isActive());

	aprtSelect(1);
	aprtCheck("both come live on an emitter",
		$aprtBar.soloButton.isActive() && $aprtBar.emitterOffButton.isActive());
	aprtCheck("every emitter is visible to begin with",
		%player.getEmitterVisible(0) && %player.getEmitterVisible(1));

	$aprtBar.soloOn = true;
	$aprtBar.reapply();
	aprtCheck("solo keeps the selected emitter", %player.getEmitterVisible(0));
	aprtCheck("and hides the other one", !%player.getEmitterVisible(1));

	// Moving the selection moves what is isolated -- solo means "the one I am
	// looking at", not "the one I first pressed it on".
	aprtSelect(2);
	aprtCheck("solo follows the selection", %player.getEmitterVisible(1));
	aprtCheck("and lets the first one go", !%player.getEmitterVisible(0));

	$aprtBar.soloOn = false;
	$aprtBar.reapply();
	aprtCheck("clearing solo shows them all again",
		%player.getEmitterVisible(0) && %player.getEmitterVisible(1));

	// Mute is the opposite shape: it pauses only the selected one.
	$aprtBar.emitterOff = true;
	$aprtBar.reapply();
	aprtCheck("mute pauses the selected emitter", %player.getEmitterPaused(1));
	aprtCheck("and leaves the other running", !%player.getEmitterPaused(0));
	$aprtBar.emitterOff = false;
	$aprtBar.reapply();

	// The whole point of putting this on the player rather than the asset.
	$aprtAsset.saveAsset();
	aprtCheck("none of the transport dirtied the asset", !$aprtAsset.isAssetDirty());
	$aprtBar.soloOn = true;
	$aprtBar.reapply();
	$aprtBar.cycleSpeed();
	aprtCheck("and soloing still does not", !$aprtAsset.isAssetDirty());

	// An edit rebuilds the whole preview -- the commit ends in refreshAsset and
	// refreshPreview re-clicks the tile, so this is a DIFFERENT ParticlePlayER
	// afterwards, which is why the check re-reads it rather than using the handle
	// above. The rebuilt emitters all arrive visible, so the bar has to say the
	// solo again or it silently comes undone on the first field you change.
	$aprtEmitterPane.commitValue("OldestInFront", true);

	%rebuilt = AssetAdmin.previewPlayer;
	aprtCheck("the edit rebuilt the preview player", %rebuilt != %player);
	aprtCheck("solo survives an edit that rebuilt the emitters",
		!%rebuilt.getEmitterVisible(0) && %rebuilt.getEmitterVisible(1));

	$aprtBar.soloOn = false;
	$aprtBar.reapply();

	schedule(300, 0, "aprtStep12");
}

function aprtStep13()
{
	%imageTile = AssetAdmin.Dictionary["ImageAsset"].getButton("ToyAssets:TD_Barbarian_CompSprite");
	%imageTile.onClick();

	aprtCheck("the particle pane stood down", !$aprtInspector.paneScroller["Particle"].isVisible());
	aprtCheck("the emitter pane too", !$aprtInspector.paneScroller["Emitter"].isVisible());
	aprtCheck("the image pane took over", $aprtInspector.imageScroller.isVisible());
	aprtCheck("both particle panes were unbound",
		!isObject($aprtPane.target) && !isObject($aprtEmitterPane.target));

	echo("APRT DONE");
	schedule(200, 0, "quit");
}

