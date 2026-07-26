
//-----------------------------------------------------------------------------
// A small confirmation dialog used by the Gui Profile Editor. The spawner
// sets dialogText, message, confirmText, callbackTarget, and callbackMethod;
// the confirm button calls callbackTarget.callbackMethod().
//-----------------------------------------------------------------------------

function GuiProfileEditorConfirmDialog::init(%this, %width, %height)
{
	%window = %this.getObject(0);
	%content = %window.getObject(0);

	// Everything above the buttons, rather than a fixed height: the messages
	// differ in length by a factor of three, and the spawner sizes the dialog to
	// the one it is about to show (see openConfirmDialog). A fixed box silently
	// clipped the last line of the longer ones.
	%this.feedback = new GuiControl()
	{
		HorizSizing = "right";
		VertSizing = "bottom";
		Position = "12 12";
		Extent = (%width - 24) SPC (%height - 86);
		text = %this.message;
		textWrap = true;
	};
	ThemeManager.setProfile(%this.feedback, "infoProfile");
	%content.add(%this.feedback);

	%this.cancelButton = new GuiButtonCtrl()
	{
		HorizSizing = "right";
		VertSizing = "bottom";
		Position = (%width - 226) SPC (%height - 62);
		Extent = "100 30";
		Text = "Cancel";
		Command = %this.getID() @ ".onClose();";
	};
	ThemeManager.setProfile(%this.cancelButton, "buttonProfile");
	%content.add(%this.cancelButton);

	%this.confirmButton = new GuiButtonCtrl()
	{
		HorizSizing = "right";
		VertSizing = "bottom";
		Position = (%width - 116) SPC (%height - 64);
		Extent = "100 34";
		Text = %this.confirmText;
		Command = %this.getID() @ ".onConfirm();";
	};
	ThemeManager.setProfile(%this.confirmButton, "primaryButtonProfile");
	%content.add(%this.confirmButton);
}

function GuiProfileEditorConfirmDialog::onConfirm(%this)
{
	%this.callbackTarget.call(%this.callbackMethod);
	%this.onClose();
}

// This dialog sits on top of the profile editor dialog, so it must not use
// the shared EditorCore.dialog delete slot - closing both within the
// scheduled delay would leak one of them. The parent object deletes it after
// a pause; scheduling "delete" on the dialog itself would fire inside its
// own script-callback guard and assert.
function GuiProfileEditorConfirmDialog::onClose(%this)
{
	Canvas.popDialog(%this);
	EditorCore.schedule(100, "deleteDialogObject", %this);
}
