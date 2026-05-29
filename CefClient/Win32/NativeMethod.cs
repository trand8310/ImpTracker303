using System;
using System.Runtime.InteropServices;

namespace Imp.Win32
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


    }
}