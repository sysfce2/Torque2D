// The shared menu bar swapping whole menus as the editor tab changes.
// Run: tests/run.ps1 menuSwap  ; grep MSW in tests/logs/.
//
// The bar always reads Torque2D | the open editor's menus | Theme. An editor
// with no menus of its own -- the Console, the Project Manager -- leaves the two
// permanent ones and nothing between them.
//
// What is worth asserting here rather than anywhere else:
//
//   * Theme ends up LAST every time. The bar links the chain its keyboard walk
//     follows by assuming each new menu was appended, so the fixed tail has to
//     come off and go back on around every swap. If that stops happening, this is
//     what notices.
//   * The two File menus are told apart BY OBJECT. Both are called "File" and
//     that is the whole point of swapping rather than sharing -- a check that
//     looked them up by text could not tell which editor's bar it was reading.
//   * Nothing is deleted. A parked menu is still an object, in its own editor's
//     group rather than in the bar.
//
// The accelerator half is in menuSwap.input.ps1, because the only way to prove a
// shortcut reaches the right editor is to press it.

setLogMode(1);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

function mswCheck(%label, %cond)
{
	if(%cond) echo("MSW PASS: " @ %label);
	else      echo("MSW FAIL: " @ %label);
}

// The bar's top-level menus as a space-separated list of their texts, which is
// the shape the order checks below read best in.
function mswBarText()
{
	%bar = EditorCore.menuBar;
	%list = "";
	for(%i = 0; %i < %bar.getCount(); %i++)
	{
		%text = %bar.getObject(%i).getText();
		%list = (%list $= "") ? %text : (%list @ "|" @ %text);
	}

	return %list;
}

function mswLastMenu()
{
	%bar = EditorCore.menuBar;
	return %bar.getObject(%bar.getCount() - 1);
}

// Pages register in module load order: EditorConsole, ProjectManager, AssetAdmin,
// GuiEditor. Selecting a tab is what calls close() on the old editor and open()
// on the new one, which is what moves the menus.
$mswConsole = 0;
$mswProjects = 1;
$mswAssets = 2;
$mswGui = 3;

testExec("editor/main.cs");
schedule(2500, 0, "mswStep1");

//-----------------------------------------------------------------------------
// The Console has no menus, so the bar is only its two permanent ends. This is
// also the state the editor starts in -- EditorCore::open selects page 0.
//-----------------------------------------------------------------------------

function mswStep1()
{
	ProjectManager.setProjectFolder("menuSwapSmokeProject");

	mswCheck("the menu bar exists", isObject(EditorCore.menuBar));
	mswCheck("EditorCore holds the Theme menu", isObject(EditorCore.themeMenu));
	mswCheck("and a group to park menus in", isObject(EditorCore.menuPark));

	EditorCore.tabBook.selectPage($mswConsole);
	schedule(400, 0, "mswStep2");
}

function mswStep2()
{
	mswCheck("the Console leaves only the permanent menus (" @ mswBarText() @ ")",
		mswBarText() $= "Torque2D|Theme");
	mswCheck("no set is on the bar", !isObject(EditorCore.activeMenus));

	$mswAcceleratorsBare = Canvas.getAcceleratorCount();
	mswCheck("the canvas is listening for some accelerators", $mswAcceleratorsBare > 0);

	EditorCore.tabBook.selectPage($mswGui);
	schedule(600, 0, "mswStep3");
}

//-----------------------------------------------------------------------------
// The Gui Editor brings four.
//-----------------------------------------------------------------------------

function mswStep3()
{
	mswCheck("the Gui Editor's menus arrived (" @ mswBarText() @ ")",
		mswBarText() $= "Torque2D|File|Edit|Layout|Select|Theme");
	mswCheck("Theme is still last", mswLastMenu() == EditorCore.themeMenu);
	mswCheck("and the set on the bar is the Gui Editor's",
		EditorCore.activeMenus == GuiEditor.menus);

	$mswGuiFile = EditorCore.menuBar.getObject(1);
	mswCheck("the File menu on the bar is the Gui Editor's own object",
		$mswGuiFile == GuiEditor.menus.revert.getGroup());

	// Four menus of items is a lot of accelerators; the table has to have grown.
	$mswAcceleratorsGui = Canvas.getAcceleratorCount();
	mswCheck("the accelerator table grew with them (" @ $mswAcceleratorsBare @ " -> "
		@ $mswAcceleratorsGui @ ")", $mswAcceleratorsGui > $mswAcceleratorsBare);

	EditorCore.tabBook.selectPage($mswAssets);
	schedule(800, 0, "mswStep4");
}

