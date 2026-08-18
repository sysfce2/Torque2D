//-----------------------------------------------------------------------------
// Copy, cut and paste in the Gui Editor. Boots the editor, opens the PlanetX
// project so there is a real theme to work with, and puts the clipboard through
// what a person does with it: copy a control and paste it, paste it again, paste
// it somewhere else, copy a whole panel, cut something.
//
// The three things that are easy to get wrong and are checked hardest: a paste
// is ONE undo step however much it puts back, a copy is a real copy (children,
// dynamic fields, a frame set's frames, and no second helping of whatever a
// control's class builds for itself), and no two controls end up sharing a name.
// Run: tests/run.ps1 clipboard ; grep CLIP in tests/logs/.
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

function cCheck(%label, %condition)
{
	echo(%condition ? ("CLIP PASS: " @ %label) : ("CLIP FAIL: " @ %label));
}

function cUndoCount()
{
	return GuiEditor.undoRecorder.undoCount();
}

function cRedoCount()
{
	return GuiEditor.undoRecorder.redoCount();
}

function cSelect(%ctrl)
{
	GuiEditor.brain.clearSelection();
	GuiEditor.brain.select(%ctrl);
}

function cIndexOf(%parent, %ctrl)
{
	for(%i = 0; %i < %parent.getCount(); %i++)
	{
		if(%parent.getObject(%i) == %ctrl)
		{
			return %i;
		}
	}
	return -1;
}

// What a paste selected, which is what it pasted.
function cPasted(%index)
{
	%set = GuiEditor.brain.getSelected();
	return %set.getObject(%index);
}

function cPastedCount()
{
	%set = GuiEditor.brain.getSelected();
	return %set.getCount();
}

// Comparing against "" proves nothing: an absent dynamic field and an empty one
// read back the same, because an empty one is exactly what the engine deletes.
function cHasDynamicField(%ctrl, %name)
{
	for(%i = 0; %i < %ctrl.getDynamicFieldCount(); %i++)
	{
		if(getWord(%ctrl.getDynamicField(%i), 0) $= %name)
		{
			return true;
		}
	}
	return false;
}

// Menu items are nested controls, so this walks rather than indexes.
function cMenuItem(%parent, %text)
{
	for(%i = 0; %i < %parent.getCount(); %i++)
	{
		%item = %parent.getObject(%i);
		if(%item.Text $= %text)
		{
			return %item;
		}

		%found = cMenuItem(%item, %text);
		if(isObject(%found))
		{
			return %found;
		}
	}

	return 0;
}

// A frame set's tree with the control ids taken out: those are object ids, so a
// copy's are necessarily different. What has to match is the shape - every
// frame's id, split direction, extent and anchoring - and which frames hold a
// control at all.
function cFrameShape(%layout)
{
	%shape = "";
	%count = getWordCount(%layout);

	for(%i = 0; (%i + 7) < %count; %i += 8)
	{
		for(%j = 0; %j < 7; %j++)
		{
			%shape = %shape @ getWord(%layout, %i + %j) @ " ";
		}
		%shape = %shape @ ((getWord(%layout, %i + 7) != 0) ? "1" : "0") @ " ";
	}

	return %shape;
}

// A control class that builds a child of its own the moment it is created, which
// is the house pattern (TORQUE_SCRIPT.md) and the thing a copy must not do twice.
function ClipProbe::onAdd(%this)
{
	%kid = new GuiControl()
	{
		Position = "4 4";
		Extent = "20 20";
	};
	%this.add(%kid);
}

schedule(2000, 0, "cStep1");

