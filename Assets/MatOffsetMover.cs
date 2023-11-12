using UnityEngine;

public class MatOffsetMover : MonoBehaviour
{
	MeshRenderer mr;
	public float speed = 0.5f;
	public float val = 0;
	public Axis axisXY = Axis.Y;
	void Start()
	{
		mr = GetComponent<MeshRenderer>();
	}

	void FixedUpdate()
	{
		val += Time.fixedDeltaTime * speed;
		val %= 2f;
		for (int i = 0; i < mr.materials.Length; ++i)
		{
			switch (axisXY)
			{
				case Axis.X:
					mr.materials[i].mainTextureOffset = new Vector4(val-1, 0);
					break;
				case Axis.Y:
					mr.materials[i].mainTextureOffset = new Vector4(0, val-1);
					break;
				case Axis.Z:
					mr.materials[i].mainTextureOffset = new Vector4(val-1, val-1);
					break;
			}
		}
	}
}
