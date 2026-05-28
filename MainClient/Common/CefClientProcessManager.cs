using MainClient.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MainClient.Common
{
    internal sealed class CefClientProcessManager
    {
        private readonly ConcurrentDictionary<string, ProcessItem> _processes;
        private readonly Action<string> _log;

        public CefClientProcessManager(ConcurrentDictionary<string, ProcessItem> processes, Action<string> log)
        {
            _processes = processes;
            _log = log;
        }

        public void Register(string uuid, ProcessItem item)
        {
            _processes.TryAdd(uuid, item);
        }

        public void UpdateWindowHandle(string uuid, int hwnd)
        {
            if (_processes.TryGetValue(uuid, out var client))
            {
                _processes.AddOrUpdate(uuid, client, (key, oldValue) =>
                {
                    oldValue.ClientWindowHandle = hwnd;
                    return oldValue;
                });
            }
        }

        public void Remove(string uuid)
        {
            _processes.TryRemove(uuid, out _);
        }

        public int Count => _processes.Count;

        public IReadOnlyCollection<ProcessItem> Items => _processes.Values.ToList();

        public void KillAll()
        {
            foreach (var p in _processes.Values)
            {
                TryKillByPid(p.ProcessId);
            }
            _processes.Clear();
        }

        public void TryKillByPid(int pid)
        {
            try
            {
                var proc = Process.GetProcessById(pid);
                if (proc != null && !proc.HasExited)
                {
                    proc.Kill();
                }
            }
            catch (Exception ex)
            {
                _log?.Invoke(ex.Message);
                CommonHelper.KillProcExec(pid);
            }
        }
    }
}
