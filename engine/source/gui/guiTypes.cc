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
#include "platform/types.h"
#include "console/consoleTypes.h"
#include "console/console.h"
#include "console/consoleInternal.h"
#include "console/codeBlock.h"
#include "graphics/gFont.h"
#include "graphics/dgl.h"
#include "gui/guiTypes.h"
#include "graphics/gBitmap.h"
#include "graphics/TextureManager.h"

// -=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=- //
//------------------------------------------------------------------------------
// The theme-managed field names, shared by the membership glue on GuiCursor,
// GuiBorderProfile and GuiControlProfile.
static StringTableEntry themeCategoryField()
{
	static StringTableEntry categoryField = StringTable->insert("category");
	return categoryField;
}

static StringTableEntry themeOverridesField()
{
	static StringTableEntry overridesField = StringTable->insert("themeOverrides");
	return overridesField;
}

IMPLEMENT_CONOBJECT(GuiCursor);

GuiCursor::GuiCursor()
{
   mHotSpot.set(0,0);
   mRenderOffset.set(0.0f,0.0f);
   mExtent.set(1,1);
   mBitmapName = StringTable->EmptyString;

   // White is the identity for bitmap modulation, so an untinted cursor draws
   // exactly as it did before this field existed.
   mColor.set(255, 255, 255, 255);
   mCategory = StringTable->EmptyString;
}

GuiCursor::~GuiCursor()
{
}

void GuiCursor::initPersistFields()
{
   Parent::initPersistFields();

   // hotSpot and renderOffset both shift where the art lands, and they are not
   // redundant. hotSpot is a pixel nudge; renderOffset is a fraction of the
   // bitmap's own size, so "0.5 0.5" means "centered on the pointer" whatever
   // the art measures. That is what stops a 13x17 arrow and a 32x32 sizer from
   // appearing to leap when one replaces the other. The pointer ends up at
   // hotSpot + (extent * renderOffset) within the image; see render().
   addField("hotSpot",     TypePoint2I,   Offset(mHotSpot, GuiCursor));
   addField("renderOffset",TypePoint2F,   Offset(mRenderOffset, GuiCursor));
   // Written relative to the game root; see getRelativeBitmapName.
   addProtectedField("bitmapName", TypeFilename, Offset(mBitmapName, GuiCursor), &defaultProtectedSetFn, &getBitmapName, "Bitmap drawn for this cursor");
   addField("color",       TypeColorI,    Offset(mColor, GuiCursor));

   addField("category", TypeString, Offset(mCategory, GuiCursor));
   // The offset is unused: both accessors are supplied and the setter returns false.
   addProtectedField("themeOverrides", TypeString, Offset(mCategory, GuiCursor), &setThemeOverrides, &getThemeOverrides, "Theme-overridden field names");
}

bool GuiCursor::onAdd()
{
   if(!Parent::onAdd())
	  return false;

   Sim::getGuiDataGroup()->addObject(this);

   return true;
}

void GuiCursor::onRemove()
{
   Parent::onRemove();
}

const Point2I& GuiCursor::resolve()
{
   if (!mTextureHandle && mBitmapName && mBitmapName[0])
   {
	  mTextureHandle = TextureHandle(mBitmapName, TextureHandle::BitmapTexture);
	  if (mTextureHandle)
		 mExtent.set(mTextureHandle.getWidth(), mTextureHandle.getHeight());
   }

   return mExtent;
}

void GuiCursor::render(const Point2I &pos)
{
   resolve();
   if(!mTextureHandle)
	  return;

   // Render the cursor centered according to dimensions of texture
   S32 texWidth = mTextureHandle.getWidth();
   S32 texHeight = mTextureHandle.getHeight();

   Point2I renderPos = pos;
   renderPos.x -= (S32)( texWidth  * mRenderOffset.x );
   renderPos.y -= (S32)( texHeight * mRenderOffset.y );

   // The stock cursors are grayscale - black outline, white body - so this
   // colors the body and leaves the outline. mColor starts white, which is what
   // dglClearBitmapModulation sets, so an untinted cursor is unaffected.
   dglSetBitmapModulation(mColor);
   dglDrawBitmap(mTextureHandle, renderPos);
   dglClearBitmapModulation();
}

StringTableEntry GuiCursor::getRelativeBitmapName( void ) const
{
	if (mBitmapName == NULL || mBitmapName == StringTable->EmptyString)
		return mBitmapName;

	// Only a path inside the game is made relative. Art somewhere else - another
	// drive, a folder beside the repository - is left alone: a "../.." chain
	// climbing out of the game folder is no more portable than the absolute path
	// it came from. Same rule as GuiControlProfile's bitmap.
	StringTableEntry gameRoot = Platform::getMainDotCsDir();
	if (!Con::isBasePath(mBitmapName, gameRoot))
		return mBitmapName;

	return Platform::makeRelativePathName(mBitmapName, gameRoot);
}

void GuiCursor::setTheme(GuiProfileTheme* theme, bool preserveOverrides)
{
	if (mThemeMembership.mTheme == theme)
		return;

	if (mThemeMembership.mTheme != NULL)
		clearNotify(mThemeMembership.mTheme);

	mThemeMembership.mTheme = theme;
	if (!preserveOverrides)
		mThemeMembership.clearAll();

	if (theme != NULL)
		deleteNotify(theme);
}

// A cursor's art - which bitmap, and where the pointer sits within it - is the
// one thing a theme cannot derive from a palette, so a theme sets these once
// when it creates the member and never stamps them again. That makes them
// unlike every other member field: there is no theme value behind them to
// override or to reset to, and they must persist whether or not anything marked
// them. Only "color" is stamped, and only it takes part in override tracking.
static bool isCursorArtField(StringTableEntry field)
{
	static StringTableEntry bitmapField = StringTable->insert("bitmapName");
	static StringTableEntry hotSpotField = StringTable->insert("hotSpot");
	static StringTableEntry renderOffsetField = StringTable->insert("renderOffset");

	return field == bitmapField || field == hotSpotField || field == renderOffsetField;
}

