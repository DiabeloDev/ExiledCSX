using System;
using System.Collections.Generic;
using System.IO;
using Exiled.API.Features;
using ExiledCSX.Core;

namespace ExiledCSX
{
    public class Plugin : Plugin<Config>
    {
        public override string Name => "ExiledCSX";
        public override string Author => ".Diabelo";
        public override Version Version => new Version(1, 0, 0);
        public static Plugin Instance { get; private set; }
        public string ScriptsPath => Path.Combine(Paths.Plugins, "ExiledCSX/Scripts");
        private readonly ScriptEvaluator _loader = new ScriptEvaluator();
        public ScriptEvaluator Evaluator => _loader;
        private FileSystemWatcher _watcher;
        private readonly Dictionary<string, DateTime> _lastWriteTimes = new Dictionary<string, DateTime>();

        public override void OnEnabled()
        {
            Instance = this;
            if (!Directory.Exists(ScriptsPath)) Directory.CreateDirectory(ScriptsPath);
            
            LoadAll();
            if (Config.HotReload)
            {
                SetupWatcher();
            }
            
            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
            }
            
            _loader.Clear();
            Instance = null; 
            
            base.OnDisabled();
        }

        private void LoadAll()
        {
            _loader.Clear();
            if (!Directory.Exists(ScriptsPath)) return;

            foreach (var file in Directory.GetFiles(ScriptsPath, "*.csx"))
            {
                _loader.ProcessFile(file);
            }
        }

        private void SetupWatcher()
        {
            _watcher = new FileSystemWatcher(ScriptsPath, "*.csx")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
                EnableRaisingEvents = true
            };
            
            _watcher.Changed += (s, e) => OnFileChanged(e.FullPath);
            _watcher.Created += (s, e) => OnFileChanged(e.FullPath);
            _watcher.Deleted += (s, e) => OnFileDeleted(e.FullPath);
            _watcher.Renamed += (s, e) => 
            {
                OnFileDeleted(e.OldFullPath);
                OnFileChanged(e.FullPath);
            };
        }

        private void OnFileChanged(string path)
        {
            try
            {
                DateTime lastWrite = File.GetLastWriteTime(path);
                if (_lastWriteTimes.TryGetValue(path, out DateTime last) && (lastWrite - last).TotalMilliseconds < 500)
                    return;

                _lastWriteTimes[path] = lastWrite;
                
                MEC.Timing.CallDelayed(0.5f, () => 
                {
                    Log.Info($"Detected file change: {Path.GetFileName(path)}. Reloading...");
                    _loader.ProcessFile(path);
                });
            }
            catch (Exception ex)
            {
                Log.Error($"Error during Watcher event: {ex.Message}");
            }
        }

        private void OnFileDeleted(string path)
        {
            string scriptName = Path.GetFileNameWithoutExtension(path);
            if (string.IsNullOrEmpty(scriptName)) return;
            
            if (_loader.Unload(scriptName))
            {
                Log.Info($"Script {scriptName} unloaded.");
            }
            
            if (_lastWriteTimes.ContainsKey(path))
                _lastWriteTimes.Remove(path);
        }
    }
}