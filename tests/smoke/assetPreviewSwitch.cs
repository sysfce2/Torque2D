// Switching the library's selection from one asset kind to another must leave the
// preview showing the asset that was chosen, and nothing else.
// Run: tests/run.ps1 assetPreviewSwitch  ; grep APSW in tests/logs/.
//
// The case this exists for is image -> animation. Choosing an animation builds the
// three-way split, which resizes the SceneWindow, whose onExtentChange answers a
// resize by re-clicking the selected tile -- and the selected tile is not updated
// until the END of onClick, so the tile it re-clicks is the PREVIOUS one. That
// re-click cleared the preview scene and put the old asset back in it, on top of
// the animation sprite that had just been made.
//
// Every direction is checked rather than just that one, because the fix is in the
// resize path that all of them go through.
//
// NOTE: a COPY of toybox/ToyAssets, because selecting assets can write to them.

setLogMode(1);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

function pswCheck(%label, %cond)
{
	if(%cond) echo("APSW PASS: " @ %label);
	else      echo("APSW FAIL: " @ %label);
}

$pswAnimId  = "ToyAssets:TD_Barbarian_Death";
$pswImageId = "ToyAssets:TD_Barbarian_CompSprite";
$pswFontId  = "ToyAssets:ArialFont";

function pswLoadFixtureAssets()
{
	%copy = testRoot("assetPreviewSwitchSmokeProject/ToyAssets");

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

// What the preview scene actually holds, as a line that can be read in the log.
//
// The first few only. An image preview is one sprite per cell, and the sheet this
// suite uses has a hundred of them -- a line naming them all overran the console's
// own buffer, which a debug build reports as a modal assert and a hung test.
function pswSceneReport()
{
	%scene = AssetAdmin.AssetScene;
	%count = %scene.getCount();
	%shown = mGetMin(%count, 3);
	%out = %count @ " object(s):";

	for(%i = 0; %i < %shown; %i++)
	{
		%object = %scene.getObject(%i);
		%out = %out SPC %object.getClassName();

		if(%object.isMemberOfClass("SpriteBase"))
		{
			%out = %out @ "(" @ (%object.isStaticFrameProvider() ? %object.getImage() : %object.getAnimation()) @ ")";
		}
	}

	if(%count > %shown)
	{
		%out = %out SPC "...";
	}

	return %out;
}

function pswTile(%kind, %assetId)
{
	return AssetAdmin.Dictionary[%kind].getButton(%assetId);
}

function pswSameSize(%a, %b)
{
	return mAbs(getWord(%a, 0) - getWord(%b, 0)) < 0.01 &&
		mAbs(getWord(%a, 1) - getWord(%b, 1)) < 0.01;
}

testExec("editor/main.cs");
schedule(2000, 0, "pswStep1");

//-----------------------------------------------------------------------------

function pswStep1()
{
	createPath(testRoot("shots/"));

	// Spelled out rather than held in a variable: tests/run.ps1 finds the folder
	// to delete by reading this file for setProjectFolder("...").
	ProjectManager.setProjectFolder("assetPreviewSwitchSmokeProject");
	EditorPreferences.path = testRoot("shots/assetPreviewSwitchSmokePrefs.taml");

	pswCheck("fixture asset module registered", pswLoadFixtureAssets());

	EditorCore.tabBook.selectPage(2);

	schedule(600, 0, "pswStep2");
}

//-----------------------------------------------------------------------------
// Image first, so that the animation is chosen with a tile already selected --
// which is the whole point. Chosen as the first tile of the session, an animation
// has always worked, because there is no previous tile for the resize to re-click.
//-----------------------------------------------------------------------------

function pswStep2()
{
	$pswImageTile = pswTile("ImageAsset", $pswImageId);
	$pswAnimTile  = pswTile("AnimationAsset", $pswAnimId);
	$pswFontTile  = pswTile("FontAsset", $pswFontId);

	pswCheck("the image tile is in the library", isObject($pswImageTile));
	pswCheck("the animation tile is in the library", isObject($pswAnimTile));
	pswCheck("the font tile is in the library", isObject($pswFontTile));

	$pswImageTile.onClick();

	schedule(600, 0, "pswStep3");
}

function pswStep3()
{
	pswCheck("the image is previewed (" @ pswSceneReport() @ ")",
		AssetAdmin.AssetScene.getCount() > 0);
	pswCheck("the animation stage is down for an image", !AssetAdmin.animationStage.built);

	$pswAnimTile.onClick();

	schedule(900, 0, "pswStep4");
}

//-----------------------------------------------------------------------------
// The bug.
//-----------------------------------------------------------------------------

function pswStep4()
{
	%stage = AssetAdmin.animationStage;

	pswCheck("the stage is built for an animation", %stage.built);

	// The preview holds the animation and only the animation. An image preview of
	// a multi-frame sheet is one sprite per cell, so a count of 1 is already most
	// of the statement, and the animation id is the rest of it.
	pswCheck("the preview holds one object (" @ pswSceneReport() @ ")",
		AssetAdmin.AssetScene.getCount() == 1);
	pswCheck("and it is a sprite playing the animation",
		AssetAdmin.AssetScene.getObject(0).getAnimation() $= $pswAnimId);

	// And the stage is following the sprite that is actually in the scene, rather
	// than one that was cleared out from under it.
	pswCheck("the preview sprite is live", isObject(AssetAdmin.previewSprite));
	pswCheck("the stage adopted the sprite in the scene",
		%stage.previewSprite == AssetAdmin.AssetScene.getObject(0));

	// And it is sized for the preview it actually ended up in. The sprite is made
	// before the split exists, measured against a preview area that the split is
	// about to take a palette and a timeline out of -- so a sprite nobody re-sized
	// after the build is one that overflows the frame holding it.
	pswCheck("the sprite is sized to the split preview (" @ %stage.previewSprite.getSize() @ ")",
		pswSameSize(%stage.previewSprite.getSize(),
			AssetAdmin.assetWindow.getWorldSize(%stage.imageAsset.getFrameSize(0))));

	schedule(600, 0, "pswStep5");
}

//-----------------------------------------------------------------------------
// Back out again, and in from a third kind.
//-----------------------------------------------------------------------------

function pswStep5()
{
	$pswFontTile.onClick();

	schedule(900, 0, "pswStep6");
}

function pswStep6()
{
	pswCheck("the stage came down for the font", !AssetAdmin.animationStage.built);
	pswCheck("the font is previewed alone (" @ pswSceneReport() @ ")",
		AssetAdmin.AssetScene.getCount() == 1);
	pswCheck("and it is the text sprite",
		AssetAdmin.AssetScene.getObject(0).getClassName() $= "TextSprite");

	$pswAnimTile.onClick();

	schedule(900, 0, "pswStep7");
}

function pswStep7()
{
	pswCheck("the stage is built coming from a font", AssetAdmin.animationStage.built);
	pswCheck("the preview holds one object (" @ pswSceneReport() @ ")",
		AssetAdmin.AssetScene.getCount() == 1);
	pswCheck("and it is a sprite playing the animation",
		AssetAdmin.AssetScene.getObject(0).getAnimation() $= $pswAnimId);

	// Selecting the same animation again is the path that has always worked -- the
	// split is already up, so nothing is resized and nothing re-clicks. Checked so
	// that a fix for the other direction cannot quietly break it.
	$pswAnimTile.onClick();

	schedule(600, 0, "pswStep8");
}

function pswStep8()
{
	pswCheck("re-choosing the same animation keeps it (" @ pswSceneReport() @ ")",
		AssetAdmin.AssetScene.getCount() == 1 &&
		AssetAdmin.AssetScene.getObject(0).getAnimation() $= $pswAnimId);
	pswCheck("and the stage is still following it",
		AssetAdmin.animationStage.previewSprite == AssetAdmin.AssetScene.getObject(0));

	echo("APSW DONE");
	schedule(200, 0, "quit");
}
