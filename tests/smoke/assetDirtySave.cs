// Asset Manager unsaved-changes smoke test. Drives the whole of what replaced
// "every edit writes the file": an edit marks the asset unsaved and leaves the
// file alone, Save writes it, Revert puts it back, Undo and Redo step through the
// session, and Duplicate branches what is on screen including the unsaved part.
// Run: tests/run.ps1 assetDirtySave  ; grep ADS in tests/logs/.
//
// This is the suite that would catch the old behavior coming back, so most of the
// assertions are about the FILE rather than about the object: the point of the
// change is that editing stopped touching it.
//
// Driven by calling the inspector's own document-bar methods rather than by
// clicking them, for the same reason assetImageInspector does -- where a button
// lands depends on layout arithmetic this file would then be testing.
//
// NOTE: a COPY of toybox/ToyAssets, never the module itself. Saving genuinely
// writes, so aimed at the repository copy this test would rewrite tracked
// content. The copy goes inside the throwaway project folder, which
// tests/run.ps1 deletes before every run.
//
// NOTE: EditorPreferences writes to the tester's real per-user application data
// folder. Step 1 redirects it for the duration, as assetLibrary does.

setLogMode(1);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

function adsCheck(%label, %cond)
{
	if(%cond) echo("ADS PASS: " @ %label);
	else      echo("ADS FAIL: " @ %label);
}

// Gems is 512 x 512 cut into 8 x 8 cells of 64.
$adsAssetId = "AdsFixture:Gems";
$adsAnimationId = "AdsFixture:TD_Knight_MoveSouth";

// The whole asset file as one string, which is how "did the file change?" is
// asked below. These files are a few hundred bytes; there is no need to be
// cleverer than reading them.
function adsReadAssetFile(%assetId)
{
	%path = AssetDatabase.getAssetFilePath(%assetId);

	%file = new FileObject();
	if(!%file.openForRead(%path))
	{
		%file.delete();
		return "";
	}

	%contents = "";
	while(!%file.isEOF())
	{
		%contents = %contents @ %file.readLine() @ "\n";
	}

	%file.close();
	%file.delete();

	return %contents;
}

// The fixture is a COPY of ToyAssets re-badged under a module id of its own.
//
// The rename is not tidiness. The editor scans the whole of toybox/ on the way
// up, which registers the real ToyAssets/1 -- so a fixture that kept that id is
// a second module claiming it, and which of the two wins depends on the order
// the two scans happen to finish in. That made this suite fail differently on
// every run, and left assets pointing at a module definition that had been
// superseded.
function adsWriteFixtureModule(%path)
{
	%file = new FileObject();
	if(!%file.openForWrite(%path))
	{
		%file.delete();
		return false;
	}

	%file.writeLine("<ModuleDefinition");
	%file.writeLine("	ModuleId=\"AdsFixture\"");
	%file.writeLine("	VersionId=\"1\"");
	%file.writeLine("	BuildId=\"1\"");
	%file.writeLine("	Description=\"Throwaway asset fixture for the assetDirtySave smoke test.\">");
	%file.writeLine("		<DeclaredAssets");
	%file.writeLine("			Path=\"assets\"");
	%file.writeLine("			Extension=\"asset.taml\"");
	%file.writeLine("			Recurse=\"true\"/>");
	%file.writeLine("</ModuleDefinition>");

	%file.close();
	%file.delete();

	return true;
}

function adsLoadFixtureAssets()
{
	%copy = testRoot("assetDirtySaveSmokeProject/AdsFixture");

	if(!pathCopy(testRoot("toybox/ToyAssets"), %copy, false))
	{
		return false;
	}

	if(!adsWriteFixtureModule(%copy @ "/1/module.taml"))
	{
		return false;
	}

	ModuleDatabase.scanModules(%copy);
	%module = ModuleDatabase.findModule("AdsFixture", 1);
	if(!isObject(%module))
	{
		return false;
	}
	AssetDatabase.addModuleDeclaredAssets(%module);
	return true;
}

