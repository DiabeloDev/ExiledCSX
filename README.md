# ExiledCSX

**ExiledCSX** is a powerful scripting engine for the [EXILED](https://github.com/ExMod-Team/EXILED) framework in SCP: Secret Laboratory. It allows server owners to load, unload, and reload C# scripts (.csx) at runtime without restarting the server or recompiling the entire plugin.

## Features
- **Hot-Reloading:** Automatically reloads scripts when the file is saved.
- **Full Event Support:** Automatically binds methods to EXILED events based on argument types.
- **Metadata Support:** Define script name, author, and version within a comment block.
- **Lifecycle Methods:** Optional `OnEnabled` and `OnDisabled` methods for custom initialization.

## Installation
1. Install [EXILED](https://github.com/ExMod-Team/EXILED) on your server.
2. Download `ExiledCSX.dll` from the releases page and put it in your `EXILED/Plugins` folder.
3. Restart the server to generate the necessary folders.
4. Place your `.csx` scripts in `EXILED/Plugins/ExiledCSX/Scripts`.

## Script Example
The engine automatically detects the `Joined` event based on the `JoinedEventArgs`. You can also use `ScriptAPI` to register custom sub-commands.

```csharp
/*
Name: Welcome Script
Author: .Diabelo
Description: Sends a message when a player joins and registers a command.
Version: 1.0.0
*/

using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;

// Called when the script is loaded/enabled
public void OnEnabled() 
{
    Log.Info("Welcome Script has been loaded!");

    // Register a custom command accessible via: csx call hello
    ScriptAPI.Commands["hello"] = (player, args) => {
        player.Broadcast(5, "Hello from the script world!");
    };
}

// Called when the script is unloaded/disabled
public void OnDisabled()
{
    Log.Warn("Welcome Script unloaded.");
}

// Automatic Event Binding
public void OnVerified(VerifiedEventArgs ev)
{
    ev.Player.Broadcast(5, $"Welcome to the server, {ev.Player.Nickname}!");
}
```

## Commands
| Command | Description | Permission |
| :--- | :--- | :--- |
| `csx list` | Lists all loaded scripts and available files. | `csx.manage` |
| `csx load <name>` | Loads a specific .csx file. | `csx.manage` |
| `csx unload <name>` | Unloads a specific script. | `csx.manage` |
| `csx reload` | Reloads all scripts from the folder. | `csx.manage` |
| `csx verify <name>` | Checks script syntax without loading it. | `csx.manage` |
| `csx run <code>` | Executes a C# code line directly from the console. | `csx.manage` |
| `csx call <cmd> [args]` | Calls a custom command registered by a script. | None* |

*\*Access to `csx call` depends on the logic defined inside the script.*

## License
This project is licensed under the Apache License 2.0 - see the [LICENSE](LICENSE) file for details.
