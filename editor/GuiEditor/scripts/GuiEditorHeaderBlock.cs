
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
// The toggle row is four flags that change the Gui a player will run: whether a
// control draws, whether it responds, whether events reach it, and whether the
// editor may drop things into it.
//
// It used to be six. hidden and locked sat at the front, and they are not that
// kind of flag at all -- neither is ever written to a file, because both are
// working state rather than part of the document. Standing them next to the four
// that ARE the document read as a promise that they were too. They are the
// Explorer tree's two columns now, where a whole branch's state can be read at a
// glance and nothing suggests it will be saved.
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
	%this.buildMenuItem();

	// The value grid starts empty; bindClass fills it, because what belongs
	// there is the one thing here that changes with the class. Pair-width cells:
	// a principal value is usually one or two short fields, and a window's title
	// height wants its six switches beside it rather than under them.
	%this.valueGrid = %this.pane.makeCellGrid(0, %this.pane.pairWidth);
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

	// The four runtime flags, now that the sheet has art for them. They were
	// captioned checkboxes, which cost so much width that only two fitted and
	// useInput had to live in the Command section instead. Four icons fit in
	// less than two captions did.
	//
	// Two of the four have a genuine pair (an eye against a struck-through one,
	// on against off) and two do not, so those reuse one icon and let the
	// pressed state carry the reading -- the same thing the anchor pins do.
	//
	// They start at 4 now. There used to be a gap here, between these and the two
	// editor-only toggles that ran in front of them; the gap was the boundary
	// between "changes the game" and "changes your view of it", and with hidden
	// and locked gone to the Explorer tree there is no boundary left to mark.
	%this.visibleButton = %this.makeIconToggle(%row, 4, "Visible", "Visible",
		$EditorIcon::eye, $EditorIcon::invisible_light,
		"Draws when the game runs. Its children draw with it -- hiding a control hides everything inside it.",
		"Does not draw when the game runs, and neither do its children. A layout container skips a hidden control entirely, so hiding one can move its siblings.");
	%this.activeButton = %this.makeIconToggle(%row, 32, "Active", "Active",
		$EditorIcon::on, $EditorIcon::off,
		"Responds when the game runs: it takes clicks and keys and draws in its ordinary colors.",
		"Inert when the game runs. It still draws, in the profile's disabled colors, but ignores every click and key.");
	%this.inputButton = %this.makeIconToggle(%row, 60, "useInput", "Accepts Input",
		$EditorIcon::cursor_arrow, $EditorIcon::cursor_arrow,
		"Touch and key events reach this control.",
		"Touch and key events pass straight through to whatever is behind it. Turn this off on a backdrop or a label so it cannot swallow clicks meant for something else.");
	%this.containerButton = %this.makeIconToggle(%row, 88, "isContainer", "Accepts Children",
		$EditorIcon::folder_open, $EditorIcon::folder,
		"The editor drops controls into this one when you draw them over it.",
		"The editor never drops controls into this one. They land in its parent instead, on top of it.");
}