testExec("editor/main.cs");
schedule(2000, 0, "adsStep1");

//-----------------------------------------------------------------------------
// Opening the Asset Manager on an image asset.
//-----------------------------------------------------------------------------

function adsStep1()
{
	createPath(testRoot("shots/"));

	// Spelled out rather than held in a variable: tests/run.ps1 finds the folder
	// to delete by reading this file for setProjectFolder("..."), so a name it
	// cannot see is a folder it cannot sweep.
	ProjectManager.setProjectFolder("assetDirtySaveSmokeProject");
	EditorPreferences.path = testRoot("shots/assetDirtySaveSmokePrefs.taml");

	// Pages register in load order: EditorConsole, ProjectManager, AssetAdmin,
	// GuiEditor. Selecting the tab is what calls AssetAdmin::open.
	EditorCore.tabBook.selectPage(2);

	schedule(800, 0, "adsStep1b");
}

// The fixture is registered HERE, after the editor has settled, and not before
// it.
//
// EditorProjectSelector calls ModuleDatabase.clearDatabase() when it picks up a
// project, and that deletes every ModuleDefinition object and rescans. Assets
// declared before it survive in the asset manager -- their AssetDefinitions are
// untouched -- but the ModuleDefinition each one points at has been freed. A
// fixture registered ahead of that wipe therefore has assets that work and a
// module that is gone, which is not a state the editor ever puts a real project
// in, and not one worth writing a test around.
function adsStep1b()
{
	adsCheck("fixture asset module registered", adsLoadFixtureAssets());

	// The library was built before the fixture existed, so it has to be told.
	AssetAdmin.libWindow.loadAssets();

	schedule(600, 0, "adsStep2");
}

function adsStep2()
{
	$adsInspector = AssetAdmin.inspector;
	$adsTile = AssetAdmin.Dictionary["ImageAsset"].getButton($adsAssetId);

	adsCheck("the fixture gave the library " @ $adsAssetId, isObject($adsTile));

	if(!isObject($adsTile))
	{
		echo("ADS ABORT: no fixture asset, the rest of the run would prove nothing");
		schedule(300, 0, "quit");
		return;
	}

	$adsTile.onClick();
	schedule(400, 0, "adsStep3");
}

//-----------------------------------------------------------------------------
// A freshly opened asset is clean, and the document bar says so.
//-----------------------------------------------------------------------------

function adsStep3()
{
	$adsAsset = AssetDatabase.acquireAsset($adsAssetId);

	adsCheck("asset loaded", isObject($adsAsset));
	adsCheck("a freshly opened asset has nothing unsaved", !$adsAsset.isAssetDirty());
	adsCheck("and the database agrees", !AssetDatabase.isAssetDirty($adsAssetId));
	adsCheck("nothing in the project is unsaved yet", AssetDatabase.getDirtyAssetCount() == 0);

	adsCheck("the document bar is on show", $adsInspector.documentButtonBar.isVisible());
	adsCheck("Save is greyed with nothing to save", !$adsInspector.getSaveAssetEnabled());
	adsCheck("Revert is greyed with nothing to put back", !$adsInspector.getRevertAssetEnabled());
	adsCheck("Undo is greyed with nothing done yet", !$adsInspector.getUndoAssetEnabled());
	adsCheck("Redo is greyed with nothing undone yet", !$adsInspector.getRedoAssetEnabled());

	// The File and Edit menus offer the same five commands as the bar beside them
	// and answer to the same predicates. Held by handle, never looked up by text:
	// Undo and Redo carry the step label in their text and it changes underneath.
	$adsMenus = AssetAdmin.menus;
	adsCheck("the Asset Manager has a menu set", isObject($adsMenus));
	adsCheck("menu Save is greyed too", !$adsMenus.save.Active);
	adsCheck("menu Revert is greyed too", !$adsMenus.revert.Active);
	adsCheck("menu Undo is greyed too", !$adsMenus.undo.Active);
	adsCheck("menu Redo is greyed too", !$adsMenus.redo.Active);
	adsCheck("Save All is greyed with nothing unsaved anywhere", !$adsMenus.saveAll.Active);
	adsCheck("Duplicate is offered, because there is a document", $adsMenus.duplicate.Active);

	adsCheck("the tile is not marked", !$adsTile.dirtyMark.isVisible());

	// Everything below compares against this.
	$adsFileAtStart = adsReadAssetFile($adsAssetId);
	$adsCellWidthAtStart = $adsAsset.getCellWidth();

	adsCheck("read the asset file (" @ strlen($adsFileAtStart) @ " bytes)", strlen($adsFileAtStart) > 0);

	schedule(200, 0, "adsStep4");
}

