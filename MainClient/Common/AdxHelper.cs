using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MainClient.Common
{
    public class AdxHelper
    {

        public static void SendSms(string name, string phone)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(phone))
            {
                return;
            }

            try
            {
                HttpHelper http = new HttpHelper();
                var item = new HttpItem()
                {
                    URL = AppSetting.SmsApiUrl,
                    Method = "POST",
                    ContentType = "application/x-www-form-urlencoded",
                    Postdata = $"name={System.Web.HttpUtility.UrlEncode(name)}&phone={phone}",
                    Timeout = 10000,
                    Allowautoredirect = true,
                };
                var hr = http.GetHtml(item);
                if (hr.StatusCode == System.Net.HttpStatusCode.OK)
                {

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

            }

        }
    }
}
