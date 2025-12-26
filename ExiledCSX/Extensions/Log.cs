using System;

namespace ExiledCSX.Extensions
{
    public static class Log
    {
        /*
        public static void Info(string message)
        {
            ServerConsole.AddLog($"[ExiledCSX] {message}", ConsoleColor.White);
        }

        public static void Warn(string message)
        {
            ServerConsole.AddLog($"[ExiledCSX] {message}", ConsoleColor.Yellow);
        }
        
        public static void Error(string message)
        {
            ServerConsole.AddLog($"[ExiledCSX] {message}", ConsoleColor.DarkRed);
        }
        
        public static void Debug(string message)
        {
            ServerConsole.AddLog($"[ExiledCSX] {message}", ConsoleColor.DarkGray);
        }
        */
        public static void ErrorScript(string message)
        {
            ServerConsole.AddLog($"[ExiledCSX - Script Error] {message}", ConsoleColor.DarkRed);
        }
    }
}