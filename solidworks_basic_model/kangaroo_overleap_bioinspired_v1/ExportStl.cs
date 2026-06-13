using System;
using System.IO;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Export the saved assembly to a single STL for external rendering / verification.
class ExportStl
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
        // Export STL of the whole assembly (not just one part).
        swApp.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swSTLComponentsIntoOneFile, true);
        swApp.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swExportStlUnits, (int)swLengthUnit_e.swMM);

        string asmPath = Path.Combine(Root, "Kangaroo_Overleap_BioInspired_Assembly.SLDASM");
        int e = 0, w = 0;
        var model = (ModelDoc2)swApp.OpenDoc6(asmPath, (int)swDocumentTypes_e.swDocASSEMBLY,
            (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", ref e, ref w);
        model.ForceRebuild3(false);
        int err = model.SaveAs3(Path.Combine(Root, "assembly_export.STL"), 0, 0);
        Console.WriteLine("STL export err=" + err);
    }
}
