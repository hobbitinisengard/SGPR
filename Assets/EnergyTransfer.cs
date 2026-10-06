using RVP;
using System.Collections.Generic;
using UnityEngine;

public class EnergyTransfer : MonoBehaviour
{
	AudioSource pitsBuzzing;
	public GameObject elecCam;
	readonly Dictionary<VehicleParent, HashSet<Collider>> collidersInTunnel = new();
	readonly Dictionary<VehicleParent, float> lastRefuelFixedTime = new();

	private void Start()
	{
		pitsBuzzing = GetComponent<AudioSource>();
	}
	private void OnTriggerEnter(Collider other)
	{
		VehicleParent vp = GetVehicle(other);
		if (!vp)
			return;

		if (collidersInTunnel.TryGetValue(vp, out HashSet<Collider> colliders))
		{
			colliders.Add(other);
			return;
		}

		colliders = new HashSet<Collider> { other };
		collidersInTunnel.Add(vp, colliders);
		RaceManager.I.hud.infoText.AddMessage(new Message(vp.name + " " + F.I.LocStr(F.I.s_raceType == RaceType.Survival ? "IS DISCHARGING!" : "IS RECHARGING!"), BottomInfoType.PIT_IN));
		vp.PlayBatteryLoadingFXs(true);
		var pitsPathCreator = transform.parent.parent.GetComponent<EnergyTunnelPath>().pitsPathCreator;
		// The player keeps control through the charging trigger. Their autodrive
		// starts at the dedicated PitsTriggerAutoDrive farther along the pit route.
		if (vp != RaceManager.I.playerCar)
			vp.followAI.DriveThruPits(pitsPathCreator);
		pitsBuzzing.volume = 1;
		vp.customCam = elecCam;
	}
	private void OnTriggerExit(Collider other)
	{
		VehicleParent vp = GetVehicle(other);
		if (!vp || !collidersInTunnel.TryGetValue(vp, out HashSet<Collider> colliders))
			return;

		colliders.Remove(other);
		if (colliders.Count > 0)
			return;

		collidersInTunnel.Remove(vp);
		lastRefuelFixedTime.Remove(vp);
		vp.PlayBatteryLoadingFXs(false);
		if (collidersInTunnel.Count == 0)
			pitsBuzzing.volume = 0.5f;
		vp.customCam = null;
	}
	private void OnTriggerStay(Collider other)
	{
		VehicleParent vp = GetVehicle(other);
		if (!vp || !collidersInTunnel.ContainsKey(vp))
			return;

		float fixedTime = Time.fixedTime;
		if (lastRefuelFixedTime.TryGetValue(vp, out float lastTime) && Mathf.Approximately(lastTime, fixedTime))
			return;
		lastRefuelFixedTime[vp] = fixedTime;
		vp.RefuelSourceEnergy();
	}

	static VehicleParent GetVehicle(Collider other)
	{
		if (!other || !other.attachedRigidbody)
			return null;
		return other.attachedRigidbody.GetComponent<VehicleParent>();
	}
}
