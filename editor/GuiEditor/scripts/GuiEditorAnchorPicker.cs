
//-----------------------------------------------------------------------------
// The sizing widget in the Gui Editor's properties pane header: which edges of
// a control stay put when its parent resizes.
//
// Four edge pins say directly what the field means -- which edges stay put --
// and the readout along the bottom names the pair the Gui file will actually
// carry, so nothing is hidden.
//
// The names below are the anchor set, which now comes first in the engine's
// tables (guiControl.cc):
//
//   anchorLeft    the left edge stays put
//   anchorRight   the right edge stays put
//   width         both edges stay; the width follows the parent
//   center        neither edge; the control stays centred
//   scale         both edges scale with the parent
//   fill          position 0, extent = the parent's inner extent
//
// The original names said the opposite of what they did -- "right" pinned the
// LEFT edge, because parentResized has no branch for it and so nothing moves.
// They still load; they are simply never written any more. This widget only
// ever speaks the new set.
//
// Fill and Scale are not pin states -- fill also zeroes the position and
// measures against the parent's INNER rect, which no combination of pins can
// express -- so they are per-axis toggles that supersede the pins. Their rows
// are labelled H: and V: because two unlabelled rows of identical checkboxes
// gave no clue which axis was which. The chips are named for the values they
// set, so "Scale" is now the field's own word rather than this editor's.
//
// Nothing here has a fixed width. The pane lays its cells out in a GuiGridCtrl,
// which resizes each child to the column it computed, so a hard-coded width is
// overwritten anyway -- and a widget about sizing flags ought to use them. The
// pin cluster stays pinned left, the labels and the readout follow the width.
//
// The creator sets owner inline, then calls build(). Changes are reported to
// owner.onAnchorChanged(); the widget never writes to a control.
//-----------------------------------------------------------------------------

function GuiEditorAnchorPicker::onAdd(%this)
{
	ThemeManager.setProfile(%this, "emptyProfile");
}

function GuiEditorAnchorPicker::build(%this)
{
	// Tall enough for the pin cluster and the readout beneath it. Height is the
	// one dimension worth stating: the grid runs in variable-row mode, where a
	// cell takes the taller of its child and the nominal cell size, so this is
	// what reserves the room. Width is the grid's to decide.
	%w = getWord(%this.getExtent(), 0);
	%this.setExtent(%w, 124);

	%this.title = %this.makeLabel(4, 0, %w - 8, "Anchor", "width");

	// The pin cluster. Each wears the arrow that points at the edge it holds.
	%this.topPin = %this.makePin(34, 18, "top", $EditorIcon::arrow_top, "Pin the top edge");
	%this.leftPin = %this.makePin(8, 44, "left", $EditorIcon::arrow_left, "Pin the left edge");
	%this.rightPin = %this.makePin(60, 44, "right", $EditorIcon::arrow_right, "Pin the right edge");
	%this.bottomPin = %this.makePin(34, 70, "bottom", $EditorIcon::arrow_bottom, "Pin the bottom edge");

	// The two special modes, one row per axis and each row named, so it is
	// never a guess which axis a checkbox belongs to.
	%this.hLabel = %this.makeLabel(96, 22, 20, "H:", "right");
	%this.hFill = %this.makeChip(118, 20, 56, "h", "fill", "Fill",
		"Fill the parent horizontally");
	%this.hRel = %this.makeChip(178, 20, 80, "h", "scale", "Scale",
		"Scale both edges with the parent's width");

	%this.vLabel = %this.makeLabel(96, 62, 20, "V:", "right");
	%this.vFill = %this.makeChip(118, 60, 56, "v", "fill", "Fill",
		"Fill the parent vertically");
	%this.vRel = %this.makeChip(178, 60, 80, "v", "scale", "Scale",
		"Scale both edges with the parent's height");

	// The resolved pair, along the bottom where it reads as the answer rather
	// than as another control.
	%this.readout = %this.makeLabel(8, 98, %w - 16, "", "width");
}

