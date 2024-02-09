using UnityEngine;

namespace HinputClasses {
    /// <summary>
	/// Hinput class responsible for handling settings.<br/> <br/>
	/// Can be attached to a gameobject to expose settings. Otherwise it will automatically be instantiated at runtime
	/// the first time Hinput is called, with default settings.
	/// </summary>
	public class Settings : MonoBehaviour {
		// --------------------
		// SINGLETON PATTERN
		// --------------------

		//The instance of Settings. Assigned when first called.
		private static Settings _instance;
		public static Settings instance { 
			get {
				CheckInstance();
				return _instance;
			} 
		}

		private static void CheckInstance() {
			if (_instance != null) return;
			
			GameObject go = new GameObject {name = "Hinput Settings"};
			_instance = go.AddComponent<Settings>();
		}

		private void Awake () {
			if (_instance == null) _instance = this;
			if (_instance != this) Destroy(this);
			DontDestroyOnLoad (this);
		}


		// --------------------
		// IMPLICIT CONVERSION
		// --------------------

		public enum DefaultPressTypes { SimplePress, DoublePress, LongPress }
		[Header("Implicit Conversion")]

		[SerializeField]
		[Tooltip("The default conversion of Pressable to Press values\n\n"+
		         "Determines how Hinput interprets buttons, triggers and stick directions when the type of Press to " +
		         "use is not specified")]
		private DefaultPressTypes _defaultPressType = DefaultPressTypes.SimplePress;
		/// <summary>
		/// The default conversion of Pressable to boolean values<br/> <br/>
		/// Determines how Hinput interprets buttons, triggers and stick directions when the type of Press to use is
		/// not specified
		/// </summary>
		public static DefaultPressTypes defaultPressType { 
			get { return instance._defaultPressType; } 
			set { instance._defaultPressType = value; }  
		}
		
		public enum DefaultPressFeatures { Pressed, JustPressed, Released, JustReleased }
		[SerializeField]
		[Tooltip("The default conversion of Press and Pressable to boolean values\n\n"+
		         "Determines how Hinput interprets buttons, triggers and stick directions when the feature to use " +
		         "is not specified.")]
		private DefaultPressFeatures _defaultPressFeature = DefaultPressFeatures.Pressed;
		/// <summary>
		/// The default conversion of Press and Pressable to boolean values<br/> <br/>
		/// Determines how Hinput interprets buttons, triggers and stick directions when the feature to use is not
		/// specified.
		/// </summary>
		public static DefaultPressFeatures defaultPressFeature { 
			get { return instance._defaultPressFeature; } 
			set { instance._defaultPressFeature = value; }  
		}


		// --------------------
		// PRESSES
		// --------------------
		
		[Header("Presses")]

		[SerializeField]
		[Range(0,2)]
		[Tooltip("The maximum duration between the start of two presses for them to be considered a double press.")]
		private float _doublePressDuration = 0.3f;
		/// <summary>
		/// The maximum duration between the start of two presses for them to be considered a double press.
		/// </summary>
		public static float doublePressDuration { 
			get { return instance._doublePressDuration; } 
			set { instance._doublePressDuration = value; }  
		}

		[SerializeField]
		[Range(0,2)]
		[Tooltip("The minimum duration of a press for it to be considered a long press.")]
		private float _longPressDuration = 0.3f;
		/// <summary>
		/// The minimum duration of a press for it to be considered a long press.
		/// </summary>
		public static float longPressDuration { 
			get { return instance._longPressDuration; } 
			set { instance._longPressDuration = value; }  
		}


		// --------------------
		// STICKS
		// --------------------

		public enum StickTypes { FourDirections = 90, EightDirections = 45 }
		[Header("Sticks")]

