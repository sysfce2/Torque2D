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

#ifndef _DECLARED_ASSETS_H_
#include "assets/declaredAssets.h"
#endif

#ifndef _CONSOLETYPES_H_
#include "console/consoleTypes.h"
#endif

//-----------------------------------------------------------------------------

IMPLEMENT_CONOBJECT( DeclaredAssets );

//-----------------------------------------------------------------------------

void DeclaredAssets::initPersistFields()
{
    // Call Parent.
    Parent::initPersistFields();
        
    // TypeCaseString, not TypeString: both of these are read off a case
    // sensitive filesystem and written back to it. TypeString interns without
    // caseSens, and the string table's hash is case insensitive, so whichever
    // spelling of a name reached the table first is the spelling that comes
    // back -- a module declaring "sprites" gets "Sprites" written into its
    // module.taml the moment anything else in the process has interned that.
    // Safe to make case sensitive here because neither value is ever compared
    // as a StringTableEntry: getPath only ever feeds a dSprintf, and
    // getExtension is matched with dStricmp inside the scan.
    addField("Path", TypeCaseString, Offset(mPath, DeclaredAssets), "" );
    addField("Extension", TypeCaseString, Offset(mExtension, DeclaredAssets), "" );
    addField("Recurse", TypeBool, Offset(mRecurse, DeclaredAssets), "" );
}
