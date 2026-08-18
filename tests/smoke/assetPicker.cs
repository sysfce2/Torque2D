// Asset-picker smoke test. Drives EditorAssetPickerDialog from the Gui Profile
// Editor's Image Asset row: the new "asset" row kind, the one-shot asset query,
// live substring filtering, the grid re-flow that filtering depends on,
// selection, choosing, cancelling and dialog cleanup.
// Run: tests/run.ps1 assetPicker  ; grep APSMOKE in tests/logs/.

// Mode 1 rather than the usual 2: it opens, appends and closes the log on every
// write, so a crash mid-run still leaves every line that got as far as being
// echoed. Mode 2 holds the file open and loses the buffered tail.
setLogMode(1);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

function fCheck(%label, %cond)
{
	if(%cond) echo("APSMOKE PASS: " @ %label);
	else      echo("APSMOKE FAIL: " @ %label);
}

// The picker is pushed straight onto the Canvas, so that is where it is found.
// Deliberately not by name: editorMode(true) stops assignName registering names
// while an editor is open, so anything built in here is nameless.
function fPicker()
{
	for(%i = 0; %i < Canvas.getCount(); %i++)
	{
		%ctrl = Canvas.getObject(%i);
		if(%ctrl.class $= "EditorAssetPickerDialog")
		{
			return %ctrl;
		}
	}
	return 0;
}

// What the picker should be showing: every non-internal image asset, counted
// independently of the dialog so a broken query cannot agree with itself.
function fExpectedImageCount()
{
	%query = new AssetQuery();
	AssetDatabase.findAllAssets(%query);
	AssetDatabase.findAssetType(%query, "ImageAsset", true);

	%count = 0;
	for(%i = 0; %i < %query.getCount(); %i++)
	{
		if(!AssetDatabase.isAssetInternal(%query.getAsset(%i)))
		{
			%count++;
		}
	}
	%query.delete();
	return %count;
}

function fVisibleCount(%picker)
{
	%shown = 0;
	for(%i = 0; %i < %picker.grid.getCount(); %i++)
	{
		if(%picker.grid.getObject(%i).isVisible())
		{
			%shown++;
		}
	}
	return %shown;
}

function fSelectCategory(%category)
{
	%d = GuiEditor.profileEditorDialog;
	%proxy = %d.library.categoryProxy[$fTheme.getId() @ "_" @ %category];
	%d.onTreeSelect(%proxy);
	return %proxy;
}

testExec("editor/main.cs");
schedule(2000, 0, "fStep1");

//-----------------------------------------------------------------------------
// The row itself: imageAsset must now be an "asset" row with a Find button.
//-----------------------------------------------------------------------------

// A bare project has no image assets to pick: the only ones in the database
// belong to the editor itself and are all marked AssetInternal, which the picker
// rightly skips. So bring in a real asset pack. ToyAssets is nothing but assets
// -- no ScriptFile, no CreateFunction -- so registering what it declares adds 90
// image and animation assets without running a line of toy code.
function fLoadFixtureAssets()
{
	ModuleDatabase.scanModules(testRoot("toybox/ToyAssets"));
	%module = ModuleDatabase.findModule("ToyAssets", 1);
	if(!isObject(%module))
	{
		return false;
	}
	AssetDatabase.addModuleDeclaredAssets(%module);
	return true;
}

function fStep1()
{
	createPath(testRoot("shots/"));
	ProjectManager.setProjectFolder("assetPickerSmokeProject");
	fCheck("fixture asset module registered", fLoadFixtureAssets());
	GuiEditor.open();
	GuiEditor.openProfileEditor();

	%d = GuiEditor.profileEditorDialog;
	fCheck("dialog opened", isObject(%d));

	$fTheme = %d.library.createTheme("APSmoke");
	%d.tree.refresh();
	fSelectCategory("Button");

	%form = %d.profileForm;
	$fRow = %form.row["imageAsset"];
	fCheck("imageAsset row exists", isObject($fRow));
	fCheck("imageAsset row is an asset row", $fRow.kind $= "asset");
	fCheck("imageAsset row has a Find button", isObject($fRow.findButton));
	fCheck("bitmap row still a file row", %form.row["bitmap"].kind $= "file");
	fCheck("bitmap row still has its Find button", isObject(%form.row["bitmap"].findButton));

	// The two rows must not have collided over the shared helper.
	fCheck("the two Find buttons are different objects",
		$fRow.findButton != %form.row["bitmap"].findButton);

	schedule(400, 0, "fStep2");
}

