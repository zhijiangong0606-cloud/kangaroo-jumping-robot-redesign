using System;
using System.IO;
using System.Collections.Generic;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Stage 1 (v4 - PRINTABLE + OPERABLE):
// Every leg link is a flat plate with TWO pivot through-holes (one per pivot).
// Holes are produced by sketching the outer rectangle AND the pivot circles in a
// single sketch, then extruding the bounded region (rectangle minus circles).
// This auto-creates clean through holes and is far more robust than FeatureCut.
//
// The torso is replaced by a real body cage = two side plates + standoffs, so the
// leg links PIN DIRECTLY to the body at the fixed pivots H0/H1/H2 (and tail at T).
//
// Convention: sketch on Front Plane (XY = side plane). Link local origin = pivot-1,
// body runs +X to pivot-2 (length L). Extrude MIDPLANE on Z so every part is
// symmetric about its own z=0; the assembly then offsets each part to its layer.
//
// Fits (PLA / FDM):
//   moving link hole  = 3.4 mm (r 1.70)  -> 0.4 mm running clearance on a 3.0 pin
//   body plate hole   = 3.2 mm (r 1.60)  -> snug for fixed M3 / press pin
//   pin diameter      = 3.0 mm  (M3 bolt or printed rod)
class CreatePrintableParts
{
    const double MM = 0.001;
    const double HOLE_MOVE = 3.4;   // moving-link pivot hole dia
    const double HOLE_BODY = 3.2;   // body plate hole dia
    const double PIN_DIA   = 3.0;   // pin / M3

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

        // ---- Body cage: two side plates (left/right) + standoffs ----
        // Plate covers fixed pivots H0(0,0) H1(-80,60) H2(120,60) and tail root T(-95,2).
        BodyPlate("BodyPlate_Side.SLDPRT", 4);

        // Standoff: spacer tube that bolts the two plates together at the 4 corners.
        // Cage gap between plate inner faces = 64 mm -> standoff length 64.
        Standoff("Standoff_64.SLDPRT", 64, 8, PIN_DIA + 0.2);  // 64 long, OD8, bore 3.2

        // ---- Leg links: file, length(pivot-to-pivot), height, thickness, fillet ----
        // thickness 4 mm for all moving links -> uniform layer pitch.
        HoledBeam("L1_Crank_40.SLDPRT",   40, 24, 4, 3, HOLE_MOVE);
        HoledBeam("L2_Coupler_120.SLDPRT",120, 22, 4, 3, HOLE_MOVE);
        HoledBeam("L3_Thigh_100.SLDPRT",  100, 32, 4, 4, HOLE_MOVE);
        HoledBeam("L4_Shank_140.SLDPRT",  140, 30, 4, 4, HOLE_MOVE);
        HoledBeam("L5_Rocker_180.SLDPRT", 180, 28, 4, 4, HOLE_MOVE);
        HoledBeam("Foot_80.SLDPRT",        80, 34, 4, 5, HOLE_MOVE);
        HoledBeam("TailRod_210.SLDPRT",   210, 22, 4, 4, HOLE_MOVE);
        HoledBeam("ElasticTendon_52.SLDPRT",52, 12, 4, 2, HOLE_MOVE);

        // ---- Round / box solids ----
        CylHole("TailMass.SLDPRT", 22, 40, HOLE_MOVE, 6);   // ball-ish mass, center bore
        CylHole("Drum.SLDPRT",     20, 26, HOLE_BODY, 3);   // winding drum, bore
        Pin("M3_Axle_80.SLDPRT", PIN_DIA / 2.0, 80);        // full-width synchronizing pin
        Pin("M3_Axle_40.SLDPRT", PIN_DIA / 2.0, 40);        // tail / single-side pin
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

    // Open a sketch with snapping/inference DISABLED so small circles at the origin
    // are added to the DB verbatim (lone r=1.5 circles otherwise get rejected).
    static void OpenSketch(ModelDoc2 m)
    {
        var sm = m.SketchManager;
        sm.InsertSketch(true);
        sm.AddToDB = true;
        sm.DisplayWhenAdded = false;
    }

