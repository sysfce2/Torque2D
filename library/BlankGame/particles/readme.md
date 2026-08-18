# particles

Particle effects — explosions, smoke, sparks, rain — live here.

Each effect is a single `.particle.taml` describing one or more emitters and how
every one of their properties changes over the life of the effect:

```xml
<ParticleAsset
    AssetName="playerDeath"
    Lifetime="0.2"
    LifeMode="STOP">
    ...
</ParticleAsset>
```

A `ParticlePlayer` in your scene then plays it by asset id,
`YourModule:playerDeath` — where `YourModule` is the `ModuleId` at the top of the
`module.taml` next to this folder.

This folder is scanned **recursively**, so subfolders are fine.

These are not files to write by hand. Open the editor with **Ctrl + ~**: the
Asset Manager's particle editor plays the effect live while you drag its curves,
which is the only practical way to tune one.

This file is only here to keep the folder around in an empty project. Delete it
whenever you like.
