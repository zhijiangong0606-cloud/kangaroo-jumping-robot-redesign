using System;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

class MateDiag2
{
    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        ISldWorks app = (ISldWorks)System.Runtime.InteropServices.Marshal.GetActiveObject("SldWorks.Application");
        ModelDoc2 doc = null;
        object docsObj = app.GetDocuments();
        if (docsObj != null)
        {
            object[] docs = (object[])docsObj;
            foreach (object o in docs)
            {
                ModelDoc2 d = (ModelDoc2)o;
                if (d.GetType2() == (int)swDocumentTypes_e.swDocASSEMBLY)
                {
                    Console.WriteLine("ASM doc: " + d.GetTitle());
                    doc = d;
                }
                else Console.WriteLine("other doc: " + d.GetTitle() + " type=" + d.GetType2());
            }
        }
        if (doc == null) { Console.WriteLine("no assembly doc open"); return; }
        AssemblyDoc asm = (AssemblyDoc)doc;

        object[] comps = (object[])asm.GetComponents(false);
        Console.WriteLine("=== COMPONENTS (" + comps.Length + ") ===");
        foreach (object o in comps)
        {
            Component2 c = (Component2)o;
            object[] mates = (object[])c.GetMates();
            int mc = (mates == null) ? 0 : mates.Length;
            bool isFixed = c.IsFixed();
            Console.WriteLine(c.Name2 + " | mates=" + mc + " | fixed=" + isFixed);
        }

        Console.WriteLine("\n=== ALL MATES ===");
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
                    if (m != null) { total++; Console.WriteLine(sub.Name + " | type=" + m.Type + " | suppressed=" + sub.IsSuppressed()); }
                    sub = (Feature)sub.GetNextSubFeature();
                }
            }
            feat = (Feature)feat.GetNextFeature();
        }
        Console.WriteLine("total mates=" + total);
    }
}
