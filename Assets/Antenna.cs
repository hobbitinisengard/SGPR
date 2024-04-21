using RVP;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class Antenna : MonoBehaviour
{
	Transform goal;
	Vector3 goal_vel = Vector3.zero;
	VehicleParent vp;
	Transform follower;
	Vector3 follower_vel = Vector3.zero;

	int nodes = 0;
	public float halflife = 1; //drag
	public float frequency = 2; //Spring
	public float targetHeight = 0.26f; // how high above this should target be located?
	Vector3 goal_pos = Vector3.zero;
	Vector3 goal_prevPos = Vector3.zero;
	float[] nodes_y_heights;
	// Start is called before the first frame update
	Vector3 ElevateY(Vector3 pos, float height)
	{
		return pos + vp.tr.up * height;
		//return new Vector3(pos.x, pos.y + height, pos.z);
	}
	private void OnDestroy()
	{
		Destroy(follower.gameObject);
	}

	void Start()
	{
		vp = transform.GetTopmostParentComponent<VehicleParent>();
		nodes = transform.childCount;

		nodes_y_heights = new float[nodes];
		for (int i = 0; i < nodes; ++i)
			nodes_y_heights[i] = transform.localPosition.y + i * targetHeight;

		goal = new GameObject("Goal").transform;
		goal.parent = transform;
		goal_pos = ElevateY(transform.position, targetHeight * (nodes + 1));
		goal_prevPos = goal_pos;
		goal.position = goal_pos;

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
		Vector3 Up = vp.upDir * targetHeight;
		for (int i = 0; i < nodes - 1; ++i)
		{
			Vector3 straight2spring_vec = (follower.position - transform.GetChild(i).position).normalized * targetHeight;

			transform.GetChild(i + 1).position =
				 transform.GetChild(i).position + Vector3.Slerp(Up, straight2spring_vec, Easing.OutCubic((float)i / (nodes + 1)));
			transform.GetChild(i).transform
				 .LookAt(transform.GetChild(i + 1).position);
		}
		transform.GetChild(nodes - 1).transform.LookAt(follower.position);
	}
	private void Update_Follower()
	{
		goal_prevPos = goal_pos;
		goal_pos = goal.position;
		goal_vel = goal_pos - goal_prevPos;

		Vector3 follower_pos = follower.position;
		damper_spring(ref follower_pos.x, ref follower_vel.x, goal_pos.x, goal_vel.x);
		damper_spring(ref follower_pos.y, ref follower_vel.y, goal_pos.y, goal_vel.y);
		damper_spring(ref follower_pos.z, ref follower_vel.z, goal_pos.z, goal_vel.z);

		follower.position = follower_pos;


		//goal_prevPos = goal_pos;
		//goal_pos = goal.position;
		//goal_vel = goal_pos - goal_prevPos;

		//Vector3 follower_pos = follower.position;
		//damper_spring(ref follower_pos.x, ref follower_vel.x, goal_pos.x, goal_vel.x);
		//damper_spring(ref follower_pos.y, ref follower_vel.y, goal_pos.y, goal_vel.y);
		//damper_spring(ref follower_pos.z, ref follower_vel.z, goal_pos.z, goal_vel.z);
		
		//follower.position = follower_pos;

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
