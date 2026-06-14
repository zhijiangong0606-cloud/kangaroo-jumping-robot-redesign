using System;
using System.Collections.Generic;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Robust pin/bolt concentric mate builder (v6).
// Coordinate face-picking (SelectByID2) is unreliable at shared pivots with several
// coaxial cylinder walls, so this instead enumerates each component's faces, finds the
// cylindrical face whose world axis passes through the pivot (X,Y) along Z with the
// expected radius, and selects that Face2 object directly for AddMate5. One concentric
// mate is added between the bolt shank and every link/plate hole at each pivot.
class FixMatesV6
{
    const double MM = 0.001;
    static readonly string Asm =
        @"C:\Users\Gzj\Desktop\kangaroo_robot_redesign\solidworks_basic_model\kangaroo_overleap_bioinspired_v1\Kangaroo_Overleap_BioInspired_Assembly.SLDASM";
    static SldWorks sw;
    static ModelDoc2 model;
    static AssemblyDoc asm;
    static MathUtility mu;

    struct P { public double X, Y; public P(double x, double y){X=x;Y=y;} }
    static readonly P H0=new P(0,0), H1=new P(-94.8,64.4), H2=new P(134.3,38.0);
    static readonly P A=new P(-153.798,64.376), B=new P(-85.791,-20.646), F=new P(6.717,-59.504);
    static readonly P T=new P(-95,2);

    [STAThread]
    static void Main()
    {
        try { Run(); }
        catch (Exception ex) { Console.WriteLine(ex.GetType().FullName + ": " + ex.Message); }
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

        object[] comps = (object[])asm.GetComponents(true);
        // Components added via AddComponent5 are FIXED -> concentric mates over-constrain
        // and return err=1. Float everything except the first body plate (ground), so the
        // chain becomes a real constrained-but-movable mechanism.
        int floated = 0;
        bool keptGround = false;
        foreach (Component2 c in comps)
        {
            if (c.IsSuppressed()) continue;
            if (!keptGround && c.Name2.StartsWith("BodyPlate")) { keptGround = true; continue; }
            model.ClearSelection2(true);
            c.Select4(false, null, false);
            asm.UnfixComponent();
            floated++;
        }
        model.ClearSelection2(true);
        Console.WriteLine("floated " + floated + " components (ground=" + keptGround + ")");

        // pivot -> (prefix, count expected) handled generically: at each pivot we mate
        // the bolt shank to every cylindrical hole (r 1.55..1.75) that is coaxial there.
        P[] pivots = { H0, H1, H2, A, B, F, T };
        int ok = 0, fail = 0;
        foreach (P p in pivots)
        {
            Face2 axleFace = null;
            var holeFaces = new List<Face2>();
            foreach (Component2 c in comps)
            {
                if (c.IsSuppressed()) continue;
                Body2 body = (Body2)c.GetBody();
                if (body == null) continue;
                MathTransform xf = c.Transform2;
                string nm = c.Name2;
                bool isAxle = nm.StartsWith("M3_Bolt") || nm.StartsWith("M3_Axle") || nm.StartsWith("M3_GroovedAxle");
                object[] fcs = (object[])body.GetFaces();
                foreach (Face2 f in fcs)
                {
                    Surface s = (Surface)f.GetSurface();
                    if (s == null || !s.IsCylinder()) continue;
                    double[] cp = (double[])s.CylinderParams;
                    double[] wp = TPt(xf, cp[0], cp[1], cp[2]);
                    double[] da = TDir(xf, cp[3], cp[4], cp[5]);
                    bool axisZ = Math.Abs(da[2]) > Math.Abs(da[0]) + Math.Abs(da[1]);
                    if (!axisZ) continue;
                    double dx = wp[0]/MM - p.X, dy = wp[1]/MM - p.Y;
                    if (dx*dx + dy*dy > 4.0) continue;           // within 2 mm of pivot XY
                    double r = cp[6]/MM;
                    if (isAxle && r >= 1.3 && r <= 1.6) { if (axleFace == null) axleFace = f; }
                    else if (!isAxle && r >= 1.5 && r <= 1.8) holeFaces.Add(f);
                }
            }
            if (axleFace == null) { Console.WriteLine("pivot ("+p.X+","+p.Y+") NO AXLE FACE"); continue; }
            foreach (Face2 hf in holeFaces)
            {
                model.ClearSelection2(true);
                ((Entity)axleFace).Select4(false, null);
                ((Entity)hf).Select4(true, null);
                int err = 0;
                var mate = asm.AddMate5((int)swMateType_e.swMateCONCENTRIC,
                    (int)swMateAlign_e.swMateAlignCLOSEST, false, 0,0,0, 0,0, 0,0,0, false, false, 0, out err);
                model.ClearSelection2(true);
                if (mate != null && err == 0) ok++; else { fail++; if (fail <= 3) Console.WriteLine("  mate fail err="+err+" mateNull="+(mate==null)); }
            }
            Console.WriteLine("pivot ("+p.X+","+p.Y+") holes="+holeFaces.Count);
        }
        model.ForceRebuild3(false);
        int se = model.SaveAs3(Asm, 0, 2);
        Console.WriteLine("MATES ok="+ok+" fail="+fail+" saveErr="+se);
    }

    static double[] TPt(MathTransform xf, double x, double y, double z)
    { var pt=(MathPoint)mu.CreatePoint(new[]{x,y,z}); pt=(MathPoint)pt.MultiplyTransform(xf);
      var a=(double[])pt.ArrayData; return new[]{a[0],a[1],a[2]}; }
    static double[] TDir(MathTransform xf, double x, double y, double z)
    { var v=(MathVector)mu.CreateVector(new[]{x,y,z}); v=(MathVector)v.MultiplyTransform(xf);
      var a=(double[])v.ArrayData; return new[]{a[0],a[1],a[2]}; }
}
