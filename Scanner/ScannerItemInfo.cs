using System;
using System.Collections.Generic;

namespace Scanner
{
    public class ScannerItemInfo
    {
        public float R1;
        public float R2;
        public float StartAng;
        public float EndAng;
        public long Size;
        public long HiddenItemsSize;
        public List<ScannerItemInfo> Items = new List<ScannerItemInfo>();

        public virtual string Name { get; set; }

        public void CalcSize()
        {
            if (Items.Count == 0) return;
            Size = HiddenItemsSize;
            foreach (var item in Items)
            {
                item.CalcSize();
                Size += item.Size;
            }
        }
    }
}
