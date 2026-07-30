//-----------------------------------------------------------------------------
// Where a control lands when it arrives in the document.
//
// Two gestures put one there, and both used to be able to put it somewhere
// nobody can see:
//
//   a drag   GuiDragAndDropCtrl hit-tests from the drag control's PARENT, which
//            is the brain, and GuiControl::findHitControl answers "me" when no
//            child is hit -- whatever the point. So the brain heard
//            onControlDropped for every point on screen, and placed the control
//            at the cursor: drag one back onto the palette to change your mind
//            and it was added behind the palette.
//
//   a click  places the control in the middle of the container being worked in.
//            A real Gui is designed at 1024x768 and the canvas frame is a few
//            hundred pixels wide, so the middle of a container is regularly off
//            the side of the canvas -- behind the palette or the explorer.
//
// The drags here are built rather than posted. A real drag would need
// startDragging, which mouse-locks the canvas, and the point of the test is
// what onControlDropped does with the payload it is handed -- so the object
// graph a drag makes (payload inside a GuiDragAndDropCtrl inside the brain) is
// assembled directly, and the drag control is deleted afterwards exactly as
// GuiDragAndDropCtrl::onTouchUp deletes it.
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

function cdCheck(%label, %condition)
{
	if(%condition)
	{
		$Pass++;
		echo("CDROP PASS: " @ %label);
	}
	else
	{
		$Fail++;
		echo("CDROP FAIL: " @ %label);
	}
}

schedule(2000, 0, "cdSetup");

// A project, so there is a theme: an unthemed drop skips a whole branch of
// acceptControl and this suite is about the placing, not the theming.
function cdSetup()
{
	ProjectManager.setProjectFolder("PlanetX");
	ModuleDatabase.ScanModules("PlanetX");
	ModuleDatabase.LoadExplicit("AppCore", 1);

	GuiEditor.open();
	schedule(500, 0, "cdDropChecks");
}

//-----------------------------------------------------------------------------
// Helpers. Global coordinates throughout -- the position these callbacks are
// handed is local to the brain, which is why neither the code nor the test
// measures anything with it.
//-----------------------------------------------------------------------------

// The rect of a control, as "x y width height" in global coordinates.
function cdInside(%point, %ctrl)
{
	%x = getWord(%point, 0);
	%y = getWord(%point, 1);
	%at = %ctrl.getGlobalPosition();
	%ext = %ctrl.getExtent();

	return (%x >= getWord(%at, 0) && %y >= getWord(%at, 1) &&
		%x < getWord(%at, 0) + getWord(%ext, 0) &&
		%y < getWord(%at, 1) + getWord(%ext, 1));
}

// The whole control, not just its corner.
function cdWhollyInside(%ctrl, %container)
{
	%at = %ctrl.getGlobalPosition();
	%ext = %ctrl.getExtent();
	%far = (getWord(%at, 0) + getWord(%ext, 0) - 1) SPC
		(getWord(%at, 1) + getWord(%ext, 1) - 1);

	return cdInside(%at, %container) && cdInside(%far, %container);
}

// The object graph a drag has when the mouse comes up, without the mouse lock.
// beginDrag grabs the payload by the middle, so the cursor is its centre.
function cdBuildDrag(%payload, %cursor)
{
	%brainAt = GuiEditor.brain.getGlobalPosition();
	%size = %payload.getExtent();

	%x = getWord(%cursor, 0) - getWord(%brainAt, 0) - (getWord(%size, 0) / 2);
	%y = getWord(%cursor, 1) - getWord(%brainAt, 1) - (getWord(%size, 1) / 2);

	%drag = new GuiDragAndDropCtrl()
	{
		Profile = "GuiDragAndDropProfile";
		HorizSizing = "anchorLeft";
		VertSizing = "anchorTop";
		Position = mFloor(%x) SPC mFloor(%y);
		Extent = %size;
		deleteOnMouseUp = true;
	};
	%drag.add(%payload);
	GuiEditor.brain.add(%drag);

	return %drag;
}

// Press, drag to %cursor, release. Answers the payload, which may be dead.
function cdDrop(%class, %cursor)
{
	%payload = eval("return new " @ %class @ "();");
	%drag = cdBuildDrag(%payload, %cursor);

	GuiEditor.brain.onControlDropped(%payload, %drag.getPosition());
	%drag.delete();

	return %payload;
}

