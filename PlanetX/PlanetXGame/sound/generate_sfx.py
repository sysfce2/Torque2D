#!/usr/bin/env python3
"""Generate PlanetX placeholder sound effects.

Synthesizes the game's nine SFX as mono, 16-bit PCM, 22050 Hz WAV files (the
format the engine already loads for steam.wav) directly into this folder, using
only the Python standard library. Re-run after tweaking a recipe:

    python generate_sfx.py

These are hand-tuned placeholders meant to be good enough to ship; if you replace
one, keep the file name (the AssetName in the sibling .audio.taml is stable).
"""

import math
import os
import random
import struct
import wave

RATE = 22050
HEADROOM = 0.85          # peak target after normalization (leaves a little room)
HERE = os.path.dirname(os.path.abspath(__file__))

random.seed(20260717)    # reproducible noise


# --- tiny synth toolkit --------------------------------------------------------

def _n(seconds):
    return int(seconds * RATE)


def silence(seconds):
    return [0.0] * _n(seconds)


def add(base, layer, at=0.0):
    """Mix `layer` into `base` starting at time `at` (seconds), growing base."""
    start = _n(at)
    need = start + len(layer)
    if need > len(base):
        base.extend([0.0] * (need - len(base)))
    for i, s in enumerate(layer):
        base[start + i] += s
    return base


def tone(seconds, f0, f1=None, wave_kind="sine", detune=0.0):
    """A gliding oscillator from f0 to f1 (defaults to constant f0)."""
    f1 = f0 if f1 is None else f1
    out = []
    phase = 0.0
    total = _n(seconds)
    for i in range(total):
        t = i / total
        freq = f0 + (f1 - f0) * t
        phase += 2.0 * math.pi * freq / RATE
        if wave_kind == "sine":
            v = math.sin(phase)
        elif wave_kind == "saw":
            frac = (phase / (2.0 * math.pi)) % 1.0
            v = 2.0 * frac - 1.0
        elif wave_kind == "square":
            v = 1.0 if math.sin(phase) >= 0.0 else -1.0
        elif wave_kind == "triangle":
            frac = (phase / (2.0 * math.pi)) % 1.0
            v = 4.0 * abs(frac - 0.5) - 1.0
        else:
            v = 0.0
        if detune:
            v = 0.5 * v + 0.5 * math.sin(phase * (1.0 + detune))
        out.append(v)
    return out


def noise(seconds):
    return [random.uniform(-1.0, 1.0) for _ in range(_n(seconds))]


def vibrato(samples, depth_hz, rate_hz, base_hz):
    """Approximate vibrato by amplitude-shaping (cheap, good enough for growls)."""
    out = []
    for i, s in enumerate(samples):
        lfo = 1.0 + (depth_hz / base_hz) * math.sin(2.0 * math.pi * rate_hz * i / RATE)
        out.append(s * lfo)
    return out


def low_pass(samples, cutoff_hz):
    """One-pole low-pass filter."""
    rc = 1.0 / (2.0 * math.pi * cutoff_hz)
    dt = 1.0 / RATE
    a = dt / (rc + dt)
    out = []
    y = 0.0
    for x in samples:
        y = y + a * (x - y)
        out.append(y)
    return out


def high_pass(samples, cutoff_hz):
    rc = 1.0 / (2.0 * math.pi * cutoff_hz)
    dt = 1.0 / RATE
    a = rc / (rc + dt)
    out = []
    y = 0.0
    prev = 0.0
    for x in samples:
        y = a * (y + x - prev)
        prev = x
        out.append(y)
    return out


def envelope(samples, attack=0.005, decay=None, release=0.02, sustain=1.0, hold=None):
    """Simple AHR envelope over the whole buffer; `decay` unused knob kept simple.

    attack: fade-in seconds. release: fade-out seconds at the tail. hold: exp decay
    time-constant across the body (None = flat sustain).
    """
    n = len(samples)
    a = _n(attack)
    r = _n(release)
    out = list(samples)
    for i in range(n):
        g = sustain
        if i < a:
            g *= i / max(1, a)
        if i > n - r:
            g *= max(0.0, (n - i) / max(1, r))
        if hold:
            g *= math.exp(-(i / RATE) / hold)
        out[i] *= g
    return out


def normalize(samples, peak=HEADROOM):
    hi = max((abs(s) for s in samples), default=0.0)
    if hi <= 1e-9:
        return samples
    k = peak / hi
    return [s * k for s in samples]


