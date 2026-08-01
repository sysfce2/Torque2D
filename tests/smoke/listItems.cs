//-----------------------------------------------------------------------------
// Static list rows: the ones a list box or a drop down is authored with, rather
// than the ones a script fills in at runtime.
//
// An item is neither a field nor a child object, so it takes a route of its own
// to disk - TAML custom nodes - and a route of its own into a clone. This checks
// both, and checks the one control that must NOT take them: a GuiTreeViewCtrl
// generates its rows from a root object, so a written-out set would be stale.
//
// Run: tests/run.ps1 listItems ; grep LISTITEMS in console.log.
//-----------------------------------------------------------------------------

setLogMode(2);
setScriptExecEcho(false);
trace(false);
$Scripts::ignoreDSOs = true;
setCompanyAndProduct("Torque Game Engines", "Torque2D");
ModuleDatabase.EchoInfo = false;
AssetDatabase.EchoInfo = false;
AssetDatabase.IgnoreAutoUnload = true;

function liCheck(%label, %condition)
{
	echo(%condition ? ("LISTITEMS PASS: " @ %label) : ("LISTITEMS FAIL: " @ %label));
}

function liScratch()
{
	return testRoot("shots/listItemsScratch");
}

function liReadFile(%file)
{
	%fo = new FileObject();
	if(!%fo.openForRead(%file))
	{
		%fo.delete();
		return "";
	}

	%text = "";
	while(!%fo.isEOF())
	{
		%text = %text @ %fo.readLine() @ " ";
	}
	%fo.close();
	%fo.delete();

	return %text;
}

testExec("editor/main.cs");
schedule(2000, 0, "liStep1");

//-----------------------------------------------------------------------------
// The list as a string, which is what the editor and its undo stack both use.
//-----------------------------------------------------------------------------

function liStep1()
{
	createPath(liScratch() @ "/");

	%list = new GuiListBoxCtrl();

	liCheck("an empty list reads as an empty string", %list.getItemList() $= "");

	// A bare list of captions: every field after the first is left off, so each
	// row keeps what LBItem's constructor gave it.
	%list.setItemList("Easy" NL "Normal" NL "Hard");
	liCheck("three captions make three rows", %list.getItemCount() == 3);
	liCheck("caption 0 read back", %list.getItemText(0) $= "Easy");
	liCheck("caption 2 read back", %list.getItemText(2) $= "Hard");
	liCheck("a row with no fields is active", %list.getItemActive(1));
	liCheck("a row with no fields is unselected", %list.getSelectedItem() == -1);

	// A caption with spaces in it, which is why the encoding splits on TAB and
	// the parse does not go near getWord.
	%list.setItemList("Two words" TAB "7" TAB "1" TAB "0" TAB "0" TAB "1 1 1 1");
	liCheck("a caption may hold spaces", %list.getItemText(0) $= "Two words");
	liCheck("ID survives the round trip", %list.getItemID(0) == 7);

	// An empty caption is a legal row - the editor's Add button makes one.
	%list.setItemList("" NL "after");
	liCheck("an empty caption is still a row", %list.getItemCount() == 2);
	liCheck("the row after an empty one is intact", %list.getItemText(1) $= "after");

	%list.delete();

	schedule(100, 0, "liStep2");
}

//-----------------------------------------------------------------------------
// To disk and back. Custom nodes, so this is the TAML path only - the .gui
// script writer cannot carry them, which is what the save dialog warns about.
//-----------------------------------------------------------------------------

