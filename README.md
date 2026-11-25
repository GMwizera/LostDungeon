# Chronicles of the Lost Dungeon

A 3D dungeon crawler with a 45 degree camera, made in Unity 6 for the Advanced C# summative. Fight through five levels, defeat three kinds of enemies, open chests, find keys and escape. The real weather in Kigali changes how each run plays.

| Build | Link |
|---|---|
| WebGL (Unity Play) | _add link_ |
| Windows | _add link_ |
| Android (APK) | _add link_ |
| Video walkthrough | _add link_ |
| Q&A video | _add link_ |

## Controls

| Action | Keyboard / mouse | Gamepad | Touch |
|---|---|---|---|
| Move | WASD | Left stick | Left joystick |
| Sword | Left mouse | West button | Sword button |
| Dash | Space | South button | Dash button |
| Fireball (unlocked after level 1) | Q or right mouse | Right shoulder | Fireball button |
| Interact (chests, doors) | E | North button | Play button |
| Pause | Esc | Start | Pause button (top right) |

## Running the project

1. Open `LostDungeon` with **Unity 6000.6.7f1**.
2. Run **Tools > Lost Dungeon > Build Game** once. It generates the levels, scenes, icons, sounds and Addressables entries.
3. Open `Assets/_Project/Scenes/MainMenu.unity` and press Play.

Tests: **Window > General > Test Runner**. EditMode has 74 unit tests; PlayMode has 11 tests that run the real game.

## Features

- **5 levels**, each with its own layout, floor, enemies, objective and reward. Locked levels can't be opened; progress is saved and survives restarting the game.
- **Level select** shows each level's state with icons only: padlock (locked), green play arrow (available), gold check (completed).
- **Player**: 8-direction movement and animation, health with short invulnerability after a hit, sword (with critical hits), Dash and Fireball abilities, death and game over.
- **3 enemy types**:
  - **Warrior** chases the player and fights up close.
  - **Archer** keeps its distance, backs off when the player gets close, and shoots arrows.
  - **Guardian** has armour, defends one spot and walks back to it when the player leaves.
- **Interaction**: chests, a locked exit door, keys, gold, healing potions, breakable barrels and floor traps.
- **UI**: main menu, level select, settings (music, effects, touch controls, reset progress), HUD (hearts, ability cooldowns, inventory, objective, level name, dungeon conditions), pause, level complete and game over screens.
- **Audio**: music, combat, enemy, UI and level-complete sounds.

## UI design

The first UI concepts were made in **Google Stitch** (the early screens are in [`Docs/stitch`](Docs/stitch)). The final interface moved on from those flat black concepts to a warmer "temple" style that suits the 3D dungeon, built in Unity UI Toolkit (`UI/LostDungeon.uss`):

- Translucent dark panels with rounded gold frames, so the 3D scene shows behind the menus.
- One colour per meaning: **gold** for loot, rooms and the main action, **cyan** for weather and dash, **red** for health and enemies, **green** for the key, rewards and an open exit.
- Each screen shows only what the player needs at that moment.

Screens:
- **Main menu:** title, live Kigali weather in one line, the hero standing in a lit 3D temple, a compact menu (Continue, New Game, Level Select, Settings, Exit) and a hearts and gold strip.
- **Level select:** one card per room with a status icon (play, check or lock, never text), its enemies and its reward.
- **Gameplay HUD:** element orbs and a vitality bar, a pouch strip, a room counter with dots for the enemies left, the objective banner, the weather, round ability buttons with cooldown rings, enemy name plates and floating damage numbers. On touch screens, a ring joystick.
- **Level complete:** the room cleared, the reward earned, small spoil chips, the five-room track and the next-room button.
- **Pause and game over** screens in the same style. The gameplay HUD hides while any of these screens is open.

## 3D presentation

The game is fully 3D: rigged, animated KayKit characters in modular stone rooms, seen through a perspective camera at 45 degrees around and 40 degrees down.

