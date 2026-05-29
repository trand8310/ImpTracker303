using MainClient.Infrastructure;
using Newtonsoft.Json.Linq;
using System.Collections.Specialized;
using System.Web;

namespace MainClient.Common
{
    public sealed class TrackingUrlProcessor
    {
        //public delegate string TrackingFormatter(string url, string ip, string userAgent, JToken param, OSType os, JObject ipInfo, JToken dev = null);

        //private readonly TrackingFormatter _defaultFormatter;
        //private readonly TrackingFormatter _gridSumFormatter;
        //private readonly TrackingFormatter _ipinyouFormatter;
        //private readonly TrackingFormatter _mafengwoFormatter;
        private readonly AppSettings _appSettings;
        public TrackingUrlProcessor(AppSettings appSettings)
        {
            _appSettings = appSettings;
        }

        public string Format(string sourceUrl, string ip, string userAgent, JObject task, OSType os, JToken dev)
        {
            if (string.IsNullOrWhiteSpace(sourceUrl))
            {
                return string.Empty;
            }

            var domain = new Uri(sourceUrl).Host;

            if (domain.Contains("gridsumdissector.com"))
            {
                return _gridSumFormatter(sourceUrl, ip, userAgent, task, os, null, dev);
            }
            if (domain.Contains("ipinyou.com"))
            {
                return _ipinyouFormatter(sourceUrl, ip, userAgent, task, os, null, dev);
            }
            if (domain.Contains("mafengwo.cn"))
            {
                return _mafengwoFormatter(sourceUrl, ip, userAgent, task, os, null, dev);
            }

            return _defaultFormatter(sourceUrl, ip, userAgent, task, os, null, dev);
        }





        /// <summary>
        /// 秒针URL处理
        /// </summary>
        /// <param name="url"></param>
        /// <param name="ip"></param>
        /// <param name="userAgent"></param>
        /// <param name="param"></param>
        /// <param name="os"></param>
        /// <param name="dev"></param>
        /// <returns></returns>
        private string _defaultFormatter(string url, string ip, string userAgent, JToken param, OSType os, JObject ipInfo, JToken dev = null)
        {

            if (param["huichuanip"] != null && param["huichuanip"].ToString().Equals("on") && !string.IsNullOrWhiteSpace(ip))
            {
                url = url.Replace("__IP__", ip);
            }
            if (param["huichuan"] != null && param["huichuan"].ToString().Equals("on"))
            {
                //__OS__//1位数字,取0~3。0表示Android，1表示iOS，2表示Windows Phone，3表示其他
                if (!_appSettings.NoneOS)
                {
                    if (os == OSType.ANDROID)
                        url = url.Replace("__OS__", "0");
                    else if (os == OSType.IOS)
                        url = url.Replace("__OS__", "1");
                    else if (os == OSType.WINDOWS_PHONE)
                        url = url.Replace("__OS__", "2");
                    else
                        url = url.Replace("__OS__", "3");
                }

                if (os == OSType.IOS)
                {
                    if (_appSettings.UsingIOSIMEI)
                    {
                        string imei = string.Empty;
                        if (dev != null && dev["imei"] != null)
                        {
                            imei = dev["imei"].ToString();
                        }
                        var imei_md5 = CommonHelper.MD5Hash(imei);
                        url = url.Replace("__IMEI__", imei_md5);
                    }

                    if (_appSettings.UsingIOSMAC)
                    {
                        string mac = string.Empty;
                        if (dev != null && dev["mac"] != null)
                        {
                            mac = dev["mac"].ToString().ToUpper();
                        }
                        var macmd51 = CommonHelper.MD5Hash(mac);
                        var macmd52 = CommonHelper.MD5Hash(mac.Replace(":", ""));

                        url = url.Replace("__MAC1__", macmd51);
                        url = url.Replace("__MAC__", macmd52);
                    }

                    string idfa = string.Empty;
                    if (dev != null && dev["idfa"] != null)
                    {
                        idfa = dev["idfa"].ToString().ToUpper();
                    }
                    url = url.Replace("__IDFA__", idfa);

                }
                else if (os == OSType.ANDROID)
                {


                    string mac = string.Empty;
                    if (dev != null && dev["mac"] != null)
                    {
                        mac = dev["mac"].ToString().ToUpper();
                    }
                    if (string.IsNullOrWhiteSpace(mac))
                    {
                        mac = CommonHelper.GetRandomMacAddress();
                    }

                    var macmd51 = CommonHelper.MD5Hash(mac);
                    var macmd52 = CommonHelper.MD5Hash(mac.Replace(":", ""));

                    url = url.Replace("__MAC1__", macmd51);
                    url = url.Replace("__MAC__", macmd52);



                    string imei = string.Empty;
                    if (dev != null && dev["imei"] != null)
                    {
                        imei = dev["imei"].ToString();
                    }
                    var imei_md5 = CommonHelper.MD5Hash(imei);
                    url = url.Replace("__IMEI__", imei_md5);



                    string androidId = string.Empty;
                    if (dev != null && dev["android_id"] != null)
                    {
                        androidId = dev["android_id"].ToString().ToUpper();
                    }
 
                    var androidId_md5 = CommonHelper.MD5Hash(androidId);

                    url = url.Replace("__ANDROIDID__", androidId_md5);
                    url = url.Replace("__ANDROIDID1__", androidId);


                    if (dev != null && dev["oaid"] != null)
                    {
                        var oaid = dev["oaid"].ToString();
                        if (!string.IsNullOrWhiteSpace(oaid))
                        {
                            url = url.Replace("__OAID__", oaid);
                            var oaid_md5 = CommonHelper.MD5Hash(oaid);
                            url = url.Replace("__OAID1__", oaid_md5);
                        }
                    }
                }
                if (url.Contains("[timestamp]"))
                    url = url.Replace("[timestamp]", CommonHelper.UnixTimeNowSecond().ToString());
                if (url.Contains("__TS__"))
                    url = url.Replace("__TS__", CommonHelper.UnixTimeNowSecond().ToString());

            }
            return url;
        }

