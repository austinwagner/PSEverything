using System;

namespace PSEverything.Ipc
{
    [Flags]
    internal enum ItemFlags : uint
    {
        File = 0,
        Folder = 1,
        Drive = 2
    }
}