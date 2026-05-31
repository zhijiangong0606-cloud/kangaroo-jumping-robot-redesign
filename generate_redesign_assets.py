from PIL import Image, ImageDraw, ImageFont
from pathlib import Path
import csv
import math

OUT = Path("C:/Users/Gzj/Desktop/kangaroo_robot_redesign")
OUT.mkdir(parents=True, exist_ok=True)

FONT_REG = "C:/Windows/Fonts/msyh.ttc"
FONT_BOLD = "C:/Windows/Fonts/msyhbd.ttc"


def font(size, bold=False):
    path = FONT_BOLD if bold else FONT_REG
    try:
        return ImageFont.truetype(path, size)
    except Exception:
        return ImageFont.load_default()


COLORS = {
    "bg": (247, 249, 250),
    "ink": (32, 38, 46),
    "muted": (96, 108, 122),
    "blue": (37, 99, 235),
    "green": (22, 163, 74),
    "orange": (234, 88, 12),
    "red": (220, 38, 38),
    "purple": (124, 58, 237),
    "panel": (255, 255, 255),
    "line": (172, 184, 198),
}


def canvas(title, subtitle=None, size=(1600, 1000)):
    img = Image.new("RGB", size, COLORS["bg"])
    d = ImageDraw.Draw(img)
    d.rectangle([0, 0, size[0], 92], fill=(24, 33, 45))
    d.text((42, 22), title, font=font(34, True), fill=(255, 255, 255))
    if subtitle:
        d.text((44, 62), subtitle, font=font(17), fill=(203, 213, 225))
    return img, d


def text_box(d, xy, text, width, fnt, fill=COLORS["ink"], line_spacing=6):
    x, y = xy
    lines = []
    for para in str(text).split("\n"):
        cur = ""
        for ch in para:
            test = cur + ch
            if d.textbbox((0, 0), test, font=fnt)[2] <= width:
                cur = test
            else:
                if cur:
                    lines.append(cur)
                cur = ch
        lines.append(cur)
    yy = y
    for line in lines:
        d.text((x, yy), line, font=fnt, fill=fill)
        yy += fnt.size + line_spacing
    return yy


def rounded(d, box, fill, outline=None, radius=12, width=2):
    d.rounded_rectangle(box, radius=radius, fill=fill, outline=outline, width=width)


def arrow(d, p1, p2, fill, width=5, head=16):
    d.line([p1, p2], fill=fill, width=width)
    ang = math.atan2(p2[1] - p1[1], p2[0] - p1[0])
    pts = [
        p2,
        (p2[0] - head * math.cos(ang - 0.45), p2[1] - head * math.sin(ang - 0.45)),
        (p2[0] - head * math.cos(ang + 0.45), p2[1] - head * math.sin(ang + 0.45)),
    ]
    d.polygon(pts, fill=fill)


def joint(d, p, label, fill=(255, 255, 255), outline=COLORS["ink"]):
    x, y = p
    d.ellipse([x - 13, y - 13, x + 13, y + 13], fill=fill, outline=outline, width=3)
    d.text((x + 16, y - 12), label, font=font(18, True), fill=outline)


def link(d, p1, p2, fill, width=15):
    d.line([p1, p2], fill=(255, 255, 255), width=width + 7)
    d.line([p1, p2], fill=fill, width=width)


def mechanism_points(offset=(0, 0), scale=1.6):
    ox, oy = offset
    pts = {
        "H0": (0, 0),
        "H1": (-100, 70),
        "H2": (170, 70),
        "A": (-74, 120),
        "B": (90, 125),
        "F": (300, 245),
        "Foot": (390, 260),
        "T": (250, -25),
        "Tail": (470, -75),
    }
    return {k: (int(ox + v[0] * scale), int(oy + v[1] * scale)) for k, v in pts.items()}


