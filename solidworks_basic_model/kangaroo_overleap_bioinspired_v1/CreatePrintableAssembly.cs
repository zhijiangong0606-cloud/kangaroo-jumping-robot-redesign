using System;
using System.IO;
using System.Collections.Generic;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Stage 2 (v4 - REAL, OPERABLE ASSEMBLY)
// Body = cage of two side plates (z = +-34) tied by 4 corner standoffs.
// Each hind leg = closed chain of holed links, each link on its own Z-layer so the
// three links that meet at joints B and F never collide and can actually rotate.
// A single full-width pin runs through both plates and BOTH legs at each pivot,
// physically connecting every leg link to the body and synchronizing left/right.
// After placing parts at the solved 1-DOF stance, concentric mates are added at the
// pin axes so the model is a true, constrained, hand-operable mechanism.
class CreatePrintableAssembly
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

    // Solved crouch stance (theta = -74 deg). Foot under-and-ahead, knees bent.
    static readonly P H0 = new P(0, 0), H1 = new P(-80, 60), H2 = new P(120, 60);
    static readonly P A  = new P(-68.97, 21.55);
    static readonly P B  = new P(-35.20, -93.60);
    static readonly P F  = new P(102.45, -119.14);
    static readonly P Foot = new P(181.11, -133.74);
    static readonly P T  = new P(-95, 2);
    static readonly P TailEnd = new P(-273.1, -109.3);

    // --- Z layers (mid-plane of each part, mm) ---
    const double PLATE_Z = 34;     // side plates at +-34 (4 mm thick)
    // right-leg link layers (left leg mirrored to negative):
    const double Z_L1 = 29, Z_L5 = 24, Z_L3 = 19, Z_L2 = 14, Z_L4 = 9, Z_FOOT = 4;
    const double Z_TENDON = 31;    // tendon sits just inside the right plate
    const double Z_TAIL = 0;       // tail rod on center plane

    static readonly List<string> log = new List<string>();

    [STAThread]
    static void Main()
    {
        try { Run(); }
        catch (Exception ex) { Console.WriteLine(ex.GetType().FullName + ": " + ex.Message); }
        foreach (var s in log) Console.WriteLine(s);
    }

    static void Run()
    {
        swApp = (SldWorks)Activator.CreateInstance(Type.GetTypeFromProgID("SldWorks.Application"));
        swApp.Visible = true;
        model = (ModelDoc2)swApp.NewDocument(TemplateAsm, 0, 0, 0);
        asm = (AssemblyDoc)model;

        // ---- Body cage: two side plates + 4 corner standoffs ----
        Place("BodyPlate_Side.SLDPRT",  0, 0,  PLATE_Z, 0);
        Place("BodyPlate_Side.SLDPRT",  0, 0, -PLATE_Z, 0);
        // 4 corner standoffs span the gap (centered on z=0, length 64).
        // Top-left corner moved out to (-130,95): an interference sweep showed the
        // original (-118,83) was hit by L1/L2 at theta 127-160 deg; (-130,95) clears
        // the full crank rotation with >=5 mm margin.
        double[][] corners = {
            new[]{-118.0,-28.0}, new[]{138.0,-28.0}, new[]{-130.0,95.0}, new[]{138.0,95.0}
        };
        foreach (var c in corners) Place("Standoff_64.SLDPRT", c[0], c[1], 0, 0, true);

        // ---- Two hind legs (right z>0, left z<0 mirrored) ----
        foreach (int side in new[] { +1, -1 })
        {
            LinkZ("L1_Crank_40.SLDPRT",   H1, A,    side * Z_L1);
            LinkZ("L2_Coupler_120.SLDPRT",A,  B,    side * Z_L2);
            LinkZ("L3_Thigh_100.SLDPRT",  H0, B,    side * Z_L3);
            LinkZ("L4_Shank_140.SLDPRT",  B,  F,    side * Z_L4);
            LinkZ("L5_Rocker_180.SLDPRT", H2, F,    side * Z_L5);
            LinkZ("Foot_80.SLDPRT",       F,  Foot, side * Z_FOOT);
        }

        // ---- Tail (center plane) + tail mass ----
        LinkZ("TailRod_210.SLDPRT", T, TailEnd, Z_TAIL);
        Place("TailMass.SLDPRT", TailEnd.X, TailEnd.Y, 0, 0);

        // ---- Drive / energy modules mounted on the cage ----
        Place("Motor.SLDPRT", 30, 95, 0, 0);     // motor across the top deck
        Place("Servo.SLDPRT", 110, 92, 0, 0);    // release servo near crank
        Place("Drum.SLDPRT", -70, 60, 0, 0, true);   // winding drum coaxial w/ H1 region
        Place("Latch.SLDPRT", -55, 82, 0, 0);
        LinkZ("ElasticTendon_52.SLDPRT", new P(-70, 60), A, Z_TENDON);  // drum -> crank A

        // ---- Synchronizing pins through both plates + both legs ----
        // Full-width (80 mm) pin, axis along Z, centered on z=0 at each shared pivot.
        foreach (var p in new[] { H0, H1, H2, A, B, F })
            PinZ("M3_Axle_80.SLDPRT", p.X, p.Y);
        // Tail pin (single, 40 mm) at T.
        PinZ("M3_Axle_40.SLDPRT", T.X, T.Y);

        model.ForceRebuild3(false);
        model.ViewZoomtofit2();

        // ---- Add concentric mates: pin <-> each link hole at every pivot ----
        AddPinMates();

        model.ForceRebuild3(false);
        int err = model.SaveAs3(Path.Combine(Root, "Kangaroo_Overleap_BioInspired_Assembly.SLDASM"), 0, 2);
        log.Add("ASSEMBLY saveErr=" + err);
    }

    static void LinkZ(string file, P a, P b, double z)
    {
        Place(file, a.X, a.Y, z, Math.Atan2(b.Y - a.Y, b.X - a.X));
    }

    // Pin oriented along +Z: rotate the part (built along its own Z extrude=local Z;
    // a Pin extruded MidPlane on Z is already axial along Z) -> just translate.
    static void PinZ(string file, double x, double y)
    {
        Place(file, x, y, 0, 0);
    }

    static readonly List<string> placed = new List<string>();

    static void Place(string file, double x, double y, double z, double rz, bool axialZ = false)
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
            placed.Add(comp.Name2);
        }
        log.Add((comp == null ? "FAILED " : "added ") + file + (comp != null ? " ["+comp.Name2+"]" : ""));
    }

    // Concentric mates between each full-width pin and every hole it passes through.
    // Faces are coordinate-picked: a point on a cylinder wall = (cx+r, cy, z).
    // The Z value disambiguates which link's hole at a shared pivot is selected.
    const double HOLE_MOVE_R = 1.70;  // moving link hole radius
    const double HOLE_BODY_R = 1.60;  // plate hole radius
    const double PIN_R       = 1.50;  // pin radius

    static void AddPinMates()
    {
        // joint -> list of link Z-layers (per side) that have a hole there.
        var moving = new Dictionary<P, double[]>();
        moving[H1] = new[]{ Z_L1 };
        moving[H2] = new[]{ Z_L5 };
        moving[H0] = new[]{ Z_L3 };
        moving[A]  = new[]{ Z_L1, Z_L2 };
        moving[B]  = new[]{ Z_L2, Z_L3, Z_L4 };
        moving[F]  = new[]{ Z_L4, Z_L5, Z_FOOT };

        int ok = 0, fail = 0;
        P[] pivots = { H0, H1, H2, A, B, F };
        bool[] isFixed = { true, true, true, false, false, false };
        for (int i = 0; i < pivots.Length; i++)
        {
            P p = pivots[i];
            foreach (int side in new[]{ +1, -1 })
                foreach (double zl in moving[p])
                {
                    bool added = Concentric(
                        p.X + PIN_R, p.Y, 0,                 // point on pin wall (z=0 inside cage)
                        p.X + HOLE_MOVE_R, p.Y, side * zl);  // point on link hole wall
                    if (added) ok++; else fail++;
                }
            if (isFixed[i])
                foreach (int side in new[]{ +1, -1 })
                {
                    bool added = Concentric(
                        p.X + PIN_R, p.Y, side * 30,                 // pin wall near plate
                        p.X + HOLE_BODY_R, p.Y, side * PLATE_Z);     // plate hole wall
                    if (added) ok++; else fail++;
                }
        }
        log.Add("MATES concentric ok=" + ok + " failed=" + fail);
    }

    // Select two cylindrical faces by coordinate and add a concentric mate.
    static bool Concentric(double x1,double y1,double z1, double x2,double y2,double z2)
    {
        try
        {
            model.ClearSelection2(true);
            var ext = model.Extension;
            bool s1 = ext.SelectByID2("", "FACE", x1*MM, y1*MM, z1*MM, false, 1, null, 0);
            bool s2 = ext.SelectByID2("", "FACE", x2*MM, y2*MM, z2*MM, true,  1, null, 0);
            if (!s1 || !s2) { model.ClearSelection2(true); return false; }
            int err = 0;
            var mate = asm.AddMate5(
                (int)swMateType_e.swMateCONCENTRIC,
                (int)swMateAlign_e.swMateAlignCLOSEST,
                false, 0,0,0, 0,0, 0,0,0, false, false, 0, out err);
            model.ClearSelection2(true);
            return mate != null && err == 0;
        }
        catch { model.ClearSelection2(true); return false; }
    }
}

