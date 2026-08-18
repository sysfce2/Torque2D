// Asset Manager image-inspector smoke test. Drives the custom pane that replaced
// the generic GuiInspector for image assets: the four reflowing blocks, the
// read-only rows, the commit path, and the notification chain that carries a
// change made anywhere else back to the pane. Then the Image Layers tab's color
// column, which is where the blend color lives now.
// Run: tests/run.ps1 assetImageInspector  ; grep AIMG in tests/logs/.
//
// Driven by calling the pane rather than by posting input. Where a row sits on
// screen depends on how many columns the grid chose and how far the scroller has
// been dragged, neither of which script can read -- so a click at a computed
// point would be testing the arithmetic in this file.
//
// NOTE: a COPY of toybox/ToyAssets, never the module itself. Committing a field
// writes the asset straight back to its own file -- every setter ends in
// refreshAsset -- so aimed at the repository copy this test would rewrite tracked
// content. The copy goes inside the throwaway project folder, which
// tests/run.ps1 deletes before every run.
//
// NOTE: EditorPreferences writes to the tester's real per-user application data
// folder. Step 1 redirects it for the duration, as assetLibrary does.

setLogMode(1);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

function aiCheck(%label, %cond)
{
	if(%cond) echo("AIMG PASS: " @ %label);
	else      echo("AIMG FAIL: " @ %label);
}

// A control that ends past the bottom of the one holding it is a control whose
// last few pixels are clipped away, and on a scroller those pixels are its down
// arrow. Both scrollers are checked, because they are built the same way.
function aiFitsParent(%label, %child, %parent)
{
	%bottom = getWord(%child.getPosition(), 1) + getWord(%child.getExtent(), 1);
	%room = getWord(%parent.getExtent(), 1);
	aiCheck(%label SPC "(" @ %bottom SPC "of" SPC %room @ ")", %bottom <= %room);
}

// Gems is 512 x 512 cut into 8 x 8 cells of 64 -- a picture whose every number
// is checkable, and a power of two, so the info line has all three things to say.
$aiAssetId = "ToyAssets:Gems";

