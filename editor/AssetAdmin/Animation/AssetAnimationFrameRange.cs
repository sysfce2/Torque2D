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
// Turning "the death animation is frames 28 to 32" into a list of frames.
//
// Kept apart from the dialog that asks for the numbers, because it is the part
// worth checking: the dialog shows the answer back to the user before they
// commit to it, and the test asks for the same answer without opening anything.
//
// Nothing here touches an asset or a control.
//-----------------------------------------------------------------------------

// A guard against a fat-fingered hold. AnimationAsset::getAnimationFrames formats
// into a fixed 4096-byte buffer, so the ENGINE quietly truncates somewhere near a
// thousand frames; this stops well short of that.
$AssetAnimationFrameRange::maxFrames = 512;

//-----------------------------------------------------------------------------
// Three stages, and the order of them is the whole design.
//
//   1. the stepped run from start to end, counting down when end is the smaller
//   2. if ping-pong, the reverse of that MINUS both shared end frames
//   3. then every element repeated %hold times
//
// Hold goes last so that "shared end frames" means one frame rather than N slots.
// Dropping those two is what stops the turn at each end lasting twice as long as
// every other frame, which reads as a stutter.
//-----------------------------------------------------------------------------

function AssetAnimationFrameRange::build(%this, %start, %end, %step, %hold, %pingPong)
{
	%start = mFloor(%start);
	%end = mFloor(%end);
	%step = mGetMax(1, mFloor(%step));
	%hold = mGetMax(1, mFloor(%hold));

	%forward = "";
	%direction = (%end >= %start) ? %step : -%step;

	for(%frame = %start; (%direction > 0) ? (%frame <= %end) : (%frame >= %end); %frame += %direction)
	{
		%forward = (%forward $= "") ? %frame : (%forward SPC %frame);
	}

	%list = %forward;

	if(%pingPong)
	{
		// From the second-to-last back to the second: both ends are already in the
		// run and playing them twice is what makes a ping-pong stutter.
		%count = getWordCount(%forward);
		for(%i = %count - 2; %i >= 1; %i--)
		{
			%list = %list SPC getWord(%forward, %i);
		}
	}

	if(%hold > 1)
	{
		%held = "";
		%count = getWordCount(%list);
		for(%i = 0; %i < %count; %i++)
		{
			%frame = getWord(%list, %i);
			for(%r = 0; %r < %hold; %r++)
			{
				%held = (%held $= "") ? %frame : (%held SPC %frame);
			}
		}
		%list = %held;
	}

	return %list;
}

// Why a given set of numbers cannot be used, or "" when it can.
//
// Takes the image's frame count so the message can name it: counting from one is
// the mistake people actually make, and "0 to 99" says more than "out of range".
function AssetAnimationFrameRange::problemWith(%this, %start, %end, %step, %hold, %pingPong, %imageFrameCount)
{
	if(%start $= "" || %end $= "")
	{
		return "Give a first and last frame.";
	}

	if(%start < 0 || %end < 0)
	{
		return "Frames start at 0.";
	}

	if(%imageFrameCount > 0 && (%start >= %imageFrameCount || %end >= %imageFrameCount))
	{
		return "This image has" SPC %imageFrameCount SPC "frames, numbered 0 to" SPC (%imageFrameCount - 1) @ ".";
	}

	if(%step < 1)
	{
		return "A step of less than 1 would never get there.";
	}

	if(%hold < 1)
	{
		return "Every frame has to be held at least once.";
	}

	%count = getWordCount(%this.build(%start, %end, %step, %hold, %pingPong));
	if(%count > $AssetAnimationFrameRange::maxFrames)
	{
		return "That would make" SPC %count SPC "frames, and" SPC
			$AssetAnimationFrameRange::maxFrames SPC "is the most an animation can hold here.";
	}

	return "";
}

// What the user is about to get, in words. The strongest argument for keeping the
// builder callable without a dialog: this line is the builder's own answer read
// back, not a second description of it that could drift.
function AssetAnimationFrameRange::describe(%this, %frames, %mode, %existingCount)
{
	%count = getWordCount(%frames);
	if(%count == 0)
	{
		return "";
	}

	%shown = %frames;
	if(%count > 12)
	{
		%shown = "";
		for(%i = 0; %i < 10; %i++)
		{
			%shown = (%shown $= "") ? getWord(%frames, %i) : (%shown SPC getWord(%frames, %i));
		}
		%shown = %shown SPC "..." SPC getWord(%frames, %count - 1);
	}

	%tail = (%mode $= "append")
		? ("appended to the" SPC %existingCount SPC ((%existingCount == 1) ? "already there" : "already there"))
		: "replacing what is there";

	return %shown SPC "-" SPC %count SPC ((%count == 1) ? "frame," : "frames,") SPC %tail @ ".";
}
