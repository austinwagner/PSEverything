using System;
using System.Runtime.InteropServices;
using PSEverything.PInvoke;
using PSEverything.Unmanaged;

namespace PSEverything.Ipc
{
    internal class Query
    {
        private readonly IntPtr _ptr;

        public int Size { get; }

        public Query(IntPtr replyHwnd, SearchFlags flags, string searchString)
        {
            Size = 22 + searchString.Length * 2;
            _ptr = Marshal.AllocHGlobal(Size);
            var writer = new UnmanagedMemoryWriter(_ptr);
            // HWND reply_hwnd
            writer.Write((uint)replyHwnd.ToInt64());
            // ULONG_PTR reply_copydata_message (ULONG_PTR is defined as unsigned __int3264, but uint seems 
            //                                   to work here even when running both processes as 64-bit)
            writer.Write(0u);
            // DWORD search_flags
            writer.Write((uint)flags);
            // DWORD offset
            writer.Write(0u);
            // DWORD max_results
            writer.Write(uint.MaxValue);
            // DWORD search_string[1]
            writer.Write(searchString);
        }

        ~Query()
        {
            Marshal.FreeHGlobal(_ptr);
        }

        public CopyDataStructure ToCopyDataStructure()
        {
            // Is there a way to be sure that we don't free the Query instance before
            // the CopyDataStructure?
            return new CopyDataStructure
            {
                cbData = Size,
                dwData = new IntPtr(2),
                lpData = _ptr
            };
        }
    }
}