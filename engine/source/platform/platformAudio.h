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

#ifndef _PLATFORMAUDIO_H_
#define _PLATFORMAUDIO_H_

#ifndef _PLATFORM_H_
#include "platform/platform.h"
#endif

#ifndef _PLATFORMAL_H_
#include "platform/platformAL.h"
#endif

#ifndef _MMATH_H_
#include "math/mMath.h"
#endif

#ifndef _BITSET_H_
#include "collection/bitSet.h"
#endif

typedef U32 AUDIOHANDLE;
#define NULL_AUDIOHANDLE 0

//--------------------------------------------------------------------------

namespace Audio
{
   enum Constants {

      AudioVolumeChannels = 32,

      /// The channel an editor auditions an asset on.
      ///
      /// Reserved so a preview cannot be silenced by, or interfere with, the
      /// running game's mix: a game that has turned its music channel down to
      /// nothing would otherwise make the Asset Manager unable to play a music
      /// asset at all, because alxCreateSource refuses to build a source on a
      /// muted channel rather than building a quiet one.
      ///
      /// The last channel rather than the first free one, because channel
      /// numbering is a per-game convention with no engine meaning and games
      /// count up from zero.
      AudioPreviewChannel = AudioVolumeChannels - 1
   };

   //--------------------------------------
   // sound property description
   struct Description
   {
      F32  mVolume;    // 0-1    1=loudest volume
      S32  mVolumeChannel;
      bool mIsLooping;
      bool mIsStreaming;
      bool mIsPriority; // when the voice pool is full, a priority sound is only culled if every other source is also a priority sound

      bool mIs3D;
      F32  mReferenceDistance;
      F32  mMaxDistance;
      U32  mConeInsideAngle;
      U32  mConeOutsideAngle;
      F32  mConeOutsideVolume;
      Point3F mConeVector;

      // environment info
      F32 mEnvironmentLevel;
   };

   void initOpenAL();
   void shutdownOpenAL();
   void destroy();
}   

class AudioDescription;
class AudioAsset;
class AudioEnvironment;
class AudioSampleEnvironment;
class AudioStreamSource;

AUDIOHANDLE alxCreateSource(const Audio::Description *desc, const char *filename, const MatrixF *transform=NULL, AudioSampleEnvironment * sampleEnvironment = 0);
AUDIOHANDLE alxCreateSource(AudioDescription *descObject, const char *filename, const MatrixF *transform=NULL, AudioSampleEnvironment * sampleEnvironment = 0);
AUDIOHANDLE alxCreateSource(const AudioAsset *profile, const MatrixF *transform=NULL);
AUDIOHANDLE alxCreateSource_AD(const AudioAsset *profile, const AudioDescription* description, const MatrixF *transform);
AudioStreamSource* alxFindAudioStreamSource(AUDIOHANDLE handle);

AUDIOHANDLE alxPlay(AUDIOHANDLE handle);
bool alxPause(AUDIOHANDLE handle);
void alxPauseAll();
void alxUnPause(AUDIOHANDLE handle);
void alxUnPauseAll();
void alxStop(AUDIOHANDLE handle);
void alxStopAll();

// one-shot helper alxPlay functions, create and play in one call
AUDIOHANDLE alxPlay(const AudioAsset *profile, const MatrixF *transform=NULL, const Point3F *velocity=NULL);

/// Audition an asset as it was authored, ignoring the running game's mix.
///
/// Same file, looping and streaming flags as alxPlay, but at full volume on
/// Audio::AudioPreviewChannel and past the master volume, so an editor hears the
/// asset itself rather than the asset as this particular game happens to be
/// mixing it. For editor previews only -- a game wants alxPlay.
AUDIOHANDLE alxPlayPreview(const AudioAsset *profile);


// Source
void alxSourcef(AUDIOHANDLE handle, ALenum pname, ALfloat value);
void alxSourcefv(AUDIOHANDLE handle, ALenum pname, ALfloat *values);
void alxSource3f(AUDIOHANDLE handle, ALenum pname, ALfloat value1, ALfloat value2, ALfloat value3);
void alxSourcei(AUDIOHANDLE handle, ALenum pname, ALint value);
void alxSourceMatrixF(AUDIOHANDLE handle, const MatrixF *transform);

void alxGetSourcef(AUDIOHANDLE handle, ALenum pname, ALfloat *value);
void alxGetSourcefv(AUDIOHANDLE handle, ALenum pname, ALfloat *values);
void alxGetSource3f(AUDIOHANDLE handle, ALenum pname, ALfloat *value1, ALfloat *value2, ALfloat *value3);
void alxGetSourcei(AUDIOHANDLE handle, ALenum pname, ALint *value);

/**   alSource3f access extension for use with Point3F's
*/

inline void alxSourcePoint3F(AUDIOHANDLE handle, ALenum pname, const Point3F *value)
{
   alxSource3f(handle, pname, value->x, value->y, value->z);
}

/**   alGetSource3f access extension for use with Point3F's
*/

inline void alxSourceGetPoint3F(AUDIOHANDLE handle, ALenum pname, Point3F * value)
{
   alxGetSource3f(handle, pname, &value->x, &value->y, &value->z);
}

// Listener

void alxListenerMatrixF(const MatrixF *transform);
void alxListenerf(ALenum param, ALfloat value);
void alxGetListenerf(ALenum param, ALfloat *value);


/**   alListener3f access extension for use with Point3F's
*/

inline void alxListenerPoint3F(ALenum pname, const Point3F *value)
{
   ALfloat ptArray[10];
   ptArray[0] = value->x;
   ptArray[1] = value->y;
   ptArray[2] = value->z;
   alListenerfv(pname, ptArray);
}

/**   alGetListener3f access extension for use with Point3F's
*/

inline void alxGetListenerPoint3F(ALenum pname, Point3F *value)
{
   ALfloat ptArray[10];
   ptArray[0] = value->x;
   ptArray[1] = value->y;
   ptArray[2] = value->z;
   alGetListenerfv(pname, ptArray);
   value->x = ptArray[0];
   value->y = ptArray[1];
   value->z = ptArray[2];
}

// Environment
void alxEnvironmenti(ALenum pname, ALint value);
void alxEnvironmentf(ALenum pname, ALfloat value);
void alxGetEnvironmenti(ALenum pname, ALint * value);
void alxGetEnvironmentf(ALenum pname, ALfloat * value);

void alxSetEnvironment(const AudioEnvironment * environment);
const AudioEnvironment * alxGetEnvironment();

// misc
void alxUpdateTypeGain(U32 type);
bool alxIsValidHandle(AUDIOHANDLE handle);
bool alxIsPlaying(AUDIOHANDLE handle);
void alxUpdate();
F32 alxGetStreamPosition( AUDIOHANDLE handle );
F32 alxGetStreamDuration( AUDIOHANDLE handle );

#endif  // _H_PLATFORMAUDIO_
