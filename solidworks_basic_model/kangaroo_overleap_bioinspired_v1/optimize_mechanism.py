"""
P0 fix: re-optimize the 6-bar hind-leg so the FOOT gets a real VERTICAL extension
stroke (a jumping leg, not a horizontal scrub).

Topology is preserved (same closed chain H1-A-B-H0 / B-F-H2, foot off F) so the CAD
structure, Z-layering, shared M3 axles and assembly method all still apply -- only the
link lengths and ground-pivot coordinates change.

Search goal over a crank sub-stroke [theta_lo, theta_hi]:
  * large foot vertical travel (dy) over the stroke   -> can do work to launch
  * foot stays below the body and roughly under it     -> pushes the body up
  * good transmission angle (>= 40 deg) the whole stroke -> no dead point
  * link lengths in a printable, sane range
We score candidates and print the best, then write mechanism_pose_v2.json with the
crouch (stored) and launch (released) poses + the chosen geometry.
"""
import math, json, random

FOOT_LEN = 80.0

def ci(c0, r0, c1, r1):
    x0, y0 = c0; x1, y1 = c1
    d = math.hypot(x1 - x0, y1 - y0)
    if d > r0 + r1 + 1e-9 or d < abs(r0 - r1) - 1e-9 or d == 0:
        return None
    a = (r0*r0 - r1*r1 + d*d) / (2*d)
    h = math.sqrt(max(0.0, r0*r0 - a*a))
    xm = x0 + a*(x1-x0)/d; ym = y0 + a*(y1-y0)/d
    xs = h*(y1-y0)/d; ys = h*(x1-x0)/d
    return ((xm+xs, ym-ys), (xm-xs, ym+ys))

def solve(g, theta_deg):
    H0, H1, H2 = g["H0"], g["H1"], g["H2"]
    L1, L2, L3, L4, L5 = g["L1"], g["L2"], g["L3"], g["L4"], g["L5"]
    th = math.radians(theta_deg)
    A = (H1[0] + L1*math.cos(th), H1[1] + L1*math.sin(th))
    b = ci(A, L2, H0, L3)
    if not b: return None
    B = min(b, key=lambda p: (p[1], -p[0]))
    f = ci(B, L4, H2, L5)
    if not f: return None
    F = min(f, key=lambda p: p[1])
    ang = math.atan2(F[1]-B[1], F[0]-B[0])
    Foot = (F[0] + FOOT_LEN*math.cos(ang), F[1] + FOOT_LEN*math.sin(ang))
    return dict(theta=theta_deg, A=A, B=B, F=F, Foot=Foot)

def ang_at(c, p1, p2):
    v1 = (p1[0]-c[0], p1[1]-c[1]); v2 = (p2[0]-c[0], p2[1]-c[1])
    n1 = math.hypot(*v1); n2 = math.hypot(*v2)
    if n1 == 0 or n2 == 0: return 0.0
    cc = max(-1, min(1, (v1[0]*v2[0]+v1[1]*v2[1])/(n1*n2)))
    return math.degrees(math.acos(cc))

def evaluate(g):
    """Return (score, info) for a geometry, or None if invalid."""
    H0, H2 = g["H0"], g["H2"]
    # sweep crank, collect solvable poses with foot below body and good transmission
    poses = []
    for t10 in range(-1800, 1801, 20):
        t = t10/10.0
        s = solve(g, t)
        if not s: continue
        muB = ang_at(s["B"], s["A"], H0); muB = min(muB, 180-muB)
        muF = ang_at(s["F"], s["B"], H2); muF = min(muF, 180-muF)
        s["muB"] = muB; s["muF"] = muF
        poses.append(s)
    if len(poses) < 12: return None
    # find a contiguous stroke where foot is below y=-60 (under body) and
    # transmission angles stay >= 40 deg, maximizing foot vertical travel.
    best = None
    n = len(poses)
    for i in range(n):
        if poses[i]["Foot"][1] > -40: continue
        ylo = poses[i]["Foot"][1]
        for j in range(i+1, min(i+60, n)):
            seg = poses[i:j+1]
            if any(abs(seg[k+1]["theta"]-seg[k]["theta"]) > 2.5 for k in range(len(seg)-1)):
                break  # discontinuous (passed a dead branch)
            if any(p["muB"] < 40 or p["muF"] < 40 for p in seg):
                break
            # foot must stay BELOW the body the whole stroke (real downward push)
            if any(p["Foot"][1] > -25 for p in seg):
                continue
            fx = [p["Foot"][0] for p in seg]
            xspread = max(fx) - min(fx)
            footy_min = min(p["Foot"][1] for p in seg)
            mu_min = min(min(p["muB"], p["muF"]) for p in seg)
            # leg EXTENDS during release: foot must move DOWN (more negative y) from
            # crouch to launch, i.e. the body is pushed up off the foot.
            vy = seg[0]["Foot"][1] - seg[-1]["Foot"][1]   # >0 means foot goes down
            if vy < 25: continue
            # reward downward stroke + transmission, penalize horizontal scrub
            score = vy * (mu_min/45.0) - 0.4*xspread
            if best is None or score > best[0]:
                best = (score, dict(i=i, j=j, vy=vy, xspread=xspread,
                                    mu_min=mu_min, footy_min=footy_min,
                                    theta_lo=seg[0]["theta"], theta_hi=seg[-1]["theta"],
                                    crouch=seg[0], launch=seg[-1]))
    return best

