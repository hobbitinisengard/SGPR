using UnityEngine;
using UnityEngine.UIElements.Experimental;

// Worm models need to have their armature to be Z up.
// The fbx model needs armature export settings: Primary Bone +Z, Secondary Bone -X
public class WormAntenna2 : MonoBehaviour
{
	public Transform goal;
	public Transform root;
	Vector3 goal_vel = Vector3.zero;
	Transform follower;
	Vector3 follower_vel = Vector3.zero;

	public float halflife = 1; //drag
	public float frequency = 2; //Spring
	static Vector3[] initLookNext;
	static Vector3[] initPoss;
	static float[] initDists;
	const int nodes_number = 3;

	void Start()
	{
		if (initLookNext == null)
		{
			initLookNext = new Vector3[nodes_number];
			initDists = new float[nodes_number - 1];
			initPoss = new Vector3[nodes_number - 1];

			Transform node = root;
			for (int i = 0; i < nodes_number; ++i)
			{
				initLookNext[i] = node.GetChild(0).transform.position - node.transform.position;
				if (i < nodes_number - 1)
				{
					initDists[i] = Vector3.Distance(node.GetChild(0).transform.position, node.transform.position);
					initPoss[i] = transform.InverseTransformPoint(node.position);
				}
				node = node.GetChild(0);
			}
		}
		Transform lastNode = transform;
		for (int i = 0; i <= nodes_number; ++i)
			lastNode = lastNode.GetChild(0);

		follower = new GameObject("Follower").transform;
		follower.gameObject.layer = 2;
		follower.position = goal.position;
	}
	private void FixedUpdate()
	{
		if (halflife == 0 || frequency == 0) return;
			Update_Follower();
	}
	private void LateUpdate()
	{
		Transform node = root;
		Vector3 localUp = transform.up;
		Vector3 followerPos = transform.InverseTransformPoint(follower.position);
		for (int i = 0; i < nodes_number - 1; ++i)
		{
			Vector3 straight2spring_vec = (followerPos - initPoss[i]).normalized * initDists[i];
			Vector3 nextPos =
				 node.position + Vector3.Slerp(initLookNext[i], straight2spring_vec, Easing.OutCubic((float)i / (nodes_number + 1)));
			node.LookAt(nextPos, localUp);

			node = node.GetChild(0);
		}
		node.LookAt(follower.position, localUp);
	}
	private void Update_Follower()
	{
		// update follower_pos & follower_vel
		Vector3 follower_pos = follower.position;
		Vector3 goal_pos = goal.position;
		damper_spring(ref follower_pos.x, ref follower_vel.x, goal_pos.x, goal_vel.x);
		damper_spring(ref follower_pos.y, ref follower_vel.y, goal_pos.y, goal_vel.y);
		damper_spring(ref follower_pos.z, ref follower_vel.z, goal_pos.z, goal_vel.z);
		follower.position = follower_pos;

		void damper_spring(ref float x, ref float v, in float x_goal, in float v_goal)
		{
			float dt = Time.fixedDeltaTime;
			float g = x_goal;
			float q = v_goal;
			float s = frequency_to_stiffness(frequency);
			float d = halflife_to_damping(halflife);
			float c = g + (d * q) / (s + Mathf.Epsilon);
			float y = d / 2.0f;

			if (Mathf.Abs(s - (d * d) / 4.0f) < Mathf.Epsilon) // Critically Damped
			{
				float j0 = x - c;
				float j1 = v + j0 * y;

				float eydt = fast_negexp(y * dt);

				x = j0 * eydt + dt * j1 * eydt + c;
				v = -y * j0 * eydt - y * dt * j1 * eydt + j1 * eydt;
			}
			else if (s - (d * d) / 4.0f > 0.0) // Under Damped
			{
				float w = Mathf.Sqrt(s - (d * d) / 4.0f);
				float j = Mathf.Sqrt(squaref(v + y * (x - c)) / (w * w + Mathf.Epsilon) + squaref(x - c));
				float p = Mathf.Atan((v + (x - c) * y) / (-(x - c) * w + Mathf.Epsilon));

				j = (x - c) > 0.0f ? j : -j;

				float eydt = fast_negexp(y * dt);

				x = j * eydt * Mathf.Cos(w * dt + p) + c;
				v = -y * j * eydt * Mathf.Cos(w * dt + p) - w * j * eydt * Mathf.Sin(w * dt + p);
			}
			else if (s - (d * d) / 4.0f < 0.0) // Over Damped
			{
				float y0 = (d + Mathf.Sqrt(d * d - 4 * s)) / 2.0f;
				float y1 = (d - Mathf.Sqrt(d * d - 4 * s)) / 2.0f;
				float j1 = (c * y0 - x * y0 - v) / (y1 - y0);
				float j0 = x - j1 - c;

				float ey0dt = fast_negexp(y0 * dt);
				float ey1dt = fast_negexp(y1 * dt);

				x = j0 * ey0dt + j1 * ey1dt + c;
				v = -y0 * j0 * ey0dt - y1 * j1 * ey1dt;
			}
		}
		float frequency_to_stiffness(float frequency)
		{
			return squaref(2.0f * Mathf.PI * frequency);
		}

		float halflife_to_damping(float halflife)
		{
			return (4.0f * 0.69314718056f) / (halflife + Mathf.Epsilon);
		}
		float fast_negexp(float x)
		{
			return 1.0f / (1.0f + x + 0.48f * x * x + 0.235f * x * x * x);
		}
		float squaref(float x) { return x * x; }
	}
}
