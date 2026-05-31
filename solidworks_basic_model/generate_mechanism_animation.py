from PIL import Image, ImageDraw, ImageFont
from pathlib import Path
import math

OUT = Path("C:/Users/Gzj/Desktop/kangaroo_robot_redesign/solidworks_basic_model")
FONT = "C:/Windows/Fonts/arial.ttf"
FONTB = "C:/Windows/Fonts/arialbd.ttf"


def font(size, bold=False):
    return ImageFont.truetype(FONTB if bold else FONT, size)


H0 = (0.0, 0.0)
H1 = (-80.0, 60.0)
H2 = (120.0, 60.0)
R1, R2, R3, R4, R5 = 40.0, 120.0, 100.0, 140.0, 180.0
TAIL_ROOT = (200.0, -20.0)
TAIL_TIP_BASE = (400.0, -60.0)


def circle_intersections(c0, r0, c1, r1):
    x0, y0 = c0
    x1, y1 = c1
    dx, dy = x1 - x0, y1 - y0
    d = math.hypot(dx, dy)
    if d > r0 + r1 or d < abs(r0 - r1) or d == 0:
        return []
    a = (r0 * r0 - r1 * r1 + d * d) / (2 * d)
    h = math.sqrt(max(0, r0 * r0 - a * a))
    xm = x0 + a * dx / d
    ym = y0 + a * dy / d
    rx = -dy * h / d
    ry = dx * h / d
    return [(xm + rx, ym + ry), (xm - rx, ym - ry)]


def solve(theta_deg, previous=None):
    a = math.radians(theta_deg)
    A = (H1[0] + R1 * math.cos(a), H1[1] + R1 * math.sin(a))
    candidates = []
    for B in circle_intersections(A, R2, H0, R3):
        for F in circle_intersections(B, R4, H2, R5):
            candidates.append((A, B, F))
    if not candidates:
        return None
    if previous:
        _, prev_b, prev_f = previous
        return min(candidates, key=lambda item: dist(item[1], prev_b) + dist(item[2], prev_f))
    return max(candidates, key=lambda item: item[2][1])


def dist(a, b):
    return math.hypot(a[0] - b[0], a[1] - b[1])


def lerp(a, b, t):
    return a + (b - a) * t


def rotate_about(p, root, deg):
    a = math.radians(deg)
    x, y = p[0] - root[0], p[1] - root[1]
    return (root[0] + x * math.cos(a) - y * math.sin(a), root[1] + x * math.sin(a) + y * math.cos(a))


def make_motion_states(n=96):
    # Input crank schedule. Slow preload, quick release, short reset.
    angles = []
    phases = []
    for i in range(n):
        t = i / n
        if t < 0.46:
            u = t / 0.46
            theta = lerp(-120, -35, 0.5 - 0.5 * math.cos(math.pi * u))
            phase = "PRELOAD"
        elif t < 0.70:
            u = (t - 0.46) / 0.24
            theta = lerp(-35, 110, 1 - (1 - u) ** 3)
            phase = "RELEASE / EXTEND"
        else:
            u = (t - 0.70) / 0.30
            theta = lerp(110, -120, 0.5 - 0.5 * math.cos(math.pi * u))
            phase = "RESET"
        angles.append(theta)
        phases.append(phase)

    states = []
    prev = None
    for i, theta in enumerate(angles):
        sol = solve(theta, prev)
        if sol is None:
            sol = prev
        prev = sol
        A, B, F = sol
        foot = (F[0] + 82, F[1] + 14)
        # Tail counter-pitch: slight active-looking stabilizing swing.
        tail_swing = -8 * math.sin(2 * math.pi * i / n)
        tail_tip = rotate_about(TAIL_TIP_BASE, TAIL_ROOT, tail_swing)
        states.append(
            {
                "theta": theta,
                "phase": phases[i],
                "A": A,
                "B": B,
                "F": F,
                "foot": foot,
                "tail": tail_tip,
                "progress": i / n,
            }
        )
    return states


COL = {
    "bg": (244, 247, 250),
    "navy": (15, 23, 42),
    "ink": (30, 41, 59),
    "muted": (100, 116, 139),
    "plate": (226, 232, 240),
    "edge": (71, 85, 105),
    "orange": (234, 88, 12),
    "blue": (37, 99, 235),
    "purple": (124, 58, 237),
    "green": (22, 163, 74),
    "teal": (14, 116, 144),
    "cyan": (56, 189, 248),
    "red": (220, 38, 38),
    "gold": (245, 158, 11),
}


