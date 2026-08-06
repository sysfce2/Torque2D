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

function KeyboardToy::create( %this )
{
    // Keep hold of what we make. The Sandbox owns the controls once they are
    // added, so the toy cannot assume they are still there when it is torn
    // down, and a held id does not depend on the name being registered.
    %this.mainDlg = TamlRead("./MainGameDlg.gui.taml");
    Sandbox.add( %this.mainDlg );

    %this.changeUsernameDlg = TamlRead("./ChangeUsernameDlg.gui.taml");
    Sandbox.add( %this.changeUsernameDlg );

    // Reset the toy.
    %this.reset();
}


//-----------------------------------------------------------------------------

function KeyboardToy::destroy( %this )
{
    // The Sandbox may have taken these down with it already, so delete only
    // what is still standing.
    if ( isObject(%this.mainDlg) )
        %this.mainDlg.delete();

    if ( isObject(%this.changeUsernameDlg) )
        %this.changeUsernameDlg.delete();
}

//-----------------------------------------------------------------------------

function KeyboardToy::reset( %this )
{
    // Clear the scene.
    SandboxScene.clear();

    Canvas.pushDialog(%this.mainDlg);
}
//-----------------------------------------------------------------------------
