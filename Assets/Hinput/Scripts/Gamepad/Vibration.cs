using System.Collections;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
	using XInputDotNetPure;
#endif

namespace HinputClasses.Internal {
	// Hinput class handling the vibration of a gamepad.
	public class Vibration {
		// --------------------
		// PRIVATE PROPERTIES
		// --------------------

		#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
			private readonly PlayerIndex index;
		#endif
		private readonly bool canVibrate = false;
		private float prevLeft;
		private float prevRight;


		// --------------------
		// CONSTRUCTOR
		// --------------------

	    public Vibration (int index) {
		    if (Utils.os != Utils.OS.Windows) return;
		    if (index > 3) return;
		    
			
			#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
			    this.index = (PlayerIndex)index;
			    canVibrate = true;
			#endif
		    
	    }


		// --------------------
		// UPDATE
		// --------------------

		public void Update () {
			if (currentLeft.IsEqualTo(prevLeft) && currentRight.IsEqualTo(prevRight)) return;
			
			prevLeft = currentLeft;
			prevRight = currentRight;
			
			#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
				try { GamePad.SetVibration(index, currentLeft, currentRight); } 
				catch { /*Ignore exceptions here*/ }
			#endif
		}


		// --------------------
		// PUBLIC PROPERTIES
		// --------------------
		
		public float currentLeft;
		public float currentRight;


		// --------------------
		// PUBLIC METHODS
		// --------------------

		public void Vibrate (float left, float right, float duration) {
			if (canVibrate) Utils.Coroutine(_Vibrate(left, right, duration));
			else Utils.VibrationNotAvailableError();
		}

		public void Vibrate(AnimationCurve leftCurve, AnimationCurve rightCurve) {
			if (canVibrate) Utils.Coroutine(_Vibrate(leftCurve, rightCurve));
			else Utils.VibrationNotAvailableError();
		}

		public void Vibrate(VibrationPreset vibrationPreset, float left, float right, float duration) {
			if (canVibrate) {
				PresetCurves presetCurves = GetCurves(vibrationPreset, left, right, duration);
				Utils.Coroutine(_Vibrate(presetCurves.leftCurve, presetCurves.rightCurve));
			}
			else Utils.VibrationNotAvailableError();
		}

		public void VibrateAdvanced (float left, float right) {
			if (canVibrate) {
				currentLeft += left;
				currentRight += right;
			}
			else Utils.VibrationNotAvailableError();
		}

		public void StopVibration(float duration) {
			if (canVibrate) {
				Utils.StopRoutines();
				Utils.Coroutine(_StopVibration(duration));
			}
		}


		// --------------------
		// PRIVATE METHODS
		// --------------------

		private IEnumerator _Vibrate (float left, float right, float duration) {
			currentRight += right;
			currentLeft += left;
			yield return new WaitForSecondsRealtime (duration);
			currentRight -= right;
			currentLeft -= left;
		}

		private IEnumerator _Vibrate(AnimationCurve leftCurve, AnimationCurve rightCurve) {
			float time = 0;
			bool leftCurveIsOver = (leftCurve.keys.Length == 0);
			bool rightCurveIsOver = (rightCurve.keys.Length == 0);
			
			float leftCurveDuration = leftCurve.keys.Last().time;
			float rightCurveDuration = rightCurve.keys.Last().time;

			while(!leftCurveIsOver || !rightCurveIsOver) {
				float leftCurveValue;
				if (leftCurveIsOver) leftCurveValue = 0;
				else leftCurveValue = leftCurve.Evaluate(time);

				float rightCurveValue;
				if (rightCurveIsOver) rightCurveValue = 0;
				else rightCurveValue = rightCurve.Evaluate(time);

				time += Time.unscaledDeltaTime;
				leftCurveIsOver = (time > leftCurveDuration);
				rightCurveIsOver = (time > rightCurveDuration);
				
				currentLeft += leftCurveValue;
				currentRight += rightCurveValue;
				yield return new WaitForEndOfFrame();
				currentLeft -= leftCurveValue;
				currentRight -= rightCurveValue;
			}
		}

		private IEnumerator _StopVibration(float duration) {
			float originLeft = currentLeft;
			float originRight = currentRight;
			
			currentLeft = 0;
			currentRight = 0;

			float timeLeft = duration;
			while (timeLeft > 0) {
				timeLeft -= Time.unscaledDeltaTime;

				currentLeft += originLeft * timeLeft / duration;
				currentRight += originRight * timeLeft / duration;
				yield return new WaitForEndOfFrame();
				currentLeft -= originLeft * timeLeft / duration;
				currentRight -= originRight * timeLeft / duration;
			}
			
			Update(); // Stop vibration if gamepad is disabled (no more update)
		}


