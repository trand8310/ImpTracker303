

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CefClient
{
    public static class ControlExtensions
    {
        public static void DoubleBuffered(this Control control, bool enable)
        {
            var prop = typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            prop?.SetValue(control, enable, null);
        }


        /// <summary>
        /// Executes the Action asynchronously on the UI thread, does not block execution on the calling thread.
        /// </summary>
        /// <param name="control">the control for which the update is required</param>
        /// <param name="action">action to be performed on the control</param>
        public static void InvokeOnUiThreadIfRequired(this Control control, Action action)
        {
            //If you are planning on using a similar function in your own code then please be sure to
            //have a quick read over https://stackoverflow.com/questions/1874728/avoid-calling-invoke-when-the-control-is-disposed
            //No action
            if (control.Disposing || control.IsDisposed || !control.IsHandleCreated)
            {
                return;
            }

            if (control.InvokeRequired)
            {
                control.BeginInvoke(action);
            }
            else
            {
                action.Invoke();
            }
        }


        private static Task UiInvokeAsync(this Control control, Action action, CancellationToken cancellationToken = default)
        {
            return control.UiInvokeAsync(() =>
            {
                action();
                return true;
            }, cancellationToken);
        }

        public static Task<T> UiInvokeAsync<T>(this Control control, Func<T> func, CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            var _owner = control;
            if (control.IsDisposed || control.Disposing)
            {
                tcs.TrySetException(new ObjectDisposedException(nameof(_owner)));
                return tcs.Task;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                tcs.TrySetCanceled(cancellationToken);
                return tcs.Task;
            }

            void Execute()
            {
                try
                {
                    if (_owner.IsDisposed || _owner.Disposing)
                    {
                        tcs.TrySetException(new ObjectDisposedException(nameof(_owner)));
                        return;
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        tcs.TrySetCanceled(cancellationToken);
                        return;
                    }

                    tcs.TrySetResult(func());
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }

            try
            {
                if (control.InvokeRequired)
                {
                    _owner.BeginInvoke((Action)Execute);
                }
                else
                {
                    Execute();
                }
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }

            return tcs.Task;
        }
    }
}
