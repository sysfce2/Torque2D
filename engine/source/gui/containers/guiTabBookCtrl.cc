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
#include "console/console.h"
#include "console/consoleTypes.h"
#include "graphics/dgl.h"
#include "sim/simBase.h"
#include "gui/guiCanvas.h"
#include "gui/containers/guiTabBookCtrl.h"
#include "platform/event.h"
#include "io/fileStream.h"
#include "gui/containers/guiScrollCtrl.h"
#include "gui/editor/guiEditCtrl.h"
#include "gui/guiDefaultControlRender.h"
#include "gui/containers/guiFrameSetCtrl.h"

#include "guiTabBookCtrl_ScriptBinding.h"

// So we can set tab alignment via gui editor
static EnumTable::Enums tabAlignEnums[] =
{
   { GuiTabBookCtrl::AlignTop,   "Top"    },
   { GuiTabBookCtrl::AlignLeft,  "Left"   },
   { GuiTabBookCtrl::AlignBottom,"Bottom" },
   { GuiTabBookCtrl::AlignRight,	"Right"  }
};
static EnumTable gTabAlignEnums(4,&tabAlignEnums[0]);


IMPLEMENT_CONOBJECT(GuiTabBookCtrl);

GuiTabBookCtrl::GuiTabBookCtrl()
{
   VECTOR_SET_ASSOCIATION(mPages);
   mFontHeight = 0;
   mTabPosition = GuiTabBookCtrl::AlignTop;
   mLastTabPosition = mTabPosition;
   mActivePage = NULL;
   mHoverTab = NULL;
   mHasTexture = false;
   mBitmapBounds = NULL;
   mBounds.extent.set( 400, 300 );
   mPageRect = RectI(0,0,0,0);
   mTabRect = RectI(0,0,0,0);

   // Empty until a layout pass in edit mode fills it in, and read by
   // getAddTabGlobalRect - which script can ask about a book that has never laid
   // itself out at all.
   mAddTabRect = RectI(0,0,0,0);
   mTabDownPosition = Point2I();
   mDepressed = false;

   mPages.reserve(12);
   mMinTabWidth = 64;
   mTabWidth = 64;
   mIsFrameSetGenerated = false;

   mTabProfile = NULL;

   setField("profile", "GuiDefaultProfile");
   setField("TabProfile", "GuiTabProfile");
}

void GuiTabBookCtrl::initPersistFields()
{
   Parent::initPersistFields();
   addField("TabPosition",		TypeEnum,		Offset(mTabPosition,GuiTabBookCtrl), 1, &gTabAlignEnums );
   addField("MinTabWidth", TypeS32,    Offset(mMinTabWidth,GuiTabBookCtrl));
   addField("TabProfile", TypeGuiProfile, Offset(mTabProfile, GuiTabBookCtrl));
}

bool GuiTabBookCtrl::onAdd()
{
   Parent::onAdd();

   return true;
}


void GuiTabBookCtrl::onRemove()
{
   Parent::onRemove();
}

void GuiTabBookCtrl::onChildRemoved( GuiControl* child )
{
   for (S32 i = 0; i < mPages.size(); i++ )
   {
      GuiTabPageCtrl* tab = mPages[i].Page;
      if( tab == child )
      {
         if( tab == mActivePage )
            mActivePage = NULL;
         mPages.erase( i );
         break;
      }
   }

   if( mPages.empty() )
      mActivePage = NULL;
   else if (mActivePage == NULL )
      mActivePage = static_cast<GuiTabPageCtrl*>(mPages[0].Page);

   // The strip has one fewer tab in it, and nothing else re-runs the layout on a
   // removal: solveDirty watches the tab position, the font height and the first
   // tab's width, and a delete changes none of them. Without this the surviving
   // tabs keep the rectangles they were given and leave a hole where the deleted
   // one used to be.
   calculatePageTabs();

   // Whichever page was promoted above was hidden the moment some other tab was
   // chosen, and nothing has told it otherwise.
   syncPageVisibility();
}

void GuiTabBookCtrl::syncPageVisibility()
{
   for( S32 i = 0; i < mPages.size(); i++ )
   {
      GuiTabPageCtrl* page = mPages[i].Page;
      if( page != NULL )
         page->setVisible( page == mActivePage );
   }
}

