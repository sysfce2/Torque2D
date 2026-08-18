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

// We don't want tests in a shipping version.
#ifndef TORQUE_SHIPPING

#ifndef _UNIT_TESTING_H_
#include "testing/unitTesting.h"
#endif

#ifndef _DECLARED_ASSETS_H_
#include "assets/declaredAssets.h"
#endif

#ifndef _REFERENCED_ASSETS_H_
#include "assets/referencedAssets.h"
#endif

#ifndef _MODULE_DEFINITION_H_
#include "module/moduleDefinition.h"
#endif

//-----------------------------------------------------------------------------
// A declared path comes back out spelled the way it went in.
//
// The bug this pins down was found by making a project: BlankGame declares
// Path="sprites" and Path="fonts", and the copy written into a new project said
// Path="Sprites" and Path="Fonts". On Windows nobody notices. On Linux those
// directories do not exist, so an asset a person later puts in sprites/ is
// silently never scanned.
//
// Nothing in the path code was wrong. The field was TypeString, which interns
// without caseSens, and the string table's hash is case insensitive by
// construction -- so the first spelling of a name to reach the table becomes
// the spelling everyone gets. Whether a project came out right depended on what
// else the editor had loaded first, which is why it looked intermittent.
//
// These go through setDataField rather than the C++ setters on purpose. That is
// the same route TAML takes, so it exercises the console type that was actually
// changed rather than the setter beside it.
//
// Every spelling here is unique to its own test: the string table is a
// process-wide singleton with no way to remove an entry, so a shared name would
// make these depend on each other's order.
//-----------------------------------------------------------------------------

// Put a spelling in the table so the field has something to be folded into. In
// the wild this is another module, an editor script, or a directory scan --
// anything at all, which is the point.
static void poisonStringTable( const char* pSpelling )
{
    StringTable->insert( pSpelling );
}

TEST( DeclaredPathCaseTests, ADeclaredPathKeepsItsSpelling )
{
    poisonStringTable( "DpcSpritesOne" );

    DeclaredAssets declared;
    declared.setDataField( StringTable->insert( "Path" ), NULL, "dpcspritesone" );

    ASSERT_STREQ( declared.getPath(), "dpcspritesone" );

    SUCCEED();
}

TEST( DeclaredPathCaseTests, ADeclaredExtensionKeepsItsSpelling )
{
    poisonStringTable( "DpcAssetTamlTwo" );

    DeclaredAssets declared;
    declared.setDataField( StringTable->insert( "Extension" ), NULL, "dpcassettamltwo" );

    ASSERT_STREQ( declared.getExtension(), "dpcassettamltwo" );

    SUCCEED();
}

TEST( DeclaredPathCaseTests, AReferencedPathKeepsItsSpelling )
{
    poisonStringTable( "DpcSpritesThree" );

    ReferencedAssets referenced;
    referenced.setDataField( StringTable->insert( "Path" ), NULL, "dpcspritesthree" );

    ASSERT_STREQ( referenced.getPath(), "dpcspritesthree" );

    SUCCEED();
}

TEST( DeclaredPathCaseTests, AReferencedExtensionKeepsItsSpelling )
{
    poisonStringTable( "DpcAssetTamlFour" );

    ReferencedAssets referenced;
    referenced.setDataField( StringTable->insert( "Extension" ), NULL, "dpcassettamlfour" );

    ASSERT_STREQ( referenced.getExtension(), "dpcassettamlfour" );

    SUCCEED();
}

TEST( DeclaredPathCaseTests, AModuleScriptFileKeepsItsSpelling )
{
    poisonStringTable( "DpcGameFive.cs" );

    ModuleDefinition definition;
    definition.setDataField( StringTable->insert( "ScriptFile" ), NULL, "dpcgamefive.cs" );

    ASSERT_STREQ( definition.getScriptFile(), "dpcgamefive.cs" );

    SUCCEED();
}

TEST( DeclaredPathCaseTests, AModuleAssetTagsManifestKeepsItsSpelling )
{
    poisonStringTable( "DpcTagsSix.taml" );

    ModuleDefinition definition;
    definition.setDataField( StringTable->insert( "AssetTagsManifest" ), NULL, "dpctagssix.taml" );

    ASSERT_STREQ( definition.getAssetTagsManifest(), "dpctagssix.taml" );

    SUCCEED();
}

// THE BOUNDARY, asserted so it is a decision rather than an oversight.
//
// A module Id is an identifier, not a path. It IS compared as a string table
// pointer -- ModuleManager does it in nine places for load order, groups, types
// and dependency resolution -- and those comparisons have always been case
// insensitive, so a project depending on "AppCore=1" resolves against a module
// spelling itself "appCore". Making this case sensitive to match the paths above
// would quietly break that, and the breakage would look like a missing module
// rather than a spelling problem.
//
// So ModuleId stays TypeString, and this test is here to say that on purpose.
TEST( DeclaredPathCaseTests, AModuleIdIsStillFoldedBecauseItIsAnIdentifier )
{
    poisonStringTable( "DpcModuleSeven" );

    ModuleDefinition definition;
    definition.setDataField( StringTable->insert( "ModuleId" ), NULL, "dpcmoduleseven" );

    ASSERT_STREQ( definition.getModuleId(), "DpcModuleSeven" );

    SUCCEED();
}

#endif // TORQUE_SHIPPING
