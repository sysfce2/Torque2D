
function GuiProfileEditorInspector::onPostApply(%this, %object)
{
	%this.dialog.onProfileChanged(%object);
}
