using System;
using System.IO;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Stage 1: build clean native 3D parts for the kangaroo bio-inspired jumper.
// Convention: sketch on Front Plane (XY = side-mechanism plane),
// extrude MIDPLANE along Z so every part is symmetric about z=0.
// Link parts: local origin at pivot-1, body runs +X to pivot-2 (capsule).
// Cylinder/box parts: local origin at the part center.
class CreateBioInspiredParts
{
    const double MM = 0.001;
    static readonly string OutDir =
        @"C:\Users\Gzj\Desktop\kangaroo_robot_redesign\solidworks_basic_model\kangaroo_overleap_bioinspired_v1\parts";
    static readonly string TemplatePart =
        @"C:\ProgramData\SOLIDWORKS\SOLIDWORKS 2026\templates\gb_part.prtdot";
    static SldWorks swApp;

    [STAThread]
    static void Main()
    {
        try { Run(); }
        catch (Exception ex) { Console.WriteLine(ex.GetType().FullName + ": " + ex.Message); }
    }

    static void Run()
    {
        Directory.CreateDirectory(OutDir);
        swApp = (SldWorks)Activator.CreateInstance(Type.GetTypeFromProgID("SldWorks.Application"));
        swApp.Visible = true;

        BodyPlate();
        Link("L1_Crank_40.SLDPRT", 40, 16, 8);
        Link("L2_Coupler_120.SLDPRT", 120, 16, 8);
        Link("L3_Thigh_100.SLDPRT", 100, 20, 8);
        Link("L4_Shank_140.SLDPRT", 140, 20, 8);
        Link("L5_Rocker_180.SLDPRT", 180, 20, 8);
        Foot("Foot_80.SLDPRT", 80, 30, 26);
        Link("TailRod_210.SLDPRT", 210, 14, 10);
        Link("ElasticTendon_52.SLDPRT", 52, 7, 7);
        Cyl("TailMass.SLDPRT", 16, 30);
        Cyl("Drum.SLDPRT", 18, 24);
        Cyl("M3_Axle_60.SLDPRT", 1.5, 60);
        Cyl("Standoff_44.SLDPRT", 6, 44);
        Box("Motor.SLDPRT", 70, 36, 36);
        Box("Servo.SLDPRT", 42, 26, 38);
        Box("Latch.SLDPRT", 44, 12, 16);

        Console.WriteLine("PARTS DONE");
    }

    static ModelDoc2 NewPart(string title)
    {
        var m = (ModelDoc2)swApp.NewDocument(TemplatePart, 0, 0, 0);
        m.SetTitle2(title);
        m.Extension.SelectByID2("Front Plane", "PLANE", 0, 0, 0, false, 0, null, 0);
        var sm = (SelectionMgr)m.SelectionManager;
        if (sm.GetSelectedObjectCount2(-1) == 0)
            m.Extension.SelectByID2("Plane1", "PLANE", 0, 0, 0, false, 0, null, 0);
        return m;
    }

    static void MidExtrude(ModelDoc2 m, double depthMm)
    {
        // MidPlane end condition (4): symmetric about the sketch plane.
        m.FeatureManager.FeatureExtrusion2(true, false, false,
            (int)swEndConditions_e.swEndCondMidPlane, 0,
            depthMm * MM, 0, false, false, false, false, 0, 0,
            false, false, false, false, true, true, true, 0, 0, false);
    }

    static void Save(ModelDoc2 m, string file)
    {
        m.ForceRebuild3(false);
        int err = m.SaveAs3(Path.Combine(OutDir, file), 0, 2);
        Console.WriteLine(file + " saveErr=" + err);
        swApp.CloseDoc(m.GetTitle());
    }

