
function GuiEditorBrain::onAdd(%this)
{
    %this.setFirstResponder();
    %this.setSnapToGrid("10");
}

//-----------------------------------------------------------------------------
// A control arriving by drag.
//
// Both of these are only ever called about a point that is over the canvas -
// except that they are not. GuiDragAndDropCtrl hit-tests from the drag control's
// PARENT, which is this control, and GuiControl::findHitControl answers "me"
// when none of its children was hit, whatever the point it was asked about. So
// this hears about a drop anywhere on the screen: over the palette, over the
// explorer, over the menu bar. Left to itself it then adds the control at the
// cursor, which is how dragging one back onto the palette to change your mind
// put a control behind the palette.
//
// So each of them asks first. A drop that is not over the canvas is not a drop:
// nothing is added, and the payload dies with the drag control that carried it
// (deleteOnMouseUp, and a group deletes what it holds).
//-----------------------------------------------------------------------------

function GuiEditorBrain::onControlDragged(%this, %payload, %position)
{
	// Off the canvas: keep the container that is being worked in. Hit-testing a
	// point outside the Gui answers the Gui itself, so without this a drag that
	// strayed over a tool window quietly reset the add set to the root - and the
	// click gesture places into the add set, so the next click moved with it.
	if(!%this.isOverCanvas(%this.cursorFrom(%payload)))
	{
		return;
	}

	%x = getWord(%position, 0);
	%y = getWord(%position, 1);
	%target = %this.root.findHitControl(%x, %y);

	while(! %target.isContainer )
	{
		%target = %target.getParent();
	}

	if(%target != %this.getCurrentAddset())
	{
		%this.setCurrentAddSet(%target);
	}
}

function GuiEditorBrain::onControlDropped(%this, %payload, %position)
{
   if(!%this.isOverCanvas(%this.cursorFrom(%payload)))
   {
      return;
   }

   %pos = %payload.getGlobalPosition();
   %x = getWord(%pos, 0);
   %y = getWord(%pos, 1);

   %this.acceptControl(%payload);

   %payload.setPositionGlobal(%x, %y);
   %this.schedule(40, "finishControlDropped", %payload, %x, %y);
}

//-----------------------------------------------------------------------------
// Where the canvas is, and where the pointer is over it.
//
// Everything here is in global coordinates. The position the two callbacks above
// are handed is not: GuiDragAndDropCtrl::sendDragEvent builds it from the drag
// control's own bounds, which are local to this control - which is why neither
// of them measures anything with it, and why the payload is asked instead. A
// drag grabs a control by the middle (see GuiEditorControlTile::beginDrag), so
// the middle of the payload is where the pointer is.
//-----------------------------------------------------------------------------

function GuiEditorBrain::cursorFrom(%this, %payload)
{
   %at = %payload.getGlobalPosition();
   %size = %payload.getExtent();

   return mFloor(getWord(%at, 0) + (getWord(%size, 0) / 2)) SPC
      mFloor(getWord(%at, 1) + (getWord(%size, 1) / 2));
}

function GuiEditorBrain::isOverCanvas(%this, %point)
{
   %canvas = %this.canvasRect();
   %x = getWord(%point, 0);
   %y = getWord(%point, 1);

   return %x >= getWord(%canvas, 0) && %y >= getWord(%canvas, 1) &&
      %x < getWord(%canvas, 0) + getWord(%canvas, 2) &&
      %y < getWord(%canvas, 1) + getWord(%canvas, 3);
}

// "x y width height" of the Gui being edited.
function GuiEditorBrain::canvasRect(%this)
{
   return %this.root.getGlobalPosition() SPC %this.root.getExtent();
}

