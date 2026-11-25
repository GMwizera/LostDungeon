# Chronicles of the Lost Dungeon

A 3D dungeon game made in Unity 6 for my Advanced C# summative. You fight through 5 rooms, beat 3 kinds of skeletons, open chests, find keys and escape. The real weather in Kigali changes how each game plays.

| | Link |
|---|---|
| WebGL (Unity Play) | _add link_ |
| Windows | _add link_ |
| Android (APK) | _add link_ |
| Walkthrough video | _add link_ |
| Q&A video | _add link_ |

## How to run it

1. Open the `LostDungeon` folder in Unity **6000.6.7f1**.
2. Click **Tools > Lost Dungeon > Build Game**. This makes the levels, scenes, effects and Addressables groups.
3. Open `Assets/_Project/Scenes/MainMenu.unity` and press Play.

Tests are in **Window > General > Test Runner**.

## Controls

| | Keyboard and mouse | Touch |
|---|---|---|
| Move | WASD | Joystick |
| Strike | Left click | STRIKE |
| Dash | Space | DASH |
| Fireball (after room 1) | Q or right click | FIRE |
| Open chests and doors | E | USE |
| Pause | Esc | Pause button |

A gamepad works too.

## What's in the game

- **5 rooms**, each with its own look, enemies, goal and reward. You unlock the next room by finishing the one before. Progress is saved.
- **Level select** shows each room's state with icons only: a lock, a play arrow or a check.
- **The hero** can move, strike (with critical hits), dash, throw fireballs, open things, get hurt and die.
- **3 enemies:**
  - The **Warrior** runs at you and hits up close.
  - The **Archer** keeps its distance and shoots. If you corner it, it shoots point blank.
  - The **Guardian** is big and armoured. It guards one spot and goes back to it.
- **Things to use:** chests, a locked exit door, keys, gold, potions, breakable barrels and spike traps.
- **Screens:** main menu, level select, settings, HUD, pause, level complete and game over.
- **Sound:** music, combat, enemy, menu and level complete sounds.

The first UI ideas came from Google Stitch (`Docs/stitch`). The final style is darker and warmer to suit the 3D rooms.

## How the code is organised

```
Assets/_Project
  Scripts/Core   plain C# rules (health, damage, saving, A*, pooling...). No Unity.
  Scripts/Game   the Unity side: player, enemies, levels, UI, audio
  Editor         tools that build the levels, scenes, effects and the game
  Tests          unit tests (Core) and play tests (the real game)
```

Core does not know about Unity or the Game code. That makes the rules easy to test, and it is why Core did not change at all when I moved the game from 2D to 3D.

Systems talk through events in `GameEvents`. When an enemy dies, it just says so. The objective, the save, the sound and the HUD each react on their own, and the enemy knows none of them.

**Adding things:**
- **New enemy:** make an `EnemyData` asset and pick a behaviour. A new way of fighting is one new `EnemyBehaviour` class. Nothing else changes.
- **New ability:** one new `AbilityDefinition` class, or a new asset of an old one with different numbers.
- **New level:** add a layout to `LevelBuilder`, mark it Addressable and add it to the catalog.

## Design patterns

| Pattern | Where | Why |
|---|---|---|
| Singleton | `GameManager`, `SaveManager`, `AudioManager`, `PoolManager`, `WeatherService` | One of each, kept between scenes |
| Observer | `GameEvents`, `CombatFeedback` | UI, sound and saving react without direct links |
| Strategy | `IAbility` (abilities and enemy attacks), `IEnemyBehaviour` | Swap behaviours as assets |
| State | `StateMachine<T>` for enemies (Idle, Chase, Attack, Retreat, ReturnHome, Dead), `GameState` | Each state is its own small class, not a big if/else |
| Object Pool | `ObjectPool<T>`, `PoolManager` | Fireballs, arrows, enemies and effects are reused |
| Template Method (extra) | `AbilityDefinition.Activate` | Every ability shows its effect, then does its own thing |

## Interfaces

