using CefClient.Common;
using CefSharp;
using CefSharp.OffScreen;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace CefClient
{
    public class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            var consumerId = args
            .FirstOrDefault(x => x.StartsWith("--consumer-id=", StringComparison.OrdinalIgnoreCase))
            ?.Substring("--consumer-id=".Length);

            if (!string.IsNullOrWhiteSpace(consumerId))
            {
                CefCachePaths.RootCachePath = System.IO.Path.Combine(new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).Parent.FullName, "User Data", consumerId);
                CefCachePaths.GlobalCachePath = System.IO.Path.Combine(new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).Parent.FullName, "Global");
            }

            CefSharpSettings.ShutdownOnExit = true;
            CefSharpSettings.SubprocessExitIfParentProcessClosed = true;
            Cef.EnableWaitForBrowsersToClose();
            var settings = new CefSettings()
            {
                Locale = "zh-CN",
                RootCachePath = CefCachePaths.RootCachePath,
                //UserDataPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "User Data"),
                //BrowserSubprocessPath= System.IO.Path.Combine(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "CefSharp.BrowserSubprocess.exe"),
                PersistSessionCookies = false,
                PersistUserPreferences = false,
                IgnoreCertificateErrors = true,
                LogSeverity = LogSeverity.Disable,
                WindowlessRenderingEnabled = true,
                UserAgent = "Mozilla/5.0 (Linux; Android 8.0.0; SM-G955U Build/R16NW) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/116.0.0.0 Mobile Safari/537.36"
            };
            // 启用媒体流
            settings.CefCommandLineArgs.Add("enable-media-stream", "1");
            // 限制 HTTP 磁盘缓存大小，单位是字节
            // 这里限制为 200MB
            //settings.CefCommandLineArgs.Add("disk-cache-size", (200 * 1024 * 1024).ToString());
            // 限制媒体缓存大小，单位是字节
            // 这里限制为 50MB
            //settings.CefCommandLineArgs.Add("media-cache-size", (50 * 1024 * 1024).ToString());
            //// 禁用应用缓存，减少写盘
            //settings.CefCommandLineArgs.Add("disable-application-cache", "1");
            //// 可选：减少 GPU 缓存相关写入
            //settings.CefCommandLineArgs.Add("disable-gpu-shader-disk-cache", "1");
            //Cef.EnableHighDPISupport();
            settings.DisableGpuAcceleration();
            settings.SetOffScreenRenderingBestPerformanceArgs();
            //settings.LogSeverity = LogSeverity.Disable;
            //settings.LogFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cef_debug.log");
            var success = Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);
            Application.ApplicationExit += (sender, e) =>
            {
                if (Cef.IsInitialized)
                {
                    Cef.WaitForBrowsersToClose();
                    Cef.Shutdown();
                }
            };
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
            return 0;
        }





    }
}
