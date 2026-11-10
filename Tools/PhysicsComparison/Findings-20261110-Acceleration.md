# Acceleration comparison, 2026-11-10

Capture: C:/Users/bernz/Desktop/SGP Reversed/tools/physics-comparison/captures/original-1794305977082/acceleration.csv
1597 rows, physical slot 0, Formula 17 (source Car19 / Unity car18).

## Observations

| Quantity | Original capture | User's Unity capture |
| --- | ---: | ---: |
| Effective mass at start | 1095 | 365 |
| Configured dry mass | 895 | 165 (derived: 365 minus 200 fuel) |
| Maximum torque | 625 | 625 |
| Drive mode | 2 | 2 |
| Cd | 1.05 | 1.05 |
| Cl | 1.35 | 1.35 |
| Frontal area | 2.86 | 2.86 |

The second continuous capture interval spans ticks 2848..4229, 1381 steps in
23.0147 seconds: approximately 60.005 Hz. Tick time and real time agree here.
Interpolated first speed crossings within this interval:

- 1..100 km/h: 7.053 seconds real, 7.052 seconds nominal simulation.
- 1..200 km/h: 12.946 seconds real, 12.946 seconds nominal simulation.
- Peak longitudinal speed: 218.070 km/h. No 261 km/h sample exists in the original
  capture, so the logged Unity point at 261 km/h cannot be compared directly.

These are observations of the supplied driving session, not controlled flat-track
benchmarks; terrain, throttle release and steering can affect the times.

## Confirmed loading discrepancy and fix

Original source: src/retail_campaign_presentation.cpp:
apply_campaign_upgrade_4365d0 adds the part mass at word08 BEFORE returning for
selection 0. default_part(kind) selects part 5, except kind 13 (Drive), part 3.
The selected-zero part's OTHER coefficients are not applied.

Unity's OriginalVehiclePartCatalog.TryGetEffectiveParameter previously skipped
zero selections entirely. The Additive Mass branch now adds their fallback mass,
while the ordinary parameter branch continues to inherit car coefficients.

Formula 17 zero-selection contributions missing in Unity:

| Part category | Fallback | Missing mass |
| --- | --- | ---: |
| Engine | Engine05 | 145 |
| Battery | Battery05 | 35 |
| Tyres | Tyres05 | 40 |
| Brakes | Brakes05 | 40 |
| Suspension | Suspension05 | 75 |
| Chassis | Chassis05 | 35 |
| Drive | Drive03 | 85 |
| Horn | Horns05 | 80 |
| Stunt boost | StuntBoost05 | 165 |
| Gears | Gears05 | 30 |
| Total | | 730 |

Base mass 140 + selected Bms04 mass 25 + fallback mass 730 = dry mass 895.
Adding 200 fuel gives 1095, exactly as recorded in the original capture.
For stock Stuntman (Car21) the same rule predicts dry mass 1015 and initial
mass 1090 with 75 fuel. This is derived from catalog/default setup data;
this particular capture contains only Formula 17.

The rule is applied generically; cars with explicit parts retain their mass for
those slots. Cars with other zero slots (e.g. stock horn) also gain the omitted
mass, matching the source. No motor, friction, drag, lift or timing constants
were altered for this fix. Existing car-specific/user mass overrides remain active.

No Unity build or Play Mode test was run. Verify in a new spawn/race so the runtime
configuration is rebuilt; an already spawned car keeps its previous parameters.
