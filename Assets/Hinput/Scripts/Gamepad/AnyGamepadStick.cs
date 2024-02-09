using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HinputClasses.Internal {
    /// <summary>
    /// Hinput class representing a given type of stick, such as a left stick, a right stick, or a D-Pad, on every
    /// gamepad at once.
    /// </summary>
    public class AnyGamepadStick : Stick {
        // --------------------
        // CONSTRUCTOR
        // --------------------

        public AnyGamepadStick(string stickName, Gamepad gamepad, int index)
            : base(stickName, gamepad, index, true) {
            sticks = Hinput.gamepad.Select(g => g.sticks[index]).ToList();
        }


        // --------------------
        // STICKS
        // --------------------

        // Every stick of this type
        private readonly List<Stick> sticks;


        // --------------------
        // PUBLIC PROPERTIES
        // --------------------

        protected override Vector2 GetPosition() {
            List<Stick> activeSticks = sticks.Where(s => s.distance.IsNotEqualTo(0)).ToList();
            if (activeSticks.Count == 0) return Vector2.zero;
            else return new Vector2(
                activeSticks.Average(stick => stick.horizontal),
                activeSticks.Average(stick => stick.vertical)
            );
        }
    }
}