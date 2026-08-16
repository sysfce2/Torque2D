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

#ifndef _ASSET_BASE_H_
#include "assets/assetBase.h"
#endif

#ifndef _IMAGE_ASSET_H_
#include "2d/assets/ImageAsset.h"
#endif

#ifndef _ANIMATION_ASSET_H_
#include "2d/assets/AnimationAsset.h"
#endif

#ifndef _PARTICLE_ASSET_H_
#include "2d/assets/ParticleAsset.h"
#endif

#ifndef _PARTICLE_ASSET_EMITTER_H_
#include "2d/assets/ParticleAssetEmitter.h"
#endif

#ifndef _AUDIO_ASSET_H_
#include "audio/AudioAsset.h"
#endif

#ifndef _FONT_ASSET_H_
#include "2d/assets/FontAsset.h"
#endif

#ifndef _CONSOLE_H_
#include "console/console.h"
#endif

//-----------------------------------------------------------------------------
// Copying an asset's state onto another asset of the same type.
//
// This is what clone(), acquireAsset(id, true) and the Asset Manager's undo all
// stand on, and until these tests existed every asset type got it wrong. Each
// type listed its own fields by hand, and every one of those lists had drifted
// from the fields it was supposed to carry:
//
//   ImageAsset            copied its cell COUNT into its cell OFFSET, never
//                         copied image layers at all, and dropped explicit cells
//                         entirely unless ExplicitMode happened to be on.
//   AnimationAsset        chose between numbered and named frames by reading the
//                         TARGET's NamedCellsMode -- still the default -- and
//                         then set that mode afterwards.
//   ParticleAssetEmitter  same shape: it read the TARGET's frame-provider mode,
//                         which defaults to static, so an animated emitter
//                         copied as a blank static one.
//
// The fix was to stop listing fields: AssetBase::copyTo walks the field table,
// and each type overrides copyAssetStateTo only for the state no field describes
// -- TAML custom nodes and Taml children. The last two tests here are what keep
// it that way, by enumerating the field table rather than a list of their own.
//
// None of this needs a canvas or a GL context. An asset that the asset manager
// does not own never reaches initializeAsset or onAssetRefresh, so no texture is
// ever registered: ImageAsset::onAdd does nothing, and insertLayer only loads a
// layer's bitmap when the asset is owned.
//-----------------------------------------------------------------------------

static StringTableEntry assetFieldName( const char* name )
{
    return StringTable->insert( name );
}

static const char* readAssetField( SimObject* object, const char* name )
{
    return object->getDataField( assetFieldName( name ), NULL );
}

static void writeAssetField( SimObject* object, const char* name, const char* value )
{
    object->setDataField( assetFieldName( name ), NULL, value );
}

// A registered, unowned asset of the given type. Unowned is the point: every
// setter's refreshAsset() returns immediately without an owning manager, so
// nothing here writes a file, marks anything dirty, or touches a texture.
template<typename T> static T* newScratchAsset( void )
{
    T* pAsset = new T();
    pAsset->registerObject();
    return pAsset;
}

//-----------------------------------------------------------------------------
// One test per defect. Each of these failed before the hand-written copyTo
// overrides were replaced.
//-----------------------------------------------------------------------------

TEST( AssetStateCopyTests, ImageAssetCarriesCellOffsetsRatherThanCounts )
{
    ImageAsset* source = newScratchAsset<ImageAsset>();
    source->setCellCountX( 4 );
    source->setCellCountY( 5 );
    source->setCellOffsetX( 7 );
    source->setCellOffsetY( 9 );

    ImageAsset* target = newScratchAsset<ImageAsset>();
    source->copyTo( target );

    ASSERT_EQ( target->getCellOffsetX(), 7 )
        << "The cell offset was copied from the cell COUNT.";
    ASSERT_EQ( target->getCellOffsetY(), 9 )
        << "The cell offset was copied from the cell COUNT.";
    ASSERT_EQ( target->getCellCountX(), 4 );
    ASSERT_EQ( target->getCellCountY(), 5 );

    source->deleteObject();
    target->deleteObject();
}

