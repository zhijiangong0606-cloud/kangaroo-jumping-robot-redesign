using System;
using System.Collections.Generic;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Colored render: open assembly, assign a bright material color per component class
// (cage / each link / foot / pins / modules), switch to shaded-with-edges, maximize
// the window for resolution, then export high-res PNGs from iso/front/right/back.
class RenderColored
{
    static readonly string Dir =
        @"C:\Users\Gzj\Desktop\kangaroo_robot_redesign\solidworks_basic_model\kangaroo_overleap_bioinspired_v1\";
    static readonly string Asm = Dir + "Kangaroo_Overleap_BioInspired_Assembly.SLDASM";
    static SldWorks sw;
    static ModelDoc2 model;

    // 9-value material array: R,G,B, ambient, diffuse, specular, shininess, transparency, emission
    static double[] Mat(double r, double g, double b)
    {
        return new double[] { r, g, b, 1.0, 1.0, 0.4, 0.35, 0.0, 0.0 };
    }

    // prefix -> color (0..1 RGB)
    static readonly List<KeyValuePair<string, double[]>> Palette = new List<KeyValuePair<string, double[]>>
    {
        new KeyValuePair<string,double[]>("BodyPlate", Mat(0.30,0.35,0.42)),  // graphite blue-grey
        new KeyValuePair<string,double[]>("Standoff",  Mat(0.55,0.58,0.62)),  // steel grey
        new KeyValuePair<string,double[]>("L1_Crank",  Mat(0.90,0.30,0.24)),  // red
        new KeyValuePair<string,double[]>("L2_Coupler",Mat(0.95,0.62,0.10)),  // orange
        new KeyValuePair<string,double[]>("L3_Thigh",  Mat(0.30,0.65,0.30)),  // green
        new KeyValuePair<string,double[]>("L4_Shank",  Mat(0.15,0.55,0.80)),  // blue
        new KeyValuePair<string,double[]>("L5_Rocker", Mat(0.55,0.35,0.75)),  // purple
        new KeyValuePair<string,double[]>("Foot",      Mat(0.95,0.80,0.15)),  // yellow
        new KeyValuePair<string,double[]>("TailRod",   Mat(0.80,0.45,0.20)),  // brown-orange
        new KeyValuePair<string,double[]>("TailMass",  Mat(0.20,0.20,0.22)),  // near black
        new KeyValuePair<string,double[]>("M3_Axle",   Mat(0.10,0.10,0.12)),  // dark pins
        new KeyValuePair<string,double[]>("Motor",     Mat(0.25,0.45,0.70)),  // module blue
        new KeyValuePair<string,double[]>("Servo",     Mat(0.70,0.20,0.45)),  // magenta
        new KeyValuePair<string,double[]>("Drum",      Mat(0.40,0.70,0.65)),  // teal
        new KeyValuePair<string,double[]>("Latch",     Mat(0.85,0.55,0.55)),  // salmon
        new KeyValuePair<string,double[]>("ElasticTendon", Mat(0.95,0.90,0.30)), // bright yellow
    };

    [STAThread]
    static void Main()
    {
        try { Run(); }
        catch (Exception ex) { Console.WriteLine(ex.GetType().FullName + ": " + ex.Message); }
    }

    static void Run()
    {
        sw = (SldWorks)Activator.CreateInstance(Type.GetTypeFromProgID("SldWorks.Application"));
        sw.Visible = true;
        int e = 0, w = 0;
        model = (ModelDoc2)sw.OpenDoc6(Asm, (int)swDocumentTypes_e.swDocASSEMBLY,
            (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", ref e, ref w);
        var asm = (AssemblyDoc)model;

        // Maximize for resolution (swWindowState_e.swWindowStateMaximized == 1).
        try { sw.FrameState = 1; } catch { }

        // Color each component by class.
        object[] comps = (object[])asm.GetComponents(true);
        int colored = 0;
        foreach (Component2 comp in comps)
        {
            if (comp == null) continue;
            string nm = comp.Name2;
            double[] mat = null;
            foreach (var kv in Palette) if (nm.StartsWith(kv.Key)) { mat = kv.Value; break; }
            if (mat == null) continue;
            try
            {
                comp.SetMaterialPropertyValues2(mat,
                    (int)swInConfigurationOpts_e.swThisConfiguration, "");
                colored++;
            }
            catch (Exception ex) { Console.WriteLine("color fail " + nm + ": " + ex.Message); }
        }
        Console.WriteLine("colored=" + colored + "/" + comps.Length);

        model.ForceRebuild3(false);

        // Shaded with edges (swViewDisplayMode_e.swVIEWDISPLAY_SHADEDWITHEDGES == 4).
        try
        {
            var mv = (ModelView)model.ActiveView;
            mv.DisplayMode = 4;
        }
        catch (Exception ex) { Console.WriteLine("dispmode: " + ex.Message); }

        // Standard view IDs (swStandardViews_e): Front=1 Right=4 Isometric=7 Dimetric=9.
        // Passing the integer viewId is far more reliable headless than the "*Name"
        // string with -1, which was leaving every capture in the same orientation.
        Shot(7, "render_iso_v5.png");
        Shot(1, "render_front_v5.png");
        Shot(4, "render_right_v5.png");
        Shot(9, "render_dimetric_v5.png");

        sw.CloseDoc(model.GetTitle());
        Console.WriteLine("DONE");
    }

    static void Shot(int viewId, string file)
    {
        model.ShowNamedView2("", viewId);
        model.ViewZoomtofit2();
        model.GraphicsRedraw2();
        System.Threading.Thread.Sleep(400); // let the view settle before grabbing the buffer
        // High-res device-independent export.
        bool ok = model.SaveBMP(Dir + file.Replace(".png", ".bmp"), 1800, 1350);
        int err = model.SaveAs3(Dir + file, 0, 2);
        Console.WriteLine(file + " pngErr=" + err + " bmpOk=" + ok);
    }
}
