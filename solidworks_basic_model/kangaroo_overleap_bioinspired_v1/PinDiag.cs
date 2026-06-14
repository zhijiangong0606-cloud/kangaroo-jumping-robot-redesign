using System;
using System.IO;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

class PinDiag
{
    const double MM = 0.001;
    static readonly string OutDir =
        @"C:\Users\Gzj\Desktop\kangaroo_robot_redesign\solidworks_basic_model\kangaroo_overleap_bioinspired_v1\parts";
    static readonly string Tpl =
        @"C:\ProgramData\SOLIDWORKS\SOLIDWORKS 2026\templates\gb_part.prtdot";
    static SldWorks sw;

    [STAThread]
    static void Main()
    {
        sw = (SldWorks)Activator.CreateInstance(Type.GetTypeFromProgID("SldWorks.Application"));
        sw.Visible = true;
        Try("diag_r150_top.SLDPRT", 1.5, 90, true);
        Try("diag_r150_front.SLDPRT", 1.5, 90, false);
        Try("diag_r200_front.SLDPRT", 2.0, 90, false);
        TryC("diag_createcircle.SLDPRT", 1.5, 90);
        TryOff("diag_offorigin.SLDPRT", 1.5, 90);
        TryDB("diag_adddb.SLDPRT", 1.5, 90);
    }

    static void TryDB(string file, double rad, double depth)
    {
        var m = (ModelDoc2)sw.NewDocument(Tpl, 0, 0, 0);
        m.SetTitle2(file.Replace(".SLDPRT",""));
        if (!m.Extension.SelectByID2("Front Plane", "PLANE", 0,0,0,false,0,null,0))
            m.Extension.SelectByID2("Plane1", "PLANE", 0,0,0,false,0,null,0);
        var sm = (SketchManager)m.SketchManager;
        sm.InsertSketch(true);
        bool oldDB = sm.AddToDB; bool oldDisp = sm.DisplayWhenAdded;
        sm.AddToDB = true; sm.DisplayWhenAdded = false;
        object c = sm.CreateCircleByRadius(0, 0, 0, rad * MM);
        sm.AddToDB = oldDB; sm.DisplayWhenAdded = oldDisp;
        Console.WriteLine(file + " circleNull=" + (c == null));
        sm.InsertSketch(true);
        m.FeatureManager.FeatureExtrusion2(true, false, false,
            (int)swEndConditions_e.swEndCondMidPlane, 0, depth * MM, 0,
            false, false, false, false, 0, 0, false, false, false, false,
            true, true, true, 0, 0, false);
        m.ForceRebuild3(false);
        var b = (object[])((PartDoc)m).GetBodies2((int)swBodyType_e.swSolidBody, false);
        Console.WriteLine("  bodies=" + (b == null ? 0 : b.Length));
        sw.CloseDoc(m.GetTitle());
    }

    static void TryC(string file, double rad, double depth)
    {
        var m = (ModelDoc2)sw.NewDocument(Tpl, 0, 0, 0);
        m.SetTitle2(file.Replace(".SLDPRT",""));
        if (!m.Extension.SelectByID2("Front Plane", "PLANE", 0,0,0,false,0,null,0))
            m.Extension.SelectByID2("Plane1", "PLANE", 0,0,0,false,0,null,0);
        var sm = (SketchManager)m.SketchManager;
        sm.InsertSketch(true);
        object c = sm.CreateCircle(0, 0, 0, rad * MM, 0, 0);
        Console.WriteLine(file + " circleNull=" + (c == null));
        sm.InsertSketch(true);
        m.FeatureManager.FeatureExtrusion2(true, false, false,
            (int)swEndConditions_e.swEndCondMidPlane, 0, depth * MM, 0,
            false, false, false, false, 0, 0, false, false, false, false,
            true, true, true, 0, 0, false);
        m.ForceRebuild3(false);
        var b = (object[])((PartDoc)m).GetBodies2((int)swBodyType_e.swSolidBody, false);
        Console.WriteLine("  bodies=" + (b == null ? 0 : b.Length));
        sw.CloseDoc(m.GetTitle());
    }

    static void TryOff(string file, double rad, double depth)
    {
        var m = (ModelDoc2)sw.NewDocument(Tpl, 0, 0, 0);
        m.SetTitle2(file.Replace(".SLDPRT",""));
        if (!m.Extension.SelectByID2("Front Plane", "PLANE", 0,0,0,false,0,null,0))
            m.Extension.SelectByID2("Plane1", "PLANE", 0,0,0,false,0,null,0);
        var sm = (SketchManager)m.SketchManager;
        sm.InsertSketch(true);
        object c = sm.CreateCircleByRadius(0.05, 0.05, 0, rad * MM);
        Console.WriteLine(file + " circleNull=" + (c == null));
        sm.InsertSketch(true);
        m.FeatureManager.FeatureExtrusion2(true, false, false,
            (int)swEndConditions_e.swEndCondMidPlane, 0, depth * MM, 0,
            false, false, false, false, 0, 0, false, false, false, false,
            true, true, true, 0, 0, false);
        m.ForceRebuild3(false);
        var b = (object[])((PartDoc)m).GetBodies2((int)swBodyType_e.swSolidBody, false);
        Console.WriteLine("  bodies=" + (b == null ? 0 : b.Length));
        sw.CloseDoc(m.GetTitle());
    }

    static void Try(string file, double rad, double depth, bool topPlane)
    {
        var m = (ModelDoc2)sw.NewDocument(Tpl, 0, 0, 0);
        m.SetTitle2(file.Replace(".SLDPRT",""));
        string plane = topPlane ? "Top Plane" : "Front Plane";
        bool sel = m.Extension.SelectByID2(plane, "PLANE", 0, 0, 0, false, 0, null, 0);
        if (!sel) m.Extension.SelectByID2(topPlane ? "Plane3" : "Plane1", "PLANE", 0, 0, 0, false, 0, null, 0);
        var sm = (SketchManager)m.SketchManager;
        sm.InsertSketch(true);
        object c = sm.CreateCircleByRadius(0, 0, 0, rad * MM);
        Console.WriteLine(file + " circleNull=" + (c == null));
        sm.InsertSketch(true);
        m.FeatureManager.FeatureExtrusion2(true, false, false,
            (int)swEndConditions_e.swEndCondMidPlane, 0, depth * MM, 0,
            false, false, false, false, 0, 0, false, false, false, false,
            true, true, true, 0, 0, false);
        m.ForceRebuild3(false);
        var b = (object[])((PartDoc)m).GetBodies2((int)swBodyType_e.swSolidBody, false);
        Console.WriteLine("  bodies=" + (b == null ? 0 : b.Length));
        sw.CloseDoc(m.GetTitle());
    }
}