//-----------------------------------------------------------------------------
// The change this whole feature exists for: an edit does NOT write the file.
//-----------------------------------------------------------------------------

function adsStep4()
{
	$adsAsset.setCellWidth(32);

	adsCheck("the edit reached the asset", $adsAsset.getCellWidth() == 32);
	adsCheck("the asset now has unsaved changes", $adsAsset.isAssetDirty());
	adsCheck("and it is counted", AssetDatabase.getDirtyAssetCount() == 1);

	// The point of the exercise.
	adsCheck("the file on disk was NOT touched", adsReadAssetFile($adsAssetId) $= $adsFileAtStart);

	adsCheck("Save is offered now", $adsInspector.getSaveAssetEnabled());
	adsCheck("Revert is offered now", $adsInspector.getRevertAssetEnabled());
	adsCheck("and the menu followed", $adsMenus.save.Active && $adsMenus.revert.Active);
	adsCheck("Save All woke up with it", $adsMenus.saveAll.Active);

	// The step label is the one thing the menu says that the bar only whispers in
	// a tooltip.
	adsCheck("menu Undo names the step (" @ $adsMenus.undo.getText() @ ")",
		$adsMenus.undo.Active && strstr($adsMenus.undo.getText(), "Undo") == 0);
	adsCheck("the tile is marked unsaved", $adsTile.dirtyMark.isVisible());
	adsCheck("the mark is a control of its own, not part of the caption",
		$adsTile.caption.getText() $= $adsTile.assetName);
	adsCheck("so the name it sorts and searches by is untouched",
		$adsTile.assetName !$= "" && strstr($adsTile.assetName, "*") == -1);
	adsCheck("and the mark is square",
		getWord($adsTile.dirtyMark.getExtent(), 0) == getWord($adsTile.dirtyMark.getExtent(), 1));

	// An edit is an undo step.
	adsCheck("the edit became an undo step",
		AssetAdmin.undoRecorder.getUndoCount($adsAssetId) == 1);
	adsCheck("and nothing is waiting to be redone",
		AssetAdmin.undoRecorder.getRedoCount($adsAssetId) == 0);

	schedule(200, 0, "adsStep5");
}

//-----------------------------------------------------------------------------
// Undo and redo.
//-----------------------------------------------------------------------------

function adsStep5()
{
	AssetAdmin.undoRecorder.undo($adsAsset);

	adsCheck("undo put the value back (" @ $adsAsset.getCellWidth() @ ")",
		$adsAsset.getCellWidth() == $adsCellWidthAtStart);
	adsCheck("undoing back to the saved state leaves nothing unsaved",
		!$adsAsset.isAssetDirty());
	adsCheck("the tile mark went with it", !$adsTile.dirtyMark.isVisible());
	adsCheck("and there is something to redo",
		AssetAdmin.undoRecorder.getRedoCount($adsAssetId) == 1);

	AssetAdmin.undoRecorder.redo($adsAsset);

	adsCheck("redo brought the value back (" @ $adsAsset.getCellWidth() @ ")",
		$adsAsset.getCellWidth() == 32);
	adsCheck("and the asset is unsaved again", $adsAsset.isAssetDirty());
	adsCheck("the file is still untouched through all of that",
		adsReadAssetFile($adsAssetId) $= $adsFileAtStart);

	schedule(200, 0, "adsStep6");
}

