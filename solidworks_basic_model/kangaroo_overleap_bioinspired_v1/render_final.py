"""Produce a clean single-view final render from the assembly STL."""
import struct
import numpy as np
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from mpl_toolkits.mplot3d.art3d import Poly3DCollection


def read_stl(path):
    txt = open(path, "r", errors="ignore").read()
    if txt.lstrip().startswith("solid") and "facet normal" in txt:
        tris, cur = [], []
        for line in txt.splitlines():
            line = line.strip()
            if line.startswith("vertex"):
                cur.append([float(x) for x in line.split()[1:4]])
                if len(cur) == 3:
                    tris.append(cur); cur = []
        return np.array(tris)
    with open(path, "rb") as f:
        f.read(80); n = struct.unpack("<I", f.read(4))[0]
        tris = np.zeros((n, 3, 3))
        for i in range(n):
            f.read(12)
            for j in range(3):
                tris[i, j] = struct.unpack("<3f", f.read(12))
            f.read(2)
        return tris


tris = read_stl("assembly_export.STL")
allv = tris.reshape(-1, 3)
ctr = allv.mean(0)
rng = (allv.max(0) - allv.min(0)).max() / 2

fig = plt.figure(figsize=(12, 9))
ax = fig.add_subplot(111, projection="3d")
# simple shading by triangle Z-normal for depth cueing
v0, v1, v2 = tris[:, 0], tris[:, 1], tris[:, 2]
nrm = np.cross(v1 - v0, v2 - v0)
ln = np.linalg.norm(nrm, axis=1, keepdims=True); ln[ln == 0] = 1
nrm = nrm / ln
light = np.array([0.4, 0.5, 0.75]); light = light / np.linalg.norm(light)
shade = 0.55 + 0.45 * np.clip(nrm @ light, 0, 1)
base = np.array([0.42, 0.55, 0.70])
cols = np.clip(shade[:, None] * base, 0, 1)
pc = Poly3DCollection(tris, facecolors=cols, edgecolor="#2c3742", linewidths=0.05)
ax.add_collection3d(pc)
ax.set_xlim(ctr[0] - rng, ctr[0] + rng)
ax.set_ylim(ctr[1] - rng, ctr[1] + rng)
ax.set_zlim(ctr[2] - rng, ctr[2] + rng)
ax.view_init(elev=20, azim=-62)
ax.set_box_aspect((1, 1, 1))
ax.set_axis_off()
ax.set_title("Kangaroo-Inspired Jumping Robot — Bio-Inspired Assembly (Overleap-referenced)",
             fontsize=12)
fig.tight_layout()
fig.savefig("final_render.png", dpi=110, bbox_inches="tight")
print("wrote final_render.png")