function cStep1()
{
	ProjectManager.setProjectFolder("PlanetX");
	ModuleDatabase.ScanModules("PlanetX");
	ModuleDatabase.LoadExplicit("AppCore", 1);

	$cTheme = nameToID("PlanetX");
	cCheck("PlanetX theme loaded", isObject($cTheme));

	GuiEditor.open();
	cCheck("the editor built a clipboard", isObject(GuiEditor.clipboard));
	cCheck("which starts empty", GuiEditor.clipboard.isEmpty());

	// Before anything has been copied, so this is the only moment it can be
	// checked.
	%pasteItem = cMenuItem(EditorCore.menuBar, "Paste");
	cCheck("Paste is greyed with nothing on the clipboard", !%pasteItem.Active);

	// The stage: a panel with three named children, and a second container to
	// paste into.
	$cPanel = new GuiControl() { Position = "10 10"; Extent = "400 300"; };
	GuiEditor.rootGui.add($cPanel);

	$cA = new GuiButtonCtrl(clipA) { Position = "10 10"; Extent = "80 30"; Text = "A"; };
	$cPanel.add($cA);
	$cB = new GuiButtonCtrl(clipB) { Position = "10 50"; Extent = "80 30"; Text = "B"; };
	$cPanel.add($cB);
	$cC = new GuiButtonCtrl(clipC) { Position = "10 90"; Extent = "80 30"; Text = "C"; };
	$cPanel.add($cC);

	$cOther = new GuiControl() { Position = "10 320"; Extent = "300 120"; };
	GuiEditor.rootGui.add($cOther);

	GuiEditor.setTheme($cTheme, false);
	GuiEditor.undoRecorder.clear();

	cCheck("the stage's controls are named", $cA.getName() $= "clipA");
	cCheck("the stack starts empty", cUndoCount() == 0 && cRedoCount() == 0);

	schedule(300, 0, "cStepCopyPaste");
}

//-----------------------------------------------------------------------------
// One control: copy, paste, and what the paste is worth on the undo stack.
//-----------------------------------------------------------------------------

function cStepCopyPaste()
{
	GuiEditor.undoRecorder.clear();
	%grid = GuiEditor.brain.getGridSize();

	cSelect($cA);
	GuiEditor.Copy();

	cCheck("a copy fills the clipboard", !GuiEditor.clipboard.isEmpty());
	cCheck("and records nothing on the undo stack", cUndoCount() == 0);
	cCheck("and does not touch the original", $cA.getParent() == $cPanel);

	%pasteItem = cMenuItem(EditorCore.menuBar, "Paste");
	cCheck("Paste is offered once something is copied", %pasteItem.Active);

	%before = $cPanel.getCount();
	GuiEditor.Paste();

	%copy = cPasted(0);
	cCheck("a paste is one step", cUndoCount() == 1);
	cCheck("it added one control", $cPanel.getCount() == (%before + 1));
	cCheck("into the container the original was in", %copy.getParent() == $cPanel);
	cCheck("and it is a different object", %copy != $cA);
	cCheck("of the same class", %copy.getClassName() $= $cA.getClassName());
	cCheck("with the same extent", %copy.getExtent() $= $cA.getExtent());
	cCheck("the same caption", %copy.Text $= $cA.Text);
	cCheck("and the same profile (" @ %copy.getFieldValue("Profile") @ ")",
		%copy.getFieldValue("Profile") $= $cA.getFieldValue("Profile"));
	cCheck("the paste is selected", cPastedCount() == 1);

	// Stepped one grid line so it is not hidden exactly behind the original.
	%wanted = (getWord($cA.getPosition(), 0) + %grid) SPC
		(getWord($cA.getPosition(), 1) + %grid);
	cCheck("stepped off the original (" @ %copy.getPosition() @ " wanted " @ %wanted @ ")",
		%copy.getPosition() $= %wanted);

	// The name is the original's, made unique - not shared with it.
	cCheck("the copy was renamed (" @ %copy.getName() @ ")", %copy.getName() $= "clipA2");
	cCheck("and the original kept its own name", $cA.getName() $= "clipA");
	cCheck("with no marker left behind", !cHasDynamicField(%copy, "clipName"));

	// Undo does not delete: the control lives in the trash, which is what leaves
	// redo something to put back.
	%trash = GuiEditor.brain.getTrash();
	GuiEditor.Undo();
	cCheck("undo took the paste out of the Gui", %copy.getGroup() == %trash);
	cCheck("but did not delete it", isObject(%copy));
	cCheck("and the container is back to what it held", $cPanel.getCount() == %before);

	GuiEditor.Redo();
	cCheck("redo pasted it again", %copy.getParent() == $cPanel);
	cCheck("still named for the original", %copy.getName() $= "clipA2");

	// A second paste while the first copy is still in the Gui: it has to step past
	// it and be named around it, rather than colliding with what it just made.
	GuiEditor.Paste();
	%twice = cPasted(0);

	%wanted = (getWord($cA.getPosition(), 0) + (2 * %grid)) SPC
		(getWord($cA.getPosition(), 1) + (2 * %grid));
	cCheck("a second paste steps past the first (" @ %twice.getPosition() @ ")",
		%twice.getPosition() $= %wanted);
	cCheck("and is named around it (" @ %twice.getName() @ ")", %twice.getName() $= "clipA3");

	// Put the stage back the way the next step expects to find it.
	GuiEditor.Undo();
	GuiEditor.Undo();
	cCheck("the stage is back to three children", $cPanel.getCount() == 3);

	schedule(300, 0, "cStepRepeat");
}

