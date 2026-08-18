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
#include "platform/platform.h"
#include "gui/guiListBoxCtrl.h"
#include "gui/guiCanvas.h"
#include "persistence/taml/tamlCustom.h"
#include <algorithm>

#include "guiListBoxCtrl_ScriptBinding.h"

IMPLEMENT_CONOBJECT(GuiListBoxCtrl);

GuiListBoxCtrl::GuiListBoxCtrl()
{
   mItems.clear();
   mSelectedItems.clear();
   mMultipleSelections = true;
   mFitParentWidth = true;
   mItemSize = Point2I(10,20);
   mLastClickItem = NULL;
   mRendersChildren = false;
   mIsContainer = false;
   mActive = true;
   caller = this;

   setField("profile", "GuiListBoxProfile");
}

GuiListBoxCtrl::~GuiListBoxCtrl()
{
   clearItems();
}

void GuiListBoxCtrl::initPersistFields()
{
   Parent::initPersistFields();

   addField( "AllowMultipleSelections", TypeBool, Offset( mMultipleSelections, GuiListBoxCtrl) );
   addField( "FitParentWidth", TypeBool, Offset( mFitParentWidth, GuiListBoxCtrl) );
}

bool GuiListBoxCtrl::onWake()
{
   if( !Parent::onWake() )
	  return false;

   updateSize();

   return true;
}

void GuiListBoxCtrl::addObject(SimObject *obj)
{
	AssertWarn(0, "GuiListBoxCtrl::addObject: cannot add children to the GuiListBoxCtrl. It is not a container. Child was not added.");
	removeObject(obj);
}

#pragma region ItemAccessors
void GuiListBoxCtrl::clearItems()
{
   // Free item list allocated memory
   while( mItems.size() )
	  deleteItem( 0 );

   // Free our vector lists
   mItems.clear();
   mSelectedItems.clear();
}

void GuiListBoxCtrl::clearSelection()
{
   if( !mSelectedItems.size() )
	  return;

   VectorPtr<LBItem*>::iterator i = mSelectedItems.begin();
   for( ; i != mSelectedItems.end(); i++ )
	  (*i)->isSelected = false;

   mSelectedItems.clear();

   if (caller->isMethod("onUnselectAll"))
   {
	   Con::executef(caller, 1, "onUnselectAll");
   }
}

void GuiListBoxCtrl::removeSelection( S32 index )
{
   // Range Check
   if( index >= mItems.size() || index < 0 )
   {
	  Con::warnf("GuiListBoxCtrl::removeSelection - index out of range!" );
	  return;
   }

   removeSelection( mItems[index], index );
}
void GuiListBoxCtrl::removeSelection( LBItem *item, S32 index )
{
   if( !mSelectedItems.size() )
	  return;

   if( !item )
	  return;

   for( S32 i = 0 ; i < mSelectedItems.size(); i++ )
   {
	  if( mSelectedItems[i] == item )
	  {
		 mSelectedItems.erase( &mSelectedItems[i] );
		 item->isSelected = false;
		 if (caller->isMethod("onUnselect"))
		 {
			Con::executef(caller, 4, "onUnselect", Con::getIntArg( index ), item->itemText, Con::getIntArg(item->ID));
		 }
		 return;
	  }
   }
}

void GuiListBoxCtrl::addSelection( S32 index )
{
   // Range Check
   if( index >= mItems.size() || index < 0 )
   {
	  Con::warnf("GuiListBoxCtrl::addSelection- index out of range!" );
	  return;
   }

   addSelection( mItems[index], index );
}
void GuiListBoxCtrl::addSelection( LBItem *item, S32 index )
{
   if( !mMultipleSelections )
   {
	  if( !mSelectedItems.empty() )
	  {
		 LBItem* selItem = mSelectedItems.front();
		 if( selItem != item )
			clearSelection();
		 else
			return;
	  }
   }
   else
   {
	  if( !mSelectedItems.empty() )
	  {
		 for( S32 i = 0; i < mSelectedItems.size(); i++ )
		 {
			if( mSelectedItems[ i ] == item )
			   return;
		 }
	  }
   }

   item->isSelected = true;
   mSelectedItems.push_front( item );

   ScrollToIndex(index);

   if(caller->isMethod("onSelect"))
		Con::executef(caller, 4, "onSelect", Con::getIntArg( index ), item->itemText, Con::getIntArg(item->ID));

}

S32 GuiListBoxCtrl::getItemIndex( LBItem *item )
{
   if( mItems.empty() )
	  return -1;

   // Lookup the index of an item in our list, by the pointer to the item
   for( S32 i = 0; i < mItems.size(); i++ )
	  if( mItems[i] == item )
		 return i;

   return -1;
}

S32 GuiListBoxCtrl::getItemCount()
{
   return mItems.size();
}

S32 GuiListBoxCtrl::getSelCount()
{
   return mSelectedItems.size();
}

