//-----------------------------------------------------------------------------
// Authoring a GuiTabBookCtrl in the Gui Editor.
//
// A tab page is the one control the palette will not offer, because it is the
// one control that means nothing outside its container. So the book makes its
// own: one when it is dropped, and one for every click on the "+" tab it draws
// at the end of its strip while the Gui is being authored. This checks that the
// palette really has stopped offering it, that both routes to a page produce the
// same object, that a book can be emptied and still be recoverable, and that a
// page cannot be dragged, dropped or pasted anywhere but into a book.
//
// Runs on the real editor UI throughout, because the "+" only exists where
// isEditMode() is true - which needs the editor pushed onto the canvas rather
// than merely registered.
//
// Run: tests/run.ps1 tabBook ; grep TABBOOK in tests/logs/.
//-----------------------------------------------------------------------------

setLogMode(2);
setScriptExecEcho(false);
trace(false);
$Scripts::ignoreDSOs = true;
setCompanyAndProduct("Torque Game Engines", "Torque2D");
ModuleDatabase.EchoInfo = false;
AssetDatabase.EchoInfo = false;
AssetDatabase.IgnoreAutoUnload = true;

testExec("editor/main.cs");

function tbCheck(%label, %condition)
{
	echo(%condition ? ("TABBOOK PASS: " @ %label) : ("TABBOOK FAIL: " @ %label));
}

function tbUndoCount()
{
	return GuiEditor.undoRecorder.undoCount();
}

function tbSelect(%ctrl)
{
	GuiEditor.brain.clearSelection();
	GuiEditor.brain.select(%ctrl);
}

// Is the inner rect wholly inside the outer one? Both are "x y width height".
function tbInside(%inner, %outer)
{
	return getWord(%inner, 0) >= getWord(%outer, 0) &&
		getWord(%inner, 1) >= getWord(%outer, 1) &&
		(getWord(%inner, 0) + getWord(%inner, 2)) <= (getWord(%outer, 0) + getWord(%outer, 2)) &&
		(getWord(%inner, 1) + getWord(%inner, 3)) <= (getWord(%outer, 1) + getWord(%outer, 3));
}

function tbGlobalRect(%ctrl)
{
	return %ctrl.getGlobalPosition() SPC %ctrl.getExtent();
}

schedule(2000, 0, "tbStep1");

function tbStep1()
{
	ProjectManager.setProjectFolder("PlanetX");
	ModuleDatabase.ScanModules("PlanetX");
	ModuleDatabase.LoadExplicit("AppCore", 1);

	// By id: a bare identifier in TorqueScript is a string, and everything here
	// compares object handles.
	$tbTheme = nameToID("PlanetX");
	tbCheck("PlanetX theme loaded", isObject($tbTheme));

	GuiEditor.open();
	GuiEditor.setTheme($tbTheme, false);

	// The real editor UI, on the canvas, from the very start. Everything about
	// the "+" is gated on isEditMode(), which answers false until the Gui being
	// edited is actually on screen under the editor.
	EditorCore.open();
	EditorCore.tabBook.selectPageName("Gui Editor");

	schedule(800, 0, "tbStepPalette");
}

//-----------------------------------------------------------------------------
// The palette, which has to refuse the class without losing it.
//
// The row stays in the icon table: the frame number IS the row index, so
// dropping the row would repoint every icon below it onto the wrong art, and
// dropping the class from covered[] would have the sweep over the class registry
// offer it straight back with a question mark for an icon.
//-----------------------------------------------------------------------------

function tbStepPalette()
{
	%icons = GuiEditor.controlIcons;

	tbCheck("the palette will not place a tab page", !%icons.isPlaceableClass("GuiTabPageCtrl"));
	tbCheck("no Tab Page tile in Layout",
		strstr(%icons.keysInGroup("Layout"), "GuiTabPageCtrl") == -1);
	tbCheck("but Tab Book is still there",
		strstr(%icons.keysInGroup("Layout"), "GuiTabBookCtrl") != -1);

	tbCheck("the entry survives in the table", %icons.isKnown("GuiTabPageCtrl"));
	tbCheck("and still counts as covered", %icons.coversClass("GuiTabPageCtrl"));
	tbCheck("so it keeps its label (" @ %icons.labelFor("GuiTabPageCtrl") @ ")",
		%icons.labelFor("GuiTabPageCtrl") $= "Tab Page");

	// The two frames either side of the seam. If the row had been deleted rather
	// than refused, frameFor would answer 0 for Tab Page and Window would have
	// slid down onto its art. Asserted as adjacency rather than two literals:
	// the frame IS the row index, so removing any earlier entry renumbers both
	// without saying anything about the seam these two are here to guard.
	%tabPage = %icons.frameFor("GuiTabPageCtrl");
	tbCheck("Tab Page keeps a frame of its own (" @ %tabPage @ ")", %tabPage > 0);
	tbCheck("Window sits right after it (" @ %icons.frameFor("GuiWindowCtrl") @ ")",
		%icons.frameFor("GuiWindowCtrl") == (%tabPage + 1));

	schedule(300, 0, "tbStepDrop");
}

