using PathCreation;
using UnityEngine;
using Unity.Profiling;
namespace RVP
{
	internal static class OriginalContactPhysics
	{
		// ColRestitutionTab from the original level configs. All 25 supplied level
		// configs (default plus tracks 1..24) contain this same 128-sample table.
		static readonly float[] RestitutionCurve =
		{
			0.000000f, 0.000000f, 0.000000f, 0.000000f, 0.000000f, 0.000000f, 0.000000f, 0.000000f,
			0.000000f, 0.000567f, 0.001135f, 0.001702f, 0.002270f, 0.002837f, 0.003405f, 0.003972f,
			0.004539f, 0.007853f, 0.011166f, 0.014479f, 0.017793f, 0.021106f, 0.024419f, 0.027733f,
			0.031046f, 0.044842f, 0.058639f, 0.072435f, 0.086232f, 0.100029f, 0.113825f, 0.127622f,
			0.141418f, 0.168396f, 0.195375f, 0.222353f, 0.249331f, 0.276310f, 0.303288f, 0.330266f,
			0.357244f, 0.386774f, 0.416303f, 0.445833f, 0.475362f, 0.498690f, 0.522018f, 0.545346f,
			0.568673f, 0.587365f, 0.606056f, 0.624748f, 0.643439f, 0.662131f, 0.680822f, 0.699514f,
			0.718205f, 0.733274f, 0.748342f, 0.763410f, 0.778478f, 0.793546f, 0.808615f, 0.823683f,
			0.838751f, 0.850874f, 0.862996f, 0.875119f, 0.887242f, 0.894770f, 0.902297f, 0.909825f,
			0.917353f, 0.924881f, 0.932408f, 0.939936f, 0.947464f, 0.951480f, 0.955497f, 0.959514f,
			0.963531f, 0.967547f, 0.971564f, 0.975581f, 0.979597f, 0.984698f, 0.989799f, 0.994899f,
			1.000000f, 1.000000f, 1.000000f, 1.000000f, 1.000000f, 1.000000f, 1.000000f, 1.000000f,
			1.000000f, 1.000000f, 1.000000f, 1.000000f, 1.000000f, 1.000000f, 1.000000f, 1.000000f,
			1.000000f, 1.000000f, 1.000000f, 1.000000f, 1.000000f, 1.000000f, 1.000000f, 1.000000f,
			1.000000f, 1.000000f, 1.000000f, 1.000000f, 1.000000f, 1.000000f, 1.000000f, 1.000000f
		};
		public static float EffectiveRestitution(float sourceNormalSpeed, Vector3 normal, Vector3 radial,
			float sourceGravity)
		{
			float restitution = MotionRestitution(sourceNormalSpeed, sourceGravity, radial);
			if (radial.sqrMagnitude <= 0.00000000000001f)
				return restitution;
			return (1 - Vector3.Dot(normal.normalized, radial.normalized)) * restitution;
		}
		public static float MotionRestitution(float sourceNormalSpeed, float sourceGravity, Vector3 radial)
		{
			float restitution = BaseRestitution(sourceNormalSpeed, sourceGravity);
			if (radial.sqrMagnitude <= 0.00000000000001f)
				return restitution;
			float index = Mathf.Abs(sourceNormalSpeed) * 128 / Mathf.Max(0.001f, sourceGravity * 4);
			int curveIndex = (int)index;
			float curve = curveIndex < RestitutionCurve.Length ? RestitutionCurve[curveIndex] : 1;
			return restitution * curve;
		}
		public static float BaseRestitution(float sourceNormalSpeed, float sourceGravity)
		{
			float index = Mathf.Abs(sourceNormalSpeed) * 128 / Mathf.Max(0.001f, sourceGravity * 4);
			int curveIndex = (int)index;
			float curve = curveIndex < RestitutionCurve.Length ? RestitutionCurve[curveIndex] : 1;
			return curve * 0.05f;
		}
	}
	/// <summary>
	/// Recovered Stunt GP vehicle model from vehicle.cpp and vehicle_stunt.cpp.
	/// It uses Stunt GP units and a 60 Hz source tick; Unity colliders are queried
	/// as track geometry while this component applies the source contact response,
	/// drivetrain, suspension, steering, aerodynamic and airborne behavior.
	/// </summary>
	public sealed class OriginalVehiclePhysics
	{
		static readonly ProfilerMarker SourceCollisionExclusionMarker =
			new ProfilerMarker("RVP.OriginalPhysics.CollisionExclusions");
		static readonly ProfilerMarker SourceVehiclePairMarker =
			new ProfilerMarker("RVP.OriginalPhysics.VehiclePairs");
		static readonly ProfilerMarker SourceWheelContactMarker =
			new ProfilerMarker("RVP.OriginalPhysics.WheelContacts");
		static readonly ProfilerMarker SourceTrackMeshSweepMarker =
			new ProfilerMarker("RVP.OriginalPhysics.TrackMeshSweep");
		static readonly ProfilerMarker SourcePowertrainMarker =
			new ProfilerMarker("RVP.OriginalPhysics.Powertrain");
		static readonly ProfilerMarker SourceTyreMarker =
			new ProfilerMarker("RVP.OriginalPhysics.Tyres");
		static readonly ProfilerMarker SourceSuspensionMarker =
			new ProfilerMarker("RVP.OriginalPhysics.Suspension");
		static readonly ProfilerMarker SourceAirMotionMarker =
			new ProfilerMarker("RVP.OriginalPhysics.AirMotion");
		const float SourceSpeedToMetresPerSecond = 0.6f;
		const float SourceMassToKilograms = 0.001f;
		const float SourceLengthToMetres = 0.01f;
		const float SourceTicksPerSecond = 60f;
		const float SourceEnergyCapacity = 1000f;
		const float SourceHumanRespawnEnergyCost = 5f;
		const float SourceCpuRespawnEnergyCost = 3.5f;
		const float SourceHumanStuntEnergyAwardPercent = 9f;
		const float SourceCpuStuntEnergyAwardPercent = 7f; // The retail award truncates Gameplay.csv 7.5 before applying it.
		const float SourceStuntEnergyAwardMultiplier = 1.5f;
		// The source adapter disables Rigidbody.useGravity and integrates gravity
		// itself. Derive the source-unit values from Unity's project setting so a
		// configured gravity (for example -30 m/s^2) affects these cars too.
		static float SourceGravityMagnitude => Physics.gravity.magnitude /
			(SourceSpeedToMetresPerSecond * SourceTicksPerSecond);
		static Vector3 SourceGravityDirection => Physics.gravity.sqrMagnitude > 0.000001f
			? Physics.gravity.normalized : Vector3.down;
		static Vector3 SourceWorldUp => -SourceGravityDirection;
		static Vector3 SourceGravityAcceleration => Physics.gravity;
		const float SourcePitlaneTargetSpeed = 41.666668f; // 90 km/h scaled by the original gameplay rule.
		const float SourcePitlaneRefuelLimit = 1000f; // gameplay.csv value 100 scaled by 10.
		const float SourceUnityRailActivationDistance = 3.5f;
		// Source contact profile defaults used when a Unity surface has no authored
		// values. GroundSurfaceMaster supplies the actual per-surface values.
		const float DefaultSourceTrackGrip = 64f / 64f;
		const float DefaultSourceTrackRolling = 1f;
		const float DefaultSourceTrackRoughness = 0f;
		const float SourceAccelerationFactor = 100f * 0.00027777778f * 3f;
		// 4081D0 uses a 1000-unit dead zone, the gameplay.csv CPU/human catch-up
		// distances scaled to source hundredths of metres, and a 25% setup gain.
		const float SourcePerformanceDeadZoneMetres = 10f;
		const float SourceCpuPerformanceDistanceMetres = 250f;
		const float SourceHumanPerformanceDistanceMetres = 300f;
		const float SourcePerformanceGain = 0.25f;
		const float SourceTorqueMap1SkillThreshold = 40f;
		const float SourceTorqueMap2SkillThreshold = 65f;
		const float SourceTorqueMap3SkillThreshold = 80f;
		const float SourceCpuLeaderSkillPenaltyDistanceMetres = 250f;
		const float SourceCpuLeaderSkillPenalty = 0.35f;
		const float SourceCpuCatchupDistanceMetres = 100f;
		const float SourceCpuCatchupSkillGain = 0.20f;
		// gameplay.csv rules scaled to the original runtime values.
		const float SourceCpuCollisionEnergyModifier = 1f;
		const float SourceCpuEngineConsumptionModifier = 1.10f;
		const float SourceCpuTurboConsumptionModifier = 1.15f;
		const float SourceCpuBoostUsageModifier = 0.95f;
		const float SourceCpuAerodynamicDragModifier = 1.15f;
		const float SourceCollisionEnergyMinimum = 0.25f;
		const float SourceCollisionEnergyMaximum = 0.5f;
		const float SourceCollisionEnergyScale = 0.01f;
		const float SourceCollisionEnergyLimit = 0.75f;
		// VehicleParent wheel order: front left, front right, rear left, rear right.
		static readonly int[] UnityWheelIndex = { 2, 3, 0, 1 };
		static readonly float SourceStuntPhaseBoundary = System.BitConverter.Int32BitsToSingle(0x3f3504f4);
		static readonly System.Collections.Generic.HashSet<int> SourceTrackColliderIds = new();
		static readonly System.Collections.Generic.HashSet<int> SourceVehicleColliderIds = new();
		static readonly System.Collections.Generic.HashSet<int> SourceInvisibleLevelColliderIds = new();
		static bool invisibleLevelLayerInitialized;
		static int invisibleLevelLayer = -1;
		static readonly System.Collections.Generic.Dictionary<VehicleParent, Vector3> SourceVehiclePairPositions = new();
		static Collider[] sourceTrackCollisionTargets = System.Array.Empty<Collider>();
		static Collider[] sourceVehicleCollisionTargets = System.Array.Empty<Collider>();
		static Collider[] sourceInvisibleLevelCollisionTargets = System.Array.Empty<Collider>();
		static EnergyTunnelPath[] sourceUnityRailPaths = System.Array.Empty<EnergyTunnelPath>();
		static float nextSourceUnityRailPathScan;
		static float nextSourceTrackCollisionScan;
		static int sourceTrackCollisionGeneration;
		static int sourceTrackCollisionMask = int.MinValue;
		static int sourceVehiclePairSnapshotTick = -1;
		static int sourceWakeResetTick = -1;
		static uint sourcePhysicsRandomState;
		static bool sourcePhysicsRandomInitialized;
		readonly VehicleParent vehicle;
		readonly OriginalVehiclePhysicsConfig parameters;
		readonly Transform bodyTransform;
		Vector3 initialBodyLocalPosition;
		readonly Quaternion initialBodyLocalRotation;
		readonly float[] wheelSpeed = new float[4];
		readonly float[] wheelAcceleration = new float[4];
		readonly float[] sourceStaticFriction = new float[4];
		readonly float[] sourceKineticFriction = new float[4];
		readonly float[] filteredContactLoad = new float[4];
		readonly float[] sourceSuspension = new float[4];
		readonly float[] contactCountdown = new float[4];
		readonly float[] contactSpringTravel = new float[4];
		readonly float[] sourceWheelSlip = new float[4];
		readonly float[] sourceWheelSideSlip = new float[4];
		readonly float[] sourceWheelGripUsage = new float[4];
		readonly float[] sourceTireParticleAccumulator = new float[4];
		readonly int[] sourceTireParticleEvents = new int[4];
		readonly float[] sourceWheelSurfaceGrip = { DefaultSourceTrackGrip, DefaultSourceTrackGrip, DefaultSourceTrackGrip, DefaultSourceTrackGrip };
		readonly float[] sourceWheelSurfaceRolling = { DefaultSourceTrackRolling, DefaultSourceTrackRolling, DefaultSourceTrackRolling, DefaultSourceTrackRolling };
		readonly float[] sourceWheelSurfaceRoughness = { DefaultSourceTrackRoughness, DefaultSourceTrackRoughness, DefaultSourceTrackRoughness, DefaultSourceTrackRoughness };
		readonly float[] sourceLateralSpeed = new float[4];
		readonly float[] sourceLongitudinalSpeed = new float[4];
		readonly float[] sourceAxleForce = new float[2];
		readonly int[] controlFlags = new int[4];
		readonly Vector3[] sourceWheelLocal = new Vector3[4];
		readonly Vector3[] sourceProbeLocal = new Vector3[6];
		readonly Vector3[] sourceConfiguredProbeLocal = new Vector3[6];
		readonly float[] sourceProbeRadius = new float[6];
		readonly Vector3[] previousSourceProbeWorld = new Vector3[6];
		readonly int[] sourceProbeContactIterations = new int[6];
		readonly int[] sourceProbeHitsThisTick = new int[6];
		readonly Vector3[] sourceProbeFirstContactNormal = new Vector3[6];
		readonly float[] sourceProbeFirstContactSpeed = new float[6];
		readonly Vector3[] sourceProbeFirstContactPoint = new Vector3[6];
		readonly Vector3[] sourceProbeFirstContactCenter = new Vector3[6];
		readonly Vector3[] sourceWheelRotationDeltaThisTick = new Vector3[4];
		Vector3 sourceAccelerationTiltDeltaThisTick;
		Vector3 sourceRightBasis;
		Vector3 sourceForwardBasis;
		Vector3 sourceUpBasis;
		readonly bool[] hasPreviousSourceProbe = new bool[6];
		readonly float[] sourceProbeWheelLoad = new float[4];
		readonly bool[] sourceProbeWheelTouched = new bool[4];
		readonly Vector3[] sourceWakePositions = new Vector3[8];
		readonly float[] sourceWakeRadii = new float[8];
		readonly Vector3[] sourceWheelContactNormal = new Vector3[4];
		readonly bool[] sourceWheelGroundSurfaceContactThisTick = new bool[4];
		readonly bool[] hasSourceWheelContactNormal = new bool[4];
		readonly Vector3[] sourceWheelContactPoint = new Vector3[4];
		readonly Collider[] sourceWheelContactCollider = new Collider[4];
		readonly int[] sourceWheelSurfaceType = new int[4];
		readonly bool[] hasSourceWheelSurfaceContact = new bool[4];
		readonly RaycastHit[] sourceProbeCastHits = new RaycastHit[64];
		readonly Collider[] sourceProbeOverlapHits = new Collider[64];
		Collider[] appliedSourceTrackCollisionTargets = System.Array.Empty<Collider>();
		Collider[] appliedSourceVehicleCollisionTargets = System.Array.Empty<Collider>();
		Collider[] appliedSourceInvisibleLevelCollisionTargets = System.Array.Empty<Collider>();
		int sourceProbeCount = 4;
		float rpm;
		float torque;
		float clutch = 1f;
		float turboRpm;
		float turboForce;
		float sourceEffectiveMass = 1;
		float fuel;
		float energy;
		float steeringBoost;
		float steeringDegrees;
		float sourceSteering;
		float currentSourceSpeed;
		float throttleSignal;
		float pitchAcceleration;
		float yawAcceleration;
		float rollAcceleration;
		float pitchSpeed;
		float yawSpeed;
		float rollSpeed;
		float stuntRollProgress;
		int stuntRollDirection;
		bool stuntRollActive;
		bool stuntRollInputArmed = true;
		float sourceBodyRoll;
		float sourceBodyPitch;
		float sourceBodyHeave;
		Vector3 sourceAngularVelocity;
		float effectiveComHeight;
		float sourceCom2628;
		float sourceCom262c;
		Vector3 sourceContactAngularStep;
		Quaternion sourceIncrementalRotation = Quaternion.identity;
		Matrix4x4 sourceContactAngularResponse = Matrix4x4.identity;
		Quaternion stepStartRotation;
		Quaternion stepRotation;
		Quaternion lastSourceControlledRotation;
		bool hasSourceControlledRotation;
		bool stepRotationChanged;
		bool sourceProbeIterationOverflow;
		bool sourceContactVelocityRespawnRequested;
		// Temporary diagnostic switch: suppress all resets requested by this physics solver.
		static readonly bool SourceRequestedResetsEnabled = false;
		bool sourceContactResetPending;
		bool sourceContactResetDiagnosticLogged;
		string sourceContactResetReason;
		Vector3 sourceContactVelocityBeforeThisTick;
		Vector3 sourceContactVelocityDeltaThisTick;
		Vector3 sourceContactVelocityAfterThisTick;
		bool hasGroundSurfaceContactThisTick;
		Vector3 groundSurfaceContactNormalSumThisTick;
		bool sourceRotationDiagnosticLogged;
		int sourceStabilityDiagnosticSamples;
		float nextSourceStabilityDiagnosticTime;
		bool sourceSuspensionDiagnosticLogged;
		bool sourceSuspensionObservationLogged;
		int sourceSuspensionStableContactTicks;
		float sourceFirstSuspensionContactTime = -1;
		readonly float[] sourceWheelProbeVerticalFitOffset = new float[4];
		readonly float[] sourceWheelVisualVerticalOffset = new float[4];
		float sourceBodyProbeVerticalFitOffset;
		float sourceContactCounter;
		int sourceUnsafeContactTicks;
		int sourceOpposingDirectionTicks;
		int sourceLastUnsafeProbe = -1;
		int sourceLastUnsafeFlags = -1;
		float sourceLastUnsafeAlignment;
		Vector3 sourceLastUnsafePoint;
		Vector3 sourceLastUnsafeNormal;
		string sourceLastUnsafeCause;
		string sourceLastUnsafeCollider;
		int sourceLastUnsafeLayer = -1;
		float sourceWakeTicks;
		float sourceAirFactor = 1;
		Quaternion pendingContactRotation = Quaternion.identity;
		bool hasPendingContactRotation;
		int lastVehiclePairImpulseTick = -1;
		float nextVehicleCollisionHopTime;
		int lastSourceCollisionHoldTick = -1;
		float sourceCollisionHoldTicks;
		float sourceImpactEnergyLossThisTick;
		float stuntPressedAt = -1;
		bool stuntPressed;
		bool stuntActive;
		bool sourceTurboActive;
		bool sourceStartBoostActiveThisTick;
		bool sourceSpecialMode;
		bool sourceRailControlsActive;
		RailbarLogic sourceRailbar;
		float sourceRailbarSeenTime = float.NegativeInfinity;
		float sourceRailbarSupportedTime = float.NegativeInfinity;
		public bool IsRailbarGrinding => sourceRailbar && sourceRailbar.isActiveAndEnabled &&
			Time.fixedTime - sourceRailbarSupportedTime <= Time.fixedDeltaTime * 1.5f;
		bool sourceRailControlDisabled;
		EnergyTunnelPath sourceUnityRailPathThisTick;
		EnergyTunnelPath sourceRailCompletedPath;
		bool sourceMassOverride;
		uint sourceRailProgress;
		float sourceRailTicks;
		float sourceRailEnergyAdded;
		float sourceRailThrottle;
		byte sourceRailBrakeInput;
		// source_cold_vehicle_state initializes gear to 1 (neutral). Gear 2 is
		// first gear in the retail state encoding.
		int gear = 1;
		float startBoostTicks;
		float performanceMultiplier = 1;
		float sourceUpgradeCondition;
		float airTicks;
		float steeringRamp;
		float brakeRamp;
		float downshiftTicks;
		int comResetTicks;
		int sourceContactClass;
		int sourceSuspensionTick;
		float sourceSuspensionTickPhase;
		int sourceIdleHold;
		int sourceIdleInterval;
		int appliedSourceTrackCollisionGeneration = -1;
		int sourceAiStuntMeter;
		float sourceAiStuntDeadline;
		bool sourceAiStuntAirborne;
		int sourceAiStuntInputKind;
		int sourceAiTurboTicks;
		int sourceAiTurboCooldown;
		int sourceAiTurboRestTicks;
		bool sourceAiTurboActive;
		uint sourceAiLaunchTick;
		bool sourceAiLaunchPending;
		bool sourceAiStuntInputThisTick;
		readonly int[] sourceStuntPhaseIndex = new int[2];
		readonly int[] sourceStuntLastPhase = { -1, -1 };
		bool DigitalControls => vehicle.basicInput && vehicle.basicInput.playerInput &&
			vehicle.basicInput.playerInput.currentControlScheme == "Keyboard" && !vehicle.followAI.selfDriving;
		bool SourcePlayerControlsSuppressed => sourceRailControlsActive && sourceRailControlDisabled;
		bool SourceAutomaticShiftAllowed =>
			(F.I && F.I.s_raceType == RaceType.TimeTrial) ||
			CountDownSeq.Countdown <= 0;
		float SourceRawAccelInput =>
			sourceAiStuntInputThisTick && ((sourceAiStuntInputKind == 0 && gear != 0) ||
				(sourceAiStuntInputKind == 1 && gear == 0))
				? Mathf.Max(vehicle.accelInput, 1) : vehicle.accelInput;
		float SourceRawBrakeInput =>
			sourceAiStuntInputThisTick && ((sourceAiStuntInputKind == 0 && gear == 0) ||
				(sourceAiStuntInputKind == 1 && gear != 0))
				? Mathf.Max(vehicle.brakeInput, 1) : vehicle.brakeInput;
		float SourceSteerInput => sourceAiStuntInputThisTick
			? (sourceAiStuntInputKind == 2 ? -1 : sourceAiStuntInputKind == 3 ? 1 : 0)
			: vehicle.steerInput;
		float SourceAiAirThrottleInput => sourceAiStuntInputThisTick
			? (sourceAiStuntInputKind == 0 ? 1 : 0) : vehicle.accelInput;
		float SourceAiAirBrakeInput => sourceAiStuntInputThisTick
			? (sourceAiStuntInputKind == 1 ? 1 : 0) : vehicle.brakeInput;
		float SourceAiAirRollInput => sourceAiStuntInputThisTick
			? (sourceAiStuntInputKind == 4 ? -1 : sourceAiStuntInputKind == 5 ? 1 : 0)
			: vehicle.rollInput;
		float SourceCpuSkill => !F.I ? 85f : F.I.s_cpuLevel switch
		{
			CpuLevel.Easy => 70f,
			CpuLevel.Medium => 85f,
			CpuLevel.Hard => 100f,
			_ => 85f,
		};
		bool SourceUpgradeActive => vehicle.followAI && vehicle.followAI.IsCPU;
		float SourceUpgradeCondition => SourceUpgradeActive ? sourceUpgradeCondition : 0f;
		int SourceTorqueCurveIndex
		{
			get
			{
				// Time-trial startup explicitly selects torque map 3 for its player
				// (retail_time_trial.cpp::start_time_trial). Other player modes retain
				// the torque map assembled from the selected car setup.
				if (!vehicle.followAI || !vehicle.followAI.IsCPU)
					return F.I && F.I.s_raceType == RaceType.TimeTrial ? 3 :
						Mathf.Clamp(parameters.torqueCurveIndex, 0, parameters.torqueCurves.Length - 1);
				// CPU maps come from Gameplay.csv's 40/65/80 skill thresholds. The
				// current CPU difficulty setting supplies the corresponding skill value.
				float skill = SourceCpuSkill;
				return skill > SourceTorqueMap3SkillThreshold ? 3
					: skill > SourceTorqueMap2SkillThreshold ? 2
					: skill > SourceTorqueMap1SkillThreshold ? 1 : 0;
			}
		}
		public static bool IsUsable(OriginalVehiclePhysicsConfig p)
		{
			if (p == null || p.formatVersion != 1 || p.mass <= 0 || p.rpmLimit <= 0 ||
				p.gearCount < 2 ||
				p.tyres == null || p.tyres.Length != 4 || p.ratios == null || p.ratios.Length != 8 ||
				p.torqueCurves == null || p.torqueCurves.Length != 4)
				return false;
			if (p.gearCount >= p.ratios.Length)
				return false;
			for (int i = 0; i < 4; i++)
				if (p.tyres[i] == null || p.tyres[i].radius <= 0 || p.torqueCurves[i] == null || p.torqueCurves[i].Length != 128)
					return false;
		return p.frictionCurve?.Length == 128 && p.steeringCurve?.Length == 128 &&
			p.digitalSteering?.Length == 128 && p.digitalBrake?.Length == 128 &&
				p.analogSteering?.Length == 128 && p.velocitySteering?.Length == 128 &&
				p.analogBrake?.Length == 128 && p.consumptionCurve?.Length == 128 &&
				p.turboCurve?.Length == 128;
		}
		public OriginalVehiclePhysics(VehicleParent owner, OriginalVehiclePhysicsConfig config)
		{
			InitializeSourcePhysicsRandom();
			vehicle = owner;
			parameters = config;
			AlignInitialPoseToTrackSurface();
			lastSourceControlledRotation = owner.rb.rotation;
			InitializeSourceWheelLayout();
			bodyTransform = owner.bodyObj ? owner.bodyObj.transform : null;
			initialBodyLocalPosition = bodyTransform ? bodyTransform.localPosition : Vector3.zero;
			initialBodyLocalRotation = bodyTransform ? bodyTransform.localRotation : Quaternion.identity;
			rpm = parameters.rpmIdle;
			torque = parameters.maxTorque * parameters.torqueCurves[0][0];
			fuel = parameters.fuelCapacity;
			energy = parameters.fuelCapacity;
			sourceEffectiveMass = (float)((double)fuel * parameters.fuelUnitMass + parameters.mass);
			effectiveComHeight = parameters.comHeight;
			float sourceTicks = Time.fixedTime * SourceTicksPerSecond;
			sourceSuspensionTick = Mathf.FloorToInt(sourceTicks);
			sourceSuspensionTickPhase = sourceTicks - sourceSuspensionTick;
			ApplySourceDimensions();
			ResetSourceProbeHistory();
			FitSourceProbeHeightsToPrefabTires();
			// The fit changes the probe offsets; reset history so the next sweep
			// starts from the fitted locations.
			ResetSourceProbeHistory();
			ResetSourceWakeHistory();
			nextSourceTrackCollisionScan = 0;
			if (vehicle.engine && vehicle.engine.transmission)
				vehicle.engine.transmission.SetOriginalGear(gear, parameters);
		}
		void AlignInitialPoseToTrackSurface()
		{
			if (!RaceManager.I || !vehicle.rb)
				return;
			Vector3 worldUp = SourceWorldUp;
			Vector3 rayOrigin = vehicle.rb.position + worldUp * 5;
			RaycastHit[] hits = Physics.RaycastAll(rayOrigin, -worldUp, 20,
				RaceManager.I.wheelCastMask, QueryTriggerInteraction.Ignore);
			int closestTrackHit = -1;
			float closestDistance = float.PositiveInfinity;
			for (int i = 0; i < hits.Length; i++)
				if (hits[i].distance < closestDistance && IsSourceTrackCollider(hits[i].collider))
				{
					closestTrackHit = i;
					closestDistance = hits[i].distance;
				}
			if (closestTrackHit < 0)
				return;
			Vector3 surfaceNormal = hits[closestTrackHit].normal.normalized;
			if (Vector3.Dot(surfaceNormal, worldUp) < 0)
				surfaceNormal = -surfaceNormal;
			Vector3 forward = Vector3.ProjectOnPlane(vehicle.rb.rotation * Vector3.forward, surfaceNormal);
			if (forward.sqrMagnitude < 0.0001f)
				return;
			Quaternion initialRotation = vehicle.rb.rotation;
			Quaternion alignedRotation = Quaternion.LookRotation(forward.normalized, surfaceNormal);
			float correctionDegrees = Quaternion.Angle(initialRotation, alignedRotation);
			if (correctionDegrees <= 0.25f)
				return;
			vehicle.rb.rotation = alignedRotation;
			//Debug.LogWarning($"[OriginalVehiclePhysics] Initial pose aligned to Unity track surface: " +
				//$"car={vehicle.carConfig?.name}, source={parameters.sourceConfig}, " +
				//$"correction={correctionDegrees:F2} deg, normal={surfaceNormal.ToString("F2")}, " +
				//$"rotation={initialRotation.eulerAngles.ToString("F1")} -> " +
				//$"{alignedRotation.eulerAngles.ToString("F1")}", vehicle);
		}
		public float Energy => energy;
		public float EnergyCapacity => Mathf.Max(0, parameters.fuelCapacity);
		public float EnergyPercent => EnergyCapacity > 0 ? energy / EnergyCapacity : 0;
		public float EnergyThreshold => Mathf.Clamp01(parameters.turboEnergyThreshold);
		public float SourceRefuelRate => Mathf.Max(0, parameters.refuelRate);
		public float EngineRpmLimit => Mathf.Max(1, parameters.rpmLimit);
		public float SteeringLimitDegrees => Mathf.Abs(parameters.steeringMax);
		public int CurrentGear => gear;
		public bool TurboActive => sourceTurboActive;
		public void RefreshParameters()
		{
			fuel = Mathf.Clamp(fuel, 0, Mathf.Max(0, parameters.fuelCapacity));
			energy = Mathf.Clamp(energy, 0, Mathf.Max(0, parameters.fuelCapacity));
			sourceEffectiveMass = (float)((double)fuel * parameters.fuelUnitMass + parameters.mass);
			effectiveComHeight = parameters.comHeight;
			bool preserveAngularVelocity = !vehicle.rb.isKinematic;
			Vector3 angularVelocity = preserveAngularVelocity
				? vehicle.rb.angularVelocity : Vector3.zero;
			InitializeSourceWheelLayout();
			ApplySourceDimensions();
			if (preserveAngularVelocity && !vehicle.rb.isKinematic)
				vehicle.rb.angularVelocity = angularVelocity;
			ResetSourceProbeHistory();
			FitSourceProbeHeightsToPrefabTires();
			ResetSourceProbeHistory();
			if (vehicle.engine && vehicle.engine.transmission)
				vehicle.engine.transmission.SetOriginalGear(gear, parameters);
		}
		void InitializeSourceWheelLayout()
		{
			string sourceName = System.IO.Path.GetFileNameWithoutExtension(parameters.sourceConfig ?? string.Empty);
			int selector = 0;
			if (sourceName.StartsWith("car") && sourceName.Length > 3)
				int.TryParse(sourceName.Substring(3), out selector);
			float sourceCenterY = 0;
			if (selector == 6)
			{
				sourceProbeCount = 6;
				sourceCenterY = -20; // The two extra probes are both at y=60.
			}
			else if (selector == 17)
			{
				sourceProbeCount = 6;
				sourceCenterY = -30; // The two extra probes are at y=50 and y=130.
			}
			else if (selector == 1 || selector == 3 || selector == 4 || selector == 5 ||
				selector == 8 || selector == 9 || selector == 10 || selector == 12 ||
				selector == 14 || selector == 15 || selector == 16 || selector == 18 ||
				selector == 19 || selector == 20)
			{
				sourceProbeCount = 5;
				sourceCenterY = -6; // The fifth probe is at y=30 and all five are centered.
			}
			else if (selector == 2 || selector == 7 || selector == 11 || selector == 13)
			{
				sourceProbeCount = 5;
				sourceCenterY = -10; // The fifth probe is at y=50 and all five are centered.
			}
			for (int i = 0; i < sourceWheelLocal.Length; i++)
			{
				Vector3 local = new Vector3(i % 2 == 0 ? -60 : 60, sourceCenterY,
					i < 2 ? -110 : 110);
				sourceWheelLocal[i] = local;
				sourceProbeLocal[i] = local;
				sourceProbeRadius[i] = 70;
			}
			if (sourceProbeCount == 5)
			{
				bool shorterFifthProbe = selector == 2 || selector == 7 || selector == 11 || selector == 13;
				sourceProbeLocal[4] = new Vector3(0, (shorterFifthProbe ? 50 : 30) + sourceCenterY, 0);
				sourceProbeRadius[4] = shorterFifthProbe ? 80 : 60;
			}
			else if (selector == 6)
			{
				sourceProbeLocal[4] = new Vector3(0, 60 + sourceCenterY, 70);
				sourceProbeLocal[5] = new Vector3(0, 60 + sourceCenterY, -70);
				sourceProbeRadius[4] = sourceProbeRadius[5] = 110;
			}
			else if (selector == 17)
			{
				sourceProbeLocal[4] = new Vector3(0, 50 + sourceCenterY, 0);
				sourceProbeLocal[5] = new Vector3(0, 130 + sourceCenterY, 0);
				sourceProbeRadius[4] = sourceProbeRadius[5] = 80;
			}
			for (int i = 0; i < sourceProbeCount; i++)
				sourceConfiguredProbeLocal[i] = sourceProbeLocal[i];
		}
		void UpdateSourceWheelProbeGeometry()
		{
			if (vehicle.wheels == null || vehicle.wheels.Length < 4 || !vehicle.tr)
				return;
			Vector3 wheelProbeOffsetSum = Vector3.zero;
			int mappedWheelCount = 0;
			for (int sourceIndex = 0; sourceIndex < 4; sourceIndex++)
			{
				Wheel wheel = vehicle.wheels[UnityWheelIndex[sourceIndex]];
				if (!wheel || !wheel.connected || !wheel.rim || !wheel.susParent)
					continue;
				// Keep each contact probe fixed to the suspension mount. Following the
				// rendered rim makes the probe sweep along with suspension travel, which
				// feeds wheel animation back into contact velocity and causes bounce.
				// The source car-local probe constants can be offset from Unity prefab
				// mounts, so use the authored suspension anchor instead.
				Vector3 sourceLocal = vehicle.tr.InverseTransformPoint(wheel.susParent.tr.position) /
					SourceLengthToMetres;
				wheelProbeOffsetSum += sourceLocal - sourceConfiguredProbeLocal[sourceIndex];
				mappedWheelCount++;
				sourceWheelLocal[sourceIndex] = sourceLocal;
				sourceProbeLocal[sourceIndex] = sourceLocal;
				// initialize_rig in the original game gives each of the four wheel
				// contact probes a fixed 70 cm radius, independent of rendered tires.
			}
			if (mappedWheelCount == 4)
			{
				// The retail rig centers its probes around the body origin. Prefab roots
				// are not always centered between their axles (Formula 17's suspension
				// anchors average 18.7 cm behind its root), so remove the anchors' shared
				// horizontal offset while preserving the actual wheelbase and track.
				// It also keeps all four wheel probes on one local-Y plane; prefab mounts
				// can have different Y values to compensate for different tire radii.
				float commonWheelProbeY = 0.25f * (sourceWheelLocal[0].y + sourceWheelLocal[1].y +
					sourceWheelLocal[2].y + sourceWheelLocal[3].y);
				Vector3 wheelProbeOffset = wheelProbeOffsetSum / mappedWheelCount;
				for (int i = 0; i < 4; i++)
				{
					sourceWheelLocal[i].x -= wheelProbeOffset.x;
					sourceWheelLocal[i].z -= wheelProbeOffset.z;
					sourceWheelLocal[i].y = commonWheelProbeY;
					sourceProbeLocal[i] = sourceWheelLocal[i];
				}
				// Preserve the prefab's vertical origin offset for body probes, while
				// retaining the source rig's centered horizontal coordinates.
				for (int i = 4; i < sourceProbeCount; i++)
					sourceProbeLocal[i] = sourceConfiguredProbeLocal[i] +
						new Vector3(0, wheelProbeOffset.y, 0);
			}
			for (int i = 0; i < 4; i++)
				sourceProbeLocal[i].y += sourceWheelProbeVerticalFitOffset[i];
			for (int i = 4; i < sourceProbeCount; i++)
				sourceProbeLocal[i].y += sourceBodyProbeVerticalFitOffset;
		}
		void FitSourceProbeHeightsToPrefabTires()
		{
			if (vehicle.wheels == null || vehicle.wheels.Length < 4 || !vehicle.rb || !vehicle.tr)
				return;
			// Refit from the unmodified rig each time parameters are refreshed.
			// UpdateSourceWheelProbeGeometry applies these offsets, so measuring
			// before clearing them would subtract the previous fit on every refresh.
			System.Array.Clear(sourceWheelProbeVerticalFitOffset, 0,
				sourceWheelProbeVerticalFitOffset.Length);
			System.Array.Clear(sourceWheelVisualVerticalOffset, 0,
				sourceWheelVisualVerticalOffset.Length);
			sourceBodyProbeVerticalFitOffset = 0;
			UpdateSourceWheelProbeGeometry();
			float totalOffset = 0;
			float totalTireBottomHeight = 0;
			int fittedWheelCount = 0;
			float[] expectedTireBottomHeights = new float[4];
			bool[] fittedWheels = new bool[4];
			string wheelFits = string.Empty;
			for (int sourceIndex = 0; sourceIndex < 4; sourceIndex++)
			{
				Wheel wheel = vehicle.wheels[UnityWheelIndex[sourceIndex]];
				if (!wheel || !wheel.connected || !wheel.rim || !wheel.susParent)
					continue;
				float tireRadius = wheel.actualRadius > 0 ? wheel.actualRadius : wheel.tireRadius;
				if (tireRadius <= 0)
					continue;
				Suspension suspension = wheel.susParent;
				Vector3 springDirection = suspension.springDirection.sqrMagnitude > 0.5f
					? suspension.springDirection.normalized : -vehicle.tr.up;
				Vector3 upDirection = suspension.upDir.sqrMagnitude > 0.5f
					? suspension.upDir.normalized : vehicle.tr.up;
				Vector3 maxCompressPoint = suspension.maxCompressPoint;
				if ((maxCompressPoint - suspension.tr.position).sqrMagnitude > 0.25f)
					maxCompressPoint = suspension.tr.position;
				OriginalTyrePhysicsConfig tyre = parameters.tyres[sourceIndex];
				float totalTravel = Mathf.Max(0.001f, tyre.travelIn + tyre.travelOut);
				float sourceRestTravel = Mathf.Clamp01(tyre.travelIn / totalTravel);
				float angleRadius = Mathf.Pow(Mathf.Max(
					Mathf.Abs(Mathf.Sin(suspension.sideAngle * Mathf.Deg2Rad)),
					Mathf.Abs(Mathf.Sin(suspension.casterAngle * Mathf.Deg2Rad))), 2);
				Vector3 steeringDirection = suspension.tr.TransformDirection(
					Mathf.Sin(wheel.tr.localEulerAngles.y * Mathf.Deg2Rad), 0,
					Mathf.Cos(wheel.tr.localEulerAngles.y * Mathf.Deg2Rad));
				Vector3 expectedRimPosition = maxCompressPoint +
					springDirection * suspension.suspensionDistance * sourceRestTravel +
					upDirection * angleRadius * tireRadius +
					suspension.pivotOffset * steeringDirection -
					suspension.pivotOffset * (suspension.forwardDir.sqrMagnitude > 0.5f
						? suspension.forwardDir : suspension.tr.forward);
				float rimHeight = Vector3.Dot(expectedRimPosition - vehicle.rb.position, vehicle.tr.up);
				float probeHeight = Vector3.Dot(
					vehicle.rb.rotation * (sourceProbeLocal[sourceIndex] * SourceLengthToMetres), vehicle.tr.up);
				float probeRadius = sourceProbeRadius[sourceIndex] * SourceLengthToMetres;
				float clearanceAtProbeContact = rimHeight - probeHeight + probeRadius - tireRadius;
				if (float.IsNaN(clearanceAtProbeContact) || float.IsInfinity(clearanceAtProbeContact) ||
					Mathf.Abs(clearanceAtProbeContact) > 0.75f)
					continue;
				sourceWheelProbeVerticalFitOffset[sourceIndex] =
					clearanceAtProbeContact / SourceLengthToMetres;
				totalOffset += clearanceAtProbeContact / SourceLengthToMetres;
				expectedTireBottomHeights[sourceIndex] = rimHeight - tireRadius;
				totalTireBottomHeight += expectedTireBottomHeights[sourceIndex];
				fittedWheels[sourceIndex] = true;
				fittedWheelCount++;
				wheelFits += $" w{sourceIndex}={clearanceAtProbeContact:F3}m";
			}
			if (fittedWheelCount == 0)
				return;
			// Wheel presentation aligns the tire bottoms below to their average.
			// Fit the contact probes to that same geometry: fitting to the unadjusted
			// tire bottoms leaves unequal-radius axles at different probe heights.
			// UpdateSourceSupportBasis then interprets that built-in slope as road
			// pitch, even at spawn (Sky Hawk), feeding rotation back into contacts.
			float averageTireBottomHeight = totalTireBottomHeight / fittedWheelCount;
			for (int sourceIndex = 0; sourceIndex < 4; sourceIndex++)
			{
				if (!fittedWheels[sourceIndex])
					continue;
				float visualOffset = averageTireBottomHeight - expectedTireBottomHeights[sourceIndex];
				sourceWheelVisualVerticalOffset[sourceIndex] = visualOffset;
				sourceWheelProbeVerticalFitOffset[sourceIndex] += visualOffset / SourceLengthToMetres;
			}
			sourceBodyProbeVerticalFitOffset = totalOffset / fittedWheelCount;
			//Debug.Log($"[OriginalVehiclePhysics] Initial probe placement: " +
			//	$"car={vehicle.carConfig?.name}, source={parameters.sourceConfig}, " +
			//	$"probeOffsets={wheelFits}, bodyProbeOffset=" +
			//	$"{sourceBodyProbeVerticalFitOffset * SourceLengthToMetres:F3}m.", vehicle);
		}
		Vector3 SourceProbeWorldPosition(int index)
		{
			return SourceProbeWorldPosition(index, vehicle.rb.rotation);
		}
		Vector3 SourceProbeWorldPosition(int index, Quaternion rotation)
		{
			Vector3 sourceLocal = sourceProbeLocal[index];
			// The retail world and Unity both use +Y as up. The source models use
			// +Z as forward and the wheel probes sit at negative local Y.
			Vector3 unityLocal = sourceLocal;
			return vehicle.rb.position + rotation * (unityLocal * SourceLengthToMetres);
		}
		public void ResetSourceProbeHistory()
		{
			UpdateSourceWheelProbeGeometry();
			hasGroundSurfaceContactThisTick = false;
			groundSurfaceContactNormalSumThisTick = Vector3.zero;
			System.Array.Clear(sourceWheelGroundSurfaceContactThisTick, 0,
				sourceWheelGroundSurfaceContactThisTick.Length);
			sourceIncrementalRotation = Quaternion.identity;
			pendingContactRotation = Quaternion.identity;
			hasPendingContactRotation = false;
			sourceContactAngularStep = Vector3.zero;
			lastSourceControlledRotation = vehicle.rb.rotation;
			stepStartRotation = stepRotation = lastSourceControlledRotation;
			stepRotationChanged = false;
			hasSourceControlledRotation = true;
			sourceAngularVelocity = Vector3.zero;
			System.Array.Clear(sourceProbeContactIterations, 0, sourceProbeContactIterations.Length);
			System.Array.Clear(sourceProbeHitsThisTick, 0, sourceProbeHitsThisTick.Length);
			sourceRotationDiagnosticLogged = false;
			sourceStabilityDiagnosticSamples = 0;
			nextSourceStabilityDiagnosticTime = Time.fixedTime + 0.5f;
			for (int i = 0; i < sourceProbeCount; i++)
			{
				previousSourceProbeWorld[i] = SourceProbeWorldPosition(i);
				hasPreviousSourceProbe[i] = true;
			}
			UpdateSourceSupportBasis(vehicle.rb.rotation);
		}
		void ResetSourceWakeHistory()
		{
			Vector3 position = vehicle.rb.position;
			for (int i = 0; i < sourceWakePositions.Length; i++)
			{
				sourceWakePositions[i] = position;
				sourceWakeRadii[i] = 0;
			}
			sourceWakeTicks = 0;
			sourceAirFactor = 1;
		}
		static void ResetSourceAirFactorsForTick(int tick)
		{
			if (sourceWakeResetTick == tick)
				return;
			if (F.I)
			{
				var cars = F.I.s_cars;
				for (int i = 0; i < cars.Count; i++)
				{
					VehicleParent car = cars[i];
					if (car && car.originalVehiclePhysics != null)
						car.originalVehiclePhysics.sourceAirFactor = 1;
				}
			}
			sourceWakeResetTick = tick;
		}
		void UpdateSourcePositionHistoryAndWake(float tickScale)
		{
			if (!vehicle.raceBox || !vehicle.raceBox.enabled || !vehicle.gameObject.activeInHierarchy)
				return;
			sourceWakeTicks += tickScale;
			if (sourceWakeTicks >= 8)
			{
				for (int i = sourceWakePositions.Length - 1; i > 0; i--)
				{
					sourceWakePositions[i] = sourceWakePositions[i - 1];
					sourceWakeRadii[i] = sourceWakeRadii[i - 1] * 0.95f;
				}
				sourceWakePositions[0] = vehicle.rb.position;
				sourceWakeRadii[0] = 400 * SourceLengthToMetres;
				sourceWakeTicks -= 8;
			}
			// retail::update_vehicle_position_history updates trails in every race
			// mode, but does not apply the wake during Time Trial (source mode 4).
			if (!F.I || F.I.s_raceType == RaceType.TimeTrial || currentSourceSpeed < 16.666666f)
				return;
			Vector3 oldestToNewest = sourceWakePositions[0] - sourceWakePositions[7];
			float totalLength = oldestToNewest.magnitude;
			if (totalLength <= 0.000001f)
				return;
			Vector3 direction = oldestToNewest / totalLength;
			var cars = F.I.s_cars;
			for (int target = 0; target < cars.Count; target++)
			{
				VehicleParent other = cars[target];
				if (!other || other == vehicle || other.originalVehiclePhysics == null || !other.rb ||
					!other.raceBox || !other.raceBox.enabled || !other.gameObject.activeInHierarchy ||
					other.originalVehiclePhysics.currentSourceSpeed <= 16.666666f)
					continue;
				Vector3 position = other.rb.position;
				float projected = Vector3.Dot(sourceWakePositions[0] - position, direction);
				if (projected <= 0 || projected >= totalLength)
					continue;
				for (int segment = 0; segment < 7; segment++)
				{
					Vector3 a = sourceWakePositions[segment];
					Vector3 delta = a - sourceWakePositions[segment + 1];
					float length = delta.magnitude;
					if (length <= SourceLengthToMetres)
						continue;
					Vector3 axis = delta / length;
					Vector3 fromOther = a - position;
					float along = Vector3.Dot(fromOther, axis);
					if (along <= 0 || along >= length)
						continue;
					Vector3 radial = fromOther - along * axis;
					float radius = sourceWakeRadii[segment] - along / length *
						(sourceWakeRadii[segment] - sourceWakeRadii[segment + 1]);
					float radiusSquared = radius * radius;
					float distanceSquared = radial.sqrMagnitude;
					if (distanceSquared >= radiusSquared)
						continue;
					float ratio = distanceSquared / radiusSquared;
					other.originalVehiclePhysics.sourceAirFactor = Mathf.Max(0.1f, ratio * ratio);
					break;
				}
			}
		}
		void UpdateSourceIdleSuspension(int contactClass)
		{
			// retail vehicle_tick_composition calls idle_vehicle_suspension after
			// vehicle_dynamics_tick. This changes spring state for the next tick only.
			if (contactClass != 0)
				return;
			float speed = Mathf.Abs(currentSourceSpeed);
			if (speed > 3)
			{
				sourceIdleHold = 300;
				return;
			}
			sourceIdleHold = Mathf.Max(0, sourceIdleHold - 1);
			if (sourceIdleHold >= 180)
				return;
			sourceIdleInterval--;
			if (sourceIdleInterval > 0)
				return;
			sourceIdleInterval = 8;
			float amount = (float)(1.0 / ((double)(sourceIdleHold / 16 + 1) + speed));
			if (sourceIdleHold == 0 && SourceRandomInteger(32) == 1)
				amount = 2;
			for (int i = 0; i < sourceSuspension.Length; i++)
				sourceSuspension[i] += amount;
		}
		void UpdateSourceSupportBasis(Quaternion fallbackRotation)
		{
			// update_support_basis uses the source probe order directly. Probe 0 is
			// on the left, so its lateral vector points left; convert that vector to
			// Unity-right. Source Y and Unity Y are both up.
			Vector3 a = previousSourceProbeWorld[0];
			Vector3 b = previousSourceProbeWorld[1];
			Vector3 c = previousSourceProbeWorld[2];
			Vector3 d = previousSourceProbeWorld[3];
			Vector3 left = ((a + c) - (b + d)) * 0.5f;
			Vector3 forward = ((c + d) - (a + b)) * 0.5f;
			if (left.sqrMagnitude > 0.000001f && forward.sqrMagnitude > 0.000001f)
			{
				left.Normalize();
				forward.Normalize();
				Vector3 up = Vector3.Cross(left, forward);
				if (up.sqrMagnitude > 0.000001f)
				{
					sourceRightBasis = -left;
					sourceForwardBasis = forward;
					sourceUpBasis = up.normalized;
					return;
				}
			}
			sourceRightBasis = fallbackRotation * Vector3.right;
			sourceForwardBasis = fallbackRotation * Vector3.forward;
			sourceUpBasis = fallbackRotation * Vector3.up;
		}
		bool IsSourceTrackCollider(Collider collider)
		{
			if (!collider || collider.isTrigger || collider.attachedRigidbody || IsInvisibleLevelCollider(collider))
				return false;
			return collider.GetComponentInParent<VehicleParent>() == null;
		}
		static bool IsInvisibleLevelCollider(Collider collider)
		{
			if (!invisibleLevelLayerInitialized)
			{
				invisibleLevelLayer = LayerMask.NameToLayer("InvisibleLevel");
				invisibleLevelLayerInitialized = true;
			}
			return collider && invisibleLevelLayer >= 0 && collider.gameObject.layer == invisibleLevelLayer;
		}
		void ApplySourceTrackCollisionExclusions()
		{
			if (!RaceManager.I)
				return;
			int layerMask = RaceManager.I.wheelCastMask.value;
			if (Time.unscaledTime >= nextSourceTrackCollisionScan || sourceTrackCollisionMask != layerMask)
			{
				Collider[] sceneColliders = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None);
				var trackIds = new System.Collections.Generic.HashSet<int>();
				var trackTargets = new System.Collections.Generic.List<Collider>();
				var invisibleLevelIds = new System.Collections.Generic.HashSet<int>();
				var invisibleLevelTargets = new System.Collections.Generic.List<Collider>();
				var vehicleIds = new System.Collections.Generic.HashSet<int>();
				var vehicleTargets = new System.Collections.Generic.List<Collider>();
				foreach (Collider candidate in sceneColliders)
				{
					if (!candidate || candidate.isTrigger || !candidate.enabled ||
						!candidate.gameObject.activeInHierarchy)
						continue;
					// InvisibleLevel is a visual/placement aid only. Do not let the
					// source solver select it, and explicitly disable PhysX contacts
					// between it and every collider belonging to an original-physics car.
					if (IsInvisibleLevelCollider(candidate))
					{
						invisibleLevelIds.Add(candidate.GetInstanceID());
						invisibleLevelTargets.Add(candidate);
						continue;
					}
					VehicleParent colliderVehicle = candidate.GetComponentInParent<VehicleParent>();
					if (colliderVehicle)
					{
						if (colliderVehicle.originalVehiclePhysics == null || candidate.attachedRigidbody != colliderVehicle.rb)
							continue;
						vehicleIds.Add(candidate.GetInstanceID());
						vehicleTargets.Add(candidate);
						continue;
					}
					// Contacts use the colliders generated from the active Unity map (including
					// mergedTrackCollider), with the same layer mask as the wheel queries.
					if ((layerMask & (1 << candidate.gameObject.layer)) == 0 ||
						!IsSourceTrackCollider(candidate))
						continue;
					trackIds.Add(candidate.GetInstanceID());
					trackTargets.Add(candidate);
				}
				if (!SourceTrackColliderIds.SetEquals(trackIds) || sourceTrackCollisionMask != layerMask ||
					!SourceVehicleColliderIds.SetEquals(vehicleIds) ||
					!SourceInvisibleLevelColliderIds.SetEquals(invisibleLevelIds))
				{
					SourceTrackColliderIds.Clear();
					SourceTrackColliderIds.UnionWith(trackIds);
					sourceTrackCollisionTargets = trackTargets.ToArray();
					SourceVehicleColliderIds.Clear();
					SourceVehicleColliderIds.UnionWith(vehicleIds);
					sourceVehicleCollisionTargets = vehicleTargets.ToArray();
					SourceInvisibleLevelColliderIds.Clear();
					SourceInvisibleLevelColliderIds.UnionWith(invisibleLevelIds);
					sourceInvisibleLevelCollisionTargets = invisibleLevelTargets.ToArray();
					sourceTrackCollisionMask = layerMask;
				}
				// IgnoreCollision is not persistent across collider/rigidbody
				// deactivation. Reapply the cached pairs after each periodic scan.
				sourceTrackCollisionGeneration++;
				nextSourceTrackCollisionScan = Time.unscaledTime + 1;
			}
			if (appliedSourceTrackCollisionGeneration == sourceTrackCollisionGeneration)
				return;
			Collider[] ownColliders = vehicle.GetComponentsInChildren<Collider>();
			foreach (Collider own in ownColliders)
			{
				if (!own || !own.enabled || !own.gameObject.activeInHierarchy)
					continue;
				bool isVehicleBodyCollider = own.attachedRigidbody == vehicle.rb;
				foreach (Collider oldInvisibleLevel in appliedSourceInvisibleLevelCollisionTargets)
					if (oldInvisibleLevel && oldInvisibleLevel.enabled && oldInvisibleLevel.gameObject.activeInHierarchy &&
						!SourceInvisibleLevelColliderIds.Contains(oldInvisibleLevel.GetInstanceID()))
						Physics.IgnoreCollision(own, oldInvisibleLevel, false);
				foreach (Collider invisibleLevel in sourceInvisibleLevelCollisionTargets)
					if (invisibleLevel && invisibleLevel != own)
						Physics.IgnoreCollision(own, invisibleLevel, true);
				// Preserve trigger colliders for start/finish and gameplay events.
				// Their only ignored pairs are with InvisibleLevel above.
				if (own.isTrigger || !isVehicleBodyCollider)
					continue;
				foreach (Collider oldTrack in appliedSourceTrackCollisionTargets)
					if (isVehicleBodyCollider && oldTrack && oldTrack.enabled && oldTrack.gameObject.activeInHierarchy &&
						!SourceTrackColliderIds.Contains(oldTrack.GetInstanceID()))
						Physics.IgnoreCollision(own, oldTrack, false);
				foreach (Collider oldVehicle in appliedSourceVehicleCollisionTargets)
					if (isVehicleBodyCollider && oldVehicle && oldVehicle.enabled && oldVehicle.gameObject.activeInHierarchy &&
						!SourceVehicleColliderIds.Contains(oldVehicle.GetInstanceID()))
						Physics.IgnoreCollision(own, oldVehicle, false);
				foreach (Collider track in sourceTrackCollisionTargets)
					if (isVehicleBodyCollider && track && track != own)
						Physics.IgnoreCollision(own, track, true);
				foreach (Collider otherCollider in sourceVehicleCollisionTargets)
					if (isVehicleBodyCollider && otherCollider && otherCollider != own &&
						otherCollider.GetComponentInParent<VehicleParent>() != vehicle)
						Physics.IgnoreCollision(own, otherCollider, true);
			}
			appliedSourceTrackCollisionTargets = sourceTrackCollisionTargets;
			appliedSourceVehicleCollisionTargets = sourceVehicleCollisionTargets;
			appliedSourceInvisibleLevelCollisionTargets = sourceInvisibleLevelCollisionTargets;
			appliedSourceTrackCollisionGeneration = sourceTrackCollisionGeneration;
		}
		bool TryFindSourceProbeContact(Vector3 start, Vector3 movement, float radius,
			out Collider collider, out Vector3 point, out Vector3 normal, out Vector3 probeCenter,
			out float travelDistance, out int triangleIndex)
		{
			collider = null;
			point = normal = probeCenter = Vector3.zero;
			travelDistance = 0;
			triangleIndex = -1;
			if (!RaceManager.I)
				return false;
			int layerMask = RaceManager.I.wheelCastMask;
			float distance = movement.magnitude;
			Vector3 end = start + movement;
			if (distance > 0.001f && TrySweepSourceTrackMeshes(
				sourceTrackCollisionTargets,
				layerMask, start, movement, radius, out Collider sourceCollider, out Vector3 sourcePoint,
				out Vector3 sourceNormal, out Vector3 sourceCenter, out float sourceTravel,
				out int sourceTriangleIndex))
			{
				collider = sourceCollider;
				point = sourcePoint;
				normal = sourceNormal;
				probeCenter = sourceCenter;
				travelDistance = Mathf.Clamp(sourceTravel, 0, distance);
				triangleIndex = sourceTriangleIndex;
				return true;
			}
			// PhysX sphere casts skip colliders that overlap their starting sphere.
			// Recover the source solver's zero-distance contact before sweeping so
			// a high-speed landing cannot begin inside the track and tunnel through it.
			if (distance > 0.001f && TryFindSourceProbeOverlap(start, movement.normalized, radius,
				layerMask, out Collider startCollider, out Vector3 startPoint, out Vector3 startNormal,
				out Vector3 startProbeCenter, out int startTriangleIndex, true) &&
				Vector3.Dot(movement, startNormal) < -0.0001f)
			{
				collider = startCollider;
				point = startPoint;
				normal = startNormal;
				probeCenter = startProbeCenter;
				travelDistance = 0;
				triangleIndex = startTriangleIndex;
				return true;
			}
			if (distance > 0.001f)
			{
				Vector3 direction = movement / distance;
				RaycastHit[] castHits = sourceProbeCastHits;
				int hitCount = Physics.SphereCastNonAlloc(start, radius, direction,
					castHits, distance + 0.01f, layerMask, QueryTriggerInteraction.Ignore);
				// NonAlloc queries silently cap their result count at the buffer size.
				// A long probe sweep can cross many modular tile colliders; in that case
				// use the complete result set so the nearest source contact is not omitted.
				if (hitCount == castHits.Length)
				{
					castHits = Physics.SphereCastAll(start, radius, direction,
						distance + 0.01f, layerMask, QueryTriggerInteraction.Ignore);
					hitCount = castHits.Length;
				}
				float closestDistance = float.PositiveInfinity;
				int closestTriangleIndex = -1;
				for (int i = 0; i < hitCount; i++)
				{
					RaycastHit hit = castHits[i];
					if (hit.collider is MeshCollider meshCollider &&
						OriginalTrackSphereSweep.WasProcessed(meshCollider))
						continue;
					if (hit.distance >= closestDistance || !IsSourceTrackCollider(hit.collider))
						continue;
					closestDistance = hit.distance;
					collider = hit.collider;
					point = hit.point;
					normal = hit.normal.normalized;
					closestTriangleIndex = hit.triangleIndex;
				}
				if (collider)
				{
					travelDistance = Mathf.Clamp(closestDistance, 0, distance);
					probeCenter = start + direction * travelDistance;
					triangleIndex = closestTriangleIndex;
					return true;
				}
			}
			if (!TryFindSourceProbeOverlap(end, movement.sqrMagnitude > 0.000001f
				? movement.normalized : SourceGravityDirection, radius, layerMask,
				out collider, out point, out normal, out probeCenter, out triangleIndex, distance > 0.001f))
				return false;
			if (distance > 0.001f && Vector3.Dot(movement, normal) > 0.0001f)
				return false;
			travelDistance = distance;
			return true;
		}
		bool TrySweepSourceTrackMeshes(Collider[] colliders, int layerMask, Vector3 start,
			Vector3 movement, float radius, out Collider collider, out Vector3 point, out Vector3 normal,
			out Vector3 center, out float travelDistance, out int triangleIndex)
		{
			using (SourceTrackMeshSweepMarker.Auto())
				return OriginalTrackSphereSweep.TrySweep(colliders, layerMask, start, movement, radius,
					out collider, out point, out normal, out center, out travelDistance, out triangleIndex);
		}
		bool TryFindSourceProbeOverlap(Vector3 center, Vector3 rayDirection, float radius, int layerMask,
			out Collider collider, out Vector3 point, out Vector3 normal, out Vector3 correctedCenter,
			out int triangleIndex, bool skipSourceSweptMeshes = false)
		{
			collider = null;
			point = normal = correctedCenter = Vector3.zero;
			triangleIndex = -1;
			Collider[] overlapHits = sourceProbeOverlapHits;
			int overlapCount = Physics.OverlapSphereNonAlloc(center, radius + 0.001f, overlapHits,
				layerMask, QueryTriggerInteraction.Ignore);
			// Like SphereCastNonAlloc, a full buffer may hide the best recovery surface
			// when the probe begins inside several overlapping track pieces.
			if (overlapCount == overlapHits.Length)
			{
				overlapHits = Physics.OverlapSphere(center, radius + 0.001f,
					layerMask, QueryTriggerInteraction.Ignore);
				overlapCount = overlapHits.Length;
			}
			float closestSquaredDistance = float.PositiveInfinity;
			int closestTriangleIndex = -1;
			for (int i = 0; i < overlapCount; i++)
			{
				Collider candidate = overlapHits[i];
				if (skipSourceSweptMeshes && candidate is MeshCollider sweptMeshCollider &&
					OriginalTrackSphereSweep.WasProcessed(sweptMeshCollider))
					continue;
				if (!IsSourceTrackCollider(candidate))
					continue;
				Vector3 candidatePoint;
				Vector3 candidateNormal;
				float squaredDistance;
				int candidateTriangleIndex = -1;
				bool centerInsideCollider;
				bool supportsClosestPoint = candidate is BoxCollider || candidate is SphereCollider ||
					candidate is CapsuleCollider ||
					(candidate is MeshCollider closestPointMesh && closestPointMesh.convex);
				if (!supportsClosestPoint)
				{
					// ClosestPoint is unsupported for terrain and non-convex meshes.
					// Cast inward from outside the collider bounds along probe motion,
					// then gravity as a fallback for lateral motion over terrain.
					Bounds bounds = candidate.bounds;
					float reach = Vector3.Distance(center, bounds.center) + bounds.extents.magnitude +
						radius + 0.01f;
					Vector3 firstDirection = rayDirection.sqrMagnitude > 0.000001f
						? rayDirection.normalized : SourceGravityDirection;
					bool foundSurface = false;
					squaredDistance = float.PositiveInfinity;
					candidatePoint = Vector3.zero;
					candidateNormal = Vector3.zero;
					for (int rayIndex = 0; rayIndex < 2; rayIndex++)
					{
						Vector3 castDirection = rayIndex == 0 ? firstDirection : SourceGravityDirection;
						if (rayIndex == 1 && Mathf.Abs(Vector3.Dot(firstDirection, castDirection)) > 0.98f)
							continue;
						Ray inwardRay = new Ray(center - castDirection * reach, castDirection);
						if (!candidate.Raycast(inwardRay, out RaycastHit rayHit, reach + radius + 0.01f))
							continue;
						float hitDistanceSquared = (center - rayHit.point).sqrMagnitude;
						if (hitDistanceSquared >= squaredDistance)
							continue;
						foundSurface = true;
						squaredDistance = hitDistanceSquared;
						candidatePoint = rayHit.point;
						candidateNormal = rayHit.normal.normalized;
						candidateTriangleIndex = rayHit.triangleIndex;
					}
					if (!foundSurface)
						continue;
					// TerrainCollider represents a solid height field. If its overlap
					// query found the probe below the sampled surface, recover it even
					// when penetration depth exceeds the probe radius.
					centerInsideCollider = candidate is TerrainCollider || squaredDistance <= 0.000001f;
				}
				else
				{
					candidatePoint = candidate.ClosestPoint(center);
					Vector3 fromSurface = center - candidatePoint;
					squaredDistance = fromSurface.sqrMagnitude;
					centerInsideCollider = squaredDistance <= 0.000001f;
					if (squaredDistance > 0.000001f)
						candidateNormal = fromSurface / Mathf.Sqrt(squaredDistance);
					else
					{
						// A center already inside a thick primitive or convex mesh makes
						// ClosestPoint return the center. Trace inward from beyond its
						// bounds to recover the entry surface and its normal.
						Vector3 castDirection = rayDirection.sqrMagnitude > 0.000001f
							? rayDirection.normalized
							: SourceGravityDirection;
						Bounds bounds = candidate.bounds;
						float reach = Vector3.Distance(center, bounds.center) + bounds.extents.magnitude +
							radius + 0.01f;
						Ray inwardRay = new Ray(center - castDirection * reach, castDirection);
						if (!candidate.Raycast(inwardRay, out RaycastHit hit, reach + radius + 0.01f))
							continue;
						candidatePoint = hit.point;
						candidateNormal = hit.normal.normalized;
						squaredDistance = (center - candidatePoint).sqrMagnitude;
						candidateTriangleIndex = hit.triangleIndex;
					}
				}
				if (!centerInsideCollider && candidate is MeshCollider && candidateNormal.sqrMagnitude > 0.000001f &&
					candidate.Raycast(new Ray(center, -candidateNormal), out RaycastHit surfaceHit, radius + 0.01f))
					candidateTriangleIndex = surfaceHit.triangleIndex;
				if (squaredDistance >= closestSquaredDistance ||
					(!centerInsideCollider && squaredDistance > (radius + 0.001f) * (radius + 0.001f)))
					continue;
				closestSquaredDistance = squaredDistance;
				collider = candidate;
				point = candidatePoint;
				normal = candidateNormal;
				closestTriangleIndex = candidateTriangleIndex;
			}
			if (collider)
			{
				correctedCenter = point + normal * radius;
				triangleIndex = closestTriangleIndex;
			}
			return collider;
		}
		void ApplySourceSurface(int sourceWheel, Wheel wheel, Collider collider, Vector3 point, int triangleIndex)
		{
			int surfaceType = 0;
			GroundSurfaceInstance surface = collider ? collider.GetComponent<GroundSurfaceInstance>() : null;
			TerrainSurface terrain = collider ? collider.GetComponent<TerrainSurface>() : null;
			if (surface || terrain)
			{
				hasGroundSurfaceContactThisTick = true;
				if (!sourceWheelGroundSurfaceContactThisTick[sourceWheel])
				{
					sourceWheelGroundSurfaceContactThisTick[sourceWheel] = true;
					groundSurfaceContactNormalSumThisTick += sourceWheelContactNormal[sourceWheel];
				}
			}
			if (surface)
				surfaceType = surface.surfaceType;
			else if (terrain)
				surfaceType = terrain.GetDominantSurfaceTypeAtPoint(point);
			// Unity map surfaces provide the source-format contact channels. Unknown
			// surfaces use the common asphalt fallback profile.
			wheel.contactPoint.sourceGrip = DefaultSourceTrackGrip;
			wheel.contactPoint.sourceFlags = 1;
			wheel.contactPoint.sourceRolling = DefaultSourceTrackRolling;
			wheel.contactPoint.sourceRoughness = (int)DefaultSourceTrackRoughness;
			GroundSurface[] sourceSurfaces = GroundSurfaceMaster.surfaceTypesStatic;
			if (sourceSurfaces != null && surfaceType >= 0 && surfaceType < sourceSurfaces.Length &&
				sourceSurfaces[surfaceType] != null)
			{
				GroundSurface sourceSurface = sourceSurfaces[surfaceType];
				if (sourceSurface.sourceGrip >= 0)
					wheel.contactPoint.sourceGrip = sourceSurface.sourceGrip;
				if (sourceSurface.sourceFlags >= 0)
					wheel.contactPoint.sourceFlags = sourceSurface.sourceFlags;
				if (sourceSurface.sourceRolling >= 0)
					wheel.contactPoint.sourceRolling = sourceSurface.sourceRolling;
				if (sourceSurface.sourceRoughness >= 0)
					wheel.contactPoint.sourceRoughness = Mathf.Clamp(sourceSurface.sourceRoughness, 0, 255);
			}
			// Collider-local source material data remains authoritative even when a
			// scene has no global surface table or uses a surface type outside it.
			if (surface)
			{
				if (surface.sourceGrip >= 0)
					wheel.contactPoint.sourceGrip = surface.sourceGrip;
				if (surface.sourceFlags >= 0)
					wheel.contactPoint.sourceFlags = surface.sourceFlags;
				if (surface.sourceRolling >= 0)
					wheel.contactPoint.sourceRolling = surface.sourceRolling;
				if (surface.sourceRoughness >= 0)
					wheel.contactPoint.sourceRoughness = Mathf.Clamp(surface.sourceRoughness, 0, 255);
				if (surface.TryGetSourceSubmeshProfile(triangleIndex, out float submeshGrip,
					out float submeshRolling, out int submeshRoughness, out int submeshGripByte,
					out int submeshFlags))
				{
					if (submeshGripByte >= 0)
						wheel.contactPoint.sourceGrip = submeshGripByte * 0.015625f;
					if (submeshGrip >= 0)
						wheel.contactPoint.sourceGrip = submeshGrip;
					if (submeshRolling >= 0)
						wheel.contactPoint.sourceRolling = submeshRolling;
					if (submeshRoughness >= 0)
						wheel.contactPoint.sourceRoughness = Mathf.Clamp(submeshRoughness, 0, 255);
					if (submeshFlags >= 0)
						wheel.contactPoint.sourceFlags = submeshFlags;
				}
			}
			// 004091E0: type 10 remaps effective grip after decoding the raw grip byte.
			if ((wheel.contactPoint.sourceFlags & 15) == 10)
			{
				wheel.contactPoint.sourceFlags = (wheel.contactPoint.sourceFlags & 0xf0) | 1;
				wheel.contactPoint.sourceGrip = 4;
			}
			sourceWheelContactPoint[sourceWheel] = point;
			sourceWheelContactCollider[sourceWheel] = collider;
			sourceWheelSurfaceType[sourceWheel] = surfaceType;
			hasSourceWheelSurfaceContact[sourceWheel] = true;
			sourceWheelSurfaceGrip[sourceWheel] = wheel.contactPoint.sourceGrip;
			sourceWheelSurfaceRolling[sourceWheel] = wheel.contactPoint.sourceRolling;
			sourceWheelSurfaceRoughness[sourceWheel] = wheel.contactPoint.sourceRoughness;
		}
		int ConfiguredSourceSurfaceFlags(Collider collider, Vector3 point, int triangleIndex)
		{
			int surfaceType = 0;
			GroundSurfaceInstance surface = collider ? collider.GetComponent<GroundSurfaceInstance>() : null;
			TerrainSurface terrain = collider ? collider.GetComponent<TerrainSurface>() : null;
			if (surface)
				surfaceType = surface.surfaceType;
			else if (terrain)
				surfaceType = terrain.GetDominantSurfaceTypeAtPoint(point);
			int flags = 1;
			GroundSurface[] sourceSurfaces = GroundSurfaceMaster.surfaceTypesStatic;
			if (sourceSurfaces != null && surfaceType >= 0 && surfaceType < sourceSurfaces.Length &&
				sourceSurfaces[surfaceType] != null && sourceSurfaces[surfaceType].sourceFlags >= 0)
				flags = sourceSurfaces[surfaceType].sourceFlags;
			if (surface)
			{
				if (surface.sourceFlags >= 0)
					flags = surface.sourceFlags;
				if (surface.TryGetSourceSubmeshProfile(triangleIndex, out _, out _, out _, out _,
					out int submeshFlags) && submeshFlags >= 0)
					flags = submeshFlags;
			}
			return flags;
		}
		void ProcessSourceContactMaterial(int probeIndex, Collider collider, Vector3 point, Vector3 normal,
			int triangleIndex, float alignment)
		{
			// contact_solver calls process_material for every probe impact, including
			// the body probes. Preserve the raw type here: apply_material remaps type
			// 10 to type 1 later, while process_material sees the original flags.
			int flags = ConfiguredSourceSurfaceFlags(collider, point, triangleIndex);
			int type = flags & 15;
			bool unsafeContact = false;
			if (type >= 1 && type <= 4)
				sourceMassOverride = false;
			if (!sourceSpecialMode && sourceContactClass == 0 && (flags & 15) == 10)
			{
				sourceSpecialMode = true;
				sourceRailProgress = 0;
				sourceRailEnergyAdded = 0;
				sourceRailTicks = 0;
			}
			if ((flags & 0x40) != 0)
			{
				sourceMassOverride = true;
				if (currentSourceSpeed < 8.333333f)
					unsafeContact = true;
			}
			if ((flags & 0x80) != 0)
			{
				unsafeContact = true;
				stuntActive = false;
				airTicks = 0;
				pitchAcceleration = yawAcceleration = 0;
				pitchSpeed = yawSpeed = 0;
				ResetSourceStuntRoll();
				ResetSourceStuntPhaseHistory();
			}
			if ((flags & 0x20) != 0 && currentSourceSpeed < 1.6666666f)
				unsafeContact = true;
			if (!F.I || F.I.s_raceType != RaceType.Stunt)
			{
				Vector3 heading = vehicle.followAI && vehicle.followAI.trackPathCreator
					? vehicle.followAI.trackPathCreator.path.GetDirectionAtDistance(vehicle.followAI.progress).normalized
					: sourceForwardBasis.normalized;
				if (Vector3.Dot(heading, sourceForwardBasis) < -0.707f)
				{
					sourceOpposingDirectionTicks++;
					if (sourceOpposingDirectionTicks > 600)
					{
						sourceContactResetPending = true;
						sourceContactResetReason = "opposing-direction timeout";
						sourceOpposingDirectionTicks = 0;
					}
				}
				else
					sourceOpposingDirectionTicks = 0;
			}
			if (!unsafeContact && alignment > 0.7071068f)
			{
				sourceUnsafeContactTicks = 0;
				return;
			}
			// Keep the last contact that advanced the retail safety timer. When the
			// timer requests a reset, this distinguishes an authored unsafe Unity
			// surface from a bad normal or a track-collider mismatch.
			sourceLastUnsafeProbe = probeIndex;
			sourceLastUnsafeFlags = flags;
			sourceLastUnsafeAlignment = alignment;
			sourceLastUnsafePoint = point;
			sourceLastUnsafeNormal = normal;
			if (!unsafeContact)
				sourceLastUnsafeCause = "contact alignment <= 0.707";
			else if ((flags & 0x80) != 0)
				sourceLastUnsafeCause = "material 0x80";
			else if ((flags & 0x40) != 0 && currentSourceSpeed < 8.333333f)
				sourceLastUnsafeCause = "material 0x40 below speed threshold";
			else if ((flags & 0x20) != 0 && currentSourceSpeed < 1.6666666f)
				sourceLastUnsafeCause = "material 0x20 below speed threshold";
			else
				sourceLastUnsafeCause = "material safety flag";
			sourceLastUnsafeCollider = collider ? collider.name : "<no Unity collider>";
			sourceLastUnsafeLayer = collider ? collider.gameObject.layer : -1;
			sourceUnsafeContactTicks++;
			if (sourceUnsafeContactTicks >= 180)
			{
				sourceContactResetPending = true;
				sourceContactResetReason = "unsafe surface-contact timeout";
				sourceUnsafeContactTicks = 0;
			}
		}
		public OriginalTyrePhysicsConfig TyreForUnityWheel(int index)
		{
			return parameters.tyres[index < 2 ? index + 2 : index - 2];
		}
		void ApplySourceDimensions()
		{
			vehicle.rb.mass = (parameters.mass + fuel * parameters.fuelUnitMass) * SourceMassToKilograms;
			vehicle.rb.centerOfMass = Vector3.zero;
			// VehicleParent holds newly spawned cars kinematic until ApplySetup has
			// applied their config. Unity rejects velocity writes in that state.
			if (!vehicle.rb.isKinematic)
				vehicle.rb.angularVelocity = Vector3.zero;
			// The retail tick applies gravity in vehicle_tick_composition immediately
			// before vehicle dynamics, with a 100 m/s velocity guard. Reproduce it in
			// the source adapter instead of letting PhysX add gravity after this tick.
			vehicle.rb.useGravity = false;
			vehicle.rb.linearDamping = 0;
			vehicle.rb.angularDamping = 0;
			vehicle.rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
			// Keep prefab colliders enabled so they can enter start/finish and
			// gameplay triggers. ApplySourceTrackCollisionExclusions suppresses
			// solid PhysX contacts against track and other cars without disabling
			// the colliders or their trigger interactions.
			for (int i = 0; i < 4; i++)
			{
				Wheel wheel = vehicle.wheels[UnityWheelIndex[i]];
				OriginalTyrePhysicsConfig tyre = parameters.tyres[i];
				// Keep the prefab radius for wheel contacts and rendering. The source
				// radius remains in the original config and is converted where the
				// drivetrain needs it; applying it to this visual wheel pivot makes
				// the larger remake meshes sink into the track (notably car01).
				// The prefab suspension anchors are built for their existing travel.
				// Keep that geometry and map the original in/out travel onto it.
				float prefabTravel = vehicle.PrefabSuspensionTravel(UnityWheelIndex[i]);
				contactSpringTravel[i] = Mathf.Max(0.01f,
					prefabTravel > 0 ? prefabTravel : wheel.susParent.suspensionDistance);
				wheel.susParent.suspensionDistance = contactSpringTravel[i];
				wheelSpeed[i] = Vector3.Dot(vehicle.rb.GetPointVelocity(wheel.tr.position), vehicle.tr.forward) /
					SourceSpeedToMetresPerSecond;
			}
		}
		static float Curve(float[] samples, float x)
		{
			// The retail curve index truncates, then clamps to 127.
			return samples[Mathf.Clamp((int)x, 0, 127)];
		}
		static float SourceCurve(float[] samples, double x)
		{
			// curve_index uses the original truncating conversion, with out of
			// range values selecting the last sample.
			int index = x <= -1.0 || x >= 128.0 ? 127 : (int)x;
			return samples[index];
		}
		static void InitializeSourcePhysicsRandom()
		{
			if (sourcePhysicsRandomInitialized)
				return;
			// Retail seeds the shared vehicle RNG from local time during cold start.
			sourcePhysicsRandomState = unchecked((uint)System.DateTime.Now.TimeOfDay.TotalSeconds);
			sourcePhysicsRandomInitialized = true;
		}
		static double SourceRandomSigned(float amplitude)
		{
			sourcePhysicsRandomState = unchecked(sourcePhysicsRandomState * 0x343fdu + 0x269ec3u);
			int value = (int)((sourcePhysicsRandomState >> 16) & 0x7fffu) - 0x3fff;
			return (double)value * amplitude * (double)3.051851e-05f;
		}
		static int SourceRandomInteger(int amplitude)
		{
			sourcePhysicsRandomState = unchecked(sourcePhysicsRandomState * 0x343fdu + 0x269ec3u);
			int sample = (int)((sourcePhysicsRandomState >> 16) & 0x7fffu) - 0x3fff;
			return sample * amplitude / 0x7fff;
		}
		public bool AIStuntInProgress => sourceAiStuntMeter > 0;
		public bool PrepareAIStunt()
		{
			if (AIStuntInProgress || !vehicle.followAI || !vehicle.followAI.IsCPU ||
				!vehicle.followAI.selfDriving || vehicle.followAI.Pitting || CountDownSeq.Countdown > 0)
				return false;
			// FollowAI arms at the Unity ramp marker; wait for takeoff before steering a trick.
			sourceAiStuntMeter = 1;
			sourceAiStuntAirborne = false;
			sourceAiStuntDeadline = Time.fixedTime + 1;
			vehicle.SetSGPShift(1);
			int type = UnityEngine.Random.Range(0, 4);
			sourceAiStuntInputKind = type < 2
				? (type == 0 ? 4 : 2) + UnityEngine.Random.Range(0, 2)
				: type - 2;
			sourceAiLaunchPending = true;
			sourceAiLaunchTick = (uint)Mathf.RoundToInt(Time.fixedTime / Time.fixedDeltaTime);
			return true;
		}
		void UpdateSourceAIStunt(int tick)
		{
			sourceAiStuntInputThisTick = false;
			if (!AIStuntInProgress)
				return;
			if (!vehicle.followAI || !vehicle.followAI.IsCPU || !vehicle.followAI.selfDriving ||
				vehicle.followAI.Pitting || !vehicle.raceBox || !vehicle.raceBox.enabled ||
				Time.fixedTime >= sourceAiStuntDeadline || (sourceAiStuntAirborne && vehicle.reallyGroundedWheels > 0))
			{
				FinishAIStunt();
				return;
			}
			if (vehicle.reallyGroundedWheels > 0)
			{
				// The key stays armed while approaching the lip, as in the old AI coroutine.
				sourceAiLaunchTick = unchecked((uint)tick);
				return;
			}
			if (!sourceAiStuntAirborne)
			{
				sourceAiStuntAirborne = true;
				ClearAIStuntDirection();
				sourceAiStuntDeadline = Time.fixedTime + 0.5f;
			}
			sourceAiStuntInputThisTick = true;
		}
		void ClearAIStuntDirection()
		{
			vehicle.SetAccel(0);
			vehicle.SetBrake(0);
			vehicle.SetSteer(0);
			vehicle.SetRoll(0);
		}
		void FinishAIStunt()
		{
			if (sourceAiStuntAirborne)
				ClearAIStuntDirection();
			vehicle.SetSGPShift(0);
			sourceAiStuntMeter = 0;
			sourceAiLaunchPending = sourceAiStuntInputThisTick = false;
			sourceAiStuntAirborne = false;
		}
		static int SourceStuntSector(float transverse, float facing)
		{
			if (facing < 0)
			{
				if (transverse < 0)
					return transverse <= -SourceStuntPhaseBoundary ? 6 : 5;
				return transverse < SourceStuntPhaseBoundary ? 4 : 3;
			}
			if (transverse < 0)
				return transverse <= -SourceStuntPhaseBoundary ? 7 : 8;
			return transverse < SourceStuntPhaseBoundary ? 1 : 2;
		}
		void UpdateSourceStuntPhaseHistory()
		{
			Vector3 gravity = SourceGravityDirection;
			int pitchPhase = SourceStuntSector(-Vector3.Dot(gravity, sourceForwardBasis),
				-Vector3.Dot(gravity, sourceUpBasis));
			if (sourceStuntLastPhase[0] != pitchPhase && sourceStuntPhaseIndex[0] < 64)
			{
				sourceStuntPhaseIndex[0]++;
				sourceStuntLastPhase[0] = pitchPhase;
			}
			float projection = Vector3.Dot(gravity, vehicle.rb.linearVelocity);
			Vector3 horizontal = (vehicle.rb.linearVelocity - gravity * projection).normalized;
			Vector3 side = Vector3.Cross(horizontal, gravity).normalized;
			int yawPhase = SourceStuntSector(Vector3.Dot(side, sourceRightBasis),
				Vector3.Dot(horizontal, sourceForwardBasis));
			if (sourceStuntLastPhase[1] != yawPhase && sourceStuntPhaseIndex[1] < 64)
			{
				sourceStuntPhaseIndex[1]++;
				sourceStuntLastPhase[1] = yawPhase;
			}
		}
		void ResetSourceStuntPhaseHistory()
		{
			System.Array.Clear(sourceStuntPhaseIndex, 0, sourceStuntPhaseIndex.Length);
			for (int i = 0; i < sourceStuntLastPhase.Length; i++)
				sourceStuntLastPhase[i] = -1;
		}
		public float SteeringDegreesFor(Suspension suspension)
		{
			// vehicle_rotation(1, steering) sends +Z toward +X for a positive
			// source angle. Unity uses the same yaw direction for a +Z-forward car.
			return steeringDegrees;
		}
		public float CurrentSourceSpeed => currentSourceSpeed;
		// FollowAI's legacy off-track check only raycasts the Road layer. Preserve
		// actual source contacts with authored track surfaces on other layers.
		public bool HasGroundSurfaceContactThisTick => hasGroundSurfaceContactThisTick;
		public bool TryGetGroundSurfaceContactNormal(out Vector3 normal)
		{
			normal = groundSurfaceContactNormalSumThisTick.normalized;
			return hasGroundSurfaceContactThisTick && normal.sqrMagnitude > 0.5f;
		}
		// The source integrator advances its orientation directly and keeps Unity's
		// rigidbody angular velocity at zero. Give presentation code the equivalent
		// angular velocity of the orientation change for this source tick.
		public Vector3 SourceAngularVelocity => sourceAngularVelocity;
		public int GroundedWheelCount
		{
			get
			{
				int count = 0;
				for (int i = 0; i < contactCountdown.Length; i++)
					if (contactCountdown[i] >= 4)
						count++;
				return count;
			}
		}
		public bool StuntActive => stuntActive;
		public int ReallyGroundedWheelCount
		{
			get
			{
				int count = 0;
				for (int i = 0; i < sourceProbeWheelTouched.Length; i++)
					if (sourceProbeWheelTouched[i])
						count++;
				return count;
			}
		}
		public float GetWheelTravelDistance(Wheel wheel)
		{
			for (int i = 0; i < 4; i++)
			{
				if (vehicle.wheels[UnityWheelIndex[i]] != wheel)
					continue;
				OriginalTyrePhysicsConfig tyre = parameters.tyres[i];
				float totalTravel = Mathf.Max(0.001f, tyre.travelIn + tyre.travelOut);
				// The source uses positive suspension for bump/compression and negative
				// suspension for droop. Wheel.PositionWheel travels down along the spring
				// direction, so the normalized visual position runs in the opposite direction.
				return Mathf.Clamp01((tyre.travelIn - sourceSuspension[i]) / totalTravel);
			}
			return wheel.susParent ? wheel.susParent.compression : 0;
		}
		public float GetWheelVisualVerticalOffset(Wheel wheel)
		{
			if (vehicle.wheels == null || vehicle.wheels.Length < 4 || !wheel)
				return 0;
			for (int sourceIndex = 0; sourceIndex < 4; sourceIndex++)
			{
				if (vehicle.wheels[UnityWheelIndex[sourceIndex]] == wheel)
					return sourceWheelVisualVerticalOffset[sourceIndex];
			}
			return 0;
		}
		public void UpdateWheelContact(Wheel wheel)
		{
			int unityIndex = System.Array.IndexOf(vehicle.wheels, wheel);
			int sourceIndex = SourceIndexForUnityWheel(unityIndex);
			if (sourceIndex < 0)
				return;
			bool hasContact = hasSourceWheelSurfaceContact[sourceIndex];
			Vector3 normal = hasSourceWheelContactNormal[sourceIndex]
				? sourceWheelContactNormal[sourceIndex] : sourceUpBasis;
			if (normal.sqrMagnitude <= 0.000001f)
				normal = vehicle.tr.up;
			normal.Normalize();
			Vector3 point = hasContact ? sourceWheelContactPoint[sourceIndex] : wheel.tr.position;
			Collider collider = hasContact ? sourceWheelContactCollider[sourceIndex] : null;
			if (collider && (!collider.enabled || !collider.gameObject.activeInHierarchy))
				collider = null;
			Vector3 relativeVelocity = wheel.tr.InverseTransformDirection(
				vehicle.rb.GetPointVelocity(wheel.tr.position));
			float distance = wheel.susParent.suspensionDistance * GetWheelTravelDistance(wheel);
			wheel.SetOriginalContact(contactCountdown[sourceIndex] >= 4,
				sourceProbeWheelTouched[sourceIndex], point, normal, collider, relativeVelocity,
				distance, hasContact ? sourceWheelSurfaceType[sourceIndex] : 0);
		}
		public void GetWheelSlip(int unityWheelIndex, out float forward, out float sideways)
		{
			int sourceIndex = unityWheelIndex < 2 ? unityWheelIndex + 2 : unityWheelIndex - 2;
			forward = sourceWheelSlip[sourceIndex];
			sideways = sourceWheelSideSlip[sourceIndex];
		}
		public float GetWheelGripUsage(int unityWheelIndex)
		{
			int sourceIndex = SourceIndexForUnityWheel(unityWheelIndex);
			return sourceIndex >= 0 ? sourceWheelGripUsage[sourceIndex] : 0;
		}
		int SourceIndexForUnityWheel(int unityWheelIndex)
		{
			if (unityWheelIndex < 0 || unityWheelIndex >= UnityWheelIndex.Length)
				return -1;
			return unityWheelIndex < 2 ? unityWheelIndex + 2 : unityWheelIndex - 2;
		}
		public bool ShouldEmitSourceSkidStrip(int unityWheelIndex)
		{
			int sourceIndex = SourceIndexForUnityWheel(unityWheelIndex);
			if (sourceIndex < 0 || sourceIndex >= sourceWheelGripUsage.Length || sourceContactClass != 0)
				return false;
			float usage = sourceWheelGripUsage[sourceIndex];
			// vehicle_skid_strip.cpp: slow motion starts at grip usage 2; above
			// source speed 30, the original requires 8.5. At exactly 30 neither
			// comparison rejects the strip.
			if (currentSourceSpeed > 30)
				return usage >= 8.5f;
			if (currentSourceSpeed < 30)
				return usage >= 2;
			return true;
		}
		public int ConsumeSourceTireParticleEvents(int unityWheelIndex)
		{
			int sourceIndex = SourceIndexForUnityWheel(unityWheelIndex);
			if (sourceIndex < 0 || sourceIndex >= sourceTireParticleEvents.Length)
				return 0;
			int count = sourceTireParticleEvents[sourceIndex];
			sourceTireParticleEvents[sourceIndex] = 0;
			return count;
		}
		void UpdateSourceTireParticleAccumulator()
		{
			for (int i = 0; i < sourceWheelGripUsage.Length; i++)
			{
				// contact_tire_particles.cpp updates this accumulator only while
				// source speed is below 100; the native comparison's C0 flag means
				// strictly less-than, so exactly 100 is excluded too.
				if (currentSourceSpeed >= 100)
					continue;
				float usage = sourceWheelGripUsage[i];
				float accumulated = sourceTireParticleAccumulator[i];
				if (usage < 3.5f)
				{
					double decreased = (double)accumulated - 2.0;
					accumulated = (float)decreased;
					if (decreased < 20)
						accumulated = 20;
				}
				else
				{
					double slip = System.Math.Min(System.Math.Abs((double)usage * 0.25), 1.0);
					double added = ((slip * sourceWheelSurfaceGrip[i]) * 20.0) /
						sourceWheelSurfaceRolling[i] + accumulated;
					accumulated = (float)added;
				}
				if (accumulated > 200)
				{
					sourceTireParticleEvents[i]++;
					accumulated = 175;
				}
				sourceTireParticleAccumulator[i] = accumulated;
			}
		}
		public float SourceWheelNormalSpeed(Vector3 probePosition, Vector3 normal, Vector3 groundVelocity)
		{
			normal.Normalize();
			Vector3 relativeVelocity = vehicle.rb.linearVelocity - groundVelocity;
			float centreSpeed = Vector3.Dot(relativeVelocity / SourceSpeedToMetresPerSecond, normal);
			Vector3 radial = (probePosition - vehicle.rb.position) / SourceLengthToMetres;
			// contact_solver::update_probe_velocity transforms the entire probe offset
			// by body.incremental. Its point velocity therefore contains the finite
			// per-tick displacement, not the small-angle angular-velocity estimate.
			float rotationalSpeed = Vector3.Dot(sourceIncrementalRotation * radial - radial, normal);
			return centreSpeed + rotationalSpeed;
		}
		Vector3 SourceProbeImpactVelocityChange(Vector3 normal, float sourceNormalSpeed, float restitution)
		{
			float sourceVelocityChange = -(1 + restitution) * sourceNormalSpeed / sourceProbeCount;
			return normal * (sourceVelocityChange * SourceSpeedToMetresPerSecond);
		}
		public void AddSourceEnergy(float amount)
		{
			if (amount <= 0 || parameters.fuelCapacity <= 0)
				return;
			energy = Mathf.Min(parameters.fuelCapacity, energy + amount);
		}
		public void RefillSourceEnergy()
		{
			energy = Mathf.Max(0, parameters.fuelCapacity);
		}
		public void ApplySourceRespawnEnergyCost()
		{
			if (F.I && F.I.s_raceType == RaceType.Stunt)
				return;
			float sourceCost = SourceUpgradeActive ? SourceCpuRespawnEnergyCost : SourceHumanRespawnEnergyCost;
			energy = (float)((double)energy - (double)sourceCost * parameters.fuelCapacity / SourceEnergyCapacity);
		}
		public void AddSourceStuntEnergyAward()
		{
			if (F.I && F.I.s_raceType == RaceType.TimeTrial)
				return;
			float awardPercent = SourceUpgradeActive
				? SourceCpuStuntEnergyAwardPercent : SourceHumanStuntEnergyAwardPercent;
			double amount = (double)awardPercent * SourceStuntEnergyAwardMultiplier *
				0.01 * parameters.fuelCapacity;
			energy = Mathf.Min(parameters.fuelCapacity, (float)((double)energy + amount));
		}
		public void ResetToNeutral()
		{
			gear = 1;
			sourceProbeIterationOverflow = false;
			sourceIncrementalRotation = Quaternion.identity;
			pendingContactRotation = Quaternion.identity;
			hasPendingContactRotation = false;
			sourceContactAngularStep = Vector3.zero;
			lastSourceControlledRotation = vehicle.rb.rotation;
			stepStartRotation = stepRotation = lastSourceControlledRotation;
			stepRotationChanged = false;
			hasSourceControlledRotation = true;
			sourceRotationDiagnosticLogged = false;
			sourceSuspensionDiagnosticLogged = false;
			sourceSuspensionObservationLogged = false;
			sourceSuspensionStableContactTicks = 0;
			sourceFirstSuspensionContactTime = -1;
			System.Array.Clear(sourceProbeContactIterations, 0, sourceProbeContactIterations.Length);
			sourceMassOverride = false;
			sourceSpecialMode = false;
			sourceRailControlsActive = false;
			sourceUnityRailPathThisTick = null;
			sourceRailControlDisabled = false;
			sourceRailCompletedPath = null;
			sourceRailProgress = 0;
			sourceRailEnergyAdded = 0;
			sourceRailTicks = 0;
			sourceRailThrottle = 0;
			sourceRailBrakeInput = 0;
			sourceEffectiveMass = (float)((double)fuel * parameters.fuelUnitMass + parameters.mass);
			vehicle.rb.mass = Mathf.Max(0.001f, sourceEffectiveMass * SourceMassToKilograms);
			// vehicle_stunt.cpp::reset_vehicle_motion clears motion and the stored
			// engine RPM, then vehicle_reset.cpp selects neutral without engaging a
			// new clutch transition.
			rpm = 0;
			vehicle.rb.linearVelocity = Vector3.zero;
			vehicle.rb.angularVelocity = Vector3.zero;
			throttleSignal = 0;
			downshiftTicks = 0;
			sourceContactVelocityRespawnRequested = false;
			sourceContactResetPending = false;
			sourceContactResetDiagnosticLogged = false;
			sourceContactResetReason = null;
			System.Array.Clear(controlFlags, 0, controlFlags.Length);
			System.Array.Clear(wheelSpeed, 0, wheelSpeed.Length);
			System.Array.Clear(wheelAcceleration, 0, wheelAcceleration.Length);
			System.Array.Clear(filteredContactLoad, 0, filteredContactLoad.Length);
			System.Array.Clear(sourceProbeWheelLoad, 0, sourceProbeWheelLoad.Length);
			System.Array.Clear(sourceProbeWheelTouched, 0, sourceProbeWheelTouched.Length);
			System.Array.Clear(sourceSuspension, 0, sourceSuspension.Length);
			System.Array.Clear(contactCountdown, 0, contactCountdown.Length);
			sourceRailbar = null;
			sourceRailbarSeenTime = sourceRailbarSupportedTime = float.NegativeInfinity;
			System.Array.Clear(sourceWheelSlip, 0, sourceWheelSlip.Length);
			System.Array.Clear(sourceWheelSideSlip, 0, sourceWheelSideSlip.Length);
			System.Array.Clear(sourceWheelGripUsage, 0, sourceWheelGripUsage.Length);
			System.Array.Clear(sourceTireParticleAccumulator, 0, sourceTireParticleAccumulator.Length);
			System.Array.Clear(sourceTireParticleEvents, 0, sourceTireParticleEvents.Length);
			System.Array.Clear(sourceWheelContactNormal, 0, sourceWheelContactNormal.Length);
			System.Array.Clear(hasSourceWheelContactNormal, 0, hasSourceWheelContactNormal.Length);
			System.Array.Clear(sourceWheelContactPoint, 0, sourceWheelContactPoint.Length);
			System.Array.Clear(sourceWheelContactCollider, 0, sourceWheelContactCollider.Length);
			System.Array.Clear(sourceWheelSurfaceType, 0, sourceWheelSurfaceType.Length);
			System.Array.Clear(hasSourceWheelSurfaceContact, 0, hasSourceWheelSurfaceContact.Length);
			turboRpm = turboForce = 0;
			sourceTurboActive = false;
			sourceStartBoostActiveThisTick = false;
			startBoostTicks = 0;
			performanceMultiplier = 1;
			airTicks = 0;
			stuntActive = stuntPressed = false;
			stuntPressedAt = -1;
			pitchAcceleration = yawAcceleration = 0;
			pitchSpeed = yawSpeed = 0;
			ResetSourceStuntRoll();
			sourceBodyRoll = sourceBodyPitch = sourceBodyHeave = 0;
			effectiveComHeight = parameters.comHeight;
			sourceCom2628 = sourceCom262c = 0;
			comResetTicks = 0;
			sourceContactClass = 0;
			sourceContactAngularStep = Vector3.zero;
			hasSourceControlledRotation = false;
			pendingContactRotation = Quaternion.identity;
			hasPendingContactRotation = false;
			lastVehiclePairImpulseTick = -1;
			nextVehicleCollisionHopTime = 0;
			lastSourceCollisionHoldTick = -1;
			sourceCollisionHoldTicks = 0;
			sourceContactCounter = 0;
			currentSourceSpeed = 0;
			sourceAngularVelocity = Vector3.zero;
			if (AIStuntInProgress)
				FinishAIStunt();
			sourceAiStuntMeter = sourceAiStuntInputKind = 0;
			sourceAiStuntAirborne = false;
			sourceAiStuntDeadline = 0;
			sourceAiTurboTicks = sourceAiTurboCooldown = sourceAiTurboRestTicks = 0;
			sourceAiTurboActive = false;
			sourceAiLaunchTick = 0;
			sourceAiLaunchPending = sourceAiStuntInputThisTick = false;
			ResetSourceStuntPhaseHistory();
			if (bodyTransform)
			{
				bodyTransform.localPosition = initialBodyLocalPosition;
				bodyTransform.localRotation = initialBodyLocalRotation;
			}
			foreach (Wheel wheel in vehicle.wheels)
			{
				wheel.currentRPM = 0;
				wheel.originalGripUsage = 0;
				wheel.SetOriginalContact(false, false, wheel.tr.position, sourceUpBasis,
					null, Vector3.zero, wheel.susParent.suspensionDistance *
					GetWheelTravelDistance(wheel), 0);
			}
			vehicle.upshiftPressed = false;
			vehicle.downshiftPressed = false;
			if (vehicle.engine && vehicle.engine.transmission)
				vehicle.engine.transmission.SetOriginalGear(gear, parameters);
		}
		public void ResolveBodyContact(Collision collision, bool entering = false)
		{
			// contact_response.cpp linear_friction: tangential motion is
			// cancelled below 20% of impact speed, otherwise 10% is removed. The
			// source's velocity unit is one 60 Hz tick, independent of Unity's step.
			Vector3 relative = collision.relativeVelocity / SourceSpeedToMetresPerSecond;
			for (int i = 0; i < collision.contactCount; i++)
			{
				ContactPoint contact = collision.GetContact(i);
				VehicleParent otherCar = contact.otherCollider
					? contact.otherCollider.GetComponentInParent<VehicleParent>() : null;
				// Changing collider layers does not gate the manual source solver;
				// honor ghost protection also for queued/kinematic body callbacks.
				if (otherCar && ((vehicle.ghost && !vehicle.ghost.hittable) ||
					(otherCar.ghost && !otherCar.ghost.hittable)))
					continue;
				// Track probes already resolve these static colliders in FilterWheelContacts.
				// Applying the callback response too adds a second impact impulse and angular
				// rotation on each landing. Keep this path for static obstacles outside that
				// source query and for dynamic vehicle contacts.
				if (contact.thisCollider.GetComponentInParent<Wheel>())
					continue;
				Vector3 normal = contact.normal.normalized;
				float centreNormalSpeed = Vector3.Dot(relative, normal);
				Rigidbody other = contact.otherCollider ? contact.otherCollider.attachedRigidbody : null;
				bool staticContact = !other || other.isKinematic;
				if (staticContact && contact.otherCollider &&
					SourceTrackColliderIds.Contains(contact.otherCollider.GetInstanceID()))
					continue;
				Vector3 contactRadial = contact.point - vehicle.rb.position;
				float rotationNormalSpeed = Vector3.Dot(
					Vector3.Cross(sourceContactAngularStep, contactRadial) / SourceLengthToMetres, normal);
				float normalSpeed = centreNormalSpeed + rotationNormalSpeed;
				if (staticContact)
				{
					float sourceGravity = SourceGravityMagnitude;
					ApplyAngularImpactResponse(contact, normal, normalSpeed, sourceGravity);
					ApplyAngularContactFriction(contact, normal, normalSpeed);
					if (entering && normalSpeed < 0)
					{
						Vector3 radial = vehicle.rb.position - contact.point;
						float restitution = OriginalContactPhysics.EffectiveRestitution(normalSpeed, normal,
							radial, sourceGravity);
						float beforeImpactSpeed = normalSpeed * SourceSpeedToMetresPerSecond;
						float afterImpactSpeed = Vector3.Dot(vehicle.rb.GetPointVelocity(contact.point), normal);
						float targetSpeed = -beforeImpactSpeed * restitution;
						vehicle.rb.linearVelocity += normal * (targetSpeed - afterImpactSpeed);
					}
				}
				else
				{
					// Vehicle pairs use contact_effects.cpp's center-to-center pair
					// impulse, not the track probe friction/restitution path.
					continue;
				}
				float strength = Mathf.Min(50f, Mathf.Abs(normalSpeed));
				Vector3 tangent = relative - normal * centreNormalSpeed;
				float length = tangent.magnitude;
				if (length <= 0.001f || strength <= 0)
					continue;
				float removed = length > strength * 0.2f ? strength * 0.1f : length;
				vehicle.rb.linearVelocity -= tangent / length *
					(removed * SourceSpeedToMetresPerSecond);
			}
			// The source keeps angular motion in its orientation matrix and does not
			// leave PhysX's contact torque running alongside the recovered response.
			if (hasSourceControlledRotation)
				vehicle.rb.rotation = lastSourceControlledRotation;
			vehicle.rb.angularVelocity = Vector3.zero;
		}
		void ResolveSourceVehiclePair()
		{
			if (!F.I || F.I.s_raceType == RaceType.TimeTrial || !vehicle.gameObject.activeInHierarchy ||
				!vehicle.raceBox || !vehicle.raceBox.enabled || (vehicle.ghost && !vehicle.ghost.hittable))
				return;
			int tick = Mathf.RoundToInt(Time.fixedTime / Mathf.Max(0.0001f, Time.fixedDeltaTime));
			float tickScale = Time.fixedDeltaTime * SourceTicksPerSecond;
			if (sourceVehiclePairSnapshotTick != tick)
			{
				SourceVehiclePairPositions.Clear();
				foreach (VehicleParent car in F.I.s_cars)
					if (car && car.originalVehiclePhysics != null && car.rb && car.gameObject.activeInHierarchy)
					{
						car.originalVehiclePhysics.AdvanceSourceCollisionHold(tick, tickScale);
						SourceVehiclePairPositions[car] = car.rb.position;
					}
				sourceVehiclePairSnapshotTick = tick;
			}
			if (lastVehiclePairImpulseTick == tick || sourceCollisionHoldTicks > 0)
				return;
			for (int i = 0; i < F.I.s_cars.Count; i++)
			{
				VehicleParent otherVehicle = F.I.s_cars[i];
				if (!otherVehicle || otherVehicle == vehicle || otherVehicle.originalVehiclePhysics == null || !otherVehicle.rb ||
					!otherVehicle.gameObject.activeInHierarchy || otherVehicle.rb.isKinematic ||
					!otherVehicle.raceBox || !otherVehicle.raceBox.enabled ||
					(otherVehicle.ghost && !otherVehicle.ghost.hittable))
					continue;
				if (!SourceVehiclePairPositions.TryGetValue(vehicle, out Vector3 thisPreviousPosition) ||
					!SourceVehiclePairPositions.TryGetValue(otherVehicle, out Vector3 otherPreviousPosition))
					continue;
				Vector3 previousSeparation = thisPreviousPosition - otherPreviousPosition;
				if (previousSeparation.sqrMagnitude >= 9)
					continue;
				if (otherVehicle.originalVehiclePhysics.sourceCollisionHoldTicks > 0)
				{
					if (otherVehicle.originalVehiclePhysics.sourceCollisionHoldTicks < 10)
						otherVehicle.originalVehiclePhysics.sourceCollisionHoldTicks += 60;
					continue;
				}
				Vector3 separation = vehicle.rb.position - otherVehicle.rb.position;
				float distance = separation.magnitude / SourceLengthToMetres;
				lastVehiclePairImpulseTick = tick;
				// contact_effects.cpp applies this center-to-center overlap impulse
				// to the first selected partner using previous-position proximity.
				// A pair can be visited again when the other car reaches its own source
				// slot, so do not impose a Unity instance-ID ordering here.
				Vector3 normal = separation.sqrMagnitude > 0 ? separation.normalized : Vector3.zero;
				float normalSpeed = Vector3.Dot(
					otherVehicle.rb.linearVelocity - vehicle.rb.linearVelocity, normal) /
					SourceSpeedToMetresPerSecond;
				float overlap = (300 - distance) * 0.0033333334f;
				float firstMass = Mathf.Max(1, parameters.mass + fuel * parameters.fuelUnitMass);
				float secondMass = Mathf.Max(1, otherVehicle.rb.mass / SourceMassToKilograms);
				float inverseTotalMass = 1 / (firstMass + secondMass);
				float firstImpulse = overlap * secondMass * inverseTotalMass * 25;
				float secondImpulse = -overlap * firstMass * inverseTotalMass * 25;
				Vector3 firstVelocityChange = normal * firstImpulse;
				Vector3 secondVelocityChange = normal * secondImpulse;
				// contact_effects.cpp::apply_pair_impulse attenuates the vertical
				// component to half strength while leaving horizontal response intact.
				firstVelocityChange.y *= 0.5f;
				secondVelocityChange.y *= 0.5f;
				vehicle.rb.linearVelocity += firstVelocityChange * SourceSpeedToMetresPerSecond;
				otherVehicle.rb.linearVelocity += secondVelocityChange * SourceSpeedToMetresPerSecond;
				ApplyVehicleCollisionHop(otherVehicle.originalVehiclePhysics, normalSpeed);
				ApplySourceImpactEnergy(normalSpeed);
				effectiveComHeight = parameters.comHeight;
				sourceCom2628 = sourceCom262c = 0;
				comResetTicks = -12;
				return;
			}
		}
		void ApplyVehicleCollisionHop(OriginalVehiclePhysics other, float sourceClosingSpeed)
		{
			// The recovered pair impulse has no upward component for equal-height
			// centers. Add a small hop to reproduce the visible collision response
			// on Unity tracks, separately from the retail horizontal impulse.
			float closingSpeed = sourceClosingSpeed * SourceSpeedToMetresPerSecond;
			if (CountDownSeq.Countdown > 0 || closingSpeed < 0.5f ||
				Time.fixedTime < nextVehicleCollisionHopTime ||
				Time.fixedTime < other.nextVehicleCollisionHopTime)
				return;
			bool supported = false;
			for (int i = 0; i < 4; i++)
				if (contactCountdown[i] >= 4 || other.contactCountdown[i] >= 4)
				{
					supported = true;
					break;
				}
			if (!supported)
				return;
			// Both source slots can process the pair; debounce the hop for both
			// cars, so one impact cannot launch them twice or chatter while rubbing.
			nextVehicleCollisionHopTime = other.nextVehicleCollisionHopTime =
				Time.fixedTime + 8f / SourceTicksPerSecond;
			float height = Mathf.Lerp(0.03f, 0.07f, Mathf.Clamp01(closingSpeed / 12));
			ApplyCollisionHopVelocity(height);
			other.ApplyCollisionHopVelocity(height);
		}
		void ApplyCollisionHopVelocity(float height)
		{
			Vector3 up = SourceWorldUp;
			float speedMetres = currentSourceSpeed * SourceSpeedToMetresPerSecond;
			float downforce = Mathf.Max(0, speedMetres * speedMetres * sourceAirFactor *
				parameters.frontalArea * parameters.liftCoefficient * 0.615f);
			float acceleration = Physics.gravity.magnitude + downforce /
				(Mathf.Max(1, sourceEffectiveMass) * 4) * SourceSpeedToMetresPerSecond * SourceTicksPerSecond;
			// Compensate for active downforce so a fast, light car still hops a few
			// centimetres. Limit the speed to keep the response small at any speed.
			float upwardSpeed = Mathf.Min(3.5f, Mathf.Sqrt(2 * acceleration * height));
			float existingUpwardSpeed = Vector3.Dot(vehicle.rb.linearVelocity, up);
			vehicle.rb.linearVelocity += up * Mathf.Max(0, upwardSpeed - existingUpwardSpeed);
		}
		void ApplySourceImpactEnergy(float strength)
		{
			if (sourceImpactEnergyLossThisTick > SourceCollisionEnergyLimit)
				return;
			float amount = Mathf.Abs(strength) * SourceCollisionEnergyScale;
			if (amount < SourceCollisionEnergyMinimum)
				return;
			amount = Mathf.Min(amount, SourceCollisionEnergyMaximum);
			if (SourceUpgradeActive)
				amount = (float)(((1.0 - SourceUpgradeCondition) * SourceCpuCollisionEnergyModifier + 1.0) * amount);
			energy = Mathf.Max(0, energy - amount);
			sourceImpactEnergyLossThisTick += amount;
		}
		void AdvanceSourceCollisionHold(int tick, float tickScale)
		{
			if (lastSourceCollisionHoldTick == tick)
				return;
			sourceCollisionHoldTicks = Mathf.Max(0, sourceCollisionHoldTicks - tickScale);
			lastSourceCollisionHoldTick = tick;
		}
		public void SetStuntButton(int pressed)
		{
			if (pressed != 0)
			{
				if (!stuntPressed)
				{
					stuntPressedAt = Time.time;
					stuntPressed = true;
				}
			}
			else
				stuntPressed = false;
		}
		public void CancelStunt()
		{
			if (AIStuntInProgress)
				FinishAIStunt();
			sourceAiStuntMeter = 0;
			sourceAiLaunchPending = sourceAiStuntInputThisTick = false;
			stuntActive = false;
			stuntPressed = false;
			stuntPressedAt = -1;
			pitchAcceleration = yawAcceleration = 0;
			pitchSpeed = yawSpeed = 0;
			ResetSourceStuntRoll();
			ResetSourceStuntPhaseHistory();
		}
		void ResetSourceStuntRoll()
		{
			rollAcceleration = rollSpeed = stuntRollProgress = 0;
			stuntRollDirection = 0;
			stuntRollActive = false;
			stuntRollInputArmed = true;
		}
		bool TryLaunchOnTakeoff(int tick)
		{
			float strength;
			if (sourceAiLaunchPending)
			{
				sourceAiLaunchPending = false;
				float elapsedTicks = Mathf.Abs(unchecked((int)(sourceAiLaunchTick - (uint)tick)));
				float windowTicks = Mathf.Max(parameters.launchTime * SourceTicksPerSecond, 0.001f);
				if (elapsedTicks >= windowTicks)
					return false;
				float adjustedTicks = Mathf.Max(0, elapsedTicks - parameters.launchTolerance * SourceTicksPerSecond);
				strength = Mathf.Clamp01((windowTicks - adjustedTicks) / windowTicks);
			}
			else
			{
				if (stuntPressedAt < 0)
					return false;
				float elapsed = Time.time - stuntPressedAt;
				stuntPressedAt = -1;
				// 004116f0: the stunt key is timed against actual takeoff.
				float window = Mathf.Max(parameters.launchTime, 0.001f);
				if (elapsed >= window)
					return false;
				float adjusted = Mathf.Max(0, elapsed - parameters.launchTolerance);
				strength = Mathf.Clamp01((window - adjusted) / window);
			}
			float effectiveMass = Mathf.Max(1, parameters.mass + fuel * parameters.fuelUnitMass);
			float lift = Mathf.Min(25, strength * strength * parameters.launchSpeed / effectiveMass);
			vehicle.rb.linearVelocity += SourceWorldUp * (lift * SourceSpeedToMetresPerSecond);
			stuntActive = lift > 0;
			airTicks = 0;
			return stuntActive;
		}
		public void Step()
		{
			hasGroundSurfaceContactThisTick = false;
			groundSurfaceContactNormalSumThisTick = Vector3.zero;
			System.Array.Clear(sourceWheelGroundSurfaceContactThisTick, 0,
				sourceWheelGroundSurfaceContactThisTick.Length);
			if (vehicle.wheels == null || vehicle.wheels.Length < 4 || vehicle.rb.isKinematic)
				return;
			UpdateSourceWheelProbeGeometry();
			int tick = Mathf.RoundToInt(Time.fixedTime / Mathf.Max(0.0001f, Time.fixedDeltaTime));
			ResetSourceAirFactorsForTick(tick);
			float tickScale = Time.fixedDeltaTime * SourceTicksPerSecond;
			sourceRailControlsActive = false;
			sourceUnityRailPathThisTick = null;
			// contact::solver clears its accumulated impact loss once before the
			// contact pass, so the cap applies only to impacts within this source tick.
			sourceImpactEnergyLossThisTick = 0;
			vehicle.rb.mass = Mathf.Max(0.001f, sourceEffectiveMass * SourceMassToKilograms);
			vehicle.rb.centerOfMass = Vector3.zero;
			using (SourceCollisionExclusionMarker.Auto())
				ApplySourceTrackCollisionExclusions();
			using (SourceVehiclePairMarker.Auto())
				ResolveSourceVehiclePair();
			HoldSourceStartVehicle(tickScale);
			Vector3 contactStartPosition = vehicle.rb.position;
			Vector3 contactStartVelocity = vehicle.rb.linearVelocity;
			// contact_solver::tick advances the source body by its incoming velocity
			// before sweeping the probes. Unity normally integrates after FixedUpdate,
			// so stage that same displacement here and remove Unity's later duplicate.
			vehicle.rb.position += vehicle.rb.linearVelocity * Time.fixedDeltaTime;
			Quaternion startRotation = vehicle.rb.rotation;
			// contact_solver first premultiplies the saved incremental matrix onto
			// the body orientation. Contact responses then update that matrix for the
			// following source tick; dynamics rebuilds the body basis separately.
			stepStartRotation = sourceIncrementalRotation * startRotation;
			float appliedContactIncrementDegrees = Quaternion.Angle(startRotation, stepStartRotation);
			Vector3 appliedContactRotationVector = RotationVector(sourceIncrementalRotation);
			stepRotation = stepStartRotation;
			stepRotationChanged = false;
			sourceContactAngularStep = RotationVector(sourceIncrementalRotation);
			// The project has Auto Sync Transforms enabled. Each physics query in
			// the probe loop would otherwise synchronize every recently moved wheel
			// and vehicle again. The track query mask excludes vehicle colliders, so
			// sync the current scene once before the batch and keep auto-sync off until
			// its queries finish. Restore the project's setting even if a query fails.
			bool restoreAutoSyncTransforms = Physics.autoSyncTransforms;
			Physics.autoSyncTransforms = false;
			try
			{
				using (SourceWheelContactMarker.Auto())
				{
					Physics.SyncTransforms();
					FilterWheelContacts(tickScale, contactStartPosition, contactStartVelocity);
				}
			}
			finally
			{
				Physics.autoSyncTransforms = restoreAutoSyncTransforms;
			}
			float pendingContactRotationDegrees = hasPendingContactRotation
				? Quaternion.Angle(Quaternion.identity, pendingContactRotation) : 0;
			Vector3 pendingContactRotationVector = hasPendingContactRotation
				? RotationVector(pendingContactRotation) : Vector3.zero;
			if (sourceProbeIterationOverflow)
			{
				// contact_solver::step returns before unpack_pose on its probe-attempt
				// limit. Its staged body pose, velocity delta, and incremental rotation
				// are scratch state; keep the incoming source pose for vehicle dynamics.
				stepRotation = startRotation;
				pendingContactRotation = Quaternion.identity;
				hasPendingContactRotation = false;
			}
			else if (hasPendingContactRotation)
			{
				LimitSourceContactOvercorrection();
				sourceIncrementalRotation = (pendingContactRotation * sourceIncrementalRotation).normalized;
			}
			// Diagnostics should report the correction actually applied by the solve.
			pendingContactRotationVector = hasPendingContactRotation
				? RotationVector(pendingContactRotation) : Vector3.zero;
			pendingContactRotationDegrees = hasPendingContactRotation
				? Quaternion.Angle(Quaternion.identity, pendingContactRotation) : 0;
			pendingContactRotation = Quaternion.identity;
			hasPendingContactRotation = false;
			if (!sourceProbeIterationOverflow)
				StabilizeSourceIncrementalRotation();
			vehicle.engine.transmission.SetOriginalGear(gear, parameters);
			PrepareSourcePhysicalParameters();
			UpdateSourceAIStunt(tick);
			// Spline proximity alone must not take control from a player. FollowAI's
			// pit path is assigned only by an explicit pit-entry/auto-drive trigger.
			PathCreator assignedPitPath = vehicle.followAI ? vehicle.followAI.PitsPathCreator : null;
			bool unityRailNear = TryFindSourceUnityRail(vehicle.rb.position, assignedPitPath,
				out EnergyTunnelPath unityRail, out _, out float unityRailDistanceSqr);
			if (sourceRailCompletedPath && (unityRail != sourceRailCompletedPath || unityRailDistanceSqr > 36))
				sourceRailCompletedPath = null;
			if (unityRailNear && unityRail != sourceRailCompletedPath && CountDownSeq.Countdown <= 0)
			{
				if (!sourceSpecialMode)
				{
					sourceRailProgress = 0;
					sourceRailEnergyAdded = 0;
					sourceRailTicks = 0;
				}
				sourceSpecialMode = true;
				sourceRailControlsActive = true;
				sourceUnityRailPathThisTick = unityRail;
			}
			else if (sourceRailControlDisabled && !unityRailNear)
			{
				// Leaving a Unity-authored pit path releases the automatic rail controls.
				sourceRailControlDisabled = false;
				sourceSpecialMode = false;
			}
			int grounded = sourceContactCounter > 0 ? 1 : 0;
			using (SourcePowertrainMarker.Auto())
			{
				UpdateSourceControlFlags(grounded);
				UpdateSourceBraking(tickScale, grounded);
				UpdateSteering(tickScale);
				UpdateSourceRail(tickScale);
				UpdateSourceUpgradeCondition();
				UpdateSourcePerformance();
				UpdateSourceStartBoost(tickScale);
				UpdateSourceAITurbo();
				UpdateSourceCenterOfMass();
				// The retail tick keeps the prior support basis through input, rail,
				// progression and COM handling, then refreshes it immediately before
				// approach_body_speed, gravity and vehicle_dynamics_core.
				UpdateSourceSupportBasis(stepRotation);
				ApproachSourceBodySpeed(currentSourceSpeed, sourceContactClass, tickScale);
				ApplySourceGravity();
				UpdateEngine(currentSourceSpeed, grounded, sourceContactClass, tickScale);
			}
			int dynamicsContactClass = ClassifySourceContacts();
			Quaternion incrementalAfterContact = sourceIncrementalRotation;
			using (SourceTyreMarker.Auto())
				ApplyTyres(tickScale, dynamicsContactClass);
			Vector3 tyreDynamicsIncrementVector = RotationVector(
				sourceIncrementalRotation * Quaternion.Inverse(incrementalAfterContact));
			using (SourceTyreMarker.Auto())
				UpdateSourceTireParticleAccumulator();
			ApplyAerodynamics(tickScale, dynamicsContactClass);
			using (SourceSuspensionMarker.Auto())
			{
				ApplySourceSuspensionTick(tickScale);
				AttenuateSourceSteering(tickScale);
				UpdateOriginalSuspensionPose(currentSourceSpeed, tickScale);
			}
			LogSourceSuspensionStateAfterStableContact();
			Quaternion incrementalBeforeAirMotion = sourceIncrementalRotation;
			using (SourceAirMotionMarker.Auto())
				UpdateAirMotion(tickScale);
			ApplyRailbarSupport();
			Vector3 airIncrementVector = RotationVector(
				sourceIncrementalRotation * Quaternion.Inverse(incrementalBeforeAirMotion));
			LogFormulaStability(appliedContactRotationVector, pendingContactRotationVector,
				tyreDynamicsIncrementVector, airIncrementVector);
			float dynamicsRotationDegrees = Quaternion.Angle(stepStartRotation, stepRotation);
			Vector3 dynamicsRotationVector = RotationVector(stepRotation * Quaternion.Inverse(stepStartRotation));
			bool hasSourceProbeContactThisTick = false;
			for (int i = 0; i < sourceProbeCount; i++)
				if (sourceProbeHitsThisTick[i] > 0)
				{
					hasSourceProbeContactThisTick = true;
					break;
				}
			if (!sourceRotationDiagnosticLogged && hasSourceProbeContactThisTick)
			{
				string probeDetails = string.Empty;
				string probeEnds = string.Empty;
				for (int i = 0; i < sourceProbeCount; i++)
				{
					probeEnds += (probeEnds.Length == 0 ? string.Empty : "; ") +
						$"p{i}={previousSourceProbeWorld[i].ToString("F2")}";
					if (sourceProbeHitsThisTick[i] > 0)
						probeDetails += (probeDetails.Length == 0 ? string.Empty : "; ") +
							$"p{i}: hits={sourceProbeHitsThisTick[i]}, streak={sourceProbeContactIterations[i]}, " +
							$"firstNormal={sourceProbeFirstContactNormal[i].ToString("F2")}, " +
							$"firstVn={sourceProbeFirstContactSpeed[i]:F2}, " +
							$"surface={sourceProbeFirstContactPoint[i].ToString("F2")}, " +
							$"center={sourceProbeFirstContactCenter[i].ToString("F2")}";
				}
				string wheelRotationDetails =
					$"w0={sourceWheelRotationDeltaThisTick[0].ToString("F3")}, " +
					$"w1={sourceWheelRotationDeltaThisTick[1].ToString("F3")}, " +
					$"w2={sourceWheelRotationDeltaThisTick[2].ToString("F3")}, " +
					$"w3={sourceWheelRotationDeltaThisTick[3].ToString("F3")}";
				string wheelGeometry = string.Empty;
				for (int i = 0; i < 4; i++)
				{
					Wheel wheel = vehicle.wheels[UnityWheelIndex[i]];
					if (!wheel || !wheel.susParent || !wheel.rim)
						continue;
					Vector3 anchorLocal = vehicle.tr.InverseTransformPoint(wheel.susParent.tr.position);
					Vector3 rimLocal = vehicle.tr.InverseTransformPoint(wheel.rim.position);
					wheelGeometry += (wheelGeometry.Length == 0 ? string.Empty : "; ") +
						$"p{i}->u{UnityWheelIndex[i]}:{wheel.name}, front={wheel.isFront}, " +
						$"probeR={sourceProbeRadius[i] * SourceLengthToMetres:F3}, " +
						$"tireR={wheel.actualRadius:F3}/{wheel.tireRadius:F3}, " +
						$"cfgR={parameters.tyres[i].radius * SourceLengthToMetres:F3}, " +
						$"probeLocal={sourceProbeLocal[i].ToString("F3")}, " +
						$"anchor={anchorLocal.ToString("F3")}, rim={rimLocal.ToString("F3")}";
				}
				//Debug.LogWarning($"[OriginalVehiclePhysics] Rotation stages: car={vehicle.carConfig?.name}, source={parameters.sourceConfig}, appliedIncremental={appliedContactIncrementDegrees:F2} deg rotVecRad={appliedContactRotationVector.ToString("F2")}, newContact={pendingContactRotationDegrees:F2} deg rotVecRad={pendingContactRotationVector.ToString("F2")}, contactVelocity={sourceContactVelocityBeforeThisTick.ToString("F2")} -> {sourceContactVelocityAfterThisTick.ToString("F2")} m/s, delta={sourceContactVelocityDeltaThisTick.ToString("F2")} m/s, tyreDynamicsIncrement={tyreDynamicsIncrementVector.ToString("F2")} rad ({wheelRotationDetails}, accelTilt={sourceAccelerationTiltDeltaThisTick.ToString("F3")} rad), airIncrement={airIncrementVector.ToString("F2")} rad, axlePose={dynamicsRotationDegrees:F2} deg rotVecRad={dynamicsRotationVector.ToString("F2")}, tickHits={sourceProbeHitsThisTick[0]},{sourceProbeHitsThisTick[1]},{sourceProbeHitsThisTick[2]},{sourceProbeHitsThisTick[3]},{(sourceProbeCount > 4 ? sourceProbeHitsThisTick[4] : 0)}, streak={sourceProbeContactIterations[0]},{sourceProbeContactIterations[1]},{sourceProbeContactIterations[2]},{sourceProbeContactIterations[3]},{(sourceProbeCount > 4 ? sourceProbeContactIterations[4] : 0)}, contacts={sourceContactCounter}, bodyPos={vehicle.rb.position.ToString("F2")}, bodyEuler={stepStartRotation.eulerAngles.ToString("F1")}, resultEuler={stepRotation.eulerAngles.ToString("F1")}; probeEnds=[{probeEnds}]; {probeDetails}; wheelGeometry=[{wheelGeometry}]", vehicle);
				sourceRotationDiagnosticLogged = true;
			}
			UpdateSourcePositionHistoryAndWake(tickScale);
			UpdateSourceIdleSuspension(dynamicsContactClass);
			if (stepRotationChanged)
				vehicle.rb.MoveRotation(stepRotation);
			Quaternion appliedRotation = stepRotationChanged ? stepRotation : startRotation;
			Quaternion rotationDelta = appliedRotation * Quaternion.Inverse(startRotation);
			rotationDelta.ToAngleAxis(out float rotationDegrees, out Vector3 rotationAxis);
			if (rotationDegrees > 180)
			{
				rotationDegrees = 360 - rotationDegrees;
				rotationAxis = -rotationAxis;
			}
			sourceAngularVelocity = Time.fixedDeltaTime > 0
				? rotationAxis * (rotationDegrees * Mathf.Deg2Rad / Time.fixedDeltaTime)
				: Vector3.zero;
			lastSourceControlledRotation = stepRotationChanged ? stepRotation : vehicle.rb.rotation;
			hasSourceControlledRotation = true;
			vehicle.rb.angularVelocity = Vector3.zero;
			sourceContactAngularStep = RotationVector(sourceIncrementalRotation);
			// Let the following PhysX step integrate the new source velocity while
			// keeping this tick's position at incomingVelocity * dt, as in retail.
			vehicle.rb.position -= vehicle.rb.linearVelocity * Time.fixedDeltaTime;
			sourceContactClass = dynamicsContactClass;
			bool contactIterationReset = sourceProbeIterationOverflow;
			bool contactVelocityReset = sourceContactVelocityRespawnRequested;
			if (contactIterationReset || contactVelocityReset)
			{
				sourceContactResetReason = contactIterationReset
					? "probe iteration overflow"
					: "contact velocity delta exceeded source limit";
				sourceContactResetPending = true;
			}
			sourceProbeIterationOverflow = false;
			sourceContactVelocityRespawnRequested = false;
			if (sourceContactResetPending && SourceRequestedResetsEnabled)
			{
				if (!sourceContactResetDiagnosticLogged)
				{
					Debug.LogWarning($"[OriginalVehiclePhysics] Reset requested by source contact safety: " +
						$"reason={sourceContactResetReason ?? "unspecified"}, " +
						$"probe iteration overflow={contactIterationReset}, velocity-delta threshold={contactVelocityReset}, " +
						$"unsafe contact ticks={sourceUnsafeContactTicks}, opposing-direction ticks={sourceOpposingDirectionTicks}, " +
						$"surface contact={hasGroundSurfaceContactThisTick}, contacts={sourceContactCounter}, " +
						$"velocity={vehicle.rb.linearVelocity}, track={F.I?.s_trackName ?? "<unknown>"}, " +
						$"lastUnsafeProbe={sourceLastUnsafeProbe}, flags=0x{sourceLastUnsafeFlags:X2}, " +
						$"cause={sourceLastUnsafeCause ?? "<unknown>"}, contactSource=Unity map, " +
						$"alignment={sourceLastUnsafeAlignment:F3}, speed={currentSourceSpeed:F2}, " +
						$"normal={sourceLastUnsafeNormal.ToString("F2")}, point={sourceLastUnsafePoint.ToString("F2")}, " +
						$"collider={sourceLastUnsafeCollider ?? "<none>"}, layer={sourceLastUnsafeLayer}", vehicle);
					sourceContactResetDiagnosticLogged = true;
				}
				vehicle.ResetOnTrack();
				if (CountDownSeq.Countdown <= 0)
				{
					sourceContactResetPending = false;
					sourceContactResetDiagnosticLogged = false;
					sourceContactResetReason = null;
				}
			}
		}
		void LogFormulaStability(Vector3 appliedContact, Vector3 newContact, Vector3 tyreRotation,
			Vector3 airRotation)
		{
			// Bounded diagnostic for the remaining Formula 17 oscillation: build
			// strings only when emitting, at most twelve entries per spawn/reset.
			if (vehicle.carNumber != 18 || sourceStabilityDiagnosticSamples >= 12 ||
				Time.fixedTime < nextSourceStabilityDiagnosticTime || sourceContactCounter <= 0)
				return;
			nextSourceStabilityDiagnosticTime = Time.fixedTime + 1;
			sourceStabilityDiagnosticSamples++;
			int support = 0;
			string wheels = string.Empty;
			for (int i = 0; i < 4; i++)
			{
				if (sourceProbeWheelTouched[i])
					support++;
				Vector3 probe = SourceProbeWorldPosition(i, stepStartRotation);
				float gap = hasSourceWheelContactNormal[i]
					? Vector3.Dot(probe - sourceWheelContactPoint[i], sourceWheelContactNormal[i]) -
						sourceProbeRadius[i] * SourceLengthToMetres : float.NaN;
				wheels += $" w{i}: hit={sourceProbeHitsThisTick[i]}, streak={sourceProbeContactIterations[i]}, " +
					$"gap={gap:F4}m, impact={sourceProbeFirstContactSpeed[i]:F3}, " +
					$"load={sourceProbeWheelLoad[i]:F2}/{filteredContactLoad[i]:F2}, bump={sourceSuspension[i]:F2};";
			}
			double speedMetres = (double)currentSourceSpeed * SourceSpeedToMetresPerSecond;
			double downforce = speedMetres * speedMetres * sourceAirFactor * parameters.frontalArea *
				parameters.liftCoefficient * 0.615;
			//Debug.Log($"[OriginalVehiclePhysics] Formula17 stability {sourceStabilityDiagnosticSamples}/12: " +
			//	$"source={parameters.sourceConfig}, support={support}/4, class={sourceContactClass}, " +
			//	$"speed={currentSourceSpeed:F2}, rootY={vehicle.rb.position.y:F4}, " +
			//	$"velocity={vehicle.rb.linearVelocity.ToString("F3")}, bodyHeave={sourceBodyHeave:F2}, " +
			//	$"downforce={downforce:F2}, mass={sourceEffectiveMass:F2}, " +
			//	$"appliedRad={appliedContact.ToString("F4")}, contactRad={newContact.ToString("F4")}, " +
			//	$"tyreRad={tyreRotation.ToString("F4")}, airRad={airRotation.ToString("F4")}, " +
			//	$"bodyHits={(sourceProbeCount > 4 ? sourceProbeHitsThisTick[4] : 0)}, " +
			//	$"bodyImpact={(sourceProbeCount > 4 ? sourceProbeFirstContactSpeed[4] : 0):F3};{wheels}", vehicle);
		}
		void ApplySourceGravity()
		{
			// vehicle.cpp apply_gravity returns when source speed squared exceeds
			// 27777.777, equivalent to 100 m/s with the recovered unit conversion.
			if (vehicle.rb.linearVelocity.sqrMagnitude > 10000)
				return;
			vehicle.rb.linearVelocity += SourceGravityAcceleration * Time.fixedDeltaTime;
		}
		int ClassifySourceContacts()
		{
			// vehicle.cpp classifies by the four contact grace counters: zero
			// expired wheels is class 0, all four is class 1, and a partial set
			// is class 2. Class 1 is the fully airborne state.
			int expired = 0;
			for (int i = 0; i < contactCountdown.Length; i++)
				if (contactCountdown[i] < 4)
					expired++;
			return expired == 0 ? 0 : expired == 4 ? 1 : 2;
		}
		void ApproachSourceBodySpeed(float sourceSpeed, int sourceContactClass, float tickScale)
		{
			// vehicle.cpp approach_body_speed eases all tire speeds halfway toward
			// chassis speed whenever the contact class is partial or airborne.
			if (sourceContactClass == 0)
				return;
			float blend = 1 - Mathf.Pow(0.5f, tickScale);
			for (int i = 0; i < wheelSpeed.Length; i++)
				wheelSpeed[i] += (sourceSpeed - wheelSpeed[i]) * blend;
		}
		void UpdateSourceCenterOfMass()
		{
			// vehicle_tick.cpp update_vehicle_com reacts to a lost or uneven axle by
			// temporarily moving the effective COM height, then eases it back.
			if (F.I && F.I.s_raceType == RaceType.Stunt)
				return; // vehicle_tick_composition skips this stage in source game mode 3.
			if ((contactCountdown[2] <= 0 && contactCountdown[3] <= 0 &&
				contactCountdown[0] > 2 && contactCountdown[1] > 2) ||
				(contactCountdown[0] <= 0 && contactCountdown[2] <= 0 &&
				contactCountdown[1] > 2 && contactCountdown[3] > 2) ||
				(contactCountdown[1] <= 0 && contactCountdown[3] <= 0 &&
				contactCountdown[0] > 2 && contactCountdown[2] > 2))
			{
				sourceCom262c = 500;
				comResetTicks = 24;
			}
			int unsupported = 0;
			int supported = 0;
			for (int i = 0; i < contactCountdown.Length; i++)
			{
				if (contactCountdown[i] <= 0)
					unsupported++;
				if (contactCountdown[i] > 2)
					supported++;
			}
			if (unsupported == 4 || supported >= 3)
			{
				effectiveComHeight = parameters.comHeight;
				sourceCom2628 = sourceCom262c = 0;
				comResetTicks = 0;
				return;
			}
			uint timer = unchecked((uint)comResetTicks);
			if (timer == 0)
				return;
			comResetTicks = unchecked((int)(timer - 1u));
			Vector3 unitGravity = SourceGravityDirection;
			double gravityAlongUp = ((double)unitGravity.z * sourceUpBasis.z +
				(double)unitGravity.y * sourceUpBasis.y) + (double)unitGravity.x * sourceUpBasis.x;
			double gravitySquare = gravityAlongUp * gravityAlongUp;
			if (gravityAlongUp < 0)
				gravitySquare = -gravitySquare;
			double target = (gravitySquare + 1.0f) * sourceCom262c;
			double delta = target - sourceCom2628;
			if (delta < 0.01f)
				delta = 0.01f;
			else if (delta > 1.0f)
				delta = 1.0f;
			effectiveComHeight = (float)(parameters.comHeight - target * delta);
			sourceCom2628 = (float)target;
		}
		static Vector3 RotationVector(Quaternion rotation)
		{
			rotation.ToAngleAxis(out float angleDegrees, out Vector3 axis);
			if (angleDegrees > 180)
			{
				angleDegrees = 360 - angleDegrees;
				axis = -axis;
			}
			return axis * (angleDegrees * Mathf.Deg2Rad);
		}
		void ApplyAngularImpactResponse(ContactPoint contact, Vector3 normal, float normalSpeed, float sourceGravity)
		{
			ApplyAngularImpactResponse(contact.point, normal, normalSpeed, sourceGravity, vehicle.rb.rotation);
		}
		void ApplyAngularImpactResponse(Vector3 contactPoint, Vector3 normal, float normalSpeed, float sourceGravity)
		{
			ApplyAngularImpactResponse(contactPoint, normal, normalSpeed, sourceGravity, stepStartRotation);
		}
		void ApplyAngularImpactResponse(Vector3 contactPoint, Vector3 normal, float normalSpeed,
			float sourceGravity, Quaternion contactOrientation, bool accumulateResponse = false)
		{
			// track_contact.cpp angular_response turns the tangential component of
			// a probe's normal impact velocity into an inertia-weighted body rotation.
			Vector3 radial = vehicle.rb.position - contactPoint;
			// The source skips angular accumulation only below 1e-5 source units;
			// convert that length threshold to metres before comparing squared values.
			if (radial.sqrMagnitude < 1e-14f)
				return;
			radial.Normalize();
			float restitution = OriginalContactPhysics.BaseRestitution(normalSpeed, sourceGravity);
			float sourceMass = sourceEffectiveMass;
			float probeMass = sourceMass * 0.25f;
			Vector3 localRadial = Quaternion.Inverse(contactOrientation) * radial;
			Vector3 inertiaRadial = (contactOrientation *
				new Vector3(localRadial.x, localRadial.y * 0.5f, localRadial.z * 0.5f)).normalized;
			Vector3 normalVelocity = normal * normalSpeed;
			// accumulate_impact computes the angular impulse from the unmodified
			// body-to-probe radial; the inertia-transformed radial is only used when
			// angular_response builds the rotation axis.
			Vector3 tangentialVelocity = normalVelocity - radial * Vector3.Dot(normalVelocity, radial);
			float angle = -(1 + restitution) * tangentialVelocity.magnitude / probeMass;
			// The recovered contact inertia is diag(1, 0.5, 0.5) in body space.
			Vector3 axis = Vector3.Cross(normal, inertiaRadial).normalized;
			if (axis.sqrMagnitude <= 0.000001f || Mathf.Abs(angle) <= 0.000001f)
				return;
			// track_contact::angular_response rotates in a left-handed frame whose
			// X axis is normal x inertiaRadial. Convert its angle before using Unity's
			// right-handed axis-angle rotation.
			Quaternion delta = Quaternion.AngleAxis(-angle * Mathf.Rad2Deg, axis);
			if (accumulateResponse && normalSpeed < -0.000001f)
			{
				// Jacobian of the angular contact impulse with respect to the saved
				// angular step. Point normal motion is dot(step, radial x normal).
				Vector3 velocityGradient = Vector3.Cross(
					(contactPoint - vehicle.rb.position) / SourceLengthToMetres, normal);
				float response = (1 + restitution) * tangentialVelocity.magnitude /
					(-normalSpeed * probeMass);
				for (int row = 0; row < 3; row++)
					for (int column = 0; column < 3; column++)
						sourceContactAngularResponse[row, column] +=
							response * axis[row] * velocityGradient[column];
			}
			pendingContactRotation = hasPendingContactRotation
				? delta * pendingContactRotation
				: delta;
			hasPendingContactRotation = true;
		}
		void ApplyAngularContactFriction(ContactPoint contact, Vector3 normal, float normalSpeed)
		{
			ApplyAngularContactFriction(contact.point, normal, normalSpeed, vehicle.rb.rotation);
		}
		void ApplyAngularContactFriction(Vector3 probeWorldPosition, Vector3 normal, float normalSpeed,
			Quaternion contactOrientation)
		{
			Vector3 probeLocalSource = Quaternion.Inverse(contactOrientation) *
				(probeWorldPosition - vehicle.rb.position) / SourceLengthToMetres;
			ApplyAngularContactFriction(probeWorldPosition, probeLocalSource, normal, normalSpeed,
				contactOrientation * Vector3.up);
		}
		void ApplyAngularContactFriction(Vector3 probeWorldPosition, Vector3 probeLocalSource,
			Vector3 normal, float normalSpeed, Vector3 snapshotUp)
		{
			// contact_response.cpp angular_friction transforms the authored local
			// probe by the complete incremental matrix. Keep this in source units;
			// the point and support rotation remain in world metres.
			float radius = probeLocalSource.magnitude;
			if (radius <= 0.0001f)
				return;
			Vector3 tangent = sourceIncrementalRotation * probeLocalSource - probeLocalSource;
			float projection = Vector3.Dot(normal, tangent);
			tangent -= normal * projection;
			float tangentLength = tangent.magnitude;
			Vector3 tangentDirection = tangentLength > 0.000001f ? tangent / tangentLength : Vector3.zero;
			float strength = Mathf.Clamp(normalSpeed, -5, 5);
			if (strength * 0.2f < tangentLength)
				tangentLength = strength * 0.1f;
			float angle = tangentLength / radius;
			Vector3 signAxis = Vector3.Cross(snapshotUp, probeLocalSource).normalized;
			if (Vector3.Dot(tangentDirection, signAxis) < 0)
				angle = -angle;
			Vector3 radial = probeWorldPosition - vehicle.rb.position;
			Vector3 rotationAxis = SupportRotationAxis(radial, radial.magnitude);
			if (rotationAxis.sqrMagnitude <= 0.000001f || Mathf.Abs(angle) <= 0.000001f)
				return;
			// support_rotation builds a left-handed source basis (y = cross(x, z)).
			// Converting that basis to Unity's right-handed quaternion convention
			// reverses the angle around the corresponding support axis.
			Quaternion delta = Quaternion.AngleAxis(-angle * Mathf.Rad2Deg, rotationAxis);
			pendingContactRotation = hasPendingContactRotation
				? delta * pendingContactRotation
				: delta;
			hasPendingContactRotation = true;
		}
		static Vector3 SupportRotationAxis(Vector3 radialFromBody, float radius)
		{
			Vector3 supportZ = -radialFromBody / radius;
			Vector3 supportX = Vector3.Cross(Vector3.up, supportZ).normalized;
			return Vector3.Cross(supportX, supportZ).normalized;
		}
		void UpdateSteering(float tickScale)
		{
			if (SourcePlayerControlsSuppressed)
				return;
			steeringBoost = Mathf.Clamp(steeringBoost + (vehicle.SGPshiftbutton != 0 ? 0.1f : -0.42857143f) * tickScale, 0, 3);
			float input = Mathf.Clamp(SourceSteerInput, -1, 1);
			if (DigitalControls)
			{
				if (input != 0)
				{
					float amount = Curve(parameters.digitalSteering, steeringRamp) * parameters.steeringAcceleration * (steeringBoost + 1);
					sourceSteering = Mathf.Clamp(sourceSteering + Mathf.Sign(input) * amount * tickScale,
						-parameters.steeringMax, parameters.steeringMax);
					steeringRamp = Mathf.Min(127, steeringRamp + tickScale);
				}
				else
					steeringRamp = Mathf.Max(0, steeringRamp - 6 * tickScale);
			}
			else
			{
				float inputCurve = Curve(parameters.analogSteering, Mathf.Abs(input) * 127);
				float amount = Mathf.Min(1, inputCurve * parameters.steeringSensitivity);
				bool sourceAnalogAssist = !vehicle.followAI.selfDriving && vehicle.basicInput &&
					vehicle.basicInput.playerInput &&
					vehicle.basicInput.playerInput.currentControlScheme == "Gamepad";
				if (sourceAnalogAssist)
				{
					int speedIndex = Mathf.Clamp((int)(Mathf.Abs(currentSourceSpeed) * 0.64f), 0, 127);
					amount = Mathf.Min(1, amount * parameters.velocitySteering[speedIndex] * (steeringBoost + 1));
				}
				sourceSteering = Mathf.Sign(input) * amount * parameters.steeringMax;
			}
			steeringDegrees = sourceSteering;
		}
		void AttenuateSourceSteering(float tickScale)
		{
			// vehicle_dynamics_core attenuates s.steering after the current tick's
			// axle forces and suspension, so this value drives the next tick and the
			// subsequent airborne steering calculation.
			float attenuation = Curve(parameters.steeringCurve, Mathf.Abs(currentSourceSpeed));
			sourceSteering *= Mathf.Pow(attenuation, tickScale);
			steeringDegrees = sourceSteering;
		}
		void ChangeGear(int next)
		{
			next = Mathf.Clamp(next, 0, Mathf.Min(parameters.gearCount, parameters.ratios.Length - 1));
			if (next == gear)
				return;
			int previousGear = gear;
			if (next < gear)
				downshiftTicks = Mathf.Max(0, parameters.shiftTime * SourceTicksPerSecond);
			gear = next;
			clutch = 0;
			vehicle.engine.transmission.SetOriginalGear(gear, parameters);
			TrySourceTrickstartAtGearChange(previousGear, gear);
		}
		void TrySourceTrickstartAtGearChange(int previousGear, int nextGear)
		{
			// Source gear 1 is neutral and source gear 2 is first (displayed N -> 1).
			if (previousGear != 1 || nextGear != 2)
				return;
			float countdown = CountDownSeq.Countdown;
			if (F.I && F.I.s_raceType == RaceType.Stunt)
			{
				//Debug.Log($"[Trickstart] Not activated: disabled in Stunt mode. car={vehicle.name}, " +
				//	$"countdown={countdown:F3}s, rpm={rpm:F0}/{parameters.rpmMax:F0}.", vehicle);
				return;
			}
			float rpmPercent = parameters.rpmMax > 0 ? rpm / parameters.rpmMax * 100 : 0;
			if (rpm <= parameters.rpmMax * 0.6f || rpm >= parameters.rpmMax * 0.8f)
			{
				//Debug.Log($"[Trickstart] Not activated: engine rpm is outside the 60-80% window. " +
				//	$"car={vehicle.name}, track={F.I?.s_trackName ?? "<unknown>"}, " +
				//	$"countdown={countdown:F3}s, rpm={rpm:F0}/{parameters.rpmMax:F0} ({rpmPercent:F1}%).", vehicle);
				return;
			}
			startBoostTicks = 180;
			rpm = parameters.rpmMax;
			// The original suspension pose uses active turbo to add body pitch
			// during acceleration; it does not apply a separate heave offset.
			//Debug.Log($"[Trickstart] Activated on N->1 shift. car={vehicle.name}, " +
			//	$"track={F.I?.s_trackName ?? "<unknown>"}, countdown={countdown:F3}s, " +
			//	$"triggerRpm={rpmPercent:F1}%.", vehicle);
			if (vehicle.raceBox)
				vehicle.raceBox.DoOriginalTrickstart();
		}
		void ShiftToNeutral()
		{
			if (gear == 1)
				return;
			gear = 1;
			clutch = 0;
			vehicle.engine.transmission.SetOriginalGear(gear, parameters);
		}
		void UpdateAutomaticShift(float sourceSpeed, float throttle, float tickScale)
		{
			// Stock Stunt GP initializes parameters.manual_gears to zero and the
			// recovered config/tuning loaders never overwrite it. Keep the source
			// automatic gearbox independent of any gearbox component setting.
			bool sourceShiftInput = !SourcePlayerControlsSuppressed &&
				(vehicle.upshiftPressed || vehicle.downshiftPressed);
			if (rpm < parameters.rpmIdle + 200 && controlFlags[0] == 0 && controlFlags[1] == 0 &&
				controlFlags[2] == 0 && controlFlags[3] == 0 && gear != 1 &&
				!sourceShiftInput)
				ShiftToNeutral();
			if (downshiftTicks > 0)
			{
				downshiftTicks = Mathf.Max(0, downshiftTicks - tickScale);
				rpm += 200 * tickScale;
			}
			if (clutch != 1)
				return;
			if (throttle < 0.4f)
			{
				if ((gear == 1 || gear == 2) && controlFlags[3] == 1 && sourceSpeed < 8.333333f)
					ChangeGear(gear - 1);
				if (gear > 2 && rpm < parameters.rpmLimit * 0.4f)
					ChangeGear(gear - 1);
				return;
			}
			if (gear == 1)
			{
				if (parameters.rpmLimit * 0.5f < rpm)
					ChangeGear(gear + 1);
				return;
			}
			if (gear == 0)
				return;
			if (gear > 2)
			{
				float previousRpm = wheelSpeed[2] * parameters.ratios[gear - 1] * parameters.finalDrive * 3600 /
					Mathf.Max(0.001f, parameters.tyres[0].radius * 2 * Mathf.PI);
				if (previousRpm < parameters.rpmLimit * 0.9f - 2500)
				{
					ChangeGear(gear - 1);
					return;
				}
			}
			if (gear <= 6 && rpm > parameters.rpmLimit * 0.9f)
				ChangeGear(gear + 1);
		}
		void UpdateSourceControlFlags(int grounded)
		{
			if (SourcePlayerControlsSuppressed)
				return;
			float throttleInput = Mathf.Clamp01(gear == 0 ? SourceRawBrakeInput : SourceRawAccelInput);
			float brakeInput = Mathf.Clamp01(gear == 0 ? SourceRawAccelInput : SourceRawBrakeInput);
			bool brakeActive = brakeInput > 0 && (grounded > 0 || !DigitalControls);
			bool throttleActive = throttleInput > 0 &&
				(!DigitalControls || grounded > 0 || airTicks <= 60);
			controlFlags[0] = controlFlags[2];
			controlFlags[2] = throttleActive ? 1 : 0;
			if (!DigitalControls || grounded > 0)
			{
				controlFlags[1] = controlFlags[3];
				controlFlags[3] = brakeActive ? 1 : 0;
			}
		}
		void UpdateSourceBraking(float tickScale, int grounded)
		{
			if (SourcePlayerControlsSuppressed)
				return;
			float brakeInput = Mathf.Clamp01(gear == 0 ? SourceRawAccelInput : SourceRawBrakeInput);
			if (DigitalControls && grounded == 0)
				return; // digital_brake leaves both the ramp and flags untouched in air
			if (DigitalControls)
			{
				if (brakeInput > 0)
				{
					double force = -(double)Curve(parameters.digitalBrake, brakeRamp) * parameters.brakeAcceleration;
					brakeRamp = Mathf.Min(127, brakeRamp + 6 * tickScale);
					if (vehicle.ebrakeInput > 0)
						wheelSpeed[0] = wheelSpeed[1] = 0;
					else
						ApplySourceBrakingForce(force);
				}
				else
					brakeRamp = Mathf.Max(0, brakeRamp - 40 * tickScale);
				return;
			}
			if (brakeInput <= 0)
				return;
			double analogForce = -(double)Curve(parameters.analogBrake, brakeInput * 127) * parameters.brakeAcceleration;
			if (vehicle.ebrakeInput > 0)
				wheelSpeed[0] = wheelSpeed[1] = 0;
			else
				ApplySourceBrakingForce(analogForce);
		}
		static bool TryFindSourceUnityRail(Vector3 position, PathCreator assignedPitPath,
			out EnergyTunnelPath nearestRail,
			out float nearestPathDistance, out float nearestDistanceSqr)
		{
			if (!assignedPitPath)
			{
				nearestRail = null;
				nearestPathDistance = 0;
				nearestDistanceSqr = float.PositiveInfinity;
				return false;
			}
			if (Time.time >= nextSourceUnityRailPathScan)
			{
				sourceUnityRailPaths = Object.FindObjectsByType<EnergyTunnelPath>(FindObjectsSortMode.None);
				nextSourceUnityRailPathScan = Time.time + 1;
			}
			nearestRail = null;
			nearestPathDistance = 0;
			nearestDistanceSqr = float.PositiveInfinity;
			for (int i = 0; i < sourceUnityRailPaths.Length; i++)
			{
				EnergyTunnelPath candidate = sourceUnityRailPaths[i];
				if (!candidate || !candidate.isActiveAndEnabled ||
					candidate.pitsPathCreator != assignedPitPath)
					continue;
				var path = candidate.pitsPathCreator.path;
				if (path == null || path.length <= 0.001f)
					continue;
				float pathDistance = path.GetClosestDistanceAlongPath(position);
				Vector3 closestPoint = path.GetPointAtDistance(pathDistance, EndOfPathInstruction.Stop);
				float distanceSqr = (position - closestPoint).sqrMagnitude;
				if (distanceSqr >= nearestDistanceSqr)
					continue;
				nearestRail = candidate;
				nearestPathDistance = pathDistance;
				nearestDistanceSqr = distanceSqr;
			}
			return nearestRail && nearestDistanceSqr <=
				SourceUnityRailActivationDistance * SourceUnityRailActivationDistance;
		}
		void UpdateSourceRail(float tickScale)
		{
			if (!sourceRailControlsActive || !sourceUnityRailPathThisTick ||
				!sourceUnityRailPathThisTick.pitsPathCreator)
				return;
			var railPath = sourceUnityRailPathThisTick.pitsPathCreator.path;
			if (railPath == null || railPath.length <= 0.001f)
				return;
			if (!sourceRailControlDisabled)
				throttleSignal = 0; // disable_vehicle_control clears the stored throttle on entry.
			sourceRailControlDisabled = true;
			sourceRailThrottle = throttleSignal;
			sourceRailBrakeInput = 0;
			if (unchecked((int)sourceRailProgress) > 75)
			{
				// The original switches to a ten-unit crawl in the final quarter of the rail.
				float error = 10 - currentSourceSpeed;
				ApplySourceRailSpeedError(error, tickScale);
			}
			else
				ApplySourceRailSpeedError(SourcePitlaneTargetSpeed - currentSourceSpeed, tickScale);
			Vector3 sourcePosition = vehicle.rb.position / SourceLengthToMetres;
			Vector3 ahead = vehicle.rb.position + sourceForwardBasis * 8;
			float pathDistance = railPath.GetClosestDistanceAlongPath(ahead);
			Vector3 projected = railPath.GetPointAtDistance(pathDistance, EndOfPathInstruction.Stop) /
				SourceLengthToMetres;
			sourceRailProgress = (uint)Mathf.Clamp(
				Mathf.FloorToInt(pathDistance / railPath.length * 100), 0, 100);
			Vector3 gravity = SourceGravityDirection;
			float dx = sourcePosition.x - projected.x;
			float dy = sourcePosition.y - projected.y;
			double dz = (double)sourcePosition.z - projected.z;
			// contact_rail.cpp preserves the source operation order and adds the
			// unit-gravity projection before measuring lateral steering.
			double gravityProjection = ((double)gravity.z * dz + (double)gravity.y * dy) +
				(double)gravity.x * dx;
			float projectedGravityX = (float)((double)gravity.x * gravityProjection);
			float projectedGravityY = (float)((double)gravity.y * gravityProjection);
			Vector3 delta = new Vector3(
				(float)((double)projectedGravityX + dx),
				(float)((double)projectedGravityY + dy),
				(float)(gravityProjection * gravity.z + dz));
			// The retail basis_x points to the car's left; sourceRightBasis is its
			// Unity-space opposite because the remake uses +X as right.
			double steeringValue = ((double)delta.z * -sourceRightBasis.z +
				(double)delta.y * -sourceRightBasis.y) + (double)delta.x * -sourceRightBasis.x;
			float steering = (float)(steeringValue * 0.005f);
			if (Mathf.Abs(steering) > 0.1f)
				sourceSteering = Mathf.Clamp(sourceSteering + steering, -parameters.steeringMax, parameters.steeringMax);
			steeringDegrees = sourceSteering;
			UpdateSourceRailRefuel(tickScale);
			if (unchecked((int)sourceRailProgress) >= 99)
			{
				sourceSpecialMode = false;
				sourceRailControlDisabled = false;
				sourceRailCompletedPath = sourceUnityRailPathThisTick;
			}
		}
		void ApplySourceRailSpeedError(float error, float tickScale)
		{
			if (error > 0)
			{
				sourceRailThrottle = Mathf.Min(error * 0.1f, 1);
				sourceRailBrakeInput = 0;
				controlFlags[2] = 1;
				return;
			}
			if (error < -3.3333333f)
			{
				if (error < -16.666666f)
				{
					vehicle.rb.linearVelocity = vehicle.rb.linearVelocity * 0.95f +
						SourceGravityAcceleration * Time.fixedDeltaTime;
					sourceRailBrakeInput = 0;
				}
				else
					sourceRailBrakeInput = (byte)Mathf.Clamp((int)(error * -16), 0, 255);
				sourceRailThrottle = 0;
				controlFlags[2] = 0;
				ApplySourceRailBraking(tickScale);
				return;
			}
			sourceRailThrottle *= 0.8f;
		}
		void ApplySourceRailBraking(float tickScale)
		{
			if (sourceContactCounter <= 0)
				return;
			controlFlags[1] = controlFlags[3];
			controlFlags[3] = 0;
			if (sourceRailBrakeInput != 0)
			{
				double force = -(double)Curve(parameters.digitalBrake, brakeRamp) * parameters.brakeAcceleration;
				controlFlags[3] = 1;
				brakeRamp = Mathf.Min(127, brakeRamp + 6 * tickScale);
				ApplySourceBrakingForce(force);
			}
			else
				brakeRamp = Mathf.Max(0, brakeRamp - 40 * tickScale);
		}
		void UpdateSourceRailRefuel(float tickScale)
		{
			int progress = unchecked((int)sourceRailProgress);
			if (progress >= 40 && progress < 65)
			{
				sourceRailTicks += tickScale;
				while (sourceRailTicks >= 3)
				{
					sourceRailTicks -= 3;
					if (parameters.refuelRate <= 0 ||
						SourcePitlaneRefuelLimit * parameters.refuelRate <= sourceRailEnergyAdded ||
						energy >= parameters.fuelCapacity)
						continue;
					// The trigger also adds refuelRate per second. The old x10 rail
					// multiplier made the same tunnel fill a battery in a few ticks.
					float amount = parameters.refuelRate;
					sourceRailEnergyAdded += amount;
					energy += amount;
				}
			}
		}
		void ApplySourceBrakingForce(double force)
		{
			if (wheelSpeed[0] < 0)
				force = -force;
			float first = (float)(force * parameters.brakeBias);
			double second = (1.0 - parameters.brakeBias) * force;
			wheelAcceleration[0] = wheelAcceleration[1] = first;
			wheelAcceleration[2] = wheelAcceleration[3] = (float)second;
			float firstLimit = (float)(System.Math.Abs((double)first) * 2);
			float secondLimit = (float)(System.Math.Abs(second) * 2);
			for (int i = 0; i < 4; i++)
			{
				float limit = i < 2 ? firstLimit : secondLimit;
				if (Mathf.Abs(wheelSpeed[i]) < limit)
				{
					wheelSpeed[i] = 0;
					wheelAcceleration[i] = 0;
				}
			}
		}
		void PrepareSourcePhysicalParameters()
		{
			// prepare_physical_parameters runs before input and start-boost updates.
			sourceEffectiveMass = sourceMassOverride
				? 1000
				: (float)((double)fuel * parameters.fuelUnitMass + parameters.mass);
			vehicle.rb.mass = Mathf.Max(0.001f, sourceEffectiveMass * SourceMassToKilograms);
				effectiveComHeight = sourceMassOverride
				? System.BitConverter.Int32BitsToSingle(unchecked((int)0x41f00001))
					: parameters.comHeight;
			if (sourceSpecialMode && effectiveComHeight > 10)
				effectiveComHeight = 10;
			double friction = gear == 0 ? 2.0 : performanceMultiplier;
			for (int i = 0; i < 4; i++)
			{
				// The original stores scaled tyre coefficients before the contact
				// solver multiplies each one by the surface grip.
				sourceStaticFriction[i] = (float)(friction * parameters.tyres[i].staticFriction);
				sourceKineticFriction[i] = (float)(friction * parameters.tyres[i].kineticFriction);
			}
			torque *= performanceMultiplier;
		}
		void UpdateSourceUpgradeCondition()
		{
			if (!SourceUpgradeActive)
			{
				sourceUpgradeCondition = 0;
				return;
			}
			// Rival AI starts every update from its configured skill. The remake
			// exposes a global CPU level instead of each source driver's skill.
			float baseCondition = SourceCpuSkill * 0.01f;
			sourceUpgradeCondition = baseCondition;
			if (!F.I || F.I.s_inEditor || !vehicle.raceBox || !vehicle.raceBox.enabled ||
				!vehicle.gameObject.activeInHierarchy)
				return;
			var cars = F.I.s_cars;
			VehicleParent leader = null;
			VehicleParent second = null;
			float leaderProgress = float.NegativeInfinity;
			float secondProgress = float.NegativeInfinity;
			for (int i = 0; i < cars.Count; i++)
			{
				VehicleParent candidate = cars[i];
				if (!candidate || !candidate.raceBox || !candidate.raceBox.enabled ||
					!candidate.followAI || !candidate.followAI.trackPathCreator ||
					candidate.followAI.trackPathCreator.path == null || !candidate.gameObject.activeInHierarchy)
					continue;
				float progress = candidate.raceBox.RaceProgressLaps;
				if (progress > leaderProgress)
				{
					second = leader;
					secondProgress = leaderProgress;
					leader = candidate;
					leaderProgress = progress;
				}
				else if (progress > secondProgress)
				{
					second = candidate;
					secondProgress = progress;
				}
			}
			if (!leader || cars.Count < 2)
				return;
			float routeLength = 0;
			if (RaceManager.I && RaceManager.I.racingPaths != null && RaceManager.I.racingPaths.Length > 1 &&
				RaceManager.I.racingPaths[1] && RaceManager.I.racingPaths[1].path != null)
				routeLength = RaceManager.I.racingPaths[1].path.length;
			else if (vehicle.followAI.trackPathCreator && vehicle.followAI.trackPathCreator.path != null)
				routeLength = vehicle.followAI.trackPathCreator.path.length;
			if (routeLength <= 0)
				return;
			if (leader == vehicle)
			{
				if (!second)
					return;
				float gap = (leaderProgress - secondProgress) * routeLength;
				if (gap <= SourceCpuLeaderSkillPenaltyDistanceMetres)
					return;
				float fraction = Mathf.Min(1, (gap - SourceCpuLeaderSkillPenaltyDistanceMetres) /
					SourceCpuLeaderSkillPenaltyDistanceMetres);
				sourceUpgradeCondition = Mathf.Max(0, baseCondition - fraction * SourceCpuLeaderSkillPenalty);
				return;
			}
			float distanceBehind = (leaderProgress - vehicle.raceBox.RaceProgressLaps) * routeLength;
			if (distanceBehind <= SourceCpuCatchupDistanceMetres)
				return;
			float catchupFraction = Mathf.Min(1, (distanceBehind - SourceCpuCatchupDistanceMetres) /
				SourceCpuCatchupDistanceMetres);
			sourceUpgradeCondition = Mathf.Min(1, baseCondition + catchupFraction * SourceCpuCatchupSkillGain);
		}
		void UpdateSourcePerformance()
		{
			if (!F.I || F.I.s_inEditor || !vehicle.raceBox || !vehicle.raceBox.enabled ||
				!vehicle.gameObject.activeInHierarchy)
				return;
			var cars = F.I.s_cars;
			VehicleParent reference = null;
			float bestPlaceValue = float.NegativeInfinity;
			for (int i = 0; i < cars.Count; i++)
			{
				VehicleParent candidate = cars[i];
				if (!candidate || !candidate.raceBox || !candidate.raceBox.enabled ||
					!candidate.gameObject.activeInHierarchy)
					continue;
				// The source ranking and performance reference both use route
				// progress, including in stunt, drift, and time-trial modes.
				float placeValue = candidate.raceBox.RaceProgressLaps;
				if (placeValue > bestPlaceValue)
				{
					bestPlaceValue = placeValue;
					reference = candidate;
				}
			}
			// performance_reference falls back to source slot 2 when no active
			// first-place vehicle is found.
			if (!reference && cars.Count > 2)
				reference = cars[2];
			performanceMultiplier = 1;
			if (!reference || reference == vehicle || !reference.raceBox)
				return;
			// Both FollowAI.LapProgressPercent values use racingPaths[1] as their
			// common denominator. Source performance scales the progress difference
			// by one shared route length, rather than each car's selected AI path.
			float routeLength = 0;
			if (RaceManager.I && RaceManager.I.racingPaths != null && RaceManager.I.racingPaths.Length > 1 &&
				RaceManager.I.racingPaths[1] && RaceManager.I.racingPaths[1].path != null)
				routeLength = RaceManager.I.racingPaths[1].path.length;
			else if (vehicle.followAI && vehicle.followAI.trackPathCreator &&
				vehicle.followAI.trackPathCreator.path != null)
				routeLength = vehicle.followAI.trackPathCreator.path.length;
			if (routeLength <= 0)
				return;
			float gap = (vehicle.raceBox.RaceProgressLaps - reference.raceBox.RaceProgressLaps) * routeLength;
			float shiftedGap = gap - SourcePerformanceDeadZoneMetres;
			if (shiftedGap >= 0)
				return;
			bool upgraded = vehicle.followAI && vehicle.followAI.IsCPU;
			float limit = upgraded
				? SourceCpuPerformanceDistanceMetres
				: SourceHumanPerformanceDistanceMetres;
			float distance = Mathf.Max(shiftedGap, -limit);
			float factor = Mathf.Min(1, Mathf.Abs(distance) / limit * 2);
			performanceMultiplier = SourcePerformanceGain * factor + 1;
		}
		void UpdateSourceStartBoost(float tickScale)
		{
			sourceStartBoostActiveThisTick = false;
			// update_vehicle_start_boost exits for source race mode 3 (stunt).
			// Time trial is source mode 4 and still uses the start boost.
			if (F.I && F.I.s_raceType == RaceType.Stunt)
				return;
			if (startBoostTicks > 0)
			{
				// update_vehicle_start_boost sets turbo_active before vehicle_dynamics_core
				// on every active tick, including the tick that decrements the timer to 0.
				sourceStartBoostActiveThisTick = true;
				startBoostTicks = Mathf.Max(0, startBoostTicks - tickScale);
				performanceMultiplier = startBoostTicks * 0.0055555556900799274f + 1;
				fuel = parameters.fuelCapacity;
				energy = parameters.fuelCapacity;
				sourceImpactEnergyLossThisTick = 0;
				return;
			}
		}
		bool SourceAiTurboForSpeed()
		{
			if (!(parameters.fuelCapacity * 0.3f < energy) || !(SourceRawAccelInput > 0.75f) ||
				!(currentSourceSpeed > 3.3333333f))
				return false;
			float threshold = ((1 - SourceUpgradeCondition) * SourceCpuBoostUsageModifier + 1) *
				(parameters.rpmMax * 0.4f);
			return threshold > rpm;
		}
		bool SourceAiTurboForStunt()
		{
			return parameters.fuelCapacity * 0.5f < energy && SourceRawAccelInput > 0.5f &&
				parameters.rpmMax * 0.75f > rpm && vehicle.followAI &&
				!vehicle.followAI.Pitting && vehicle.followAI.NextStuntPointIn(100);
		}
		void UpdateSourceAITurbo()
		{
			if (CountDownSeq.Countdown > 0 || !SourceUpgradeActive ||
				!vehicle.followAI.selfDriving || vehicle.followAI.Pitting ||
				!vehicle.raceBox || !vehicle.raceBox.enabled)
			{
				sourceAiTurboTicks = sourceAiTurboCooldown = sourceAiTurboRestTicks = 0;
				sourceAiTurboActive = false;
				return;
			}
			sourceAiTurboActive = false;
			if (sourceAiTurboCooldown != 0)
			{
				--sourceAiTurboCooldown;
				return;
			}
			if (sourceAiTurboTicks != 0)
			{
				sourceAiTurboActive = true;
				--sourceAiTurboTicks;
				if (parameters.rpmMax * 0.99f < rpm || sourceAiStuntMeter > 0)
					sourceAiTurboTicks = 0;
				if (sourceAiTurboTicks == 0)
				{
			sourceAiTurboActive = false;
					sourceAiTurboCooldown = sourceAiTurboRestTicks;
				}
				return;
			}
			bool speed = SourceAiTurboForSpeed();
			if (!speed && !SourceAiTurboForStunt())
				return;
			float factor = SourceCpuBoostUsageModifier * SourceUpgradeCondition + 1;
			double activeRange = speed ? 120 : 60;
			double activeBase = speed ? 180 : 120;
			sourceAiTurboTicks = unchecked((int)((System.Math.Abs(SourceRandomSigned((float)activeRange)) + activeBase) * factor));
			sourceAiTurboRestTicks = unchecked((int)((System.Math.Abs(SourceRandomSigned(300)) + 300) / factor));
		}
		void HoldSourceStartVehicle(float tickScale)
		{
			// retail_race_start.cpp::hold_start_vehicles runs while the normal-race
			// countdown has more than 60 of its 301 ticks left. It advances the
			// brake ramp and zeros all four source wheel speeds. The remake exposes
			// the matching interval as seconds, excluding its final GO fade.
			if (!F.I || F.I.s_inEditor || F.I.s_raceType == RaceType.Stunt ||
				!vehicle.raceBox || !vehicle.raceBox.enabled || CountDownSeq.Countdown <= 1)
				return;
			brakeRamp += 6 * tickScale;
			System.Array.Clear(wheelSpeed, 0, wheelSpeed.Length);
		}
		void UpdateEngine(float sourceSpeed, int grounded, int sourceContactClass, float tickScale)
		{
			// vehicle.cpp::engine_tick consumes fuel from the RPM values at the
			// start of this source tick, before turbo update and before the new
			// engine acceleration is calculated. Keeping this at the end made the
			// low-energy torque limit react one tick late and charged the newly
			// calculated RPM instead of the previous RPM.
			if (gear == 0)
			{
				sourceTurboActive = false;
				while (rpm > parameters.rpmLimit * 0.5f)
					rpm *= 0.8f;
			}
			ConsumeSourceEngineEnergy(tickScale);
			float sourceMass = sourceEffectiveMass;
			float throttleInput = Mathf.Clamp01(gear == 0 ? SourceRawBrakeInput : SourceRawAccelInput);
			if (sourceRailControlsActive)
				throttleSignal = sourceRailThrottle;
			else if (DigitalControls)
			{
				if (grounded > 0)
					throttleSignal *= Mathf.Pow(0.9f, tickScale);
				if (throttleInput > 0 && (grounded > 0 || airTicks <= 60))
					throttleSignal = Mathf.Min(1, throttleSignal + 0.15f * tickScale);
				else if (grounded > 0)
					throttleSignal *= Mathf.Pow(0.5f, tickScale);
			}
			else if (throttleInput > 0)
				throttleSignal = grounded == 0 && airTicks > 60
					? throttleSignal * Mathf.Pow(0.9f, tickScale) : throttleInput;
			else
				throttleSignal *= Mathf.Pow(0.5f, tickScale);
			float throttle = throttleSignal;
			bool playerBoostAllowed = !sourceRailControlsActive;
			bool boostRequested = SourceUpgradeActive ? sourceAiTurboActive : vehicle.boostButton != 0;
			bool turboActive = gear != 0 && (sourceStartBoostActiveThisTick ||
				(playerBoostAllowed && boostRequested)) &&
				energy >= parameters.fuelCapacity * parameters.turboEnergyThreshold;
			sourceTurboActive = turboActive;
			if (turboActive)
				turboRpm = Mathf.Min(parameters.turboMax - 1, turboRpm + parameters.turboAcceleration * tickScale);
			else
				turboRpm *= Mathf.Pow(parameters.turboDecay, tickScale);
			if (turboRpm < 50)
				turboRpm = 0;
			turboForce = turboRpm > 0 ? Curve(parameters.turboCurve, turboRpm * 128 / Mathf.Max(1, parameters.turboMax)) * parameters.turboScale : 0;
			// engine_tick updates turbo before the automatic gearbox. The stock
			// game has manual_gears disabled, so shift buttons cannot select gears
			// directly; the source automatic shift logic decides each gear.
			if (SourceAutomaticShiftAllowed)
				UpdateAutomaticShift(sourceSpeed, throttle, tickScale);
			if (clutch < 1)
				clutch = Mathf.Min(1, clutch + tickScale / Mathf.Max(1, parameters.shiftTime * 60));
			if (clutch < 0.5f)
				throttle = 0;
			if (throttle < 0.1f)
				rpm *= Mathf.Pow(parameters.engineDecay, tickScale);
			float ratio = gear == 1 ? 2 : parameters.ratios[gear];
			int torqueCurveIndex = SourceTorqueCurveIndex;
			float driveClutch = gear == 1 ? 0 : clutch;
			float clutchMass = (float)((double)driveClutch * sourceMass);
			float drivenContactTicks = parameters.driveMode == 0
				? contactCountdown[0] + contactCountdown[1]
				: parameters.driveMode == 1
					? contactCountdown[2] + contactCountdown[3]
					: contactCountdown[0] + contactCountdown[1] + contactCountdown[2] + contactCountdown[3];
			int requiredContactTicks = parameters.driveMode == 2 ? 16 : 8;
			if (drivenContactTicks < requiredContactTicks)
				clutchMass = (float)((double)clutchMass * 0.25);
			float engineMass = (float)((double)clutchMass + 50);
			double accelerationValue = (double)ratio * torque * parameters.finalDrive * parameters.efficiency * throttle /
				engineMass * 100 * (double)0.00027777778f * 3;
			float acceleration = (float)accelerationValue;
			if (!sourceSpecialMode && energy <= 10)
			{
				float scaledSpeed = sourceSpeed * 0.013333334f;
				if (scaledSpeed > 1)
					scaledSpeed = 1;
				acceleration = (float)((1 - (double)scaledSpeed * scaledSpeed) * acceleration);
			}
			float radius = (float)(parameters.tyres[0].radius * 0.01f);
			float circumference = (float)((double)radius * 6.2831855f);
			rpm = (float)((double)ratio * parameters.finalDrive * acceleration * 60 / circumference * tickScale + rpm);
			while (rpm > parameters.rpmLimit)
				rpm *= 0.9f;
			if (rpm < 100)
				rpm = 100;
			if (rpm < parameters.rpmIdle)
				rpm *= Mathf.Pow(1.05f, tickScale);
			float driveAcceleration = (float)((double)acceleration * driveClutch);
			DistributeSourceDrive(driveAcceleration, sourceContactClass);
			if (gear != 1)
			{
				double drivenWheelSpeed = parameters.driveMode == 1 ? wheelSpeed[2]
					: parameters.driveMode == 0 ? wheelSpeed[0] :
						((double)wheelSpeed[2] + wheelSpeed[0]) * 0.5;
				double mismatch = ((double)radius * rpm * 6.2831855f /
					((double)ratio * parameters.finalDrive * 60) - drivenWheelSpeed * 0.6f) * clutch;
				float rpmChange = (float)((double)engineMass * 0.02f * ratio * mismatch * parameters.finalDrive /
					circumference);
				rpm = (float)((double)rpm - (double)rpmChange * tickScale);
				float mismatchMassFactor = 50 / engineMass;
				float mismatchAcceleration = (float)((double)mismatchMassFactor * mismatch * 1.6666666f);
				DistributeSourceDrive(mismatchAcceleration, sourceContactClass);
				while (rpm > parameters.rpmLimit)
					rpm *= 0.9f;
			}
			torque = Mathf.Max(250, Curve(parameters.torqueCurves[torqueCurveIndex],
				rpm * 128 / Mathf.Max(1, parameters.rpmLimit)) * parameters.maxTorque);
			DriveForce engineDrive = vehicle.engine.GetComponent<DriveForce>();
			if (engineDrive)
			{
				engineDrive.rpm = rpm;
				engineDrive.torque = torque * throttle;
			}
		}
		void ConsumeSourceEngineEnergy(float tickScale)
		{
			// Source mode 3 (stunt) is the one mode that skips engine energy use;
			// time trial consumes energy. Evaluate from incoming RPM as the source
			// engine_tick does, while retaining the editor/inactive-race guards.
			if (!F.I || F.I.s_inEditor || !vehicle.raceBox || !vehicle.raceBox.enabled ||
				F.I.s_raceType == RaceType.Stunt)
				return;
			float engineConsumption = Curve(parameters.consumptionCurve,
				rpm * 128 / Mathf.Max(1, parameters.rpmMax)) * parameters.fuelConsumption * tickScale;
			float turboConsumption = Curve(parameters.consumptionCurve,
				turboRpm * 128 / Mathf.Max(1, parameters.turboMax)) * parameters.turboConsumption * tickScale;
			if (SourceUpgradeActive)
			{
				engineConsumption = (float)(((1.0 - SourceUpgradeCondition) * SourceCpuEngineConsumptionModifier + 1.0) * engineConsumption);
				turboConsumption = (float)(((1.0 - SourceUpgradeCondition) * SourceCpuTurboConsumptionModifier + 1.0) * turboConsumption);
			}
			// The retail fuel field is allowed to pass below zero; only the battery
			// energy field is clamped by consume_engine_energy/consume_turbo_energy.
			fuel = (float)((double)fuel - engineConsumption);
			energy = Mathf.Max(0, energy - engineConsumption - turboConsumption);
		}
		void DistributeSourceDrive(float acceleration, int sourceContactClass)
		{
			double value = acceleration;
			if (acceleration > 0 && turboRpm != 0 && sourceContactClass == 0)
				value = value * turboForce + value;
			value *= 4;
			if (parameters.driveMode == 1 || parameters.driveMode == 2)
			{
				double second = parameters.driveMode == 1 ? value : value * parameters.powerSplit;
				for (int i = 2; i < 4; i++)
					wheelAcceleration[i] = (float)(second + wheelAcceleration[i]);
			}
			if (parameters.driveMode == 0 || parameters.driveMode == 2)
			{
				double first = parameters.driveMode == 0 ? value : (1.0 - parameters.powerSplit) * value;
				for (int i = 0; i < 2; i++)
					wheelAcceleration[i] = (float)(first + wheelAcceleration[i]);
			}
		}
		void FilterWheelContacts(float tickScale, Vector3 contactStartPosition, Vector3 contactStartVelocity)
		{
			float sourceGravity = SourceGravityMagnitude;
			float probeMass = sourceEffectiveMass * 0.25f;
			sourceContactAngularResponse = Matrix4x4.identity;
			sourceContactVelocityBeforeThisTick = contactStartVelocity;
			sourceContactVelocityDeltaThisTick = Vector3.zero;
			sourceContactVelocityAfterThisTick = contactStartVelocity;
			Vector3 averagePositionCorrection = Vector3.zero;
			Vector3 accumulatedVelocityChange = Vector3.zero;
			Vector3 accumulatedClosingContactNormal = Vector3.zero;
			float accumulatedClosingContactSpeed = 0;
			float accumulatedRestitutionSpeed = 0;
			System.Array.Clear(sourceProbeWheelLoad, 0, sourceProbeWheelLoad.Length);
			System.Array.Clear(sourceProbeWheelTouched, 0, sourceProbeWheelTouched.Length);
			System.Array.Clear(sourceProbeHitsThisTick, 0, sourceProbeHitsThisTick.Length);
			System.Array.Clear(sourceProbeFirstContactNormal, 0, sourceProbeFirstContactNormal.Length);
			System.Array.Clear(sourceProbeFirstContactSpeed, 0, sourceProbeFirstContactSpeed.Length);
			System.Array.Clear(sourceProbeFirstContactPoint, 0, sourceProbeFirstContactPoint.Length);
			System.Array.Clear(sourceProbeFirstContactCenter, 0, sourceProbeFirstContactCenter.Length);
			sourceContactVelocityRespawnRequested = false;
			bool correctedPosition = false;
			// contact_response.cpp::initialize_rig gives four wheel probes (radius
			// 70 cm), plus model-specific body probes. Solve those source spheres
			// before vehicle dynamics, matching world_tick's contact stage.
			bool iterationOverflow = false;
			for (int i = 0; i < sourceProbeCount; i++)
			{
				Vector3 current = SourceProbeWorldPosition(i, stepStartRotation);
				Vector3 previous = hasPreviousSourceProbe[i] ? previousSourceProbeWorld[i] : current;
				Vector3 segmentStart = previous;
				Vector3 sourceMotion = current - previous;
				float remaining = 1;
				Vector3 finalProbePosition = current;
				bool probeTouched = false;
				bool probeOverflow = false;
				float probeRadius = sourceProbeRadius[i] * SourceLengthToMetres;
				int attempt = 0;
				for (;;)
				{
					Vector3 segmentMotion = sourceMotion * remaining;
					float segmentLength = segmentMotion.magnitude;
					bool foundContact = TryFindSourceProbeContact(segmentStart, segmentMotion, probeRadius,
						out Collider collider, out Vector3 point, out Vector3 normal,
						out Vector3 probeCenter, out float travelDistance, out int triangleIndex);
					// A just-supported wheel can miss the exact sphere boundary by a
					// fraction of a millimetre. Losing its load and resetting its impact
					// streak then prevents settling, particularly on light cars. Recheck
					// only nearby contacts against the actual Unity geometry. A tiny
					// outgoing motion must also keep support, or an alternating roll
					// clears the streak on each side before stabilization can run.
					const float restingContactSkin = 0.0025f;
					if (!foundContact && attempt == 0 && i < 4 && contactCountdown[i] > 0 &&
						hasSourceWheelContactNormal[i] && sourceWheelContactCollider[i])
					{
						Vector3 previousNormal = sourceWheelContactNormal[i];
						float gap = Vector3.Dot(current - sourceWheelContactPoint[i], previousNormal) - probeRadius;
						if (gap >= 0 && gap <= restingContactSkin &&
							Vector3.Dot(segmentMotion, previousNormal) <= restingContactSkin &&
							TryFindSourceProbeOverlap(current, -previousNormal, probeRadius + restingContactSkin,
								RaceManager.I.wheelCastMask, out collider, out point, out normal,
								out probeCenter, out triangleIndex) &&
							Vector3.Dot(normal, previousNormal) > 0.99f)
						{
							// Revalidate the gap on the newly queried surface, including seams.
							float actualGap = Vector3.Dot(current - point, normal) - probeRadius;
							if (Mathf.Abs(actualGap) <= restingContactSkin)
							{
								// Query tolerance does not change the physical sphere radius.
								probeCenter -= normal * restingContactSkin;
								travelDistance = segmentLength;
								foundContact = true;
							}
						}
					}
					if (!foundContact)
					{
						if (attempt == 0)
							sourceProbeContactIterations[i] = 0;
						attempt++;
						if (attempt > 64)
						{
							iterationOverflow = probeOverflow = true;
							break;
						}
						finalProbePosition = segmentStart + segmentMotion;
						break;
					}
					probeTouched = true;
					sourceProbeHitsThisTick[i]++;
					sourceProbeContactIterations[i] = Mathf.Min(128,
						sourceProbeContactIterations[i] + 1);
					sourceContactCounter = Mathf.Min(8, sourceContactCounter + 1);
					// Retail accumulate_impact uses the current sweep motion; the wheel
					// load separately uses update_probe_velocity. After a hit the sweep
					// is reflected, so another triangle must not replay the incoming
					// rigidbody impulse. Light cars amplify that feedback (Formula 17).
					float wheelNormalSpeed = SourceWheelNormalSpeed(current, normal, Vector3.zero);
					float normalSpeed = Mathf.Min(0, Vector3.Dot(sourceMotion, normal) /
						(SourceLengthToMetres * Mathf.Max(0.000001f, tickScale)));
					if (sourceProbeHitsThisTick[i] == 1)
					{
						sourceProbeFirstContactNormal[i] = normal;
						sourceProbeFirstContactSpeed[i] = normalSpeed;
						sourceProbeFirstContactPoint[i] = point;
						sourceProbeFirstContactCenter[i] = probeCenter;
					}
					float alignment = Vector3.Dot(normal, stepStartRotation * Vector3.up);
					bool wheelContact = i < 4 && alignment > 0.70710678f;
					if (wheelContact)
					{
						sourceProbeWheelTouched[i] = true;
						// The wheel load comes from the rigid probe velocity, not its
						// reflected sweep segment used by accumulate_impact.
						// Resting tolerance can retain a slightly separating contact.
						// A tire supports compression; it cannot pull the road upward.
						sourceProbeWheelLoad[i] = Mathf.Max(0, -wheelNormalSpeed * probeMass);
						contactCountdown[i] = 8;
						sourceWheelContactNormal[i] = normal;
						hasSourceWheelContactNormal[i] = true;
						Wheel wheel = vehicle.wheels[UnityWheelIndex[i]];
						wheel.contactPoint.normal = normal;
						ApplySourceSurface(i, wheel, collider, point, triangleIndex);
					}
					else
					{
						// finish_impact_physical's body-probe path resets the COM recovery
						// state and adds source linear/angular contact friction.
						effectiveComHeight = parameters.comHeight;
						sourceCom2628 = sourceCom262c = 0;
						comResetTicks = -12;
						if (Mathf.Abs(normal.y) < 0.5f)
							vehicle.rb.linearVelocity += normal * SourceSpeedToMetresPerSecond;
						Vector3 sourceVelocity = vehicle.rb.linearVelocity / SourceSpeedToMetresPerSecond;
						Vector3 tangentVelocity = sourceVelocity - normal * Vector3.Dot(sourceVelocity, normal);
						float tangentLength = tangentVelocity.magnitude;
						float strength = Mathf.Min(50, Mathf.Abs(normalSpeed));
						if (tangentLength > 0.001f && strength > 0)
						{
							float removed = tangentLength > strength * 0.2f ? strength * 0.1f : tangentLength;
							accumulatedVelocityChange -= tangentVelocity / tangentLength *
								(removed * SourceSpeedToMetresPerSecond);
						}
						ApplyAngularContactFriction(current, sourceProbeLocal[i], normal, normalSpeed,
							sourceUpBasis);
					}
					// impact() first finishes the physical response, then applies energy
					// loss and material effects. Wheel impacts use the quarter-strength
					// effect set by finish_impact_physical; body probes use full strength.
					ApplySourceImpactEnergy(wheelContact ? normalSpeed * 0.25f : normalSpeed);
					ProcessSourceContactMaterial(i, collider, point, normal, triangleIndex, alignment);
					float impactRestitution = OriginalContactPhysics.EffectiveRestitution(normalSpeed, normal,
						vehicle.rb.position - current, sourceGravity);
					accumulatedVelocityChange += SourceProbeImpactVelocityChange(normal, normalSpeed,
						impactRestitution);
					if (normalSpeed < 0)
					{
						float closingSpeed = -normalSpeed;
						accumulatedClosingContactNormal += normal * closingSpeed;
						accumulatedClosingContactSpeed += closingSpeed;
						accumulatedRestitutionSpeed += closingSpeed * impactRestitution;
					}
					ApplyAngularImpactResponse(current, normal, normalSpeed, sourceGravity, stepStartRotation, true);
					attempt++;
					if (attempt > 64)
					{
						iterationOverflow = probeOverflow = true;
						break;
					}
					float restitution = OriginalContactPhysics.MotionRestitution(normalSpeed, sourceGravity,
						vehicle.rb.position - current);
					float normalMotion = Vector3.Dot(sourceMotion, normal);
					Vector3 reflectedMotion = sourceMotion - normal * (normalMotion * (1 + restitution));
					remaining = segmentLength > 0.000001f
						? Mathf.Clamp01((segmentLength - travelDistance) / segmentLength) : 0;
					segmentStart = probeCenter + normal * 0.001f;
					sourceMotion = reflectedMotion;
					finalProbePosition = segmentStart + sourceMotion * remaining;
					// No sweep remains after a fully consumed or inelastic impact.
					// Re-querying that stationary sphere can rediscover the same surface
					// within the overlap tolerance until the 64-attempt reset limit.
					if (remaining <= 0.000001f ||
						(sourceMotion * remaining).sqrMagnitude <= 0.000000000001f)
						break;
				}
				if (probeOverflow)
				{
					// update_probe_position has already advanced this probe before the
					// retail solver returns; that staged probe position becomes its next
					// tick's previous point even though the body pose was not committed.
					previousSourceProbeWorld[i] = current;
					hasPreviousSourceProbe[i] = true;
					break;
				}
				if (i < 4)
					FilterSourceWheelLoad(i, tickScale);
				if (probeTouched)
				{
					averagePositionCorrection += finalProbePosition - current;
					correctedPosition = true;
				}
				previousSourceProbeWorld[i] = finalProbePosition;
				hasPreviousSourceProbe[i] = true;
			}
			sourceProbeIterationOverflow = iterationOverflow;
			if (iterationOverflow)
			{
				// The retail contact step returns before committing accumulated probe
				// motion, body velocity, or body rotation. Per-impact wheel/surface and
				// damage state already written above is retained for the reset stage.
				vehicle.rb.position = contactStartPosition;
				vehicle.rb.linearVelocity = contactStartVelocity;
				sourceContactVelocityAfterThisTick = contactStartVelocity;
				return;
			}
			// Several probes can hit in one tick. Bound their combined separating
			// speed to the original weighted restitution so overlapping contacts do
			// not turn a falling body into an unintended launch.
			if (accumulatedClosingContactSpeed > 0.0001f)
			{
				Vector3 supportNormal = accumulatedClosingContactNormal.normalized;
				if (supportNormal.sqrMagnitude > 0.000001f)
				{
					float incomingSupportSpeed = Vector3.Dot(contactStartVelocity, supportNormal);
					float resultingSupportSpeed = Vector3.Dot(
						contactStartVelocity + accumulatedVelocityChange, supportNormal);
					float allowedSeparationSpeed = Mathf.Max(0, -incomingSupportSpeed) *
						(accumulatedRestitutionSpeed / accumulatedClosingContactSpeed);
					if (incomingSupportSpeed < 0 && resultingSupportSpeed > allowedSeparationSpeed)
						accumulatedVelocityChange -= supportNormal * (resultingSupportSpeed - allowedSeparationSpeed);
				}
			}
			// contact_solver marks respawn_requested when the accumulated contact
			// velocity delta exceeds 250 source units, but still commits this solve.
			float sourceVelocityDelta = accumulatedVelocityChange.magnitude /
				SourceSpeedToMetresPerSecond;
			sourceContactVelocityRespawnRequested = sourceVelocityDelta > 250;
			sourceContactVelocityDeltaThisTick = accumulatedVelocityChange;
			vehicle.rb.linearVelocity += accumulatedVelocityChange;
			sourceContactVelocityAfterThisTick = vehicle.rb.linearVelocity;
			if (correctedPosition)
			{
				Vector3 positionCorrection = averagePositionCorrection / sourceProbeCount;
				vehicle.rb.position += positionCorrection;
				// Retail keeps each corrected query endpoint as probe history. Moving
				// those endpoints with the averaged body correction puts them above
				// the surface and adds a false closing motion to the next sweep.
			}
		}
		void FilterSourceWheelLoad(int i, float tickScale)
		{
			Wheel wheel = vehicle.wheels[UnityWheelIndex[i]];
			float sourceLoad = sourceProbeWheelTouched[i] ? sourceProbeWheelLoad[i] : 0;
			// filter_wheel_load runs once per 60 Hz source tick, including in
			// the air. Step fractional Unity ticks with the equivalent source
			// filter decay so the load and grace timer keep source-time duration.
			float remainingTicks = tickScale;
			while (remainingTicks > 0)
			{
				float step = Mathf.Min(1, remainingTicks);
				float difference = Mathf.Max(filteredContactLoad[i] - sourceLoad, -6000f);
				float filterStep = 1 - Mathf.Pow(0.875f, step);
				filteredContactLoad[i] -= difference * filterStep;
				contactCountdown[i] -= step;
				if (contactCountdown[i] < 0)
					filteredContactLoad[i] = 0;
				remainingTicks -= step;
			}
			UpdateWheelContact(wheel);
		}
		void LimitSourceContactOvercorrection()
		{
			Vector3 angularStep = RotationVector(sourceIncrementalRotation);
			Vector3 correction = RotationVector(pendingContactRotation);
			// Simultaneous contacts evaluate the incoming angular step separately.
			// On light rigs their summed correction can reverse and amplify it:
			// Formula 17's logs show ~0.10 rad in and ~0.20 rad against that step.
			// Use the implicit contact response only for this excessive corrective
			// feedback; ordinary impulses and first impacts retain the retail path.
			if (sourceContactClass == 1 || angularStep.sqrMagnitude <= 0.000001f ||
				Vector3.Dot(angularStep, correction) >= 0 ||
				correction.sqrMagnitude <= angularStep.sqrMagnitude)
				return;
			Vector3 resolved = sourceContactAngularResponse.inverse.MultiplyVector(correction);
			float angle = resolved.magnitude;
			if (float.IsNaN(angle) || float.IsInfinity(angle) || angle >= correction.magnitude)
				return;
			pendingContactRotation = angle > 0.000001f
				? Quaternion.AngleAxis(angle * Mathf.Rad2Deg, resolved / angle)
				: Quaternion.identity;
		}
		void StabilizeSourceIncrementalRotation()
		{
			// contact_response.cpp damps and settles the incremental matrix only
			// after each of the four wheel probes has accumulated four impacts.
			bool supportsStabilization = true;
			for (int i = 0; i < 4; i++)
				if (sourceProbeContactIterations[i] < 4)
				{
					supportsStabilization = false;
					break;
				}
			if (supportsStabilization)
			{
				Vector3 x = sourceIncrementalRotation * Vector3.right;
				Vector3 y = sourceIncrementalRotation * Vector3.up;
				Vector3 z = sourceIncrementalRotation * Vector3.forward;
				if (Mathf.Abs(z.x) <= 0.05f && Mathf.Abs(z.y) <= 0.05f)
				{
					x.x *= 1.5f;
					y.y *= 1.5f;
					z.z *= 1.5f;
					x.Normalize();
					y.Normalize();
					z.Normalize();
					if (z.sqrMagnitude > 0.000001f && y.sqrMagnitude > 0.000001f)
						sourceIncrementalRotation = Quaternion.LookRotation(z, y);
				}
				z = sourceIncrementalRotation * Vector3.forward;
				if (Mathf.Abs(z.x) <= 0.002f && Mathf.Abs(z.y) <= 0.002f)
					sourceIncrementalRotation = Quaternion.identity;
			}
			// limit_incremental trims any matrix column component beyond the
			// recovered 45-degree bound by rotating the incremental basis in place.
			const float threshold = 0.70710677f;
			Vector3 forward = sourceIncrementalRotation * Vector3.forward;
			Vector3 right = sourceIncrementalRotation * Vector3.right;
			sourceIncrementalRotation = LimitSourceIncrementalAxis(sourceIncrementalRotation, 0, forward.y, threshold);
			sourceIncrementalRotation = LimitSourceIncrementalAxis(sourceIncrementalRotation, 1, -forward.x, threshold);
			sourceIncrementalRotation = LimitSourceIncrementalAxis(sourceIncrementalRotation, 2, -right.y, threshold);
		}
		static Quaternion LimitSourceIncrementalAxis(Quaternion rotation, int axis, float value, float threshold)
		{
			if (Mathf.Abs(value) <= threshold)
				return rotation;
			float argument = value < 0 ? value + threshold : value - threshold;
			float sine = Mathf.Sqrt(Mathf.Max(0, (1 + argument) * (1 - argument)));
			float degrees = Mathf.Atan2(argument, sine) * Mathf.Rad2Deg;
			Vector3 direction = axis == 0 ? Vector3.right : axis == 1 ? Vector3.up : Vector3.forward;
			return Quaternion.AngleAxis(degrees, direction) * rotation;
		}
		void ApplySourceSuspensionTick(float tickScale)
		{
			float sourceGravity = SourceGravityMagnitude;
			float sourceMass = vehicle.rb.mass / SourceMassToKilograms;
			float sourceReferenceLoad = sourceMass * sourceGravity * 0.25f;
			float remainingTicks = Mathf.Max(0, tickScale);
			while (remainingTicks > 0.000001f)
			{
				if (sourceSuspensionTickPhase >= 0.999999f)
				{
					sourceSuspensionTickPhase = 0;
					sourceSuspensionTick++;
				}
				float step = Mathf.Min(remainingTicks, 1 - sourceSuspensionTickPhase);
				bool sampleRoughness = sourceSuspensionTickPhase <= 0.000001f &&
					(sourceSuspensionTick & 3) == 0;
				for (int i = 0; i < 4; i++)
				{
					float force = filteredContactLoad[i] - sourceReferenceLoad;
					float roughness = sourceWheelSurfaceRoughness[i];
					if (sampleRoughness && roughness != 0)
					{
						double noise = SourceRandomSigned(roughness + roughness);
						force = (float)((noise - roughness) * currentSourceSpeed * 0.0054f * 3 + force);
					}
					UpdateSourceSuspension(i, force, step);
				}
				sourceSuspensionTickPhase += step;
				remainingTicks -= step;
				if (sourceSuspensionTickPhase >= 0.999999f)
				{
					sourceSuspensionTickPhase = 0;
					sourceSuspensionTick++;
				}
			}
		}
		void UpdateSourceSuspension(int index, float force, float tickScale)
		{
			OriginalTyrePhysicsConfig tyre = parameters.tyres[index];
			float remainingTicks = Mathf.Max(0, tickScale);
			while (remainingTicks > 0)
			{
				float step = Mathf.Min(1, remainingTicks);
				bool outOfCompression = sourceSuspension[index] < 0;
				float damping = outOfCompression ? tyre.dampingOut : tyre.dampingIn;
				float stiffness = outOfCompression ? tyre.stiffnessOut : tyre.stiffnessIn;
				float decay = Mathf.Pow(damping, step);
				float springResponse = Mathf.Abs(1 - damping) > 0.000001f
					? stiffness * (1 - decay) / (1 - damping)
					: stiffness * step;
				sourceSuspension[index] = decay * sourceSuspension[index] + force * springResponse;
				sourceSuspension[index] = Mathf.Clamp(sourceSuspension[index], -tyre.travelOut, tyre.travelIn);
				remainingTicks -= step;
			}
		}
		void LogSourceSuspensionStateAfterStableContact()
		{
			if (sourceSuspensionDiagnosticLogged)
				return;
			int supportingWheels = 0;
			for (int i = 0; i < 4; i++)
				if (sourceProbeWheelTouched[i])
					supportingWheels++;
			sourceSuspensionStableContactTicks = supportingWheels >= 3
				? sourceSuspensionStableContactTicks + 1 : 0;
			if (supportingWheels > 0 && sourceFirstSuspensionContactTime < 0)
				sourceFirstSuspensionContactTime = Time.fixedTime;
			bool stableContact = sourceSuspensionStableContactTicks >= 60;
			float contactObservationTime = sourceFirstSuspensionContactTime < 0
				? 0 : Time.fixedTime - sourceFirstSuspensionContactTime;
			bool contactObservation = !sourceSuspensionObservationLogged && contactObservationTime >= 1;
			if (!stableContact && !contactObservation)
				return;
			float sourceMass = vehicle.rb.mass / SourceMassToKilograms;
			float referenceLoad = sourceMass * SourceGravityMagnitude * 0.25f;
			string wheelStates = string.Empty;
			for (int i = 0; i < 4; i++)
			{
				Wheel wheel = vehicle.wheels[UnityWheelIndex[i]];
				Suspension suspension = wheel.susParent;
				OriginalTyrePhysicsConfig tyre = parameters.tyres[i];
				bool outOfCompression = sourceSuspension[i] < 0;
				float configuredStiffness = outOfCompression ? tyre.stiffnessOut : tyre.stiffnessIn;
				float configuredDamping = outOfCompression ? tyre.dampingOut : tyre.dampingIn;
				float bumpTravelUse = Mathf.Max(0, sourceSuspension[i]) / Mathf.Max(0.001f, tyre.travelIn);
				float unityCompressedFraction = 1 - suspension.compression;
				Vector3 contactNormal = hasSourceWheelContactNormal[i]
					? sourceWheelContactNormal[i] : sourceUpBasis;
				float visualClearance = Vector3.Dot(
					wheel.rim.position - sourceWheelContactPoint[i], contactNormal.normalized) - wheel.actualRadius;
				Vector3 probeCenter = sourceProbeHitsThisTick[i] > 0
					? sourceProbeFirstContactCenter[i]
					: SourceProbeWorldPosition(i, stepStartRotation);
				Vector3 probePoint = sourceProbeHitsThisTick[i] > 0
					? sourceProbeFirstContactPoint[i]
					: sourceWheelContactPoint[i];
				Vector3 probeNormal = sourceProbeHitsThisTick[i] > 0
					? sourceProbeFirstContactNormal[i]
					: contactNormal;
				float probeSurfaceGap = Vector3.Dot(probeCenter - probePoint, probeNormal.normalized) -
					sourceProbeRadius[i] * SourceLengthToMetres;
				float probeToRim = Vector3.Dot(wheel.rim.position - probeCenter, probeNormal.normalized);
				wheelStates += (i == 0 ? string.Empty : "; ") +
					$"w{i}: touched={sourceProbeWheelTouched[i]}, sourceBump={sourceSuspension[i]:F2}/{tyre.travelIn:F2} ({bumpTravelUse:P0}), " +
					$"unityTravelPosition={wheel.travelDist:F3}, unityCompressed={unityCompressedFraction:P0}, " +
					$"cfgSpring={configuredStiffness:F3}, " +
					$"damping={configuredDamping:F2}, " +
					$"suspensionDistance={suspension.suspensionDistance:F3}, radius={wheel.actualRadius:F3}, " +
					$"visualClearance={visualClearance:F3}, probeY={probeCenter.y:F3}, " +
					$"probeToRim={probeToRim:F3}, probeGap={probeSurfaceGap:F3}, " +
					$"load={filteredContactLoad[i]:F2}/" +
					$"{sourceProbeWheelLoad[i]:F2}, anchorY={wheel.susParent.tr.position.y:F3}, " +
					$"rimY={wheel.rim.position.y:F3}, surfaceY={sourceWheelContactPoint[i].y:F3}";
			}
			float bodyLocalY = bodyTransform ? bodyTransform.localPosition.y : 0;
			float averageSuspension = 0.25f * (sourceSuspension[0] + sourceSuspension[1] +
				sourceSuspension[2] + sourceSuspension[3]);
			float bodyClearance = float.NaN;
			Renderer bodyRenderer = bodyTransform ? bodyTransform.GetComponent<Renderer>() : null;
			if (bodyRenderer && supportingWheels > 0)
			{
				Vector3 planePoint = Vector3.zero;
				Vector3 planeNormal = Vector3.zero;
				for (int i = 0; i < 4; i++)
					if (sourceProbeWheelTouched[i])
					{
						planePoint += sourceWheelContactPoint[i];
						planeNormal += sourceWheelContactNormal[i];
					}
				planePoint /= supportingWheels;
				if (planeNormal.sqrMagnitude > 0.0001f)
				{
					planeNormal.Normalize();
					Bounds bounds = bodyRenderer.bounds;
					Vector3 extents = bounds.extents;
					float lowestBodyProjection = Vector3.Dot(bounds.center, planeNormal) -
						Mathf.Abs(planeNormal.x) * extents.x -
						Mathf.Abs(planeNormal.y) * extents.y -
						Mathf.Abs(planeNormal.z) * extents.z;
					bodyClearance = lowestBodyProjection - Vector3.Dot(planePoint, planeNormal);
				}
			}
			//Debug.LogWarning($"[OriginalVehiclePhysics] Suspension state after contact observation: " +
				//$"stableContact={stableContact}, supportingWheels={supportingWheels}/4, " +
				//$"stableTicks={sourceSuspensionStableContactTicks}, " +
				//$"secondsSinceFirstContact={contactObservationTime:F2}, bodyClearance={bodyClearance:F3}, " +
				//$"sourceConfig={parameters.sourceConfig}, rootY={vehicle.rb.position.y:F3}, " +
				//$"verticalSpeed={Vector3.Dot(vehicle.rb.linearVelocity, sourceUpBasis):F2}m/s, " +
				//$"rootEuler={vehicle.rb.rotation.eulerAngles.ToString("F1")}, " +
				//$"rootAngularVelocity={vehicle.rb.angularVelocity.ToString("F2")}, " +
				//$"bodyY={bodyLocalY:F3}, bodyHeave={sourceBodyHeave:F2}, " +
				//$"bodyRoll={sourceBodyRoll:F3}, bodyPitch={sourceBodyPitch:F3}, " +
				//$"rideHeight={parameters.rideHeight:F2}, averageSuspension={averageSuspension:F2}, " +
				//$"comHeight={parameters.comHeight:F2}/{effectiveComHeight:F2}, " +
				//$"sourceMass={sourceMass:F2}, sourceGravity={SourceGravityMagnitude:F3}, " +
				//$"referenceLoad={referenceLoad:F2}, wheelProbeRadius={sourceProbeRadius[0] * SourceLengthToMetres:F3}m, " +
				//$"probeHits={sourceProbeHitsThisTick[0]},{sourceProbeHitsThisTick[1]}," +
				//$"{sourceProbeHitsThisTick[2]},{sourceProbeHitsThisTick[3]}, {wheelStates}", vehicle);
			if (stableContact)
				sourceSuspensionDiagnosticLogged = true;
			else
				sourceSuspensionObservationLogged = true;
		}
		void UpdateOriginalSuspensionPose(float sourceSpeed, float tickScale)
		{
			float left = 0.5f * (sourceSuspension[0] + sourceSuspension[2]);
			float right = 0.5f * (sourceSuspension[1] + sourceSuspension[3]);
			float averageTrack = Mathf.Max(0.001f, 0.5f * (parameters.trackRear + parameters.trackFront));
			float roll = (left - right) / averageTrack;
			sourceBodyRoll = roll + (sourceBodyRoll - roll) * Mathf.Pow(0.5f, tickScale);
			float rear = 0.5f * (sourceSuspension[0] + sourceSuspension[1]);
			float front = 0.5f * (sourceSuspension[2] + sourceSuspension[3]);
			float pitch = -(rear - front) / Mathf.Max(0.001f, parameters.wheelbase);
			if (sourceTurboActive && Mathf.Abs(sourceSpeed) < 50 && pitch < 0)
			{
				pitch *= (turboRpm * 16 / Mathf.Max(1, parameters.turboMax)) /
					(sourceSpeed * 0.1f + 1);
			}
			float trickstartPitch = 0;
			if (startBoostTicks > 0 && sourceStartBoostActiveThisTick)
			{
				// The retail model only amplifies pitch already created by suspension
				// movement. That can be almost zero at a stationary start, so provide
				// the launch pose directly on the rendered body for the 3-second boost.
				float trickstartAge = 1 - Mathf.Clamp01(startBoostTicks / 180f);
				float trickstartAttack = Mathf.SmoothStep(0, 1, trickstartAge / 0.05f);
				float trickstartRelease = 1 - Mathf.SmoothStep(0, 1, trickstartAge);
				trickstartPitch = 0.2f * trickstartAttack * trickstartRelease;
			}
			float pitchDecay = Mathf.Pow(0.95f, tickScale);
			sourceBodyPitch = Mathf.Clamp((pitch * 0.125f + sourceBodyPitch) * pitchDecay,
				-0.3f, 0.3f);
			float displayedBodyPitch = Mathf.Clamp(sourceBodyPitch + trickstartPitch, -0.3f, 0.3f);
			float averageSuspension = 0.25f * (sourceSuspension[0] + sourceSuspension[1] +
				sourceSuspension[2] + sourceSuspension[3]);
			sourceBodyHeave = parameters.rideHeight - averageSuspension;
			if (!bodyTransform)
				return;
			Vector3 bodyUp = new Vector3(sourceBodyRoll, 1, 0).normalized;
			// vehicle_model_pose.cpp stores forward as (0, body_pitch, 1).
			// Keep that sign in Unity so acceleration and braking pitch the chassis
			// in the same direction as the source model pose.
			Vector3 bodyForward = new Vector3(0, displayedBodyPitch, 1).normalized;
			Quaternion sourcePose = Quaternion.LookRotation(bodyForward, bodyUp);
			// The retail renderer adds body_heave to the model's local Y position
			// (vehicle_model_pose.cpp / source_pmd_vehicle_pose.cpp). Positive
			// suspension compression therefore lowers the body relative to the
			// wheel anchors; subtracting this value raised the body instead.
			bodyTransform.localPosition = initialBodyLocalPosition + Vector3.up *
				(sourceBodyHeave * SourceLengthToMetres);
			// sourcePose is expressed in the vehicle's local axes. Apply it outside
			// the prefab's static mesh alignment so cars whose body mesh is rotated
			// (car17 is aligned by 180 degrees around Y) still lean with the vehicle.
			bodyTransform.localRotation = sourcePose * initialBodyLocalRotation;
		}
		void ApplyTyres(float tickScale, int sourceContactClass)
		{
			System.Array.Clear(sourceWheelRotationDeltaThisTick, 0, sourceWheelRotationDeltaThisTick.Length);
			sourceAccelerationTiltDeltaThisTick = Vector3.zero;
			// vehicle_dynamics_core resolves tyre forces into front and rear axle
			// velocities. Accumulating four AddForceAtPosition calls adds artificial
			// roll/yaw impulses that the original axle model never generated.
			for (int i = 0; i < wheelSpeed.Length; i++)
			{
				float integratedWheelSpeed = (float)((double)wheelAcceleration[i] * tickScale + wheelSpeed[i]);
				wheelSpeed[i] = Mathf.Clamp(integratedWheelSpeed, -416.66666f, 416.66666f);
				wheelAcceleration[i] = 0;
			}
			Vector3 forward = sourceForwardBasis;
			Vector3 right = sourceRightBasis;
			Vector3 up = sourceUpBasis;
			// The source builds vehicle_rotation(1, steering) in the fixed world
			// coordinate frame, then applies it to the world-space axle basis.
			Quaternion steering = Quaternion.AngleAxis(sourceSteering, Vector3.up);
			Vector3 steeredForward = steering * forward;
			Vector3 steeredRight = steering * right;
			Vector3 sourceVelocity = vehicle.rb.linearVelocity / SourceSpeedToMetresPerSecond;
			float sideSpeed = Vector3.Dot(steeredRight, sourceVelocity);
			float frontForce = Mathf.Clamp(-sideSpeed * Vector3.Dot(steeredRight, forward), -0.5f, 0.5f);
			float rearForce = 0;
			float sourceMass = sourceEffectiveMass;
			float speed = Vector3.Dot(forward, sourceVelocity);
			float frontSpeed = Vector3.Dot(steeredForward, sourceVelocity);
			float transfer = -speed * Vector3.Dot(forward, steeredRight);
			float lateral = -transfer * Vector3.Dot(steeredRight, right);
			float originalSide = Vector3.Dot(right, sourceVelocity);
			float frontLateral = (1 - parameters.comA) * lateral + originalSide;
			float rearLateral = (1 - parameters.comB) * lateral + originalSide;
			sourceLateralSpeed[0] = sourceLateralSpeed[1] = rearLateral;
			sourceLateralSpeed[2] = sourceLateralSpeed[3] = frontLateral;
			sourceLongitudinalSpeed[0] = sourceLongitudinalSpeed[1] = speed;
			sourceLongitudinalSpeed[2] = sourceLongitudinalSpeed[3] = frontSpeed;
			sourceAxleForce[0] = frontForce;
			sourceAxleForce[1] = 0;
			for (int i = 0; i < 4; i++)
			{
				Wheel wheel = vehicle.wheels[UnityWheelIndex[i]];
				sourceWheelSlip[i] = 0;
				sourceWheelSideSlip[i] = 0;
				sourceWheelGripUsage[i] = 0;
				// The source has four permanent wheel states. Tire response is gated
				// by source contact load, never by the remake wheel's detachable-mesh
				// or connected flag.
				if (filteredContactLoad[i] <= 8.515625f)
				{
					wheel.currentRPM = wheelSpeed[i] * SourceSpeedToMetresPerSecond * 60 /
						(Mathf.Max(0.001f, parameters.tyres[i].radius * SourceLengthToMetres) * 2 * Mathf.PI);
					continue;
				}
				float longitudinal = sourceLongitudinalSpeed[i];
				float slip = wheelSpeed[i] - longitudinal;
				float wheelLateral = sourceLateralSpeed[i];
				double magnitude = System.Math.Sqrt((double)wheelLateral * wheelLateral +
					(double)slip * slip * 0.64000005f);
				float storedMagnitude = (float)magnitude;
				float load = (float)((double)filteredContactLoad[i] /
					((double)sourceEffectiveMass * 0.25));
				float grip = sourceWheelSurfaceGrip[i];
				float staticFriction = (float)((double)sourceStaticFriction[i] * grip);
				float kineticFriction = (float)((double)sourceKineticFriction[i] * grip);
				double available = (double)load * SourceCurve(parameters.frictionCurve, magnitude);
				double longitudinalAvailable = available;
				if (F.I && F.I.s_raceType == RaceType.Drift && throttleSignal > 0.1f && slip > 0)
				{
					// Drift mode needs drive traction while the tyre is already using
					// lateral grip. Evaluate the drive part of the friction budget from
					// longitudinal slip alone, while keeping combined slip for sideways
					// response and grip telemetry. Other race modes retain source behavior.
					longitudinalAvailable = (double)load * SourceCurve(parameters.frictionCurve,
						System.Math.Abs((double)slip) * 0.8);
				}
				double threshold = available * staticFriction;
				if (System.Math.Abs(threshold) < 0.01f)
					threshold = 0.01f;
				sourceWheelGripUsage[i] = (float)System.Math.Abs((double)storedMagnitude / threshold);
				float longitudinalResidual = slip;
				float lateralResidual;
				if (storedMagnitude <= threshold)
					lateralResidual = 0;
				else
				{
					double lateralChange = System.Math.Min(System.Math.Abs((double)wheelLateral), available) * kineticFriction;
					lateralResidual = (float)(wheelLateral < 0
						? wheelLateral + lateralChange : wheelLateral - lateralChange);
					if (slip > 0)
						longitudinalAvailable *= 3;
					float slipCorrection = (float)System.Math.Min(System.Math.Abs((double)slip), longitudinalAvailable);
					longitudinalResidual = (float)((double)slipCorrection * kineticFriction *
						(slip < 0 ? -1.0 : 1.0));
				}
				float longitudinalChange = (float)((double)longitudinalResidual * 0.5);
				wheelSpeed[i] = (float)((double)wheelSpeed[i] - (double)longitudinalChange * tickScale);
				sourceLateralSpeed[i] = lateralResidual;
				int axle = i < 2 ? 1 : 0;
				sourceAxleForce[axle] = (float)((double)longitudinalChange + sourceAxleForce[axle]);
				double roadSpeed = System.Math.Abs((double)longitudinal) * 1.3424487f;
				double resistance = -((roadSpeed * roadSpeed * 0.000035f + 0.15f) *
					(1.0 / parameters.tyres[i].pressure) + 0.005f);
				resistance = resistance * filteredContactLoad[i] /
					((double)sourceEffectiveMass * 0.25) * sourceWheelSurfaceRolling[i] * 100 * 0.00027777778f;
				if (System.Math.Abs((double)longitudinal) < System.Math.Abs(resistance))
					resistance = -System.Math.Abs((double)longitudinal);
				if (wheelSpeed[i] < 0)
					resistance = -resistance;
				float storedResistance = (float)resistance;
				wheelSpeed[i] = (float)((double)storedResistance * tickScale + wheelSpeed[i]);
				sourceAxleForce[axle] = (float)((double)storedResistance * 0.5 + sourceAxleForce[axle]);
				wheel.currentRPM = wheelSpeed[i] * SourceSpeedToMetresPerSecond * 60 /
					(Mathf.Max(0.001f, parameters.tyres[i].radius * SourceLengthToMetres) * 2 * Mathf.PI);
				Quaternion beforeWheelRotation = sourceIncrementalRotation;
				ApplyWheelContactRotation(i, sourceMass);
				sourceWheelRotationDeltaThisTick[i] = RotationVector(
					sourceIncrementalRotation * Quaternion.Inverse(beforeWheelRotation));
			}
			frontForce = 0.5f * sourceAxleForce[0];
			rearForce = 0.5f * sourceAxleForce[1];
			float frontSide = 0.5f * (sourceLateralSpeed[3] + sourceLateralSpeed[2]);
			float rearSide = 0.5f * (sourceLateralSpeed[1] + sourceLateralSpeed[0]);
			// vehicle_dynamics_core uses total speed only in contact class 1
			// (all four contact grace counters expired); other classes use forward speed.
			float sourceSpeed = sourceContactClass == 1 ? sourceVelocity.magnitude : speed;
			float dragSpeed = sourceSpeed * SourceSpeedToMetresPerSecond;
			float drag = dragSpeed * dragSpeed * sourceAirFactor * parameters.frontalArea * parameters.dragCoefficient * 0.6128368f;
			if (SourceUpgradeActive)
				drag += (1 - SourceUpgradeCondition) * SourceCpuAerodynamicDragModifier;
			float dragStep = drag / sourceMass * 100 * 0.00027777778f * tickScale;
			frontForce -= dragStep;
			rearForce -= dragStep;
			for (int i = 0; i < wheelSpeed.Length; i++)
				wheelSpeed[i] -= dragStep;
			Vector3 correction = -right * lateral;
			Vector3 frontAxleVelocity = forward * (speed + frontForce) + right * frontSide + correction;
			Vector3 rearAxleVelocity = forward * (speed + rearForce) + right * rearSide;
			Vector3 previousVelocity = vehicle.rb.linearVelocity;
			float verticalVelocity = Vector3.Dot(previousVelocity, up);
			Vector3 nextVelocity = (frontAxleVelocity + rearAxleVelocity) * (0.5f * SourceSpeedToMetresPerSecond);
			nextVelocity += up * verticalVelocity;
			vehicle.rb.linearVelocity = nextVelocity;
			// The source advances each axle independently, then rebuilds its basis
			// from their new separation and the support up vector.
			Vector3 axleDisplacement = (frontAxleVelocity - rearAxleVelocity) * tickScale;
			if (sourceContactClass == 0)
			{
				// vehicle.cpp's bank_displacement shifts the front support point
				// under lateral gravity while the contact class is 0.
				Vector3 sourceGravity = SourceGravityDirection * SourceGravityMagnitude;
				float lateralGravity = Vector3.Dot(sourceGravity, right);
				if (Mathf.Abs(lateralGravity) >= 0.2079116f)
				{
					if (speed < 1)
						lateralGravity = 0;
					float bankOffset = sourceMass * parameters.comA * lateralGravity * 0.003125f;
					axleDisplacement += right * (bankOffset * tickScale);
				}
			}
			Vector3 axleDelta = axleDisplacement * SourceLengthToMetres;
			Vector3 newForward = forward * (parameters.wheelbase * SourceLengthToMetres) + axleDelta;
			if (newForward.sqrMagnitude > 0.000001f && up.sqrMagnitude > 0.000001f)
			{
				newForward.Normalize();
				stepRotation = Quaternion.LookRotation(newForward, up);
				stepRotationChanged = true;
			}
			sourceForwardBasis = newForward.sqrMagnitude > 0.000001f ? newForward : stepRotation * Vector3.forward;
			sourceRightBasis = Vector3.Cross(sourceUpBasis, sourceForwardBasis).normalized;
			Vector3 acceleration = (nextVelocity - previousVelocity) / SourceSpeedToMetresPerSecond;
			float sourcePitch = Vector3.Dot(acceleration, sourceForwardBasis) *
				effectiveComHeight / Mathf.Max(1, parameters.wheelbase);
			float sourceRoll = Vector3.Dot(acceleration, sourceRightBasis) *
				effectiveComHeight / Mathf.Max(1, (parameters.trackRear + parameters.trackFront) * 0.5f);
			if (Mathf.Abs(sourcePitch) + Mathf.Abs(sourceRoll) > 0.0001f)
			{
				// acceleration_tilt writes a world-space correction into the
				// incremental matrix after the core has rebuilt its orientation.
				Quaternion localTilt = Quaternion.AngleAxis(-sourceRoll, Vector3.forward) *
					Quaternion.AngleAxis(sourcePitch, Vector3.right);
				Quaternion worldTilt = stepRotation * localTilt * Quaternion.Inverse(stepRotation);
				Quaternion beforeAccelerationTilt = sourceIncrementalRotation;
				sourceIncrementalRotation = (worldTilt * sourceIncrementalRotation).normalized;
				sourceAccelerationTiltDeltaThisTick = RotationVector(
					sourceIncrementalRotation * Quaternion.Inverse(beforeAccelerationTilt));
			}
			float finalSpeed = sourceContactClass == 1
				? nextVelocity.magnitude / SourceSpeedToMetresPerSecond
				: Vector3.Dot(sourceForwardBasis, nextVelocity) / SourceSpeedToMetresPerSecond;
			currentSourceSpeed = finalSpeed;
			for (int i = 0; i < 4; i++)
			{
				Wheel wheel = vehicle.wheels[UnityWheelIndex[i]];
				sourceWheelSlip[i] = Mathf.Abs(finalSpeed) < 1e-5f ? 0 : wheelSpeed[i] / finalSpeed - 1;
				sourceWheelSideSlip[i] = sourceLateralSpeed[i] / Mathf.Max(1, Mathf.Abs(finalSpeed));
			}
			// vehicle.cpp applies the retail wheel speed decay in the four-wheel
			// contact class.
			if (sourceContactClass == 1)
			{
				float wheelDecay = Mathf.Pow(0.995f, tickScale);
				for (int i = 0; i < wheelSpeed.Length; i++)
					wheelSpeed[i] *= wheelDecay;
			}
		}
		void ApplyWheelContactRotation(int sourceIndex, float sourceMass)
		{
			// vehicle.cpp wheel_rotation converts each wheel's residual tangential
			// movement into a support rotation. The retail implementation uses the
			// first wheel's contact normal for all four wheels; retain that behavior.
			Vector3 sourceLocal = sourceWheelLocal[sourceIndex];
			// wheel_rotation applies the source incremental matrix directly to the
			// source-local probe vector. The body orientation is used only for the
			// world-space support point passed to support_rotation.
			Vector3 transformedLocal = sourceIncrementalRotation * sourceLocal;
			Vector3 tangent = transformedLocal - sourceLocal;
			Vector3 normal = hasSourceWheelContactNormal[0]
				? sourceWheelContactNormal[0] : Vector3.zero;
			// wheel_rotation reads wheel zero's stored normal without a fallback;
			// before its first contact that value is the zero vector.
			float projection = Vector3.Dot(tangent, normal);
			tangent -= normal * projection;
			float tangentLength = tangent.magnitude;
			Vector3 direction = tangentLength > 0.000001f ? tangent / tangentLength : Vector3.zero;
			Vector3 supportRadial = previousSourceProbeWorld[sourceIndex] - vehicle.rb.position;
			float radius = sourceLocal.magnitude;
			if (radius <= 0.0001f)
				return;
			float sourceGravity = SourceGravityMagnitude;
			float referenceLoad = sourceMass * sourceGravity * 0.25f;
			float mass = sourceMass * 0.25f;
			float load = Mathf.Min(filteredContactLoad[sourceIndex], referenceLoad);
			float force = tangentLength * mass;
			if (force > load * sourceStaticFriction[sourceIndex])
				force = load * sourceKineticFriction[sourceIndex] * 0.5f;
			float angle = force / (mass * radius);
			Vector3 side = Vector3.Cross(sourceUpBasis, sourceLocal).normalized;
			if (sourceUpBasis.y < 0)
				angle = -angle;
			if (Vector3.Dot(direction, side) >= 0)
				angle = -angle;
			ApplyWheelSupportRotation(supportRadial, angle);
		}
		void ApplyWheelSupportRotation(Vector3 radial, float angle)
		{
			float radius = radial.magnitude;
			if (radius <= 0.000001f)
				return;
			Vector3 axis = SupportRotationAxis(radial, radius);
			if (axis.sqrMagnitude <= 0.000001f || Mathf.Abs(angle) <= 0.000001f)
				return;
			// source support_rotation's axis-angle sign is opposite Unity's
			// right-handed quaternion convention after the vehicle Y-axis mapping.
			sourceIncrementalRotation = (Quaternion.AngleAxis(-angle * Mathf.Rad2Deg, axis) *
				sourceIncrementalRotation).normalized;
		}
		public void RegisterRailbarSupport(RailbarLogic railbar)
		{
			// Trigger callbacks run after physics; consume support in the next
			// source tick, where its orientation and velocity are actually resolved.
			sourceRailbar = railbar;
			sourceRailbarSeenTime = Time.fixedTime;
		}
		void ApplyRailbarSupport()
		{
			if (!sourceRailbar || sourceRailControlsActive ||
				Time.fixedTime - sourceRailbarSeenTime > Time.fixedDeltaTime * 1.5f ||
				!sourceRailbar.TryGetSupport(vehicle, out Vector3 axis, out Vector3 normal))
			{
				sourceRailbarSupportedTime = float.NegativeInfinity;
				return;
			}
			sourceRailbarSupportedTime = Time.fixedTime;
			float lateralBlend = 1 - Mathf.Exp(-sourceRailbar.lateralDamping * Time.fixedDeltaTime);
			float rotationBlend = 1 - Mathf.Exp(-sourceRailbar.rotationDamping * Time.fixedDeltaTime);
			Vector3 velocity = vehicle.rb.linearVelocity;
			Vector3 along = axis * Vector3.Dot(velocity, axis);
			Vector3 away = normal * Mathf.Max(0, Vector3.Dot(velocity, normal));
			Vector3 sideways = Vector3.ProjectOnPlane(velocity - along, normal);
			vehicle.rb.linearVelocity = along + away + sideways * (1 - lateralBlend);
			// Preserve yaw relative to the rail: sideways grinds must stay sideways.
			Vector3 forward = Vector3.ProjectOnPlane(stepRotation * Vector3.forward, normal);
			if (forward.sqrMagnitude > 0.000001f)
			{
				stepRotation = Quaternion.Slerp(stepRotation,
					Quaternion.LookRotation(forward.normalized, normal), rotationBlend);
				stepRotationChanged = true;
			}
			sourceIncrementalRotation = Quaternion.Slerp(sourceIncrementalRotation,
				Quaternion.identity, rotationBlend);
			sourceForwardBasis = stepRotation * Vector3.forward;
			sourceRightBasis = stepRotation * Vector3.right;
			sourceUpBasis = stepRotation * Vector3.up;
			// A supported grind is not free-flight stunt rotation. Landing on the
			// rail clears residual flip/roll speed; leaving it restores air control.
			stuntActive = false;
			pitchAcceleration = yawAcceleration = pitchSpeed = yawSpeed = 0;
			ResetSourceStuntRoll();
			airTicks = 0;
		}
		void ApplyAerodynamics(float tickScale, int sourceContactClass)
		{
			Vector3 velocity = vehicle.rb.linearVelocity;
			Vector3 sourceForward = sourceForwardBasis;
			Vector3 sourceUp = sourceUpBasis;
			float sourceSpeed = sourceContactClass == 1
				? velocity.magnitude / SourceSpeedToMetresPerSecond
				: Vector3.Dot(velocity, sourceForward) / SourceSpeedToMetresPerSecond;
			if (sourceContactClass != 0 || Mathf.Abs(sourceSpeed) <= 0.001f)
				return;
			double speedMetres = (double)sourceSpeed * SourceSpeedToMetresPerSecond;
			double lift = speedMetres * speedMetres * sourceAirFactor * parameters.frontalArea *
				parameters.liftCoefficient * (double)-0.615f;
			float liftStep = (float)(lift / ((double)sourceEffectiveMass * 4) *
				SourceSpeedToMetresPerSecond * tickScale);
			vehicle.rb.linearVelocity += sourceUp * liftStep;
		}
		void UpdateAirMotion(float tickScale)
		{
			// steer_air_velocity tests the contact counter before the air tick
			// decrements it. A vehicle on its final contact tick must not steer.
			bool hadContact = sourceContactCounter > 0;
			if (!hadContact && !stuntActive)
			{
				Vector3 velocity = vehicle.rb.linearVelocity;
				float sourceSpeed = velocity.magnitude / SourceSpeedToMetresPerSecond;
				if (sourceSpeed >= 1)
				{
					Vector3 sourceGravityDirection = SourceGravityDirection;
					Vector3 side = Vector3.Cross(sourceGravityDirection, velocity.normalized);
					// UpdateSteering stores Unity's right-positive steering sign, while
					// the source air-velocity model expects positive steering for left.
					float steering = -Mathf.Clamp(steeringDegrees, -5, 5) * 0.2f;
					float amount = Mathf.Min(1, sourceSpeed * 0.012f) * steering;
					vehicle.rb.linearVelocity += side * (amount * SourceSpeedToMetresPerSecond * tickScale);
				}
			}
			if (sourceContactCounter > 0)
				sourceContactCounter = Mathf.Max(0, sourceContactCounter - tickScale);
			if (sourceContactCounter > 0)
			{
				airTicks = 0;
				stuntActive = false;
				pitchAcceleration = yawAcceleration = 0;
				pitchSpeed = yawSpeed = 0;
				ResetSourceStuntRoll();
				ResetSourceStuntPhaseHistory();
				return;
			}
			if (hadContact)
			{
				ResetSourceStuntPhaseHistory();
			}
			else
				UpdateSourceStuntPhaseHistory();
			bool launchedThisTick = hadContact && TryLaunchOnTakeoff(Mathf.RoundToInt(Time.fixedTime /
				Mathf.Max(0.0001f, Time.fixedDeltaTime)));
			airTicks += tickScale;
			if (airTicks < 16)
				LevelSourceIncrementalRotation(0.9f);
			if (!stuntActive)
				return;
			float stuntBrakeInput = SourcePlayerControlsSuppressed
				? 0 : SourceAiAirBrakeInput;
			float stuntThrottleInput = SourcePlayerControlsSuppressed
				? 0 : SourceAiAirThrottleInput;
			float stuntSteeringInput = SourcePlayerControlsSuppressed
				? 0 : SourceSteerInput;
			float stuntRollInput = SourcePlayerControlsSuppressed ? 0 : SourceAiAirRollInput;
			float mass = Mathf.Max(1, parameters.mass);
			float pitchLimit = Mathf.Min(12, parameters.maxPitchSpeed / mass);
			float yawLimit = Mathf.Min(12, parameters.maxYawSpeed / mass);
			float rollLimit = pitchLimit;
			bool pitchCommand = stuntBrakeInput > 0.5f || stuntThrottleInput > 0.5f;
			bool rollCommand = (vehicle.SGPshiftbutton > 0 || sourceAiStuntInputThisTick) &&
				Mathf.Abs(stuntRollInput) > 0.2f;
			if (!rollCommand && !stuntRollActive)
				stuntRollInputArmed = true;
			if (!pitchCommand && rollCommand && stuntRollInputArmed && !stuntRollActive)
			{
				stuntRollInputArmed = false;
				stuntRollActive = true;
				stuntRollDirection = stuntRollInput > 0 ? -1 : 1;
				stuntRollProgress = 0;
				rollAcceleration = rollSpeed = 0;
			}
			bool pitchProcessed = false;
			bool rollProcessed = rollCommand || stuntRollActive;
			bool yawProcessed = false;
			if (!launchedThisTick && pitchCommand && Mathf.Abs(yawSpeed) < 0.2f &&
				stuntBrakeInput > 0.5f)
			{
				pitchAcceleration = Mathf.Min(0.8f, pitchAcceleration + 0.8f * tickScale);
				pitchSpeed = Mathf.Min(pitchLimit, pitchSpeed + pitchAcceleration * tickScale);
				pitchProcessed = true;
			}
			else if (!launchedThisTick && pitchCommand && Mathf.Abs(yawSpeed) < 0.2f &&
				stuntThrottleInput > 0.5f)
			{
				pitchAcceleration = Mathf.Max(-0.8f, pitchAcceleration - 0.8f * tickScale);
				pitchSpeed = Mathf.Max(-pitchLimit, pitchSpeed + pitchAcceleration * tickScale);
				pitchProcessed = true;
			}
			if (!launchedThisTick && !pitchCommand && !rollCommand &&
				Mathf.Abs(pitchSpeed) < 0.2f && stuntSteeringInput < -0.5f)
			{
				yawAcceleration = Mathf.Min(0.8f, yawAcceleration + 0.8f * tickScale);
				yawSpeed = Mathf.Min(yawLimit, yawSpeed + yawAcceleration * tickScale);
				yawProcessed = true;
			}
			else if (!launchedThisTick && !pitchCommand && !rollCommand &&
				Mathf.Abs(pitchSpeed) < 0.2f && stuntSteeringInput > 0.5f)
			{
				yawAcceleration = Mathf.Max(-0.8f, yawAcceleration - 0.8f * tickScale);
				yawSpeed = Mathf.Max(-yawLimit, yawSpeed + yawAcceleration * tickScale);
				yawProcessed = true;
			}
			if (!launchedThisTick && !pitchProcessed && !rollProcessed && !yawProcessed)
			{
				pitchAcceleration = yawAcceleration = rollAcceleration = 0;
				AssistAirPitchAndYaw();
			}
			float rollAngleThisTick = 0;
			if (stuntRollActive && !launchedThisTick)
			{
				rollAcceleration = Mathf.Min(0.8f, rollAcceleration + 0.8f * tickScale);
				rollSpeed = Mathf.Min(rollLimit, rollSpeed + rollAcceleration * tickScale);
				float remainingRoll = Mathf.Max(0, 360 - stuntRollProgress);
				float requestedRoll = Mathf.Min(remainingRoll, rollSpeed * tickScale);
				rollAngleThisTick = stuntRollDirection * requestedRoll;
				stuntRollProgress += requestedRoll;
				if (stuntRollProgress >= 360)
				{
					stuntRollActive = false;
					stuntRollDirection = 0;
					rollAcceleration = rollSpeed = 0;
				}
			}
			// Match SGP_Evo's pitch/roll signs; source steering yaw remains left-handed.
			Quaternion rotation = stepRotation;
			rotation *= Quaternion.AngleAxis(pitchSpeed * tickScale, Vector3.right);
			rotation *= Quaternion.AngleAxis(-yawSpeed * tickScale, Vector3.up);
			rotation *= Quaternion.AngleAxis(rollAngleThisTick, Vector3.forward);
			stepRotation = rotation;
			stepRotationChanged = true;
		}
		void LevelSourceIncrementalRotation(float divisor)
		{
			Vector3 x = sourceIncrementalRotation * Vector3.right;
			Vector3 y = sourceIncrementalRotation * Vector3.up;
			Vector3 z = sourceIncrementalRotation * Vector3.forward;
			float scale = 1 / divisor;
			x.x *= scale;
			y.y *= scale;
			z.z *= scale;
			x.Normalize();
			y.Normalize();
			z.Normalize();
			if (z.sqrMagnitude > 0.000001f && y.sqrMagnitude > 0.000001f)
				sourceIncrementalRotation = Quaternion.LookRotation(z, y);
		}
		void AssistAirPitchAndYaw()
		{
			// vehicle_stunt.cpp::assist_air_pitch / assist_air_yaw run only when
			// neither stunt axis received input during this source tick.
			Vector3 gravity = SourceGravityDirection;
			if (Vector3.Dot(gravity, sourceUpBasis) < 0 && Mathf.Abs(pitchSpeed) >= 0.05f)
			{
				float pitch = Mathf.Abs(Vector3.Dot(gravity, sourceForwardBasis));
				if (pitchSpeed < 0)
					pitch = -pitch;
				pitchSpeed = pitch * 6;
			}
			Vector3 velocity = vehicle.rb.linearVelocity;
			if (Vector3.Dot(sourceForwardBasis, velocity) > 0 && Mathf.Abs(yawSpeed) >= 0.05f &&
				velocity.sqrMagnitude > 0.000001f)
			{
				float yaw = Mathf.Abs(Vector3.Dot(velocity.normalized, sourceRightBasis));
				if (yawSpeed < 0)
					yaw = -yaw;
				yawSpeed = yaw * 6;
			}
		}
	}
}
