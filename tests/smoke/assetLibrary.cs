// Asset Library smoke test. Drives the Asset Manager's right-hand library
// through script: the pinned toolbar, the tiles/rows view switch, the live
// search across all five groups (name, description and category), the per-group
// header counts, sorting by name and by category, and the preference round trip.
// Run: tests/run.ps1 assetLibrary  ; grep ALIB in tests/logs/.
//
// Driven by calling the library rather than by posting input. A tile's position
// on screen depends on the scroll offset, which groups are open and how many
// columns the grid chose, none of which script can read -- so a click at a
// computed point would be testing the arithmetic in this file.
//
// NOTE: EditorPreferences writes to the tester's real per-user application data
// folder, which nothing in tests/run.ps1 cleans up. Step 1 redirects it into
// shots/ for the duration so a test run cannot change how the editor opens
// afterwards. It cannot redirect the READ -- the library is built during
// AssetAdmin::create, long before this file gets a turn -- which is why the
// order checks pin the sort field themselves rather than assume one.
//
// NOTE: the fixture copy this makes is about 30 MB and is left behind; there is
// no deleteDirectory binding in script to tidy it. tests/run.ps1 deletes every
// *SmokeProject folder before each run, so it never accumulates.

setLogMode(1);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

function alCheck(%label, %cond)
{
	if(%cond) echo("ALIB PASS: " @ %label);
	else      echo("ALIB FAIL: " @ %label);
}

// A bare project has no assets but the editor's own, which are all marked
// AssetInternal and rightly skipped. ToyAssets is nothing but assets -- no
// ScriptFile, no CreateFunction -- so registering what it declares gives the
// library real images and animations without running a line of toy code.
//
// A COPY of it, though, never the module itself. Step 5 edits an asset's
// description and category, and AssetManager::refreshAsset writes the asset
// straight back to its own file the moment a field changes (assetManager.cc) --
// so aimed at toybox/ToyAssets this test rewrites tracked repository content,
// and rewrites it in Taml's own idiom rather than the way it was authored. The
// copy goes inside the throwaway project folder, which tests/run.ps1 deletes
// before every run.
function alLoadFixtureAssets()
{
	%copy = testRoot("assetLibrarySmokeProject/ToyAssets");

	if(!pathCopy(testRoot("toybox/ToyAssets"), %copy, false))
	{
		return false;
	}

	ModuleDatabase.scanModules(%copy);
	%module = ModuleDatabase.findModule("ToyAssets", 1);
	if(!isObject(%module))
	{
		return false;
	}
	AssetDatabase.addModuleDeclaredAssets(%module);
	return true;
}

function alVisibleCount(%dictionary)
{
	%shown = 0;
	for(%i = 0; %i < %dictionary.grid.getCount(); %i++)
	{
		if(%dictionary.grid.getObject(%i).isVisible())
		{
			%shown++;
		}
	}
	return %shown;
}

// Deliberately does not call AssetDictionary::sortsAfter -- comparing the order
// with the function that produced it would agree with any bug in it.
//
// Names the pair it tripped on. "the list is not sorted" sends you back to the
// engine with 90 tiles to read; "brick_05 before brick_04 at 7" does not.
// Run the check, then build the label from what it found -- the label cannot
// read $alSortFailure in the same expression that sets it.
function alSortedCheck(%label, %dictionary, %field)
{
	%ok = alSortedOk(%dictionary, %field);
	alCheck(%label SPC $alSortFailure, %ok);

	if(!%ok)
	{
		%count = %dictionary.grid.getCount();
		%start = %count - 6;
		if(%start < 0)
		{
			%start = 0;
		}
		for(%i = %start; %i < %count; %i++)
		{
			%tile = %dictionary.grid.getObject(%i);
			echo("ALIB TAIL " @ %i @ ": name='" @ %tile.assetName @
				"' sortName='" @ %tile.sortName @ "' key='" @ %tile.searchKey @ "'");
		}
	}
}

