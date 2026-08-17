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
	// addItem, not add. GuiControl::add is what was being called here -- it takes a
	// CONTROL and puts it inside this one -- so the list stayed empty, the box
	// read "none", and the second choice could not be picked at all.
	%this.modeDropDown = %form.createDropDownItem(%item);
	%this.modeDropDown.addItem("Append to the timeline");
	%this.modeDropDown.addItem("Replace the timeline");

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

	// Below whatever the form actually came out as, rather than below a number
	// written here: the grid decides its own height from how many rows six items
	// make, and a seventh field would silently land underneath this.
	%formBottom = getWord(%form.getPosition(), 1) + getWord(%form.getExtent(), 1);

	// The answer line. textExtend grows it downward for a long answer, which is
	// why the buttons sit well clear of where it starts rather than just under it.
	%this.feedback = new GuiControl()
	{
		HorizSizing = "width";
		VertSizing = "anchorTop";
		Position = "12" SPC (%formBottom + 8);
		Extent = (%width - 24) SPC 90;
		text = "";
		textWrap = true;
		textExtend = true;
	};
	ThemeManager.setProfile(%this.feedback, "infoProfile");
	%content.add(%this.feedback);

	// Measured from the room the content actually has, not from the dialog's own
	// height -- the title bar and border take 34 of it, and buttons placed
	// without allowing for that fall off the bottom.
	%bottom = %this.contentHeight() - 12;

	%this.cancelButton = new GuiButtonCtrl()
	{
		HorizSizing = "anchorRight";
		VertSizing = "anchorBottom";
		Position = (%width - 222) SPC (%bottom - 32);
		Extent = "100 30";
		Text = "Cancel";
		Command = %this.getID() @ ".onClose();";
	};
	ThemeManager.setProfile(%this.cancelButton, "buttonProfile");
	%content.add(%this.cancelButton);

	%this.applyButton = new GuiButtonCtrl()
	{
		HorizSizing = "anchorRight";
		VertSizing = "anchorBottom";
		Position = (%width - 112) SPC (%bottom - 34);
		Extent = "100 34";
		Text = "Apply";
		Command = %this.getID() @ ".onApply();";
	};
	ThemeManager.setProfile(%this.applyButton, "primaryButtonProfile");
	%content.add(%this.applyButton);

	// Down here with the rest of the starting values, and not beside the add()s
	// that fill the list, because a drop down that has not been added to anything
	// yet is not awake -- and the selection made on it then does not stick. The
	// list showed "none" until the user opened it.
	%this.modeDropDown.setSelected(0);

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

// getSelectedItem, not getSelected: the latter is not a method on a drop down at
// all, so this always answered "append" and Replace was unreachable.
function AssetAnimationRangeDialog::mode(%this)
{
	return (%this.modeDropDown.getSelectedItem() == 1) ? "replace" : "append";
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