void GuiCursor::onStaticModified(const char* slotName, const char* newValue)
{
	Parent::onStaticModified(slotName, newValue);

	StringTableEntry slot = StringTable->insert(slotName);

	// The texture is loaded once and cached, so pointing a live cursor at new
	// art has to drop it - otherwise the old bitmap draws forever and the
	// extent, which the hot spot is measured against, stays wrong.
	static StringTableEntry bitmapField = StringTable->insert("bitmapName");
	if (slot == bitmapField)
	{
		mTextureHandle = TextureHandle();
		mExtent.set(1, 1);
	}

	// On a themed cursor, an external write to a stamped field becomes an
	// override that stamping will preserve. Category, the override list and the
	// art fields are not stamped, so they are not overridable.
	if (mThemeMembership.mTheme != NULL)
	{
		if (slot != themeCategoryField() && slot != themeOverridesField() && !isCursorArtField(slot))
			mThemeMembership.markOverride(slot);
	}
}

bool GuiCursor::writeField(StringTableEntry fieldname, const char* value)
{
	if (!Parent::writeField(fieldname, value))
		return false;

	// Themed cursors persist their art, their category and the override list
	// unconditionally; everything else is derived from the theme and persists
	// only when explicitly overridden.
	if (mThemeMembership.mTheme != NULL)
	{
		if (fieldname != themeCategoryField() &&
			fieldname != themeOverridesField() &&
			!isCursorArtField(fieldname) &&
			findField(fieldname) != NULL &&
			!mThemeMembership.isOverridden(fieldname))
			return false;
	}

	return true;
}

void GuiCursor::onDeleteNotify(SimObject* object)
{
	if (object == (SimObject*)mThemeMembership.mTheme)
		mThemeMembership.mTheme = NULL;

	Parent::onDeleteNotify(object);
}

// Setup the type, this will keep Border profiles from being listed with normal profiles.
ConsoleType(GuiCursor, TypeGuiCursor, sizeof(GuiCursor*), "")

ConsoleSetType(TypeGuiCursor)
{
	GuiCursor *profile = NULL;
	if (argc == 1)
		Sim::findObject(argv[0], profile);

	if (!profile)
	{
		profile = dynamic_cast<GuiCursor*>(Sim::findObject("DefaultCursor"));

		// A cursor is cosmetic, and every read site copes with not having one --
		// GuiTextEditCtrl::getCursor re-resolves lazily and otherwise just leaves
		// the caller's cursor alone. This used to be an AssertFatal, which put up
		// a modal dialog and wedged the whole editor whenever a .gui.taml named a
		// cursor that isn't a registered object (themes build their GuiCursors
		// anonymously, so the name in a saved file resolves to nothing). Warn and
		// leave the field unset instead -- same call this file already makes for a
		// missing font below.
		AssertWarn(profile != NULL, avar("GuiCursor: requested gui cursor (%s) does not exist and there is no DefaultCursor - leaving it unset.", argv[0]));
	}

	GuiCursor **obj = (GuiCursor **)dptr;
	if ((*obj) == profile)
		return;

	*obj = profile;
}

ConsoleGetType(TypeGuiCursor)
{
	static char returnBuffer[256];

	GuiCursor **obj = (GuiCursor**)dptr;
	dSprintf(returnBuffer, sizeof(returnBuffer), "%s", *obj ? (*obj)->getName() ? (*obj)->getName() : (*obj)->getIdString() : "");
	return returnBuffer;
}

IMPLEMENT_CONOBJECT(GuiBorderProfile);

GuiBorderProfile::GuiBorderProfile()
{
	for(S32 i = 0; i < 4; i++)
	{
		mMargin[i] = 0;
		mBorder[i] = 0;
		mBorderColor[i].set(255, 255, 255, 255);
		mPadding[i] = 0;
	}

   GuiBorderProfile *def = dynamic_cast<GuiBorderProfile*>(Sim::findObject("GuiDefaultBorderProfile"));
   if (def)
   {
      for (S32 i = 0; i < 4; i++)
      {
         mMargin[i] = def->mMargin[i];
         mBorder[i] = def->mBorder[i];
         mBorderColor[i] = def->mBorderColor[i];
         mPadding[i] = def->mPadding[i];
      }
   }

	mUnderfill = true;
	mCategory = StringTable->EmptyString;
	mIsCustom = false;
}

GuiBorderProfile::~GuiBorderProfile()
{
}

void GuiBorderProfile::initPersistFields()
{
	Parent::initPersistFields();

	addField("margin", TypeS32, Offset(mMargin[0], GuiBorderProfile));
	addField("marginHL", TypeS32, Offset(mMargin[1], GuiBorderProfile));
	addField("marginSL", TypeS32, Offset(mMargin[2], GuiBorderProfile));
	addField("marginNA", TypeS32, Offset(mMargin[3], GuiBorderProfile));

	addField("border", TypeS32, Offset(mBorder[0], GuiBorderProfile));
	addField("borderHL", TypeS32, Offset(mBorder[1], GuiBorderProfile));
	addField("borderSL", TypeS32, Offset(mBorder[2], GuiBorderProfile));
	addField("borderNA", TypeS32, Offset(mBorder[3], GuiBorderProfile));

	addField("borderColor", TypeColorI, Offset(mBorderColor[0], GuiBorderProfile));
	addField("borderColorHL", TypeColorI, Offset(mBorderColor[1], GuiBorderProfile));
	addField("borderColorSL", TypeColorI, Offset(mBorderColor[2], GuiBorderProfile));
	addField("borderColorNA", TypeColorI, Offset(mBorderColor[3], GuiBorderProfile));

	addField("padding", TypeS32, Offset(mPadding[0], GuiBorderProfile));
	addField("paddingHL", TypeS32, Offset(mPadding[1], GuiBorderProfile));
	addField("paddingSL", TypeS32, Offset(mPadding[2], GuiBorderProfile));
	addField("paddingNA", TypeS32, Offset(mPadding[3], GuiBorderProfile));

	addField("underfill", TypeBool, Offset(mUnderfill, GuiBorderProfile));

	addField("isCustom", TypeBool, Offset(mIsCustom, GuiBorderProfile));

	addField("category", TypeString, Offset(mCategory, GuiBorderProfile));
	// The offset is unused: both accessors are supplied and the setter returns false.
	addProtectedField("themeOverrides", TypeString, Offset(mCategory, GuiBorderProfile), &setThemeOverrides, &getThemeOverrides, "Theme-overridden field names");
}

