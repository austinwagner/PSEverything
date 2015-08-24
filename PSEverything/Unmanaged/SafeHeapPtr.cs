using System;
using System.Runtime.InteropServices;

namespace PSEverything.Unmanaged
{
    internal class SafeHeapPtr<T>
    {
        private readonly IntPtr _ptr;

        public SafeHeapPtr(T structure)
        {
            _ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(T)));
            Marshal.StructureToPtr(structure, _ptr, false);
        }

        ~SafeHeapPtr()
        {
            Marshal.FreeHGlobal(_ptr);
        }

        public static implicit operator IntPtr(SafeHeapPtr<T> shp)
        {
            return shp._ptr;
        }
    }
}