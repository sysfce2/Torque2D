//-----------------------------------------------------------------------------
// Deleting a control, from each of the three places a person can ask for it,
// and the one thing that has to be true afterwards: the Explorer tree shows the
// document as it now is.
//
// The routes are not symmetrical, which is how the tree came to go stale. Two of
// them (the Delete key on the canvas, and Cut) delete and then announce it, and
// the Explorer window is listening. The third is the Delete key while the tree
// itself holds first responder: the TREE announces, the brain does the deleting
// -- and postEvent does not deliver to the object that posted, so the window
// that owns the tree never heard that anything had gone.
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

function edCheck(%label, %condition)
{
	if(%condition)
	{
		$Pass++;
		echo("EDEL PASS: " @ %label);
	}
	else
	{
		$Fail++;
		echo("EDEL FAIL: " @ %label);
	}
}

schedule(2000, 0, "edSetup");

function edSetup()
{
	ProjectManager.setProjectFolder("PlanetX");
	ModuleDatabase.ScanModules("PlanetX");
	ModuleDatabase.LoadExplicit("AppCore", 1);

	GuiEditor.open();
	schedule(500, 0, "edCanvasKey");
}

//-----------------------------------------------------------------------------
// Helpers.
//-----------------------------------------------------------------------------

// One control on the canvas, selected, with the tree showing it.
function edPlace()
{
	%tile = edFindTile("GuiButtonCtrl");
	%tile.onClick();

	%ctrl = GuiEditor.rootGui.getObject(GuiEditor.rootGui.getCount() - 1);
	GuiEditor.brain.onInspect(%ctrl);

	return %ctrl;
}

// What every route has to leave behind: the control gone from the document, and
// gone from the tree as well.
function edGone(%label, %ctrl, %items)
{
	%tree = GuiEditor.explorerWindow.tree;

	edCheck(%label @ ": the control left the document",
		%ctrl.getGroup() != GuiEditor.rootGui);
	edCheck(%label @ ": the tree no longer lists it",
		%tree.findItemID(%ctrl) == -1);
	edCheck(%label @ ": the tree lost a row (" @ %items @ " -> " @
		%tree.getItemCount() @ ")", %tree.getItemCount() == %items - 1);
}

//-----------------------------------------------------------------------------
// The Delete key with the canvas in charge. The C++ deletes and then calls
// onDelete, which is the announcement.
//-----------------------------------------------------------------------------

function edCanvasKey()
{
	%tree = GuiEditor.explorerWindow.tree;
	%ctrl = edPlace();

	edCheck("the tree lists a control that was just placed", %tree.findItemID(%ctrl) != -1);
	%items = %tree.getItemCount();

	GuiEditor.brain.deleteSelection();
	GuiEditor.brain.onDelete();
	edGone("canvas Delete", %ctrl, %items);

	schedule(100, 0, "edCut");
}

//-----------------------------------------------------------------------------
// Cut, which is a copy and then that same delete.
//-----------------------------------------------------------------------------

function edCut()
{
	%tree = GuiEditor.explorerWindow.tree;
	%ctrl = edPlace();
	%items = %tree.getItemCount();

	GuiEditor.Cut();
	edGone("Cut", %ctrl, %items);

	schedule(100, 0, "edTreeKey");
}

//-----------------------------------------------------------------------------
// The Delete key while the tree holds first responder, which is what happens
// after clicking a row. GuiListBoxCtrl::onKeyDown calls onDeleteKey.
//-----------------------------------------------------------------------------

function edTreeKey()
{
	%tree = GuiEditor.explorerWindow.tree;
	%ctrl = edPlace();
	%items = %tree.getItemCount();

	%index = %tree.findItemID(%ctrl);
	edCheck("the tree can find the row to delete", %index != -1);

	%tree.onDeleteKey(%index, %tree.getItemText(%index), %ctrl);
	edGone("tree Delete", %ctrl, %items);

	// A delete is one undo step whichever route asked for it, and undoing it has
	// to put the row back -- the same refresh, the other way round.
	GuiEditor.Undo();
	edCheck("undo brought the control back", %ctrl.getGroup() == GuiEditor.rootGui);
	edCheck("and the tree lists it again", %tree.findItemID(%ctrl) != -1);

	echo("EDEL DONE  " @ $Pass @ " passed, " @ $Fail @ " failed");
	quit();
}

function edFindTile(%key)
{
	%window = GuiEditor.ctrlListWindow;
	for(%g = 0; %g < %window.groupCount; %g++)
	{
		%group = %window.group[%g];
		for(%i = 0; %i < %group.tileCount; %i++)
		{
			if(%group.tile[%i].key $= %key)
			{
				return %group.tile[%i];
			}
		}
	}
	return 0;
}
