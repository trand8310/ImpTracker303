using AdxImp.Win32;
using MainClient.Common;
using MainClient.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Web;

namespace MainClient
{
    public partial class MainForm : Form
    {


        private readonly ILogger _logger;
        private AppSetting setting = null;
        private CancellationTokenSource cts = null;
        private SynchronizationContext sync;
        private ConcurrentDictionary<string, ProcessItem> processOfList;

        /// <summary>
        /// 标记应用程序是否重启
        /// </summary>
        private bool applicationrestart = false;
        private bool applicationstop = false;
        /// <summary>
        /// UV合计数量
        /// </summary>
        private int TotalUVCount = 0;
        private IpHelper _ipHelper;
        private ProxyChecker.ProxyTester _ipTester = new ProxyChecker.ProxyTester();
        private Stopwatch sw = new Stopwatch();
        private TaskDispatchManager taskDispatchManager = null;
        private CefClientProcessManager cefProcessManager = null;
        private List<JToken> pendingTasks = new List<JToken>();
        //ANDROID 设备参数
        private ConcurrentQueue<JToken> android_dev_queues = new ConcurrentQueue<JToken>();
        //IOS 设备参数
        private ConcurrentQueue<JToken> ios_dev_queues = new ConcurrentQueue<JToken>();
        private IHttpClientFactory _httpClientFactory;
        private IntPtr selfWndHandle = IntPtr.Zero;
        private static readonly int CopyDataSendConcurrency = Math.Max(8, Math.Min(64, Environment.ProcessorCount * 4));
        private readonly SemaphoreSlim copyDataSendSemaphore = new SemaphoreSlim(CopyDataSendConcurrency, CopyDataSendConcurrency);
        private readonly CancellationTokenSource messageProcessingCts = new CancellationTokenSource();
        private readonly Channel<string> messageChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
        private Task messageProcessingTask = Task.CompletedTask;

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

        public void LogInfo(string msg)
        {
            if (InvokeRequired)
            {
                Invoke((MethodInvoker)(() => { LogInfo(msg); }));
                return;
            }
            LogDetailTextBox.AppendText($"{System.DateTime.Now.ToString("[HH:mm:ss]")} {msg}{Environment.NewLine}");
            LogDetailTextBox.ScrollToCaret();

        }


        #endregion

        #region 更新




        #endregion

        #region 消息处理
        private void StartMessageProcessor()
        {
            messageProcessingTask = Task.Run(() => ProcessClientMessagesAsync(messageProcessingCts.Token));
        }

        private async Task ProcessClientMessagesAsync(CancellationToken token)
        {
            try
            {
                await foreach (var message in messageChannel.Reader.ReadAllAsync(token))
                {
                    ResolveMessage(message);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "客户端消息处理队列异常");
                LogWriteLine("客户端消息处理队列异常：" + ex.Message);
            }
        }

        private void ResolveMessage(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            try
            {
                var message = JObject.Parse(value);
                var msg = message.Value<string>("Msg");
                if (!string.Equals(msg, "CLIENT_STARTED", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                var clientId = message.Value<string>("ClientId");
                if (string.IsNullOrWhiteSpace(clientId))
                {
                    LogWriteLine("客户端注册消息缺少ClientId");
                    return;
                }

                var clientHandleValue = message.Value<long?>("ClientHandle");
                if (!clientHandleValue.HasValue || clientHandleValue.Value == 0)
                {
                    LogWriteLine($"客户端注册消息句柄无效：ClientId={clientId}");
                    return;
                }

                var clientHandle = new IntPtr(clientHandleValue.Value);
                this.cefProcessManager?.UpdateWindowHandle(clientId, clientHandle);
            }
            catch (JsonException ex)
            {
                _logger?.LogWarning(ex, "客户端消息JSON解析失败：{Message}", value);
                LogWriteLine("客户端消息JSON解析失败：" + ex.Message);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "客户端消息处理失败：{Message}", value);
                LogWriteLine("客户端消息处理失败：" + ex.Message);
            }
        }

        private async Task<IntPtr> SendLoadUrlMessage(ProcessItem clientProcess, string url, string url2, JObject _args, string userAgent, string referer, JObject param, JToken devInfo, string cacheIndex)
        {
            if (clientProcess == null || clientProcess.ClientWindowHandle == IntPtr.Zero)
            {
                LogWriteLine("LOAD消息发送失败：客户端窗口句柄为空");
                return IntPtr.Zero;
            }

            var message = JsonConvert.SerializeObject(JObject.FromObject(new
            {
                Msg = "LOAD",
                Url = url,
                Url2 = url2,
                args = _args,
                UserAgent = userAgent,
                Referer = referer,
                DevInfo = devInfo,
                Param = param,
                CacheIndex = cacheIndex
            }));

            var cds = new COPYDATASTRUCT
            {
                dwData = new IntPtr(100),
                lpData = message,
                cbData = (message.Length + 1) * 2
            };

            await copyDataSendSemaphore.WaitAsync(this.cts?.Token ?? CancellationToken.None);
            try
            {
                IntPtr sendResult;
                var ret = NativeMethod.SendMessageTimeout(
                    clientProcess.ClientWindowHandle,
                    WinTypes.WM_COPYDATA,
                    selfWndHandle,
                    ref cds,
                    WinTypes.SMTO_ABORTIFHUNG,
                    3000,
                    out sendResult
                );

                if (ret == IntPtr.Zero)
                {
                    var error = Marshal.GetLastWin32Error();
                    LogWriteLine($"LOAD消息发送失败或超时：ProcessId={clientProcess.ProcessId}, Hwnd={clientProcess.ClientWindowHandle}, Error={error}");
                }

                return ret;
            }
            finally
            {
                copyDataSendSemaphore.Release();
            }
        }

        private static void SendShowFormMessage(IntPtr clientWindowHandle, bool show = true)
        {
            if (clientWindowHandle == IntPtr.Zero)
            {
                return;
            }

            var message = JsonConvert.SerializeObject(JObject.FromObject(new
            {
                Msg = show ? "SHOW" : "HIDE",
            }));

            var cds = new COPYDATASTRUCT
            {
                dwData = new IntPtr(100),
                lpData = message,
                cbData = (message.Length + 1) * 2
            };
            NativeMethod.SendMessage(clientWindowHandle, WinTypes.WM_COPYDATA, 0, ref cds);
        }

        protected override void DefWndProc(ref System.Windows.Forms.Message m)
        {
            switch (m.Msg)
            {
                case WinTypes.WM_COPYDATA:
                    try
                    {
                        var data = new COPYDATASTRUCT();
                        data = (COPYDATASTRUCT)m.GetLParam(data.GetType());
                        var rawMessage = data.lpData;
                        if (!string.IsNullOrWhiteSpace(rawMessage))
                        {
                            if (!messageChannel.Writer.TryWrite(rawMessage))
                            {
                                LogWriteLine("客户端消息队列已关闭，消息被忽略");
                            }
                        }
                        m.Result = IntPtr.One;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "WM_COPYDATA消息入队失败");
                        LogWriteLine("WM_COPYDATA消息入队失败：" + ex.Message);
                        m.Result = IntPtr.Zero;
                    }
                    break;
                default:
                    base.DefWndProc(ref m);
                    break;
            }
        }
        #endregion




        public MainForm(IHttpClientFactory httpClientFactory)
        {
            InitializeComponent();
            this._httpClientFactory = httpClientFactory;
            this.Text += $"［{AppSetting.AppVertion}］";
            sync = SynchronizationContext.Current;
            LoadAppSetting();
            if (this.setting == null)
            {
                this.setting = new AppSetting();
                UpdateAppSetting();
            }
            _ipHelper = new IpHelper(httpClientFactory);
            StartMessageProcessor();


            List<Control> controls = new List<Control>();
            foreach (var c in groupBox2.Controls)
            {
                if (c is NumericUpDown)
                {
                    (c as NumericUpDown).ValueChanged += (s, e) =>
                    {
                        UpdateAppSetting();
                    };
                }
                else if (c is TextBox)
                {
                    (c as TextBox).TextChanged += (s, e) =>
                    {
                        UpdateAppSetting();
                    };
                }
                else if (c is CheckBox)
                {
                    (c as CheckBox).Click += (s, e) =>
                    {
                        UpdateAppSetting();
                    };
                }
            }

            foreach (var c in groupBox5.Controls)
            {
                if (c is NumericUpDown)
                {
                    (c as NumericUpDown).ValueChanged += (s, e) =>
                    {
                        UpdateAppSetting();
                    };
                }
                else if (c is TextBox)
                {
                    (c as TextBox).TextChanged += (s, e) =>
                    {
                        UpdateAppSetting();
                    };
                }
                else if (c is CheckBox)
                {
                    (c as CheckBox).Click += (s, e) =>
                    {
                        UpdateAppSetting();
                    };
                }
            }


            LoadUserAgentData();

            var cachePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "chrome", "User Data");
            if (System.IO.Directory.Exists(cachePath))
                CleanCefCache(cachePath, maxTotalSizeMb: 40_000, keepRecentDays: 7);
        }