//-----------------------------------------------------------------------------
// Dropping a book, which has to arrive with a page in it.
//-----------------------------------------------------------------------------

function tbStepDrop()
{
	GuiEditor.undoRecorder.clear();
	GuiEditor.brain.setCurrentAddSet(GuiEditor.rootGui);

	// Where the canvas can actually show something: a drop is only a drop when
	// the middle of the payload is over the Gui being edited.
	%room = GuiEditor.brain.visiblePartOf(GuiEditor.rootGui);
	$tbAt = (getWord(%room, 0) + 30) SPC (getWord(%room, 1) + 30);

	$tbBook = new GuiTabBookCtrl() { Extent = "300 130"; Position = $tbAt; };
	GuiEditor.brain.onControlDropped($tbBook, "50 50");

	tbCheck("the book arrived", $tbBook.getParent() == GuiEditor.rootGui);
	tbCheck("carrying exactly one page (" @ $tbBook.getCount() @ ")", $tbBook.getCount() == 1);

	$tbPage = $tbBook.getObject(0);
	tbCheck("which is a tab page", $tbPage.getClassName() $= "GuiTabPageCtrl");
	tbCheck("captioned \"" @ $tbPage.getText() @ "\"", $tbPage.getText() $= "Page 1");
	tbCheck("the only page is the active one", $tbBook.getSelectedPage() == 0);
	tbCheck("and it is showing", $tbPage.isVisible());

	// Themed by category rather than by profile name, which is what the theme
	// applier actually decides.
	tbCheck("the book was themed on arrival (" @ $tbBook.Profile.category @ ")",
		$tbBook.Profile.category $= "TabBook");
	tbCheck("and the page it brought with it (" @ $tbPage.Profile.category @ ")",
		$tbPage.Profile.category $= "TabPage");

	// The page came with the book, so it is part of the book arriving rather
	// than a second thing the user did.
	tbCheck("the whole thing is one undo step (" @ tbUndoCount() @ ")", tbUndoCount() == 1);

	// After the drop's own schedule(40), so the undo is not racing it.
	schedule(200, 0, "tbStepDropUndo");
}

function tbStepDropUndo()
{
	%trash = GuiEditor.brain.getTrash();

	GuiEditor.Undo();

	// getGroup, not getParent: a control sitting in the trash - a plain SimGroup
	// - reads as having no parent at all.
	tbCheck("undo took the book out of the Gui", $tbBook.getGroup() == %trash);
	tbCheck("with its page still inside it", $tbPage.getParent() == $tbBook);

	GuiEditor.Redo();
	tbCheck("redo put the book back", $tbBook.getParent() == GuiEditor.rootGui);
	tbCheck("still holding its page (" @ $tbBook.getCount() @ ")", $tbBook.getCount() == 1);
	tbCheck("which is showing again", $tbPage.isVisible());

	schedule(300, 0, "tbStepPlus");
}

//-----------------------------------------------------------------------------
// The "+" tab, which is what GuiTabBookCtrl::requestNewPage asks for. Called
// directly here: the click that reaches it is C++, and what this suite is about
// is what the editor does once asked.
//-----------------------------------------------------------------------------

