using MainClient.Common;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MainClient
{
    static class Program
    {
        private static IServiceProvider ServiceProvider { get; set; }
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            ConfigureServices();
            Application.Run((MainForm)ServiceProvider.GetService(typeof(MainForm)));
        }

        private static void ConfigureServices()
        {
            var services = new ServiceCollection();
            services.AddHttpClient();
            services.AddHttpClient("hailiangip", c =>
            {
                c.BaseAddress = new Uri("http://111.73.45.100:7808");
                c.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
                c.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.36");
            });
            services.AddSingleton<MainForm>();
            ServiceProvider = services.BuildServiceProvider();
        }
    }
}
