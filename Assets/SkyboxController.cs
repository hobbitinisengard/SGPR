using UnityEngine;

[ExecuteAlways]
public class SkyboxController : MonoBehaviour
{
	GameObject nightTimeLights;
	public GameObject extraToTurnOffInNight;
	public GameObject nightSky;
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
		if (nightTimeLights)
			nightTimeLights.SetActive(F.I.s_isNight);

		nightSky.SetActive(F.I.s_isNight);

		if (extraToTurnOffInNight)
			extraToTurnOffInNight.SetActive(!F.I.s_isNight);
	}
}
