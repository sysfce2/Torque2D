
function AssetParticleGraphUnit::onAdd(%this)
{
	// Everything here is placed against the left inset rather than a literal 30,
	// so a subclass wanting another column of its own down the left side moves the
	// whole arrangement by overriding one number.
	%inset = %this.getLeftInset();

	%this.graph = %this.createGraph();
	%this.graph.HorizSizing = "width";
	%this.graph.VertSizing = "height";
	%this.graph.Position = %inset SPC 18;
	%this.graph.Extent = (getWord(%this.extent, 0) - %inset - 10) SPC (getWord(%this.extent, 1) - 60);
	ThemeManager.setProfile(%this.graph, "graphProfile");
	%this.add(%this.graph);

	// The value buttons sit in the column immediately left of the graph, whatever
	// the inset is.
	%valueX = %inset - 28;

	// Value zoom buttons. A plus and minus in a square rather than the magnifier
	// pair these used to wear: the icon set has a magnifier but no +/- variants
	// of it, and the squared pair stays distinct from the round plus and minus,
	// which mean add and remove everywhere else in the editor.
	%center = 6 + mRound(getWord(%this.graph.extent, 1) / 2);
	%this.valueZoomInButton = new GuiButtonCtrl()
	{
		Class = "EditorIconButton";
		Frame = $EditorIcon::sq_plus;
		Position = %valueX SPC (%center + 13);
		Command = %this.getId() @ ".valueZoomIn();";
		Tooltip = "Zoom In";
	};
	ThemeManager.setProfile(%this.valueZoomInButton, "iconButtonProfile");
	%this.add(%this.valueZoomInButton);

	%this.valueZoomOutButton = new GuiButtonCtrl()
	{
		Class = "EditorIconButton";
		Frame = $EditorIcon::sq_minus;
		Position = %valueX SPC (%center - 13);
		Command = %this.getId() @ ".valueZoomOut();";
		Tooltip = "Zoom Out";
	};
	ThemeManager.setProfile(%this.valueZoomOutButton, "iconButtonProfile");
	%this.add(%this.valueZoomOutButton);

	//Value move buttons
	%this.valueMoveUpButton = new GuiButtonCtrl()
	{
		Class = "EditorIconButton";
		Frame = $EditorIcon::arrow_top;
		Position = %valueX SPC 18;
		Command = %this.getId() @ ".valueMoveUp();";
		Tooltip = "Move Graph Up";
	};
	ThemeManager.setProfile(%this.valueMoveUpButton, "iconButtonProfile");
	%this.add(%this.valueMoveUpButton);

	%this.valueMoveDownButton = new GuiButtonCtrl()
	{
		Class = "EditorIconButton";
		Frame = $EditorIcon::arrow_bottom;
		Position = %valueX SPC (getWord(%this.extent, 1) - 66);
		Command = %this.getId() @ ".valueMoveDown();";
		Tooltip = "Move Graph Down";
	};
	ThemeManager.setProfile(%this.valueMoveDownButton, "iconButtonProfile");
	%this.add(%this.valueMoveDownButton);

	//time zoom buttons
	%bottom = getWord(%this.extent, 1) - 38;
	%this.timeZoomContainer = new GuiControl()
	{
		HorizSizing = "Center";
		Position = "0" SPC %bottom;
		Extent = "50 24";
	};
	ThemeManager.setProfile(%this.timeZoomContainer, "emptyProfile");
	%this.add(%this.timeZoomContainer);

	%this.timeZoomInButton = new GuiButtonCtrl()
	{
		Class = "EditorIconButton";
		Frame = $EditorIcon::sq_plus;
		Position = "0 0";
		Command = %this.getId() @ ".timeZoomIn();";
		Tooltip = "Zoom In";
	};
	ThemeManager.setProfile(%this.timeZoomInButton, "iconButtonProfile");
	%this.timeZoomContainer.add(%this.timeZoomInButton);

	%this.timeZoomOutButton = new GuiButtonCtrl()
	{
		Class = "EditorIconButton";
		Frame = $EditorIcon::sq_minus;
		Position = "26 0";
		Command = %this.getId() @ ".timeZoomOut();";
		Tooltip = "Zoom Out";
	};
	ThemeManager.setProfile(%this.timeZoomOutButton, "iconButtonProfile");
	%this.timeZoomContainer.add(%this.timeZoomOutButton);

	//Time move buttons
	%this.timeMoveBackButton = new GuiButtonCtrl()
	{
		Class = "EditorIconButton";
		Frame = $EditorIcon::arrow_left;
		HorizSizing = "right";
		Position = %inset SPC %bottom;
		Command = %this.getId() @ ".timeMoveBack();";
		Tooltip = "Move Graph Back";
	};
	ThemeManager.setProfile(%this.timeMoveBackButton, "iconButtonProfile");
	%this.add(%this.timeMoveBackButton);

	%this.timeMoveForwardButton = new GuiButtonCtrl()
	{
		Class = "EditorIconButton";
		Frame = $EditorIcon::arrow_right;
		HorizSizing = "left";
		Position = (getWord(%this.graph.extent, 0) + 6) SPC %bottom;
		Command = %this.getId() @ ".timeMoveForward();";
		Tooltip = "Move Graph Forward";
	};
	ThemeManager.setProfile(%this.timeMoveForwardButton, "iconButtonProfile");
	%this.add(%this.timeMoveForwardButton);

	%this.addExtraControls();
}

