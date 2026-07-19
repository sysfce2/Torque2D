//-----------------------------------------------------------------------------
// PlanetXUpgradeScreen: the end-of-level upgrade picker that stands in for the old
// victory dialog. A manager ScriptObject (same shape as PlanetXHud): it builds one
// GuiControl dialog of upgrade cards in onAdd and deletes it in onRemove, so the
// whole screen is freed with a single delete.
//
// The offered upgrade keys are handed in as %this.offered (a space-separated list
// from PlanetXUpgrades::offer). Each card shows a 3:4 image, a title, and a short
// description, with a CHOOSE/UNDO button beneath it. Selection is mouse-driven and
// sequential: a free card is claimed by the next player who still needs a pick
// (player 1, then player 2 in co-op); clicking a claimed card releases it (undo).
// DEPLOY appears once everyone has chosen and advances to the next level.
//
// Player 1's color is green (#21bf84); player 2's is red (#f64b48) - the same colors
// their hull bars use. See TORQUE_SCRIPT.md.
//-----------------------------------------------------------------------------

// Card art tints: a claimed card glows in its owner's color; a free card is white.
$PlanetX::UpgradeFreeTint = "255 255 255 255";
$PlanetX::UpgradeP1Tint   = "90 230 170 255";
$PlanetX::UpgradeP2Tint   = "255 120 118 255";

// Text colors matching the two players (used for titles, prompt, and badges).
$PlanetX::UpgradeP1Color = "33 191 132 255";
$PlanetX::UpgradeP2Color = "246 75 72 255";

function PlanetXUpgradeScreen::onAdd(%this)
{
	%this.count = getWordCount(%this.offered);

	// No card claimed by either player yet (-1 = this player hasn't chosen).
	%this.pick[1] = -1;
	%this.pick[2] = -1;

	%this.build();
	%this.refresh();
}

/// The dialog and every control in it hang off %this.dialog, so one delete frees the
/// whole screen. ScreenFade only ever removes the dialog from its backdrop; it never
/// deletes it, so ownership stays here.
function PlanetXUpgradeScreen::onRemove(%this)
{
	if (isObject(%this.dialog))
		%this.dialog.delete();
}

//-----------------------------------------------------------------------------
// Construction.
//-----------------------------------------------------------------------------

function PlanetXUpgradeScreen::build(%this)
{
	%dialogW = 800;
	%dialogH = 606;
	%cardW = 232;
	%cardH = 406;
	%gap = 20;
	%cardsY = 112;

	// The cards are centered as a row, so two cards sit as neatly as three.
	%cardsW = %this.count * %cardW + (%this.count - 1) * %gap;
	%startX = (%dialogW - %cardsW) / 2;

	%this.dialog = new GuiControl()
	{
		Profile = "GuiWindowProfile";
		HorizSizing = "center";
		VertSizing = "center";
		Position = ((1024 - %dialogW) / 2) SPC ((768 - %dialogH) / 2);
		Extent = %dialogW SPC %dialogH;
	};

	%heading = new GuiControl()
	{
		Profile = "PlanetXLabelProfile";
		HorizSizing = "center";
		VertSizing = "bottom";
		Position = "0 24";
		Extent = %dialogW SPC 44;
		Text = "LEVEL" SPC %this.levelNum SPC "CLEARED";
		Align = "center";
		OverrideFontColor = "1";
		FontColor = $PlanetX::UpgradeP1Color;
	};
	%this.dialog.add(%heading);

	// Prompt line: whose turn it is, or that they are ready to deploy.
	%this.prompt = new GuiControl()
	{
		Profile = "PlanetXCardTitleProfile";
		HorizSizing = "center";
		VertSizing = "bottom";
		Position = "0 74";
		Extent = %dialogW SPC 36;
		Align = "center";
		OverrideFontColor = "1";
	};
	%this.dialog.add(%this.prompt);

	for (%i = 0; %i < %this.count; %i++)
	{
		%x = %startX + %i * (%cardW + %gap);
		%this.buildCard(%i, getWord(%this.offered, %i), %x, %cardsY, %cardW, %cardH);
	}

	// DEPLOY is hidden until everyone has chosen (see refresh).
	%this.deployButton = new GuiButtonCtrl()
	{
		Profile = "PlanetXButtonProfile";
		HorizSizing = "center";
		VertSizing = "bottom";
		Position = ((%dialogW - 280) / 2) SPC 530;
		Extent = "280 52";
		Text = "DEPLOY";
		Command = "PlanetXGame.playClick(); PlanetXGame.upgradeScreen.deploy();";
	};
	%this.dialog.add(%this.deployButton);
}

