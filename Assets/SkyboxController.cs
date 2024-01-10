using UnityEngine;

[ExecuteAlways]
public class SkyboxController : MonoBehaviour
{
	[SerializeField] Transform _Sun = default;
	//[SerializeField] Transform _Moon = default;
	GameObject nightTimeLights;
	public GameObject nightSkybox;
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
	}
	//void LateUpdate()
	//{
		// Directions are defined to point towards the object

		// Sun
		//Shader.SetGlobalVector("_SunDir", -_Sun.transform.forward);

		// Moon
		//Shader.SetGlobalVector("_MoonDir", -_Moon.transform.forward);
	//}
}
