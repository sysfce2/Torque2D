

function GuiEditorInspectorWindow::onAdd(%this)
{
    // Fill rather than width/height -- the window's only child wants its whole
    // content rect. See GuiEditorControlListWindow for why.
    %this.scroller = new GuiScrollCtrl()
	{
		HorizSizing="fill";
		VertSizing="fill";
		Position="0 0";
		Extent="352 354";
		hScrollBar="alwaysOff";
		vScrollBar="alwaysOn";
		constantThumbHeight="0";
		showArrowButtons="1";
		scrollBarThickness="14";
	};
	ThemeManager.setProfile(%this.scroller, "emptyProfile");
	ThemeManager.setProfile(%this.scroller, "thumbProfile", "ThumbProfile");
	ThemeManager.setProfile(%this.scroller, "trackProfile", "TrackProfile");
	ThemeManager.setProfile(%this.scroller, "scrollArrowProfile", "ArrowProfile");
	%this.add(%this.scroller);

    // The custom pane that replaced the native GuiInspector. The inspector is
    // still a live C++ class -- the Asset Admin uses one -- but it has no place
    // here: it reflected every registered field, which for a Gui control means
    // offering fields the class provably never reads.
    %this.pane = new GuiChainCtrl()
	{
		class = "GuiEditorInspectorPane";
		HorizSizing = "width";
		Position = "0 0";
		Extent = "338 354";
		IsVertical = true;
		ChildSpacing = 6;
		paneWidth = 338;
		window = %this;
	};
	%this.scroller.add(%this.pane);
	%this.pane.build();

    %this.inspectList = new SimSet();
}

function GuiEditorInspectorWindow::onRemove(%this)
{
    if(isObject(%this.inspectList))
    {
        %this.inspectList.deleteObjects();
        %this.inspectList.delete();
    }
}

// Edit arrives after every drag and every resize on the canvas, not only on a
// fresh selection (guiEditCtrl.cc calls it from the mouse-up that ends both).
// Rebinding on each of those would rebuild the class sections while the user is
// still dragging, so an Edit for the control already bound reloads geometry and
// nothing else.
function GuiEditorInspectorWindow::onEdit(%this, %object)
{
    if(isObject(%object) && %object == %this.pane.target)
    {
        %this.pane.refreshGeometry();
        return;
    }
    %this.pane.bind(%object);
}

// Something re-profiled the control under the pane: a fresh drop being themed
// on arrival, or Set Theme sweeping the whole document. The pane caches what it
// found -- which slots were worth a Variants row, and what each drop-down
// offers -- so a re-profile behind its back leaves it describing the control
// the way it used to be.
//
// This is what a dropped control needs, because it is announced (and inspected)
// wearing its constructor's profiles and only themed afterwards.
function GuiEditorInspectorWindow::onRethemed(%this, %object)
{
    if(isObject(%this.pane.target))
    {
        %this.pane.bind(%this.pane.target);
    }
}

// Reparenting changes which geometry fields the control is allowed to edit --
// a chain, grid, frame set or tab book writes its children's bounds itself --
// so the pane has to re-evaluate against the new parent. The brain has always
// posted this event; nothing listened to it until now.
function GuiEditorInspectorWindow::onParentChange(%this, %parent)
{
    if(isObject(%this.pane.target))
    {
        %this.pane.bind(%this.pane.target);
    }
}

function GuiEditorInspectorWindow::onClearInspect(%this, %object)
{
    if(isObject(%object))
    {
        %this.inspectList.removeIfMember(%object);
        %count = %this.inspectList.getCount();
        if(%count > 0)
        {
            %this.pane.bind(%this.inspectList.getObject(%count - 1));
        }
    }
}

function GuiEditorInspectorWindow::onClearInspectAll(%this)
{
	%this.inspectList.clear();
    %this.pane.unbind();
}

function GuiEditorInspectorWindow::onAlsoInspect(%this, %object)
{
    %this.inspectList.add(%object);
    %this.pane.bind(%object);
}

// A write landed on the selected control. The canvas has to redraw, and the
// explorer tree may be showing a name that just changed.
function GuiEditorInspectorWindow::onPaneCommit(%this, %object)
{
    %this.postEvent("PostApply", %object);
}

function GuiEditorInspectorWindow::onObjectRemoved(%this)
{
	%this.onClearInspectAll(%this);
}