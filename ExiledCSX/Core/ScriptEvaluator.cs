using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Exiled.API.Features;
using Exiled.Events.Features;
using Exiled.Loader;
using ExiledCSX.Models;
using ExiledCSX.Logging;
using Mono.CSharp;
using MEC;
using Delegate = System.Delegate;

namespace ExiledCSX.Core
{
    public class ScriptEvaluator
    {
        private readonly Dictionary<string, object> _activeInstances = new Dictionary<string, object>();
        private readonly Dictionary<string, List<EventAssignment>> _scriptSubscriptions = new Dictionary<string, List<EventAssignment>>();
        private readonly Dictionary<string, ScriptMetadata> _scriptMetadata = new Dictionary<string, ScriptMetadata>();
        
        private ScriptMetadata ExtractMetadata(string content)
        {
            var meta = new ScriptMetadata();
            var match = Regex.Match(content, @"/\*(.*?)\*/", RegexOptions.Singleline);
            
            if (match.Success)
            {
                string comment = match.Groups[1].Value;
                meta.Name = ParseValue(comment, "Name") ?? meta.Name;
                meta.Author = ParseValue(comment, "Author") ?? meta.Author;
                meta.Description = ParseValue(comment, "Description") ?? meta.Description;
                meta.Version = ParseValue(comment, "Version") ?? meta.Version;
            }
            return meta;
        }

        private string ParseValue(string comment, string key)
        {
            var match = Regex.Match(comment, $@"{key}:\s*(.*)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }

        private Evaluator CreateScopedEvaluator()
        {
            var settings = new CompilerSettings { StdLib = false };
            var evaluator = new Evaluator(new CompilerContext(settings, new ExiledCsxPrinter()));
            
            var uniqueAssemblies = new Dictionary<string, Assembly>();

            void AddAssembly(Assembly asm) {
                if (asm == null) return;
                string name = asm.GetName().Name;
                if (!uniqueAssemblies.ContainsKey(name)) uniqueAssemblies[name] = asm;
            }

            try
            {
                AddAssembly(typeof(object).Assembly);
                AddAssembly(typeof(Uri).Assembly);
                AddAssembly(typeof(Enumerable).Assembly);
                AddAssembly(typeof(System.Net.Http.HttpClient).Assembly);
                
                AddAssembly(typeof(ReferenceHub).Assembly); 
                AddAssembly(typeof(Player).Assembly);
                AddAssembly(typeof(Exiled.Events.Handlers.Player).Assembly);
                AddAssembly(typeof(Timing).Assembly);
                
                AddAssembly(typeof(UnityEngine.GameObject).Assembly);
                AddAssembly(typeof(UnityEngine.Vector3).Assembly);
                AddAssembly(typeof(UnityEngine.Physics).Assembly); 
                
                if (Loader.Plugins != null)
                {
                    foreach (var plugin in Loader.Plugins)
                        AddAssembly(plugin.Assembly);
                }

                if (Loader.Dependencies != null)
                {
                    foreach (var dep in Loader.Dependencies)
                        AddAssembly(dep);
                }
            }
            catch (Exception ex) { Log.Error($"Reference error: {ex.Message}"); }
            
            foreach (var asm in uniqueAssemblies.Values)
            {
                try { evaluator.ReferenceAssembly(asm); } catch { }
            }
    
            return evaluator;
        }

        private (List<string> usings, string body) ParseScript(string path)
        {
            string[] lines = File.ReadAllLines(path);
            var usings = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "using System;",
                "using System.Collections;",
                "using System.Collections.Generic;",
                "using System.Linq;",
                "using Exiled.API.Features;",
                "using Exiled.Events.Handlers;",
                "using Exiled.Events.EventArgs.Player;",
                "using Exiled.Events.EventArgs.Server;",
                "using Exiled.Events.EventArgs.Map;",
                "using Exiled.Events.EventArgs.Item;",
                "using Exiled.Events.EventArgs.Warhead;",
                "using MEC;",               
                "using ExiledCSX.Core;"     
            };

            List<string> body = new List<string>();
            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("using ") && trimmed.EndsWith(";")) 
                {
                    usings.Add(trimmed);
                }
                else 
                {
                    body.Add(line);
                }
            }
            return (usings.ToList(), string.Join("\n", body));
        }
        
