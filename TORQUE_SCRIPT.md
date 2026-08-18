# Writing TorqueScript for Torque2D

Read this **before writing or refactoring any `.cs` game code.** TorqueScript is
permissive — it will happily let you build a game as one giant bag of functions — so the
discipline has to come from you. These rules keep a script codebase readable, leak-free, and
able to grow past a demo.

The examples here are deliberately generic (a `Coin`, an `Enemy`/`Goblin`/`Ogre` hierarchy, a
`Weapon`/`Pistol`, a `HudManager`). Apply the *pattern*, not the names.

---

## 0. The one-sentence version

**Every object is a class in its own file; the object builds itself in `onAdd` and frees
everything it made in `onRemove`; each object owns and destroys the objects it creates.** The
rest of this document is corollaries.

---

## 1. A namespace *is* a class — never build a god-namespace

In TorqueScript, `function Foo::bar(%this)` defines method `bar` on class `Foo`. The engine
calls it a "namespace," but for you it is a class. Therefore:

> **Do not pile unrelated methods into one namespace.** If you find crystal logic, HUD logic,
> bullet logic, and level generation all defined as `function MyGame::*`, you have written one
> class doing everyone's job. That is the anti-pattern this whole document exists to prevent.

The **only** legitimate top-level singleton is the module object — the `%this` passed to your
module's create/destroy functions. It orchestrates; it does not implement everyone's behavior.

---

## 2. One class per file; name the file after the class

Each class gets its own `.cs` file, and the file is named for the class. Every function in that
file belongs to that class.

- A shared prefix or suffix may be dropped from the filename: class `PlanetXCrystal` → file
  `crystal.cs`; class `SpaceEnemy` → `enemy.cs`. The class name keeps the prefix; the file name
  need not.
- If a file contains `function A::foo` and `function B::bar` for two unrelated classes `A` and
  `B`, split it. (A small base class and its one concrete subclass may share a file only if you
  have a reason; prefer one file each.)

---

## 3. Reach for `ScriptObject` — especially for managers and systems

Anything that isn't a visual/physical scene object but still has state and behavior should be a
`ScriptObject` subclass, not a pile of functions on the module namespace. HUD managers, input
controllers, level owners, weapons, spawners — all `ScriptObject`s.

```cpp
%hud = new ScriptObject() { class = "HudManager"; };

function HudManager::onAdd(%this)   { /* build the bars/labels */ }
function HudManager::onRemove(%this){ /* delete them */ }
function HudManager::setHealth(%this, %fraction) { /* ... */ }
```

Creating a `ScriptObject` with a `class` gives you a place to hang state (`%this.foo`) and
methods, and — crucially — lifecycle callbacks (rule 4).

---

## 4. Lifecycle lives in `onAdd` / `onRemove`

The engine **automatically** calls the script `onAdd` when an object is registered and
`onRemove` when it is unregistered/deleted. This is true for **every** `SimObject` — `Sprite`,
`SceneObject`, `ScriptObject`, `GuiControl` — as long as the object has a `class`.
(See `engine/source/sim/simObject.cc`: `registerObject()` → `onAdd`, `unregisterObject()` →
`onRemove`.)

**The `new{}` block is the constructor.** Fields you set inside it are applied *before* `onAdd`
runs, so `onAdd` can read them.

### Who sets what: split by who controls the value

- The **spawner** sets `class` (and `superclass`, rule 8) plus **any value it needs to
  control** — position, an index, a difficulty stat, a target object. Those are the constructor
  arguments.
- The **object's `onAdd`** sets everything that is **always the same** for that class — its
  size, image, collision shape, sub-objects, and any repeating schedules.

```cpp
// Spawner: sets only what IT decides — the class and where the coin goes.
function Level::spawnCoin(%this, %position)
{
    %coin = new Sprite() { class = "Coin"; Position = %position; };
    %this.scene.add(%coin);
}

// The coin looks the same everywhere, so it configures its own appearance.
function Coin::onAdd(%this)
{
    %this.setSize("2 2");
    %this.setImage("MyGame:coin");
    %this.createCircleCollisionShape(1);
    %this.setCollisionShapeIsSensor(0, true);
    %this.spin();                 // starts a repeating schedule
}
```

If the coin *always* appeared at the same spot, `onAdd` would set the position too. The rule is
about control, not about a fixed list of fields.

### Passing parameters (including whole objects) into the constructor

Set any field on the handle in the `new{}` block; `onAdd` reads it back. Object handles are just
integers, so you can pass entire objects the same way.

