# Game Master Console Development Project

This Unity project hosts Game Master Console for development, testing, and documentation examples. The console source lives in its own Git submodule at `Packages/com.kekchpek.gamemaster-console`.

## Open the project

Clone the project and its submodule together.

```sh
git clone --recurse-submodules https://github.com/kekchpek/GameMasterConsoleEnv.git
```

If you already cloned the project, run `git submodule update --init --recursive` from its root before opening it. Add the project folder in Unity Hub and open it with Unity **6000.2.14f1**. Unity loads the console directly from the submodule as an embedded package.

## Develop the console

1. Create a branch in the submodule with `git -C Packages/com.kekchpek.gamemaster-console switch -c my-console-change`.
2. Edit the console code in the submodule and test it in this Unity project. The [console README](https://github.com/kekchpek/GameMasterConsole#readme) describes how to start the server and connect the client.
3. Commit and push the console changes from the submodule. To share that revision with other environment users, stage the submodule path and commit the updated reference in this repository after the console commit is on its remote.

On first import, Unity may update `README.md.meta`, `Runtime.meta`, and `package.json.meta` in the console submodule. These metadata changes belong to the console repository.
