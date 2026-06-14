"""
Build a clean assembly drawing (装配图) from the known component placements:
- three orthographic views (front XY, top XZ, right ZY) + isometric
- numbered balloons on the front view
- a BOM table keyed to the balloons
Each component is drawn as its projected bounding rectangle (schematic 装配图 style).
"""
import math
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.patches import Rectangle, Circle, FancyArrow
import numpy as np
import matplotlib.font_manager as fm

# Use a Windows CJK font so Chinese labels render.
for _f in ["C:/Windows/Fonts/msyh.ttc", "C:/Windows/Fonts/simhei.ttf",
           "C:/Windows/Fonts/simsun.ttc"]:
    try:
        fm.fontManager.addfont(_f)
    except Exception:
        pass
plt.rcParams["font.sans-serif"] = ["Microsoft YaHei", "SimHei", "SimSun"]
plt.rcParams["axes.unicode_minus"] = False

# ---- component library: local extents (xmin,xmax,ymin,ymax,zmin,zmax) mm ----
def beam(L, H, T): return (-H/2, L+H/2, -H/2, H/2, -T/2, T/2)
def box(lx, ly, lz): return (-lx/2, lx/2, -ly/2, ly/2, -lz/2, lz/2)
def cyl(r, d): return (-r, r, -r, r, -d/2, d/2)

EXT = {
 'Torso': box(248,130,60), 'L1':beam(40,24,16),'L2':beam(120,22,16),
 'L3':beam(100,32,20),'L4':beam(140,30,20),'L5':beam(180,28,18),
 'Foot':beam(80,34,26),'Tail':beam(210,22,18),'Tendon':beam(52,12,12),
 'TailMass':cyl(24,44),'Drum':cyl(20,30),'Motor':box(74,40,40),
 'Servo':box(46,30,42),'Latch':box(50,16,22),'Axle':cyl(2,120),
}

# ---- solved pose ----
H0=(0,0);H1=(-80,60);H2=(120,60);A=(-68.97,21.55);B=(-35.20,-93.60)
F=(102.45,-119.14);Foot=(181.11,-133.74);T=(-95,2);TE=(-273.1,-109.3)
LEGZ=42;TOP=112
def ang(a,b): return math.atan2(b[1]-a[1],b[0]-a[0])

# placed components: (item#, name, key, x, y, z, rot)
PARTS=[
 (1,'Torso_3D','Torso',22,47,0,0),
 (2,'L1_Crank_40','L1',*H1,LEGZ,ang(H1,A)),
 (3,'L2_Coupler_120','L2',*A,LEGZ,ang(A,B)),
 (4,'L3_Thigh_100','L3',*H0,LEGZ,ang(H0,B)),
 (5,'L4_Shank_140','L4',*B,LEGZ,ang(B,F)),
 (6,'L5_Rocker_180','L5',*H2,LEGZ,ang(H2,F)),
 (7,'Foot_80','Foot',*F,LEGZ,ang(F,Foot)),
 (8,'TailRod_210','Tail',*T,0,ang(T,TE)),
 (9,'TailMass','TailMass',*TE,0,0),
 (10,'Motor','Motor',-10,TOP-18,0,0),
 (11,'Servo','Servo',95,TOP-13,0,0),
 (12,'Drum','Drum',-70,TOP-10,0,0),
 (13,'ElasticTendon_52','Tendon',-70,TOP-10,LEGZ,ang((-70,TOP-10),A)),
 (14,'Latch','Latch',-80,TOP-6,0,0),
 (15,'M3_Axle_120','Axle',0,0,0,0),
]
QTY={1:1,2:2,3:2,4:2,5:2,6:2,7:2,8:1,9:1,10:1,11:1,12:1,13:1,14:1,15:7}
MAT={1:'PLA/亚克力 4mm',2:'PLA/铝',3:'PLA/铝',4:'PLA/亚克力',5:'PLA/亚克力',
 6:'PLA/亚克力',7:'PLA+橡胶垫',8:'碳/铝/PLA 杆',9:'钢配重',10:'外购(占位)',
 11:'外购(占位)',12:'PLA/铝',13:'拉簧/橡皮筋',14:'PLA/铝',15:'钢 M3'}

