using RVP;
using UnityEngine;

public class LapTrigger : MonoBehaviour
{
	private void OnTriggerEnter(Collider car)
	{
		car.attachedRigidbody.GetComponent<RaceBox>().NextLap();
		car.attachedRigidbody.GetComponent<FollowAI>().progress = 0;
	}
}
