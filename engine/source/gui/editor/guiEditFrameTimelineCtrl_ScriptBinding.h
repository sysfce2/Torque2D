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

ConsoleMethodGroupBeginWithDocs(GuiEditFrameTimelineCtrl, GuiEditFrameStripCtrl)

/*! Gets the frames the timeline holds, in order.
    Trimmed, deliberately unlike AnimationAsset::getAnimationFrames, which leaves
    a trailing space.
    @return The frame indices, space separated, or an empty string.
*/
ConsoleMethodWithDocs(GuiEditFrameTimelineCtrl, getFrames, ConsoleString, 2, 2, ())
{
    return object->getFrames();
}

/*! Replaces the whole list.
    Separators are space, tab and newline, matching what
    AnimationAsset::setAnimationFrames accepts, so a list can be handed straight
    from one to the other.
    @param frames The frame indices, space separated.
    @return No return value.
*/
ConsoleMethodWithDocs(GuiEditFrameTimelineCtrl, setFrames, ConsoleVoid, 3, 3, (frames))
{
    object->setFrames(argv[2]);
}

/*! Gets the frames the timeline holds, by cell name, in order.

    For an image in explicit mode, whose cells have names.  A frame whose cell has
    since been deleted still reports its name -- the timeline keeps it, and draws
    it as an empty outlined slot, rather than dropping a frame the user never
    asked to lose.
    @return The cell names, space separated, or an empty string.
*/
ConsoleMethodWithDocs(GuiEditFrameTimelineCtrl, getNamedFrames, ConsoleString, 2, 2, ())
{
    return object->getNamedFrames();
}

/*! Replaces the whole list, by cell name.
    Separators are space, tab, newline and comma, matching what
    AnimationAsset::setNamedAnimationFrames accepts.
    @param names The cell names, space separated.
    @return No return value.
*/
ConsoleMethodWithDocs(GuiEditFrameTimelineCtrl, setNamedFrames, ConsoleVoid, 3, 3, (names))
{
    object->setNamedFrames(argv[2]);
}

/*! Gets the cell name in a slot.
    @param slot The slot index.
    @return The cell name, or an empty string when the image does not name its cells.
*/
ConsoleMethodWithDocs(GuiEditFrameTimelineCtrl, getNameAt, ConsoleString, 3, 3, (slot))
{
    return object->getNameAt(dAtoi(argv[2]));
}

/*! Puts a frame into the list at a slot.
    @param slot Where it goes, from 0 to the count inclusive.
    @param frame The image frame index.
    @return Whether it went in.
*/
ConsoleMethodWithDocs(GuiEditFrameTimelineCtrl, insertFrame, ConsoleBool, 4, 4, (slot, frame))
{
    return object->insertFrame(dAtoi(argv[2]), dAtoi(argv[3]));
}

/*! Takes a slot out of the list.
    @param slot The slot index.
    @return Whether there was one to take.
*/
ConsoleMethodWithDocs(GuiEditFrameTimelineCtrl, removeSlot, ConsoleBool, 3, 3, (slot))
{
    return object->removeSlot(dAtoi(argv[2]));
}

/*! Moves a slot to a new place in the list.
    @param from The slot to lift.
    @param to Where to put it, measured against the list as it stands now.
    @return Whether anything moved.
*/
ConsoleMethodWithDocs(GuiEditFrameTimelineCtrl, moveSlot, ConsoleBool, 4, 4, (from, to))
{
    return object->moveSlot(dAtoi(argv[2]), dAtoi(argv[3]));
}

/*! Picks a slot, or -1 for none.
    @param slot The slot index.
    @return No return value.
*/
ConsoleMethodWithDocs(GuiEditFrameTimelineCtrl, setSelectedSlot, ConsoleVoid, 3, 3, (slot))
{
    object->setSelectedSlot(dAtoi(argv[2]));
}

/*! Gets the picked slot.
    @return The slot index, or -1 when nothing is picked.
*/
ConsoleMethodWithDocs(GuiEditFrameTimelineCtrl, getSelectedSlot, ConsoleInt, 2, 2, ())
{
    return object->getSelectedSlot();
}

/*! Gets the slot the preview is currently showing.
    @return The slot index, or -1 when there is no preview or it is not playing
    this animation.
*/
ConsoleMethodWithDocs(GuiEditFrameTimelineCtrl, getPlayheadSlot, ConsoleInt, 2, 2, ())
{
    return object->getPlayheadSlot();
}

/*! Points the playhead marker at a sprite's playback.
    @param sprite The preview sprite.
    @return Whether it was a sprite.
*/
ConsoleMethodWithDocs(GuiEditFrameTimelineCtrl, setPreviewSprite, ConsoleBool, 3, 3, (sprite))
{
    SpriteBase* sprite = dynamic_cast<SpriteBase*>(Sim::findObject(argv[2]));
    if (sprite == NULL)
    {
        Con::warnf("GuiEditFrameTimelineCtrl::setPreviewSprite() - '%s' is not a sprite.", argv[2]);
        return false;
    }

    object->setPreviewSprite(sprite);
    return true;
}

/*! Forgets the preview sprite, so the playhead marker stands down.
    @return No return value.
*/
ConsoleMethodWithDocs(GuiEditFrameTimelineCtrl, clearPreviewSprite, ConsoleVoid, 2, 2, ())
{
    object->clearPreviewSprite();
}

/*! Gets where a slot is on the canvas.
    @param slot The slot index.
    @return The rect as "x y width height" in global coordinates.
*/
ConsoleMethodWithDocs(GuiEditFrameTimelineCtrl, getSlotRect, ConsoleString, 3, 3, (slot))
{
    const RectI slotRect = object->getCellRectGlobal(dAtoi(argv[2]));

    char* buffer = Con::getReturnBuffer(64);
    dSprintf(buffer, 64, "%d %d %d %d", slotRect.point.x, slotRect.point.y, slotRect.extent.x, slotRect.extent.y);
    return buffer;
}

/*! Shows the insertion caret for a point on the canvas.
    @param x The global x coordinate, or "x y" as one argument.
    @param y The global y coordinate.
    @return The slot a drop released here would land in.
*/
ConsoleMethodWithDocs(GuiEditFrameTimelineCtrl, showCaretAt, ConsoleInt, 3, 4, (x, y))
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

    return object->showCaretAt(point);
}

/*! Hides the insertion caret.
    @return No return value.
*/
ConsoleMethodWithDocs(GuiEditFrameTimelineCtrl, clearCaret, ConsoleVoid, 2, 2, ())
{
    object->clearCaret();
}

/*! Puts a frame in where a drag was released.
    Uses the same arithmetic the caret was drawn from, so what was shown is what
    happens.
    @param point The global point as "x y".
    @param frame The image frame index.
    @return Whether it went in.
*/
ConsoleMethodWithDocs(GuiEditFrameTimelineCtrl, insertFrameAtPoint, ConsoleBool, 4, 4, (point, frame))
{
    Point2I point(0, 0);
    dSscanf(argv[2], "%d %d", &point.x, &point.y);

    return object->insertFrameAtPoint(point, dAtoi(argv[3]));
}

ConsoleMethodGroupEndWithDocs(GuiEditFrameTimelineCtrl)
