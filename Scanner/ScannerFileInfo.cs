using System.IO;

namespace Scanner
{
    public class ScannerFileInfo : ScannerItemInfo
    {
        public ScannerFileInfo(ScannerDirInfo prnt,FileInfo f)
        {
            Parent = prnt;
            Name = f.Name;
            Size = f.Length;
        }
        public ScannerDirInfo Parent;
        public override string Name { get; set; }
        
    }
}
