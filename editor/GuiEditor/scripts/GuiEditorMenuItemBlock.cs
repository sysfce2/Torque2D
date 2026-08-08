
//-----------------------------------------------------------------------------
// Everything about a menu item, in the header.
//
// A GuiMenuItemCtrl is unlike anything else in the palette: it calls
// SimObject::initPersistFields rather than GuiControl's, so it has no profile,
// no geometry, no tooltip and no sizing - and then registers a handful of
// fields of its own. What is left is a header with a caption in it and nothing
// underneath, which is why those fields live up here rather than in a section
// of their own. There is nothing to scroll past to reach them.
//
// It owns its own caption box for the same reason. The shared GuiEditorTextBlock
// is a multi-line box with wrap, extend, alignment and font rows attached - all
// of which a menu item hides - and a menu caption is one short line that decides
// how wide the menu is. A box three lines tall for a word like "File" says the
// wrong thing about what belongs there.
//
// The block owns its widgets and no values: every row reports here and this
// forwards to the pane, so the pane stays the only thing that writes to the
// control. The creator sets pane, spec and blockWidth inline, then calls build()
// once after adding it.
//-----------------------------------------------------------------------------

function GuiEditorMenuItemBlock::onAdd(%this)
{
	ThemeManager.setProfile(%this, "emptyProfile");
}

function GuiEditorMenuItemBlock::build(%this)
{
	%this.typing = false;

	%this.grid = %this.pane.makeCellGrid(0, %this.pane.rowWidth);
	%this.add(%this.grid);

	%this.buildCaption();
	%this.buildKind();
	%this.buildCommands();

	// Visible and Active are not here. They are GuiControl's names, and a menu
	// item registers them again for itself, so the header's own toggle row can
	// show them - see GuiEditorControlSpec::stateToggles. A second pair of
	// switches a few pixels below the first would be worse than no switches.
}

//-----------------------------------------------------------------------------
// The caption. One line, because that is what a menu label is - and because for
// a top-level menu it is also the width of the menu, which the bar recomputes
// from the text on every keystroke.
//-----------------------------------------------------------------------------

function GuiEditorMenuItemBlock::buildCaption(%this)
{
	%row = %this.pane.makeFieldRow(%this.grid, "text", "Caption", "text", "");
	%this.textRow = %row;

	// Command is free on a field row -- it commits on AltCommand (blur) and
	// ReturnCommand -- and GuiTextEditCtrl runs Command on every edit to its
	// buffer. So this is the per-keystroke hook, and it is what makes the menu on
	// the canvas grow and shrink under the cursor as the caption is typed.
	%row.editor.Command = %this.getID() @ ".onCaptionTyped();";
}

// Straight onto the control rather than through the pane's writeField: this runs
// per character, and an edit is one change however many keys it took. The undo
// step is written once, on commit, from the value stashed here.
function GuiEditorMenuItemBlock::onCaptionTyped(%this)
{
	if(%this.populating || !isObject(%this.pane.target))
	{
		return;
	}

	%ctrl = %this.pane.target;

	// Taken on the first keystroke, because that is the last moment the control
	// still holds what the edit started from.
	if(!%this.typing)
	{
		%this.typing = true;
		%this.typingTarget = %ctrl;
		%this.textBeforeEdit = %ctrl.getFieldValue("text");
	}

	%ctrl.text = %this.textRow.getValue();

	// Cheap -- the explorer tree refreshes the one row's label -- and it keeps
	// the tree reading the same as the canvas while you type.
	%this.pane.afterCommit();
}

function GuiEditorMenuItemBlock::endTyping(%this)
{
	%this.typing = false;
	%this.typingTarget = "";
	%this.textBeforeEdit = "";
}

//-----------------------------------------------------------------------------
// What picking the item does. Toggle and Radio are two bool fields but one
// decision -- a menu item is a plain command, a checkable one, or one of a set
// -- so they are one control here, and each writes both fields.
//-----------------------------------------------------------------------------

function GuiEditorMenuItemBlock::buildKind(%this)
{
	%row = new GuiControl()
	{
		class = "EditorChoiceRow";
		labelText = "Kind";
		labelWidth = 76;
		fieldName = "kind";
		owner = %this;
	};

	%row.addChoice("command", $EditorIcon::list_bullets,
		"Runs its command and closes the menu. The ordinary kind.");
	%row.addChoice("toggle", $EditorIcon::checkbox_checked,
		"Carries a tick that turns on and off, like a Show Grid switch.");
	%row.addChoice("radio", $EditorIcon::round,
		"One of a run of items where only one is on at a time. The run is the items either side of it, so keep them together.");
	%row.addChoice("spacer", $EditorIcon::round_minus,
		"A rule across the menu, to group what is above it apart from what is below. It is not pickable and runs no command. Written as a single dash in the caption, which is what makes one in a .gui file too.");

	%this.grid.add(%row);
	%row.build();
	%this.kindRow = %row;

	%this.onRow = %this.pane.makeFieldRow(%this.grid, "IsOn", "Starts On", "bool", "");
}