TEST( AssetStateCopyTests, ImageAssetCarriesImageLayers )
{
    ImageAsset* source = newScratchAsset<ImageAsset>();
    source->addLayer( "one.png", Point2I( 3, 4 ), ColorF( 1.0f, 0.5f, 0.25f, 1.0f ), false );
    source->addLayer( "two.png", Point2I( 5, 6 ), ColorF( 0.5f, 0.5f, 0.5f, 0.5f ), false );

    ImageAsset* target = newScratchAsset<ImageAsset>();
    source->copyTo( target );

    ASSERT_EQ( target->getLayerCount(), 2u )
        << "Image layers were never copied at all.";
    ASSERT_STREQ( target->getLayerImage( 1 ), "one.png" );
    ASSERT_STREQ( target->getLayerImage( 2 ), "two.png" );
    ASSERT_EQ( target->getLayerPosition( 2 ).x, 5 );
    ASSERT_EQ( target->getLayerPosition( 2 ).y, 6 );

    source->deleteObject();
    target->deleteObject();
}

// Explicit cells have no unit test on purpose. addExplicitCell validates every
// cell against getImageWidth()/getImageHeight(), so it refuses to add anything at
// all until a real bitmap is loaded -- which needs a file on disk and a GL
// context, and so belongs in tests/smoke. What is asserted there is that the
// cells survive a copy even with ExplicitMode off, because the cells outlive
// being switched out of explicit mode and the old copy dropped them.

// No mode is set here, and none can be: named cells mode is not a field any more,
// it is read from the image asset, and a unit test cannot give an animation a real
// image -- that needs a bitmap and a GL context. So this asserts the thing the
// copy is actually responsible for, which is that the named list travels.
//
// The old version of this test set the mode on the source first, which is what
// caught the original defect: the copy asked the TARGET which mode it was in, and
// the target was still numbered, so the named frames were silently not copied.
// That question no longer has a wrong answer to give, because both lists are
// copied unconditionally. The mode's own coverage went with the field; see the
// note beside assertEveryFieldCopies<AnimationAsset> below.
TEST( AssetStateCopyTests, AnimationAssetCarriesNamedFrames )
{
    AnimationAsset* source = newScratchAsset<AnimationAsset>();
    source->setNamedAnimationFrames( "head body tail" );

    AnimationAsset* target = newScratchAsset<AnimationAsset>();
    source->copyTo( target );

    ASSERT_EQ( target->getSpecifiedNamedAnimationFrames().size(), 3 );

    // Compared by interned identity, not by spelling. StringTable->insert folds
    // case by default, so it hands back whichever capitalisation reached it first
    // -- "head" came back as "HEAD" here, from something else entirely that had
    // already interned that word. Frame names are matched the same way everywhere
    // else, so this is the comparison that means what the caller thinks it means.
    ASSERT_EQ( target->getSpecifiedNamedAnimationFrames()[0], StringTable->insert( "head" ) );
    ASSERT_EQ( target->getSpecifiedNamedAnimationFrames()[2], StringTable->insert( "tail" ) );

    source->deleteObject();
    target->deleteObject();
}

// Both lists survive a copy, not merely the one in use. An image can be switched
// between explicit and cell mode, which switches every animation on it between
// name and index space, and the list that is not in use is what makes that
// reversible -- a copy that dropped it would lose the frames on the way back.
TEST( AssetStateCopyTests, AnimationAssetCarriesBothFrameLists )
{
    AnimationAsset* source = newScratchAsset<AnimationAsset>();
    source->setAnimationFrames( "0 1 2" );
    source->setNamedAnimationFrames( "head body tail" );

    AnimationAsset* target = newScratchAsset<AnimationAsset>();
    source->copyTo( target );

    ASSERT_EQ( target->getSpecifiedAnimationFrames().size(), 3 );
    ASSERT_EQ( target->getSpecifiedNamedAnimationFrames().size(), 3 );

    source->deleteObject();
    target->deleteObject();
}

