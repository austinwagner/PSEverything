using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using PSEverything.Unmanaged;

namespace PSEverything.Ipc
{
    internal class ResultList
    {
        public uint TotalFolders;
        public uint TotalFiles;
        public uint TotalItems;
        public uint NumFolders;
        public uint NumFiles;
        public uint NumItems;
        public uint Offset;
        public List<ResultItem> Items = new List<ResultItem>();

        public ResultList(IntPtr ptr)
        {
            var reader = new UnmanagedMemoryReader(ptr);
            TotalFolders = reader.ReadUInt32();
            TotalFiles = reader.ReadUInt32();
            TotalItems = reader.ReadUInt32();
            NumFolders = reader.ReadUInt32();
            NumFiles = reader.ReadUInt32();
            NumItems = reader.ReadUInt32();
            Offset = reader.ReadUInt32();

            for (int i = 0; i < NumItems; i++)
            {
                var flags = (ItemFlags)reader.ReadUInt32();
                var fileOffset = (int)reader.ReadUInt32();
                var pathOffset = (int)reader.ReadUInt32();

                Items.Add(new ResultItem
                {
                    Flags = flags,
                    Filename = Marshal.PtrToStringUni(ptr + fileOffset),
                    Path = Marshal.PtrToStringUni(ptr + pathOffset)
                });
            }
        }
    }
}