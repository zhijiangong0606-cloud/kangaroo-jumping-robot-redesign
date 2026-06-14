"""
Motion-range + interference sweep for the 1-DOF hind-leg closed chain.

Reuses the circle-circle solver. For each crank angle theta:
  1. Solve A, B, F (closed-chain solvability = range of motion).
  2. Z-layer analysis: leg links sit on separate 4 mm layers (5 mm pitch),
     so link-vs-link physical collision is impossible. We still verify it.
  3. Check each link (modeled as a thick capsule = segment +- height/2) against
     the full-width corner STANDOFFS and the center modules that share its Z range.

Outputs a JSON report + prints the continuous valid theta range and any clashes.
"""
import math, json

H0 = (0.0, 0.0); H1 = (-94.8, 64.4); H2 = (134.3, 38.0)
L1, L2, L3, L4, L5 = 59.0, 108.9, 88.2, 100.3, 160.6
FOOT_LEN = 80.0

# link -> (mid-plane Z, thickness, height)  matching the CAD
LAYER = {
    "L1": (29, 4, 24), "L2": (14, 4, 22), "L3": (19, 4, 32),
    "L4": (9, 4, 30), "L5": (24, 4, 28), "Foot": (4, 4, 34),
}
PLATE_Z, PLATE_T = 34, 4
# standoffs: full-width tubes (z -32..32), OD 8 -> radius 4, at 4 corners
STANDOFF_R = 4.0
STANDOFFS = [(-135.0, -28.0), (152.0, -28.0), (-135.0, 98.0), (152.0, 98.0)]


def zrange(mid, t):
    return (mid - t / 2.0, mid + t / 2.0)


def zoverlap(a, b):
    return not (a[1] <= b[0] or b[1] <= a[0])


def dist(p, q):
    return math.hypot(p[0] - q[0], p[1] - q[1])


def circ_intersect(c0, r0, c1, r1):
    x0, y0 = c0; x1, y1 = c1
    d = math.hypot(x1 - x0, y1 - y0)
    if d > r0 + r1 + 1e-9 or d < abs(r0 - r1) - 1e-9 or d == 0:
        return None
    a = (r0 * r0 - r1 * r1 + d * d) / (2 * d)
    h2 = max(0.0, r0 * r0 - a * a)
    h = math.sqrt(h2)
    xm = x0 + a * (x1 - x0) / d; ym = y0 + a * (y1 - y0) / d
    xs = h * (y1 - y0) / d; ys = h * (x1 - x0) / d
    return ((xm + xs, ym - ys), (xm - xs, ym + ys))


def solve(theta_deg):
    th = math.radians(theta_deg)
    A = (H1[0] + L1 * math.cos(th), H1[1] + L1 * math.sin(th))
    bsol = circ_intersect(A, L2, H0, L3)
    if not bsol:
        return None
    B = min(bsol, key=lambda p: (p[1], -p[0]))
    fsol = circ_intersect(B, L4, H2, L5)
    if not fsol:
        return None
    F = min(fsol, key=lambda p: p[1])
    ang = math.atan2(F[1] - B[1], F[0] - B[0])
    Foot = (F[0] + FOOT_LEN * math.cos(ang), F[1] + FOOT_LEN * math.sin(ang))
    return dict(theta=theta_deg, A=A, B=B, F=F, Foot=Foot)


def seg_point_dist(a, b, p):
    """min distance from point p to segment a-b."""
    ax, ay = a; bx, by = b; px, py = p
    dx, dy = bx - ax, by - ay
    L2_ = dx * dx + dy * dy
    if L2_ == 0:
        return dist(a, p)
    t = max(0.0, min(1.0, ((px - ax) * dx + (py - ay) * dy) / L2_))
    cx, cy = ax + t * dx, ay + t * dy
    return math.hypot(px - cx, py - cy)


def link_segments(s):
    return {
        "L1": (H1, s["A"]), "L2": (s["A"], s["B"]), "L3": (H0, s["B"]),
        "L4": (s["B"], s["F"]), "L5": (H2, s["F"]), "Foot": (s["F"], s["Foot"]),
    }


# ---- 1. Z-layer disjointness proof ----
names = list(LAYER.keys())
zr = {n: zrange(LAYER[n][0], LAYER[n][1]) for n in names}
overlaps = []
for i in range(len(names)):
    for j in range(i + 1, len(names)):
        if zoverlap(zr[names[i]], zr[names[j]]):
            overlaps.append((names[i], names[j]))
# also vs plate
plate_zr = zrange(PLATE_Z, PLATE_T)
plate_clash_layers = [n for n in names if zoverlap(zr[n], plate_zr)]

# ---- 2. sweep theta ----
solvable = []
for t10 in range(-1800, 1801):
    t = t10 / 10.0
    if solve(t):
        solvable.append(t)

# continuous ranges
ranges = []
if solvable:
    start = prev = solvable[0]
    for t in solvable[1:]:
        if abs(t - prev) > 0.15:
            ranges.append((start, prev)); start = t
        prev = t
    ranges.append((start, prev))

# ---- 3. interference vs standoffs across the sweep (1 deg steps) ----
clashes = []
half_h = {n: LAYER[n][2] / 2.0 for n in names}
for t in range(-180, 181):
    s = solve(t)
    if not s:
        continue
    segs = link_segments(s)
    for n, (a, b) in segs.items():
        # link half-width perpendicular clearance = height/2; treat standoff as a
        # disk of radius STANDOFF_R that shares this link's full Z (standoff spans all)
        for k, c in enumerate(STANDOFFS):
            d = seg_point_dist(a, b, c)
            clearance = d - STANDOFF_R - half_h[n]
            if clearance < 0:
                clashes.append({"theta": t, "link": n, "standoff": k,
                                "gap_mm": round(clearance, 2)})

report = {
    "z_ranges": zr,
    "link_link_overlaps": overlaps,
    "plate_clash_layers": plate_clash_layers,
    "solvable_ranges_deg": [[round(a, 1), round(b, 1)] for a, b in ranges],
    "n_solvable_samples": len(solvable),
    "standoff_clashes": clashes,
}
with open("motion_interference_report.json", "w", encoding="utf-8") as f:
    json.dump(report, f, indent=2)

print("=== Z-layer disjointness ===")
for n in names:
    print("  %-5s z=%s" % (n, zr[n]))
print("  link-link Z overlaps (would allow collision):", overlaps or "NONE")
print("  layers overlapping body plate:", plate_clash_layers or "NONE")
print()
print("=== Motion range (closed-chain solvable) ===")
for a, b in ranges:
    print("  theta in [%.1f, %.1f] deg  (span %.1f)" % (a, b, b - a))
print()
print("=== Standoff interference across sweep ===")
if not clashes:
    print("  NONE - no link hits any corner standoff across full motion")
else:
    print("  %d clash samples:" % len(clashes))
    for c in clashes[:20]:
        print("   ", c)
print("\nWrote motion_interference_report.json")
