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

ConsoleMethodGroupBeginWithDocs(AssetBase, SimObject)

/*! Refresh the asset.
    This marks the asset as having unsaved changes and tells everything watching it
    that it changed.  It does NOT write the asset's file -- use saveAsset for that.
    @return No return value.
*/
ConsoleMethodWithDocs( AssetBase, refreshAsset, ConsoleVoid, 2, 2, ())
{
    object->refreshAsset();
}

//-----------------------------------------------------------------------------

/*! Gets the assets' Asset Id.  This is only available if the asset was acquired from the asset manager.
    @return The assets' Asset Id.
*/
ConsoleMethodWithDocs( AssetBase, getAssetId, ConsoleString, 2, 2, ())
{
    return object->getAssetId();
}

//-----------------------------------------------------------------------------

/*! Whether the asset has changes that have not been saved.
    @return Whether the asset has unsaved changes or not.
*/
ConsoleMethodWithDocs( AssetBase, isAssetDirty, ConsoleBool, 2, 2, ())
{
    return object->getAssetDirty();
}

//-----------------------------------------------------------------------------

/*! Write the asset to its file.
    @return Whether the save was successful or not.
*/
ConsoleMethodWithDocs( AssetBase, saveAsset, ConsoleBool, 2, 2, ())
{
    return object->saveAsset();
}

//-----------------------------------------------------------------------------

/*! Discard the asset's unsaved changes and reload it from its file.
    @return Whether the revert was successful or not.
*/
ConsoleMethodWithDocs( AssetBase, revertAsset, ConsoleBool, 2, 2, ())
{
    return object->revertAsset();
}

//-----------------------------------------------------------------------------

/*! Takes a detached copy of everything the asset currently holds.
    The copy is a registered object that the caller owns and must delete.  Taking one
    changes nothing about this asset: it is not marked as changed and nobody is notified.
    @return The snapshot object's Id, or 0 if the snapshot could not be taken.
*/
ConsoleMethodWithDocs( AssetBase, createStateSnapshot, ConsoleInt, 2, 2, ())
{
    AssetBase* pSnapshot = object->createStateSnapshot();

    return pSnapshot == NULL ? 0 : pSnapshot->getId();
}

//-----------------------------------------------------------------------------

/*! Puts a snapshot taken by createStateSnapshot back onto this asset.
    The asset object itself is kept, so anything already using the asset keeps working.
    This does not mark the asset as changed -- the caller decides what the asset's
    unsaved state should be afterwards.
    @param snapshot The snapshot object to restore.
    @return Whether the restore was successful or not.
*/
ConsoleMethodWithDocs( AssetBase, restoreStateSnapshot, ConsoleBool, 3, 3, (snapshot))
{
    AssetBase* pSnapshot = Sim::findObject<AssetBase>( argv[2] );

    if ( pSnapshot == NULL )
    {
        // Warn.
        Con::warnf( "AssetBase::restoreStateSnapshot() - Could not find the snapshot object '%s'.", argv[2] );
        return false;
    }

    return object->restoreStateSnapshot( pSnapshot );
}

ConsoleMethodGroupEndWithDocs(AssetBase)