S32 GuiListBoxCtrl::getSelectedItem()
{
   if( mSelectedItems.empty() || mItems.empty() )
	  return -1;

   for( S32 i = 0 ; i < mItems.size(); i++ )
	  if( mItems[i]->isSelected )
		 return i;

   return -1;
}

void GuiListBoxCtrl::getSelectedItems( Vector<S32> &Items )
{
   // Clear our return vector
   Items.clear();
   
   // If there are no selected items, return an empty vector
   if( mSelectedItems.empty() )
	  return;
   
   for( S32 i = 0; i < mItems.size(); i++ )
	  if( mItems[i]->isSelected )
		 Items.push_back( i );
}

S32 GuiListBoxCtrl::findItemText( StringTableEntry text, bool caseSensitive )
{
   // Check Proper Arguments
   if( !text || !text[0] || text == StringTable->EmptyString )
   {
	  Con::warnf("GuiListBoxCtrl::findItemText - No Text Specified!");
	  return -1;
   }

   // Check Items Exist.
   if( mItems.empty() )
	  return -1;

   // Lookup the index of an item in our list, by the pointer to the item
   for( S32 i = 0; i < mItems.size(); i++ )
   {
	  // Case Sensitive Compare?
	  if( caseSensitive && ( dStrcmp( mItems[i]->itemText, text ) == 0 ) )
		 return i;
	  else if (!caseSensitive && ( dStricmp( mItems[i]->itemText, text ) == 0 ))
		 return i;
   }

   // Not Found!
   return -1;
}

void GuiListBoxCtrl::setSelectionInternal(StringTableEntry text)
{
	if(text != StringTable->EmptyString)
	{
		S32 index = findItemText(text);
		if (index != -1)
		{
			mSelectedItems.clear();
			LBItem *item = mItems[index];
			item->isSelected = true;
			mSelectedItems.push_front(item);
		}
	}
}

void GuiListBoxCtrl::setCurSel( S32 index )
{
   // Range Check
   if( index >= mItems.size() )
   {
	  Con::warnf("GuiListBoxCtrl::setCurSel - index out of range!" );
	  return;
   }

   // If index -1 is specified, we clear the selection
   if( index == -1 )
   {
		for (auto item : mItems)
		{
			item->isSelected = false;
		}
	  mSelectedItems.clear();
	  return;
   }

   // Add the selection
   addSelection( mItems[ index ], index );
}

void GuiListBoxCtrl::setCurSelRange( S32 start, S32 stop )
{
   // Verify Selection Range
   if( start < 0 )
	  start = 0;
   else if( start > mItems.size() )
	  start = mItems.size();

   if( stop < 0 )
	  stop = 0;
   else if( stop > mItems.size() )
	  stop = mItems.size();

   S32 iterStart = ( start < stop ) ? start : stop;
   S32 iterStop  = ( start < stop ) ? stop : start;

   for( ; iterStart <= iterStop; iterStart++ )
	  addSelection( mItems[iterStart], iterStart );
}

S32 GuiListBoxCtrl::addItem( StringTableEntry text, void *itemData )
{
   // This just calls insert item at the end of the list
   return insertItem( mItems.size(), text, itemData );
}

S32 GuiListBoxCtrl::addItemWithColor( StringTableEntry text, ColorF color, void *itemData )
{
   // This just calls insert item at the end of the list
   return insertItemWithColor( mItems.size(), text, color, itemData );
}

S32	GuiListBoxCtrl::addItemWithID(StringTableEntry text, S32 ID, void *itemData)
{
	S32 index = insertItem( mItems.size(), text, itemData);
	setItemID(index, ID);
	return index;
}

void GuiListBoxCtrl::setItemColor( S32 index, ColorF color )
{
   if ((index >= mItems.size()) || index < 0)
   {
	  Con::warnf("GuiListBoxCtrl::setItemColor - invalid index");
	  return;
   }

   LBItem* item = mItems[index];
   item->hasColor = true;
   item->color = color;
}

void GuiListBoxCtrl::clearItemColor(S32 index)
{
	if ((index >= mItems.size()) || index < 0)
	{
		Con::warnf("GuiListBoxCtrl::setItemColor - invalid index");
		return;
	}

	LBItem* item = mItems[index];
	item->hasColor = false;
}

void GuiListBoxCtrl::clearAllColors()
{
	if (!mSelectedItems.size())
		return;

	VectorPtr<LBItem*>::iterator i = mSelectedItems.begin();
	for (; i != mSelectedItems.end(); i++)
		(*i)->hasColor = false;
}

ColorF GuiListBoxCtrl::getItemColor(S32 index)
{
	if ((index >= mItems.size()) || index < 0)
	{
		Con::warnf("GuiListBoxCtrl::getItemColor - invalid index");
		return ColorF(0,0,0,0);
	}

	LBItem* item = mItems[index];
	return item->color;
}

