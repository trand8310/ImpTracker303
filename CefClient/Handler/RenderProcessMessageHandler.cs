using CefSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CefClient.Handler
{
    public class RenderProcessMessageHandler : IRenderProcessMessageHandler
    {
        private readonly int screenWidth ;
        private readonly int screenHeight;
        private readonly string userAgent;
        public RenderProcessMessageHandler(int screenWidth,int screenHeight,string userAgent)
        {
            this.screenWidth = screenWidth;
            this.screenHeight = screenHeight;
            this.userAgent = userAgent;
        }

        //navigator.userAgent
        public void OnContextReleased(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame)
        {
 
        }

        public void OnFocusedNodeChanged(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IDomNode node)
        {
      
        }

        public void OnUncaughtException(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, JavascriptException exception)
        {
 
        }

        public void OnContextCreated(IWebBrowser browserControl, IBrowser browser, IFrame frame)
        {
            StringBuilder js = new StringBuilder();
            js.AppendLine($"Object.defineProperty(window.screen,'width',{{get:() => {this.screenWidth}}});");
            js.AppendLine($"Object.defineProperty(window.screen,'availWidth',{{get:() => {this.screenWidth}}});");
            js.AppendLine($"Object.defineProperty(window.screen,'height',{{get:() => {this.screenHeight}}});");
            js.AppendLine($"Object.defineProperty(window.screen,'availHeight',{{get:() => {this.screenHeight}}});");
            js.AppendLine($"Object.defineProperty(navigator,'userAgent',{{get:() => \"{ this.userAgent}\"}});");
            js.AppendLine($"Object.defineProperty(navigator,'appVersion',{{get:() => \"{ this.userAgent}\"}});");
            frame.ExecuteJavaScriptAsync(js.ToString());
        }
    }

}
