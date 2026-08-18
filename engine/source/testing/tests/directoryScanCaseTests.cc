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

#ifndef _PLATFORM_H_
#include "platform/platform.h"
#endif

#ifndef _PLATFORM_FILEIO_H_
#include "platform/platformFileIO.h"
#endif

#ifndef _STRINGTABLE_H_
#include "string/stringTable.h"
#endif

// platform.h only forward declares Vector, and both scans return one.
#ifndef _VECTOR_H_
#include "collection/vector.h"
#endif

//-----------------------------------------------------------------------------
// A directory scan reports the names that are actually on disk.
//
// This is the half of the string table case problem that cannot be fixed by
// choosing a better field type, because the strings do not come from a taml
// file -- they come from readdir. The string table's hash is case insensitive
// by construction, so an ordinary insert of a name read off the filesystem
// returns whichever spelling of it reached the table first, and several arrive
// during static initialisation before any of this runs: SpriteBatch interns
// "Sprites" as a taml node name and guiProfileTheme interns "Fonts" as a field
// group. The result on a case sensitive filesystem was that a directory
// genuinely called sprites or fonts was enumerated as "Sprites" or "Fonts", and
// every caller that then tried to open it failed -- deleteDirectory could not
// recurse into it, and getDirectoryList named something nothing could stat.
//
// The names below are deliberately the real ones. A test using invented
// spellings would pass whether or not the words that actually collide are
// handled, and those words are the whole point.
//-----------------------------------------------------------------------------

#define SCANCASE_ROOT   "_unitTestScanCase_RemoveMe"

static void scanCasePath( char* buffer, U32 bufferSize, const char* relative )
{
    if ( relative == NULL )
    {
        dSprintf( buffer, bufferSize, "%s/%s",
            Platform::getCurrentDirectory(), SCANCASE_ROOT );
        return;
    }

    dSprintf( buffer, bufferSize, "%s/%s/%s",
        Platform::getCurrentDirectory(), SCANCASE_ROOT, relative );
}

static bool scanCaseWriteFile( const char* path )
{
    Platform::createPath( path );

    File file;
    if ( file.open( path, File::Write ) != File::Ok )
        return false;

    U32 written = 0;
    const bool ok = file.write( 2, "hi", &written ) == File::Ok;
    file.close();
    return ok;
}

// Was any of the names the scan returned spelled this way?
static bool scanFound( Vector<StringTableEntry>& names, const char* spelling )
{
    for ( S32 i = 0; i < names.size(); i++ )
    {
        if ( dStrcmp( names[i], spelling ) == 0 )
            return true;
    }

    return false;
}

static bool scanFoundFile( Vector<Platform::FileInfo>& files, const char* spelling )
{
    for ( S32 i = 0; i < files.size(); i++ )
    {
        if ( dStrcmp( files[i].pFileName, spelling ) == 0 )
            return true;
    }

    return false;
}

TEST( DirectoryScanCaseTests, AScanReportsTheSpellingOnDisk )
{
    char root[1024];
    scanCasePath( root, sizeof( root ), NULL );

    if ( Platform::isDirectory( root ) )
        Platform::deleteDirectory( root );

    // Lower case on disk, and every one of these words is already in the string
    // table capitalised by the time any of this runs.
    char file[1024];
    scanCasePath( file, sizeof( file ), "sprites/readme.md" );
    ASSERT_TRUE( scanCaseWriteFile( file ) ) << "Could not write the scratch file.";
    scanCasePath( file, sizeof( file ), "fonts/readme.md" );
    ASSERT_TRUE( scanCaseWriteFile( file ) ) << "Could not write the scratch file.";

    // Belt and braces: make sure the capitalised spellings really are in the
    // table, so this test cannot pass by the collision simply not existing.
    StringTable->insert( "Sprites" );
    StringTable->insert( "Fonts" );
    StringTable->insert( "README.md" );

    // With a trailing separator, exactly as the getDirectoryList binding calls
    // it. Without one the back-end returns its children as "/sprites" rather
    // than "sprites", which is a separate quirk and not what this is about.
    char rootSlash[1024];
    dSprintf( rootSlash, sizeof( rootSlash ), "%s/", root );

    Vector<StringTableEntry> directories;
    ASSERT_TRUE( Platform::dumpDirectories( rootSlash, directories, 0, true ) ) << "Could not scan the scratch root.";

    ASSERT_TRUE( scanFound( directories, "sprites" ) ) << "sprites came back spelled some other way.";
    ASSERT_TRUE( scanFound( directories, "fonts" ) ) << "fonts came back spelled some other way.";
    ASSERT_FALSE( scanFound( directories, "Sprites" ) ) << "A directory was reported under a spelling that is not on disk.";
    ASSERT_FALSE( scanFound( directories, "Fonts" ) ) << "A directory was reported under a spelling that is not on disk.";

    // And the file names inside them.
    char fontsDir[1024];
    scanCasePath( fontsDir, sizeof( fontsDir ), "fonts" );

    Vector<Platform::FileInfo> files;
    ASSERT_TRUE( Platform::dumpPath( fontsDir, files, 0 ) ) << "Could not scan the scratch directory.";

    ASSERT_TRUE( scanFoundFile( files, "readme.md" ) ) << "readme.md came back spelled some other way.";
    ASSERT_FALSE( scanFoundFile( files, "README.md" ) ) << "A file was reported under a spelling that is not on disk.";

    // What all of the above is really about: a caller can open what the scan
    // named. This is the step that failed in the wild -- deleteDirectory
    // recursing into a child it had just been told about.
    ASSERT_TRUE( Platform::deleteDirectory( root ) ) << "Could not delete a tree the scan had just enumerated.";
    ASSERT_FALSE( Platform::isDirectory( root ) ) << "The tree is still there.";

    SUCCEED();
}

#endif // TORQUE_SHIPPING