function tbStepPlus()
{
	GuiEditor.undoRecorder.clear();

	GuiEditor.brain.onAddTabPage($tbBook);

	tbCheck("the book grew a page (" @ $tbBook.getCount() @ ")", $tbBook.getCount() == 2);

	$tbPage2 = $tbBook.getObject(1);
	tbCheck("numbered on from the last (" @ $tbPage2.getText() @ ")", $tbPage2.getText() $= "Page 2");
	tbCheck("themed like any arrival (" @ $tbPage2.Profile.category @ ")",
		$tbPage2.Profile.category $= "TabPage");

	tbCheck("the new page is the active tab", $tbBook.getSelectedPage() == 1);
	tbCheck("so it is the one showing", $tbPage2.isVisible());
	tbCheck("and the first one is not", !$tbPage.isVisible());

	tbCheck("the new page is selected", GuiEditor.brain.selectionList() $= $tbPage2);
	tbCheck("and is where the next control would land",
		GuiEditor.brain.getCurrentAddSet() == $tbPage2);

	tbCheck("adding a page is one step (" @ tbUndoCount() @ ")", tbUndoCount() == 1);

	GuiEditor.Undo();
	tbCheck("undo took the page back off (" @ $tbBook.getCount() @ ")", $tbBook.getCount() == 1);
	tbCheck("leaving the book alone", $tbBook.getParent() == GuiEditor.rootGui);
	tbCheck("and the first page showing again", $tbPage.isVisible());

	GuiEditor.Redo();
	tbCheck("redo put it back (" @ $tbBook.getCount() @ ")", $tbBook.getCount() == 2);

	// A third, to prove the numbering keeps counting rather than restarting.
	GuiEditor.brain.onAddTabPage($tbBook);
	tbCheck("a third page carries on the numbering (" @ $tbBook.getObject(2).getText() @ ")",
		$tbBook.getObject(2).getText() $= "Page 3");

	schedule(300, 0, "tbStepGeometry");
}

//-----------------------------------------------------------------------------
// Where the "+" is, which is the half of it a click depends on.
//-----------------------------------------------------------------------------

function tbStepGeometry()
{
	%rect = $tbBook.getAddPageTabRect();
	%book = tbGlobalRect($tbBook);

	tbCheck("the book reports a \"+\" tab (" @ %rect @ ")", getWord(%rect, 2) > 0);
	tbCheck("square, as an affordance rather than a tab (" @
		getWord(%rect, 2) @ "x" @ getWord(%rect, 3) @ ")",
		getWord(%rect, 2) == getWord(%rect, 3));
	tbCheck("inside the book it belongs to", tbInside(%rect, %book));

	// Three tabs at the default minimum width sit to its left, so a "+" at the
	// very start of the strip would mean it had been laid out before them.
	tbCheck("after the tabs rather than before them",
		getWord(%rect, 0) > getWord(%book, 0));

	// A book with no pages is an ordinary state now that the palette cannot
	// supply one, and the "+" is the only way back out of it.
	$tbEmpty = new GuiTabBookCtrl() { Extent = "300 130"; Position = $tbAt; };
	GuiEditor.rootGui.add($tbEmpty);
	tbCheck("a book with no pages still offers a \"+\" (" @ $tbEmpty.getAddPageTabRect() @ ")",
		getWord($tbEmpty.getAddPageTabRect(), 2) > 0);

	// Outside the Gui being authored there is no "+" at all - which is what
	// keeps it off the editor's own chrome, every window of which is a real
	// GuiControl on the same canvas.
	%loose = new GuiTabBookCtrl() { Extent = "300 130"; };
	tbCheck("a book outside the edited Gui has none (" @ %loose.getAddPageTabRect() @ ")",
		getWord(%loose.getAddPageTabRect(), 2) == 0);
	%loose.delete();

	schedule(300, 0, "tbStepDelete");
}

//-----------------------------------------------------------------------------
// Removing pages, which is the ordinary Delete and nothing new.
//-----------------------------------------------------------------------------

function tbStepDelete()
{
	GuiEditor.undoRecorder.clear();

	// The ACTIVE page first, which is the case with something to get wrong: the
	// book has to promote another page AND show it, where before it promoted one
	// and left it hidden from whenever some other tab was last chosen.
	%active = $tbBook.getObject($tbBook.getSelectedPage());
	tbSelect(%active);
	GuiEditor.brain.onObjectRemoved(%active);

	tbCheck("the page went (" @ $tbBook.getCount() @ ")", $tbBook.getCount() == 2);
	tbCheck("and the page that took over is showing",
		$tbBook.getObject($tbBook.getSelectedPage()).isVisible());

	// Then down to nothing, because emptying a book is what used to leave it
	// drawing nothing at all.

	while($tbBook.getCount() > 0)
	{
		%page = $tbBook.getObject(0);
		tbSelect(%page);
		GuiEditor.brain.onObjectRemoved(%page);
	}

	tbCheck("the book can be emptied (" @ $tbBook.getCount() @ ")", $tbBook.getCount() == 0);
	tbCheck("and is still recoverable (" @ $tbBook.getAddPageTabRect() @ ")",
		getWord($tbBook.getAddPageTabRect(), 2) > 0);

	// Straight back out of it, through the same door the "+" uses.
	GuiEditor.brain.onAddTabPage($tbBook);
	tbCheck("the \"+\" refills an emptied book (" @ $tbBook.getCount() @ ")",
		$tbBook.getCount() == 1);
	tbCheck("numbering from the start again (" @ $tbBook.getObject(0).getText() @ ")",
		$tbBook.getObject(0).getText() $= "Page 1");
	tbCheck("with the new page showing", $tbBook.getObject(0).isVisible());

	schedule(300, 0, "tbStepUndoDelete");
}