// These two assert the frame-provider MODE and not the asset id, because an
// AssetPtr will not hold an id that does not resolve and no real image or
// animation asset exists in a unit test. The mode is the defect anyway: what
// went wrong was an animated emitter arriving static and blank, and it is the
// mode that decides which of the two exclusive fields the emitter then reads.
TEST( AssetStateCopyTests, ParticleAssetEmitterCarriesAnimatedMode )
{
    ParticleAssetEmitter* source = newScratchAsset<ParticleAssetEmitter>();
    source->setAnimation( "SomeModule:someAnimation" );
    ASSERT_FALSE( source->isStaticFrameProvider() ) << "Test setup failed.";

    ParticleAssetEmitter* target = newScratchAsset<ParticleAssetEmitter>();
    source->copyTo( target );

    ASSERT_FALSE( target->isStaticFrameProvider() )
        << "The copy asked the TARGET whether it was a static frame provider, and "
           "a fresh emitter defaults to static, so an animated emitter copied as a "
           "blank static one.";

    source->deleteObject();
    target->deleteObject();
}

// The mirror of the above, and the reason copyTo cannot simply copy every field
// and stop: Image and Animation are both persist fields, setAnimation clears
// static mode unconditionally, and Animation is listed after Image in the field
// table. So a purely generic field copy leaves EVERY static emitter animated.
TEST( AssetStateCopyTests, ParticleAssetEmitterCarriesStaticMode )
{
    ParticleAssetEmitter* source = newScratchAsset<ParticleAssetEmitter>();
    source->setImage( "SomeModule:someImage", 3 );
    ASSERT_TRUE( source->isStaticFrameProvider() ) << "Test setup failed.";

    ParticleAssetEmitter* target = newScratchAsset<ParticleAssetEmitter>();
    source->copyTo( target );

    ASSERT_TRUE( target->isStaticFrameProvider() )
        << "Animation is listed after Image in the field table, so the generic "
           "field copy left the emitter animated.";

    source->deleteObject();
    target->deleteObject();
}

//-----------------------------------------------------------------------------
// The particle emitter's console API, which the Asset Manager's emitter pane is
// the first thing to lean on. Each of these is a defect the pane found.
//-----------------------------------------------------------------------------

// EmitterAngle is stored in DEGREES: the persist field writes what it is given
// and ParticlePlayer::configureParticle does mDegToRad(getEmitterAngle()) when it
// places the particle. The console setter used to store mDegToRad(angle) and the
// getter to hand back mRadToDeg(stored), so the two of them agreed with each
// other and with nothing else -- an angle set from script reached the renderer 57
// times too small, and reading the field by name gave a different number from
// reading it by method.
TEST( AssetStateCopyTests, EmitterAngleIsDegreesEverywhere )
{
    ParticleAssetEmitter* emitter = newScratchAsset<ParticleAssetEmitter>();

    Con::evaluatef( "%d.setEmitterAngle( 90 );", emitter->getId() );

    ASSERT_FLOAT_EQ( emitter->getEmitterAngle(), 90.0f )
        << "The console setter stored something other than the degrees it was given.";
    ASSERT_STREQ( readAssetField( emitter, "EmitterAngle" ), "90" )
        << "The field and the console setter disagree about the unit.";

    emitter->deleteObject();
}

// SimObject::getFieldValue(fieldName) is how everything in the editor reads a
// persistent field by name. ParticleAssetEmitter and ParticleAsset each declared
// a getFieldValue(time) of their own -- the graph sampler -- which SHADOWED it,
// so the ordinary call returned a sample of whichever curve was selected at
// dAtof(fieldName) == 0 seconds. It did not fail; it returned a plausible 1.
TEST( AssetStateCopyTests, EmitterFieldsAreReadableByName )
{
    ParticleAssetEmitter* emitter = newScratchAsset<ParticleAssetEmitter>();
    emitter->setEmitterName( "smoke" );

    const char* name = Con::evaluatef( "return %d.getFieldValue( EmitterName );", emitter->getId() );

    ASSERT_STREQ( name, "smoke" )
        << "getFieldValue answered with a graph sample rather than the field.";

    emitter->deleteObject();
}