        /// <summary>
        /// 国双URL
        /// </summary>
        /// <param name="url"></param>
        /// <param name="ip"></param>
        /// <param name="userAgent"></param>
        /// <param name="param"></param>
        /// <param name="os"></param>
        /// <param name="ipInfo"></param>
        /// <param name="dev"></param>
        /// <returns></returns>
        private string _gridSumFormatter(string url, string ip, string userAgent, JToken param, OSType os, JObject ipInfo, JToken dev = null)
        {
            //https://i.gridsumdissector.com/v/?gscmd=impress&gid=gad_155_Y9RU7SQW&os=__OS__&if=__IDFA__&oid=__OPENUDID__&aid=__ANDROIDID__&im=__IMEI__&oa=__OAID__&m=__MAC__&ip=__IP__&ts=__TS__&did=__DUID__&aaid=__AAID__&uid=__UDID__&odin=__ODIN__&ua=__UA__&lbs=__LBS__

            if (param["huichuanip"] != null && param["huichuanip"].ToString().Equals("on") && string.IsNullOrWhiteSpace(ip))
            {
                url = url.Replace("__IP__", ip);
            }
            if (param["huichuan"] != null && param["huichuan"].ToString().Equals("on"))
            {
                if (os == OSType.ANDROID)
                    url = url.Replace("__OS__", "0");
                else if (os == OSType.IOS)
                    url = url.Replace("__OS__", "1");
                else if (os == OSType.WINDOWS_PHONE)
                    url = url.Replace("__OS__", "2");
                else
                    url = url.Replace("__OS__", "3");





                if (os == OSType.IOS)
                {
                    if (_appSettings.UsingIOSIMEI)
                    {
                        string imei = string.Empty;
                        if (dev != null && dev["imei"] != null)
                        {
                            imei = dev["imei"].ToString().ToLower();
                        }
      

                        var imei_md5 = CommonHelper.MD5Hash(imei);
                        url = url.Replace("__IMEI__", imei_md5);
                    }

                    if (_appSettings.UsingIOSMAC)
                    {
                        string mac = string.Empty;
                        if (dev != null && dev["mac"] != null)
                        {
                            mac = dev["mac"].ToString().ToUpper();
                        }
                        if (string.IsNullOrWhiteSpace(mac))
                        {
                            mac = CommonHelper.GetRandomMacAddress().ToUpper();
                        }
                        var macmd5 = CommonHelper.MD5Hash(mac);
                        url = url.Replace("__MAC__", CommonHelper.MD5Hash(macmd5));
                    }

                    string idfa = string.Empty;
                    if (dev != null && dev["idfa"] != null)
                    {
                        idfa = dev["idfa"].ToString().ToUpper();
                    }
            
                    url = url.Replace("__IDFA__", idfa);

                }
                else if (os == OSType.ANDROID)
                {


                    string imei = string.Empty;
                    if (dev != null && dev["imei"] != null)
                    {
                        imei = dev["imei"].ToString().ToLower();
                    }
         

                    var imei_md5 = CommonHelper.MD5Hash(imei);
                    url = url.Replace("__IMEI__", imei_md5);


                    string mac = string.Empty;
                    if (dev != null && dev["mac"] != null)
                    {
                        mac = dev["mac"].ToString().ToUpper();
                    }
                    if (string.IsNullOrWhiteSpace(mac))
                    {
                        mac = CommonHelper.GetRandomMacAddress().ToUpper();
                    }
                    var macmd5 = CommonHelper.MD5Hash(mac);
                    url = url.Replace("__MAC__", CommonHelper.MD5Hash(macmd5));


                    string androidId = string.Empty;
                    if (dev != null && dev["android_id"] != null)
                    {
                        androidId = dev["android_id"].ToString().ToUpper();
                    }
     
                    var androidId_md5 = CommonHelper.MD5Hash(androidId);
                    url = url.Replace("__ANDROIDID__", androidId_md5);
                }
                url = url.Replace("__TS__", CommonHelper.UnixTimeNow().ToString());
            }
            return url;
        }