function alSortedOk(%dictionary, %field)
{
	$alSortFailure = "";

	for(%i = 1; %i < %dictionary.grid.getCount(); %i++)
	{
		%prev = %dictionary.grid.getObject(%i - 1);
		%next = %dictionary.grid.getObject(%i);
		%wrong = false;

		if(%field $= "category")
		{
			%order = stricmp(%prev.sortCategory, %next.sortCategory);
			%wrong = (%order > 0) ||
				(%order == 0 && stricmp(%prev.sortName, %next.sortName) > 0);
		}
		else
		{
			%wrong = stricmp(%prev.sortName, %next.sortName) > 0;
		}

		if(%wrong)
		{
			$alSortFailure = "at" SPC %i @ ":" SPC %prev.assetName SPC "[" @
				%prev.sortCategory @ "] before" SPC %next.assetName SPC "[" @
				%next.sortCategory @ "]";
			return false;
		}
	}
	return true;
}

// Every asset id in the group, so a reorder that loses or duplicates one is
// caught. A monotonic list can still be the wrong list.
function alAssetIdSet(%dictionary)
{
	%ids = "";
	for(%i = 0; %i < %dictionary.grid.getCount(); %i++)
	{
		%ids = %ids TAB %dictionary.grid.getObject(%i).assetID;
	}
	return %ids;
}

function alHoldsEvery(%dictionary, %ids)
{
	for(%i = 0; %i < getFieldCount(%ids); %i++)
	{
		%id = getField(%ids, %i);
		if(%id $= "")
		{
			continue;
		}
		if(!isObject(%dictionary.getButton(%id)))
		{
			return false;
		}
	}
	return true;
}

function alSearch(%needle)
{
	$alWindow.searchBox.setText(%needle);
	$alWindow.applyFilter();
}

testExec("editor/main.cs");
schedule(2000, 0, "alStep1");

//-----------------------------------------------------------------------------
// Opening the library.
//-----------------------------------------------------------------------------

function alStep1()
{
	createPath(testRoot("shots/"));

	// Spelled out rather than held in a variable, here and in the copy path
	// above: tests/run.ps1 finds the folder to delete by reading this file for
	// setProjectFolder("..."), so a name it cannot see is a folder it cannot
	// sweep.
	ProjectManager.setProjectFolder("assetLibrarySmokeProject");

	// Before anything can toggle a view and write one.
	EditorPreferences.path = testRoot("shots/assetLibrarySmokePrefs.taml");

	alCheck("fixture asset module registered", alLoadFixtureAssets());

	// Pages register in load order: EditorConsole, ProjectManager, AssetAdmin,
	// GuiEditor. Selecting the tab is what calls AssetAdmin::open.
	EditorCore.tabBook.selectPage(2);

	schedule(600, 0, "alStep2");
}

//-----------------------------------------------------------------------------
// Structure.
//-----------------------------------------------------------------------------

function alStep2()
{
	$alWindow = AssetAdmin.libWindow;
	alCheck("library window exists", isObject($alWindow));
	alCheck("library window is an AssetLibraryWindow", $alWindow.class $= "AssetLibraryWindow");

	alCheck("toolbar built", isObject($alWindow.toolbar));
	alCheck("search box built", isObject($alWindow.searchBox));
	alCheck("count label built", isObject($alWindow.countLabel));
	alCheck("view row offers two choices", $alWindow.viewRow.choiceCount == 2);
	alCheck("sort row offers two choices", $alWindow.sortRow.choiceCount == 2);
	alCheck("search box fires per keystroke",
		strstr($alWindow.searchBox.Command, "onSearchChanged") != -1);

	// A glyph rather than the word "Search", which was clipped in every theme.
	alCheck("search icon built", isObject($alWindow.searchIcon));
	alCheck("search icon is the filter glyph",
		$alWindow.searchIcon.Frame == $EditorIcon::filter);
	alCheck("search box starts clear of the icon",
		getWord($alWindow.searchBox.getPosition(), 0)
			>= getWord($alWindow.searchIcon.getPosition(), 0) + $AssetLibraryWindow::iconSize);
	alCheck("search box has room left", getWord($alWindow.searchBox.getExtent(), 0) > 0);

	// The tint is a colour COPIED onto the sprite, so swapping the theme's profile
	// underneath would leave it behind. Prove the re-read happens by wrecking it.
	$alWindow.searchIcon.setImageColor("255 0 255 255");
	%wrongTint = $alWindow.searchIcon.imageColor;
	$alWindow.onThemeChange(ThemeManager.activeTheme);
	alCheck("a theme change re-tints the search icon",
		$alWindow.searchIcon.imageColor !$= %wrongTint);

	alCheck("five asset groups", $alWindow.dictionaryCount == 5);
	alCheck("the dialogs can still find a group by type",
		AssetAdmin.Dictionary["ImageAsset"] == $alWindow.dictionary[0]);

	// The whole point of the pinned bar: the scroller starts where the toolbar
	// ends, and nothing was clipped off the bottom doing it.
	%barBottom = getWord($alWindow.toolbar.getPosition(), 1)
		+ getWord($alWindow.toolbar.getExtent(), 1);
	%scrollTop = getWord($alWindow.scroller.getPosition(), 1);
	alCheck("scroller starts below the toolbar (" @ %scrollTop SPC %barBottom @ ")",
		%scrollTop == %barBottom);
	alCheck("scroller has height left", getWord($alWindow.scroller.getExtent(), 1) > 0);

	$alImages = AssetAdmin.Dictionary["ImageAsset"];
	$alAudio = AssetAdmin.Dictionary["AudioAsset"];
	$alImageCount = $alImages.getButtonCount();

	alCheck("the fixture gave the library image assets (" @ $alImageCount @ ")",
		$alImageCount > 1);

	// With an empty database every count below compares nought to nought and
	// passes without testing anything, which reads green while proving nothing.
	if($alImageCount < 2)
	{
		echo("ALIB ABORT: too few image assets, the rest of the run would prove nothing");
		schedule(300, 0, "quit");
		return;
	}

	schedule(300, 0, "alStep3");
}

