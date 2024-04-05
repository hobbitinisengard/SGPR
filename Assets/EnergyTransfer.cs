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
		//Debug.Log("enter");
		
		var vp = other.attachedRigidbody.transform.GetComponent<VehicleParent>();
		RaceManager.I.hud.infoText.AddMessage(new Message(vp.name + " IS RECHARGING!", BottomInfoType.PIT_IN));
		vp.SetBatteryLoading(true);
		var pitsPathCreator = transform.parent.parent.GetComponent<EnergyTunnelPath>().pitsPathCreator;
		vp.transform.GetComponent<FollowAI>().DriveThruPits(pitsPathCreator);
		pitsBuzzing.volume = 1;
		vp.customCam = elecCam;
	}
	private void OnTriggerExit(Collider other)
	{
		//Debug.Log("exit");
		var vp = other.attachedRigidbody.transform.GetComponent<VehicleParent>();
		vp.SetBatteryLoading(false);
		pitsBuzzing.volume = 0.5f;
		vp.customCam = null;
	}
	private void OnTriggerStay(Collider other)
	{
		var vp = other.attachedRigidbody.transform.GetComponent<VehicleParent>();
		vp.ChargeBattery();
	}
}
