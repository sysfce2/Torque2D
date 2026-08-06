//-----------------------------------------------------------------------------
// A caption the author cleared has to come back cleared.
//
// SimObject::writeField drops every empty value, so a blank caption is written
// as an absent one. Anything the constructor seeds therefore stands back up on
// read and silently replaces the author's blank - which is what put "Button" on
// all thirty seven keys of the VirtualKeyboard the first time it went through
// the Gui Editor. A button carries no caption of its own now; the Gui Editor
// captions the ones it places.
//
// This is a smoke suite rather than a C++ unit test because building a button
// assigns it a profile, and a profile loads its font, which registers a texture
// - and a unit test has no GL context, so that asserts.
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
schedule(4000, 0, "btDefaults");

function btDefaults()
{
	// What a freshly built control arrives with. Every one of these is checked
	// for existence first: getText() on something that was never made answers
	// the empty string too, so "has no caption" would pass on a control that
	// does not exist.
	%button = new GuiButtonCtrl();
	smokeCheck("a button was made", isObject(%button));
	smokeCheck("a new button has no caption (" @ %button.getText() @ ")",
		%button.getText() $= "");
	%button.delete();

	// A check box and a radio take their text from GuiButtonCtrl, so the seeded
	// caption used to reach them too -- a check box read "Button".
	%check = new GuiCheckBoxCtrl();
	smokeCheck("a check box was made", isObject(%check));
	smokeCheck("a new check box has no caption (" @ %check.getText() @ ")",
		%check.getText() $= "");
	%check.delete();

	%radio = new GuiRadioCtrl();
	smokeCheck("a radio was made", isObject(%radio));
	smokeCheck("a new radio has no caption (" @ %radio.getText() @ ")",
		%radio.getText() $= "");
	%radio.delete();

	// The drop down is deliberately left alone: it draws its text only while
	// nothing is selected, so "none" is an empty state, not a caption.
	//
	// Read through the text FIELD, not getText(). GuiDropDownCtrl binds its own
	// getText, which answers the selected item's text and an empty string when
	// nothing is selected -- it never reads mText at all. Asserting on getText
	// here would report a placeholder that is perfectly intact as missing.
	%drop = new GuiDropDownCtrl();
	smokeCheck("a drop down was made", isObject(%drop));
	smokeCheck("a new drop down still reads none (" @ %drop.text @ ")",
		%drop.text $= "none");
	%drop.delete();

	schedule(200, 0, "btRoundTrip");
}

function btRoundTrip()
{
	%path = testRoot("shots/buttonTextRoundTrip.taml");

	// The bug itself: blank is written as absent, and absent used to read back
	// as "Button".
	%blank = new GuiButtonCtrl();
	%blank.setText("");
	TamlWrite(%blank, %path);
	%blank.delete();

	%loaded = TamlRead(%path);
	smokeCheck("a blank caption survives the round trip (" @ %loaded.getText() @ ")",
		isObject(%loaded) && %loaded.getText() $= "");
	%loaded.delete();

	// And the ordinary case still works.
	%captioned = new GuiButtonCtrl();
	%captioned.setText("Change username");
	TamlWrite(%captioned, %path);
	%captioned.delete();

	%reloaded = TamlRead(%path);
	smokeCheck("a set caption survives the round trip (" @ %reloaded.getText() @ ")",
		isObject(%reloaded) && %reloaded.getText() $= "Change username");
	%reloaded.delete();

	fileDelete(%path);
	echo("SMOKE DONE");
	quit();
}
