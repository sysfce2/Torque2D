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

/// Game
$Game::CompanyName              = "Torque Game Engines";
$Game::ProductName              = "Torque2D";

/// iOS
$pref::iOS::ScreenOrientation   = $iOS::constant::Landscape;
$pref::iOS::ScreenDepth		    = 32;
$pref::iOS::UseGameKit          = 0;
$pref::iOS::UseMusic            = 0;
$pref::iOS::UseMoviePlayer      = 0;
$pref::iOS::UseAutoRotate       = 1;
$pref::iOS::EnableOrientationRotation = 1;
$pref::iOS::EnableOtherOrientationRotation = 1;
$pref::iOS::StatusBarType       = 0;

/// AppCore. Which theme's cursors are installed under the names the engine
/// looks up when a control names none of its own - DefaultCursor, EditCursor
/// and the rest (see gui/guiCursors.cs). Empty is the usual answer: a project
/// with one theme uses it, and one with several is asked to say which.
$pref::AppCore::cursorTheme = "";

/// T2D
$pref::T2D::ParticlePlayerEmissionRateScale = 1.0;
$pref::T2D::ParticlePlayerSizeScale = 1.0;
$pref::T2D::ParticlePlayerForceScale = 1.0;
$pref::T2D::ParticlePlayerTimeScale = 1.0;
$pref::T2D::warnFileDeprecated = 1;
$pref::T2D::warnSceneOccupancy = 1;
$pref::T2D::imageAssetGlobalFilterMode = Bilinear;
$pref::T2D::TAMLSchema="";
$pref::T2D::JSONStrict = 1;

/// Video
$pref::Video::appliedPref = 0;
$pref::Video::displayDevice = "OpenGL";
$pref::Video::preferOpenGL = 1;
$pref::Video::fullScreen = 0;
$pref::Video::defaultResolution = "1024 768";
$pref::Video::windowedRes = "1024 768 32";
$pref::OpenGL::gammaCorrection = 0.5;

/// Fonts. The project's one font-cache folder, shared by every theme it holds --
/// a cache is keyed by face and size alone, so a second location could only hold
/// a duplicate of what the first already has. This is where the GUI Profile
/// Editor bakes the caches for the fonts a theme uses, and what a profile naming
/// no directory of its own falls back to. It sits beside the themes rather than
/// inside this module, because the themes are the project's while AppCore is
/// boilerplate a project starts from. (AppCore's own legacy profiles in
/// gui/guiProfiles.cs name ^AppCore/fonts explicitly -- that is where their
/// bundled caches ship, and they are unaffected by this.)
///
/// Derived from where this module sits (<project>/AppCore/<version>), the same
/// way the editor finds the themes folder, so renaming or moving the project
/// keeps working. Expanded, not left as a ^AppCore expando: the resource manager
/// does not resolve expandos for font cache lookups.
%appCoreModule = ModuleDatabase.findModule( "AppCore", 1 );
$Gui::fontCacheDirectory = isObject( %appCoreModule ) ?
	pathConcat( filePath( filePath( makeFullPath( %appCoreModule.getModulePath(), getMainDotCsDir() ) ) ), "themes/fonts" ) :
	expandPath( "^AppCore/fonts" );

/// Generic fallback font (a .ttf rasterized by FreeType) used by platforms that
/// have no system fonts to synthesize a missing face/size from -- currently the
/// web (Emscripten) build. Kept inside AppCore so a shipped game is self-contained
/// (nothing here depends on the removable editor/). The editor registers its OWN
/// copy of this var when it loads (editor/EditorCore/scripts/defaultPreferences.cs).
$pref::Web::fallbackFont = expandPath( "^AppCore/fonts/Roboto-Regular.ttf" );
