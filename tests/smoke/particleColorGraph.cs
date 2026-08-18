// Asset Manager color-graph smoke test. Drives the Emitter Graph tab's one
// collapsed entry -- red, green and blue on a single graph with a mixed-color
// strip under it -- and the channel toggles that pick which of the three a click
// on the plot will edit.
// Run: tests/run.ps1 particleColorGraph  ; grep PCLR in tests/logs/.
//
// Driven by calling the tool rather than by posting input, for the same reason
// assetParticleInspector is: where a list row sits depends on the font, and where
// a graph key sits depends on a plot rect computed from it.
//
// NOTE: a COPY of toybox/ToyAssets. Selecting a color channel repairs the fields
// it draws -- that is what the graph has always done -- so it must not touch the
// real content tree.

setLogMode(1);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

function pclrCheck(%label, %cond)
{
	if(%cond) echo("PCLR PASS: " @ %label);
	else      echo("PCLR FAIL: " @ %label);
}

// bonfire has two emitters, so the emitter index is a real value rather than
// always zero -- which is the case the graph used to keep its old channel through.
$pclrAssetId = "ToyAssets:bonfire";

// Where "Color Channel" and "Alpha Channel" sit in the emitter list now that the
// three color entries have become one. Eleven fields precede them.
$pclrColorIndex = 11;
$pclrAlphaIndex = 12;