`IDamageable` (anything that can be hurt), `IAbility`, `IEnemyBehaviour`, `IState<T>`, `IInteractable` (chests, doors), `ICollectable` (pickups), `IPoolable`, `IDashable`, `INavigationGrid` (so A* doesn't care how a level is built) and `ISaveStorage` (file on PC and phone, browser storage on WebGL).

## Addressables

I used **Unity Addressables** to load each level only when it is needed.

- **What it manages:** the 5 level prefabs (`Level1` to `Level5`), plus everything they use: enemy prefabs, 3D models, animations, enemy data and the arrow. Each level is its own bundle ("Pack Separately"), so only the room you play is in memory.
- **Loading:** `LevelManager` calls `Addressables.InstantiateAsync("LevelN")` and a loading bar fills while it loads.
- **Releasing:** when you leave the room, `LevelManager` calls `Addressables.Release`. It clears the pooled enemies and arrows first, because they came from that bundle.
- **If loading fails:** the loading screen shows a message and a Back to Menu button. The game never freezes.
- **Building the content:** use **Tools > Lost Dungeon > Build > Windows, WebGL or Android**. Each one rebuilds the Addressables content for that platform first, then builds the game. The content ships inside the build.

A play test checks that all 5 levels load.

## Weather API

`WeatherService` asks [Open-Meteo](https://open-meteo.com) for the weather in Kigali right now. It is free, needs no key and works in the browser.

| Weather in Kigali | In the dungeon |
|---|---|
| Rain | Enemies 15% slower |
| Storm | Enemies 30% slower, darker |
| 28 °C or more | Enemies 15% faster |
| Night | Darker, enemies see you later |
| Clear day | Normal |

If there is no internet or it takes over 6 seconds, the game plays normally and shows OFFLINE.

## Algorithms

**A\* pathfinding** (`Core/Navigation/AStarPathfinder.cs`)
- *What for:* enemies walk around pillars and crates to reach you.
- *How:* A* on the floor grid, moving up, down, left and right, with Manhattan distance as the guess.
- *Why:* it finds the shortest path but checks far fewer cells than a plain search.

**Min-heap** (`Core/Navigation/MinHeap.cs`)
- *What for:* gives A* the best cell to try next.
- *How:* my own binary heap in an array.
- *Why:* much faster than searching a list for the lowest cost every step.

**Nearest thing to use** (`Game/Interaction/PlayerInteractor.cs`)
- *What for:* picks which chest or door E opens when a few are close.
- *How:* one pass over what is nearby, keeping the closest.
- *Why:* there are only a few things, so one simple pass is enough.

**Inventory sort** (`Core/Inventory/Inventory.cs`)
- *What for:* keeps items in the same order (biggest stack first).
- *How:* insertion sort.
- *Why:* there are very few item types, and insertion sort is simple and stable.

**Damage** (`Core/Combat/DamageCalculator.cs`) works out critical hits and armour. The random number is passed in, so tests can force a critical hit.

## Saving

`SaveManager` saves one JSON file with: finished levels, stats, inventory, settings and unlocked rewards (Fireball, extra hearts). I picked JSON because it is built into Unity, easy to read and small. A broken save never crashes the game. It just starts fresh.

## Platforms

The game runs on Windows, WebGL and Android. Conditional compilation (`#if`) handles the differences:

| Code | Why |
|---|---|
| `#if UNITY_WEBGL` save in browser storage and hide Exit | A browser can't write files or close its own tab |
| `#if UNITY_ANDROID \|\| UNITY_IOS` 60 fps, screen stays on, lighter graphics | Phones get hot and run out of battery |
| `#if UNITY_ANDROID \|\| UNITY_IOS` vibrate when hurt | Only phones can vibrate |
| `#elif UNITY_STANDALONE` vsync on | Smooth on PC monitors |

The lighter phone graphics use their own render settings: no anti-aliasing, fewer lights, cheaper shadows and 85% resolution. Keyboard, gamepad and touch all go through `PlayerInputReader`. The UI scales to any screen.

**Extras:** dynamic torch lighting, particle effects, post-processing, and each level loading only when needed.

## Tests

- **74 unit tests** (NUnit) for damage, health, inventory, level unlocking, saving (including broken saves), the state machine, A*, the min-heap, the object pool, cooldowns, events and the weather rules.
- **11 play tests** that run the real game: they play room 1 to the end, load every level, check the walls hold, and check every enemy chases and hits you. One was added after a bug where a room's enemies never noticed the player.

## Credits

- 3D art and animations: KayKit by [Kay Lousberg](https://kaylousberg.itch.io) (CC0).
- Icons, effects, sounds and music: made by the project's editor scripts.
