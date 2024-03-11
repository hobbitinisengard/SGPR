using UnityEngine;

[ExecuteAlways]
public class SkyboxController : MonoBehaviour
{
	GameObject nightTimeLights;
	public GameObject nightSkybox;
	public GameObject extraToTurnOffInNight;
	private void Awake()
	{
		if (transform.childCount > 0)
			nightTimeLights = transform.GetChild(0).gameObject;
	}
	private void OnEnable()
	{
		SetNightTimeLights();
	}
	public void SetNightTimeLights()
	{
		nightSkybox.SetActive(Info.s_isNight);

		if (nightTimeLights)
			nightTimeLights.SetActive(Info.s_isNight);

		if (extraToTurnOffInNight)
			extraToTurnOffInNight.SetActive(!Info.s_isNight);
	}
}
