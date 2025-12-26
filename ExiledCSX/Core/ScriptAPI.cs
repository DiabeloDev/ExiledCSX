using System;
using System.Collections.Generic;
using System.IO;
using Exiled.API.Features;
using Newtonsoft.Json;

namespace ExiledCSX.Core
{
    public static class ScriptAPI
    {
        public static Dictionary<string, object> Data = new Dictionary<string, object>();
        
        public static Dictionary<string, Action<Player, string[]>> Commands = 
            new Dictionary<string, Action<Player, string[]>>();
        
        private static string DataFolder => Path.Combine(Plugin.Instance.ScriptsPath, "Data");
        
        public static void SaveData(string key, object value)
        {
            try
            {
                if (!Directory.Exists(DataFolder)) Directory.CreateDirectory(DataFolder);
                string path = Path.Combine(DataFolder, $"{key}.json");
                File.WriteAllText(path, JsonConvert.SerializeObject(value, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Log.Error($"[ScriptAPI Save] Failed to save '{key}': {ex.Message}");
            }
        }
        
        public static T LoadData<T>(string key)
        {
            try
            {
                string path = Path.Combine(DataFolder, $"{key}.json");
                if (!File.Exists(path)) return default;
                return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                Log.Error($"[ScriptAPI Load] Failed to load '{key}': {ex.Message}");
                return default;
            }
        }
    }
}