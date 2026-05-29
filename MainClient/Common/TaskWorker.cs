using MainClient.Infrastructure;
using Newtonsoft.Json.Linq;
using System.Threading.Channels;

namespace MainClient.Common
{
    internal sealed class TaskWorker : IAsyncDisposable
    {
        private readonly AppSettings _appSettings;
        private readonly HttpClient _httpClient;
        private TaskDispatchManager _dispatchManager;
        private CancellationTokenSource _appCts;

        public TaskWorker(AppSettings appSettings)
        {
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15)
            };
        }

        public void Start()
        {
            if (_dispatchManager != null && _dispatchManager.IsStarted)
            {
                LogWriteLine("任务调度器已经启动。");
                return;
            }

            _appCts = new CancellationTokenSource();

            int capacity = _appSettings.ChannelCapacity <= 0
                ? 500
                : _appSettings.ChannelCapacity;

            int consumerCount = _appSettings.MaxConcurrency <= 0
                ? 1
                : _appSettings.MaxConcurrency;

            _dispatchManager = new TaskDispatchManager(
                capacity: capacity,
                log: LogWriteLine,
                error: ex => LogWriteLine(ex.ToString())
            );

            _dispatchManager.Start(
                consumerCount: consumerCount,
                producer: ProducerAsync,
                consumer: ConsumerAsync,
                externalToken: _appCts.Token
            );

            LogWriteLine($"任务调度器启动完成，consumerCount={consumerCount}, capacity={capacity}");
        }

        public async Task StopAsync()
        {
            try
            {
                if (_appCts != null)
                {
                    _appCts.Cancel();
                }

                if (_dispatchManager != null)
                {
                    await _dispatchManager.StopAsync(
                        timeoutMs: 10000,
                        drainPending: true
                    ).ConfigureAwait(false);
                }
            }
            finally
            {
                try
                {
                    _appCts?.Dispose();
                }
                catch
                {
                }

                _appCts = null;
                _dispatchManager = null;
            }

            LogWriteLine("任务调度器已停止。");
        }

        private async Task ProducerAsync(
            ChannelWriter<JToken> writer,
            CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    List<JToken> rawTasks;

                    try
                    {
                        rawTasks = await TaskFetchHelper.GetRawTasksAsync(
                            httpClient: _httpClient,
                            taskApiUrl: BuildTaskApiUrl(),
                            token: token
                        ).ConfigureAwait(false);
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

                    if (rawTasks.Count == 0)
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

                    foreach (var task in rawTasks)
                    {
                        token.ThrowIfCancellationRequested();

                        for (int i = 0; i < multiple; i++)
                        {
                            token.ThrowIfCancellationRequested();

                            var cloned = task.DeepClone();

                            if (cloned is JObject obj)
                            {
                                obj["_copyIndex"] = i + 1;
                                obj["_copyTotal"] = multiple;
                                obj["_dispatchTime"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                            }

                            // 重点：
                            // 正常运行时，如果 Channel 满了，这里会等待。
                            // 点击停止时，token 取消，这里会立即退出。
                            await writer.WriteAsync(cloned, token).ConfigureAwait(false);

                            writeCount++;
                        }
                    }

                    LogWriteLine($"本轮取回={rawTasks.Count}，倍率={multiple}，写入队列={writeCount}");
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
                            var copyIndex = item["_copyIndex"]?.Value<int>() ?? 1;
                            var copyTotal = item["_copyTotal"]?.Value<int>() ?? 1;

                            LogWriteLine($"Consumer-{consumerId} 开始处理任务 id={id}, copy={copyIndex}/{copyTotal}");

                            await HandleTaskAsync(
                                consumerId,
                                item,
                                token
                            ).ConfigureAwait(false);

                            LogWriteLine($"Consumer-{consumerId} 处理完成 id={id}, copy={copyIndex}/{copyTotal}");
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

        private string BuildTaskApiUrl()
        {
            var name = Uri.EscapeDataString(_appSettings.TaskName ?? "");
            var host = Uri.EscapeDataString(Environment.MachineName);
            var ver = Uri.EscapeDataString(AppConsts.AppVersion);

            return $"{_appSettings.TaskApiUrl}" +
                   $"?type=1" +
                   $"&action=getTask" +
                   $"&name={name}" +
                   $"&host={host}" +
                   $"&ver={ver}" +
                   $"&_t={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        }

        private async Task HandleTaskAsync(
            int consumerId,
            JToken item,
            CancellationToken token)
        {
            // 这里写你的实际任务逻辑
            // 比如打开 CefSharp、Playwright、请求接口、执行 UV 等

            token.ThrowIfCancellationRequested();

            await Task.Delay(1000, token).ConfigureAwait(false);

            // 示例：
            var url = item["url"]?.ToString();
            var id = item["id"]?.ToString();

            LogWriteLine($"Consumer-{consumerId} 模拟处理任务 id={id}, url={url}");
        }

        private void LogWriteLine(string message)
        {
            // 这里替换成你自己的日志方法
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}");
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync().ConfigureAwait(false);

            try
            {
                _httpClient?.Dispose();
            }
            catch
            {
            }
        }
    }
}
