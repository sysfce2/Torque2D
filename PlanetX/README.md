# PlanetX

A small but complete demo game for Torque2D 4.0, built the same way the
Project Manager scaffolds a real game project. It exists so you can read one
codebase that goes all the way: title screen, gameplay, win/lose dialogs, and
back to the title.

![PlanetX](AppCore/1/projectIcon.png)

## Playing

Launch Torque2D and pick the **PlanetX** card in the Project Selector. Or boot
straight into it from `main.cs` (see the commented PlanetX block there).

Your rocket put you down on the wrong side of the planet. Somewhere out there
is the crystal you came for.

- **WASD / arrow keys** — move
- **Mouse** — aim; **hold left button** — fire
- **Escape** — abandon the mission and return to the title screen

Aliens wander the surface and will swarm you on sight. Contact hurts; your
hull bar is top-left. Touch the crystal to win.

## How it's put together

```
PlanetX/
├── AppCore/1/     project bootstrap (copied from library/AppCore, retinted
│                  to the Rocket Edition palette in gui/guiProfiles.cs)
├── Audio/1/       the standard Audio module (verbatim library copy)
├── ScreenFade/1/  canvas fade transitions (verbatim library copy)
└── PlanetXGame/   the game itself
    ├── game.cs            module lifecycle + the title/playing/won/lost state machine
    ├── scripts/level.cs   scene, CompositeSprite tile map, walls, rocks, rocket
    ├── scripts/player.cs  the spaceman: movement, health, damage
    ├── scripts/input.cs   WASD ActionMap + mouse aim (window-point re-projection)
    ├── scripts/bullet.cs  pooled laser bolts and impact bursts
    ├── scripts/alien.cs   alien factory + contact damage
    ├── scripts/crystal.cs the objective (a static sensor)
    ├── scripts/hud.cs     hull bar + objective hint
    ├── scripts/behaviors/ ChaseBehavior + TakesDamageBehavior (adapted from DeathBallToy)
    └── gui/               title screen, victory + game-over dialogs (TAML)
```

Things worth stealing:

- **Project scaffold** — a top-level folder with its own AppCore is all it
  takes to appear in the Project Selector. Only the project folder is scanned
  at boot, so the project carries copies of every module and asset it uses.
- **Palette retint** — the six colors in `AppCore::SetProfileColors`
  (`AppCore/1/gui/guiProfiles.cs`) restyle every stock GUI profile at once.
- **Angle convention** — `mAtan(Vector2Sub(target, origin))` and
  `setLinearVelocityPolar` both use 0° = +X, counter-clockwise. All PlanetX
  art is drawn facing +X, so no fudge offsets appear anywhere.
- **Pooled projectiles** — `bullet.cs` pre-builds its bolts and bursts
  (TruckToy's pattern) so firing never allocates mid-play.
- **Behaviors** — the alien AI is two small `BehaviorTemplate`s composed onto
  a plain Sprite; `chaseBehavior.cs` shows the self-scheduled tick pattern
  with a game-state guard.
- **Teardown** — `PlanetXGame::teardownLevel` deletes the scene + root GUI
  and rebuilds from scratch for every retry; three loops in a row leak
  nothing (check `PlanetXScene.getCount()` stays constant).

All sprite art was generated for this demo in the Torque2D Rocket Edition
palette (#EA4848 / #A62646 / #801946 / #300022 / #21BF84) and is MIT-licensed
with the engine, like the rest of the project.