		[SerializeField]
		[Tooltip("The type of stick to use.\n\n"+
		         "- Set it to Four Directions for 4-directional sticks, with virtual buttons that span 1/4 of a " +
		         "circle (90 degrees). Use diagonals with caution in this case.\n\n" +
		         "- Set it to Eight Directions for 8-directional sticks, with virtual buttons that span 1/8 of a " +
		         "circle (45 degrees).")]
		private StickTypes _stickType = StickTypes.EightDirections;
		/// <summary>
		/// The type of stick to use. <br/> <br/>
		/// - Set it to Four Directions for 4-directional sticks, with virtual buttons that span 1/4 of a circle
		/// (90 degrees). Use diagonals with caution in this case.<br/>
		/// - Set it to Eight Directions for 8-directional sticks, with virtual buttons that span 1/8 of a circle
		/// (45 degrees).
		/// </summary>
		public static StickTypes stickType { 
			get { return instance._stickType; } 
			set { instance._stickType = value; }  
		}

		[SerializeField]
		[Range(0,1)]
		[Tooltip("The distance from the origin beyond which stick inputs start being registered.")]
		private float _stickDeadZone = 0.2f;
		/// <summary>
		/// The distance from the origin beyond which stick inputs start being registered.
		/// </summary>
		public static float stickDeadZone { 
			get { return instance._stickDeadZone; } 
			set { instance._stickDeadZone = value; } 
		}

		[SerializeField]
		[Range(0,1)]
		[Tooltip("The distance from the end of the dead zone beyond which stick inputs are considered pushed.")]
		private float _stickPressedZone = 0.5f;
		/// <summary>
		/// The distance from the end of the dead zone beyond which stick inputs are considered pushed.
		/// </summary>
		public static float stickPressedZone { 
			get { return instance._stickPressedZone; } 
			set { instance._stickPressedZone = value; }  
		}

		[SerializeField]
		[Tooltip("The Camera on which the worldPositionCamera feature of Stick should be based. If no Camera is set, " +
		         "Hinput will try to find one on the scene.")]
		private Transform _worldCamera = null;
		/// <summary>
		/// The Camera on which the worldPositionCamera feature of Stick should be based. If no Camera is set,  
		/// Hinput will try to find one on the scene.
		/// </summary>
		public static Transform worldCamera { 
			get { 
				if (instance._worldCamera == null) {
					if (Camera.main != null) instance._worldCamera = Camera.main.transform;
					else if (FindObjectOfType<Camera>() != null) 
						instance._worldCamera = FindObjectOfType<Camera>().transform;
					else return null;
				}

				return instance._worldCamera;
			} 
			set { instance._worldCamera = value; } 
		}


		// --------------------
		// TRIGGERS
		// --------------------

		[Header("Triggers")]

		[SerializeField]
		[Range(0,1)]
		[Tooltip("The distance from the origin beyond which trigger inputs start being registered.")]
		private float _triggerDeadZone = 0.1f;
		/// <summary>
		/// The distance from the origin beyond which trigger inputs start being registered.
		/// </summary>
		public static float triggerDeadZone { 
			get { return instance._triggerDeadZone; } 
			set { instance._triggerDeadZone = value; } 
		}

		[SerializeField]
		[Range(0,1)]
		[Tooltip("The distance from the end of the dead zone beyond which trigger inputs are considered pushed.")]
		private float _triggerPressedZone = 0.5f;
		/// <summary>
		/// The distance from the end of the dead zone beyond which trigger inputs are considered pushed.
		/// </summary>
		public static float triggerPressedZone { 
			get { return instance._triggerPressedZone; } 
			set { instance._triggerPressedZone = value; }  
		}


		// --------------------
		// VIBRATION DEFAULTS
		// --------------------

		[Header("Vibration Defaults")]

		[SerializeField]
		[Range(0,1)]
		[Tooltip("The default intensity of the left (low-frequency) motor when controllers vibrate.\n\n"+
		         "The left motor's vibration feels like a low rumble.")]
		private float _vibrationDefaultLeftIntensity = 0.2f;
		/// <summary>
		/// The default intensity of the left (low-frequency) motor when controllers vibrate.<br/> <br/>
		/// The left motor's vibration feels like a low rumble.
		/// </summary>
		public static float vibrationDefaultLeftIntensity { 
			get { return instance._vibrationDefaultLeftIntensity; } 
			set { instance._vibrationDefaultLeftIntensity = value; }  
		}

