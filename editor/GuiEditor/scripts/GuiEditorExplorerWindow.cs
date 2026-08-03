
function GuiEditorExplorerWindow::onAdd(%this)
{
    %this.scroller = new GuiScrollCtrl()
	{
		// Fill rather than width/height -- the window's only child wants its
		// whole content rect. See GuiEditorControlListWindow for why.
		HorizSizing="fill";
		VertSizing="fill";
		Position="0 0";
		Extent="392 355";
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

	// A real class rather than a script class on a plain tree. The two columns of
	// editor state have to draw in row with each item and be hit tested by the
	// pixel, and a list box refuses children, so there is nothing script could
	// have hung them on.
	//
	// Note there is no class= here any more, and there must not be: the C++ class
	// owns the namespace now, so GuiEditorExplorerTree.cs writes into it directly.
	// Setting class= to the same name makes Namespace::classLinkTo log "cannot
	// change namespace parent linkage" every time the editor opens.
    %this.tree = new GuiEditorExplorerTree()
	{
		HorizSizing="width";
		VertSizing="height";
		Position="0 0";
		Extent="228 355";
		BindToGuiEditor="1";
		AllowReorder="1";

		// The eye and the padlock. Frames come from EditorIcons.cs, never from
		// the engine: that file is generated and alphabetical, so a number baked
		// into C++ would silently become a different picture on the next rebuild.
		StateIcons = "EditorCore:editorIcons16";
		EyeFrame = $EditorIcon::eye;
		LockFrame = $EditorIcon::padlock_closed;

		// And a miniature of each control's own class, between the triangle and
		// the text. Which frame is onGetItemIcon's answer, asked once per row as
		// the tree builds -- the sheet is picked by size, same as everywhere else.
		IconImage = GuiEditor.controlIcons.sheetFor(16);
		IconSize = 16;

		// Named for the state each describes, because the eye reads inverted --
		// it is present when the control is NOT hidden. The bodies are the ones
		// the properties pane used to carry; the headings are new, and say
		// "Shown in the editor" rather than "Visible" because Visible is a real
		// runtime flag that still lives in the pane and means something else.
		ShownTip = "Shown in the editor - On" NL
			"Drawn on the canvas as it will be in the game.";
		HiddenTip = "Shown in the editor - Off" NL
			"Hidden while you work. The control still draws when the game runs -- this only takes it out of the way on the canvas, so you can reach what is behind it. It is not saved with the Gui.";
		LockedTip = "Locked - On" NL
			"Cannot be picked or dragged on the canvas. Use this to stop a backdrop swallowing every click meant for what sits on top of it. It is not saved with the Gui.";
		UnlockedTip = "Locked - Off" NL
			"Can be picked and dragged on the canvas.";
	};
	ThemeManager.setProfile(%this.tree, "treeViewProfile");
	ThemeManager.setProfile(%this.tree, "tipProfile", "TooltipProfile");
	%this.scroller.add(%this.tree);
	// The window itself, not a control inside it: the properties pane posts
	// PostApply through its window so the tree can pick up a name that just
	// changed. (It used to listen to the native GuiInspector, which is gone.)
	%this.tree.startListening(GuiEditor.inspectorWindow);
}

function GuiEditorExplorerWindow::inspect(%this, %object)
{
    %this.tree.inspect(%object);
}

function GuiEditorExplorerWindow::onAlsoInspect(%this, %ctrl)
{
	%this.tree.startRadioSilence();
	%index = %this.tree.findItemID(%ctrl.getID());
	if(%index != -1)
	{
		%this.tree.setSelected(%index, true);
	}
	%this.tree.endRadioSilence();
}

function GuiEditorExplorerWindow::onClearInspect(%this, %ctrl)
{
	%this.tree.startRadioSilence();
	%index = %this.tree.findItemID(%ctrl.getID());
	if(%index != -1)
	{
		%this.tree.setSelected(%ctrl, false);
	}
	%this.tree.endRadioSilence();
}

function GuiEditorExplorerWindow::onClearInspectAll(%this)
{
	%this.tree.startRadioSilence();
	%this.tree.clearSelection();
	%this.tree.endRadioSilence();
}

function GuiEditorExplorerWindow::onObjectRemoved(%this)
{
	%this.tree.refresh();
}

function GuiEditorExplorerWindow::onAddControl(%this, %ctrl)
{
	%this.tree.refresh();
}

function GuiEditorExplorerWindow::onParentChange(%this, %parent)
{
	%index = %this.tree.findItemID(%parent);
	while(%index != -1)
	{
		%this.tree.setItemOpen(%index, true);
		%index = %this.tree.getItemParent(%index);
	} 
	%this.tree.refresh();
}