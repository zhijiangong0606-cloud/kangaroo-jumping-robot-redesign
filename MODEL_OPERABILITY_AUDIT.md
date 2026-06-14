# Model Operability Audit

This audit records the current state of the CAD model and the changes made to improve buildability.

## Current SolidWorks Model Status

> **v7 update (physics-driven fixes):** a mechanics feasibility audit
> (`feasibility_audit.py`) found the old linkage gave the foot almost **no vertical
> push stroke** (2.2 mm of vertical vs 92 mm of horizontal scrub) — it could not
> have jumped regardless of spring energy. The mechanism was re-optimized
> (`optimize_mechanism.py`, topology unchanged) to **34.6 mm vertical foot travel**
> with a **60° minimum transmission angle** and zero interference over the working
> stroke. The latch was given a **14° self-locking tooth** so an MG90S servo can
> release it (~1 kg·cm vs the 3.7 kg·cm the old square tooth needed), the side
> plates were lightened (~27%), and a battery counter-mass + foot pads were added.
> Full assembly: 42 components, 29 concentric mates, solid STL.

Main assembly:

- `solidworks_basic_model/kangaroo_overleap_bioinspired_v1/Kangaroo_Overleap_BioInspired_Assembly.SLDASM`

The assembly is suitable for:

- explaining the mechanism architecture
- showing module placement
- demonstrating the relation between motor preload, latch release, elastic tendon, tail stabilizer, and closed-chain hind leg
- visual presentation and course reporting

Status after v6 functional-part pass:

- link parts now carry real hole pitches, lightening cuts, and bearing reference circles
- latch/pawl is now a functional solid with a defined catch face, not a block placeholder
- drum, foot, and standoff parts are modeled as buildable solids
- motor and servo remain envelope references pending final purchased-part dimensions
- geometry checked programmatically: parts mesh correctly, 27 concentric mates verified, set is printable
- a full SolidWorks dynamic mate set for motion simulation is still outstanding

## Practical Issues Found

| Issue | Impact | Fix / Mitigation |
|---|---|---|
| Simplified link geometry | Buildable, but not optimized for weight or bearings | Added manufacturing DXF with lightening holes and bearing reference circles |
| Placeholder motor and servo | Actual purchased parts may not match | Added adjustable mounting reference holes and noted final resizing requirement |
| Placeholder latch | Cannot be fabricated directly | RESOLVED v6: latch/pawl modeled as functional solid with defined catch face; final tuning after servo horn selected |
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