//-----------------------------------------------------------------------------
// Pasting again, and pasting elsewhere. Nothing is ever laid exactly on top of
// something already pasted.
//-----------------------------------------------------------------------------

function cStepRepeat()
{
	GuiEditor.undoRecorder.clear();
	%grid = GuiEditor.brain.getGridSize();
	%from = $cA.getPosition();

	// A fresh copy, which starts the stepping over.
	GuiEditor.brain.setCurrentAddSet($cPanel);
	cSelect($cA);
	GuiEditor.Copy();

	GuiEditor.Paste();
	%first = cPasted(0);
	cCheck("the first paste steps once (" @ %first.getPosition() @ ")",
		%first.getPosition() $= ((getWord(%from, 0) + %grid) SPC (getWord(%from, 1) + %grid)));

	// A different container: the position it had, unstepped, because there is
	// nothing there to hide behind.
	cSelect($cOther);
	GuiEditor.brain.setCurrentAddSet($cOther);
	GuiEditor.Paste();
	%second = cPasted(0);

	cCheck("pasting elsewhere puts it in that container", %second.getParent() == $cOther);
	cCheck("at the position it had (" @ %second.getPosition() @ ")",
		%second.getPosition() $= %from);

	// Back to the panel, where a paste has already been. The step carries on from
	// where that container left off rather than starting again, or this paste
	// would land exactly on the copy already sitting there.
	GuiEditor.brain.setCurrentAddSet($cPanel);
	GuiEditor.Paste();
	%third = cPasted(0);

	cCheck("coming back to a container carries on stepping (" @ %third.getPosition() @ ")",
		%third.getPosition() $= ((getWord(%from, 0) + (2 * %grid)) SPC
			(getWord(%from, 1) + (2 * %grid))));
	cCheck("so it does not land on the copy already there",
		%third.getPosition() !$= %first.getPosition());
	cCheck("and all three are different objects",
		%first != %second && %second != %third && %first != %third);

	// Tidy up: three pastes, three steps back.
	GuiEditor.Undo();
	GuiEditor.Undo();
	GuiEditor.Undo();
	cCheck("all three pastes came back off the stack", cUndoCount() == 0);
	cCheck("and the panel holds what it started with", $cPanel.getCount() == 3);
	cCheck("as does the other container", $cOther.getCount() == 0);

	schedule(300, 0, "cStepMultiple");
}

//-----------------------------------------------------------------------------
// More than one control at once, and the reduction that stops a control being
// pasted twice.
//-----------------------------------------------------------------------------

function cStepMultiple()
{
	GuiEditor.undoRecorder.clear();
	GuiEditor.brain.setCurrentAddSet($cPanel);

	GuiEditor.brain.clearSelection();
	GuiEditor.brain.select($cA);
	GuiEditor.brain.addSelection($cC);
	GuiEditor.Copy();

	%before = $cPanel.getCount();
	GuiEditor.Paste();

	cCheck("pasting two controls is still one step", cUndoCount() == 1);
	cCheck("and both arrived", $cPanel.getCount() == (%before + 2));
	cCheck("both are selected", cPastedCount() == 2);

	// Document order, not selection order: the copies sit in the same z-order as
	// the originals.
	%firstCopy = cPasted(0);
	%secondCopy = cPasted(1);
	cCheck("in the order they were in the document",
		%firstCopy.Text $= "A" && %secondCopy.Text $= "C");

	GuiEditor.Undo();
	cCheck("one undo took both back", $cPanel.getCount() == %before);
	cCheck("and left nothing on the undo stack", cUndoCount() == 0);

	// A control inside another selected control is already coming along.
	GuiEditor.undoRecorder.clear();
	GuiEditor.brain.clearSelection();
	GuiEditor.brain.select($cPanel);
	GuiEditor.brain.addSelection($cB);
	GuiEditor.Copy();

	%rootBefore = GuiEditor.rootGui.getCount();
	GuiEditor.brain.setCurrentAddSet(GuiEditor.rootGui);
	GuiEditor.Paste();

	cCheck("selecting a container and something inside it pastes one control",
		GuiEditor.rootGui.getCount() == (%rootBefore + 1));
	cCheck("which is the container", cPastedCount() == 1);

	%panelCopy = cPasted(0);
	cCheck("and it brought its three children (" @ %panelCopy.getCount() @ ")",
		%panelCopy.getCount() == 3);

	GuiEditor.Undo();
	cCheck("and that was one step too", GuiEditor.rootGui.getCount() == %rootBefore);

	schedule(300, 0, "cStepNested");
}

