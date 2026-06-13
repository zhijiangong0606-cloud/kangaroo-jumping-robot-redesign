using System;
using System.IO;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

class CreateTrue3DFixedAssembly
{
    const double MM = 0.001;
    static readonly string Root = @"C:\Users\Gzj\Desktop\kangaroo_robot_redesign\solidworks_basic_model";
    static readonly string PartDir = Path.Combine(Root, "true3d_parts");
    static readonly string TemplateAsm = @"C:\ProgramData\SOLIDWORKS\SOLIDWORKS 2026\templates\gb_assembly.asmdot";

    struct P
    {
        public double X, Y;
        public P(double x, double y) { X = x; Y = y; }
    }

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
        var swType = Type.GetTypeFromProgID("SldWorks.Application");
        var swApp = (SldWorks)Activator.CreateInstance(swType);
        swApp.Visible = true;

        var model = (ModelDoc2)swApp.NewDocument(TemplateAsm, 0, 0, 0);
        var asm = (AssemblyDoc)model;

        P H0 = new P(0, 0), H1 = new P(-80, 60), H2 = new P(120, 60);
        P A = new P(-42.4, 46.3), B = new P(45, 79.9), F = new P(-58.1, 231);
        P Foot = new P(25, 246), T = new P(200, -20), Tail = new P(400, -60);

        // Correct coordinate convention:
        // SolidWorks XY plane = side mechanism plane.
        // SolidWorks Z axis = thickness / left-right spacing.
        Add(asm, swApp, "BodyPlate_3D.SLDPRT", 0, 0, -18, 0);
        Add(asm, swApp, "BodyPlate_3D.SLDPRT", 0, 0, 18, 0);

        foreach (double z in new[] { -28.0, 28.0 })
        {
            AddLink(asm, swApp, "L1_Crank_40_3D.SLDPRT", H1, A, z);
            AddLink(asm, swApp, "L2_Coupler_120_3D.SLDPRT", A, B, z);
            AddLink(asm, swApp, "L3_Thigh_100_3D.SLDPRT", H0, B, z);
            AddLink(asm, swApp, "L4_Shank_140_3D.SLDPRT", B, F, z);
            AddLink(asm, swApp, "L5_Rocker_180_3D.SLDPRT", H2, F, z);
            AddLink(asm, swApp, "Foot_90_3D.SLDPRT", F, Foot, z);
        }

        AddLink(asm, swApp, "TailRod_220_3D.SLDPRT", T, Tail, 0);
        Add(asm, swApp, "TailMass_3D.SLDPRT", Tail.X, Tail.Y, 0, 0);
        AddLink(asm, swApp, "ElasticTendon_3D.SLDPRT", new P(-65, 78), B, -36);

        Add(asm, swApp, "Motor_3D.SLDPRT", -25, 92, -8, 0);
        Add(asm, swApp, "Drum_3D.SLDPRT", -55, 80, -36, 0);
        Add(asm, swApp, "Servo_3D.SLDPRT", 135, 95, -8, 0);
        Add(asm, swApp, "Latch_3D.SLDPRT", 80, 72, -36, 0);

        foreach (var p in new[] { H0, H1, H2, A, B, F, T })
            Add(asm, swApp, "M3_Axle_52_3D.SLDPRT", p.X, p.Y, 0, 0);

        foreach (var p in new[] { H0, H1, H2, A, B, F })
            Add(asm, swApp, "Spacer_24_3D.SLDPRT", p.X, p.Y, 0, 0);

        model.ShowNamedView2("*Isometric", 7);
        model.ViewZoomtofit2();
        model.ForceRebuild3(false);
        int err = model.SaveAs3(Path.Combine(Root, "KangarooRobot_TRUE_3D_FIXED_assembly.SLDASM"), 0, 2);
        Console.WriteLine("assembly save err=" + err);
    }

    static void AddLink(AssemblyDoc asm, SldWorks swApp, string file, P a, P b, double z)
    {
        Add(asm, swApp, file, a.X, a.Y, z, Math.Atan2(b.Y - a.Y, b.X - a.X));
    }

    static void Add(AssemblyDoc asm, SldWorks swApp, string file, double x, double y, double z, double rz)
    {
        string path = Path.Combine(PartDir, file);
        int errors = 0, warnings = 0;
        swApp.OpenDoc6(path, (int)swDocumentTypes_e.swDocPART, (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", ref errors, ref warnings);
        var comp = asm.AddComponent5(path, (int)swAddComponentConfigOptions_e.swAddComponentConfigOptions_CurrentSelectedConfig, "", false, "", x * MM, y * MM, z * MM);
        if (comp != null) comp.Transform2 = Transform(swApp, x * MM, y * MM, z * MM, rz);
        Console.WriteLine((comp == null ? "FAILED " : "Added ") + file);
    }

    static MathTransform Transform(SldWorks swApp, double x, double y, double z, double rz)
    {
        var mu = (MathUtility)swApp.GetMathUtility();
        double c = Math.Cos(rz), s = Math.Sin(rz);
        double[] data = { c, -s, 0, s, c, 0, 0, 0, 1, x, y, z, 1, 0, 0, 0 };
        return (MathTransform)mu.CreateTransform(data);
    }
}
