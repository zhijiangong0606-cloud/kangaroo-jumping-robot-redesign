using System;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

class OpenTrue3DAssembly
{
    [STAThread]
    static void Main()
    {
        string asmPath = @"C:\Users\Gzj\Desktop\kangaroo_robot_redesign\solidworks_basic_model\KangarooRobot_TRUE_3D_assembly.SLDASM";
        var swType = Type.GetTypeFromProgID("SldWorks.Application");
        var swApp = (SldWorks)Activator.CreateInstance(swType);
        swApp.Visible = true;

        int errors = 0;
        int warnings = 0;
        var model = (ModelDoc2)swApp.OpenDoc6(
            asmPath,
            (int)swDocumentTypes_e.swDocASSEMBLY,
            (int)swOpenDocOptions_e.swOpenDocOptions_Silent,
            "",
            ref errors,
            ref warnings
        );

        if (model == null)
        {
            Console.WriteLine("Failed to open assembly. errors=" + errors + " warnings=" + warnings);
            System.Environment.Exit(1);
        }

        int activateErrors = 0;
        swApp.ActivateDoc3(model.GetTitle(), false, (int)swRebuildOnActivation_e.swRebuildActiveDoc, ref activateErrors);
        model.ShowNamedView2("*Isometric", 7);
        model.ViewZoomtofit2();
        model.ForceRebuild3(false);
        Console.WriteLine("Opened assembly: " + asmPath);
    }
}