// The graph this unit wraps. A subclass showing something other than one curve
// answers with its own control and inherits every button above unchanged.
function AssetParticleGraphUnit::createGraph(%this)
{
	return new GuiParticleGraphInspector();
}

// How much of the unit's left edge belongs to buttons rather than to the graph.
function AssetParticleGraphUnit::getLeftInset(%this)
{
	return 30;
}

// Anything a subclass wants in the room its inset bought. Nothing, here.
function AssetParticleGraphUnit::addExtraControls(%this)
{
}

// A unit that has nothing to show is taken out of the grid rather than emptied,
// so the cells that remain close up over it.
function AssetParticleGraphUnit::attach(%this)
{
	if(!%this.Tool.isMember(%this))
	{
		%this.Tool.add(%this);
	}
}

function AssetParticleGraphUnit::detach(%this)
{
	if(%this.Tool.isMember(%this))
	{
		%this.Tool.remove(%this);
	}
}

function AssetParticleGraphUnit::setToScale(%this, %scaleName)
{
	%this.graph.setDisplayLabels("Time", "Scale");
	%this.graph.setDisplayField(%scaleName);
}

function AssetParticleGraphUnit::setToBase(%this, %baseName, %variName, %emitterID)
{
	%this.graph.setDisplayLabels("Time", "Base Value");
	%this.graph.setDisplayField(%baseName, %emitterID);
}

function AssetParticleGraphUnit::setToVari(%this, %variName, %emitterID)
{
	if(%variName $= "")
	{
		%this.detach();
		return;
	}

	%this.attach();
	%this.graph.setDisplayLabels("Time", "Variation");
	%this.graph.setDisplayField(%variName, %emitterID);
}

function AssetParticleGraphUnit::setToLife(%this, %lifeName, %emitterID)
{
	if(%lifeName $= "")
	{
		%this.detach();
		return;
	}

	%this.attach();
	%this.graph.setDisplayLabels("Time", "Scale");
	%this.graph.setDisplayField(%lifeName, %emitterID);
}

function AssetParticleGraphUnit::setValueController(%this, %controller)
{
	if(!isObject(%controller))
	{
		return;
	}
	if(isObject(%this.valueController))
	{
		%this.valueController.delete();
	}

	%this.valueController = %controller;
	%this.refreshCamera();
}

function AssetParticleGraphUnit::setTimeController(%this, %controller)
{
	if(!isObject(%controller))
	{
		return;
	}
	if(isObject(%this.timeController))
	{
		%this.timeController.delete();
	}

	%this.timeController = %controller;
	%this.refreshCamera();
}

function AssetParticleGraphUnit::refreshCamera(%this)
{
	if(!isObject(%this.timeController) || !isObject(%this.valueController))
	{
		return;
	}

	%xMin = %this.timeController.getCameraMin();
	%xMax = %this.timeController.getCameraMax();
	%yMin = %this.valueController.getCameraMin();
	%yMax = %this.valueController.getCameraMax();

	%this.graph.setDisplayArea(%xMin SPC %yMin SPC %xMax SPC %yMax);

	%this.valueMoveUpButton.setActive(%this.valueController.getMoveUpEnabled());
	%this.valueMoveDownButton.setActive(%this.valueController.getMoveDownEnabled());
	%this.valueZoomInButton.setActive(%this.valueController.getZoomInEnabled());
	%this.valueZoomOutButton.setActive(%this.valueController.getZoomOutEnabled());

	%this.timeMoveForwardButton.setActive(%this.timeController.getMoveUpEnabled());
	%this.timeMoveBackButton.setActive(%this.timeController.getMoveDownEnabled());
	%this.timeZoomInButton.setActive(%this.timeController.getZoomInEnabled());
	%this.timeZoomOutButton.setActive(%this.timeController.getZoomOutEnabled());
}

function AssetParticleGraphUnit::valueZoomIn(%this)
{
	%this.valueController.zoomIn();
	%this.refreshCamera();
}

function AssetParticleGraphUnit::valueZoomOut(%this)
{
	%this.valueController.zoomOut();
	%this.refreshCamera();
}

function AssetParticleGraphUnit::valueMoveUp(%this)
{
	%this.valueController.moveUp();
	%this.refreshCamera();
}

function AssetParticleGraphUnit::valueMoveDown(%this)
{
	%this.valueController.moveDown();
	%this.refreshCamera();
}

function AssetParticleGraphUnit::timeZoomIn(%this)
{
	%this.timeController.zoomIn();
	%this.refreshCamera();
}

function AssetParticleGraphUnit::timeZoomOut(%this)
{
	%this.timeController.zoomOut();
	%this.refreshCamera();
}

function AssetParticleGraphUnit::timeMoveBack(%this)
{
	%this.timeController.moveDown();
	%this.refreshCamera();
}

function AssetParticleGraphUnit::timeMoveForward(%this)
{
	%this.timeController.moveUp();
	%this.refreshCamera();
}

function AssetParticleGraphUnit::setVarianceGraph(%this, %variGraph)
{
	%this.graph.setVariationGraphInspector(%variGraph.graph);
}
