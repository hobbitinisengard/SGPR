using RVP;
using System;
using UnityEngine;

public class RaceBox : MonoBehaviour
{
    VehicleParent vp;
    public float distance { get; private set; }
    public float aero { get; private set; }
    public DateTime lapStartTime;
    public TimeSpan bestLapTime { get; private set; }

    /// <summary>
    /// number of laps done already
    /// </summary>
    public int curNoLap { get; private set; }
    /// <summary>
    /// All number of laps of this race
    /// </summary>
    public int NoLaps { get; private set; }

    public int starLevel;

    public float grantedComboTime;
    public DateTime comboStartTime;
    float maxAeroMeterVelocity = 0.05f;
    float minAeroMeterVelocity = 0;
    public float aeroMeterVelocity = 0;
    float aeroMeterResponsiveness = 1f;
    void Start()
    {
        vp = transform.GetComponent<VehicleParent>();
        lapStartTime = DateTime.MinValue;
        bestLapTime = TimeSpan.MaxValue;
        curNoLap = 0;
        NoLaps = 18;
        starLevel = 0;
        comboStartTime = DateTime.MinValue;
    }
    private void Update()
    {
        if(Time.timeScale != 0)
        {
            aeroMeterVelocity = Mathf.Lerp(aeroMeterVelocity,
            (vp.groundedWheels == 0 && !vp.colliding) ? maxAeroMeterVelocity : minAeroMeterVelocity
            , Time.deltaTime * aeroMeterResponsiveness);
            //aeroMeterVelocity *= Mathf.Clamp01(vp.rb.velocity.magnitude / 46);
            aero += aeroMeterVelocity;
        }
    }
    public void AddAeroPts(int pts)
    {
        aero += pts;
    }
    public void SetGrantedComboTime(float seconds)
    {
        grantedComboTime = seconds;
        comboStartTime = DateTime.Now;
        starLevel = Mathf.Clamp(++starLevel, 0, 10);
    }
    public TimeSpan CurLapTime()
    {
        if (lapStartTime == DateTime.MinValue)
            return TimeSpan.MinValue;
        return DateTime.Now - lapStartTime;
    }
    public void NextLap()
    {
        var curlaptime = CurLapTime();
        if (curlaptime < bestLapTime && curlaptime != TimeSpan.MinValue)
            bestLapTime = CurLapTime();

        lapStartTime = DateTime.Now;
        curNoLap++;
    }
    public bool ComboActive()
    {
        bool active = (DateTime.Now - comboStartTime).TotalSeconds < grantedComboTime;
        if (!active)
            starLevel = 0;
        return active;
    }
}
