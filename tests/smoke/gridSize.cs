//-----------------------------------------------------------------------------
// The Layout menu's two grid commands, which are one setting between them.
//
// Set Grid Size names the spacing; Snap to Grid says whether to use it. The C++
// keeps them apart on purpose -- setSnapToGrid(0) only clears the flag and leaves
// mGridSnap alone, so getGridSize still answers "the grid size even if the grid
// is off" -- and the point of this suite is that the script above it keeps them
// apart too. A toggle is not a place to decide how big the grid is.
//
// Snapping is checked by what it does rather than by a getter, because there
// isn't one: hasSnapToGrid is C++-side only. A nudge is the tell. The
// moveSelection binding asks hasSnapToGrid and picks moveAndSnapSelection or the
// plain move, so a one-pixel nudge either lands on the next grid line or moves
// the control by exactly one pixel.
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

function gsCheck(%label, %condition)
{
	echo(%condition ? ("GRID PASS: " @ %label) : ("GRID FAIL: " @ %label));
}

// A dialog is pushed onto the Canvas and nothing keeps a handle to it, so it is
// found the way it is displayed: as the Canvas's newest child.
function gsDialog(%class)
{
	for(%i = Canvas.getCount() - 1; %i >= 0; %i--)
	{
		%obj = Canvas.getObject(%i);
		if(%obj.class $= %class)
		{
			return %obj;
		}
	}

	return 0;
}

function gsNudgeBy(%ctrl)
{
	GuiEditor.brain.clearSelection();
	GuiEditor.brain.select(%ctrl);

	%before = getWord(%ctrl.getPosition(), 0);
	GuiEditor.brain.moveSelection(1, 0);

	return getWord(%ctrl.getPosition(), 0) - %before;
}

schedule(2000, 0, "gsSetup");

function gsSetup()
{
	ProjectManager.setProjectFolder("PlanetX");
	ModuleDatabase.ScanModules("PlanetX");
	ModuleDatabase.LoadExplicit("AppCore", 1);

	GuiEditor.open();

	// Off a grid line to start with, so a snapped nudge and an unsnapped one
	// cannot land on the same number by luck.
	$gsCtrl = new GuiButtonCtrl() { Position = "3 3"; Extent = "80 30"; Text = "A"; };
	GuiEditor.rootGui.add($gsCtrl);

	schedule(300, 0, "gsStepDialog");
}

//-----------------------------------------------------------------------------
// Setting the size, through the dialog the menu opens.
//-----------------------------------------------------------------------------

function gsStepDialog()
{
	GuiEditor.SetGridSize();

	%dialog = gsDialog("GuiEditorGridSizeDialog");
	gsCheck("the Set Grid Size dialog opened", isObject(%dialog));
	gsCheck("and it starts on the current size",
		%dialog.gridSizeBox.getText() == GuiEditor.brain.getGridSize());

	%dialog.gridSizeBox.setText("25");
	%dialog.onDone();

	gsCheck("the dialog set the grid size", GuiEditor.brain.getGridSize() == 25);
	gsCheck("and a nudge snaps to it", gsNudgeBy($gsCtrl) == 22);

	schedule(300, 0, "gsStepToggle");
}

//-----------------------------------------------------------------------------
// Toggling it off and on, which is the Layout menu's Snap to Grid item.
//-----------------------------------------------------------------------------

function gsStepToggle()
{
	$gsCtrl.setPosition(3, 3);

	GuiEditor.SnapToGrid(false);
	gsCheck("snap off moves by the pixel", gsNudgeBy($gsCtrl) == 1);
	gsCheck("and the size is remembered while it is off",
		GuiEditor.brain.getGridSize() == 25);

	$gsCtrl.setPosition(3, 3);

	GuiEditor.SnapToGrid(true);
	gsCheck("the size survived the round trip", GuiEditor.brain.getGridSize() == 25);
	gsCheck("and snap is on again", gsNudgeBy($gsCtrl) == 22);

	schedule(300, 0, "gsDone");
}

function gsDone()
{
	echo("GRID DONE");
	quit();
}
