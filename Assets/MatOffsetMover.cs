using UnityEngine;

public class MatOffsetMover : MonoBehaviour
{
	MeshRenderer mr;
	public float speed = 0.5f;
	public float val = 0;
	void Start()
	{
		mr = GetComponent<MeshRenderer>();
	}

	// Update is called once per frame
	void Update()
	{
		val += Time.deltaTime * speed;
		val %= 1f;
		for (int i = 0; i < mr.materials.Length; ++i)
		{
			mr.materials[i].mainTextureOffset = new Vector4(0, val);
			//mr.materials[i].SetTextureOffset("_BaseMap", new Vector2(0, val));
			//mr.materials[i].SetVector("_Offset", new Vector4(0, val));
		}
	}
}
