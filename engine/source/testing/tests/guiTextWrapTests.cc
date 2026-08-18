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

#ifndef _GUICONTROL_H_
#include "gui/guiControl.h"
#endif

#ifndef _GUITYPES_H_
#include "gui/guiTypes.h"
#endif

//-----------------------------------------------------------------------------
// Where a wrapped control breaks its lines.
//
// The case these exist for: a single word too long to fit, with no space to
// break at. The word-fitting loop pushes such a word as a line of its own and
// clears the line it was building, and the push at the foot of the loop then
// added a SECOND, empty line after it -- so a one-word caption was two lines
// tall.
//
// That was never only a cosmetic problem, and it was never only a bottom-aligned
// one. The line count becomes blockHeight, and blockHeight is what
// getTextVerticalOffset positions from, what mTextExtend sizes a control from,
// and what renderText compares against the room available to decide whether the
// text fits at all. Bottom alignment is simply where it showed: the empty line
// took the bottom slot and pushed the word up out of it. The last test here is
// that reasoning written down.
//
// wrapParagraph takes a measuring function rather than a GFont so that all of
// this can be checked with no canvas: asking a profile for a font registers a
// texture, and TextureManager::refresh asserts without a GL context -- which in
// a debug build is a modal box, so the failure arrives as a hang.
//-----------------------------------------------------------------------------

//-----------------------------------------------------------------------------
// Helpers
//-----------------------------------------------------------------------------

// A stand-in for a font: every character is ten pixels wide, spaces included.
// Real fonts are proportional, but nothing here is about the widths themselves,
// only about which words land on which line -- and a width that can be worked
// out in the head is what makes the numbers in these tests readable.
static U32 tenPerChar(void* context, const char* text)
{
    TORQUE_UNUSED(context);
    return (U32)(dStrlen(text) * 10);
}

static vector<string> wrap(const char* paragraph, const S32 totalWidth)
{
    return GuiControl::wrapParagraph(string(paragraph), totalWidth, &tenPerChar, NULL);
}

//-----------------------------------------------------------------------------
// The ordinary cases, so the fix below is known not to have cost them.
//-----------------------------------------------------------------------------

TEST(GuiTextWrapTests, TextThatFitsIsOneLine)
{
    vector<string> lines = wrap("ab", 100);

    ASSERT_EQ(1U, lines.size());
    EXPECT_STREQ("ab", lines[0].c_str());
}

TEST(GuiTextWrapTests, TextThatDoesNotFitBreaksAtTheSpace)
{
    // "aaa bbb" wants 70 and has 50, so it breaks; each half fits on its own.
    vector<string> lines = wrap("aaa bbb", 50);

    ASSERT_EQ(2U, lines.size());
    EXPECT_STREQ("aaa ", lines[0].c_str());
    EXPECT_STREQ("bbb", lines[1].c_str());
}

// An empty paragraph still owes one empty line. It is what a blank line between
// two others is made of, and what gives an empty multi-line text box somewhere
// to put its caret -- so the guard against the stray line must not swallow it.
TEST(GuiTextWrapTests, AnEmptyParagraphIsStillOneLine)
{
    vector<string> lines = wrap("", 100);

    ASSERT_EQ(1U, lines.size());
    EXPECT_STREQ("", lines[0].c_str());
}

//-----------------------------------------------------------------------------
// The word that cannot be broken.
//-----------------------------------------------------------------------------

// "BlankCircle" wants 110 and has 100. There is nowhere to break it, so it takes
// one line and is clipped -- one line, not one line and an empty one after it.
TEST(GuiTextWrapTests, AWordTooLongToFitIsStillOneLine)
{
    vector<string> lines = wrap("BlankCircle", 100);

    ASSERT_EQ(1U, lines.size());
    EXPECT_STREQ("BlankCircle ", lines[0].c_str());
}

TEST(GuiTextWrapTests, AWordTooLongToFitAtTheEndAddsNoEmptyLine)
{
    vector<string> lines = wrap("hi BlankCircle", 100);

    ASSERT_EQ(2U, lines.size());
    EXPECT_STREQ("hi ", lines[0].c_str());
    EXPECT_STREQ("BlankCircle ", lines[1].c_str());
}

// The same word first rather than last. This one never had the bug -- the word
// after it refills the line the push at the foot of the loop empties -- and it
// is here so that a future fix cannot cure the end case by breaking this one.
TEST(GuiTextWrapTests, AWordTooLongToFitAtTheStartKeepsWhatFollows)
{
    vector<string> lines = wrap("BlankCircle hi", 100);

    ASSERT_EQ(2U, lines.size());
    EXPECT_STREQ("BlankCircle ", lines[0].c_str());
    EXPECT_STREQ("hi", lines[1].c_str());
}

// Two of them running together, which is the shape a wrapped list of long asset
// names takes: one line each, and nothing between them.
TEST(GuiTextWrapTests, TwoWordsTooLongToFitTakeOneLineEach)
{
    vector<string> lines = wrap("BlankCircle CannonballSprite", 100);

    ASSERT_EQ(2U, lines.size());
    EXPECT_STREQ("BlankCircle ", lines[0].c_str());
    EXPECT_STREQ("CannonballSprite ", lines[1].c_str());
}

//-----------------------------------------------------------------------------
// Why the stray line mattered, and to which alignments.
//-----------------------------------------------------------------------------

// One extra line moves the text by a whole line under BottomVAlign and by half a
// line under MiddleVAlign. Under TopVAlign it moves nothing at all -- which is
// the whole reason this looked like a bottom-aligned bug and was not one.
TEST(GuiTextWrapTests, AStrayLineMovesEveryAlignmentExceptTop)
{
    const S32 lineHeight = 16;
    const S32 roomHeight = 34;
    const S32 oneLine = lineHeight;
    const S32 twoLines = lineHeight * 2;

    EXPECT_EQ(0, GuiControl::getTextVerticalOffset(oneLine, roomHeight, TopVAlign));
    EXPECT_EQ(0, GuiControl::getTextVerticalOffset(twoLines, roomHeight, TopVAlign));

    // 18 down to 2: the caption climbs a full line height.
    EXPECT_EQ(18, GuiControl::getTextVerticalOffset(oneLine, roomHeight, BottomVAlign));
    EXPECT_EQ(2, GuiControl::getTextVerticalOffset(twoLines, roomHeight, BottomVAlign));

    // 9 down to 1: half of one.
    EXPECT_EQ(9, GuiControl::getTextVerticalOffset(oneLine, roomHeight, MiddleVAlign));
    EXPECT_EQ(1, GuiControl::getTextVerticalOffset(twoLines, roomHeight, MiddleVAlign));
}

#endif // TORQUE_SHIPPING
