using UnityEngine;

namespace HinputClasses {
    /// <summary>
    /// Hinput class representing a specific way of pressing a button, like a regular press, a double press or a long
    /// press.
    /// </summary>
    public class Press {
        // --------------------
        // ID
        // --------------------
        
        /// <summary>
        /// The button a press refers to.
        /// </summary>
        public Pressable button;
        
        /// <summary>
        /// The gamepad of the button a press refers to.
        /// </summary>
        public Gamepad gamepad;
        
        
        // --------------------
        // PRIVATE PROPERTIES
        // --------------------
        
        private int lastPressedFrame = -1; // *force wave* this input was never pressed
        private int lastReleasedFrame = -1;
        private float lastPressed = Mathf.NegativeInfinity;
        private float lastReleased = Mathf.NegativeInfinity;
        
        
        // --------------------
        // IMPLICIT CONVERSIONS
        // --------------------
        
        public static implicit operator bool (Press press) {
            if (Settings.defaultPressFeature == Settings.DefaultPressFeatures.JustReleased) return press.justReleased;
            if (Settings.defaultPressFeature == Settings.DefaultPressFeatures.Released) return press.released;
            if (Settings.defaultPressFeature == Settings.DefaultPressFeatures.JustPressed) return press.justPressed;
            return press.pressed;
        }
        
        
        // --------------------
        // CONSTRUCTOR
        // --------------------

        public Press(Pressable button) {
            this.button = button;
            gamepad = button.gamepad;
        }
        
        
        // --------------------
        // UPDATE
        // --------------------

        public void Update(bool isPressed) {
            pressed = isPressed;
            
            if (pressed) {
                lastPressedFrame = Time.frameCount;
                lastPressed = Time.time;
            } else {
                lastReleasedFrame = Time.frameCount;
                lastReleased = Time.time;
            }
        }
        
        
        // --------------------
        // PUBLIC PROPERTIES
        // --------------------
		
        /// <summary>
        /// Returns true if a press is pressed. Returns false otherwise.
        /// </summary>
        public bool pressed { get; private set; }
		
        /// <summary>
        /// Returns true if a press is released. Returns false otherwise.
        /// </summary>
        public bool released { get { return !pressed; } }
        
        /// <summary>
        /// Returns true if a press has been pressed this frame. Returns false otherwise.
        /// </summary>
        public bool justPressed { get { return (pressed && (lastPressedFrame - lastReleasedFrame) == 1); } }
		
        /// <summary>
        /// Returns true if a press has been released this frame. Returns false otherwise.
        /// </summary>
        public bool justReleased { get { return (released && (lastReleasedFrame - lastPressedFrame) == 1); } }

        /// <summary>
        /// How long a press has been pressed (0 if it is released).
        /// </summary>
        public float pressDuration { get { return Mathf.Max(lastPressed - lastReleased, 0); } }

        /// <summary>
        /// How long a press has been released (0 if it is pressed).
        /// </summary>
        public float releaseDuration { get { return Mathf.Max(lastReleased - lastPressed, 0); } }
    }
}