- **Rendering:** URP Universal renderer with HDR, 4x MSAA, soft real-time shadows from a cool moonlight, flickering warm torches on the walls, linear fog, and a post-processing profile (bloom, ACES tone mapping, colour grading, vignette). Built by `Editor/RenderSetup.cs`.
- **Characters:** one shared skeleton rig, so one set of animations drives the hero (an elemental mage with a staff) and all three skeleton enemies. A generated Animator Controller per character blends idle, walk and run from speed and plays attack, cast, hit and death on triggers (`CharacterAnimator`).
- **Feel:** a gold slash arc on every strike, a cyan wind swirl on dash, a glowing fireball with a flame trail, sparks on hits (bigger on critical hits), floating damage numbers ("-4 CRIT!"), a name plate with a health bar over each enemy, glowing rings under the hero (cyan) and enemies (red), opening chest lids, rising spike traps, and an exit door that swings open with light pouring through. Effects are pooled (`PooledEffect`).
- **Gameplay stays 2D underneath:** the rules work on the floor plane (x, y), and `Ground` converts to the 3D world (x, 0, y). That is why the whole Core library and its tests did not change when the game moved to 3D.

## Architecture

```
Assets/_Project
  Scripts/Core   LostDungeon.Core   plain C#, no Unity references: rules and algorithms (fully unit tested)
  Scripts/Game   LostDungeon.Game   MonoBehaviours that connect the rules to Unity
  Editor         LostDungeon.Editor tools that generate levels, scenes, assets and builds
  Tests          EditMode (Core) and PlayMode (whole game) tests
  UI             UI Toolkit layouts (UXML) and styles (USS)
```

Core never references Game or Unity, so the game rules (health, damage, inventory, level unlocking, saving, pathfinding, state machine, object pool) can be tested and reused on their own. Game depends on Core, never the other way round.

Systems talk through **events** in `GameEvents` instead of holding references to each other. For example, when an enemy dies it raises `EnemyDefeated`; the level objective, save system, audio and HUD all react, and the enemy knows none of them.

### Adding content without changing systems

- **New enemy type**: create an `EnemyData` asset (stats, colour, attack ability) and choose a behaviour asset. A new way of fighting only needs one new `EnemyBehaviour` class. `EnemyController` and the states don't change.
- **New ability**: one new `AbilityDefinition` subclass, or a new asset of an existing one (a different projectile prefab and numbers makes a new spell).
- **New level**: add a layout in `LevelBuilder`, or build a prefab by hand, mark it Addressable and add a `LevelDefinition` to the catalog.

## Interfaces

| Interface | Used by | Why |
|---|---|---|
| `IDamageable` | Player, enemies, barrels | Swords, fireballs, arrows and traps hurt anything without knowing what it is |
| `IAbility` | Sword, Dash, Fireball, enemy attacks | Player and enemies trigger abilities the same way |
| `IEnemyBehaviour` | Warrior, Archer, Guardian brains | Swappable AI |
| `IState<T>` | Enemy states | State machine works with any state |
| `IInteractable` | Chest, exit door | Player uses the nearest one without knowing its type |
| `ICollectable` | Pickups | Walk-over items |
| `IPoolable` | Projectiles, enemies | Reset when reused from a pool |
| `IDashable` | Player movement | Dash works on anything that can dash |
| `INavigationGrid` | Level grid, test grids | Pathfinding doesn't depend on how a level is built |
| `ISaveStorage` | File storage, PlayerPrefs storage | Save logic doesn't depend on the platform |

## Design patterns

