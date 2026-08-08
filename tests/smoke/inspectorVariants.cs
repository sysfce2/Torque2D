// Variants smoke test. The properties pane hides a control's secondary profile
// slots -- contentProfile, thumbProfile and the rest -- until there is actually
// something to choose between, and the set that decides whether a row appears
// is deliberately narrower than the set the row then offers.
//
// The rule, from the design:
//
//   anchors = theme members of the slot's category
//           + standalones stamped for that category
//           + whatever the slot currently holds
//   row appears  <=>  count(anchors) > 1
//   options      =  anchors + uncategorised ("Any") standalones
//
// So an "Any" standalone can be picked in a row that exists, but can never be
// the reason one appears -- otherwise a single uncategorised profile would put
// a row on every slot of every control.
// Run: tests/run.ps1 inspectorVariants  ; grep IVSMOKE in tests/logs/.

setLogMode(1);
$Scripts::ignoreDSOs = true;
setScriptExecEcho(false);
trace(false);

function vCheck(%label, %cond)
{
	if(%cond) echo("IVSMOKE PASS: " @ %label);
	else      echo("IVSMOKE FAIL: " @ %label);
}

function vPane()
{
	return GuiEditor.inspectorWindow.pane;
}

// Rebind so the pane re-evaluates the slots against the theme as it is now.
function vRebind(%ctrl)
{
	%pane = vPane();
	%pane.bind(%ctrl);
	return %pane;
}

function vHasRow(%pane, %field)
{
	return isObject(%pane.row[%field]);
}

function vOffers(%pane, %field, %name)
{
	%row = %pane.row[%field];
	return isObject(%row) && %row.editor.findItemText(%name, false) >= 0;
}

testExec("editor/main.cs");
schedule(2000, 0, "vStep1");

//-----------------------------------------------------------------------------
// One member per category: no Variants rows at all.
//-----------------------------------------------------------------------------

function vStep1()
{
	ProjectManager.setProjectFolder("inspectorVariantsSmokeProject");
	GuiEditor.open();

	// A window is the control with the most secondary slots -- content, close,
	// minimise and maximise -- so it is the one that would be noisiest if the
	// threshold were wrong.
	$vWindow = new GuiWindowCtrl();
	GuiEditor.rootGui.add($vWindow);

	$vTheme = GuiEditor.themeLibrary.createTheme("IVSmoke");
	vCheck("theme created", isObject($vTheme));

	GuiEditor.themeApplier.applyToBranch($vWindow, $vTheme, true);
	GuiEditor.themeName = $vTheme.getName();

	%pane = vRebind($vWindow);
	vCheck("window bound", %pane.target == $vWindow);
	vCheck("no Variants section with one member per category",
		!isObject(%pane.panel["Variants"]));
	vCheck("no content slot row", !vHasRow(%pane, "contentProfile"));
	vCheck("no close button slot row", !vHasRow(%pane, "closeButtonProfile"));

	// The header's own Profile picker exists regardless -- it is not a Variants
	// row and has no threshold.
	vCheck("header profile picker still there", %pane.header.profileRow.isVisible());

	schedule(200, 0, "vStep2");
}

//-----------------------------------------------------------------------------
// A second WindowContent in the theme: that slot, and only that slot, appears.
//-----------------------------------------------------------------------------

function vStep2()
{
	$vExtra = $vTheme.createProfile("WindowContent");
	vCheck("extra WindowContent created", isObject($vExtra));
	vCheck("theme reports two WindowContent members",
		getWordCount($vTheme.getProfiles("WindowContent")) == 2);

	%pane = vRebind($vWindow);
	vCheck("Variants section appeared", isObject(%pane.panel["Variants"]));
	vCheck("content slot got a row", vHasRow(%pane, "contentProfile"));
	vCheck("Variants section is visible", %pane.panel["Variants"].isVisible());

	// Only the slot with a choice. The other three categories still have one
	// member each.
	vCheck("close button slot still hidden", !vHasRow(%pane, "closeButtonProfile"));
	vCheck("min button slot still hidden", !vHasRow(%pane, "minButtonProfile"));
	vCheck("tooltip slot still hidden", !vHasRow(%pane, "tooltipProfile"));

	// The row offers both members and starts on the one the control wears.
	vCheck("row offers the default member",
		vOffers(%pane, "contentProfile", $vTheme.getProfile("WindowContent").getName()));
	vCheck("row offers the extra member",
		vOffers(%pane, "contentProfile", $vExtra.getName()));

	%current = GuiEditor.themeApplier.fieldProfile($vWindow, "contentProfile");
	vCheck("row shows what the control wears",
		isObject(%current) && %pane.row["contentProfile"].getValue() $= %current.getName());

	schedule(200, 0, "vStep3");
}