// %sizing is the raw enum, so "right" means pinned to the left and "width"
// means both edges -- the very naming this widget exists to hide, spelled out
// here because these are the only four places it is written by hand.
function GuiEditorAnchorPicker::makeLabel(%this, %x, %y, %w, %text, %sizing)
{
	%label = new GuiControl()
	{
		HorizSizing = %sizing;
		Position = %x SPC %y;
		Extent = %w SPC 20;
		Text = %text;
		align = "left";
		vAlign = "middle";
	};
	ThemeManager.setProfile(%label, "labelProfile");
	%this.add(%label);
	return %label;
}

// A pin is a toggle, so it is a checkbox rather than a button: the state has to
// stay put, and only a checkbox refuses to act while it is disabled.
function GuiEditorAnchorPicker::makePin(%this, %x, %y, %edge, %frame, %tip)
{
	%pin = new GuiCheckBoxCtrl()
	{
		class = "EditorToggleIcon";
		Position = %x SPC %y;
		Extent = "24 24";
		frameOff = %frame;
		frameOn = %frame;
		tipOn = %tip;
		tipOff = %tip;
		toggleName = %edge;
		owner = %this;
	};
	ThemeManager.setProfile(%pin, "iconButtonProfile");
	ThemeManager.setProfile(%pin, "tipProfile", "TooltipProfile");
	%this.add(%pin);
	return %pin;
}

function GuiEditorAnchorPicker::makeChip(%this, %x, %y, %w, %axis, %mode, %text, %tip)
{
	%chip = new GuiCheckBoxCtrl()
	{
		Position = %x SPC %y;
		Extent = %w SPC 22;
		Text = %text;
		boxOffset = "0 1";
		boxExtent = "16 16";
		textOffset = "19 1";
		textExtent = (%w - 19) SPC 21;
		Tooltip = %tip;
		Command = %this.getID() @ ".onChipClicked(\"" @ %axis @ "\",\"" @ %mode @ "\");";
	};
	ThemeManager.setProfile(%chip, "checkboxProfile");
	ThemeManager.setProfile(%chip, "tipProfile", "TooltipProfile");
	%this.add(%chip);
	return %chip;
}

//-----------------------------------------------------------------------------
// The enum on both sides of the widget. These two functions are the whole of
// the naming problem, kept together so the inversion is visible in one place.
//-----------------------------------------------------------------------------

function GuiEditorAnchorPicker::readEnums(%this, %horiz, %vert)
{
	%this.hSpecial = "";
	%this.vSpecial = "";
	%this.pinLeft = false;
	%this.pinRight = false;
	%this.pinTop = false;
	%this.pinBottom = false;

	// Both name sets are read, because a Gui written before the rename still
	// carries the old ones and the engine will happily hand them back if the
	// field was set from such a file in the same session.
	switch$(%horiz)
	{
		case "fill": %this.hSpecial = "fill";
		case "scale" or "relative": %this.hSpecial = "scale";
		case "width": %this.pinLeft = true; %this.pinRight = true;
		case "anchorLeft" or "right": %this.pinLeft = true;
		case "anchorRight" or "left": %this.pinRight = true;
		// "center" pins neither, which is the remaining state.
	}

	switch$(%vert)
	{
		case "fill": %this.vSpecial = "fill";
		case "scale" or "relative": %this.vSpecial = "scale";
		case "height": %this.pinTop = true; %this.pinBottom = true;
		case "anchorTop" or "bottom": %this.pinTop = true;
		case "anchorBottom" or "top": %this.pinBottom = true;
	}

	%this.updateWidgets();
}

function GuiEditorAnchorPicker::horizEnum(%this)
{
	if(%this.hSpecial !$= "")
	{
		return %this.hSpecial;
	}
	if(%this.pinLeft && %this.pinRight)
	{
		return "width";
	}
	if(%this.pinLeft)
	{
		return "anchorLeft";
	}
	if(%this.pinRight)
	{
		return "anchorRight";
	}
	return "center";
}

