from PIL import Image, ImageDraw, ImageFont
from pathlib import Path
import csv
import math

OUT = Path("C:/Users/Gzj/Desktop/kangaroo_robot_redesign")
FONT_REG = "C:/Windows/Fonts/msyh.ttc"
FONT_BOLD = "C:/Windows/Fonts/msyhbd.ttc"


def font(size, bold=False):
    return ImageFont.truetype(FONT_BOLD if bold else FONT_REG, size)


H0 = (0.0, 0.0)
H1 = (-80.0, 60.0)
H2 = (120.0, 60.0)
R1, R2, R3, R4, R5 = 40.0, 120.0, 100.0, 140.0, 180.0


def circle_intersections(c0, r0, c1, r1):
    x0, y0 = c0
    x1, y1 = c1
    dx, dy = x1 - x0, y1 - y0
    d = math.hypot(dx, dy)
    if d > r0 + r1 or d < abs(r0 - r1) or d == 0:
        return []
    a = (r0 * r0 - r1 * r1 + d * d) / (2 * d)
    h = math.sqrt(max(0, r0 * r0 - a * a))
    xm, ym = x0 + a * dx / d, y0 + a * dy / d
    rx, ry = -dy * h / d, dx * h / d
    return [(xm + rx, ym + ry), (xm - rx, ym - ry)]


def solve(theta_deg):
    a = math.radians(theta_deg)
    A = (H1[0] + R1 * math.cos(a), H1[1] + R1 * math.sin(a))
    candidates = []
    for B in circle_intersections(A, R2, H0, R3):
        for F in circle_intersections(B, R4, H2, R5):
            candidates.append((A, B, F))
    if not candidates:
        return None
    return sorted(candidates, key=lambda item: -item[2][1])[0]


def map_point(p, ox=500, oy=280, s=2.4):
    return ox + p[0] * s, oy + p[1] * s


def draw_link(d, p1, p2, color, width=12):
    d.line([p1, p2], fill="white", width=width + 6)
    d.line([p1, p2], fill=color, width=width)


def draw_joint(d, p, label=None):
    x, y = p
    d.ellipse([x - 11, y - 11, x + 11, y + 11], fill="white", outline=(32, 38, 46), width=3)
    if label:
        d.text((x + 13, y - 10), label, font=font(16, True), fill=(32, 38, 46))


def arrow(d, p1, p2, fill, width=3, head=10):
    d.line([p1, p2], fill=fill, width=width)
    a = math.atan2(p2[1] - p1[1], p2[0] - p1[0])
    d.polygon(
        [
            p2,
            (p2[0] - head * math.cos(a - 0.5), p2[1] - head * math.sin(a - 0.5)),
            (p2[0] - head * math.cos(a + 0.5), p2[1] - head * math.sin(a + 0.5)),
        ],
        fill=fill,
    )


def dim_arrow(d, p1, p2, text, offset=(0, 0)):
    arrow(d, p1, p2, (96, 108, 122), 3, 8)
    arrow(d, p2, p1, (96, 108, 122), 3, 8)
    mx = (p1[0] + p2[0]) / 2 + offset[0]
    my = (p1[1] + p2[1]) / 2 + offset[1]
    d.rounded_rectangle([mx - 48, my - 17, mx + 58, my + 17], radius=6, fill="white", outline=(172, 184, 198))
    d.text((mx - 38, my - 12), text, font=font(15, True), fill=(32, 38, 46))


