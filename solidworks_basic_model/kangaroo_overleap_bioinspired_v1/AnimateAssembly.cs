using System;
using System.Collections.Generic;
using System.IO;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

// 3D motion animation of the REAL assembly. Opens the saved .SLDASM, suppresses mates so
// component transforms are free, then for each crank angle theta re-solves the v7 6-bar
// (same circle-circle kinematics as the pose solver) and repositions the moving parts:
//   leg links L1..L5 + Foot (both sides), and the moving-pivot hardware (bushings,
//   grooved pins, circlips, foot pads) that follow pivots A/B/F/Foot.
// Everything else (plates, fixed-pivot bearings/bolts, winch module, tail) stays put.
// Captures one BMP per frame into frames/. Does NOT save the assembly (read-only demo).
class AnimateAssembly
{
    const double MM = 0.001;
    static readonly string Root =
        @"C:\Users\Gzj\Desktop\kangaroo_robot_redesign\solidworks_basic_model\kangaroo_overleap_bioinspired_v1";
    static readonly string Asm = Root + @"\Kangaroo_Overleap_BioInspired_Assembly.SLDASM";
    static readonly string FramesDir = Root + @"\frames";
    static SldWorks sw;
    static ModelDoc2 model;
    static AssemblyDoc asm;
    static MathUtility mu;

    struct P { public double X, Y; public P(double x, double y){X=x;Y=y;} }
    // v7 fixed pivots + link lengths
    static readonly P H0=new P(0,0), H1=new P(-94.8,64.4), H2=new P(134.3,38.0);
    const double L1=59.0, L2=108.9, L3=88.2, L4=100.3, L5=160.6, LFOOT=80.0;
    // Z layers (mid-plane of each link)
    const double Z_L1=29, Z_L5=24, Z_L3=19, Z_L2=14, Z_L4=9, Z_FOOT=4;
    // Dual-mode tail: rod pivots about T and swings with the crank phase. At crouch
    // (theta=-180, storing energy) the tail is DOWN (5th-leg ground support); at launch
    // (theta=-96) it swings UP to act as an airborne counterbalance. The Ø44 mass + tip
    // pin track the swung rod tip.
    const double T_X=-95.0, T_Y=2.0, TAIL_LEN=210.0;
    const double TAIL_DOWN_DEG=-153.87, TAIL_UP_DEG=-190.37;
    const double CRANK_CROUCH=-180.0, CRANK_LAUNCH=-96.0;
    static Component2 tailRod, tailMass, tailTipPin;
    static double tailRodZ, tailMassZ, tailTipPinZ;
    // crouch-pose seed for branch continuation
    static P Bprev=new P(-85.791,-20.646), Fprev=new P(6.717,-59.504);

    static List<P[]> intersections(P c0,double r0,P c1,double r1)
    {
        var res=new List<P[]>();
        double dx=c1.X-c0.X, dy=c1.Y-c0.Y, d=Math.Sqrt(dx*dx+dy*dy);
        if(d>r0+r1||d<Math.Abs(r0-r1)||d==0) return res;
        double a=(r0*r0-r1*r1+d*d)/(2*d);
        double h=Math.Sqrt(Math.Max(0,r0*r0-a*a));
        double xm=c0.X+a*dx/d, ym=c0.Y+a*dy/d, rx=-dy*h/d, ry=dx*h/d;
        res.Add(new[]{ new P(xm+rx,ym+ry) }); res.Add(new[]{ new P(xm-rx,ym-ry) });
        return res;
    }
    static P nearest(List<P[]> opts,P prev)
    {
        P best=prev; double bd=1e18;
        foreach(var o in opts){ var p=o[0]; double dd=(p.X-prev.X)*(p.X-prev.X)+(p.Y-prev.Y)*(p.Y-prev.Y); if(dd<bd){bd=dd;best=p;} }
        return best;
    }
    // solve A,B,F,Foot for crank angle theta (deg), continuing from previous branch
    static void Solve(double thetaDeg, out P A, out P B, out P F, out P Foot)
    {
        double th=thetaDeg*Math.PI/180.0;
        A=new P(H1.X+L1*Math.Cos(th), H1.Y+L1*Math.Sin(th));
        B=nearest(intersections(A,L2,H0,L3),Bprev);
        F=nearest(intersections(B,L4,H2,L5),Fprev);
        Bprev=B; Fprev=F;
        double ux=F.X-B.X, uy=F.Y-B.Y, ul=Math.Sqrt(ux*ux+uy*uy);
        Foot=new P(F.X+LFOOT*ux/ul, F.Y+LFOOT*uy/ul);   // foot = colinear extension of shank
    }
    // a leg-link component: which pivots are its a (origin) and b (aim) ends
    class Link { public Component2 C; public Func<Frame,P> A; public Func<Frame,P> B; public double Z; }
    // a follower (symmetric part) that just tracks a pivot
    class Follow { public Component2 C; public Func<Frame,P> Pv; public double Z; }
    struct Frame { public P A, B, F, Foot; }