function aiLoadFixtureAssets()
{
	%copy = testRoot("assetImageInspectorSmokeProject/ToyAssets");

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
schedule(2000, 0, "aiStep1");

//-----------------------------------------------------------------------------
// Opening the Asset Manager on an image asset.
//-----------------------------------------------------------------------------

function aiStep1()
{
	createPath(testRoot("shots/"));

	// Spelled out rather than held in a variable, here and in the copy path
	// above: tests/run.ps1 finds the folder to delete by reading this file for
	// setProjectFolder("..."), so a name it cannot see is a folder it cannot
	// sweep.
	ProjectManager.setProjectFolder("assetImageInspectorSmokeProject");
	EditorPreferences.path = testRoot("shots/assetImageInspectorSmokePrefs.taml");

	aiCheck("fixture asset module registered", aiLoadFixtureAssets());

	// Pages register in load order: EditorConsole, ProjectManager, AssetAdmin,
	// GuiEditor. Selecting the tab is what calls AssetAdmin::open.
	EditorCore.tabBook.selectPage(2);

	schedule(600, 0, "aiStep2");
}

//-----------------------------------------------------------------------------
// The pane exists and stands down until an image asset is chosen.
//-----------------------------------------------------------------------------

function aiStep2()
{
	$aiInspector = AssetAdmin.inspector;
	$aiPane = $aiInspector.imagePane;

	aiCheck("image pane built", isObject($aiPane));
	aiCheck("image pane is an AssetImageInspectorPane",
		$aiPane.getClassNamespace() $= "AssetImageInspectorPane");
	aiCheck("image pane inherits the shared pane",
		$aiPane.getSuperClassNamespace() $= "AssetInspectorPane");
	aiCheck("image pane starts hidden", !$aiInspector.imageScroller.isVisible());
	aiCheck("generic inspector is the one on show", $aiInspector.insScroller.isVisible());

	$aiTile = AssetAdmin.Dictionary["ImageAsset"].getButton($aiAssetId);
	aiCheck("the fixture gave the library " @ $aiAssetId, isObject($aiTile));

	if(!isObject($aiTile))
	{
		echo("AIMG ABORT: no fixture asset, the rest of the run would prove nothing");
		schedule(300, 0, "quit");
		return;
	}

	$aiTile.onClick();
	schedule(400, 0, "aiStep3");
}

//-----------------------------------------------------------------------------
// Binding. The pane takes over the Inspector page for an image asset.
//-----------------------------------------------------------------------------

function aiStep3()
{
	$aiAsset = AssetDatabase.acquireAsset($aiAssetId);

	aiCheck("image pane is on show", $aiInspector.imageScroller.isVisible());
	aiCheck("generic inspector stood down", !$aiInspector.insScroller.isVisible());
	aiCheck("pane is bound to the asset", $aiPane.target == $aiAsset);
	aiCheck("pane knows the asset id", $aiPane.assetId $= $aiAssetId);
	aiCheck("the delete button acts on the bound asset",
		$aiInspector.inspectedObject() == $aiAsset);

	// A "file" row's Find button measures its answer from the asset's own folder,
	// not from the game root -- that is what the stored path is relative to.
	aiCheck("find base is the asset's folder",
		$aiPane.findBase $= AssetDatabase.getAssetPath($aiAssetId));

	// The tab page is resized twice on the way up -- once when it joins the book,
	// again when the frame set gives the window its real size -- and a scroller
	// that kept its original gap to each edge came out taller than the page,
	// which clipped its down arrow off the bottom.
	aiFitsParent("image scroller fits its page",
		$aiInspector.imageScroller, $aiInspector.insPage);
	aiFitsParent("generic inspector scroller fits its page",
		$aiInspector.insScroller, $aiInspector.insPage);
	aiFitsParent("tab book fits the inspector",
		$aiInspector.tabBook, $aiInspector);

	schedule(200, 0, "aiStep4");
}

//-----------------------------------------------------------------------------
// The header: what the asset is.
//-----------------------------------------------------------------------------

function aiStep4()
{
	aiCheck("name row shows the asset name", $aiPane.nameRow.getValue() $= "Gems");

	// The stored path is absolute; the collapsed one is what the file on disk
	// says and the only one that means anything to a reader.
	%file = $aiPane.fileRow.getValue();
	aiCheck("file row shows the relative path (" @ %file @ ")", %file $= "Gems.png");
	aiCheck("file row is a file row, so it has a Find button",
		isObject($aiPane.fileRow.findButton));

	// Renaming is not wired up, so the one row that cannot be changed says why.
	aiCheck("name row is read-only", !$aiPane.nameRow.editor.isActive());
	aiCheck("name row says why it is read-only",
		strstr($aiPane.nameRow.editor.Tooltip, "Renaming") != -1);

	// Four values the pane deliberately does not carry: two that exist to keep an
	// asset out of the editor, and two that only restate what is already on show.
	aiCheck("internal is not offered", !isObject($aiPane.row["AssetInternal"]));
	aiCheck("private is not offered", !isObject($aiPane.row["AssetPrivate"]));
	aiCheck("asset id is not offered", !isObject($aiPane.assetIdRow));
	aiCheck("asset file is not offered", !isObject($aiPane.assetFileRow));

	// And the ones that are not.
	aiCheck("category row is editable", $aiPane.categoryRow.editor.isActive());
	aiCheck("file row is editable", $aiPane.fileRow.editor.isActive());

	schedule(200, 0, "aiStep5");
}

//-----------------------------------------------------------------------------
// The cell table, and the line that says what the engine made of it.
//-----------------------------------------------------------------------------

function aiStep5()
{
	%grid = $aiPane.cellGrid;
	aiCheck("cell table built", isObject(%grid));
	aiCheck("cell count X loaded", %grid.box["CellCountX"].getText() == 8);
	aiCheck("cell count Y loaded", %grid.box["CellCountY"].getText() == 8);
	aiCheck("cell width loaded", %grid.box["CellWidth"].getText() == 64);
	aiCheck("cell height loaded", %grid.box["CellHeight"].getText() == 64);
	aiCheck("cell offset X loaded", %grid.box["CellOffsetX"].getText() == 0);
	aiCheck("cell stride Y loaded", %grid.box["CellStrideY"].getText() == 0);
	aiCheck("row order loaded", %grid.rowOrderBox.getStateOn() == $aiAsset.getCellRowOrder());

	// Width beside height, which is the whole reason the table exists: the stock
	// inspector puts them in an "X Values" group and a "Y Values" group four
	// fields apart.
	aiCheck("width and height share a line",
		getWord(%grid.box["CellWidth"].getPosition(), 1)
			== getWord(%grid.box["CellHeight"].getPosition(), 1));
	aiCheck("width and height are side by side",
		getWord(%grid.box["CellWidth"].getPosition(), 0)
			< getWord(%grid.box["CellHeight"].getPosition(), 0));

	%info = $aiPane.infoLabel.getText();
	aiCheck("info line gives the size (" @ %info @ ")", strstr(%info, "512 x 512") == 0);
	aiCheck("info line counts the frames", strstr(%info, "64 frames") != -1);
	aiCheck("info line reports power of two", strstr(%info, "power of two") != -1);
	aiCheck("nothing to warn about", !$aiPane.warningLabel.isVisible());

	// The info line lives in the frames block, and a block is a quarter of the
	// pane at its widest -- so it has to be as wide as its block and no wider. A
	// chain positions its children without resizing them, so a label authored at
	// the pane's width hangs off the end of the block and is clipped rather than
	// wrapped.
	aiCheck("info line is sized to its block",
		getWord($aiPane.infoLabel.getExtent(), 0)
			<= getWord($aiPane.framesChain.getExtent(), 0));
	aiCheck("cell table is sized to its block",
		getWord($aiPane.cellGrid.getExtent(), 0)
			<= getWord($aiPane.framesChain.getExtent(), 0));

	schedule(200, 0, "aiStep6");
}

//-----------------------------------------------------------------------------
// Committing. A cell value written from the table reaches the asset, and what
// the asset made of it comes back.
//-----------------------------------------------------------------------------

function aiStep6()
{
	%grid = $aiPane.cellGrid;

	%grid.box["CellCountX"].setText(4);
	%grid.commitBox(%grid.box["CellCountX"]);

	aiCheck("a cell commit reaches the asset", $aiAsset.getCellCountX() == 4);
	aiCheck("the asset recut itself (" @ $aiAsset.getFrameCount() @ ")",
		$aiAsset.getFrameCount() == 32);

	// The commit reloads the pane, so the readout is the asset's answer rather
	// than the number that was typed.
	aiCheck("info line followed the recut",
		strstr($aiPane.infoLabel.getText(), "32 frames") != -1);
	aiCheck("the table reloaded from the asset",
		%grid.box["CellCountX"].getText() == 4);

	// An impossible cut: 8 cells of 128 needs 1024 pixels and the image is 512,
	// so calculateImage refuses it and falls back to one frame. That used to be a
	// line in the console and nothing on screen.
	%grid.box["CellWidth"].setText(128);
	%grid.commitBox(%grid.box["CellWidth"]);
	%grid.box["CellCountX"].setText(8);
	%grid.commitBox(%grid.box["CellCountX"]);

	aiCheck("an impossible cut warns",
		$aiPane.warningLabel.isVisible() &&
		strstr($aiPane.warningLabel.getText(), "do not fit") != -1);

	// Back to what the asset was authored with.
	%grid.box["CellWidth"].setText(64);
	%grid.commitBox(%grid.box["CellWidth"]);

	aiCheck("the warning clears again", !$aiPane.warningLabel.isVisible());
	aiCheck("restored to 64 frames", $aiAsset.getFrameCount() == 64);

	schedule(200, 0, "aiStep7");
}

//-----------------------------------------------------------------------------
// A change made anywhere else. Explicit frame mode belongs to another tab, and
// turning it on has to reach the pane -- refreshAsset fires AssetBase::onRefresh,
// which tells the inspector.
//-----------------------------------------------------------------------------

function aiStep7()
{
	$aiAsset.setExplicitMode(true);

	aiCheck("explicit mode greys the cell table", !$aiPane.cellGrid.enabled);
	aiCheck("a greyed box says why",
		strstr($aiPane.cellGrid.box["CellCountX"].Tooltip, "Explicit frame mode") != -1);
	aiCheck("explicit mode warns",
		$aiPane.warningLabel.isVisible() &&
		strstr($aiPane.warningLabel.getText(), "Explicit Frames tab") != -1);

	$aiAsset.setExplicitMode(false);

	aiCheck("leaving explicit mode gives the table back", $aiPane.cellGrid.enabled);
	aiCheck("a box gets its own tip back",
		strstr($aiPane.cellGrid.box["CellCountX"].Tooltip, "How many cells") == 0);
	aiCheck("and the warning goes", !$aiPane.warningLabel.isVisible());

	schedule(200, 0, "aiStep7b");
}

//-----------------------------------------------------------------------------
// The Explicit Frames tab names the cells it adds -- or rather, it no longer
// does, and reads back the name the engine chose.
//
// It used to build "Frame" @ %index itself with no uniqueness check at all, so
// adding a cell after deleting one from the middle produced a duplicate name that
// the rename box right beside it would have refused. An animation addresses these
// cells BY name, so two cells answering to one name is not cosmetic.
//-----------------------------------------------------------------------------

function aiStep7b()
{
	%tool = AssetAdmin.inspector.imageFrameEditPage;
	aiCheck("the explicit frames tool exists", isObject(%tool));

	// Through the checkbox, which is what a person clicks: it is toggleExplicitMode
	// that adds the first cell for an image that has none.
	%tool.explicitModeCheckbox.setStateOn(true);
	%tool.toggleExplicitMode();

	aiCheck("turning explicit mode on cuts a first cell", $aiAsset.getExplicitCellCount() == 1);
	aiCheck("and the engine named it (" @ $aiAsset.getExplicitCellName(0) @ ")",
		$aiAsset.getExplicitCellName(0) $= "Frame0");

	%tool.addNewCell();

	aiCheck("a second cell is named for its own index",
		$aiAsset.getExplicitCellName(1) $= "Frame1");

	// The row shows what the engine chose, rather than what the tool guessed.
	%row = %tool.rowChain.getObject(%tool.rowChain.getCount() - 1);
	aiCheck("the new row carries that name (" @ %row.CellName @ ")", %row.CellName $= "Frame1");
	aiCheck("and its name box shows it", %row.nameBox.getText() $= "Frame1");

	// Every cell is addressable, which is the invariant the naming exists for.
	aiCheck("the name resolves back to its index", $aiAsset.getExplicitCellIndex("Frame1") == 1);

	%tool.explicitModeCheckbox.setStateOn(false);
	%tool.toggleExplicitMode();

	// The cells outlive the mode -- turning it off must not throw them away, or
	// every animation naming them would be unresolvable for good.
	aiCheck("the cells survive leaving explicit mode", $aiAsset.getExplicitCellCount() == 2);

	schedule(200, 0, "aiStep8");
}

//-----------------------------------------------------------------------------
// The sections. A field row commits the same way the table does.
//-----------------------------------------------------------------------------

function aiStep8()
{
	// No collapsible sections: eleven fields fit in the open, and a section header
	// is a thing to click before you can read anything.
	aiCheck("nothing is hidden behind a panel", !isObject($aiPane.panel["Rendering"])
		&& !isObject($aiPane.panel["Asset"]));

	// Four blocks in one grid, which is what lets the same pane be a column, a
	// square or a row.
	aiCheck("the content is one grid of four blocks",
		isObject($aiPane.contentGrid) && $aiPane.contentGrid.getCount() == 4);
	aiCheck("the grid is capped at four columns",
		$aiPane.contentGrid.MaxColCount == 4);

	aiCheck("filter is a drop-down", $aiPane.row["FilterMode"].kind $= "enum");
	aiCheck("filter offers three modes",
		$aiPane.row["FilterMode"].editor.getItemCount() == 3);

	// The blend color is not here. It tints the base layer that an asset's layers
	// are composed onto, does nothing at all when there are no layers -- which is
	// nearly every image asset -- and is edited on the Image Layers tab, in the
	// row for the thing it tints. Offered here it was a picker that did nothing.
	aiCheck("blend color is not offered here", !isObject($aiPane.row["BlendColor"]));

	// A description is a paragraph, and the library searches by it.
	%row = $aiPane.row["AssetDescription"];
	aiCheck("description is a paragraph box", %row.kind $= "multiline");

	%row.editor.setText("a smoke test description");
	$aiPane.onFieldRowCommit(%row);
	aiCheck("a row commit reaches the asset",
		$aiAsset.AssetDescription $= "a smoke test description");
	aiCheck("the library tile heard about it",
		strstr($aiTile.searchKey, "a smoke test description") != -1);

	// An unchanged row is left alone: every write saves the asset's file, so a
	// box the user only tabbed through must not rewrite it.
	$aiAsset.AssetCategory = "Smoke";
	$aiPane.refresh();
	%before = $aiPane.categoryRow.getValue();
	$aiPane.onFieldRowCommit($aiPane.categoryRow);
	aiCheck("an untouched row writes nothing",
		%before $= "Smoke" && $aiAsset.AssetCategory $= "Smoke");

	schedule(200, 0, "aiStep9");
}

//-----------------------------------------------------------------------------
// Reflow. The inspector is the bottom frame of the frame set, so it opens wide
// and short and is dragged to whatever shape suits the work.
//-----------------------------------------------------------------------------

// How many columns the four blocks are currently spread across: the number of
// them sharing the top row. Read from where the grid actually put them rather
// than from the arithmetic that placed them, so it disagrees when the layout is
// wrong.
function aiColumnCount()
{
	%grid = $aiPane.contentGrid;
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

// The reflow, at the three widths it was designed against. Driven by resizing
// the pane, because the widest of them is wider than the test canvas -- the
// frame-driven case follows in aiStep9Narrow, and that is the one that catches
// the width failing to arrive.
function aiStep9()
{
	%h = getWord($aiPane.getExtent(), 1);

	// Put back afterwards. The pane follows its scroller by the CHANGE in width
	// rather than by recomputing from it, so a width written here by hand is one
	// the scroller never agrees to take back -- and the frame-driven checks below
	// would then be measuring this line rather than the editor.
	%natural = getWord($aiPane.getExtent(), 0);

	$aiPane.resize(0, 0, 380, %h);
	aiCheck("a narrow pane stacks the blocks (" @ aiColumnCount() @ " across)",
		aiColumnCount() == 1);

	$aiPane.resize(0, 0, 672, %h);
	aiCheck("the pane as it opens is two by two (" @ aiColumnCount() @ " across)",
		aiColumnCount() == 2);

	// The foot of a wide screen. The cap is what stops the grid working out six
	// columns from the width, filling four of them and leaving two empty.
	$aiPane.resize(0, 0, 1600, %h);
	aiCheck("a wide pane is a single row (" @ aiColumnCount() @ " across)",
		aiColumnCount() == 4);
	aiCheck("wide, the blocks share the width evenly",
		getWord($aiPane.settingsChain.getExtent(), 0)
			== getWord($aiPane.identityChain.getExtent(), 0));
	aiCheck("wide, the blocks reach the right-hand edge",
		getWord($aiPane.descriptionChain.getPosition(), 0)
			+ getWord($aiPane.descriptionChain.getExtent(), 0) >= 1580);

	$aiPane.resize(0, 0, %natural, %h);

	// Leave the library most of the width and the inspector most of the height,
	// so the inspector becomes the tall narrow column the reflow exists for.
	AssetAdmin.content.setFrameSize(AssetAdmin.libraryFrameId,
		getWord(Canvas.getExtent(), 0) - 400);
	AssetAdmin.content.setFrameSize(AssetAdmin.inspectorFrameId,
		getWord(Canvas.getExtent(), 1) - 220);

	schedule(400, 0, "aiStep9Narrow");
}

function aiStep9Narrow()
{
	// The window has to fit the frame the divider left it. A frame set moves its
	// divider whatever the window's MinExtent says, and a window that refuses to
	// follow simply hangs off the right-hand edge and is clipped -- so the
	// measurement that catches it is against the library beside it, not against
	// anything inside the window.
	%right = getWord(AssetAdmin.inspectorWindow.getPosition(), 0)
		+ getWord(AssetAdmin.inspectorWindow.getExtent(), 0);
	%libLeft = getWord(AssetAdmin.libWindow.getPosition(), 0);
	aiCheck("narrow, the inspector stays out of the library (" @ %right SPC %libLeft @ ")",
		%right <= %libLeft);

	%paneW = getWord($aiPane.getExtent(), 0);
	%viewW = getWord($aiInspector.imageScroller.getExtent(), 0);
	aiCheck("narrow, the pane fits what can be seen of it (" @ %paneW SPC %viewW @ ")",
		%paneW <= %viewW);

	aiCheck("narrow, the content grid fits the pane",
		getWord($aiPane.contentGrid.getExtent(), 0) <= %paneW);
	aiCheck("narrow, the description fits the pane",
		getWord($aiPane.descriptionRow.getExtent(), 0) <= %paneW);
	aiCheck("narrow, the blocks stack (" @ aiColumnCount() @ " across)",
		aiColumnCount() == 1);

	// The Find button is the right-hand end of the widest row in the header, so
	// it is the first thing to fall off the edge when the pane does not shrink.
	%find = $aiPane.fileRow.findButton;
	aiCheck("narrow, the Find button is still on screen",
		getWord(%find.getPosition(), 0) + getWord(%find.getExtent(), 0) <= %paneW);

	AssetAdmin.content.setFrameSize(AssetAdmin.libraryFrameId, 324);
	AssetAdmin.content.setFrameSize(AssetAdmin.inspectorFrameId, 360);

	schedule(400, 0, "aiStep10");
}

//-----------------------------------------------------------------------------
// Unbinding. Choosing an asset of another kind hands the page back.
//-----------------------------------------------------------------------------

function aiStep10()
{
	// A real font asset, chosen the way a user would. This used to hand the IMAGE
	// asset to loadFontAsset and check that the generic inspector took over --
	// which stopped meaning anything when font assets got a pane of their own.
	// The question is the same either way: does the image pane hand the page back.
	%fontTile = AssetAdmin.Dictionary["FontAsset"].getButton("ToyAssets:ArialFont");
	aiCheck("the fixture gave the library a font asset too", isObject(%fontTile));
	%fontTile.onClick();

	aiCheck("another asset kind takes the page from the image pane",
		$aiInspector.paneScroller["Font"].isVisible() && !$aiInspector.imageScroller.isVisible());
	aiCheck("the image pane let go of its asset", !isObject($aiPane.target));

	// And back, so nothing about the swap is one-way.
	$aiTile.onClick();
	$aiInspector.loadImageAsset($aiAsset, $aiAssetId);
	aiCheck("and it takes the page back", $aiInspector.imageScroller.isVisible());
	aiCheck("rebound to the asset", $aiPane.target == $aiAsset);

	schedule(300, 0, "aiStep11");
}

//-----------------------------------------------------------------------------
// The Image Layers tab, which is where the blend color went. Row 0 is not a
// layer anybody added -- it is the asset's own image, the base every other row
// is drawn onto, and its tint IS the asset's BlendColor. It is locked while it
// is alone, because a tint on a base with nothing composed onto it shows up
// nowhere.
//-----------------------------------------------------------------------------

// Rows in the chain: 0 is the header, 1 is the base, and a layer N is at N + 1.
function aiLayerRow(%index)
{
	return $aiLayers.rowChain.getObject(%index + 1);
}

function aiStep11()
{
	$aiLayers = $aiInspector.imageLayersEditPage;
	%base = aiLayerRow(0);

	aiCheck("the layers tab has a base row", isObject(%base) && %base.LayerIndex == 0);
	aiCheck("the color column is a picker, not a text box",
		%base.colorBox.getClassName() $= "GuiColorPopupCtrl");
	aiCheck("the picker offers the exact numbers too", %base.colorBox.showColorValues);

	// Alone, so locked.
	aiCheck("the base color is locked while it is alone",
		$aiAsset.getLayerCount() == 0 && !%base.colorBox.isActive());
	aiCheck("a padlock says so without being hovered", %base.lockIcon.isVisible());
	aiCheck("the padlock is drawn dark over the swatch (" @ %base.lockIcon.imageColor @ ")",
		getWord(%base.lockIcon.imageColor, 0) == 0
			&& getWord(%base.lockIcon.imageColor, 3) < 128);
	aiCheck("and the swatch says why if you do hover",
		strstr(%base.colorBox.Tooltip, "no layers yet") != -1);

	$aiLayers.addNewLayer();
	schedule(300, 0, "aiStep12");
}

function aiStep12()
{
	%base = aiLayerRow(0);
	%layer = aiLayerRow(1);

	aiCheck("adding a layer built a row for it", isObject(%layer) && %layer.LayerIndex == 1);
	aiCheck("a layer unlocks the base color",
		%base.colorBox.isActive() && !%base.lockIcon.isVisible());
	aiCheck("the base swatch stops explaining itself", %base.colorBox.Tooltip $= "");
	aiCheck("a real layer's color was never locked",
		%layer.colorBox.isActive() && !%layer.lockIcon.isVisible());

	// The base row now writes the asset's blend color. Through the picker, as a
	// ColorF -- the same four numbers read as a ColorI would round to black.
	%base.colorBox.setColorF("1 0.5 0.25 1");
	%base.LayerColorChange();

	%blend = $aiAsset.getBlendColor();
	aiCheck("the base row writes the asset's blend color (" @ %blend @ ")",
		getWord(%blend, 0) > 0.9 && getWord(%blend, 1) > 0.4 && getWord(%blend, 1) < 0.6);
	aiCheck("and the layer above keeps its own color (" @ $aiAsset.getLayerBlendColor(1) @ ")",
		getWord($aiAsset.getLayerBlendColor(1), 1) > 0.9);

	// And a layer row writes that layer's.
	%layer.colorBox.setColorF("0.25 1 0.5 1");
	%layer.LayerColorChange();
	aiCheck("a layer row writes its own color (" @ $aiAsset.getLayerBlendColor(1) @ ")",
		getWord($aiAsset.getLayerBlendColor(1), 0) < 0.4);

	// Through the button's own path, schedule and all, rather than reaching past
	// it -- the deferral exists because the button raising the event is about to
	// be deleted.
	%layer.RemoveLayer();
	schedule(400, 0, "aiStep13");
}

function aiStep13()
{
	%base = aiLayerRow(0);

	aiCheck("taking the layer away locks the base color again",
		$aiAsset.getLayerCount() == 0 && !%base.colorBox.isActive());
	aiCheck("and the padlock comes back", %base.lockIcon.isVisible());

	AssetDatabase.releaseAsset($aiAssetId);
	schedule(400, 0, "quit");
}