//-----------------------------------------------------------------------------
// A copy of a whole panel: everything below it comes across, and none of it
// shares a name with the original.
//-----------------------------------------------------------------------------

function cStepNested()
{
	GuiEditor.undoRecorder.clear();
	GuiEditor.brain.setCurrentAddSet(GuiEditor.rootGui);

	cSelect($cPanel);
	GuiEditor.Copy();
	GuiEditor.Paste();

	%copy = cPasted(0);
	cCheck("the panel copy holds three children", %copy.getCount() == 3);

	%childA = %copy.getObject(0);
	%childB = %copy.getObject(1);
	%childC = %copy.getObject(2);

	cCheck("the children are new objects",
		%childA != $cA && %childB != $cB && %childC != $cC);
	cCheck("in the same order", %childA.Text $= "A" && %childC.Text $= "C");
	cCheck("with the positions they had",
		%childA.getPosition() $= $cA.getPosition() &&
		%childC.getPosition() $= $cC.getPosition());
	cCheck("and the extents they had", %childB.getExtent() $= $cB.getExtent());
	cCheck("wearing the same profiles",
		%childB.getFieldValue("Profile") $= $cB.getFieldValue("Profile"));

	cCheck("every child was renamed (" @ %childA.getName() SPC %childB.getName() SPC
		%childC.getName() @ ")",
		%childA.getName() !$= "clipA" && %childB.getName() !$= "clipB" &&
		%childC.getName() !$= "clipC");
	cCheck("and named for what it was", %childA.getName() $= "clipA2");
	cCheck("with no markers left behind",
		!cHasDynamicField(%childA, "clipName") && !cHasDynamicField(%childC, "clipName"));

	GuiEditor.Undo();
	cCheck("undoing the panel paste takes the whole branch", cUndoCount() == 0);

	schedule(300, 0, "cStepDynamic");
}

//-----------------------------------------------------------------------------
// Dynamic fields, which the .cs writer would have dropped - the reason the
// clipboard clones rather than serialises.
//-----------------------------------------------------------------------------

function cStepDynamic()
{
	GuiEditor.undoRecorder.clear();
	GuiEditor.brain.setCurrentAddSet($cPanel);

	GuiEditor.undoRecorder.writeDynamicField($cB, "cSmokeTag", "hello");
	cCheck("the original carries a dynamic field", cHasDynamicField($cB, "cSmokeTag"));

	cSelect($cB);
	GuiEditor.Copy();
	GuiEditor.Paste();

	%copy = cPasted(0);
	cCheck("the copy carries it too", cHasDynamicField(%copy, "cSmokeTag"));
	cCheck("with the value it had", %copy.cSmokeTag $= "hello");

	GuiEditor.Undo();
	GuiEditor.undoRecorder.clear();
	$cB.cSmokeTag = "";

	schedule(300, 0, "cStepClass");
}

//-----------------------------------------------------------------------------
// The one a clipboard built on serialising, or on plain construction, gets
// wrong: a control whose class builds a child in onAdd. The copy must hold the
// children the original has, and not a second set of them.
//-----------------------------------------------------------------------------

