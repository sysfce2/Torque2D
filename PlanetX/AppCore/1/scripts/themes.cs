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

/// The project's GUI skin. A GuiProfileTheme derives a complete set of control
/// profiles from six colors, three fonts and a border size, and the GUI Profile
/// Editor writes one .taml per theme into <project>/themes. This is where a
/// running game reads them back, so what the editor stamped into a .gui is what
/// renders.
///
/// The folder sits beside AppCore rather than inside it because the themes are
/// the project's, while AppCore is boilerplate the project started from and
/// replaces wholesale when it updates.

/// The face a generated theme starts on. A theme normally names its own face and
/// ships baked glyph caches beside it, which is what makes it render the same
/// everywhere; a theme built here has no caches of its own, so it asks for
/// something the platform can actually supply.
function AppCore::SetProfileFont(%this)
{
	if ($platform $= "windows")
		%this.platformFontType = "share tech mono";
	else if ($platform $= "Android")
		// "Droid" is gone from modern Android (Roboto since ~2014); request "Roboto",
		// which is the system face AND the bundled assets/fonts/Roboto-Regular.ttf.
		%this.platformFontType = "Roboto";
	else if ($platformUnixType $= "emscripten")
		// Web build: the browser has no system fonts and there's no font backend,
		// so use a face that ships a pre-baked .uft glyph cache ("share tech mono"
		// at sizes 12/14/16/18/24 under ^AppCore/fonts). $platform is "x86UNIX" on
		// the web build (same as desktop Linux), so key off $platformUnixType.
		%this.platformFontType = "share tech mono";
	else
		%this.platformFontType = "monaco";
}

function AppCore::getThemesPath(%this)
{
	%module = ModuleDatabase.findModule("AppCore", 1);
	if(!isObject(%module))
	{
		return "";
	}

	// <project>/AppCore/<version> -> <project>/themes. The same derivation
	// $Gui::fontCacheDirectory uses (scripts/defaultPreferences.cs).
	return pathConcat(filePath(filePath(makeFullPath(%module.getModulePath(), getMainDotCsDir()))), "themes");
}

function AppCore::loadThemes(%this)
{
	%path = %this.getThemesPath();
	if(%path $= "")
	{
		warn("AppCore::loadThemes: could not locate the AppCore module, so the project's themes were not loaded.");
		return;
	}
	createPath(%path @ "/");

	%found = false;
	%pattern = %path @ "/*.taml";
	for(%file = findFirstFile(%pattern); %file !$= ""; %file = findNextFile(%pattern))
	{
		if(%this.loadTheme(%file))
		{
			%found = true;
		}
	}

	// A project that has never had a theme - one made before themes existed, or
	// a folder someone emptied. Give it the stock one rather than leaving every
	// control on the engine's bare fallback profile.
	if(!%found)
	{
		%this.createStockTheme(%path);
	}
}

/// Reads one file from the themes folder. Alongside themes it may hold stand-alone
/// profiles, which the Profile Editor writes as a one-profile bundle (and, from
/// older versions, as a bare profile). Loading is all these need: a profile puts
/// itself in the Gui data group under its own name, which is how a Gui finds it.
function AppCore::loadTheme(%this, %file)
{
	%object = TAMLRead(%file);
	if(!isObject(%object))
	{
		warn("AppCore::loadThemes: could not read " @ %file);
		return false;
	}

	%class = %object.getClassName();
	if(%class $= "GuiProfileTheme")
	{
		if(%object.getName() $= "")
		{
			warn("AppCore::loadThemes: skipping " @ %file @ " - the theme has no name (possibly a name collision).");
			%object.delete();
			return false;
		}

		%this.repairFontDirectory(%object);
		return true;
	}

	if(%class $= "ScriptGroup" || %class $= "GuiControlProfile")
	{
		return false;
	}

	warn("AppCore::loadThemes: skipping " @ %file @ " - not a theme or profile.");
	%object.delete();
	return false;
}

/// A project keeps every font cache in one folder beside its themes, so a theme
/// naming any other folder is a theme that came from somewhere else - shipped
/// with the library, or copied in from another project. Point it at this
/// project's folder, in memory only: nothing is rewritten on disk, and the theme
/// restamps its members so their fontDirectory follows.
function AppCore::repairFontDirectory(%this, %theme)
{
	%directory = makeRelativePath(pathConcat(%this.getThemesPath(), "fonts"), getMainDotCsDir());
	if(%theme.fontDirectory !$= %directory)
	{
		%theme.fontDirectory = %directory;
	}
}

/// Builds the stock theme in code and writes it out, so the project ends up with
/// the same editable file a new project is shipped. The palette is the one
/// AppCore's hand-written profiles used for years; the face comes from the
/// platform, since a generated theme has no baked font caches of its own to name.
function AppCore::createStockTheme(%this, %path)
{
	%this.SetProfileFont();

	%theme = new GuiProfileTheme("Base")
	{
		colorBackground = "43 43 43 255";
		colorSurface = "81 92 102 255";
		colorForeground = "224 224 224 255";
		colorAccent = "54 135 196 255";
		colorHighlight = "245 210 50 255";
		colorWarning = "196 54 71 255";

		fontBody = %this.platformFontType;
		fontTitle = %this.platformFontType;
		fontCode = %this.platformFontType;
		fontDirectory = makeRelativePath(pathConcat(%path, "fonts"), getMainDotCsDir());
		fontSize = 16;

		borderSize = 1;
	};

	%file = pathConcat(%path, "Base.taml");
	TAMLWrite(%theme, %file);

	echo("AppCore: no theme found, so the stock theme was written to " @ %file @ ".");
	return %theme;
}