		// --------------------
		// VIBRATION PRESETS
		// --------------------

		private struct PresetCurves {
			public readonly AnimationCurve leftCurve;
			public readonly AnimationCurve rightCurve;

			public PresetCurves(AnimationCurve left, AnimationCurve right) {
				leftCurve = left;
				rightCurve = right;
			}
		}

		private PresetCurves GetCurves(VibrationPreset vibrationPreset, float left, float right, float duration) {
			AnimationCurve leftCurve = new AnimationCurve();
			AnimationCurve rightCurve = new AnimationCurve();
			if (vibrationPreset == VibrationPreset.ButtonPress) {
				leftCurve.AddKey(0, 0.5f);
				leftCurve.AddKey(0.1f, 0.5f);
				
				rightCurve.AddKey(0, 0.5f);
				rightCurve.AddKey(0.1f, 0.5f);
			}
			
			if (vibrationPreset == VibrationPreset.ImpactLight) {
				leftCurve.AddKey(0, 0f);
				leftCurve.AddKey(0.2f, 0f);
				
				rightCurve.AddKey(0, 0.5f);
				rightCurve.AddKey(0.2f, 0.5f);
			}
			
			if (vibrationPreset == VibrationPreset.Impact) {
				leftCurve.AddKey(0, 0.2f);
				leftCurve.AddKey(0.2f, 0.2f);
				
				rightCurve.AddKey(0, 0.8f);
				rightCurve.AddKey(0.2f, 0.8f);
			}
			
			if (vibrationPreset == VibrationPreset.ImpactHeavy) {
				leftCurve.AddKey(0, 0.5f);
				leftCurve.AddKey(0.2f, 0.5f);
				
				rightCurve.AddKey(0, 1);
				rightCurve.AddKey(0.2f, 1);
			}
			
			if (vibrationPreset == VibrationPreset.ExplosionShort) {
				leftCurve.AddKey(0, 0.6f);
				leftCurve.AddKey(0.2f, 0.6f);
				
				rightCurve.AddKey(0, 0.3f);
				rightCurve.AddKey(0.2f, 0.3f);
			}
			
			if (vibrationPreset == VibrationPreset.Explosion) {
				leftCurve.AddKey(0, 0.8f);
				leftCurve.AddKey(0.5f, 0.8f);
				
				rightCurve.AddKey(0, 0.4f);
				rightCurve.AddKey(0.5f, 0.4f);
			}
			
			if (vibrationPreset == VibrationPreset.ExplosionLong) {
				leftCurve.AddKey(0, 1f);
				leftCurve.AddKey(1f, 1f);
				leftCurve.AddKey(1.1f, 0f);
				
				rightCurve.AddKey(0, 0.5f);
				rightCurve.AddKey(1f, 0.5f);
				rightCurve.AddKey(1.1f, 0f);
			}
			
			if (vibrationPreset == VibrationPreset.AmbientSubtle) {
				leftCurve.AddKey(0, 0.1f);
				leftCurve.AddKey(10f, 0.1f);
				
				rightCurve.AddKey(0, 0);
				rightCurve.AddKey(10f, 0);
			}
			
			if (vibrationPreset == VibrationPreset.Ambient) {
				leftCurve.AddKey(0, 0.3f);
				leftCurve.AddKey(10f, 0.3f);
				
				rightCurve.AddKey(0, 0.1f);
				rightCurve.AddKey(10f, 0.1f);
			}
			
			if (vibrationPreset == VibrationPreset.AmbientStrong) {
				leftCurve.AddKey(0, 0.6f);
				leftCurve.AddKey(10f, 0.6f);
				
				rightCurve.AddKey(0, 0.3f);
				rightCurve.AddKey(10f, 0.3f);
			}

			// Multiply the duration and intensity of the curves
			leftCurve.keys = leftCurve.keys
				.ToList()
				.Select(key => new Keyframe(key.time * duration, key.value * left))
				.ToArray();
			
			rightCurve.keys = rightCurve.keys
				.ToList()
				.Select(key => new Keyframe(key.time * duration, key.value * right))
				.ToArray();

			return new PresetCurves(leftCurve, rightCurve);
		}
	}
}