// The tab strip is drawn from mPages, which is filled in the order pages are
// added and is otherwise independent of the child list. Anything that
// rearranges the children - a drag in the Gui Editor's tree, or an undo putting
// a deleted page back where it came from - therefore leaves a page sitting in
// the middle of the children and at the end of the tab strip. The children are
// the truth, so rebuild the order from them.
void GuiTabBookCtrl::childrenReordered()
{
	Vector<TabHeaderInfo> ordered;

	for (iterator i = begin(); i != end(); i++)
	{
		GuiTabPageCtrl* page = dynamic_cast<GuiTabPageCtrl*>(*i);
		if (!page)
			continue;

		for (S32 p = 0; p < mPages.size(); p++)
		{
			if (mPages[p].Page == page)
			{
				ordered.push_back(mPages[p]);
				break;
			}
		}
	}

	// Anything the children did not account for is a page the book believes in
	// and the child list does not; dropping it here would leak it out of the
	// strip, so only take the rebuild when the two agree on the count.
	if (ordered.size() != mPages.size())
		return;

	mPages.clear();
	for (S32 p = 0; p < ordered.size(); p++)
		mPages.push_back(ordered[p]);

	calculatePageTabs();

	// Undo puts a deleted page back by moving it, and the recorder's layout fix
	// restores position, extent and sizing - not visibility. A page that was
	// showing when it was deleted comes back still showing, on top of whichever
	// page took over from it.
	syncPageVisibility();

	Parent::childrenReordered();
}

void GuiTabBookCtrl::onChildAdded( GuiControl *child )
{
   GuiTabPageCtrl *page = dynamic_cast<GuiTabPageCtrl*>(child);
   if( !page )
   {
      Con::warnf("GuiTabBookCtrl::onChildAdded - attempting to add NON GuiTabPageCtrl as child page");

      // Work out where it is going BEFORE taking it out of the book. A book with
      // no active page and no parent has nowhere to send it, and removing it
      // first left it registered with no group at all - which a book emptied of
      // its pages in the editor makes an ordinary thing to run into.
      GuiControl *destination = mActivePage;
      if( destination == NULL )
      {
         Con::warnf("GuiTabBookCtrl::onChildAdded - unable to find active page to reassign ownership of new child control to, placing on parent");
         destination = getParent();
      }

      if( destination == NULL )
      {
         Con::warnf("GuiTabBookCtrl::onChildAdded - no parent to place it on either; leaving it where it is");
         return;
      }

      removeObject( child );
      destination->addObject( child );
      return;
   }


   TabHeaderInfo newPage;

   newPage.Page      = page;
   newPage.TabRow    = -1;
   newPage.TabColumn = -1;

   mPages.push_back( newPage );

   // A book with pages always has an active one. Without this a book holding a
   // single page draws that page's tab unselected and shows nothing inside it,
   // and onMouseDownEditor's "select the page behind the tab" has nothing to
   // select.
   //
   // Deliberately not selectPage(), which ends in an onTabSelected script
   // callback: this runs from inside addObject, and EditorCore adds one page per
   // editor as each editor's module loads. Its handler opens the editor that
   // page belongs to, so during load the first one would open before the rest of
   // them exist.
   if( mActivePage == NULL )
      mActivePage = page;

   // Calculate Page Information
   calculatePageTabs();

   syncPageVisibility();

   child->resize( Point2I(0, 0), mPageRect.extent );
}


bool GuiTabBookCtrl::onWake()
{
   if (! Parent::onWake())
      return false;

   mHasTexture = mProfile->constructBitmapArray();
   if( mHasTexture )
      mBitmapBounds = mProfile->mBitmapArrayRects.address();

   //increment the tab profile
   if (mTabProfile != NULL)
		mTabProfile->incRefCount();

   return true;
}

void GuiTabBookCtrl::onSleep()
{
   Parent::onSleep();

   //decrement the tab profile referrence
   if (mTabProfile != NULL)
       mTabProfile->decRefCount();
}

void GuiTabBookCtrl::setControlTabProfile(GuiControlProfile* prof)
{
    AssertFatal(prof, "GuiTabBookCtrl::setControlTabProfile: invalid tab profile");
    if (prof == mTabProfile)
        return;
    if (mAwake)
        mTabProfile->decRefCount();
    mTabProfile = prof;
    if (mAwake)
        mTabProfile->incRefCount();

	calculatePageTabs();
}

void GuiTabBookCtrl::addNewPage()
{
   char textbuf[1024];

   GuiTabPageCtrl * page = new GuiTabPageCtrl();

   page->setField("profile", "GuiTabPageProfile");

   dSprintf(textbuf, sizeof(textbuf), "TabBookPage%d_%d", getId(), page->getId());
   page->registerObject(textbuf);

   this->addObject( page );
}

void GuiTabBookCtrl::requestNewPage()
{
   // The GuiEditCtrl wears the GuiEditorBrain namespace, so this arrives at
   // GuiEditorBrain::onAddTabPage. A book being edited by anything that has no
   // handler for it simply gets no page, which is the right way for this to
   // fail.
   GuiEditCtrl* edit = GuiControl::smEditorHandle;
   if( edit != NULL && edit->isMethod("onAddTabPage") )
   {
      Con::executef( edit, 2, "onAddTabPage", getIdString() );
   }
}