		[SerializeField]
		[Range(0,1)]
		[Tooltip("The default intensity of the right (high-frequency) motor when controllers vibrate.\n\n" +
		         "The right motor's vibration feels like a sharp buzz.")]
		private float _vibrationDefaultRightIntensity = 0.8f;
		/// <summary>
		/// The default intensity of the right (high-frequency) motor when controllers vibrate.<br/> <br/>
		/// The right motor's vibration feels like a sharp buzz.
		/// </summary>
		public static float vibrationDefaultRightIntensity { 
			get { return instance._vibrationDefaultRightIntensity; } 
			set { instance._vibrationDefaultRightIntensity = value; }  
		}

		[SerializeField]
		[Range(0,2)]
		[Tooltip("The default duration of gamepad vibrations.")]
		private float _vibrationDefaultDuration = 0.2f;
		/// <summary>
		/// The default duration of gamepad vibrations.
		/// </summary>
		public static float vibrationDefaultDuration { 
			get { return instance._vibrationDefaultDuration; } 
			set { instance._vibrationDefaultDuration = value; }  
		}


		// --------------------
		// PERFORMANCE
		// --------------------

		[Header("Performance")]
		[SerializeField]
		[Range(0,8)]
		[Tooltip("The maximum amount of gamepads to be tracked by Hinput.\n\n" +
		         "Reducing this before entering play mode may improve performance.")]
		private int _amountOfGamepads = 8;
		/// <summary>
		/// The maximum amount of gamepads to be tracked by Hinput.<br/> <br/>
		/// Reducing this before entering play mode may improve performance.
		/// </summary>
		public static int amountOfGamepads { 
			get { return instance._amountOfGamepads; } 
			set { instance._amountOfGamepads = value; }  
		}

		[SerializeField]
		[Tooltip("Checking this before entering play mode will cause the AnyGamepad feature to not be tracked by " +
		         "Hinput. \n\n" +
		         "This may improve performance.")]
		private bool _disableAnyGamepad = false;
		/// <summary>
		/// Checking this before entering play mode will cause the AnyGamepad feature to not be tracked by Hinput.
		/// <br/> <br/>
		/// This may improve performance.
		/// </summary>
		public static bool disableAnyGamepad { 
			get { return instance._disableAnyGamepad; } 
			set { instance._disableAnyGamepad = value; }  
		}

		[SerializeField]
		[Tooltip("Checking this before entering play mode will cause the A button of gamepads to not be tracked by " +
		         "Hinput. \n\n" +
		         "This may improve performance.")]
		private bool _disableA = false;
		/// <summary>
		/// Checking this before entering play mode will cause the A button of gamepads to not be tracked by Hinput.
		/// <br/> <br/>
		/// This may improve performance.
		/// </summary>
		public static bool disableA { 
			get { return instance._disableA; } 
			set { instance._disableA = value; }  
		}

		[SerializeField]
		[Tooltip("Checking this before entering play mode will cause the B button of gamepads to not be tracked by " +
		         "Hinput. \n\n" +
		         "This may improve performance.")]
		private bool _disableB = false;
		/// <summary>
		/// Checking this before entering play mode will cause the B button of gamepads to not be tracked by Hinput.
		/// <br/> <br/>
		/// This may improve performance.
		/// </summary>
		public static bool disableB { 
			get { return instance._disableB; } 
			set { instance._disableB = value; }  
		}

		[SerializeField]
		[Tooltip("Checking this before entering play mode will cause the X button of gamepads to not be tracked by " +
		         "Hinput. \n\n" +
		         "This may improve performance.")]
		private bool _disableX = false;
		/// <summary>
		/// Checking this before entering play mode will cause the X button of gamepads to not be tracked by Hinput.
		/// <br/> <br/>
		/// This may improve performance.
		/// </summary>
		public static bool disableX { 
			get { return instance._disableX; } 
			set { instance._disableX = value; }  
		}

