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
// "The death animation is frames 28 to 32", asked for in a dialog.
//
// Dragging twenty-five frames one at a time is the thing this exists to spare
// people. The arithmetic lives in AssetAnimationFrameRange, which knows nothing
// about controls; this asks for the numbers and shows the answer back before
// anything is written.
//
// That feedback line is the whole point of the dialog rather than a nicety --
// step, hold and ping-pong interact in ways nobody should have to predict, and
// reading the actual frames out is quicker than explaining the rules.
//-----------------------------------------------------------------------------

function AssetAnimationRangeDialog::init(%this, %width, %height)
{
	%window = %this.getObject(0);
	%content = %window.getObject(0);

	%form = new GuiGridCtrl()
	{
		class = "EditorForm";
		extent = %width SPC %height;
		cellSizeX = %width / 2;
		cellSizeY = 50;
		cellModeX = "fixed";
		cellModeY = "fixed";
		maxColCount = 2;
	};
	%form.addListener(%this);

	%half = %width / 2;

	%item = %form.addFormItem("First Frame", %half SPC 30);
	%this.startBox = %form.createTextEditItem(%item);

	%item = %form.addFormItem("Last Frame", %half SPC 30);
	%this.endBox = %form.createTextEditItem(%item);

	%item = %form.addFormItem("Step", %half SPC 30);
	%this.stepBox = %form.createTextEditItem(%item);

	%item = %form.addFormItem("Hold Each Frame", %half SPC 30);
	%this.holdBox = %form.createTextEditItem(%item);

	%item = %form.addFormItem("Ping-pong", %half SPC 30);
	%this.pingPongBox = %form.createCheckboxItem(%item);

	%item = %form.addFormItem("Mode", %half SPC 30);
	%this.modeDropDown = %form.createDropDownItem(%item);
	%this.modeDropDown.add("Append to the timeline", 0);
	%this.modeDropDown.add("Replace the timeline", 1);
	%this.modeDropDown.setSelected(0);

	%content.add(%form);

	// Every box re-asks the same question on every keystroke, so Apply is only
	// ever live when the numbers make sense and the line below always describes
	// what is about to happen.
	%command = %this.getId() @ ".validate();";
	%this.startBox.Command = %command;
	%this.endBox.Command = %command;
	%this.stepBox.Command = %command;
	%this.holdBox.Command = %command;
	%this.pingPongBox.Command = %command;
	%this.modeDropDown.Command = %command;

	%this.feedback = new GuiControl()
	{
		HorizSizing = "right";
		VertSizing = "bottom";
		Position = "12 168";
		Extent = (%width - 24) SPC 70;
		text = "";
		textWrap = true;
		textExtend = true;
	};
	ThemeManager.setProfile(%this.feedback, "infoProfile");
	%content.add(%this.feedback);

	%this.cancelButton = new GuiButtonCtrl()
	{
		HorizSizing = "right";
		VertSizing = "bottom";
		Position = (%width - 222) SPC (%height - 42);
		Extent = "100 30";
		Text = "Cancel";
		Command = %this.getID() @ ".onClose();";
	};
	ThemeManager.setProfile(%this.cancelButton, "buttonProfile");
	%content.add(%this.cancelButton);

	%this.applyButton = new GuiButtonCtrl()
	{
		HorizSizing = "right";
		VertSizing = "bottom";
		Position = (%width - 112) SPC (%height - 44);
		Extent = "100 34";
		Text = "Apply";
		Command = %this.getID() @ ".onApply();";
	};
	ThemeManager.setProfile(%this.applyButton, "primaryButtonProfile");
	%content.add(%this.applyButton);

	%this.startBox.setText(0);
	%this.endBox.setText(mGetMax(0, %this.imageFrameCount() - 1));
	%this.stepBox.setText(1);
	%this.holdBox.setText(1);

	%this.validate();
}

function AssetAnimationRangeDialog::imageFrameCount(%this)
{
	if(!isObject(%this.stage) || !isObject(%this.stage.imageAsset))
	{
		return 0;
	}

	return %this.stage.imageAsset.getFrameCount();
}

function AssetAnimationRangeDialog::mode(%this)
{
	return (%this.modeDropDown.getSelected() == 1) ? "replace" : "append";
}

function AssetAnimationRangeDialog::validate(%this)
{
	%range = AssetAdmin.frameRange;

	%start = %this.startBox.getText();
	%end = %this.endBox.getText();
	%step = %this.stepBox.getText();
	%hold = %this.holdBox.getText();
	%pingPong = %this.pingPongBox.getValue();

	%problem = %range.problemWith(%start, %end, %step, %hold, %pingPong, %this.imageFrameCount());
	if(%problem !$= "")
	{
		%this.applyButton.setActive(false);
		%this.feedback.setText(%problem);
		return false;
	}

	%frames = %range.build(%start, %end, %step, %hold, %pingPong);

	%this.applyButton.setActive(true);
	%this.feedback.setText(%range.describe(%frames, %this.mode(),
		%this.stage.timelinePane.strip.getCellCount()));

	return true;
}

function AssetAnimationRangeDialog::onApply(%this)
{
	if(!%this.validate())
	{
		return;
	}

	%frames = AssetAdmin.frameRange.build(%this.startBox.getText(), %this.endBox.getText(),
		%this.stepBox.getText(), %this.holdBox.getText(), %this.pingPongBox.getValue());

	if(%this.mode() $= "replace")
	{
		%this.stage.setFrames(%frames);
	}
	else
	{
		%this.stage.appendFrames(%frames);
	}

	%this.onClose();
}
