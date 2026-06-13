using System;
using System.IO;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Stage 2: assemble the bio-inspired jumper from native parts at the
// geometry-solved stance pose, then capture real isometric screenshots.
class CreateBioInspiredAssembly
{
    const double MM = 0.001;
    static readonly string Root =
        @"C:\Users\Gzj\Desktop\kangaroo_robot_redesign\solidworks_basic_model\kangaroo_overleap_bioinspired_v1";
    static readonly string PartDir = Path.Combine(Root, "parts");
    static readonly string TemplateAsm =
        @"C:\ProgramData\SOLIDWORKS\SOLIDWORKS 2026\templates\gb_assembly.asmdot";
    static SldWorks swApp;
    static AssemblyDoc asm;
    static ModelDoc2 model;

    struct P { public double X, Y; public P(double x, double y) { X = x; Y = y; } }

    // Solved stance pose (mm) from solve_mechanism.py
    static readonly P H0 = new P(0, 0), H1 = new P(-80, 60), H2 = new P(120, 60);
    static readonly P A = new P(-100.6015, 25.7133);
    static readonly P B = new P(-53.3385, -84.5872);
    static readonly P F = new P(83.0532, -116.1673);
    static readonly P Foot = new P(160.9913, -134.2131);
    static readonly P T = new P(-70, -5);
    static readonly P TailEnd = new P(-251.8653, -110);
    static readonly P DrumC = new P(-55, 50);

    [STAThread]
    static void Main()
    {
        try { Run(); }
        catch (Exception ex) { Console.WriteLine(ex.GetType().FullName + ": " + ex.Message); }
    }

    static void Run()
    {
        swApp = (SldWorks)Activator.CreateInstance(Type.GetTypeFromProgID("SldWorks.Application"));
        swApp.Visible = true;
        model = (ModelDoc2)swApp.NewDocument(TemplateAsm, 0, 0, 0);
        asm = (AssemblyDoc)model;

        // --- Frame: two side plates ---
        Place("BodyPlate_3D.SLDPRT", 0, 0, 24, 0);
        Place("BodyPlate_3D.SLDPRT", 0, 0, -24, 0);

        // --- Hind-leg closed chain, mirrored on both sides (z = +-15) ---
        foreach (double z in new[] { 15.0, -15.0 })
        {
            Link("L1_Crank_40.SLDPRT", H1, A, z);
            Link("L2_Coupler_120.SLDPRT", A, B, z);
            Link("L3_Thigh_100.SLDPRT", H0, B, z);
            Link("L4_Shank_140.SLDPRT", B, F, z);
            Link("L5_Rocker_180.SLDPRT", H2, F, z);
            Link("Foot_80.SLDPRT", F, Foot, z);
        }

        // --- Tail (single, centered) ---
        Link("TailRod_210.SLDPRT", T, TailEnd, 0);
        Place("TailMass.SLDPRT", TailEnd.X, TailEnd.Y, 0, 0);

        // --- Energy / drive modules (centered in the frame gap) ---
        Place("Drum.SLDPRT", DrumC.X, DrumC.Y, 0, 0);
        Link("ElasticTendon_52.SLDPRT", DrumC, A, 0);
        Place("Motor.SLDPRT", -55, 82, 0, 0);
        Place("Servo.SLDPRT", 120, 86, 0, 0);
        Place("Latch.SLDPRT", -80, 74, 0, 0);

        // --- M3 axles through all pivots (tie left+right, span both plates) ---
        foreach (var p in new[] { H0, H1, H2, A, B, F, T })
            Place("M3_Axle_60.SLDPRT", p.X, p.Y, 0, 0);

        // --- Standoffs tying the two side plates (frame-only corners) ---
        foreach (var p in new[] { new P(-100, -18), new P(143, -18), new P(125, 98), new P(-85, 98) })
            Place("Standoff_44.SLDPRT", p.X, p.Y, 0, 0);

        model.ForceRebuild3(false);
        int err = model.SaveAs3(Path.Combine(Root, "Kangaroo_Overleap_BioInspired_Assembly.SLDASM"), 0, 2);
        Console.WriteLine("ASSEMBLY saveErr=" + err);

        // --- Capture real screenshots from multiple views ---
        Shot("*Isometric", "view_isometric.bmp");
        Shot("*Trimetric", "view_trimetric.bmp");
        Shot("*Right", "view_right.bmp");
        Shot("*Front", "view_front.bmp");
        Console.WriteLine("SHOTS DONE");
    }

    static void Shot(string view, string file)
    {
        model.ShowNamedView2(view, -1);
        model.ViewZoomtofit2();
        model.GraphicsRedraw2();
        bool ok = model.SaveBMP(Path.Combine(Root, file), 1600, 1200);
        Console.WriteLine(file + " ok=" + ok);
    }

    static void Link(string file, P a, P b, double z)
    {
        Place(file, a.X, a.Y, z, Math.Atan2(b.Y - a.Y, b.X - a.X));
    }

    static void Place(string file, double x, double y, double z, double rz)
    {
        string path = Path.Combine(PartDir, file);
        int e = 0, w = 0;
        swApp.OpenDoc6(path, (int)swDocumentTypes_e.swDocPART,
            (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", ref e, ref w);
        var comp = asm.AddComponent5(path,
            (int)swAddComponentConfigOptions_e.swAddComponentConfigOptions_CurrentSelectedConfig,
            "", false, "", x * MM, y * MM, z * MM);
        if (comp != null)
        {
            var mu = (MathUtility)swApp.GetMathUtility();
            double c = Math.Cos(rz), s = Math.Sin(rz);
            double[] d = { c, -s, 0, s, c, 0, 0, 0, 1, x * MM, y * MM, z * MM, 1, 0, 0, 0 };
            comp.Transform2 = (MathTransform)mu.CreateTransform(d);
        }
        Console.WriteLine((comp == null ? "FAILED " : "added ") + file);
    }
}