function liStep2()
{
	%list = new GuiListBoxCtrl()
	{
		Extent = "180 120";
		AllowMultipleSelections = false;
	};
	%list.setItemList(
		"Easy"   TAB "1" TAB "1" TAB "0" TAB "0" TAB "1 1 1 1" NL
		"Normal" TAB "2" TAB "1" TAB "1" TAB "0" TAB "1 1 1 1" NL
		"Hard"   TAB "3" TAB "0" TAB "1" TAB "1" TAB "1 0 0 1");

	%before = %list.getItemList();

	%file = pathConcat(liScratch(), "list.gui.taml");
	TAMLWrite(%list, %file);

	// Lower-cased before every search below. StringTable hands back the first
	// spelling of a name it was ever given, so which capitalisation an attribute
	// is written in is not ours to decide - "ID" comes out as "Id" - and a test
	// that pinned the case would fail on a spelling that works perfectly.
	%text = strlwr(liReadFile(%file));
	liCheck("the file carries an Items section", strstr(%text, "guilistboxctrl.items") != -1);
	liCheck("the file carries a row", strstr(%text, "text=\"normal\"") != -1);

	// Only what differs from an LBItem's defaults is written, so an ordinary row
	// is one attribute.
	liCheck("a default ID is not written", strstr(%text, "id=\"0\"") == -1);
	liCheck("a default Active is not written", strstr(%text, "active=\"1\"") == -1);
	liCheck("a color is written where there is one", strstr(%text, "color=") != -1);

	%read = TAMLRead(%file);
	liCheck("the file reads back as a list box", isObject(%read) && %read.getClassName() $= "GuiListBoxCtrl");
	liCheck("every row came back", isObject(%read) && %read.getItemCount() == 3);
	liCheck("the list round trips exactly", isObject(%read) && strcmp(%read.getItemList(), %before) == 0);
	liCheck("the selection came back with it", isObject(%read) && %read.getSelectedItem() != -1);

	// Named separately, because a field silently failing to come back is what a
	// whole-list comparison reports least clearly. Every one of these is matched
	// by a StringTable pointer on the way in, and an attribute name interned the
	// case-sensitive way stops matching the parser's without saying so.
	liCheck("an ID survives the file", isObject(%read) && %read.getItemID(1) == 2);
	liCheck("an inactive row survives the file", isObject(%read) && !%read.getItemActive(2));

	// The color through the record, because getItemColor is C++ only - the
	// bindings expose setItemColor and clearItemColor but never a getter.
	%hard = getRecord(%read.getItemList(), 2);
	liCheck("a color survives the file",
		getField(%hard, 4) == 1 && getField(%hard, 5) $= "1 0 0 1");

	if(isObject(%read))
	{
		%read.delete();
	}

	// A deep clone copies fields and children, and an item is neither. This is
	// the path the Gui Editor's copy, cut and paste take.
	%clone = %list.deepClone();
	liCheck("a deep clone carries the rows", isObject(%clone) && strcmp(%clone.getItemList(), %before) == 0);
	if(isObject(%clone))
	{
		%clone.delete();
	}

	%list.delete();

	schedule(100, 0, "liStep3");
}

//-----------------------------------------------------------------------------
// A drop down keeps its rows in a list box that is nobody's child, so none of
// the above reaches it on its own.
//-----------------------------------------------------------------------------

function liStep3()
{
	%drop = new GuiDropDownCtrl()
	{
		Extent = "140 24";
	};
	%drop.setItemList("Red" TAB "10" NL "Green" TAB "20" NL "Blue" TAB "30");

	liCheck("a drop down takes a list", %drop.getItemCount() == 3);
	liCheck("a drop down reads its rows back", %drop.getItemText(1) $= "Green");
	liCheck("a drop down keeps IDs", %drop.getItemID(2) == 30);

	%before = %drop.getItemList();

	%file = pathConcat(liScratch(), "drop.gui.taml");
	TAMLWrite(%drop, %file);

	%text = liReadFile(%file);
	liCheck("the drop down's section is named for its own class",
		strstr(%text, "GuiDropDownCtrl.Items") != -1);

	%read = TAMLRead(%file);
	liCheck("a drop down round trips", isObject(%read) && strcmp(%read.getItemList(), %before) == 0);
	if(isObject(%read))
	{
		%read.delete();
	}

	%clone = %drop.deepClone();
	liCheck("a cloned drop down carries its rows",
		isObject(%clone) && strcmp(%clone.getItemList(), %before) == 0);
	if(isObject(%clone))
	{
		%clone.delete();
	}

	%drop.delete();

	schedule(100, 0, "liStep4");
}

