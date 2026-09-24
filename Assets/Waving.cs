using UnityEngine;

public class Waving : MonoBehaviour
{
	Vector3 initPos;
	public float waveSpeed = 1.0f;
	float beginPos = 597;
	float Length = 6;
	float sin = 0;
	void Start()
	{
		initPos = new Vector3(initPos.x, initPos.y, beginPos);
	}

	// from 591 to 603, so 597 +- 6
	void Update()
	{
		transform.position = new Vector3(initPos.x, initPos.y, beginPos + Length * Mathf.Sin(sin));
		sin += Time.deltaTime * waveSpeed;
	}
}