| Pattern | Where | Problem it solves |
|---|---|---|
| Singleton | `Singleton<T>`: `GameManager`, `SaveManager`, `AudioManager`, `PoolManager`, `WeatherService` | Exactly one of each global system, kept alive between scenes |
| Observer | `GameEvents` and `CombatFeedback` (C# events) | UI, audio, saving and objectives react to gameplay with no direct references |
| Strategy | `IAbility` (player abilities and enemy attacks), `IEnemyBehaviour` (enemy brains) | Interchangeable behaviour chosen as assets in the Inspector |
| State | `StateMachine<T>` with Idle, Chase, Attack, Retreat, ReturnHome and Dead states; `GameState` for the game flow | Each state's logic lives in its own class instead of a large if/else block |
| Object Pool | `ObjectPool<T>` + `PoolManager` | Fireballs, arrows, enemies and visual effects are reused instead of created and destroyed |
| Template Method (extra) | `AbilityDefinition.Activate` shows the ability's effect, then calls each ability's own `Perform` | Every ability gets its visual effect without repeating that code |

## Asset and content management: Addressables

**Approach:** Unity Addressables (2.11).

**What it manages:** the five level prefabs (`Assets/_Project/Levels/Level1` to `Level5`). Each level contains its 3D floor, walls, torches, props, chest, door, traps, pickups, navigation grid and enemy spawner. The enemy prefabs, models, animations, enemy data, attack abilities and arrow prefab they use come along as dependencies. The `Levels` group uses **Pack Separately**, so every level is its own bundle and only the level being played is loaded.

**Loading:** `LevelManager` calls `Addressables.InstantiateAsync("LevelN")` when the Game scene starts. While it loads, a progress bar fills (`LevelLoadProgress` event).

**Releasing:** when the level ends (next level, restart, main menu) `LevelManager` calls `Addressables.Release(handle)`, which destroys the instance and lets the bundle unload. Pooled enemies and arrows are cleared first, because they were created from prefabs inside that bundle and must not outlive it.

**Failure handling:** if a level can't be loaded (missing content, wrong platform build), the error is logged, the loading screen shows a message and a "Back to Menu" button, and the game stays responsive.

**Building and deploying the content:**
1. Tools > Lost Dungeon > Build Game creates the Addressables group and entries.
2. The build menu (**Tools > Lost Dungeon > Build > Windows / WebGL / Android**) switches nothing by itself: run it with that platform active. It cleans and rebuilds the Addressables content for the active platform (`AddressableAssetSettings.BuildPlayerContent`) and then builds the player. Content is local, so it ships inside the build (`StreamingAssets/aa/<Platform>`).
3. Manual alternative: Window > Asset Management > Addressables > Groups > Build > New Build > Default Build Script, then File > Build Profiles > Build.

The PlayMode test `EveryLevelAddress_LoadsThroughAddressables` checks that all five addresses load.

## REST API: Open-Meteo weather

`WeatherService` requests the current weather in Kigali from `https://api.open-meteo.com/v1/forecast` (free, no key, HTTPS and CORS enabled, so it also works in the browser build). `DungeonConditions` turns it into gameplay:

| Weather | Effect |
|---|---|
| Rain | Enemies 15% slower |
| Thunderstorm | Enemies 30% slower, darker |
| Heat (28 °C or more) | Enemies 15% faster |
| Night in Kigali | Dungeon darker, enemies notice you later |
| Clear day | Normal |

If the request fails or takes more than 6 seconds, the game uses normal conditions and the HUD says the weather is unavailable. Nothing waits on the network.

## Algorithms

### 1. A* pathfinding (`Core/Navigation/AStarPathfinder.cs`)
- **Purpose:** enemies walk around walls and obstacles to reach the player (and the Guardian walks back to its post).
- **Approach:** A* on the level's floor grid (one cell per floor tile), 4-directional moves of cost 1, Manhattan distance as the heuristic, my own binary min-heap (`MinHeap.cs`) as the open list. Paths are recalculated a few times per second and capped at 2000 searched cells so a bad request can't freeze the game.
- **Why:** with an admissible heuristic A* always finds a shortest path while searching far fewer cells than breadth-first search. 4-directional moves follow the tile edges, so enemies never cut through pillar or wall corners.

### 2. Binary min-heap (`Core/Navigation/MinHeap.cs`)
- **Purpose:** gives A* the most promising cell next.
- **Approach:** array-backed binary heap with sift-up and sift-down; ties are broken by insertion order so paths are the same every run.
- **Why:** O(log n) push and pop instead of scanning a list for the lowest cost every step.

### 3. Nearest interactable search (`Game/Interaction/PlayerInteractor.cs`)
- **Purpose:** decide which chest or door the Interact button uses when several are close.
- **Approach:** one linear pass over the colliders in range, keeping the closest usable `IInteractable`, comparing squared distances.
- **Why:** there are only a handful of candidates, so one O(n) pass is simplest and fastest; squared distance avoids a square root per item.

### 4. Inventory sorting (`Core/Inventory/Inventory.cs`)
- **Purpose:** show and save items in a stable order (largest stack first, then by name).
- **Approach:** insertion sort.
- **Why:** the inventory holds very few item types, where insertion sort is simple, stable and fast.

### 5. Damage calculation (`Core/Combat/DamageCalculator.cs`)
- **Purpose:** critical hits and the Guardian's armour.
- **Approach:** the random roll is passed in, so tests can force a critical; armour reduces damage but a hit always does at least 1.
- **Why:** keeps the rules in one tested place and fights fair.

## Save system (JSON)

`SaveManager` stores one `SaveData` object as JSON (`JsonUtility`): completed levels, stats (enemies defeated, gold, deaths, time played), inventory, settings (music, effects, touch controls) and unlocked rewards (Fireball, extra health). Stats are updated by listening to `GameEvents`.

JSON was chosen because it is readable (easy to check and debug), built into Unity, and the save is small. A corrupt or old save never crashes the game: `SaveService` falls back to a fresh save and fills in missing parts.

## Multi-platform and conditional compilation

`Game/Platform/PlatformFeatures.cs` and `SaveManager` use Unity conditional compilation:

| Code | Why |
|---|---|
| `#if UNITY_WEBGL && !UNITY_EDITOR` save to `PlayerPrefs` (browser IndexedDB), otherwise a file in `persistentDataPath` | Browser builds can't rely on normal file writes |
| `#if UNITY_WEBGL` hide the Exit button | A web page can't close its own tab |
| `#if UNITY_ANDROID \|\| UNITY_IOS` 60 fps cap, screen stays on, quality level 0 which uses a lighter mobile render pipeline (no MSAA, 4 lights per object, hard 1024 shadows over 16 m, 85% resolution), touch controls on by default | Battery, heat, frame rate and touch input on phones |
| `#if UNITY_ANDROID \|\| UNITY_IOS` `Handheld.Vibrate()` when hurt | Only phones have a vibration motor |
| `#elif UNITY_STANDALONE` vsync on | Smooth frame pacing on desktop monitors |

Input uses the Input System with keyboard/mouse, gamepad and on-screen touch controls feeding the same `PlayerInputReader` events. UI Toolkit scales with screen size (reference 1280x720).

## Tests

74 EditMode unit tests (NUnit) and 11 PlayMode tests. The PlayMode tests start the real game from the main menu, play level 1 to the end (save, unlock and debrief screen), load every level through Addressables, check with real physics that walls and room corners are solid, push the player into every wall while dashing to make sure it can't leave the room, and check each enemy type (including the ones a room spawns itself) chases the player and really attacks up close (written after a bug where a cornered archer never fired). The unit tests cover: damage calculation, health, inventory logic, level unlocking, save/load (including corrupt saves), state machine transitions, A* correctness, the min-heap, object pool behaviour, cooldowns, events, direction maths and weather rules.

## Credits

- 3D art and animations: KayKit Dungeon Pack, Adventurers, Skeletons and Character Animations by [Kay Lousberg](https://kaylousberg.itch.io) (CC0).
- Icons, visual effects, fireball, sound effects and music: generated by the project's editor scripts.