//-----------------------------------------------------------------------------
// Opening the picker.
//-----------------------------------------------------------------------------

function fStep2()
{
	$fExpected = fExpectedImageCount();
	fCheck("the project has image assets to pick from (" @ $fExpected @ ")", $fExpected > 0);

	// Stop here rather than run on. With an empty database every count below
	// compares nought to nought and passes without testing anything, which reads
	// as a green run when nothing was exercised at all.
	if($fExpected == 0)
	{
		echo("APSMOKE ABORT: no image assets, the rest of the run would prove nothing");
		schedule(300, 0, "quit");
		return;
	}

	// Captured with nothing pushed, so the leak checks later have a real floor.
	$fCanvasBefore = Canvas.getCount();

	$fRow.onFindAssetClicked();

	%picker = fPicker();
	fCheck("picker opened", isObject(%picker));
	fCheck("picker knows its asset type", %picker.assetType $= "ImageAsset");
	fCheck("picker built one cell per image asset",
		%picker.grid.getCount() == $fExpected);
	fCheck("picker counted them the same way", %picker.totalCount == $fExpected);
	fCheck("every cell visible before filtering", fVisibleCount(%picker) == $fExpected);
	fCheck("count label agrees", %picker.countLabel.getText() $= ($fExpected SPC "of" SPC $fExpected));
	fCheck("Choose disabled with nothing chosen", !%picker.chooseButton.isActive());

	// The title is built from the class-style type name, so it needs the internal
	// capital broken out and the article chosen to match.
	fCheck("picker titled for what it is picking", %picker.dialogText $= "Choose an Image Asset");
	fCheck("title takes 'a' before a consonant",
		EditorCore.assetTypeTitle("FontAsset") $= "Choose a Font Asset");
	fCheck("title takes 'an' before a vowel",
		EditorCore.assetTypeTitle("AudioAsset") $= "Choose an Audio Asset");

	// The cells must actually carry thumbnails, not just tooltips.
	%first = %picker.grid.getObject(0);
	fCheck("a cell carries a thumbnail", %first.getCount() > 0);
	fCheck("a cell knows its asset", %first.assetId !$= "");
	fCheck("a cell has a lowercased search key", %first.searchKey $= strlwr(%first.assetId));

	// The shot has to wait a beat: screenShot grabs the frame that has already
	// been drawn, so taking it here would capture the canvas as it was before
	// the picker was pushed.
	schedule(400, 0, "fShotOpen");
}

function fShotOpen()
{
	screenShot(testRoot("shots/assetPickerOpen.png"), "PNG");
	schedule(200, 0, "fStep3");
}

//-----------------------------------------------------------------------------
// Filtering, and the grid re-flow it depends on.
//-----------------------------------------------------------------------------

function fStep3()
{
	%picker = fPicker();
	$fFullHeight = getWord(%picker.grid.getExtent(), 1);
	fCheck("unfiltered grid is taller than one row", $fFullHeight > 60);

	// Search an asset's whole id. Not asserting exactly one survivor: with 90
	// assets one id is easily a prefix of another ("..._1" inside "..._11"), and
	// what matters is that the target survived and the list actually narrowed.
	$fTarget = %picker.grid.getObject(0).assetId;
	%picker.searchBox.setText($fTarget);
	%picker.onSearchChanged();
	%narrowed = fVisibleCount(%picker);

	fCheck("the searched asset survived", %picker.grid.getObject(0).isVisible());
	fCheck("searching a full asset id narrowed the list (" @ %narrowed @ " of " @ $fExpected @ ")",
		%narrowed >= 1 && %narrowed < $fExpected);
	fCheck("count label agrees with the filter",
		%picker.countLabel.getText() $= (%narrowed SPC "of" SPC $fExpected));

	// The whole point of the re-flow: a grid that ignored setVisible would keep
	// its full height and leave the hidden cells' holes behind.
	%narrowHeight = getWord(%picker.grid.getExtent(), 1);
	fCheck("grid re-flowed to fit the filtered cells (" @ $fFullHeight @ " -> " @ %narrowHeight @ ")",
		%narrowHeight < $fFullHeight);
	fCheck("the surviving cell moved up to the first row",
		getWord(%picker.grid.getObject(0).getPosition(), 1) < 60);

	schedule(400, 0, "fShotFiltered");
}