def draw_verified_dimension_figure():
    img = Image.new("RGB", (1600, 1000), (247, 249, 250))
    d = ImageDraw.Draw(img)
    d.rectangle([0, 0, 1600, 92], fill=(24, 33, 45))
    d.text((42, 22), "校核版机构简图：尺寸闭合的一自由度后肢", font=font(34, True), fill="white")
    d.text((44, 62), "推荐以本图和 verified DXF 为准；单位 mm", font=font(17), fill=(203, 213, 225))
    d.rounded_rectangle([55, 130, 1545, 910], radius=18, fill="white", outline=(172, 184, 198), width=2)

    A, B, F = solve(-20)
    raw = {
        "H0": H0,
        "H1": H1,
        "H2": H2,
        "A": A,
        "B": B,
        "F": F,
        "Foot": (F[0] + 70, F[1] + 15),
        "T": (200, -20),
        "Tail": (400, -60),
    }
    pts = {k: map_point(v) for k, v in raw.items()}

    body = [
        (pts["H1"][0] - 105, pts["H1"][1] - 105),
        (pts["H2"][0] + 125, pts["H2"][1] - 98),
        (pts["T"][0] + 92, pts["T"][1] + 70),
        (pts["H0"][0] - 130, pts["H0"][1] + 105),
    ]
    d.polygon(body, fill=(235, 239, 245), outline=(100, 116, 139))

    colors = {
        "L1": (234, 88, 12),
        "L2": (37, 99, 235),
        "L3": (124, 58, 237),
        "L4": (22, 163, 74),
        "L5": (14, 116, 144),
        "Foot": (56, 189, 248),
        "Tail": (31, 41, 55),
    }
    for a, b, c in [
        ("H1", "A", colors["L1"]),
        ("A", "B", colors["L2"]),
        ("H0", "B", colors["L3"]),
        ("B", "F", colors["L4"]),
        ("H2", "F", colors["L5"]),
        ("F", "Foot", colors["Foot"]),
        ("T", "Tail", colors["Tail"]),
    ]:
        draw_link(d, pts[a], pts[b], c, 13)
    for k in ["H0", "H1", "H2", "A", "B", "F", "T"]:
        draw_joint(d, pts[k], k)

    dim_arrow(d, pts["H0"], pts["H1"], "100", (-30, -35))
    dim_arrow(d, pts["H0"], pts["H2"], "134", (10, -40))
    dim_arrow(d, pts["H1"], pts["A"], "40", (-42, 0))
    dim_arrow(d, pts["A"], pts["B"], "120", (0, 40))
    dim_arrow(d, pts["H0"], pts["B"], "100", (40, -20))
    dim_arrow(d, pts["B"], pts["F"], "140", (20, -35))
    dim_arrow(d, pts["H2"], pts["F"], "180", (52, 0))
    dim_arrow(d, pts["T"], pts["Tail"], "200", (-5, -35))

    traj = []
    for deg in range(-120, 121, 5):
        sol = solve(deg)
        if sol:
            traj.append(map_point(sol[2]))
    for p, q in zip(traj, traj[1:]):
        d.line([p, q], fill=(220, 38, 38), width=2)
    d.text((790, 790), "红线：F 点运动包络，说明闭链机构在该角度范围内可连续运动", font=font(20, True), fill=(220, 38, 38))

    d.rounded_rectangle([1110, 180, 1495, 745], radius=14, fill=(248, 250, 252), outline=(172, 184, 198), width=2)
    d.text((1135, 210), "校核尺寸", font=font(28, True), fill=(32, 38, 46))
    rows = [
        ("H0-H1", "100 mm"),
        ("H0-H2", "134 mm"),
        ("L1 H1-A", "40 mm"),
        ("L2 A-B", "120 mm"),
        ("L3 H0-B", "100 mm"),
        ("L4 B-F", "140 mm"),
        ("L5 H2-F", "180 mm"),
        ("足部延伸", "70-90 mm"),
        ("尾杆", "180-220 mm"),
    ]
    y = 265
    for name, value in rows:
        d.text((1140, y), name, font=font(19, True), fill=(96, 108, 122))
        d.text((1325, y), value, font=font(19), fill=(32, 38, 46))
        y += 48
    d.rounded_rectangle([1128, 700, 1475, 730], radius=7, fill=(240, 253, 244), outline=(134, 239, 172))
    d.text((1140, 705), "单侧自由度 M=1，推荐样机尺寸", font=font(16, True), fill=(22, 163, 74))
    img.save(OUT / "20_dimensioned_mechanism_verified_cn.png", quality=95)


def draw_verified_gif():
    frames = []
    angles = list(range(-120, 121, 10)) + list(range(120, -121, -10))
    for deg in angles:
        img = Image.new("RGB", (900, 560), (247, 249, 250))
        d = ImageDraw.Draw(img)
        d.rectangle([0, 0, 900, 54], fill=(24, 33, 45))
        d.text((24, 13), "校核版闭链机构运动", font=font(22, True), fill="white")
        A, B, F = solve(deg)
        raw = {
            "H0": H0,
            "H1": H1,
            "H2": H2,
            "A": A,
            "B": B,
            "F": F,
            "Foot": (F[0] + 70, F[1] + 15),
            "T": (200, -20),
            "Tail": (400, -60),
        }
        pts = {k: map_point(v, 250, 110, 1.55) for k, v in raw.items()}
        d.line([(40, 510), (860, 510)], fill=(148, 163, 184), width=3)
        d.polygon(
            [
                (pts["H1"][0] - 70, pts["H1"][1] - 75),
                (pts["H2"][0] + 90, pts["H2"][1] - 70),
                (pts["T"][0] + 72, pts["T"][1] + 50),
                (pts["H0"][0] - 90, pts["H0"][1] + 78),
            ],
            fill=(235, 239, 245),
            outline=(96, 108, 122),
        )
        for a, b, c in [
            ("H1", "A", (234, 88, 12)),
            ("A", "B", (37, 99, 235)),
            ("H0", "B", (124, 58, 237)),
            ("B", "F", (22, 163, 74)),
            ("H2", "F", (14, 116, 144)),
            ("F", "Foot", (56, 189, 248)),
            ("T", "Tail", (31, 41, 55)),
        ]:
            draw_link(d, pts[a], pts[b], c, 9)
        for k in ["H0", "H1", "H2", "A", "B", "F"]:
            draw_joint(d, pts[k])
        d.rounded_rectangle([28, 70, 245, 120], radius=8, fill="white", outline=(172, 184, 198))
        d.text((45, 82), f"输入曲柄角 {deg}°", font=font(20, True), fill=(37, 99, 235))
        frames.append(img)
    frames[0].save(OUT / "21_verified_motion_envelope_cn.gif", save_all=True, append_images=frames[1:], duration=90, loop=0)


