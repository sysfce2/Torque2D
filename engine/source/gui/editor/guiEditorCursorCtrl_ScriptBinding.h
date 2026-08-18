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

ConsoleMethodGroupBeginWithDocs(GuiEditorCursorCtrl, GuiControl)

/*! Gets the size of the cursor art, or "0 0" when there is none to show.
    @return The image size as "width height".
*/
ConsoleMethodWithDocs(GuiEditorCursorCtrl, getImageExtent, ConsoleString, 2, 2, ())
{
    char* buffer = Con::getReturnBuffer(32);
    const Point2I extent = object->getImageExtent();
    dSprintf(buffer, 32, "%d %d", extent.x, extent.y);
    return buffer;
}

/*! Gets the pixel of the art the pointer actually lands on: the renderOffset
    anchor floored to a pixel, plus the hotSpot nudge. This is the number a
    readout should show, because neither field alone answers "where does it
    point".
    @return The pixel as "x y", or "0 0" when there is no art.
*/
ConsoleMethodWithDocs(GuiEditorCursorCtrl, getEffectiveHotSpot, ConsoleString, 2, 2, ())
{
    char* buffer = Con::getReturnBuffer(32);

    GuiCursor* cursor = object->getCursor();
    const Point2I extent = object->getImageExtent();
    if (cursor == NULL || extent.x <= 0 || extent.y <= 0)
    {
        dStrcpy(buffer, "0 0");
        return buffer;
    }

    const Point2I hotSpot = GuiEditorCursorCtrl::getEffectiveHotSpot(cursor->getHotSpot(), cursor->getRenderOffset(), extent);
    dSprintf(buffer, 32, "%d %d", hotSpot.x, hotSpot.y);
    return buffer;
}

/*! Sets the magnification, clamped to 1..16.
    @param zoom The magnification factor.
    @return No return value.
*/
ConsoleMethodWithDocs(GuiEditorCursorCtrl, setZoom, ConsoleVoid, 3, 3, (zoom))
{
    object->setZoom(dAtoi(argv[2]));
}

/*! Gets the current magnification, which is always the one being drawn: a zoom
    too big for the pane is clamped rather than pretended at.
    @return The magnification factor.
*/
ConsoleMethodWithDocs(GuiEditorCursorCtrl, getZoom, ConsoleInt, 2, 2, ())
{
    return object->getZoom();
}

/*! Gets the largest magnification this art fits the pane at. Zooming in stops
    here, so a pane can grey out its "+" instead of offering a zoom that would
    do nothing.
    @return The maximum magnification factor.
*/
ConsoleMethodWithDocs(GuiEditorCursorCtrl, getMaxZoom, ConsoleInt, 2, 2, ())
{
    return object->getMaxZoom();
}

ConsoleMethodGroupEndWithDocs(GuiEditorCursorCtrl)