//-----------------------------------------------------------------------------
// A drop has to be over the canvas.
//-----------------------------------------------------------------------------

function cdDropChecks()
{
	%root = GuiEditor.rootGui;
	%palette = GuiEditor.ctrlListWindow;

	// Aimed at the palette, which is where a hand goes to change its mind.
	%at = %palette.getGlobalPosition();
	%cursor = (getWord(%at, 0) + 60) SPC (getWord(%at, 1) + 140);
	cdCheck("the test is aiming off the canvas", !cdInside(%cursor, %root));

	%before = %root.getCount();
	%depth = GuiEditor.undoRecorder.undoCount();
	%payload = cdDrop("GuiButtonCtrl", %cursor);

	cdCheck("a drop over the palette adds nothing (" @ %root.getCount() @ ")",
		%root.getCount() == %before);
	cdCheck("the control it was carrying goes with the drag", !isObject(%payload));
	cdCheck("and it is not an undo step (" @
		(GuiEditor.undoRecorder.undoCount() - %depth) @ ")",
		GuiEditor.undoRecorder.undoCount() == %depth);

	// The same gesture, ended over the canvas.
	%rootAt = %root.getGlobalPosition();
	%cursor = (getWord(%rootAt, 0) + 80) SPC (getWord(%rootAt, 1) + 90);
	%payload = cdDrop("GuiButtonCtrl", %cursor);

	cdCheck("a drop over the canvas is added (" @ %root.getCount() @ ")",
		%root.getCount() == %before + 1);
	cdCheck("it is in the add set", %payload.getGroup() == GuiEditor.brain.getCurrentAddSet());
	cdCheck("it landed under the cursor", %payload.getGlobalCenter() $= %cursor);

	// A drop lands twice -- once now, once 40ms later, because the container it
	// arrived in may have moved it. Let the second one happen before going on.
	schedule(200, 0, "cdAddSetChecks", %payload);
}

//-----------------------------------------------------------------------------
// A drag that wanders off the canvas must not change the container being worked
// in: the click gesture places into it, so losing it silently moves where the
// next click puts a control.
//-----------------------------------------------------------------------------

function cdAddSetChecks(%dropped)
{
	cdCheck("the drop stayed under the cursor after its second placing",
		cdWhollyInside(%dropped, GuiEditor.rootGui));

	// A container to be working in, sized so it is wholly on the canvas.
	%panel = new GuiControl()
	{
		Position = "20 20";
		Extent = "200 200";
	};
	ThemeManager.setProfile(%panel, "panelProfile");
	GuiEditor.rootGui.add(%panel);
	GuiEditor.brain.setCurrentAddSet(%panel);
	cdCheck("working in the panel", GuiEditor.brain.getCurrentAddSet() == %panel);

	%palette = GuiEditor.ctrlListWindow;
	%at = %palette.getGlobalPosition();
	%cursor = (getWord(%at, 0) + 60) SPC (getWord(%at, 1) + 140);

	%payload = new GuiButtonCtrl();
	%drag = cdBuildDrag(%payload, %cursor);
	GuiEditor.brain.onControlDragged(%payload, %drag.getPosition());
	%drag.delete();

	cdCheck("dragging off the canvas keeps the container (" @
		GuiEditor.brain.getCurrentAddSet() @ ")",
		GuiEditor.brain.getCurrentAddSet() == %panel);

	schedule(100, 0, "cdClickChecks", %panel);
}

//-----------------------------------------------------------------------------
// A click places in the middle of the container being worked in.
//-----------------------------------------------------------------------------

