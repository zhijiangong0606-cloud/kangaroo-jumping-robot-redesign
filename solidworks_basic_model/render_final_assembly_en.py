from PIL import Image, ImageDraw, ImageFont
from pathlib import Path

OUT = Path("C:/Users/Gzj/Desktop/kangaroo_robot_redesign/solidworks_basic_model")
FONT = "C:/Windows/Fonts/arial.ttf"
FONTB = "C:/Windows/Fonts/arialbd.ttf"


def font(size, bold=False):
    return ImageFont.truetype(FONTB if bold else FONT, size)


def main():
    W, H = 1800, 1200
    img = Image.new("RGB", (W, H), (242, 245, 248))
    d = ImageDraw.Draw(img)
    ink = (30, 41, 59)
    muted = (100, 116, 139)
    plate = (226, 232, 240)
    edge = (71, 85, 105)
    blue = (37, 99, 235)
    orange = (234, 88, 12)
    purple = (124, 58, 237)
    green = (22, 163, 74)
    teal = (14, 116, 144)
    cyan = (56, 189, 248)
    red = (220, 38, 38)

    d.rectangle([0, 0, W, 110], fill=(15, 23, 42))
    d.text((48, 28), "Kangaroo-Inspired Jumping Robot - Completed Engineering Layout", font=font(36, True), fill="white")
    d.text((50, 72), "motor preload + elastic tendon + latch release + adjustable tail stabilizer", font=font(20), fill=(203, 213, 225))

    def iso(x, y, z):
        return 570 + x * 2.05 + y * 1.2, 410 + z * 1.75 - y * 0.65

    def shade(c, delta):
        return tuple(max(0, min(255, v + delta)) for v in c)

    def poly3(points, color, outline=edge):
        d.polygon([iso(*p) for p in points], fill=color, outline=outline)

    def prism_xz(poly, y0, y1, color):
        front = [(x, y0, z) for x, z in poly]
        back = [(x, y1, z) for x, z in poly]
        for i in range(len(poly)):
            j = (i + 1) % len(poly)
            poly3([front[i], front[j], back[j], back[i]], shade(color, -18))
        poly3(back, shade(color, 18))
        poly3(front, color)

    def capsule(p, q, y, w, color):
        p0 = iso(p[0], y, p[1])
        q0 = iso(q[0], y, q[1])
        d.line([p0, q0], fill=shade(color, -45), width=int(w * 2.2 + 7))
        d.line([p0, q0], fill=color, width=int(w * 2.2))
        for pt in [p0, q0]:
            d.ellipse([pt[0] - w * 1.05, pt[1] - w * 1.05, pt[0] + w * 1.05, pt[1] + w * 1.05], fill=shade(color, 20), outline=edge, width=2)

    def box3(cx, y, z, lx, ly, lz, color):
        x0, x1 = cx - lx / 2, cx + lx / 2
        y0, y1 = y - ly / 2, y + ly / 2
        z0, z1 = z - lz / 2, z + lz / 2
        pts = [(x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0), (x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1)]
        for face, col in [([0, 1, 2, 3], shade(color, -20)), ([4, 7, 6, 5], shade(color, 25)), ([1, 5, 6, 2], shade(color, 5)), ([0, 4, 5, 1], color), ([2, 6, 7, 3], shade(color, -10))]:
            poly3([pts[i] for i in face], col)

    def cyl_disc(cx, y, z, r, depth, color):
        p = iso(cx, y, z)
        p2 = iso(cx, y + depth, z)
        d.ellipse([p[0] - r * 2.0, p[1] - r * 1.35, p[0] + r * 2.0, p[1] + r * 1.35], fill=shade(color, 25), outline=edge, width=3)
        d.ellipse([p2[0] - r * 2.0, p2[1] - r * 1.35, p2[0] + r * 2.0, p2[1] + r * 1.35], fill=color, outline=edge, width=3)
        d.line([p[0], p[1] - r * 1.35, p2[0], p2[1] - r * 1.35], fill=edge, width=2)
        d.line([p[0], p[1] + r * 1.35, p2[0], p2[1] + r * 1.35], fill=edge, width=2)

    H0 = (0, 0)
    H1 = (-80, 60)
    H2 = (120, 60)
    A = (-42.4, 46.3)
    B = (45, 79.9)
    F = (-58.1, 231)
    foot = (25, 246)
    T = (200, -20)
    tail = (400, -60)

    d.ellipse([250, 945, 1400, 1125], fill=(214, 220, 228))
    body = [(-125, -45), (250, -45), (330, 35), (270, 120), (-110, 130), (-165, 50)]
    prism_xz(body, -10, -6, plate)
    prism_xz(body, 6, 10, plate)

    box3(-25, 0, 90, 60, 28, 32, (148, 163, 184))
    cyl_disc(-55, -12, 80, 18, 24, (96, 165, 250))
    box3(135, 0, 95, 38, 24, 35, (134, 239, 172))
    box3(80, 0, 72, 50, 10, 14, (251, 191, 36))

    for y in [-18, 18]:
        capsule(H1, A, y, 8, orange)
        capsule(A, B, y, 8, blue)
        capsule(H0, B, y, 9, purple)
        capsule(B, F, y, 9, green)
        capsule(H2, F, y, 9, teal)
        capsule(F, foot, y, 12, cyan)
    capsule(T, tail, 0, 7, (51, 65, 85))
    tail_tip = iso(tail[0], 0, tail[1])
    d.ellipse([tail_tip[0] - 30, tail_tip[1] - 22, tail_tip[0] + 30, tail_tip[1] + 22], fill=(71, 85, 105), outline=edge, width=3)

    for name, p in [("H0", H0), ("H1", H1), ("H2", H2), ("A", A), ("B", B), ("F", F), ("T", T)]:
        p_l = iso(p[0], -24, p[1])
        p_r = iso(p[0], 24, p[1])
        d.line([p_l, p_r], fill=(15, 23, 42), width=5)
        d.ellipse([p_r[0] - 7, p_r[1] - 7, p_r[0] + 7, p_r[1] + 7], fill="white", outline=ink, width=2)
        d.text((p_r[0] + 8, p_r[1] - 10), name, font=font(15, True), fill=ink)

    start = iso(-64, -22, 78)
    end = iso(45, -22, 80)
    last = start
    for i in range(1, 16):
        t = i / 16
        x = start[0] * (1 - t) + end[0] * t
        y = start[1] * (1 - t) + end[1] * t + (14 if i % 2 else -14)
        d.line([last, (x, y)], fill=red, width=5)
        last = (x, y)
    d.line([last, end], fill=red, width=5)

    d.rounded_rectangle([1230, 180, 1715, 690], radius=16, fill="white", outline=(180, 190, 205), width=2)
    d.text((1260, 215), "Modules", font=font(30, True), fill=ink)
    items = [
        ("dual body side plates", plate),
        ("1-DOF closed-chain hind leg", green),
        ("motor and winding drum", blue),
        ("servo latch release", orange),
        ("elastic tendon energy storage", red),
        ("tail mass stabilizer", (51, 65, 85)),
        ("M3 axles and spacers", (15, 23, 42)),
    ]
    y = 275
    for txt, c in items:
        d.rounded_rectangle([1260, y, 1290, y + 22], radius=4, fill=c, outline=edge)
        d.text((1310, y - 2), txt, font=font(21), fill=ink)
        y += 55

    d.rounded_rectangle([90, 1015, 1710, 1145], radius=16, fill="white", outline=(180, 190, 205), width=2)
    d.text((125, 1045), "Key dimensions: H0-H1=100 mm, H0-H2=134 mm, L1=40 mm, L2=120 mm, L3=100 mm, L4=140 mm, L5=180 mm, tail=180-220 mm", font=font(23, True), fill=ink)
    d.text((125, 1090), "The completed SolidWorks assembly includes rotated links, axles, spacers, elastic tendon, tail mass, motor, drum, servo and latch placeholders.", font=font(20), fill=muted)

    out = OUT / "final_completed_engineering_render_en.png"
    img.save(out, quality=95)
    print(out)


if __name__ == "__main__":
    main()
