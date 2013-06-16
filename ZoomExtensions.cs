//
// This source code is licensed in accordance with the licensing outlined
// on the main Tychaia website (www.tychaia.com).  Changes to the
// license on the website apply retroactively.
//
using System;
using System.Drawing;

namespace Redpoint.FlowGraph
{
    public static class ZoomExtensions
    {
        public static Point Apply(this Point p, float f)
        {
            return new Point((int)(p.X * f), (int)(p.Y * f));
        }
        
        public static Rectangle Apply(this Rectangle p, float f)
        {
            return new Rectangle(
                (int)(p.X * f),
                (int)(p.Y * f),
                (int)(p.Width * f),
                (int)(p.Height * f));
        }
        
        public static Rectangle Fix(this Rectangle r)
        {
            if (r.Width < 0)
            {
                r.X += r.Width;
                r.Width = Math.Abs(r.Width);
            }
            if (r.Height < 0)
            {
                r.Y += r.Height;
                r.Height = Math.Abs(r.Height);
            }
            return r;
        }
    }
}

