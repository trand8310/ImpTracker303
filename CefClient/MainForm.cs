using CefClient.Common;
using CefClient.Common.CefClient.Common;
using CefClient.Handler;
using CefSharp;
using CefSharp.DevTools;
using CefSharp.DevTools.Emulation;
using CefSharp.DevTools.Network;
using CefSharp.Handler;
using CefSharp.OffScreen;
using CefSharp.Structs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace CefClient
{
    public partial class MainForm : Form
    {
        #region Win32
        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        private static extern int SendMessage(int hWnd, int msg, int wParam, ref COPYDATASTRUCT lParam);
        [DllImport("User32.dll", EntryPoint = "FindWindow")]
        private static extern int FindWindow(string lpClassName, string lpWindowName);
        const int WM_COPYDATA = 0x004A;
        const int WM_MYSYMPLE = 0x005A;
        #endregion

        private SynchronizationContext sync;
        private int hMainWnd = 0;
        private bool showform = true;
        private string uuid = string.Empty;
        private readonly ResourceCacheManager _sharedResourceCacheManager;

        #region  LogWrite
        void LogCallback(params object[] parameters)
        {

            var callee = new StackFrame(1, false).GetMethod();
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("Callback: ");
            sb.Append(callee.Name);
            sb.Append("(");
            var pm = callee.GetParameters();
            for (var i = 0; i <= pm.Length - 1; i++)
            {
                sb.Append(pm[i].Name);
                if (parameters.Length > i)
                {
                    sb.Append(" = {");
                    if (parameters[i] != null)
                    {
                        sb.Append(parameters[i].ToString());
                    }
                    else
                    {
                        sb.Append("null");
                    }
                    sb.Append("}");
                }
                if (i < pm.Length - 1)
                {
                    sb.Append(", ");
                }
            }
            sb.Append(")");
            LogWriteLine(sb.ToString());
        }


        public void LogWriteLine()
        {
            LogWrite(Environment.NewLine);
        }

        public void LogWriteLine(string msg)
        {
            LogWrite(msg + Environment.NewLine);
        }

        public void LogWriteLine(string msg, params object[] parameters)
        {
            LogWrite(msg + Environment.NewLine, parameters);
        }

        public void LogWrite(string msg, params object[] parameters)
        {
            LogWrite(string.Format(msg, parameters));
        }
        public void LogWrite(string msg)
        {
            if (InvokeRequired)
            {
                Invoke((MethodInvoker)(() => { LogWrite(msg); }));
                return;
            }
            LogTextBox.AppendText($"{System.DateTime.Now.ToString("[HH:mm:ss]")} {msg}");
            //LogTextBox.SelectionStart = LogTextBox.TextLength - 1;
            LogTextBox.ScrollToCaret();

        }
        #endregion


        private Task UiInvokeAsync(Action action, CancellationToken cancellationToken = default)
        {
            return UiInvokeAsync(() =>
            {
                action();
                return true;
            }, cancellationToken);
        }

        private Task<T> UiInvokeAsync<T>(Func<T> func, CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            var _owner = this;
            if (this.IsDisposed || this.Disposing)
            {
                tcs.TrySetException(new ObjectDisposedException(nameof(_owner)));
                return tcs.Task;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                tcs.TrySetCanceled(cancellationToken);
                return tcs.Task;
            }

            void Execute()
            {
                try
                {
                    if (_owner.IsDisposed || _owner.Disposing)
                    {
                        tcs.TrySetException(new ObjectDisposedException(nameof(_owner)));
                        return;
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        tcs.TrySetCanceled(cancellationToken);
                        return;
                    }

                    tcs.TrySetResult(func());
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }

            try
            {
                if (this.InvokeRequired)
                {
                    _owner.BeginInvoke((Action)Execute);
                }
                else
                {
                    Execute();
                }
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }

            return tcs.Task;
        }

        public Task<bool> LoadRequestAsync(
            ChromiumWebBrowser browser,
            string url,
            string requestMethod = "GET",
            string referrer = null,
            WebHeaderCollection headers = null,
            byte[] postDataBytes = null,
            int timeoutMs = 15000)
        {
            var tcs = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            if (browser == null ||
                browser.IsDisposed ||
                string.IsNullOrWhiteSpace(url))
            {
                tcs.TrySetResult(false);
                return tcs.Task;
            }

            EventHandler<FrameLoadEndEventArgs> frameLoadEndHandler = null;
            EventHandler<LoadErrorEventArgs> loadErrorHandler = null;

            var cts = new CancellationTokenSource();

            void Cleanup()
            {
                try
                {
                    browser.FrameLoadEnd -= frameLoadEndHandler;
                    browser.LoadError -= loadErrorHandler;
                    cts.Cancel();
                    cts.Dispose();
                }
                catch
                {
                }
            }

            frameLoadEndHandler = (s, e) =>
            {
                if (!e.Frame.IsMain)
                    return;

                bool success =
                    e.HttpStatusCode >= 200 &&
                    e.HttpStatusCode < 400;

                Cleanup();
                tcs.TrySetResult(success);
            };

            loadErrorHandler = (s, e) =>
            {
                if (!e.Frame.IsMain)
                    return;

                // Aborted 有时是新导航打断旧导航，不一定是真失败
                Cleanup();
                tcs.TrySetResult(false);
            };

            browser.FrameLoadEnd += frameLoadEndHandler;
            browser.LoadError += loadErrorHandler;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(timeoutMs, cts.Token);

                    if (!tcs.Task.IsCompleted)
                    {
                        try
                        {
                            browser.GetBrowser()?.StopLoad();
                        }
                        catch
                        {
                        }

                        Cleanup();
                        tcs.TrySetResult(false);
                    }
                }
                catch (TaskCanceledException)
                {
                }
            });

            try
            {
                var frame = browser.GetMainFrame();

                if (frame == null || !frame.IsValid)
                {
                    Cleanup();
                    tcs.TrySetResult(false);
                    return tcs.Task;
                }

                bool initializePostData =
                    string.Equals(requestMethod, "POST", StringComparison.OrdinalIgnoreCase);

                var request = frame.CreateRequest(
                    initializePostData: initializePostData);

                if (initializePostData &&
                    postDataBytes != null &&
                    postDataBytes.Length > 0)
                {
                    request.InitializePostData();

                    if (request.PostData != null)
                    {
                        request.PostData.AddData(postDataBytes);
                    }
                }

                request.Url = url;
                request.Method = string.IsNullOrWhiteSpace(requestMethod)
                    ? "GET"
                    : requestMethod.ToUpperInvariant();

                if (headers != null && headers.HasKeys())
                {
                    var originHeaders = request.Headers ?? new NameValueCollection();

                    foreach (string keyName in headers.AllKeys)
                    {
                        if (!string.IsNullOrWhiteSpace(keyName))
                        {
                            originHeaders.Set(keyName, headers[keyName]);
                        }
                    }

                    request.Headers = originHeaders;
                }

                if (!string.IsNullOrWhiteSpace(referrer))
                {
                    request.SetReferrer(
                        referrer,
                        ReferrerPolicy.NeverClearReferrer);
                }

                frame.LoadRequest(request);
            }
            catch
            {
                Cleanup();
                tcs.TrySetResult(false);
            }
            return tcs.Task;
        }





        private async Task MainAsync(string url, string url2, string userAgent, string referer,
            JObject taskParam, JObject devInfo, JObject _args, string cachePath,
            Action<string> addressChanged, Action<string> LogWriteLine,
            Action<byte[], int, int> DisplayScreenshot, bool screenshot = false)
        {




            var proxy_server = _args["proxy_server"]?.ToString();
            var browserSettings = new BrowserSettings()
            {
                WindowlessFrameRate = 15,
                //ImageLoading = CefState.Disabled,
                //Databases = CefState.Disabled,
                //LocalStorage = CefState.Disabled,
                WebGl = CefState.Disabled,

            };
            //DisableImage
            if (_args.SelectToken("disableImage")?.Value<bool>() ?? false)
            {
                browserSettings.ImageLoading = CefState.Disabled;
            }


            var requestContextSettings = new RequestContextSettings
            {
                //CachePath = cachePath,
                AcceptLanguageList = "zh-CN,en-US",
                PersistSessionCookies = false,
            };
            int sleepInt = 0;
            if (taskParam.ContainsKey("sleep") && !string.IsNullOrWhiteSpace(taskParam["sleep"].ToString()))
            {
                var sleep = taskParam["sleep"].ToString();
                sleepInt = new Random((int)System.DateTime.Now.Millisecond).Next(1, 5);
                try
                {
                    if (sleep.Contains("-"))
                    {
                        var values = sleep.Split('-');
                        sleepInt = new Random().Next(Convert.ToInt32(values[0]), Convert.ToInt32(values[1]));
                    }
                    else
                    {
                        sleepInt = Convert.ToInt32(sleep);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }

            sleepInt = 180;
            var sw = devInfo["sw"].Value<int>();
            var sh = devInfo["sh"].Value<int>();
            var os = _args["os"]?.Value<int>() ?? 1;
            var name = devInfo["name"]?.Value<string>();
            var pvTotal = taskParam["pv"].Value<int>();
            var devProfile = os == 7 ? DeviceViewportMatcher.Match(sw, sh, DeviceSystemType.Windows) : os == 2 ? DeviceViewportMatcher.Match(sw, sh, DeviceSystemType.IOS, string.IsNullOrWhiteSpace(name) ? null : name) : DeviceViewportMatcher.Match(sw, sh, DeviceSystemType.Android);



            LogWriteLine($"缓存:{cachePath},sleep={sleepInt},os={os},pv={pvTotal},osv={devInfo["osv"]?.Value<string>()}");


            //K30 PRO:393 * 873
            //Pixel5: 393 * 851
            //Sumsung S9+412 * 946
            //Sumsung Galaxy S8 + 360 * 740
            //const int loadTimeoutMs = 30000;
            const int pvIntervalMs = 1000;

            using (var requestContext = new RequestContext(requestContextSettings))
            {
                using (var browser = new ChromiumWebBrowser("about:blank", browserSettings, requestContext))
                {
                    browser.Size = new System.Drawing.Size(sw, sh);
                    browser.LifeSpanHandler = new CfxLifeSpanHandler();
                    browser.JsDialogHandler = new CfxJsDialogHandler();
                    //browser.RequestHandler = new ExternalProtocolRequestHandler(message => { });

                    //CefCachePaths.RootCachePath = System.IO.Path.Combine(new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).Parent.FullName, "User Data", consumerId);
                    var cachedRequestHandler = new CachedRequestHandler(_sharedResourceCacheManager);
                    browser.RequestHandler = cachedRequestHandler;


                    if (screenshot)
                    {
                        browser.AddressChanged += (s, args) =>
                        {
                            addressChanged(args.Address);
                        };
                        //browser.FrameLoadStart += (s, args) =>
                        //{
                        //    if (args.Frame.IsMain)
                        //    {

                        //        browser.ShowDevTools();
                        //    }
                        //};
                    }

                    await browser.WaitForInitialLoadAsync();
                    await Cef.UIThreadTaskFactory.StartNew(() =>
                    {
                        browser.Size = new System.Drawing.Size(sw, sh);

                        #region 代理设置
                        if (!string.IsNullOrWhiteSpace(proxy_server))
                        {
                            // LogWriteLine($"设置代理IP:{proxy_server}");
                            var context = browser.GetBrowser().GetHost().RequestContext;
                            var v = new Dictionary<string, object>();
                            v["mode"] = "fixed_servers";
                            v["server"] = proxy_server;
                            bool success = context.SetPreference("proxy", v, out string error);
                        }
                        #endregion

                    });
       
                    using (var devToolsClient = browser.GetDevToolsClient())
                    {
                        //await devToolsClient.Storage.ClearDataForOriginAsync("*", "cache_storage,cookies,local_storage");
                        try
                        {
                            if (os == 1 || os == 2)
                            {
                                await devToolsClient.Emulation.SetTouchEmulationEnabledAsync(true, new Random().Next(4, 6));
                                await devToolsClient.Emulation.SetDeviceMetricsOverrideAsync(
                                    width: devProfile.ViewportWidth,
                                    height: devProfile.ViewportHeight,
                                    deviceScaleFactor: devProfile.DeviceScaleFactor,
                                    scale: 1,
                                    screenWidth: devProfile.ViewportWidth,
                                    screenHeight: devProfile.ViewportHeight,
                                    positionX: 0,
                                    positionY: 0,
                                    screenOrientation: new CefSharp.DevTools.Emulation.ScreenOrientation { Angle = 0, Type = CefSharp.DevTools.Emulation.ScreenOrientationType.PortraitPrimary },
                                    mobile: true);


                                var userAgentMetadata = new UserAgentMetadata()
                                {
                                    Brands = new List<UserAgentBrandVersion>(),
                                    FullVersionList = new List<UserAgentBrandVersion>(),
                                    Mobile = true,
                                    Model = devInfo["model"]?.Value<string>() ?? "",
                                    Architecture = "",
                                    Platform = (os == 2 ? "iPhone" : "Android"),
                                    PlatformVersion = devInfo["osv"]?.Value<string>(),
                                    Bitness = "",
                                    Wow64 = false,
                                };

                                await devToolsClient.Emulation.SetUserAgentOverrideAsync(userAgent: userAgent, platform: os == 2 ? "iOS" : "Linux aarch64", userAgentMetadata: userAgentMetadata);
                                await devToolsClient.Emulation.SetScrollbarsHiddenAsync(true);
                            }
                            else
                            {
                                await devToolsClient.Emulation.SetUserAgentOverrideAsync(userAgent: userAgent);
                            }

                            for (var pvIndex = 1; pvIndex <= pvTotal; pvIndex++)
                            {
                                if (!string.IsNullOrWhiteSpace(referer))
                                {
                                    await LoadRequestAsync(browser, url, "GET", referrer: referer);
                                }
                                else
                                {
                                    var loadResponse = await browser.LoadUrlAsync(url);
                                    if (!loadResponse.Success)
                                    {
                                        // 加载失败
                                        LogWriteLine(
                                            $"LoadUrlAsync失败: Url={url}, " +
                                            $"Success={loadResponse?.Success}, " +
                                            $"ErrorCode={loadResponse?.ErrorCode}, " +
                                            $"HttpStatusCode={loadResponse?.HttpStatusCode}");
                                    }
                                }
                                if (pvIndex < pvTotal && pvIntervalMs > 0)
                                {
                                    await Task.Delay(TimeSpan.FromMilliseconds(pvIntervalMs));
                                }
                            }

                           
                            
                            if (screenshot)
                            {
                                await Task.Delay(TimeSpan.FromSeconds(5));
                                var host = browser.GetBrowserHost();
                                host.Invalidate(PaintElementType.View);
                                var screenshotBytes = await browser.CaptureScreenshotAsync();
                                if (screenshotBytes != null || screenshotBytes.Length > 0)
                                {
                                    DisplayBitmap(screenshotBytes, devProfile.ViewportWidth, devProfile.ViewportHeight);
                                }
                            }

                            if (sleepInt > 0)
                            {
                                await Task.Delay(sleepInt * 1000);
                            }
                            else
                            {
                                await Task.Delay(TimeSpan.FromSeconds(4));
                            }
     
                        }
                        catch (Exception ex)
                        {

                            LogWriteLine($"err:{ex.Message}");
                        }
                        finally
                        {
                            var flushed = cachedRequestHandler.WaitForPendingWrites(15000);
                            if (!flushed)
                            {
                                LogWriteLine("缓存落盘等待超时(15s)，可能仍有少量资源未写入。");
                            }
                        }

                    }
                }
            }

        }
        private void DisplayBitmap(byte[] screenshotBytes, int sw, int sh)
        {
            using (var stream = new MemoryStream(screenshotBytes))
            {
                using (var image = Image.FromStream(stream))
                {
                    var oldImage = pictureBoxSreenshot.Image;
                    var screenshot = new Bitmap(image);
                    if (screenshot != null)
                    {
                        pictureBoxSreenshot.Image = null;
                        pictureBoxSreenshot.Image = screenshot;
                        UiInvokeAsync(() =>
                        {
                            pictureBoxSreenshot.Width = sw;
                            pictureBoxSreenshot.Height = sh;
                        });

                    }
                    oldImage?.Dispose();
                }
            }



        }
        private async Task<string> CloseProxyServer(string ipkey, string port)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                HttpResponseMessage response = await httpClient.GetAsync($"{ipkey}&pattern=json&port={port}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }
        private async Task ResolveMessage(string value)
        {
            var message = (JObject)JsonConvert.DeserializeObject(value);

            if (message["Msg"].ToString().Equals("LOAD"))
            {
                LogWriteLine(value);
                var url = message["Url"].ToString();
                var url2 = message["Url2"]?.ToString();
                var referer = message["Referer"]?.ToString();
                var args = (JObject)JsonConvert.DeserializeObject(message["args"].ToString());
                var proxy_server = args["proxy_server"]?.ToString();
                var userAgent = message["UserAgent"].ToString();
                var taskParam = (JObject)JsonConvert.DeserializeObject(message["Param"].ToString());
                var devInfo = (JObject)JsonConvert.DeserializeObject(message["DevInfo"].ToString());
                var cacheIndex = message["CacheIndex"].ToString();
                var ua = devInfo["ua"].Value<string>();
                if (!string.IsNullOrEmpty(url))
                {
                    var cachePath = System.IO.Path.Combine(CefCachePaths.RootCachePath, cacheIndex);
                    if (!System.IO.Directory.Exists(cachePath))
                    {
                        System.IO.Directory.CreateDirectory(cachePath);
                    }
                    LogWriteLine($"执行任务[{taskParam["id"]}]:{taskParam["title"]},{proxy_server},{url},{referer},{cacheIndex},{ua},开始");
                    try
                    {
                        await MainAsync(url, url2, userAgent, referer, taskParam, devInfo, args, cachePath, (address) =>
                        {
                            UiInvokeAsync(() =>
                            {
                                this.textBox1.Text = address;
                            });
                        }, LogWriteLine, DisplayBitmap, this.showform);

                    }
                    catch (Exception ex)
                    {
                        LogWriteLine(ex.InnerException?.Message);
                    }
                    LogWriteLine($"执行任务[{taskParam["id"]}]:{taskParam["title"]},{proxy_server},{cacheIndex},完成");

                }
            }
            else if (message["Msg"].ToString().Equals("STOP"))
            {
                await Task.Run(() =>
                {
                    LogWriteLine("5秒后退出该进程");
                    SpinWait.SpinUntil(() => false, 5000);
                    sync.Post((p) =>
                    {
                        System.Environment.Exit(0);
                    }, null);
                });
            }
            else if (message["Msg"].ToString().Equals("SHOW"))
            {
                sync.Post((p) =>
                {
                    this.showform = true;
                    this.SetVisibleCore(true);
                }, null);
            }
            else if (message["Msg"].ToString().Equals("HIDE"))
            {
                sync.Post((p) =>
                {
                    this.showform = false;
                    this.SetVisibleCore(false);
                }, null);
            }
        }
        protected override void DefWndProc(ref System.Windows.Forms.Message m)
        {
            switch (m.Msg)
            {
                case WM_COPYDATA:
                    COPYDATASTRUCT data = new COPYDATASTRUCT();
                    Type myType = data.GetType();
                    data = (COPYDATASTRUCT)m.GetLParam(myType);
                    if (!string.IsNullOrWhiteSpace(data.lpData))
                    {
                        Task.Run(async () => await ResolveMessage(data.lpData));
                    }
                    break;
                default:
                    base.DefWndProc(ref m);
                    break;
            }
        }
        public MainForm()
        {
            InitializeComponent();
            _sharedResourceCacheManager = new ResourceCacheManager(new ResourceCacheOptions
            {
                CacheRoot = Path.Combine(CefCachePaths.GlobalCachePath, "resource_cache"),
                MaxMemoryCaptureBytes = 10 * 1024 * 1024,
                CacheExpireDays = 3,
                EnableImageCache = true,
                EnableScriptCache = true,
                EnableCssCache = true,
                EnableFontCache = false,
                EnableVideoCache = false
            });
            this.sync = SynchronizationContext.Current;
            var commandLineArgs = System.Environment.GetCommandLineArgs();
            foreach (var c in commandLineArgs)
            {
                if (c.StartsWith("hwnd="))
                {
                    hMainWnd = Convert.ToInt32(c.Split('=')[1]);
                }
                else if (c.StartsWith("showform="))
                {
                    showform = Convert.ToBoolean(c.Split('=')[1]);
                }
                else if (c.StartsWith("uuid="))
                {
                    uuid = c.Split('=')[1];
                }
            }
            SendRegMessage();
            LogWriteLine($"{Process.GetCurrentProcess().Id},{this.Handle},{System.DateTime.Now.ToString("HH:mm:ss")}");
        }
        protected override void SetVisibleCore(bool value)
        {
            value = showform;
            LogWriteLine($"SetVisibleCore={showform}");
            base.SetVisibleCore(value);
        }
        private void SendRegMessage()
        {
            var currentProcess = Process.GetCurrentProcess();
            var message = JsonConvert.SerializeObject(JObject.FromObject(new
            {
                Msg = "REG",
                WindowHandle = (int)this.Handle,
                uuid = this.uuid,
                ProcessId = currentProcess.Id,
                ProcessPath = currentProcess.MainModule.FileName,
            }));

            byte[] sarr = System.Text.Encoding.Default.GetBytes(message);
            COPYDATASTRUCT cds;
            cds.dwData = (IntPtr)100;
            cds.lpData = message;
            cds.cbData = sarr.Length + 1;
            SendMessage(hMainWnd, WM_COPYDATA, 0, ref cds);
        }
        private void MainForm_Load(object sender, EventArgs e)
        {

        }
        private static async Task<string> GetIp(string url)
        {
            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        private static async Task<string> GetDev()
        {
            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.GetAsync("http://117.21.200.18:9000/api/getdev.php?count=1&type=android");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        private static async Task<string> GetTask(string name)
        {
            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.GetAsync($"http://117.21.200.19/client-v2.php?type=1&action=getTask&task={name}&test=0&_t={System.DateTime.Now.Ticks}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            string urlText = textBox1.Text;

            Task.Factory.StartNew(async () =>
            {

                var dev = (JObject)JObject.Parse(await GetDev())["data"][0];
                var task = (JObject)JObject.Parse(await GetTask("dytest"))["task"][0];




                var ua = dev["ua"].Value<string>();

                string proxy_server = null;
                string realIp = null;
                var url = task["url"].Value<string>();
                url = "https://m.douyu.com/topic/bossxly?ditchName=H5yangfei04";
                var referer = task["referer"]?.Value<string>();
                var _args = new JObject();
                if (!string.IsNullOrWhiteSpace(proxy_server))
                {
                    _args["IsProxyMode"] = true;
                    _args["proxy_server"] = proxy_server;
                }
                else
                {
                    _args["IsProxyMode"] = false;
                    _args["proxy_server"] = null;
                }


                _args["os"] = 1;
                proxy_server = null;
                var cachePath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "User Data", "1", "s0");
                await MainAsync(url, null, ua, referer, task, dev, _args, cachePath, (address) =>
                {
                    this.sync.Post((p) =>
                    {
                        this.textBox1.Text = p.ToString();
                    }, address);
                }, LogWriteLine, DisplayBitmap, screenshot: true);
                LogWriteLine($"{proxy_server},完成");

            });

        }
    }
    public struct COPYDATASTRUCT
    {
        public IntPtr dwData;
        public int cbData;
        [MarshalAs(UnmanagedType.LPStr)]
        public string lpData;
    }
}