bool GuiListBoxCtrl::getItemHasColor(S32 index)
{
	if ((index >= mItems.size()) || index < 0)
	{
		Con::warnf("GuiListBoxCtrl::getItemHasColor - invalid index");
		return false;
	}

	LBItem* item = mItems[index];
	return item->hasColor;
}

void GuiListBoxCtrl::setItemID(S32 index, S32 ID)
{
	if ((index >= mItems.size()) || index < 0)
	{
		Con::warnf("GuiListBoxCtrl::setItemID - invalid index");
		return;
	}

	LBItem* item = mItems[index];
	item->ID = ID;
}

S32 GuiListBoxCtrl::getItemID(S32 index)
{
	if ((index >= mItems.size()) || index < 0)
	{
		Con::warnf("GuiListBoxCtrl::setItemID - invalid index");
		return 0;
	}

	LBItem* item = mItems[index];
	return item->ID;
}

S32 GuiListBoxCtrl::findItemID(S32 ID)
{
	// Check Items Exist.
	if (mItems.empty())
		return -1;

	// Lookup the index of an item in our list, by the pointer to the item
	for (S32 i = 0; i < mItems.size(); i++)
	{
		if (mItems[i]->ID == ID)
		{
			return i;
		}
	}

	// Not Found!
	return -1;
}

void GuiListBoxCtrl::setItemActive(S32 index)
{
	if ((index >= mItems.size()) || index < 0)
	{
		Con::warnf("GuiListBoxCtrl::setItemActive - invalid index");
		return;
	}

	LBItem* item = mItems[index];
	item->isActive = true;
}

void GuiListBoxCtrl::setItemInactive(S32 index)
{
	if ((index >= mItems.size()) || index < 0)
	{
		Con::warnf("GuiListBoxCtrl::setItemInactive - invalid index");
		return;
	}

	LBItem* item = mItems[index];
	item->isActive = false;
}

bool GuiListBoxCtrl::getItemActive(S32 index)
{
	if ((index >= mItems.size()) || index < 0)
	{
		Con::warnf("GuiListBoxCtrl::getItemActive - invalid index");
		return true;
	}

	LBItem* item = mItems[index];
	return item->isActive;
}

S32 GuiListBoxCtrl::insertItem( S32 index, StringTableEntry text, void *itemData )
{
   // If the index is greater than our list size, insert it at the end
   if( index >= mItems.size() )
	  index = mItems.size();

   // Sanity checking
   if( !text )
   {
 	  Con::warnf("GuiListBoxCtrl::insertItem - cannot add NULL string" );
	  return -1;
   }

   LBItem *newItem = createItem();
   if( !newItem )
   {
	  Con::warnf("GuiListBoxCtrl::insertItem - error allocating item memory!" );
	  return -1;
   }

   // Assign item data
   newItem->itemText = StringTable->insert(text, true);
   newItem->isSelected = false;
   newItem->isActive = true;
   newItem->ID = 0;
   newItem->itemData = itemData;
   newItem->hasColor = false;

   // Add to list
   mItems.insert(index);
   mItems[index] = newItem;

   // Resize our list to fit our items
   updateSize();

   // Return our index in list (last)
   return index;
}

GuiListBoxCtrl::LBItem* GuiListBoxCtrl::createItem()
{
	LBItem* newItem = new LBItem;
	if (!newItem)
	{
		return nullptr;
	}
	return newItem;
}

S32 GuiListBoxCtrl::insertItemWithColor( S32 index, StringTableEntry text, ColorF color, void *itemData )
{
	if( color == ColorF(-1, -1, -1) )
	{
		Con::warnf("GuiListBoxCtrl::insertItem - cannot add NULL color" );
			return -1;
	}

	index = insertItem(index, text, itemData);

	if(index != -1)
	{
		setItemColor(index, color);
	}

	// Return our index in list (last)
	return index;
}

void  GuiListBoxCtrl::deleteItem( S32 index )
{
   // Range Check
   if( index >= mItems.size() || index < 0 )
   {
	  Con::warnf("GuiListBoxCtrl::deleteItem - index out of range!" );
	  return;
   }

   // Grab our item
   LBItem* item = mItems[ index ];
   if( !item )
   {
	  Con::warnf("GuiListBoxCtrl::deleteItem - Bad Item Data!" );
	  return;
   }

   // Remove it from the selected list.
   if( item->isSelected )
   {
	  for( VectorPtr<LBItem*>::iterator i = mSelectedItems.begin(); i != mSelectedItems.end(); i++ )
	  {
		 if( item == *i )
		 {
			mSelectedItems.erase_fast( i );
			break;
		 }
	  }
   }

   // Remove it from the list
   mItems.erase( &mItems[ index ] );

   // Free the memory associated with it
   delete item;
}

