using RVP;
using UnityEngine;

public enum Direction { ANTICLOCK = -1, CLOCK = 1};
public class RotationDampStruct
{
    public static float evoSmoothTime = 2f;
    public static float evoMaxSpeed = 60f;
    public static float evoCoeff = 10;
    public static float minEvoCoeff = 10;
    public static float maxEvoCoeff = 20;
    

    float pos = 0;
    public float targetPos = 0;
    public float velocity = 0;
    public float offset = 0;
    public float deltaCoeff = 1f;
    public float Pos()
    {
        return pos + offset;
    }
    public void UpdateTarget(Direction dir)
    {
        if (velocity * (int)dir < 0)
            DecreaseCoeff();
        if(dir == Direction.CLOCK)
        {
            if (pos > 315)
                targetPos = float.MaxValue;
            else
                targetPos = 360;
        }
        else
        {
            if (pos < 45)
                targetPos = float.MinValue;
            else
                targetPos = 0;
        }
    }
    float degs(float deg)
    {
        if (deg > 360)
            deg -= 360;
        if (deg < 0)
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
        velocity = 0;
        evoCoeff = minEvoCoeff;
    }
    public void SmoothDamp()
    {
        pos = degs(pos);
        pos = Mathf.SmoothDamp(pos, targetPos, ref velocity,
               evoSmoothTime, evoMaxSpeed, Time.fixedDeltaTime * evoCoeff);
        
    }
    public void IncreaseCoeff()
    {
        evoCoeff += deltaCoeff;
        evoCoeff = Mathf.Clamp(evoCoeff, minEvoCoeff, maxEvoCoeff);
    }
    public void DecreaseCoeff()
    {
        evoCoeff -= deltaCoeff;
        evoCoeff = Mathf.Clamp(evoCoeff, minEvoCoeff, maxEvoCoeff);
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
        rX_delta = rX.deltaCoeff;
        posy = rY.Pos();
        tary = rY.targetPos;
		
        if (stunting && (vp.groundedWheels > 0 || vp.crashing))
            stunting = false;
        if (!stunting && !vp.colliding && vp.groundedWheels == 0 && Time.time - shiftPressTime < maxTimeToInit)
        {
            stunting = true;
            euler = vp.tr.rotation.eulerAngles;
            rX.Init(euler.x);
            rY.Init(euler.y, true);
            rZ.Init(euler.z);
        }
			
        if (vp.SGPshiftbutton)
		{
			if(!stunting && prevSGPShiftButton == false)
			{ // shift press before jump
				shiftPressTime = Time.time;
			}
		}
        if (stunting)
        {
            if (vp.accelInput > 0)
            { // backflip
                rX.UpdateTarget(Direction.ANTICLOCK);
                rX.IncreaseCoeff();
            }
            if(vp.brakeInput > 0)
            {
                // frontflip
                rX.UpdateTarget(Direction.CLOCK);
                rX.IncreaseCoeff();
            }
            if(vp.rollInput != 0)
            { 
                if (vp.rollInput > 0)
                { // right roll
                    rZ.UpdateTarget(Direction.CLOCK);
                }
                else if (vp.rollInput < 0)
                { // left roll
                    rZ.UpdateTarget(Direction.ANTICLOCK);
                }
                rZ.IncreaseCoeff();
            }
            if(vp.steerInput != 0)
            { // left/right 
                rY.IncreaseCoeff();
              
                if (vp.steerInput > 0)
                    rY.UpdateTarget(Direction.CLOCK);
                else
                    rY.UpdateTarget(Direction.ANTICLOCK);
            }
			rX.SmoothDamp();
            rY.SmoothDamp();
            rZ.SmoothDamp();
            rb.rotation = Quaternion.Euler(rX.Pos(), rY.Pos(), rZ.Pos());
        }
		prevSGPShiftButton = vp.SGPshiftbutton;
	}
}

