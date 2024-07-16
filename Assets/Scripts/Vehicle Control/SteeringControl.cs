using UnityEngine;
using System;

namespace RVP
{
	[DisallowMultipleComponent]
	[AddComponentMenu("RVP/Vehicle Controllers/Steering Control", 2)]

	// Class for steering vehicles
	public class SteeringControl : MonoBehaviour
	{
		Transform tr;
		Transform steeringWheel;
		VehicleParent vp;

		public AudioSource servoAudio;
		[Tooltip("First wheel should be FL")]
		public Suspension[] steeredWheels;
		[Range(0, 1f)]
		public float holdDuration = 0;
		public float steerLimit;
		public float maxDegreesRotation;
		static AnimationCurve keyboardInputCurve;
		static AnimationCurve analogInputCurve;
		public float holdComebackSpeed;
		[Range(0,1)]
		public float holdCurveValue = 0;
		public float steerAdd = 0.25f;

		public AnimationCurve steerLimitCurve;

		public AnimationCurve steerComebackCurve = AnimationCurve.Linear(0,1,55,.1f);
		private float targetSteer;
		[Range(1,10)]
		public float gamma = 2;
		public float shiftRearFriction;
		internal float driftRearFriction;
		//internal float driftRearFrictionInit;
		[Range(0, 1)]
		public float asi;

