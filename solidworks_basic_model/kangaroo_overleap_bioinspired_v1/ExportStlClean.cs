using System;
using System.IO;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Robust STL re-export: close all open docs first (free the ~$ lock that makes
// SaveAs silently fail), open the assembly fresh, force a rebuild, export binary
// STL with components merged into one file, then report the resulting file size
// so we KNOW it actually wrote geometry (SaveAs3 err=0 is not trustworthy for STL).
class ExportStlClean
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

        // 1) free any lock held by an already-open copy of the assembly
        swApp.CloseAllDocuments(true);

        // 2) STL export prefs: merge components, binary, millimetres, fine deviation
        swApp.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swSTLComponentsIntoOneFile, true);
        swApp.SetUserPreferenceToggle((int)swUserPreferenceToggle_e.swSTLBinaryFormat, true);
        swApp.SetUserPreferenceIntegerValue((int)swUserPreferenceIntegerValue_e.swExportStlUnits, (int)swLengthUnit_e.swMM);

        string asmPath = Path.Combine(Root, "Kangaroo_Overleap_BioInspired_Assembly.SLDASM");
        string outPath = Path.Combine(Root, "assembly_export.STL");

        int e = 0, w = 0;
        var model = (ModelDoc2)swApp.OpenDoc6(asmPath, (int)swDocumentTypes_e.swDocASSEMBLY,
            (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", ref e, ref w);
        if (model == null) { Console.WriteLine("OPEN FAILED e=" + e + " w=" + w); return; }
        Console.WriteLine("opened, openErr=" + e + " warn=" + w);

        model.ForceRebuild3(false);

        int err = model.SaveAs3(outPath, 0, 0);
        Console.WriteLine("SaveAs3 err=" + err);

        // 3) trustworthy check: actual file size on disk
        var fi = new FileInfo(outPath);
        Console.WriteLine("STL bytes=" + (fi.Exists ? fi.Length.ToString() : "MISSING"));
    }
}