    [STAThread]
    static void Main()
    {
        try { Run(); }
        catch (Exception ex) { Console.WriteLine(ex.GetType().FullName + ": " + ex.Message); }
    }

    static void Run()
    {
        Directory.CreateDirectory(FramesDir);
        foreach (var f in Directory.GetFiles(FramesDir, "*.bmp")) File.Delete(f);
        sw = (SldWorks)Activator.CreateInstance(Type.GetTypeFromProgID("SldWorks.Application"));
        sw.Visible = true;
        mu = (MathUtility)sw.GetMathUtility();
        int e = 0, w = 0;
        model = (ModelDoc2)sw.OpenDoc6(Asm, (int)swDocumentTypes_e.swDocASSEMBLY,
            (int)swOpenDocOptions_e.swOpenDocOptions_Silent, "", ref e, ref w);
        asm = (AssemblyDoc)model;
        Console.WriteLine("opened err=" + e);

        // Suppress all mates so component transforms move freely (read-only demo, no save).
        int sup = 0;
        Feature feat = (Feature)model.FirstFeature();
        while (feat != null)
        {
            if (feat.GetTypeName2() == "MateGroup")
            {
                Feature s = (Feature)feat.GetFirstSubFeature();
                while (s != null) { try { s.SetSuppression2(0, 1, null); sup++; } catch {} s = (Feature)s.GetNextSubFeature(); }
            }
            feat = (Feature)feat.GetNextFeature();
        }
        Console.WriteLine("suppressed mates=" + sup);

        // crouch-pose pivot anchors (theta=-180) for matching followers to a pivot
        P A0=new P(-153.8,64.4), B0=new P(-85.791,-20.646), F0=new P(6.717,-59.504), Foot0=new P(80.475,-90.485);

        var links = new List<Link>();
        var follows = new List<Follow>();
        object[] comps = (object[])asm.GetComponents(true);
        foreach (Component2 c in comps)
        {
            if (c.IsSuppressed()) continue;
            string nm = c.Name2;
            double[] t = (double[])((MathTransform)c.Transform2).ArrayData;
            double z = t[11] / MM;
            if      (nm.StartsWith("L1_Crank"))   links.Add(new Link{ C=c, A=f=>H1,     B=f=>f.A,    Z=z });
            else if (nm.StartsWith("L2_Coupler")) links.Add(new Link{ C=c, A=f=>f.A,    B=f=>f.B,    Z=z });
            else if (nm.StartsWith("L3_Thigh"))   links.Add(new Link{ C=c, A=f=>H0,     B=f=>f.B,    Z=z });
            else if (nm.StartsWith("L4_Shank"))   links.Add(new Link{ C=c, A=f=>f.B,    B=f=>f.F,    Z=z });
            else if (nm.StartsWith("L5_Rocker"))  links.Add(new Link{ C=c, A=f=>H2,     B=f=>f.F,    Z=z });
            else if (nm.StartsWith("Foot_80"))    links.Add(new Link{ C=c, A=f=>f.F,    B=f=>f.Foot, Z=z });
            else if (nm.StartsWith("Bushing") || nm.StartsWith("M3_GroovedAxle") || nm.StartsWith("Circlip") || nm.StartsWith("FootPad"))
            {
                double cx = t[9]/MM, cy = t[10]/MM;
                Func<Frame,P> pv = NearPivot(cx, cy, A0, B0, F0, Foot0);
                if (pv != null) follows.Add(new Follow{ C=c, Pv=pv, Z=z });
            }
            else if (nm.StartsWith("TailRod"))   { tailRod = c;  tailRodZ = z; }
            else if (nm.StartsWith("TailMass"))  { tailMass = c; tailMassZ = z; }
            else if (nm.StartsWith("M3_Axle_40") && t[9]/MM < -250) { tailTipPin = c; tailTipPinZ = z; }
        }
        Console.WriteLine("animated links=" + links.Count + " followers=" + follows.Count
            + " tail=" + ((tailRod!=null?1:0)+(tailMass!=null?1:0)+(tailTipPin!=null?1:0)) + "/3");

        // Capture the SAME working stroke from several camera angles. Front shows the
        // planar 6-bar fold; Isometric/Dimetric reveal the L/R Z-layer stack + outboard
        // winch; Right is the pure side profile; Top shows lateral spread. SolidWorks
        // standard named views: *Front *Back *Left *Right *Top *Bottom *Isometric
        // *Dimetric *Trimetric.
        // BUGFIX: ShowNamedView2(name, -1) silently no-ops in script context, so all
        // four GIFs came out from the same camera. Pass the swStandardViews_e *integer
        // ID* instead (Front=1, Right=4, Isometric=7, Dimetric=9); the ID overload is
        // what actually rotates the camera. Redraw twice so the new view settles before
        // the zoom is framed.
        string[] viewNames = { "*Front", "*Isometric", "*Right", "*Dimetric" };
        string[] viewTags  = { "front",  "iso",        "right",  "dimetric" };
        int[]    viewIds   = { 1,        7,            4,        9        };

        const double TH0 = -180, TH1 = -96, STEP = 3.0;
        for (int v = 0; v < viewNames.Length; v++)
        {
            model.ShowNamedView2(viewNames[v], viewIds[v]);
            model.GraphicsRedraw2();
            PoseAt(TH1, links, follows);          // pose at launch to frame the zoom
            model.ViewZoomtofit2();
            model.GraphicsRedraw2();
            int idx = 0;
            for (double th = TH0; th <= TH1 + 0.01; th += STEP)
            {
                PoseAt(th, links, follows);
                model.GraphicsRedraw2();
                string path = FramesDir + "\\" + viewTags[v] + "_" + idx.ToString("D3") + ".bmp";
                model.SaveBMP(path, 900, 680);
                idx++;
            }
            Console.WriteLine("view " + viewTags[v] + " frames=" + idx);
        }
        Console.WriteLine("DONE dir=" + FramesDir);
    }

