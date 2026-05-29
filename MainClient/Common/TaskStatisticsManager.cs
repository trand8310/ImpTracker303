using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;

namespace MainClient.Common
{
    internal static class TaskStageNames
    {
        public const string Start = "开始";
        public const string Exposure = "曝光";
        public const string Click = "点击";
        public const string Complete = "完成";
        public const string Fail = "失败";

        public static readonly IReadOnlyList<string> Defaults = new[]
        {
            Start,
            Exposure,
            Click,
            Complete,
            Fail
        };
    }

    internal sealed class TaskStageChangedEventArgs : EventArgs
    {
        public TaskStageChangedEventArgs(TaskStageRecord record, TaskStatisticsSnapshot taskSnapshot, TaskStatisticsSummary summary)
        {
            Record = record;
            TaskSnapshot = taskSnapshot;
            Summary = summary;
        }

        public TaskStageRecord Record { get; }

        public TaskStatisticsSnapshot TaskSnapshot { get; }

        public TaskStatisticsSummary Summary { get; }
    }

    internal sealed class TaskStageRecord
    {
        public required string TaskId { get; init; }

        public required string Stage { get; init; }

        public int? ConsumerId { get; init; }

        public DateTimeOffset OccurredAt { get; init; }

        public string? Message { get; init; }
    }

    internal sealed class TaskStatisticsSnapshot
    {
        public required string TaskId { get; init; }

        public required string CurrentStage { get; init; }

        public DateTimeOffset FirstSeenAt { get; init; }

        public DateTimeOffset LastUpdatedAt { get; init; }

        public int TotalStageCount { get; init; }

        public required IReadOnlyDictionary<string, long> StageCounts { get; init; }
    }

    internal sealed class TaskStatisticsSummary
    {
        public int TaskCount { get; init; }

        public long TotalStageCount { get; init; }

        public required IReadOnlyDictionary<string, long> StageCounts { get; init; }
    }

