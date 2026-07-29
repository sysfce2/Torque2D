
//-----------------------------------------------------------------------------
// The dynamic-field section of the Gui Editor's properties pane: the fields a
// script put on a control, which are not registered anywhere and so cannot come
// from GuiEditorControlSpec.
//
// Built fresh rather than ported. GuiInspectorDynamicGroup::createContent is
// broken in three ways that are not worth carrying over:
//
//   - it hardcodes the profile names "EditorButton", "GuiButtonProfile" and
//     "GuiTextProfile", which were 3.x script profiles and do not exist in 4.0,
//     so its Add button and the shell around it wear nothing;
//   - it calls registerObject("zAddButton") -- a fixed global name -- so
//     inspecting a second object collides with the first, and in editor mode
//     the name is never registered at all;
//   - the Add button is created in createContent but has to be rescued and
//     pushed back by clearFields on every refresh.
//
// What a control holds is filtered, not just listed. A GuiFrameSetCtrl
// serializes its entire frame tree into dynamic fields (frameID0, frameChild10,
// frameExtentX2 and so on -- see guiFrameSetCtrl.cc), and hand-editing those
// can leave a Gui that will not load. GuiEditorControlSpec::hidesDynamicField
// owns that list.
//
// Renaming is deliberately absent: remove and add again. The old inspector
// offered it, but a rename is a delete and an add anyway, and making it one
// visible step costs a name box per row that is wrong to touch by accident.
//
// A dynamic field cannot hold an empty value. SimFieldDictionary::setFieldValue
// frees the entry when the value is empty, so "" is not a value a field can
// have -- it is how a field is removed. That decides the shape of Add: naming a
// field cannot create it, because there is nothing to create it with. So Add
// puts up a row for the name and the field appears on the control the moment
// the row is given a value; a row left empty was never a field. It also makes
// the bin button and clearing the box the same operation, which is what the
// engine already believed.
//
// The creator sets pane and blockWidth inline, then calls build() once after
// adding it. Each row reports here rather than to the pane, because a dynamic
// field is written with setFieldValue on a name this section knows and the pane
// does not.
//-----------------------------------------------------------------------------

function GuiEditorDynamicFields::onAdd(%this)
{
	ThemeManager.setProfile(%this, "emptyProfile");
}

function GuiEditorDynamicFields::build(%this)
{
	%this.fieldNames = "";

	%this.grid = %this.pane.makeCellGrid(0);
	%this.add(%this.grid);

	%this.buildAddRow();
}

// A name box and an Add button. Inline rather than a dialog: naming a field is
// the whole of the operation, so a modal for it would be ceremony.
function GuiEditorDynamicFields::buildAddRow(%this)
{
	%w = %this.blockWidth;
	%buttonW = 56;

	%row = new GuiControl()
	{
		HorizSizing = "width";
		Position = "0 0";
		Extent = %w SPC 30;
	};
	ThemeManager.setProfile(%row, "emptyProfile");
	%this.add(%row);
	%this.addRow = %row;

	%this.nameBox = new GuiTextEditCtrl()
	{
		HorizSizing = "width";
		Position = "4 4";
		Extent = (%w - %buttonW - 16) SPC 22;
		Tooltip = "Name for a new dynamic field";
	};
	ThemeManager.setProfile(%this.nameBox, "textEditProfile");
	ThemeManager.setProfile(%this.nameBox, "tipProfile", "TooltipProfile");
	%this.nameBox.ReturnCommand = %this.getID() @ ".onAddClicked();";
	%row.add(%this.nameBox);

	%this.addButton = new GuiButtonCtrl()
	{
		HorizSizing = "left";
		Position = (%w - %buttonW - 4) SPC 4;
		Extent = %buttonW SPC 22;
		Text = "Add";
		Command = %this.getID() @ ".onAddClicked();";
	};
	ThemeManager.setProfile(%this.addButton, "buttonProfile");
	%row.add(%this.addButton);
}

//-----------------------------------------------------------------------------
// Binding. The rows are rebuilt per control rather than filtered: unlike a
// registered field there is no fixed set to hide from, and two controls rarely
// carry the same dynamic fields.
//-----------------------------------------------------------------------------