void GuiTabBookCtrl::resize(const Point2I &newPosition, const Point2I &newExtent)
{
   Parent::resize( newPosition, newExtent );

   calculatePageTabs();

   // Resize Children
   SimSet::iterator i;
   for(i = begin(); i != end(); i++)
   {
      GuiControl *ctrl = static_cast<GuiControl *>(*i);
      ctrl->resize( Point2I(0, 0), mPageRect.extent );
   }
}

void GuiTabBookCtrl::childResized(GuiControl *child)
{
	if(mPageRect.extent != child->mBounds.extent || child->mBounds.point != Point2I::Zero)
	{
		child->resize( Point2I(0,0), mPageRect.extent );
	}
}

Point2I GuiTabBookCtrl::getTabLocalCoord(const Point2I &src)
{
	//Get the border profiles
	GuiBorderProfile *leftProfile = mProfile->getLeftBorder();
	GuiBorderProfile *topProfile = mProfile->getTopBorder();

	S32 leftSize = (leftProfile) ? leftProfile->getMargin(NormalState) + leftProfile->getBorder(NormalState) + leftProfile->getPadding(NormalState) : 0;
	S32 topSize = (topProfile) ? topProfile->getMargin(NormalState) + topProfile->getBorder(NormalState) + topProfile->getPadding(NormalState) : 0;

	Point2I ret = Point2I(src.x - leftSize, src.y - topSize);
	ret.x -= mTabRect.point.x;
	ret.y -= mTabRect.point.y;

	return ret;
}

RectI GuiTabBookCtrl::getAddTabGlobalRect()
{
	// isEditMode as well as the rectangle, because closing the editor does not
	// re-run the layout: the book would go on reporting the "+" it had until
	// something else happened to resize it. Nothing DRAWS one - renderAddTab has
	// no editor to ask for a colour - so this is the only place it could show.
	if (!mAddTabRect.isValidRect() || !isEditMode())
	{
		return RectI(0, 0, 0, 0);
	}

	// The same walk onRender makes to reach the point it hands renderTabs, which
	// is the origin mAddTabRect is measured from.
	Point2I totalOffset = localToGlobalCoord(Point2I(0, 0)) + mTabRect.point;
	RectI ctrlRect = applyMargins(totalOffset, mTabRect.extent, NormalState, mProfile);
	RectI fillRect = applyBorders(ctrlRect.point, ctrlRect.extent, NormalState, mProfile);
	RectI contentRect = applyPadding(fillRect.point, fillRect.extent, NormalState, mProfile);

	return RectI(contentRect.point + mAddTabRect.point, mAddTabRect.extent);
}

void GuiTabBookCtrl::onTouchDown(const GuiEvent &event)
{
    Point2I localMouse = globalToLocalCoord( event.mousePoint );
    if( mTabRect.pointInRect( localMouse ) )
    {
		mTabDownPosition = event.mousePoint;
		mDepressed = true;
		mouseLock();

		Point2I tabLocalMouse = getTabLocalCoord(localMouse);
        GuiTabPageCtrl *tab = findHitTab(tabLocalMouse);
        if( tab != NULL && tab->isActive() )
            selectPage( tab );
    }
    else
    {
        Parent::onTouchDown(event);
    }
}

void GuiTabBookCtrl::onTouchMove(const GuiEvent &event)
{
   Point2I localMouse = globalToLocalCoord( event.mousePoint );
   if( mTabRect.pointInRect( localMouse ) )
   {
	   Point2I tabLocalMouse = getTabLocalCoord(localMouse);
      GuiTabPageCtrl *tab = findHitTab(tabLocalMouse);
      if( tab != NULL && mHoverTab != tab )
         mHoverTab = tab;
      else if ( !tab )
         mHoverTab = NULL;
   }
   else
   {
	   mHoverTab = NULL;
   }
   Parent::onTouchMove( event );
}

void GuiTabBookCtrl::onTouchLeave( const GuiEvent &event )
{
   mHoverTab = NULL;
   Parent::onTouchLeave(event);
}

