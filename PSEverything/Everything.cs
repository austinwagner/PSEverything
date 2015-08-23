using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace PSEverything
{
    public sealed class Everything : NativeWindow
    {
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ChangeWindowMessageFilterEx(IntPtr hWnd, uint msg, ChangeWindowMessageFilterExAction action, ref ChangeFilterStruct changeInfo);

        [StructLayout(LayoutKind.Sequential)]
        private struct CopyData
        { 
            public IntPtr dwData;
            public int cbData;
            public IntPtr lpData;
        }

        private enum MessageFilterInfo : uint
        {
            None = 0, AlreadyAllowed = 1, AlreadyDisAllowed = 2, AllowedHigher = 3
        };


        private enum ChangeWindowMessageFilterExAction : uint
        {
            Reset = 0, Allow = 1, DisAllow = 2
        };

        [StructLayout(LayoutKind.Sequential)]
        private struct ChangeFilterStruct
        {
            public uint size;
            public MessageFilterInfo info;
        }

        private const string EverythingIpcClass = "EVERYTHING_TASKBAR_NOTIFICATION";

        private const uint WmCopyData = 0x4A;

        private List<FileSystemInfo> _results = new List<FileSystemInfo>();
        
        private ManualResetEvent _resultsReady = new ManualResetEvent(false);

        private SearchFlags _flags = SearchFlags.None;

        public bool MatchPath
        {
            get { return HasFlag(SearchFlags.MatchPath); }
            set { SetFlag(value, SearchFlags.MatchPath); }
        }

        public bool MatchCase
        {
            get { return HasFlag(SearchFlags.MatchCase); }
            set { SetFlag(value, SearchFlags.MatchCase); }
        }

        public bool MatchWholeWord
        {
            get { return HasFlag(SearchFlags.MatchWholeWord); }
            set { SetFlag(value, SearchFlags.MatchWholeWord); }
        }

        public bool MatchAccents
        {
            get { return HasFlag(SearchFlags.MatchAccents); }
            set { SetFlag(value, SearchFlags.MatchAccents); }
        }

        public bool Regex
        {
            get { return HasFlag(SearchFlags.Regex); }
            set { SetFlag(value, SearchFlags.Regex); }
        }

        private void SetFlag(bool value, SearchFlags flag)
        {
            if (value)
            {
                _flags |= flag;
            }
            else
            {
                _flags &= ~flag;
            }
        }

        private bool HasFlag(SearchFlags flag)
        {
            return (_flags & flag) == flag;
        }

        public Everything()
        {
            var cp = new CreateParams
            {
                Caption = "PSEverything IPC Window",
                ClassName = "Static",
                ClassStyle = 0,
                Style = 0,
                ExStyle = 0,
                X = 0,
                Y = 0,
                Height = 1,
                Width = 1,
                Parent = IntPtr.Zero,
                Param = null
            };

            CreateHandle(cp);
            
            var cs = new ChangeFilterStruct
            {
                size = (uint) Marshal.SizeOf(typeof (ChangeFilterStruct))
            };

            if (!ChangeWindowMessageFilterEx(Handle, WmCopyData, ChangeWindowMessageFilterExAction.Allow, ref cs))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Error allowing WM_COPYDATA mesasage from lower privilege processes.");
            }
        }

        public IList<FileSystemInfo> Search(string searchString)
        {
            _results.Clear();
            _resultsReady.Reset();

            var ipcWindow = FindWindow(EverythingIpcClass, "");
            if (ipcWindow == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Error finding Everything Search IPC message window.");
            }

            var query = new Query(Handle, _flags, searchString);

            var cds = new CopyData
            {
                cbData = query.Size,
                dwData = new IntPtr(2),
                lpData = query
            };

            var cdsPtr = new SafeHeapPtr<CopyData>(cds);
            if (SendMessage(ipcWindow, WmCopyData, Handle, cdsPtr) == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Error sending IPC request to Everything Search.");
            }

            _resultsReady.WaitOne();
            return _results.ToList();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmCopyData)
            {
                var cds = (CopyData)m.GetLParam(typeof(CopyData));
                if (cds.dwData == IntPtr.Zero)
                {
                    var results = new ResultList(cds.lpData);
                    foreach (var item in results.Items)
                    {
                        if ((item.Flags & ItemFlags.Folder) == ItemFlags.Folder)
                        {
                            _results.Add(new DirectoryInfo(item.FullPath));
                        }
                        else
                        {
                            _results.Add(new FileInfo(item.FullPath));
                        }
                    }

                    _resultsReady.Set();
                }

                m.Result = new IntPtr(1);
            }

            DefWndProc(ref m);
        }
    }

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

    internal class Query
    {
        private readonly IntPtr _ptr;

        public int Size { get; }

        public Query(IntPtr replyHwnd, SearchFlags flags, string searchString)
        {
            Size = 22 + searchString.Length * 2;
            _ptr = Marshal.AllocHGlobal(Size);
            var writer = new UnmanagedMemoryWriter(_ptr);
            writer.Write((uint)replyHwnd.ToInt64());
            writer.Write(0);
            writer.Write((uint)flags);
            writer.Write(0);
            writer.Write(uint.MaxValue);
            writer.Write(searchString);
        }

        ~Query()
        {
            Marshal.FreeHGlobal(_ptr);
        }

        public static implicit operator IntPtr(Query q)
        {
            return q._ptr;
        }
    }

    internal class ResultItem
    {
        public ItemFlags Flags;
        public string Filename;
        public string Path;
        public string FullPath => System.IO.Path.Combine(Path, Filename);
    }

    [Flags]
    internal enum SearchFlags : uint
    {
        None = 0,
        MatchCase = 1,
        MatchWholeWord = 2,
        MatchPath = 4,
        Regex = 8,
        MatchAccents = 16
    }

    [Flags]
    internal enum ItemFlags : uint
    {
        File = 0,
        Folder = 1,
        Drive = 2
    }

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
