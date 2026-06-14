using System;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
class MassProbe {
  [STAThread] static void Main(string[] a){
    var sw=(SldWorks)Activator.CreateInstance(Type.GetTypeFromProgID("SldWorks.Application"));
    sw.Visible=true; int e=0,w=0;
    string dir=@"C:\Users\Gzj\Desktop\kangaroo_robot_redesign\solidworks_basic_model\kangaroo_overleap_bioinspired_v1\parts\";
    string[] files={"BodyPlate_Side","L1_Crank_40","L2_Coupler_120","L3_Thigh_100","L4_Shank_140","L5_Rocker_180","Foot_80","Drum","Latch","MotorClamp","TailRod_210"};
    double rho=1.24e-6; // g/mm3 -> kg? use g: vol(mm3)*1.24e-3. We'll print grams.
    foreach(var f in files){
      var m=(ModelDoc2)sw.OpenDoc6(dir+f+".SLDPRT",(int)swDocumentTypes_e.swDocPART,(int)swOpenDocOptions_e.swOpenDocOptions_Silent,"",ref e,ref w);
      if(m==null){Console.WriteLine(f+" : open fail");continue;}
      var ext=m.Extension; var mp=ext.CreateMassProperty();
      double vol=mp.Volume; // m3
      double grams=vol*1e9*1.24e-3; // m3->mm3 *rho(g/mm3)
      Console.WriteLine(f+" vol="+Math.Round(vol*1e9,0)+"mm3  mass(PLA)="+Math.Round(grams,1)+"g");
      sw.CloseDoc(f);
    }
  }
}
