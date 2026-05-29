
using System.Runtime.InteropServices;

namespace AdxImp.Win32
{
    public class NativeMethod
    {

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessage(
              IntPtr hWnd,
              int Msg,
              IntPtr wParam,
              ref COPYDATASTRUCT lParam
          );

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr SendMessageTimeout(
            IntPtr hWnd,
            int Msg,
            IntPtr wParam,
            ref COPYDATASTRUCT lParam,
            uint fuFlags,
            uint uTimeout,
            out IntPtr lpdwResult
        );




        [DllImport("user32.dll")]
        public static extern IntPtr FindWindowEx(IntPtr parentWindow, IntPtr previousChildWindow, string windowClass, string windowTitle);
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowThreadProcessId(IntPtr window, out int process);
        public static IntPtr[] GetProcessWindows(int process, string title)
        {
            IntPtr[] apRet = (new IntPtr[256]);
            int iCount = 0;
            IntPtr pLast = IntPtr.Zero;
            do
            {
                pLast = FindWindowEx(IntPtr.Zero, pLast, null, title);
                int iProcess_;
                GetWindowThreadProcessId(pLast, out iProcess_);
                if (iProcess_ == process) apRet[iCount++] = pLast;
            } while (pLast != IntPtr.Zero);
            System.Array.Resize(ref apRet, iCount);
            return apRet;
        }


    }
}