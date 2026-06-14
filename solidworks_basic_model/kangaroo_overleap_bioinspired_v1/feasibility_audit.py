"""
Physics feasibility audit for the kangaroo jumping robot.

Three make-or-break checks for a real, runnable build:

(A) Transmission angle / dead-point check across the working stroke.
    A linkage near a dead point (mu -> 0 or 180 deg) cannot transmit useful
    force regardless of input torque. We compute the transmission angle at
    every joint of the 6-bar chain over the release stroke and flag mu<40deg.

(B) Energy budget self-consistency.
    The design table assumes m=0.45kg, k=900N/m, eta=45%. We back out the
    spring force, required preload force, motor winding torque, and check the
    drum/motor torque budget against a real 37D gearmotor.

(C) Foot-force transmission ratio.
    Using the Jacobian of the closed chain, map tendon pull at A to vertical
    ground reaction at the foot at the release pose, and check the leg can
    actually launch m*g with margin.
"""
import math, json

H0 = (0.0, 0.0); H1 = (-80.0, 60.0); H2 = (120.0, 60.0)
L1, L2, L3, L4, L5 = 40.0, 120.0, 100.0, 140.0, 180.0
FOOT_LEN = 80.0


def circ_intersect(c0, r0, c1, r1):
    x0, y0 = c0; x1, y1 = c1
    d = math.hypot(x1 - x0, y1 - y0)
    if d > r0 + r1 + 1e-9 or d < abs(r0 - r1) - 1e-9 or d == 0:
        return None
    a = (r0 * r0 - r1 * r1 + d * d) / (2 * d)
    h = math.sqrt(max(0.0, r0 * r0 - a * a))
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


def angle_between(p_center, p1, p2):
    """interior angle at p_center formed by p_center->p1 and p_center->p2, deg."""
    v1 = (p1[0] - p_center[0], p1[1] - p_center[1])
    v2 = (p2[0] - p_center[0], p2[1] - p_center[1])
    n1 = math.hypot(*v1); n2 = math.hypot(*v2)
    if n1 == 0 or n2 == 0:
        return 0.0
    c = max(-1.0, min(1.0, (v1[0]*v2[0] + v1[1]*v2[1]) / (n1*n2)))
    return math.degrees(math.acos(c))


# =========================================================================
# (A) Transmission angle across the stroke
# =========================================================================
# Working stroke: crouched (stored) -> extended (launch). We find the full
# solvable range, then look at transmission angle at coupler joints.
PRINT = []
def out(s): PRINT.append(s); print(s)

out("=" * 64)
out("(A) TRANSMISSION-ANGLE / DEAD-POINT CHECK")
out("=" * 64)

stroke = []
for t10 in range(-1800, 1801):
    t = t10/10.0
    s = solve(t)
    if s:
        stroke.append(s)

# transmission angle at B for the A-B-..-H0 dyad (coupler L2 vs rocker L3):
# mu = angle at B between B->A and B->H0  (classic four-bar transmission angle)
# and at F between F->B and F->H2 (second dyad L4 vs L5)
mu_min_B = 999; mu_min_F = 999; worst = None
samples = []
for s in stroke:
    muB = angle_between(s["B"], s["A"], H0)
    muB = min(muB, 180 - muB)
    muF = angle_between(s["F"], s["B"], H2)
    muF = min(muF, 180 - muF)
    samples.append((s["theta"], muB, muF, s["Foot"]))
    if muB < mu_min_B:
        mu_min_B = muB
    if muF < mu_min_F:
        mu_min_F = muF; worst = s["theta"]

out("  solvable crank range: theta in [%.1f, %.1f] deg" %
    (stroke[0]["theta"], stroke[-1]["theta"]))
out("  min transmission angle at B (L2-L3 dyad): %.1f deg" % mu_min_B)
out("  min transmission angle at F (L4-L5 dyad): %.1f deg" % mu_min_F)
GOOD = 40.0
out("  threshold for good transmission: >= %.0f deg" % GOOD)
out("  VERDICT B-dyad: %s" % ("OK" if mu_min_B >= GOOD else "RISK - near dead point"))
out("  VERDICT F-dyad: %s" % ("OK" if mu_min_F >= GOOD else "RISK - near dead point"))

