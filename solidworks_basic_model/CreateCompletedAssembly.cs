using System;
using System.IO;
using System.Runtime.InteropServices;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

class CreateCompletedAssembly
{
    const double MM = 0.001;

    struct P
    {
        public double X, Z;
        public P(double x, double z) { X = x; Z = z; }
    }

    [STAThread]
    static void Main()
    {
        try { Run(); }
        catch (Exception ex)
        {
            Console.WriteLine("EXCEPTION TYPE: " + ex.GetType().FullName);
            Console.WriteLine("MESSAGE: " + ex.Message);
        }
    }

    static void Run()
    {
        string folder = @"C:\Users\Gzj\Desktop\kangaroo_robot_redesign\solidworks_basic_model";
        string partFolder = Path.Combine(folder, "native_parts");
        string asmTemplate = @"C:\ProgramData\SOLIDWORKS\SOLIDWORKS 2026\templates\gb_assembly.asmdot";
        string outAsm = Path.Combine(folder, "KangarooRobot_completed_engineering_layout.SLDASM");

        var swType = Type.GetTypeFromProgID("SldWorks.Application");
        var swApp = (SldWorks)Activator.CreateInstance(swType);
        swApp.Visible = true;

        var model = (ModelDoc2)swApp.NewDocument(asmTemplate, 0, 0, 0);
        if (model == null) throw new Exception("Failed to create assembly document.");
        var asm = (AssemblyDoc)model;
        swApp.ActivateDoc3(model.GetTitle(), false, (int)swRebuildOnActivation_e.swDontRebuildActiveDoc, 0);

        P H0 = new P(0, 0);
        P H1 = new P(-80, 60);
        P H2 = new P(120, 60);
        P A = new P(-42.4, 46.3);
        P B = new P(45, 79.9);
        P F = new P(-58.1, 231);
        P Foot = new P(25, 246);
        P T = new P(200, -20);
        P Tail = new P(400, -60);

        Add(asm, swApp, partFolder, "BodySidePlate_verified.SLDPRT", 0, -8, 0, 0);
        Add(asm, swApp, partFolder, "BodySidePlate_verified.SLDPRT", 0, 8, 0, 0);

        foreach (double y in new[] { -18.0, 18.0 })
        {
            AddLink(asm, swApp, partFolder, "L1_Crank_40mm.SLDPRT", H1, A, y);
            AddLink(asm, swApp, partFolder, "L2_Coupler_120mm.SLDPRT", A, B, y);
            AddLink(asm, swApp, partFolder, "L3_ThighRocker_100mm.SLDPRT", H0, B, y);
            AddLink(asm, swApp, partFolder, "L4_Shank_140mm.SLDPRT", B, F, y);
            AddLink(asm, swApp, partFolder, "L5_RearRocker_180mm.SLDPRT", H2, F, y);
            AddLink(asm, swApp, partFolder, "FootPad_85mm.SLDPRT", F, Foot, y);
        }

        AddLink(asm, swApp, partFolder, "TailRod_210mm.SLDPRT", T, Tail, 0);
        Add(asm, swApp, partFolder, "TailMass_30g_placeholder.SLDPRT", Tail.X, 0, Tail.Z, 0);
        AddLink(asm, swApp, partFolder, "ElasticTendon_120mm.SLDPRT", new P(-64, 78), B, -22);

        Add(asm, swApp, partFolder, "WindingDrum_r18_w24.SLDPRT", -55, 0, 80, Math.PI / 2);
        Add(asm, swApp, partFolder, "GearMotor_Placeholder_60x32x26.SLDPRT", -25, 0, 90, 0);
        Add(asm, swApp, partFolder, "Servo_Placeholder_38x35x22.SLDPRT", 135, 0, 95, 0);
        Add(asm, swApp, partFolder, "Latch_Placeholder_50x12x8.SLDPRT", 80, 0, 72, 0);

        foreach (var joint in new[] { H0, H1, H2, A, B, F, T })
        {
            Add(asm, swApp, partFolder, "M3_Axle_40mm.SLDPRT", joint.X, 0, joint.Z, Math.PI / 2);
        }
        foreach (var s in new[] { H0, H1, H2, A, B, F })
        {
            Add(asm, swApp, partFolder, "Spacer_16mm.SLDPRT", s.X, 0, s.Z + 8, Math.PI / 2);
        }

        model.ForceRebuild3(false);
        int err = model.SaveAs3(outAsm, (int)swSaveAsVersion_e.swSaveAsCurrentVersion, (int)swSaveAsOptions_e.swSaveAsOptions_Silent);
        Console.WriteLine("SaveAs3 errorCode=" + err);
        Console.WriteLine(outAsm);
    }

    static void AddLink(AssemblyDoc asm, SldWorks swApp, string partFolder, string file, P start, P end, double y)
    {
        double rz = Math.Atan2(end.Z - start.Z, end.X - start.X);
        Add(asm, swApp, partFolder, file, start.X, y, start.Z, rz);
    }

    static void Add(AssemblyDoc asm, SldWorks swApp, string partFolder, string file, double x, double y, double z, double rz)
    {
        string path = Path.Combine(partFolder, file);
        if (!File.Exists(path))
        {
            Console.WriteLine("Missing " + path);
            return;
        }
        int errors = 0, warnings = 0;
        var part = (ModelDoc2)swApp.OpenDoc6(path, (int)swDocumentTypes_e.swDocPART, (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", ref errors, ref warnings);
        if (part == null)
        {
            Console.WriteLine("OPEN FAILED " + file + " errors=" + errors);
            return;
        }
        var comp = asm.AddComponent5(path, (int)swAddComponentConfigOptions_e.swAddComponentConfigOptions_CurrentSelectedConfig, "", false, "", x * MM, y * MM, z * MM);
        if (comp != null)
        {
            comp.Transform2 = Transform(x * MM, y * MM, z * MM, rz);
            Console.WriteLine("Added " + file);
        }
        else
        {
            Console.WriteLine("FAILED " + file);
        }
    }

    static MathTransform Transform(double x, double y, double z, double rz)
    {
        var swApp = (SldWorks)Marshal.GetActiveObject("SldWorks.Application");
        var mu = (MathUtility)swApp.GetMathUtility();
        double c = Math.Cos(rz);
        double s = Math.Sin(rz);
        double[] data = new double[]
        {
            c, -s, 0,
            s, c, 0,
            0, 0, 1,
            x, y, z,
            1, 0, 0, 0
        };
        return (MathTransform)mu.CreateTransform(data);
    }
}
