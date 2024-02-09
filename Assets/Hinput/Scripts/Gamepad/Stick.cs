using UnityEngine;
using HinputClasses.Internal;

namespace HinputClasses {
	/// <summary>
	/// Hinput class representing a gamepad stick, such as a left stick, a right stick, or a D-pad.
	/// </summary>
	public class Stick {
		// --------------------
		// ID
		// --------------------

		/// <summary>
		/// The index of a stick on its gamepad (0 for a left stick, 1 for a right stick, 2 for a D-pad).
		/// </summary>
		public readonly int index;

		/// <summary>
		/// The name of a stick, like “Gamepad0_LeftStick” or “AnyGamepad_DPad”.
		/// </summary>
		public readonly string name;
		
		/// <summary>
		/// The gamepad a stick is attached to.
		/// </summary>
		public readonly Gamepad gamepad;
		
		
		// --------------------
		// ENABLED
		// --------------------
		
		/// <summary>
		/// Returns true if a stick is being tracked by Hinput. Returns false otherwise.
		/// </summary>
		public bool isEnabled { get; private set; }
		
		/// <summary>
		/// Enable a stick so that Hinput starts tracking it.
		/// </summary>
		public void Enable() {
			isEnabled = true;
		}

		/// <summary>
		/// Reset and disable a stick so that Hinput stops tracking it. <br/> <br/>
		/// This may improve performances.
		/// </summary>
		public void Disable() {
			Reset();
			isEnabled = false;
		}

		/// <summary>
		/// Hinput internal method. You don't need to use it.
		/// </summary>
		public void Reset() {
			positionRaw = Vector2.zero;
			
			up.Reset();
			down.Reset();
			left.Reset();
			right.Reset();
			upLeft.Reset();
			downLeft.Reset();
			upRight.Reset();
			downRight.Reset();
			inPressedZone.Reset();
		}

		
		// --------------------
		// IMPLICIT CONVERSION
		// --------------------

		public static implicit operator Vector2 (Stick stick) { return stick.position; }
		public static implicit operator bool (Stick stick) { return stick.inPressedZone; }


		// --------------------
		// CONSTRUCTOR
		// --------------------

		public Stick(string stickName, Gamepad gamepad, int index, bool isEnabled) {
			this.gamepad = gamepad;
			this.index = index;
			this.isEnabled = isEnabled;
			
			name = gamepad.name + "_" + stickName;

			up = new StickDirection ("Up", 90, this);
			down = new StickDirection ("Down", -90, this);
			left = new StickDirection ("Left", 180, this);
			right = new StickDirection ("Right", 0, this);
			upLeft = new StickDirection ("UpLeft", 135, this);
			downLeft = new StickDirection ("DownLeft", -135, this);
			upRight = new StickDirection ("UpRight", 45, this);
			downRight = new StickDirection ("DownRight", -45, this);
			inPressedZone = new StickPressedZone("PressedZone", this);
			
			if (index == 0 || index == 1) { // Sticks
				horizontalAxis = new Axis (Utils.os + "_" + name + "_Horizontal");
				verticalAxis = new Axis (Utils.os + "_" + name + "_Vertical");
			}
			
			if (index == 2) { // DPad
				horizontalAxis = new Axis (Utils.os + "_" + name + "_Horizontal", 
					Utils.os + "_" + name + "_Right", 
					Utils.os + "_" + name + "_Left");
				verticalAxis = new Axis (Utils.os + "_" + name + "_Vertical", 
					Utils.os + "_" + name + "_Up", 
					Utils.os + "_" + name + "_Down");
			}
		}


		// --------------------
		// PRIVATE PROPERTIES
		// --------------------

		private readonly Axis horizontalAxis;
		private readonly Axis verticalAxis;
		private Vector2 positionRaw;


		// --------------------
		// UPDATE
		// --------------------

