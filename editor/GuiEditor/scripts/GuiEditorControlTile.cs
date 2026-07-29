//-----------------------------------------------------------------------------
// One entry in the control palette: a picture of a control you can drag onto the
// canvas, or click to drop into whatever container is current.
//
// The tile holds a palette KEY rather than a class name. For all but four
// entries those are the same string, but a bare GuiControl appears four times --
// as the wrapper, the backdrop, the line of text and the modal scrim -- and the
// key is what tells them apart. GuiEditorControlIcons turns a key into a class,
// a profile category, a label and a frame.
//
// Two shapes, set by setMode and driven by the group's grid changing its cell
// size underneath. Nothing here re-reads the extent on resize: the children
// carry sizing flags that keep them placed, so dragging the frame narrower
// reflows the grid and the tiles follow without script.
//
//   grid   an 80px picture centred in the cell, no caption, name in the tooltip
//   rows   a 32px picture at the left with the name beside it
//
// The creator sets key and owner inline, then calls setMode.
//-----------------------------------------------------------------------------

// Grid mode wants the 128 sheet (sheetFor shrinks it, which stays sharp); rows
// mode is under the small sheet's own resolution and takes it 1:1.
$GuiEditorControlTile::gridArt = 80;
$GuiEditorControlTile::rowArt = 32;
$GuiEditorControlTile::rowTextLeft = 40;

// How far the pointer must travel before a press counts as a drag rather than a
// click. Without it a shaky click starts a drag, and the two gestures do
// different things now that a click also places a control.
$GuiEditorControlTile::dragSlop = 5;

function GuiEditorControlTile::onAdd(%this)
{
	%icons = GuiEditor.controlIcons;

	%this.text = "";
	%this.dragged = false;

	// The label reads "Check Box"; the tooltip says GuiCheckBoxCtrl. The class
	// name is what someone types in script, so it should not disappear from the
	// palette just because the tile is showing a friendlier one -- and in grid
	// mode, where there is no caption at all, the tooltip is the only name.
	%this.tooltip = %icons.classFor(%this.key);

	%this.icon = new GuiSpriteCtrl()
	{
		Position = "0 0";
		Extent = "16 16";
		constrainProportions = "1";
		fullSize = "0";
		UseInput = false;
	};
	ThemeManager.setProfile(%this.icon, "spriteProfile");
	%this.add(%this.icon);

	%this.caption = new GuiControl()
	{
		Position = "0 0";
		Extent = "16 16";
		Text = %icons.labelFor(%this.key);
		align = "left";
		vAlign = "middle";
		Visible = false;
		UseInput = false;
	};
	ThemeManager.setProfile(%this.caption, "labelProfile");
	%this.add(%this.caption);

	ThemeManager.setProfile(%this, "tipProfile", "TooltipProfile");
}

//-----------------------------------------------------------------------------
// Layout.
//-----------------------------------------------------------------------------

function GuiEditorControlTile::setMode(%this, %mode)
{
	%this.mode = %mode;
	%w = getWord(%this.getExtent(), 0);
	%h = getWord(%this.getExtent(), 1);

	if(%mode $= "rows")
	{
		%art = $GuiEditorControlTile::rowArt;
		%left = $GuiEditorControlTile::rowTextLeft;

		// anchorLeft holds the left edge and lets the right one move, so the
		// picture stays put as the column widens; the caption takes the slack.
		%this.icon.HorizSizing = "anchorLeft";
		%this.icon.VertSizing = "center";
		%this.icon.setExtent(%art, %art);
		%this.icon.setPosition(4, (%h - %art) / 2);

		%this.caption.HorizSizing = "width";
		%this.caption.VertSizing = "center";
		%this.caption.setExtent(%w - %left - 4, %art);
		%this.caption.setPosition(%left, (%h - %art) / 2);
		%this.caption.setVisible(true);
	}
	else
	{
		%art = $GuiEditorControlTile::gridArt;

		%this.icon.HorizSizing = "center";
		%this.icon.VertSizing = "center";
		%this.icon.setExtent(%art, %art);
		%this.icon.setPosition((%w - %art) / 2, (%h - %art) / 2);

		%this.caption.setVisible(false);
	}

	// The sheet is picked from the size the art is DRAWN at, not from the mode,
	// so the two stay in step if either number changes.
	%this.icon.setImage(GuiEditor.controlIcons.sheetFor(%art));
	%this.icon.setImageFrame(GuiEditor.controlIcons.frameFor(%this.key));
	%this.icon.imageSize = %art SPC %art;
}

//-----------------------------------------------------------------------------
// Making one. Shared by both gestures so a dragged control and a clicked one
// cannot drift apart.
//-----------------------------------------------------------------------------

