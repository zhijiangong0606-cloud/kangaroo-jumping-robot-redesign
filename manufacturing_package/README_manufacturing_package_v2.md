# Manufacturing Package v2

This package improves the practical buildability of the kangaroo-inspired jumping robot.

## Files

- `body_side_plate_manufacturing_v2.dxf`: body side plate with pivot holes, motor/servo mounting placeholders, tendon anchors, and lightening holes.
- `links_manufacturing_v2.dxf`: link cutting layout with M3 holes, bearing reference circles, lightening holes, and tendon hook points.
- `manufacturing_checklist.csv`: fabrication and assembly checklist.

## Important Practical Changes

1. Pivot holes now include both M3 clearance holes and bearing-clearance reference circles.
2. Body side plate includes motor, servo, and tendon anchor references.
3. Long links include lightening holes.
4. L3/L4 include tendon hook points for rubber band or extension spring attachment.
5. The package separates manufacturing DXF files from the earlier conceptual CAD files.

## Suggested Fabrication

- First prototype: 3 mm acrylic or 3D printed PLA.
- Stronger version: 2-3 mm aluminum links and 3-4 mm carbon/acrylic body plates.
- Pivots: M3 bolts with washers and locknuts.
- Optional bearings: select bearings first, then resize the bearing reference circles.

## Assembly Notes

- Keep left and right legs synchronized with a shared H1 input shaft.
- Add washers between links to avoid rubbing.
- Start elastic preload at 20 mm before testing larger preload values.
- Use a limit switch or current limit to prevent motor over-winding.
- Test tail masses at 0 g, 15 g, and 30 g.
