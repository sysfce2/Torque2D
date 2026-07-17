# PlanetX Sound Effects — Design

**Date:** 2026-07-17
**Branch:** planetX
**Status:** Approved design, ready for implementation plan

## Goal

Add nine sound-effect triggers to the PlanetX demo game, wired to existing
gameplay events. Placeholder `.wav` files are synthesized now and aimed to be
good enough to keep; asset names are stable so any file can be swapped later by
overwriting the `.wav` in place. Enemy sounds attenuate with distance so a dense
swarm does not turn into a wall of noise.

## Decisions (resolved during brainstorming)

1. **Sound source — Hybrid.** Synthesize placeholder SFX *and* do the full
   wiring now, so the game is complete and playable immediately. Asset names stay
   fixed so nicer files can be dropped in later. Sounds are tuned to be pleasant
   enough to keep.
2. **Enemy chase / give-up scope — all enemies, throttled + distance-attenuated.**
   Any bug or brute can trigger the chase/give-up sounds, but each asset is
   rate-limited and its volume falls off with distance to the player.
3. **Distance audio — script-only volume falloff (no engine change).** Use
   `alxSourcef(handle, AL_GAIN_LINEAR, gain)` after playing. No stereo panning
   (that would need the engine's `mIs3D` path, deliberately out of scope).
4. **All changes stay in the PlanetXGame module.** The `Audio` library module
   (`PlanetX/Audio/1/`) is a direct import from the shared module library and must
   **not** be modified. We only *call* its existing public API (`Audio.PlaySound`)
   and layer game-side behavior (throttle, gain) on top, inside `PlanetXGame`.

## Background: how audio already works here

- A global `Audio` object (the `Audio` library module, `PlanetX/Audio/1/audio.cs`)
  exposes `Audio.PlaySound("PlanetXGame:<name>")` and `Audio.PlayMusic(...)`.
- Each SFX is a `.wav` plus a sibling `.audio.taml` `AudioAsset` in
  `PlanetX/PlanetXGame/sound/`, on `VolumeChannel="1"` (the SFX channel; music is
  channel 0). Today only `laser` and `steamHiss` exist.
- The module's `module.taml` already globs `sound/*.audio.taml` recursively, so
  **new sound assets are auto-declared — no `module.taml` change is needed.**
- AssetId is `PlanetXGame:<AssetName>` (ModuleId + AssetName).

### Verified engine facts (basis for the distance-falloff approach)

- `AL_GAIN_LINEAR` is a settable per-source float
  (`audio_ScriptBinding.cc:83`); barewords like `AL_GAIN_LINEAR` resolve to the
  enum, matching the existing `Audio::SetPitch` usage of `AL_PITCH`.
- `alxSourcef(handle, AL_GAIN_LINEAR, v)` stores the un-attenuated per-source gain
  in `mSourceVolume[]` (`audio.cc:1454`); the per-frame update *multiplies*
  channel and master volume into that stored value (`audio.cc:1468`, `:2140`), so
  a gain set right after `alxPlay` **persists** and is not clobbered.
- `AudioAsset` exposes only `AudioFile`/`Volume`/`VolumeChannel`/`Looping`/
  `Streaming` — **not** the `mIs3D` / reference-distance fields — which is why
  true 3D panning is out of scope without an engine change.
- Existing SFX are 16-bit PCM; the engine loads both mono/22050 (`steam.wav`) and
  stereo/44100 (`laser.wav`). New SFX will be **mono, 16-bit PCM, 22050 Hz**.

## The nine sounds

All play on channel 1 (SFX). "Site" is the exact call location.

| # | Asset name  | Trigger | Site | Guard / attenuation |
|---|-------------|---------|------|---------------------|
| 1 | `footstep`  | player is walking | `player.cs` | repeating loop while `moving && state == "playing"` |
| 2 | `enemyChase`| wander → chase transition | `enemy.cs::updateChase` | distance falloff + per-asset throttle |
| 3 | `enemyDeath`| enemy killed | `enemy.cs::takeDamage` (fatal branch) | distance falloff + per-asset throttle |
| 4 | `enemyGiveUp`| chase → wander transition | `enemy.cs::updateChase` | distance falloff + per-asset throttle |
| 5 | `crystalGet`| crystal secured | `game.cs::onWin` | one-shot |
| 6 | `playerDeath`| player dies | `game.cs::onPlayerDeath` | one-shot |
| 7 | `levelStart`| level built/entered | `level.cs::onAdd` (end) | one-shot; covers new game, next level, and retry |
| 8 | `uiClick`   | menu/dialog button pressed | 6 buttons in 3 `.gui.taml` files | per-button |
| 9 | `playerHurt`| player takes non-fatal damage | `player.cs::takeDamage` | only when `health > 0` after the hit |

## Component 1 — level-wide enemy sound service (`level.cs`)

The shared throttle and the distance→gain mapping live on **`PlanetXLevel`**, the
game-side owner of every enemy. This mirrors the existing `playBurst` effects
service (same file, same "level-wide service" role) and keeps the throttle state
**global across all enemies** — which is what makes "40 bugs aggro on one tick →
one `enemyChase` fires" work. It calls only the Audio module's existing public
`Audio.PlaySound`; the shared module is untouched.

```cs
$PlanetX::SoundRefDistance     = 8;    // full volume within this many world units
$PlanetX::SoundMaxDistance     = 40;   // silent (and skipped) at/beyond this range
$PlanetX::EnemySoundThrottleMs = 150;  // per-asset rate limit, shared across enemies

/// Distance-attenuated one-shot for an enemy event at %position. Silent past
/// SoundMaxDistance (which doubles as the "near the camera" gate, since the camera
/// is mounted to the player), and rate-limited per asset name across the whole
/// swarm so a mass aggro or a chain of deaths does not blare.
function PlanetXLevel::playEnemySound(%this, %name, %position)
{
    if (!isObject(%this.player))
        return;

    %d = Vector2Length(Vector2Sub(%this.player.getPosition(), %position));
    if (%d >= $PlanetX::SoundMaxDistance)
        return;

    %now = getSimTime();
    if (%now - %this.lastEnemySound[%name] < $PlanetX::EnemySoundThrottleMs)
        return;
    %this.lastEnemySound[%name] = %now;

    %gain = mClamp(1 - (%d - $PlanetX::SoundRefDistance)
                       / ($PlanetX::SoundMaxDistance - $PlanetX::SoundRefDistance), 0, 1);

    %handle = Audio.PlaySound(%name);          // shared module's public API, unmodified
    if (%handle)
        alxSourcef(%handle, AL_GAIN_LINEAR, mClamp(%gain, 0, 1));
}
```

- Throttle state is per-asset (`%this.lastEnemySound[%name]`), so `enemyChase`,
  `enemyDeath`, and `enemyGiveUp` rate-limit independently; the first play of a
  name reads `""` → always passes. State lives on the level, so it resets cleanly
  each new level.
- `getSimTime()` is the sim clock the AI ticks already run on; `AL_GAIN_LINEAR`
  via `alxSourcef` matches the existing `AL_PITCH` bareword usage in `audio.cs`.
- Because distance already thins the mix, the throttle is relaxed to ~150 ms
  (down from the ~400 ms a throttle-only design would have needed).

## Component 2 — enemy AI transitions (`enemy.cs`)

`updateChase` currently has no persistent chase/wander state, so add a
`%this.chasing` flag (initialized `false` in `init()`) and fire the sounds only on
the **transition**, not every tick. Each site is guarded by
`isObject(PlanetXGame.level)` (matching the existing `playBurst` call guard):

- Compute `%wantChase = (%distance < %this.aggroRadius)`.
- `%wantChase && !%this.chasing` → `PlanetXGame.level.playEnemySound("PlanetXGame:enemyChase", %this.getPosition())`; set `chasing = true`.
- `!%wantChase && %this.chasing` → `PlanetXGame.level.playEnemySound("PlanetXGame:enemyGiveUp", %this.getPosition())`; set `chasing = false`.

The existing chase/wander movement logic is unchanged. In `takeDamage`, the fatal
branch already runs inside `if (isObject(PlanetXGame.level))` (for `playBurst`);
add `PlanetXGame.level.playEnemySound("PlanetXGame:enemyDeath", %this.getPosition())`
there, before `safeDelete()`, while the object still has a valid position.

The non-distance one-shots (#5, #6, #7, #8, #9) and the footstep loop (#1) call the
existing `Audio.PlaySound(...)` directly.

## Component 3 — player footsteps (`player.cs`)

A self-rescheduling loop, following the same "cancel in `onRemove`" pattern the
crystal pulse and enemy chase tick already use:

- `$PlanetX::FootstepIntervalMs` ≈ 300 ms (tuned to the walk-animation cadence at
  implementation time by reading `spacemanWalkAnim`).
- When `updateVelocity` transitions the player to moving, start the loop if not
  already pending.
- `PlanetXPlayer::footstep(%this)`: if `state == "playing" && moving`, play
  `PlanetXGame:footstep` and reschedule; otherwise stop (drop the event).
- `onRemove` cancels a pending `footstepEvent` (added alongside the existing
  weapon cleanup).

## Component 4 — one-shot event sounds

- **#5 crystal:** `game.cs::onWin` → `Audio.PlaySound("PlanetXGame:crystalGet")`.
- **#6 player death:** `game.cs::onPlayerDeath` → `Audio.PlaySound("PlanetXGame:playerDeath")`.
- **#7 level start:** end of `level.cs::onAdd` → `Audio.PlaySound("PlanetXGame:levelStart")`.
  One site covers new game, next level, and retry (all route through a fresh level).
- **#9 player damage:** in `player.cs::takeDamage`, after applying damage, play
  `PlanetXGame:playerHurt` **only if `%this.health > 0`** — the fatal hit routes
  through `onPlayerDeath` (#6), so a killing blow plays one sound, not two.

## Component 5 — button clicks (#8)

No native profile button-sound exists in this engine, so prepend the click to each
button's `Command` string in the three GUI TAML files (6 buttons total):

- `titleGui.gui.taml`: START MISSION, QUIT
- `victoryGui.gui.taml`: next level, to title
- `gameOverGui.gui.taml`: retry, to title

Example: `Command="Audio.PlaySound(\"PlanetXGame:uiClick\"); PlanetXGame.startGame();"`.
Known limitation: the QUIT button's click may be cut off by the app exiting —
acceptable (quitting needs no audible feedback).

## Component 6 — placeholder SFX generation

A committed Python generator, `PlanetX/PlanetXGame/sound/generate_sfx.py`, writes
the nine `.wav` files (mono, 16-bit PCM, 22050 Hz) so the placeholders are
reproducible and tweakable. It uses only the standard library (`wave`, `struct`,
`math`) — confirmed Python 3.10 is available. Each `.wav` gets a sibling
`.audio.taml` identical in form to the existing ones:

```xml
<AudioAsset AssetName="<name>" AudioFile="<name>.wav" VolumeChannel="1" />
```

Synthesis recipes (tuned for character, not just beeps):

| Asset | Recipe | Approx length |
|-------|--------|---------------|
| `footstep`   | low-passed noise burst, fast decay — soft "tmp" | ~70 ms |
| `enemyChase` | low sawtooth ~90→70 Hz with slight vibrato + noise — a growl | ~250 ms |
| `enemyDeath` | noise burst + descending tone — a squishy splat | ~180 ms |
| `enemyGiveUp`| descending sine ~400→200 Hz — a deflating "boop-down" | ~200 ms |
| `crystalGet` | bell-like ascending arpeggio (e.g. C–E–G sines, decaying) | ~450 ms |
| `playerDeath`| descending sawtooth ~300→60 Hz + noise — a somber boom | ~700 ms |
| `levelStart` | rising two-note / swept sine — a hopeful cue | ~500 ms |
| `uiClick`    | short ~1 kHz triangle blip, fast decay — a tick | ~40 ms |
| `playerHurt` | harsh short square/noise with slight downward pitch — a grunt | ~150 ms |

All tones use short attack/decay envelopes to avoid clicks; peaks normalized below
full scale to leave headroom.

## Scope (YAGNI)

**In:** the nine triggers, one generic Audio helper, distance-based volume falloff
for enemy sounds, the generator.

**Out:** stereo/3D panning, per-sound pitch randomization, footstep surface
variation, positional music, any engine change.

## Conventions & verification

- Every script change is a small addition to an existing class, following
  `TORQUE_SCRIPT.md` (self-configuring objects; cancel self-rescheduling events in
  `onRemove`). Run the **`checking-torquescript-conventions`** skill on the changed
  `.cs` files before finishing.
- No unit tests exist for audio; verification is **manual**: build/run PlanetX and
  confirm each of the nine sounds fires at its event, that footsteps start/stop
  with movement, that a killing blow plays only the death sound, and that a dense
  swarm attenuates with distance rather than blaring.

## Files touched

- `PlanetX/PlanetXGame/scripts/level.cs` — `playEnemySound` service + sound constants; `levelStart`.
- `PlanetX/PlanetXGame/scripts/enemy.cs` — `chasing` flag, chase/give-up transitions, death sound call.
- `PlanetX/PlanetXGame/scripts/player.cs` — footstep loop + `onRemove` cancel; `playerHurt`.
- `PlanetX/PlanetXGame/game.cs` — `crystalGet` (onWin), `playerDeath` (onPlayerDeath).
- `PlanetX/PlanetXGame/gui/{titleGui,victoryGui,gameOverGui}.gui.taml` — `uiClick` on 6 buttons.
- `PlanetX/PlanetXGame/sound/generate_sfx.py` — new generator.
- `PlanetX/PlanetXGame/sound/<name>.wav` + `<name>.audio.taml` — 9 new assets.

**Not touched:** `PlanetX/Audio/1/` (shared library import — call-only), and
`module.taml` (existing `sound/*.audio.taml` glob covers the new assets).
