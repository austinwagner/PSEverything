using System;
using System.Runtime.InteropServices;

namespace PSEverything.PInvoke
{
    static internal class Win32
    {
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr SendMessage(IntPtr hWnd, WindowMessage msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool ChangeWindowMessageFilterEx(IntPtr hWnd, WindowMessage msg, ChangeWindowMessageFilterExAction action, ref ChangeFilterStruct changeInfo);
    }
}