		void GenerateGammaCurve()
		{
			if(analogInputCurve == null || gamma != F.I.playerData.steerGamma)
			{
				gamma = F.I.playerData.steerGamma;
				Keyframe[] keys2 = new Keyframe[64];
				for (int i = 0; i < keys2.Length; i++)
				{
					keys2[i].time = (float)i / keys2.Length;
					keys2[i].value = Mathf.Pow(keys2[i].time, gamma);
				}
				analogInputCurve = new AnimationCurve(keys2);
			}
		}
		void Generate_digitalSteeringInputCurve()
		{
			if (keyboardInputCurve == null)
			{
				double[] digitalSteeringInputEnv = { 0, 0.175000, 0.189238, 0.203475, 0.217713, 0.231950, 0.246188, 0.260426, 0.274663, 0.288901, 0.293119, 0.297337, 0.301555, 0.305773, 0.309991, 0.314209, 0.318426, 0.322644, 0.327860, 0.333075, 0.338291, 0.343506, 0.348722, 0.353937, 0.359153, 0.364368, 0.367577, 0.370786, 0.373995, 0.377204, 0.380413, 0.383622, 0.386831, 0.390039, 0.390845, 0.391651, 0.392456, 0.393262, 0.394068, 0.394873, 0.395679, 0.396485, 0.400782, 0.405079, 0.409375, 0.413672, 0.414673, 0.415674, 0.416675, 0.417676, 0.418677, 0.419678, 0.420679, 0.421680, 0.426539, 0.431397, 0.436255, 0.441114, 0.445972, 0.450831, 0.455689, 0.460547, 0.466211, 0.471875, 0.477540, 0.483204, 0.488868, 0.494532, 0.500196, 0.505860, 0.513184, 0.520508, 0.527832, 0.535157, 0.542481, 0.549805, 0.557129, 0.564453, 0.575196, 0.585938, 0.596680, 0.607422, 0.618164, 0.628907, 0.639649, 0.650391, 0.661280, 0.672168, 0.683057, 0.693946, 0.704834, 0.715723, 0.726612, 0.737500, 0.752344, 0.767188, 0.782031, 0.796875, 0.811719, 0.826563, 0.841406, 0.856250, 0.867969, 0.879688, 0.891406, 0.903125, 0.914844, 0.926563, 0.938281, 0.950000, 0.954688, 0.959375, 0.964063, 0.968750, 0.973438, 0.978125, 0.982813, 0.987500, 0.990625, 0.993750, 0.996875, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000, 1.000000 };

				Keyframe[] keys = new Keyframe[digitalSteeringInputEnv.Length];
				for (int i = 0; i < keys.Length; i++)
				{
					keys[i].time = (float)i / keys.Length;
					keys[i].value = (float)digitalSteeringInputEnv[i];//Mathf.Clamp01((float)(digitalSteeringInputEnv[i]-0.160));
				}
				keyboardInputCurve = new AnimationCurve(keys);
			}
		}
		void Start()
		{
			//GenerateSteeringInputCurve();
			Generate_digitalSteeringInputCurve();
			GenerateGammaCurve();
			tr = transform;
			vp = tr.GetTopmostParentComponent<VehicleParent>();
			if (tr.childCount > 0)
				steeringWheel = tr.GetChild(0);
		}
		void FixedUpdate()
		{
			asi = Mathf.Abs(vp.steerInput);
			steerLimit = 1;
			if (!vp.followAI.selfDriving)
			{
				F.I.controllerInUse = (vp.basicInput.playerInput.currentControlScheme != "Keyboard");

				if (F.I.playerData.steerGamma != gamma)
				{
					GenerateGammaCurve();
				}
				
				if (asi >= holdCurveValue)
				{
					var steerLimitVar = steerLimitCurve.Evaluate(vp.velMag);
					//if (steerLimitVar > steerLimit)
					steerLimit = steerLimitVar;
					servoAudio.volume = 1f;
					servoAudio.pitch = (Mathf.Abs(targetSteer) > asi) ? 1.5f : 1;

					if(asi != holdCurveValue)
						holdDuration = Mathf.Clamp01(holdDuration + (vp.SGPshiftbutton > 0 ? 4 : 1) * (F.I.controllerInUse ? 20 : 1 ) * steerAdd * Time.fixedDeltaTime);
				}
				else
				{

					steerLimit = steerLimitCurve.Evaluate(vp.localVelocity.z);
					servoAudio.volume = 0;
					holdDuration = Mathf.Lerp(holdDuration, asi, holdComebackSpeed * 10 * Time.fixedDeltaTime);
				}

				if (F.I.controllerInUse)
					holdCurveValue = Mathf.Lerp(holdCurveValue, asi, 10 * Time.deltaTime);//analogInputCurve.Evaluate(holdDuration);
				else
					holdCurveValue = keyboardInputCurve.Evaluate(holdDuration);
			}
			
			if (steeringWheel != null)
				steeringWheel.localRotation = Quaternion.Lerp(steeringWheel.localRotation, 
					Quaternion.Euler(0,0, -holdCurveValue * 120 * vp.steerInput), 5*Time.fixedDeltaTime);

			if(F.I.s_raceType == RaceType.Drift)
			{
				//steerLimit *= 2;
				float target = Mathf.Lerp(vp.wheels[2].initSidewaysFriction, driftRearFriction, 2*vp.accelInput-1);
				foreach(var w in vp.wheels)
					w.sidewaysFriction = Mathf.Lerp(w.sidewaysFriction, target, 10 * Time.fixedDeltaTime);
			}
			else
			{
				vp.wheels[2].sidewaysFriction = Mathf.Lerp(vp.wheels[2].initSidewaysFriction, shiftRearFriction, holdCurveValue);
				vp.wheels[3].sidewaysFriction = vp.wheels[2].sidewaysFriction;
			}

			foreach (Suspension curSus in steeredWheels)// Set steer angles in wheels
			{
				if (vp.followAI.selfDriving)
				{
					curSus.steerAngle = vp.steerInput;
				}
				else
				{
					float sign = F.Sign(vp.steerInput);
					targetSteer = sign * Mathf.Min(holdCurveValue, asi) * steerLimit;
					curSus.steerAngle = Mathf.Lerp(curSus.steerAngle, 
						targetSteer,
						((sign==0)?steerComebackCurve.Evaluate(vp.velMag) : 10) * Time.fixedDeltaTime);
				}
			}
		}
		
	}
}