# =========================================================================
# (B) Energy budget self-consistency
# =========================================================================
out("")
out("=" * 64)
out("(B) ENERGY-BUDGET SELF-CONSISTENCY")
out("=" * 64)
m = 0.45; g = 9.81; k = 900.0; eta = 0.45
for x_mm in [30, 40, 45]:
    x = x_mm/1000.0
    E = 0.5*k*x*x
    F_spring = k*x
    h = eta*E/(m*g)
    out("  preload %2d mm: E=%.3f J  F_spring=%.1f N  est height=%.1f cm" %
        (x_mm, E, F_spring, h*100))
out("")
# Motor torque to wind tendon at max preload
x = 0.045; F_spring = k*x
for r_mm in [12, 18]:
    r = r_mm/1000.0
    tau = F_spring * r
    out("  drum r=%2d mm: winding torque needed = %.3f N.m (= %.1f kg.cm)" %
        (r_mm, tau, tau/g*100))
out("  ref: 37D 6V gearmotor stall ~ 1.2-2.5 N.m (geared) -> ample for slow wind")
out("  NOTE: spring force %.0f N at full preload is the real structural load" % (k*0.045))

# =========================================================================
# (C) Foot-force transmission (numeric Jacobian)
# =========================================================================
out("")
out("=" * 64)
out("(C) FOOT-FORCE TRANSMISSION (release pose)")
out("=" * 64)
# pick the most-crouched solvable pose as release start
rel = stroke[0]
# numeric d(Foot)/d(theta) and d(A)/d(theta)
def deriv(theta):
    h = 0.05
    s1 = solve(theta - h); s2 = solve(theta + h)
    if not s1 or not s2:
        return None
    dFoot = ((s2["Foot"][0]-s1["Foot"][0])/(2*h),
             (s2["Foot"][1]-s1["Foot"][1])/(2*h))
    dA = ((s2["A"][0]-s1["A"][0])/(2*h),
          (s2["A"][1]-s1["A"][1])/(2*h))
    return dA, dFoot

# Virtual work: tendon pulls at A along tendon direction (approx toward drum at
# H1 region, i.e. tangential to crank). Power in = F_tendon . dA/dt.
# Power out = F_foot_vertical * dFoot_y/dt. Ratio at equal dtheta.
mid = stroke[len(stroke)//2]
d = deriv(mid["theta"])
if d:
    dA, dFoot = d
    speedA = math.hypot(*dA)
    speedFy = abs(dFoot[1])
    # crank torque from tendon: tau = F_spring * L1(perp) ~ F_spring*L1*sin(mu_crank)
    # foot vertical force from crank torque via velocity ratio (lever law):
    # F_foot_y * |dFoot_y| = tau * dtheta -> F_foot_y = tau / |dFoot_y/dtheta|
    Fsp = k*0.045
    tau_crank = Fsp * (L1/1000.0)   # upper bound, perp lever
    dFooty_dtheta = abs(dFoot[1]) * (1.0)  # mm per deg
    # convert: dFoot_y in mm per deg -> per rad
    dFooty_drad = abs(dFoot[1]) * (180/math.pi) / 1000.0  # m per rad
    if dFooty_drad > 1e-9:
        F_foot = tau_crank / dFooty_drad
        out("  at mid-stroke theta=%.1f:" % mid["theta"])
        out("    crank torque from tendon (max) = %.3f N.m" % tau_crank)
        out("    foot vertical travel rate = %.4f m/rad" % dFooty_drad)
        out("    -> peak vertical foot force ~ %.1f N" % F_foot)
        out("    weight to lift m*g = %.2f N" % (m*g))
        out("    static force margin = %.1fx" % (F_foot/(m*g)))
        out("    (>1 needed to launch; high ratio = brief impulse, good)")

# save
with open("feasibility_report.txt", "w", encoding="utf-8") as f:
    f.write("\n".join(PRINT))
out("")
out("wrote feasibility_report.txt")
