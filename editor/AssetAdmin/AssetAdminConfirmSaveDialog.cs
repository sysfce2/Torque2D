//-----------------------------------------------------------------------------
// What stands between unsaved assets and the two commands that would discard
// them: Close Project and Exit.
//
// Modelled on GuiEditorConfirmSaveDialog, and the same three answers, because the
// question is the same one: the third option people actually want is "save it,
// then carry on with what I asked for", and making them Cancel, save by hand and
// repeat themselves is not really an option at all.
//
// Where it differs is that this is about a set rather than a document. There is
// no Save As to go wrong, so Save All is unconditional and the interrupted
// command resumes immediately after it.
//
// It owns none of the decision. AssetAdmin holds the command that was
// interrupted; each button here only says which way to go. See
// AssetAdmin::guardAssets.
//-----------------------------------------------------------------------------

function AssetAdminConfirmSaveDialog::init(%this, %width, %height)
{
	%window = %this.getObject(0);
	%content = %window.getObject(0);

	%this.feedback = new GuiControl()
	{
		HorizSizing = "width";
		VertSizing = "anchorTop";
		Position = "12 12";
		Extent = (%width - 24) SPC 64;
		text = %this.message;
		textWrap = true;
		textExtend = true;
	};
	ThemeManager.setProfile(%this.feedback, "infoProfile");
	%content.add(%this.feedback);

	// Measured from the room the content actually has, not from the dialog's own
	// height -- the title bar and border take 34 of it.
	%bottom = %this.contentHeight() - 12;

	// Right to left in the order they escalate: abandon what you asked for, go
	// through with it, or write the files first.
	//
	// The middle one is wider than the other two because "Discard All" does not
	// fit in the 100 the others use -- it came out as "Discard A".
	%this.cancelButton = new GuiButtonCtrl()
	{
		HorizSizing = "anchorRight";
		VertSizing = "anchorBottom";
		Position = (%width - 356) SPC (%bottom - 32);
		Extent = "100 30";
		Text = "Cancel";
		Command = %this.getID() @ ".onCancel();";
	};
	ThemeManager.setProfile(%this.cancelButton, "buttonProfile");
	%content.add(%this.cancelButton);

	%this.discardButton = new GuiButtonCtrl()
	{
		HorizSizing = "anchorRight";
		VertSizing = "anchorBottom";
		Position = (%width - 246) SPC (%bottom - 32);
		Extent = "120 30";
		Text = "Discard All";
		Command = %this.getID() @ ".onDiscard();";
	};
	ThemeManager.setProfile(%this.discardButton, "buttonProfile");
	%content.add(%this.discardButton);

	%this.saveButton = new GuiButtonCtrl()
	{
		HorizSizing = "anchorRight";
		VertSizing = "anchorBottom";
		Position = (%width - 116) SPC (%bottom - 34);
		Extent = "100 34";
		Text = "Save All";
		Command = %this.getID() @ ".onSave();";
	};
	ThemeManager.setProfile(%this.saveButton, "primaryButtonProfile");
	%content.add(%this.saveButton);
}

function AssetAdminConfirmSaveDialog::onCancel(%this)
{
	AssetAdmin.dropPendingCommand();
	%this.closeNow();
}

// The assets stay unsaved, and the command goes ahead anyway. It resumes AFTER
// this guard rather than at the top of the chain, or the assets would still be
// unsaved and the same question would be asked forever.
function AssetAdminConfirmSaveDialog::onDiscard(%this)
{
	%this.closeNow();
	AssetAdmin.runPendingCommand();
}

function AssetAdminConfirmSaveDialog::onSave(%this)
{
	%this.closeNow();
	AssetAdmin.saveAllAssets();
	AssetAdmin.runPendingCommand();
}

// The window X, which is the same answer as Cancel: no to the question asked.
function AssetAdminConfirmSaveDialog::onClose(%this)
{
	%this.onCancel();
}

// Not the shared EditorCore.dialog slot: the Gui Editor's own confirm dialog can
// follow this one within the scheduled delay, and the two would race for it.
function AssetAdminConfirmSaveDialog::closeNow(%this)
{
	Canvas.popDialog(%this);
	EditorCore.schedule(100, "deleteDialogObject", %this);
}
