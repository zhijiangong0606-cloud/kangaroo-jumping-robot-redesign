using System;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Verify + render: open assembly, report rebuild errors and mate count, then save
// isometric + front + right PNG renders for visual confirmation.
class VerifyRender
{
    static readonly string Dir =
        @"C:\Users\Gzj\Desktop\kangaroo_robot_redesign\solidworks_basic_model\kangaroo_overleap_bioinspired_v1\";
    static readonly string Asm = Dir + "Kangaroo_Overleap_BioInspired_Assembly.SLDASM";

    [STAThread]
    static void Main()
    {
        var sw = (SldWorks)Activator.CreateInstance(Type.GetTypeFromProgID("SldWorks.Application"));
        sw.Visible = true;
        int e = 0, w = 0;
        var model = (ModelDoc2)sw.OpenDoc6(Asm, (int)swDocumentTypes_e.swDocASSEMBLY,
            (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", ref e, ref w);
        var asm = (AssemblyDoc)model;

        int rebuildErr = model.ForceRebuild3(false) ? 1 : 0;
        Console.WriteLine("forceRebuild returned=" + rebuildErr);

        // count mates
        int mates = 0;
        var feat = (Feature)model.FirstFeature();
        while (feat != null)
        {
            if (feat.GetTypeName2() == "MateGroup")
            {
                var sub = (Feature)feat.GetFirstSubFeature();
                while (sub != null) { if (sub.GetTypeName2()=="MateConcentric"||sub.GetTypeName2()=="MateCoincident") mates++; sub = (Feature)sub.GetNextSubFeature(); }
            }
            feat = (Feature)feat.GetNextFeature();
        }
        Console.WriteLine("mate count=" + mates);

        object[] comps = (object[])asm.GetComponents(true);
        Console.WriteLine("components=" + comps.Length);

        model.ShowNamedView2("*Isometric", -1);
        model.ViewZoomtofit2();
        Save(model, Dir + "render_iso_v4.png");

        model.ShowNamedView2("*Front", -1);
        model.ViewZoomtofit2();
        Save(model, Dir + "render_front_v4.png");

        model.ShowNamedView2("*Right", -1);
        model.ViewZoomtofit2();
        Save(model, Dir + "render_right_v4.png");

        sw.CloseDoc(model.GetTitle());
        Console.WriteLine("DONE");
    }

    static void Save(ModelDoc2 m, string path)
    {
        int err = m.SaveAs3(path, 0, 2);
        Console.WriteLine("img " + System.IO.Path.GetFileName(path) + " err=" + err);
    }
}