```cpp
// Spawner makes a row of balls and tells each one its index.
for (%i = 0; %i < 10; %i++)
{
    %ball = new Sprite() { class = "Ball"; i = %i; target = %player; };
    %this.scene.add(%ball);
}

function Ball::onAdd(%this)
{
    // even balls red, odd balls blue — decided from the passed-in index.
    if (%this.i % 2 == 0) %this.setBlendColor(1, 0, 0);
    else                  %this.setBlendColor(0, 0, 1);
    // %this.target is a live object handle passed straight in.
}
```

### `onRemove` frees everything the object created

Whatever an object `new`s, schedules, or listens to in `onAdd`, it must tear down in `onRemove`.
This is the other half of the constructor/destructor pair, and it is what makes rule 5 work.

> Note: `onAdd` runs *before* the spawner adds the object to a Scene/parent, so don't do work in
> `onAdd` that requires scene membership. Setting size/image/collision/schedules is fine.

---

## 5. Ownership is a chain of responsibility

Each object owns — and is responsible for deleting — the objects **it** created. A parent deletes
only its *direct* children; the cascade does the rest:

- `Scene.delete()` unregisters every SceneObject in it → each one's `onRemove` fires.
- A `GuiControl` parent's `delete()` deletes its child controls.
- A manager `ScriptObject` deletes the objects it holds references to, in its `onRemove`.

Done right, deleting the top object recursively frees the entire tree, and **you can never leak
by forgetting a cleanup line** — because cleanup lives next to creation, in the same class.

```
Game (module singleton)
└─ level      (ScriptObject)      Game::onDestroy -> level.delete()
   ├─ scene   (Scene)             Level::onRemove -> scene.delete()  (frees all SceneObjects)
   ├─ hud     (HudManager)        Level::onRemove -> hud.delete()
   └─ player  (SceneObject)
      └─ weapon (Weapon)          Player::onRemove -> weapon.delete()
```

**Adding to a container transfers lifetime ownership.** Once you `Scene.add(%obj)` or
`%parent.add(%control)`, that container owns the object's lifetime — deleting the container frees
it. A manager that *pools* SceneObjects (a weapon with a bullet pool, a level with an effects
pool) therefore does **not** delete those objects in its own `onRemove`; the scene safeDeletes
them when it tears down. The manager's `onRemove` only cancels the schedules and frees the
non-scene objects it still owns:

```cpp
function Weapon::onRemove(%this)
{
    // Bullets were add()ed to the scene -> the scene frees them. We only own the
    // fire/heat schedules, so those are all we cancel here.
    %this.stopFiring();
    if (isEventPending(%this.heatEvent)) cancel(%this.heatEvent);
}
```

**Guard the deletes you *do* make across a boundary with `isObject()`.** When a class genuinely
owns something that lives elsewhere and cascade order isn't guaranteed, check before deleting so a
double-free can't happen:

```cpp
function Level::onRemove(%this)
{
    if (isObject(%this.hud))   %this.hud.delete();     // a ScriptObject we own
    if (isObject(%this.scene)) %this.scene.delete();   // frees every SceneObject in it
}
```

---

## 6. Own by reference (`%this.child`), not by global name

Store what you create on `%this` (`%this.healthBar`, `%this.weapon`), and delete it through that
reference. A globally-named object (`new GuiProgressCtrl(HealthBar){...}`) is fine for engine
*lookup*, but do not let a global name be the *only* thing keeping ownership straight — it makes
teardown depend on remembering every name. Ownership must be explicit and local to the owner.

---

## 7. Track your schedules; cancel them in `onRemove`

`schedule()` returns an event id. A self-rescheduling timer (a pulse, a tick loop) that is never
cancelled keeps firing after its object is gone — an orphaned event. Store the id and cancel it:

```cpp
function Coin::spin(%this)
{
    %this.rotate();
    %this.spinEvent = %this.schedule(600, "spin");   // keep the id
}

function Coin::onRemove(%this)
{
    if (isEventPending(%this.spinEvent))
        cancel(%this.spinEvent);
}
```

A defensive `if (!isObject(%this)) return;` at the top of a scheduled method is a band-aid, not a
substitute for cancelling.

---

## 8. Inheritance: `class` + `superclass`

Set both `class` and `superclass` on an object and the engine builds a **real, linked namespace
hierarchy**. Method lookup walks `class → superclass → …` until it finds the method:

```cpp
%goblin = new Sprite() { class = "Goblin"; superclass = "Enemy"; };
%goblin.attack();   // tries Goblin::attack, then Enemy::attack
```

Two things that surprise people:

1. **The hierarchy is directional and sticky.** Once you create an object linking `Goblin →
   Enemy`, the engine remembers it. Creating another object that links them the other way
   (`Enemy` with `superclass="Goblin"`) is an error. Keep every class's superclass consistent
   everywhere.

2. **Only the most-derived `::onAdd` fires.** There is no automatic constructor chaining — if
   `Goblin::onAdd` exists, `Enemy::onAdd` does **not** also run.