def draw_frame(state, path_history, size=(1400, 850), scale=2.15):
    W, H = size
    img = Image.new("RGB", size, COL["bg"])
    d = ImageDraw.Draw(img)
    d.rectangle([0, 0, W, 84], fill=COL["navy"])
    d.text((34, 18), "Kangaroo-inspired jumping mechanism animation", font=font(30, True), fill="white")
    d.text((36, 52), "motor preload -> latch release -> elastic tendon drives 1-DOF closed-chain leg", font=font(17), fill=(203, 213, 225))

    ox, oy = 450, 270

    def p(pt):
        return (ox + pt[0] * scale, oy + pt[1] * scale)

    def line(a, b, color, width=10):
        d.line([p(a), p(b)], fill=(255, 255, 255), width=width + 7)
        d.line([p(a), p(b)], fill=color, width=width)

    def joint(pt, label, r=10):
        x, y = p(pt)
        d.ellipse([x - r, y - r, x + r, y + r], fill="white", outline=COL["ink"], width=3)
        d.text((x + 12, y - 11), label, font=font(14, True), fill=COL["ink"])

    # Ground and shadow
    ground_y = oy + 720
    d.line([(75, ground_y), (1290, ground_y)], fill=(148, 163, 184), width=4)
    d.ellipse([220, ground_y - 28, 1010, ground_y + 60], fill=(226, 232, 240))

    # Body side plate.
    body = [(-125, -45), (250, -45), (330, 35), (270, 120), (-110, 130), (-165, 50)]
    body_pts = [p(x) for x in body]
    d.polygon(body_pts, fill=COL["plate"], outline=COL["edge"])
    shifted = [(x + 22, y - 14) for x, y in body_pts]
    d.line(shifted + [shifted[0]], fill=(148, 163, 184), width=2)

    A, B, F, foot = state["A"], state["B"], state["F"], state["foot"]
    tail = state["tail"]

    # Foot trajectory history.
    if len(path_history) > 1:
        pts = [p(item) for item in path_history[-55:]]
        for i in range(len(pts) - 1):
            alpha = i / max(1, len(pts) - 1)
            col = (255, int(180 - 70 * alpha), int(120 - 60 * alpha))
            d.line([pts[i], pts[i + 1]], fill=col, width=3)

    # Energy module and latch.
    motor_center = (-25, 90)
    drum_center = (-55, 80)
    servo_center = (135, 95)
    latch_center = (80, 72)
    mx, my = p(motor_center)
    d.rounded_rectangle([mx - 50, my - 28, mx + 50, my + 28], radius=7, fill=(148, 163, 184), outline=COL["edge"], width=2)
    dx, dy = p(drum_center)
    drum_angle = state["progress"] * 2 * math.pi * 4.0
    d.ellipse([dx - 28, dy - 28, dx + 28, dy + 28], fill=(147, 197, 253), outline=COL["edge"], width=3)
    d.line([dx, dy, dx + 25 * math.cos(drum_angle), dy + 25 * math.sin(drum_angle)], fill=COL["navy"], width=4)
    sx, sy = p(servo_center)
    d.rounded_rectangle([sx - 38, sy - 28, sx + 38, sy + 28], radius=6, fill=(134, 239, 172), outline=COL["edge"], width=2)
    lx, ly = p(latch_center)
    latch_col = COL["gold"] if state["phase"] != "RELEASE / EXTEND" else COL["red"]
    d.rounded_rectangle([lx - 45, ly - 13, lx + 45, ly + 13], radius=5, fill=latch_col, outline=COL["edge"], width=2)

    # Links.
    line(H1, A, COL["orange"], 9)
    line(A, B, COL["blue"], 9)
    line(H0, B, COL["purple"], 10)
    line(B, F, COL["green"], 11)
    line(H2, F, COL["teal"], 10)
    line(F, foot, COL["cyan"], 13)
    line(TAIL_ROOT, tail, (51, 65, 85), 9)

    # Elastic tendon as zig-zag.
    start = (-65, 78)
    end = B
    coils = 14 if state["phase"] == "PRELOAD" else 9
    last = p(start)
    for i in range(1, coils + 1):
        u = i / (coils + 1)
        x = start[0] * (1 - u) + end[0] * u
        y = start[1] * (1 - u) + end[1] * u
        q = p((x, y))
        q = (q[0], q[1] + (12 if i % 2 else -12))
        d.line([last, q], fill=COL["red"], width=4)
        last = q
    d.line([last, p(end)], fill=COL["red"], width=4)

    for label, pt in [("H0", H0), ("H1", H1), ("H2", H2), ("A", A), ("B", B), ("F", F), ("T", TAIL_ROOT)]:
        joint(pt, label)

    # Tail mass and foot contact.
    tx, ty = p(tail)
    d.ellipse([tx - 22, ty - 22, tx + 22, ty + 22], fill=(51, 65, 85), outline=COL["edge"], width=3)
    fx, fy = p(foot)
    d.rounded_rectangle([fx - 45, fy - 10, fx + 45, fy + 12], radius=8, fill=COL["cyan"], outline=COL["edge"], width=2)

    # Status panel.
    d.rounded_rectangle([1030, 130, 1345, 418], radius=14, fill="white", outline=(180, 190, 205), width=2)
    d.text((1055, 155), "Motion state", font=font(24, True), fill=COL["ink"])
    rows = [
        ("phase", state["phase"]),
        ("input crank", f"{state['theta']:.0f} deg"),
        ("elastic tendon", "stretched" if state["phase"] == "PRELOAD" else "releasing"),
        ("tail", "counter-pitch stabilizer"),
    ]
    yy = 205
    for k, v in rows:
        d.text((1055, yy), k, font=font(16, True), fill=COL["muted"])
        d.text((1170, yy), v, font=font(16), fill=COL["ink"])
        yy += 44

    # Cycle progress bar.
    d.rounded_rectangle([1030, 450, 1345, 525], radius=12, fill="white", outline=(180, 190, 205), width=2)
    d.text((1055, 468), "cycle progress", font=font(17, True), fill=COL["ink"])
    d.rounded_rectangle([1055, 500, 1320, 512], radius=6, fill=(226, 232, 240))
    d.rounded_rectangle([1055, 500, 1055 + 265 * state["progress"], 512], radius=6, fill=COL["blue"])

    d.text((72, 800), "Foot path is shown in orange. The mechanism is solved geometrically from the closed-chain linkage dimensions.", font=font(16), fill=COL["muted"])
    return img


