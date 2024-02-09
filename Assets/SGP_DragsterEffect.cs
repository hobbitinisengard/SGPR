using RVP;
using UnityEngine;

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

	void Update()
	{
		if (gearbox)
		{
			if (vp.engine.boosting && vp.velMag > 0 && Mathf.Abs(vp.velMag) < 30 && !gearbox.IsShifting() && gearbox.selectedGear != 1)
			{
				var pos = vp.centerOfMassObj.localPosition;
				pos.z = COM_Movement * (1 - Mathf.Abs(vp.velMag) / 30);
				
				vp.centerOfMassObj.localPosition = pos;
				vp.SetCenterOfMass();
				modified_COM = true;
			}
			else if (modified_COM)
			{
				var pos = vp.centerOfMassObj.localPosition;
				pos.z = 0;
				vp.centerOfMassObj.localPosition = pos;
				vp.SetCenterOfMass();
				modified_COM = false;
			}
		}
	}
}
