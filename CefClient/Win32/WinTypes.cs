namespace Imp.Win32
{
    using System;
    using System.Runtime.InteropServices;


    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct COPYDATASTRUCT
    {
        public IntPtr dwData;
        public int cbData;

        [MarshalAs(UnmanagedType.LPWStr)]
        public string lpData;
    }
    public class WinTypes
    {
        public const int WM_COPYDATA = 0x004A;
        public const int WM_MYSYMPLE = 0x005A;

        public const uint SMTO_NORMAL = 0x0000;
        public const uint SMTO_BLOCK = 0x0001;
        public const uint SMTO_ABORTIFHUNG = 0x0002;
        public const uint SMTO_NOTIMEOUTIFNOTHUNG = 0x0008;
    }
}