        public void ExecuteDirect(string code)
        {
            try
            {
                var evaluator = CreateScopedEvaluator();
                if (!evaluator.Run("using System; using System.Collections.Generic; using System.Linq; using Exiled.API.Features; using UnityEngine; using MEC;")) return;
                evaluator.Run(code);
            }
            catch (Exception ex)
            {
                string msg = ex.InnerException?.Message ?? ex.Message;
                Log.Error($"[csx run] Runtime error: {msg}");
            }
        }
        
        public bool ProcessFile(string path)
        {
            string fileName = Path.GetFileNameWithoutExtension(path);
            if (_activeInstances.ContainsKey(fileName)) Unload(fileName);

            try
            {
                string content = File.ReadAllText(path);
                ScriptMetadata meta = ExtractMetadata(content);

                var (usings, body) = ParseScript(path);
                var evaluator = CreateScopedEvaluator();
                
                string safeName = fileName.Replace(" ", "_").Replace("-", "_");
                string className = $"Class_{safeName}_{Guid.NewGuid():n}";

                string fullCode = string.Join("\n", usings) + $"\npublic class {className} \n{{ \n{body}\n}}";

                if (evaluator.Run(fullCode))
                {
                    Type type = (Type)evaluator.Evaluate($"typeof({className})");
                    if (type != null)
                    {
                        object instance = Activator.CreateInstance(type);
                        _activeInstances[fileName] = instance;
                        _scriptMetadata[fileName] = meta;
                        
                        Bind(type, instance, fileName);
                        
                        var onEnabled = type.GetMethod("OnEnabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        onEnabled?.Invoke(instance, null);
                        
                        Log.Info($"Loaded script: {meta.Name} (v{meta.Version}) by {meta.Author}");
                        return true;
                    }
                }
            }
            catch (Exception ex) { Log.Error($"Error in {fileName}: {ex.Message}"); }
            return false;
        }

        public bool VerifyFile(string path, out string errorMessage)
        {
            errorMessage = string.Empty;
            try
            {
                var (usings, body) = ParseScript(path);
                var evaluator = CreateScopedEvaluator();
                string code = string.Join("\n", usings) + $"\npublic class V_{Guid.NewGuid():n} {{ \n{body}\n}}";
                return evaluator.Run(code);
            }
            catch (Exception ex) { errorMessage = ex.Message; return false; }
        }

        private void Bind(Type type, object instance, string scriptName)
        {
            if (!_scriptSubscriptions.ContainsKey(scriptName)) _scriptSubscriptions[scriptName] = new List<EventAssignment>();

            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                var parameters = method.GetParameters();
                if (parameters.Length != 1) continue;
                Type argType = parameters[0].ParameterType;
                if (EventMapper.Subscribe.TryGetValue(argType, out var binder))
                {
                    var handlerType = typeof(CustomEventHandler<>).MakeGenericType(argType);
                    Delegate del = Delegate.CreateDelegate(handlerType, instance, method);
                    binder(del);
                    _scriptSubscriptions[scriptName].Add(new EventAssignment(argType, del));
                }
            }
        }

        public bool Unload(string scriptName)
        {
            if (!_activeInstances.TryGetValue(scriptName, out object instance)) return false;
            
            var onDisabled = instance.GetType().GetMethod("OnDisabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            onDisabled?.Invoke(instance, null);

            if (_scriptSubscriptions.TryGetValue(scriptName, out var subs))
            {
                foreach (var sub in subs)
                {
                    if (EventMapper.Unsubscribe.TryGetValue(sub.ArgsType, out var unbinder))
                        try { unbinder(sub.Handler); } catch { }
                }
            }
            
            _scriptSubscriptions.Remove(scriptName);
            _activeInstances.Remove(scriptName);
            _scriptMetadata.Remove(scriptName);
            return true;
        }

        public void Clear() { foreach (var name in _activeInstances.Keys.ToList()) Unload(name); }
        public Dictionary<string, ScriptMetadata> GetLoadedScripts() => _scriptMetadata;
    }
}