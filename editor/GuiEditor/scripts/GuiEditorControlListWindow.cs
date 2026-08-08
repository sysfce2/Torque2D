//GuiEditorControlListWindow.cs
//
// The control palette. Two view buttons at the top, then the placeable controls
// as collapsible groups of icon tiles.
//
// What this replaced: a GuiListBoxCtrl of raw class names, which told a person
// nothing about what any of them looked like. The class names now live in the
// tooltips and the pictures do the work.
//
//   controls   GuiEditorControlIcons, generated alongside the icon sheets
//   groups     one GuiEditorControlGroup per section, stacked in a chain
//   tiles      one GuiEditorControlTile per entry, inside each group's grid

$GuiEditorControlListWindow::modeBarHeight = 28;

function GuiEditorControlListWindow::onAdd(%this)
{
	%this.mode = "grid";
	%this.groupCount = 0;

	%this.buildModeRow();

	// Built filling the whole content rect, then moved down under the mode bar by
	// fitScroller. Fill is the only way to find out how big that rect is: it is
	// the window's extent less the title bar and whatever borders the profile
	// asks for, and script cannot ask for any of those numbers.
	%this.scroller = new GuiScrollCtrl()
	{
		HorizSizing = "fill";
		VertSizing = "fill";
		Position = "0 0";
		Extent = "242 327";
		hScrollBar = "alwaysOff";
		vScrollBar = "dynamic";
		constantThumbHeight = "0";
		showArrowButtons = "1";
		scrollBarThickness = "14";
	};
	ThemeManager.setProfile(%this.scroller, "scrollingPanelProfile");
	ThemeManager.setProfile(%this.scroller, "scrollingPanelThumbProfile", "ThumbProfile");
	ThemeManager.setProfile(%this.scroller, "scrollingPanelTrackProfile", "TrackProfile");
	ThemeManager.setProfile(%this.scroller, "scrollingPanelArrowProfile", "ArrowProfile");
	%this.add(%this.scroller);
	%this.fitScroller();

	// Fill across, and nothing about widths anywhere below.
	//
	// The scroller's horizontal bar is alwaysOff, so across is an axis that does
	// not scroll: the room is bounded and the engine knows exactly what it is --
	// the inner rect, less the vertical bar when that is showing. Fill asks for
	// precisely that, and keeps asking as the frame is dragged and as the bar
	// comes and goes. Down is left alone; that axis scrolls, so the chain is as
	// tall as its groups and GuiScrollCtrl refuses fill there.
	%this.groupChain = new GuiChainCtrl()
	{
		HorizSizing = "fill";
		Position = "0 0";
		Extent = "228 4";
		IsVertical = true;
		ChildSpacing = 2;
	};
	ThemeManager.setProfile(%this.groupChain, "emptyProfile");
	%this.scroller.add(%this.groupChain);

	%this.populate();
}

// Put the scroller under the mode bar without ever naming a border thickness.
//
// A fixed bar above a stretching pane has no sizing flag of its own: "fill"
// takes the whole inner rect and throws the position away, so the bar ends up
// underneath it, and "height" keeps the gaps the authored extent happened to
// start with -- which means guessing the title height and the border sizes, the
// exact guess that left every one of these windows clipping its last eight
// pixels when the title bar grew from 20 to 28.
//
// So let the engine measure instead: the scroller is built filling, one resize
// pass makes that real, and what it reports back IS the content rect. Take the
// bar off the top of it and switch to "height", which from then on holds the top
// edge where it was put and lets the bottom follow the window.
function GuiEditorControlListWindow::fitScroller(%this)
{
	%w = getWord(%this.getExtent(), 0);
	%h = getWord(%this.getExtent(), 1);

	// Nudge the width by a pixel and back: one parentResized through every child,
	// widths unchanged. Same move as GuiEditorInspectorPane::forceLayout.
	%this.resize(0, 0, %w + 1, %h);
	%this.resize(0, 0, %w, %h);

	%inner = %this.scroller.getExtent();
	%bar = $GuiEditorControlListWindow::modeBarHeight;

	%this.scroller.HorizSizing = "width";
	%this.scroller.VertSizing = "height";
	%this.scroller.resize(0, %bar, getWord(%inner, 0), getWord(%inner, 1) - %bar);
}

function GuiEditorControlListWindow::onRemove(%this)
{
	// The mode row and the scroller are this window's two children; the groups
	// and their tiles hang off the scroller and go with it.
	if(isObject(%this.modeRow))
	{
		%this.modeRow.delete();
	}
	if(isObject(%this.scroller))
	{
		%this.scroller.delete();
	}
}

//-----------------------------------------------------------------------------
// The view switch. GuiEditorChoiceRow is already "a radio group that looks like
// a segmented control" -- a row of toggle buttons of which exactly one is down,
// which is exactly this. It carries a caption by default; two icons say enough
// on their own, so the label width goes to nothing.
//-----------------------------------------------------------------------------

