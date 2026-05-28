using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace MainClient.Models
{
    public class ProcessItem
    {
        public string ProcessPath { get; set; }
        public int ProcessId { get; set; }
        public int ClientWindowHandle { get; set; }
        public DateTime time { get; set; }

        public int Count;
        public int IncrementCount()
        {
            return Interlocked.Increment(ref Count);
        }
    }
}