// Where a control goes when the gesture never said - which is what a click on a
// palette tile is: the middle of the container being worked in, or of as much of
// that container as the canvas can show.
//
// The clipping is not fussiness. A Gui is authored at its own size, usually
// 1024x768, and the canvas frame is a few hundred pixels wide, so a container
// runs off the side of it more often than not - and the middle of one that does
// is behind the palette or the explorer, where the control cannot be seen or
// dragged. Clipping first means a container that fits, which is every container
// in a Gui the size of the canvas, still gets its own exact middle.
function GuiEditorBrain::centredPlacement(%this, %payload)
{
   %target = %this.getCurrentAddSet();
   if(!isObject(%target))
   {
      %target = %this.root;
   }

   %room = %this.visiblePartOf(%target);
   %size = %payload.getExtent();

   %x = getWord(%room, 0) + ((getWord(%room, 2) - getWord(%size, 0)) / 2);
   %y = getWord(%room, 1) + ((getWord(%room, 3) - getWord(%size, 1)) / 2);

   return mFloor(%x) SPC mFloor(%y);
}

// What a control and the canvas share, as "x y width height". A container with
// nothing on the canvas at all answers the whole canvas: there is no good
// placement inside it, and off-screen is not a better answer than visible.
function GuiEditorBrain::visiblePartOf(%this, %ctrl)
{
   %canvas = %this.canvasRect();
   %at = %ctrl.getGlobalPosition();
   %size = %ctrl.getExtent();

   %left = mFloor(mGetMax(getWord(%at, 0), getWord(%canvas, 0)));
   %top = mFloor(mGetMax(getWord(%at, 1), getWord(%canvas, 1)));
   %right = mFloor(mGetMin(getWord(%at, 0) + getWord(%size, 0),
      getWord(%canvas, 0) + getWord(%canvas, 2)));
   %bottom = mFloor(mGetMin(getWord(%at, 1) + getWord(%size, 1),
      getWord(%canvas, 1) + getWord(%canvas, 3)));

   if(%right <= %left || %bottom <= %top)
   {
      return %canvas;
   }

   return %left SPC %top SPC (%right - %left) SPC (%bottom - %top);
}

// Everything about a control arriving in the document except where it lands.
//
// A drop knows where the cursor was and so places the control in global
// coordinates, twice, because the container it landed in may have moved it. A
// paste has no cursor: it knows the position the control held in its old parent,
// which is a local one, and sets it before calling this - so the properties pane
// reads the position the control is going to keep. Everything else is the same
// for both, and lives here rather than in each caller: the undo record, the
// theme, the selection, and the events the Explorer tree and the panes listen
// for.
function GuiEditorBrain::acceptControl(%this, %payload)
{
   // The add itself is the undo step, recorded from onAddNewCtrl below.
   %this.addNewCtrl(%payload);

   // Between the two, so the page is in the book before the theme walks it and
   // after the book's own add has been recorded. A tab book with no pages is a
   // book with nothing to put anything in, and the palette no longer offers the
   // page that would fix that.
   //
   // The page is added with a plain add(), which announces nothing, so nothing
   // records it: undo of the drop puts the book in the trash with its page
   // inside, which is one step, which is what the user did.
   if(%payload.isMemberOfClass("GuiTabBookCtrl") && %payload.getCount() == 0)
   {
      %this.newTabPage(%payload);
   }

   %this.adoptControl(%payload);
}

// The half of arriving that is the same however a control got here: the theme it
// takes on, and the events that tell the Explorer tree and the panes about it.
//
// Split out because a tab page arrives by neither of the routes above. Its book
// makes it, from the "+" tab, and it needs all of this and none of the
// placement.
//
// The undo record is deliberately NOT here, because what counts as one step
// differs by caller: a dropped control is a step of its own, and a page seeded
// inside a book that is itself arriving is part of that book arriving.
function GuiEditorBrain::adoptControl(%this, %ctrl)
{
   // A control arrives wearing whatever its C++ constructor named - a
   // GuiWindowCtrl names five, from GuiWindowProfile down - so put it on the
   // Gui's theme straight away. Themed on arrival is the whole point: the drop
   // is the last time anyone should have to think about which profile a button
   // wants.
   //
   // This has to run after the control has a parent, because the parent decides
   // which category it takes (a control sitting directly on the root is the
   // Gui's backdrop and gets Panel, not Label). But adding it is also what
   // announces the selection, so everything that inspects the control has
   // already read it wearing the constructor's profiles. Rethemed tells them to
   // look again.
   //
   // None of it is recorded: theming a control that is arriving is part of it
   // arriving, not a second thing the user did. Undo puts the whole control in
   // the trash, where it keeps its profiles and its position, so redo has
   // nothing to put back but the control itself.
   %theme = GuiEditor.themeByName(GuiEditor.themeName);
   if(isObject(%theme))
   {
      GuiEditor.undoRecorder.suspend();
      GuiEditor.themeApplier.applyToBranch(%ctrl, %theme, false);
      GuiEditor.undoRecorder.resume();

      %this.postEvent("Rethemed", %ctrl);
   }

   %this.setFirstResponder();
   %this.postEvent("AddControl", %ctrl);
   %this.postEvent("Inspect", %ctrl);
}

