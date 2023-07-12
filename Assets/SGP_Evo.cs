using RVP;
using UnityEngine;

public enum Direction { ANTICLOCK = -1, CLOCK = 1};
public class RotationDampStruct
{
    readonly float evoSmoothTime = 0.05f;
    readonly float staticEvoMaxSpeed = 400; // 400 = hustler max speed. 1500 = dart max speed
    readonly float evoAcceleration = 15; // 15 = hustler max acc.  57 = dart max acc

    // increases when holding shift
    float evoMaxSpeed = 1; 

    float pos = 0;
    public float targetPos = 0;
    public float evoCoeff = 1;
    public float speed = 0;
    public float offset = 0;
    public bool Active()
    {
        return Mathf.Abs(targetPos - pos) > 2;
    }
    public float Pos()
    {
        return pos + offset;
    }
    public void UpdateTarget(Direction dir)
    {
        if (speed * (int)dir < 0)
        {
            speed = 0;
            evoMaxSpeed = 0.1f;
        }
            
        if(dir == Direction.CLOCK)
        {
            if (pos > 315)
                targetPos = 999; // big +value
            else
                targetPos = 360;
        }
        else
        {
            if (pos < 45)
                targetPos = -999; // big -value
            else
                targetPos = 0;
        }
    }
    float degs(float deg)
    {
        if (deg > 360)
            deg -= 360;
        else if (deg < 0)
            deg += 360;
        return deg;
    }
    public void Init(float newpos, bool isY = false)
    {
        if (isY)
        {
            offset = newpos;
            pos = 0;
        }
        else
            pos = newpos;
        targetPos = pos;
        speed = 0;
        evoMaxSpeed = 1;
    }
    public void SmoothDamp()
    {
        pos = degs(pos);
        pos = Mathf.SmoothDamp(pos, targetPos, ref speed,
               evoSmoothTime, evoMaxSpeed, Time.fixedDeltaTime);
    }
    public void IncreaseEvoSpeed()
    {
        evoMaxSpeed += evoAcceleration;
        if (evoMaxSpeed > staticEvoMaxSpeed)
            evoMaxSpeed = staticEvoMaxSpeed;
    }
    public void DecreaseMaxEvoSpeed()
    {
        if(!Active())
            evoMaxSpeed = speed;
    }
}
public class SGP_Evo : MonoBehaviour
{
    Rigidbody rb;
	VehicleParent vp;
	float shiftPressTime;
	bool prevSGPShiftButton = false;
	public bool stunting = false;
	float maxTimeToInit = 1f;
	public RotationDampStruct rX, rY, rZ;
    public float rX_delta;
    public Vector3 euler;
    public float posy;
    public float tary;

    
	void Start()
	{
        rb = GetComponent<Rigidbody>();
		vp = GetComponent<VehicleParent>();
		rX = new RotationDampStruct();
		rY = new RotationDampStruct();
		rZ = new RotationDampStruct();
	}
	public bool IsStunting()
	{
		return stunting;
	}

	void FixedUpdate()
	{
        posy = rY.Pos();
        tary = rY.targetPos;
		
        if (vp.groundedWheels > 0 || vp.colliding)
            stunting = false;

        if (vp.SGPshiftbutton)
        {
            if (vp.groundedWheels != 0 && !stunting && prevSGPShiftButton == false)
            { // shift press before jump
                shiftPressTime = Time.time;
            }
        }

        if (!stunting && !vp.crashing && vp.groundedWheels == 0 && Time.time - shiftPressTime < maxTimeToInit)
        {
            stunting = true;
            euler = vp.tr.rotation.eulerAngles;
            rX.Init(euler.x);
            rY.Init(euler.y, true);
            rZ.Init(euler.z);
        }
			
        
        if (stunting)
        {
            if(vp.SGPshiftbutton)
            {
                if (vp.accelInput > 0)
                { // backflip
                    rX.UpdateTarget(Direction.ANTICLOCK);
                    rX.IncreaseEvoSpeed();
                }
                if (vp.brakeInput > 0)
                {
                    // frontflip
                    rX.UpdateTarget(Direction.CLOCK);
                    rX.IncreaseEvoSpeed();
                }
                if (vp.rollInput != 0)
                {
                    if (vp.rollInput > 0)
                    { // right barrel roll
                        rZ.UpdateTarget(Direction.CLOCK);
                    }
                    else if (vp.rollInput < 0)
                    { // left barrel roll
                        rZ.UpdateTarget(Direction.ANTICLOCK);
                    }
                    rZ.IncreaseEvoSpeed();
                }
                if (vp.steerInput != 0)
                { // rotation left/right 
                    rY.IncreaseEvoSpeed();

                    if (vp.steerInput > 0)
                        rY.UpdateTarget(Direction.CLOCK);
                    else
                        rY.UpdateTarget(Direction.ANTICLOCK);
                }
            }
            else
            {
                rX.DecreaseMaxEvoSpeed();
                rY.DecreaseMaxEvoSpeed();
                rZ.DecreaseMaxEvoSpeed();
            }
            
			rX.SmoothDamp();
            rY.SmoothDamp();
            rZ.SmoothDamp();
            rb.rotation = Quaternion.Euler(rX.Pos(), rY.Pos(), rZ.Pos());
        }
		prevSGPShiftButton = vp.SGPshiftbutton;
	}
}

