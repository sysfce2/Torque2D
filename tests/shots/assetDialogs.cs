// Visual harness for the Asset Manager's three dialogs. Four shots:
//
//   0  Duplicate, as it opens
//   1  Duplicate, showing its longest refusal message -- the one that grows the
//      feedback line and used to push the buttons out of sight
//   2  Frame Range, with its answer line filled in
//   3  Unsaved Assets, the guard that stands in front of Close Project and Exit
//
// All three lay their buttons out from the room the CONTENT has rather than from
// the dialog's own height, and the difference between those two is the 34 pixels
// the title bar and border take. Getting it wrong is invisible until something
// reaches the bottom of the dialog, which is exactly what these shots check.
//
// Run: tests/run.ps1 -Shots assetDialogs ; look in shots/.

setLogMode(1);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

testExec("editor/main.cs");
schedule(2500, 0, "aqOpenProject");

function aqOpenProject()
{
	ProjectManager.setProjectFolder("PlanetX");
	EditorCore.projectSelector.onProjectSelected(pathConcat(getMainDotCsDir(), "PlanetX"));

	createPath(testRoot("shots/"));
	EditorPreferences.path = testRoot("shots/assetDialogsShotPrefs.taml");

	schedule(2500, 0, "aqOpenEditor");
}

function aqOpenEditor()
{
	EditorCore.toggleEditor();
	EditorCore.tabBook.selectPage(2);
	schedule(1500, 0, "aqLoadAssets");
}

// Registered after the editor has opened: the project selector calls
// ModuleDatabase.clearDatabase(), which would take this module with it.
function aqLoadAssets()
{
	ModuleDatabase.scanModules(testRoot("toybox/ToyAssets"));
	%module = ModuleDatabase.findModule("ToyAssets", 1);
	if(isObject(%module))
	{
		AssetDatabase.addModuleDeclaredAssets(%module);
	}
	AssetAdmin.libWindow.loadAssets();

	AssetAdmin.Dictionary["ImageAsset"].setExpanded(true);
	AssetAdmin.libWindow.relayout();

	schedule(1000, 0, "aqSelectImage");
}

function aqSelectImage()
{
	%tile = AssetAdmin.Dictionary["ImageAsset"].getButton("ToyAssets:Gems");
	if(isObject(%tile))
	{
		%tile.onClick();
	}

	schedule(800, 0, "aqDuplicateShot");
}

function aqGrab(%name)
{
	// screenShot does not create its folder and reports failure by logging.
	createPath(testRoot("shots/"));
	screenShot(testRoot("shots/assetDialogs" @ %name @ ".png"), "PNG");
}

function aqDuplicateShot()
{
	AssetAdmin.inspector.DuplicateAsset();
	schedule(600, 0, "aqDuplicateGrab");
}

function aqDuplicateGrab()
{
	aqGrab(0);

	// The longest thing the dialog ever says, forced directly so the shot does
	// not depend on finding a library module to be refused by.
	$aqDialog = Canvas.getObject(Canvas.getCount() - 1);
	$aqDialog.feedback.setText("You cannot add assets to a library module. Updates to the module would remove your assets. Instead, copy this asset into your own module.");

	schedule(600, 0, "aqDuplicateLongGrab");
}

function aqDuplicateLongGrab()
{
	aqGrab(1);

	$aqDialog.onClose();
	schedule(600, 0, "aqSelectAnimation");
}

//-----------------------------------------------------------------------------
// The Frame Range dialog, which needs an animation open for the stage to exist.
//-----------------------------------------------------------------------------

function aqSelectAnimation()
{
	AssetAdmin.Dictionary["AnimationAsset"].setExpanded(true);
	AssetAdmin.libWindow.relayout();

	%tile = AssetAdmin.Dictionary["AnimationAsset"].getButton("ToyAssets:TD_Knight_MoveSouth");
	if(isObject(%tile))
	{
		%tile.onClick();
	}

	schedule(1200, 0, "aqRangeShot");
}

function aqRangeShot()
{
	AssetAdmin.animationStage.openRangeDialog();
	schedule(800, 0, "aqRangeGrab");
}

function aqRangeGrab()
{
	aqGrab(2);

	$aqDialog = Canvas.getObject(Canvas.getCount() - 1);
	$aqDialog.onClose();

	schedule(600, 0, "aqGuardShot");
}

//-----------------------------------------------------------------------------
// The unsaved-assets guard. Reached in the editor from Torque2D -> Close Project
// or Torque2D -> Exit; raised here through the same call those two make.
//-----------------------------------------------------------------------------

function aqGuardShot()
{
	AssetDatabase.setAssetDirty("ToyAssets:Gems", true);
	AssetDatabase.setAssetDirty("ToyAssets:Blocks", true);

	EditorCore.guardedCommand("echo(\"the guarded command ran\");");
	schedule(800, 0, "aqGuardGrab");
}

function aqGuardGrab()
{
	aqGrab(3);

	// Cancel rather than Discard: nothing here should go on to run the command.
	%dialog = Canvas.getObject(Canvas.getCount() - 1);
	if(isObject(%dialog))
	{
		%dialog.onCancel();
	}

	schedule(600, 0, "quit");
}
