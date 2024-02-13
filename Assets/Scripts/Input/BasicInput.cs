using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RVP
{
	[DisallowMultipleComponent]
	[AddComponentMenu("RVP/Input/Basic Input", 0)]

	// Class for setting the input with the input manager
	public class BasicInput : MonoBehaviour
	{
		VehicleParent vp;
		[NonSerialized]
		public PlayerInput playerInput;
		public InputActionReference driveInput;
		public InputActionReference boostInput;
		public InputActionReference evoInput;
		public InputActionReference honkInput;
		public InputActionReference rollInput;
		public string upshiftButton;
		public string downshiftButton;
		public string lightsButton;
		public string resetOnTrackButton;

		public InputActionReference lookBackInput;
		public InputActionReference lookAxisInput;

		private void Awake()
		{
			vp = transform.parent.GetComponent<VehicleParent>();
			vp.basicInput = this;
			playerInput = GameObject.Find("Canvas").GetComponent<PlayerInput>();
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
			Vector2 input2 = driveInput.action.ReadValue<Vector2>();
			vp.SetAccel(Mathf.Clamp01(input2.y));
			vp.SetBrake(Mathf.Abs(Mathf.Clamp(input2.y, -1, 0)));
			vp.SetSteer(input2.x);
			vp.SetHonkerInput(honkInput.action.ReadValue<float>()==1);
			vp.SetBoost(boostInput.action.ReadValue<float>()==1);
			vp.SetSGPShift(evoInput.action.ReadValue<float>()==1);

			if (!string.IsNullOrEmpty(resetOnTrackButton))
			{
				if (Input.GetButtonDown(resetOnTrackButton))
					vp.ResetOnTrack();
			}
			vp.SetRoll(rollInput.action.ReadValue<float>());

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