StringTableEntry GuiListBoxCtrl::getItemText( S32 index )
{
   // Range Checking
   if( index > mItems.size() || index < 0 )
   {
	  Con::warnf( "GuiListBoxCtrl::getItemText - index out of range!" );
	  return StringTable->EmptyString;
   }
   
   return mItems[ index ]->itemText;   
}

void GuiListBoxCtrl::setItemText( S32 index, StringTableEntry text )
{
   // Sanity Checking
   if( !text )
   {
	  Con::warnf("GuiListBoxCtrl::setItemText - Invalid Text Specified!" );
	  return;
   }
   // Range Checking
   if( index > mItems.size() || index < 0 )
   {
	  Con::warnf( "GuiListBoxCtrl::getItemText - index out of range!" );
	  return;
   }

   mItems[ index ]->itemText = StringTable->insert( text );
}
#pragma endregion

#pragma region Persistence
//-----------------------------------------------------------------------------
// Static rows: the ones a list is authored with rather than filled with at
// runtime.
//
// An item is not a field and not a child object - GuiListBoxCtrl::addObject
// refuses children outright - so neither of the two things a writer walks can
// see one. They go out as TAML custom nodes instead, which is what
// GuiFrameSetCtrl does with its frame tree for exactly the same reason:
//
//     <GuiListBoxCtrl Extent="180 120">
//         <GuiListBoxCtrl.Items>
//             <Item Text="Easy" ID="1" />
//             <Item Text="Normal" ID="2" Selected="1" />
//             <Item Text="Hard" ID="3" Color="1 0 0 1" Active="0" />
//         </GuiListBoxCtrl.Items>
//     </GuiListBoxCtrl>
//
// TamlXmlWriter::compileCustomElements prefixes the section with the element
// name it is writing, so a subclass gets <GuiDropDownCtrl.Items> with nothing
// extra to do, and the reader's findNode() sees the unprefixed name either way.
//
// Everything but the caption is written only when it differs from what LBItem's
// constructor sets, so an ordinary list of captions stays one attribute a row.
//
// The two limitations are the frame set's, and worth naming: the legacy .gui
// script writer knows nothing of custom nodes, so a Gui saved in that format
// loses its rows (the Gui Editor's save dialog says so), and a deep clone has
// to copy them by hand - see deepCloneChildren below.
//-----------------------------------------------------------------------------

// Interned case-INSENSITIVELY, which is what every one of these is compared
// against: TamlCustomNodes::findNode and the XML parser both intern with the
// default, so a name interned the case-sensitive way is a different pointer and
// silently matches nothing.
//
// It is not theoretical. StringTable hands back the first spelling of a name it
// was ever given, so a case-sensitive "ID" here wrote itself out as whatever
// spelling reached the table first - "Id", from somewhere else in the engine -
// and then failed to recognise it on the way back in, dropping every ID in the
// file with a warning. Which spelling wins depends on static initialisation
// order across translation units, so it can change from one build to the next.
//
// It also means a hand-edited file may spell these however it likes.
static StringTableEntry itemsNodeName		= StringTable->insert("Items");
static StringTableEntry itemNodeName		= StringTable->insert("Item");
static StringTableEntry itemTextName		= StringTable->insert("Text");
static StringTableEntry itemIDName			= StringTable->insert("ID");
static StringTableEntry itemActiveName		= StringTable->insert("Active");
static StringTableEntry itemSelectedName	= StringTable->insert("Selected");
static StringTableEntry itemColorName		= StringTable->insert("Color");

void GuiListBoxCtrl::onTamlCustomWrite(TamlCustomNodes& customNodes)
{
	// Debug Profiling.
	PROFILE_SCOPE(GuiListBoxCtrl_OnTamlCustomWrite);

	// Call parent.
	Parent::onTamlCustomWrite(customNodes);

	if (!writesItems() || mItems.size() == 0)
	{
		return;
	}

	TamlCustomNode* pItemsNode = customNodes.addNode(itemsNodeName);

	for (S32 i = 0; i < mItems.size(); i++)
	{
		LBItem* item = mItems[i];
		TamlCustomNode* pItemNode = pItemsNode->addNode(itemNodeName);

		pItemNode->addField(itemTextName, item->itemText);

		if (item->ID != 0)
		{
			pItemNode->addField(itemIDName, item->ID);
		}
		if (!item->isActive)
		{
			pItemNode->addField(itemActiveName, item->isActive);
		}
		if (item->isSelected)
		{
			pItemNode->addField(itemSelectedName, item->isSelected);
		}
		if (item->hasColor)
		{
			pItemNode->addField(itemColorName, item->color);
		}
	}
}

