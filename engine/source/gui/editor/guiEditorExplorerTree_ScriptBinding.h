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

ConsoleMethodGroupBeginWithDocs(GuiEditorExplorerTree, GuiTreeViewCtrl)

/*! The width of one gutter column, in pixels.
	@return The column width, dividers included.
*/
ConsoleMethodWithDocs(GuiEditorExplorerTree, getGutterColumnWidth, ConsoleInt, 2, 2, ())
{
	return GuiEditorExplorerTree::smColumnWidth;
}

/*! Which gutter column a point in the tree's own coordinates falls in.
	@param localX The x offset into the control.
	@return "hidden", "locked", or "" for neither.
*/
ConsoleMethodWithDocs(GuiEditorExplorerTree, gutterColumnAt, ConsoleString, 3, 3, "(S32 localX)")
{
	switch (GuiEditorExplorerTree::columnAt(dAtoi(argv[2]), object->gutterInset()))
	{
	case GuiEditorExplorerTree::GutterHidden:
		return "hidden";
	case GuiEditorExplorerTree::GutterLocked:
		return "locked";
	default:
		return "";
	}
}

/*! The center of one column's box on one row, in canvas coordinates.
	Exists so a test can find its own target rather than carry a hard-coded
	point: a coordinate that drifted off the box would report a missing item,
	which is exactly what a broken hit test reports, and the test would be lying
	either way.
	@param index The zero-based raw item index.
	@param column "hidden" or "locked".
	@return The point, as "x y".
*/
ConsoleMethodWithDocs(GuiEditorExplorerTree, getGutterPoint, ConsoleString, 4, 4, "(S32 index, string column)")
{
	S32 index = dAtoi(argv[2]);
	if (index < 0 || index >= object->mItems.size())
	{
		Con::warnf("GuiEditorExplorerTree::getGutterPoint() - Invalid index given.");
		return "0 0";
	}

	GuiEditorExplorerTree::GutterColumn column = (dStricmp(argv[3], "locked") == 0)
		? GuiEditorExplorerTree::GutterLocked
		: GuiEditorExplorerTree::GutterHidden;

	Point2I point = object->getGutterPoint(index, column);

	char* buffer = Con::getReturnBuffer(32);
	dSprintf(buffer, 32, "%d %d", point.x, point.y);
	return buffer;
}

ConsoleMethodGroupEndWithDocs(GuiEditorExplorerTree)
