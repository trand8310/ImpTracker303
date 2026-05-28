using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace MainClient.Common
{
    internal sealed class TaskDispatchManager
    {
        private readonly Channel<JToken> _channel;
        private readonly List<Task> _consumerTasks = new List<Task>();
        private Task _producerTask;

        public TaskDispatchManager(int capacity)
        {
            var options = new BoundedChannelOptions(capacity)
            {
                SingleWriter = true,
                SingleReader = false,
                FullMode = BoundedChannelFullMode.Wait
            };
            _channel = Channel.CreateBounded<JToken>(options);
        }

        public ChannelReader<JToken> Reader => _channel.Reader;
        public ChannelWriter<JToken> Writer => _channel.Writer;

        public void Start(
            int consumerCount,
            Func<ChannelWriter<JToken>, CancellationToken, Task> producer,
            Func<int, ChannelReader<JToken>, CancellationToken, Task> consumer,
            CancellationToken token)
        {
            _producerTask = Task.Factory.StartNew(
                async () => await producer(_channel.Writer, token),
                token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default).Unwrap();

            _consumerTasks.Clear();
            for (int i = 1; i <= consumerCount; i++)
            {
                var consumerId = i;
                var task = Task.Factory.StartNew(
                    async () => await consumer(consumerId, _channel.Reader, token),
                    token,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default).Unwrap();
                _consumerTasks.Add(task);
            }
        }

        public async Task StopAsync(int timeoutMs)
        {
            _channel.Writer.TryComplete();
            var tasks = new List<Task>();
            if (_producerTask != null) tasks.Add(_producerTask);
            if (_consumerTasks.Count > 0) tasks.AddRange(_consumerTasks);
            if (tasks.Count == 0) return;
            await Task.WhenAny(Task.WhenAll(tasks), Task.Delay(timeoutMs));
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
    }
}
