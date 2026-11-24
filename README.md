# Game Master Console Development Project

This Unity project hosts Game Master Console for development, testing, and documentation examples. The console source lives in its own Git submodule at `Packages/com.kekchpek.gamemaster-console`.

## Open the project

Clone the project and its submodule together.

```sh
git clone --recurse-submodules https://github.com/kekchpek/GameMasterConsoleEnv.git
```

If you already cloned the project, run `git submodule update --init --recursive` from its root before opening it. Add the project folder in Unity Hub and open it with Unity **6000.2.14f1**. Unity loads the console directly from the submodule as an embedded package.

## Play Rift Arena

Open `Assets/ArenaDemo/RiftArena.unity` and press **Play**. Move with **WASD** or the arrow keys and click or hold the **left mouse button** to swing toward the cursor. Enemies spawn on marked pads and stay there. Defeating them earns XP, and each level increases your damage so later enemies take fewer hits.

In Play Mode, open **Window → Game Master → Launch GUI** for the editor console or **Launch Terminal** for the Python client. Open the GUI's **Help** tab or enter `help` to list all available commands.

- `GrantXp 50` adds 50 XP and applies normal level progression.
- `SetLevel 5` sets the player's level to 5 and resets XP progress within that level. Valid levels are 1–1000.
- `SpawnEnemy` spawns one enemy. Pass a count, as in `SpawnEnemy 30`, to spawn more. Enemies fill available marked pads first, then appear elsewhere in the arena.
- `SpawnCenterEnemies` spawns ten enemies near the map center when there is room for all ten. It takes no arguments.
- `TeleportToCenter` moves the player and camera to the center of the arena. It takes no arguments.

## Develop the console

1. Create a branch in the submodule with `git -C Packages/com.kekchpek.gamemaster-console switch -c my-console-change`.
2. Edit the console code in the submodule and test it in this Unity project. The [console README](https://github.com/kekchpek/GameMasterConsole#readme) describes how to start the server and connect the client.
3. Commit the console changes from the submodule, then stage the submodule path and commit the updated reference in this repository. When publishing both commits, push the console commit first so other checkouts can fetch it.