        /// <summary>
        /// 清理 Cef 缓存目录
        /// 规则：
        /// 1. 超过 keepRecentDays 天的文件直接删
        /// 2. 如果目录总大小超过 maxTotalSizeMb，从最旧文件开始删
        /// </summary>
        private static void CleanCefCache(string cachePath, long maxTotalSizeMb, int keepRecentDays)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(cachePath))
                    return;

                if (!Directory.Exists(cachePath))
                    return;

                var dir = new DirectoryInfo(cachePath);
                var now = DateTime.Now;
                var expireTime = now.AddDays(-keepRecentDays);

                // 先删过期文件
                foreach (var file in dir.GetFiles("*", SearchOption.AllDirectories))
                {
                    try
                    {
                        if (file.LastWriteTime < expireTime)
                        {
                            file.IsReadOnly = false;
                            file.Delete();
                        }
                    }
                    catch
                    {
                        // Cef 运行中可能占用部分文件，忽略即可
                    }
                }

                // 删除空目录
                DeleteEmptyDirectories(cachePath);

                // 再判断总大小
                long maxBytes = maxTotalSizeMb * 1024L * 1024L;

                var files = dir.GetFiles("*", SearchOption.AllDirectories)
                               .OrderBy(f => f.LastWriteTime)
                               .ToList();

                long totalSize = files.Sum(f =>
                {
                    try { return f.Length; }
                    catch { return 0L; }
                });

                if (totalSize <= maxBytes)
                    return;

                foreach (var file in files)
                {
                    if (totalSize <= maxBytes)
                        break;

                    try
                    {
                        long len = file.Length;
                        file.IsReadOnly = false;
                        file.Delete();
                        totalSize -= len;
                    }
                    catch
                    {
                        // 文件被占用就跳过
                    }
                }

                DeleteEmptyDirectories(cachePath);
            }
            catch
            {
                // 清理失败不影响主程序启动
            }
        }
        private static void DeleteEmptyDirectories(string rootPath)
        {
            try
            {
                if (!Directory.Exists(rootPath))
                    return;

                foreach (var dir in Directory.GetDirectories(rootPath, "*", SearchOption.AllDirectories)
                                             .OrderByDescending(x => x.Length))
                {
                    try
                    {
                        if (!Directory.EnumerateFileSystemEntries(dir).Any())
                        {
                            Directory.Delete(dir, false);
                        }
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
        }


        private void AddTaskInfo(JToken tasks)
        {
            this.Invoke(new MethodInvoker(() =>
            {
                this.taskInfoListView.BeginUpdate();
                this.taskInfoListView.Items.Clear();
                try
                {
                    foreach (var task in tasks)
                    {
                        ListViewItem lvi = new ListViewItem();
                        lvi.Tag = task["id"].ToString();
                        lvi.Text = $"{task["type"].ToString()}-{task["title"].ToString()}";
                        lvi.SubItems.Add("");
                        lvi.SubItems.Add("");
                        lvi.SubItems.Add("");
                        lvi.SubItems.Add("");
                        lvi.SubItems.Add("");
                        this.taskInfoListView.Items.Add(lvi);
                    }
                }
                finally
                {
                    this.taskInfoListView.EndUpdate();
                }

            }));
        }

        #region 设备
        public static SemaphoreSlim _mutex_dev = new SemaphoreSlim(1);

        /// <summary>
        /// 获取设备信息
        /// </summary>
        /// <param name="os"></param>
        /// <returns></returns>
        private JToken GetRandomDevByOS(OSType os)
        {
            if (os == OSType.IOS)
            {
                var jo = new JObject();
                jo.Add("idfa", DevMan.GetIdfa());
                jo.Add("imei", DevMan.GetImei());
                jo.Add("mac", CommonHelper.GetRandomMacAddress());
                return jo;
            }
            else if (os == OSType.ANDROID)
            {

                var jo = new JObject();
                jo.Add("android_id", DevMan.GetAndroidId());
                jo.Add("imei", DevMan.GetImei().ToLower());
                jo.Add("mac", CommonHelper.GetRandomMacAddress().ToUpper());
                return jo;
            }
            return null;
        }

        /// <summary>
        /// 获取设备信息
        /// </summary>
        /// <param name="os"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        /// 
        private async Task<JToken> GetDevByOS(OSType os)
        {
            if (os == OSType.IOS)
            {
                if (ios_dev_queues.TryDequeue(out var result))
                {
                    return result;
                }
                var json = await GetDevs(os, 100);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return null;
                }
                try
                {
                    var jo = (JObject)JsonConvert.DeserializeObject(json);
                    if (jo != null)
                    {
                        foreach (var item in jo["data"])
                        {
                            ios_dev_queues.Enqueue(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                if (ios_dev_queues.TryDequeue(out result))
                {
                    return result;
                }
                return null;
            }
            else if (os == OSType.ANDROID)
            {
                if (android_dev_queues.TryDequeue(out var result))
                {
                    return result;
                }
                var json = await GetDevs(os, 100);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return null;
                }
                try
                {
                    var jo = (JObject)JsonConvert.DeserializeObject(json);
                    if (jo != null)
                    {
                        foreach (var item in jo["data"])
                        {
                            android_dev_queues.Enqueue(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }

                if (android_dev_queues.TryDequeue(out result))
                {
                    return result;
                }
                return null;
            }
            else
            {
                return null;
            }
        }
        private async Task<string> GetDevs(OSType os, int count = 100)
        {
            try
            {

                await _mutex_dev.WaitAsync();
                HttpResponseMessage response = await _client.GetAsync($"{setting.DevApiUrl}?type={(os == OSType.IOS ? "ios" : "android")}&count={count}");
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    return responseBody;
                }
                else
                {
                    _logger.LogInformation($"GetDevs,{response.StatusCode}");
                    return null;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogInformation($"GetDevs,{ex.Message},{ex.StackTrace},{ex.InnerException.Message}");
                return null;
            }
            finally
            {
                _mutex_dev.Release();
            }
        }

        #endregion


        /// <summary>
        /// 秒针URL处理
        /// </summary>
        /// <param name="url"></param>
        /// <param name="ip"></param>
        /// <param name="userAgent"></param>
        /// <param name="param"></param>
        /// <param name="os"></param>
        /// <param name="dev"></param>
        /// <returns></returns>
        private string FormatUrlText(string url, string ip, string userAgent, JToken param, OSType os, JObject ipInfo, JToken dev = null)
        {

            if (param["huichuanip"] != null && param["huichuanip"].ToString().Equals("on") && !string.IsNullOrWhiteSpace(ip))
            {
                url = url.Replace("__IP__", ip);
            }
            if (param["huichuan"] != null && param["huichuan"].ToString().Equals("on"))
            {
                //__OS__//1位数字,取0~3。0表示Android，1表示iOS，2表示Windows Phone，3表示其他
                if (!setting.NoneOS)
                {
                    if (os == OSType.ANDROID)
                        url = url.Replace("__OS__", "0");
                    else if (os == OSType.IOS)
                        url = url.Replace("__OS__", "1");
                    else if (os == OSType.WINDOWS_PHONE)
                        url = url.Replace("__OS__", "2");
                    else
                        url = url.Replace("__OS__", "3");
                }

                if (os == OSType.IOS)
                {
                    if (setting.UsingIOSIMEI)
                    {
                        string imei = string.Empty;
                        if (dev != null && dev["imei"] != null)
                        {
                            imei = dev["imei"].ToString();
                        }
                        if (string.IsNullOrWhiteSpace(imei))
                        {
                            imei = DevMan.GetImei();
                        }
                        var imei_md5 = CommonHelper.MD5Hash(imei);
                        url = url.Replace("__IMEI__", imei_md5);
                    }

                    if (setting.UsingIOSMAC)
                    {
                        string mac = string.Empty;
                        if (dev != null && dev["mac"] != null)
                        {
                            mac = dev["mac"].ToString().ToUpper();
                        }
                        if (string.IsNullOrWhiteSpace(mac))
                        {
                            mac = CommonHelper.GetRandomMacAddress();
                        }

                        var macmd51 = CommonHelper.MD5Hash(mac);
                        var macmd52 = CommonHelper.MD5Hash(mac.Replace(":", ""));

                        url = url.Replace("__MAC1__", macmd51);
                        url = url.Replace("__MAC__", macmd52);
                    }

                    string idfa = string.Empty;
                    if (dev != null && dev["idfa"] != null)
                    {
                        idfa = dev["idfa"].ToString().ToUpper();
                    }
                    if (string.IsNullOrWhiteSpace(idfa))
                    {
                        idfa = DevMan.GetIdfa().ToUpper();
                    }
                    url = url.Replace("__IDFA__", idfa);

                }
                else if (os == OSType.ANDROID)
                {


                    string mac = string.Empty;
                    if (dev != null && dev["mac"] != null)
                    {
                        mac = dev["mac"].ToString().ToUpper();
                    }
                    if (string.IsNullOrWhiteSpace(mac))
                    {
                        mac = CommonHelper.GetRandomMacAddress();
                    }

                    var macmd51 = CommonHelper.MD5Hash(mac);
                    var macmd52 = CommonHelper.MD5Hash(mac.Replace(":", ""));

                    url = url.Replace("__MAC1__", macmd51);
                    url = url.Replace("__MAC__", macmd52);



                    string imei = string.Empty;
                    if (dev != null && dev["imei"] != null)
                    {
                        imei = dev["imei"].ToString();
                    }
                    if (string.IsNullOrWhiteSpace(imei))
                    {
                        imei = DevMan.GetImei();
                    }
                    var imei_md5 = CommonHelper.MD5Hash(imei);
                    url = url.Replace("__IMEI__", imei_md5);



                    string androidId = string.Empty;
                    if (dev != null && dev["android_id"] != null)
                    {
                        androidId = dev["android_id"].ToString().ToUpper();
                    }
                    if (string.IsNullOrWhiteSpace(androidId))
                    {
                        androidId = DevMan.GetAndroidId().ToUpper();
                    }
                    var androidId_md5 = CommonHelper.MD5Hash(androidId);

                    url = url.Replace("__ANDROIDID__", androidId_md5);
                    url = url.Replace("__ANDROIDID1__", androidId);


                    if (dev != null && dev["oaid"] != null)
                    {
                        var oaid = dev["oaid"].ToString();
                        if (!string.IsNullOrWhiteSpace(oaid))
                        {
                            url = url.Replace("__OAID__", oaid);
                            var oaid_md5 = CommonHelper.MD5Hash(oaid);
                            url = url.Replace("__OAID1__", oaid_md5);
                        }
                    }
                }
                if (url.Contains("[timestamp]"))
                    url = url.Replace("[timestamp]", CommonHelper.UnixTimeNowSecond().ToString());
                if (url.Contains("__TS__"))
                    url = url.Replace("__TS__", CommonHelper.UnixTimeNowSecond().ToString());

            }
            return url;
        }

        /// <summary>
        /// 国双URL
        /// </summary>
        /// <param name="url"></param>
        /// <param name="ip"></param>
        /// <param name="userAgent"></param>
        /// <param name="param"></param>
        /// <param name="os"></param>
        /// <param name="ipInfo"></param>
        /// <param name="dev"></param>
        /// <returns></returns>
        private string GridSumissector(string url, string ip, string userAgent, JToken param, OSType os, JObject ipInfo, JToken dev = null)
        {
            //https://i.gridsumdissector.com/v/?gscmd=impress&gid=gad_155_Y9RU7SQW&os=__OS__&if=__IDFA__&oid=__OPENUDID__&aid=__ANDROIDID__&im=__IMEI__&oa=__OAID__&m=__MAC__&ip=__IP__&ts=__TS__&did=__DUID__&aaid=__AAID__&uid=__UDID__&odin=__ODIN__&ua=__UA__&lbs=__LBS__

            if (param["huichuanip"] != null && param["huichuanip"].ToString().Equals("on") && string.IsNullOrWhiteSpace(ip))
            {
                url = url.Replace("__IP__", ip);
            }
            if (param["huichuan"] != null && param["huichuan"].ToString().Equals("on"))
            {
                if (os == OSType.ANDROID)
                    url = url.Replace("__OS__", "0");
                else if (os == OSType.IOS)
                    url = url.Replace("__OS__", "1");
                else if (os == OSType.WINDOWS_PHONE)
                    url = url.Replace("__OS__", "2");
                else
                    url = url.Replace("__OS__", "3");





                if (os == OSType.IOS)
                {
                    if (setting.UsingIOSIMEI)
                    {
                        string imei = string.Empty;
                        if (dev != null && dev["imei"] != null)
                        {
                            imei = dev["imei"].ToString().ToLower();
                        }
                        if (string.IsNullOrWhiteSpace(imei))
                        {
                            imei = DevMan.GetImei().ToLower();
                        }

                        var imei_md5 = CommonHelper.MD5Hash(imei);
                        url = url.Replace("__IMEI__", imei_md5);
                    }

                    if (setting.UsingIOSMAC)
                    {
                        string mac = string.Empty;
                        if (dev != null && dev["mac"] != null)
                        {
                            mac = dev["mac"].ToString().ToUpper();
                        }
                        if (string.IsNullOrWhiteSpace(mac))
                        {
                            mac = CommonHelper.GetRandomMacAddress().ToUpper();
                        }
                        var macmd5 = CommonHelper.MD5Hash(mac);
                        url = url.Replace("__MAC__", CommonHelper.MD5Hash(macmd5));
                    }

                    string idfa = string.Empty;
                    if (dev != null && dev["idfa"] != null)
                    {
                        idfa = dev["idfa"].ToString().ToUpper();
                    }
                    if (string.IsNullOrWhiteSpace(idfa))
                    {
                        idfa = DevMan.GetIdfa().ToUpper();
                    }
                    url = url.Replace("__IDFA__", idfa);

                }
                else if (os == OSType.ANDROID)
                {


                    string imei = string.Empty;
                    if (dev != null && dev["imei"] != null)
                    {
                        imei = dev["imei"].ToString().ToLower();
                    }
                    if (string.IsNullOrWhiteSpace(imei))
                    {
                        imei = DevMan.GetImei().ToLower();
                    }

                    var imei_md5 = CommonHelper.MD5Hash(imei);
                    url = url.Replace("__IMEI__", imei_md5);


                    string mac = string.Empty;
                    if (dev != null && dev["mac"] != null)
                    {
                        mac = dev["mac"].ToString().ToUpper();
                    }
                    if (string.IsNullOrWhiteSpace(mac))
                    {
                        mac = CommonHelper.GetRandomMacAddress().ToUpper();
                    }
                    var macmd5 = CommonHelper.MD5Hash(mac);
                    url = url.Replace("__MAC__", CommonHelper.MD5Hash(macmd5));


                    string androidId = string.Empty;
                    if (dev != null && dev["android_id"] != null)
                    {
                        androidId = dev["android_id"].ToString().ToUpper();
                    }
                    if (string.IsNullOrWhiteSpace(androidId))
                    {
                        androidId = DevMan.GetAndroidId().ToUpper();
                    }
                    var androidId_md5 = CommonHelper.MD5Hash(androidId);
                    url = url.Replace("__ANDROIDID__", androidId_md5);
                }
                url = url.Replace("__TS__", CommonHelper.UnixTimeNow().ToString());
            }
            return url;
        }

        /// <summary>
        /// 深演广告
        /// </summary>
        /// <param name="url"></param>
        /// <param name="ip"></param>
        /// <param name="userAgent"></param>
        /// <param name="param"></param>
        /// <param name="os"></param>
        /// <param name="ipInfo"></param>
        /// <param name="dev"></param>
        /// <returns></returns>
        private string FormatUrl_ipinyou(string url, string ip, string userAgent, JToken param, OSType os, JObject ipInfo, JToken dev = null)
        {
            //http://vt.ipinyou.com/IinK3066gI5vwOkVZ-.IcX5R_.sWLZhPIi7pbkvccpO3kUXEe5DrZWFlJbrDuAyySZ_T8kzY9epmcXfrEv_RzyW4f.txHx607mbPPtH8cJVys8k_?tmp=[timestamp]&mob_idfa=[idfa]&mob_imei=[imei]&mob_android=[androidid]&mob_os=[os]&mob_oaid=[oaid]&mob_mac=[mac]

            if (param["huichuan"] != null && param["huichuan"].ToString().Equals("on"))
            {
                url = url.Replace("[timestamp]", CommonHelper.UnixTimeNow().ToString());
                string os_val = string.Empty;
                if (os == OSType.ANDROID)
                    os_val = "0";

                else if (os == OSType.IOS)
                    os_val = "1";
                else if (os == OSType.WINDOWS_PHONE)
                    os_val = "2";
                else
                    os_val = "3";

                url = url.Replace("[os]", os_val);
                url = url.Replace("__OS__", os_val);

                if (os == OSType.IOS)
                {
                    if (setting.UsingIOSIMEI)
                    {
                        string imei = string.Empty;
                        if (dev != null && dev["imei"] != null)
                        {
                            imei = dev["imei"].ToString().ToLower();
                        }
                        if (string.IsNullOrWhiteSpace(imei))
                        {
                            imei = DevMan.GetImei().ToLower();
                        }
                        var imei_md5 = CommonHelper.MD5Hash(imei);
                        url = url.Replace("[imei]", imei_md5);
                        url = url.Replace("__IMEI__", imei_md5);
                    }

                    if (setting.UsingIOSMAC)
                    {
                        string mac = string.Empty;
                        if (dev != null && dev["mac"] != null)
                        {
                            mac = dev["mac"].ToString().ToUpper();
                        }
                        if (string.IsNullOrWhiteSpace(mac))
                        {
                            mac = CommonHelper.GetRandomMacAddress().ToUpper();
                        }
                        var macmd5 = CommonHelper.MD5Hash(mac.Replace(":", ""));
                        url = url.Replace("[mac]", macmd5);
                        url = url.Replace("__MAC__", macmd5);
                    }

                    string idfa = string.Empty;
                    if (dev != null && dev["idfa"] != null)
                    {
                        idfa = dev["idfa"].ToString().ToUpper();
                    }
                    if (string.IsNullOrWhiteSpace(idfa))
                    {
                        idfa = DevMan.GetIdfa().ToUpper();
                    }
                    url = url.Replace("[idfa]", idfa);
                    url = url.Replace("__IDFA__", idfa);
                }

                else if (os == OSType.ANDROID)
                {

                    string imei = string.Empty;
                    if (dev != null && dev["imei"] != null)
                    {
                        imei = dev["imei"].ToString().ToLower();
                    }
                    if (string.IsNullOrWhiteSpace(imei))
                    {
                        imei = DevMan.GetImei().ToLower();
                    }
                    var imei_md5 = CommonHelper.MD5Hash(imei);
                    url = url.Replace("[imei]", imei_md5);
                    url = url.Replace("__IMEI__", imei_md5);



                    string mac = string.Empty;
                    if (dev != null && dev["mac"] != null)
                    {
                        mac = dev["mac"].ToString().ToUpper();
                    }
                    if (string.IsNullOrWhiteSpace(mac))
                    {
                        mac = CommonHelper.GetRandomMacAddress().ToUpper();
                    }
                    var macmd5 = CommonHelper.MD5Hash(mac.Replace(":", ""));
                    url = url.Replace("[mac]", macmd5);
                    url = url.Replace("__MAC__", macmd5);




                    string androidId = string.Empty;
                    if (dev != null && dev["android_id"] != null)
                    {
                        androidId = dev["android_id"].ToString().ToLower();
                    }
                    if (string.IsNullOrWhiteSpace(androidId))
                    {
                        androidId = DevMan.GetAndroidId().ToLower();
                    }
                    var androidId_md5 = CommonHelper.MD5Hash(androidId);
                    url = url.Replace("[androidid]", androidId_md5);
                    url = url.Replace("__ANDROIDID__", androidId_md5);
                }
            }
            return url;
        }


        public static string ReplacePlaceholderValues(string url, Dictionary<string, string> valueReplacements)
        {
            var uri = new Uri(url);

            // 保留路径部分
            string fragment = uri.Fragment;
            string baseUrl = uri.GetLeftPart(UriPartial.Path);

            // 解析现有参数
            NameValueCollection queryParams = HttpUtility.ParseQueryString(uri.Query);

            // 遍历所有参数值
            foreach (string key in queryParams.AllKeys)
            {
                string value = queryParams[key];
                if (value != null && valueReplacements.ContainsKey(value))
                {
                    queryParams[key] = valueReplacements[value];
                }
            }

            // 重新组合 URL
            string newQuery = queryParams.Count > 0 ? "?" + queryParams.ToString() : string.Empty;
            return baseUrl + newQuery + fragment;
        }





        private string FormatUrl_mafengwo(string url, string ip, string userAgent, JToken param, OSType os, JObject ipInfo, JToken dev = null)
        {
            //http://vt.ipinyou.com/IinK3066gI5vwOkVZ-.IcX5R_.sWLZhPIi7pbkvccpO3kUXEe5DrZWFlJbrDuAyySZ_T8kzY9epmcXfrEv_RzyW4f.txHx607mbPPtH8cJVys8k_?tmp=[timestamp]&mob_idfa=[idfa]&mob_imei=[imei]&mob_android=[androidid]&mob_os=[os]&mob_oaid=[oaid]&mob_mac=[mac]
            //https://admonitor.mafengwo.cn/flux/i.gif?open_udid=__MFWUDID__&idfa=__IDFA__&imei=__IMEI__&oaid=__OAID__&os=__OS__&platform=1APP&pos_key=app_sales_banner_gd&ad_id=1020997&mate_id=634081&uid=35758&mate_type=230&source=__MFWSOURCE__&put_type=gdcpm&cycle_number=&contract=MFWPGS202507029&target=https%3A%2F%2Fg.cn.miaozhen.com%2Fx%2Fk%3D2466417%26p%3D8tSzf%26rt%3D2%26pro%3Ds%26dx%3D__IPDX__%26ns%3D__IP__%26ni%3D__IESID__%26v%3D__LOC__%26xa%3D__ADPLATFORM__%26tr%3D__REQUESTID__%26vg%3D__AUTOPLAY__%26nh%3D__AUTOREFRESH__%26mo%3D__OS__%26m0%3D__OPENUDID__%26m0a%3D__DUID__%26m1%3D__ANDROIDID1__%26m1a%3D__ANDROIDID__%26m2%3D__IMEI__%26m4%3D__AAID__%26m5%3D__IDFA__%26m6%3D__MAC1__%26m6a%3D__MAC__%26m11%3D__OAID__%26m14%3D__CAID__%26m5a%3D__IDFV__%26mn%3D__ANAME__%26m5b%3D__IDFA1__%26m11a%3D__OAID1__%26m14a%3D__CAID1__%26gc%3D__GCID__%26o%3D

            if (param["huichuan"] != null && param["huichuan"].ToString().Equals("on"))
            {

                var updates = new Dictionary<string, string>();
                var ts = CommonHelper.UnixTimeNow();

                updates.Add("[timestamp]", ts.ToString());
                updates.Add("__TS__", ts.ToString());

                string os_val = string.Empty;
                if (os == OSType.ANDROID)
                    os_val = "0";
                else if (os == OSType.IOS)
                    os_val = "1";
                else if (os == OSType.WINDOWS_PHONE)
                    os_val = "2";
                else
                    os_val = "3";

                updates.Add("[os]", os_val);
                updates.Add("__OS__", os_val);


                if (os == OSType.IOS)
                {
                    if (setting.UsingIOSIMEI)
                    {
                        string imei = string.Empty;
                        if (dev != null && dev["imei"] != null)
                        {
                            imei = dev["imei"].ToString().ToLower();
                        }
                        if (string.IsNullOrWhiteSpace(imei))
                        {
                            imei = DevMan.GetImei().ToLower();
                        }
                        var imei_md5 = CommonHelper.MD5Hash(imei);
                        updates.Add("[imei]", imei_md5);
                        updates.Add("__IMEI__", imei_md5);
                    }


                    if (setting.UsingIOSMAC)
                    {
                        string mac = string.Empty;
                        if (dev != null && dev["mac"] != null)
                        {
                            mac = dev["mac"].ToString().ToUpper();
                        }
                        if (string.IsNullOrWhiteSpace(mac))
                        {
                            mac = CommonHelper.GetRandomMacAddress().ToUpper();
                        }
                        var macmd5 = CommonHelper.MD5Hash(mac.Replace(":", ""));

                        updates.Add("[mac]", macmd5);
                        updates.Add("__MAC__", macmd5);
                    }

                    string idfa = string.Empty;
                    if (dev != null && dev["idfa"] != null)
                    {
                        idfa = dev["idfa"].ToString().ToUpper();
                    }
                    if (string.IsNullOrWhiteSpace(idfa))
                    {
                        idfa = DevMan.GetIdfa().ToUpper();
                    }

                    updates.Add("[idfa]", idfa);
                    updates.Add("__IDFA__", idfa);
                }

                else if (os == OSType.ANDROID)
                {

                    string imei = string.Empty;
                    if (dev != null && dev["imei"] != null)
                    {
                        imei = dev["imei"].ToString().ToLower();
                    }
                    if (string.IsNullOrWhiteSpace(imei))
                    {
                        imei = DevMan.GetImei().ToLower();
                    }
                    var imei_md5 = CommonHelper.MD5Hash(imei);
                    updates.Add("[imei]", imei_md5);
                    updates.Add("__IMEI__", imei_md5);

                    string mac = string.Empty;
                    if (dev != null && dev["mac"] != null)
                    {
                        mac = dev["mac"].ToString().ToUpper();
                    }
                    if (string.IsNullOrWhiteSpace(mac))
                    {
                        mac = CommonHelper.GetRandomMacAddress().ToUpper();
                    }
                    var macmd5 = CommonHelper.MD5Hash(mac.Replace(":", ""));

                    updates.Add("[mac]", macmd5);
                    updates.Add("__MAC__", macmd5);


                    string androidId = string.Empty;
                    if (dev != null && dev["android_id"] != null)
                    {
                        androidId = dev["android_id"].ToString().ToLower();
                    }
                    if (string.IsNullOrWhiteSpace(androidId))
                    {
                        androidId = DevMan.GetAndroidId().ToLower();
                    }
                    var androidId_md5 = CommonHelper.MD5Hash(androidId);

                    updates.Add("[androidid]", androidId_md5);
                    updates.Add("__ANDROIDID__", androidId_md5);

                    updates.Add("[androidid1]", androidId);
                    updates.Add("__ANDROIDID1__", androidId);



                    if (dev != null && dev["oaid"] != null)
                    {
                        var oaid = dev["oaid"].ToString();
                        if (!string.IsNullOrWhiteSpace(oaid))
                        {
                            updates.Add("[oaid]", oaid);
                            updates.Add("__OAID__", oaid);
                            var oaid_md5 = CommonHelper.MD5Hash(oaid);
                            updates.Add("[oaid1]", oaid_md5);
                            updates.Add("__OAID1__", oaid_md5);
                        }

                    }

                }

                url = ReplacePlaceholderValues(url, updates);
            }



            return url;
        }





        #region ua
        private Dictionary<int, List<string>> ua_dic = new Dictionary<int, List<string>>();
        private void LoadUserAgentData()
        {
            Task.Run(() =>
            {
                for (int uaIndex = 1; uaIndex < 10; uaIndex++)
                {
                    var filePath = System.IO.Path.GetFullPath($"Data/ua_{uaIndex}.txt");
                    if (System.IO.File.Exists(filePath))
                    {
                        var values = System.IO.File.ReadAllLines(filePath).ToList();
                        ua_dic.Add(uaIndex, values);
                    }
                }
            });
        }
        /// <summary>
        /// 获取有效的UserAgent
        /// </summary>
        /// <param name="uaIndex"></param>
        /// <returns></returns>
        private string GetUserAgent(int uaIndex)
        {
            //1:ANDROID
            //2: 移动安桌微信
            //3:移动IPAD
            //4:移动Iphone
            //5:PC火狐
            //6:PCIE
            //7:PC谷歌
            //8:PC苹果
            //9:移动苹果微信
            List<string> values;
            if (ua_dic.TryGetValue(uaIndex, out values))
            {
                return values[new Random().Next(0, values.Count() - 1)];
            }
            values = ua_dic[new Random().Next(1, ua_dic.Count())];
            return values[new Random().Next(0, values.Count() - 1)];
        }
        #endregion

        #region IP

        private static HttpClient _client = new HttpClient();

        private static JArray region_1 = (JArray)JsonConvert.DeserializeObject(Properties.Resources.region_1);
        private static JArray region_2 = (JArray)JsonConvert.DeserializeObject(Properties.Resources.region_2);
        private static JArray region_3 = (JArray)JsonConvert.DeserializeObject(Properties.Resources.region_3);
        private static JArray region_4_1 = (JArray)JsonConvert.DeserializeObject(Properties.Resources.region_4_1);
        private static JArray region_4_2 = (JArray)JsonConvert.DeserializeObject(Properties.Resources.region_4_2);
        private static JArray region_51daili = (JArray)JsonConvert.DeserializeObject(Properties.Resources.region_51daili);
        private static JArray region_shenlong = (JArray)JsonConvert.DeserializeObject(Properties.Resources.region_shenlong);


        private static SemaphoreSlim _mutex = new SemaphoreSlim(1);
        private static ConcurrentDictionary<string, ConcurrentQueue<string>> ipOfList = new ConcurrentDictionary<string, ConcurrentQueue<string>>();
        private async Task<string> GetIps(JObject job, int count = 1)
        {
            string p = string.Empty;
            string c = string.Empty;
            var url = setting.AllIpApiUrl;


            if (url.Contains("51daili.com"))
            {
                #region 51daili.com
                //http://bapi.51daili.com/traffic/getip?linePoolIndex=1&packid=12&time=2&qty=12&port=1&format=txt&usertype=17&uid=39905
                //if (query["format"] != null && query["format"].ToString().Equals("json"))
                //    format = IPFormat.JSON;
                //else
                //    format = IPFormat.TXT;


                if (count > 1)
                {
                    if (Regex.IsMatch(url, @"qty=[\d]*"))
                        url = Regex.Replace(url, @"qty=[\d]*", $"qty={count}");
                    else
                        url = url += $"&qty={count}";

                    //if(url.Contains("format="))
                    //    url = Regex.Replace(url, "format=txt", "format=json");
                    //else
                    //    url = url += $"&format=json";
                }


                if (job["address"] != null && !string.IsNullOrEmpty(job["address"].ToString()) && !job["address"].ToString().Equals("全部"))
                {
                    var address_list = job["address"].ToString().Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    var address = address_list[Math.Abs(Guid.NewGuid().GetHashCode()) % address_list.Length].Split(':');
                    var area_addr = string.Empty;
                    string area = string.Empty;
                    if (address.Length > 1)
                    {
                        var m1 = Regex.Match(address[0], @"\w+");
                        if (m1.Success)
                        {
                            p = m1.Value;
                            var area_prov = region_51daili.Where(w => w["provinceName"].ToString().Contains(m1.Value)).OrderByDescending(o => Convert.ToInt64(o["provinceCode"].ToString())).FirstOrDefault();
                            if (area_prov != null)
                            {
                                var m2 = Regex.Match(address[1], @"\w+");
                                if (m2.Success)
                                {
                                    c = m2.Value;
                                    var area_city = area_prov["mallCityList"].FirstOrDefault(w => w["cityName"].ToString().Contains(m2.Value));
                                    if (area_city != null)
                                    {
                                        if (Regex.IsMatch(url, @"regionCode=[\w]*[^&]?"))
                                            url = Regex.Replace(url, @"regionCode=[\w]*[^&]?", $"regionCode={area_city["cityCode"]}");
                                        else
                                            url = url += $"&regionCode={area_city["cityCode"]}";
                                    }
                                    else
                                    {
                                        if (Regex.IsMatch(url, @"regionCode=[\w]*[^&]?"))
                                            url = Regex.Replace(url, @"area=[\w]*[^&]?", $"regionCode={area_prov["provinceCode"]}");
                                        else
                                            url = url += $"&regionCode={area_prov["provinceCode"]}";
                                    }
                                }
                                else
                                {
                                    if (Regex.IsMatch(url, @"regionCode=[\w]*[^&]?"))
                                        url = Regex.Replace(url, @"area=[\w]*[^&]?", $"regionCode={area_prov["provinceCode"]}");
                                    else
                                        url = url += $"&regionCode={area_prov["provinceCode"]}";
                                }
                            }
                        }
                    }
                    else
                    {
                        var m1 = Regex.Match(address[0], @"\w+");
                        if (m1.Success)
                        {
                            p = m1.Value;
                            var area_prov = region_51daili.Where(w => w["provinceName"].ToString().Contains(m1.Value)).OrderByDescending(o => Convert.ToInt64(o["provinceCode"].ToString())).FirstOrDefault();
                            if (area_prov != null)
                            {
                                if (Regex.IsMatch(url, @"regionCode=[\w]*[^&]?"))
                                    url = Regex.Replace(url, @"regionCode=[\w]*[^&]?", $"regionCode={area_prov["provinceCode"]}");
                                else
                                    url = url += $"&regionCode={area_prov["provinceCode"]}";
                            }
                        }
                    }
                }
                #endregion
            }
            else if (url.Contains("shenlongip.com"))
            {
                #region shenlongip.com
                //rip=1:真实IP
                //http://api.shenlongip.com/ip?key=pjr1xjh4&area=310100,320100,320200,320300,320400,320500,320600,320700,320800,320900,321000,321100,321200,321300&protocol=1&mr=1&pattern=txt&count=1&sign=e207c36f5687a57e9802c8190f428ea4
                //if (query["format"] != null && query["format"].ToString().Equals("json"))
                //    format = IPFormat.JSON;
                //else
                //    format = IPFormat.TXT;

                if (setting.RealIp)
                {

                    if (Regex.IsMatch(url, @"pattern=\w+"))
                        url = Regex.Replace(url, @"pattern=\w+", $"pattern=json");
                    else
                        url = url += $"&pattern=json";


                    if (Regex.IsMatch(url, @"rip=\d+"))
                        url = Regex.Replace(url, @"rip=\d+", $"rip=1");
                    else
                        url = url += $"&rip=1";
                }
                else
                {
                    if (Regex.IsMatch(url, @"pattern=\w+"))
                        url = Regex.Replace(url, @"pattern=\w+", $"pattern=txt");
                    if (Regex.IsMatch(url, @"rip=\d+"))
                        url = Regex.Replace(url, @"rip=\d+", $"rip=0");
                }



                if (count > 1)
                {
                    if (Regex.IsMatch(url, @"count=[\d]*"))
                        url = Regex.Replace(url, @"count=[\d]*", $"count={count}");
                    else
                        url = url += $"&count={count}";
                }
                else if (job["address"] != null && !string.IsNullOrEmpty(job["address"].ToString()) && !job["address"].ToString().Equals("全部"))
                {
                    var address_list = job["address"].ToString().Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    List<string> area_codes = new List<string>();
                    foreach (var address in address_list)
                    {
                        if (address.Contains(":"))
                        {
                            var address_values = address.Split(':');
                            var m1 = Regex.Match(address_values[0], @"\w+");
                            if (m1.Success)
                            {
                                p = m1.Value;
                                var m2 = Regex.Match(address_values[1], @"\w+");
                                if (m2.Success)
                                {
                                    c = m2.Value;
                                    var areas = region_shenlong.Where(w => w["name"].ToString().Contains(m2.Value)).ToList();
                                    if (areas != null && areas.Count() > 0)
                                    {
                                        var area_code = string.Join(",", areas.Select(s => s["code"].ToString()));
                                        area_codes.Add(area_code);
                                    }
                                }
                                else
                                {
                                    var areas = region_shenlong.Where(w => w["name"].ToString().Contains(m1.Value)).ToList();
                                    if (areas != null && areas.Count() > 0)
                                    {
                                        var area_code = string.Join(",", areas.Select(s => s["code"].ToString()));
                                        area_codes.Add(area_code);
                                    }
                                }
                            }
                        }
                        else
                        {
                            var m1 = Regex.Match(address, @"\w+");
                            if (m1.Success)
                            {
                                p = m1.Value;
                                var areas = region_shenlong.Where(w => w["name"].ToString().Contains(m1.Value));
                                if (areas != null && areas.Count() > 0)
                                {
                                    var area_code = string.Join(",", areas.Select(s => s["code"].ToString()));
                                    area_codes.Add(area_code);
                                }
                            }
                        }
                    }

                    //if (address_list.Count() > 1)
                    //{
                    //    p = string.Empty;
                    //    c = string.Empty;
                    //}

                    if (area_codes.Count() > 0)
                    {
                        var all_area_code = string.Join(",", area_codes);
                        if (Regex.IsMatch(url, @"area=[\d,]+"))
                            url = Regex.Replace(url, @"area=[\d,]+", $"area={all_area_code}");
                        else
                            url = url += $"&area={all_area_code}";
                    }

                    #region 地区
                    //var address = address_list[Math.Abs(Guid.NewGuid().GetHashCode()) % address_list.Length].Split(':');
                    //var area_addr = string.Empty;
                    //string area = string.Empty;
                    //if (address.Length > 1)
                    //{
                    //    var m1 = Regex.Match(address[0], @"\w+");
                    //    if (m1.Success)
                    //    {
                    //        p = m1.Value;
                    //        var m2 = Regex.Match(address[1], @"\w+");
                    //        if (m2.Success)
                    //        {
                    //            c = m2.Value;
                    //            var areas = region_shenlong.Where(w => w["name"].ToString().Contains(m2.Value)).ToList();
                    //            if (areas != null && areas.Count() > 0)
                    //            {
                    //                var area_codes = string.Join(",", areas.Select(s => s["code"].ToString()));
                    //                if (Regex.IsMatch(url, @"area=[\d,]+"))
                    //                    url = Regex.Replace(url, @"area=[\d,]+", $"area={area_codes}");
                    //                else
                    //                    url = url += $"&area={area_codes}";
                    //            }
                    //        }
                    //        else
                    //        {
                    //            var areas = region_shenlong.Where(w => w["name"].ToString().Contains(m1.Value)).ToList();
                    //            if (areas != null && areas.Count() > 0)
                    //            {
                    //                var area_codes = string.Join(",", areas.Select(s => s["code"].ToString()));
                    //                if (Regex.IsMatch(url, @"area=[\d,]+"))
                    //                    url = Regex.Replace(url, @"area=[\d,]+", $"area={area_codes}");
                    //                else
                    //                    url = url += $"&area={area_codes}";
                    //            }
                    //        }
                    //    }
                    //}
                    //else
                    //{
                    //    var m1 = Regex.Match(address[0], @"\w+");
                    //    if (m1.Success)
                    //    {
                    //        p = m1.Value;
                    //        var areas = region_shenlong.Where(w => w["name"].ToString().Contains(m1.Value));
                    //        if (areas != null && areas.Count() > 0)
                    //        {
                    //            var area_codes = string.Join(",", areas.Select(s => s["code"].ToString()));
                    //            if (Regex.IsMatch(url, @"area=[\d,]+"))
                    //                url = Regex.Replace(url, @"area=[\d,]+", $"area={area_codes}");
                    //            else
                    //                url = url += $"&area={area_codes}";
                    //        }
                    //    }
                    //}
                    #endregion

                }
                #endregion
            }
            else if (url.Contains("113.96.182.17") || url.Contains("113.96"))
            {
                if (count > 1)
                {
                    if (Regex.IsMatch(url, @"number=[\d]*"))
                        url = Regex.Replace(url, @"number=[\d]*", $"number={count}");
                    else
                        url = url += $"&number={count}";
                }


                try
                {
                    if (job["address"] != null && !string.IsNullOrEmpty(job["address"].ToString()) && !job["address"].ToString().Equals("全部"))
                    {
                        var addrs = job["address"].ToString().Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                        var address = addrs[Math.Abs(Guid.NewGuid().GetHashCode()) % addrs.Length].Split(':');
                        var m1 = Regex.Match(address[0], @"\w+");
                        string area = string.Empty;
                        if (m1.Success)
                        {
                            p = m1.Value;
                            var p_datas = region_3.Where(w => w["province"].ToString().Contains(m1.Value));
                            if (p_datas != null && p_datas.Count() > 0)
                            {
                                var province = p_datas.FirstOrDefault()?["code"].ToString();
                                if (province != null)
                                {
                                    area = province.Substring(0, 2) + string.Join("", province.ToArray().Skip(2).ToList().Select(s => "0"));
                                    if (address.Length > 1)
                                    {
                                        var m2 = Regex.Match(address[1], @"\w+");
                                        if (m2.Success)
                                        {
                                            c = m2.Value;
                                            var area2 = p_datas.Where(w => w["city"].ToString().Contains(m2.Value)).FirstOrDefault()?["code"].ToString();
                                            if (!string.IsNullOrWhiteSpace(area2))
                                            {
                                                area = area2;
                                            }
                                        }
                                    }
                                    if (Regex.IsMatch(url, @"area=[\w]*[^&]?"))
                                        url = Regex.Replace(url, @"area=[\w]*[^&]?", $"area={area}");
                                    else
                                        url = url += $"&area={area}";

                                }





                            }

                        }
                    }
                }
                catch (Exception ex)
                {
                    LogWriteLine($"{ex.Message}");
                    return null;
                }

            }
            else if (url.Contains("xiaoxiongcloud.com") || url.Contains("find.dl321.net"))
            {
                if (count > 1)
                {
                    if (Regex.IsMatch(url, @"count=[\d]*"))
                        url = Regex.Replace(url, @"count=[\d]*", $"count={count}");
                    else
                        url = url += $"&count={count}";
                }

                try
                {
                    if (job["address"] != null && !string.IsNullOrEmpty(job["address"].ToString()) && !job["address"].ToString().Equals("全部"))
                    {
                        var addrs = job["address"].ToString().Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                        var address = addrs[Math.Abs(Guid.NewGuid().GetHashCode()) % addrs.Length].Split(':');
                        var m1 = Regex.Match(address[0], @"\w+");
                        string area = string.Empty;
                        if (m1.Success)
                        {
                            p = m1.Value;
                            var province = region_4_1.Where(w => w["province"].ToString().Contains(m1.Value)).FirstOrDefault()?["code"];
                            if (province != null)
                            {
                                if (Regex.IsMatch(url, @"province=[\w]*[^&]?"))
                                    url = Regex.Replace(url, @"province=[\w]*[^&]?", $"province={province}");
                                else
                                    url = url += $"&province={province}";
                            }
                            if (address.Length > 1)
                            {
                                var m2 = Regex.Match(address[1], @"\w+");
                                if (m2.Success)
                                {
                                    c = m2.Value;
                                    var city = region_4_2.Where(w => w["city"].ToString().Contains(c)).FirstOrDefault()?["code"];
                                    if (city != null)
                                    {
                                        if (Regex.IsMatch(url, @"city=[\w]*[^&]?"))
                                            url = Regex.Replace(url, @"city=[\w]*[^&]?", $"city={city}");
                                        else
                                            url = url += $"&city={city}";
                                    }

                                }
                            }

                        }
                    }
                }
                catch (Exception ex)
                {
                    LogWriteLine($"{ex.Message}");
                    return null;
                }

            }
            else if (url.Contains("api.hailiangip.com") || url.Contains("111.73.45.100") || url.Contains("47.97.20.179") || url.Contains("api.test.myipproxy.com"))
            {
                if (count > 1)
                {
                    if (Regex.IsMatch(url, @"num=[\d]*"))
                        url = Regex.Replace(url, @"num=[\d]*", $"num={count}");
                    else
                        url = url += $"&num={count}";
                }
                if (setting.RealIp)
                {
                    if (Regex.IsMatch(url, @"dataType=[\d]*"))
                        url = Regex.Replace(url, @"dataType=[\d]*", $"dataType=0");
                    else
                        url = url += $"&dataType=0";
                }
                else
                {
                    if (Regex.IsMatch(url, @"dataType=[\d]*"))
                        url = Regex.Replace(url, @"dataType=[\d]*", $"dataType=1");
                    else
                        url = url += $"&dataType=1";
                }
                try
                {

                    if (job["address"] != null && !string.IsNullOrEmpty(job["address"].ToString()) && !job["address"].ToString().Equals("全部"))
                    {
                        var addrs = job["address"].ToString().Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                        var address = addrs[Math.Abs(Guid.NewGuid().GetHashCode()) % addrs.Length].Split(':');
                        var m1 = Regex.Match(address[0], @"\d+");
                        string pid = string.Empty, cid;
                        if (m1.Success)
                        {
                            pid = m1.Value;
                            if (Regex.IsMatch(url, @"pid=[\d]*"))
                                url = Regex.Replace(url, @"pid=[\d]*", $"pid={pid}");
                            else
                                url = url += $"&pid={pid}";
                        }
                        if (address.Count() > 1)
                        {
                            var m2 = Regex.Match(address[1], @"\d+");
                            if (m2.Success)
                            {
                                cid = m2.Value;
                                if (!string.IsNullOrWhiteSpace(pid) && !pid.Equals(cid))
                                {
                                    if (Regex.IsMatch(url, @"cid=[\d]*"))
                                        url = Regex.Replace(url, @"cid=[\d]*", $"cid={cid}");
                                    else
                                        url = url += $"&cid={cid}";
                                }

                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogWriteLine($"{ex.Message}");
                    return null;
                }
            }
            else if (url.Contains(".ipjldl.com"))
            {
                try
                {
                    if (job["address"] != null && !string.IsNullOrEmpty(job["address"].ToString()) && !job["address"].ToString().Equals("全部"))
                    {
                        var addrs = job["address"].ToString().Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                        var address = addrs[Math.Abs(Guid.NewGuid().GetHashCode()) % addrs.Length].Split(':');
                        var m1 = Regex.Match(address[0], @"\w+");
                        string pro, city;
                        if (m1.Success)
                        {
                            pro = m1.Value;
                            if (pro.Contains("宁夏"))
                            {
                                pro = "宁夏回族自治区";
                            }
                            else if (pro.Contains("天津"))
                            {
                                pro = "天津直辖市";
                            }
                            else if (pro.Contains("重庆"))
                            {
                                pro = "重庆直辖市";
                            }
                            else if (pro.Contains("上海"))
                            {
                                pro = "上海直辖市";
                            }
                            else if (pro.Contains("内蒙古"))
                            {
                                pro = "内蒙古自治区";
                            }
                            else
                            {
                                pro += "省";
                            }
                            url = Regex.Replace(url, @"pro=[\w]*[^&]?", $"pro={pro}");
                        }
                        var m2 = Regex.Match(address[1], @"\w+");

                        if (m2.Success)
                        {
                            city = m2.Value;
                            city += "市";
                            url = Regex.Replace(url, @"city=[\w]*[^&]?", $"city={city}");
                        }
                        Console.WriteLine(url);
                    }
                }
                catch (Exception ex)
                {
                    LogWriteLine(ex.Message);
                    return null;
                }
            }
            else if (url.Contains("api2.xkdaili.com") || url.Contains("81.68.214.41"))
            {

            }
            else if (url.Contains("napi.zhuzhaiip.com:9999") || url.Contains("222.186.43.209:501") || url.Contains("222.186.180.218:900"))
            {
                try
                {
                    if (job["address"] != null && !string.IsNullOrEmpty(job["address"].ToString()) && !job["address"].ToString().Equals("全部"))
                    {
                        var addrs = job["address"].ToString().Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                        var address = addrs[Math.Abs(Guid.NewGuid().GetHashCode()) % addrs.Length].Split(':');
                        var m1 = Regex.Match(address[0], @"\w+");
                        string province, city;
                        if (m1.Success)
                        {
                            p = m1.Value;
                            var p_datas = region_1.Where(w => w["province"].ToString().Contains(m1.Value));
                            if (p_datas != null && p_datas.Count() > 0)
                            {
                                province = p_datas.Where(w => w["city"].ToString().Equals("全部")).FirstOrDefault()?["code"].ToString();
                                if (province != null)
                                {
                                    if (Regex.IsMatch(url, @"province=[\w]*[^&]?"))
                                        url = Regex.Replace(url, @"province=[\w]*[^&]?", $"province={province}");
                                    else
                                        url = url += $"&province={province}";
                                }
                                if (address.Length > 1)
                                {
                                    var m2 = Regex.Match(address[1], @"\w+");
                                    if (m2.Success)
                                    {
                                        c = m2.Value;
                                        city = p_datas.Where(w => w["city"].ToString().Contains(m2.Value)).FirstOrDefault()?["code"].ToString();
                                        if (city != null && !province.Equals(city))
                                        {

                                            if (Regex.IsMatch(url, @"city=[\w]*[^&]?"))
                                                url = Regex.Replace(url, @"city=[\w]*[^&]?", $"city={city}");
                                            else
                                                url = url += $"&city={city}";
                                        }

                                    }
                                }


                            }

                        }
                    }
                }
                catch (Exception ex)
                {
                    LogWriteLine($"{ex.Message}");
                    return null;
                }
            }
            else if (url.Contains("pool.hufengyun.com") || url.Contains("8.136.221.72:8100"))
            {
                try
                {
                    if (job["address"] != null && !string.IsNullOrEmpty(job["address"].ToString()) && !job["address"].ToString().Equals("全部"))
                    {
                        var addrs = job["address"].ToString().Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                        var address = addrs[Math.Abs(Guid.NewGuid().GetHashCode()) % addrs.Length].Split(':');
                        var m1 = Regex.Match(address[0], @"\w+");
                        string province, city;
                        if (m1.Success)
                        {
                            p = m1.Value;
                            var p_datas = region_2.Where(w => w["province"].ToString().Contains(m1.Value));
                            if (p_datas != null && p_datas.Count() > 0)
                            {

                                province = p_datas.Where(w => w["city"].ToString().Equals(p)).FirstOrDefault()?["code"].ToString();
                                if (province != null)
                                {
                                    if (Regex.IsMatch(url, @"city=[\w]*[^&]?"))
                                        url = Regex.Replace(url, @"city=[\w]*[^&]?", $"city={province}");
                                    else
                                        url = url += $"&city={province}";
                                }
                                if (address.Length > 1)
                                {
                                    var m2 = Regex.Match(address[1], @"\w+");
                                    if (m2.Success)
                                    {
                                        c = m2.Value;
                                        city = p_datas.Where(w => w["city"].ToString().Contains(m2.Value)).FirstOrDefault()?["code"].ToString();
                                        if (city != null && !province.Equals(city))
                                        {

                                            if (Regex.IsMatch(url, @"city=[\w]*[^&]?"))
                                                url = Regex.Replace(url, @"city=[\w]*[^&]?", $"city={city}");
                                            else
                                                url = url += $"&city={city}";
                                        }

                                    }
                                }


                            }

                        }
                    }
                }
                catch (Exception ex)
                {
                    LogWriteLine($"{ex.Message}");
                    return null;
                }
            }

            try
            {
                await _mutex.WaitAsync();
                HttpResponseMessage response = await _client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"获取IP[{job["id"]}]:{job["address"]},{url},{responseBody}");

                    if (url.Contains("113.96.182.17"))
                    {
                        if (responseBody.Contains("错误") || responseBody.Contains("余额不足"))
                        {
                            return responseBody;
                        }
                        var ipjson = (JObject)JsonConvert.DeserializeObject(responseBody.Trim());
                        var jo = new JObject();
                        StringBuilder buf = new StringBuilder();
                        buf.Append(ipjson["domain"].ToString());
                        var port = ipjson["port"].FirstOrDefault();
                        if (port != null)
                        {
                            buf.Append(":" + port.ToString());
                        }
                        jo["data"] = buf.ToString();
                        jo["p"] = p;
                        jo["c"] = c;
                        jo["authuser"] = ipjson["authuser"]?.ToString();
                        jo["authpass"] = ipjson["authpass"]?.ToString();
                        return JsonConvert.SerializeObject(jo);
                    }
                    else if (url.Contains("xiaoxiongcloud.com") || url.Contains("find.dl321.net"))
                    {
                        var ipjson = (JObject)JsonConvert.DeserializeObject(responseBody.Trim());
                        var list = ipjson["list"].Select(s =>
                        {
                            return JObject.FromObject(new { ip = s["sever"], port = s["port"], realIp = s["ip"] });

                        }).ToList();

                        var jo = new JObject();
                        jo["data"] = JArray.FromObject(list);
                        jo["serialNo"] = url;
                        jo["p"] = p;
                        jo["c"] = c;
                        return JsonConvert.SerializeObject(jo);
                    }
                    else
                    {

                        var resp = responseBody.Trim();
                        if (JsonHelper.IsJson(resp))
                        {
                            var jo = new JObject();
                            jo["success"] = true;
                            jo["province"] = p;
                            jo["city"] = c;
                            jo["data"] = JObject.FromObject(JsonConvert.DeserializeObject(resp));
                            return JsonConvert.SerializeObject(jo);
                        }
                        return JsonConvert.SerializeObject(JObject.FromObject(new { success = true, data = responseBody.Trim(), province = p, city = c }));






                        //return JsonConvert.SerializeObject(JObject.FromObject(new { success = true, data = (!setting.RealIp ? responseBody.Trim() : JObject.FromObject(JsonConvert.DeserializeObject(responseBody.Trim()))), province = p, city = c }));
                    }
                }
                else
                {
                    _logger.LogInformation($"任务[{job["id"]}]:{job["address"]},{url},{response.StatusCode}");
                    return null;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError($"任务[{job["id"]}]:{job["address"]},{url},{ex.Message},{ex.StackTrace},{ex.InnerException?.Message}");
                LogWriteLine(ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"任务[{job["id"]}]:{job["address"]},{url},{ex.Message},{ex.StackTrace},{ex.InnerException?.Message}");
                LogWriteLine(ex.Message);
                return null;
            }
            finally
            {
                await Task.Delay(50);
                _mutex.Release();
            }
        }

        public async Task<string> GetIpInfo(string proxy, string fields = "query")
        {
            HttpClientHandler httpClientHandler = new HttpClientHandler() { Proxy = new WebProxy(proxy, BypassOnLocal: false), UseProxy = true };
            using (var client = new HttpClient(httpClientHandler))
            {
                try
                {
                    client.Timeout = TimeSpan.FromSeconds(15);
                    HttpResponseMessage response = await client.GetAsync($"http://ip-api.com/json/?lang=zh-CN&fields={fields}");
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        return await response.Content.ReadAsStringAsync();
                    }
                }
                catch (WebException ex)
                {
                    Debug.WriteLine(ex.Message);

                }
                return null;
            }
        }

        public async Task<string> GetIpInfo(string proxy)
        {
            using (var client = new HttpClient(new HttpClientHandler() { Proxy = new WebProxy(proxy, BypassOnLocal: false), UseProxy = true }))
            {
                try
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                    HttpResponseMessage response = await client.GetAsync($"http://117.21.200.18:9000/api/ipinfo.php");
                    response.EnsureSuccessStatusCode();
                    if (response.IsSuccessStatusCode)
                    {
                        var jo = JObject.Parse(await response.Content.ReadAsStringAsync());
                        if (jo["success"]?.Value<bool>() ?? false)
                        {
                            return jo.SelectToken("ip")?.Value<string>();
                        }
                    }
                }
                catch (Exception)
                {

                }
            }
            ;
            return null;

        }



        /// <summary>
        /// 获取IP区域
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static async Task<string> GetIpArea(string ip)
        {
            try
            {
                HttpResponseMessage response = await _client.GetAsync($"{AppSetting.QueryIpApiUrl}?ip={ip}");
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    return responseBody;
                }
                else
                {
                    return null;
                }

            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }
        }

        public static string GetIpArea(string ip, string proxy)
        {
            try
            {
                HttpHelper http = new HttpHelper();
                var item = new HttpItem()
                {
                    URL = "http://myip.top",
                    Method = "GET",
                    ProxyIp = proxy,
                    Allowautoredirect = true,
                    Timeout = 5000,

                };
                var hr = http.GetHtml(item);
                if (hr.StatusCode == HttpStatusCode.OK)
                {
                    return hr.Html;
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }
        }



        public static string GetIpAreaByLocal(string ip, string proxy)
        {
            try
            {
                HttpHelper http = new HttpHelper();
                var item = new HttpItem()
                {
                    URL = $"http://ip.ipkuu.com/ip.php",
                    Method = "GET",
                    ProxyIp = proxy,
                    Allowautoredirect = true,
                    Timeout = 3000,

                };
                var hr = http.GetHtml(item);
                if (hr.StatusCode == HttpStatusCode.OK)
                {
                    //_logger.LogInformation($"GetIpAreaByLocal:{ip} => {hr.Html}");
                    return hr.Html;
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }
        }

        #endregion

        #region 应用设置
        private void LoadAppSetting()
        {
            if (!System.IO.File.Exists(@"appSetting.json"))
            {
                return;
            }
            setting = JsonConvert.DeserializeObject<AppSetting>(System.IO.File.ReadAllText(@"appSetting.json"));
            textBox_DevApiUrl.Text = setting.DevApiUrl;
            textBox_AllIpApiUrl.Text = setting.AllIpApiUrl;
            textBox_TaskApiUrl.Text = setting.TaskApiUrl;
            numericUpDown_GetTaskInterval.Value = setting.GetTaskInterval;
            numericUpDown_MaximumParallel.Value = setting.MaximumParallel;
            numericUpDown_MaximumLimitedConcurrency.Value = setting.MaximumLimitedConcurrency;
            textBox_TaskIdentify.Text = setting.TaskIdentify;
            numericUpDown_UVInterval.Value = setting.UVInterval;
            checkBox_IsHiddenMode.Checked = setting.IsHiddenMode;
            checkBox_IsProxyMode.Checked = setting.NoProxy;
            numericUpDown_Multiple.Value = setting.Multiple;
            checkBox_RealIp.Checked = setting.RealIp;
            numericUpDown_MainProcessResetIntervalMinutes.Value = setting.MainProcessResetIntervalMinutes;
            numericUpDown_ChildProcessResetIntervalMinutes.Value = setting.ChildProcessResetIntervalMinutes;
            checkBox_SendSms.Checked = setting.SendSms;
            textBox_SmsName.Text = setting.SmsName;
            textBox_SmsPhone.Text = setting.SmsPhone;
            numericUpDown_SendSmsTimeout.Value = setting.SendSmsTimeout;
            checkBox_NoneOS.Checked = setting.NoneOS;
            checkBox_IPAreaCheck.Checked = setting.IPAreaCheck;
            checkBox_UsingSystemDevs.Checked = setting.UsingSystemDevs;
            checkBox_UsingIOSIMEI.Checked = setting.UsingIOSIMEI;
            checkBox_UsingIOSMAC.Checked = setting.UsingIOSMAC;
            checkBox_CheckIp.Checked = setting.CheckIp;

        }
        private void UpdateAppSetting()
        {
            setting.DevApiUrl = textBox_DevApiUrl.Text;
            setting.AllIpApiUrl = textBox_AllIpApiUrl.Text;
            setting.TaskApiUrl = textBox_TaskApiUrl.Text;
            setting.GetTaskInterval = (int)numericUpDown_GetTaskInterval.Value;
            setting.MaximumParallel = (int)numericUpDown_MaximumParallel.Value;
            setting.TaskIdentify = textBox_TaskIdentify.Text;
            setting.MaximumLimitedConcurrency = (int)numericUpDown_MaximumLimitedConcurrency.Value;
            setting.UVInterval = (int)numericUpDown_UVInterval.Value;
            setting.IsHiddenMode = checkBox_IsHiddenMode.Checked;
            setting.NoProxy = checkBox_IsProxyMode.Checked;
            setting.Multiple = (int)numericUpDown_Multiple.Value;
            setting.RealIp = checkBox_RealIp.Checked;
            setting.MainProcessResetIntervalMinutes = (int)numericUpDown_MainProcessResetIntervalMinutes.Value;
            setting.ChildProcessResetIntervalMinutes = (int)numericUpDown_ChildProcessResetIntervalMinutes.Value;
            setting.SendSms = checkBox_SendSms.Checked;
            setting.SmsName = textBox_SmsName.Text;
            setting.SmsPhone = textBox_SmsPhone.Text;
            setting.SendSmsTimeout = (int)numericUpDown_SendSmsTimeout.Value;
            setting.NoneOS = checkBox_NoneOS.Checked;
            setting.IPAreaCheck = checkBox_IPAreaCheck.Checked;
            setting.UsingSystemDevs = checkBox_UsingSystemDevs.Checked;
            setting.UsingIOSIMEI = checkBox_UsingIOSIMEI.Checked;
            setting.UsingIOSMAC = checkBox_UsingIOSMAC.Checked;
            setting.CheckIp = checkBox_CheckIp.Checked;


            System.IO.File.WriteAllText(@"appSetting.json", JsonConvert.SerializeObject(this.setting, Formatting.Indented));
        }
        #endregion


        private async Task ProducerAsync(ChannelWriter<JToken> writer, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (applicationstop || applicationrestart)
                {
                    LogWriteLine("停止获取任务");
                    break;
                }
                var content = CommonHelper.HttpGet($"{setting.TaskApiUrl}?type=1&action=getTask&task={setting.TaskIdentify}&test=0&_t={System.DateTime.Now.Ticks}");
                if (!string.IsNullOrWhiteSpace(content))
                {
                    if (content.Equals("empty"))
                    {
                        sync.Post((p) => { this.taskInfoListView.Items.Clear(); }, null);
                        LogWriteLine($"共取到[0]条任务");
                    }
                    else
                    {
                        try
                        {
                            var tasks = (JObject)JsonConvert.DeserializeObject(content);
                            int taskCount = tasks["task"].Count();
                            if (taskCount > 0)
                            {
                                if (setting.Multiple > 1)
                                {
                                    for (int i = 0; i < setting.Multiple; i++)
                                    {
                                        foreach (var task in tasks["task"])
                                        {
                                            await writer.WriteAsync(task, token);
                                        }
                                    }
                                }
                                else
                                {
                                    foreach (var task in tasks["task"])
                                    {
                                        await writer.WriteAsync(task, token);
                                    }
                                }
                                AddTaskInfo(tasks["task"]);
                                LogWriteLine($"获取[{taskCount}]条任务");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                    }
                }
                else
                {
                    LogWriteLine("获取任务");
                }
                SpinWait.SpinUntil(() => token.IsCancellationRequested, setting.GetTaskInterval);
            }
        }

        private async Task ConsumerAsync(int consumerId, ChannelReader<JToken> reader, CancellationToken token)
        {

            int jobTimeRandomSeed = setting.ChildProcessResetIntervalMinutes * 60 + new Random(Guid.NewGuid().GetHashCode()).Next(-30, 30);

            bool jobFirst = true;
            ProcessItem client = null;
            Process process = null;

            while (!token.IsCancellationRequested && !applicationrestart)
            {
                if (!await reader.WaitToReadAsync(token))
                {
                    break;
                }
                if (reader.TryRead(out var jobVal))
                {
                    var job = (JObject)jobVal;
                    if (jobFirst)
                    {
                        LogInfo($"创建进程:开始");
                        jobFirst = false;
                        jobTimeRandomSeed = setting.ChildProcessResetIntervalMinutes * 60 + new Random(Guid.NewGuid().GetHashCode()).Next(-30, 30);
                        var clientId = Guid.NewGuid().ToString("N");
                        var clientExecutablePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CefClient", "CefClient.exe");
                        process = CreateNewProcess(clientExecutablePath, this.selfWndHandle, clientId, consumerId);
                        client = new ProcessItem() { ProcessId = process.Id, ClientWindowHandle = 0, ProcessPath = clientExecutablePath, time = System.DateTime.Now };
                        this.cefProcessManager.Register(clientId, client);
                        SpinWait.SpinUntil(() => this.cts.IsCancellationRequested || client.ClientWindowHandle != 0, 30 * 1000);
                        try
                        {
                            LogInfo($"创建进程:完成{process.MainModule.FileName}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                        await Task.Delay(new Random().Next(500, 1000), this.cts.Token);
                    }

                    sync.Post((pi) =>
                    {
                        label15.Text = $"活动进程:{pi}";
                    }, this.cefProcessManager != null ? this.cefProcessManager.Count : 0);

                    var task = job as JObject;
                    if (this.cts.IsCancellationRequested)
                    {
                        return;
                    }
                    var taskId = Convert.ToInt32(task["id"].ToString());
                    var title = task["title"].ToString();
                    string proxy_server = string.Empty;
                    string realIp = string.Empty;
                    string ip = string.Empty;

                redo_getip:
                    if (this.cts.IsCancellationRequested || applicationrestart)
                    {
                        return;
                    }

                    if (!setting.NoProxy)
                    {
                        var proxyJSON = await GetIps(task, 1);
                        if (string.IsNullOrWhiteSpace(proxyJSON) || !proxyJSON.Contains(":") || proxyJSON.Contains("频繁") || proxyJSON.Contains("频率") || proxyJSON.Contains("太快") || proxyJSON.Contains("失败") || proxyJSON.Contains("错误") || proxyJSON.Contains("余额不足"))
                        {
                            LogWriteLine($"IP异常:{proxyJSON}");
                            _logger.LogError($"任务[{task["id"]}]:,地区:{task["address"]},IP异常{proxyJSON}");
                            await Task.Delay(new Random().Next(50, 100));
                            goto redo_getip;
                        }
                        JObject ipData = null;
                        string task_province = string.Empty;
                        string task_city = string.Empty;
                        if (proxyJSON.Contains("serialNo") && proxyJSON.Contains("realIp"))
                        {
                            var jo = (JObject)JsonConvert.DeserializeObject(proxyJSON);
                            if (jo["data"] is JArray)
                            {
                                var ipinfo = (JObject)jo["data"].FirstOrDefault();
                                proxy_server = $"{ipinfo["ip"]}:{ipinfo["port"]}";
                                if (ipinfo.ContainsKey("realIp"))
                                {
                                    realIp = ipinfo.SelectToken("realIp").Value<string>();
                                }
                                else if (ipinfo.ContainsKey("rip"))
                                {
                                    realIp = ipinfo.SelectToken("rip").Value<string>();
                                }
                            }
                            else
                            {
                                var _jo = (JObject)JsonConvert.DeserializeObject(jo["data"].ToString());
                                var ipinfo = (JObject)_jo["data"].FirstOrDefault();
                                proxy_server = $"{ipinfo["ip"]}:{ipinfo["port"]}";
                                if (ipinfo.ContainsKey("realIp"))
                                {
                                    realIp = ipinfo.SelectToken("realIp").Value<string>();
                                }
                                else if (ipinfo.ContainsKey("rip"))
                                {
                                    realIp = ipinfo.SelectToken("rip").Value<string>();
                                }
                            }
                        }
                        else
                        {
                            ipData = (JObject)JsonConvert.DeserializeObject(proxyJSON);
                            task_province = ipData["province"].ToString().Trim();
                            task_city = ipData["city"].ToString().Trim();
                            //LogWriteLine("proxyJSON=" + proxyJSON);
                            if (proxyJSON.Contains("data") && proxyJSON.Contains("success") && proxyJSON.Contains("province") && proxyJSON.Contains("city"))
                            {
                                if (ipData["data"]?.Type == JTokenType.String)
                                {
                                    proxyJSON = $"{ipData["data"].Value<string>().Trim()}";
                                }
                                else
                                {
                                    ipData = (JObject)JsonConvert.DeserializeObject(ipData["data"].ToString());
                                    if (ipData["data"].Count() == 0)
                                    {
                                        LogWriteLine("IP异常1");
                                        _logger.LogError($"任务[{task["id"]}]:,地区:{task["address"]},IP异常{proxyJSON}");
                                        await Task.Delay(new Random().Next(50, 100));
                                        goto redo_getip;
                                    }
                                    var ipItem = ipData["data"].FirstOrDefault();
                                    proxyJSON = $"{ipItem["ip"].ToString().Trim()}:{ipItem["port"].ToString().Trim()}";
                                    if (ipItem.SelectToken("rip") != null)
                                        realIp = ipItem.SelectToken("rip").Value<string>();
                                }





                                if (setting.IPAreaCheck)
                                {
                                    //var ip_province = ipItem["province"].ToString();
                                    //var ip_city = ipItem["city"].ToString();
                                    //if (!string.IsNullOrWhiteSpace(task_province))
                                    //{
                                    //    if (string.IsNullOrWhiteSpace(ip_province) || !ip_province.Contains(task_province))
                                    //    {
                                    //        LogWriteLine("IP异常,省份无效");
                                    //        await Task.Delay(new Random().Next(50, 100));
                                    //        goto redo_getip;
                                    //    }
                                    //}
                                    //if (!string.IsNullOrWhiteSpace(task_city))
                                    //{
                                    //    if (string.IsNullOrWhiteSpace(ip_city) || !ip_city.Contains(task_city))
                                    //    {
                                    //        LogWriteLine("IP异常,城市无效");
                                    //        await Task.Delay(new Random().Next(50, 100));
                                    //        goto redo_getip;
                                    //    }
                                    //}
                                }
                            }
                            else
                            {
                                proxyJSON = ipData["data"].ToString().Trim();
                            }
                            string pattern = @"(?:(?:[0,1]?\d?\d|2[0-4]\d|25[0-5])\.){3}(?:[0,1]?\d?\d|2[0-4]\d|25[0-5]):\d{0,5}";
                            if (!Regex.IsMatch(proxyJSON, pattern))
                            {
                                LogWriteLine("IP异常2:" + proxyJSON);
                                _logger.LogError($"任务[{task["id"]}]:,地区:{task["address"]},IP异常{proxyJSON}");
                                await Task.Delay(new Random().Next(100, 200));
                                goto redo_getip;
                            }
                            proxy_server = proxyJSON.Trim();
                            if (setting.CheckIp || (setting.RealIp && string.IsNullOrWhiteSpace(realIp)))
                            {
                                var result = await _ipTester.TestAsync(proxy_server);
                                if (result.IsValid)
                                {
                                    var ip_json = JObject.Parse(result.Data);
                                    if (ip_json.ContainsKey("query"))
                                        realIp = ip_json["query"].Value<string>();
                                    if (ip_json.ContainsKey("ip"))
                                        realIp = ip_json["ip"].Value<string>();
                                }
                                else
                                {
                                    LogWriteLine("IP检测失败:" + proxyJSON + $",{result.Data}");
                                    await Task.Delay(new Random().Next(100, 200));
                                    goto redo_getip;
                                }
                            }
                        }

                        _logger.LogInformation($"任务[{task["id"]}]:{title},IP:{realIp},地区:{task["address"]}");
                        if (!string.IsNullOrWhiteSpace(proxy_server) && proxy_server.Contains(":"))
                        {
                            ip = proxy_server.Substring(0, proxy_server.IndexOf(":"));
                        }
                        else
                        {
                            LogWriteLine($"IP异常");
                            goto redo_getip;
                        }

                            }
                            realIp = areaData["content"]["ip"].ToString();
                            isp = areaData["content"]["isp"].ToString();

                        #region IP地区检测
                        string isp = string.Empty;
                        if (setting.IPAreaCheck)
                        {
                            var areaJson = GetIpAreaByLocal(ip, proxy_server);
                            _logger.LogInformation($"IP检测:{areaJson}");
                            if (string.IsNullOrWhiteSpace(areaJson))
                            {
                                LogWriteLine($"IP异常,代理无效:{proxyJSON}");
                                await Task.Delay(new Random().Next(50, 100));
                                goto redo_getip;
                            }
                            var areaData = (JObject)JsonConvert.DeserializeObject(areaJson);
                            if (!string.IsNullOrWhiteSpace(task_province))
                            {
                                if (string.IsNullOrWhiteSpace(areaData["data"]["region"].ToString()) || !areaData["data"]["region"].ToString().Contains(task_province))
                                {
                                    LogWriteLine("IP异常,省份无效");
                                    await Task.Delay(new Random().Next(50, 100));
                                    goto redo_getip;
                                }
                            }
                            if (!string.IsNullOrWhiteSpace(task_city))
                            {
                                if (string.IsNullOrWhiteSpace(areaData["data"]["city"].ToString()) || !areaData["data"]["city"].ToString().Contains(task_city))
                                {
                                    LogWriteLine("IP异常,城市无效");
                                    await Task.Delay(new Random().Next(50, 100));
                                    goto redo_getip;
                                }

                            }
                            realIp = areaData["content"]["ip"].ToString();
                            isp = areaData["content"]["isp"].ToString();

                        }
                        #endregion



                    }




                    }

                    string url = task["url"].ToString();
                    string url2 = task["url2"].ToString();
                    int uvCount = Convert.ToInt32(task["uv"].ToString());
                    bool huichuan = false;
                    if (task["huichuan"] != null && task["huichuan"].ToString().Equals("on"))
                    {
                        huichuan = true;
                    }



                    int abl = 100;
                    if (task.ContainsKey("abl") && int.TryParse(task["abl"].ToString(), out int ablr))
                    {
                        abl = ablr;
                    }
                    var uaClients = task["client"].ToString().Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
                    var uaRates = new Dictionary<int, int>();//UA
                    foreach (var ua in uaClients)
                    {
                        uaRates.Add(Convert.ToInt32(ua), 0);
                    }
                    var refererRates = new List<RefererInfo>();
                    if (task["referer"] != null && !string.IsNullOrWhiteSpace(task["referer"].ToString()))
                    {
                        var referers = task["referer"].ToString().Split(new string[] { "<br/>" }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var referer in referers)
                        {
                            if (referer.Contains("|"))
                            {
                                var refs = referer.Split('|');
                                refererRates.Add(new RefererInfo() { Url = refs[1], Count = 0, Rate = Convert.ToInt32(refs[0]) * 0.01 });
                            }
                            else
                            {
                                refererRates.Add(new RefererInfo() { Url = referer, Count = 0, Rate = 1 });
                            }
                        }
                    }

                    string url = task["url"].ToString();
                    string url2 = task["url2"].ToString();
                    int uvCount = Convert.ToInt32(task["uv"].ToString());
                    bool huichuan = false;
                    if (task["huichuan"] != null && task["huichuan"].ToString().Equals("on"))
                    {
                        huichuan = true;
                    }

                    for (int uvIndex = 1; uvIndex <= uvCount; uvIndex++)
                    {
                        if (this.cts.IsCancellationRequested || applicationrestart)
                        {
                            break;
                        }
                        if (process.HasExited)
                        {
                            jobFirst = true;
                            break;
                        }
                        var referer = string.Empty;
                        if (refererRates.Count() > 0)
                        {
                            var f = refererRates.Where(w => w.Count < (w.Rate * uvCount)).OrderBy(o => o.Count).FirstOrDefault();
                            if (f != null)
                            {
                                f.Count = f.Count + 1;
                                referer = f.Url;
                            }
                            else
                            {
                                f = refererRates.OrderByDescending(o => o.Rate).FirstOrDefault();
                            }
                        }


                        OSType os = OSType.UNUNKNOWN;


                        if (abl != 0)
                        {
                            uaRates = uaRates.OrderBy(o => o.Value).ToDictionary(r => r.Key, r => r.Value);
                            var uaIndex = uaRates.FirstOrDefault().Key;
                            var uaValue = uaRates[uaIndex];

                            if (abl != 100)
                            {
                                if (uaIndex == 1)
                                {
                                    uaRates[uaIndex] = uaValue + (1 * (100 - abl));

                                }
                                else
                                {
                                    uaRates[uaIndex] = uaValue + 1 * abl;
                                }

                            }
                            else
                            {
                                uaRates[uaIndex] = uaValue + 1;
                            }
                        }
                        else
                        {

                            if (uaClients.Count() > 1)
                            {
                                var r_ua_index = new Random(Guid.NewGuid().GetHashCode()).Next(0, uaClients.Length);
                                if (r_ua_index >= uaClients.Length)
                                {
                                    r_ua_index--;
                                }
                                var uaIndex = Convert.ToInt32(uaClients[r_ua_index]);
                                os = DevMan.GetOSByClient(uaIndex);

                            }
                            else
                            {
                                os = DevMan.GetOSByClient(uaRates.FirstOrDefault().Key);
                            }
                        }

                        JToken dev = await GetDevByOS(os);
                        var ua = dev["ua"].Value<string>();



                        #region 网址处理

                        string uv_url = string.Empty;
                        string domain = new Uri(url).Host;

                        if (domain.Contains("miaozhen.com"))
                        {
                            uv_url = FormatUrlText(url, ip, ua, task, os, null, dev);
                        }
                        else if (domain.Contains("gridsumdissector.com"))
                        {
                            uv_url = GridSumissector(url, ip, ua, task, os, null, dev);
                        }
                        else if (domain.Contains("ipinyou.com"))
                        {
                            uv_url = FormatUrl_ipinyou(url, ip, ua, task, os, null, dev);
                        }
                        else if (domain.Contains("mafengwo.cn"))
                        {
                            uv_url = FormatUrl_mafengwo(url, ip, ua, task, os, null, dev);
                        }
                        else
                        {
                            uv_url = FormatUrlText(url, ip, ua, task, os, null, dev);
                        }

                        string uv_url2 = string.Empty;
                        if (!string.IsNullOrWhiteSpace(url2))
                        {
                            if (url2.Contains("miaozhen.com"))
                            {
                                uv_url2 = FormatUrlText(url2, ip, ua, task, os, null, dev);
                            }
                            else if (url2.Contains("gridsumdissector.com"))
                            {

                                uv_url2 = GridSumissector(url2, ip, ua, task, os, null, dev);
                            }
                            else if (url2.Contains("ipinyou.com"))
                            {

                                uv_url2 = FormatUrl_ipinyou(url2, ip, ua, task, os, null, dev);
                            }
                            else if (domain.Contains("mafengwo.cn"))
                            {
                                uv_url = FormatUrl_mafengwo(url, ip, ua, task, os, null, dev);
                            }
                            else
                            {
                                uv_url2 = FormatUrlText(url2, ip, ua, task, os, null, dev);

                            }
                        }
                        #endregion

                        var cacheIndex = $"s{uvIndex}";

                        var _args = new JObject();
                        //_args["ipkey"] = null;
                        if (setting.NoProxy)
                        {
                            _args["IsProxyMode"] = false;
                            _args["proxy_server"] = null;
                        }
                        else
                        {
                            _args["IsProxyMode"] = true;
                            _args["proxy_server"] = proxy_server;

                        }
                        _args["os"] = (int)os;
                        var msgret = await SendLoadUrlMessage(client, uv_url, uv_url2, _args, ua, referer, task, dev, cacheIndex);
                        Interlocked.Increment(ref TotalUVCount);
                        LogWriteLine($"提交任务[{task["id"]}]:{task["title"]},process=[{consumerId}],os={os},osv={dev["osv"]},cache={cacheIndex},{proxy_server},{uvIndex}/{uvCount}");
                        sync.Post((pi) =>
                        {
                            label5.Text = $"提交数量:{pi}";
                            label7.Text = $"运行时间:{(int)sw.Elapsed.TotalMinutes}分钟";
                        }, this.TotalUVCount);

                        if (uvCount > 1)
                        {
                            SpinWait.SpinUntil(() => false, setting.UVInterval);
                        }
                    }

                    #region 清理代码
                    if (!jobFirst)
                    {
                        if (process != null && !process.HasExited && setting.ChildProcessResetIntervalMinutes > 0 && ((TimeSpan)(System.DateTime.Now - process.StartTime)).TotalSeconds > jobTimeRandomSeed)
                        {
                            jobFirst = true;
                            if (process != null && !process.HasExited)
                            {
                                LogInfo($"清理进程:开始{process.MainModule.FileName}");
                                await Task.Delay(1000, this.cts.Token);
                                if (process != null && !process.HasExited)
                                {
                                    try
                                    {
                                        process.Kill();
                                    }
                                    catch (Exception ex)
                                    {
                                        LogWriteLine(ex.Message);
                                        CommonHelper.KillProcExec(process.Id);
                                    }
                                }
                            }
                        }
                    }
                    #endregion

                    SpinWait.SpinUntil(() => this.cts.IsCancellationRequested, setting.UVInterval);
                }
            }

            if (process != null && !process.HasExited)
            {
                try
                {
                    process.Kill();
                }
                catch (Exception ex)
                {
                    LogWriteLine(ex.Message);
                    CommonHelper.KillProcExec(process.Id);
                }
            }
        }

        private Process CreateNewProcess(string filePath, IntPtr hWnd, string clientId, int consumerId)
        {
            try
            {
                ProcessStartInfo processInfo = new ProcessStartInfo();
                processInfo.FileName = filePath;
                processInfo.Arguments = $"--main-handle={hWnd.ToInt64()} --hidden-mode={setting.IsHiddenMode} --client-id={clientId} --consumer-id={consumerId}";
                processInfo.UseShellExecute = false;
                processInfo.CreateNoWindow = true;
                Process process = new Process();
                process.EnableRaisingEvents = true;
                process.StartInfo = processInfo;
                process.Exited += (a, b) =>
                {
                    LogWriteLine($"退出进程{clientId}");
                    this.cefProcessManager?.Remove(clientId);
                    sync.Post((pi) =>
                    {
                        label15.Text = $"活动进程:{pi}";
                    }, this.cefProcessManager != null ? this.cefProcessManager.Count : 0);
                };
                process.Start();
                return process;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return null;

        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            var commandLineArgs = System.Environment.GetCommandLineArgs();
            var restart_flag = false;
            foreach (var c in commandLineArgs)
            {
                if (c.StartsWith("restart"))
                {
                    restart_flag = true;
                }
                else if (c.StartsWith("tasklist_dat="))
                {
                    var tasklist_dat = c.Split('=')[1];
                    if (!string.IsNullOrWhiteSpace(tasklist_dat) && System.IO.File.Exists(tasklist_dat))
                    {
                        var content = System.IO.File.ReadAllText(tasklist_dat);
                        var values = JsonConvert.DeserializeObject<List<JToken>>(content);
                        if (values != null)
                        {
                            this.pendingTasks.AddRange(values);
                        }
                        try
                        {
                            System.IO.File.Delete(tasklist_dat);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);

                        }
                    }
                }
                else if (c.StartsWith("totaluvcount="))
                {
                    if (Int32.TryParse(c.Split('=')[1], out int _cnt))
                    {
                        this.TotalUVCount = _cnt;
                        label5.Text = $"提交数量:{this.TotalUVCount}";
                    }
                }
            }
            if (restart_flag)
            {
                sync.Post((p) =>
                {
                    buttonStart.PerformClick();
                }, null);
            }
            label6.Text = "CPU:" + Environment.ProcessorCount.ToString();

            //textBox_SmsName.Text = CommonHelper.GetIpAddress();
        }

        //IP列列
        private ConcurrentDictionary<int, BlockingCollection<string>> taskIpDict = new ConcurrentDictionary<int, BlockingCollection<string>>();
        private int GetTaskQueueCapacity()
        {
            return setting.Multiple > 1 ? 3 + setting.Multiple : 3;
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            if (buttonStart.Text.Equals("停止"))
            {
                applicationrestart = false;
                applicationstop = true;
                buttonStart.Enabled = false;
                buttonStart.Text = "停止中...";
                buttonStart.ForeColor = Color.Black;
                this.buttonStart.Enabled = false;
                Task.Run(async () =>
                {
                    await Task.Delay(5 * 1000);
                    sync.Post((p) =>
                    {
                        this.cts.Cancel();
                        sw.Stop();
                        this.TopMost = false;
                    }, null);
                    if (this.taskDispatchManager != null)
                    {
                        await this.taskDispatchManager.StopAsync(8 * 1000);
                    }
                    this.cefProcessManager?.KillAll();
                    CommonHelper.ClearProcesses(new string[] { "CefClient", "CefSharp.BrowserSubprocess", "WerFault" });
                    #region 删除物理文件
                    /*
                    for (int parallelIndex = 1; parallelIndex <= setting.MaximumParallel; parallelIndex++)
                    {
                        try
                        {
                            Directory.Delete(System.IO.Path.Combine(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "chrome", "User Data", parallelIndex.ToString()), recursive: true);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                        try
                        {
                            CommonHelper.DeleteCookieFile(System.IO.Path.Combine(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "chrome", "User Data", parallelIndex.ToString()));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                    */
                    #endregion
                    this.BeginInvoke(new MethodInvoker(() =>
                    {
                        this.TotalUVCount = 0;
                        buttonStart.Text = "开始";
                        buttonStart.ForeColor = Color.Black;
                        buttonStart.Enabled = true;
                        this.buttonStart.Enabled = true;
                    }));
                });
                return;
            }
            applicationrestart = false;
            applicationstop = false;
            UpdateAppSetting();

            this.taskDispatchManager = new TaskDispatchManager(GetTaskQueueCapacity());

            this.selfWndHandle = this.Handle;
            this.processOfList = new System.Collections.Concurrent.ConcurrentDictionary<string, ProcessItem>();
            this.processOfList.Clear();
            this.cefProcessManager = new CefClientProcessManager(this.processOfList, LogWriteLine);
            buttonStart.Text = "停止";
            buttonStart.ForeColor = Color.Blue;
            sw.Reset();
            sw.Start();
            this.cts = new CancellationTokenSource();
            this.cts.Token.Register(() =>
            {
                buttonStart.Enabled = false;
                buttonStart.Text = "停止中...";
                buttonStart.ForeColor = Color.Black;
                this.buttonStart.Enabled = false;
            });

            #region 获取任务及执行任务
            foreach (var pendingTask in this.pendingTasks)
            {
                this.taskDispatchManager.Writer.TryWrite(pendingTask);
            }
            this.pendingTasks.Clear();
            this.taskDispatchManager.Start(setting.MaximumParallel, ProducerAsync, ConsumerAsync, this.cts.Token);

            #endregion

            #region 守护任务
            var defends = Task.Factory.StartNew(async () =>
            {
                var restartGuard = new AppRestartGuard(
                    setting.MainProcessResetIntervalMinutes,
                    setting.SendSms,
                    setting.SendSmsTimeout,
                    LogWriteLine,
                    AdxHelper.SendSms);
                await restartGuard.WaitForRestartAsync(this.cts.Token, setting.SmsName, setting.SmsPhone);
                if (this.cts.IsCancellationRequested)
                {
                    return;
                }
                applicationrestart = true;
                sync.Post((p) =>
                {
                    this.buttonStart.Enabled = false;
                    this.button1.Enabled = false;
                }, null);

                string tasklist_dat = string.Empty;

                var remainingTasks = this.taskDispatchManager != null ? this.taskDispatchManager.DrainPending() : new List<JToken>();
                if (remainingTasks.Count > 0)
                {
                    ///暂时存任务列表
                    _logger.LogInformation("暂时存任务列表");
                    try
                    {
                        tasklist_dat = $"tasklist_dat{System.DateTime.Now.Ticks}.tmp";
                        System.IO.File.WriteAllText(tasklist_dat, JsonConvert.SerializeObject(remainingTasks));

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, ex.Message);
                    }

                }
                _logger.LogInformation("延时5秒");
                await Task.Delay(5000);
                if (this.cefProcessManager != null && this.cefProcessManager.Count > 0)
                {
                    _logger.LogInformation("清理未结束的进程");
                    this.cefProcessManager.KillAll();
                }
                CommonHelper.ClearProcesses(new string[] { "CefClient", "CefSharp.BrowserSubprocess", "WerFault" });
                sync.Post((p) =>
                {
                    _logger.LogInformation("清理所有的进程内存");
                    //NativeMethod.EmptyWorkingSet(Process.GetCurrentProcess().Handle);
                    string arguments = $"restart";
                    if (!string.IsNullOrWhiteSpace(tasklist_dat))
                    {
                        arguments = $"{arguments} tasklist_dat={tasklist_dat}";
                    }
                    _logger.LogInformation($"重启进程{arguments}");
                    Process.Start(Application.ExecutablePath, arguments);
                    _logger.LogInformation("关闭当前进程");
                    try
                    {
                        Process.GetCurrentProcess().Kill();
                    }
                    catch (Exception ex)
                    {
                        CommonHelper.KillProcExec(Process.GetCurrentProcess().Id);
                        _logger.LogError(ex.Message);
                        Debug.WriteLine(ex.Message);
                    }

                }, null);


            }, cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            #endregion
        }
        private void button1_Click(object sender, EventArgs e)
        {
            buttonStart.Enabled = false;
            button1.Enabled = false;
            Task.Run(() =>
            {
                CommonHelper.ClearProcesses(new string[] { "CefClient", "CefSharp.BrowserSubprocess", "WerFault" });
                GC.Collect();
                GC.WaitForPendingFinalizers();
                foreach (Process process in Process.GetProcesses())
                {
                    try
                    {
                        //NativeMethod.EmptyWorkingSet(process.Handle);
                    }
                    catch (Exception)
                    {
                    }
                }
                var cachePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "chrome", "User Data");
                if (System.IO.Directory.Exists(cachePath))
                    CommonHelper.ClearDirectory(cachePath);

                this.BeginInvoke(new MethodInvoker(() =>
                {
                    buttonStart.Enabled = true;
                    button1.Enabled = true;
                }));


            });

        }
        private void checkBox_ShowWeb_Click(object sender, EventArgs e)
        {
            if (this.processOfList != null && this.processOfList.Count() > 0)
            {
                foreach (var p in this.processOfList.Keys)
                {
                    //SendShowFormMessage(this.processOfList[p].ClientWindowHandle, checkBox_ShowWeb.Checked);
                }
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            AdxHelper.SendSms(textBox_SmsName.Text, textBox_SmsPhone.Text);
        }






        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            messageChannel.Writer.TryComplete();
            messageProcessingCts.Cancel();
            copyDataSendSemaphore.Dispose();
            messageProcessingCts.Dispose();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {


        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.IO.DirectoryInfo dir = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.Startup));
            foreach (System.IO.FileInfo file in dir.GetFiles())
                file.Delete();
            Process.Start(new ProcessStartInfo { FileName = Environment.GetFolderPath(Environment.SpecialFolder.Startup), UseShellExecute = true });
            CommonHelper.CreateShortcut("曝光");
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string currentDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            Process.Start(new ProcessStartInfo { FileName = currentDirectory, UseShellExecute = true });
        }
    }

}