void GuiListBoxCtrl::onTamlCustomRead(const TamlCustomNodes& customNodes)
{
	// Debug Profiling.
	PROFILE_SCOPE(GuiListBoxCtrl_OnTamlCustomRead);

	// Call parent.
	Parent::onTamlCustomRead(customNodes);

	const TamlCustomNode* pItemsNode = customNodes.findNode(itemsNodeName);

	if (pItemsNode == NULL)
	{
		return;
	}

	// Whatever the control was built holding. A list read into an object that
	// already has rows is a replacement, not an append.
	clearItems();

	const TamlCustomNodeVector& itemNodes = pItemsNode->getChildren();
	for (TamlCustomNodeVector::const_iterator nodeItr = itemNodes.begin(); nodeItr != itemNodes.end(); ++nodeItr)
	{
		const TamlCustomNode* pItemNode = *nodeItr;

		if (pItemNode->getNodeName() != itemNodeName)
		{
			Con::warnf("GuiListBoxCtrl::onTamlCustomRead() - Unknown tag name of '%s'. Only '%s' is valid.",
				pItemNode->getNodeName(), itemNodeName);
			continue;
		}

		// Made before the fields are read, because every field below writes into
		// it. A row with no Text at all is still a row - an empty caption is a
		// legal item, and the editor can make one.
		LBItem* item = appendItemInternal(StringTable->EmptyString);
		if (!item)
		{
			continue;
		}

		const TamlCustomFieldVector& fields = pItemNode->getFields();
		for (TamlCustomFieldVector::const_iterator fieldItr = fields.begin(); fieldItr != fields.end(); ++fieldItr)
		{
			const TamlCustomField* pField = *fieldItr;
			StringTableEntry fieldName = pField->getFieldName();

			if (fieldName == itemTextName)
			{
				item->itemText = StringTable->insert(pField->getFieldValue(), true);
			}
			else if (fieldName == itemIDName)
			{
				pField->getFieldValue(item->ID);
			}
			else if (fieldName == itemActiveName)
			{
				pField->getFieldValue(item->isActive);
			}
			else if (fieldName == itemSelectedName)
			{
				pField->getFieldValue(item->isSelected);
			}
			else if (fieldName == itemColorName)
			{
				pField->getFieldValue(item->color);
				item->hasColor = true;
			}
			else
			{
				Con::warnf("GuiListBoxCtrl::onTamlCustomRead() - Encountered an unknown field name of '%s'.", fieldName);
			}
		}

		// The selection list is kept beside the items and has to agree with them.
		if (item->isSelected)
		{
			mSelectedItems.push_front(item);
		}
	}

	updateSize();
}

//-----------------------------------------------------------------------------
// The list as one string, for the two callers that want all of it at once: the
// Gui Editor's Items section, which edits the list as a whole rather than a row
// at a time, and its undo stack, which records what the list was and what it
// became. The same job getFrameLayout/setFrameLayout do for a frame set, and
// like those it is opaque - the file format is the custom nodes above.
//
// One record per item, newline separated; TAB between fields, in a fixed order:
//
//     text  ID  active  selected  hasColor  "r g b a"
//
// A caption may hold neither character. A list row is one line, and a
// GuiTextEditCtrl can produce neither - Enter commits (handleEnterKey has no
// insert path) and Tab moves the focus - but setItemList strips them anyway,
// because a caption arriving from script has been through neither.
//-----------------------------------------------------------------------------

const char* GuiListBoxCtrl::getItemList()
{
	// Measured rather than guessed: a list is any length, and the return buffer
	// has to be asked for at the size it will actually need. 64 covers the five
	// numbers, the four color components and the separators.
	U32 size = 1;
	for (S32 i = 0; i < mItems.size(); i++)
	{
		size += dStrlen(mItems[i]->itemText) + 64;
	}

	char* buffer = Con::getReturnBuffer(size);
	buffer[0] = '\0';

	U32 used = 0;
	for (S32 i = 0; i < mItems.size(); i++)
	{
		LBItem* item = mItems[i];

		// A row without a color has no color to write: ColorF's default
		// constructor is empty (gColor.h), so item->color is whatever was in
		// that memory until something sets it. Printing it would make two
		// identical lists read as different strings, which is the one thing an
		// opaque round-trippable form may not do.
		ColorF color = item->hasColor ? item->color : ColorF(0.0f, 0.0f, 0.0f, 0.0f);

		used += dSprintf(buffer + used, size - used, "%s%s\t%d\t%d\t%d\t%d\t%g %g %g %g",
			(i == 0) ? "" : "\n",
			item->itemText,
			item->ID,
			item->isActive ? 1 : 0,
			item->isSelected ? 1 : 0,
			item->hasColor ? 1 : 0,
			color.red, color.green, color.blue, color.alpha);
	}

	return buffer;
}

// One field of a record, copied out of the middle of the string it sits in.
// Returns the buffer, so it reads as an argument to dAtoi and friends.
static const char* copyItemField(char* buffer, const U32 size, const char* start, S32 length)
{
	if (length < 0)
	{
		length = 0;
	}
	if ((U32)length >= size)
	{
		length = size - 1;
	}

	dMemcpy(buffer, start, length);
	buffer[length] = '\0';

	return buffer;
}