bool GuiBorderProfile::onAdd()
{
	if (!Parent::onAdd())
		return false;

	Sim::getGuiDataGroup()->addObject(this);

	return true;
}

void GuiBorderProfile::onRemove()
{
	Parent::onRemove();
}

void GuiBorderProfile::setTheme(GuiProfileTheme* theme, bool preserveOverrides)
{
	if (mThemeMembership.mTheme == theme)
		return;

	if (mThemeMembership.mTheme != NULL)
		clearNotify(mThemeMembership.mTheme);

	mThemeMembership.mTheme = theme;
	if (!preserveOverrides)
		mThemeMembership.clearAll();

	if (theme != NULL)
		deleteNotify(theme);
}

void GuiBorderProfile::onStaticModified(const char* slotName, const char* newValue)
{
	Parent::onStaticModified(slotName, newValue);

	// On a themed border, any external field write becomes an override that
	// stamping will preserve. Category and the override list itself are
	// theme-managed.
	if (mThemeMembership.mTheme != NULL)
	{
		StringTableEntry slot = StringTable->insert(slotName);
		if (slot != themeCategoryField() && slot != themeOverridesField())
			mThemeMembership.markOverride(slot);
	}
}

bool GuiBorderProfile::writeField(StringTableEntry fieldname, const char* value)
{
	if (!Parent::writeField(fieldname, value))
		return false;

	// Themed borders persist only explicitly overridden fields; everything
	// else is derived from the theme. Category and the override list always
	// persist so a loaded theme can rebind its members.
	if (mThemeMembership.mTheme != NULL)
	{
		if (fieldname != themeCategoryField() &&
			fieldname != themeOverridesField() &&
			findField(fieldname) != NULL &&
			!mThemeMembership.isOverridden(fieldname))
			return false;
	}

	return true;
}

void GuiBorderProfile::onDeleteNotify(SimObject* object)
{
	if (object == (SimObject*)mThemeMembership.mTheme)
		mThemeMembership.mTheme = NULL;

	Parent::onDeleteNotify(object);
}

S32 GuiBorderProfile::getMargin(const GuiControlState state)
{
	return getMax(mMargin[getStateIndex(state)], 0);
}

S32 GuiBorderProfile::getBorder(const GuiControlState state)
{
	return getMax(mBorder[getStateIndex(state)], 0);
}

const ColorI& GuiBorderProfile::getBorderColor(const GuiControlState state)
{
	return mBorderColor[getStateIndex(state)];
}

S32 GuiBorderProfile::getPadding(const GuiControlState state)
{
	return getMax(mPadding[getStateIndex(state)], 0);
}

// Setup the type, this will keep Border profiles from being listed with normal profiles.
ConsoleType(GuiBProfile, TypeGuiBorderProfile, sizeof(GuiBorderProfile*), "")

ConsoleSetType(TypeGuiBorderProfile)
{
   GuiBorderProfile *profile = NULL;
   if (argc == 1)
      Sim::findObject(argv[0], profile);

   AssertWarn(profile != NULL, avar("GuiBorderProfile: requested gui profile (%s) does not exist.", argv[0]));
   if (!profile)
      profile = dynamic_cast<GuiBorderProfile*>(Sim::findObject("GuiDefaultBorderProfile"));

   AssertFatal(profile != NULL, avar("GuiBorderProfile: unable to find specified profile (%s) and GuiDefaultProfile does not exist!", argv[0]));

   GuiBorderProfile **obj = (GuiBorderProfile **)dptr;
   if ((*obj) == profile)
      return;

   *obj = profile;
}

ConsoleGetType(TypeGuiBorderProfile)
{
   static char returnBuffer[256];

   GuiBorderProfile **obj = (GuiBorderProfile**)dptr;
   dSprintf(returnBuffer, sizeof(returnBuffer), "%s", *obj ? (*obj)->getName() ? (*obj)->getName() : (*obj)->getIdString() : "");
   return returnBuffer;
}

//------------------------------------------------------------------------------
IMPLEMENT_CONOBJECT(GuiControlProfile);

static EnumTable::Enums alignEnums[] =
{
   { AlignmentType::LeftAlign,          "left"      },
   { AlignmentType::CenterAlign,        "center"    },
   { AlignmentType::RightAlign,         "right"     }
};
static EnumTable gAlignTable(3, &alignEnums[0]);

static EnumTable::Enums vAlignEnums[] =
{
   { VertAlignmentType::TopVAlign,          "top"      },
   { VertAlignmentType::MiddleVAlign,        "middle"    },
   { VertAlignmentType::BottomVAlign,         "bottom"     }
};
static EnumTable gVAlignTable(3, &vAlignEnums[0]);