function GuiEditorDynamicFields::bind(%this, %ctrl)
{
	// A named-but-empty row belongs to the control it was named on. Moving the
	// selection abandons it, which is right: it was never a field.
	if(%ctrl != %this.target)
	{
		%this.pendingField = "";
	}

	%this.target = %ctrl;
	%this.grid.deleteObjects();
	%this.fieldNames = "";

	if(!isObject(%ctrl))
	{
		return;
	}

	%class = %ctrl.getClassName();
	%count = %ctrl.getDynamicFieldCount();
	for(%i = 0; %i < %count; %i++)
	{
		%name = getWord(%ctrl.getDynamicField(%i), 0);
		if(%name $= "" || %this.pane.spec.hidesDynamicField(%class, %name))
		{
			continue;
		}
		%this.addFieldRow(%name);
	}

	// The row for a field that has been named but not yet given a value. It is
	// last because it is the newest, and it disappears on its own if it is left
	// empty and the selection moves.
	if(%this.pendingField !$= "" && %ctrl.getFieldValue(%this.pendingField) $= "")
	{
		%this.addFieldRow(%this.pendingField);
	}
}

function GuiEditorDynamicFields::addFieldRow(%this, %name)
{
	%row = new GuiControl()
	{
		class = "GuiProfileEditorFieldRow";
		Position = "0 0";
		fieldName = %name;
		labelText = %name;
		kind = "text";
		owner = %this;
	};
	%this.grid.add(%row);
	%row.build();

	// The row's reset button is already a small icon pinned to the cell's right
	// edge, which is exactly where a remove wants to be, so it wears the bin;
	// the row calls owner.onProfileRowReset when it is clicked, and for a
	// dynamic field "reset" means "take it away".
	%row.resetButton.icon.setImageFrame($EditorIcon::trash);
	%row.resetButton.Tooltip = "Remove this field";
	%row.resetButton.setVisible(true);

	%row.setValue(%this.target.getFieldValue(%name));

	%this.row[%name] = %row;
	%this.fieldNames = (%this.fieldNames $= "") ? %name : (%this.fieldNames SPC %name);
	return %row;
}

// Is there anything to show? The section hides itself when a control carries no
// dynamic fields, which is most of them.
function GuiEditorDynamicFields::hasFields(%this)
{
	return %this.fieldNames !$= "";
}

//-----------------------------------------------------------------------------
// Editing.
//-----------------------------------------------------------------------------

function GuiEditorDynamicFields::onProfileRowCommit(%this, %row)
{
	if(!isObject(%this.target) || !%row.hasChanged())
	{
		return;
	}

	%value = %row.getValue();
	%this.target.setFieldValue(%row.fieldName, %value);
	%row.markClean();
	%this.pane.afterCommit();

	if(%row.fieldName $= %this.pendingField && %value !$= "")
	{
		// It is a real field now, so it will come back from the control's own
		// list and no longer needs holding open.
		%this.pendingField = "";
	}
	else if(%value $= "")
	{
		// An emptied box is a removed field, the same as the bin button.
		// Deferred for the same reason: this arrives from inside the row.
		%this.schedule(0, "rebindDeferred");
	}
}

// Remove. A dynamic field is cleared by writing "" to it -- there is no delete
// -- and the row goes with it, so this rebinds rather than trying to unpick one
// cell from the grid.
function GuiEditorDynamicFields::onProfileRowReset(%this, %row)
{
	if(!isObject(%this.target))
	{
		return;
	}

	%this.target.setFieldValue(%row.fieldName, "");
	%this.pane.afterCommit();

	// Deferred: this call arrives from the button inside the row that is about
	// to be freed, so deleting the grid's children here would pull the control
	// out from under the event being dispatched.
	%this.schedule(0, "rebindDeferred");
}

function GuiEditorDynamicFields::rebindDeferred(%this)
{
	%this.bind(%this.target);
	%this.pane.onDynamicFieldsChanged();
}

function GuiEditorDynamicFields::onAddClicked(%this)
{
	%name = trim(%this.nameBox.getText());
	if(%name $= "" || !isObject(%this.target))
	{
		return;
	}

	// A name that collides with a registered field would write the real field
	// instead of making a dynamic one, which is not what the button says.
	if(%this.target.getFieldType(%name) !$= "")
	{
		warn("GuiEditorDynamicFields: '" @ %name @ "' is a built-in field of " @
			%this.target.getClassName() @ ", not a dynamic one.");
		return;
	}

	if(%this.target.getFieldValue(%name) !$= "")
	{
		warn("GuiEditorDynamicFields: '" @ %name @ "' is already set on this control.");
		return;
	}

	// Named, not created: an empty dynamic field does not exist, so the row is
	// what Add produces and the first value is what makes it a field.
	%this.pendingField = %name;
	%this.nameBox.setText("");

	%this.bind(%this.target);
	%this.pane.onDynamicFieldsChanged();

	// Put the caret in the new row's box, since typing a value is the whole of
	// what is left to do.
	%row = %this.row[%name];
	if(isObject(%row))
	{
		%row.editor.setFirstResponder();
	}
}