void GuiListBoxCtrl::setItemList(const char* itemList)
{
	// The order the fields are written in above. A record may stop short of all
	// six: a script handing over a bare list of captions is a legal list, and
	// every field left off keeps whatever LBItem's constructor gave it.
	const S32 fieldMax = 6;

	clearItems();

	for (const char* at = itemList; at != NULL && *at; )
	{
		const char* recordEnd = dStrchr(at, '\n');
		if (recordEnd == NULL)
		{
			recordEnd = at + dStrlen(at);
		}

		// Split on TAB by pointer rather than through getWord or getUnit: the
		// first field is a caption, which may be empty and may hold spaces, and
		// neither of those survives a word split.
		const char* field[fieldMax];
		S32 fieldLength[fieldMax];
		S32 fieldCount = 0;

		const char* fieldStart = at;
		for (const char* scan = at; scan <= recordEnd; scan++)
		{
			if (scan != recordEnd && *scan != '\t')
			{
				continue;
			}

			if (fieldCount < fieldMax)
			{
				field[fieldCount] = fieldStart;
				fieldLength[fieldCount] = (S32)(scan - fieldStart);
				fieldCount++;
			}
			fieldStart = scan + 1;
		}

		char scratch[1024];

		LBItem* item = appendItemInternal(StringTable->insert(
			copyItemField(scratch, sizeof(scratch), field[0], fieldLength[0]), true));

		if (item != NULL)
		{
			if (fieldCount > 1)
			{
				item->ID = dAtoi(copyItemField(scratch, sizeof(scratch), field[1], fieldLength[1]));
			}
			if (fieldCount > 2)
			{
				item->isActive = dAtob(copyItemField(scratch, sizeof(scratch), field[2], fieldLength[2]));
			}
			if (fieldCount > 3)
			{
				item->isSelected = dAtob(copyItemField(scratch, sizeof(scratch), field[3], fieldLength[3]));
			}
			if (fieldCount > 4)
			{
				item->hasColor = dAtob(copyItemField(scratch, sizeof(scratch), field[4], fieldLength[4]));
			}
			if (fieldCount > 5 && item->hasColor)
			{
				const char* colorText = copyItemField(scratch, sizeof(scratch), field[5], fieldLength[5]);
				Con::setData(TypeColorF, &item->color, 0, 1, &colorText);
			}

			// The selection list is kept beside the items and has to agree with
			// them, or getSelectedItem and the render disagree about what is on.
			if (item->isSelected)
			{
				mSelectedItems.push_front(item);
			}
		}

		at = (*recordEnd == '\0') ? recordEnd : (recordEnd + 1);
	}

	// The list is the control's size, so the canvas has to be told. This is what
	// makes a row typed in the Gui Editor appear under the cursor as it is typed.
	updateSize();
	setUpdate();
}

// A row added with none of the noise the public paths make: insertItem resizes
// the control once per item and addSelection scrolls and fires onSelect, and
// neither belongs in the middle of loading a list.
GuiListBoxCtrl::LBItem* GuiListBoxCtrl::appendItemInternal(StringTableEntry text)
{
	LBItem* newItem = createItem();
	if (!newItem)
	{
		Con::warnf("GuiListBoxCtrl::appendItemInternal - error allocating item memory!");
		return NULL;
	}

	newItem->itemText = text;
	mItems.push_back(newItem);

	return newItem;
}

// Items are neither fields nor children, so a clone made by copying both comes
// back with an empty list. GuiFrameSetCtrl overrides the same phase for the same
// reason, and it is what makes the Gui Editor's copy, cut and paste carry the
// rows without knowing anything about them.
void GuiListBoxCtrl::deepCloneChildren(SimObject* clone)
{
	Parent::deepCloneChildren(clone);

	GuiListBoxCtrl* pListBox = dynamic_cast<GuiListBoxCtrl*>(clone);
	if (pListBox == NULL)
	{
		return;
	}

	pListBox->setItemList(getItemList());
}
#pragma endregion

#pragma region Sizing
void GuiListBoxCtrl::updateSize()
{
   if( !mProfile || !mProfile->getFont(mFontSizeAdjust))
	  return;

   GFont *font = mProfile->getFont(mFontSizeAdjust);
   Point2I contentSize = Point2I(10, font->getHeight() + 2);

   if (!mFitParentWidth)
   {
	  // Find the maximum width cell:
	  S32 maxWidth = 1;
	  for ( U32 i = 0; i < (U32)mItems.size(); i++ )
	  {
		 S32 width = font->getStrWidth( mItems[i]->itemText );
		 if( width > maxWidth )
			maxWidth = width;
	  }
	  contentSize.x = maxWidth + 6;
   }

   mItemSize = this->getOuterExtent(contentSize, NormalState, mProfile);
   Point2I newExtent = Point2I(mItemSize.x, mItemSize.y * mItems.size());

   //Don't update the extent.x if we are matching our parent's size. We will handle it during rendering.
   if (mFitParentWidth)
   {
	   newExtent.x = mBounds.extent.x;
   }

   resize( mBounds.point, newExtent );
}

