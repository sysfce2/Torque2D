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
//   grid   a 56px picture with the name under it, wrapping to two lines
//   rows   a 32px picture at the left with the name beside it
//
// The creator sets key and owner inline, then calls setMode.
//-----------------------------------------------------------------------------

// Both sizes come off the 64 sheet -- sheetFor only reaches for the 128 one
// above 64. Rows mode is at that sheet's own resolution and takes it 1:1; grid
// mode shrinks it by an eighth, which is a gentler downscale than 128 to 56
// would be and holds the thin strokes better.
$GuiEditorControlTile::gridArt = 56;
$GuiEditorControlTile::rowArt = 32;
$GuiEditorControlTile::rowTextLeft = 40;

// How much of a grid tile is kept clear for the name: two lines of the largest
// label font any shipped theme uses -- Torque Suit's fontSize - 2, which is 20
// -- plus the two pixels labelProfile pads above and below.
//
// This is a budget the icon is placed against, not a box the text is put in.
// The caption itself fills the tile and bottom-aligns, so a third line would
// grow UP into the picture rather than off the bottom. Nothing in the table can
// reach three: the longest label wraps to two at this width, and the only other
// source of names is the Undrawn sweep, whose entries are raw class names -- one
// unbreakable word, kept to a single line and clipped across, with the full name
// in the tooltip either way.
$GuiEditorControlTile::gridCaption = 44;

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
	// palette just because the tile is showing a friendlier one. Both modes show
	// the label, so the tooltip is the only place the class name appears.
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

	%this.startListening(ThemeManager);
	%this.refreshTint();
}

//-----------------------------------------------------------------------------
// Color.
//
// The sheets are greyscale, drawn to be modulated rather than to be shown as
// they are -- so the tint is not decoration, it is what makes the picture
// legible. Left alone a GuiSpriteCtrl blends with opaque white, which is why
// this was invisible on the theme the editor starts in: that one draws its text
// white too, so an icon nobody had tinted looked exactly right. On the light
// theme the same untinted art is a white smear on a pale panel.
//
// The color comes from the tile's OWN profile rather than from the caption's,
// because the icon is drawn on the tile. The two name the same color in every
// theme that ships, and the profile a thing is drawn on is the one that should
// decide when they stop agreeing.
//
// It has to be re-read on a theme change: ThemeManager swaps the profile object
// on everything that registered one, which is enough for backgrounds and text,
// but a color copied onto a sprite is a copy and stays behind.
//-----------------------------------------------------------------------------

function GuiEditorControlTile::refreshTint(%this)
{
	%this.icon.setImageColor(ThemeManager.activeTheme.itemSelectProfile.fontColor);
}

function GuiEditorControlTile::onThemeChange(%this, %theme)
{
	%this.refreshTint();
}

//-----------------------------------------------------------------------------
// Layout.
//-----------------------------------------------------------------------------

function GuiEditorControlTile::setMode(%this, %mode)
{
	%this.mode = %mode;
	%w = getWord(%this.getExtent(), 0);
	%h = getWord(%this.getExtent(), 1);

	// One caption serves both modes, so each branch sets every property the other
	// one touches. Leaving a mode to inherit what the last one happened to write
	// is how a switch back stops being a switch back.
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
		%this.caption.align = "left";
		%this.caption.vAlign = "middle";

		// A row is one line tall, so wrapping there would only ever hide the
		// tail of a name the row has the width to show.
		%this.caption.textWrap = false;
		%this.caption.setVisible(true);
	}
	else
	{
		%art = $GuiEditorControlTile::gridArt;
		%band = $GuiEditorControlTile::gridCaption;

		// The caption takes the whole inner rect and bottom-aligns its text
		// inside it, so the ENGINE decides where the floor of the tile is.
		// Nothing here has to know what the tile's own profile insets, which is
		// a per-theme number -- 3 pixels a side on the base theme, 4 on Torque
		// Suit -- that script has no way to ask for. Positioning a short band
		// against the outer extent instead would hang it below the inner rect
		// and renderChild would clip the last line's descenders off.
		//
		// Bottom alignment is also what makes a row read level: a one-line name
		// like "Check Box" sits on the same line as the second line of "Radio
		// Button" beside it, rather than floating half a line higher.
		%this.caption.HorizSizing = "fill";
		%this.caption.VertSizing = "fill";
		%this.caption.align = "center";
		%this.caption.vAlign = "bottom";
		%this.caption.textWrap = true;
		%this.caption.setVisible(true);
		%this.caption.applySizing();

		// And that fill is how the inset gets measured. What the caption now
		// reports IS the inner rect, so the picture can be centered in the room
		// left above the band whatever a theme's border costs, instead of
		// sitting at a fixed offset a fatter border would push into the text.
		%innerH = getWord(%this.caption.getExtent(), 1);

		// "center" is resolved against the inner rect too, so the picture and
		// the name below it share one center line. Doing this arithmetically
		// off %w would put the icon half a border to the right of its label.
		%this.icon.HorizSizing = "center";
		%this.icon.VertSizing = "anchorTop";
		%this.icon.setExtent(%art, %art);
		%this.icon.setPosition(0, (%innerH - %band - %art) / 2);
		%this.icon.applySizing();
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

	// Centred in the container being worked in, which the brain knows because it
	// owns both the add set and the canvas the container has to be visible on.
	//
	// onControlDropped reads getGlobalPosition BEFORE addNewCtrl reparents, and
	// puts the control back there afterwards. Nothing owns the payload yet, so
	// its Position IS its global position.
	%payload.Position = GuiEditor.brain.centredPlacement(%payload);

	// Deliberately not onControlDragged first: that picks the add set by
	// hit-testing the cursor, and a click means "the container I am already
	// working in", not "whatever is under this point".
	//
	// And placeControl rather than onControlDropped, because the cursor test that
	// one opens with is a drag's question. A click has no cursor, the position
	// above is already inside the visible part of the container, and a control
	// that pins its own position never took it anyway.
	GuiEditor.brain.placeControl(%payload);
}