// An emitter in animated mode holding no animation asset is indistinguishable
// from a static one holding no image if you only look at the assets, so the mode
// has to be askable directly. The pane's source picker depends on it.
TEST( AssetStateCopyTests, EmitterReportsItsFrameProviderMode )
{
    ParticleAssetEmitter* emitter = newScratchAsset<ParticleAssetEmitter>();

    emitter->setAnimation( "" );
    ASSERT_STREQ( Con::evaluatef( "return %d.isStaticMode();", emitter->getId() ), "0" )
        << "An emitter with no animation asset still reported static mode.";

    emitter->setImage( "" );
    ASSERT_STREQ( Con::evaluatef( "return %d.isStaticMode();", emitter->getId() ), "1" );

    emitter->deleteObject();
}

// setNamedImageFrame is handed an empty name by every copy and clone, because
// copyFieldsFrom walks the whole field table and a numeric-frame emitter's
// NamedFrame is empty. Taking it would flip the emitter into named-frame mode.
TEST( AssetStateCopyTests, EmptyNamedFrameIsRefused )
{
    ParticleAssetEmitter* emitter = newScratchAsset<ParticleAssetEmitter>();

    ASSERT_FALSE( emitter->setNamedImageFrame( "" ) );
    ASSERT_FALSE( emitter->isUsingNamedImageFrame() );

    emitter->deleteObject();
}

TEST( AssetStateCopyTests, ParticleAssetCarriesEmitters )
{
    ParticleAsset* source = newScratchAsset<ParticleAsset>();
    ParticleAssetEmitter* pFirst = source->createEmitter();
    pFirst->setEmitterName( "sparks" );
    ParticleAssetEmitter* pSecond = source->createEmitter();
    pSecond->setEmitterName( "smoke" );

    ParticleAsset* target = newScratchAsset<ParticleAsset>();
    source->copyTo( target );

    ASSERT_EQ( target->getEmitterCount(), 2u );
    ASSERT_STREQ( target->getEmitter( 0 )->getEmitterName(), "sparks" );
    ASSERT_STREQ( target->getEmitter( 1 )->getEmitterName(), "smoke" );
    ASSERT_NE( target->getEmitter( 0 ), pFirst )
        << "The emitters must be copies, not the originals shared into two assets.";

    source->deleteObject();
    target->deleteObject();
}

//-----------------------------------------------------------------------------
// The two that stop this drifting again.
//
// Both walk the field table rather than a list written here, so a field added to
// any asset type tomorrow is covered the day it is added -- which is the whole
// reason the hand-written copies were removed.
//-----------------------------------------------------------------------------

// A value that is legal for the field's type and differs from the default. Asset
// id fields are left alone: they name assets that do not exist in a unit test,
// and the emitter's Image/Animation pair is asserted on its own above.
// Enums and asset-id fields deliberately return NULL: an enum needs a value from
// its own table, and an asset id would have to name an asset that exists. The
// emitter's Image/Animation pair is the one case where that matters, and it has
// two tests of its own above.
static const char* distinctValueForType( const U32 type, const S32 index )
{
    static char buffer[64];

    if ( type == (U32)TypeBool )
        return "1";

    if ( type == (U32)TypeS32 )
    {
        dSprintf( buffer, sizeof( buffer ), "%d", 3 + index );
        return buffer;
    }

    if ( type == (U32)TypeF32 )
    {
        dSprintf( buffer, sizeof( buffer ), "%d.5", 2 + index );
        return buffer;
    }

    if ( type == (U32)TypeVector2 )
    {
        dSprintf( buffer, sizeof( buffer ), "%d %d", 6 + index, 7 + index );
        return buffer;
    }

    if ( type == (U32)TypeColorF )
        return "0.25 0.5 0.75 1";

    if ( type == (U32)TypeString || type == (U32)TypeCaseString )
    {
        dSprintf( buffer, sizeof( buffer ), "value%d", index );
        return buffer;
    }

    return NULL;
}