void GuiListBoxCtrl::parentResized(const Point2I &oldParentExtent, const Point2I &newParentExtent)
{
   Parent::parentResized( oldParentExtent, newParentExtent );

   updateSize();
}
#pragma endregion

#pragma region Rendering
void GuiListBoxCtrl::onRender( Point2I offset, const RectI &updateRect )
{
	RectI clip = dglGetClipRect();
	if (mFitParentWidth && ( mBounds.extent.x != clip.extent.x || mItemSize.x != clip.extent.x))
	{
		mBounds.extent.x = clip.extent.x;
		mItemSize.x = clip.extent.x;
	}

	for ( S32 i = 0; i < mItems.size(); i++)
	{
		// Only render visible items
		if ((i + 1) * mItemSize.y + offset.y < updateRect.point.y)
		continue;

		// Break out once we're no longer in visible item range
		if( i * mItemSize.y + offset.y >= updateRect.point.y + updateRect.extent.y)
		break;

		RectI itemRect = RectI( offset.x, offset.y + ( i * mItemSize.y ), mItemSize.x, mItemSize.y );

		// Render our item
		onRenderItem( itemRect, mItems[i] );
	}
}

void GuiListBoxCtrl::onRenderItem( RectI &itemRect, LBItem *item )
{
   Point2I cursorPt = Point2I(0,0);
   GuiCanvas *root = getRoot();
   if (root)
   {
	   cursorPt = root->getCursorPos();
   }
   GuiControlState currentState = GuiControlState::NormalState;
   if (!mActive || !item->isActive)
	   currentState = GuiControlState::DisabledState;
   else if (item->isSelected)
	   currentState = GuiControlState::SelectedState;
   else if (itemRect.pointInRect(cursorPt))
	   currentState = GuiControlState::HighlightState;
   RectI ctrlRect = applyMargins(itemRect.point, itemRect.extent, currentState, mProfile);

   if (!ctrlRect.isValidRect())
   {
	   return;
   }

   renderUniversalRect(ctrlRect, mProfile, currentState);

   //Render Text
   dglSetBitmapModulation(getFontColor(mProfile, currentState));
   RectI fillRect = applyBorders(ctrlRect.point, ctrlRect.extent, currentState, mProfile);
   RectI contentRect = applyPadding(fillRect.point, fillRect.extent, currentState, mProfile);

   // Render color bullet if needed
   if (item->hasColor)
   {
	   RectI drawArea = RectI(contentRect.point.x, contentRect.point.y, contentRect.extent.y, contentRect.extent.y);
	  ColorI color = item->color;
	   renderColorBullet(drawArea, color, 5);

	   contentRect.point.x += contentRect.extent.y;
	   contentRect.extent.x -= contentRect.extent.y;
   }

   renderText(contentRect.point, contentRect.extent, item->itemText, mProfile);
}

void GuiListBoxCtrl::ScrollToIndex(const S32 targetIndex)
{
	GuiScrollCtrl* parent = dynamic_cast<GuiScrollCtrl *>(getParent());
	if (parent)
		parent->scrollRectVisible(RectI(0, mItemSize.y * targetIndex, mItemSize.x, mItemSize.y));
}
#pragma endregion

#pragma region InputEvents
void GuiListBoxCtrl::onTouchDragged(const GuiEvent &event)
{
	if (!mActive || !mVisible)
	{
		return;
	}

	S32 hitIndex = getHitIndex(event);

   if (hitIndex >= mItems.size() || hitIndex == -1)
	   return;

   LBItem *hitItem = mItems[hitIndex];
   if (hitItem == NULL || !hitItem->isActive)
	   return;

	if(caller->isMethod("onTouchDragged"))
	{
		Con::executef(caller, 4, "onTouchDragged", Con::getIntArg(hitIndex), hitItem->itemText, Con::getIntArg(hitItem->ID));
	}
	else
	{
		GuiControl* parent = getParent();
		if (parent)
			parent->onTouchDragged(event);
	}
}

void GuiListBoxCtrl::onTouchDown( const GuiEvent &event )
{
	if (!mActive || !mVisible)
	{
		return;
	}

	setFirstResponder();

   S32 hitIndex = getHitIndex(event);

   if ( hitIndex >= mItems.size() || hitIndex == -1 )
	  return;

   LBItem *hitItem = mItems[ hitIndex ];
   if ( hitItem == NULL || !hitItem->isActive)
	  return;

	handleItemClick(hitItem, hitIndex, event);
}