//-----------------------------------------------------------------------------
// Choosing the extra, and the write going in by id rather than by name.
//-----------------------------------------------------------------------------

function vStep3()
{
	%pane = vRebind($vWindow);
	%row = %pane.row["contentProfile"];

	// applyValue rather than setValue: setValue also records the baseline that
	// tells a later commit nothing was edited.
	%row.applyValue($vExtra.getName());
	%row.commit();

	%now = GuiEditor.themeApplier.fieldProfile($vWindow, "contentProfile");
	vCheck("choosing a variant reached the control", %now == $vExtra.getId());

	// A theme member chosen deliberately survives a re-apply: applyToControl
	// skips any slot already wearing a profile from this theme.
	GuiEditor.themeApplier.applyToBranch($vWindow, $vTheme, false);
	%after = GuiEditor.themeApplier.fieldProfile($vWindow, "contentProfile");
	vCheck("the choice survives Set Theme", %after == $vExtra.getId());

	schedule(200, 0, "vStep4");
}

//-----------------------------------------------------------------------------
// Standalones. A categorised one is an anchor; an "Any" one is only an option.
//-----------------------------------------------------------------------------

function vStep4()
{
	%library = GuiEditor.themeLibrary;

	// Uncategorised -- what createStandalone makes, and what the Profile
	// Editor shows as "Any".
	$vAny = %library.createStandalone("IVAnyProfile");
	vCheck("standalone created", isObject($vAny));
	vCheck("standalone starts uncategorised", $vAny.category $= "");

	%pane = vRebind($vWindow);

	// THE RULE: it must not have made any new row appear.
	vCheck("Any standalone adds no close button row", !vHasRow(%pane, "closeButtonProfile"));
	vCheck("Any standalone adds no tooltip row", !vHasRow(%pane, "tooltipProfile"));
	vCheck("Any standalone adds no min button row", !vHasRow(%pane, "minButtonProfile"));

	// But it is offered where a row already exists, and in the header picker,
	// because those slots are on show regardless.
	vCheck("Any standalone offered in an existing Variants row",
		vOffers(%pane, "contentProfile", "IVAnyProfile"));
	vCheck("Any standalone offered as the control's own profile",
		%pane.header.profileRow.editor.findItemText("IVAnyProfile", false) >= 0);

	// Stamp it for a category and the matching slot must appear.
	$vAny.category = "WindowCloseButton";
	%pane = vRebind($vWindow);
	vCheck("categorised standalone makes its slot appear",
		vHasRow(%pane, "closeButtonProfile"));
	vCheck("the new row offers the standalone",
		vOffers(%pane, "closeButtonProfile", "IVAnyProfile"));
	vCheck("other slots still hidden", !vHasRow(%pane, "minButtonProfile"));

	schedule(200, 0, "vStep5");
}

//-----------------------------------------------------------------------------
// What is never offered, and what the "currently assigned" term is for.
//-----------------------------------------------------------------------------

function vStep5()
{
	%pane = vRebind($vWindow);

	// A script profile is neither a theme member nor a standalone the editor
	// manages. The old inspector listed every named profile in the sim; that is
	// the dropdown this pane exists to replace.
	vCheck("script profiles are not offered",
		!vOffers(%pane, "contentProfile", "GuiDefaultProfile"));

	// A second theme's members are not offered either, even at the right
	// category -- the Gui wears one theme.
	$vOther = GuiEditor.themeLibrary.createTheme("IVOther");
	%pane = vRebind($vWindow);
	vCheck("another theme's member is not offered",
		!vOffers(%pane, "contentProfile", $vOther.getProfile("WindowContent").getName()));

	// Unless the control is actually wearing it. The currently-assigned term
	// exists so a slot holding something the theme does not offer is visible
	// and changeable rather than silently stuck -- hand-edit a saved Gui to
	// point one slot at another theme and that slot, and only that slot, gets a
	// picker offering both.
	$vWindow.setEditFieldValue("minButtonProfile", $vOther.getProfile("WindowButton").getId());
	%pane = vRebind($vWindow);
	vCheck("an off-theme assignment makes its row appear",
		vHasRow(%pane, "minButtonProfile"));
	vCheck("and the row offers what is actually worn",
		vOffers(%pane, "minButtonProfile", $vOther.getProfile("WindowButton").getName()));

	// Only that slot. maxButtonProfile shares minButtonProfile's category and
	// is still on the current theme, so it stays quiet -- the current value is
	// counted per slot, not per category.
	vCheck("a sibling slot on the current theme stays hidden",
		!vHasRow(%pane, "maxButtonProfile"));

	$vWindow.setEditFieldValue("Profile", $vOther.getProfile("Window").getId());
	%pane = vRebind($vWindow);
	vCheck("the header always shows the control's own profile",
		%pane.header.profileRow.getValue() $= $vOther.getProfile("Window").getName());

	// A control with no secondary slots at all never gets the section.
	$vButton = new GuiButtonCtrl();
	GuiEditor.rootGui.add($vButton);
	GuiEditor.themeApplier.applyToBranch($vButton, $vTheme, true);
	%pane = vRebind($vButton);
	vCheck("a button has no Variants section", !isObject(%pane.panel["Variants"]));

	schedule(200, 0, "vStep6");
}