//-----------------------------------------------------------------------------
// The Asset Manager brings two, called the same thing and meaning something
// else.
//-----------------------------------------------------------------------------

function mswStep4()
{
	mswCheck("the Asset Manager's menus replaced them (" @ mswBarText() @ ")",
		mswBarText() $= "Torque2D|File|Edit|Theme");
	mswCheck("Theme is still last", mswLastMenu() == EditorCore.themeMenu);
	mswCheck("the set on the bar is the Asset Manager's",
		EditorCore.activeMenus == AssetAdmin.menus);

	// By object, not by text. Both menus are called "File".
	%file = EditorCore.menuBar.getObject(1);
	mswCheck("the File menu is a different object from the Gui Editor's",
		%file != $mswGuiFile);
	mswCheck("and it is the one the Asset Manager built",
		%file == AssetAdmin.menus.save.getGroup());

	// Nothing was thrown away on the way out.
	mswCheck("the Gui Editor's Revert still exists while parked",
		isObject(GuiEditor.menus.revert));
	mswCheck("its File menu is parked in the Gui Editor's own group",
		$mswGuiFile.getGroup() == GuiEditor.menus.parked);
	mswCheck("which is not the bar", GuiEditor.menus.parked != EditorCore.menuBar);

	// A parked item is not accelerator-mediated, so its command still runs when
	// something asks for it directly. Parking takes away the shortcut, not the
	// menu item.
	mswCheck("a parked item is still active and visible",
		$mswGuiFile.isVisible());

	schedule(300, 0, "mswStep5");
}

//-----------------------------------------------------------------------------
// Back and forth. The bar repairs its own bookkeeping on every add and remove,
// and that is the part most likely to rot.
//-----------------------------------------------------------------------------

function mswStep5()
{
	$mswThemeOnAt = "";
	$mswCycle = 0;
	mswCycle();
}

function mswCycle()
{
	$mswCycle++;

	if($mswCycle > 3)
	{
		schedule(400, 0, "mswStep6");
		return;
	}

	EditorCore.tabBook.selectPage($mswGui);
	schedule(400, 0, "mswCycleBack");
}

function mswCycleBack()
{
	mswCheck("cycle " @ $mswCycle @ ": the Gui Editor's four came back",
		mswBarText() $= "Torque2D|File|Edit|Layout|Select|Theme");

	EditorCore.tabBook.selectPage($mswAssets);
	schedule(400, 0, "mswCycleCheck");
}

function mswCycleCheck()
{
	mswCheck("cycle " @ $mswCycle @ ": and the Asset Manager's two came back",
		mswBarText() $= "Torque2D|File|Edit|Theme");
	mswCheck("cycle " @ $mswCycle @ ": Theme is still last",
		mswLastMenu() == EditorCore.themeMenu);

	mswCycle();
}

//-----------------------------------------------------------------------------
// The Theme menu is a radio group, and it made all those trips off and back on
// the bar. Exactly one of its four is still on, and it is the same one.
//-----------------------------------------------------------------------------

function mswStep6()
{
	%theme = EditorCore.themeMenu;
	%on = 0;
	%which = "";
	for(%i = 0; %i < %theme.getCount(); %i++)
	{
		%item = %theme.getObject(%i);
		if(%item.IsOn)
		{
			%on++;
			%which = %item.getText();
		}
	}

	mswCheck("the Theme menu still has four themes", %theme.getCount() == 4);
	mswCheck("exactly one is chosen (" @ %which @ ")", %on == 1);
	mswCheck("and it is the one the editor started on", %which $= "Construction Vest");

	EditorCore.tabBook.selectPage($mswProjects);
	schedule(500, 0, "mswStep7");
}

//-----------------------------------------------------------------------------
// The Project Manager, like the Console, has none - and needed no code to say
// so. The editor it replaced took its own menus with it.
//-----------------------------------------------------------------------------

function mswStep7()
{
	mswCheck("the Project Manager leaves the permanent menus (" @ mswBarText() @ ")",
		mswBarText() $= "Torque2D|Theme");
	mswCheck("no set is on the bar", !isObject(EditorCore.activeMenus));
	mswCheck("and the accelerator table shrank back (" @ Canvas.getAcceleratorCount() @ ")",
		Canvas.getAcceleratorCount() == $mswAcceleratorsBare);

	// Both editors' menus are alive, parked, and nowhere near the bar.
	mswCheck("the Gui Editor's menus are all parked",
		GuiEditor.menus.parked.getCount() == GuiEditor.menus.menuCount);
	mswCheck("so are the Asset Manager's",
		AssetAdmin.menus.parked.getCount() == AssetAdmin.menus.menuCount);

	schedule(300, 0, "mswStep8");
}

