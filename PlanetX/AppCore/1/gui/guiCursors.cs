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

/// The mouse cursors a GUI names by convention: a text field asks for EditCursor,
/// a window's edges for LeftRightCursor and friends, and a control with none of
/// its own gets DefaultCursor.
///
/// This file used to build a project's ~70 GUI profiles as well. Those are now a
/// GuiProfileTheme (see scripts/themes.cs), which derives the whole set from six
/// colors and is editable in the GUI Profile Editor - so a project skins itself
/// by editing a theme rather than by forking a thousand lines of script. Cursors
/// have not moved into the theme yet, so they stay here.

/// Registers %object under %name, or - if something already holds the name -
/// copies the new object's fields onto the existing one and throws the new one
/// away. That is how a project tunes an object the engine or another module
/// already made, GuiDefaultProfile being the standing example.
function AppCore::SafeCreateNamedObject(%this, %name, %object)
{
	if(isObject(%name))
	{
		%originalObject = nameToID(%name);
		if(%originalObject.getClassName() !$= %object.getClassName())
		{
			warn("Attempted to change the class of the named object " @ %name @ "!");
			warn("Original Class: " @ %originalObject.getClassName());
			warn("New Class: " @ %object.getClassName());
			return;
		}
		%originalObject.assignFieldsFrom(%object);
		%object.delete();
	}
	else
	{
		%object.setName(%name);
	}
}

function AppCore::createGuiCursors(%this)
{
	%this.SafeCreateNamedObject("DefaultCursor", new GuiCursor()
	{
	    hotSpot = "1 1";
	    renderOffset = "0 0";
	    bitmapName = "^AppCore/gui/images/cursors/defaultCursor";
	});

	%this.SafeCreateNamedObject("LeftRightCursor", new GuiCursor()
	{
	   hotSpot = "0.5 0";
	   renderOffset = "0.5 0.4";
	   bitmapName = "^AppCore/gui/images/cursors/leftRight";
	});

	%this.SafeCreateNamedObject("UpDownCursor", new GuiCursor()
	{
	   hotSpot = "1 1";
	   renderOffset = "0.5 0.5";
	   bitmapName = "^AppCore/gui/images/cursors/upDown";
	});

	%this.SafeCreateNamedObject("NWSECursor", new GuiCursor()
	{
	   hotSpot = "1 1";
	   renderOffset = "0.5 0.5";
	   bitmapName = "^AppCore/gui/images/cursors/NWSE";
	});

	%this.SafeCreateNamedObject("NESWCursor", new GuiCursor()
	{
	   hotSpot = "1 1";
	   renderOffset = "0.5 0.5";
	   bitmapName = "^AppCore/gui/images/cursors/NESW";
	});

	%this.SafeCreateNamedObject("MoveCursor", new GuiCursor()
	{
	   hotSpot = "1 1";
	   renderOffset = "0.5 0.5";
	   bitmapName = "^AppCore/gui/images/cursors/move";
	});

	%this.SafeCreateNamedObject("EditCursor", new GuiCursor()
	{
	   hotSpot = "0 0";
	   renderOffset = "0.5 0.5";
	   bitmapName = "^AppCore/gui/images/cursors/ibeam";
	});
}