static bool isCopyableField( const AbstractClassRep::Field& field )
{
    // A group marker is not a field. Note the engine's spelling of the third one.
    if ( field.type == AbstractClassRep::StartGroupFieldType ||
         field.type == AbstractClassRep::EndGroupFieldType ||
         field.type == AbstractClassRep::DepricatedFieldType )
        return false;

    // AssetName is a no-op once an asset is owned and is deliberately not part of
    // a copy's identity; AssetPrivate has no setter at all.
    if ( field.pFieldname == StringTable->insert( "AssetName" ) ||
         field.pFieldname == StringTable->insert( "AssetPrivate" ) )
        return false;

    return distinctValueForType( field.type, 0 ) != NULL;
}

// Write a distinct value into every field this test knows how to write, then
// assert a copy answers with all of them.
template<typename T> static void assertEveryFieldCopies( const char* pTypeName )
{
    T* source = newScratchAsset<T>();
    T* target = newScratchAsset<T>();

    const AbstractClassRep::FieldList& fields = source->getFieldList();

    S32 written = 0;
    for( U32 index = 0; index < (U32)fields.size(); ++index )
    {
        const AbstractClassRep::Field& field = fields[index];
        if ( !isCopyableField( field ) )
            continue;

        const char* pValue = distinctValueForType( field.type, index );
        writeAssetField( source, field.pFieldname, pValue );
        ++written;
    }

    ASSERT_GT( written, 0 ) << pTypeName << " has no writable fields; the test is broken.";

    source->copyTo( target );

    for( U32 index = 0; index < (U32)fields.size(); ++index )
    {
        const AbstractClassRep::Field& field = fields[index];
        if ( !isCopyableField( field ) )
            continue;

        // getDataField answers out of Con::getData's rotating buffer, so the two
        // reads cannot both be live at once -- comparing them directly would
        // compare the second string with itself and pass no matter what.
        char expected[256];
        dStrncpy( expected, readAssetField( source, field.pFieldname ), sizeof( expected ) - 1 );
        expected[sizeof( expected ) - 1] = '\0';

        ASSERT_STREQ( readAssetField( target, field.pFieldname ), expected )
            << pTypeName << "::" << field.pFieldname << " did not copy. If this field "
               "is new, it needs nothing: copyTo walks the field table. If it keeps "
               "its state somewhere a field cannot describe, it belongs in that "
               "type's copyAssetStateTo.";
    }

    source->deleteObject();
    target->deleteObject();
}

TEST( AssetStateCopyTests, EveryImageAssetFieldCopies )
{
    assertEveryFieldCopies<ImageAsset>( "ImageAsset" );
}

// This no longer covers named cells mode, and nothing else does either. That is
// on purpose: the mode stopped being a field, so there is nothing for a copy to
// carry -- it is read from whichever image the copy points at, which makes it
// right by construction. What a copy still has to carry is both frame lists, and
// AnimationAssetCarriesBothFrameLists above is what asserts that.
TEST( AssetStateCopyTests, EveryAnimationAssetFieldCopies )
{
    assertEveryFieldCopies<AnimationAsset>( "AnimationAsset" );
}

TEST( AssetStateCopyTests, EveryAudioAssetFieldCopies )
{
    assertEveryFieldCopies<AudioAsset>( "AudioAsset" );
}

TEST( AssetStateCopyTests, EveryFontAssetFieldCopies )
{
    assertEveryFieldCopies<FontAsset>( "FontAsset" );
}

TEST( AssetStateCopyTests, EveryParticleAssetFieldCopies )
{
    assertEveryFieldCopies<ParticleAsset>( "ParticleAsset" );
}

TEST( AssetStateCopyTests, EveryParticleAssetEmitterFieldCopies )
{
    assertEveryFieldCopies<ParticleAssetEmitter>( "ParticleAssetEmitter" );
}

#endif // TORQUE_SHIPPING
