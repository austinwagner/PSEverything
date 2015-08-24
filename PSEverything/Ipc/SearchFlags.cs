using System;

namespace PSEverything.Ipc
{
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
}