		[SerializeField]
		[Tooltip("Checking this before entering play mode will cause the Y button of gamepads to not be tracked by " +
		         "Hinput. \n\n" +
		         "This may improve performance.")]
		private bool _disableY = false;
		/// <summary>
		/// Checking this before entering play mode will cause the Y button of gamepads to not be tracked by Hinput.
		/// <br/> <br/>
		/// This may improve performance.
		/// </summary>
		public static bool disableY { 
			get { return instance._disableY; } 
			set { instance._disableY = value; }  
		}

		[SerializeField]
		[Tooltip("Checking this before entering play mode will cause the left bumper of gamepads to not be tracked by " +
		         "Hinput. \n\n" +
		         "This may improve performance.")]
		private bool _disableLeftBumper = false;
		/// <summary>
		/// Checking this before entering play mode will cause the left bumper of gamepads to not be tracked by Hinput.
		/// <br/> <br/>
		/// This may improve performance.
		/// </summary>
		public static bool disableLeftBumper { 
			get { return instance._disableLeftBumper; } 
			set { instance._disableLeftBumper = value; }  
		}

		[SerializeField]
		[Tooltip("Checking this before entering play mode will cause the right bumper of gamepads to not be tracked " +
		         "by Hinput. \n\n" +
		         "This may improve performance.")]
		private bool _disableRightBumper = false;
		/// <summary>
		/// Checking this before entering play mode will cause the right bumper of gamepads to not be tracked by Hinput.
		/// <br/> <br/>
		/// This may improve performance.
		/// </summary>
		public static bool disableRightBumper { 
			get { return instance._disableRightBumper; } 
			set { instance._disableRightBumper = value; }  
		}

		[SerializeField]
		[Tooltip("Checking this before entering play mode will cause the left trigger of gamepads to not be tracked " +
		         "by Hinput. \n\n" +
		         "This may improve performance.")]
		private bool _disableLeftTrigger = false;
		/// <summary>
		/// Checking this before entering play mode will cause the left trigger of gamepads to not be tracked by Hinput.
		/// <br/> <br/>
		/// This may improve performance.
		/// </summary>
		public static bool disableLeftTrigger { 
			get { return instance._disableLeftTrigger; } 
			set { instance._disableLeftTrigger = value; }  
		}

		[SerializeField]
		[Tooltip("Checking this before entering play mode will cause the right trigger of gamepads to not be tracked " +
		         "by Hinput. \n\n" +
		         "This may improve performance.")]
		private bool _disableRightTrigger = false;
		/// <summary>
		/// Checking this before entering play mode will cause the right trigger of gamepads to not be tracked by
		/// Hinput. <br/> <br/>
		/// This may improve performance.
		/// </summary>
		public static bool disableRightTrigger { 
			get { return instance._disableRightTrigger; } 
			set { instance._disableRightTrigger = value; }  
		}

		[SerializeField]
		[Tooltip("Checking this before entering play mode will cause the back button of gamepads to not be tracked " +
		         "by Hinput. \n\n" +
		         "This may improve performance.")]
		private bool _disableBack = false;
		/// <summary>
		/// Checking this before entering play mode will cause the back button of gamepads to not be tracked by Hinput.
		/// <br/> <br/>
		/// This may improve performance.
		/// </summary>
		public static bool disableBack { 
			get { return instance._disableBack; } 
			set { instance._disableBack = value; }  
		}

		[SerializeField]
		[Tooltip("Checking this before entering play mode will cause the start button of gamepads to not be tracked " +
		         "by Hinput. \n\n" +
		         "This may improve performance.")]
		private bool _disableStart = false;
		/// <summary>
		/// Checking this before entering play mode will cause the start button of gamepads to not be tracked by Hinput.
		/// <br/> <br/>
		/// This may improve performance.
		/// </summary>
		public static bool disableStart { 
			get { return instance._disableStart; } 
			set { instance._disableStart = value; }  
		}

