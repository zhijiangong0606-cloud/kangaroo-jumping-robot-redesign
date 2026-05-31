using System;
using System.Reflection;
using System.Runtime.InteropServices;

class T {
  static void Main() {
    try {
      Type t = Type.GetTypeFromProgID("SldWorks.Application.34");
      Console.WriteLine("TYPE=" + (t==null?"null":t.FullName));
      object sw = Activator.CreateInstance(t);
      Console.WriteLine("OBJ=" + sw.GetType().FullName);
      try { t.InvokeMember("Visible", BindingFlags.SetProperty, null, sw, new object[]{true}); Console.WriteLine("visible set"); } catch(Exception e){ Console.WriteLine("visible err " + e.GetBaseException().Message); }
      try { var rev=t.InvokeMember("RevisionNumber", BindingFlags.InvokeMethod, null, sw, new object[]{}); Console.WriteLine("rev="+rev); } catch(Exception e){ Console.WriteLine("rev err "+e.GetBaseException().Message); }
    } catch(Exception e) { Console.WriteLine("ERR=" + e); }
  }
}