function pclrLoadFixtureAssets()
{
	%copy = testRoot("particleColorGraphSmokeProject/ToyAssets");

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

// Selecting a row the way the tool's own inspect() does: clear first, or a list
// box that allows more than one selection simply adds to it.
function pclrSelect(%index)
{
	$pclrTool.baseList.clearSelection();
	$pclrTool.baseList.setCurSel(%index);
	$pclrTool.onSelect(%index);
}

function pclrIsShowing(%unit)
{
	return $pclrTool.toolGrid.isMember(%unit);
}

testExec("editor/main.cs");
schedule(2000, 0, "pclrStep1");

//-----------------------------------------------------------------------------

function pclrStep1()
{
	createPath(testRoot("shots/"));

	// Spelled out rather than held in a variable: tests/run.ps1 finds the folder
	// to delete by reading this file for setProjectFolder("...").
	ProjectManager.setProjectFolder("particleColorGraphSmokeProject");
	EditorPreferences.path = testRoot("shots/particleColorGraphSmokePrefs.taml");

	pclrCheck("fixture asset module registered", pclrLoadFixtureAssets());

	EditorCore.tabBook.selectPage(2);

	schedule(600, 0, "pclrStep2");
}

//-----------------------------------------------------------------------------
// Open the asset, then move the title dropdown off the effect and onto an
// emitter -- the Emitter Graph tab only exists for an emitter.
//-----------------------------------------------------------------------------

function pclrStep2()
{
	$pclrInspector = AssetAdmin.inspector;

	%tile = AssetAdmin.Dictionary["ParticleAsset"].getButton($pclrAssetId);
	pclrCheck("the particle tile is in the library", isObject(%tile));
	%tile.onClick();

	schedule(600, 0, "pclrStep3");
}

function pclrStep3()
{
	$pclrAsset = $pclrInspector.inspectedObject();

	// Index 0 is the effect; 1 is the first emitter.
	$pclrInspector.titleDropDown.setSelected(1);
	$pclrInspector.onChooseParticleAsset($pclrAsset);

	schedule(600, 0, "pclrStep4");
}

//-----------------------------------------------------------------------------
// The list collapsed three entries into one.
//-----------------------------------------------------------------------------

function pclrStep4()
{
	$pclrTool = $pclrInspector.emitterGraphPage;
	pclrCheck("the emitter graph tool is built", isObject($pclrTool));

	%list = $pclrTool.baseList;
	pclrCheck("the emitter list has thirteen entries, not fifteen", %list.getItemCount() == 13);
	pclrCheck("red, green and blue became one entry",
		%list.getItemText($pclrColorIndex) $= "Color Channel");
	pclrCheck("alpha kept its own", %list.getItemText($pclrAlphaIndex) $= "Alpha Channel");

	$pclrColorUnit = $pclrTool.colorGraph;
	pclrCheck("the color unit was built", isObject($pclrColorUnit));
	pclrCheck("it is an AssetParticleColorGraphUnit",
		$pclrColorUnit.getClassNamespace() $= "AssetParticleColorGraphUnit");
	pclrCheck("it inherits the ordinary graph unit",
		$pclrColorUnit.getSuperClassNamespace() $= "AssetParticleGraphUnit");
	pclrCheck("its graph is a GuiEditParticleColorGraph",
		$pclrColorUnit.graph.getClassName() $= "GuiEditParticleColorGraph");

	pclrCheck("it starts out of the grid", !pclrIsShowing($pclrColorUnit));

	schedule(200, 0, "pclrStep5");
}

//-----------------------------------------------------------------------------
// Selecting it swaps the whole set of units in the grid.
//-----------------------------------------------------------------------------

function pclrStep5()
{
	pclrSelect($pclrColorIndex);

	pclrCheck("selecting Color Channel shows the color unit", pclrIsShowing($pclrColorUnit));
	pclrCheck("the base graph stood down", !pclrIsShowing($pclrTool.baseGraph));
	pclrCheck("the variation graph stood down", !pclrIsShowing($pclrTool.variGraph));
	pclrCheck("the life graph stood down", !pclrIsShowing($pclrTool.lifeGraph));

	%graph = $pclrColorUnit.graph;
	pclrCheck("a channel is live", %graph.getActiveChannel() $= "Red");
	pclrCheck("and it is the field being edited", %graph.getDisplayField() $= "RedChannel");

	schedule(200, 0, "pclrStep6");
}

//-----------------------------------------------------------------------------
// The toggles are a radio group, and the graph is the one that knows.
//-----------------------------------------------------------------------------

function pclrStep6()
{
	%graph = $pclrColorUnit.graph;

	pclrCheck("the red toggle is lit", $pclrColorUnit.toggle["Red"].getValue());
	pclrCheck("the green toggle is not", !$pclrColorUnit.toggle["Green"].getValue());
	pclrCheck("the blue toggle is not", !$pclrColorUnit.toggle["Blue"].getValue());

	// A checkbox flips itself before the owner hears about it, which is exactly
	// what the owner has to put right.
	$pclrColorUnit.toggle["Green"].setStateOn(true);
	$pclrColorUnit.onToggleIconChanged($pclrColorUnit.toggle["Green"]);

	pclrCheck("clicking green makes green live", %graph.getActiveChannel() $= "Green");
	pclrCheck("and green is now the field a click edits", %graph.getDisplayField() $= "GreenChannel");
	pclrCheck("the green toggle is lit", $pclrColorUnit.toggle["Green"].getValue());
	pclrCheck("and red went out", !$pclrColorUnit.toggle["Red"].getValue());

	// Clicking the live channel would switch its checkbox off on its own.
	$pclrColorUnit.toggle["Green"].setStateOn(false);
	$pclrColorUnit.onToggleIconChanged($pclrColorUnit.toggle["Green"]);

	pclrCheck("clicking the live channel leaves it live", %graph.getActiveChannel() $= "Green");
	pclrCheck("and lights its toggle back up", $pclrColorUnit.toggle["Green"].getValue());

	schedule(200, 0, "pclrStep7");
}

//-----------------------------------------------------------------------------
// The mixed color, and the stops the strip bends at.
//-----------------------------------------------------------------------------

function pclrStep7()
{
	%graph = $pclrColorUnit.graph;
	%emitter = $pclrAsset.getEmitter(0);

	// Give the three channels a shape worth reading: red falls away, green rises,
	// blue is left alone. Every value is set through the asset, so what the graph
	// reports has to have come from the fields rather than from anything it kept.
	%emitter.selectField("RedChannel");
	%emitter.clearDataKeys();
	%emitter.setSingleDataKey(1);
	%emitter.addDataKey(1, 0);

	%emitter.selectField("GreenChannel");
	%emitter.clearDataKeys();
	%emitter.setSingleDataKey(0);
	%emitter.addDataKey(0.5, 1);

	%emitter.selectField("BlueChannel");
	%emitter.clearDataKeys();
	%emitter.setSingleDataKey(0.25);

	$pclrColorUnit.setToColor(0);

	pclrCheck("at birth the mix is the three channels' first keys",
		%graph.getColorAtTime(0) $= "1 0 0.25");
	pclrCheck("at death red has gone and green has arrived",
		%graph.getColorAtTime(1) $= "0 1 0.25");
	pclrCheck("halfway is halfway down red's ramp",
		getWord(%graph.getColorAtTime(0.5), 0) $= "0.5");
	pclrCheck("and the top of green's",
		getWord(%graph.getColorAtTime(0.5), 1) $= "1");

	// Red bends at 0 and 1, green at 0, 0.5 and 1, blue nowhere. With the window
	// at 0 to 1 that is three stops: the two edges and green's key.
	%stops = %graph.getGradientStops();
	pclrCheck("the strip bends where a channel has a key", getWordCount(%stops) == 3);
	pclrCheck("the first stop is the start of life", getWord(%stops, 0) $= "0");
	pclrCheck("the middle stop is green's key", getWord(%stops, 1) $= "0.5");
	pclrCheck("the last stop is the end of life", getWord(%stops, 2) $= "1");

	// A key in a channel that had none is a new bend in the mixed color.
	%emitter.selectField("BlueChannel");
	%emitter.addDataKey(0.75, 1);
	%graph.inspect($pclrAsset);

	pclrCheck("a key added to blue adds a stop",
		getWordCount(%graph.getGradientStops()) == 4);

	schedule(200, 0, "pclrStep8");
}

//-----------------------------------------------------------------------------
// Zoom, which was dead on every 0-1 field.
//-----------------------------------------------------------------------------

function pclrStep8()
{
	pclrCheck("the color graph can zoom in", $pclrColorUnit.valueZoomInButton.isActive());
	pclrCheck("and its time axis can too", $pclrColorUnit.timeZoomInButton.isActive());
	pclrCheck("but it is already as far out as 0-1 goes",
		!$pclrColorUnit.valueZoomOutButton.isActive());

	$pclrColorUnit.valueZoomIn();
	pclrCheck("zooming in lets you back out again", $pclrColorUnit.valueZoomOutButton.isActive());

	// Alpha is still its own entry and its own ordinary graph, and it gained the
	// same zoom for the same reason.
	pclrSelect($pclrAlphaIndex);

	pclrCheck("alpha shows the ordinary graph", pclrIsShowing($pclrTool.baseGraph));
	pclrCheck("and the color unit stood down", !pclrIsShowing($pclrColorUnit));
	pclrCheck("alpha is the field it edits",
		$pclrTool.baseGraph.graph.getDisplayField() $= "AlphaChannel");
	pclrCheck("alpha can zoom now too", $pclrTool.baseGraph.valueZoomInButton.isActive());

	schedule(200, 0, "pclrStep9");
}

//-----------------------------------------------------------------------------
// The channel survives a trip through the other emitter.
//-----------------------------------------------------------------------------

function pclrStep9()
{
	pclrSelect($pclrColorIndex);

	%graph = $pclrColorUnit.graph;
	pclrCheck("the channel came back as it was left", %graph.getActiveChannel() $= "Green");

	// The second emitter, through the same path the title dropdown uses.
	$pclrInspector.titleDropDown.setSelected(2);
	$pclrInspector.onChooseParticleAsset($pclrAsset);

	schedule(400, 0, "pclrStep10");
}

function pclrStep10()
{
	pclrSelect($pclrColorIndex);

	%graph = $pclrColorUnit.graph;
	pclrCheck("switching emitters kept the channel", %graph.getActiveChannel() $= "Green");
	pclrCheck("and the toggles followed it", $pclrColorUnit.toggle["Green"].getValue());
	pclrCheck("the graph moved to the second emitter", $pclrTool.emitterID == 1);

	echo("PCLR DONE");
	quit();
}
