//-----------------------------------------------------------------------------
// GuiFrameSetCtrl persistence.
//
// A frame set is the one container that keeps a layout of its own beside its
// child list, and none of it is a persist field: the split tree is written as
// TAML custom nodes and nothing else. So it is the one control whose file can
// look complete and load back as something else entirely - every frame merged
// into one, with the children piled into it - and until now nothing tested that.
//
// The failure it is shaped around: TAML matches custom-node field names by
// StringTable pointer, so a name that stops matching takes every value in a
// frame node with it - loadTamlFrame gives up on a frame of extent zero and the
// tree comes back flat, with nothing but a warnf to say so. That is what the
// value-count checks below are for.
//
// It cannot be provoked on demand. Whether a name matches depends on which
// spelling of it reached the string table first, which depends on static
// initialisation order across translation units - so the same source can pass
// one build and fail the next. GuiListBoxCtrl's Items nodes did exactly that;
// the frame set has so far been lucky. Both now intern case-insensitively, which
// is what removes the dependence. This test does not prove that fix on any given
// build; it is here because frame-set persistence had no coverage at all, and
// this is the shape a broken one takes.
//
// Run: tests/run.ps1 frameSet ; grep FRAMESET in console.log.
//-----------------------------------------------------------------------------

setLogMode(2);
setScriptExecEcho(false);
trace(false);
$Scripts::ignoreDSOs = true;
setCompanyAndProduct("Torque Game Engines", "Torque2D");
ModuleDatabase.EchoInfo = false;
AssetDatabase.EchoInfo = false;
AssetDatabase.IgnoreAutoUnload = true;

function fsCheck(%label, %condition)
{
	echo(%condition ? ("FRAMESET PASS: " @ %label) : ("FRAMESET FAIL: " @ %label));
}

function fsScratch()
{
	return testRoot("shots/frameSetScratch");
}

function fsReadFile(%file)
{
	%fo = new FileObject();
	if(!%fo.openForRead(%file))
	{
		%fo.delete();
		return "";
	}

	%text = "";
	while(!%fo.isEOF())
	{
		%text = %text @ %fo.readLine() @ " ";
	}
	%fo.close();
	%fo.delete();

	return %text;
}

// A frame layout with the control ids taken out. Every eighth value is the id of
// the control standing in that frame, which is a different number in a Gui that
// has been read back off disk - so it is the only part of the layout that cannot
// be compared, and the only part that says nothing about the tree's shape.
function fsShape(%layout)
{
	%out = "";
	%count = getWordCount(%layout);
	for(%i = 0; %i < %count; %i++)
	{
		if((%i % 8) == 7)
		{
			continue;
		}
		%out = (%out $= "") ? getWord(%layout, %i) : (%out SPC getWord(%layout, %i));
	}

	return %out;
}

testExec("editor/main.cs");
schedule(2000, 0, "fsStep1");

function fsStep1()
{
	createPath(fsScratch() @ "/");

	%frames = new GuiFrameSetCtrl()
	{
		Extent = "400 200";
	};

	// One split, and a control in each half. A frame takes the control as it
	// arrives (onChildAdded fills an empty frame; it never makes one), so the
	// split has to come first.
	%ids = %frames.createHorizontalSplit(1);
	%frames.setFrameSize(getWord(%ids, 0), 150);

	%left = new GuiButtonCtrl() { Text = "Left"; };
	%frames.add(%left);
	%right = new GuiButtonCtrl() { Text = "Right"; };
	%frames.add(%right);

	%before = %frames.getFrameLayout();
	fsCheck("a split frame set is three frames (" @ getWordCount(%before) @ " values)",
		getWordCount(%before) == 24);
	fsCheck("both controls are in it", %frames.getCount() == 2);

	%file = pathConcat(fsScratch(), "frames.gui.taml");
	TAMLWrite(%frames, %file);

	// Lower-cased, because which capitalisation an attribute is written in is not
	// the writer's to decide - StringTable hands back the first spelling of a name
	// it was ever given.
	%text = strlwr(fsReadFile(%file));
	fsCheck("the file carries a Frames section", strstr(%text, "guiframesetctrl.frames") != -1);
	fsCheck("with frame nodes in it", strstr(%text, "<frame") != -1);
	fsCheck("naming which control stands where", strstr(%text, "childmap=") != -1);
	fsCheck("and which way the split runs", strstr(%text, "isvertical=") != -1);

	%read = TAMLRead(%file);
	fsCheck("the file reads back as a frame set",
		isObject(%read) && %read.getClassName() $= "GuiFrameSetCtrl");
	fsCheck("both children came back", isObject(%read) && %read.getCount() == 2);

	// The shape of the failure named at the top: with the field names not
	// matching, every value in a frame node is dropped, loadTamlFrame gives up on
	// a frame of extent zero, and what comes back is a single frame - eight
	// values instead of twenty-four, with both children piled into it.
	%after = isObject(%read) ? %read.getFrameLayout() : "";
	fsCheck("the split survived the file (" @ getWordCount(%after) @ " values)",
		getWordCount(%after) == 24);
	fsCheck("the whole tree round trips", fsShape(%after) $= fsShape(%before));

	// The sizes are part of the tree, and they are what a dropped field loses
	// most quietly: a frame of the wrong width still draws.
	fsCheck("the frame sizes came with it",
		getWord(%after, 4) == getWord(%before, 4) && getWord(%after, 5) == getWord(%before, 5));

	if(isObject(%read))
	{
		%read.delete();
	}

	// A deep clone takes the same route, because the tree is not a field and not
	// a child either (GuiFrameSetCtrl::deepCloneChildren).
	%clone = %frames.deepClone();
	fsCheck("a deep clone carries the tree",
		isObject(%clone) && fsShape(%clone.getFrameLayout()) $= fsShape(%before));
	if(isObject(%clone))
	{
		%clone.delete();
	}

	%frames.delete();

	schedule(100, 0, "fsDone");
}

function fsDone()
{
	echo("FRAMESET DONE");
	quit();
}
