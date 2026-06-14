using System;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Probe: open the saved assembly, iterate each component's cylindrical faces, and
// print their axis (root + direction + radius) so we learn the coordinate system
// (part vs assembly) and can match faces to pivots deterministically.
class MateProbe
{
    static readonly string Asm =
        @"C:\Users\Gzj\Desktop\kangaroo_robot_redesign\solidworks_basic_model\kangaroo_overleap_bioinspired_v1\Kangaroo_Overleap_BioInspired_Assembly.SLDASM";

    [STAThread]
    static void Main()
    {
        var sw = (SldWorks)Activator.CreateInstance(Type.GetTypeFromProgID("SldWorks.Application"));
        sw.Visible = true;
        int e = 0, w = 0;
        var model = (ModelDoc2)sw.OpenDoc6(Asm, (int)swDocumentTypes_e.swDocASSEMBLY,
            (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", ref e, ref w);
        var asm = (AssemblyDoc)model;
        object[] comps = (object[])asm.GetComponents(true);
        Console.WriteLine("components=" + (comps == null ? 0 : comps.Length));
        int shown = 0;
        foreach (Component2 comp in comps)
        {
            string nm = comp.Name2;
            if (!(nm.StartsWith("M3_Axle_80") || nm.StartsWith("L3_Thigh") || nm.StartsWith("BodyPlate"))) continue;
            Body2 body = (Body2)comp.GetBody();
            if (body == null) { Console.WriteLine(nm + " : body NULL"); continue; }
            object[] faces = (object[])body.GetFaces();
            int ncyl = 0;
            foreach (Face2 f in faces)
            {
                Surface s = (Surface)f.GetSurface();
                if (s == null || !s.IsCylinder()) continue;
                double[] cp = (double[])s.CylinderParams;
                ncyl++;
                if (ncyl <= 3)
                    Console.WriteLine(string.Format("  {0} cyl root=({1:F1},{2:F1},{3:F1}) dir=({4:F2},{5:F2},{6:F2}) r={7:F2}mm",
                        nm, cp[0]*1000, cp[1]*1000, cp[2]*1000, cp[3], cp[4], cp[5], cp[6]*1000));
            }
            Console.WriteLine(nm + " : cylFaces=" + ncyl);
            if (++shown > 6) break;
        }
        sw.CloseDoc(model.GetTitle());
    }
}