    static void BodyPlate()
    {
        var m = NewPart("BodyPlate_3D");
        // Hexagonal side frame covering all fixed pivots + module mounts.
        double[,] pts = { { -115, -25 }, { 150, -25 }, { 150, 70 },
                          { 135, 105 }, { -95, 105 }, { -115, 70 } };
        m.SketchManager.InsertSketch(true);
        int n = pts.GetLength(0);
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            m.SketchManager.CreateLine(pts[i, 0] * MM, pts[i, 1] * MM, 0,
                                       pts[j, 0] * MM, pts[j, 1] * MM, 0);
        }
        // Fixed pivots + module mount holes (3.5 mm).
        double[,] holes = { { 0, 0 }, { -80, 60 }, { 120, 60 }, { -70, -5 },
                            { -55, 50 }, { -55, 80 }, { 120, 88 }, { -80, 78 } };
        for (int i = 0; i < holes.GetLength(0); i++)
            m.SketchManager.CreateCircleByRadius(holes[i, 0] * MM, holes[i, 1] * MM, 0, 3.5 * MM);
        m.SketchManager.InsertSketch(true);
        MidExtrude(m, 4);
        Save(m, "BodyPlate_3D.SLDPRT");
    }

    static void Link(string file, double len, double w, double th)
    {
        var m = NewPart(file.Replace(".SLDPRT", ""));
        double r = w / 2.0;
        m.SketchManager.InsertSketch(true);
        m.SketchManager.CreateCircleByRadius(0, 0, 0, r * MM);
        m.SketchManager.CreateCircleByRadius(len * MM, 0, 0, r * MM);
        m.SketchManager.CreateLine(0, r * MM, 0, len * MM, r * MM, 0);
        m.SketchManager.CreateLine(0, -r * MM, 0, len * MM, -r * MM, 0);
        // pivot bores
        m.SketchManager.CreateCircleByRadius(0, 0, 0, 1.6 * MM);
        m.SketchManager.CreateCircleByRadius(len * MM, 0, 0, 1.6 * MM);
        // lightening holes on long links
        if (len > 80)
        {
            m.SketchManager.CreateCircleByRadius(len * 0.35 * MM, 0, 0, 4 * MM);
            m.SketchManager.CreateCircleByRadius(len * 0.65 * MM, 0, 0, 4 * MM);
        }
        m.SketchManager.InsertSketch(true);
        MidExtrude(m, th);
        Save(m, file);
    }

    static void Foot(string file, double len, double w, double th)
    {
        var m = NewPart(file.Replace(".SLDPRT", ""));
        double r = w / 2.0;
        m.SketchManager.InsertSketch(true);
        m.SketchManager.CreateCircleByRadius(0, 0, 0, 8 * MM);          // ankle boss
        m.SketchManager.CreateCircleByRadius(len * MM, 0, 0, r * MM);   // rounded toe pad
        m.SketchManager.CreateLine(0, 8 * MM, 0, len * MM, r * MM, 0);
        m.SketchManager.CreateLine(0, -8 * MM, 0, len * MM, -r * MM, 0);
        m.SketchManager.CreateCircleByRadius(0, 0, 0, 1.6 * MM);
        m.SketchManager.InsertSketch(true);
        MidExtrude(m, th);
        Save(m, file);
    }

    static void Cyl(string file, double rad, double depth)
    {
        var m = NewPart(file.Replace(".SLDPRT", ""));
        m.SketchManager.InsertSketch(true);
        m.SketchManager.CreateCircleByRadius(0, 0, 0, rad * MM);
        m.SketchManager.InsertSketch(true);
        MidExtrude(m, depth);
        Save(m, file);
    }

    static void Box(string file, double lx, double ly, double lz)
    {
        var m = NewPart(file.Replace(".SLDPRT", ""));
        m.SketchManager.InsertSketch(true);
        m.SketchManager.CreateCenterRectangle(0, 0, 0, lx / 2 * MM, ly / 2 * MM, 0);
        m.SketchManager.InsertSketch(true);
        MidExtrude(m, lz);
        Save(m, file);
    }
}
