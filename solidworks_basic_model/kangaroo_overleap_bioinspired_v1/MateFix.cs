using System;
using System.Collections.Generic;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Mate pass: open the saved assembly, find every cylindrical face, transform its
// axis into ASSEMBLY space via the component transform, group faces by world pivot
// (rounded XY), then add a concentric mate between the pin face and each hole face
// at that pivot by selecting the Face2 entities directly. Saves when done.
class MateFix
{
    const double MM = 0.001;
    static readonly string Asm =
        @"C:\Users\Gzj\Desktop\kangaroo_robot_redesign\solidworks_basic_model\kangaroo_overleap_bioinspired_v1\Kangaroo_Overleap_BioInspired_Assembly.SLDASM";
    static SldWorks sw;
    static ModelDoc2 model;
    static AssemblyDoc asm;
    static MathUtility mu;

    class CFace { public Face2 Face; public double X, Y, R; public string Comp; }

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

        // Remove any pre-existing (failed) mates so we start clean.
        DeleteAllMates();

        var faces = CollectCylFaces();
        Console.WriteLine("cyl faces collected=" + faces.Count);

        // Group by world pivot (XY rounded to 1 mm).
        var groups = new Dictionary<string, List<CFace>>();
        foreach (var f in faces)
        {
            string key = Math.Round(f.X) + "," + Math.Round(f.Y);
            if (!groups.ContainsKey(key)) groups[key] = new List<CFace>();
            groups[key].Add(f);
        }

        int ok = 0, fail = 0, pinless = 0;
        foreach (var kv in groups)
        {
            var list = kv.Value;
            CFace pin = null;
            foreach (var f in list) if (Math.Abs(f.R - 1.50) < 0.06) { pin = f; break; }
            if (pin == null) { pinless++; continue; }   // group with no pin (e.g. drum bore)
            foreach (var f in list)
            {
                if (f == pin) continue;
                if (f.R < 1.5) continue;                  // skip non-pivot small cylinders
                bool added = Concentric(pin.Face, f.Face);
                if (added) ok++; else fail++;
            }
        }
        Console.WriteLine("MATES ok=" + ok + " failed=" + fail + " groupsNoPin=" + pinless);

        model.ForceRebuild3(false);
        int se = model.SaveAs3(Asm, 0, 2);
        Console.WriteLine("SAVED err=" + se);
    }

    static void DeleteAllMates()
    {
        try
        {
            var feat = (Feature)model.FirstFeature();
            var toDel = new List<string>();
            while (feat != null)
            {
                if (feat.GetTypeName2() == "MateGroup")
                {
                    var sub = (Feature)feat.GetFirstSubFeature();
                    while (sub != null) { toDel.Add(sub.Name); sub = (Feature)sub.GetNextSubFeature(); }
                }
                feat = (Feature)feat.GetNextFeature();
            }
            model.ClearSelection2(true);
            foreach (var n in toDel)
                model.Extension.SelectByID2(n, "MATE", 0, 0, 0, true, 0, null, 0);
            if (toDel.Count > 0) { model.EditDelete(); Console.WriteLine("deleted mates=" + toDel.Count); }
        }
        catch (Exception ex) { Console.WriteLine("DeleteAllMates: " + ex.Message); }
    }

    static List<CFace> CollectCylFaces()
    {
        var result = new List<CFace>();
        object[] comps = (object[])asm.GetComponents(true);
        foreach (Component2 comp in comps)
        {
            if (comp.IsSuppressed()) continue;
            Body2 body = (Body2)comp.GetBody();
            if (body == null) continue;
            MathTransform xf = comp.Transform2;
            object[] fcs = (object[])body.GetFaces();
            if (fcs == null) continue;
            foreach (Face2 f in fcs)
            {
                Surface s = (Surface)f.GetSurface();
                if (s == null || !s.IsCylinder()) continue;
                double[] cp = (double[])s.CylinderParams;   // local root + dir + radius
                double[] world = TransformPoint(xf, cp[0], cp[1], cp[2]);
                result.Add(new CFace { Face = f, X = world[0]/MM, Y = world[1]/MM, R = cp[6]/MM, Comp = comp.Name2 });
            }
        }
        return result;
    }

    static double[] TransformPoint(MathTransform xf, double x, double y, double z)
    {
        var pt = (MathPoint)mu.CreatePoint(new double[] { x, y, z });
        pt = (MathPoint)pt.MultiplyTransform(xf);
        var a = (double[])pt.ArrayData;
        return new double[] { a[0], a[1], a[2] };
    }

    static bool Concentric(Face2 a, Face2 b)
    {
        try
        {
            model.ClearSelection2(true);
            ((Entity)a).Select4(false, null);
            ((Entity)b).Select4(true, null);
            int err = 0;
            object mate = asm.AddMate5(
                (int)swMateType_e.swMateCONCENTRIC,
                (int)swMateAlign_e.swMateAlignCLOSEST,
                false, 0, 0, 0, 0, 0, 0, 0, 0, false, false, 0, out err);
            model.ClearSelection2(true);
            return mate != null;
        }
        catch { model.ClearSelection2(true); return false; }
    }
}
