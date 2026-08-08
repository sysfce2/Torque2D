
// %tooltipFunction is optional and names a method on the tool that answers the
// tip text. It exists for a button that does the same job to more than one kind
// of thing: a "new in this category" button whose tip says "Profile" while a
// cursor is selected is describing the wrong thing. Omit it and %tooltip is
// simply fixed, as it is for every other button.
function EditorButtonBar::addButton(%this, %click, %frame, %tooltip, %enabled, %tooltipFunction)
{
	if(!isObject(%this.tool))
	{
		warn("EditorButtonBar::addButton: Cannot add a button without a tool to handle callback.");
		return;
	}

	%button = new GuiButtonCtrl()
	{
		Class = "EditorIconButton";
		Frame = %frame;
		Position = "0 0";
		Command = %this.tool.getId() @ "." @ %click @ "();";
		Tooltip = %tooltip;
		EnabledFunction = %enabled;
		TooltipFunction = %tooltipFunction;
	};
	ThemeManager.setProfile(%button, "iconButtonProfile");
	%this.add(%button);

	if(%enabled !$= "")
	{
		%button.setActive(%this.tool.call(%enabled));
	}

	return %button;
}

function EditorButtonBar::refreshEnabled(%this)
{
	for(%i = 0; %i < %this.getCount(); %i++)
	{
		%button = %this.getObject(%i);
		if(%button.EnabledFunction !$= "")
		{
			%button.setActive(%this.tool.call(%button.EnabledFunction));
		}
		if(%button.TooltipFunction !$= "")
		{
			%button.Tooltip = %this.tool.call(%button.TooltipFunction);
		}
	}
}

function EditorButtonBar::onRemove(%this)
{
	%this.deleteObjects();
}
