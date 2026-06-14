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

    // v7 OPTIMIZED geometry (foot gets a real ~35 mm vertical extension stroke).
    // Assembled at the CROUCH / stored-energy pose (theta = -180 deg).
    static readonly P H0 = new P(0, 0), H1 = new P(-94.8, 64.4), H2 = new P(134.3, 38.0);
    static readonly P A  = new P(-153.798, 64.376);
    static readonly P B  = new P(-85.791, -20.646);
    static readonly P F  = new P(6.717, -59.504);
    static readonly P Foot = new P(80.475, -90.485);
    static readonly P T  = new P(-95, 2);
    static readonly P TailEnd = new P(-273.1, -109.3);
    // v11 dual-mode tail: the rod pivots about T. TS is the body-fixed stop pin that
    // rides the rod's curved limit slot (28 mm from T). The slot ends set two limits:
    //   DOWN (tail tip on the ground line)  -> rod axis angle TAIL_DOWN_DEG
    //   UP   (tail tip lifted, airborne)    -> rod axis angle TAIL_UP_DEG
    // Mid-swing (TAIL_MID_DEG) is the saved/static pose. Angles are the rod's +X axis
    // direction measured in world degrees (atan2). The 210 mm rod + Ø44 mass are unchanged.
    static readonly P TS = new P(-122.735, -1.84);  // body stop pin, |TS-T| = 28 mm
    const double TAIL_DOWN_DEG = -153.87;  // tip -> ground (Y=-90.5): pentapedal support
    const double TAIL_UP_DEG   = -190.37;  // tip lifted (Y=+40):     hopping counterbalance
    const double TAIL_MID_DEG  = -172.12;  // mid-swing: static saved pose
    const double TAIL_LEN      = 210.0;

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
        // v7 corners pushed out to clear the larger link sweep; must match the
        // BodyPlate standoff holes in CreatePrintableParts.cs.
        double[][] corners = {
            new[]{-135.0,-28.0}, new[]{152.0,-28.0}, new[]{-135.0,98.0}, new[]{152.0,98.0}
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

        // ---- Standard machine elements at the pivots (v8) ----
        // Fixed pivots H0/H1/H2: a 623 ball bearing (Ø10x4) seats in the rotating link
        // hub; its bore rides the fixed pin -> link turns on rolling elements, not on
        // plastic. The single rotating link Z-layer at each fixed pivot:
        //   H0->L3(Z_L3)   H1->L1(Z_L1)   H2->L5(Z_L5)
        P[] fbP = { H0, H1, H2 };
        double[] fbZ = { Z_L3, Z_L1, Z_L5 };
        for (int i = 0; i < fbP.Length; i++)
            foreach (int side in new[]{ +1, -1 })
                Place("Bearing_623.SLDPRT", fbP[i].X, fbP[i].Y, side * fbZ[i], 0, true);

        // Moving pivots A/B/F: a sleeve bushing (Ø6, bore3.2) presses into every link
        // hub there; the shared grooved pin runs through all the bushings.
        P[] mbP = { A, B, F };
        double[][] mbZ = { new[]{ Z_L1, Z_L2 }, new[]{ Z_L2, Z_L3, Z_L4 }, new[]{ Z_L4, Z_L5, Z_FOOT } };
        for (int i = 0; i < mbP.Length; i++)
            foreach (int side in new[]{ +1, -1 })
                foreach (double zl in mbZ[i])
                    Place("Bushing_0604.SLDPRT", mbP[i].X, mbP[i].Y, side * zl, 0, true);

        // ---- Dual-mode tail (hinged at root T, swings between two slot limits) ----
        // The rod pivots about T on a full-width axle, so it is a true revolute joint.
        // A body-fixed stop pin at TS rides the rod's curved limit slot; the slot ends
        // are the DOWN limit (tip on the ground -> 5th-leg support) and UP limit (tip
        // lifted -> airborne counterbalance). Saved at the mid-swing pose. The Ø44 mass
        // and its clamp pin stack on the rod tip exactly as before, now at the swung tip.
        double tmid = TAIL_MID_DEG * Math.PI / 180.0;
        P tipMid = new P(T.X + TAIL_LEN * Math.Cos(tmid), T.Y + TAIL_LEN * Math.Sin(tmid));
        Place("TailRod_210.SLDPRT", T.X, T.Y, Z_TAIL, tmid);          // root at T, swung to mid
        Place("TailMass.SLDPRT", tipMid.X, tipMid.Y, 10, 0);          // disk on +Z face of rod tip
        Place("M3_Axle_40.SLDPRT", tipMid.X, tipMid.Y, 8, 0, true);   // pin: rod tip hole + disk bore

        // ---- Drive / energy module = EXTERNAL WINCH on the RIGHT plate outer face ----
        // The 70 mm motor barrel will not fit inside the 64 mm cage and would foul the
        // leg links, so the whole module mounts OUTBOARD of the +Z plate (outer face
        // z=36). Motor barrel 37..107, its Ø6 shaft 107..127, the winding drum keys on
        // that shaft at z=121, the release pawl pivots and the servo sit in the drum
        // plane so the pawl lip drops on the drum rim tooth. The elastic tendon runs
        // from the drum through a guide hole in the plate to crank A inside the cage.
        const double MX = -70, MY = 92;     // motor / drum / winch axis (X,Y)
        const double Z_MOTOR = 72;          // barrel center -> 37..107 (clears plate)
        const double Z_DRUM  = 121;         // drum center on the shaft (107..127)
        Place("Motor.SLDPRT",  MX, MY, Z_MOTOR, 0, true);     // barrel + shaft along +Z
        Place("MotorClamp.SLDPRT", MX, MY - 6, Z_MOTOR, 0);   // saddle around the barrel
        Place("Drum.SLDPRT",   MX, MY, Z_DRUM, 0, true);      // keyed on shaft, tooth @+X
        // GB/T 1096 flat key in the drum/shaft keyway (both keyways at +Y). Key cross-
        // section 2x2 centred at the shaft surface (r3): centre at y = 3, spanning the
        // shaft slot floor (y2) to the bore keyway floor (y4); length 8 along Z.
        Place("Key_2x2x8.SLDPRT", MX, MY + 3.0, Z_DRUM, 0, true);
        Place("Latch.SLDPRT",  MX + 35, MY + 26, Z_DRUM, 0);  // lip reaches drum rim tooth
        Place("Servo.SLDPRT",  MX + 47, MY - 8, Z_DRUM, 0);   // output hub at the pawl tail
        LinkZ("ElasticTendon_52.SLDPRT", new P(MX, MY + 14), A, Z_DRUM); // drum anchor -> A

        // ---- Winch support bracket + posts: rigidly tie the outboard drive module
        // back to the +Z side plate so the latch/servo/drum are NOT floating. Bracket
        // sits just behind the drum plane (z=128); 3 standoff posts span to the plate;
        // a pin fixes the latch pivot, two pins fix the servo flange to the bracket.
        const double Z_BRK = 128;                          // bracket plane, behind drum
        Place("WinchBracket.SLDPRT", 0, 0, Z_BRK, 0);
        // latch pivot pin: bracket -> latch (-35,118)
        Place("M3_Axle_40.SLDPRT", -35, 118, (Z_DRUM + Z_BRK)/2.0, 0, true);
        // servo flange pins (-9 / -37, 104.5)
        Place("M3_Axle_40.SLDPRT", -9,  104.5, (Z_DRUM + Z_BRK)/2.0, 0, true);
        Place("M3_Axle_40.SLDPRT", -37, 104.5, (Z_DRUM + Z_BRK)/2.0, 0, true);
        // 3 support posts bracket(z128) -> +Z plate(z34): length 94, centered z=81
        foreach (var p in new[] { new[]{-88.0,74.0}, new[]{-2.0,74.0}, new[]{-45.0,108.0} })
            Place("WinchPost_94.SLDPRT", p[0], p[1], (34 + Z_BRK)/2.0, 0, true);

        // ---- Counter-mass: battery+controller pack bolted to the -Z plate OUTER face,
        // opposite the outboard winch, to offset its lateral tipping moment. The pack
        // body (22 thick) sits just outside the plate (plate at z -36..-32) so its inner
        // face contacts the plate; two M3 bolts through the pack tab + plate hold it.
        const double BZ = -47;              // pack center -> z -58..-36, inner face on plate
        Place("BatteryPack.SLDPRT", MX, MY - 20, BZ, 0);
        // 2 mounting bolts through the pack tab and the -Z plate (tab holes at +-28 in X)
        Place("M3_Axle_40.SLDPRT", MX + 28, MY + 2, BZ + 6, 0, true);
        Place("M3_Axle_40.SLDPRT", MX - 28, MY + 2, BZ + 6, 0, true);
        // ---- Foot pads (rubber grip + landing buffer) on each foot tip ----
        foreach (int side in new[] { +1, -1 })
            Place("FootPad.SLDPRT", Foot.X, Foot.Y, side * Z_FOOT, 0, true);

        // ---- Synchronizing axles through both plates + both legs (v8) ----
        // FIXED pivots H0/H1/H2: M3 SHCS (GB/T 70.1) + GB/T 93 spring washer + GB/T 97
        // plain washer under the nut (GB/T 6170), axially clamping the bearing-borne
        // stack. The link still spins freely -- it turns on the 623 bearing, not the pin.
        foreach (var p in new[] { H0, H1, H2 })
        {
            Place("M3_Bolt_80.SLDPRT", p.X, p.Y, -41.5, 0, true);  // head outside -Z plate
            Place("Washer_M3.SLDPRT",       p.X, p.Y, 38.75, 0, true); // plain washer on +Z plate
            Place("SpringWasher_M3.SLDPRT", p.X, p.Y, 39.4,  0, true); // spring washer
            Place("M3_Nut.SLDPRT",          p.X, p.Y, 40.6,  0, true); // nut outside +Z plate
        }
        // MOVING pivots A/B/F: grooved pin + two GB/T 896 E-rings (one just outside each
        // plate) -- the links rotate on their bushings; the circlips only retain the pin
        // axially, so the joint is NOT clamped (would lock the linkage).
        foreach (var p in new[] { A, B, F })
        {
            Place("M3_GroovedAxle_80.SLDPRT", p.X, p.Y, -40, 0, true); // base at -Z, grooves at ∓36
            Place("Circlip_E3.SLDPRT", p.X, p.Y, -36, 0, true);
            Place("Circlip_E3.SLDPRT", p.X, p.Y,  36, 0, true);
        }
        // Tail root axle (full width, through both plates + rod root) = revolute pivot.
        PinZ("M3_Axle_80.SLDPRT", T.X, T.Y);
        // Tail stop pin (full width, through both plates) rides the rod's limit slot.
        PinZ("M3_Axle_80.SLDPRT", TS.X, TS.Y);

        // ---- 4 corner standoff tie-bolts (GB/T 70.1 + GB/T 93 + GB/T 97 + GB/T 6170) ----
        // The corner standoffs were previously unbolted (held only by being trapped). A
        // long M3 now runs through each standoff bore and both plates, clamping the cage.
        foreach (var c in corners)
        {
            Place("M3_Bolt_80.SLDPRT",      c[0], c[1], -41.5, 0, true);
            Place("Washer_M3.SLDPRT",       c[0], c[1], 38.75, 0, true);
            Place("SpringWasher_M3.SLDPRT", c[0], c[1], 39.4,  0, true);
            Place("M3_Nut.SLDPRT",          c[0], c[1], 40.6,  0, true);
        }

        model.ForceRebuild3(false);
        model.ViewZoomtofit2();

        // ---- Add concentric mates: pin <-> each link hole at every pivot ----
        AddPinMates();

        model.ForceRebuild3(false);
        // A previous AnimateAssembly run may have left the OLD assembly file open in SW,
        // which locks it and makes SaveAs silently fail (saveErr=1). Close any open copy
        // of the target file (NOT our freshly-built in-memory doc) before saving.
        string outPath = Path.Combine(Root, "Kangaroo_Overleap_BioInspired_Assembly.SLDASM");
        try { swApp.CloseDoc("Kangaroo_Overleap_BioInspired_Assembly.SLDASM"); } catch {}
        int saveErr = 0, saveWarn = 0;
        bool saved = model.Extension.SaveAs(outPath, 0, 1, null, ref saveErr, ref saveWarn);
        log.Add("ASSEMBLY saved=" + saved + " err=" + saveErr + " warn=" + saveWarn);
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
            // SolidWorks Transform2 applies the 3x3 block as a ROW-vector (v*M), so the
            // rotation block must be stored transposed to rotate by +rz. The earlier
            // { c,-s, s,c } rotated by -rz, mirroring every link's far end in Y.
            double[] d = { c, s, 0, -s, c, 0, 0, 0, 1, x * MM, y * MM, z * MM, 1, 0, 0, 0 };
            comp.Transform2 = (MathTransform)mu.CreateTransform(d);
            placed.Add(comp.Name2);
        }
        log.Add((comp == null ? "FAILED " : "added ") + file + (comp != null ? " ["+comp.Name2+"]" : ""));
    }

    // Concentric mates pin-axis <-> link-hub-axis at every pivot. v8 hole radii:
    //   fixed pivots H0/H1/H2 -> Ø10 bearing seat (r5.0)
    //   moving pivots A/B/F    -> Ø6 bushing seat (r3.0)
    // A concentric mate between any two coaxial cylinders fixes the axis, so picking the
    // link-hub wall + the pin wall is sufficient (the bearing/bushing sit between them).
    const double HOLE_MOVE_R = 3.0;   // moving link bushing-seat hole radius (Ø6)
    const double HUB_FIXED_R = 5.0;   // fixed link bearing-seat hole radius (Ø10)
    const double HOLE_BODY_R = 1.60;  // plate hole radius
    const double PIN_R       = 1.50;  // pin / bolt shank radius

    static void AddPinMates()
    {
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
            double holeR = isFixed[i] ? HUB_FIXED_R : HOLE_MOVE_R;
            foreach (int side in new[]{ +1, -1 })
                foreach (double zl in moving[p])
                {
                    bool added = Concentric(
                        p.X + PIN_R, p.Y, 0,                 // point on pin wall (z=0 inside cage)
                        p.X + holeR, p.Y, side * zl);        // point on link hub wall
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
        // Tail root revolute (tail-rod root hole <-> full-width root axle at T) is added
        // by AddMatesLite, which already includes pivot T and matches Face2 objects by
        // geometry. (The SelectByID2 path below cannot hit the buried cage walls.)
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