static EnumTable::Enums charsetEnums[]=
{
	{ TGE_ANSI_CHARSET,         "ANSI" },
	{ TGE_SYMBOL_CHARSET,       "SYMBOL" },
	{ TGE_SHIFTJIS_CHARSET,     "SHIFTJIS" },
	{ TGE_HANGEUL_CHARSET,      "HANGEUL" },
	{ TGE_HANGUL_CHARSET,       "HANGUL" },
	{ TGE_GB2312_CHARSET,       "GB2312" },
	{ TGE_CHINESEBIG5_CHARSET,  "CHINESEBIG5" },
	{ TGE_OEM_CHARSET,          "OEM" },
	{ TGE_JOHAB_CHARSET,        "JOHAB" },
	{ TGE_HEBREW_CHARSET,       "HEBREW" },
	{ TGE_ARABIC_CHARSET,       "ARABIC" },
	{ TGE_GREEK_CHARSET,        "GREEK" },
	{ TGE_TURKISH_CHARSET,      "TURKISH" },
	{ TGE_VIETNAMESE_CHARSET,   "VIETNAMESE" },
	{ TGE_THAI_CHARSET,         "THAI" },
	{ TGE_EASTEUROPE_CHARSET,   "EASTEUROPE" },
	{ TGE_RUSSIAN_CHARSET,      "RUSSIAN" },
	{ TGE_MAC_CHARSET,          "MAC" },
	{ TGE_BALTIC_CHARSET,       "BALTIC" },
};

#define NUMCHARSETENUMS     (sizeof(charsetEnums) / sizeof(EnumTable::Enums))

static EnumTable gCharsetTable(NUMCHARSETENUMS, &charsetEnums[0]);

GuiControlProfile::GuiControlProfile(void) :
   mFontColor(mFontColors[BaseColor]),
   mFontColorHL(mFontColors[ColorHL]),
   mFontColorNA(mFontColors[ColorNA]),
   mFontColorSL(mFontColors[ColorSL]),
   mFontColorLink(mFontColors[ColorLink]),
   mFontColorLinkHL(mFontColors[ColorLinkHL]),
   mFontColorTextSL(mFontColors[ColorTextSL]),
   mImageAssetID( StringTable->EmptyString )
{
	mRefCount = 0;
	mBitmapArrayRects.clear();
	
	mTabable       = false;
	mCanKeyFocus   = false;

	mBorderDefault = NULL;

   mLeftProfileName = NULL;
	mBorderLeft = NULL;
   mRightProfileName = NULL;
	mBorderRight = NULL;
   mTopProfileName = NULL;
	mBorderTop = NULL;
   mBottomProfileName = NULL;
	mBorderBottom = NULL;
	
	// default font
	mFontType      = StringTable->EmptyString;
	mFontDirectory = StringTable->EmptyString;
	mFontSize      = 12;
	mFontCharset   = TGE_ANSI_CHARSET;
	mFontColors[BaseColor].set(255,255,255,255);
	
	// default bitmap
	mBitmapName    = NULL;
	mTextOffset.set(0,0);

	// default image asset
	mImageAsset = NULL;
	
	mAlignment     = AlignmentType::LeftAlign;
	mVAlignment    = VertAlignmentType::MiddleVAlign;

	//fill color
	mFillColor.set(0, 0, 0, 0);
	mFillColorHL.set(0, 0, 0, 0);
	mFillColorSL.set(0, 0, 0, 0);
	mFillColorNA.set(0, 0, 0, 0);
	mFillColorTextSL.set(100, 100, 100, 255);
   mCategory = StringTable->EmptyString;

   GuiControlProfile *def = dynamic_cast<GuiControlProfile*>(Sim::findObject("GuiDefaultProfile"));
   if (def)
   {
      mTabable = def->mTabable;
      mCanKeyFocus = def->mCanKeyFocus;

      mFillColor = def->mFillColor;
      mFillColorHL = def->mFillColorHL;
	  mFillColorSL = def->mFillColorSL;
      mFillColorNA = def->mFillColorNA;
	  mFillColorTextSL = def->mFillColorTextSL;

      mBorderDefault = def->mBorderDefault;
      mLeftProfileName = def->mLeftProfileName;
      mRightProfileName = def->mRightProfileName;
      mTopProfileName = def->mTopProfileName;
      mBottomProfileName = def->mBottomProfileName;

      // default font
      mFontType = def->mFontType;
	  mFontDirectory = def->mFontDirectory;
      mFontSize = def->mFontSize;
      mFontCharset = def->mFontCharset;

      for (U32 i = 0; i < 10; i++)
         mFontColors[i] = def->mFontColors[i];

      // default bitmap
      mBitmapName = def->mBitmapName;
      mTextOffset = def->mTextOffset;

      mAlignment = def->mAlignment;
	  mVAlignment = def->mVAlignment;
      mCursorColor = def->mCursorColor;
   }
}

// GuiDefaultProfile is not an ordinary profile. GuiControl::onWake falls back to
// it by name, around twenty control constructors setField() it onto themselves
// before script ever sees them, and the constructor above seeds every new profile
// from it. Yet until now it was a script object: three modules each created one
// (EditorCore, AppCore, Sandbox), the last loaded winning, and a project that
// deleted it - or never defined it - left the GUI with a NULL profile, which is an
// assert in a debug build and a null dereference in a release one.
//
// So the engine creates it, once, at start-up. Script may still tune it: both
// EditorCore and Sandbox assign onto the object they find rather than creating
// their own, which is how the editor keeps seeding its font face through the
// constructor copy above. Nothing has to create it, and nothing can lose it.
//
// The values here are a deliberately plain floor - transparent, white text, a face
// every platform can substitute for - because a control that lands on this profile
// has fallen through every other lookup. A project's real look comes from its
// GuiProfileTheme.
void GuiControlProfile::createDefaultProfile()
{
   GuiBorderProfile* border = dynamic_cast<GuiBorderProfile*>(Sim::findObject("GuiDefaultBorderProfile"));
   if (border == NULL)
   {
      border = new GuiBorderProfile();
      border->mUnderfill = true;
      if (!border->registerObject("GuiDefaultBorderProfile"))
      {
         delete border;
         return;
      }
   }

   if (Sim::findObject("GuiDefaultProfile") != NULL)
      return;

   GuiControlProfile* profile = new GuiControlProfile();

   // ColorI's default constructor leaves its channels uninitialized, and the
   // constructor above only sets the base font color, so spell out the whole
   // array here: this is the profile every later one copies from.
   profile->mFontColors[BaseColor].set(255, 255, 255, 255);
   profile->mFontColors[ColorHL].set(255, 255, 255, 255);
   profile->mFontColors[ColorNA].set(255, 255, 255, 128);
   profile->mFontColors[ColorSL].set(255, 255, 255, 255);
   profile->mFontColors[ColorLink].set(100, 160, 255, 255);
   profile->mFontColors[ColorLinkHL].set(140, 190, 255, 255);
   profile->mFontColors[ColorTextSL].set(0, 0, 0, 255);
   profile->mFontColors[ColorUser0].set(255, 255, 255, 255);
   profile->mFontColors[ColorUser1].set(255, 255, 255, 255);
   profile->mFontColors[ColorUser2].set(255, 255, 255, 255);

   // Arial rather than nothing: an empty face name yields a NULL font and text
   // that silently fails to draw, which is a miserable thing to debug. GFont's
   // fallback chain maps it onto Helvetica where Arial is absent.
   profile->mFontType = StringTable->insert("Arial");
   profile->mFontSize = 12;
   profile->mFontCharset = TGE_ANSI_CHARSET;

   profile->mAlignment = AlignmentType::CenterAlign;
   profile->mVAlignment = VertAlignmentType::MiddleVAlign;
   profile->mCursorColor.set(0, 0, 0, 255);
   profile->mBorderDefault = border;

   if (!profile->registerObject("GuiDefaultProfile"))
      delete profile;
}

