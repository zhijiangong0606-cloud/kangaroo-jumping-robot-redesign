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
        // Plate covers fixed pivots H0(0,0) H1(-94.8,64.4) H2(134.3,38) and tail T.
        // 3 mm thick (was 4) with a dense lightening-hole field -> ~40% lighter while
        // the pinned/standoff regions stay solid.
        BodyPlate("BodyPlate_Side.SLDPRT", 3);

        // Standoff: spacer tube that bolts the two plates together at the 4 corners.
        // Cage gap between plate inner faces = 64 mm -> standoff length 64.
        Standoff("Standoff_64.SLDPRT", 64, 8, PIN_DIA + 0.2);  // 64 long, OD8, bore 3.2

        // ---- Leg links: file, length(pivot-to-pivot), height, thickness, fillet ----
        // v7 OPTIMIZED geometry: lengths re-solved so the foot gets a real ~35 mm
        // VERTICAL extension stroke (old design scrubbed horizontally, ~2 mm only).
        // File names keep their original numeric suffix for traceability; the actual
        // pivot pitch is the second arg below.
        //   L1=59.0  L2=108.9  L3=88.2  L4=100.3  L5=160.6  (mm, hole-to-hole)
        // Per-end seats (pivot-1 @ first arg of LinkZ, pivot-2 @ second):
        //   BEAR = Ø10 ball-bearing seat at a FIXED pivot (H0/H1/H2)
        //   BUSH = Ø6  sleeve-bushing seat at a MOVING pivot (A/B/F)
        const double BEAR = 10.0, BUSH = 6.0;
        HoledBeam("L1_Crank_40.SLDPRT",   59.0,  24, 4, 3, BEAR, BUSH);   // H1->A
        HoledBeam("L2_Coupler_120.SLDPRT",108.9, 22, 4, 3, BUSH, BUSH);   // A->B
        HoledBeam("L3_Thigh_100.SLDPRT",  88.2,  32, 4, 4, BEAR, BUSH);   // H0->B
        HoledBeam("L4_Shank_140.SLDPRT",  100.3, 30, 4, 4, BUSH, BUSH);   // B->F
        HoledBeam("L5_Rocker_180.SLDPRT", 160.6, 28, 4, 4, BEAR, BUSH);   // H2->F
        HoledBeam("Foot_80.SLDPRT",        80,   34, 4, 5, BUSH, HOLE_MOVE); // F->FootTip
        // v11 DUAL-MODE TAIL: the tail is no longer a dead rod. Its root pivots about T
        // on a full-width axle (-> a real revolute joint), so it can swing between two
        // hard limits set by a body-fixed stop pin riding in a curved slot near the root:
        //   DOWN limit  = tail tip drops to the ground line -> "fifth leg" support
        //   UP   limit  = tail tip lifts clear -> airborne counterbalance during a hop
        // This mirrors the real kangaroo (O'Connor 2014: tail = propulsive 5th leg when
        // walking, lifted counterbalance when hopping). The curved slot is centred on the
        // root pivot (local origin) at radius SLOT_R, spanning +-SLOT_HALF_DEG; a Ø3.4
        // clearance width takes the Ø3 stop pin. Slot ends are the physical stops.
        // Beam height 26 (was 22): the limit slot outer edge reaches y=+-9.3 mm at the
        // extreme angles, so a 26 mm-tall rod keeps ~3.7 mm of solid material on each
        // side of the slot to carry the stop load. Tail is NOT in the v7 chain, so this
        // height change does not affect the mechanism geometry.
        TailRodHinged("TailRod_210.SLDPRT", 210, 26, 4, 4);
        HoledBeam("ElasticTendon_52.SLDPRT",52, 12, 4, 2, HOLE_MOVE, HOLE_MOVE);

        // ---- Round / box solids ----
        // Tail counterweight: a 16 mm-thick Ø44 disk with an M3 center bore. It stacks
        // ON the tail-rod end (not enveloping it) and is clamped by an M3 pin through
        // the rod-end hole + this bore. Real tuning mass = steel washers/nuts on the pin.
        CylHole("TailMass.SLDPRT", 22, 16, HOLE_MOVE, 4);   // Ø44 x 16, center bore
        Pin("M3_Axle_80.SLDPRT", PIN_DIA / 2.0, 80);        // full-width synchronizing pin
        Pin("M3_Axle_40.SLDPRT", PIN_DIA / 2.0, 40);        // tail / single-side pin
        // Grooved full-width pin for the MOVING pivots A/B/F: links rotate on bushings
        // around it; two turned grooves (just outside each plate) take GB/T 896 E-rings
        // for axial retention -- the standard "shaft + circlip" location method.
        GroovedPin("M3_GroovedAxle_80.SLDPRT", PIN_DIA / 2.0, 80, 1.15, 0.6, 3.5);
        // NOTE: Drum / Motor / Servo / Latch are NOT generated here any more --
        // they are functional parts built by CreateFunctionalParts.exe. Generating
        // them here would overwrite those with placeholder boxes. (left intentionally out)

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

    // ---------- Leg link with two pivot through-holes (per-end hole dia) ----------
    // origin at pivot-1 (0,0); pivot-2 at (L,0). Outer rectangle extended H/2 past
    // each pivot so material fully surrounds each hole. Pivot-1 hole = d1, pivot-2 = d2.
    // v8: fixed-pivot ends open a Ø10 seat for a 623 ball bearing; moving-pivot ends a
    // Ø6 seat for a sleeve bushing. Bearing (4mm wide) / bushing sit FLUSH in the 4mm
    // link, so the verified 5mm-pitch Z-stack (1mm gaps) is unchanged.
    static void HoledBeam(string file, double L, double H, double T, double fillet, double d1, double d2)
    {
        var m = NewPart(file.Replace(".SLDPRT", ""));
        double x0 = -H / 2.0, x1 = L + H / 2.0;
        double cx = (x0 + x1) / 2.0, halfx = (x1 - x0) / 2.0;
        var sm = m.SketchManager;
        OpenSketch(m);
        sm.CreateCenterRectangle(cx * MM, 0, 0, (cx + halfx) * MM, (H / 2.0) * MM, 0);
        sm.CreateCircleByRadius(0, 0, 0, (d1 / 2.0) * MM);          // pivot-1 hole (seat)
        sm.CreateCircleByRadius(L * MM, 0, 0, (d2 / 2.0) * MM);     // pivot-2 hole (seat)
        CloseSketch(m);
        MidExtrude(m, T);
        if (fillet > 0) TryFilletAll(m, fillet);
        AddProps(m, file.Replace(".SLDPRT", ""), "pivot seats: Ø" + d1 + "@0 (bearing/bushing) + Ø" + d2 + "@" + L);
        Save(m, file);
    }

    // ---------- Dual-mode tail rod (hinged at root + curved limit slot) ----------
    // Same flat beam as HoledBeam (origin at root pivot, tip hole at (L,0)), PLUS a
    // curved limit slot centred on the root pivot. The slot is one closed contour:
    // an outer arc (R + half-width) and an inner arc (R - half-width) joined by short
    // end caps, swept over +-SLOT_HALF_DEG. A body-fixed stop pin sits in this slot;
    // the two slot ends are the UP / DOWN hard limits of the swing. Single contour =
    // robust under the even/odd fill (same trick as KeyedLoop / Poly).
    public const double SLOT_R        = 28.0;   // slot mean radius from root pivot (mm)
    public const double SLOT_HALF_DEG = 18.25;  // half-span -> 36.5 deg total swing
    const double SLOT_HALF_W   = HOLE_MOVE / 2.0; // slot half-width (Ø3.4 -> Ø3 pin clear)
    static void TailRodHinged(string file, double L, double H, double T, double fillet)
    {
        var m = NewPart(file.Replace(".SLDPRT", ""));
        double x0 = -H / 2.0, x1 = L + H / 2.0;
        double cx = (x0 + x1) / 2.0, halfx = (x1 - x0) / 2.0;
        var sm = m.SketchManager;
        OpenSketch(m);
        sm.CreateCenterRectangle(cx * MM, 0, 0, (cx + halfx) * MM, (H / 2.0) * MM, 0);
        sm.CreateCircleByRadius(0, 0, 0, (HOLE_MOVE / 2.0) * MM);        // root pivot hole
        sm.CreateCircleByRadius(L * MM, 0, 0, (HOLE_MOVE / 2.0) * MM);   // tip hole (pin->mass)
        // curved limit slot, centred on root pivot (0,0). It runs ALONG the rod axis
        // (local +X, centreDeg=0) where the beam has material; the body stop pin sits
        // at the slot's mean radius when the tail is at mid-swing, and the two slot ends
        // become the UP / DOWN hard stops as the rod rotates about its root.
        ArcSlot(sm, SLOT_R, SLOT_HALF_W, SLOT_HALF_DEG, 0.0);
        CloseSketch(m);
        MidExtrude(m, T);
        if (fillet > 0) TryFilletAll(m, fillet);
        AddProps(m, file.Replace(".SLDPRT", ""),
            "dual-mode tail: root pivot Ø3.4 + tip Ø3.4; curved limit slot R" + SLOT_R +
            " +-" + SLOT_HALF_DEG + "deg for body stop pin (UP/DOWN hard limits)");
        Save(m, file);
    }

    // One closed slot contour: two concentric arcs (rMean +- halfW) over +-halfDeg about
    // centreDeg, joined by straight end caps. Angles in degrees, centre at sketch origin.
    static void ArcSlot(ISketchManager sm, double rMean, double halfW, double halfDeg, double centreDeg)
    {
        double a0 = (centreDeg - halfDeg) * Math.PI / 180.0;
        double a1 = (centreDeg + halfDeg) * Math.PI / 180.0;
        double ro = rMean + halfW, ri = rMean - halfW;
        double ox0 = ro*Math.Cos(a0), oy0 = ro*Math.Sin(a0);   // outer arc start
        double ox1 = ro*Math.Cos(a1), oy1 = ro*Math.Sin(a1);   // outer arc end
        double ix0 = ri*Math.Cos(a0), iy0 = ri*Math.Sin(a0);   // inner arc start
        double ix1 = ri*Math.Cos(a1), iy1 = ri*Math.Sin(a1);   // inner arc end
        // outer arc a0 -> a1 (CCW, dir +1), then cap, inner arc a1 -> a0 (CW, dir -1), cap
        sm.CreateArc(0, 0, 0, ox0*MM, oy0*MM, 0, ox1*MM, oy1*MM, 0, 1);
        sm.CreateLine(ox1*MM, oy1*MM, 0, ix1*MM, iy1*MM, 0);
        sm.CreateArc(0, 0, 0, ix1*MM, iy1*MM, 0, ix0*MM, iy0*MM, 0, -1);
        sm.CreateLine(ix0*MM, iy0*MM, 0, ox0*MM, oy0*MM, 0);
    }
    // v7 fixed pivots: H0(0,0) H1(-94.8,64.4) H2(134.3,38) tail root T(-95,2).
    // Plate is a rounded rectangle spanning them, with a through-hole at each pivot
    // plus 4 corner standoff holes. One sketch: outer rect + all circles.
    static void BodyPlate(string file, double T)
    {
        var m = NewPart(file.Replace(".SLDPRT", ""));
        var sm = m.SketchManager;
        // bounding box around pivots + corner standoffs, with margin.
        // Standoffs sit at the 4 explicit corners below; plate extends past them with
        // ~13 mm edge margin around every hole.
        double minX = -150, maxX = 165, minY = -42, maxY = 112;
        double cx = (minX + maxX) / 2.0, cy = (minY + maxY) / 2.0;
        double w = maxX - minX, h = maxY - minY;
        OpenSketch(m);
        sm.CreateCenterRectangle(cx * MM, cy * MM, 0, (cx + w / 2.0) * MM, (cy + h / 2.0) * MM, 0);
        // pivot holes (snug body fit): H0, H1, H2, tail root T, tail stop pin TS.
        // TS(-122.735,-1.84) = body-fixed stop pin that rides the tail's curved limit
        // slot (28 mm from root T); the slot ends are the UP/DOWN hard stops.
        foreach (var p in new[] { new[] {0.0,0.0}, new[]{-94.8,64.4}, new[]{134.3,38.0}, new[]{-95.0,2.0}, new[]{-122.735,-1.84} })
            sm.CreateCircleByRadius(p[0] * MM, p[1] * MM, 0, (HOLE_BODY / 2.0) * MM);
        // 3 winch-post tie holes (only used on the +Z plate; harmless on -Z plate)
        foreach (var p in new[] { new[]{-88.0,74.0}, new[]{-2.0,74.0}, new[]{-45.0,108.0} })
            sm.CreateCircleByRadius(p[0] * MM, p[1] * MM, 0, ((PIN_DIA + 0.2) / 2.0) * MM);
        // 4 corner standoff holes (explicit coords, must match the standoff
        // placements in CreatePrintableAssembly.cs corners[]).
        var keepout = new System.Collections.Generic.List<double[]> {
            new[]{0.0,0.0}, new[]{-94.8,64.4}, new[]{134.3,38.0}, new[]{-95.0,2.0}, new[]{-122.735,-1.84},
            new[]{-135.0,-28.0}, new[]{152.0,-28.0}, new[]{-135.0,98.0}, new[]{152.0,98.0},
            new[]{-88.0,74.0}, new[]{-2.0,74.0}, new[]{-45.0,108.0}
        };
        foreach (var p in new[] { new[]{-135.0,-28.0}, new[]{152.0,-28.0}, new[]{-135.0,98.0}, new[]{152.0,98.0} })
            sm.CreateCircleByRadius(p[0] * MM, p[1] * MM, 0, ((PIN_DIA + 0.2) / 2.0) * MM);
        // ---- lightening holes (mass reduction): Ø30 grid in open plate area,
        // skipping any position within 30 mm of a pivot/standoff/edge. Drops the
        // plate from ~130 g to ~80 g each without weakening the pinned regions.
        double LR = 17.0;   // lightening hole radius
        for (double gx = minX + 30; gx <= maxX - 30; gx += 38)
            for (double gy = minY + 30; gy <= maxY - 30; gy += 38)
            {
                bool ok = true;
                foreach (var k in keepout)
                    if (Math.Sqrt((gx-k[0])*(gx-k[0])+(gy-k[1])*(gy-k[1])) < LR + 12) { ok = false; break; }
                if (ok) sm.CreateCircleByRadius(gx * MM, gy * MM, 0, LR * MM);
            }
        CloseSketch(m);
        MidExtrude(m, T);
        AddProps(m, "机身侧板", "body cage side plate v7; pivots H0/H1/H2/T + 4 corner standoff holes");
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

    // Blind coaxial boss extruded +Z from the current top face at (0,0,faceZ).
    static void StackBoss(ModelDoc2 m, double faceZ, double rad, double len)
    {
        m.ClearSelection2(true);
        m.Extension.SelectByID2("", "FACE", 0, 0, faceZ * MM, false, 0, null, 0);
        var sm = m.SketchManager;
        sm.InsertSketch(true); sm.AddToDB = true; sm.DisplayWhenAdded = false;
        sm.CreateCircleByRadius(0, 0, 0, rad * MM);
        sm.AddToDB = false; sm.DisplayWhenAdded = true; sm.InsertSketch(true);
        m.FeatureManager.FeatureExtrusion2(true, false, false,
            (int)swEndConditions_e.swEndCondBlind, 0, len * MM, 0,
            false, false, false, false, 0, 0,
            false, false, false, false, true, true, true, 0, 0, false);
    }

    // ---------- Grooved pin (for circlip axial retention) ----------
    // Built base-at-z=0 -> top-at-z=L as stacked coaxial cylinders: a turned circlip
    // groove = a thinner segment (grooveR) between full-diameter segments, near each
    // end. No cuts -> robust. The assembly places its base at (pivotZ - L/2).
    static void GroovedPin(string file, double rad, double length, double grooveR, double grooveW, double margin)
    {
        var m = NewPart(file.Replace(".SLDPRT", ""));
        var sm = m.SketchManager;
        // base segment (blind +Z) from Front plane: full rad, length = margin
        OpenSketch(m);
        sm.CreateCircleByRadius(0, 0, 0, rad * MM);
        CloseSketch(m);
        m.FeatureManager.FeatureExtrusion2(true, false, false,
            (int)swEndConditions_e.swEndCondBlind, 0, margin * MM, 0,
            false, false, false, false, 0, 0,
            false, false, false, false, true, true, true, 0, 0, false);
        double z = margin;
        StackBoss(m, z, grooveR, grooveW); z += grooveW;                       // groove 1
        double mid = length - 2 * margin - 2 * grooveW;
        StackBoss(m, z, rad, mid); z += mid;                                   // shank
        StackBoss(m, z, grooveR, grooveW); z += grooveW;                       // groove 2
        StackBoss(m, z, rad, margin); z += margin;                             // end
        AddProps(m, "销轴(带挡圈槽)", "pin Ø" + (rad*2) + " L" + length + "; 2 circlip grooves Ø" + (grooveR*2) + " for GB/T 896 E-ring");
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