function cdClickChecks(%panel)
{
	%tile = cdFindTile("GuiButtonCtrl");
	cdCheck("found the button tile", isObject(%tile));

	// In the panel: dead centre of it.
	%probe = new GuiButtonCtrl();
	%size = %probe.getExtent();
	%at = %panel.getGlobalPosition();
	%room = %panel.getExtent();
	%want = mFloor(getWord(%at, 0) + ((getWord(%room, 0) - getWord(%size, 0)) / 2)) SPC
		mFloor(getWord(%at, 1) + ((getWord(%room, 1) - getWord(%size, 1)) / 2));
	%probe.delete();

	%tile.onClick();
	%ctrl = %panel.getObject(%panel.getCount() - 1);
	cdCheck("clicking placed it in the panel", %ctrl.getGroup() == %panel);
	cdCheck("in the middle of it (" @ %ctrl.getGlobalPosition() @ " wanted " @ %want @ ")",
		%ctrl.getGlobalPosition() $= %want);
	cdCheck("which is on the canvas", cdWhollyInside(%ctrl, GuiEditor.rootGui));

	// And with nothing selected, the middle of the canvas itself.
	GuiEditor.brain.setCurrentAddSet(GuiEditor.rootGui);
	%tile.onClick();
	%ctrl = GuiEditor.rootGui.getObject(GuiEditor.rootGui.getCount() - 1);
	cdCheck("with no container chosen it goes on the canvas",
		cdWhollyInside(%ctrl, GuiEditor.rootGui));

	schedule(100, 0, "cdBigGuiChecks");
}

//-----------------------------------------------------------------------------
// The case the canvas frame cannot show: a Gui designed at 1024x768 in a canvas
// a few hundred pixels wide. The middle of a container that runs off the side
// of the canvas is behind the palette, so the click has to place into the part
// of the container that can actually be seen.
//-----------------------------------------------------------------------------

function cdBigGuiChecks()
{
	%content = TAMLRead(testRoot("PlanetX/PlanetXGame/gui/titleGui.gui.taml"));
	GuiEditor.DisplayGuiContent(%content, false);

	%root = GuiEditor.rootGui;
	%inner = %content.getObject(0);
	GuiEditor.brain.setCurrentAddSet(%inner);

	cdCheck("the loaded Gui is wider than the canvas (" @
		getWord(%content.getExtent(), 0) @ " in " @ getWord(%root.getExtent(), 0) @ ")",
		getWord(%content.getExtent(), 0) > getWord(%root.getExtent(), 0));
	cdCheck("its container runs off the canvas", !cdWhollyInside(%inner, %root));

	%tile = cdFindTile("GuiButtonCtrl");
	%tile.onClick();
	%ctrl = %inner.getObject(%inner.getCount() - 1);

	cdCheck("clicking placed it in the container", %ctrl.getGroup() == %inner);
	cdCheck("somewhere that can be seen (" @ %ctrl.getGlobalPosition() @ ")",
		cdWhollyInside(%ctrl, %root));

	// Specifically the middle of the visible part: the container starts at the
	// canvas's left edge and runs 1024 wide, so that is the middle of the
	// canvas across, and the middle of the overlap down.
	%overlap = cdOverlapRect(%inner, %root);
	%size = %ctrl.getExtent();
	%want = mFloor(getWord(%overlap, 0) + ((getWord(%overlap, 2) - getWord(%size, 0)) / 2)) SPC
		mFloor(getWord(%overlap, 1) + ((getWord(%overlap, 3) - getWord(%size, 1)) / 2));
	cdCheck("in the middle of the part on the canvas (" @
		%ctrl.getGlobalPosition() @ " wanted " @ %want @ ")",
		%ctrl.getGlobalPosition() $= %want);

	echo("CDROP DONE  " @ $Pass @ " passed, " @ $Fail @ " failed");
	quit();
}

// "x y width height" of what two controls share, in global coordinates.
function cdOverlapRect(%a, %b)
{
	%aAt = %a.getGlobalPosition();
	%aExt = %a.getExtent();
	%bAt = %b.getGlobalPosition();
	%bExt = %b.getExtent();

	%left = mFloor(mGetMax(getWord(%aAt, 0), getWord(%bAt, 0)));
	%top = mFloor(mGetMax(getWord(%aAt, 1), getWord(%bAt, 1)));
	%right = mFloor(mGetMin(getWord(%aAt, 0) + getWord(%aExt, 0),
		getWord(%bAt, 0) + getWord(%bExt, 0)));
	%bottom = mFloor(mGetMin(getWord(%aAt, 1) + getWord(%aExt, 1),
		getWord(%bAt, 1) + getWord(%bExt, 1)));

	return %left SPC %top SPC (%right - %left) SPC (%bottom - %top);
}

function cdFindTile(%key)
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