### The `init()` convention (house style for shared setup)

Because `onAdd` doesn't chain, put shared setup in an `init()` method the whole hierarchy shares,
and have each concrete class's `onAdd` call it first:

```cpp
// Base: shared defaults live in init().
function Enemy::init(%this)
{
    %this.health = 3;
    %this.speed  = 5;
    %this.createCircleCollisionShape(0.6);
}
function Enemy::onAdd(%this) { %this.init(); }   // a plain Enemy still gets set up

// Subclass: run the shared init, then add specifics.
function Goblin::onAdd(%this)
{
    %this.init();                       // resolves to Enemy::init
    %this.health = 5;                   // override a default
    %this.setImage("MyGame:goblin");    // goblin-only setup
}
```

One layer of inheritance is usually enough; this pattern extends to deeper trees if you need it.
(The language also supports `Parent::method()` calls, but this codebase does not use them — the
`init()` convention is the house style.)

Prefer passing per-instance values in as constructor parameters (rule 4) over reading globals
inside `init()`; e.g. the spawner sets `%enemy.health = %this.currentDifficultyHealth` and
`init()` reads `%this.health`.

---

## 9. Composition: build it like an object system (has-a)

Model relationships the way you would in any OO language. A player *has-a* weapon; the weapon is
its own `ScriptObject`, held on `%this.weapon`, and called polymorphically. Swapping the concrete
weapon class changes behavior with **no change to the caller**:

```cpp
function Player::onAdd(%this)
{
    %this.weapon = new ScriptObject() { class = "Pistol"; superclass = "Weapon"; };
}
function Player::onRemove(%this)           // owner frees the owned object
{
    if (isObject(%this.weapon))
        %this.weapon.delete();
}
function Player::fire(%this)
{
    %this.weapon.fire(%this.getMuzzlePosition(), %this.aimAngle);   // Pistol or Shotgun — caller doesn't care
}
```

Keep a subsystem's whole responsibility inside its object: a weapon owns its stats, its firing
cadence, its cool-down, and its bullet pool — not scattered across the game namespace.

---

## 10. Keep the global surface minimal — but some things belong there

A few things are legitimately global; the test is whether the engine requires it:

- **`ActionMap` bind targets must be global functions.** `%map.bind("keyboard", "w", "moveUp")`
  calls a bare `function moveUp(%val)`. That's fine — name them clearly and keep them thin,
  delegating to an object: `function moveUp(%val) { $keyUp = %val; Player.updateVelocity(); }`.
- **Genuine game-state singletons** live on the module object (state transitions, the current
  level reference).

Everything else — every value that belongs to one object — lives on that object (`%this.foo`),
not in a `$Global::` variable. Fewer globals, fewer spooky couplings.

---

## 11. Before/after, in miniature

**Before** — one god-namespace, cleanup by hand, a leaked schedule:

```cpp
function MyGame::buildCoin(%this, %pos)
{
    %c = new Sprite() { class = "Coin"; Position = %pos; Size = "2 2"; Image = "MyGame:coin"; };
    %c.createCircleCollisionShape(1);
    %this.scene.add(%c);
    %c.schedule(600, "spin");          // never cancelled
    %this.coin = %c;
}
function MyGame::teardown(%this)
{
    %this.coin.delete();               // and you must remember every such line
}
```

**After** — a `Coin` class owns itself; teardown is a cascade:

```cpp
// coin.cs
function Coin::onAdd(%this)
{
    %this.setSize("2 2");
    %this.setImage("MyGame:coin");
    %this.createCircleCollisionShape(1);
    %this.spinEvent = %this.schedule(600, "spin");
}
function Coin::onRemove(%this)
{
    if (isEventPending(%this.spinEvent)) cancel(%this.spinEvent);
}

// level.cs — spawner sets only what it controls; deleting the scene frees the coin.
function Level::spawnCoin(%this, %pos)
{
    %this.scene.add(new Sprite() { class = "Coin"; Position = %pos; });
}
```

---

## Checklist for a new or edited `.cs` file

- [ ] Every `function X::y` in the file shares the same class `X` (or its base) — no god-namespace.
- [ ] The file is named after its class (prefix/suffix may be dropped).
- [ ] Managers/systems are `ScriptObject` subclasses, not functions on the module namespace.
- [ ] The class configures itself in `onAdd`; the spawner sets only class + the values it controls.
- [ ] `onRemove` deletes every object the class created and cancels every schedule it started.
- [ ] Owned objects are stored on `%this`, and cross-boundary deletes are `isObject()`-guarded.
- [ ] Shared setup across a `class`/`superclass` hierarchy goes through `init()` (only the
      most-derived `onAdd` fires).
- [ ] The only new globals are `ActionMap` bind targets or genuine game-state singletons.
