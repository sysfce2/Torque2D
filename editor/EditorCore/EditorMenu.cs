//-----------------------------------------------------------------------------
// One menu on the shared bar, or one submenu inside another - they are the same
// control and the same class, which is why nesting needs nothing extra.
//
// Never construct one of these directly. EditorMenuSet::addMenu makes the
// top-level ones and addSubMenu makes the rest, and both exist to enforce the
// one rule this control has:
//
//   A MENU MUST BE CREATED EMPTY, PUT IN ITS PARENT, AND ONLY THEN FILLED.
//
// GuiMenuItemCtrl learns which bar it belongs to when it is added to one, and a
// submenu learns it from its parent at the moment IT is added. Nothing ever
// fills that in afterwards, so a tree built standalone and handed to the bar
// whole leaves every descendant with no bar at all - and the first time such a
// menu is opened, the engine dereferences it. The methods below add first and
// return the thing to be filled, so the rule is the shape of the code rather
// than something to remember.
//-----------------------------------------------------------------------------

// A command. %accelerator and %group are both optional.
//
// %group names a set of items that grey out together, and is the answer to menus
// like the Gui Editor's Layout, where thirteen items are not thirteen questions
// but one - is anything selected. The set flips a whole group in one call. Items
// whose state is their own are greyed through the handle this returns instead.
function EditorMenu::addItem(%this, %text, %command, %accelerator, %group)
{
	%item = new GuiMenuItemCtrl()
	{
		Text = %text;
		Command = %command;
		Accelerator = %accelerator;
	};
	%this.add(%item);

	if(%group !$= "")
	{
		%this.set.addToGroup(%group, %item);
	}

	return %item;
}

// A menu inside this one. Returned empty, to be filled the same way this was.
function EditorMenu::addSubMenu(%this, %text)
{
	%menu = new GuiMenuItemCtrl()
	{
		Class = "EditorMenu";
		Text = %text;
		set = %this.set;
	};
	%this.add(%menu);

	return %menu;
}

// A checkable item. Command runs when it is switched on and %altCommand when it
// is switched off, which is the engine's own split and not a convention we could
// change here.
function EditorMenu::addToggle(%this, %text, %command, %altCommand, %accelerator, %isOn)
{
	%item = new GuiMenuItemCtrl()
	{
		Text = %text;
		Toggle = "1";
		IsOn = %isOn;
		Command = %command;
		AltCommand = %altCommand;
		Accelerator = %accelerator;
	};
	%this.add(%item);

	return %item;
}

// The horizontal rule between groups of commands. The text IS the separator -
// the engine decides an item is one by finding "-" there when it is added, which
// is also why this has to go through the same add path as everything else.
function EditorMenu::addSeparator(%this)
{
	%item = new GuiMenuItemCtrl()
	{
		Text = "-";
	};
	%this.add(%item);

	return %item;
}