        /// <summary>
        /// 深演广告
        /// </summary>
        /// <param name="url"></param>
        /// <param name="ip"></param>
        /// <param name="userAgent"></param>
        /// <param name="param"></param>
        /// <param name="os"></param>
        /// <param name="ipInfo"></param>
        /// <param name="dev"></param>
        /// <returns></returns>
        private string _ipinyouFormatter(string url, string ip, string userAgent, JToken param, OSType os, JObject ipInfo, JToken dev = null)
        {
            //http://vt.ipinyou.com/IinK3066gI5vwOkVZ-.IcX5R_.sWLZhPIi7pbkvccpO3kUXEe5DrZWFlJbrDuAyySZ_T8kzY9epmcXfrEv_RzyW4f.txHx607mbPPtH8cJVys8k_?tmp=[timestamp]&mob_idfa=[idfa]&mob_imei=[imei]&mob_android=[androidid]&mob_os=[os]&mob_oaid=[oaid]&mob_mac=[mac]

            if (param["huichuan"] != null && param["huichuan"].ToString().Equals("on"))
            {
                url = url.Replace("[timestamp]", CommonHelper.UnixTimeNow().ToString());
                string os_val = string.Empty;
                if (os == OSType.ANDROID)
                    os_val = "0";

                else if (os == OSType.IOS)
                    os_val = "1";
                else if (os == OSType.WINDOWS_PHONE)
                    os_val = "2";
                else
                    os_val = "3";

                url = url.Replace("[os]", os_val);
                url = url.Replace("__OS__", os_val);

                if (os == OSType.IOS)
                {
                    if (_appSettings.UsingIOSIMEI)
                    {
                        string imei = string.Empty;
                        if (dev != null && dev["imei"] != null)
                        {
                            imei = dev["imei"].ToString().ToLower();
                        }
    
                        var imei_md5 = CommonHelper.MD5Hash(imei);
                        url = url.Replace("[imei]", imei_md5);
                        url = url.Replace("__IMEI__", imei_md5);
                    }

                    if (_appSettings.UsingIOSMAC)
                    {
                        string mac = string.Empty;
                        if (dev != null && dev["mac"] != null)
                        {
                            mac = dev["mac"].ToString().ToUpper();
                        }
                        if (string.IsNullOrWhiteSpace(mac))
                        {
                            mac = CommonHelper.GetRandomMacAddress().ToUpper();
                        }
                        var macmd5 = CommonHelper.MD5Hash(mac.Replace(":", ""));
                        url = url.Replace("[mac]", macmd5);
                        url = url.Replace("__MAC__", macmd5);
                    }

                    string idfa = string.Empty;
                    if (dev != null && dev["idfa"] != null)
                    {
                        idfa = dev["idfa"].ToString().ToUpper();
                    }
          
                    url = url.Replace("[idfa]", idfa);
                    url = url.Replace("__IDFA__", idfa);
                }

                else if (os == OSType.ANDROID)
                {

                    string imei = string.Empty;
                    if (dev != null && dev["imei"] != null)
                    {
                        imei = dev["imei"].ToString().ToLower();
                    }
    
                    var imei_md5 = CommonHelper.MD5Hash(imei);
                    url = url.Replace("[imei]", imei_md5);
                    url = url.Replace("__IMEI__", imei_md5);



                    string mac = string.Empty;
                    if (dev != null && dev["mac"] != null)
                    {
                        mac = dev["mac"].ToString().ToUpper();
                    }
                    if (string.IsNullOrWhiteSpace(mac))
                    {
                        mac = CommonHelper.GetRandomMacAddress().ToUpper();
                    }
                    var macmd5 = CommonHelper.MD5Hash(mac.Replace(":", ""));
                    url = url.Replace("[mac]", macmd5);
                    url = url.Replace("__MAC__", macmd5);




                    string androidId = string.Empty;
                    if (dev != null && dev["android_id"] != null)
                    {
                        androidId = dev["android_id"].ToString().ToLower();
                    }
   
                    var androidId_md5 = CommonHelper.MD5Hash(androidId);
                    url = url.Replace("[androidid]", androidId_md5);
                    url = url.Replace("__ANDROIDID__", androidId_md5);
                }
            }
            return url;
        }


