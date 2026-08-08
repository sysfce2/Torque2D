//-----------------------------------------------------------------------------
// Moving a control from a large container into a small one, by each of the two
// gestures that can do it, in each of the sizing modes.
//
//   the canvas   drag the control until the pointer is over another container.
//                GuiEditCtrl::moveSelectionToCtrl reparents it and then puts it
//                back under the pointer, so where it ends up is settled and the
//                only question is what happened to its SIZE.
//
//   the tree     drag its row onto another branch. There is no pointer in that
//                gesture, so nothing supplies a position: the control keeps the
//                local one it had in its old parent, and a small enough new
//                parent can leave it entirely outside.
//
// Both were wrong for "scale". A scaled control caches the proportion of its
// parent it occupies, and nothing cleared that cache when it changed parent, so
// the old parent's proportion was applied to the new parent's extent -- a button
// 200 wide arriving in a container a quarter the width came out 50 wide. That
// half is engine-side and is pinned down properly in
// engine/source/testing/tests/guiControlReparentTests.cc, which needs no canvas.
// It is checked again here because this is the real editor doing it, through the
// real gesture code, to a real themed control whose profile has borders.
//
// The rescue is the half that only exists here: GuiEditorExplorerTree's
// onPostReorder pulls a stranded control back into view before the undo step is
// committed, so one Ctrl+Z puts it back in its old parent at its old position.
//
// Neither drag is posted. A real one needs startDragging, which mouse-locks the
// canvas, and the tree's needs mDragIndex state that only a genuine touch
// sequence sets up -- so each gesture is driven at the seam its own code uses:
// moveSelectionToCtrl for the canvas, and onPreReorder / add / onPostReorder for
// the tree, which is exactly what GuiTreeViewCtrl::reorderFromDrag does around
// its own move.
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

$Pass = 0;
$Fail = 0;

function rpCheck(%label, %condition)
{
	if(%condition)
	{
		$Pass++;
		echo("RPAR PASS: " @ %label);
	}
	else
	{
		$Fail++;
		echo("RPAR FAIL: " @ %label);
	}
}

function rpSame(%label, %got, %want)
{
	rpCheck(%label @ " (" @ %got @ ")", %got $= %want);
}

schedule(2000, 0, "rpSetup");

// A project, so there is a theme. An unthemed control wears its constructor's
// profiles, and the whole point of running this in the editor rather than in a
// unit test is that the containers here have real borders and so an inner rect
// that is smaller than their extent.
function rpSetup()
{
	ProjectManager.setProjectFolder("PlanetX");
	ModuleDatabase.ScanModules("PlanetX");
	ModuleDatabase.LoadExplicit("AppCore", 1);

	GuiEditor.open();
	schedule(500, 0, "rpBuild");
}

//-----------------------------------------------------------------------------
// The document: one large container and one small one, side by side on the root.
//-----------------------------------------------------------------------------

function rpBuild()
{
	$rpBig = rpContainer("20 20", "600 400");
	$rpSmall = rpContainer("650 20", "100 80");

	rpCheck("the large container is on the root", $rpBig.getGroup() == GuiEditor.rootGui);
	rpCheck("the small container is on the root", $rpSmall.getGroup() == GuiEditor.rootGui);

	rpCanvasDrag();
}

function rpContainer(%pos, %ext)
{
	%ctrl = new GuiControl()
	{
		Position = %pos;
		Extent = %ext;
		isContainer = true;
	};

	// Through the brain, so the control arrives the way a dropped one does: added
	// to the current add set, themed, announced, and listed in the tree.
	GuiEditor.brain.setCurrentAddSet(GuiEditor.rootGui);
	GuiEditor.brain.acceptControl(%ctrl);

	return %ctrl;
}

// A fresh button in the large container, in the sizing mode under test. Fresh
// each time: a control that has already been moved once has a recharged
// proportion cache, and the bug is about the FIRST move.
function rpButton(%pos, %horiz, %vert)
{
	%button = new GuiButtonCtrl()
	{
		Position = %pos;
		Extent = "200 40";
		Text = "Button";
	};

	GuiEditor.brain.setCurrentAddSet($rpBig);
	GuiEditor.brain.acceptControl(%button);

	%button.HorizSizing = %horiz;
	%button.VertSizing = %vert;

	return %button;
}

