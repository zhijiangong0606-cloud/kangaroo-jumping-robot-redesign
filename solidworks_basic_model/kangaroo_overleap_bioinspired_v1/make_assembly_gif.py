"""Stitch the multi-view BMP frames (front / iso / right / dimetric) captured from
SolidWorks into:
  - one looping GIF per view  (assembly_motion_<view>.gif)
  - a synchronized 2x2 grid GIF showing all four angles at once (assembly_motion_grid.gif)
Each plays the verified working stroke crouch(theta=-180) -> launch(theta=-96) then
reverses for a seamless loop, with a phase label burned in.
Source frames are the REAL v8 assembly (bearings/bushings/keyed drum)."""
from PIL import Image, ImageDraw, ImageFont
from pathlib import Path
import glob

ROOT = Path(r"C:\Users\Gzj\Desktop\kangaroo_robot_redesign\solidworks_basic_model\kangaroo_overleap_bioinspired_v1")
FR = ROOT / "frames"
VIEWS = [("front", "FRONT  (planar 6-bar fold)"),
         ("iso", "ISOMETRIC  (L/R Z-stack + winch)"),
         ("right", "RIGHT  (side profile)"),
         ("dimetric", "DIMETRIC  (lateral spread)")]

try:
    fbig = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 22)
    fsm = ImageFont.truetype("C:/Windows/Fonts/arial.ttf", 16)
    fgrid = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 18)
except Exception:
    fbig = fsm = fgrid = ImageFont.load_default()


def phase(frac):
    return "CROUCH (energy stored)" if frac < 0.15 else ("LAUNCH (extending)" if frac > 0.7 else "EXTENDING")


def banner(im, title, theta, frac, h=52, tsize=fbig):
    d = ImageDraw.Draw(im)
    d.rectangle([0, 0, im.width, h], fill=(22, 28, 38))
    d.text((14, 6), title, font=tsize, fill=(235, 240, 248))
    d.text((14, h - 22), f"crank theta = {theta:+d} deg     {phase(frac)}", font=fsm, fill=(150, 200, 255))
    return im


def load(view):
    fs = sorted(glob.glob(str(FR / f"{view}_*.bmp")))
    return [Image.open(f).convert("RGB") for f in fs]


# ---- per-view GIFs ----
n = None
view_frames = {}
for tag, title in VIEWS:
    ims = load(tag)
    n = len(ims)
    out = []
    for i, im in enumerate(ims):
        out.append(banner(im.copy(), "Kangaroo hind-leg  -  " + title, -180 + 3 * i, i / max(1, n - 1)))
    view_frames[tag] = out
    seq = out + out[-2:0:-1]
    p = ROOT / f"assembly_motion_{tag}.gif"
    seq[0].save(p, save_all=True, append_images=seq[1:], duration=80, loop=0, optimize=True, disposal=2)
    print("wrote", p.name, f"({len(seq)} frames)")

# ---- synchronized 2x2 grid GIF ----
cell_w, cell_h = 460, 348           # scaled-down per-view cell
pad = 6
grid_w = cell_w * 2 + pad * 3
grid_h = cell_h * 2 + pad * 3 + 30   # +30 for top title strip
grid_seq = []
order = ["front", "iso", "right", "dimetric"]
for i in range(n):
    g = Image.new("RGB", (grid_w, grid_h), (15, 18, 24))
    d = ImageDraw.Draw(g)
    theta = -180 + 3 * i
    d.text((12, 6), f"Kangaroo 1-DOF working stroke  -  4 views  -  crank {theta:+d} deg  ({phase(i/max(1,n-1))})",
           font=fgrid, fill=(180, 215, 255))
    for k, tag in enumerate(order):
        src = view_frames[tag][i].resize((cell_w, cell_h))
        x = pad + (k % 2) * (cell_w + pad)
        y = 30 + pad + (k // 2) * (cell_h + pad)
        g.paste(src, (x, y))
    grid_seq.append(g)

gseq = grid_seq + grid_seq[-2:0:-1]
gp = ROOT / "assembly_motion_grid.gif"
gseq[0].save(gp, save_all=True, append_images=gseq[1:], duration=90, loop=0, optimize=True, disposal=2)
print("wrote", gp.name, f"({len(gseq)} frames, {grid_w}x{grid_h})")
try:
    wp = ROOT / "assembly_motion_grid.webp"
    gseq[0].save(wp, save_all=True, append_images=gseq[1:], duration=90, loop=0, quality=86, method=6)
    print("wrote", wp.name)
except Exception as e:
    print("webp skipped:", e)
