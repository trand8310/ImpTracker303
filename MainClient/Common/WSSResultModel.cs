using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainClient.Common
{
    public class WSSResultModel
    {
        public bool success { get; set; }
        public int code { get; set; }
        public string SourceId { get; set; }
        public string Command { get; set; }
        public string Data { get; set; }
        public string Param { get; set; }
    }
}
