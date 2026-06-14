using System;
using System.Collections.Generic;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Add the MISSING mates to the already-open assembly (attach to running SW).
// 1) Each of the 4 corner standoffs: CONCENTRIC (standoff OD <-> fixed plate-1
//    corner hole) + COINCIDENT (standoff +Z end face <-> plate-1 inner face).
//    -> turns the two side plates + 4 standoffs into a rigid body cage.
// 2) Body-mounted equipment modules (Drum, Motor, Servo, Latch, TailMass,
//    ElasticTendon): FIX in their already-solved positions so they are rigidly
//    attached to the body frame instead of floating with zero mates.
// Operates on the ACTIVE doc and saves it. Parts are not modified -> no lock risk.
class AddBodyMates
{
    const double MM = 0.001;
    static SldWorks sw;
    static ModelDoc2 model;
    static AssemblyDoc asm;
    static MathUtility mu;

    class FInfo
    {
        public Component2 Comp;
        public Face2 Face;
        public bool IsCyl;
        public double R;            // radius (m) for cyl
        public double X, Y;         // world axis XY (mm) for cyl
        public double[] N;          // world normal for plane
        public double PX, PY, PZ;   // world point (mm)
    }

    [STAThread]
    static void Main()
    {
        try { Run(); }
        catch (Exception ex) { Console.WriteLine(ex.GetType().FullName + ": " + ex.Message); }
    }

    static void Run()
    {
        sw = (SldWorks)System.Runtime.InteropServices.Marshal.GetActiveObject("SldWorks.Application");
        model = (ModelDoc2)sw.ActiveDoc;
        if (model == null) { Console.WriteLine("NO ACTIVE DOC"); return; }
        asm = (AssemblyDoc)model;
        mu = (MathUtility)sw.GetMathUtility();

        // ---- locate the FIXED side plate (origin z = +34) and the 4 standoffs ----
        object[] comps = (object[])asm.GetComponents(true);
        Component2 plate1 = null;
        var standoffs = new List<Component2>();
        var auxFix = new List<Component2>();
        string[] auxNames = { "Drum-1", "Motor-1", "Servo-1", "Latch-1", "TailMass-1", "ElasticTendon_52-1" };
        foreach (Component2 c in comps)
        {
            if (c.IsSuppressed()) continue;
            string n = c.Name2;
            if (n.StartsWith("BodyPlate_Side"))
            {
                double[] o = WP(c.Transform2, 0, 0, 0);
                if (o[2] > 0) plate1 = c;     // the +34 plate (fixed)
            }
            else if (n.StartsWith("Standoff_64")) standoffs.Add(c);
            else foreach (var an in auxNames) if (n == an) auxFix.Add(c);
        }
        Console.WriteLine("plate1=" + (plate1 == null ? "NULL" : plate1.Name2) +
            " standoffs=" + standoffs.Count + " auxToFix=" + auxFix.Count);
        if (plate1 == null) { Console.WriteLine("ABORT: no fixed plate"); return; }

        var plateFaces = Collect(plate1);

        int ok = 0, fail = 0;
        foreach (Component2 so in standoffs)
        {
            var sf = Collect(so);
            double[] oo = WP(so.Transform2, 0, 0, 0);
            double cx = Math.Round(oo[0] / MM), cy = Math.Round(oo[1] / MM);

            // standoff OD face (r ~ 4mm) and +Z end plane (z ~ +32mm)
            Face2 odFace = null, topEnd = null;
            foreach (var f in sf)
            {
                if (f.IsCyl && Math.Abs(f.R / MM - 4.0) < 0.5) odFace = f.Face;
                if (!f.IsCyl && Math.Abs(f.PZ - 32.0) < 1.0) topEnd = f.Face;
            }
            // plate-1 corner hole (r ~1.6) at this XY, and inner plane z=32
            Face2 holeFace = null, innerPlane = null;
            foreach (var f in plateFaces)
            {
                if (f.IsCyl && Math.Abs(f.X - cx) < 1.5 && Math.Abs(f.Y - cy) < 1.5) holeFace = f.Face;
                if (!f.IsCyl && Math.Abs(f.PZ - 32.0) < 1.0) innerPlane = f.Face;
            }

            string tag = so.Name2 + "@(" + cx + "," + cy + ")";
            if (odFace != null && holeFace != null)
            { if (Mate(odFace, holeFace, swMateType_e.swMateCONCENTRIC)) { ok++; Console.WriteLine("  CONC " + tag); } else { fail++; Console.WriteLine("  FAIL conc " + tag); } }
            else { fail++; Console.WriteLine("  MISS faces conc " + tag + " od=" + (odFace != null) + " hole=" + (holeFace != null)); }

            if (topEnd != null && innerPlane != null)
            { if (Mate(topEnd, innerPlane, swMateType_e.swMateCOINCIDENT)) { ok++; Console.WriteLine("  COIN " + tag); } else { fail++; Console.WriteLine("  FAIL coin " + tag); } }
            else { fail++; Console.WriteLine("  MISS faces coin " + tag + " top=" + (topEnd != null) + " inner=" + (innerPlane != null)); }
        }
        Console.WriteLine("STANDOFF mates ok=" + ok + " fail=" + fail);

        // ---- Fix the body-mounted equipment modules in place ----
        int fixedN = 0;
        foreach (Component2 c in auxFix)
        {
            try
            {
                model.ClearSelection2(true);
                c.Select4(false, null, false);
                asm.FixComponent();
                model.ClearSelection2(true);
                fixedN++;
                Console.WriteLine("  FIX " + c.Name2);
            }
            catch (Exception ex) { Console.WriteLine("  FIX FAIL " + c.Name2 + ": " + ex.Message); }
        }
        Console.WriteLine("FIXED modules=" + fixedN);

        model.ForceRebuild3(false);
        int se = 0, sw2 = 0;
        bool saved = model.Save3((int)swSaveAsOptions_e.swSaveAsOptions_Silent, ref se, ref sw2);
        Console.WriteLine("SAVE ok=" + saved + " err=" + se + " warn=" + sw2);
    }

