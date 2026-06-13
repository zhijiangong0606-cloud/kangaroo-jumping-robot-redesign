# Kangaroo Bio-Inspired Jumper — SolidWorks 3D Assembly (v1)

可在 SolidWorks 2026 中打开检查的三维装配。本模型是项目最终交付的三维建模成果，取代旧的平板示意装配。

## 主装配

- `Kangaroo_Overleap_BioInspired_Assembly.SLDASM`

包含 31 个零件实例（16 个唯一零件），全部为 SolidWorks 原生实体零件（非网格导入），在求解后的“蹲伏储能”位姿下装配。

## 为什么这一版是可信的三维装配

旧装配（`KangarooRobot_TRUE_3D_*` 等）的问题在于：连杆坐标并未真正满足闭链杆长约束，足端被放到了机身上方，部件之间没有共享铰轴，因此看起来像悬空的平板。

本版本的不同点：

1. **几何先解算，再建模。** `solve_mechanism.py` 用圆-圆交点法解出闭链的真实位姿，五个杆长约束全部满足到机器精度（误差 < 1e-13 mm）：

   | 约束 | 目标 | 解算值 |
   |---|---:|---:|
   | H1-A (L1) | 40 mm | 40.000 |
   | A-B (L2) | 120 mm | 120.000 |
   | H0-B (L3) | 100 mm | 100.000 |
   | B-F (L4) | 140 mm | 140.000 |
   | H2-F (L5) | 180 mm | 180.000 |

   选用曲柄角 θ = -121°，足端 F 落在机身下方约 134 mm（蹲伏储能姿态）。

2. **零件原点定义在铰点上。** 每个连杆零件的局部原点在第一个铰孔，杆体沿 +X 伸到第二个铰孔。装配时按解算坐标平移 + 绕 Z 旋转放置，铰孔自然对齐，因此关节真正连在一起。

3. **真三维厚度。** 所有零件用 Front 平面草图 + 中面拉伸生成，关于自身中面对称。左右后肢镜像在 z = ±15 mm，两块侧板在 z = ±24 mm，导出 STL 的包围盒为 X≈417 × Y≈231 × Z≈52 mm —— Z 向 52 mm 的厚度证明这是立体结构而非平板。

4. **M3 轴 + 立柱贯穿。** 7 根 M3 轴穿过 H0/H1/H2/A/B/F/T 各铰点把左右后肢连成一体；4 根立柱把两块侧板撑开并固定，机身不再松散。

## 仿生改型（在 Overleap 结构逻辑之上）

参考开源项目 Overleap（MIT，`external_references/overleap`）的“侧板 + 腿 + 足 + 电机/轴/支架”组织方式，但本项目是按袋鼠弹性储能机理做的改型，不是直接复用其带传动腿：

- 后肢用 **一自由度六杆闭链**（Overleap 是准直驱带传动），保留课程要求的机构拓扑。
- 增加 **弹性肌腱 + 绕线轮**：电机慢速收线储能。
- 增加 **舵机 + 锁扣**：保持蹲伏储能并瞬时释放。
- 增加 **袋鼠仿生尾杆 + 尾端配重**：向后下方伸出，作为俯仰平衡 / 三点支撑。

## 零件清单

见 `parts_list.csv`。

## 渲染与验证图

- `final_render.png` —— 等轴测着色渲染。
- `verify_multiview.png` —— 等轴测 / 右视 / 前视 / 顶视四视图，用于核对“无悬空、确为三维”。
- `assembly_export.STL` —— 整机合并 STL（3972 三角面），用于外部查看与体积/包围盒核对。

## 复现 / 重建方法

脚本均在本目录，依赖已就绪（csc.exe + 同目录 Interop DLL + Python + matplotlib）。

```bash
# 1. 解算机构位姿（写出 mechanism_pose.json）
python solve_mechanism.py

# 2. 生成 16 个原生零件（启动 SolidWorks）
./CreateBioInspiredParts.exe

# 3. 装配并保存 .SLDASM
./CreateBioInspiredAssembly.exe

# 4. 导出整机 STL
./ExportStl.exe

# 5. 渲染验证图
python render_check.py     # verify_multiview.png
python render_final.py     # final_render.png
```

C# 重新编译（如需修改）：

```bash
csc -nologo -platform:x64 \
  -reference:SolidWorks.Interop.sldworks.dll \
  -reference:SolidWorks.Interop.swconst.dll \
  -out:CreateBioInspiredParts.exe CreateBioInspiredParts.cs
```

## 在 SolidWorks 中检查的建议步骤

1. 打开 `Kangaroo_Overleap_BioInspired_Assembly.SLDASM`，若提示重建可点“重建”。
2. 切换等轴测，确认机身、后肢、足、尾、电机、舵机、锁扣、弹性件位置合理且无明显悬空。
3. 用“评估 → 干涉检查”查看部件干涉（占位电机/舵机为外形包络，与连杆的轻微重叠属正常占位，可按需要微调安装孔）。
4. 各铰点处的孔已对齐，可在 H0/H1/H2/A/B/F 添加同心配合后用 H1 曲柄角驱动做机构运动检查。

## 已知简化（课程模型范围内）

- 电机 / 舵机 / 锁扣为**外形包络占位**，购得实物后按实际安装孔替换。
- 关节为通孔，未建轴承座；实物建议加 M3 螺栓 + 垫片，必要时加微型轴承。
- 弹性肌腱以直杆代表其工作长度，实物为拉簧或橡皮筋束。

这些简化不影响三维装配的可信度与机构拓扑的正确性，属于从“可信三维装配”到“可制造细化”之间的正常后续工作。
