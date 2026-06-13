using System;
using System.IO;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

class CreateTrue3DAssembly
{
    const double MM = 0.001;
    static readonly string Root = @"C:\Users\Gzj\Desktop\kangaroo_robot_redesign\solidworks_basic_model";
    static readonly string PartDir = Path.Combine(Root, "true3d_parts");
    static readonly string TemplatePart = @"C:\ProgramData\SOLIDWORKS\SOLIDWORKS 2026\templates\gb_part.prtdot";
    static readonly string TemplateAsm = @"C:\ProgramData\SOLIDWORKS\SOLIDWORKS 2026\templates\gb_assembly.asmdot";

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
            Console.WriteLine(ex.GetType().FullName);
            Console.WriteLine(ex.Message);
        }
    }

    static void Run()
    {
        Directory.CreateDirectory(PartDir);
        var swType = Type.GetTypeFromProgID("SldWorks.Application");
        var swApp = (SldWorks)Activator.CreateInstance(swType);
        swApp.Visible = true;

        CreateBodyPlate(swApp);
        CreateLink(swApp, "L1_Crank_40_3D.SLDPRT", 40, 16, 8);
        CreateLink(swApp, "L2_Coupler_120_3D.SLDPRT", 120, 16, 8);
        CreateLink(swApp, "L3_Thigh_100_3D.SLDPRT", 100, 20, 9);
        CreateLink(swApp, "L4_Shank_140_3D.SLDPRT", 140, 20, 9);
        CreateLink(swApp, "L5_Rocker_180_3D.SLDPRT", 180, 20, 9);
        CreateLink(swApp, "Foot_90_3D.SLDPRT", 90, 28, 12);
        CreateLink(swApp, "TailRod_220_3D.SLDPRT", 220, 14, 10);
        CreateCylinder(swApp, "M3_Axle_52_3D.SLDPRT", 3.0, 52);
        CreateCylinder(swApp, "Spacer_24_3D.SLDPRT", 5.0, 24);
        CreateCylinder(swApp, "Drum_3D.SLDPRT", 20.0, 28);
        CreateCylinder(swApp, "TailMass_3D.SLDPRT", 18.0, 24);
        CreateBox(swApp, "Motor_3D.SLDPRT", 70, 34, 36);
        CreateBox(swApp, "Servo_3D.SLDPRT", 42, 24, 38);
        CreateBox(swApp, "Latch_3D.SLDPRT", 60, 12, 18);
        CreateLink(swApp, "ElasticTendon_3D.SLDPRT", 125, 7, 7);

        CreateAssembly(swApp);
    }

    static void SelectFrontPlane(ModelDoc2 model)
    {
        model.Extension.SelectByID2("Front Plane", "PLANE", 0, 0, 0, false, 0, null, 0);
        var selMgr = (SelectionMgr)model.SelectionManager;
        if (selMgr.GetSelectedObjectCount2(-1) == 0)
            model.Extension.SelectByID2("Plane1", "PLANE", 0, 0, 0, false, 0, null, 0);
    }

    static ModelDoc2 NewPart(SldWorks swApp, string title)
    {
        var model = (ModelDoc2)swApp.NewDocument(TemplatePart, 0, 0, 0);
        model.SetTitle2(title);
        return model;
    }

    static void SaveClose(SldWorks swApp, ModelDoc2 model, string file)
    {
        model.ForceRebuild3(false);
        int err = model.SaveAs3(Path.Combine(PartDir, file), 0, 2);
        Console.WriteLine(file + " save err=" + err);
        swApp.CloseDoc(model.GetTitle());
    }

    static void Extrude(ModelDoc2 model, double depthMm)
    {
        model.FeatureManager.FeatureExtrusion2(true, false, false, 0, 0, depthMm * MM, 0, false, false, false, false, 0, 0, false, false, false, false, true, true, true, 0, 0, false);
    }

    static void CreateBodyPlate(SldWorks swApp)
    {
        var model = NewPart(swApp, "BodyPlate_3D");
        SelectFrontPlane(model);
        model.SketchManager.InsertSketch(true);
        double[,] pts = { { -175, 45 }, { -125, -55 }, { 255, -55 }, { 345, 35 }, { 275, 130 }, { -115, 140 } };
        for (int i = 0; i < pts.GetLength(0); i++)
        {
            int j = (i + 1) % pts.GetLength(0);
            model.SketchManager.CreateLine(pts[i, 0] * MM, pts[i, 1] * MM, 0, pts[j, 0] * MM, pts[j, 1] * MM, 0);
        }
        foreach (var p in new[] { new P(0, 0), new P(-80, 60), new P(120, 60), new P(200, -20), new P(-40, 95), new P(135, 95) })
            model.SketchManager.CreateCircleByRadius(p.X * MM, p.Z * MM, 0, 3.5 * MM);
        model.SketchManager.InsertSketch(true);
        Extrude(model, 5);
        SaveClose(swApp, model, "BodyPlate_3D.SLDPRT");
    }

    static void CreateLink(SldWorks swApp, string file, double length, double width, double thick)
    {
        var model = NewPart(swApp, file.Replace(".SLDPRT", ""));
        SelectFrontPlane(model);
        double r = width / 2.0;
        model.SketchManager.InsertSketch(true);
        model.SketchManager.CreateCircleByRadius(0, 0, 0, r * MM);
        model.SketchManager.CreateCircleByRadius(length * MM, 0, 0, r * MM);
        model.SketchManager.CreateLine(0, r * MM, 0, length * MM, r * MM, 0);
        model.SketchManager.CreateLine(0, -r * MM, 0, length * MM, -r * MM, 0);
        model.SketchManager.CreateCircleByRadius(0, 0, 0, 2.0 * MM);
        model.SketchManager.CreateCircleByRadius(length * MM, 0, 0, 2.0 * MM);
        if (length > 80)
        {
            model.SketchManager.CreateCircleByRadius(length * 0.35 * MM, 0, 0, 4.0 * MM);
            model.SketchManager.CreateCircleByRadius(length * 0.65 * MM, 0, 0, 4.0 * MM);
        }
        model.SketchManager.InsertSketch(true);
        Extrude(model, thick);
        SaveClose(swApp, model, file);
    }

    static void CreateCylinder(SldWorks swApp, string file, double radius, double depth)
    {
        var model = NewPart(swApp, file.Replace(".SLDPRT", ""));
        SelectFrontPlane(model);
        model.SketchManager.InsertSketch(true);
        model.SketchManager.CreateCircleByRadius(0, 0, 0, radius * MM);
        model.SketchManager.InsertSketch(true);
        Extrude(model, depth);
        SaveClose(swApp, model, file);
    }

    static void CreateBox(SldWorks swApp, string file, double lx, double ly, double lz)
    {
        var model = NewPart(swApp, file.Replace(".SLDPRT", ""));
        SelectFrontPlane(model);
        model.SketchManager.InsertSketch(true);
        model.SketchManager.CreateCenterRectangle(0, 0, 0, lx / 2 * MM, lz / 2 * MM, 0);
        model.SketchManager.InsertSketch(true);
        Extrude(model, ly);
        SaveClose(swApp, model, file);
    }

    static void CreateAssembly(SldWorks swApp)
    {
        var model = (ModelDoc2)swApp.NewDocument(TemplateAsm, 0, 0, 0);
        var asm = (AssemblyDoc)model;

        P H0 = new P(0, 0), H1 = new P(-80, 60), H2 = new P(120, 60);
        P A = new P(-42.4, 46.3), B = new P(45, 79.9), F = new P(-58.1, 231);
        P Foot = new P(25, 246), T = new P(200, -20), Tail = new P(400, -60);

        Add(asm, swApp, "BodyPlate_3D.SLDPRT", 0, -18, 0, 0);
        Add(asm, swApp, "BodyPlate_3D.SLDPRT", 0, 18, 0, 0);

        foreach (double y in new[] { -26.0, 26.0 })
        {
            AddLink(asm, swApp, "L1_Crank_40_3D.SLDPRT", H1, A, y);
            AddLink(asm, swApp, "L2_Coupler_120_3D.SLDPRT", A, B, y);
            AddLink(asm, swApp, "L3_Thigh_100_3D.SLDPRT", H0, B, y);
            AddLink(asm, swApp, "L4_Shank_140_3D.SLDPRT", B, F, y);
            AddLink(asm, swApp, "L5_Rocker_180_3D.SLDPRT", H2, F, y);
            AddLink(asm, swApp, "Foot_90_3D.SLDPRT", F, Foot, y);
        }
        AddLink(asm, swApp, "TailRod_220_3D.SLDPRT", T, Tail, 0);
        Add(asm, swApp, "TailMass_3D.SLDPRT", Tail.X, 0, Tail.Z, Math.PI / 2);
        AddLink(asm, swApp, "ElasticTendon_3D.SLDPRT", new P(-65, 78), B, -34);
        Add(asm, swApp, "Motor_3D.SLDPRT", -25, -8, 92, 0);
        Add(asm, swApp, "Drum_3D.SLDPRT", -55, -28, 80, Math.PI / 2);
        Add(asm, swApp, "Servo_3D.SLDPRT", 135, -8, 95, 0);
        Add(asm, swApp, "Latch_3D.SLDPRT", 80, -32, 72, 0);
        foreach (var p in new[] { H0, H1, H2, A, B, F, T })
            Add(asm, swApp, "M3_Axle_52_3D.SLDPRT", p.X, 0, p.Z, Math.PI / 2);
        foreach (var p in new[] { H0, H1, H2, A, B, F })
            Add(asm, swApp, "Spacer_24_3D.SLDPRT", p.X, 0, p.Z + 12, Math.PI / 2);

        model.ShowNamedView2("*Isometric", 7);
        model.ViewZoomtofit2();
        model.ForceRebuild3(false);
        int err = model.SaveAs3(Path.Combine(Root, "KangarooRobot_TRUE_3D_assembly.SLDASM"), 0, 2);
        Console.WriteLine("assembly save err=" + err);
    }

    static void AddLink(AssemblyDoc asm, SldWorks swApp, string file, P a, P b, double y)
    {
        Add(asm, swApp, file, a.X, y, a.Z, Math.Atan2(b.Z - a.Z, b.X - a.X));
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