function cStepClass()
{
	GuiEditor.undoRecorder.clear();
	GuiEditor.brain.setCurrentAddSet(GuiEditor.rootGui);

	$cProbe = new GuiControl()
	{
		class = "ClipProbe";
		Position = "420 10";
		Extent = "120 60";
	};
	GuiEditor.rootGui.add($cProbe);

	cCheck("the probe's class built it a child", $cProbe.getCount() == 1);

	cSelect($cProbe);
	GuiEditor.Copy();
	GuiEditor.Paste();

	%copy = cPasted(0);
	cCheck("the copy has exactly one child (" @ %copy.getCount() @ ")", %copy.getCount() == 1);
	cCheck("and it is not the original's child", %copy.getObject(0) != $cProbe.getObject(0));
	cCheck("the copy still has the class", %copy.class $= "ClipProbe");

	// Two steps, not one expression: TorqueScript cannot call a method on the
	// result of a call.
	%copyKid = %copy.getObject(0);
	%sourceKid = $cProbe.getObject(0);
	cCheck("and the child kept its geometry",
		%copyKid.getPosition() $= %sourceKid.getPosition());

	GuiEditor.Undo();

	schedule(300, 0, "cStepCut");
}

//-----------------------------------------------------------------------------
// Cut, which is a copy plus the delete the Delete key already does.
//-----------------------------------------------------------------------------

function cStepCut()
{
	GuiEditor.undoRecorder.clear();
	GuiEditor.brain.setCurrentAddSet($cPanel);
	%trash = GuiEditor.brain.getTrash();

	%index = cIndexOf($cPanel, $cB);
	cSelect($cB);
	GuiEditor.Cut();

	cCheck("a cut is one step", cUndoCount() == 1);
	cCheck("the control went to the trash", $cB.getGroup() == %trash);
	cCheck("it was not deleted", isObject($cB));
	cCheck("and the clipboard has it", !GuiEditor.clipboard.isEmpty());

	GuiEditor.Paste();
	%copy = cPasted(0);

	cCheck("the paste after a cut is a second step", cUndoCount() == 2);
	cCheck("and it arrived", %copy.getParent() == $cPanel);

	// The original is in the trash, which is not the document - so its name is
	// free and the pasted control can have it back.
	cCheck("a cut and paste keeps the name (" @ %copy.getName() @ ")",
		%copy.getName() $= "clipB");

	GuiEditor.Undo();
	cCheck("undoing the paste leaves the cut", cUndoCount() == 1);
	GuiEditor.Undo();
	cCheck("undoing the cut puts the original back", $cB.getParent() == $cPanel);
	cCheck("at the index it came from", cIndexOf($cPanel, $cB) == %index);

	schedule(300, 0, "cStepFrames");
}

//-----------------------------------------------------------------------------
// A frame set, whose layout is not in its field list at all: the frame tree is
// written as TAML custom nodes and nothing else, so copying one is the case that
// proves the copy is a deep clone rather than a field copy.
//-----------------------------------------------------------------------------

function cStepFrames()
{
	GuiEditor.undoRecorder.clear();
	GuiEditor.brain.setCurrentAddSet(GuiEditor.rootGui);

	$cFrames = new GuiFrameSetCtrl()
	{
		Position = "420 100";
		Extent = "400 200";
		DividerThickness = 4;
	};
	GuiEditor.rootGui.add($cFrames);

	%ids = $cFrames.createHorizontalSplit(1);
	%left = getWord(%ids, 0);
	%right = getWord(%ids, 1);
	$cFrames.createVerticalSplit(%left);
	$cFrames.createVerticalSplit(%right);

	for(%i = 0; %i < 4; %i++)
	{
		%panel = new GuiControl() { Extent = "40 20"; };
		$cFrames.add(%panel);
	}

	// Settle the layout before anything is measured: a frame set places its
	// children when it resizes, and until then they sit at the bounds they were
	// built with.
	$cFrames.childrenReordered();

	%sourceShape = cFrameShape($cFrames.getFrameLayout());
	cCheck("the frame set has a tree", %sourceShape !$= "");

	cSelect($cFrames);
	GuiEditor.Copy();
	GuiEditor.Paste();

	%copy = cPasted(0);
	cCheck("the copy holds four children", %copy.getCount() == 4);

	%copyShape = cFrameShape(%copy.getFrameLayout());
	cCheck("and the frame tree came with it" NL
		"           source: " @ %sourceShape NL
		"           copy:   " @ %copyShape, %copyShape $= %sourceShape);

	// Each frame's control must be one of the COPY's children. The layout's
	// eighth value per frame is the control id, so this reads them back out.
	%layout = %copy.getFrameLayout();
	%ownChildren = true;
	for(%i = 0; (%i + 7) < getWordCount(%layout); %i += 8)
	{
		%ctrl = getWord(%layout, %i + 7);
		if(%ctrl != 0 && %ctrl.getParent() != %copy)
		{
			%ownChildren = false;
		}
	}
	cCheck("holding the copy's own children, not the original's", %ownChildren);

	GuiEditor.Undo();
	cCheck("and the paste was one step", cUndoCount() == 0);

	schedule(300, 0, "cStepMenu");
}

