using Newtonsoft.Json.Linq;
using System.Threading.Channels;

namespace MainClient.Common
{
    public enum RunnerState
    {
        Stopped = 0,
        Running = 1,
        Stopping = 2,
        Faulted = 3
    }

    public enum DispatchTaskEventKind
    {
        Enqueued = 0,
        Dequeued = 1
    }

    public sealed class RunnerStateChangedEventArgs : EventArgs
    {
        public RunnerStateChangedEventArgs(RunnerState oldState, RunnerState newState, Exception? exception = null)
        {
            OldState = oldState;
            NewState = newState;
            Exception = exception;
            OccurredAt = DateTimeOffset.Now;
        }

        public RunnerState OldState { get; }

        public RunnerState NewState { get; }

        public Exception? Exception { get; }

        public DateTimeOffset OccurredAt { get; }
    }

    public sealed class DispatchTaskEventArgs : EventArgs
    {
        public DispatchTaskEventArgs(DispatchTaskEventKind kind, JToken task, int? consumerId = null)
        {
            Kind = kind;
            Task = task;
            ConsumerId = consumerId;
            OccurredAt = DateTimeOffset.Now;
        }

        public DispatchTaskEventKind Kind { get; }

        public JToken Task { get; }

        public int? ConsumerId { get; }

        public DateTimeOffset OccurredAt { get; }

        public string? TaskId => Task["id"]?.ToString();
    }

    internal sealed class TaskDispatchManager : IAsyncDisposable
    {
        private readonly object _syncRoot = new object();

        private readonly Channel<JToken> _channel;
        private readonly ChannelReader<JToken> _notifyingReader;
        private readonly ChannelWriter<JToken> _notifyingWriter;
        private readonly List<Task> _consumerTasks = new List<Task>();

        private Task? _producerTask;
        private CancellationTokenSource? _internalCts;
        private CancellationTokenRegistration _externalTokenRegistration;
        private RunnerState _state = RunnerState.Stopped;

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
                SingleWriter = true,
                SingleReader = false,
                FullMode = BoundedChannelFullMode.Wait,
                AllowSynchronousContinuations = false
            };

