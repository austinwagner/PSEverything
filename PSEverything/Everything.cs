using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using PSEverything.Ipc;
using PSEverything.PInvoke;
using PSEverything.Unmanaged;

namespace PSEverything
{
    public sealed class Everything : NativeWindow
    {
        private const string EverythingIpcClass = "EVERYTHING_TASKBAR_NOTIFICATION";

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
            return _flags.HasFlag(flag);
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
                Size = (uint) Marshal.SizeOf(typeof (ChangeFilterStruct))
            };

            if (!Win32.ChangeWindowMessageFilterEx(Handle, WindowMessage.CopyData, ChangeWindowMessageFilterExAction.Allow, ref cs))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Error allowing WM_COPYDATA mesasage from lower privilege processes.");
            }
        }

        public IList<FileSystemInfo> Search(string searchString)
        {
            _results.Clear();
            _resultsReady.Reset();

            var ipcWindow = Win32.FindWindow(EverythingIpcClass, "");
            if (ipcWindow == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Error finding Everything Search IPC message window.");
            }

            var query = new Query(Handle, _flags, searchString);
            var cdsPtr = new SafeHeapPtr<CopyDataStructure>(query.ToCopyDataStructure());
            if (Win32.SendMessage(ipcWindow, WindowMessage.CopyData, Handle, cdsPtr) == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Error sending IPC request to Everything Search.");
            }

            if (!_resultsReady.WaitOne(10000))
            {
                throw new TimeoutException("Timed out waiting for response from Everything Search.");
            }

            return _results.ToList();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == (uint)WindowMessage.CopyData)
            {
                var cds = m.GetLParam<CopyDataStructure>();
                if (cds.dwData == IntPtr.Zero)
                {
                    var results = new ResultList(cds.lpData);
                    foreach (var item in results.Items)
                    {
                        _results.Add(ToFileSystemInfo(item));
                    }

                    _resultsReady.Set();
                }

                m.Result = new IntPtr(1);
            }

            DefWndProc(ref m);
        }

        private static FileSystemInfo ToFileSystemInfo(ResultItem item)
        {
            if (item.Flags.HasFlag(ItemFlags.Folder))
            {
                return new DirectoryInfo(item.FullPath);
            }

            return new FileInfo(item.FullPath);
        }
    }
}
