using System.Runtime.InteropServices;

namespace PSEverything.PInvoke
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct ChangeFilterStruct
    {
        public uint Size;
        public MessageFilterInfo Info;
    }
}