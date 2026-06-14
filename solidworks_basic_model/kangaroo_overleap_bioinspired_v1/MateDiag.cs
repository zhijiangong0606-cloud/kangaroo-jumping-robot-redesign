using System;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

class MateDiag
{
    static void Main()
    {
        Type t = Type.GetTypeFromProgID("SldWorks.Application");
        ISldWorks app = (ISldWorks)System.Runtime.InteropServices.Marshal.GetActiveObject("SldWorks.Application");
        ModelDoc2 doc = (ModelDoc2)app.ActiveDoc;
        if (doc == null) { Console.WriteLine("no active doc"); return; }
        AssemblyDoc asm = (AssemblyDoc)doc;

        object[] comps = (object[])asm.GetComponents(false);
        Console.WriteLine("=== COMPONENTS (" + comps.Length + ") ===");
        foreach (object o in comps)
        {
            Component2 c = (Component2)o;
            // GetConstraintRelation / count mates touching this comp
            object[] mates = (object[])c.GetMates();
            int mc = (mates == null) ? 0 : mates.Length;
            string state;
            int cs = c.GetSuppression2();
            // under-constrained check via GetConnectedComponents not reliable; use mate count + fixed flag
            bool isFixed = c.IsFixed();
            Console.WriteLine(c.Name2 + " | mates=" + mc + " | fixed=" + isFixed + " | suppr=" + cs);
        }

        Console.WriteLine();
        Console.WriteLine("=== ALL MATES IN ASSEMBLY ===");
        Feature feat = (Feature)doc.FirstFeature();
        int total = 0, err = 0;
        while (feat != null)
        {
            string tn = feat.GetTypeName2();
            if (tn == "MateGroup")
            {
                Feature sub = (Feature)feat.GetFirstSubFeature();
                while (sub != null)
                {
                    Mate2 m = (Mate2)sub.GetSpecificFeature2();
                    if (m != null)
                    {
                        total++;
                        bool suppressed = sub.IsSuppressed();
                        // error state
                        Console.WriteLine(sub.Name + " | type=" + m.Type + " | suppressed=" + suppressed);
                    }
                    sub = (Feature)sub.GetNextSubFeature();
                }
            }
            feat = (Feature)feat.GetNextFeature();
        }
        Console.WriteLine("total mates=" + total);
    }
}
