using UnityEngine;

[ExecuteAlways]
public class SkyboxController : MonoBehaviour
{
	//[SerializeField] Transform _Sun = default;
	//[SerializeField] Transform _Moon = default;
	public GameObject nightTimeLights;
	private void OnEnable()
	{
		nightTimeLights.SetActive(Info.s_isNight);
	}
	//void LateUpdate()
	//{
	//	// Directions are defined to point towards the object

	//	// Sun
	//	Shader.SetGlobalVector("_SunDir", -_Sun.transform.forward);

	//	// Moon
	//	//Shader.SetGlobalVector("_MoonDir", -_Moon.transform.forward);
	//}
}
