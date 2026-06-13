# Model Operability Audit

This audit records the current state of the CAD model and the changes made to improve buildability.

## Current SolidWorks Model Status

Main assembly:

- `solidworks_basic_model/KangarooRobot_completed_engineering_layout.SLDASM`

The assembly is suitable for:

- explaining the mechanism architecture
- showing module placement
- demonstrating the relation between motor preload, latch release, elastic tendon, tail stabilizer, and closed-chain hind leg
- visual presentation and course reporting

The assembly is not yet a final production drawing because:

- link parts are simplified capsule links
- bearing seats are not fully parameterized
- latch geometry is still a placeholder
- motor and servo mounting patterns are generic placeholders
- no full SolidWorks mate set has been added for dynamic simulation

## Practical Issues Found

| Issue | Impact | Fix / Mitigation |
|---|---|---|
| Simplified link geometry | Buildable, but not optimized for weight or bearings | Added manufacturing DXF with lightening holes and bearing reference circles |
| Placeholder motor and servo | Actual purchased parts may not match | Added adjustable mounting reference holes and noted final resizing requirement |
| Placeholder latch | Cannot be fabricated directly | Keep latch as functional location reference; redesign after choosing servo horn and latch material |
| No detailed washers/spacers in early model | Links may rub during physical assembly | Added spacer part and manufacturing checklist |
| Elastic tendon path is conceptual | May rub or over-stretch | Added tendon anchor holes and hook points in manufacturing DXF |
| Tail module was only visual | Needs parameter test | Added tail mass placeholder and 0/15/30 g validation plan |

## New Buildability Package

New folder:

- `manufacturing_package/`

Files:

- `body_side_plate_manufacturing_v2.dxf`
- `links_manufacturing_v2.dxf`
- `manufacturing_checklist.csv`
- `README_manufacturing_package_v2.md`

These files are intended for laser cutting, 2D CAD cleanup, or importing into SolidWorks as sketches.

## Recommended Next CAD Step

If more SolidWorks refinement is needed, do it in this order:

1. Import `body_side_plate_manufacturing_v2.dxf` into SolidWorks.
2. Rebuild the body plate as an editable sketch.
3. Import `links_manufacturing_v2.dxf`.
4. Rebuild each link as an editable part.
5. Add concentric mates at H0/H1/H2/A/B/F/T.
6. Drive the H1 crank angle and check for interference.
7. Replace placeholder motor, servo, and latch with actual purchased components.

## Minimum Fabrication Advice

- Use M3 bolts for all pivots.
- Add washers between every moving layer.
- Keep at least 0.5 mm side clearance per moving link.
- Test by hand before adding elastic preload.
- Start preload at 20 mm and increase gradually.
- Add a hard stop to prevent the motor from over-winding the elastic tendon.

