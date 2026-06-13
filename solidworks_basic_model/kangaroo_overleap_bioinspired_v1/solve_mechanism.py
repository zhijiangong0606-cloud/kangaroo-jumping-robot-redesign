"""
Solve the single-side hind-leg closed chain for a valid static stance pose.

Topology (planar, side view in XY):
  Ground pivots: H0=(0,0), H1=(-80,60), H2=(120,60)
    |H0-H1| = 100 mm, |H0-H2| = 134 mm   (match design dimensions)
  L1 crank   H1->A : 40 mm
  L2 coupler A->B  : 120 mm
  L3 thigh   H0->B : 100 mm
  L4 shank   B->F  : 140 mm
  L5 rocker  H2->F : 180 mm
  Foot extends forward-down from F.

We sweep the crank angle, solve B and F by circle-circle intersection,
and select a crouched stance where the foot is well below the body.
"""
import math
import json

H0 = (0.0, 0.0)
H1 = (-80.0, 60.0)
H2 = (120.0, 60.0)
L1, L2, L3, L4, L5 = 40.0, 120.0, 100.0, 140.0, 180.0
FOOT_LEN = 80.0  # F -> foot tip


def dist(p, q):
    return math.hypot(p[0] - q[0], p[1] - q[1])


def circ_intersect(c0, r0, c1, r1):
    """Return the two intersection points of two circles, or None."""
    x0, y0 = c0
    x1, y1 = c1
    d = math.hypot(x1 - x0, y1 - y0)
    if d > r0 + r1 + 1e-9 or d < abs(r0 - r1) - 1e-9 or d == 0:
        return None
    a = (r0 * r0 - r1 * r1 + d * d) / (2 * d)
    h2 = r0 * r0 - a * a
    if h2 < 0:
        h2 = 0.0
    h = math.sqrt(h2)
    xm = x0 + a * (x1 - x0) / d
    ym = y0 + a * (y1 - y0) / d
    xs = h * (y1 - y0) / d
    ys = h * (x1 - x0) / d
    return ((xm + xs, ym - ys), (xm - xs, ym + ys))


def solve(theta_deg):
    th = math.radians(theta_deg)
    A = (H1[0] + L1 * math.cos(th), H1[1] + L1 * math.sin(th))
    bsol = circ_intersect(A, L2, H0, L3)
    if not bsol:
        return None
    # choose B that is lower / to the right (knee-out stance)
    B = min(bsol, key=lambda p: (p[1], -p[0]))
    fsol = circ_intersect(B, L4, H2, L5)
    if not fsol:
        return None
    # choose F that is lowest (foot drops below the frame)
    F = min(fsol, key=lambda p: p[1])
    # foot tip points forward-down from F, biased away from H2
    ang = math.atan2(F[1] - B[1], F[0] - B[0])
    Foot = (F[0] + FOOT_LEN * math.cos(ang), F[1] + FOOT_LEN * math.sin(ang))
    return dict(theta=theta_deg, A=A, B=B, F=F, Foot=Foot)


def verify(s):
    checks = {
        "H1-A (L1=40)": (dist(H1, s["A"]), 40.0),
        "A-B (L2=120)": (dist(s["A"], s["B"]), 120.0),
        "H0-B (L3=100)": (dist(H0, s["B"]), 100.0),
        "B-F (L4=140)": (dist(s["B"], s["F"]), 140.0),
        "H2-F (L5=180)": (dist(H2, s["F"]), 180.0),
    }
    ok = all(abs(a - b) < 1e-6 for a, b in checks.values())
    return ok, checks


best = None
for t in range(-180, 181):
    s = solve(t)
    if not s:
        continue
    ok, _ = verify(s)
    if not ok:
        continue
    # want foot clearly below H0 and reasonably forward of H0
    score = s["Foot"][1]  # lower is better (more negative)
    if best is None or score < best[0]:
        best = (score, s)

assert best, "no valid pose found"
s = best[1]
ok, checks = verify(s)
print("Selected crank theta = %.1f deg" % s["theta"])
print("Link-length verification (computed vs target):")
for k, (a, b) in checks.items():
    print("  %-16s %8.3f vs %7.1f  err=%.2e" % (k, a, b, abs(a - b)))
print("All constraints satisfied:", ok)
print()
pts = {"H0": H0, "H1": H1, "H2": H2,
       "A": s["A"], "B": s["B"], "F": s["F"], "Foot": s["Foot"]}
for name, p in pts.items():
    print("  %-5s = (%8.3f, %8.3f)" % (name, p[0], p[1]))

# Tail attaches at the rear-low frame point and sweeps back-down,
# opposite to the forward-reaching foot, acting as a kangaroo counterbalance / tripod.
T = (-70.0, -5.0)
tail_ang = math.radians(210.0)
TAIL_LEN = 210.0
TailEnd = (T[0] + TAIL_LEN * math.cos(tail_ang),
           T[1] + TAIL_LEN * math.sin(tail_ang))
pts["T"] = T
pts["TailEnd"] = TailEnd

out = {"theta": s["theta"], "points": {k: [round(v[0], 4), round(v[1], 4)] for k, v in pts.items()},
       "links": {"L1": L1, "L2": L2, "L3": L3, "L4": L4, "L5": L5,
                 "foot": FOOT_LEN, "tail": TAIL_LEN}}
with open("mechanism_pose.json", "w", encoding="utf-8") as f:
    json.dump(out, f, indent=2)
print("\nWrote mechanism_pose.json")
