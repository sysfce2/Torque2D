
//-----------------------------------------------------------------------------
// What stands between a modified Gui and the four commands that would discard
// it: New, Open, Close Project and Exit.
//
// Three answers rather than the Profile Editor's two. Its confirm dialog asks
// about themes, which are files the user can put back by reverting; this asks
// about the document they are in the middle of making, and the answer they
// usually want is "save it, then carry on with what I asked for". Making them
// Cancel, press Ctrl+S and repeat themselves is not a real third option.
//
// It owns none of the decision. GuiEditor holds the command that was
// interrupted; each button here only says which way to go. See
// GuiEditor::guardDocument.
//-----------------------------------------------------------------------------

function GuiEditorConfirmSaveDialog::init(%this, %width, %height)
{
	%window = %this.getObject(0);
	%content = %window.getObject(0);

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

	// Right to left in the order they escalate: abandon what you asked for,
	// go through with it, or write the file first.
	%this.cancelButton = new GuiButtonCtrl()
	{
		HorizSizing = "right";
		VertSizing = "bottom";
		Position = (%width - 336) SPC (%height - 62);
		Extent = "100 30";
		Text = "Cancel";
		Command = %this.getID() @ ".onCancel();";
	};
	ThemeManager.setProfile(%this.cancelButton, "buttonProfile");
	%content.add(%this.cancelButton);

	%this.discardButton = new GuiButtonCtrl()
	{
		HorizSizing = "right";
		VertSizing = "bottom";
		Position = (%width - 226) SPC (%height - 62);
		Extent = "100 30";
		Text = "Discard";
		Command = %this.getID() @ ".onDiscard();";
	};
	ThemeManager.setProfile(%this.discardButton, "buttonProfile");
	%content.add(%this.discardButton);

	%this.saveButton = new GuiButtonCtrl()
	{
		HorizSizing = "right";
		VertSizing = "bottom";
		Position = (%width - 116) SPC (%height - 64);
		Extent = "100 34";
		Text = "Save";
		Command = %this.getID() @ ".onSave();";
	};
	ThemeManager.setProfile(%this.saveButton, "primaryButtonProfile");
	%content.add(%this.saveButton);
}

function GuiEditorConfirmSaveDialog::onCancel(%this)
{
	GuiEditor.dropPendingCommand();
	%this.closeNow();
}

function GuiEditorConfirmSaveDialog::onDiscard(%this)
{
	%this.closeNow();
	GuiEditor.runPendingCommand();
}

// Closed before the save starts, because a Gui that has never been saved sends
// this straight to the Save As dialog and two of these stacked is one too many.
//
// Nothing is resumed here. SaveGui may or may not end in a file being written -
// Save As has its own Cancel - so the command that was interrupted is left
// waiting, and only a save that reaches the end of SaveCore releases it.
function GuiEditorConfirmSaveDialog::onSave(%this)
{
	%this.closeNow();
	GuiEditor.SaveGui();
}

// The window X, which is the same answer as Cancel: no to the question asked.
function GuiEditorConfirmSaveDialog::onClose(%this)
{
	%this.onCancel();
}

// Not the shared EditorCore.dialog slot: this dialog can be followed by the Save
// As dialog within the scheduled delay, and the two would race for it. The same
// reason GuiProfileEditorConfirmDialog does it this way.
function GuiEditorConfirmSaveDialog::closeNow(%this)
{
	Canvas.popDialog(%this);
	EditorCore.schedule(100, "deleteDialogObject", %this);
}