//-----------------------------------------------------------------------------
// The tree, which must not join in.
//-----------------------------------------------------------------------------

function liStep4()
{
	%tree = new GuiTreeViewCtrl()
	{
		Extent = "180 120";
	};

	// Reached through the base class, since nothing else would put rows in one.
	%tree.setItemList("ghost" NL "rows");
	liCheck("a tree can still hold items in memory", %tree.getItemCount() == 2);

	%file = pathConcat(liScratch(), "tree.gui.taml");
	TAMLWrite(%tree, %file);

	%text = liReadFile(%file);
	liCheck("a tree writes no Items section", strstr(%text, ".Items") == -1);

	%tree.delete();

	schedule(100, 0, "liStep5");
}

//-----------------------------------------------------------------------------
// The Items section of the properties pane, on the real editor UI.
//-----------------------------------------------------------------------------

function liStep5()
{
	ProjectManager.setProjectFolder("listItemsSmokeProject");
	GuiEditor.open();

	EditorCore.open();
	EditorCore.tabBook.selectPageName("Gui Editor");

	schedule(800, 0, "liStep6");
}

function liBind(%ctrl)
{
	GuiEditor.inspectorWindow.pane.bind(%ctrl);
	return GuiEditor.inspectorWindow.pane;
}

function liStep6()
{
	%pane = GuiEditor.inspectorWindow.pane;
	liCheck("the pane built an Items section", isObject(%pane.itemsPanel) && isObject(%pane.itemsBlock));

	// Which classes get one. A tree derives from a list box and must not.
	liCheck("a list box has an item list", %pane.spec.hasItemList("GuiListBoxCtrl"));
	liCheck("a drop down has an item list", %pane.spec.hasItemList("GuiDropDownCtrl"));
	liCheck("a tree does not", !%pane.spec.hasItemList("GuiTreeViewCtrl"));
	liCheck("a button does not", !%pane.spec.hasItemList("GuiButtonCtrl"));

	GuiEditor.brain.setCurrentAddSet(GuiEditor.rootGui);
	%room = GuiEditor.brain.visiblePartOf(GuiEditor.rootGui);
	$liAt = (getWord(%room, 0) + 30) SPC (getWord(%room, 1) + 30);

	$liList = new GuiListBoxCtrl() { Extent = "180 120"; Position = $liAt; };
	GuiEditor.brain.onControlDropped($liList, "50 50");
	liCheck("the list box arrived on the canvas", $liList.getParent() == GuiEditor.rootGui);

	GuiEditor.undoRecorder.clear();

	%pane = liBind($liList);
	liCheck("the section shows for a list box", %pane.itemsPanel.isVisible());
	liCheck("the block bound to the list box", %pane.itemsBlock.target == $liList);
	liCheck("an empty list has no rows", %pane.itemsBlock.grid.getCount() == 0);

	$liBlock = %pane.itemsBlock;

	// Add, through the Add row exactly as a click on it would.
	$liBlock.nameBox.setText("Easy");
	$liBlock.onAddClicked();
	liCheck("the control took the row", $liList.getItemCount() == 1);
	liCheck("with the caption typed", $liList.getItemText(0) $= "Easy");
	liCheck("adding a row is one undo step", GuiEditor.undoRecorder.undoCount() == 1);

	// The rows are rebuilt from a schedule(0), because a click that changes them
	// arrives from inside one.
	schedule(50, 0, "liStep7");
}

