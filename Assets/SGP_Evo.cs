using RVP;
using System;
using UnityEngine;

public enum Direction { ANTICLOCK = -1, CLOCK = 1 };
public class RotationDampStruct
{
	public float evoSmoothTime = 0.07f;
	public float staticEvoMaxSpeed = 1100; // 400 = hustler max speed. 1500 = dart max speed
	public float evoAcceleration = 15; // 15 = hustler max acc.  57 = dart max acc

	// increases when holding shift
	float evoMaxSpeed = 0;
	float pos = 0;
	public float targetPos = 0;
	public float speed = 0;
	float prevSpeed = 0;
	public float offset = 0;
	private Axis axis;
	public float Delta { get { return speed - prevSpeed; } }
	public bool Active { get { return Mathf.Abs(targetPos - pos) > 0.1f; } }
	public float Pos { get { return pos + offset; } }
	public void UpdateTargetToValue(int target)
	{
		targetPos = target;
		evoMaxSpeed = 5 * evoAcceleration;

		//Debug.Log(targetPos + " " + pos + " " + speed);
	}
	public void UpdateTarget(Direction dir)
	{
		if (speed * (int)dir < 0)
		{
			speed = 0;
			evoMaxSpeed = 0.1f;
		}

		if (dir == Direction.CLOCK)
		{
			if (pos > 315)
			{
				if (axis == Axis.X) // better landing
					targetPos = 710;
				else
					targetPos = 720;
			}
			else if (pos > 45)
			{
				if (pos > 135)
				{
					if (axis == Axis.X)
						targetPos = 350;
					else
						targetPos = 360;
				}
				else
					targetPos = 180;
			}
			else
				targetPos = 90;
		}
		else
		{
			if (pos < 45)
			{
				if (axis == Axis.X)
					targetPos = -370;
				else
					targetPos = -360;
			}
			else if (pos < 315)
			{
				if (pos > 225)
					targetPos = 180;
				else
				{
					if (axis == Axis.X)
						targetPos = -10;
					else
						targetPos = 0;
				}
			}
			else
				targetPos = 270;
		}
		//Debug.Log(targetPos + " " + pos + " " + speed);
	}

	float degs(float deg)
	{
		if (deg > 360)
		{
			deg -= 360;
		}
		else if (deg < 0)
		{
			deg += 360;
		}
		return deg;
	}
	public void Init(float newpos, Axis axis)
	{
		this.axis = axis;
		if (axis == Axis.Y)
		{
			offset = newpos;
			pos = 0;
		}
		else
			pos = newpos;
		targetPos = pos;
		speed = 0;
		evoMaxSpeed = 0;
	}
	public void SmoothDamp()
	{
		pos = degs(pos);
		prevSpeed = speed;
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
		if (!Active)
			evoMaxSpeed = speed;
	}
	public void CloseAdvancedMode()
	{
		if (speed > 0) // clockwise rotation
		{
			targetPos = 360;
		}
		else // anticlockwise rotation
		{
			targetPos = 0;
		}
	}
}
public class SGP_Evo : MonoBehaviour
{
	public AudioSource evoBloorp;
	Rigidbody rb;
	VehicleParent vp;
	float shiftPressTime;
	bool prevSGPShiftButton = false;
	public bool stunting = false;
	public bool flippedWhenInitiated = false;
	float maxTimeToInit = 1f;
	RotationDampStruct[] r;
	public float rX_delta;
	public Vector3 euler;
	public float posy;
	public float tary;
	public float rotationSpeed = 5f;
	[NonSerialized]
	public Vector3 localEvoAngularVelocity = Vector3.zero;

	public float mult = 1;

	void Awake()
	{
		rb = GetComponent<Rigidbody>();
		vp = GetComponent<VehicleParent>();
		r = F.InitializeArray<RotationDampStruct>(3);
	}
	public void SetStuntCoeffs(float evoSmoothTime, float staticEvoMaxSpeed, float evoAcceleration)
	{
		foreach(var rds in r)
		{
			rds.evoSmoothTime = evoSmoothTime;
			rds.staticEvoMaxSpeed = staticEvoMaxSpeed;
			rds.evoAcceleration = evoAcceleration;
		}
	}
	public void GetStuntCoeffs(ref float evoSmoothTime, ref float staticEvoMaxSpeed, ref float evoAcceleration)
	{
		evoSmoothTime = r[0].evoSmoothTime;
		staticEvoMaxSpeed = r[0].staticEvoMaxSpeed;
		evoAcceleration = r[0].evoAcceleration;
	}
	public bool IsStunting
	{
		get
		{
			return stunting;
		}
	}

