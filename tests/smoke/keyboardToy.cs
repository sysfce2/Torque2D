//-----------------------------------------------------------------------------
// The KeyboardToy comes up, and the VirtualKeyboard it exists to show comes up
// with it.
//
// The toy had rotted quietly: it called reset() on a bare "KeyboardToy" name
// that resolves to nothing, so the dialog was never pushed and the toy opened
// to an empty screen. Its label was a GuiTextCtrl, deleted from the engine in
// 2021, and two of the profiles it named went with AppCore's. None of that is
// visible until something loads the toy, which is what this does.
//-----------------------------------------------------------------------------

setRandomSeed();
setLogMode(2);
setScriptExecEcho(false);
trace(false);
$Scripts::ignoreDSOs = true;
setCompanyAndProduct("Torque Game Engines", "Torque2D");
ModuleDatabase.EchoInfo = false;
AssetDatabase.EchoInfo = false;

ModuleDatabase.scanModules(testRoot("toybox"));
ModuleDatabase.LoadExplicit("AppCore");

function smokeCheck(%label, %condition)
{
	echo(%condition ? ("SMOKE PASS: " @ %label) : ("SMOKE FAIL: " @ %label));
}

createPath(testRoot("shots/"));
schedule(5000, 0, "kbStepLoad");

function kbStepLoad()
{
	%toy = ModuleDatabase.findModule("KeyboardToy", 1);
	smokeCheck("the KeyboardToy module is there", isObject(%toy));

	loadToy(%toy);
	schedule(1000, 0, "kbStepDialog");
}

function kbStepDialog()
{
	// create() makes both dialogs and reset() pushes the first. Before the fix
	// reset() was never reached, so this is the check that matters.
	smokeCheck("the toy made its dialog", isObject(MainGameDlg));
	smokeCheck("the toy made its second dialog", isObject(ChangeUsernameDlg));
	smokeCheck("the dialog was pushed to the canvas", isObject(MainGameDlg) && MainGameDlg.isAwake());

	// The label was a GuiTextCtrl; a plain GuiControl carries text now.
	smokeCheck("the label survived losing GuiTextCtrl", isObject(UserNameTxt));
	smokeCheck("the label kept its text", UserNameTxt.getText() $= "NONAME");

	// Every profile the two dialogs name must actually exist, or the controls
	// silently fall back and the toy looks wrong rather than failing.
	smokeCheck("the dialog profile exists", isObject(GuiSpriteProfile));
	smokeCheck("the label profile exists", isObject(GuiTextProfile));
	smokeCheck("the entry profile exists", isObject(GuiTextEditProfile));
	smokeCheck("the button profile exists", isObject(BlueButtonProfile));

	schedule(500, 0, "kbStepKeyboard");
}

function kbStepKeyboard()
{
	// What the toy is for: the keyboard, raised the way its button raises it.
	VirtualKeyboard.push(ChangeUsernameDlg, ChangeUsernameEntry);
	schedule(1000, 0, "kbStepKeys");
}

function kbStepKeys()
{
	smokeCheck("the keyboard came up", isObject(KeyboardGui));
	smokeCheck("its keys are in the tree", isObject(KeyboardSet));

	// The four state strips that replaced GuiImageButtonCtrl. A profile's
	// imageAsset is indexed by control state, so one strip is a whole button.
	smokeCheck("the key profile exists", isObject(GuiKeyboardKeyProfile));
	smokeCheck("the space bar profile exists", isObject(GuiKeyboardSpaceProfile));
	smokeCheck("the close profile exists", isObject(GuiKeyboardCloseProfile));
	smokeCheck("the caps lock profile exists", isObject(GuiKeyboardLatchedProfile));

	screenShot(testRoot("shots/keyboardToy.png"), "PNG");
	schedule(1000, 0, "kbSmokeDone");
}

function kbSmokeDone()
{
	echo("SMOKE DONE");
	quit();
}
