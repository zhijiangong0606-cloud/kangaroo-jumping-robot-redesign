"""Render the exported assembly STL from several angles to verify 3D geometry."""
import struct
import numpy as np
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from mpl_toolkits.mplot3d.art3d import Poly3DCollection

STL = "assembly_export.STL"


def read_stl(path):
    with open(path, "rb") as f:
        head = f.read(5)
        f.seek(0)
        if head == b"solid":
            txt = open(path, "r", errors="ignore").read()
            if "facet normal" in txt:
                tris = []
                cur = []
                for line in txt.splitlines():
                    line = line.strip()
                    if line.startswith("vertex"):
                        cur.append([float(x) for x in line.split()[1:4]])
                        if len(cur) == 3:
                            tris.append(cur)
                            cur = []
                return np.array(tris)
        # binary
        f.read(80)
        n = struct.unpack("<I", f.read(4))[0]
        tris = np.zeros((n, 3, 3))
        for i in range(n):
            f.read(12)
            for j in range(3):
                tris[i, j] = struct.unpack("<3f", f.read(12))
            f.read(2)
        return tris


tris = read_stl(STL)
print("triangles:", len(tris))
allv = tris.reshape(-1, 3)
print("bbox min:", allv.min(0).round(1), "max:", allv.max(0).round(1))
ctr = allv.mean(0)

views = [("isometric", 22, -60), ("right_side", 0, -90),
         ("front", 0, 0), ("top_iso", 45, -45)]

fig = plt.figure(figsize=(16, 12))
for i, (name, el, az) in enumerate(views, 1):
    ax = fig.add_subplot(2, 2, i, projection="3d")
    pc = Poly3DCollection(tris, alpha=1.0, facecolor="#9fb6cc",
                          edgecolor="#33414f", linewidths=0.08)
    ax.add_collection3d(pc)
    rng = (allv.max(0) - allv.min(0)).max() / 2
    ax.set_xlim(ctr[0] - rng, ctr[0] + rng)
    ax.set_ylim(ctr[1] - rng, ctr[1] + rng)
    ax.set_zlim(ctr[2] - rng, ctr[2] + rng)
    ax.view_init(elev=el, azim=az)
    ax.set_title(name)
    ax.set_box_aspect((1, 1, 1))
fig.tight_layout()
fig.savefig("verify_multiview.png", dpi=70)
print("wrote verify_multiview.png")
