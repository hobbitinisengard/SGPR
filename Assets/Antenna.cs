using RVP;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class Antenna : MonoBehaviour
{
	public Transform goal;
	protected VehicleParent vp;
	protected Transform follower;
	protected Vector3 goal_vel;
	protected Vector3 follower_vel = Vector3.zero;
	[SerializeField, Min(0.01f)] protected float halflife = .2f;
	[SerializeField, Min(0.01f)] protected float frequency = 2f;
	protected Vector3 goal_pos;
	protected Vector3 goal_prevPos;
	protected Vector3 goal_initPos;

	public float targetHeight = 0.26f;
	[Header("Antenna motion")]
	[Tooltip("Bending in metres per m/s squared of acceleration.")]
	[Min(0)] public float inertia = .012f;
	[Tooltip("Backward bending in metres per m/s of vehicle speed.")]
	[Min(0)] public float airDrag = .0015f;
	[Tooltip("Maximum sideways or forward/backward deflection in metres.")]
	[Min(0)] float maxDeflection = 1;
	[Tooltip("Time in seconds used to smooth acceleration impulses.")]
	[Min(.01f)] public float accelerationSmoothing = .1f;

	Transform[] antennaNodes;
	Vector3 restOffset;
	Vector3 previousVelocity;
	Vector3 smoothedAcceleration;
	bool motionInitialized;
	bool restOffsetInitialized;
	bool ownsGoal;

	public void Reset()
	{
		motionInitialized = false;
		follower_vel = goal_vel = smoothedAcceleration = Vector3.zero;
		if (!vp || !goal || !follower) return;
		goal_pos = goal_prevPos = restOffsetInitialized
			? vp.tr.InverseTransformPoint(transform.position) + restOffset : goal_initPos;
		goal.localPosition = goal_pos;
		follower.position = goal.position;
	}

	protected void OnDestroy()
	{
		if (follower) Destroy(follower.gameObject);
		if (ownsGoal && goal) Destroy(goal.gameObject);
	}

	void Start()
	{
		vp = transform.GetTopmostParentComponent<VehicleParent>();
		if (!vp) return;
		vp.antenna = this;
		antennaNodes = new Transform[transform.childCount];
		for (int i = 0; i < antennaNodes.Length; i++)
			antennaNodes[i] = transform.GetChild(i);

		if (!goal)
		{
			goal = new GameObject("Goal").transform;
			goal.position = transform.position + vp.tr.up * targetHeight * (antennaNodes.Length + 1);
			ownsGoal = true;
		}
		goal.SetParent(vp.tr, true);
		goal_initPos = goal.localPosition;
		follower = new GameObject("Follower").transform;
		follower.gameObject.layer = 2;
		follower.SetParent(vp.tr, false);
		Reset();
	}

	protected void FixedUpdate()
	{
		if (!vp || !vp.rb || !goal || !follower || frequency <= 0 || halflife <= 0) return;
		Update_Follower();
	}

	void LateUpdate()
	{
		if (!vp || !follower || antennaNodes == null || antennaNodes.Length == 0) return;
		Vector3 up = vp.tr.up * targetHeight;
		for (int i = 0; i < antennaNodes.Length - 1; i++)
		{
			Vector3 direction = (follower.position - antennaNodes[i].position).normalized * targetHeight;
			antennaNodes[i + 1].position = antennaNodes[i].position +
				Vector3.Slerp(up, direction, Easing.OutCubic((float)i / (antennaNodes.Length + 1)));
			LookAt(antennaNodes[i], antennaNodes[i + 1].position);
		}
		LookAt(antennaNodes[antennaNodes.Length - 1], follower.position);
	}

	static void LookAt(Transform node, Vector3 target)
	{
		if ((target - node.position).sqrMagnitude > .000001f) node.LookAt(target);
	}

	protected void Update_Follower()
	{
		float dt = Time.fixedDeltaTime;
		if (dt <= 0) return;
		Vector3 basePosition = vp.tr.InverseTransformPoint(transform.position);
		if (!restOffsetInitialized)
		{
			restOffset = goal_initPos - basePosition;
			restOffsetInitialized = true;
		}
		Vector3 restPosition = vp.tr.TransformPoint(basePosition + restOffset);
		// Sample world velocity before transforming acceleration: differentiating
		// local velocity alone loses centripetal acceleration in corners.
		Vector3 velocity = vp.rb.linearVelocity +
			Vector3.Cross(vp.WorldAngularVelocity, restPosition - vp.rb.worldCenterOfMass);
		if (!OriginalVehiclePhysics.IsFinite(velocity)) { Reset(); return; }
		if (!motionInitialized)
		{
			previousVelocity = velocity;
			motionInitialized = true;
		}
		Vector3 acceleration = Vector3.ClampMagnitude((velocity - previousVelocity) / dt, 150f);
		previousVelocity = velocity;
		smoothedAcceleration = Vector3.Lerp(smoothedAcceleration, acceleration,
			1f - Mathf.Exp(-dt / Mathf.Max(.01f, accelerationSmoothing)));
		Vector3 offset = -vp.tr.InverseTransformVector(smoothedAcceleration * inertia + velocity * airDrag);
		Vector3 restDirection = restOffset.sqrMagnitude > .000001f ? restOffset.normalized : Vector3.up;
		offset = Vector3.ClampMagnitude(Vector3.ProjectOnPlane(offset, restDirection),
			Mathf.Min(maxDeflection, restOffset.magnitude * .6f));
		goal_prevPos = goal_pos;
		// Keep the tip on an arc around its mounting point instead of stretching it.
		goal_pos = basePosition + (restOffset + offset).normalized * restOffset.magnitude;
		goal_vel = (goal_pos - goal_prevPos) / dt;
		goal.localPosition = goal_pos;

		Vector3 position = follower.localPosition;
		Spring(ref position, ref follower_vel, goal_pos, dt);
		Vector3 displacement = position - (basePosition + restOffset);
		float limit = Mathf.Min(maxDeflection, restOffset.magnitude * .6f);
		if (displacement.sqrMagnitude > limit * limit)
		{
			Vector3 normal = displacement.normalized;
			position = basePosition + restOffset + normal * limit;
			follower_vel -= normal * Mathf.Max(0, Vector3.Dot(follower_vel, normal));
		}
		follower.localPosition = position;
	}

	void Spring(ref Vector3 position, ref Vector3 velocity, Vector3 target, float dt)
	{
		// Exact damped spring step, stable at different fixed update rates.
		float omega = 2f * Mathf.PI * frequency;
		float decay = Mathf.Log(2f) / Mathf.Max(.01f, halflife);
		float difference = omega * omega - decay * decay;
		Vector3 displacement = position - target;
		if (difference > .0001f)
		{
			float w = Mathf.Sqrt(difference);
			float sine = Mathf.Sin(w * dt) / w;
			float cosine = Mathf.Cos(w * dt);
			float envelope = Mathf.Exp(-decay * dt);
			position = target + envelope * (displacement * cosine + (velocity + decay * displacement) * sine);
			velocity = envelope * (velocity * cosine - (decay * velocity + omega * omega * displacement) * sine);
		}
		else if (difference < -.0001f)
		{
			float w = Mathf.Sqrt(-difference);
			float r0 = -decay + w, r1 = -decay - w;
			Vector3 a = (velocity - r1 * displacement) / (r0 - r1);
			Vector3 b = displacement - a;
			position = target + a * Mathf.Exp(r0 * dt) + b * Mathf.Exp(r1 * dt);
			velocity = a * r0 * Mathf.Exp(r0 * dt) + b * r1 * Mathf.Exp(r1 * dt);
		}
		else
		{
			Vector3 c = velocity + decay * displacement;
			float envelope = Mathf.Exp(-decay * dt);
			position = target + (displacement + c * dt) * envelope;
			velocity = (velocity - decay * c * dt) * envelope;
		}
	}
}