    static Func<Frame,P> NearPivot(double x, double y, P A0, P B0, P F0, P Foot0)
    {
        double dA=D2(x,y,A0), dB=D2(x,y,B0), dF=D2(x,y,F0), dFt=D2(x,y,Foot0);
        double m=Math.Min(Math.Min(dA,dB),Math.Min(dF,dFt));
        if (m>9.0) return null;                    // not a moving-pivot follower
        if (m==dA) return f=>f.A;
        if (m==dB) return f=>f.B;
        if (m==dF) return f=>f.F;
        return f=>f.Foot;
    }
    static double D2(double x,double y,P p){ return (x-p.X)*(x-p.X)+(y-p.Y)*(y-p.Y); }

    static void PoseAt(double theta, List<Link> links, List<Follow> follows)
    {
        P A,B,F,Foot; Solve(theta, out A, out B, out F, out Foot);
        var fr = new Frame{ A=A, B=B, F=F, Foot=Foot };
        foreach (var lk in links)
        {
            P a = lk.A(fr), b = lk.B(fr);
            double rz = Math.Atan2(b.Y-a.Y, b.X-a.X);
            SetXf(lk.C, a.X, a.Y, lk.Z, rz);
        }
        foreach (var fo in follows)
        {
            P p = fo.Pv(fr);
            SetXf(fo.C, p.X, p.Y, fo.Z, 0);
        }
        // Tail swing: map crank theta -> tail-rod angle. At crouch (storing energy) the
        // tail is DOWN as a 5th-leg ground support; at launch it swings UP as an airborne
        // counterbalance. Linear blend, clamped to the two slot-limit angles.
        if (tailRod != null)
        {
            double u = (theta - CRANK_CROUCH) / (CRANK_LAUNCH - CRANK_CROUCH);
            if (u < 0) u = 0; else if (u > 1) u = 1;
            double tailDeg = TAIL_DOWN_DEG + u * (TAIL_UP_DEG - TAIL_DOWN_DEG);
            double tr = tailDeg * Math.PI / 180.0;
            SetXf(tailRod, T_X, T_Y, tailRodZ, tr);
            double tipX = T_X + TAIL_LEN * Math.Cos(tr), tipY = T_Y + TAIL_LEN * Math.Sin(tr);
            if (tailMass   != null) SetXf(tailMass,   tipX, tipY, tailMassZ,   0);
            if (tailTipPin != null) SetXf(tailTipPin, tipX, tipY, tailTipPinZ, 0);
        }
    }

    static void SetXf(Component2 c, double x, double y, double z, double rz)
    {
        double cc=Math.Cos(rz), s=Math.Sin(rz);
        double[] d = { cc,s,0, -s,cc,0, 0,0,1, x*MM, y*MM, z*MM, 1, 0,0,0 };
        c.Transform2 = (MathTransform)mu.CreateTransform(d);
    }
}
