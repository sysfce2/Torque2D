//-----------------------------------------------------------------------------
// The menus one editor puts on the shared bar.
//
// The bar is shared but the menus are not: File means "new Gui, open Gui, save
// Gui" in the Gui Editor and "new asset, save asset, revert asset" in the Asset
// Manager, and neither is a version of the other. So each editor owns a set,
// builds it once, and hands it to EditorCore.setEditorMenus when it opens. Only
// one set is ever on the bar; the rest are parked in a group of their own.
//
// Parking rather than greying is what makes the shortcuts right. The canvas
// keeps one flat list of accelerators built by walking whatever is on show, and
// it does not check whether an item is active before firing it - only whether
// the item ITSELF is, never its menu. Greying File therefore left Ctrl+N still
// running the Gui Editor's New Gui from inside the Asset Manager. An item that
// is not in the tree is not in that list at all.
//
// Subclass this: set class to your own name and superclass to EditorMenuSet,
// then define build(), which is called once with the bar ready to be added to,
// and refresh(), which is called every time the set goes back on the bar and
// whenever the editor's state changes underneath it.
//-----------------------------------------------------------------------------

function EditorMenuSet::init(%this)
{
	// Somewhere for the menus to live while another editor has the bar. A group
	// rather than nothing at all: a control taken out of its parent with no new
	// home is registered and unreachable, which is a leak wearing a disguise.
	%this.parked = new SimGroup();
	%this.menuCount = 0;

	%this.build();

	// Built into the bar, because that is the only place a menu can be built.
	// Nobody has opened this editor yet, so take them straight back off.
	%this.detach();
}

// Overridden by every subclass. Here to say so out loud when one forgets.
function EditorMenuSet::build(%this)
{
	warn("EditorMenuSet::build - " @ %this.class @ " has no build method, so its menu set is empty.");
}

// Overridden by any subclass with something to grey out. Called on attach, so
// the menus look new every time the editor is opened rather than carrying the
// state they had when it was last closed.
function EditorMenuSet::refresh(%this)
{
}

// A top-level menu, empty, already on the bar and ready to be filled.
function EditorMenuSet::addMenu(%this, %text)
{
	%menu = new GuiMenuItemCtrl()
	{
		Class = "EditorMenu";
		Text = %text;
		set = %this;
	};
	EditorCore.menuBar.add(%menu);

	%this.menu[%this.menuCount] = %menu;
	%this.menuCount++;

	return %menu;
}

function EditorMenuSet::attach(%this)
{
	for(%i = 0; %i < %this.menuCount; %i++)
	{
		EditorCore.menuBar.add(%this.menu[%i]);
	}

	%this.refresh();
}

function EditorMenuSet::detach(%this)
{
	for(%i = 0; %i < %this.menuCount; %i++)
	{
		%this.parked.add(%this.menu[%i]);
	}
}

//-----------------------------------------------------------------------------
// Groups: items that grey out together.
//-----------------------------------------------------------------------------

function EditorMenuSet::addToGroup(%this, %group, %item)
{
	// A group is named on the spot by whoever adds to it, so the first add finds
	// no count at all. Left as the empty string it reads as zero in the addition
	// below but writes the item to the index "" rather than 0, and the read back
	// in setGroupActive finds nothing there - the first item of every group would
	// silently never grey.
	%count = %this.groupCount[%group];
	if(%count $= "")
	{
		%count = 0;
	}

	%this.groupItem[%group, %count] = %item;
	%this.groupCount[%group] = %count + 1;
}

function EditorMenuSet::setGroupActive(%this, %group, %active)
{
	%count = %this.groupCount[%group];
	for(%i = 0; %i < %count; %i++)
	{
		%this.groupItem[%group, %i].setActive(%active);
	}
}

function EditorMenuSet::onRemove(%this)
{
	// If we are the set on show, come off it properly first: the bar's own
	// bookkeeping is repaired by the remove, and EditorCore stops pointing at
	// something about to stop existing.
	if(isObject(EditorCore) && EditorCore.activeMenus == %this)
	{
		EditorCore.setEditorMenus("");
	}

	for(%i = 0; %i < %this.menuCount; %i++)
	{
		if(isObject(%this.menu[%i]))
		{
			%this.menu[%i].delete();
		}
	}

	if(isObject(%this.parked))
	{
		%this.parked.delete();
	}
}
