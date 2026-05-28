using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainClient.Models
{
    public class JobProcess
    {
        public int index { get; set; }

        public ProcessItem client { get; set; }
        public JObject task { get; set; }
    }
}