function liStep7()
{
	liCheck("the row appeared in the pane", $liBlock.grid.getCount() == 1);

	$liBlock.nameBox.setText("Normal");
	$liBlock.onAddClicked();
	$liBlock.nameBox.setText("Hard");
	$liBlock.onAddClicked();

	schedule(50, 0, "liStep8");
}

function liStep8()
{
	liCheck("three rows", $liBlock.grid.getCount() == 3 && $liList.getItemCount() == 3);

	// The first row cannot go up and the last cannot go down.
	liCheck("the first row's up arrow is off", !$liBlock.grid.getObject(0).upButton.isActive());
	liCheck("the last row's down arrow is off", !$liBlock.grid.getObject(2).downButton.isActive());
	liCheck("a middle row can go either way",
		$liBlock.grid.getObject(1).upButton.isActive() &&
		$liBlock.grid.getObject(1).downButton.isActive());

	// Retype a caption the way the box does: per keystroke, then a commit.
	%row = $liBlock.grid.getObject(0);
	%row.captionBox.setText("Simple");
	%row.onCaptionTyped();
	liCheck("typing reaches the control at once", $liList.getItemText(0) $= "Simple");

	%steps = GuiEditor.undoRecorder.undoCount();
	%row.onCommit();
	liCheck("a retyped caption is one more undo step",
		GuiEditor.undoRecorder.undoCount() == (%steps + 1));
	liCheck("and the control kept it", $liList.getItemText(0) $= "Simple");

	// An ID, and the two switches.
	%row.idBox.setText("7");
	%row.onCommit();
	liCheck("the ID was written", $liList.getItemID(0) == 7);

	%row.activeToggle.setValue(false);
	$liBlock.onItemRowToggled(%row, "active");
	liCheck("a row can be turned inactive", !$liList.getItemActive(0));

	schedule(50, 0, "liStep9");
}

function liStep9()
{
	// Move the top row down, which the pane does by rewriting the whole list.
	$liBlock.onItemRowMove($liBlock.grid.getObject(0), 1);

	schedule(50, 0, "liStep10");
}

function liStep10()
{
	liCheck("the moved row swapped with the one below",
		$liList.getItemText(0) $= "Normal" && $liList.getItemText(1) $= "Simple");
	liCheck("and carried its ID with it", $liList.getItemID(1) == 7);

	// Remove the middle row.
	$liBlock.onItemRowRemove($liBlock.grid.getObject(1));

	schedule(50, 0, "liStep11");
}

function liStep11()
{
	liCheck("the row went", $liList.getItemCount() == 2);
	liCheck("the pane agrees", $liBlock.grid.getCount() == 2);
	liCheck("and it was the right one",
		$liList.getItemText(0) $= "Normal" && $liList.getItemText(1) $= "Hard");

	// Every step back, then every step forward again.
	$liFinal = $liList.getItemList();
	$liSteps = GuiEditor.undoRecorder.undoCount();

	for(%i = 0; %i < $liSteps; %i++)
	{
		GuiEditor.Undo();
	}
	liCheck("undoing everything empties the list (" @ $liList.getItemCount() @ ")",
		$liList.getItemCount() == 0);

	for(%i = 0; %i < $liSteps; %i++)
	{
		GuiEditor.Redo();
	}
	liCheck("redoing everything puts it back exactly",
		strcmp($liList.getItemList(), $liFinal) == 0);

	schedule(50, 0, "liStep12");
}

//-----------------------------------------------------------------------------
// Only one row can start selected on a single-selection list.
//-----------------------------------------------------------------------------

function liStep12()
{
	liCheck("the pane caught up with the replay", $liBlock.grid.getCount() == 2);

	$liList.AllowMultipleSelections = false;

	%first = $liBlock.grid.getObject(0);
	%first.selectedToggle.setValue(true);
	$liBlock.onItemRowToggled(%first, "selected");

	schedule(50, 0, "liStep13");
}

