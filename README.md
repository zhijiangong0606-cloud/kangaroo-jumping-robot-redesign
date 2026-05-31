# Kangaroo-Inspired Jumping Robot Redesign

This repository contains a mechanical redesign package for a kangaroo-inspired jumping robot course project.

The design uses a motor-preloaded elastic jumping architecture:

1. A DC gear motor slowly winds a drum.
2. The winding drum stretches an elastic tendon.
3. A latch holds the crouched energy-storage state.
4. A servo releases the latch.
5. The elastic tendon rapidly drives a 1-DOF closed-chain hind-leg mechanism.
6. An adjustable tail rod and tail mass improve pitch stability.

## Highlights

- Verified 1-DOF planar closed-chain hind-leg mechanism
- Motor preload and latch-release energy storage module
- SolidWorks 2026 native part files and completed layout assembly
- Mechanism animation GIF/WebP
- Engineering render images, DXF files, BOM, dimensions, and validation plan

## Key Files

- `袋鼠仿生弹跳机器人_完整重设计报告.md` - full Chinese design report
- `README_交付说明.md` - Chinese delivery guide
- `solidworks_basic_model/KangarooRobot_completed_engineering_layout.SLDASM` - completed SolidWorks layout assembly
- `solidworks_basic_model/native_parts/` - SolidWorks native part files
- `solidworks_basic_model/final_completed_engineering_render_en.png` - final engineering render
- `solidworks_basic_model/mechanism_motion_animation.gif` - mechanism animation
- `solidworks_basic_model/mechanism_animation_keyframes_clean.png` - key-frame sheet
- `verified_body_and_links_import_to_inventor.dxf` - verified CAD layout DXF
- `BOM_redesigned_kangaroo_robot.csv` - bill of materials
- `verified_design_dimensions.csv` - verified design dimensions

## Core Dimensions

| Item | Value |
|---|---:|
| H0-H1 | 100 mm |
| H0-H2 | 134 mm |
| L1 H1-A | 40 mm |
| L2 A-B | 120 mm |
| L3 H0-B | 100 mm |
| L4 B-F | 140 mm |
| L5 H2-F | 180 mm |
| Tail rod | 180-220 mm adjustable |

## Mechanism Mobility

For the single-side planar linkage:

```text
M = 3(n - 1) - 2j = 3(6 - 1) - 2*7 = 1
```

This proves that the proposed single-side hind-leg mechanism is a determinate 1-DOF closed-chain mechanism.

## Recommended Electronics

- ESP32 DevKit
- 12 V DC gear motor
- TB6612FNG or BTS7960 motor driver
- MG90S or 20 g servo
- MPU6050 IMU
- Limit switches
- LM2596 buck converter
- 2S Li-ion/LiPo battery

## Notes

The SolidWorks model is a completed engineering layout model. Before fabrication, refine the parts with final motor mounting holes, servo mounting holes, bearing seats, fillets, lightening holes, and detailed latch geometry.

