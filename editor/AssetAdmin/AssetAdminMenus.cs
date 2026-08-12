//-----------------------------------------------------------------------------
// The Asset Manager's two menus: File and Edit.
//
// They are called File and Edit and they say nothing the Gui Editor's File and
// Edit say, which is the whole arrangement: EditorCore swaps whole sets in and
// out, so only one editor's menus are ever on the bar and both are free to mean
// what their own editor means. Ctrl+S saves the asset here and the Gui there,
// and neither has to know the other exists. See EditorMenuSet.
//
// There is no Asset menu, because every command in this editor is about an
// asset - an Asset menu would be the whole menu bar.
//
// Everything below already existed as a command on the inspector's document bar
// or on AssetAdmin, with the same predicates. This is a second way in, not new
// behavior.
//-----------------------------------------------------------------------------

function AssetAdminMenus::onAdd(%this)
{
	%this.init();
}

function AssetAdminMenus::build(%this)
{
	%file = %this.addMenu("File");

	// A submenu rather than five items, and no accelerator on any of them: there
	// is no single "new asset" here for Ctrl+N to mean, and picking a favorite of
	// the five would be arbitrary.
	%new = %file.addSubMenu("New Asset");
	%new.addItem("Image Asset...", "AssetAdmin.newImageAsset();");
	%new.addItem("Animation Asset...", "AssetAdmin.newAnimationAsset();");
	%new.addItem("Particle Asset...", "AssetAdmin.newParticleAsset();");
	%new.addItem("Bitmap Font Asset...", "AssetAdmin.newFontAsset();");
	%new.addItem("Audio Asset...", "AssetAdmin.newAudioAsset();");
	%file.addSeparator();

	%this.save = %file.addItem("Save Asset", "AssetAdmin.inspector.SaveAsset();", "Ctrl S");
	%this.saveAll = %file.addItem("Save All Assets", "AssetAdmin.saveAllAssets();", "Ctrl-Shift S");
	%file.addSeparator();

	// No accelerator, for the reason the Gui Editor's Revert has none: it throws
	// away everything since the last save and cannot be taken back.
	%this.revert = %file.addItem("Revert Asset", "AssetAdmin.inspector.RevertAsset();");

	%edit = %this.addMenu("Edit");

	// These two carry the step label - "Undo Move Frame" - the way the document
	// bar's tooltips already do, so their text is rewritten on every refresh.
	// Anything looking for them must hold the handle rather than search by text.
	%this.undo = %edit.addItem("Undo", "AssetAdmin.inspector.UndoAsset();", "Ctrl Z");
	%this.redo = %edit.addItem("Redo", "AssetAdmin.inspector.RedoAsset();", "Ctrl-Shift Z");
	%edit.addSeparator();
	%this.duplicate = %edit.addItem("Duplicate Asset...", "AssetAdmin.inspector.DuplicateAsset();", "Ctrl D");
	%edit.addSeparator();

	// No accelerator: this offers to take the asset's files off disk with it.
	%this.deleteAsset = %edit.addItem("Delete Asset...", "AssetAdmin.inspector.deleteAsset();");
}

// Called when the set goes back on the bar, and by AssetInspector on every
// change to the document - which is often, so this stays down to reading the
// same predicates the document bar's buttons read and writing a flag each.
function AssetAdminMenus::refresh(%this)
{
	%inspector = %this.tool.inspector;
	%hasDocument = isObject(%inspector.documentAsset());

	%this.save.setActive(%inspector.getSaveAssetEnabled());
	%this.saveAll.setActive(%this.tool.hasUnsavedAssets());
	%this.revert.setActive(%inspector.getRevertAssetEnabled());

	%this.undo.setActive(%inspector.getUndoAssetEnabled());
	%this.redo.setActive(%inspector.getRedoAssetEnabled());
	%this.duplicate.setActive(%hasDocument);
	%this.deleteAsset.setActive(%hasDocument);

	// The same text the buttons put in their tooltips. setText goes through the
	// bar's own update, and a dropdown re-measures itself every time it opens, so
	// a longer label is not clipped.
	%this.undo.setText(%inspector.getUndoAssetTooltip());
	%this.redo.setText(%inspector.getRedoAssetTooltip());
}
