using System;
using System.Collections.Generic;
using System.Text;

namespace MainClient.Common
{
    public class AppSetting
    {
        public const string AppVertion = "2022.09.25.13";

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



        public string AllIpApiUrl { get; set; }
        public string TaskApiUrl { get; set; }
        public string UpdateApiUrl { get; set; }
        /// <summary>
        /// 获取任务间隔(毫秒)
        /// </summary>
        public int GetTaskInterval { get; set; }
        /// <summary>
        /// 最大并行的任务数量
        /// </summary>
        public int MaximumParallel { get; set; }
        /// <summary>
        /// 独立任务标识
        /// </summary>
        public string TaskIdentify { get; set; }

        public int MaximumLimitedConcurrency { get; set; }

        public int UVInterval { get; set; }

        public bool ShowWeb { get; set; }
        public bool NoProxy { get; set; }

        /// <summary>
        /// 任务倍数
        /// </summary>
        public int Multiple { get; set; }

        public bool RealIp { get; set; }

        /// <summary>
        /// 主进程重置时间
        /// </summary>
        public int MainResetInterval { get; set; }
        /// <summary>
        /// 子进程重置时间
        /// </summary>
        public int SubResetInterval { get; set; }


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
        /// IP区域校验
        /// </summary>
        public bool IPAreaCheck { get; set; }


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
