// Asset Manager font-inspector smoke test. Drives the custom pane that replaced
// the generic GuiInspector for font assets: the three reflowing blocks, the
// relative file path, the line describing what the .fnt held, and both warnings.
// Run: tests/run.ps1 assetFontInspector  ; grep AFNT in tests/logs/.
//
// Driven by calling the pane rather than by posting input, for the same reason
// assetImageInspector is: where a row sits depends on how many columns the grid
// chose and how far the scroller has been dragged, neither of which script can
// read.
//
// NOTE: a COPY of toybox/ToyAssets. Nothing here saves, but re-pointing FontFile
// at a missing file is exactly the sort of edit that should not happen to real
// content if a later change ever makes a commit write.

setLogMode(1);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

function afnCheck(%label, %cond)
{
	if(%cond) echo("AFNT PASS: " @ %label);
	else      echo("AFNT FAIL: " @ %label);
}

// Arial.fnt: size 128, lineHeight 128, base 103, two 512 x 512 pages, 97 glyphs.
// Every number the info line prints is one that can be read out of the file.
$afnAssetId = "ToyAssets:ArialFont";

function afnLoadFixtureAssets()
{
	%copy = testRoot("assetFontInspectorSmokeProject/ToyAssets");

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

testExec("editor/main.cs");
schedule(2000, 0, "afnStep1");

//-----------------------------------------------------------------------------

function afnStep1()
{
	createPath(testRoot("shots/"));

	// Spelled out rather than held in a variable: tests/run.ps1 finds the folder
	// to delete by reading this file for setProjectFolder("...").
	ProjectManager.setProjectFolder("assetFontInspectorSmokeProject");
	EditorPreferences.path = testRoot("shots/assetFontInspectorSmokePrefs.taml");

	afnCheck("fixture asset module registered", afnLoadFixtureAssets());

	EditorCore.tabBook.selectPage(2);

	schedule(600, 0, "afnStep2");
}

//-----------------------------------------------------------------------------
// The pane exists, and stands down until a font is chosen.
//-----------------------------------------------------------------------------

function afnStep2()
{
	$afnInspector = AssetAdmin.inspector;
	$afnPane = $afnInspector.fontPane;

	afnCheck("font pane built", isObject($afnPane));
	afnCheck("it is an AssetFontInspectorPane",
		$afnPane.getClassNamespace() $= "AssetFontInspectorPane");
	afnCheck("it inherits the shared pane",
		$afnPane.getSuperClassNamespace() $= "AssetInspectorPane");
	afnCheck("it starts hidden", !$afnInspector.paneScroller["Font"].isVisible());
	afnCheck("the generic inspector is the one on show", $afnInspector.insScroller.isVisible());
	afnCheck("the font pane is in the registry", strstr($afnInspector.paneKeys, "Font") != -1);

	$afnTile = AssetAdmin.Dictionary["FontAsset"].getButton($afnAssetId);
	afnCheck("the font tile is in the library", isObject($afnTile));

	$afnTile.onClick();

	schedule(600, 0, "afnStep3");
}

//-----------------------------------------------------------------------------
// Choosing one hands the page to the pane.
//-----------------------------------------------------------------------------

function afnStep3()
{
	afnCheck("the pane took the page", $afnInspector.paneScroller["Font"].isVisible());
	afnCheck("the generic inspector stood down", !$afnInspector.insScroller.isVisible());
	afnCheck("the pane is bound to the asset", $afnPane.target == $afnTile.FontAsset);
	afnCheck("the inspector reports the pane's asset as the inspected one",
		$afnInspector.inspectedObject() == $afnTile.FontAsset);

	// The three blocks and what is in them.
	afnCheck("the identity block exists", isObject($afnPane.identityChain));
	afnCheck("the font block exists", isObject($afnPane.fontChain));
	afnCheck("the description block exists", isObject($afnPane.descriptionChain));
	afnCheck("the grid holds three blocks", $afnPane.contentGrid.getCount() == 3);

	afnCheck("asset name row", isObject($afnPane.row["AssetName"]));
	afnCheck("category row", isObject($afnPane.row["AssetCategory"]));
	afnCheck("font file row", isObject($afnPane.row["FontFile"]));
	afnCheck("auto unload row", isObject($afnPane.row["AssetAutoUnload"]));
	afnCheck("description row", isObject($afnPane.row["AssetDescription"]));

	// The two that exist to keep an asset OUT of the editor.
	afnCheck("AssetInternal is NOT offered", !isObject($afnPane.row["AssetInternal"]));
	afnCheck("nor is AssetPrivate", !isObject($afnPane.row["AssetPrivate"]));

	// Renaming an asset changes its id and every file naming it, so the row is
	// readable and inert. Asked of the box: there is no isEnabled binding.
	afnCheck("the name row is not editable", !$afnPane.row["AssetName"].editor.isActive());
	afnCheck("and it says why", $afnPane.row["AssetName"].editor.Tooltip !$= "");

	// The tooltip hook, which is most of what this pane adds over a list of
	// labels -- FontAsset's own registered doc strings are empty.
	afnCheck("the file row explains itself", $afnPane.row["FontFile"].editor.Tooltip !$= "");
	afnCheck("and the Find button carries the same tip",
		$afnPane.row["FontFile"].findButton.Tooltip $= $afnPane.row["FontFile"].editor.Tooltip);

	schedule(300, 0, "afnStep4");
}

//-----------------------------------------------------------------------------
// The path, and the line describing the file it names.
//-----------------------------------------------------------------------------

function afnStep4()
{
	%asset = $afnPane.target;

	// The whole reason getRelativeFontFile was added. The field itself holds the
	// expanded absolute path, which is neither readable nor portable.
	%shown = $afnPane.row["FontFile"].getValue();
	afnCheck("the file row shows the relative path (" @ %shown @ ")", %shown $= "Arial.fnt");
	afnCheck("which is not what the field holds", %asset.FontFile !$= %shown);

	%info = $afnPane.infoLabel.getText();
	afnCheck("the info line gives the native size (" @ %info @ ")", strstr(%info, "128 px") != -1);
	afnCheck("it counts the glyphs", strstr(%info, "97 glyphs") != -1);
	afnCheck("it counts the pages", strstr(%info, "2 pages") != -1);
	afnCheck("it gives the page size", strstr(%info, "512 x 512") != -1);

	// Line height equals the native size in this font, and the line says so once
	// rather than twice.
	afnCheck("it does not repeat the size as a line height",
		strstr(%info, "line height") == -1);

	// The queries behind the line.
	afnCheck("the asset reports its glyph count", %asset.getGlyphCount() == 97);
	afnCheck("and its page count", %asset.getPageCount() == 2);
	afnCheck("and that both pages loaded", %asset.getLoadedPageCount() == 2);
	afnCheck("and its baseline", %asset.getBaseline() == 103);

	schedule(300, 0, "afnStep5");
}

//-----------------------------------------------------------------------------
// The warning, appearing and clearing.
//
// This is the case the engine used to make untestable: buildFontData returned on
// a failed open without clearing anything, so a font pointed at a missing file
// went on reporting the glyphs of the one it used to have.
//-----------------------------------------------------------------------------

function afnStep5()
{
	%asset = $afnPane.target;

	afnCheck("no warning to begin with", !$afnPane.warningLabel.isVisible());

	$afnPane.commitValue("FontFile", "NoSuchFont.fnt");

	afnCheck("a font that did not load is called out", $afnPane.warningLabel.isVisible());
	afnCheck("and the warning says what to check",
		strstr($afnPane.warningLabel.getText(), "TEXT format") != -1);
	afnCheck("the glyph count went to zero rather than keeping the old font's",
		%asset.getGlyphCount() == 0);
	afnCheck("and the info line says so", $afnPane.infoLabel.getText() $= "No font loaded.");

	$afnPane.commitValue("FontFile", "Arial.fnt");

	afnCheck("the warning clears when the file is put back", !$afnPane.warningLabel.isVisible());
	afnCheck("and the glyphs come back", %asset.getGlyphCount() == 97);

	// Not 194. Re-reading a font used to ADD to the glyph map rather than replace
	// it, because nothing cleared mChar -- so a font asset pointed at a second
	// file reported the union of the two and only ever grew.
	$afnPane.commitValue("FontFile", "Orator Bold.fnt");
	afnCheck("a second font replaces the first rather than joining it (" @
		%asset.getGlyphCount() @ " glyphs)", %asset.getGlyphCount() == 97);
	afnCheck("and the scalars are the second font's, not the first's",
		%asset.getFontSize() == 72 && %asset.getBaseline() == 56);

	$afnPane.commitValue("FontFile", "Arial.fnt");
	afnCheck("and back again", %asset.getFontSize() == 128);

	schedule(300, 0, "afnStep6");
}

//-----------------------------------------------------------------------------
// Committing.
//-----------------------------------------------------------------------------

function afnStep6()
{
	%asset = $afnPane.target;

	$afnPane.commitValue("AssetCategory", "smokeCategory");
	afnCheck("a committed field reaches the asset", %asset.AssetCategory $= "smokeCategory");

	$afnPane.commitValue("AssetDescription", "A font, for smoke testing.");
	afnCheck("so does the description", %asset.AssetDescription $= "A font, for smoke testing.");

	// AssetAutoUnload is a real, working field on a font asset -- unlike on an
	// audio one, where the engine forces it off and the pane leaves it out.
	$afnPane.commitValue("AssetAutoUnload", false);
	afnCheck("auto unload commits", !%asset.AssetAutoUnload);
	$afnPane.commitValue("AssetAutoUnload", true);

	// Editing marks the document unsaved, which is what lights up Save and Undo.
	afnCheck("the asset is dirty after an edit", %asset.isAssetDirty());
	afnCheck("and the inspector offers to save it", $afnInspector.getSaveAssetEnabled());

	schedule(300, 0, "afnStepReflow");
}

//-----------------------------------------------------------------------------
// Reflow. The inspector is the bottom frame of the frame set, so it opens wide
// and short and is dragged to whatever shape suits the work. Three blocks in a
// grid with a 300 floor: one column when narrow, three across a wide screen.
//-----------------------------------------------------------------------------

// How many blocks are sharing the top row. Read from where the grid actually put
// them rather than from the arithmetic that placed them, so it disagrees when
// the layout is wrong.
function afnColumnCount()
{
	%grid = $afnPane.contentGrid;
	%topY = getWord(%grid.getObject(0).getPosition(), 1);

	%count = 0;
	for(%i = 0; %i < %grid.getCount(); %i++)
	{
		if(getWord(%grid.getObject(%i).getPosition(), 1) == %topY)
		{
			%count++;
		}
	}
	return %count;
}

function afnStepReflow()
{
	%h = getWord($afnPane.getExtent(), 1);

	// Put back afterwards. The pane follows its scroller by the CHANGE in width
	// rather than by recomputing from it, so a width written here by hand is one
	// the scroller never agrees to take back.
	%natural = getWord($afnPane.getExtent(), 0);

	$afnPane.resize(0, 0, 380, %h);
	afnCheck("a narrow pane stacks the blocks (" @ afnColumnCount() @ " across)",
		afnColumnCount() == 1);

	$afnPane.resize(0, 0, 672, %h);
	afnCheck("the pane as it opens is two across (" @ afnColumnCount() @ ")",
		afnColumnCount() == 2);

	// The cap is what stops the grid working out five columns from the width,
	// filling three of them and leaving two empty with the blocks too narrow.
	$afnPane.resize(0, 0, 1600, %h);
	afnCheck("a wide pane is a single row (" @ afnColumnCount() @ " across)",
		afnColumnCount() == 3);
	afnCheck("wide, the blocks share the width evenly",
		getWord($afnPane.fontChain.getExtent(), 0)
			== getWord($afnPane.identityChain.getExtent(), 0));
	afnCheck("wide, the blocks reach the right-hand edge",
		getWord($afnPane.descriptionChain.getPosition(), 0)
			+ getWord($afnPane.descriptionChain.getExtent(), 0) >= 1580);

	$afnPane.resize(0, 0, %natural, %h);

	schedule(300, 0, "afnStep7");
}

//-----------------------------------------------------------------------------
// Standing down for another asset kind.
//-----------------------------------------------------------------------------

function afnStep7()
{
	%imageTile = AssetAdmin.Dictionary["ImageAsset"].getButton("ToyAssets:TD_Barbarian_CompSprite");
	%imageTile.onClick();

	afnCheck("the font pane stood down", !$afnInspector.paneScroller["Font"].isVisible());
	afnCheck("the image pane took over", $afnInspector.imageScroller.isVisible());
	afnCheck("exactly one pane is on show at a time", !$afnInspector.insScroller.isVisible());
	afnCheck("the font pane was unbound", !isObject($afnPane.target));

	echo("AFNT DONE");
	schedule(200, 0, "quit");
}