def draw_robot_side(filename):
    img, d = canvas("袋鼠仿生弹跳机器人：总装侧视机构简图", "电机慢速预紧 + 锁止释放 + 闭链后肢快速伸展")
    rounded(d, [42, 125, 1558, 928], COLORS["panel"], COLORS["line"], 18)
    pts = mechanism_points((470, 365), 1.45)

    # Body shell
    body = [(pts["H1"][0] - 80, pts["H1"][1] - 95), (pts["H2"][0] + 100, pts["H2"][1] - 85),
            (pts["T"][0] + 80, pts["T"][1] + 62), (pts["H0"][0] - 130, pts["H0"][1] + 100)]
    d.polygon(body, fill=(235, 239, 245), outline=(96, 108, 122))
    d.text((pts["H1"][0] - 58, pts["H1"][1] - 78), "机身侧板 / 电池 / 控制器", font=font(22, True), fill=COLORS["ink"])

    # Mechanism links
    link(d, pts["H1"], pts["A"], COLORS["orange"], 14)
    link(d, pts["A"], pts["B"], COLORS["blue"], 14)
    link(d, pts["H0"], pts["B"], COLORS["purple"], 15)
    link(d, pts["B"], pts["F"], COLORS["green"], 16)
    link(d, pts["H2"], pts["F"], (14, 116, 144), 14)
    link(d, pts["F"], pts["Foot"], (56, 189, 248), 18)
    d.line([pts["T"], pts["Tail"]], fill=(31, 41, 55), width=14)
    d.ellipse([pts["Tail"][0]-28, pts["Tail"][1]-28, pts["Tail"][0]+28, pts["Tail"][1]+28], fill=(71, 85, 105))

    for name in ["H0", "H1", "H2", "A", "B", "F", "T"]:
        joint(d, pts[name], name)

    # Spring tendon
    start = (pts["H1"][0] - 42, pts["H1"][1] + 18)
    end = (pts["B"][0] + 22, pts["B"][1] + 26)
    last = start
    for i in range(1, 16):
        t = i / 16
        x = start[0] + (end[0] - start[0]) * t
        y = start[1] + (end[1] - start[1]) * t + (12 if i % 2 else -12)
        d.line([last, (x, y)], fill=COLORS["red"], width=4)
        last = (x, y)
    d.line([last, end], fill=COLORS["red"], width=4)
    d.text((start[0]-105, start[1]+35), "弹性肌腱\n橡皮筋/拉簧", font=font(20, True), fill=COLORS["red"])

    # Energy module
    rounded(d, [1015, 180, 1490, 405], (248, 250, 252), COLORS["line"], 12)
    d.text((1040, 205), "驱动与释放模块", font=font(26, True), fill=COLORS["ink"])
    bullets = [
        "减速电机 + 绕线轮：慢速拉伸弹性件",
        "棘爪/锁扣：保持蹲伏储能状态",
        "微型舵机：触发释放，完成瞬时起跳",
        "左右腿横轴同步，避免单侧偏转",
    ]
    y = 250
    for b in bullets:
        d.ellipse([1046, y+6, 1056, y+16], fill=COLORS["blue"])
        y = text_box(d, (1068, y), b, 380, font(19), COLORS["ink"], 4) + 5

    rounded(d, [1015, 455, 1490, 805], (248, 250, 252), COLORS["line"], 12)
    d.text((1040, 480), "关键仿生对应", font=font(26, True), fill=COLORS["ink"])
    rows = [
        ("袋鼠跟腱储能", "拉簧/橡皮筋预紧储能"),
        ("后肢同步蹬伸", "左右闭链腿共轴释放"),
        ("髋-膝-踝联动", "1 自由度六杆闭链"),
        ("尾部平衡", "可调尾杆与配重"),
    ]
    y = 530
    for a, b in rows:
        d.text((1045, y), a, font=font(19, True), fill=COLORS["muted"])
        arrow(d, (1185, y+15), (1235, y+15), COLORS["line"], 3, 10)
        d.text((1255, y), b, font=font(19), fill=COLORS["ink"])
        y += 60

    img.save(OUT / filename, quality=95)


