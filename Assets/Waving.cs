using UnityEngine;

public class Waving : MonoBehaviour
{
	Vector3 initPos;
	public float waveSpeed = 1.0f;
	public float beginPos = 0.27f;
	public float Length = 0.035f;
	float sin = 0;
	MeshRenderer mr;
	void Start()
	{
		initPos = new Vector3(initPos.x, initPos.y, beginPos);
		mr = GetComponent<MeshRenderer>();
	}
	void Update()
	{
		mr.materials[0].mainTextureOffset = new Vector2(0, beginPos + Length * Mathf.Sin(sin));
		sin += Time.deltaTime * waveSpeed;
	}
}