//-----------------------------------------------------------------------------
// Header counts and the default order.
//-----------------------------------------------------------------------------

function alStep3()
{
	alCheck("group header carries its count",
		$alImages.getText() $= ("Images (" @ $alImageCount @ ")"));
	alCheck("group header keeps its title", strstr($alImages.getText(), "Images") == 0);

	// findAllAssets returns hash order. Sorting on load is what makes the library
	// open the same way twice.
	//
	// Pin the field and reload rather than assume the library opened on "name":
	// the sort field is a saved preference, and EditorPreferences has already
	// read the real per-user file by the time this test can redirect it -- the
	// window is built during AssetAdmin::create, long before alStep1 runs. So
	// what the library opened on is whatever the machine last chose, and a test
	// that assumed "name" passed or failed on ambient state rather than on the
	// code. reload() is unload() + load(), the same path AssetAdmin::open takes.
	$alWindow.setSortField("name");
	$alImages.reload();
	alSortedCheck("images are in name order on load", $alImages, "name");

	$alWindow.setSortField("category");
	$alImages.reload();
	alSortedCheck("images are in category order on load", $alImages, "category");

	$alWindow.setSortField("name");
	$alImages.reload();
	alCheck("every tile visible before filtering",
		alVisibleCount($alImages) == $alImageCount);

	%total = 0;
	for(%i = 0; %i < $alWindow.dictionaryCount; %i++)
	{
		%total += $alWindow.dictionary[%i].getButtonCount();
	}
	alCheck("count line totals the whole library",
		$alWindow.countLabel.getText() $= (%total @ "/" @ %total));
	$alTotal = %total;

	// Every tile has a caption and a picture in both modes.
	%tile = $alImages.grid.getObject(0);
	alCheck("a tile has a caption", isObject(%tile.caption));
	alCheck("the caption names the asset", %tile.caption.getText() $= %tile.assetName);
	alCheck("a tile has a picture", isObject(%tile.icon));
	alCheck("the search key is lowercased", %tile.searchKey $= strlwr(%tile.searchKey));
	alCheck("the search key has no stray padding", %tile.searchKey $= trim(%tile.searchKey));

	schedule(300, 0, "alStep4");
}

//-----------------------------------------------------------------------------
// Searching by name.
//-----------------------------------------------------------------------------

