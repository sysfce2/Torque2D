# sprites

Your game's images and the animations built from them live here.

An image file is not an asset on its own. It becomes one when a `.image.taml`
sits beside it naming it:

```xml
<ImageAsset
    AssetName="playerShip"
    ImageFile="playerShip.png"/>
```

An animation is a `.animation.taml` that names frames out of one of those
images:

```xml
<AnimationAsset
    AssetName="playerShipThrust"
    Image="@asset=YourModule:playerShip"
    AnimationFrames="0 1 2 3"
    AnimationTime="0.4"/>
```

Anywhere your game asks for an image or an animation, it names it
`YourModule:playerShip` — where `YourModule` is the `ModuleId` at the top of the
`module.taml` next to this folder.

This folder is scanned **recursively**, so arrange it into subfolders however
suits you; the asset ids do not change when a file moves.

You do not have to write any of this by hand. Open the editor with **Ctrl + ~**
and the Asset Manager will create the files, cut a sprite sheet into frames, and
preview an animation as you build it.

This file is only here to keep the folder around in an empty project. Delete it
whenever you like.