def corners2d(ext, x, y, rot, ax0, ax1):
    """project local box onto a plane given by axis indices, place at (x,y) world (already 2D)."""
    lo=[ext[0],ext[2],ext[4]];hi=[ext[1],ext[3],ext[5]]
    a0=(lo[ax0],hi[ax0]);a1=(lo[ax1],hi[ax1])
    pts=[]
    for u in a0:
        for v in a1:
            pts.append((u,v))
    # rotate only in XY (front view) when ax0,ax1 == 0,1
    out=[]
    for u,v in pts:
        if (ax0,ax1)==(0,1):
            ru=u*math.cos(rot)-v*math.sin(rot)
            rv=u*math.sin(rot)+v*math.cos(rot)
            out.append((x+ru,y+rv))
        else:
            out.append((x+u,y+v))
    xs=[p[0] for p in out];ys=[p[1] for p in out]
    return min(xs),max(xs),min(ys),max(ys)

fig=plt.figure(figsize=(16.5,11.7))  # A3 landscape
fig.suptitle("袋鼠仿生弹跳机器人  装配图 (Assembly Drawing)  —  比例 1:3",fontsize=15,y=0.97)

def draw_view(ax,title,ax0,ax1,world_xy,balloon=False):
    ax.set_title(title,fontsize=11)
    for it,name,key,x,y,z,rot in PARTS:
        ext=EXT[key]
        wx,wy=world_xy(x,y,z)
        x0,x1,y0,y1=corners2d(ext,wx,wy,rot,ax0,ax1)
        fc='#cfe0ee' if key=='Torso' else ('#e8d9b5' if key in('Motor','Servo','Latch','Drum') else '#d6e6d2')
        ax.add_patch(Rectangle((x0,y0),x1-x0,y1-y0,facecolor=fc,edgecolor='#33414d',lw=0.8,alpha=0.9))
        if balloon and z>=0:
            cx,cy=(x0+x1)/2,(y0+y1)/2
            ax.add_patch(Circle((cx,cy),9,facecolor='white',edgecolor='black',lw=0.9,zorder=5))
            ax.text(cx,cy,str(it),ha='center',va='center',fontsize=7,zorder=6)
    ax.set_aspect('equal');ax.autoscale_view()
    ax.grid(True,ls=':',alpha=0.3);ax.tick_params(labelsize=6)

# front: world XY (x,y)
ax1=fig.add_axes([0.05,0.40,0.42,0.50])
draw_view(ax1,"主视图 Front (X-Y)",0,1,lambda x,y,z:(x,y),balloon=True)
# top: world X-Z (x, z)
ax2=fig.add_axes([0.05,0.08,0.42,0.26])
draw_view(ax2,"俯视图 Top (X-Z)",0,2,lambda x,y,z:(x,z))
# right: world Z-Y (z, y)
ax3=fig.add_axes([0.52,0.40,0.20,0.50])
draw_view(ax3,"左视图 Side (Z-Y)",2,1,lambda x,y,z:(z,y))

# BOM table
axb=fig.add_axes([0.52,0.06,0.45,0.30]);axb.axis('off')
rows=[["序号","零件名称","数量","材料"]]
for it,name,key,*_ in PARTS:
    rows.append([str(it),name,str(QTY[it]),MAT[it]])
tbl=axb.table(cellText=rows,loc='center',cellLoc='left',colWidths=[0.1,0.42,0.12,0.36])
tbl.auto_set_font_size(False);tbl.set_fontsize(8);tbl.scale(1,1.35)
for c in range(4): tbl[0,c].set_facecolor('#33414d');tbl[0,c].set_text_props(color='white')
axb.set_title("明细表 (BOM)",fontsize=11,loc='left')

fig.text(0.74,0.92,"机构: 单自由度六杆闭链 (M=1)\n姿态: 蹲伏待发 θ=-74°\n垂直行程≈97mm  尺寸≈250mm级",fontsize=9,
         bbox=dict(boxstyle='round',fc='#f5f5f0',ec='#999'))
fig.savefig("assembly_drawing.png",dpi=120,bbox_inches='tight')
print("wrote assembly_drawing.png")
