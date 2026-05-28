using System;
using System.IO;


namespace CefClient.Common
{
    internal static class CefCachePaths
    {
        public static string RootCachePath { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "User Data", "1");
        public static string GlobalCachePath { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "User Data", "Global");
    }
}