function alStep4()
{
	// Taken from the data rather than hardcoded, so this does not go stale when
	// the toy assets change.
	$alName = $alImages.grid.getObject(0).assetName;

	alSearch($alName);

	%shown = alVisibleCount($alImages);
	alCheck("searching a name shows at least that asset (" @ $alName @ ")", %shown >= 1);
	alCheck("searching a name hides the rest", %shown < $alImageCount);
	alCheck("header follows the filter",
		$alImages.getText() $= ("Images (" @ %shown @ ")"));

	%needle = strlwr($alName);
	%everyMatch = true;
	for(%i = 0; %i < $alImages.grid.getCount(); %i++)
	{
		%tile = $alImages.grid.getObject(%i);
		%hit = (strstr(%tile.searchKey, %needle) != -1);
		if(%tile.isVisible() != %hit)
		{
			%everyMatch = false;
		}
	}
	alCheck("exactly the matching tiles are visible", %everyMatch);

	// $= is case-insensitive but strstr is not, which is why both sides are
	// lowercased. An upper-case needle proves it.
	alSearch(strupr($alName));
	alCheck("matching ignores case", alVisibleCount($alImages) == %shown);

	// A needle nothing matches: every group keeps its header rather than
	// disappearing, so the shape of the library does not change under the person
	// typing.
	alSearch("zzqqxx");
	alCheck("no matches anywhere", alVisibleCount($alImages) == 0);
	alCheck("an emptied group stays visible", $alImages.isVisible());
	alCheck("an emptied group says so", $alImages.getText() $= "Images (0)");
	alCheck("an untouched group is emptied too", $alAudio.getText() $= "Audio (0)");
	alCheck("count line reports nothing found",
		$alWindow.countLabel.getText() $= ("0/" @ $alTotal));

	alSearch("");
	alCheck("clearing the box restores every tile",
		alVisibleCount($alImages) == $alImageCount);
	alCheck("count line restored",
		$alWindow.countLabel.getText() $= ($alTotal @ "/" @ $alTotal));

	schedule(300, 0, "alStep5");
}

//-----------------------------------------------------------------------------
// Searching by description and by category.
//
// Both are set through the asset itself, which is the production path: the
// inspector edits the same fields, AssetBase::setAssetDescription calls
// refreshAsset, and that fires onRefresh -- which is what re-keys the tile.
// refreshAsset is in-memory only, so nothing on disk is touched.
//-----------------------------------------------------------------------------

function alStep5()
{
	%tile = $alImages.grid.getObject(0);
	$alSubjectID = %tile.assetID;

	%asset = AssetDatabase.acquireAsset($alSubjectID);
	%asset.AssetDescription = "zzdescription";
	%asset.AssetCategory = "zzcategory";

	alCheck("the tile re-read its description",
		strstr(%tile.searchKey, "zzdescription") != -1);
	alCheck("the tile re-read its category", %tile.sortCategory $= "zzcategory");

	alSearch("zzdescription");
	alCheck("a description-only match is found", alVisibleCount($alImages) == 1);
	alCheck("and it is the right one", $alImages.grid.getObject(0).isVisible()
		|| $alImages.getButton($alSubjectID).isVisible());

	alSearch("zzcateg");
	alCheck("a partial category match is found", alVisibleCount($alImages) == 1);

	alSearch("");
	AssetDatabase.releaseAsset($alSubjectID);

	schedule(300, 0, "alStep6");
}

//-----------------------------------------------------------------------------
// Sorting.
//-----------------------------------------------------------------------------

function alStep6()
{
	$alIds = alAssetIdSet($alImages);

	$alWindow.setSortField("category");
	alCheck("sort field taken", $alWindow.sortField $= "category");
	alSortedCheck("images are in category order", $alImages, "category");
	alCheck("category sort kept every tile",
		$alImages.grid.getCount() == $alImageCount);
	alCheck("category sort lost none of them", alHoldsEvery($alImages, $alIds));
	// The fixture's own categories are all "sprites", and step 5 gave one asset
	// "zzcategory" -- so category order has to end with that one, and a sort that
	// quietly ignored its field would leave it wherever the name order put it.
	alCheck("the highest category sorts to the end",
		$alImages.grid.getObject($alImageCount - 1).assetID $= $alSubjectID);

	$alWindow.setSortField("name");
	alSortedCheck("images are back in name order", $alImages, "name");
	alCheck("name sort kept every tile", $alImages.grid.getCount() == $alImageCount);
	alCheck("name sort lost none of them", alHoldsEvery($alImages, $alIds));

	// Order has to survive a filter: the hidden tiles are reordered too, so
	// clearing the box must not reveal an unsorted list.
	alSearch($alName);
	$alWindow.setSortField("category");
	alSearch("");
	alSortedCheck("sorting while filtered still sorted the hidden tiles",
		$alImages, "category");
	$alWindow.setSortField("name");

	schedule(300, 0, "alStep7");
}

