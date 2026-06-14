using System;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Print world-space bounding box (mm) of selected components to verify standard-element
// placement (key in keyway, bearings/bushings coaxial at pivots, etc).
class BoxProbe
{
    const double MM = 0.001;
    static readonly string Asm =
        @"C:\Users\Gzj\Desktop\kangaroo_robot_redesign\solidworks_basic_model\kangaroo_overleap_bioinspired_v1\Kangaroo_Overleap_BioInspired_Assembly.SLDASM";

    [STAThread]
    static void Main()
    {
        try
        {
            var sw = (SldWorks)Activator.CreateInstance(Type.GetTypeFromProgID("SldWorks.Application"));
            sw.Visible = true;
            int e = 0, w = 0;
            var model = (ModelDoc2)sw.OpenDoc6(Asm, (int)swDocumentTypes_e.swDocASSEMBLY,
                (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", ref e, ref w);
            var asm = (AssemblyDoc)model;
            string[] want = { "Key_2x2x8", "Bearing_623", "Bushing_0604", "Drum", "Motor", "Circlip_E3", "TailRod", "TailMass", "M3_Axle_80" };
            object[] comps = (object[])asm.GetComponents(true);
            foreach (Component2 c in comps)
            {
                if (c.IsSuppressed()) continue;
                string nm = c.Name2;
                bool hit = false; foreach (var k in want) if (nm.StartsWith(k)) { hit = true; break; }
                if (!hit) continue;
                Body2 b = (Body2)c.GetBody();
                if (b == null) { Console.WriteLine(nm + " : NO BODY"); continue; }
                double[] bb = (double[])c.GetBox(false, false);
                if (bb == null) { Console.WriteLine(nm + " : NO BOX"); continue; }
                Console.WriteLine(nm +
                    "  X[" + R(bb[0]) + "," + R(bb[3]) + "]" +
                    " Y[" + R(bb[1]) + "," + R(bb[4]) + "]" +
                    " Z[" + R(bb[2]) + "," + R(bb[5]) + "]");
            }
        }
        catch (Exception ex) { Console.WriteLine(ex.GetType().FullName + ": " + ex.Message); }
    }
    static string R(double m) { return Math.Round(m / MM, 1).ToString(); }
}