//-----------------------------------------------------------------------------
// A second edit, so there is a stack rather than a single step -- and so the
// redo stack is seen to be dropped when the future is rewritten.
//-----------------------------------------------------------------------------

function adsStep6()
{
	$adsAsset.setCellHeight(16);

	adsCheck("two edits, two steps", AssetAdmin.undoRecorder.getUndoCount($adsAssetId) == 2);

	AssetAdmin.undoRecorder.undo($adsAsset);
	adsCheck("undo took the second edit back (" @ $adsAsset.getCellHeight() @ ")",
		$adsAsset.getCellHeight() != 16);
	adsCheck("the first edit is still in place", $adsAsset.getCellWidth() == 32);
	adsCheck("still unsaved, because this is not where it was saved",
		$adsAsset.isAssetDirty());

	// Making a change after an undo is what drops the redo stack.
	$adsAsset.setCellHeight(24);
	adsCheck("a new change dropped what was waiting to be redone",
		AssetAdmin.undoRecorder.getRedoCount($adsAssetId) == 0);

	schedule(200, 0, "adsStep7");
}

//-----------------------------------------------------------------------------
// Saving.
//-----------------------------------------------------------------------------

function adsStep7()
{
	$adsInspector.SaveAsset();

	adsCheck("saving cleared the unsaved state", !$adsAsset.isAssetDirty());
	adsCheck("and the count went with it", AssetDatabase.getDirtyAssetCount() == 0);
	adsCheck("the tile mark cleared", !$adsTile.dirtyMark.isVisible());
	adsCheck("Save is greyed again", !$adsInspector.getSaveAssetEnabled());
	adsCheck("and so is the menu's", !$adsMenus.save.Active);
	adsCheck("with nothing unsaved anywhere, Save All went too", !$adsMenus.saveAll.Active);

	$adsFileAfterSave = adsReadAssetFile($adsAssetId);
	adsCheck("the file changed this time", $adsFileAfterSave !$= $adsFileAtStart);
	adsCheck("and it holds the edited value",
		strstr($adsFileAfterSave, "CellWidth=\"32\"") != -1);

	// The history survives a save -- undo still works, it just means the asset is
	// unsaved again.
	adsCheck("the history survived the save",
		AssetAdmin.undoRecorder.getUndoCount($adsAssetId) > 0);

	AssetAdmin.undoRecorder.undo($adsAsset);
	adsCheck("undoing past the save makes it unsaved again", $adsAsset.isAssetDirty());

	schedule(200, 0, "adsStep8");
}

//-----------------------------------------------------------------------------
// Reverting.
//-----------------------------------------------------------------------------

function adsStep8()
{
	// Somewhere clearly different from the file, so the revert has something to
	// undo.
	$adsAsset.setCellWidth(8);
	adsCheck("moved away from the saved file again", $adsAsset.getCellWidth() == 8);
	adsCheck("which is unsaved", $adsAsset.isAssetDirty());

	$adsInspector.RevertAsset();

	schedule(400, 0, "adsStep9");
}

function adsStep9()
{
	// Reverting reloads the tile, which re-acquires the asset. It is the same
	// object either way -- that is the promise revertAsset makes, since every
	// Sprite in the scene is holding a pointer to it.
	adsCheck("revert went back to what was saved (" @ $adsAsset.getCellWidth() @ ")",
		$adsAsset.getCellWidth() == 32);
	adsCheck("revert cleared the unsaved state", !$adsAsset.isAssetDirty());
	adsCheck("the asset object was kept, not swapped",
		AssetDatabase.acquireAsset($adsAssetId) == $adsAsset);
	AssetDatabase.releaseAsset($adsAssetId);

	adsCheck("the file was left as it was saved",
		adsReadAssetFile($adsAssetId) $= $adsFileAfterSave);

	// A revert throws the document away, so the steps that described it go too.
	adsCheck("revert cleared the undo history",
		AssetAdmin.undoRecorder.getUndoCount($adsAssetId) == 0);

	schedule(200, 0, "adsStep10");
}

//-----------------------------------------------------------------------------
// Duplicating, including the unsaved part.
//-----------------------------------------------------------------------------

