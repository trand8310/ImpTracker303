
using System.Runtime.InteropServices;
using System.Threading.Channels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace MainClient.AdxImp
{
    /// <summary>
    /// WinForm WM_COPYDATA JSON 消息总线。
    /// 负责：
    /// 1. 接收 WM_COPYDATA
    /// 2. 快速入队
    /// 3. 后台单线程顺序消费
    /// 4. JSON 解析
    /// 5. 根据 Msg 触发事件
    /// 6. 发送 JSON 消息到子进程窗口
    /// </summary>
    public sealed class WinCopyDataMessageBus : IDisposable
    {
        private readonly Channel<string> _messageChannel;
        private readonly SemaphoreSlim _sendSemaphore;
        private readonly WinCopyDataMessageBusOptions _options;
        private readonly SynchronizationContext _syncContext;

        private CancellationTokenSource _cts;
        private Task _processingTask = Task.CompletedTask;
        private volatile bool _started;
        private bool _disposed;

        public event EventHandler<ClientRawMessageEventArgs> RawMessageReceived;

        public event EventHandler<ClientStartedEventArgs> ClientStarted;

        public event EventHandler<TaskStageMessageEventArgs> TaskStageChanged;

        public event EventHandler<UnknownClientMessageEventArgs> UnknownMessageReceived;

        public event EventHandler<MessageBusLogEventArgs> LogReceived;

        public event EventHandler<MessageBusErrorEventArgs> ErrorReceived;

        public WinCopyDataMessageBus(WinCopyDataMessageBusOptions options = null)
        {
            _options = options ?? new WinCopyDataMessageBusOptions();

            if (_options.SendConcurrency <= 0)
            {
                _options.SendConcurrency = Math.Max(8, Math.Min(64, Environment.ProcessorCount * 4));
            }

            _sendSemaphore = new SemaphoreSlim(_options.SendConcurrency, _options.SendConcurrency);

            if (_options.RaiseEventsOnCapturedContext)
            {
                _syncContext = SynchronizationContext.Current;
            }

            if (_options.ChannelCapacity > 0)
            {
                var channelOptions = new BoundedChannelOptions(_options.ChannelCapacity)
                {
                    SingleReader = true,
                    SingleWriter = false,
                    FullMode = _options.FullMode,
                    AllowSynchronousContinuations = false
                };

                _messageChannel = Channel.CreateBounded<string>(channelOptions);
            }
            else
            {
                var channelOptions = new UnboundedChannelOptions
                {
                    SingleReader = true,
                    SingleWriter = false,
                    AllowSynchronousContinuations = false
                };

                _messageChannel = Channel.CreateUnbounded<string>(channelOptions);
            }
        }

        public bool IsStarted => _started;

        /// <summary>
        /// 启动后台消息消费。
        /// 建议 Form_Load 或构造完成后调用一次。
        /// </summary>
        public void Start()
        {
            ThrowIfDisposed();

            if (_started)
            {
                return;
            }

            _started = true;
            _cts = new CancellationTokenSource();
            _processingTask = Task.Run(() => ProcessMessagesAsync(_cts.Token));

            RaiseLog("消息处理器已启动");
        }

        /// <summary>
        /// 停止后台消息消费。
        /// </summary>
        public async Task StopAsync()
        {
            if (!_started)
            {
                return;
            }

            _started = false;

            try
            {
                _cts?.Cancel();
                _messageChannel.Writer.TryComplete();

                if (_processingTask != null)
                {
                    await _processingTask.ConfigureAwait(false);
                }

                RaiseLog("消息处理器已停止");
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                RaiseError("停止消息处理器异常", ex, null);
            }
        }

        /// <summary>
        /// 给 Form.DefWndProc 调用。
        /// 如果当前消息是 WM_COPYDATA，则返回 true，表示已经处理。
        /// 如果不是，则返回 false，外部继续 base.DefWndProc。
        /// </summary>
        public bool TryHandleWndProc(ref Message m)
        {
            if (m.Msg != WinCopyDataNative.WM_COPYDATA)
            {
                return false;
            }

            try
            {
                var data = Marshal.PtrToStructure<COPYDATASTRUCT>(m.LParam);
                var rawMessage = data.lpData;

                if (string.IsNullOrWhiteSpace(rawMessage))
                {
                    m.Result = new IntPtr(1);
                    return true;
                }

                if (_options.MaxMessageChars > 0 && rawMessage.Length > _options.MaxMessageChars)
                {
                    RaiseLog($"WM_COPYDATA 消息过长，已忽略。Length={rawMessage.Length}");
                    m.Result = IntPtr.Zero;
                    return true;
                }

                if (!_started)
                {
                    RaiseLog("消息处理器未启动，WM_COPYDATA 消息被忽略");
                    m.Result = IntPtr.Zero;
                    return true;
                }

                if (!_messageChannel.Writer.TryWrite(rawMessage))
                {
                    RaiseLog("消息队列写入失败，可能队列已满或已关闭");
                    m.Result = IntPtr.Zero;
                    return true;
                }

                m.Result = new IntPtr(1);
                return true;
            }
            catch (Exception ex)
            {
                RaiseError("WM_COPYDATA 消息入队失败", ex, null);
                m.Result = IntPtr.Zero;
                return true;
            }
        }

        /// <summary>
        /// 发送 JSON 对象到指定窗口。
        /// 外部统一用这个方法即可。
        /// </summary>
        public async Task<CopyDataSendResult> SendJsonAsync(
            IntPtr targetWindowHandle,
            IntPtr senderWindowHandle,
            object payload,
            int timeoutMs = 3000,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (targetWindowHandle == IntPtr.Zero)
            {
                return CopyDataSendResult.Fail("目标窗口句柄为空", 0);
            }

            if (payload == null)
            {
                return CopyDataSendResult.Fail("发送内容不能为空", 0);
            }

            string message;

            try
            {
                message = payload is string s
                    ? s
                    : JsonConvert.SerializeObject(payload);
            }
            catch (Exception ex)
            {
                RaiseError("序列化发送消息失败", ex, null);
                return CopyDataSendResult.Fail("序列化发送消息失败：" + ex.Message, 0);
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return CopyDataSendResult.Fail("发送消息为空", 0);
            }

            if (_options.MaxMessageChars > 0 && message.Length > _options.MaxMessageChars)
            {
                return CopyDataSendResult.Fail($"发送消息过长。Length={message.Length}", 0);
            }

            var cds = new COPYDATASTRUCT
            {
                dwData = new IntPtr(_options.CopyDataType),
                lpData = message,
                cbData = (message.Length + 1) * 2
            };

            await _sendSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                IntPtr sendResult;

                var ret = WinCopyDataNative.SendMessageTimeout(
                    targetWindowHandle,
                    WinCopyDataNative.WM_COPYDATA,
                    senderWindowHandle,
                    ref cds,
                    WinCopyDataNative.SMTO_ABORTIFHUNG,
                    timeoutMs,
                    out sendResult);

                if (ret == IntPtr.Zero)
                {
                    var error = Marshal.GetLastWin32Error();
                    var errorMessage = $"消息发送失败或超时。Hwnd={targetWindowHandle}, Error={error}";
                    RaiseLog(errorMessage);

                    return CopyDataSendResult.Fail(errorMessage, error);
                }

                return CopyDataSendResult.Ok(sendResult);
            }
            catch (Exception ex)
            {
                RaiseError("发送 WM_COPYDATA 消息异常", ex, message);
                return CopyDataSendResult.Fail("发送 WM_COPYDATA 消息异常：" + ex.Message, 0);
            }
            finally
            {
                _sendSemaphore.Release();
            }
        }

        private async Task ProcessMessagesAsync(CancellationToken token)
        {
            try
            {
                while (await _messageChannel.Reader.WaitToReadAsync(token).ConfigureAwait(false))
                {
                    while (_messageChannel.Reader.TryRead(out var rawMessage))
                    {
                        token.ThrowIfCancellationRequested();
                        ResolveMessage(rawMessage);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                RaiseError("客户端消息处理队列异常", ex, null);
            }
        }

        private void ResolveMessage(string rawMessage)
        {
            if (string.IsNullOrWhiteSpace(rawMessage))
            {
                return;
            }

            try
            {
                RaiseEvent(RawMessageReceived, new ClientRawMessageEventArgs(rawMessage));

                var json = JObject.Parse(rawMessage);
                var msg = json.Value<string>("Msg") ?? string.Empty;

                if (string.IsNullOrWhiteSpace(msg))
                {
                    RaiseLog("客户端消息缺少 Msg 字段：" + rawMessage);
                    return;
                }

                if (string.Equals(msg, "CLIENT_STARTED", StringComparison.OrdinalIgnoreCase))
                {
                    HandleClientStarted(json, rawMessage);
                    return;
                }

                if (string.Equals(msg, "TASK_STATUS", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(msg, "TASK_STAGE", StringComparison.OrdinalIgnoreCase))
                {
                    HandleTaskStage(json, rawMessage);
                    return;
                }

                RaiseEvent(UnknownMessageReceived, new UnknownClientMessageEventArgs(msg, json, rawMessage));
            }
            catch (JsonException ex)
            {
                RaiseError("客户端消息 JSON 解析失败", ex, rawMessage);
            }
            catch (Exception ex)
            {
                RaiseError("客户端消息处理失败", ex, rawMessage);
            }
        }

        private void HandleClientStarted(JObject json, string rawMessage)
        {
            var clientId = json.Value<string>("ClientId");

            if (string.IsNullOrWhiteSpace(clientId))
            {
                RaiseLog("客户端注册消息缺少 ClientId");
                return;
            }

            var clientHandleValue = json.Value<long?>("ClientHandle");

            if (!clientHandleValue.HasValue || clientHandleValue.Value == 0)
            {
                RaiseLog($"客户端注册消息句柄无效：ClientId={clientId}");
                return;
            }

            var args = new ClientStartedEventArgs(
                clientId,
                new IntPtr(clientHandleValue.Value),
                json,
                rawMessage);

            RaiseEvent(ClientStarted, args);
        }

        private void HandleTaskStage(JObject json, string rawMessage)
        {
            var msg = json.Value<string>("Msg") ?? string.Empty;
            var clientId = json.Value<string>("ClientId") ?? string.Empty;

            var args = new TaskStageMessageEventArgs(
                msg,
                clientId,
                json,
                rawMessage);

            RaiseEvent(TaskStageChanged, args);
        }

        private void RaiseLog(string message)
        {
            RaiseEvent(LogReceived, new MessageBusLogEventArgs(message));
        }

        private void RaiseError(string message, Exception exception, string rawMessage)
        {
            RaiseEvent(ErrorReceived, new MessageBusErrorEventArgs(message, exception, rawMessage));
        }

        private void RaiseEvent<TEventArgs>(EventHandler<TEventArgs> handler, TEventArgs args)
            where TEventArgs : EventArgs
        {
            var temp = handler;

            if (temp == null)
            {
                return;
            }

            if (_syncContext != null)
            {
                _syncContext.Post(_ => temp(this, args), null);
            }
            else
            {
                temp(this, args);
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WinCopyDataMessageBus));
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            try
            {
                _cts?.Cancel();
                _messageChannel.Writer.TryComplete();
            }
            catch
            {
            }

            try
            {
                _sendSemaphore.Dispose();
                _cts?.Dispose();
            }
            catch
            {
            }
        }
    }

    public sealed class WinCopyDataMessageBusOptions
    {
        /// <summary>
        /// 0 表示无界队列。
        /// 高并发下建议 5000 或 10000，避免内存无限增长。
        /// </summary>
        public int ChannelCapacity { get; set; } = 0;

        public BoundedChannelFullMode FullMode { get; set; } = BoundedChannelFullMode.DropOldest;

        public int SendConcurrency { get; set; } = Math.Max(8, Math.Min(64, Environment.ProcessorCount * 4));

        public int CopyDataType { get; set; } = 100;

        /// <summary>
        /// 单条消息最大字符数。
        /// 0 表示不限制。
        /// </summary>
        public int MaxMessageChars { get; set; } = 1024 * 1024;

        /// <summary>
        /// true 时，事件回到创建该类时的线程上下文。
        /// WinForm 里建议 true，这样事件里可以直接改 UI。
        /// 注意：这个类需要在 UI 线程创建。
        /// </summary>
        public bool RaiseEventsOnCapturedContext { get; set; } = true;
    }

    public sealed class ClientRawMessageEventArgs : EventArgs
    {
        public ClientRawMessageEventArgs(string rawMessage)
        {
            RawMessage = rawMessage;
        }

        public string RawMessage { get; }
    }

    public sealed class ClientStartedEventArgs : EventArgs
    {
        public ClientStartedEventArgs(string clientId, IntPtr clientHandle, JObject json, string rawMessage)
        {
            ClientId = clientId;
            ClientHandle = clientHandle;
            Json = json;
            RawMessage = rawMessage;
        }

        public string ClientId { get; }

        public IntPtr ClientHandle { get; }

        public JObject Json { get; }

        public string RawMessage { get; }
    }

    public sealed class TaskStageMessageEventArgs : EventArgs
    {
        public TaskStageMessageEventArgs(string msg, string clientId, JObject json, string rawMessage)
        {
            Msg = msg;
            ClientId = clientId;
            Json = json;
            RawMessage = rawMessage;
        }

        public string Msg { get; }

        public string ClientId { get; }

        public JObject Json { get; }

        public string RawMessage { get; }

        public string Stage => Json.Value<string>("Stage") ?? string.Empty;

        public string Status => Json.Value<string>("Status") ?? string.Empty;

        public string TaskId => Json.Value<string>("TaskId") ?? string.Empty;

        public string Message => Json.Value<string>("Message") ?? string.Empty;

        public int? Code => Json.Value<int?>("Code");
    }

    public sealed class UnknownClientMessageEventArgs : EventArgs
    {
        public UnknownClientMessageEventArgs(string msg, JObject json, string rawMessage)
        {
            Msg = msg;
            Json = json;
            RawMessage = rawMessage;
        }

        public string Msg { get; }

        public JObject Json { get; }

        public string RawMessage { get; }
    }

    public sealed class MessageBusLogEventArgs : EventArgs
    {
        public MessageBusLogEventArgs(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }

    public sealed class MessageBusErrorEventArgs : EventArgs
    {
        public MessageBusErrorEventArgs(string message, Exception exception, string rawMessage)
        {
            Message = message;
            Exception = exception;
            RawMessage = rawMessage;
        }

        public string Message { get; }

        public Exception Exception { get; }

        public string RawMessage { get; }
    }

    public sealed class CopyDataSendResult
    {
        private CopyDataSendResult(bool success, IntPtr result, string errorMessage, int win32Error)
        {
            Success = success;
            Result = result;
            ErrorMessage = errorMessage;
            Win32Error = win32Error;
        }

        public bool Success { get; }

        public IntPtr Result { get; }

        public string ErrorMessage { get; }

        public int Win32Error { get; }

        public static CopyDataSendResult Ok(IntPtr result)
        {
            return new CopyDataSendResult(true, result, null, 0);
        }

        public static CopyDataSendResult Fail(string errorMessage, int win32Error)
        {
            return new CopyDataSendResult(false, IntPtr.Zero, errorMessage, win32Error);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct COPYDATASTRUCT
    {
        public IntPtr dwData;

        public int cbData;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpData;
    }

    public static class WinCopyDataNative
    {
        public const int WM_COPYDATA = 0x004A;

        public const int SMTO_ABORTIFHUNG = 0x0002;

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessageTimeout(
            IntPtr hWnd,
            int Msg,
            IntPtr wParam,
            ref COPYDATASTRUCT lParam,
            int fuFlags,
            int uTimeout,
            out IntPtr lpdwResult);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessage(
            IntPtr hWnd,
            int Msg,
            IntPtr wParam,
            ref COPYDATASTRUCT lParam);
    }
}