function liStep13()
{
	%second = $liBlock.grid.getObject(1);
	%second.selectedToggle.setValue(true);
	$liBlock.onItemRowToggled(%second, "selected");

	schedule(50, 0, "liStep14");
}

function liStep14()
{
	liCheck("the second row is the selected one", $liList.getSelectedItem() == 1);
	liCheck("and the first was turned off",
		!$liBlock.grid.getObject(0).selectedToggle.getValue());

	// A drop down gets the same section, over rows that live in a list box the
	// drop down owns rather than in the control the pane is bound to.
	%room = GuiEditor.brain.visiblePartOf(GuiEditor.rootGui);
	$liDrop = new GuiDropDownCtrl()
	{
		Extent = "140 24";
		Position = (getWord(%room, 0) + 30) SPC (getWord(%room, 1) + 200);
	};
	GuiEditor.brain.onControlDropped($liDrop, "50 20");

	%pane = liBind($liDrop);
	liCheck("the section shows for a drop down", %pane.itemsPanel.isVisible());

	%pane.itemsBlock.nameBox.setText("Fullscreen");
	%pane.itemsBlock.onAddClicked();
	liCheck("the drop down took the row", $liDrop.getItemCount() == 1);
	liCheck("with its caption", $liDrop.getItemText(0) $= "Fullscreen");

	// And a class that has no rows at all keeps the section out of the way.
	$liButton = new GuiButtonCtrl()
	{
		Extent = "100 30";
		Position = (getWord(%room, 0) + 30) SPC (getWord(%room, 1) + 240);
	};
	GuiEditor.brain.onControlDropped($liButton, "50 20");

	%pane = liBind($liButton);
	liCheck("the section hides for a button", !%pane.itemsPanel.isVisible());

	schedule(100, 0, "liStep15");
}

//-----------------------------------------------------------------------------
// What the legacy .gui format would drop. Custom nodes are a TAML feature, and
// the script writer walks fields and children only.
//-----------------------------------------------------------------------------

function liStep15()
{
	%summary = GuiEditor.tamlOnlyStateSummary();
	liCheck("the summary names the rows on the canvas (" @ %summary @ ")",
		strstr(%summary, "rows on 2 lists") != -1);
	liCheck("and says what to do about it", strstr(%summary, "Save as TAML") != -1);

	// A frame set counts too, but only once it has been split - an unsplit one
	// would be rebuilt as itself.
	%room = GuiEditor.brain.visiblePartOf(GuiEditor.rootGui);
	// The drop point is the middle of the payload, and it has to land ON the Gui
	// being edited - findHitControl answers "me" for any point at all, so the
	// brain polices the boundary itself and simply refuses one that misses.
	$liFrames = new GuiFrameSetCtrl()
	{
		Extent = "200 120";
		Position = (getWord(%room, 0) + 30) SPC (getWord(%room, 1) + 30);
	};
	GuiEditor.brain.onControlDropped($liFrames, "100 60");
	liCheck("the frame set arrived", $liFrames.getParent() == GuiEditor.rootGui);
	liCheck("an unsplit frame set is not counted",
		strstr(GuiEditor.tamlOnlyStateSummary(), "frame") == -1);

	$liFrames.createHorizontalSplit(1);
	%summary = GuiEditor.tamlOnlyStateSummary();
	liCheck("a split one is (" @ %summary @ ")", strstr(%summary, "1 frame layout") != -1);
	liCheck("and the heading names both kinds",
		strstr(%summary, "list rows or frame layouts") != -1);

	// And nothing to say about a document that holds none of it.
	%empty = new GuiControl();
	%saved = GuiEditor.rootGui;
	GuiEditor.rootGui = %empty;
	liCheck("an ordinary Gui gets no warning", GuiEditor.tamlOnlyStateSummary() $= "");
	GuiEditor.rootGui = %saved;
	%empty.delete();

	schedule(100, 0, "liDone");
}

function liDone()
{
	echo("LISTITEMS DONE");
	quit();
}
