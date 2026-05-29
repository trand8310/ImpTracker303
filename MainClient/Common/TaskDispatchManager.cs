using System.Threading.Channels;
using Newtonsoft.Json.Linq;

namespace MainClient.Common
{
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;

    internal sealed class TaskDispatchManager : IAsyncDisposable
    {
        private readonly object _syncRoot = new object();

        private readonly Channel<JToken> _channel;
        private readonly List<Task> _consumerTasks = new List<Task>();

        private Task? _producerTask;
        private CancellationTokenSource? _internalCts;
        private CancellationTokenRegistration _externalTokenRegistration;

        private volatile bool _started;
        private volatile bool _stopping;

        public TaskDispatchManager(
            int capacity,
            Action<string>? log = null,
            Action<Exception>? error = null)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "capacity 必须大于 0");

            OnLog = log;
            OnError = error;

            var options = new BoundedChannelOptions(capacity)
            {
                // 你的生产者目前是单生产者，所以这里 true 是合理的
                SingleWriter = true,

                // 多消费者
                SingleReader = false,

                // 满了等待，防止内存无限涨
                FullMode = BoundedChannelFullMode.Wait,

                // 尽量避免生产/消费在同一同步上下文内联执行造成链式阻塞
                AllowSynchronousContinuations = false
            };

            _channel = Channel.CreateBounded<JToken>(options);
        }

        public ChannelReader<JToken> Reader => _channel.Reader;

        public ChannelWriter<JToken> Writer => _channel.Writer;

        public bool IsStarted => _started;

        public bool IsStopping => _stopping;

        public int ConsumerCount
        {
            get
            {
                lock (_syncRoot)
                {
                    return _consumerTasks.Count;
                }
            }
        }

        public Action<string>? OnLog { get; set; }

        public Action<Exception>? OnError { get; set; }

        public void Start(
            int consumerCount,
            Func<ChannelWriter<JToken>, CancellationToken, Task> producer,
            Func<int, ChannelReader<JToken>, CancellationToken, Task> consumer,
            CancellationToken externalToken = default)
        {
            if (consumerCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(consumerCount), "consumerCount 必须大于 0");

            if (producer == null)
                throw new ArgumentNullException(nameof(producer));

            if (consumer == null)
                throw new ArgumentNullException(nameof(consumer));

            lock (_syncRoot)
            {
                if (_started)
                    throw new InvalidOperationException("TaskDispatchManager 已经启动，不能重复 Start。");

                _started = true;
                _stopping = false;

                _internalCts = new CancellationTokenSource();

                if (externalToken.CanBeCanceled)
                {
                    _externalTokenRegistration = externalToken.Register(() =>
                    {
                        TryLog("外部 CancellationToken 已取消，准备停止 TaskDispatchManager。");
                        TryCancel();
                    });
                }

                var token = _internalCts.Token;

                _producerTask = Task.Run(() => RunProducerAsync(producer, token), CancellationToken.None);

                _consumerTasks.Clear();

                for (int i = 1; i <= consumerCount; i++)
                {
                    int consumerId = i;
                    var task = Task.Run(() => RunConsumerAsync(consumerId, consumer, token), CancellationToken.None);
                    _consumerTasks.Add(task);
                }

                TryLog($"TaskDispatchManager 已启动，consumerCount={consumerCount}");
            }
        }

        private async Task RunProducerAsync(
            Func<ChannelWriter<JToken>, CancellationToken, Task> producer,
            CancellationToken token)
        {
            try
            {
                await producer(_channel.Writer, token).ConfigureAwait(false);

                TryLog("Producer 正常结束。");
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                TryLog("Producer 已取消。");
            }
            catch (Exception ex)
            {
                TryError(ex);

                // 生产者异常时，必须 Complete，否则消费者可能一直等
                _channel.Writer.TryComplete(ex);
                return;
            }

            // 生产者正常结束，也通知消费者不要再等新数据
            _channel.Writer.TryComplete();
        }

        private async Task RunConsumerAsync(
            int consumerId,
            Func<int, ChannelReader<JToken>, CancellationToken, Task> consumer,
            CancellationToken token)
        {
            try
            {
                TryLog($"Consumer-{consumerId} 已启动。");

                await consumer(consumerId, _channel.Reader, token).ConfigureAwait(false);

                TryLog($"Consumer-{consumerId} 正常结束。");
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                TryLog($"Consumer-{consumerId} 已取消。");
            }
            catch (ChannelClosedException)
            {
                TryLog($"Consumer-{consumerId} 检测到 Channel 关闭。");
            }
            catch (Exception ex)
            {
                TryError(new Exception($"Consumer-{consumerId} 异常退出。", ex));

                // 消费者异常是否要停止全局，看你的业务。
                // 全天执行场景下，建议一个消费者异常就整体取消，避免剩余消费者继续跑异常数据。
                TryCancel();

                // 同时关闭 writer，让其他消费者尽快退出
                _channel.Writer.TryComplete(ex);
            }
        }

        public async Task StopAsync(int timeoutMs = 10_000, bool drainPending = false)
        {
            if (timeoutMs <= 0)
                timeoutMs = 10_000;

            List<Task> tasks;

            lock (_syncRoot)
            {
                if (!_started)
                    return;

                if (_stopping)
                    return;

                _stopping = true;

                TryLog("TaskDispatchManager 开始停止。");

                TryCancel();

                // 不再允许写入
                _channel.Writer.TryComplete();

                tasks = new List<Task>();

                if (_producerTask != null)
                    tasks.Add(_producerTask);

                if (_consumerTasks.Count > 0)
                    tasks.AddRange(_consumerTasks);
            }

            try
            {
                var allTask = Task.WhenAll(tasks);
                var delayTask = Task.Delay(timeoutMs);

                var completedTask = await Task.WhenAny(allTask, delayTask).ConfigureAwait(false);

                if (completedTask == delayTask)
                {
                    TryLog($"TaskDispatchManager 停止超时，timeoutMs={timeoutMs}");
                }
                else
                {
                    // 这里 await 一下，把异常观察掉，避免 UnobservedTaskException
                    try
                    {
                        await allTask.ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        TryError(ex);
                    }

                    TryLog("TaskDispatchManager 已全部停止。");
                }
            }
            finally
            {
                if (drainPending)
                {
                    var left = DrainPending();
                    TryLog($"TaskDispatchManager DrainPending 完成，剩余数量={left.Count}");
                }

                CleanupState();
            }
        }

        public List<JToken> DrainPending()
        {
            var remaining = new List<JToken>();

            while (_channel.Reader.TryRead(out var item))
            {
                remaining.Add(item);
            }

            return remaining;
        }

        private void TryCancel()
        {
            try
            {
                _internalCts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private void CleanupState()
        {
            lock (_syncRoot)
            {
                _externalTokenRegistration.Dispose();

                _internalCts?.Dispose();
                _internalCts = null;

                _producerTask = null;
                _consumerTasks.Clear();

                _started = false;
                _stopping = false;
            }
        }

        private void TryLog(string message)
        {
            try
            {
                OnLog?.Invoke($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}");
            }
            catch
            {
                // 日志不能影响主流程
            }
        }

        private void TryError(Exception ex)
        {
            try
            {
                OnError?.Invoke(ex);
            }
            catch
            {
                // 错误回调也不能影响主流程
            }
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync(timeoutMs: 10_000, drainPending: false).ConfigureAwait(false);
        }
    }
}
