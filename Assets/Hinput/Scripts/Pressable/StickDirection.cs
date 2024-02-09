using HinputClasses.Internal;

namespace HinputClasses {
    /// <summary>
	/// Hinput class representing a given direction of a stick or D-pad, such as the up or down-left directions.
	/// </summary>
	public class StickDirection : StickPressable {
		// --------------------
		// PRIVATE PROPERTY
		// --------------------

		private float angle { get; }


		// --------------------
		// CONSTRUCTOR
		// --------------------

		public StickDirection (string pressableName, float angle, Stick stick) : 
			base(pressableName, stick) {
			this.angle = angle;
		}

		
		// --------------------
		// UPDATE
		// --------------------

		protected override bool GetPressed() {
			return (stick.PushedTowards(angle) && stick.distance > Settings.stickPressedZone);
		}

	}
}