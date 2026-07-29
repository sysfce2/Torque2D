//GuiEditorControlListWindow.cs

function GuiEditorControlListWindow::onAdd(%this)
{
    // "fill", not "width"/"height": this is the window's only child, so it wants
    // the whole content rect. Those two only preserve whatever gap the authored
    // extent started with, and the authored extent was measured against a 20-pixel
    // title bar -- the default is 28 now, so every one of these windows was
    // clipping its last eight pixels. Fill measures against the parent's inner
    // rect, which already has the title taken out of it, so it cannot drift again.
    %this.scroller = new GuiScrollCtrl()
	{
		HorizSizing="fill";
		VertSizing="fill";
		Position="0 0";
		Extent="242 355";
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

    %this.listBox = new GuiListBoxCtrl()
    {
		class = "GuiEditorControlListBox";
        HorizSizing="width";
		VertSizing="height";
		Position="0 0";
        AllowMultipleSelections = "0";
        fitParentWidth = "1";
    };
	ThemeManager.setProfile(%this.listBox, "listBoxProfile");
    %this.scroller.add(%this.listBox);

    %this.populate();
}

function GuiEditorControlListWindow::onRemove(%this)
{
    if(isObject(%this.scroller))
    {
        %this.scroller.delete();
    }
}

// The palette is what a person can drag into a Gui, which is not the same as
// every class deriving from GuiControl. Two kinds are filtered out: the ones
// that are editor or engine plumbing (the console, the edit control, the
// inspector types), and the ones that are real controls but are never placed by
// hand -- GuiDragAndDropCtrl is created at runtime to carry a drag payload, and
// GuiMenuItemCtrl only means anything as a child of a menu bar (it does not
// even register GuiControl's fields, calling SimObject::initPersistFields
// directly, so it has no profile, position or extent to give it).
function GuiEditorControlListWindow::populate(%this)
{
    %controls = enumerateConsoleClasses("GuiControl");
	%this.listBox.clearItems();
	for(%i = 0; %i < getFieldCount(%controls); %i++)
	{
		%field = getField(%controls, %i);

        if(%field !$= "GuiCanvas" && (%field $= "SceneWindow" || getSubStr(%field, 0, 3) $= "Gui") &&
            getSubStr(%field, 0, 10) !$= "GuiConsole" && getSubStr(%field, 0, 7) !$= "GuiEdit" &&
            getSubStr(%field, 0, 12) !$= "GuiInspector" && %field !$= "GuiMessageVectorCtrl" &&
            %field !$= "GuiParticleGraphInspector" && %field !$= "GuiGraphCtrl" && %field !$= "GuiSceneObjectCtrl" &&
            %field !$= "GuiDragAndDropCtrl" && %field !$= "GuiMenuItemCtrl")
        {
		    %this.listBox.addItem(%field);
        }
	}
}