def dxf_header():
    return ["0", "SECTION", "2", "ENTITIES"]


def dxf_footer():
    return ["0", "ENDSEC", "0", "EOF"]


def add_line(lines, x1, y1, x2, y2, layer="0"):
    lines.extend(
        [
            "0",
            "LINE",
            "8",
            layer,
            "10",
            f"{x1:.3f}",
            "20",
            f"{y1:.3f}",
            "30",
            "0",
            "11",
            f"{x2:.3f}",
            "21",
            f"{y2:.3f}",
            "31",
            "0",
        ]
    )


def add_circle(lines, x, y, r, layer="0"):
    lines.extend(["0", "CIRCLE", "8", layer, "10", f"{x:.3f}", "20", f"{y:.3f}", "30", "0", "40", f"{r:.3f}"])


def add_poly(lines, points, layer):
    for p, q in zip(points, points[1:] + [points[0]]):
        add_line(lines, p[0], p[1], q[0], q[1], layer)


def write_verified_dxf():
    lines = dxf_header()
    A, B, F = solve(-20)
    for p, q in [
        (H1, A),
        (A, B),
        (H0, B),
        (B, F),
        (H2, F),
        (F, (F[0] + 75, F[1] + 15)),
        ((200, -20), (400, -60)),
    ]:
        add_line(lines, p[0], p[1], q[0], q[1], "SCHEMATIC")
    for p in [H0, H1, H2, A, B, F, (200, -20)]:
        add_circle(lines, p[0], p[1], 2, "JOINTS")

    sx, sy = 0, -250
    outline = [(-125 + sx, -45 + sy), (250 + sx, -45 + sy), (330 + sx, 35 + sy), (270 + sx, 120 + sy), (-110 + sx, 130 + sy), (-165 + sx, 50 + sy)]
    add_poly(lines, outline, "BODY")
    for p in [H0, H1, H2, (200, -20), (55, 55), (165, 90)]:
        add_circle(lines, p[0] + sx, p[1] + sy, 2.0 if p not in [(55, 55), (165, 90)] else 6, "BODY_HOLES")

    specs = [
        ("L1", 40, 0, -420),
        ("L2", 120, 80, -420),
        ("L3", 100, 250, -420),
        ("L4", 140, 400, -420),
        ("L5", 180, 590, -420),
        ("TAIL", 210, 820, -420),
    ]
    for name, dist, ox, oy in specs:
        w = 18
        add_poly(lines, [(ox, oy - w / 2), (ox + dist, oy - w / 2), (ox + dist + 8, oy), (ox + dist, oy + w / 2), (ox, oy + w / 2), (ox - 8, oy)], name)
        add_circle(lines, ox, oy, 2.0, name)
        add_circle(lines, ox + dist, oy, 2.0, name)
    lines += dxf_footer()
    (OUT / "verified_body_and_links_import_to_inventor.dxf").write_text("\n".join(lines), encoding="ascii")


def write_verified_dimensions():
    with (OUT / "verified_design_dimensions.csv").open("w", newline="", encoding="utf-8-sig") as f:
        writer = csv.writer(f)
        writer.writerow(["项目", "数值", "单位", "说明"])
        rows = [
            ("H0-H1", 100, "mm", "校核版推荐尺寸"),
            ("H0-H2", 134, "mm", "校核版推荐尺寸"),
            ("L1 曲柄", 40, "mm", "校核版推荐尺寸"),
            ("L2 连杆", 120, "mm", "校核版推荐尺寸"),
            ("L3 大腿摇杆", 100, "mm", "校核版推荐尺寸"),
            ("L4 小腿", 140, "mm", "校核版推荐尺寸"),
            ("L5 后摇杆", 180, "mm", "校核版推荐尺寸"),
            ("尾杆", "180-220", "mm", "可调"),
        ]
        writer.writerows(rows)


if __name__ == "__main__":
    draw_verified_dimension_figure()
    draw_verified_gif()
    write_verified_dxf()
    write_verified_dimensions()
    print("Generated verified mechanism assets.")
