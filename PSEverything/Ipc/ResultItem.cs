namespace PSEverything.Ipc
{
    internal class ResultItem
    {
        public ItemFlags Flags;
        public string Filename;
        public string Path;
        public string FullPath => System.IO.Path.Combine(Path, Filename);
    }
}