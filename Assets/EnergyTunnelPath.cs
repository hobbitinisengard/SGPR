using PathCreation;
using UnityEngine;

public class EnergyTunnelPath : MonoBehaviour
{
	GameObject castableContainer;
	public PathCreator pitsPathCreator { get; private set; }
	public void CalculatePitsPath()
	{
		Tile tile = GetComponent<Tile>();
		castableContainer = new GameObject(this.name + " path container");
		pitsPathCreator = castableContainer.AddComponent<PathCreator>();
		Vector3[] pathPoints = tile.transform.GetChild(1).GetComponent<Connector>().PathsExtra();
		pitsPathCreator.bezierPath = new BezierPath(pathPoints, false, PathSpace.xyz);

		// Create castable points
		float progress = 0;
		for (int i = 0; i < 5000 && progress < pitsPathCreator.path.length; ++i)
		{
			GameObject castable = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			Destroy(castable.GetComponent<MeshRenderer>());
			castable.transform.position = pitsPathCreator.path.GetPointAtDistance(progress);
			castable.transform.parent = castableContainer.transform;
			var col = castable.GetComponent<SphereCollider>();
			col.radius = 1f;
			col.isTrigger = true;
			castable.layer = F.I.pitsLineLayer;
			castable.name = progress.ToString();
			progress += 3f;
		}
	}
	private void OnDestroy()
	{
		// destroy old castable points
		Destroy(castableContainer);
		//for (int i = 0; i < castableContainer.transform.childCount; ++i)
		//{
		//	Destroy(castableContainer.transform.GetChild(i).gameObject);
		//}
	}
}
