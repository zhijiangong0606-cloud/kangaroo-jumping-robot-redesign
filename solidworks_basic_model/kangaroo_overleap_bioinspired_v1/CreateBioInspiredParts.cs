using System;
using System.IO;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Stage 1 (v3): build chunky solid parts using ONLY clean rectangle profiles.
// Root cause of the "flat / no thickness" problem: capsule sketches (overlapping
// circles + tangent lines + arcs) never closed into a valid profile, so the
// extrude produced NO SOLID BODY. CreateCenterRectangle is proven to extrude to a
// real solid every time, so every elongated part now uses it.
// Convention: sketch on Front Plane (XY = side plane), extrude MIDPLANE on Z so
// every part is symmetric about z=0. Link parts: local origin at pivot-1, body
// runs +X to pivot-2; rectangle is extended past each pivot by half-height so the
// joints visually overlap.
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

        // Solid torso body (depth 60 mm), generously rounded.
        BoxAt("Torso_3D.SLDPRT", 22, 47, 248, 130, 60, 10);   // centered on body span

        // Beam links: (file, length pivot-to-pivot, height, thickness Z, fillet).
        Beam("L1_Crank_40.SLDPRT", 40, 24, 16, 3);
        Beam("L2_Coupler_120.SLDPRT", 120, 22, 16, 3);
        Beam("L3_Thigh_100.SLDPRT", 100, 32, 20, 4);
        Beam("L4_Shank_140.SLDPRT", 140, 30, 20, 4);
        Beam("L5_Rocker_180.SLDPRT", 180, 28, 18, 4);
        Beam("Foot_80.SLDPRT", 80, 34, 26, 5);
        Beam("TailRod_210.SLDPRT", 210, 22, 18, 4);
        Beam("ElasticTendon_52.SLDPRT", 52, 12, 12, 2);

        // Round / box solids.
        Cyl("TailMass.SLDPRT", 24, 44, 6);
        Cyl("Drum.SLDPRT", 20, 30, 3);
        Cyl("M3_Axle_120.SLDPRT", 2.0, 120, 0);   // sharp pin, no fillet
        Box("Motor.SLDPRT", 74, 40, 40, 4);
        Box("Servo.SLDPRT", 46, 30, 42, 3);
        Box("Latch.SLDPRT", 50, 16, 22, 2);

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
        m.FeatureManager.FeatureExtrusion2(true, false, false,
            (int)swEndConditions_e.swEndCondMidPlane, 0,
            depthMm * MM, 0, false, false, false, false, 0, 0,
            false, false, false, false, true, true, true, 0, 0, false);
    }

    // Round over every edge of the solid body for a cleaner, more finished look.
    static void FilletAll(ModelDoc2 m, double radiusMm)
    {
        var part = (PartDoc)m;
        var bodies = (object[])part.GetBodies2((int)swBodyType_e.swSolidBody, false);
        if (bodies == null || bodies.Length == 0) return;
        var edges = (object[])((Body2)bodies[0]).GetEdges();
        if (edges == null || edges.Length == 0) return;
        m.ClearSelection2(true);
        foreach (var e in edges) ((Entity)e).Select4(true, null);
        m.FeatureManager.FeatureFillet2(
            (int)swFeatureFilletOptions_e.swFeatureFilletUniformRadius,
            radiusMm * MM, 0,
            (int)swFeatureFilletType_e.swFeatureFilletType_Simple,
            0, 0, null, null, null, null, null);
        m.ClearSelection2(true);
    }

    static void Save(ModelDoc2 m, string file, double filletMm)
    {
        m.ForceRebuild3(false);
        var b0 = (object[])((PartDoc)m).GetBodies2((int)swBodyType_e.swSolidBody, false);
        if (b0 != null && b0.Length > 0 && filletMm > 0) FilletAll(m, filletMm);
        m.ForceRebuild3(false);
        // verify a solid body still exists after filleting
        var bodies = (object[])((PartDoc)m).GetBodies2((int)swBodyType_e.swSolidBody, false);
        int nb = bodies == null ? 0 : bodies.Length;
        int err = m.SaveAs3(Path.Combine(OutDir, file), 0, 2);
        Console.WriteLine(file + " saveErr=" + err + " solidBodies=" + nb + " fillet=" + filletMm);
        swApp.CloseDoc(m.GetTitle());
    }

    // Rectangle beam: origin at pivot-1, runs +X to pivot-2 (length L), height H,
    // extended by H/2 past each end so joints overlap. Thickness T on Z.
    static void Beam(string file, double L, double H, double T, double fillet)
    {
        var m = NewPart(file.Replace(".SLDPRT", ""));
        double x0 = -H / 2.0, x1 = L + H / 2.0;
        double cx = (x0 + x1) / 2.0, halfx = (x1 - x0) / 2.0;
        m.SketchManager.InsertSketch(true);
        m.SketchManager.CreateCenterRectangle(cx * MM, 0, 0, (cx + halfx) * MM, (H / 2.0) * MM, 0);
        m.SketchManager.InsertSketch(true);
        MidExtrude(m, T);
        Save(m, file, fillet);
    }

    // Box centered at (cx,cy) with footprint lx x ly, depth lz on Z.
    static void BoxAt(string file, double cx, double cy, double lx, double ly, double lz, double fillet)
    {
        var m = NewPart(file.Replace(".SLDPRT", ""));
        m.SketchManager.InsertSketch(true);
        m.SketchManager.CreateCenterRectangle(cx * MM, cy * MM, 0,
            (cx + lx / 2.0) * MM, (cy + ly / 2.0) * MM, 0);
        m.SketchManager.InsertSketch(true);
        MidExtrude(m, lz);
        Save(m, file, fillet);
    }

    static void Box(string file, double lx, double ly, double lz, double fillet)
    {
        BoxAt(file, 0, 0, lx, ly, lz, fillet);
    }

    static void Cyl(string file, double rad, double depth, double fillet)
    {
        var m = NewPart(file.Replace(".SLDPRT", ""));
        m.SketchManager.InsertSketch(true);
        m.SketchManager.CreateCircleByRadius(0, 0, 0, rad * MM);
        m.SketchManager.InsertSketch(true);
        MidExtrude(m, depth);
        Save(m, file, fillet);
    }
}
