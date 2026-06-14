using System;
using System.Runtime.InteropServices;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

class OpenAssembly
{
    static void Main()
    {
        Type t = Type.GetTypeFromProgID("SldWorks.Application");
        ISldWorks app = (ISldWorks)Activator.CreateInstance(t);
        app.Visible = true;

        string path = System.IO.Path.GetFullPath("Kangaroo_Overleap_BioInspired_Assembly.SLDASM");
        int err = 0;
        int warn = 0;
        ModelDoc2 doc = (ModelDoc2)app.OpenDoc6(
            path,
            (int)swDocumentTypes_e.swDocASSEMBLY,
            (int)swOpenDocOptions_e.swOpenDocOptions_Silent,
            "",
            ref err,
            ref warn);

        if (doc == null)
        {
            Console.WriteLine("open FAILED err=" + err + " warn=" + warn);
            return;
        }

        // Bring to front and fit view
        app.ActivateDoc3(doc.GetTitle(), false, (int)swRebuildOnActivation_e.swDontRebuildActiveDoc, ref err);
        ModelView mv = (ModelView)doc.ActiveView;
        if (mv != null) mv.FrameState = (int)swWindowState_e.swWindowMaximized;
        doc.ShowNamedView2("*Isometric", (int)swStandardViews_e.swIsometricView);
        doc.ViewZoomtofit2();
        app.Visible = true;
        app.FrameState = (int)swWindowState_e.swWindowMaximized;

        Console.WriteLine("opened OK title=" + doc.GetTitle() + " err=" + err + " warn=" + warn);
    }
}
