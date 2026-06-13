from pathlib import Path
import math
import csv

OUT = Path("C:/Users/Gzj/Desktop/kangaroo_robot_redesign/manufacturing_package")
OUT.mkdir(parents=True, exist_ok=True)


def dxf_header():
    return ["0", "SECTION", "2", "ENTITIES"]


def dxf_footer():
    return ["0", "ENDSEC", "0", "EOF"]


def add_line(lines, x1, y1, x2, y2, layer="0"):
    lines.extend(["0", "LINE", "8", layer, "10", f"{x1:.3f}", "20", f"{y1:.3f}", "30", "0",
                  "11", f"{x2:.3f}", "21", f"{y2:.3f}", "31", "0"])


def add_circle(lines, x, y, r, layer="0"):
    lines.extend(["0", "CIRCLE", "8", layer, "10", f"{x:.3f}", "20", f"{y:.3f}", "30", "0", "40", f"{r:.3f}"])


def add_poly(lines, pts, closed=True, layer="0"):
    seq = pts + ([pts[0]] if closed else [])
    for p, q in zip(seq, seq[1:]):
        add_line(lines, p[0], p[1], q[0], q[1], layer)


def write_dxf(name, lines):
    (OUT / name).write_text("\n".join(lines), encoding="ascii")


def rounded_plate_outline():
    return [(-165, 50), (-125, -45), (250, -45), (330, 35), (270, 120), (-110, 130)]


def create_body_plate_dxf():
    lines = dxf_header()
    add_poly(lines, rounded_plate_outline(), True, "CUT_OUTLINE")

    # Mechanism pivots.
    pivots = {
        "H0": (0, 0),
        "H1": (-80, 60),
        "H2": (120, 60),
        "T": (200, -20),
    }
    for name, (x, y) in pivots.items():
        add_circle(lines, x, y, 1.7, f"M3_{name}")
        add_circle(lines, x, y, 4.2, f"BEARING_CLEARANCE_{name}")

    # Motor placeholder: JGB37/25GA style adjustable slots.
    motor_holes = [(-55, 80), (-25, 80), (-55, 110), (-25, 110)]
    for x, y in motor_holes:
        add_circle(lines, x, y, 2.0, "MOTOR_MOUNT_M3")
    add_circle(lines, -40, 95, 11.0, "MOTOR_SHAFT_CLEARANCE")

    # Servo placeholder mounting holes.
    servo_holes = [(116, 82), (154, 82), (116, 108), (154, 108)]
    for x, y in servo_holes:
        add_circle(lines, x, y, 1.4, "SERVO_MOUNT")
    add_poly(lines, [(105, 75), (165, 75), (165, 116), (105, 116)], True, "SERVO_ENVELOPE")

    # Lightening holes. Keep away from pivots.
    for x, y, r in [(-115, 18, 14), (-70, 10, 12), (20, 88, 13), (70, 18, 14), (182, 28, 14), (245, 50, 13)]:
        add_circle(lines, x, y, r, "LIGHTENING")

    # Elastic tendon anchor holes.
    for x, y in [(-85, 78), (25, 80), (55, 92)]:
        add_circle(lines, x, y, 1.8, "TENDON_ANCHOR")

    lines += dxf_footer()
    write_dxf("body_side_plate_manufacturing_v2.dxf", lines)


