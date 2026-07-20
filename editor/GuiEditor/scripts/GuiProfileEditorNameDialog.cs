
//-----------------------------------------------------------------------------
// A small name prompt used by the Gui Profile Editor for new/rename
// operations. The spawner sets dialogText, callbackTarget, callbackMethod,
// and optionally defaultName; OK calls callbackTarget.callbackMethod(name).
//-----------------------------------------------------------------------------

function GuiProfileEditorNameDialog::init(%this, %width, %height)
{
	%window = %this.getObject(0);
	%content = %window.getObject(0);

	%form = new GuiGridCtrl()
	{
		class = "EditorForm";
		extent = %width SPC %height;
		cellSizeX = %width - 20;
		cellSizeY = 50;
	};
	%form.addListener(%this);

	%item = %form.addFormItem("Name", (%width - 20) SPC 30);
	%this.nameBox = %form.createTextEditItem(%item);
	%this.nameBox.text = %this.defaultName;
	%this.nameBox.ReturnCommand = %this.getId() @ ".onDone();";
	%this.nameBox.EscapeCommand = %this.getId() @ ".onClose();";
	%content.add(%form);

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

	%this.okButton = new GuiButtonCtrl()
	{
		HorizSizing = "right";
		VertSizing = "bottom";
		Position = (%width - 116) SPC (%height - 64);
		Extent = "100 34";
		Text = "OK";
		Command = %this.getID() @ ".onDone();";
	};
	ThemeManager.setProfile(%this.okButton, "primaryButtonProfile");
	%content.add(%this.okButton);
}

function GuiProfileEditorNameDialog::onDone(%this)
{
	%name = trim(%this.nameBox.getText());
	if(%name $= "")
	{
		return;
	}

	%this.callbackTarget.call(%this.callbackMethod, %name);
	%this.onClose();
}

// This dialog can sit on top of another EditorDialog, so it must not use the
// shared EditorCore.dialog delete slot - closing both within the scheduled
// delay would leak one of them. The parent object deletes it after a pause;
// scheduling "delete" on the dialog itself would fire inside its own
// script-callback guard and assert.
function GuiProfileEditorNameDialog::onClose(%this)
{
	Canvas.popDialog(%this);
	EditorCore.schedule(100, "deleteDialogObject", %this);
}
