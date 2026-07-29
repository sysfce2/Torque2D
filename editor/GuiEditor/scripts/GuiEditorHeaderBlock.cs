
//-----------------------------------------------------------------------------
// The always-visible top of the Gui Editor's properties pane: what you need
// about the selected control without opening anything.
//
// There are not twenty-nine of these. The header is one shell with three
// swappable blocks, and a class only chooses which shape each block takes:
//
//   identity   name, profile and the state toggles. The same for everything
//              except a menu item, which has no GuiControl fields at all.
//   geometry   position, extent and the two sizing enums -- but only the parts
//              the control's PARENT has left it. A grid, frame set or tab book
//              owns all of it; a chain owns one axis. See
//              GuiEditorControlSpec::geometryModeOf.
//   text       the control's own string, when it has one that is drawn. Named
//              for what it actually is: a window's title, a tab's caption, a
//              drop-down's placeholder.
//   value      nothing, or the one or two fields that are the whole point of
//              the control -- a slider's range, a sprite's image.
//
// The block owns its widgets and no values: every row reports to the pane, so
// the pane stays the only thing that writes to the control. The creator sets
// pane, spec and blockWidth inline, then calls build() once after adding it.
//
// The toggle row is split on purpose. hidden and locked are icon buttons, which
// is what they want to be and what the icon sheet has art for (an open and a
// closed padlock). Visible, Active and Accepts Input stay labelled checkboxes:
// the sheet has an eye, but nothing that reads as "active" or "accepts input"
// without a caption -- and a row of icons that each need a tooltip to be
// understood is worse than three short words.
//-----------------------------------------------------------------------------

function GuiEditorHeaderBlock::onAdd(%this)
{
	ThemeManager.setProfile(%this, "emptyProfile");
}

function GuiEditorHeaderBlock::build(%this)
{
	%this.rowFields = "";
	%this.valueFields = "";

	%this.buildIdentity();
	%this.buildToggles();
	%this.buildGeometry();
	%this.buildText();

	// The value grid starts empty; bindClass fills it, because what belongs
	// there is the one thing here that changes with the class.
	%this.valueGrid = %this.pane.makeCellGrid(0);
	%this.add(%this.valueGrid);
}

//-----------------------------------------------------------------------------
// Identity.
//-----------------------------------------------------------------------------

function GuiEditorHeaderBlock::buildIdentity(%this)
{
	%grid = %this.pane.makeCellGrid(0);
	%this.add(%grid);
	%this.identityGrid = %grid;

	%this.nameRow = %this.pane.addFieldRow(%grid, "name", "Name", "text", "");

	// Category sits between the name and the profile because that is the order
	// the two are decided in: what the control is, and then which of the theme's
	// profiles for that answers it. It is not a field on the control -- the
	// profile it picks is the record of the choice -- so it is built without a
	// name in the registry, and the pane intercepts its commits.
	%this.categoryRow = %this.pane.makeFieldRow(%grid, "category", "Category", "dropdown", "");

	%this.profileRow = %this.pane.addFieldRow(%grid, "Profile", "Profile", "dropdown", "");
}

//-----------------------------------------------------------------------------
// State toggles.
//-----------------------------------------------------------------------------

function GuiEditorHeaderBlock::buildToggles(%this)
{
	%w = %this.blockWidth;

	%row = new GuiControl()
	{
		HorizSizing = "width";
		Position = "0 0";
		Extent = %w SPC 28;
	};
	ThemeManager.setProfile(%row, "emptyProfile");
	%this.add(%row);
	%this.toggleRow = %row;

	// Toggles rather than buttons: these hold a value, so they are checkboxes
	// wearing an icon (GuiEditorToggleIcon). frameOn is the icon for the "on"
	// reading -- a closed padlock against an open one, and an x in a circle
	// against a tick. The x/tick pair is still a placeholder: an eye and a
	// struck-through eye is what "hidden" actually wants, and the sheet now
	// carries both ($EditorIcon::eye and ::invisible_light). Left alone here
	// because hidden is moving out to its own Explorer tree column, and that is
	// the change that should pick the icon.
	%this.hiddenButton = %this.makeIconToggle(%row, 4, "hidden",
		$EditorIcon::round_delete, $EditorIcon::round_checkmark,
		"Hidden in the editor", "Shown in the editor");
	%this.lockedButton = %this.makeIconToggle(%row, 32, "locked",
		$EditorIcon::padlock_closed, $EditorIcon::padlock_open,
		"Locked against editing", "Unlocked");

	// The four runtime flags, now that the sheet has art for them. They were
	// captioned checkboxes, which cost so much width that only two fitted and
	// useInput had to live in the Command section instead. Six icons fit where
	// two captions did.
	//
	// Two of the four have a genuine pair (an eye against a struck-through one,
	// on against off) and two do not, so those reuse one icon and let the
	// pressed state carry the reading -- the same thing the anchor pins do.
	%this.visibleButton = %this.makeIconToggle(%row, 68, "Visible",
		$EditorIcon::eye, $EditorIcon::invisible_light,
		"Draws when the game runs", "Does not draw when the game runs");
	%this.activeButton = %this.makeIconToggle(%row, 96, "Active",
		$EditorIcon::on, $EditorIcon::off,
		"Responds when the game runs", "Inert when the game runs");
	%this.inputButton = %this.makeIconToggle(%row, 124, "useInput",
		$EditorIcon::cursor_arrow, $EditorIcon::cursor_arrow,
		"Touch and key events reach this control",
		"Touch and key events pass straight through");
	%this.containerButton = %this.makeIconToggle(%row, 152, "isContainer",
		$EditorIcon::folder_open, $EditorIcon::folder,
		"The editor may drop controls into this one",
		"The editor will not drop controls into this one");
}