GuiControlProfile::~GuiControlProfile()
{
	// Still worn on the way out. Every control holding this profile now has a
	// dangling mProfile, and nothing touches it until that control renders or is
	// destroyed - usually inside Sim::shutdown, which surfaces as an access
	// violation at exit with nothing pointing back to here. Name the profile at
	// the point the mistake is actually made.
	//
	// This can't be fixed up automatically: a control's "Profile" field is a raw
	// field offset (GuiControl::initPersistFields), so assigning it writes
	// mProfile directly, bypassing setControlProfile, and ConsoleSetType only
	// receives the field address - never the owning control - so there is nowhere
	// to register a deleteNotify. Destroying profiles after the controls that wear
	// them is the only defence.
	// Only meaningful while the engine is live. Sim::shutdown tears every object
	// down in an arbitrary order, so profiles outliving or predeceasing their
	// controls there is normal and would drown this warning in noise.
	if (mRefCount != 0 && !Sim::isShuttingDown())
	{
		const char* profileName = getName() ? getName() : "<unnamed>";
		Con::warnf("GuiControlProfile (%s) deleted while still worn by %d control(s) - those controls now hold a dangling profile.", profileName, mRefCount);
	}
}

void GuiControlProfile::setTheme(GuiProfileTheme* theme, bool preserveOverrides)
{
   if (mThemeMembership.mTheme == theme)
      return;

   if (mThemeMembership.mTheme != NULL)
      clearNotify(mThemeMembership.mTheme);

   mThemeMembership.mTheme = theme;
   if (!preserveOverrides)
      mThemeMembership.clearAll();

   if (theme != NULL)
      deleteNotify(theme);
}

void GuiControlProfile::onStaticModified(const char* slotName, const char* newValue)
{
   Parent::onStaticModified(slotName, newValue);

   StringTableEntry slot = StringTable->insert(slotName);

   // On a themed profile, any external field write becomes an override that
   // stamping will preserve. Category and the override list itself are
   // theme-managed.
   if (mThemeMembership.mTheme != NULL)
   {
      if (slot != themeCategoryField() && slot != themeOverridesField())
         mThemeMembership.markOverride(slot);
   }

   // A border field was written directly (editor, script, or Taml into a live
   // profile): the cached side pointers may now be stale, so re-resolve all
   // four. A side with an empty name falls back to the current default border.
   // The recipe path sets each side's name and pointer together and skips this.
   static StringTableEntry borderDefaultField = StringTable->insert("borderDefault");
   static StringTableEntry borderLeftField    = StringTable->insert("borderLeft");
   static StringTableEntry borderRightField   = StringTable->insert("borderRight");
   static StringTableEntry borderTopField     = StringTable->insert("borderTop");
   static StringTableEntry borderBottomField  = StringTable->insert("borderBottom");
   if (slot == borderDefaultField || slot == borderLeftField || slot == borderRightField ||
       slot == borderTopField || slot == borderBottomField)
   {
      setLeftProfile(NULL);   getLeftProfile();
      setRightProfile(NULL);  getRightProfile();
      setTopProfile(NULL);    getTopProfile();
      setBottomProfile(NULL); getBottomProfile();
   }
}

bool GuiControlProfile::writeField(StringTableEntry fieldname, const char* value)
{
   if (!Parent::writeField(fieldname, value))
      return false;

   // Themed profiles persist only explicitly overridden fields; everything
   // else is derived from the theme. Category and the override list always
   // persist so a loaded theme can rebind its members.
   if (mThemeMembership.mTheme != NULL)
   {
      if (fieldname != themeCategoryField() &&
          fieldname != themeOverridesField() &&
          findField(fieldname) != NULL &&
          !mThemeMembership.isOverridden(fieldname))
         return false;
   }

   return true;
}

void GuiControlProfile::onDeleteNotify(SimObject* object)
{
   // Null the theme back-pointer if our theme is being deleted.
   if (object == (SimObject*)mThemeMembership.mTheme)
      mThemeMembership.mTheme = NULL;

   // Null any border profile pointer matching the deleted object. The border
   // setters have always registered deleteNotify for these, but the
   // notification was previously ignored, leaving dangling pointers.
   if (object == mBorderDefault)
      mBorderDefault = NULL;
   if (object == mBorderLeft)
      mBorderLeft = NULL;
   if (object == mBorderRight)
      mBorderRight = NULL;
   if (object == mBorderTop)
      mBorderTop = NULL;
   if (object == mBorderBottom)
      mBorderBottom = NULL;

   Parent::onDeleteNotify(object);
}


