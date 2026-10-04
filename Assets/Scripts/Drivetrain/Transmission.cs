using UnityEngine;

namespace RVP
{
	public abstract class Transmission : MonoBehaviour
	{
		public float strength = 1;
		[System.NonSerialized] public float health = 1;
		protected VehicleParent vp;

		public virtual void Start()
		{
			vp = transform.GetTopmostParentComponent<VehicleParent>();
		}
	}
}