/// Build card %i for upgrade %key at (%x,%y). Stashes the pieces refresh() recolors.
function PlanetXUpgradeScreen::buildCard(%this, %i, %key, %x, %y, %w, %h)
{
	%titleText = PlanetXUpgrades.title[%key];
	%descText  = PlanetXUpgrades.desc[%key];
	%imgAsset  = PlanetXUpgrades.image[%key];

	%card = new GuiControl()
	{
		Profile = "GuiDefaultProfile";
		HorizSizing = "right";
		VertSizing = "bottom";
		Position = %x SPC %y;
		Extent = %w SPC %h;
	};
	%this.dialog.add(%card);

	// Image: a 3:4 panel, centered near the top of the card.
	%imgW = 168;
	%imgH = 224;
	%img = new GuiSpriteCtrl()
	{
		Profile = "GuiDefaultProfile";
		HorizSizing = "right";
		VertSizing = "bottom";
		Position = ((%w - %imgW) / 2) SPC 8;
		Extent = %imgW SPC %imgH;
		Image = %imgAsset;
		SingleFrameBitmap = "1";
		FullSize = "1";
		ConstrainProportions = "0";
		ClampImage = "0";
		ImageColor = $PlanetX::UpgradeFreeTint;
	};
	%card.add(%img);

	// Title wraps to a second line for the longer names.
	%title = new GuiControl()
	{
		Profile = "PlanetXCardTitleProfile";
		HorizSizing = "right";
		VertSizing = "bottom";
		Position = "6 236";
		Extent = (%w - 12) SPC 50;
		Text = %titleText;
		Align = "center";
		TextWrap = "1";
	};
	%card.add(%title);

	%desc = new GuiControl()
	{
		Profile = "PlanetXCardBodyProfile";
		HorizSizing = "right";
		VertSizing = "bottom";
		Position = "12 290";
		Extent = (%w - 24) SPC 72;
		Text = %descText;
		Align = "center";
		TextWrap = "1";
	};
	%card.add(%desc);

	%btn = new GuiButtonCtrl()
	{
		Profile = "PlanetXCardButtonProfile";
		HorizSizing = "right";
		VertSizing = "bottom";
		Position = ((%w - 176) / 2) SPC 366;
		Extent = "176 34";
		Text = "CHOOSE";
		Command = "PlanetXGame.playClick(); PlanetXGame.upgradeScreen.clickCard(" @ %i @ ");";
	};
	%card.add(%btn);

	%this.cardKey[%i]    = %key;
	%this.cardOwner[%i]  = 0;
	%this.cardImage[%i]  = %img;
	%this.cardTitle[%i]  = %title;
	%this.cardButton[%i] = %btn;
}

//-----------------------------------------------------------------------------
// Selection.
//-----------------------------------------------------------------------------