//-----------------------------------------------------------------------------
// A set refreshes itself as it goes back on the bar, so what it shows is the
// editor's state now rather than the state it was carrying when it came off.
//-----------------------------------------------------------------------------

function mswStep8()
{
	EditorCore.tabBook.selectPage($mswGui);
	schedule(500, 0, "mswStep9");
}

function mswStep9()
{
	mswCheck("an untitled document leaves Revert greyed", !GuiEditor.menus.revert.Active);

	// Move the document underneath the menu without telling it. Revert is offered
	// on whether the document has a file to go back to, and this is the field it
	// reads -- written directly, so nothing along the way calls refreshFileMenu.
	$mswFilePathWas = GuiEditor.filePath;
	GuiEditor.filePath = "notARealFile.gui.taml";
	mswCheck("and it stays greyed until something asks again",
		!GuiEditor.menus.revert.Active);

	EditorCore.tabBook.selectPage($mswConsole);
	schedule(400, 0, "mswStep10");
}

function mswStep10()
{
	EditorCore.tabBook.selectPage($mswGui);
	schedule(500, 0, "mswStep11");
}

function mswStep11()
{
	// The contract that replaced forceRefreshMenu: going back on the bar re-asks
	// the editor rather than showing what the menu was carrying when it left.
	mswCheck("coming back re-asked the editor, and Revert is offered now",
		GuiEditor.menus.revert.Active);

	GuiEditor.filePath = $mswFilePathWas;
	GuiEditor.refreshFileMenu();
	mswCheck("putting the document back greys it again", !GuiEditor.menus.revert.Active);

	schedule(200, 0, "mswStep12");
}

//-----------------------------------------------------------------------------
// Groups: the items that grey together because they all answer one question.
// Driven directly rather than through a selection, so this says what it means --
// that the group registry holds every item that was put in it.
//-----------------------------------------------------------------------------

function mswStep12()
{
	%menus = GuiEditor.menus;

	mswCheck("the selection group holds thirteen items",
		%menus.groupCount["selection"] == 13);
	mswCheck("the align group holds five", %menus.groupCount["align"] == 5);
	mswCheck("the space group holds two", %menus.groupCount["space"] == 2);
	mswCheck("the restack group holds two", %menus.groupCount["restack"] == 2);

	// The first item of each group specifically. A group's count starts life
	// unset, and reading it as an index rather than as a number wrote the first
	// item to a slot nothing ever reads back -- Cut, Align Top, Space Vertically
	// and Bring to Front would each have stopped greying, and nothing else would.
	mswCheck("the first item of the selection group is registered (" @ %menus.groupItem["selection", 0].getText() @ ")",
		isObject(%menus.groupItem["selection", 0]));
	mswCheck("so is the first of align (" @ %menus.groupItem["align", 0].getText() @ ")",
		isObject(%menus.groupItem["align", 0]));
	mswCheck("so is the first of space (" @ %menus.groupItem["space", 0].getText() @ ")",
		isObject(%menus.groupItem["space", 0]));
	mswCheck("so is the first of restack (" @ %menus.groupItem["restack", 0].getText() @ ")",
		isObject(%menus.groupItem["restack", 0]));

	%menus.refreshSelection(0);
	mswCheck("nothing selected greys every item of every group",
		mswGroupAllActive(%menus, "selection") == 0 &&
		mswGroupAllActive(%menus, "align") == 0 &&
		mswGroupAllActive(%menus, "space") == 0 &&
		mswGroupAllActive(%menus, "restack") == 0);

	%menus.refreshSelection(1);
	mswCheck("one selected offers the whole selection group",
		mswGroupAllActive(%menus, "selection") == 1);
	mswCheck("and restack, which is a one-control command",
		mswGroupAllActive(%menus, "restack") == 1);
	mswCheck("but not align, which needs something to align to",
		mswGroupAllActive(%menus, "align") == 0);

	%menus.refreshSelection(2);
	mswCheck("two selected offers align", mswGroupAllActive(%menus, "align") == 1);
	mswCheck("but not spacing, which needs a middle", mswGroupAllActive(%menus, "space") == 0);
	mswCheck("and restack is gone again", mswGroupAllActive(%menus, "restack") == 0);

	%menus.refreshSelection(3);
	mswCheck("three selected offers spacing", mswGroupAllActive(%menus, "space") == 1);

	// Leave the menus describing the editor rather than the test.
	GuiEditor.brain.toggleMenuItems();

	schedule(200, 0, "mswStep13");
}

