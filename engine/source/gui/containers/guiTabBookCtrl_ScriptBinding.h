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

ConsoleMethodGroupBeginWithDocs(GuiTabBookCtrl, GuiControl)

/*! Selects the active tab by index.
	@param pageIndex The zero-based index of the tab to make active.
*/
ConsoleMethodWithDocs(GuiTabBookCtrl, selectPage, ConsoleVoid, 3, 3, "(pageIndex)")
{
	S32 pageIndex = dAtoi(argv[2]);

	object->selectPage(pageIndex);
}

/*! Selects the active tab by name.
	@param pageName The name that appears on the tab to make active.
	@return No return value
*/
ConsoleMethodWithDocs(GuiTabBookCtrl, selectPageName, ConsoleVoid, 3, 3, "(pageName)")
{
	object->selectPage(argv[2]);
}

/*! Returns the currently selected page index.
	@return The index of the currently selected page.
*/
ConsoleMethodWithDocs(GuiTabBookCtrl, getSelectedPage, ConsoleInt, 2, 2, "()")
{
	return object->getSelectedPage();
}

/*! Sets the currently used TabProfile for the GuiControl
	@param p The tabprofile you wish to set the control to use
	@return No return value
*/
ConsoleMethodWithDocs(GuiTabBookCtrl, setTabProfile, ConsoleVoid, 3, 3, (GuiControlProfile p))
{
	GuiControlProfile* profile;

	if (Sim::findObject(argv[2], profile))
		object->setControlTabProfile(profile);
}

/*! Appends an untitled page to the book.

	The raw form: it names the page itself, puts it on GuiTabPageProfile and
	tells nobody. The Gui Editor does not use this - a page made there has to be
	themed, recorded for undo and announced to the Explorer tree, so the editor
	builds its own from GuiEditorBrain::newTabPage. This is for building a book
	from script or from C++, which is what GuiFrameSetCtrl does when it docks a
	window.
	@return No return value
*/
ConsoleMethodWithDocs(GuiTabBookCtrl, addPage, ConsoleVoid, 2, 2, ())
{
	object->addNewPage();
}

/*! Returns the bounds of the editor-only "+" tab as "x y width height", in
	global coordinates.

	Empty - "0 0 0 0" - unless the book is inside the Gui being authored, which
	is the only time the "+" is drawn. A book with no pages at all still reports
	one; that is what stops an emptied book from being unrecoverable.
	@return The rectangle the "+" occupies on screen.
*/
ConsoleMethodWithDocs(GuiTabBookCtrl, getAddPageTabRect, ConsoleString, 2, 2, ())
{
	RectI rect = object->getAddTabGlobalRect();

	char* buffer = Con::getReturnBuffer(64);
	dSprintf(buffer, 64, "%d %d %d %d", rect.point.x, rect.point.y, rect.extent.x, rect.extent.y);

	return buffer;
}

ConsoleMethodGroupEndWithDocs(GuiTabBookCtrl)