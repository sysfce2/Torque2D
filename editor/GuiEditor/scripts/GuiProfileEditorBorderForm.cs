
//-----------------------------------------------------------------------------
// The custom border-editing pane shown in place of the generic inspector when a
// border node is selected in the Gui Profile Editor. It shows the border's name
// (display only -- renaming a border that profiles reference by name is
// deliberately not offered here) above a shared GuiProfileEditorBorderGrid,
// which does all the real editing. Every grid commit is forwarded to the dialog,
// which marks the theme dirty and refreshes the preview -- the same path the
// inspector's onProfileChanged took.
//
// The creator sets the dialog back-pointer inline and an initial Extent (the
// grid lays out within that width). Call build() once after adding it to its
// scroller, then bind()/unbind() to attach a border.
//-----------------------------------------------------------------------------

function GuiProfileEditorBorderForm::onAdd(%this)
{
	ThemeManager.setProfile(%this, "emptyProfile");
}

function GuiProfileEditorBorderForm::build(%this)
{
	%w = getWord(%this.extent, 0);
	%pad = 8;
	%inner = %w - %pad * 2;

	// Name header: display only.
	%this.nameLabel = new GuiControl()
	{
		Position = %pad SPC %pad;
		Extent = %inner SPC 24;
		Text = "Border:";
		align = "left";
		vAlign = "middle";
	};
	ThemeManager.setProfile(%this.nameLabel, "labelProfile");
	%this.add(%this.nameLabel);

	%gridY = %pad + 32;
	%this.grid = new GuiControl()
	{
		class = "GuiProfileEditorBorderGrid";
		Position = %pad SPC %gridY;
		gridWidth = %inner;
		owner = %this;
	};
	%this.add(%this.grid);
	%this.grid.build();

	%this.setExtent(%w, %gridY + %this.grid.gridHeight + %pad);
}

//-----------------------------------------------------------------------------
// Binding.
//-----------------------------------------------------------------------------

// %label is the friendly border name shown in the tree (its category), used for
// the display-only header; the grid edits the %border object itself.
function GuiProfileEditorBorderForm::bind(%this, %border, %label)
{
	if(!isObject(%border))
	{
		%this.unbind();
		return;
	}
	%this.border = %border;
	%this.nameLabel.setText("Border:  " @ %label);
	%this.grid.bind(%border);
}

function GuiProfileEditorBorderForm::unbind(%this)
{
	%this.border = "";
	if(isObject(%this.grid))
	{
		%this.grid.unbind();
	}
}

// The shared grid forwards every edit here; route it to the dialog's border
// commit sink (mark the theme dirty + refresh the preview).
function GuiProfileEditorBorderForm::onBorderGridCommit(%this)
{
	if(isObject(%this.dialog))
	{
		%this.dialog.onBorderChanged();
	}
}
