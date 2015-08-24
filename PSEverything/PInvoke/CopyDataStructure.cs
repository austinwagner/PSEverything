using System;
using System.Runtime.InteropServices;

namespace PSEverything.PInvoke
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct CopyDataStructure
    {
        public IntPtr dwData;
        public int cbData;
        public IntPtr lpData;
    }
}