//-----------------------------------------------------------------------------
// The Edit menu, which is what says whether any of this is available.
//-----------------------------------------------------------------------------

function cStepMenu()
{
	%cutItem = cMenuItem(EditorCore.menuBar, "Cut");
	%copyItem = cMenuItem(EditorCore.menuBar, "Copy");
	%pasteItem = cMenuItem(EditorCore.menuBar, "Paste");

	cCheck("the Edit menu has all three items",
		isObject(%cutItem) && isObject(%copyItem) && isObject(%pasteItem));

	cSelect($cA);
	cCheck("Cut is offered with a selection", %cutItem.Active);
	cCheck("so is Copy", %copyItem.Active);

	GuiEditor.brain.clearSelection();
	cCheck("Cut is greyed with nothing selected", !%cutItem.Active);
	cCheck("so is Copy", !%copyItem.Active);

	cCheck("Paste is still offered - the clipboard is not empty", %pasteItem.Active);

	schedule(300, 0, "cStepStale");
}

//-----------------------------------------------------------------------------
// The clipboard holds live controls wearing live profiles, so it goes stale in
// the one place the undo stack does: when the theme library frees a profile.
//-----------------------------------------------------------------------------

function cStepStale()
{
	%library = GuiEditor.getThemeLibrary();
	$cThemeB = %library.createTheme("ClipThemeB");
	cCheck("second theme created", isObject($cThemeB));

	GuiEditor.setTheme($cThemeB, false);

	cSelect($cA);
	GuiEditor.Copy();
	cCheck("something is on the clipboard", !GuiEditor.clipboard.isEmpty());

	// Deleting the theme detaches the document from it first, and the copies in
	// the clipboard hold the same profiles by raw pointer.
	%library.deleteTheme($cThemeB);
	cCheck("deleting a theme empties the clipboard", GuiEditor.clipboard.isEmpty());

	%pasteItem = cMenuItem(EditorCore.menuBar, "Paste");
	cCheck("and greys Paste again", !%pasteItem.Active);

	GuiEditor.setTheme($cTheme, false);
	GuiEditor.undoRecorder.clear();

	schedule(300, 0, "cStepChain");
}

//-----------------------------------------------------------------------------
// Pasting into a container that places its own children.
//
// A GuiChainCtrl lays its children out in list order and takes a child's
// position when it arrives, which it is entitled to do: a control pasted into a
// chain belongs where the chain puts it. What must still hold is that the copy
// arrived, in the right container, as one undo step.
//
// Last, and on the real editor UI, because a chain only claims its children
// while the canvas is in edit mode - which needs the editor pushed onto the
// canvas rather than merely registered.
//-----------------------------------------------------------------------------

function cStepChain()
{
	EditorCore.open();
	EditorCore.tabBook.selectPageName("Gui Editor");

	schedule(500, 0, "cStepChainRun");
}

function cStepChainRun()
{
	GuiEditor.undoRecorder.clear();

	$cChain = new GuiChainCtrl()
	{
		Position = "420 320";
		Extent = "200 120";
		IsVertical = true;
		ChildSpacing = 4;
	};
	GuiEditor.rootGui.add($cChain);

	for(%i = 0; %i < 3; %i++)
	{
		%button = new GuiButtonCtrl() { Extent = "80 24"; Text = "chain" @ %i; };
		$cChain.add(%button);
	}

	%probe = $cChain.getObject(1);
	cCheck("the editor is in edit mode (a chain lays out what it is given)",
		getWord(%probe.getPosition(), 0) == 0);

	cSelect(%probe);
	GuiEditor.Copy();

	GuiEditor.brain.setCurrentAddSet($cChain);
	GuiEditor.Paste();

	%copy = cPasted(0);
	cCheck("the paste is one step", cUndoCount() == 1);
	cCheck("it arrived in the chain", %copy.getParent() == $cChain);
	cCheck("the chain holds four now", $cChain.getCount() == 4);
	cCheck("and the copy kept its caption", %copy.Text $= %probe.Text);

	GuiEditor.Undo();
	cCheck("undo took it back out", $cChain.getCount() == 3);

	schedule(300, 0, "cStepDuplicate");
}

