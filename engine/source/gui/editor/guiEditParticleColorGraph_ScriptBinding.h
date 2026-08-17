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

ConsoleMethodGroupBeginWithDocs(GuiEditParticleColorGraph, GuiParticleGraphInspector)

/*! Sets which color channel the graph edits.
    The other two channels stay on screen, drawn dim, and a click cannot reach them.
    @param channel Red, Green or Blue. The field names RedChannel, GreenChannel and BlueChannel are accepted too.
    @return No return value.
*/
ConsoleMethodWithDocs(GuiEditParticleColorGraph, setActiveChannel, ConsoleVoid, 3, 3, (channel))
{
    GuiEditParticleColorGraph::Channel channel = GuiEditParticleColorGraph::getChannelFromName(argv[2]);

    if (channel == GuiEditParticleColorGraph::ChannelCount)
    {
        Con::warnf("GuiEditParticleColorGraph::setActiveChannel() - '%s' is not a color channel.", argv[2]);
        return;
    }

    object->setActiveChannel(channel);
}

/*! Gets which color channel the graph is editing.
    @return Red, Green or Blue.
*/
ConsoleMethodWithDocs(GuiEditParticleColorGraph, getActiveChannel, ConsoleString, 2, 2, ())
{
    switch (object->getActiveChannel())
    {
    case GuiEditParticleColorGraph::ChannelGreen:
        return "Green";
    case GuiEditParticleColorGraph::ChannelBlue:
        return "Blue";
    default:
        return "Red";
    }
}

/*! Gets the color the three channels mix to at a point in the particle's life.
    This is the color the strip under the plot is drawing at that time.
    @param time Where in the particle's life to sample, from 0 to 1.
    @return The color as "red green blue", each from 0 to 1.
*/
ConsoleMethodWithDocs(GuiEditParticleColorGraph, getColorAtTime, ConsoleString, 3, 3, (time))
{
    const ColorF color = object->getColorAtTime(dAtof(argv[2]));

    char* buffer = Con::getReturnBuffer(64);
    dSprintf(buffer, 64, "%g %g %g", color.red, color.green, color.blue);

    return buffer;
}

/*! Gets the times the mixed color strip bends at, across the visible time window.
    Every key of every channel inside the window, plus the window's two edges.
    @return A space separated list of times.
*/
ConsoleMethodWithDocs(GuiEditParticleColorGraph, getGradientStops, ConsoleString, 2, 2, ())
{
    return object->getGradientStopList();
}

/*! Sets how tall the mixed color strip under the plot is drawn.
    @param height The height in pixels, or zero for no strip at all.
    @return No return value.
*/
ConsoleMethodWithDocs(GuiEditParticleColorGraph, setStripHeight, ConsoleVoid, 3, 3, (height))
{
    object->setStripHeight(dAtoi(argv[2]));
}

ConsoleMethodGroupEndWithDocs(GuiEditParticleColorGraph)
