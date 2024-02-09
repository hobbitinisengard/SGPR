// Author : Henri Couvreur for hiloqo, 2019
// Contact : hello@hinput.co

using System.Collections.Generic;
using HinputClasses;
using HinputClasses.Internal;

/// <summary>
/// The main class of the plugin, used to access gamepads.
/// </summary>
public static class Hinput {
	// --------------------
	// GAMEPAD
	// --------------------

	private static List<Gamepad> _gamepad;
	/// <summary>
	/// A list of 8 gamepads, labelled 0 to 7.
	/// </summary>
	public static List<Gamepad> gamepad { 
		get {
			Updater.CheckInstance();
			if (_gamepad == null) {
				_gamepad = new List<Gamepad>();
				for (int i=0; i<Utils.maxGamepads; i++) _gamepad.Add(new Gamepad(i));
			}

			return _gamepad; 
		} 
	}
	
	
	// --------------------
	// ANYGAMEPAD
	// --------------------

	private static Gamepad _anyGamepad;
	/// <summary>
	/// A virtual gamepad that returns the inputs of every gamepad at once.<br/> <br/>
	///
	/// A button is pressed on AnyGamepad if there is at least one gamepad on which it is pressed. It is released on
	/// AnyGamepad if it is released on all gamepads.<br/> 
	/// The position of a stick on AnyGamepad is the average position of pushed sticks of that type on all
	/// gamepads.<br/> 
	/// Vibrating AnyGamepad vibrates all gamepads.<br/> <br/>
	/// 
	/// Examples: <br/>
	/// - If player 1 pushed their A button and player 2 pushed their B button, both the A and the B button of
	/// AnyGamepad will be pressed.<br/>
	/// - If both player 1 and player 2 hold their A button, and player 1 releases it, the A button of AnyGamepad will
	/// still be considered pressed, and will NOT trigger a justReleased event.<br/>
	/// - If player 1 pushed their left trigger by 0.2 and player 2 pushed theirs by 0.6, the left trigger of
	/// AnyGamepad will have a position of 0.6.<br/>
	/// - If player 1 positioned their right stick at (-0.2, 0.9) and player 2 has theirs at (0, 0), the
	/// position of the right stick of AnyGamepad will be (-0.2, 0.9).<br/>
	/// - If player 1 positioned their right stick at (-0.2, 0.9) and player 2 has theirs at (0.6, 0.3), the
	/// position of the right stick of AnyGamepad will be the average of both positions, (0.2, 0.6).
	/// </summary>
	public static Gamepad anyGamepad { 
		get { 
			Updater.CheckInstance();
			if (_anyGamepad == null) _anyGamepad = new AnyGamepad();
			return _anyGamepad; 
		}
	}


	// --------------------
	// ANYINPUT
	// --------------------
	
	/// <summary>
	/// A virtual button that returns every input of every gamepad at once.
	/// AnyInput is considered pressed if at least one input on that gamepad is pressed.
	/// AnyInput is considered released if every input on that gamepad is released.
	/// </summary>
	public static Pressable anyInput { get { return anyGamepad.anyInput; } }
}