function adsStep10()
{
	// An unsaved edit, so the copy can be seen to include it.
	$adsAsset.setCellWidth(64);
	adsCheck("an unsaved edit to copy", $adsAsset.isAssetDirty());

	$adsCopyId = "AdsFixture:GemsCopy";

	// NOTE: deliberately not asserted through AssetDatabase.getAssetModule here.
	// That reads AssetDefinition::mpModuleDefinition, which a module re-scan
	// leaves dangling -- the editor scans the whole repository root on the way up,
	// and this fixture sits inside it. duplicateAsset looks the module up by path
	// for exactly that reason, so what matters is the outcome below.

	%path = pathConcat(AssetDatabase.getAssetPath($adsAssetId), "GemsCopy.asset.taml");
	adsCheck("duplicate reported success",
		AssetDatabase.duplicateAsset($adsAssetId, %path, "GemsCopy"));

	adsCheck("the copy is a declared asset", AssetDatabase.isDeclaredAsset($adsCopyId));

	%copy = AssetDatabase.acquireAsset($adsCopyId);
	adsCheck("the copy loads", isObject(%copy));

	if(isObject(%copy))
	{
		adsCheck("the copy took the UNSAVED value (" @ %copy.getCellWidth() @ ")",
			%copy.getCellWidth() == 64);
		adsCheck("the copy has its own name", %copy.AssetName $= "GemsCopy");
		adsCheck("the copy starts saved", !%copy.isAssetDirty());
		AssetDatabase.releaseAsset($adsCopyId);
	}

	adsCheck("the original is still unsaved", $adsAsset.isAssetDirty());
	adsCheck("and the original's file still has the saved value",
		adsReadAssetFile($adsAssetId) $= $adsFileAfterSave);

	schedule(200, 0, "adsStep11");
}

//-----------------------------------------------------------------------------
// Save All, and the guard that asks before the work is thrown away.
//-----------------------------------------------------------------------------

function adsStep11()
{
	adsCheck("the Asset Manager reports unsaved work", AssetAdmin.hasUnsavedAssets());
	adsCheck("one asset is unsaved", AssetDatabase.getDirtyAssetCount() == 1);

	// A second unsaved asset, to prove Save All is not just Save.
	%other = AssetDatabase.acquireAsset("AdsFixture:Football");
	if(isObject(%other))
	{
		%other.AssetDescription = "changed by the smoke test";
		adsCheck("a second asset is unsaved now", AssetDatabase.getDirtyAssetCount() == 2);

		AssetAdmin.saveAllAssets();

		adsCheck("Save All saved both", AssetDatabase.getDirtyAssetCount() == 0);
		adsCheck("and the Asset Manager stops reporting unsaved work",
			!AssetAdmin.hasUnsavedAssets());
		AssetDatabase.releaseAsset("AdsFixture:Football");
	}
	else
	{
		echo("ADS SKIP: no second fixture asset, Save All checked with one");
		AssetAdmin.saveAllAssets();
		adsCheck("Save All saved the one", AssetDatabase.getDirtyAssetCount() == 0);
	}

	// With nothing unsaved the guard should not interrupt: the command runs.
	$adsGuardRan = false;
	EditorCore.guardedCommand("$adsGuardRan = true;");
	adsCheck("with nothing unsaved the guard runs the command straight through",
		$adsGuardRan);

	schedule(200, 0, "adsStep12");
}

//-----------------------------------------------------------------------------
// The guard when there IS something to lose.
//-----------------------------------------------------------------------------

function adsStep12()
{
	$adsAsset.setCellWidth(128);
	adsCheck("something unsaved again", AssetAdmin.hasUnsavedAssets());

	$adsGuardRan = false;
	EditorCore.guardedCommand("$adsGuardRan = true;");

	adsCheck("the guard held the command back", !$adsGuardRan);

	%dialog = Canvas.getContent().getObject(Canvas.getContent().getCount() - 1);
	schedule(300, 0, "adsStep13");
}

