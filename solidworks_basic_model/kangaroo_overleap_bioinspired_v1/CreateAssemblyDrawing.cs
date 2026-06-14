using System;
using System.IO;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Generate an assembly drawing (装配图): A3 GB sheet with front/top/right + iso
// views and an auto BOM table, from the saved assembly.
class CreateAssemblyDrawing
{
    static readonly string Root =
        @"C:\Users\Gzj\Desktop\kangaroo_robot_redesign\solidworks_basic_model\kangaroo_overleap_bioinspired_v1";
    static readonly string AsmPath =
        @"C:\Users\Gzj\Desktop\kangaroo_robot_redesign\solidworks_basic_model\kangaroo_overleap_bioinspired_v1\Kangaroo_Overleap_BioInspired_Assembly.SLDASM";
    static readonly string DrwTemplate =
        @"C:\ProgramData\SOLIDWORKS\SOLIDWORKS 2026\templates\gb_a3.drwdot";

    [STAThread]
    static void Main()
    {
        try { Run(); }
        catch (Exception ex) { Console.WriteLine(ex.GetType().FullName + ": " + ex.Message); }
    }

    static void Run()
    {
        var app = (SldWorks)Activator.CreateInstance(Type.GetTypeFromProgID("SldWorks.Application"));
        app.Visible = true;
        int e = 0, w = 0;
        app.OpenDoc6(AsmPath, (int)swDocumentTypes_e.swDocASSEMBLY,
            (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", ref e, ref w);

        var drw = (DrawingDoc)app.NewDocument(DrwTemplate, 0, 0, 0);
        var dmodel = (ModelDoc2)drw;

        // Front view -> root for projections. A3 landscape ~0.42 x 0.297 m.
        View front = drw.CreateDrawViewFromModelView3(AsmPath, "*Front", 0.13, 0.16, 0);
        if (front != null)
        {
            front.UseParentScale = false;
            front.ScaleDecimal = 1.0 / 3.0;     // 1:3
            drw.ActivateView(front.GetName2());
            // project TOP (place above front) and RIGHT (place to the right)
            drw.CreateUnfoldedViewAt3(0.13, 0.255, 0, false);  // top
            drw.CreateUnfoldedViewAt3(0.31, 0.16, 0, false);   // right
        }

        // Isometric, smaller, lower-right.
        View iso = drw.CreateDrawViewFromModelView3(AsmPath, "*Isometric", 0.33, 0.075, 0);
        if (iso != null)
        {
            iso.UseParentScale = false;
            iso.ScaleDecimal = 1.0 / 4.0;       // 1:4
        }

        dmodel.ForceRebuild3(false);

        // BOM table on the front view.
        if (front != null)
        {
            try
            {
                var bom = front.InsertBomTable4(true, 0.30, 0.28,
                    (int)swBOMConfigurationAnchorType_e.swBOMConfigurationAnchor_TopRight,
                    (int)swBomType_e.swBomType_PartsOnly, "Default", "", false,
                    (int)swNumberingType_e.swNumberingType_Detailed, false);
                Console.WriteLine("BOM=" + (bom == null ? "NULL" : "OK"));
            }
            catch (Exception ex) { Console.WriteLine("BOM err: " + ex.Message); }
        }

        dmodel.ViewZoomtofit2();
        dmodel.ForceRebuild3(false);
        int err = dmodel.SaveAs3(Path.Combine(Root, "Kangaroo_Assembly_Drawing.SLDDRW"), 0, 2);
        Console.WriteLine("DRAWING saveErr=" + err);

        bool ok = dmodel.SaveAs4(Path.Combine(Root, "Kangaroo_Assembly_Drawing.PDF"),
            (int)swSaveAsVersion_e.swSaveAsCurrentVersion,
            (int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref e, ref w);
        Console.WriteLine("PDF ret=" + ok + " err=" + e);
        Console.WriteLine("DRAWING DONE");
    }
}