//-----------------------------------------------------------------------------
// Tab pages.
//
// A GuiTabPageCtrl is the one control the palette does not offer, because it is
// the one control that means nothing anywhere but inside a GuiTabBookCtrl. A
// book makes its own instead: one when the book is dropped, and one for every
// click on the "+" tab it draws at the end of its strip while the Gui is being
// authored.
//-----------------------------------------------------------------------------

// GuiTabBookCtrl::requestNewPage, from a click on the "+" tab.
function GuiEditorBrain::onAddTabPage(%this, %book)
{
    if(!isObject(%book))
    {
        return;
    }

    GuiEditor.undoRecorder.begin("Add Tab Page", "");
    %page = %this.newTabPage(%book);
    GuiEditor.undoRecorder.recordAdd(%page, "Add Tab Page");
    %this.adoptControl(%page);
    GuiEditor.undoRecorder.end();

    // By index, never by name: captions are not identities - two pages can both
    // read "Page 3" once one has been deleted - and selectPageName takes the
    // first match.
    %book.selectPage(%book.getCount() - 1);

    // The add set first, because setting it clears the selection. Pointing it at
    // the new page means the next control dropped lands in the page that was
    // just made, which is the only reason anyone clicks "+".
    %this.setCurrentAddSet(%page);
    %this.selectList(%page);
}

// The one place a page is made, so that a page seeded with its book and a page
// added later are the same object.
function GuiEditorBrain::newTabPage(%this, %book)
{
    %number = %this.freePageNumber(%book);

    %page = new GuiTabPageCtrl()
    {
        Text = "Page " @ %number;
    };

    // What C++ addNewPage names, so a book built in a project with no theme
    // loaded still gets a page that draws like one. adoptControl writes over it
    // a moment later wherever there IS a theme.
    if(isObject(GuiTabPageProfile))
    {
        %page.setProfile(GuiTabPageProfile);
    }

    %book.add(%page);

    return %page;
}

// The lowest number no tab in the book is already using. Counting the pages
// instead would repeat one: three pages, delete "Page 2", and the next page
// would be a second "Page 3".
//
// Bounded rather than open: among the numbers 1 to N+1 at least one is free of N
// pages, so the loop always finds an answer inside it.
function GuiEditorBrain::freePageNumber(%this, %book)
{
    %count = %book.getCount();

    for(%n = 1; %n <= (%count + 1); %n++)
    {
        %taken = false;
        for(%i = 0; %i < %count; %i++)
        {
            if(%book.getObject(%i).getText() $= ("Page " @ %n))
            {
                %taken = true;
                break;
            }
        }

        if(!%taken)
        {
            return %n;
        }
    }

    return %count + 1;
}

function GuiEditorBrain::finishControlDropped(%this, %payload, %x, %y)
{
   %payload.setPositionGlobal(%x, %y);
}

function GuiEditorBrain::startRadioSilence(%this)
{
    %this.removeAllListeners();
}

function GuiEditorBrain::endRadioSilence(%this)
{
    %this.addListener(GuiEditor.explorerWindow);
    %this.addListener(GuiEditor.inspectorWindow);
}

//Source callbacks - Events that happened with this control and need to be relayed to other controls.
function GuiEditorBrain::onEdit(%this, %ctrl)
{
    %this.postEvent("Edit", %ctrl);
}

