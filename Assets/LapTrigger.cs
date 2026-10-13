using RVP;
using UnityEngine;

public class LapTrigger : MonoBehaviour
{
	private void OnTriggerEnter(Collider car)
	{
		if (F.I.gameMode == GameMode.Skate || !car.attachedRigidbody) return;
		if (car.attachedRigidbody.TryGetComponent<RaceBox>(out var raceBox))
			raceBox.NextLap();
	}
}
