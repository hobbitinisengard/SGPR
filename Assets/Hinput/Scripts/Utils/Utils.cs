using System.Collections;
using System.IO;
using UnityEngine;

namespace HinputClasses.Internal {
    // This class gathers a couple of useful variables and methods.
	public static class Utils {
		// --------------------
		// INTERNAL SETTINGS
		// --------------------

		//The maximum amount of gamepads supported by Hinput
		public const float maxGamepads = 8;

		//By how much to increase stick position (otherwise max stick distance is sometimes less than 1).
		public const float stickPositionMultiplier = 1.01f;
		
		// The location of the project's input manager file.
		public const string inputManagerPath = "./ProjectSettings/InputManager.asset";


		// --------------------
		// SETUP
		// --------------------

		// Returns true if Hinput is currently installed, false otherwise.
		public static bool HinputIsInstalled() {
			return (File.ReadAllText(inputManagerPath).Contains(HinputInputArray()));
		}

		// Locates the input array of Hinput, and returns its contents as a string. Logs an error if it is not present.
		public static string HinputInputArray () {
			try { return File.ReadAllText("./Assets/Hinput/Scripts/Setup/Hinput_8Controllers_inputManager"); } 
			catch { MissingInputArrayError(); }
			return null;
		}


		// --------------------
		// BUTTONS AND AXES
		// --------------------

		public static bool GetButton (string fullName) {
			try { return Input.GetButton (fullName); } 
			catch { HinputNotSetUpError (); }
			return false;
		}

		public static float GetAxis (string fullName) { return GetAxis (fullName, true); }
		public static float GetAxis (string fullName, bool logError) {
			try { return Input.GetAxisRaw (fullName); } 
			catch { if (logError) HinputNotSetUpError (); }
			return 0;
		}


		// --------------------
		// STICKS
		// --------------------

		// Returns true if stick is currently within a (Settings.stickType) degree arc oriented at angle.
		public static bool PushedTowards (this Stick stick, float angle) { 
			return (Mathf.Abs(Mathf.DeltaAngle(angle, stick.angle)) < ((float)Settings.stickType) / 2); 
		}


		// --------------------
		// COROUTINES
		// --------------------

		// A way of delegating StartCoroutine for classes that don't inherit MonoBehaviour.
		public static void Coroutine (IEnumerator coroutine) {
			Updater.instance.StartCoroutine(coroutine);
		}

		// A way of delegating StopAllCoroutines for classes that don't inherit MonoBehaviour.
		public static void StopRoutines () {
			Updater.instance.StopAllCoroutines();
		}


		// --------------------
		// OPERATING SYSTEM
		// --------------------
		
		public enum OS { Unknown, Windows, Mac, Linux }

		//The user's operating system. Assigned when first called.
		private static OS _os = OS.Unknown;
		public static OS os { 
			get { 
				if (_os == OS.Unknown) {
					string systemOs = SystemInfo.operatingSystem;
					if (systemOs.Contains("Windows")) _os = OS.Windows;
					else if (Application.platform == RuntimePlatform.XboxOne) _os = OS.Windows;
					else if (systemOs.Contains("Mac")) _os = OS.Mac;
					else if (systemOs.Contains("Linux")) _os = OS.Linux;
					else Debug.LogError("Hinput Error : Unknown OS !");
				}

				return _os;
			} 
		}


		// --------------------
		// FLOAT COMPARISON
		// --------------------

		public static bool IsEqualTo (this float target, float other) {
			return Mathf.Abs(target - other) < Mathf.Epsilon;
		}

		public static bool IsNotEqualTo (this float target, float other) {
			return Mathf.Abs(target - other) > Mathf.Epsilon;
		}

		public static Vector2 Clamp(this Vector2 target, float min, float max) {
			return new Vector2(
				Mathf.Clamp(target.x, min, max),
				Mathf.Clamp(target.y, min, max));
		}


		// --------------------
		// ERROR MESSAGES
		// --------------------

		private static void HinputNotSetUpError () {
			Debug.LogWarning("Warning : Hinput has not been set up, so gamepad inputs cannot be recorded. "+
			                 "To set it up, go to the Tools menu and click \"Hinput > Set up Hinput\".");
		}

		public static void MissingInputArrayError() {
			Debug.LogError("Hinput setup error : /Assets/Hinput/Scripts/Setup/Hinput_8Controllers_inputManager" +
			               " not found. Make sure this file is present in your project, or reinstall the package.");
		}

		public static void SetupError() {
			Debug.LogWarning("Error while setting up Hinput. Try reinstalling the plugin and rebooting your" +
			                 " computer. If the problem persists, please contact me at hello@hinput.co.");
		}

		public static void UninstallError() {
			Debug.LogWarning("Error while uninstalling Hinput. Try reinstalling the plugin and rebooting " +
			                 "your computer. If the problem persists, please contact me at hello@hinput.co.");
		}

		public static void VibrationNotAvailableError() {
			if (os != OS.Windows) Debug.LogWarning("Hinput warning : vibration is only supported on Windows computers.");
			else Debug.LogWarning("Hinput warning : vibration is only supported on four controllers.");
		}
	}
}