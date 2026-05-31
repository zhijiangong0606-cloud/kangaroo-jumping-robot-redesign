# SolidWorks Basic Model Package

This folder contains a basic 3D model package for the kangaroo-inspired jumping robot.

All STL files use millimeters. The fastest way to inspect the model is to open:

- `kangaroo_robot_basic_assembly.stl`

Native SolidWorks base parts have also been generated in:

- `native_parts/*.SLDPRT`

Use these `SLDPRT` files as the main editable modeling base.

Main modeled modules:

- left/right body side plates
- left/right five-link closed-chain hind legs
- foot pads
- adjustable tail rod
- M3 joint axle placeholders
- DC gear motor placeholder
- winding drum
- release servo placeholder
- latch placeholder

Recommended verified dimensions:

| Item | Value |
|---|---:|
| H0-H1 | 100 mm |
| H0-H2 | 134 mm |
| L1 H1-A | 40 mm |
| L2 A-B | 120 mm |
| L3 H0-B | 100 mm |
| L4 B-F | 140 mm |
| L5 H2-F | 180 mm |
| Tail rod | 180-220 mm |

Suggested SolidWorks workflow:

1. Open `kangaroo_robot_basic_assembly.stl` for quick visual checking.
2. Open the generated files in `native_parts`.
3. Create a new SolidWorks assembly and insert the native parts.
4. Use `assembly_placement_table.csv` and `SolidWorks_assembly_manual.md` as the layout reference.
5. Use concentric mates on H0/H1/H2/A/B/F/T to build the final moving assembly.

The API-generated `KangarooRobot_basic_layout.SLDASM` is only a placeholder assembly because automatic component insertion was not reliable in the current COM environment. The part files are valid and are the main deliverable.
