# 袋鼠仿生弹跳机器人重设计交付说明

本目录是一套从零重做的机械设计课程项目包。核心方案为“电机预紧式袋鼠仿生闭链弹跳机器人”：电机慢速储能，锁扣保持蹲伏状态，舵机释放，弹性肌腱瞬时驱动闭链后肢伸展，实现仿袋鼠弹跳。

## 主要文件

| 文件 | 用途 |
|---|---|
| `袋鼠仿生弹跳机器人_完整重设计报告.md` | 可直接整理成课程报告的完整方案 |
| `Inventor建模与装配指南.md` | 在 Autodesk Inventor 中建模、装配、约束的步骤 |
| `参考项目与资料.md` | GitHub/论文/公开项目参考依据 |
| `10_overall_side_mechanism_cn.png` | 总装侧视机构简图 |
| `11_dof_kinematic_proof_cn.png` | 机构自由度与闭链成立性说明 |
| `12_motor_preload_latch_module_cn.png` | 电机预紧、锁止、释放模块图 |
| `13_jump_cycle_cn.png` | 蹲伏储能到腾空落地的运动过程 |
| `14_parts_layout_cn.png` | 零件布局与制造建议 |
| `15_robustness_validation_cn.png` | 鲁棒性检测矩阵 |
| `16_estimated_simulation_results_cn.png` | 预估仿真/能量计算结果 |
| `17_dimensioned_mechanism_cn.png` | 带尺寸的机构定义图 |
| `18_mechanism_motion_principle.gif` | 机构运动原理动图 |
| `20_dimensioned_mechanism_verified_cn.png` | 推荐使用的校核版尺寸机构图 |
| `21_verified_motion_envelope_cn.gif` | 校核版闭链运动动图 |
| `verified_body_and_links_import_to_inventor.dxf` | 推荐优先导入 Inventor 的校核版 DXF |
| `body_side_plate_import_to_inventor.dxf` | 机身侧板 DXF，可导入 Inventor |
| `leg_links_and_tail_import_to_inventor.dxf` | 连杆、尾杆 DXF，可导入 Inventor |
| `mechanism_schematic_import_to_inventor.dxf` | 机构简图 DXF |
| `BOM_redesigned_kangaroo_robot.csv` | 物料清单 |
| `design_dimensions.csv` | 关键尺寸表 |
| `estimated_jump_results.csv` | 预估跳跃性能数据 |

## 一句话设计逻辑

旧方案容易被认为只是外形模仿；新版方案强调功能仿生：后肢同步、弹性储能、快速释放、尾部平衡，并用 1 自由度闭链机构保证“机构确实成立、运动可控”。

## 建议提交方式

1. 把 `袋鼠仿生弹跳机器人_完整重设计报告.md` 整理成 Word/PDF。
2. 把 `10` 到 `18` 的图片/GIF 放进答辩 PPT 或报告。
3. 在 Inventor 中导入两个 DXF，按 `Inventor建模与装配指南.md` 建立零件与装配体。
4. 实物制作前先用纸板/亚克力快速验证闭链运动，再 3D 打印或激光切割正式件。