function GuiEditorBrain::onRemoveSelected(%this,%ctrl)
{
    %this.postEvent("ClearInspect", %ctrl);
    %this.toggleMenuItems();
}

function GuiEditorBrain::onClearSelected(%this)
{
    %this.postEvent("ClearInspectAll");
    %this.toggleMenuItems();
}

function GuiEditorBrain::onAddSelected(%this, %ctrl)
{
    %this.postEvent("AlsoInspect", %ctrl);
    %this.toggleMenuItems();
}

function GuiEditorBrain::onDelete(%this)
{
	%this.postEvent("ObjectRemoved");
    %this.toggleMenuItems();
}

function GuiEditorBrain::onSelectionParentChange(%this, %parent)
{
    %this.postEvent("ParentChange", %parent);
    %this.toggleMenuItems();
}

//Receiving Callbacks - Events that happened at other controls and need to be reflected with this control.
function GuiEditorBrain::onInspect(%this, %ctrl)
{
    %this.startRadioSilence();
    %this.clearSelection();
	%this.select(%ctrl);
    %this.endRadioSilence();
    %this.toggleMenuItems();
}

function GuiEditorBrain::onAlsoInspect(%this, %ctrl)
{
    %this.startRadioSilence();
	%this.addSelection(%ctrl);
    %this.endRadioSilence();
    %this.toggleMenuItems();
}

function GuiEditorBrain::onClearInspect(%this, %ctrl)
{
    %this.startRadioSilence();
	%this.removeSelection(%ctrl);
    %this.endRadioSilence();
    %this.toggleMenuItems();
}

function GuiEditorBrain::onClearInspectAll(%this)
{
    %this.startRadioSilence();
	%this.clearSelection();
    %this.endRadioSilence();
    %this.toggleMenuItems();
}

// Something else asked for the selection to go. The Explorer tree does, from its
// own Delete key.
function GuiEditorBrain::onObjectRemoved(%this, %ctrl)
{
    %this.startRadioSilence();
	%this.deleteSelection();
    %this.endRadioSilence();

    // Then say that it went, which the receiving half of this bus does not
    // normally do. It has to here: postEvent does not deliver to whoever posted,
    // so the tree that asked for this delete has told its own window nothing,
    // and the window is what refreshes it. Announcing from here rather than at
    // each caller is what makes every route into a delete - this one, the Delete
    // key on the canvas, and Cut - leave the panes agreeing with the document.
    //
    // onDelete carries the menu update with it, so there is none of its own.
    %this.onDelete();
}

//-----------------------------------------------------------------------------
// Undo. The C++ edit control has always announced every edit it makes at the
// moment it makes it - guiEditCtrl.cc calls all of these, and each one sits
// beside a bare "// undo" comment marking where the recording used to happen.
// Nothing implemented them until now.
//
// The pairs matter: what a drag or a nudge did is only known once it is over,
// so the pre half remembers where everything was and the post half works out
// what actually moved. A mouse-down that only selected records nothing.
//-----------------------------------------------------------------------------

// Mouse-down on a selection, before a drag-move or a handle-resize.
function GuiEditorBrain::onPreEdit(%this, %selection)
{
    GuiEditor.undoRecorder.snapshot(%selection);
}

function GuiEditorBrain::onPostEdit(%this, %selection)
{
    GuiEditor.undoRecorder.commitGeometry("", "");
}

// Arrow-key and menu nudges, which the C++ gives a callback of their own
// precisely so that a run of them can be folded into one action.
function GuiEditorBrain::onPreSelectionNudged(%this, %selection)
{
    GuiEditor.undoRecorder.snapshot(%selection);
}

function GuiEditorBrain::onPostSelectionNudged(%this, %selection)
{
    GuiEditor.undoRecorder.commitGeometry("", "nudge");
}

// Fired before the controls are moved to the trash, which is the only moment
// where each one still knows the parent and index it has to go back to.
function GuiEditorBrain::onTrashSelection(%this, %selection)
{
    GuiEditor.undoRecorder.recordDeleteSelection(%selection);
}

