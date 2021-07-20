using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Scanner
{
    public partial class ScannerWindow : Form
    {
        public ScannerWindow()
        {
            InitializeComponent();
            bmp = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            gr = Graphics.FromImage(bmp);
            toolStripProgressBar1.MarqueeAnimationSpeed = 0;
            pictureBox1.SizeChanged += PictureBox1_SizeChanged;
            DragDrop += ScannerWindow_DragDrop;
            DragEnter += ScannerWindow_DragEnter;
            Load += ScannerWindow_Load;
            MouseWheel += ScannerWindow_MouseWheel;
            Task.Run(() =>
            {
                lock (btns)
                {
                    btns.Add(new UIFolderButton() { Name = "Downloads", Path = KnownFolders.GetPath(KnownFolder.Downloads) });
                    btns.Add(new UIFolderButton() { Name = "Documents", Path = KnownFolders.GetPath(KnownFolder.Documents) });
                    btns.Add(new UIFolderButton() { Name = "Pictures", Path = KnownFolders.GetPath(KnownFolder.Pictures) });
                    btns.Add(new UIFolderButton() { Name = "Videos", Path = KnownFolders.GetPath(KnownFolder.Videos) });
                    btns.Add(new UIFolderButton() { Name = "Music", Path = KnownFolders.GetPath(KnownFolder.Music) });
                }

                Parallel.ForEach(DriveInfo.GetDrives(), (item) =>
                {
                    if (!item.IsReady) return;
                    lock (btns)
                    {
                        btns.Add(new UIDiskButton()
                        {
                            Drive = item,
                            AvailableFreeSpace = item.AvailableFreeSpace,
                            TotalSize = item.TotalSize
                        });
                    }
                });
            });
        }
        private void ScannerWindow_MouseWheel(object sender, MouseEventArgs e)
        {
            radb += Math.Sign(e.Delta) * 5;
            if (radb < 20) radb = 20;
            if (radb > 240) radb = 240;
        }

        private void ScannerWindow_Load(object sender, EventArgs e)
        {
            mf = new MessageFilter();
            Application.AddMessageFilter(mf);
        }


        MessageFilter mf = null;
        private void ScannerWindow_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        private void ScannerWindow_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string file in files)
            {
                FileAttributes attr = File.GetAttributes(file);
                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    Init(new DirectoryInfo(file));
                }
                else
                {
                    Init(new FileInfo(file).Directory);
                }
            }
        }

        private void PictureBox1_SizeChanged(object sender, EventArgs e)
        {
            bmp = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            gr = Graphics.FromImage(bmp);
        }

        Bitmap bmp;
        Graphics gr;

        GraphicsPath GetSector(float startAngle, float endAngle, float r1, float r2)
        {
            GraphicsPath ret = new GraphicsPath();

            ret.AddArc(-r1, -r1, 2 * r1, 2 * r1, startAngle, endAngle - startAngle);
            GraphicsPath gptemp = new GraphicsPath();
            gptemp.AddArc(-r2, -r2, 2 * r2, 2 * r2, startAngle, endAngle - startAngle);

            ret.AddLine(ret.GetLastPoint(), gptemp.GetLastPoint());
            ret.AddArc(-r2, -r2, 2 * r2, 2 * r2, endAngle, -(endAngle - startAngle));
            ret.AddLine(gptemp.PathPoints[0], ret.PathPoints[0]);
            ret.CloseAllFigures();
            return ret;
        }

        void DrawLayer(Graphics gr, ScannerItemInfo scd, float r1, float r2, float parentStartAng, float parentEndAng)
        {
            float crntAng = parentStartAng;
            foreach (var item in scd.Items)
            {
                float perc = item.Size / (float)scd.Size;
                var deltaAng = parentEndAng - parentStartAng;
                var ang = deltaAng * perc;

                if (ang >= 1)
                {
                    item.EndAng = crntAng + ang;
                    item.R1 = r1;
                    item.R2 = r2;
                    item.StartAng = crntAng;
                    var gp = GetSector(crntAng, crntAng + ang, r1, r2);
                    var d1 = item as ScannerDirInfo;

                    if (hovered == item)
                    {
                        gr.FillPath(Brushes.LightGreen, gp);
                    }
                    else
                    {
                        if (item is ScannerFileInfo)
                        {
                            gr.FillPath(Brushes.LightYellow, gp);
                        }
                        else
                        {
                            gr.FillPath(Brushes.LightBlue, gp);
                        }
                    }
                    gr.DrawPath(Pens.Black, gp);
                    DrawLayer(gr, item, r1 + radInc, r2 + radInc, crntAng, crntAng + ang);
                    crntAng += ang;
                }
            }
        }

        bool ready = false;

        ScannerItemInfo hovered = null;
        int radInc = 20;
        int radb = 50;
        private void Timer1_Tick(object sender, EventArgs e)
        {
            var pos = pictureBox1.PointToClient(Cursor.Position);
            bool _hovered = false;
            lock (btns)
                foreach (var item in btns)
                {
                    item.Hovered = false;
                    if (item.IsInside(pos.X, pos.Y))
                    {
                        _hovered = true;
                        item.Hovered = true;
                    }
                }

            if (pictureBox1.ClientRectangle.IntersectsWith(new Rectangle(pos.X, pos.Y, 1, 1)))
            {
                Cursor = Cursors.Default;
                if (_hovered)
                    Cursor = Cursors.Hand;
            }


            ////
            gr.SmoothingMode = SmoothingMode.AntiAlias;
            gr.Clear(Color.White);
            gr.ResetTransform();


            if (ready)
            {
                hovered = null;
                var dist = Math.Sqrt(Math.Pow(pos.X - pictureBox1.Width / 2, 2) + Math.Pow(pos.Y - pictureBox1.Height / 2, 2));
                if (dist < radb)
                {
                    hovered = Root;
                }
                var ang = Math.Atan2(pos.Y - pictureBox1.Height / 2, pos.X - pictureBox1.Width / 2) * 180f / Math.PI;
                if (ang < 0) { ang += 360f; }
                foreach (var item in Items)
                {

                    if (dist >= item.R1 && dist <= item.R2 && ang >= item.StartAng && ang <= item.EndAng)
                    {
                        hovered = item;
                    }
                }
                gr.DrawString((Root as ScannerDirInfo).GetDirFullName(), new Font("Arial", 12), Brushes.Black, 5, 5);
                if (hovered != null)
                {

                    gr.DrawString(hovered.Name, new Font("Arial", 12), Brushes.Black, 5, 25);
                    gr.DrawString(Stuff.GetUserFriendlyFileSize(hovered.Size), new Font("Arial", 12), Brushes.Black, 5, 45);
                }


                gr.TranslateTransform(bmp.Width / 2, bmp.Height / 2);


                if (Root == hovered)
                {
                    gr.FillEllipse(Brushes.Green, -radb, -radb, 2 * radb, 2 * radb);
                }
                else
                {
                    gr.FillEllipse(Brushes.Violet, -radb, -radb, 2 * radb, 2 * radb);
                }
                gr.DrawEllipse(Pens.Black, -radb, -radb, 2 * radb, 2 * radb);
                var sz = Stuff.GetUserFriendlyFileSize(Root.Size);
                var ff = new Font("Arial", 12);
                var ms = gr.MeasureString(sz, ff);

                gr.DrawString(sz, ff, Brushes.White, -ms.Width / 2, 0);

                sz = Root.Name;
                ms = gr.MeasureString(sz, ff);

                gr.DrawString(sz, ff, Brushes.White, -ms.Width / 2, -ms.Height);
                DrawLayer(gr, Root, radb, radb + radInc, 0, 360);
            }

            gr.ResetTransform();
            int dyy = 80;

            lock (btns)
            {
                int left = 5;
                foreach (var item in btns.OfType<UIDiskButton>().OrderBy(z => z.Drive.Name))
                {
                    item.Left = left;
                    item.Top = dyy;
                    gr.FillEllipse(Brushes.LightBlue, left, dyy, 20, 20);
                    gr.DrawEllipse(Pens.Black, left, dyy, 20, 20);
                    gr.FillEllipse(Brushes.White, left + 7.5f, dyy + 7.5f, 5, 5);
                    gr.DrawEllipse(Pens.Black, left + 7.5f, dyy + 7.5f, 5, 5);
                    gr.DrawString($"{item.Drive.Name} ({item.AvailableFreeSpace / 1024 / 1024 / 1024:N2} / {item.TotalSize / 1024 / 1024 / 1024:N2}Gb)", SystemFonts.DefaultFont, Brushes.Black, 35, dyy + 5);
                    dyy += 30;
                }

                foreach (var item in btns.OfType<UIFolderButton>())
                {
                    item.Left = left;
                    item.Top = dyy;
                    gr.FillRectangle(Brushes.LightBlue, left, dyy + 5, 20, 15);
                    gr.DrawRectangle(Pens.Black, left, dyy + 5, 20, 15);
                    gr.FillRectangle(Brushes.LightBlue, left + 10, dyy + 2, 10, 3);
                    gr.DrawRectangle(Pens.Black, left + 10, dyy + 2, 10, 3);

                    gr.DrawString($"{item.Name}", SystemFonts.DefaultFont, Brushes.Black, 35, dyy + 5);
                    dyy += 30;
                }
            }

            pictureBox1.Image = bmp;
        }

        List<UIButton> btns = new List<UIButton>();

        void ReportProgress(int i, int max)
        {
            statusStrip1.Invoke((Action)(() =>
            {
                toolStripProgressBar1.Maximum = max;
                toolStripProgressBar1.Value = i;
            }));
        }

        public List<ScannerItemInfo> Items = new List<ScannerItemInfo>();
        public ScannerItemInfo Root = null;

        Thread th;
        internal void Init(DirectoryInfo d)
        {
            if (th != null) return;
            GC.Collect();
            toolStripProgressBar1.Visible = true;
            toolStripProgressBar1.Value = 0;
            timer2.Enabled = false;
            ready = false;
            Items.Clear();
            Root = null;
            //files.Clear();
            th = new Thread(() =>
           {
               var dirs = Stuff.GetAllDirs(d);

               Queue<ScannerItemInfo> q = new Queue<ScannerItemInfo>();
               int cnt = 0;
               Root = new ScannerDirInfo(null, d);
               pictureBox1.Invoke((Action)(() =>
               {
                   Text = d.FullName;
               }));

               q.Enqueue(Root);
               Items.Add(Root);
               while (q.Any())
               {
                   ReportProgress(cnt, dirs.Count);
                   var _dd = q.Dequeue();
                   if (_dd is ScannerFileInfo) continue;

                   var dd = _dd as ScannerDirInfo;                   
                   cnt++;
                   try
                   {
                       foreach (var item in dd.Dir.GetFiles())
                       {                           
                           dd.Items.Add(new ScannerFileInfo(dd, item));
                           Items.Add(dd.Items.Last());                           
                       }
                   }
                   catch 
                   {

                   }
                   try
                   {
                       foreach (var item in dd.Dir.GetDirectories())
                       {
                           var t = new ScannerDirInfo(dd, item);
                           Items.Add(t);
                           dd.Items.Add(t);
                           q.Enqueue(t);
                       }
                   }
                   catch 
                   {

                   }
                   if (dd.Parent != null)
                   {
                       dd.Dir = null;
                   }
               }
               ReportProgress(cnt, cnt);
               Root.CalcSize();
               ready = true;
               statusStrip1.Invoke((Action)(() =>
               {
                   timer2.Enabled = true;
                   timer2.Interval = 2000;
               }));

               th = null;
           });
            th.IsBackground = true;
            th.Start();
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (temp != null && temp is ScannerDirInfo)
            {
                var d = temp as ScannerDirInfo;
                Process.Start(d.GetDirFullName());
            }
        }

        ScannerItemInfo temp = null;
        private void ContextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            temp = hovered;
        }

        private void PictureBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (hovered == null) return;
            if (!(hovered is ScannerDirInfo)) return;

            var d = hovered as ScannerDirInfo;
            var dd = new DirectoryInfo(d.GetDirFullName());
            if (hovered == Root)
            {
                Init(dd.Parent);
            }
            else
            {
                Init(dd);
            }
        }
                

        private void Timer2_Tick(object sender, EventArgs e)
        {
            toolStripProgressBar1.Visible = false;
            timer2.Enabled = false;
        }

        private void ScannerWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (th != null)
            {
                try
                {
                    th.Abort();
                }
                catch 
                {

                }
            }
        }
                

        private void DeleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //if (MessageBox.Show($"Are you sure to delete {34}") != DialogResult.Yes) return;

        }

        private void RefreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dd = new DirectoryInfo((Root as ScannerDirInfo).GetDirFullName());
            Init(dd);
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {            
            var pos = pictureBox1.PointToClient(Cursor.Position);
            lock (btns)
            {
                foreach (var item in btns.OfType<UIFolderButton>())
                {
                    if (item.IsInside(pos.X, pos.Y))
                    {
                        Init(new DirectoryInfo(item.Path));
                        return;
                    }
                }

                foreach (var item in btns.OfType<UIDiskButton>())
                {
                    if (item.IsInside(pos.X, pos.Y))
                    {
                        Init(item.Drive.RootDirectory);
                        return;
                    }
                }
            }
        }
    }
}