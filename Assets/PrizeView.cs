using UnityEngine;

public class PrizeView : MonoBehaviour
{
	public static PrizeView I;
	public GameObject newUnlockPrefab;
	

	public void Awake()
	{
		I = this;
	}
	public void Prepare(string[] prizes)
	{

	}
	public void OKButton()
	{

	}
}