def draw_kinematic(filename):
    img, d = canvas("机构成立性：单侧六杆闭链后肢", "用自由度计算回应“无法构成机构”的问题")
    rounded(d, [50, 130, 890, 900], COLORS["panel"], COLORS["line"], 18)
    pts = mechanism_points((400, 360), 1.45)
    link(d, pts["H1"], pts["A"], COLORS["orange"], 12)
    link(d, pts["A"], pts["B"], COLORS["blue"], 12)
    link(d, pts["H0"], pts["B"], COLORS["purple"], 13)
    link(d, pts["B"], pts["F"], COLORS["green"], 13)
    link(d, pts["H2"], pts["F"], (14, 116, 144), 12)
    d.line([pts["H0"], pts["H1"], pts["H2"], pts["T"]], fill=(130, 142, 158), width=5)
    for name in ["H0", "H1", "H2", "A", "B", "F"]:
        joint(d, pts[name], name)
    d.text((110, 800), "L0 机架含 H0/H1/H2；L1-L5 为五个活动杆件", font=font(23, True), fill=COLORS["ink"])

    rounded(d, [940, 130, 1540, 900], COLORS["panel"], COLORS["line"], 18)
    d.text((980, 175), "自由度计算", font=font(34, True), fill=COLORS["ink"])
    d.text((980, 240), "平面低副机构：", font=font(24), fill=COLORS["muted"])
    d.text((980, 295), "M = 3(n - 1) - 2j", font=font(42, True), fill=COLORS["blue"])
    calc = [
        "n = 6：机架 + 5 个活动构件",
        "j = 7：H0, H1, H2, A, B, F 等转动副",
        "M = 3 × (6 - 1) - 2 × 7 = 1",
        "结论：单侧机构为确定运动的一自由度闭链机构",
    ]
    y = 380
    for line in calc:
        d.ellipse([992, y+8, 1006, y+22], fill=COLORS["green"])
        y = text_box(d, (1024, y), line, 470, font(24), COLORS["ink"], 8) + 16
    rounded(d, [980, 700, 1500, 830], (239, 246, 255), (147, 197, 253), 10)
    text_box(d, (1010, 730), "设计含义：电机只需控制一个输入量，左右腿用横轴同步后即可形成稳定的蹲伏-释放-伸展循环。", 460, font(22, True), COLORS["blue"])
    img.save(OUT / filename, quality=95)


def draw_energy_module(filename):
    img, d = canvas("电机预紧式弹性储能/锁止释放模块", "电机提供能量输入，弹性件提供瞬时峰值功率")
    rounded(d, [55, 135, 1545, 905], COLORS["panel"], COLORS["line"], 18)
    # Main components
    d.rounded_rectangle([150, 365, 340, 500], radius=16, fill=(226, 232, 240), outline=COLORS["ink"], width=3)
    d.text((190, 405), "减速电机", font=font(28, True), fill=COLORS["ink"])
    d.ellipse([430, 340, 590, 500], fill=(219, 234, 254), outline=COLORS["blue"], width=5)
    d.ellipse([482, 392, 538, 448], fill=(255,255,255), outline=COLORS["blue"], width=3)
    d.text((455, 515), "绕线轮 r=12-18mm", font=font(22, True), fill=COLORS["blue"])
    arrow(d, (340, 432), (430, 432), COLORS["muted"], 5)
    # string and spring
    d.line([(590, 420), (790, 420)], fill=COLORS["ink"], width=4)
    x0, y0, x1 = 790, 420, 1060
    last = (x0, y0)
    for i in range(1, 22):
        x = x0 + (x1-x0)*i/22
        y = y0 + (18 if i%2 else -18)
        d.line([last, (x,y)], fill=COLORS["red"], width=5)
        last = (x,y)
    d.line([last, (1125,420)], fill=COLORS["red"], width=5)
    d.text((825, 500), "弹性肌腱：拉簧/橡皮筋束", font=font(24, True), fill=COLORS["red"])
    # latch
    d.polygon([(1110,390),(1190,420),(1110,450)], fill=(254, 215, 170), outline=COLORS["orange"])
    d.rectangle([1190, 370, 1245, 470], fill=(255, 237, 213), outline=COLORS["orange"], width=3)
    d.text((1140, 335), "锁扣", font=font(25, True), fill=COLORS["orange"])
    d.rounded_rectangle([1315, 350, 1465, 500], radius=14, fill=(220, 252, 231), outline=COLORS["green"], width=4)
    d.text((1342, 405), "释放舵机", font=font(26, True), fill=COLORS["green"])
    arrow(d, (1315, 425), (1245, 425), COLORS["green"], 5)

    # Process
    y = 650
    steps = [
        ("1 预紧", "电机低速转动绕线轮，拉伸弹性件，使后肢进入蹲伏储能位。"),
        ("2 锁止", "棘爪/锁扣承受弹性件拉力，电机断电也能保持储能。"),
        ("3 释放", "舵机拨开锁扣，弹性件瞬时收缩并驱动闭链后肢快速伸展。"),
        ("4 复位", "落地后电机重新收线，准备下一次跳跃。"),
    ]
    x = 150
    for title, desc in steps:
        rounded(d, [x, y, x+310, y+155], (248, 250, 252), COLORS["line"], 12)
        d.text((x+22, y+18), title, font=font(27, True), fill=COLORS["blue"])
        text_box(d, (x+22, y+62), desc, 260, font(19), COLORS["ink"], 5)
        x += 350
    img.save(OUT / filename, quality=95)


