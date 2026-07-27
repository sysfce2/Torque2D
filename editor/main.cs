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

new ModuleManager(EditorManager);
EditorManager.addListener(AssetDatabase);
EditorManager.EchoInfo = false;

// Scans for the modules that make up the editors.
EditorManager.scanModules( "./" );

// Load various editors
EditorManager.LoadExplicit("EditorConsole");
Editormanager.LoadExplicit("ProjectManager");
EditorManager.LoadExplicit("AssetAdmin");
EditorManager.LoadExplicit("GuiEditor");

if(!isObject(AppCore))
{
	EditorCore.open();
	EditorCore.showProjectSelector();
}

//-----------------------------------------------------------------------------

// The engine calls onPreExit ahead of onExit (see shutdownGame in
// game/defaultGame.cc), while everything the editor owns is still alive.
//
// Unload exactly what was loaded above, in reverse. ModuleManager resolves each
// module's dependencies and calls the DestroyFunctions in reverse order, so every
// editor module gets to clean up after itself, and EditorCore -- which the four
// above pull in as a shared dependency rather than loading themselves -- unloads
// last, when its load count finally drops to zero.
//
// This lives beside the loads, in the editor's own boot script, so the two lists
// stay in step and any project shipping its own main.cs inherits the teardown.
function onPreExit()
{
	if(isObject(EditorManager))
	{
		EditorManager.unloadExplicit("GuiEditor");
		EditorManager.unloadExplicit("AssetAdmin");
		EditorManager.unloadExplicit("ProjectManager");
		EditorManager.unloadExplicit("EditorConsole");
	}
}
