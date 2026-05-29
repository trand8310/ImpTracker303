using CefSharp;
using CefSharp.OffScreen;
using System;
using System.Collections.Specialized;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace CefClient.Common
{
    public static class CefBrowserExtensions
    {
        public static Task<bool> LoadRequestAsync(
        this ChromiumWebBrowser browser,
        string url,
        string requestMethod = "GET",
        string referrer = null,
        WebHeaderCollection headers = null,
        byte[] postDataBytes = null,
        int timeoutMs = 15000)
        {
            var tcs = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            if (browser == null ||
                browser.IsDisposed ||
                string.IsNullOrWhiteSpace(url))
            {
                tcs.TrySetResult(false);
                return tcs.Task;
            }

            EventHandler<FrameLoadEndEventArgs> frameLoadEndHandler = null;
            EventHandler<LoadErrorEventArgs> loadErrorHandler = null;

            var cts = new CancellationTokenSource();

            void Cleanup()
            {
                try
                {
                    browser.FrameLoadEnd -= frameLoadEndHandler;
                    browser.LoadError -= loadErrorHandler;
                    cts.Cancel();
                    cts.Dispose();
                }
                catch
                {
                }
            }

            frameLoadEndHandler = (s, e) =>
            {
                if (!e.Frame.IsMain)
                    return;

                bool success =
                    e.HttpStatusCode >= 200 &&
                    e.HttpStatusCode < 400;

                Cleanup();
                tcs.TrySetResult(success);
            };

            loadErrorHandler = (s, e) =>
            {
                if (!e.Frame.IsMain)
                    return;

                // Aborted 有时是新导航打断旧导航，不一定是真失败
                Cleanup();
                tcs.TrySetResult(false);
            };

            browser.FrameLoadEnd += frameLoadEndHandler;
            browser.LoadError += loadErrorHandler;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(timeoutMs, cts.Token);

                    if (!tcs.Task.IsCompleted)
                    {
                        try
                        {
                            browser.GetBrowser()?.StopLoad();
                        }
                        catch
                        {
                        }

                        Cleanup();
                        tcs.TrySetResult(false);
                    }
                }
                catch (TaskCanceledException)
                {
                }
            });

            try
            {
                var frame = browser.GetMainFrame();

                if (frame == null || !frame.IsValid)
                {
                    Cleanup();
                    tcs.TrySetResult(false);
                    return tcs.Task;
                }

                bool initializePostData =
                    string.Equals(requestMethod, "POST", StringComparison.OrdinalIgnoreCase);

                var request = frame.CreateRequest(
                    initializePostData: initializePostData);

                if (initializePostData &&
                    postDataBytes != null &&
                    postDataBytes.Length > 0)
                {
                    request.InitializePostData();

                    if (request.PostData != null)
                    {
                        request.PostData.AddData(postDataBytes);
                    }
                }

                request.Url = url;
                request.Method = string.IsNullOrWhiteSpace(requestMethod)
                    ? "GET"
                    : requestMethod.ToUpperInvariant();

                if (headers != null && headers.HasKeys())
                {
                    var originHeaders = request.Headers ?? new NameValueCollection();

                    foreach (string keyName in headers.AllKeys)
                    {
                        if (!string.IsNullOrWhiteSpace(keyName))
                        {
                            originHeaders.Set(keyName, headers[keyName]);
                        }
                    }

                    request.Headers = originHeaders;
                }

                if (!string.IsNullOrWhiteSpace(referrer))
                {
                    request.SetReferrer(
                        referrer,
                        ReferrerPolicy.NeverClearReferrer);
                }

                frame.LoadRequest(request);
            }
            catch
            {
                Cleanup();
                tcs.TrySetResult(false);
            }
            return tcs.Task;
        }


    }
}