    static void CloseSketch(ModelDoc2 m)
    {
        var sm = m.SketchManager;
        sm.AddToDB = false;
        sm.DisplayWhenAdded = true;
        sm.InsertSketch(true);
    }


    // Fillet ONLY the outer-perimeter vertical edges would be ideal, but a uniform
    // fillet on a plate with holes can fail; so we fillet conservatively and verify
    // a solid body survives, otherwise we keep the un-filleted body.
    static bool TryFilletAll(ModelDoc2 m, double radiusMm)
    {
        var part = (PartDoc)m;
        var bodies = (object[])part.GetBodies2((int)swBodyType_e.swSolidBody, false);
        if (bodies == null || bodies.Length == 0) return false;
        var edges = (object[])((Body2)bodies[0]).GetEdges();
        if (edges == null || edges.Length == 0) return false;
        m.ClearSelection2(true);
        foreach (var e in edges) ((Entity)e).Select4(true, null);
        try
        {
            m.FeatureManager.FeatureFillet2(
                (int)swFeatureFilletOptions_e.swFeatureFilletUniformRadius,
                radiusMm * MM, 0,
                (int)swFeatureFilletType_e.swFeatureFilletType_Simple,
                0, 0, null, null, null, null, null);
        }
        catch { }
        m.ClearSelection2(true);
        m.ForceRebuild3(false);
        var after = (object[])part.GetBodies2((int)swBodyType_e.swSolidBody, false);
        return after != null && after.Length > 0;
    }

    static void Save(ModelDoc2 m, string file)
    {
        m.ForceRebuild3(false);
        var bodies = (object[])((PartDoc)m).GetBodies2((int)swBodyType_e.swSolidBody, false);
        int nb = bodies == null ? 0 : bodies.Length;
        int err = m.SaveAs3(Path.Combine(OutDir, file), 0, 2);
        Console.WriteLine(file + " saveErr=" + err + " solidBodies=" + nb);
        swApp.CloseDoc(m.GetTitle());
    }

    static void AddProps(ModelDoc2 m, string nameCn, string note)
    {
        var cpm = m.Extension.get_CustomPropertyManager("");
        cpm.Add3("PartName_CN", 30, nameCn, 2);
        cpm.Add3("Notes", 30, note, 2);
    }

    // ---------- Leg link with two pivot through-holes ----------
    // origin at pivot-1 (0,0); pivot-2 at (L,0). Outer rectangle extended H/2 past
    // each pivot so material fully surrounds each hole. Holes dia=holeDia at both
    // pivots. Single sketch (rect + 2 circles) -> extrude gives plate WITH holes.
    static void HoledBeam(string file, double L, double H, double T, double fillet, double holeDia)
    {
        var m = NewPart(file.Replace(".SLDPRT", ""));
        double x0 = -H / 2.0, x1 = L + H / 2.0;
        double cx = (x0 + x1) / 2.0, halfx = (x1 - x0) / 2.0;
        var sm = m.SketchManager;
        OpenSketch(m);
        sm.CreateCenterRectangle(cx * MM, 0, 0, (cx + halfx) * MM, (H / 2.0) * MM, 0);
        sm.CreateCircleByRadius(0, 0, 0, (holeDia / 2.0) * MM);          // pivot-1 hole
        sm.CreateCircleByRadius(L * MM, 0, 0, (holeDia / 2.0) * MM);     // pivot-2 hole
        CloseSketch(m);
        MidExtrude(m, T);
        if (fillet > 0) TryFilletAll(m, fillet);
        AddProps(m, file.Replace(".SLDPRT", ""), "pivot holes dia " + holeDia + "mm @0 and @" + L + "mm");
        Save(m, file);
    }