void GuiControlProfile::initPersistFields()
{
   Parent::initPersistFields();
   addGroup("Behavior");
      addField("tab",           TypeBool,       Offset(mTabable, GuiControlProfile));
      addField("canKeyFocus",   TypeBool,       Offset(mCanKeyFocus, GuiControlProfile));
   endGroup("Behavior");

   addGroup("FillColor");
	   addField("fillColor",     TypeColorI,     Offset(mFillColor, GuiControlProfile));
	   addField("fillColorHL",   TypeColorI,     Offset(mFillColorHL, GuiControlProfile));
	   addField("fillColorSL",   TypeColorI,     Offset(mFillColorSL, GuiControlProfile));
	   addField("fillColorNA",   TypeColorI,     Offset(mFillColorNA, GuiControlProfile));
	   addField("fillColorTextSL", TypeColorI,   Offset(mFillColorTextSL, GuiControlProfile));
   endGroup("FillColor");

   addGroup("Border");
	   addField("borderDefault", TypeGuiBorderProfile, Offset(mBorderDefault, GuiControlProfile));
	   addField("borderLeft",    TypeString, Offset(mLeftProfileName, GuiControlProfile));
	   addField("borderRight",   TypeString, Offset(mRightProfileName, GuiControlProfile));
	   addField("borderTop",     TypeString, Offset(mTopProfileName, GuiControlProfile));
	   addField("borderBottom",  TypeString, Offset(mBottomProfileName, GuiControlProfile));
   endGroup("Border");

   addGroup("Font");
	   addField("fontType",      TypeString,     Offset(mFontType, GuiControlProfile));
	   addField("fontSize",      TypeS32,        Offset(mFontSize, GuiControlProfile));
	   addField("align", TypeEnum, Offset(mAlignment, GuiControlProfile), 1, &gAlignTable);
	   addField("vAlign", TypeEnum, Offset(mVAlignment, GuiControlProfile), 1, &gVAlignTable);
	   addField("fontDirectory", TypeString,	 Offset(mFontDirectory, GuiControlProfile));
	   addField("fontCharset",   TypeEnum,       Offset(mFontCharset, GuiControlProfile), 1, &gCharsetTable);
	   addField("fontColors",    TypeColorI,     Offset(mFontColors, GuiControlProfile), 10);
	   addField("fontColor",     TypeColorI,     Offset(mFontColors[BaseColor], GuiControlProfile));
	   addField("fontColorHL",   TypeColorI,     Offset(mFontColors[ColorHL], GuiControlProfile));
	   addField("fontColorNA",   TypeColorI,     Offset(mFontColors[ColorNA], GuiControlProfile));
	   addField("fontColorSL",   TypeColorI,     Offset(mFontColors[ColorSL], GuiControlProfile));
	   addField("fontColorLink", TypeColorI,     Offset(mFontColors[ColorLink], GuiControlProfile));
	   addField("fontColorLinkHL", TypeColorI, Offset(mFontColors[ColorLinkHL], GuiControlProfile));
	   addField("fontColorTextSL", TypeColorI, Offset(mFontColors[ColorTextSL], GuiControlProfile));
   endGroup("Font");

   addField("textOffset",    TypePoint2I,    Offset(mTextOffset, GuiControlProfile));
   addField("cursorColor",   TypeColorI,     Offset(mCursorColor, GuiControlProfile));

   // Written relative to the game root; see getRelativeBitmapName.
   addProtectedField("bitmap", TypeFilename, Offset(mBitmapName, GuiControlProfile), &defaultProtectedSetFn, &getBitmapName, "Bitmap array used to render the control");
   addProtectedField("imageAsset", TypeAssetId, Offset(mImageAssetID, GuiControlProfile), &setImageAsset, &getImageAsset, "The image asset ID used to render the control");

   addField("category", TypeString, Offset(mCategory, GuiControlProfile));
   // The offset is unused: both accessors are supplied and the setter returns false.
   addProtectedField("themeOverrides", TypeString, Offset(mCategory, GuiControlProfile), &setThemeOverrides, &getThemeOverrides, "Theme-overridden field names");
}

bool GuiControlProfile::onAdd()
{
   if(!Parent::onAdd())
	  return false;

   Sim::getGuiDataGroup()->addObject(this);

   getLeftProfile();
   getRightProfile();
   getTopProfile();
   getBottomProfile();

   return true;
}

GuiBorderProfile * GuiControlProfile::getLeftProfile()
{
   // We can early out if we still have a valid profile
   if (mBorderLeft)
      return mBorderLeft;

   // Attempt to find the profile specified
   if (mLeftProfileName && *mLeftProfileName)
   {
      GuiBorderProfile *profile = dynamic_cast<GuiBorderProfile*> (Sim::findObject(mLeftProfileName));

      if (profile)
      {
         setLeftProfile(profile);
      }
   }
   else
   {
      setLeftProfile(mBorderDefault);
   }

   return mBorderLeft;
}

void GuiControlProfile::setLeftProfile(GuiBorderProfile * prof)
{
   if (prof == mBorderLeft)
      return;

   // Clear the delete notification we previously set up
   if (mBorderLeft)
      clearNotify(mBorderLeft);

   mBorderLeft = prof;

   // Make sure that the new profile will notify us when it is deleted
   if (mBorderLeft)
      deleteNotify(mBorderLeft);
}

GuiBorderProfile * GuiControlProfile::getRightProfile()
{
   // We can early out if we still have a valid profile
   if (mBorderRight)
      return mBorderRight;

   // Attempt to find the profile specified
   if (mRightProfileName && *mRightProfileName)
   {
      GuiBorderProfile *profile = dynamic_cast<GuiBorderProfile*> (Sim::findObject(mRightProfileName));

      if (profile)
      {
         setRightProfile(profile);
      }
   }
   else
   {
      setRightProfile(mBorderDefault);
   }

   return mBorderRight;
}