//-----------------------------------------------------------------------------
// Duplicate, which is a copy that never touches the clipboard: it lands in the
// control's OWN parent, one grid step off, whatever container is currently being
// worked in and whatever is on the clipboard at the time.
//-----------------------------------------------------------------------------

function cStepDuplicate()
{
	GuiEditor.undoRecorder.clear();
	%grid = GuiEditor.brain.getGridSize();

	%before = $cPanel.getCount();
	%at = $cA.getPosition();

	cSelect($cA);
	GuiEditor.Duplicate();

	%copy = cPasted(0);
	cCheck("duplicate made one control", cPastedCount() == 1);
	cCheck("in the same parent as the original", %copy.getParent() == $cPanel);
	cCheck("which now holds one more", $cPanel.getCount() == %before + 1);
	cCheck("the original is still there", $cA.getParent() == $cPanel);

	cCheck("the copy is one grid step across",
		getWord(%copy.getPosition(), 0) == getWord(%at, 0) + %grid);
	cCheck("and one down",
		getWord(%copy.getPosition(), 1) == getWord(%at, 1) + %grid);

	cCheck("the copy counted on from the original's name",
		%copy.getName() $= "clipA2");
	cCheck("and kept its caption", %copy.Text $= $cA.Text);

	cCheck("duplicate is one step", cUndoCount() == 1);
	GuiEditor.Undo();
	cCheck("undo took the copy back out", $cPanel.getCount() == %before);

	schedule(300, 0, "cStepDuplicateClipboard");
}

// The whole reason it is not Ctrl+C, Ctrl+V: what is on the clipboard is still
// on the clipboard afterwards.
function cStepDuplicateClipboard()
{
	GuiEditor.undoRecorder.clear();

	cSelect($cB);
	GuiEditor.Copy();

	cSelect($cA);
	GuiEditor.Duplicate();

	cCheck("the clipboard still holds something", !GuiEditor.clipboard.isEmpty());

	GuiEditor.brain.setCurrentAddSet($cOther);
	GuiEditor.Paste();

	%pasted = cPasted(0);
	cCheck("and what it holds is what was copied, not what was duplicated",
		%pasted.Text $= $cB.Text);

	schedule(300, 0, "cStepDuplicateNested");
}

// A panel and a button inside it: the reduction that stops the button being
// duplicated twice is the same one copy uses.
function cStepDuplicateNested()
{
	GuiEditor.undoRecorder.clear();

	%before = GuiEditor.rootGui.getCount();

	GuiEditor.brain.clearSelection();
	GuiEditor.brain.addSelection($cPanel);
	GuiEditor.brain.addSelection($cA);

	GuiEditor.Duplicate();

	cCheck("the panel was duplicated once", cPastedCount() == 1);
	cCheck("into the root", GuiEditor.rootGui.getCount() == %before + 1);

	%copy = cPasted(0);
	cCheck("and the button came with it rather than separately",
		%copy.getCount() == $cPanel.getCount());

	cCheck("still one step", cUndoCount() == 1);
	GuiEditor.Undo();
	cCheck("undo removed the whole thing", GuiEditor.rootGui.getCount() == %before);

	schedule(300, 0, "cStepMenuGreying");
}

// Both new items follow the selection, the way Cut and Copy beside them do.
// There is nothing to duplicate or delete when nothing is selected, and an item
// that stays lit is an item that lies about it.
function cStepMenuGreying()
{
	%duplicate = cMenuItem(EditorCore.menuBar, "Duplicate");
	%delete = cMenuItem(EditorCore.menuBar, "Delete");

	cCheck("the Duplicate item exists", isObject(%duplicate));
	cCheck("the Delete item exists", isObject(%delete));

	GuiEditor.brain.clearSelection();
	cCheck("Duplicate is greyed with nothing selected", !%duplicate.Active);
	cCheck("Delete is greyed with nothing selected", !%delete.Active);

	cSelect($cA);
	cCheck("Duplicate is offered once something is", %duplicate.Active);
	cCheck("Delete is offered once something is", %delete.Active);

	schedule(300, 0, "cDone");
}

function cDone()
{
	echo("CLIP DONE");
	quit();
}