// A checkbox wearing an icon, which is what a toggle button is here. It holds
// its own state and refuses to change it while inactive; the pane is told what
// the value became.
function GuiEditorHeaderBlock::makeIconToggle(%this, %row, %x, %field, %label, %frameOn, %frameOff, %tipOn, %tipOff)
{
	%button = new GuiCheckBoxCtrl()
	{
		class = "EditorToggleIcon";
		Position = %x SPC 2;
		Extent = "24 24";
		frameOn = %frameOn;
		frameOff = %frameOff;
		tipOn = %tipOn;
		tipOff = %tipOff;
		toggleName = %field;
		toggleLabel = %label;
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
	%this.pane.onToggleChanged(%toggle.toggleName, %toggle.getValue());
}

//-----------------------------------------------------------------------------
// A window's six switches. They were a section of six captioned checkboxes;
// as icons they are one cell, which fits beside Title Height in the value
// block -- the same trade the state toggles made in the row above.
//-----------------------------------------------------------------------------

function GuiEditorHeaderBlock::buildWindowToggles(%this, %grid)
{
	%size = 24;
	%gap = 2;
	%fields = %this.spec.windowToggles();

	%row = new GuiControl()
	{
		Position = "0 0";
		Extent = (getWordCount(%fields) * (%size + %gap)) SPC (%size + 4);
	};
	ThemeManager.setProfile(%row, "emptyProfile");
	%grid.add(%row);

	// field TAB label TAB icon TAB on TAB off
	%table =
		"canMove"      TAB "Move"          TAB $EditorIcon::cursor_drag_arrow TAB
			"The player can drag the window by its title bar." TAB
			"The window stays where it is put. Use this for a dialog that should not be moved off what it is explaining." NL
		"canClose"     TAB "Close"         TAB $EditorIcon::app_window_cross TAB
			"The title bar carries a close button." TAB
			"No close button. The game has to take the window down itself, which is what a modal dialog with its own buttons wants." NL
		"canMinimize"  TAB "Minimize"      TAB $EditorIcon::round_minus TAB
			"The title bar carries a minimise button, which rolls the window up to its title." TAB
			"No minimise button." NL
		"canMaximize"  TAB "Maximize"      TAB $EditorIcon::expand TAB
			"The title bar carries a maximise button, which fills the window's parent." TAB
			"No maximise button." NL
		"resizeWidth"  TAB "Resize Width"  TAB $EditorIcon::arrow_two_head TAB
			"The player can drag the window's left and right edges." TAB
			"The width is fixed at whatever the Gui was saved with." NL
		"resizeHeight" TAB "Resize Height" TAB $EditorIcon::arrow_two_head_2 TAB
			"The player can drag the window's top and bottom edges." TAB
			"The height is fixed at whatever the Gui was saved with.";

	%count = getRecordCount(%table);
	for(%i = 0; %i < %count; %i++)
	{
		%rec = getRecord(%table, %i);
		%field = getField(%rec, 0);
		%this.windowButton[%field] = %this.makeIconToggle(%row, %i * (%size + %gap),
			%field, getField(%rec, 1), getField(%rec, 2), getField(%rec, 2),
			getField(%rec, 3), getField(%rec, 4));
	}

	%this.windowToggleRow = %row;
}

// Load the six from the control, when the bound class has them.
function GuiEditorHeaderBlock::refreshWindowToggles(%this, %ctrl)
{
	if(!isObject(%this.windowToggleRow))
	{
		return;
	}

	%fields = %this.spec.windowToggles();
	for(%i = 0; %i < getWordCount(%fields); %i++)
	{
		%field = getWord(%fields, %i);
		%this.windowButton[%field].setValue(%ctrl.getFieldValue(%field));
	}
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

	// A field the control's own class rewrites every frame is not greyed but
	// gone: there is no value to look at and nothing a tooltip could usefully
	// say about it. A menu bar is always at the top of what it is in.
	%this.positionRow.setVisible(%spec.isGeometryFieldShown(%mode, "Position"));
	%this.extentRow.setVisible(%spec.isGeometryFieldShown(%mode, "Extent"));
	%this.anchorPicker.setVisible(%spec.isGeometryFieldShown(%mode, "HorizSizing"));

	%this.anchorPicker.setAxisEnabled(
		%spec.isGeometryFieldLive(%mode, "HorizSizing"),
		%spec.isGeometryFieldLive(%mode, "VertSizing"));

	// Position and Extent are the two fields whose axes can disagree. A vertical
	// chain stacks its children on Y and copies X straight back from the child;
	// a menu bar's width is its parent's and its height is its own. The rows'
	// own setEnabled works on both boxes at once, so the axes are set directly --
	// the same two widgets it would touch.
	%this.applyAxes(%this.positionRow, %spec.livePositionAxes(%mode), %reason);
	%this.applyAxes(%this.extentRow, %spec.liveExtentAxes(%mode), %reason);
}

function GuiEditorHeaderBlock::applyAxes(%this, %row, %axes, %reason)
{
	%liveX = strstr(%axes, "x") >= 0;
	%liveY = strstr(%axes, "y") >= 0;

	%row.editor.setActive(%liveX);
	%row.editorY.setActive(%liveY);
	%row.editor.Tooltip = %liveX ? "" : %reason;
	%row.editorY.Tooltip = %liveY ? "" : %reason;
}

//-----------------------------------------------------------------------------
// Text.
//-----------------------------------------------------------------------------

// The whole text story is one component now -- the string, the two flags that
// change what it does to the control, both alignments, and the size and color
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

	// Not simply !bare: a bare class has none of GuiControl's fields except the
	// ones it turns round and registers again, and a menu item does that with
	// Visible and Active. useInput it genuinely does not have.
	%states = %spec.stateToggles(%class);
	%this.visibleButton.setVisible(%spec.listHas(%states, "Visible"));
	%this.activeButton.setVisible(%spec.listHas(%states, "Active"));
	%this.inputButton.setVisible(%spec.listHas(%states, "useInput"));

	// isContainer is dead where the control cannot draw children -- GuiControl's
	// setIsContainerFn forces the field false for those -- so the button goes
	// rather than sitting there wired to nothing.
	%this.containerButton.setVisible(%spec.isContainerFieldVisible(%ctrl));

	// The text block is only in the header for the classes whose text is a
	// principal property of them. A grid can technically draw text; it goes in a
	// collapsed section instead of the first thing anyone sees. The pane places
	// it and binds it -- it owns both copies.
	%this.textBlock.setVisible(%spec.textBlockHome(%class) $= "header");

	// A menu item's own fields, which are the only ones it has. It brings its own
	// caption box, so the shared text block stands down for it.
	%menuItem = (%class $= "GuiMenuItemCtrl");
	%this.menuItemBlock.setVisible(%menuItem);
	if(%menuItem)
	{
		%this.textBlock.setVisible(false);
		%this.menuItemBlock.bind(%ctrl);
	}

	%this.buildValueRows(%ctrl, %class);
	%this.resizeToFit();
}

//-----------------------------------------------------------------------------
// The menu item block. Its own file, because a menu item shares almost nothing
// with anything else in the palette - see GuiEditorMenuItemBlock.
//-----------------------------------------------------------------------------

function GuiEditorHeaderBlock::buildMenuItem(%this)
{
	%this.menuItemBlock = new GuiChainCtrl()
	{
		class = "GuiEditorMenuItemBlock";
		HorizSizing = "width";
		Position = "0 0";
		Extent = %this.blockWidth SPC 24;
		IsVertical = true;
		pane = %this.pane;
		spec = %this.spec;
		blockWidth = %this.blockWidth;
		Visible = false;
	};
	%this.add(%this.menuItemBlock);
	%this.menuItemBlock.build();
}

// The principal value: whatever the control is for. Rebuilt rather than
// filtered, because these fields are the one part of the header that does not
// exist on every class -- there is no shared row to hide.
function GuiEditorHeaderBlock::buildValueRows(%this, %ctrl, %class)
{
	%this.pane.clearRows(%this.valueFields);
	%this.valueGrid.deleteObjects();
	%this.valueFields = "";
	%this.windowToggleRow = "";

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

	// A window's switches go in the same grid as its Title Height, so the two
	// share a line rather than costing a section of their own.
	if(%class $= "GuiWindowCtrl")
	{
		%this.buildWindowToggles(%this.valueGrid);
		%count++;
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
