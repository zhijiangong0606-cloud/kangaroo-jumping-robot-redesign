using System;
using System.IO;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Stage 1c (v8 - STANDARD MACHINE ELEMENTS):
// Adds GB/ISO standard machine elements so the robot uses real designed parts instead
// of plain printed rods running in plastic holes:
//   Bearing_623     GB/T 276  deep-groove ball bearing 3x10x4  (fixed pivots H0/H1/H2)
//   Bushing_0604    GB/T      sleeve bushing OD6 bore3.2 L4     (moving pivots A/B/F)
//   Key_2x2x8       GB/T 1096 flat key 2x2x8                   (drum <-> motor shaft)
//   Circlip_E3      GB/T 896  E-ring for d3 shaft              (axial retention)
//   SpringWasher_M3 GB/T 93   spring lock washer M3            (anti-loosening)
//   Washer_M3       GB/T 97   plain washer M3                  (load spread)
// Convention identical to the other generators: sketch on Front Plane (XY=side view),
// extrude MIDPLANE on Z so every part is symmetric about its own z=0.
class CreateStandardParts
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

        Bearing623("Bearing_623.SLDPRT");
        Bushing("Bushing_0604.SLDPRT", 6.0, 3.2, 4.0);
        Key("Key_2x2x8.SLDPRT", 2.0, 2.0, 8.0);
        Circlip("Circlip_E3.SLDPRT");
        SpringWasher("SpringWasher_M3.SLDPRT");
        PlainWasher("Washer_M3.SLDPRT");

        Console.WriteLine("STANDARD PARTS DONE");
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
        sm.InsertSketch(true); sm.AddToDB = true; sm.DisplayWhenAdded = false;
    }
    static void CloseSketch(ModelDoc2 m)
    {
        var sm = m.SketchManager;
        sm.AddToDB = false; sm.DisplayWhenAdded = true; sm.InsertSketch(true);
    }
    static void MidExtrude(ModelDoc2 m, double depthMm)
    {
        m.FeatureManager.FeatureExtrusion2(true, false, false,
            (int)swEndConditions_e.swEndCondMidPlane, 0,
            depthMm * MM, 0, false, false, false, false, 0, 0,
            false, false, false, false, true, true, true, 0, 0, false);
    }
    static void Circle(ISketchManager sm, double x, double y, double r)
    {
        sm.CreateCircleByRadius(x * MM, y * MM, 0, r * MM);
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

    // ---------------- GB standard parts ----------------

    // GB/T 276 deep-groove ball bearing 623: bore 3, OD 10, width 4. Modelled as the
    // two races (outer ring + inner ring) separated by the ball-track gap -- reads as a
    // real bearing in section, gives a true OD face to mate the link hub and a bore face
    // to mate the shaft. (Balls/cage omitted; standard purchased part envelope.)
    static void Bearing623(string file)
    {
        var m = NewPart("Bearing_623");
        var sm = m.SketchManager;
        // outer race: annulus 4..5 (one robust 2-circle profile)
        OpenSketch(m);
        Circle(sm, 0, 0, 5.0);
        Circle(sm, 0, 0, 4.0);
        CloseSketch(m);
        MidExtrude(m, 4);
        // inner race: annulus 1.5..2.5 as a second (non-merged) body in the same part,
        // sketched again on the Front Plane. Separate body = the ball-track gap reads in
        // section, exactly like a real bearing.
        m.ClearSelection2(true);
        m.Extension.SelectByID2("Front Plane", "PLANE", 0, 0, 0, false, 0, null, 0);
        sm.InsertSketch(true); sm.AddToDB = true; sm.DisplayWhenAdded = false;
        Circle(sm, 0, 0, 2.5);
        Circle(sm, 0, 0, 1.5);
        sm.AddToDB = false; sm.DisplayWhenAdded = true; sm.InsertSketch(true);
        m.FeatureManager.FeatureExtrusion2(true, false, false,
            (int)swEndConditions_e.swEndCondMidPlane, 0, 4 * MM, 0,
            false, false, false, false, 0, 0,
            false, false, false, false, true, true, true, 0, 0, false);
        AddProps(m, "深沟球轴承 623", "GB/T 276 deep-groove ball bearing d3 D10 B4; fixed pivots H0/H1/H2");
        Save(m, file);
    }

    // Plain sleeve bushing (flangeless): OD x bore x length. Pressed into the link hub;
    // the shared Ø3 pin runs through it with a running fit. Flangeless on purpose -- a
    // flange would eat the 1 mm inter-layer Z gap of the verified stack, so the sleeve
    // sits flush in the 4 mm link.
    static void Bushing(string file, double od, double bore, double len)
    {
        var m = NewPart("Bushing_0604");
        var sm = m.SketchManager;
        OpenSketch(m);
        Circle(sm, 0, 0, od / 2.0);
        Circle(sm, 0, 0, bore / 2.0);
        CloseSketch(m);
        MidExtrude(m, len);
        AddProps(m, "含油滑动衬套", "sleeve bushing OD" + od + " bore" + bore + " L" + len + "; moving pivots A/B/F");
        Save(m, file);
    }

    // GB/T 1096 flat key: width b (X) x height h (Y) x length L (along shaft = Z).
    // Centred at origin; placed at the shaft/hub interface in the assembly. The key
    // length runs ALONG the shaft axis so it seats in the axial keyways of drum + shaft.
    static void Key(string file, double b, double h, double L)
    {
        var m = NewPart("Key_2x2x8");
        var sm = m.SketchManager;
        double hx = b / 2.0, hy = h / 2.0;
        OpenSketch(m);
        // explicit closed 4-line loop (robust for small sections under AddToDB)
        sm.CreateLine(-hx * MM, -hy * MM, 0,  hx * MM, -hy * MM, 0);
        sm.CreateLine( hx * MM, -hy * MM, 0,  hx * MM,  hy * MM, 0);
        sm.CreateLine( hx * MM,  hy * MM, 0, -hx * MM,  hy * MM, 0);
        sm.CreateLine(-hx * MM,  hy * MM, 0, -hx * MM, -hy * MM, 0);
        CloseSketch(m);
        MidExtrude(m, L);   // length L along Z (the shaft axis)
        AddProps(m, "平键 2x2x8", "GB/T 1096 flat key b" + b + " h" + h + " L" + L + "; drum<->motor shaft torque");
        Save(m, file);
    }

    // GB/T 896 E-ring for a d3 shaft groove. Open C-clip: single closed contour =
    // outer arc (mouth gap at +X) + inner arc + two end lines. Snaps into the pin groove
    // to retain the link stack axially.
    static void Circlip(string file)
    {
        var m = NewPart("Circlip_E3");
        var sm = m.SketchManager;
        double ro = 3.5, ri = 1.2;             // OD 7, sits in a Ø2.4 groove
        double a = 25 * Math.PI / 180.0;       // half mouth angle
        double oxT = ro * Math.Cos(a), oyT = ro * Math.Sin(a);
        double ixT = ri * Math.Cos(a), iyT = ri * Math.Sin(a);
        OpenSketch(m);
        // outer arc from +a CCW round to -a (the long way, leaving the +X mouth open)
        sm.CreateArc(0, 0, 0, oxT * MM, oyT * MM, 0, oxT * MM, -oyT * MM, 0, 1);
        sm.CreateLine(oxT * MM, -oyT * MM, 0, ixT * MM, -iyT * MM, 0);   // bottom end
        sm.CreateArc(0, 0, 0, ixT * MM, -iyT * MM, 0, ixT * MM, iyT * MM, 0, -1); // inner arc back
        sm.CreateLine(ixT * MM, iyT * MM, 0, oxT * MM, oyT * MM, 0);     // top end
        CloseSketch(m);
        MidExtrude(m, 0.4);
        AddProps(m, "轴用弹性挡圈 E3", "GB/T 896 E-ring for d3 shaft groove; axial retention of moving pivots");
        Save(m, file);
    }

    // GB/T 93 spring lock washer M3: split helical ring, modelled as a thin split annulus.
    static void SpringWasher(string file)
    {
        var m = NewPart("SpringWasher_M3");
        var sm = m.SketchManager;
        double ro = 3.1, ri = 1.6;
        double a = 12 * Math.PI / 180.0;
        double oxT = ro * Math.Cos(a), oyT = ro * Math.Sin(a);
        double ixT = ri * Math.Cos(a), iyT = ri * Math.Sin(a);
        OpenSketch(m);
        sm.CreateArc(0, 0, 0, oxT * MM, oyT * MM, 0, oxT * MM, -oyT * MM, 0, 1);
        sm.CreateLine(oxT * MM, -oyT * MM, 0, ixT * MM, -iyT * MM, 0);
        sm.CreateArc(0, 0, 0, ixT * MM, -iyT * MM, 0, ixT * MM, iyT * MM, 0, -1);
        sm.CreateLine(ixT * MM, iyT * MM, 0, oxT * MM, oyT * MM, 0);
        CloseSketch(m);
        MidExtrude(m, 0.8);
        AddProps(m, "弹簧垫圈 M3", "GB/T 93 spring lock washer M3; anti-loosening under bolt head/nut");
        Save(m, file);
    }

    // GB/T 97 plain washer M3: flat annulus OD7 ID3.2 t0.5.
    static void PlainWasher(string file)
    {
        var m = NewPart("Washer_M3");
        var sm = m.SketchManager;
        OpenSketch(m);
        Circle(sm, 0, 0, 3.5);
        Circle(sm, 0, 0, 1.65);
        CloseSketch(m);
        MidExtrude(m, 0.5);
        AddProps(m, "平垫圈 M3", "GB/T 97 plain washer M3 OD7 ID3.4 t0.5; spreads bolt load");
        Save(m, file);
    }
}