    // ---------- Body side plate (the "box" the legs pin to) ----------
    // Fixed pivots in body frame: H0(0,0) H1(-80,60) H2(120,60) tail root T(-95,2).
    // Plate is a rounded rectangle spanning them, with a through-hole at each pivot
    // plus 4 corner standoff holes. One sketch: outer rect + all circles.
    static void BodyPlate(string file, double T)
    {
        var m = NewPart(file.Replace(".SLDPRT", ""));
        var sm = m.SketchManager;
        // bounding box around pivots + corner standoffs, with margin.
        // Corner standoffs now sit at explicit coords (-130,95)/(138,95) up top
        // (moved out to clear the L1/L2 sweep), so the plate must extend past
        // y=95 and x=138 with edge margin. Keep ~13 mm around every hole.
        double minX = -145, maxX = 152, minY = -42, maxY = 110;
        double cx = (minX + maxX) / 2.0, cy = (minY + maxY) / 2.0;
        double w = maxX - minX, h = maxY - minY;
        OpenSketch(m);
        sm.CreateCenterRectangle(cx * MM, cy * MM, 0, (cx + w / 2.0) * MM, (cy + h / 2.0) * MM, 0);
        // pivot holes (snug body fit)
        foreach (var p in new[] { new[] {0.0,0.0}, new[]{-80.0,60.0}, new[]{120.0,60.0}, new[]{-95.0,2.0} })
            sm.CreateCircleByRadius(p[0] * MM, p[1] * MM, 0, (HOLE_BODY / 2.0) * MM);
        // 4 corner standoff holes (explicit coords, must match the standoff
        // placements in CreatePrintableAssembly.cs corners[]).
        foreach (var p in new[] { new[]{-118.0,-28.0}, new[]{138.0,-28.0}, new[]{-130.0,95.0}, new[]{138.0,95.0} })
            sm.CreateCircleByRadius(p[0] * MM, p[1] * MM, 0, ((PIN_DIA + 0.2) / 2.0) * MM);
        CloseSketch(m);
        MidExtrude(m, T);
        AddProps(m, "机身侧板", "body cage side plate; pivots H0/H1/H2/T + 4 corner standoff holes");
        Save(m, file);
    }

    // ---------- Standoff spacer tube (ties the two plates together) ----------
    static void Standoff(string file, double length, double od, double bore)
    {
        var m = NewPart(file.Replace(".SLDPRT", ""));
        var sm = m.SketchManager;
        OpenSketch(m);
        sm.CreateCircleByRadius(0, 0, 0, (od / 2.0) * MM);
        sm.CreateCircleByRadius(0, 0, 0, (bore / 2.0) * MM);
        CloseSketch(m);
        MidExtrude(m, length);
        AddProps(m, "支撑立柱", "standoff OD" + od + " bore" + bore + " len" + length);
        Save(m, file);
    }

    // ---------- Cylinder with center bore ----------
    static void CylHole(string file, double rad, double depth, double boreDia, double fillet)
    {
        var m = NewPart(file.Replace(".SLDPRT", ""));
        var sm = m.SketchManager;
        OpenSketch(m);
        sm.CreateCircleByRadius(0, 0, 0, rad * MM);
        sm.CreateCircleByRadius(0, 0, 0, (boreDia / 2.0) * MM);
        CloseSketch(m);
        MidExtrude(m, depth);
        if (fillet > 0) TryFilletAll(m, fillet);
        Save(m, file);
    }

    // ---------- Solid pin ----------
    static void Pin(string file, double rad, double length)
    {
        var m = NewPart(file.Replace(".SLDPRT", ""));
        var sm = m.SketchManager;
        OpenSketch(m);
        sm.CreateCircleByRadius(0, 0, 0, rad * MM);
        CloseSketch(m);
        MidExtrude(m, length);
        AddProps(m, "销轴", "pin dia " + (rad * 2) + " len " + length);
        Save(m, file);
    }

    static void Box(string file, double lx, double ly, double lz, double fillet)
    {
        var m = NewPart(file.Replace(".SLDPRT", ""));
        var sm = m.SketchManager;
        OpenSketch(m);
        sm.CreateCenterRectangle(0, 0, 0, (lx / 2.0) * MM, (ly / 2.0) * MM, 0);
        CloseSketch(m);
        MidExtrude(m, lz);
        if (fillet > 0) TryFilletAll(m, fillet);
        Save(m, file);
    }
}
