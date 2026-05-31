# Autodesk Inventor 建模与装配指南

本机未检测到完整 Autodesk Inventor 安装，仅发现 DWG TrueView。因此本项目交付 DXF 与建模步骤，便于在安装 Inventor 的电脑上打开并继续建模。

## 1. 建议文件结构

```text
KangarooJumpRobot/
  Parts/
    BodySidePlate.ipt
    L1_Crank.ipt
    L2_Coupler.ipt
    L3_ThighRocker.ipt
    L4_Shank.ipt
    L5_RearRocker.ipt
    Foot.ipt
    TailRod.ipt
    WindingDrum.ipt
    Latch.ipt
  Assembly/
    KangarooJumpRobot.iam
```

## 2. 导入 DXF

可导入文件：

- `body_side_plate_import_to_inventor.dxf`
- `leg_links_and_tail_import_to_inventor.dxf`
- `mechanism_schematic_import_to_inventor.dxf`
- `verified_body_and_links_import_to_inventor.dxf`：推荐优先使用的校核版 DXF

导入步骤：

1. 打开 Inventor，新建 `Standard(mm).ipt`。
2. 选择 `导入 CAD` 或在草图中插入 DXF。
3. 确认单位为 mm。
4. 对封闭轮廓执行 `拉伸`，侧板厚度取 3-4 mm，连杆厚度取 3 mm。
5. 对所有铰接孔使用 M3 clearance，建议直径 3.2 mm。

## 3. 零件建模建议

### 3.1 机身侧板

侧板包含固定孔：

| 孔位 | 功能 |
|---|---|
| H0 | 大腿摇杆固定铰点 |
| H1 | 输入曲柄固定铰点/横轴位置 |
| H2 | 后摇杆固定铰点 |
| T | 尾杆安装点 |
| motor | 减速电机安装区 |
| servo | 释放舵机安装区 |

建议：

- 左右侧板镜像使用。
- 侧板之间使用 8-12 mm 铜柱或打印垫柱连接。
- 机身前部留电池和控制板空间。

### 3.2 连杆

连杆孔距：

| 零件 | 孔距 |
|---|---:|
| L1 曲柄 | 40 mm |
| L2 连杆 | 120 mm |
| L3 大腿摇杆 | 100 mm |
| L4 小腿杆 | 140 mm |
| L5 后摇杆 | 180 mm |
| 尾杆 | 180-220 mm |

连杆两端建议做圆角，避免应力集中。若 3D 打印，孔附近加厚到 5-6 mm；若激光切割，孔边距离保持至少 6 mm。

### 3.3 绕线轮

建议参数：

- 外径：24-36 mm。
- 线槽宽度：3-5 mm。
- 中心孔：按电机轴直径建模，常见为 3 mm 或 D 型轴。
- 侧面加小孔用于固定线绳。

### 3.4 锁扣

锁扣可采用简单棘爪式：

- 一个固定在机身上的转动棘爪。
- 一个与曲柄或收线机构连接的卡槽。
- 舵机拨杆转动 20-40° 即可脱开。

课程样机不需要复杂加工，先用 PLA 打印锁扣，后续根据磨损换成铝片或钢片。

## 4. 装配约束

装配建议顺序：

1. 插入两片 `BodySidePlate.ipt`，用垫柱约束为平行侧板。
2. 插入 H1 输入横轴，左右曲柄固定在同一轴上。
3. 单侧依次装配 L1、L2、L3、L4、L5。
4. 复制或镜像另一侧机构。
5. 装配足部，保证左右足底在同一平面。
6. 装配尾杆与尾端配重。
7. 装配电机、绕线轮、弹性件和锁扣。

关键约束：

- 所有铰链用 `Insert` 或同轴约束。
- 左右曲柄使用同一根横轴，保证相位一致。
- 足部不应与机身侧板干涉。
- 弹性件两端不要穿过连杆或侧板。

## 5. 运动检查

在 Inventor 中可用以下方式检查：

- 手动拖动输入曲柄，观察全行程是否卡死。
- 打开接触集，检查足部和机身是否干涉。
- 使用驱动约束让 H1 曲柄转动 0-90°，观察 F 点轨迹。
- 若出现死点，优先调整 H2 位置或 L5 后摇杆长度。

## 6. 出图建议

课程报告至少建议输出：

- 总装侧视图。
- 单侧机构简图。
- 机身侧板工程图。
- 连杆工程图。
- 储能锁止模块局部放大图。
- BOM 表。
