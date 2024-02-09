using System.Collections.Generic;
using System.Linq;

namespace HinputClasses.Internal {
    /// <summary>
    /// Hinput class representing every input of a controller at once
    /// </summary>
    public class AnyInput : GamepadPressable {
        // --------------------
        // CONSTRUCTOR
        // --------------------

        public AnyInput(string pressableName, Gamepad gamepad, bool isEnabled) : 
            base(pressableName, gamepad, -1, isEnabled) {
            inputs = new List<Pressable> {
                gamepad.A, gamepad.B, gamepad.X, gamepad.Y,
                gamepad.leftBumper, gamepad.rightBumper, 
                gamepad.leftTrigger, gamepad.rightTrigger,
                gamepad.back, gamepad.start, 
                gamepad.leftStickClick, gamepad.rightStickClick,
                gamepad.leftStick.inPressedZone, gamepad.rightStick.inPressedZone, gamepad.dPad.inPressedZone
            };
        }
        
        
        // --------------------
        // INPUTS
        // --------------------
        
        // Every input on this gamepad
        private readonly List<Pressable> inputs;
        

        // --------------------
        // UPDATE
        // --------------------

        protected override bool GetPressed() { return inputs.Any(input => input.simplePress.pressed); }
    }
}