function fShotFiltered()
{
	screenShot(testRoot("shots/assetPickerFiltered.png"), "PNG");
	schedule(200, 0, "fStep3b");
}

function fStep3b()
{
	%picker = fPicker();

	// Matching must ignore case and match anywhere in the id, not just its start.
	%picker.searchBox.setText(strupr(getSubStr($fTarget, 12, 5)));
	%picker.onSearchChanged();
	fCheck("substring search is case-insensitive", fVisibleCount(%picker) >= 1);

	// Nothing matches: empty, not broken.
	%picker.searchBox.setText("zzzznosuchassetzzzz");
	%picker.onSearchChanged();
	fCheck("gibberish hides everything", fVisibleCount(%picker) == 0);
	fCheck("count label shows none", %picker.countLabel.getText() $= ("0 of" SPC $fExpected));

	// Clearing puts them all back.
	%picker.searchBox.setText("");
	%picker.onSearchChanged();
	fCheck("clearing the box restores every cell", fVisibleCount(%picker) == $fExpected);
	fCheck("grid grew back", getWord(%picker.grid.getExtent(), 1) == $fFullHeight);

	schedule(400, 0, "fStep4");
}

//-----------------------------------------------------------------------------
// Selecting and choosing.
//-----------------------------------------------------------------------------

function fStep4()
{
	%picker = fPicker();
	%item = %picker.grid.getObject(0);

	// Go through the button's own callback, so the item-to-dialog wiring is
	// what is being tested and not just the dialog's half of it.
	%item.onClick();
	fCheck("clicking a cell selects it", %picker.selectedAsset $= %item.assetId);
	fCheck("the chosen cell is remembered", %picker.chosenItem == %item);
	fCheck("Choose became available", %picker.chooseButton.isActive());
	fCheck("detail line names the asset",
		strstr(%picker.detailLabel.getText(), %item.assetId) != -1);
	fCheck("detail line reports the frames",
		strstr(%picker.detailLabel.getText(), "frame") != -1);
	fCheck("the picker holds exactly one asset reference", %picker.heldAsset $= %item.assetId);

	// Selecting a second cell must let the first one go.
	if(%picker.grid.getCount() > 1)
	{
		%second = %picker.grid.getObject(1);
		%second.onClick();
		fCheck("selecting another cell moves the reference", %picker.heldAsset $= %second.assetId);
		%item.onClick();
	}

	$fChosen = %picker.selectedAsset;
	%picker.onDone();

	fCheck("choosing wrote the id into the row", $fRow.editor.getText() $= $fChosen);
	fCheck("choosing wrote the id into the profile",
		GuiEditor.profileEditorDialog.profileForm.target.imageAsset $= $fChosen);
	fCheck("the edit was recorded as a theme override",
		$fTheme.isFieldOverridden(GuiEditor.profileEditorDialog.profileForm.target, "imageAsset"));
	fCheck("the row shows its reset button", $fRow.resetButton.isVisible());
	fCheck("the bitmap field was left alone",
		GuiEditor.profileEditorDialog.profileForm.target.bitmap $= "");

	schedule(400, 0, "fStep5");
}

//-----------------------------------------------------------------------------
// Cancelling, and cleanup.
//-----------------------------------------------------------------------------

function fStep5()
{
	fCheck("the picker left the canvas", !isObject(fPicker()));
	fCheck("no dialog leaked", Canvas.getCount() == $fCanvasBefore);

	// Re-opening should come back to what the field already holds.
	$fRow.onFindAssetClicked();
	%picker = fPicker();
	fCheck("re-opening preselects the current asset", %picker.selectedAsset $= $fChosen);
	fCheck("re-opening enables Choose", %picker.chooseButton.isActive());

	%picker.onClose();
	schedule(300, 0, "fStep6");
}

