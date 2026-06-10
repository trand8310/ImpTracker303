using MainClient.Common;
using MainClient.Infrastructure;
using MainClient.Ipc;
using MainClient.Logging;
using MainClient.LogViewer;
using MainClient.Scheduler;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Serilog.Events;
using System.Collections.Concurrent;
using System.Management;
using System.Threading.Channels;


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
                LoadPersistedOnStart = false,

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





        #region osr
        private readonly ConcurrentQueue<string> _osrScreenshotQueue = new();
        private readonly ConcurrentDictionary<string, OsrPreviewItem> _osrPendingScreenshots = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, byte> _osrQueuedScreenshotKeys = new(StringComparer.OrdinalIgnoreCase);
        private readonly System.Windows.Forms.Timer _osrScreenshotTimer;
        private OsrPreviewForm? _osrScreenshotPreviewForm;
        private int _osrScreenshotQueueCount;
        private int _osrScreenshotTimerStartPending;
        private const int OsrScreenshotsPerTick = 2;
        private const int OsrScreenshotQueueIntervalMs = 100;

        private readonly record struct OsrPreviewItem(
            string PreviewKey,
            string ConsumerId,
            string BrowserId,
            string ScreenshotBase64);

        private Task ShowOsrScreenshotAsync(PipeEnvelope screenshot, int consumerId)
        {
            if (_appSettings.IsHiddenMode || !_appSettings.IsOsrMode)
                return Task.CompletedTask;

            var browserId = screenshot.BrowserId;
            var base64 = screenshot.Data?["base64"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(browserId) || string.IsNullOrWhiteSpace(base64))
                return Task.CompletedTask;

            var consumerIdText = consumerId.ToString();
            var previewKey = $"consumer_{consumerIdText}";
            EnqueueOrUpdateOsrScreenshot(new OsrPreviewItem(
                previewKey,
                consumerIdText,
                browserId,
                base64));

            ScheduleOsrScreenshotDrain();
            return Task.CompletedTask;
        }

        private void EnqueueOrUpdateOsrScreenshot(OsrPreviewItem item)
        {
            _osrPendingScreenshots.AddOrUpdate(item.PreviewKey, item, (_, _) => item);

            if (!_osrQueuedScreenshotKeys.TryAdd(item.PreviewKey, 0))
                return;

            _osrScreenshotQueue.Enqueue(item.PreviewKey);
            Interlocked.Increment(ref _osrScreenshotQueueCount);
        }

        private void ScheduleOsrScreenshotDrain()
        {
            if (IsDisposed || Disposing)
                return;

            if (Interlocked.Exchange(ref _osrScreenshotTimerStartPending, 1) == 1)
                return;

            void StartTimerOnUiThread()
            {
                Interlocked.Exchange(ref _osrScreenshotTimerStartPending, 0);

                if (IsDisposed || Disposing || Volatile.Read(ref _osrScreenshotQueueCount) <= 0)
                    return;

                if (!_osrScreenshotTimer.Enabled)
                    _osrScreenshotTimer.Start();
            }

            try
            {
                if (InvokeRequired)
                    BeginInvoke((Action)StartTimerOnUiThread);
                else
                    StartTimerOnUiThread();
            }
            catch
            {
                Interlocked.Exchange(ref _osrScreenshotTimerStartPending, 0);
            }
        }

        private void DrainOsrScreenshotQueue()
        {
            if (_appSettings.IsHiddenMode || !_appSettings.IsOsrMode)
            {
                ClearOsrScreenshotQueue();
                _osrScreenshotTimer.Stop();
                return;
            }

            if (_osrScreenshotPreviewForm == null || _osrScreenshotPreviewForm.IsDisposed)
            {
                _osrScreenshotPreviewForm = new OsrPreviewForm();
                _osrScreenshotPreviewForm.FormClosed += (_, _) => _osrScreenshotPreviewForm = null;
            }

            for (var i = 0; i < OsrScreenshotsPerTick; i++)
            {
                if (!_osrScreenshotQueue.TryDequeue(out var previewKey))
                    break;

                _osrQueuedScreenshotKeys.TryRemove(previewKey, out _);
                Interlocked.Decrement(ref _osrScreenshotQueueCount);

                if (!_osrPendingScreenshots.TryRemove(previewKey, out var item))
                    continue;

                _osrScreenshotPreviewForm.ShowScreenshot(item.ConsumerId, item.BrowserId, item.ScreenshotBase64);
            }

            if (Volatile.Read(ref _osrScreenshotQueueCount) <= 0)
                _osrScreenshotTimer.Stop();
        }

        private void ClearOsrScreenshotQueue()
        {
            while (_osrScreenshotQueue.TryDequeue(out _))
            {
            }

            Interlocked.Exchange(ref _osrScreenshotQueueCount, 0);
            _osrPendingScreenshots.Clear();
            _osrQueuedScreenshotKeys.Clear();
        }

        #endregion

        #region LogWrite

        private readonly ConcurrentQueue<UiLogItem> _uiLogBuffer = new();
        private readonly System.Windows.Forms.Timer _uiTimer = new();
        private CancellationTokenSource _uiLogCts = new();
        private int _flushing = 0;
        private const int MaxFlushCount = 500;
        // 新控件
        private LogViewerUltra logViewer;
        private void StartLogConsumer()
        {
            // 初始化新控件
            logViewer = new LogViewerUltra()
            {
                Dock = DockStyle.Fill
            };
            groupBox4.Controls.Add(logViewer);

            // 后台读取日志
            Task.Run(async () =>
            {
                var reader = UiLogChannel.Channel.Reader;

                try
                {
                    await foreach (var item in reader.ReadAllAsync(_uiLogCts.Token))
                    {
                        if (_uiLogCts.IsCancellationRequested)
                            break;

                        _uiLogBuffer.Enqueue(item);
                    }
                }
                catch (OperationCanceledException) { }

            }, _uiLogCts.Token);

            // UI Timer
            _uiTimer.Interval = 200;
            _uiTimer.Tick += (_, __) =>
            {
                if (Interlocked.Exchange(ref _flushing, 1) == 1)
                    return;

                try
                {
                    FlushLogsToUi();
                }
                finally
                {
                    Interlocked.Exchange(ref _flushing, 0);
                }
            };
            _uiTimer.Start();

            this.FormClosing += (s, e) =>
            {
                try
                {
                    _uiTimer.Stop();
                    _uiLogCts.Cancel();
                    UiLogChannel.Channel.Writer.TryComplete();
                }
                catch { }
            };
        }
        private void FlushLogsToUi()
        {
            if (IsDisposed || Disposing)
                return;

            if (!IsHandleCreated || logViewer.IsDisposed)
                return;

            if (_uiLogBuffer.IsEmpty)
                return;

            int count = 0;

            while (_uiLogBuffer.TryDequeue(out var item))
            {
                logViewer.WriteLog(item.Message, ConvertLevel(item.Level));

                if (++count >= MaxFlushCount)
                    break;
            }
        }
        // 日志级别映射
        private LogLevel ConvertLevel(LogEventLevel level) => level switch
        {
            LogEventLevel.Verbose => LogLevel.Trace,
            LogEventLevel.Debug => LogLevel.Debug,
            LogEventLevel.Information => LogLevel.Information,
            LogEventLevel.Warning => LogLevel.Warning,
            LogEventLevel.Error => LogLevel.Error,
            _ => LogLevel.Information
        };

        public void LogWriteLine(string message)
        {
            _logger.LogInformation(message);
        }

        #endregion

        #region 应用设置
        private void LoadAppSetting()
        {
            textBox_ProxyIpUrl.Text = _appSettings.ProxyIpUrl;
            textBox_TaskApiUrl.Text = _appSettings.TaskApiUrl;
            textBox_DevApiUrl.Text = _appSettings.DevApiUrl;
            numericUpDown_TaskPullIntervalMs.Value = _appSettings.TaskPullIntervalMs;
            numericUpDown_UvExecutionInterval.Value = _appSettings.UvExecutionIntervalMs;
            numericUpDown_MaxConcurrency.Value = _appSettings.MaxConcurrency;
            numericUpDown_ChannelCapacity.Value = _appSettings.ChannelCapacity;
            textBox_TaskName.Text = _appSettings.TaskName;
            numericUpDown_Multiple.Value = _appSettings.Multiple;
            numericUpDown_MainProcessResetIntervalMinutes.Value = _appSettings.MainProcessResetIntervalMinutes;
            checkBox_IsHiddenMode.Checked = _appSettings.IsHiddenMode;
            checkBox_IsProxyMode.Checked = _appSettings.IsProxyMode;
            numericUpDown_IpValidityDuration.Value = _appSettings.IpValidityDuration;
            checkBox_IsDetailLog.Checked = _appSettings.IsDetailLog;
            checkBox_IsRealIp.Checked = _appSettings.IsRealIp;
            checkBox_IsCheckIp.Checked = _appSettings.IsCheckIp;
            checkBox_IsOsrMode.Checked = _appSettings.IsOsrMode;
        }
        private static object lock_config = new object();
        private void UpdateAppSetting()
        {
            lock (lock_config)
            {

                _appSettings.ProxyIpUrl = textBox_ProxyIpUrl.Text;
                _appSettings.TaskApiUrl = textBox_TaskApiUrl.Text;
                _appSettings.DevApiUrl = textBox_DevApiUrl.Text;
                _appSettings.TaskPullIntervalMs = (int)numericUpDown_TaskPullIntervalMs.Value;
                _appSettings.UvExecutionIntervalMs = (int)numericUpDown_UvExecutionInterval.Value;
                _appSettings.MaxConcurrency = (int)numericUpDown_MaxConcurrency.Value;
                _appSettings.ChannelCapacity = (int)numericUpDown_ChannelCapacity.Value;
                _appSettings.TaskName = textBox_TaskName.Text;
                _appSettings.Multiple = (int)numericUpDown_Multiple.Value;
                _appSettings.MainProcessResetIntervalMinutes = (int)numericUpDown_MainProcessResetIntervalMinutes.Value;
                _appSettings.IsHiddenMode = checkBox_IsHiddenMode.Checked;
                _appSettings.IsProxyMode = checkBox_IsProxyMode.Checked;
                _appSettings.IpValidityDuration = (int)numericUpDown_IpValidityDuration.Value;
                _appSettings.IsDetailLog = checkBox_IsDetailLog.Checked;
                _appSettings.IsRealIp = checkBox_IsRealIp.Checked;
                _appSettings.IsCheckIp = checkBox_IsCheckIp.Checked;
                _appSettings.IsOsrMode = checkBox_IsOsrMode.Checked;

                UserConfigService.Save("AppSettings", _appSettings);
            }

        }
        #endregion







        public MainForm(
            AdxHelper adxHelper,
            IpHelper ipHelper,
            ProxyTester ipTester,
            AppSettings appSettings,
            IHttpClientFactory httpClientFactory,
            ILogger<MainForm> logger)
        {
            InitializeComponent();
            this._adxHelper = adxHelper;
            this._ipHelper = ipHelper;
            this._ipTester = ipTester;
            this._appSettings = appSettings;
            this._logger = logger;
            this._httpClientFactory = httpClientFactory;

            LoadAppSetting();

            InitTaskDispatchManager();

            #region 数据初始化
            foreach (var item in new ManagementObjectSearcher("Select * from Win32_ComputerSystem").Get())
            {
                toolStripStatusLabel1.Text = $"CPU:{item["NumberOfLogicalProcessors"]}";
            }
            #endregion
        }


        private void MainForm_Load(object sender, EventArgs e)
        {
            StartLogConsumer();
            _logger.LogInformation("应用已启动");
            Task.Run(() =>
            {
                this.InvokeOnUiThreadIfRequired(() =>
                {

                    #region 控件初始化
                    var controls = new List<Control>() { groupBox2 };
                    foreach (var control in controls)
                    {
                        foreach (var c in control.Controls)
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
                            else if (c is RadioButton)
                            {
                                (c as RadioButton).Click += (s, e) =>
                                {
                                    UpdateAppSetting();
                                };
                            }
                            else if (c is ComboBox)
                            {
                                (c as ComboBox).SelectedIndexChanged += (s, e) =>
                                {
                                    UpdateAppSetting();
                                };
                            }
                        }
                    }
                    #endregion

                });
            });
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
        }
    }

}
