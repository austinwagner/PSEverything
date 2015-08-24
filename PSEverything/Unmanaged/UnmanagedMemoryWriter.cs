using System;
using System.Runtime.InteropServices;
using System.Text;

namespace PSEverything.Unmanaged
{
    internal class UnmanagedMemoryWriter
    {
        private IntPtr _ptr;

        public UnmanagedMemoryWriter(IntPtr ptr)
        {
            _ptr = ptr;
        }

        public void Write(uint val)
        {
            Marshal.Copy(BitConverter.GetBytes(val), 0, _ptr, 4);
            _ptr += 4;
        }

        public void Write(string val)
        {
            var b = Encoding.Unicode.GetBytes(val + "\0");
            Marshal.Copy(b, 0, _ptr, b.Length);
        }
    }
}