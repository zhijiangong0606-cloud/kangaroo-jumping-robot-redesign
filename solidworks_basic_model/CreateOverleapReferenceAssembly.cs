using System;
using System.IO;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

class CreateOverleapReferenceAssembly
{
    const double MM = 0.001;
    static readonly string ProjectRoot = @"C:\Users\Gzj\Desktop\kangaroo_robot_redesign";
    static readonly string SourceParts = Path.Combine(ProjectRoot, @"external_references\overleap\Parts");
    static readonly string OutRoot = Path.Combine(ProjectRoot, @"solidworks_basic_model\overleap_reference_model");
    static readonly string OutParts = Path.Combine(OutRoot, "imported_parts");
    static readonly string TemplateAsm = @"C:\ProgramData\SOLIDWORKS\SOLIDWORKS 2026\templates\gb_assembly.asmdot";

    [STAThread]
    static void Main()
    {
        try { Run(); }
        catch (Exception ex)
        {
            Console.WriteLine(ex.GetType().FullName);
            Console.WriteLine(ex.Message);
        }
    }

    static void Run()
    {
        Directory.CreateDirectory(OutRoot);
        Directory.CreateDirectory(OutParts);

        var swType = Type.GetTypeFromProgID("SldWorks.Application");
        var swApp = (SldWorks)Activator.CreateInstance(swType);
        swApp.Visible = true;

        string[] stls = Directory.GetFiles(SourceParts, "*.stl");
        Array.Sort(stls);

        foreach (var stl in stls)
        {
            ImportStlToPart(swApp, stl);
        }

        var model = (ModelDoc2)swApp.NewDocument(TemplateAsm, 0, 0, 0);
        var asm = (AssemblyDoc)model;

        foreach (var part in Directory.GetFiles(OutParts, "*.SLDPRT"))
        {
            AddPartAtOrigin(swApp, asm, part);
        }

        model.ShowNamedView2("*Isometric", 7);
        model.ViewZoomtofit2();
        model.ForceRebuild3(false);
        int err = model.SaveAs3(Path.Combine(OutRoot, "Overleap_reference_imported_assembly.SLDASM"), 0, 2);
        Console.WriteLine("assembly save err=" + err);
    }

    static void ImportStlToPart(SldWorks swApp, string stlPath)
    {
        string baseName = Path.GetFileNameWithoutExtension(stlPath).Replace(" ", "_");
        string outPath = Path.Combine(OutParts, baseName + ".SLDPRT");
        if (File.Exists(outPath))
        {
            Console.WriteLine("exists " + Path.GetFileName(outPath));
            return;
        }

        int errors = 0, warnings = 0;
        var model = (ModelDoc2)swApp.OpenDoc6(
            stlPath,
            (int)swDocumentTypes_e.swDocPART,
            (int)swOpenDocOptions_e.swOpenDocOptions_Silent,
            "",
            ref errors,
            ref warnings
        );
        if (model == null)
        {
            Console.WriteLine("import failed " + stlPath + " errors=" + errors);
            return;
        }
        model.ForceRebuild3(false);
        int saveErr = model.SaveAs3(outPath, 0, 2);
        Console.WriteLine("imported " + Path.GetFileName(stlPath) + " saveErr=" + saveErr);
        swApp.CloseDoc(model.GetTitle());
    }

    static void AddPartAtOrigin(SldWorks swApp, AssemblyDoc asm, string partPath)
    {
        int errors = 0, warnings = 0;
        swApp.OpenDoc6(partPath, (int)swDocumentTypes_e.swDocPART, (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", ref errors, ref warnings);
        var comp = asm.AddComponent5(partPath, (int)swAddComponentConfigOptions_e.swAddComponentConfigOptions_CurrentSelectedConfig, "", false, "", 0, 0, 0);
        Console.WriteLine((comp == null ? "FAILED " : "added ") + Path.GetFileName(partPath));
    }
}
