using System;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// Open the assembly and count mates in the MateGroup feature tree (self-contained).
class MateCount
{
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
            var doc = (ModelDoc2)sw.OpenDoc6(Asm, (int)swDocumentTypes_e.swDocASSEMBLY,
                (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", ref e, ref w);
            Feature feat = (Feature)doc.FirstFeature();
            int total = 0;
            while (feat != null)
            {
                if (feat.GetTypeName2() == "MateGroup")
                {
                    Feature sub = (Feature)feat.GetFirstSubFeature();
                    while (sub != null)
                    {
                        Mate2 m = (Mate2)sub.GetSpecificFeature2();
                        if (m != null) total++;
                        sub = (Feature)sub.GetNextSubFeature();
                    }
                }
                feat = (Feature)feat.GetNextFeature();
            }
            Console.WriteLine("total mates=" + total);
        }
        catch (Exception ex) { Console.WriteLine(ex.GetType().FullName + ": " + ex.Message); }
    }
}