//-----------------------------------------------------------------------------
// View mode.
//-----------------------------------------------------------------------------

function alStep7()
{
	%tile = $alImages.grid.getObject(0);

	alCheck("starts in grid mode", $alWindow.viewMode $= "grid");
	alCheck("grid tile draws the art at tile size",
		getWord(%tile.icon.getExtent(), 0) == $AssetDictionaryButton::gridArt);
	alCheck("grid caption is centred", %tile.caption.align $= "center");
	alCheck("grid caption sits under the art", %tile.caption.vAlign $= "bottom");
	alCheck("grid caption wraps", %tile.caption.textWrap);

	$alWindow.setViewMode("rows");

	alCheck("mode taken", $alWindow.viewMode $= "rows");
	alCheck("groups were told", $alImages.viewMode $= "rows");
	alCheck("row tile draws the art small",
		getWord(%tile.icon.getExtent(), 0) == $AssetDictionaryButton::rowArt);
	alCheck("row caption is left aligned", %tile.caption.align $= "left");
	alCheck("row caption is centred vertically", %tile.caption.vAlign $= "middle");
	alCheck("row caption does not wrap", !%tile.caption.textWrap);
	alCheck("row art is left of the caption",
		getWord(%tile.icon.getPosition(), 0) < getWord(%tile.caption.getPosition(), 0));

	// One column: every tile shares an x, and the rows are the row height.
	%x = getWord($alImages.grid.getObject(0).getPosition(), 0);
	%oneColumn = true;
	for(%i = 0; %i < $alImages.grid.getCount(); %i++)
	{
		if(getWord($alImages.grid.getObject(%i).getPosition(), 0) != %x)
		{
			%oneColumn = false;
		}
	}
	alCheck("rows mode is one column", %oneColumn);
	alCheck("rows mode uses the row height",
		getWord(%tile.getExtent(), 1) == $AssetDictionary::rowHeight);
	alCheck("a row is wider than a tile",
		getWord(%tile.getExtent(), 0) > $AssetDictionary::gridCell);

	// The picture is the same picture -- nothing was rebuilt.
	alCheck("the same tile object survived the switch",
		$alImages.grid.getObject(0).assetID $= %tile.assetID);

	// Filtering still works in rows mode.
	alSearch("zzqqxx");
	alCheck("rows mode filters too", alVisibleCount($alImages) == 0);
	alSearch("");

	$alWindow.setViewMode("grid");
	alCheck("back to grid art size",
		getWord(%tile.icon.getExtent(), 0) == $AssetDictionaryButton::gridArt);

	schedule(300, 0, "alStep8");
}

//-----------------------------------------------------------------------------
// Preferences.
//-----------------------------------------------------------------------------

function alStep8()
{
	$alWindow.setViewMode("rows");
	$alWindow.setSortField("category");

	alCheck("view mode remembered",
		EditorPreferences.get("assetLibraryViewMode", "grid") $= "rows");
	alCheck("sort field remembered",
		EditorPreferences.get("assetLibrarySortField", "name") $= "category");

	// The file is the point: an in-memory field would survive nothing.
	alCheck("a preferences file was written",
		EditorPreferences.fileExists(EditorPreferences.path));

	// And what is in it is what the next session would read.
	%stored = TamlRead(EditorPreferences.path);
	alCheck("the file parses back", isObject(%stored));
	alCheck("view mode is in the file", %stored.assetLibraryViewMode $= "rows");
	alCheck("sort field is in the file", %stored.assetLibrarySortField $= "category");

	// Saved as a plain ScriptObject on purpose: writing the preferences object
	// itself would record class="EditorPreferences", and reading it back would
	// build a second one that loads the file again.
	alCheck("the file does not rebuild a preferences object",
		%stored.class $= "");
	%stored.delete();

	$alWindow.setViewMode("grid");
	$alWindow.setSortField("name");

	echo("ALIB DONE");
	schedule(300, 0, "quit");
}
