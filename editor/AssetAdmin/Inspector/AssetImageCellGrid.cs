
//-----------------------------------------------------------------------------
// The eight values that cut an image into frames, as one small table:
//
//              X       Y
//     Count  [  4  ] [  2  ]
//     Size   [128  ] [128  ]
//     Offset [  0  ] [  0  ]
//     Stride [  0  ] [  0  ]
//
//     [x] Row order
//
// Eight captioned field rows would say the same thing, and the stock inspector
// tries: it reflects them into an "X Values" group and a "Y Values" group, so a
// cell's width is four fields away from its height and the pairs that have to be
// read together never appear together. They are two columns of one table, and
// this draws them as one -- which also keeps them one cell of the pane's grid,
// so no reflow can ever split a pair across a column break.
//
// The table never writes the asset. It hands each edit to its owner --
// owner.onCellGridCommit(%field, %value) -- so the pane stays the only thing
// that touches the asset, and the only thing that has to know that every write
// saves the file. The same reason GuiProfileEditorBorderGrid reports to its
// host rather than editing through it.
//
// The creator sets owner inline, then calls build() once after adding the table
// to its container -- the container decides how wide the cell is, so build() has
// to run after the add. It records the laid-out height in .gridHeight.
//-----------------------------------------------------------------------------

function AssetImageCellGrid::onAdd(%this)
{
	ThemeManager.setProfile(%this, "emptyProfile");
}

// Row title TAB X field TAB Y field TAB X tip TAB Y tip.
function AssetImageCellGrid::rowTable(%this)
{
	return
		"Count"  TAB "CellCountX"  TAB "CellCountY"  TAB
			"How many cells across the image." TAB
			"How many cells down the image." NL
		"Size"   TAB "CellWidth"   TAB "CellHeight"  TAB
			"The width of one cell, in pixels." TAB
			"The height of one cell, in pixels." NL
		"Offset" TAB "CellOffsetX" TAB "CellOffsetY" TAB
			"The gap between the left edge of the image and the first cell." TAB
			"The gap between the top edge of the image and the first cell." NL
		"Stride" TAB "CellStrideX" TAB "CellStrideY" TAB
			"From one cell's left edge to the next one's. Leave at 0 to use the cell width." TAB
			"From one cell's top edge to the next one's. Leave at 0 to use the cell height.";
}

// Every field the table edits, in one list, for the pane's greying.
function AssetImageCellGrid::fields(%this)
{
	return "CellCountX CellCountY CellWidth CellHeight CellOffsetX CellOffsetY CellStrideX CellStrideY CellRowOrder";
}

function AssetImageCellGrid::build(%this)
{
	// The width available, which is not the width taken: the table is eight boxes
	// of two or three digits, and past a certain point a wider block only buys
	// whitespace between them. So it measures what it is given, sizes its boxes
	// to fit, and then shrinks to what it actually used.
	//
	// Which is also why it carries no sizing flag. A block that grows leaves the
	// table where it is rather than stretching it into a row of very wide number
	// boxes, and a block can never get narrower than the table needs -- the
	// blocks have a 300-pixel floor and the table wants 216.
	%avail = getWord(%this.getExtent(), 0);
	%x0 = 4;
	%labelW = 52;
	%gap = 6;
	%rowH = 26;
	%boxH = 22;
	%headerH = 16;

	// The boxes hold two or three digits, so they are sized for that -- a number
	// box the width of half a pane reads as a text field with a number lost in
	// it. Narrow enough and they give ground instead of clipping.
	%room = %avail - %x0 - %labelW - %gap - 4;
	%boxW = mGetMin(72, mGetMax(34, (%room - %gap) / 2));
	%w = %x0 + %labelW + %gap + %boxW + %gap + %boxW + 4;

	%colX[0] = %x0 + %labelW + %gap;
	%colX[1] = %colX[0] + %boxW + %gap;

	// The two column captions. Nothing else says which box is which axis.
	%axis = "X" TAB "Y";
	for(%c = 0; %c < 2; %c++)
	{
		%cap = new GuiControl()
		{
			Position = %colX[%c] SPC 2;
			Extent = %boxW SPC %headerH;
			Text = getField(%axis, %c);
			align = "center";
			vAlign = "middle";
			UseInput = false;
		};
		ThemeManager.setProfile(%cap, "labelProfile");
		%this.add(%cap);
	}

	%rows = %this.rowTable();
	%count = getRecordCount(%rows);
	for(%r = 0; %r < %count; %r++)
	{
		%rec = getRecord(%rows, %r);
		%y = %headerH + 4 + (%r * %rowH);

		%label = new GuiControl()
		{
			Position = %x0 SPC %y;
			Extent = %labelW SPC %boxH;
			Text = getField(%rec, 0);
			align = "left";
			vAlign = "middle";
			UseInput = false;
		};
		ThemeManager.setProfile(%label, "labelProfile");
		%this.add(%label);

		for(%c = 0; %c < 2; %c++)
		{
			%this.makeBox(%colX[%c], %y, %boxW, %boxH,
				getField(%rec, 1 + %c), getField(%rec, 3 + %c));
		}
	}

	// Which way the frame numbers run. It only means anything once the image is
	// cut both ways, but it is the last thing about the cut, so it belongs here
	// rather than in a section of its own.
	%uy = %headerH + 8 + (%count * %rowH);
	%cbW = %w - %x0 - 4;
	%this.rowOrderBox = new GuiCheckBoxCtrl()
	{
		Position = %x0 SPC %uy;
		Extent = %cbW SPC 26;
		Text = "Row order";
		boxOffset = "0 4";
		boxExtent = "18 18";
		textOffset = "26 4";
		textExtent = (%cbW - 26) SPC 18;
		Tooltip = "Number the frames left to right, then top to bottom. Turn it off to number them down each column instead.";
		Command = %this.getID() @ ".commitRowOrder();";
	};
	ThemeManager.setProfile(%this.rowOrderBox, "checkboxProfile");
	ThemeManager.setProfile(%this.rowOrderBox, "tipProfile", "TooltipProfile");
	%this.add(%this.rowOrderBox);

	%this.gridHeight = %uy + 30;
	%this.setExtent(%w, %this.gridHeight);
}

