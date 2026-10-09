"""Summarize matching runtime CSVs and check wheel outputs against the source friction equations.
Usage: python compare.py UNITY_CAPTURE ORIGINAL_CAPTURE [--min-speed 80 --max-speed 120]
Speed bounds are km/h. This is an analysis of recorded data, not a driving benchmark.
"""
import argparse, csv, math, statistics, struct, collections
from pathlib import Path

def f32(x):
    return struct.unpack('f',struct.pack('f',x))[0]

def rows(folder, name):
    with (folder/name).open(encoding='utf-8-sig',newline='') as stream:
        for row in csv.DictReader(stream):
            yield {key:float(value) for key,value in row.items()}

def summary(data, field):
    values=sorted(row[field] for row in data if math.isfinite(row[field]))
    if not values: return 'n/a'
    p95=values[min(len(values)-1,math.ceil(.95*len(values))-1)]
    return f'n={len(values)} mean={statistics.fmean(values):.6g} median={statistics.median(values):.6g} p95={p95:.6g}'

def friction_error(row):
    # vehicle.cpp::tyre_friction: use the input and selected curve sample recorded
    # at this exact call. No car config is substituted or guessed here.
    lat=row['lat_before_m_s']/.6
    slip=row['slip_m_s']/.6
    available=row['normalized_load']*row['curve_value']
    threshold=available*row['static_coeff']
    if abs(threshold)<f32(.01): threshold=f32(.01)
    magnitude=f32(math.sqrt(lat*lat+slip*slip*f32(.64000005)))
    sliding=magnitude>threshold
    if not sliding:
        residual_lat=0
        residual_slip=slip
    else:
        change=min(abs(lat),available)*row['kinetic_coeff']
        residual_lat=f32(lat+change if lat<0 else lat-change)
        drive_budget=available*(3 if slip>0 else 1)
        residual_slip=f32(f32(min(abs(slip),drive_budget))*row['kinetic_coeff']*(-1 if slip<0 else 1))
    return abs(residual_lat*.6-row['lat_after_m_s']),abs(residual_slip*.5*.6-row['drive_delta_m_s']),int(sliding)!=int(row['sliding'])

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('unity',type=Path);parser.add_argument('original',type=Path)
    parser.add_argument('--min-speed',type=float,default=0);parser.add_argument('--max-speed',type=float,default=1000)
    parser.add_argument('--wheel',type=int,choices=range(4))
    parser.add_argument('--grip',type=float,help='Compare only this exact surface grip (e.g. 1).')
    parser.add_argument('--camera-slot',type=int,default=0)
    parser.add_argument('--contact-class',type=int,choices=range(3),default=0)
    args=parser.parse_args()
    for label,folder in [('UNITY',args.unity),('ORIGINAL',args.original)]:
        all_camera=list(rows(folder,'camera.csv'))
        camera=[r for r in all_camera if r['slot']==args.camera_slot and r['contact_class']==args.contact_class
                and args.min_speed<=r['speed_m_s']*3.6<=args.max_speed]
        print(f'\nCamera coverage: total={len(all_camera)}, selected={len(camera)}, slots={dict(collections.Counter(r["slot"] for r in all_camera))}')
        modes=folder/'camera_modes.csv'
        if modes.exists():
            print('Actual camera modes:',dict(collections.Counter((int(r['controller']),int(r['vehicle_slot']),int(r['mode'])) for r in rows(folder,'camera_modes.csv'))))
        if not all_camera: print('Camera recording is EMPTY; rebuild with mode coverage instrumentation. No camera comparison is possible.')
        tyre=[r for r in rows(folder,'tyres.csv') if r['slot']==0
              and args.min_speed<=abs(r['long_speed_m_s'])*3.6<=args.max_speed
              and (args.wheel is None or r['wheel']==args.wheel)
              and (args.grip is None or math.isclose(r['grip'],args.grip,rel_tol=1e-6))]
        print(f'\n{label}: {folder}\nCamera (matched contact class/speed):')
        for field in ['error_before_m','error_after_m','response_ratio','target_error_after_m','view_turn_deg','move_m','approach_scale','limit_m','lens']:
            print(f'  {field}: {summary(camera,field)}')
        print('Tyres (speed/wheel filtered; compare the same setup and driving manoeuvre):')
        for wheel in range(4):
            subset=[r for r in tyre if r['wheel']==wheel]
            if not subset: continue
            print(f'  Wheel {wheel} (RL,RR,FL,FR): rows={len(subset)} kineticFraction={statistics.fmean(r["sliding"] for r in subset):.4f}')
            for field in ['mass_source','steering_deg','normalized_load','grip','static_coeff','kinetic_coeff','curve_value','available_source','threshold_source','grip_usage','lat_before_m_s','lat_after_m_s','long_speed_m_s','wheel_speed_m_s','slip_m_s','config_static','config_kinetic','drive_delta_m_s']:
                print(f'    {field}: {summary(subset,field)}')
            errors=[friction_error(r) for r in subset]
            print(f'    Source-equation residuals: max lateral={max(e[0] for e in errors):.7g} m/s; '
                  f'max drive={max(e[1] for e in errors):.7g} m/s; branch mismatches={sum(e[2] for e in errors)}')
    print('\nTick IDs/positions from separate runs are not aligned. Different inputs/loads/configs can explain differing summaries.')
    print('The source-equation check replays the recorded friction inputs, not the full vehicle simulation. Drift mode has a deliberate drive-budget change.')
if __name__=='__main__': main()