void GuiTabBookCtrl::onTouchDragged(const GuiEvent& event)
{
	if (mDepressed && mActivePage && mActivePage->size() > 0)
	{
		Point2I deltaMousePosition = event.mousePoint - mTabDownPosition;
		const S32 dragDist = 20;
		if (mAbs(deltaMousePosition.x) > dragDist || mAbs(deltaMousePosition.y) > dragDist)
		{
			//That's cool, but to transform the tab into window, we need a parent FrameSet and a grandchild that's a docked window.
			GuiFrameSetCtrl* frameSet = dynamic_cast<GuiFrameSetCtrl*>(getParent());
			GuiWindowCtrl* window = dynamic_cast<GuiWindowCtrl*>((*mActivePage)[0]);
			if (frameSet && window && window->mPageDocked)
			{
				//We have a winner!!!
				mDepressed = false;
				mouseUnlock();
				frameSet->undockWindowFromBook(window, this, mActivePage);
				return;
			}
		}
	}
	Parent::onTouchDragged(event);
}

void GuiTabBookCtrl::onTouchUp(const GuiEvent& event)
{
	if (mDepressed)
	{
		mouseUnlock();
	}
	Parent::onTouchUp(event);
}

bool GuiTabBookCtrl::onMouseDownEditor(const GuiEvent &event, const Point2I& offset)
{
   bool handled = false;
   Point2I localMouse = globalToLocalCoord( event.mousePoint );

   if( mTabRect.pointInRect( localMouse ) )
   {
      // Tab rectangles are measured from the strip's CONTENT, not from the
      // control. onTouchDown has always converted before asking and this has
      // always not, which put every editor tab hit out by the book's margin,
      // border and padding - and, for a bottom or right strip, by the whole
      // width of the page area as well.
      Point2I tabLocalMouse = getTabLocalCoord( localMouse );

      // Before the real tabs: the "+" sits inside the strip, so a stale tab
      // rectangle must not get first refusal on it.
      if( mAddTabRect.isValidRect() && mAddTabRect.pointInRect( tabLocalMouse ) )
      {
         requestNewPage();

         // Nothing else happens on this click. Selection follows the page the
         // editor is about to make, not the page that happened to be showing.
         return true;
      }

      GuiTabPageCtrl *tab = findHitTab( tabLocalMouse );
      if( tab != NULL )
      {
         selectPage( tab );
         handled = true;
      }
   }

   // This shouldn't be called if it's not design time, but check just incase
   if ( GuiControl::smDesignTime )
   {
      // If we clicked in the editor and our addset is the tab book
      // ctrl, select the child ctrl so we can edit it's properties
      GuiEditCtrl* edit = GuiControl::smEditorHandle;
      if( edit  && ( edit->getAddSet() == this ) && mActivePage != NULL )
         edit->select( mActivePage );
   }

   if (!handled)
   {
	   return Parent::onMouseDownEditor(event, offset);
   }
   return true;

}

void GuiTabBookCtrl::onPreRender()
{
   // sometimes we need to resize because of a changed persistent field
   // that's what this does
   solveDirty();
}

void GuiTabBookCtrl::onRender(Point2I offset, const RectI &updateRect)
{
   Point2I totalOffset = offset + mTabRect.point;
	RectI ctrlRect = applyMargins(totalOffset, mTabRect.extent, NormalState, mProfile);

	if (!ctrlRect.isValidRect())
	{
		return;
	}

	renderUniversalRect(ctrlRect, mProfile, NormalState);
	RectI fillRect = applyBorders(ctrlRect.point, ctrlRect.extent, NormalState, mProfile);
	RectI contentRect = applyPadding(fillRect.point, fillRect.extent, NormalState, mProfile);
	if (contentRect.isValidRect())
	{
		renderTabs(contentRect.point);
	}

	if(mPageRect.isValidRect())
	{
		// Render Children
		renderChildControls(offset, RectI(offset + mPageRect.point, mPageRect.extent), updateRect);
	}
}

void GuiTabBookCtrl::renderTabs( const Point2I &offset )
{
   for( S32 i = 0; i < mPages.size(); i++ )
   {
      RectI tabBounds = mPages[i].TabRect;
      tabBounds.point += offset;
      GuiTabPageCtrl *tab = mPages[i].Page;
      if( tab != NULL )
         renderTab( tabBounds, tab );
   }

   // After the real tabs, so it always reads as the end of the strip. Empty
   // unless calculatePageTabs found itself in edit mode, which is the whole test
   // - a book with no pages still gets one, and that is the only thing standing
   // between an emptied book and being unrecoverable.
   if( mAddTabRect.isValidRect() )
   {
      RectI addBounds = mAddTabRect;
      addBounds.point += offset;
      renderAddTab( addBounds );
   }
}