		protected virtual Vector2 GetPosition() {
			if (Settings.stickDeadZone.IsEqualTo(1)) return Vector2.zero;
			if (positionRaw.magnitude < Settings.stickDeadZone) return Vector2.zero;
			
			return (Utils.stickPositionMultiplier/(1 - Settings.stickDeadZone) 
				        *(positionRaw - positionRaw.normalized*Settings.stickDeadZone)).Clamp(-1, 1);
		}

		/// <summary>
		/// Hinput internal method. You don't need to use it.
		/// </summary>
		public void Update () {
			if (!isEnabled) return;

			//Update axes first, then position, then inPressedZone, then directions.
			if (horizontalAxis != null && verticalAxis != null) 
				positionRaw = new Vector2(horizontalAxis.GetPosition(), verticalAxis.GetPosition());

			position = GetPosition();

			inPressedZone.Update();

			up.Update();
			down.Update();
			left.Update();
			right.Update();
			
			upLeft.Update();
			downLeft.Update();
			upRight.Update();
			downRight.Update();
		}

		
		// --------------------
		// PUBLIC PROPERTIES
		// --------------------

		/// <summary>
		/// The coordinates of a stick.
		/// </summary>
		public Vector2 position { get; private set; }

		/// <summary>
		/// The position of a stick along the horizontal axis (between -1 and 1).
		/// </summary>
		public float horizontal { get { return position.x; } }

		/// <summary>
		/// The position of a stick along the vertical axis (between -1 and 1).
		/// </summary>
		public float vertical { get { return position.y; } }

		/// <summary>
		/// The distance from the current position of a stick to its origin (between 0 and 1).
		/// </summary>
		public float distance { get { return Mathf.Clamp01(position.magnitude); } }
				
		/// <summary>
		/// The angle between the current position of a stick and the horizontal axis 
		/// (In degrees : left=180, up=90, right=0, down=-90).
		/// </summary>
		public float angle {
			get {
				if (position.sqrMagnitude.IsEqualTo(0)) return 0;
				return Vector2.SignedAngle(Vector2.right, position);
			} 
		}

		/// <summary>
		/// The coordinates of a stick as a Vector3 facing the camera (The horizontal and vertical coordinates of the
		/// stick are interpreted as the right and up directions of the camera).
		/// </summary>
		public Vector3 worldPositionCamera { 
			get { 
				try { return (Settings.worldCamera.right*horizontal + Settings.worldCamera.up*vertical); }
				catch { Debug.LogError ("Hinput error : No camera found !"); }
				return Vector2.zero;
			} 
		}

		/// <summary>
		/// The coordinates of a stick as a horizontal Vector3, with a y value of 0 (The horizontal and vertical
		/// coordinates of the stick are interpreted as the absolute right and forward directions).
		/// </summary>
		public Vector3 worldPositionFlat { get { return new Vector3 (horizontal, 0, vertical); } }

		/// <summary>
		/// The virtual button considered pressed if a stick is pushed in any
		/// direction.
		/// </summary>
		public readonly StickPressedZone inPressedZone;

		/// <summary>
		/// The virtual button considered pressed if a stick is pushed up.
		/// </summary>
		public readonly StickDirection up;

		/// <summary>
		/// The virtual button considered pressed if a stick is pushed down.
		/// </summary>
		public readonly StickDirection down;

		/// <summary>
		/// The virtual button considered pressed if a stick is pushed left.
		/// </summary>
		public readonly StickDirection left;

		/// <summary>
		/// The virtual button considered pressed if a stick is pushed right.
		/// </summary>
		public readonly StickDirection right;

		/// <summary>
		/// The virtual button considered pressed if a stick is pushed up-left.
		/// </summary>
		public readonly StickDirection upLeft;

		/// <summary>
		/// The virtual button considered pressed if a stick is pushed down-left.
		/// </summary>
		public readonly StickDirection downLeft;

		/// <summary>
		/// The virtual button considered pressed if a stick is pushed up-right.
		/// </summary>
		public readonly StickDirection upRight;

		/// <summary>
		/// The virtual button considered pressed if a stick is pushed down-right.
		/// </summary>
		public readonly StickDirection downRight;
	}
}