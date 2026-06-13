using System;
using System.IO;
using System.Threading;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Reopen the saved assembly and capture genuinely distinct oriented views.
class ShootViews
{
    static readonly string Root =
        @"C:\Users\Gzj\Desktop\kangaroo_robot_redesign\solidworks_basic_model\kangaroo_overleap_bioinspired_v1";

    [STAThread]
    static void Main()
    {
        try { Run(); }
        catch (Exception ex) { Console.WriteLine(ex.GetType().FullName + ": " + ex.Message); }
    }

    static void Run()
    {
        var swApp = (SldWorks)Activator.CreateInstance(Type.GetTypeFromProgID("SldWorks.Application"));
        swApp.Visible = true;
        string asmPath = Path.Combine(Root, "Kangaroo_Overleap_BioInspired_Assembly.SLDASM");
        int e = 0, w = 0;
        var model = (ModelDoc2)swApp.OpenDoc6(asmPath, (int)swDocumentTypes_e.swDocASSEMBLY,
            (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", ref e, ref w);
        swApp.ActivateDoc3(model.GetTitle(), false,
            (int)swRebuildOnActivation_e.swDontRebuildActiveDoc, ref e);
        model.ShowConfiguration2("Default");

        // (viewName, file). Named views are reliable when the doc is active.
        var views = new[] {
            new[]{ "*Isometric", "view_isometric.bmp" },
            new[]{ "*Right",     "view_right.bmp" },
            new[]{ "*Front",     "view_front.bmp" },
            new[]{ "*Dimetric",  "view_dimetric.bmp" },
        };
        foreach (var v in views)
        {
            model.ShowNamedView2(v[0], -1);
            model.ViewZoomtofit2();
            model.GraphicsRedraw2();
            Thread.Sleep(700);
            bool ok = model.SaveBMP(Path.Combine(Root, v[1]), 1600, 1200);
            Console.WriteLine(v[1] + " ok=" + ok);
        }
        Console.WriteLine("SHOTS DONE");
    }
}
