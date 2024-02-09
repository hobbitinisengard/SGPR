using UnityEngine;
using UnityEngine.UIElements.Experimental;
namespace RVP
{
	public class JapDyndak : MonoBehaviour
	{
		// x -60, -110
		public Axis axis = Axis.Z;
		public float minVal = -110;
		public float maxVal = -60;
		float target = 0;
		float lastTime = 0;
		Quaternion init;
		Quaternion targetQ;
		Vector3 initialEuler;

		//AnimationCurve mover;
		public float moveTimeSecs = 1.5f;
		private void OnEnable()
		{
			lastTime = Time.time;
			init = transform.rotation;
			initialEuler = transform.rotation.eulerAngles;
		}
		private void Update()
		{
			if (Time.time - lastTime > moveTimeSecs)
			{
				lastTime = Time.time;
				target = maxVal - Random.value * (maxVal - minVal);
				init = transform.rotation;
				switch (axis)
				{
					case Axis.X:
						targetQ = Quaternion.Euler(target, initialEuler.y, initialEuler.z);
						break;
					case Axis.Y:
						targetQ = Quaternion.Euler(initialEuler.x, target, initialEuler.z);
						break;
					case Axis.Z:
						targetQ = Quaternion.Euler(initialEuler.x, initialEuler.y, target);
						break;
					default:
						break;
				}
			}
			transform.rotation = Quaternion.Lerp(init, targetQ,
				Easing.InOutSine((Time.time - lastTime) / moveTimeSecs));
		}
	}
}