def draw_jump_cycle(filename):
    img, d = canvas("运动过程：蹲伏储能 - 释放蹬伸 - 腾空 - 落地复位", "四阶段展示袋鼠后肢弹性跳跃机理")
    stages = [
        ("A 蹲伏储能", 0.85, "机身下降，弹性肌腱被拉长"),
        ("B 触发释放", 1.00, "锁扣打开，闭链腿开始伸展"),
        ("C 快速蹬伸", 1.25, "足端向后下方推地，获得起跳速度"),
        ("D 腾空/落地", 1.05, "尾部调姿，足端缓冲准备复位"),
    ]
    for i, (title, sc, desc) in enumerate(stages):
        x0 = 70 + i * 380
        rounded(d, [x0, 150, x0+330, 850], COLORS["panel"], COLORS["line"], 14)
        d.text((x0+24, 180), title, font=font(28, True), fill=COLORS["ink"])
        base_y = 560 - (i == 3) * 80
        pts = mechanism_points((x0+110, base_y), sc)
        link(d, pts["H1"], pts["A"], COLORS["orange"], 8)
        link(d, pts["A"], pts["B"], COLORS["blue"], 8)
        link(d, pts["H0"], pts["B"], COLORS["purple"], 9)
        link(d, pts["B"], pts["F"], COLORS["green"], 9)
        link(d, pts["H2"], pts["F"], (14,116,144), 8)
        link(d, pts["F"], pts["Foot"], (56,189,248), 10)
        d.line([pts["T"], pts["Tail"]], fill=(31,41,55), width=8)
        d.line([(x0+35, 720), (x0+300, 720)], fill=(148,163,184), width=4)
        if i == 2:
            arrow(d, (pts["F"][0]+20, pts["F"][1]+40), (pts["F"][0]+80, pts["F"][1]+95), COLORS["red"], 5)
            d.text((x0+170, 610), "推地力", font=font(19, True), fill=COLORS["red"])
        text_box(d, (x0+24, 760), desc, 280, font(20), COLORS["muted"])
    img.save(OUT / filename, quality=95)


