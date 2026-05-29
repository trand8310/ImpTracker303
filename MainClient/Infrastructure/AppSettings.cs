
namespace MainClient.Infrastructure
{
    public class AppSettings
    {
 




        /// <summary>
        /// 短信接口
        /// </summary>
        public const string SmsApiUrl = "http://117.21.200.18:9000/sendsms.php";
        /// <summary>
        /// IP区域查询
        /// </summary>
        public const string QueryIpApiUrl = "http://117.21.200.141/queryip.php";

        /// <summary>
        /// 设备接口
        /// </summary>
        public string DevApiUrl { get; set; } = "http://117.21.200.18:9000/api/getdev.php";



        public string ProxyIpUrl { get; set; }
        public string TaskApiUrl { get; set; }
        public string UpdateApiUrl { get; set; }
        /// <summary>
        /// 任务提取间隔(毫秒)
        /// </summary>
        public int TaskPullIntervalMs { get; set; } = 1000;
        /// <summary>
        /// 任务提取出错延时(毫秒)
        /// </summary>
        public int TaskPullErrorDelayMs { get; set; } = 1000;
        /// <summary>
        /// 单UV执行间隔
        /// </summary>
        public int UvExecutionIntervalMs { get; set; } = 1000;

        /// <summary>
        /// 任务队列
        /// </summary>
        public int ChannelCapacity { get; set; } = 1;
        /// <summary>
        /// 并发数量
        /// </summary>
        public int MaxConcurrency { get; set; }
        /// <summary>
        /// 独立任务标识
        /// </summary>
        public string TaskName { get; set; } = "";
        /// <summary>
        /// 隐藏模式
        /// </summary>
        public bool IsHiddenMode { get; set; } = false;

        /// <summary>
        /// 任务倍数
        /// </summary>
        public int Multiple { get; set; }

        /// <summary>
        /// 代理模式
        /// </summary>
        public bool IsProxyMode { get; set; }
        /// <summary>
        /// 真实IP
        /// </summary>
        public bool IsRealIp { get; set; }

        /// <summary>
        /// Ip有效性校验
        /// </summary>
        public bool CheckIpHealth { get; set; }
        /// <summary>
        /// Ip地区校验
        /// </summary>
        public bool CheckIpRegion { get; set; }
        /// <summary>
        /// IP有效期
        /// </summary>
        public int IpValidityDuration { get; set; }

        /// <summary>
        /// 主进程重置时间
        /// </summary>
        public int MainProcessResetIntervalMinutes { get; set; }
        /// <summary>
        /// 子进程重置时间
        /// </summary>
        public int ChildProcessResetIntervalMinutes { get; set; }


        /// <summary>
        /// 短信服务
        /// </summary>
        public bool SendSms { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        public string SmsName { get; set; }
        public string SmsPhone { get; set; }

        public int SendSmsTimeout { get; set; }
        public bool NoneOS { get; set; }



        /// <summary>
        /// 使用系统设备信息
        /// </summary>
        public bool UsingSystemDevs { get; set; }


        /// <summary>
        /// IOS使用IMEI
        /// </summary>
        public bool UsingIOSIMEI { get; set; }

        /// <summary>
        /// IOS使用MAC
        /// </summary>
        public bool UsingIOSMAC { get; set; }

        /// <summary>
        /// 检测IP有效性
        /// </summary>
        public bool CheckIp { get; set; }

        public bool DisableImage { get; set; }




    }
}