/// A card was clicked. Clicking your own card releases it (undo). Clicking a free
/// card claims it for whoever is choosing - and if that player already holds another
/// card, moves their pick to this one, so a solo player switches just by clicking a
/// different card. One owner per card keeps the two co-op players on distinct upgrades.
function PlanetXUpgradeScreen::clickCard(%this, %i)
{
	%owner = %this.cardOwner[%i];

	if (%owner != 0)
	{
		// Undo: hand the card back.
		%this.cardOwner[%i] = 0;
		%this.pick[%owner] = -1;
	}
	else
	{
		// Whoever is choosing claims this card. Solo, that is always player 1, so a
		// click on a different card just switches the pick. In co-op it is player 1
		// until they have chosen, then player 2; once both have chosen, changing a
		// pick means undoing it first (click your own card).
		%who = 0;
		if (%this.pick[1] == -1)
			%who = 1;
		else if ($PlanetX::twoPlayer && %this.pick[2] == -1)
			%who = 2;
		else if (!$PlanetX::twoPlayer)
			%who = 1;

		if (%who == 0)
			return;

		// Switching: release the card this player was holding before taking the new one.
		if (%this.pick[%who] != -1)
			%this.cardOwner[%this.pick[%who]] = 0;

		%this.cardOwner[%i] = %who;
		%this.pick[%who] = %i;
	}

	%this.refresh();
}

/// Have all the choosers (one solo, both in co-op) picked a card?
function PlanetXUpgradeScreen::isReady(%this)
{
	if (%this.pick[1] == -1)
		return false;
	if ($PlanetX::twoPlayer && %this.pick[2] == -1)
		return false;
	return true;
}

/// Repaint every card to its owner state, update the prompt, and show DEPLOY once
/// the picks are in.
function PlanetXUpgradeScreen::refresh(%this)
{
	for (%i = 0; %i < %this.count; %i++)
	{
		%owner = %this.cardOwner[%i];
		%title = PlanetXUpgrades.title[%this.cardKey[%i]];

		if (%owner == 1)
		{
			%this.cardImage[%i].setImageColor($PlanetX::UpgradeP1Tint);
			%this.colorText(%this.cardTitle[%i], %title, "1", $PlanetX::UpgradeP1Color);
			%this.cardButton[%i].setText("UNDO");
		}
		else if (%owner == 2)
		{
			%this.cardImage[%i].setImageColor($PlanetX::UpgradeP2Tint);
			%this.colorText(%this.cardTitle[%i], %title, "1", $PlanetX::UpgradeP2Color);
			%this.cardButton[%i].setText("UNDO");
		}
		else
		{
			%this.cardImage[%i].setImageColor($PlanetX::UpgradeFreeTint);
			%this.colorText(%this.cardTitle[%i], %title, "0", $PlanetX::UpgradeFreeTint);
			%this.cardButton[%i].setText("CHOOSE");
		}
	}

	%ready = %this.isReady();
	if (%ready)
		%this.setPrompt("READY TO DEPLOY", "255 255 255 255");
	else if (%this.pick[1] == -1)
		%this.setPrompt($PlanetX::twoPlayer ? "PLAYER 1 - CHOOSE" : "CHOOSE AN UPGRADE", $PlanetX::UpgradeP1Color);
	else
		%this.setPrompt("PLAYER 2 - CHOOSE", $PlanetX::UpgradeP2Color);

	%this.deployButton.setVisible(%ready);
}

/// Recolor a text control. OverrideFontColor/FontColor only take effect on the next
/// setText, so we re-set the text to force the repaint (same trick as PlanetXHud).
function PlanetXUpgradeScreen::colorText(%this, %ctrl, %text, %override, %color)
{
	%ctrl.OverrideFontColor = %override;
	%ctrl.FontColor = %color;
	%ctrl.setText(%text);
}

function PlanetXUpgradeScreen::setPrompt(%this, %text, %color)
{
	%this.prompt.OverrideFontColor = "1";
	%this.prompt.FontColor = %color;
	%this.prompt.setText(%text);
}

//-----------------------------------------------------------------------------
// Deploy.
//-----------------------------------------------------------------------------

/// Bank each player's chosen upgrade and advance. nextLevel closes this dialog (it
/// posts dialogClose to the active dialog), tears the level down, and rebuilds - the
/// new players re-apply the banked upgrades in their onAdd.
function PlanetXUpgradeScreen::deploy(%this)
{
	if (!%this.isReady())
		return;

	PlanetXUpgrades.take(%this.cardKey[%this.pick[1]], 1);
	if ($PlanetX::twoPlayer)
		PlanetXUpgrades.take(%this.cardKey[%this.pick[2]], 2);

	PlanetXGame.nextLevel();
}