def draw_parts_layout(filename):
    img, d = canvas("零件布局与制造建议", "以 3D 打印/亚克力激光切割为主，适合本科课程制作")
    rounded(d, [60, 135, 1540, 900], COLORS["panel"], COLORS["line"], 18)
    parts = [
        ("机身侧板 x2", (130, 210), (430, 300), COLORS["blue"], "3-4mm 亚克力/PLA；开 H0/H1/H2/T 与电机孔"),
        ("曲柄 L1 x2", (530, 225), (180, 36), COLORS["orange"], "孔距 28mm，连接输入横轴与 A 点"),
        ("连杆 L2 x2", (820, 225), (250, 36), COLORS["blue"], "孔距 105mm，传递曲柄运动"),
        ("大腿摇杆 L3 x2", (1190, 225), (230, 38), COLORS["purple"], "孔距 95mm，形成髋-膝联动"),
        ("小腿 L4 x2", (160, 455), (280, 42), COLORS["green"], "孔距 115mm，末端接足部"),
        ("后摇杆 L5 x2", (560, 455), (330, 36), (14,116,144), "孔距 150mm，限制足端轨迹"),
        ("足部 x2", (1010, 450), (300, 54), (56,189,248), "带橡胶垫，增大触地摩擦并缓冲"),
        ("尾杆 + 配重", (180, 680), (430, 32), (31,41,55), "尾长 180-220mm，尾端 0-30g 可调"),
        ("绕线轮/锁扣", (780, 650), (280, 115), COLORS["red"], "电机预紧、棘爪锁止、舵机释放"),
        ("横轴/垫柱", (1180, 650), (250, 105), COLORS["muted"], "M3 螺栓 + 8-12mm 垫柱保证双侧平行"),
    ]
    for name, (x,y), (w,h), col, note in parts:
        rounded(d, [x, y, x+w, y+h], tuple(min(255, c+35) for c in col), col, 10, 3)
        d.text((x+14, y+8), name, font=font(22, True), fill=COLORS["ink"])
        text_box(d, (x+14, y+h+14), note, w+20, font(17), COLORS["muted"], 4)
        if "x2" in name:
            d.text((x+w-44, y+8), "×2", font=font(20, True), fill=COLORS["ink"])
    img.save(OUT / filename, quality=95)


def draw_robustness(filename):
    img, d = canvas("鲁棒性检测方案：面向课程答辩的验证矩阵", "数据不足处可先估测，实测后替换")
    rounded(d, [55, 135, 1545, 905], COLORS["panel"], COLORS["line"], 18)
    cols = [80, 330, 630, 930, 1220, 1500]
    headers = ["检测项目", "扰动/变量", "测量指标", "判定标准", "输出图表"]
    y0 = 190
    for i, h in enumerate(headers):
        d.rectangle([cols[i], y0, cols[i+1], y0+58], fill=(30, 41, 59), outline=(226,232,240))
        d.text((cols[i]+14, y0+15), h, font=font(20, True), fill=(255,255,255))
    rows = [
        ("机构运动", "输入角 0-90°", "是否卡死、足端轨迹", "全程无干涉", "轨迹曲线"),
        ("起跳性能", "预紧量 20/30/40mm", "跳高、跳远、起跳角", "高度随预紧增大", "柱状图"),
        ("落地稳定", "尾重 0/15/30g", "俯仰角峰值", "无明显翻转", "角度-时间"),
        ("结构强度", "连杆 5-20N 载荷", "最大应力/变形", "安全系数 > 1.5", "云图"),
        ("制造误差", "孔距 ±1mm", "跳高波动", "波动 < 20%", "敏感性图"),
        ("摩擦影响", "橡胶/光滑足垫", "打滑距离", "低打滑更优", "对比表"),
    ]
    y = y0 + 58
    row_h = 90
    for r, row in enumerate(rows):
        fill = (248,250,252) if r % 2 else (255,255,255)
        for i, txt in enumerate(row):
            d.rectangle([cols[i], y, cols[i+1], y+row_h], fill=fill, outline=(226,232,240))
            text_box(d, (cols[i]+12, y+18), txt, cols[i+1]-cols[i]-24, font(18, i==0), COLORS["ink"], 4)
        y += row_h
    rounded(d, [95, 790, 1460, 865], (240,253,244), (134,239,172), 10)
    text_box(d, (120, 812), "建议实验最小集：每个预紧量重复 5 次，用手机慢动作视频标定跳高；把估测数据标注为“预估值”，答辩前替换成实测均值和标准差。", 1300, font(22, True), COLORS["green"], 5)
    img.save(OUT / filename, quality=95)


def dxf_header():
    return ["0","SECTION","2","ENTITIES"]


def dxf_footer():
    return ["0","ENDSEC","0","EOF"]


def add_line(lines, x1, y1, x2, y2, layer="0"):
    lines += ["0","LINE","8",layer,"10",f"{x1:.3f}","20",f"{y1:.3f}","30","0","11",f"{x2:.3f}","21",f"{y2:.3f}","31","0"]


