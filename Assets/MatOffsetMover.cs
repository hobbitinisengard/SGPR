using UnityEngine;
namespace RVP
{
	public class MatOffsetMover : MonoBehaviour
	{
		MeshRenderer mr;
		public float speed = 0.5f;
		float val = 0;
		public Axis axisXY = Axis.Y;
		void Awake()
		{
			mr = GetComponent<MeshRenderer>();
		}

		void FixedUpdate()
		{
			val += Time.fixedDeltaTime * speed;
			val %= 2f;
			for (int i = 0; i < mr.materials.Length; ++i)
			{
				Vector2 offset;
				switch (axisXY)
				{
					case Axis.X:
						offset = new Vector2(val - 1, 0);
						break;
					case Axis.Y:
						offset = new Vector2(0, val - 1);
						break;
					case Axis.Z:
						offset = new Vector2(val - 1, val - 1);
						break;
					default:
						offset = new Vector2(val - 1, 0);
						break;
				}
				mr.materials[i].SetTextureOffset("_BaseColorMap", offset);
			}
		}
	}
}
