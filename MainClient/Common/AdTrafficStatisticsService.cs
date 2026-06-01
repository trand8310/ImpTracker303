namespace MainClient.Common
{
    using global::MainClient.Infrastructure;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json.Linq;
    using System.Collections.Concurrent;
    using System.Collections.ObjectModel;

    namespace MainClient.UiTask
    {
        public enum AdTrafficTaskStateKind
        {
            Request = 0,
            Start = 1,
            DSP = 2,
            Clickthrough = 3,
            Success = 4,
            Complete = 5,
            Error = 6,
            Failure = 7,
            X5Sec = 8,
        }

        public static class AdTrafficTaskStateKindExtensions
        {
            public static string MetricName(this AdTrafficTaskStateKind kind) => kind switch
            {
                AdTrafficTaskStateKind.Request => "request",
                AdTrafficTaskStateKind.Start => "start",
                AdTrafficTaskStateKind.DSP => "dsp",
                AdTrafficTaskStateKind.Clickthrough => "click",
                AdTrafficTaskStateKind.Success => "success",
                AdTrafficTaskStateKind.Complete => "complete",
                AdTrafficTaskStateKind.Error => "error",
                AdTrafficTaskStateKind.Failure => "failure",
                AdTrafficTaskStateKind.X5Sec => "x5sec",
                _ => "unknown"
            };

            public static string DisplayName(this AdTrafficTaskStateKind kind) => kind switch
            {
                AdTrafficTaskStateKind.Request => "请求",
                AdTrafficTaskStateKind.Start => "开始",
                AdTrafficTaskStateKind.DSP => "曝光",
                AdTrafficTaskStateKind.Clickthrough => "点击",
                AdTrafficTaskStateKind.Success => "成功",
                AdTrafficTaskStateKind.Complete => "完成",
                AdTrafficTaskStateKind.Error => "错误",
                AdTrafficTaskStateKind.Failure => "失败",
                AdTrafficTaskStateKind.X5Sec => "X5Sec",
                _ => "未知"
            };
        }

        public sealed class AdTrafficStateChangedEventArgs : EventArgs
        {
            public AdTrafficStateChangedEventArgs(
                int taskId,
                AdTrafficTaskStateKind kind,
                int count,
                string? data,
                AdTrafficTaskLocalSnapshot taskSnapshot,
                AdTrafficLocalSummarySnapshot summary)
            {
                TaskId = taskId;
                Kind = kind;
                Count = count;
                Data = data;
                TaskSnapshot = taskSnapshot;
                Summary = summary;
            }

            public int TaskId { get; }

            public AdTrafficTaskStateKind Kind { get; }

            public int Count { get; }

            public string? Data { get; }

            public AdTrafficTaskLocalSnapshot TaskSnapshot { get; }

            public AdTrafficLocalSummarySnapshot Summary { get; }
        }

        public sealed class AdTrafficTaskLocalSnapshot
        {
            public int TaskId { get; init; }

            public AdTrafficTaskStateKind CurrentState { get; init; }

            public DateTimeOffset FirstSeenAt { get; init; }

            public DateTimeOffset LastUpdatedAt { get; init; }

            public long TotalCount { get; init; }

            public required IReadOnlyDictionary<AdTrafficTaskStateKind, long> StateCounts { get; init; }

            public long GetCount(AdTrafficTaskStateKind kind)
            {
                return StateCounts.TryGetValue(kind, out var value) ? value : 0;
            }
        }

        public sealed class AdTrafficLocalSummarySnapshot
        {
            public int TaskCount { get; init; }

            public long TotalCount { get; init; }

            public required IReadOnlyDictionary<AdTrafficTaskStateKind, long> StateCounts { get; init; }

            public long GetCount(AdTrafficTaskStateKind kind)
            {
                return StateCounts.TryGetValue(kind, out var value) ? value : 0;
            }
        }

        public readonly record struct AdTrafficRemoteDelta(long Start, long Dsp, long Click)
        {
            public bool IsEmpty => Start == 0 && Dsp == 0 && Click == 0;
        }

        public readonly record struct AdTrafficProxyIpDelta(long Fetched, long Consumed, string[] ConsumedIps)
        {
            public bool IsEmpty =>
                Fetched == 0 &&
                Consumed == 0 &&
                (ConsumedIps == null || ConsumedIps.Length == 0);
        }

        public sealed class AdTrafficStatisticsService : IAsyncDisposable, IDisposable
        {
            private readonly AdxHelper _adxHelper;
            private readonly AppSettings _appSettings;
            private readonly ILogger _logger;

            private readonly int _retryCount;
            private readonly TimeSpan _flushInterval;

            private readonly ConcurrentDictionary<int, TaskLocalCounter> _localTasks = new();
            private readonly ConcurrentDictionary<AdTrafficTaskStateKind, long> _localSummary = new();

            private readonly ConcurrentDictionary<int, RemoteTaskCounter> _remoteTasks = new();
            private readonly ConcurrentDictionary<int, ProxyIpCounter> _proxyIpStats = new();

            private readonly RemoteTaskCounter _hostCounter = new();

            private readonly SemaphoreSlim _lifecycleLock = new(1, 1);
            private readonly SemaphoreSlim _flushOnceLock = new(1, 1);
            private readonly SemaphoreSlim _requestSemaphore;

            private CancellationTokenSource? _runCts;
            private Task? _flushLoopTask;

            private int _state;
            private long _localTotalCount;

            private readonly ConcurrentDictionary<TaskHourKey, RemoteTaskCounter> _taskGlobalBaseline = new();
            private readonly ConcurrentDictionary<TaskHourKey, double> _taskClickRates = new();
            private readonly ConcurrentDictionary<TaskHourKey, SemaphoreSlim> _baselineInitLocks = new();

            public AdTrafficStatisticsService(
                AdxHelper adxHelper,
                AppSettings appSettings,
                ILogger<AdTrafficStatisticsService> logger,
                int maxConcurrentRequests = 5,
                int retryCount = 3,
                TimeSpan? flushInterval = null)
            {
                _adxHelper = adxHelper;
                _appSettings = appSettings;
                _logger = logger;

                _retryCount = retryCount < 0 ? 0 : retryCount;
                _flushInterval = flushInterval ?? TimeSpan.FromSeconds(1);

                if (maxConcurrentRequests <= 0)
                    maxConcurrentRequests = 5;

                _requestSemaphore = new SemaphoreSlim(maxConcurrentRequests);

                foreach (AdTrafficTaskStateKind kind in Enum.GetValues(typeof(AdTrafficTaskStateKind)))
                {
                    _localSummary.TryAdd(kind, 0);
                }
            }

            public event EventHandler<AdTrafficStateChangedEventArgs>? StateChanged;

            public bool IsStarted => Volatile.Read(ref _state) == 1;

            public bool IsStopping => Volatile.Read(ref _state) == 2;

            public bool IsStopped => Volatile.Read(ref _state) == 3;

            public bool IsDisposed => Volatile.Read(ref _state) == 4;

            public async Task StartAsync(CancellationToken cancellationToken = default)
            {
                await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    var state = Volatile.Read(ref _state);

                    if (state == 1)
                        return;

                    if (state == 4)
                        throw new ObjectDisposedException(nameof(AdTrafficStatisticsService));

                    _runCts?.Dispose();
                    _runCts = new CancellationTokenSource();

                    _flushLoopTask = Task.Run(() => FlushLoopAsync(_runCts.Token));

                    Volatile.Write(ref _state, 1);
                }
                finally
                {
                    _lifecycleLock.Release();
                }
            }

            public async Task StopAsync(CancellationToken cancellationToken = default)
            {
                await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    var state = Volatile.Read(ref _state);

                    if (state == 0 || state == 3)
                    {
                        Volatile.Write(ref _state, 3);
                        return;
                    }

                    if (state == 4)
                        return;

                    if (state == 2)
                        return;

                    Volatile.Write(ref _state, 2);
                }
                finally
                {
                    _lifecycleLock.Release();
                }

                await FlushOnceAsync(cancellationToken).ConfigureAwait(false);

                var cts = _runCts;

                if (cts != null)
                {
                    try
                    {
                        cts.Cancel();
                    }
                    catch
                    {
                    }
                }

                if (_flushLoopTask != null)
                {
                    try
                    {
                        await _flushLoopTask.ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }

                cts?.Dispose();
                _runCts = null;
                _flushLoopTask = null;

                Volatile.Write(ref _state, 3);
            }

            /// <summary>
            /// 增加任务状态。
            /// 本地统计立即生效；远端 start/dsp/click 会进入聚合 delta，定时上报。
            /// </summary>
            public void AddTaskState(
                int taskId,
                AdTrafficTaskStateKind kind,
                int count = 1,
                string? data = null,
                bool raiseEvent = true)
            {
                if (taskId <= 0 || count <= 0)
                    return;

                if (kind == AdTrafficTaskStateKind.X5Sec)
                {
                    if (!string.IsNullOrWhiteSpace(data))
                        _logger.LogWarning("X5Sec. taskId={TaskId}, data={Data}", taskId, data);

                    return;
                }

                var now = DateTimeOffset.Now;

                var localCounter = _localTasks.GetOrAdd(taskId, id => new TaskLocalCounter(id, now));
                var taskSnapshot = localCounter.Add(kind, count, now);

                _localSummary.AddOrUpdate(kind, count, (_, old) => old + count);
                Interlocked.Add(ref _localTotalCount, count);

                var remoteCounter = _remoteTasks.GetOrAdd(taskId, _ => new RemoteTaskCounter());
                remoteCounter.Add(kind, count);

                _hostCounter.Add(kind, count);

                if (raiseEvent)
                {
                    RaiseStateChanged(taskId, kind, count, data, taskSnapshot);
                }
            }

            public void AddRequest(int taskId, int count = 1)
                => AddTaskState(taskId, AdTrafficTaskStateKind.Request, count);

            public void AddStart(int taskId, int count = 1)
                => AddTaskState(taskId, AdTrafficTaskStateKind.Start, count);

            public void AddDsp(int taskId, int count = 1)
                => AddTaskState(taskId, AdTrafficTaskStateKind.DSP, count);

            public void AddClick(int taskId, int count = 1)
                => AddTaskState(taskId, AdTrafficTaskStateKind.Clickthrough, count);

            public void AddSuccess(int taskId, int count = 1)
                => AddTaskState(taskId, AdTrafficTaskStateKind.Success, count);

            public void AddComplete(int taskId, int count = 1)
                => AddTaskState(taskId, AdTrafficTaskStateKind.Complete, count);

            public void AddError(int taskId, int count = 1)
                => AddTaskState(taskId, AdTrafficTaskStateKind.Error, count);

            public void AddFailure(int taskId, int count = 1)
                => AddTaskState(taskId, AdTrafficTaskStateKind.Failure, count);

            public void AddFetchedIp(int taskId, int count = 1)
            {
                if (taskId <= 0 || count <= 0)
                    return;

                var stat = _proxyIpStats.GetOrAdd(taskId, _ => new ProxyIpCounter());
                stat.AddFetched(count);
            }

            public void AddConsumedIp(int taskId, string? ip, int count = 1)
            {
                if (taskId <= 0 || count <= 0)
                    return;

                var stat = _proxyIpStats.GetOrAdd(taskId, _ => new ProxyIpCounter());
                stat.AddConsumed(count);

                if (!string.IsNullOrWhiteSpace(ip))
                    stat.AddConsumedIp(ip);
            }

            public AdTrafficTaskLocalSnapshot? GetTaskSnapshot(int taskId)
            {
                if (taskId <= 0)
                    return null;

                return _localTasks.TryGetValue(taskId, out var counter)
                    ? counter.ToSnapshot()
                    : null;
            }

            public IReadOnlyList<AdTrafficTaskLocalSnapshot> GetTaskSnapshots()
            {
                return _localTasks.Values
                    .Select(x => x.ToSnapshot())
                    .OrderBy(x => x.TaskId)
                    .ToList();
            }

            public AdTrafficLocalSummarySnapshot GetSummary()
            {
                var dict = _localSummary
                    .OrderBy(x => x.Key)
                    .ToDictionary(x => x.Key, x => x.Value);

                return new AdTrafficLocalSummarySnapshot
                {
                    TaskCount = _localTasks.Count,
                    TotalCount = Interlocked.Read(ref _localTotalCount),
                    StateCounts = new ReadOnlyDictionary<AdTrafficTaskStateKind, long>(dict)
                };
            }

            public RemoteTaskCounter? GetRemoteTaskCounter(int taskId)
            {
                return _remoteTasks.TryGetValue(taskId, out var counter) ? counter : null;
            }

            public RemoteTaskCounter GetHostCounter()
            {
                return _hostCounter;
            }

            public void ResetLocal()
            {
                _localTasks.Clear();
                _localSummary.Clear();

                foreach (AdTrafficTaskStateKind kind in Enum.GetValues(typeof(AdTrafficTaskStateKind)))
                {
                    _localSummary.TryAdd(kind, 0);
                }

                Interlocked.Exchange(ref _localTotalCount, 0);
            }

            public async Task<double> GetClickRatioAsync(int taskId, double taskCtr = 100)
            {
                if (taskId <= 0)
                    return 0;

                var hourKey = GetHourKey();
                await EnsureTaskBaselineAsync(taskId, hourKey, taskCtr).ConfigureAwait(false);

                var key = new TaskHourKey(taskId, hourKey);
                var baseline = _taskGlobalBaseline[key];
                var current = _remoteTasks.GetOrAdd(taskId, _ => new RemoteTaskCounter());

                var totalDsp = baseline.DSP + current.DSP;

                if (totalDsp <= 0)
                    return 0;

                var totalClick = baseline.Clickthrough + current.Clickthrough;
                return totalClick / (double)totalDsp;
            }

            public async Task<bool> CanClickthroughAsync(int taskId, double taskCtr = 100)
            {
                if (taskId <= 0)
                    return false;

                var hourKey = GetHourKey();
                await EnsureTaskBaselineAsync(taskId, hourKey, taskCtr).ConfigureAwait(false);

                var key = new TaskHourKey(taskId, hourKey);
                var baseline = _taskGlobalBaseline[key];
                var current = _remoteTasks.GetOrAdd(taskId, _ => new RemoteTaskCounter());

                var rate = _taskClickRates.TryGetValue(key, out var r) ? r : taskCtr;

                if (rate <= 0)
                    return false;

                var totalDsp = baseline.DSP + current.DSP;

                if (totalDsp <= 0)
                    return true;

                var totalClick = baseline.Clickthrough + current.Clickthrough;
                var targetClick = (long)Math.Floor(totalDsp * rate * 0.01);

                return totalClick < targetClick;
            }

            public async Task FlushOnceAsync(CancellationToken cancellationToken = default)
            {
                await _flushOnceLock.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    var flushTasks = new List<Task>(64);

                    foreach (var pair in _remoteTasks)
                    {
                        var taskId = pair.Key;
                        var counter = pair.Value;

                        var delta = counter.GetDelta();

                        if (delta.IsEmpty)
                            continue;

                        var metrics = counter.ToMetricDictionary(delta);

                        if (metrics.Count == 0)
                            continue;

                        flushTasks.Add(FlushTaskStateAsync(taskId, counter, delta, metrics, cancellationToken));
                    }

                    foreach (var pair in _proxyIpStats)
                    {
                        var taskId = pair.Key;
                        var counter = pair.Value;

                        var delta = counter.GetDelta();

                        if (delta.IsEmpty)
                            continue;

                        var metrics = new Dictionary<string, long>(2);

                        if (delta.Fetched > 0)
                            metrics["fetched"] = delta.Fetched;

                        if (delta.Consumed > 0)
                            metrics["consumed"] = delta.Consumed;

                        flushTasks.Add(FlushProxyIpAsync(taskId, counter, delta, metrics, cancellationToken));
                    }

                    {
                        var hostDelta = _hostCounter.GetDelta();

                        if (!hostDelta.IsEmpty)
                        {
                            var metrics = _hostCounter.ToMetricDictionary(hostDelta);

                            if (metrics.Count > 0)
                            {
                                flushTasks.Add(FlushHostStateAsync(_hostCounter, hostDelta, metrics, cancellationToken));
                            }
                        }
                    }

                    if (flushTasks.Count > 0)
                        await Task.WhenAll(flushTasks).ConfigureAwait(false);
                }
                finally
                {
                    _flushOnceLock.Release();
                }
            }

            private async Task FlushLoopAsync(CancellationToken cancellationToken)
            {
                using var timer = new PeriodicTimer(_flushInterval);

                try
                {
                    while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
                    {
                        await FlushOnceAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "AdTrafficStatisticsService flush loop crashed.");
                }
            }

            private async Task FlushTaskStateAsync(
                int taskId,
                RemoteTaskCounter counter,
                AdTrafficRemoteDelta delta,
                Dictionary<string, long> metrics,
                CancellationToken cancellationToken)
            {
                await _requestSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    await RetryAsync(
                        () => _adxHelper.UpdateTaskStateAsync(taskId, metrics, cancellationToken),
                        _retryCount,
                        cancellationToken).ConfigureAwait(false);

                    counter.Commit(delta);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Flush task state failed. taskId={TaskId}", taskId);
                }
                finally
                {
                    _requestSemaphore.Release();
                }
            }

            private async Task FlushHostStateAsync(
                RemoteTaskCounter counter,
                AdTrafficRemoteDelta delta,
                Dictionary<string, long> metrics,
                CancellationToken cancellationToken)
            {
                await _requestSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    await RetryAsync(
                        () => _adxHelper.UpdateHostStateAsync(metrics, cancellationToken),
                        _retryCount,
                        cancellationToken).ConfigureAwait(false);

                    counter.Commit(delta);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Flush host state failed.");
                }
                finally
                {
                    _requestSemaphore.Release();
                }
            }

            private async Task FlushProxyIpAsync(
                int taskId,
                ProxyIpCounter counter,
                AdTrafficProxyIpDelta delta,
                Dictionary<string, long> metrics,
                CancellationToken cancellationToken)
            {
                await _requestSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    await RetryAsync(
                        () => _adxHelper.UpdateProxyIpStateAsync(
                            taskId,
                            metrics,
                            delta.ConsumedIps.ToList(),
                            cancellationToken),
                        _retryCount,
                        cancellationToken).ConfigureAwait(false);

                    counter.Commit(delta);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Flush proxy ip state failed. taskId={TaskId}", taskId);
                }
                finally
                {
                    _requestSemaphore.Release();
                }
            }

            private async Task RetryAsync(Func<Task> func, int retryCount, CancellationToken cancellationToken)
            {
                Exception? last = null;

                for (var attempt = 0; attempt <= retryCount; attempt++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        await func().ConfigureAwait(false);
                        return;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        last = ex;

                        if (attempt >= retryCount)
                            break;

                        await Task.Delay(500, cancellationToken).ConfigureAwait(false);
                    }
                }

                throw last ?? new InvalidOperationException("Retry failed.");
            }

            private async Task EnsureTaskBaselineAsync(int taskId, string hourKey, double taskCtr)
            {
                var key = new TaskHourKey(taskId, hourKey);

                if (_taskGlobalBaseline.ContainsKey(key))
                {
                    _taskClickRates[key] = taskCtr;
                    return;
                }

                var gate = _baselineInitLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

                await gate.WaitAsync().ConfigureAwait(false);

                try
                {
                    if (_taskGlobalBaseline.ContainsKey(key))
                    {
                        _taskClickRates[key] = taskCtr;
                        return;
                    }

                    var resp = await _adxHelper.GetTaskStatusAsync(taskId).ConfigureAwait(false);

                    var baseline = new RemoteTaskCounter();

                    if (resp != null)
                    {
                        baseline.Start = resp.SelectToken("data.start")?.Value<long>() ?? 0;
                        baseline.DSP = resp.SelectToken("data.dsp")?.Value<long>() ?? 0;
                        baseline.Clickthrough = resp.SelectToken("data.click")?.Value<long>() ?? 0;
                    }

                    _taskGlobalBaseline[key] = baseline;
                    _taskClickRates[key] = taskCtr;
                }
                finally
                {
                    gate.Release();
                }
            }

            private void RaiseStateChanged(
                int taskId,
                AdTrafficTaskStateKind kind,
                int count,
                string? data,
                AdTrafficTaskLocalSnapshot taskSnapshot)
            {
                try
                {
                    StateChanged?.Invoke(
                        this,
                        new AdTrafficStateChangedEventArgs(
                            taskId,
                            kind,
                            count,
                            data,
                            taskSnapshot,
                            GetSummary()));
                }
                catch
                {
                    // UI 或订阅方异常不能影响任务执行。
                }
            }

            public void Dispose()
            {
                DisposeAsync().AsTask().GetAwaiter().GetResult();
            }

            public async ValueTask DisposeAsync()
            {
                await _lifecycleLock.WaitAsync().ConfigureAwait(false);

                try
                {
                    if (Volatile.Read(ref _state) == 4)
                        return;
                }
                finally
                {
                    _lifecycleLock.Release();
                }

                try
                {
                    await StopAsync().ConfigureAwait(false);
                }
                finally
                {
                    Volatile.Write(ref _state, 4);

                    _runCts?.Dispose();
                    _lifecycleLock.Dispose();
                    _flushOnceLock.Dispose();
                    _requestSemaphore.Dispose();

                    foreach (var gate in _baselineInitLocks.Values)
                    {
                        gate.Dispose();
                    }
                }
            }

            private readonly record struct TaskHourKey(int TaskId, string HourKey);

            private static string? _cachedHourKey;
            private static long _cachedHourTicks;

            private static readonly long HourTicks = TimeSpan.TicksPerHour;
            private static readonly TimeSpan BeijingOffset = TimeSpan.FromHours(8);

            private static string GetHourKey()
            {
                var utcNow = DateTime.UtcNow;
                var currentHourTicks = utcNow.Ticks / HourTicks * HourTicks;

                var cachedTicks = Volatile.Read(ref _cachedHourTicks);
                var cachedKey = Volatile.Read(ref _cachedHourKey);

                if (cachedKey != null && cachedTicks == currentHourTicks)
                    return cachedKey;

                var beijingTime = new DateTime(currentHourTicks, DateTimeKind.Utc).Add(BeijingOffset);
                var newKey = beijingTime.ToString("yyyyMMddHH");

                Volatile.Write(ref _cachedHourKey, newKey);
                Volatile.Write(ref _cachedHourTicks, currentHourTicks);

                return newKey;
            }

            public sealed class RemoteTaskCounter
            {
                public long Request;
                public long Start;
                public long DSP;
                public long Clickthrough;
                public long Success;
                public long Complete;
                public long Error;
                public long Failure;

                private long _deltaStart;
                private long _deltaDsp;
                private long _deltaClick;

                public double ClickRatio
                {
                    get
                    {
                        var dsp = Interlocked.Read(ref DSP);
                        if (dsp <= 0)
                            return 0;

                        var click = Interlocked.Read(ref Clickthrough);
                        return click / (double)dsp;
                    }
                }

                public void Add(AdTrafficTaskStateKind kind, int count)
                {
                    if (count <= 0)
                        return;

                    switch (kind)
                    {
                        case AdTrafficTaskStateKind.Request:
                            Interlocked.Add(ref Request, count);
                            break;

                        case AdTrafficTaskStateKind.Start:
                            Interlocked.Add(ref Start, count);
                            Interlocked.Add(ref _deltaStart, count);
                            break;

                        case AdTrafficTaskStateKind.DSP:
                            Interlocked.Add(ref DSP, count);
                            Interlocked.Add(ref _deltaDsp, count);
                            break;

                        case AdTrafficTaskStateKind.Clickthrough:
                            Interlocked.Add(ref Clickthrough, count);
                            Interlocked.Add(ref _deltaClick, count);
                            break;

                        case AdTrafficTaskStateKind.Success:
                            Interlocked.Add(ref Success, count);
                            break;

                        case AdTrafficTaskStateKind.Complete:
                            Interlocked.Add(ref Complete, count);
                            break;

                        case AdTrafficTaskStateKind.Error:
                            Interlocked.Add(ref Error, count);
                            break;

                        case AdTrafficTaskStateKind.Failure:
                            Interlocked.Add(ref Failure, count);
                            break;
                    }
                }

                public AdTrafficRemoteDelta GetDelta()
                {
                    return new AdTrafficRemoteDelta(
                        Start: Interlocked.Read(ref _deltaStart),
                        Dsp: Interlocked.Read(ref _deltaDsp),
                        Click: Interlocked.Read(ref _deltaClick));
                }

                public void Commit(AdTrafficRemoteDelta delta)
                {
                    if (delta.Start > 0)
                        Interlocked.Add(ref _deltaStart, -delta.Start);

                    if (delta.Dsp > 0)
                        Interlocked.Add(ref _deltaDsp, -delta.Dsp);

                    if (delta.Click > 0)
                        Interlocked.Add(ref _deltaClick, -delta.Click);
                }

                public Dictionary<string, long> ToMetricDictionary(AdTrafficRemoteDelta delta)
                {
                    var dict = new Dictionary<string, long>(3);

                    if (delta.Start > 0)
                        dict["start"] = delta.Start;

                    if (delta.Dsp > 0)
                        dict["dsp"] = delta.Dsp;

                    if (delta.Click > 0)
                        dict["click"] = delta.Click;

                    return dict;
                }
            }

            private sealed class ProxyIpCounter
            {
                private long _fetched;
                private long _consumed;

                private readonly object _ipsLock = new();
                private readonly List<string> _pendingConsumedIps = new();

                public void AddFetched(long count = 1)
                {
                    if (count > 0)
                        Interlocked.Add(ref _fetched, count);
                }

                public void AddConsumed(long count = 1)
                {
                    if (count > 0)
                        Interlocked.Add(ref _consumed, count);
                }

                public void AddConsumedIp(string ip)
                {
                    if (string.IsNullOrWhiteSpace(ip))
                        return;

                    lock (_ipsLock)
                    {
                        _pendingConsumedIps.Add(ip);
                    }
                }

                public AdTrafficProxyIpDelta GetDelta()
                {
                    string[] ips;

                    lock (_ipsLock)
                    {
                        ips = _pendingConsumedIps.ToArray();
                    }

                    return new AdTrafficProxyIpDelta(
                        Fetched: Interlocked.Read(ref _fetched),
                        Consumed: Interlocked.Read(ref _consumed),
                        ConsumedIps: ips);
                }

                public void Commit(AdTrafficProxyIpDelta delta)
                {
                    if (delta.Fetched > 0)
                        Interlocked.Add(ref _fetched, -delta.Fetched);

                    if (delta.Consumed > 0)
                        Interlocked.Add(ref _consumed, -delta.Consumed);

                    if (delta.ConsumedIps != null && delta.ConsumedIps.Length > 0)
                    {
                        lock (_ipsLock)
                        {
                            var removeCount = Math.Min(delta.ConsumedIps.Length, _pendingConsumedIps.Count);

                            if (removeCount > 0)
                                _pendingConsumedIps.RemoveRange(0, removeCount);
                        }
                    }
                }
            }

            private sealed class TaskLocalCounter
            {
                private readonly object _syncRoot = new();
                private readonly Dictionary<AdTrafficTaskStateKind, long> _stateCounts = new();

                public TaskLocalCounter(int taskId, DateTimeOffset firstSeenAt)
                {
                    TaskId = taskId;
                    FirstSeenAt = firstSeenAt;
                    LastUpdatedAt = firstSeenAt;
                    CurrentState = AdTrafficTaskStateKind.Request;
                }

                private int TaskId { get; }

                private AdTrafficTaskStateKind CurrentState { get; set; }

                private DateTimeOffset FirstSeenAt { get; }

                private DateTimeOffset LastUpdatedAt { get; set; }

                private long TotalCount { get; set; }

                public AdTrafficTaskLocalSnapshot Add(
                    AdTrafficTaskStateKind kind,
                    int count,
                    DateTimeOffset occurredAt)
                {
                    lock (_syncRoot)
                    {
                        CurrentState = kind;
                        LastUpdatedAt = occurredAt;
                        TotalCount += count;

                        _stateCounts.TryGetValue(kind, out var old);
                        _stateCounts[kind] = old + count;

                        return CreateSnapshotUnsafe();
                    }
                }

                public AdTrafficTaskLocalSnapshot ToSnapshot()
                {
                    lock (_syncRoot)
                    {
                        return CreateSnapshotUnsafe();
                    }
                }

                private AdTrafficTaskLocalSnapshot CreateSnapshotUnsafe()
                {
                    var dict = _stateCounts
                        .OrderBy(x => x.Key)
                        .ToDictionary(x => x.Key, x => x.Value);

                    return new AdTrafficTaskLocalSnapshot
                    {
                        TaskId = TaskId,
                        CurrentState = CurrentState,
                        FirstSeenAt = FirstSeenAt,
                        LastUpdatedAt = LastUpdatedAt,
                        TotalCount = TotalCount,
                        StateCounts = new ReadOnlyDictionary<AdTrafficTaskStateKind, long>(dict)
                    };
                }
            }
        }
    }
}
