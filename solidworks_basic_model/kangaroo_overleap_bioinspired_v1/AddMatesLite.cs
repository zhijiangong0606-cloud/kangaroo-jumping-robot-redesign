using System;
using System.Collections.Generic;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Single-pass pin concentric-mate builder.
// Coordinate picking (SelectByID2) can't hit the interior cylinder walls buried in the
// cage, so like FixMatesV6 we select Face2 objects directly. FixMatesV6 crashes because
// it re-traverses all 52 components' faces ONCE PER PIVOT (7x). Here we enumerate every
// Z-axis cylinder face exactly ONCE into a cache, then match per pivot from the cache --
// ~7x less COM load. Per-component try/catch isolates bad geometry; save after each pivot.
class AddMatesLite
{
    const double MM = 0.001;
    static readonly string Root =
        @"C:\Users\Gzj\Desktop\kangaroo_robot_redesign\solidworks_basic_model\kangaroo_overleap_bioinspired_v1";
    static readonly string Asm = Root + @"\Kangaroo_Overleap_BioInspired_Assembly.SLDASM";
    static SldWorks sw;
    static ModelDoc2 model;
    static AssemblyDoc asm;
    static MathUtility mu;

    struct P { public double X, Y; public P(double x, double y){X=x;Y=y;} }
    static readonly P H0=new P(0,0), H1=new P(-94.8,64.4), H2=new P(134.3,38.0);
    static readonly P A=new P(-153.798,64.376), B=new P(-85.791,-20.646), F=new P(6.717,-59.504);
    static readonly P T=new P(-95,2);

    // A cached cylindrical face: world axis (X,Y) along Z, radius r, whether it's a pin/axle.
    class CylFace { public Face2 Face; public double X, Y, R; public bool IsAxle; }

    [STAThread]
    static void Main()
    {
        try { Run(); }
        catch (Exception ex) { Console.WriteLine("FATAL " + ex.GetType().Name + ": " + ex.Message); }
    }

    static void Run()
    {
        sw = (SldWorks)Activator.CreateInstance(Type.GetTypeFromProgID("SldWorks.Application"));
        sw.Visible = true;
        mu = (MathUtility)sw.GetMathUtility();
        int e = 0, w = 0;
        model = (ModelDoc2)sw.OpenDoc6(Asm, (int)swDocumentTypes_e.swDocASSEMBLY,
            (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", ref e, ref w);
        asm = (AssemblyDoc)model;
        Console.WriteLine("opened err=" + e);

        // Ensure ground fixed, everything else floated (idempotent safety).
        object[] comps = (object[])asm.GetComponents(true);
        bool keptGround = false;
        foreach (Component2 c in comps)
        {
            if (c.IsSuppressed()) continue;
            model.ClearSelection2(true);
            c.Select4(false, null, false);
            if (!keptGround && c.Name2.StartsWith("BodyPlate")) { asm.FixComponent(); keptGround = true; }
            else asm.UnfixComponent();
        }
        model.ClearSelection2(true);
        Console.WriteLine("ground fixed=" + keptGround);

        // ---- SINGLE PASS: cache every Z-axis cylindrical face in the assembly ----
        var cache = new List<CylFace>();
        foreach (Component2 c in comps)
        {
            if (c.IsSuppressed()) continue;
            try
            {
                Body2 body = (Body2)c.GetBody();
                if (body == null) continue;
                MathTransform xf = c.Transform2;
                bool isAxle = c.Name2.StartsWith("M3_Bolt") || c.Name2.StartsWith("M3_Axle") || c.Name2.StartsWith("M3_GroovedAxle");
                object[] fcs = (object[])body.GetFaces();
                if (fcs == null) continue;
                foreach (Face2 f in fcs)
                {
                    Surface s = (Surface)f.GetSurface();
                    if (s == null || !s.IsCylinder()) continue;
                    double[] cp = (double[])s.CylinderParams;
                    double[] wp = TPt(xf, cp[0], cp[1], cp[2]);
                    double[] da = TDir(xf, cp[3], cp[4], cp[5]);
                    if (Math.Abs(da[2]) <= Math.Abs(da[0]) + Math.Abs(da[1])) continue;  // axis not Z
                    cache.Add(new CylFace { Face=f, X=wp[0]/MM, Y=wp[1]/MM, R=cp[6]/MM, IsAxle=isAxle });
                }
            }
            catch (Exception ex) { Console.WriteLine("skip " + c.Name2 + ": " + ex.GetType().Name); }
        }
        Console.WriteLine("cached Z-cyl faces=" + cache.Count);

        var names  = new[]{ "H0","H1","H2","A","B","F","T" };
        var pts    = new[]{ H0, H1, H2, A, B, F, T };
        int totalOk = 0, totalFail = 0;
        for (int i = 0; i < pts.Length; i++)
        {
            P p = pts[i];
            // at this pivot: the one axle wall (r 1.3..1.6) + every hole wall (r 1.5..1.8)
            Face2 axle = null;
            var holes = new List<Face2>();
            foreach (var cf in cache)
            {
                double dx = cf.X - p.X, dy = cf.Y - p.Y;
                if (dx*dx + dy*dy > 4.0) continue;                       // within 2mm of pivot
                if (cf.IsAxle && cf.R >= 1.3 && cf.R <= 1.6) { if (axle == null) axle = cf.Face; }
                else if (!cf.IsAxle && cf.R >= 1.5 && cf.R <= 1.8) holes.Add(cf.Face);
            }
            int ok = 0, fail = 0;
            if (axle == null) { Console.WriteLine("pivot " + names[i] + " NO AXLE"); }
            else foreach (Face2 hf in holes)
                if (Concentric(axle, hf)) ok++; else fail++;
            int se = model.SaveAs3(Asm, 0, 2);   // save after each pivot
            Console.WriteLine("pivot " + names[i] + " holes=" + holes.Count + " ok=" + ok + " fail=" + fail + " saveErr=" + se);
            totalOk += ok; totalFail += fail;
        }
        Console.WriteLine("TOTAL MATES ok=" + totalOk + " fail=" + totalFail);
    }

    static double[] TPt(MathTransform xf, double x, double y, double z)
    { var pt=(MathPoint)mu.CreatePoint(new[]{x,y,z}); pt=(MathPoint)pt.MultiplyTransform(xf);
      var a=(double[])pt.ArrayData; return new[]{a[0],a[1],a[2]}; }
    static double[] TDir(MathTransform xf, double x, double y, double z)
    { var v=(MathVector)mu.CreateVector(new[]{x,y,z}); v=(MathVector)v.MultiplyTransform(xf);
      var a=(double[])v.ArrayData; return new[]{a[0],a[1],a[2]}; }

    static bool Concentric(Face2 a, Face2 b)
    {
        try
        {
            model.ClearSelection2(true);
            ((Entity)a).Select4(false, null);
            ((Entity)b).Select4(true, null);
            int err = 0;
            var mate = asm.AddMate5((int)swMateType_e.swMateCONCENTRIC,
                (int)swMateAlign_e.swMateAlignCLOSEST, false, 0,0,0, 0,0, 0,0,0, false, false, 0, out err);
            model.ClearSelection2(true);
            return mate != null;   // err=1 is the known non-fatal over-define warning
        }
        catch { model.ClearSelection2(true); return false; }
    }
}