S32 GuiListBoxCtrl::getHitIndex(const GuiEvent& event)
{
	Point2I localPoint = globalToLocalCoord(event.mousePoint);
	return (localPoint.y < 0) ? -1 : (S32)mFloor((F32)localPoint.y / (F32)mItemSize.y);
}

void GuiListBoxCtrl::handleItemClick(LBItem* hitItem, S32 hitIndex, const GuiEvent& event)
{
   if( !mMultipleSelections )
   {
	  handleItemClick_SingleSelection(hitItem, hitIndex, event);
   }
   else
   {
	   if (!handleItemClick_MultiSelection(hitItem, hitIndex, event))
	   {
		   return;
	   }
   }

   handleItemClick_ClickCallbacks(hitItem, hitIndex, event);
   mLastClickItem = hitItem;
}

void GuiListBoxCtrl::handleItemClick_SingleSelection(LBItem* hitItem, S32 hitIndex, const GuiEvent& event)
{
	// No current selection?  Just select the cell and move on
	S32 selItem = getSelectedItem();

	if (selItem != hitIndex && selItem != -1)
		clearSelection();

	// Set the current selection
	setCurSel(hitIndex);
}

bool GuiListBoxCtrl::handleItemClick_MultiSelection(LBItem* hitItem, S32 hitIndex, const GuiEvent& event)
{
	// Deal with multiple selections
	if (event.modifier & SI_CTRL)
	{
		// Ctrl-Click toggles selection
		if (hitItem->isSelected)
		{
			removeSelection(hitItem, hitIndex);

			// We return here when we deselect an item because we don't store last clicked when we deselect
			return false;
		}
		else
			addSelection(hitItem, hitIndex);
	}
	else if (event.modifier & SI_SHIFT)
	{
		if (!mLastClickItem)
			addSelection(hitItem, hitIndex);
		else
			setCurSelRange(getItemIndex(mLastClickItem), hitIndex);
	}
	else
	{
		if (getSelCount() != 0)
		{
			S32 selItem = getSelectedItem();
			if (selItem != -1 && mItems[selItem] != hitItem)
				clearSelection();
		}
		addSelection(hitItem, hitIndex);
	}
	return true;
}

void GuiListBoxCtrl::handleItemClick_ClickCallbacks(LBItem* hitItem, S32 hitIndex, const GuiEvent& event)
{
	if (hitItem == mLastClickItem && event.mouseClickCount == 2)
	{
		if (caller->isMethod("onDoubleClick"))
			Con::executef(caller, 4, "onDoubleClick", Con::getIntArg(hitIndex), hitItem->itemText, Con::getIntArg(hitItem->ID));
	}
	else if (caller->isMethod("onClick"))
	{
		Con::executef(caller, 4, "onClick", Con::getIntArg(hitIndex), hitItem->itemText, Con::getIntArg(hitItem->ID));
	}
}

bool GuiListBoxCtrl::onKeyDown(const GuiEvent &event)
{
	//if this control is a dead end, make sure the event stops here
	if (!mVisible || !mActive || !mAwake || mItems.size() == 0)
		return true;

	S32 index = getSelectedItem();
	switch (event.keyCode)
	{
	case KEY_RETURN:
		if (mAltConsoleCommand[0])
			Con::evaluate(mAltConsoleCommand, false);
		return true;
	case KEY_LEFT:
	case KEY_UP:
		if (index == -1)
		{
			//Select the bottom item
			addSelection(mItems.size() - 1);
			return true;
		}
		else if(index != 0)
		{
			addSelection(index - 1);
			return true;
		}
		break;
	case KEY_DOWN:
	case KEY_RIGHT:
		if (index == -1)
		{
			//Select the top item
			addSelection(0);
			return true;
		}
		else if (index != (mItems.size() - 1))
		{
			addSelection(index + 1);
			return true;
		}
		break;
	case KEY_HOME:
		addSelection(0);
		return true;
	case KEY_END:
		addSelection(mItems.size() - 1);
		return true;
	case KEY_DELETE:
		if (index != -1 && isMethod("onDeleteKey"))
			Con::executef(caller, 4, "onDeleteKey", Con::getIntArg(index), getItemText(index), Con::getIntArg(getItemID(index)));

		return true;
	default:
		return Parent::onKeyDown(event);
	};
	return false;
}
#pragma endregion

#pragma region sorting
bool GuiListBoxCtrl::LBItem::sIncreasing = true;

void GuiListBoxCtrl::sortByText(bool increasing)
{
	if (mItems.size() < 2)
		return;

	LBItem::sIncreasing = increasing;
	std::sort(mItems.begin(), mItems.end(), LBItem::compByText);
}

void GuiListBoxCtrl::sortByID(bool increasing)
{
	if (mItems.size() < 2)
		return;

	LBItem::sIncreasing = increasing;
	std::sort(mItems.begin(), mItems.end(), LBItem::compByID);
}
#pragma endregion