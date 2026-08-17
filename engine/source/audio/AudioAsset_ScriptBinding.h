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

//-----------------------------------------------------------------------------
// One method, and the shortness of this file is not an oversight.
//
// AudioAsset has never had console methods of its own. Its six fields are all
// registered with setters and getters, so script reads and writes them by name
// -- %asset.Volume, %asset.Looping -- and there is nothing a wrapper would add.
//
// The one thing a field cannot answer is the audio file in the form it is
// stored in. TypeAssetLooseFilePath keeps the expanded absolute path in memory
// and collapses it only on the way to a TAML file, so reading the field gives
// something machine-specific that no editor should put in a text box and no
// user should be asked to retype. collapseAssetFilePath is protected on
// AssetBase, so script cannot reach it without this. ImageAsset solved the same
// problem the same way (getRelativeImageFile).
//-----------------------------------------------------------------------------

ConsoleMethodGroupBeginWithDocs(AudioAsset, AssetBase)

//-----------------------------------------------------------------------------

/*! Gets the audio file as a path relative to the asset file.
    @return Returns the audio file relative to the asset file.
*/
ConsoleMethodWithDocs(AudioAsset, getRelativeAudioFile, ConsoleString, 2, 2, ())
{
    return object->getRelativeAudioFile();
}

//-----------------------------------------------------------------------------

ConsoleMethodGroupEndWithDocs(AudioAsset)