def write_wav(name, samples):
    samples = normalize(samples)
    path = os.path.join(HERE, name + ".wav")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        frames = bytearray()
        for s in samples:
            v = int(max(-1.0, min(1.0, s)) * 32767.0)
            frames += struct.pack("<h", v)
        w.writeframes(bytes(frames))
    # sibling asset
    taml = os.path.join(HERE, name + ".audio.taml")
    with open(taml, "w", encoding="utf-8") as f:
        f.write('<AudioAsset\n')
        f.write('    AssetName="%s"\n' % name)
        f.write('    AudioFile="%s.wav"\n' % name)
        f.write('    VolumeChannel="1" />\n')
    print("wrote %s.wav (%d samples) + %s.audio.taml" % (name, len(samples), name))


# --- the nine sounds -----------------------------------------------------------

def footstep():
    # Soft "thmp": a low-passed noise burst with a bit of low-frequency body.
    body = low_pass(noise(0.07), 700)
    body = envelope(body, attack=0.002, release=0.02, hold=0.02)
    thump = envelope(tone(0.07, 130, 90, "sine"), attack=0.002, release=0.03, hold=0.03)
    out = add(silence(0.07), body)
    out = add(out, [0.6 * s for s in thump])
    return out


def enemy_chase():
    # Low menacing growl: descending saw with vibrato + a little grit.
    saw = tone(0.25, 95, 72, "saw")
    saw = vibrato(saw, depth_hz=6, rate_hz=13, base_hz=85)
    grit = low_pass(noise(0.25), 1200)
    out = add(silence(0.25), saw)
    out = add(out, [0.2 * s for s in grit])
    return envelope(out, attack=0.02, release=0.06, hold=0.35)


def enemy_death():
    # Squishy splat: a fast band-limited noise burst + a descending tone.
    splat = high_pass(low_pass(noise(0.18), 2600), 300)
    splat = envelope(splat, attack=0.001, release=0.05, hold=0.05)
    drop = envelope(tone(0.18, 420, 120, "saw"), attack=0.002, release=0.05, hold=0.08)
    out = add(silence(0.18), splat)
    out = add(out, [0.7 * s for s in drop])
    return out


def enemy_give_up():
    # Deflating "boop-down": a descending sine with a soft decay.
    glide = tone(0.2, 400, 200, "sine")
    return envelope(glide, attack=0.008, release=0.05, hold=0.12)


def crystal_get():
    # Bright bell-like ascending arpeggio C-E-G with a shimmering second harmonic.
    notes = [523.25, 659.25, 783.99]
    out = silence(0.45)
    for i, f in enumerate(notes):
        partial = tone(0.28, f, f, "sine")
        shimmer = [0.35 * s for s in tone(0.28, f * 2.0, f * 2.0, "sine")]
        note = [a + b for a, b in zip(partial, shimmer)]
        note = envelope(note, attack=0.004, release=0.1, hold=0.16)
        out = add(out, note, at=i * 0.08)
    return out


def player_death():
    # Somber boom: descending saw with a sub-sine and a fading noise wash.
    saw = tone(0.7, 300, 60, "saw")
    sub = [0.6 * s for s in tone(0.7, 150, 45, "sine")]
    wash = [0.25 * s for s in low_pass(noise(0.7), 900)]
    out = [a + b + c for a, b, c in zip(saw, sub, wash)]
    return envelope(out, attack=0.006, release=0.2, hold=0.4)


def level_start():
    # Hopeful rising cue: two ascending notes (G4 -> C5) with a soft harmonic.
    out = silence(0.5)
    for i, f in enumerate([392.0, 523.25]):
        n1 = tone(0.3, f, f, "triangle")
        n2 = [0.4 * s for s in tone(0.3, f * 1.5, f * 1.5, "sine")]
        note = [a + b for a, b in zip(n1, n2)]
        note = envelope(note, attack=0.01, release=0.09, hold=0.22)
        out = add(out, note, at=i * 0.16)
    return out


def ui_click():
    # Crisp tick: a very short triangle blip.
    blip = tone(0.04, 1000, 900, "triangle")
    return envelope(blip, attack=0.001, release=0.015, hold=0.02)


def player_hurt():
    # Harsh grunt: a short square with a slight downward pitch + noise edge.
    sq = tone(0.15, 210, 150, "square")
    edge = [0.3 * s for s in high_pass(noise(0.15), 500)]
    out = [a + b for a, b in zip(sq, edge)]
    return envelope(out, attack=0.003, release=0.05, hold=0.09)


SOUNDS = {
    "footstep": footstep,
    "enemyChase": enemy_chase,
    "enemyDeath": enemy_death,
    "enemyGiveUp": enemy_give_up,
    "crystalGet": crystal_get,
    "playerDeath": player_death,
    "levelStart": level_start,
    "uiClick": ui_click,
    "playerHurt": player_hurt,
}


def main():
    for name, fn in SOUNDS.items():
        write_wav(name, fn())


if __name__ == "__main__":
    main()
