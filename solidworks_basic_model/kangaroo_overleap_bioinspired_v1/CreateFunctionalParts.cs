using System;
using System.IO;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Stage 1b (v6 - FUNCTIONAL energy module):
// Rebuilds the placeholder Motor/Servo/Latch/Drum into REAL, fitting, printable parts
// and adds retention hardware (Washer, M3 bolt + nut) and a motor clamp.
// Leg links / plates / standoff / tail / tendon are left untouched (already buildable).
//
// Convention (same as CreatePrintableParts): sketch on Front Plane (XY = side plane),
// extrude MIDPLANE on Z so each part is symmetric about its own z=0.
// Fits (PLA/FDM): moving hole 3.4, body/snug hole 3.2, pin/M3 = 3.0.
class CreateFunctionalParts
{
    const double MM = 0.001;
    const double HOLE_MOVE = 3.4;
    const double PIN_DIA   = 3.0;

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

        Latch("Latch.SLDPRT");
        Drum("Drum.SLDPRT");
        Motor("Motor.SLDPRT");
        Servo("Servo.SLDPRT");
        Washer("Washer.SLDPRT", 7.0, HOLE_MOVE, 1.0);
        BoltSocket("M3_Bolt_80.SLDPRT", PIN_DIA / 2.0, 80, 5.5, 3.0);
        HexNut("M3_Nut.SLDPRT", 5.5, 2.4, 3.2);
        MotorClamp("MotorClamp.SLDPRT");
        WinchBracket("WinchBracket.SLDPRT");    // carries latch pivot + servo + drum-axis; ties drive module to plate
        WinchPost("WinchPost_94.SLDPRT", 94, 8, PIN_DIA + 0.2);  // long post: bracket -> +Z side plate
        BatteryPack("BatteryPack.SLDPRT");      // 2S Li-ion + controller mass, mounts -Z to balance winch
        FootPad("FootPad.SLDPRT");              // rubber ground-contact buffer on the foot tip

