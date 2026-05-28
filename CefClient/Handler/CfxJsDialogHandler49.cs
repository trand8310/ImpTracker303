using CefSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CefClient.Handler
{
    public class CfxJsDialogHandler : IJsDialogHandler
    {
        public bool OnJSDialog(IWebBrowser browserControl, IBrowser browser, string originUrl, string acceptLang, CefJsDialogType dialogType, string messageText, string defaultPromptText, IJsDialogCallback callback, ref bool suppressMessage)
        {
            switch (dialogType)
            {
                case CefSharp.CefJsDialogType.Alert:
                    suppressMessage = true;
                    return false;
                case CefSharp.CefJsDialogType.Confirm:
                    callback.Continue(false, string.Empty);
                    suppressMessage = false;
                    return true;
                case CefSharp.CefJsDialogType.Prompt:
                    break;
                default:
                    break;
            }
            return false;
        }

        public bool OnJSBeforeUnload(IWebBrowser browserControl, IBrowser browser, string message, bool isReload, IJsDialogCallback callback)
        {
            //NOTE: No need to execute the callback if you return false
            // callback.Continue(true);

            //NOTE: Returning false will trigger the default behaviour, you need to return true to handle yourself.
            return true;
        }

        public void OnResetDialogState(IWebBrowser browserControl, IBrowser browser)
        {

        }

        public void OnDialogClosed(IWebBrowser browserControl, IBrowser browser)
        {

        }
    }
}
