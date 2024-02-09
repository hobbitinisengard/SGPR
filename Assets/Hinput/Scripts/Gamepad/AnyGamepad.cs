using System.Linq;
using UnityEngine;

namespace HinputClasses.Internal {
    /// <summary>
    /// Hinput class representing every gamepad at once.
    /// </summary>
    public class AnyGamepad : Gamepad {
        // --------------------
        // ID
        // --------------------

        public override string type { get { return "AnyGamepad"; } }
        public override bool isConnected { get { return Hinput.gamepad.Any(gamepad => gamepad.isConnected); } }
        
        
        // --------------------
        // CONSTRUCTOR
        // --------------------

        public AnyGamepad() {
            index = -1;
            name = "AnyGamepad";
            isEnabled = !Settings.disableAnyGamepad;
			
            A = new AnyGamepadButton ("A", this, 0, !Settings.disableA); 
            B = new AnyGamepadButton ("B", this, 1, !Settings.disableB);
            X = new AnyGamepadButton ("X", this, 2, !Settings.disableX);
            Y = new AnyGamepadButton ("Y", this, 3, !Settings.disableY);
			
            leftBumper = new AnyGamepadButton ("LeftBumper", this, 4, !Settings.disableLeftBumper);
            rightBumper = new AnyGamepadButton ("RightBumper", this, 5, !Settings.disableRightBumper);
            back = new AnyGamepadButton ("Back", this, 8, !Settings.disableBack);
            start = new AnyGamepadButton ("Start", this, 9, !Settings.disableStart);
			
            leftStickClick = new AnyGamepadButton ("LeftStickClick", this, 10, !Settings.disableLeftStickClick);
            rightStickClick = new AnyGamepadButton("RightStickClick", this, 11, !Settings.disableRightStickClick);

            leftStick = new AnyGamepadStick("LeftStick", this, 0);
            rightStick = new AnyGamepadStick("RightStick", this, 1);
            dPad = new AnyGamepadStick("DPad", this, 2);
            
            vibration = new Vibration (-1);

            SetUp();
        }
		
		
        // --------------------
        // UPDATE
        // --------------------

        protected override bool UpdateIsRequired() { return isEnabled; }
        

		// --------------------
		// VIBRATION
		// --------------------

		public override float leftVibration { get { return -1; } }

		public override float rightVibration { get { return -1; } }

		public override void Vibrate() {
			Hinput.gamepad.Take(4).ToList()
				.ForEach(gamepad => gamepad.Vibrate());
		}

		public override void Vibrate(float duration) {
			Hinput.gamepad.Take(4).ToList()
				.ForEach(gamepad => gamepad.Vibrate(duration));
		}
		
		public override void Vibrate(float leftIntensity, float rightIntensity) {
			Hinput.gamepad.Take(4).ToList()
				.ForEach(gamepad => gamepad.Vibrate(leftIntensity, rightIntensity));
		}
		
		public override void Vibrate(float leftIntensity, float rightIntensity, float duration) {
			Hinput.gamepad.Take(4).ToList()
				.ForEach(gamepad => gamepad.Vibrate(leftIntensity, rightIntensity, duration));
		}
		
		public override void Vibrate(AnimationCurve curve) {
			Hinput.gamepad.Take(4).ToList()
				.ForEach(gamepad => gamepad.Vibrate(curve));
		}
		
		public override void Vibrate(AnimationCurve leftCurve, AnimationCurve rightCurve) {
			Hinput.gamepad.Take(4).ToList()
				.ForEach(gamepad => gamepad.Vibrate(leftCurve, rightCurve));
		}

		public override void Vibrate(VibrationPreset vibrationPreset) {
			Hinput.gamepad.Take(4).ToList()
				.ForEach(gamepad => gamepad.Vibrate(vibrationPreset));
		}

		public override void Vibrate(VibrationPreset vibrationPreset, float duration) {
			Hinput.gamepad.Take(4).ToList()
				.ForEach(gamepad => gamepad.Vibrate(vibrationPreset, duration));
		}

		public override void Vibrate(VibrationPreset vibrationPreset, float leftIntensity, float rightIntensity) {
			Hinput.gamepad.Take(4).ToList()
				.ForEach(gamepad => gamepad.Vibrate(vibrationPreset, leftIntensity, rightIntensity));
		}

		public override void Vibrate(VibrationPreset vibrationPreset, float leftIntensity, float rightIntensity, 
			float duration) {
			Hinput.gamepad.Take(4).ToList()
				.ForEach(gamepad => gamepad.Vibrate(vibrationPreset, leftIntensity, rightIntensity, duration));
		}
		
		public override void VibrateAdvanced(float leftIntensity, float rightIntensity) {
			Hinput.gamepad.Take(4).ToList()
				.ForEach(gamepad => gamepad.VibrateAdvanced(leftIntensity, rightIntensity));
		}

		public override void StopVibration () {
			Hinput.gamepad.Take(4).ToList()
				.ForEach(gamepad => gamepad.StopVibration());
		}

		public override void StopVibration (float duration) {
			Hinput.gamepad.Take(4).ToList()
				.ForEach(gamepad => gamepad.StopVibration(duration));
		}
    }
}