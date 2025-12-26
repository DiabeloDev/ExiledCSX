using System.ComponentModel;
using Exiled.API.Interfaces;

namespace ExiledCSX
{
    public class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; } = false;
        [Description("Should the plugin automatically reload scripts when a file is saved?")]
        public bool HotReload { get; set; } = true;
    }
}