// The chooser changed. Two fields, so one transaction: the C++ setters each
// rewrite mDisplayType, and the one being turned OFF has to go first or it
// undoes the one being turned on.
function GuiEditorMenuItemBlock::onChoiceRowChanged(%this, %row)
{
	if(%this.populating || !isObject(%this.pane.target))
	{
		return;
	}

	%kind = %row.getValue();
	%wasSpacer = (%this.pane.target.getFieldValue("text") $= "-");

	GuiEditor.undoRecorder.begin("Menu Item Kind", "");
	if(%kind $= "toggle")
	{
		%this.pane.writeField("Radio", false);
		%this.pane.writeField("Toggle", true);
	}
	else if(%kind $= "radio")
	{
		%this.pane.writeField("Toggle", false);
		%this.pane.writeField("Radio", true);
	}
	else
	{
		%this.pane.writeField("Toggle", false);
		%this.pane.writeField("Radio", false);
	}

	// A separator has no field of its own: a single dash in the caption is the
	// whole of it, in a .gui file and here (GuiMenuItemCtrl::setText). So the
	// kind is written by writing the caption, and leaving it means clearing the
	// dash - otherwise picking Command off a separator would leave a separator.
	if(%kind $= "spacer")
	{
		%this.pane.writeField("text", "-");
	}
	else if(%wasSpacer)
	{
		%this.pane.writeField("text", "");
	}
	GuiEditor.undoRecorder.end();

	%this.refreshKind();
	%this.pane.afterCommit();
}

// What the rest of the block has to say depends on the kind. "Starts On" only
// means something where there is a mark to start on, and a separator is not
// pickable at all - so it runs nothing, answers to nothing, and is not named.
function GuiEditorMenuItemBlock::refreshKind(%this)
{
	%kind = %this.kindRow.getValue();
	%spacer = (%kind $= "spacer");

	%this.onRow.setVisible(!%spacer && %kind !$= "command");

	%this.textRow.setVisible(!%spacer);
	%this.commandRow.setVisible(!%spacer);
	%this.altCommandRow.setVisible(!%spacer);
	%this.acceleratorRow.setVisible(!%spacer);
	%this.variableRow.setVisible(!%spacer);

	%this.resizeToFit();
}

//-----------------------------------------------------------------------------
// What picking it runs.
//-----------------------------------------------------------------------------

function GuiEditorMenuItemBlock::buildCommands(%this)
{
	%this.commandRow = %this.pane.makeFieldRow(%this.grid, "Command", "Command", "text", "");
	%this.altCommandRow = %this.pane.makeFieldRow(%this.grid, "AltCommand", "Alt Command", "text", "");
	%this.acceleratorRow = %this.pane.makeFieldRow(%this.grid, "Accelerator", "Shortcut", "text", "");
	%this.variableRow = %this.pane.makeFieldRow(%this.grid, "Variable", "Variable", "text", "");
}

//-----------------------------------------------------------------------------
// Loading and writing.
//-----------------------------------------------------------------------------

function GuiEditorMenuItemBlock::bind(%this, %ctrl)
{
	%this.populating = true;

	%this.textRow.setValue(%ctrl.getFieldValue("text"));
	%this.commandRow.setValue(%ctrl.getFieldValue("Command"));
	%this.altCommandRow.setValue(%ctrl.getFieldValue("AltCommand"));
	%this.acceleratorRow.setValue(%ctrl.getFieldValue("Accelerator"));
	%this.variableRow.setValue(%ctrl.getFieldValue("Variable"));
	%this.onRow.setValue(%ctrl.getFieldValue("IsOn"));

	// Only inside a menu. A rule across the menu BAR would have nothing either
	// side of it to separate, so the engine does not make one there
	// (GuiMenuItemCtrl::setText) and this must not offer it either.
	%parent = %ctrl.getParent();
	%inMenu = isObject(%parent) && %parent.isMemberOfClass("GuiMenuItemCtrl");
	%this.kindRow.setChoiceVisible("spacer", %inMenu);

	// The caption is asked first, because a dash outranks the other two: an item
	// carrying one is a separator whatever Toggle and Radio happen to hold.
	%kind = "command";
	if(%inMenu && %ctrl.getFieldValue("text") $= "-")
	{
		%kind = "spacer";
	}
	else if(%ctrl.getFieldValue("Toggle"))
	{
		%kind = "toggle";
	}
	else if(%ctrl.getFieldValue("Radio"))
	{
		%kind = "radio";
	}
	%this.kindRow.setValue(%kind);

	%this.populating = false;

	%this.refreshKind();
}

// Every row in this block reports here. The caption is the one that has already
// written itself, per keystroke, so it puts the old value back and writes it
// once - which is what makes an edit one step on the undo stack however many
// keys it took.
function GuiEditorMenuItemBlock::onProfileRowCommit(%this, %row)
{
	if(%this.populating || !isObject(%this.pane.target))
	{
		return;
	}

	%field = %row.fieldName;

	if(%field $= "text")
	{
		%value = %row.getValue();
		%before = %this.textBeforeEdit;
		%wasTyping = %this.typing;
		%this.endTyping();
		%row.markClean();

		if(!%wasTyping)
		{
			return;
		}

		%this.pane.target.text = %before;
		%this.pane.writeField("text", %value);

		// Typing a dash is the documented way to make a separator, so the chooser
		// has to notice one arriving that way as well as being picked.
		%this.bind(%this.pane.target);
		%this.pane.afterCommit();
		return;
	}

	if(!%row.hasChanged())
	{
		return;
	}

	%row.markClean();
	%this.pane.writeField(%field, %row.getValue());
	%this.pane.afterCommit();
}

// Nothing here has a reset button, so this is only ever the row asking politely.
function GuiEditorMenuItemBlock::onProfileRowReset(%this, %row)
{
}

// Nudge the width and put it back, which is one parentResized through every
// child -- the same trick the header block and the pane use to re-measure a
// chain after something in it was shown or hidden.
function GuiEditorMenuItemBlock::resizeToFit(%this)
{
	%w = getWord(%this.getExtent(), 0);
	%h = getWord(%this.getExtent(), 1);
	%this.resize(0, 0, %w + 1, %h);
	%this.resize(0, 0, %w, %h);
}
