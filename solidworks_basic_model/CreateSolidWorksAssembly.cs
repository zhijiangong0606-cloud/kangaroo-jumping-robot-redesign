using System;
using System.IO;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

class CreateSolidWorksAssembly
{
    const double MM = 0.001;

    [STAThread]
    static void Main()
    {
        try
        {
            Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine("EXCEPTION TYPE: " + ex.GetType().FullName);
            Console.WriteLine("MESSAGE: " + ex.Message);
            if (ex.InnerException != null) Console.WriteLine("INNER: " + ex.InnerException.Message);
        }
    }

    static void Run()
    {
        string folder = @"C:\Users\Gzj\Desktop\kangaroo_robot_redesign\solidworks_basic_model";
        string partFolder = Path.Combine(folder, "native_parts");
        string asmTemplate = @"C:\ProgramData\SOLIDWORKS\SOLIDWORKS 2026\templates\gb_assembly.asmdot";
        string outAsm = Path.Combine(folder, "KangarooRobot_assembled_layout_v2.SLDASM");

        var swType = Type.GetTypeFromProgID("SldWorks.Application");
        var swApp = (SldWorks)Activator.CreateInstance(swType);
        swApp.Visible = true;

        var model = (ModelDoc2)swApp.NewDocument(asmTemplate, 0, 0, 0);
        if (model == null)
        {
            Console.WriteLine("Failed to create assembly document.");
            System.Environment.Exit(1);
        }
        var asm = (AssemblyDoc)model;
        swApp.ActivateDoc3(model.GetTitle(), false, (int)swRebuildOnActivation_e.swDontRebuildActiveDoc, 0);

        Add(asm, partFolder, "BodySidePlate_verified.SLDPRT", 0, -8, 0);
        Add(asm, partFolder, "BodySidePlate_verified.SLDPRT", 0, 8, 0);
        Add(asm, partFolder, "L1_Crank_40mm.SLDPRT", -80, -18, 60);
        Add(asm, partFolder, "L1_Crank_40mm.SLDPRT", -80, 18, 60);
        Add(asm, partFolder, "L2_Coupler_120mm.SLDPRT", -43, -18, 46);
        Add(asm, partFolder, "L2_Coupler_120mm.SLDPRT", -43, 18, 46);
        Add(asm, partFolder, "L3_ThighRocker_100mm.SLDPRT", 0, -18, 0);
        Add(asm, partFolder, "L3_ThighRocker_100mm.SLDPRT", 0, 18, 0);
        Add(asm, partFolder, "L4_Shank_140mm.SLDPRT", 45, -18, 80);
        Add(asm, partFolder, "L4_Shank_140mm.SLDPRT", 45, 18, 80);
        Add(asm, partFolder, "L5_RearRocker_180mm.SLDPRT", 120, -18, 60);
        Add(asm, partFolder, "L5_RearRocker_180mm.SLDPRT", 120, 18, 60);
        Add(asm, partFolder, "FootPad_85mm.SLDPRT", -58, -18, 231);
        Add(asm, partFolder, "FootPad_85mm.SLDPRT", -58, 18, 231);
        Add(asm, partFolder, "TailRod_210mm.SLDPRT", 200, 0, -20);
        Add(asm, partFolder, "WindingDrum_r18_w24.SLDPRT", -55, 0, 80);
        Add(asm, partFolder, "GearMotor_Placeholder_60x32x26.SLDPRT", -25, 0, 90);
        Add(asm, partFolder, "Servo_Placeholder_38x35x22.SLDPRT", 135, 0, 95);
        Add(asm, partFolder, "Latch_Placeholder_50x12x8.SLDPRT", 80, 0, 72);

        model.ForceRebuild3(false);
        int err = model.SaveAs3(outAsm, (int)swSaveAsVersion_e.swSaveAsCurrentVersion, (int)swSaveAsOptions_e.swSaveAsOptions_Silent);
        Console.WriteLine("SaveAs3 errorCode=" + err);
        Console.WriteLine(outAsm);
    }

    static void Add(AssemblyDoc asm, string partFolder, string file, double x, double y, double z)
    {
        string path = Path.Combine(partFolder, file);
        if (!File.Exists(path))
        {
            Console.WriteLine("Missing " + path);
            return;
        }
        int errors = 0;
        int warnings = 0;
        var swType = Type.GetTypeFromProgID("SldWorks.Application");
        var swApp = (SldWorks)System.Runtime.InteropServices.Marshal.GetActiveObject("SldWorks.Application");
        var part = (ModelDoc2)swApp.OpenDoc6(path, (int)swDocumentTypes_e.swDocPART, (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", ref errors, ref warnings);
        if (part == null)
        {
            Console.WriteLine("OPEN FAILED " + file + " errors=" + errors + " warnings=" + warnings);
            return;
        }
        var comp = asm.AddComponent5(path, (int)swAddComponentConfigOptions_e.swAddComponentConfigOptions_CurrentSelectedConfig, "", false, "", x * MM, y * MM, z * MM);
        Console.WriteLine((comp == null ? "FAILED " : "Added  ") + file);
    }
}
