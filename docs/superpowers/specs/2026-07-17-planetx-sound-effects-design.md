# PlanetX Sound Effects — Design

**Date:** 2026-07-17
**Branch:** planetX
**Status:** Implemented and shipped. Refined during playtest (distance falloff removed, music ducked); this doc reflects the as-built result.

## Goal

Add nine sound-effect triggers to the PlanetX demo game, wired to existing
gameplay events. Placeholder `.wav` files are synthesized now and aimed to be
good enough to keep; asset names are stable so any file can be swapped later by
overwriting the `.wav` in place. Enemy sounds are rate-limited, and the music is
ducked under the SFX, so a dense swarm does not turn into a wall of noise.

## Decisions (resolved during brainstorming)

1. **Sound source — Hybrid.** Synthesize placeholder SFX *and* do the full
   wiring now, so the game is complete and playable immediately. Asset names stay
   fixed so nicer files can be dropped in later. Sounds are tuned to be pleasant
   enough to keep.
2. **Enemy chase / give-up scope — all enemies, throttled.** Any bug or brute can
   trigger the chase/give-up sounds; a per-asset throttle keeps a mass aggro from
   stacking. (Enemy sounds play at full volume — see Decision 3.)
3. **Distance audio — tried, then removed.** A script-only volume falloff
   (`alxSourcef(handle, AL_GAIN_LINEAR, gain)`) was built and verified in-engine,
   but in playtest enemies aggro from far enough that the attenuated sound was too
   quiet against the rest of the mix. It was removed: enemy sounds now play at
   **full volume**, and the **music is ducked** instead (`Audio.SetMusicVolume`,
   `$PlanetX::MusicVolume`) so effects cut through. No stereo panning either (that
   would need the engine's `mIs3D` path, out of scope).
4. **All changes stay in the PlanetXGame module.** The `Audio` library module
   (`PlanetX/Audio/1/`) is a direct import from the shared module library and must
   **not** be modified. We only *call* its existing public API (`Audio.PlaySound`,
   `Audio.SetMusicVolume`) and layer game-side behavior (throttle, music duck) on
   top, inside `PlanetXGame`.
5. **Enemies announce sound events; the level handles them.** Enemies do not touch
   audio or reference the level directly. They raise object-to-object events
   (`EnemyStartChase`, `EnemyStopChase`, `EnemyDeath`); the level listens to each
   enemy it spawns and implements `onEnemyStartChase` / `onEnemyStopChase` /
   `onEnemyDeath`, where it owns the throttling. This is the same event mechanism
   `PlanetXGame` already uses with `ScreenFade`.

## Background: how audio already works here

- A global `Audio` object (the `Audio` library module, `PlanetX/Audio/1/audio.cs`)
  exposes `Audio.PlaySound("PlanetXGame:<name>")` and `Audio.PlayMusic(...)`.
- Each SFX is a `.wav` plus a sibling `.audio.taml` `AudioAsset` in
  `PlanetX/PlanetXGame/sound/`, on `VolumeChannel="1"` (the SFX channel; music is
  channel 0). Today only `laser` and `steamHiss` exist.
- The module's `module.taml` already globs `sound/*.audio.taml` recursively, so
  **new sound assets are auto-declared — no `module.taml` change is needed.**
- AssetId is `PlanetXGame:<AssetName>` (ModuleId + AssetName).

### Verified engine facts

> The `AL_GAIN_LINEAR` falloff below was implemented and then removed in playtest
> (see Decision 3). It is kept here because the mechanism still works if per-source
> volume is ever wanted again.

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
- **The engine loads only PCM WAV and OGG.** A-law/mu-law WAVs (`wFormatTag` 6/7)
  do not decode — a swapped-in SFX must be exported as 16-bit PCM WAV or OGG, or it
  silently fails to load / plays garbage. (Learned the hard way during playtest;
  two swapped files came in as A-law and had to be converted.)

## The nine sounds

All play on channel 1 (SFX). "Site" is the exact call location.

| # | Asset name  | Trigger | Site | Guard / attenuation |
|---|-------------|---------|------|---------------------|
| 1 | `footstep`  | player is walking | `player.cs` | repeating loop while `moving && state == "playing"` |
| 2 | `enemyChase`| wander → chase transition | `enemy.cs` posts `EnemyStartChase` → `level.cs::onEnemyStartChase` | per-asset throttle (full volume) |
| 3 | `enemyDeath`| enemy killed | `enemy.cs` posts `EnemyDeath` (fatal branch) → `level.cs::onEnemyDeath` | per-asset throttle (full volume) |
| 4 | `enemyGiveUp`| chase → wander transition | `enemy.cs` posts `EnemyStopChase` → `level.cs::onEnemyStopChase` | per-asset throttle (full volume) |
| 5 | `crystalGet`| crystal secured | `game.cs::onWin` | one-shot |
| 6 | `playerDeath`| player dies | `game.cs::onPlayerDeath` | one-shot |
| 7 | `levelStart`| level built/entered | `level.cs::onAdd` (end) | one-shot; covers new game, next level, and retry |
| 8 | `uiClick`   | menu/dialog button pressed | 6 buttons in 3 `.gui.taml` files | per-button |
| 9 | `playerHurt`| player takes non-fatal damage | `player.cs::takeDamage` | only when `health > 0` after the hit |

### How the object-to-object event system works (verified in engine source)

- `emitter.postEvent("EventName", data)` calls `on"EventName"` on every listener
  that implements it (`SimObject::postEvent`, `simObject.cc:1481`), passing the
  single `data` string as the handler's first argument after `%this` (the same way
  `onDialogSwap(%this, %dialog)` receives its payload).