// A checkbox wearing an icon, which is what a toggle button is here. It holds
// its own state and refuses to change it while inactive; the pane is told what
// the value became.
function GuiEditorHeaderBlock::makeIconToggle(%this, %row, %x, %field, %frameOn, %frameOff, %tipOn, %tipOff)
{
	%button = new GuiCheckBoxCtrl()
	{
		class = "GuiEditorToggleIcon";
		Position = %x SPC 2;
		Extent = "24 24";
		frameOn = %frameOn;
		frameOff = %frameOff;
		tipOn = %tipOn;
		tipOff = %tipOff;
		toggleName = %field;
		owner = %this;
	};
	ThemeManager.setProfile(%button, "iconButtonProfile");
	ThemeManager.setProfile(%button, "tipProfile", "TooltipProfile");
	%row.add(%button);
	return %button;
}

// A toggle icon changed. It has already flipped itself, so the pane is handed
// the new value rather than being asked to work it out.
function GuiEditorHeaderBlock::onToggleIconChanged(%this, %toggle)
{
	%this.pane.onToggleChanged(%toggle.toggleName);
}

function GuiEditorHeaderBlock::makeToggleBox(%this, %row, %x, %w, %field, %label, %tip)
{
	%box = new GuiCheckBoxCtrl()
	{
		Position = %x SPC 3;
		Extent = %w SPC 22;
		Text = %label;
		boxOffset = "0 2";
		boxExtent = "18 18";
		textOffset = "22 2";
		textExtent = (%w - 22) SPC 18;
		Tooltip = %tip;
		Command = %this.getID() @ ".onToggleClicked(\"" @ %field @ "\");";
	};
	ThemeManager.setProfile(%box, "checkboxProfile");
	ThemeManager.setProfile(%box, "tipProfile", "TooltipProfile");
	%row.add(%box);

	%box.fieldName = %field;
	return %box;
}

function GuiEditorHeaderBlock::onToggleClicked(%this, %field)
{
	%this.pane.onToggleChanged(%field);
}

//-----------------------------------------------------------------------------
// Geometry. Which of these the control may edit is its parent's answer, so the
// pane recomputes it on selection AND on reparent, and passes the mode here.
//-----------------------------------------------------------------------------

function GuiEditorHeaderBlock::buildGeometry(%this)
{
	%grid = %this.pane.makeCellGrid(0);
	%this.add(%grid);
	%this.geometryGrid = %grid;

	%this.positionRow = %this.pane.addFieldRow(%grid, "Position", "Position", "point", "");
	%this.extentRow = %this.pane.addFieldRow(%grid, "Extent", "Extent", "point", "");

	// The two sizing enums share one widget, because they are one decision and
	// their names read backwards -- see GuiEditorAnchorPicker. It goes in the
	// same grid as a cell of its own so it reflows with everything else, and it
	// carries no authored width: the grid resizes every cell to the column it
	// computed, so a width set here would only be overwritten.
	%this.anchorPicker = new GuiControl()
	{
		class = "GuiEditorAnchorPicker";
		HorizSizing = "width";
		Position = "0 0";
		Extent = %this.pane.rowWidth SPC 124;
		owner = %this;
	};
	%grid.add(%this.anchorPicker);
	%this.anchorPicker.build();
}

// The picker changed. Both enums are written, because a single click can move
// either of them and writing only the one that visibly changed would leave the
// other stale after a Fill is cleared.
function GuiEditorHeaderBlock::onAnchorChanged(%this, %picker)
{
	%this.pane.onSizingChanged(%picker.horizEnum(), %picker.vertEnum());
}

