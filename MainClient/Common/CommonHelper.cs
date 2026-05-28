using MainClient.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SamMate.Common
{
    public class CommonHelper
    {
        public static string HmacSha1Sign(byte[] input, byte[] key)
        {
            HMACSHA1 myhmacsha1 = new HMACSHA1(key);
            MemoryStream stream = new MemoryStream(input);
            return myhmacsha1.ComputeHash(stream).Aggregate("", (s, e) => s + String.Format("{0:x2}", e), s => s);
        }
        public static long UnixTimeNow()
        {
            return new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
        }
        public static long UnixTimeNowSecond()
        {
            return new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
        }

        public static string CreateMD5(string input)
        {
            using (var md5 = MD5.Create())
            {
                var result = md5.ComputeHash(Encoding.ASCII.GetBytes(input));
                var strResult = BitConverter.ToString(result);
                return strResult.Replace("-", "").ToLower();
            }
        }

        public static string MD5Hash(string input)
        {
            using (var md5 = MD5.Create())
            {
                var result = md5.ComputeHash(Encoding.ASCII.GetBytes(input));
                var strResult = BitConverter.ToString(result);
                return strResult.Replace("-", "");
            }
        }





        public static Int16 Get16BitHash(string s)
        {
            return (Int16)(s.GetHashCode() & 0xFFFF);
        }

        public static string ComputeHash(string input)
        {
            byte[] bytes = Encoding.Default.GetBytes(input);
            HashAlgorithm iSHA = new SHA1CryptoServiceProvider();
            bytes = iSHA.ComputeHash(bytes);
            StringBuilder buf = new StringBuilder();
            foreach (byte b in bytes)
            {
                buf.AppendFormat("{0:x2}", b);
            }
            return buf.ToString().ToUpper();
        }

        public static string GetIpAddress()
        {
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                Console.WriteLine("No Network Available");
            }
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            var ipAddress = host.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
            return ipAddress.ToString();
        }


        public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (DirectoryInfo dir in source.GetDirectories())
            {
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            }

            foreach (FileInfo file in source.GetFiles())
            {
                file.CopyTo(Path.Combine(target.FullName, file.Name), true);
            }
        }

        public static long CreateIMEI(long imei)
        {
            var current = imei;
            var checksum = 0;
            for (int i = 0; i < 7; i++)
            {
                var d1 = (int)(current % 10) * 2;
                current = current / 10;
                var d0 = (int)(current % 10);
                current = current / 10;
                checksum += +d0 + d1 / 10 + d1 % 10;
            }
            checksum = 10 - (checksum % 10);
            if (checksum == 10)
                checksum = 0;
            return imei * 10 + checksum;
        }






        public static string CreateDeviceUUID()
        {
            Guid result = Guid.NewGuid();
            byte[] guidBytes = result.ToByteArray();
            for (int i = 0; i < 8; i++)
            {
                byte t = guidBytes[15 - i];
                guidBytes[15 - i] = guidBytes[i];
                guidBytes[i] = t;
            }

            return new Guid(guidBytes).ToString();
        }

        /// <summary>  
        /// 根据GUID获取16位的唯一字符串  
        /// </summary>  
        /// <param name=\"guid\"></param>  
        /// <returns></returns>  
        public static string GuidTo16String()
        {
            long i = 1;
            foreach (byte b in Guid.NewGuid().ToByteArray())
                i *= ((int)b + 1);
            return string.Format("{0:x}", i - DateTime.Now.Ticks);
        }
        /// <summary>  
        /// 根据GUID获取19位的唯一数字序列  
        /// </summary>  
        /// <returns></returns>  
        public static long GuidToLongID()
        {
            byte[] buffer = Guid.NewGuid().ToByteArray();
            return BitConverter.ToInt64(buffer, 0);
        }
        public static string GetRandomWifiMacAddress()
        {
            var random = new Random();
            var buffer = new byte[6];
            random.NextBytes(buffer);
            buffer[0] = 02;
            var result = string.Concat(buffer.Select(x => string.Format("{0}", x.ToString("X2"))).ToArray());
            return result.ToUpper().Insert(2, "-");
        }
        public static string GetRandomMacAddress()
        {
            var random = new Random();
            var buffer = new byte[6];
            random.NextBytes(buffer);
            var result = String.Concat(buffer.Select(x => string.Format("{0}:", x.ToString("X2"))).ToArray());
            return result.TrimEnd(':');
        }


        public static int GetOS(string userAgent)
        {
            var tmp = userAgent.ToLower();
            if (tmp.Contains("android"))
                return 0;//Android
            else if (tmp.ToLower().Contains("windows phone"))
                return 2;//Windows Phone
            else if (tmp.Contains("iphone") || tmp.Contains("ipad"))
                return 1;//Iphone
            return 3;
        }





        public static bool HttpGet(string url, out string result, string proxyIp = null)
        {
            HttpHelper http = new HttpHelper();
            var item = new HttpItem()
            {
                URL = url,
                Method = "GET",
                Allowautoredirect = true,
                Timeout = 5000,
            };

            if (!string.IsNullOrWhiteSpace(proxyIp))
            {
                item.ProxyIp = proxyIp;
            }

            var hr = http.GetHtml(item);
            result = hr.Html;
            return hr.StatusCode == HttpStatusCode.OK;
        }

        static object ipresasync = new object();
        public static string HttpGet(string url)
        {
            try
            {
                HttpHelper http = new HttpHelper();
                var item = new HttpItem()
                {
                    URL = url,
                    Method = "GET",
                    Allowautoredirect = true,
                    Timeout = 5000,
                };
                var hr = http.GetHtml(item);
                if (hr.StatusCode == HttpStatusCode.OK)
                {
                    return hr.Html.Trim();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }


            return null;
        }
        public static async Task<string> HttpGetAsync(string url)
        {
            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            return result;
        }

        public static void ClearProcesses(string[] processNames, string baseDir = null)
        {
            var Processes = Process.GetProcesses().Where(w => processNames.Contains(w.ProcessName));
            foreach (Process item in Processes)
            {
                if (!item.HasExited)
                {
                    try
                    {
                        item.Kill();
                    }
                    catch (Exception ex)
                    {
                        KillProcExec(item.Id);
                        Debug.WriteLine(ex.Message);
                    }


                    //    if (!string.IsNullOrWhiteSpace(baseDir) && item.MainModule.FileName.StartsWith(baseDir))
                    //    {
                    //        try
                    //        {
                    //            item.Kill();
                    //        }
                    //        catch (Exception ex)
                    //        {
                    //            KillProcExec(item.Id);
                    //            Debug.WriteLine(ex.Message);
                    //        }
                    //    }
                    //    else
                    //    {
                    //        try
                    //        {
                    //            item.Kill();
                    //        }
                    //        catch (Exception ex)
                    //        {
                    //            KillProcExec(item.Id);
                    //            Debug.WriteLine(ex.Message);
                    //        }
                    //    }


                }
            }
        }


        public static Process ExecCmd()
        {
            Process p = null;
            try
            {
                p = new Process();
                p.StartInfo.FileName = "cmd.exe";
                p.StartInfo.UseShellExecute = false;        //是否使用操作系统shell启动
                p.StartInfo.RedirectStandardInput = true;   //接受来自调用程序的输入信息
                p.StartInfo.RedirectStandardOutput = true;  //由调用程序获取输出信息
                p.StartInfo.RedirectStandardError = true;   //重定向标准错误输出
                p.StartInfo.CreateNoWindow = true;          //不显示程序窗口
            }
            catch (Exception e)
            {
                throw e;
            }
            return p;
        }
        public static bool KillProcExec(int procId)
        {
            string cmd = string.Format("taskkill /f /t /im {0}", procId); //强制结束指定进程
            Process ps = null;
            try
            {
                ps = ExecCmd();
                ps.Start();
                ps.StandardInput.WriteLine(cmd + "&exit");
                return true;
            }
            catch
            {
                throw;
            }
            finally
            {
                ps.Close();
            }
        }


        public static string HttpGet(string url, string proxyIp = null)
        {
            HttpHelper http = new HttpHelper();
            var item = new HttpItem()
            {
                URL = url,
                Method = "GET",
                Allowautoredirect = true,
                Timeout = 5000,
            };
            if (!string.IsNullOrWhiteSpace(proxyIp))
            {
                item.ProxyIp = proxyIp;
            }
            var hr = http.GetHtml(item);
            if (hr.StatusCode == HttpStatusCode.OK)
            {
                return hr.Html.Trim();
            }
            return null;
        }


        //public static async Task<string> HttpGetAsync(string url, string ip = null, string port = null)
        //{
        //    //    var proxiedHttpClientHandler = new HttpClientHandler() { UseProxy = true };
        //    //    proxiedHttpClientHandler.Proxy = new System.Net.WebProxy(ip, Convert.ToInt32(port));

        //    //HttpClientHandler handler = new HttpClientHandler()
        //    //{
        //    //    Proxy = new WebProxy("http://127.0.0.1:8888"),
        //    //    UseProxy = true,
        //    //};
        //    var serviceProvider = new ServiceCollection().AddHttpClient().BuildServiceProvider();
        //    var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
        //    var client = httpClientFactory.CreateClient();
        //    var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, url));
        //    var content = await response.Content.ReadAsStringAsync();
        //    return content;
        //}


        public static long IpToInt(string ip)
        {
            string[] items = ip.Split('.');
            return long.Parse(items[0]) << 24
                    | long.Parse(items[1]) << 16
                    | long.Parse(items[2]) << 8
                    | long.Parse(items[3]);
        }
        public static void DeleteCookieFile(string dirRoot)
        {
            try
            {
                string[] rootDirs = Directory.GetDirectories(dirRoot);
                string[] rootFiles = Directory.GetFiles(dirRoot);
                foreach (string s2 in rootFiles)
                {
                    if (s2.Contains("Cookies"))
                    {
                        File.Delete(s2);
                    }
                }
                foreach (string s1 in rootDirs)
                {
                    DeleteCookieFile(s1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
            }
        }
        public static Image Base64ToImage(string base64String)
        {
            // Convert base 64 string to byte[]
            byte[] imageBytes = Convert.FromBase64String(base64String);
            // Convert byte[] to Image
            using (var ms = new MemoryStream(imageBytes, 0, imageBytes.Length))
            {
                Image image = Image.FromStream(ms, true);
                return image;
            }
        }


        //保存图片时设置质量
        public static void SaveImageWithQuality(Image bmp, long level)
        {
            ImageCodecInfo jgpEncoder = GetEncoder(ImageFormat.Jpeg);
            System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;
            EncoderParameters myEncoderParameters = new EncoderParameters(1);
            EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, level);
            myEncoderParameters.Param[0] = myEncoderParameter;
            bmp.Save(@"test.jpg", jgpEncoder, myEncoderParameters);
        }


        /// <summary>
        /// 图片尺寸压缩
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="maxWidth"></param>
        /// <param name="maxHeight"></param>
        /// <returns></returns>
        public static System.Drawing.Bitmap CompressImageWithSize(System.Drawing.Bitmap bitmap, int maxWidth = 1024, int maxHeight = 1024)
        {
            int actualWidth = bitmap.Width < maxWidth ? bitmap.Width : maxWidth;
            int actualHeight = int.Parse(Math.Round(bitmap.Height * (double)actualWidth / bitmap.Width).ToString());
            try
            {
                var actualBitmap = new System.Drawing.Bitmap(actualWidth, actualHeight);
                var g = System.Drawing.Graphics.FromImage(actualBitmap);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Default;
                g.DrawImage(bitmap, new System.Drawing.Rectangle(0, 0, actualWidth, actualHeight)
                    , new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height)
                    , System.Drawing.GraphicsUnit.Pixel);
                g.Dispose();
                return actualBitmap;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }
        }



        /// <summary>
        /// 图像质量压缩
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="encoding"></param>
        /// <param name="quality"></param>
        /// <returns></returns>
        public static System.Drawing.Bitmap CompressImageWithQuality(System.Drawing.Bitmap bitmap, System.Drawing.Imaging.ImageCodecInfo encoding, int quality = 70)
        {
            var ps = new System.Drawing.Imaging.EncoderParameters(1);
            ps.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            var stream = new MemoryStream();
            bitmap.Save(stream, encoding, ps);
            var compressedBitmap = new System.Drawing.Bitmap(stream);
            return compressedBitmap;
        }

        public static Dictionary<string, System.Drawing.Imaging.ImageCodecInfo> GetImageEncoders()
        {
            var result = new Dictionary<string, System.Drawing.Imaging.ImageCodecInfo>();
            var encoders = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders().ToList();
            foreach (var encode in encoders)
                result.Add(encode.MimeType, encode);
            return result;
        }







        public static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
        public static void ClearCacheFile(int processIndex)
        {
            #region 删除物理文件

            try
            {
                string cachePath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "chrome", "User Data", processIndex.ToString());
                if (System.IO.Directory.Exists(cachePath))
                    Directory.Delete(cachePath, recursive: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            #endregion
        }



        /// <summary>
        /// 清空目录内容（不删除根目录）
        /// </summary>
        /// <param name="path"></param>
        public static void ClearDirectory(string path)
        {
            if (!Directory.Exists(path))
                return;

            try
            {
                // 删除所有文件
                foreach (var file in Directory.GetFiles(path))
                {
                    try
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);
                    }
                    catch
                    {
                        // 可记录日志
                    }
                }

                // 删除所有子目录
                foreach (var dir in Directory.GetDirectories(path))
                {
                    try
                    {
                        RemoveDirectoryRecursive(dir);
                    }
                    catch
                    {
                        // 可记录日志
                    }
                }
            }
            catch
            {
                // 整体异常处理
            }
        }

        /// <summary>
        /// 删除目录（强力递归）
        /// </summary>
        /// <param name="dir"></param>
        private static void RemoveDirectoryRecursive(string dir)
        {
            if (!Directory.Exists(dir))
                return;

            // 清除只读属性
            foreach (var file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
            {
                try
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }
                catch { }
            }

            Directory.Delete(dir, true);
        }



        public static void CreateShortCut(string shortcutName)
        {
            var shortcutPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup), $"{shortcutName}{string.Join("", AppSetting.AppVertion.Split('.').Skip(1).Take(2))}.lnk");
            if (System.IO.File.Exists(shortcutPath))
            {
                System.IO.File.Delete(shortcutPath);
            }
            byte[] bytes = null;
            using (System.Security.Principal.WindowsImpersonationContext ctx = System.Security.Principal.WindowsIdentity.Impersonate(IntPtr.Zero))
            {
                var path = Path.GetTempPath();
                string temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".lnk");
                try
                {
                    IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
                    IWshRuntimeLibrary.IWshShortcut shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(temp);
                    shortcut.TargetPath = System.Windows.Forms.Application.ExecutablePath;
                    shortcut.Save();
                    bytes = System.IO.File.ReadAllBytes(temp);
                }
                finally
                {
                    if (System.IO.File.Exists(temp)) System.IO.File.Delete(temp);
                }
            }
            System.IO.File.WriteAllBytes(shortcutPath, bytes);
        }




        public static void CreateShortcut(string shortcutName)
        {
            IWshRuntimeLibrary.WshShell wsh = new IWshRuntimeLibrary.WshShell();
            var shortcutPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), $"{shortcutName}{string.Join("", AppSetting.AppVertion.Split('.').Skip(1).Take(2))}.lnk");
            if (System.IO.File.Exists(shortcutPath))
            {
                System.IO.File.Delete(shortcutPath);
            }
            try
            {
                IWshRuntimeLibrary.IWshShortcut shortcut = wsh.CreateShortcut(shortcutPath) as IWshRuntimeLibrary.IWshShortcut;
                shortcut.Arguments = "restart";
                shortcut.TargetPath = System.Windows.Forms.Application.ExecutablePath;
                shortcut.WindowStyle = 1;
                shortcut.Description = shortcutName;
                shortcut.WorkingDirectory = System.AppDomain.CurrentDomain.BaseDirectory;
                shortcut.IconLocation = System.Windows.Forms.Application.ExecutablePath;
                shortcut.Save();

            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }

        }
    }
}