function adsStep13()
{
	// The dialog is pushed onto the Canvas, so it is the Canvas' last child.
	%dialog = Canvas.getObject(Canvas.getCount() - 1);
	adsCheck("a dialog was raised", isObject(%dialog));

	if(isObject(%dialog) && %dialog.getClassNamespace() $= "AssetAdminConfirmSaveDialog")
	{
		adsCheck("it is the unsaved-assets dialog", true);

		// Save All, which writes and then lets the command through.
		%dialog.onSave();
		schedule(400, 0, "adsStep14");
		return;
	}

	adsCheck("it is the unsaved-assets dialog", false);
	schedule(300, 0, "quit");
}

function adsStep14()
{
	adsCheck("answering Save All saved the work", AssetDatabase.getDirtyAssetCount() == 0);
	adsCheck("and let the held command through", $adsGuardRan);

	AssetDatabase.releaseAsset($adsAssetId);
	schedule(200, 0, "adsStep15");
}

//-----------------------------------------------------------------------------
// Particles, which is what this whole change was for.
//
// A particle asset is the one kind whose editing mostly happens in C++ -- the
// graph editor drags data keys around and calls refreshAsset itself on mouse-up,
// and the emitter's scalar fields go through the stock GuiInspector. None of that
// passes through TorqueScript, so none of it could be recorded by a script-side
// recorder. What is asserted here is that it is nonetheless tracked, because the
// tracking hangs off the change notification rather than off the edit.
//
// It also exercises the emitter deep-copy: an undo rebuilds the emitter list from
// the snapshot, so emitters surviving with their values intact is the thing that
// used to be broken in ParticleAsset::copyTo.
//-----------------------------------------------------------------------------

function adsStep15()
{
	$adsParticleId = "AdsFixture:bonfire";

	%tile = AssetAdmin.Dictionary["ParticleAsset"].getButton($adsParticleId);
	adsCheck("the library has the particle asset", isObject(%tile));

	if(!isObject(%tile))
	{
		schedule(300, 0, "quit");
		return;
	}

	%tile.onClick();
	schedule(400, 0, "adsStep16");
}

function adsStep16()
{
	$adsParticle = AssetDatabase.acquireAsset($adsParticleId);

	adsCheck("the particle asset loaded", isObject($adsParticle));
	adsCheck("it has emitters", $adsParticle.getEmitterCount() > 0);
	adsCheck("and starts with nothing unsaved", !$adsParticle.isAssetDirty());

	$adsEmitterCount = $adsParticle.getEmitterCount();
	$adsParticleFileAtStart = adsReadAssetFile($adsParticleId);

	// A field on an EMITTER, which is what the stock inspector writes. The
	// emitter has no file of its own -- it forwards to the asset that owns it.
	%emitter = $adsParticle.getEmitter(0);
	$adsEmitterAngleAtStart = %emitter.EmitterAngle;
	%emitter.EmitterAngle = 45;

	adsCheck("the emitter took the value (" @ %emitter.EmitterAngle @ ")",
		%emitter.EmitterAngle == 45);
	adsCheck("changing an emitter leaves the ASSET unsaved", $adsParticle.isAssetDirty());
	adsCheck("and the particle file was NOT touched",
		adsReadAssetFile($adsParticleId) $= $adsParticleFileAtStart);
	adsCheck("the emitter change became an undo step",
		AssetAdmin.undoRecorder.getUndoCount($adsParticleId) == 1);

	AssetAdmin.undoRecorder.undo($adsParticle);
	schedule(300, 0, "adsStep17");
}

function adsStep17()
{
	adsCheck("undo kept every emitter (" @ $adsParticle.getEmitterCount() @ " of " @ $adsEmitterCount @ ")",
		$adsParticle.getEmitterCount() == $adsEmitterCount);

	// The emitters are rebuilt by the restore, so this is deliberately a fresh
	// handle rather than the one from before.
	%emitter = $adsParticle.getEmitter(0);
	adsCheck("undo put the emitter value back (" @ %emitter.EmitterAngle @ ")",
		%emitter.EmitterAngle == $adsEmitterAngleAtStart);
	adsCheck("the emitter kept its name", %emitter.EmitterName !$= "");
	adsCheck("undoing back to the saved state leaves nothing unsaved",
		!$adsParticle.isAssetDirty());
	adsCheck("the particle file was never written",
		adsReadAssetFile($adsParticleId) $= $adsParticleFileAtStart);

	AssetDatabase.releaseAsset($adsParticleId);
	schedule(200, 0, "adsStep18");
}