//-----------------------------------------------------------------------------
// The accelerators, which are the reason for swapping rather than greying.
//
// The canvas keeps one flat list of shortcuts, built by walking whatever is on
// show, and it does not check whether an item is active before firing it - only
// whether the item itself is, never its menu. So greying File left Ctrl+N still
// running the Gui Editor's New Gui from inside the Asset Manager. Nothing about
// that is visible from script: the list has no read-back beyond its size, and
// the only way to prove a key reaches the right editor is to press it.
//
// Ctrl+N, because it is the Gui Editor's alone - the Asset Manager has no Ctrl+N
// at all, so "nothing happened" is the whole answer. What it does is observable
// without a dialog: New Gui throws the document's file path away.
//
// Both rounds matter. Without the second, "nothing happened" in the first would
// be equally well explained by the key never arriving.
//-----------------------------------------------------------------------------

$mswSentinel = "notARealFile.gui.taml";

function mswTargetFile()
{
	return testRoot("shots/menuSwapTarget.txt");
}

function mswAskForKey(%round)
{
	%file = new FileObject();
	if(!%file.openForWrite(mswTargetFile()))
	{
		%file.delete();
		return false;
	}
	%file.writeLine(%round);
	%file.close();
	%file.delete();

	return true;
}

function mswStep13()
{
	createPath(testRoot("shots/"));

	// Something for New Gui to visibly throw away.
	GuiEditor.filePath = $mswSentinel;

	// Register the Gui Editor's shortcuts the way ordinary use does, so the swap
	// that follows has something real to take away again.
	//
	// A dialog going up and coming down is what rebuilds the canvas's accelerator
	// list, and opening any menu does exactly this - the bar pushes a full-canvas
	// background at layer 99 while a dropdown is showing and pops it on close. So
	// a user who opens one menu and then changes tab has left the outgoing
	// editor's shortcuts in the list. Without them being taken out, Ctrl+N below
	// would still reach the Gui Editor from inside the Asset Manager, which is the
	// bug this whole arrangement exists to prevent.
	%pushed = new GuiControl() { Position = "0 0"; Extent = "1 1"; };
	Canvas.pushDialog(%pushed);
	Canvas.popDialog(%pushed);
	%pushed.delete();

	mswCheck("a dialog round trip left the Gui Editor's shortcuts registered ("
		@ Canvas.getAcceleratorCount() @ ")",
		Canvas.getAcceleratorCount() == $mswAcceleratorsGui);

	EditorCore.tabBook.selectPage($mswAssets);
	schedule(600, 0, "mswStep14");
}

function mswStep14()
{
	mswCheck("the Asset Manager has the bar for the key test",
		EditorCore.activeMenus == AssetAdmin.menus);
	mswCheck("and the Gui Editor's shortcuts left with its menus ("
		@ Canvas.getAcceleratorCount() @ ")",
		Canvas.getAcceleratorCount() < $mswAcceleratorsGui);

	mswCheck("asked for round 1 (Ctrl+N with the Asset Manager open)", mswAskForKey(1));
	schedule(4000, 0, "mswStep15");
}

function mswStep15()
{
	// The Gui Editor's New Gui is parked. Its shortcut must have gone with it.
	mswCheck("Ctrl+N did not reach the parked Gui Editor (" @ GuiEditor.filePath @ ")",
		GuiEditor.filePath $= $mswSentinel);

	EditorCore.tabBook.selectPage($mswGui);
	schedule(600, 0, "mswStep16");
}

function mswStep16()
{
	mswCheck("the Gui Editor has the bar again",
		EditorCore.activeMenus == GuiEditor.menus);
	mswCheck("and its document still has the sentinel to lose",
		GuiEditor.filePath $= $mswSentinel);

	mswCheck("asked for round 2 (Ctrl+N with the Gui Editor open)", mswAskForKey(2));
	schedule(4000, 0, "mswStep17");
}

function mswStep17()
{
	// The control. If this fails the first round proved nothing - it would only
	// have shown that no key arrived at all.
	mswCheck("Ctrl+N reached the open Gui Editor and made a new document",
		GuiEditor.filePath $= "");

	echo("MSW DONE");
	schedule(300, 0, "quit");
}

// 1 if every item in the group is active, 0 if none is, -1 if they disagree --
// which would mean the group had stopped being one thing.
function mswGroupAllActive(%menus, %group)
{
	%count = %menus.groupCount[%group];
	%active = 0;
	for(%i = 0; %i < %count; %i++)
	{
		if(%menus.groupItem[%group, %i].Active)
		{
			%active++;
		}
	}

	if(%active == 0)      return 0;
	if(%active == %count) return 1;
	return -1;
}