def make_contact_sheet(states):
    picks = [0, 24, 44, 58, 72, 92]
    labels = ["start preload", "preloaded", "latch release", "leg extension", "flight/reset", "ready"]
    thumbs = []
    history = []
    for i, st in enumerate(states):
        history.append(st["foot"])
        if i in picks:
            frame = draw_frame(st, history, size=(700, 425), scale=1.05)
            thumbs.append(frame)
    sheet = Image.new("RGB", (1400, 1275), COL["bg"])
    d = ImageDraw.Draw(sheet)
    d.rectangle([0, 0, 1400, 80], fill=COL["navy"])
    d.text((30, 22), "Key frames of the jumping mechanism", font=font(30, True), fill="white")
    for idx, frame in enumerate(thumbs):
        x = (idx % 2) * 700
        y = 100 + (idx // 2) * 390
        sheet.paste(frame, (x, y))
        d.rounded_rectangle([x + 22, y + 22, x + 250, y + 58], radius=8, fill="white", outline=(180, 190, 205))
        d.text((x + 36, y + 30), labels[idx], font=font(16, True), fill=COL["ink"])
    sheet.save(OUT / "mechanism_animation_keyframes.png", quality=95)


def main():
    states = make_motion_states(96)
    frames = []
    history = []
    for st in states:
        history.append(st["foot"])
        frames.append(draw_frame(st, history))

    gif = OUT / "mechanism_motion_animation.gif"
    frames[0].save(
        gif,
        save_all=True,
        append_images=frames[1:],
        duration=55,
        loop=0,
        optimize=False,
        disposal=2,
    )
    # Animated WebP is often smoother/smaller in modern viewers.
    webp = OUT / "mechanism_motion_animation.webp"
    try:
        frames[0].save(webp, save_all=True, append_images=frames[1:], duration=55, loop=0, quality=90, method=6)
    except Exception:
        pass
    make_contact_sheet(states)
    print(gif)
    print(OUT / "mechanism_animation_keyframes.png")


if __name__ == "__main__":
    main()
