using RVP;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

// Worm models need to have their armature to be Z up.
// The fbx model needs armature export settings: Primary Bone +Z, Secondary Bone -X
public class WormAntenna : Antenna
{
	public Transform root;
	static Vector3[] initLookNext;
	static Vector3[] initUps;
	static float[] initDists;
	const int nodes = 2;

	void Start()
	{
		vp = transform.GetTopmostParentComponent<VehicleParent>();
		if (initLookNext == null)
		{
			initLookNext = new Vector3[nodes];
			initDists = new float[nodes - 1];
			initUps = new Vector3[nodes - 1];

			Transform node = root;
			for (int i = 0; i < nodes; ++i)
			{
				initLookNext[i] = node.GetChild(0).transform.position - node.transform.position;
				if(i < nodes - 1)
				{
					initDists[i] = Vector3.Distance(node.GetChild(0).transform.position, node.transform.position);
					initUps[i] = node.up;
				}
				node = node.GetChild(0);
			}
		}
		Transform lastNode = transform;
		for (int i = 0; i <= nodes; ++i)
			lastNode = lastNode.GetChild(0);

		goal.parent = vp.tr;
		goal_pos = goal.localPosition;
		goal_prevPos = goal_pos;
		goal_initPos = goal_pos;

		follower = new GameObject("Follower").transform;
		follower.gameObject.layer = 2;
		follower.parent = vp.tr;
		follower.position = goal.position;
	}
	void LateUpdate()
	{
		Transform node = root;
		Vector3 localUp = transform.up;
		for (int i = 0; i < nodes-1; ++i)
		{
			Vector3 straight2spring_vec = (follower.position - node.position).normalized * initDists[i];
			Vector3 nextPos =
				 node.position + Vector3.Slerp(initLookNext[i], straight2spring_vec, Easing.OutCubic((float)(i+1) / (nodes+1)));
			node.LookAt(nextPos, localUp);
			
			node = node.GetChild(0);
		}
		node.LookAt(follower.position, localUp);
	}
}
