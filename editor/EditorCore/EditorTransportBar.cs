//-----------------------------------------------------------------------------
// Copyright (c) 2013 GarageGames, LLC
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to
// deal in the Software without restriction, including without limitation the
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
// sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
// IN THE SOFTWARE.
//-----------------------------------------------------------------------------

//-----------------------------------------------------------------------------
// The chrome a transport bar is made of: sized icon buttons, stateful toggles,
// and the gaps between them. What each button DOES belongs to the subclass; this
// knows only how one should look and how to put it on the chain.
//
// A bar of these sits as an overlay on a preview rather than in a strip of its
// own -- which is where the audio play button already sat, and proof that an
// overlay there receives clicks over a SceneWindow. It costs no layout and takes
// no room from the art.
//
// Not built from EditorButtonBar: that makes EditorIconButtons, which are
// momentary, and a transport usually has at least one button that has to SHOW a
// state.
//
// A subclass is a GuiChainCtrl with class = its own name and superclass =
// "EditorTransportBar". onAdd does not chain in TorqueScript, so the subclass's
// onAdd calls %this.init() first and then adds its buttons.
//
// Used by AssetAnimationTransportBar and AssetParticleTransportBar.
//-----------------------------------------------------------------------------

$EditorTransportBar::buttonSize = 24;
$EditorTransportBar::playSize = 36;
$EditorTransportBar::spacing = 4;
$EditorTransportBar::gap = 16;

function EditorTransportBar::init(%this)
{
	ThemeManager.setProfile(%this, "emptyProfile");
}

// How much bigger a toggle has to be than a push button to LOOK the same size.
//
// They draw differently. A GuiButtonCtrl paints its chrome across the whole
// control less its margins; a GuiCheckBoxCtrl paints a box that
// GuiCheckBoxCtrl::onRender clamps into the CONTENT rect -- inside the borders
// and padding as well. iconButtonProfile has a 2 pixel border on all four sides,
// so a 24 pixel toggle drew a 20 pixel box beside a 24 pixel button, and no
// amount of boxExtent fixed it: the clamp will not let the box out.
//
// So the toggle is built that much larger and its box comes out the right size.
// Read from the profile rather than written as 4, because a theme is free to
// give the button a different border.
function EditorTransportBar::chromeInset(%this)
{
	%profile = ThemeManager.activeTheme.iconButtonProfile;

	return (%profile.borderLeft.border + %profile.borderRight.border) SPC
		(%profile.borderTop.border + %profile.borderBottom.border);
}

// A button that shows which of two states it is in, and reports the change to
// the bar as onToggleIconChanged.
function EditorTransportBar::addToggle(%this, %name, %frameOn, %frameOff, %tipOn, %tipOff)
{
	%size = $EditorTransportBar::buttonSize;
	%inset = %this.chromeInset();

	%button = new GuiCheckBoxCtrl()
	{
		class = "EditorToggleIcon";
		Position = "0 0";
		VertSizing = "center";
		Extent = (%size + getWord(%inset, 0)) SPC (%size + getWord(%inset, 1));
		frameOn = %frameOn;
		frameOff = %frameOff;
		tipOn = %tipOn;
		tipOff = %tipOff;
		toggleName = %name;
		owner = %this;
	};
	ThemeManager.setProfile(%button, "iconButtonProfile");
	ThemeManager.setProfile(%button, "tipProfile", "TooltipProfile");
	%this.add(%button);

	return %button;
}

function EditorTransportBar::addButton(%this, %method, %frame, %tooltip, %size)
{
	%size = (%size $= "") ? $EditorTransportBar::buttonSize : %size;

	// Said in the block, not set afterwards. EditorIconButton forces its own
	// extent in onAdd and its hover handlers animate the icon to sizes of their
	// own, so a resize applied after the add survived exactly until the pointer
	// first crossed it -- and the chain had already sized itself around the
	// smaller button by then, which is what clipped the big one.
	%button = new GuiButtonCtrl()
	{
		class = "EditorIconButton";
		Position = "0 0";
		VertSizing = "center";
		// buttonSize only. The icon is deliberately left at its default, so the
		// big play button is a bigger BUTTON with the same picture on it as the
		// rest -- which is what makes it easy to find without making it look like
		// a different kind of control.
		buttonSize = %size;
		Frame = %frame;
		Command = %this.getId() @ "." @ %method @ "();";
		Tooltip = %tooltip;
	};
	ThemeManager.setProfile(%button, "iconButtonProfile");
	ThemeManager.setProfile(%button, "tipProfile", "TooltipProfile");
	%this.add(%button);

	return %button;
}

// A chain lays out what it can see, so an empty control is how a gap is spelled
// -- there is no spacing-before on a child.
function EditorTransportBar::addSpacer(%this, %width)
{
	%spacer = new GuiControl()
	{
		Position = "0 0";
		Extent = %width SPC $EditorTransportBar::buttonSize;
		UseInput = false;
	};
	ThemeManager.setProfile(%spacer, "emptyProfile");
	%this.add(%spacer);

	return %spacer;
}

// A chain lays out only the children it can see, and nothing re-lays it out when
// one is hidden -- so swapping two buttons over is a resize away from leaving a
// hole where the other one was. Every subclass that hides a button needs this.
function EditorTransportBar::relayout(%this)
{
	%this.resize(getWord(%this.getPosition(), 0), getWord(%this.getPosition(), 1),
		getWord(%this.getExtent(), 0), getWord(%this.getExtent(), 1));
}
