using UnityEngine;

namespace HinputClasses.Internal {
	// Hinput class measuring a stick axis, and feeding it to a Stick.
    public class Axis {
     	// --------------------
     	// CONSTRUCTORS
     	// --------------------
 
     	// D-pad constructor
     	public Axis (string axisFullName, string positiveButtonFullName, string negativeButtonFullName) {
     		this.axisFullName = axisFullName;
     		this.positiveButtonFullName = positiveButtonFullName;
     		this.negativeButtonFullName = negativeButtonFullName;

            if (Utils.os == Utils.OS.Windows) AxisType = AT.Axis;
            if (Utils.os == Utils.OS.Mac) AxisType = AT.Buttons;
        }
 
     	// left/right stick constructor
     	public Axis (string axisFullName) {
     		this.axisFullName = axisFullName;
     		positiveButtonFullName = "";
     		negativeButtonFullName = "";
            
            AxisType = AT.Axis;
        }
 
     	
        // --------------------
        // PRIVATE PROPERTIES
        // --------------------
 
        private readonly string axisFullName;
        private readonly string positiveButtonFullName;
        private readonly string negativeButtonFullName;
        
        private enum AT { None, Axis, Buttons }
        private AT AxisType = AT.None;
        
        
        // --------------------
        // POSITION
        // --------------------

        public float GetPosition() {
	        // The D-pad will be recorded as two axes or four buttons, depending on the gamepad driver used.
	        // Measure both the axes and the buttons, and ignore the one that returns an error.
	        // In the future, always call the one that worked.
	        if (AxisType == AT.Axis) return GetAxisValue();
	        if (AxisType == AT.Buttons) return GetButtonValue();

	        float value = GetAxisValue();
	        if (value.IsNotEqualTo(0))AxisType = AT.Axis;
	        else {
		        value = GetButtonValue();
		        if (value.IsNotEqualTo(0)) AxisType = AT.Buttons;
	        }

	        return value;
        }

        private float GetAxisValue() {
	        return Utils.GetAxis(axisFullName, false);
        }

        private float GetButtonValue() {
	        float buttonValue = 0f;
	        if (Utils.GetButton(positiveButtonFullName)) buttonValue += 1;
	        if (Utils.GetButton(negativeButtonFullName)) buttonValue -= 1;
	        return buttonValue;
        }
    }
}