void GuiControlProfile::setRightProfile(GuiBorderProfile * prof)
{
   if (prof == mBorderRight)
      return;

   // Clear the delete notification we previously set up
   if (mBorderRight)
      clearNotify(mBorderRight);

   mBorderRight = prof;

   // Make sure that the new profile will notify us when it is deleted
   if (mBorderRight)
      deleteNotify(mBorderRight);
}

GuiBorderProfile * GuiControlProfile::getTopProfile()
{
   // We can early out if we still have a valid profile
   if (mBorderTop)
      return mBorderTop;

   // Attempt to find the profile specified
   if (mTopProfileName && *mTopProfileName)
   {
      GuiBorderProfile *profile = dynamic_cast<GuiBorderProfile*> (Sim::findObject(mTopProfileName));

      if (profile)
      {
         setTopProfile(profile);
      }
   }
   else
   {
      setTopProfile(mBorderDefault);
   }

   return mBorderTop;
}

void GuiControlProfile::setTopProfile(GuiBorderProfile * prof)
{
   if (prof == mBorderTop)
      return;

   // Clear the delete notification we previously set up
   if (mBorderTop)
      clearNotify(mBorderTop);

   mBorderTop = prof;

   // Make sure that the new profile will notify us when it is deleted
   if (mBorderTop)
      deleteNotify(mBorderTop);
}

GuiBorderProfile * GuiControlProfile::getBottomProfile()
{
   // We can early out if we still have a valid profile
   if (mBorderBottom)
      return mBorderBottom;

   // Attempt to find the profile specified
   if (mBottomProfileName && *mBottomProfileName)
   {
      GuiBorderProfile *profile = dynamic_cast<GuiBorderProfile*> (Sim::findObject(mBottomProfileName));

      if (profile)
      {
         setBottomProfile(profile);
      }
   }
   else
   {
      setBottomProfile(mBorderDefault);
   }

   return mBorderBottom;
}

void GuiControlProfile::setBottomProfile(GuiBorderProfile * prof)
{
   if (prof == mBorderBottom)
      return;

   // Clear the delete notification we previously set up
   if (mBorderBottom)
      clearNotify(mBorderBottom);

   mBorderBottom = prof;

   // Make sure that the new profile will notify us when it is deleted
   if (mBorderBottom)
      deleteNotify(mBorderBottom);
}

S32 GuiControlProfile::constructBitmapArray()
{
   if(mBitmapArrayRects.size())
	  return mBitmapArrayRects.size();

   GBitmap *bmp = mTextureHandle.getBitmap();

   // Make sure the texture exists.
   if( !bmp )
	  return 0;
  
   //get the separator color
   ColorI sepColor;
   if ( !bmp || !bmp->getColor( 0, 0, sepColor ) )
	{
	  Con::errorf("Failed to create bitmap array from %s for profile %s - couldn't ascertain seperator color!", mBitmapName, getName());
	  AssertFatal( false, avar("Failed to create bitmap array from %s for profile %s - couldn't ascertain seperator color!", mBitmapName, getName()));
	  return 0;
	}

   //now loop through all the scroll pieces, and find the bounding rectangle for each piece in each state
   S32 curY = 0;

   // ascertain the height of this row...
   ColorI color;
   mBitmapArrayRects.clear();
   while(curY < (S32)bmp->getHeight())
   {
	  // skip any sep colors
	  bmp->getColor( 0, curY, color);
	  if(color == sepColor)
	  {
		 curY++;
		 continue;
	  }
	  // ok, process left to right, grabbing bitmaps as we go...
	  S32 curX = 0;
	  while(curX < (S32)bmp->getWidth())
	  {
		 bmp->getColor(curX, curY, color);
		 if(color == sepColor)
		 {
			curX++;
			continue;
		 }
		 S32 startX = curX;
		 while(curX < (S32)bmp->getWidth())
		 {
			bmp->getColor(curX, curY, color);
			if(color == sepColor)
			   break;
			curX++;
		 }
		 S32 stepY = curY;
		 while(stepY < (S32)bmp->getHeight())
		 {
			bmp->getColor(startX, stepY, color);
			if(color == sepColor)
			   break;
			stepY++;
		 }
		 mBitmapArrayRects.push_back(RectI(startX, curY, curX - startX, stepY - curY));
	  }
	  // ok, now skip to the next separation color on column 0
	  while(curY < (S32)bmp->getHeight())
	  {
		 bmp->getColor(0, curY, color);
		 if(color == sepColor)
			break;
		 curY++;
	  }
   }
   return mBitmapArrayRects.size();
}

void GuiControlProfile::incRefCount(F32 fontAdjust)
{
	if(!mRefCount)
	{
		if(mFontDirectory == StringTable->EmptyString)
			mFontDirectory = Con::getVariable("$GUI::fontCacheDirectory");

		//load the font
		S32 size = getFontSize(fontAdjust);
		addFont(size);

		//Set the bitmap
		if ( mBitmapName != NULL && mBitmapName != StringTable->EmptyString )
		{
			mTextureHandle = TextureHandle(mBitmapName, TextureHandle::BitmapKeepTexture);
			if (!(bool)mTextureHandle)
				Con::errorf("Failed to load profile bitmap (%s)",mBitmapName);
		}

		//set the image asset
		if (mImageAssetID != NULL && mImageAssetID != StringTable->EmptyString)
		{
			mImageAsset = mImageAssetID;
		}
	}

   mRefCount++;
}

void GuiControlProfile::decRefCount()
{
	AssertFatal(mRefCount, avar("GuiControlProfile::%s::decRefCount: zero ref count", this->getName()));
   if(!mRefCount)
	  return;
   --mRefCount;

	if(!mRefCount)
	{
		mFontMap.clear();
		mTextureHandle = NULL;
		if(mImageAsset != NULL)
		{
			mImageAsset.clear();
		}
	}
}