//-----------------------------------------------------------------------------
// The real drop order, which is where this all went wrong.
//
// GuiEditorBrain::onControlDropped adds the control -- which announces it, and
// so inspects it -- and only THEN applies the theme. A GuiWindowCtrl's
// constructor names five profiles (GuiWindowProfile, GuiWindowContentProfile
// and the rest), so the pane used to read all five as the control's current
// choice: four slots crossed the threshold, every drop-down offered a
// Gui*Profile, and the boxes showed one as selected. None of it was true a
// moment later.
//-----------------------------------------------------------------------------

function vStep6()
{
	GuiEditor.themeName = $vTheme.getName();

	// Exactly what the brain does, in its order.
	$vDropped = new GuiWindowCtrl();
	vCheck("a fresh window wears its constructor's profile",
		$vDropped.Profile $= "GuiWindowProfile");

	GuiEditor.brain.addNewCtrl($vDropped);
	GuiEditor.themeApplier.applyToBranch($vDropped, $vTheme, false);
	GuiEditor.brain.postEvent("Rethemed", $vDropped);

	%pane = GuiEditor.inspectorWindow.pane;
	vCheck("the drop left the pane on the dropped control", %pane.target == $vDropped);
	vCheck("the control ended up on the theme",
		$vDropped.Profile $= $vTheme.getProfile("Window").getName());

	// The symptom. Not "no Variants section at all" -- by now this theme has a
	// second WindowContent and there is a standalone stamped WindowCloseButton,
	// so those two slots have a genuine choice and should show. The bug was the
	// slots with NO choice showing, because the constructor's profile counted
	// as one. WindowButton has a single member and no standalone, so its two
	// slots are the clean test.
	vCheck("a slot with no real choice stays hidden after a drop",
		!vHasRow(%pane, "minButtonProfile") && !vHasRow(%pane, "maxButtonProfile"));

	// And no constructor default may appear anywhere, including in the rows
	// that are legitimately on show.
	%slots = %pane.panelFields["Variants"];
	%leaked = "";
	for(%i = 0; %i < getWordCount(%slots); %i++)
	{
		%field = getWord(%slots, %i);
		if(%pane.row[%field].editor.findItemText("Gui" @ %field, false) >= 0)
		{
			%leaked = %leaked SPC %field;
		}
	}
	vCheck("no constructor default leaked into a Variants row (" @ %leaked @ ")",
		%leaked $= "");
	vCheck("content row offers the theme's member",
		!vHasRow(%pane, "contentProfile") ||
		vOffers(%pane, "contentProfile", $vTheme.getProfile("WindowContent").getName()));

	// And the header must show what the control actually wears, not the
	// constructor default it was announced with.
	vCheck("the header shows the themed profile",
		%pane.header.profileRow.getValue() $= $vTheme.getProfile("Window").getName());
	vCheck("the constructor default is not offered",
		%pane.header.profileRow.editor.findItemText("GuiWindowProfile", false) < 0);

	schedule(200, 0, "vStep7");
}

// Set Theme sweeps the document; the pane has to hear about that too, and the
// list it rebuilds must not keep a ghost of the theme it just left.
function vStep7()
{
	%pane = GuiEditor.inspectorWindow.pane;
	%before = $vDropped.Profile;

	GuiEditor.setTheme($vOther, false);

	vCheck("Set Theme moved the control", $vDropped.Profile !$= %before);
	vCheck("the header followed Set Theme without a reselect",
		%pane.header.profileRow.getValue() $= $vOther.getProfile("Window").getName());
	vCheck("the old theme's profile is not left in the list",
		%pane.header.profileRow.editor.findItemText(%before, false) < 0);

	echo("IVSMOKE DONE");
	schedule(300, 0, "quit");
}
