using System;
using System.IO;
using System.Linq;
using CommandSystem;
using Exiled.API.Features;
using Exiled.Permissions.Extensions;
using ExiledCSX.Core;

namespace ExiledCSX.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    public class CSXParent : ParentCommand
    {
        public CSXParent() => LoadGeneratedCommands();

        public override string Command => "csx";
        public override string[] Aliases { get; } = { "exiledcsx" };
        public override string Description => "C# Script Management (ExiledCSX)";

        public override void LoadGeneratedCommands()
        {
            RegisterCommand(new Reload());
            RegisterCommand(new Load());
            RegisterCommand(new Unload());
            RegisterCommand(new ListCmd());
            RegisterCommand(new Verify());
            RegisterCommand(new Help());
            RegisterCommand(new Run());
            RegisterCommand(new Call());
        }

        protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            response = "Usage: csx [reload | load | unload | list | verify | help]";
            return false;
        }

        private class Reload : ICommand
        {
            public string Command => "reload";
            public string[] Aliases => Array.Empty<string>();
            public string Description => "Reloads all scripts.";
            public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                if (!sender.CheckPermission("csx.manage")) { response = "Permission denied!"; return false; }
                Plugin.Instance.Evaluator.Clear();
                int count = Directory.GetFiles(Plugin.Instance.ScriptsPath, "*.csx")
                    .Count(file => Plugin.Instance.Evaluator.ProcessFile(file));
                response = $"Scripts reloaded. Successfully loaded: {count}";
                return true;
            }
        }

        private class Load : ICommand
        {
            public string Command => "load";
            public string[] Aliases => Array.Empty<string>();
            public string Description => "Loads a specific .csx file.";
            public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                if (!sender.CheckPermission("csx.manage")) { response = "Permission denied!"; return false; }
                string name = string.Join(" ", arguments);
                string path = Path.Combine(Plugin.Instance.ScriptsPath, name.EndsWith(".csx") ? name : name + ".csx");

                if (!File.Exists(path)) { response = "File does not exist!"; return false; }
                
                if (Plugin.Instance.Evaluator.ProcessFile(path))
                {
                    response = $"Script {name} has been loaded.";
                    return true;
                }
                response = "Error during loading. Check console.";
                return false;
            }
        }

        private class Unload : ICommand
        {
            public string Command => "unload";
            public string[] Aliases => Array.Empty<string>();
            public string Description => "Unloads a specific script.";
            public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                if (!sender.CheckPermission("csx.manage")) { response = "Permission denied!"; return false; }
                string name = string.Join(" ", arguments).Replace(".csx", "");
                if (Plugin.Instance.Evaluator.Unload(name))
                {
                    response = $"Script {name} has been unloaded.";
                    return true;
                }
                response = "Script not found.";
                return false;
            }
        }

        private class ListCmd : ICommand
        {
            public string Command => "list";
            public string[] Aliases => Array.Empty<string>();
            public string Description => "Lists loaded scripts.";
            public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                var loaded = Plugin.Instance.Evaluator.GetLoadedScripts();
                response = "\n<color=green>--- Loaded Scripts ---</color>\n";
                if (loaded.Count == 0) response += "No active scripts.\n";
                else
                {
                    foreach (var script in loaded)
                        response += $"• <color=yellow>{script.Key}</color>\n";
                }
                return true;
            }
        }

        private class Verify : ICommand
        {
            public string Command => "verify";
            public string[] Aliases => Array.Empty<string>();
            public string Description => "Verifies .csx syntax.";
            public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                string name = string.Join(" ", arguments);
                string path = Path.Combine(Plugin.Instance.ScriptsPath, name.EndsWith(".csx") ? name : name + ".csx");
                if (!File.Exists(path)) { response = "File does not exist!"; return false; }

                if (Plugin.Instance.Evaluator.VerifyFile(path, out string err))
                {
                    response = $"Script {name} is syntactically correct.";
                    return true;
                }
                response = $"Script error: {err}";
                return false;
            }
        }
        
        private class Run : ICommand
        {
            public string Command => "run";
            public string[] Aliases => Array.Empty<string>();
            public string Description => "Executes C# code directly.";
            public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                if (!sender.CheckPermission("csx.manage")) { response = "No permission"; return false; }
                Plugin.Instance.Evaluator.ExecuteDirect(string.Join(" ", arguments)); 
                response = "Code executed.";
                return true;
            }
        }
        
        private class Call : ICommand
        {
            public string Command => "call";
            public string[] Aliases => Array.Empty<string>();
            public string Description => "Calls script command.";
            public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                if (arguments.Count < 1) { response = "Usage: csx call <cmd_name>"; return false; }
                string cmdName = arguments.At(0);
                if (ScriptAPI.Commands.TryGetValue(cmdName, out var action))
                {
                    action.Invoke(Player.Get(sender), arguments.Skip(1).ToArray());
                    response = $"Executed '{cmdName}'.";
                    return true;
                }
                response = $"Command '{cmdName}' not found.";
                return false;
            }
        }
        
        private class Help : ICommand
        {
            public string Command => "help";
            public string[] Aliases => new[] { "?" };
            public string Description => "Help for ExiledCSX.";
            public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                response = "\n--- ExiledCSX ---\ncsx list | load | unload | reload | verify | run | call";
                return true;
            }
        }
    }
}