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
// The inspector for a particle asset, in place of the generic one. This is the
// ASSET half; the emitter dropdown in the title bar swaps to
// AssetEmitterInspectorPane for everything below index 0.
//
// A ParticleAsset registers two persistent fields of its own -- Lifetime and
// LifeMode -- and that is genuinely all of it. Everything else an effect is made
// of lives either on its emitters or in the nine scale graphs, and neither is a
// value a text box can hold. So this pane is small on purpose, and what it adds
// over a list of two fields is the pair of readouts: what the effect is made of,
// and whether any of it will actually draw.
//
// Three blocks, the shape AssetFontInspectorPane uses:
//
//   Identity     the name and the category
//   Effect       how long it runs and what it does when it gets there
//   Description  the prose the library is searched by
//
// The nine *Scale fields (LifetimeScale, QuantityScale, SizeXScale ... ) are not
// here and cannot be: each is a curve over the effect's age rather than a number,
// and the Scale Graph tab beside this one is where a curve is drawn. Same for the
// emitters, which are objects rather than values.
//
// AssetName is shown but not editable, for the reason the other panes give:
// AssetBase::setAssetName does nothing once the asset manager owns the asset, so
// a box that accepted typing would silently do nothing. A real rename is
// AssetDatabase.renameDeclaredAsset.
//
// Absent, each for a checkable reason:
//   AssetInternal, AssetPrivate   they exist to keep an asset OUT of the editor
//   asset id, asset file          the module and the name are on show, and the
//                                 file is where the manager put it
//-----------------------------------------------------------------------------

$AssetParticleInspectorPane::cellWidth = 300;
$AssetParticleInspectorPane::cellCount = 3;
$AssetParticleInspectorPane::descriptionHeight = 150;

function AssetParticleInspectorPane::onAdd(%this)
{
	// onAdd does not chain, so the shared setup runs from here.
	%this.init();
}

//-----------------------------------------------------------------------------
// Construction.
//-----------------------------------------------------------------------------

function AssetParticleInspectorPane::buildPane(%this)
{
	%grid = %this.makeCellGrid(0, $AssetParticleInspectorPane::cellWidth,
		$AssetParticleInspectorPane::cellCount);
	%this.add(%grid);
	%this.contentGrid = %grid;

	%this.buildIdentityCell(%grid);
	%this.buildEffectCell(%grid);
	%this.buildDescriptionCell(%grid);

	%this.buildWarning();

	%this.nameRow.setEnabled(false, "Renaming an asset changes its id and every file that refers to it, " @
		"so it is not something the inspector can do safely on its own.");
}

// addFieldRow takes the label and the kind as arguments rather than asking the
// tables for them, so every call here would otherwise repeat the same lookups.
function AssetParticleInspectorPane::addField(%this, %container, %field)
{
	return %this.addFieldRow(%container, %field, %this.labelFor(%field),
		%this.kindFor(%field), %this.enumItemsFor(%field));
}

function AssetParticleInspectorPane::buildIdentityCell(%this, %grid)
{
	%chain = %this.makeCell(%grid, 4);
	%this.identityChain = %chain;

	%this.nameRow = %this.addField(%chain, "AssetName");
	%this.addField(%chain, "AssetCategory");
}

function AssetParticleInspectorPane::buildEffectCell(%this, %grid)
{
	%chain = %this.makeCell(%grid, 4);
	%this.effectChain = %chain;

	// LifeMode first, because it decides whether the Lifetime under it means
	// anything at all.
	%this.lifeModeRow = %this.addField(%chain, "LifeMode");
	%this.lifetimeRow = %this.addField(%chain, "Lifetime");

	// What the effect is made of. Read-only: an emitter is not a value, and the
	// dropdown in the title bar is where one is chosen.
	%this.infoLabel = %this.makeInfoLabel(%chain, "labelProfile");
	%this.infoLabel.textWrap = true;
	%this.infoLabel.textExtend = true;
	%this.infoLabel.vAlign = "top";
}

function AssetParticleInspectorPane::buildDescriptionCell(%this, %grid)
{
	%chain = %this.makeCell(%grid, 4);
	%this.descriptionChain = %chain;

	%this.addField(%chain, "AssetDescription");
}

// Below the grid rather than in a block. A warning is a sentence, and a sentence
// read across the whole pane is one or two lines where the same sentence in a
// third of it is five.
function AssetParticleInspectorPane::buildWarning(%this)
{
	%this.warningLabel = %this.makeInfoLabel(%this, "overrideLabelProfile");
	%this.warningLabel.textWrap = true;
	%this.warningLabel.textExtend = true;
	%this.warningLabel.vAlign = "top";
	%this.warningLabel.setVisible(false);
}

//-----------------------------------------------------------------------------
// The field tables.
//-----------------------------------------------------------------------------

function AssetParticleInspectorPane::labelFor(%this, %field)
{
	switch$(%field)
	{
		case "AssetName": return "Asset Name";
		case "AssetCategory": return "Category";
		case "AssetDescription": return "Description";
		case "LifeMode": return "Life Mode";
		case "Lifetime": return "Lifetime (seconds)";
	}

	return %field;
}

function AssetParticleInspectorPane::kindFor(%this, %field)
{
	switch$(%field)
	{
		case "LifeMode": return "enum";
		case "Lifetime": return "decimal";
		case "AssetDescription": return "multiline";
	}

	return "text";
}