void GuiTabBookCtrl::renderAddTab( RectI tabRect )
{
   GuiEditCtrl* edit = GuiControl::smEditorHandle;
   if( edit == NULL )
      return;

   // Ghosted, so it never passes for a page. Brighter under the cursor, so it
   // reads as something to click rather than a gap the tabs did not fill - the
   // same idea as the frame set's split handles.
   ColorI fill = edit->getEditorColor();
   fill.alpha = 100;

   GuiCanvas* root = getRoot();
   if( root != NULL && tabRect.pointInRect( root->getCursorPos() ) )
      fill.alpha = 200;

   dglDrawRectFill( tabRect, fill );

   dglSetBitmapModulation( getFontColor( edit->mProfile, NormalState ) );
   F32 tempAdjust = mFontSizeAdjust;
   mFontSizeAdjust = 1.5f;
   renderText( tabRect.point, tabRect.extent, "+", edit->mProfile );
   mFontSizeAdjust = tempAdjust;
}

void GuiTabBookCtrl::renderTab( RectI tabRect, GuiTabPageCtrl *tab )
{
   StringTableEntry text = tab->getText();

   GuiControlState currentState = GuiControlState::NormalState;
   if (mActivePage == tab)
   {
	   currentState = SelectedState;
   }
   else if (mHoverTab == tab)
   {
	   currentState = HighlightState;
   }

   RectI ctrlRect = applyMargins(tabRect.point, tabRect.extent, currentState, mTabProfile);
   if (!ctrlRect.isValidRect())
   {
	   return;
   }

   renderUniversalRect(ctrlRect, mTabProfile, currentState);

   //Render Text
   dglSetBitmapModulation(getFontColor(mTabProfile, currentState));
   RectI fillRect = applyBorders(ctrlRect.point, ctrlRect.extent, currentState, mTabProfile);
   RectI contentRect = applyPadding(fillRect.point, fillRect.extent, currentState, mTabProfile);

   TextRotationOptions rot = tRotateNone;
   if (mTabPosition == AlignLeft)
   {
		rot = tRotateLeft;
   }
   else if(mTabPosition == AlignRight)
   {
		rot = tRotateRight;
   }

   renderText(contentRect.point, contentRect.extent, text, mTabProfile, rot);
}

// This is nothing but a clever hack to allow the tab page children
// to cast this to a GuiControl* so that the file doesn't need to have circular
// includes.  generic method overriding for the win!
void GuiTabBookCtrl::setUpdate()
{
   Parent::setUpdate();

   setUpdateRegion(Point2I(0,0), mBounds.extent);

   calculatePageTabs();
}

void GuiTabBookCtrl::solveDirty()
{
   bool dirty = false;
   GFont* font = mTabProfile->getFont(mFontSizeAdjust);
   if( mTabPosition != mLastTabPosition )
   {
      mLastTabPosition = mTabPosition;
      dirty = true;
   }
   else if( mTabProfile != NULL && font != NULL && font->getHeight() != mFontHeight )
   {
      dirty = true;
   }
   else if(mPages.size() > 0 && mTabProfile != NULL && font != NULL)
   {
	   S32 tabWidth = calculatePageTabWidth(mPages[0].Page);
	   tabWidth = getMax(tabWidth, mMinTabWidth);
	   if(mTabWidth != tabWidth)
	   {
		  dirty = true;
	   }
   }

   if( dirty )
   {
      resize( mBounds.point, mBounds.extent );
   }

}

S32 GuiTabBookCtrl::calculatePageTabWidth( GuiTabPageCtrl *page )
{
   if( !page )
      return mTabWidth;

   StringTableEntry text = page->getText();

   if( !text || dStrlen(text) == 0 || !mTabProfile )
      return mTabWidth;

	S32 textLength = mTabProfile->getFont(mFontSizeAdjust)->getStrNWidth(text, dStrlen(text));

   Point2I innerExtent = Point2I(textLength, textLength);
	Point2I outerExtent = getOuterExtent(innerExtent, NormalState, mTabProfile);

	if (mTabPosition == AlignTop || mTabPosition == AlignBottom)
	{
		return outerExtent.x;
	}
	else
	{
		return outerExtent.y;
	}
}

