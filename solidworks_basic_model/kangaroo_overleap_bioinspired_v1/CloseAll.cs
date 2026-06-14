using System;
using SolidWorks.Interop.sldworks;

// Close every document open in the running SolidWorks session WITHOUT saving,
// so the next CreatePrintableParts run can overwrite the .SLDPRT files
// (open docs hold ~$ locks -> SaveAs3 returns saveErr=1).
class CloseAll
{
    [STAThread]
    static void Main()
    {
        var swApp = (SldWorks)Activator.CreateInstance(
            Type.GetTypeFromProgID("SldWorks.Application"));
        var doc = (ModelDoc2)swApp.GetFirstDocument();
        int n = 0;
        while (doc != null)
        {
            var next = (ModelDoc2)doc.GetNext();
            string title = doc.GetTitle();
            n++;
            Console.WriteLine("closing: " + title);
            doc = next;
        }
        // CloseAllDocuments(includeUnsaved=true) discards changes and frees locks.
        swApp.CloseAllDocuments(true);
        Console.WriteLine("CLOSED " + n + " docs");
    }
}
