using AdxImp.Win32;
using MainClient.Common;
using MainClient.Infrastructure;
using MainClient.Models;
using MainClient.ProxyChecker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog.Core;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using System.Net.Http;
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
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger _logger;
        private readonly AppSettings _appSettings;


        private readonly AdxHelper _adxHelper;
        private readonly IpHelper _ipHelper;
        private readonly ProxyTester _ipTester;
        private readonly TrackingUrlProcessor _trackingUrlProcessor;

        private CancellationTokenSource cts;

        private ConcurrentDictionary<string, ProcessItem> processOfList;


        /// <summary>
        /// UV合计数量
        /// </summary>
        private int TotalUVCount = 0;




        private TaskDispatchManager taskDispatchManager = null;
        private CefClientProcessManager cefProcessManager = null;

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
                        m.Result = new IntPtr(1);
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



        public MainForm(
            TrackingUrlProcessor trackingUrlProcessor,
            AdxHelper adxHelper,
            IpHelper ipHelper,
            ProxyTester ipTester,
            AppSettings appSettings,
            IHttpClientFactory httpClientFactory,
            ILogger<MainForm> logger)
        {
            InitializeComponent();
            this._trackingUrlProcessor = trackingUrlProcessor;
            this._appSettings = appSettings;
            this._adxHelper = adxHelper;
            this._ipHelper = ipHelper;
            this._ipTester = ipTester;
            this._logger = logger;
            this._httpClientFactory = httpClientFactory;

            this.Text += $"［{AppConsts.AppVersion}］";
            LoadAppSetting();
            if (this._appSettings == null)
            {
                this._appSettings = new AppSettings();
                UpdateAppSetting();
            }

            StartMessageProcessor();

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


            var cachePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "chrome", "User Data");
            if (System.IO.Directory.Exists(cachePath))
                FileHelper.CleanCefCache(cachePath, maxTotalSizeMb: 40_000, keepRecentDays: 7);
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


        #region 应用设置
        private void LoadAppSetting()
        {

            checkBox_IsProxyMode.Checked = _appSettings.IsProxyMode;
            checkBox_IsRealIp.Checked = _appSettings.IsRealIp;
            checkBox_CheckIpHealth.Checked = _appSettings.CheckIpHealth;
            checkBox_CheckIpRegion.Checked = _appSettings.CheckIpRegion;
            numericUpDown_IpValidityDuration.Value = _appSettings.IpValidityDuration;

            textBox_DevApiUrl.Text = _appSettings.DevApiUrl;
            textBox_ProxyIpUrl.Text = _appSettings.ProxyIpUrl;
            textBox_TaskApiUrl.Text = _appSettings.TaskApiUrl;


            textBox_TaskName.Text = _appSettings.TaskName;
            numericUpDown_TaskPullIntervalMs.Value = _appSettings.TaskPullIntervalMs;
            numericUpDown_UvExecutionIntervalMs.Value = _appSettings.UvExecutionIntervalMs;
            numericUpDown_ChannelCapacity.Value = _appSettings.ChannelCapacity;
            numericUpDown_MaxConcurrency.Value = _appSettings.MaxConcurrency;
            numericUpDown_Multiple.Value = _appSettings.Multiple;
            checkBox_IsHiddenMode.Checked = _appSettings.IsHiddenMode;
            numericUpDown_MainProcessResetIntervalMinutes.Value = _appSettings.MainProcessResetIntervalMinutes;
            numericUpDown_ChildProcessResetIntervalMinutes.Value = _appSettings.ChildProcessResetIntervalMinutes;


            checkBox_SendSms.Checked = _appSettings.SendSms;
            textBox_SmsName.Text = _appSettings.SmsName;
            textBox_SmsPhone.Text = _appSettings.SmsPhone;
            numericUpDown_SendSmsTimeout.Value = _appSettings.SendSmsTimeout;
            checkBox_NoneOS.Checked = _appSettings.NoneOS;
            checkBox_UsingSystemDevs.Checked = _appSettings.UsingSystemDevs;
            checkBox_UsingIOSIMEI.Checked = _appSettings.UsingIOSIMEI;
            checkBox_UsingIOSMAC.Checked = _appSettings.UsingIOSMAC;





        }
        private static object lock_config = new object();
        private void UpdateAppSetting()
        {
            lock (lock_config)
            {

                _appSettings.IsProxyMode = checkBox_IsProxyMode.Checked;
                _appSettings.IsRealIp = checkBox_IsRealIp.Checked;
                _appSettings.CheckIpHealth = checkBox_CheckIpHealth.Checked;
                _appSettings.CheckIpRegion = checkBox_CheckIpRegion.Checked;
                _appSettings.IpValidityDuration = (int)numericUpDown_IpValidityDuration.Value;


                _appSettings.DevApiUrl = textBox_DevApiUrl.Text;
                _appSettings.ProxyIpUrl = textBox_ProxyIpUrl.Text;
                _appSettings.TaskApiUrl = textBox_TaskApiUrl.Text;


                _appSettings.TaskName = textBox_TaskName.Text;
                _appSettings.TaskPullIntervalMs = (int)numericUpDown_TaskPullIntervalMs.Value;
                _appSettings.UvExecutionIntervalMs = (int)numericUpDown_UvExecutionIntervalMs.Value;
                _appSettings.ChannelCapacity = (int)numericUpDown_ChannelCapacity.Value;
                _appSettings.MaxConcurrency = (int)numericUpDown_MaxConcurrency.Value;
                _appSettings.Multiple = (int)numericUpDown_Multiple.Value;
                _appSettings.IsHiddenMode = checkBox_IsHiddenMode.Checked;
                _appSettings.MainProcessResetIntervalMinutes = (int)numericUpDown_MainProcessResetIntervalMinutes.Value;
                _appSettings.ChildProcessResetIntervalMinutes = (int)numericUpDown_ChildProcessResetIntervalMinutes.Value;


                _appSettings.SendSms = checkBox_SendSms.Checked;
                _appSettings.SmsName = textBox_SmsName.Text;
                _appSettings.SmsPhone = textBox_SmsPhone.Text;
                _appSettings.SendSmsTimeout = (int)numericUpDown_SendSmsTimeout.Value;
                _appSettings.NoneOS = checkBox_NoneOS.Checked;
                _appSettings.UsingSystemDevs = checkBox_UsingSystemDevs.Checked;
                _appSettings.UsingIOSIMEI = checkBox_UsingIOSIMEI.Checked;
                _appSettings.UsingIOSMAC = checkBox_UsingIOSMAC.Checked;

                UserConfigService.Save("AppSettings", _appSettings);
            }
        }
        #endregion


        private async Task ProducerAsync(
            ChannelWriter<JToken> writer,
            CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    List<JToken> taskOfList;

                    try
                    {
                        taskOfList = await _adxHelper.GetTasksAsync(token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        LogWriteLine($"拉取任务异常: {ex}");

                        int delay = _appSettings.TaskPullErrorDelayMs <= 0
                            ? 1000
                            : _appSettings.TaskPullErrorDelayMs;

                        await Task.Delay(delay, token).ConfigureAwait(false);
                        continue;
                    }

                    if (taskOfList.Count == 0)
                    {
                        int interval = _appSettings.TaskPullIntervalMs <= 0
                            ? 500
                            : _appSettings.TaskPullIntervalMs;

                        await Task.Delay(interval, token).ConfigureAwait(false);
                        continue;
                    }

                    int multiple = _appSettings.Multiple <= 0
                        ? 1
                        : _appSettings.Multiple;

                    int writeCount = 0;

                    int fetchCount = taskOfList.Count();

                    foreach (var task in taskOfList)
                    {
                        token.ThrowIfCancellationRequested();

                        for (int i = 0; i < multiple; i++)
                        {
                            token.ThrowIfCancellationRequested();

                            var cloned = task.DeepClone();

                            if (cloned is JObject obj)
                            {
                                //obj["_copyIndex"] = i + 1;
                                //obj["_copyTotal"] = multiple;
                                //obj["_dispatchTime"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                            }

                            // 重点：
                            // 正常运行时，如果 Channel 满了，这里会等待。
                            // 点击停止时，token 取消，这里会立即退出。
                            await writer.WriteAsync(cloned, token).ConfigureAwait(false);

                            writeCount++;
                        }
                    }

                    LogWriteLine($"本轮取回={fetchCount}，倍率={multiple}，写入队列={writeCount}");
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                LogWriteLine("Producer 已取消。");
            }
            catch (ChannelClosedException)
            {
                LogWriteLine("Producer 检测到 Channel 已关闭。");
            }
            catch (Exception ex)
            {
                LogWriteLine($"Producer 主循环异常: {ex}");
            }
            finally
            {
                writer.TryComplete();
            }
        }

        private async Task ConsumerAsync(
            int consumerId,
            ChannelReader<JToken> reader,
            CancellationToken token)
        {
            try
            {
                while (await reader.WaitToReadAsync(token).ConfigureAwait(false))
                {
                    while (reader.TryRead(out var item))
                    {
                        token.ThrowIfCancellationRequested();

                        try
                        {
                            var id = item["id"]?.ToString();

                            LogWriteLine($"Consumer-{consumerId} 开始处理任务 id={id}");

                            await HandleTaskAsync(
                                consumerId,
                                item,
                                token
                            ).ConfigureAwait(false);

                            LogWriteLine($"Consumer-{consumerId} 处理完成 id={id}");
                        }
                        catch (OperationCanceledException) when (token.IsCancellationRequested)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            // 单个任务异常，不让整个消费者挂掉
                            LogWriteLine($"Consumer-{consumerId} 处理任务异常: {ex}");
                        }
                    }
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                LogWriteLine($"Consumer-{consumerId} 已取消。");
            }
            catch (ChannelClosedException)
            {
                LogWriteLine($"Consumer-{consumerId} 检测到 Channel 已关闭。");
            }
            catch (Exception ex)
            {
                LogWriteLine($"Consumer-{consumerId} 异常退出: {ex}");
            }
        }


        private async Task HandleTaskAsync(
            int consumerId,
            JToken item,
            CancellationToken token)
        {
            // 这里写你的实际任务逻辑
            // 比如打开 CefSharp、Playwright、请求接口、执行 UV 等
            token.ThrowIfCancellationRequested();
            await Task.Delay(Random.Shared.Next(1000,8000), token).ConfigureAwait(false);
            // 示例：
            var url = item["url"]?.ToString();
            var id = item["id"]?.ToString();
            LogWriteLine($"Consumer-{consumerId} 模拟处理任务 id={id}, url={url}");
        }





        private Process CreateNewProcess(string filePath, IntPtr hWnd, string clientId, int consumerId)
        {
            try
            {
                ProcessStartInfo processInfo = new ProcessStartInfo();
                processInfo.FileName = filePath;
                processInfo.Arguments = $"--main-handle={hWnd.ToInt64()} --hidden-mode={_appSettings.IsHiddenMode} --client-id={clientId} --consumer-id={consumerId}";
                processInfo.UseShellExecute = false;
                processInfo.CreateNoWindow = true;
                Process process = new Process();
                process.EnableRaisingEvents = true;
                process.StartInfo = processInfo;
                process.Exited += (a, b) =>
                {
                    LogWriteLine($"退出进程{clientId}");
                    this.cefProcessManager?.Remove(clientId);
                    this.InvokeOnUiThreadIfRequired(() =>
                    {
                        label15.Text = $"活动进程:{(this.cefProcessManager != null ? this.cefProcessManager.Count : 0)}";



                    });
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
                this.InvokeOnUiThreadIfRequired(() =>
                {
                    buttonStart.PerformClick();


                });

            }
            label6.Text = "CPU:" + Environment.ProcessorCount.ToString();

            //textBox_SmsName.Text = CommonHelper.GetIpAddress();
        }

        private int GetTaskQueueCapacity()
        {
            //return _appSettings.Multiple > 1 ? 3 + _appSettings.Multiple : 3;
            return _appSettings.ChannelCapacity;
        }

        private async Task StopRunningTasksAsync()
        {
            try
            {
                this.cts?.Cancel();
                if (this.taskDispatchManager != null)
                {
                    await this.taskDispatchManager.StopAsync(8 * 1000);
                }
                this.cefProcessManager?.KillAll();
                CommonHelper.ClearProcesses(new string[] { "CefClient", "CefSharp.BrowserSubprocess", "WerFault" });
            }
            finally
            {
                ResetStartButtonAfterStop();
            }
        }

        private void ResetStartButtonAfterStop()
        {
            this.BeginInvoke(new MethodInvoker(() =>
            {
                this.TotalUVCount = 0;
                buttonStart.Text = "开始";
                buttonStart.ForeColor = Color.Black;
                buttonStart.Enabled = true;
                this.buttonStart.Enabled = true;
            }));
        }

        private void buttonStart_Click(object sender, EventArgs e)
        {
            if (buttonStart.Text.Equals("停止"))
            {
                buttonStart.Enabled = false;
                buttonStart.Text = "停止中...";
                buttonStart.ForeColor = Color.Black;
                this.buttonStart.Enabled = false;
                Task.Run(StopRunningTasksAsync);
                return;
            }
            StartRunningTasks();
        }

        private void StartRunningTasks()
        {
            UpdateAppSetting();
            this.taskDispatchManager = new TaskDispatchManager(GetTaskQueueCapacity(), LogWriteLine, ex => LogWriteLine(ex.ToString()));
            SubscribeTaskDispatchManagerEvents(this.taskDispatchManager);
            this.selfWndHandle = this.Handle;
            this.processOfList = new System.Collections.Concurrent.ConcurrentDictionary<string, ProcessItem>();
            this.processOfList.Clear();
            this.cefProcessManager = new CefClientProcessManager(this.processOfList, LogWriteLine);
            buttonStart.Text = "停止";
            buttonStart.ForeColor = Color.Blue;
            this.cts = new CancellationTokenSource();
            this.cts.Token.Register(() =>
            {
                buttonStart.Enabled = false;
                buttonStart.Text = "停止中...";
                buttonStart.ForeColor = Color.Black;
                this.buttonStart.Enabled = false;
            });

            #region 获取任务及执行任务
            this.taskDispatchManager.Start(
                _appSettings.MaxConcurrency,
                ProducerAsync,
                ConsumerAsync,
                this.cts.Token);
            #endregion

            StartRestartGuard();
        }


        private void SubscribeTaskDispatchManagerEvents(TaskDispatchManager manager)
        {
            manager.StateChanged += (_, e) =>
            {
                LogInfo($"任务调度器状态变更：{e.OldState} -> {e.NewState}");

                if (e.Exception != null)
                {
                    LogWriteLine($"任务调度器异常：{e.Exception.Message}");
                }

                this.InvokeOnUiThreadIfRequired(() =>
                {
                    if (e.NewState == RunnerState.Running)
                    {
                        buttonStart.Text = "停止";
                        buttonStart.ForeColor = Color.Blue;
                        buttonStart.Enabled = true;
                    }
                    else if (e.NewState == RunnerState.Stopping)
                    {
                        buttonStart.Text = "停止中...";
                        buttonStart.ForeColor = Color.Black;
                        buttonStart.Enabled = false;
                    }
                    else if (e.NewState == RunnerState.Stopped || e.NewState == RunnerState.Faulted)
                    {
                        buttonStart.Text = "开始";
                        buttonStart.ForeColor = Color.Black;
                        buttonStart.Enabled = true;
                    }
                });
            };

            manager.TaskReceived += (_, e) =>
            {
                LogInfo($"任务入队：id={e.TaskId ?? "-"}");
            };

            manager.TaskConsumed += (_, e) =>
            {
                LogInfo($"任务出队：consumer={e.ConsumerId?.ToString() ?? "-"}, id={e.TaskId ?? "-"}");
            };
        }

        private void StartRestartGuard()
        {
            Task.Factory.StartNew(
                async () => await RunRestartGuardAsync(),
                cts.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default).Unwrap();
        }

        private async Task RunRestartGuardAsync()
        {
            var restartGuard = new AppRestartGuard(
                _appSettings.MainProcessResetIntervalMinutes,
                _appSettings.SendSms,
                _appSettings.SendSmsTimeout,
                LogWriteLine,
                AdxHelper.SendSms);
            await restartGuard.WaitForRestartAsync(this.cts.Token, _appSettings.SmsName, _appSettings.SmsPhone);
            if (this.cts.IsCancellationRequested)
            {
                return;
            }

            this.InvokeOnUiThreadIfRequired(() =>
            {

                this.buttonStart.Enabled = false;
                this.button1.Enabled = false;
            });


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

            this.InvokeOnUiThreadIfRequired(() =>
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

            });



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