// Grey rather than hide, and say why. A control sitting in a grid still HAS a
// position; it is just not the control's to set, and blanking the row would
// leave no way to see where the grid put it.
function GuiEditorHeaderBlock::applyGeometryMode(%this, %mode)
{
	%spec = %this.spec;
	%reason = "The parent container sets this.";

	%this.anchorPicker.setAxisEnabled(
		%spec.isGeometryFieldLive(%mode, "HorizSizing"),
		%spec.isGeometryFieldLive(%mode, "VertSizing"));
	%this.extentRow.setEnabled(%spec.isGeometryFieldLive(%mode, "Extent"), %reason);

	// Position is the one field whose two axes can disagree: a vertical chain
	// stacks its children on Y and copies X straight back from the child, so
	// half of this row is live. The row's own setEnabled works on both boxes at
	// once, so the axes are set directly -- the same two widgets it would touch.
	%axes = %spec.livePositionAxes(%mode);
	%liveX = strstr(%axes, "x") >= 0;
	%liveY = strstr(%axes, "y") >= 0;

	%this.positionRow.editor.setActive(%liveX);
	%this.positionRow.editorY.setActive(%liveY);
	%this.positionRow.editor.Tooltip = %liveX ? "" : %reason;
	%this.positionRow.editorY.Tooltip = %liveY ? "" : %reason;
}

//-----------------------------------------------------------------------------
// Text.
//-----------------------------------------------------------------------------

// The whole text story is one component now -- the string, the two flags that
// change what it does to the control, both alignments, and the size and colour
// it is drawn in. The header holds one copy of it and the pane's Text section
// holds the other; see GuiEditorTextBlock.
//
// textGrid, textRow, alignRow and vAlignRow stay as names for what the block
// owns, because they are how the pane and the smoke tests reach these widgets.
function GuiEditorHeaderBlock::buildText(%this)
{
	%this.textBlock = %this.pane.makeTextBlock(%this, 0);

	%this.textGrid = %this.textBlock;
	%this.textRow = %this.textBlock.textRow;
	%this.alignRow = %this.textBlock.alignRow;
	%this.vAlignRow = %this.textBlock.vAlignRow;
}

//-----------------------------------------------------------------------------
// Binding to a class. Everything above exists for every control; this decides
// which of it applies and what the value block holds.
//-----------------------------------------------------------------------------

function GuiEditorHeaderBlock::bindClass(%this, %ctrl, %class)
{
	%spec = %this.spec;
	%bare = %spec.hasFlag(%class, "bare");

	// A menu item has no GuiControl fields at all -- it calls
	// SimObject::initPersistFields rather than GuiControl's -- so it keeps its
	// name and its caption and loses everything else here.
	%this.profileRow.setVisible(!%bare);

	// Only one class in the palette is ambiguous enough to need this, and a
	// class with no profile at all cannot use it either.
	%this.categoryRow.setVisible(!%bare && %spec.categoryChoices(%class) !$= "");
	%this.geometryGrid.setVisible(!%bare);
	%this.visibleButton.setVisible(!%bare);
	%this.activeButton.setVisible(!%bare);
	%this.inputButton.setVisible(!%bare);

	// isContainer is dead where the control cannot draw children -- GuiControl's
	// setIsContainerFn forces the field false for those -- so the button goes
	// rather than sitting there wired to nothing.
	%this.containerButton.setVisible(%spec.isContainerFieldVisible(%ctrl));

	// The text block is only in the header for the classes whose text is a
	// principal property of them. A grid can technically draw text; it goes in a
	// collapsed section instead of the first thing anyone sees. The pane places
	// it and binds it -- it owns both copies.
	%this.textBlock.setVisible(%spec.textBlockHome(%class) $= "header");

	%this.buildValueRows(%ctrl, %class);
	%this.resizeToFit();
}

// The principal value: whatever the control is for. Rebuilt rather than
// filtered, because these fields are the one part of the header that does not
// exist on every class -- there is no shared row to hide.
function GuiEditorHeaderBlock::buildValueRows(%this, %ctrl, %class)
{
	%this.pane.clearRows(%this.valueFields);
	%this.valueGrid.deleteObjects();
	%this.valueFields = "";

	%fields = %this.spec.headerValueFields(%class);

	// A sprite names its picture three mutually exclusive ways, so the header
	// shows the one it is actually using rather than all three.
	if(%class $= "GuiSpriteCtrl")
	{
		%fields = %this.spec.spriteSourceFields(%this.spec.spriteSourceModeOf(%ctrl));
	}

	%count = getWordCount(%fields);
	for(%i = 0; %i < %count; %i++)
	{
		%field = getWord(%fields, %i);
		%this.pane.addFieldRow(%this.valueGrid, %field,
			%this.spec.labelFor(%field), %this.pane.kindFor(%ctrl, %field),
			%this.pane.enumItemsFor(%ctrl, %field));
		%this.valueFields = (%this.valueFields $= "") ? %field : (%this.valueFields SPC %field);
	}

	%this.valueGrid.setVisible(%count > 0);
}

// A GuiChainCtrl positions its children without resizing them, and a grid only
// learns its height once something lays it out, so nudge the width by a pixel
// and back to force exactly one parentResized through every child. Same trick
// GuiProfileEditorProfileForm::build uses, and needed here for the same reason.
function GuiEditorHeaderBlock::resizeToFit(%this)
{
	%w = getWord(%this.getExtent(), 0);
	%h = getWord(%this.getExtent(), 1);
	%this.resize(0, 0, %w + 1, %h);
	%this.resize(0, 0, %w, %h);
}