void GuiTabBookCtrl::calculatePageTabs()
{
   // Ahead of every return below: a book that leaves edit mode must not be left
   // holding a "+" tab that is no longer drawn, or getAddPageTabRect reports a
   // rectangle nothing will answer a click in.
   mAddTabRect.set(Point2I(0, 0), Point2I(0, 0));

   // The "+" tab is the only reason to lay out a book with no pages. Without it
   // an empty book short-circuits here exactly as it always has: mTabRect keeps
   // the zero it was constructed with, and onRender returns on the invalid rect
   // before drawing anything at all.
   //
   // mTabProfile is the other half of what that short circuit has been quietly
   // protecting - the font lookup below dereferences it, and it is only set from
   // a profile the constructor names, which a bare engine need not have.
   const bool wantAddTab = isEditMode() && mTabProfile != NULL;

   // Short Circuit.
   //
   // If the tab size is zero, don't render tabs,
   //  and assume it's a tab-less tab-book - JDD
   if( mPages.empty() && !wantAddTab )
      return;

   S32 currRow    = 0;
   S32 currColumn = 0;
   S32 currX      = 0;
   S32 currY      = 0;
   S32 tabHeight  = 0;
   RectI innerRect = getInnerRect();
   mFontHeight = mTabProfile->getFont(mFontSizeAdjust)->getHeight();
   Point2I innerExtent = Point2I(mFontHeight, mFontHeight);
   Point2I fontBasedBounds = getOuterExtent(innerExtent, NormalState, mTabProfile);

   if (mTabPosition == AlignTop || mTabPosition == AlignBottom)
   {
	   tabHeight = fontBasedBounds.y;
   }
   else
   {
	   tabHeight = fontBasedBounds.x;
   }

   for( S32 i = 0; i < mPages.size(); i++ )
   {
      // Fetch Tab Width
      S32 tabWidth = calculatePageTabWidth( mPages[i].Page );
      tabWidth = getMax( tabWidth, mMinTabWidth );

	  if (i == 0)
	  {
		  mTabWidth = tabWidth;
	  }

      TabHeaderInfo &info = mPages[i];
      switch( mTabPosition )
      {
      case AlignTop:
      case AlignBottom:
         // If we're going to go outside our bounds
         // with this tab move it down a row
         if( currX + tabWidth > innerRect.extent.x )
         {
            // Calculate and Advance State.
            balanceRow( currRow, currX );
            info.TabRow = ++currRow;
            // Reset Necessaries
            info.TabColumn = currColumn = currX = 0;
         }
         else
         {
            info.TabRow = currRow;
            info.TabColumn = currColumn++;
         }

         // Calculate Tabs Bounding Rect
         info.TabRect.point.x  = currX;
		 info.TabRect.point.y = (info.TabRow * tabHeight);
         info.TabRect.extent.x = tabWidth;
         info.TabRect.extent.y = tabHeight;

         currX += tabWidth;
         break;
      case AlignLeft:
      case AlignRight:
         // If we're going to go outside our bounds
         // with this tab move it down a row
         if( currY + tabWidth > innerRect.extent.y )
         {
            // Balance Tab Column.
            balanceColumn( currColumn, currY );

            // Calculate and Advance State.
            info.TabColumn = ++currColumn;
            info.TabRow = currRow = currY = 0;
         }
         else
         {
            info.TabColumn = currColumn;
            info.TabRow = currRow++;
         }

         // Calculate Tabs Bounding Rect
		 info.TabRect.point.x = (info.TabColumn * tabHeight);
         info.TabRect.point.y  = currY;
		 info.TabRect.extent.x = tabHeight;
         info.TabRect.extent.y = tabWidth;

         currY += tabWidth;
         break;
      };
   }

   // The "+" tab goes after the last real one, laid out by the same rules but
   // square, so it reads as an affordance rather than a page with no name.
   //
   // It wraps like a tab too. That grows mTabRect and shrinks mPageRect, which
   // re-sizes every page - but only while the Gui is being authored, and not
   // durably: the book sizes each page from mPageRect whenever one is added, so
   // a Gui saved with the "+" on a row of its own loads back unchanged.
   //
   // Only the counter that survives the loop needs bumping. currRow feeds the
   // strip's height for a top or bottom book, currColumn its width for a left or
   // right one; the other is written and never read.
   if( wantAddTab )
   {
      const S32 addSize = tabHeight;

      switch( mTabPosition )
      {
      case AlignTop:
      case AlignBottom:
         // currX > 0 so a strip too narrow for even one square does not push the
         // "+" onto an empty row it still cannot fit on.
         if( currX + addSize > innerRect.extent.x && currX > 0 )
         {
            balanceRow( currRow, currX );
            currRow++;
            currX = 0;
         }

         mAddTabRect.point.x  = currX;
         mAddTabRect.point.y  = currRow * tabHeight;
         mAddTabRect.extent.x = addSize;
         mAddTabRect.extent.y = tabHeight;
         break;
      case AlignLeft:
      case AlignRight:
         if( currY + addSize > innerRect.extent.y && currY > 0 )
         {
            balanceColumn( currColumn, currY );
            currColumn++;
            currY = 0;
         }

         mAddTabRect.point.x  = currColumn * tabHeight;
         mAddTabRect.point.y  = currY;
         mAddTabRect.extent.x = tabHeight;
         mAddTabRect.extent.y = addSize;
         break;
      };
   }

   currRow++;
   currColumn++;

   Point2I colExtent = Point2I(currColumn * tabHeight, currRow * tabHeight);
   Point2I outerExtent = getOuterExtent(colExtent, NormalState, mProfile);

   // Extent before point, in every case. A bottom or right strip places itself
   // by measuring back from the far edge, so reading mTabRect.extent before this
   // pass has written it takes the size the strip was LAST time - zero on the
   // first pass after construction, which is the only pass a book gets when it
   // has no pages to add and so nothing to trigger a second one.
   switch( mTabPosition )
   {
   case AlignTop:
      mTabRect.extent.x = mBounds.extent.x;
      mTabRect.extent.y = outerExtent.y;
      mTabRect.point.x = 0;
      mTabRect.point.y = 0;

      mPageRect.point.x = 0;
      mPageRect.point.y = mTabRect.extent.y;
      mPageRect.extent.x = mTabRect.extent.x;
      mPageRect.extent.y = mBounds.extent.y - mTabRect.extent.y;

      break;
   case AlignBottom:
      mTabRect.extent.x = mBounds.extent.x;
      mTabRect.extent.y = outerExtent.y;
      mTabRect.point.x = 0;
      mTabRect.point.y = mBounds.extent.y - mTabRect.extent.y;

      mPageRect.point.x = 0;
      mPageRect.point.y = 0;
      mPageRect.extent.x = mTabRect.extent.x;
      mPageRect.extent.y = mBounds.extent.y - mTabRect.extent.y;

      break;
   case AlignLeft:
      mTabRect.extent.x = outerExtent.x;
      mTabRect.extent.y = mBounds.extent.y;
      mTabRect.point.x = 0;
      mTabRect.point.y = 0;

      mPageRect.point.x = mTabRect.extent.x;
      mPageRect.point.y = 0;
      mPageRect.extent.x = mBounds.extent.x - mTabRect.extent.x;
      mPageRect.extent.y = mBounds.extent.y;

      break;
   case AlignRight:
      mTabRect.extent.x = outerExtent.x;
      mTabRect.extent.y = mBounds.extent.y;
      mTabRect.point.x = mBounds.extent.x - mTabRect.extent.x;
      mTabRect.point.y = 0;

      mPageRect.point.x = 0;
      mPageRect.point.y = 0;
      mPageRect.extent.x = mBounds.extent.x - mTabRect.extent.x;
      mPageRect.extent.y = mTabRect.extent.y;

      break;
   };


}