        public static string ReplacePlaceholderValues(string url, Dictionary<string, string> valueReplacements)
        {
            var uri = new Uri(url);

            // 保留路径部分
            string fragment = uri.Fragment;
            string baseUrl = uri.GetLeftPart(UriPartial.Path);

            // 解析现有参数
            NameValueCollection queryParams = HttpUtility.ParseQueryString(uri.Query);

            // 遍历所有参数值
            foreach (string key in queryParams.AllKeys)
            {
                string value = queryParams[key];
                if (value != null && valueReplacements.ContainsKey(value))
                {
                    queryParams[key] = valueReplacements[value];
                }
            }

            // 重新组合 URL
            string newQuery = queryParams.Count > 0 ? "?" + queryParams.ToString() : string.Empty;
            return baseUrl + newQuery + fragment;
        }





        private string _mafengwoFormatter(string url, string ip, string userAgent, JToken param, OSType os, JObject ipInfo, JToken dev = null)
        {
            //http://vt.ipinyou.com/IinK3066gI5vwOkVZ-.IcX5R_.sWLZhPIi7pbkvccpO3kUXEe5DrZWFlJbrDuAyySZ_T8kzY9epmcXfrEv_RzyW4f.txHx607mbPPtH8cJVys8k_?tmp=[timestamp]&mob_idfa=[idfa]&mob_imei=[imei]&mob_android=[androidid]&mob_os=[os]&mob_oaid=[oaid]&mob_mac=[mac]
            //https://admonitor.mafengwo.cn/flux/i.gif?open_udid=__MFWUDID__&idfa=__IDFA__&imei=__IMEI__&oaid=__OAID__&os=__OS__&platform=1APP&pos_key=app_sales_banner_gd&ad_id=1020997&mate_id=634081&uid=35758&mate_type=230&source=__MFWSOURCE__&put_type=gdcpm&cycle_number=&contract=MFWPGS202507029&target=https%3A%2F%2Fg.cn.miaozhen.com%2Fx%2Fk%3D2466417%26p%3D8tSzf%26rt%3D2%26pro%3Ds%26dx%3D__IPDX__%26ns%3D__IP__%26ni%3D__IESID__%26v%3D__LOC__%26xa%3D__ADPLATFORM__%26tr%3D__REQUESTID__%26vg%3D__AUTOPLAY__%26nh%3D__AUTOREFRESH__%26mo%3D__OS__%26m0%3D__OPENUDID__%26m0a%3D__DUID__%26m1%3D__ANDROIDID1__%26m1a%3D__ANDROIDID__%26m2%3D__IMEI__%26m4%3D__AAID__%26m5%3D__IDFA__%26m6%3D__MAC1__%26m6a%3D__MAC__%26m11%3D__OAID__%26m14%3D__CAID__%26m5a%3D__IDFV__%26mn%3D__ANAME__%26m5b%3D__IDFA1__%26m11a%3D__OAID1__%26m14a%3D__CAID1__%26gc%3D__GCID__%26o%3D

            if (param["huichuan"] != null && param["huichuan"].ToString().Equals("on"))
            {

                var updates = new Dictionary<string, string>();
                var ts = CommonHelper.UnixTimeNow();

                updates.Add("[timestamp]", ts.ToString());
                updates.Add("__TS__", ts.ToString());

                string os_val = string.Empty;
                if (os == OSType.ANDROID)
                    os_val = "0";
                else if (os == OSType.IOS)
                    os_val = "1";
                else if (os == OSType.WINDOWS_PHONE)
                    os_val = "2";
                else
                    os_val = "3";

                updates.Add("[os]", os_val);
                updates.Add("__OS__", os_val);


                if (os == OSType.IOS)
                {
                    if (_appSettings.UsingIOSIMEI)
                    {
                        string imei = string.Empty;
                        if (dev != null && dev["imei"] != null)
                        {
                            imei = dev["imei"].ToString().ToLower();
                        }
           
                        var imei_md5 = CommonHelper.MD5Hash(imei);
                        updates.Add("[imei]", imei_md5);
                        updates.Add("__IMEI__", imei_md5);
                    }


                    if (_appSettings.UsingIOSMAC)
                    {
                        string mac = string.Empty;
                        if (dev != null && dev["mac"] != null)
                        {
                            mac = dev["mac"].ToString().ToUpper();
                        }
                        if (string.IsNullOrWhiteSpace(mac))
                        {
                            mac = CommonHelper.GetRandomMacAddress().ToUpper();
                        }
                        var macmd5 = CommonHelper.MD5Hash(mac.Replace(":", ""));

                        updates.Add("[mac]", macmd5);
                        updates.Add("__MAC__", macmd5);
                    }

                    string idfa = string.Empty;
                    if (dev != null && dev["idfa"] != null)
                    {
                        idfa = dev["idfa"].ToString().ToUpper();
                    }
      

                    updates.Add("[idfa]", idfa);
                    updates.Add("__IDFA__", idfa);
                }

                else if (os == OSType.ANDROID)
                {

                    string imei = string.Empty;
                    if (dev != null && dev["imei"] != null)
                    {
                        imei = dev["imei"].ToString().ToLower();
                    }
 
                    var imei_md5 = CommonHelper.MD5Hash(imei);
                    updates.Add("[imei]", imei_md5);
                    updates.Add("__IMEI__", imei_md5);

                    string mac = string.Empty;
                    if (dev != null && dev["mac"] != null)
                    {
                        mac = dev["mac"].ToString().ToUpper();
                    }
                    if (string.IsNullOrWhiteSpace(mac))
                    {
                        mac = CommonHelper.GetRandomMacAddress().ToUpper();
                    }
                    var macmd5 = CommonHelper.MD5Hash(mac.Replace(":", ""));

                    updates.Add("[mac]", macmd5);
                    updates.Add("__MAC__", macmd5);


                    string androidId = string.Empty;
                    if (dev != null && dev["android_id"] != null)
                    {
                        androidId = dev["android_id"].ToString().ToLower();
                    }
 
                    var androidId_md5 = CommonHelper.MD5Hash(androidId);

                    updates.Add("[androidid]", androidId_md5);
                    updates.Add("__ANDROIDID__", androidId_md5);

                    updates.Add("[androidid1]", androidId);
                    updates.Add("__ANDROIDID1__", androidId);



                    if (dev != null && dev["oaid"] != null)
                    {
                        var oaid = dev["oaid"].ToString();
                        if (!string.IsNullOrWhiteSpace(oaid))
                        {
                            updates.Add("[oaid]", oaid);
                            updates.Add("__OAID__", oaid);
                            var oaid_md5 = CommonHelper.MD5Hash(oaid);
                            updates.Add("[oaid1]", oaid_md5);
                            updates.Add("__OAID1__", oaid_md5);
                        }

                    }

                }

                url = ReplacePlaceholderValues(url, updates);
            }
            return url;
        }
    }
}