    internal sealed class TaskStatisticsManager
    {
        private readonly ConcurrentDictionary<string, TaskCounter> _tasks = new ConcurrentDictionary<string, TaskCounter>(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, long> _summaryStageCounts = new ConcurrentDictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        private long _totalStageCount;

        public TaskStatisticsManager()
        {
            foreach (var stage in TaskStageNames.Defaults)
            {
                RegisterStage(stage);
            }
        }

        public event EventHandler<TaskStageChangedEventArgs>? StageChanged;

        public void Reset()
        {
            _tasks.Clear();
            _summaryStageCounts.Clear();
            Interlocked.Exchange(ref _totalStageCount, 0);

            foreach (var stage in TaskStageNames.Defaults)
            {
                RegisterStage(stage);
            }
        }

        public void RegisterStage(string stage)
        {
            var normalizedStage = NormalizeStage(stage);
            _summaryStageCounts.TryAdd(normalizedStage, 0);
        }

        public TaskStageRecord Record(JToken? task, string stage, int? consumerId = null, string? message = null)
        {
            return Record(GetTaskId(task), stage, consumerId, message);
        }

        public TaskStageRecord Record(string? taskId, string stage, int? consumerId = null, string? message = null)
        {
            var normalizedTaskId = NormalizeTaskId(taskId);
            var normalizedStage = NormalizeStage(stage);
            var occurredAt = DateTimeOffset.Now;

            RegisterStage(normalizedStage);
            var taskCounter = _tasks.GetOrAdd(normalizedTaskId, id => new TaskCounter(id, occurredAt));
            var taskSnapshot = taskCounter.Record(normalizedStage, occurredAt);

            _summaryStageCounts.AddOrUpdate(normalizedStage, 1, (_, value) => value + 1);
            Interlocked.Increment(ref _totalStageCount);

            var record = new TaskStageRecord
            {
                TaskId = normalizedTaskId,
                Stage = normalizedStage,
                ConsumerId = consumerId,
                OccurredAt = occurredAt,
                Message = message
            };

            RaiseStageChanged(record, taskSnapshot, GetSummary());
            return record;
        }

        public TaskStatisticsSnapshot? GetTaskSnapshot(string? taskId)
        {
            var normalizedTaskId = NormalizeTaskId(taskId);
            return _tasks.TryGetValue(normalizedTaskId, out var counter) ? counter.ToSnapshot() : null;
        }

        public IReadOnlyList<TaskStatisticsSnapshot> GetTaskSnapshots()
        {
            return _tasks.Values
                .Select(counter => counter.ToSnapshot())
                .OrderBy(snapshot => snapshot.TaskId, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public TaskStatisticsSummary GetSummary()
        {
            var stageCounts = _summaryStageCounts
                .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

            return new TaskStatisticsSummary
            {
                TaskCount = _tasks.Count,
                TotalStageCount = Interlocked.Read(ref _totalStageCount),
                StageCounts = new ReadOnlyDictionary<string, long>(stageCounts)
            };
        }

        public string BuildSummaryText()
        {
            var summary = GetSummary();
            var stages = string.Join("，", summary.StageCounts.Select(pair => $"{pair.Key}:{pair.Value}"));
            return $"任务统计汇总：任务数={summary.TaskCount}，阶段记录数={summary.TotalStageCount}，{stages}";
        }

        public static string GetTaskId(JToken? task)
        {
            if (task == null)
            {
                return "-";
            }

            foreach (var fieldName in new[] { "id", "taskId", "TaskId", "task_id", "uuid" })
            {
                var value = task[fieldName]?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            var rawTask = task.ToString(Newtonsoft.Json.Formatting.None);
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawTask));
            return Convert.ToHexString(hashBytes, 0, 8);
        }

        private static string NormalizeTaskId(string? taskId)
        {
            return string.IsNullOrWhiteSpace(taskId) ? "-" : taskId.Trim();
        }

        private static string NormalizeStage(string stage)
        {
            return string.IsNullOrWhiteSpace(stage) ? "未命名状态" : stage.Trim();
        }

        private void RaiseStageChanged(TaskStageRecord record, TaskStatisticsSnapshot taskSnapshot, TaskStatisticsSummary summary)
        {
            try
            {
                StageChanged?.Invoke(this, new TaskStageChangedEventArgs(record, taskSnapshot, summary));
            }
            catch
            {
                // 统计事件订阅方异常不能影响任务消费流程。
            }
        }

        private sealed class TaskCounter
        {
            private readonly object _syncRoot = new object();
            private readonly Dictionary<string, long> _stageCounts = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

            public TaskCounter(string taskId, DateTimeOffset firstSeenAt)
            {
                TaskId = taskId;
                FirstSeenAt = firstSeenAt;
                LastUpdatedAt = firstSeenAt;
                CurrentStage = string.Empty;
            }

            private string TaskId { get; }

            private string CurrentStage { get; set; }

            private DateTimeOffset FirstSeenAt { get; }

            private DateTimeOffset LastUpdatedAt { get; set; }

            private int TotalStageCount { get; set; }

            public TaskStatisticsSnapshot Record(string stage, DateTimeOffset occurredAt)
            {
                lock (_syncRoot)
                {
                    CurrentStage = stage;
                    LastUpdatedAt = occurredAt;
                    TotalStageCount++;

                    _stageCounts.TryGetValue(stage, out var currentCount);
                    _stageCounts[stage] = currentCount + 1;

                    return CreateSnapshotUnsafe();
                }
            }

            public TaskStatisticsSnapshot ToSnapshot()
            {
                lock (_syncRoot)
                {
                    return CreateSnapshotUnsafe();
                }
            }

            private TaskStatisticsSnapshot CreateSnapshotUnsafe()
            {
                var stageCounts = _stageCounts
                    .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);

                return new TaskStatisticsSnapshot
                {
                    TaskId = TaskId,
                    CurrentStage = CurrentStage,
                    FirstSeenAt = FirstSeenAt,
                    LastUpdatedAt = LastUpdatedAt,
                    TotalStageCount = TotalStageCount,
                    StageCounts = new ReadOnlyDictionary<string, long>(stageCounts)
                };
            }
        }
    }
}
