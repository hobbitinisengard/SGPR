namespace HinputClasses {
    /// <summary>
    /// Hinput enum listing some vibration patterns that can be played on a gamepad.
    /// </summary>
    public enum VibrationPreset {
        /// <summary>
        /// A short vibration, suitable for feedback after the player pressed a button.<br/> <br/>
        /// Similar to Vibrate(0.5f, 0.5f, 0.1f).
        /// </summary>
        ButtonPress,
        
        /// <summary>
        /// A short and intense vibration, suitable for a light impact.<br/> <br/>
        /// Similar to Vibrate(0f, 0.5f, 0.2f).
        /// </summary>
        ImpactLight,
        
        /// <summary>
        /// A short and intense vibration, suitable for an impact.<br/> <br/>
        /// Similar to Vibrate(0.2f, 0.8f, 0.2f).
        /// </summary>
        Impact,
        
        /// <summary>
        /// A short and intense vibration, suitable for a heavy impact.<br/> <br/>
        /// Similar to Vibrate(0.5f, 1f, 0.2f).
        /// </summary>
        ImpactHeavy,
        
        /// <summary>
        /// A low and powerful vibration, suitable for a short or distant explosion.<br/> <br/>
        /// Similar to Vibrate(0.6f, 0.3f, 0.2f).
        /// </summary>
        ExplosionShort,
        
        /// <summary>
        /// A low and powerful vibration, suitable for an explosion.<br/> <br/>
        /// Similar to Vibrate(0.8f, 0.4f, 0.5f).
        /// </summary>
        Explosion,
        
        /// <summary>
        /// A low and powerful vibration, suitable for a long or nearby explosion.<br/> <br/>
        /// Similar to Vibrate(1f, 0.5f, 1f).
        /// </summary>
        ExplosionLong,
        
        /// <summary>
        /// A 10-second constant, low and subtle vibration, suitable for an ongoing event.<br/> <br/>
        /// Similar to Vibrate(0.1f, 0f, 10f).
        /// </summary>
        AmbientSubtle,
        
        /// <summary>
        /// A 10-second constant, low vibration, suitable for an ongoing event.<br/> <br/>
        /// Similar to Vibrate(0.3f, 0.1f, 10f).
        /// </summary>
        Ambient,
        
        /// <summary>
        /// A 10-second constant, low and strong vibration, suitable for an ongoing event.<br/> <br/>
        /// Similar to Vibrate(0.6f, 0.3f, 10f).
        /// </summary>
        AmbientStrong
    }
}