void GuiTabBookCtrl::balanceColumn( S32 column , S32 totalTabWidth )
{
   // Short Circuit.
   //
   // If the tab size is zero, don't render tabs,
   //  and assume it's a tab-less tab-book - JDD
   if( mPages.empty())
      return;

   Vector<TabHeaderInfo*> rowTemp;
   rowTemp.clear();

   for( S32 i = 0; i < mPages.size(); i++ )
   {
      TabHeaderInfo &info = mPages[i];

      if(info.TabColumn == column )
         rowTemp.push_back( &mPages[i] );
   }

   if( rowTemp.empty() )
      return;

   // Balance the tabs across the remaining space
   RectI innerRect = getInnerRect();
   S32 spaceToDivide = innerRect.extent.y - totalTabWidth;
   S32 pointDelta    = 0;
   for( S32 i = 0; i < rowTemp.size(); i++ )
   {
      TabHeaderInfo &info = *rowTemp[i];
      S32 extraSpace = (S32)( spaceToDivide / rowTemp.size() );
      info.TabRect.extent.y += extraSpace;
      info.TabRect.point.y += pointDelta;
      pointDelta += extraSpace;
   }

}
void GuiTabBookCtrl::balanceRow( S32 row, S32 totalTabWidth )
{
   // Short Circuit.
   //
   // If the tab size is zero, don't render tabs,
   //  and assume it's a tab-less tab-book - JDD
   if( mPages.empty())
      return;

   Vector<TabHeaderInfo*> rowTemp;
   rowTemp.clear();

   for( S32 i = 0; i < mPages.size(); i++ )
   {
      TabHeaderInfo &info = mPages[i];

      if(info.TabRow == row )
         rowTemp.push_back( &mPages[i] );
   }

   if( rowTemp.empty() )
      return;

   // Balance the tabs across the remaining space
   RectI innerRect = getInnerRect();
   S32 spaceToDivide = innerRect.extent.x - totalTabWidth;
   S32 pointDelta    = 0;
   for( S32 i = 0; i < rowTemp.size(); i++ )
   {
      TabHeaderInfo &info = *rowTemp[i];
      S32 extraSpace = (S32)spaceToDivide / ( rowTemp.size() );
      info.TabRect.extent.x += extraSpace;
      info.TabRect.point.x += pointDelta;
      pointDelta += extraSpace;
   }
}


