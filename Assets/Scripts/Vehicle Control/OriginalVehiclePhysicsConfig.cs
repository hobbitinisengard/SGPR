using System;

namespace RVP
{
	/// <summary>
	/// Vehicle parameters read from the original Stunt GP config files. Values and
	/// curve samples are kept in their source units; the runtime adapter performs
	/// the conversion to Unity units.
	/// </summary>
	[Serializable]
	public sealed class OriginalVehiclePhysicsConfig
	{
		public int formatVersion;
		public string sourceConfig;
		public float mass, comA, comB, comHeight, wheelbase, trackFront, trackRear;
		public float length, width, height, rideHeight;
		public float rpmIdle, rpmLimit, rpmMax, engineDecay, maxTorque;
		public float[][] torqueCurves;
		public int driveMode, gearCount;
		public float finalDrive, shiftTime, efficiency, powerSplit;
		public float[] ratios;
		public float brakeBias, brakeAcceleration;
		public float[] frictionCurve;
		public OriginalTyrePhysicsConfig[] tyres;
		public float dragCoefficient, liftCoefficient, frontalArea;
		public float steeringMax, steeringSensitivity, steeringAcceleration;
		public float[] steeringCurve, digitalSteering, digitalBrake, analogSteering, analogBrake, velocitySteering;
		public float fuelCapacity, fuelUnitMass, fuelConsumption, refuelRate;
		public float[] consumptionCurve;
		public float turboAcceleration, turboMax, turboDecay, turboScale, turboConsumption, turboEnergyThreshold;
		public float[] turboCurve;
		public float launchTime, launchTolerance, launchSpeed, maxPitchSpeed, maxYawSpeed;
	}

	[Serializable]
	public sealed class OriginalTyrePhysicsConfig
	{
		public float radius, pressure, staticFriction, kineticFriction;
		public float travelIn, dampingIn, stiffnessIn, travelOut, dampingOut, stiffnessOut;
	}
}
