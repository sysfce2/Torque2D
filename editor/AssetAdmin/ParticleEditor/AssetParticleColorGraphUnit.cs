
//-----------------------------------------------------------------------------
// The graph unit for an emitter's color, the one selection that shows something
// other than a single curve.
//
// It is an AssetParticleGraphUnit with two differences: the graph it builds is a
// GuiEditParticleColorGraph rather than a plain inspector, and it buys itself a
// second column down the left side for the three channel toggles. Everything
// else -- the zoom and pan buttons, the cameras, attaching to and detaching from
// the tool's grid -- is the superclass's and unchanged.
//
// Exactly one channel is live. The toggles are a radio group, and which one is
// pressed is asked of the graph rather than remembered here, so the buttons
// cannot drift out of step with what a click on the plot will edit.
//-----------------------------------------------------------------------------

function AssetParticleColorGraphUnit::createGraph(%this)
{
	return new GuiEditParticleColorGraph();
}

function AssetParticleColorGraphUnit::getLeftInset(%this)
{
	// 30 for the zoom and pan column the superclass places, and 30 more for the
	// channel toggles this unit adds outside it.
	return 60;
}

function AssetParticleColorGraphUnit::addExtraControls(%this)
{
	%this.channelCount = 3;
	%this.channel[0] = "Red";
	%this.channel[1] = "Green";
	%this.channel[2] = "Blue";

	for(%i = 0; %i < %this.channelCount; %i++)
	{
		%channel = %this.channel[%i];

		%toggle = new GuiCheckBoxCtrl()
		{
			Class = "AssetParticleChannelToggle";
			superclass = "EditorToggleIcon";
			channel = %channel;
			owner = %this;
			frameOff = $EditorIcon::square_shape;
			tipOff = "Edit the " @ %channel @ " channel";
			Position = "2" SPC (18 + (%i * 26));
			Extent = "24 24";
		};
		ThemeManager.setProfile(%toggle, "iconButtonProfile");
		%this.add(%toggle);

		%this.toggle[%channel] = %toggle;
	}
}

// Point the unit at an emitter and show it. The labels say Color rather than the
// Base Value the other units use: these are life curves, whatever the field
// collection files them under.
//
// The channel is whichever one was already live, so switching emitters leaves you
// looking at the same channel you were editing. Setting the field is what carries
// the emitter index, and the graph keeps its live channel in step with it.
function AssetParticleColorGraphUnit::setToColor(%this, %emitterID)
{
	%this.attach();
	%this.graph.setDisplayLabels("Time", "Color");
	%this.graph.setDisplayField(%this.graph.getActiveChannel() @ "Channel", %emitterID);
	%this.refreshToggles();
}

// A checkbox has already flipped itself by the time this runs, so the live
// channel is put back on rather than being allowed to switch off, and the other
// two are cleared.
function AssetParticleColorGraphUnit::onToggleIconChanged(%this, %toggle)
{
	%this.graph.setActiveChannel(%toggle.channel);
	%this.refreshToggles();
}

// The graph is asked which channel is live rather than told, so a channel set
// any other way still lights the right button.
function AssetParticleColorGraphUnit::refreshToggles(%this)
{
	%active = %this.graph.getActiveChannel();

	for(%i = 0; %i < %this.channelCount; %i++)
	{
		%channel = %this.channel[%i];
		%this.toggle[%channel].setValue(%channel $= %active);
	}
}
