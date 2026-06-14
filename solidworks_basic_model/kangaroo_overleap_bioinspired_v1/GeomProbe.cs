using System;
using System.Collections.Generic;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// READ-ONLY geometry probe. For the components that currently have zero mates
// (standoffs, drum, motor, servo, latch, tailmass, tendon), report their world
// position, planar faces (normal + a point), and cylindrical faces (axis pivot
// XY + radius + Z extent). Used to pick exact entities for the new mates.
// Does NOT modify or save the assembly.
class GeomProbe
{
    const double MM = 0.001;
    static readonly string Asm =
        @"C:\Users\Gzj\Desktop\kangaroo_robot_redesign\solidworks_basic_model\kangaroo_overleap_bioinspired_v1\Kangaroo_Overleap_BioInspired_Assembly.SLDASM";
    static SldWorks sw;
    static ModelDoc2 model;
    static AssemblyDoc asm;
    static MathUtility mu;

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

        string[] want = { "Standoff", "Drum", "Motor", "Servo", "Latch", "TailMass", "ElasticTendon", "BodyPlate", "TailRod", "M3_Axle_40" };
        object[] comps = (object[])asm.GetComponents(true);
        foreach (Component2 comp in comps)
        {
            string nm = comp.Name2;
            bool hit = false;
            foreach (var k in want) if (nm.StartsWith(k)) { hit = true; break; }
            if (!hit) continue;
            if (comp.IsSuppressed()) continue;
            Body2 body = (Body2)comp.GetBody();
            if (body == null) { Console.WriteLine(nm + " : NO BODY"); continue; }
            MathTransform xf = comp.Transform2;
            double[] o = TransformPoint(xf, 0, 0, 0);
            Console.WriteLine("=== " + nm + " origin=(" + R(o[0]) + "," + R(o[1]) + "," + R(o[2]) + ") ===");

            object[] fcs = (object[])body.GetFaces();
            int planar = 0, cyl = 0;
            foreach (Face2 f in fcs)
            {
                Surface s = (Surface)f.GetSurface();
                if (s == null) continue;
                if (s.IsCylinder())
                {
                    double[] cp = (double[])s.CylinderParams;
                    double[] wp = TransformPoint(xf, cp[0], cp[1], cp[2]);
                    // axis direction in world
                    double[] da = TransformDir(xf, cp[3], cp[4], cp[5]);
                    Console.WriteLine("  CYL pivotXY=(" + R(wp[0]) + "," + R(wp[1]) + ") r=" + R2(cp[6]) +
                        " axis=(" + R2(da[0]) + "," + R2(da[1]) + "," + R2(da[2]) + ")");
                    cyl++;
                }
                else if (s.IsPlane())
                {
                    double[] pp = (double[])s.PlaneParams;   // normal(3) + root point(3)
                    double[] wn = TransformDir(xf, pp[0], pp[1], pp[2]);
                    double[] wr = TransformPoint(xf, pp[3], pp[4], pp[5]);
                    // only report faces whose normal is mostly Z (candidate mating faces) to limit noise
                    Console.WriteLine("  PLN n=(" + R2(wn[0]) + "," + R2(wn[1]) + "," + R2(wn[2]) +
                        ") pt=(" + R(wr[0]) + "," + R(wr[1]) + "," + R(wr[2]) + ")");
                    planar++;
                }
            }
            Console.WriteLine("  faces planar=" + planar + " cyl=" + cyl);
        }
    }

    static double[] TransformPoint(MathTransform xf, double x, double y, double z)
    {
        var pt = (MathPoint)mu.CreatePoint(new double[] { x, y, z });
        pt = (MathPoint)pt.MultiplyTransform(xf);
        var a = (double[])pt.ArrayData;
        return new double[] { a[0], a[1], a[2] };
    }

    static double[] TransformDir(MathTransform xf, double x, double y, double z)
    {
        var v = (MathVector)mu.CreateVector(new double[] { x, y, z });
        v = (MathVector)v.MultiplyTransform(xf);
        var a = (double[])v.ArrayData;
        return new double[] { a[0], a[1], a[2] };
    }

    static string R(double mWorld) { return Math.Round(mWorld / MM, 1).ToString(); }
    static string R2(double mWorld) { return Math.Round(mWorld / MM, 2).ToString(); }
}
