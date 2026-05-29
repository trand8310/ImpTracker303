using CefClient.Common;
using CefClient.Handler;
using CefSharp;
using CefSharp.DevTools.Emulation;
using CefSharp.OffScreen;
using Imp.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace CefClient
{
    public partial class MainForm : Form
    {
        private IntPtr hMainWnd = IntPtr.Zero;
        private bool isHiddenMode = false;
        private string _clientId = string.Empty;
        private ResourceCacheManager _sharedResourceCacheManager;


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


        private async Task MainAsync(string url, string url2, string userAgent, string referer,
            JObject taskParam, JObject devInfo, JObject _args, string cachePath,
            Action<string> addressChanged, Action<string> log,
            Action<byte[], int, int> screenshot, bool hasScreenshot = false)
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

            log($"缓存:{cachePath},sleep={sleepInt},os={os},pv={pvTotal},osv={devInfo["osv"]?.Value<string>()}");

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


                    if (hasScreenshot)
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
                                    await browser.LoadRequestAsync(url, "GET", referrer: referer);
                                }
                                else
                                {
                                    var loadResponse = await browser.LoadUrlAsync(url);
                                    if (!loadResponse.Success)
                                    {
                                        // 加载失败
                                        log(
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



                            if (hasScreenshot)
                            {
                                await Task.Delay(TimeSpan.FromSeconds(5));
                                var host = browser.GetBrowserHost();
                                host.Invalidate(PaintElementType.View);
                                var screenshotBytes = await browser.CaptureScreenshotAsync();
                                if (screenshotBytes != null || screenshotBytes.Length > 0)
                                {
                                    this.PreviewScreenshot(screenshotBytes, devProfile.ViewportWidth, devProfile.ViewportHeight);
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

                            log($"err:{ex.Message}");
                        }
                        finally
                        {
                            var flushed = cachedRequestHandler.WaitForPendingWrites(15000);
                            if (!flushed)
                            {
                                log("缓存落盘等待超时(15s)，可能仍有少量资源未写入。");
                            }
                        }

                    }
                }
            }

        }


        #region PreviewScreenshot
        private readonly object _screenshotLock = new object();

        private void PreviewScreenshot(byte[] screenshotBytes, int sw, int sh)
        {
            lock (_screenshotLock)
            {
                PreviewScreenshotCore(screenshotBytes, sw, sh);
            }
        }

        private void PreviewScreenshotCore(byte[] screenshotBytes, int sw, int sh)
        {
            if (screenshotBytes == null || screenshotBytes.Length == 0)
                return;

            if (pictureBoxSreenshot == null || pictureBoxSreenshot.IsDisposed)
                return;

            if (sw <= 0 || sh <= 0)
                return;

            if (sw > 10000 || sh > 10000)
                return;

            Bitmap newBitmap = null;

            try
            {
                using (var stream = new MemoryStream(screenshotBytes))
                using (var image = Image.FromStream(stream, false, false))
                {
                    newBitmap = new Bitmap(image);
                }

                this.InvokeOnUiThreadIfRequired(() =>
                {
                    if (pictureBoxSreenshot == null || pictureBoxSreenshot.IsDisposed)
                    {
                        newBitmap?.Dispose();
                        newBitmap = null;
                        return;
                    }

                    Image oldImage = pictureBoxSreenshot.Image;

                    pictureBoxSreenshot.SuspendLayout();

                    try
                    {
                        pictureBoxSreenshot.Image = null;
                        pictureBoxSreenshot.Size = new Size(sw, sh);
                        pictureBoxSreenshot.Image = newBitmap;
                        newBitmap = null;
                    }
                    finally
                    {
                        pictureBoxSreenshot.ResumeLayout();
                    }

                    oldImage?.Dispose();
                });
            }
            catch
            {
                newBitmap?.Dispose();
                // 这里建议写日志
            }
        }

        #endregion


        #region ResolveMessage
        private readonly SemaphoreSlim _loadSemaphore = new SemaphoreSlim(1, 1);
        private volatile bool _stopping = false;
        private Task RunFireAndForgetAsync(Func<Task> action)
        {
            return Task.Run(async () =>
            {
                try
                {
                    await action().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    LogWriteLine("后台任务异常：" + ex);
                }
            });
        }
        private static JObject TryParseObject(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            try
            {
                return JObject.Parse(json);
            }
            catch
            {
                return null;
            }
        }
        private static string GetString(JObject obj, string name, string defaultValue = "")
        {
            if (obj == null)
                return defaultValue;

            var token = obj[name];

            if (token == null || token.Type == JTokenType.Null)
                return defaultValue;

            return token.ToString();
        }
        private static JObject GetObject(JObject obj, string name)
        {
            if (obj == null)
                return new JObject();

            var token = obj[name];

            if (token == null || token.Type == JTokenType.Null)
                return new JObject();

            if (token.Type == JTokenType.Object)
                return (JObject)token;

            var text = token.ToString();

            if (string.IsNullOrWhiteSpace(text))
                return new JObject();

            try
            {
                return JObject.Parse(text);
            }
            catch
            {
                return new JObject();
            }
        }
        private async Task ResolveMessageSafeAsync(string value)
        {
            try
            {
                await ResolveMessageAsync(value).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogWriteLine("ResolveMessage 异常：" + ex);
            }
        }
        private async Task ResolveMessageAsync(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            var message = TryParseObject(value);
            if (message == null)
            {
                LogWriteLine("消息JSON格式错误：" + value);
                return;
            }

            var msg = GetString(message, "Msg");

            switch (msg.ToUpperInvariant())
            {
                case "LOAD":
                    await HandleLoadAsync(message, value).ConfigureAwait(false);
                    break;

                case "STOP":
                    await HandleStopAsync().ConfigureAwait(false);
                    break;

                case "SHOW":
                    HandleShow();
                    break;

                case "HIDE":
                    HandleHide();
                    break;

                default:
                    LogWriteLine("未知消息类型：" + msg);
                    break;
            }
        }
        private async Task HandleLoadAsync(JObject message, string rawMessage)
        {
            if (_stopping)
            {
                LogWriteLine("进程正在停止，忽略LOAD任务");
                return;
            }

            await _loadSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                if (_stopping)
                {
                    LogWriteLine("进程正在停止，忽略LOAD任务");
                    return;
                }

                LogWriteLine(rawMessage);

                var url = GetString(message, "Url");
                if (string.IsNullOrWhiteSpace(url))
                {
                    LogWriteLine("LOAD任务Url为空，已忽略");
                    return;
                }

                var url2 = GetString(message, "Url2");
                var referer = GetString(message, "Referer");
                var userAgent = GetString(message, "UserAgent");
                var cacheIndex = GetString(message, "CacheIndex", "default");

                var args = GetObject(message, "args");
                var taskParam = GetObject(message, "Param");
                var devInfo = GetObject(message, "DevInfo");

                var proxyServer = GetString(args, "proxy_server");
                var ua = GetString(devInfo, "ua");

                var taskId = GetString(taskParam, "id");
                var taskTitle = GetString(taskParam, "title");

                var cachePath = Path.Combine(CefCachePaths.RootCachePath, cacheIndex);

                try
                {
                    Directory.CreateDirectory(cachePath);
                }
                catch (Exception ex)
                {
                    LogWriteLine($"创建缓存目录失败：{cachePath}，异常：{ex}");
                    return;
                }

                LogWriteLine($"执行任务[{taskId}]:{taskTitle},{proxyServer},{url},{referer},{cacheIndex},{ua},开始");

                try
                {
                    await MainAsync(
                        url,
                        url2,
                        userAgent,
                        referer,
                        taskParam,
                        devInfo,
                        args,
                        cachePath,
                        address =>
                        {
                            SetTextBoxAddress(address);
                        },
                        LogWriteLine,
                        PreviewScreenshot,
                        this.isHiddenMode
                    ).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    LogWriteLine($"执行任务[{taskId}]异常：" + ex);
                }
                finally
                {
                    LogWriteLine($"执行任务[{taskId}]:{taskTitle},{proxyServer},{cacheIndex},完成");
                }
            }
            finally
            {
                _loadSemaphore.Release();
            }
        }
        private async Task HandleStopAsync()
        {
            if (_stopping)
                return;

            _stopping = true;

            LogWriteLine("5秒后退出该进程");

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            }
            catch
            {
            }

            ExitProcess();
        }
        private void HandleShow()
        {
            this.InvokeOnUiThreadIfRequired(() =>
            {
                this.isHiddenMode = true;
                this.SetVisibleCore(true);
            });
        }
        private void HandleHide()
        {
            this.InvokeOnUiThreadIfRequired(() =>
            {
                this.isHiddenMode = false;
                this.SetVisibleCore(false);
            });
        }
        private void SetTextBoxAddress(string address)
        {
            this.InvokeOnUiThreadIfRequired(() =>
            {
                if (!this.IsDisposed && this.textBox1 != null && !this.textBox1.IsDisposed)
                {
                    this.textBox1.Text = address ?? string.Empty;
                }
            });
        }
        private void ExitProcess()
        {
            try
            {
                this.InvokeOnUiThreadIfRequired(() =>
                {
                    try
                    {
                        Application.Exit();
                    }
                    catch
                    {
                        Environment.Exit(0);
                    }
                });
            }
            catch
            {
                Environment.Exit(0);
            }
        }
        protected override void DefWndProc(ref System.Windows.Forms.Message m)
        {
            switch (m.Msg)
            {
                case WinTypes.WM_COPYDATA:
                    {
                        try
                        {
                            var data = new COPYDATASTRUCT();
                            var type = data.GetType();
                            data = (COPYDATASTRUCT)m.GetLParam(type);

                            var rawMessage = data.lpData;

                            if (!string.IsNullOrWhiteSpace(rawMessage))
                            {
                                _ = RunFireAndForgetAsync(() => ResolveMessageSafeAsync(rawMessage));
                            }
                        }
                        catch (Exception ex)
                        {
                            LogWriteLine("WM_COPYDATA处理异常：" + ex);
                        }

                        break;
                    }

                default:
                    base.DefWndProc(ref m);
                    break;
            }
        }
        #endregion

        private void InitResourceCacheManager()
        {
            _sharedResourceCacheManager = new ResourceCacheManager(new ResourceCacheOptions
            {
                CacheRoot = Path.Combine(CefCachePaths.GlobalCachePath, "resource_cache"),
                MaxMemoryCaptureBytes = 10 * 1024 * 1024,
                CacheExpireDays = 3,

                EnableImageCache = true,
                EnableScriptCache = true,
                EnableCssCache = true,
                EnableFontCache = true,
                EnableVideoCache = false
            });
        }
        private static string GetCommandLineString(string[] args, string prefix, string defaultValue = "")
        {
            if (args == null || args.Length == 0 || string.IsNullOrWhiteSpace(prefix))
                return defaultValue;

            var arg = args.FirstOrDefault(x =>
                x.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(arg))
                return defaultValue;

            return arg.Substring(prefix.Length).Trim();
        }
        private static IntPtr GetCommandLineIntPtr(string[] args, string prefix)
        {
            var value = GetCommandLineString(args, prefix);

            if (string.IsNullOrWhiteSpace(value))
                return IntPtr.Zero;

            if (long.TryParse(value, out var handleValue) && handleValue != 0)
                return new IntPtr(handleValue);

            return IntPtr.Zero;
        }
        private static bool GetCommandLineBool(string[] args, string prefix, bool defaultValue = false)
        {
            var value = GetCommandLineString(args, prefix);

            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            if (bool.TryParse(value, out var result))
                return result;

            if (value == "1")
                return true;

            if (value == "0")
                return false;

            if (value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("y", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("on", StringComparison.OrdinalIgnoreCase))
                return true;

            if (value.Equals("no", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("n", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("off", StringComparison.OrdinalIgnoreCase))
                return false;

            return defaultValue;
        }
        private void ParseCommandLineArgs()
        {
            var args = Environment.GetCommandLineArgs();
            hMainWnd = GetCommandLineIntPtr(args, "--main-handle=");
            isHiddenMode = GetCommandLineBool(args, "--hidden-mode=", false);
            _clientId = GetCommandLineString(args, "--client-id=");
        }


        private void MainForm_Shown(object sender, EventArgs e)
        {
            if (isHiddenMode)
            {
                this.Hide();
            }
            if (!string.IsNullOrWhiteSpace(_clientId))
                NotifyMainProcessClientStarted(_clientId);
            LogWriteLine($"{Process.GetCurrentProcess().Id},{this.Handle},{DateTime.Now:HH:mm:ss}");
        }
        protected override void SetVisibleCore(bool value)
        {
            if (isHiddenMode && !IsHandleCreated)
            {
                base.SetVisibleCore(false);
                return;
            }
            base.SetVisibleCore(value);
        }

        public MainForm()
        {
            InitializeComponent();
            InitResourceCacheManager();
            ParseCommandLineArgs();
            this.Shown += MainForm_Shown;
        }

        private void NotifyMainProcessClientStarted(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                LogWriteLine("注册失败：clientId为空");
                return;
            }

            if (hMainWnd == IntPtr.Zero)
            {
                LogWriteLine("注册失败：主窗口句柄为空");
                return;
            }

            string processPath = string.Empty;
            int processId = 0;

            try
            {
                using (var currentProcess = Process.GetCurrentProcess())
                {
                    processId = currentProcess.Id;

                    try
                    {
                        processPath = currentProcess.MainModule?.FileName ?? string.Empty;
                    }
                    catch
                    {
                        processPath = Application.ExecutablePath;
                    }
                }
            }
            catch (Exception ex)
            {
                LogWriteLine("获取当前进程信息失败：" + ex);
            }

            var payload = new JObject
            {
                ["Msg"] = "CLIENT_STARTED",
                ["ClientHandle"] = this.Handle.ToInt64(),
                ["ClientId"] = clientId,
                ["ProcessId"] = processId,
                ["ProcessPath"] = processPath
            };

            string message = payload.ToString(Formatting.None);

            try
            {
                var cds = new COPYDATASTRUCT
                {
                    dwData = new IntPtr(100),
                    lpData = message,

                    // Unicode 一个字符 2 字节，最后补一个 \0，也是 2 字节
                    cbData = (message.Length + 1) * 2
                };

                IntPtr sendResult;

                var ret = NativeMethod.SendMessageTimeout(
                    hMainWnd,
                    WinTypes.WM_COPYDATA,
                    this.Handle,
                    ref cds,
                    WinTypes.SMTO_ABORTIFHUNG,
                    3000,
                    out sendResult
                );

                if (ret == IntPtr.Zero)
                {
                    int error = Marshal.GetLastWin32Error();
                    LogWriteLine($"注册消息发送失败或超时：ClientId={clientId}, Error={error}");
                    return;
                }

                LogWriteLine($"注册消息发送成功：ClientId={clientId}, ProcessId={processId}");
            }
            catch (Exception ex)
            {
                LogWriteLine("发送注册消息异常：" + ex);
            }
        }





        private void buttonStart_Click(object sender, EventArgs e)
        {
            string urlText = textBox1.Text;

            Task.Factory.StartNew(async () =>
            {

                var dev = (JObject)JObject.Parse(await AdHelper.GetDev())["data"][0];
                var task = (JObject)JObject.Parse(await AdHelper.GetTask("dytest"))["task"][0];
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
                    this.InvokeOnUiThreadIfRequired(() =>
                    {
                        this.textBox1.Text = address;
                    });
                }, LogWriteLine, screenshot: PreviewScreenshot, hasScreenshot: true);
                LogWriteLine($"{proxy_server},完成");

            });

        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }
    }

}