// Done with a control, between cases. Through the editor rather than a bare
// delete(): a control that is still the selection when it goes leaves the brain
// and every pane holding an id that no longer answers. This is the route the
// Delete key takes, and it puts the control in the trash rather than destroying
// it, which is also what keeps undo coherent.
function rpDiscard(%button)
{
	GuiEditor.brain.onInspect(%button);
	GuiEditor.brain.deleteSelection();
	GuiEditor.brain.onDelete();
}

//-----------------------------------------------------------------------------
// The canvas drag. What has to survive it is the SIZE: the position is the
// gesture's to decide and it has already decided, by putting the control back
// under the pointer it was dragged with.
//-----------------------------------------------------------------------------

function rpCanvasDrag()
{
	rpCanvasCase("anchored", "anchorLeft", "anchorTop");
	rpCanvasCase("width/height", "width", "height");
	rpCanvasCase("scale", "scale", "scale");

	rpCanvasOwned("center", "center", "center", true);
	rpCanvasOwned("fill", "fill", "fill", false);

	schedule(100, 0, "rpTreeDrag");
}

// The modes that keep their own geometry. A move must change neither the extent
// nor where the control appears on screen.
function rpCanvasCase(%mode, %horiz, %vert)
{
	%button = rpButton("100 100", %horiz, %vert);

	%extent = %button.getExtent();
	%where = %button.getGlobalPosition();

	GuiEditor.brain.onInspect(%button);
	GuiEditor.brain.moveSelectionToCtrl($rpSmall);

	rpCheck("canvas " @ %mode @ ": the control changed parent",
		%button.getGroup() == $rpSmall);
	rpSame("canvas " @ %mode @ ": the extent is the one it was dragged at",
		%button.getExtent(), %extent);
	rpSame("canvas " @ %mode @ ": and it is still where it was dropped",
		%button.getGlobalPosition(), %where);

	rpDiscard(%button);
}

// The two modes that compute their geometry from the parent every layout, and
// so are expected to move rather than to stay.
//
// What they are held to is that they settled against the container they are in
// NOW: running the layout again must change nothing. A control still carrying
// the old parent's answer fails that immediately, and unlike a hard-coded
// coordinate it does not need this test to know what the container's borders
// cost -- which is the whole reason these two are checked in the real themed
// editor rather than only in the unit suite.
//
// center is in this group for its POSITION only. It centers a control; it does
// not resize one, so a 200-wide button centered in a container half that wide
// stays 200 wide and hangs out of both sides. That is correct, and it is why
// this cannot simply assert that the control fits.
function rpCanvasOwned(%mode, %horiz, %vert, %keepsExtent)
{
	%button = rpButton("100 100", %horiz, %vert);
	%extent = %button.getExtent();

	GuiEditor.brain.onInspect(%button);
	GuiEditor.brain.moveSelectionToCtrl($rpSmall);

	rpCheck("canvas " @ %mode @ ": the control changed parent",
		%button.getGroup() == $rpSmall);
	rpSettled("canvas " @ %mode, %button);

	if(%keepsExtent)
	{
		rpSame("canvas " @ %mode @ ": centering moves a control, it does not resize one",
			%button.getExtent(), %extent);
	}
	else
	{
		rpCheck("canvas " @ %mode @ ": it took the new container's width, not the old one's",
			getWord(%button.getExtent(), 0) < getWord($rpBig.getExtent(), 0));
		rpCheck("canvas " @ %mode @ ": and its height",
			getWord(%button.getExtent(), 1) < getWord($rpBig.getExtent(), 1));
	}

	rpDiscard(%button);
}

// Re-running the layout against the parent the control has now must be a no-op.
// applySizing is parentResized with a zero delta, so the modes that respond to a
// change have nothing to respond to and the two that describe a position simply
// reassert it -- which is exactly the question being asked.
function rpSettled(%label, %ctrl)
{
	%was = %ctrl.getPosition() SPC %ctrl.getExtent();
	%ctrl.applySizing();

	rpSame(%label @ ": the move left it where a fresh layout would put it",
		%ctrl.getPosition() SPC %ctrl.getExtent(), %was);
}