function tbStepUndoDelete()
{
	// Right back to three pages, and exactly one of them visible at every point
	// along the way. Restoring the page that was active when it was deleted is
	// the case that used to draw two pages on top of each other: the recorder
	// puts back position, extent and sizing, and visibility is not among them.
	while(tbUndoCount() > 0)
	{
		GuiEditor.Undo();
	}

	tbCheck("undo walked back to three pages (" @ $tbBook.getCount() @ ")",
		$tbBook.getCount() == 3);

	%showing = 0;
	for(%i = 0; %i < $tbBook.getCount(); %i++)
	{
		if($tbBook.getObject(%i).isVisible())
		{
			%showing++;
		}
	}
	tbCheck("with exactly one of them showing (" @ %showing @ ")", %showing == 1);

	schedule(300, 0, "tbStepRules");
}

//-----------------------------------------------------------------------------
// Where a page is allowed to live. GuiControl::canBeChildOf is the rule; the
// Explorer tree drag, the canvas drag and paste are the three doors that ask it.
//-----------------------------------------------------------------------------

function tbStepRules()
{
	$tbPanel = new GuiControl() { Position = "10 400"; Extent = "300 200"; };
	GuiEditor.rootGui.add($tbPanel);

	$tbButton = new GuiButtonCtrl() { Position = "10 10"; Extent = "80 30"; Text = "B"; };
	$tbPanel.add($tbButton);

	%page = $tbBook.getObject(0);

	tbCheck("a page belongs in its book", %page.canBeChildOf($tbBook));
	tbCheck("and in any other book", %page.canBeChildOf($tbEmpty));
	tbCheck("but not in a panel", !%page.canBeChildOf($tbPanel));
	tbCheck("nor on the root", !%page.canBeChildOf(GuiEditor.rootGui));
	tbCheck("an ordinary control still goes anywhere", $tbButton.canBeChildOf($tbPanel));
	tbCheck("including at a book, which re-homes it itself",
		$tbButton.canBeChildOf($tbBook));

	// The canvas drag. Both halves, because a rule that refuses everything would
	// pass the first check on its own.
	tbSelect(%page);
	GuiEditor.brain.moveSelectionToCtrl($tbPanel);
	tbCheck("dragging a page onto a panel leaves it alone", %page.getParent() == $tbBook);

	tbSelect(%page);
	GuiEditor.brain.moveSelectionToCtrl($tbEmpty);
	tbCheck("dragging it onto another book moves it", %page.getParent() == $tbEmpty);

	tbSelect($tbButton);
	GuiEditor.brain.moveSelectionToCtrl($tbPanel);
	tbCheck("an ordinary control is not caught by the rule",
		$tbButton.getParent() == $tbPanel);

	schedule(300, 0, "tbStepPaste");
}

function tbStepPaste()
{
	%page = $tbEmpty.getObject(0);

	tbSelect(%page);
	GuiEditor.Copy();

	// Into a panel: refused, and the clipboard is left holding it so that
	// selecting a book and pasting again does what was meant.
	%before = $tbPanel.getCount();
	GuiEditor.brain.setCurrentAddSet($tbPanel);
	GuiEditor.Paste();
	tbCheck("pasting a page into a panel puts nothing there (" @ $tbPanel.getCount() @ ")",
		$tbPanel.getCount() == %before);

	%before = $tbBook.getCount();
	GuiEditor.brain.setCurrentAddSet($tbBook);
	GuiEditor.Paste();
	tbCheck("pasting it into a book does (" @ $tbBook.getCount() @ ")",
		$tbBook.getCount() == (%before + 1));

	schedule(300, 0, "tbDone");
}

function tbDone()
{
	echo("TABBOOK DONE");
	quit();
}
