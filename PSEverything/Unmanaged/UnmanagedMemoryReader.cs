using System;
using System.Runtime.InteropServices;

namespace PSEverything.Unmanaged
{
    internal class UnmanagedMemoryReader
    {
        private IntPtr _ptr;

        public UnmanagedMemoryReader(IntPtr ptr)
        {
            _ptr = ptr;
        }

        public uint ReadUInt32()
        {
            var b = new byte[4];
            for (int i = 0; i < b.Length; i++)
            {
                b[i] = Marshal.ReadByte(_ptr);
                _ptr += 1;
            }

            return BitConverter.ToUInt32(b, 0);
        }
    }
}