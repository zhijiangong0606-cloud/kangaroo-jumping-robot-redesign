using System;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;
class MassProbe2 {
  [STAThread] static void Main(){
    var sw=(SldWorks)Activator.CreateInstance(Type.GetTypeFromProgID("SldWorks.Application"));
    sw.Visible=true; int e=0,w=0;
    string dir=@"C:\Users\Gzj\Desktop\kangaroo_robot_redesign\solidworks_basic_model\kangaroo_overleap_bioinspired_v1\parts\";
    // file, count, density g/mm3 (steel 7.85e-3, bronze 8.5e-3, PLA 1.24e-3)
    object[][] items = {
      new object[]{"Bearing_623", 6, 7.85e-3},
      new object[]{"Bushing_0604", 16, 8.5e-3},
      new object[]{"Key_2x2x8", 1, 7.85e-3},
      new object[]{"Circlip_E3", 6, 7.85e-3},
      new object[]{"SpringWasher_M3", 7, 7.85e-3},
      new object[]{"Washer_M3", 7, 7.85e-3},
      new object[]{"M3_Bolt_80", 7, 7.85e-3},
      new object[]{"M3_Nut", 7, 7.85e-3},
      new object[]{"M3_GroovedAxle_80", 3, 7.85e-3},
      new object[]{"M3_Axle_40", 5, 7.85e-3},
    };
    double total=0;
    foreach(var it in items){
      string f=(string)it[0]; int n=(int)it[1]; double rho=(double)it[2];
      var m=(ModelDoc2)sw.OpenDoc6(dir+f+".SLDPRT",(int)swDocumentTypes_e.swDocPART,(int)swOpenDocOptions_e.swOpenDocOptions_Silent,"",ref e,ref w);
      if(m==null){Console.WriteLine(f+" : open fail");continue;}
      var mp=m.Extension.CreateMassProperty();
      double mm3=mp.Volume*1e9;
      double g=mm3*rho;
      total += g*n;
      Console.WriteLine(f+" x"+n+"  vol="+Math.Round(mm3,0)+"mm3  unit="+Math.Round(g,2)+"g  subtotal="+Math.Round(g*n,1)+"g");
      sw.CloseDoc(f);
    }
    Console.WriteLine("METAL HARDWARE TOTAL = "+Math.Round(total,1)+" g");
  }
}