function GuiEditorAnchorPicker::vertEnum(%this)
{
	if(%this.vSpecial !$= "")
	{
		return %this.vSpecial;
	}
	if(%this.pinTop && %this.pinBottom)
	{
		return "height";
	}
	if(%this.pinTop)
	{
		return "anchorTop";
	}
	if(%this.pinBottom)
	{
		return "anchorBottom";
	}
	return "center";
}

//-----------------------------------------------------------------------------
// Interaction.
//-----------------------------------------------------------------------------

// A pin toggled itself; read its state back rather than flipping our own copy,
// so the widget and the checkbox can never disagree.
function GuiEditorAnchorPicker::onToggleIconChanged(%this, %pin)
{
	if(%this.populating)
	{
		return;
	}

	// A pin and a special cannot both be in effect, so touching a pin takes
	// that axis back to the pins.
	switch$(%pin.toggleName)
	{
		case "left": %this.pinLeft = %pin.getStateOn(); %this.hSpecial = "";
		case "right": %this.pinRight = %pin.getStateOn(); %this.hSpecial = "";
		case "top": %this.pinTop = %pin.getStateOn(); %this.vSpecial = "";
		case "bottom": %this.pinBottom = %pin.getStateOn(); %this.vSpecial = "";
	}

	%this.updateWidgets();
	%this.owner.onAnchorChanged(%this);
}

function GuiEditorAnchorPicker::onChipClicked(%this, %axis, %mode)
{
	if(%this.populating)
	{
		return;
	}

	// Clicking the mode that is already on turns it off, which drops the axis
	// back to whatever its pins say.
	if(%axis $= "h")
	{
		%this.hSpecial = (%this.hSpecial $= %mode) ? "" : %mode;
	}
	else
	{
		%this.vSpecial = (%this.vSpecial $= %mode) ? "" : %mode;
	}

	%this.updateWidgets();
	%this.owner.onAnchorChanged(%this);
}

// Show the current state. A pin is on only when its axis is actually being
// driven by pins -- with Fill in effect the pins are not what is happening, so
// they read as off.
function GuiEditorAnchorPicker::updateWidgets(%this)
{
	%this.populating = true;

	%hPinned = %this.hSpecial $= "";
	%vPinned = %this.vSpecial $= "";

	%this.leftPin.setValue(%this.pinLeft && %hPinned);
	%this.rightPin.setValue(%this.pinRight && %hPinned);
	%this.topPin.setValue(%this.pinTop && %vPinned);
	%this.bottomPin.setValue(%this.pinBottom && %vPinned);

	%this.hFill.setStateOn(%this.hSpecial $= "fill");
	%this.hRel.setStateOn(%this.hSpecial $= "scale");
	%this.vFill.setStateOn(%this.vSpecial $= "fill");
	%this.vRel.setStateOn(%this.vSpecial $= "scale");

	%this.readout.setText(%this.horizEnum() @ " / " @ %this.vertEnum());

	%this.populating = false;
}

// Grey the axis where the parent owns the sizing anyway. Everything here is a
// checkbox, which will not act while inactive, so this disables the behaviour
// as well as the look.
function GuiEditorAnchorPicker::setAxisEnabled(%this, %horiz, %vert)
{
	%this.leftPin.setActive(%horiz);
	%this.rightPin.setActive(%horiz);
	%this.hFill.setActive(%horiz);
	%this.hRel.setActive(%horiz);
	%this.hLabel.setActive(%horiz);

	%this.topPin.setActive(%vert);
	%this.bottomPin.setActive(%vert);
	%this.vFill.setActive(%vert);
	%this.vRel.setActive(%vert);
	%this.vLabel.setActive(%vert);
}
