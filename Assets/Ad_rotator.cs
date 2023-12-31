using UnityEngine;
public enum Axis { X, Y, Z };
public class Ad_rotator : MonoBehaviour
{
	public float speed = 0.1f;
	float pos = 0;
	Vector3 init_rot;
	public Axis rotateAlong = Axis.Z;
	private void Start()
	{
		init_rot = transform.rotation.eulerAngles;
	}
	float degs(float deg)
	{
		if (deg > 360)
			deg -= 360;
		if (deg < 0)
			deg += 360;
		return deg;
	}
	// Update is called once per frame
	void FixedUpdate()
	{
		pos += speed * Time.fixedDeltaTime * 100;
		pos = degs(pos);
		switch (rotateAlong)
		{
			case Axis.X:
				transform.rotation = Quaternion.Euler(pos, init_rot.y, init_rot.z);
				break;
			case Axis.Y:
				transform.rotation = Quaternion.Euler(init_rot.x, pos, init_rot.z);
				break;
			case Axis.Z:
				transform.rotation = Quaternion.Euler(init_rot.x, init_rot.y, pos);
				break;
		}
	}
}