//-----------------------------------------------------------------------------
// One thing the user did is one press of undo.
//
// Adding a frame used to take two, because the drop path committed the change
// twice -- insertFrameAtPoint announces itself AND the handler committed again --
// and setAnimationFrames had no "ignore no change" guard, so the second commit
// was recorded as a step that put nothing back.
//-----------------------------------------------------------------------------

function adsStep18()
{
	%tile = AssetAdmin.Dictionary["AnimationAsset"].getButton($adsAnimationId);
	adsCheck("the library has the animation asset", isObject(%tile));

	if(!isObject(%tile))
	{
		schedule(300, 0, "quit");
		return;
	}

	%tile.onClick();
	schedule(500, 0, "adsStep19");
}

function adsStep19()
{
	$adsAnimation = AssetDatabase.acquireAsset($adsAnimationId);
	%stage = AssetAdmin.animationStage;

	adsCheck("the animation asset loaded", isObject($adsAnimation));
	adsCheck("the animation stage is built", %stage.built);

	if(!isObject($adsAnimation) || !%stage.built)
	{
		schedule(300, 0, "quit");
		return;
	}

	$adsFramesAtStart = $adsAnimation.getAnimationFrames();

	// The palette-click path: append one frame.
	%stage.appendFrame(0);

	adsCheck("adding a frame is exactly one undo step (" @ AssetAdmin.undoRecorder.getUndoCount($adsAnimationId) @ ")",
		AssetAdmin.undoRecorder.getUndoCount($adsAnimationId) == 1);

	// Writing the same list back is not a change, so it must not become a step.
	%stage.commitFrames($adsAnimation.getAnimationFrames());

	adsCheck("committing an unchanged frame list adds no step (" @ AssetAdmin.undoRecorder.getUndoCount($adsAnimationId) @ ")",
		AssetAdmin.undoRecorder.getUndoCount($adsAnimationId) == 1);

	// And one press of undo puts the frame back.
	AssetAdmin.undoRecorder.undo($adsAnimation);
	schedule(300, 0, "adsStep20");
}

function adsStep20()
{
	adsCheck("one undo removed the frame (" @ $adsAnimation.getAnimationFrames() @ ")",
		$adsAnimation.getAnimationFrames() $= $adsFramesAtStart);
	adsCheck("and there is nothing further to undo",
		AssetAdmin.undoRecorder.getUndoCount($adsAnimationId) == 0);

	// A restore copies the snapshot's DYNAMIC fields onto the live asset too, so
	// anything the recorder wrote on a snapshot would end up in the asset's file
	// the next time it was saved. Real content files grew stepLabel="Set Frames"
	// wasDirty="1" this way.
	adsCheck("undo left no recorder bookkeeping on the asset",
		$adsAnimation.stepLabel $= "" && $adsAnimation.wasDirty $= "");
	adsCheck("with exactly one thing to redo",
		AssetAdmin.undoRecorder.getRedoCount($adsAnimationId) == 1);

	AssetAdmin.undoRecorder.redo($adsAnimation);
	schedule(300, 0, "adsStep21");
}

function adsStep21()
{
	adsCheck("one redo put the frame back (" @ $adsAnimation.getAnimationFrames() @ ")",
		$adsAnimation.getAnimationFrames() !$= $adsFramesAtStart);
	adsCheck("and nothing further to redo",
		AssetAdmin.undoRecorder.getRedoCount($adsAnimationId) == 0);

	AssetDatabase.releaseAsset($adsAnimationId);
	schedule(400, 0, "quit");
}