    static List<FInfo> Collect(Component2 comp)
    {
        var list = new List<FInfo>();
        Body2 body = (Body2)comp.GetBody();
        if (body == null) return list;
        MathTransform xf = comp.Transform2;
        object[] fcs = (object[])body.GetFaces();
        if (fcs == null) return list;
        foreach (Face2 f in fcs)
        {
            Surface s = (Surface)f.GetSurface();
            if (s == null) continue;
            if (s.IsCylinder())
            {
                double[] cp = (double[])s.CylinderParams;
                double[] wp = WP(xf, cp[0], cp[1], cp[2]);
                list.Add(new FInfo { Comp = comp, Face = f, IsCyl = true, R = cp[6],
                    X = Math.Round(wp[0] / MM), Y = Math.Round(wp[1] / MM) });
            }
            else if (s.IsPlane())
            {
                double[] pp = (double[])s.PlaneParams;
                double[] wn = WV(xf, pp[0], pp[1], pp[2]);
                double[] wr = WP(xf, pp[3], pp[4], pp[5]);
                list.Add(new FInfo { Comp = comp, Face = f, IsCyl = false, N = wn,
                    PX = wr[0] / MM, PY = wr[1] / MM, PZ = wr[2] / MM });
            }
        }
        return list;
    }

    static bool Mate(Face2 a, Face2 b, swMateType_e type)
    {
        try
        {
            model.ClearSelection2(true);
            ((Entity)a).Select4(false, null);
            ((Entity)b).Select4(true, null);
            int err = 0;
            object m = asm.AddMate5((int)type, (int)swMateAlign_e.swMateAlignCLOSEST,
                false, 0, 0, 0, 0, 0, 0, 0, 0, false, false, 0, out err);
            model.ClearSelection2(true);
            return m != null;
        }
        catch { model.ClearSelection2(true); return false; }
    }

    static double[] WP(MathTransform xf, double x, double y, double z)
    {
        var pt = (MathPoint)mu.CreatePoint(new double[] { x, y, z });
        pt = (MathPoint)pt.MultiplyTransform(xf);
        return (double[])pt.ArrayData;
    }
    static double[] WV(MathTransform xf, double x, double y, double z)
    {
        var v = (MathVector)mu.CreateVector(new double[] { x, y, z });
        v = (MathVector)v.MultiplyTransform(xf);
        return (double[])v.ArrayData;
    }
}