function GuiEditorControlTile::makePayload(%this)
{
	%class = GuiEditor.controlIcons.classFor(%this.key);
	%payload = eval("return new " @ %class @ "();");
	if(!isObject(%payload))
	{
		return 0;
	}

	// Only the four GuiControl faces ask for a category; every other class pins
	// its own in GuiEditorThemeApplier::buildClassTable, so the field stays unset
	// and the applier's own answer stands. It is consumed by the first theme
	// pass -- see applyToBranch.
	%category = GuiEditor.controlIcons.categoryFor(%this.key);
	if(%category !$= "")
	{
		%payload.paletteCategory = %category;
	}

	return %payload;
}

//-----------------------------------------------------------------------------
// Dragging. Lifted from the list box this replaced, offsets and all.
//-----------------------------------------------------------------------------

function GuiEditorControlTile::onTouchDown(%this, %modifier, %position, %clickCount)
{
	%this.pressAt = Canvas.getCursorPos();
	%this.dragged = false;
}

function GuiEditorControlTile::onTouchDragged(%this, %modifier, %position, %clickCount)
{
	if(%this.dragged)
	{
		return;
	}

	%now = Canvas.getCursorPos();
	%dx = mAbs(getWord(%now, 0) - getWord(%this.pressAt, 0));
	%dy = mAbs(getWord(%now, 1) - getWord(%this.pressAt, 1));
	if(%dx < $GuiEditorControlTile::dragSlop && %dy < $GuiEditorControlTile::dragSlop)
	{
		return;
	}

	%this.dragged = true;
	%this.beginDrag(%now);
}

function GuiEditorControlTile::beginDrag(%this, %cursorPos)
{
	%payload = %this.makePayload();
	if(!isObject(%payload))
	{
		return;
	}

	%position = GuiEditor.brain.getGlobalPosition();
	%xOffset = (getWord(%payload.extent, 0) / 2) + getWord(%position, 0);
	%yOffset = (getWord(%payload.extent, 1) / 2) + getWord(%position, 1);

	// Where the drag starts, so the payload does not jump on the first frame.
	%xPos = getWord(%cursorPos, 0) - %xOffset;
	%yPos = getWord(%cursorPos, 1) - %yOffset;

	%dragCtrl = new GuiDragAndDropCtrl()
	{
		canSaveDynamicFields = "0";
		Profile = "GuiDragAndDropProfile";
		HorizSizing = "anchorLeft";
		VertSizing = "anchorTop";
		Position = %xPos SPC %yPos;
		extent = %payload.extent;
		MinExtent = "32 32";
		canSave = "1";
		Visible = "1";
		hovertime = "1000";
		Text = GuiEditor.controlIcons.classFor(%this.key);
		deleteOnMouseUp = true;
	};

	%dragCtrl.add(%payload);
	GuiEditor.brain.add(%dragCtrl);
	%dragCtrl.startDragging(%xOffset, %yOffset);
}

//-----------------------------------------------------------------------------
// Clicking. A click is a drop that never moved.
//
// It routes through the brain's own onControlDropped rather than adding the
// control itself, which is the whole point: that path is where theming,
// selection, the AddControl event and undo recording all live, and a second way
// into the document would have to reproduce every one of them and would go stale
// the first time one changed.
//-----------------------------------------------------------------------------

function GuiEditorControlTile::onClick(%this)
{
	// A button keeps mDepressed through a drag -- it never overrides
	// onTouchDragged -- so releasing after a drag fires onAction as well. Without
	// this the gesture would place two controls.
	if(%this.dragged)
	{
		%this.dragged = false;
		return;
	}

	%payload = %this.makePayload();
	if(!isObject(%payload))
	{
		return;
	}

	// onControlDropped reads getGlobalPosition BEFORE addNewCtrl reparents, and
	// puts the control back there afterwards. Nothing owns the payload yet, so
	// its Position IS its global position.
	%payload.Position = %this.dropPosition(%payload);

	// Deliberately not onControlDragged first: that picks the add set by
	// hit-testing the cursor, and a click means "the container I am already
	// working in", not "whatever is under this point".
	GuiEditor.brain.onControlDropped(%payload, %payload.Position);
}

// Centred in the current add set, which is the container the editor is already
// adding to; addNewControl falls back to the root when there is none.
function GuiEditorControlTile::dropPosition(%this, %payload)
{
	%target = GuiEditor.brain.getCurrentAddSet();
	if(!isObject(%target))
	{
		%target = GuiEditor.rootGui;
	}

	%at = %target.getGlobalPosition();
	%room = %target.getExtent();
	%size = %payload.getExtent();

	%x = getWord(%at, 0) + ((getWord(%room, 0) - getWord(%size, 0)) / 2);
	%y = getWord(%at, 1) + ((getWord(%room, 1) - getWord(%size, 1)) / 2);

	return mFloor(%x) SPC mFloor(%y);
}
