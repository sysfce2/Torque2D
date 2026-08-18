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

/// The mouse cursors a GUI names by convention: a text field asks for
/// EditCursor, a window's edges for LeftRightCursor and friends, and a control
/// with none of its own gets DefaultCursor. Those names are hard-coded in the
/// engine (guiTextEditCtrl.cc, guiWindowCtrl.cc, guiFrameSetCtrl.cc,
/// guiEditCtrl.cc), so something has to answer to them.
///
/// This file used to answer by building seven cursors out of literals, the last
/// of the hand-written GUI furniture after the ~70 profiles became a
/// GuiProfileTheme. Now the theme owns cursors too - each one its own art,
/// tinted from the theme's palette - and this installs a chosen theme's set
/// under the canonical names. A control that names a cursor outright still wins;
/// this is only what everything else falls back to, including the canvas arrow.
///
/// It is also callable at any time, which is how a game swaps between themes
/// that look nothing alike:
///
///     AppCore.installThemeCursors(Combat);
///     Canvas.setCursor(DefaultCursor);

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

/// Every theme the project loaded, as a space-separated list of ids. They live
/// in the Gui data group, which is where GuiProfileTheme::onAdd puts them.
function AppCore::getThemes(%this)
{
	%themes = "";
	if(!isObject(GuiDataGroup))
	{
		return %themes;
	}

	for(%i = 0; %i < GuiDataGroup.getCount(); %i++)
	{
		%object = GuiDataGroup.getObject(%i);
		if(%object.getClassName() $= "GuiProfileTheme")
		{
			%themes = (%themes $= "") ? %object.getId() : (%themes SPC %object.getId());
		}
	}

	return %themes;
}

/// Which theme's cursors become the canonical ones. A project with one theme
/// never has to think about this; a project with several says so by setting
/// $pref::AppCore::cursorTheme, and gets told when it hasn't.
function AppCore::cursorTheme(%this)
{
	%themes = %this.getThemes();
	%count = getWordCount(%themes);
	if(%count == 0)
	{
		return 0;
	}

	if($pref::AppCore::cursorTheme !$= "")
	{
		for(%i = 0; %i < %count; %i++)
		{
			%theme = getWord(%themes, %i);
			if(%theme.getName() $= $pref::AppCore::cursorTheme)
			{
				return %theme;
			}
		}
		warn("AppCore::cursorTheme: $pref::AppCore::cursorTheme names '" @ $pref::AppCore::cursorTheme @ "', which is not a loaded theme.");
	}

	if(%count == 1)
	{
		return getWord(%themes, 0);
	}

	for(%i = 0; %i < %count; %i++)
	{
		%theme = getWord(%themes, %i);
		if(%theme.getName() $= "Base")
		{
			return %theme;
		}
	}

	%first = getWord(%themes, 0);
	warn("AppCore::cursorTheme: " @ %count @ " themes are loaded and none is named 'Base', so the cursors come from '" @
		%first.getName() @ "'. Set $pref::AppCore::cursorTheme to choose.");
	return %first;
}

/// Point the canonical cursor names at %theme's cursors. The names are copies
/// rather than the members themselves: a name can only belong to one object,
/// and a theme's members have to keep their own names for the Guis that
/// reference them.
function AppCore::installThemeCursors(%this, %theme)
{
	if(!isObject(%theme))
	{
		warn("AppCore::installThemeCursors: no theme to install cursors from.");
		return false;
	}

	%categories = %theme.getCursorCategoryNames();
	%count = getWordCount(%categories);
	for(%i = 0; %i < %count; %i++)
	{
		%category = getWord(%categories, %i);
		%cursor = %theme.getCursor(%category);
		if(!isObject(%cursor))
		{
			continue;
		}

		// The category's canonical name comes from the engine's own table, so
		// this never has to take a theme name apart to find it.
		%this.SafeCreateNamedObject(%theme.getCursorCanonicalName(%category), new GuiCursor()
		{
			bitmapName = %cursor.bitmapName;
			hotSpot = %cursor.hotSpot;
			renderOffset = %cursor.renderOffset;
			color = %cursor.color;
		});
	}

	return true;
}