            _channel = Channel.CreateBounded<JToken>(options);
            _notifyingReader = new NotifyingChannelReader(_channel.Reader, RaiseTaskDequeued);
            _notifyingWriter = new NotifyingChannelWriter(_channel.Writer, RaiseTaskEnqueued);
        }

        public event EventHandler<RunnerStateChangedEventArgs>? StateChanged;

        public event EventHandler<DispatchTaskEventArgs>? TaskReceived;

        public event EventHandler<DispatchTaskEventArgs>? TaskConsumed;

        public event EventHandler<DispatchTaskEventArgs>? TaskEnqueued;

        public event EventHandler<DispatchTaskEventArgs>? TaskDequeued;

        public ChannelReader<JToken> Reader => _notifyingReader;

        public ChannelWriter<JToken> Writer => _notifyingWriter;

        public RunnerState State => _state;

        public bool IsStarted => State == RunnerState.Running;

        public bool IsStopping => State == RunnerState.Stopping;

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
                if (_state == RunnerState.Running || _state == RunnerState.Stopping)
                    throw new InvalidOperationException("TaskDispatchManager 已经启动，不能重复 Start。");

                _internalCts = new CancellationTokenSource();
                SetState(RunnerState.Running);

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
                    var reader = new NotifyingChannelReader(_channel.Reader, item => RaiseTaskDequeued(item, consumerId));
                    var task = Task.Run(() => RunConsumerAsync(consumerId, reader, consumer, token), CancellationToken.None);
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
                await producer(_notifyingWriter, token).ConfigureAwait(false);

                TryLog("Producer 已正常结束。");
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                TryLog("Producer 已取消。");
            }
            catch (Exception ex)
            {
                Fault(ex);
                _channel.Writer.TryComplete(ex);
                return;
            }

            _channel.Writer.TryComplete();
        }

        private async Task RunConsumerAsync(
            int consumerId,
            ChannelReader<JToken> reader,
            Func<int, ChannelReader<JToken>, CancellationToken, Task> consumer,
            CancellationToken token)
        {
            try
            {
                TryLog($"Consumer-{consumerId} 已启动。");

                await consumer(consumerId, reader, token).ConfigureAwait(false);

                TryLog($"Consumer-{consumerId} 已正常结束。");
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
                Fault(new Exception($"Consumer-{consumerId} 异常退出。", ex));
                TryCancel();
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
                if (_state == RunnerState.Stopped)
                    return;

                if (_state == RunnerState.Stopping)
                    return;

                if (_state != RunnerState.Faulted)
                    SetState(RunnerState.Stopping);

                TryLog("TaskDispatchManager 开始停止。");

                TryCancel();
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
                    try
                    {
                        await allTask.ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Fault(ex);
                    }

                    TryLog("TaskDispatchManager 已全部停止。");
                }
            }
            finally
            {
                if (drainPending)
                {
                    var left = DrainPending();
                    TryLog($"TaskDispatchManager DrainPending 完成，剩余任务={left.Count}");
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

                SetState(RunnerState.Stopped);
            }
        }

        private void SetState(RunnerState newState, Exception? exception = null)
        {
            RunnerState oldState;

            lock (_syncRoot)
            {
                oldState = _state;
                if (oldState == newState)
                    return;

                _state = newState;
            }

            RaiseStateChanged(oldState, newState, exception);
        }

        private void Fault(Exception ex)
        {
            TryError(ex);
            SetState(RunnerState.Faulted, ex);
        }

        private void RaiseStateChanged(RunnerState oldState, RunnerState newState, Exception? exception)
        {
            try
            {
                StateChanged?.Invoke(this, new RunnerStateChangedEventArgs(oldState, newState, exception));
            }
            catch
            {
                // 事件订阅方异常不能影响调度器主流程。
            }
        }

        private void RaiseTaskEnqueued(JToken item)
        {
            try
            {
                var args = new DispatchTaskEventArgs(DispatchTaskEventKind.Enqueued, item);
                TaskReceived?.Invoke(this, args);
                TaskEnqueued?.Invoke(this, args);
            }
            catch
            {
                // 事件订阅方异常不能影响调度器主流程。
            }
        }

        private void RaiseTaskDequeued(JToken item, int? consumerId)
        {
            try
            {
                var args = new DispatchTaskEventArgs(DispatchTaskEventKind.Dequeued, item, consumerId);
                TaskConsumed?.Invoke(this, args);
                TaskDequeued?.Invoke(this, args);
            }
            catch
            {
                // 事件订阅方异常不能影响调度器主流程。
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
                // 日志回调异常不能影响调度器主流程。
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
                // 错误回调异常不能影响调度器主流程。
            }
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync(timeoutMs: 10_000, drainPending: false).ConfigureAwait(false);
        }

        private sealed class NotifyingChannelWriter : ChannelWriter<JToken>
        {
            private readonly ChannelWriter<JToken> _inner;
            private readonly Action<JToken> _onWritten;

            public NotifyingChannelWriter(ChannelWriter<JToken> inner, Action<JToken> onWritten)
            {
                _inner = inner;
                _onWritten = onWritten;
            }

            public override bool TryComplete(Exception? error = null) => _inner.TryComplete(error);

            public override bool TryWrite(JToken item)
            {
                var written = _inner.TryWrite(item);
                if (written)
                    _onWritten(item);

                return written;
            }

            public override ValueTask<bool> WaitToWriteAsync(CancellationToken cancellationToken = default) =>
                _inner.WaitToWriteAsync(cancellationToken);

            public override async ValueTask WriteAsync(JToken item, CancellationToken cancellationToken = default)
            {
                await _inner.WriteAsync(item, cancellationToken).ConfigureAwait(false);
                _onWritten(item);
            }
        }

        private sealed class NotifyingChannelReader : ChannelReader<JToken>
        {
            private readonly ChannelReader<JToken> _inner;
            private readonly Action<JToken> _onRead;

            public NotifyingChannelReader(ChannelReader<JToken> inner, Action<JToken> onRead)
            {
                _inner = inner;
                _onRead = onRead;
            }

            public override Task Completion => _inner.Completion;

            public override bool CanCount => _inner.CanCount;

            public override bool CanPeek => _inner.CanPeek;

            public override int Count => _inner.Count;

            public override bool TryPeek(out JToken item) => _inner.TryPeek(out item);

            public override async ValueTask<JToken> ReadAsync(CancellationToken cancellationToken = default)
            {
                var item = await _inner.ReadAsync(cancellationToken).ConfigureAwait(false);
                _onRead(item);
                return item;
            }

            public override bool TryRead(out JToken item)
            {
                var read = _inner.TryRead(out item);
                if (read)
                    _onRead(item);

                return read;
            }

            public override ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default) =>
                _inner.WaitToReadAsync(cancellationToken);
        }
    }
}
