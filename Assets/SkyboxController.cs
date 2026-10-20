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
        if (!F.I) return;
        bool night = F.I.s_timeOfDay == TimeOfDay.Night;
        // The daytime sky mesh lives on this root; disabling its child alone leaves
        // both sky shells rendering into the same depth buffer at night.
        foreach (var skyRenderer in GetComponents<Renderer>()) skyRenderer.enabled = !night;
        if (nightTimeLights)
            nightTimeLights.SetActive(F.I.s_timeOfDay == TimeOfDay.Night);

		if (nightSky) nightSky.SetActive(night);

		if (extraToTurnOffInNight)
			extraToTurnOffInNight.SetActive(F.I.s_timeOfDay != TimeOfDay.Night);
	}
}
