// Visual harness for the tab book's editor-only "+" tab.
//
// Two books in every shot, because the two cases fail differently:
//   populated   three pages, so the "+" has to land after the last real tab
//   empty       no pages at all -- which before this drew NOTHING, tab strip
//               included, because calculatePageTabs short-circuited and left
//               mTabRect zero for onRender to bail on
//
// All four tab positions, not just the two obvious ones: a bottom or right strip
// places itself by measuring back from the far edge, and used to read its own
// extent before this pass had written it. A book with pages hid that behind a
// second layout pass; a book with none gets only the first.
//
// Run: tests/run.ps1 -Shots tabBook ; look in shots/.

setLogMode(1);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

testExec("editor/main.cs");
schedule(2500, 0, "tbOpenProject");

// A real project, loaded the way the project selector does it, so the books are
// drawn wearing a project's theme rather than the engine's fallbacks.
function tbOpenProject()
{
	ProjectManager.setProjectFolder("PlanetX");
	EditorCore.projectSelector.onProjectSelected(pathConcat(getMainDotCsDir(), "PlanetX"));
	schedule(2500, 0, "tbOpenEditor");
}

// Loading AppCore starts the project's game over the canvas; the editor comes
// back the way Ctrl+~ does it. Pages register in load order: EditorConsole,
// ProjectManager, AssetAdmin, GuiEditor.
function tbOpenEditor()
{
	EditorCore.toggleEditor();
	EditorCore.tabBook.selectPage(3);
	schedule(1500, 0, "tbStep1");
}

function tbStep1()
{
	$tbTheme = GuiEditor.themeLibrary.createTheme("TabBookShotTheme");
	GuiEditor.themeName = $tbTheme.getName();

	// Where the canvas can actually show something. A Gui is authored at its own
	// size and the editor's frame is a few hundred pixels wide, so the middle of
	// the root is off screen more often than not; the brain already works out
	// which part of a container is visible for click placement.
	%room = GuiEditor.brain.visiblePartOf(GuiEditor.rootGui);
	%left = getWord(%room, 0) + 20;
	%top = getWord(%room, 1) + 20;

	$tbBook = tbPlace(%left, %top);
	for(%i = 1; %i <= 3; %i++)
	{
		%page = new GuiTabPageCtrl() { Text = "Page " @ %i; };
		$tbBook.add(%page);
		GuiEditor.themeApplier.applyToBranch(%page, $tbTheme, true);
	}

	$tbEmpty = tbPlace(%left, %top + 160);

	createPath(testRoot("shots/"));

	$tbCases = "Top" TAB "Bottom" TAB "Left" TAB "Right";
	$tbIndex = 0;
	schedule(800, 0, "tbShoot");
}

function tbPlace(%x, %y)
{
	%book = new GuiTabBookCtrl() { Extent = "300 130"; };
	GuiEditor.rootGui.add(%book);
	%book.setPositionGlobal(%x, %y);
	GuiEditor.themeApplier.applyToBranch(%book, $tbTheme, true);
	return %book;
}

function tbShoot()
{
	%pos = getField($tbCases, $tbIndex);
	$tbBook.TabPosition = %pos;
	$tbEmpty.TabPosition = %pos;

	// solveDirty notices the changed field on the next onPreRender and resizes,
	// which is what re-runs calculatePageTabs. Nothing to call by hand.
	schedule(600, 0, "tbGrab", %pos);
}

function tbGrab(%pos)
{
	screenShot(testRoot("shots/tabbook-" @ %pos @ ".png"), "PNG");
	echo("SHOT: wrote tabbook-" @ %pos @ ".png");

	$tbIndex++;
	if($tbIndex < getFieldCount($tbCases))
	{
		schedule(300, 0, "tbShoot");
		return;
	}

	echo("SHOT DONE");
	schedule(300, 0, "quit");
}
