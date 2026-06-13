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

## Final Deliverables (latest)

- **`袋鼠仿生弹跳机器人_设计任务书与说明书.md`** — course design task & specification document (12 sections, Chinese). Primary submission.
- **`solidworks_basic_model/kangaroo_overleap_bioinspired_v1/`** — final trustworthy 3D SolidWorks assembly:
  - `Kangaroo_Overleap_BioInspired_Assembly.SLDASM` — main assembly (16 native parts, 31 instances)
  - `parts/` — 16 native `.SLDPRT` files
  - `model_readme.md` — how the model was built and how to verify it
  - `parts_list.csv` — model parts list
  - `final_render.png` / `verify_multiview.png` — render + 4-view verification
  - `assembly_export.STL` — merged whole-assembly STL
  - `solve_mechanism.py` + `mechanism_pose.json` — geometry solver (all link-length constraints met to machine precision)

The new assembly is **geometry-solved first, then modeled**: every joint physically connects (shared M3 axles), the chain is a real closed loop, and the export bounding box is X≈417 × Y≈231 × Z≈52 mm — the 52 mm depth confirms it is a true 3D body, not a flat plate.

## Other Files

- `袋鼠仿生弹跳机器人_完整重设计报告.md` - full Chinese design report
- `README_交付说明.md` - Chinese delivery guide
- `solidworks_basic_model/mechanism_motion_animation.gif` - mechanism principle animation
- `solidworks_basic_model/mechanism_animation_keyframes_clean.png` - key-frame sheet
- `manufacturing_package/` - buildability-improved DXF files and fabrication checklist
- `external_references/overleap/` - Overleap open-source jumping leg (MIT), used as a CAD/assembly reference
- `MODEL_OPERABILITY_AUDIT.md` - CAD/model operability audit and improvement notes
- `BOM_redesigned_kangaroo_robot.csv` - bill of materials
- `verified_design_dimensions.csv` - verified design dimensions

## Historical Models (superseded, kept for reference only)

The earlier flat-looking assemblies are **not** the final deliverable. They had links that did not satisfy the closed-chain length constraints, a foot placed above the body, and no shared pivot axles, so parts appeared to float:

- `solidworks_basic_model/KangarooRobot_completed_engineering_layout.SLDASM`
- `solidworks_basic_model/KangarooRobot_TRUE_3D_assembly.SLDASM`
- `solidworks_basic_model/KangarooRobot_TRUE_3D_FIXED_assembly.SLDASM`

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

For fabrication-oriented files, use the new `manufacturing_package/` folder. It adds manufacturing DXFs with lightening holes, M3 pivot references, bearing reference circles, tendon anchor holes, motor/servo mounting references, and an assembly checklist.

