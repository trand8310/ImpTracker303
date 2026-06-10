using MainClient.Common;
using MainClient.Infrastructure;
using MainClient.Ipc;
using MainClient.Logging;
using MainClient.LogViewer;
using MainClient.Models;
using MainClient.Scheduler;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog.Events;
using System.Collections.Concurrent;
using System.Management;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Channels;


namespace MainClient
{
    public partial class MainForm : Form
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger _logger;
        private readonly AppSettings _appSettings;
        private readonly TrafficAggregator _aggregator;
        private readonly System.Windows.Forms.Timer _statsTimer = new();
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
            // 当前界面统计由 _statsTimer 周期性拉取 TrafficAggregator 快照统一刷新，
            // 避免高频事件直接更新 UI 造成界面抖动或跨线程访问。
        }

        private void StartStatsRefreshTimer()
        {
            _statsTimer.Interval = 1000;
            _statsTimer.Tick += (_, __) => RefreshTrafficStatsToUi();
            _statsTimer.Start();

            RefreshTrafficStatsToUi();

            this.FormClosing += (_, __) =>
            {
                _statsTimer.Stop();
            };
        }

        private void RefreshTrafficStatsToUi()
        {
            try
            {
                var host = _aggregator.GetHostSnapshot();
                var taskSnapshot = _taskManager.Snapshot;

                label_request.Text = $"请求数量:{host.Request}";
                label_start.Text = $"提交数量:{host.Start}";
                label_dsp.Text = $"曝光数量:{host.Dsp}";
                label_click.Text = $"点击数量:{host.Clickthrough} ({host.ClickRatio:P2})";
                label_time.Text = $"运行时间:{FormatElapsed(taskSnapshot.RunElapsed)}";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RefreshTrafficStatsToUi failed.");
            }
        }

        private static string FormatElapsed(TimeSpan elapsed)
        {
            if (elapsed.TotalHours >= 1)
                return $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";

            return elapsed.ToString(@"mm\:ss");
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

        private static bool TryMapBrowserStatusToTaskState(string? stage, out TrafficTaskStateKind state)
        {
            switch (stage?.Trim().ToLowerInvariant())
            {
                case "start":
                    state = TrafficTaskStateKind.Start;
                    return true;
                case "dsp":
                    state = TrafficTaskStateKind.DSP;
                    return true;
                case "click":
                    state = TrafficTaskStateKind.Clickthrough;
                    return true;
                case "success":
                    state = TrafficTaskStateKind.Success;
                    return true;
                case "error":
                    state = TrafficTaskStateKind.Error;
                    return true;
                case "failure":
                    state = TrafficTaskStateKind.Failure;
                    return true;
                case "complete":
                    state = TrafficTaskStateKind.Complete;
                    return true;
                case "x5sec":
                    state = TrafficTaskStateKind.X5Sec;
                    return true;
                default:
                    state = default;
                    return false;
            }
        }


        private JToken BuildStartPayload(ConsumerTaskContext ctx, JToken task)
        {
            return new JObject
            {
                ["taskId"] = ctx.UniqueId,
                ["taskTitle"] = ctx.TaskTitle ?? "",
                ["os"] = (int)(ctx.OS),
                ["totalUv"] = ctx.TotalUV,
                ["task"] = task.ToString()
            };
        }
        private JToken BuildRunBrowserPayload(
          ConsumerTaskContext ctx,
          JToken taskObj,
          JToken devObj,
          int consumerId,
          int uvIndex)
        {

            var timestamp = CommonHelper.UnixTimeNowSecond();

            var ua = devObj["ua"]?.Value<string>();
            var url = taskObj["url"]?.Value<string>();
            var referer = taskObj["referer"]?.Value<string>();

            //if (!string.IsNullOrWhiteSpace(referer))
            //    referer = UrlHelper.URLMacroReplacement(referer, ctx.RealIp, taskObj, devObj, ctx.OS, _appSettings, timestamp);

            //timestamp = CommonHelper.UnixTimeNowSecond();
            //if (!string.IsNullOrWhiteSpace(url))
            //    url = UrlHelper.URLMacroReplacement(url, ctx.RealIp, taskObj, devObj, ctx.OS, _appSettings, timestamp);


            return new JObject
            {
                ["taskId"] = ctx.UniqueId,
                ["taskTitle"] = ctx.TaskTitle ?? "",
                ["uvIndex"] = uvIndex,
                ["consumerId"] = consumerId,
                ["os"] = (int)(ctx.OS),
                ["device"] = devObj.DeepClone(),
                ["userAgent"] = ua,
                ["isProxyMode"] = _appSettings.IsProxyMode,
                ["proxy_server"] = ctx.ProxyServer ?? string.Empty,
                ["isHiddenMode"] = _appSettings.IsHiddenMode,
                ["task"] = taskObj.DeepClone(),
                ["url"] = url,
                ["referer"] = referer,
                // OSR 端用这些短超时防止慢页面长期占住本次 UV，影响后续任务调度。
                ["loadTimeoutMs"] = 8000,
                ["firstScreenshotDelayMs"] = 1000,
                ["finalScreenshotDelayMs"] = 1500,
                ["screenshotTimeoutMs"] = 3000,
                ["titleTimeoutMs"] = 1000,
            };
        }

        private bool ShouldStopRemainingUv(ConsumerTaskContext ctx, BrowserRunResponse result)
        {
            // 这里你先按你自己的业务判断
            // 例如 result.Data 里回了 stopRemainingUv = true
            var stop = result.Data?["stopRemainingUv"]?.Value<bool?>() ?? false;
            return stop;
        }



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
                //await Task.Delay(5000, token).ConfigureAwait(false);

                // 这里写你的真实业务逻辑
                // await RunBrowserTaskAsync(task, token);
                var devClientId = task["client"]?.Value<string>()?
                .Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault() ?? "0";

                var ctx = new ConsumerTaskContext
                {
                    TaskId = task["id"]!.Value<int>(),
                    TotalUV = task["uv"]!.Value<int>()!,
                    TotalPV = task["pv"]!.Value<int>()!,
                    DevClientId = devClientId,
                    OS = _adxHelper.GetOS(devClientId),
                    TaskTitle = task["title"]?.Value<string>() ?? string.Empty,
                    StartTime = DateTime.Now
                };

                var initDev = await GetDeviceForTaskAsync(ctx.OS, ctx.TaskId, 0, token);
                if (initDev == null)
                {
                    _logger.LogWarning("ConsumerAsync get device failed after retries. taskId={TaskId}, uv={Uv}", ctx.TaskId, 1);
                    return;
                }

                await PrepareProxyContextAsync(ctx, task, token);

                var ipTtlSeconds = _appSettings.IpValidityDuration;
                if (ipTtlSeconds <= 0)
                {
                    _logger.LogWarning("ConsumerAsync invalid IpTtl={IpTtl}, taskId={TaskId}", ipTtlSeconds, ctx.TaskId);
                    return;
                }

                bool stopRemainingUv = await ExecuteTaskByCefClientAsync(
                    ctx,
                    task,
                    consumerId,
                    initDev,
                    token);

                if (stopRemainingUv)
                {
                    _logger.LogInformation("ConsumerAsync stop remaining uv. taskId={TaskId}", ctx.TaskId);
                }

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


        private async Task<bool> ExecuteTaskByCefClientAsync(
           ConsumerTaskContext ctx,
           JToken task,
           int consumerId,
           JToken initDev,
           CancellationToken token)
        {
            ctx.UniqueId = Guid.NewGuid().ToString("D");

            var cefProcessDirectory = "CefClient";
            var cefProcessFileName = "CefClient.exe";
            var cefExePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, cefProcessDirectory, cefProcessFileName);
            _logger.LogInformation("Use {CefProcessFileName} for taskId={TaskId}, uniqueId={UniqueId}, consumer={ConsumerId}", cefProcessFileName, ctx.TaskId, ctx.UniqueId, consumerId);
            var cefConsumerId = consumerId.ToString();

            await using var session = new CefClientSession(cefExePath, TimeSpan.FromSeconds(15), cefConsumerId, _appSettings.IsHiddenMode);

            session.OnLog += message =>
            {
                _logger.LogInformation("CefClient[{TaskId}] {Message}", ctx.TaskId, message);
                return Task.CompletedTask;
            };

            session.OnBrowserScreenshot += screenshot =>
            {
                if (!string.IsNullOrWhiteSpace(screenshot.TaskId) &&
                    !string.Equals(screenshot.TaskId, ctx.UniqueId, StringComparison.OrdinalIgnoreCase))
                {
                    return Task.CompletedTask;
                }
                return Task.CompletedTask;
                //return ShowOsrScreenshotAsync(screenshot, consumerId);
            };

            session.OnBrowserStatus += status =>
            {
                if (!string.IsNullOrWhiteSpace(status.TaskId) &&
                    !string.Equals(status.TaskId, ctx.UniqueId, StringComparison.OrdinalIgnoreCase))
                {
                    return Task.CompletedTask;
                }

                var stage = status.Data?["stage"]?.Value<string>() ?? "unknown";
                var browserId = status.BrowserId ?? string.Empty;
                if (string.Equals(stage, "log", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation(
                        "CefClient[{TaskId}][{BrowserId}] {Message}",
                        ctx.TaskId,
                        browserId,
                        status.Message);
                    return Task.CompletedTask;
                }

                _logger.LogInformation(
                    "CefClient browser status. taskId={TaskId}, browserId={BrowserId}, stage={Stage}, success={Success}, msg={Message}",
                    ctx.TaskId,
                    browserId,
                    stage,
                    status.Success,
                    status.Message);

                if (TryMapBrowserStatusToTaskState(stage, out var state))
                {
                    var count = status.Data?["count"]?.Value<int?>() ?? 1;
                    _aggregator.EnqueueTaskState(new TrafficTaskStateEvent(
                        ctx.TaskId,
                        state,
                        Math.Max(1, count),
                        JsonConvert.SerializeObject(status.Data)));
                }
                return Task.CompletedTask;
            };

            var completedUvTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            int dispatchedUvCount = 0;
            int completedUvCount = 0;
            bool stopRemainingUvByResult = false;
            var inFlightBrowsers = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
            var uvRunTimeout = TimeSpan.FromSeconds(_appSettings.IpValidityDuration);

            void TryCompleteAll()
            {
                if (Volatile.Read(ref dispatchedUvCount) <= 0)
                    return;

                if (Volatile.Read(ref completedUvCount) >= Volatile.Read(ref dispatchedUvCount))
                    completedUvTcs.TrySetResult(true);
            }

            Task StartUvTimeoutWatchdogAsync(string browserId, CancellationToken watchdogToken)
            {
                return Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(uvRunTimeout, watchdogToken);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }

                    if (!inFlightBrowsers.TryRemove(browserId, out _))
                        return;

                    var done = Interlocked.Increment(ref completedUvCount);
                    _logger.LogWarning(
                        "UV run timeout fallback. taskId={TaskId}, browserId={BrowserId}, timeout={TimeoutSeconds}s, completed={Completed}/{Dispatched}",
                        ctx.TaskId,
                        browserId,
                        (int)uvRunTimeout.TotalSeconds,
                        done,
                        Volatile.Read(ref dispatchedUvCount));

                    try
                    {
                        await session.RemoveBrowserAsync(ctx.UniqueId, browserId, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex,
                            "Timeout fallback remove browser failed. taskId={TaskId}, browserId={BrowserId}",
                            ctx.TaskId,
                            browserId);
                    }

                    TryCompleteAll();
                }, CancellationToken.None);
            }

            session.OnBrowserResult += async response =>
            {
                if (!string.Equals(response.TaskId, ctx.UniqueId, StringComparison.OrdinalIgnoreCase))
                    return;

                if (string.IsNullOrWhiteSpace(response.BrowserId))
                    return;

                if (!inFlightBrowsers.TryRemove(response.BrowserId, out _))
                {
                    _logger.LogDebug(
                        "Ignore duplicated or late browserResult. taskId={TaskId}, browserId={BrowserId}",
                        ctx.TaskId,
                        response.BrowserId);
                    return;
                }

                var uvNumber = Interlocked.Increment(ref completedUvCount);
                _logger.LogInformation(
                    "RunBrowserAsync done. taskId={TaskId}, uv={Uv}, browserId={BrowserId}, success={Success}, msg={Message}",
                    ctx.TaskId,
                    uvNumber,
                    response.BrowserId,
                    response.Success,
                    response.Message);

                var result = new BrowserRunResponse
                {
                    Success = response.Success ?? false,
                    Message = response.Message ?? string.Empty,
                    Data = response.Data
                };

                if (ShouldStopRemainingUv(ctx, result))
                {
                    stopRemainingUvByResult = true;
                }

                var removedByCefClient = response.Data?["removedByCefClient"]?.Value<bool?>() ?? false;
                if (!removedByCefClient)
                {
                    try
                    {
                        await session.RemoveBrowserAsync(ctx.UniqueId, response.BrowserId, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex,
                            "RemoveBrowserAsync failed after browserResult. taskId={TaskId}, browserId={BrowserId}",
                            ctx.TaskId, response.BrowserId);
                    }
                }

                if (Volatile.Read(ref completedUvCount) >= Volatile.Read(ref dispatchedUvCount))
                {
                    completedUvTcs.TrySetResult(true);
                }
            };

            try
            {
                await session.StartAsync(token);

                var startPayload = BuildStartPayload(ctx, task);
                await session.StartTaskAsync(ctx.UniqueId, startPayload, token);

                var ipTtlSeconds = _appSettings.IpValidityDuration;
                using var ipTtlCts = new CancellationTokenSource(TimeSpan.FromSeconds(ipTtlSeconds));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, ipTtlCts.Token);
                var innerToken = linkedCts.Token;
                var uvIntervalMs = Math.Max(1000, _appSettings.UvExecutionIntervalMs <= 0 ? 1000 : _appSettings.UvExecutionIntervalMs);

                async Task<bool> WaitForDispatchedUvCompletionAsync()
                {
                    if (Volatile.Read(ref dispatchedUvCount) <= 0)
                        return false;

                    TryCompleteAll();

                    using var completionRegistration = innerToken.Register(() => completedUvTcs.TrySetCanceled(innerToken));
                    await completedUvTcs.Task;

                    return stopRemainingUvByResult;
                }

                for (int uvIndex = 0; uvIndex < ctx.TotalUV; uvIndex++)
                {
                    if (token.IsCancellationRequested)
                        return false;

                    string browserId = $"uv_{uvIndex + 1}";

                   // _aggregator.EnqueueTaskState(new AdTrafficTaskStateEvent(ctx.TaskId, AdTrafficTaskStateKind.Request, 1));
                    try
                    {
                        var dev = await GetDeviceForTaskAsync(ctx.OS, ctx.TaskId, uvIndex, innerToken);
                        if (dev == null)
                        {
                            _logger.LogWarning("GetDeviceForTaskAsync failed. taskId={TaskId}, uv={Uv}",
                                ctx.TaskId, uvIndex + 1);
                            continue;
                        }

                        NormalizeDevice(dev, ctx.OS);

                        if (!inFlightBrowsers.TryAdd(browserId, 0))
                        {
                            _logger.LogWarning(
                                "Duplicated in-flight OSR browserId. taskId={TaskId}, browserId={BrowserId}",
                                ctx.TaskId,
                                browserId);
                            continue;
                        }

                        Interlocked.Increment(ref dispatchedUvCount);
                        _ = StartUvTimeoutWatchdogAsync(browserId, innerToken);

                        try
                        {
                            // OSR 模式同样只按 UVInterval 投递 runBrowser，不等待 browserResult。
                            var uvPayload = BuildRunBrowserPayload(ctx, task, dev, consumerId, uvIndex);

                            await session.RunBrowserNoWaitAsync(
                                ctx.UniqueId,
                                browserId,
                                uvPayload,
                                innerToken);
                        }
                        catch
                        {
                            inFlightBrowsers.TryRemove(browserId, out _);
                            Interlocked.Decrement(ref dispatchedUvCount);
                            throw;
                        }

                        if (uvIndex < ctx.TotalUV - 1)
                            await Task.Delay(uvIntervalMs, innerToken);
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        return false;
                    }
                    catch (OperationCanceledException) when (ipTtlCts.IsCancellationRequested)
                    {
                        LogWriteLine($"任务 {ctx.TaskTitle}[{ctx.TaskId}] 的 IP 总有效时长已到，停止后续 UV。");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "ExecuteTaskByCefClientAsync OSR uv failed. taskId={TaskId}, uv={Uv}, consumer={ConsumerId}",
                            ctx.TaskId, uvIndex + 1, consumerId);
                    }
                }

                return await WaitForDispatchedUvCompletionAsync();

            }
            finally
            {
                try
                {
                    await session.CloseGracefullyAsync(TimeSpan.FromSeconds(2));
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "CloseGracefullyAsync failed. taskId={TaskId}", ctx.TaskId);
                }
            }
        }



        /// <summary>
        /// 获取设备
        /// </summary>
        /// <param name="os"></param>
        /// <param name="taskId"></param>
        /// <param name="uvIndex"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task<JToken?> GetDeviceForTaskAsync(OSType os, int taskId, int uvIndex, CancellationToken token)
        {
            for (int retry = 0; retry < 5; retry++)
            {
                token.ThrowIfCancellationRequested();

                var dev = await _adxHelper.GetDeviceAsync(os, 100);
                if (dev != null)
                    return dev;
            }
            return null;
        }
        /// <summary>
        /// 标准化设备信息
        /// </summary>
        /// <param name="dev"></param>
        /// <param name="os"></param>
        private void NormalizeDevice(JToken dev, OSType os)
        {
            var ua = dev["ua"]?.Value<string>() ?? string.Empty;

            if (os == OSType.ANDROID)
            {

            }
            else if (os == OSType.IOS)
            {
                dev["full_version"] = dev["osv"]?.DeepClone();
            }
            else if (os == OSType.PC)
            {
                dev["gpu"] = dev["renderer"]?.DeepClone();
                dev["vendor"] = dev["vender"]?.DeepClone();

            }
        }

        #region 代理 / IP 信息
        /// <summary>
        /// 准备代理 / IP 信息
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="task"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task PrepareProxyContextAsync(ConsumerTaskContext ctx, JToken task, CancellationToken token)
        {
            ctx.ProxyServer = null;
            ctx.RealIp = string.Empty;
            ctx.IpInfo = null;

            if (_appSettings.IsProxyMode)
            {
                if (!string.IsNullOrWhiteSpace(_appSettings.ProxyIpUrl))
                {
                    await PrepareRemoteProxyAsync(ctx, task, token);
                }
                else
                {
                    await PrepareLocalProxyAsync(ctx, token);
                }
            }
            else
            {
                await PrepareDirectNetworkIpInfoAsync(ctx, token);
            }
        }
        /// <summary>
        /// 远程代理模式
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="task"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private async Task PrepareRemoteProxyAsync(ConsumerTaskContext ctx, JToken task, CancellationToken token)
        {
            const int maxRetry = 10;
            for (int retry = 1; retry <= maxRetry; retry++)
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    //_aggregator.EnqueueFetchedIp(ctx.TaskId, 1);
                    var ipEntity = await _ipHelper.GetProxyIpAsync(task);
                    if (ipEntity == null)
                    {
                        LogWriteLine("获取IP错误");
                        await Task.Delay(Random.Shared.Next(100, 200), token);
                        continue;
                    }

                    FillProxyServerFromEntity(ctx, ipEntity);

                    if (string.IsNullOrWhiteSpace(ctx.ProxyServer) || !IsValidProxyServer(ctx.ProxyServer))
                    {
                        LogWriteLine($"IP异常,{ctx.ProxyServer}");
                        await Task.Delay(Random.Shared.Next(100, 200), token);
                        continue;
                    }

                    if (_appSettings.IsCheckIp || _appSettings.IsRealIp)
                    {
                        var ok = await TryFillIpInfoAsync(ctx, token);
                        if (!ok)
                        {
                            LogWriteLine($"无法获取IP信息,{ctx.ProxyServer}");
                            await Task.Delay(Random.Shared.Next(100, 200), token);
                            continue;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(ctx.RealIp))
                    {
                        // _aggregator.EnqueueConsumedIp(ctx.TaskId, ctx.RealIp, 1);
                    }
                    return;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    LogWriteLine($"IP异常,{ex.Message}");

                    if (ex.Message.Contains("没有满足您选择的条件IP"))
                        await Task.Delay(Random.Shared.Next(2000, 3000), token);

                    await Task.Delay(Random.Shared.Next(300, 500), token);
                }
            }

            throw new InvalidOperationException($"获取代理 IP 失败，taskId={ctx.TaskId}");
        }
        /// <summary>
        /// 本地代理模式
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private async Task PrepareLocalProxyAsync(ConsumerTaskContext ctx, CancellationToken token)
        {
            ctx.ProxyServer = "127.0.0.1:7890";

            var result = await _ipTester.TestAsync(ctx.ProxyServer);
            if (!result.IsValid)
            {
                LogWriteLine($"无法获取IP信息,{ctx.ProxyServer}");
                throw new InvalidOperationException($"无法获取IP信息,{ctx.ProxyServer}");
            }

            ApplyIpTestResult(ctx, result);
        }
        /// <summary>
        /// 非代理模式
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private async Task PrepareDirectNetworkIpInfoAsync(ConsumerTaskContext ctx, CancellationToken token)
        {
            if (!_appSettings.IsCheckIp && !_appSettings.IsRealIp)
                return;

            var result = await _ipTester.TestAsync(ctx.ProxyServer);
            if (!result.IsValid)
            {
                LogWriteLine($"无法获取IP信息,{ctx.ProxyServer}");
                throw new InvalidOperationException($"无法获取IP信息,{ctx.ProxyServer}");
            }

            ApplyIpTestResult(ctx, result);
        }
        #endregion

        #region 辅助方法：填代理 / 验证代理 / 填 IP 结果
        /// <summary>
        /// 辅助方法：填代理 / 验证代理 / 填 IP 结果
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="ipEntity"></param>
        private void FillProxyServerFromEntity(ConsumerTaskContext ctx, dynamic ipEntity)
        {
            if (ipEntity.format == IPFormat.JSON)
            {
                ctx.ProxyServer = $"{ipEntity.json["ip"]}:{ipEntity.json["port"]}";

                if (_appSettings.IsRealIp)
                {
                    ctx.RealIp =
                        ipEntity.json["rip"]?.GetValue<string>() ??
                        ipEntity.json["real_ip"]?.GetValue<string>() ??
                        ipEntity.json["realIp"]?.GetValue<string>() ??
                        string.Empty;
                }
            }
            else
            {
                ctx.ProxyServer = ipEntity.value;
                if (_appSettings.IsRealIp)
                    ctx.RealIp = ctx.ProxyServer ?? string.Empty;
            }
        }

        /// <summary>
        /// 验证代理1
        /// </summary>
        /// <param name="proxyServer"></param>
        /// <returns></returns>
        private bool IsValidProxyServer(string proxyServer)
        {
            const string pattern = @"(?:(?:[0,1]?\d?\d|2[0-4]\d|25[0-5])\.){3}(?:[0,1]?\d?\d|2[0-4]\d|25[0-5]):\d{1,5}";
            return Regex.IsMatch(proxyServer, pattern);
        }

        /// <summary>
        /// 验证代理2
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task<bool> TryFillIpInfoAsync(ConsumerTaskContext ctx, CancellationToken token)
        {
            var result = await _ipTester.TestAsync(ctx.ProxyServer);
            if (!result.IsValid)
                return false;

            ApplyIpTestResult(ctx, result);
            return true;
        }
        /// <summary>
        /// 验证代理3
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="result"></param>

        private void ApplyIpTestResult(ConsumerTaskContext ctx, dynamic result)
        {
            if (result.SuccessUrl.Equals("http://ip-api.com/json") ||
                result.SuccessUrl.Equals("http://117.21.200.221/api/dash/ipinfo.php") ||
                result.SuccessUrl.Equals("http://117.21.200.18:9000/api/dash/ipinfo.php") ||
                result.SuccessUrl.Equals("http://211.154.24.179:9000/api/dash/ipinfo.php"))
            {
                ctx.IpInfo = JsonNode.Parse(result.Data)?.AsObject();
                ctx.RealIp = ctx.IpInfo["query"]?.Value<string>() ?? string.Empty;
            }
            else
            {
                var ipJson = JsonNode.Parse(result.Data)?.AsObject();
                if (ipJson?.ContainsKey("query") == true)
                    ctx.RealIp = ipJson["query"]?.GetValue<string>() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(ctx.RealIp) && ipJson?.ContainsKey("ip") == true)
                    ctx.RealIp = ipJson["ip"]?.GetValue<string>() ?? string.Empty;

                ctx.IpInfo = new JObject
                {
                    ["query"] = ctx.RealIp
                };
            }
        }
        #endregion



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
            if (_appSettings.IsHiddenMode)
                return Task.CompletedTask;

            var browserId = screenshot.BrowserId;
            var base64 = screenshot.Data?["base64"]?.Value<string>();
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
            if (_appSettings.IsHiddenMode)
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

                UserConfigService.Save("AppSettings", _appSettings);
            }

        }
        #endregion







        public MainForm(
            TrafficAggregator aggregator,
            AdxHelper adxHelper,
            IpHelper ipHelper,
            ProxyTester ipTester,
            AppSettings appSettings,
            IHttpClientFactory httpClientFactory,
            ILogger<MainForm> logger)
        {
            InitializeComponent();
            this._aggregator = aggregator;
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

            components ??= new System.ComponentModel.Container();
            _osrScreenshotTimer = new System.Windows.Forms.Timer(components);
            _osrScreenshotTimer.Interval = OsrScreenshotQueueIntervalMs;
            _osrScreenshotTimer.Tick += (_, _) => DrainOsrScreenshotQueue();
        }


        private void MainForm_Load(object sender, EventArgs e)
        {
            StartLogConsumer();
            StartStatsRefreshTimer();
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
