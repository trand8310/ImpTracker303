using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MainClient.Common
{
    internal sealed class AppRestartGuard
    {
        private readonly int _mainResetIntervalMinutes;
        private readonly bool _sendSms;
        private readonly int _sendSmsTimeoutMinutes;
        private readonly Action<string> _log;
        private readonly Action<string, string> _sendSmsAction;

        public AppRestartGuard(int mainResetIntervalMinutes, bool sendSms, int sendSmsTimeoutMinutes, Action<string> log, Action<string, string> sendSmsAction)
        {
            _mainResetIntervalMinutes = mainResetIntervalMinutes;
            _sendSms = sendSms;
            _sendSmsTimeoutMinutes = sendSmsTimeoutMinutes;
            _log = log;
            _sendSmsAction = sendSmsAction;
        }

        public async Task WaitForRestartAsync(CancellationToken token, string smsName, string smsPhone)
        {
            int detectedLock = 0;
            int appRandomSeek = _mainResetIntervalMinutes * 60 + new Random(Guid.NewGuid().GetHashCode()).Next(-30, 30);
            while (!token.IsCancellationRequested)
            {
                var process = Process.GetCurrentProcess();
                var totalSeconds = (int)(DateTime.Now - process.StartTime).TotalSeconds;
                if (_mainResetIntervalMinutes > 0 && totalSeconds > appRandomSeek)
                {
                    _log?.Invoke("将重启应用程序");
                    return;
                }
                detectedLock++;
                if (_sendSms && _sendSmsTimeoutMinutes > 0 && detectedLock > _sendSmsTimeoutMinutes * 60)
                {
                    detectedLock = 1;
                    _sendSmsAction?.Invoke(smsName, smsPhone);
                    _log?.Invoke("检测到超时,发送短信");
                    await Task.Delay(TimeSpan.FromMinutes(30), token);
                }
                await Task.Delay(1000, token);
            }
        }
    }
}
