using System.Drawing;

namespace Scanner
{
    public class UIButton
    {
        public bool Hovered { get; set; }
        public int Top;
        public int Left;
        public int Width = 100;
        public int Height = 30;
        public bool IsInside(int x, int y)
        {
            return new Rectangle(Left, Top, Width, Height).IntersectsWith(new Rectangle(x, y, 1, 1));
        }

    }

}
