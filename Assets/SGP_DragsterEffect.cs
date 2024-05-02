using RVP;
using UnityEngine;
/// <summary>
/// real-time center of mass setting (trickstart + drifts)
/// </summary>
public class SGP_DragsterEffect : MonoBehaviour
{
	GearboxTransmission gearbox;
	public float COM_Movement = -0.6f;
	bool modified_COM = false;
	VehicleParent vp;
	
	void Awake()
	{
		vp = GetComponent<VehicleParent>();
		var engine = vp.engine as GasMotor;
		if (engine != null)
		{
			gearbox = engine.transmission;
		}
	}
	private void Start()
	{
		if (F.I.s_raceType == RaceType.Drift)
		{
			enabled = false;
		}
	}
	public void UpdateWorks()
	{
		if (gearbox)
		{
			if (vp.engine.boosting && vp.velMag > 0 && Mathf.Abs(vp.velMag) < 30 && !gearbox.IsShifting() && gearbox.selectedGear != 1)
			{
				ChassisSavable chassis = (ChassisSavable)vp.carConfig.GetPartReadonly(PartType.Chassis);
				var pos = vp.rb.centerOfMass;

				pos.z = chassis.longtitunalCOM + COM_Movement * (1 - Mathf.Abs(vp.velMag) / 30);

				vp.rb.centerOfMass = pos;
				modified_COM = true;
			}
			else if (modified_COM)
			{
				ChassisSavable chassis = (ChassisSavable)vp.carConfig.GetPartReadonly(PartType.Chassis);
				vp.rb.centerOfMass = new Vector3(0, chassis.verticalCOM, chassis.longtitunalCOM);
				modified_COM = false;
			}
		}
	}
	void Update()
	{
		UpdateWorks();
	}
}
