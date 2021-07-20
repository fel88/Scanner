using System.IO;
using System.Text;

namespace Scanner
{
    public class ScannerDirInfo : ScannerItemInfo
    {
        public ScannerDirInfo(ScannerDirInfo prnt, DirectoryInfo d)
        {
            Parent = prnt;
            Dir = d;
            Name = d.Name;
        }
        public ScannerDirInfo Parent;

        public DirectoryInfo Dir;
        public override string Name { get; set; }

        public string GetDirFullName()
        {
            ScannerDirInfo d = this;
            StringBuilder sb = new StringBuilder();

            while (d != null)
            {
                if (d.Parent == null)
                {
                    sb.Insert(0, d.Dir.FullName + "\\");
                }
                else
                {
                    sb.Insert(0, d.Name + "\\");
                }
                d = d.Parent;
            }


            return sb.ToString();

        }        
    }
}
