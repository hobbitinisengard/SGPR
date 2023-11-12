using UnityEngine;

public class HeliLights : MonoBehaviour
{
	float timer0 = 0;
	public GameObject lights;
	private void OnEnable()
	{
		timer0 = Time.time;
	}
	void Update()
	{
		if(Time.time - timer0 > 0.5f)
		{
			timer0 = Time.time;
			lights.SetActive(!lights.activeSelf);
		}
	}
}
