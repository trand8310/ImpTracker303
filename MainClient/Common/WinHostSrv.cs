using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SamMate.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace MainClient.Common
{
    public class WinHostSrv : WebSocketBehavior
    {
        public WinHostSrv()
        {

        }
        protected override void OnMessage(MessageEventArgs e)
        {
            Task.Factory.StartNew((t) =>
           {
               var model = new WSSResultModel();
               var msg = (JObject)JsonConvert.DeserializeObject(Convert.ToString(t));
               if (msg != null && msg["command"] != null)
               {
                   var command = msg["command"].ToString();
                   if (command.Equals("capture-screen"))
                   {
                       using (Bitmap image = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height))
                       {
                           Graphics g = Graphics.FromImage(image);
                           g.CopyFromScreen(new Point(0, 0), new Point(0, 0), image.Size, CopyPixelOperation.SourceCopy);
                           var encoding = CommonHelper.GetImageEncoders()["image/jpeg"];
                           using (var compressedBitmap = CommonHelper.CompressImageWithQuality(image, encoding, 70))
                           {
                               compressedBitmap.Save($"{System.DateTime.Now.ToString("yyyyMMddHHmmss")}.jpg", ImageFormat.Jpeg);
                               using (MemoryStream m = new MemoryStream())
                               {
                                   compressedBitmap.Save(m, System.Drawing.Imaging.ImageFormat.Gif);
                                   byte[] imageBytes = m.ToArray();
                                   model.SourceId = msg["sourceId"]?.ToString();
                                   model.Command = "capture-screen";
                                   model.Data = Convert.ToBase64String(imageBytes);
                                   model.success = true;
                                   model.code = 200;
                               }
                           }
                       }
                   }
                   else if (command.Equals("capture-window"))
                   {

                   }
               }

               SendAsync(JsonConvert.SerializeObject(model), (res) =>
               {

               });
           }, e.Data);

        }
    }
}
