using UnityEngine;

public class RandomHelmetColor : MonoBehaviour
{
	void Start()
	{
		var mr = GetComponent<MeshRenderer>();
		var mats = mr.materials;
		mats[0].color = F.RandomColor();
		mats[1].color = F.RandomColor();
		mr.materials = mats;
	}
}