- `listener.startListening(emitter)` registers the listener on the emitter
  (`emitter.addListener(listenerId)`), exactly as `game.cs` does with `ScreenFade`.
- `postEvent` is a **no-op when nobody is listening**, and a **reentrancy guard**
  forbids posting another event on the same object mid-dispatch. Our handlers only
  play a sound (no re-posting), so both are safe.
- The sound is chosen by the event *name*; the events carry no payload. (An earlier
  version passed the enemy's position for distance attenuation, since removed.)

## Component 1 — enemy events (`enemy.cs`)

Enemies announce state changes and know nothing about audio or the level. The
event name selects the sound; the events carry no payload.

`updateChase` has no persistent chase/wander state today, so add a `%this.chasing`
flag (initialized `false` in `init()`) and post only on the **transition**, not
every tick:

- `%wantChase = (%distance < %this.aggroRadius)`.
- `%wantChase && !%this.chasing` → `%this.postEvent("EnemyStartChase")`; set `chasing = true`.
- `!%wantChase && %this.chasing` → `%this.postEvent("EnemyStopChase")`; set `chasing = false`.

The existing chase/wander movement logic is unchanged. In `takeDamage`, the fatal
branch posts `%this.postEvent("EnemyDeath")` before `safeDelete()`. The existing
`playBurst` visual call in that branch stays as-is — this change only adds the
sound event, so the enemy gains no new coupling beyond `postEvent` on itself.

## Component 2 — level listens and plays (`level.cs`)

The level subscribes to every enemy it spawns and owns the sound policy. Because
one listener (the level) handles the whole swarm, the throttle state is naturally
global — "40 bugs aggro on one tick → one `enemyChase`."

**Subscribe on spawn.** In `spawnBug` and `spawnBrute`, after `PlanetXScene.add`,
add `%this.startListening(%enemy)` (the level starts listening to that enemy). No
teardown needed: enemies never outlive the level, and a deleted enemy's listener
list is freed with it.

**Handlers** map each event to a throttled, full-volume sound:

```cs
function PlanetXLevel::onEnemyStartChase(%this) { %this.playEnemySound("PlanetXGame:enemyChase"); }
function PlanetXLevel::onEnemyStopChase(%this)  { %this.playEnemySound("PlanetXGame:enemyGiveUp"); }
function PlanetXLevel::onEnemyDeath(%this)       { %this.playEnemySound("PlanetXGame:enemyDeath"); }
```

**The service** mirrors the existing `playBurst` level-wide service and calls only
the Audio module's unmodified public `Audio.PlaySound`:

```cs
$PlanetX::EnemySoundThrottleMs = 200;  // per-asset rate limit, shared across enemies

/// Play an enemy event's sound at full volume, rate-limited per asset name across
/// the whole swarm so a mass aggro or a chain of deaths does not blare.
function PlanetXLevel::playEnemySound(%this, %name)
{
    %now = getSimTime();
    if (%now - %this.lastEnemySound[%name] < $PlanetX::EnemySoundThrottleMs)
        return;
    %this.lastEnemySound[%name] = %now;

    Audio.PlaySound(%name);
}
```

- Throttle state is per-asset (`%this.lastEnemySound[%name]`), so `enemyChase`,
  `enemyDeath`, and `enemyGiveUp` rate-limit independently; the first play of a
  name reads `""` → always passes. State lives on the level, so it resets each new
  level.
- `getSimTime()` is the sim clock the AI ticks already run on. The 200 ms throttle
  suits the swapped-in ~1 s enemy clips — a longer clip wants a longer throttle so a
  new one does not start before the last has mostly played.

The one-shots (#5, #6, #7, #8, #9) and the footstep loop (#1) call the existing
`Audio.PlaySound(...)` directly. Music is ducked once at startup
(`PlanetXGame::create` → `Audio.SetMusicVolume($PlanetX::MusicVolume)`, 0.6) so the
SFX on channel 1 cut through the soundtrack on channel 0.

## Component 3 — player footsteps (`player.cs`)

A self-rescheduling loop, following the same "cancel in `onRemove`" pattern the
crystal pulse and enemy chase tick already use:

- `$PlanetX::FootstepIntervalMs` = 200 ms — half the 400 ms walk cycle
  (`spaceman_walk.animation.taml`), i.e. one step per foot-plant.
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

Each `Command` calls a shared `PlanetXGame.playClick()` helper first (a one-liner
that plays `uiClick`), e.g. `Command="PlanetXGame.playClick(); PlanetXGame.startGame();"`
— this avoids XML-escaping nested quotes in the attribute and keeps the asset id in
one place. Known limitation: the QUIT button's click may be cut off by the app
exiting — acceptable (quitting needs no audible feedback).

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

**In:** the nine triggers, the event-driven enemy→level sound wiring, the level's
`playEnemySound` throttle service, music ducking, the generator.

**Out:** distance/volume falloff (built, then removed in playtest), stereo/3D
panning, per-sound pitch randomization, footstep surface variation, positional
music, any engine change.

## Conventions & verification

- Every script change is a small addition to an existing class, following
  `TORQUE_SCRIPT.md` (self-configuring objects; cancel self-rescheduling events in
  `onRemove`). Run the **`checking-torquescript-conventions`** skill on the changed
  `.cs` files before finishing.
- No unit tests exist for audio; verification is **manual**: build/run PlanetX and
  confirm each of the nine sounds fires at its event, that footsteps start/stop
  with movement, that a killing blow plays only the death sound, and that a dense
  swarm is tamed by the throttle rather than blaring.

## Files touched

- `PlanetX/PlanetXGame/scripts/level.cs` — `startListening` on each spawned enemy; `onEnemyStartChase`/`onEnemyStopChase`/`onEnemyDeath` handlers; `playEnemySound` service + sound constants; `levelStart`.
- `PlanetX/PlanetXGame/scripts/enemy.cs` — `chasing` flag; post `EnemyStartChase`/`EnemyStopChase` on transition; post `EnemyDeath` on the fatal hit.
- `PlanetX/PlanetXGame/scripts/player.cs` — footstep loop + `onRemove` cancel; `playerHurt`.
- `PlanetX/PlanetXGame/game.cs` — `crystalGet` (onWin), `playerDeath` (onPlayerDeath); `playClick` helper; music duck (`$PlanetX::MusicVolume`, `Audio.SetMusicVolume` in `create`).
- `PlanetX/PlanetXGame/gui/{titleGui,victoryGui,gameOverGui}.gui.taml` — `uiClick` on 6 buttons.
- `PlanetX/PlanetXGame/sound/generate_sfx.py` — new generator.
- `PlanetX/PlanetXGame/sound/<name>.wav` + `<name>.audio.taml` — 9 new assets.

**Not touched:** `PlanetX/Audio/1/` (shared library import — call-only), and
`module.taml` (existing `sound/*.audio.taml` glob covers the new assets).
