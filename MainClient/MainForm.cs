using AdxImp.Win32;
using MainClient.AdxImp;
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



        private readonly TaskStatisticsManager taskStatisticsManager = new TaskStatisticsManager();
        private WinCopyDataMessageBus? _messageBus;
        private IntPtr _selfWndHandle;

        private CefClientProcessManager cefProcessManager = null;
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

        #region 消息处理

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _selfWndHandle = this.Handle;
            _messageBus = new WinCopyDataMessageBus(new WinCopyDataMessageBusOptions
            {
                ChannelCapacity = 10000,
                FullMode = BoundedChannelFullMode.DropOldest,
                RaiseEventsOnCapturedContext = true,
                MaxMessageChars = 1024 * 1024
            });
            _messageBus.ClientStarted += MessageBus_ClientStarted;
            _messageBus.TaskStageChanged += MessageBus_TaskStageChanged;
            _messageBus.UnknownMessageReceived += MessageBus_UnknownMessageReceived;
            _messageBus.LogReceived += MessageBus_LogReceived;
            _messageBus.Start();
            this.FormClosing += async (s, e) =>
            {
                if (_messageBus != null)
                {
                    await _messageBus.StopAsync();
                    _messageBus.Dispose();
                    _messageBus = null;
                }
            };
        }

        protected override void DefWndProc(ref Message m)
        {
            if (_messageBus != null && _messageBus.TryHandleWndProc(ref m))
            {
                return;
            }
            base.DefWndProc(ref m);
        }

        private void MessageBus_ClientStarted(object sender, ClientStartedEventArgs e)
        {
            // 原来的：
            // this.cefProcessManager?.UpdateWindowHandle(clientId, clientHandle);

            this.cefProcessManager?.UpdateWindowHandle(e.ClientId, e.ClientHandle);

            LogWriteLine($"客户端已启动：ClientId={e.ClientId}, Hwnd={e.ClientHandle}");
        }

        private void MessageBus_TaskStageChanged(object sender, TaskStageMessageEventArgs e)
        {
            // 原来的：
            // RecordTaskStageFromClient(message);

            RecordTaskStageFromClient(e.Json);

            LogWriteLine($"任务状态变化：Msg={e.Msg}, ClientId={e.ClientId}, Stage={e.Stage}, Status={e.Status}");
        }

        private void MessageBus_UnknownMessageReceived(object sender, UnknownClientMessageEventArgs e)
        {
            LogWriteLine($"收到未知客户端消息：Msg={e.Msg}");
        }

        private void MessageBus_LogReceived(object sender, MessageBusLogEventArgs e)
        {
            LogWriteLine(e.Message);
        }

        private void MessageBus_ErrorReceived(object sender, MessageBusErrorEventArgs e)
        {
            LogWriteLine($"{e.Message}：{e.Exception.Message}");

            // 如果你有 ILogger
            // _logger?.LogError(e.Exception, e.Message);
        }










        //private async Task<IntPtr> SendLoadUrlMessage(ProcessItem clientProcess, string url, string url2, JObject _args, string userAgent, string referer, JObject param, JToken devInfo, string cacheIndex)
        //{
        //    if (clientProcess == null || clientProcess.ClientWindowHandle == IntPtr.Zero)
        //    {
        //        LogWriteLine("LOAD消息发送失败：客户端窗口句柄为空");
        //        return IntPtr.Zero;
        //    }

        //    var message = JsonConvert.SerializeObject(JObject.FromObject(new
        //    {
        //        Msg = "LOAD",
        //        Url = url,
        //        Url2 = url2,
        //        args = _args,
        //        UserAgent = userAgent,
        //        Referer = referer,
        //        DevInfo = devInfo,
        //        Param = param,
        //        CacheIndex = cacheIndex
        //    }));

        //    var cds = new COPYDATASTRUCT
        //    {
        //        dwData = new IntPtr(100),
        //        lpData = message,
        //        cbData = (message.Length + 1) * 2
        //    };

        //    await copyDataSendSemaphore.WaitAsync(this.cts?.Token ?? CancellationToken.None);
        //    try
        //    {
        //        IntPtr sendResult;
        //        var ret = NativeMethod.SendMessageTimeout(
        //            clientProcess.ClientWindowHandle,
        //            WinTypes.WM_COPYDATA,
        //            selfWndHandle,
        //            ref cds,
        //            WinTypes.SMTO_ABORTIFHUNG,
        //            3000,
        //            out sendResult
        //        );

        //        if (ret == IntPtr.Zero)
        //        {
        //            var error = Marshal.GetLastWin32Error();
        //            LogWriteLine($"LOAD消息发送失败或超时：ProcessId={clientProcess.ProcessId}, Hwnd={clientProcess.ClientWindowHandle}, Error={error}");
        //        }

        //        return ret;
        //    }
        //    finally
        //    {
        //        copyDataSendSemaphore.Release();
        //    }
        //}

        //private static void SendShowFormMessage(IntPtr clientWindowHandle, bool show = true)
        //{
        //    if (clientWindowHandle == IntPtr.Zero)
        //    {
        //        return;
        //    }

        //    var message = JsonConvert.SerializeObject(JObject.FromObject(new
        //    {
        //        Msg = show ? "SHOW" : "HIDE",
        //    }));

        //    var cds = new COPYDATASTRUCT
        //    {
        //        dwData = new IntPtr(100),
        //        lpData = message,
        //        cbData = (message.Length + 1) * 2
        //    };
        //    NativeMethod.SendMessage(clientWindowHandle, WinTypes.WM_COPYDATA, 0, ref cds);
        //}


        #endregion

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

        #region 任务调度管理

        private TaskDispatchManager _taskManager = default!;
        private void InitTaskDispatchManager()
        {
            _taskManager = new TaskDispatchManager(new TaskDispatchManagerOptions
            {
                // 队列容量,表示最多提前缓存 指定数量 任务
                Capacity = _appSettings.ChannelCapacity,

                // 停止时，把队列里还没被取出的任务落盘
                PersistPendingOnStop = true,

                // 下次启动时，先加载上次落盘的任务
                LoadPersistedOnStart = true,

                // 加载成功后删除落盘文件，避免重复执行
                DeletePersistenceFileAfterLoad = true,

                PersistenceFilePath = Path.Combine(
                    AppContext.BaseDirectory,
                    "pending_tasks.json"),

                // 单个任务失败，不影响整体继续跑
                ContinueOnTaskError = true,

                // 停止最多等待 8 秒
                DefaultStopTimeout = TimeSpan.FromSeconds(8)
            });

            _taskManager.ConfigureStart(new TaskDispatchStartOptions
            {
                // 消费者数量
                ConsumerCount = _appSettings.MaxConcurrency,
                // 生产者方法
                Producer = ProducerAsync,
                // 消费者方法
                Consumer = ConsumerAsync
            });
            _taskManager.StateChanged += TaskManager_StateChanged;
            _taskManager.LogEmitted += TaskManager_LogEmitted;
            _taskManager.TaskEnqueued += TaskManager_TaskEnqueued;
            _taskManager.TaskDequeued += TaskManager_TaskDequeued;
            _taskManager.TaskStarted += TaskManager_TaskStarted;
            _taskManager.TaskSucceeded += TaskManager_TaskSucceeded;
            _taskManager.TaskFailed += TaskManager_TaskFailed;
            _taskManager.TaskCanceled += TaskManager_TaskCanceled;
            _taskManager.TaskDropped += TaskManager_TaskDropped;
            _taskManager.PendingTasksPersisted += TaskManager_PendingTasksPersisted;
            _taskManager.PersistedTasksLoaded += TaskManager_PersistedTasksLoaded;
            _taskManager.StatisticsChanged += TaskManager_StatisticsChanged;
            RefreshStartStopButton(_taskManager.State);

            this.FormClosing += async (s, e) =>
            {
                if (_taskManager == null)
                    return;
                if (_taskManager.State == RunnerState.Running ||
                    _taskManager.State == RunnerState.Stopping)
                {
                    e.Cancel = true;

                    btnStartStop.Enabled = false;
                    btnStartStop.Text = "停止中...";

                    try
                    {
                        await _taskManager.StopAsync(new TaskDispatchStopOptions
                        {
                            Timeout = TimeSpan.FromSeconds(8),
                            PersistPending = true
                        });
                    }
                    catch
                    {
                    }

                    e.Cancel = false;
                    Close();
                }

            };
        }

        #region 状态变化事件：更新按钮文本
        private void TaskManager_StateChanged(
        object? sender,
        RunnerStateChangedEventArgs e)
        {
            //this.InvokeOnUiThreadIfRequired(() =>
            //{
            //    RefreshStartStopButton(e.NewState);
            //    AddLog($"状态变化: {e.OldState} -> {e.NewState}");
            //});
        }
        private void RefreshStartStopButton(RunnerState state)
        {
            switch (state)
            {
                case RunnerState.Stopped:
                    btnStartStop.Enabled = true;
                    btnStartStop.Text = "开始";
                    break;

                case RunnerState.Running:
                    btnStartStop.Enabled = true;
                    btnStartStop.Text = "停止";
                    break;

                case RunnerState.Stopping:
                    btnStartStop.Enabled = false;
                    btnStartStop.Text = "停止中...";
                    break;

                case RunnerState.Faulted:
                    btnStartStop.Enabled = true;
                    btnStartStop.Text = "重新开始";
                    break;
            }
        }

        #endregion

        #region 日志事件
        private void TaskManager_LogEmitted(
        object? sender,
        DispatchLogEventArgs e)
        {
            //this.InvokeOnUiThreadIfRequired(() =>
            //{
            //    AddLog(e.ToString());

            //    if (e.Exception != null)
            //    {
            //        AddLog(e.Exception.ToString());
            //    }
            //});
        }
        #endregion

        #region 任务事件
        private void TaskManager_TaskEnqueued(
        object? sender,
        DispatchTaskEventArgs e)
        {
            //this.InvokeOnUiThreadIfRequired(() =>
            //{
            //    AddLog($"任务入队: {e.TaskId}");
            //});
        }

        private void TaskManager_TaskDequeued(
            object? sender,
            DispatchTaskEventArgs e)
        {
            //this.InvokeOnUiThreadIfRequired(() =>
            //{
            //    AddLog($"任务出队: Consumer={e.ConsumerId}, TaskId={e.TaskId}");
            //});
        }

        private void TaskManager_TaskStarted(
            object? sender,
            DispatchTaskEventArgs e)
        {
            //this.InvokeOnUiThreadIfRequired(() =>
            //{
            //    AddLog($"任务开始: Consumer={e.ConsumerId}, TaskId={e.TaskId}");
            //});
        }

        private void TaskManager_TaskSucceeded(
            object? sender,
            DispatchTaskEventArgs e)
        {
            //this.InvokeOnUiThreadIfRequired(() =>
            //{
            //    AddLog($"任务成功: Consumer={e.ConsumerId}, TaskId={e.TaskId}, 耗时={e.Elapsed?.TotalMilliseconds:0}ms");
            //});
        }

        private void TaskManager_TaskFailed(
            object? sender,
            DispatchTaskEventArgs e)
        {
            //this.InvokeOnUiThreadIfRequired(() =>
            //{
            //    AddLog($"任务失败: Consumer={e.ConsumerId}, TaskId={e.TaskId}, Error={e.Exception?.Message}");
            //});
        }

        private void TaskManager_TaskCanceled(
            object? sender,
            DispatchTaskEventArgs e)
        {
            //this.InvokeOnUiThreadIfRequired(() =>
            //{
            //    AddLog($"任务取消: Consumer={e.ConsumerId}, TaskId={e.TaskId}");
            //});
        }

        private void TaskManager_TaskDropped(
            object? sender,
            DispatchTaskEventArgs e)
        {
            //BeginInvokeSafe(() =>
            //{
            //    AddLog($"任务丢弃/待落盘: TaskId={e.TaskId}");
            //});
        }
        #endregion

        #region 任务队列的落盘/恢复
        private void TaskManager_PendingTasksPersisted(
        object? sender,
        PendingTasksPersistedEventArgs e)
        {
            BeginInvokeSafe(() =>
            {
                AddLog($"剩余任务已落盘: Count={e.Count}, File={e.FilePath}");
            });
        }

        private void TaskManager_PersistedTasksLoaded(
            object? sender,
            PersistedTasksLoadedEventArgs e)
        {
            BeginInvokeSafe(() =>
            {
                AddLog($"落盘任务已恢复: Count={e.Count}, File={e.FilePath}");
            });
        }
        #endregion

        #region 任务执行状态统计
        private void TaskManager_StatisticsChanged(
        object? sender,
        TaskDispatchSnapshot snapshot)
        {
            //BeginInvokeSafe(() =>
            //{
            //    lblQueue.Text = snapshot.QueueCount.ToString();
            //    lblSuccess.Text = snapshot.SucceededCount.ToString();
            //    lblFail.Text = snapshot.FailedCount.ToString();
            //    lblRunning.Text = snapshot.State.ToString();
            //});
        }
        #endregion

        #region 生产任务
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

        #endregion

        #region 执行任务
        private async Task ConsumerAsync(
        int consumerId,
        JToken task,
        CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (task == null)
                return;

            var taskId = task["id"]?.Value<int>();


            AddLog($"Consumer-{consumerId} 开始执行任务: {taskId}");

            try
            {
                // 模拟任务执行耗时
                await Task.Delay(5000, token).ConfigureAwait(false);

                // 这里写你的真实业务逻辑
                // await RunBrowserTaskAsync(task, token);

                AddLog($"Consumer-{consumerId} 任务完成: {taskId}");
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                AddLog($"Consumer-{consumerId} 任务取消: {taskId}");
                throw;
            }
            catch (Exception ex)
            {
                AddLog($"Consumer-{consumerId} 任务异常: {taskId}, {ex.Message}");

                // 这里可以选择 throw
                // 因为 ContinueOnTaskError = true，所以 throw 后只会算单任务失败，不会拖垮整体
                throw;
            }
        }
        #endregion

        private void AddLog(string message)
        {
            //if (IsDisposed)
            //    return;

            //var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}";

            //if (LogTextBox.IsDisposed)
            //    return;

            //LogTextBox.AppendText(line);
            // _logger.LogInformation(message);
            LogWriteLine(message);
        }

        private void BeginInvokeSafe(Action action)
        {
            if (IsDisposed)
                return;

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(action);
                }
                catch
                {
                }
            }
            else
            {
                action();
            }
        }

        #endregion

        protected override async void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            await DestroyResourcesAsync();
        }
        private async Task DestroyResourcesAsync()
        {
            await Task.CompletedTask;
        }




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

            InitTaskDispatchManager();


            LoadAppSetting();
            if (this._appSettings == null)
            {
                this._appSettings = new AppSettings();
                UpdateAppSetting();
            }

            //StartMessageProcessor();

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


            //var cachePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "chrome", "User Data");
            //if (System.IO.Directory.Exists(cachePath))
            //    FileHelper.CleanCefCache(cachePath, maxTotalSizeMb: 40_000, keepRecentDays: 7);
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
                        //this.TotalUVCount = _cnt;
                        //label5.Text = $"提交数量:{this.TotalUVCount}";
                    }
                }
            }
            if (restart_flag)
            {
                this.InvokeOnUiThreadIfRequired(() =>
                {
                    btnStartStop.PerformClick();


                });

            }
            label6.Text = "CPU:" + Environment.ProcessorCount.ToString();

            //textBox_SmsName.Text = CommonHelper.GetIpAddress();
        }







        private async void btnStartStop_Click(object sender, EventArgs e)
        {
            btnStartStop.Enabled = false;
            try
            {
                await _taskManager.ToggleAsync(new TaskDispatchStopOptions
                {
                    Timeout = TimeSpan.FromSeconds(8),
                    // 停止时保存队列中还没取出的任务
                    PersistPending = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.ToString(),
                    "任务调度异常",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                RefreshStartStopButton(_taskManager.State);
            }


            //if (buttonStart.Text.Equals("停止"))
            //{
            //    buttonStart.Enabled = false;
            //    buttonStart.Text = "停止中...";
            //    buttonStart.ForeColor = Color.Black;
            //    this.buttonStart.Enabled = false;
            //    Task.Run(StopRunningTasksAsync);
            //    return;
            //}
            //StartRunningTasks();
        }

        private void StartRunningTasks()
        {
            //UpdateAppSetting();
            //this.taskStatisticsManager.Reset();
            //SubscribeTaskStatisticsEvents(this.taskStatisticsManager);
            //this.taskDispatchManager = new TaskDispatchManager(GetTaskQueueCapacity(), LogWriteLine, ex => LogWriteLine(ex.ToString()));
            //SubscribeTaskDispatchManagerEvents(this.taskDispatchManager);
            //this.selfWndHandle = this.Handle;
            //this.processOfList = new System.Collections.Concurrent.ConcurrentDictionary<string, ProcessItem>();
            //this.processOfList.Clear();
            //this.cefProcessManager = new CefClientProcessManager(this.processOfList, LogWriteLine);
            //btnStartStop.Text = "停止";
            //btnStartStop.ForeColor = Color.Blue;
            //this.cts = new CancellationTokenSource();
            //this.cts.Token.Register(() =>
            //{
            //    btnStartStop.Enabled = false;
            //    btnStartStop.Text = "停止中...";
            //    btnStartStop.ForeColor = Color.Black;
            //    this.btnStartStop.Enabled = false;
            //});

            //#region 获取任务及执行任务
            //this.taskDispatchManager.Start(
            //    _appSettings.MaxConcurrency,
            //    ProducerAsync,
            //    ConsumerAsync,
            //    this.cts.Token);
            //#endregion

            //StartRestartGuard();
        }



        private void RecordTaskStage(JToken task, string stage, int? consumerId = null, string? message = null)
        {
            var record = this.taskStatisticsManager.Record(task, stage, consumerId, message);
            var logMessage = $"任务阶段统计：id={record.TaskId}, stage={record.Stage}, consumer={record.ConsumerId?.ToString() ?? "-"}";
            if (!string.IsNullOrWhiteSpace(record.Message))
            {
                logMessage += $", message={record.Message}";
            }
            LogInfo(logMessage);
        }

        private void RecordTaskStageFromClient(JObject message)
        {
            var taskId = message.Value<string>("TaskId")
                ?? message.Value<string>("taskId")
                ?? message.Value<string>("Id")
                ?? message.Value<string>("id");
            var stage = message.Value<string>("Stage")
                ?? message.Value<string>("stage")
                ?? message.Value<string>("Status")
                ?? message.Value<string>("status");
            var consumerId = message.Value<int?>("ConsumerId") ?? message.Value<int?>("consumerId");
            var detail = message.Value<string>("Message") ?? message.Value<string>("message");

            if (string.IsNullOrWhiteSpace(stage))
            {
                LogWriteLine("客户端任务状态消息缺少Stage/Status");
                return;
            }

            var record = this.taskStatisticsManager.Record(taskId, stage, consumerId, detail);
            LogInfo($"客户端任务阶段统计：id={record.TaskId}, stage={record.Stage}, consumer={record.ConsumerId?.ToString() ?? "-"}");
        }


        private void TaskStatisticsManager_StageChanged(object? sender, TaskStageChangedEventArgs e)
        {
            if (e.Summary.TotalStageCount % 20 == 0 ||
                string.Equals(e.Record.Stage, TaskStageNames.Complete, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(e.Record.Stage, TaskStageNames.Fail, StringComparison.OrdinalIgnoreCase))
            {
                LogInfo(this.taskStatisticsManager.BuildSummaryText());
            }
        }







        private void button1_Click(object sender, EventArgs e)
        {
            btnStartStop.Enabled = false;
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
                    btnStartStop.Enabled = true;
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