GuiTabPageCtrl *GuiTabBookCtrl::findHitTab( const GuiEvent &event )
{
   return findHitTab( event.mousePoint );
}

GuiTabPageCtrl *GuiTabBookCtrl::findHitTab( Point2I hitPoint )
{
   // Short Circuit.
   //
   // If the tab size is zero, don't render tabs,
   //  and assume it's a tab-less tab-book - JDD
   if( mPages.empty())
      return NULL;

   for( S32 i = 0; i < mPages.size(); i++ )
   {
      if( mPages[i].TabRect.pointInRect( hitPoint ) )
         return mPages[i].Page;
   }
   return NULL;
}

U32 GuiTabBookCtrl::getSelectedPage()
{
	U32 index = 0;

	for (U32 i = 0; i < mPages.size(); i++)
	{
		if (mActivePage == mPages[i].Page)
		{
			index = i;
			break;
		}
	}

	return index;
}

void GuiTabBookCtrl::selectPage( S32 index )
{
   if( index < 0 || index >= mPages.size())
      return;

   // Select the page
   selectPage( mPages[ index ].Page );
}


void GuiTabBookCtrl::selectPage( GuiTabPageCtrl *page )
{
   Vector<TabHeaderInfo>::iterator i = mPages.begin();
   for( ; i != mPages.end() ; i++ )
   {
      GuiTabPageCtrl *tab = reinterpret_cast<GuiTabPageCtrl*>((*i).Page);
      if( page == tab )
      {
         mActivePage = tab;
         tab->setVisible( true );

         // Notify User
         char *retBuffer = Con::getReturnBuffer( 512 );
         dStrcpy( retBuffer, tab->getText() );
         Con::executef( this, 2, "onTabSelected",  retBuffer );

      }
      else
         tab->setVisible( false );
   }
}

void GuiTabBookCtrl::selectPage( const char* pageName )
{
   Vector<TabHeaderInfo>::iterator i = mPages.begin();
   for( ; i != mPages.end() ; i++ )
   {
      GuiTabPageCtrl *tab = reinterpret_cast<GuiTabPageCtrl*>((*i).Page);
      if( dStricmp( pageName, tab->getText() ) == 0 )
      {
         mActivePage = tab;
         tab->setVisible( true );

         // Notify User
         char *retBuffer = Con::getReturnBuffer( 512 );
         dStrcpy( retBuffer, tab->getText() );
         Con::executef( this, 2, "onTabSelected",  retBuffer );

      }
      else
         tab->setVisible( false );
   }
}


bool GuiTabBookCtrl::onKeyDown(const GuiEvent &event)
{
   // Tab      = Next Page
   // Ctrl-Tab = Previous Page
   if( 0 && event.keyCode == KEY_TAB )
   {
      if( event.modifier & SI_CTRL )
         selectPrevPage();
      else 
         selectNextPage();

      return true;
   }

   return Parent::onKeyDown( event );
}

void GuiTabBookCtrl::selectNextPage()
{
   if( mPages.empty() )
      return;

   if( mActivePage == NULL )
      mActivePage = mPages[0].Page;

   S32 nI = 0;
   for( ; nI < mPages.size(); nI++ )
   {
      GuiTabPageCtrl *tab = mPages[ nI ].Page;
      if( tab == mActivePage )
      {
         if( nI == ( mPages.size() - 1 ) )
            selectPage( 0 );
         else if ( nI + 1 <= ( mPages.size() - 1 ) ) 
            selectPage( nI + 1 );
         else
            selectPage( 0 );

         // Notify User
         if( isMethod( "onTabSelected" ) )
         {
            char *retBuffer = Con::getReturnBuffer( 512 );
            dStrcpy( retBuffer, tab->getText() );
            Con::executef( this, 2, "onTabSelected",  retBuffer );
         }

         return;
      }
   }
}

void GuiTabBookCtrl::selectPrevPage()
{
   if( mPages.empty() )
      return;

   if( mActivePage == NULL )
      mActivePage = mPages[0].Page;

   S32 nI = 0;
   for( ; nI < mPages.size(); nI++ )
   {
      GuiTabPageCtrl *tab = mPages[ nI ].Page;
      if( tab == mActivePage )
      {
         if( nI == 0 )
            selectPage( mPages.size() - 1 );
         else
            selectPage( nI - 1 );

         // Notify User
         if( isMethod( "onTabSelected" ) )
         {
            char *retBuffer = Con::getReturnBuffer( 512 );
            dStrcpy( retBuffer, tab->getText() );
            Con::executef( this, 2, "onTabSelected",  retBuffer );
         }

         return;
      }
   }

}
