using UnityEngine;

namespace RVP
{
	[RequireComponent(typeof(VehicleParent))]
	[DisallowMultipleComponent]
	[AddComponentMenu("RVP/Input/Basic Input", 0)]

	// Class for setting the input with the input manager
	public class BasicInput : MonoBehaviour
	{
		VehicleParent vp;
		public string accelAxis;
		public string steerAxis;
		public string ebrakeAxis;
		public string boostButton;
		public string upshiftButton;
		public string downshiftButton;
		public string honkButton;

		//public string pitchAxis;
		//public string yawAxis;
		public string rollAxis;
		public string shiftSGPButton;
		//public string lockSGPButton;
		public string lightsButton;
		public string resetOnTrackButton;
		public float startedAccelInputTime;

		void Start()
		{
			vp = GetComponent<VehicleParent>();
		}

		void Update()
		{
			// Get single-frame input presses
			if (!string.IsNullOrEmpty(upshiftButton))
			{
				if (Input.GetButtonDown(upshiftButton))
				{
					vp.PressUpshift();
				}
			}

			if (!string.IsNullOrEmpty(downshiftButton))
			{
				if (Input.GetButtonDown(downshiftButton))
				{
					vp.PressDownshift();
				}
			}
			if (!string.IsNullOrEmpty(lightsButton))
			{
				if (Input.GetButtonDown(lightsButton))
					vp.SetLights();
			}
		}

		void FixedUpdate()
		{
			// Get constant inputs
			
			if (!string.IsNullOrEmpty(accelAxis))
			{
				float input = Input.GetAxis(accelAxis);
				//if (input == 1)
				//{
				//	if(vp.accelInput == 0)
				//		startedAccelInputTime = Time.time;
				//	input = 2*(Time.time - startedAccelInputTime);
				//}
				if(input>=0)
					vp.SetAccel(input);
				if(input <= 0)
					vp.SetBrake(Mathf.Abs(input));
			}

			if (!string.IsNullOrEmpty(steerAxis))
			{
				vp.SetSteer(Input.GetAxis(steerAxis));
			}

			if (!string.IsNullOrEmpty(ebrakeAxis))
			{
				vp.SetEbrake(Input.GetAxis(ebrakeAxis));
			}
			if (!string.IsNullOrEmpty(honkButton))
			{
				vp.SetHonkerInput(Input.GetButton(honkButton));
			}
			if (!string.IsNullOrEmpty(boostButton))
			{
				vp.SetBoost(Input.GetButton(boostButton));
			}
			if (!string.IsNullOrEmpty(shiftSGPButton))
			{
				vp.SetSGPShift(Input.GetButton(shiftSGPButton));
			}
			//if (!string.IsNullOrEmpty(lockSGPButton))
			//{
			//    vp.SetSGPLock(Input.GetButton(lockSGPButton));
			//}

			if (!string.IsNullOrEmpty(resetOnTrackButton))
			{
				if (Input.GetButtonDown(resetOnTrackButton))
					vp.ResetOnTrack();
			}
			//if (!string.IsNullOrEmpty(pitchAxis)) {
			//    vp.SetPitch(Input.GetAxis(pitchAxis));
			//}

			//if (!string.IsNullOrEmpty(yawAxis)) {
			//    vp.SetYaw(Input.GetAxis(yawAxis));
			//}

			if (!string.IsNullOrEmpty(rollAxis))
			{
				vp.SetRoll(Input.GetAxis(rollAxis));
			}

			if (!string.IsNullOrEmpty(upshiftButton))
			{
				vp.SetUpshift(Input.GetAxis(upshiftButton));
			}

			if (!string.IsNullOrEmpty(downshiftButton))
			{
				vp.SetDownshift(Input.GetAxis(downshiftButton));
			}

		}
	}
}