        Console.WriteLine("FUNCTIONAL PARTS DONE");
    }

    // ---------------- shared helpers (same proven pattern) ----------------
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

    static void MidExtrude(ModelDoc2 m, double depthMm)
    {
        m.FeatureManager.FeatureExtrusion2(true, false, false,
            (int)swEndConditions_e.swEndCondMidPlane, 0,
            depthMm * MM, 0, false, false, false, false, 0, 0,
            false, false, false, false, true, true, true, 0, 0, false);
    }

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

    static void AddProps(ModelDoc2 m, string nameCn, string note)
    {
        var cpm = m.Extension.get_CustomPropertyManager("");
        cpm.Add3("PartName_CN", 30, nameCn, 2);
        cpm.Add3("Notes", 30, note, 2);
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

    static void Rect(ISketchManager sm, double cx, double cy, double hx, double hy)
    {
        sm.CreateCenterRectangle(cx * MM, cy * MM, 0, (cx + hx) * MM, (cy + hy) * MM, 0);
    }
    static void Hole(ISketchManager sm, double x, double y, double dia)
    {
        sm.CreateCircleByRadius(x * MM, y * MM, 0, (dia / 2.0) * MM);
    }
    // Single closed polyline outline (avoids the overlapping-contour even-odd void).
    static void Poly(ISketchManager sm, double[][] pts)
    {
        int n = pts.Length;
        for (int i = 0; i < n; i++)
        {
            var a = pts[i]; var b = pts[(i + 1) % n];
            sm.CreateLine(a[0] * MM, a[1] * MM, 0, b[0] * MM, b[1] * MM, 0);
        }
    }
    // Merge-boss a rectangle on the Front plane (second feature, midplane on Z).
    static void RectBoss(ModelDoc2 m, double cx, double cy, double hx, double hy, double depth)
    {
        m.ClearSelection2(true);
        m.Extension.SelectByID2("Front Plane", "PLANE", 0, 0, 0, false, 0, null, 0);
        var sm = m.SketchManager;
        sm.InsertSketch(true); sm.AddToDB = true; sm.DisplayWhenAdded = false;
        sm.CreateCenterRectangle(cx * MM, cy * MM, 0, (cx + hx) * MM, (cy + hy) * MM, 0);
        sm.AddToDB = false; sm.DisplayWhenAdded = true; sm.InsertSketch(true);
        MidExtrude(m, depth);
    }

    // Merge-boss a closed polyline (Front plane, midplane on Z) - for raked teeth.
    static void PolyBoss(ModelDoc2 m, double[][] pts, double depth)
    {
        m.ClearSelection2(true);
        m.Extension.SelectByID2("Front Plane", "PLANE", 0, 0, 0, false, 0, null, 0);
        var sm = m.SketchManager;
        sm.InsertSketch(true); sm.AddToDB = true; sm.DisplayWhenAdded = false;
        int n = pts.Length;
        for (int i = 0; i < n; i++)
        {
            var a = pts[i]; var b = pts[(i + 1) % n];
            sm.CreateLine(a[0] * MM, a[1] * MM, 0, b[0] * MM, b[1] * MM, 0);
        }
        sm.AddToDB = false; sm.DisplayWhenAdded = true; sm.InsertSketch(true);
        MidExtrude(m, depth);
    }

    // Blind boss extruded from the planar end face at z=faceZ, +Z direction.
    static void EndBoss(ModelDoc2 m, double faceZ, double radius, double length)
    {
        m.ClearSelection2(true);
        m.Extension.SelectByID2("", "FACE", 0, 0, faceZ * MM, false, 0, null, 0);
        var sm = m.SketchManager;
        sm.InsertSketch(true); sm.AddToDB = true; sm.DisplayWhenAdded = false;
        sm.CreateCircleByRadius(0, 0, 0, radius * MM);
        sm.AddToDB = false; sm.DisplayWhenAdded = true; sm.InsertSketch(true);
        m.FeatureManager.FeatureExtrusion2(true, false, false,
            (int)swEndConditions_e.swEndCondBlind, 0, length * MM, 0,
            false, false, false, false, 0, 0,
            false, false, false, false, true, true, true, 0, 0, false);
    }

    // Keyed output shaft: circle radius R with an INWARD keyway notch at +Y (floorR<R),
    // extruded length from the end face. Mates the GB/T 1096 key to transmit torque.
    static void KeyedEndBoss(ModelDoc2 m, double faceZ, double R, double floorR, double hw, double length)
    {
        m.ClearSelection2(true);
        m.Extension.SelectByID2("", "FACE", 0, 0, faceZ * MM, false, 0, null, 0);
        var sm = m.SketchManager;
        sm.InsertSketch(true); sm.AddToDB = true; sm.DisplayWhenAdded = false;
        KeyedLoop(sm, R, floorR, hw);
        sm.AddToDB = false; sm.DisplayWhenAdded = true; sm.InsertSketch(true);
        m.FeatureManager.FeatureExtrusion2(true, false, false,
            (int)swEndConditions_e.swEndCondBlind, 0, length * MM, 0,
            false, false, false, false, 0, 0,
            false, false, false, false, true, true, true, 0, 0, false);
    }

    // Closed loop = a circle of radius R with a rectangular keyway notch at +Y, as ONE
    // contour (major arc + 3 lines). floorR > R -> notch points OUTWARD (a hub bore
    // keyway); floorR < R -> notch points INWARD (a shaft keyway). hw = half key width.
    // Single contour => robust under the even/odd fill (same trick as Poly).
    static void KeyedLoop(ISketchManager sm, double R, double floorR, double hw)
    {
        double yc = Math.Sqrt(R * R - hw * hw);
        // major arc from (+hw,yc) the long way (through -Y) to (-hw,yc); dir -1 = CW.
        sm.CreateArc(0, 0, 0, hw * MM, yc * MM, 0, -hw * MM, yc * MM, 0, -1);
        sm.CreateLine(-hw * MM, yc * MM, 0, -hw * MM, floorR * MM, 0);   // left wall
        sm.CreateLine(-hw * MM, floorR * MM, 0, hw * MM, floorR * MM, 0); // floor
        sm.CreateLine(hw * MM, floorR * MM, 0, hw * MM, yc * MM, 0);      // right wall
    }

    // ---------------- functional parts ----------------

    // L-shaped release pawl. Pivot hole at origin; long tail arm in +X (servo horn
    // pushes it), hook arm down in -Y ending in a catch lip in -X that grabs the
    // drum tooth. Spring-return anchor hole near the tail.
    static void Latch(string file)
    {
        var m = NewPart("Latch");
        var sm = m.SketchManager;
        // base = tail/lever arm rectangle with pivot + spring holes (proven pattern)
        OpenSketch(m);
        Rect(sm, 18.5, 0, 26.5, 7);    // arm: x -8..45, y -7..7
        Hole(sm, 0, 0, HOLE_MOVE);     // pivot hole on a body pin
        Hole(sm, 42, 0, 2.5);          // return-spring anchor
        CloseSketch(m);
        MidExtrude(m, 6);
        // hook arm down (-Y); lip face is back-raked 14 deg so the drum-tooth load
        // self-locks the pawl (low servo release force, see latch_stress analysis).
        RectBoss(m, -3.0, -11.5, 5.0, 18.75, 6);   // hook arm: x -8..2, y -30..7
        // raked catch lip: the contact face (right edge) leans back 14 deg
        double rk = Math.Tan(14 * Math.PI / 180.0);
        PolyBoss(m, new[] {
            new[]{ -8.0, -22.0 }, new[]{ -8.0 - 4.0*rk, -22.0 - 4.0 },  // raked face
            new[]{ -14.0, -26.0 }, new[]{ -14.0, -22.0 }
        }, 6);
        TryFilletAll(m, 0.8);
        AddProps(m, "释放棘爪", "pawl: pivot@0; servo pushes tail @+X; 14deg self-locking lip; spring hole @42");
        Save(m, file);
    }

    // Winding drum (disc in XY, axis along Z). Center bore for the 6 mm motor shaft,
    // a back-raked ratchet tooth on the rim the latch lip self-locks against, and a
    // tendon anchor hole. Tooth widened for low bearing stress.
    static void Drum(string file)
    {
        var m = NewPart("Drum");
        var sm = m.SketchManager;
        OpenSketch(m);
        sm.CreateCircleByRadius(0, 0, 0, 18 * MM);   // drum body r18
        // keyed bore: Ø6.2 bore (r3.1) with an OUTWARD keyway notch at +Y, floor r4.0,
        // half-width 1.0 -> a 2x1 keyway that mates the GB/T 1096 key on the motor shaft.
        KeyedLoop(sm, 3.1, 4.0, 1.0);
        Hole(sm, 0, 14, 2.5);                        // tendon anchor through-hole
        CloseSketch(m);
        MidExtrude(m, 26);
        // back-raked sawtooth at +X rim: locking face (toward +Y) leans back 14 deg
        // to match the pawl lip; gentle ramp on the other side for re-cocking.
        double rk = Math.Tan(14 * Math.PI / 180.0);
        PolyBoss(m, new[] {
            new[]{ 18.0,  4.0 },                       // root on rim, +Y side
            new[]{ 23.5,  4.0 - 5.5*rk },              // tip (raked locking face)
            new[]{ 23.5, -3.0 },                       // tip, -Y side
            new[]{ 18.0, -3.0 }                        // root, ramp back to rim
        }, 26);
        AddProps(m, "绕线轮", "bore Ø6.2 + 2x1 keyway (GB/T 1096); 14deg back-raked ratchet tooth @+X; tendon hole @r14");
        Save(m, file);
    }

    // 37D-class DC gear motor envelope: Ø37 barrel, L70, with protruding Ø6 output
    // shaft. Purchased part; the printed MotorClamp saddles the barrel.
    static void Motor(string file)
    {
        var m = NewPart("Motor");
        var sm = m.SketchManager;
        OpenSketch(m);
        sm.CreateCircleByRadius(0, 0, 0, 18.5 * MM);
        CloseSketch(m);
        MidExtrude(m, 70);
        KeyedEndBoss(m, 35, 3.0, 2.0, 1.0, 20);   // Ø6 keyed output shaft, 20mm proud, 2x1 keyway @+Y
        AddProps(m, "减速电机", "37D gear motor envelope Ø37xL70, Ø6 keyed shaft (GB/T 1096); held by MotorClamp");
        Save(m, file);
    }

    // MG90S-class servo envelope with mounting flange (2 holes) and output hub.
    // Silhouette in XY, extruded 12 mm (servo body width). Output hub at +Y top.
    static void Servo(string file)
    {
        var m = NewPart("Servo");
        var sm = m.SketchManager;
        OpenSketch(m);
        // single closed silhouette: body 23 wide, flange ears at y~21, hub stub on top.
        Poly(sm, new[] {
            new[]{ 11.5,  0.0}, new[]{ 11.5, 18.0}, new[]{ 16.0, 18.0},
            new[]{ 16.0, 23.0}, new[]{ 11.5, 23.0}, new[]{ 11.5, 29.0},
            new[]{  2.5, 29.0}, new[]{  2.5, 34.0}, new[]{ -2.5, 34.0},
            new[]{ -2.5, 29.0}, new[]{-11.5, 29.0}, new[]{-11.5, 23.0},
            new[]{-16.0, 23.0}, new[]{-16.0, 18.0}, new[]{-11.5, 18.0},
            new[]{-11.5,  0.0}
        });
        Hole(sm, 14, 20.5, 3.2);        // flange screw holes
        Hole(sm, -14, 20.5, 3.2);
        CloseSketch(m);
        MidExtrude(m, 12);
        AddProps(m, "舵机", "MG90S envelope 23x12x29, flange holes @+-14, output hub @top");
        Save(m, file);
    }

    static void Washer(string file, double od, double id, double t)
    {
        var m = NewPart("Washer");
        var sm = m.SketchManager;
        OpenSketch(m);
        sm.CreateCircleByRadius(0, 0, 0, (od / 2.0) * MM);
        sm.CreateCircleByRadius(0, 0, 0, (id / 2.0) * MM);
        CloseSketch(m);
        MidExtrude(m, t);
        AddProps(m, "垫片", "anti-friction washer OD" + od + " ID" + id + " t" + t);
        Save(m, file);
    }

    // Socket-head bolt: cylindrical head + threaded shank (modelled smooth).
    static void BoltSocket(string file, double shankR, double shankLen, double headDia, double headLen)
    {
        var m = NewPart(file.Replace(".SLDPRT", ""));
        var sm = m.SketchManager;
        OpenSketch(m);
        sm.CreateCircleByRadius(0, 0, 0, (headDia / 2.0) * MM);  // head at z=0 layer
        CloseSketch(m);
        MidExtrude(m, headLen);
        EndBoss(m, headLen / 2.0, shankR, shankLen);             // shank out +Z
        AddProps(m, "内六角螺栓", "M3 SHCS head Ø" + headDia + " shank Ø" + (shankR * 2) + " L" + shankLen);
        Save(m, file);
    }

    static void HexNut(string file, double acrossFlats, double t, double bore)
    {
        var m = NewPart("M3_Nut");
        var sm = m.SketchManager;
        OpenSketch(m);
        double circumR = (acrossFlats / 2.0) / Math.Cos(Math.PI / 6.0); // flats->circumradius
        sm.CreatePolygon(0, 0, 0, circumR * MM, 0, 0, 6, true);
        sm.CreateCircleByRadius(0, 0, 0, (bore / 2.0) * MM);
        CloseSketch(m);
        MidExtrude(m, t);
        AddProps(m, "六角螺母", "M3 hex nut AF" + acrossFlats + " t" + t + " bore" + bore);
        Save(m, file);
    }

    // U-saddle clamp that the Ø37 motor barrel slides into; bolts to the deck.
    static void MotorClamp(string file)
    {
        var m = NewPart("MotorClamp");
        var sm = m.SketchManager;
        OpenSketch(m);
        Rect(sm, 0, 6, 24, 30);          // block 48 wide x 60 tall
        sm.CreateCircleByRadius(0, 6 * MM, 0, 18.7 * MM);  // barrel bore Ø37.4
        Hole(sm, 20, -20, 3.4);          // base mounting holes
        Hole(sm, -20, -20, 3.4);
        CloseSketch(m);
        MidExtrude(m, 12);
        TryFilletAll(m, 2);
        AddProps(m, "电机抱箍", "motor saddle bore Ø37.4, 2x M3 base holes, width 12");
        Save(m, file);
    }

    // Battery + controller pack envelope (2S Li-ion 18650 x2 + ESP32 + driver).
    // Mounted on the -Z plate outer face as the counter-mass for the outboard winch.
    // 2x M3 mounting tabs match the deck. ~70x40x22 box.
    static void BatteryPack(string file)
    {
        var m = NewPart("BatteryPack");
        var sm = m.SketchManager;
        OpenSketch(m);
        Rect(sm, 0, 0, 35, 20);          // 70 x 40 body (single contour)
        CloseSketch(m);
        MidExtrude(m, 22);
        RectBoss(m, 0, 22, 30, 3, 22);   // top mounting tab strip, merged
        TryFilletAll(m, 2);
        AddProps(m, "电池控制包", "2S Li-ion + ESP32 + driver envelope; counter-mass on -Z plate; ~75g; drill 2x M3 in tab");
        Save(m, file);
    }

    // Rubber foot pad / buffer that snaps onto the foot tip for grip + landing damping.
    // Disc with a blind socket that fits over the printed foot end.
    static void FootPad(string file)
    {
        var m = NewPart("FootPad");
        var sm = m.SketchManager;
        OpenSketch(m);
        sm.CreateCircleByRadius(0, 0, 0, 12 * MM);   // pad Ø24
        CloseSketch(m);
        MidExtrude(m, 8);
        AddProps(m, "足垫", "TPU/rubber foot pad Ø24x8 for grip + landing damping; press-fit on foot tip");
        Save(m, file);
    }

    // Winch support bracket: a flat plate in the winch plane (z~121) that carries the
    // latch pivot, servo flange, and motor-shaft clearance, tying the whole outboard
    // drive module rigidly back to the main +Z side plate via 3 standoff posts.
    // Without it the latch/servo/drum just float ~85 mm off the plate. Plate lies in
    // the XY plane like a side plate; assembly places it at z=121.
    static void WinchBracket(string file)
    {
        var m = NewPart("WinchBracket");
        var sm = m.SketchManager;
        // footprint covers winch axis (-70,92), latch pivot (-35,118), servo (-9..-37,104.5)
        double minX = -92, maxX = 2, minY = 70, maxY = 134;
        double cx = (minX + maxX) / 2.0, cy = (minY + maxY) / 2.0;
        double w = maxX - minX, h = maxY - minY;
        OpenSketch(m);
        sm.CreateCenterRectangle(cx * MM, cy * MM, 0, (cx + w / 2.0) * MM, (cy + h / 2.0) * MM, 0);
        // motor shaft / drum hub clearance bore at the winch axis
        sm.CreateCircleByRadius(-70 * MM, 92 * MM, 0, 10 * MM);
        // latch pivot hole (the pawl turns on a pin fixed here)
        sm.CreateCircleByRadius(-35 * MM, 118 * MM, 0, (HOLE_MOVE / 2.0) * MM);
        // servo flange mount holes (match Servo wings at +-14 about its hub @ -23,104.5)
        sm.CreateCircleByRadius(-9 * MM, 104.5 * MM, 0, (3.4 / 2.0) * MM);
        sm.CreateCircleByRadius(-37 * MM, 104.5 * MM, 0, (3.4 / 2.0) * MM);
        // 3 standoff post holes tying back to the main side plate (all within the
        // plate footprint y<=112, x in [-150,165]); triangular layout for rigidity
        foreach (var p in new[] { new[]{-88.0,74.0}, new[]{-2.0,74.0}, new[]{-45.0,108.0} })
            sm.CreateCircleByRadius(p[0] * MM, p[1] * MM, 0, ((PIN_DIA + 0.2) / 2.0) * MM);
        CloseSketch(m);
        MidExtrude(m, 4);
        AddProps(m, "卷扬支架", "outboard winch support plate: latch pivot + servo flange + motor bore + 3 standoff posts to side plate");
        Save(m, file);
    }

    // Standoff post tube (OD x length along Z, through-bore for an M3 tie rod).
    // Ties the winch bracket back to the main side plate.
    static void WinchPost(string file, double length, double od, double bore)
    {
        var m = NewPart(file.Replace(".SLDPRT", ""));
        var sm = m.SketchManager;
        OpenSketch(m);
        sm.CreateCircleByRadius(0, 0, 0, (od / 2.0) * MM);
        sm.CreateCircleByRadius(0, 0, 0, (bore / 2.0) * MM);
        CloseSketch(m);
        MidExtrude(m, length);
        AddProps(m, "卷扬立柱", "standoff post OD" + od + " x" + length + " bore" + bore + "; bracket->side plate");
        Save(m, file);
    }
}
