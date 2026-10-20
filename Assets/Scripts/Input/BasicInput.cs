using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RVP
{
	[DisallowMultipleComponent]
	[AddComponentMenu("RVP/Input/Basic Input", 0)]

	// Class for setting the input with the input manager
	public class BasicInput : MonoBehaviour
	{
		static int pauseFrame = -1;
		VehicleParent vp;
		[NonSerialized]
		public PlayerInput playerInput;
		PlayerInput cachedInput;
		readonly Dictionary<Guid, InputAction> localActions = new();
		public InputAction Action(InputActionReference reference)
		{
			if (!reference) return null;
			if (vp.localPlayerIndex < 0 || !playerInput) return reference.action;
			if (cachedInput != playerInput) { cachedInput = playerInput; localActions.Clear(); }
			Guid id = reference.action.id;
			if (!localActions.TryGetValue(id, out var action))
			{
				action = playerInput.actions.FindAction(id.ToString());
				localActions[id] = action;
			}
			return action;
		}
		
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
			if (!vp || vp.originalVehiclePhysics == null) return;
			if (vp.localPlayerIndex >= 0)
			{
				if (Action(F.I.escRef)?.WasPressedThisFrame() == true && pauseFrame != Time.frameCount)
				{ pauseFrame = Time.frameCount; RaceManager.I.hud.TogglePause(); }
				if (Action(F.I.lightsInput)?.WasPressedThisFrame() == true) vp.Switchlights();
				if (vp.localCamera && Action(vp.localCamera.changeCamRef)?.WasPressedThisFrame() == true)
					vp.localCamera.CycleFollowCamera();
			}
			bool playerDriving = vp.Owner && !vp.followAI.selfDriving && F.I && !F.I.chat.texting;
			if (!playerDriving)
			{
				vp.originalVehiclePhysics.CancelSuspensionJump();
				return;
			}
			vp.originalVehiclePhysics.SetSuspensionJumpInput(Action(F.I.altInputRef)?.IsPressed() == true);
		}
		void OnDisable()
		{
			F.I.lightsInput.action.performed -= LightsInputPerformed;
			if (vp && vp.originalVehiclePhysics != null)
				vp.originalVehiclePhysics.CancelSuspensionJump();
		}
		private void OnEnable()
		{
			if (!vp.followAI.selfDriving && F.I.gameMode != GameMode.Splitscreen)
			{
				F.I.lightsInput.action.performed += LightsInputPerformed;
			}
		}

		private void LightsInputPerformed(InputAction.CallbackContext obj)
		{
			vp.Switchlights();
		}

		void FixedUpdate()
		{
			if (!vp.followAI.selfDriving)
			{
				if (vp.Owner)
				{
					if (F.I.chat.texting)
						return;

					if (vp.localPlayerIndex < 0 && !string.IsNullOrEmpty(upshiftButton))
					{
						if (Input.GetButtonDown(upshiftButton))
						{
							vp.PressUpshift();
						}
					}

					if (vp.localPlayerIndex < 0 && !string.IsNullOrEmpty(downshiftButton))
					{
						if (Input.GetButtonDown(downshiftButton))
						{
							vp.PressDownshift();
						}
					}

					Vector2 input2 = Action(F.I.driveRef)?.ReadValue<Vector2>() ?? Vector2.zero;
					var mobile = MobileControls.I;
					bool touchDriving = mobile && mobile.Driving && vp == RaceManager.I.playerCar;
					if (touchDriving)
					{
						Vector2 touch = mobile.Drive;
						if (touch.x != 0) input2.x = touch.x;
						if (touch.y != 0) input2.y = touch.y;
					}
					vp.SetAccel(Mathf.Clamp01(input2.y));
					vp.SetBrake(Mathf.Abs(Mathf.Clamp(input2.y, -1, 0)));
					vp.SetSteer(input2.x);
					vp.SetBoost(touchDriving && mobile.Nitro ? 1 : (int)(Action(F.I.boostInput)?.ReadValue<float>() ?? 0));
					if (F.I.chat.texting)
						return;

					
					vp.SetHonkerInput((int)(Action(F.I.honkInput)?.ReadValue<float>() ?? 0));
					vp.SetSGPShift(touchDriving && mobile.Stunts ? 1 : (int)(Action(F.I.evoInput)?.ReadValue<float>() ?? 0));
					if ((Action(F.I.resetOnTrackInput)?.ReadValue<float>() ?? 0) == 1
						&& Time.time - vp.resetOnTrackTime > 5)
					{
						vp.ResetOnTrack();
					}
					vp.SetRoll(Action(F.I.rollInput)?.ReadValue<float>() ?? 0);

					if (vp.localPlayerIndex < 0 && !string.IsNullOrEmpty(upshiftButton))
					{
						vp.SetUpshift(Input.GetAxis(upshiftButton));
					}

					if (vp.localPlayerIndex < 0 && !string.IsNullOrEmpty(downshiftButton))
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
