using RVP;
using UnityEngine;

public class EnergyTransfer : MonoBehaviour
{
	AudioSource pitsBuzzing;
	public GameObject elecCam;
	private void Start()
	{
		pitsBuzzing = GetComponent<AudioSource>();
	}
	private void OnTriggerEnter(Collider other)
	{
		Debug.Log("enter");
		var vp = other.attachedRigidbody.transform.GetComponent<VehicleParent>();
		vp.SetBatteryLoading(true);
		var pitsPathCreator = transform.parent.parent.GetComponent<EnergyTunnelPath>().pitsPathCreator;
		vp.transform.GetComponent<FollowAI>().DriveThruPits(pitsPathCreator);
		pitsBuzzing.volume = 1;
		vp.elecTunnelCam = elecCam;
	}
	private void OnTriggerExit(Collider other)
	{
		Debug.Log("exit");
		var vp = other.attachedRigidbody.transform.GetComponent<VehicleParent>();
		vp.SetBatteryLoading(false);
		pitsBuzzing.volume = 0.5f;
		vp.elecTunnelCam = null;
	}
	private void OnTriggerStay(Collider other)
	{
		var vp = other.attachedRigidbody.transform.GetComponent<VehicleParent>();
		if (!vp.batteryLoadingSnd.isPlaying)
		{
			vp.batteryLoadingSnd.clip = Info.audioClips["elec" + Mathf.RoundToInt(3 * Random.value)];
			vp.batteryLoadingSnd.Play();
		}
		vp.battery = Mathf.Clamp01(vp.battery + vp.batteryLoadDelta*Time.fixedDeltaTime);
	}
}