//-----------------------------------------------------------------------------
// The tree drag. Here the position is nobody's to decide, so it is the position
// that is at stake -- and the extent has to hold as well.
//-----------------------------------------------------------------------------

function rpTreeDrag()
{
	// Both axes outside the small container: 300 across is past a 100-wide one,
	// and 100 down is past an 80-tall one.
	rpTreeCase("anchored", "anchorLeft", "anchorTop", "300 100", "0 0");
	rpTreeCase("width/height", "width", "height", "300 100", "0 0");
	rpTreeCase("scale", "scale", "scale", "300 100", "0 0");

	// One axis outside. 20 down is inside 80 even once the container's borders
	// have taken their share, so it is kept: a control that was 20 pixels down is
	// still 20 pixels down.
	rpTreeCase("one axis", "anchorLeft", "anchorTop", "300 20", "0 20");

	// Inside already, and not to be touched.
	rpTreeCase("already visible", "anchorLeft", "anchorTop", "10 10", "10 10");

	// The two that place themselves need no rescue, and must not get one: a
	// control the layout has already put somewhere is not stranded, whatever its
	// coordinates read. fill in particular sits at 0,0 with the parent's whole
	// inner extent, which no rescue would touch, and center can legitimately be
	// at a NEGATIVE position when the control is wider than the container.
	rpTreeOwned("center", "center", "center");
	rpTreeOwned("fill", "fill", "fill");

	schedule(100, 0, "rpUndo");
}

function rpTreeOwned(%mode, %horiz, %vert)
{
	%button = rpButton("300 100", %horiz, %vert);

	rpTreeMove(%button, $rpSmall);

	rpCheck("tree " @ %mode @ ": the control changed parent",
		%button.getGroup() == $rpSmall);
	rpSettled("tree " @ %mode, %button);

	rpDiscard(%button);
}

function rpTreeCase(%mode, %horiz, %vert, %at, %expect)
{
	%button = rpButton(%at, %horiz, %vert);
	%extent = %button.getExtent();

	rpTreeMove(%button, $rpSmall);

	rpCheck("tree " @ %mode @ ": the control changed parent",
		%button.getGroup() == $rpSmall);
	rpSame("tree " @ %mode @ ": the extent survived the move",
		%button.getExtent(), %extent);
	rpSame("tree " @ %mode @ ": it is somewhere the user can see",
		%button.getPosition(), %expect);

	rpDiscard(%button);
}

// What GuiTreeViewCtrl::reorderFromDrag does around its own move, with the
// selection set the way a drag would have left it.
function rpTreeMove(%ctrl, %target)
{
	%tree = GuiEditor.explorerWindow.tree;

	%index = %tree.findItemID(%ctrl);
	rpCheck("the tree has a row for the control", %index != -1);

	%tree.clearSelection();
	%tree.setSelected(%index, true);

	%tree.onPreReorder();
	%target.add(%ctrl);
	%tree.onPostReorder();

	%tree.refresh();
}

//-----------------------------------------------------------------------------
// Undo. The rescue runs before commitHierarchy, so the corrected position is
// what the undo step records as the "after" -- which means one step puts the
// control back in its old parent AT ITS OLD POSITION, rather than leaving it
// rescued somewhere it was never placed.
//-----------------------------------------------------------------------------

function rpUndo()
{
	%button = rpButton("300 100", "anchorLeft", "anchorTop");
	%extent = %button.getExtent();

	rpTreeMove(%button, $rpSmall);
	rpSame("undo: the move stranded it and the rescue caught it",
		%button.getPosition(), "0 0");

	GuiEditor.Undo();

	rpCheck("undo put the control back in the container it came from",
		%button.getGroup() == $rpBig);
	rpSame("undo put it back where it was, not where it was rescued to",
		%button.getPosition(), "300 100");
	rpSame("undo left the extent alone", %button.getExtent(), %extent);

	// And forward again, because a rescue that only survives one direction is a
	// rescue the user loses by pressing Ctrl+Y.
	GuiEditor.Redo();

	rpCheck("redo moved it back into the small container",
		%button.getGroup() == $rpSmall);
	rpSame("redo restored the rescued position", %button.getPosition(), "0 0");

	echo("RPAR DONE  " @ $Pass @ " passed, " @ $Fail @ " failed");
	quit();
}