		[SerializeField]
		[Tooltip("Checking this before entering play mode will cause the left stick click of gamepads to not be " +
		         "tracked by Hinput. \n\n" +
		         "This may improve performance.")]
		private bool _disableLeftStickClick = false;
		/// <summary>
		/// Checking this before entering play mode will cause the left stick click of gamepads to not be tracked by
		/// Hinput. <br/> <br/>
		/// This may improve performance.
		/// </summary>
		public static bool disableLeftStickClick { 
			get { return instance._disableLeftStickClick; } 
			set { instance._disableLeftStickClick = value; }  
		}

		[SerializeField]
		[Tooltip("Checking this before entering play mode will cause the right stick click of gamepads to not be " +
		         "tracked by Hinput. \n\n" +
		         "This may improve performance.")]
		private bool _disableRightStickClick = false;
		/// <summary>
		/// Checking this before entering play mode will cause the right stick click of gamepads to not be tracked by
		/// Hinput. <br/> <br/>
		/// This may improve performance.
		/// </summary>
		public static bool disableRightStickClick { 
			get { return instance._disableRightStickClick; } 
			set { instance._disableRightStickClick = value; }  
		}

		[SerializeField]
		[Tooltip("Checking this before entering play mode will cause the xBox button of gamepads to not be tracked " +
		         "by Hinput. \n\n" +
		         "This may improve performance.")]
		private bool _disableXBoxButton = false;
		/// <summary>
		/// Checking this before entering play mode will cause the xBox button of gamepads to not be tracked by Hinput.
		/// <br/> <br/>
		/// This may improve performance.
		/// </summary>
		public static bool disableXBoxButton { 
			get { return instance._disableXBoxButton; } 
			set { instance._disableXBoxButton = value; }  
		}

		[SerializeField]
		[Tooltip("Checking this before entering play mode will cause the left stick of gamepads to not be tracked " +
		         "by Hinput. \n\n" +
		         "This may improve performance.")]
		private bool _disableLeftStick = false;
		/// <summary>
		/// Checking this before entering play mode will cause the left stick of gamepads to not be tracked by Hinput.
		/// <br/> <br/>
		/// This may improve performance.
		/// </summary>
		public static bool disableLeftStick { 
			get { return instance._disableLeftStick; } 
			set { instance._disableLeftStick = value; }  
		}

		[SerializeField]
		[Tooltip("Checking this before entering play mode will cause the right stick of gamepads to not be tracked " +
		         "by Hinput. \n\n" +
		         "This may improve performance.")]
		private bool _disableRightStick = false;
		/// <summary>
		/// Checking this before entering play mode will cause the right stick of gamepads to not be tracked by Hinput.
		/// <br/> <br/>
		/// This may improve performance.
		/// </summary>
		public static bool disableRightStick { 
			get { return instance._disableRightStick; } 
			set { instance._disableRightStick = value; }  
		}

		[SerializeField]
		[Tooltip("Checking this before entering play mode will cause the D-pad of gamepads to not be tracked by " +
		         "Hinput. \n\n" +
		         "This may improve performance.")]
		private bool _disableDPad = false;
		/// <summary>
		/// Checking this before entering play mode will cause the D-pad of gamepads to not be tracked by Hinput.
		/// <br/> <br/>
		/// This may improve performance.
		/// </summary>
		public static bool disableDPad { 
			get { return instance._disableDPad; } 
			set { instance._disableDPad = value; }  
		}

		[SerializeField]
		[Tooltip("Checking this before entering play mode will cause the AnyInput feature of gamepads to not be " +
		         "tracked by Hinput. \n\n" +
		         "This may improve performance.")]
		private bool _disableAnyInput = false;
		/// <summary>
		/// Checking this before entering play mode will cause the anyInput feature of gamepads to not be tracked by
		/// Hinput. <br/> <br/>
		/// This may improve performance.
		/// </summary>
		public static bool disableAnyInput { 
			get { return instance._disableAnyInput; } 
			set { instance._disableAnyInput = value; }  
		}
    }
}