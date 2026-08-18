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

ConsoleMethodGroupBeginWithDocs(GuiEditFrameStripCtrl, GuiControl)

/*! Sets the image asset whose frames the cells are drawn from.
    An empty id empties the grid.
    @param imageAssetId The asset id of the image.
    @return No return value.
*/
ConsoleMethodWithDocs(GuiEditFrameStripCtrl, setImageAsset, ConsoleVoid, 3, 3, (imageAssetId))
{
    object->setImageAssetId(argv[2]);
}

/*! Gets the image asset the cells are drawn from.
    @return The asset id, or an empty string when none is set.
*/
ConsoleMethodWithDocs(GuiEditFrameStripCtrl, getImageAsset, ConsoleString, 2, 2, ())
{
    return object->getImageAssetId();
}

/*! Gets how many frames the image asset offers.
    This is not the same as the number of cells: a timeline has as many cells as
    the animation has slots, which may repeat a frame or leave frames out.
    @return The frame count, or 0 when there is no usable image.
*/
ConsoleMethodWithDocs(GuiEditFrameStripCtrl, getImageFrameCount, ConsoleInt, 2, 2, ())
{
    return object->getImageFrameCount();
}

/*! Gets how many cells are laid out.
    @return The cell count.
*/
ConsoleMethodWithDocs(GuiEditFrameStripCtrl, getCellCount, ConsoleInt, 2, 2, ())
{
    return object->getCellCount();
}

/*! Gets which image frame a cell is showing.
    @param index The cell index.
    @return The image frame index, or -1 when the cell does not exist.
*/
ConsoleMethodWithDocs(GuiEditFrameStripCtrl, getFrameAt, ConsoleInt, 3, 3, (index))
{
    const S32 index = dAtoi(argv[2]);
    if (index < 0 || index >= object->getCellCount())
    {
        return -1;
    }

    return object->getFrameAt(index);
}

/*! Sets the pixel size of the square one frame draws in, clamped to what the
    control will show.
    @param cellSize The size in pixels.
    @return No return value.
*/
ConsoleMethodWithDocs(GuiEditFrameStripCtrl, setCellSize, ConsoleVoid, 3, 3, (cellSize))
{
    object->setCellSize(dAtoi(argv[2]));
}

/*! Gets the pixel size of the square one frame draws in.
    @return The size in pixels.
*/
ConsoleMethodWithDocs(GuiEditFrameStripCtrl, getCellSize, ConsoleInt, 2, 2, ())
{
    return object->getCellSize();
}

/*! Gets where a cell is on the canvas.
    For a test that must not hard-code a point: a stale coordinate reports a
    missing cell, which is exactly what a broken hit test reports.
    @param index The cell index.
    @return The rect as "x y width height" in global coordinates, or "0 0 0 0".
*/
ConsoleMethodWithDocs(GuiEditFrameStripCtrl, getCellRect, ConsoleString, 3, 3, (index))
{
    const RectI cellRect = object->getCellRectGlobal(dAtoi(argv[2]));

    char* buffer = Con::getReturnBuffer(64);
    dSprintf(buffer, 64, "%d %d %d %d", cellRect.point.x, cellRect.point.y, cellRect.extent.x, cellRect.extent.y);
    return buffer;
}

/*! Gets which cell a point on the canvas falls on.
    @param x The global x coordinate.
    @param y The global y coordinate.
    @return The cell index, or -1 for the gaps between cells and anywhere off them.
*/
ConsoleMethodWithDocs(GuiEditFrameStripCtrl, getCellAtPoint, ConsoleInt, 3, 4, (x, y))
{
    Point2I point(0, 0);
    if (argc == 3)
    {
        dSscanf(argv[2], "%d %d", &point.x, &point.y);
    }
    else
    {
        point.set(dAtoi(argv[2]), dAtoi(argv[3]));
    }

    return object->cellAtGlobal(point);
}

ConsoleMethodGroupEndWithDocs(GuiEditFrameStripCtrl)