def add_circle(lines, x, y, r, layer="0"):
    lines += ["0","CIRCLE","8",layer,"10",f"{x:.3f}","20",f"{y:.3f}","30","0","40",f"{r:.3f}"]


def add_polyline(lines, pts, closed=True, layer="0"):
    for a, b in zip(pts, pts[1:] + ([pts[0]] if closed else [])):
        add_line(lines, a[0], a[1], b[0], b[1], layer)


def write_dxf(name, lines):
    (OUT / name).write_text("\n".join(lines), encoding="ascii")


def create_dxf_files():
    # body side plate in mm
    lines = dxf_header()
    outline = [(0,0),(285,0),(330,55),(290,125),(35,135),(-15,85)]
    add_polyline(lines, outline, True, "OUTLINE")
    holes = {"H1":(45,72), "H0":(145,2), "H2":(315,72), "T":(290,-25), "motor":(115,78), "servo":(225,95)}
    for name, (x,y) in holes.items():
        add_circle(lines, x, y, 2.0 if name not in ["motor","servo"] else 6.0, "HOLES")
    # motor mount slot holes
    for x,y in [(100,62),(130,62),(100,94),(130,94)]:
        add_circle(lines, x,y,1.8,"MOUNT")
    lines += dxf_footer()
    write_dxf("body_side_plate_import_to_inventor.dxf", lines)

    # individual links
    lines = dxf_header()
    specs = [
        ("L1_crank_28", 28, 0, 0),
        ("L2_coupler_105", 105, 0, 45),
        ("L3_thigh_95", 95, 0, 90),
        ("L4_shank_115", 115, 0, 135),
        ("L5_rear_rocker_150", 150, 0, 180),
        ("tail_rod_210", 210, 0, 235),
    ]
    for name, dist, ox, oy in specs:
        w = 16 if dist < 120 else 18
        add_polyline(lines, [(ox,oy-w/2),(ox+dist,oy-w/2),(ox+dist+8,oy),(ox+dist,oy+w/2),(ox,oy+w/2),(-8+ox,oy)], True, name)
        add_circle(lines, ox,oy,2.0,name)
        add_circle(lines, ox+dist,oy,2.0,name)
    lines += dxf_footer()
    write_dxf("leg_links_and_tail_import_to_inventor.dxf", lines)


def write_tables():
    dims = [
        ("H0-H1 机架孔距", "120", "mm", "固定轴距，建议先按 DXF 调整"),
        ("H0-H2 机架孔距", "170", "mm", "闭链基准长度"),
        ("L1 曲柄 H1-A", "28", "mm", "输入曲柄，决定蹲伏幅度"),
        ("L2 连杆 A-B", "105", "mm", "传递输入"),
        ("L3 大腿摇杆 H0-B", "95", "mm", "仿生股骨段"),
        ("L4 小腿 B-F", "115", "mm", "仿生胫骨/跖骨段"),
        ("L5 后摇杆 H2-F", "150", "mm", "约束足端轨迹"),
        ("尾杆", "180-220", "mm", "通过尾端配重调节俯仰"),
        ("弹性预紧量", "20-45", "mm", "先小后大，逐步测试"),
        ("绕线轮半径", "12-18", "mm", "影响电机扭矩需求"),
    ]
    with (OUT / "design_dimensions.csv").open("w", newline="", encoding="utf-8-sig") as f:
        w = csv.writer(f)
        w.writerow(["项目", "数值", "单位", "说明"])
        w.writerows(dims)


if __name__ == "__main__":
    draw_robot_side("10_overall_side_mechanism_cn.png")
    draw_kinematic("11_dof_kinematic_proof_cn.png")
    draw_energy_module("12_motor_preload_latch_module_cn.png")
    draw_jump_cycle("13_jump_cycle_cn.png")
    draw_parts_layout("14_parts_layout_cn.png")
    draw_robustness("15_robustness_validation_cn.png")
    create_dxf_files()
    write_tables()
    print("Generated redesign assets in", OUT)