# --- random search around the original sizes, topology kept ---
import time
random.seed(7)
orig = dict(H0=(0.0,0.0), H1=(-80.0,60.0), H2=(120.0,60.0),
            L1=40, L2=120, L3=100, L4=140, L5=180)
best_overall = None
t_start = time.time()
TIME_BUDGET = 180.0   # seconds
it = 0
while time.time() - t_start < TIME_BUDGET:
    it += 1
    g = dict(
        H0=(0.0,0.0),
        H1=(random.uniform(-95,-55), random.uniform(35,75)),
        H2=(random.uniform(70,135), random.uniform(35,75)),
        L1=random.uniform(30,70),
        L2=random.uniform(90,150),
        L3=random.uniform(70,130),
        L4=random.uniform(100,170),
        L5=random.uniform(130,210),
    )
    r = evaluate(g)
    if r is None: continue
    if best_overall is None or r[0] > best_overall[0]:
        best_overall = (r[0], g, r[1])

print("iterations=%d  elapsed=%.0fs" % (it, time.time()-t_start))
assert best_overall, "no candidate found"
score, g, info = best_overall
print("BEST score=%.1f" % score)
print("geometry:")
for k in ["H1","H2"]:
    print("  %s = (%.1f, %.1f)" % (k, g[k][0], g[k][1]))
for k in ["L1","L2","L3","L4","L5"]:
    print("  %s = %.1f" % (k, g[k]))
print("stroke: theta %.1f -> %.1f deg" % (info["theta_lo"], info["theta_hi"]))
print("foot vertical travel vy = %.1f mm   (was ~2.2 mm)" % info["vy"])
print("foot horizontal spread  = %.1f mm" % info["xspread"])
print("min transmission angle  = %.1f deg" % info["mu_min"])
print("crouch foot = (%.1f, %.1f)" % (info["crouch"]["Foot"][0], info["crouch"]["Foot"][1]))
print("launch foot = (%.1f, %.1f)" % (info["launch"]["Foot"][0], info["launch"]["Foot"][1]))

# round link lengths to clean values and re-verify
def dump(g, info):
    out = {"H0": [0.0,0.0],
           "H1": [round(g["H1"][0],1), round(g["H1"][1],1)],
           "H2": [round(g["H2"][0],1), round(g["H2"][1],1)],
           "links": {k: round(g[k],1) for k in ["L1","L2","L3","L4","L5"]},
           "foot_len": FOOT_LEN,
           "stroke_deg": [round(info["theta_lo"],1), round(info["theta_hi"],1)],
           "foot_vertical_travel_mm": round(info["vy"],1),
           "min_transmission_deg": round(info["mu_min"],1),
           "crouch": {k: [round(info["crouch"][k][0],3), round(info["crouch"][k][1],3)]
                      for k in ["A","B","F","Foot"]},
           "launch": {k: [round(info["launch"][k][0],3), round(info["launch"][k][1],3)]
                      for k in ["A","B","F","Foot"]}}
    return out

with open("mechanism_pose_v2.json","w",encoding="utf-8") as f:
    json.dump(dump(g, info), f, indent=2, ensure_ascii=False)
print("\nwrote mechanism_pose_v2.json")