// Fired after the control has been put in the add set.
function GuiEditorBrain::onAddNewCtrl(%this, %ctrl)
{
    GuiEditor.undoRecorder.recordAdd(%ctrl, "");
}

// The same for a whole set of them, which is how a loaded selection arrives.
function GuiEditorBrain::onAddNewCtrlSet(%this, %selection)
{
    GuiEditor.undoRecorder.begin("Add Controls", "");
    for(%i = 0; %i < %selection.getCount(); %i++)
    {
        GuiEditor.undoRecorder.recordAdd(%selection.getObject(%i), "");
    }
    GuiEditor.undoRecorder.end();
}

// Put the selection on the controls a replay changed, and tell everyone.
function GuiEditorBrain::restoreSelection(%this, %list)
{
    %this.selectList(%list);
}

// Select exactly these controls, and say so.
//
// The announcement has to be made here, because addSelection makes none:
// it is the receiving half of the bus - what this class calls, under radio
// silence, when the tree or the pane has already announced a selection - so it
// changes the C++ selection and says nothing. Calling it on its own leaves the
// canvas drawing handles round a control the properties pane has never heard
// of, and the clearSelection ahead of it has already emptied the pane.
function GuiEditorBrain::selectList(%this, %list)
{
    // Already the selection, with values that changed underneath it - which is
    // the commonest undo there is: change a setting, press Ctrl+Z. Re-announcing
    // would rebuild the whole properties pane, when all it needs is to re-read
    // the control it is already showing.
    if(%list $= %this.selectionList())
    {
        %this.postEvent("Replayed");
        return;
    }

    %this.clearSelection();

    for(%i = 0; %i < getWordCount(%list); %i++)
    {
        %ctrl = getWord(%list, %i);
        %this.addSelection(%ctrl);

        // What the C++ would have called had it done the selecting, so there is
        // one definition of what announcing a selection means.
        %this.onAddSelected(%ctrl);
    }
}

function GuiEditorBrain::selectionList(%this)
{
    %set = %this.getSelected();
    %list = "";

    for(%i = 0; %i < %set.getCount(); %i++)
    {
        %ctrl = %set.getObject(%i);
        %list = (%list $= "") ? %ctrl : (%list SPC %ctrl);
    }

    return %list;
}

function GuiEditorBrain::toggleMenuItems(%this)
{
    %count = %this.getSelected().getCount();
    EditorCore.menuBar.setMenuActive("Deselect", %count != 0);
    EditorCore.menuBar.setMenuActive("Cut", %count != 0);
    EditorCore.menuBar.setMenuActive("Copy", %count != 0);
    EditorCore.menuBar.setMenuActive("Nudge Up", %count != 0);
    EditorCore.menuBar.setMenuActive("Nudge Down", %count != 0);
    EditorCore.menuBar.setMenuActive("Nudge Left", %count != 0);
    EditorCore.menuBar.setMenuActive("Nudge Right", %count != 0);
    EditorCore.menuBar.setMenuActive("Expand Height", %count != 0);
    EditorCore.menuBar.setMenuActive("Shrink Height", %count != 0);
    EditorCore.menuBar.setMenuActive("Expand Width", %count != 0);
    EditorCore.menuBar.setMenuActive("Shrink Width", %count != 0);
    EditorCore.menuBar.setMenuActive("Align Top", %count > 1);
    EditorCore.menuBar.setMenuActive("Align Bottom", %count > 1);
    EditorCore.menuBar.setMenuActive("Align Left", %count > 1);
    EditorCore.menuBar.setMenuActive("Align Right", %count > 1);
    EditorCore.menuBar.setMenuActive("Center Horizontally", %count > 1);
    EditorCore.menuBar.setMenuActive("Space Vertically", %count > 2);
    EditorCore.menuBar.setMenuActive("Space Horizontally", %count > 2);
    EditorCore.menuBar.setMenuActive("Bring to Front", %count == 1);
    EditorCore.menuBar.setMenuActive("Push to Back", %count == 1);
}