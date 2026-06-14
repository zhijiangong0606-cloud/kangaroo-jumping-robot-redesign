using System;
using System.Collections.Generic;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Connectivity audit: for every component, get its world-space bounding box and test
// whether it physically overlaps (shares/touches volume within TOL) at least one other
// component. A component that overlaps nothing is "floating" (unconnected). Also report,
// per component, which others it touches -- so we can confirm pins actually pass through
// holes and parts rest on each other rather than hanging in space.
class ConnAudit
{
    const double MM = 0.001;
    const double TOL = 0.6;  // mm: boxes within 0.6mm count as touching
    static readonly string Asm =
        @"C:\Users\Gzj\Desktop\kangaroo_robot_redesign\solidworks_basic_model\kangaroo_overleap_bioinspired_v1\Kangaroo_Overleap_BioInspired_Assembly.SLDASM";
    static SldWorks sw;
    static ModelDoc2 model;
    static AssemblyDoc asm;

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
        int e = 0, w = 0;
        model = (ModelDoc2)sw.OpenDoc6(Asm, (int)swDocumentTypes_e.swDocASSEMBLY,
            (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", ref e, ref w);
        asm = (AssemblyDoc)model;

        object[] comps = (object[])asm.GetComponents(true);
        var names = new List<string>();
        var boxes = new List<double[]>();   // xmin,ymin,zmin,xmax,ymax,zmax (mm)
        foreach (Component2 c in comps)
        {
            if (c.IsSuppressed()) continue;
            Body2 b = (Body2)c.GetBody();
            if (b == null) continue;
            double[] bb = (double[])c.GetBox(false, false); // world box in meters
            if (bb == null) continue;
            names.Add(c.Name2);
            boxes.Add(new double[] { bb[0]/MM, bb[1]/MM, bb[2]/MM, bb[3]/MM, bb[4]/MM, bb[5]/MM });
        }

        int n = names.Count;
        Console.WriteLine("components with bodies: " + n);
        var floating = new List<string>();
        for (int i = 0; i < n; i++)
        {
            var touch = new List<string>();
            for (int j = 0; j < n; j++)
            {
                if (i == j) continue;
                if (Overlap(boxes[i], boxes[j])) touch.Add(names[j]);
            }
            if (touch.Count == 0)
            {
                floating.Add(names[i]);
                Console.WriteLine("FLOATING: " + names[i] + " touches NOTHING");
            }
            else
            {
                Console.WriteLine(names[i] + "  touches " + touch.Count + ": " + string.Join(", ", touch));
            }
        }
        Console.WriteLine();
        Console.WriteLine(floating.Count == 0
            ? "RESULT: no floating components -- every part overlaps at least one other."
            : "RESULT: " + floating.Count + " FLOATING components: " + string.Join(", ", floating));
    }

    // boxes overlap (or touch within TOL) on all three axes
    static bool Overlap(double[] a, double[] b)
    {
        return (a[0] <= b[3] + TOL && b[0] <= a[3] + TOL) &&
               (a[1] <= b[4] + TOL && b[1] <= a[4] + TOL) &&
               (a[2] <= b[5] + TOL && b[2] <= a[5] + TOL);
    }
}