def create_link_dxf():
    lines = dxf_header()
    specs = [
        ("L1_crank_40", 40, 0, 0, 16),
        ("L2_coupler_120", 120, 75, 0, 16),
        ("L3_thigh_100", 100, 235, 0, 18),
        ("L4_shank_140", 140, 375, 0, 18),
        ("L5_rear_rocker_180", 180, 555, 0, 18),
        ("foot_85", 85, 780, 0, 24),
        ("tail_210", 210, 910, 0, 12),
    ]
    for name, length, ox, oy, width in specs:
        r = width / 2
        # Manufacturing-friendly capsule approximation.
        seg = 18
        pts = []
        for i in range(seg + 1):
            a = math.pi / 2 + math.pi * i / seg
            pts.append((ox + r * math.cos(a), oy + r * math.sin(a)))
        for i in range(seg + 1):
            a = 3 * math.pi / 2 + math.pi * i / seg
            pts.append((ox + length + r * math.cos(a), oy + r * math.sin(a)))
        add_poly(lines, pts, True, f"{name}_OUTLINE")
        add_circle(lines, ox, oy, 1.7, f"{name}_M3_A")
        add_circle(lines, ox + length, oy, 1.7, f"{name}_M3_B")
        add_circle(lines, ox, oy, 4.0, f"{name}_BEARING_A")
        add_circle(lines, ox + length, oy, 4.0, f"{name}_BEARING_B")
        if length > 80:
            for u in [0.33, 0.66]:
                add_circle(lines, ox + length * u, oy, max(3.0, width * 0.22), f"{name}_LIGHTENING")
        if name.startswith("L4") or name.startswith("L3"):
            add_circle(lines, ox + length * 0.55, oy + width * 0.45, 1.6, f"{name}_TENDON_HOOK")
    lines += dxf_footer()
    write_dxf("links_manufacturing_v2.dxf", lines)


def create_checklist():
    rows = [
        ["Item", "Check", "Target"],
        ["Pivot holes", "M3 clearance", "3.2-3.4 mm drill"],
        ["Bearing option", "micro bearing seat", "outer diameter to match purchased bearing"],
        ["Side plate spacing", "left-right gap", "16-24 mm total with spacers"],
        ["Link clearance", "avoid plate rubbing", ">= 0.5 mm each side"],
        ["Elastic tendon", "anchor hooks", "no rubbing through full stroke"],
        ["Latch", "release direction", "servo horn must clear latch in <45 deg"],
        ["Tail", "mass adjustability", "0/15/30 g test points"],
        ["Foot", "contact material", "rubber pad bonded or screwed"],
        ["Motor", "winding direction", "limit switch before over-stretch"],
    ]
    with (OUT / "manufacturing_checklist.csv").open("w", newline="", encoding="utf-8-sig") as f:
        csv.writer(f).writerows(rows)


def create_notes():
    text = """# Manufacturing Package v2

This package improves the practical buildability of the kangaroo-inspired jumping robot.

## Files

- `body_side_plate_manufacturing_v2.dxf`: body side plate with pivot holes, motor/servo mounting placeholders, tendon anchors, and lightening holes.
- `links_manufacturing_v2.dxf`: link cutting layout with M3 holes, bearing reference circles, lightening holes, and tendon hook points.
- `manufacturing_checklist.csv`: fabrication and assembly checklist.

## Important Practical Changes

1. Pivot holes now include both M3 clearance holes and bearing-clearance reference circles.
2. Body side plate includes motor, servo, and tendon anchor references.
3. Long links include lightening holes.
4. L3/L4 include tendon hook points for rubber band or extension spring attachment.
5. The package separates manufacturing DXF files from the earlier conceptual CAD files.

## Suggested Fabrication

- First prototype: 3 mm acrylic or 3D printed PLA.
- Stronger version: 2-3 mm aluminum links and 3-4 mm carbon/acrylic body plates.
- Pivots: M3 bolts with washers and locknuts.
- Optional bearings: select bearings first, then resize the bearing reference circles.

## Assembly Notes

- Keep left and right legs synchronized with a shared H1 input shaft.
- Add washers between links to avoid rubbing.
- Start elastic preload at 20 mm before testing larger preload values.
- Use a limit switch or current limit to prevent motor over-winding.
- Test tail masses at 0 g, 15 g, and 30 g.
"""
    (OUT / "README_manufacturing_package_v2.md").write_text(text, encoding="utf-8")


if __name__ == "__main__":
    create_body_plate_dxf()
    create_link_dxf()
    create_checklist()
    create_notes()
    print(f"Generated manufacturing package in {OUT}")
