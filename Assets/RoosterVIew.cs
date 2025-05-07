using TMPro;
using UnityEngine;

public class RoosterVIew : MonoBehaviour
{
	public Transform container;
	int allRoosters = 0;
	private void OnEnable()
	{
		for(int i=0; i<container.childCount; i++)
		{
			var carClass = container.GetChild(i);
			for(int j=0; j<carClass.childCount; j++)
			{
				var btn = carClass.GetChild(j);
				var car = F.I.Car(btn.name);
				btn.GetChild(0).GetComponent<TextMeshProUGUI>().text = F.I.LocStr(car.name) + ": " + car.rooster;
			}
		}
	}
	public void Reset()
	{
		foreach (var car in F.I.cars)
			car.rooster = 0;
		allRoosters = 0;
		OnEnable();
	}
	public void SwitchCarRooster(Transform btn)
	{
		int dir = F.I.shiftRef.action.ReadValue<float>() > 0.5f ? -1 : 1;
		var car = F.I.Car(btn.name);
		int newRoosters = allRoosters + dir;
		int newCarRooster = car.rooster + dir;
		if(newRoosters <= 9 && newRoosters >= 0 && newCarRooster <=9 && newCarRooster >= 0)
		{
			car.rooster += dir;
			allRoosters = newRoosters;
			btn.GetChild(0).GetComponent<TextMeshProUGUI>().text = F.I.LocStr(car.name) + ": " + car.rooster;
		}
	}
}
