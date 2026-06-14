using System;
using System.Drawing;
using System.Drawing.Imaging;

class Resize
{
    static readonly string Dir =
        @"C:\Users\Gzj\Desktop\kangaroo_robot_redesign\solidworks_basic_model\kangaroo_overleap_bioinspired_v1\";

    [STAThread]
    static void Main()
    {
        string[] names = { "render_iso_v5", "render_front_v5", "render_right_v5", "render_dimetric_v5" };
        foreach (var n in names)
        {
            using (var src = new Bitmap(Dir + n + ".bmp"))
            {
                int w = 1280, h = (int)(src.Height * (1280.0 / src.Width));
                using (var dst = new Bitmap(w, h))
                using (var g = Graphics.FromImage(dst))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.DrawImage(src, 0, 0, w, h);
                    dst.Save(Dir + n + "_small.png", ImageFormat.Png);
                }
            }
            Console.WriteLine("ok " + n);
        }
    }
}