	void FixedUpdate()
	{
		if (vp.SGPshiftbutton)
		{
			if (!stunting && prevSGPShiftButton == false)
			{ // shift press before jump
				if(vp.reallyGroundedWheels > 0 && !vp.crashing)
				{
					shiftPressTime = Time.time;
					flippedWhenInitiated = false;
				}
				else if(vp.reallyGroundedWheels == 0 && vp.crashing)
				{
					shiftPressTime = Time.time;
					flippedWhenInitiated = true;
				}
			}
		}

		if (!stunting && Time.time - shiftPressTime < maxTimeToInit && !vp.colliding && !vp.crashing && vp.reallyGroundedWheels == 0)
		{
			evoBloorp.Play();
			stunting = true;
			euler = vp.tr.rotation.eulerAngles;
			r[0].Init(euler.x, Axis.X); //rX
			r[1].Init(euler.y, Axis.Y); // rY
			r[2].Init(euler.z, Axis.Z); // rZ
		}

		if (stunting)
		{
			if (!flippedWhenInitiated && (vp.crashing || vp.colliding || vp.reallyGroundedWheels > 0))
			{
				stunting = false;
				return;
			}

			if(flippedWhenInitiated && !vp.crashing && !vp.colliding && vp.reallyGroundedWheels == 0)
				flippedWhenInitiated = false;

			if (vp.SGPshiftbutton)
			{
				if (vp.accelInput > 0.5f)
				{ // backflip
					r[0].UpdateTarget(Direction.ANTICLOCK);
					r[0].IncreaseEvoSpeed();
				}
				else if (vp.brakeInput > 0)
				{
					// frontflip
					r[0].UpdateTarget(Direction.CLOCK);
					r[0].IncreaseEvoSpeed();
				}
				if (vp.rollInput != 0)
				{
					if (vp.rollInput > 0.5f)
					{ // right barrel roll
						r[2].UpdateTarget(Direction.CLOCK);
					}
					else if (vp.rollInput < -0.5f)
					{ // left barrel roll
						r[2].UpdateTarget(Direction.ANTICLOCK);
					}
					r[2].IncreaseEvoSpeed();
				}
				if (vp.steerInput != 0)
				{ // rotation left/right 
					if (vp.steerInput > 0.5f)
						r[1].UpdateTarget(Direction.CLOCK);
					else if(vp.steerInput < -0.5f)
						r[1].UpdateTarget(Direction.ANTICLOCK);
					r[1].IncreaseEvoSpeed();

					int rest = (int)r[0].Pos % 90;
					r[0].UpdateTargetToValue(90 * ((int)r[0].Pos / 90) + ((rest < 45) ? 0 : 90));
				}
			}
			else
			{
				foreach (RotationDampStruct rds in r)
				{
					rds.DecreaseMaxEvoSpeed();
					// rotation locking to 90 and 180deg turns off when not pressing shift
					if (!vp.SGPshiftbutton)
						rds.CloseAdvancedMode();
				}
			}

			foreach (RotationDampStruct rds in r)
				rds.SmoothDamp();


			rb.rotation = Quaternion.Euler(r[0].Pos, r[1].Pos, r[2].Pos);
			rb.angularVelocity = vp.transform.TransformDirection(Mathf.Deg2Rad * new Vector3(r[0].speed, r[1].speed, r[2].speed));

			//rb.AddRelativeTorque(Mathf.Deg2Rad * new Vector3(r[0].Delta, r[1].Delta, r[2].Delta), ForceMode.VelocityChange);

			//Vector3 delta = new Vector3(r[0].Delta(), r[1].Delta(), r[2].Delta());


			//if(vp.name.Contains("Clone"))
			//{
			//    Debug.DrawRay(vp.transform.position, vp.transform.TransformDirection(localEvoAngularVelocity), Color.red, 3);
			//    //Debug.Log(localEvoAngularVelocity.magnitude);

			//}
		}
		prevSGPShiftButton = vp.SGPshiftbutton;
	}

	internal void Reset()
	{
		stunting = false;
	}
}