function fStep6()
{
	fCheck("cancelling closed the picker", !isObject(fPicker()));
	fCheck("cancelling left the field alone", $fRow.editor.getText() $= $fChosen);
	fCheck("cancelling leaked nothing", Canvas.getCount() == $fCanvasBefore);

	// Reset must undo an asset the same as any other field.
	%form = GuiEditor.profileEditorDialog.profileForm;
	%form.onFieldRowReset($fRow);
	fCheck("reset cleared the asset override",
		!$fTheme.isFieldOverridden(%form.target, "imageAsset"));
	fCheck("reset hid the row's reset button", !$fRow.resetButton.isVisible());

	// A commit that changes nothing must not re-record an override -- text boxes
	// commit on blur, so this is what merely tabbing through the row does.
	$fRow.commit();
	fCheck("a no-op blur is not an edit", !$fTheme.isFieldOverridden(%form.target, "imageAsset"));

	schedule(400, 0, "fStep7");
}

//-----------------------------------------------------------------------------
// The other caller: the Find button on an asset field in the Gui Editor's
// properties pane.
//
// This block used to drive the native GuiInspector, whose GuiInspectorTypeAsset
// baked an EditorCore.openAssetPicker call straight into a "..." button's
// Command. The Gui Editor no longer builds an inspector -- GuiEditorInspectorPane
// replaced it -- so the path under test is now EditorFieldRow's
// "asset" kind, which routes the click through onFindAssetClicked instead of a
// baked-in command string. Same promise, one indirection later.
//
// GuiInspectorTypeAsset itself is still live: editor/AssetAdmin/AssetInspector.cs
// builds a GuiInspector. Nothing covers that one, which is a gap this change
// created rather than closed.
//-----------------------------------------------------------------------------

function fStep7()
{
	// A GuiSpriteCtrl's Image field is a TypeAssetId, which is what gets the
	// Find button. It has to be a Gui control now rather than a Sprite: the
	// pane binds controls, and a scene object was only ever usable here because
	// the old inspector took any SimObject.
	$fSprite = new GuiSpriteCtrl();
	GuiEditor.rootGui.add($fSprite);

	$fPane = GuiEditor.inspectorWindow.pane;
	fCheck("the editor's properties pane is available", isObject($fPane));
	$fPane.bind($fSprite);

	%row = $fPane.row["Image"];
	fCheck("pane built a row for the asset field", isObject(%row));
	fCheck("the asset field got an asset row", isObject(%row) && %row.kind $= "asset");
	fCheck("asset row has a Find button", isObject(%row) && isObject(%row.findButton));

	// Run exactly what a click would run.
	eval(%row.findButton.Command);

	%picker = fPicker();
	fCheck("the Find button opened the picker", isObject(%picker));
	fCheck("the picker opened on the right asset type", %picker.assetType $= "ImageAsset");

	%item = %picker.grid.getObject(0);
	%item.onClick();
	$fInspectorChoice = %picker.selectedAsset;
	%picker.onDone();

	schedule(400, 0, "fStep8");
}

function fStep8()
{
	fCheck("choosing wrote the asset onto the bound control",
		$fSprite.Image $= $fInspectorChoice);
	fCheck("the pane's picker closed", !isObject(fPicker()));

	$fPane.unbind();
	$fSprite.delete();

	// Land on a border node before quitting. Quitting with a profile node
	// selected trips a teardown crash that predates this work -- the preview's
	// sample controls still wear the theme's member profiles when the modules
	// unload and delete them (see the GuiControl profile-lifetime problem).
	%d = GuiEditor.profileEditorDialog;
	%bname = getWord($fTheme.getBorderCategoryNames(), 0);
	%d.onTreeSelect(new ScriptObject(){ kind = "border"; theme = $fTheme; category = %bname; treeLabel = %bname; });

	echo("APSMOKE DONE");
	schedule(300, 0, "quit");
}
