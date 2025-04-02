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
		
		public string upshiftButton;
		public string downshiftButton;
		public string lightsButton;
		public string resetOnTrackButton;
		
		private void Awake()
		{
			vp = transform.parent.GetComponent<VehicleParent>();
			vp.basicInput = this;
			playerInput = GameObject.Find("Canvas").GetComponent<PlayerInput>();
		}
		void Update()
		{
			if (F.I.chat.texting)
				return;

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
					vp.Switchlights();
			}
		}

		void FixedUpdate()
		{
			if (!vp.followAI.selfDriving)
			{
				if (vp.Owner)
				{
					Vector2 input2 = F.I.driveRef.action.ReadValue<Vector2>();
					vp.SetAccel(Mathf.Clamp01(input2.y));
					vp.SetBrake(Mathf.Abs(Mathf.Clamp(input2.y, -1, 0)));
					vp.SetSteer(input2.x);
					vp.SetBoost((int)F.I.boostInput.action.ReadValue<float>());
					vp.SetBunnyhop((int)F.I.bunnyhopInput.action.ReadValue<float>());
					if (F.I.chat.texting)
						return;

					vp.SetHonkerInput((int)F.I.honkInput.action.ReadValue<float>());
					vp.SetSGPShift((int)F.I.evoInput.action.ReadValue<float>());
					if (F.I.resetOnTrackInput.action.ReadValue<float>() == 1
						&& Time.time - vp.resetOnTrackTime > 5)
					{
						vp.ResetOnTrack();
					}
					vp.SetRoll(F.I.rollInput.action.ReadValue<float>());

					if (!string.IsNullOrEmpty(upshiftButton))
					{
						vp.SetUpshift(Input.GetAxis(upshiftButton));
					}

					if (!string.IsNullOrEmpty(downshiftButton))
					{
						vp.SetDownshift(Input.GetAxis(downshiftButton));
					}
				}
				else
				{
					vp.SetAccel(vp.accelInput);
					vp.SetBoost(vp.boostButton);
					vp.SetHonkerInput(vp.honkInput);
					vp.SetSGPShift(vp.SGPshiftbutton);
					vp.SetRoll(vp.rollInput);
				}
			}
		}
	}
}