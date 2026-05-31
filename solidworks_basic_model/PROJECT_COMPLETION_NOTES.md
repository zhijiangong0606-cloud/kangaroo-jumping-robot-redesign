# Kangaroo Jumping Robot - Completion Notes

## Final SolidWorks Deliverables

Main assembly:

- `KangarooRobot_completed_engineering_layout.SLDASM`

Native parts:

- `BodySidePlate_verified.SLDPRT`
- `L1_Crank_40mm.SLDPRT`
- `L2_Coupler_120mm.SLDPRT`
- `L3_ThighRocker_100mm.SLDPRT`
- `L4_Shank_140mm.SLDPRT`
- `L5_RearRocker_180mm.SLDPRT`
- `FootPad_85mm.SLDPRT`
- `TailRod_210mm.SLDPRT`
- `WindingDrum_r18_w24.SLDPRT`
- `GearMotor_Placeholder_60x32x26.SLDPRT`
- `Servo_Placeholder_38x35x22.SLDPRT`
- `Latch_Placeholder_50x12x8.SLDPRT`
- `M3_Axle_40mm.SLDPRT`
- `Spacer_16mm.SLDPRT`
- `ElasticTendon_120mm.SLDPRT`
- `TailMass_30g_placeholder.SLDPRT`

Final presentation render:

- `final_completed_engineering_render_en.png`

## Design Concept

This design uses a motor-preloaded elastic jumping architecture:

1. The DC gear motor slowly winds the drum.
2. The winding drum stretches the elastic tendon.
3. The latch holds the crouched energy-storage state.
4. A servo releases the latch.
5. The elastic tendon rapidly drives the 1-DOF closed-chain hind leg.
6. The tail rod and tail mass reduce pitch instability during takeoff and landing.

## Mechanism Dimensions

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

Single-side mechanism mobility:

```text
M = 3(n - 1) - 2j = 3(6 - 1) - 2*7 = 1
```

## Recommended Build Version

Recommended electronics:

- ESP32 DevKit
- 12 V DC gear motor
- TB6612FNG or BTS7960 motor driver
- MG90S or 20 g servo
- MPU6050 IMU
- 2-3 limit switches
- LM2596 buck converter
- 2S Li-ion/LiPo battery

Recommended mechanical hardware:

- 3-4 mm acrylic, carbon plate, or PLA for body plates
- 3 mm acrylic, PLA, or aluminum for links
- M3 bolts as pivots
- 8-16 mm spacers between side plates
- rubber pad on foot contact surface
- rubber bands or extension springs as elastic tendon

## What Still Needs Manual Refinement

The current SolidWorks files are a completed layout model, not yet final manufacturing drawings. Before fabrication, refine:

- add fillets to all link ends
- add real bearing seats if bearings are available
- add lightening holes to body plates
- add actual motor mounting pattern for the purchased motor
- add actual servo mounting holes
- replace latch placeholder with a detailed pawl/slot geometry
- define rubber band or spring attachment hooks
- add concentric mates for motion simulation
- drive H1 crank angle to check interference

## Suggested Validation

Minimum test set:

| Test | Variable | Measurement |
|---|---|---|
| leg motion | crank angle 0-90 deg | interference / dead point |
| jump height | tendon preload 20/30/40 mm | jump height and range |
| tail stability | tail mass 0/15/30 g | max pitch angle |
| foot friction | rubber/no rubber | slip distance |
| repeatability | 5 jumps per setting | mean and standard deviation |

Mark any estimated data clearly until physical tests are completed.