// One numeric box. The class is what gives it the arrow keys; the tip is kept on
// the box as well as set on it, because greying the table replaces every tooltip
// with the reason and has to be able to put them back.
function AssetImageCellGrid::makeBox(%this, %x, %y, %w, %h, %field, %tip)
{
	%box = new GuiTextEditCtrl()
	{
		class = "AssetImageCellInput";
		Position = %x SPC %y;
		Extent = %w SPC %h;
		inputMode = "Number";
		align = "center";
		Tooltip = %tip;
		tipText = %tip;
		cellField = %field;
		grid = %this;
	};
	ThemeManager.setProfile(%box, "textEditProfile");
	ThemeManager.setProfile(%box, "tipProfile", "TooltipProfile");
	%box.AltCommand = %this.getID() @ ".commitBox(" @ %box.getID() @ ");";
	%box.ReturnCommand = %this.getID() @ ".commitBox(" @ %box.getID() @ ");";
	%this.add(%box);
	%this.box[%field] = %box;
	return %box;
}

//-----------------------------------------------------------------------------
// Values.
//-----------------------------------------------------------------------------

// Load the asset's nine values. The populating guard keeps setText and
// setStateOn from echoing straight back through the commits.
function AssetImageCellGrid::load(%this, %asset)
{
	if(!isObject(%asset))
	{
		return;
	}

	%this.populating = true;

	%rows = %this.rowTable();
	%count = getRecordCount(%rows);
	for(%r = 0; %r < %count; %r++)
	{
		%rec = getRecord(%rows, %r);
		for(%c = 0; %c < 2; %c++)
		{
			%field = getField(%rec, 1 + %c);
			%this.box[%field].setText(%asset.getFieldValue(%field));
		}
	}

	%this.rowOrderBox.setStateOn(%asset.getCellRowOrder());

	%this.populating = false;
}

// Inert but still readable, which is what the table becomes in explicit frame
// mode: the values are still the ones the asset holds, they are simply not the
// ones it cuts by. Blanking them would lose what a user is about to go back to.
function AssetImageCellGrid::setEnabled(%this, %enabled, %reason)
{
	%rows = %this.rowTable();
	%count = getRecordCount(%rows);
	for(%r = 0; %r < %count; %r++)
	{
		%rec = getRecord(%rows, %r);
		for(%c = 0; %c < 2; %c++)
		{
			%box = %this.box[getField(%rec, 1 + %c)];
			%box.setActive(%enabled);
			%box.Tooltip = %enabled ? %box.tipText : %reason;
		}
	}

	%this.rowOrderBox.setActive(%enabled);
	%this.enabled = %enabled;
}

//-----------------------------------------------------------------------------
// Commit. Nothing here writes the asset -- see the header.
//-----------------------------------------------------------------------------

function AssetImageCellGrid::commitBox(%this, %box)
{
	// mFloor, not the text: a box left holding "12.0" would write "12.0" into a
	// field the engine reads as a whole number of pixels.
	%this.notify(%box.cellField, mFloor(%box.getText()));
}

function AssetImageCellGrid::commitRowOrder(%this)
{
	%this.notify("CellRowOrder", %this.rowOrderBox.getStateOn());
}

function AssetImageCellGrid::notify(%this, %field, %value)
{
	if(%this.populating || !isObject(%this.owner))
	{
		return;
	}

	%this.owner.onCellGridCommit(%field, %value);
}

//-----------------------------------------------------------------------------
// The numeric boxes: up and down nudge by one. A second class in this file, for
// the reason EditorFieldRow keeps its two -- it exists only to route one engine
// callback back to the widget that owns the box, and a file of its own would say
// nothing this one does not.
//
// Clicking places the caret, as it does in every other box in the editor; see
// the note in EditorFieldRow for why nothing re-selects here.
//-----------------------------------------------------------------------------

function AssetImageCellInput::onUpArrow(%this)
{
	%this.nudge(1);
}

function AssetImageCellInput::onDownArrow(%this)
{
	%this.nudge(-1);
}

function AssetImageCellInput::nudge(%this, %delta)
{
	// None of these nine can be negative, and a cell count of zero is what the
	// asset already means by "one row" -- so the floor is where the field's own
	// validation is, not somewhere this decides.
	%value = %this.getText() + %delta;
	%this.setText(mGetMax(0, %value));
	%this.selectAllText();
	%this.grid.commitBox(%this);
}