// What the bitmap path should look like in a file. TypeFilename expands
// whatever it is handed the moment it is set, so mBitmapName is always an
// absolute path on the machine that set it - and writing that down produces a
// profile that renders on exactly one computer. Relative to the game root is
// what survives the trip, and it is what TypeFilename expands back correctly on
// the next machine to load it.
StringTableEntry GuiControlProfile::getRelativeBitmapName( void ) const
{
	if (mBitmapName == NULL || mBitmapName == StringTable->EmptyString)
		return mBitmapName;

	// Only a path inside the game is made relative. An image somewhere else -
	// another drive, a folder beside the repository - is left alone: a "../.."
	// chain climbing out of the game folder is no more portable than the
	// absolute path it came from, and pretending otherwise would hide that.
	StringTableEntry gameRoot = Platform::getMainDotCsDir();
	if (!Con::isBasePath(mBitmapName, gameRoot))
		return mBitmapName;

	return Platform::makeRelativePathName(mBitmapName, gameRoot);
}

void GuiControlProfile::setImageAsset(const char* pImageAssetID)
{
	// Sanity!
	AssertFatal(pImageAssetID != NULL, "Cannot use a NULL asset ID.");

	// Fetch the asset ID
	mImageAssetID = StringTable->insert(pImageAssetID);

	// Assign asset if this profile is being used.
	if (mRefCount != 0)
		mImageAsset = pImageAssetID;
}

S32 GuiControlProfile::getFontSize(F32 fontAdjust)
{
	S32 size = mRound(mFontSize * fontAdjust);
	if (size < 1)
	{
		size = 8;//This is an arbitrary value. Feel free to change it to something you like better.
	}
	return size;
}

GFont* GuiControlProfile::getFont(F32 fontAdjust)
{
	S32 size = getFontSize(fontAdjust);
	if (mFontMap.find(size) == mFontMap.end())
	{
		addFont(size);
	}
	GFont* font = mFontMap[size];

	// The requested face/size couldn't be loaded — no cached .uft/.fnt AND no
	// platform font backend could synthesize it (e.g. the web build has no font
	// backend, or a font is missing on Android). Don't AssertFatal: that hard-
	// crashes the whole engine over a single missing font, and every text-render
	// site here dereferences the result. Degrade gracefully instead — fall back to
	// any other size already loaded for this profile so text still renders (at a
	// near size); if the profile has no usable font at all, return NULL and let the
	// text-render paths skip drawing (they tolerate a NULL font). addFont() above
	// already declines to insert a failed font, so this mirrors that contract.
	if (font == nullptr)
	{
		for (HashMap<S32, GFont*>::iterator itr = mFontMap.begin(); itr != mFontMap.end(); ++itr)
		{
			if (itr->value != nullptr)
			{
				font = itr->value;
				break;
			}
		}
	}

	return font;
}

void GuiControlProfile::addFont(S32 fontSize)
{
	if (mFontMap.find(fontSize) == mFontMap.end())
	{
		GFont* font = GFont::create(mFontType, fontSize, mFontDirectory);
		if (font == nullptr)
		{
			Con::errorf("Failed to load/create profile font (%s/%d)", mFontType, fontSize);
		}
		else
		{
			mFontMap[fontSize] = font;
		}
	}
}

const ColorI& GuiControlProfile::getFillColor(const GuiControlState state)
{
	switch (state)
	{
	default:
	case NormalState:
	case NormalStateOn:
		return mFillColor;
		break;
	case HighlightState:
	case HighlightStateOn:
		return mFillColorHL;
		break;
	case SelectedState:
	case SelectedStateOn:
		return mFillColorSL;
		break;
	case DisabledState:
	case DisabledStateOn:
		return mFillColorNA;
		break;
	}
}

const ColorI& GuiControlProfile::getFontColor(const GuiControlState state)
{
	switch (state)
	{
	default:
	case NormalState:
	case NormalStateOn:
		return mFontColor;
		break;
	case HighlightState:
	case HighlightStateOn:
		return mFontColorHL;
		break;
	case SelectedState:
	case SelectedStateOn:
		return mFontColorSL;
		break;
	case DisabledState:
	case DisabledStateOn:
		return mFontColorNA;
		break;
	}
}

bool GuiControlProfile::usesAssetRendering(const GuiControlState state)
{
	return mImageAsset != NULL && mImageAsset->isAssetValid() && mImageAsset->getFrameCount() > state;
}

bool GuiControlProfile::usesBitmapRendering(const GuiControlState state)
{
	return !usesAssetRendering(state) && mBitmapName != NULL && constructBitmapArray() > state;
}

bool GuiControlProfile::usesDefaultRendering(const GuiControlState state)
{
	return !usesAssetRendering(state) && !usesBitmapRendering(state);
}

ConsoleType( GuiProfile, TypeGuiProfile, sizeof(GuiControlProfile*), "" )

ConsoleSetType( TypeGuiProfile )
{
   GuiControlProfile *profile = NULL;
   if(argc == 1)
	  Sim::findObject(argv[0], profile);

   AssertWarn(profile != NULL, avar("GuiControlProfile: requested gui profile (%s) does not exist.", argv[0]));
   if(!profile)
	  profile = dynamic_cast<GuiControlProfile*>(Sim::findObject("GuiDefaultProfile"));

   AssertFatal(profile != NULL, avar("GuiControlProfile: unable to find specified profile (%s) and GuiDefaultProfile does not exist!", argv[0]));

   GuiControlProfile **obj = (GuiControlProfile **)dptr;
   if((*obj) == profile)
	  return;

   *obj = profile;
   //Note: reference counts are change in guiControl only if the guiControl is awake.
}

ConsoleGetType( TypeGuiProfile )
{
   static char returnBuffer[256];

   GuiControlProfile **obj = (GuiControlProfile**)dptr;
   dSprintf(returnBuffer, sizeof(returnBuffer), "%s", *obj ? (*obj)->getName() ? (*obj)->getName() : (*obj)->getIdString() : "");
   return returnBuffer;
}