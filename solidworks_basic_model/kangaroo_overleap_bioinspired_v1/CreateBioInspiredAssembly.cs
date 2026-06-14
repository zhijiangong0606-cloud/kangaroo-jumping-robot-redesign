using System;
using System.IO;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Stage 2 (v2): assemble chunky parts into a visibly-3D robot.
// Solid torso in the middle (z=0, 60mm deep); the two hind legs straddle it
// at z=+-42; long M3 axles run through the torso to tie both legs; modules
// sit on the torso. Same solved stance pose as solve_mechanism.py.
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

    // Solved leg-like crouch stance (theta=-74 deg, branch B0F0); foot under-and-
    // ahead of the body, knees bent, ready to extend. Verified vs link lengths.
    static readonly P H0 = new P(0, 0), H1 = new P(-80, 60), H2 = new P(120, 60);
    static readonly P A = new P(-68.97, 21.55);
    static readonly P B = new P(-35.20, -93.60);
    static readonly P F = new P(102.45, -119.14);
    static readonly P Foot = new P(181.11, -133.74);
    // Tail roots at the REAR-LOW of the torso and sweeps down-back (kangaroo-like).
    static readonly P T = new P(-95, 2);
    static readonly P TailEnd = new P(-273.1, -109.3);

    const double LEGZ = 42.0;     // leg plane offset (torso half-depth 30 + clearance)
    const double TORSO_TOP = 112; // torso top deck Y (BoxAt center 47 + half 65)
    const double TORSO_HALF_Z = 30;

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

        // --- Solid torso (center) ---
        Place("Torso_3D.SLDPRT", 0, 0, 0, 0);

        // --- Hind-leg closed chain, one set each side, straddling the torso ---
        foreach (double z in new[] { LEGZ, -LEGZ })
        {
            Link("L1_Crank_40.SLDPRT", H1, A, z);
            Link("L2_Coupler_120.SLDPRT", A, B, z);
            Link("L3_Thigh_100.SLDPRT", H0, B, z);
            Link("L4_Shank_140.SLDPRT", B, F, z);
            Link("L5_Rocker_180.SLDPRT", H2, F, z);
            Link("Foot_80.SLDPRT", F, Foot, z);
        }

        // --- Tail beam (roots into rear-low torso) + ball mass at its tip ---
        Link("TailRod_210.SLDPRT", T, TailEnd, 0);
        Place("TailMass.SLDPRT", TailEnd.X, TailEnd.Y, 0, 0);

        // --- Drive / energy modules SEATED on the torso deck (bottom embedded
        //     2 mm into the top face so they physically connect, not float) ---
        // Motor half-height 20 -> center at deck top (112) - 20 + 2 embed = 94 ... keep
        // it sitting with its base on the deck: centerY = TORSO_TOP - halfHeight + embed.
        Place("Motor.SLDPRT", -10, TORSO_TOP - 20 + 2, 0, 0);   // 74x40x40, halfY=20
        Place("Servo.SLDPRT", 95, TORSO_TOP - 15 + 2, 0, 0);    // 46x30x42, halfY=15
        Place("Drum.SLDPRT", -70, TORSO_TOP - 10, 0, 0);        // drum on deck near motor
        Place("Latch.SLDPRT", -80, TORSO_TOP - 8 + 2, 0, 0);    // latch by crank pivot
        // Elastic tendon: from drum down to the crank pivot A (drive coupling).
        Link("ElasticTendon_52.SLDPRT", new P(-70, TORSO_TOP - 10), A, LEGZ);

        // --- Long M3 axles through the torso tie both legs at every pivot ---
        foreach (var p in new[] { H0, H1, H2, A, B, F })
            Place("M3_Axle_120.SLDPRT", p.X, p.Y, 0, 0);

        model.ForceRebuild3(false);
        int err = model.SaveAs3(Path.Combine(Root, "Kangaroo_Overleap_BioInspired_Assembly.SLDASM"), 0, 2);
        Console.WriteLine("ASSEMBLY saveErr=" + err);
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