function GuiEditorControlListWindow::buildModeRow(%this)
{
	%this.modeRow = new GuiControl()
	{
		class = "GuiEditorChoiceRow";
		HorizSizing = "width";
		Position = "4 2";
		labelText = "";

		// 4, not 0: build() sizes its caption at labelWidth - 4, so a zero here
		// asks for a control four pixels wide in the wrong direction.
		labelWidth = 4;
		owner = %this;
		fieldName = "mode";
	};
	%this.add(%this.modeRow);

	%this.modeRow.addChoice("grid", $EditorIcon::grid_2x2, "Show controls as a grid of large icons");
	%this.modeRow.addChoice("rows", $EditorIcon::list_bullets, "Show controls as a compact list");
	%this.modeRow.build();
	%this.modeRow.setValue(%this.mode);
}

function GuiEditorControlListWindow::onChoiceRowChanged(%this, %row)
{
	%this.setMode(%row.getValue());
}

function GuiEditorControlListWindow::setMode(%this, %mode)
{
	%this.mode = %mode;

	for(%i = 0; %i < %this.groupCount; %i++)
	{
		%this.group[%i].setMode(%mode);
	}

	%this.relayout();
}

// Each group has to be told to remeasure, and then the chain has to be told its
// children changed height. Neither happens on its own: a chain positions its
// children without resizing them, and a panel only learns its height from a
// parentResized it would otherwise never receive.
//
// No widths here. The chain fills the scroller, and the groups follow the chain,
// so how much room the vertical bar leaves is the engine's answer to give -- see
// GuiScrollCtrl::getInnerRect and preventUnsizedModes. This used to measure that
// room in script and hand it out, which held only until the frame was next
// dragged: "width" sizing adds the parent's CHANGE to a child's own width, so an
// authored number is offset forever and never replaced. tests/smoke/palette.cs
// sweeps the window across 31 widths rather than checking one, because a single
// static check is exactly what all three script attempts passed.
function GuiEditorControlListWindow::relayout(%this)
{
	for(%i = 0; %i < %this.groupCount; %i++)
	{
		%this.group[%i].forceLayout();
	}

	%w = getWord(%this.groupChain.getExtent(), 0);
	%h = getWord(%this.groupChain.getExtent(), 1);
	%this.groupChain.resize(0, 0, %w, %h);
}

//-----------------------------------------------------------------------------
// Filling it.
//
// The generated table decides the order and the grouping. Then anything the
// engine registers that the table has never heard of is swept into a group of
// its own wearing the question-mark icon -- so a control class added after the
// icons were generated still appears and can still be placed. It just has no
// picture yet.
//-----------------------------------------------------------------------------

function GuiEditorControlListWindow::populate(%this)
{
	%icons = GuiEditor.controlIcons;
	%groups = %icons.groups();

	for(%g = 0; %g < getFieldCount(%groups); %g++)
	{
		%name = getField(%groups, %g);
		%group = %this.addGroup(%name);

		%keys = %icons.keysInGroup(%name);
		for(%i = 0; %i < getFieldCount(%keys); %i++)
		{
			%group.addTile(getField(%keys, %i));
		}

		// A GuiExpandCtrl starts collapsed (mExpanded is false in the
		// constructor), and it measures its open height from the children it has
		// at the moment it opens -- so this has to come after the tiles, not with
		// the panel.
		%group.setExpanded(true);
	}

	%this.addUndrawnClasses();
	%this.setMode(%this.mode);
}

function GuiEditorControlListWindow::addGroup(%this, %title)
{
	%group = new GuiPanelCtrl()
	{
		class = "GuiEditorControlGroup";

		// Width, NOT fill, however much a group is meant to be exactly as wide as
		// the chain. A GuiPanelCtrl sizes itself to its children --
		// GuiExpandCtrl::parentResized ends by writing mExpandedExtent straight
		// into mBounds.extent -- and a direct write like that goes around resize,
		// which is the only thing that honours fill. Asked to fill, a panel is
		// clamped and then immediately overwrites the clamp with its own answer.
		HorizSizing = "width";
		Position = "0 0";
		Extent = "242" SPC $GuiEditorControlGroup::headerHeight;
		MinExtent = "80" SPC $GuiEditorControlGroup::headerHeight;
		Text = %title;
		command = "";
		owner = %this;
	};
	ThemeManager.setProfile(%group, "panelProfile");
	%this.groupChain.add(%group);

	%this.group[%this.groupCount] = %group;
	%this.groupCount++;

	return %group;
}

// The runtime sweep, kept as a tail rather than as the whole list. Both the
// filter and the table come from GuiEditorControlIcons, which is generated from
// the same rule the sheets are built with -- so the sweep cannot disagree with
// the table about what counts as placeable, and this does not have to reach
// across into another window to ask.
function GuiEditorControlListWindow::addUndrawnClasses(%this)
{
	%icons = GuiEditor.controlIcons;
	%classes = enumerateConsoleClasses("GuiControl");
	%group = 0;

	for(%i = 0; %i < getFieldCount(%classes); %i++)
	{
		%name = trim(getField(%classes, %i));

		// coversClass, not isKnown: the four GuiControl entries are keyed
		// "GuiControl:Empty" and so on, so asking whether "GuiControl" is a known
		// KEY says no and would add a fifth, iconless copy of it.
		if(!%icons.isPlaceableClass(%name) || %icons.coversClass(%name))
		{
			continue;
		}

		// Built only if something turns up, so the ordinary case shows four
		// groups rather than five with an empty one.
		if(!isObject(%group))
		{
			%group = %this.addGroup("Undrawn");
		}
		%group.addTile(%name);
	}
}