function AssetParticleInspectorPane::enumItemsFor(%this, %field)
{
	// The engine's labels are INFINITE, CYCLE, STOP and KILL (LifeModeTable in
	// ParticleAsset.cc). Both the lookup on the way in and the drop-down's own
	// search ignore case, so these can read as words.
	if(%field $= "LifeMode")
	{
		return "Infinite" TAB "Cycle" TAB "Stop" TAB "Kill";
	}

	return "";
}

function AssetParticleInspectorPane::tipFor(%this, %field)
{
	switch$(%field)
	{
		case "LifeMode": return "What happens when the effect reaches its lifetime. Infinite never gets " @
			"there. Cycle starts it again from the beginning. Stop stops emitting and lets the particles " @
			"already out live their own lifetimes. Kill deletes the player outright, which is what a " @
			"one-shot explosion wants.";

		case "Lifetime": return "How long the effect runs before its life mode takes over, in seconds. " @
			"This is the EFFECT's clock -- the one the scale graphs are drawn against -- not how long an " @
			"individual particle lasts, which is the emitter's Lifetime.";

		case "AssetCategory": return "A word of your own for grouping assets in the library. Nothing in " @
			"the engine reads it.";

		case "AssetDescription": return "What this effect is for. The library's search box reads it.";
	}

	return "";
}

function AssetParticleInspectorPane::editorHeightFor(%this, %field)
{
	if(%field $= "AssetDescription")
	{
		return $AssetParticleInspectorPane::descriptionHeight;
	}

	return 0;
}

//-----------------------------------------------------------------------------
// Loading. Everything the row loop does not reach.
//-----------------------------------------------------------------------------

function AssetParticleInspectorPane::refreshExtras(%this)
{
	%this.infoLabel.setText(%this.describeEffect(%this.target));
	%this.applyGating();
	%this.showWarning(%this.warningFor(%this.target));
}

// The pane's one gating rule. An infinite effect never reaches its lifetime, so
// the number is not read -- greyed rather than hidden, because it still holds a
// value that comes back the moment the mode changes.
function AssetParticleInspectorPane::applyGating(%this)
{
	if(!isObject(%this.target))
	{
		return;
	}

	%infinite = (%this.target.getLifeMode() $= "INFINITE");

	%this.setRowsEnabled("Lifetime", !%infinite,
		"An infinite effect never reaches its lifetime, so this is not read. Choose another life mode " @
		"to use it.");
}

// What the effect is made of. Asked of the asset, stored nowhere on it, which is
// why it is a line of text rather than a set of rows.
function AssetParticleInspectorPane::describeEffect(%this, %asset)
{
	%count = %asset.getEmitterCount();

	if(%count == 0)
	{
		return "No emitters.";
	}

	%names = "";
	for(%i = 0; %i < %count; %i++)
	{
		%name = %asset.getEmitter(%i).getEmitterName();
		if(%name $= "")
		{
			%name = "(unnamed)";
		}

		%names = (%names $= "") ? %name : (%names @ ", " @ %name);
	}

	return %count SPC ((%count == 1) ? "emitter" : "emitters") @ ":" SPC %names @ ".";
}

// In the order they matter. Only the first is shown, because the first is the one
// that has to be fixed before any of the others can be judged.
function AssetParticleInspectorPane::warningFor(%this, %asset)
{
	// ParticleAsset::isAssetValid is exactly this test, and an invalid particle
	// asset draws nothing at all.
	if(%asset.getEmitterCount() == 0)
	{
		return "This effect has no emitters, so nothing will be drawn. Add one with the + button beside " @
			"the dropdown above.";
	}

	// An emitter with neither an image nor an animation is skipped outright by
	// ParticlePlayer, both when it builds its emitter nodes and when it renders --
	// silently, so without this there is nothing to see and nothing said.
	%blank = 0;
	%count = %asset.getEmitterCount();
	for(%i = 0; %i < %count; %i++)
	{
		%emitter = %asset.getEmitter(%i);
		if(%emitter.getImage() $= "" && %emitter.getAnimation() $= "")
		{
			%blank++;
		}
	}

	if(%blank > 0)
	{
		if(%blank == %count)
		{
			return ((%count == 1) ? "This effect's emitter has" : "None of this effect's emitters have") SPC
				"an image or an animation, so nothing will be drawn. Choose one on the emitter's page.";
		}

		return %blank SPC "of this effect's" SPC %count SPC "emitters have no image or animation and will " @
			"not be drawn. Choose one on each emitter's page.";
	}

	return "";
}

// forceLayout only when the visibility actually changed: a chain skips hidden
// children when it lays out, and nothing re-lays it out on setVisible.
function AssetParticleInspectorPane::showWarning(%this, %text)
{
	%wanted = (%text !$= "");
	%changed = (%wanted != %this.warningLabel.isVisible());

	%this.warningLabel.setText(%text);
	%this.warningLabel.setVisible(%wanted);

	if(%changed)
	{
		%this.forceLayout();
	}
}

// The commit ends in refreshAsset, which rebuilds the preview player's emitter
// nodes -- so the transport's play/stop state is no longer whatever it was, and
// the solo it was holding has been rebuilt away. The bar is the only thing that
// knows either, so it is the only thing that has to be told.
function AssetParticleInspectorPane::afterCommit(%this)
{
	if(isObject(AssetAdmin.particleTransportBar))
	{
		AssetAdmin.particleTransportBar.refresh();
	}
}
